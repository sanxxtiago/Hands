using System;
using UnityEngine;

public sealed class OrientationPhase1Exploration : MonoBehaviour
{
    [SerializeField] private OrientationPhase1HandTracker handTracker;
    [SerializeField, Min(0f)] private float requiredActiveTime = 10f;

    public event Action<float, float> OnProgressChanged;
    public event Action OnCompleted;

    public bool IsConfigured => handTracker != null;
    public bool IsCompleted => _completed;

    private bool _running;
    private bool _completed;
    private float _activeTime;

    private void Awake()
    {
        if (handTracker != null)
            return;

        Debug.LogError(
            "[OrientationPhase1Exploration] Falta asignar el tracker de manos.",
            this);
        enabled = false;
    }

    private void OnEnable()
    {
        if (handTracker != null)
            handTracker.OnActivityChanged += HandleActivityChanged;
    }

    public void BeginExploration()
    {
        ResetExplorationState();
        _running = true;
        NotifyProgress();
    }

    public void ResetExplorationState()
    {
        _running = false;
        _completed = false;
        _activeTime = 0f;
    }

    private void HandleActivityChanged(float leftActivity, float rightActivity)
    {
        if (!_running || _completed)
            return;

        if (leftActivity > 0f || rightActivity > 0f)
            _activeTime += Time.deltaTime;

        NotifyProgress();

        float requiredTime = Mathf.Max(requiredActiveTime, Mathf.Epsilon);
        if (_activeTime / requiredTime < 1f)
            return;

        _completed = true;
        _running = false;
        OnCompleted?.Invoke();
    }

    private void NotifyProgress()
    {
        OnProgressChanged?.Invoke(_activeTime, requiredActiveTime);
    }

    private void OnDisable()
    {
        if (handTracker != null)
            handTracker.OnActivityChanged -= HandleActivityChanged;

        ResetExplorationState();
    }
}
