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

        Debug.Log("-- Uso absoluto --");
        Debug.Log($"Hand: {summary.absoluteUsage[0] * 100f:F1}%");
        Debug.Log($"Wrist: {summary.absoluteUsage[1] * 100f:F1}%");
        Debug.Log($"Forearm: {summary.absoluteUsage[2] * 100f:F1}%");

        Debug.Log("-- Uso relativo --");
        Debug.Log($"Hand: {summary.relativeUsage[0] * 100f:F1}%");
        Debug.Log($"Wrist: {summary.relativeUsage[1] * 100f:F1}%");
        Debug.Log($"Forearm: {summary.relativeUsage[2] * 100f:F1}%");

        Debug.Log($"Duración total: {summary.totalDurationSeconds:F2}s");
        Debug.Log($"Tiempo activo: {summary.totalActiveSeconds:F2}s");
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