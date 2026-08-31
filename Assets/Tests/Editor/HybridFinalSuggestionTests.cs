#if UNITY_INCLUDE_TESTS
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class HybridFinalSuggestionTests
{
    private HybridExerciseProfile profile;
    private ErgonomicCalibrationProfile calibration;
    private HybridSuggestionProfile runtime;

    [SetUp]
    public void SetUp()
    {
        calibration = ScriptableObject.CreateInstance<ErgonomicCalibrationProfile>();
        runtime = ScriptableObject.CreateInstance<HybridSuggestionProfile>();
        profile = ScriptableObject.CreateInstance<HybridExerciseProfile>();
        Set(profile, "calibrationProfile", calibration);
        Set(profile, "runtimeProfile", runtime);
        Set(profile, "coordinationEnabled", true);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(profile);
        Object.DestroyImmediate(calibration);
        Object.DestroyImmediate(runtime);
    }

    [TestCase("expectedDurationSeconds", 0f)]
    [TestCase("minimumFinalObservationSeconds", float.NaN)]
    [TestCase("minimumActivityRatio", 2f)]
    [TestCase("goodPerformanceThreshold", float.PositiveInfinity)]
    [TestCase("intermediatePerformanceThreshold", 30f)]
    public void Profile_RejectsInvalidValues(string field, float value)
    {
        Assert.That(profile.TryValidate(out _), Is.True);
        Set(profile, field, value);
        Assert.That(profile.TryValidate(out _), Is.False);
    }

    [Test]
    public void Profile_RejectsInvalidTargetsAndMissingCalibration()
    {
        Set(profile.LeftHand, "wristTarget", 0.9f);
        Assert.That(profile.TryValidate(out _), Is.False);
        Set(profile.LeftHand, "wristTarget", 0.4f);
        Set(profile, "calibrationProfile", null);
        Assert.That(profile.TryValidate(out _), Is.False);
    }

    [TestCase(ExerciseType.Insert, 40f, 60f, 120f)]
    [TestCase(ExerciseType.Insert, 90f, 60f, 120f)]
    [TestCase(ExerciseType.Insert, 130f, 60f, 120f)]
    [TestCase(ExerciseType.OSU, 20f, 30f, 60f)]
    [TestCase(ExerciseType.OSU, 40f, 30f, 60f)]
    [TestCase(ExerciseType.OSU, 80f, 30f, 60f)]
    [TestCase(ExerciseType.DuckHunter, 0f, 0.2f, 0.5f)]
    [TestCase(ExerciseType.DuckHunter, 4f, 0.2f, 0.5f)]
    [TestCase(ExerciseType.DuckHunter, 8f, 0.2f, 0.5f)]
    public void PerformanceOnly_PreservesBaselineCategories(ExerciseType type, float value, float good, float intermediate)
    {
        Set(profile, "exerciseType", type);
        Set(profile, "goodPerformanceThreshold", good);
        Set(profile, "intermediatePerformanceThreshold", intermediate);
        ExerciseSummary exercise = Exercise();
        exercise.exerciseType = type;
        exercise.completionTime = exercise.totalInteractionDelay = value;
        exercise.interactionCount = 10;
        exercise.ducksMissed = (int)value;
        exercise.ducksHit = 10 - (int)value;
        string text = HybridFinalSuggestionBuilder.Build(profile, exercise, Hand(HandType.LEFT), Hand(HandType.RIGHT));
        Assert.That(text, Is.EqualTo(GeneralSuggestionBuilder.Build(type, value, value, exercise.ducksHit, exercise.ducksMissed)));
        Assert.That(text, Does.Not.Contain("\n"));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void PostureOnly_UsesAllDimensionsAndPreservesHistory(int dimension)
    {
        HandErgonomicExposureSummary left = Hand(HandType.LEFT);
        Assign(ref left, dimension, Dimension(65f, 60f));
        string text = HybridFinalSuggestionBuilder.Build(profile, Exercise(), left, Hand(HandType.RIGHT), false);
        Assert.That(text, Does.Contain("izquierda"));
        Assert.That(text, Does.Contain("sostenida"));
        Assert.That(text, Does.Contain(dimension == 2 ? "giro" : "muñeca"));
        Assert.That(text, Does.Not.Contain("\n"));
    }

    [Test]
    public void SustainedOutranksCumulativeAcrossHandsAndDimensions()
    {
        HandErgonomicExposureSummary left = Hand(HandType.LEFT);
        HandErgonomicExposureSummary right = Hand(HandType.RIGHT);
        left.wristFlexionExtension = Dimension(85f, 10f);
        right.wristPronationSupination = Dimension(65f, 60f);
        string text = HybridFinalSuggestionBuilder.Build(profile, Exercise(), left, right);
        Assert.That(text, Does.StartWith("La mano derecha"));
        Assert.That(text, Does.Contain("sostenida de giro"));
        right.wristRadialUlnarDeviation = Dimension(70f, 70f);
        text = HybridFinalSuggestionBuilder.Build(profile, Exercise(), left, right);
        Assert.That(text, Does.Contain("sostenida en la muñeca"));
    }

    [Test]
    public void PostureUsageAndPerformance_CombineInTwoLinesWithoutSpeedPressure()
    {
        ExerciseSummary exercise = Exercise();
        exercise.completionTime = 150f;
        exercise.leftHand = Usage(HandType.LEFT, 0.6f, 0.1f);
        HandErgonomicExposureSummary left = Hand(HandType.LEFT);
        left.wristFlexionExtension = Dimension(65f, 60f);
        string text = HybridFinalSuggestionBuilder.Build(profile, exercise, left, Hand(HandType.RIGHT));
        Assert.That(text.Split('\n'), Has.Length.EqualTo(2));
        Assert.That(text, Does.Contain("redistribuye"));
        Assert.That(text, Does.Contain("postura cómoda sobre la rapidez"));
        Assert.That(text, Does.Not.Contain("reducir el tiempo"));
    }

    [Test]
    public void TwistExposure_BlocksRedistributionEvenWhenWristHasHigherPriority()
    {
        ExerciseSummary exercise = Exercise();
        exercise.leftHand = Usage(HandType.LEFT, 0.6f, 0.1f);
        HandErgonomicExposureSummary left = Hand(HandType.LEFT);
        left.wristFlexionExtension = Dimension(80f, 70f);
        left.wristPronationSupination = Dimension(1f, 1f);
        string text = HybridFinalSuggestionBuilder.Build(profile, exercise, left, Hand(HandType.RIGHT));
        Assert.That(text, Does.Contain("más neutra"));
        Assert.That(text, Does.Not.Contain("redistribuye"));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void InvalidEvidence_NeverAuthorizesCoordination(int invalidCase)
    {
        ExerciseSummary exercise = Exercise();
        exercise.leftHand = Usage(HandType.LEFT, 0.6f, 0.1f);
        HandErgonomicExposureSummary left = Hand(HandType.LEFT);
        if (invalidCase == 0) exercise.leftHand.relativeUsage[0] = float.NaN;
        if (invalidCase == 1) exercise.leftHand.zones[0] = MotionZone.Wrist;
        if (invalidCase == 2) left.wristPronationSupination.validObservationSeconds = 0f;
        if (invalidCase == 3) exercise.leftHand.activityRatio = 0f;
        string text = HybridFinalSuggestionBuilder.Build(profile, exercise, left, Hand(HandType.RIGHT));
        Assert.That(text, Is.EqualTo(GeneralSuggestionBuilder.Build(ExerciseType.Insert, 40f, 0f, 0, 0)));
    }

    [Test]
    public void UsageGoals_AreExerciseSpecificAndCanBeDisabled()
    {
        ExerciseSummary exercise = Exercise();
        exercise.leftHand = Usage(HandType.LEFT, 0.6f, 0.1f);
        string text = HybridFinalSuggestionBuilder.Build(profile, exercise, Hand(HandType.LEFT), Hand(HandType.RIGHT));
        Assert.That(text, Does.Contain("objetivo configurado"));
        Set(profile, "coordinationEnabled", false);
        text = HybridFinalSuggestionBuilder.Build(profile, exercise, Hand(HandType.LEFT), Hand(HandType.RIGHT));
        Assert.That(text, Does.Not.Contain("objetivo configurado"));
        Set(profile, "coordinationEnabled", true);
        Set(profile.LeftHand, "wristTarget", 0.6f);
        Set(profile.LeftHand, "forearmTarget", 0.1f);
        text = HybridFinalSuggestionBuilder.Build(profile, exercise, Hand(HandType.LEFT), Hand(HandType.RIGHT));
        Assert.That(text, Does.Not.Contain("objetivo configurado"));
    }

    [Test]
    public void Interpreter_SummaryKeepsPeakAfterNeutralNewEpisodeAndGap()
    {
        Set(calibration, "sustainedExposureThresholdSeconds", 0.5f);
        ErgonomicExposureInterpreter interpreter = new ErgonomicExposureInterpreter(HandType.LEFT, calibration);
        for (int i = 0; i <= 3; i++) interpreter.TryProcess(Frame(i, i * 0.25f, 30f), out _);
        interpreter.TryProcess(Frame(4, 1f, 0f), out _);
        interpreter.TryProcess(Frame(5, 1.25f, 30f), out _);
        interpreter.TryProcess(Frame(6, 5f, 30f), out _);
        ErgonomicExposureDimensionSummary summary = interpreter.GetSummary().wristFlexionExtension;
        Assert.That(summary.maximumSustainedExposureSeconds, Is.EqualTo(0.75f));
        Assert.That(summary.sustainedExposureSeconds, Is.Zero);
        Assert.That(summary.hasReachedSustainedExposureThreshold, Is.True);
        Assert.That(summary.validObservationSeconds, Is.EqualTo(1.25f));
        Assert.That(summary.cumulativeExposureSeconds, Is.EqualTo(1f));
        interpreter.Reset();
        Assert.That(interpreter.GetSummary().wristFlexionExtension.validObservationSeconds, Is.Zero);
        Assert.That(interpreter.GetSummary().wristFlexionExtension.maximumSustainedExposureSeconds, Is.Zero);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Synchronizer_AcceptsEitherOrderAndResetsBetweenExercises(bool exposureFirst)
    {
        HybridExerciseResultSynchronizer sync = new HybridExerciseResultSynchronizer();
        sync.Begin();
        if (exposureFirst) sync.CaptureExposure(90f, Hand(HandType.LEFT), Hand(HandType.RIGHT));
        sync.CaptureUsage(90f, Usage(HandType.LEFT), Usage(HandType.RIGHT));
        if (!exposureFirst) sync.CaptureExposure(90f, Hand(HandType.LEFT), Hand(HandType.RIGHT));
        Assert.That(sync.TryFinalize(90f, out bool ready), Is.True);
        Assert.That(ready, Is.True);
        Assert.That(sync.TryFinalize(90f, out _), Is.False);
        sync.Begin();
        sync.CaptureUsage(90f, Usage(HandType.LEFT), Usage(HandType.RIGHT));
        Assert.That(sync.TryFinalize(90f, out ready), Is.True);
        Assert.That(ready, Is.False);
        sync.Begin();
        Assert.That(sync.TryFinalize(90f, out _), Is.False);
    }

    [Test]
    public void Synchronizer_RejectsMismatchedDurationsAndHands()
    {
        HybridExerciseResultSynchronizer sync = new HybridExerciseResultSynchronizer();
        sync.Begin();
        sync.CaptureUsage(90f, Usage(HandType.RIGHT), Usage(HandType.LEFT));
        Assert.That(sync.TryFinalize(90f, out _), Is.False);
        sync.Begin();
        sync.CaptureUsage(90f, Usage(HandType.LEFT), Usage(HandType.RIGHT));
        sync.CaptureExposure(80f, Hand(HandType.LEFT), Hand(HandType.RIGHT));
        Assert.That(sync.TryFinalize(90f, out bool ready), Is.True);
        Assert.That(ready, Is.False);
    }

    [TestCase(true, true)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Lifecycle_FinalTextIsReadyForUIRegardlessOfStopOrder(bool exposureFirst, bool provideExposure)
    {
        GameObject go = new GameObject("Final suggestion lifecycle");
        go.SetActive(false);
        GameManager manager = go.AddComponent<GameManager>();
        SessionRecorder recorder = go.AddComponent<SessionRecorder>();
        MetricsTrackingSystem metrics = go.AddComponent<MetricsTrackingSystem>();
        ResultsManager resultsManager = go.AddComponent<ResultsManager>();
        GameObject prefab = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Results.prefab");
        prefab.SetActive(false);
        ResultsUI ui = prefab.GetComponentInChildren<ResultsUI>(true);
        resultsManager.resultsUI = ui;
        Set(recorder, "hybridProfile", profile);
        string atEnd = null;
        string atResults = null;
        HandErgonomicExposureSummary left = Hand(HandType.LEFT);
        left.wristFlexionExtension = Dimension(65f, 60f);
        void PublishExposure(float duration)
        {
            if (provideExposure) ErgonomicExposureEventBus.PublishTrackingStop(duration, left, Hand(HandType.RIGHT));
        }
        void BeforeFinalization(float _) => atEnd = SessionRecorder.LastGeneralSuggestion;
        void ShowResults() => atResults = SessionRecorder.LastGeneralSuggestion;
        try
        {
            // El consumidor se suscribe primero: aun así debe esperar a la finalización.
            Invoke(resultsManager, "OnEnable");
            Invoke(recorder, "OnEnable");
            if (exposureFirst) GameManager.OnExerciseEnd += PublishExposure;
            Invoke(metrics, "OnEnable");
            if (!exposureFirst) GameManager.OnExerciseEnd += PublishExposure;
            GameManager.OnExerciseEnd += BeforeFinalization;
            GameManager.OnShowResults += ShowResults;
            manager.SetState(GAMESTATE.PLAYING);
            recorder.SetInsertPiecesData(40f);
            if (!provideExposure) LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("\\[HybridFinalSuggestions\\] Falta resumen ergonómico"));
            LogAssert.Expect(LogType.Error, "[SessionRecorder] No existe un SessionManager para confirmar el ejercicio.");
            manager.EndExercise(90f);
            Assert.That(atEnd, Is.Null);
            Assert.That(atResults, Is.Not.Null.And.Not.Empty);
            Assert.That(atResults.Contains("sostenida"), Is.EqualTo(provideExposure));
            ExerciseSummary stored = Exercise();
            stored.generalSuggestion = atResults;
            ExerciseSummary restored = JsonUtility.FromJson<ExerciseSummary>(JsonUtility.ToJson(stored));
            Assert.That(restored.generalSuggestion, Is.EqualTo(atResults));

            Assert.That(ui.generalSuggestionText.text, Is.EqualTo(restored.generalSuggestion));
            manager.SetState(GAMESTATE.PLAYING);
            Assert.That(SessionRecorder.LastGeneralSuggestion, Is.Null);
        }
        finally
        {
            GameManager.OnExerciseEnd -= PublishExposure;
            GameManager.OnExerciseEnd -= BeforeFinalization;
            GameManager.OnShowResults -= ShowResults;
            Invoke(recorder, "OnDisable");
            Invoke(metrics, "OnDisable");
            Invoke(resultsManager, "OnDisable");
            PrefabUtility.UnloadPrefabContents(prefab);
            Object.DestroyImmediate(go);
        }
    }

    [TestCase("Insert", ExerciseType.Insert)]
    [TestCase("OSU", ExerciseType.OSU)]
    [TestCase("DuckHunter", ExerciseType.DuckHunter)]
    public void Scenes_SharePerExerciseProfileAndCalibration(string name, ExerciseType type)
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenPreviewScene($"Assets/Scenes/{name}.unity");
        try
        {
            SessionRecorder recorder = Find<SessionRecorder>(scene);
            HybridSuggestionTrackingSystem hybrid = Find<HybridSuggestionTrackingSystem>(scene);
            ErgonomicExposureTrackingSystem exposure = Find<ErgonomicExposureTrackingSystem>(scene);
            HybridExerciseProfile asset = (HybridExerciseProfile)new SerializedObject(recorder).FindProperty("hybridProfile").objectReferenceValue;
            Assert.That(asset, Is.Not.Null);
            Assert.That(asset.TryValidate(out string error), Is.True, error);
            Assert.That(asset.ExerciseType, Is.EqualTo(type));
            Assert.That(asset.CoordinationEnabled, Is.EqualTo(type == ExerciseType.Insert));
            Assert.That(new SerializedObject(hybrid).FindProperty("exerciseProfile").objectReferenceValue, Is.EqualTo(asset));
            Assert.That(new SerializedObject(exposure).FindProperty("calibrationProfile").objectReferenceValue, Is.EqualTo(asset.CalibrationProfile));
            Assert.That(new SerializedObject(hybrid).FindProperty("suggestionProfile").objectReferenceValue, Is.EqualTo(asset.RuntimeProfile));
            Assert.That(Find<ResultsUI>(scene).generalSuggestionText, Is.Not.Null);
            Assert.That(Find<ExerciseFeedbackSystem>(scene), Is.Not.Null);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static T Find<T>(UnityEngine.SceneManagement.Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null) return component;
        }
        return null;
    }

    [Test]
    public void PersistenceAndSessionReader_KeepTheTwoLineTextWithoutModelChanges()
    {
        ExerciseSummary exercise = Exercise();
        HandErgonomicExposureSummary left = Hand(HandType.LEFT);
        left.wristFlexionExtension = Dimension(65f, 60f);
        exercise.generalSuggestion = HybridFinalSuggestionBuilder.Build(profile, exercise, left, Hand(HandType.RIGHT));
        SessionSummary session = new SessionSummary { SessionId = 1 };
        const string testUser = "hybrid-final-suggestion-test";
        ExerciseResultPersistenceService service = new ExerciseResultPersistenceService(
            new SessionService(testUser), new ScoreService(testUser), new LeaderboardService(), testUser, "Prueba",
            new ExpositionServices(testUser));
        ExerciseScore score = new ExerciseScore { exerciseType = ScoreExerciseType.Insert, totalScore = 75f, scoreGrade = "Good", isValid = true };
        Assert.That(service.CommitExerciseResult(session, exercise, score), Is.EqualTo(ExerciseCommitOutcome.Pending));
        Assert.That(session.Summaries[0].generalSuggestion, Is.EqualTo(exercise.generalSuggestion));
        // Completar solo el modelo en memoria para leerlo; no se escribe en datos de usuarios.
        session.AddSummary(new ExerciseSummary { exerciseType = ExerciseType.OSU });
        session.AddSummary(new ExerciseSummary { exerciseType = ExerciseType.DuckHunter });
        SessionSummary restored = Newtonsoft.Json.JsonConvert.DeserializeObject<SessionSummary>(
            Newtonsoft.Json.JsonConvert.SerializeObject(session));
        GameObject go = new GameObject("Session suggestion text");
        go.SetActive(false);
        try
        {
            SessionReader reader = go.AddComponent<SessionReader>();
            TMPro.TextMeshProUGUI label = go.AddComponent<TMPro.TextMeshProUGUI>();
            Set(reader, "session", restored);
            Set(reader, "generalSuggestionText", label);
            Invoke(reader, "SetGeneralSuggestion");
            Assert.That(label.text, Is.EqualTo(exercise.generalSuggestion));
            Assert.That(label.text.Split('\n'), Has.Length.EqualTo(2));
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void ResultsPrefab_HasRoomForTheLongestFinalRecommendation()
    {
        ExerciseSummary exercise = Exercise();
        exercise.completionTime = 150f;
        exercise.leftHand = Usage(HandType.LEFT, 0.6f, 0.1f);
        HandErgonomicExposureSummary left = Hand(HandType.LEFT);
        left.wristFlexionExtension = Dimension(65f, 60f);
        string text = HybridFinalSuggestionBuilder.Build(profile, exercise, left, Hand(HandType.RIGHT));
        GameObject prefab = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Results.prefab");
        try
        {
            TMPro.TMP_Text label = prefab.GetComponentInChildren<ResultsUI>(true).generalSuggestionText;
            Vector2 preferred = label.GetPreferredValues(text, label.rectTransform.rect.width, Mathf.Infinity);
            Assert.That(preferred.y, Is.LessThanOrEqualTo(label.rectTransform.rect.height), text);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
    }

    private ExerciseSummary Exercise() => new ExerciseSummary
    {
        exerciseType = ExerciseType.Insert, exerciseDuration = 90f, completionTime = 40f,
        leftHand = Usage(HandType.LEFT), rightHand = Usage(HandType.RIGHT)
    };

    private HandErgonomicExposureSummary Hand(HandType hand) => new HandErgonomicExposureSummary
    {
        handType = hand, calibrationProfileId = calibration.GetInstanceID(),
        wristFlexionExtension = Dimension(), wristRadialUlnarDeviation = Dimension(), wristPronationSupination = Dimension()
    };

    private static HandUsageSummary Usage(HandType hand, float wrist = 0.4f, float forearm = 0.2f) => new HandUsageSummary
    {
        handType = hand, zones = new[] { MotionZone.Hand, MotionZone.Wrist, MotionZone.Forearm },
        relativeUsage = new[] { 1f - wrist - forearm, wrist, forearm },
        absoluteUsage = new[] { 0.4f, wrist, forearm }, intensity = new float[3],
        totalActiveSeconds = 80f, activityRatio = 0.8f
    };

    private static ErgonomicExposureDimensionSummary Dimension(float cumulative = 0f, float maximum = 0f) => new ErgonomicExposureDimensionSummary
    {
        validObservationSeconds = 90f, cumulativeExposureSeconds = cumulative, maximumSustainedExposureSeconds = maximum,
        hasReachedCumulativeExposureAlert = cumulative >= 60f, hasReachedSustainedExposureThreshold = maximum >= 60f
    };

    private static void Assign(ref HandErgonomicExposureSummary hand, int dimension, ErgonomicExposureDimensionSummary data)
    {
        if (dimension == 0) hand.wristFlexionExtension = data;
        else if (dimension == 1) hand.wristRadialUlnarDeviation = data;
        else hand.wristPronationSupination = data;
    }

    private static FrameErgonomicData Frame(long id, float timestamp, float angle) => new FrameErgonomicData
    {
        frameId = id, timestamp = timestamp, handType = HandType.LEFT,
        wristFlexionExtension = new ErgonomicAngleData { degrees = angle, isAvailable = true, isValid = true }
    };

    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    private static void Invoke(object target, string name) =>
        target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
}
#endif
