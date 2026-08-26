using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public abstract class DotBehaviour : MonoBehaviour
{
    public float timeToInteract = 3f;
    public float hitRadius = .05f;

    [SerializeField] private float fadeDuration = .2f;
    [Tooltip("Duracion total del pop de escala al tocar el objetivo.")]
    [SerializeField, Min(0f)] private float hitPopDuration = .35f;
    [Tooltip("Crecimiento del objetivo en el pico del pop (0.15 = +15%).")]
    [SerializeField, Min(0f)] private float hitPopAmplitude = .15f;
    [Tooltip("Escala con la que nace el halo respecto al radio de deteccion (2 = doble de radio). Con 1 queda desactivado.")]
    [SerializeField, Min(1f)] private float haloStartScale = 2f;
    [Tooltip("Opacidad del halo durante el cierre hacia el objetivo.")]
    [SerializeField, Range(0f, 1f)] private float haloAlpha = .35f;
    [Tooltip("Duracion de la expansion y desvanecimiento del halo al tocar el objetivo.")]
    [SerializeField, Min(0f)] private float haloDismissDuration = .15f;
    [Tooltip("Sprite circunferencial blanco del halo; si falta, se genera uno proceduralmente.")]
    [SerializeField] private Sprite haloSprite;

    public bool IsHitted { get; protected set; }

    [SerializeField] protected SpriteRenderer bg;
    [SerializeField] protected MeshRenderer ring;

    private Material ringMaterial;
    private Vector3 dotBaseScale;

    // Detras del anillo para evitar ambiguedad de orden con materiales transparentes.
    private const float HaloDepthOffset = 0.001f;

    private static Sprite generatedCircumferenceSprite;

    private Tween hitPopTween;
    private Tween haloTween;
    private Transform haloTransform;
    private SpriteRenderer haloRenderer;
    private Vector3 haloBaseScale;

    public HandType requiredHand = HandType.NONE;

    public event Action<DotBehaviour> OnCompleted;
    public event Action<DotBehaviour> OnMissed;
    public event Action<DotBehaviour> OnFailed;
    public event Action<DotBehaviour> OnTouched;

    protected virtual void Awake()
    {
        ringMaterial = ring.material;
        dotBaseScale = transform.localScale;

        CreateApproachHalo();
    }

    protected virtual void Start()
    {
        StartCoroutine(Fade(0, 1));
        StartCoroutine(WaitForInteraction());
        StartApproachHalo();
    }

    public virtual void Hit()
    {
        if (IsHitted)
            return;

        IsHitted = true;

        PlayHitPop();
        DismissApproachHalo();

        OnTouched?.Invoke(this);
    }


    protected void Complete()
    {
        StartCoroutine(CompleteRoutine());
    }

    IEnumerator CompleteRoutine()
    {
        yield return Fade(1, 0);

        OnCompleted?.Invoke(this);
    }

    protected void Fail()
    {
        Debug.Log("[ScoreSystem][OSU] Objetivo fallo: " + name);
        OnFailed?.Invoke(this);
    }

    protected IEnumerator WaitForInteraction()
    {
        float time = 0;

        while (time < timeToInteract && !IsHitted)
        {
            time += Time.deltaTime;
            yield return null;
        }

        if (!IsHitted)
        {
            OnMissed?.Invoke(this);
        }
    }

    public void SetColor(HandType hand)
    {
        requiredHand = hand;

        Color color = hand switch
        {
            HandType.LEFT => HandsColor.Left,
            HandType.RIGHT => HandsColor.Right,
            _ => HandsColor.Default
        };

        color.a = .6f;

        bg.color = color;

        if (ringMaterial != null)
        {
            ringMaterial.color = color;
        }

        ApplyHaloColor();
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0;

        Color bgColor = bg.color;
        Color ringColor = ringMaterial.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;

            float alpha =
                Mathf.Lerp(from, to, t / fadeDuration);

            bgColor.a = alpha * .6f;
            ringColor.a = alpha;

            bg.color = bgColor;
            ringMaterial.color = ringColor;

            yield return null;
        }

        bgColor.a = to * .6f;
        ringColor.a = to;

        bg.color = bgColor;
        ringMaterial.color = ringColor;
    }

    private void CreateApproachHalo()
    {
        Sprite sprite = haloSprite != null ? haloSprite : GetOrCreateCircumferenceSprite();
        if (sprite == null)
            return;

        GameObject halo = new GameObject("ApproachHalo");

        haloTransform = halo.transform;
        haloTransform.SetParent(transform, false);
        haloTransform.localPosition =
            ring.transform.localPosition + Vector3.back * HaloDepthOffset;

        haloRenderer = halo.AddComponent<SpriteRenderer>();
        haloRenderer.sprite = sprite;

        float lossyScale = Mathf.Abs(transform.lossyScale.x);
        float spriteSize = sprite.bounds.size.x;

        if (lossyScale <= 0f || spriteSize <= 0f)
        {
            Destroy(halo);
            haloTransform = null;
            haloRenderer = null;
            return;
        }

        // El halo termina cerrandose sobre el radio de deteccion, no sobre el visual del anillo.
        haloBaseScale = Vector3.one * (hitRadius * 2f / spriteSize / lossyScale);
        haloTransform.localScale = haloBaseScale * haloStartScale;

        ApplyHaloColor();
    }

    private static Sprite GetOrCreateCircumferenceSprite()
    {
        if (generatedCircumferenceSprite != null)
            return generatedCircumferenceSprite;

        const int Resolution = 256;
        const float ThicknessFraction = 0.05f;
        const float AntiAliasPadding = 1f;

        Texture2D texture = new Texture2D(
            Resolution,
            Resolution,
            TextureFormat.RGBA32,
            false);

        float half = (Resolution - 1) * 0.5f;
        float outerRadius = half;
        float innerRadius =
            outerRadius - Mathf.Max(2f, Resolution * ThicknessFraction);
        Color[] pixels = new Color[Resolution * Resolution];

        for (int y = 0; y < Resolution; y++)
        {
            for (int x = 0; x < Resolution; x++)
            {
                float dx = x - half;
                float dy = y - half;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                float innerCoverage =
                    Mathf.Clamp01(distance - innerRadius + AntiAliasPadding);
                float outerCoverage =
                    Mathf.Clamp01(outerRadius - distance + AntiAliasPadding);

                pixels[y * Resolution + x] =
                    new Color(1f, 1f, 1f, Mathf.Min(innerCoverage, outerCoverage));
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);

        generatedCircumferenceSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, Resolution, Resolution),
            new Vector2(0.5f, 0.5f),
            Resolution * 0.25f);

        return generatedCircumferenceSprite;
    }

    private void StartApproachHalo()
    {
        if (haloTransform == null)
            return;

        if (timeToInteract <= 0f || haloStartScale <= 1f)
        {
            haloTransform.gameObject.SetActive(false);
            return;
        }

        haloTween = haloTransform
            .DOScale(haloBaseScale, timeToInteract)
            .SetEase(Ease.Linear);
    }

    private void DismissApproachHalo()
    {
        if (haloTransform == null || !haloTransform.gameObject.activeSelf)
            return;

        haloTween?.Kill();
        haloTween = null;

        if (haloDismissDuration <= 0f)
        {
            haloTransform.gameObject.SetActive(false);
            return;
        }

        Sequence exitSequence = DOTween.Sequence();
        exitSequence.Join(
            haloTransform
                .DOScale(haloTransform.localScale * 1.2f, haloDismissDuration)
                .SetEase(Ease.OutQuad));
        exitSequence.Join(
            haloRenderer
                .DOFade(0f, haloDismissDuration)
                .SetEase(Ease.OutQuad));
        exitSequence.OnComplete(HideHalo);

        haloTween = exitSequence;
    }

    private void HideHalo()
    {
        haloTween = null;

        if (haloTransform != null)
            haloTransform.gameObject.SetActive(false);
    }

    private void PlayHitPop()
    {
        if (hitPopDuration <= 0f || hitPopAmplitude <= 0f)
            return;

        hitPopTween?.Kill();

        const float growFraction = 0.35f;

        Sequence popSequence = DOTween.Sequence();
        popSequence.Append(
            transform
                .DOScale(dotBaseScale * (1f + hitPopAmplitude), hitPopDuration * growFraction)
                .SetEase(Ease.OutQuad));
        popSequence.Append(
            transform
                .DOScale(dotBaseScale, hitPopDuration * (1f - growFraction))
                .SetEase(Ease.InOutQuad));

        hitPopTween = popSequence;
    }

    private void ApplyHaloColor()
    {
        if (haloRenderer == null || ringMaterial == null)
            return;

        Color haloColor = ringMaterial.color;
        haloColor.a = haloAlpha;
        haloRenderer.color = haloColor;
    }

    private void OnDestroy()
    {
        hitPopTween?.Kill();
        haloTween?.Kill();
    }
}
