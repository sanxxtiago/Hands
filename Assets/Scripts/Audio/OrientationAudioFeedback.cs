using UnityEngine;

// Sonidos de la orientación del usuario: avance de fase y encaje de la Fase 3.
public class OrientationAudioFeedback : MonoBehaviour
{
    [SerializeField] private OrientationManager phaseManager;

    [Tooltip("Opcional: slot de la Fase 3 para sonar al colocar la pieza de orientación.")]
    [SerializeField] private OrientationSlotBehaviour orientationSlot;

    private void OnEnable()
    {
        if (phaseManager == null)
            Debug.LogError("[Audio] Orientación: falta asignar el manager de fase en el feedback de audio.", this);
        else
            phaseManager.OnPhaseCompleted += HandlePhaseCompleted;

        if (orientationSlot != null)
            orientationSlot.OnPieceFitted += HandlePieceFitted;
    }

    private void OnDisable()
    {
        if (phaseManager != null)
            phaseManager.OnPhaseCompleted -= HandlePhaseCompleted;

        if (orientationSlot != null)
            orientationSlot.OnPieceFitted -= HandlePieceFitted;
    }

    private void HandlePhaseCompleted()
    {
        AudioManager.Play(AudioType.PhaseCompleted);
    }

    private void HandlePieceFitted()
    {
        AudioManager.Play(AudioType.PieceSnapped);
    }
}
