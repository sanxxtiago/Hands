using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

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

    public CanvasGroup group;

    void OnEnable()
    {
        GameManager.OnExerciseEnd += SetTimeText;
    }

    void OnDisable()
    {
        GameManager.OnExerciseEnd -= SetTimeText;
    }

    void Start()
    {
        group.alpha = 0f;
    }
    public void Display()
    {
        group.DOKill();
        group.alpha = 0;
        group.DOFade(1, 0.3f);
    }
    public void SetTimeText(float duration)
    {
        timeElapsedText.text = $"Tiempo total: {Math.Round(duration,2)} S";
    }

    public void SetResults(HandUsageSummary left, HandUsageSummary right)
    {
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
