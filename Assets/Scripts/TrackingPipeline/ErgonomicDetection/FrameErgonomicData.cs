using UnityEngine;

public struct FrameErgonomicData
{
    public long frameId;
    public float timestamp;
    public HandType handType;

    public Vector3 elbowPosition;
    public Vector3 wristPosition;
    public Vector3 forearmAxis;
    public Quaternion forearmRotation;
    public Quaternion palmRelativeToForearm;

    public bool isForearmPoseAvailable;
    public bool isForearmPoseValid;
    public bool isWristRelativeRotationAvailable;
    public bool isWristRelativeRotationValid;

    public ErgonomicAngleData wristFlexionExtension;
    public ErgonomicAngleData wristRadialUlnarDeviation;
    public ErgonomicAngleData wristPronationSupination;
}
