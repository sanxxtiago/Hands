using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Tira de patos de la fase DuckHunter activa. Cada pato se compone de una capa
// coloreable para el cuerpo y otra capa que conserva los colores de ojos y alas.
public sealed class DuckPhaseProgressUI : MonoBehaviour
{
    [SerializeField] private DuckSequenceRunner sequenceRunner;
    [SerializeField] private RectTransform ducksContainer;
    [Tooltip("Sprite blanco del cuerpo del pato; recibe el color de requiredHand.")]
    [SerializeField] private Sprite duckBodySprite;
    [Tooltip("Sprite transparente de ojos y alas; debe conservar sus colores originales.")]
    [SerializeField] private Sprite duckDetailsSprite;
    [Tooltip("Sprite mostrado encima del pato cuando llega al destino sin ser cazado.")]
    [SerializeField] private Sprite failureSprite;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.35f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.35f;
    [Tooltip("Duracion de la atenuacion de un pato cazado.")]
    [SerializeField, Min(0f)] private float completedFadeDuration = 0.35f;
    [Tooltip("Alpha final de un pato cazado; las capas conservan sus colores.")]
    [SerializeField, Range(0f, 1f)] private float completedAlpha = 0.35f;
    [Tooltip("Color del sprite de fallo. Usa blanco para conservar el color del sprite.")]
    [SerializeField] private Color failureColor = Color.white;
    [SerializeField, Min(0f)] private float failureFadeDuration = 0.12f;
    [Tooltip("Micro-punch al cazar un pato; en 0 lo desactiva.")]
    [SerializeField, Min(0f)] private float hitPunchDuration = 0.15f;
    [SerializeField] private Vector3 hitPunchScale = new Vector3(0.15f, 0.15f, 0.15f);
    [SerializeField, Min(1f)] private float duckSize = 24f;
    [SerializeField, Min(1)] private int ducksPerRow = 4;
    [SerializeField] private CanvasGroup containerGroup;

    private readonly List<DuckVisual> duckVisuals = new List<DuckVisual>();
    private readonly List<RectTransform> rowContainers = new List<RectTransform>(2);
    private Tween fadeTween;
    private int currentPhaseIndex = -1;

    private CanvasGroup ContainerGroup
    {
        get
        {
            if (containerGroup == null && ducksContainer != null)
            {
                containerGroup = ducksContainer.GetComponent<CanvasGroup>();
                if (containerGroup == null)
                    containerGroup = ducksContainer.gameObject.AddComponent<CanvasGroup>();
            }

            return containerGroup;
        }
    }

    private void Awake()
    {
        if (ducksContainer == null)
        {
            Debug.LogError("[DuckProgress] Falta la referencia ducksContainer.", this);
            enabled = false;
            return;
        }

        CanvasGroup group = ContainerGroup;
        if (group == null)
        {
            Debug.LogError("[DuckProgress] Falta el CanvasGroup del contenedor.", this);
            enabled = false;
            return;
        }

        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 0f;

        if (duckBodySprite == null || duckDetailsSprite == null)
        {
            Debug.LogWarning(
                "[DuckProgress] Faltan los sprites separados del cuerpo y de los detalles del pato.",
                this);
        }
    }

    private void OnEnable()
    {
        if (sequenceRunner == null)
            sequenceRunner = FindFirstObjectByType<DuckSequenceRunner>();

        if (sequenceRunner == null)
        {
            Debug.LogError("[DuckProgress] No se encontro un DuckSequenceRunner.", this);
            return;
        }

        sequenceRunner.OnPhaseCompositionChanged += HandlePhaseComposition;
        sequenceRunner.OnDuckProcessed += HandleDuckProcessed;
        ExerciseProgressManager.OnPhaseCompleted += HandlePhaseCompleted;
    }

    private void OnDisable()
    {
        if (sequenceRunner != null)
        {
            sequenceRunner.OnPhaseCompositionChanged -= HandlePhaseComposition;
            sequenceRunner.OnDuckProcessed -= HandleDuckProcessed;
        }

        ExerciseProgressManager.OnPhaseCompleted -= HandlePhaseCompleted;
        ResetState();
    }

    private void HandlePhaseComposition(int phaseIndex, DuckSequenceStep[] composition)
    {
        currentPhaseIndex = phaseIndex;
        RebuildDucks(composition);
        FadeIn();
    }

    private void HandleDuckProcessed(int phaseIndex, int stepIndex, bool wasHit)
    {
        if (phaseIndex != currentPhaseIndex ||
            stepIndex < 0 ||
            stepIndex >= duckVisuals.Count)
        {
            return;
        }

        DuckVisual duck = duckVisuals[stepIndex];
        if (duck.IsProcessed)
            return;

        if (wasHit)
            MarkHit(duck);
        else
            MarkFailed(duck);
    }

    private void HandlePhaseCompleted(int phaseIndex, int phaseCount)
    {
        if (phaseIndex != currentPhaseIndex)
            return;

        FadeOut();
    }

    private void RebuildDucks(DuckSequenceStep[] composition)
    {
        ClearDucks();

        int maxSupportedDucks = 2 * ducksPerRow; // 2 rows × 4 ducks = 8 max
        int count = composition != null ? Mathf.Min(composition.Length, maxSupportedDucks) : 0;
        for (int i = 0; i < count; i++)
        {
            int rowIndex = i / ducksPerRow;
            if (rowIndex >= rowContainers.Count)
                rowContainers.Add(CreateRowContainer(rowIndex));

            duckVisuals.Add(CreateDuckVisual(i, composition[i], rowContainers[rowIndex]));
        }

        PositionRows(rowContainers.Count);
    }

