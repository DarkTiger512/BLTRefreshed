using System.Collections.Concurrent;

namespace BLT.ExtensionService.Infrastructure;

public sealed class RequestGuard(IConfiguration configuration)
{
    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> rates = new();
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> replay = new();
    private int Limit => configuration.GetValue("BLT:ViewerActionsPerMinute", 12);
    private int MaxAge => configuration.GetValue("BLT:RequestMaxAgeSeconds", 30);

    public bool Accept(Guid requestId, string partition, DateTimeOffset timestamp, out string error)
    {
        error = "";
        var now = DateTimeOffset.UtcNow;
        if (timestamp < now.AddSeconds(-MaxAge) || timestamp > now.AddSeconds(5)) { error = "Request timestamp is stale."; return false; }
        foreach (var item in replay.Where(item => item.Value < now.AddMinutes(-5)).ToArray()) replay.TryRemove(item.Key, out _);
        if (!replay.TryAdd(requestId, now)) { error = "Duplicate request."; return false; }
        var queue = rates.GetOrAdd(partition, _ => new Queue<DateTimeOffset>());
        lock (queue)
        {
            while (queue.TryPeek(out var oldest) && oldest < now.AddMinutes(-1)) queue.Dequeue();
            if (queue.Count >= Limit) { error = "Action rate limit exceeded."; return false; }
            queue.Enqueue(now);
        }
        return true;
    }
}
