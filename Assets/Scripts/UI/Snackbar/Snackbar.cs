using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Snackbar : MonoBehaviour
{
    //public string message;
    public Color error;
    public Color warning;
    public Color success;
    public Image bg;
    public CanvasGroup group;
    public TMP_Text message;

    void OnEnable()
    {
        SnackbarManager.OnShow += Config;
    }
    void Start()
    {
        group.alpha = 0;
    }
    private void Config(SNACKBARTYPE snackBarType, string msg)
    {
        group.DOKill();

        switch (snackBarType)
        {
            case SNACKBARTYPE.ERROR:
                bg.color = error;
                break;
            case SNACKBARTYPE.WARNING:
                bg.color = warning;
                break;
            case SNACKBARTYPE.SUCCESS:
                bg.color = success;
                break;
        }

        message.text = msg;

        Show();
    }

    void Show()
    {
        group.alpha = 0;
        group.DOFade(1, 0.3f);

        DOVirtual.DelayedCall(2f, Hide);
        Invoke("Hide", 2f);
    }

    void Hide()
    {
        group.DOFade(0, 0.3f);
    }

    void OnDisable()
    {
        SnackbarManager.OnShow -= Config;
    }
}
