public class ExcessHandRule : IRule
{
    public bool Evaluate(AnalysisContext ctx)
    {
        return ctx.deviation.hand > 1f;
    }

    public Suggestion Build(AnalysisContext ctx)
    {
        return new Suggestion
        {
            message = "Estás abusando de la mano",
            severity = ctx.deviation.hand
        };
    }
}
