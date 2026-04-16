using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneLoader : MonoBehaviour
{
    public GameManager gameManager;
    void Start()
    {
        gameManager.StartCountdown();
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
    }
}
