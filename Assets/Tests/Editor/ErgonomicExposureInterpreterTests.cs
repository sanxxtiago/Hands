#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class ErgonomicExposureInterpreterTests
{
    private const BindingFlags InstancePrivate =
        BindingFlags.Instance | BindingFlags.NonPublic;

    private ErgonomicCalibrationProfile _profile;

    [SetUp]
    public void SetUp()
    {
        _profile = ScriptableObject.CreateInstance<ErgonomicCalibrationProfile>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_profile);
    }

    [Test]
    public void Profile_DefaultValuesAreCompleteAndValid()
    {
        Assert.That(_profile.TryValidate(out string validationError), Is.True,
            validationError);
        Assert.That(
            _profile.TryGetCalibration(
                ErgonomicPostureDimension.WristFlexionExtension,
                out ErgonomicAngleCalibration flexionExtension),
            Is.True);
        Assert.That(flexionExtension.MinimumOptimalDegrees, Is.EqualTo(-15f));
        Assert.That(flexionExtension.MaximumOptimalDegrees, Is.EqualTo(15f));
        Assert.That(
            _profile.TryGetCalibration(
                ErgonomicPostureDimension.WristRadialUlnarDeviation,
                out ErgonomicAngleCalibration radialUlnar),
            Is.True);
        Assert.That(radialUlnar.IsEnabled, Is.True);
        Assert.That(
            _profile.TryGetCalibration(
                ErgonomicPostureDimension.WristPronationSupination,
                out ErgonomicAngleCalibration pronationSupination),
            Is.True);
        Assert.That(pronationSupination.IsEnabled, Is.True);
    }

    [Test]
    public void Profile_InvalidTemporalValueAndDuplicateDimensionAreRejected()
    {
        SetPrivateField(_profile, "cumulativeExposureAlertSeconds", 0f);

        Assert.That(_profile.TryValidate(out string timeValidationError), Is.False);
        Assert.That(timeValidationError, Does.Contain("temporales"));

        SetPrivateField(_profile, "cumulativeExposureAlertSeconds", 60f);
        SetPrivateField(
            _profile,
            "angleCalibrations",
            new List<ErgonomicAngleCalibration>
            {
                new ErgonomicAngleCalibration(
                    ErgonomicPostureDimension.WristFlexionExtension,
                    -15f,
                    15f,
                    string.Empty),
                new ErgonomicAngleCalibration(
                    ErgonomicPostureDimension.WristFlexionExtension,
                    -15f,
                    15f,
                    string.Empty),
                new ErgonomicAngleCalibration(
                    ErgonomicPostureDimension.WristPronationSupination,
                    -45f,
                    45f,
                    string.Empty)
            });

        Assert.That(_profile.TryValidate(out string duplicateValidationError),
            Is.False);
        Assert.That(duplicateValidationError, Does.Contain("duplicada"));
    }

    [Test]
    public void TryProcess_AccumulatesAllDimensionsAndResetsOnlySustainedExposure()
    {
        ErgonomicExposureInterpreter interpreter =
            new ErgonomicExposureInterpreter(HandType.LEFT, _profile);

        interpreter.TryProcess(CreateFrame(0f, HandType.LEFT, 20f, 20f, 50f),
            out _);
        interpreter.TryProcess(CreateFrame(0.2f, HandType.LEFT, 20f, 20f, 50f),
            out FrameErgonomicExposureData outsideFrame);
        interpreter.TryProcess(CreateFrame(0.4f, HandType.LEFT, 0f, 0f, 0f),
            out FrameErgonomicExposureData neutralFrame);

        AssertDimensionExposure(
            outsideFrame.wristFlexionExtension,
            0.2f,
            0.2f,
            true);
        AssertDimensionExposure(
            outsideFrame.wristRadialUlnarDeviation,
            0.2f,
            0.2f,
            true);
        AssertDimensionExposure(
            outsideFrame.wristPronationSupination,
            0.2f,
            0.2f,
            true);
        AssertDimensionExposure(
            neutralFrame.wristFlexionExtension,
            0.2f,
            0f,
            false);
        AssertDimensionExposure(
            neutralFrame.wristRadialUlnarDeviation,
            0.2f,
            0f,
            false);
        AssertDimensionExposure(
            neutralFrame.wristPronationSupination,
            0.2f,
            0f,
            false);
    }

    [Test]
    public void TryProcess_CrossesExposureThresholdsOnlyOnce()
    {
        SetPrivateField(_profile, "cumulativeExposureAlertSeconds", 0.4f);
        SetPrivateField(_profile, "sustainedExposureThresholdSeconds", 0.4f);

        ErgonomicExposureInterpreter interpreter =
            new ErgonomicExposureInterpreter(HandType.LEFT, _profile);

        interpreter.TryProcess(CreateFrame(0f, HandType.LEFT, 20f, 0f, 0f),
            out _);
        interpreter.TryProcess(CreateFrame(0.2f, HandType.LEFT, 20f, 0f, 0f),
            out FrameErgonomicExposureData firstOutsideFrame);
        interpreter.TryProcess(CreateFrame(0.4f, HandType.LEFT, 20f, 0f, 0f),
            out FrameErgonomicExposureData thresholdFrame);
        interpreter.TryProcess(CreateFrame(0.6f, HandType.LEFT, 20f, 0f, 0f),
            out FrameErgonomicExposureData afterThresholdFrame);

        Assert.That(
            firstOutsideFrame.wristFlexionExtension.crossedCumulativeExposureAlert,
            Is.False);
        Assert.That(
            thresholdFrame.wristFlexionExtension.crossedCumulativeExposureAlert,
            Is.True);
        Assert.That(
            thresholdFrame.wristFlexionExtension.crossedSustainedExposureThreshold,
            Is.True);
        Assert.That(
            afterThresholdFrame.wristFlexionExtension.crossedCumulativeExposureAlert,
            Is.False);
        Assert.That(
            afterThresholdFrame.wristFlexionExtension.crossedSustainedExposureThreshold,
            Is.False);
    }

    [Test]
    public void TryProcess_InvalidTimestampAndTrackingGapDoNotAddExposure()
    {
        ErgonomicExposureInterpreter interpreter =
            new ErgonomicExposureInterpreter(HandType.RIGHT, _profile);

        interpreter.TryProcess(CreateFrame(0f, HandType.RIGHT, 20f, 0f, 0f),
            out _);
        interpreter.TryProcess(CreateFrame(0.2f, HandType.RIGHT, 20f, 0f, 0f),
            out FrameErgonomicExposureData accumulatedFrame);
        interpreter.TryProcess(CreateFrame(0.2f, HandType.RIGHT, 20f, 0f, 0f),
            out FrameErgonomicExposureData duplicateTimestampFrame);
        FrameErgonomicData invalidMeasurementFrame =
            CreateFrame(0.3f, HandType.RIGHT, 20f, 0f, 0f);
        invalidMeasurementFrame.wristFlexionExtension.isValid = false;
        interpreter.TryProcess(invalidMeasurementFrame,
            out FrameErgonomicExposureData invalidMeasurementResult);
        interpreter.TryProcess(CreateFrame(0.4f, HandType.RIGHT, 20f, 0f, 0f),
            out FrameErgonomicExposureData resumedFrame);
        interpreter.TryProcess(CreateFrame(1f, HandType.RIGHT, 20f, 0f, 0f),
            out FrameErgonomicExposureData gapFrame);

        Assert.That(
            accumulatedFrame.wristFlexionExtension.cumulativeExposureSeconds,
            Is.EqualTo(0.2f).Within(0.0001f));
        Assert.That(
            duplicateTimestampFrame.wristFlexionExtension.cumulativeExposureSeconds,
            Is.EqualTo(0.2f).Within(0.0001f));
        Assert.That(
            duplicateTimestampFrame.wristFlexionExtension.sustainedExposureSeconds,
            Is.Zero);
        Assert.That(
            invalidMeasurementResult.wristFlexionExtension.cumulativeExposureSeconds,
            Is.EqualTo(0.2f).Within(0.0001f));
        Assert.That(
            invalidMeasurementResult.wristFlexionExtension.sustainedExposureSeconds,
            Is.Zero);
        Assert.That(
            resumedFrame.wristFlexionExtension.cumulativeExposureSeconds,
            Is.EqualTo(0.3f).Within(0.0001f));
        Assert.That(
            resumedFrame.wristFlexionExtension.sustainedExposureSeconds,
            Is.EqualTo(0.1f).Within(0.0001f));
        Assert.That(
            gapFrame.wristFlexionExtension.cumulativeExposureSeconds,
            Is.EqualTo(0.3f).Within(0.0001f));
        Assert.That(gapFrame.wristFlexionExtension.sustainedExposureSeconds,
            Is.Zero);
    }

    [Test]
    public void TryProcess_NonFiniteTimestampResetsSustainedExposure()
    {
        ErgonomicExposureInterpreter interpreter =
            new ErgonomicExposureInterpreter(HandType.RIGHT, _profile);

        interpreter.TryProcess(CreateFrame(0f, HandType.RIGHT, 20f, 0f, 0f),
            out _);
        interpreter.TryProcess(CreateFrame(0.2f, HandType.RIGHT, 20f, 0f, 0f),
            out _);

        FrameErgonomicData nonFiniteTimestampFrame =
            CreateFrame(0f, HandType.RIGHT, 20f, 0f, 0f);
        nonFiniteTimestampFrame.timestamp = float.NaN;

        Assert.That(
            interpreter.TryProcess(nonFiniteTimestampFrame, out _),
            Is.False);

        interpreter.TryProcess(CreateFrame(0.3f, HandType.RIGHT, 20f, 0f, 0f),
            out FrameErgonomicExposureData resumedFrame);

        Assert.That(
            resumedFrame.wristFlexionExtension.cumulativeExposureSeconds,
            Is.EqualTo(0.3f).Within(0.0001f));
        Assert.That(
            resumedFrame.wristFlexionExtension.sustainedExposureSeconds,
            Is.EqualTo(0.1f).Within(0.0001f));
    }

    [Test]
    public void Interpreters_KeepHandsIndependentAndResetTheirState()
    {
        ErgonomicExposureInterpreter leftInterpreter =
            new ErgonomicExposureInterpreter(HandType.LEFT, _profile);
        ErgonomicExposureInterpreter rightInterpreter =
            new ErgonomicExposureInterpreter(HandType.RIGHT, _profile);

        leftInterpreter.TryProcess(CreateFrame(0f, HandType.LEFT, 20f, 0f, 0f),
            out _);
        leftInterpreter.TryProcess(CreateFrame(0.2f, HandType.LEFT, 20f, 0f, 0f),
            out _);
        rightInterpreter.TryProcess(CreateFrame(0f, HandType.RIGHT, 0f, 0f, 0f),
            out _);
        rightInterpreter.TryProcess(CreateFrame(0.2f, HandType.RIGHT, 0f, 0f, 0f),
            out _);

        Assert.That(
            leftInterpreter.GetSummary().wristFlexionExtension
                .cumulativeExposureSeconds,
            Is.EqualTo(0.2f).Within(0.0001f));
        Assert.That(
            rightInterpreter.GetSummary().wristFlexionExtension
                .cumulativeExposureSeconds,
            Is.Zero);

        leftInterpreter.Reset();

        Assert.That(
            leftInterpreter.GetSummary().wristFlexionExtension
                .cumulativeExposureSeconds,
            Is.Zero);
    }

    [Test]
    public void TrackingSystem_PublishesFramesAndSummaryWithoutMetricsDependency()
    {
        GameObject trackingObject = new GameObject("ErgonomicExposureTracking");
        trackingObject.SetActive(false);
        ErgonomicExposureTrackingSystem trackingSystem =
            trackingObject.AddComponent<ErgonomicExposureTrackingSystem>();
        SetPrivateField(trackingSystem, "calibrationProfile", _profile);
        InvokePrivate(trackingSystem, "CreateInterpreters");

        int receivedFrames = 0;
        bool receivedSummary = false;
        HandErgonomicExposureSummary leftSummary = default;
        HandErgonomicExposureSummary rightSummary = default;

        void CaptureFrame(FrameErgonomicExposureData frame) => receivedFrames++;
        void CaptureSummary(
            float duration,
            HandErgonomicExposureSummary left,
            HandErgonomicExposureSummary right)
        {
            receivedSummary = true;
            leftSummary = left;
            rightSummary = right;
        }

        ErgonomicExposureEventBus.OnFrame += CaptureFrame;
        ErgonomicExposureEventBus.OnTrackingStop += CaptureSummary;

        try
        {
            InvokePrivate(trackingSystem, "OnEnable");
            trackingSystem.RunTracking();

            ErgonomicEventBus.Publish(
                CreateFrame(0f, HandType.LEFT, 20f, 0f, 0f));
            ErgonomicEventBus.Publish(
                CreateFrame(0.2f, HandType.LEFT, 20f, 0f, 0f));
            trackingSystem.StopTracking(1f);

            Assert.That(receivedFrames, Is.EqualTo(2));
            Assert.That(receivedSummary, Is.True);
            Assert.That(
                leftSummary.wristFlexionExtension.cumulativeExposureSeconds,
                Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(
                rightSummary.wristFlexionExtension.cumulativeExposureSeconds,
                Is.Zero);
        }
        finally
        {
            ErgonomicExposureEventBus.OnFrame -= CaptureFrame;
            ErgonomicExposureEventBus.OnTrackingStop -= CaptureSummary;
            InvokePrivate(trackingSystem, "OnDisable");
            Object.DestroyImmediate(trackingObject);
        }
    }

    private static FrameErgonomicData CreateFrame(
        float timestamp,
        HandType handType,
        float flexionExtension,
        float radialUlnar,
        float pronationSupination)
    {
        return new FrameErgonomicData
        {
            frameId = (long)(timestamp * 100f) + 1,
            timestamp = timestamp,
            handType = handType,
            wristFlexionExtension = CreateAngle(flexionExtension),
            wristRadialUlnarDeviation = CreateAngle(radialUlnar),
            wristPronationSupination = CreateAngle(pronationSupination)
        };
    }

    private static ErgonomicAngleData CreateAngle(float degrees)
    {
        return new ErgonomicAngleData
        {
            degrees = degrees,
            isAvailable = true,
            isValid = true
        };
    }

    private static void AssertDimensionExposure(
        ErgonomicExposureDimensionData data,
        float expectedCumulative,
        float expectedSustained,
        bool expectedOutside)
    {
        Assert.That(data.isOutsideOptimalRange, Is.EqualTo(expectedOutside));
        Assert.That(data.cumulativeExposureSeconds,
            Is.EqualTo(expectedCumulative).Within(0.0001f));
        Assert.That(data.sustainedExposureSeconds,
            Is.EqualTo(expectedSustained).Within(0.0001f));
    }

    private static void SetPrivateField<TTarget, TValue>(
        TTarget target,
        string fieldName,
        TValue value)
    {
        FieldInfo field = typeof(TTarget).GetField(
            fieldName,
            InstancePrivate);

        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            InstancePrivate);

        Assert.That(method, Is.Not.Null);
        method.Invoke(target, null);
    }
}
#endif
