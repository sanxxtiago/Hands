using System.Collections.Generic;
using System.Linq;

public class SessionService
{
    private SessionsData sessionsData = new();

    public IReadOnlyList<SessionSummary> Sessions => sessionsData.Sessions;

    public int TotalSessions => sessionsData.Sessions.Count;

    public void Load()
    {
        sessionsData = SaveSystem.Load<SessionsData>(SaveFiles.Sessions);

        if (sessionsData == null)
        {
            sessionsData = new SessionsData();
        }
    }

    public void Save()
    {
        SaveSystem.Save(SaveFiles.Sessions, sessionsData);
    }

    public SessionSummary AddSession(SessionSummary summary)
    {
        summary.SessionId = GetNextSessionId();

        sessionsData.Sessions.Add(summary);

        Save();

        return summary;
    }

    public IReadOnlyList<SessionSummary> GetLastSessions(int count = 7)
    {
        return sessionsData.Sessions
            .OrderByDescending(s => s.SessionId)
            .Take(count)
            .Reverse()
            .ToList();
    }

    public void DeleteAll()
    {
        SaveSystem.Delete(SaveFiles.Sessions);
        sessionsData = new SessionsData();
    }

    private int GetNextSessionId()
    {
        if (sessionsData.Sessions.Count == 0)
            return 1;

        return sessionsData.Sessions.Max(s => s.SessionId) + 1;
    }
}