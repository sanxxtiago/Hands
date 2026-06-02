using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class OrientationProgressUI : MonoBehaviour
{
    [SerializeField] private OrientationManager orientationManager;
    [SerializeField] private Slider progressBar;
    [SerializeField] private float animationDuration = 0.35f;

    private Tween progressTween;

    private void OnEnable()
    {
        orientationManager.OnProgressChanged += UpdateProgress;
    }

    private void OnDisable()
    {
        orientationManager.OnProgressChanged -= UpdateProgress;

        progressTween?.Kill();
    }

    private void UpdateProgress(float progress)
    {
        progressTween?.Kill();

        progressTween = progressBar
            .DOValue(progress, animationDuration)
            .SetEase(Ease.OutCubic);
    }
}