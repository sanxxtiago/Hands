using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class DemoManager : MonoBehaviour
{
    [Header("Video Player")]
    [SerializeField] VideoPlayer demoPlayer;
    [SerializeField] Slider progressBar;
    [SerializeField] private TMP_Text remainingTimeText;
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
        UpdateRemainingTime();
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
        if (demoPlayer == null || progressBar == null)
            return;

        double length = demoPlayer.length;
        if (length <= 0.0 || double.IsNaN(length) || double.IsInfinity(length))
        {
            progressBar.value = 0f;
            return;
        }

        progressBar.value = Mathf.Clamp01((float)(demoPlayer.time / length));
    }

    private void UpdateRemainingTime()
    {
        if (demoPlayer == null || remainingTimeText == null)
            return;

        double length = demoPlayer.length;
        if (length <= 0.0 || double.IsNaN(length) || double.IsInfinity(length))
        {
            remainingTimeText.text = "--:--";
            return;
        }

        double remainingTime = Mathf.Max(0f, (float)(length - demoPlayer.time));
        int totalSeconds = Mathf.CeilToInt((float)remainingTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        remainingTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

}
