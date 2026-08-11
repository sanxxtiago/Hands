using Unity.VisualScripting;
using UnityEngine;

public class OSUSequenceRunner : MonoBehaviour
{
    private OSUSequence sequence;
    [SerializeField] private TargetDetector detector;
    [SerializeField] private LineRenderer pathPrefab;
    private OSUBasedExercise exerciseController;
    private int currentStepIndex;

    private DotBehaviour currentDot;

    private float _stepSpawnTime;
    public float TotalInteractionTime { get; private set; }
    public int InteractionCount {get; private set;}

    public void StartSequence(OSUSequence sequence, OSUBasedExercise controller)
    {
        exerciseController = controller;
        this.sequence = sequence;
        currentStepIndex = 0;
        TotalInteractionTime = 0f;
        InteractionCount = 0;

        SpawnCurrentStep();
    }

    private void SpawnCurrentStep()
    {
        if (currentStepIndex >= sequence.steps.Count)
        {
            return;
        }

        OSUStep step = sequence.steps[currentStepIndex];

        _stepSpawnTime = Time.time;

        Vector3 spawnPosition = step.spawnPosition;

        if (step.path != null)
        {
            spawnPosition =
                step.path.curves[0].controlPoints[0];
        }

        GameObject instance =
            Instantiate(step.prefab,
                        spawnPosition,
                        Quaternion.identity);

        if (!instance.TryGetComponent(out currentDot))
        {
            Debug.LogError(
                $"Prefab {step.prefab.name} does not contain DotBehaviour");
            return;
        }
        currentDot.SetColor(step.requiredHand);
        if (currentDot is TrackingDotBehaviour trackingDot)
        {
            trackingDot.SetPath(step.path, pathPrefab);
        }

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
        UnsubscribeDot(dot);

        currentStepIndex++;

        exerciseController.progressManager.AddCompletedStep();

        Destroy(dot.gameObject);

        SpawnCurrentStep();
    }

    private void HandleDotMissed(DotBehaviour dot)
    {
        UnsubscribeDot(dot);

        currentStepIndex++;

        exerciseController.progressManager.AddMissedStep();

        Destroy(dot.gameObject);

        SpawnCurrentStep();
    }

    private void UnsubscribeDot(DotBehaviour dot)
    {
        dot.OnCompleted -= HandleDotCompleted;
        dot.OnMissed -= HandleDotMissed;
        dot.OnTouched -= HandleDotTouched;
    }
}