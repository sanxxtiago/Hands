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

    // Variable para recordar a qué pato le estamos apuntando en el frame actual
    private DuckBehaviour currentTarget;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
    }

    private void OnEnable()
    {
        poseListener.AimStarted += EnableLaser;
        poseListener.AimEnded += DisableLaser;

        // NUEVO: Nos suscribimos al evento de disparo
        poseListener.ShootStarted += HandleShoot;
    }

    private void OnDisable()
    {
        poseListener.AimStarted -= EnableLaser;
        poseListener.AimEnded -= DisableLaser;

        // NUEVO: Nos desuscribimos para evitar memory leaks
        poseListener.ShootStarted -= HandleShoot;
    }

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

        // Resetear el objetivo en cada frame antes de volver a comprobar
        currentTarget = null;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, laserLength, hitMask))
        {
            endPoint = hit.point;

            // SOLO se identifica el objetivo
            if (hit.collider.TryGetComponent(out DuckBehaviour duck))
            {
                currentTarget = duck;
                // Tip UX: Aquí podrías cambiar el color del LineRenderer a verde
                // para darle feedback al paciente de que ya lo tiene en la mira.
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

    // Este método solo se ejecuta cuando el paciente hace la pose del gatillo (Pinch)
    private void HandleShoot()
    {
        // Validamos que estemos en modo apuntar y que tengamos un pato en la mira
        if (lineRenderer.enabled && currentTarget != null)
        {
            Debug.Log($"¡PUM! Le diste al pato: {currentTarget.name}");
            currentTarget.Hit(handType);

            // Opcional: Podrías añadir un efecto visual (partículas) o de sonido aquí
        }
    }

    private void EnableLaser()
    {
        lineRenderer.enabled = true;
    }

    private void DisableLaser()
    {
        lineRenderer.enabled = false;
        currentTarget = null; // Limpiamos la mira al bajar la mano
    }
}