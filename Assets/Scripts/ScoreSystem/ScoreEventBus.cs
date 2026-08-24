using System;

public static class ScoreEventBus
{
    public static event Action<ExerciseScore> OnScoreCompleted;

    public static void Publish(ExerciseScore score)
    {
        if (score == null)
        {
            ScoreSystemLog.Error("No se puede publicar un score nulo.");
            return;
        }

        if (!score.isValid)
        {
            ScoreSystemLog.Warning("Se descarto la publicacion de un score invalido.");
            return;
        }

        if (!ScoreMath.IsFinite(score.totalScore))
        {
            ScoreSystemLog.Error("Se descarto la publicacion de un score no finito.");
            return;
        }

        score.totalScore = ScoreMath.ClampScore(score.totalScore);
        OnScoreCompleted?.Invoke(score);
    }
}
