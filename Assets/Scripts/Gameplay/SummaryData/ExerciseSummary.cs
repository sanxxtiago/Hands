using System;

[Serializable]
public class ExerciseSummary
{
    public ExerciseType exerciseType;
    public float exerciseDuration;
    public HandUsageSummary leftHand;
    public HandUsageSummary rightHand;

    //OSU
    public float totalInteractionDelay;
    public int interactionCount;

    //Duck Hunter
    public int ducksHit;
    public int ducksMissed;

    //Insert
    public float completionTime;

    //Suggestion
    public string generalSuggestion;
}
