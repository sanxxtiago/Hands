using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class ArmResultUI : ArmColor
{

    public TMP_Text handResult;
    public TMP_Text wristResult;
    public TMP_Text foreArmResult;
    public TMP_Text activity;
    public TMP_Text inactivity;

    private (float hand, float wrist, float forearm) lastData;
    private bool hasData = false;
    public void PaintOnResults((float hand, float wrist, float forearm) data)
    {
        lastData = data;
        hasData = true;

        // si no está activo → no animar todavía
        if (!gameObject.activeInHierarchy) return;

        PlayAnimation();
    }
    public void PlayAnimation()
    {
        if (!hasData) return;

        StopAllCoroutines();
        StartCoroutine(AnimateToResult(lastData.hand, lastData.wrist, lastData.forearm));
    }
    public void SetTextResult((float hand, float wrist, float forearm) data, float activeValue, float inactiveValue, bool showActivity)
    {
        int digits = 1;
        handResult.text = "Mano: " + Math.Round(data.hand * 100f, digits).ToString() + "%";
        wristResult.text = "Muñeca: " + Math.Round(data.wrist * 100, digits).ToString() + "%";
        foreArmResult.text = "Antebrazo: " + Math.Round(data.forearm * 100f, digits).ToString() + "%";

        if (!showActivity) return;
        activity.text = "Actividad: " + Math.Round(activeValue * 100f, digits).ToString() + "%";
        inactivity.text = "Inactividad: " + Math.Round(inactiveValue * 100f, digits).ToString() + "%";

    }

    IEnumerator AnimateToResult(float h, float w, float f)
    {
        float t = 0f;
        float duration = 0.5f;

        Color startHand = handImage.color;
        Color startWrist = wristImage.color;
        Color startForearm = foreArmImage.color;

        Color targetHand = Color.Lerp(defaultColor, paintColor, h);
        Color targetWrist = Color.Lerp(defaultColor, paintColor, w);
        Color targetForearm = Color.Lerp(defaultColor, paintColor, f);

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;

            handImage.color = Color.Lerp(startHand, targetHand, lerp);
            wristImage.color = Color.Lerp(startWrist, targetWrist, lerp);
            foreArmImage.color = Color.Lerp(startForearm, targetForearm, lerp);

            yield return null;
        }

        handImage.color = targetHand;
        wristImage.color = targetWrist;
        foreArmImage.color = targetForearm;
    }

    public void LogAverageIntensity((float hand, float wrist, float forearm) data)
{
    Debug.Log(
        $"[Average Intensity]\n" +
        $"Mano: {Mathf.Round(data.hand * 100f)}%\n" +
        $"Muñeca: {Mathf.Round(data.wrist * 100f)}%\n" +
        $"Antebrazo: {Mathf.Round(data.forearm * 100f)}%"
    );
}
}