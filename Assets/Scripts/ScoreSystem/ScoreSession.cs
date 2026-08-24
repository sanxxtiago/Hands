using System;

public sealed class ScoreSession : IScoreSession
{
    public bool IsActive { get; private set; }

    public ScoreExerciseType CurrentExerciseType { get; private set; }

    public void Begin(ScoreExerciseType exerciseType)
    {
        if (!Enum.IsDefined(typeof(ScoreExerciseType), exerciseType))
        {
            ScoreSystemLog.Error("No se puede iniciar una sesion con un tipo de ejercicio invalido.");
            Reset();
            return;
        }

        CurrentExerciseType = exerciseType;
        IsActive = true;
    }

    public void Complete()
    {
        IsActive = false;
    }

    public void Reset()
    {
        IsActive = false;
        CurrentExerciseType = default(ScoreExerciseType);
    }
}
