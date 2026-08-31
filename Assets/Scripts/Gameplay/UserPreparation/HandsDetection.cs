using System;
using Leap;
using UnityEngine;

public class HandsDetection : MonoBehaviour
{
    public event Action<bool> OnLeftHandDetectionChanged;
    public event Action<bool> OnRightHandDetectionChanged;

    [Header("Leap")]
    [SerializeField] private LeapServiceProvider provider;

    private bool leftDetected;
    private bool rightDetected;

    public bool IsLeftDetected => leftDetected;
    public bool IsRightDetected => rightDetected;

    private void Awake()
    {
        if (provider != null)
            return;

        Debug.LogError(
            "[HandsDetection] Falta asignar el LeapServiceProvider.",
            this);
        enabled = false;
    }

    private void Update()
    {
        Frame frame = provider.CurrentFrame;
        bool currentLeftDetected = false;
        bool currentRightDetected = false;

        foreach (Hand hand in frame.Hands)
        {
            if (hand.IsLeft)
                currentLeftDetected = true;
            else
                currentRightDetected = true;
        }

        if (currentLeftDetected != leftDetected)
        {
            leftDetected = currentLeftDetected;
            OnLeftHandDetectionChanged?.Invoke(leftDetected);
        }

        if (currentRightDetected != rightDetected)
        {
            rightDetected = currentRightDetected;
            OnRightHandDetectionChanged?.Invoke(rightDetected);
        }
    }
}
