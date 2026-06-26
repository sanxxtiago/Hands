using System;
using UnityEngine;

public abstract class DotBehaviour : MonoBehaviour
{
    public float lifeTime = 3;
    public float hitRadius = .1f;

    public bool IsHitted { get; protected set; }

    [SerializeField]
    protected SpriteRenderer bg;

    public event Action<DotBehaviour> OnCompleted;
    public event Action<DotBehaviour> OnFailed;

    public abstract void Hit();

    protected void Complete()
    {
        Debug.Log($"{name} Complete");
        OnCompleted?.Invoke(this);
    }

    protected void Fail()
    {
        Debug.Log($"{name} Failed");
        OnFailed?.Invoke(this);
    }

    public void SetColor(HandType hand)
    {
        Color color = hand switch
        {
            HandType.LEFT => HandsColor.Left,
            HandType.RIGHT => HandsColor.Right,
            _ => HandsColor.Default
        };

        color.a = 0.6f;

        bg.color = color;
    }
}