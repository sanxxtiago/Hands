using System;
using UnityEngine;

[Serializable]
public sealed class ErgonomicAngleCalibration
{
    [SerializeField] private ErgonomicPostureDimension dimension;
    [SerializeField] private bool isEnabled = true;
    [SerializeField] private float minimumOptimalDegrees;
    [SerializeField] private float maximumOptimalDegrees;
    [SerializeField, TextArea]
    private string calibrationNote;

    public ErgonomicPostureDimension Dimension => dimension;
    public bool IsEnabled => isEnabled;
    public float MinimumOptimalDegrees => minimumOptimalDegrees;
    public float MaximumOptimalDegrees => maximumOptimalDegrees;
    public string CalibrationNote => calibrationNote;

    public ErgonomicAngleCalibration()
    {
    }

    public ErgonomicAngleCalibration(
        ErgonomicPostureDimension dimension,
        float minimumOptimalDegrees,
        float maximumOptimalDegrees,
        string calibrationNote)
    {
        this.dimension = dimension;
        this.minimumOptimalDegrees = minimumOptimalDegrees;
        this.maximumOptimalDegrees = maximumOptimalDegrees;
        this.calibrationNote = calibrationNote;
    }

    public bool IsWithinOptimalRange(float degrees)
    {
        return degrees >= minimumOptimalDegrees &&
            degrees <= maximumOptimalDegrees;
    }

    public bool TryValidate(out string validationError)
    {
        if (!IsFinite(minimumOptimalDegrees) || !IsFinite(maximumOptimalDegrees))
        {
            validationError = "los limites angulares deben ser finitos";
            return false;
        }

        if (minimumOptimalDegrees > maximumOptimalDegrees)
        {
            validationError = "el limite minimo no puede superar el maximo";
            return false;
        }

        validationError = null;
        return true;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
