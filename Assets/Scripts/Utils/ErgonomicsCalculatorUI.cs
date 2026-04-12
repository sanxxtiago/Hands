using UnityEngine;
public static class ErgonomicsCalculatorUI
{
    public static (float hand, float wrist, float forearm) Calculate(
        GestureInputEventArgs e,
        ref Quaternion lastRotation,
        ref bool hasRotation
    )
    {
        float handSpeed = e.handVelocity.magnitude;
        float hand = Mathf.Clamp01(handSpeed);

        float wrist = 0f;

        if (hasRotation)
        {
            float angle = Quaternion.Angle(lastRotation, e.handRotation);
            wrist = Mathf.Clamp01(angle / 30f);
        }

        lastRotation = e.handRotation;
        hasRotation = true;

        float forearm = Mathf.Clamp01(e.handVelocity.magnitude);

        return (hand, wrist, forearm);
    }
}