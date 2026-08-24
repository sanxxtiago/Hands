using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class CoffeeScoreIndicator : MonoBehaviour
{
    private const float TotalScore = 100f;
    private const int CupCount = 5;
    private const float PointsPerCup = TotalScore / CupCount;

    [Header("Referencias")]
    [SerializeField] private CoffeeCupFill[] cups;
    [SerializeField] private TMP_Text scoreValueText;

    [Header("Configuración")]
    [SerializeField, Tooltip("Formato numérico del score (ej: 0 para enteros, 0.0 para un decimal).")]
    private string scoreFormat = "0";
    [SerializeField, Min(0f), Tooltip("Duración por defecto de la animación de llenado (segundos).")]
    private float defaultAnimationDuration = 0.5f;

    private float displayedScore;
    private Tween fillTween;

    public float DisplayedScore => displayedScore;

    private void OnDisable()
    {
        KillTween();
    }

    public void SetScore(float score)
    {
        KillTween();
        WarnIfCupsMissing();
        ApplyScore(score);
    }

    public void SetScoreAnimated(float score)
    {
        SetScoreAnimated(score, defaultAnimationDuration);
    }

    public void SetScoreAnimated(float score, float duration)
    {
        float target = Mathf.Clamp(score, 0f, TotalScore);
        KillTween();
        WarnIfCupsMissing();

        if (duration <= 0f)
        {
            ApplyScore(target);
            return;
        }

        fillTween = DOTween.To(GetDisplayedScore, ApplyScore, target, duration).SetEase(Ease.OutCubic);
    }

    private float GetDisplayedScore()
    {
        return displayedScore;
    }

    private void ApplyScore(float score)
    {
        displayedScore = Mathf.Clamp(score, 0f, TotalScore);

        if (cups != null)
        {
            for (int i = 0; i < cups.Length; i++)
            {
                if (cups[i] == null)
                {
                    continue;
                }

                float cupFill = Mathf.Clamp01((displayedScore - i * PointsPerCup) / PointsPerCup);
                cups[i].SetFill(cupFill);
            }
        }

        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreValueText == null)
        {
            return;
        }

        try
        {
            scoreValueText.text = displayedScore.ToString(scoreFormat) + "/" + TotalScore.ToString("0");
        }
        catch (FormatException)
        {
            Debug.LogWarning("[CoffeeScore] El formato '" + scoreFormat + "' no es válido; se usa '0'.", this);
            scoreFormat = "0";
            scoreValueText.text = displayedScore.ToString(scoreFormat) + "/" + TotalScore.ToString("0");
        }
    }

    private void WarnIfCupsMissing()
    {
        if (cups == null || cups.Length == 0)
        {
            Debug.LogWarning("[CoffeeScore] El indicador no tiene tazas asignadas.", this);
        }
    }

    private void KillTween()
    {
        if (fillTween == null)
        {
            return;
        }

        fillTween.Kill();
        fillTween = null;
    }

    private void OnValidate()
    {
        if (cups != null && cups.Length != CupCount)
        {
            Debug.LogWarning("[CoffeeScore] El indicador requiere exactamente 5 tazas.", this);
        }
    }

