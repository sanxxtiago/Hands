using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrientationPhase2Manager : OrientationManager
{
    [Header("Configuration")]
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private int objectivesToComplete = 4;

    [Header("Touch Feedback")]
    [SerializeField] private ParticleSystem targetTouchEffectPrefab;
    [SerializeField, Min(0f)] private float nextTargetDelay = 0.12f;

    [Header("Phase Completion")]
    [SerializeField, Min(0f)] private float finalTargetDelay = 0.75f;

    public event Action<int, int> OnProgressChanged;

    private int _currentSpawnIndex;
    private int _completedObjectives;
    private OrientationTarget _activeTarget;
    private ParticleEffectPlayer _particleEffectPlayer;
    private readonly List<OrientationTarget> _feedbackTargets = new List<OrientationTarget>();
    private Coroutine _nextTargetCoroutine;
    private Coroutine _phaseCompletionCoroutine;
    private bool _isCompletingPhase;

    private void Awake()
    {
        _particleEffectPlayer = GetComponent<ParticleEffectPlayer>();
    }

    private void Start()
    {
        if (targetPrefab == null)
        {
            Debug.LogError("[OrientationPhase2] Falta el prefab del objetivo.", this);
            return;
        }

        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogError("[OrientationPhase2] No hay puntos de spawn configurados.", this);
            return;
        }

        objectivesToComplete = Mathf.Min(
            objectivesToComplete,
            spawnPoints.Count);

        _currentSpawnIndex = 0;
        _completedObjectives = 0;
        _isCompletingPhase = false;

        NotifyUI();

        SpawnNextTarget();
    }

    private void SpawnNextTarget()
    {
        if (_completedObjectives >= objectivesToComplete)
        {
            CompletePhase();
            return;
        }

        if (_currentSpawnIndex >= spawnPoints.Count)
        {
            Debug.LogError("[OrientationPhase2] Se agotaron los puntos de spawn antes de completar los objetivos.", this);
            CompletePhase();
            return;
        }

        Transform point = spawnPoints[_currentSpawnIndex];
        if (point == null)
        {
            Debug.LogError("[OrientationPhase2] Hay un punto de spawn sin referencia.", this);
            _currentSpawnIndex++;
            SpawnNextTarget();
            return;
        }

        GameObject instance = Instantiate(
            targetPrefab,
            point.position,
            point.rotation);

        OrientationTarget target = instance.GetComponent<OrientationTarget>();
        if (target == null)
        {
            Debug.LogError("[OrientationPhase2] El prefab del objetivo no contiene OrientationTarget.", instance);
            Destroy(instance);
            return;
        }

        _activeTarget = target;
        _activeTarget.OnTouchDetected += HandleTargetTouchDetected;
        _activeTarget.OnTouchFeedbackCompleted += HandleTargetFeedbackCompleted;

        _currentSpawnIndex++;
    }

    private void HandleTargetTouchDetected(OrientationTarget target)
    {
        if (_isCompletingPhase || target == null || target != _activeTarget)
            return;

        _activeTarget.OnTouchDetected -= HandleTargetTouchDetected;
        _activeTarget = null;
        _feedbackTargets.Add(target);

        Vector3 targetPosition = target.transform.position;
        _particleEffectPlayer?.Play(targetTouchEffectPrefab, targetPosition);

        _completedObjectives++;

        NotifyUI();

        if (_completedObjectives >= objectivesToComplete)
        {
            _isCompletingPhase = true;
            _phaseCompletionCoroutine = StartCoroutine(CompletePhaseAfterDelay());
            return;
        }

        _nextTargetCoroutine = StartCoroutine(SpawnNextTargetAfterDelay());
    }

    private void HandleTargetFeedbackCompleted(OrientationTarget target)
    {
        if (target == null)
            return;

        target.OnTouchFeedbackCompleted -= HandleTargetFeedbackCompleted;
        _feedbackTargets.Remove(target);
        Destroy(target.gameObject);
    }

    private IEnumerator SpawnNextTargetAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, nextTargetDelay));

        _nextTargetCoroutine = null;
        if (!_isCompletingPhase)
            SpawnNextTarget();
    }

    private IEnumerator CompletePhaseAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, finalTargetDelay));

        _phaseCompletionCoroutine = null;
        if (_isCompletingPhase)
            CompletePhase();
    }

    private void OnDisable()
    {
        if (_phaseCompletionCoroutine != null)
        {
            StopCoroutine(_phaseCompletionCoroutine);
            _phaseCompletionCoroutine = null;
        }

        if (_nextTargetCoroutine != null)
        {
            StopCoroutine(_nextTargetCoroutine);
            _nextTargetCoroutine = null;
        }

        _isCompletingPhase = false;

        if (_activeTarget != null)
        {
            _activeTarget.OnTouchDetected -= HandleTargetTouchDetected;
            _activeTarget.OnTouchFeedbackCompleted -= HandleTargetFeedbackCompleted;
            Destroy(_activeTarget.gameObject);
            _activeTarget = null;
        }

        for (int i = _feedbackTargets.Count - 1; i >= 0; i--)
        {
            OrientationTarget target = _feedbackTargets[i];
            if (target != null)
            {
                target.OnTouchDetected -= HandleTargetTouchDetected;
                target.OnTouchFeedbackCompleted -= HandleTargetFeedbackCompleted;
                Destroy(target.gameObject);
            }
        }

        _feedbackTargets.Clear();
    }

    private void NotifyUI()
    {
        OnProgressChanged?.Invoke(_completedObjectives, objectivesToComplete);
    }
}
