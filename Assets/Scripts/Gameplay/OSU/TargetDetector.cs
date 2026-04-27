using UnityEngine;

public class TargetDetector : MonoBehaviour
{
    public GameplayHandInput input;
    public DotBehaviour target;
    void Update()
    {
        if (target == null) return;

        if (!target.IsHitted)
        {
            TryHit(target);
        }
        else if (target.IsTrackable && target.IsHitted)
        {
            TrackDot(target);
        }
    }

    private void TryHit(DotBehaviour dot)
    {

        if (!dot.IsHitted && input.LeftHandData != null)
        {
            Vector3 leftPos = input.LeftHandData.Value.position;

            if (Vector3.Distance(leftPos, dot.transform.position) <= dot.hitRadius)
            {
                dot.Hit();
            }
        }

        if (!dot.IsHitted && input.RightHandData != null)
        {
            Vector3 rightPos = input.RightHandData.Value.position;

            if (Vector3.Distance(rightPos, dot.transform.position) <= dot.hitRadius)
            {
                dot.Hit();
            }
        }

    }

    void TrackDot(DotBehaviour dot)
    {
        Vector3? handPos = GetActiveHandPosition(dot.transform.position);

        if (handPos == null) return;

        float dist = Vector3.Distance(handPos.Value, dot.transform.position);

        if (dist <= dot.followRadius)
        {
            dot.timeOutside = 0f;
            dot.SetTrackingState(true);
        }
        else
        {
            dot.timeOutside += Time.deltaTime;
            dot.SetTrackingState(false);

            if (dot.timeOutside > 0.3f)
            {
                dot.Fail();
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