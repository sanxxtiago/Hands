using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class OrientationPieceBehaviour : Interactable
{
    public event Action OnGrabbed;
    public event Action OnReleased;

    public bool isFitted;
    public bool IsGrabbed { get; private set; }
    private Rigidbody rb;
    private Collider pieceCollider;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pieceCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (isFitted)
            return;

        transform.position = ClampPosition(transform.position);
    }

    public override bool CanInteract(InteractionType interactionType)
    {
        return !isFitted;
    }

    public void FitIn()
    {
        if (isFitted)
            return;

        isFitted = true;
        IsGrabbed = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    public void DisableInteractionCollider()
    {
        if (pieceCollider != null)
            pieceCollider.enabled = false;
    }

    public override void OnGrabStart()
    {
        base.OnGrabStart();
        IsGrabbed = true;
        OnGrabbed?.Invoke();
    }

    public override void OnGrabEnd()
    {
        base.OnGrabEnd();
        IsGrabbed = false;
        OnReleased?.Invoke();
    }
}
