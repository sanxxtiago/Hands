using UnityEngine;

public sealed class ErgonomicExposureInterpreter
{
    private readonly HandType _handType;
    private readonly ErgonomicCalibrationProfile _profile;
    private readonly DimensionState[] _states = new DimensionState[3];

    private bool _hasPreviousTimestamp;
    private float _previousTimestamp;
    private bool _isConfigurationValid;

    public ErgonomicExposureInterpreter(
        HandType handType,
        ErgonomicCalibrationProfile profile)
    {
        _handType = handType;
        _profile = profile;
        _isConfigurationValid = profile != null &&
            profile.TryValidate(out _);
    }

    public bool IsConfigurationValid => _isConfigurationValid;

    public void Reset()
    {
        for (int i = 0; i < _states.Length; i++)
            _states[i] = default;

        _hasPreviousTimestamp = false;
        _previousTimestamp = 0f;
        _isConfigurationValid = _profile != null &&
            _profile.TryValidate(out _);
    }

    public bool TryProcess(
        FrameErgonomicData frame,
        out FrameErgonomicExposureData exposureFrame)
    {
        exposureFrame = default;

        if (!_isConfigurationValid || frame.handType != _handType)
        {
            return false;
        }

        if (!IsFinite(frame.timestamp))
        {
            ResetSustainedExposure();
            return false;
        }

        bool hasUsableDelta = TryGetDelta(frame.timestamp, out float deltaTime);

        exposureFrame = new FrameErgonomicExposureData
        {
            frameId = frame.frameId,
            timestamp = frame.timestamp,
            handType = frame.handType,
            wristFlexionExtension = ProcessDimension(
                ErgonomicPostureDimension.WristFlexionExtension,
                frame.wristFlexionExtension,
                hasUsableDelta,
                deltaTime),
            wristRadialUlnarDeviation = ProcessDimension(
                ErgonomicPostureDimension.WristRadialUlnarDeviation,
                frame.wristRadialUlnarDeviation,
                hasUsableDelta,
                deltaTime),
            wristPronationSupination = ProcessDimension(
                ErgonomicPostureDimension.WristPronationSupination,
                frame.wristPronationSupination,
                hasUsableDelta,
                deltaTime)
        };

        return true;
    }

    public HandErgonomicExposureSummary GetSummary()
    {
        return new HandErgonomicExposureSummary
        {
            handType = _handType,
            wristFlexionExtension = BuildSummary(
                ErgonomicPostureDimension.WristFlexionExtension),
            wristRadialUlnarDeviation = BuildSummary(
                ErgonomicPostureDimension.WristRadialUlnarDeviation),
            wristPronationSupination = BuildSummary(
                ErgonomicPostureDimension.WristPronationSupination)
        };
    }

    private bool TryGetDelta(float timestamp, out float deltaTime)
    {
        deltaTime = 0f;

        if (!_hasPreviousTimestamp)
        {
            _previousTimestamp = timestamp;
            _hasPreviousTimestamp = true;
            return false;
        }

        float rawDelta = timestamp - _previousTimestamp;
        _previousTimestamp = timestamp;

        return rawDelta > 0f && rawDelta <= _profile.MaximumFrameGapSeconds &&
            IsFinite(rawDelta) && (deltaTime = rawDelta) > 0f;
    }

    private ErgonomicExposureDimensionData ProcessDimension(
        ErgonomicPostureDimension dimension,
        ErgonomicAngleData angle,
        bool hasUsableDelta,
        float deltaTime)
    {
        int stateIndex = (int)dimension;
        DimensionState state = _states[stateIndex];
        _profile.TryGetCalibration(dimension, out ErgonomicAngleCalibration calibration);

        bool isMeasurementValid = angle.isAvailable && angle.isValid &&
            IsFinite(angle.degrees);

        ErgonomicExposureDimensionData data = new ErgonomicExposureDimensionData
        {
            degrees = angle.degrees,
            isMeasurementAvailable = angle.isAvailable,
            isMeasurementValid = isMeasurementValid,
            isEnabled = calibration.IsEnabled,
            cumulativeExposureSeconds = state.cumulativeExposureSeconds,
            sustainedExposureSeconds = state.sustainedExposureSeconds,
            hasReachedCumulativeExposureAlert =
                state.hasReachedCumulativeExposureAlert,
            hasReachedSustainedExposureThreshold =
                state.hasReachedSustainedExposureThreshold
        };

        if (!calibration.IsEnabled)
        {
            state.sustainedExposureSeconds = 0f;
            _states[stateIndex] = state;
            return data;
        }

        if (!isMeasurementValid || !hasUsableDelta)
        {
            state.sustainedExposureSeconds = 0f;
            _states[stateIndex] = state;
            data.sustainedExposureSeconds = 0f;
            return data;
        }

        data.isOutsideOptimalRange =
            !calibration.IsWithinOptimalRange(angle.degrees);

        if (!data.isOutsideOptimalRange)
        {
            state.sustainedExposureSeconds = 0f;
            _states[stateIndex] = state;
            data.sustainedExposureSeconds = 0f;
            return data;
        }

        bool hadCumulativeAlert = state.hasReachedCumulativeExposureAlert;
        bool hadSustainedAlert = state.hasReachedSustainedExposureThreshold;

        state.cumulativeExposureSeconds += deltaTime;
        state.sustainedExposureSeconds += deltaTime;
        state.hasReachedCumulativeExposureAlert =
            state.cumulativeExposureSeconds >=
            _profile.CumulativeExposureAlertSeconds;
        state.hasReachedSustainedExposureThreshold =
            state.sustainedExposureSeconds >=
            _profile.SustainedExposureThresholdSeconds;
        _states[stateIndex] = state;

        data.cumulativeExposureSeconds = state.cumulativeExposureSeconds;
        data.sustainedExposureSeconds = state.sustainedExposureSeconds;
        data.hasReachedCumulativeExposureAlert =
            state.hasReachedCumulativeExposureAlert;
        data.hasReachedSustainedExposureThreshold =
            state.hasReachedSustainedExposureThreshold;
        data.crossedCumulativeExposureAlert =
            !hadCumulativeAlert && state.hasReachedCumulativeExposureAlert;
        data.crossedSustainedExposureThreshold =
            !hadSustainedAlert && state.hasReachedSustainedExposureThreshold;
        return data;
    }

    private ErgonomicExposureDimensionSummary BuildSummary(
        ErgonomicPostureDimension dimension)
    {
        DimensionState state = _states[(int)dimension];
        return new ErgonomicExposureDimensionSummary
        {
            cumulativeExposureSeconds = state.cumulativeExposureSeconds,
            sustainedExposureSeconds = state.sustainedExposureSeconds,
            hasReachedCumulativeExposureAlert =
                state.hasReachedCumulativeExposureAlert,
            hasReachedSustainedExposureThreshold =
                state.hasReachedSustainedExposureThreshold
        };
    }

    private void ResetSustainedExposure()
    {
        for (int i = 0; i < _states.Length; i++)
        {
            DimensionState state = _states[i];
            state.sustainedExposureSeconds = 0f;
            _states[i] = state;
        }
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private struct DimensionState
    {
        public float cumulativeExposureSeconds;
        public float sustainedExposureSeconds;
        public bool hasReachedCumulativeExposureAlert;
        public bool hasReachedSustainedExposureThreshold;
    }
}
