using UnityEngine;

public class ExcessWristRule : IRule
{
    private readonly float criticality;

    public ExcessWristRule(float criticality)
    {
        this.criticality = criticality;
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
            severity = Mathf.Clamp01(ctx.deviation.wrist - 1f) * criticality
        };
    }
}