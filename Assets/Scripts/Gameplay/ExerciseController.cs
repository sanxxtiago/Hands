using System.Collections;
using UnityEngine;

public abstract class ExerciseController : MonoBehaviour
{
    public GameManager gameManager;
    public ResultsUI resultsUI;
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

    //DebugPrintSummary("LEFT HAND", leftSummary);
    //DebugPrintSummary("RIGHT HAND", rightSummary);

    //NUEVO
    resultsUI.Display(leftSummary, rightSummary);
}

    private void DebugPrintSummary(string label, ExerciseSummary summary)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"===== {label} | {summary.handType} =====");

        sb.AppendLine($"Duración total : {summary.totalDurationSeconds:F2}s");
        sb.AppendLine($"Tiempo activo  : {summary.totalActiveSeconds:F2}s");
        sb.AppendLine($"Ratio actividad: {summary.activityRatio:P1}");
        sb.AppendLine();

        sb.AppendLine($"{"Zona",-12} {"Absoluto",10} {"Relativo",10} {"Intensidad",12}");
        sb.AppendLine(new string('-', 48));

        for (int i = 0; i < summary.zones.Length; i++)
        {
            sb.AppendLine(
                $"{summary.zones[i],-12} " +
                $"{summary.absoluteUsage[i] * 100f,9:F1}% " +
                $"{summary.relativeUsage[i] * 100f,9:F1}% " +
                $"{summary.intensity[i],12:F3}"
            );
        }

        Debug.Log(sb.ToString());
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