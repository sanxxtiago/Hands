using UnityEngine;

public sealed class WristPostureDetector : IErgonomicDetector
{
    private const float MinimumVectorSqrMagnitude = 0.000001f;

    public void Evaluate(
        HandDataSnapshot current,
        HandDataSnapshot previous,
        ref FrameErgonomicData frame)
    {
        if (!frame.isForearmPoseValid ||
            !IsValidRotation(current.palmRotation))
        {
            return;
        }

        frame.isWristRelativeRotationAvailable = true;
        frame.palmRelativeToForearm =
            Quaternion.Inverse(current.forearmRotation) * current.palmRotation;
        frame.isWristRelativeRotationValid =
            IsValidRotation(frame.palmRelativeToForearm);

        if (!frame.isWristRelativeRotationValid ||
            !IsValidVector(current.handDirection) ||
            !IsValidVector(current.palmNormal))
        {
            return;
        }

        Vector3 radialAxis = Vector3.Cross(
            current.palmNormal.normalized,
            current.handDirection.normalized);

        frame.wristFlexionExtension = CalculateProjectedSignedAngle(
            frame.forearmAxis,
            current.handDirection,
            radialAxis);

        frame.wristRadialUlnarDeviation = CalculateProjectedSignedAngle(
            frame.forearmAxis,
            current.handDirection,
            current.palmNormal);

        Vector3 forearmReference = current.forearmRotation * Vector3.up;
        frame.wristPronationSupination = CalculateProjectedSignedAngle(
            forearmReference,
            current.palmNormal,
            frame.forearmAxis);
    }

    private static ErgonomicAngleData CalculateProjectedSignedAngle(
        Vector3 from,
        Vector3 to,
        Vector3 axis)
    {
        if (!IsValidVector(from) || !IsValidVector(to) || !IsValidVector(axis))
            return default;

        Vector3 normalizedAxis = axis.normalized;
        Vector3 projectedFrom = Vector3.ProjectOnPlane(from, normalizedAxis);
        Vector3 projectedTo = Vector3.ProjectOnPlane(to, normalizedAxis);

        if (!IsValidVector(projectedFrom) || !IsValidVector(projectedTo))
        {
            return new ErgonomicAngleData
            {
                isAvailable = true,
                isValid = false
            };
        }

        return new ErgonomicAngleData
        {
            degrees = Vector3.SignedAngle(
                projectedFrom,
                projectedTo,
                normalizedAxis),
            isAvailable = true,
            isValid = true
        };
    }

    private static bool IsValidVector(Vector3 value)
    {
        return IsFinite(value) && value.sqrMagnitude > MinimumVectorSqrMagnitude;
    }

    private static bool IsValidRotation(Quaternion value)
    {
        return IsFinite(value) &&
            value.x * value.x + value.y * value.y +
            value.z * value.z + value.w * value.w > MinimumVectorSqrMagnitude;
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
