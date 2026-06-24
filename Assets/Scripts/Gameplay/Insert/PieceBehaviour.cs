using System;
using UnityEngine;
public enum PieceState
{
    Idle,
    Snapped,
    Grabbed
}
public class PieceBehaviour : Interactable
{
    public static event Action OnPieceSnapped;
    public SlotType pieceType;
    public PieceState state = PieceState.Idle;

    public bool requireRotation = false;
    [HideInInspector] public Rigidbody rb;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
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

        foreach(Transform child in gameObject.GetComponentInChildren<Transform>())
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
        OnPieceSnapped?.Invoke();
    }

}
