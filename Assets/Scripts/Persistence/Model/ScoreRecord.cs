using System;

[Serializable]
public class ScoreRecord
{
    public string sessionGuid;
    public int sessionIdNumeric;
    public int exerciseIndex;
    public ScoreExerciseType exerciseType;
    public DateTime recordedAt;

    public float totalScore;
    public string scoreGrade;
    public TrophyTier trophyTier;
    public string motivationalMessage;
    public bool isValid;

    public ScoreStatsData statsData;
    public ScoreBreakdown[] breakdown;

    public ScoreRecord()
    {
        recordedAt = DateTime.Now;
        breakdown = Array.Empty<ScoreBreakdown>();
        statsData = new ScoreStatsData();
        scoreGrade = "Invalid";
        trophyTier = TrophyTier.Bronze;
        isValid = false;
    }
}
