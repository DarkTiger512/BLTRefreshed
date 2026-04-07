using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
        #region XP
        [LocDisplayName("{=lwU4dELT}Use Raw XP"),
         LocCategory("XP", "{=06KnYhyh}XP"),
         LocDescription("{=dICRr4BH}Use raw XP values instead of adjusting by focus and attributes, also ignoring skill cap. This avoids characters getting stuck when focus and attributes are not well distributed. "),
         PropertyOrder(1), Document, UsedImplicitly]
        public bool UseRawXP { get; set; } = true;

        [LocDisplayName("{=S5FAna09}Raw XP Skill Cap"),
         LocCategory("XP", "{=06KnYhyh}XP"),
         LocDescription("{=WUzqXuHN}Skill cap when using Raw XP. Skills will not go above this value. 330 is the vanilla XP skill cap."),
         PropertyOrder(2), Range(0, 1023), Document, UsedImplicitly]
        public int RawXPSkillCap { get; set; } = 330;
        #endregion

        #region Kill Rewards
        [LocDisplayName("{=94Ouh5It}Gold Per Kill"),
         LocCategory("Kill Rewards", "{=E2RBmb1K}Kill Rewards"),
         LocDescription("{=iSAMxZ8a}Gold the hero gets for every kill"),
         PropertyOrder(1), Document, UsedImplicitly]
        public int GoldPerKill { get; set; } = 5000;

        [LocDisplayName("{=DMGKBoJT}XP Per Kill"),
         LocCategory("Kill Rewards", "{=E2RBmb1K}Kill Rewards"),
         LocDescription("{=kwW5pZT9}XP the hero gets for every kill"),
         PropertyOrder(2), Document, UsedImplicitly]
        public int XPPerKill { get; set; } = 5000;

        [LocDisplayName("{=a1zjEuUe}XP Per Killed"),
         LocCategory("Kill Rewards", "{=E2RBmb1K}Kill Rewards"),
         LocDescription("{=bW8t2g5N}XP the hero gets for being killed"),
         PropertyOrder(3), Document, UsedImplicitly]
        public int XPPerKilled { get; set; } = 2000;

        [LocDisplayName("{=cRV9HDdf}Heal Per Kill"),
         LocCategory("Kill Rewards", "{=E2RBmb1K}Kill Rewards"),
         LocDescription("{=7VWAZgfK}HP the hero gets for every kill"),
         PropertyOrder(4), Document, UsedImplicitly]
        public int HealPerKill { get; set; } = 20;

        [LocDisplayName("{=lIhhHjih}Retinue Gold Per Kill"),
         LocCategory("Kill Rewards", "{=E2RBmb1K}Kill Rewards"),
         LocDescription("{=h93j0qw3}Gold the hero gets for every kill their retinue gets"),
         PropertyOrder(5), Document, UsedImplicitly]
        public int RetinueGoldPerKill { get; set; } = 2500;

        [LocDisplayName("{=KwlWrzDS}Retinue Heal Per Kill"),
         LocCategory("Kill Rewards", "{=E2RBmb1K}Kill Rewards"),
         LocDescription("{=Q3UVoHmt}HP the hero's retinue gets for every kill"),
         PropertyOrder(6), Document, UsedImplicitly]
        public int RetinueHealPerKill { get; set; } = 50;

        [LocDisplayName("{=wSzUkbNR}Relative Level Scaling"),
         LocCategory("Kill Rewards", "{=E2RBmb1K}Kill Rewards"),
         LocDescription("{=1LTDJZ7Y}How much to scale the kill rewards by, based on relative level of the two characters. If this is 0 (or not set) then the rewards are always as specified, if this is higher than 0 then the rewards increase if the killed unit is higher level than the hero, and decrease if it is lower. At a value of 0.5 (recommended) at level difference of 10 would give about 2.5 times the normal rewards for gold, xp and health."),
         PropertyOrder(7),
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         Document, UsedImplicitly]
        public float RelativeLevelScaling { get; set; } = 0.5f;

        [LocDisplayName("{=BDk1G4nc}Level Scaling Cap"),
         LocCategory("Kill Rewards", "{=E2RBmb1K}Kill Rewards"),
         LocDescription("{=Vod0pJEN}Caps the maximum multiplier for the level difference, defaults to 5 if not specified"),
         PropertyOrder(8),
         Range(0, 10), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         Document, UsedImplicitly]
        public float LevelScalingCap { get; set; } = 5;

        [LocDisplayName("{=EYXeYMQo}Minimum Gold Per Kill"),
         LocCategory("Kill Rewards", "{=E2RBmb1K}Kill Rewards"),
         LocDescription("{=J2GKeUI9}Minimum percent gold earned per kill"),
         PropertyOrder(9),
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         Document, UsedImplicitly]
        public float MinimumGoldPerKill { get; set; } = 0.5f;
        #endregion

        #region Battle End Rewards
        [LocDisplayName("{=IQTT5vYE}Win Gold"),
         LocCategory("Battle End Rewards", "{=uPwaOKdT}Battle End Rewards"),
         LocDescription("{=pc3G0W39}Gold won if the heroes side wins"),
         PropertyOrder(1), Document, UsedImplicitly]
        public int WinGold { get; set; } = 10000;

        [LocDisplayName("{=h8I3PWkV}Win XP"),
         LocCategory("Battle End Rewards", "{=uPwaOKdT}Battle End Rewards"),
         LocDescription("{=F7Tw4D07}XP the hero gets if the heroes side wins"),
         PropertyOrder(2), Document, UsedImplicitly]
        public int WinXP { get; set; } = 10000;

        [LocDisplayName("{=lfCWK7aA}Lose Gold"),
         LocCategory("Battle End Rewards", "{=uPwaOKdT}Battle End Rewards"),
         LocDescription("{=E209XRml}Gold lost if the heroes side loses(negative to win gold)"),
         PropertyOrder(3), Document, UsedImplicitly]
        public int LoseGold { get; set; } = 5000;

        [LocDisplayName("{=Vobr36Bl}Lose XP"),
         LocCategory("Battle End Rewards", "{=uPwaOKdT}Battle End Rewards"),
         LocDescription("{=itAfYdmO}XP the hero gets if the heroes side loses"),
         PropertyOrder(4), Document, UsedImplicitly]
        public int LoseXP { get; set; } = 5000;

        [LocDisplayName("{=ihB1KMOY}Difficulty Scaling On Players Side"),
         LocCategory("Battle End Rewards", "{=uPwaOKdT}Battle End Rewards"),
         LocDescription("{=Bt1PS0aC}Apply difficulty scaling to players side"),
         PropertyOrder(5), Document, UsedImplicitly]
        public bool DifficultyScalingOnPlayersSide { get; set; } = true;

        [LocDisplayName("{=nym7EtAd}Difficulty Scaling On Enemy Side"),
         LocCategory("Battle End Rewards", "{=uPwaOKdT}Battle End Rewards"),
         LocDescription("{=U0hZef9L}Apply difficulty scaling to enemy side"),
         PropertyOrder(6), Document, UsedImplicitly]
        public bool DifficultyScalingOnEnemySide { get; set; } = true;

        [LocDisplayName("{=CaVuq5tE}Difficulty Scaling"),
         LocCategory("Battle End Rewards", "{=uPwaOKdT}Battle End Rewards"),
         LocDescription("{=IhhfIQ74}End reward difficulty scaling: determines the extent to which higher difficulty battles increase the above rewards (0 to 1)"),
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         PropertyOrder(7), Document, UsedImplicitly]
        public float DifficultyScaling { get; set; } = 1;

        [LocDisplayName("{=891WqOrJ}Difficulty Scaling Min"),
         LocCategory("Battle End Rewards", "{=uPwaOKdT}Battle End Rewards"),
         LocDescription("{=FPXz7lBi}Min difficulty scaling multiplier"),
         PropertyOrder(8),
         Range(0, 1), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         Document, UsedImplicitly]
        public float DifficultyScalingMin { get; set; } = 0.2f;
        [YamlIgnore, Browsable(false)]
        public float DifficultyScalingMinClamped => MathF.Clamp(DifficultyScalingMin, 0, 1);

        [LocDisplayName("{=Wsho5Yns}Difficulty Scaling Max"),
         LocCategory("Battle End Rewards", "{=uPwaOKdT}Battle End Rewards"),
         LocDescription("{=ZW7O1JTv}Max difficulty scaling multiplier"),
         PropertyOrder(9),
         Range(1, 10), Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)),
         Document, UsedImplicitly]
        public float DifficultyScalingMax { get; set; } = 3f;
        [YamlIgnore, Browsable(false)]
        public float DifficultyScalingMaxClamped => Math.Max(DifficultyScalingMax, 1f);
        #endregion
        #endregion
    }
}
