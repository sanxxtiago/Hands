using Cinemachine;
using UnityEngine;

// Adaptador de patos que llegan al destino sin ser cazados hacia Cinemachine.
public sealed class HunterFailureCameraShake : MonoBehaviour
{
    [SerializeField] private DuckSequenceRunner sequenceRunner;
    [Tooltip("Fuente de impulso de Cinemachine asignada a esta camara o a otro objeto.")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField, Min(0f)] private float shakeForce = 1f;

    private bool warnedMissingImpulseSource;

    private void Awake()
    {
        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void OnEnable()
    {
        if (sequenceRunner == null)
            sequenceRunner = FindFirstObjectByType<DuckSequenceRunner>();

        if (sequenceRunner == null)
        {
            Debug.LogWarning("[HunterCameraShake] No se encontro un DuckSequenceRunner.", this);
            return;
        }

        // Un disparo incorrecto no genera shake; solo se procesa la llegada del pato.
        sequenceRunner.OnDuckMissed += HandleDuckMissed;
    }

    private void OnDisable()
    {
        if (sequenceRunner == null)
            return;

        sequenceRunner.OnDuckMissed -= HandleDuckMissed;
    }

    private void HandleDuckMissed(DuckScoreContext context)
    {
        if (!context.wasMissed || context.wasHit)
            return;

        TriggerShake();
    }

    private void TriggerShake()
    {
        if (impulseSource == null)
        {
            if (!warnedMissingImpulseSource)
            {
                Debug.LogWarning(
                    "[HunterCameraShake] Falta un CinemachineImpulseSource para reproducir el shake.",
                    this);
                warnedMissingImpulseSource = true;
            }

            return;
        }

        if (shakeForce > 0f)
            impulseSource.GenerateImpulseWithForce(shakeForce);
        else
            impulseSource.GenerateImpulse();
    }
}
