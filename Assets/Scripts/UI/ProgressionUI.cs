using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ProgressionUI : MonoBehaviour
{
    private const int ExpectedPhaseCount = 3;

    // Se conserva para no romper las referencias existentes en las escenas.
    [SerializeField] private Image phasePoint1;
    [SerializeField] private Image phasePoint2;
    [SerializeField] private Image phasePoint3;
    [SerializeField] private Sprite emptyPhasePointSprite;
    [SerializeField] private Sprite filledPhasePointSprite;
    [SerializeField, Min(0f)] private float phasePointAnimationDuration = 0.2f;

    private Image[] phasePoints;
    private Tween[] phasePointTweens;
    private Vector3[] phasePointScales;
    private int completedPhaseCount;
    private int lastWarnedPhaseCount = int.MinValue;

    private void Awake()
    {
        phasePoints = new[]
        {
            phasePoint1,
            phasePoint2,
            phasePoint3
        };
        phasePointTweens = new Tween[ExpectedPhaseCount];
        phasePointScales = new Vector3[ExpectedPhaseCount];

        for (int i = 0; i < phasePoints.Length; i++)
        {
            phasePointScales[i] = phasePoints[i] != null
                ? phasePoints[i].transform.localScale
                : Vector3.one;
        }

        ResetPhasePoints();
    }

    private void OnEnable()
    {
        ExerciseProgressManager.OnPhaseChanged += UpdatePhase;
        ExerciseProgressManager.OnPhaseCompleted += CompletePhase;
    }

    private void OnDisable()
    {
        ExerciseProgressManager.OnPhaseChanged -= UpdatePhase;
        ExerciseProgressManager.OnPhaseCompleted -= CompletePhase;

        KillPhasePointTweens(true);
    }

    private void UpdatePhase(int phaseIndex, int phaseCount)
    {
        WarnIfUnexpectedPhaseCount(phaseCount);

        if (phaseIndex < 0 || phaseIndex >= ExpectedPhaseCount)
        {
            if (phaseIndex < 0)
                ResetPhasePoints();

            Debug.LogWarning(
                $"[ProgressionUI] Índice de fase inválido: {phaseIndex} para " +
                $"{phaseCount} fases.");
            return;
        }

        if (phaseIndex == 0)
            ResetPhasePoints();
    }

    private void CompletePhase(int phaseIndex, int phaseCount)
    {
        WarnIfUnexpectedPhaseCount(phaseCount);

        if (phaseIndex < 0 || phaseIndex >= ExpectedPhaseCount)
        {
            Debug.LogWarning(
                $"[ProgressionUI] Índice de fase completada inválido: {phaseIndex} " +
                $"para {phaseCount} fases.");
            return;
        }

        if (phaseIndex < completedPhaseCount)
            return;

        if (phaseIndex != completedPhaseCount)
        {
            Debug.LogWarning(
                $"[ProgressionUI] La fase completada {phaseIndex + 1} llegó fuera " +
                $"de orden. Se esperaba la fase {completedPhaseCount + 1}.");
            return;
        }

        Image phasePoint = phasePoints[phaseIndex];

        if (phasePoint != null)
        {
            phasePoint.sprite = filledPhasePointSprite;
            AnimatePhasePoint(phaseIndex);
        }

        completedPhaseCount++;
    }

    private void WarnIfUnexpectedPhaseCount(int phaseCount)
    {
        if (phaseCount == ExpectedPhaseCount)
        {
            lastWarnedPhaseCount = int.MinValue;
            return;
        }

        if (lastWarnedPhaseCount != phaseCount)
        {
            Debug.LogWarning(
                $"[ProgressionUI] Se esperaban {ExpectedPhaseCount} fases, pero se " +
                $"recibieron {phaseCount}. Se usarán solo los puntos disponibles.");
            lastWarnedPhaseCount = phaseCount;
        }
    }

    private void ResetPhasePoints()
    {
        completedPhaseCount = 0;

        if (phasePoints == null)
            return;

        for (int i = 0; i < phasePoints.Length; i++)
        {
            KillPhasePointTween(i, true);

            if (phasePoints[i] != null)
                phasePoints[i].sprite = emptyPhasePointSprite;
        }
    }

    private void AnimatePhasePoint(int index)
    {
        if (phasePoints[index] == null)
            return;

        KillPhasePointTween(index, false);

        Transform pointTransform = phasePoints[index].transform;
        Vector3 originalScale = phasePointScales[index];

        if (phasePointAnimationDuration <= 0f)
        {
            pointTransform.localScale = originalScale;
            return;
        }

        pointTransform.localScale = originalScale * 0.85f;
        phasePointTweens[index] = pointTransform
            .DOScale(originalScale, phasePointAnimationDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => phasePointTweens[index] = null);
    }

    private void KillPhasePointTweens(bool resetScale)
    {
        if (phasePointTweens == null)
            return;

        for (int i = 0; i < phasePointTweens.Length; i++)
            KillPhasePointTween(i, resetScale);
    }

    private void KillPhasePointTween(int index, bool resetScale)
    {
        phasePointTweens[index]?.Kill();
        phasePointTweens[index] = null;

        if (resetScale && phasePoints[index] != null)
            phasePoints[index].transform.localScale = phasePointScales[index];
    }

   
}
