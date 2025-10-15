using System;
using System.Linq;
using System.Text;
using BannerlordTwitch;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Achievements;
using JetBrains.Annotations;

namespace BLTAdoptAHero
{
    [LocDisplayName("{=BLT_Leaderboard_Name}Duel Leaderboard"),
     LocDescription("{=BLT_Leaderboard_Desc}Display the top duel winners from the current save"),
     UsedImplicitly]
    public class LeaderboardCommand : ICommandHandler
    {
        public Type HandlerConfigType => null;

        public void Execute(ReplyContext context, object config)
        {
            int topCount = 10; // Default value
            
            // Get all adopted heroes with their duel stats
            var heroStats = BLTAdoptAHeroCampaignBehavior.GetAllAdoptedHeroes()
                .Select(hero => new
                {
                    Hero = hero,
                    OwnerName = BLTAdoptAHeroCampaignBehavior.Current.GetHeroOwner(hero),
                    Wins = BLTAdoptAHeroCampaignBehavior.Current.GetAchievementTotalStat(hero, AchievementStatsData.Statistic.DuelWins),
                    Losses = BLTAdoptAHeroCampaignBehavior.Current.GetAchievementTotalStat(hero, AchievementStatsData.Statistic.DuelLosses),
                    Streak = BLTAdoptAHeroCampaignBehavior.Current.GetAchievementTotalStat(hero, AchievementStatsData.Statistic.DuelWinStreak),
                    BestStreak = BLTAdoptAHeroCampaignBehavior.Current.GetAchievementTotalStat(hero, AchievementStatsData.Statistic.DuelBestStreak)
                })
                .Where(h => h.Wins > 0) // Only show heroes who have won at least one duel
                .OrderByDescending(h => h.Wins)
                .ThenByDescending(h => h.BestStreak)
                .Take(topCount)
                .ToList();

            if (!heroStats.Any())
            {
                ActionManager.SendNonReply(context, "{=BLT_Leaderboard_Empty}No duels have been fought yet! Use !duel to challenge someone.".Translate());
                return;
            }

            // Build leaderboard message
            var sb = new StringBuilder();
            sb.AppendLine("{=BLT_Leaderboard_Title}DUEL LEADERBOARD".Translate());
            
            int rank = 1;
            foreach (var stat in heroStats)
            {
                string rankPrefix = rank switch
                {
                    1 => "#1",
                    2 => "#2",
                    3 => "#3",
                    _ => $"#{rank}"
                };

                int totalDuels = stat.Wins + stat.Losses;
                float winRate = totalDuels > 0 ? (float)stat.Wins / totalDuels * 100 : 0;

                sb.Append($"{rankPrefix} {stat.OwnerName}: {stat.Wins}W-{stat.Losses}L ({winRate:F0}%)");
                
                if (stat.Streak > 0)
                {
                    sb.Append($" [Streak: {stat.Streak}]");
                }
                
                if (stat.BestStreak > stat.Streak && stat.BestStreak > 2)
                {
                    sb.Append($" (Best: {stat.BestStreak})");
                }

                sb.AppendLine();
                rank++;
            }

            ActionManager.SendNonReply(context, sb.ToString().Trim());
        }
    }
}
