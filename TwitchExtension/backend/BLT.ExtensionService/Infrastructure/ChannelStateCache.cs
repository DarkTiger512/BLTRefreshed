using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using BLT.ExtensionService.Models;

namespace BLT.ExtensionService.Infrastructure;

public sealed class ChannelStateCache
{
    private sealed record Entry(JsonObject Envelope, long Revision, DateTimeOffset UpdatedAt);
    private readonly ConcurrentDictionary<string, Entry> entries = new(StringComparer.Ordinal);

    public bool TryAccept(string channel, string message, out string normalized)
    {
        normalized = string.Empty;
        JsonObject root;
        try { root = JsonNode.Parse(message)?.AsObject() ?? throw new JsonException(); }
        catch (JsonException) { return false; }
        if (root["v"]?.GetValue<int>() != ProtocolKinds.Version || root["channelId"]?.GetValue<string>() != channel) return false;
        var kind = root["kind"]?.GetValue<string>();
        if (kind is not ("state.snapshot" or "state.patch")) return false;
        var data = root["data"] as JsonObject;
        var mission = data?["mission"] as JsonObject;
        if (data is null || mission is null || !TryRevision(mission, out var revision)) return false;

        if (kind == "state.snapshot")
        {
            var snapshot = (JsonObject)root.DeepClone();
            entries[channel] = new(snapshot, revision, DateTimeOffset.UtcNow);
            normalized = snapshot.ToJsonString();
            return true;
        }

        while (entries.TryGetValue(channel, out var current))
        {
            if (revision < current.Revision) return false;
            if (revision == current.Revision && !ChangesNonMissionState(current.Envelope, data)) return false;
            var snapshot = (JsonObject)current.Envelope.DeepClone();
            var snapshotData = snapshot["data"]!.AsObject();
            foreach (var property in data) snapshotData[property.Key] = property.Value?.DeepClone();
            snapshot["id"] = root["id"]?.DeepClone();
            snapshot["timestamp"] = root["timestamp"]?.DeepClone();
            snapshot["kind"] = "state.snapshot";
            var next = new Entry(snapshot, revision, DateTimeOffset.UtcNow);
            if (!entries.TryUpdate(channel, next, current)) continue;
            normalized = snapshot.ToJsonString();
            return true;
        }
        return false;
    }

    public bool TryGet(string channel, out string snapshot)
    {
        if (entries.TryGetValue(channel, out var entry)) { snapshot = entry.Envelope.ToJsonString(); return true; }
        snapshot = string.Empty;
        return false;
    }

    public DateTimeOffset? LastStateAt(string channel) => entries.TryGetValue(channel, out var entry) ? entry.UpdatedAt : null;
    public void Clear(string channel) => entries.TryRemove(channel, out _);

    private static bool TryRevision(JsonObject mission, out long revision)
    {
        revision = 0;
        try { revision = mission["revision"]?.GetValue<long>() ?? -1; return revision >= 0; }
        catch (InvalidOperationException) { return false; }
    }

    private static bool ChangesNonMissionState(JsonObject envelope, JsonObject patchData)
    {
        var currentData = envelope["data"] as JsonObject;
        if (currentData is null) return true;
        foreach (var property in patchData)
        {
            if (property.Key == "mission") continue;
            if (!JsonNode.DeepEquals(currentData[property.Key], property.Value)) return true;
        }
        return false;
    }
}
