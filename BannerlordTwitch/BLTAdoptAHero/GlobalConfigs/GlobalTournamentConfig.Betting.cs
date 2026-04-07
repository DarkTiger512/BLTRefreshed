using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.UI;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    internal partial class GlobalTournamentConfig
    {
        [LocDisplayName("{=rne7aMUR}Enable Betting"),
         LocCategory("Betting", "{=n1Agm9uJ}Betting"),
         LocDescription("{=FOQEPZD5}Enable betting"),
         PropertyOrder(1), UsedImplicitly, Document]
        public bool EnableBetting { get; set; } = true;

        [LocDisplayName("{=njh9b5GB}Betting On Final Only"),
         LocCategory("Betting", "{=n1Agm9uJ}Betting"),
         LocDescription("{=KGcz71VJ}Only allow betting on the final betting"),
         PropertyOrder(2), UsedImplicitly, Document]
        public bool BettingOnFinalOnly { get; set; }
    }
}
