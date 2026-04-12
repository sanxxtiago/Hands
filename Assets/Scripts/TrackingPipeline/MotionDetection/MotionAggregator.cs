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

    public FrameMotionData Process(HandDataSnapshot current)
    {
        if (_previousSnapshot.frameId == current.frameId)
            return default;
            
        // ⚠️ 1. Filtrar por mano
        if (current.handType != _handType)
            return default;

        var motions = new List<MotionData>(_motionDetectors.Count);
        var gestures = new List<GestureState>(_gestureDetectors.Count);

        // ⚙️ 2. MOTION DETECTION (con contexto temporal)
        foreach (var detector in _motionDetectors)
        {
            MotionData data = detector.Evaluate(current, _previousSnapshot);
            motions.Add(data);
        }

        // ✋ 3. GESTURES (no necesitan previous por ahora)
        foreach (var detector in _gestureDetectors)
        {
            GestureState state = detector.Evaluate(current);
            gestures.Add(state);
        }

        // 🧠 4. Guardar estado para siguiente frame
        _previousSnapshot = current;

        // 📦 5. Output
        return new FrameMotionData
        {
            frameId = current.frameId,
            handType = current.handType,
            timestamp = Time.time,
            motions = motions,
            gestures = gestures
        };
    }
}