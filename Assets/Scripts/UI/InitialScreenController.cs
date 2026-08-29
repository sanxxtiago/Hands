using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InitialScreenController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text registeredUserText;
    [SerializeField] private GameObject registrationPanel;
    [SerializeField] private Transition transition;

    [Header("User Profiles")]
    [SerializeField] private Button userChipButton;
    [SerializeField] private GameObject userDropdownPanel;
    [SerializeField] private Transform userDropdownContent;
    [SerializeField] private Button userProfileOptionPrefab;

    [Header("Navigation")]
    [SerializeField] private string nextSceneName = "UserPreparation";

    private bool wasRegistered;
    private UserService subscribedUserService;

  

    private void OnEnable()
    {
        if (startButton != null)
            startButton.onClick.AddListener(LoadUserPreparation);

        if (userChipButton != null)
            userChipButton.onClick.AddListener(ToggleUserDropdown);

        SubscribeToUserService();
    }

    private void Start()
    {
        //ApplyVisualTheme();
        SubscribeToUserService();
        RefreshUserState();
        transition.FadeOut();
    }

    private void Update()
    {
        if (PersistenceManager.Instance == null)
            return;

        bool hasCurrentUser = PersistenceManager.Instance.UserService.HasCurrentUser;
        if (hasCurrentUser != wasRegistered)
            RefreshUserState();
    }

    private void OnDisable()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(LoadUserPreparation);

        if (userChipButton != null)
            userChipButton.onClick.RemoveListener(ToggleUserDropdown);

        UnsubscribeFromUserService();
    }

    public void RefreshUserState()
    {
        if (PersistenceManager.Instance == null)
        {
            Debug.LogWarning("InitialScreen: no existe un PersistenceManager.", this);
            return;
        }

        UserService userService = PersistenceManager.Instance.UserService;
        wasRegistered = userService.HasCurrentUser;

        if (registeredUserText != null)
        {
            registeredUserText.text = wasRegistered
                ? userService.UserName
                : "Sin registrar";
        }

        if (registrationPanel != null)
            registrationPanel.SetActive(!wasRegistered);

        if (startButton != null)
            startButton.gameObject.SetActive(wasRegistered);

        if (!wasRegistered)
            CloseUserDropdown();
    }

    public void OpenRegistrationForm()
    {
        if (registrationPanel == null)
        {
            Debug.LogWarning("InitialScreen: no existe un panel de registro.", this);
            return;
        }

        CloseUserDropdown();
        registrationPanel.SetActive(true);
    }

    public void ToggleUserDropdown()
    {
        if (userDropdownPanel == null)
        {
            Debug.LogWarning("InitialScreen: no existe un panel de perfiles.", this);
            return;
        }

        if (userDropdownPanel.activeSelf)
        {
            CloseUserDropdown();
            return;
        }

        if (PersistenceManager.Instance == null ||
            PersistenceManager.Instance.UserService == null)
        {
            Debug.LogWarning("InitialScreen: UserService no está disponible.", this);
            return;
        }

        UserService userService = PersistenceManager.Instance.UserService;
        if (!userService.HasUsers)
            return;

        RebuildUserDropdown(userService);
        userDropdownPanel.SetActive(true);
    }

    private void RebuildUserDropdown(UserService userService)
    {
        if (userDropdownContent == null || userProfileOptionPrefab == null)
        {
            Debug.LogWarning("InitialScreen: el desplegable de perfiles no está configurado.", this);
            return;
        }

        for (int i = userDropdownContent.childCount - 1; i >= 0; i--)
        {
            GameObject child = userDropdownContent.GetChild(i).gameObject;

            if (child == userProfileOptionPrefab.gameObject)
                continue;

            Destroy(child);
        }

        foreach (UserProfile profile in userService.Profiles)
        {
            if (profile == null)
                continue;

            Button option = Instantiate(userProfileOptionPrefab, userDropdownContent);
            option.gameObject.SetActive(true);

            TMP_Text optionText = null;
            TMP_Text[] optionTexts = option.GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text text in optionTexts)
            {
                if (text.gameObject.activeSelf)
                {
                    optionText = text;
                    break;
                }
            }

            if (optionText != null)
                optionText.text = profile.Name;

            string profileId = profile.UserId;
            option.onClick.RemoveAllListeners();
            option.onClick.AddListener(() => SelectUser(profileId));
        }
    }

    private void SelectUser(string userId)
    {
        if (PersistenceManager.Instance == null)
            return;

        if (PersistenceManager.Instance.SelectUser(userId))
            CloseUserDropdown();
    }

    private void CloseUserDropdown()
    {
        if (userDropdownPanel != null)
            userDropdownPanel.SetActive(false);
    }

    private void SubscribeToUserService()
    {
        if (subscribedUserService != null || PersistenceManager.Instance == null)
            return;

        UserService userService = PersistenceManager.Instance.UserService;
        if (userService == null)
            return;

        userService.OnCurrentUserChanged += HandleCurrentUserChanged;
        subscribedUserService = userService;
    }

    private void UnsubscribeFromUserService()
    {
        if (subscribedUserService == null)
            return;

        subscribedUserService.OnCurrentUserChanged -= HandleCurrentUserChanged;
        subscribedUserService = null;
    }

    private void HandleCurrentUserChanged()
    {
        RefreshUserState();
        CloseUserDropdown();
    }

    public void LoadUserPreparation()
    {
        if (!wasRegistered)
            return;

        if (transition == null)
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        transition.FadeIn(() => SceneManager.LoadScene(nextSceneName));
    }

}
