using System;
using UnityEngine;

public class DuckBehaviour : MonoBehaviour
{
    public event Action<DuckBehaviour> OnReachedDestination;
    public event Action<DuckBehaviour> OnHit;

    [SerializeField] private Vector3 startPoint;
    [SerializeField] private Vector3 endPoint;
    [SerializeField] private float duration;
    private float elapsedTime;

    [SerializeField] private bool isMoving;
    private bool isHit = false;

    public void Initialize(Vector3 startPoint, Vector3 endPoint, float duration)
    {
        this.startPoint = startPoint;
        this.endPoint = endPoint;
        this.duration = Mathf.Max(0.01f, duration);

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
            //isMoving = false;
            t = 0;
            transform.position = startPoint;
            OnReachedDestination?.Invoke(this);

        }
    }

    public void Hit()
    {
        if (isHit)
            return;
        //QUITAR COMENTARIO
        //isHit = true;
        isMoving = false;
        Debug.Log("DUCK HITTED!");
        OnHit?.Invoke(this);
    }
}