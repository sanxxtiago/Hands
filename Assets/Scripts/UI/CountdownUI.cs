using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class CountdownUI : MonoBehaviour
{
    public GameManager gameManager;
    public static event Action OnCountdownFinished;
    public CanvasGroup canvasGroup;
    public TMP_Text text;
    public int countdownTime = 3;

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
    }

    void StartCountdown()
    {
        StartCoroutine(Countdown());
    }

    IEnumerator Countdown()
    {
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        for (int i = countdownTime; i > 0f; i--)
        {
            text.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        text.text = "GO";

        yield return new WaitForSeconds(0.5f);
        canvasGroup.alpha = 0f;
        OnCountdownFinished?.Invoke();
        // GameManager.Instance.SetState(GAMESTATE.PLAYING);
    }
}

