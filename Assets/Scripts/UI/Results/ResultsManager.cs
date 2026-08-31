using UnityEngine;

public class ResultsManager : MonoBehaviour
{
    public ResultsUI resultsUI;
    private bool hasPendingResults;
    private float pendingDuration;
    private HandUsageSummary pendingLeft;
    private HandUsageSummary pendingRight;

    private void OnEnable()
    {
        MetricsTrackingSystem.OnTrackingStop += HandleSetResults;
        GameManager.OnExcerciseStart += ClearPendingResults;
        GameManager.OnShowResults += ApplyPendingResults;
    }

    private void OnDisable()
    {
        MetricsTrackingSystem.OnTrackingStop -= HandleSetResults;
        GameManager.OnExcerciseStart -= ClearPendingResults;
        GameManager.OnShowResults -= ApplyPendingResults;
        ClearPendingResults();
    }

    public void OpenResults()
    {
        ApplyPendingResults();
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
        pendingDuration = duration;
        pendingLeft = leftSummary;
        pendingRight = rightSummary;
        hasPendingResults = true;
    }

    private void ClearPendingResults()
    {
        hasPendingResults = false;
        pendingLeft = pendingRight = default;
    }

    private void ApplyPendingResults()
    {
        if (!hasPendingResults) return;
        if (resultsUI == null)
        {
            Debug.LogWarning("[ResultsSystem][ResultsManager] No se pueden cargar resultados sin ResultsUI.");
            return;
        }

        resultsUI.SetResults(pendingDuration, pendingLeft, pendingRight);
        ClearPendingResults();
    }
}
