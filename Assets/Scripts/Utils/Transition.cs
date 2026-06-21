using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class Transition : MonoBehaviour
{
    [SerializeField] private TMP_Text message;
    [SerializeField] private float fadeDuration = 1f;
    private bool isTransitioning = false;
    public event Action OnFadeOutCompleted;
    private CanvasGroup group;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    public void FadeOut()
    {
        group.DOKill();

        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;

        group
            .DOFade(0f, fadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                OnFadeOutCompleted?.Invoke();
            });
    }

    public void FadeIn(
     Action onComplete = null,
     float holdDuration = 2f)
    {
        if (isTransitioning)
            return;

        isTransitioning = true;

        group.DOKill();

        group.interactable = false;
        group.blocksRaycasts = true;
        group.alpha = 0f;

        Sequence seq = DOTween.Sequence();

        seq.Append(
            group
                .DOFade(1f, fadeDuration)
                .SetEase(Ease.InQuad)
        );

        seq.AppendInterval(holdDuration);

        seq.OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }

    public void SetMessage(string text)
    {
        if (message.Equals(""))
            return;

        if (message != null)
            message.text = text;
    }
}