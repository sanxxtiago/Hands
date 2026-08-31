using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public sealed class OrientationPhase3FeedbackController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private OrientationPhase3Manager orientationManager;
    [SerializeField] private ParticleEffectPlayer particleEffectPlayer;
    [SerializeField] private ParticleSystem fittedEffectPrefab;
    [SerializeField] private CanvasGroup messageGroup;
    [SerializeField] private RectTransform messageRect;

    [Header("Animacion")]
    [SerializeField, Min(0f)] private float piecePunchDuration = 0.18f;
    [SerializeField, Min(0f)] private float piecePunchScale = 0.06f;
    [SerializeField, Min(0f)] private float targetPunchDuration = 0.2f;
    [SerializeField, Min(0f)] private float targetPunchScale = 0.08f;
    [SerializeField, Min(0f)] private float messageFadeOutDuration = 0.1f;
    [SerializeField, Min(0f)] private float messageFadeInDuration = 0.15f;
    [SerializeField, Min(0f)] private float messageOffset = 8f;

    [Header("Colores")]
    [SerializeField] private Color grabbedColor = new Color(1f, 0.86f, 0.35f, 1f);
    [SerializeField] private Color targetReadyColor = new Color(1f, 0.82f, 0.25f, 1f);
    [SerializeField] private Color targetCompletedColor = new Color(0.35f, 1f, 0.55f, 1f);

    private OrientationPieceBehaviour spawnedPiece;
    private OrientationSlotBehaviour targetBehaviour;
    private RendererColorData[] pieceRenderers = Array.Empty<RendererColorData>();
    private RendererColorData[] targetRenderers = Array.Empty<RendererColorData>();
    private Vector3 pieceOriginalScale;
    private Vector3 targetOriginalScale;
    private Vector2 messageOriginalPosition;
    private Tween targetPulseTween;
    private Sequence messageSequence;
    private MaterialPropertyBlock colorPropertyBlock;
    private bool completionFeedbackPlayed;
    private bool warningLogged;

    private void Awake()
    {
        colorPropertyBlock = new MaterialPropertyBlock();

        if (orientationManager == null)
            orientationManager = GetComponent<OrientationPhase3Manager>();

        if (particleEffectPlayer == null)
            particleEffectPlayer = GetComponent<ParticleEffectPlayer>();

        if (messageRect == null && messageGroup != null)
            messageRect = messageGroup.GetComponent<RectTransform>();

        if (messageRect != null)
            messageOriginalPosition = messageRect.anchoredPosition;
    }

    private void OnEnable()
    {
        if (orientationManager == null)
            return;

        orientationManager.OnObjectsSpawned += HandleObjectsSpawned;
        orientationManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (orientationManager != null)
        {
            orientationManager.OnObjectsSpawned -= HandleObjectsSpawned;
            orientationManager.OnStateChanged -= HandleStateChanged;
        }

        KillTweens();

        if (particleEffectPlayer != null)
            particleEffectPlayer.ClearEffects();

        RestoreVisuals();
        UnsubscribeFromObjects();
    }

    private void HandleObjectsSpawned(
        OrientationPieceBehaviour piece,
        OrientationSlotBehaviour targetSlot)
    {
        UnsubscribeFromObjects();

        spawnedPiece = piece;
        targetBehaviour = targetSlot;
        completionFeedbackPlayed = false;

        if (spawnedPiece != null)
        {
            pieceOriginalScale = spawnedPiece.transform.localScale;
            CacheVisuals(spawnedPiece.transform, out pieceRenderers);

            spawnedPiece.OnGrabbed += HandlePieceGrabbed;
            spawnedPiece.OnReleased += HandlePieceReleased;
        }

        if (targetBehaviour != null)
        {
            targetOriginalScale = targetBehaviour.transform.localScale;
            CacheVisuals(targetBehaviour.transform, out targetRenderers);

            targetBehaviour.OnPieceEntered += HandlePieceEntered;
            targetBehaviour.OnPieceExited += HandlePieceExited;
            targetBehaviour.OnPieceFitted += HandlePieceFitted;
        }
    }

    private void UnsubscribeFromObjects()
    {
        if (spawnedPiece != null)
        {
            spawnedPiece.OnGrabbed -= HandlePieceGrabbed;
            spawnedPiece.OnReleased -= HandlePieceReleased;
        }

        if (targetBehaviour != null)
        {
            targetBehaviour.OnPieceEntered -= HandlePieceEntered;
            targetBehaviour.OnPieceExited -= HandlePieceExited;
            targetBehaviour.OnPieceFitted -= HandlePieceFitted;
        }

        spawnedPiece = null;
        targetBehaviour = null;
    }

    private void HandleStateChanged(OrientationPhase3State state)
    {
        AnimateMessage();
    }

    private void HandlePieceGrabbed()
    {
        if (spawnedPiece == null || completionFeedbackPlayed)
            return;

        spawnedPiece.transform.DOKill(false);
        spawnedPiece.transform.localScale = pieceOriginalScale;
        spawnedPiece.transform
            .DOPunchScale(Vector3.one * piecePunchScale, piecePunchDuration, 1, 0.5f)
            .SetEase(Ease.OutQuad);

        SetColors(pieceRenderers, grabbedColor);
    }

    private void HandlePieceReleased()
    {
        if (spawnedPiece == null || completionFeedbackPlayed)
            return;

        spawnedPiece.transform.DOKill(false);
        spawnedPiece.transform.localScale = pieceOriginalScale;
        RestoreColors(pieceRenderers);
    }

    private void HandlePieceEntered()
    {
        if (targetBehaviour == null || completionFeedbackPlayed)
            return;

        targetBehaviour.transform.DOKill(false);
        targetBehaviour.transform.localScale = targetOriginalScale;
        targetBehaviour.transform
            .DOPunchScale(Vector3.one * targetPunchScale, targetPunchDuration, 1, 0.5f)
            .SetEase(Ease.OutQuad);

        SetColors(targetRenderers, targetReadyColor);
        StartTargetPulse();
    }

    private void HandlePieceExited()
    {
        if (targetBehaviour == null || completionFeedbackPlayed)
            return;

        StopTargetPulse();
        targetBehaviour.transform.DOKill(false);
        targetBehaviour.transform.localScale = targetOriginalScale;
        RestoreColors(targetRenderers);
    }

    private void HandlePieceFitted()
    {
        if (completionFeedbackPlayed || spawnedPiece == null || targetBehaviour == null)
            return;

        completionFeedbackPlayed = true;
        StopTargetPulse();
        SetColors(targetRenderers, targetCompletedColor);
        RestoreColors(pieceRenderers);

        spawnedPiece.transform.DOKill(false);
        targetBehaviour.transform.DOKill(false);
        spawnedPiece.transform.localScale = pieceOriginalScale;

        // No hay snap: la pieza se ha soltado y queda dentro del objetivo.
        // Consolidar el estado y reproducir el feedback de completado de inmediato.
        spawnedPiece.DisableInteractionCollider();
        PlayFittedEffect();
    }

    private void PlayFittedEffect()
    {
        if (particleEffectPlayer == null || fittedEffectPrefab == null)
        {
            if (!warningLogged)
            {
                Debug.LogWarning(
                    "[OrientationPhase3] Falta la referencia del efecto de encaje; " +
                    "la fase continuará sin partículas.");
                warningLogged = true;
            }

            return;
        }

        Vector3 effectPosition = targetBehaviour != null
            ? targetBehaviour.transform.position
            : spawnedPiece != null ? spawnedPiece.transform.position : transform.position;

        particleEffectPlayer.Play(fittedEffectPrefab, effectPosition);
    }

    private void AnimateMessage()
    {
        if (messageGroup == null)
            return;

        messageSequence?.Kill(false);
        messageGroup.DOKill(false);

        if (messageRect == null)
        {
            messageGroup.alpha = 0f;
            messageGroup.DOFade(1f, messageFadeInDuration).SetEase(Ease.OutQuad);
            return;
        }

        messageRect.DOKill(false);
        messageRect.anchoredPosition = messageOriginalPosition + Vector2.up * messageOffset;
        messageSequence = DOTween.Sequence();
        messageSequence.Append(
            messageGroup
                .DOFade(0f, messageFadeOutDuration)
                .SetEase(Ease.InQuad));
        messageSequence.Append(
            messageRect
                .DOAnchorPos(messageOriginalPosition, messageFadeInDuration)
                .SetEase(Ease.OutQuad));
        messageSequence.Join(
            messageGroup
                .DOFade(1f, messageFadeInDuration)
                .SetEase(Ease.OutQuad));
    }

    private void StartTargetPulse()
    {
        StopTargetPulse();

        Color currentColor = targetReadyColor;
        targetPulseTween = DOTween.To(
                () => currentColor,
                color =>
                {
                    currentColor = color;
                    SetColors(targetRenderers, color);
                },
                targetCompletedColor,
                0.45f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopTargetPulse()
    {
        targetPulseTween?.Kill();
        targetPulseTween = null;
    }

    private void KillTweens()
    {
        targetPulseTween?.Kill();
        targetPulseTween = null;
        messageSequence?.Kill(false);
        messageSequence = null;

        if (spawnedPiece != null)
            spawnedPiece.transform.DOKill(false);

        if (targetBehaviour != null)
            targetBehaviour.transform.DOKill(false);

        messageGroup?.DOKill(false);
        messageRect?.DOKill(false);
    }

    private void RestoreVisuals()
    {
        if (spawnedPiece != null)
        {
            spawnedPiece.transform.localScale = pieceOriginalScale;
            RestoreColors(pieceRenderers);
        }

        if (targetBehaviour != null)
        {
            targetBehaviour.transform.localScale = targetOriginalScale;
            RestoreColors(targetRenderers);
        }

        if (messageGroup != null)
            messageGroup.alpha = 1f;

        if (messageRect != null)
            messageRect.anchoredPosition = messageOriginalPosition;
    }

    private static void CacheVisuals(
        Transform root,
        out RendererColorData[] renderers)
    {
        if (root == null)
        {
            renderers = Array.Empty<RendererColorData>();
            return;
        }

        Renderer[] sourceRenderers = root.GetComponentsInChildren<Renderer>(true);
        List<RendererColorData> colorRenderers = new List<RendererColorData>(sourceRenderers.Length);

        for (int i = 0; i < sourceRenderers.Length; i++)
        {
            Renderer renderer = sourceRenderers[i];
            Material material = renderer != null ? renderer.sharedMaterial : null;
            int colorPropertyId = GetColorPropertyId(material);

            if (renderer == null || colorPropertyId < 0)
                continue;

            colorRenderers.Add(new RendererColorData
            {
                renderer = renderer,
                colorPropertyId = colorPropertyId,
                originalColor = material.GetColor(colorPropertyId)
            });
        }

        renderers = colorRenderers.ToArray();
    }

    private void SetColors(RendererColorData[] renderers, Color color)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].renderer == null)
                continue;

            renderers[i].renderer.GetPropertyBlock(colorPropertyBlock);
            colorPropertyBlock.SetColor(renderers[i].colorPropertyId, color);
            renderers[i].renderer.SetPropertyBlock(colorPropertyBlock);
            colorPropertyBlock.Clear();
        }
    }

    private void RestoreColors(RendererColorData[] renderers)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].renderer == null)
                continue;

            renderers[i].renderer.GetPropertyBlock(colorPropertyBlock);
            colorPropertyBlock.SetColor(renderers[i].colorPropertyId, renderers[i].originalColor);
            renderers[i].renderer.SetPropertyBlock(colorPropertyBlock);
            colorPropertyBlock.Clear();
        }
    }

    private static int GetColorPropertyId(Material material)
    {
        if (material == null)
            return -1;

        if (material.HasProperty("_BaseColor"))
            return Shader.PropertyToID("_BaseColor");

        if (material.HasProperty("_Color"))
            return Shader.PropertyToID("_Color");

        if (material.HasProperty("_TintColor"))
            return Shader.PropertyToID("_TintColor");

        return -1;
    }

    private struct RendererColorData
    {
        public Renderer renderer;
        public int colorPropertyId;
        public Color originalColor;
    }
}
