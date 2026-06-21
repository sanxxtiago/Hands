using System;
using System.Collections;
using Leap;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OrientationPhase1Manager : OrientationManager
{
    [Header("Leap")]
    [SerializeField] private LeapServiceProvider provider;

    [Header("Exploration")]
    [SerializeField] private float requiredActiveTime = 10f;

    [Header("Thresholds")]
    [SerializeField] private float movementThreshold = 0.005f; // 5 mm
    [SerializeField] private float rotationThreshold = 3f;     // 3°

    [Header("Grab")]
    [SerializeField] private float grabThreshold = 0.8f;

    public event Action<float, float> OnProgressChanged;
    public event Action OnExplorationCompleted;

    private Vector3 _lastLeftPos;
    private Vector3 _lastRightPos;

    private Quaternion _lastLeftRot;
    private Quaternion _lastRightRot;

    private bool _leftInitialized;
    private bool _rightInitialized;

    private bool _explorationCompleted;
    private bool _phaseCompleted;

    private float _activeTime;

    private void Update()
    {
        Frame frame = provider.CurrentFrame;

        Hand leftHand = null;
        Hand rightHand = null;

        foreach (Hand hand in frame.Hands)
        {
            if (hand.IsLeft)
                leftHand = hand;
            else
                rightHand = hand;
        }

        if (!_explorationCompleted)
        {
            bool activityDetected =
                DetectActivity(leftHand, rightHand);

            if (activityDetected)
            {
                _activeTime += Time.deltaTime;
            }

            float progress =
                Mathf.Clamp01(_activeTime / requiredActiveTime);
            Debug.Log(progress);
            OnProgressChanged?.Invoke(_activeTime, requiredActiveTime);

            if (progress >= 1f)
            {
                _explorationCompleted = true;
                OnExplorationCompleted?.Invoke();
            }
        }
        else if (!_phaseCompleted)
        {
            if (BothHandsGrabbing(leftHand, rightHand))
            {
                CompletePhase();
            }
        }
    }

    protected override void CompletePhase()
    {
        _phaseCompleted = true;
        base.CompletePhase();
    }

    private bool DetectActivity(
        Hand leftHand,
        Hand rightHand)
    {
        bool leftActive = false;
        bool rightActive = false;

        if (leftHand != null)
        {
            Vector3 currentPos = leftHand.PalmPosition;
            Quaternion currentRot = leftHand.Rotation;

            if (_leftInitialized)
            {
                float movementDelta =
                    Vector3.Distance(currentPos, _lastLeftPos);

                float rotationDelta =
                    Quaternion.Angle(currentRot, _lastLeftRot);

                leftActive =
                    movementDelta > movementThreshold ||
                    rotationDelta > rotationThreshold;
            }

            _lastLeftPos = currentPos;
            _lastLeftRot = currentRot;
            _leftInitialized = true;
        }
        else
        {
            _leftInitialized = false;
        }

        if (rightHand != null)
        {
            Vector3 currentPos = rightHand.PalmPosition;
            Quaternion currentRot = rightHand.Rotation;

            if (_rightInitialized)
            {
                float movementDelta =
                    Vector3.Distance(currentPos, _lastRightPos);

                float rotationDelta =
                    Quaternion.Angle(currentRot, _lastRightRot);

                rightActive =
                    movementDelta > movementThreshold ||
                    rotationDelta > rotationThreshold;
            }

            _lastRightPos = currentPos;
            _lastRightRot = currentRot;
            _rightInitialized = true;
        }
        else
        {
            _rightInitialized = false;
        }

        return leftActive || rightActive;
    }

    private bool BothHandsGrabbing(
        Hand leftHand,
        Hand rightHand)
    {
        if (leftHand == null || rightHand == null)
            return false;

        return leftHand.GrabStrength >= grabThreshold &&
               rightHand.GrabStrength >= grabThreshold;
    }
}