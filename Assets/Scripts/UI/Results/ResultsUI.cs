using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultsUI : MonoBehaviour
{
    public ArmResultUI rightArmAbsoluteResult;
    public ArmResultUI rightArmRelativeResult;

    public ArmResultUI leftArmAbsoluteResult;
    public ArmResultUI leftArmRelativeResult;
    public TMP_Text timeElapsedText;
    [Header("LEFT - ABS")]
    public TMP_Text leftAbsHand;
    public TMP_Text leftAbsWrist;
    public TMP_Text leftAbsForearm;

    [Header("LEFT - REL")]
    public TMP_Text leftRelHand;
    public TMP_Text leftRelWrist;
    public TMP_Text leftRelForearm;

    [Header("RIGHT - ABS")]
    public TMP_Text rightAbsHand;
    public TMP_Text rightAbsWrist;
    public TMP_Text rightAbsForearm;

    [Header("RIGHT - REL")]
    public TMP_Text rightRelHand;
    public TMP_Text rightRelWrist;
    public TMP_Text rightRelForearm;

    [Header("LEFT - ACTIVITY")]
    public TMP_Text leftActivityText;
    public TMP_Text leftDurationText;

    [Header("RIGHT - ACTIVITY")]
    public TMP_Text rightActivityText;
    public TMP_Text rightDurationText;

    [Header("GENERAL SUGGESTION")]
    public TMP_Text generalSuggestionText;

    public CanvasGroup group;
    [SerializeField] private Button closeButton;
    [SerializeField] private float fadeInTime = .5f;

    private void OnEnable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }
    }

    private void OnDisable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Hide);
        }
    }

    private void Start()
    {
        Hide();
    }

    public void Display()
    {
        if (group == null)
            return;

        group.DOKill();
        group.alpha = 0f;
        group.interactable = true;
        group.blocksRaycasts = true;
        group.DOFade(1f, fadeInTime);
    }

    public void Hide()
    {
        if (group == null)
            return;

        group.DOKill();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    public void SetResults(float duration, HandUsageSummary left, HandUsageSummary right)
    {
        timeElapsedText.text = $"Tiempo total: {Math.Round(duration,2)} S";

        //pintar brazos
        leftArmAbsoluteResult.Paint(left, left.absoluteUsage);
        rightArmAbsoluteResult.Paint(right, right.absoluteUsage);

        leftArmRelativeResult.Paint(left, left.relativeUsage);
        rightArmRelativeResult.Paint(right, right.relativeUsage);

        //texto
        // LEFT ABS
        SetZoneTexts(left, left.absoluteUsage,
            leftAbsHand, leftAbsWrist, leftAbsForearm);

        // LEFT REL
        SetZoneTexts(left, left.relativeUsage,
            leftRelHand, leftRelWrist, leftRelForearm);

        // RIGHT ABS
        SetZoneTexts(right, right.absoluteUsage,
            rightAbsHand, rightAbsWrist, rightAbsForearm);

        // RIGHT REL
        SetZoneTexts(right, right.relativeUsage,
            rightRelHand, rightRelWrist, rightRelForearm);

        //Actividad
        leftActivityText.text = $"Actividad: {left.activityRatio * 100f:F1}%";
        leftDurationText.text = $"Tiempo activo: {left.totalActiveSeconds:F1}s";

        rightActivityText.text = $"Actividad: {right.activityRatio * 100f:F1}%";
        rightDurationText.text = $"Tiempo activo: {right.totalActiveSeconds:F1}s";

        if (generalSuggestionText != null)
        {
            string suggestion = SessionRecorder.LastGeneralSuggestion;
            generalSuggestionText.text = string.IsNullOrWhiteSpace(suggestion)
                ? "Continúa practicando para mejorar tu desempeño."
                : suggestion;
        }
    }
    void SetZoneTexts(HandUsageSummary summary, float[] values,
                  TMP_Text handText,
                  TMP_Text wristText,
                  TMP_Text forearmText)
    {
        float hand = 0f;
        float wrist = 0f;
        float forearm = 0f;

        for (int i = 0; i < summary.zones.Length; i++)
        {
            switch (summary.zones[i])
            {
                case MotionZone.Hand:
                    hand = values[i];
                    break;

                case MotionZone.Wrist:
                    wrist = values[i];
                    break;

                case MotionZone.Forearm:
                    forearm = values[i];
                    break;
            }
        }

        handText.text = $"Mano: {(hand * 100f):F1}%";
        wristText.text = $"Muñeca: {(wrist * 100f):F1}%";
        forearmText.text = $"Antebrazo: {(forearm * 100f):F1}%";
    }
}
