using UnityEngine;
using UnityEngine.UI;

public class ArmColor : MonoBehaviour
{
    public GestureDetector gestureDetector;
    public HAND hand;

    [Header("Colors")]
    public Color defaultColor;
    public Color paintColor;

    [Header("UI")]
    public Image handImage;
    public Image wristImage;
    public Image foreArmImage;

    [Header("Smoothing")]
    public float colorLerpSpeed = 10f;

    private ErgonomicsCalculator calculator = new ErgonomicsCalculator();

    private void OnEnable()
    {
        gestureDetector.OnHandUpdate += UpdateVisual;
    }

    private void OnDisable()
    {
        gestureDetector.OnHandUpdate -= UpdateVisual;
    }


    private void UpdateVisual(GestureInputEventArgs e)
    {
        if (e.hand != this.hand) return;

        var (handA, wristA, forearmA) = calculator.Calculate(e);

        ApplyColor(handImage, handA);
        ApplyColor(wristImage, wristA);
        ApplyColor(foreArmImage, forearmA);
    }

    void ApplyColor(Image img, float activity)
    {
        Color target = Color.Lerp(defaultColor, paintColor, activity);
        img.color = Color.Lerp(img.color, target, Time.deltaTime * colorLerpSpeed);
    }
}