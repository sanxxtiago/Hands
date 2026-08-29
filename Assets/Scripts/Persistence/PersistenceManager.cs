using UnityEngine;

public class PersistenceManager : MonoBehaviour
{

    public static PersistenceManager Instance { get; private set; }

    public UserService UserService { get; private set; }
    public SessionService SessionService { get; private set; }
    public ScoreService ScoreService { get; private set; }
    public LeaderboardService LeaderboardService { get; private set; }
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

        InitializeServices();
    }

    private void InitializeServices()
    {
        UserService = new UserService();
        UserService.Load();

        string userId = UserService.CurrentUser?.UserId;

        SessionService = new SessionService(userId);
        ScoreService = new ScoreService(userId);
        LeaderboardService = new LeaderboardService();
        SessionService.Load();
        ScoreService.Load();
        LeaderboardService.Load();

        UserService.OnCurrentUserChanged += OnCurrentUserChanged;
        //SettingsService.Load();
    }

    private void OnCurrentUserChanged()
    {
        string userId = UserService.CurrentUser?.UserId;

        Debug.Log(
            $"[PersistenceManager] Recargando datos para el usuario {userId ?? "ninguno"}.");

        SessionService.SetUserContext(userId);
        ScoreService.SetUserContext(userId);

        SessionService.Load();
        ScoreService.Load();
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
