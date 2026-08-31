using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ExpositionRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text dimensionText;
    [SerializeField] private ExpositionStatusIcon statusIcon;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private Image outline;

    public void SetData(string dimensionName, bool hasAlert, float cumulativeExposureSeconds, Color handColor)
    {
        if (dimensionText != null)
            dimensionText.text = dimensionName;
        if (statusIcon != null)
            statusIcon.SetWarning(hasAlert);
        if (statusText != null)
        {
            statusText.text = hasAlert ? "Alerta" : "Normal";
            statusText.color = hasAlert ? new Color(1f, 0.75686276f, 0.02745098f, 1f) : new Color(0.13333334f, 0.77254903f, 0.36862746f, 1f);
        }
        if (timeText != null)
        {
            float safeSeconds = float.IsNaN(cumulativeExposureSeconds) || float.IsInfinity(cumulativeExposureSeconds) || cumulativeExposureSeconds < 0f ? 0f : cumulativeExposureSeconds;
            timeText.text = safeSeconds.ToString("F1", CultureInfo.InvariantCulture) + " s";
        }
        if (outline != null)
            outline.color = new Color(handColor.r, handColor.g, handColor.b, 0.9f);
    }
}
