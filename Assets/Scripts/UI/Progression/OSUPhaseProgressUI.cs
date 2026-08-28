using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Tira de objetivos de la fase OSU activa. Conserva el orden de la secuencia y
// diferencia visualmente objetivos fijos y objetivos con trayectoria mediante sprites configurables.
public sealed class OSUPhaseProgressUI : MonoBehaviour
{
    [SerializeField] private OSUSequenceRunner sequenceRunner;
    [SerializeField] private RectTransform targetsContainer;
    [Tooltip("Sprite usado para un objetivo fijo.")]
    [SerializeField] private Sprite fixedTargetSprite;
    [Tooltip("Sprite usado para un objetivo con trayectoria.")]
    [SerializeField] private Sprite trackingTargetSprite;
    [Tooltip("Sprite alternativo cuando falta el sprite del tipo de objetivo.")]
    [SerializeField] private Sprite fallbackSprite;
    [Tooltip("Sprite mostrado encima del objetivo cuando falla. Puede asignarse mas adelante.")]
    [SerializeField] private Sprite failureSprite;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.35f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.35f;
    [Tooltip("Duracion de la transicion de color hacia el estado completado.")]
    [SerializeField, Min(0f)] private float completedFadeDuration = 0.35f;
    [Tooltip("Color de los objetivos completados.")]
    [SerializeField] private Color completedColor = new Color(0.55f, 0.58f, 0.66f, 0.35f);
    [Tooltip("Color del sprite de fallo. El sprite puede conservar su propio color usando blanco.")]
    [SerializeField] private Color failureColor = Color.white;
    [Tooltip("Color alternativo del objetivo si no se asigno un sprite de X.")]
    [SerializeField] private Color failureFallbackColor = new Color(1f, 0.25f, 0.25f, 0.8f);
    [Tooltip("Duracion de la aparicion del indicador de fallo.")]
    [SerializeField, Min(0f)] private float failureFadeDuration = 0.12f;
    [Tooltip("Micro-punch al completar un objetivo; en 0 lo desactiva.")]
    [SerializeField, Min(0f)] private float completionPunchDuration = 0.15f;
    [SerializeField] private Vector3 completionPunchScale = new Vector3(0.15f, 0.15f, 0.15f);
    [SerializeField, Min(1f)] private float targetSize = 20f;
    [SerializeField, Min(1)] private int targetsPerRow = 4;
    [SerializeField] private CanvasGroup containerGroup;

    private readonly List<TargetVisual> targetVisuals = new List<TargetVisual>();
    private readonly List<RectTransform> rowContainers = new List<RectTransform>(2);
    private Tween fadeTween;
    private int currentPhaseIndex = -1;

    private CanvasGroup ContainerGroup
    {
        get
        {
            if (containerGroup == null && targetsContainer != null)
            {
                containerGroup = targetsContainer.GetComponent<CanvasGroup>();
                if (containerGroup == null)
                    containerGroup = targetsContainer.gameObject.AddComponent<CanvasGroup>();
            }

            return containerGroup;
        }
    }

    private void Awake()
    {
        if (targetsContainer == null)
        {
            Debug.LogError("[OSUProgress] Falta la referencia targetsContainer.", this);
            enabled = false;
            return;
        }

        CanvasGroup group = ContainerGroup;
        if (group == null)
        {
            Debug.LogError("[OSUProgress] Falta el CanvasGroup del contenedor.", this);
            enabled = false;
            return;
        }

        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 0f;
    }

    private void OnEnable()
    {
        if (sequenceRunner == null)
            sequenceRunner = FindFirstObjectByType<OSUSequenceRunner>();

        if (sequenceRunner == null)
        {
            Debug.LogError("[OSUProgress] No se encontro un OSUSequenceRunner.", this);
            return;
        }

        sequenceRunner.OnPhaseCompositionChanged += HandlePhaseComposition;
        sequenceRunner.OnTargetProcessed += HandleTargetProcessed;
        ExerciseProgressManager.OnPhaseCompleted += HandlePhaseCompleted;
    }

