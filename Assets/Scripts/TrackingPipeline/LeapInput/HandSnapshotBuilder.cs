using Leap;
using UnityEngine;

public class HandSnapshotBuilder
{
    public HandDataSnapshot?[] Build(Frame frame)
    {
        HandDataSnapshot left = default;
        HandDataSnapshot right = default;

        foreach (Hand hand in frame.Hands)
        {
            var snapshot = new HandDataSnapshot
            {
                frameId = frame.Id,
                timestamp = Time.time,

                handType = hand.IsLeft ? HandType.LEFT : HandType.RIGHT,

                palmPosition = hand.PalmPosition,
                wristPosition = hand.WristPosition,
                elbowPosition = hand.Arm.ElbowPosition,

                palmNormal = hand.PalmNormal,
                handDirection = hand.Direction,
                forearmDirection = hand.Arm.Direction,

                palmRotation = hand.Rotation,
                forearmRotation = hand.Arm.Rotation,

                grabStrength = hand.GrabStrength,
                pinchStrength = hand.PinchStrength
            };

            if (hand.IsLeft)
                left = snapshot;
            else
                right = snapshot;
        }

        return new HandDataSnapshot?[] { left, right };
    }
}