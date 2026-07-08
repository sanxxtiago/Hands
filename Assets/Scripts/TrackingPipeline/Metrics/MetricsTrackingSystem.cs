using System;
using UnityEngine;

public class MetricsTrackingSystem : MonoBehaviour
{
    public bool isTracking;

    public ExerciseMetricsTracker leftTracker = new ExerciseMetricsTracker(HandType.LEFT);
    public ExerciseMetricsTracker rightTracker = new ExerciseMetricsTracker(HandType.RIGHT);

    public static event Action<HandUsageSummary, HandUsageSummary> OnTrackingStop;
    public static event Action<RuntimeMetrics, RuntimeMetrics> OnSnapshot;
   
    void OnEnable()
    {
        MotionEventBus.OnFrame += OnFrameReceived;
        GameManager.OnExcerciseStart += RunTracking;
        GameManager.OnExerciseEnd += StopTracking;
    }
    void OnDisable()
    {
        MotionEventBus.OnFrame -= OnFrameReceived;
        GameManager.OnExcerciseStart -= RunTracking;
        GameManager.OnExerciseEnd -= StopTracking;
    }
    public void RunTracking()
    {
        
        leftTracker.Reset();
        rightTracker.Reset();
        isTracking = true;
    }

    public void StopTracking(float duration)
    {
        isTracking = false;
        OnTrackingStop?.Invoke(GetLeftSummary(duration), GetRightSummary(duration));
        //REVISAR
        ExerciseSummary summary = new ExerciseSummary();
        summary.exerciseType = ExerciseType.Duck;
        summary.leftHand = GetLeftSummary(duration);
        summary.rightHand = GetRightSummary(duration);
        SessionManager.Instance.AddExerciseSummary(summary);

    }

    private void OnFrameReceived(FrameMotionData frame)
    {
        //Solo procesa frames cuando isTracking = true
        if (!isTracking) return;

        if (frame.handType == HandType.LEFT)
            leftTracker.OnFrameReceived(frame);

        if (frame.handType == HandType.RIGHT)
            rightTracker.OnFrameReceived(frame);
    }

    public HandUsageSummary GetLeftSummary(float duration)
    {
        return MetricsSummaryBuilder.Build(leftTracker, duration);
    }
    public HandUsageSummary GetRightSummary(float duration)
    {
        return MetricsSummaryBuilder.Build(rightTracker, duration);
    }
}