using System;
using UnityEngine;

public class ExerciseProgressManager : MonoBehaviour
{
    private int completedSteps;
    private int targetSteps;
    private int processedSteps;

    public static event Action<int, int> OnProgressChanged;
    public static event Action<int> OnManagerInitialized;

    public void Initialize(int targetSteps)
    {
        this.targetSteps = targetSteps;
        completedSteps = 0;
        processedSteps = 0;

        OnManagerInitialized?.Invoke(targetSteps);
    }

    public void AddCompletedStep()
    {
        completedSteps++;
        processedSteps++;

        OnProgressChanged?.Invoke(completedSteps, targetSteps);
    }

    public void AddMissedStep()
    {
        processedSteps++;
    }

    public bool IsCompleted()
    {
        return processedSteps >= targetSteps;
    }

    public float Progress()
    {
        return targetSteps > 0
            ? (float)completedSteps / targetSteps
            : 0f;
    }
}
