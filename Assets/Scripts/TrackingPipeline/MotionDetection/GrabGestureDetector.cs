using UnityEngine;

public class GrabGestureDetector : IGestureDetector
{
    public float threshold = 0.7f;
    public GestureState Evaluate(HandDataSnapshot snap)
    {
        float strength = snap.grabStrength;

        return new GestureState
        {
            type = GestureType.Grab,
            handType = snap.handType,
            strength = strength,
            isActive = strength > threshold,
            frameId = snap.frameId
        };
    }
}