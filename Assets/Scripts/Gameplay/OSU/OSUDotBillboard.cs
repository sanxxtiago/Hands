using UnityEngine;

[DisallowMultipleComponent]
public sealed class OSUDotBillboard : MonoBehaviour
{
    [Tooltip("Camara que debe mirar el objetivo. Si queda vacia, se usa la camara marcada como MainCamera.")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("Correccion de orientacion local si el frente del prefab necesita un ajuste.")]
    [SerializeField] private Vector3 facingOffsetEuler;

    private Transform targetCameraTransform;

    private void Awake()
    {
        ResolveCamera();
    }

    private void LateUpdate()
    {
        if (!ResolveCamera())
            return;

        // Todos los dots comparten el plano de la camara; la perspectiva aporta
        // la inclinacion natural de los objetivos alejados del centro.
        Quaternion cameraPlaneRotation = Quaternion.LookRotation(
            -targetCameraTransform.forward,
            targetCameraTransform.up);

        transform.rotation =
            cameraPlaneRotation * Quaternion.Euler(facingOffsetEuler);
    }

    private bool ResolveCamera()
    {
        if (targetCameraTransform != null)
            return true;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return false;

        targetCameraTransform = targetCamera.transform;
        return true;
    }
}
