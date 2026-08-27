using DG.Tweening;
using UnityEngine;

public sealed class FailureTimeDilationEffect : MonoBehaviour
{
    [SerializeField] private OSUSequenceRunner osuSequenceRunner;
    [SerializeField] private DuckSequenceRunner duckSequenceRunner;
    [Tooltip("Time scale aplicado durante el golpe de fallo.")]
    [SerializeField, Range(0.05f, 1f)] private float slowedTimeScale = 0.25f;
    [Tooltip("Duracion real del time scale reducido, en segundos.")]
    [SerializeField, Min(0f)] private float slowDuration = 0.08f;
    [Tooltip("Duracion real de la restauracion del time scale a 1.")]
    [SerializeField, Min(0f)] private float restoreDuration = 0.06f;

    private Tween restoreTween;

    private void OnEnable()
    {
        if (osuSequenceRunner == null)
            osuSequenceRunner = FindFirstObjectByType<OSUSequenceRunner>();

        if (duckSequenceRunner == null)
            duckSequenceRunner = FindFirstObjectByType<DuckSequenceRunner>();

        if (osuSequenceRunner != null)
        {
            osuSequenceRunner.OnTargetMissed += HandleTargetFailure;
            osuSequenceRunner.OnTargetFailed += HandleTargetFailure;
        }

        if (duckSequenceRunner != null)
            duckSequenceRunner.OnDuckMissed += HandleDuckMissed;
    }

    private void OnDisable()
    {
        if (osuSequenceRunner != null)
        {
            osuSequenceRunner.OnTargetMissed -= HandleTargetFailure;
            osuSequenceRunner.OnTargetFailed -= HandleTargetFailure;
        }

        if (duckSequenceRunner != null)
            duckSequenceRunner.OnDuckMissed -= HandleDuckMissed;

        StopDilation();
    }

    private void HandleTargetFailure(OSUTargetScoreContext context)
    {
        Play();
    }

    private void HandleDuckMissed(DuckScoreContext context)
    {
        if (!context.wasMissed || context.wasHit)
            return;

        Play();
    }

    public void Play()
    {
        restoreTween?.Kill();
        restoreTween = null;
        Time.timeScale = Mathf.Clamp(slowedTimeScale, 0.05f, 1f);

        float holdDuration = Mathf.Max(0f, slowDuration);
        float restoreTime = Mathf.Max(0f, restoreDuration);

        if (restoreTime <= 0f)
        {
            restoreTween = DOVirtual
                .DelayedCall(holdDuration, RestoreTimeScale)
                .SetUpdate(true);
            return;
        }

        restoreTween = DOTween
            .To(
                () => Time.timeScale,
                value => Time.timeScale = value,
                1f,
                restoreTime)
            .SetDelay(holdDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(RestoreTimeScale);
    }

    private void StopDilation()
    {
        restoreTween?.Kill();
        restoreTween = null;
        Time.timeScale = 1f;
    }

    private void RestoreTimeScale()
    {
        restoreTween = null;
        Time.timeScale = 1f;
    }
}
