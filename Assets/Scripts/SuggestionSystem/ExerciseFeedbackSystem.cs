
using UnityEngine;

public class ExerciseFeedbackSystem : MonoBehaviour
{
    public ExerciseProfile profile;
    public MetricsTrackingSystem trackingSystem;

    private SuggestionEngine leftEngine;
    private SuggestionEngine rightEngine;

    private float warmupTime = 2f;
    void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        leftEngine = BuildEngine(profile.leftHand);
        rightEngine = BuildEngine(profile.rightHand);
    }

    SuggestionEngine BuildEngine(HandProfile handProfile)
    {
        var engine = new SuggestionEngine();

        engine.AddRule(new TimedRule(
            new LowActivityRule(handProfile.minActivity), 2f, 5f));

        engine.AddRule(new TimedRule(
            new ExcessWristRule(handProfile.wrist.tolerance), 1.5f, 5f));

        engine.AddRule(new TimedRule(
            new LowForearmRule(handProfile.forearm.tolerance), 1.5f, 5f));

        engine.AddRule(new TimedRule(
            new ExcessHandRule(handProfile.hand.tolerance), 2f, 5f));

        return engine;
    }

    public void Evaluate(float elapsedTime, float dt)
    {
        
        if (elapsedTime < warmupTime) return;

        EvaluateHand(trackingSystem.leftTracker, leftEngine, elapsedTime, dt);
        EvaluateHand(trackingSystem.rightTracker, rightEngine, elapsedTime, dt);
    }

    void EvaluateHand(
        ExerciseMetricsTracker tracker,
        SuggestionEngine engine,
        float elapsedTime,
        float dt)
    {
        var handProfile = GetProfile(tracker.HandType);
        if (!handProfile.isActive)
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

        if (suggestion != null)
        {
            Debug.Log($"[{tracker.HandType}] {suggestion.message}");
        }
    }

    HandProfile GetProfile(HandType hand)
    {
        return hand == HandType.LEFT
            ? profile.leftHand
            : profile.rightHand;
    }
}