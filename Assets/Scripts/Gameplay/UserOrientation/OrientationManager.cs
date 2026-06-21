using System;
using UnityEngine;

public abstract class OrientationManager : MonoBehaviour
{
    public event Action OnPhaseCompleted;
    protected virtual void CompletePhase()
    {
        OnPhaseCompleted?.Invoke();
    }
}
