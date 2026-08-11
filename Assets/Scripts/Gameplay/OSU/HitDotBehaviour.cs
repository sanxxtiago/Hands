using System.Collections;
using UnityEngine;

public class HitDotBehaviour : DotBehaviour
{

    public override void Hit()
    {
        base.Hit();

        Complete();

        Destroy(gameObject, .5f);
    }
}