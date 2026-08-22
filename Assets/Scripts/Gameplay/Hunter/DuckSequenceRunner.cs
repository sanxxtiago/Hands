using System;
using System.Collections;
using UnityEngine;

public class DuckSequenceRunner : MonoBehaviour
{
    // Eventos para que el HunterExercise registre los puntos y métricas.
    public event Action OnSequenceCompleted;
    public event Action OnDuckHit;
    public event Action OnDuckMissed;

    [Header("Referencias Espaciales")]
    [SerializeField] private Transform leftBoundary;
    [SerializeField] private Transform rightBoundary;
    [SerializeField] private DuckBehaviour duckPrefab;

    private DuckSequence currentSequence;
    private HunterExercise exerciseController;
    private int currentPhaseIndex;
    private int currentStepIndex;
    private DuckBehaviour activeDuck;
    private Coroutine sequenceCoroutine;

    public int DucksHit { get; private set; }
    public int DucksMissed { get; private set; }

    public void StartSequence(DuckSequence sequence, HunterExercise controller)
    {
        StopSequence();

        currentSequence = sequence;
        exerciseController = controller;
        currentPhaseIndex = 0;
        currentStepIndex = 0;
        DucksHit = 0;
        DucksMissed = 0;

        if (currentSequence == null || currentSequence.PhaseCount == 0)
        {
            Debug.LogError("DuckHunter: no hay fases configuradas en la secuencia.");
            return;
        }

        sequenceCoroutine = StartCoroutine(SequenceRoutine());
    }

    public void StopSequence()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        if (activeDuck != null)
            CleanUpDuck(activeDuck);
    }

    private void OnDisable()
    {
        StopSequence();
    }

    private IEnumerator SequenceRoutine()
    {
        while (currentPhaseIndex < currentSequence.PhaseCount)
        {
            DuckPhaseDefinition currentPhase =
                currentSequence.Phases[currentPhaseIndex];

            if (!exerciseController.progressManager.BeginPhase(currentPhaseIndex))
            {
                Debug.LogError(
                    $"DuckHunter: no se pudo iniciar la fase {currentPhaseIndex + 1}.");
                yield break;
            }

            currentStepIndex = 0;

            while (currentStepIndex < currentPhase.StepCount)
            {
                DuckSequenceStep currentStep =
                    currentPhase.Steps[currentStepIndex];

                // Espera entre patos individuales de la misma fase.
                if (currentStep.delayBeforeSpawn > 0f)
                    yield return new WaitForSeconds(currentStep.delayBeforeSpawn);

                SpawnDuck(currentStep);

                // El runner espera hasta que el pato es cazado o llega al destino.
                yield return new WaitUntil(() => activeDuck == null);

                currentStepIndex++;
            }

            if (currentPhaseIndex >= currentSequence.PhaseCount - 1)
                break;

            // Pausa entre fases; no cuenta como objetivo procesado.
            if (currentPhase.TransitionDelay > 0f)
                yield return new WaitForSeconds(currentPhase.TransitionDelay);

            currentPhaseIndex++;
        }

        sequenceCoroutine = null;
        OnSequenceCompleted?.Invoke();
    }

    private void SpawnDuck(DuckSequenceStep step)
    {
        if (duckPrefab == null)
        {
            Debug.LogError("DuckHunter: no hay un prefab de pato asignado.");
            return;
        }

        activeDuck = Instantiate(duckPrefab);

        // Suscribimos los eventos del pato.
        activeDuck.OnHit += HandleDuckHit;
        activeDuck.OnReachedDestination += HandleDuckMissed;

        // Le pasamos la información espacial y lógica.
        activeDuck.Initialize(
            step.spawnSide,
            step.requiredHand,
            step.movementDuration,
            leftBoundary.position,
            rightBoundary.position);
    }

    private void HandleDuckHit(DuckBehaviour duck)
    {
        DucksHit++;
        CleanUpDuck(duck);
        OnDuckHit?.Invoke();
    }

    private void HandleDuckMissed(DuckBehaviour duck)
    {
        DucksMissed++;
        CleanUpDuck(duck);
        OnDuckMissed?.Invoke();
    }

    private void CleanUpDuck(DuckBehaviour duck)
    {
        duck.OnHit -= HandleDuckHit;
        duck.OnReachedDestination -= HandleDuckMissed;

        Destroy(duck.gameObject);

        // Liberamos la referencia para que la coroutine siga su curso.
        if (activeDuck == duck)
            activeDuck = null;
    }
}
