using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotBehaviour : MonoBehaviour
{
    private static readonly List<SlotBehaviour> activeSlots = new();
    private static readonly int baseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int legacyColorId = Shader.PropertyToID("_Color");
    private static SlotBehaviour highlightedSlot;
    private static PieceBehaviour highlightedPiece;

    public Transform snapPoint;
    public SlotType slotType;
    public float snapAngle = 20f;
    public float snapDistance = 10f;
    public float snapZOffset = -0.02f;
    public float snapSpeed = 0.2f;
    public bool isFilled;

    [Header("Feedback visual")]
    [Tooltip("Color del destello que aparece al completar el encaje.")]
    [SerializeField] private Color snapFlashColor = new(0.49f, 1f, 0.47f, 1f);

    [Tooltip("Velocidad del pulso del slot mientras la pieza esta agarrada.")]
    [SerializeField, Min(0f)] private float highlightPulseSpeed = 0.5f;

    [Tooltip("Escala maxima adicional del slot durante el resaltado.")]
    [SerializeField, Range(0f, 0.2f)] private float highlightScale = 0.06f;

    [Tooltip("Intensidad minima del color del resaltado.")]
    [SerializeField, Range(0f, 1f)] private float highlightMinIntensity = 0.45f;

    [Tooltip("Intensidad maxima del color del resaltado.")]
    [SerializeField, Range(0f, 1f)] private float highlightMaxIntensity = 0.8f;

    [Tooltip("Duracion del destello al encajar la pieza.")]
    [SerializeField, Min(0f)] private float snapFlashDuration = 0.4f;

    [Tooltip("Escala maxima adicional del destello de encaje.")]
    [SerializeField, Range(0f, 0.35f)] private float snapFlashScale = 0.18f;

    private Renderer[] visualRenderers;
    private Transform[] visualTransforms;
    private Material[][] visualMaterials;
    private Vector3[] visualBaseScales;
    private MaterialPropertyBlock visualPropertyBlock;
    private Coroutine alignAndSnapCoroutine;
    private float feedbackElapsed;
    private Color highlightColor = HandsColor.Default;
    private bool isSnapping;
    private bool isHighlighted;
    private bool isFlashing;
    private PieceBehaviour currentPiece;

    public static SlotBehaviour FindCorrespondingSlot(PieceBehaviour piece)
    {
        if (piece == null)
            return null;

        Transform phaseRoot = piece.transform.root;
        SlotBehaviour closestSlot = null;
        float closestDistance = float.MaxValue;

        for (int i = activeSlots.Count - 1; i >= 0; i--)
        {
            SlotBehaviour slot = activeSlots[i];

            if (slot == null)
            {
                activeSlots.RemoveAt(i);
                continue;
            }

            if (!slot.isActiveAndEnabled ||
                slot.isFilled ||
                slot.isSnapping ||
                slot.slotType != piece.pieceType ||
                slot.transform.root != phaseRoot)
            {
                continue;
            }

            float distance = (slot.transform.position - piece.transform.position).sqrMagnitude;

            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            closestSlot = slot;
        }

        return closestSlot;
    }

    public static void ClearHighlightedSlot()
    {
        SlotBehaviour slot = highlightedSlot;
        highlightedSlot = null;
        highlightedPiece = null;

        if (slot != null)
            slot.ClearHighlightVisual();
    }

    public static void ClearHighlightFor(PieceBehaviour piece)
    {
        if (piece == null || highlightedPiece != piece)
            return;

        ClearHighlightedSlot();
    }

    private void Awake()
    {
        visualRenderers = GetComponentsInChildren<Renderer>(true);
        visualTransforms = new Transform[visualRenderers.Length];
        visualMaterials = new Material[visualRenderers.Length][];
        visualBaseScales = new Vector3[visualRenderers.Length];
        visualPropertyBlock = new MaterialPropertyBlock();

        for (int i = 0; i < visualRenderers.Length; i++)
        {
            Renderer visualRenderer = visualRenderers[i];
            visualTransforms[i] = visualRenderer.transform;
            visualBaseScales[i] = visualRenderer.transform.localScale;
            visualMaterials[i] = visualRenderer.materials;
        }
    }

    private void OnEnable()
    {
        if (!activeSlots.Contains(this))
            activeSlots.Add(this);
    }

    private void OnDisable()
    {
        CancelAlignAndSnap();
        CleanupVisualFeedback();
        Unregister();
    }

    private void OnDestroy()
    {
        CancelAlignAndSnap();
        CleanupVisualFeedback();
        Unregister();
    }

    private void OnTriggerStay(Collider other)
    {
        if (isSnapping || isFilled)
            return;

        if (!other.TryGetComponent<PieceBehaviour>(out PieceBehaviour piece))
            return;

        if (currentPiece != null)
            return;

        if (!piece.CanSnap(slotType, transform.position, snapDistance))
            return;

        isSnapping = true;
        currentPiece = piece;

        alignAndSnapCoroutine = StartCoroutine(AlignAndSnap(piece));
    }

    private void Update()
    {
        if (!isHighlighted && !isFlashing)
            return;

        feedbackElapsed += Time.deltaTime;

        if (isFlashing)
        {
            float flashProgress = snapFlashDuration <= 0f
                ? 1f
                : Mathf.Clamp01(feedbackElapsed / snapFlashDuration);

            if (flashProgress >= 1f)
            {
                isFlashing = false;
                feedbackElapsed = 0f;
                ResetVisualFeedback();
                return;
            }

            float intensity = 1f - Mathf.SmoothStep(0f, 1f, flashProgress);
            ApplyVisualFeedback(snapFlashColor, intensity, snapFlashScale);
            return;
        }

        float pulse = highlightPulseSpeed <= 0f
            ? 1f
            : 0.5f + 0.5f * Mathf.Sin(
                feedbackElapsed * highlightPulseSpeed * Mathf.PI * 2f);
        float highlightIntensity = Mathf.Lerp(
            highlightMinIntensity,
            highlightMaxIntensity,
            pulse);

        ApplyVisualFeedback(highlightColor, highlightIntensity, highlightScale);
    }

    private IEnumerator AlignAndSnap(PieceBehaviour piece)
    {
        Vector3 startPos = piece.transform.position;
        Quaternion startRot = piece.transform.rotation;

        float t = 0f;

        Quaternion targetRot = snapPoint.rotation;

        while (t < 1f)
        {
            //  t += Time.deltaTime * snapSpeed;
            t += Time.deltaTime * snapSpeed * (1f + t);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            piece.transform.position = Vector3.Lerp(startPos, snapPoint.position, smoothT);

            //solo rota si el objeto lo necesita
            if (piece.requireRotation)
            {
                piece.transform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
            }
            else
            {
                piece.transform.rotation = Quaternion.identity;
            }

            yield return null;
        }

        //asegurar posición final exacta
        piece.transform.position = snapPoint.position;

        if (piece.requireRotation)
        {
            piece.transform.rotation = targetRot;
        }
        else
        {
            piece.transform.rotation = Quaternion.identity;
        }

        isSnapping = false;
        currentPiece = null;
        alignAndSnapCoroutine = null;
        piece.Snap();

        isFilled = true;
        ClearHighlightFor(piece);
        PlaySnapFlash();
    }

    public void HighlightFor(PieceBehaviour piece)
    {
        if (piece == null || isFilled)
            return;

        if (highlightedSlot != null && highlightedSlot != this)
            highlightedSlot.ClearHighlightVisual();

        highlightedSlot = this;
        highlightedPiece = piece;
        highlightColor = piece.PieceColor;
        isHighlighted = true;
        isFlashing = false;
        feedbackElapsed = 0f;
    }

    public void PlaySnapFlash()
    {
        if (highlightedSlot == this)
        {
            highlightedSlot = null;
            highlightedPiece = null;
        }

        isHighlighted = false;
        isFlashing = true;
        feedbackElapsed = 0f;
    }

    private void ClearHighlightVisual()
    {
        isHighlighted = false;

        if (!isFlashing)
            ResetVisualFeedback();
    }

    private void CleanupVisualFeedback()
    {
        if (highlightedSlot == this)
        {
            highlightedSlot = null;
            highlightedPiece = null;
        }

        isHighlighted = false;
        isFlashing = false;
        feedbackElapsed = 0f;
        ResetVisualFeedback();
    }

    private void Unregister()
    {
        activeSlots.Remove(this);
    }

    private void CancelAlignAndSnap()
    {
        if (alignAndSnapCoroutine != null)
        {
            StopCoroutine(alignAndSnapCoroutine);
            alignAndSnapCoroutine = null;
        }

        isSnapping = false;
        currentPiece = null;
    }

    private void ApplyVisualFeedback(Color effectColor, float intensity, float additionalScale)
    {
        for (int i = 0; i < visualRenderers.Length; i++)
        {
            Renderer visualRenderer = visualRenderers[i];
            Transform visualTransform = visualTransforms[i];

            if (visualRenderer == null || visualTransform == null)
                continue;

            visualTransform.localScale =
                visualBaseScales[i] * (1f + additionalScale * intensity);

            Material[] materials = visualMaterials[i];
            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];
                visualPropertyBlock.Clear();

                if (material != null)
                {
                    SetFeedbackColor(material, baseColorId, effectColor, intensity);
                    SetFeedbackColor(material, legacyColorId, effectColor, intensity);
                }

                visualRenderer.SetPropertyBlock(visualPropertyBlock, j);
            }
        }
    }

    private void SetFeedbackColor(
        Material material,
        int propertyId,
        Color effectColor,
        float intensity)
    {
        if (!material.HasProperty(propertyId))
            return;

        Color baseColor = material.GetColor(propertyId);
        Color feedbackColor = Color.Lerp(baseColor, effectColor, intensity);
        feedbackColor.a = baseColor.a;
        visualPropertyBlock.SetColor(propertyId, feedbackColor);
    }

    private void ResetVisualFeedback()
    {
        if (visualRenderers == null)
            return;

        for (int i = 0; i < visualRenderers.Length; i++)
        {
            Renderer visualRenderer = visualRenderers[i];
            Transform visualTransform = visualTransforms[i];

            if (visualTransform != null)
                visualTransform.localScale = visualBaseScales[i];

            if (visualRenderer == null)
                continue;

            Material[] materials = visualMaterials[i];
            for (int j = 0; j < materials.Length; j++)
                visualRenderer.SetPropertyBlock(null, j);
        }
    }
}
