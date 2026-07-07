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
            while (currentStepIndex < currentSequence.steps.Length)
            {
                DuckSequenceStep currentStep = currentSequence.steps[currentStepIndex];

                // 1. Pausa terapéutica antes de que salga el pato
                yield return new WaitForSeconds(currentStep.delayBeforeSpawn);

                // 2. Aparece el pato
                SpawnDuck(currentStep);

                // 3. El Runner se queda esperando aquí hasta que el pato desaparezca
                // (activeDuck se vuelve null cuando lo cazan o llega al final)
                yield return new WaitUntil(() => activeDuck == null);

                // 4. Pasamos al siguiente paso
                currentStepIndex++;
            }

            // 5. Se acabó la terapia
            OnSequenceCompleted?.Invoke();
        }

        private void SpawnDuck(DuckSequenceStep step)
        {
            // Nota: Para optimizar más adelante puedes usar un Object Pool
            activeDuck = Instantiate(duckPrefab);

            // Nos suscribimos a los eventos del pato
            activeDuck.OnHit += HandleDuckHit;
            activeDuck.OnReachedDestination += HandleDuckMissed;

            // Le pasamos la info espacial y lógica
            activeDuck.Initialize(step.spawnSide, step.movementDuration, leftBoundary.position, rightBoundary.position);
        }

        private void HandleDuckHit(DuckBehaviour duck)
        {
            CleanUpDuck(duck);
            OnDuckHit?.Invoke();
        }

        private void HandleDuckMissed(DuckBehaviour duck)
        {
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