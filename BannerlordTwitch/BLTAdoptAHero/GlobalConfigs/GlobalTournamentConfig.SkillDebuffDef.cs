using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.Core;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    public class SkillDebuffDef : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [LocDisplayName("{=OEMBeawy}Skill"),
         LocDescription("{=twgHWqU6}Skill or skill group to modifer (all skills in a group will be modified)"),
         PropertyOrder(1), UsedImplicitly, Document]
        public SkillsEnum Skill { get; set; } = SkillsEnum.All;

        [LocDisplayName("{=5a40vmYi}Skill Reduction Percent Per Win"),
         LocDescription("{=zoMHI9O3}Reduction to the skill per win (in %). See https://www.desmos.com/calculator/ajydvitcer for visualization of how skill will be modified."),
         PropertyOrder(2), UIRangeAttribute(0, 50, 0.5f),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), UsedImplicitly, Document]
        public float SkillReductionPercentPerWin { get; set; } = 3.2f;

        [LocDisplayName("{=hMB4oFmk}Floor Percent"),
         LocDescription("{=HebJIfaE}The lower limit (in %) that the skill(s) can be reduced to."),
         PropertyOrder(2), UIRangeAttribute(0, 100, 0.5f),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), UsedImplicitly, Document]
        public float FloorPercent { get; set; } = 65f;

        [LocDisplayName("{=L7AKFlpb}Example"),
         LocDescription("{=vgXMv8oH}Shows the % reduction of the skill over 20 tournaments"),
         PropertyOrder(3), ReadOnly(true), YamlIgnore, UsedImplicitly]
        public string Example => string.Join(", ",
            Enumerable.Range(0, 20)
                .Select(i => $"{i}: {100 * SkillModifier(i):0}%"));

        public float SkillModifier(int wins)
        {
            return (float)(FloorPercent + (100 - FloorPercent) * Math.Pow(1f - SkillReductionPercentPerWin / 100f, wins * wins)) / 100f;
        }

        public SkillModifierDef ToModifier(int wins)
        {
            return new SkillModifierDef
            {
                Skill = Skill,
                ModifierPercent = SkillModifier(wins) * 100,
            };
        }

        public override string ToString()
        {
            return "{=OEMBeawy}Skill".Translate() +
                   $": {Skill}, " +
                   "{=5a40vmYi}Skill Reduction Percent Per Win".Translate() +
                   $": {SkillReductionPercentPerWin}%, " +
                   "{=hMB4oFmk}Floor Percent".Translate() +
                   $": {FloorPercent}%";
        }
    }
}
