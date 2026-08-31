using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ErgonomicCalibrationProfile",
    menuName = "Ergonomic Detection/Calibration Profile")]
public sealed class ErgonomicCalibrationProfile : ScriptableObject
{
    [SerializeField, Min(1)] private int profileVersion = 1;
    [SerializeField, Min(0.01f)]
    private float cumulativeExposureAlertSeconds = 60f;
    [SerializeField, Min(0.01f)]
    private float sustainedExposureThresholdSeconds = 60f;
    [SerializeField, Min(0.01f)]
    private float maximumFrameGapSeconds = 0.25f;
    [SerializeField] private List<ErgonomicAngleCalibration> angleCalibrations =
        new List<ErgonomicAngleCalibration>
        {
            new ErgonomicAngleCalibration(
                ErgonomicPostureDimension.WristFlexionExtension,
                -15f,
                15f,
                "Rango preventivo del prototipo inspirado en RULA; no es oficial."),
            new ErgonomicAngleCalibration(
                ErgonomicPostureDimension.WristRadialUlnarDeviation,
                -15f,
                15f,
                "Rango provisional del prototipo; requiere validacion clinica."),
            new ErgonomicAngleCalibration(
                ErgonomicPostureDimension.WristPronationSupination,
                -45f,
                45f,
                "Rango provisional del prototipo; requiere validacion clinica.")
        };

    public int ProfileVersion => profileVersion;
    public float CumulativeExposureAlertSeconds => cumulativeExposureAlertSeconds;
    public float SustainedExposureThresholdSeconds =>
        sustainedExposureThresholdSeconds;
    public float MaximumFrameGapSeconds => maximumFrameGapSeconds;

    public bool TryGetCalibration(
        ErgonomicPostureDimension dimension,
        out ErgonomicAngleCalibration calibration)
    {
        for (int i = 0; i < angleCalibrations.Count; i++)
        {
            ErgonomicAngleCalibration candidate = angleCalibrations[i];
            if (candidate != null && candidate.Dimension == dimension)
            {
                calibration = candidate;
                return true;
            }
        }

        calibration = null;
        return false;
    }

    public bool TryValidate(out string validationError)
    {
        if (profileVersion <= 0)
        {
            validationError = "profileVersion debe ser mayor que cero";
            return false;
        }

        if (!IsPositiveFinite(cumulativeExposureAlertSeconds) ||
            !IsPositiveFinite(sustainedExposureThresholdSeconds) ||
            !IsPositiveFinite(maximumFrameGapSeconds))
        {
            validationError = "los umbrales temporales deben ser positivos y finitos";
            return false;
        }

        if (angleCalibrations == null || angleCalibrations.Count != 3)
        {
            validationError = "deben existir exactamente tres calibraciones angulares";
            return false;
        }

        HashSet<ErgonomicPostureDimension> dimensions =
            new HashSet<ErgonomicPostureDimension>();

        for (int i = 0; i < angleCalibrations.Count; i++)
        {
            ErgonomicAngleCalibration calibration = angleCalibrations[i];
            if (calibration == null)
            {
                validationError = $"la calibracion angular {i} es nula";
                return false;
            }

            if (!dimensions.Add(calibration.Dimension))
            {
                validationError =
                    $"la dimension {calibration.Dimension} esta duplicada";
                return false;
            }

            if (!calibration.TryValidate(out string angleError))
            {
                validationError = $"{calibration.Dimension}: {angleError}";
                return false;
            }
        }

        foreach (ErgonomicPostureDimension dimension in
            System.Enum.GetValues(typeof(ErgonomicPostureDimension)))
        {
            if (!dimensions.Contains(dimension))
            {
                validationError = $"falta la dimension {dimension}";
                return false;
            }
        }

        validationError = null;
        return true;
    }

    private void OnValidate()
    {
        if (!TryValidate(out string validationError))
        {
            Debug.LogError(
                $"[ErgonomicExposure] Perfil de calibracion invalido: " +
                $"{validationError}.",
                this);
        }
    }

    private static bool IsPositiveFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
    }
}
