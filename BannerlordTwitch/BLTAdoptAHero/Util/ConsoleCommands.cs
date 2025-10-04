using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Util;
using BLTAdoptAHero;
using HarmonyLib;
using JetBrains.Annotations;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace BLTAdoptAHero.Util
{
    internal static class ConsoleCommands
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("showhero", "blt")]
        [UsedImplicitly]
        public static string ShowHero(List<string> strings)
        {
            if (strings.Count != 1)
            {
                return "Provide the hero name";
            }

            var hero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(strings[0]);
            if (hero == null)
            {
                return $"Couldn't find hero {strings[0]}";
            }

            if (!PartyScreenAllowed)
                return "Hero screen not allowed now (you must be on the campaign map, not in a siege or encounter)"; ;

            if (ScreenManager.TopScreen is not MapScreen)
            {
                Game.Current.GameStateManager.PopState();
            }

            OpenScreenAsInventoryOf(hero.CharacterObject);

            return $"Opened inventory of {strings[0]}";
        }

        private class FakeMarketData : IMarketData
        {
            public int GetPrice(ItemObject item, MobileParty tradingParty, bool isSelling, PartyBase merchantParty)
            {
                return item.Value;
            }

            public int GetPrice(EquipmentElement itemRosterElement, MobileParty tradingParty, bool isSelling, PartyBase merchantParty)
            {
                return itemRosterElement.ItemValue;
            }
        }

        private static void OpenScreenAsInventoryOf(CharacterObject character)
        {
            var inventoryLogicFieldInfo = AccessTools.Field(typeof(InventoryManager), "_inventoryLogic");

            //InventoryManager.OpenScreenAsInventoryOf(MobileParty.MainParty, character);

            // Might be broken since 1.7.0
            var inventoryLogic = new InventoryLogic(null);
            inventoryLogicFieldInfo.SetValue(InventoryManager.Instance, inventoryLogic);
            var memberRoster = new TroopRoster(null);
            memberRoster.AddToCounts(character, 1);
            inventoryLogic.Initialize(new(), new(), memberRoster, false, true, character, InventoryManager.InventoryCategoryType.None, new FakeMarketData(), false);
            var state = Game.Current.GameStateManager.CreateState<InventoryState>();
            state.InitializeLogic(inventoryLogic);
            Game.Current.GameStateManager.PushState(state);
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("showheroes", "blt")]
        [UsedImplicitly]
        public static string ShowHeroes(List<string> strings)
        {
            return OpenAdoptedHeroScreen();
        }

        private static bool PartyScreenAllowed
        {
            get
            {
                if (Game.Current.GameStateManager.ActiveState is PartyState)
                {
                    return false;
                }
                if (Hero.MainHero.HeroState == Hero.CharacterStates.Prisoner)
                {
                    return false;
                }
                if (MobileParty.MainParty.MapEvent != null)
                {
                    return false;
                }
                return Mission.Current == null || Mission.Current.IsPartyWindowAccessAllowed;
            }
        }

        private static string OpenAdoptedHeroScreen()
        {
            if (!PartyScreenAllowed)
                return "Heroes screen not allowed now (you must be on the campaign map, not in a siege or encounter)";

            if (ScreenManager.TopScreen is not MapScreen)
            {
                Game.Current.GameStateManager.PopState();
            }

            // var _partyScreenLogic = new PartyScreenLogic();
            // AccessTools.Field(typeof(PartyScreenManager), "_partyScreenLogic").SetValue(PartyScreenManager.Instance, _partyScreenLogic);
            // AccessTools.Field(typeof(PartyScreenManager), "_currentMode").SetValue(PartyScreenManager.Instance, PartyScreenMode.Normal);

            var heroRoster = new TroopRoster(null);
            foreach (var hero in BLTAdoptAHeroCampaignBehavior.GetAllAdoptedHeroes().OrderBy(h => h.Name.Raw().ToLower()))
            {
                heroRoster.AddToCounts(hero.CharacterObject, 1);
            }

            if (heroRoster.Count == 0)
            {
                return "No heroes to show";
            }

            PartyScreenManager.OpenScreenWithDummyRoster(
                heroRoster,
                new TroopRoster(null),
                new TroopRoster(null),
                new TroopRoster(null), new("Viewers"),
                null, 0, 0, null, null, null);

            // _partyScreenLogic.Initialize(new PartyScreenLogicInitializationData
            // {
            //     LeftMemberRoster = heroRoster,
            //     Header = new ("Viewers"),
            // });
            //_partyScreenLogic.Initialize(heroRoster, new(null), MobileParty.MainParty, false, new("Viewers"), 0, (_, _, _, _, _, _, _, _, _) => true, new("BLT Viewer Heroes"), false);
            //_partyScreenLogic.InitializeTrade(PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.NotTransferable);
            //_partyScreenLogic.SetTroopTransferableDelegate((_, _, _, _) => false);

            // var partyState = Game.Current.GameStateManager.CreateState<PartyState>();
            // partyState.InitializeLogic(_partyScreenLogic);
            // Game.Current.GameStateManager.PushState(partyState);

            return "Heroes screen opened";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("debug_troop_tree", "blt")]
        [UsedImplicitly]
        public static string DebugTroopTree(List<string> args)
        {
            if (args.Count == 0)
            {
                return "Usage: blt.debug_troop_tree [culture_name] - Shows troop tree and hiring validation for culture (e.g., blt.debug_troop_tree sturgia)";
            }

            string cultureName = args[0].ToLower();
            
            // Find the culture
            var culture = Campaign.Current?.ObjectManager?.GetObjectTypeList<CultureObject>()
                ?.FirstOrDefault(c => c.StringId.ToLower().Contains(cultureName) || 
                                     c.Name.ToString().ToLower().Contains(cultureName));
            
            if (culture == null)
            {
                var availableCultures = string.Join(", ", Campaign.Current?.ObjectManager?.GetObjectTypeList<CultureObject>()
                    ?.Select(c => c.StringId) ?? new List<string>());
                return $"Culture '{cultureName}' not found. Available: {availableCultures}";
            }

            var behavior = Campaign.Current?.GetCampaignBehavior<BLTAdoptAHeroCampaignBehavior>();
            if (behavior == null)
            {
                return "BLTAdoptAHeroCampaignBehavior not found. Make sure BLTAdoptAHero is loaded.";
            }
            
            var output = $"=== {culture.Name} ({culture.StringId}) Troop Tree & Hiring Debug ===\n";
            output += $"\n*** CULTURE BASIC TROOPS ***\n";
            output += $"BasicTroop: {(culture.BasicTroop != null ? culture.BasicTroop.Name.ToString() : "NONE")} ({(culture.BasicTroop != null ? culture.BasicTroop.StringId : "N/A")})\n";
            output += $"EliteBasicTroop: {(culture.EliteBasicTroop != null ? culture.EliteBasicTroop.Name.ToString() : "NONE")} ({(culture.EliteBasicTroop != null ? culture.EliteBasicTroop.StringId : "N/A")})\n";
            
            // Get all basic troops for this culture
            var basicTroops = Campaign.Current.ObjectManager.GetObjectTypeList<CharacterObject>()
                .Where(t => t.Culture == culture && t.IsBasicTroop && !t.IsHero)
                .OrderBy(t => t.Tier)
                .ToList();

            foreach (var troop in basicTroops)
            {
                output += $"\n{'='} {troop.Name} (T{troop.Tier}) - {troop.StringId} {'='}\n";
                
                // Show if this is BasicTroop or EliteBasicTroop
                bool isBasicTroop = culture.BasicTroop?.StringId == troop.StringId;
                bool isEliteTroop = culture.EliteBasicTroop?.StringId == troop.StringId;
                
                if (isBasicTroop)
                    output += $"  *** THIS IS BasicTroop FOR {culture.Name} ***\n";
                if (isEliteTroop)
                    output += $"  *** THIS IS EliteBasicTroop FOR {culture.Name} ***\n";
                if (!isBasicTroop && !isEliteTroop)
                    output += $"  (Not a culture recruitment troop)\n";
                
                output += $"\n  TROOP STATS:\n";
                output += $"    Formation: {troop.DefaultFormationClass}\n";
                output += $"    Mounted: {troop.IsMounted}\n";
                output += $"    Tier: {troop.Tier}\n";
                
                // Show what's in the index for this troop
                var troopInfo = TroopTreeIndex.GetTroopInfo(troop);
                if (troopInfo != null)
                {
                    output += $"\n  TROOP TREE INDEX:\n";
                    output += $"    Can become Cavalry: {troopInfo.CanBecomeCavalry}\n";
                    output += $"    Can become Archer: {troopInfo.CanBecomeArcher}\n";
                    output += $"    Can become Horse Archer: {troopInfo.CanBecomeHorseArcher}\n";
                    output += $"    Possible Formations: {string.Join(", ", troopInfo.PossibleFormations)}\n";
                }
                else
                {
                    output += $"\n  **NOT IN TROOP TREE INDEX**\n";
                }
                
                // Show upgrade paths
                var upgrades = troop.UpgradeTargets;
                if (upgrades?.Length > 0)
                {
                    output += $"\n  UPGRADE PATHS:\n";
                    foreach (var upgrade in upgrades)
                    {
                        output += $"    → {upgrade.Name} (T{upgrade.Tier}) {upgrade.DefaultFormationClass} {(upgrade.IsMounted ? "MOUNTED" : "")}\n";
                    }
                }
                else
                {
                    output += $"\n  UPGRADE PATHS: None (final tier)\n";
                }
                
                // Show final destinations
                var finalDestinations = behavior.GetFinalTierDestinationsForDebug(troop);
                if (finalDestinations.Any())
                {
                    output += $"\n  FINAL DESTINATIONS (Tier 5-6):\n";
                    foreach (var dest in finalDestinations)
                    {
                        output += $"    ⟹ {dest.Name} (T{dest.Tier}) {dest.DefaultFormationClass} {(dest.IsMounted ? "MOUNTED" : "")}\n";
                    }
                }
                else
                {
                    output += $"\n  FINAL DESTINATIONS: None or already final tier\n";
                }
                
                // VALIDATION CHECKS - Show if this troop would be hired for different hero classes
                output += $"\n  HIRING VALIDATION (Would this be hired for...):\n";
                
                var heroClasses = new[] 
                { 
                    ("Skirmisher", "skirmisher"), 
                    ("Infantry", "infantry"), 
                    ("Cavalry", "cavalry"),
                    ("Horse Archer", "horsearcher"),
                    ("Ranged", "ranged")
                };
                
                foreach (var (className, formation) in heroClasses)
                {
                    // Create a mock hero class for testing
                    var testClass = new HeroClassDef { Formation = formation };
                    
                    if (troopInfo != null)
                    {
                        bool canBeHired = behavior.TestCanTroopEventuallyBecomeClass(troopInfo, testClass);
                        output += $"    {className}: {(canBeHired ? "✓ YES - WOULD BE HIRED" : "✗ NO - REJECTED")}\n";
                        
                        if (!canBeHired && finalDestinations.Any())
                        {
                            // Show WHY it was rejected
                            var incompatibleDests = finalDestinations.Where(dest =>
                            {
                                return formation.ToLower() switch
                                {
                                    "skirmisher" or "infantry" or "heavyinfantry" => 
                                        dest.IsMounted || (dest.DefaultFormationClass != FormationClass.Infantry && 
                                                          dest.DefaultFormationClass != FormationClass.HeavyInfantry),
                                    "cavalry" or "lightcavalry" or "heavycavalry" =>
                                        !dest.IsMounted || dest.DefaultFormationClass == FormationClass.HorseArcher,
                                    "horsearcher" =>
                                        dest.DefaultFormationClass != FormationClass.HorseArcher,
                                    "ranged" =>
                                        dest.IsMounted || dest.DefaultFormationClass != FormationClass.Ranged,
                                    _ => false
                                };
                            }).ToList();
                            
                            if (incompatibleDests.Any())
                            {
                                output += $"      REASON: Has incompatible final destination(s):\n";
                                foreach (var bad in incompatibleDests)
                                {
                                    output += $"        ✗ {bad.Name} ({bad.DefaultFormationClass}{(bad.IsMounted ? " MOUNTED" : "")})\n";
                                }
                            }
                        }
                    }
                    else
                    {
                        output += $"    {className}: ? UNKNOWN (not in index)\n";
                    }
                }
            }
            
            // Write to log file for easier reading
            string logPath = $"../../Modules/BLTAdoptAHero/debug_troop_tree_{culture.StringId}.txt";
            try
            {
                System.IO.File.WriteAllText(logPath, output);
                InformationManager.DisplayMessage(new InformationMessage($"Troop tree debug written to: {logPath}"));
                return $"Debug analysis for {culture.Name} written to: {logPath}";
            }
            catch (System.Exception ex)
            {
                return $"Failed to write debug file: {ex.Message}";
            }
        }
    }
}