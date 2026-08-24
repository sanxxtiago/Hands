using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CoffeeCupFill : MonoBehaviour
{
    [SerializeField] private RectTransform fillArea;
    [SerializeField] private RectTransform fill;
    [SerializeField] private Image outline;
    [SerializeField] private Image glow;
    [SerializeField] private Color emptyOutlineColor = new Color32(0x33, 0x41, 0x55, 0xFF);
    [SerializeField] private Color fullOutlineColor = new Color32(0x25, 0x63, 0xEB, 0xFF);
    [SerializeField, Range(0f, 1f), Tooltip("Opacidad máxima del glow cuando la taza está llena.")]
    private float glowMaxAlpha = 0.35f;

    private float currentFill;
    private bool warnedMissingRefs;

    public float CurrentFill => currentFill;

    public void Setup(RectTransform fillAreaRect, RectTransform fillRect, Image outlineImage, Image glowImage)
    {
        fillArea = fillAreaRect;
        fill = fillRect;
        outline = outlineImage;
        glow = glowImage;
        warnedMissingRefs = false;
    }

    public void SetFill(float amount)
    {
        currentFill = Mathf.Clamp01(amount);

        if (fillArea == null || fill == null)
        {
            if (!warnedMissingRefs)
            {
                warnedMissingRefs = true;
                Debug.LogWarning("[CoffeeScore] Una taza no tiene fillArea/fill asignados; el llenado no se mostrará.", this);
            }
            return;
        }

        float height = fillArea.rect.height * currentFill;
        fill.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        if (outline != null)
        {
            outline.color = Color.Lerp(emptyOutlineColor, fullOutlineColor, currentFill);
        }

        if (glow != null)
        {
            Color glowColor = glow.color;
            glowColor.a = glowMaxAlpha * currentFill;
            glow.color = glowColor;
        }
    }
}
