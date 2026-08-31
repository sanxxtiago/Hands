using UnityEngine;

public sealed class ForearmPostureDetector : IErgonomicDetector
{
    private const float MinimumAxisSqrMagnitude = 0.000001f;

    public void Evaluate(
        HandDataSnapshot current,
        HandDataSnapshot previous,
        ref FrameErgonomicData frame)
    {
        frame.elbowPosition = current.elbowPosition;
        frame.wristPosition = current.wristPosition;
        frame.forearmRotation = current.forearmRotation;

        Vector3 forearmAxis = current.forearmDirection;
        if (!IsValidVector(forearmAxis))
            forearmAxis = current.wristPosition - current.elbowPosition;

        frame.isForearmPoseAvailable = IsFinite(current.elbowPosition) &&
            IsFinite(current.wristPosition) &&
            IsFinite(current.forearmRotation);

        if (!frame.isForearmPoseAvailable ||
            !IsValidVector(forearmAxis) ||
            !IsValidRotation(current.forearmRotation))
        {
            return;
        }

        frame.forearmAxis = forearmAxis.normalized;
        frame.isForearmPoseValid = true;
    }

    private static bool IsValidVector(Vector3 value)
    {
        return IsFinite(value) && value.sqrMagnitude > MinimumAxisSqrMagnitude;
    }

    private static bool IsValidRotation(Quaternion value)
    {
        return IsFinite(value) &&
            value.x * value.x + value.y * value.y +
            value.z * value.z + value.w * value.w > MinimumAxisSqrMagnitude;
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    private static bool IsFinite(Quaternion value)
    {
        return IsFinite(value.x) && IsFinite(value.y) &&
            IsFinite(value.z) && IsFinite(value.w);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
