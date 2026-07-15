using UnityEngine;

public class SessionReader : MonoBehaviour
{
    private void Start()
    {
        SessionSummary session = SessionManager.Instance.CurrentSession;

        if (session == null)
        {
            Debug.Log("No hay una sesión activa.");
            return;
        }

        Debug.Log($"Ejercicios registrados: {session.Summaries.Count}");

        for (int i = 0; i < session.Summaries.Count; i++)
        {
            ExerciseSummary summary = session.Summaries[i];

            Debug.Log(
                $"Ejercicio {i + 1}\n" +
                $"Tipo: {summary.exerciseType}\n" +
                $"Duración: {summary.leftHand.totalDurationSeconds:F2}s"
            );
        }
    }
}