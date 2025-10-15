using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Achievements;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    [LocDisplayName("{=BLT_Duel_Name}Duel"),
     LocDescription("{=BLT_Duel_Desc}Challenge another viewer to a duel. Winner gets gold and leaderboard glory!"),
     UsedImplicitly]
    public class DuelCommand : ICommandHandler
    {
        private class Settings : IDocumentable
        {
            [LocDisplayName("{=BLT_Duel_GoldCost}Gold Cost"),
             LocDescription("{=BLT_Duel_GoldCost_Desc}How much gold it costs to challenge someone to a duel"),
             UsedImplicitly, Document]
            public int GoldCost { get; set; } = 1000;

            [LocDisplayName("{=BLT_Duel_WinnerGold}Winner Gold"),
             LocDescription("{=BLT_Duel_WinnerGold_Desc}How much gold the winner receives"),
             UsedImplicitly, Document]
            public int WinnerGold { get; set; } = 2000;

            [LocDisplayName("{=BLT_Duel_WinnerXP}Winner XP"),
             LocDescription("{=BLT_Duel_WinnerXP_Desc}How much XP the winner receives"),
             UsedImplicitly, Document]
            public int WinnerXP { get; set; } = 500;

            [LocDisplayName("{=BLT_Duel_Timeout}Timeout (Seconds)"),
             LocDescription("{=BLT_Duel_Timeout_Desc}How long the challenged player has to accept (in seconds)"),
             UsedImplicitly, Document]
            public int TimeoutSeconds { get; set; } = 30;

            [LocDisplayName("{=BLT_Duel_Cooldown}Cooldown (Seconds)"),
             LocDescription("{=BLT_Duel_Cooldown_Desc}Cooldown between duels (in seconds)"),
             UsedImplicitly, Document]
            public int Cooldown { get; set; } = 60;

            public void GenerateDocumentation(IDocumentationGenerator generator)
            {
                generator.PropertyValuePair("Gold Cost", $"{GoldCost}{Naming.Gold}");
                generator.PropertyValuePair("Winner Gold", $"{WinnerGold}{Naming.Gold}");
                generator.PropertyValuePair("Winner XP", $"{WinnerXP} XP");
                generator.PropertyValuePair("Timeout", $"{TimeoutSeconds} seconds");
                generator.PropertyValuePair("Cooldown", $"{Cooldown} seconds");
            }
        }

        // Track pending duel challenges: (challenger, target) -> challenge time
        [YamlIgnore]
        private static readonly Dictionary<(string challenger, string target), DateTime> pendingDuels = new();
        
        public Type HandlerConfigType => typeof(Settings);

        public void Execute(ReplyContext context, object config)
        {
            var settings = (Settings)config;
            var args = context.Args?.Split(' ');
            
            // Handle !accept command
            if (string.IsNullOrEmpty(context.Args) || args?.FirstOrDefault()?.ToLower() == "accept")
            {
                HandleAccept(context, settings);
                return;
            }

            // Handle !duel {target} command
            string targetName = args?.FirstOrDefault();
            if (string.IsNullOrEmpty(targetName))
            {
                ActionManager.SendReply(context, "{=BLT_Duel_Usage}Usage: !duel {viewer_name} or !duel accept".Translate());
                return;
            }

            HandleChallenge(context, targetName, settings);
        }

        private void HandleChallenge(ReplyContext context, string targetName, Settings settings)
        {
            string challengerName = context.UserName;
            static string Sanitize(string s) => s?.Replace("@", string.Empty).Replace(" ", string.Empty).Replace("\\s", string.Empty);
            var targetCanonical = Sanitize(targetName);
            var challengerCanonical = Sanitize(challengerName);
            
            // Can't duel yourself
            if (string.Equals(targetCanonical, challengerCanonical, StringComparison.OrdinalIgnoreCase))
            {
                ActionManager.SendReply(context, "{=BLT_Duel_CantDuelSelf}You cannot duel yourself!".Translate());
                return;
            }

            // Get challenger hero
            var challenger = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(challengerCanonical);
            if (challenger == null)
            {
                ActionManager.SendReply(context, "{=BLT_Duel_NoHero}You must have an adopted hero to duel! Use !adopt first.".Translate());
                return;
            }

            // Check if challenger is alive
            if (!challenger.IsAlive)
            {
                ActionManager.SendReply(context, "{=BLT_Duel_ChallengerDead}Your hero is dead and cannot duel!".Translate());
                return;
            }

            // Get target hero
            var target = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(targetCanonical);
            if (target == null)
            {
                ActionManager.SendReply(context, "{=BLT_Duel_TargetNoHero}{target} does not have an adopted hero!".Translate(("target", targetName)));
                return;
            }

            // Check if target is alive
            if (!target.IsAlive)
            {
                ActionManager.SendReply(context, "{=BLT_Duel_TargetDead}{target}'s hero is dead!".Translate(("target", targetName)));
                return;
            }

            // Check challenger gold
            int challengerGold = BLTAdoptAHeroCampaignBehavior.Current.GetHeroGold(challenger);
            if (challengerGold < settings.GoldCost)
            {
                ActionManager.SendReply(context, 
                    "{=BLT_Duel_NotEnoughGold}You need {cost} gold to challenge someone to a duel! (You have {current})".Translate(
                        ("cost", settings.GoldCost), ("current", challengerGold)));
                return;
            }

            // Check if there's already a pending challenge
            var challengeKey = (challengerCanonical, targetCanonical);
            if (pendingDuels.ContainsKey(challengeKey))
            {
                ActionManager.SendReply(context, "{=BLT_Duel_AlreadyChallenged}You have already challenged {target}! Wait for them to accept.".Translate(("target", targetName)));
                return;
            }

            // Check if target has already challenged the challenger (reverse duel)
            var reverseKey = (targetCanonical, challengerCanonical);
            if (pendingDuels.ContainsKey(reverseKey))
            {
                // Auto-accept if both want to duel each other!
                pendingDuels.Remove(reverseKey);
                ExecuteDuel(context, challenger, target, challengerName, targetName, settings);
                return;
            }

            // Deduct cost and create challenge
            BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(challenger, -settings.GoldCost);
            pendingDuels[challengeKey] = DateTime.Now;
            
            // Send challenge notification respecting the command's RespondInOverlay/RespondInTwitch settings
            string challengeMessage = "{=BLT_Duel_ChallengeReceived}{challenger} challenges {target} to a duel! Type !duel accept within {timeout} seconds to fight!".Translate(
                ("challenger", "@" + challengerName), ("target", "@" + targetName), ("timeout", settings.TimeoutSeconds));
            
            ActionManager.SendNonReply(context, challengeMessage);
        }

        private void HandleAccept(ReplyContext context, Settings settings)
        {
            string acceptorName = context.UserName;
            static string Sanitize(string s) => s?.Replace("@", string.Empty).Replace(" ", string.Empty).Replace("\\s", string.Empty);
            var acceptorCanonical = Sanitize(acceptorName);
            
            // Find any challenge where this user is the target
            var challenge = pendingDuels.FirstOrDefault(kvp => 
                kvp.Key.target.Equals(acceptorCanonical, StringComparison.OrdinalIgnoreCase));
            
            if (challenge.Key == default)
            {
                ActionManager.SendReply(context, "{=BLT_Duel_NoChallenge}You don't have any pending duel challenges!".Translate());
                return;
            }

            // Check timeout
            if ((DateTime.Now - challenge.Value).TotalSeconds >= settings.TimeoutSeconds)
            {
                pendingDuels.Remove(challenge.Key);
                ActionManager.SendReply(context, "{=BLT_Duel_Expired}The duel challenge has expired!".Translate());
                return;
            }

            // Get heroes
            string challengerName = challenge.Key.challenger;
            var challenger = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(challengerName);
            var target = BLTAdoptAHeroCampaignBehavior.Current.GetAdoptedHero(acceptorCanonical);

            if (challenger == null || !challenger.IsAlive)
            {
                pendingDuels.Remove(challenge.Key);
                ActionManager.SendReply(context, "{=BLT_Duel_ChallengerGone}The challenger's hero is no longer available!".Translate());
                return;
            }

            if (target == null || !target.IsAlive)
            {
                pendingDuels.Remove(challenge.Key);
                ActionManager.SendReply(context, "{=BLT_Duel_NoHero}You must have an adopted hero to accept! Use !adopt first.".Translate());
                return;
            }

            // Remove the challenge
            pendingDuels.Remove(challenge.Key);

            // Execute the duel!
            ExecuteDuel(context, challenger, target, challengerName, acceptorName, settings);
        }

        private void ExecuteDuel(ReplyContext context, Hero challenger, Hero target, string challengerName, string targetName, Settings settings)
        {
            // Calculate combat power for both heroes
            float challengerPower = CalculateCombatPower(challenger);
            float targetPower = CalculateCombatPower(target);

            // Determine winner with weighted random
            float totalPower = challengerPower + targetPower;
            float challengerWinChance = challengerPower / totalPower;
            
            bool challengerWins = MBRandom.RandomFloat < challengerWinChance;
            
            Hero winner = challengerWins ? challenger : target;
            Hero loser = challengerWins ? target : challenger;
            string winnerName = challengerWins ? challengerName : targetName;
            string loserName = challengerWins ? targetName : challengerName;

            // Award rewards
            int goldReward = settings.WinnerGold + settings.GoldCost; // Winner gets reward + the entry fee
            BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(winner, goldReward);
            winner.AddSkillXp(DefaultSkills.OneHanded, settings.WinnerXP);

            // Update stats
            UpdateDuelStats(winner, true);
            UpdateDuelStats(loser, false);

            // Get current stats for display
            int winnerWins = BLTAdoptAHeroCampaignBehavior.Current.GetAchievementTotalStat(winner, AchievementStatsData.Statistic.DuelWins);
            int winnerStreak = BLTAdoptAHeroCampaignBehavior.Current.GetAchievementTotalStat(winner, AchievementStatsData.Statistic.DuelWinStreak);

            // Announce results - respecting the command's RespondInOverlay/RespondInTwitch settings
            string resultMessage = "{=BLT_Duel_Result}DUEL COMPLETE! {winner} defeats {loser}! {winner} wins {gold} gold and gains {xp} XP! (W:{wins} Streak:{streak})".Translate(
                ("winner", "@" + winnerName),
                ("loser", "@" + loserName),
                ("gold", goldReward),
                ("xp", settings.WinnerXP),
                ("wins", winnerWins),
                ("streak", winnerStreak));

            // Send result to overlay/chat based on command settings (no in-game notification to avoid interrupting streamer)
            ActionManager.SendNonReply(context, resultMessage);
        }

        private float CalculateCombatPower(Hero hero)
        {
            // Use Bannerlord's built-in simulation power calculation
            float attackPower = 0;
            float defencePower = 0;
            hero.CharacterObject.GetSimulationAttackPower(out attackPower, out defencePower);
            
            // Factor in equipment tier
            int equipTier = BLTAdoptAHeroCampaignBehavior.Current.GetEquipmentTier(hero);
            float tierBonus = 1f + (equipTier * 0.1f); // 10% per tier
            
            // Factor in level
            float levelBonus = 1f + (hero.Level * 0.02f); // 2% per level
            
            // Combine factors
            float totalPower = (attackPower + defencePower) * tierBonus * levelBonus;
            
            // Ensure minimum power to avoid division by zero
            return Math.Max(totalPower, 10f);
        }

        private void UpdateDuelStats(Hero hero, bool won)
        {
            var campaign = BLTAdoptAHeroCampaignBehavior.Current;
            
            // Get current stats
            int currentWins = campaign.GetAchievementTotalStat(hero, AchievementStatsData.Statistic.DuelWins);
            int currentLosses = campaign.GetAchievementTotalStat(hero, AchievementStatsData.Statistic.DuelLosses);
            int currentStreak = campaign.GetAchievementTotalStat(hero, AchievementStatsData.Statistic.DuelWinStreak);
            int bestStreak = campaign.GetAchievementTotalStat(hero, AchievementStatsData.Statistic.DuelBestStreak);
            
            if (won)
            {
                // Update wins and streak
                campaign.SetAchievementTotalStat(hero, AchievementStatsData.Statistic.DuelWins, currentWins + 1);
                currentStreak++;
                campaign.SetAchievementTotalStat(hero, AchievementStatsData.Statistic.DuelWinStreak, currentStreak);
                
                if (currentStreak > bestStreak)
                {
                    campaign.SetAchievementTotalStat(hero, AchievementStatsData.Statistic.DuelBestStreak, currentStreak);
                }
            }
            else
            {
                // Update losses and reset streak
                campaign.SetAchievementTotalStat(hero, AchievementStatsData.Statistic.DuelLosses, currentLosses + 1);
                campaign.SetAchievementTotalStat(hero, AchievementStatsData.Statistic.DuelWinStreak, 0);
            }
        }

        // Clean up expired challenges periodically
        public static void CleanupExpiredChallenges(int timeoutSeconds)
        {
            var expiredKeys = pendingDuels
                .Where(kvp => (DateTime.Now - kvp.Value).TotalSeconds >= timeoutSeconds)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                pendingDuels.Remove(key);
            }
        }
    }
}
