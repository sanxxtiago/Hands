using System.Collections.Generic;

public static class MetricsProcessor
{
    public static NormalizedMetrics Normalize(RuntimeMetrics m)
    {
        float hand = m.usageByZone.GetValueOrDefault(MotionZone.Hand);
        float wrist = m.usageByZone.GetValueOrDefault(MotionZone.Wrist);
        float forearm = m.usageByZone.GetValueOrDefault(MotionZone.Forearm);

        float total = hand + wrist + forearm;

        if (total <= 0.01f) return new NormalizedMetrics();

        return new NormalizedMetrics
        {
            hand = hand / total,
            wrist = wrist / total,
            forearm = forearm / total
        };
    }

    public static Deviation GetDeviation(NormalizedMetrics m, HandProfile p)
    {
        return new Deviation
        {
            hand = (m.hand - p.hand.normalized) / p.hand.tolerance,
            wrist = (m.wrist - p.wrist.normalized) / p.wrist.tolerance,
            forearm = (m.forearm - p.forearm.normalized) / p.forearm.tolerance
        };
    }
}