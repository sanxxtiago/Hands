using System.Collections.Generic;
using System.Linq;

public class ScoreService
{
    private string userId;
    private readonly ScoreClassificationCatalog classificationCatalog;
    private ScoresData scoresData = new();

    public ScoreService()
    {
    }

    public ScoreService(string userId)
    {
        SetUserContext(userId);
    }

    public ScoreService(
        string userId,
        ScoreClassificationCatalog classificationCatalog)
    {
        this.classificationCatalog = classificationCatalog;
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
            return;
        }

        scoresData.Records ??= new List<ScoreRecord>();

        if (ScoreClassificationMigration.NormalizeRecords(
            scoresData.Records,
            classificationCatalog))
        {
            Save();
        }
    }

    public void Save()
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        SaveSystem.Save(userId, SaveFiles.Scores, scoresData);
    }

    public bool UpsertScore(ScoreRecord record)
    {
        if (record == null
            || !record.isValid
            || string.IsNullOrWhiteSpace(record.sessionGuid)
            || !ScoreMath.IsFinite(record.totalScore)
            || record.totalScore < 0f
            || !ExerciseResultIdentity.TryGetExerciseType(
                record.exerciseType,
                out _))
        {
            UnityEngine.Debug.LogWarning("[ScoreService] Se rechazo un ScoreRecord invalido.");
            return false;
        }

        if (record.recordedAt == default)
            record.recordedAt = System.DateTime.Now;

        int existingIndex = scoresData.Records.FindIndex(
            existing => existing != null
                && existing.sessionGuid == record.sessionGuid
                && existing.exerciseType == record.exerciseType);

        if (existingIndex < 0)
        {
            scoresData.Records.Add(record);
            Save();
            return true;
        }

        ScoreRecord existingRecord = scoresData.Records[existingIndex];
        return ExerciseResultIdentity.AreEquivalent(existingRecord, record);
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

internal static class ScoreClassificationMigration
{
    public static bool NormalizeRecords(
        IList<ScoreRecord> records,
        ScoreClassificationCatalog catalog)
    {
        if (records == null || catalog == null)
            return false;

        bool changed = false;

        for (int i = 0; i < records.Count; i++)
        {
            ScoreRecord record = records[i];
            if (record == null || record.classificationProfileVersion > 0)
                continue;

            ScoreClassificationProfile profile = catalog.GetProfile(record.exerciseType);
            if (profile == null)
            {
                DebugLogMissingProfile(record.exerciseType);
                changed |= SetRecordClassification(
                    record,
                    "Invalid",
                    TrophyTier.None,
                    0);
                continue;
            }

            if (!record.isValid || !ScoreMath.IsFinite(record.totalScore))
            {
                if (!ScoreMath.IsFinite(record.totalScore) && record.isValid)
                {
                    record.isValid = false;
                    changed = true;
                }

                changed |= SetRecordClassification(
                    record,
                    "Invalid",
                    TrophyTier.None,
                    profile.ProfileVersion);
                continue;
            }

            if (!profile.TryResolve(record.totalScore, out ScoreClassification classification))
            {
                changed |= SetRecordClassification(
                    record,
                    "Invalid",
                    TrophyTier.None,
                    0);
                continue;
            }

            changed |= SetRecordClassification(
                record,
                classification.Grade.ToString(),
                classification.TrophyTier,
                classification.ProfileVersion);
        }

        return changed;
    }

    public static bool NormalizeLeaderboardEntry(
        LeaderboardEntry entry,
        ScoreExerciseType exerciseType,
        ScoreClassificationCatalog catalog)
    {
        if (entry == null
            || entry.ClassificationProfileVersion > 0
            || catalog == null)
        {
            return false;
        }

        ScoreClassificationProfile profile = catalog.GetProfile(exerciseType);
        if (profile == null)
        {
            DebugLogMissingProfile(exerciseType);
            return false;
        }

        if (!ScoreMath.IsFinite(entry.Score))
            return false;

        if (!profile.TryResolve(entry.Score, out ScoreClassification classification))
            return false;

        bool changed = entry.ScoreGrade != classification.Grade.ToString()
            || entry.TrophyTier != classification.TrophyTier
            || entry.ClassificationProfileVersion != classification.ProfileVersion;

        entry.ScoreGrade = classification.Grade.ToString();
        entry.TrophyTier = classification.TrophyTier;
        entry.ClassificationProfileVersion = classification.ProfileVersion;
        return changed;
    }

    private static bool SetRecordClassification(
        ScoreRecord record,
        string grade,
        TrophyTier trophyTier,
        int profileVersion)
    {
        bool changed = record.scoreGrade != grade
            || record.trophyTier != trophyTier
            || record.classificationProfileVersion != profileVersion;

        record.scoreGrade = grade;
        record.trophyTier = trophyTier;
        record.classificationProfileVersion = profileVersion;
        return changed;
    }

    private static void DebugLogMissingProfile(ScoreExerciseType exerciseType)
    {
        UnityEngine.Debug.LogError(
            $"[ScoreSystem] No existe perfil de clasificacion para {exerciseType}.");
    }
}
