using System.Collections;
using UnityEngine;

public abstract class ExerciseController : MonoBehaviour
{
    public Exercise type;
    private Exercise current;
    public GameManager gameManager;

    [SerializeField] protected float exerciseDuration = 30f;

    protected virtual void OnEnable()
    {
        GameManager.OnSetExercise += HandleSetExercise;
        GameManager.OnGameStart += HandleStartExercise;
    }

    protected virtual void OnDisable()
    {
        GameManager.OnGameStart -= HandleStartExercise;
        GameManager.OnSetExercise -= HandleSetExercise;
    }

    private void HandleSetExercise(Exercise current)
    {
        this.current = current;
    }

    public void HandleStartExercise()
    {
        if (current != type) return;

        Debug.Log("empezo " + type.ToString());
        StartCoroutine(ExerciseRoutine());
    }

    IEnumerator ExerciseRoutine()
    {
        OnExerciseStart();

        float elapsedTime = 0;

        while (elapsedTime <= exerciseDuration && !IsCompleted())
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime > exerciseDuration)
            {
                elapsedTime = exerciseDuration;
                break;
            }

            Tick(elapsedTime);

            yield return null;
        }
        
        Debug.Log("Terminóoo - Time: " + (elapsedTime));
        OnExerciseEnd(elapsedTime);
    }


    protected abstract void OnExerciseStart();
    protected abstract void Tick(float timeLeft);
    protected void OnExerciseEnd(float duration)
    {
        gameManager.EndExercise(duration);
    }
    protected abstract bool IsCompleted();

}
