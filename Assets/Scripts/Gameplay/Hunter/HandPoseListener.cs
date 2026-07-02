using System;
using UnityEngine;

public class HandPoseListener : MonoBehaviour
{
    public event Action AimStarted;
    public event Action AimEnded;

    public event Action ShootStarted;
    public event Action ShootEnded;
    [SerializeField] private bool DebugEvents = false;
    public bool IsAiming { get; private set; }

    public void OnAimPoseBegin()
    {
        IsAiming = true;
        AimStarted?.Invoke();
        if (DebugEvents)
            Debug.Log("AIM-BEG");
    }

    public void OnAimPoseEnd()
    {
        IsAiming = false;
        AimEnded?.Invoke();
        if (DebugEvents)
            Debug.Log("AIM-END");
    }

    public void OnShootPoseBegin()
    {
        ShootStarted?.Invoke();
        if (DebugEvents)
            Debug.Log("SHOT-BEG");
    }

    public void OnShootPoseEnd()
    {
        ShootEnded?.Invoke();
        if (DebugEvents)
            Debug.Log("SHOT-END");
    }
}