using System;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using TaleWorlds.CampaignSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Actions
{
    [LocDisplayName("{=simgold001}Sim Gold"),
     LocDescription("{=simgold002}Simulation command to give unlimited gold to heroes for testing purposes. Usage: (amount)"),
     UsedImplicitly]
    public class SimGold : HeroCommandHandlerBase
    {
        protected class Settings : IDocumentable
        {
            [LocDisplayName("{=simgold003}Default Amount"),
             LocDescription("{=simgold004}Default amount of gold to give if no amount is specified"),
             PropertyOrder(1), UsedImplicitly]
            public int DefaultAmount { get; set; } = 10000;

            [LocDisplayName("{=simgold005}Max Amount"),
             LocDescription("{=simgold006}Maximum amount of gold that can be given in one command (0 = unlimited)"),
             PropertyOrder(2), UsedImplicitly]
            public int MaxAmount { get; set; } = 0;

            public void GenerateDocumentation(IDocumentationGenerator generator)
            {
                generator.PropertyValuePair("Default Amount", $"{DefaultAmount}{Naming.Gold}");
                if (MaxAmount > 0)
                    generator.PropertyValuePair("Maximum Amount", $"{MaxAmount}{Naming.Gold}");
                else
                    generator.PropertyValuePair("Maximum Amount", "Unlimited");
            }
        }

        public override Type HandlerConfigType => typeof(Settings);

        protected override void ExecuteInternal(Hero adoptedHero, ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            var settings = (Settings)config;
            int amount = settings.DefaultAmount;

            // Parse amount from arguments if provided
            if (!string.IsNullOrEmpty(context.Args?.Trim()))
            {
                var args = context.Args.Trim();
                if (!int.TryParse(args, out amount) || amount <= 0)
                {
                    onFailure("{=simgold007}Invalid amount. Please specify a positive number.".Translate());
                    return;
                }
            }

            // Check max amount limit if set
            if (settings.MaxAmount > 0 && amount > settings.MaxAmount)
            {
                onFailure("{=simgold008}Amount too large. Maximum is {MaxAmount}{GoldIcon}".Translate(
                    ("MaxAmount", settings.MaxAmount),
                    ("GoldIcon", Naming.Gold)));
                return;
            }

            // Give the gold
            int newGold = BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(adoptedHero, amount);

            onSuccess("{=simgold009}[SIM] Added {Amount}{GoldIcon} to {HeroName}. New balance: {NewGold}{GoldIcon}".Translate(
                ("Amount", amount),
                ("GoldIcon", Naming.Gold),
                ("HeroName", adoptedHero.FirstName),
                ("NewGold", newGold)));
        }
    }
}
