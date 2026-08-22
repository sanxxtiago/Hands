using System.Collections;
using UnityEngine;

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

    private float _stepSpawnTime;
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
        TotalInteractionTime = 0f;
        InteractionCount = 0;

        if (this.sequence == null || this.sequence.PhaseCount == 0)
        {
            Debug.LogError("OSU: no hay fases configuradas en la secuencia.");
            return;
        }

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
            Debug.LogError($"OSU: no se pudo iniciar la fase {currentPhaseIndex + 1}.");
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
                $"OSU: la fase {currentPhaseIndex + 1}, paso {currentStepIndex + 1} " +
                "no tiene un prefab valido.");
            return;
        }

        _stepSpawnTime = Time.time;

        Vector3 spawnPosition = step.spawnPosition;

        if (step.path != null)
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
                $"Prefab {step.prefab.name} no contiene DotBehaviour.");
            Destroy(instance);
            return;
        }

        currentDot.SetColor(step.requiredHand);

        if (currentDot is TrackingDotBehaviour trackingDot)
            trackingDot.SetPath(step.path, pathPrefab, sequence.PointsDepth);

        detector.target = currentDot;

        currentDot.OnCompleted += HandleDotCompleted;
        currentDot.OnMissed += HandleDotMissed;
        currentDot.OnTouched += HandleDotTouched;
    }

    private void HandleDotTouched(DotBehaviour dot)
    {
        InteractionCount++;
        TotalInteractionTime += Time.time - _stepSpawnTime;
        Debug.Log($"TT: {TotalInteractionTime}");
    }

    private void HandleDotCompleted(DotBehaviour dot)
    {
        if (dot != currentDot)
            return;

        UnsubscribeDot(dot);
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

        Debug.Log("Missed");
        UnsubscribeDot(dot);
        currentDot = null;
        currentStepIndex++;

        exerciseController.progressManager.AddMissedStep();

        Destroy(dot.gameObject);
        AdvanceAfterStep();
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
    }

    private void StopPhaseTransition()
    {
        if (phaseTransitionCoroutine == null)
            return;

        StopCoroutine(phaseTransitionCoroutine);
        phaseTransitionCoroutine = null;
    }
}
