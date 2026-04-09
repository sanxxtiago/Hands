using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceBehaviour : Grabbable
{
    public bool isSnapped = false;
    public bool IsGrabbedNow;

    void Update()
    {
        IsGrabbedNow = IsGrabbed;
    }
     public override bool CanBeGrabbed()
    {
        return !isSnapped;
    }
}
