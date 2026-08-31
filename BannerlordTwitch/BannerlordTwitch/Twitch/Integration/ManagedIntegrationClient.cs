using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BannerlordTwitch.Util;

namespace BannerlordTwitch.Integration
{
    public sealed class IntegrationInventoryItem
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Equipped { get; set; }
    }

    public sealed class IntegrationEquipmentSlot
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string Accepts { get; set; }
        public string ItemName { get; set; }
        public int? CustomItemIndex { get; set; }
    }

    public sealed class IntegrationInventorySnapshot
    {
        public string HeroName { get; set; }
        public int Limit { get; set; }
        public string Error { get; set; }
        public IntegrationInventoryItem[] Items { get; set; } = Array.Empty<IntegrationInventoryItem>();
        public IntegrationEquipmentSlot[] Slots { get; set; } = Array.Empty<IntegrationEquipmentSlot>();
    }

    public static class IntegrationInventoryProvider
    {
        public static Func<string, IntegrationInventorySnapshot> Get { private get; set; }
        public static IntegrationInventorySnapshot For(string userName) => Get?.Invoke(userName) ?? new IntegrationInventorySnapshot { Error = "Inventory is unavailable in this game." };
    }

    public sealed class ManagedIntegrationClient : IDisposable
    {
        private readonly AuthSettings auth;
        private readonly string channelId;
        private readonly IntegrationActionCatalog catalog;
        private readonly HttpClient http = new();
        private readonly CancellationTokenSource lifetime = new();
        private ClientWebSocket socket;
        private bool disposed;
        private readonly ConcurrentDictionary<Guid, byte> receivedRequests = new();

        public event Action<IntegrationActionRequest> ActionRequested;
        public bool IsConnected => socket?.State == WebSocketState.Open;

        public ManagedIntegrationClient(AuthSettings auth, string channelId)
        {
            this.auth = auth;
            this.channelId = channelId;
            catalog = IntegrationActionCatalog.Load();
            _ = RunAsync();
        }

        private async Task RunAsync()
        {
            var delay = TimeSpan.FromSeconds(2);
            while (!disposed)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(auth.IntegrationCredential) && !string.IsNullOrWhiteSpace(auth.IntegrationPairingCode))
                        await ExchangePairingCodeAsync(lifetime.Token);
                    if (!auth.IntegrationConfigured) return;
                    socket?.Dispose();
                    socket = new ClientWebSocket();
                    socket.Options.SetRequestHeader("Authorization", $"Bearer {auth.IntegrationCredential}");
                    socket.Options.AddSubProtocol("blt.integration.v1");
                    await socket.ConnectAsync(GameSocketUri(), lifetime.Token);
                    delay = TimeSpan.FromSeconds(2);
                    Log.LogFeedSystem("[Integration] Connected to managed Twitch Extension service");
                    await SendAsync("hello", new { modVersion = typeof(ManagedIntegrationClient).Assembly.GetName().Version?.ToString(), protocolVersion = IntegrationProtocol.Version }, lifetime.Token);
                    await SendRawAsync(JsonSerializer.Serialize(new { v = IntegrationProtocol.Version, id = Guid.NewGuid(), kind = "manifest", channelId, timestamp = DateTimeOffset.UtcNow, data = JsonSerializer.Deserialize<JsonElement>(catalog.ManifestJson) }), lifetime.Token);
                    await SendAsync("state.snapshot", new { connected = true, gameStarted = Settings.GameStarted, unavailable = new { }, cooldowns = new { } }, lifetime.Token);
                    await ReceiveAsync(lifetime.Token);
                }
                catch (OperationCanceledException) { return; }
                catch (Exception ex) { Log.Error($"[Integration] Connection failed: {ex.Message}"); }
                if (disposed) return;
                try { await Task.Delay(delay, lifetime.Token); } catch (OperationCanceledException) { return; }
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 60));
            }
        }

        private async Task ExchangePairingCodeAsync(CancellationToken token)
        {
            var endpoint = new Uri(new Uri(EnsureTrailingSlash(auth.IntegrationServiceUrl)), "api/pairing/exchange");
            var payload = JsonSerializer.Serialize(new { code = auth.IntegrationPairingCode.Trim().ToUpperInvariant() });
            using var response = await http.PostAsync(endpoint, new StringContent(payload, Encoding.UTF8, "application/json"), token);
            response.EnsureSuccessStatusCode();
            var exchange = JsonSerializer.Deserialize<PairingExchangeResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (exchange == null || !string.Equals(exchange.ChannelId, channelId, StringComparison.Ordinal)) throw new InvalidDataException("Pairing response did not match this Twitch channel");
            auth.IntegrationChannelId = exchange.ChannelId;
            auth.IntegrationInstallationId = exchange.InstallationId;
            auth.IntegrationCredential = exchange.InstallationCredential;
            auth.IntegrationPairingCode = null;
            AuthSettings.Save(auth);
            Log.LogFeedSystem("[Integration] Twitch Extension pairing completed");
        }

        private async Task ReceiveAsync(CancellationToken token)
        {
            var buffer = new byte[64 * 1024];
            while (socket.State == WebSocketState.Open)
            {
                using var stream = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close) return;
                    stream.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);
                if (result.MessageType == WebSocketMessageType.Text) HandleMessage(Encoding.UTF8.GetString(stream.ToArray()));
            }
        }

        private void HandleMessage(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.GetProperty("v").GetInt32() != IntegrationProtocol.Version) return;
                var kind = root.GetProperty("kind").GetString();
                var data = root.GetProperty("data");
                var user = root.GetProperty("user");
                if (kind == "inventory.request")
                {
                    var requestId = root.GetProperty("id").GetGuid();
                    var userName = user.GetProperty("name").GetString();
                    MainThreadSync.Run(() =>
                    {
                        var inventory = IntegrationInventoryProvider.For(userName);
                        _ = string.IsNullOrEmpty(inventory.Error)
                            ? SendAsync("inventory.snapshot", inventory, lifetime.Token, requestId)
                            : SendAsync("inventory.error", new { error = inventory.Error }, lifetime.Token, requestId);
                    });
                    return;
                }
                if (kind != "action.request") return;
                var request = new IntegrationActionRequest
                {
                    RequestId = root.GetProperty("id").GetGuid(), ChannelId = root.GetProperty("channelId").GetString(), Timestamp = root.GetProperty("timestamp").GetDateTimeOffset(),
                    ActionId = data.GetProperty("actionId").GetString(), Args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(data.GetProperty("args").GetRawText()),
                    User = new IntegrationUser { Id = user.GetProperty("id").GetString(), Name = user.GetProperty("name").GetString(), Roles = JsonSerializer.Deserialize<string[]>(user.GetProperty("roles").GetRawText()) }
                };
                if (!string.Equals(request.ChannelId, channelId, StringComparison.Ordinal)) return;
                if (Math.Abs((DateTimeOffset.UtcNow - request.Timestamp).TotalSeconds) > 30) return;
                if (!receivedRequests.TryAdd(request.RequestId, 0)) return;
                ActionRequested?.Invoke(request);
            }
            catch (Exception ex) { Log.Error($"[Integration] Rejected malformed action request: {ex.Message}"); }
        }

        public bool TryResolve(IntegrationActionRequest request, out IntegrationActionDefinition definition, out string legacyArgs, out string error)
        {
            definition = null; legacyArgs = null; error = null;
            try
            {
                if (!catalog.TryGet(request.ActionId, out definition)) { error = "Unknown action."; return false; }
                legacyArgs = catalog.BuildLegacyArguments(definition, request.Args);
                return true;
            }
            catch (ArgumentException ex) { error = ex.Message; return false; }
        }

        public Task SendActionAcceptedAsync(Guid requestId) => SendAsync("action.accepted", new { requestId }, lifetime.Token, requestId);
        public Task SendActionResultAsync(Guid requestId, string[] messages) => SendAsync("action.result", new { requestId, messages }, lifetime.Token, requestId);
        public Task SendActionErrorAsync(Guid requestId, string error) => SendAsync("action.error", new { requestId, error }, lifetime.Token, requestId);

        private Task SendAsync(string kind, object data, CancellationToken token, Guid? id = null) =>
            SendRawAsync(JsonSerializer.Serialize(new { v = IntegrationProtocol.Version, id = id ?? Guid.NewGuid(), kind, channelId, timestamp = DateTimeOffset.UtcNow, data }), token);

        private async Task SendRawAsync(string json, CancellationToken token)
        {
            if (socket?.State != WebSocketState.Open) return;
            var bytes = Encoding.UTF8.GetBytes(json);
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
        }

        private Uri GameSocketUri()
        {
            var builder = new UriBuilder(new Uri(new Uri(EnsureTrailingSlash(auth.IntegrationServiceUrl)), $"ws/game/{Uri.EscapeDataString(channelId)}"))
            { Scheme = auth.IntegrationServiceUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws" };
            return builder.Uri;
        }

        private static string EnsureTrailingSlash(string value) => value.EndsWith("/") ? value : value + "/";

        public void Dispose()
        {
            if (disposed) return;
            disposed = true; lifetime.Cancel(); socket?.Dispose(); http.Dispose(); lifetime.Dispose();
        }
    }
}
