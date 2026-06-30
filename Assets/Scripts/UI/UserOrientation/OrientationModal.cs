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
    [SerializeField] private string sceneName;
    private CanvasGroup group;
    private bool isClosing;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        orientationManager.OnPhaseCompleted += Open;
    }

    private void OnDisable()
    {
        orientationManager.OnPhaseCompleted -= Open;
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