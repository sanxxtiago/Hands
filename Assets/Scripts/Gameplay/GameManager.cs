using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Transition transition;
    public MetricsTrackingSystem trackingSystem;
    [SerializeField] private ExerciseType exerciseType;
    public GAMESTATE currentState;
    public CountdownUI countdown;
    public ArmRuntimeUI left;
    public ArmRuntimeUI right;

    public static event Action OnCountdownStart;
    public static event Action OnExcerciseStart;
    //Pasa la duración del ejercicio al finalizarlo
    public static event Action<float> OnExerciseEnd;
    public static event Action OnShowResults;
    private void OnEnable()
    {
        //transition.OnFadeOutCompleted += StartCountdown;
        MetricsTrackingSystem.OnTrackingStop += SaveExerciseSummary;
        DemoManager.OnDemoClosed += StartCountdown;
        CountdownUI.OnCountdownFinished += OnCountdownFinished;
    }

    private void OnDisable()
    {
        //transition.OnFadeOutCompleted -= StartCountdown;
        MetricsTrackingSystem.OnTrackingStop -= SaveExerciseSummary;
        DemoManager.OnDemoClosed -= StartCountdown;
        CountdownUI.OnCountdownFinished -= OnCountdownFinished;
    }

    void Update()
    {
        if (!trackingSystem.isTracking) return;

        left.SetData(trackingSystem.leftTracker.GetRuntimeSnapshot());
        right.SetData(trackingSystem.rightTracker.GetRuntimeSnapshot());
    }



    private void StartCountdown()
    {
        SetState(GAMESTATE.COUNTDOWN);
    }
    void OnCountdownFinished()
    {
        SetState(GAMESTATE.PLAYING);
    }
    public void SetState(GAMESTATE newState)
    {
        currentState = newState;

        switch (newState)
        {
            case GAMESTATE.COUNTDOWN:
                OnCountdownStart?.Invoke();
                break;

            case GAMESTATE.PLAYING:
                OnExcerciseStart?.Invoke();
                break;

            case GAMESTATE.RESULTS:
                OnShowResults?.Invoke();
                break;
        }
    }

    public void EndExercise(float duration)
    {
        OnExerciseEnd?.Invoke(duration);
        SetState(GAMESTATE.RESULTS);
    }

    private void SaveExerciseSummary(HandUsageSummary leftSummary, HandUsageSummary rightSummary)
    {
        ExerciseSummary summary = new()
        {
            exerciseType = exerciseType,
            leftHand = leftSummary,
            rightHand = rightSummary
        };
        SessionManager.Instance.AddExerciseSummary(summary);
    }

}