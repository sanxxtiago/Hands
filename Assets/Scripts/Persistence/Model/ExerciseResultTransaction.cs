using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public enum ExerciseCommitOutcome
{
    Pending,
    Committed,
    Duplicate,
    Conflict,
    Rejected,
    Failed
}

[Serializable]
public sealed class ExerciseResultTransaction
{
    public string userId;
    public string userName;
    public string sessionGuid;
    public int sessionId;
    public SessionSummary session;
    public List<ExerciseResultTransactionItem> items = new();
}

[Serializable]
public sealed class ExerciseResultTransactionItem
{
    public string idempotencyKey;
    public ExerciseType exerciseType;
    public ExerciseSummary summary;
    public ScoreRecord score;
}

internal static class ExerciseResultIdentity
{
    public const int RequiredExerciseCount = 3;

    public static string CreateKey(string sessionGuid, ExerciseType exerciseType)
    {
        return sessionGuid + "|" + exerciseType;
    }

    public static bool TryGetExerciseType(
        ScoreExerciseType scoreExerciseType,
        out ExerciseType exerciseType)
    {
        switch (scoreExerciseType)
        {
            case ScoreExerciseType.Insert:
                exerciseType = ExerciseType.Insert;
                return true;
            case ScoreExerciseType.OSU:
                exerciseType = ExerciseType.OSU;
                return true;
            case ScoreExerciseType.DuckHunter:
                exerciseType = ExerciseType.DuckHunter;
                return true;
            default:
                exerciseType = default;
                return false;
        }
    }

    public static bool TryGetScoreExerciseType(
        ExerciseType exerciseType,
        out ScoreExerciseType scoreExerciseType)
    {
        switch (exerciseType)
        {
            case ExerciseType.Insert:
                scoreExerciseType = ScoreExerciseType.Insert;
                return true;
            case ExerciseType.OSU:
                scoreExerciseType = ScoreExerciseType.OSU;
                return true;
            case ExerciseType.DuckHunter:
                scoreExerciseType = ScoreExerciseType.DuckHunter;
                return true;
            default:
                scoreExerciseType = default;
                return false;
        }
    }

    public static bool IsCompleteSession(SessionSummary session)
    {
        if (session == null || session.Summaries == null
            || session.Summaries.Count != RequiredExerciseCount)
        {
            return false;
        }

        HashSet<ExerciseType> exerciseTypes = new();
        foreach (ExerciseSummary summary in session.Summaries)
        {
            if (summary == null || !exerciseTypes.Add(summary.exerciseType))
                return false;
        }

        return exerciseTypes.Contains(ExerciseType.Insert)
            && exerciseTypes.Contains(ExerciseType.OSU)
            && exerciseTypes.Contains(ExerciseType.DuckHunter);
    }

    public static bool AreEquivalent<T>(T left, T right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left == null || right == null)
            return false;

        string leftJson = JsonConvert.SerializeObject(left);
        string rightJson = JsonConvert.SerializeObject(right);
        return string.Equals(leftJson, rightJson, StringComparison.Ordinal);
    }
}
