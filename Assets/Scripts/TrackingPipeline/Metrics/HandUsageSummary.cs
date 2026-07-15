using System;

[Serializable]
public struct HandUsageSummary
{
    public HandType handType;

    public MotionZone[] zones;

    public float[] absoluteUsage;
    public float[] relativeUsage;
    public float[] intensity;

    //public float totalDurationSeconds;
    public float totalActiveSeconds;

    public float activityRatio;
}