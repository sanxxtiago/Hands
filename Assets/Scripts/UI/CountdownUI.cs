using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class CountdownUI : MonoBehaviour
{
    public GameManager gameManager;
    public static event Action OnCountdownFinished;
    public CanvasGroup canvasGroup;
    public TMP_Text text;
    public int countdownTime = 3;
    [Header("Fade")]
    [SerializeField, Min(0f), Tooltip("Duración del fundido de salida al terminar la cuenta atrás.")]
    private float fadeDuration = 0.3f;
    private Coroutine countdownCoroutine;

    void Start()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void OnEnable()
    {
        GameManager.OnCountdownStart += StartCountdown;
    }

    private void OnDisable()
    {
        GameManager.OnCountdownStart -= StartCountdown;
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        if (canvasGroup != null)
            canvasGroup.DOKill();
    }

    void StartCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        if (canvasGroup != null)
            canvasGroup.DOKill();
        countdownCoroutine = StartCoroutine(Countdown());
    }

    IEnumerator Countdown()
    {
        if (canvasGroup == null)
            yield break;
        canvasGroup.DOKill();
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        for (int i = countdownTime; i > 0f; i--)
        {
            text.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        text.text = "¡VAMOS!";

        yield return new WaitForSeconds(0.5f);
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        // Fundido de salida; el evento se emite al completarlo para encadenar con PLAYING.
        canvasGroup
            .DOFade(0f, fadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                countdownCoroutine = null;
                OnCountdownFinished?.Invoke();
            });
        // GameManager.Instance.SetState(GAMESTATE.PLAYING);
    }
}

