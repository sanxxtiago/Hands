using System;
using UnityEngine;

public class OrientationPhase1Manager : OrientationManager
{
    [Header("Components")]
    [SerializeField] private OrientationPhase1HandTracker handTracker;
    [SerializeField] private OrientationPhase1Exploration exploration;

    [Header("Grab")]
    [SerializeField, Min(0f)] private float bothHandsHoldTime = 0.25f;

    [Header("Volume sequence")]
    [SerializeField] private OrientationPhase1Volume firstVolume;
    [SerializeField] private OrientationPhase1Volume secondVolume;
    [SerializeField] private OrientationPhase1Volume thirdVolume;
    [SerializeField] private OrientationPhase1Volume fourthVolume;

    public event Action<float, float> OnProgressChanged;
    public event Action<float, float> OnActivityFeedbackChanged;
    public event Action<bool, bool> OnGripStateChanged;
    public event Action<bool, bool> OnHandPresenceChanged;
    public event Action OnExplorationCompleted;

    private bool _explorationCompleted;
    private bool _phaseCompleted;
    private bool _volumeSequenceStarted;

    private float _bothHandsClosedTime;
    private int _currentVolumeIndex = -1;
    private OrientationPhase1Volume[] _volumeSequence;

    private void Awake()
    {
        _volumeSequence = new[]
        {
            firstVolume,
            secondVolume,
            thirdVolume,
            fourthVolume
        };

        if (handTracker == null || exploration == null)
        {
            Debug.LogError(
                "[OrientationPhase1] Faltan referencias a los componentes de la fase.",
                this);
            enabled = false;
            return;
        }

        if (!handTracker.IsConfigured || !exploration.IsConfigured)
        {
            Debug.LogError(
                "[OrientationPhase1] La configuración de tracking o exploración está incompleta.",
                this);
            enabled = false;
            return;
        }

        if (!ValidateVolumeSequence())
        {
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (handTracker == null || exploration == null)
            return;

        handTracker.OnActivityChanged += HandleActivityChanged;
        handTracker.OnGripStateChanged += HandleGripStateChanged;
        handTracker.OnHandPresenceChanged += HandleHandPresenceChanged;
        exploration.OnProgressChanged += HandleProgressChanged;
        exploration.OnCompleted += HandleExplorationCompleted;

        for (int i = 0; i < _volumeSequence.Length; i++)
            _volumeSequence[i].OnTouched += HandleVolumeTouched;

        DeactivateAllVolumes();
        exploration.BeginExploration();
    }

    private void Update()
    {
        if (!_explorationCompleted || _phaseCompleted)
            return;

        if (_volumeSequenceStarted)
            return;

        if (!handTracker.AreBothHandsGripping)
        {
            _bothHandsClosedTime = 0f;
            return;
        }

        _bothHandsClosedTime += Time.deltaTime;
        if (_bothHandsClosedTime >= bothHandsHoldTime)
            StartVolumeSequence();
    }

    protected override void CompletePhase()
    {
        _phaseCompleted = true;
        _volumeSequenceStarted = false;
        _bothHandsClosedTime = 0f;
        DeactivateAllVolumes();
        base.CompletePhase();
    }

    private void HandleActivityChanged(float leftActivity, float rightActivity)
    {
        if (_explorationCompleted)
        {
            OnActivityFeedbackChanged?.Invoke(0f, 0f);
            return;
        }

        OnActivityFeedbackChanged?.Invoke(leftActivity, rightActivity);
    }

    private void HandleGripStateChanged(bool leftGripping, bool rightGripping)
    {
        OnGripStateChanged?.Invoke(leftGripping, rightGripping);
    }

    private void HandleHandPresenceChanged(bool leftPresent, bool rightPresent)
    {
        OnHandPresenceChanged?.Invoke(leftPresent, rightPresent);
    }

    private void HandleProgressChanged(float activeTime, float requiredActiveTime)
    {
        OnProgressChanged?.Invoke(activeTime, requiredActiveTime);
    }

    private void HandleExplorationCompleted()
    {
        if (_explorationCompleted)
            return;

        _explorationCompleted = true;
        handTracker.PublishCurrentStates();
        OnExplorationCompleted?.Invoke();
    }

    private void StartVolumeSequence()
    {
        if (_volumeSequenceStarted || _phaseCompleted)
            return;

        _volumeSequenceStarted = true;
        _currentVolumeIndex = 0;
        ActivateCurrentVolume();
    }

    private void HandleVolumeTouched(OrientationPhase1Volume volume)
    {
        if (!_volumeSequenceStarted || _phaseCompleted || volume == null)
            return;

        if (_currentVolumeIndex < 0 ||
            _currentVolumeIndex >= _volumeSequence.Length ||
            volume != _volumeSequence[_currentVolumeIndex])
        {
            return;
        }

        if (_currentVolumeIndex >= _volumeSequence.Length - 1)
        {
            _volumeSequenceStarted = false;
            CompletePhase();
            return;
        }

        _currentVolumeIndex++;
        ActivateCurrentVolume();
    }

    private bool ValidateVolumeSequence()
    {
        for (int i = 0; i < _volumeSequence.Length; i++)
        {
            if (_volumeSequence[i] == null)
            {
                Debug.LogError(
                    "[OrientationPhase1] Falta asignar uno de los cuatro volúmenes.",
                    this);
                return false;
            }

            for (int previousIndex = 0; previousIndex < i; previousIndex++)
            {
                if (_volumeSequence[i] != _volumeSequence[previousIndex])
                    continue;

                Debug.LogError(
                    "[OrientationPhase1] La secuencia contiene volúmenes repetidos.",
                    this);
                return false;
            }
        }

        return true;
    }

    private void ActivateCurrentVolume()
    {
        for (int i = 0; i < _volumeSequence.Length; i++)
        {
            bool shouldBeActive = i == _currentVolumeIndex;
            GameObject volumeObject = _volumeSequence[i].gameObject;

            if (volumeObject.activeSelf != shouldBeActive)
                volumeObject.SetActive(shouldBeActive);
        }
    }

    private void DeactivateAllVolumes()
    {
        if (_volumeSequence == null)
            return;

        for (int i = 0; i < _volumeSequence.Length; i++)
        {
            if (_volumeSequence[i] != null)
                _volumeSequence[i].gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (handTracker != null)
        {
            handTracker.OnActivityChanged -= HandleActivityChanged;
            handTracker.OnGripStateChanged -= HandleGripStateChanged;
            handTracker.OnHandPresenceChanged -= HandleHandPresenceChanged;
        }

        if (_volumeSequence != null)
        {
            for (int i = 0; i < _volumeSequence.Length; i++)
            {
                if (_volumeSequence[i] != null)
                    _volumeSequence[i].OnTouched -= HandleVolumeTouched;
            }
        }

        if (exploration != null)
        {
            exploration.OnProgressChanged -= HandleProgressChanged;
            exploration.OnCompleted -= HandleExplorationCompleted;
            exploration.ResetExplorationState();
        }

        DeactivateAllVolumes();
        handTracker?.ResetTrackingState();
        _explorationCompleted = false;
        _phaseCompleted = false;
        _volumeSequenceStarted = false;
        _bothHandsClosedTime = 0f;
        _currentVolumeIndex = -1;
    }
}
