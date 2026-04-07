using Leap;
using System.Collections;
using UnityEngine;

public class LeapControllerInitialization : MonoBehaviour
{
    public LeapServiceProvider provider;
    private Controller controller;

    void Start()
    {
        controller = new Controller();
        StartCoroutine(InitializeLeap());
    }

    IEnumerator InitializeLeap()
    {
        // Esperar conexión real
        while (!controller.IsConnected)
        {
            Debug.Log("Esperando Leap...");
            yield return null;
        }

        Debug.Log("Leap conectado");

        // Esperar a que el provider esté listo
        yield return new WaitForSeconds(0.5f);

        // Ahora sí forzar modo
        provider.ChangeTrackingMode(LeapServiceProvider.TrackingOptimizationMode.Desktop);

        Debug.Log("Modo Desktop aplicado");
    }
}