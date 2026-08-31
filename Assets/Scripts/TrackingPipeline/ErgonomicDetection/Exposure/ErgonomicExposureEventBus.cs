using System;

public static class ErgonomicExposureEventBus
{
    public static event Action<FrameErgonomicExposureData> OnFrame;
    public static event Action<float, HandErgonomicExposureSummary,
        HandErgonomicExposureSummary> OnTrackingStop;

    public static void Publish(FrameErgonomicExposureData frame)
    {
        OnFrame?.Invoke(frame);
    }

    public static void PublishTrackingStop(
        float duration,
        HandErgonomicExposureSummary leftSummary,
        HandErgonomicExposureSummary rightSummary)
    {
        OnTrackingStop?.Invoke(duration, leftSummary, rightSummary);
    }
}
