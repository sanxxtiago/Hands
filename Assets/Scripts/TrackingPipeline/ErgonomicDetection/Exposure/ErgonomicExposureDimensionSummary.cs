using System;

[Serializable]
public struct ErgonomicExposureDimensionSummary
{
    public float cumulativeExposureSeconds;
    public float sustainedExposureSeconds;
    public bool hasReachedCumulativeExposureAlert;
    public bool hasReachedSustainedExposureThreshold;
}
