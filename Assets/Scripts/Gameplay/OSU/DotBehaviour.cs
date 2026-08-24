using System;
using System.Collections;
using UnityEngine;

public abstract class DotBehaviour : MonoBehaviour
{
    public float timeToInteract = 3f;
    public float hitRadius = .05f;

    [SerializeField] private float fadeDuration = .2f;

    public bool IsHitted { get; protected set; }

    [SerializeField] protected SpriteRenderer bg;
    [SerializeField] protected MeshRenderer ring;

    private Material ringMaterial;

    public HandType requiredHand = HandType.NONE;

    public event Action<DotBehaviour> OnCompleted;
    public event Action<DotBehaviour> OnMissed;
    public event Action<DotBehaviour> OnFailed;
    public event Action<DotBehaviour> OnTouched;

    protected virtual void Awake()
    {
        ringMaterial = ring.material;
    }

    protected virtual void Start()
    {
        StartCoroutine(Fade(0, 1));
        StartCoroutine(WaitForInteraction());
    }

    public virtual void Hit()
    {
        if (IsHitted)
            return;

        IsHitted = true;

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
}
