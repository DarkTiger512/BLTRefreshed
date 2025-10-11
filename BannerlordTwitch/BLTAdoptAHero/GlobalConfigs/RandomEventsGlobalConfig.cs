using System.Collections.Generic;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BLTAdoptAHero.Annotations;
using BLTAdoptAHero.Events;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.GlobalConfigs
{
    [LocDisplayName("{=BLT_Events_Name}Random Events Configuration"),
     LocDescription("{=BLT_Events_Desc}Configure random world events that can occur during gameplay"),
     UsedImplicitly]
    public class RandomEventsGlobalConfig : IDocumentable
    {
        #region Static
        private const string ID = "BLT Random Events Config";

        internal static void Register() => ActionManager.RegisterGlobalConfigType(ID, typeof(RandomEventsGlobalConfig));
        internal static RandomEventsGlobalConfig Get() => ActionManager.GetGlobalConfig<RandomEventsGlobalConfig>(ID);
        internal static RandomEventsGlobalConfig Get(BannerlordTwitch.Settings fromSettings) => fromSettings.GetGlobalConfig<RandomEventsGlobalConfig>(ID);
        #endregion

        [LocDisplayName("{=BLT_Events_Enabled}Enable Random Events"),
         LocDescription("{=BLT_Events_EnabledDesc}Master toggle for the random events system"),
         PropertyOrder(1), UsedImplicitly]
        public bool EnableRandomEvents { get; set; } = true;

        [LocDisplayName("{=BLT_Events_GlobalMultiplier}Global Trigger Chance Multiplier"),
         LocDescription("{=BLT_Events_GlobalMultiplierDesc}Multiply all event chances by this value (0.1 = 10%, 1.0 = 100%, 2.0 = 200%)"),
         PropertyOrder(2), UsedImplicitly]
        public float GlobalChanceMultiplier { get; set; } = 1.0f;

        [LocDisplayName("{=BLT_Events_PriestCrusade}Priest's Crusade Event"),
         LocDescription("{=BLT_Events_PriestCrusadeDesc}A fanatical priest declares holy war on your kingdom"),
         PropertyOrder(10), UsedImplicitly, ExpandableObject]
        public PriestCrusadeEvent.Settings PriestCrusadeSettings { get; set; } = new PriestCrusadeEvent.Settings();

        [LocDisplayName("{=BLT_Events_ImmortalEncounter}The Immortal Encounter Event"),
         LocDescription("{=BLT_Events_ImmortalEncounterDesc}A mysterious immortal being challenges you to mortal combat"),
         PropertyOrder(11), UsedImplicitly, ExpandableObject]
        public ImmortalEncounterEvent.Settings ImmortalEncounterSettings { get; set; } = new ImmortalEncounterEvent.Settings();

        [LocDisplayName("{=BLT_Events_CursedArtifact}The Cursed Artifact Event"),
         LocDescription("{=BLT_Events_CursedArtifactDesc}A hero becomes cursed and must break the curse to gain a legendary weapon"),
         PropertyOrder(12), UsedImplicitly, ExpandableObject]
        public CursedArtifactSettings CursedArtifactSettings { get; set; } = new CursedArtifactSettings();

        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            generator.PropertyValuePair("Random Events Enabled", EnableRandomEvents ? "Yes" : "No");
            generator.PropertyValuePair("Global Chance Multiplier", $"{GlobalChanceMultiplier:F2}x");
            
            if (PriestCrusadeSettings.Enabled)
            {
                generator.H2("Priest's Crusade Event");
                generator.PropertyValuePair("Trigger Chance", $"{PriestCrusadeSettings.TriggerChance * 100:F2}% per day");
                generator.PropertyValuePair("Cooldown", $"{PriestCrusadeSettings.CooldownDays} days");
                generator.PropertyValuePair("Army Size", $"{PriestCrusadeSettings.ArmySizePercent}% of player clan strength");
                generator.PropertyValuePair("Minimum Kingdom Tier", PriestCrusadeSettings.MinimumKingdomTier.ToString());
            }

            if (ImmortalEncounterSettings.Enabled)
            {
                generator.H2("The Immortal Encounter Event");
                generator.PropertyValuePair("Trigger Chance", $"{ImmortalEncounterSettings.TriggerChance * 100:F2}% per day");
                generator.PropertyValuePair("Cooldown", $"{ImmortalEncounterSettings.CooldownDays} days");
                generator.PropertyValuePair("Army Size", $"{ImmortalEncounterSettings.ArmySizePercent}% of player clan strength");
                generator.PropertyValuePair("Victory Gold Reward", $"{ImmortalEncounterSettings.GoldRewardPerParticipant} gold per participant");
                generator.PropertyValuePair("Minimum Player Level", ImmortalEncounterSettings.MinimumPlayerLevel.ToString());
            }

            if (CursedArtifactSettings != null)
            {
                generator.H2("The Cursed Artifact Event");
                generator.PropertyValuePair("Trigger Chance", $"{CursedArtifactSettings.TriggerChancePerDay:F2}% per day");
                generator.PropertyValuePair("Gold Drain", $"{CursedArtifactSettings.GoldDrainPerDay} per day");
                generator.PropertyValuePair("XP Drain", $"{CursedArtifactSettings.XPDrainPerDay} per day");
                generator.PropertyValuePair("Damage Dealt", $"{CursedArtifactSettings.DamageDealtPercent}%");
                generator.PropertyValuePair("Damage Taken", $"{CursedArtifactSettings.DamageTakenPercent}%");
                generator.PropertyValuePair("Gold to Break", $"{CursedArtifactSettings.GoldToBreakCurse} gold");
                generator.PropertyValuePair("Battles to Break", $"{CursedArtifactSettings.BattlesToWin} battles");
                generator.PropertyValuePair("Weapon Vanish Chance", $"{CursedArtifactSettings.WeaponVanishChance}%");
            }
        }
    }
}
