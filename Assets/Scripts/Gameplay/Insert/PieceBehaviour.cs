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

    public bool requireRotation = false;
    [HideInInspector] public Rigidbody rb;
    private IgnorePhysicalHands ignoreHands;
    [SerializeField] private Renderer pieceRenderer;
    void OnEnable()
    {
        CountdownUI.OnCountdownFinished += SetPieceChirality;
    }
    void OnDisable()
    {
        CountdownUI.OnCountdownFinished -= SetPieceChirality;
    }

    void Awake()
    {

        rb = GetComponent<Rigidbody>();
        ignoreHands = GetComponent<IgnorePhysicalHands>();
        if (pieceRenderer == null)
            pieceRenderer = GetComponent<Renderer>();

        switch (requiredHand)
        {
            case HandType.NONE:
                SetPieceColor(HandsColor.Default);
                break;
            case HandType.LEFT:
                SetPieceColor(HandsColor.Left);
                break;
            case HandType.RIGHT:
                SetPieceColor(HandsColor.Right);
                break;
        }

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

    public override void OnGrabStart()
    {
        base.OnGrabStart();
        state = PieceState.Grabbed;
    }

    public override void OnGrabEnd()
    {
        base.OnGrabEnd();
        state = PieceState.Idle;
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
        OnPieceSnapped?.Invoke(this);
    }

    private void SetPieceColor(Color color)
    {
        pieceRenderer.material.color = color;
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
}
