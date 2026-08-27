using BLTAdoptAHero.Util;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var children = new Dictionary<string, string[]>
{
    ["root"] = new[] { "infantry", "ranged", "dead-end" },
    ["infantry"] = new[] { "legionary" },
    ["ranged"] = new[] { "sharpshooter" },
    ["dead-end"] = Array.Empty<string>(),
    ["legionary"] = Array.Empty<string>(),
    ["sharpshooter"] = Array.Empty<string>()
};
var terminals = CycleSafeGraph.FindTerminals("root", node => children[node]);
Assert(terminals.SequenceEqual(new[] { "legionary", "sharpshooter", "dead-end" }), "Branch/dead-end terminals failed.");

children["cycle-a"] = new[] { "cycle-b" };
children["cycle-b"] = new[] { "cycle-a" };
Assert(CycleSafeGraph.FindTerminals("cycle-a", node => children[node]).Count == 0,
    "A closed cycle must not masquerade as a terminal.");

var troops = new[]
{
    new Troop("same-incompatible", "A", false, 2),
    new Troop("other-compatible", "B", true, 6),
    new Troop("same-compatible-low", "A", true, 4),
    new Troop("same-compatible-high-z", "A", true, 6),
    new Troop("same-compatible-high-a", "A", true, 6)
};
var selected = SmartTroopPolicy.Select(troops, t => t.Culture == "A", t => t.Compatible,
    new[] { new Troop("fallback", "A", true, 1) }, t => t.Id, t => t.MaxTier);
Assert(selected.Value.Id == "same-compatible-high-a" && selected.FallbackTier == 1 && selected.Score == 6,
    "Tier 1 scoring/stable tie-break failed.");

selected = SmartTroopPolicy.Select(troops.Where(t => t.Culture != "A" || !t.Compatible),
    t => t.Culture == "A", t => t.Compatible, Array.Empty<Troop>(), t => t.Id, t => t.MaxTier);
Assert(selected.Value.Id == "other-compatible" && selected.FallbackTier == 2, "Tier 2 failed.");

selected = SmartTroopPolicy.Select(troops.Where(t => !t.Compatible), t => t.Culture == "A",
    t => t.Compatible, Array.Empty<Troop>(), t => t.Id, t => t.MaxTier);
Assert(selected.Value.Id == "same-incompatible" && selected.FallbackTier == 3, "Tier 3 failed.");

selected = SmartTroopPolicy.Select(Array.Empty<Troop>(), _ => false, _ => false,
    new[] { new Troop("fallback", "A", true, 1) }, t => t.Id, t => t.MaxTier);
Assert(selected.Value.Id == "fallback" && selected.FallbackTier == 4, "Tier 4 failed.");

var branch = SmartTroopPolicy.SelectCompatible(troops, t => t.Compatible, t => t.MaxTier, t => t.Id);
Assert(branch.Value.Id == "other-compatible", "Compatible branch score/tie-break failed.");
Assert(branch.Rejections.Any(r => r.Contains("same-incompatible")), "Rejected branches were not diagnosed.");

var replacement = SmartTroopPolicy.SelectClosestTier(troops, 5, t => t.Culture == "A", t => t.Compatible,
    t => t.MaxTier, t => t.MaxTier, t => t.Id);
Assert(replacement.Value.Id == "same-compatible-high-a", "Closest-tier culture/score preference failed.");
var missing = SmartTroopPolicy.SelectClosestTier(troops, 5, _ => true, _ => false,
    t => t.MaxTier, t => t.MaxTier, t => t.Id);
Assert(missing.Value == null && missing.Rejections.Count == 1, "Missing replacement failed.");

Assert(SmartTroopPolicy.InterpretRole("Horse Archer", true) == SmartTroopPolicy.SmartTroopRole.HorseArcher,
    "Explicit horse archer must override mounted cavalry.");
Assert(SmartTroopPolicy.InterpretRole("Ranged", true) == SmartTroopPolicy.SmartTroopRole.Cavalry,
    "Mounted must override a non-horse-archer formation.");
Assert(SmartTroopPolicy.InterpretRole("Ranged", false) == SmartTroopPolicy.SmartTroopRole.FootRanged,
    "Foot ranged role failed.");
Assert(SmartTroopPolicy.InterpretRole("Skirmisher", false) == SmartTroopPolicy.SmartTroopRole.InfantryFamily,
    "Skirmisher infantry-family role failed.");
Assert(SmartTroopPolicy.InterpretRole("CustomRole", false) == SmartTroopPolicy.SmartTroopRole.Unknown,
    "Unknown role fallback failed.");

Assert(!SmartTroopPolicy.CanAfford(100, 75, 50), "Insufficient gold should block mutation.");
Assert(SmartTroopPolicy.CanAfford(125, 75, 50), "Exact gold should be affordable.");

Console.WriteLine("Smart troop graph, role, scoring, fallback, replacement and affordability tests passed.");

internal sealed record Troop(string Id, string Culture, bool Compatible, int MaxTier);
