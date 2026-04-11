using UnityEngine;
using System.Collections;

public abstract class ExerciseController : MonoBehaviour
{
    protected ErgonomicsTracker LeftTracker => TrackerManager.Instance.left;
    protected ErgonomicsTracker RightTracker => TrackerManager.Instance.right;

    protected bool isRunning;
    [SerializeField]
    protected float duration = 30f;
    private void OnEnable()
    {
        GameManager.Instance.OnGameStart += StartExercise;
    }
    void OnDisable()
    {
        GameManager.Instance.OnGameStart -= StartExercise;
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

        GameManager.Instance.EndExercise();
    }

    public void StartExercise()
    {
        StartCoroutine(ExerciseRoutine());
    }

    protected void ShowResults()
    {
        var left = LeftTracker.GetPercentages();
        var right = RightTracker.GetPercentages();

        Debug.Log("===== LEFT HAND =====");
        Debug.Log($"Mano: {left.hand * 100f:F1}%");
        Debug.Log($"Muñeca: {left.wrist * 100f:F1}%");
        Debug.Log($"Antebrazo: {left.forearm * 100f:F1}%");

        Debug.Log("===== RIGHT HAND =====");
        Debug.Log($"Mano: {right.hand * 100f:F1}%");
        Debug.Log($"Muñeca: {right.wrist * 100f:F1}%");
        Debug.Log($"Antebrazo: {right.forearm * 100f:F1}%");
    }

    protected abstract void OnExerciseStart();
    protected abstract void Tick(float timeLeft);
    protected abstract void OnExerciseEnd();
    protected abstract bool IsCompleted();
}