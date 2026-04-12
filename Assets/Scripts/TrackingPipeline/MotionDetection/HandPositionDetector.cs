using UnityEngine;

public class HandPositionDetector : IMotionDetector
{
    public float threshold = 0.3f;

    // Valores definidos en calibración
    private float minHeight = 0f;
    private float maxHeight = 0.5f;

    // Offset base (posición neutra del usuario)
    private float referenceHeight = 0f;

    public void SetCalibration(float min, float max, float reference)
    {
        minHeight = min;
        maxHeight = max;
        referenceHeight = reference;
    }

    public MotionData Evaluate(HandDataSnapshot current, HandDataSnapshot previous)
    {
        float height = current.palmPosition.y;

        // Altura relativa al baseline
        float relativeHeight = height - referenceHeight;

        // Normalización basada en calibración real
        float normalized = Mathf.InverseLerp(minHeight, maxHeight, relativeHeight);

        // Velocidad vertical
        float velocity = 0f;
        if (previous.frameId != 0)
        {
            float deltaY = current.palmPosition.y - previous.palmPosition.y;
            float deltaTime = Time.deltaTime;
            velocity = deltaTime > 0 ? deltaY / deltaTime : 0f;
        }

        return new MotionData
        {
            zone = MotionZone.HandElevation, // 👈 más específico
            handType = current.handType,

            value = normalized,
            rawAngle = relativeHeight, // aquí es distancia real
            velocity = velocity,

            isActive = normalized > threshold,

            frameId = current.frameId
        };
    }
}