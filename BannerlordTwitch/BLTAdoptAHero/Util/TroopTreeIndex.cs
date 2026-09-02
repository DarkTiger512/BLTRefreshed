using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace BLTAdoptAHero.Util
{
    /// <summary>Cycle-safe index and the single source of truth for smart-retinue selection.</summary>
    public static class TroopTreeIndex
    {
        public sealed class TroopInfo
        {
            public CharacterObject Troop { get; internal set; }
            public IReadOnlyList<CharacterObject> TerminalDestinations { get; internal set; } = Array.Empty<CharacterObject>();
            public IReadOnlyCollection<FormationClass> ReachableFormations { get; internal set; } = Array.Empty<FormationClass>();
            public int MaximumReachableTier { get; internal set; }
            public bool CanReach(HeroClassDef heroClass) => CompatibleTerminals(heroClass).Any();
            public IEnumerable<CharacterObject> CompatibleTerminals(HeroClassDef heroClass) =>
                TerminalDestinations.Where(t => IsCompatible(t, InterpretRole(heroClass)));
        }

        public sealed class SelectionResult
        {
            public SmartTroopPolicy.SmartTroopRole InterpretedRole { get; internal set; }
            public CharacterObject SelectedTroop { get; internal set; }
            public int FallbackTier { get; internal set; }
            public int Score { get; internal set; }
            public IReadOnlyList<string> RejectionReasons { get; internal set; } = Array.Empty<string>();
        }

        private static readonly Dictionary<CharacterObject, TroopInfo> Index = new();
        private static bool isBuilt;

        public static void BuildIndex()
        {
            SaveCrashDiagnostics.Mark("TroopTreeIndex.BuildIndex begin");
            Index.Clear();
            foreach (var troop in CharacterObject.All.Where(t => t != null && !t.IsHero))
            {
                var terminals = CycleSafeGraph.FindTerminals(troop,
                    t => t.UpgradeTargets ?? Array.Empty<CharacterObject>());
                Index[troop] = new TroopInfo
                {
                    Troop = troop,
                    TerminalDestinations = terminals,
                    ReachableFormations = terminals.Select(t => t.DefaultFormationClass).Distinct().ToList(),
                    MaximumReachableTier = terminals.Any() ? terminals.Max(t => t.Tier) : troop.Tier
                };
            }
            isBuilt = true;
            SaveCrashDiagnostics.Mark($"TroopTreeIndex.BuildIndex end count={Index.Count}");
            Log.Info($"[TroopTreeIndex] Indexed {Index.Count} loaded troops");
        }

        public static void Clear()
        {
            Index.Clear();
            isBuilt = false;
            SaveCrashDiagnostics.Mark("TroopTreeIndex.Clear");
        }

        public static TroopInfo GetTroopInfo(CharacterObject troop)
        {
            EnsureBuilt();
            return troop != null && Index.TryGetValue(troop, out var info) ? info : null;
        }

        public static bool CanReachHeroClass(CharacterObject troop, HeroClassDef heroClass) =>
            GetTroopInfo(troop)?.CanReach(heroClass) == true;

        public static SelectionResult SelectHire(Hero hero, HeroClassDef heroClass,
            IEnumerable<CharacterObject> candidates, IEnumerable<CharacterObject> safeFallback)
        {
            var role = InterpretRole(heroClass);
            WarnUnknown(heroClass, role);
            var selection = SmartTroopPolicy.Select(candidates,
                t => t.Culture == hero?.Culture,
                t => IsCompatiblePath(t, role),
                safeFallback.Where(t => IsCompatiblePath(t, SmartTroopPolicy.SmartTroopRole.InfantryFamily)),
                t => t.StringId,
                t => CompatibleScore(t, role));
            return ToResult(role, selection);
        }

        public static SelectionResult SelectCompatibleUpgrade(CharacterObject troop, HeroClassDef heroClass)
        {
            var role = InterpretRole(heroClass);
            WarnUnknown(heroClass, role);
            var selection = SmartTroopPolicy.SelectCompatible(troop?.UpgradeTargets,
                t => IsCompatiblePath(t, role),
                t => CompatibleScore(t, role),
                t => t.StringId);
            return ToResult(role, selection);
        }

        public static SelectionResult SelectClosestReplacement(CharacterObject troop, HeroClassDef heroClass,
            CultureObject preferredCulture)
        {
            EnsureBuilt();
            var role = InterpretRole(heroClass);
            WarnUnknown(heroClass, role);
            var selection = SmartTroopPolicy.SelectClosestTier(Index.Keys, troop?.Tier ?? 0,
                t => t.Culture == preferredCulture,
                t => IsCompatiblePath(t, role),
                t => t.Tier,
                t => CompatibleScore(t, role),
                t => t.StringId);
            return ToResult(role, selection);
        }

        public static string Describe(CharacterObject troop, HeroClassDef heroClass)
        {
            var info = GetTroopInfo(troop);
            if (info == null) return $"{troop?.StringId ?? "<null>"}: not indexed";
            var role = InterpretRole(heroClass);
            string destinations = string.Join(", ", info.TerminalDestinations.Select(t =>
                $"{t.StringId}[T{t.Tier},{t.DefaultFormationClass}{(t.IsMounted ? ",mounted" : "")}]"));
            return $"{troop.StringId} -> {destinations}; class={heroClass?.Formation ?? "none"}; " +
                   $"role={role}; maxTier={info.MaximumReachableTier}; compatible={IsCompatiblePath(troop, role)}";
        }

        public static SmartTroopPolicy.SmartTroopRole InterpretRole(HeroClassDef heroClass) =>
            SmartTroopPolicy.InterpretRole(heroClass?.Formation, heroClass?.Mounted == true);

        private static bool IsCompatiblePath(CharacterObject troop, SmartTroopPolicy.SmartTroopRole role) =>
            GetTroopInfo(troop)?.TerminalDestinations.Any(t => IsCompatible(t, role)) == true;

        private static int CompatibleScore(CharacterObject troop, SmartTroopPolicy.SmartTroopRole role)
        {
            var info = GetTroopInfo(troop);
            var compatible = info?.TerminalDestinations.Where(t => IsCompatible(t, role)).ToList();
            return compatible?.Any() == true ? compatible.Max(t => t.Tier) : -1;
        }

        private static bool IsCompatible(CharacterObject troop, SmartTroopPolicy.SmartTroopRole role)
        {
            if (troop == null) return false;
            var actual = troop.DefaultFormationClass;
            return role switch
            {
                SmartTroopPolicy.SmartTroopRole.HorseArcher => troop.IsMounted && actual == FormationClass.HorseArcher,
                SmartTroopPolicy.SmartTroopRole.Cavalry => troop.IsMounted &&
                    actual is FormationClass.Cavalry or FormationClass.LightCavalry or FormationClass.HeavyCavalry,
                SmartTroopPolicy.SmartTroopRole.FootRanged => !troop.IsMounted && actual == FormationClass.Ranged,
                SmartTroopPolicy.SmartTroopRole.InfantryFamily or SmartTroopPolicy.SmartTroopRole.Unknown => !troop.IsMounted &&
                    actual is FormationClass.Infantry or FormationClass.HeavyInfantry or FormationClass.Skirmisher,
                _ => false
            };
        }

        private static SelectionResult ToResult(SmartTroopPolicy.SmartTroopRole role,
            SmartTroopPolicy.Selection<CharacterObject> selection) => new()
        {
            InterpretedRole = role,
            SelectedTroop = selection.Value,
            FallbackTier = selection.FallbackTier,
            Score = selection.Score,
            RejectionReasons = selection.Rejections
        };

        private static void WarnUnknown(HeroClassDef heroClass, SmartTroopPolicy.SmartTroopRole role)
        {
            if (role == SmartTroopPolicy.SmartTroopRole.Unknown)
                Log.Info($"[TroopTreeIndex] WARNING: unknown hero formation '{heroClass?.Formation ?? "<none>"}'; using safe foot-infantry family");
        }

        private static void EnsureBuilt()
        {
            if (!isBuilt) BuildIndex();
        }
    }
}
