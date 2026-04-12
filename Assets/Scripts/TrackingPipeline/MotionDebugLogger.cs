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
        Debug.Log($"FRAME {frame.frameId} | {frame.handType}");

        foreach (var m in frame.motions)
        {
            Debug.Log($"  MOTION {m.zone} | value: {m.value:F2} | active: {m.isActive}");
        }

        foreach (var g in frame.gestures)
        {
            Debug.Log($"  GESTURE {g.type} | strength: {g.strength:F2}");
        }
    }
}