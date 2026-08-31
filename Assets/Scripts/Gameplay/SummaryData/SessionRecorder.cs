using UnityEngine;

public class SessionRecorder : MonoBehaviour
{
    [SerializeField] private ExerciseType exerciseType;
    [SerializeField] private HybridExerciseProfile hybridProfile;

    public static string LastGeneralSuggestion { get; private set; }

    private float totalInteractionDelay;
    private int interactionCount;

    private int ducksHit;
    private int ducksMissed;

    private float completionTime;
    private ExerciseScore pendingScore;
    private bool hasPerformance;
    private readonly HybridExerciseResultSynchronizer results = new HybridExerciseResultSynchronizer();

    private void OnEnable()
    {
        MetricsTrackingSystem.OnTrackingStop += results.CaptureUsage;
        ErgonomicExposureEventBus.OnTrackingStop += results.CaptureExposure;
        GameManager.OnExcerciseStart += BeginExercise;
        GameManager.OnExerciseFinalizing += SaveExerciseSummary;
    }

    private void OnDisable()
    {
        MetricsTrackingSystem.OnTrackingStop -= results.CaptureUsage;
        ErgonomicExposureEventBus.OnTrackingStop -= results.CaptureExposure;
        GameManager.OnExcerciseStart -= BeginExercise;
        GameManager.OnExerciseFinalizing -= SaveExerciseSummary;
        results.Clear();
        pendingScore = null;
    }

    private void BeginExercise()
    {
        results.Begin();
        LastGeneralSuggestion = null;
        pendingScore = null;
        totalInteractionDelay = completionTime = 0f;
        interactionCount = ducksHit = ducksMissed = 0;
        hasPerformance = false;
    }

    public void SetOsuData(float totalInteractionDelay, int interactionCount)
    {
        this.totalInteractionDelay = totalInteractionDelay;
        this.interactionCount = interactionCount;
        hasPerformance = true;
    }

    public void SetDuckHunterData(int ducksHit, int ducksMissed)
    {
        this.ducksHit = ducksHit;
        this.ducksMissed = ducksMissed;
        hasPerformance = true;
    }

    public void SetInsertPiecesData(float completionTime)
    {
        this.completionTime = completionTime;
        hasPerformance = true;
    }

    public void SetPendingScore(ExerciseScore score)
    {
        pendingScore = score;
    }

    private void SaveExerciseSummary(float duration)
    {
        if (!results.TryFinalize(duration, out bool exposureReady))
        {
            pendingScore = null;
            Debug.LogWarning("[HybridFinalSuggestions] No hay resumen de uso pendiente del ejercicio; no se confirma un resultado incompleto o duplicado.", this);
            return;
        }

        ExerciseSummary summary = new()
        {
            exerciseType = exerciseType,
            exerciseDuration = duration,
            leftHand = results.LeftUsage,
            rightHand = results.RightUsage,

            totalInteractionDelay = totalInteractionDelay,
            interactionCount = interactionCount,

            ducksHit = ducksHit,
            ducksMissed = ducksMissed,

            completionTime = completionTime
        };

        bool profileReady = hybridProfile != null && hybridProfile.TryValidate(out _) && hybridProfile.ExerciseType == exerciseType;
        bool matchingCalibration = profileReady && exposureReady &&
            results.LeftExposure.calibrationProfileId == hybridProfile.CalibrationProfile.GetInstanceID() &&
            results.RightExposure.calibrationProfileId == hybridProfile.CalibrationProfile.GetInstanceID();
        if (matchingCalibration)
        {
            summary.generalSuggestion = HybridFinalSuggestionBuilder.Build(hybridProfile, summary,
                results.LeftExposure, results.RightExposure, hasPerformance);
        }
        else
        {
            Debug.LogWarning("[HybridFinalSuggestions] Falta resumen ergonómico compatible o perfil del ejercicio; se utiliza el desempeño como respaldo.", this);
            summary.generalSuggestion = hasPerformance ? GeneralSuggestionBuilder.Build(
                exerciseType, completionTime, totalInteractionDelay, ducksHit, ducksMissed) : string.Empty;
        }
        if (string.IsNullOrWhiteSpace(summary.generalSuggestion))
            summary.generalSuggestion = "No hay datos suficientes para formular una sugerencia final.";
        LastGeneralSuggestion = summary.generalSuggestion;
        Debug.Log($"[HybridFinalSuggestions] Sugerencia final ({exerciseType}): {summary.generalSuggestion}");

        ExerciseScore score = pendingScore;
        pendingScore = null;

        if (SessionManager.Instance == null)
        {
            Debug.LogError("[SessionRecorder] No existe un SessionManager para confirmar el ejercicio.");
            return;
        }

        ExerciseCommitOutcome outcome = SessionManager.Instance.CommitExerciseResult(
            summary,
            score);

        Debug.Log(
            $"[SessionRecorder] Resultado del commit: ejercicio={exerciseType}, estado={outcome}.");
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
