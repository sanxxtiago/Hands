using UnityEngine;

public class ErgonomicsTracker : MonoBehaviour
{
    public GestureDetector gestureDetector;
    public HAND hand;

    private ErgonomicsCalculator calculator = new ErgonomicsCalculator();

    // acumuladores por zona
    private float handAccum;
    private float wristAccum;
    private float forearmAccum;

    // tiempo total del ejercicio
    private float totalTime;

    // tiempo donde NO hubo actividad
    private float inactiveTime;

    // umbral mínimo para considerar "actividad"
    private float activityThreshold = 0.01f;

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

        var (handA, wristA, forearmA) = calculator.CalculateActivity(e);

        float dt = Time.deltaTime;

        // acumular tiempo total SIEMPRE
        totalTime += dt;

        // acumular uso por zona
        handAccum += handA * dt;
        wristAccum += wristA * dt;
        forearmAccum += forearmA * dt;

        // detectar si hubo actividad real
        float totalActivity = handA + wristA + forearmA;

        if (totalActivity < activityThreshold)
        {
            inactiveTime += dt;
        }
    }

    // =========================
    // 📊 MÉTRICAS
    // =========================

    // 🔹 1. Uso absoluto (respecto al tiempo total)
    public (float hand, float wrist, float forearm) GetAbsoluteUsage()
    {
        if (totalTime <= 0) return (0, 0, 0);

        return (
            handAccum / totalTime,
            wristAccum / totalTime,
            forearmAccum / totalTime
        );
    }

    // 🔹 2. Distribución relativa (solo cuando hubo actividad)
    public (float hand, float wrist, float forearm) GetRelativeDistribution()
    {
        float total = handAccum + wristAccum + forearmAccum;

        if (total <= 0) return (0, 0, 0);

        return (
            handAccum / total,
            wristAccum / total,
            forearmAccum / total
        );
    }

    // 🔹 3. Inactividad
    public float GetInactivePercentage()
    {
        if (totalTime <= 0) return 0;
        return inactiveTime / totalTime;
    }

    // 🔹 opcional: tiempo activo real
    public float GetActivePercentage()
    {
        return 1f - GetInactivePercentage();
    }

    // 🔹 raw data (por si luego quieres cosas más pro)
    public (float hand, float wrist, float forearm) GetRawTotals()
    {
        return (handAccum, wristAccum, forearmAccum);
    }

    public float GetTotalTime() => totalTime;

    public void ResetData()
    {
        handAccum = 0;
        wristAccum = 0;
        forearmAccum = 0;

        totalTime = 0;
        inactiveTime = 0;
    }
}