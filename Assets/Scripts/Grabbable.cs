using UnityEngine;

public class Grabbable : MonoBehaviour
{
    public event System.Action<Grabbable> OnForcedRelease;

    public bool IsGrabbed { get; private set; }

    public virtual bool CanBeGrabbed()
    {
        return true;
    }

    public virtual void Grab()
    {
        IsGrabbed = true;
    }

    public virtual void Release()
    {
        IsGrabbed = false;
    }

    public virtual void ForceRelease()
    {
        if (!IsGrabbed) return;

        IsGrabbed = false;
        
        OnForcedRelease?.Invoke(this);
    }

}