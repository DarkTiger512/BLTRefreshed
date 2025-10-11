using System;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Events
{
    [LocDisplayName("{=BLT_PE_Name}Priest's Crusade"),
     LocDescription("{=BLT_PE_Desc}A fanatical priest declares a holy war against your kingdom"),
     UsedImplicitly]
    public class PriestCrusadeEvent : RandomEventBase
    {
        public class Settings
        {
            [LocDisplayName("{=BLT_PE_Enabled}Enabled"),
             LocDescription("{=BLT_PE_EnabledDesc}Enable or disable the Priest's Crusade event"),
             PropertyOrder(1), UsedImplicitly]
            public bool Enabled { get; set; } = true;

            [LocDisplayName("{=BLT_PE_Chance}Trigger Chance Per Day"),
             LocDescription("{=BLT_PE_ChanceDesc}Chance per day for the event to trigger (0.0 to 1.0)"),
             PropertyOrder(2), UsedImplicitly]
            public float TriggerChance { get; set; } = 0.005f; // 0.5% per day

            [LocDisplayName("{=BLT_PE_Cooldown}Cooldown Days"),
             LocDescription("{=BLT_PE_CooldownDesc}Minimum days between events"),
             PropertyOrder(3), UsedImplicitly]
            public int CooldownDays { get; set; } = 60;

            [LocDisplayName("{=BLT_PE_ArmyPercent}Army Size Percentage"),
             LocDescription("{=BLT_PE_ArmyPercentDesc}Priest's army size as percentage of player's clan strength (10-200%)"),
             PropertyOrder(4), UsedImplicitly]
            public int ArmySizePercent { get; set; } = 80;

            [LocDisplayName("{=BLT_PE_MinTier}Minimum Kingdom Tier"),
             LocDescription("{=BLT_PE_MinTierDesc}Minimum player kingdom tier required for event to trigger"),
             PropertyOrder(5), UsedImplicitly]
            public int MinimumKingdomTier { get; set; } = 2;

            [LocDisplayName("{=BLT_PE_PriestName}Priest Name"),
             LocDescription("{=BLT_PE_PriestNameDesc}Name of the crusading priest"),
             PropertyOrder(6), UsedImplicitly]
            public string PriestName { get; set; } = "Father Zealot";

            [LocDisplayName("{=BLT_PE_ClanName}Crusade Clan Name"),
             LocDescription("{=BLT_PE_ClanNameDesc}Name of the crusader clan"),
             PropertyOrder(7), UsedImplicitly]
            public string CrusadeClanName { get; set; } = "Holy Crusaders";
        }

        private Settings config;

        public PriestCrusadeEvent(Settings settings)
        {
            config = settings;
            IsEnabled = settings.Enabled;
            TriggerChancePerDay = settings.TriggerChance;
            CooldownDays = settings.CooldownDays;
        }

        public override string EventId => "priest_crusade";
        public override string EventName => "Priest's Crusade";
        public override string EventDescription => "A fanatical priest declares holy war on your kingdom with an army of zealots";

        protected override bool CheckSpecificConditions()
        {
            // Player must be in a kingdom
            if (Hero.MainHero.Clan?.Kingdom == null)
                return false;

            // Kingdom must be at minimum tier
            if (Hero.MainHero.Clan.Kingdom.Clans.Sum(c => c.Tier) < config.MinimumKingdomTier)
                return false;

            return true;
        }

        protected override void ExecuteEvent()
        {
            Log.Info("[Priest Crusade Event] ExecuteEvent called - starting event creation");
            try
            {
                // Create the crusader clan
                Log.Info("[Priest Crusade Event] Creating crusader clan...");
                var crusaderClan = CreateCrusaderClan();
                if (crusaderClan == null)
                {
                    Log.Error("[Priest Crusade Event] Failed to create crusader clan");
                    return;
                }
                Log.Info($"[Priest Crusade Event] Created clan: {crusaderClan.Name}");

                // Create the priest hero
                Log.Info("[Priest Crusade Event] Creating priest hero...");
                var priest = CreatePriestHero(crusaderClan);
                if (priest == null)
                {
                    Log.Error("[Priest Crusade Event] Failed to create priest hero");
                    return;
                }
                Log.Info($"[Priest Crusade Event] Created priest: {priest.Name}");

                // Create the crusader army
                Log.Info("[Priest Crusade Event] Creating crusader army...");
                var crusaderArmy = CreateCrusaderArmy(priest);
                if (crusaderArmy == null)
                {
                    Log.Error("[Priest Crusade Event] Failed to create crusader army");
                    return;
                }
                Log.Info($"[Priest Crusade Event] Created army with {crusaderArmy.Party.MemberRoster.TotalManCount} troops");

                // Declare war on player's kingdom
                Log.Info("[Priest Crusade Event] Declaring war...");
                if (Hero.MainHero.Clan.Kingdom != null)
                {
                    // Use clan-to-kingdom war declaration since crusader clan is independent
                    DeclareWarAction.ApplyByDefault(crusaderClan, Hero.MainHero.Clan.Kingdom);
                    Log.Info($"[Priest Crusade Event] War declared between {crusaderClan.Name} and {Hero.MainHero.Clan.Kingdom.Name}");
                }
                else
                {
                    Log.Error("[Priest Crusade Event] Player is not in a kingdom, cannot declare war");
                    return;
                }

                // Show notification
                Log.Info("[Priest Crusade Event] Showing notification...");
                Log.ShowInformation(
                    $"{config.PriestName} has declared a holy crusade against {Hero.MainHero.Clan.Kingdom.Name}! " +
                    $"'{GetRandomCrusadeQuote()}' An army of {crusaderArmy.Party.MemberRoster.TotalManCount} zealots marches on your lands!",
                    priest.CharacterObject,
                    Log.Sound.Horns2
                );

                Log.Info($"[Priest Crusade Event] Event completed successfully! {crusaderClan.Name} has declared war on {Hero.MainHero.Clan.Kingdom.Name}");
            }
            catch (Exception ex)
            {
                Log.Error($"[Priest Crusade Event] Exception in ExecuteEvent: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private Clan CreateCrusaderClan()
        {
            var clanName = $"{config.CrusadeClanName}";
            var crusaderClan = Clan.CreateClan(clanName);
            
            // Use player's culture or a random one
            var culture = Hero.MainHero.Culture;
            var banner = Banner.CreateRandomBanner();
            
            crusaderClan.InitializeClan(new TextObject(clanName), new TextObject(clanName), culture, banner);
            crusaderClan.UpdateHomeSettlement(Settlement.All.Where(s => s.IsTown).ToList().GetRandomElementInefficiently());
            
            // Create a minor faction/kingdom for the crusaders
            var kingdomName = $"The Holy Crusade";
            // Note: Creating kingdoms requires more complex logic, for now the clan will be independent
            
            return crusaderClan;
        }

        private Hero CreatePriestHero(Clan clan)
        {
            try
            {
                // Create a MALE character template for the priest
                var culture = Hero.MainHero.Culture;
                var template = culture.NotableAndWandererTemplates
                    .FirstOrDefault(t => !t.IsFemale && (t.Occupation == Occupation.Wanderer || t.Occupation == Occupation.Preacher))
                    ?? culture.NotableAndWandererTemplates.FirstOrDefault(t => !t.IsFemale);

                if (template == null)
                {
                    Log.Error("[Priest Crusade Event] Could not find male character template for priest");
                    return null;
                }

                // Create the priest hero
                var priest = HeroCreator.CreateSpecialHero(template, clan.HomeSettlement);
                priest.SetName(new TextObject(config.PriestName), new TextObject(config.PriestName));
                
                // Make him clan leader
                clan.SetLeader(priest);
                
                // Give him some skills
                priest.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Leadership, 200);
                priest.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Tactics, 150);
                priest.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Charm, 180);
                
                // Equip the priest with modest tier 2-3 equipment (priestly robes and basic weapons)
                EquipPriest(priest);
                
                Log.Info($"[Priest Crusade Event] Created priest {priest.Name} with tier 2-3 equipment");
                
                return priest;
            }
            catch (Exception ex)
            {
                Log.Error($"[Priest Crusade Event] Error creating priest: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private void EquipPriest(Hero priest)
        {
            try
            {
                // Get tier 2-3 equipment for the priest (modest, priestly gear)
                var equipment = new Equipment();
                
                // Get all tier 2-3 items using MBObjectManager
                var weapons = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                    .Where(i => i.Type == ItemObject.ItemTypeEnum.OneHandedWeapon || i.Type == ItemObject.ItemTypeEnum.TwoHandedWeapon)
                    .Where(i => i.Tier >= ItemObject.ItemTiers.Tier2 && i.Tier <= ItemObject.ItemTiers.Tier3)
                    .ToList();
                
                var helmets = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                    .Where(i => i.Type == ItemObject.ItemTypeEnum.HeadArmor)
                    .Where(i => i.Tier >= ItemObject.ItemTiers.Tier2 && i.Tier <= ItemObject.ItemTiers.Tier3)
                    .ToList();
                
                var armors = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                    .Where(i => i.Type == ItemObject.ItemTypeEnum.BodyArmor)
                    .Where(i => i.Tier >= ItemObject.ItemTiers.Tier2 && i.Tier <= ItemObject.ItemTiers.Tier3)
                    .ToList();
                
                var boots = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                    .Where(i => i.Type == ItemObject.ItemTypeEnum.LegArmor)
                    .Where(i => i.Tier >= ItemObject.ItemTiers.Tier2 && i.Tier <= ItemObject.ItemTiers.Tier3)
                    .ToList();
                
                var capes = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                    .Where(i => i.Type == ItemObject.ItemTypeEnum.Cape)
                    .Where(i => i.Tier >= ItemObject.ItemTiers.Tier2 && i.Tier <= ItemObject.ItemTiers.Tier3)
                    .ToList();

                // Equip with modest gear
                if (weapons.Any())
                    equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon0, new EquipmentElement(weapons.GetRandomElement()));
                if (helmets.Any())
                    equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Head, new EquipmentElement(helmets.GetRandomElement()));
                if (armors.Any())
                    equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Body, new EquipmentElement(armors.GetRandomElement()));
                if (boots.Any())
                    equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Leg, new EquipmentElement(boots.GetRandomElement()));
                if (capes.Any())
                    equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Cape, new EquipmentElement(capes.GetRandomElement()));

                // Set both battle and civilian equipment
                priest.BattleEquipment.FillFrom(equipment);
                priest.CivilianEquipment.FillFrom(equipment);
                
                Log.Info($"[Priest Crusade Event] Equipped {priest.Name} with tier 2-3 priestly gear");
            }
            catch (Exception ex)
            {
                Log.Error($"[Priest Crusade Event] Error equipping priest: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private MobileParty CreateCrusaderArmy(Hero priest)
        {
            try
            {
                // Calculate army size based on player's clan strength
                int playerClanStrength = (int)Hero.MainHero.Clan.TotalStrength;
                int targetArmySize = (int)((playerClanStrength * config.ArmySizePercent) / 100f);
                targetArmySize = Math.Max(100, Math.Min(targetArmySize, 1000)); // Clamp between 100 and 1000

                // Create the party
                var party = priest.Clan.CreateNewMobileParty(priest);
                Log.Info($"[Priest Crusade Event] Created mobile party for {priest.Name}");

                // Get peasant mishmash troops - tier 1-3 from various cultures (with valid equipment)
                var culture = priest.Culture;
                
                // Collect tier 1-3 troops with valid equipment from all cultures for variety
                var allCrusaderTroops = CharacterObject.All.Where(c => 
                    c.IsHero == false && 
                    c.Occupation == Occupation.Soldier &&
                    c.Tier >= 1 && c.Tier <= 3 && // Peasant tiers
                    c.BattleEquipments != null && c.BattleEquipments.Any() && // Must have equipment
                    c.BattleEquipments.Any(eq => eq.Horse.Item == null) // Exclude cavalry
                ).ToList();

                if (allCrusaderTroops.Any())
                {
                    // Create a true peasant mishmash - random groups from various cultures and tiers
                    int remainingTroops = targetArmySize;
                    int groupCount = Math.Min(8, allCrusaderTroops.Count); // Up to 8 different troop types for variety
                    
                    for (int i = 0; i < groupCount && remainingTroops > 0; i++)
                    {
                        var troopType = allCrusaderTroops[MBRandom.RandomInt(allCrusaderTroops.Count)];
                        
                        // Random group sizes to create a chaotic peasant mob feel
                        int minGroupSize = Math.Max(1, remainingTroops / (groupCount * 2));
                        int maxGroupSize = Math.Max(minGroupSize, remainingTroops / (groupCount - i));
                        int count = MBRandom.RandomInt(minGroupSize, maxGroupSize + 1);
                        count = Math.Min(count, remainingTroops);
                        
                        party.MemberRoster.AddToCounts(troopType, count);
                        remainingTroops -= count;
                        
                        Log.Info($"[Priest Crusade Event] Added {count}x {troopType.Name} (Tier {troopType.Tier})");
                    }
                }
                else
                {
                    // Fallback - use culture's basic troops
                    Log.Info("[Priest Crusade Event] No valid low-tier troops found, using basic troops");
                    var basicTroop = culture.BasicTroop;
                    if (basicTroop != null)
                    {
                        party.MemberRoster.AddToCounts(basicTroop, targetArmySize);
                    }
                }

                // Set party properties - allow them to move around
                party.Ai.SetDoNotMakeNewDecisions(false);
                
                Log.Info($"[Priest Crusade Event] Created peasant crusader army with {party.MemberRoster.TotalManCount} troops");

                return party;
            }
            catch (Exception ex)
            {
                Log.Error($"[Priest Crusade Event] Error creating crusader army: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private string GetRandomCrusadeQuote()
        {
            var quotes = new[]
            {
                "Deus Vult!",
                "God wills it!",
                "The heavens demand justice!",
                "Your sins shall be cleansed by holy fire!",
                "The faithful shall inherit these lands!",
                "Repent or face divine wrath!",
                "The Lord commands your downfall!"
            };
            return quotes[MBRandom.RandomInt(quotes.Length)];
        }
    }
}
