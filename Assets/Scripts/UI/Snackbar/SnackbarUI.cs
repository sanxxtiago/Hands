using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SnackbarUI : MonoBehaviour
{
    [Header("Colores")]
    [SerializeField] private Color error = new Color(0.827f, 0.118f, 0.118f, 1f);
    [SerializeField] private Color warning = new Color(1f, 0.757f, 0.027f, 1f);
    [SerializeField] private Color success = new Color(0.298f, 0.686f, 0.314f, 1f);
    [SerializeField] private Color errorText = Color.white;
    [SerializeField] private Color warningText = Color.black;
    [SerializeField] private Color successText = Color.black;

    [Header("Referencias")]
    [SerializeField] private Image bg;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text message;

    private Tween hideTween;

    private void OnEnable()
    {
        SnackbarManager.OnShow += Config;
    }

    private void Start()
    {
        group.alpha = 0;
    }

    private void Config(SNACKBARTYPE snackBarType, string msg, float time)
    {
        group.DOKill();

        Color backgroundColor = error;
        Color textColor = errorText;

        switch (snackBarType)
        {
            case SNACKBARTYPE.ERROR:
                backgroundColor = error;
                textColor = errorText;
                break;
            case SNACKBARTYPE.WARNING:
                backgroundColor = warning;
                textColor = warningText;
                break;
            case SNACKBARTYPE.SUCCESS:
                backgroundColor = success;
                textColor = successText;
                break;
        }

        bg.color = backgroundColor;
        message.color = textColor;
        message.text = msg;

        Show(time);
    }

    private void Show(float duration)
    {
        group.DOKill();

        if (hideTween != null && hideTween.IsActive())
            hideTween.Kill();

        group.alpha = 0;
        group.DOFade(1, 0.3f);

        hideTween = DOVirtual.DelayedCall(duration, Hide);
    }

    private void Hide()
    {
        group.DOFade(0, 0.3f);
    }

    private void OnDisable()
    {
        SnackbarManager.OnShow -= Config;

        group.DOKill();

        if (hideTween != null && hideTween.IsActive())
            hideTween.Kill();
    }
}
