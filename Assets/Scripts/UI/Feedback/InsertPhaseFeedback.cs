using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(ParticleEffectPlayer))]
public class InsertPhaseFeedback : MonoBehaviour
{
    [SerializeField] private ParticleSystem phaseCompleteEffectPrefab;
    [SerializeField] private RectTransform phasePoint1;
    [SerializeField] private RectTransform phasePoint2;
    [SerializeField] private RectTransform phasePoint3;
    [SerializeField, Min(0f)] private float punchDuration = 0.3f;
    [SerializeField] private Vector3 punchScale = new Vector3(0.18f, 0.18f, 0.18f);
    [SerializeField, Min(0)] private int punchVibrato = 1;
    [SerializeField, Range(0f, 1f)] private float punchElasticity = 0.6f;

    private ParticleEffectPlayer particleEffectPlayer;
    private RectTransform[] phasePoints;
    private readonly HashSet<int> completedPhases = new HashSet<int>();
    private readonly Dictionary<Transform, Vector3> originalScales =
        new Dictionary<Transform, Vector3>();

    private void Awake()
    {
        TryGetComponent<ParticleEffectPlayer>(out particleEffectPlayer);
        phasePoints = new[] { phasePoint1, phasePoint2, phasePoint3 };

        for (int i = 0; i < phasePoints.Length; i++)
        {
            if (phasePoints[i] != null)
                originalScales[phasePoints[i]] = phasePoints[i].localScale;
        }

        if (particleEffectPlayer == null)
        {
            Debug.LogError(
                "[InsertFeedback] Falta ParticleEffectPlayer en el feedback de fases.",
                this);
        }

        if (phaseCompleteEffectPrefab == null)
        {
            Debug.LogWarning(
                "[InsertFeedback] El prefab de partículas de finalización de fase no está asignado.",
                this);
        }
    }

    private void OnEnable()
    {
        ExerciseProgressManager.OnPhaseCompleted += OnPhaseCompleted;
        GameManager.OnExcerciseStart += ResetForExercise;
    }

    private void OnDisable()
    {
        ExerciseProgressManager.OnPhaseCompleted -= OnPhaseCompleted;
        GameManager.OnExcerciseStart -= ResetForExercise;
        ResetFeedbackState();
    }

    private void OnPhaseCompleted(int phaseIndex, int phaseCount)
    {
        if (phaseIndex < 0 || phaseIndex >= phasePoints.Length)
        {
            Debug.LogWarning(
                $"[InsertFeedback] Índice de fase inválido: {phaseIndex} para " +
                $"{phaseCount} fases y {phasePoints.Length} puntos configurados.",
                this);
            return;
        }

        if (!completedPhases.Add(phaseIndex))
            return;

        RectTransform phasePoint = phasePoints[phaseIndex];
        if (phasePoint == null)
        {
            Debug.LogWarning(
                $"[InsertFeedback] El punto de UI de la fase {phaseIndex + 1} " +
                "no está asignado.",
                this);
            return;
        }

        Vector3 pointPosition = phasePoint.position;
        particleEffectPlayer?.Play(
            phaseCompleteEffectPrefab,
            pointPosition,
            phasePoint);
        Punch(phasePoint);
    }

    private void ResetForExercise()
    {
        ResetFeedbackState();
    }

    private void ResetFeedbackState()
    {
        completedPhases.Clear();
        RestoreScales();
        particleEffectPlayer?.ClearEffects();
    }

    private void Punch(RectTransform target)
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
    }

    private static void RestoreScale(Transform target, Vector3 originalScale)
    {
        if (target == null)
            return;

        target.DOKill();
        target.localScale = originalScale;
    }
}
