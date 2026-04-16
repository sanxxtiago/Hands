using UnityEngine;

public struct GameplayHandData{

    public GameplayHandData(Vector3 pos, Quaternion rot, Vector3 vel)
    {
        position = pos;
        rotation = rot;
        velocity = vel;
    }
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
}