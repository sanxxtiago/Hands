using UnityEngine;

public struct HandDataSnapshot
{
    public long frameId;
    public HandType handType;

    public Vector3 palmPosition;
    public Quaternion palmRotation;
    public Quaternion forearmRotation;

    public float grabStrength;
    public float pinchStrength;
}
