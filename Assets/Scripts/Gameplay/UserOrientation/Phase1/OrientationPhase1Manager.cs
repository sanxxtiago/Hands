using System;
using Leap;
using UnityEngine;

public class OrientationPhase1Manager : OrientationManager
{
    [Header("Leap")]
    [SerializeField] private LeapServiceProvider provider;

    [Header("Exploration")]
    [SerializeField, Min(0f)] private float requiredActiveTime = 10f;
    [SerializeField, Min(0f)] private float activitySaturationDistance = 0.03f;
    [SerializeField, Min(0f)] private float activitySaturationRotation = 20f;

    [Header("Thresholds")]
    [SerializeField, Min(0f)] private float movementThreshold = 0.005f;
    [SerializeField, Min(0f)] private float rotationThreshold = 3f;

    [Header("Grab")]
    [SerializeField, Range(0f, 1f)] private float grabThreshold = 0.8f;
    [SerializeField, Range(0f, 1f)] private float releaseThreshold = 0.65f;
    [SerializeField, Min(0f)] private float bothHandsHoldTime = 0.25f;

    public event Action<float, float> OnProgressChanged;
    public event Action<float, float> OnActivityFeedbackChanged;
    public event Action<bool, bool> OnGripStateChanged;
    public event Action<bool, bool> OnHandPresenceChanged;
    public event Action OnExplorationCompleted;

    private Vector3 _lastLeftPos;
    private Vector3 _lastRightPos;
    private Quaternion _lastLeftRot;
    private Quaternion _lastRightRot;

    private bool _leftInitialized;
    private bool _rightInitialized;
    private bool _leftHandPresent;
    private bool _rightHandPresent;
    private bool _leftGripping;
    private bool _rightGripping;
    private bool _explorationCompleted;
    private bool _phaseCompleted;

    private float _activeTime;
    private float _bothHandsClosedTime;

    private void Awake()
    {
        if (provider != null)
            return;

        Debug.LogError(
            "[OrientationPhase1] Falta asignar el LeapServiceProvider.",
            this);
        enabled = false;
    }

    private void Update()
    {
        Frame frame = provider.CurrentFrame;
        Hand leftHand = null;
        Hand rightHand = null;

        if (frame != null)
        {
            foreach (Hand hand in frame.Hands)
            {
                if (hand.IsLeft)
                    leftHand = hand;
                else
                    rightHand = hand;
            }
        }

        UpdateHandStates(leftHand, rightHand);

        if (!_explorationCompleted)
        {
            bool activityDetected = DetectActivity(
                leftHand,
                rightHand,
                out float leftActivity,
                out float rightActivity);

            OnActivityFeedbackChanged?.Invoke(leftActivity, rightActivity);

            if (activityDetected)
                _activeTime += Time.deltaTime;

            float requiredTime = Mathf.Max(requiredActiveTime, Mathf.Epsilon);
            float progress = Mathf.Clamp01(_activeTime / requiredTime);
            OnProgressChanged?.Invoke(_activeTime, requiredActiveTime);

            if (progress < 1f)
                return;

            _explorationCompleted = true;
            PublishHandStates();
            OnExplorationCompleted?.Invoke();
            return;
        }

        if (_phaseCompleted)
            return;

        OnActivityFeedbackChanged?.Invoke(0f, 0f);

        if (!BothHandsGrabbing())
        {
            _bothHandsClosedTime = 0f;
            return;
        }

        _bothHandsClosedTime += Time.deltaTime;
        if (_bothHandsClosedTime >= bothHandsHoldTime)
            CompletePhase();
    }

    protected override void CompletePhase()
    {
        _phaseCompleted = true;
        _bothHandsClosedTime = 0f;
        base.CompletePhase();
    }

    private bool DetectActivity(
        Hand leftHand,
        Hand rightHand,
        out float leftActivity,
        out float rightActivity)
    {
        leftActivity = CalculateActivity(
            leftHand,
            ref _lastLeftPos,
            ref _lastLeftRot,
            ref _leftInitialized);
        rightActivity = CalculateActivity(
            rightHand,
            ref _lastRightPos,
            ref _lastRightRot,
            ref _rightInitialized);

        return leftActivity > 0f || rightActivity > 0f;
    }

    private float CalculateActivity(
        Hand hand,
        ref Vector3 previousPosition,
        ref Quaternion previousRotation,
        ref bool initialized)
    {
        if (hand == null)
        {
            initialized = false;
            return 0f;
        }

        Vector3 currentPosition = hand.PalmPosition;
        Quaternion currentRotation = hand.Rotation;

        if (!initialized)
        {
            previousPosition = currentPosition;
            previousRotation = currentRotation;
            initialized = true;
            return 0f;
        }

        float movementDelta = Vector3.Distance(currentPosition, previousPosition);
        float rotationDelta = Quaternion.Angle(currentRotation, previousRotation);

        previousPosition = currentPosition;
        previousRotation = currentRotation;

        if (movementDelta <= movementThreshold && rotationDelta <= rotationThreshold)
            return 0f;

        float movementScore = NormalizeActivity(
            movementDelta,
            movementThreshold,
            activitySaturationDistance);
        float rotationScore = NormalizeActivity(
            rotationDelta,
            rotationThreshold,
            activitySaturationRotation);

        return Mathf.Max(movementScore, rotationScore);
    }

    private float NormalizeActivity(
        float value,
        float threshold,
        float saturation)
    {
        if (saturation <= threshold)
            return 1f;

        return Mathf.Clamp01(Mathf.InverseLerp(threshold, saturation, value));
    }

    private void UpdateHandStates(Hand leftHand, Hand rightHand)
    {
        bool leftPresent = leftHand != null;
        bool rightPresent = rightHand != null;
        bool leftGripping = UpdateGripState(leftHand, _leftGripping);
        bool rightGripping = UpdateGripState(rightHand, _rightGripping);

        if (leftPresent != _leftHandPresent || rightPresent != _rightHandPresent)
        {
            _leftHandPresent = leftPresent;
            _rightHandPresent = rightPresent;
            OnHandPresenceChanged?.Invoke(_leftHandPresent, _rightHandPresent);
        }

        if (leftGripping != _leftGripping || rightGripping != _rightGripping)
        {
            _leftGripping = leftGripping;
            _rightGripping = rightGripping;
            OnGripStateChanged?.Invoke(_leftGripping, _rightGripping);
        }
    }

    private bool UpdateGripState(Hand hand, bool previousState)
    {
        if (hand == null)
            return false;

        float threshold = previousState
            ? Mathf.Min(grabThreshold, releaseThreshold)
            : grabThreshold;

        return hand.GrabStrength >= threshold;
    }

    private void PublishHandStates()
    {
        OnHandPresenceChanged?.Invoke(_leftHandPresent, _rightHandPresent);
        OnGripStateChanged?.Invoke(_leftGripping, _rightGripping);
    }

    private bool BothHandsGrabbing()
    {
        return _leftHandPresent &&
               _rightHandPresent &&
               _leftGripping &&
               _rightGripping;
    }

    private void OnDisable()
    {
        _leftInitialized = false;
        _rightInitialized = false;
        _leftHandPresent = false;
        _rightHandPresent = false;
        _leftGripping = false;
        _rightGripping = false;
        _explorationCompleted = false;
        _phaseCompleted = false;
        _activeTime = 0f;
        _bothHandsClosedTime = 0f;
    }
}
