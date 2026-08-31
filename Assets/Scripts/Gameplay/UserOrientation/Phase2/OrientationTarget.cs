using System;
using DG.Tweening;
using UnityEngine;

public class OrientationTarget : MonoBehaviour
{
    public event Action<OrientationTarget> OnTouchDetected;
    public event Action<OrientationTarget> OnTouchFeedbackCompleted;

    [SerializeField] private float floatHeight = 0.03f;
    [SerializeField] private float duration = 1f;
    [SerializeField] private LayerMask touchLayers;

    [Header("Touch Feedback")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private SpriteRenderer targetHalo;
    [SerializeField] private Color touchedColor = new Color(0.75f, 0.9f, 1f, 1f);
    [SerializeField, Min(0f)] private float punchScale = 0.12f;
    [SerializeField, Min(0f)] private float punchDuration = 0.2f;
    [SerializeField, Min(0f)] private float highlightDuration = 0.15f;
    [SerializeField] private float disappearDuration = 0.3f;

    private Vector3 initialPosition;
    private Vector3 initialScale;
    private Vector3 initialHaloScale;
    private Tween floatingTween;
    private Tween touchFeedbackTween;
    private Tween haloScaleTween;
    private Tween haloAlphaTween;
    private bool touched;

    private Material materialInstance;
    private Color initialMaterialColor;
    private Color initialHaloColor;

    private void Awake()
    {
        initialPosition = transform.localPosition;
        initialScale = transform.localScale;

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (targetHalo == null)
        {
            Transform haloTransform = transform.Find("TargetHalo");
            if (haloTransform != null)
                targetHalo = haloTransform.GetComponent<SpriteRenderer>();
        }

        if (targetHalo != null)
        {
            initialHaloScale = targetHalo.transform.localScale;
            initialHaloColor = targetHalo.color;
        }

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

        if (targetHalo != null)
        {
            targetHalo.transform.localScale = initialHaloScale;
            targetHalo.color = initialHaloColor;
        }

        floatingTween = transform
            .DOLocalMoveY(initialPosition.y + floatHeight, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        StartHaloFeedback();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (touched || !IsTouchLayer(other.collider.gameObject.layer))
            return;

        touched = true;

        floatingTween?.Kill();
        touchFeedbackTween?.Kill();
        haloScaleTween?.Kill();
        haloAlphaTween?.Kill();

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
            OnTouchFeedbackCompleted?.Invoke(this);
        });

        touchFeedbackTween = feedback;

        OnTouchDetected?.Invoke(this);
    }

    private void OnDisable()
    {
        floatingTween?.Kill();
        touchFeedbackTween?.Kill();
        haloScaleTween?.Kill();
        haloAlphaTween?.Kill();
        floatingTween = null;
        touchFeedbackTween = null;
        haloScaleTween = null;
        haloAlphaTween = null;

        if (materialInstance != null && materialInstance.HasProperty("_BaseColor"))
            materialInstance.SetColor("_BaseColor", initialMaterialColor);

        transform.localScale = initialScale;
        transform.localPosition = initialPosition;

        if (targetHalo != null)
        {
            targetHalo.transform.localScale = initialHaloScale;
            targetHalo.color = initialHaloColor;
        }
    }

    private void OnDestroy()
    {
        if (materialInstance != null)
            Destroy(materialInstance);
    }

    private void StartHaloFeedback()
    {
        if (targetHalo == null)
            return;

        haloScaleTween = targetHalo.transform
            .DOScale(initialHaloScale * 1.08f, 0.8f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        haloAlphaTween = targetHalo
            .DOFade(0.22f, 0.8f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private bool IsTouchLayer(int layer)
    {
        return (touchLayers.value & (1 << layer)) != 0;
    }
}
