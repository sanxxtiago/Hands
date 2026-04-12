public struct MotionData
{
    public MotionZone zone;
    public HandType handType;

    public float value;        // 0..1
    public float rawAngle;     // grados reales (clave para clínica)
    public float velocity;     // opcional

    public bool isActive;

    public long frameId;
}