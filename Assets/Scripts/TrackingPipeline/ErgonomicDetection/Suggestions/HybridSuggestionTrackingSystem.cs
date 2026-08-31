using UnityEngine;

[DisallowMultipleComponent]
public sealed class HybridSuggestionTrackingSystem : MonoBehaviour
{
    [SerializeField] private HybridSuggestionProfile suggestionProfile;
    [SerializeField] private ErgonomicCalibrationProfile calibrationProfile;
    [Tooltip("Objetivo explícito del ejercicio. ObserveOnly no recomienda aumentar articulaciones.")]
    [SerializeField] private HybridCoordinationGoal leftGoal = HybridCoordinationGoal.ObserveOnly;
    [SerializeField] private HybridCoordinationGoal rightGoal = HybridCoordinationGoal.ObserveOnly;
    [Tooltip("LogOnly permite comparar con el sistema actual sin duplicar notificaciones. LogAndSnackbar no desactiva el sistema actual.")]
    [SerializeField] private HybridSuggestionOutput output = HybridSuggestionOutput.LogOnly;

    private UsageInterpreter leftUsage;
    private UsageInterpreter rightUsage;
    private readonly HybridFramePairer leftPair = new HybridFramePairer(HandType.LEFT);
    private readonly HybridFramePairer rightPair = new HybridFramePairer(HandType.RIGHT);
    private SuggestionDecisionOrchestrator orchestrator;
    private HybridSuggestionGate gate;
    private HybridSuggestionData? leftCandidate;
    private HybridSuggestionData? rightCandidate;
    private int synchronizationErrors;

    public bool IsTracking { get; private set; }
    public int SuggestionsEmitted => gate == null ? 0 : gate.EmittedCount;

    private void OnEnable()
    {
        MotionEventBus.OnFrame += OnMotionFrame;
        ErgonomicExposureEventBus.OnFrame += OnExposureFrame;
        GameManager.OnExcerciseStart += BeginExercise;
        GameManager.OnExerciseEnd += EndExercise;
    }

    private void OnDisable()
    {
        MotionEventBus.OnFrame -= OnMotionFrame;
        ErgonomicExposureEventBus.OnFrame -= OnExposureFrame;
        GameManager.OnExcerciseStart -= BeginExercise;
        GameManager.OnExerciseEnd -= EndExercise;
        IsTracking = false;
        ClearPending();
    }

    public void BeginExercise()
    {
        IsTracking = false;
        ClearPending();
        if (suggestionProfile == null || !suggestionProfile.TryValidate(out _) ||
            calibrationProfile == null || !calibrationProfile.TryValidate(out _) ||
            !System.Enum.IsDefined(typeof(HybridCoordinationGoal), leftGoal) ||
            !System.Enum.IsDefined(typeof(HybridCoordinationGoal), rightGoal))
        {
            Debug.LogError("[HybridSuggestions] Faltan perfiles válidos u objetivos conocidos.", this);
            return;
        }

        leftUsage = new UsageInterpreter(HandType.LEFT, suggestionProfile.UsageWindowSeconds,
            calibrationProfile.MaximumFrameGapSeconds);
        rightUsage = new UsageInterpreter(HandType.RIGHT, suggestionProfile.UsageWindowSeconds,
            calibrationProfile.MaximumFrameGapSeconds);
        orchestrator = new SuggestionDecisionOrchestrator(suggestionProfile, calibrationProfile, leftGoal, rightGoal);
        gate = new HybridSuggestionGate(suggestionProfile);
        leftPair.Reset();
        rightPair.Reset();
        synchronizationErrors = 0;
        IsTracking = true;
    }

    public void EndExercise(float duration)
    {
        if (!IsTracking) return;
        IsTracking = false;
        ClearPending();
        Debug.Log($"[HybridSuggestions] Fin de ejercicio | duración: {duration:F2}s" +
            $" | sugerencias: {SuggestionsEmitted} | descartes: {synchronizationErrors} | salida: {output}", this);
    }

