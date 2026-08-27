using BLTAdoptAHero.Util;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var children = new Dictionary<string, string[]>
{
    ["root"] = new[] { "infantry", "ranged" },
    ["infantry"] = new[] { "legionary" },
    ["ranged"] = new[] { "sharpshooter" },
    ["legionary"] = Array.Empty<string>(),
    ["sharpshooter"] = Array.Empty<string>()
};
var terminals = CycleSafeGraph.FindTerminals("root", node => children[node]);
Assert(terminals.SequenceEqual(new[] { "legionary", "sharpshooter" }), "Branch terminals were not preserved.");

children["cycle-a"] = new[] { "cycle-b" };
children["cycle-b"] = new[] { "cycle-a" };
var cycle = CycleSafeGraph.FindTerminals("cycle-a", node => children[node]);
Assert(cycle.Count == 1, "A cyclic troop tree must terminate deterministically.");

var troops = new[]
{
    new Troop("same-incompatible", "A", false),
    new Troop("other-compatible", "B", true),
    new Troop("same-compatible", "A", true)
};
var selected = SmartTroopPolicy.Select(troops, t => t.Culture == "A", t => t.Compatible,
    new[] { new Troop("fallback", "A", true) }, t => t.Id);
Assert(selected.Value.Id == "same-compatible" && selected.FallbackTier == 1, "Tier 1 selection failed.");

selected = SmartTroopPolicy.Select(troops.Where(t => t.Id != "same-compatible"), t => t.Culture == "A",
    t => t.Compatible, Array.Empty<Troop>(), t => t.Id);
Assert(selected.Value.Id == "other-compatible" && selected.FallbackTier == 2, "Tier 2 selection failed.");

selected = SmartTroopPolicy.Select(troops.Where(t => !t.Compatible), t => t.Culture == "A",
    t => t.Compatible, Array.Empty<Troop>(), t => t.Id);
Assert(selected.Value.Id == "same-incompatible" && selected.FallbackTier == 3, "Tier 3 selection failed.");

selected = SmartTroopPolicy.Select(Array.Empty<Troop>(), _ => false, _ => false,
    new[] { new Troop("fallback", "A", true) }, t => t.Id);
Assert(selected.Value.Id == "fallback" && selected.FallbackTier == 4, "Tier 4 selection failed.");

Console.WriteLine("Smart troop graph and fallback policy tests passed.");

internal sealed record Troop(string Id, string Culture, bool Compatible);
