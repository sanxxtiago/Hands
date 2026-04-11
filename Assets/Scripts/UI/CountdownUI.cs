using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class CountdownUI : MonoBehaviour
{
    public event Action OnCountdownFinished;
    public CanvasGroup canvasGroup;
    public TMP_Text text;
    public int countdownTime = 3;

    void Start()
    {
        canvasGroup.alpha = 1f;
    }

    private void OnEnable()
    {
        GameManager.Instance.OnCountdownStart += StartCountdown;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnCountdownStart -= StartCountdown;
    }

    void StartCountdown()
    {
        StartCoroutine(Countdown());
    }

    IEnumerator Countdown()
    {
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

