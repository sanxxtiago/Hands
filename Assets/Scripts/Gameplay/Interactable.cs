using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public event System.Action<Interactable> OnForcedRelease;

    private Collider[] clampColliders;

    // =========================
    // CAPABILITIES
    // =========================
    public virtual bool CanInteract(InteractionType interactionType) => true;
    public virtual bool CanInteract(InteractionType interactionType, HandType handType) =>
        CanInteract(interactionType);

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
        if (BoundingBox.Instance == null)
            return worldPosition;

        clampColliders ??= GetComponentsInChildren<Collider>();

        return BoundingBox.Instance.ClampInsideBox(
            worldPosition,
            transform,
            clampColliders);
    }
}
