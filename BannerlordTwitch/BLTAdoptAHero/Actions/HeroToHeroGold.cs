using System;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero;
using BLTAdoptAHero.Annotations;
using TaleWorlds.CampaignSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero.Actions
{
    [LocDisplayName("{=HSuPuNDk}Give Gold To Hero"),
     LocDescription("{=wqR23RYf}Allows viewer heroes to give gold to other viewer heroes. Usage: (username) (amount)"),
     UsedImplicitly]
    public class HeroToHeroGold : HeroCommandHandlerBase
    {
        protected class Settings : IDocumentable
        {
            [LocDisplayName("{=Zx9K2mLp}Minimum Amount"),
             LocDescription("{=4Hn8VqWr}Minimum amount of gold that can be transferred"),
             PropertyOrder(1), UsedImplicitly]
            public int MinAmount { get; set; } = 1;

            [LocDisplayName("{=Vp2X8nRe}Maximum Amount"),
             LocDescription("{=9Bx4WzLs}Maximum amount of gold that can be transferred in one transaction"),
             PropertyOrder(2), UsedImplicitly]
            public int MaxAmount { get; set; } = 10000;

            [LocDisplayName("{=8Kj5RqNm}Transaction Fee Percent"),
             LocDescription("{=7Yt3DnQp}Percentage fee taken from the transferred amount (0-100)"),
             PropertyOrder(3), UsedImplicitly]
            public int TransactionFeePercent { get; set; } = 0;

            public void GenerateDocumentation(IDocumentationGenerator generator)
            {
                generator.PropertyValuePair("Minimum Amount", $"{MinAmount}{Naming.Gold}");
                generator.PropertyValuePair("Maximum Amount", $"{MaxAmount}{Naming.Gold}");
                generator.PropertyValuePair("Transaction Fee", $"{TransactionFeePercent}%");
            }
        }

        public override Type HandlerConfigType => typeof(Settings);

        protected override void ExecuteInternal(Hero adoptedHero, ReplyContext context, object config, Action<string> onSuccess, Action<string> onFailure)
        {
            var settings = (Settings)config;
            var splitArgs = context.Args?.Trim().Split(' ');
            
            if (splitArgs?.Length != 2)
            {
                onFailure("{=9M2HqKfL}Usage: (username) (amount)".Translate());
                return;
            }

            string targetHeroName = splitArgs[0].Replace("@", string.Empty);
            var targetHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(targetHeroName);
            
            if (targetHero == null)
            {
                onFailure("{=vDsYxMKq}Couldn't find recipient '{Name}'".Translate(("Name", targetHeroName)));
                return;
            }
            
            if (targetHero == adoptedHero)
            {
                onFailure("{=5Zx8NpQr}You can't send gold to yourself".Translate());
                return;
            }
            
            if (!int.TryParse(splitArgs[1], out int amount) || amount < settings.MinAmount)
            {
                onFailure("{=8Kj9NmPq}Invalid amount. Minimum is {MinAmount}{GoldIcon}".Translate(
                    ("MinAmount", settings.MinAmount),
                    ("GoldIcon", Naming.Gold)));
                return;
            }
            
            if (amount > settings.MaxAmount)
            {
                onFailure("{=3Vx7QnRt}Amount too large. Maximum is {MaxAmount}{GoldIcon}".Translate(
                    ("MaxAmount", settings.MaxAmount),
                    ("GoldIcon", Naming.Gold)));
                return;
            }
            
            int senderGold = BLTAdoptAHeroCampaignBehavior.Current.GetHeroGold(adoptedHero);
            if (senderGold < amount)
            {
                onFailure(Naming.NotEnoughGold(amount, senderGold));
                return;
            }
            
            // Calculate fee
            int fee = (amount * settings.TransactionFeePercent) / 100;
            int receivedAmount = amount - fee;
            
            // Execute the transfer
            BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(adoptedHero, -amount, true);
            BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(targetHero, receivedAmount);
            
            // Success message
            string message = fee > 0 
                ? "{=2Kx7NvRq}Sent {Amount}{GoldIcon} to @{TargetName} (received: {ReceivedAmount}{GoldIcon}, fee: {Fee}{GoldIcon})".Translate(
                    ("Amount", amount),
                    ("GoldIcon", Naming.Gold),
                    ("TargetName", targetHero.FirstName),
                    ("ReceivedAmount", receivedAmount),
                    ("Fee", fee))
                : "{=9Jx4MnPv}Sent {Amount}{GoldIcon} to @{TargetName}".Translate(
                    ("Amount", amount),
                    ("GoldIcon", Naming.Gold),
                    ("TargetName", targetHero.FirstName));
            
            onSuccess(message);
            
            // Send notification to recipient
            ActionManager.SendNonReply(context,
                "{=6Kx2NvRt}@{TargetName} received {ReceivedAmount}{GoldIcon} from @{SenderName}".Translate(
                    ("TargetName", targetHero.FirstName),
                    ("ReceivedAmount", receivedAmount),
                    ("GoldIcon", Naming.Gold),
                    ("SenderName", adoptedHero.FirstName)));
        }
    }
}