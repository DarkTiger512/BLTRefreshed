using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.SaveSystem;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace BLTAdoptAHero.Behaviors
{
    public sealed class CursedArtifactBehavior : CampaignBehaviorBase
    {
        public static CursedArtifactBehavior Current => Campaign.Current?.GetCampaignBehavior<CursedArtifactBehavior>();

        private CurseRecord active;
        private List<CurseHistoryEntry> history = new();
        private HashSet<string> missionParticipants = new(StringComparer.Ordinal);
        private int? playedMapEventHash;
        private double lastTriggerDay = -100000;

        public CurseRecord Active => active?.Status is CurseLifecycle.Active or CurseLifecycle.CompletedPendingReward ? active : null;
        public IReadOnlyList<CurseHistoryEntry> History => history;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, (victim, _, _, _) =>
            {
                if (victim?.StringId == Active?.HeroId) Fail("the cursed hero died");
            });
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, () =>
            {
                missionParticipants.Clear();
                playedMapEventHash = null;
                TryGrantPendingReward();
            });
        }

        public override void SyncData(IDataStore dataStore)
        {
            using var sync = new ScopedJsonSync(dataStore, nameof(CursedArtifactBehavior));
            sync.SyncDataAsJson("ActiveV1", ref active);
            sync.SyncDataAsJson("HistoryV1", ref history);
            dataStore.SyncData("LastTriggerDay", ref lastTriggerDay);
            history ??= new List<CurseHistoryEntry>();
            if (active != null) active.ProcessedBattleIds ??= new HashSet<string>(StringComparer.Ordinal);
        }

        public bool IsCursed(Hero hero) => hero != null && Active?.Status == CurseLifecycle.Active && Active.HeroId == hero.StringId;
        public int BattleProgress(Hero hero) => IsCursed(hero) ? active.QualifyingWins : 0;
        public float OutgoingDamageMultiplier(Hero hero) => IsCursed(hero)
            ? CursedArtifactPolicy.OutgoingMultiplier(BLTAdoptAHeroModule.CommonConfig?.CursedArtifactOutgoingPenaltyPercent ?? 20f) : 1f;
        public float IncomingDamageMultiplier(Hero hero) => IsCursed(hero)
            ? CursedArtifactPolicy.IncomingMultiplier(BLTAdoptAHeroModule.CommonConfig?.CursedArtifactIncomingIncreasePercent ?? 25f) : 1f;
        public bool IsEligible(Hero hero) => hero != null && !hero.IsDead && hero.IsActive && hero.IsAdopted()
            && !string.IsNullOrWhiteSpace(BLTAdoptAHeroCampaignBehavior.Current?.GetHeroOwner(hero));

        public void MarkMissionParticipant(Hero hero)
        {
            if (!IsCursed(hero)) return;
            missionParticipants.Add(hero.StringId);
            if (hero.PartyBelongedTo?.MapEvent != null)
                playedMapEventHash = hero.PartyBelongedTo.MapEvent.GetHashCode();
        }

        private void OnDailyTick()
        {
            var cfg = BLTAdoptAHeroModule.CommonConfig;
            if (cfg?.RandomEventsEnabled != true || cfg.CursedArtifactEnabled != true) return;
            if (Active?.Status == CurseLifecycle.CompletedPendingReward) { TryGrantPendingReward(); return; }
            if (Active != null) return;

            double day = CampaignTime.Now.ToDays;
            if (day - lastTriggerDay < CursedArtifactPolicy.ClampCooldown(cfg.CursedArtifactCooldownDays)) return;
            float chance = CursedArtifactPolicy.ClampChance(cfg.CursedArtifactDailyChancePercent);
            float roll = MBRandom.RandomFloat * 100f;
            if (cfg.CursedArtifactDiagnostics) Log.Info($"[Cursed Artifact] daily roll {roll:0.00}/{chance:0.00}");
            if (roll >= chance) return;

            var eligible = BLTAdoptAHeroCampaignBehavior.GetAllAdoptedHeroes().Where(IsEligible).OrderBy(h => h.StringId).ToList();
            if (eligible.Count == 0) return;
            var hero = eligible[MBRandom.RandomInt(eligible.Count)];
            active = new CurseRecord { HeroId = hero.StringId, Owner = BLTAdoptAHeroCampaignBehavior.Current.GetHeroOwner(hero), StartedAt = CampaignTime.Now.ToString() };
            lastTriggerDay = day;
            Log.LogFeedEvent("{=BLTCurseStarted}A cursed artifact has bound itself to @{Owner}! Win {RequiredWins} campaign battles to transform it into a legendary weapon."
                .Translate(("Owner", active.Owner), ("RequiredWins", CursedArtifactPolicy.ClampRequiredWins(cfg.CursedArtifactRequiredWins))));
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (Active?.Status != CurseLifecycle.Active || mapEvent?.Winner == null || !missionParticipants.Contains(active.HeroId)
                || playedMapEventHash != mapEvent.GetHashCode()) return;
            try
            {
                if (!QualifyingType(mapEvent.EventType)) { Diagnostic("rejected non-qualifying battle type"); return; }
                Hero hero = ResolveHero();
                if (hero?.PartyBelongedTo?.MapEventSide != mapEvent.Winner) { Diagnostic("rejected loss or unavailable winning side"); return; }
                string battleId = $"{mapEvent.EventType}:{CampaignTime.Now.ToHours:F4}:{string.Join(",", mapEvent.InvolvedParties.Select(p => p.MobileParty?.StringId).Where(x => x != null).OrderBy(x => x))}";
                if (!CursedArtifactPolicy.RecordVictory(active, battleId, BLTAdoptAHeroModule.CommonConfig.CursedArtifactRequiredWins)) { Diagnostic("duplicate battle callback"); return; }
                Log.LogFeedEvent("{=BLTCurseProgress}@{Owner} won a cursed battle ({Wins}/{RequiredWins})."
                    .Translate(("Owner", active.Owner), ("Wins", active.QualifyingWins),
                        ("RequiredWins", CursedArtifactPolicy.ClampRequiredWins(BLTAdoptAHeroModule.CommonConfig.CursedArtifactRequiredWins))));
                if (active.Status == CurseLifecycle.CompletedPendingReward) TryGrantPendingReward();
            }
            finally { missionParticipants.Clear(); playedMapEventHash = null; }
        }

        private static bool QualifyingType(MapEvent.BattleTypes type) => type is MapEvent.BattleTypes.FieldBattle
            or MapEvent.BattleTypes.Siege or MapEvent.BattleTypes.Hideout or MapEvent.BattleTypes.Raid
            or MapEvent.BattleTypes.SallyOut or MapEvent.BattleTypes.SiegeOutside;

        private void TryGrantPendingReward()
        {
            if (active?.Status != CurseLifecycle.CompletedPendingReward) return;
            Hero hero = ResolveHero();
            if (hero == null || hero.IsDead) { Fail("the cursed hero is no longer available"); return; }
            try
            {
                var cfg = BLTAdoptAHeroModule.CommonConfig;
                var modifier = new RandomItemModifierDef
                {
                    Power = 1f,
                    WeaponDamage = new RangeInt(cfg.CursedArtifactWeaponBonus, cfg.CursedArtifactWeaponBonus),
                    WeaponSpeed = new RangeInt(cfg.CursedArtifactWeaponBonus, cfg.CursedArtifactWeaponBonus),
                    WeaponMissileSpeed = new RangeInt(cfg.CursedArtifactWeaponBonus, cfg.CursedArtifactWeaponBonus),
                    ThrowingStack = new RangeInt(0, 0)
                };
                var generated = RewardHelpers.GenerateRewardType(RewardHelpers.RewardType.Weapon, 6, hero, hero.GetClass(), false,
                    modifier, "Cursed Legacy", 1f);
                if (generated.item == null || generated.modifier == null) { Diagnostic("reward generation returned no compatible weapon"); return; }
                RewardHelpers.AssignCustomReward(hero, generated.item, generated.modifier, generated.slot);
                bool stored = BLTAdoptAHeroCampaignBehavior.Current.GetCustomItems(hero)
                    .Any(i => i.Item == generated.item && i.ItemModifier == generated.modifier);
                if (!stored) { Diagnostic("reward assignment was not persisted; will retry"); return; }
                active.RewardItemId = generated.item.StringId;
                active.Status = CurseLifecycle.Completed;
                active.FinishedAt = CampaignTime.Now.ToString();
                AddHistory(active, null);
                Log.LogFeedEvent("{=BLTCurseCompleted}@{Owner} broke the curse and received the legendary Cursed Legacy!"
                    .Translate(("Owner", active.Owner)));
                active = null;
            }
            catch (Exception ex) { Log.Error($"[Cursed Artifact] reward pending after failure: {ex}"); }
        }

        private Hero ResolveHero() => string.IsNullOrWhiteSpace(Active?.HeroId) ? null
            : MBObjectManager.Instance.GetObject<Hero>(Active.HeroId);

        private void Fail(string reason)
        {
            if (Active == null) return;
            active.Status = CurseLifecycle.Failed;
            active.FinishedAt = CampaignTime.Now.ToString();
            active.FailureReason = reason;
            AddHistory(active, reason);
            Log.LogFeedEvent("{=BLTCurseFailed}The cursed artifact event failed. No reward was granted.".Translate());
            active = null;
            missionParticipants.Clear();
            playedMapEventHash = null;
        }

        private void AddHistory(CurseRecord record, string reason) => history.Add(new CurseHistoryEntry
        {
            HeroId = record.HeroId, Owner = record.Owner, Status = record.Status, Wins = record.QualifyingWins,
            FinishedAt = record.FinishedAt, Reason = reason
        });

        private static void Diagnostic(string message)
        {
            if (BLTAdoptAHeroModule.CommonConfig?.CursedArtifactDiagnostics == true) Log.Info($"[Cursed Artifact] {message}");
        }
    }
}
