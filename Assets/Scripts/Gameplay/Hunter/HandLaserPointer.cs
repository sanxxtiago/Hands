using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HandLaserPointer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameplayHandInput handInput;
    [SerializeField] private HandPoseListener poseListener;

    [Header("Settings")]
    [SerializeField] private HandType handType = HandType.RIGHT;
    [SerializeField] private float laserLength = 2f;
    [SerializeField] private float laserOriginOffset = 0.02f;
    [SerializeField] private LayerMask hitMask;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
    }

    //TESTEAR EL RAYO SIN LA DETECCIÓN DEL GESTO
    // private void OnEnable()
    // {
    //     poseListener.AimStarted += EnableLaser;
    //     poseListener.AimEnded += DisableLaser;
    // }

    // private void OnDisable()
    // {
    //     poseListener.AimStarted -= EnableLaser;
    //     poseListener.AimEnded -= DisableLaser;
    // }

    private void Update()
    {
        if (!lineRenderer.enabled)
            return;

        GameplayHandData? hand = handInput.GetHand(handType);

        if (!hand.HasValue)
            return;

        Vector3 direction = hand.Value.rotation * Vector3.forward;
        Vector3 origin = hand.Value.position + direction * laserOriginOffset;

        Vector3 endPoint = Vector3.zero;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, laserLength, hitMask))
        {
            Debug.Log($"Hit: {hit.collider.name}");
            if (hit.collider.TryGetComponent(out DuckBehaviour duck))
            {
                duck.Hit();
                endPoint = hit.point;

            }
        }
        else
        {
            endPoint = origin + direction * laserLength;
        }


        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);

        Debug.DrawRay(origin, direction * laserLength, Color.red);
    }

    private void EnableLaser()
    {
        lineRenderer.enabled = true;
    }

    private void DisableLaser()
    {
        lineRenderer.enabled = false;
    }
}