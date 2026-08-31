#if UNITY_INCLUDE_TESTS
using NUnit.Framework;

public sealed class ExerciseResultPersistenceServiceTests
{
    private ExerciseResultPersistenceService persistenceService;
    private ExpositionServices expositionServices;

    [SetUp]
    public void SetUp()
    {
        expositionServices = new ExpositionServices("transaction-test-user");
        persistenceService = new ExerciseResultPersistenceService(
            new SessionService("transaction-test-user"),
            new ScoreService("transaction-test-user"),
            new LeaderboardService(),
            "transaction-test-user",
            "Usuario de prueba",
            expositionServices);
    }

    [TearDown]
    public void TearDown()
    {
        expositionServices.DeleteAll();
    }

    [Test]
    public void FirstResultIsPendingAndARepeatedConfirmationIsDuplicate()
    {
        SessionSummary session = CreateSession();
        ExerciseSummary summary = CreateSummary(ExerciseType.Insert);
        ExerciseScore score = CreateScore(ScoreExerciseType.Insert, 75f);

        ExerciseCommitOutcome firstOutcome = persistenceService.CommitExerciseResult(
            session,
            summary,
            score);

        ExerciseCommitOutcome duplicateOutcome = persistenceService.CommitExerciseResult(
            session,
            summary,
            score);

        Assert.That(firstOutcome, Is.EqualTo(ExerciseCommitOutcome.Pending));
        Assert.That(duplicateOutcome, Is.EqualTo(ExerciseCommitOutcome.Duplicate));
    }

    [Test]
    public void RepeatedKeyWithDifferentScoreIsConflict()
    {
        SessionSummary session = CreateSession();
        ExerciseSummary summary = CreateSummary(ExerciseType.Insert);
        ExerciseScore originalScore = CreateScore(ScoreExerciseType.Insert, 75f);

        Assert.That(
            persistenceService.CommitExerciseResult(session, summary, originalScore),
            Is.EqualTo(ExerciseCommitOutcome.Pending));

        ExerciseScore conflictingScore = CreateScore(ScoreExerciseType.Insert, 50f);

        Assert.That(
            persistenceService.CommitExerciseResult(session, summary, conflictingScore),
            Is.EqualTo(ExerciseCommitOutcome.Conflict));
    }

    [Test]
    public void RepeatedKeyWithDifferentExpositionIsConflict()
    {
        SessionSummary session = CreateSession();
        ExerciseSummary summary = CreateSummary(ExerciseType.Insert);
        ExerciseScore score = CreateScore(ScoreExerciseType.Insert, 75f);
        ExpositionSummary originalExposition = CreateExposition(session, summary, 0f);

        Assert.That(
            persistenceService.CommitExerciseResult(
                session,
                summary,
                score,
                originalExposition),
            Is.EqualTo(ExerciseCommitOutcome.Pending));

        ExpositionSummary conflictingExposition = CreateExposition(session, summary, 1f);

        Assert.That(
            persistenceService.CommitExerciseResult(
                session,
                summary,
                score,
                conflictingExposition),
            Is.EqualTo(ExerciseCommitOutcome.Conflict));
    }

    [Test]
    public void ExpositionServicePersistsAndLoadsRecordsByUser()
    {
        SessionSummary session = CreateSession();
        ExerciseSummary summary = CreateSummary(ExerciseType.Insert);
        ExpositionSummary exposition = CreateExposition(session, summary, 0f);

        Assert.That(expositionServices.Upsert(exposition), Is.True);

        ExpositionServices restoredService = new ExpositionServices(
            "transaction-test-user");
        restoredService.Load();

        Assert.That(
            restoredService.GetExposition(
                session.SessionGuid,
                ExerciseType.Insert),
            Is.Not.Null);
        Assert.That(restoredService.TotalRecords, Is.EqualTo(1));

        restoredService.DeleteAll();
    }

    [Test]
    public void IncompleteSessionCannotBeUpserted()
    {
        SessionService sessionService = new SessionService("transaction-test-user");

        Assert.That(sessionService.UpsertSession(CreateSession()), Is.False);
        Assert.That(sessionService.TotalSessions, Is.EqualTo(0));
    }

    private static SessionSummary CreateSession()
    {
        return new SessionSummary { SessionId = 1 };
    }

    private static ExerciseSummary CreateSummary(ExerciseType exerciseType)
    {
        return new ExerciseSummary
        {
            exerciseType = exerciseType,
            exerciseDuration = 10f,
            generalSuggestion = "Sugerencia"
        };
    }

    private static ExerciseScore CreateScore(
        ScoreExerciseType exerciseType,
        float totalScore)
    {
        return new ExerciseScore
        {
            exerciseType = exerciseType,
            totalScore = totalScore,
            scoreGrade = "Good",
            isValid = true
        };
    }

    private static ExpositionSummary CreateExposition(
        SessionSummary session,
        ExerciseSummary summary,
        float cumulativeExposureSeconds)
    {
        ExpositionDimensionSummary dimension = new ExpositionDimensionSummary
        {
            validObservationSeconds = 2f,
            maximumSustainedExposureSeconds = cumulativeExposureSeconds,
            cumulativeExposureSeconds = cumulativeExposureSeconds,
            sustainedExposureSeconds = cumulativeExposureSeconds,
            hasReachedCumulativeExposureAlert = cumulativeExposureSeconds > 0f,
            hasReachedSustainedExposureThreshold = cumulativeExposureSeconds > 0f
        };

        return new ExpositionSummary
        {
            sessionGuid = session.SessionGuid,
            sessionId = session.SessionId,
            exerciseIndex = session.Summaries.Count,
            exerciseType = summary.exerciseType,
            exerciseDuration = summary.exerciseDuration,
            leftHand = new HandExpositionSummary
            {
                handType = HandType.LEFT,
                wristFlexionExtension = dimension,
                wristRadialUlnarDeviation = dimension,
                wristPronationSupination = dimension
            },
            rightHand = new HandExpositionSummary
            {
                handType = HandType.RIGHT,
                wristFlexionExtension = dimension,
                wristRadialUlnarDeviation = dimension,
                wristPronationSupination = dimension
            }
        };
    }
}
#endif
