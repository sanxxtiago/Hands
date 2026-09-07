using UnityEngine;
using System.Collections.Generic;

public class ExerciseFeedbackSystem : MonoBehaviour
{
    public ExerciseProfile profile;
    public MetricsTrackingSystem trackingSystem;

    private SuggestionEngine leftEngine;
    private SuggestionEngine rightEngine;
    private TimedRule leftLowActivityRule;
    private TimedRule rightLowActivityRule;

    [SerializeField] private float warmupTime = 2f;
    [SerializeField, Min(1)] private int maxSuggestionsPerExercise = 3;
    [SerializeField, Min(0f)] private float suggestionCooldown = 8f;
    [SerializeField, Min(0.5f)] private float snackbarDuration = 3f;

    [Tooltip("Minima actividad de una mano para evaluar sus reglas de zona.")]
    [SerializeField, Min(0f)]
    private float zoneEvalMinActivity = 0.05f;

    [Tooltip("Tiempo tras el cual una misma sugerencia (regla + mano) puede repetirse.")]
    [SerializeField, Min(0f)]
    private float repeatSuggestionAfter = 30f;

    private int suggestionsEmitted;
    private float cooldownRemaining;
    private readonly Dictionary<string, float> lastEmittedElapsed = new();

    void Start()
    {
        Initialize();
    }

    public void BeginExercise()
    {
        suggestionsEmitted = 0;
        cooldownRemaining = 0f;
        lastEmittedElapsed.Clear();
        Initialize();
    }

    public void Initialize()
    {
        if (profile == null)
        {
            Debug.LogWarning("SuggestionSystem: no hay ExerciseProfile asignado.");
            leftEngine = null;
            rightEngine = null;
            leftLowActivityRule = null;
            rightLowActivityRule = null;
            return;
        }

        leftEngine = BuildEngine(profile.leftHand);
        rightEngine = BuildEngine(profile.rightHand);
        leftLowActivityRule = BuildLowActivityRule(profile.leftHand);
        rightLowActivityRule = BuildLowActivityRule(profile.rightHand);
    }

    SuggestionEngine BuildEngine(HandProfile handProfile)
    {
        if (handProfile == null)
            return null;

        var engine = new SuggestionEngine();

        engine.AddRule(new TimedRule(
            new ExcessWristRule(handProfile.wristCriticality), 1.5f, 5f));

        engine.AddRule(new TimedRule(
            new LowForearmRule(handProfile.forearmCriticality), 1.5f, 5f));

        engine.AddRule(new TimedRule(
            new ExcessHandRule(handProfile.handCriticality), 2f, 5f));

        return engine;
    }

    TimedRule BuildLowActivityRule(HandProfile handProfile)
    {
        if (handProfile == null)
            return null;

        return new TimedRule(
            new LowActivityRule(handProfile.minActivity), 2f, 5f);
    }

    public void Evaluate(float elapsedTime, float dt)
    {
        if (elapsedTime < warmupTime) return;
        if (trackingSystem == null || profile == null) return;
        if (suggestionsEmitted >= maxSuggestionsPerExercise) return;

        cooldownRemaining = Mathf.Max(0f, cooldownRemaining - dt);

        // En cooldown global los engines se congelan: evita que los TimedRule
        // consuman su disparo interno mientras la emisión está bloqueada.
        if (cooldownRemaining > 0f) return;

        Suggestion best = null;
        HandType bestHand = HandType.NONE;
        float bestScore = 0f;

        EvaluateHand(trackingSystem.leftTracker, leftEngine, elapsedTime, dt,
            ref best, ref bestHand, ref bestScore);
        EvaluateHand(trackingSystem.rightTracker, rightEngine, elapsedTime, dt,
            ref best, ref bestHand, ref bestScore);
        EvaluateLeadingLowActivity(elapsedTime, dt,
            ref best, ref bestHand, ref bestScore);

        if (best == null || string.IsNullOrWhiteSpace(best.message)) return;

        // Dedup por regla + mano con re-arme temporal: la misma sugerencia
        // puede repetirse si la conducta persiste, sin agotar el presupuesto.
        string repeatKey = best.message + "|" + bestHand;
        if (lastEmittedElapsed.TryGetValue(repeatKey, out float lastElapsed)
            && elapsedTime - lastElapsed < repeatSuggestionAfter)
            return;

        lastEmittedElapsed[repeatKey] = elapsedTime;
        suggestionsEmitted++;
        cooldownRemaining = suggestionCooldown;

        string handPrefix = HandPrefix(bestHand);

        Debug.Log($"[SuggestionSystem] {handPrefix}{best.message}");
        SnackbarManager.Show(
            SNACKBARTYPE.WARNING,
            $"{handPrefix}{best.message}",
            snackbarDuration);
    }

