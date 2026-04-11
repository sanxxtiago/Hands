using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneLoader : MonoBehaviour
{
    void Start()
    {
        GameManager.Instance.StartExercise();
    }
    void Update()
    {
        if (Input.GetKey(KeyCode.R))
        {
            ReloadScene();
        }
    }

    public void ReloadScene()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(sceneIndex);
        SceneManager.LoadSceneAsync(1);
    }
}
