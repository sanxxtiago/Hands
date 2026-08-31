using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class OrientationPhase3ProgressUI : MonoBehaviour
{
    [SerializeField] private OrientationPhase3Manager orientationManager;
    [SerializeField] private TMP_Text progressionText;
    [SerializeField] private Slider percentageSlider;
    [SerializeField] private Color pendingColor = new Color(0.55f, 0.6f, 0.68f, 1f);
    [SerializeField] private Color activeColor = new Color(1f, 0.82f, 0.25f, 1f);
    [SerializeField] private Color completedColor = new Color(0.35f, 1f, 0.55f, 0.55f);
    [SerializeField, Min(0f)] private float transitionDuration = 0.25f;

    private Vector3 originalScale;

    private void Awake()
    {
        if (orientationManager == null)
            orientationManager = FindFirstObjectByType<OrientationPhase3Manager>();

        if (progressionText == null)
            progressionText = GetComponentInChildren<TMP_Text>();

        if (progressionText != null)
            originalScale = progressionText.rectTransform.localScale;

        if (percentageSlider != null)
            percentageSlider.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (orientationManager == null)
            return;

        orientationManager.OnStateChanged += HandleStateChanged;
        HandleStateChanged(orientationManager.CurrentState);
    }

    private void OnDisable()
    {
        if (orientationManager != null)
            orientationManager.OnStateChanged -= HandleStateChanged;

        if (progressionText != null)
        {
            progressionText.rectTransform.DOKill(false);
            progressionText.rectTransform.localScale = originalScale;
        }
    }

    private void HandleStateChanged(OrientationPhase3State state)
    {
        if (progressionText == null)
            return;

        int activeStep = GetActiveStep(state);
        progressionText.text = BuildProgressText(activeStep);

        progressionText.rectTransform.DOKill(false);
        progressionText.rectTransform.localScale = originalScale;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(
            progressionText.rectTransform
                .DOScale(originalScale * 1.08f, transitionDuration * 0.5f)
                .SetEase(Ease.OutQuad));
        sequence.Append(
            progressionText.rectTransform
                .DOScale(originalScale, transitionDuration * 0.5f)
                .SetEase(Ease.InOutSine));
    }

    private string BuildProgressText(int activeStep)
    {
        return string.Concat(
            FormatStep("Tomar", 0, activeStep),
            "   ",
            FormatStep("Mover", 1, activeStep),
            "   ",
            FormatStep("Encajar", 2, activeStep));
    }

    private string FormatStep(string label, int step, int activeStep)
    {
        Color color = step < activeStep || activeStep == 3
            ? completedColor
            : step == activeStep
                ? activeColor
                : pendingColor;

        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{label}</color>";
    }

    private static int GetActiveStep(OrientationPhase3State state)
    {
        switch (state)
        {
            case OrientationPhase3State.Moving:
                return 1;
            case OrientationPhase3State.ReadyToRelease:
                return 2;
            case OrientationPhase3State.Completed:
                return 3;
            default:
                return 0;
        }
    }
}
