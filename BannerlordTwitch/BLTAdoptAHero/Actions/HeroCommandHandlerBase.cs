using System;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using TaleWorlds.CampaignSystem;

namespace BLTAdoptAHero
{
    public abstract class HeroCommandHandlerBase : ICommandHandler
    {
        public void Execute(ReplyContext context, object config)
        {
            var adoptedHero = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(context.UserName);
            if (adoptedHero == null)
            {
                ActionManager.SendFailure(context, AdoptAHero.NoHeroMessage);
                return;
            }

            ExecuteInternal(adoptedHero, context, config, s =>
                {
                    if (!string.IsNullOrEmpty(s)) ActionManager.SendSuccess(context, s);
                },
                s =>
                {
                    if (!string.IsNullOrEmpty(s)) ActionManager.SendFailure(context, s);
                });
        }

        protected abstract void ExecuteInternal(Hero adoptedHero, ReplyContext context, object config,
            Action<string> onSuccess, Action<string> onFailure);

        public virtual Type HandlerConfigType => null;
    }
}
