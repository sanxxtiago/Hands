using UnityEngine;

public struct InteractionEvent
{
    public GestureType type;
    public GesturePhase phase;
    public HandType handType;
    public float strength;
    public long frameId;
    public Vector3 palmPosition;
    public Quaternion palmRotation;
    public Vector3 velocity;
}