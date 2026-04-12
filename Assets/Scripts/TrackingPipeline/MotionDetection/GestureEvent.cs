public struct GestureEvent
{
    public GestureType type;
    public HandType handType;

    public float strength;   // 0..1
    public long frameId;
}