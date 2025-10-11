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
        [LocDisplayName("{=CursedArtifact_BattlesToWin}Battles To Win"),
         LocDescription("{=CursedArtifact_BattlesToWinDesc}Number of battles cursed hero must participate in to break curse"),
         PropertyOrder(1), LocCategory("Breaking", "{=CursedArtifact_Breaking}Breaking"), UsedImplicitly]
        public int BattlesToWin { get; set; } = 10;

        [LocDisplayName("{=CursedArtifact_VanishChance}Weapon Vanish Chance %"),
         LocDescription("{=CursedArtifact_VanishChanceDesc}Chance weapon vanishes when curse breaks (0-100)"),
         PropertyOrder(2), LocCategory("Breaking", "{=CursedArtifact_Breaking}Breaking"), UsedImplicitly]
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
        private static CursedArtifactSettings staticConfig; // Store for static methods
        
        // Track cursed heroes: Hero -> (battles completed, curse powers)
        private static readonly Dictionary<Hero, CurseData> cursedHeroes = new();

        private class CurseData
        {
            public int BattlesCompleted { get; set; }
            public AddDamagePower DamageDealtPower { get; set; }
            public TakeDamagePower DamageTakenPower { get; set; }
        }

        public CursedArtifactEvent(CursedArtifactSettings settings)
        {
            config = settings;
            staticConfig = settings; // Store for static access
            IsEnabled = true;
            TriggerChancePerDay = settings.TriggerChancePerDay / 100f; // Convert to 0-1 range
            CooldownDays = settings.CooldownDays;
        }

        public override string EventId => "cursed_artifact";
        public override string EventName => "The Cursed Artifact";
        public override string EventDescription => "A hero becomes cursed with debuffs, but can gain legendary rewards by breaking it";

        public static bool IsHeroCursed(Hero hero) => cursedHeroes.ContainsKey(hero);
        
        public static int GetCurseBattleProgress(Hero hero) => 
            cursedHeroes.TryGetValue(hero, out var data) ? data.BattlesCompleted : 0;

        public static void ApplyCursePowersInBattle(Hero hero, PowerHandler powerHandler)
        {
            if (!cursedHeroes.TryGetValue(hero, out var curseData))
                return;

            // Apply damage dealt debuff
            ((IHeroPowerPassive)curseData.DamageDealtPower).OnHeroJoinedBattle(hero, null);

            // Apply damage taken debuff
            ((IHeroPowerPassive)curseData.DamageTakenPower).OnHeroJoinedBattle(hero, null);

            Log.Info($"[Cursed Artifact] Applied curse powers to {hero.Name} in battle");
        }

        protected override bool CheckSpecificConditions()
        {
            // Must have at least one hero who isn't already cursed
            return BLTAdoptAHeroCampaignBehavior.GetAllAdoptedHeroes()
                ?.Any(h => !IsHeroCursed(h)) == true;
        }

        protected override void ExecuteEvent()
        {
            Log.Info("[Cursed Artifact Event] ExecuteEvent called");
            
            // Find eligible heroes (not already cursed)
            var eligibleHeroes = BLTAdoptAHeroCampaignBehavior.GetAllAdoptedHeroes()
                .Where(h => !IsHeroCursed(h))
                .ToList();

            if (!eligibleHeroes.Any())
            {
                Log.Info("[Cursed Artifact Event] No eligible heroes found");
                return;
            }

            var victim = eligibleHeroes.SelectRandom();

            // Apply curse
            ApplyCurse(victim);

            Log.Info($"[Cursed Artifact Event] {victim.Name} has been cursed!");
            InformationManager.DisplayMessage(new InformationMessage(
                $"{victim.Name} has been cursed by a mysterious artifact! They deal {config.DamageDealtPercent}% damage and take {config.DamageTakenPercent}% damage. Win {config.BattlesToWin} battles to break the curse!",
                Colors.Red));
        }

        private void ApplyCurse(Hero hero)
        {
            // Create damage modifier powers as temporary runtime powers
            // These will be removed when the curse breaks
            var damageDealtPower = new AddDamagePower
            {
                DamageModifierPercent = config.DamageDealtPercent,
            };

            var damageTakenPower = new TakeDamagePower
            {
                DamageModifierPercent = config.DamageTakenPercent,
            };

            // Track the curse (we don't actually add to passive powers, we track them separately)
            cursedHeroes[hero] = new CurseData
            {
                BattlesCompleted = 0,
                DamageDealtPower = damageDealtPower,
                DamageTakenPower = damageTakenPower
            };

            // Apply the powers in battle via the power handler
            // This will be done automatically when the hero joins battle

            Log.Info($"[Cursed Artifact] Applied curse to {hero.Name}");
        }

        public static void OnBattleCompleted(Hero hero)
        {
            if (!cursedHeroes.TryGetValue(hero, out var curseData))
                return;

            curseData.BattlesCompleted++;
            
            int battlesNeeded = staticConfig?.BattlesToWin ?? 10;
            
            Log.Info($"[Cursed Artifact] {hero.Name} completed battle {curseData.BattlesCompleted}/{battlesNeeded}");

            if (curseData.BattlesCompleted >= battlesNeeded)
            {
                BreakCurse(hero);
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{hero.Name} completed a battle while cursed! ({curseData.BattlesCompleted}/{battlesNeeded})",
                    Colors.Yellow));
            }
        }

        private static void BreakCurse(Hero hero)
        {
            if (!cursedHeroes.TryGetValue(hero, out var curseData))
                return;

            var config = staticConfig ?? new CursedArtifactSettings();

            // Remove from tracking (this automatically removes curse powers since we check IsHeroCursed)
            cursedHeroes.Remove(hero);

            // Check if weapon vanishes
            bool weaponVanished = MBRandom.RandomFloat * 100f < config.WeaponVanishChance;

            if (weaponVanished)
            {
                // Weapon vanishes - no reward
                InformationManager.DisplayMessage(new InformationMessage(
                    $"The curse on {hero.Name} is broken! But the cursed artifact vanished in a burst of dark energy...",
                    Colors.Green));
                Log.Info($"[Cursed Artifact] Curse broken for {hero.Name}, weapon vanished");
            }
            else
            {
                // Give legendary weapon and bonuses
                GiveRewards(hero, config);
                InformationManager.DisplayMessage(new InformationMessage(
                    $"The curse on {hero.Name} is broken! The artifact transforms into a legendary weapon! +{config.AttributeBonus} to all attributes, +{config.WeaponSkillBonus} weapon skill!",
                    Colors.Green));
                Log.Info($"[Cursed Artifact] Curse broken for {hero.Name}, rewards granted");
            }
        }

        private static void GiveRewards(Hero hero, CursedArtifactSettings config)
        {
            // Create legendary tier 6 weapon with damage bonus
            var heroClass = hero.GetClass();
            var modifierDef = new RandomItemModifierDef 
            { 
                Power = config.WeaponDamageBonus / 100f,  // Convert 150% to 1.5x multiplier
                WeaponDamage = new RangeInt(50, 75),       // Higher damage range for legendary
                WeaponSpeed = new RangeInt(40, 60)         // Higher speed range for legendary
            };

            var (weapon, modifier, slot) = RewardHelpers.GenerateRewardType(
                RewardHelpers.RewardType.Weapon,
                tier: 6,  // Tier 6 = legendary
                hero: hero,
                heroClass: heroClass,
                allowDuplicates: false,
                modifierDef: modifierDef,
                customItemName: "Cursed Legacy",  // Legendary name for the transformed artifact
                customItemPower: config.WeaponDamageBonus / 100f
            );

            if (weapon != null)
            {
                string assignResult = RewardHelpers.AssignCustomReward(hero, weapon, modifier, slot);
                Log.Info($"[Cursed Artifact] Legendary weapon created for {hero.Name}: {assignResult}");
            }
            else
            {
                Log.Error($"[Cursed Artifact] Failed to create legendary weapon for {hero.Name}, class: {heroClass?.Name}");
            }
            
            // Increase all attributes
            hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, config.AttributeBonus, checkUnspentPoints: false);
            hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, config.AttributeBonus, checkUnspentPoints: false);
            hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, config.AttributeBonus, checkUnspentPoints: false);
            hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Cunning, config.AttributeBonus, checkUnspentPoints: false);
            hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Social, config.AttributeBonus, checkUnspentPoints: false);
            hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Intelligence, config.AttributeBonus, checkUnspentPoints: false);

            // Increase weapon skills
            if (heroClass?.WeaponSkills != null)
            {
                foreach (var skill in heroClass.WeaponSkills)
                {
                    hero.HeroDeveloper.AddSkillXp(skill, config.WeaponSkillBonus);
                }
            }

            Log.Info($"[Cursed Artifact] Full rewards granted to {hero.Name}: legendary weapon, +{config.AttributeBonus} attributes, +{config.WeaponSkillBonus} weapon skill");
        }
    }
}
