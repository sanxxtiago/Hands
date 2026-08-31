using System;

public static class ErgonomicEventBus
{
    public static event Action<FrameErgonomicData> OnFrame;

    public static void Publish(FrameErgonomicData frame)
    {
        OnFrame?.Invoke(frame);
    }
}
