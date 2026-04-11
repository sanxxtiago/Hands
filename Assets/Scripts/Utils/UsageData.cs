public class UsageData
{
    public float hand;
    public float wrist;
    public float forearm;

    public void Reset()
    {
        hand = 0;
        wrist = 0;
        forearm = 0;
    }

    public (float h, float w, float f) GetPercentages()
    {
        float total = hand + wrist + forearm;

        if (total <= 0) return (0, 0, 0);

        return (
            hand / total,
            wrist / total,
            forearm / total
        );
    }
}