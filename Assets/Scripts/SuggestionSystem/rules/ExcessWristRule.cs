public class ExcessWristRule : IRule
{
    private float tolerance;

    public ExcessWristRule(float tolerance)
    {
        this.tolerance = tolerance;
    }

    public bool Evaluate(AnalysisContext ctx)
    {
        return ctx.deviation.wrist > 1f;
    }

    public Suggestion Build(AnalysisContext ctx)
    {
        return new Suggestion
        {
            message = "Estás abusando de la muñeca",
            severity = ctx.deviation.wrist
        };
    }
}