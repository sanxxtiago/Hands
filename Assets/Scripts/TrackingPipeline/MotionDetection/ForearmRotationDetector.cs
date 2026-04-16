using UnityEngine;

public class ForearmRotationDetector : IMotionDetector
{
    public float threshold = 3f;
    public float smoothing = 0.2f;

    private float _smoothedDelta;
    public bool debug = false;

    public MotionData Evaluate(HandDataSnapshot current, HandDataSnapshot previous)
    {
        float delta = 0f;

        if (previous.frameId != 0)
        {
            Quaternion deltaRotation =
                Quaternion.Inverse(previous.forearmRotation) * current.forearmRotation;

            delta = Quaternion.Angle(Quaternion.identity, deltaRotation);
        }
        if (delta < 0.5f)
            delta = 0f;

        _smoothedDelta = Mathf.Lerp(_smoothedDelta, delta, smoothing);

        bool isActive = _smoothedDelta > threshold;
        float normalized = Mathf.Clamp01(_smoothedDelta / 30f);

#if UNITY_EDITOR
        if (debug)
            Debug.Log($"[FOREARM] {current.handType} | Delta: {_smoothedDelta:F2}° | Active: {isActive}");
#endif

        return new MotionData
        {
            zone = MotionZone.Forearm,
            handType = current.handType,
            value = normalized,
            rawAngle = _smoothedDelta,
            isActive = isActive,
            frameId = current.frameId
        };
    }
}
