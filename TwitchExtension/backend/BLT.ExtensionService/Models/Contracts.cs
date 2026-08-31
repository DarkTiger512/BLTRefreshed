using System.Text.Json;
using System.Text.Json.Serialization;

namespace BLT.ExtensionService.Models;

public static class ProtocolKinds
{
    public const int Version = 1;
    public static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "hello", "manifest", "state.snapshot", "state.patch", "action.request", "action.accepted",
        "action.result", "action.error", "inventory.request", "inventory.snapshot", "inventory.error",
        "retinue.request", "retinue.snapshot", "retinue.error", "connection.status"
    };
}

public sealed record IntegrationUser(string Id, string Name, IReadOnlyList<string> Roles);

public sealed record IntegrationEnvelope(
    [property: JsonPropertyName("v")] int Version,
    string Id,
    string Kind,
    string ChannelId,
    DateTimeOffset Timestamp,
    IntegrationUser? User,
    JsonElement Data);

public sealed record ActionSubmission(string RequestId, string ActionId, Dictionary<string, JsonElement> Args, DateTimeOffset Timestamp);
public sealed record InventorySubmission(string RequestId, DateTimeOffset Timestamp);
public sealed record RetinueSubmission(string RequestId, DateTimeOffset Timestamp);
public sealed record PairingExchangeRequest(string Code);
public sealed record PairingExchangeResponse(string ChannelId, string InstallationId, string InstallationCredential, DateTimeOffset IssuedAt);
public sealed record PairingCodeResponse(string Code, DateTimeOffset ExpiresAt);
public sealed record CommandPreference(string ActionId, bool Enabled);
public sealed record ChannelConfiguration(IReadOnlyList<CommandPreference> Commands, DateTimeOffset UpdatedAt);

public sealed record TwitchPrincipal(
    string ChannelId,
    string UserId,
    string OpaqueUserId,
    string Role,
    bool IsLinked,
    string DisplayName)
{
    public IReadOnlyList<string> Roles => Role switch
    {
        "broadcaster" => ["viewer", "moderator", "broadcaster"],
        "moderator" => ["viewer", "moderator"],
        _ => ["viewer"]
    };
}
