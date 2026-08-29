using System;
using UnityEngine;

// Sonidos de DuckHunter: disparo por pose de gatillo y pato cazado.
public class HunterAudioFeedback : MonoBehaviour
{
    [SerializeField] private DuckSequenceRunner sequenceRunner;
    [SerializeField] private HandPoseListener[] poseListeners = Array.Empty<HandPoseListener>();

    private void OnEnable()
    {
        if (sequenceRunner == null)
            Debug.LogError("[Audio] DuckHunter: falta asignar el DuckSequenceRunner en el feedback de audio.", this);

        if (poseListeners.Length == 0)
            Debug.LogWarning("[Audio] DuckHunter: no hay HandPoseListener asignados; el disparo no sonará.", this);

        if (sequenceRunner != null)
            sequenceRunner.OnDuckHit += HandleDuckHit;

        if (sequenceRunner != null)
            sequenceRunner.OnDuckMissed += HandleDuckMissed;

        for (int i = 0; i < poseListeners.Length; i++)
        {
            if (poseListeners[i] != null)
                poseListeners[i].ShootStarted += HandleShootStarted;
        }
    }

    private void OnDisable()
    {
        if (sequenceRunner != null)
            sequenceRunner.OnDuckHit -= HandleDuckHit;

        if (sequenceRunner != null)
            sequenceRunner.OnDuckMissed -= HandleDuckMissed;

        for (int i = 0; i < poseListeners.Length; i++)
        {
            if (poseListeners[i] != null)
                poseListeners[i].ShootStarted -= HandleShootStarted;
        }
    }

    // Suena al jalar el gatillo, haya o no un pato en la mira.
    private void HandleShootStarted()
    {
        AudioManager.Play(AudioType.LaserShot);
    }

    private void HandleDuckHit(DuckScoreContext context)
    {
        AudioManager.Play(AudioType.DuckHit);
    }

    private void HandleDuckMissed(DuckScoreContext context)
    {
        AudioManager.Play(AudioType.DuckEscape);
    }
}
