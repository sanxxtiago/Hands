using UnityEngine;

public class LowForearmRule : IRule
{
    private readonly float criticality;

    public LowForearmRule(float criticality)
    {
        this.criticality = criticality;
    }

    public bool Evaluate(AnalysisContext ctx)
    {
        return ctx.deviation.forearm < -1f;
    }

    public Suggestion Build(AnalysisContext ctx)
    {
        return new Suggestion
        {
            message = "Involucra más el antebrazo",
            severity = Mathf.Clamp01(-ctx.deviation.forearm - 1f) * criticality
        };
    }
}