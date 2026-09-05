using System;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;

namespace BLTAdoptAHero
{
    public class BalanceCommand : ICommandHandler
    {
        public Type HandlerConfigType => null;
        public void Execute(ReplyContext context, object config)
        {
            var mission = BLTSummonBehavior.Current;
            if (mission == null || !BLTSummonBehavior.BalanceBattle)
            { ActionManager.SendFailure(context, "{=BLTBalanceUnavailable}Joining bonuses are available during battle missions.".Translate()); return; }
            ActionManager.SendSuccess(context, mission.BalanceSummary());
        }
    }
}
