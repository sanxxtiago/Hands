using System;

public sealed class SuggestionDecisionOrchestrator
{
    private readonly float maximumGapSeconds;
    private readonly float cumulativeAlertSeconds;
    private readonly float sustainedThresholdSeconds;
    private readonly float minimumObservationSeconds;
    private readonly float minimumRotationSignal;
    private readonly float lowContribution;
    private readonly float highContribution;
    private readonly float coordinationHoldSeconds;
    private readonly float warmupSeconds;
    private readonly HybridCoordinationGoal leftGoal;
    private readonly HybridCoordinationGoal rightGoal;
    private readonly HandState[] states = new HandState[2];

    public SuggestionDecisionOrchestrator(HybridSuggestionProfile profile,
        ErgonomicCalibrationProfile calibration, HybridCoordinationGoal leftGoal,
        HybridCoordinationGoal rightGoal)
    {
        if (profile == null || !profile.TryValidate(out _) ||
            calibration == null || !calibration.TryValidate(out _) ||
            !Enum.IsDefined(typeof(HybridCoordinationGoal), leftGoal) ||
            !Enum.IsDefined(typeof(HybridCoordinationGoal), rightGoal))
            throw new ArgumentException("Configuración híbrida inválida.");
        maximumGapSeconds = calibration.MaximumFrameGapSeconds;
        cumulativeAlertSeconds = calibration.CumulativeExposureAlertSeconds;
        sustainedThresholdSeconds = calibration.SustainedExposureThresholdSeconds;
        minimumObservationSeconds = profile.MinimumObservationSeconds;
        minimumRotationSignal = profile.MinimumRotationSignal;
        lowContribution = profile.LowContribution;
        highContribution = profile.HighContribution;
        coordinationHoldSeconds = profile.CoordinationHoldSeconds;
        warmupSeconds = profile.WarmupSeconds;
        this.leftGoal = leftGoal;
        this.rightGoal = rightGoal;
    }

    public void Reset() => Array.Clear(states, 0, states.Length);

    public void BreakContinuity(HandType hand)
    {
        int index = Index(hand);
        if (index < 0) return;
        states[index].warmup = 0f;
        states[index].coordinationSeconds = 0f;
        states[index].continuous = false;
    }

