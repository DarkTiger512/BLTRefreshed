using System;
using System.Collections.Generic;

namespace BLTAdoptAHero.Util
{
    public enum CurseLifecycle { Active, CompletedPendingReward, Completed, Failed }

    public sealed class CurseRecord
    {
        public int Version { get; set; } = 1;
        public string HeroId { get; set; }
        public string Owner { get; set; }
        public string StartedAt { get; set; }
        public int QualifyingWins { get; set; }
        public CurseLifecycle Status { get; set; } = CurseLifecycle.Active;
        public HashSet<string> ProcessedBattleIds { get; set; } = new(StringComparer.Ordinal);
        public string FinishedAt { get; set; }
        public string RewardItemId { get; set; }
        public string FailureReason { get; set; }
    }

    public sealed class CurseHistoryEntry
    {
        public string HeroId { get; set; }
        public string Owner { get; set; }
        public CurseLifecycle Status { get; set; }
        public int Wins { get; set; }
        public string FinishedAt { get; set; }
        public string Reason { get; set; }
    }

    public static class CursedArtifactPolicy
    {
        public static float ClampChance(float value) => Math.Max(0f, Math.Min(100f, value));
        public static int ClampCooldown(int value) => Math.Max(0, Math.Min(3650, value));
        public static int ClampRequiredWins(int value) => Math.Max(1, Math.Min(1000, value));
        public static float OutgoingMultiplier(float penaltyPercent) => 1f - Math.Max(0f, Math.Min(95f, penaltyPercent)) / 100f;
        public static float IncomingMultiplier(float increasePercent) => 1f + Math.Max(0f, Math.Min(400f, increasePercent)) / 100f;

        public static bool RecordVictory(CurseRecord record, string battleId, int requiredWins)
        {
            if (record?.Status != CurseLifecycle.Active || string.IsNullOrWhiteSpace(battleId)) return false;
            record.ProcessedBattleIds ??= new HashSet<string>(StringComparer.Ordinal);
            if (!record.ProcessedBattleIds.Add(battleId)) return false;
            record.QualifyingWins++;
            if (record.QualifyingWins >= ClampRequiredWins(requiredWins))
                record.Status = CurseLifecycle.CompletedPendingReward;
            return true;
        }
    }
}
