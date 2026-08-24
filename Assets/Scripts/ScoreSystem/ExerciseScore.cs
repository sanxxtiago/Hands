using System;

[Serializable]
public sealed class ExerciseScore
{
    public ScoreExerciseType exerciseType;
    public float totalScore;
    public string scoreGrade;
    public ScoreBreakdown[] breakdown;
    public string motivationalMessage;
    public bool isValid;

    public ExerciseScore()
    {
        scoreGrade = "Invalid";
        breakdown = Array.Empty<ScoreBreakdown>();
    }
}

internal static class ScoreResultFactory
{
    public static ExerciseScore Create(
        ScoreExerciseType exerciseType,
        ScoreBreakdown[] breakdown,
        bool isValid)
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

        return new ExerciseScore
        {
            exerciseType = exerciseType,
            totalScore = finalIsValid ? totalScore : 0f,
            scoreGrade = GetGrade(finalIsValid, totalScore),
            breakdown = normalizedBreakdown,
            motivationalMessage = GetMotivationalMessage(finalIsValid, totalScore),
            isValid = finalIsValid
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

    private static string GetGrade(bool isValid, float score)
    {
        if (!isValid)
            return "Invalid";

        if (score >= 90f)
            return "Excellent";

        if (score >= 75f)
            return "Good";

        if (score >= 60f)
            return "Fair";

        return "NeedsPractice";
    }

    private static string GetMotivationalMessage(bool isValid, float score)
    {
        if (!isValid)
            return "No se pudo calcular la puntuacion con los datos recibidos.";

        if (score >= 90f)
            return "Excelente trabajo. Mantene este ritmo.";

        if (score >= 75f)
            return "Buen trabajo. Sigue practicando para mejorar.";

        if (score >= 60f)
            return "Buen comienzo. Intenta mejorar poco a poco.";

        return "Sigue practicando; cada intento cuenta.";
    }
}
