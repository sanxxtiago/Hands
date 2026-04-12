public static class MetricsSummaryBuilder
{
    public static ExerciseSummary Build(ExerciseMetricsTracker tracker)
    {
        var runtime = tracker.GetRuntimeSnapshot();

        float totalFrames = 0;
        float totalActiveFrames = 0;

        float[] absolute = new float[3];
        float[] relative = new float[3];

        var zones = new MotionZone[]
        {
            MotionZone.Hand,
            MotionZone.Wrist,
            MotionZone.Forearm
        };

        for (int i = 0; i < zones.Length; i++)
        {
            var zone = zones[i];

            var record = tracker.GetZoneRecord(zone);

            totalFrames += record.totalFrames;
            totalActiveFrames += record.activeFrames;

            absolute[i] = record.totalFrames == 0
                ? 0
                : (float)record.activeFrames / record.totalFrames;
        }

        for (int i = 0; i < zones.Length; i++)
        {
            var zone = zones[i];
            var record = tracker.GetZoneRecord(zone);

            relative[i] = totalActiveFrames == 0
                ? 0
                : (float)record.activeFrames / totalActiveFrames;
        }

        return new ExerciseSummary
        {
            handType = runtime.handType,
            absoluteUsage = absolute,
            relativeUsage = relative,
            totalDurationSeconds = runtime.elapsedSeconds,
            totalActiveSeconds = totalActiveFrames / 115f // Leap ~115fps aprox
        };
    }
    
}

public struct ExerciseSummary
{
    public HandType handType;
    public float[] absoluteUsage;
    public float[] relativeUsage;
    public float totalDurationSeconds;
    public float totalActiveSeconds;
}