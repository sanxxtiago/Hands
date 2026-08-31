using System;
using System.Collections.Generic;

public class LeaderboardData
{
    public List<LeaderboardEntry> Entries { get; set; } = new();
}

[Serializable]
public class LeaderboardEntry
{
    public string UserId;
    public string UserName;
    public float Score;
    public float? DurationSeconds;
    public DateTime RecordedAt;
    public string SessionGuid;
    public string ScoreGrade;
    public TrophyTier TrophyTier;
    public int ClassificationProfileVersion;
}

public sealed class LeaderboardQueryResult
{
    public IReadOnlyList<LeaderboardEntry> TopEntries { get; }
    public LeaderboardEntry CurrentUserEntry { get; }
    public int CurrentUserPosition { get; }
    public bool HasCurrentUserEntry => CurrentUserEntry != null;

    public LeaderboardQueryResult(
        IReadOnlyList<LeaderboardEntry> topEntries,
        LeaderboardEntry currentUserEntry,
        int currentUserPosition)
    {
        TopEntries = topEntries ?? Array.Empty<LeaderboardEntry>();
        CurrentUserEntry = currentUserEntry;
        CurrentUserPosition = currentUserPosition;
    }
}
