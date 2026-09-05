using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly HashSet<Agent> failedSpawns = new();
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
            if (!BalanceBattle || failedSpawns.Contains(agent) || agent?.IsHuman != true || agent.Team == null || Mission.PlayerTeam == null) return;
            var hero = agent.GetAdoptedHero();
            if (hero == null || voluntarySpawns.Contains(hero)) return;
            if (!agent.Team.IsFriendOf(Mission.PlayerTeam) && !agent.Team.IsEnemyOf(Mission.PlayerTeam)) return;
            string owner = BalanceOwner(hero);
            if (balance.Find(owner) != null) return;
            balance.RegisterAutomatic(owner, agent.Team.IsFriendOf(Mission.PlayerTeam));
            PublishBalance();
        }
        public void ReconcileBalance()
        {
            if (!BalanceBattle) return;
            foreach (var agent in Mission.Agents) RegisterBalanceAgent(agent);
        }
        partial void PublishBalance();
        public void ReconcileBalanceOwner(string previous, string current)
        {
            balance.ReconcileOwner(previous, current);
            foreach (var hero in balanceOwners.Where(x => string.Equals(x.Value, previous, StringComparison.OrdinalIgnoreCase)).Select(x => x.Key).ToArray())
                balanceOwners[hero] = current;
            PublishBalance();
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
            private readonly Agent previousAgent;
            private readonly HeroSummonState previousState;
            private readonly (Agent agent, AgentState state, int count, float time) previousSpawn;
            public bool Succeeded { get; private set; }
            public Action OnFailure { get; set; }
            internal BalanceJoin(BLTSummonBehavior behavior, Hero hero, BattleBalanceLedger.Join join)
            {
                this.behavior = behavior; this.hero = hero; this.join = join;
                previousAgent = hero.GetAgent();
                previousState = behavior.GetHeroSummonState(hero);
                if (previousState != null) previousSpawn = (previousState.CurrentAgent, previousState.State, previousState.TimesSummoned, previousState.SummonTime);
            }
            public void Commit()
            {
                behavior.ReconcileBalance();
                bool firstParticipation = behavior.balance.Find(behavior.BalanceOwner(hero)) == null;
                var participation = join.Commit(BalanceConfig);
                Succeeded = true;
                if (firstParticipation)
                    BLTAdoptAHeroCampaignBehavior.Current.IncreaseParticipationCount(hero, participation.PlayerSide, forced: false);
                behavior.PublishBalance();
            }
            public void Dispose()
            {
                join.Dispose();
                behavior.voluntarySpawns.Remove(hero);
                if (!Succeeded)
                {
                    // If the engine spawned before a later setup failure, remove only agents
                    // created by this attempt. Never remove a hero rejected as already present.
                    var spawned = hero.GetAgent();
                    if (spawned != null && spawned != previousAgent)
                    { behavior.failedSpawns.Add(spawned); spawned.FadeOut(true, true); }
                    if (previousState == null && behavior.GetHeroSummonState(hero) is HeroSummonState created)
                    {
                        foreach (var retinue in created.Retinue.Concat(created.Retinue2))
                        {
                            if (retinue.Agent?.IsActive() == true) retinue.Agent.FadeOut(true, true);
                            created.Party?.MemberRoster?.AddToCounts(retinue.Troop, -1);
                        }
                        behavior.heroSummonStates.Remove(created);
                    }
                    else if (previousState != null)
                    {
                        previousState.CurrentAgent = previousSpawn.agent; previousState.State = previousSpawn.state;
                        previousState.TimesSummoned = previousSpawn.count; previousState.SummonTime = previousSpawn.time;
                    }
                    OnFailure?.Invoke();
                }
            }
        }
        // Called after custom mission result rewards have consumed the locked bonuses.
        public void ClearBalance() { balance.Clear(); balanceOwners.Clear(); voluntarySpawns.Clear(); failedSpawns.Clear(); }
    }
}
