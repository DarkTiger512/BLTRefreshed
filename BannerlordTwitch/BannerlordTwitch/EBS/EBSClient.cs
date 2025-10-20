using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BannerlordTwitch.EBS
{
    public class EBSClient : IDisposable
    {
        private readonly Uri _endpoint;
        private readonly string _channelId;
        private readonly string _modToken;
        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        private Task _receiveLoopTask;

        public bool Connected => _ws != null && _ws.State == WebSocketState.Open;

        public EBSClient(string endpoint, string channelId, string modToken)
        {
            _endpoint = new Uri(endpoint);
            _channelId = channelId;
            _modToken = modToken;
        }

        public void Start()
        {
            if (Connected) return;
            _cts = new CancellationTokenSource();
            _receiveLoopTask = Task.Run(() => ConnectLoop(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            try { _ws?.Abort(); } catch { }
            _ws = null;
        }

        private async Task ConnectLoop(CancellationToken ct)
        {
            int attempt = 0;
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await ConnectOnce(ct);
                    attempt = 0;

                    // Receive loop
                    var buffer = new byte[8192];
                    while (_ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                    {
                        var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                        if (result.MessageType == WebSocketMessageType.Close) break;
                        var txt = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        try
                        {
                            var doc = JsonDocument.Parse(txt);
                            if (doc.RootElement.TryGetProperty("type", out var t) && t.GetString() == "viewer_action")
                            {
                                // TODO: dispatch to command system. Not obvious entrypoint; log for now.
                                var action = doc.RootElement.GetProperty("action").GetString();
                                Console.WriteLine($"[EBS] viewer_action received: {action}");
                            }
                        }
                        catch { }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EBS] connect/receive error: {ex.Message}");
                }

                attempt++;
                var delay = Math.Min(30000, (int)Math.Pow(2, Math.Min(6, attempt)) * 1000);
                await Task.Delay(delay, ct).ContinueWith(_ => { });
            }
        }

        private async Task ConnectOnce(CancellationToken ct)
        {
            _ws?.Dispose();
            _ws = new ClientWebSocket();
            var query = $"/mod?channel_id={Uri.EscapeDataString(_channelId)}&token={Uri.EscapeDataString(_modToken)}";
            var uriBuilder = new UriBuilder(_endpoint) { Path = _endpoint.AbsolutePath.TrimEnd('/') + "/mod", Query = $"channel_id={Uri.EscapeDataString(_channelId)}&token={Uri.EscapeDataString(_modToken)}" };
            var wsUri = uriBuilder.Uri;
            Console.WriteLine($"[EBS] connecting to {wsUri}");
            await _ws.ConnectAsync(wsUri, ct);

            // start ping loop
            _ = Task.Run(async () => {
                while (_ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    try
                    {
                        var ping = Encoding.UTF8.GetBytes("ping");
                        await _ws.SendAsync(new ArraySegment<byte>(ping), WebSocketMessageType.Text, true, ct);
                    }
                    catch { }
                    await Task.Delay(30000, ct).ContinueWith(_ => { });
                }
            }, ct);
        }

        public async Task SendOverlayState(object state)
        {
            if (!Connected) return;
            try
            {
                var payload = JsonSerializer.Serialize(new { type = "overlay_state", state });
                var bytes = Encoding.UTF8.GetBytes(payload);
                await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EBS] send error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Stop();
            _ws?.Dispose();
        }
    }
}
