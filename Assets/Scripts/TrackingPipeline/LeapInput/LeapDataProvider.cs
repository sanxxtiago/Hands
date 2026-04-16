using System;
using Leap;
using UnityEngine;

public class LeapDataProvider : MonoBehaviour
{
    public event Action<Frame> OnFrameReady;
    [SerializeField]
    private LeapServiceProvider _provider;
    public bool RawFramDebugger = false;
    void Update()
    {
        Frame frame = _provider.CurrentFrame;
        if (frame == null) return;

        if (RawFramDebugger)
            Debug.Log($"LEAP FRAME: {frame.Id} | hands: {frame.Hands.Count}");

        OnFrameReady?.Invoke(frame);
    }
    public Frame GetCurrentFrame()
    {
        return _provider.CurrentFrame;
    }
}