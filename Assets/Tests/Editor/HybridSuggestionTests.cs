#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEditor.SceneManagement;

public sealed class HybridSuggestionTests
{
    private HybridSuggestionProfile profile;
    private ErgonomicCalibrationProfile calibration;

    [SetUp]
    public void SetUp()
    {
        profile = ScriptableObject.CreateInstance<HybridSuggestionProfile>();
        calibration = ScriptableObject.CreateInstance<ErgonomicCalibrationProfile>();
        Set(profile, "warmupSeconds", 0.1f);
        Set(profile, "coordinationHoldSeconds", 0.2f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(profile);
        Object.DestroyImmediate(calibration);
    }

    [Test]
    public void Profile_RejectsNonFiniteValuesAndInvertedContributions()
    {
        Assert.That(profile.TryValidate(out _), Is.True);
        Set(profile, "usageWindowSeconds", float.NaN);
        Assert.That(profile.TryValidate(out _), Is.False);
        Set(profile, "usageWindowSeconds", 5f);
        Set(profile, "lowContribution", 0.8f);
        Assert.That(profile.TryValidate(out _), Is.False);
        Set(profile, "lowContribution", 0.35f);
        Set(profile, "minimumObservationSeconds", 6f);
        Assert.That(profile.TryValidate(out _), Is.False);
    }

    [Test]
    public void Usage_WeightsByTimeAndTrimsPartialIntervals()
    {
        UsageInterpreter interpreter = new UsageInterpreter(HandType.LEFT, 0.3f, 0.25f);
        interpreter.TryProcess(Motion(1, 0f, 1f, 0f), out FrameUsageData first);
        Assert.That(first.isValid, Is.False);
        interpreter.TryProcess(Motion(2, 0.2f, 1f, 0f), out _);
        interpreter.TryProcess(Motion(3, 0.3f, 0f, 1f), out FrameUsageData weighted);
        Assert.That(weighted.wristContribution, Is.EqualTo(2f / 3f).Within(0.0001f));
        Assert.That(weighted.handActivityRatio, Is.EqualTo(1f).Within(0.0001f));
        interpreter.TryProcess(Motion(4, 0.4f, 0f, 1f), out FrameUsageData trimmed);
        Assert.That(trimmed.observedSeconds, Is.EqualTo(0.3f).Within(0.0001f));
        Assert.That(trimmed.wristContribution, Is.EqualTo(1f / 3f).Within(0.0001f));
        Assert.That(trimmed.forearmContribution, Is.EqualTo(2f / 3f).Within(0.0001f));
    }

    [Test]
    public void Usage_GapsMissingZonesAndStaleFramesCannotInventParticipation()
    {
        UsageInterpreter interpreter = new UsageInterpreter(HandType.LEFT, 5f, 0.25f);
        interpreter.TryProcess(Motion(1, 0f), out _);
        interpreter.TryProcess(Motion(2, 0.2f), out _);
        Assert.That(interpreter.TryProcess(Motion(2, 0.2f), out _), Is.False);
        interpreter.TryProcess(Motion(3, 0.3f), out FrameUsageData afterDuplicate);
        Assert.That(afterDuplicate.observedSeconds, Is.Zero);
        interpreter.TryProcess(Motion(4, 1f), out FrameUsageData afterGap);
        Assert.That(afterGap.isValid, Is.False);
        FrameMotionData missing = Motion(5, 1.1f);
        missing.motions.RemoveAt(0);
        interpreter.TryProcess(missing, out FrameUsageData invalid);
        Assert.That(invalid.isValid, Is.False);
        interpreter.TryProcess(Motion(6, 1.2f), out FrameUsageData resume);
        Assert.That(resume.observedSeconds, Is.Zero);
        Assert.That(interpreter.TryProcess(Motion(7, float.NaN), out _), Is.False);
        Assert.That(interpreter.TryProcess(Motion(8, 0.5f), out _), Is.False);
        interpreter.Reset();
        interpreter.TryProcess(Motion(1, 0f), out FrameUsageData restart);
        Assert.That(restart.observedSeconds, Is.Zero);
    }

    [Test]
    public void Usage_ZeroSignalHasNoArtificialContribution()
    {
        UsageInterpreter interpreter = new UsageInterpreter(HandType.LEFT, 5f, 0.25f);
        interpreter.TryProcess(Motion(1, 0f, 0f, 0f), out _);
        interpreter.TryProcess(Motion(2, 0.2f, 0f, 0f), out FrameUsageData result);
        Assert.That(result.meanRotationSignal, Is.Zero);
        Assert.That(result.wristContribution, Is.Zero);
        Assert.That(result.forearmContribution, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Pairer_JoinsEitherOrderExactlyOnce(bool exposureFirst)
    {
        HybridFramePairer pairer = new HybridFramePairer(HandType.LEFT);
        FrameUsageData usage = Usage(1, 1f);
        FrameErgonomicExposureData exposure = Exposure(1, 1f);
        Assert.That(exposureFirst ? pairer.Push(exposure) : pairer.Push(usage), Is.EqualTo(HybridPairStatus.Waiting));
        Assert.That(exposureFirst ? pairer.Push(usage) : pairer.Push(exposure), Is.EqualTo(HybridPairStatus.Ready));
        Assert.That(pairer.Usage.frameId, Is.EqualTo(pairer.Exposure.frameId));
        Assert.That(pairer.Push(usage), Is.EqualTo(HybridPairStatus.Rejected));
        Assert.That(pairer.Push(exposure), Is.EqualTo(HybridPairStatus.Rejected));
    }

    [Test]
    public void Pairer_RejectsTimestampMismatchAndDifferentHands()
    {
        HybridFramePairer pairer = new HybridFramePairer(HandType.LEFT);
        pairer.Push(Usage(1, 1f));
        Assert.That(pairer.Push(Exposure(1, 1.1f)), Is.EqualTo(HybridPairStatus.Rejected));
        FrameErgonomicExposureData right = Exposure(2, 2f);
        right.handType = HandType.RIGHT;
        Assert.That(pairer.Push(right), Is.EqualTo(HybridPairStatus.Rejected));
        pairer.Push(Usage(3, 3f));
        Assert.That(pairer.Push(Exposure(2, 2f)), Is.EqualTo(HybridPairStatus.Rejected));
        Assert.That(pairer.Push(Exposure(3, 3f)), Is.EqualTo(HybridPairStatus.Ready));
    }

    [Test]
    public void Decision_HighWristUseAloneDoesNotMeanPosturalExposure()
    {
        SuggestionDecisionOrchestrator decision = Decision(HybridCoordinationGoal.IncludeForearm);
        for (int i = 1; i <= 8; i++)
            Assert.That(decision.TryEvaluate(Usage(i, i * 0.2f, 0.9f), Exposure(i, i * 0.2f), out _), Is.False);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void Decision_ProtectsEveryExposedDimensionEvenWithoutUsage(int dimension)
    {
        SuggestionDecisionOrchestrator decision = Decision();
        HybridSuggestionData last = default;
        bool emitted = false;
        for (int i = 1; i <= 5; i++)
        {
            FrameUsageData usage = Usage(i, i * 0.2f);
            usage.isValid = false;
            FrameErgonomicExposureData exposure = Exposure(i, i * 0.2f, dimension);
            emitted = decision.TryEvaluate(usage, exposure, out last);
        }
        Assert.That(emitted, Is.True);
        Assert.That(last.priority, Is.EqualTo(2));
        Assert.That(last.dimension, Is.EqualTo((ErgonomicPostureDimension)dimension));
        Assert.That(last.type, Is.EqualTo(dimension == 2 ? HybridSuggestionType.ReduceTwist : HybridSuggestionType.NeutralWrist));
    }

    [Test]
    public void Decision_RedistributionRequiresGoalAndCurrentExposure()
    {
        SuggestionDecisionOrchestrator withGoal = Decision(HybridCoordinationGoal.IncludeForearm);
        SuggestionDecisionOrchestrator observe = Decision();
        HybridSuggestionData coordinated = default;
        HybridSuggestionData protection = default;
        for (int i = 1; i <= 6; i++)
        {
            withGoal.TryEvaluate(Usage(i, i * 0.2f, 0.9f), Exposure(i, i * 0.2f, 0), out coordinated);
            observe.TryEvaluate(Usage(i, i * 0.2f, 0.9f), Exposure(i, i * 0.2f, 0), out protection);
        }
        Assert.That(coordinated.type, Is.EqualTo(HybridSuggestionType.RedistributeToForearm));
        Assert.That(protection.type, Is.EqualTo(HybridSuggestionType.NeutralWrist));
    }

    [Test]
    public void Decision_DoesNotRecommendForearmWhenTwistIsOutsideRange()
    {
        SuggestionDecisionOrchestrator decision = Decision(HybridCoordinationGoal.IncludeForearm);
        for (int i = 1; i <= 6; i++)
        {
            FrameErgonomicExposureData exposure = Exposure(i, i * 0.2f, 0);
            exposure.wristPronationSupination.isOutsideOptimalRange = true;
            if (decision.TryEvaluate(Usage(i, i * 0.2f, 0.9f), exposure, out HybridSuggestionData result))
                Assert.That(result.type, Is.EqualTo(HybridSuggestionType.NeutralWrist));
        }
    }

    [Test]
    public void Decision_IncludeWristRequiresGoalNeutralityAndEnoughSignal()
    {
        SuggestionDecisionOrchestrator decision = Decision(HybridCoordinationGoal.IncludeWrist);
        HybridSuggestionData last = default;
        bool emitted = false;
        for (int i = 1; i <= 6; i++)
            emitted = decision.TryEvaluate(Usage(i, i * 0.2f, 0.1f), Exposure(i, i * 0.2f), out last);
        Assert.That(emitted, Is.True);
        Assert.That(last.type, Is.EqualTo(HybridSuggestionType.IncludeWrist));
        FrameErgonomicExposureData outside = Exposure(7, 1.4f);
        outside.wristFlexionExtension.isOutsideOptimalRange = true;
        Assert.That(decision.TryEvaluate(Usage(7, 1.4f, 0.1f), outside, out _), Is.False);
        FrameUsageData noSignal = Usage(8, 1.6f, 0.1f);
        noSignal.meanRotationSignal = 0f;
        Assert.That(decision.TryEvaluate(noSignal, Exposure(8, 1.6f), out _), Is.False);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void Decision_UnknownOrDisabledOrHistoricalExposureCannotAuthorizeMoreWrist(int condition)
    {
        SuggestionDecisionOrchestrator decision = Decision(HybridCoordinationGoal.IncludeWrist);
        for (int i = 1; i <= 8; i++)
        {
            FrameErgonomicExposureData frame = Exposure(i, i * 0.2f);
            if (condition == 0) frame.wristRadialUlnarDeviation.isMeasurementValid = false;
            if (condition == 1) frame.wristRadialUlnarDeviation.isEnabled = false;
            if (condition == 2) frame.wristRadialUlnarDeviation.hasReachedCumulativeExposureAlert = true;
            Assert.That(decision.TryEvaluate(Usage(i, i * 0.2f, 0.1f), frame, out _), Is.False);
        }
    }

    [Test]
    public void Decision_AccumulatedAlertDoesNotWarnWhenAlreadyNeutral()
    {
        SuggestionDecisionOrchestrator decision = Decision();
        for (int i = 1; i <= 4; i++)
        {
            FrameErgonomicExposureData frame = Exposure(i, i * 0.2f, 0);
            frame.wristFlexionExtension.isOutsideOptimalRange = false;
            frame.wristFlexionExtension.hasReachedCumulativeExposureAlert = true;
            frame.wristFlexionExtension.hasReachedSustainedExposureThreshold = true;
            Assert.That(decision.TryEvaluate(Usage(i, i * 0.2f), frame, out _), Is.False);
        }
    }

    [Test]
    public void Decision_UsesCurrentSustainedTimeRatherThanLatchedFlag()
    {
        SuggestionDecisionOrchestrator decision = Decision();
        HybridSuggestionData last = default;
        for (int i = 1; i <= 4; i++)
        {
            FrameErgonomicExposureData frame = Exposure(i, i * 0.2f, 0);
            frame.wristFlexionExtension.sustainedExposureSeconds = 0.2f;
            frame.wristFlexionExtension.hasReachedSustainedExposureThreshold = true;
            decision.TryEvaluate(Usage(i, i * 0.2f), frame, out last);
        }
        Assert.That(last.priority, Is.EqualTo(1));
    }

    [Test]
    public void Decision_HandsRestartAndTrackingGapRemainIndependent()
    {
        SuggestionDecisionOrchestrator decision = Decision(HybridCoordinationGoal.IncludeWrist);
        for (int i = 1; i <= 5; i++)
            decision.TryEvaluate(Usage(i, i * 0.2f, 0.1f), Exposure(i, i * 0.2f), out _);
        Assert.That(decision.TryEvaluate(Usage(6, 3f, 0.1f), Exposure(6, 3f), out _), Is.False);
        FrameUsageData right = Usage(7, 3.2f, 0.1f);
        right.handType = HandType.RIGHT;
        FrameErgonomicExposureData rightExposure = Exposure(7, 3.2f);
        rightExposure.handType = HandType.RIGHT;
        Assert.That(decision.TryEvaluate(right, rightExposure, out _), Is.False);
        decision.Reset();
        Assert.That(decision.TryEvaluate(Usage(1, 0f, 0.1f), Exposure(1, 0f), out _), Is.False);
    }

    [Test]
    public void Gate_PrioritizesPostureAcrossHandsAndLimitsNotifications()
    {
        HybridSuggestionGate gate = new HybridSuggestionGate(profile);
        HybridSuggestionData motor = new HybridSuggestionData
        {
            exposure = Exposure(1, 1f), type = HybridSuggestionType.IncludeWrist, priority = 0
        };
        HybridSuggestionData posture = motor;
        posture.exposure.handType = HandType.RIGHT;
        posture.type = HybridSuggestionType.NeutralWrist;
        posture.priority = 2;
        Assert.That(gate.TrySelect(motor, posture, 1f, out HybridSuggestionData selected), Is.True);
        Assert.That(selected.HandType, Is.EqualTo(HandType.RIGHT));
        Assert.That(gate.TrySelect(motor, posture, 2f, out _), Is.False);
        Assert.That(gate.TrySelect(motor, posture, 10f, out _), Is.False);
        Assert.That(gate.TrySelect(motor, null, 10f, out _), Is.True);
        Assert.That(gate.TrySelect(motor, posture, 20f, out _), Is.False);
        motor.type = HybridSuggestionType.NeutralWrist;
        motor.priority = 1;
        Assert.That(gate.TrySelect(motor, null, 20f, out _), Is.True);
        motor.type = HybridSuggestionType.RedistributeToForearm;
        Assert.That(gate.TrySelect(motor, null, 30f, out _), Is.False);
        gate.Reset();
        Assert.That(gate.TrySelect(motor, null, 0f, out _), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Tracking_ConsumesBusesAndStopsWithoutAffectingMotionConsumers(bool showSnackbar)
    {
        GameObject go = new GameObject("Hybrid integration");
        go.SetActive(false);
        HybridSuggestionTrackingSystem tracker = go.AddComponent<HybridSuggestionTrackingSystem>();
        Set(tracker, "suggestionProfile", profile);
        Set(tracker, "calibrationProfile", calibration);
        Set(tracker, "output", showSnackbar ? HybridSuggestionOutput.LogAndSnackbar : HybridSuggestionOutput.LogOnly);
        int suggestions = 0;
        int motionFrames = 0;
        int snackbars = 0;
        void OnSuggestion(HybridSuggestionData data) => suggestions++;
        void OnMotion(FrameMotionData data) => motionFrames++;
        void OnSnackbar(SNACKBARTYPE type, string message, float duration) => snackbars++;
        HybridSuggestionEventBus.OnSuggestion += OnSuggestion;
        MotionEventBus.OnFrame += OnMotion;
        SnackbarManager.OnShow += OnSnackbar;
        try
        {
            Invoke(tracker, "OnEnable");
            tracker.BeginExercise();
            for (int i = 1; i <= 5; i++)
            {
                MotionEventBus.Publish(Motion(i, i * 0.2f));
                ErgonomicExposureEventBus.Publish(Exposure(i, i * 0.2f, 0));
                Invoke(tracker, "LateUpdate");
            }
            Assert.That(suggestions, Is.EqualTo(1));
            Assert.That(motionFrames, Is.EqualTo(5));
            Assert.That(snackbars, Is.EqualTo(showSnackbar ? 1 : 0));
            tracker.EndExercise(1f);
            MotionEventBus.Publish(Motion(6, 1.2f));
            ErgonomicExposureEventBus.Publish(Exposure(6, 1.2f, 0));
            Invoke(tracker, "LateUpdate");
            Assert.That(suggestions, Is.EqualTo(1));
            tracker.BeginExercise();
            Assert.That(tracker.SuggestionsEmitted, Is.Zero);
            Invoke(tracker, "OnDisable");
            Assert.That(tracker.IsTracking, Is.False);
        }
        finally
        {
            HybridSuggestionEventBus.OnSuggestion -= OnSuggestion;
            MotionEventBus.OnFrame -= OnMotion;
            SnackbarManager.OnShow -= OnSnackbar;
            Invoke(tracker, "OnDisable");
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Tracking_RejectsMismatchedEvidenceAndClearsPendingCandidate()
    {
        GameObject go = new GameObject("Hybrid mismatched frames");
        go.SetActive(false);
        HybridSuggestionTrackingSystem tracker = go.AddComponent<HybridSuggestionTrackingSystem>();
        Set(tracker, "suggestionProfile", profile);
        Set(tracker, "calibrationProfile", calibration);
        try
        {
            Invoke(tracker, "OnEnable");
            tracker.BeginExercise();
            MotionEventBus.Publish(Motion(1, 0.2f));
            ErgonomicExposureEventBus.Publish(Exposure(1, 0.2f, 0));
            MotionEventBus.Publish(Motion(2, 0.4f));
            ErgonomicExposureEventBus.Publish(Exposure(2, 0.4f, 0));
            // El siguiente frame incompleto invalida el candidato anterior antes de presentarlo.
            MotionEventBus.Publish(Motion(3, 0.6f));
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("\\[HybridSuggestions\\] Frame inválido o sin pareja"));
            ErgonomicExposureEventBus.Publish(Exposure(3, 0.61f, 0));
            Invoke(tracker, "LateUpdate");
            Assert.That(tracker.SuggestionsEmitted, Is.Zero);
        }
        finally
        {
            Invoke(tracker, "OnDisable");
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Tracking_GameManagerEventsResetActualExposureAndHybridBetweenExercises()
    {
        GameObject go = new GameObject("Hybrid exercise lifecycle");
        go.SetActive(false);
        GameManager manager = go.AddComponent<GameManager>();
        ErgonomicExposureTrackingSystem exposure = go.AddComponent<ErgonomicExposureTrackingSystem>();
        HybridSuggestionTrackingSystem hybrid = go.AddComponent<HybridSuggestionTrackingSystem>();
        Set(calibration, "cumulativeExposureAlertSeconds", 0.5f);
        Set(calibration, "sustainedExposureThresholdSeconds", 0.5f);
        Set(exposure, "calibrationProfile", calibration);
        Set(hybrid, "calibrationProfile", calibration);
        Set(hybrid, "suggestionProfile", profile);
        int emitted = 0;
        void Capture(HybridSuggestionData data) => emitted++;
        HybridSuggestionEventBus.OnSuggestion += Capture;
        try
        {
            Invoke(exposure, "OnEnable");
            Invoke(hybrid, "OnEnable");
            for (int exercise = 0; exercise < 2; exercise++)
            {
                manager.SetState(GAMESTATE.PLAYING);
                Assert.That(exposure.IsTracking && hybrid.IsTracking, Is.True);
                Assert.That(hybrid.SuggestionsEmitted, Is.Zero);
                for (int i = 1; i <= 5; i++)
                {
                    float time = i * 0.25f;
                    MotionEventBus.Publish(Motion(i, time));
                    ErgonomicEventBus.Publish(new FrameErgonomicData
                    {
                        frameId = i, timestamp = time, handType = HandType.LEFT,
                        wristFlexionExtension = new ErgonomicAngleData { degrees = 25f, isAvailable = true, isValid = true },
                        wristRadialUlnarDeviation = new ErgonomicAngleData { degrees = 0f, isAvailable = true, isValid = true },
                        wristPronationSupination = new ErgonomicAngleData { degrees = 0f, isAvailable = true, isValid = true }
                    });
                    Invoke(hybrid, "LateUpdate");
                }
                Assert.That(hybrid.SuggestionsEmitted, Is.EqualTo(1));
                manager.EndExercise(1.25f);
                Assert.That(exposure.IsTracking || hybrid.IsTracking, Is.False);
            }
            Assert.That(emitted, Is.EqualTo(2));
        }
        finally
        {
            HybridSuggestionEventBus.OnSuggestion -= Capture;
            Invoke(exposure, "OnDisable");
            Invoke(hybrid, "OnDisable");
            Object.DestroyImmediate(go);
        }
    }

    [TestCase("Insert")]
    [TestCase("OSU")]
    [TestCase("DuckHunter")]
    public void Scenes_HaveHybridTrackingWithSameCalibrationAndBaseline(string name)
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenPreviewScene($"Assets/Scenes/{name}.unity");
        try
        {
            HybridSuggestionTrackingSystem hybrid = null;
            ErgonomicExposureTrackingSystem exposure = null;
            int hybridCount = 0;
            int baselineCount = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (HybridSuggestionTrackingSystem component in root.GetComponentsInChildren<HybridSuggestionTrackingSystem>(true))
                {
                    hybrid = component;
                    hybridCount++;
                }
                if (exposure == null) exposure = root.GetComponentInChildren<ErgonomicExposureTrackingSystem>(true);
                baselineCount += root.GetComponentsInChildren<ExerciseFeedbackSystem>(true).Length;
            }
            Assert.That(hybridCount, Is.EqualTo(1));
            Assert.That(baselineCount, Is.GreaterThan(0));
            Assert.That(hybrid.gameObject, Is.EqualTo(exposure.gameObject));
            SerializedObject hybridFields = new SerializedObject(hybrid);
            SerializedObject exposureFields = new SerializedObject(exposure);
            Assert.That(hybridFields.FindProperty("suggestionProfile").objectReferenceValue, Is.Not.Null);
            Assert.That(hybridFields.FindProperty("calibrationProfile").objectReferenceValue,
                Is.EqualTo(exposureFields.FindProperty("calibrationProfile").objectReferenceValue));
            Assert.That(hybridFields.FindProperty("output").enumValueIndex, Is.Zero);
            Assert.That(hybridFields.FindProperty("leftGoal").enumValueIndex, Is.Zero);
            Assert.That(hybridFields.FindProperty("rightGoal").enumValueIndex, Is.Zero);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private SuggestionDecisionOrchestrator Decision(HybridCoordinationGoal goal = HybridCoordinationGoal.ObserveOnly)
    {
        return new SuggestionDecisionOrchestrator(profile, calibration, goal, HybridCoordinationGoal.ObserveOnly);
    }

    private static FrameMotionData Motion(long id, float time, float wrist = 0.9f, float forearm = 0.1f)
    {
        return new FrameMotionData
        {
            frameId = id, timestamp = time, handType = HandType.LEFT,
            motions = new List<MotionData>
            {
                new MotionData { zone = MotionZone.Hand, value = 0.5f, isActive = true },
                new MotionData { zone = MotionZone.Wrist, value = wrist, isActive = wrist > 0f },
                new MotionData { zone = MotionZone.Forearm, value = forearm, isActive = forearm > 0f }
            }
        };
    }

    private static FrameUsageData Usage(long id, float time, float wrist = 0.9f)
    {
        return new FrameUsageData
        {
            frameId = id, timestamp = time, handType = HandType.LEFT,
            isValid = true, observedSeconds = 5f, meanRotationSignal = 0.2f,
            wristContribution = wrist, forearmContribution = 1f - wrist
        };
    }

    private static FrameErgonomicExposureData Exposure(long id, float time, int exposed = -1)
    {
        return new FrameErgonomicExposureData
        {
            frameId = id, timestamp = time, handType = HandType.LEFT,
            wristFlexionExtension = Dimension(exposed == 0),
            wristRadialUlnarDeviation = Dimension(exposed == 1),
            wristPronationSupination = Dimension(exposed == 2)
        };
    }

    private static ErgonomicExposureDimensionData Dimension(bool exposed)
    {
        return new ErgonomicExposureDimensionData
        {
            degrees = exposed ? 50f : 0f, isEnabled = true,
            isMeasurementAvailable = true, isMeasurementValid = true,
            isOutsideOptimalRange = exposed,
            cumulativeExposureSeconds = exposed ? 65f : 0f,
            sustainedExposureSeconds = exposed ? 65f : 0f,
            hasReachedCumulativeExposureAlert = exposed,
            hasReachedSustainedExposureThreshold = exposed
        };
    }

    private static void Set(object target, string field, object value)
    {
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    private static void Invoke(object target, string method)
    {
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }
}
#endif
