using Leap;
using UnityEngine;

public class GameplayHandInput : MonoBehaviour
{
    public LeapDataProvider dataProvider;
    public GameplayHandData? LeftHandData { get; private set; }
    public GameplayHandData? RightHandData { get; private set; }

    void Update()
    {
        Frame currentFrame = dataProvider.GetCurrentFrame();
        if (currentFrame == null) return;

        LeftHandData = null;
        RightHandData = null;

        foreach (Hand hand in currentFrame.Hands)
        {
            if (hand.IsLeft)
            {
                LeftHandData = new GameplayHandData(hand.PalmPosition, hand.Rotation, hand.PalmVelocity);
            }
            else
            {
                RightHandData = new GameplayHandData(hand.PalmPosition, hand.Rotation, hand.PalmVelocity);
            }
        }
    }

    public GameplayHandData? GetHand(HandType handType, Vector3? targetPosition = null)
    {
        switch (handType)
        {
            case HandType.LEFT:
                return LeftHandData;

            case HandType.RIGHT:
                return RightHandData;

            case HandType.NONE:
                return GetClosestHand(targetPosition);

            default:
                return null;
        }
    }

    private GameplayHandData? GetClosestHand(Vector3? targetPosition)
    {
        if (targetPosition == null)
            return LeftHandData ?? RightHandData;

        if (LeftHandData == null)
            return RightHandData;

        if (RightHandData == null)
            return LeftHandData;

        float leftDistance = Vector3.Distance(
            LeftHandData.Value.position,
            targetPosition.Value);

        float rightDistance = Vector3.Distance(
            RightHandData.Value.position,
            targetPosition.Value);

        return leftDistance <= rightDistance
            ? LeftHandData
            : RightHandData;
    }
}
