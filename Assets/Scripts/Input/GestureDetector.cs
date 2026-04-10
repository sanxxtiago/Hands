using System;
using System.Collections.Generic;
using Leap;
using UnityEngine;

public class GestureDetector : MonoBehaviour
{
    public event Action<GestureInputEventArgs> OnGrabStart;
    public event Action<GestureInputEventArgs> OnGrabEnd;
    public event Action<GestureInputEventArgs> OnPinchStart;
    public event Action<GestureInputEventArgs> OnPinchEnd;
    public event Action<GestureInputEventArgs> OnRotateStart;
    public event Action<GestureInputEventArgs> OnRotateEnd;

    public event Action<GestureInputEventArgs> OnHandUpdate;
    public event Action<GestureInputEventArgs> OnRotateUpdate;



    public LeapProvider leapProvider;
    public float grabStrengthThreshold = 0.8f;
    public float pinchStrengthThreshold = 0.8f;

    private Dictionary<HAND, GESTURESTATE> handStates = new();
    private RotationDetector rotationDetector = new RotationDetector();
    void Update()
    {
        foreach (var hand in leapProvider.CurrentFrame.Hands)
        {
            HAND currentHand = hand.IsLeft ? HAND.LEFT : HAND.RIGHT;

            Vector3 handPos = hand.PalmPosition;
            Quaternion rotation = hand.Rotation;
            Vector3 velocity = hand.PalmVelocity;
            float grab = hand.GrabStrength;
            float pinch = hand.PinchStrength;

            OnHandUpdate?.Invoke(
                new GestureInputEventArgs(currentHand, handPos, rotation, velocity, grab, pinch)
            );

            bool isGrab = grab > grabStrengthThreshold;
            bool isPinch = pinch > pinchStrengthThreshold;

            bool isRotate = rotationDetector.IsRotating(
                currentHand,
                rotation,
                grab,
                pinch,
                velocity
            );

            bool rotateEnded = rotationDetector.HasRotationEnded(currentHand);

            handStates.TryGetValue(currentHand, out GESTURESTATE prevState);

            // =========================
            // GESTURE STATE (GRAB / PINCH / IDLE)
            // =========================
            GESTURESTATE newState = GESTURESTATE.IDLE;

            if (isGrab)
                newState = GESTURESTATE.GRAB;
            else if (isPinch)
                newState = GESTURESTATE.PINCH;
            else
                newState = GESTURESTATE.IDLE;

            GestureInputEventArgs e = new GestureInputEventArgs(
                currentHand, handPos, rotation, velocity, grab, pinch
            );

            // =========================
            // STATE TRANSITIONS
            // =========================

            if (prevState != newState)
            {
                if (prevState == GESTURESTATE.GRAB)
                    OnGrabEnd?.Invoke(e);

                if (prevState == GESTURESTATE.PINCH)
                    OnPinchEnd?.Invoke(e);

                if (newState == GESTURESTATE.GRAB)
                    OnGrabStart?.Invoke(e);

                if (newState == GESTURESTATE.PINCH)
                    OnPinchStart?.Invoke(e);

                handStates[currentHand] = newState;
            }

            // =========================
            // ROTATION (INDEPENDENT SYSTEM)
            // =========================

            if (isRotate && prevState != GESTURESTATE.ROTATE)
            {
                OnRotateStart?.Invoke(e);
            }

            if (isRotate)
            {
                OnRotateUpdate?.Invoke(e);
            }

            if (prevState == GESTURESTATE.ROTATE && rotateEnded)
            {
                OnRotateEnd?.Invoke(e);
                rotationDetector.Reset(currentHand);
            }
        }
    }
}