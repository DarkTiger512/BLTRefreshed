using System;
using System.Collections.Generic;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    internal partial class BLTSummonBehavior
    {
        private readonly BattleBalanceLedger balance = new();
        private readonly Dictionary<Hero, string> balanceOwners = new();
        private readonly HashSet<Hero> voluntarySpawns = new();
        public static BattleBalanceSettings BalanceConfig => BLTAdoptAHeroModule.CommonConfig.BattleBalance;
        public static bool BalanceBattle => Mission.Current != null && Campaign.Current != null
            && !MissionHelpers.InTournament() && !MissionHelpers.InArenaPracticeMission()
            && !MissionHelpers.InTrainingFieldMission() && !MissionHelpers.InFriendlyMission()
            && (Mission.Current.Mode == MissionMode.Battle || Mission.Current.Mode == MissionMode.Deployment);
        private string BalanceOwner(Hero hero)
        {
            if (hero == null) return null;
            // Retain the identity for rewards after death/replacement; never parse displayed names.
            if (!balanceOwners.TryGetValue(hero, out var owner))
            {
                owner = BLTAdoptAHeroCampaignBehavior.Current?.GetHeroOwner(hero);
                if (!string.IsNullOrEmpty(owner)) balanceOwners[hero] = owner;
            }
            return owner;
        }
        private void RegisterBalanceAgent(Agent agent)
        {
            if (!BalanceBattle || agent?.IsHuman != true || agent.Team == null || Mission.PlayerTeam == null) return;
            var hero = agent.GetAdoptedHero();
            if (hero == null || voluntarySpawns.Contains(hero)) return;
            if (!agent.Team.IsFriendOf(Mission.PlayerTeam) && !agent.Team.IsEnemyOf(Mission.PlayerTeam)) return;
            balance.RegisterAutomatic(BalanceOwner(hero), agent.Team.IsFriendOf(Mission.PlayerTeam));
        }
        public void ReconcileBalance()
        {
            if (!BalanceBattle) return;
            foreach (var agent in Mission.Agents) RegisterBalanceAgent(agent);
        }
        public int BalanceSummoners => balance.Summoners;
        public int BalanceAttackers => balance.Attackers;
        public double BalanceOffer(bool playerSide) => balance.Offer(BalanceConfig, playerSide);
        public double? LockedBalanceBonus(Hero hero) => balance.Find(BalanceOwner(hero))?.Bonus;
        public static double BalanceFactor(Hero hero) => 1 + (Current?.LockedBalanceBonus(hero) ?? 0);
        public string BalanceSummary()
        {
            ReconcileBalance();
            return "{=BLTBalanceOffers}Summon {SUMMON} / Attack {ATTACK} — next join offers: Summon +{SB}%, Attack +{AB}% battle gold and XP. Final bonus is set when the join succeeds."
                .Translate(("SUMMON", BalanceSummoners), ("ATTACK", BalanceAttackers),
                    ("SB", (100 * BalanceOffer(true)).ToString("0.#")), ("AB", (100 * BalanceOffer(false)).ToString("0.#")));
        }
        public string LockedBalanceSummary(Hero hero) => LockedBalanceBonus(hero) is double bonus
            ? "{=BLTBalanceLocked}Your locked joining bonus: +{BONUS}% battle gold and XP until this mission ends."
                .Translate(("BONUS", (bonus * 100).ToString("0.#"))) : "";
        public BalanceJoin BeginBalanceJoin(Hero hero, bool playerSide)
        {
            ReconcileBalance();
            var join = balance.Begin(BalanceOwner(hero), playerSide);
            if (join == null) return null;
            voluntarySpawns.Add(hero);
            return new BalanceJoin(this, hero, join);
        }
        public sealed class BalanceJoin : IDisposable
        {
            private readonly BLTSummonBehavior behavior;
            private readonly Hero hero;
            private readonly BattleBalanceLedger.Join join;
            public bool Succeeded { get; private set; }
            public Action OnFailure { get; set; }
            internal BalanceJoin(BLTSummonBehavior behavior, Hero hero, BattleBalanceLedger.Join join)
            { this.behavior = behavior; this.hero = hero; this.join = join; }
            public void Commit()
            {
                behavior.ReconcileBalance();
                join.Commit(BalanceConfig);
                Succeeded = true;
            }
            public void Dispose()
            {
                join.Dispose();
                behavior.voluntarySpawns.Remove(hero);
                if (!Succeeded) OnFailure?.Invoke();
                if (!Succeeded && behavior.GetHeroSummonState(hero) is HeroSummonState state && state.TimesSummoned == 0)
                    behavior.heroSummonStates.Remove(state);
            }
        }
        // Called after custom mission result rewards have consumed the locked bonuses.
        public void ClearBalance() { balance.Clear(); balanceOwners.Clear(); voluntarySpawns.Clear(); }
    }
}
