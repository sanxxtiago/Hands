using System;

[Serializable]
public sealed class ExerciseScore
{
    public ScoreExerciseType exerciseType;
    public float totalScore;
    public string scoreGrade;
    public TrophyTier trophyTier;
    public int classificationProfileVersion;
    public ScoreBreakdown[] breakdown;
    public string motivationalMessage;
    public bool isValid;
    public ScoreStatsData statsData;

    public ExerciseScore()
    {
        scoreGrade = "Invalid";
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
            scoreGrade = finalIsValid ? classification.Grade.ToString() : "Invalid",
            trophyTier = finalIsValid ? classification.TrophyTier : TrophyTier.None,
            classificationProfileVersion = finalIsValid ? classification.ProfileVersion : 0,
            breakdown = normalizedBreakdown,
            motivationalMessage = GetMotivationalMessage(
                finalIsValid ? classification.Grade : ScoreGrade.Invalid),
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
            scoreGrade = "Invalid",
            trophyTier = TrophyTier.None,
            classificationProfileVersion = 0,
            breakdown = Array.Empty<ScoreBreakdown>(),
            motivationalMessage = "No se pudo calcular la puntuacion con los datos recibidos.",
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

    private static string GetMotivationalMessage(ScoreGrade grade)
    {
        switch (grade)
        {
            case ScoreGrade.Excellent:
                return "Excelente trabajo. Mantene este ritmo.";
            case ScoreGrade.Good:
                return "Buen trabajo. Sigue practicando para mejorar.";
            case ScoreGrade.Fair:
                return "Buen comienzo. Intenta mejorar poco a poco.";
            case ScoreGrade.NeedsPractice:
                return "Sigue practicando; cada intento cuenta.";
            default:
                return "No se pudo calcular la puntuacion con los datos recibidos.";
        }
    }
}
