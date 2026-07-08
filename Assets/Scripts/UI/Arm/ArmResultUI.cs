using System.Collections;
using UnityEngine;

public class ArmResultUI : ArmUI
{
    private HandUsageSummary _summary;
    private float[] _values;
    private bool hasData = false;

    public void Paint(HandUsageSummary summary, float[] values)
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

        //Asegurar rango válido 0-1
        h = Mathf.Clamp01(h);
        w = Mathf.Clamp01(w);
        f = Mathf.Clamp01(f);
        //Darle más fuerza a los colores
        h = Mathf.Pow(h, 1.3f);
        w = Mathf.Pow(w, 1.3f);
        f = Mathf.Pow(f, 1.3f);

        float t = 0f;
        float duration = 0.5f;

        Color startHand = handImage.color;
        Color startWrist = wristImage.color;
        Color startForearm = foreArmImage.color;

        Color targetHand = usageGradient.Evaluate(h);
        Color targetWrist = usageGradient.Evaluate(w);
        Color targetForearm = usageGradient.Evaluate(f);

        while (t < duration)
        {
            t += Time.deltaTime;
            //float lerp = t / duration;
            float lerp = Mathf.SmoothStep(0f, 1f, t / duration);
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