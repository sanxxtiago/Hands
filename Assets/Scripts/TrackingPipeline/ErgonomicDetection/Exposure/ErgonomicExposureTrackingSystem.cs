using UnityEngine;

public sealed class ErgonomicExposureTrackingSystem : MonoBehaviour
{
    [SerializeField] private ErgonomicCalibrationProfile calibrationProfile;

    private ErgonomicExposureInterpreter _leftInterpreter;
    private ErgonomicExposureInterpreter _rightInterpreter;
    private bool _isTracking;

    public bool IsTracking => _isTracking;

    private void Awake()
    {
        CreateInterpreters();
    }

    private void OnEnable()
    {
        ErgonomicEventBus.OnFrame += OnErgonomicFrameReceived;
        GameManager.OnExcerciseStart += RunTracking;
        GameManager.OnExerciseEnd += StopTracking;
    }

    private void OnDisable()
    {
        _isTracking = false;
        ErgonomicEventBus.OnFrame -= OnErgonomicFrameReceived;
        GameManager.OnExcerciseStart -= RunTracking;
        GameManager.OnExerciseEnd -= StopTracking;
    }

    public void RunTracking()
    {
        _isTracking = false;
        CreateInterpreters();
        if (!EnsureInterpreters())
            return;

        _leftInterpreter.Reset();
        _rightInterpreter.Reset();
        _isTracking = true;
    }

    public void StopTracking(float duration)
    {
        if (!_isTracking)
            return;

        _isTracking = false;
        HandErgonomicExposureSummary leftSummary = _leftInterpreter.GetSummary();
        HandErgonomicExposureSummary rightSummary = _rightInterpreter.GetSummary();

        LogTrackingSummary(duration, leftSummary, rightSummary);
        ErgonomicExposureEventBus.PublishTrackingStop(
            duration,
            leftSummary,
            rightSummary);
    }

    private void OnErgonomicFrameReceived(FrameErgonomicData frame)
    {
        if (!_isTracking)
            return;

        ErgonomicExposureInterpreter interpreter = frame.handType == HandType.LEFT
            ? _leftInterpreter
            : _rightInterpreter;

        if (interpreter.TryProcess(frame, out FrameErgonomicExposureData exposureFrame))
            ErgonomicExposureEventBus.Publish(exposureFrame);
    }

    private void CreateInterpreters()
    {
        _leftInterpreter = new ErgonomicExposureInterpreter(
            HandType.LEFT,
            calibrationProfile);
        _rightInterpreter = new ErgonomicExposureInterpreter(
            HandType.RIGHT,
            calibrationProfile);
    }

    private bool EnsureInterpreters()
    {
        if (_leftInterpreter != null && _rightInterpreter != null &&
            _leftInterpreter.IsConfigurationValid &&
            _rightInterpreter.IsConfigurationValid)
        {
            return true;
        }

        CreateInterpreters();

        if (_leftInterpreter.IsConfigurationValid &&
            _rightInterpreter.IsConfigurationValid)
        {
            return true;
        }

        Debug.LogError(
            "[ErgonomicExposure] Falta un perfil de calibracion valido.",
            this);
        return false;
    }

    private static void LogTrackingSummary(
        float duration,
        HandErgonomicExposureSummary leftSummary,
        HandErgonomicExposureSummary rightSummary)
    {
        Debug.Log(
            "[ErgonomicExposure] Fin de ejercicio\n" +
            $"Duracion: {duration:F2}s\n" +
            FormatHandSummary(leftSummary) + "\n" +
            FormatHandSummary(rightSummary));
    }

    private static string FormatHandSummary(HandErgonomicExposureSummary summary)
    {
        return $"{summary.handType}\n" +
            FormatDimensionSummary(
                "  Flexion/extension",
                summary.wristFlexionExtension) + "\n" +
            FormatDimensionSummary(
                "  Desviacion radial/cubital",
                summary.wristRadialUlnarDeviation) + "\n" +
            FormatDimensionSummary(
                "  Pronacion/supinacion",
                summary.wristPronationSupination);
    }

    private static string FormatDimensionSummary(
        string label,
        ErgonomicExposureDimensionSummary summary)
    {
        return $"{label} | acumulada: {summary.cumulativeExposureSeconds:F2}s" +
            $" | continua: {summary.sustainedExposureSeconds:F2}s" +
            $" | continua máxima: {summary.maximumSustainedExposureSeconds:F2}s" +
            $" | observación válida: {summary.validObservationSeconds:F2}s" +
            $" | alerta acumulada: {FormatFlag(summary.hasReachedCumulativeExposureAlert)}" +
            $" | alerta sostenida: {FormatFlag(summary.hasReachedSustainedExposureThreshold)}";
    }

    private static string FormatFlag(bool value)
    {
        return value ? "si" : "no";
    }
}
