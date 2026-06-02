using UnityEngine;
using UnityEngine.SceneManagement;

public class PreparationSceneLoader : MonoBehaviour
{
    [SerializeField] private HandsDetection handsDetection;
    [SerializeField] private string nextSceneName;

    private void OnEnable()
    {
        handsDetection.OnPreparationCompleted += LoadNextScene;
    }

    private void OnDisable()
    {
        handsDetection.OnPreparationCompleted -= LoadNextScene;
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}