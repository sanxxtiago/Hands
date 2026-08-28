using System;
using System.Collections.Generic;

public static class ScoreTierResolver
{
    [Serializable]
    public struct TierThresholds
    {
        public float bronzeMax;
        public float silverMax;

        public TierThresholds(float bronzeMax, float silverMax)
        {
            this.bronzeMax = bronzeMax;
            this.silverMax = silverMax;
        }
    }

    private static readonly Dictionary<ScoreExerciseType, TierThresholds> _thresholds = new()
    {
        { ScoreExerciseType.Insert,     new TierThresholds(50f, 80f) },
        { ScoreExerciseType.OSU,        new TierThresholds(50f, 80f) },
        { ScoreExerciseType.DuckHunter, new TierThresholds(50f, 80f) },
    };

    public static TrophyTier GetTier(float totalScore, ScoreExerciseType exerciseType)
    {
        if (!TryGetThresholds(exerciseType, out var thresholds))
            return TrophyTier.Bronze;

        if (totalScore >= thresholds.silverMax)
            return TrophyTier.Gold;

        if (totalScore >= thresholds.bronzeMax)
            return TrophyTier.Silver;

        return TrophyTier.Bronze;
    }

    public static TrophyTier GetTier(ExerciseScore score)
    {
        if (score == null || !score.isValid)
            return TrophyTier.Bronze;

        return GetTier(score.totalScore, score.exerciseType);
    }

    public static void SetThresholds(ScoreExerciseType exerciseType, float bronzeMax, float silverMax)
    {
        _thresholds[exerciseType] = new TierThresholds(bronzeMax, silverMax);
    }

    public static TierThresholds GetThresholds(ScoreExerciseType exerciseType)
    {
        TryGetThresholds(exerciseType, out var result);
        return result;
    }

    private static bool TryGetThresholds(ScoreExerciseType exerciseType, out TierThresholds result)
    {
        return _thresholds.TryGetValue(exerciseType, out result);
    }
}
