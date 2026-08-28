using UnityEngine;

public class SessionRecorder : MonoBehaviour
{
    [SerializeField] private ExerciseType exerciseType;

    public static string LastGeneralSuggestion { get; private set; }

    private float totalInteractionDelay;
    private int interactionCount;

    private int ducksHit;
    private int ducksMissed;

    private float completionTime;

    private void OnEnable()
    {
        MetricsTrackingSystem.OnTrackingStop += SaveExerciseSummary;
    }

    private void OnDisable()
    {
        MetricsTrackingSystem.OnTrackingStop -= SaveExerciseSummary;
    }

    public void SetOsuData(float totalInteractionDelay, int interactionCount)
    {
        this.totalInteractionDelay = totalInteractionDelay;
        this.interactionCount = interactionCount;
    }

    public void SetDuckHunterData(int ducksHit, int ducksMissed)
    {
        this.ducksHit = ducksHit;
        this.ducksMissed = ducksMissed;
    }

    public void SetInsertPiecesData(float completionTime)
    {
        this.completionTime = completionTime;
    }

    private void SaveExerciseSummary(
        float duration,
        HandUsageSummary leftSummary,
        HandUsageSummary rightSummary)
    {
        string generalSuggestion = GeneralSuggestionBuilder.Build(
            exerciseType,
            completionTime,
            totalInteractionDelay,
            ducksHit,
            ducksMissed);

        LastGeneralSuggestion = generalSuggestion;

        ExerciseSummary summary = new()
        {
            exerciseType = exerciseType,
            exerciseDuration = duration,
            leftHand = leftSummary,
            rightHand = rightSummary,

            totalInteractionDelay = totalInteractionDelay,
            interactionCount = interactionCount,

            ducksHit = ducksHit,
            ducksMissed = ducksMissed,

            completionTime = completionTime,
            generalSuggestion = generalSuggestion
        };

        Debug.Log($"[SuggestionSystem] Sugerencia final: {summary.generalSuggestion}");

        SessionManager.Instance.AddExerciseSummary(summary);
    }
}

public static class GeneralSuggestionBuilder
{
    public static string Build(
        ExerciseType exerciseType,
        float completionTime,
        float totalInteractionTime,
        int ducksHit,
        int ducksMissed)
    {
        return exerciseType switch
        {
            ExerciseType.Insert => BuildInsert(completionTime),
            ExerciseType.OSU => BuildOsu(totalInteractionTime),
            ExerciseType.DuckHunter => BuildDuckHunter(ducksHit, ducksMissed),
            _ => "Continúa practicando para mejorar tu desempeño."
        };
    }

    private static string BuildInsert(float duration)
    {
        if (duration <= 60f)
            return "Muy buen ritmo en la inserción de piezas.";

        if (duration <= 120f)
            return "Buen trabajo. Intenta mantener un ritmo más constante.";

        return "Practica movimientos más fluidos para reducir el tiempo de inserción.";
    }

    private static string BuildOsu(float totalInteractionTime)
    {
        if (totalInteractionTime <= 30f)
            return "Muy buenos tiempos de reacción en los objetivos.";

        if (totalInteractionTime <= 60f)
            return "Buen trabajo. Intenta reducir gradualmente el tiempo de interacción.";

        return "Concéntrate en anticipar la posición de los objetivos para reaccionar más rápido.";
    }

    private static string BuildDuckHunter(int ducksHit, int ducksMissed)
    {
        int total = ducksHit + ducksMissed;
        if (total == 0)
            return "No hubo objetivos suficientes para evaluar la precisión.";

        float missedRatio = (float)ducksMissed / total;

        if (missedRatio <= 0.2f)
            return "Muy buena precisión al cazar los objetivos.";

        if (missedRatio <= 0.5f)
            return "Buen trabajo. Intenta mejorar la precisión de los disparos.";

        return "Mantén la mira sobre el objetivo antes de disparar para reducir los fallos.";
    }
}