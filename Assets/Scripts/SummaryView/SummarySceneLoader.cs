public class SummarySceneLoader : SceneLoader
{
    public void SetSceneName(string name)
    {
        nextSceneName = name;
    }

    public void SetTransitionMessage(string msg)
    {
        transitionMessage = msg;
    }

    public void LoadScene()
    {
        LoadNextScene();
    }
}
