using UnityEngine;

public class ResultsManager : MonoBehaviour
{
    public ResultsUI resultsUI;
    void OnEnable()
    {
        GameManager.OnShowResults += HandleShowResults;
        MetricsTrackingSystem.OnTrackingStop += HandleSetResults;
    }
    void OnDisable()
    {
        GameManager.OnShowResults -= HandleShowResults;
        MetricsTrackingSystem.OnTrackingStop -= HandleSetResults;
    }

    private void HandleShowResults()
    {
        Debug.Log("RESULTSSSSS");
        resultsUI.Display();
    }

    private void HandleSetResults(ExerciseSummary leftSummary, ExerciseSummary rightSummary)
    {
        resultsUI.SetResults(leftSummary, rightSummary);
    }
}