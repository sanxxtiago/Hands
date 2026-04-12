using UnityEngine;

public class Grabbable : Interactable
{
    public bool IsGrabbed { get; private set; }
    protected Rigidbody rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (IsGrabbed) return;

        Vector3 target = ClampPosition(rb.position);
        Vector3 smooth = Vector3.Lerp(rb.position, target, Time.fixedDeltaTime * 10f);

        rb.MovePosition(smooth);
    }
    public override void OnGrabStart(HandType hand, Vector3 handPos, Quaternion handRot)
    {
        IsGrabbed = true;

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.drag = 10f;
        rb.angularDrag = 10f;
    }

    public override void OnGrabUpdate(
        HandType hand,
        Vector3 targetPos,
        Quaternion targetRot,
        float posSmooth,
        float rotSmooth
    )
    {
        if (rb == null) return;

        Vector3 velocity = (targetPos - rb.position) * posSmooth;
        rb.velocity = velocity;

        Quaternion delta = targetRot * Quaternion.Inverse(rb.rotation);
        delta.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f) angle -= 360f;

        rb.angularVelocity = angle * Mathf.Deg2Rad * rotSmooth * axis;
    }

    public override void OnGrabEnd(HandType hand, Vector3 releaseVelocity)
    {
        IsGrabbed = false;
        rb.isKinematic = false;
        rb.drag = 10f;
        rb.angularDrag = 10f;
        rb.velocity = releaseVelocity;
    }

    public override void ForceRelease()
    {
        if (!IsGrabbed) return;

        IsGrabbed = false;
        rb.isKinematic = false;

        InvokeForcedRelease();
    }
}