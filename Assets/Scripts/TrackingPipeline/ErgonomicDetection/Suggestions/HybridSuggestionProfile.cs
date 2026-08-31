using UnityEngine;

[CreateAssetMenu(fileName = "HybridSuggestionProfile",
    menuName = "Ergonomic Detection/Hybrid Suggestion Profile")]
public sealed class HybridSuggestionProfile : ScriptableObject
{
    [Tooltip("Ventana reciente para participación motora; calibración propia, no RULA.")]
    [SerializeField, Min(0.1f)] private float usageWindowSeconds = 5f;
    [SerializeField, Min(0.1f)] private float minimumObservationSeconds = 2f;
    [Tooltip("Media mínima de las señales normalizadas de muñeca y antebrazo. No es intensidad clínica.")]
    [SerializeField, Range(0.001f, 2f)] private float minimumRotationSignal = 0.01f;
    [SerializeField, Range(0f, 1f)] private float lowContribution = 0.35f;
    [SerializeField, Range(0f, 1f)] private float highContribution = 0.65f;
    [Tooltip("Tiempo válido de la condición antes de proponer redistribución. No es un umbral RULA.")]
    [SerializeField, Min(0.1f)] private float coordinationHoldSeconds = 2f;
    [SerializeField, Min(0.1f)] private float warmupSeconds = 2f;
    [SerializeField, Min(0f)] private float cooldownSeconds = 8f;
    [SerializeField, Min(1)] private int maximumSuggestionsPerExercise = 3;
    [SerializeField, Min(0.1f)] private float snackbarSeconds = 3f;

    public float UsageWindowSeconds => usageWindowSeconds;
    public float MinimumObservationSeconds => minimumObservationSeconds;
    public float MinimumRotationSignal => minimumRotationSignal;
    public float LowContribution => lowContribution;
    public float HighContribution => highContribution;
    public float CoordinationHoldSeconds => coordinationHoldSeconds;
    public float WarmupSeconds => warmupSeconds;
    public float CooldownSeconds => cooldownSeconds;
    public int MaximumSuggestionsPerExercise => maximumSuggestionsPerExercise;
    public float SnackbarSeconds => snackbarSeconds;

    public bool TryValidate(out string error)
    {
        if (!Positive(usageWindowSeconds) || !Positive(minimumObservationSeconds) ||
            minimumObservationSeconds > usageWindowSeconds ||
            !Positive(minimumRotationSignal) || minimumRotationSignal > 2f ||
            !Positive(coordinationHoldSeconds) || !Positive(warmupSeconds) ||
            !Finite(cooldownSeconds) || cooldownSeconds < 0f ||
            !Positive(snackbarSeconds) || maximumSuggestionsPerExercise < 1)
        {
            error = "Tiempos, ventana o señal mínima inválidos.";
            return false;
        }

        if (!Finite(lowContribution) || !Finite(highContribution) ||
            lowContribution < 0f || highContribution > 1f ||
            lowContribution >= highContribution)
        {
            error = "Los límites de participación deben cumplir 0 <= bajo < alto <= 1.";
            return false;
        }

        error = null;
        return true;
    }

    private void OnValidate()
    {
        if (!TryValidate(out string error))
            Debug.LogError($"[HybridSuggestions] {error}", this);
    }

    private static bool Positive(float value) => Finite(value) && value > 0f;
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
