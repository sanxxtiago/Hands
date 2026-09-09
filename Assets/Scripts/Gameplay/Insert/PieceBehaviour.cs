using System;
using System.Collections.Generic;
using DG.Tweening;
using Leap;
using Leap.PhysicalHands;
using UnityEngine;
public enum PieceState
{
    Idle,
    Snapped,
    Grabbed
}
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(IgnorePhysicalHands))]

//[RequireComponent(typeof(Renderer))]
public class PieceBehaviour : Interactable
{
    public static event Action<PieceBehaviour> OnPieceSnapped;
    public static event Action<PieceBehaviour> OnPieceGrabbed;
    public HandType requiredHand = HandType.NONE;
    public SlotType pieceType;
    public PieceState state = PieceState.Idle;
    private int scorePhaseIndex = -1;

    public bool requireRotation = false;
    public Color PieceColor => GetRequiredHandColor();
    public int ScorePhaseIndex => scorePhaseIndex;
    [HideInInspector] public Rigidbody rb;
    private IgnorePhysicalHands ignoreHands;
    [SerializeField] private Renderer pieceRenderer;

    [Header("Feedback de agarre")]
    [Tooltip("Incremento proporcional de la escala mientras la pieza esta agarrada.")]
    [SerializeField, Range(0f, 0.25f)] private float grabScaleIncrease = 0.1f;

    [Tooltip("Duracion del tween de escala al agarrar o soltar la pieza.")]
    [SerializeField, Min(0f)] private float grabScaleDuration = 0.12f;

    [Tooltip("Color del destello de emision al agarrar la pieza.")]
    [SerializeField] private Color grabGlowColor = new(0.85f, 0.93f, 1f, 1f);

    [Tooltip("Duracion del decaimiento del destello de emision al agarrar.")]
    [SerializeField, Min(0f)] private float grabGlowDuration = 0.25f;

    private Vector3 baseScale;
    private Material pieceMaterial;
    private Color baseEmission;
    private static readonly int emissionColorId = Shader.PropertyToID("_EmissionColor");
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private static readonly List<PieceBehaviour> activePieces = new();
    private static int lastResetFrame = -1;
    void OnEnable()
    {
        CountdownUI.OnCountdownFinished += SetPieceChirality;

        if (!activePieces.Contains(this))
            activePieces.Add(this);
    }
    void OnDisable()
    {
        SlotBehaviour.ClearHighlightFor(this);
        CountdownUI.OnCountdownFinished -= SetPieceChirality;
        activePieces.Remove(this);

        CancelGrabAnchorAdjustment();
        transform.DOKill();
        transform.localScale = baseScale;

        if (pieceMaterial != null)
        {
            pieceMaterial.DOKill();
            if (pieceMaterial.HasProperty(emissionColorId))
                pieceMaterial.SetColor(emissionColorId, baseEmission);
        }
    }

    void Awake()
    {
        baseScale = transform.localScale;

        // La fase se instancia tal cual viene en el prefab y nadie recoloca
        // las piezas despues, asi que la pose de Awake es la inicial valida.
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        int grabbableLayer = LayerMask.NameToLayer("GrabbableLayer");
        if (grabbableLayer >= 0 && gameObject.layer != grabbableLayer)
        {
            Debug.LogWarning(
                $"[Insert] La pieza '{name}' no esta en la capa 'GrabbableLayer'; " +
                "el sistema de interaccion no detectara el agarre.",
                this);
        }

        rb = GetComponent<Rigidbody>();
        ignoreHands = GetComponent<IgnorePhysicalHands>();
        if (pieceRenderer == null)
            pieceRenderer = GetComponent<Renderer>();

        if (pieceRenderer != null)
        {
            pieceMaterial = pieceRenderer.material;
            baseEmission = pieceMaterial.HasProperty(emissionColorId)
                ? pieceMaterial.GetColor(emissionColorId)
                : Color.black;
        }

        SetPieceColor(PieceColor);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
            TryResetUnsnappedPieces();

        if (state == PieceState.Snapped)
            return;

        transform.position = ClampPosition(transform.position);
    }
    public override bool CanInteract(InteractionType interactionType)
    {
        return state != PieceState.Snapped;
    }

    public override bool CanInteract(InteractionType interactionType, HandType handType)
    {
        if (!CanInteract(interactionType))
            return false;

        if (interactionType != InteractionType.Grab || requiredHand == HandType.NONE)
            return true;

        return requiredHand == handType;
    }

    public override void OnGrabStart(InteractableData handData)
    {
        if (state == PieceState.Snapped)
        {
            SlotBehaviour.ClearHighlightFor(this);
            return;
        }

        base.OnGrabStart(handData);

        state = PieceState.Grabbed;
        OnPieceGrabbed?.Invoke(this);

        transform.DOKill();
        if (grabScaleIncrease > 0f && grabScaleDuration > 0f)
        {
            transform
                .DOScale(baseScale * (1f + grabScaleIncrease), grabScaleDuration)
                .SetEase(Ease.OutQuad);
        }

        PlayGrabGlow();

        SlotBehaviour.ClearHighlightedSlot();
        SlotBehaviour correspondingSlot =
            SlotBehaviour.FindCorrespondingSlot(this);

        if (correspondingSlot == null)
        {
            Debug.LogWarning(
                $"[Insert] No se encontro un slot correspondiente para la pieza " +
                $"'{name}' de tipo '{pieceType}'.");
            return;
        }

        correspondingSlot.HighlightFor(this);
    }

