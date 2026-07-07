using System;
using UnityEngine;

public class DuckBehaviour : MonoBehaviour
{
    public event Action<DuckBehaviour> OnReachedDestination;
    public event Action<DuckBehaviour> OnHit;

    // Ya no las exponemos en el inspector, el pato las calcula internamente
    private Vector3 startPoint;
    private Vector3 endPoint;
    private int floor;
    private float duration;

    private float elapsedTime;
    private bool isMoving;
    private bool isHit = false;

    public void Initialize(SpawnSide side, float duration, Vector3 leftWorldBound, Vector3 rightWorldBound)
    {
        this.duration = Mathf.Max(0.01f, duration);
        //this.floor = floor;
        // El pato abstrae su origen y destino basado en el enum
        if (side == SpawnSide.Left)
        {
            startPoint = leftWorldBound;
            endPoint = rightWorldBound;
        }
        else // SpawnSide.Right
        {
            startPoint = rightWorldBound;
            endPoint = leftWorldBound;
        }

        transform.position = startPoint;

        elapsedTime = 0f;
        isMoving = true;
        isHit = false;
    }

    private void Update()
    {
        if (!isMoving)
            return;

        elapsedTime += Time.deltaTime;

        float t = Mathf.Clamp01(elapsedTime / duration);

        transform.position = Vector3.Lerp(startPoint, endPoint, t);

        if (t >= 1f)
        {
            isMoving = false;
            OnReachedDestination?.Invoke(this);
        }
    }

    public void Hit()
    {
        if (isHit)
            return;

        isHit = true;
        isMoving = false;
        Debug.Log("¡PATO CAZADO!");

        OnHit?.Invoke(this);
    }
}