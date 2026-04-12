public struct GestureState
{
    public GestureType type;
    public HandType handType;

    public float strength;   // 0..1 continuo
    public bool isActive;    // threshold final

    public long frameId;
}