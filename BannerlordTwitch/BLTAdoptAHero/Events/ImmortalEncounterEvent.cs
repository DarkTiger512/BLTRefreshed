using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Events
{
    [LocDisplayName("{=BLT_IE_Name}The Immortal Encounter"),
     LocDescription("{=BLT_IE_Desc}A mysterious immortal challenges you and your viewers to battle"),
     UsedImplicitly]
    public class ImmortalEncounterEvent : RandomEventBase
    {
        public class Settings
        {
            [LocDisplayName("{=BLT_IE_Enabled}Enabled"),
             LocDescription("{=BLT_IE_EnabledDesc}Enable or disable The Immortal encounter event"),
             PropertyOrder(1), UsedImplicitly]
            public bool Enabled { get; set; } = true;

            [LocDisplayName("{=BLT_IE_Chance}Trigger Chance Per Day"),
             LocDescription("{=BLT_IE_ChanceDesc}Chance per day for the event to trigger (0.0 to 1.0)"),
             PropertyOrder(2), UsedImplicitly]
            public float TriggerChance { get; set; } = 0.002f; // 0.2% per day

            [LocDisplayName("{=BLT_IE_Cooldown}Cooldown Days"),
             LocDescription("{=BLT_IE_CooldownDesc}Minimum days between encounters"),
             PropertyOrder(3), UsedImplicitly]
            public int CooldownDays { get; set; } = 90;

            [LocDisplayName("{=BLT_IE_ArmyPercent}Army Size Percentage"),
             LocDescription("{=BLT_IE_ArmyPercentDesc}Immortal's army size as percentage of player's party strength (50-200%)"),
             PropertyOrder(4), UsedImplicitly]
            public int ArmySizePercent { get; set; } = 120;

            [LocDisplayName("{=BLT_IE_MinLevel}Minimum Player Level"),
             LocDescription("{=BLT_IE_MinLevelDesc}Minimum player hero level required for event to trigger"),
             PropertyOrder(5), UsedImplicitly]
            public int MinimumPlayerLevel { get; set; } = 10;

            [LocDisplayName("{=BLT_IE_GoldReward}Gold Reward Per Participant"),
             LocDescription("{=BLT_IE_GoldRewardDesc}Gold reward given to each BLT participant if they win the battle"),
             PropertyOrder(6), UsedImplicitly]
            public int GoldRewardPerParticipant { get; set; } = 100000;

            [LocDisplayName("{=BLT_IE_ImmortalName}The Immortal's Name"),
             LocDescription("{=BLT_IE_ImmortalNameDesc}Name of the mysterious immortal"),
             PropertyOrder(7), UsedImplicitly]
            public string ImmortalName { get; set; } = "The Immortal";

            [LocDisplayName("{=BLT_IE_CultName}Cult Name"),
             LocDescription("{=BLT_IE_CultNameDesc}Name of the immortal's cult"),
             PropertyOrder(8), UsedImplicitly]
            public string CultName { get; set; } = "Cult of the Eternal";
        }

        private Settings config;
        private static MobileParty immortalParty;
        private static Hero immortalHero;
        private static bool dialogueActive = false;

        public ImmortalEncounterEvent(Settings settings)
        {
            config = settings;
            IsEnabled = settings.Enabled;
            TriggerChancePerDay = settings.TriggerChance;
            CooldownDays = settings.CooldownDays;
        }

        public override string EventId => "immortal_encounter";
        public override string EventName => "The Immortal Encounter";
        public override string EventDescription => "A mysterious immortal challenges you to a deadly battle for glory and gold";
        
        public override void RegisterDialogs(CampaignGameStarter starter)
        {
            RegisterDialogue(starter);
        }

        protected override bool CheckSpecificConditions()
        {
            // Player must be alive and on the map
            if (Hero.MainHero == null || !Hero.MainHero.IsAlive)
                return false;

            // Player must have a party
            if (MobileParty.MainParty == null)
                return false;

            // Player must be high enough level
            if (Hero.MainHero.Level < config.MinimumPlayerLevel)
                return false;

            // Player must not be in a settlement or siege
            if (MobileParty.MainParty.IsGarrison || MobileParty.MainParty.BesiegedSettlement != null)
                return false;

            // Don't trigger if already in conversation or battle
            if (PlayerEncounter.Current != null || dialogueActive)
                return false;

            return true;
        }

        protected override void ExecuteEvent()
        {
            Log.Info("[Immortal Encounter] ExecuteEvent called - creating encounter");
            try
            {
                // Create the immortal and their party
                Log.Info("[Immortal Encounter] Creating immortal hero...");
                immortalHero = CreateImmortalHero();
                if (immortalHero == null)
                {
                    Log.Error("[Immortal Encounter] Failed to create immortal hero");
                    return;
                }
                Log.Info($"[Immortal Encounter] Created immortal: {immortalHero.Name}");

                // Create the cult party
                Log.Info("[Immortal Encounter] Creating cult army...");
                immortalParty = CreateCultArmy(immortalHero);
                if (immortalParty == null)
                {
                    Log.Error("[Immortal Encounter] Failed to create cult army");
                    return;
                }
                Log.Info($"[Immortal Encounter] Created cult army with {immortalParty.MemberRoster.TotalManCount} troops");

                // Position the party right on top of the player
                immortalParty.Position2D = MobileParty.MainParty.Position2D;
                
                // Set dialogue flag before encounter
                dialogueActive = true;
                
                Log.Info($"[Immortal Encounter] Setting up encounter. DialogActive: {dialogueActive}, ImmortalHero: {immortalHero?.Name}");
                
                // Use the proper encounter initiation method
                try
                {
                    // This will start the encounter and automatically trigger conversation
                    EncounterManager.StartPartyEncounter(MobileParty.MainParty.Party, immortalParty.Party);
                    
                    Log.Info("[Immortal Encounter] Encounter started via EncounterManager");
                }
                catch (Exception encounterEx)
                {
                    Log.Error($"[Immortal Encounter] EncounterManager failed: {encounterEx.Message}, trying PlayerEncounter");
                    
                    try
                    {
                        // Fallback method
                        PlayerEncounter.Start();
                        PlayerEncounter.Current.SetupFields(MobileParty.MainParty.Party, immortalParty.Party);
                        
                        // Force start conversation with the immortal as the conversationalist  
                        CampaignMapConversation.OpenConversation(
                            new ConversationCharacterData(CharacterObject.PlayerCharacter, MobileParty.MainParty.Party),
                            new ConversationCharacterData(immortalHero.CharacterObject, immortalParty.Party)
                        );
                        
                        Log.Info("[Immortal Encounter] Conversation opened via ConversationManager");
                    }
                    catch (Exception fallbackEx)
                    {
                        Log.Error($"[Immortal Encounter] All encounter methods failed: {fallbackEx.Message}");
                        SetPartyAiAction.GetActionForEngagingParty(immortalParty, MobileParty.MainParty);
                    }
                }
                
                Log.Info("[Immortal Encounter] Event setup completed");
            }
            catch (Exception ex)
            {
                Log.Error($"[Immortal Encounter] Exception in ExecuteEvent: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void RegisterDialogue(CampaignGameStarter starter)
        {
            // Block all default wanderer/companion dialogs when immortal encounter is active
            starter.AddDialogLine(
                "immortal_block_defaults",
                "start",
                "immortal_encounter_challenge",
                "Well, well... what do we have here? A warrior with... followers watching from beyond. How intriguing.",
                () => 
                {
                    bool isImmortalConvo = dialogueActive && 
                                          immortalHero != null && 
                                          Hero.OneToOneConversationHero == immortalHero;
                    
                    if (isImmortalConvo)
                    {
                        Log.Info("[Immortal Encounter] Dialog condition TRUE - should show custom dialog");
                    }
                    
                    return isImmortalConvo;
                },
                null,
                1000 // VERY high priority to override all other dialogs
            );

            // Immortal's challenge
            starter.AddDialogLine(
                "immortal_encounter_taunt",
                "immortal_encounter_challenge",
                "immortal_encounter_response",
                $"I am {config.ImmortalName}, and I have walked these lands for centuries. " +
                "I challenge you and all who stand with you to face me and my faithful in battle. " +
                "Should you triumph, great rewards await. Should you fall... well, you won't live to regret it. " +
                "What say you, mortal?",
                null,
                null,
                100
            );

            // Player accepts the challenge
            starter.AddPlayerLine(
                "immortal_accept",
                "immortal_encounter_response",
                "immortal_encounter_accepted",
                "I accept your challenge, demon! Prepare to meet your end!",
                null,
                () => { },
                100
            );

            // Player refuses
            starter.AddPlayerLine(
                "immortal_refuse",
                "immortal_encounter_response",
                "immortal_encounter_refused",
                "I have no interest in fighting you. Begone!",
                null,
                () => { },
                100,
                null,
                null
            );

            // Response to acceptance - leads to battle
            starter.AddDialogLine(
                "immortal_accepted_response",
                "immortal_encounter_accepted",
                "close_window",
                "Hahahaha! Excellent! Let us see if your blade is as sharp as your tongue. TO ARMS!",
                null,
                () => AcceptBattle(),
                100
            );

            // Response to refusal - immortal leaves
            starter.AddDialogLine(
                "immortal_refused_response",
                "immortal_encounter_refused",
                "close_window",
                "*The figure's eyes glow with amusement* A coward's choice... but I am patient. We shall meet again when you find your courage. *vanishes in dark smoke*",
                null,
                () => RefuseBattle(),
                100
            );
        }

        private void AcceptBattle()
        {
            Log.Info("[Immortal Encounter] Player accepted battle");
            try
            {
                // Disable summon/attack commands for this battle
                ImmortalBattleRestrictions.ImmmortalBattleActive = true;

                // Make sure encounter is set up
                if (PlayerEncounter.Current == null)
                {
                    PlayerEncounter.Start();
                    PlayerEncounter.Current.SetupFields(MobileParty.MainParty.Party, immortalParty.Party);
                }
                
                // Start the battle
                PlayerEncounter.StartBattle();
                
                // Register battle end handler
                CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnBattleEnd);
                
                dialogueActive = false;
            }
            catch (Exception ex)
            {
                Log.Error($"[Immortal Encounter] Error in AcceptBattle: {ex.Message}\n{ex.StackTrace}");
                CleanupEncounter();
            }
        }

        private void RefuseBattle()
        {
            Log.Info("[Immortal Encounter] Player refused battle");
            try
            {
                // Show message
                Log.ShowInformation(
                    $"{config.ImmortalName} vanishes into the shadows, his mocking laughter echoing across the land...",
                    immortalHero.CharacterObject,
                    Log.Sound.None
                );

                CleanupEncounter();
            }
            catch (Exception ex)
            {
                Log.Error($"[Immortal Encounter] Error in RefuseBattle: {ex.Message}\n{ex.StackTrace}");
                CleanupEncounter();
            }
        }

        private void OnBattleEnd(MapEvent mapEvent)
        {
            Log.Info("[Immortal Encounter] Battle ended");
            try
            {
                if (mapEvent == null || !mapEvent.InvolvedParties.Any(p => p.MobileParty == immortalParty))
                    return;

                ImmortalBattleRestrictions.ImmmortalBattleActive = false;

                // Check if player won
                if (mapEvent.BattleState == BattleState.AttackerVictory && mapEvent.AttackerSide.LeaderParty.MobileParty == MobileParty.MainParty)
                {
                    Log.Info("[Immortal Encounter] Player won the battle!");
                    RewardParticipants();
                }
                else
                {
                    Log.Info("[Immortal Encounter] Player lost or fled the battle");
                    Log.ShowInformation(
                        $"{config.ImmortalName} laughs as you retreat. 'Perhaps next time, mortal...'",
                        null,
                        Log.Sound.None
                    );
                }

                CleanupEncounter();
            }
            catch (Exception ex)
            {
                Log.Error($"[Immortal Encounter] Error in OnBattleEnd: {ex.Message}\n{ex.StackTrace}");
                CleanupEncounter();
            }
        }

        private void RewardParticipants()
        {
            try
            {
                var adoptedHeroCampaign = BLTAdoptAHeroCampaignBehavior.Current;
                if (adoptedHeroCampaign == null)
                {
                    Log.Error("[Immortal Encounter] Could not find BLTAdoptAHeroCampaignBehavior");
                    return;
                }

                int rewardedCount = 0;
                var participants = ImmortalBattleRestrictions.BattleParticipants;
                
                // Only reward heroes who actually participated in the battle
                foreach (var hero in participants)
                {
                    if (hero != null && hero.IsAlive)
                    {
                        adoptedHeroCampaign.ChangeHeroGold(hero, config.GoldRewardPerParticipant);
                        rewardedCount++;
                        Log.Info($"[Immortal Encounter] Rewarded {hero.Name} with {config.GoldRewardPerParticipant} gold");
                    }
                }

                if (rewardedCount > 0)
                {
                    Log.ShowInformation(
                        $"Victory! {config.ImmortalName} dissipates into smoke. 'Well fought, mortals... until we meet again.' " +
                        $"{rewardedCount} BLT participant{(rewardedCount != 1 ? "s have" : " has")} been rewarded with {config.GoldRewardPerParticipant} gold each!",
                        immortalHero?.CharacterObject,
                        Log.Sound.Horns2
                    );
                }
                else
                {
                    Log.ShowInformation(
                        $"Victory! {config.ImmortalName} dissipates into smoke. 'Well fought, mortals... until we meet again.'",
                        immortalHero?.CharacterObject,
                        Log.Sound.Horns2
                    );
                }

                Log.Info($"[Immortal Encounter] Rewarded {rewardedCount} participants (out of {participants.Count} tracked)");
            }
            catch (Exception ex)
            {
                Log.Error($"[Immortal Encounter] Error in RewardParticipants: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void CleanupEncounter()
        {
            Log.Info("[Immortal Encounter] Cleaning up encounter");
            try
            {
                dialogueActive = false;
                ImmortalBattleRestrictions.ImmmortalBattleActive = false;
                ImmortalBattleRestrictions.ClearParticipants();

                if (immortalParty != null && immortalParty.IsActive)
                {
                    DestroyPartyAction.Apply(null, immortalParty);
                }

                if (immortalHero != null && immortalHero.IsAlive)
                {
                    KillCharacterAction.ApplyByRemove(immortalHero, true);
                }

                immortalParty = null;
                immortalHero = null;

                // Unregister event handlers
                CampaignEvents.OnSessionLaunchedEvent.ClearListeners(this);
                CampaignEvents.MapEventEnded.ClearListeners(this);
            }
            catch (Exception ex)
            {
                Log.Error($"[Immortal Encounter] Error in CleanupEncounter: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private Hero CreateImmortalHero()
        {
            try
            {
                // Create a temporary clan for the immortal
                var tempClan = Clan.CreateClan($"clan_{config.CultName}");
                var culture = Hero.MainHero.Culture;
                var banner = Banner.CreateRandomBanner();
                tempClan.InitializeClan(new TextObject(config.CultName), new TextObject(config.CultName), culture, banner);

                // Create the immortal hero - try different occupations to avoid wanderer recruitment dialogs
                var template = culture.NotableAndWandererTemplates
                    .FirstOrDefault(t => !t.IsFemale && (t.Occupation == Occupation.Mercenary || t.Occupation == Occupation.Gangster))
                    ?? culture.NotableAndWandererTemplates.FirstOrDefault(t => !t.IsFemale);

                if (template == null)
                {
                    template = culture.NotableAndWandererTemplates.FirstOrDefault();
                }

                if (template == null)
                {
                    Log.Error("[Immortal Encounter] Could not find character template");
                    return null;
                }

                var immortal = HeroCreator.CreateSpecialHero(template, null, null, null, -1);
                immortal.SetName(new TextObject(config.ImmortalName), new TextObject(config.ImmortalName));
                
                // Make the immortal extremely powerful
                immortal.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.OneHanded, 300);
                immortal.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.TwoHanded, 300);
                immortal.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Polearm, 300);
                immortal.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Athletics, 300);
                immortal.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Tactics, 250);
                immortal.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Leadership, 250);

                // Give the immortal powerful equipment
                EquipImmortal(immortal);

                immortal.Clan = tempClan;
                tempClan.SetLeader(immortal);
                
                // Make absolutely sure this isn't treated as a wanderer
                immortal.CompanionOf = null;

                return immortal;
            }
            catch (Exception ex)
            {
                Log.Error($"[Immortal Encounter] Error creating immortal hero: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private void EquipImmortal(Hero immortal)
        {
            try
            {
                // Get all high-tier equipment items
                var allItems = Game.Current.ObjectManager.GetObjectTypeList<ItemObject>();
                
                // Filter for tier 5+ items (high quality)
                var weapons = allItems.Where(i => i.Type == ItemObject.ItemTypeEnum.OneHandedWeapon || 
                                                   i.Type == ItemObject.ItemTypeEnum.TwoHandedWeapon || 
                                                   i.Type == ItemObject.ItemTypeEnum.Polearm)
                                      .Where(i => i.Tier >= ItemObject.ItemTiers.Tier5)
                                      .ToList();
                                      
                var shields = allItems.Where(i => i.Type == ItemObject.ItemTypeEnum.Shield)
                                     .Where(i => i.Tier >= ItemObject.ItemTiers.Tier4)
                                     .ToList();
                                     
                var helmets = allItems.Where(i => i.Type == ItemObject.ItemTypeEnum.HeadArmor)
                                     .Where(i => i.Tier >= ItemObject.ItemTiers.Tier5)
                                     .ToList();
                                     
                var armors = allItems.Where(i => i.Type == ItemObject.ItemTypeEnum.BodyArmor)
                                    .Where(i => i.Tier >= ItemObject.ItemTiers.Tier5)
                                    .ToList();
                                    
                var boots = allItems.Where(i => i.Type == ItemObject.ItemTypeEnum.LegArmor)
                                   .Where(i => i.Tier >= ItemObject.ItemTiers.Tier4)
                                   .ToList();
                                   
                var gloves = allItems.Where(i => i.Type == ItemObject.ItemTypeEnum.HandArmor)
                                    .Where(i => i.Tier >= ItemObject.ItemTiers.Tier4)
                                    .ToList();
                                    
                var capes = allItems.Where(i => i.Type == ItemObject.ItemTypeEnum.Cape)
                                   .Where(i => i.Tier >= ItemObject.ItemTiers.Tier3)
                                   .ToList();

                // Create battle equipment
                var equipment = new Equipment();
                
                // Add random weapon(s)
                if (weapons.Any())
                {
                    var weapon = weapons.GetRandomElement();
                    equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon0, new EquipmentElement(weapon));
                    
                    // If one-handed, maybe add shield
                    if (weapon.Type == ItemObject.ItemTypeEnum.OneHandedWeapon && shields.Any() && MBRandom.RandomFloat < 0.7f)
                    {
                        equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon1, new EquipmentElement(shields.GetRandomElement()));
                    }
                    else if (weapons.Any() && MBRandom.RandomFloat < 0.3f)
                    {
                        // Add a second weapon
                        equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon1, new EquipmentElement(weapons.GetRandomElement()));
                    }
                }
                
                // Add armor pieces
                if (helmets.Any())
                    equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Head, new EquipmentElement(helmets.GetRandomElement()));
                if (armors.Any())
                    equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Body, new EquipmentElement(armors.GetRandomElement()));
                if (boots.Any())
                    equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Leg, new EquipmentElement(boots.GetRandomElement()));
                if (gloves.Any())
                    equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Gloves, new EquipmentElement(gloves.GetRandomElement()));
                if (capes.Any())
                    equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Cape, new EquipmentElement(capes.GetRandomElement()));

                // Set both battle and civilian equipment to the same gear
                immortal.BattleEquipment.FillFrom(equipment);
                immortal.CivilianEquipment.FillFrom(equipment);
                
                Log.Info($"[Immortal Encounter] Equipped {config.ImmortalName} with high-tier gear");
            }
            catch (Exception ex)
            {
                Log.Error($"[Immortal Encounter] Error equipping immortal: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private MobileParty CreateCultArmy(Hero leader)
        {
            try
            {
                // Calculate army size based on player's party strength
                int playerPartySize = MobileParty.MainParty.MemberRoster.TotalManCount;
                int targetArmySize = (int)((playerPartySize * config.ArmySizePercent) / 100f);
                targetArmySize = Math.Max(50, Math.Min(targetArmySize, 500)); // Clamp between 50 and 500

                // Create the party
                var party = leader.Clan.CreateNewMobileParty(leader);
                Log.Info($"[Immortal Encounter] Created mobile party for {leader.Name}");

                // Get all high-tier troops from the game (tier 5-6 = elite troops)
                // IMPORTANT: Only select troops with valid equipment to avoid invisible gear
                var culture = leader.Culture;
                var allTroops = CharacterObject.All.Where(c => 
                    c.IsHero == false && 
                    c.Occupation == Occupation.Soldier &&
                    c.Culture == culture &&
                    c.Tier >= 5 && // Tier 5-6 troops (elite)
                    c.BattleEquipments != null && c.BattleEquipments.Any() && // Must have equipment
                    c.BattleEquipments.Any(eq => eq.Horse.Item == null) // Exclude cavalry to avoid horse equipment issues
                ).ToList();

                // Fallback to any culture if player culture has no high tier troops
                if (!allTroops.Any())
                {
                    allTroops = CharacterObject.All.Where(c => 
                        c.IsHero == false && 
                        c.Occupation == Occupation.Soldier &&
                        c.Tier >= 5 &&
                        c.BattleEquipments != null && c.BattleEquipments.Any() &&
                        c.BattleEquipments.Any(eq => eq.Horse.Item == null)
                    ).ToList();
                }

                // Further fallback to tier 4+ if still nothing
                if (!allTroops.Any())
                {
                    allTroops = CharacterObject.All.Where(c => 
                        c.IsHero == false && 
                        c.Occupation == Occupation.Soldier &&
                        c.Culture == culture &&
                        c.Tier >= 4 &&
                        c.BattleEquipments != null && c.BattleEquipments.Any() &&
                        c.BattleEquipments.Any(eq => eq.Horse.Item == null)
                    ).ToList();
                }

                if (allTroops.Any())
                {
                    // Add variety - different types of elite troops
                    int remainingTroops = targetArmySize;
                    int troopTypesCount = Math.Min(4, allTroops.Count); // Use up to 4 different troop types
                    
                    for (int i = 0; i < troopTypesCount && remainingTroops > 0; i++)
                    {
                        var troopType = allTroops[MBRandom.RandomInt(allTroops.Count)];
                        int count = remainingTroops / (troopTypesCount - i);
                        party.MemberRoster.AddToCounts(troopType, count);
                        remainingTroops -= count;
                        
                        Log.Info($"[Immortal Encounter] Added {count}x {troopType.Name} (Tier {troopType.Tier})");
                    }
                }
                else
                {
                    // Ultimate fallback - use basic troops
                    Log.Info("[Immortal Encounter] No high-tier troops found, using basic troops");
                    var basicTroop = culture.BasicTroop;
                    if (basicTroop != null)
                    {
                        party.MemberRoster.AddToCounts(basicTroop, targetArmySize);
                    }
                }

                // Set party properties
                party.Ai.SetDoNotMakeNewDecisions(true); // Prevent the party from wandering
                
                Log.Info($"[Immortal Encounter] Created army with {party.MemberRoster.TotalManCount} troops");

                return party;
            }
            catch (Exception ex)
            {
                Log.Error($"[Immortal Encounter] Error creating cult army: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }
    }

    /// <summary>
    /// Static class to track Immortal battle state for restricting enemy-side joins
    /// </summary>
    public static class ImmortalBattleRestrictions
    {
        public static bool ImmmortalBattleActive { get; set; } = false;
        
        /// <summary>
        /// Tracks which BLT heroes have actually participated in the Immortal battle
        /// </summary>
        public static HashSet<Hero> BattleParticipants { get; set; } = new HashSet<Hero>();
        
        /// <summary>
        /// Registers a hero as having participated in the Immortal battle
        /// </summary>
        public static void RegisterParticipant(Hero hero)
        {
            if (ImmmortalBattleActive && hero != null)
            {
                BattleParticipants.Add(hero);
                Log.Info($"[Immortal Encounter] Registered battle participant: {hero.Name}");
            }
        }
        
        /// <summary>
        /// Clears the participant list (called when battle ends or encounter is cleaned up)
        /// </summary>
        public static void ClearParticipants()
        {
            BattleParticipants.Clear();
        }
    }
}
