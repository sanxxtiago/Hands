using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public MetricsTrackingSystem trackingSystem;
    public GAMESTATE currentState;
    public CountdownUI countdown;
    public ResultsUI resultsUI;
    public ArmRuntimeUI left;
    public ArmRuntimeUI right;

    public Exercise currentExercise;
    public event Action OnCountdownStart;
    public event Action<Exercise> OnGameStart;
    public event Action OnGameEnd;

    private void OnEnable()
    {
        countdown.OnCountdownFinished += HandleCountdownFinished;
    }

    private void OnDisable()
    {
        countdown.OnCountdownFinished -= HandleCountdownFinished;
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
                trackingSystem.RunTracking();
                OnGameStart?.Invoke(currentExercise);
                break;

            case GAMESTATE.RESULTS:
                Debug.Log("RESULTSSSSS");
                //OnResults?.Invoke();
                trackingSystem.StopTracking();
                ShowResults();
                OnGameEnd?.Invoke();
                break;
        }
    }

    public void EndExercise()
    {
        SetState(GAMESTATE.RESULTS);
    }

    protected void ShowResults()
    {
        resultsUI.Display(trackingSystem.GetLeftSummary(5f), trackingSystem.GetRightSummary(5f));
    }
}