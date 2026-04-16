using System.Collections;
using UnityEngine;

public abstract class ExerciseController : MonoBehaviour
{
    public Exercise type;
    public GameManager gameManager;

    [SerializeField] protected float duration = 30f;

    protected virtual void OnEnable()
    {
        gameManager.OnGameStart += StartExercise;
    }

    protected virtual void OnDisable()
    {
        gameManager.OnGameStart -= StartExercise;
    }

    public void StartExercise(Exercise exercise)
    {
        if (exercise != type) return;
        Debug.Log("empezo " + type.ToString());
        StartCoroutine(ExerciseRoutine());
    }

    IEnumerator ExerciseRoutine()
    {
        OnExerciseStart();

        float timer = duration;

        while (timer > 0f && !IsCompleted())
        {
            timer -= Time.deltaTime;
            Tick(timer);

            yield return null;
        }
        Debug.Log("Terminóoo");

        OnExerciseEnd();
    }


    protected abstract void OnExerciseStart();
    protected abstract void Tick(float timeLeft);
    protected void OnExerciseEnd()
    {
        gameManager.EndExercise();
    }
    protected abstract bool IsCompleted();

}
// private void DebugPrintSummary(string label, ExerciseSummary summary)
// {
//     var sb = new System.Text.StringBuilder();
//     sb.AppendLine($"===== {label} | {summary.handType} =====");

//     sb.AppendLine($"Duración total : {summary.totalDurationSeconds:F2}s");
//     sb.AppendLine($"Tiempo activo  : {summary.totalActiveSeconds:F2}s");
//     sb.AppendLine($"Ratio actividad: {summary.activityRatio:P1}");
//     sb.AppendLine();

//     sb.AppendLine($"{"Zona",-12} {"Absoluto",10} {"Relativo",10} {"Intensidad",12}");
//     sb.AppendLine(new string('-', 48));

//     for (int i = 0; i < summary.zones.Length; i++)
//     {
//         sb.AppendLine(
//             $"{summary.zones[i],-12} " +
//             $"{summary.absoluteUsage[i] * 100f,9:F1}% " +
//             $"{summary.relativeUsage[i] * 100f,9:F1}% " +
//             $"{summary.intensity[i],12:F3}"
//         );
//     }

//     Debug.Log(sb.ToString());
// }

//}