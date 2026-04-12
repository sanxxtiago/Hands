using System.Collections.Generic;
using UnityEngine;

public class ExerciseMetricsTracker
{
    private readonly HandType _handType;

    private Dictionary<MotionZone, ZoneUsageRecord> _records =
        new Dictionary<MotionZone, ZoneUsageRecord>();

    private int _totalFrames;
    private float _elapsedTime;
    private float _activeTime;

    public ExerciseMetricsTracker(HandType handType)
    {
        _handType = handType;
    }

    public void OnFrameReceived(FrameMotionData frame)
    {
        float dt = Time.deltaTime;

        _totalFrames++;
        _elapsedTime += dt;

        bool anyActiveThisFrame = false;

        foreach (var motion in frame.motions)
        {
            if (motion.handType != _handType)
                continue;

            // Crear zona si no existe (dinámico)
            if (!_records.ContainsKey(motion.zone))
            {
                _records[motion.zone] = new ZoneUsageRecord
                {
                    zone = motion.zone
                };
            }

            var record = _records[motion.zone];

            record.totalFrames++;
            record.accumulatedValue += motion.value; // 🔥 intensidad acumulada

            if (motion.isActive)
            {
                record.activeFrames++;
                record.activeTime += dt;
                anyActiveThisFrame = true;
            }

            _records[motion.zone] = record;
        }

        if (anyActiveThisFrame)
            _activeTime += dt;
    }

    public RuntimeMetrics GetRuntimeSnapshot()
    {
        return new RuntimeMetrics
        {
            handType = _handType,
            elapsedSeconds = _elapsedTime
        };
    }

    public void Reset()
    {
        _totalFrames = 0;
        _elapsedTime = 0f;
        _activeTime = 0f;

        _records.Clear();
    }

    public ZoneUsageRecord GetZoneRecord(MotionZone zone)
    {
        if (_records.ContainsKey(zone))
            return _records[zone];

        return new ZoneUsageRecord { zone = zone };
    }

    public IEnumerable<MotionZone> GetTrackedZones()
    {
        return _records.Keys;
    }

    public float GetActiveTime()
    {
        return _activeTime;
    }

    public float GetActivityRatio()
    {
        if (_elapsedTime <= 0f) return 0f;
        return _activeTime / _elapsedTime;
    }
}