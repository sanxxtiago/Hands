using System;

[Serializable]
public class ExerciseSummary
{
    public ExerciseType exerciseType;
    public float exerciseDuration;
    public HandUsageSummary leftHand;
    public HandUsageSummary rightHand;
}
