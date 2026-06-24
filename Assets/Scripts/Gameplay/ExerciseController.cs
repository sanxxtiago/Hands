using System.Collections;
using UnityEngine;

public abstract class ExerciseController : MonoBehaviour
{
    public GameManager gameManager;
    public ExerciseProgressManager progressManager;
    public ExerciseFeedbackSystem feedbackSystem;

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
        OnExerciseStart();

        float elapsedTime = 0;


        while (!progressManager.IsCompleted())
        {
            elapsedTime += Time.deltaTime;
            //El sistema de feedback evalúa durante la duración del ejercicio
            //feedbackSystem.Evaluate(elapsedTime, Time.deltaTime);
            yield return null;
        }

        OnExerciseEnd(elapsedTime);
    }

    protected abstract void OnExerciseStart();
    protected void OnExerciseEnd(float duration)
    {
        gameManager.EndExercise(duration);
    }

}