    private void OnDisable()
    {
        if (sequenceRunner != null)
        {
            sequenceRunner.OnPhaseCompositionChanged -= HandlePhaseComposition;
            sequenceRunner.OnTargetProcessed -= HandleTargetProcessed;
        }

        ExerciseProgressManager.OnPhaseCompleted -= HandlePhaseCompleted;
        ResetState();
    }

    private void HandlePhaseComposition(int phaseIndex, OSUStep[] composition)
    {
        currentPhaseIndex = phaseIndex;
        RebuildTargets(composition);
        FadeIn();
    }

    private void HandleTargetProcessed(int phaseIndex, int stepIndex, bool completed)
    {
        if (phaseIndex != currentPhaseIndex ||
            stepIndex < 0 ||
            stepIndex >= targetVisuals.Count)
        {
            return;
        }

        TargetVisual target = targetVisuals[stepIndex];
        if (target.IsProcessed)
            return;

        if (completed)
            MarkCompleted(target);
        else
            MarkFailed(target);
    }

    private void HandlePhaseCompleted(int phaseIndex, int phaseCount)
    {
        if (phaseIndex != currentPhaseIndex)
            return;

        FadeOut();
    }

    private void RebuildTargets(OSUStep[] composition)
    {
        ClearTargets();

        int count = composition != null ? Mathf.Min(composition.Length, 2 * targetsPerRow) : 0;
        for (int i = 0; i < count; i++)
        {
            int rowIndex = i / targetsPerRow;
            if (rowIndex >= rowContainers.Count)
                rowContainers.Add(CreateRowContainer(rowIndex));

            targetVisuals.Add(CreateTargetVisual(i, composition[i], rowContainers[rowIndex]));
        }

        PositionRows(rowContainers.Count);
    }

