using System.Collections.Generic;
using System.Linq;

public class ScoreService
{
    private ScoresData scoresData = new();

    public IReadOnlyList<ScoreRecord> Records => scoresData.Records;

    public int TotalRecords => scoresData.Records.Count;

    public void Load()
    {
        scoresData = SaveSystem.Load<ScoresData>(SaveFiles.Scores);

        if (scoresData == null)
        {
            scoresData = new ScoresData();
        }
    }

    public void Save()
    {
        SaveSystem.Save(SaveFiles.Scores, scoresData);
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
        SaveSystem.Delete(SaveFiles.Scores);
        scoresData = new ScoresData();
    }
}
