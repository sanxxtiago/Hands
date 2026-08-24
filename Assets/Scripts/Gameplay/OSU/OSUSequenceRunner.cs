using System;
using System.Collections;
using UnityEngine;

public struct OSUTargetScoreContext
{
    public int targetIndex;
    public float spawnTime;
    public bool hasPath;
    public float reactionTime;
    public float timeOutsidePath;
    public bool wasTouched;
    public bool wasCompleted;
    public bool wasMissed;
    public bool isFollowing;
    public string failureReason;
}

public class OSUSequenceRunner : MonoBehaviour
{
    private OSUSequence sequence;
    [SerializeField] private TargetDetector detector;
    [SerializeField] private LineRenderer pathPrefab;
    private OSUBasedExercise exerciseController;
    private int currentPhaseIndex;
    private int currentStepIndex;
    private Coroutine phaseTransitionCoroutine;

    private DotBehaviour currentDot;
    private int currentTargetIndex;
    private int nextTargetIndex;

    private float _stepSpawnTime;
    public event Action<int> OnSequenceStarted;
    public event Action<OSUTargetScoreContext> OnTargetSpawned;
    public event Action<OSUTargetScoreContext> OnTargetTouched;
    public event Action<OSUTargetScoreContext> OnTargetCompleted;
    public event Action<OSUTargetScoreContext> OnTargetMissed;
    public event Action<OSUTargetScoreContext> OnTargetFailed;
    public event Action<OSUTargetScoreContext> OnTargetTrackingStateChanged;
    public float TotalInteractionTime { get; private set; }
    public int InteractionCount { get; private set; }

    public void StartSequence(OSUSequence sequence, OSUBasedExercise controller)
    {
        StopPhaseTransition();
        UnsubscribeCurrentDot();

        exerciseController = controller;
        this.sequence = sequence;
        currentPhaseIndex = 0;
        currentStepIndex = 0;
        nextTargetIndex = 0;
        TotalInteractionTime = 0f;
        InteractionCount = 0;

        if (this.sequence == null || this.sequence.PhaseCount == 0)
        {
            Debug.LogError("[ScoreSystem][OSU] No hay fases configuradas en la secuencia.");
            return;
        }

        OnSequenceStarted?.Invoke(CountTargets(this.sequence));
        BeginCurrentPhase();
    }

    private void OnDisable()
    {
        StopPhaseTransition();
        UnsubscribeCurrentDot();
    }

    private void BeginCurrentPhase()
    {
        if (currentPhaseIndex >= sequence.PhaseCount)
            return;

        currentStepIndex = 0;

        if (!exerciseController.progressManager.BeginPhase(currentPhaseIndex))
        {
            Debug.LogError($"[ScoreSystem][OSU] No se pudo iniciar la fase {currentPhaseIndex + 1}.");
            return;
        }

        SpawnCurrentStep();
    }

    private void SpawnCurrentStep()
    {
        OSUPhaseDefinition phase = sequence.Phases[currentPhaseIndex];

        if (currentStepIndex >= phase.StepCount)
        {
            StartNextPhase();
            return;
        }

        OSUStep step = phase.Steps[currentStepIndex];

        if (step == null || step.prefab == null)
        {
            Debug.LogError(
                $"[ScoreSystem][OSU] La fase {currentPhaseIndex + 1}, paso {currentStepIndex + 1} " +
                "no tiene un prefab valido.");
            return;
        }

        Vector3 spawnPosition = step.spawnPosition;

        if (step.path != null && step.path.curves != null && step.path.curves.Count > 0 &&
            step.path.curves[0] != null && step.path.curves[0].controlPoints != null &&
            step.path.curves[0].controlPoints.Length > 0)
        {
            spawnPosition =
                step.path.curves[0].controlPoints[0];
        }

        spawnPosition.z = sequence.PointsDepth;

        GameObject instance =
            Instantiate(step.prefab,
                        spawnPosition,
                        Quaternion.identity);

        if (!instance.TryGetComponent(out currentDot))
        {
            Debug.LogError(
                $"[ScoreSystem][OSU] El prefab {step.prefab.name} no contiene DotBehaviour.");
            Destroy(instance);
            return;
        }

        _stepSpawnTime = Time.time;
        currentTargetIndex = nextTargetIndex++;

        currentDot.SetColor(step.requiredHand);

        if (currentDot is TrackingDotBehaviour trackingDot)
            trackingDot.SetPath(step.path, pathPrefab, sequence.PointsDepth);

        detector.target = currentDot;

        currentDot.OnCompleted += HandleDotCompleted;
        currentDot.OnMissed += HandleDotMissed;
        currentDot.OnTouched += HandleDotTouched;
        currentDot.OnFailed += HandleDotFailed;

        if (currentDot is TrackingDotBehaviour trackingDotForEvents)
        {
            trackingDotForEvents.OnTrackingStateChanged += HandleTrackingStateChanged;
            OnTargetSpawned?.Invoke(CreateContext(currentDot, true));
        }
        else
        {
            OnTargetSpawned?.Invoke(CreateContext(currentDot, false));
        }
    }

