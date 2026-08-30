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
    [SerializeField] private Color touchedColor = new Color(0.75f, 0.9f, 1f, 1f);
    [SerializeField, Min(0f)] private float punchScale = 0.12f;
    [SerializeField, Min(0f)] private float punchDuration = 0.2f;
    [SerializeField, Min(0f)] private float highlightDuration = 0.15f;
    [SerializeField] private float disappearDuration = 0.3f;

    private Vector3 initialPosition;
    private Vector3 initialScale;
    private Tween floatingTween;
    private Tween touchFeedbackTween;
    private bool touched;

    private Material materialInstance;
    private Color initialMaterialColor;

    private void Awake()
    {
        initialPosition = transform.localPosition;
        initialScale = transform.localScale;

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (targetRenderer != null)
        {
            materialInstance = Instantiate(targetRenderer.material);
            targetRenderer.material = materialInstance;

            if (materialInstance.HasProperty("_BaseColor"))
                initialMaterialColor = materialInstance.GetColor("_BaseColor");
        }
    }

    private void OnEnable()
    {
        touched = false;
        transform.localPosition = initialPosition;
        transform.localScale = initialScale;

        if (materialInstance != null && materialInstance.HasProperty("_BaseColor"))
            materialInstance.SetColor("_BaseColor", initialMaterialColor);

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
        touchFeedbackTween?.Kill();

        Sequence feedback = DOTween.Sequence();

        feedback.Append(
            transform.DOPunchScale(
                new Vector3(punchScale, punchScale, punchScale),
                punchDuration,
                1,
                0.5f
            )
        );

        if (materialInstance != null)
        {
            feedback.Insert(
                0f,
                materialInstance.DOColor(
                    touchedColor,
                    "_BaseColor",
                    highlightDuration
                )
            );
        }

        feedback.Append(
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

        touchFeedbackTween = feedback;
    }

    private void OnDisable()
    {
        floatingTween?.Kill();
        touchFeedbackTween?.Kill();
        floatingTween = null;
        touchFeedbackTween = null;

        if (materialInstance != null && materialInstance.HasProperty("_BaseColor"))
            materialInstance.SetColor("_BaseColor", initialMaterialColor);

        transform.localScale = initialScale;
        transform.localPosition = initialPosition;
    }
}
