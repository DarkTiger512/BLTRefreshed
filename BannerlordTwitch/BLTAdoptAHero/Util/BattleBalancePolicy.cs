using System;
using System.Collections.Generic;
using System.Linq;

namespace BLTAdoptAHero.Util
{
    public class BattleBalanceSettings
    {
        public bool Enabled { get; set; } = true;
        public double MaximumBonus { get; set; } = .20;
        public int DenominatorFloor { get; set; } = 4;
        public bool IsValid() => !double.IsNaN(MaximumBonus) && !double.IsInfinity(MaximumBonus)
            && MaximumBonus >= 0 && DenominatorFloor > 0;
    }

    public static class BattleBalancePolicy
    {
        public static double Offer(BattleBalanceSettings settings, int own, int other)
        {
            if (settings == null || !settings.Enabled || !settings.IsValid() || own < 0 || other <= own) return 0;
            return settings.MaximumBonus * (((double)other - own) / Math.Max((double)own + other, settings.DenominatorFloor));
        }
    }

    // One instance per battle mission. All operations execute on the game thread.
    public sealed class BattleBalanceLedger
    {
        public sealed class Participation
        {
            public bool PlayerSide { get; }
            public double Bonus { get; }
            public Participation(bool playerSide, double bonus) { PlayerSide = playerSide; Bonus = bonus; }
        }
        private readonly Dictionary<string, Participation> entries = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Join> pending = new(StringComparer.OrdinalIgnoreCase);
        public int Summoners => entries.Values.Count(x => x.PlayerSide);
        public int Attackers => entries.Count - Summoners;
        public Participation Find(string owner) => owner != null && entries.TryGetValue(owner, out var entry) ? entry : null;
        public double Offer(BattleBalanceSettings settings, bool playerSide) => BattleBalancePolicy.Offer(settings,
            playerSide ? Summoners : Attackers, playerSide ? Attackers : Summoners);
        public void RegisterAutomatic(string owner, bool playerSide)
        {
            if (!string.IsNullOrWhiteSpace(owner) && !pending.ContainsKey(owner) && !entries.ContainsKey(owner))
                entries.Add(owner, new Participation(playerSide, 0));
        }
        public Join Begin(string owner, bool playerSide)
        {
            if (string.IsNullOrWhiteSpace(owner) || pending.ContainsKey(owner)
                || (Find(owner) is Participation prior && prior.PlayerSide != playerSide)) return null;
            var join = new Join(this, owner, playerSide);
            pending.Add(owner, join);
            return join;
        }
        public void ReconcileOwner(string previous, string current)
        {
            if (string.IsNullOrWhiteSpace(previous) || string.IsNullOrWhiteSpace(current)
                || string.Equals(previous, current, StringComparison.OrdinalIgnoreCase)) return;
            if (entries.TryGetValue(previous, out var entry))
            {
                if (!entries.ContainsKey(current)) entries[current] = entry;
                entries.Remove(previous);
            }
            if (pending.TryGetValue(previous, out var join))
            {
                pending.Remove(previous);
                if (!pending.ContainsKey(current)) { join.Rename(current); pending[current] = join; }
            }
        }
        public void Clear() { entries.Clear(); pending.Clear(); }
        public sealed class Join : IDisposable
        {
            private BattleBalanceLedger ledger;
            private string owner;
            private readonly bool playerSide;
            internal Join(BattleBalanceLedger ledger, string owner, bool playerSide)
            { this.ledger = ledger; this.owner = owner; this.playerSide = playerSide; }
            internal void Rename(string current) { owner = current; }
            public Participation Commit(BattleBalanceSettings settings)
            {
                if (ledger == null || (!ledger.pending.TryGetValue(owner, out var active) || active != this)) throw new InvalidOperationException("Join is no longer pending.");
                var result = ledger.Find(owner);
                if (result == null)
                {
                    result = new Participation(playerSide, ledger.Offer(settings, playerSide));
                    ledger.entries.Add(owner, result);
                }
                Dispose();
                return result;
            }
            public void Dispose()
            {
                if (ledger != null && ledger.pending.TryGetValue(owner, out var active) && active == this) ledger.pending.Remove(owner);
                ledger = null;
            }
        }
    }
}
