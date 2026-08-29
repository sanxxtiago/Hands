using UnityEngine;

public class ScorePersistenceListener : MonoBehaviour
{
    public static ScorePersistenceListener Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        ScoreEventBus.OnScoreCompleted += OnScoreCompleted;
    }

    private void OnDisable()
    {
        ScoreEventBus.OnScoreCompleted -= OnScoreCompleted;
    }

    private void OnScoreCompleted(ExerciseScore score)
    {
        if (score == null || !score.isValid)
            return;

        PersistenceManager persistenceManager = PersistenceManager.Instance;

        if (persistenceManager == null)
        {
            Debug.LogWarning("[ScorePersistenceListener] PersistenceManager no disponible.");
            return;
        }

        var session = SessionManager.Instance?.CurrentSession;
        if (session == null)
        {
            Debug.LogWarning("[ScorePersistenceListener] No hay sesion activa; se ignoro el score.");
            return;
        }

        int exerciseIndex = session.Summaries.Count;

        ScoreRecord record = new ScoreRecord
        {
            sessionGuid = session.SessionGuid,
            sessionIdNumeric = session.SessionId,
            exerciseIndex = exerciseIndex,
            exerciseType = score.exerciseType,
            totalScore = score.totalScore,
            scoreGrade = score.scoreGrade,
            trophyTier = ScoreTierResolver.GetTier(score),
            motivationalMessage = score.motivationalMessage,
            isValid = score.isValid,
            statsData = score.statsData,
            breakdown = score.breakdown
        };

        persistenceManager.ScoreService.AddScore(record);

        UserData currentUser = persistenceManager.UserService?.CurrentUser;
        persistenceManager.LeaderboardService?.UpdateHighscore(
            record,
            currentUser?.UserId,
            currentUser?.Name);

        Debug.Log($"[ScorePersistenceListener] Score persistido: ejercicio={score.exerciseType}, " +
            $"score={score.totalScore:F2}, grado={score.scoreGrade}, " +
            $"sessionGuid={session.SessionGuid}, ejercicioIndex={exerciseIndex}.");
    }
}
