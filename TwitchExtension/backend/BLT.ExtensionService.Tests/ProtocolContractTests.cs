using BLT.ExtensionService.Models;

namespace BLT.ExtensionService.Tests;

public sealed class ProtocolContractTests
{
    [Fact]
    public void PrestigeIsOptionalAndRoundTripsWithoutLosingRequirementsOrRanks()
    {
        var options = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
        var old = System.Text.Json.JsonSerializer.Deserialize<ViewerSnapshot>("{\"adopted\":true,\"heroName\":\"Viewer\",\"gold\":50000}", options);
        Assert.Null(old!.Prestige);
        var current = old with { Prestige = new PrestigeSnapshot(3, 50, 1200, 1250, 12500000, false, "More kills required", "Reset progression",
            [new PrestigePerk("might", "Might", "+2% damage", 3, 10)]) };
        var json = System.Text.Json.JsonSerializer.Serialize(current, options);
        var roundTrip = System.Text.Json.JsonSerializer.Deserialize<ViewerSnapshot>(json, options)!;
        Assert.Equal(12500000, roundTrip.Prestige!.RequiredGold);
        Assert.Equal(3, roundTrip.Prestige.Perks[0].Rank);
        Assert.Equal("More kills required", roundTrip.Prestige.BlockingReason);
        Assert.Contains("\"prestige\"", json);
    }
    [Fact]
    public void VersionOneContainsEveryRequiredMessageKind()
    {
        string[] required = ["hello", "manifest", "state.snapshot", "state.patch", "action.request", "command.request", "action.accepted", "action.result", "action.error", "inventory.request", "inventory.snapshot", "inventory.error", "retinue.request", "retinue.snapshot", "retinue.error", "viewer.subscribe", "viewer.unsubscribe", "viewer.state", "connection.status"];
        Assert.Equal(1, ProtocolKinds.Version);
        Assert.All(required, kind => Assert.Contains(kind, ProtocolKinds.Allowed));
    }
}
