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

        // 🔹 Primera pasada
        for (int i = 0; i < count; i++)
        {
            var record = tracker.GetZoneRecord(zones[i]);

            absolute[i] = record.totalFrames == 0
                ? 0
                : (float)record.activeFrames / record.totalFrames;

            intensity[i] = record.totalFrames == 0
                ? 0
                : record.accumulatedValue / record.totalFrames;

            totalZoneActiveTime += record.activeTime;
        }

        // 🔹 Segunda pasada (distribución)
        for (int i = 0; i < count; i++)
        {
            var record = tracker.GetZoneRecord(zones[i]);

            relative[i] = totalZoneActiveTime == 0
                ? 0
                : record.activeTime / totalZoneActiveTime;
        }

        return new ExerciseSummary
        {
            handType = tracker.GetRuntimeSnapshot().handType,

            zones = zones.ToArray(),

            absoluteUsage = absolute,   // % de activación
            relativeUsage = relative,   // distribución de uso
            intensity = intensity,     // 🔥 calidad de uso

            totalDurationSeconds = tracker.GetRuntimeSnapshot().elapsedSeconds,
            totalActiveSeconds = totalActiveTime,

            activityRatio = tracker.GetActivityRatio() // 🔥 clave en prevención
        };
    }
}