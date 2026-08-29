using UnityEngine;

public sealed class ScoresSummaryActions : MonoBehaviour
{
    [SerializeField] private ScoresSummarySceneLoader sceneLoader;

    public void OpenMetrics()
    {
        if (sceneLoader == null)
        {
            Debug.LogWarning("[ScoresSummary] Falta ScoresSummarySceneLoader para abrir las métricas.", this);
            return;
        }

        sceneLoader.LoadMetrics();
    }

    public void ReplayFromFirstExercise()
    {
        if (sceneLoader == null)
        {
            Debug.LogWarning("[ScoresSummary] Falta ScoresSummarySceneLoader para repetir el ejercicio.", this);
            return;
        }

        sceneLoader.LoadReplay();
    }

    public void ReturnToMainMenu()
    {
        if (sceneLoader == null)
        {
            Debug.LogWarning("[ScoresSummary] Falta ScoresSummarySceneLoader para volver al menú principal.", this);
            return;
        }

        sceneLoader.LoadMainMenu();
    }
}
