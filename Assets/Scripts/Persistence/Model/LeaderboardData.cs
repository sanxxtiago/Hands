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
    public DateTime RecordedAt;
    public string SessionGuid;
    public string ScoreGrade;
    public TrophyTier TrophyTier;
}
