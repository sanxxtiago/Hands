public class ArmColorCalculator : ArmColor
{
    public GestureDetector gestureDetector;
    private ErgonomicsCalculator calculator = new ErgonomicsCalculator();

    private void OnEnable()
    {
        gestureDetector.OnHandUpdate += HandleUpdate;
    }

    private void OnDisable()
    {
        gestureDetector.OnHandUpdate -= HandleUpdate;
    }

    void HandleUpdate(GestureInputEventArgs e)
    {
        if (e.hand != hand) return;

        var (h, w, f) = calculator.CalculateActivity(e);

        ApplyAll(h, w, f);
    }
}