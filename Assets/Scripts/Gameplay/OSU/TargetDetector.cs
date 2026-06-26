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
            Vector3? handPos =
                GetActiveHandPosition(trackingDot.transform.position);

            if (handPos != null)
            {
                trackingDot.Track(handPos.Value);
            }
        }
    }
    private void TryHit(DotBehaviour dot)
    {

        if (!dot.IsHitted && input.LeftHandData != null)
        {
            if (dot.requiredHand == HandType.RIGHT)
                return;

            Vector3 leftPos = input.LeftHandData.Value.position;

            if (Vector3.Distance(leftPos, dot.transform.position) <= dot.hitRadius)
            {
                dot.Hit();
            }
        }

        if (!dot.IsHitted && input.RightHandData != null)
        {
            if (dot.requiredHand == HandType.LEFT)
                return;

            Vector3 rightPos = input.RightHandData.Value.position;

            if (Vector3.Distance(rightPos, dot.transform.position) <= dot.hitRadius)
            {
                dot.Hit();
            }
        }

    }



    Vector3? GetActiveHandPosition(Vector3 target)
    {
        if (input.RightHandData == null && input.LeftHandData == null) return null;
        float rightDistance = 0;
        float leftDistance = 0;

        if (input.RightHandData != null)
            rightDistance = Vector3.Distance(target, input.RightHandData.Value.position);

        if (input.LeftHandData != null)
            leftDistance = Vector3.Distance(target, input.LeftHandData.Value.position);
        if (rightDistance == 0) return input.LeftHandData.Value.position;

        if (leftDistance == 0) return input.RightHandData.Value.position;


        return (leftDistance < rightDistance) ? input.LeftHandData.Value.position : input.RightHandData.Value.position;
    }
}