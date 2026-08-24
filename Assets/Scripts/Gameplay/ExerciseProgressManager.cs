using System;
using UnityEngine;

public class ExerciseProgressManager : MonoBehaviour
{
    private int[] phaseTargets = Array.Empty<int>();
    private int[] phaseCompletedSteps = Array.Empty<int>();
    private int[] phaseProcessedSteps = Array.Empty<int>();
    private int currentPhaseIndex = -1;
    private int exerciseTargetSteps;
    private int exerciseCompletedSteps;
    private int exerciseProcessedSteps;

    // Progreso de la fase activa: los fallos se procesan, pero no cuentan como completados.
    public static event Action<int, int> OnProgressChanged;
    public static event Action<int> OnManagerInitialized;
    public static event Action<int, int> OnPhaseChanged;
    public static event Action<int, int> OnPhaseCompleted;
    public static event Action<int> OnExerciseInitialized;
    public static event Action<int, int> OnExerciseProgressChanged;
    public static event Action<int, int> OnExerciseProcessedChanged;

    public int CurrentPhaseIndex => currentPhaseIndex;
    public int PhaseCount => phaseTargets.Length;
    public int CurrentPhaseTarget => GetCurrentPhaseValue(phaseTargets);
    public int CurrentPhaseCompletedSteps => GetCurrentPhaseValue(phaseCompletedSteps);
    public int CurrentPhaseProcessedSteps => GetCurrentPhaseValue(phaseProcessedSteps);
    public int ExerciseTargetSteps => exerciseTargetSteps;
    public int ExerciseCompletedSteps => exerciseCompletedSteps;
    public int ExerciseProcessedSteps => exerciseProcessedSteps;

    public void Initialize(params int[] targets)
    {
        phaseTargets = targets ?? Array.Empty<int>();
        phaseCompletedSteps = new int[phaseTargets.Length];
        phaseProcessedSteps = new int[phaseTargets.Length];
        currentPhaseIndex = phaseTargets.Length > 0 ? 0 : -1;
        exerciseTargetSteps = 0;
        exerciseCompletedSteps = 0;
        exerciseProcessedSteps = 0;

        for (int i = 0; i < phaseTargets.Length; i++)
        {
            phaseTargets[i] = Mathf.Max(0, phaseTargets[i]);
            exerciseTargetSteps += phaseTargets[i];
        }

        OnManagerInitialized?.Invoke(CurrentPhaseTarget);
        OnExerciseInitialized?.Invoke(exerciseTargetSteps);
        OnPhaseChanged?.Invoke(currentPhaseIndex, PhaseCount);
        PublishExerciseProgress();
    }

    public bool BeginPhase(int phaseIndex)
    {
        if (phaseIndex < 0 || phaseIndex >= phaseTargets.Length)
            return false;

        currentPhaseIndex = phaseIndex;
        OnManagerInitialized?.Invoke(CurrentPhaseTarget);
        OnPhaseChanged?.Invoke(currentPhaseIndex, PhaseCount);
        OnProgressChanged?.Invoke(CurrentPhaseCompletedSteps, CurrentPhaseTarget);

        if (CurrentPhaseTarget == 0)
            PublishPhaseCompleted();

        return true;
    }

    public bool BeginNextPhase()
    {
        return IsPhaseCompleted() && BeginPhase(currentPhaseIndex + 1);
    }

    public void AddCompletedStep()
    {
        if (!CanProcessCurrentPhase())
            return;

        phaseCompletedSteps[currentPhaseIndex]++;
        phaseProcessedSteps[currentPhaseIndex]++;
        exerciseCompletedSteps++;
        exerciseProcessedSteps++;

        PublishCurrentPhaseProgress();
        PublishPhaseCompletedIfNeeded();
        PublishExerciseProgress();
    }

    public void AddMissedStep()
    {
        if (!CanProcessCurrentPhase())
            return;

        phaseProcessedSteps[currentPhaseIndex]++;
        exerciseProcessedSteps++;

        PublishCurrentPhaseProgress();
        PublishPhaseCompletedIfNeeded();
        PublishExerciseProgress();
    }

    public bool IsCompleted()
    {
        return IsPhaseCompleted();
    }

    public bool IsPhaseCompleted()
    {
        return CurrentPhaseTarget == 0 ||
            CurrentPhaseProcessedSteps >= CurrentPhaseTarget;
    }

    public bool IsExerciseCompleted()
    {
        return exerciseProcessedSteps >= exerciseTargetSteps;
    }

    public float Progress()
    {
        return CurrentPhaseTarget > 0
            ? (float)CurrentPhaseCompletedSteps / CurrentPhaseTarget
            : 0f;
    }

    public float ExerciseProgress()
    {
        return exerciseTargetSteps > 0
            ? (float)exerciseCompletedSteps / exerciseTargetSteps
            : 0f;
    }

    public float ExerciseProcessedProgress()
    {
        return exerciseTargetSteps > 0
            ? (float)exerciseProcessedSteps / exerciseTargetSteps
            : 0f;
    }

    private bool CanProcessCurrentPhase()
    {
        return currentPhaseIndex >= 0 &&
            currentPhaseIndex < phaseTargets.Length &&
            !IsPhaseCompleted();
    }

    private int GetCurrentPhaseValue(int[] values)
    {
        return currentPhaseIndex >= 0 && currentPhaseIndex < values.Length
            ? values[currentPhaseIndex]
            : 0;
    }

    private void PublishCurrentPhaseProgress()
    {
        OnProgressChanged?.Invoke(CurrentPhaseCompletedSteps, CurrentPhaseTarget);
    }

    private void PublishPhaseCompletedIfNeeded()
    {
        if (IsPhaseCompleted())
            PublishPhaseCompleted();
    }

    private void PublishPhaseCompleted()
    {
        OnPhaseCompleted?.Invoke(currentPhaseIndex, PhaseCount);
    }

    private void PublishExerciseProgress()
    {
        OnExerciseProgressChanged?.Invoke(
            exerciseCompletedSteps,
            exerciseTargetSteps);

        OnExerciseProcessedChanged?.Invoke(
            exerciseProcessedSteps,
            exerciseTargetSteps);
    }
}
