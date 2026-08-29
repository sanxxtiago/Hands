using System.Collections.Generic;
using System.Linq;

public class ScoreService
{
    private string userId;
    private ScoresData scoresData = new();

    public ScoreService()
    {
    }

    public ScoreService(string userId)
    {
        SetUserContext(userId);
    }

    public IReadOnlyList<ScoreRecord> Records => scoresData.Records;

    public int TotalRecords => scoresData.Records.Count;

    public void SetUserContext(string userId)
    {
        this.userId = userId;
        scoresData = new ScoresData();
    }

    public void Load()
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            scoresData = new ScoresData();
            return;
        }

        scoresData = SaveSystem.Load<ScoresData>(userId, SaveFiles.Scores);

        if (scoresData == null)
        {
            scoresData = new ScoresData();
        }
    }

    public void Save()
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        SaveSystem.Save(userId, SaveFiles.Scores, scoresData);
    }

    public ScoreRecord AddScore(ScoreRecord record)
    {
        if (record == null)
        {
            UnityEngine.Debug.LogWarning("[ScoreService] Se ignoró un ScoreRecord nulo.");
            return null;
        }

        if (record.recordedAt == default)
            record.recordedAt = System.DateTime.Now;

        scoresData.Records.Add(record);
        Save();
        return record;
    }

    public IReadOnlyList<ScoreRecord> GetScoresForSession(string sessionGuid)
    {
        if (string.IsNullOrWhiteSpace(sessionGuid))
            return System.Array.Empty<ScoreRecord>();

        return scoresData.Records
            .Where(r => r.sessionGuid == sessionGuid)
            .OrderBy(r => r.exerciseIndex)
            .ToList();
    }

    public IReadOnlyList<ScoreRecord> GetLastScores(int count)
    {
        return scoresData.Records
            .OrderByDescending(r => r.recordedAt)
            .Take(count)
            .Reverse()
            .ToList();
    }

    public void DeleteAll()
    {
        if (!string.IsNullOrWhiteSpace(userId))
            SaveSystem.Delete(userId, SaveFiles.Scores);

        scoresData = new ScoresData();
    }
}
