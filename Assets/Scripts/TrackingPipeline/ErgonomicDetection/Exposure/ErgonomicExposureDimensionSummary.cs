using System;

[Serializable]
public struct ErgonomicExposureDimensionSummary
{
    public float validObservationSeconds;
    public float maximumSustainedExposureSeconds;
    public float cumulativeExposureSeconds;
    public float sustainedExposureSeconds;
    public bool hasReachedCumulativeExposureAlert;
    public bool hasReachedSustainedExposureThreshold;
}
