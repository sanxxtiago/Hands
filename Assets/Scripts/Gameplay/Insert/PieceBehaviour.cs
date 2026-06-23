using UnityEngine;

public class PieceBehaviour : Grabbable
{
    public INSERTTYPE pieceType;
    public bool isSnapped;
    public bool requireRotation = false;
    [HideInInspector] public Rigidbody rb;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        if (isSnapped)
            return;

        transform.position = ClampPosition(transform.position);
    }
    public override bool CanInteract(InteractionType interactionType)
    {
        return !isSnapped;
    }

    public override void OnGrabStart()
    {
        base.OnGrabStart();
    }

    public override void OnGrabEnd()
    {
        base.OnGrabEnd();
        //rb.useGravity = true;
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

}
