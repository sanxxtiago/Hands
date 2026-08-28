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

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        transform.position = ClampPosition(transform.position);
    }

    public override bool CanInteract(InteractionType interactionType)
    {
        return !isFitted;
    }

    public void FitIn()
    {
        isFitted = true;
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
