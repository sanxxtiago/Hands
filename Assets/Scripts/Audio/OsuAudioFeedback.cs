using UnityEngine;

// Sonoriza el timer visual del halo sobre los objetivos de OSU:
// loop corto desde el spawn, cortado al resolverse (toque, fallo o expiración).
public class OsuAudioFeedback : MonoBehaviour
{
    [SerializeField] private OSUSequenceRunner sequenceRunner;

    private void OnEnable()
    {
        if (sequenceRunner == null)
        {
            Debug.LogError("[Audio] OSU: falta asignar el OSUSequenceRunner en el feedback de audio.", this);
            return;
        }

        sequenceRunner.OnTargetSpawned += HandleTargetSpawned;
        sequenceRunner.OnTargetTouched += HandleTargetResolved;
        sequenceRunner.OnTargetCompleted += HandleTargetResolved;
        sequenceRunner.OnTargetMissed += HandleTargetResolved;
        sequenceRunner.OnTargetFailed += HandleTargetResolved;
    }

    private void OnDisable()
    {
        if (sequenceRunner == null)
            return;

        sequenceRunner.OnTargetSpawned -= HandleTargetSpawned;
        sequenceRunner.OnTargetTouched -= HandleTargetResolved;
        sequenceRunner.OnTargetCompleted -= HandleTargetResolved;
        sequenceRunner.OnTargetMissed -= HandleTargetResolved;
        sequenceRunner.OnTargetFailed -= HandleTargetResolved;
    }

    private void HandleTargetSpawned(OSUTargetScoreContext context)
    {
        AudioManager.PlayLoop(AudioType.OsuHaloTimer);
    }

    private void HandleTargetResolved(OSUTargetScoreContext context)
    {
        AudioManager.StopLoop(AudioType.OsuHaloTimer);
    }
}
