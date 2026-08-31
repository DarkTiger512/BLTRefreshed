using BLT.ExtensionService.Models;

namespace BLT.ExtensionService.Tests;

public sealed class ProtocolContractTests
{
    [Fact]
    public void VersionOneContainsEveryRequiredMessageKind()
    {
        string[] required = ["hello", "manifest", "state.snapshot", "state.patch", "action.request", "action.accepted", "action.result", "action.error", "inventory.request", "inventory.snapshot", "inventory.error", "connection.status"];
        Assert.Equal(1, ProtocolKinds.Version);
        Assert.All(required, kind => Assert.Contains(kind, ProtocolKinds.Allowed));
    }
}
