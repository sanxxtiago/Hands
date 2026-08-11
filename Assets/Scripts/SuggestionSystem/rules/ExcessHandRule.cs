using UnityEngine;

public class ExcessHandRule : IRule
{
    private readonly float criticality;

    public ExcessHandRule(float criticality)
    {
        this.criticality = criticality;
    }

    public bool Evaluate(AnalysisContext ctx)
    {
        return ctx.deviation.hand > 1f;
    }

    public Suggestion Build(AnalysisContext ctx)
    {
        return new Suggestion
        {
            message = "Estás abusando de la mano",
            severity = Mathf.Clamp01(ctx.deviation.hand - 1f) * criticality
        };
    }
}