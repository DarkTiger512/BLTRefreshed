using BLTAdoptAHero.Util;

internal static class PrestigeTests
{
    public static void Run()
    {
        static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        var settings = new PrestigeSettings();
        var progress = new PrestigeProgress();
        Check(settings.IsValid(), "Default configuration must be valid.");
        Check(progress.Count == 0 && progress.Rank("might") == 0, "Missing save data starts without prestige.");
        Check(PrestigePolicy.RequiredKills(settings, 0) == 500 && PrestigePolicy.RequiredGold(settings, 0) == 5000000, "First prestige requirements.");
        Check(PrestigePolicy.RequiredKills(settings, 49) == 12750 && PrestigePolicy.RequiredGold(settings, 49) == 127500000, "Final prestige requirements.");
        Check(PrestigePolicy.RequiredGold(settings, int.MaxValue) > int.MaxValue, "Requirement arithmetic must not overflow.");
        Check(!PrestigePolicy.CanChoose(settings, progress, "unknown"), "Unknown perk rejected.");
        foreach (string perk in PrestigePolicy.Perks)
        {
            for (int rank = 0; rank < 10; rank++)
            {
                Check(PrestigePolicy.CanChoose(settings, progress, perk), "Available perk must be selectable.");
                progress.Ranks[perk] = rank + 1;
                progress.Count++;
            }
            Check(!PrestigePolicy.CanChoose(settings, progress, perk), "Capped perk rejected.");
        }
        Check(progress.Count == 50 && PrestigePolicy.Perks.All(p => !PrestigePolicy.CanChoose(settings, progress, p)), "Stop after 50 prestiges.");
        Check(PrestigePolicy.ScalePositive(1000, 1.1) == 1100, "Multipliers preserve exact percentages before rounding.");
        Check(PrestigePolicy.ScalePositive(1000, 1.1, 1.2, 1.5) == 1980, "Different modifiers multiply.");
        Check(PrestigePolicy.ScalePositive(1, 1.1, 1.2) == 1, "Round once at the end.");
        Check(PrestigePolicy.ScalePositive(-100, 1.1, 1.2) == -100 && PrestigePolicy.ScalePositive(0, 1.1) == 0, "Never boost penalties or zero rewards.");
        Check(PrestigePolicy.ScalePositive(int.MaxValue, 2) == int.MaxValue, "Reward arithmetic saturates.");
        Check(PrestigePolicy.ScalePositive(100, 1.1) + 100 == 210, "Refund is added after reward scaling.");
        for (int mask = 0; mask < 32; mask++)
            Check(PrestigePolicy.QualifyingKill((mask & 1) != 0, (mask & 2) != 0, (mask & 4) != 0, (mask & 8) != 0, (mask & 16) != 0) == (mask == 31), "All kill qualifications required.");
        var confirmation = new PrestigeConfirmation();
        var now = new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc);
        Check(!confirmation.Consume("might", 0, now), "No implicit preview.");
        confirmation.Preview("might", 0, now);
        Check(confirmation.Consume("might", 0, now.AddSeconds(59)), "Live matching preview accepted.");
        Check(!confirmation.Consume("might", 0, now.AddSeconds(59)), "Duplicate confirmation rejected.");
        confirmation.Preview("might", 0, now);
        Check(!confirmation.Consume("might", 0, now.AddSeconds(60)), "Expires exactly at 60 seconds.");
        confirmation.Preview("might", 0, now);
        Check(!confirmation.Consume("fortune", 0, now), "Perk must match preview.");
        confirmation.Preview("might", 0, now);
        Check(!confirmation.Consume("might", 1, now), "Changed prestige invalidates preview.");
        settings.ResiliencePerRank = .1;
        Check(!settings.IsValid(), "Do not allow damage immunity through invalid config.");
        Console.WriteLine("Prestige policy tests passed.");
    }
}
