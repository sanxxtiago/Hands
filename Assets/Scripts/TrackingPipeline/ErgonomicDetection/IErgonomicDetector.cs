public interface IErgonomicDetector
{
    void Evaluate(
        HandDataSnapshot current,
        HandDataSnapshot previous,
        ref FrameErgonomicData frame);
}
