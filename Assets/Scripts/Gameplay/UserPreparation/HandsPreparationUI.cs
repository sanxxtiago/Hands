using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HandsPreparationUI : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private HandsDetection handsDetection;
    [SerializeField] private HandsPreparationFlow preparationFlow;

    [Header("Canvas")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private RectTransform leftStatusRoot;
    [SerializeField] private RectTransform rightStatusRoot;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;
    [SerializeField] private Vector2 statusOffset = new Vector2(0f, 120f);

    [Header("Status Icons")]
    [SerializeField] private Image leftStatusIcon;
    [SerializeField] private Image rightStatusIcon;
    [SerializeField] private Sprite missingSprite;
    [SerializeField] private Sprite detectedSprite;

    [Header("Message")]
    [SerializeField] private TMP_Text messageText;

    private RectTransform canvasRect;
    private Camera projectionCamera;

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        canvasRect = uiCanvas.transform as RectTransform;
        projectionCamera = uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? Camera.main
            : uiCanvas.worldCamera;

        if (projectionCamera == null)
        {
            Debug.LogError(
                "[HandsPreparationUI] No se encontro una camara para proyectar las manos sobre el Canvas.",
                this);
            enabled = false;
            return;
        }

        messageText.text = "Coloca ambas manos sobre el sensor";
        SetDetectionIcon(leftStatusIcon, false);
        SetDetectionIcon(rightStatusIcon, false);
    }

    private void OnEnable()
    {
        if (handsDetection != null)
        {
            handsDetection.OnLeftHandDetectionChanged += HandleLeftHandDetectionChanged;
            handsDetection.OnRightHandDetectionChanged += HandleRightHandDetectionChanged;
            SyncDetectionState();
        }

        if (preparationFlow != null)
        {
            preparationFlow.OnWaitingForHands += HandleWaitingForHands;
            preparationFlow.OnCountdownStep += HandleCountdownStep;
            preparationFlow.OnPreparationReady += HandlePreparationReady;
            SyncFlowState();
        }
    }

    private void OnDisable()
    {
        if (handsDetection != null)
        {
            handsDetection.OnLeftHandDetectionChanged -= HandleLeftHandDetectionChanged;
            handsDetection.OnRightHandDetectionChanged -= HandleRightHandDetectionChanged;
        }

        if (preparationFlow != null)
        {
            preparationFlow.OnWaitingForHands -= HandleWaitingForHands;
            preparationFlow.OnCountdownStep -= HandleCountdownStep;
            preparationFlow.OnPreparationReady -= HandlePreparationReady;
        }
    }

    private void LateUpdate()
    {
        PositionStatus(leftStatusRoot, leftHand);
        PositionStatus(rightStatusRoot, rightHand);
    }

    private void HandleLeftHandDetectionChanged(bool detected)
    {
        SetDetectionIcon(leftStatusIcon, detected);
    }

    private void HandleRightHandDetectionChanged(bool detected)
    {
        SetDetectionIcon(rightStatusIcon, detected);
    }

    private void HandleWaitingForHands()
    {
        messageText.text = "Coloca ambas manos sobre el sensor";
    }

    private void HandleCountdownStep(int secondsRemaining)
    {
        messageText.SetText("Preparando...\n{0}", secondsRemaining);
    }

    private void HandlePreparationReady()
    {
        messageText.text = "\u00A1Listo!";
    }

    private void SyncDetectionState()
    {
        SetDetectionIcon(leftStatusIcon, handsDetection.IsLeftDetected);
        SetDetectionIcon(rightStatusIcon, handsDetection.IsRightDetected);
    }

    private void SyncFlowState()
    {
        if (preparationFlow.IsReadyMessageVisible)
        {
            HandlePreparationReady();
            return;
        }

        if (preparationFlow.IsCountdownRunning && preparationFlow.CurrentCountdownStep > 0)
        {
            HandleCountdownStep(preparationFlow.CurrentCountdownStep);
            return;
        }

        HandleWaitingForHands();
    }

    private void PositionStatus(RectTransform statusRoot, Transform hand)
    {
        if (statusRoot == null || hand == null || uiCanvas == null || canvasRect == null)
            return;

        Camera eventCamera = uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : uiCanvas.worldCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            projectionCamera,
            hand.position);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                eventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        statusRoot.anchoredPosition = localPoint + statusOffset;
    }

    private void SetDetectionIcon(Image image, bool detected)
    {
        if (image == null)
            return;

        image.sprite = detected ? detectedSprite : missingSprite;
        image.color = Color.white;
    }

    private bool ValidateReferences()
    {
        if (handsDetection == null || preparationFlow == null)
        {
            Debug.LogError(
                "[HandsPreparationUI] Faltan HandsDetection o HandsPreparationFlow.",
                this);
            return false;
        }

        if (uiCanvas == null || leftStatusRoot == null || rightStatusRoot == null ||
            leftHand == null || rightHand == null)
        {
            Debug.LogError(
                "[HandsPreparationUI] Faltan referencias del Canvas o de las manos.",
                this);
            return false;
        }

        if (leftStatusIcon == null || rightStatusIcon == null ||
            missingSprite == null || detectedSprite == null || messageText == null)
        {
            Debug.LogError(
                "[HandsPreparationUI] Faltan referencias de los indicadores o del mensaje.",
                this);
            return false;
        }

        return true;
    }
}
