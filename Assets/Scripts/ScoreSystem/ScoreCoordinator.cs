public sealed class ScoreCoordinator
{
    private readonly IScoreSession scoreSession;
    private readonly InsertScoreCalculator insertCalculator;
    private readonly OSUScoreCalculator osuCalculator;
    private readonly DuckHunterScoreCalculator duckHunterCalculator;
    private ScoreExerciseType activeExerciseType;
    private bool hasActiveExercise;

    public bool IsSessionActive => scoreSession != null && scoreSession.IsActive;

    public ScoreCoordinator()
        : this(
            new InsertScoreConfig(),
            new OSUScoreConfig(),
            new DuckHunterScoreConfig(),
            null)
    {
    }

    public ScoreCoordinator(InsertScoreConfig insertConfig)
        : this(
            insertConfig,
            new OSUScoreConfig(),
            new DuckHunterScoreConfig(),
            null)
    {
    }

    public ScoreCoordinator(
        InsertScoreConfig insertConfig,
        OSUScoreConfig osuConfig,
        DuckHunterScoreConfig duckHunterConfig,
        IScoreSession scoreSession = null)
    {
        this.scoreSession = scoreSession ?? new ScoreSession();
        insertCalculator = new InsertScoreCalculator(insertConfig);
        osuCalculator = new OSUScoreCalculator(osuConfig);
        duckHunterCalculator = new DuckHunterScoreCalculator(duckHunterConfig);
    }

    public void BeginExercise(ScoreExerciseType exerciseType)
    {
        if (!IsKnownExerciseType(exerciseType))
        {
            ScoreSystemLog.Error("No se puede iniciar un ejercicio de score con un tipo invalido.");
            Reset();
            return;
        }

        if (scoreSession.IsActive)
            ScoreSystemLog.Warning("Se reiniciara la sesion de score activa.");

        scoreSession.Begin(exerciseType);
        activeExerciseType = exerciseType;
        hasActiveExercise = scoreSession.IsActive;
    }

    public ExerciseScore CompleteInsert(InsertScoreData data)
    {
        if (!CanComplete(ScoreExerciseType.Insert))
            return ScoreResultFactory.Invalid(ScoreExerciseType.Insert, "No hay una sesion activa de Insert.");

        return CompleteScore(insertCalculator.Calculate(data));
    }

    public ExerciseScore CompleteOSU(OSUScoreData data)
    {
        if (!CanComplete(ScoreExerciseType.OSU))
            return ScoreResultFactory.Invalid(ScoreExerciseType.OSU, "No hay una sesion activa de OSU.");

        return CompleteScore(osuCalculator.Calculate(data));
    }

    public ExerciseScore CompleteDuckHunter(DuckHunterScoreData data)
    {
        if (!CanComplete(ScoreExerciseType.DuckHunter))
            return ScoreResultFactory.Invalid(ScoreExerciseType.DuckHunter, "No hay una sesion activa de DuckHunter.");

        return CompleteScore(duckHunterCalculator.Calculate(data));
    }

    public void Reset()
    {
        scoreSession.Reset();
        hasActiveExercise = false;
    }

    private ExerciseScore CompleteScore(ExerciseScore score)
    {
        scoreSession.Complete();
        hasActiveExercise = false;

        if (score != null && score.isValid)
            ScoreEventBus.Publish(score);

        return score;
    }

    private bool CanComplete(ScoreExerciseType exerciseType)
    {
        return hasActiveExercise
            && scoreSession.IsActive
            && activeExerciseType == exerciseType;
    }

    private static bool IsKnownExerciseType(ScoreExerciseType exerciseType)
    {
        return exerciseType == ScoreExerciseType.Insert
            || exerciseType == ScoreExerciseType.OSU
            || exerciseType == ScoreExerciseType.DuckHunter;
    }
}
