using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.UI;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    internal partial class GlobalTournamentConfig
    {
        public class RoundRewardsDef
        {
            [LocDisplayName("{=IQTT5vYE}Win Gold"),
             LocDescription("{=i7Ns7K2i}Gold won if the hero wins thier match in the round"),
             PropertyOrder(1), UsedImplicitly, Document]
            public int WinGold { get; set; } = 10000;

            [LocDisplayName("{=h8I3PWkV}Win XP"),
             LocDescription("{=b0MeEmOS}XP given if the hero wins thier match in the round"),
             PropertyOrder(2), UsedImplicitly, Document]
            public int WinXP { get; set; } = 10000;

            [LocDisplayName("{=Vobr36Bl}Lose XP"),
             LocDescription("{=Oq7LMvoF}XP given if the hero loses thier match in a round"),
             PropertyOrder(3), UsedImplicitly, Document]
            public int LoseXP { get; set; } = 2500;

            public override string ToString() =>
                "{=IQTT5vYE}Win Gold".Translate() +
                $" {WinGold}, " +
                "{=h8I3PWkV}Win XP".Translate() +
                $" {WinXP}, " +
                "{=Vobr36Bl}Lose XP".Translate() +
                $" {LoseXP}";
        }

        [LocDisplayName("{=VeSh8k7c}Round 1 Rewards"),
         LocCategory("Round Rewards", "{=g0A4pXY2}Round Rewards"),
         LocDescription("{=VeSh8k7c}Round 1 Rewards"),
         PropertyOrder(1), ExpandableObject, UsedImplicitly, Document]
        public RoundRewardsDef Round1Rewards { get; set; } = new() { WinGold = 5000, WinXP = 5000, LoseXP = 5000 };
        [LocDisplayName("{=gQ9YX7my}Round 2 Rewards"),
         LocCategory("Round Rewards", "{=g0A4pXY2}Round Rewards"),
         LocDescription("{=gQ9YX7my}Round 2 Rewards"),
         PropertyOrder(2), ExpandableObject, UsedImplicitly, Document]
        public RoundRewardsDef Round2Rewards { get; set; } = new() { WinGold = 7500, WinXP = 7500, LoseXP = 7500 };
        [LocDisplayName("{=17pTC7NS}Round 3 Rewards"),
         LocCategory("Round Rewards", "{=g0A4pXY2}Round Rewards"),
         LocDescription("{=17pTC7NS}Round 3 Rewards"),
         PropertyOrder(3), ExpandableObject, UsedImplicitly, Document]
        public RoundRewardsDef Round3Rewards { get; set; } = new() { WinGold = 10000, WinXP = 10000, LoseXP = 10000 };
        [LocDisplayName("{=pxMWYe5J}Round 4 Rewards"),
         LocCategory("Round Rewards", "{=g0A4pXY2}Round Rewards"),
         LocDescription("{=pxMWYe5J}Round 4 Rewards"),
         PropertyOrder(4), ExpandableObject, UsedImplicitly, Document]
        public RoundRewardsDef Round4Rewards { get; set; } = new() { WinGold = 0, WinXP = 0, LoseXP = 0 };

        [YamlIgnore, Browsable(false)]
        public RoundRewardsDef[] RoundRewards => new[] { Round1Rewards, Round2Rewards, Round3Rewards, Round4Rewards };
    }
}
