using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Video;

public class DemoManager : MonoBehaviour
{
    [Header("Video Player")]
    [SerializeField] VideoPlayer demoPlayer;

    [Header("Animation")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private float fadeDuration = 0.3f;
    private bool isClosing = false;
    public event Action OnDemoClosed;
    void Start()
    {
        group.alpha = 1;
        group.interactable = true;
        group.blocksRaycasts = true;
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

}
