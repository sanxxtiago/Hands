#if UNITY_INCLUDE_TESTS
using NUnit.Framework;

public sealed class ExerciseResultPersistenceServiceTests
{
    private ExerciseResultPersistenceService persistenceService;

    [SetUp]
    public void SetUp()
    {
        persistenceService = new ExerciseResultPersistenceService(
            new SessionService("transaction-test-user"),
            new ScoreService("transaction-test-user"),
            new LeaderboardService(),
            "transaction-test-user",
            "Usuario de prueba");
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
}
#endif
