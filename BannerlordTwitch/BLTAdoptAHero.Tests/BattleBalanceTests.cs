using BLTAdoptAHero.Util;

internal static class BattleBalanceTests
{
    public static void Run()
    {
        static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        static bool Near(double x, double y) => Math.Abs(x - y) < 1e-12;
        var config = new BattleBalanceSettings();
        Check(config.IsValid(), "Defaults valid");
        foreach (var (summon, attack, sb, ab) in new[] { (0,0,0d,0d), (1,0,0d,.05), (3,1,0d,.10), (8,2,0d,.12), (10,0,0d,.20), (2,8,.12,0d) })
        {
            Check(Near(BattleBalancePolicy.Offer(config, summon, attack), sb), "Example summon offer");
            Check(Near(BattleBalancePolicy.Offer(config, attack, summon), ab), "Mirrored attack offer");
        }
        for (int a = 0; a <= 100; a++) for (int b = 0; b <= 100; b++)
        {
            var offer = BattleBalancePolicy.Offer(config,a,b);
            Check(offer >= 0 && offer <= .20, "Default cap");
            Check(a < b || offer == 0, "No majority or tied bonus");
        }
        foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -.1 })
        {
            config.MaximumBonus = invalid;
            Check(!config.IsValid() && BattleBalancePolicy.Offer(config,0,10) == 0, "Invalid maximum neutral");
        }
        config.MaximumBonus = .2;
        foreach (int invalid in new[] {0,-1}) { config.DenominatorFloor = invalid; Check(!config.IsValid(), "Positive denominator required"); }
        config.DenominatorFloor = 4;
        config.Enabled = false; Check(BattleBalancePolicy.Offer(config,0,10) == 0,"Disabled neutral"); config.Enabled = true;
        var ledger = new BattleBalanceLedger();
        ledger.RegisterAutomatic("Auto",true); ledger.RegisterAutomatic("AUTO",true);
        Check(ledger.Summoners == 1 && ledger.Find("auto").Bonus == 0,"Automatic unique owner counts without bonus");
        using (var failed = ledger.Begin("Viewer",false))
        {
            ledger.RegisterAutomatic("Viewer",false);
            Check(ledger.Find("viewer") == null && ledger.Attackers == 0,"Voluntary callback cannot register automatic");
            Check(ledger.Begin("VIEWER",false) == null,"Duplicate pending request rejected");
        }
        Check(ledger.Find("viewer") == null,"Failed join leaves no entry");
        using (var joined = ledger.Begin("Viewer",false)) Check(Near(joined.Commit(config).Bonus,.05),"Success commits offer");
        Check(ledger.Offer(config,true) == 0 && ledger.Offer(config,false) == 0,"Successful join updates next offer");
        ledger.RegisterAutomatic("Viewer",false);
        for (int i = 0; i < 10; i++) ledger.RegisterAutomatic("Other"+i,false);
        Check(Near(ledger.Find("viewer").Bonus,.05),"Equalizing and reversing sides never changes locked bonus");
        using (var resummon = ledger.Begin("VIEWER",false)) Check(Near(resummon.Commit(config).Bonus,.05),"Resummon/replacement uses owner lock");
        Check(ledger.Begin("viewer",true) == null,"Replacement cannot switch sides");
        using (var autoResummon = ledger.Begin("Auto",true)) Check(autoResummon.Commit(config).Bonus == 0,"Automatic entrants cannot later earn a bonus");
        Check(PrestigePolicy.ScalePositive(101,1.12,1.2,1.5) == 203,"Compose difficulty, balance and prestige before rounding");
        Check(PrestigePolicy.ScalePositive(-101,1.2,1.2) == -101 && PrestigePolicy.ScalePositive(0,1.2) == 0,"Only positive rewards");
        Check(PrestigePolicy.ScalePositive(100,1.2,1.2)+100 == 244,"Refund added outside multiplier");
        ledger.ReconcileOwner("Viewer", "DisplayName");
        Check(ledger.Find("Viewer") == null && Near(ledger.Find("DisplayName").Bonus,.05), "Ownership reconciliation preserves locked reward");
        Check(ledger.Begin("DisplayName",true) == null, "Reconciled owner still cannot switch sides");
        using (var pending = ledger.Begin("NumericId",true))
        {
            ledger.ReconcileOwner("NumericId","NewName");
            pending.Commit(config);
            Check(ledger.Find("NewName") != null && ledger.Find("NumericId") == null,"Pending reconciliation preserves unique identity");
        }
        var stale = ledger.Begin("Stale",true);
        ledger.Clear();
        using (var fresh = ledger.Begin("Stale",true))
        {
            stale.Dispose();
            fresh.Commit(config);
        }
        ledger.Clear(); Check(ledger.Summoners == 0 && ledger.Attackers == 0 && ledger.Find("Viewer") == null,"Mission cleanup");
        Console.WriteLine("Battle balance policy and lifecycle tests passed.");
    }
}
