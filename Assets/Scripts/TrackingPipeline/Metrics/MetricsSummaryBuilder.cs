using System.Linq;

public static class MetricsSummaryBuilder
{
    public static ExerciseSummary Build(ExerciseMetricsTracker tracker, float duration)
    {
        var zones = tracker.GetTrackedZones().ToArray();
        var records = zones.Select(z => tracker.GetZoneRecord(z)).ToArray();

        int totalFrames = tracker.TotalFrames;
        float totalActiveTime = tracker.GetActiveTime();
        float totalZoneActiveTime = records.Sum(r => r.activeTime);

        return new ExerciseSummary
        {
            handType = tracker.HandType,
            zones = zones,
            absoluteUsage = records.Select(r => totalFrames > 0
                ? (float)r.activeFrames / totalFrames
                : 0f).ToArray(),
            relativeUsage = records.Select(r => totalZoneActiveTime > 0
                ? r.activeTime / totalZoneActiveTime
                : 0f).ToArray(),
            intensity = records.Select(r => totalFrames > 0
                ? r.accumulatedValue / totalFrames
                : 0f).ToArray(),
            totalDurationSeconds = tracker.ElapsedTime,
            totalActiveSeconds = totalActiveTime,
            activityRatio = tracker.GetActivityRatio(duration)
        };
    }
}
