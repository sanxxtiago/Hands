using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class ArmResultUI : ArmColor
{
    private ExerciseSummary _summary;
    private float[] _values;
    private bool hasData = false;

    public void Paint(ExerciseSummary summary, float[] values)
    {
        _summary = summary;
        _values = values;
        hasData = true;

        if (!gameObject.activeInHierarchy) return;

        PlayAnimation();
    }

    public void PlayAnimation()
    {
        if (!hasData) return;

        StopAllCoroutines();
        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        float h = 0f, w = 0f, f = 0f;

        for (int i = 0; i < _summary.zones.Length; i++)
        {
            switch (_summary.zones[i])
            {
                case MotionZone.Hand: h = _values[i]; break;
                case MotionZone.Wrist: w = _values[i]; break;
                case MotionZone.Forearm: f = _values[i]; break;
            }
        }

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
}