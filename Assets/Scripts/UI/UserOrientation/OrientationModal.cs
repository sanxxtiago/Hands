using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CanvasGroup))]
public class OrientationModal : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OrientationPhase3Manager orientationManager;
    [SerializeField] private Transition transition;

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private RectTransform modalContent;
    [SerializeField, Min(0f)] private float popDuration = 0.25f;
    [SerializeField] private string sceneName;
    private CanvasGroup group;
    private Vector3 modalContentScale = Vector3.one;
    private bool isClosing;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();

        if (modalContent == null)
        {
            Transform content = transform.Find("ModalContent");
            if (content != null)
                modalContent = content.GetComponent<RectTransform>();
        }

        if (modalContent != null)
            modalContentScale = modalContent.localScale;
    }

    private void OnEnable()
    {
        orientationManager.OnPhaseCompleted += Open;
    }

    private void OnDisable()
    {
        if (orientationManager != null)
            orientationManager.OnPhaseCompleted -= Open;

        group?.DOKill(false);
        modalContent?.DOKill(false);
    }

    private void Start()
    {
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    public void Open()
    {
        isClosing = false;

        group.DOKill();

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        if (modalContent != null)
        {
            modalContent.DOKill(false);
            modalContent.localScale = modalContentScale * 0.96f;
            modalContent
                .DOScale(modalContentScale, popDuration)
                .SetEase(Ease.OutBack);
        }

        group
            .DOFade(1f, fadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                group.interactable = true;
                group.blocksRaycasts = true;
            });
    }

    public void Continue()
    {
        Debug.Log(isClosing);
        if (isClosing)
            return;

        CloseAndExecute(() =>
        {
            transition.FadeIn(() =>
            {
                SceneManager.LoadScene(sceneName);
            });
        });
    }

    public void Retry()
    {
        Debug.Log(isClosing);

        if (isClosing)
            return;

        CloseAndExecute(() =>
        {
            transition.SetMessage("Reiniciando familiarización...");

            transition.FadeIn(() =>
            {
                SceneManager.LoadScene("UserOrientationPhase1");
            });
        });
    }

    private void CloseAndExecute(Action callback)
    {
        isClosing = true;

        group.interactable = false;
        group.blocksRaycasts = false;

        group.DOKill();

        group
            .DOFade(0f, 0.2f)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                callback?.Invoke();
            });
    }
}
