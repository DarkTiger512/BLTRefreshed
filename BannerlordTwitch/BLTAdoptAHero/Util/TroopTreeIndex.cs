using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using BannerlordTwitch.Util;

namespace BLTAdoptAHero.Util
{
    /// <summary>
    /// Dynamic troop tree indexing system that maps all troops to their possible final upgrade paths.
    /// This allows for efficient lookups of where specific unit types (like cavalry) can be found
    /// in the complex upgrade hierarchies across all cultures and mods.
    /// </summary>
    public static class TroopTreeIndex
    {
        private static readonly Dictionary<string, TroopInfo> _troopIndex = new();
        private static readonly Dictionary<FormationClass, List<CharacterObject>> _formationIndex = new();
        private static readonly Dictionary<CharacterObject, List<CharacterObject>> _upgradePathsCache = new();
        private static bool _isIndexed = false;

        public class TroopInfo
        {
            public CharacterObject Troop { get; set; }
            public List<CharacterObject> AllUpgradePaths { get; set; } = new();
            public List<FormationClass> PossibleFormations { get; set; } = new();
            public bool CanBecomeFormation(FormationClass formation) => PossibleFormations.Contains(formation);
            public bool CanBecomeCavalry => CanBecomeFormation(FormationClass.Cavalry) || 
                                           CanBecomeFormation(FormationClass.HeavyCavalry) || 
                                           CanBecomeFormation(FormationClass.LightCavalry);
            public bool CanBecomeArcher => CanBecomeFormation(FormationClass.Ranged);
            public bool CanBecomeHorseArcher => CanBecomeFormation(FormationClass.HorseArcher);
        }

        /// <summary>
        /// Build the complete troop tree index for all cultures and mods.
        /// This should be called once at game start.
        /// </summary>
        public static void BuildIndex()
        {
            Log.Info("[TroopTreeIndex] Building dynamic troop tree index...");
            
            _troopIndex.Clear();
            _formationIndex.Clear();
            _upgradePathsCache.Clear();
            
            // Initialize formation index
            foreach (FormationClass formation in Enum.GetValues(typeof(FormationClass)))
            {
                _formationIndex[formation] = new List<CharacterObject>();
            }
            
            // Index all troops from all cultures
            var allTroops = GetAllTroopsFromAllCultures();
            
            foreach (var troop in allTroops)
            {
                IndexTroop(troop);
            }
            
            _isIndexed = true;
            
            Log.Info($"[TroopTreeIndex] Indexed {_troopIndex.Count} troops across {_formationIndex.Count} formation types");
            
            // Log some statistics
            foreach (var formation in _formationIndex.Keys)
            {
                var count = _formationIndex[formation].Count;
                if (count > 0)
                {
                    Log.Info($"[TroopTreeIndex] Formation {formation}: {count} troops can eventually become this type");
                }
            }
        }

        /// <summary>
        /// Get all troops that can eventually become the specified formation class.
        /// </summary>
        public static List<CharacterObject> GetTroopsThatCanBecome(FormationClass formation)
        {
            EnsureIndexed();
            return _formationIndex.TryGetValue(formation, out var troops) ? troops : new List<CharacterObject>();
        }

        /// <summary>
        /// Get all troops from a specific culture that can eventually become the specified formation class.
        /// </summary>
        public static List<CharacterObject> GetTroopsThatCanBecome(FormationClass formation, CultureObject culture)
        {
            EnsureIndexed();
            return GetTroopsThatCanBecome(formation)
                .Where(t => t.Culture == culture)
                .ToList();
        }

        /// <summary>
        /// Check if a specific troop can eventually upgrade to the specified formation class.
        /// </summary>
        public static bool CanTroopBecomeFormation(CharacterObject troop, FormationClass formation)
        {
            EnsureIndexed();
            return _troopIndex.TryGetValue(troop.StringId, out var troopInfo) 
                && troopInfo.CanBecomeFormation(formation);
        }

        /// <summary>
        /// Get the complete upgrade tree for a troop (all possible final destinations).
        /// </summary>
        public static List<CharacterObject> GetAllUpgradePaths(CharacterObject troop)
        {
            EnsureIndexed();
            
            if (_upgradePathsCache.TryGetValue(troop, out var cached))
                return cached;
                
            return _troopIndex.TryGetValue(troop.StringId, out var troopInfo) 
                ? troopInfo.AllUpgradePaths 
                : new List<CharacterObject>();
        }

        /// <summary>
        /// Get troop information including all upgrade possibilities.
        /// </summary>
        public static TroopInfo GetTroopInfo(CharacterObject troop)
        {
            EnsureIndexed();
            return _troopIndex.TryGetValue(troop.StringId, out var info) ? info : null;
        }

