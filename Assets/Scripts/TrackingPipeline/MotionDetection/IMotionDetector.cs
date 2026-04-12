using UnityEngine.XR;

public interface IMotionDetector
{
    public MotionData Evaluate(HandDataSnapshot snap);
}
