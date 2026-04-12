using UnityEngine;

public class ErgonomicsTracker : MonoBehaviour
{
    public GestureDetector gestureDetector;
    public HandType hand;

    private ErgonomicsCalculator calculator = new ErgonomicsCalculator();

    // =========================
    // SENSIBILIDAD (solo intensidad)
    // =========================
    [Header("Sensitivity")]
    public float handSensitivity = 2f;
    public float wristSensitivity = 3f;
    public float forearmSensitivity = 1.5f;

    // =========================
    // TIEMPO TOTAL
    // =========================
    private float totalTime;

    // inactividad global
    private float inactiveTime;

    // =========================
    // DISTRIBUCIÓN (TIEMPO - RAW)
    // =========================
    private float handActiveTime;
    private float wristActiveTime;
    private float forearmActiveTime;

    // =========================
    // RAW INPUT (SIN SENSIBILIDAD)
    // =========================
    private float rawHand;
    private float rawWrist;
    private float rawForearm;

    // =========================
    // INTENSIDAD (CON SENSIBILIDAD)
    // =========================
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
            // =========================
            // INTENSIDAD (CON SENSIBILIDAD)
            // =========================
            float handI = Mathf.Clamp01(rawHand * handSensitivity);
            float wristI = Mathf.Clamp01(rawWrist * wristSensitivity);
            float forearmI = Mathf.Clamp01(rawForearm * forearmSensitivity);

            // =========================
            // DISTRIBUCIÓN (TIEMPO - RAW)
            // =========================
            bool handActive = rawHand > 0.01f;
            bool wristActive = rawWrist > 0.01f;
            bool forearmActive = rawForearm > 0.01f;

            if (handActive) handActiveTime += dt;
            if (wristActive) wristActiveTime += dt;
            if (forearmActive) forearmActiveTime += dt;

            // =========================
            // INTENSIDAD ACUMULADA
            // =========================
            handIntensityAccum += handI * dt;
            wristIntensityAccum += wristI * dt;
            forearmIntensityAccum += forearmI * dt;

            // =========================
            // INACTIVIDAD GLOBAL (RAW)
            // =========================
            float totalActivity = (rawHand + rawWrist + rawForearm) / 3f;

            if (totalActivity <= 0.01f)
                inactiveTime += dt;
        }

        // reset frame
        rawHand = rawWrist = rawForearm = 0f;
        hasDataThisFrame = false;
    }

    void Track(GestureInputEventArgs e)
    {
        if (e.hand != hand) return;

        if (Time.frameCount == lastProcessedFrame) return;
        lastProcessedFrame = Time.frameCount;

        var (handA, wristA, forearmA) = calculator.CalculateActivity(e);

        // SOLO RAW
        rawHand = handA;
        rawWrist = wristA;
        rawForearm = forearmA;

        hasDataThisFrame = true;
    }

    // =========================
    // MÉTRICAS
    // =========================

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

        return (totalTime - inactiveTime) / totalTime;
    }

    public (float hand, float wrist, float forearm) GetAverageIntensity()
    {
        return (
            handActiveTime > 0 ? handIntensityAccum / handActiveTime : 0,
            wristActiveTime > 0 ? wristIntensityAccum / wristActiveTime : 0,
            forearmActiveTime > 0 ? forearmIntensityAccum / forearmActiveTime : 0
        );
    }

    public float GetTotalTime() => totalTime;

    public void ResetData()
    {
        totalTime = 0;
        inactiveTime = 0;

        handActiveTime = 0;
        wristActiveTime = 0;
        forearmActiveTime = 0;

        handIntensityAccum = 0;
        wristIntensityAccum = 0;
        forearmIntensityAccum = 0;

        rawHand = 0;
        rawWrist = 0;
        rawForearm = 0;
    }
}