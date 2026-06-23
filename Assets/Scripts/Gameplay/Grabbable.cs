using UnityEngine;

public class Grabbable : Interactable
{
    public bool IsGrabbed = false;

    public override void OnGrabStart()
    {
        IsGrabbed = true;
    }

    public override void OnGrabEnd()
    {
        IsGrabbed = false;
    }
}