using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BannerlordTwitch.Integration
{
    public static class IntegrationProtocol
    {
        public const int Version = 1;
    }

    public sealed class IntegrationUser
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();
        public bool IsModerator => Roles.Contains("moderator", StringComparer.OrdinalIgnoreCase);
        public bool IsBroadcaster => Roles.Contains("broadcaster", StringComparer.OrdinalIgnoreCase);
        public bool IsSubscriber => Roles.Contains("subscriber", StringComparer.OrdinalIgnoreCase);
    }

    public sealed class IntegrationActionRequest
    {
        public Guid RequestId { get; set; }
        public string ChannelId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public IntegrationUser User { get; set; }
        public string ActionId { get; set; }
        public Dictionary<string, JsonElement> Args { get; set; } = new();
    }

    public sealed class IntegrationActionManifest
    {
        public int ProtocolVersion { get; set; }
        public List<IntegrationActionDefinition> Actions { get; set; } = new();
    }

    public sealed class IntegrationActionDefinition
    {
        public string Id { get; set; }
        public string LegacyName { get; set; }
        public string Handler { get; set; }
        public string[] Permissions { get; set; } = Array.Empty<string>();
        public List<IntegrationActionInput> Inputs { get; set; } = new();
    }

    public sealed class IntegrationActionInput
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public bool Required { get; set; }
        public List<IntegrationActionOption> Options { get; set; } = new();
        public string OptionsSource { get; set; }
        public double? Minimum { get; set; }
        public double? Maximum { get; set; }
        public string ConfirmationPolicy { get; set; }
        public string LegacyToken { get; set; }
        public string VisibleWhenInput { get; set; }
        public string[] VisibleWhenValues { get; set; } = Array.Empty<string>();
    }

    public sealed class IntegrationActionOption
    {
        public string Value { get; set; }
    }

    internal sealed class PairingExchangeResponse
    {
        [JsonPropertyName("channelId")] public string ChannelId { get; set; }
        [JsonPropertyName("installationId")] public string InstallationId { get; set; }
        [JsonPropertyName("installationCredential")] public string InstallationCredential { get; set; }
    }
}
