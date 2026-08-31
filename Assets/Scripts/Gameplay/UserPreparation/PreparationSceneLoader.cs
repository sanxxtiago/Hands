using UnityEngine;

public class PreparationSceneLoader : SceneLoader
{
    [SerializeField] private HandsPreparationFlow preparationFlow;

    private void OnEnable()
    {
        if (preparationFlow == null)
        {
            Debug.LogError(
                "[PreparationSceneLoader] Falta asignar HandsPreparationFlow.",
                this);
            return;
        }

        preparationFlow.OnPreparationCompleted += LoadNextScene;
    }

    private void OnDisable()
    {
        if (preparationFlow != null)
            preparationFlow.OnPreparationCompleted -= LoadNextScene;
    }

}