    private RectTransform CreateRowContainer(int rowIndex)
    {
        GameObject rowObject = new GameObject("DuckRow_" + (rowIndex + 1), typeof(RectTransform), typeof(HorizontalLayoutGroup));
        RectTransform rowRect = (RectTransform)rowObject.transform;
        rowRect.SetParent(ducksContainer, false);
        rowRect.anchorMin = new Vector2(0f, 0.5f);
        rowRect.anchorMax = new Vector2(1f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(0f, duckSize);

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
        float offset = rowCount > 1 ? (duckSize + rowSpacing) * 0.5f : 0f;

        for (int i = 0; i < rowCount; i++)
        {
            RectTransform row = rowContainers[i];
            row.anchoredPosition = new Vector2(0f, offset - i * (duckSize + rowSpacing));
        }
    }

    private DuckVisual CreateDuckVisual(int index, DuckSequenceStep step, RectTransform rowContainer)
    {
        GameObject duckObject = new GameObject(
            "DuckIcon_" + (index + 1),
            typeof(RectTransform),
            typeof(CanvasGroup));

        RectTransform duckRect = (RectTransform)duckObject.transform;
        duckRect.SetParent(rowContainer, false);
        duckRect.sizeDelta = new Vector2(duckSize, duckSize);

        CanvasGroup duckGroup = duckObject.GetComponent<CanvasGroup>();
        duckGroup.alpha = 1f;

        Image bodyImage = CreateLayer(
            duckRect,
            "Body",
            duckBodySprite,
            GetBaseColor(step.requiredHand));
        Image detailsImage = CreateLayer(
            duckRect,
            "Details",
            duckDetailsSprite,
            Color.white);
        Image failureImage = CreateLayer(
            duckRect,
            "FailureOverlay",
            failureSprite,
            failureColor);

        failureImage.gameObject.SetActive(false);
        return new DuckVisual(duckRect, duckGroup, bodyImage, detailsImage, failureImage);
    }

    private static Image CreateLayer(
        RectTransform parent,
        string layerName,
        Sprite sprite,
        Color color)
    {
        GameObject layerObject = new GameObject(
            layerName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        RectTransform layerRect = (RectTransform)layerObject.transform;
        layerRect.SetParent(parent, false);
        layerRect.anchorMin = Vector2.zero;
        layerRect.anchorMax = Vector2.one;
        layerRect.anchoredPosition = Vector2.zero;
        layerRect.sizeDelta = Vector2.zero;
        layerRect.localScale = Vector3.one;

        Image image = layerObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private void MarkHit(DuckVisual duck)
    {
        duck.IsProcessed = true;
        duck.FailureImage.gameObject.SetActive(false);
        KillDuckTweens(duck);

        if (hitPunchDuration > 0f)
        {
            duck.RootTransform.localScale = Vector3.one;
            duck.RootTransform.DOPunchScale(
                hitPunchScale,
                hitPunchDuration,
                1,
                1);
        }

        if (completedFadeDuration > 0f)
        {
            duck.Group
                .DOFade(completedAlpha, completedFadeDuration)
                .SetEase(Ease.OutCubic);
        }
        else
        {
            duck.Group.alpha = completedAlpha;
        }
    }

    private void MarkFailed(DuckVisual duck)
    {
        duck.IsProcessed = true;
        KillDuckTweens(duck);

        if (failureSprite == null)
            return;

        duck.FailureImage.color = failureColor;
        duck.FailureImage.gameObject.SetActive(true);

        if (failureFadeDuration > 0f)
        {
            Color transparent = failureColor;
            transparent.a = 0f;
            duck.FailureImage.color = transparent;
            duck.FailureImage
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

        ClearDucks();
    }

    private void ClearDucks()
    {
        for (int i = duckVisuals.Count - 1; i >= 0; i--)
        {
            DuckVisual duck = duckVisuals[i];
            if (duck == null || duck.RootTransform == null)
                continue;

            KillDuckTweens(duck);
            Destroy(duck.RootTransform.gameObject);
        }

        duckVisuals.Clear();

        for (int i = rowContainers.Count - 1; i >= 0; i--)
        {
            if (rowContainers[i] != null)
                Destroy(rowContainers[i].gameObject);
        }

        rowContainers.Clear();
    }

    private static void KillDuckTweens(DuckVisual duck)
    {
        if (duck.RootTransform != null)
        {
            duck.RootTransform.DOKill();
            duck.RootTransform.transform.DOKill();
        }

        if (duck.Group != null)
            duck.Group.DOKill();

        if (duck.FailureImage != null)
        {
            duck.FailureImage.DOKill();
            duck.FailureImage.transform.DOKill();
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

    private sealed class DuckVisual
    {
        public readonly RectTransform RootTransform;
        public readonly CanvasGroup Group;
        public readonly Image BodyImage;
        public readonly Image DetailsImage;
        public readonly Image FailureImage;
        public bool IsProcessed;

        public DuckVisual(
            RectTransform rootTransform,
            CanvasGroup group,
            Image bodyImage,
            Image detailsImage,
            Image failureImage)
        {
            RootTransform = rootTransform;
            Group = group;
            BodyImage = bodyImage;
            DetailsImage = detailsImage;
            FailureImage = failureImage;
        }
    }
}
