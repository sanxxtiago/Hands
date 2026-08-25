using Cinemachine;
using UnityEngine;

// Adaptador de fallos OSU hacia el sistema de impulsos de Cinemachine.
public sealed class OSUFailureCameraShake : MonoBehaviour
{
    [SerializeField] private OSUSequenceRunner sequenceRunner;
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
            sequenceRunner = FindObjectOfType<OSUSequenceRunner>();

        if (sequenceRunner == null)
        {
            Debug.LogWarning("[OSUCameraShake] No se encontro un OSUSequenceRunner.", this);
            return;
        }

        sequenceRunner.OnTargetMissed += HandleTargetFailure;
        sequenceRunner.OnTargetFailed += HandleTargetFailure;
    }

    private void OnDisable()
    {
        if (sequenceRunner == null)
            return;

        sequenceRunner.OnTargetMissed -= HandleTargetFailure;
        sequenceRunner.OnTargetFailed -= HandleTargetFailure;
    }

    private void HandleTargetFailure(OSUTargetScoreContext context)
    {
        if (impulseSource == null)
        {
            if (!warnedMissingImpulseSource)
            {
                Debug.LogWarning(
                    "[OSUCameraShake] Falta un CinemachineImpulseSource para reproducir el shake.",
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
