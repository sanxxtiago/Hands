using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(ParticleEffectPlayer))]
public class InsertPieceFeedback : MonoBehaviour
{
    [SerializeField] private ParticleSystem pieceSnapEffectPrefab;
    [SerializeField, Min(0f)] private float punchDuration = 0.25f;
    [SerializeField] private Vector3 punchScale = new Vector3(0.12f, 0.12f, 0.12f);
    [SerializeField, Min(0)] private int punchVibrato = 1;
    [SerializeField, Range(0f, 1f)] private float punchElasticity = 0.5f;
    [SerializeField] private bool playParticles = true;
    [SerializeField] private bool playPunch = true;

    private ParticleEffectPlayer particleEffectPlayer;
    private readonly HashSet<PieceBehaviour> processedPieces =
        new HashSet<PieceBehaviour>();
    private readonly Dictionary<Transform, Vector3> originalScales =
        new Dictionary<Transform, Vector3>();

    private void Awake()
    {
        if (!TryGetComponent<ParticleEffectPlayer>(out particleEffectPlayer))
        {
            Debug.LogError(
                "[InsertFeedback] Falta ParticleEffectPlayer en el feedback de piezas.",
                this);
        }

        if (playParticles && pieceSnapEffectPrefab == null)
        {
            Debug.LogWarning(
                "[InsertFeedback] El prefab de partículas de encaje no está asignado.",
                this);
        }
    }

    private void OnEnable()
    {
        PieceBehaviour.OnPieceSnapped += OnPieceSnapped;
        GameManager.OnExcerciseStart += ResetForExercise;
    }

    private void OnDisable()
    {
        PieceBehaviour.OnPieceSnapped -= OnPieceSnapped;
        GameManager.OnExcerciseStart -= ResetForExercise;
        ResetFeedbackState();
    }

    private void OnPieceSnapped(PieceBehaviour piece)
    {
        if (piece == null)
        {
            Debug.LogWarning(
                "[InsertFeedback] Se recibió OnPieceSnapped con una pieza nula.",
                this);
            return;
        }

        if (!processedPieces.Add(piece))
            return;

        Transform pieceTransform = piece.transform;
        if (pieceTransform == null)
            return;

        Vector3 piecePosition = pieceTransform.position;

        if (playParticles && particleEffectPlayer != null)
        {
            particleEffectPlayer.Play(pieceSnapEffectPrefab, piecePosition);
        }

        if (!playPunch || !piece.gameObject.activeInHierarchy)
            return;

        Punch(pieceTransform);
    }

    private void ResetForExercise()
    {
        ResetFeedbackState();
    }

    private void ResetFeedbackState()
    {
        processedPieces.Clear();
        RestoreScales();
        particleEffectPlayer?.ClearEffects();
    }

    private void Punch(Transform target)
    {
        if (!originalScales.TryGetValue(target, out Vector3 originalScale))
        {
            originalScale = target.localScale;
            originalScales.Add(target, originalScale);
        }

        target.DOKill();
        target.localScale = originalScale;

        if (punchDuration <= 0f || punchVibrato <= 0)
            return;

        target
            .DOPunchScale(
                punchScale,
                punchDuration,
                punchVibrato,
                punchElasticity)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => RestoreScale(target, originalScale));
    }

    private void RestoreScales()
    {
        foreach (KeyValuePair<Transform, Vector3> scale in originalScales)
            RestoreScale(scale.Key, scale.Value);

        originalScales.Clear();
    }

    private static void RestoreScale(Transform target, Vector3 originalScale)
    {
        if (target == null)
            return;

        target.DOKill();
        target.localScale = originalScale;
    }
}