        /// <summary>
        /// Find the best troops for a hero class from a specific culture.
        /// Returns troops ordered by compatibility (exact matches first, then eventual upgrade paths).
        /// </summary>
        public static List<CharacterObject> FindBestTroopsForHeroClass(HeroClassDef heroClass, CultureObject culture)
        {
            EnsureIndexed();
            
            if (heroClass == null) return new List<CharacterObject>();
            
            var results = new List<CharacterObject>();
            var heroFormation = heroClass.Formation?.ToLower();
            
            // Get all troops from the culture (basic and elite)
            var cultureTroops = new List<CharacterObject>();
            if (culture.BasicTroop != null) cultureTroops.Add(culture.BasicTroop);
            if (culture.EliteBasicTroop != null) cultureTroops.Add(culture.EliteBasicTroop);
            
            foreach (var baseTroop in cultureTroops)
            {
                var troopInfo = GetTroopInfo(baseTroop);
                if (troopInfo == null) continue;
                
                // Check if this troop can eventually become what the hero class needs
                bool isCompatible = heroFormation switch
                {
                    "cavalry" or "lightcavalry" or "heavycavalry" => troopInfo.CanBecomeCavalry,
                    "ranged" => troopInfo.CanBecomeArcher,
                    "horsearcher" => troopInfo.CanBecomeHorseArcher,
                    "infantry" or "heavyinfantry" => troopInfo.CanBecomeFormation(FormationClass.Infantry) || 
                                                    troopInfo.CanBecomeFormation(FormationClass.HeavyInfantry),
                    "skirmisher" => troopInfo.CanBecomeFormation(FormationClass.Skirmisher),
                    _ => true // Unknown class, allow all
                };
                
                if (isCompatible)
                {
                    results.Add(baseTroop);
                }
            }
            
            return results;
        }

        /// <summary>
        /// Rebuild the index. Useful when mods are loaded/unloaded.
        /// </summary>
        public static void RebuildIndex()
        {
            _isIndexed = false;
            BuildIndex();
        }

        #region Private Methods

        private static void EnsureIndexed()
        {
            if (!_isIndexed)
            {
                BuildIndex();
            }
        }

        private static List<CharacterObject> GetAllTroopsFromAllCultures()
        {
            var allTroops = new List<CharacterObject>();
            
            foreach (var culture in MBObjectManager.Instance.GetObjectTypeList<CultureObject>())
            {
                if (culture.BasicTroop != null)
                    allTroops.Add(culture.BasicTroop);
                    
                if (culture.EliteBasicTroop != null)
                    allTroops.Add(culture.EliteBasicTroop);
            }
            
            return allTroops.Distinct().ToList();
        }

        private static void IndexTroop(CharacterObject troop)
        {
            if (troop == null || _troopIndex.ContainsKey(troop.StringId))
                return;
                
            var troopInfo = new TroopInfo { Troop = troop };
            
            // Find all possible upgrade paths for this troop
            var allUpgrades = FindAllUpgradeTargets(troop, new HashSet<string>());
            troopInfo.AllUpgradePaths = allUpgrades;
            
            // Index by formation classes this troop can eventually become
            var possibleFormations = new HashSet<FormationClass> { troop.DefaultFormationClass };
            
            foreach (var upgrade in allUpgrades)
            {
                possibleFormations.Add(upgrade.DefaultFormationClass);
            }
            
            troopInfo.PossibleFormations = possibleFormations.ToList();
            
            // Add to formation-based indices
            foreach (var formation in possibleFormations)
            {
                if (!_formationIndex[formation].Contains(troop))
                {
                    _formationIndex[formation].Add(troop);
                }
            }
            
            _troopIndex[troop.StringId] = troopInfo;
            _upgradePathsCache[troop] = allUpgrades;
        }

        private static List<CharacterObject> FindAllUpgradeTargets(CharacterObject troop, HashSet<string> visited)
        {
            if (troop == null || visited.Contains(troop.StringId))
                return new List<CharacterObject>();
                
            visited.Add(troop.StringId);
            
            var allTargets = new List<CharacterObject>();
            
            // If this troop has no upgrades, it's a terminal node
            if (troop.UpgradeTargets == null || !troop.UpgradeTargets.Any())
            {
                allTargets.Add(troop);
                return allTargets;
            }
            
            // Recursively find all upgrade targets
            foreach (var upgradeTarget in troop.UpgradeTargets)
            {
                var subTargets = FindAllUpgradeTargets(upgradeTarget, new HashSet<string>(visited));
                allTargets.AddRange(subTargets);
            }
            
            return allTargets.Distinct().ToList();
        }

        #endregion
    }
}
