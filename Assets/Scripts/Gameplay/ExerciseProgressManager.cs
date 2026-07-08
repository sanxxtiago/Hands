using System;
using UnityEngine;

public class ExerciseProgressManager : MonoBehaviour
{
    [SerializeField] private int currentSteps;
    [SerializeField] private int targetSteps;
    public bool simulateEndExercise = false;
    public static event Action<int, int> OnProgressChanged;
    public static event Action<int> OnManagerInitialized;

    void Update()
    {
        if (simulateEndExercise)
        {
            simulateEndExercise = false;
            int stepsRemaining = targetSteps - currentSteps;
            AddStep(stepsRemaining);
        }
    }
    public float Progress()
    {
        return targetSteps > 0
            ? (float)currentSteps / targetSteps
            : 0f;
    }
    public bool IsCompleted()
    {
        return currentSteps >= targetSteps;
    }

    public void Initialize(int targetSteps)
    {
        this.targetSteps = targetSteps;
        currentSteps = 0;
        OnManagerInitialized?.Invoke(targetSteps);
    }

    public void AddStep(int amount)
    {
        currentSteps += amount;
        OnProgressChanged?.Invoke(currentSteps, targetSteps);
    }
}
