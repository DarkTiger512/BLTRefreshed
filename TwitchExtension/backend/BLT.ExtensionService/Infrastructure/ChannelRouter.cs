using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using BLT.ExtensionService.Models;

namespace BLT.ExtensionService.Infrastructure;

public sealed class ChannelRouter(ChannelStateCache stateCache)
{
    private static readonly JsonSerializerOptions WireJson = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, WebSocket> games = new(StringComparer.Ordinal);
    private sealed record ViewerConnection(string UserId, string DisplayName, IReadOnlyList<string> Roles, WebSocket Socket);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, ViewerConnection>> viewers = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Guid, (string Channel, string UserId)> privateRequests = new();

    public bool IsGameConnected(string channel) => games.TryGetValue(channel, out var socket) && socket.State == WebSocketState.Open;
    public DateTimeOffset? LastStateAt(string channel) => stateCache.LastStateAt(channel);
    public JsonElement RuntimeCommands(string channel)
    {
        if (stateCache.TryGet(channel, out var state))
        {
            using var document = JsonDocument.Parse(state);
            if (document.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("commands", out var commands)) return commands.Clone();
        }
        return JsonSerializer.SerializeToElement(Array.Empty<object>());
    }
    public Task BroadcastConfigurationAsync(string channel, ChannelConfiguration configuration, CancellationToken token) =>
        BroadcastViewerAsync(channel, Envelope("configuration.updated", channel, new { configuration.SchemaVersion, configuration.ExtensionEnabled, configuration.Commands, configuration.Revision, configuration.UpdatedAt }), token);

    public async Task AttachGameAsync(string channel, WebSocket socket, CancellationToken token)
    {
        if (games.TryGetValue(channel, out var previous) && previous.State == WebSocketState.Open)
            await previous.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Replaced by a new game connection", token);
        stateCache.Clear(channel);
        games[channel] = socket;
        await BroadcastViewerAsync(channel, Envelope("connection.status", channel, new { connected = true, gameStarted = true }), token);
        await PumpAsync(socket, async message => await RouteGameMessageAsync(channel, message, token), token);
        if (games.TryRemove(new KeyValuePair<string, WebSocket>(channel, socket)))
        {
            stateCache.Clear(channel);
            await BroadcastViewerAsync(channel, Envelope("connection.status", channel, new { connected = false, gameStarted = false }), CancellationToken.None);
        }
    }

    public async Task AttachViewerAsync(string channel, TwitchPrincipal principal, WebSocket socket, CancellationToken token)
    {
        var id = Guid.NewGuid();
        viewers.GetOrAdd(channel, _ => new ConcurrentDictionary<Guid, ViewerConnection>())[id] = new(principal.UserId, principal.DisplayName, principal.Roles, socket);
        await SendAsync(socket, Envelope("connection.status", channel, new { connected = IsGameConnected(channel), gameStarted = IsGameConnected(channel) }), token);
        if (stateCache.TryGet(channel, out var state)) await SendAsync(socket, state, token);
        await SendGameAsync(channel, new { v = ProtocolKinds.Version, id = Guid.NewGuid(), kind = "viewer.subscribe", channelId = channel, timestamp = DateTimeOffset.UtcNow, user = new IntegrationUser(principal.UserId, principal.DisplayName, principal.Roles), data = new { } }, token);
        await PumpAsync(socket, _ => Task.CompletedTask, token);
        if (viewers.TryGetValue(channel, out var channelViewers)) channelViewers.TryRemove(id, out _);
        if (!viewers.TryGetValue(channel, out channelViewers) || !channelViewers.Values.Any(viewer => viewer.UserId == principal.UserId))
            await SendGameAsync(channel, new { v = ProtocolKinds.Version, id = Guid.NewGuid(), kind = "viewer.unsubscribe", channelId = channel, timestamp = DateTimeOffset.UtcNow, user = new IntegrationUser(principal.UserId, principal.DisplayName, principal.Roles), data = new { } }, CancellationToken.None);
    }

    public async Task<bool> SendGameAsync(string channel, object payload, CancellationToken token)
    {
        if (!games.TryGetValue(channel, out var socket) || socket.State != WebSocketState.Open) return false;
        await SendAsync(socket, JsonSerializer.Serialize(payload, WireJson), token);
        return true;
    }

    public void RegisterPrivateRequest(Guid requestId, string channel, string userId) => privateRequests[requestId] = (channel, userId);
    public void ForgetPrivateRequest(Guid requestId) => privateRequests.TryRemove(requestId, out _);

    private async Task RouteGameMessageAsync(string channel, string message, CancellationToken token)
    {
        try
        {
            using var document = JsonDocument.Parse(message);
            var root = document.RootElement;
            var kind = root.GetProperty("kind").GetString();
            if (kind is "state.snapshot" or "state.patch")
            {
                if (!stateCache.TryAccept(channel, message, out _)) return;
            }
            if (kind is "action.accepted" or "action.result" or "action.error" or "inventory.snapshot" or "inventory.error" or "retinue.snapshot" or "retinue.error")
            {
                var requestId = root.GetProperty("id").GetGuid();
                if (privateRequests.TryGetValue(requestId, out var target) && target.Channel == channel)
                {
                    await SendViewerAsync(channel, target.UserId, message, token);
                    if (kind is not "action.accepted") ForgetPrivateRequest(requestId);
                }
                return;
            }
            if (kind == "viewer.state")
            {
                var targetUserId = root.GetProperty("data").GetProperty("userId").GetString();
                if (!string.IsNullOrWhiteSpace(targetUserId)) await SendViewerAsync(channel, targetUserId, message, token);
                return;
            }
        }
        catch (JsonException) { return; }
        await BroadcastViewerAsync(channel, message, token);
    }

    private async Task SendViewerAsync(string channel, string userId, string message, CancellationToken token)
    {
        if (!viewers.TryGetValue(channel, out var sockets)) return;
        foreach (var pair in sockets.Where(pair => pair.Value.UserId == userId).ToArray())
        {
            if (pair.Value.Socket.State != WebSocketState.Open) { sockets.TryRemove(pair.Key, out _); continue; }
            try { await SendAsync(pair.Value.Socket, message, token); } catch (WebSocketException) { sockets.TryRemove(pair.Key, out _); }
        }
    }

    private async Task BroadcastViewerAsync(string channel, string message, CancellationToken token)
    {
        if (!viewers.TryGetValue(channel, out var sockets)) return;
        foreach (var pair in sockets.ToArray())
        {
            if (pair.Value.Socket.State != WebSocketState.Open) { sockets.TryRemove(pair.Key, out _); continue; }
            try { await SendAsync(pair.Value.Socket, message, token); } catch (WebSocketException) { sockets.TryRemove(pair.Key, out _); }
        }
    }

    private static async Task PumpAsync(WebSocket socket, Func<string, Task> onMessage, CancellationToken token)
    {
        var buffer = new byte[64 * 1024];
        try
        {
            while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                using var stream = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(buffer, token);
                    if (result.MessageType == WebSocketMessageType.Close) return;
                    stream.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);
                if (result.MessageType == WebSocketMessageType.Text) await onMessage(Encoding.UTF8.GetString(stream.ToArray()));
            }
        }
        catch (WebSocketException) { }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    private static Task SendAsync(WebSocket socket, string message, CancellationToken token) =>
        socket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, token);

    private static string Envelope(string kind, string channel, object data) => JsonSerializer.Serialize(new
    {
        v = ProtocolKinds.Version, id = Guid.NewGuid(), kind, channelId = channel, timestamp = DateTimeOffset.UtcNow, data
    });
}
