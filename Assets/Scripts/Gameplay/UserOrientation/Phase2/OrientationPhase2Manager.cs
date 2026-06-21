using System;
using System.Collections.Generic;
using UnityEngine;

public class OrientationPhase2Manager : OrientationManager
{
    [Header("Configuration")]
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private int objectivesToComplete = 4;

    public event Action<int, int> OnProgressChanged;

    private int _currentSpawnIndex;
    private int _completedObjectives;
    private OrientationTarget _activeTarget;

    private void Start()
    {
        objectivesToComplete = Mathf.Min(
            objectivesToComplete,
            spawnPoints.Count);

        _completedObjectives = 0;

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

        Transform point = spawnPoints[_currentSpawnIndex];

        GameObject instance = Instantiate(
            targetPrefab,
            point.position,
            point.rotation);

        _activeTarget = instance.GetComponent<OrientationTarget>();

        _activeTarget.OnTouched += HandleTargetTouched;

        _currentSpawnIndex++;
    }

    private void HandleTargetTouched()
    {
        _activeTarget.OnTouched -= HandleTargetTouched;

        Destroy(_activeTarget.gameObject);

        _completedObjectives++;

        NotifyUI();

        SpawnNextTarget();
    }

    private void NotifyUI()
    {
        OnProgressChanged?.Invoke(_completedObjectives, objectivesToComplete);
    }
}