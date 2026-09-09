using UnityEngine;

// Sonidos de la orientación del usuario: avance de fase y encaje de la Fase 3.
public class OrientationAudioFeedback : MonoBehaviour
{
    [SerializeField] private OrientationManager phaseManager;

    [Tooltip("Opcional: slot de la Fase 3 para sonar al colocar la pieza de orientación.")]
    [SerializeField] private OrientationSlotBehaviour orientationSlot;

    private OrientationPhase2Manager phase2Manager;
    private int lastCompletedCount;

    private OrientationPhase3Manager phase3Manager;
    private OrientationSlotBehaviour spawnedSlot;

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

        // Fase 3: pieza y slot se instancian en runtime; el manager avisa
        // con OnObjectsSpawned y ahí se suscribe el encaje.
        phase3Manager = phaseManager as OrientationPhase3Manager;
        if (phase3Manager != null)
            phase3Manager.OnObjectsSpawned += HandleObjectsSpawned;
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

        if (phase3Manager != null)
        {
            phase3Manager.OnObjectsSpawned -= HandleObjectsSpawned;
            phase3Manager = null;
        }

        UnsubscribeSpawnedSlot();

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

    private void HandleObjectsSpawned(
        OrientationPieceBehaviour spawnedPiece,
        OrientationSlotBehaviour spawnedSlotBehaviour)
    {
        UnsubscribeSpawnedSlot();

        spawnedSlot = spawnedSlotBehaviour;
        if (spawnedSlot != null)
            spawnedSlot.OnPieceFitted += HandlePieceFitted;
    }

    private void UnsubscribeSpawnedSlot()
    {
        if (spawnedSlot != null)
        {
            spawnedSlot.OnPieceFitted -= HandlePieceFitted;
            spawnedSlot = null;
        }
    }

    private void HandleOrientationProgress(int completed, int total)
    {
        // El manager notifica (0, N) al arrancar; solo suena al avanzar.
        if (completed > lastCompletedCount)
            AudioManager.Play(AudioType.OrientationTargetPop);

        lastCompletedCount = completed;
    }
}
