using UnityEngine;
public class WristRotationDetector : IMotionDetector
{
    public float threshold = 2f;   // grados de cambio mínimo entre frames
    public float smoothing = 0.2f;

    private float _smoothedDelta;
    private bool debug = true;
    public MotionData Evaluate(HandDataSnapshot current, HandDataSnapshot previous)
    {
        float delta = 0f;

        if (previous.frameId != 0)
        {
            Quaternion prevRelative =
                Quaternion.Inverse(previous.forearmRotation) * previous.palmRotation;

            Quaternion currRelative =
                Quaternion.Inverse(current.forearmRotation) * current.palmRotation;

            // Cambio de orientación entre el frame anterior y el actual
            Quaternion deltaRotation = Quaternion.Inverse(prevRelative) * currRelative;

            delta = Quaternion.Angle(Quaternion.identity, deltaRotation);
        }

        _smoothedDelta = Mathf.Lerp(_smoothedDelta, delta, smoothing);

        bool isActive = _smoothedDelta > threshold;

        float normalized = Mathf.Clamp01(_smoothedDelta / 90f);

        if (debug)
            Debug.Log($"[WRIST] {current.handType} | Delta: {_smoothedDelta:F2}° | Active: {isActive}");

        return new MotionData
        {
            zone = MotionZone.Wrist,
            handType = current.handType,
            value = normalized,
            rawAngle = _smoothedDelta,
            isActive = isActive,
            frameId = current.frameId
        };
    }
}