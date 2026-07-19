using UnityEngine;

public class PersistenceManager : MonoBehaviour
{

    public static PersistenceManager Instance { get; private set; }

    public UserService UserService { get; private set; }
    //public SessionService SessionService { get; private set; }
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
        //SessionService = new SessionService();
        //SettingsService = new SettingsService();

        UserService.Load();
        Debug.Log(UserService.UserName);
        //SessionService.Load();
        //SettingsService.Load();
    }
}