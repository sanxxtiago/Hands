using UnityEngine;

public class ForearmRotationDetector : IMotionDetector
{
    public float threshold = 0.25f;

    public MotionData Evaluate(HandDataSnapshot snap)
    {
        float angle = snap.forearmRotation.eulerAngles.x;

        float normalized = Mathf.Abs(angle) / 180f;

        return new MotionData
        {
            zone = MotionZone.Forearm,
            handType = snap.handType,
            value = normalized,
            isActive = normalized > threshold,
            frameId = snap.frameId
        };
    }
}