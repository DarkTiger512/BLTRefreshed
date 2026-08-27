using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace BLTAdoptAHero.Util
{
    /// <summary>
    /// Cycle-safe index of every loaded troop and the terminal formations reachable from it.
    /// The index intentionally uses loaded CharacterObjects rather than hard-coded culture trees so
    /// total conversions and troop-tree mods participate automatically.
    /// </summary>
    public static class TroopTreeIndex
    {
        public sealed class TroopInfo
        {
            public CharacterObject Troop { get; internal set; }
            public IReadOnlyList<CharacterObject> TerminalDestinations { get; internal set; }
                = Array.Empty<CharacterObject>();

            public bool CanReach(HeroClassDef heroClass) =>
                TerminalDestinations.Any(t => IsFormationCompatible(t, heroClass));
        }

        private static readonly Dictionary<CharacterObject, TroopInfo> Index = new();
        private static bool isBuilt;

        public static void BuildIndex()
        {
            Index.Clear();

            foreach (var troop in CharacterObject.All.Where(t => t != null && !t.IsHero))
            {
                Index[troop] = new TroopInfo
                {
                    Troop = troop,
                    TerminalDestinations = CycleSafeGraph.FindTerminals(
                        troop,
                        t => t.UpgradeTargets ?? Array.Empty<CharacterObject>())
                };
            }

            isBuilt = true;
            Log.Info($"[TroopTreeIndex] Indexed {Index.Count} loaded troops");
        }

        public static TroopInfo GetTroopInfo(CharacterObject troop)
        {
            EnsureBuilt();
            return troop != null && Index.TryGetValue(troop, out var info) ? info : null;
        }

        public static bool CanReachHeroClass(CharacterObject troop, HeroClassDef heroClass)
        {
            if (heroClass == null) return true;
            return GetTroopInfo(troop)?.CanReach(heroClass) == true;
        }

        public static CharacterObject SelectCompatibleUpgrade(CharacterObject troop, HeroClassDef heroClass)
        {
            if (troop?.UpgradeTargets == null) return null;

            return troop.UpgradeTargets
                .Where(t => CanReachHeroClass(t, heroClass))
                .OrderBy(t => t.StringId, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        public static string Describe(CharacterObject troop, HeroClassDef heroClass)
        {
            var info = GetTroopInfo(troop);
            if (info == null) return $"{troop?.StringId ?? "<null>"}: not indexed";

            string destinations = string.Join(", ", info.TerminalDestinations.Select(t =>
                $"{t.StringId}[{t.DefaultFormationClass}{(t.IsMounted ? ",mounted" : "")}]"));
            return $"{troop.StringId} -> {destinations}; class={heroClass?.Formation ?? "none"}; compatible={info.CanReach(heroClass)}";
        }

        private static void EnsureBuilt()
        {
            if (!isBuilt) BuildIndex();
        }

        private static bool IsFormationCompatible(CharacterObject troop, HeroClassDef heroClass)
        {
            if (troop == null || heroClass == null || string.IsNullOrWhiteSpace(heroClass.Formation))
                return true;

            string formation = heroClass.Formation.Replace(" ", string.Empty).ToLowerInvariant();
            FormationClass actual = troop.DefaultFormationClass;

            return formation switch
            {
                "infantry" or "heavyinfantry" =>
                    !troop.IsMounted && actual is FormationClass.Infantry or FormationClass.HeavyInfantry,
                "ranged" or "archer" or "crossbow" =>
                    !troop.IsMounted && actual == FormationClass.Ranged,
                "skirmisher" => !troop.IsMounted && actual == FormationClass.Skirmisher,
                "cavalry" or "lightcavalry" or "heavycavalry" =>
                    troop.IsMounted && actual is FormationClass.Cavalry or FormationClass.LightCavalry or FormationClass.HeavyCavalry,
                "horsearcher" => troop.IsMounted && actual == FormationClass.HorseArcher,
                _ => Enum.TryParse(heroClass.Formation, true, out FormationClass desired)
                    ? actual == desired
                    : true
            };
        }
    }
}
