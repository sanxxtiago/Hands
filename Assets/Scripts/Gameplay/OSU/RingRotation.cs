using UnityEngine;

public class RingRotation : MonoBehaviour
{
    [SerializeField] private float ringRotationSpeed = 10f;

    void Update()
    {
        transform.Rotate(Time.deltaTime * ringRotationSpeed * Vector3.up);

    }
}
