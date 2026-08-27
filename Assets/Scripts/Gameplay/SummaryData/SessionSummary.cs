using System;
using System.Collections.Generic;
[Serializable]
public class SessionSummary
{
    public int SessionId;
    public string SessionGuid;
    public List<ExerciseSummary> Summaries { get; } = new();
    public DateTime date;
    public SessionSummary()
    {
        SessionGuid = System.Guid.NewGuid().ToString();
        date = DateTime.Now;
    }
    public void AddSummary(ExerciseSummary summary)
    {
        Summaries.Add(summary);
    }
}