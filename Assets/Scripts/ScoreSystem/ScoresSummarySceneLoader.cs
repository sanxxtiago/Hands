using UnityEngine;

public sealed class ScoresSummarySceneLoader : SceneLoader
{
    [Header("Scores Summary Destinations")]
    [SerializeField] private string metricsSceneName = "SessionSummary";
    [SerializeField] private string replaySceneName = "Insert";
    [SerializeField] private string mainMenuSceneName = "Initial";

    [Header("Transition Messages")]
    [SerializeField] private string metricsTransitionMessage;
    [SerializeField] private string replayTransitionMessage;
    [SerializeField] private string mainMenuTransitionMessage;

    public void LoadMetrics()
    {
        LoadScene(metricsSceneName, metricsTransitionMessage);
    }

    public void LoadReplay()
    {
        LoadScene(replaySceneName, replayTransitionMessage);
    }

    public void LoadMainMenu()
    {
        LoadScene(mainMenuSceneName, mainMenuTransitionMessage);
    }

    private void LoadScene(string sceneName, string message)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[ScoresSummary] El nombre de la escena de destino está vacío.", this);
            return;
        }

        nextSceneName = sceneName;
        transitionMessage = message;
        LoadNextScene();
    }
}
