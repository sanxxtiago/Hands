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
    public event Action<GestureInputEventArgs> OnHandUpdate;

    public LeapProvider leapProvider;
    public float grabStrengthThreshold = 0.8f;
    public float pinchStrengthThreshold = 0.8f;


    private Dictionary<HAND, GESTURESTATE> handStates = new();

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

            GESTURESTATE newState = GESTURESTATE.IDLE;

            OnHandUpdate?.Invoke(new GestureInputEventArgs(currentHand, handPos, rotation, velocity, grab, pinch));

            if (grab > grabStrengthThreshold)
                newState = GESTURESTATE.GRAB;
            else if (pinch > pinchStrengthThreshold)
                newState = GESTURESTATE.PINCH;

            handStates.TryGetValue(currentHand, out GESTURESTATE prevState);
            if (prevState != newState)
            {
                // SALIDAS
                if (prevState == GESTURESTATE.GRAB)
                    OnGrabEnd?.Invoke(new GestureInputEventArgs(currentHand, handPos, rotation, velocity, grab, pinch));

                if (prevState == GESTURESTATE.PINCH)
                    OnPinchEnd?.Invoke(new GestureInputEventArgs(currentHand, handPos, rotation, velocity, grab, pinch));

                // ENTRADAS
                if (newState == GESTURESTATE.GRAB)
                    OnGrabStart?.Invoke(new GestureInputEventArgs(currentHand, handPos, rotation, velocity, grab, pinch));

                if (newState == GESTURESTATE.PINCH)
                    OnPinchStart?.Invoke(new GestureInputEventArgs(currentHand, handPos, rotation, velocity, grab, pinch));

                handStates[currentHand] = newState;
            }
        }
    }
}