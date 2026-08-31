using System;
using UnityEngine;

[CreateAssetMenu(fileName = "HybridExerciseProfile", menuName = "Ergonomic Detection/Hybrid Exercise Profile")]
public sealed class HybridExerciseProfile : ScriptableObject
{
    [SerializeField] private ExerciseType exerciseType;
    [SerializeField, Min(1f)] private float expectedDurationSeconds = 90f;
    [SerializeField] private ErgonomicCalibrationProfile calibrationProfile;
    [SerializeField] private HybridSuggestionProfile runtimeProfile;
    [SerializeField] private bool coordinationEnabled;
    [SerializeField] private HybridHandUsageGoal leftHand = new HybridHandUsageGoal();
    [SerializeField] private HybridHandUsageGoal rightHand = new HybridHandUsageGoal();
    [Tooltip("Tiempo angular válido mínimo por dimensión para interpretar coordinación. Cero exposición sin observación no equivale a postura neutra.")]
    [SerializeField, Min(0.1f)] private float minimumFinalObservationSeconds = 2f;
    [SerializeField, Range(0f, 1f)] private float minimumActivityRatio = 0.05f;
    [Tooltip("Segundos para Insert/OSU; proporción de fallos para DuckHunter. Calibración del prototipo.")]
    [SerializeField, Min(0f)] private float goodPerformanceThreshold = 60f;
    [SerializeField, Min(0f)] private float intermediatePerformanceThreshold = 120f;

    public ExerciseType ExerciseType => exerciseType;
    public float ExpectedDurationSeconds => expectedDurationSeconds;
    public ErgonomicCalibrationProfile CalibrationProfile => calibrationProfile;
    public HybridSuggestionProfile RuntimeProfile => runtimeProfile;
    public bool CoordinationEnabled => coordinationEnabled;
    public float MinimumFinalObservationSeconds => minimumFinalObservationSeconds;
    public float MinimumActivityRatio => minimumActivityRatio;
    public float GoodPerformanceThreshold => goodPerformanceThreshold;
    public float IntermediatePerformanceThreshold => intermediatePerformanceThreshold;
    public HybridHandUsageGoal LeftHand => leftHand;
    public HybridHandUsageGoal RightHand => rightHand;

    public bool TryValidate(out string error)
    {
        if (!Enum.IsDefined(typeof(ExerciseType), exerciseType) ||
            !Positive(expectedDurationSeconds) || !Positive(minimumFinalObservationSeconds) ||
            minimumFinalObservationSeconds > expectedDurationSeconds ||
            !Unit(minimumActivityRatio) || !Finite(goodPerformanceThreshold) || goodPerformanceThreshold < 0f ||
            !Finite(intermediatePerformanceThreshold) || intermediatePerformanceThreshold <= goodPerformanceThreshold ||
            (exerciseType == ExerciseType.DuckHunter && intermediatePerformanceThreshold > 1f))
        {
            error = "Duración, observación, actividad o umbrales de desempeño inválidos.";
            return false;
        }
        if (calibrationProfile == null || !calibrationProfile.TryValidate(out _) ||
            runtimeProfile == null || !runtimeProfile.TryValidate(out _) ||
            leftHand == null || !leftHand.IsValid || rightHand == null || !rightHand.IsValid)
        {
            error = "Falta calibración, configuración runtime u objetivos válidos por mano.";
            return false;
        }
        error = null;
        return true;
    }

    private void OnValidate()
    {
        // Un asset recién creado todavía necesita sus referencias en Inspector.
        if (calibrationProfile != null && runtimeProfile != null && !TryValidate(out string error))
            Debug.LogError($"[HybridFinalSuggestions] {error}", this);
    }

    internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    internal static bool Unit(float value) => Finite(value) && value >= 0f && value <= 1f;
    private static bool Positive(float value) => Finite(value) && value > 0f;
}

[Serializable]
public sealed class HybridHandUsageGoal
{
    [Tooltip("Uso relativo final sobre mano + muñeca + antebrazo. No es la contribución de señal rotacional de runtime.")]
    [SerializeField, Range(0f, 1f)] private float wristTarget = 0.4f;
    [SerializeField, Range(0f, 1f)] private float forearmTarget = 0.2f;
    [SerializeField, Range(0f, 1f)] private float wristTolerance = 0.05f;
    [SerializeField, Range(0f, 1f)] private float forearmTolerance = 0.05f;
    [Tooltip("Objetivo explícito para sugerencias en vivo; independiente del reparto final de actividad.")]
    [SerializeField] private HybridCoordinationGoal runtimeGoal = HybridCoordinationGoal.ObserveOnly;

    public float WristTarget => wristTarget;
    public float ForearmTarget => forearmTarget;
    public float WristTolerance => wristTolerance;
    public float ForearmTolerance => forearmTolerance;
    public HybridCoordinationGoal RuntimeGoal => runtimeGoal;
    public bool IsValid => HybridExerciseProfile.Unit(wristTarget) && HybridExerciseProfile.Unit(forearmTarget) &&
        wristTarget + forearmTarget <= 1f && HybridExerciseProfile.Unit(wristTolerance) && wristTolerance > 0f &&
        HybridExerciseProfile.Unit(forearmTolerance) && forearmTolerance > 0f &&
        Enum.IsDefined(typeof(HybridCoordinationGoal), runtimeGoal);
}
