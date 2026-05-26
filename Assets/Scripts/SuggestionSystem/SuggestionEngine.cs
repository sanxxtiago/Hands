using System.Collections.Generic;
using UnityEngine;

public class SuggestionEngine
{
    private List<TimedRule> rules = new();

    public void AddRule(TimedRule rule)
    {
        rules.Add(rule);
    }

    public Suggestion Evaluate(AnalysisContext ctx, float dt)
    {
        Debug.Log(
            $"Hand: {ctx.deviation.hand:F2} | " +
            $"Wrist: {ctx.deviation.wrist:F2} | " +
            $"Forearm: {ctx.deviation.forearm:F2}"
        );
        Suggestion best = null;

        foreach (var rule in rules)
        {
            var suggestion = rule.Update(ctx, dt);

            if (suggestion == null) continue;

            if (best == null || suggestion.severity > best.severity)
                best = suggestion;
        }

        return best;
    }
}