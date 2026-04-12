using UnityEngine.XR;

public interface IMotionDetector
{
    MotionData Evaluate(HandDataSnapshot current, HandDataSnapshot previous);
}