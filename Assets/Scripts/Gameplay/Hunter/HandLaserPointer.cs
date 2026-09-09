using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HandLaserPointer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameplayHandInput handInput;
    [SerializeField] private HandPoseListener poseListener;

    [Header("Settings")]
    [SerializeField] private HandType handType = HandType.RIGHT;
    [Tooltip("Distancia máxima del rayo. El límite real es la pared (capa HunterWall), salvo que un pato se cruce antes.")]
    [SerializeField, Min(0f)] private float maxLaserDistance = 2f;
    [SerializeField] private float laserOriginOffset = 0.02f;
    [Tooltip("Capas que detienen el láser: DuckLayer + HunterWall. El hit más cercano manda, así el pato tapa a la pared.")]
    [SerializeField] private LayerMask hitMask;

    [Header("Retícula")]
    [Tooltip("Retícula de mundo que se ancla al punto final del láser.")]
    [SerializeField] private HandLaserReticle reticle;

    private LineRenderer lineRenderer;

    // Variable para recordar a qué pato le estamos apuntando en el frame actual
    private DuckBehaviour currentTarget;
    private bool isAiming;
    private bool isShooting;
    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
    }

    private void OnEnable()
    {
        poseListener.AimStarted += HandleAimStarted;
        poseListener.AimEnded += HandleAimEnded;

        poseListener.ShootStarted += HandleShootStarted;
        poseListener.ShootEnded += HandleShootEnded;
    }

    private void OnDisable()
    {
        poseListener.AimStarted -= HandleAimStarted;
        poseListener.AimEnded -= HandleAimEnded;

        poseListener.ShootStarted -= HandleShootStarted;
        poseListener.ShootEnded -= HandleShootEnded;
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
        Vector3 facing = direction;
        bool alignToSurface = false;
        float traveledDistance = maxLaserDistance;

        // Resetear el objetivo en cada frame antes de volver a comprobar
        currentTarget = null;

        // El rayo llega hasta la pared; si un pato se cruza antes, ese hit es el más cercano y manda.
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxLaserDistance, hitMask))
        {
            endPoint = hit.point;
            facing = hit.normal;
            alignToSurface = true;
            traveledDistance = hit.distance;

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
            // Sin impacto (p. ej. apuntando fuera de la pared): el láser flota a distancia máxima.
            endPoint = origin + direction * maxLaserDistance;
        }

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);

        reticle?.UpdatePose(endPoint, facing, traveledDistance, alignToSurface);

        Debug.DrawRay(origin, direction * maxLaserDistance, Color.red);
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
        reticle?.Show();
    }

    private void DisableLaser()
    {
        lineRenderer.enabled = false;
        currentTarget = null; // Limpiamos la mira al bajar la mano
        reticle?.Hide();
    }

    private void HandleAimStarted()
    {
        isAiming = true;
        UpdateLaserState();
    }

    private void HandleAimEnded()
    {
        isAiming = false;
        UpdateLaserState();
    }

    private void HandleShootStarted()
    {
        isShooting = true;
        UpdateLaserState();

        if (currentTarget != null)
        {
            Debug.Log($"¡PUM! Le diste al pato: {currentTarget.name}");
            currentTarget.Hit(handType);
        }
    }

    private void HandleShootEnded()
    {
        isShooting = false;
        UpdateLaserState();
    }

    private void UpdateLaserState()
    {
        bool enabled = isAiming || isShooting;

        if (enabled)
            EnableLaser();
        else
            DisableLaser();
    }
}