    public override void OnGrabEnd()
    {
        base.OnGrabEnd();
        state = PieceState.Idle;
        SlotBehaviour.ClearHighlightFor(this);

        transform.DOKill();
        if (grabScaleDuration > 0f)
        {
            transform
                .DOScale(baseScale, grabScaleDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => transform.localScale = baseScale);
        }
        else
        {
            transform.localScale = baseScale;
        }
    }

    // Reinicio rapido de depuracion: devuelve a su pose inicial las piezas
    // que aun no se han encajado. La guarda de frame evita que cada pieza
    // repita el mismo reinicio en el frame en que se pulsa la tecla.
    private static void TryResetUnsnappedPieces()
    {
        if (Time.frameCount == lastResetFrame)
            return;

        lastResetFrame = Time.frameCount;

        int resetCount = 0;
        for (int i = activePieces.Count - 1; i >= 0; i--)
        {
            PieceBehaviour piece = activePieces[i];

            if (piece == null)
            {
                activePieces.RemoveAt(i);
                continue;
            }

            if (piece.ResetToInitialPose())
                resetCount++;
        }

        Debug.Log(
            $"[Insert] Reinicio rapido con '0': {resetCount} piezas " +
            "devueltas a su posicion inicial.");
    }

    private bool ResetToInitialPose()
    {
        if (state == PieceState.Snapped)
            return false;

        // Si esta agarrada se libera primero para que el InteractionManager
        // suelte su referencia y restaure escala y resaltado por el camino normal.
        ForceRelease();
        CancelGrabAnchorAdjustment();

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(initialPosition, initialRotation);
        transform.localScale = baseScale;

        return true;
    }

    public void LockPhysics()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.detectCollisions = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    public void UpdateLayer()
    {
        LayerMask newLayer = LayerMask.GetMask("Default");
        gameObject.layer = newLayer;

        foreach (Transform child in gameObject.GetComponentInChildren<Transform>())
        {
            child.gameObject.layer = newLayer;
        }

    }

    public bool CanSnap(SlotType slotType, Vector3 slotPos, float snapDistance)
    {

        if (state == PieceState.Grabbed || state == PieceState.Snapped)
            return false;
        if (pieceType != slotType)
            return false;

        float dist = Vector3.Distance(transform.position, slotPos);

        return dist < snapDistance;
    }

    public void Snap()
    {
        state = PieceState.Snapped;
        LockPhysics();
        UpdateLayer();
        SlotBehaviour.ClearHighlightFor(this);
        OnPieceSnapped?.Invoke(this);
    }

    private void PlayGrabGlow()
    {
        if (pieceMaterial == null || !pieceMaterial.HasProperty(emissionColorId))
            return;

        if (grabGlowDuration <= 0f)
            return;

        pieceMaterial.EnableKeyword("_EMISSION");
        pieceMaterial.DOKill();
        pieceMaterial.SetColor(emissionColorId, grabGlowColor);
        pieceMaterial
            .DOColor(baseEmission, emissionColorId, grabGlowDuration)
            .SetEase(Ease.OutQuad);
    }

    private void SetPieceColor(Color color)
    {
        if (pieceMaterial != null)
            pieceMaterial.color = color;
    }

    private Color GetRequiredHandColor()
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

    private void SetPieceChirality()
    {
        if (ignoreHands == null)
            ignoreHands = GetComponent<IgnorePhysicalHands>();

        if (ignoreHands == null)
        {
            Debug.LogError(
                $"[Insert] La pieza '{name}' requiere el componente " +
                "IgnorePhysicalHands para configurar la mano permitida.",
                this);
            return;
        }

        switch (requiredHand)
        {
            case HandType.NONE:
                ignoreHands.DisableAllGrabbing = false;
                ignoreHands.DisableAllHandCollisions = false;
                break;
            case HandType.LEFT:
                ignoreHands.DisableAllGrabbing = true;
                ignoreHands.HandToIgnoreGrabs = ChiralitySelection.RIGHT;
                ignoreHands.HandToIgnoreCollisions = ChiralitySelection.RIGHT;
                break;
            case HandType.RIGHT:
                ignoreHands.DisableAllGrabbing = true;
                ignoreHands.HandToIgnoreGrabs = ChiralitySelection.LEFT;
                ignoreHands.HandToIgnoreCollisions = ChiralitySelection.LEFT;
                break;
        }
    }

    public void ApplyChirality()
    {
        SetPieceChirality();
    }

    public void SetScorePhaseIndex(int phaseIndex)
    {
        scorePhaseIndex = phaseIndex;
    }
}
