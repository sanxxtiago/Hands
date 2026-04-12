using UnityEngine;

public class ArmColorCalculator : ArmColor
{
    public GestureDetector gestureDetector;

    private Quaternion lastRotation;
    private bool hasRotation;

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

        var (h, w, f) = ErgonomicsCalculatorUI.Calculate(
            e,
            ref lastRotation,
            ref hasRotation
        );

        ApplyAll(h, w, f);
    }
}