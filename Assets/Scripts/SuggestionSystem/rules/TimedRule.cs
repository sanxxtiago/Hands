public class TimedRule
{
    private IRule rule;

    private float activeTime;
    private float cooldown;

    private float triggerTime;
    private float cooldownTime;

    public TimedRule(IRule rule, float triggerTime, float cooldownTime)
    {
        this.rule = rule;
        this.triggerTime = triggerTime;
        this.cooldownTime = cooldownTime;
    }

    public Suggestion Update(AnalysisContext ctx, float dt)
    {
        if (cooldown > 0f)
        {
            cooldown -= dt;
            return null;
        }

        if (rule.Evaluate(ctx))
        {
            activeTime += dt;

            if (activeTime >= triggerTime)
            {
                activeTime = 0f;
                cooldown = cooldownTime;

                return rule.Build(ctx);
            }
        }
        else
        {
            activeTime = 0f;
        }

        return null;
    }
}