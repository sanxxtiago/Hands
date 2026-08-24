using System;
using System.Collections;
using UnityEngine;

public struct DuckScoreContext
{
    public int duckIndex;
    public float spawnTime;
    public float reactionTime;
    public float availableTime;
    public bool wasHit;
    public bool wasMissed;
    public HandType requiredHand;
    public HandType hitHand;
}

public class DuckSequenceRunner : MonoBehaviour
{
    // Eventos para que el HunterExercise registre los puntos y métricas.
    public event Action OnSequenceCompleted;
    public event Action<int> OnSequenceStarted;
    public event Action<DuckScoreContext> OnDuckSpawned;
    public event Action<DuckScoreContext> OnDuckHit;
    public event Action<DuckScoreContext> OnDuckMissed;

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
    private int nextDuckIndex;
    private int activeDuckIndex;

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
        nextDuckIndex = 0;

        if (currentSequence == null || currentSequence.PhaseCount == 0)
        {
            Debug.LogError("[ScoreSystem][DuckHunter] No hay fases configuradas en la secuencia.");
            return;
        }

        OnSequenceStarted?.Invoke(CountDucks(currentSequence));
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
                    $"[ScoreSystem][DuckHunter] No se pudo iniciar la fase {currentPhaseIndex + 1}.");
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
            Debug.LogError("[ScoreSystem][DuckHunter] No hay un prefab de pato asignado.");
            return;
        }

        activeDuck = Instantiate(duckPrefab);
        activeDuckIndex = nextDuckIndex++;

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

        OnDuckSpawned?.Invoke(CreateContext(activeDuck));
    }

    private void HandleDuckHit(DuckBehaviour duck)
    {
        if (duck != activeDuck || duck.IsMissed)
            return;

        DuckScoreContext context = CreateContext(duck);
        DucksHit++;
        OnDuckHit?.Invoke(context);
        CleanUpDuck(duck);
    }

    private void HandleDuckMissed(DuckBehaviour duck)
    {
        if (duck != activeDuck || duck.IsHit)
            return;

        DuckScoreContext context = CreateContext(duck);
        DucksMissed++;
        OnDuckMissed?.Invoke(context);
        CleanUpDuck(duck);
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

    private DuckScoreContext CreateContext(DuckBehaviour duck)
    {
        return new DuckScoreContext
        {
            duckIndex = activeDuckIndex,
            spawnTime = duck.SpawnTime,
            reactionTime = duck.HasReactionTime ? duck.ReactionTime : 0f,
            availableTime = duck.AvailableTime,
            wasHit = duck.IsHit,
            wasMissed = duck.IsMissed,
            requiredHand = duck.RequiredHand,
            hitHand = duck.HitHand
        };
    }

    private static int CountDucks(DuckSequence sequence)
    {
        int count = 0;
        for (int i = 0; i < sequence.PhaseCount; i++)
            count += sequence.Phases[i]?.StepCount ?? 0;

        return count;
    }
}