#if UNITY_EDITOR
    private const float CupWidth = 96f;
    private const float CupHeight = 120f;
    private const float CupSpacing = 24f;
    private const float PanelWidth = 640f;
    private const float PanelHeight = 280f;
    private const float HeaderTopOffset = 24f;
    private const float HeaderHeight = 96f;
    private const float PanelPadding = 32f;

    private static readonly Color PanelBackgroundColor = new Color32(0x1E, 0x29, 0x3B, 0xD9);
    private static readonly Color PanelBorderColor = new Color32(0x33, 0x41, 0x55, 0xFF);
    private static readonly Color CupOutlineEmptyColor = new Color32(0x33, 0x41, 0x55, 0xFF);
    private static readonly Color CupFillColor = new Color32(0x25, 0x63, 0xEB, 0xFF);
    private static readonly Color HeaderLabelColor = new Color32(0xC3, 0xC6, 0xD7, 0xFF);
    private static readonly Color HeaderValueColor = new Color32(0xF8, 0xFA, 0xFC, 0xFF);

    private static Sprite PlaceholderSprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    private void Reset()
    {
        BuildPlaceholderHierarchy();
    }

    [ContextMenu("Build Placeholder Hierarchy")]
    private void BuildPlaceholderHierarchy()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[CoffeeScore] La construcción de la jerarquía solo está disponible en modo edición.");
            return;
        }

        Undo.RecordObject(this, "Build Coffee Score Indicator");
        ClearChildren(transform);

        RectTransform root = EnsureRoot();
        CreatePanelBackground(root);
        scoreValueText = CreateHeader(root);

        RectTransform row = CreateRow(root);
        cups = new CoffeeCupFill[CupCount];
        for (int i = 0; i < CupCount; i++)
        {
            cups[i] = CreateCup(row, i);
        }

        ApplyScore(0f);
        EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    private RectTransform EnsureRoot()
    {
        RectTransform root = transform as RectTransform;
        if (root == null)
        {
            root = gameObject.AddComponent<RectTransform>();
        }

        root.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        return root;
    }

    private void CreatePanelBackground(RectTransform root)
    {
        Image background = root.gameObject.GetComponent<Image>();
        if (background == null)
        {
            background = root.gameObject.AddComponent<Image>();
        }

        background.sprite = PlaceholderSprite;
        background.type = Image.Type.Sliced;
        background.color = PanelBackgroundColor;
        background.raycastTarget = false;

        RectTransform border = CreateChild("PanelBorder", root);
        Stretch(border, 0f);
        Image borderImage = border.gameObject.AddComponent<Image>();
        borderImage.sprite = PlaceholderSprite;
        borderImage.type = Image.Type.Sliced;
        borderImage.fillCenter = false;
        borderImage.color = PanelBorderColor;
        borderImage.raycastTarget = false;
    }

    private TMP_Text CreateHeader(RectTransform root)
    {
        RectTransform header = CreateChild("Header", root);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.anchoredPosition = new Vector2(0f, -HeaderTopOffset);
        header.sizeDelta = new Vector2(0f, HeaderHeight);

        TMP_Text label = CreateText("ScoreLabel", header, "PUNTOS TOTALES", 12f, HeaderLabelColor, 2f);
        label.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        label.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        label.rectTransform.pivot = new Vector2(0.5f, 1f);
        label.rectTransform.anchoredPosition = new Vector2(0f, -2f);
        label.rectTransform.sizeDelta = new Vector2(400f, 20f);

        TMP_Text value = CreateText("ScoreValue", header, "0/100", 32f, HeaderValueColor, 0f);
        value.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        value.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        value.rectTransform.pivot = new Vector2(0.5f, 0f);
        value.rectTransform.anchoredPosition = Vector2.zero;
        value.rectTransform.sizeDelta = new Vector2(400f, 56f);

        return value;
    }

    private RectTransform CreateRow(RectTransform root)
    {
        RectTransform row = CreateChild("CupsRow", root);
        row.anchorMin = new Vector2(0f, 0f);
        row.anchorMax = new Vector2(1f, 0f);
        row.pivot = new Vector2(0.5f, 0f);
        row.anchoredPosition = new Vector2(0f, PanelPadding);
        row.sizeDelta = new Vector2(0f, CupHeight);

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = CupSpacing;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        return row;
    }

    private CoffeeCupFill CreateCup(RectTransform row, int index)
    {
        RectTransform slot = CreateChild("CupSlot_" + index, row);
        slot.sizeDelta = new Vector2(CupWidth, CupHeight);
        LayoutElement slotLayout = slot.gameObject.AddComponent<LayoutElement>();
        slotLayout.preferredWidth = CupWidth;
        slotLayout.preferredHeight = CupHeight;

        RectTransform glowRect = CreateChild("CupGlow", slot);
        Stretch(glowRect, 6f);
        Image glowImage = glowRect.gameObject.AddComponent<Image>();
        glowImage.sprite = PlaceholderSprite;
        glowImage.type = Image.Type.Sliced;
        Color glowColor = CupFillColor;
        glowColor.a = 0f;
        glowImage.color = glowColor;
        glowImage.raycastTarget = false;

        RectTransform outlineRect = CreateChild("CupOutline", slot);
        Stretch(outlineRect, 0f);
        Image outlineImage = outlineRect.gameObject.AddComponent<Image>();
        outlineImage.sprite = PlaceholderSprite;
        outlineImage.type = Image.Type.Sliced;
        outlineImage.color = CupOutlineEmptyColor;
        outlineImage.raycastTarget = false;

        RectTransform fillArea = CreateChild("CupFillArea", slot);
        fillArea.anchorMin = Vector2.zero;
        fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = new Vector2(12f, 8f);
        fillArea.offsetMax = new Vector2(-12f, -20f);
        fillArea.gameObject.AddComponent<RectMask2D>();

        RectTransform fillRect = CreateChild("CupFill", fillArea);
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 0f);
        fillRect.pivot = new Vector2(0.5f, 0f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;
        Image fillImage = fillRect.gameObject.AddComponent<Image>();
        fillImage.sprite = PlaceholderSprite;
        fillImage.type = Image.Type.Sliced;
        fillImage.color = CupFillColor;
        fillImage.raycastTarget = false;

        CoffeeCupFill cup = slot.gameObject.AddComponent<CoffeeCupFill>();
        cup.Setup(fillArea, fillRect, outlineImage, glowImage);
        Undo.RegisterCreatedObjectUndo(slot.gameObject, "Create Coffee Cup Slot");
        return cup;
    }

    private static TMP_Text CreateText(string textName, Transform parent, string content, float fontSize, Color color, float characterSpacing)
    {
        GameObject go = new GameObject(textName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.characterSpacing = characterSpacing;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateChild(string childName, Transform parent)
    {
        GameObject go = new GameObject(childName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform target, float expansion)
    {
        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.one;
        target.pivot = new Vector2(0.5f, 0.5f);
        target.offsetMin = new Vector2(-expansion, -expansion);
        target.offsetMax = new Vector2(expansion, expansion);
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
        }
    }
#endif
}
