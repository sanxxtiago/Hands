using UnityEngine;

[System.Serializable]
public class OSUStep
{
    [SerializeField] private string stepId;
    public HandType requiredHand;
    public GameObject prefab;
    public Vector3 spawnPosition;
    public PathData path;
    public bool IsTrackingStep => path != null;
}