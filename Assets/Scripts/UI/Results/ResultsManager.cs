using UnityEngine;

public class ResultsManager : MonoBehaviour
{
    public ResultsUI resultsUI;

    private void OnEnable()
    {
        MetricsTrackingSystem.OnTrackingStop += HandleSetResults;
    }

    private void OnDisable()
    {
        MetricsTrackingSystem.OnTrackingStop -= HandleSetResults;
    }

    public void OpenResults()
    {
        if (resultsUI == null)
        {
            Debug.LogWarning("[ResultsSystem][ResultsManager] Falta la referencia a ResultsUI.");
            return;
        }

        resultsUI.Display();
    }

    public void CloseResults()
    {
        if (resultsUI == null)
        {
            Debug.LogWarning("[ResultsSystem][ResultsManager] Falta la referencia a ResultsUI.");
            return;
        }

        resultsUI.Hide();
    }

    private void HandleSetResults(float duration, HandUsageSummary leftSummary, HandUsageSummary rightSummary)
    {
        if (resultsUI == null)
        {
            Debug.LogWarning("[ResultsSystem][ResultsManager] No se pueden cargar resultados sin ResultsUI.");
            return;
        }

        resultsUI.SetResults(duration, leftSummary, rightSummary);
    }
}
