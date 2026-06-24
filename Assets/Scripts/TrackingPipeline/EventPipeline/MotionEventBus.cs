using System;
using UnityEngine;

public static class MotionEventBus
{
    public static event Action<FrameMotionData> OnFrame;

    public static void Publish(FrameMotionData frame)
    {
        //Debug.Log($"FROM MEB: {frame.handType}");
        OnFrame?.Invoke(frame);
    }
}