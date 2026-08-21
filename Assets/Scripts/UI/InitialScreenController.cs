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

    [Header("Navigation")]
    [SerializeField] private string nextSceneName = "UserPreparation";

    private bool wasRegistered;

  

    private void OnEnable()
    {
        if (startButton != null)
            startButton.onClick.AddListener(LoadUserPreparation);
    }

    private void Start()
    {
        //ApplyVisualTheme();
        RefreshUserState();
        transition.FadeOut();
    }

    private void Update()
    {
        if (PersistenceManager.Instance == null)
            return;

        bool isRegistered = PersistenceManager.Instance.UserService.IsRegistered;
        if (isRegistered != wasRegistered)
            RefreshUserState();
    }

    private void OnDisable()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(LoadUserPreparation);
    }

    public void RefreshUserState()
    {
        if (PersistenceManager.Instance == null)
        {
            Debug.LogWarning("InitialScreen: no existe un PersistenceManager.", this);
            return;
        }

        UserService userService = PersistenceManager.Instance.UserService;
        wasRegistered = userService.IsRegistered;

        if (registeredUserText != null)
        {
            registeredUserText.text = wasRegistered
                ? userService.UserName
                : "Usuario sin registrar";
        }

        if (registrationPanel != null)
            registrationPanel.SetActive(!wasRegistered);

        if (startButton != null)
            startButton.gameObject.SetActive(wasRegistered);
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
