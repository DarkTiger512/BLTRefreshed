using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using BLT.ExtensionService.Models;

namespace BLT.ExtensionService.Infrastructure;

public sealed class ChannelRouter
{
    private readonly ConcurrentDictionary<string, WebSocket> games = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WebSocket>> viewers = new(StringComparer.Ordinal);

    public bool IsGameConnected(string channel) => games.TryGetValue(channel, out var socket) && socket.State == WebSocketState.Open;

    public async Task AttachGameAsync(string channel, WebSocket socket, CancellationToken token)
    {
        if (games.TryGetValue(channel, out var previous) && previous.State == WebSocketState.Open)
            await previous.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Replaced by a new game connection", token);
        games[channel] = socket;
        await BroadcastViewerAsync(channel, Envelope("connection.status", channel, new { connected = true, gameStarted = true }), token);
        await PumpAsync(socket, async message => await BroadcastViewerAsync(channel, message, token), token);
        games.TryRemove(new KeyValuePair<string, WebSocket>(channel, socket));
        await BroadcastViewerAsync(channel, Envelope("connection.status", channel, new { connected = false, gameStarted = false }), CancellationToken.None);
    }

    public async Task AttachViewerAsync(string channel, WebSocket socket, CancellationToken token)
    {
        var id = Guid.NewGuid();
        viewers.GetOrAdd(channel, _ => new ConcurrentDictionary<Guid, WebSocket>())[id] = socket;
        await SendAsync(socket, Envelope("connection.status", channel, new { connected = IsGameConnected(channel), gameStarted = IsGameConnected(channel) }), token);
        await PumpAsync(socket, _ => Task.CompletedTask, token);
        if (viewers.TryGetValue(channel, out var channelViewers)) channelViewers.TryRemove(id, out _);
    }

    public async Task<bool> SendGameAsync(string channel, object payload, CancellationToken token)
    {
        if (!games.TryGetValue(channel, out var socket) || socket.State != WebSocketState.Open) return false;
        await SendAsync(socket, JsonSerializer.Serialize(payload), token);
        return true;
    }

    private async Task BroadcastViewerAsync(string channel, string message, CancellationToken token)
    {
        if (!viewers.TryGetValue(channel, out var sockets)) return;
        foreach (var pair in sockets.ToArray())
        {
            if (pair.Value.State != WebSocketState.Open) { sockets.TryRemove(pair.Key, out _); continue; }
            try { await SendAsync(pair.Value, message, token); } catch (WebSocketException) { sockets.TryRemove(pair.Key, out _); }
        }
    }

    private static async Task PumpAsync(WebSocket socket, Func<string, Task> onMessage, CancellationToken token)
    {
        var buffer = new byte[64 * 1024];
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

    private static Task SendAsync(WebSocket socket, string message, CancellationToken token) =>
        socket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, token);

    private static string Envelope(string kind, string channel, object data) => JsonSerializer.Serialize(new
    {
        v = ProtocolKinds.Version, id = Guid.NewGuid(), kind, channelId = channel, timestamp = DateTimeOffset.UtcNow, data
    });
}
