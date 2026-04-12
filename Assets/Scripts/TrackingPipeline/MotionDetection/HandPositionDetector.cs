using UnityEngine;

public class HandPositionDetector : IMotionDetector
{
    public float threshold = 0.3f;

    public MotionData Evaluate(HandDataSnapshot snap)
    {
        float height = snap.palmPosition.y;

        float normalized = Mathf.Clamp01(height / 0.5f);

        return new MotionData
        {
            zone = MotionZone.Hand,
            handType = snap.handType,
            value = normalized,
            isActive = normalized > threshold,
            frameId = snap.frameId
        };
    }
}