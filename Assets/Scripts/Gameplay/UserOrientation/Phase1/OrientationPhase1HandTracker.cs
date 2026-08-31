using System;
using Leap;
using UnityEngine;

public sealed class OrientationPhase1HandTracker : MonoBehaviour
{
    [Header("Leap")]
    [SerializeField] private LeapServiceProvider provider;

    [Header("Activity")]
    [SerializeField, Min(0f)] private float activitySaturationDistance = 0.03f;
    [SerializeField, Min(0f)] private float activitySaturationRotation = 20f;

    [Header("Thresholds")]
    [SerializeField, Min(0f)] private float movementThreshold = 0.005f;
    [SerializeField, Min(0f)] private float rotationThreshold = 3f;

    [Header("Grab")]
    [SerializeField, Range(0f, 1f)] private float grabThreshold = 0.8f;
    [SerializeField, Range(0f, 1f)] private float releaseThreshold = 0.65f;

    public event Action<float, float> OnActivityChanged;
    public event Action<bool, bool> OnGripStateChanged;
    public event Action<bool, bool> OnHandPresenceChanged;

    public bool IsConfigured => provider != null;
    public bool AreBothHandsGripping =>
        _leftHandPresent &&
        _rightHandPresent &&
        _leftGripping &&
        _rightGripping;

    private Vector3 _lastLeftPosition;
    private Vector3 _lastRightPosition;
    private Quaternion _lastLeftRotation;
    private Quaternion _lastRightRotation;
    private Vector3 _leftPalmPosition;
    private Vector3 _rightPalmPosition;

    private bool _leftInitialized;
    private bool _rightInitialized;
    private bool _leftHandPresent;
    private bool _rightHandPresent;
    private bool _leftGripping;
    private bool _rightGripping;

    private void Awake()
    {
        if (provider != null)
            return;

        Debug.LogError(
            "[OrientationPhase1Tracking] Falta asignar el LeapServiceProvider.",
            this);
        enabled = false;
    }

    private void Update()
    {
        if (provider == null)
            return;

        Frame frame = provider.CurrentFrame;
        Hand leftHand = null;
        Hand rightHand = null;

        if (frame != null)
        {
            foreach (Hand hand in frame.Hands)
            {
                if (hand.IsLeft)
                {
                    leftHand = hand;
                    _leftPalmPosition = hand.PalmPosition;
                }
                else
                {
                    rightHand = hand;
                    _rightPalmPosition = hand.PalmPosition;
                }
            }
        }

        if (leftHand == null)
            _leftPalmPosition = Vector3.zero;

        if (rightHand == null)
            _rightPalmPosition = Vector3.zero;

        UpdateHandStates(leftHand, rightHand);

        float leftActivity = CalculateActivity(
            leftHand,
            ref _lastLeftPosition,
            ref _lastLeftRotation,
            ref _leftInitialized);
        float rightActivity = CalculateActivity(
            rightHand,
            ref _lastRightPosition,
            ref _lastRightRotation,
            ref _rightInitialized);

        OnActivityChanged?.Invoke(leftActivity, rightActivity);
    }

    public void PublishCurrentStates()
    {
        OnHandPresenceChanged?.Invoke(_leftHandPresent, _rightHandPresent);
        OnGripStateChanged?.Invoke(_leftGripping, _rightGripping);
    }

    public bool TryGetPalmPosition(HandType handType, out Vector3 palmPosition)
    {
        if (handType == HandType.LEFT && _leftHandPresent)
        {
            palmPosition = _leftPalmPosition;
            return true;
        }

        if (handType == HandType.RIGHT && _rightHandPresent)
        {
            palmPosition = _rightPalmPosition;
            return true;
        }

        palmPosition = Vector3.zero;
        return false;
    }

    public void ResetTrackingState()
    {
        _leftInitialized = false;
        _rightInitialized = false;
        _leftHandPresent = false;
        _rightHandPresent = false;
        _leftGripping = false;
        _rightGripping = false;
        _lastLeftPosition = Vector3.zero;
        _lastRightPosition = Vector3.zero;
        _lastLeftRotation = Quaternion.identity;
        _lastRightRotation = Quaternion.identity;
        _leftPalmPosition = Vector3.zero;
        _rightPalmPosition = Vector3.zero;
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

    private void OnDisable()
    {
        ResetTrackingState();
    }
}
