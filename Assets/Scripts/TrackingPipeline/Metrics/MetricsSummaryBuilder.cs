using System.Collections.Generic;

public static class MetricsSummaryBuilder
{
    public static ExerciseSummary Build(ExerciseMetricsTracker tracker)
    {
        var zones = new List<MotionZone>(tracker.GetTrackedZones());
        int count = zones.Count;

        float[] absolute = new float[count];
        float[] relative = new float[count];
        float[] intensity = new float[count];

        float totalActiveTime = tracker.GetActiveTime();

        float totalZoneActiveTime = 0f;
        for (int i = 0; i < count; i++)
            totalZoneActiveTime += tracker.GetZoneRecord(zones[i]).activeTime;

        for (int i = 0; i < count; i++)
        {
            var record = tracker.GetZoneRecord(zones[i]);

            absolute[i] = tracker.TotalFrames > 0
                ? (float)record.activeFrames / tracker.TotalFrames
                : 0f;

            relative[i] = totalZoneActiveTime > 0
                ? record.activeTime / totalZoneActiveTime
                : 0f;

            intensity[i] = tracker.TotalFrames > 0
                ? record.accumulatedValue / tracker.TotalFrames
                : 0f;
        }

        return new ExerciseSummary
        {
            handType = tracker.HandType,
            zones = zones.ToArray(),
            absoluteUsage = absolute,
            relativeUsage = relative,
            intensity = intensity,
            totalDurationSeconds = tracker.ElapsedTime,
            totalActiveSeconds = totalActiveTime,
            activityRatio = tracker.GetActivityRatio()
        };
    }
}