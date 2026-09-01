using System;
using System.Collections.Concurrent;
using System.Threading;

namespace BannerlordTwitch.Integration
{
    public sealed class IntegrationRequestLifecycle : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> pending = new();
        private readonly ConcurrentDictionary<Guid, byte> terminal = new();

        public bool TryAccept(Guid requestId, out CancellationToken timeoutToken)
        {
            timeoutToken = default;
            if (terminal.ContainsKey(requestId)) return false;
            var source = new CancellationTokenSource();
            if (!pending.TryAdd(requestId, source)) { source.Dispose(); return false; }
            timeoutToken = source.Token;
            return true;
        }

        public bool TryComplete(Guid requestId)
        {
            if (!terminal.TryAdd(requestId, 0)) return false;
            CancelPending(requestId);
            return true;
        }

        public bool TryExpire(Guid requestId)
        {
            if (!pending.TryRemove(requestId, out var source)) return false;
            source.Dispose();
            return terminal.TryAdd(requestId, 0);
        }

        public void Forget(Guid requestId) => terminal.TryRemove(requestId, out _);

        private void CancelPending(Guid requestId)
        {
            if (!pending.TryRemove(requestId, out var source)) return;
            source.Cancel();
            source.Dispose();
        }

        public void Dispose()
        {
            foreach (var source in pending.Values) { source.Cancel(); source.Dispose(); }
            pending.Clear(); terminal.Clear();
        }
    }
}
