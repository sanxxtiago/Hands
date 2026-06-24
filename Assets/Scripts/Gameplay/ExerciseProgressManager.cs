using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExerciseProgressManager : MonoBehaviour
{
    public int currentSteps;
    public int targetSteps;
    public bool simulateEndExercise = false;
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
    }

    public void AddStep(int amount = 1)
    {
        currentSteps += amount;
    }
}
