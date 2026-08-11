public class LowForearmRule : IRule
{
    public bool Evaluate(AnalysisContext ctx)
    {
        return ctx.deviation.forearm < -1f;
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
