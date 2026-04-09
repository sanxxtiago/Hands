using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{

    public LeapControllerInitialization leapControllerInitialization;
    void OnEnable()
    {
        leapControllerInitialization.OnLeapConnected += HandleOnLeapConnected;
        leapControllerInitialization.OnLeapDisconnected += HandleOnLeapDisconnected;

    }
    void HandleOnLeapConnected()
    {
        Debug.Log("Leap connected!");
        SnackbarManager.Show(SNACKBARTYPE.SUCCESS, "Leap connected!");
    }
    void HandleOnLeapDisconnected()
    {
        Debug.Log("Leap connected!");
        SnackbarManager.Show(SNACKBARTYPE.ERROR, "Leap disconnected!");
    }

#if UNITY_EDITOR
    void Update()
    {
        if (Input.GetKey(KeyCode.A))
        {
            SnackbarManager.Show(SNACKBARTYPE.SUCCESS, "Leap Success!");
        }
        if (Input.GetKey(KeyCode.S))
        {
            SnackbarManager.Show(SNACKBARTYPE.WARNING, "Leap Warning!");
        }
        if (Input.GetKey(KeyCode.D))
        {
            SnackbarManager.Show(SNACKBARTYPE.ERROR, "Leap Error!");
        }
    }
#endif

    void OnDisable()
    {
        leapControllerInitialization.OnLeapConnected -= HandleOnLeapConnected;
        leapControllerInitialization.OnLeapDisconnected -= HandleOnLeapDisconnected;

    }
}
