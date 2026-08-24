using System;

internal static class ScoreMath
{
    private const float ScoreMinimum = 0f;
    private const float ScoreMaximum = 100f;
    private const float RatioEpsilon = 0.0001f;

    public static bool IsFinite(float value)
    {
        return !float.IsNaN(value)
            && !float.IsInfinity(value);
    }

    public static float NonNegativeFinite(float value)
    {
        if (!IsFinite(value) || value <= 0f)
            return 0f;

        return value;
    }

    public static float ClampScore(float value)
    {
        if (float.IsNaN(value) || value <= ScoreMinimum)
            return ScoreMinimum;

        if (float.IsPositiveInfinity(value) || value >= ScoreMaximum)
            return ScoreMaximum;

        return value;
    }

    public static float NormalizeLowerIsBetter(
        float ratio,
        float excellentRatio,
        float maximumRatio)
    {
        if (float.IsNaN(ratio) || float.IsPositiveInfinity(ratio))
            return ScoreMinimum;

        if (float.IsNegativeInfinity(ratio))
            return ScoreMaximum;

        float excellent = NonNegativeFinite(excellentRatio);
        float maximum = NonNegativeFinite(maximumRatio);

        if (maximum <= excellent)
            maximum = excellent + 1f;

        if (ratio <= excellent)
            return ScoreMaximum;

        if (ratio >= maximum)
            return ScoreMinimum;

        float normalized = (maximum - ratio) / (maximum - excellent);
        return ClampScore(normalized * ScoreMaximum);
    }

    public static float CalculateWeightedScore(ScoreBreakdown[] breakdown)
    {
        if (breakdown == null || breakdown.Length == 0)
            return ScoreMinimum;

        double weightedScore = 0d;
        double totalWeight = 0d;

        for (int i = 0; i < breakdown.Length; i++)
        {
            float weight = breakdown[i].weight;

            if (!IsFinite(weight) || weight <= 0f)
                continue;

            weightedScore += ClampScore(breakdown[i].metricScore) * weight;
            totalWeight += weight;
        }

        if (totalWeight <= RatioEpsilon)
            return ScoreMinimum;

        return (float)(weightedScore / totalWeight);
    }

    public static bool HasPositiveWeight(ScoreBreakdown[] breakdown)
    {
        if (breakdown == null)
            return false;

        for (int i = 0; i < breakdown.Length; i++)
        {
            if (IsFinite(breakdown[i].weight) && breakdown[i].weight > 0f)
                return true;
        }

        return false;
    }
}
