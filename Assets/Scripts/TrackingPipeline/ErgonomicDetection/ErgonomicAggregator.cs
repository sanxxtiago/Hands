using System.Collections.Generic;

public sealed class ErgonomicAggregator
{
    private readonly HandType _handType;
    private readonly List<IErgonomicDetector> _detectors;

    private HandDataSnapshot _previousSnapshot;

    public ErgonomicAggregator(
        HandType handType,
        List<IErgonomicDetector> detectors)
    {
        _handType = handType;
        _detectors = detectors ?? new List<IErgonomicDetector>();
    }

    public bool Process(HandDataSnapshot current, out FrameErgonomicData frame)
    {
        frame = default;

        if (current.handType != _handType)
            return false;

        if (_previousSnapshot.frameId == current.frameId)
            return false;

        if (_previousSnapshot.frameId == 0)
        {
            _previousSnapshot = current;
            return false;
        }

        frame = new FrameErgonomicData
        {
            frameId = current.frameId,
            timestamp = current.timestamp,
            handType = current.handType
        };

        foreach (IErgonomicDetector detector in _detectors)
        {
            detector.Evaluate(current, _previousSnapshot, ref frame);
        }

        _previousSnapshot = current;
        return true;
    }
}
