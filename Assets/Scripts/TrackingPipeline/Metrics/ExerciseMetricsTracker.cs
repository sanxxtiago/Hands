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

    // Ventana deslizante para feedback en vivo. El acumulado de arriba queda
    // intacto: alimenta resultados finales, scores y persistencia.
    private readonly float _windowSeconds;
    private readonly Queue<WindowSample> _window = new Queue<WindowSample>();
    private int _windowFrames;
    private int _windowHandFrames;
    private int _windowWristFrames;
    private int _windowForearmFrames;
    private float _windowActiveTime;
    private float _windowTime;

    public int TotalFrames => _totalFrames;
    public HandType HandType => _handType;
    public float ElapsedTime => _elapsedTime;
    public ExerciseMetricsTracker(HandType handType, float windowSeconds = 5f)
    {
        _handType = handType;
        _windowSeconds = Mathf.Max(0.5f, windowSeconds);
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

        PushWindowSample(activatedThisFrame, anyActiveThisFrame, dt, frame.timestamp);
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
        _window.Clear();
        _windowFrames = 0;
        _windowHandFrames = 0;
        _windowWristFrames = 0;
        _windowForearmFrames = 0;
        _windowActiveTime = 0f;
        _windowTime = 0f;
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

    // Reparto por zona solo con los frames dentro de la ventana reciente.
    // Misma semántica que GetRuntimeSnapshot: fracción de frames activos.
    public RuntimeMetrics GetWindowedSnapshot()
    {
        var usageByZone = new Dictionary<MotionZone, float>();

        if (_windowFrames > 0)
        {
            usageByZone[MotionZone.Hand] = (float)_windowHandFrames / _windowFrames;
            usageByZone[MotionZone.Wrist] = (float)_windowWristFrames / _windowFrames;
            usageByZone[MotionZone.Forearm] = (float)_windowForearmFrames / _windowFrames;
        }

        return new RuntimeMetrics
        {
            handType = _handType,
            usageByZone = usageByZone
        };
    }

    // Actividad reciente: tiempo activo / tiempo real contenido en la ventana.
    // Ventana vacía o sin tiempo → 0 (los consumidores ya manejan el cero).
    public float GetWindowedActivityRatio()
    {
        if (_windowTime <= 0.0001f) return 0f;
        return Mathf.Max(0f, _windowActiveTime) / _windowTime;
    }

    private void PushWindowSample(
        HashSet<MotionZone> activatedThisFrame, bool anyActiveThisFrame, float dt, float timestamp)
    {
        int mask = ZoneMask(activatedThisFrame);

        _window.Enqueue(new WindowSample
        {
            zoneMask = mask,
            anyActive = anyActiveThisFrame,
            dt = dt,
            timestamp = timestamp
        });
        _windowFrames++;
        _windowTime += dt;
        if (anyActiveThisFrame)
            _windowActiveTime += dt;
        if ((mask & WindowSample.HandBit) != 0)
            _windowHandFrames++;
        if ((mask & WindowSample.WristBit) != 0)
            _windowWristFrames++;
        if ((mask & WindowSample.ForearmBit) != 0)
            _windowForearmFrames++;

        while (_window.Count > 0 && timestamp - _window.Peek().timestamp > _windowSeconds)
        {
            WindowSample old = _window.Dequeue();
            _windowFrames--;
            _windowTime -= old.dt;
            if (old.anyActive)
                _windowActiveTime -= old.dt;
            if ((old.zoneMask & WindowSample.HandBit) != 0)
                _windowHandFrames--;
            if ((old.zoneMask & WindowSample.WristBit) != 0)
                _windowWristFrames--;
            if ((old.zoneMask & WindowSample.ForearmBit) != 0)
                _windowForearmFrames--;
        }
    }

    private static int ZoneMask(HashSet<MotionZone> activatedThisFrame)
    {
        int mask = 0;
        foreach (MotionZone zone in activatedThisFrame)
        {
            switch (zone)
            {
                case MotionZone.Hand: mask |= WindowSample.HandBit; break;
                case MotionZone.Wrist: mask |= WindowSample.WristBit; break;
                case MotionZone.Forearm: mask |= WindowSample.ForearmBit; break;
            }
        }
        return mask;
    }

    private struct WindowSample
    {
        public const int HandBit = 1;
        public const int WristBit = 2;
        public const int ForearmBit = 4;

        public int zoneMask;
        public bool anyActive;
        public float dt;
        public float timestamp;
    }

}