using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleEffectPlayer))]
public abstract class ExerciseController : MonoBehaviour
{
    public GameManager gameManager;
    public ExerciseProgressManager progressManager;
    public ExerciseFeedbackSystem feedbackSystem;
    public SessionRecorder sessionRecorder;

[Header("Feedback espacial de fase")]
    [Tooltip("Prefab de particulas 3D que se muestra al completar una fase.")]
    [SerializeField] private ParticleSystem phaseCompleteEffectPrefab;
    [Tooltip("Punto estable del escenario donde aparece el efecto de fase.")]
    [SerializeField] private Transform phaseFeedbackAnchor;

    [Header("Pacing")]
    [Tooltip("Espera tras completar el ejercicio antes de pasar a resultados, para apreciar los efectos finales (muerte del último pato, partículas, etc.).")]
    [SerializeField, Min(0f)] private float exerciseEndDelay = 3f;

    protected float elapsedTime = 0;
    private ParticleEffectPlayer particleEffectPlayer;
    private readonly HashSet<int> completedPhases = new HashSet<int>();

    protected virtual void Awake()
    {
        particleEffectPlayer = GetComponent<ParticleEffectPlayer>();
    }

    protected virtual void OnEnable()
    {
        GameManager.OnExcerciseStart += HandleStartExercise;
        ExerciseProgressManager.OnPhaseCompleted += HandlePhaseCompleted;
    }

    protected virtual void OnDisable()
    {
        GameManager.OnExcerciseStart -= HandleStartExercise;
        ExerciseProgressManager.OnPhaseCompleted -= HandlePhaseCompleted;
        particleEffectPlayer?.ClearEffects();
    }

    public void HandleStartExercise()
    {
        ResetPhaseFeedback();
        StartCoroutine(ExerciseRoutine());
    }

    private void HandlePhaseCompleted(int phaseIndex, int phaseCount)
    {
if (phaseIndex < 0 || phaseIndex >= phaseCount ||
            !completedPhases.Add(phaseIndex))
        {
            return;
        }

        if (progressManager == null ||
            progressManager.CurrentPhaseIndex != phaseIndex ||
            progressManager.CurrentPhaseCompletedSteps < progressManager.CurrentPhaseTarget)
        {
            return;
        }

        if (phaseCompleteEffectPrefab == null || phaseFeedbackAnchor == null)
            return;

        particleEffectPlayer?.Play(
            phaseCompleteEffectPrefab,
            phaseFeedbackAnchor.position);
    }

    private void ResetPhaseFeedback()
    {
        completedPhases.Clear();
        particleEffectPlayer?.ClearEffects();
    }

    IEnumerator ExerciseRoutine()
    {
        elapsedTime = 0f;
        OnExerciseStart();

        feedbackSystem?.BeginExercise();

        
        if (SessionManager.Instance.CurrentSession == null)
        {
            SessionManager.Instance.BeginSession();
        }

        while (!IsExerciseCompleted())
        {
            elapsedTime += Time.deltaTime;
            //El sistema de feedback evalúa durante la duración del ejercicio
            feedbackSystem?.Evaluate(elapsedTime, Time.deltaTime);
            yield return null;
        }

        // Pausa opcional para que el paciente vea los efectos finales
        // (muerte del último objetivo, partículas, etc.) antes de pasar a resultados.
        if (exerciseEndDelay > 0f)
            yield return new WaitForSeconds(exerciseEndDelay);

        OnExerciseEnd(elapsedTime);
        if (SessionManager.Instance.CurrentSession != null)
        {
            SessionManager.Instance.EndSession();
        }
    }

    protected abstract void OnExerciseStart();

    protected virtual bool IsExerciseCompleted()
    {
        return progressManager != null && progressManager.IsExerciseCompleted();
    }

    protected void OnExerciseEnd(float duration)
    {
        SetSpecificData();
        gameManager.EndExercise(duration);
    }

    protected abstract void SetSpecificData();

}
