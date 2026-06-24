using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class OrientationPieceBehaviour : Interactable
{
    public bool isFitted;
    public bool IsGrabbed = false;
    private Rigidbody rb;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        //Clase base
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
        //Debug.Log("GRABBING FROM ORI");
        IsGrabbed = true;
    }

    public override void OnGrabEnd()
    {
        base.OnGrabEnd();
        IsGrabbed = false;
    }
}
