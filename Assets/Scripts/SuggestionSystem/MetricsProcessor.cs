using System.Collections.Generic;
using UnityEngine;

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
        const float minimumTolerance = 0.0001f;

        return new Deviation
        {
            hand = (m.hand - p.hand.normalized) /
                Mathf.Max(p.hand.tolerance, minimumTolerance),
            wrist = (m.wrist - p.wrist.normalized) /
                Mathf.Max(p.wrist.tolerance, minimumTolerance),
            forearm = (m.forearm - p.forearm.normalized) /
                Mathf.Max(p.forearm.tolerance, minimumTolerance)
        };
    }
}
