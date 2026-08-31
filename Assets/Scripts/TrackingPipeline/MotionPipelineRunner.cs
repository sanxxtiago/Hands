using System.Collections.Generic;
using UnityEngine;

public class MotionPipelineRunner : MonoBehaviour
{
    public LeapDataProvider leapProvider;

    private HandSnapshotBuilder _snapshotBuilder;
    private MotionAggregator _leftAggregator;
    private MotionAggregator _rightAggregator;
    private ErgonomicAggregator _leftErgonomicAggregator;
    private ErgonomicAggregator _rightErgonomicAggregator;
    private MotionEventDispatcher _dispatcher;
    private ErgonomicEventDispatcher _ergonomicDispatcher;

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

        _leftErgonomicAggregator = CreateErgonomicAggregator(HandType.LEFT);
        _rightErgonomicAggregator = CreateErgonomicAggregator(HandType.RIGHT);

        _dispatcher = new MotionEventDispatcher();
        _ergonomicDispatcher = new ErgonomicEventDispatcher();
    }

    void OnEnable() => leapProvider.OnFrameReady += OnFrame;
    void OnDisable() => leapProvider.OnFrameReady -= OnFrame;

    void OnFrame(Leap.Frame frame)
    {
        HandDataSnapshot?[] snapshots = _snapshotBuilder.Build(frame);

        foreach (var snapNullable in snapshots)
        {
            if (snapNullable == null) continue;

            ProcessSnapshot(snapNullable.Value);
        }
    }

    private static ErgonomicAggregator CreateErgonomicAggregator(HandType handType)
    {
        return new ErgonomicAggregator(
            handType,
            new List<IErgonomicDetector>
            {
                new ForearmPostureDetector(),
                new WristPostureDetector()
            });
    }

    private void ProcessSnapshot(HandDataSnapshot snapshot)
    {
        MotionAggregator motionAggregator = snapshot.handType == HandType.LEFT
            ? _leftAggregator
            : _rightAggregator;

        ErgonomicAggregator ergonomicAggregator = snapshot.handType == HandType.LEFT
            ? _leftErgonomicAggregator
            : _rightErgonomicAggregator;

        bool hasMotionData = motionAggregator.Process(
            snapshot,
            out FrameMotionData motionData);
        bool hasErgonomicData = ergonomicAggregator.Process(
            snapshot,
            out FrameErgonomicData ergonomicData);

        if (hasMotionData != hasErgonomicData)
        {
            Debug.LogError(
                $"[MotionPipelineRunner] Desincronizacion entre MotionData y " +
                $"ErgonomicData para {snapshot.handType} en frame {snapshot.frameId}.");
            return;
        }

        if (!hasMotionData)
            return;

        if (!AreSynchronized(motionData, ergonomicData))
        {
            Debug.LogError(
                $"[MotionPipelineRunner] Los datos no coinciden para " +
                $"{snapshot.handType} en frame {snapshot.frameId}.");
            return;
        }

        _dispatcher.Dispatch(motionData);
        _ergonomicDispatcher.Dispatch(ergonomicData);
    }

    private static bool AreSynchronized(
        FrameMotionData motionData,
        FrameErgonomicData ergonomicData)
    {
        return motionData.frameId == ergonomicData.frameId &&
            motionData.handType == ergonomicData.handType &&
            motionData.timestamp == ergonomicData.timestamp;
    }
}
