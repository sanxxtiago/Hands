using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public GAMESTATE currentState;
    public CountdownUI countdown;
    public event Action OnCountdownStart;
    public event Action OnGameStart;
    public event Action OnGameEnd;
    public event Action OnResults;

    private void OnEnable()
    {
        countdown.OnCountdownFinished += HandleCountdownFinished;
    }

    private void OnDisable()
    {
        countdown.OnCountdownFinished -= HandleCountdownFinished;
    }

    void HandleCountdownFinished()
    {
        SetState(GAMESTATE.PLAYING);
    }

    public void StartExercise()
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
                OnResults?.Invoke();
                break;
        }
    }

    public void EndExercise()
    {
        SetState(GAMESTATE.RESULTS);
        OnGameEnd?.Invoke();
    }
}