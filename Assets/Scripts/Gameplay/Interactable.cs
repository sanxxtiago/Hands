using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public event System.Action<Interactable> OnForcedRelease;

    public virtual bool CanInteract(HAND hand) => true;

    public virtual void OnGrabStart(HAND hand, Vector3 handPos, Quaternion handRot) { }

    public virtual void OnGrabUpdate(
        HAND hand,
        Vector3 targetPos,
        Quaternion targetRot,
        float posSmooth,
        float rotSmooth
    )
    { }

    public virtual void OnGrabEnd(HAND hand, Vector3 releaseVelocity) { }

    public virtual void ForceRelease() { }

    protected Vector3 ClampPosition(Vector3 worldPosition)
    {
        return BoundingBox.Instance.ClampInsideBox(worldPosition);
    }

    protected void InvokeForcedRelease()
    {
        OnForcedRelease?.Invoke(this);
    }
}
