using System;
using UnityEngine;

public class GestureInputEventArgs : EventArgs
{
    public HandType hand;
    public Vector3 handPosition;
    public Quaternion handRotation;
    public Vector3 handVelocity;
    public float grabStrenght;
    public float pinchStrenght;

    public GestureInputEventArgs(HandType h, Vector3 pos, Quaternion rot, Vector3 vel, float grabS, float pinchS)
    {
        hand = h;
        handPosition = pos;
        handRotation = rot;
        handVelocity = vel;
        grabStrenght = grabS;
        pinchStrenght = pinchS;
    }

}
