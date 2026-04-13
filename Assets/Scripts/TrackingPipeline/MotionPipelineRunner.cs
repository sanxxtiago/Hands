using System.Collections.Generic;
using UnityEngine;

public class MotionPipelineRunner : MonoBehaviour
{
    public LeapDataProvider leapProvider;

    private HandSnapshotBuilder _snapshotBuilder;
    private MotionAggregator _leftAggregator;
    private MotionAggregator _rightAggregator;
    private MotionEventDispatcher _dispatcher;

    private HandDataSnapshot _prevLeft;
    private HandDataSnapshot _prevRight;

    void Awake()
    {
        _snapshotBuilder = new HandSnapshotBuilder();

        _leftAggregator = new MotionAggregator(
            HandType.LEFT,
            new List<IMotionDetector>
            {
            new WristRotationDetector(),
            new ForearmRotationDetector(),
            new HandPositionDetector()
            },
            new List<IGestureDetector>
            {
            new GrabGestureDetector(),
            new PinchGestureDetector()
            }
        );

        _rightAggregator = new MotionAggregator(
            HandType.RIGHT,
            new List<IMotionDetector>
            {
            new WristRotationDetector(),
            new ForearmRotationDetector(),
            new HandPositionDetector()
            },
            new List<IGestureDetector>
            {
            new GrabGestureDetector(),
            new PinchGestureDetector()
            }
        );

        _dispatcher = new MotionEventDispatcher();
    }

    void OnEnable() => leapProvider.OnFrameReady += OnFrame;
    void OnDisable() => leapProvider.OnFrameReady -= OnFrame;

    void OnFrame(Leap.Frame frame)
    {
        HandDataSnapshot?[] snapshots = _snapshotBuilder.Build(frame);

        foreach (var snapNullable in snapshots)
        {
            if (snapNullable == null) continue;

            HandDataSnapshot snap = snapNullable.Value;

            MotionAggregator aggregator = snap.handType == HandType.LEFT
                ? _leftAggregator
                : _rightAggregator;

            if (aggregator.Process(snap, out FrameMotionData result))
                _dispatcher.Dispatch(result);
        }
    }
}