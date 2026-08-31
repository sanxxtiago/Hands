using System;
using System.Collections.Generic;

[Serializable]
public sealed class ExpositionSummary
{
    public string sessionGuid;
    public int sessionId;
    public int exerciseIndex;
    public ExerciseType exerciseType;
    public float exerciseDuration;
    public HandExpositionSummary leftHand;
    public HandExpositionSummary rightHand;
}

[Serializable]
public sealed class HandExpositionSummary
{
    public HandType handType;
    public ExpositionDimensionSummary wristFlexionExtension;
    public ExpositionDimensionSummary wristRadialUlnarDeviation;
    public ExpositionDimensionSummary wristPronationSupination;
}

[Serializable]
public struct ExpositionDimensionSummary
{
    public float validObservationSeconds;
    public float maximumSustainedExposureSeconds;
    public float cumulativeExposureSeconds;
    public float sustainedExposureSeconds;
    public bool hasReachedCumulativeExposureAlert;
    public bool hasReachedSustainedExposureThreshold;
}

public sealed class ExpositionsData
{
    public List<ExpositionSummary> Records { get; set; } = new();
}
