using System;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [SerializeField] private SessionSummary currentSession;
    public SessionSummary CurrentSession => currentSession;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void BeginSession()
    {
        Debug.Log("Sesion iniciada");
        currentSession = new SessionSummary();
    }

    public void AddExerciseSummary(ExerciseSummary summary)
    {
        if (CurrentSession == null)
        {
            Debug.LogWarning("No active session. Call BeginSession() first.");
            return;
        }

        CurrentSession.AddSummary(summary);
    }

    public SessionSummary EndSession()
    {
        return CurrentSession;
    }

    public void ClearSession()
    {
        currentSession = null;
    }
}