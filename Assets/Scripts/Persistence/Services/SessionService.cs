using System.Collections.Generic;
using System.Linq;

public class SessionService
{
    private string userId;
    private SessionsData sessionsData = new();

    public SessionService()
    {
    }

    public SessionService(string userId)
    {
        SetUserContext(userId);
    }

    public IReadOnlyList<SessionSummary> Sessions => sessionsData.Sessions;

    public int TotalSessions => sessionsData.Sessions.Count;

    public void SetUserContext(string userId)
    {
        this.userId = userId;
        sessionsData = new SessionsData();
    }

    public void Load()
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            sessionsData = new SessionsData();
            return;
        }

        sessionsData = SaveSystem.Load<SessionsData>(userId, SaveFiles.Sessions);

        if (sessionsData == null)
        {
            sessionsData = new SessionsData();
        }
    }

    public void Save()
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        SaveSystem.Save(userId, SaveFiles.Sessions, sessionsData);
    }

    public SessionSummary AddSession(SessionSummary summary)
    {
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
        if (!string.IsNullOrWhiteSpace(userId))
            SaveSystem.Delete(userId, SaveFiles.Sessions);

        sessionsData = new SessionsData();
    }

    public int PeekNextSessionId() => GetNextSessionId();

    private int GetNextSessionId()
    {
        if (sessionsData.Sessions.Count == 0)
            return 1;

        return sessionsData.Sessions.Max(s => s.SessionId) + 1;
    }
}
