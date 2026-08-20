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

    private void Awake()
    {
        if (transition == null)
            transition = CreateFallbackTransition();
    }

    private void OnEnable()
    {
        if (startButton != null)
            startButton.onClick.AddListener(LoadUserPreparation);
    }

    private void Start()
    {
        RefreshUserState();
        transition?.FadeOut();
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

    private Transition CreateFallbackTransition()
    {
        GameObject transitionObject = new("Transition", typeof(RectTransform));
        Canvas canvas = transitionObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasGroup canvasGroup = transitionObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        Image overlay = transitionObject.AddComponent<Image>();
        overlay.color = Color.black;

        RectTransform rectTransform = overlay.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        return transitionObject.AddComponent<Transition>();
    }
}
