using System;

[Serializable]
public struct ScoreBreakdown
{
    public string metricId;
    public float rawValue;
    public float metricScore;
    public float weight;

    public ScoreBreakdown(string metricId, float rawValue, float metricScore, float weight)
    {
        this.metricId = metricId;
        this.rawValue = rawValue;
        this.metricScore = metricScore;
        this.weight = weight;
    }
}
