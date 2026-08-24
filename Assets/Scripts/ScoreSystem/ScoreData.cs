using System;

[Serializable]
public sealed class InsertScoreData
{
    public float completionTime;
    public int totalPieces;
    public int completedPieces;
    public int phaseCount;
    public float[] phaseTimes = Array.Empty<float>();
}

[Serializable]
public struct InsertPieceResult
{
    public float placementTime;
    public bool wasPlaced;
}

[Serializable]
public struct OSUTargetScoreData
{
    public int targetIndex;
    public float reactionTime;
    public float timeOutsidePath;
    public bool wasTouched;
    public bool wasCompleted;
    public bool wasMissed;
    public bool hadPath;
}

[Serializable]
public sealed class OSUScoreData
{
    public int totalTargets;
    public int completedTargets;
    public int missedTargets;
    public float totalReactionTime;
    public float totalTimeOutsidePath;
    public OSUTargetScoreData[] targets = Array.Empty<OSUTargetScoreData>();
}

[Serializable]
public struct DuckScoreData
{
    public int duckIndex;
    public float reactionTime;
    public float availableTime;
    public bool wasHit;
    public bool wasMissed;
}

[Serializable]
public sealed class DuckHunterScoreData
{
    public int totalDucks;
    public int ducksHit;
    public int ducksMissed;
    public float totalReactionTime;
    public DuckScoreData[] ducks = Array.Empty<DuckScoreData>();
}
