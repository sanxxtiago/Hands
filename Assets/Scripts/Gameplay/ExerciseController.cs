using System.Collections;
using UnityEngine;

public abstract class ExerciseController : MonoBehaviour
{
    public GameManager gameManager;

    protected ExerciseMetricsTracker leftTracker;
    protected ExerciseMetricsTracker rightTracker;

    protected bool isRunning;
    [SerializeField] protected float duration = 30f;

    protected virtual void OnEnable()
    {
        gameManager.OnGameStart += StartExercise;
    }

    protected virtual void OnDisable()
    {
        gameManager.OnGameStart -= StartExercise;
    }

    public void StartExercise()
    {
        leftTracker = new ExerciseMetricsTracker(HandType.LEFT);
        rightTracker = new ExerciseMetricsTracker(HandType.RIGHT);

        MotionEventBus.OnFrame += OnFrameReceived;

        StartCoroutine(ExerciseRoutine());
    }

    IEnumerator ExerciseRoutine()
    {
        isRunning = true;

        leftTracker.Reset();
        rightTracker.Reset();

        OnExerciseStart();

        float timer = duration;

        while (timer > 0f && !IsCompleted())
        {
            timer -= Time.deltaTime;
            Tick(timer);

            yield return null;
        }

        OnExerciseEnd();

        ShowResults();

        MotionEventBus.OnFrame -= OnFrameReceived;

        isRunning = false;

        gameManager.EndExercise();
    }

    protected void ShowResults()
    {
        var leftSummary = MetricsSummaryBuilder.Build(leftTracker);
        var rightSummary = MetricsSummaryBuilder.Build(rightTracker);

        DebugPrintSummary("LEFT HAND", leftSummary);
        DebugPrintSummary("RIGHT HAND", rightSummary);
    }

    private void DebugPrintSummary(string label, ExerciseSummary summary)
    {
        Debug.Log($"===== {label} =====");

        var zones = summary.zones;

        Debug.Log("-- Uso absoluto (activación) --");
        for (int i = 0; i < zones.Length; i++)
        {
            Debug.Log($"{zones[i]}: {summary.absoluteUsage[i] * 100f:F1}%");
        }

        Debug.Log("-- Uso relativo (distribución) --");
        for (int i = 0; i < zones.Length; i++)
        {
            Debug.Log($"{zones[i]}: {summary.relativeUsage[i] * 100f:F1}%");
        }

        Debug.Log("-- Intensidad de movimiento --");
        for (int i = 0; i < zones.Length; i++)
        {
            Debug.Log($"{zones[i]}: {summary.intensity[i]:F3}");
        }

        Debug.Log($"Duración total: {summary.totalDurationSeconds:F2}s");
        Debug.Log($"Tiempo activo: {summary.totalActiveSeconds:F2}s");
        Debug.Log($"Ratio actividad: {summary.activityRatio:P1}");
    }

    private void OnFrameReceived(FrameMotionData frame)
    {
        if (!isRunning) return;

        if (frame.handType == HandType.LEFT)
            leftTracker.OnFrameReceived(frame);

        if (frame.handType == HandType.RIGHT)
            rightTracker.OnFrameReceived(frame);
    }

    protected abstract void OnExerciseStart();
    protected abstract void Tick(float timeLeft);
    protected abstract void OnExerciseEnd();
    protected abstract bool IsCompleted();
}