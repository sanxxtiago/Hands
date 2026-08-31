using System;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [System.NonSerialized] private SessionSummary currentSession;
    public SessionSummary CurrentSession => currentSession;

    private string currentSessionUserId;
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
        if (currentSession != null)
        {
            Debug.LogWarning("[SessionManager] Ya existe una sesion activa; no se creara otra.");
            return;
        }

        PersistenceManager persistenceManager = PersistenceManager.Instance;
        if (persistenceManager == null
            || persistenceManager.SessionService == null
            || persistenceManager.ExerciseResultService == null
            || !persistenceManager.ExerciseResultService.IsReady)
        {
            Debug.LogError(
                "[SessionManager] No se puede iniciar la sesion: la persistencia no esta disponible o bloqueada.");
            return;
        }

        string userId = persistenceManager.UserService?.CurrentUser?.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            Debug.LogError("[SessionManager] No se puede iniciar la sesion sin un usuario activo.");
            return;
        }

        Debug.Log("[SessionManager] Sesion iniciada.");
        currentSession = new SessionSummary
        {
            SessionId = persistenceManager.SessionService.PeekNextSessionId()
        };

        currentSessionUserId = userId;
        currentSessionPersisted = false;
    }

    public ExerciseCommitOutcome CommitExerciseResult(
        ExerciseSummary summary,
        ExerciseScore score)
    {
        if (CurrentSession == null)
        {
            Debug.LogWarning("[SessionManager] No hay una sesion activa para confirmar el ejercicio.");
            return ExerciseCommitOutcome.Rejected;
        }

        PersistenceManager persistenceManager = PersistenceManager.Instance;
        ExerciseResultPersistenceService persistenceService =
            persistenceManager?.ExerciseResultService;

        string activeUserId = persistenceManager?.UserService?.CurrentUser?.UserId;
        if (!string.Equals(currentSessionUserId, activeUserId, StringComparison.Ordinal))
        {
            Debug.LogWarning(
                "[SessionManager] El usuario cambio durante la sesion; se descarta el resultado.");
            ClearSession();
            return ExerciseCommitOutcome.Rejected;
        }

        if (persistenceService == null)
        {
            Debug.LogError("[SessionManager] No existe el coordinador de persistencia transaccional.");
            return ExerciseCommitOutcome.Failed;
        }

        ExerciseCommitOutcome outcome = persistenceService.CommitExerciseResult(
            CurrentSession,
            summary,
            score);

        if (outcome == ExerciseCommitOutcome.Committed)
            currentSessionPersisted = true;

        return outcome;
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

        Debug.LogWarning(
            "[SessionManager] La sesion no fue confirmada por el coordinador y se descarta.");
        ClearSession();
        return null;
    }

    public void ClearSession()
    {
        string sessionGuid = currentSession?.SessionGuid;
        PersistenceManager.Instance?.ExerciseResultService?.DiscardPendingSession(sessionGuid);
        currentSession = null;
        currentSessionUserId = null;
        currentSessionPersisted = false;
    }
}
