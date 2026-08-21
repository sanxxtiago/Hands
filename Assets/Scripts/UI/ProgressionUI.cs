using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressionUI : MonoBehaviour
{
    [SerializeField] private Slider progressBar;
    [SerializeField] private float animationDuration = 0.35f;

    [SerializeField] private TMP_Text progressionText;
    [SerializeField] private TMP_Text phaseText;

    private Tween progressTween;

    private void OnEnable()
    {
        ExerciseProgressManager.OnProgressChanged += UpdateProgressBar;
        ExerciseProgressManager.OnManagerInitialized += InitializeUI;
        ExerciseProgressManager.OnPhaseChanged += UpdatePhase;
    }

    private void OnDisable()
    {
        ExerciseProgressManager.OnProgressChanged -= UpdateProgressBar;
        ExerciseProgressManager.OnManagerInitialized -= InitializeUI;
        ExerciseProgressManager.OnPhaseChanged -= UpdatePhase;

        progressTween?.Kill();
    }

    private void UpdateProgressBar(int _completedObjectives, int objectivesToComplete)
    {
        progressionText.text = $"{_completedObjectives}/{objectivesToComplete}";
        float progress =
            objectivesToComplete == 0
            ? 0
            : _completedObjectives;
        progressTween?.Kill();

        progressTween = progressBar
            .DOValue(progress, animationDuration)
            .SetEase(Ease.OutCubic);
    }

    private void InitializeUI(int targetCount)
    {
        progressionText.text = $"0/{targetCount}";
        progressBar.maxValue = targetCount;
    }

    private void UpdatePhase(int phaseIndex, int phaseCount)
    {
        if (phaseText == null)
            return;

        phaseText.text = phaseCount > 0
            ? $"Fase {phaseIndex + 1}/{phaseCount}"
            : string.Empty;
    }
}
