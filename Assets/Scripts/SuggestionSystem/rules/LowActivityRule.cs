public class LowActivityRule : IRule
{
    private float threshold;

    public LowActivityRule(float threshold)
    {
        this.threshold = threshold;
    }

    public Suggestion Build(AnalysisContext ctx)
    {
        float severity = threshold > 0f
            ? (threshold - ctx.activityRatio) / threshold
            : 1f;

        return new Suggestion
        {
            message = "Muévete más, hay poca actividad",
            severity = UnityEngine.Mathf.Clamp01(severity)
        };
    }

    public bool Evaluate(AnalysisContext ctx)
    {
        return ctx.activityRatio < threshold;
    }


}
