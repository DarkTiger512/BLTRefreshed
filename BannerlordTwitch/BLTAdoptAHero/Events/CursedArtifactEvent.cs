using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using BLTAdoptAHero.Powers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Events
{
    [CategoryOrder("Trigger", 1)]
    [CategoryOrder("Curse", 2)]
    [CategoryOrder("Breaking", 3)]
    [CategoryOrder("Reward", 4)]
    public class CursedArtifactSettings
    {
        [LocDisplayName("{=CursedArtifact_TriggerChance}Trigger Chance"),
         LocDescription("{=CursedArtifact_TriggerChanceDesc}Chance per day that a hero becomes cursed (0-100)"),
         PropertyOrder(1), LocCategory("Trigger", "{=RandomEvent_Trigger}Trigger"), UsedImplicitly]
        public float TriggerChancePerDay { get; set; } = 0.5f;

        [LocDisplayName("{=CursedArtifact_Cooldown}Cooldown Days"),
         LocDescription("{=CursedArtifact_CooldownDesc}Minimum days between curse events"),
         PropertyOrder(2), LocCategory("Trigger", "{=RandomEvent_Trigger}Trigger"), UsedImplicitly]
        public int CooldownDays { get; set; } = 30;

        // Curse Effects
        [LocDisplayName("{=CursedArtifact_GoldDrain}Gold Drain Per Day"),
         LocDescription("{=CursedArtifact_GoldDrainDesc}How much gold is drained from player per day (0 to disable)"),
         PropertyOrder(1), LocCategory("Curse", "{=CursedArtifact_Curse}Curse"), UsedImplicitly]
        public int GoldDrainPerDay { get; set; } = 1000;

        [LocDisplayName("{=CursedArtifact_XPDrain}XP Drain Per Day"),
         LocDescription("{=CursedArtifact_XPDrainDesc}How much XP is drained from cursed hero per day (0 to disable)"),
         PropertyOrder(2), LocCategory("Curse", "{=CursedArtifact_Curse}Curse"), UsedImplicitly]
        public int XPDrainPerDay { get; set; } = 500;

        [LocDisplayName("{=CursedArtifact_DamageDealtPenalty}Damage Dealt Penalty %"),
         LocDescription("{=CursedArtifact_DamageDealtPenaltyDesc}Cursed hero deals this % of normal damage (50 = half damage)"),
         PropertyOrder(3), LocCategory("Curse", "{=CursedArtifact_Curse}Curse"), UsedImplicitly]
        public float DamageDealtPercent { get; set; } = 50f;

        [LocDisplayName("{=CursedArtifact_DamageTakenPenalty}Damage Taken Penalty %"),
         LocDescription("{=CursedArtifact_DamageTakenPenaltyDesc}Cursed hero takes this % of normal damage (150 = 50% more damage)"),
         PropertyOrder(4), LocCategory("Curse", "{=CursedArtifact_Curse}Curse"), UsedImplicitly]
        public float DamageTakenPercent { get; set; } = 150f;

        // Breaking Conditions
        [LocDisplayName("{=CursedArtifact_GoldToPay}Gold To Break Curse"),
         LocDescription("{=CursedArtifact_GoldToPayDesc}Gold required to break the curse instantly"),
         PropertyOrder(1), LocCategory("Breaking", "{=CursedArtifact_Breaking}Breaking"), UsedImplicitly]
        public int GoldToBreakCurse { get; set; } = 100000;

        [LocDisplayName("{=CursedArtifact_BattlesToWin}Battles To Win"),
         LocDescription("{=CursedArtifact_BattlesToWinDesc}Number of battles cursed hero must participate in to break curse"),
         PropertyOrder(2), LocCategory("Breaking", "{=CursedArtifact_Breaking}Breaking"), UsedImplicitly]
        public int BattlesToWin { get; set; } = 10;

        [LocDisplayName("{=CursedArtifact_VanishChance}Weapon Vanish Chance %"),
         LocDescription("{=CursedArtifact_VanishChanceDesc}Chance weapon vanishes when curse breaks (0-100)"),
         PropertyOrder(3), LocCategory("Breaking", "{=CursedArtifact_Breaking}Breaking"), UsedImplicitly]
        public float WeaponVanishChance { get; set; } = 10f;

        // Rewards
        [LocDisplayName("{=CursedArtifact_WeaponSkillBonus}Weapon Skill Bonus"),
         LocDescription("{=CursedArtifact_WeaponSkillBonusDesc}Skill increase for weapon type when curse is broken"),
         PropertyOrder(1), LocCategory("Reward", "{=CursedArtifact_Reward}Reward"), UsedImplicitly]
        public int WeaponSkillBonus { get; set; } = 100;

        [LocDisplayName("{=CursedArtifact_WeaponDamageBonus}Weapon Damage Bonus %"),
         LocDescription("{=CursedArtifact_WeaponDamageBonusDesc}Permanent damage bonus on legendary weapon (150 = 50% more damage)"),
         PropertyOrder(2), LocCategory("Reward", "{=CursedArtifact_Reward}Reward"), UsedImplicitly]
        public float WeaponDamageBonus { get; set; } = 150f;

        [LocDisplayName("{=CursedArtifact_AttributeBonus}Attribute Bonus"),
         LocDescription("{=CursedArtifact_AttributeBonusDesc}All attributes increased by this amount"),
         PropertyOrder(3), LocCategory("Reward", "{=CursedArtifact_Reward}Reward"), UsedImplicitly]
        public int AttributeBonus { get; set; } = 3;
    }

    [LocDisplayName("{=BLT_CA_Name}The Cursed Artifact"),
     LocDescription("{=BLT_CA_Desc}A hero becomes cursed and must break the curse to gain a legendary weapon"),
     UsedImplicitly]
    public class CursedArtifactEvent : RandomEventBase
    {
        private readonly CursedArtifactSettings config;

        public CursedArtifactEvent(CursedArtifactSettings settings)
        {
            config = settings;
            IsEnabled = true;
            TriggerChancePerDay = settings.TriggerChancePerDay / 100f; // Convert to 0-1 range
            CooldownDays = settings.CooldownDays;
        }

        public override string EventId => "cursed_artifact";
        public override string EventName => "The Cursed Artifact";
        public override string EventDescription => "A hero becomes cursed with debuffs, but can gain legendary rewards by breaking it";

        protected override bool CheckSpecificConditions()
        {
            // Must have at least one hero
            return BLTAdoptAHeroCampaignBehavior.GetAllAdoptedHeroes()?.Any() == true;
        }

        protected override void ExecuteEvent()
        {
            Log.Info("[Cursed Artifact Event] ExecuteEvent called");
            
            // Find eligible heroes
            var eligibleHeroes = BLTAdoptAHeroCampaignBehavior.GetAllAdoptedHeroes()
                .ToList();

            if (!eligibleHeroes.Any())
            {
                Log.Info("[Cursed Artifact Event] No eligible heroes found");
                return;
            }

            var victim = eligibleHeroes.SelectRandom();

            Log.Info($"[Cursed Artifact Event] {victim.Name} has been cursed!");
            InformationManager.DisplayMessage(new InformationMessage(
                $"{victim.Name} has been cursed by a mysterious artifact! They will be weakened in battle until the curse is lifted.",
                Colors.Red));
        }
    }
}
