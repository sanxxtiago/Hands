using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Tira de mini-fases del ejercicio Insert: muestra los iconos de las piezas de la
// fase activa y las transiciona a un estado deshabilitado a medida que se encajan.
public sealed class InsertPhaseProgressUI : MonoBehaviour
{
    [SerializeField] private RectTransform piecesContainer;
    [Tooltip("Catalogo SlotType -> sprite; una entrada vacia usa el sprite de fallback.")]
    [SerializeField] private PieceIconCatalog iconCatalog;
    [Tooltip("Sprite usado cuando el catalogo no tiene icono para el tipo de pieza.")]
    [SerializeField] private Sprite fallbackSprite;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.35f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.35f;
    [Tooltip("Duracion de la transicion de color hacia el estado deshabilitado.")]
    [SerializeField, Min(0f)] private float disableFadeDuration = 0.35f;
    [Tooltip("Color al que convergen los iconos de piezas ya encajadas.")]
    [SerializeField] private Color disabledColor = new Color(0.55f, 0.58f, 0.66f, 0.35f);
    [Tooltip("Micro-punch al encajar una pieza; en 0 lo desactiva.")]
    [SerializeField, Min(0f)] private float snapPunchDuration = 0.15f;
    [SerializeField] private Vector3 snapPunchScale = new Vector3(0.15f, 0.15f, 0.15f);
    [SerializeField, Min(1f)] private float pieceSize = 50f;

    private readonly List<Image> pieceImages = new List<Image>();
    private readonly List<bool> completedFlags = new List<bool>();
    private readonly List<PieceStepDescriptor> currentComposition = new List<PieceStepDescriptor>();
    [SerializeField] private CanvasGroup containerGroup;
    private Tween fadeTween;
    private int currentPhaseIndex = -1;

    // Resolucion perezosa: permite usar la referencia del Inspector o recuperar el
    // CanvasGroup del contenedor cuando la escena aun no lo ha serializado.
    private CanvasGroup ContainerGroup
    {
        get
        {
            if (containerGroup == null && piecesContainer != null)
            {
                containerGroup = piecesContainer.GetComponent<CanvasGroup>();
                if (containerGroup == null)
                    containerGroup = piecesContainer.gameObject.AddComponent<CanvasGroup>();
            }

            return containerGroup;
        }
    }

    private void Awake()
    {
        if (piecesContainer == null)
        {
            Debug.LogError("[InsertProgress] Falta la referencia piecesContainer.", this);
            enabled = false;
            return;
        }

        CanvasGroup group = ContainerGroup;
        if (group == null)
        {
            Debug.LogError("[InsertProgress] Falta el CanvasGroup del contenedor.", this);
            enabled = false;
            return;
        }

        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 0f;
    }

    private void OnEnable()
    {
        // La composicion reinicia la UI; evitar OnExcerciseStart impide borrar el primer FadeIn.
        WallInsertExercise.OnPhaseCompositionChanged += HandlePhaseComposition;
        ExerciseProgressManager.OnStepCompleted += HandleStepCompleted;
        ExerciseProgressManager.OnPhaseCompleted += HandlePhaseCompleted;
    }

    private void OnDisable()
    {
        WallInsertExercise.OnPhaseCompositionChanged -= HandlePhaseComposition;
        ExerciseProgressManager.OnStepCompleted -= HandleStepCompleted;
        ExerciseProgressManager.OnPhaseCompleted -= HandlePhaseCompleted;

        ResetState();
    }

    private void HandlePhaseComposition(int phaseIndex, PieceStepDescriptor[] composition)
    {
        Debug.Log($"HPC {phaseIndex} + {composition.Length}");
        currentPhaseIndex = phaseIndex;
        RebuildPieces(composition);
        FadeIn();
    }

    private void HandleStepCompleted(int phaseIndex, PieceStepDescriptor descriptor)
    {
        if (phaseIndex != currentPhaseIndex)
            return;

        int index = FindPendingIndex(descriptor);
        if (index >= 0)
            MarkCompleted(index);
    }

    private void HandlePhaseCompleted(int phaseIndex, int phaseCount)
    {
        if (phaseIndex != currentPhaseIndex)
            return;

        FadeOut();
    }

    private void RebuildPieces(PieceStepDescriptor[] composition)
    {
        ClearPieces();

        int count = composition != null ? composition.Length : 0;

        for (int i = 0; i < count; i++)
        {
            PieceStepDescriptor descriptor = composition[i];
            pieceImages.Add(CreatePieceImage(i, descriptor));
            completedFlags.Add(false);
            currentComposition.Add(descriptor);
        }
    }

    private Image CreatePieceImage(int index, PieceStepDescriptor descriptor)
    {
        GameObject go = new GameObject(
            "PieceIcon_" + (index + 1),
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        Image image = go.GetComponent<Image>();
        image.sprite = iconCatalog != null ? iconCatalog.GetIcon(descriptor.PieceType) : null;
        if (image.sprite == null)
            image.sprite = fallbackSprite;
        image.color = GetBaseColor(descriptor.RequiredHand);
        image.raycastTarget = false;

        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(piecesContainer, false);
        rect.sizeDelta = new Vector2(pieceSize, pieceSize);
        return image;
    }

    private int FindPendingIndex(PieceStepDescriptor descriptor)
    {
        int firstPending = -1;

        for (int i = 0; i < pieceImages.Count; i++)
        {
            if (completedFlags[i])
                continue;

            if (firstPending < 0)
                firstPending = i;

            if (!descriptor.IsValid)
                return firstPending;

            PieceStepDescriptor target = currentComposition[i];
            if (target.PieceType == descriptor.PieceType &&
                target.RequiredHand == descriptor.RequiredHand)
                return i;
        }

        return firstPending;
    }

    private void MarkCompleted(int index)
    {
        completedFlags[index] = true;

        Image image = pieceImages[index];
        if (image == null)
            return;

        image.DOKill();
        Transform imageTransform = image.transform;
        imageTransform.DOKill();

        if (snapPunchDuration > 0f)
        {
            imageTransform.localScale = Vector3.one;
            imageTransform.DOPunchScale(snapPunchScale, snapPunchDuration, 1, 1);
        }

        if (disableFadeDuration > 0f)
            image.DOColor(disabledColor, disableFadeDuration).SetEase(Ease.OutCubic);
        else
            image.color = disabledColor;
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

        ClearPieces();
    }

    private void ClearPieces()
    {
        for (int i = 0; i < pieceImages.Count; i++)
        {
            Image image = pieceImages[i];
            if (image == null)
                continue;

            image.DOKill();
            image.transform.DOKill();
            Destroy(image.gameObject);
        }

        pieceImages.Clear();
        completedFlags.Clear();
        currentComposition.Clear();
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
}
