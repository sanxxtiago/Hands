using System;

public enum HybridCoordinationGoal
{
    ObserveOnly,
    IncludeWrist,
    IncludeForearm
}

public enum HybridSuggestionType
{
    NeutralWrist,
    ReduceTwist,
    RedistributeToForearm,
    IncludeWrist
}

public enum HybridSuggestionOutput
{
    LogOnly,
    LogAndSnackbar
}

[Serializable]
public struct FrameUsageData
{
    public long frameId;
    public float timestamp;
    public HandType handType;
    public bool isValid;
    public float observedSeconds;
    public float handActivityRatio;
    public float wristActivityRatio;
    public float forearmActivityRatio;
    public float meanRotationSignal;
    public float wristContribution;
    public float forearmContribution;
}

[Serializable]
public struct HybridSuggestionData
{
    public FrameUsageData usage;
    public FrameErgonomicExposureData exposure;
    public HybridSuggestionType type;
    public ErgonomicPostureDimension dimension;
    // Prioridad ordinal: 0 = objetivo motor, 1 = acumulada, 2 = sostenida.
    public int priority;
    public float conditionSeconds;

    public HandType HandType => exposure.handType;
    public long FrameId => exposure.frameId;
    public float Timestamp => exposure.timestamp;
    public ErgonomicExposureDimensionData TriggeringExposure =>
        dimension == ErgonomicPostureDimension.WristFlexionExtension ? exposure.wristFlexionExtension :
        (dimension == ErgonomicPostureDimension.WristRadialUlnarDeviation ?
            exposure.wristRadialUlnarDeviation : exposure.wristPronationSupination);

    public string Message
    {
        get
        {
            switch (type)
            {
                case HybridSuggestionType.ReduceTwist:
                    return "Has mantenido un giro fuera del rango configurado; reduce el giro sin forzar.";
                case HybridSuggestionType.RedistributeToForearm:
                    return "La muñeca está fuera de rango y concentra el movimiento; reduce su participación e involucra más el antebrazo.";
                case HybridSuggestionType.IncludeWrist:
                    return "La muñeca participa poco para el objetivo de este ejercicio; inclúyela manteniendo una postura neutra.";
                default:
                    return "Has mantenido la muñeca fuera del rango configurado; vuelve a una posición más neutra.";
            }
        }
    }
}
