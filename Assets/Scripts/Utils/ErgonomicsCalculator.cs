using UnityEngine;
public class ErgonomicsCalculator
{
    public float handThreshold = 0.1f;
    public float wristThreshold = 0.5f;
    public float forearmThreshold = 0.05f;

    public float wristScale = 10f;
    public float forearmScale = 1.0f;

    private Quaternion lastRotation;
    private bool hasLastRotation = false;

    public (float hand, float wrist, float forearm) Calculate(GestureInputEventArgs e)
    {
        // =========================
        // HAND
        // =========================
        float rawHand = Mathf.Max(e.grabStrenght, e.pinchStrenght);
        float handActivity = ApplyDeadzone01(rawHand, handThreshold);

        // =========================
        // WRIST
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
        // FOREARM
        // =========================
        float speed = e.handVelocity.magnitude;
        speed = Mathf.Max(0f, speed - forearmThreshold);
        float forearmActivity = Mathf.Clamp01(speed / forearmScale);

        return (handActivity, wristActivity, forearmActivity);
    }

    private float ApplyDeadzone01(float value, float threshold)
    {
        if (value <= threshold) return 0f;
        return (value - threshold) / (1f - threshold);
    }
}