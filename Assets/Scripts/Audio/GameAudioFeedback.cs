using UnityEngine;

// Puente global de audio para eventos estáticos del juego.
// Un componente por escena de ejercicio; sin referencias en Inspector.
public class GameAudioFeedback : MonoBehaviour
{
    private void OnEnable()
    {
        GameManager.OnExcerciseStart += HandleExerciseStart;
        GameManager.OnExerciseEnd += HandleExerciseEnd;
        ExerciseProgressManager.OnPhaseCompleted += HandlePhaseCompleted;
        PieceBehaviour.OnPieceSnapped += HandlePieceSnapped;
    }

    private void OnDisable()
    {
        GameManager.OnExcerciseStart -= HandleExerciseStart;
        GameManager.OnExerciseEnd -= HandleExerciseEnd;
        ExerciseProgressManager.OnPhaseCompleted -= HandlePhaseCompleted;
        PieceBehaviour.OnPieceSnapped -= HandlePieceSnapped;
    }

    private void HandleExerciseStart()
    {
        AudioManager.PlayLoop(AudioType.ExerciseAmbience);
    }

    private void HandleExerciseEnd(float duration)
    {
        AudioManager.StopLoop(AudioType.ExerciseAmbience);
        AudioManager.Play(AudioType.ExerciseCompleted);
    }

    private void HandlePhaseCompleted(int phaseIndex, int phaseCount)
    {
        AudioManager.Play(AudioType.PhaseCompleted);
    }

    private void HandlePieceSnapped(PieceBehaviour piece)
    {
        AudioManager.Play(AudioType.PieceSnapped);
    }
}
