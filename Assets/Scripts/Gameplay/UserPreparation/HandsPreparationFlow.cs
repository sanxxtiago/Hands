using System;
using System.Collections;
using UnityEngine;

public sealed class HandsPreparationFlow : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private HandsDetection handsDetection;

    [Header("Countdown")]
    [SerializeField, Min(1)] private int countdownSeconds = 3;
    [SerializeField, Min(0f)] private float readyDisplayDuration = 0.25f;

    private Coroutine preparationRoutine;
    private bool isWaitingForHands;
    private bool completionEmitted;
    private bool isReadyMessageVisible;
    private int currentCountdownStep;

    public event Action OnWaitingForHands;
    public event Action<int> OnCountdownStep;
    public event Action OnPreparationReady;
    public event Action OnPreparationCompleted;

    public bool IsCountdownRunning => preparationRoutine != null;
    public bool IsReadyMessageVisible => isReadyMessageVisible;
    public int CurrentCountdownStep => currentCountdownStep;

    private void Awake()
    {
        if (handsDetection != null)
            return;

        Debug.LogError(
            "[HandsPreparationFlow] Falta asignar HandsDetection.",
            this);
        enabled = false;
    }

    private void OnEnable()
    {
        handsDetection.OnLeftHandDetectionChanged += HandleHandDetectionChanged;
        handsDetection.OnRightHandDetectionChanged += HandleHandDetectionChanged;
        EvaluateReadiness();
    }

    private void OnDisable()
    {
        if (handsDetection != null)
        {
            handsDetection.OnLeftHandDetectionChanged -= HandleHandDetectionChanged;
            handsDetection.OnRightHandDetectionChanged -= HandleHandDetectionChanged;
        }

        StopPreparationRoutine();
        isWaitingForHands = false;
        isReadyMessageVisible = false;
        completionEmitted = false;
        currentCountdownStep = 0;
    }

    private void HandleHandDetectionChanged(bool _)
    {
        EvaluateReadiness();
    }

    private void EvaluateReadiness()
    {
        bool bothHandsDetected = handsDetection.IsLeftDetected && handsDetection.IsRightDetected;

        if (!bothHandsDetected)
        {
            StopPreparationRoutine();

            if (!isWaitingForHands)
            {
                isWaitingForHands = true;
                OnWaitingForHands?.Invoke();
            }

            return;
        }

        isWaitingForHands = false;

        if (completionEmitted || preparationRoutine != null)
            return;

        preparationRoutine = StartCoroutine(PreparationRoutine());
    }

    private IEnumerator PreparationRoutine()
    {
        for (int secondsRemaining = countdownSeconds; secondsRemaining > 0; secondsRemaining--)
        {
            if (!AreBothHandsDetected())
            {
                preparationRoutine = null;
                EvaluateReadiness();
                yield break;
            }

            currentCountdownStep = secondsRemaining;
            OnCountdownStep?.Invoke(secondsRemaining);
            yield return new WaitForSeconds(1f);
        }

        currentCountdownStep = 0;

        if (!AreBothHandsDetected())
        {
            preparationRoutine = null;
            EvaluateReadiness();
            yield break;
        }

        isReadyMessageVisible = true;
        OnPreparationReady?.Invoke();
        yield return new WaitForSeconds(readyDisplayDuration);
        isReadyMessageVisible = false;

        if (!AreBothHandsDetected())
        {
            preparationRoutine = null;
            EvaluateReadiness();
            yield break;
        }

        completionEmitted = true;
        preparationRoutine = null;
        OnPreparationCompleted?.Invoke();
    }

    private bool AreBothHandsDetected()
    {
        return handsDetection.IsLeftDetected && handsDetection.IsRightDetected;
    }

    private void StopPreparationRoutine()
    {
        if (preparationRoutine == null)
            return;

        StopCoroutine(preparationRoutine);
        preparationRoutine = null;
        currentCountdownStep = 0;
        isReadyMessageVisible = false;
    }
}
