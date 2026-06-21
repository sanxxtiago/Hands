using UnityEngine;

public class OrientationSceneLoader : SceneLoader
{
    [SerializeField] private OrientationManager phase;

    void OnEnable()
    {
        phase.OnPhaseCompleted += LoadNextScene;
    }

    void OnDisable()
    {
        phase.OnPhaseCompleted -= LoadNextScene;
    }
}
