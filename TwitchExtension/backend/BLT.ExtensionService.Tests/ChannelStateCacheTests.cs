using System.Text.Json;
using BLT.ExtensionService.Infrastructure;

namespace BLT.ExtensionService.Tests;

public sealed class ChannelStateCacheTests
{
    [Fact]
    public void RebuildsLatestSnapshotAndRejectsStaleOrWrongChannelPatches()
    {
        var cache = new ChannelStateCache();
        Assert.True(cache.TryAccept("42", Envelope("state.snapshot", "42", 5, 100), out _));
        Assert.True(cache.TryAccept("42", Envelope("state.patch", "42", 6, 75), out _));
        Assert.False(cache.TryAccept("42", Envelope("state.patch", "42", 6, 50), out _));
        Assert.False(cache.TryAccept("42", Envelope("state.patch", "other", 7, 50), out _));
        Assert.True(cache.TryGet("42", out var snapshot));
        using var document = JsonDocument.Parse(snapshot);
        Assert.Equal("state.snapshot", document.RootElement.GetProperty("kind").GetString());
        Assert.Equal(6, document.RootElement.GetProperty("data").GetProperty("mission").GetProperty("revision").GetInt64());
        Assert.Equal(75, document.RootElement.GetProperty("data").GetProperty("mission").GetProperty("combatants")[0].GetProperty("hp").GetInt32());
        Assert.True(document.RootElement.GetProperty("data").GetProperty("gameStarted").GetBoolean());
    }

    [Fact]
    public void RequiresSnapshotBeforePatchAndClearRemovesViewerReplay()
    {
        var cache = new ChannelStateCache();
        Assert.False(cache.TryAccept("42", Envelope("state.patch", "42", 1, 75), out _));
        Assert.True(cache.TryAccept("42", Envelope("state.snapshot", "42", 1, 100), out _));
        Assert.NotNull(cache.LastStateAt("42"));
        cache.Clear("42");
        Assert.False(cache.TryGet("42", out _));
        Assert.Null(cache.LastStateAt("42"));
    }

    [Fact]
    public void AcceptsCampaignTransitionWithoutMissionRevisionChange()
    {
        var cache = new ChannelStateCache();
        Assert.True(cache.TryAccept("42", Envelope("state.snapshot", "42", 5, 100, false), out _));
        Assert.True(cache.TryAccept("42", Envelope("state.patch", "42", 5, 100, true), out var normalized));
        using var document = JsonDocument.Parse(normalized);
        Assert.True(document.RootElement.GetProperty("data").GetProperty("gameStarted").GetBoolean());
    }

    private static string Envelope(string kind, string channel, long revision, int hp, bool gameStarted = true) => JsonSerializer.Serialize(new
    {
        v = 1, id = Guid.NewGuid(), kind, channelId = channel, timestamp = DateTimeOffset.UtcNow,
        data = new { gameStarted, mission = new { active = true, kind = "battle", revision, combatants = new[] { new { id = "hero", hp } } } }
    });
}
