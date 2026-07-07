using UnityEngine;

public class OSUSequenceRunner : MonoBehaviour
{
    private OSUSequence sequence;
    [SerializeField] private TargetDetector detector;
    [SerializeField] private LineRenderer pathPrefab;
    private int currentStepIndex;

    private DotBehaviour currentDot;

    public void StartSequence(OSUSequence sequence)
    {
        this.sequence = sequence;
        currentStepIndex = 0;

        SpawnCurrentStep();
    }

    private void SpawnCurrentStep()
    {
        if (currentStepIndex >= sequence.steps.Count)
        {
            return;
        }

        OSUStep step = sequence.steps[currentStepIndex];

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
    }

    private void HandleDotCompleted(DotBehaviour dot)
    {
        dot.OnCompleted -= HandleDotCompleted;

        currentStepIndex++;

        SpawnCurrentStep();
    }
}