using UnityEngine;

public class Grabbable : Interactable
{
    private Vector3 positionOffset;
    private Quaternion rotationOffset;
    //private Transform transform;
    public bool IsGrabbed = false;

    void Awake()
    {
        //transform = base.transform;
    }

    public override void OnGrabStart(InteractableData data)
    {
        IsGrabbed = true;
        Vector3 handPos = data.position;
        Quaternion handRot = data.rotation;
        positionOffset = transform.position - handPos;
        //rotationOffset = Quaternion.Inverse(handRot) * _transform.rotation;
    }

    public override void OnGrabUpdate(InteractableData data)
    {
        Vector3 targetPos = data.position + positionOffset;
        Quaternion targetRot = data.rotation * rotationOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            ClampPosition(targetPos),
            Time.deltaTime * 20f
        );

        // _transform.rotation = Quaternion.Slerp(
        //     _transform.rotation,
        //     targetRot,
        //     Time.deltaTime * 15f
        // );

    }
    public override void OnGrabEnd(InteractableData data)
    {
        IsGrabbed = false;
    }
}