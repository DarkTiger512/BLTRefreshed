using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.UI;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    internal partial class GlobalTournamentConfig
    {
        [LocDisplayName("{=IQTT5vYE}Win Gold"),
         LocCategory("Rewards", "{=FHkvQbcR}Rewards"),
         LocDescription("{=F8g49VCy}Gold won if the hero wins the tournaments"),
         PropertyOrder(1), UsedImplicitly, Document]
        public int WinGold { get; set; } = 50000;

        [LocDisplayName("{=h8I3PWkV}Win XP"),
         LocCategory("Rewards", "{=FHkvQbcR}Rewards"),
         LocDescription("{=3BudXy0O}XP given if the hero wins the tournaments"),
         PropertyOrder(2), UsedImplicitly, Document]
        public int WinXP { get; set; } = 50000;

        [LocDisplayName("{=5vMTYqdu}Participate XP"),
         LocCategory("Rewards", "{=FHkvQbcR}Rewards"),
         LocDescription("{=TXqjPXh7}XP given if the hero participates in a tournament but doesn't win"),
         PropertyOrder(3), UsedImplicitly, Document]
        public int ParticipateXP { get; set; } = 10000;

        [LocDisplayName("{=Xu2KD7Kc}Prize"),
         LocCategory("Rewards", "{=FHkvQbcR}Rewards"),
         LocDescription("{=wYzmyUCE}Winners prize"),
         PropertyOrder(4), ExpandableObject, Expand, UsedImplicitly, Document]
        public GeneratedRewardDef Prize { get; set; } = new()
        {
            ArmorWeight = 0.3f,
            WeaponWeight = 1f,
            MountWeight = 0.1f,
            Tier1Weight = 0,
            Tier2Weight = 0,
            Tier3Weight = 0,
            Tier4Weight = 0,
            Tier5Weight = 0,
            Tier6Weight = 1,
            CustomWeight = 1,
            CustomItemName = "{=hCNpHVJY}Prize {ITEMNAME}",
            CustomItemPower = 1,
        };
    }
}
