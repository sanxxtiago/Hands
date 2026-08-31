using System;

/// <summary>Participación de las señales publicadas; no estima carga ni esfuerzo articular.</summary>
public sealed class UsageInterpreter
{
    private const int Capacity = 2048;
    private readonly HandType handType;
    private readonly float windowSeconds;
    private readonly float maximumGapSeconds;
    private readonly Interval[] intervals = new Interval[Capacity];
    private int head;
    private int count;
    private bool hasPrevious;
    private bool previousValid;
    private long lastFrameId;
    private float lastTimestamp;
    private double observed;
    private double handActive;
    private double wristActive;
    private double forearmActive;
    private double wristSignal;
    private double forearmSignal;

    public UsageInterpreter(HandType handType, float windowSeconds, float maximumGapSeconds)
    {
        if ((handType != HandType.LEFT && handType != HandType.RIGHT) ||
            !Finite(windowSeconds) || windowSeconds <= 0f ||
            !Finite(maximumGapSeconds) || maximumGapSeconds <= 0f)
            throw new ArgumentException("Configuración de participación inválida.");

        this.handType = handType;
        this.windowSeconds = windowSeconds;
        this.maximumGapSeconds = maximumGapSeconds;
    }

    public void Reset()
    {
        ClearWindow();
        hasPrevious = false;
        previousValid = false;
        lastFrameId = 0;
        lastTimestamp = 0f;
    }

    public bool TryProcess(FrameMotionData frame, out FrameUsageData result)
    {
        result = default;
        if (frame.handType != handType)
            return false;

        if (!Finite(frame.timestamp) || frame.frameId <= 0)
        {
            BreakContinuity();
            return false;
        }

        // No retroceder la marca temporal: un frame viejo nunca crea tiempo ficticio.
        if (hasPrevious && (frame.frameId <= lastFrameId || frame.timestamp <= lastTimestamp))
        {
            BreakContinuity();
            return false;
        }

        bool valid = TryRead(frame, out Interval sample);
        float dt = hasPrevious ? frame.timestamp - lastTimestamp : 0f;
        if (valid && previousValid && dt > 0f && dt <= maximumGapSeconds)
        {
            sample.end = frame.timestamp;
            sample.duration = dt;
            Append(sample);
            Trim(frame.timestamp - windowSeconds);
        }
        else
        {
            ClearWindow();
        }

        hasPrevious = true;
        previousValid = valid;
        lastFrameId = frame.frameId;
        lastTimestamp = frame.timestamp;
        double totalSignal = wristSignal + forearmSignal;
        result = new FrameUsageData
        {
            frameId = frame.frameId,
            timestamp = frame.timestamp,
            handType = handType,
            isValid = valid && observed > 0.000001d,
            observedSeconds = (float)observed,
            handActivityRatio = observed > 0d ? (float)(handActive / observed) : 0f,
            wristActivityRatio = observed > 0d ? (float)(wristActive / observed) : 0f,
            forearmActivityRatio = observed > 0d ? (float)(forearmActive / observed) : 0f,
            meanRotationSignal = observed > 0d ? (float)(totalSignal / observed) : 0f,
            wristContribution = totalSignal > 0.000001d ? (float)(wristSignal / totalSignal) : 0f,
            forearmContribution = totalSignal > 0.000001d ? (float)(forearmSignal / totalSignal) : 0f
        };
        return true;
    }

    private static bool TryRead(FrameMotionData frame, out Interval sample)
    {
        sample = default;
        if (frame.motions == null)
            return false;

        int mask = 0;
        for (int i = 0; i < frame.motions.Count; i++)
        {
            MotionData motion = frame.motions[i];
            int bit;
            switch (motion.zone)
            {
                case MotionZone.Hand: bit = 1; break;
                case MotionZone.Wrist: bit = 2; break;
                case MotionZone.Forearm: bit = 4; break;
                default: continue;
            }
            if ((mask & bit) != 0 || !Finite(motion.value) || motion.value < 0f || motion.value > 1f)
                return false;
            mask |= bit;
            if (bit == 1) sample.hand = motion.isActive ? 1f : 0f;
            if (bit == 2)
            {
                sample.wrist = motion.isActive ? 1f : 0f;
                sample.wristValue = motion.value;
            }
            if (bit == 4)
            {
                sample.forearm = motion.isActive ? 1f : 0f;
                sample.forearmValue = motion.value;
            }
        }
        return mask == 7;
    }

    private void Append(Interval sample)
    {
        if (count == Capacity)
            RemoveFirst();
        intervals[(head + count) % Capacity] = sample;
        count++;
        Accumulate(sample, sample.duration);
    }

    private void Trim(float cutoff)
    {
        while (count > 0 && intervals[head].end <= cutoff)
            RemoveFirst();
        if (count == 0)
            return;
        Interval first = intervals[head];
        float removed = cutoff - (first.end - first.duration);
        if (removed <= 0f)
            return;
        Accumulate(first, -removed);
        first.duration -= removed;
        intervals[head] = first;
    }

    private void RemoveFirst()
    {
        Accumulate(intervals[head], -intervals[head].duration);
        head = (head + 1) % Capacity;
        count--;
    }

    private void Accumulate(Interval sample, double dt)
    {
        observed += dt;
        handActive += sample.hand * dt;
        wristActive += sample.wrist * dt;
        forearmActive += sample.forearm * dt;
        wristSignal += sample.wristValue * dt;
        forearmSignal += sample.forearmValue * dt;
    }

    private void BreakContinuity()
    {
        ClearWindow();
        previousValid = false;
    }

    private void ClearWindow()
    {
        head = count = 0;
        observed = handActive = wristActive = forearmActive = wristSignal = forearmSignal = 0d;
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private struct Interval
    {
        public float end;
        public float duration;
        public float hand;
        public float wrist;
        public float forearm;
        public float wristValue;
        public float forearmValue;
    }
}
