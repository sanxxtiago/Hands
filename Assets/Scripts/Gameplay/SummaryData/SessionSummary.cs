using System;
using System.Collections.Generic;
[Serializable]
public class SessionSummary
{
    public List<ExerciseSummary> Summaries { get; } = new();
    public DateTime date;
    public SessionSummary()
    {
        date = DateTime.Now;
    }
    public void AddSummary(ExerciseSummary summary)
    {
        Summaries.Add(summary);
    }
}