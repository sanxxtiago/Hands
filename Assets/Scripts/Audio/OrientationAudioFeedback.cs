using UnityEngine;

// Sonidos de la orientación del usuario: avance de fase y encaje de la Fase 3.
public class OrientationAudioFeedback : MonoBehaviour
{
    [SerializeField] private OrientationManager phaseManager;

    [Tooltip("Opcional: slot de la Fase 3 para sonar al colocar la pieza de orientación.")]
    [SerializeField] private OrientationSlotBehaviour orientationSlot;

    private OrientationPhase2Manager phase2Manager;
    private int lastCompletedCount;

    private void OnEnable()
    {
        AudioManager.PlayLoop(AudioType.ExerciseAmbience);

        if (phaseManager == null)
            Debug.LogError("[Audio] Orientación: falta asignar el manager de fase en el feedback de audio.", this);
        else
            phaseManager.OnPhaseCompleted += HandlePhaseCompleted;

        if (orientationSlot != null)
            orientationSlot.OnPieceFitted += HandlePieceFitted;

        // Fase 2: los objetivos se instancian en runtime, así que el POP
        // se detecta por el avance del contador del manager.
        phase2Manager = phaseManager as OrientationPhase2Manager;
        if (phase2Manager != null)
        {
            lastCompletedCount = 0;
            phase2Manager.OnProgressChanged += HandleOrientationProgress;
        }
    }

    private void OnDisable()
    {
        if (phaseManager != null)
            phaseManager.OnPhaseCompleted -= HandlePhaseCompleted;

        if (orientationSlot != null)
            orientationSlot.OnPieceFitted -= HandlePieceFitted;

        if (phase2Manager != null)
        {
            phase2Manager.OnProgressChanged -= HandleOrientationProgress;
            phase2Manager = null;
        }

        AudioManager.StopLoop(AudioType.ExerciseAmbience);
    }

    private void HandlePhaseCompleted()
    {
        AudioManager.Play(AudioType.PhaseCompleted);
    }

    private void HandlePieceFitted()
    {
        AudioManager.Play(AudioType.PieceSnapped);
    }

    private void HandleOrientationProgress(int completed, int total)
    {
        // El manager notifica (0, N) al arrancar; solo suena al avanzar.
        if (completed > lastCompletedCount)
            AudioManager.Play(AudioType.OrientationTargetPop);

        lastCompletedCount = completed;
    }
}
