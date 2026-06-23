using System.Collections.Generic;
using UnityEngine;
public struct FrameMotionData
{
    public long frameId;
    public HandType handType;
    public Vector3 handPos;
    public Quaternion handRotation;
    public float timestamp;

    public List<MotionData> motions;
    public List<GestureStateData> gestures;
}