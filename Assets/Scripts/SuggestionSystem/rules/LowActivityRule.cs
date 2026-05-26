public class LowActivityRule : IRule
{
    private float threshold;

    public LowActivityRule(float threshold)
    {
        this.threshold = threshold;
    }

    public Suggestion Build(AnalysisContext ctx)
    {
        return new Suggestion
        {
            message = "Muévete más, hay poca actividad",
            severity = 1f - ctx.activityRatio
        };
    }

    public bool Evaluate(AnalysisContext ctx)
    {
        return ctx.activityRatio < 1f;
    }


}