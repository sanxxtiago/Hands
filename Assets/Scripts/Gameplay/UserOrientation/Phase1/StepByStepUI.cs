using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StepByStepUI : MonoBehaviour
{
    private const string GripInstruction = "Cierra ambos pu\u00f1os para continuar";
    private const string CompletedInstruction = "\u00a1Fase completada!";

    [SerializeField] private OrientationPhase1Manager phase1;
    [Header("Secuencia de volúmenes")]
    [SerializeField] private OrientationPhase1Volume firstVolume;
    [SerializeField] private OrientationPhase1Volume secondVolume;
    [SerializeField] private OrientationPhase1Volume thirdVolume;
    [SerializeField] private OrientationPhase1Volume fourthVolume;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text progressionText;
    [SerializeField] private TMP_Text instructionText;

    [Header("Progreso")]
    [SerializeField, Min(0f)] private float progressSmoothTime = 0.12f;
    [SerializeField, Min(0f)] private float activityPulseScale = 0.03f;
    [SerializeField, Min(0f)] private float activityPulseDecay = 4f;
    [SerializeField, Range(0f, 1f)] private float activityPulseThreshold = 0.1f;

    [Header("Indicadores de manos")]
    [SerializeField] private Color missingHandColor = new Color(0.45f, 0.45f, 0.5f, 1f);
    [SerializeField] private Color detectedHandColor = new Color(1f, 0.9f, 0.3f, 1f);
    [SerializeField] private Color closedHandColor = new Color(0.3f, 1f, 0.45f, 1f);

    [Header("Celebraci\u00f3n")]
    [SerializeField, Min(0f)] private float instructionAnimationDuration = 0.3f;
    [SerializeField, Min(0f)] private float instructionSlideDistance = 12f;
    [SerializeField] private ParticleSystem phaseCompleteEffect;
    [SerializeField] private Transform completionOrigin;

    private RectTransform fillRect;
    private Image fillImage;
    private Color initialFillColor;
    private Vector3 initialFillScale;
    private RectTransform instructionRect;
    private CanvasGroup instructionGroup;
    private Vector2 initialInstructionPosition;
    private string initialInstructionText;
    private bool initialInstructionRichText;
    private OrientationPhase1Volume[] volumeSequence;
    private string[] volumeInstructions;
    private TMP_Text phase1Text;
    private TMP_Text phase2Text;
    private TMP_Text phase3Text;
    private Color initialPhase1Color;
    private Color initialPhase2Color;
    private Color initialPhase3Color;
    private Vector3 initialPhase1Scale;
    private Vector3 initialPhase2Scale;
    private Vector3 initialPhase3Scale;
    private FontStyles initialPhase1FontStyle;
    private FontStyles initialPhase2FontStyle;
    private FontStyles initialPhase3FontStyle;
    private ParticleEffectPlayer particleEffectPlayer;
    private GameObject gripFeedbackRoot;
    private TMP_Text leftGripText;
    private TMP_Text rightGripText;
    private Tween instructionTween;
    private Tween phaseCompletionTween;
    private float targetProgress;
    private float displayedProgress;
    private float progressVelocity;
    private float leftActivity;
    private float rightActivity;
    private float activityPulse;
    private int lastDisplayedPercent = -1;
    private bool leftHandPresent;
    private bool rightHandPresent;
    private bool leftHandClosed;
    private bool rightHandClosed;
    private bool awaitingGrip;
    private bool volumeSequenceStarted;
    private bool phaseCompleted;
    private int currentVolumeIndex = -1;

    private void Awake()
    {
        if (phase1 == null)
        {
            Debug.LogError(
                "[OrientationPhase1UI] Falta asignar el manager de la Fase 1.",
                this);
            enabled = false;
            return;
        }

        if (progressBar != null)
        {
            fillRect = progressBar.fillRect;
            if (fillRect != null)
            {
                fillImage = fillRect.GetComponent<Image>();
                initialFillScale = fillRect.localScale;
                initialFillColor = fillImage != null
                    ? fillImage.color
                    : Color.white;
            }
        }

        if (instructionText != null)
        {
            instructionRect = instructionText.rectTransform;
            initialInstructionPosition = instructionRect.anchoredPosition;
            initialInstructionText = instructionText.text;
            initialInstructionRichText = instructionText.richText;
            instructionGroup = instructionText.GetComponent<CanvasGroup>();
            if (instructionGroup == null)
                instructionGroup = instructionText.gameObject.AddComponent<CanvasGroup>();

            instructionText.richText = true;
        }

        volumeSequence = new[]
        {
            firstVolume,
            secondVolume,
            thirdVolume,
            fourthVolume
        };
        volumeInstructions = CreateVolumeInstructions();

        ResolvePhaseLabel();
        CreateGripFeedback();

        if (phaseCompleteEffect != null)
        {
            particleEffectPlayer = GetComponent<ParticleEffectPlayer>();
            if (particleEffectPlayer == null)
                particleEffectPlayer = gameObject.AddComponent<ParticleEffectPlayer>();
        }
    }

    private void OnEnable()
    {
        if (phase1 == null)
            return;

        if (instructionText != null)
            instructionText.richText = true;

        phase1.OnProgressChanged += UpdateProgressBar;
        phase1.OnActivityFeedbackChanged += HandleActivityFeedbackChanged;
        phase1.OnGripStateChanged += HandleGripStateChanged;
        phase1.OnHandPresenceChanged += HandleHandPresenceChanged;
        phase1.OnExplorationCompleted += HandleExplorationCompleted;
        phase1.OnPhaseCompleted += HandlePhaseCompleted;

        SubscribeToVolumes();
    }

    private void Update()
    {
        UpdateVolumeInstruction();
        UpdateProgressVisuals();
        UpdateActivityVisuals();
    }

    private void OnDisable()
    {
        if (phase1 != null)
        {
            phase1.OnProgressChanged -= UpdateProgressBar;
            phase1.OnActivityFeedbackChanged -= HandleActivityFeedbackChanged;
            phase1.OnGripStateChanged -= HandleGripStateChanged;
            phase1.OnHandPresenceChanged -= HandleHandPresenceChanged;
            phase1.OnExplorationCompleted -= HandleExplorationCompleted;
            phase1.OnPhaseCompleted -= HandlePhaseCompleted;
        }

        UnsubscribeFromVolumes();

        instructionTween?.Kill();
        phaseCompletionTween?.Kill();
        instructionTween = null;
        phaseCompletionTween = null;

        if (particleEffectPlayer != null)
            particleEffectPlayer.ClearEffects();

        if (fillRect != null)
            fillRect.localScale = initialFillScale;

        if (fillImage != null)
            fillImage.color = initialFillColor;

        if (phase1Text != null)
        {
            phase1Text.color = initialPhase1Color;
            phase1Text.fontStyle = initialPhase1FontStyle;
            phase1Text.transform.localScale = initialPhase1Scale;
        }

        RestorePhaseLabel(phase2Text, initialPhase2Color, initialPhase2Scale, initialPhase2FontStyle);
        RestorePhaseLabel(phase3Text, initialPhase3Color, initialPhase3Scale, initialPhase3FontStyle);

        if (instructionGroup != null)
            instructionGroup.alpha = 1f;

        if (instructionText != null)
        {
            instructionText.text = initialInstructionText;
            instructionText.richText = initialInstructionRichText;
        }

        if (instructionRect != null)
            instructionRect.anchoredPosition = initialInstructionPosition;

        if (gripFeedbackRoot != null)
            gripFeedbackRoot.SetActive(false);

        awaitingGrip = false;
        volumeSequenceStarted = false;
        phaseCompleted = false;
        currentVolumeIndex = -1;
        targetProgress = 0f;
        displayedProgress = 0f;
        progressVelocity = 0f;
        lastDisplayedPercent = -1;
        leftActivity = 0f;
        rightActivity = 0f;
        leftHandPresent = false;
        rightHandPresent = false;
        leftHandClosed = false;
        rightHandClosed = false;
        activityPulse = 0f;
    }

    private void UpdateProgressVisuals()
    {
        if (progressBar == null)
            return;

        if (progressSmoothTime <= 0f)
        {
            displayedProgress = targetProgress;
            progressVelocity = 0f;
        }
        else
        {
            displayedProgress = Mathf.SmoothDamp(
                displayedProgress,
                targetProgress,
                ref progressVelocity,
                progressSmoothTime);
        }

        progressBar.SetValueWithoutNotify(displayedProgress);

        if (progressionText == null)
            return;

        int displayedPercent = Mathf.Clamp(
            Mathf.RoundToInt(displayedProgress * 100f),
            0,
            100);

        if (displayedPercent == lastDisplayedPercent)
            return;

        lastDisplayedPercent = displayedPercent;
        progressionText.text = displayedPercent + "%";
    }

    private void UpdateActivityVisuals()
    {
        if (fillRect == null || phaseCompleted)
            return;

        float activity = Mathf.Max(leftActivity, rightActivity);
        float targetPulse = !awaitingGrip && activity >= activityPulseThreshold
            ? activity * activityPulseScale
            : 0f;

        activityPulse = Mathf.MoveTowards(
            activityPulse,
            targetPulse,
            Time.deltaTime * activityPulseDecay);

        fillRect.localScale = initialFillScale * (1f + activityPulse);

        if (fillImage != null)
        {
            Color activityColor = Color.Lerp(
                initialFillColor,
                detectedHandColor,
                activityPulse * 4f);
            fillImage.color = activityColor;
        }
    }

    public void UpdateProgressBar(float activeTime, float requiredActiveTime)
    {
        float requiredTime = Mathf.Max(requiredActiveTime, Mathf.Epsilon);
        targetProgress = Mathf.Clamp01(activeTime / requiredTime);
    }

    public void UpdateMessage()
    {
        HandleExplorationCompleted();
    }

    private void HandleActivityFeedbackChanged(float leftScore, float rightScore)
    {
        leftActivity = Mathf.Clamp01(leftScore);
        rightActivity = Mathf.Clamp01(rightScore);
    }

    private void HandleHandPresenceChanged(bool leftPresent, bool rightPresent)
    {
        leftHandPresent = leftPresent;
        rightHandPresent = rightPresent;

        if (awaitingGrip)
            UpdateGripVisuals();
    }

    private void HandleGripStateChanged(bool leftClosed, bool rightClosed)
    {
        leftHandClosed = leftClosed;
        rightHandClosed = rightClosed;

        if (awaitingGrip)
            UpdateGripVisuals();
    }

    private void HandleExplorationCompleted()
    {
        awaitingGrip = true;

        if (gripFeedbackRoot != null)
            gripFeedbackRoot.SetActive(true);

        UpdateGripVisuals();
        AnimateInstruction(GripInstruction);
    }

    private void UpdateVolumeInstruction()
    {
        if (!awaitingGrip || volumeSequenceStarted || phaseCompleted)
            return;

        if (volumeSequence == null || volumeSequence.Length == 0 || volumeSequence[0] == null)
            return;

        if (!volumeSequence[0].isActiveAndEnabled)
            return;

        volumeSequenceStarted = true;
        currentVolumeIndex = 0;

        if (gripFeedbackRoot != null)
            gripFeedbackRoot.SetActive(false);

        AnimateInstruction(volumeInstructions[currentVolumeIndex]);
    }

    private void HandleVolumeTouched(OrientationPhase1Volume volume)
    {
        if (!volumeSequenceStarted || phaseCompleted || volume == null)
            return;

        if (currentVolumeIndex < 0 ||
            currentVolumeIndex >= volumeSequence.Length ||
            volume != volumeSequence[currentVolumeIndex])
        {
            return;
        }

        if (currentVolumeIndex >= volumeSequence.Length - 1)
        {
            volumeSequenceStarted = false;
            currentVolumeIndex = -1;
            return;
        }

        currentVolumeIndex++;
        AnimateInstruction(volumeInstructions[currentVolumeIndex]);
    }

    private void HandlePhaseCompleted()
    {
        phaseCompleted = true;
        awaitingGrip = false;
        volumeSequenceStarted = false;
        currentVolumeIndex = -1;
        targetProgress = 1f;
        displayedProgress = 1f;
        progressVelocity = 0f;
        leftActivity = 0f;
        rightActivity = 0f;
        activityPulse = 0f;

        if (fillRect != null)
            fillRect.localScale = initialFillScale;

        if (progressBar != null)
            progressBar.SetValueWithoutNotify(1f);

        if (progressionText != null)
        {
            lastDisplayedPercent = 100;
            progressionText.text = "100%";
        }

        if (fillImage != null)
            fillImage.color = closedHandColor;

        if (gripFeedbackRoot != null)
            gripFeedbackRoot.SetActive(true);

        leftHandPresent = true;
        rightHandPresent = true;
        leftHandClosed = true;
        rightHandClosed = true;
        UpdateGripVisuals();

        AnimateInstruction(CompletedInstruction);

        if (phase1Text != null)
        {
            phase1Text.color = closedHandColor;
            phase1Text.fontStyle = FontStyles.Bold;
            phaseCompletionTween?.Kill();
            Sequence completionSequence = DOTween.Sequence();
            completionSequence.Append(
                phase1Text.transform
                    .DOPunchScale(Vector3.one * 0.12f, 0.35f, 1, 0.5f)
                    .SetEase(Ease.OutCubic));
            completionSequence.AppendInterval(0.45f);
            completionSequence.Append(phase1Text.DOFade(0.65f, 0.2f));
            phaseCompletionTween = completionSequence;
        }

        ActivateNextPhase(phase2Text);

        PlayCompletionEffect();
    }

    private void AnimateInstruction(string message)
    {
        if (instructionText == null || instructionGroup == null || instructionRect == null)
            return;

        instructionTween?.Kill();

        if (instructionAnimationDuration <= 0f)
        {
            instructionText.text = message;
            instructionGroup.alpha = 1f;
            instructionRect.anchoredPosition = initialInstructionPosition;
            return;
        }

        float halfDuration = instructionAnimationDuration * 0.5f;
        Sequence sequence = DOTween.Sequence();
        sequence.Append(instructionGroup.DOFade(0f, halfDuration));
        sequence.AppendCallback(() =>
        {
            instructionText.text = message;
            instructionRect.anchoredPosition = initialInstructionPosition + Vector2.down * instructionSlideDistance;
        });
        sequence.Append(instructionGroup.DOFade(1f, halfDuration));
        sequence.Join(
            instructionRect
                .DOAnchorPos(initialInstructionPosition, halfDuration)
                .SetEase(Ease.OutCubic));
        instructionTween = sequence;
    }

    private void SubscribeToVolumes()
    {
        if (volumeSequence == null)
            return;

        for (int i = 0; i < volumeSequence.Length; i++)
        {
            if (volumeSequence[i] != null)
                volumeSequence[i].OnTouched += HandleVolumeTouched;
        }
    }

    private void UnsubscribeFromVolumes()
    {
        if (volumeSequence == null)
            return;

        for (int i = 0; i < volumeSequence.Length; i++)
        {
            if (volumeSequence[i] != null)
                volumeSequence[i].OnTouched -= HandleVolumeTouched;
        }
    }

    private static string[] CreateVolumeInstructions()
    {
        return new[]
        {
            HighlightHandWords("Mueve tu mano derecha hacia la zona izquierda"),
            HighlightHandWords("Mueve tu mano izquierda hacia la zona derecha"),
            "Mueve una de tus manos hacia la parte trasera",
            "Mueve una de tus manos hacia la base de la mesa"
        };
    }

    private static string HighlightHandWords(string message)
    {
        message = HighlightWord(message, "derecha", HandsColor.Right);
        message = HighlightWord(message, "Derecha", HandsColor.Right);
        message = HighlightWord(message, "derecho", HandsColor.Right);
        message = HighlightWord(message, "Derecho", HandsColor.Right);
        message = HighlightWord(message, "izquierda", HandsColor.Left);
        message = HighlightWord(message, "Izquierda", HandsColor.Left);
        message = HighlightWord(message, "izquierdo", HandsColor.Left);
        message = HighlightWord(message, "Izquierdo", HandsColor.Left);
        return message;
    }

    private static string HighlightWord(string message, string word, Color color)
    {
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        string highlightedWord = "<color=#" + colorHex + ">" + word + "</color>";
        return message.Replace(word, highlightedWord);
    }

    private void PlayCompletionEffect()
    {
        if (phaseCompleteEffect == null)
            return;

        if (particleEffectPlayer == null)
        {
            particleEffectPlayer = GetComponent<ParticleEffectPlayer>();
            if (particleEffectPlayer == null)
                particleEffectPlayer = gameObject.AddComponent<ParticleEffectPlayer>();
        }

        Vector3 position = completionOrigin != null
            ? completionOrigin.position
            : transform.position;
        particleEffectPlayer.Play(phaseCompleteEffect, position);
    }

    private void ResolvePhaseLabel()
    {
        if (progressionText == null)
            return;

        Transform progressionParent = progressionText.transform.parent;
        if (progressionParent == null)
            return;

        Transform phase1Transform = progressionParent.Find("Phase1");
        if (phase1Transform == null)
            return;

        phase1Text = phase1Transform.GetComponent<TMP_Text>();
        phase2Text = FindPhaseLabel(progressionParent, "Phase2");
        phase3Text = FindPhaseLabel(progressionParent, "Phase3");

        StorePhaseLabelState(phase1Text, out initialPhase1Color, out initialPhase1Scale, out initialPhase1FontStyle);
        StorePhaseLabelState(phase2Text, out initialPhase2Color, out initialPhase2Scale, out initialPhase2FontStyle);
        StorePhaseLabelState(phase3Text, out initialPhase3Color, out initialPhase3Scale, out initialPhase3FontStyle);
    }

    private void CreateGripFeedback()
    {
        if (progressBar == null)
            return;

        gripFeedbackRoot = new GameObject("GripFeedback", typeof(RectTransform));
        RectTransform rootRect = (RectTransform)gripFeedbackRoot.transform;
        rootRect.SetParent(progressBar.transform, false);
        rootRect.anchorMin = new Vector2(0.5f, 0f);
        rootRect.anchorMax = new Vector2(0.5f, 0f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = new Vector2(0f, -4f);
        rootRect.sizeDelta = new Vector2(180f, 32f);

        leftGripText = CreateGripLabel(rootRect, "GripLeft", "L", -50f);
        rightGripText = CreateGripLabel(rootRect, "GripRight", "R", 50f);
        gripFeedbackRoot.SetActive(false);
    }

    private TMP_Text CreateGripLabel(
        RectTransform parent,
        string objectName,
        string label,
        float xPosition)
    {
        GameObject labelObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        RectTransform labelRect = (RectTransform)labelObject.transform;
        labelRect.SetParent(parent, false);
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(xPosition, 0f);
        labelRect.sizeDelta = new Vector2(72f, 32f);

        TMP_Text text = labelObject.GetComponent<TMP_Text>();
        text.text = label;
        if (progressionText != null)
        {
            text.font = progressionText.font;
            text.fontSharedMaterial = progressionText.fontSharedMaterial;
        }
        text.fontSize = 26f;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        text.color = missingHandColor;
        return text;
    }

    private void UpdateGripVisuals()
    {
        UpdateGripVisual(leftGripText, leftHandPresent, leftHandClosed);
        UpdateGripVisual(rightGripText, rightHandPresent, rightHandClosed);
    }

    private void UpdateGripVisual(TMP_Text text, bool handPresent, bool handClosed)
    {
        if (text == null)
            return;

        text.color = !handPresent
            ? missingHandColor
            : handClosed
                ? closedHandColor
                : detectedHandColor;
        text.fontStyle = handClosed ? FontStyles.Bold : FontStyles.Normal;
    }

    private void ActivateNextPhase(TMP_Text nextPhaseText)
    {
        if (nextPhaseText == null)
            return;

        nextPhaseText.color = detectedHandColor;
        nextPhaseText.fontStyle = FontStyles.Bold;
        nextPhaseText.transform.localScale = initialPhase2Scale;
        nextPhaseText.DOKill();
        nextPhaseText.transform
            .DOPunchScale(Vector3.one * 0.06f, 0.3f, 1, 0.5f)
            .SetEase(Ease.OutCubic);
    }

    private static TMP_Text FindPhaseLabel(Transform parent, string phaseName)
    {
        Transform phaseTransform = parent.Find(phaseName);
        return phaseTransform != null
            ? phaseTransform.GetComponent<TMP_Text>()
            : null;
    }

    private static void StorePhaseLabelState(
        TMP_Text text,
        out Color color,
        out Vector3 scale,
        out FontStyles fontStyle)
    {
        if (text == null)
        {
            color = Color.white;
            scale = Vector3.one;
            fontStyle = FontStyles.Normal;
            return;
        }

        color = text.color;
        scale = text.transform.localScale;
        fontStyle = text.fontStyle;
    }

    private static void RestorePhaseLabel(
        TMP_Text text,
        Color color,
        Vector3 scale,
        FontStyles fontStyle)
    {
        if (text == null)
            return;

        text.DOKill();
        text.transform.DOKill();
        text.color = color;
        text.fontStyle = fontStyle;
        text.transform.localScale = scale;
    }
}
