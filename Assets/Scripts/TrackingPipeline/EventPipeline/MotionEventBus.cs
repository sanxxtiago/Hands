using System;

public static class MotionEventBus
{
    public static event Action<FrameMotionData> OnFrame;

    public static void Publish(FrameMotionData frame)
    {
        OnFrame?.Invoke(frame);
    }
}