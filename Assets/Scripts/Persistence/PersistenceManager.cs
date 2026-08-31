using UnityEngine;

public class PersistenceManager : MonoBehaviour
{
    [Header("Score Classification")]
    [SerializeField] private ScoreClassificationCatalog classificationCatalog;

    public static PersistenceManager Instance { get; private set; }

    public UserService UserService { get; private set; }
    public SessionService SessionService { get; private set; }
    public ScoreService ScoreService { get; private set; }
    public LeaderboardService LeaderboardService { get; private set; }
    public ExerciseResultPersistenceService ExerciseResultService { get; private set; }
    //public SettingsService SettingsService { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ValidateClassificationConfiguration();
        InitializeServices();
    }

    private void InitializeServices()
    {
        UserService = new UserService();
        UserService.Load();

        string userId = UserService.CurrentUser?.UserId;

        SessionService = new SessionService(userId);
        ScoreService = new ScoreService(userId, classificationCatalog);
        LeaderboardService = new LeaderboardService(classificationCatalog);
        SessionService.Load();
        ScoreService.Load();
        LeaderboardService.Load();

        ExerciseResultService = new ExerciseResultPersistenceService(
            SessionService,
            ScoreService,
            LeaderboardService,
            userId,
            UserService.CurrentUser?.Name);
        ExerciseResultService.RecoverPendingTransaction();

        UserService.OnCurrentUserChanged += OnCurrentUserChanged;
        //SettingsService.Load();
    }

    private void ValidateClassificationConfiguration()
    {
        if (classificationCatalog == null)
        {
            Debug.LogError(
                "[PersistenceManager] No hay catalogo de clasificacion asignado.",
                this);
            return;
        }

        if (!classificationCatalog.TryValidate(out string validationError))
        {
            Debug.LogError(
                $"[PersistenceManager] Catalogo de clasificacion invalido: {validationError}.",
                this);
        }
    }

    private void OnCurrentUserChanged()
    {
        string userId = UserService.CurrentUser?.UserId;

        SessionManager.Instance?.ClearSession();

        Debug.Log(
            $"[PersistenceManager] Recargando datos para el usuario {userId ?? "ninguno"}.");

        SessionService.SetUserContext(userId);
        ScoreService.SetUserContext(userId);

        SessionService.Load();
        ScoreService.Load();

        ExerciseResultService?.SetUserContext(userId, UserService.CurrentUser?.Name);
        ExerciseResultService?.RecoverPendingTransaction();
    }

    public bool SelectUser(string userId)
    {
        if (UserService == null)
        {
            Debug.LogWarning("[PersistenceManager] UserService no está inicializado.");
            return false;
        }

        return UserService.SelectUser(userId);
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        if (UserService != null)
            UserService.OnCurrentUserChanged -= OnCurrentUserChanged;

        Instance = null;
    }
}