    private void OnMotionFrame(FrameMotionData frame)
    {
        if (!IsTracking || !IsHand(frame.handType)) return;
        SetCandidate(frame.handType, null);
        UsageInterpreter interpreter = frame.handType == HandType.LEFT ? leftUsage : rightUsage;
        HybridFramePairer pair = frame.handType == HandType.LEFT ? leftPair : rightPair;
        if (!interpreter.TryProcess(frame, out FrameUsageData usage))
        {
            pair.ClearPending();
            Reject(frame.handType);
            return;
        }
        ProcessPair(frame.handType, pair, pair.Push(usage));
    }

    private void OnExposureFrame(FrameErgonomicExposureData frame)
    {
        if (!IsTracking || !IsHand(frame.handType)) return;
        SetCandidate(frame.handType, null);
        HybridFramePairer pair = frame.handType == HandType.LEFT ? leftPair : rightPair;
        ProcessPair(frame.handType, pair, pair.Push(frame));
    }

    private void ProcessPair(HandType hand, HybridFramePairer pair, HybridPairStatus status)
    {
        if (status == HybridPairStatus.Rejected)
        {
            Reject(hand);
            return;
        }
        if (status != HybridPairStatus.Ready) return;
        if (orchestrator.TryEvaluate(pair.Usage, pair.Exposure, out HybridSuggestionData suggestion))
            SetCandidate(hand, suggestion);
    }

    private void Reject(HandType hand)
    {
        orchestrator.BreakContinuity(hand);
        synchronizationErrors++;
        if (synchronizationErrors == 1)
            Debug.LogWarning($"[HybridSuggestions] Frame inválido o sin pareja para {hand}; se reinicia la continuidad. Los descartes se resumen al terminar.", this);
    }

    private void LateUpdate()
    {
        if (!IsTracking) return;
        // Esperar al final de Update permite priorizar ambas manos del mismo ciclo.
        if (gate.TrySelect(leftCandidate, rightCandidate, Time.time, out HybridSuggestionData suggestion))
        {
            string hand = suggestion.HandType == HandType.LEFT ? "Mano izquierda" : "Mano derecha";
            Debug.Log($"[HybridSuggestions] [{hand}] {suggestion.Message}" +
                $" | frame: {suggestion.FrameId} | t: {suggestion.Timestamp:F3}" +
                $" | regla: {suggestion.type} | prioridad: {suggestion.priority}" +
                $" | dimensión: {suggestion.dimension} | condición: {suggestion.conditionSeconds:F2}s" +
                $" | ángulo: {suggestion.TriggeringExposure.degrees:F1}°" +
                $" | acumulada/continua: {suggestion.TriggeringExposure.cumulativeExposureSeconds:F2}/{suggestion.TriggeringExposure.sustainedExposureSeconds:F2}s" +
                $" | participación muñeca/antebrazo: {suggestion.usage.wristContribution:P0}/{suggestion.usage.forearmContribution:P0}" +
                $" | ventana válida: {suggestion.usage.observedSeconds:F2}s", this);
            HybridSuggestionEventBus.Publish(suggestion);
            if (output == HybridSuggestionOutput.LogAndSnackbar)
                SnackbarManager.Show(SNACKBARTYPE.WARNING, $"{hand}: {suggestion.Message}", suggestionProfile.SnackbarSeconds);
        }
        leftCandidate = rightCandidate = null;
    }

    private void ClearPending()
    {
        leftPair.ClearPending();
        rightPair.ClearPending();
        leftCandidate = rightCandidate = null;
    }

    private void SetCandidate(HandType hand, HybridSuggestionData? candidate)
    {
        if (hand == HandType.LEFT) leftCandidate = candidate;
        else rightCandidate = candidate;
    }

    private static bool IsHand(HandType hand) => hand == HandType.LEFT || hand == HandType.RIGHT;
}
