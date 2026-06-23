using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class OrientationPieceBehaviour : Grabbable
{
    public bool isFitted;
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
        rb.isKinematic = true;
    }

    public override void OnGrabStart()//InteractableData data)
    {
        base.OnGrabStart();
        rb.useGravity = false;
        Debug.Log("GRABBING FROM ORI");
    }

    public override void OnGrabEnd()//InteractableData data)
    {
        base.OnGrabEnd();
        rb.useGravity = true;
    }
}
