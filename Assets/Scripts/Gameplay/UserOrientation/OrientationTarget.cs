using System;
using DG.Tweening;
using UnityEngine;

public class OrientationTarget : MonoBehaviour
{
    public event Action OnTouched;
    [SerializeField] private float floatHeight = 0.03f;
    [SerializeField] private float duration = 1f;

    private Vector3 initialPosition;
    private Tween floatingTween;
    private void OnEnable()
    {
        floatingTween = transform
            .DOLocalMoveY(initialPosition.y + floatHeight, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void Awake()
    {
        initialPosition = transform.localPosition;
    }

    private void OnCollisionEnter(Collision other)
    {
        OnTouched?.Invoke();
    }


    private void OnDisable()
    {
        floatingTween?.Kill();
        transform.localPosition = initialPosition;
    }
}