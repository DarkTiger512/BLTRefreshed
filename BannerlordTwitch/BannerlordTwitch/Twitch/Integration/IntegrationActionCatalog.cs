using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BannerlordTwitch.Util;

namespace BannerlordTwitch.Integration
{
    public sealed class IntegrationActionCatalog
    {
        private readonly Dictionary<string, IntegrationActionDefinition> actions;
        public string ManifestJson { get; }

        private IntegrationActionCatalog(string json, IntegrationActionManifest manifest)
        {
            ManifestJson = json;
            actions = manifest.Actions.ToDictionary(action => action.Id, StringComparer.OrdinalIgnoreCase);
        }

        public static IntegrationActionCatalog Load()
        {
            string path = Path.Combine(Path.GetDirectoryName(typeof(IntegrationActionCatalog).Assembly.Location) ?? ".", "..", "..", "TwitchIntegration", "action-manifest.json");
            string json = File.ReadAllText(path);
            var manifest = JsonSerializer.Deserialize<IntegrationActionManifest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (manifest == null || manifest.ProtocolVersion != IntegrationProtocol.Version)
                throw new InvalidDataException($"Unsupported Twitch integration manifest at {path}");
            return new IntegrationActionCatalog(json, manifest);
        }

        public bool TryGet(string actionId, out IntegrationActionDefinition action) => actions.TryGetValue(actionId, out action);

        public string BuildLegacyArguments(IntegrationActionDefinition action, IReadOnlyDictionary<string, JsonElement> values)
        {
            if (string.Equals(action.Id, "command.eliteretinue", StringComparison.OrdinalIgnoreCase))
                return BuildEliteRetinueArguments(action, values);
            var result = new List<string>();
            if (values.Keys.Any(key => action.Inputs.All(input => !string.Equals(input.Id, key, StringComparison.OrdinalIgnoreCase))))
                throw new ArgumentException("The request contains an unknown argument");
            foreach (var input in action.Inputs)
            {
                if (!values.TryGetValue(input.Id, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                {
                    if (input.Required) throw new ArgumentException($"{input.Id} is required");
                    continue;
                }
                if (input.Type == "confirmation")
                {
                    if (input.Required && value.ValueKind != JsonValueKind.True) throw new ArgumentException($"{input.Id} must be confirmed");
                    continue;
                }
                if (input.Type == "choice" && !input.Options.Any(option =>
                    string.Equals(option.Value, value.GetString(), StringComparison.OrdinalIgnoreCase)))
                    throw new ArgumentException($"{input.Id} is not a valid choice");
                if (input.Type == "number")
                {
                    if (!value.TryGetDouble(out var number)) throw new ArgumentException($"{input.Id} must be a number");
                    if (input.Minimum.HasValue && number < input.Minimum.Value) throw new ArgumentException($"{input.Id} is below the minimum");
                    if (input.Maximum.HasValue && number > input.Maximum.Value) throw new ArgumentException($"{input.Id} is above the maximum");
                }
                string text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
                if (string.IsNullOrWhiteSpace(text))
                {
                    if (input.Required) throw new ArgumentException($"{input.Id} is required");
                    continue;
                }
                if (text.Any(char.IsControl) || text.Contains('\n') || text.Contains('\r')) throw new ArgumentException($"{input.Id} contains invalid characters");
                result.Add(text.Trim());
            }
            return string.Join(" ", result);
        }

        private static string BuildEliteRetinueArguments(IntegrationActionDefinition action, IReadOnlyDictionary<string, JsonElement> values)
        {
            if (values.Keys.Any(key => action.Inputs.All(input => !string.Equals(input.Id, key, StringComparison.OrdinalIgnoreCase))))
                throw new ArgumentException("The request contains an unknown argument");
            if (!values.TryGetValue("operation", out var operationValue) || operationValue.ValueKind != JsonValueKind.String)
                throw new ArgumentException("operation is required");
            var operation = operationValue.GetString();
            return operation switch
            {
                "upgrade-one" => "1",
                "upgrade-all" => "all",
                "clear-all" => "clear all",
                "clear-slot" when values.TryGetValue("slot", out var slot) && slot.TryGetInt32(out var index) && index > 0 => $"clear {index}",
                "clear-slot" => throw new ArgumentException("A positive slot number is required when dismissing one troop"),
                _ => throw new ArgumentException("operation is not a valid elite retinue action")
            };
        }
    }
}
