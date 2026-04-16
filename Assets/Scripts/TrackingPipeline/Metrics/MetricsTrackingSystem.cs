using UnityEngine;

public class MetricsTrackingSystem : MonoBehaviour
{
    public bool isTracking;

    public ExerciseMetricsTracker leftTracker;
    public ExerciseMetricsTracker rightTracker;

    void OnEnable()
    {
        MotionEventBus.OnFrame += OnFrameReceived;
    }
    void OnDisable()
    {
        MotionEventBus.OnFrame -= OnFrameReceived;
    }
    public void RunTracking()
    {
        leftTracker = new ExerciseMetricsTracker(HandType.LEFT);
        rightTracker = new ExerciseMetricsTracker(HandType.RIGHT);
        leftTracker.Reset();
        rightTracker.Reset();
        isTracking = true;

    }

    public void StopTracking()
    {
        isTracking = false;
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
        return MetricsSummaryBuilder.Build(leftTracker, duration);//duration);
    }
    public ExerciseSummary GetRightSummary(float duration)
    {
        return MetricsSummaryBuilder.Build(rightTracker, duration);//duration);
    }
}