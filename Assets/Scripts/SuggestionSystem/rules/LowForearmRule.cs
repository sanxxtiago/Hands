public class LowForearmRule : IRule
{
    private float tolerance;

    public LowForearmRule(float tolerance)
    {
        this.tolerance = tolerance;
    }

    public bool Evaluate(AnalysisContext ctx)
    {
        return ctx.deviation.forearm < -tolerance;
    }

    public Suggestion Build(AnalysisContext ctx)
    {
        return new Suggestion
        {
            message = "Involucra más el antebrazo",
            severity = -ctx.deviation.forearm
        };
    }
}