    private RectTransform CreateRowContainer(int rowIndex)
    {
        GameObject rowObject = new GameObject("TargetRow_" + (rowIndex + 1), typeof(RectTransform), typeof(HorizontalLayoutGroup));
        RectTransform rowRect = (RectTransform)rowObject.transform;
        rowRect.SetParent(targetsContainer, false);
        rowRect.anchorMin = new Vector2(0f, 0.5f);
        rowRect.anchorMax = new Vector2(1f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(0f, targetSize);

        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childScaleWidth = false;
        layout.childScaleHeight = false;
        return rowRect;
    }

    private void PositionRows(int rowCount)
    {
        float rowSpacing = 4f;
        float offset = rowCount > 1 ? (targetSize + rowSpacing) * 0.5f : 0f;
        for (int i = 0; i < rowCount; i++)
            rowContainers[i].anchoredPosition = new Vector2(0f, offset - i * (targetSize + rowSpacing));
    }

    private TargetVisual CreateTargetVisual(int index, OSUStep step, RectTransform rowContainer)
    {
        GameObject targetObject = new GameObject(
            "TargetIcon_" + (index + 1),
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        RectTransform targetRect = (RectTransform)targetObject.transform;
        targetRect.SetParent(rowContainer, false);
        targetRect.sizeDelta = new Vector2(targetSize, targetSize);

        Image targetImage = targetObject.GetComponent<Image>();
        targetImage.sprite = ResolveTargetSprite(step);
        targetImage.color = GetBaseColor(step != null ? step.requiredHand : HandType.NONE);
        targetImage.preserveAspect = true;
        targetImage.raycastTarget = false;

        GameObject failureObject = new GameObject(
            "FailureOverlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        RectTransform failureRect = (RectTransform)failureObject.transform;
        failureRect.SetParent(targetRect, false);
        failureRect.anchorMin = Vector2.zero;
        failureRect.anchorMax = Vector2.one;
        failureRect.anchoredPosition = Vector2.zero;
        failureRect.sizeDelta = Vector2.zero;
        failureRect.localScale = Vector3.one;

        Image failureImage = failureObject.GetComponent<Image>();
        failureImage.sprite = failureSprite;
        failureImage.color = failureColor;
        failureImage.preserveAspect = true;
        failureImage.raycastTarget = false;
        failureObject.SetActive(false);

        return new TargetVisual(targetImage, failureImage);
    }

    private Sprite ResolveTargetSprite(OSUStep step)
    {
        bool isTracking = step != null && step.IsTrackingStep;
        Sprite sprite = isTracking ? trackingTargetSprite : fixedTargetSprite;

        if (sprite == null)
            sprite = fallbackSprite;

        return sprite;
    }

    private void MarkCompleted(TargetVisual target)
    {
        target.IsProcessed = true;
        target.FailureImage.gameObject.SetActive(false);
        KillTargetTweens(target);

        Transform targetTransform = target.TargetImage.transform;
        if (completionPunchDuration > 0f)
        {
            targetTransform.localScale = Vector3.one;
            targetTransform.DOPunchScale(
                completionPunchScale,
                completionPunchDuration,
                1,
                1);
        }

        if (completedFadeDuration > 0f)
        {
            target.TargetImage
                .DOColor(completedColor, completedFadeDuration)
                .SetEase(Ease.OutCubic);
        }
        else
        {
            target.TargetImage.color = completedColor;
        }
    }

    private void MarkFailed(TargetVisual target)
    {
        target.IsProcessed = true;
        KillTargetTweens(target);

        if (failureSprite == null)
        {
            if (failureFadeDuration > 0f)
            {
                target.TargetImage
                    .DOColor(failureFallbackColor, failureFadeDuration)
                    .SetEase(Ease.OutCubic);
            }
            else
            {
                target.TargetImage.color = failureFallbackColor;
            }

            return;
        }

        target.FailureImage.color = failureColor;
        target.FailureImage.gameObject.SetActive(true);

        if (failureFadeDuration > 0f)
        {
            Color transparent = failureColor;
            transparent.a = 0f;
            target.FailureImage.color = transparent;
            target.FailureImage
                .DOColor(failureColor, failureFadeDuration)
                .SetEase(Ease.OutCubic);
        }
    }

    private void FadeIn()
    {
        fadeTween?.Kill();
        CanvasGroup group = ContainerGroup;
        if (group == null)
            return;

        group.alpha = 0f;
        if (fadeInDuration <= 0f)
        {
            group.alpha = 1f;
            fadeTween = null;
            return;
        }

        fadeTween = group
            .DOFade(1f, fadeInDuration)
            .SetEase(Ease.OutCubic);
    }

    private void FadeOut()
    {
        fadeTween?.Kill();
        CanvasGroup group = ContainerGroup;
        if (group == null)
            return;

        if (fadeOutDuration <= 0f)
        {
            group.alpha = 0f;
            fadeTween = null;
            return;
        }

        fadeTween = group
            .DOFade(0f, fadeOutDuration)
            .SetEase(Ease.OutCubic);
    }

    private void ResetState()
    {
        fadeTween?.Kill();
        fadeTween = null;
        currentPhaseIndex = -1;

        CanvasGroup group = ContainerGroup;
        if (group != null)
            group.alpha = 0f;

        ClearTargets();
    }

    private void ClearTargets()
    {
        for (int i = 0; i < targetVisuals.Count; i++)
        {
            TargetVisual target = targetVisuals[i];
            if (target == null || target.TargetImage == null)
                continue;

            KillTargetTweens(target);
            Destroy(target.TargetImage.gameObject);
        }

        targetVisuals.Clear();

        for (int i = rowContainers.Count - 1; i >= 0; i--)
        {
            if (rowContainers[i] != null)
                Destroy(rowContainers[i].gameObject);
        }

        rowContainers.Clear();
    }

    private static void KillTargetTweens(TargetVisual target)
    {
        if (target.TargetImage != null)
        {
            target.TargetImage.DOKill();
            target.TargetImage.transform.DOKill();
        }

        if (target.FailureImage != null)
        {
            target.FailureImage.DOKill();
            target.FailureImage.transform.DOKill();
        }
    }

    private static Color GetBaseColor(HandType requiredHand)
    {
        switch (requiredHand)
        {
            case HandType.LEFT:
                return HandsColor.Left;
            case HandType.RIGHT:
                return HandsColor.Right;
            default:
                return HandsColor.Default;
        }
    }

    private sealed class TargetVisual
    {
        public readonly Image TargetImage;
        public readonly Image FailureImage;
        public bool IsProcessed;

        public TargetVisual(Image targetImage, Image failureImage)
        {
            TargetImage = targetImage;
            FailureImage = failureImage;
        }
    }
}
