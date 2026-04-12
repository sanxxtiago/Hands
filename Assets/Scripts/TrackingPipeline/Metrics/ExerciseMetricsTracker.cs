using System.Collections.Generic;
using UnityEngine;

public class ExerciseMetricsTracker
{
    private readonly HandType _handType;

    private Dictionary<MotionZone, ZoneUsageRecord> _records;

    private int _totalFrames;
    private float _elapsedTime;
    private float _activeTime;

    public ExerciseMetricsTracker(HandType handType)
    {
        _handType = handType;

        _records = new Dictionary<MotionZone, ZoneUsageRecord>
        {
            { MotionZone.Hand, new ZoneUsageRecord { zone = MotionZone.Hand } },
            { MotionZone.Wrist, new ZoneUsageRecord { zone = MotionZone.Wrist } },
            { MotionZone.Forearm, new ZoneUsageRecord { zone = MotionZone.Forearm } }
        };
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

            var record = _records[motion.zone];

            record.totalFrames++;

            if (motion.isActive)
            {
                record.activeFrames++;
                anyActiveThisFrame = true;
            }

            _records[motion.zone] = record;
        }

        if (anyActiveThisFrame)
            _activeTime += dt;
    }

    public RuntimeMetrics GetRuntimeSnapshot()
    {
        float[] usage = new float[3];

        usage[0] = SafeDivide(_records[MotionZone.Hand]);
        usage[1] = SafeDivide(_records[MotionZone.Wrist]);
        usage[2] = SafeDivide(_records[MotionZone.Forearm]);

        return new RuntimeMetrics
        {
            handType = _handType,
            usageByZone = usage,
            elapsedSeconds = _elapsedTime
        };
    }
    public void Reset()
    {
        _totalFrames = 0;
        _elapsedTime = 0f;
        _activeTime = 0f;

        _records[MotionZone.Hand] = new ZoneUsageRecord { zone = MotionZone.Hand };
        _records[MotionZone.Wrist] = new ZoneUsageRecord { zone = MotionZone.Wrist };
        _records[MotionZone.Forearm] = new ZoneUsageRecord { zone = MotionZone.Forearm };
    }
    
    private float SafeDivide(ZoneUsageRecord record)
    {
        if (record.totalFrames == 0)
            return 0f;

        return (float)record.activeFrames / record.totalFrames;
    }
    public ZoneUsageRecord GetZoneRecord(MotionZone zone)
    {
        return _records[zone];
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