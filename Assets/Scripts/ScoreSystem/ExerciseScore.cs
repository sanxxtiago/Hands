using System;

[Serializable]
public sealed class ExerciseScore
{
    public ScoreExerciseType exerciseType;
    public float totalScore;
    public TrophyTier trophyTier;
    public int classificationProfileVersion;
    public ScoreBreakdown[] breakdown;
    public bool isValid;
    public ScoreStatsData statsData;

    public ExerciseScore()
    {
        trophyTier = TrophyTier.None;
        classificationProfileVersion = 0;
        breakdown = Array.Empty<ScoreBreakdown>();
        statsData = new ScoreStatsData();
    }
}

[Serializable]
public struct ScoreStatsData
{
    public float exerciseDuration;
    public int hits;
    public int misses;
}

internal static class ScoreResultFactory
{
    public static ExerciseScore Create(
        ScoreExerciseType exerciseType,
        ScoreBreakdown[] breakdown,
        ScoreStatsData statsData,
        bool isValid,
        ScoreClassificationProfile classificationProfile)
    {
        ScoreBreakdown[] normalizedBreakdown = NormalizeBreakdown(breakdown);
        float totalScore = ScoreMath.CalculateWeightedScore(normalizedBreakdown);
        bool finalIsValid = isValid
            && normalizedBreakdown.Length > 0
            && ScoreMath.HasPositiveWeight(normalizedBreakdown)
            && ScoreMath.IsFinite(totalScore);

        if (isValid && !finalIsValid)
            ScoreSystemLog.Error("No se pudo construir un score valido con la configuracion recibida.");

        totalScore = ScoreMath.ClampScore(totalScore);
        ScoreClassification classification = ScoreClassification.Invalid;

        if (finalIsValid)
        {
            if (classificationProfile == null
                || !classificationProfile.TryResolve(totalScore, out classification))
            {
                ScoreSystemLog.Error(
                    "No se pudo clasificar el score: falta un perfil valido de clasificacion.");
                finalIsValid = false;
                totalScore = 0f;
            }
        }

        return new ExerciseScore
        {
            exerciseType = exerciseType,
            totalScore = finalIsValid ? totalScore : 0f,
            trophyTier = finalIsValid ? classification.TrophyTier : TrophyTier.None,
            classificationProfileVersion = finalIsValid ? classification.ProfileVersion : 0,
            breakdown = normalizedBreakdown,
            isValid = finalIsValid,
            statsData = statsData
        };
    }

    public static ExerciseScore Invalid(ScoreExerciseType exerciseType, string reason)
    {
        ScoreSystemLog.Error(reason);

        return new ExerciseScore
        {
            exerciseType = exerciseType,
            totalScore = 0f,
            trophyTier = TrophyTier.None,
            classificationProfileVersion = 0,
            breakdown = Array.Empty<ScoreBreakdown>(),
            isValid = false
        };
    }

    private static ScoreBreakdown[] NormalizeBreakdown(ScoreBreakdown[] breakdown)
    {
        if (breakdown == null || breakdown.Length == 0)
            return Array.Empty<ScoreBreakdown>();

        ScoreBreakdown[] normalized = new ScoreBreakdown[breakdown.Length];

        for (int i = 0; i < breakdown.Length; i++)
        {
            ScoreBreakdown item = breakdown[i];
            normalized[i] = new ScoreBreakdown(
                item.metricId,
                item.rawValue,
                ScoreMath.ClampScore(item.metricScore),
                ScoreMath.NonNegativeFinite(item.weight));
        }

        return normalized;
    }
}
