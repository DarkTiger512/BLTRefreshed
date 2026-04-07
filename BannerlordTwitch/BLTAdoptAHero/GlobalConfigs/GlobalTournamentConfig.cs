using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    [CategoryOrder("General", 1),
     CategoryOrder("Equipment", 2),
     CategoryOrder("Balancing", 3),
     CategoryOrder("Round Type", 4),
     CategoryOrder("Round Rewards", 5),
     CategoryOrder("Rewards", 6),
     CategoryOrder("Betting", 7),
     CategoryOrder("Prize", 8),
     CategoryOrder("Prize Tier", 9),
     CategoryOrder("Custom Prize", 10),
     LocDisplayName("{=AkDCrLgg}Tournament Config")]
    internal partial class GlobalTournamentConfig : IDocumentable
    {
        private const string ID = "Adopt A Hero - Tournament Config";

        internal static void Register() => ActionManager.RegisterGlobalConfigType(ID, typeof(GlobalTournamentConfig));
        internal static GlobalTournamentConfig Get() => ActionManager.GetGlobalConfig<GlobalTournamentConfig>(ID);
        internal static GlobalTournamentConfig Get(BannerlordTwitch.Settings fromSettings) => fromSettings.GetGlobalConfig<GlobalTournamentConfig>(ID);

        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
        }
    }
}
