using UnityEngine;

public class SessionRecorder : MonoBehaviour
{
    [SerializeField] private ExerciseType exerciseType;

    private void OnEnable()
    {
        MetricsTrackingSystem.OnTrackingStop += SaveExerciseSummary;
    }

    private void OnDisable()
    {
        MetricsTrackingSystem.OnTrackingStop -= SaveExerciseSummary;
    }

    private void SaveExerciseSummary(float duration, HandUsageSummary leftSummary, HandUsageSummary rightSummary)
    {
        ExerciseSummary summary = new()
        {
            exerciseType = exerciseType,
            exerciseDuration = duration,
            leftHand = leftSummary,
            rightHand = rightSummary
        };
        SessionManager.Instance.AddExerciseSummary(summary);
    }
}