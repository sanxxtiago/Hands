using UnityEngine;

public class PreparationSceneLoader : SceneLoader
{
    [SerializeField] private HandsDetection handsDetection;

    private void OnEnable()
    {
        handsDetection.OnPreparationCompleted += LoadNextScene;
    }

    private void OnDisable()
    {
        handsDetection.OnPreparationCompleted -= LoadNextScene;
    }

   
}