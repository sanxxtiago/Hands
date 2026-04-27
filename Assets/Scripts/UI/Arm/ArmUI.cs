using UnityEngine;
using UnityEngine.UI;

public abstract class ArmUI : MonoBehaviour
{
    public HandType hand;

    public Gradient usageGradient;
    public Image handImage;
    public Image wristImage;
    public Image foreArmImage;

    public float colorLerpSpeed = 10f;

    protected void ApplyColor(Image img, float value)
    {
        // value debe estar entre 0 y 1
        Color target = usageGradient.Evaluate(value);
        img.color = Color.Lerp(img.color, target, Time.deltaTime * colorLerpSpeed);
    }
    protected void ApplyAll(float hand, float wrist, float forearm)
    {
        ApplyColor(handImage, hand);
        ApplyColor(wristImage, wrist);
        ApplyColor(foreArmImage, forearm);
    }
}