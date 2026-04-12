using UnityEngine;

public class ErgonomicsTracker : MonoBehaviour
{
    public GestureDetector gestureDetector;
    public HAND hand;

    private ErgonomicsCalculator calculator = new ErgonomicsCalculator();

    // acumuladores por zona (intensidad)
    private float handAccum;
    private float wristAccum;
    private float forearmAccum;

    // tiempo total del ejercicio
    private float totalTime;

    // tiempo donde NO hubo actividad
    private float inactiveTime;

    // 🔥 thresholds independientes
    [Header("Activity Thresholds")]
    public float handThreshold = 0.05f;
    public float wristThreshold = 0.08f;
    public float forearmThreshold = 0.1f;

    // tiempo activo por zona
    private float handActiveTime;
    private float wristActiveTime;
    private float forearmActiveTime;

    // valores por frame
    private float handFrame;
    private float wristFrame;
    private float forearmFrame;
    private float handIntensityAccum;
    private float wristIntensityAccum;
    private float forearmIntensityAccum;

    private bool hasDataThisFrame;
    private int lastProcessedFrame = -1;

    private void OnEnable()
    {
        gestureDetector.OnHandUpdate += Track;
    }

    private void OnDisable()
    {
        gestureDetector.OnHandUpdate -= Track;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        totalTime += dt;

        if (hasDataThisFrame)
        {
            float handI = Mathf.Clamp01(handFrame);
            float wristI = Mathf.Clamp01(wristFrame);
            float forearmI = Mathf.Clamp01(forearmFrame);

            // acumulado total (lo que ya tienes)
            handAccum += handI * dt;
            wristAccum += wristI * dt;
            forearmAccum += forearmI * dt;

            bool handActive = handFrame > handThreshold;
            bool wristActive = wristFrame > wristThreshold;
            bool forearmActive = forearmFrame > forearmThreshold;

            //intensidad separada
            if (handActive) handIntensityAccum += handI * dt;
            if (wristActive) wristIntensityAccum += wristI * dt;
            if (forearmActive) forearmIntensityAccum += forearmI * dt;

            //actividad por zona (con thresholds independientes)


            if (handActive) handActiveTime += dt;
            if (wristActive) wristActiveTime += dt;
            if (forearmActive) forearmActiveTime += dt;

            //actividad global CORRECTA
            bool isAnyActive = handActive || wristActive || forearmActive;

            if (!isAnyActive)
                inactiveTime += dt;
        }
        else
        {
            //sin datos → asumimos inactividad
            //inactiveTime += dt;
        }

        // reset frame
        handFrame = wristFrame = forearmFrame = 0f;
        hasDataThisFrame = false;
    }

    void Track(GestureInputEventArgs e)
    {
        if (e.hand != hand) return;

        //solo 1 vez por frame
        if (Time.frameCount == lastProcessedFrame) return;
        lastProcessedFrame = Time.frameCount;

        var (handA, wristA, forearmA) = calculator.CalculateActivity(e);

        //solo cachear
        handFrame = handA;
        wristFrame = wristA;
        forearmFrame = forearmA;

        hasDataThisFrame = true;
    }

    //MÉTRICAS
    public (float hand, float wrist, float forearm) GetAbsoluteUsage()
    {
        if (totalTime <= 0) return (0, 0, 0);

        return (
            handActiveTime / totalTime,
            wristActiveTime / totalTime,
            forearmActiveTime / totalTime
        );
    }

    public (float hand, float wrist, float forearm) GetRelativeDistribution()
    {
        float totalActive = handActiveTime + wristActiveTime + forearmActiveTime;

        if (totalActive <= 0) return (0, 0, 0);

        return (
            handActiveTime / totalActive,
            wristActiveTime / totalActive,
            forearmActiveTime / totalActive
        );
    }

    public float GetInactivePercentage()
    {
        if (totalTime <= 0) return 0;
        return inactiveTime / totalTime;
    }

    public float GetActivePercentage()
    {
        return 1f - GetInactivePercentage();
    }

    public float GetTotalActivePercentage()
    {
        if (totalTime <= 0) return 0;

        float activeTime =
            totalTime - inactiveTime;

        return activeTime / totalTime;
    }

    public (float hand, float wrist, float forearm) GetRawTotals()
    {
        return (handAccum, wristAccum, forearmAccum);
    }

    public (float hand, float wrist, float forearm) GetAverageIntensity()
    {
        return (
            Mathf.Clamp01(handActiveTime > 0 ? handIntensityAccum / handActiveTime : 0),
            Mathf.Clamp01(wristActiveTime > 0 ? wristIntensityAccum / wristActiveTime : 0),
            Mathf.Clamp01(forearmActiveTime > 0 ? forearmIntensityAccum / forearmActiveTime : 0)
        );
    }

    public float GetTotalTime() => totalTime;

    public void ResetData()
    {
        handAccum = 0;
        wristAccum = 0;
        forearmAccum = 0;

        handActiveTime = 0;
        wristActiveTime = 0;
        forearmActiveTime = 0;

        totalTime = 0;
        inactiveTime = 0;
    }
}