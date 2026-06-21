using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneLoader : MonoBehaviour
{
    [SerializeField] protected string nextSceneName;
    [SerializeField] protected Transition transition;
    [SerializeField] protected string transitionMessage;
    void Start()
    {
        transition.FadeOut();
    }
    void Update()
    {
        if (Input.GetKey(KeyCode.R))
        {
            ReloadScene();
        }
    }

    protected void LoadNextScene()
    {
        transition.SetMessage(transitionMessage);
        transition.FadeIn(() => SceneManager.LoadScene(nextSceneName), 3f);
    }
    protected void ReloadScene()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(sceneIndex);
    }
}
