using System;
using DG.Tweening;
using UnityEngine;

public class OrientationTarget : MonoBehaviour
{
    public event Action OnTouched;

    [SerializeField] private float floatHeight = 0.03f;
    [SerializeField] private float duration = 1f;

    [Header("Touch Feedback")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color touchedColor = Color.green;
    [SerializeField] private float disappearDuration = 0.3f;

    private Vector3 initialPosition;
    private Tween floatingTween;
    private bool touched;

    private Material materialInstance;

    private void Awake()
    {
        initialPosition = transform.localPosition;

        if (targetRenderer != null)
        {
            materialInstance = Instantiate(targetRenderer.material);
            targetRenderer.material = materialInstance;
        }
    }

    private void OnEnable()
    {
        touched = false;
        transform.localPosition = initialPosition;

        floatingTween = transform
            .DOLocalMoveY(initialPosition.y + floatHeight, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (touched)
            return;

        touched = true;

        floatingTween?.Kill();

        Sequence feedback = DOTween.Sequence();

        if (materialInstance != null)
        {
            feedback.Join(
                materialInstance.DOColor(
                    touchedColor,
                    "_BaseColor",
                    0.15f
                )
            );
        }

        feedback.Join(
            transform.DOScale(
                Vector3.zero,
                disappearDuration
            )
            .SetEase(Ease.InBack)
        );

        feedback.OnComplete(() =>
        {
            OnTouched?.Invoke();
        });
    }

    private void OnDisable()
    {
        floatingTween?.Kill();

        transform.localScale = Vector3.one;
        transform.localPosition = initialPosition;
    }
}