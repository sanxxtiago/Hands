using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressionUI : MonoBehaviour
{
    [SerializeField] private Slider progressBar;
    [SerializeField] private float animationDuration = 0.35f;

    [SerializeField] private TMP_Text progressionText;

    private Tween progressTween;

    private void OnEnable()
    {
        ExerciseProgressManager.OnProgressChanged += UpdateProgressBar;
        ExerciseProgressManager.OnManagerInitialized += InitializeUI;
    }

    private void OnDisable()
    {
        ExerciseProgressManager.OnProgressChanged -= UpdateProgressBar;
        ExerciseProgressManager.OnManagerInitialized -= InitializeUI;

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
}
