using UnityEngine;

public class ErgonomicsCalculator
{
    public float handThreshold = 0.05f;
    public float wristThreshold = 2f;      // grados
    public float forearmThreshold = 0.1f;  // velocidad mínima

    public float wristScale = 30f;
    public float forearmScale = 1.5f;

    private Quaternion lastRotation;
    private bool hasLastRotation = false;

    private Vector3 lastVelocity;
    private bool hasLastVelocity = false;

    public (float hand, float wrist, float forearm) CalculateActivity(GestureInputEventArgs e)
    {
        // =========================
        // HAND (movimiento global real)
        // =========================
        float handSpeed = e.handVelocity.magnitude;
        float handActivity = ApplyDeadzone01(handSpeed, handThreshold);

        // =========================
        // WRIST (cambio de rotación)
        // =========================
        float wristActivity = 0f;

        if (hasLastRotation)
        {
            float angle = Quaternion.Angle(lastRotation, e.handRotation);

            angle = Mathf.Max(0f, angle - wristThreshold);
            wristActivity = Mathf.Clamp01(angle / wristScale);
        }

        lastRotation = e.handRotation;
        hasLastRotation = true;

        // =========================
        // FOREARM (movimiento global filtrado)
        // =========================
        float forearmSpeed = e.handVelocity.magnitude;

        forearmSpeed = Mathf.Max(0f, forearmSpeed - forearmThreshold);
        float forearmActivity = Mathf.Clamp01(forearmSpeed / forearmScale);

        return (handActivity, wristActivity, forearmActivity);
    }

    private float ApplyDeadzone01(float value, float threshold)
    {
        if (value <= threshold) return 0f;
        return (value - threshold) / (1f - threshold);
    }
}