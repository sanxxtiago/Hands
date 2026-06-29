using UnityEngine;

public class TargetDetector : MonoBehaviour
{
    public GameplayHandInput input;
    public DotBehaviour target;
    void Update()
    {
        if (target == null)
            return;

        if (!target.IsHitted)
        {
            TryHit(target);
            return;
        }

        if (target is TrackingDotBehaviour trackingDot)
        {
            GameplayHandData? hand = input.GetHand(trackingDot.requiredHand, trackingDot.transform.position);

            if (hand != null)
            {
                trackingDot.Track(hand.Value);
            }
        }
    }
    private void TryHit(DotBehaviour dot)
    {
        GameplayHandData? hand = input.GetHand(dot.requiredHand, dot.transform.position);

        if (hand == null)
            return;

        if (Vector3.Distance(
            hand.Value.position,
            dot.transform.position) <= dot.hitRadius)
        {
            dot.Hit();
        }
    }

}