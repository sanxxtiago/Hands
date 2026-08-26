using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class VignetteFeedbackEffect : MonoBehaviour
{
    [Tooltip("Imagen blanca con alpha radial para los fallos; su color se configura desde Image.color.")]
    [SerializeField] private Image failureImage;
    [Tooltip("Imagen blanca con alpha radial para completar una fase; su color se configura desde Image.color.")]
    [SerializeField] private Image successImage;
    [SerializeField] private OSUSequenceRunner osuSequenceRunner;
    [SerializeField] private DuckSequenceRunner duckSequenceRunner;
    [SerializeField, Range(0f, 1f)] private float failurePeakAlpha = 0.22f;
    [SerializeField, Range(0f, 1f)] private float successPeakAlpha = 0.12f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.06f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.4f;

    private Tween failureTween;
    private Tween successTween;

    private void Awake()
    {
        if (successImage == null)
            successImage = GetComponent<Image>();

        if (failureImage == null && successImage == null)
        {
            Debug.LogError("[VignetteFeedback] Falta la Image del efecto.", this);
            enabled = false;
            return;
        }

        ConfigureImage(failureImage);
        ConfigureImage(successImage);
        SetAlpha(failureImage, 0f);
        SetAlpha(successImage, 0f);
    }

    private void OnEnable()
    {
        if (osuSequenceRunner == null)
            osuSequenceRunner = FindFirstObjectByType<OSUSequenceRunner>();

        if (duckSequenceRunner == null)
            duckSequenceRunner = FindFirstObjectByType<DuckSequenceRunner>();

        if (osuSequenceRunner != null)
        {
            osuSequenceRunner.OnTargetMissed += HandleTargetFailure;
            osuSequenceRunner.OnTargetFailed += HandleTargetFailure;
        }

        if (duckSequenceRunner != null)
            duckSequenceRunner.OnDuckMissed += HandleDuckMissed;

        ExerciseProgressManager.OnPhaseCompleted += HandlePhaseCompleted;
    }

    private void OnDisable()
    {
        if (osuSequenceRunner != null)
        {
            osuSequenceRunner.OnTargetMissed -= HandleTargetFailure;
            osuSequenceRunner.OnTargetFailed -= HandleTargetFailure;
        }

        if (duckSequenceRunner != null)
            duckSequenceRunner.OnDuckMissed -= HandleDuckMissed;

        ExerciseProgressManager.OnPhaseCompleted -= HandlePhaseCompleted;
        StopVignettes();
    }

    private void HandleTargetFailure(OSUTargetScoreContext context)
    {
        PlayFailure();
    }

    private void HandleDuckMissed(DuckScoreContext context)
    {
        if (!context.wasMissed || context.wasHit)
            return;

        PlayFailure();
    }

    private void HandlePhaseCompleted(int phaseIndex, int phaseCount)
    {
        PlaySuccess();
    }

    public void PlayFailure()
    {
        failureTween = PlayVignette(
            failureImage,
            failureTween,
            failurePeakAlpha);
    }

    public void PlaySuccess()
    {
        successTween = PlayVignette(
            successImage,
            successTween,
            successPeakAlpha);
    }

    private Tween PlayVignette(Image image, Tween activeTween, float targetAlpha)
    {
        activeTween?.Kill();

        if (image == null || targetAlpha <= 0f)
            return null;

        SetAlpha(image, 0f);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(
            image
                .DOFade(targetAlpha, fadeInDuration)
                .SetEase(Ease.OutQuad));
        sequence.Append(
            image
                .DOFade(0f, fadeOutDuration)
                .SetEase(Ease.OutCubic));
        return sequence;
    }

    private void StopVignettes()
    {
        failureTween?.Kill();
        successTween?.Kill();
        failureTween = null;
        successTween = null;

        if (failureImage != null)
            failureImage.DOKill();

        if (successImage != null)
            successImage.DOKill();

        SetAlpha(failureImage, 0f);
        SetAlpha(successImage, 0f);
    }

    private static void ConfigureImage(Image image)
    {
        if (image != null)
            image.raycastTarget = false;
    }

    private static void SetAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
