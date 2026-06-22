using UnityEngine;

public class ForearmRotationDetector : IMotionDetector
{
    public float threshold = 3f; //3 grados de cambio mínimo entre frames
    public float smoothing = 0.2f;

    private float _smoothedDelta;
    public bool debug = false;

    public MotionData Evaluate(HandDataSnapshot current, HandDataSnapshot previous)
    {
        float delta = 0f;

        //Detecta el cambio de rotación
        if (previous.frameId != 0)
        {
            Quaternion deltaRotation =
                Quaternion.Inverse(previous.forearmRotation) * current.forearmRotation;

            //Cálula el cambio en el ángulo
            delta = Quaternion.Angle(Quaternion.identity, deltaRotation);
        }

        //Elimina ruído
        if (delta < 0.5f)
            delta = 0f;

        _smoothedDelta = Mathf.Lerp(_smoothedDelta, delta, smoothing);

        bool isActive = _smoothedDelta > threshold;
        float normalized = Mathf.Clamp01(_smoothedDelta / 30f);

        if (debug)
            Debug.Log($"[FOREARM] {current.handType} | Delta: {_smoothedDelta:F2}° | Active: {isActive}");

        return new MotionData
        {
            zone = MotionZone.Forearm,
            //handType = current.handType,
            value = normalized,
            rawAngle = _smoothedDelta,
            isActive = isActive
        };
    }
}