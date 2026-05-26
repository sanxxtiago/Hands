public interface IRule
{
    bool Evaluate(AnalysisContext ctx);
    Suggestion Build(AnalysisContext ctx);
}