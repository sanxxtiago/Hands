public interface IScoreSession
{
    void Begin(ScoreExerciseType exerciseType);

    bool IsActive { get; }

    void Complete();

    void Reset();
}
