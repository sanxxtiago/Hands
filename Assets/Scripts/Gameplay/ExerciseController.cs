using System;
using System.Collections;
using UnityEngine;

public abstract class ExerciseController : MonoBehaviour
{
    public GameManager gameManager;
    public ExerciseProgressManager progressManager;
    public ExerciseFeedbackSystem feedbackSystem;
    public SessionRecorder sessionRecorder;

    protected float elapsedTime = 0;
    protected virtual void OnEnable()
    {
        GameManager.OnExcerciseStart += HandleStartExercise;
    }

    protected virtual void OnDisable()
    {
        GameManager.OnExcerciseStart -= HandleStartExercise;
    }

    public void HandleStartExercise()
    {
        StartCoroutine(ExerciseRoutine());
    }

    IEnumerator ExerciseRoutine()
    {
        elapsedTime = 0f;
        OnExerciseStart();

        feedbackSystem?.BeginExercise();

        
        if (SessionManager.Instance.CurrentSession == null)
        {
            SessionManager.Instance.BeginSession();
        }

        while (!IsExerciseCompleted())
        {
            elapsedTime += Time.deltaTime;
            //El sistema de feedback evalúa durante la duración del ejercicio
            feedbackSystem?.Evaluate(elapsedTime, Time.deltaTime);
            yield return null;
        }
        OnExerciseEnd(elapsedTime);
        if (SessionManager.Instance.CurrentSession != null)
        {
            SessionManager.Instance.EndSession();
        }
    }

    protected abstract void OnExerciseStart();

    protected virtual bool IsExerciseCompleted()
    {
        return progressManager != null && progressManager.IsExerciseCompleted();
    }

    protected void OnExerciseEnd(float duration)
    {
        SetSpecificData();
        gameManager.EndExercise(duration);
    }

    protected abstract void SetSpecificData();

}
