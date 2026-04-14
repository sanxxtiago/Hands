using UnityEngine;

public class PieceBehaviour : Grabbable
{
    public INSERTTYPE pieceType;
    public bool isSnapped;
    public bool requireRotation = false;
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
        return !isSnapped;
    }

    public void Snap()
    {
        isSnapped = true;
        rb.isKinematic = true;
    }

    public override void OnGrabStart(InteractableData data)
    {
        //
    }


}
