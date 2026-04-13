using UnityEngine;

public class PinchGestureDetector : IGestureDetector
{
    public float enterThreshold = 0.65f;
    public float exitThreshold = 0.55f;

    private bool _isActive = false;

    public GestureState Evaluate(HandDataSnapshot snap)
    {
        float grab = snap.grabStrength;
        float pinchRaw = snap.pinchStrength;

        //señal efectiva
        float effectivePinch = pinchRaw;

        //inhibición contextual (correcta)
        if (grab > 0.7f)
        {
            effectivePinch *= 0.3f;
        }

        bool previousState = _isActive;

        //ENTRADA
        if (!_isActive && effectivePinch > enterThreshold)
        {
            _isActive = true;
            //Debug.Log($"{snap.handType} PINCH START");
        }

        //SALIDA
        else if (_isActive && effectivePinch < exitThreshold)
        {
            _isActive = false;
            //Debug.Log($"{snap.handType} PINCH END");
        }

        return new GestureState
        {
            type = GestureType.Pinch,
            handType = snap.handType,
            strength = pinchRaw,
            isActive = _isActive,
            frameId = snap.frameId
        };
    }
}