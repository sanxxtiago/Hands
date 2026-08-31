public enum HybridPairStatus
{
    Waiting,
    Ready,
    Rejected
}

/// <summary>Conserva como máximo un pendiente por fuente y mano, sin retener listas de movimiento.</summary>
public sealed class HybridFramePairer
{
    private readonly HandType hand;
    private bool hasUsage;
    private bool hasExposure;
    private bool hasCompleted;
    private long lastFrameId;
    private float lastTimestamp;
    public FrameUsageData Usage { get; private set; }
    public FrameErgonomicExposureData Exposure { get; private set; }

    public HybridFramePairer(HandType hand)
    {
        this.hand = hand;
    }

    public void Reset()
    {
        ClearPending();
        hasCompleted = false;
        lastFrameId = 0;
        lastTimestamp = 0f;
    }

    public void ClearPending()
    {
        hasUsage = hasExposure = false;
        Usage = default;
        Exposure = default;
    }

    public HybridPairStatus Push(FrameUsageData usage)
    {
        if (!Accept(usage.handType, usage.frameId, usage.timestamp))
            return Reject();
        bool overwritten = hasUsage;
        Usage = usage;
        hasUsage = true;
        HybridPairStatus status = Pair();
        return overwritten && status == HybridPairStatus.Waiting ? HybridPairStatus.Rejected : status;
    }

    public HybridPairStatus Push(FrameErgonomicExposureData exposure)
    {
        if (!Accept(exposure.handType, exposure.frameId, exposure.timestamp))
            return Reject();
        bool overwritten = hasExposure;
        Exposure = exposure;
        hasExposure = true;
        HybridPairStatus status = Pair();
        return overwritten && status == HybridPairStatus.Waiting ? HybridPairStatus.Rejected : status;
    }

    private bool Accept(HandType candidateHand, long id, float timestamp)
    {
        return candidateHand == hand && id > 0 && !float.IsNaN(timestamp) &&
            !float.IsInfinity(timestamp) &&
            (!hasCompleted || (id > lastFrameId && timestamp > lastTimestamp));
    }

    private HybridPairStatus Pair()
    {
        if (!hasUsage || !hasExposure) return HybridPairStatus.Waiting;
        if (Usage.frameId != Exposure.frameId)
        {
            if (Usage.frameId < Exposure.frameId) hasUsage = false;
            else hasExposure = false;
            return HybridPairStatus.Rejected;
        }
        if (Usage.timestamp != Exposure.timestamp) return Reject();
        hasCompleted = true;
        lastFrameId = Usage.frameId;
        lastTimestamp = Usage.timestamp;
        hasUsage = hasExposure = false;
        return HybridPairStatus.Ready;
    }

    private HybridPairStatus Reject()
    {
        ClearPending();
        return HybridPairStatus.Rejected;
    }
}
