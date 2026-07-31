using UnityEngine;

public class SummaryUIActions : MonoBehaviour
{
    [SerializeField] private SummarySceneLoader sceneLoader;

    [Header("New Session")]
    [SerializeField] private string firstExerciseScene;
    [SerializeField] private string feTransitionMessage;

    [Header("User Preparation")]
    [SerializeField] private string userPreparationScece;
    [SerializeField] private string upTransitionMessage;


    public void ExitApp()
    {
#if UNITY_EDITOR
        // Stops play mode if you are running inside the Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // Closes the application if it is a standalone built game
            Application.Quit();
#endif
    }

    public void NewSession()
    {
        sceneLoader.SetSceneName(firstExerciseScene);
        sceneLoader.SetTransitionMessage(feTransitionMessage);
        sceneLoader.LoadScene();
    }

    public void UserPreparation()
    {
        sceneLoader.SetSceneName(userPreparationScece);
        sceneLoader.SetTransitionMessage(upTransitionMessage);
        sceneLoader.LoadScene();
    }
}
