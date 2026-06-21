using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEditor.VersionControl;
public class StepByStepUI : MonoBehaviour
{
    [SerializeField] private OrientationPhase1Manager phase1;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text progressionText;
    [SerializeField] private TMP_Text instructionText;
    void OnEnable()
    {
        phase1.OnProgressChanged += UpdateProgressBar;
        phase1.OnExplorationCompleted += UpdateMessage;
    }

    void OnDisable()
    {
        phase1.OnProgressChanged -= UpdateProgressBar;
        phase1.OnExplorationCompleted -= UpdateMessage;

    }
    public void UpdateProgressBar(float _activeTime, float requiredActiveTime)
    {
        float progress = Mathf.Clamp01(_activeTime / requiredActiveTime);
        progressionText.text = $"{Math.Round(progress, 2) * 100}%";
        progressBar.value = progress;
    }

    public void UpdateMessage()
    {
        instructionText.text = "Cierra ambos puños para continuar";
    }
}
