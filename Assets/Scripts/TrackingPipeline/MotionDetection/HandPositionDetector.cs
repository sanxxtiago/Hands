using UnityEngine;

public class HandPositionDetector : IMotionDetector
{
    public float movementThreshold = 0.02f; // metros por frame
    public float smoothing = 0.2f;

    private float _smoothedDelta;

    public MotionData Evaluate(HandDataSnapshot current, HandDataSnapshot previous)
    {
        float delta = 0f;

        if (previous.frameId != 0)
        {
            Vector3 deltaPos = current.palmPosition - previous.palmPosition;

            delta = deltaPos.magnitude;

            //eliminar ruido del sensor
            if (delta < 0.002f)
                delta = 0f;
        }

        //suavizado
        float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime * 60f);
        _smoothedDelta = Mathf.Lerp(_smoothedDelta, delta, t);

        bool isActive = _smoothedDelta > movementThreshold;

        //normalizado para métricas
        float normalized = Mathf.Clamp01(_smoothedDelta / 0.1f);

        return new MotionData
        {
            zone = MotionZone.Hand, 
            handType = current.handType,

            value = normalized,
            rawAngle = _smoothedDelta,
            velocity = _smoothedDelta / Mathf.Max(Time.deltaTime, 0.0001f),

            isActive = isActive
        };
    }
}