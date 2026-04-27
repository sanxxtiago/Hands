using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public MetricsTrackingSystem trackingSystem;
    public GAMESTATE currentState;
    public CountdownUI countdown;
    public ArmRuntimeUI left;
    public ArmRuntimeUI right;

    public Exercise currentExercise;
    public static event Action<Exercise> OnSetExercise;
    public static event Action OnCountdownStart;
    public static event Action OnGameStart;
    public static event Action OnShowResults;
    public static event Action<float> OnGameEnd;
    //private float exerciseDuration = 0;
    private void OnEnable()
    {
        CountdownUI.OnCountdownFinished += HandleCountdownFinished;
    }

    private void OnDisable()
    {
        CountdownUI.OnCountdownFinished -= HandleCountdownFinished;
    }

    void Start()
    {
        OnSetExercise?.Invoke(currentExercise);
    }

    void Update()
    {
        if (!trackingSystem.isTracking) return;

        left.SetData(trackingSystem.leftTracker.GetRuntimeSnapshot());
        right.SetData(trackingSystem.rightTracker.GetRuntimeSnapshot());
    }

    void HandleCountdownFinished()
    {
        SetState(GAMESTATE.PLAYING);
    }

    public void StartCountdown()
    {
        SetState(GAMESTATE.COUNTDOWN);
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
                OnGameStart?.Invoke();
                break;

            case GAMESTATE.RESULTS:
                OnShowResults?.Invoke();
                break;
        }
    }

    public void EndExercise(float duration)
    {
        OnGameEnd?.Invoke(duration);
        SetState(GAMESTATE.RESULTS);
    }

    public void NextExercise()
    {
        Exercise next = Exercise.Osu;
        OnSetExercise?.Invoke(next);
        SetState(GAMESTATE.COUNTDOWN);
    }

}