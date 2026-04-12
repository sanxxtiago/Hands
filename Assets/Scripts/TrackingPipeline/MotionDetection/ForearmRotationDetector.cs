using UnityEngine;

public class ForearmRotationDetector : IMotionDetector
{
    public float threshold = 0.2f;

    public MotionData Evaluate(HandDataSnapshot current, HandDataSnapshot previous)
    {
        if (previous.frameId == 0)
            return default;

        // Eje del antebrazo
        Vector3 axis = current.forearmDirection.normalized;

        // Rotación entre frames (pronación/supinación incremental)
        float deltaAngle = Vector3.SignedAngle(
            previous.palmNormal,
            current.palmNormal,
            axis
        );

        // Acumulado opcional (si luego quieres tracking continuo)
        float absAngle = Mathf.Abs(deltaAngle);

        // Normalización (temporal, luego va con calibración)
        float normalized = Mathf.Clamp01(absAngle / 90f);

        // Velocidad angular (grados por segundo)
        float deltaTime = Time.deltaTime;
        float angularVelocity = deltaTime > 0 ? deltaAngle / deltaTime : 0f;

        return new MotionData
        {
            zone = deltaAngle > 0 
                ? MotionZone.Supination 
                : MotionZone.Pronation,

            handType = current.handType,

            value = normalized,
            rawAngle = deltaAngle,
            velocity = angularVelocity,

            isActive = normalized > threshold,

            frameId = current.frameId
        };
    }
}