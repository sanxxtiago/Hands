using UnityEngine;

public struct HandDataSnapshot
{
    public long frameId;
    public float timestamp;

    public HandType handType;

    // POSICIÓN
    public Vector3 palmPosition;
    public Vector3 wristPosition;
    public Vector3 elbowPosition;

    // DIRECCIONES (clave biomecánica)
    public Vector3 palmNormal;
    public Vector3 handDirection;
    public Vector3 forearmDirection;

    // ROTACIONES
    public Quaternion palmRotation;
    public Quaternion forearmRotation;

    // GESTOS
    public float grabStrength;
    public float pinchStrength;
}