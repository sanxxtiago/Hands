using System;

[Serializable]
public struct ErgonomicExposureDimensionData
{
    public float degrees;
    public bool isMeasurementAvailable;
    public bool isMeasurementValid;
    public bool isEnabled;
    public bool isOutsideOptimalRange;
    public float cumulativeExposureSeconds;
    public float sustainedExposureSeconds;
    public bool hasReachedCumulativeExposureAlert;
    public bool hasReachedSustainedExposureThreshold;
    public bool crossedCumulativeExposureAlert;
    public bool crossedSustainedExposureThreshold;
}
