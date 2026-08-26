using System;
using System.Collections;
using DG.Tweening;
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
    private HandType requiredHand;
    private float elapsedTime;
    private float spawnTime;
    private float reactionTime;
    private bool hasReactionTime;
    private bool isMoving;
    private bool isHit = false;
    private bool isMissed;
    private HandType hitHand;
    [SerializeField] private Renderer body;
    [SerializeField] private Renderer wings;

    [Header("Feedback visual")]
    [Tooltip("Duración del fade de entrada al aparecer.")]
    [SerializeField, Min(0f)] private float spawnFadeDuration = 0.25f;
    [Tooltip("Duración del fade de salida cuando el pato escapa.")]
    [SerializeField, Min(0f)] private float escapeFadeDuration = 0.35f;
    [Tooltip("Duración del aplastamiento inicial al ser cazado.")]
    [SerializeField, Min(0f)] private float squashDuration = 0.15f;
    [Tooltip("Duración del rebote elástico tras el aplastamiento.")]
    [SerializeField, Min(0f)] private float stretchDuration = 0.25f;
    [Tooltip("Duración del desvanecimiento final; la secuencia completa al ser cazado dura la suma de las tres fases (por defecto 1s).")]
    [SerializeField, Min(0f)] private float vanishDuration = 0.6f;
    [Tooltip("Escala del aplastamiento inicial.")]
    [SerializeField] private Vector3 squashScale = new Vector3(1.3f, 0.55f, 1.3f);
    [Tooltip("Escala del rebote elástico.")]
    [SerializeField] private Vector3 stretchScale = new Vector3(0.85f, 1.2f, 0.85f);
    [Tooltip("Escala uniforme final antes de desaparecer.")]
    [SerializeField, Range(0f, 1f)] private float vanishedScale = 0.05f;
    [Tooltip("Partículas instanciadas en la posición del pato al ser cazado.")]
    [SerializeField] private ParticleSystem deathEffectPrefab;
    [Tooltip("Vida máxima del efecto de partículas; debe superar la duración total del sistema.")]
    [SerializeField, Min(0.1f)] private float deathEffectLifetime = 2f;

    // Margen extra antes de la autodestrucción para cubrir imprecisions de los tweens.
    private const float DespawnSafetyMargin = 0.15f;

    private Material bodyMaterial;
    private Material wingsMaterial;
    private Color bodyBaseColor = Color.white;
    private Color wingsBaseColor = Color.white;
    private Collider interactionCollider;
    private float currentAlpha = 1f;
    private bool isDespawning;
    private Tween spawnFadeTween;
    private Tween despawnTween;
    private Sequence deathSequence;

    void Awake()
    {
        if (body == null)
            body = GetComponent<Renderer>();

        if (body != null)
            bodyMaterial = body.material;

        if (wings != null)
            wingsMaterial = wings.material;

        TryGetComponent(out interactionCollider);
    }

    public float SpawnTime => spawnTime;
    public float ReactionTime => reactionTime;
    public float AvailableTime => duration;
    public bool IsHit => isHit;
    public bool IsMissed => isMissed;
    public HandType RequiredHand => requiredHand;
    public HandType HitHand => hitHand;
    public bool HasReactionTime => hasReactionTime;

    public void Initialize(SpawnSide side, HandType requiredHand, float duration, Vector3 leftWorldBound, Vector3 rightWorldBound)
    {
        this.duration = Mathf.Max(0.01f, duration);
        this.requiredHand = requiredHand;
        //this.floor = floor;
        // El pato abstrae su origen y destino basado en el enum
        if (side == SpawnSide.Left)
        {
            startPoint = leftWorldBound;
            endPoint = rightWorldBound;
            transform.rotation = Quaternion.Euler(0, 90, 0);
        }
        else if (side == SpawnSide.Right)
        {
            startPoint = rightWorldBound;
            endPoint = leftWorldBound;
            transform.rotation = Quaternion.Euler(0, -90, 0);

        }
        else //SpawnSide.Center
        {
            Vector3 worldCenter = new(0, rightWorldBound.y, rightWorldBound.z);
            startPoint = worldCenter;
            endPoint = worldCenter;
            transform.rotation = Quaternion.Euler(0, -90, 0);

        }

        transform.position = startPoint;

        elapsedTime = 0f;
        spawnTime = Time.time;
        reactionTime = 0f;
        hasReactionTime = false;
        isMoving = true;
        isHit = false;
        isMissed = false;
        switch (this.requiredHand)
        {
            case HandType.NONE:
                SetPieceColor(HandsColor.Default);
                break;
            case HandType.LEFT:
                SetPieceColor(HandsColor.Left);
                break;
            case HandType.RIGHT:
                SetPieceColor(HandsColor.Right);
                break;
        }

        isDespawning = false;
        StartSpawnFade();
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
            if (!isHit)
                isMissed = true;
            OnReachedDestination?.Invoke(this);
        }
    }

    public void Hit(HandType requiredHand)
    {
        if (this.requiredHand != requiredHand && this.requiredHand != HandType.NONE)
            return;

        if (isHit || isDespawning)
            return;

        isHit = true;
        hitHand = requiredHand;
        reactionTime = Mathf.Max(0f, Time.time - spawnTime);
        hasReactionTime = ScoreMath.IsFinite(reactionTime);
        if (!hasReactionTime)
            reactionTime = 0f;
        isMoving = false;

        OnHit?.Invoke(this);
    }

    // El runner entrega el pato aquí; con animación reproduce su salida y se autodestruye.
    public void BeginDespawn(bool animateDespawn)
    {
        if (isDespawning)
            return;

        isDespawning = true;
        isMoving = false;

        if (interactionCollider != null)
            interactionCollider.enabled = false;

        spawnFadeTween?.Kill();
        spawnFadeTween = null;

        if (!animateDespawn)
        {
            Destroy(gameObject);
            return;
        }

        float visualDuration = isHit ? PlayHitDespawnSequence() : PlayEscapeDespawn();

        if (visualDuration <= 0f)
        {
            ApplyAlpha(0f);
            Destroy(gameObject);
            return;
        }

        StartCoroutine(DestroyAfterMaximumLifetime(visualDuration + DespawnSafetyMargin));
    }

    private void StartSpawnFade()
    {
        spawnFadeTween?.Kill();
        spawnFadeTween = null;

        if (spawnFadeDuration <= 0f)
        {
            ApplyAlpha(1f);
            return;
        }

        ApplyAlpha(0f);
        spawnFadeTween = DOTween
            .To(GetCurrentAlpha, ApplyAlpha, 1f, spawnFadeDuration)
            .SetEase(Ease.OutCubic);
    }

    private float PlayEscapeDespawn()
    {
        if (escapeFadeDuration <= 0f)
            return 0f;

        despawnTween?.Kill();
        despawnTween = DOTween
            .To(GetCurrentAlpha, ApplyAlpha, 0f, escapeFadeDuration)
            .SetEase(Ease.OutCubic);
        return escapeFadeDuration;
    }

    private float PlayHitDespawnSequence()
    {
        float totalDuration = squashDuration + stretchDuration + vanishDuration;
        if (totalDuration <= 0f)
            return 0f;

        SpawnDeathEffect();

        deathSequence?.Kill();
        deathSequence = DOTween.Sequence();
        deathSequence.Append(transform
            .DOScale(squashScale, squashDuration)
            .SetEase(Ease.OutQuad));
        deathSequence.Append(transform
            .DOScale(stretchScale, stretchDuration)
            .SetEase(Ease.OutQuad));
        deathSequence.Append(transform
            .DOScale(Vector3.one * vanishedScale, vanishDuration)
            .SetEase(Ease.InQuad));
        deathSequence.Join(DOTween
            .To(GetCurrentAlpha, ApplyAlpha, 0f, vanishDuration)
            .SetEase(Ease.InQuad));
        return totalDuration;
    }

    private void SpawnDeathEffect()
    {
        if (deathEffectPrefab == null)
            return;

        ParticleSystem effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        // El prefab tiene playOnAwake desactivado; la reproducción es explícita.
        effect.Play(true);
        // La vida programada sobrevive a la destrucción del pato.
        Destroy(effect.gameObject, deathEffectLifetime);
    }

    private IEnumerator DestroyAfterMaximumLifetime(float maximumDuration)
    {
        yield return new WaitForSeconds(maximumDuration);
        Destroy(gameObject);
    }

    private void SetPieceColor(Color color)
    {
        bodyBaseColor = color;
        wingsBaseColor = color;
        ApplyColorAndAlpha();
    }

    private float GetCurrentAlpha()
    {
        return currentAlpha;
    }

    private void ApplyAlpha(float alpha)
    {
        currentAlpha = Mathf.Clamp01(alpha);
        ApplyColorAndAlpha();
    }

    private void ApplyColorAndAlpha()
    {
        if (bodyMaterial != null)
            bodyMaterial.color = new Color(bodyBaseColor.r, bodyBaseColor.g, bodyBaseColor.b, currentAlpha);

        if (wingsMaterial != null)
            wingsMaterial.color = new Color(wingsBaseColor.r, wingsBaseColor.g, wingsBaseColor.b, currentAlpha);
    }

    private void OnDisable()
    {
        spawnFadeTween?.Kill();
        despawnTween?.Kill();
        deathSequence?.Kill();
        spawnFadeTween = null;
        despawnTween = null;
        deathSequence = null;
    }
}
