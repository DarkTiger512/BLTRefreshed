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

var ammo = AmmoReport.Create(new[]
{
    new AmmoStackSnapshot { Slot = 3, Name = "Javelins", Current = 0, Maximum = 5 },
    new AmmoStackSnapshot { Slot = 1, Name = "Arrows", Current = 12, Maximum = 24 },
    new AmmoStackSnapshot { Slot = 2, Name = "Bolts", Current = 7, Maximum = 10 }
}, true);
Assert(ammo.Kind == AmmoReportKind.Available && ammo.TotalCurrent == 19 && ammo.TotalMaximum == 39,
    "Mixed ammunition totals failed.");
Assert(ammo.Details == "Arrows: 12/24, Bolts: 7/10, Javelins: 0/5",
    "Ammunition output must follow stable equipment-slot order.");

ammo = AmmoReport.Create(new[]
{
    new AmmoStackSnapshot { Slot = 0, Name = "Throwing Axes", Current = 0, Maximum = 3 }
}, false);
Assert(ammo.Kind == AmmoReportKind.Depleted && ammo.TotalCurrent == 0 && ammo.TotalMaximum == 3,
    "Depleted thrown ammunition failed.");
Assert(AmmoReport.Create(Array.Empty<AmmoStackSnapshot>(), true).Kind == AmmoReportKind.MissingAmmo,
    "Ranged weapon without ammunition failed.");
Assert(AmmoReport.Create(Array.Empty<AmmoStackSnapshot>(), false).Kind == AmmoReportKind.NoRangedWeapon,
    "No ranged equipment failed.");

Console.WriteLine("Smart troop and ammunition policy tests passed.");

internal sealed record Troop(string Id, string Culture, bool Compatible, int MaxTier);
