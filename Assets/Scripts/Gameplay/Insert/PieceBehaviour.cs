using System;
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

[RequireComponent(typeof(Renderer))]
public class PieceBehaviour : Interactable
{
    public static event Action<PieceBehaviour> OnPieceSnapped;
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
    void OnEnable()
    {
        CountdownUI.OnCountdownFinished += SetPieceChirality;
    }
    void OnDisable()
    {
        SlotBehaviour.ClearHighlightFor(this);
        CountdownUI.OnCountdownFinished -= SetPieceChirality;
    }

    void Awake()
    {

        rb = GetComponent<Rigidbody>();
        ignoreHands = GetComponent<IgnorePhysicalHands>();
        if (pieceRenderer == null)
            pieceRenderer = GetComponent<Renderer>();

        SetPieceColor(PieceColor);

    }
    void Update()
    {
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

    public override void OnGrabStart()
    {
        base.OnGrabStart();

        if (state == PieceState.Snapped)
        {
            SlotBehaviour.ClearHighlightFor(this);
            return;
        }

        state = PieceState.Grabbed;

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

    private void SetPieceColor(Color color)
    {
        pieceRenderer.material.color = color;
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
