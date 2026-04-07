using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BannerlordTwitch.Annotations;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.UI;
using TaleWorlds.Library;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    internal partial class GlobalCommonConfig
    {
        #region User Editable
        #region General
        [LocDisplayName("{=xwcKN7sH}Sub Boost"),
         LocCategory("General", "{=C5T5nnix}General"),
         LocDescription("{=rX68wbfF}Multiplier applied to all rewards for subscribers (less or equal to 1 means no boost). NOTE: This is only partially implemented, it works for bot commands only currently."),
         PropertyOrder(1), Document, UsedImplicitly,
         Range(0.5, 10), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor))]
        public float SubBoost { get; set; } = 1;

        [LocDisplayName("{=O0LU5WBa}Custom Reward Modifiers"),
         LocCategory("General", "{=C5T5nnix}General"),
         LocDescription("{=tp3YdGmo}The specification for custom item rewards, applies to tournament prize and achievement rewards"),
         PropertyOrder(2), ExpandableObject, UsedImplicitly]
        public RandomItemModifierDef CustomRewardModifiers { get; set; } = new();

        [LocDisplayName("{=}Custom Inventory Item Limit"),
         LocCategory("General", "{=C5T5nnix}General"),
         LocDescription("{=}Maximum custom inventory items allowed. This only applies when smithing, other rewards will always be added to inventory (but they will contribute to the limit). If you set this high then inventory management and console spam may become a problem."),
         PropertyOrder(3), UsedImplicitly]
        public int CustomItemLimit { get; set; } = 8;

        [LocDisplayName("{=}Custom Companion Limit"),
         LocCategory("General", "{=C5T6nnix}General"),
         LocDescription("{=}Flat number increase to companion limit"),
         PropertyOrder(4), UsedImplicitly]
        public int CustomCompanionLimit { get; set; } = 7;
        #endregion

        #region Battle
        [LocDisplayName("{=X8r0C5fx}Start With Full Health"),
         LocCategory("Battle", "{=9qAD6eZR}Battle"),
         LocDescription("{=HbNVrZuv}Whether the hero will always start with full health"),
         PropertyOrder(1), Document, UsedImplicitly]
        public bool StartWithFullHealth { get; set; } = true;

        [LocDisplayName("{=fxZIKL65}Start Health Multiplier"),
         LocCategory("Battle", "{=9qAD6eZR}Battle"),
         LocDescription("{=8yNIRS9S}Amount to multiply normal starting health by, to give heroes better staying power vs others"),
         PropertyOrder(2),
         Range(0.1, 10), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         Document, UsedImplicitly]
        public float StartHealthMultiplier { get; set; } = 2;

        [LocDisplayName("{=HvcTekVk}Start Retinue Health Multiplier"),
         LocCategory("Battle", "{=9qAD6eZR}Battle"),
         LocDescription("{=G6JJT2ot}Amount to multiply normal retinue starting health by, to give retinue better staying power vs others"),
         PropertyOrder(3),
         Range(0.1, 10), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         Document, UsedImplicitly]
        public float StartRetinueHealthMultiplier { get; set; } = 2;

        [LocDisplayName("{=ZPmBe7XI}Morale Loss Factor (not implemented)"),
         LocCategory("Battle", "{=9qAD6eZR}Battle"),
         LocDescription("{=tpgJtS5q}Reduces morale loss when summoned heroes die"),
         PropertyOrder(4),
         Range(0, 2), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         Document, UsedImplicitly]
        public float MoraleLossFactor { get; set; } = 0.5f;

        [LocDisplayName("{=bXdC2trk}Retinue Use Heroes Formation"),
         LocCategory("Battle", "{=9qAD6eZR}Battle"),
         LocDescription("{=D8uDzXlV}Whether an adopted heroes retinue should spawn in the same formation as the hero (otherwise they will go into default formations)"),
         PropertyOrder(13), Document, UsedImplicitly]
        public bool RetinueUseHeroesFormation { get; set; }

        [LocDisplayName("{=OlJrCEyE}Summon Cooldown In Seconds"),
         LocCategory("Battle", "{=9qAD6eZR}Battle"),
         LocDescription("{=DeGB2BGZ}Minimum time between summons for a specific hero"),
         PropertyOrder(7),
         Range(0, int.MaxValue),
         Document, UsedImplicitly]
        public int SummonCooldownInSeconds { get; set; } = 20;

        [Browsable(false), YamlIgnore]
        public bool CooldownEnabled => SummonCooldownInSeconds > 0;

        [LocDisplayName("{=f9HVD2cC}Summon Cooldown Use Multiplier"),
         LocCategory("Battle", "{=9qAD6eZR}Battle"),
         LocDescription("{=4gZlfHzM}How much to multiply the cooldown by each time summon is used. e.g. if Summon Cooldown is 20 seconds, and UseMultiplier is 1.1 (the default), then the first summon has a cooldown of 20 seconds, and the next 24 seconds, the 10th 52 seconds, and the 20th 135 seconds. See https://www.desmos.com/calculator/muej1o5eg5 for a visualization of this."),
         PropertyOrder(6),
         Range(1, 10), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         Document, UsedImplicitly]
        public float SummonCooldownUseMultiplier { get; set; } = 1.1f;

        [LocDisplayName("{=ViLoy0k3}Summon Cooldown Example"),
         LocCategory("Battle", "{=9qAD6eZR}Battle"),
         LocDescription("{=xZoSFrAb}Shows the consecutive cooldowns (in seconds) for 10 summons"),
         PropertyOrder(7), YamlIgnore, ReadOnly(true), UsedImplicitly]
        public string SummonCooldownExample => string.Join(", ",
            Enumerable.Range(1, 10)
                .Select(i => $"{i}: {GetCooldownTime(i):0}s"));
        #endregion

        #region Death
        [LocDisplayName("{=4sNJRQyw}Allow Death"),
         LocCategory("Death", "{=dbU7WEKG}Death"),
         LocDescription("{=VbBUYOfc}Whether an adopted hero is allowed to die"),
         PropertyOrder(1), Document, UsedImplicitly]
        public bool AllowDeath { get; set; }

        [Browsable(false), UsedImplicitly]
        public float DeathChance { get; set; } = 0.1f;

        [LocDisplayName("{=ZEfAPyOm}Final Death Chance Percent"),
         LocCategory("Death", "{=dbU7WEKG}Death"),
         LocDescription("{=xlt1pNuT}Final death chance percent (includes vanilla chance)"),
         PropertyOrder(2),
         Range(0, 10), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         YamlIgnore, Document, UsedImplicitly]
        public float FinalDeathChancePercent
        {
            get => DeathChance * 10f;
            set => DeathChance = value * 0.1f;
        }

        [LocDisplayName("{=sbc5Fp4o}Apply Death Chance To All Heroes"),
         LocCategory("Death", "{=dbU7WEKG}Death"),
         LocDescription("{=nbR7NLNz}Whether to apply the Death Chance changes to all heroes, not just adopted ones"),
         PropertyOrder(5), Document, UsedImplicitly]
        public bool ApplyDeathChanceToAllHeroes { get; set; } = true;

        [LocDisplayName("{=TsGie7KT}Retinue Death Chance Percent"),
         LocCategory("Death", "{=dbU7WEKG}Death"),
         LocDescription("{=hbP7F9oz}Retinue death chance percent (this determines the chance that a killing blow will " +
                        "actually kill the retinue, removing them from the adopted hero's retinue list)"),
         PropertyOrder(6),
         Range(0, 100), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         YamlIgnore, Document, UsedImplicitly]
        public float RetinueDeathChancePercent
        {
            get => RetinueDeathChance * 100f;
            set => RetinueDeathChance = value * 0.01f;
        }

        [Browsable(false), UsedImplicitly]
        public float RetinueDeathChance { get; set; } = 0.025f;
        #endregion
        #endregion
    }
}
