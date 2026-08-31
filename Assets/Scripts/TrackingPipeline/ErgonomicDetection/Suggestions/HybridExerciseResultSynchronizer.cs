public sealed class HybridExerciseResultSynchronizer
{
    private bool active;
    private bool hasUsage;
    private bool hasExposure;
    private float usageDuration;
    private float exposureDuration;
    public HandUsageSummary LeftUsage { get; private set; }
    public HandUsageSummary RightUsage { get; private set; }
    public HandErgonomicExposureSummary LeftExposure { get; private set; }
    public HandErgonomicExposureSummary RightExposure { get; private set; }

    public void Begin()
    {
        Clear();
        active = true;
    }

    public void Clear()
    {
        active = hasUsage = hasExposure = false;
        usageDuration = exposureDuration = 0f;
        LeftUsage = RightUsage = default;
        LeftExposure = RightExposure = default;
    }

    public void CaptureUsage(float duration, HandUsageSummary left, HandUsageSummary right)
    {
        if (!active || hasUsage || !ValidDuration(duration) || left.handType != HandType.LEFT || right.handType != HandType.RIGHT) return;
        usageDuration = duration;
        LeftUsage = left;
        RightUsage = right;
        hasUsage = true;
    }

    public void CaptureExposure(float duration, HandErgonomicExposureSummary left, HandErgonomicExposureSummary right)
    {
        if (!active || hasExposure || !ValidDuration(duration) || left.handType != HandType.LEFT || right.handType != HandType.RIGHT) return;
        exposureDuration = duration;
        LeftExposure = left;
        RightExposure = right;
        hasExposure = true;
    }

    public bool TryFinalize(float duration, out bool exposureReady)
    {
        exposureReady = false;
        if (!active) return false;
        active = false;
        if (!hasUsage || !ValidDuration(duration) || usageDuration != duration) return false;
        exposureReady = hasExposure && exposureDuration == duration;
        return true;
    }

    private static bool ValidDuration(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
}
