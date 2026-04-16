using System.Collections;
using System.Collections.Generic;
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
}
