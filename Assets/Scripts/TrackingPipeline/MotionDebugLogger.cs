using UnityEngine;

public class MotionDebugLogger : MonoBehaviour
{
    void OnEnable()
    {
        MotionEventBus.OnFrame += LogFrame;
    }

    void OnDisable()
    {
        MotionEventBus.OnFrame -= LogFrame;
    }

    void LogFrame(FrameMotionData frame)
    {

        foreach (var m in frame.motions)
        {
            if (m.isActive)
            {
                Debug.Log($"FRAME {frame.frameId} | {frame.handType}");

                Debug.Log($"  MOTION {m.zone} | value: {m.value:F2} | active: {m.isActive}");
            }
        }
    }

    // foreach (var g in frame.gestures)
    // {
    //     Debug.Log($"  GESTURE {g.type} | strength: {g.strength:F2}");
    // }

}