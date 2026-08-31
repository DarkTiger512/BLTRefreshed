using BLT.ExtensionService.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace BLT.ExtensionService.Tests;

public sealed class RequestGuardTests
{
    private static RequestGuard Create(int limit = 2) => new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["BLT:ViewerActionsPerMinute"] = limit.ToString(), ["BLT:RequestMaxAgeSeconds"] = "30"
    }).Build());

    [Fact]
    public void RejectsDuplicateAndStaleRequests()
    {
        var guard = Create();
        var id = Guid.NewGuid();
        Assert.True(guard.Accept(id, "channel:user:action", DateTimeOffset.UtcNow, out _));
        Assert.False(guard.Accept(id, "channel:user:action", DateTimeOffset.UtcNow, out var duplicate));
        Assert.Equal("Duplicate request.", duplicate);
        Assert.False(guard.Accept(Guid.NewGuid(), "channel:user:action", DateTimeOffset.UtcNow.AddMinutes(-1), out var stale));
        Assert.Equal("Request timestamp is stale.", stale);
    }

    [Fact]
    public void RateLimitsPerViewerActionPartition()
    {
        var guard = Create(1);
        Assert.True(guard.Accept(Guid.NewGuid(), "a", DateTimeOffset.UtcNow, out _));
        Assert.False(guard.Accept(Guid.NewGuid(), "a", DateTimeOffset.UtcNow, out var error));
        Assert.Equal("Action rate limit exceeded.", error);
        Assert.True(guard.Accept(Guid.NewGuid(), "b", DateTimeOffset.UtcNow, out _));
    }
}