    public bool TryEvaluate(FrameUsageData usage, FrameErgonomicExposureData exposure,
        out HybridSuggestionData suggestion)
    {
        suggestion = default;
        int index = Index(exposure.handType);
        if (index < 0) return false;
        if (usage.handType != exposure.handType || usage.frameId != exposure.frameId ||
            usage.timestamp != exposure.timestamp || !Finite(exposure.timestamp) || exposure.frameId <= 0)
        {
            BreakContinuity(exposure.handType);
            return false;
        }

        HandState state = states[index];
        if (state.hasPrevious && (exposure.frameId <= state.frameId || exposure.timestamp <= state.timestamp))
        {
            BreakContinuity(exposure.handType);
            return false;
        }

        float dt = state.hasPrevious ? exposure.timestamp - state.timestamp : 0f;
        bool continuous = state.continuous && dt > 0f && dt <= maximumGapSeconds;
        state.hasPrevious = true;
        state.frameId = exposure.frameId;
        state.timestamp = exposure.timestamp;
        state.continuous = true;
        if (!continuous)
        {
            state.warmup = state.coordinationSeconds = 0f;
            states[index] = state;
            return false;
        }

        bool allNeutral = true;
        bool allKnown = true;
        bool anyKnown = false;
        bool historicalAlert = false;
        int priority = 0;
        ErgonomicPostureDimension selected = ErgonomicPostureDimension.WristFlexionExtension;
        float selectedSeconds = 0f;
        for (int i = 0; i < 3; i++)
        {
            ErgonomicExposureDimensionData dimension = Dimension(exposure, i);
            bool known = Known(dimension);
            anyKnown |= known;
            allKnown &= known;
            allNeutral &= known && !dimension.isOutsideOptimalRange;
            historicalAlert |= dimension.hasReachedCumulativeExposureAlert ||
                dimension.hasReachedSustainedExposureThreshold;
            if (!known || !dimension.isOutsideOptimalRange) continue;

            // La bandera sostenida puede quedar registrada tras volver a neutral.
            // La protección actual se decide con el tiempo continuo actual.
            int candidatePriority = dimension.sustainedExposureSeconds >= sustainedThresholdSeconds
                ? 2 : (dimension.hasReachedCumulativeExposureAlert ||
                    dimension.cumulativeExposureSeconds >= cumulativeAlertSeconds ? 1 : 0);
            float seconds = candidatePriority == 2 ? dimension.sustainedExposureSeconds : dimension.cumulativeExposureSeconds;
            if (candidatePriority > priority ||
                (candidatePriority == priority && candidatePriority > 0 && seconds > selectedSeconds))
            {
                priority = candidatePriority;
                selected = (ErgonomicPostureDimension)i;
                selectedSeconds = seconds;
            }
        }

        if (!anyKnown)
        {
            BreakContinuity(exposure.handType);
            states[index].frameId = state.frameId;
            states[index].timestamp = state.timestamp;
            states[index].hasPrevious = true;
            return false;
        }

        state.warmup += dt;
        bool ready = UsageReady(usage);
        HybridCoordinationGoal goal = index == 0 ? leftGoal : rightGoal;
        bool twistNeutral = Known(exposure.wristPronationSupination) &&
            !exposure.wristPronationSupination.isOutsideOptimalRange;
        bool redistribute = priority > 0 && allKnown && twistNeutral && ready &&
            goal == HybridCoordinationGoal.IncludeForearm &&
            usage.wristContribution >= highContribution &&
            usage.forearmContribution <= lowContribution;
        bool includeWrist = priority == 0 && allNeutral && !historicalAlert && ready &&
            goal == HybridCoordinationGoal.IncludeWrist &&
            usage.wristContribution <= lowContribution &&
            usage.forearmContribution >= highContribution;
        int coordination = redistribute ? 1 : (includeWrist ? 2 : 0);
        state.coordinationSeconds = coordination != 0 && coordination == state.coordination
            ? state.coordinationSeconds + dt : 0f;
        state.coordination = coordination;
        states[index] = state;
        if (state.warmup < warmupSeconds) return false;

        HybridSuggestionType type;
        if (priority > 0)
        {
            type = selected == ErgonomicPostureDimension.WristPronationSupination
                ? HybridSuggestionType.ReduceTwist : HybridSuggestionType.NeutralWrist;
            if (redistribute && state.coordinationSeconds >= coordinationHoldSeconds)
                type = HybridSuggestionType.RedistributeToForearm;
        }
        else if (includeWrist && state.coordinationSeconds >= coordinationHoldSeconds)
        {
            type = HybridSuggestionType.IncludeWrist;
        }
        else return false;

        suggestion = new HybridSuggestionData
        {
            usage = usage,
            exposure = exposure,
            type = type,
            dimension = selected,
            priority = priority,
            conditionSeconds = priority > 0 ? selectedSeconds : state.coordinationSeconds
        };
        return true;
    }

    private bool UsageReady(FrameUsageData usage)
    {
        return usage.isValid && Finite(usage.observedSeconds) &&
            usage.observedSeconds >= minimumObservationSeconds &&
            Finite(usage.meanRotationSignal) && usage.meanRotationSignal >= minimumRotationSignal &&
            Unit(usage.wristContribution) && Unit(usage.forearmContribution) &&
            Math.Abs(usage.wristContribution + usage.forearmContribution - 1f) < 0.001f;
    }

    private static bool Known(ErgonomicExposureDimensionData data)
    {
        return data.isEnabled && data.isMeasurementAvailable && data.isMeasurementValid &&
            Finite(data.degrees) && Finite(data.cumulativeExposureSeconds) &&
            data.cumulativeExposureSeconds >= 0f && Finite(data.sustainedExposureSeconds) &&
            data.sustainedExposureSeconds >= 0f;
    }

    private static ErgonomicExposureDimensionData Dimension(FrameErgonomicExposureData frame, int index)
    {
        return index == 0 ? frame.wristFlexionExtension :
            (index == 1 ? frame.wristRadialUlnarDeviation : frame.wristPronationSupination);
    }

    private static int Index(HandType hand) => hand == HandType.LEFT ? 0 : (hand == HandType.RIGHT ? 1 : -1);
    private static bool Unit(float value) => Finite(value) && value >= 0f && value <= 1f;
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private struct HandState
    {
        public bool hasPrevious;
        public bool continuous;
        public long frameId;
        public float timestamp;
        public float warmup;
        public int coordination;
        public float coordinationSeconds;
    }
}
