using Leap;
using System;
using System.Collections;
using UnityEngine;

public class LeapControllerInitialization : MonoBehaviour
{
    public event Action OnLeapConnected;
    public event Action OnLeapDisconnected;


    public LeapServiceProvider provider;
    private Controller controller;
    private bool wasConnected = false;
    void Start()
    {
        Application.targetFrameRate = 60;
        controller = new Controller();
        StartCoroutine(InitializeLeap());
    }

    void Update()
    {
        bool isConnected = controller.IsConnected;

        //Se conectó
        if (isConnected && !wasConnected)
        {
            StartCoroutine(InitializeLeap());
        }

        //Se desconectó
        if (!isConnected && wasConnected)
        {
            OnLeapDisconnected?.Invoke();
        }

        wasConnected = isConnected;
    }

    IEnumerator InitializeLeap()
    {
        SnackbarManager.Show(SNACKBARTYPE.WARNING, "Esperando Leap...", 1000f);
        //Esperar conexión real 
        while (!controller.IsConnected)
        {
            Debug.Log("Esperando Leap...");
            yield return null;
        }
        //Esperar a que el provider esté listo
        yield return new WaitForSeconds(0.5f);
        OnLeapConnected?.Invoke();
        //Forzar modo Desktop
        provider.ChangeTrackingMode(LeapServiceProvider.TrackingOptimizationMode.Desktop);
        Debug.Log("Modo Desktop aplicado");
    }
}