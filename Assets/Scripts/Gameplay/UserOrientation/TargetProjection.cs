using UnityEngine;

public class TargetProjection : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform projection;

    [Header("Table")]
    [SerializeField] private float tablePlanePosition;
    private void Update()
    {
        UpdateProjection();
    }

    private void UpdateProjection()
    {
        Vector3 targetPos = target.position;

        projection.position = new Vector3(
            targetPos.x,
            tablePlanePosition,
            targetPos.z);
    }

   
}