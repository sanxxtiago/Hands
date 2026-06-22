using System.Collections.Generic;
using UnityEngine;

public class ExerciseMetricsTracker
{

    private Dictionary<MotionZone, ZoneUsageRecord> _records =
        new Dictionary<MotionZone, ZoneUsageRecord>();
    private readonly HandType _handType;

    private int _totalFrames;
    private float _elapsedTime;
    private float _activeTime;
    public int TotalFrames => _totalFrames;
    public HandType HandType => _handType;
    public float ElapsedTime => _elapsedTime;
    public ExerciseMetricsTracker(HandType handType)
    {
        _handType = handType;
    }
    private float lastTimestamp = -1f;
    public void OnFrameReceived(FrameMotionData frame)
    {
        //float dt = Time.deltaTime;
        //float dt = Time.time - frame.timestamp;
        float dt = 0f;

        if (lastTimestamp >= 0f)
            dt = frame.timestamp - lastTimestamp;

        lastTimestamp = frame.timestamp;

        _totalFrames++;
        _elapsedTime += dt;

        bool anyActiveThisFrame = false;
        var activatedThisFrame = new HashSet<MotionZone>();
        var intensityThisFrame = new Dictionary<MotionZone, float>();

        // 1. Recoger intensidad máxima por zona desde motions
        foreach (var motion in frame.motions)
        {
            if(frame.handType != _handType) continue;

            if (!intensityThisFrame.ContainsKey(motion.zone))
                intensityThisFrame[motion.zone] = 0f;

            intensityThisFrame[motion.zone] = Mathf.Max(
                intensityThisFrame[motion.zone], motion.value);

            if (motion.isActive)
                activatedThisFrame.Add(motion.zone);
        }

        // 2. Recoger intensidad máxima de Hand desde gestures
        foreach (var gesture in frame.gestures)
        {
            if (frame.handType != _handType) continue;

            if (!intensityThisFrame.ContainsKey(MotionZone.Hand))
                intensityThisFrame[MotionZone.Hand] = 0f;

            intensityThisFrame[MotionZone.Hand] = Mathf.Max(
                intensityThisFrame[MotionZone.Hand], gesture.strength);

            if (gesture.isActive)
                activatedThisFrame.Add(MotionZone.Hand);
        }

        // 3. Aplicar al record una sola vez por zona
        foreach (var kvp in intensityThisFrame)
        {
            if (!_records.ContainsKey(kvp.Key))
                _records[kvp.Key] = new ZoneUsageRecord { zone = kvp.Key };

            var record = _records[kvp.Key];
            record.accumulatedValue += kvp.Value;

            if (activatedThisFrame.Contains(kvp.Key))
            {
                record.activeFrames++;
                record.activeTime += dt;
                anyActiveThisFrame = true;
            }

            _records[kvp.Key] = record;
        }

        if (anyActiveThisFrame)
            _activeTime += dt;
    }


    //Para las manos en UI 
    public RuntimeMetrics GetRuntimeSnapshot()
    {
        var usageByZone = new Dictionary<MotionZone, float>();

        foreach (var kvp in _records)
        {
            usageByZone[kvp.Key] = _totalFrames > 0
                ? (float)kvp.Value.activeFrames / _totalFrames
                : 0f;
        }

        return new RuntimeMetrics
        {
            handType = _handType,
            usageByZone = usageByZone
        };
    }

    public void Reset()
    {
        _totalFrames = 0;
        _elapsedTime = 0f;
        _activeTime = 0f;
        lastTimestamp = -1f;
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

    public float GetActivityRatio(float elapsedTime)
    {
        if (_elapsedTime <= 0f) return 0f;
        //Debug.Log($"Elapsed: {Mathf.Max(exerciseDuration,_elapsedTime)} | Active: {_activeTime}");
        return _activeTime / elapsedTime;
        //return _activeTime / Mathf.Max(exerciseDuration,_elapsedTime);

    }

}