﻿using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BLTAdoptAHero.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    internal abstract class ImproveAdoptedHero : ActionHandlerBase
    {
        protected class SettingsBase
        {
            [LocDisplayName("{=01yarGvZ}Amount Low"),
             LocDescription("{=UVKWPBjq}Lower bound of amount to improve"),
             PropertyOrder(1), UsedImplicitly]
            public int AmountLow { get; set; }
            [LocDisplayName("{=qi7FVuQB}Amount High"),
             LocDescription("{=tU4rSLcx}Upper bound of amount to improve"),
             PropertyOrder(2), UsedImplicitly]
            public int AmountHigh { get; set; }
            [LocDisplayName("{=DlZ7sV1N}Gold Cost"),
             LocDescription("{=pGwh9edB}Gold that will be taken from the hero"),
             PropertyOrder(3), UsedImplicitly]
            public int GoldCost { get; set; }
        }

        // protected override Type ConfigType => typeof(SettingsBase);

        protected override void ExecuteInternal(ReplyContext context, object config,
            Action<string> onSuccess,
            Action<string> onFailure)
        {
            var settings = (SettingsBase)config;
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                onFailure(AdoptAHero.NoHeroMessage);
                return;
            }

            int availableGold = BLTAdoptAHeroCampaignBehavior.Current.GetHeroGold(adoptedHero);
            if (availableGold < settings.GoldCost)
            {
                onFailure(Naming.NotEnoughGold(settings.GoldCost, availableGold));
                return;
            }

            int amount = MBRandom.RandomInt(settings.AmountLow, settings.AmountHigh);
            (bool success, string description) = Improve(context.UserName, adoptedHero, amount, settings, context.Args);
            if (success)
            {
                onSuccess(description);
                BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(adoptedHero, -settings.GoldCost);
            }
            else
            {
                onFailure(description);
            }
        }

        protected abstract (bool success, string description) Improve(string userName, Hero adoptedHero, int amount, SettingsBase settings, string args);

        private static string[] EnumFlagsToArray<T>(T flags) =>
            flags.ToString().Split(',').Select(s => s.Trim()).ToArray();
        private static (string[] skillNames, float weight)[] SkillGroups =
        {
            (skillNames: SkillGroup.SkillsToStrings(SkillsEnum.Melee), weight: 3f),
            (skillNames: SkillGroup.SkillsToStrings(SkillsEnum.Ranged), weight: 2f),
            (skillNames: SkillGroup.SkillsToStrings(SkillsEnum.Support), weight: 1f),
            (skillNames: SkillGroup.SkillsToStrings(SkillsEnum.Movement), weight: 2f),
            (skillNames: SkillGroup.SkillsToStrings(SkillsEnum.Personal), weight: 1f),
        };

        protected static SkillObject GetSkill(Hero hero, SkillsEnum skills, bool auto, Func<SkillObject, bool> predicate = null)
        {
            predicate ??= s => true;
            var selectedSkills = new List<(SkillObject skill, float weight)>();
            if (auto)
            {
                // Select skill to improve:
                // Class skills         weight x 5
                var heroClass = BLTAdoptAHeroCampaignBehavior.Current.GetClass(hero);
                if (heroClass != null)
                {
                    selectedSkills.AddRange(heroClass.Skills.Select(skill => (skill, weight: 15f)));
                }

                // Equipment skills     weight x 2
                selectedSkills.AddRange(hero.BattleEquipment
                    .YieldFilledWeaponSlots()
                    .SelectMany(s => s.element.Item.Weapons?.Select(w => w.RelevantSkill) ?? Enumerable.Empty<SkillObject>())
                    .Distinct()
                    .Except(selectedSkills.Select(s => s.skill))
                    .Select(skill => (skill, weight: 4f))
                );

                // Other skills         weight x 1
                selectedSkills.AddRange(CampaignHelpers.AllSkillObjects
                    .Except(selectedSkills.Select(s => s.skill))
                    .Select(skill => (skill, weight: 1f))
                );
            }
            else
            {
                selectedSkills.AddRange(SkillGroup.GetSkills(SkillGroup.SkillsToStrings(skills))
                    .Select(skill => (skill, weight: 1f)));
            }

            return selectedSkills
                .Where(o => predicate(o.skill))
                .SelectRandomWeighted(o => o.weight)
                .skill;
        }
    }
}