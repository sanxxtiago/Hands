using UnityEngine;

public class WristRotationDetector : IMotionDetector
{
    public float threshold = 0.2f;

    public MotionData Evaluate(HandDataSnapshot snap)
    {
        float angle = snap.palmRotation.eulerAngles.z;

        float normalized = Mathf.Abs(angle) / 180f;

        return new MotionData
        {
            zone = MotionZone.Wrist,
            handType = snap.handType,
            value = normalized,
            isActive = normalized > threshold,
            frameId = snap.frameId
        };
    }
}