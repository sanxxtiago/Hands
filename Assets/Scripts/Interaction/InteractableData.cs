using UnityEngine;

public struct InteractableData
{
    public Vector3 position;
    public Quaternion rotation;

    public InteractableData(Vector3 pos, Quaternion quat)
    {
        position = pos;
        rotation = quat;
    }
}