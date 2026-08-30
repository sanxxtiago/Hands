using System;
using System.Collections.Generic;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [SerializeField] private SessionSummary currentSession;
    public SessionSummary CurrentSession => currentSession;

    private bool currentSessionPersisted;

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
        currentSession = new SessionSummary
        {
            SessionId = PersistenceManager.Instance.SessionService.PeekNextSessionId()
        };

        currentSessionPersisted = false;
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
        if (CurrentSession == null)
        {
            Debug.LogWarning("[SessionManager] No hay una sesion activa para finalizar.");
            return null;
        }

        if (currentSessionPersisted)
            return CurrentSession;

        if (!HasAllExercises(CurrentSession))
        {
            Debug.LogWarning("[SessionManager] La sesion incompleta se descarta.");
            ClearSession();
            return null;
        }

        if (PersistenceManager.Instance == null || PersistenceManager.Instance.SessionService == null)
        {
            Debug.LogWarning("[SessionManager] No se puede guardar la sesion: PersistenceManager no disponible.");
            return null;
        }

        PersistenceManager.Instance.SessionService.AddSession(CurrentSession);
        currentSessionPersisted = true;
        return CurrentSession;
    }

    public void ClearSession()
    {
        currentSession = null;
        currentSessionPersisted = false;
    }

    private static bool HasAllExercises(SessionSummary session)
    {
        if (session.Summaries == null || session.Summaries.Count != 3)
            return false;

        HashSet<ExerciseType> exerciseTypes = new();
        foreach (ExerciseSummary summary in session.Summaries)
        {
            if (summary == null || !exerciseTypes.Add(summary.exerciseType))
                return false;
        }

        return exerciseTypes.Contains(ExerciseType.Insert)
            && exerciseTypes.Contains(ExerciseType.OSU)
            && exerciseTypes.Contains(ExerciseType.DuckHunter);
    }
}
