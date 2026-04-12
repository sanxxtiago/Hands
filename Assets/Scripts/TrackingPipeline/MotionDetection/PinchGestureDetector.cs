using UnityEngine;

public class PinchGestureDetector : IGestureDetector
{
    public float threshold = 0.6f;

    public GestureState Evaluate(HandDataSnapshot snap)
    {
        float strength = snap.pinchStrength;

        return new GestureState
        {
            type = GestureType.Pinch,
            handType = snap.handType,
            strength = strength,
            isActive = strength > threshold,
            frameId = snap.frameId
        };
    }
}
