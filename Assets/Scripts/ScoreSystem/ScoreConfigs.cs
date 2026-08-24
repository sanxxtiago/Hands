using System;

[Serializable]
public sealed class InsertScoreConfig
{
    public float excellentRatio = 1f;
    public float maximumRatio = 2f;
    public float baseTime = 5f;
    public float timePerPiece = 5f;
    public float rotationExtraTime = 0f;
    public float timeWeight = 0.75f;
    public float completionWeight = 0.25f;
}

[Serializable]
public sealed class OSUScoreConfig
{
    public float excellentRatio = 1f;
    public float maximumRatio = 2f;
    public float expectedReactionTime = 1f;
    public float expectedTrackingTime = 0.5f;
    public float missedTargetPenalty = 1f;
    public float reactionWeight = 0.4f;
    public float trackingWeight = 0.3f;
    public float completionWeight = 0.3f;
}

[Serializable]
public sealed class DuckHunterScoreConfig
{
    public float excellentRatio = 1f;
    public float maximumRatio = 2f;
    public float expectedReactionTime = 1f;
    public float missedDuckPenalty = 1f;
    public float reactionWeight = 0.35f;
    public float accuracyWeight = 0.65f;
}
