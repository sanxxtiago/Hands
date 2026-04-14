using UnityEngine;
using UnityEngine.UI;

public abstract class ArmUI : MonoBehaviour
{
    public HandType hand;

    public Color defaultColor;
    public Color paintColor;

    public Image handImage;
    public Image wristImage;
    public Image foreArmImage;

    public float colorLerpSpeed = 10f;

    protected void ApplyColor(Image img, float value)
    {
        Color target = Color.Lerp(defaultColor, paintColor, value);
        img.color = Color.Lerp(img.color, target, Time.deltaTime * colorLerpSpeed);
    }

    protected void ApplyAll(float hand, float wrist, float forearm)
    {
        ApplyColor(handImage, hand);
        ApplyColor(wristImage, wrist);
        ApplyColor(foreArmImage, forearm);
    }
}