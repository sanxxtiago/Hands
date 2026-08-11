using System;
using System.Collections;
using UnityEngine;

public class DuckSequenceRunner : MonoBehaviour
    {
        // Eventos para que el HunterExercise registre los puntos y métricas
        public event Action OnSequenceCompleted;
        public event Action OnDuckHit;
        public event Action OnDuckMissed;

        [Header("Referencias Espaciales")]
        [SerializeField] private Transform leftBoundary;
        [SerializeField] private Transform rightBoundary;
        [SerializeField] private DuckBehaviour duckPrefab;

        private DuckSequence currentSequence;
        private int currentStepIndex;
        private DuckBehaviour activeDuck;
        private Coroutine sequenceCoroutine;

        public int DucksHit {get; private set;}
        public int DucksMissed {get; private set;}

        public void StartSequence(DuckSequence sequence)
        {
            currentSequence = sequence;
            currentStepIndex = 0;

            if (sequenceCoroutine != null)
                StopCoroutine(sequenceCoroutine);

            sequenceCoroutine = StartCoroutine(SequenceRoutine());
        }

        public void StopSequence()
        {
            if (sequenceCoroutine != null)
                StopCoroutine(sequenceCoroutine);
            
            if (activeDuck != null)
                CleanUpDuck(activeDuck);
        }

        private IEnumerator SequenceRoutine()
        {
            while (currentStepIndex < currentSequence.steps.Count)
            {
                DuckSequenceStep currentStep = currentSequence.steps[currentStepIndex];

                //Pausa antes de que salga el pato
                yield return new WaitForSeconds(currentStep.delayBeforeSpawn);

                SpawnDuck(currentStep);

                //El Runner se queda esperando aquí hasta que el pato desaparezca
                // (activeDuck se vuelve null cuando lo cazan o llega al final)
                yield return new WaitUntil(() => activeDuck == null);

                //siguiente paso
                currentStepIndex++;
            }

            OnSequenceCompleted?.Invoke();
        }

        private void SpawnDuck(DuckSequenceStep step)
        {
            activeDuck = Instantiate(duckPrefab);

            //suscribimos a los eventos del pato
            activeDuck.OnHit += HandleDuckHit;
            activeDuck.OnReachedDestination += HandleDuckMissed;

            // Le pasamos la info espacial y lógica
            activeDuck.Initialize(step.spawnSide, step.requiredHand,step.movementDuration, leftBoundary.position, rightBoundary.position);
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
            
            // Liberamos la referencia para que la corrutina siga su curso
            if (activeDuck == duck)
                activeDuck = null; 
        }
    }