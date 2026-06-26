using UnityEngine;

public class HitDotBehaviour : DotBehaviour
{
    public override void Hit()
    {
        if (IsHitted) return;

        IsHitted = true;

        Complete();

        Destroy(gameObject, .3f);
    }
}