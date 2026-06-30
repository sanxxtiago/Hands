using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class DemoManager : MonoBehaviour
{
    [Header("Video Player")]
    [SerializeField] VideoPlayer demoPlayer;
    [SerializeField] Slider progressBar;
    [Header("Animation")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private float fadeDuration = 0.3f;
    private bool isClosing = false;
    public static event Action OnDemoClosed;
    void Start()
    {
        group.alpha = 1;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    void Update()
    {
        UpdateProgressBar();
    }
    //Referencia al botón
    public void CloseDemo()
    {
        if (isClosing)
            return;

        isClosing = true;

        group.DOKill();
        group.interactable = false;
        group.blocksRaycasts = false;

        group
            .DOFade(0f, fadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                demoPlayer.Stop();
                OnDemoClosed?.Invoke();
                isClosing = false;
            });
    }

    private void UpdateProgressBar()
    {
        float progress = (float)demoPlayer.time / (float)demoPlayer.length;
        progressBar.value = progress;
    }

}
