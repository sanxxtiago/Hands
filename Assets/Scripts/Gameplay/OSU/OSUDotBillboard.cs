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

        Vector3 toCamera = targetCameraTransform.position - transform.position;

        if (toCamera.sqrMagnitude <= 0.000001f)
            return;

        Vector3 cameraUp = targetCameraTransform.up;

        if (Mathf.Abs(Vector3.Dot(toCamera.normalized, cameraUp)) > 0.9999f)
            cameraUp = targetCameraTransform.right;

        Quaternion cameraFacingRotation =
            Quaternion.LookRotation(toCamera, cameraUp);

        transform.rotation =
            cameraFacingRotation * Quaternion.Euler(facingOffsetEuler);
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
