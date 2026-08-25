using Cinemachine;
using UnityEngine;

// Adaptador de fallos OSU y DuckHunter hacia el sistema de impulsos de Cinemachine.
public sealed class OSUFailureCameraShake : MonoBehaviour
{
    [SerializeField] private OSUSequenceRunner sequenceRunner;
    [SerializeField] private DuckSequenceRunner duckSequenceRunner;
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
            sequenceRunner = FindFirstObjectByType<OSUSequenceRunner>();

        if (duckSequenceRunner == null)
            duckSequenceRunner = FindFirstObjectByType<DuckSequenceRunner>();

        if (sequenceRunner != null)
        {
            sequenceRunner.OnTargetMissed += HandleTargetFailure;
            sequenceRunner.OnTargetFailed += HandleTargetFailure;
        }

        if (duckSequenceRunner != null)
            duckSequenceRunner.OnDuckMissed += HandleDuckFailure;

        if (sequenceRunner == null && duckSequenceRunner == null)
        {
            Debug.LogWarning("[FailureCameraShake] No se encontro un runner compatible.", this);
        }
    }

    private void OnDisable()
    {
        if (sequenceRunner != null)
        {
            sequenceRunner.OnTargetMissed -= HandleTargetFailure;
            sequenceRunner.OnTargetFailed -= HandleTargetFailure;
        }

        if (duckSequenceRunner != null)
            duckSequenceRunner.OnDuckMissed -= HandleDuckFailure;
    }

    private void HandleTargetFailure(OSUTargetScoreContext context)
    {
        TriggerShake();
    }

    private void HandleDuckFailure(DuckScoreContext context)
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
                    "[FailureCameraShake] Falta un CinemachineImpulseSource para reproducir el shake.",
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
