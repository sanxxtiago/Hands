public class GrabGestureDetector : IGestureDetector
{
    public float enterThreshold = 0.75f;
    public float exitThreshold = 0.60f;

    private bool _isActive = false;

    public GestureStateData Evaluate(HandDataSnapshot snap)
    {
        float raw = snap.grabStrength;

        bool previousState = _isActive;

        //ENTRADA
        if (!_isActive && raw > enterThreshold)
        {
            _isActive = true;
            //Debug.Log("Grab START");
        }

        //SALIDA
        else if (_isActive && raw < exitThreshold)
        {
            _isActive = false;
            //Debug.Log("Grab END");
        }

        //ESTADO CONTINUO (HOLD)
        if (_isActive && previousState)
        {
            // esto es HOLD explícito
            //Debug.Log("Grab HOLD");
        }

        return new GestureStateData
        {
            type = GestureType.GRAB,
            //handType = snap.handType,
            strength = raw,
            isActive = _isActive && previousState
        };
    }
}