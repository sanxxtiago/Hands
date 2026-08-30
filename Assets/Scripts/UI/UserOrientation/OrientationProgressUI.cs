using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class OrientationProgressUI : MonoBehaviour
{
    private const int TargetsPerRow = 3;

    [SerializeField] private OrientationPhase2Manager orientationManager;
    [SerializeField] private RectTransform targetsContainer;
    [SerializeField] private Sprite targetSprite;
    [SerializeField] private CanvasGroup containerGroup;
    [SerializeField, Min(1f)] private float targetSize = 20f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.35f;
    [SerializeField, Min(0f)] private float completionAnimationDuration = 0.35f;
    [SerializeField, Range(0f, 1f)] private float completedAlpha = 0.35f;
    [SerializeField] private Color highlightColor = new Color(1f, 0.95f, 0.55f, 1f);

    private readonly List<Image> targetImages = new List<Image>();
    private readonly List<RectTransform> rowContainers = new List<RectTransform>(2);
    private Tween fadeTween;
    private int currentTargetCount = -1;
    private int completedTargetCount;

    private void Awake()
    {
        if (targetsContainer == null)
        {
            Debug.LogError("[OrientationProgress] Falta la referencia targetsContainer.", this);
            enabled = false;
            return;
        }

        if (containerGroup == null)
            containerGroup = targetsContainer.GetComponent<CanvasGroup>();

        if (containerGroup == null)
            containerGroup = targetsContainer.gameObject.AddComponent<CanvasGroup>();

        containerGroup.blocksRaycasts = false;
        containerGroup.interactable = false;
        containerGroup.alpha = 0f;

        HorizontalLayoutGroup layout = targetsContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
            layout.enabled = false;
    }

    private void OnEnable()
    {
        if (orientationManager == null)
            orientationManager = FindFirstObjectByType<OrientationPhase2Manager>();

        if (orientationManager == null)
        {
            Debug.LogError("[OrientationProgress] No se encontro un OrientationPhase2Manager.", this);
            return;
        }

        orientationManager.OnProgressChanged += HandleProgressChanged;
    }

    private void OnDisable()
    {
        if (orientationManager != null)
            orientationManager.OnProgressChanged -= HandleProgressChanged;

        fadeTween?.Kill();
        fadeTween = null;
        ClearTargets();
        currentTargetCount = -1;
        completedTargetCount = 0;

        if (containerGroup != null)
            containerGroup.alpha = 0f;
    }

    private void HandleProgressChanged(int completedObjectives, int objectivesToComplete)
    {
        if (objectivesToComplete < 0)
            return;

        if (objectivesToComplete != currentTargetCount)
        {
            currentTargetCount = objectivesToComplete;
            RebuildTargets(objectivesToComplete);
            FadeIn();
        }

        int newCompletedCount = Mathf.Clamp(completedObjectives, 0, targetImages.Count);
        for (int i = completedTargetCount; i < newCompletedCount; i++)
            MarkCompleted(i);

        completedTargetCount = newCompletedCount;
    }

    private void RebuildTargets(int targetCount)
    {
        ClearTargets();

        int rowCount = Mathf.CeilToInt((float)targetCount / TargetsPerRow);
        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            rowContainers.Add(CreateRowContainer(rowIndex, rowCount));

        for (int i = 0; i < targetCount; i++)
        {
            int rowIndex = i / TargetsPerRow;
            targetImages.Add(CreateTargetImage(i, rowContainers[rowIndex]));
        }
    }

    private RectTransform CreateRowContainer(int rowIndex, int rowCount)
    {
        GameObject rowObject = new GameObject(
            "TargetRow_" + (rowIndex + 1),
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup));

        RectTransform rowRect = (RectTransform)rowObject.transform;
        rowRect.SetParent(targetsContainer, false);
        rowRect.anchorMin = new Vector2(0f, 0.5f);
        rowRect.anchorMax = new Vector2(1f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(0f, targetSize);

        float rowSpacing = 4f;
        float offset = rowCount > 1
            ? (targetSize + rowSpacing) * (rowCount - 1) * 0.5f
            : 0f;
        rowRect.anchoredPosition = new Vector2(
            0f,
            offset - rowIndex * (targetSize + rowSpacing));

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

    private Image CreateTargetImage(int index, RectTransform rowContainer)
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
        targetImage.sprite = targetSprite;
        targetImage.preserveAspect = true;
        targetImage.raycastTarget = false;
        return targetImage;
    }

    private void MarkCompleted(int index)
    {
        if (index < 0 || index >= targetImages.Count)
            return;

        Image targetImage = targetImages[index];
        if (targetImage == null)
            return;

        targetImage.DOKill();
        targetImage.transform.DOKill();

        Transform targetTransform = targetImage.transform;
        targetTransform.localScale = Vector3.one;
        targetImage.color = WithAlpha(targetImage.color, 1f);

        if (completionAnimationDuration <= 0f)
        {
            targetImage.color = WithAlpha(highlightColor, completedAlpha);
            return;
        }

        Sequence completionSequence = DOTween.Sequence();
        completionSequence.Join(
            targetTransform.DOPunchScale(
                new Vector3(0.08f, 0.08f, 0.08f),
                completionAnimationDuration,
                1,
                0.5f));
        completionSequence.Join(
            targetImage
                .DOColor(highlightColor, completionAnimationDuration)
                .SetEase(Ease.OutCubic));
        completionSequence.Join(
            targetImage
                .DOFade(completedAlpha, completionAnimationDuration)
                .SetEase(Ease.OutCubic));
    }

    private void FadeIn()
    {
        fadeTween?.Kill();

        if (containerGroup == null)
            return;

        containerGroup.alpha = 0f;
        if (fadeInDuration <= 0f)
        {
            containerGroup.alpha = 1f;
            return;
        }

        fadeTween = containerGroup
            .DOFade(1f, fadeInDuration)
            .SetEase(Ease.OutCubic);
    }

    private void ClearTargets()
    {
        for (int i = 0; i < targetImages.Count; i++)
        {
            if (targetImages[i] == null)
                continue;

            targetImages[i].DOKill();
            targetImages[i].transform.DOKill();
            Destroy(targetImages[i].gameObject);
        }

        targetImages.Clear();

        for (int i = rowContainers.Count - 1; i >= 0; i--)
        {
            if (rowContainers[i] != null)
                Destroy(rowContainers[i].gameObject);
        }

        rowContainers.Clear();
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a *= alpha;
        return color;
    }
}
