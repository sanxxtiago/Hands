
using UnityEngine;
using System.Collections.Generic;

public class ExerciseFeedbackSystem : MonoBehaviour
{
    public ExerciseProfile profile;
    public MetricsTrackingSystem trackingSystem;

    private SuggestionEngine leftEngine;
    private SuggestionEngine rightEngine;

    [SerializeField] private float warmupTime = 2f;
    [SerializeField, Min(1)] private int maxSuggestionsPerExercise = 3;
    [SerializeField, Min(0f)] private float suggestionCooldown = 8f;

    private int suggestionsEmitted;
    private float cooldownRemaining;
    private readonly HashSet<string> emittedMessages = new();

    void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (profile == null)
        {
            Debug.LogWarning("SuggestionSystem: no hay ExerciseProfile asignado.");
            leftEngine = null;
            rightEngine = null;
            return;
        }

        leftEngine = BuildEngine(profile.leftHand);
        rightEngine = BuildEngine(profile.rightHand);
    }

    public void BeginExercise()
    {
        suggestionsEmitted = 0;
        cooldownRemaining = 0f;
        emittedMessages.Clear();
        Initialize();
    }

    SuggestionEngine BuildEngine(HandProfile handProfile)
    {
        if (handProfile == null)
            return null;

        var engine = new SuggestionEngine();

        engine.AddRule(new TimedRule(
            new LowActivityRule(handProfile.minActivity), 2f, 5f));

        engine.AddRule(new TimedRule(
            new ExcessWristRule(), 1.5f, 5f));

        engine.AddRule(new TimedRule(
            new LowForearmRule(), 1.5f, 5f));

        engine.AddRule(new TimedRule(
            new ExcessHandRule(), 2f, 5f));

        return engine;
    }

    public void Evaluate(float elapsedTime, float dt)
    {
        if (elapsedTime < warmupTime) return;
        if (trackingSystem == null || profile == null) return;
        if (suggestionsEmitted >= maxSuggestionsPerExercise) return;

        cooldownRemaining = Mathf.Max(0f, cooldownRemaining - dt);
        bool canEmit = cooldownRemaining <= 0f;

        Suggestion bestSuggestion = null;
        HandType bestHand = HandType.NONE;

        EvaluateHand(trackingSystem.leftTracker, leftEngine, elapsedTime, dt,
            ref bestSuggestion, ref bestHand);
        EvaluateHand(trackingSystem.rightTracker, rightEngine, elapsedTime, dt,
            ref bestSuggestion, ref bestHand);

        if (!canEmit) return;
        if (bestSuggestion == null || string.IsNullOrWhiteSpace(bestSuggestion.message))
            return;

        if (!emittedMessages.Add(bestSuggestion.message))
            return;

        suggestionsEmitted++;
        cooldownRemaining = suggestionCooldown;
        Debug.Log($"[SuggestionSystem] [{bestHand}] {bestSuggestion.message}");
    }

    void EvaluateHand(
        ExerciseMetricsTracker tracker,
        SuggestionEngine engine,
        float elapsedTime,
        float dt,
        ref Suggestion bestSuggestion,
        ref HandType bestHand)
    {
        var handProfile = GetProfile(tracker.HandType);
        if (engine == null || handProfile == null || !handProfile.isActive)
            return;

        var snapshot = tracker.GetRuntimeSnapshot();

        var normalized = MetricsProcessor.Normalize(snapshot);
        var deviation = MetricsProcessor.GetDeviation(normalized, handProfile);

        var context = new AnalysisContext
        {
            deviation = deviation,
            activityRatio = tracker.GetActivityRatio(elapsedTime)
        };

        var suggestion = engine.Evaluate(context, dt);
        if (suggestion != null &&
            (bestSuggestion == null || suggestion.severity > bestSuggestion.severity))
        {
            bestSuggestion = suggestion;
            bestHand = tracker.HandType;
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
