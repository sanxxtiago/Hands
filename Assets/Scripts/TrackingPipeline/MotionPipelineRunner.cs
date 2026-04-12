using System.Collections.Generic;
using UnityEngine;

public class MotionPipelineRunner : MonoBehaviour
{
    public LeapDataProvider leapProvider;

    private HandSnapshotBuilder snapshotBuilder;
    private MotionAggregator leftAggregator;
    private MotionAggregator rightAggregator;
    private MotionEventDispatcher dispatcher;

    void Awake()
    {
        snapshotBuilder = new HandSnapshotBuilder();

        leftAggregator = new MotionAggregator(
            HandType.LEFT,
            new List<IMotionDetector>
            {
                //new WristRotationDetector(),
                new ForearmRotationDetector(),
                // new HandPositionDetector()
            },
            new List<IGestureDetector>
            {
                //new GrabGestureDetector(),
                //new PinchGestureDetector()
            }
        );

        rightAggregator = new MotionAggregator(
            HandType.RIGHT,
            new List<IMotionDetector>
            {
                // new WristRotationDetector(),
                 new ForearmRotationDetector(),
                // new HandPositionDetector()
            },
            new List<IGestureDetector>
            {
                //new GrabGestureDetector(),
                //new PinchGestureDetector()
            }
        );

        dispatcher = new MotionEventDispatcher();
    }

    void OnEnable()
    {
        leapProvider.OnFrameReady += OnFrame;
    }

    void OnDisable()
    {
        leapProvider.OnFrameReady -= OnFrame;
    }

    void OnFrame(Leap.Frame frame)
    {
        var snapshots = snapshotBuilder.Build(frame);

        foreach (var snap in snapshots)
        {
            FrameMotionData result;

            if (snap.handType == HandType.LEFT)
                result = leftAggregator.Process(snap);
            else
                result = rightAggregator.Process(snap);

            dispatcher.Dispatch(result);
        }
    }
}