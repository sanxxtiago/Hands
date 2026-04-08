using UnityEngine;
using Leap;
using System;

public class GrabExample : MonoBehaviour
{
    // public GrabDetector grabDetector;

    // private void Update()
    // {
    //     if (grabDetector.IsGrabbing)
    //     {
    //         Debug.Log("Grabbing");
    //     }

    //     if (grabDetector.GrabStartedThisFrame)
    //     {
    //         Debug.Log("Grab started this frame.");
    //     }
    // }
    public GestureDetector gestureDetector;
    void OnEnable()
    {
        gestureDetector.OnGrabStart += _OnGrabStart;
        gestureDetector.OnGrabEnd += _OnGrabEnd;

    }

    private void _OnGrabStart(GestureInputEventArgs eventArgs)
    {
        Debug.Log(eventArgs.handPosition);
        Debug.Log("Is Grabbing.");
    }

    private void _OnGrabEnd(GestureInputEventArgs eventArgs)
    {
        Debug.Log("Is NOT Grabbing.");
    }

    void Osable()
    {
        gestureDetector.OnGrabStart -= _OnGrabStart;
        gestureDetector.OnGrabEnd -= _OnGrabEnd;
    }
}