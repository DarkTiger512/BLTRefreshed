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
        "retinue.request", "retinue.snapshot", "retinue.error", "command.request", "viewer.subscribe",
        "viewer.unsubscribe", "viewer.state", "connection.status", "configuration.updated"
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
public sealed record CommandSubmission(string RequestId, string CommandLine, DateTimeOffset Timestamp);
public sealed record InventorySubmission(string RequestId, DateTimeOffset Timestamp);
public sealed record RetinueSubmission(string RequestId, DateTimeOffset Timestamp);
public sealed record PairingExchangeRequest(string Code);
public sealed record PairingExchangeResponse(string ChannelId, string InstallationId, string InstallationCredential, DateTimeOffset IssuedAt);
public sealed record PairingCodeResponse(string Code, DateTimeOffset ExpiresAt);
public sealed record PairingRequestSubmission(string Code, string ModVersion, string PlatformLabel, string Fingerprint);
public sealed record PairingRequestReceipt(Guid RequestId, string RequestToken, string CandidateCredential, DateTimeOffset ExpiresAt, string Status);
public sealed record PairingRequestStatus(Guid RequestId, string Status, string? ChannelId, string? InstallationId, DateTimeOffset ExpiresAt);
public sealed record PairingRequestSummary(Guid RequestId, string ModVersion, string PlatformLabel, string Fingerprint, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, string Status);
public sealed record PairingDecision(Guid RequestId, string Decision);
public sealed record ConfigurationApplyRequest(ChannelConfiguration Configuration, IReadOnlyList<PairingDecision> PairingDecisions);
public sealed record CommandPreference(string ActionId, bool Enabled, Dictionary<string, JsonElement>? Settings = null);
public sealed record ConfigurationProfile(int ProfileId, bool ExtensionEnabled, IReadOnlyList<CommandPreference> Commands);
public sealed record ChannelConfiguration(int SchemaVersion, bool ExtensionEnabled, IReadOnlyList<CommandPreference> Commands, long Revision, DateTimeOffset UpdatedAt, IReadOnlyList<ConfigurationProfile>? Profiles = null, int ActiveProfile = 1);
public sealed record InstallationSummary(Guid InstallationId, DateTimeOffset CreatedAt, DateTimeOffset? LastSeenAt, DateTimeOffset? RevokedAt);

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
