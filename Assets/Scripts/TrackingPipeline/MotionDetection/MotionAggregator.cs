using System.Collections.Generic;

public class MotionAggregator
{
    private readonly HandType _handType;

    private readonly List<IMotionDetector> _motionDetectors = new List<IMotionDetector>();
    private readonly List<IGestureDetector> _gestureDetectors = new List<IGestureDetector>();

    public MotionAggregator(
     HandType handType,
     List<IMotionDetector> motionDetectors,
     List<IGestureDetector> gestureDetectors)
    {
        _handType = handType;

        _motionDetectors = motionDetectors ?? new List<IMotionDetector>();
        _gestureDetectors = gestureDetectors ?? new List<IGestureDetector>();
    }

    public FrameMotionData Process(HandDataSnapshot snap)
    {
        var motions = new List<MotionData>(3);
        var gestureStates = new List<GestureState>(2);

        // 1. MOTION DETECTION (continuo)
        foreach (var detector in _motionDetectors)
        {
            MotionData data = detector.Evaluate(snap);
            motions.Add(data);
        }

        // 2. GESTURE DETECTION (estado continuo)
        foreach (var detector in _gestureDetectors)
        {
            GestureState state = detector.Evaluate(snap);
            gestureStates.Add(state);
        }

        // 3. BUILD FRAME OUTPUT
        return new FrameMotionData
        {
            frameId = snap.frameId,
            handType = snap.handType,
            timestamp = UnityEngine.Time.time,
            motions = motions,
            gestures = gestureStates
        };
    }
}