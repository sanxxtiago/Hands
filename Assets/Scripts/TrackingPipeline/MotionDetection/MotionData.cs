public struct MotionData
{
    public MotionZone zone;
    public HandType handType;

    public float value;      // 0..1 normalizado
    public bool isActive;    // supera threshold

    public long frameId;
}