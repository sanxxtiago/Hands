using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

[Serializable]
public class InsertPhaseDefinition
{
    [SerializeField] private GameObject prefab;
    [SerializeField, Min(1)] private int expectedPieces = 1;

    public GameObject Prefab => prefab;
    public int ExpectedPieces => expectedPieces;
}

public class WallInsertExercise : ExerciseController
{
    [Tooltip("Fases ordenadas de menor a mayor dificultad.")]
    [SerializeField] private List<InsertPhaseDefinition> phases = new();

    [Tooltip("Tiempo de espera antes de mostrar la siguiente fase.")]
    [SerializeField, Min(0f)] private float phaseTransitionDelay = 0.75f;

    private int currentPhaseIndex;
    private GameObject currentPhaseInstance;
    private bool invalidConfiguration;
    private bool phaseTransitionInProgress;

    public float CompletionTime => elapsedTime;

    protected override void OnEnable()
    {
        base.OnEnable();
        PieceBehaviour.OnPieceSnapped += OnPieceSnapped;
    }
    protected override void OnDisable()
    {
        StopAllCoroutines();
        phaseTransitionInProgress = false;
        DeactivateCurrentPhase();
        PieceBehaviour.OnPieceSnapped -= OnPieceSnapped;
        base.OnDisable();
    }

    void Start()
    {
        int[] phaseTargets = new int[phases.Count];

        for (int i = 0; i < phases.Count; i++)
            phaseTargets[i] = phases[i]?.ExpectedPieces ?? 0;

        progressManager.Initialize(phaseTargets);
    }

    protected override void OnExerciseStart()
    {
        if (phases.Count == 0)
        {
            return;
        }

        currentPhaseIndex = -1;
        invalidConfiguration = false;
        AdvanceToNextPhase();
    }

    protected override bool IsExerciseCompleted()
    {
        return invalidConfiguration || base.IsExerciseCompleted();
    }

    public void OnPieceSnapped(PieceBehaviour piece)
    {
        if (phases.Count == 0 || invalidConfiguration)
            return;

        if (phaseTransitionInProgress)
            return;

        if (currentPhaseInstance == null ||
            piece == null ||
            !piece.transform.IsChildOf(currentPhaseInstance.transform))
        {
            return;
        }

        progressManager.AddCompletedStep();

        if (!progressManager.IsCompleted())
            return;

        if (currentPhaseIndex >= phases.Count - 1)
            return;

        StartCoroutine(AdvanceToNextPhaseAfterDelay());
    }

    protected override void SetSpecificData()
    {
        sessionRecorder.SetInsertPiecesData(CompletionTime);
    }

    private void AdvanceToNextPhase()
    {
        DeactivateCurrentPhase();

        currentPhaseIndex++;

        if (currentPhaseIndex >= phases.Count)
        {
            return;
        }

        InsertPhaseDefinition phase = phases[currentPhaseIndex];

        if (phase == null || phase.Prefab == null)
        {
            Debug.LogError(
                $"Insert: la fase {currentPhaseIndex + 1} no tiene un prefab asignado.");
            invalidConfiguration = true;
            return;
        }

        currentPhaseInstance = Instantiate(phase.Prefab);

        PieceBehaviour[] phasePieces =
            currentPhaseInstance.GetComponentsInChildren<PieceBehaviour>(true);

        if (phasePieces.Length != phase.ExpectedPieces)
        {
            Debug.LogWarning(
                $"Insert: la fase {currentPhaseIndex + 1} espera " +
                $"{phase.ExpectedPieces} piezas, pero contiene {phasePieces.Length}.");
        }

        foreach (PieceBehaviour piece in phasePieces)
            piece.ApplyChirality();

        progressManager.BeginPhase(currentPhaseIndex);
    }

    private IEnumerator AdvanceToNextPhaseAfterDelay()
    {
        phaseTransitionInProgress = true;
        DeactivateCurrentPhase();

        if (phaseTransitionDelay > 0f)
            yield return new WaitForSeconds(phaseTransitionDelay);

        phaseTransitionInProgress = false;
        AdvanceToNextPhase();
    }

    private void DeactivateCurrentPhase()
    {
        if (currentPhaseInstance == null)
            return;

        currentPhaseInstance.SetActive(false);
        Destroy(currentPhaseInstance);
        currentPhaseInstance = null;
    }

}
