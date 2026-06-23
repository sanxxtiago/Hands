using System.Collections.Generic;
using UnityEngine;

public class MotionAggregator
{
    private readonly HandType _handType;

    private readonly List<IMotionDetector> _motionDetectors;
    private readonly List<IGestureDetector> _gestureDetectors;

    private HandDataSnapshot _previousSnapshot;

    public MotionAggregator(
        HandType handType,
        List<IMotionDetector> motionDetectors,
        List<IGestureDetector> gestureDetectors)
    {
        _handType = handType;

        _motionDetectors = motionDetectors ?? new List<IMotionDetector>();
        _gestureDetectors = gestureDetectors ?? new List<IGestureDetector>();
    }

    public bool Process(HandDataSnapshot current, out FrameMotionData frame)
    {
        frame = default;

        // 1. Filtrar por mano
        if (current.handType != _handType)
            return false;

        // 2. Evitar reprocesar mismo frame
        if (_previousSnapshot.frameId == current.frameId)
            return false;

        // 3. Primer frame → no hay delta válido
        if (_previousSnapshot.frameId == 0)
        {
            _previousSnapshot = current;
            return false;
        }

        var motions = new List<MotionData>(_motionDetectors.Count);
        var gestures = new List<GestureStateData>(_gestureDetectors.Count);

        // 4. Motion detectors (continuo)
        foreach (var detector in _motionDetectors)
        {
            motions.Add(detector.Evaluate(current, _previousSnapshot));
        }

        // 5. Gesture detectors (discreto)
        foreach (var detector in _gestureDetectors)
        {
            gestures.Add(detector.Evaluate(current));
        }

        // 6. Guardar snapshot actual
        _previousSnapshot = current;

        // 7. Construir frame
        frame = new FrameMotionData
        {
            frameId = current.frameId,
            handType = current.handType,
            handPos = current.palmPosition,
            handRotation = current.palmRotation,
            timestamp = Time.time,
            motions = motions,
            gestures = gestures
        };

        return true;
    }
}