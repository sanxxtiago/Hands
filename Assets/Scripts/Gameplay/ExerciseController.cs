using UnityEngine;
using System.Collections;

public abstract class ExerciseController : MonoBehaviour
{
    public GameManager gameManager;
    protected ErgonomicsTracker LeftTracker => TrackerManager.Instance.left;
    protected ErgonomicsTracker RightTracker => TrackerManager.Instance.right;

    protected bool isRunning;
    [SerializeField]
    protected float duration = 30f;
    private void OnEnable()
    {
        gameManager.OnGameStart += StartExercise;
    }
    void OnDisable()
    {
        gameManager.OnGameStart -= StartExercise;
    }

    IEnumerator ExerciseRoutine()
    {
        isRunning = true;

        LeftTracker.ResetData();
        RightTracker.ResetData();

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

        isRunning = false;

        gameManager.EndExercise();
    }

    public void StartExercise()
    {
        StartCoroutine(ExerciseRoutine());
    }

    protected void ShowResults()
    {
        ShowHandResults("LEFT HAND", LeftTracker);
        ShowHandResults("RIGHT HAND", RightTracker);
    }

    void ShowHandResults(string label, ErgonomicsTracker tracker)
    {
        var abs = tracker.GetAbsoluteUsage();
        var rel = tracker.GetRelativeDistribution();
        float inactive = tracker.GetInactivePercentage();
        float active = tracker.GetActivePercentage();

        Debug.Log($"===== {label} =====");

        Debug.Log("-- Uso absoluto (sobre tiempo total) --");
        Debug.Log($"Mano: {abs.hand * 100f:F1}%");
        Debug.Log($"Muñeca: {abs.wrist * 100f:F1}%");
        Debug.Log($"Antebrazo: {abs.forearm * 100f:F1}%");

        Debug.Log("-- Distribución del movimiento (solo actividad) --");
        Debug.Log($"Mano: {rel.hand * 100f:F1}%");
        Debug.Log($"Muñeca: {rel.wrist * 100f:F1}%");
        Debug.Log($"Antebrazo: {rel.forearm * 100f:F1}%");

        Debug.Log("-- Actividad --");
        Debug.Log($"Activo: {active * 100f:F1}%");
        Debug.Log($"Inactivo: {inactive * 100f:F1}%");
    }

    protected abstract void OnExerciseStart();
    protected abstract void Tick(float timeLeft);
    protected abstract void OnExerciseEnd();
    protected abstract bool IsCompleted();
}