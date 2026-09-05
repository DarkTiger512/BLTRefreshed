using System;
using BannerlordTwitch.Integration;
using TaleWorlds.CampaignSystem;

namespace BLTAdoptAHero
{
    internal partial class BLTSummonBehavior
    {
        private readonly string balanceMissionId = Guid.NewGuid().ToString("N");
        public IntegrationBalanceSnapshot PublishedBalance { get; private set; }
        partial void PublishBalance()
        {
            PublishedBalance = BalanceBattle ? new IntegrationBalanceSnapshot
            {
                MissionId = balanceMissionId, Summoners = BalanceSummoners, Attackers = BalanceAttackers,
                SummonOffer = BalanceOffer(true), AttackOffer = BalanceOffer(false)
            } : null;
        }
        public IntegrationViewerBalance ViewerBalance(Hero hero) => BalanceBattle && LockedBalanceBonus(hero) is double bonus
            ? new IntegrationViewerBalance { MissionId = balanceMissionId, LockedBonus = bonus } : null;
    }
}
