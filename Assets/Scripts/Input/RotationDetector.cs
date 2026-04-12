using System.Collections.Generic;
using UnityEngine;
using Leap;

public class RotationDetector
{
    private Dictionary<HandType, bool> isRotating = new();
    private Dictionary<HandType, Quaternion> lastRotation = new();
    private Dictionary<HandType, float> rotationAccum = new();

    public float rotateThreshold = 25f;
    public float noiseThreshold = 1f;
    private Dictionary<HandType, float> lastRotationTime = new();
    public float rotationEndDelay = 0.25f;

    // public bool IsRotating(HAND hand, Quaternion currentRot, float grab, float pinch, Vector3 velocity)
    // {
    //     if (!lastRotation.ContainsKey(hand))
    //     {
    //         lastRotation[hand] = currentRot;
    //         rotationAccum[hand] = 0;
    //         return false;
    //     }

    //     float angleDelta = Quaternion.Angle(lastRotation[hand], currentRot);

    //     bool isFree = grab < 0.2f && pinch < 0.2f;
    //     bool isStable = velocity.magnitude < 0.3f;

    //     if (isFree && isStable)
    //     {
    //         if (angleDelta > noiseThreshold)
    //             rotationAccum[hand] += angleDelta;
    //     }
    //     else
    //     {
    //         rotationAccum[hand] = 0;
    //     }

    //     lastRotation[hand] = currentRot;
    //     lastRotationTime[hand] = Time.time;

    //     return rotationAccum[hand] >= rotateThreshold;
    // }

    public bool UpdateRotation(HandType hand, Quaternion currentRot, float grab, float pinch, Vector3 velocity)
    {
        if (!lastRotation.ContainsKey(hand))
        {
            lastRotation[hand] = currentRot;
            rotationAccum[hand] = 0;
            isRotating[hand] = false;
            return false;
        }

        float angleDelta = Quaternion.Angle(lastRotation[hand], currentRot);

        bool isFree = grab < 0.2f && pinch < 0.2f;
        bool isStable = velocity.magnitude < 0.3f;

        if (isFree && isStable && angleDelta > noiseThreshold)
        {
            rotationAccum[hand] += angleDelta;

            if (!isRotating[hand] && rotationAccum[hand] >= rotateThreshold)
            {
                isRotating[hand] = true;
                lastRotationTime[hand] = Time.time;
                return true; // 🔥 SOLO dispara una vez (inicio)
            }

            if (isRotating[hand])
            {
                lastRotationTime[hand] = Time.time;
            }
        }
        else
        {
            rotationAccum[hand] = 0;
        }

        lastRotation[hand] = currentRot;
        return false;
    }

    public bool HasRotationEnded(HandType hand)
    {
        if (!isRotating.ContainsKey(hand) || !isRotating[hand])
            return false;

        if ((Time.time - lastRotationTime[hand]) > rotationEndDelay)
        {
            isRotating[hand] = false;
            rotationAccum[hand] = 0;
            return true;
        }

        return false;
    }

    public bool IsCurrentlyRotating(HandType hand)
    {
        return isRotating.ContainsKey(hand) && isRotating[hand];
    }

    public void Reset(HandType hand)
    {
        rotationAccum[hand] = 0;
    }
}