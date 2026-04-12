public struct ZoneUsageRecord
{
    public MotionZone zone;

    public int totalFrames;
    public int activeFrames;

    public float accumulatedValue; // 🔥 intensidad total
    public float activeTime;       // 🔥 tiempo activo real
}