    private void HandleDotTouched(DotBehaviour dot)
    {
        if (dot != currentDot)
            return;

        InteractionCount++;
        float reactionTime = Mathf.Max(0f, Time.time - _stepSpawnTime);
        TotalInteractionTime += reactionTime;
        OSUTargetScoreContext context = CreateContext(dot, dot is TrackingDotBehaviour);
        context.reactionTime = reactionTime;
        context.wasTouched = true;
        OnTargetTouched?.Invoke(context);
    }

    private void HandleDotCompleted(DotBehaviour dot)
    {
        if (dot != currentDot)
            return;

        UnsubscribeDot(dot);
        OSUTargetScoreContext context = CreateContext(dot, dot is TrackingDotBehaviour);
        context.wasTouched = dot.IsHitted;
        context.wasCompleted = true;
        context.timeOutsidePath = GetTimeOutsidePath(dot);
        OnTargetCompleted?.Invoke(context);
        currentDot = null;
        currentStepIndex++;

        exerciseController.progressManager.AddCompletedStep();

        Destroy(dot.gameObject);
        AdvanceAfterStep();
    }

    private void HandleDotMissed(DotBehaviour dot)
    {
        if (dot != currentDot)
            return;

        UnsubscribeDot(dot);
        OSUTargetScoreContext context = CreateContext(dot, dot is TrackingDotBehaviour);
        context.wasMissed = true;
        context.failureReason = "timeout";
        OnTargetMissed?.Invoke(context);
        currentDot = null;
        currentStepIndex++;

        exerciseController.progressManager.AddMissedStep();

        Destroy(dot.gameObject);
        AdvanceAfterStep();
    }

    private void HandleDotFailed(DotBehaviour dot)
    {
        if (dot != currentDot)
            return;

        UnsubscribeDot(dot);
        OSUTargetScoreContext context = CreateContext(dot, dot is TrackingDotBehaviour);
        context.wasTouched = dot.IsHitted;
        context.wasMissed = true;
        context.timeOutsidePath = GetTimeOutsidePath(dot);
        context.failureReason = "trayectoria perdida";
        OnTargetFailed?.Invoke(context);
        currentDot = null;
        currentStepIndex++;

        exerciseController.progressManager.AddMissedStep();
        Destroy(dot.gameObject);
        AdvanceAfterStep();
    }

    private void HandleTrackingStateChanged(bool isFollowing, float timeOutsidePath)
    {
        if (currentDot == null)
            return;

        OSUTargetScoreContext context = CreateContext(currentDot, true);
        context.isFollowing = isFollowing;
        context.timeOutsidePath = timeOutsidePath;
        OnTargetTrackingStateChanged?.Invoke(context);
    }

    private void AdvanceAfterStep()
    {
        OSUPhaseDefinition phase = sequence.Phases[currentPhaseIndex];

        if (currentStepIndex < phase.StepCount)
        {
            SpawnCurrentStep();
            return;
        }

        if (currentPhaseIndex < sequence.PhaseCount - 1)
            StartNextPhase();
    }

    private void StartNextPhase()
    {
        if (phaseTransitionCoroutine != null)
            return;

        if (currentPhaseIndex >= sequence.PhaseCount - 1)
            return;

        float delay = sequence.Phases[currentPhaseIndex].TransitionDelay;
        phaseTransitionCoroutine = StartCoroutine(TransitionToNextPhase(delay));
    }

    private IEnumerator TransitionToNextPhase(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        currentPhaseIndex++;
        phaseTransitionCoroutine = null;
        BeginCurrentPhase();
    }

    private void UnsubscribeCurrentDot()
    {
        if (currentDot == null)
            return;

        UnsubscribeDot(currentDot);
        Destroy(currentDot.gameObject);
        currentDot = null;
    }

    private void UnsubscribeDot(DotBehaviour dot)
    {
        dot.OnCompleted -= HandleDotCompleted;
        dot.OnMissed -= HandleDotMissed;
        dot.OnTouched -= HandleDotTouched;
        dot.OnFailed -= HandleDotFailed;

        if (dot is TrackingDotBehaviour trackingDot)
            trackingDot.OnTrackingStateChanged -= HandleTrackingStateChanged;
    }

    private OSUTargetScoreContext CreateContext(DotBehaviour dot, bool hasPath)
    {
        return new OSUTargetScoreContext
        {
            targetIndex = currentTargetIndex,
            spawnTime = _stepSpawnTime,
            hasPath = hasPath,
            timeOutsidePath = GetTimeOutsidePath(dot)
        };
    }

    private static float GetTimeOutsidePath(DotBehaviour dot)
    {
        return dot is TrackingDotBehaviour trackingDot
            ? trackingDot.TotalTimeOutside
            : 0f;
    }

    private static int CountTargets(OSUSequence targetSequence)
    {
        int count = 0;
        for (int i = 0; i < targetSequence.PhaseCount; i++)
            count += targetSequence.Phases[i]?.StepCount ?? 0;

        return count;
    }

    private void StopPhaseTransition()
    {
        if (phaseTransitionCoroutine == null)
            return;

        StopCoroutine(phaseTransitionCoroutine);
        phaseTransitionCoroutine = null;
    }
}
