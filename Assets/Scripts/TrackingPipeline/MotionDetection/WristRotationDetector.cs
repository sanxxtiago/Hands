using UnityEngine;

public class WristRotationDetector : IMotionDetector
{
    public float threshold = 0.2f;

    public MotionData Evaluate(HandDataSnapshot current, HandDataSnapshot previous)
    {
        float angle = Vector3.Angle(
            current.palmNormal,
            current.forearmDirection
        );

        float normalized = Mathf.Clamp01(angle / 90f);

        return new MotionData
        {
            zone = MotionZone.WristFlexion,
            handType = current.handType,
            value = normalized,
            isActive = normalized > threshold,
            frameId = current.frameId
        };
    }
}