    static string HandPrefix(HandType hand)
    {
        switch (hand)
        {
            case HandType.LEFT: return "Mano izquierda: ";
            case HandType.RIGHT: return "Mano derecha: ";
            default: return string.Empty;
        }
    }

    static float ScoreFor(float severity, float activityRatio)
    {
        return severity * (0.25f + 0.75f * activityRatio);
    }

    void EvaluateHand(
        ExerciseMetricsTracker tracker,
        SuggestionEngine engine,
        float elapsedTime,
        float dt,
        ref Suggestion best,
        ref HandType bestHand,
        ref float bestScore)
    {
        if (engine == null || tracker == null) return;

        var handProfile = GetProfile(tracker.HandType);
        if (handProfile == null) return;

        float activity = tracker.GetActivityRatio(elapsedTime);

        // Mano casi inmóvil: su señal de zonas no es significativa.
        // Se resetea el engine para no disparar con trigger acumulado previo.
        if (activity < zoneEvalMinActivity)
        {
            engine.Reset();
            return;
        }

        var snapshot = tracker.GetRuntimeSnapshot();
        var normalized = MetricsProcessor.Normalize(snapshot);
        var deviation = MetricsProcessor.GetDeviation(normalized, handProfile);

        var context = new AnalysisContext
        {
            deviation = deviation,
            activityRatio = activity
        };

        var suggestion = engine.Evaluate(context, dt);
        if (suggestion == null) return;

        // Prioriza la mano con más actividad/movimiento
        float score = ScoreFor(suggestion.severity, activity);

        if (best == null || score > bestScore)
        {
            best = suggestion;
            bestHand = tracker.HandType;
            bestScore = score;
        }
    }

    void EvaluateLeadingLowActivity(
        float elapsedTime,
        float dt,
        ref Suggestion best,
        ref HandType bestHand,
        ref float bestScore)
    {
        if (leftLowActivityRule == null && rightLowActivityRule == null) return;
        if (trackingSystem == null) return;

        // Mano con más actividad: si ni siquiera ella alcanza el mínimo,
        // el usuario no está ejecutando el movimiento esperado
        ExerciseMetricsTracker leading = trackingSystem.leftTracker;
        float leadingActivity = trackingSystem.leftTracker.GetActivityRatio(elapsedTime);

        float rightActivity = trackingSystem.rightTracker.GetActivityRatio(elapsedTime);
        if (rightActivity > leadingActivity)
        {
            leading = trackingSystem.rightTracker;
            leadingActivity = rightActivity;
        }

        TimedRule rule = leading.HandType == HandType.LEFT
            ? leftLowActivityRule
            : rightLowActivityRule;

        if (rule == null) return;

        var context = new AnalysisContext
        {
            deviation = new Deviation(),
            activityRatio = leadingActivity
        };

        var suggestion = rule.Update(context, dt);
        if (suggestion == null) return;

        // Misma escala que las reglas de zona; sin etiqueta de mano porque
        // es un estado global (la mano líder es la que MÁS se mueve).
        float score = ScoreFor(suggestion.severity, leadingActivity);
        if (best == null || score > bestScore)
        {
            best = suggestion;
            bestHand = HandType.NONE;
            bestScore = score;
        }
    }

    HandProfile GetProfile(HandType hand)
    {
        if (profile == null)
            return null;

        return hand == HandType.LEFT
            ? profile.leftHand
            : profile.rightHand;
    }
}