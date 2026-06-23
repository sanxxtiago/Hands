using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public event System.Action<Interactable> OnForcedRelease;

    // =========================
    // CAPABILITIES
    // =========================
    public virtual bool CanInteract(InteractionType interactionType) => true;

    // =========================
    // GRAB
    // =========================
    public virtual void OnGrabStart() { }
    public virtual void OnGrabEnd() { }

    // =========================
    // ROTATE
    // =========================
    public virtual void OnRotate(InteractableData data){ }

    // =========================
    // PINCH / SELECT
    // =========================
    public virtual void OnSelect(InteractableData data) { }

    // =========================
    // FORCE RELEASE
    // =========================
    public virtual void ForceRelease()
    {
        OnForcedRelease?.Invoke(this);
    }

    // =========================
    // UTILS
    // =========================
    protected Vector3 ClampPosition(Vector3 worldPosition)
    {
        return BoundingBox.Instance.ClampInsideBox(worldPosition);
    }
}