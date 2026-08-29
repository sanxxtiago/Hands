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
        if (!IsFinite(totalScore))
            return TrophyTier.Bronze;

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
        if (!TryGetThresholds(exerciseType, out _))
            throw new ArgumentOutOfRangeException(nameof(exerciseType), exerciseType, "El tipo de ejercicio no tiene umbrales configurados.");

        if (!IsFinite(bronzeMax) || bronzeMax < 0f)
            throw new ArgumentOutOfRangeException(nameof(bronzeMax), bronzeMax, "El umbral de bronce debe ser finito y no negativo.");

        if (!IsFinite(silverMax) || silverMax < bronzeMax)
            throw new ArgumentOutOfRangeException(nameof(silverMax), silverMax, "El umbral de plata debe ser finito y mayor o igual que el umbral de bronce.");

        _thresholds[exerciseType] = new TierThresholds(bronzeMax, silverMax);
    }

    public static TierThresholds GetThresholds(ScoreExerciseType exerciseType)
    {
        if (!TryGetThresholds(exerciseType, out var result))
            throw new ArgumentOutOfRangeException(nameof(exerciseType), exerciseType, "El tipo de ejercicio no tiene umbrales configurados.");

        return result;
    }

    private static bool TryGetThresholds(ScoreExerciseType exerciseType, out TierThresholds result)
    {
        return _thresholds.TryGetValue(exerciseType, out result);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
