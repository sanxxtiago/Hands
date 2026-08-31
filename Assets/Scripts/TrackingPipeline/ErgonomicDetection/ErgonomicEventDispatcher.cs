public sealed class ErgonomicEventDispatcher
{
    public void Dispatch(FrameErgonomicData frame)
    {
        ErgonomicEventBus.Publish(frame);
    }
}
