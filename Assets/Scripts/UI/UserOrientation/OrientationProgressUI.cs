using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class OrientationProgressUI : MonoBehaviour
{
    [SerializeField] private OrientationPhase2Manager orientationManager;
    [SerializeField] private Slider progressBar;
    [SerializeField] private float animationDuration = 0.35f;

    [SerializeField] private TMP_Text progressionText;

    private Tween progressTween;

    private void OnEnable()
    {
        orientationManager.OnProgressChanged += UpdateProgressBar;
    }

    private void OnDisable()
    {
        orientationManager.OnProgressChanged -= UpdateProgressBar;

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
}