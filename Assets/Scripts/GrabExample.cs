using UnityEngine;
using Leap;

public class GrabExample : MonoBehaviour
{
    public GrabDetector grabDetector;

    private void Update()
    {
        if (grabDetector.IsGrabbing)
        {
            // Handle grab logic here
            Debug.Log("Grabbing.");
        }

        if (grabDetector.GrabStartedThisFrame)
        {
            // Handle grab start logic here
            Debug.Log("Grab started this frame.");
        }
    }
}