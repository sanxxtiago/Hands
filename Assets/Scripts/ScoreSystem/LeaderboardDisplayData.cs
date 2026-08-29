using System;
using System.Collections.Generic;

public sealed class LeaderboardDisplayData
{
    public IReadOnlyList<LeaderboardRowData> Rows { get; }
    public string StatusMessage { get; }

    public LeaderboardDisplayData(
        IReadOnlyList<LeaderboardRowData> rows,
        string statusMessage)
    {
        Rows = rows ?? Array.Empty<LeaderboardRowData>();
        StatusMessage = statusMessage ?? string.Empty;
    }
}

public sealed class LeaderboardRowData
{
    public bool IsVisible { get; }
    public int Position { get; }
    public string UserName { get; }
    public float Score { get; }
    public float? DurationSeconds { get; }
    public bool IsCurrentUser { get; }

    private LeaderboardRowData(
        bool isVisible,
        int position,
        string userName,
        float score,
        float? durationSeconds,
        bool isCurrentUser)
    {
        IsVisible = isVisible;
        Position = position;
        UserName = userName ?? string.Empty;
        Score = score;
        DurationSeconds = durationSeconds;
        IsCurrentUser = isCurrentUser;
    }

    public static LeaderboardRowData FromEntry(
        LeaderboardEntry entry,
        int position,
        bool isCurrentUser)
    {
        if (entry == null)
            return Hidden();

        string userName = string.IsNullOrWhiteSpace(entry.UserName)
            ? "Jugador"
            : entry.UserName.Trim();

        return new LeaderboardRowData(
            true,
            position,
            userName,
            entry.Score,
            entry.DurationSeconds,
            isCurrentUser);
    }

    public static LeaderboardRowData Hidden()
    {
        return new LeaderboardRowData(false, 0, string.Empty, 0f, null, false);
    }
}
