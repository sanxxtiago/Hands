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
        ApplyVisualTheme();
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

    private void ApplyVisualTheme()
    {
        if (startButton != null)
        {
            startButton.image.color = new Color(0.04f, 0.55f, 0.62f, 1f);
            startButton.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 86f);
        }

        if (registeredUserText != null)
            registeredUserText.color = new Color(0.68f, 0.9f, 0.96f, 1f);

        if (registrationPanel != null)
        {
            Image panelImage = registrationPanel.GetComponent<Image>();
            if (panelImage != null)
                panelImage.color = new Color(0.035f, 0.08f, 0.14f, 0.92f);
        }

        GameObject titleObject = GameObject.Find("Tittle");
        if (titleObject != null && titleObject.TryGetComponent(out TMPro.TMP_Text titleText))
        {
            titleText.color = new Color(0.78f, 0.95f, 1f, 1f);
            titleText.fontSize = 72f;
        }
    }
}
