using UnityEngine;
public class ErgonomicsTracker : MonoBehaviour
{
    public GestureDetector gestureDetector;
    public HAND hand;

    private ErgonomicsCalculator calculator = new ErgonomicsCalculator();

    private float handAccum;
    private float wristAccum;
    private float forearmAccum;

    private void OnEnable()
    {
        gestureDetector.OnHandUpdate += Track;
    }

    private void OnDisable()
    {
        gestureDetector.OnHandUpdate -= Track;
    }

    void Track(GestureInputEventArgs e)
    {
        if (e.hand != hand) return;

        var (handA, wristA, forearmA) = calculator.Calculate(e);

        // acumulación por tiempo
        handAccum += handA * Time.deltaTime;
        wristAccum += wristA * Time.deltaTime;
        forearmAccum += forearmA * Time.deltaTime;
    }

    public (float hand, float wrist, float forearm) GetPercentages()
    {
        float total = handAccum + wristAccum + forearmAccum;

        if (total == 0) return (0, 0, 0);

        return (
            handAccum / total,
            wristAccum / total,
            forearmAccum / total
        );
    }

    public (float hand, float wrist, float forearm) GetRawTotals()
    {
        return (handAccum, wristAccum, forearmAccum);
    }

    public void ResetData()
    {
        handAccum = 0;
        wristAccum = 0;
        forearmAccum = 0;
    }
}