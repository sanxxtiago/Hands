using System;
using UnityEngine;

public class MetricsTrackingSystem : MonoBehaviour
{
    public bool isTracking;

    public ExerciseMetricsTracker leftTracker = new ExerciseMetricsTracker(HandType.LEFT);
    public ExerciseMetricsTracker rightTracker = new ExerciseMetricsTracker(HandType.RIGHT);

    public static event Action<ExerciseSummary, ExerciseSummary> OnTrackingStop;
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

    public ExerciseSummary GetLeftSummary(float duration)
    {
        return MetricsSummaryBuilder.Build(leftTracker, duration);
    }
    public ExerciseSummary GetRightSummary(float duration)
    {
        return MetricsSummaryBuilder.Build(rightTracker, duration);
    }
}