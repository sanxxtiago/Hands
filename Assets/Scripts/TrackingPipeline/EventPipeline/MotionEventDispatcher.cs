public class MotionEventDispatcher
{
    public void Dispatch(FrameMotionData frame)
    {
        MotionEventBus.Publish(frame);
    }
}