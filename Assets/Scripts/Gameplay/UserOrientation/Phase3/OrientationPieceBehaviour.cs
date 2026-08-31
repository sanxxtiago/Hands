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
        rb.useGravity = false;
        rb.detectCollisions = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    public void DisableInteractionCollider()
    {
        if (pieceCollider != null)
            pieceCollider.enabled = false;
    }

    // La pieza reposa sobre la base cuando su movimiento residual es despreciable.
    // Se exige este estado antes de considerarla encajada para que la física la
    // asiente por gravedad (caída + descanso) y no se capture en pleno vuelo.
    public bool IsAtRest(float linearThreshold = 0.05f, float angularThreshold = 0.05f)
    {
        if (rb.isKinematic)
            return true;

        return rb.velocity.sqrMagnitude < linearThreshold * linearThreshold
            && rb.angularVelocity.sqrMagnitude < angularThreshold * angularThreshold;
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

        if (!isFitted)
        {
            // La pieza hereda la velocidad de la mano al soltarla; se limpia el
            // impulso lateral para que solo se deje caer (gravedad) y no "salga a volar".
            rb.velocity = new Vector3(0f, Mathf.Min(rb.velocity.y, 0f), 0f);
            rb.angularVelocity = Vector3.zero;
        }

        OnReleased?.Invoke();
    }
}
