using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LeaderboardService
{
    private readonly ScoreClassificationCatalog classificationCatalog;
    private readonly Dictionary<ScoreExerciseType, LeaderboardData> leaderboards =
        new Dictionary<ScoreExerciseType, LeaderboardData>
        {
            { ScoreExerciseType.Insert, new LeaderboardData() },
            { ScoreExerciseType.OSU, new LeaderboardData() },
            { ScoreExerciseType.DuckHunter, new LeaderboardData() }
        };

    public LeaderboardService(ScoreClassificationCatalog classificationCatalog = null)
    {
        this.classificationCatalog = classificationCatalog;
    }

    public void Load()
    {
        LoadLeaderboard(ScoreExerciseType.Insert);
        LoadLeaderboard(ScoreExerciseType.OSU);
        LoadLeaderboard(ScoreExerciseType.DuckHunter);
    }

    public void Save()
    {
        SaveLeaderboard(ScoreExerciseType.Insert);
        SaveLeaderboard(ScoreExerciseType.OSU);
        SaveLeaderboard(ScoreExerciseType.DuckHunter);
    }

    public bool UpdateHighscore(
        ScoreRecord record,
        string userId,
        string userName)
    {
        if (record == null || !record.isValid)
        {
            Debug.LogWarning("[LeaderboardService] Se ignoró un score inválido.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            Debug.LogWarning("[LeaderboardService] Se ignoró un score sin usuario.");
            return false;
        }

        if (!IsValidScore(record.totalScore))
        {
            Debug.LogWarning("[LeaderboardService] Se ignoró un score no válido.");
            return false;
        }

        LeaderboardData leaderboard;
        if (!leaderboards.TryGetValue(record.exerciseType, out leaderboard))
        {
            Debug.LogWarning(
                $"[LeaderboardService] No existe leaderboard para {record.exerciseType}.");
            return false;
        }

        leaderboard.Entries ??= new List<LeaderboardEntry>();

        LeaderboardEntry currentEntry = leaderboard.Entries.FirstOrDefault(
            entry => entry != null && entry.UserId == userId);

        if (currentEntry != null && record.totalScore <= currentEntry.Score)
            return false;

        LeaderboardEntry updatedEntry = new LeaderboardEntry
        {
            UserId = userId,
            UserName = userName ?? string.Empty,
            Score = record.totalScore,
            DurationSeconds = GetValidDuration(record.statsData.exerciseDuration),
            RecordedAt = record.recordedAt == default
                ? DateTime.Now
                : record.recordedAt,
            SessionGuid = record.sessionGuid,
            ScoreGrade = record.scoreGrade,
            TrophyTier = record.trophyTier,
            ClassificationProfileVersion = record.classificationProfileVersion
        };

        if (currentEntry == null)
        {
            leaderboard.Entries.Add(updatedEntry);
        }
        else
        {
            int currentIndex = leaderboard.Entries.IndexOf(currentEntry);
            leaderboard.Entries[currentIndex] = updatedEntry;
        }

        SortEntries(leaderboard.Entries);
        SaveLeaderboard(record.exerciseType);

        Debug.Log(
            $"[LeaderboardService] Highscore actualizado: usuario={userId}, " +
            $"ejercicio={record.exerciseType}, score={record.totalScore:F2}.");

        return true;
    }

    public IReadOnlyList<LeaderboardEntry> GetTopEntries(
        ScoreExerciseType exerciseType,
        int count = 5)
    {
        if (count <= 0)
            return Array.Empty<LeaderboardEntry>();

        return GetOrderedEntries(exerciseType)
            .Take(count)
            .ToList();
    }

    public LeaderboardEntry GetUserEntry(
        ScoreExerciseType exerciseType,
        string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return GetOrderedEntries(exerciseType)
            .FirstOrDefault(entry => entry.UserId == userId);
    }

    public int GetUserPosition(
        ScoreExerciseType exerciseType,
        string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return 0;
        }

        List<LeaderboardEntry> orderedEntries = GetOrderedEntries(exerciseType);

        for (int i = 0; i < orderedEntries.Count; i++)
        {
            if (orderedEntries[i].UserId == userId)
                return i + 1;
        }

        return 0;
    }

    public LeaderboardQueryResult GetLeaderboard(
        ScoreExerciseType exerciseType,
        string userId,
        int topCount = 5)
    {
        List<LeaderboardEntry> orderedEntries = GetOrderedEntries(exerciseType);
        LeaderboardEntry currentUserEntry = null;
        int currentUserPosition = 0;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            for (int i = 0; i < orderedEntries.Count; i++)
            {
                if (orderedEntries[i].UserId != userId)
                    continue;

                currentUserEntry = orderedEntries[i];
                currentUserPosition = i + 1;
                break;
            }
        }

        int safeTopCount = Math.Max(0, topCount);
        IReadOnlyList<LeaderboardEntry> topEntries = orderedEntries
            .Take(safeTopCount)
            .ToList();

        return new LeaderboardQueryResult(
            topEntries,
            currentUserEntry,
            currentUserPosition);
    }

    private List<LeaderboardEntry> GetOrderedEntries(ScoreExerciseType exerciseType)
    {
        if (!leaderboards.TryGetValue(exerciseType, out LeaderboardData leaderboard))
            return new List<LeaderboardEntry>();

        return GetOrderedEntries(leaderboard.Entries);
    }

    private void LoadLeaderboard(ScoreExerciseType exerciseType)
    {
        if (!TryGetSaveFile(exerciseType, out string fileName))
            return;

        LeaderboardData loadedData = SaveSystem.Load<LeaderboardData>(fileName)
            ?? new LeaderboardData();

        loadedData.Entries ??= new List<LeaderboardEntry>();
        leaderboards[exerciseType] = loadedData;

        bool changed = NormalizeEntries(loadedData);

        for (int i = 0; i < loadedData.Entries.Count; i++)
        {
            changed |= ScoreClassificationMigration.NormalizeLeaderboardEntry(
                loadedData.Entries[i],
                exerciseType,
                classificationCatalog);
        }

        if (changed)
            SaveLeaderboard(exerciseType);
    }

    private void SaveLeaderboard(ScoreExerciseType exerciseType)
    {
        if (!TryGetSaveFile(exerciseType, out string fileName))
            return;

        SaveSystem.Save(fileName, leaderboards[exerciseType]);
    }

    private static List<LeaderboardEntry> GetOrderedEntries(
        IEnumerable<LeaderboardEntry> entries)
    {
        if (entries == null)
            return new List<LeaderboardEntry>();

        return entries
            .Where(IsValidEntry)
            .OrderByDescending(entry => entry.Score)
            .ThenBy(GetComparableRecordedAt)
            .ThenBy(entry => entry.UserId, StringComparer.Ordinal)
            .ToList();
    }

    private static void SortEntries(List<LeaderboardEntry> entries)
    {
        List<LeaderboardEntry> orderedEntries = GetOrderedEntries(entries);
        entries.Clear();
        entries.AddRange(orderedEntries);
    }

    private static bool TryGetSaveFile(
        ScoreExerciseType exerciseType,
        out string fileName)
    {
        switch (exerciseType)
        {
            case ScoreExerciseType.Insert:
                fileName = SaveFiles.InsertLeaderboard;
                return true;
            case ScoreExerciseType.OSU:
                fileName = SaveFiles.OsuLeaderboard;
                return true;
            case ScoreExerciseType.DuckHunter:
                fileName = SaveFiles.HunterLeaderboard;
                return true;
            default:
                fileName = null;
                return false;
        }
    }

    private static bool NormalizeEntries(LeaderboardData data)
    {
        Dictionary<string, LeaderboardEntry> entriesByUser =
            new Dictionary<string, LeaderboardEntry>(StringComparer.Ordinal);

        foreach (LeaderboardEntry entry in data.Entries)
        {
            if (!IsValidEntry(entry))
                continue;

            if (!entriesByUser.TryGetValue(entry.UserId, out LeaderboardEntry currentEntry)
                || IsBetterEntry(entry, currentEntry))
            {
                entriesByUser[entry.UserId] = entry;
            }
        }

        List<LeaderboardEntry> normalizedEntries = GetOrderedEntries(entriesByUser.Values);
        bool changed = data.Entries.Count != normalizedEntries.Count;

        if (!changed)
        {
            for (int i = 0; i < data.Entries.Count; i++)
            {
                if (!ReferenceEquals(data.Entries[i], normalizedEntries[i]))
                {
                    changed = true;
                    break;
                }
            }
        }

        data.Entries.Clear();
        data.Entries.AddRange(normalizedEntries);
        return changed;
    }

    private static bool IsBetterEntry(
        LeaderboardEntry candidate,
        LeaderboardEntry current)
    {
        if (candidate.Score != current.Score)
            return candidate.Score > current.Score;

        return GetComparableRecordedAt(candidate) < GetComparableRecordedAt(current);
    }

    private static bool IsValidEntry(LeaderboardEntry entry)
    {
        return entry != null
            && !string.IsNullOrWhiteSpace(entry.UserId)
            && IsValidScore(entry.Score);
    }

    private static DateTime GetComparableRecordedAt(LeaderboardEntry entry)
    {
        return entry.RecordedAt == default
            ? DateTime.MaxValue
            : entry.RecordedAt;
    }

    private static bool IsValidScore(float value)
    {
        return !float.IsNaN(value)
            && !float.IsInfinity(value)
            && value >= 0f;
    }

    private static float? GetValidDuration(float value)
    {
        return ScoreMath.IsFinite(value) && value >= 0f
            ? value
            : (float?)null;
    }
}
