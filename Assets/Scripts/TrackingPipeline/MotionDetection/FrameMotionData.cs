using System.Collections.Generic;

public struct FrameMotionData
{
    public long frameId;
    public HandType handType;
    public float timestamp;

    public List<MotionData> motions;
    public List<GestureState> gestures;
}