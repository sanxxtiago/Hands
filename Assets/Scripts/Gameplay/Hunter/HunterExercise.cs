using UnityEngine;

public class HunterExercise : ExerciseController
{
    [SerializeField] private DuckSequenceRunner sequenceRunner;
    [SerializeField] private DuckSequence sequence;

    protected override void OnEnable()
    {
        base.OnEnable();

        sequenceRunner.OnDuckHit += HandleDuckHit;
        sequenceRunner.OnDuckMissed += HandleDuckMissed;
        sequenceRunner.OnSequenceCompleted += HandleSequenceCompleted;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        sequenceRunner.OnDuckHit -= HandleDuckHit;
        sequenceRunner.OnDuckMissed -= HandleDuckMissed;
        sequenceRunner.OnSequenceCompleted -= HandleSequenceCompleted;
    }

    protected override void OnExerciseStart()
    {
        sequenceRunner.StartSequence(sequence);
    }

    private void HandleDuckHit()
    {
        // métricas
    }

    private void HandleDuckMissed()
    {
        // métricas
    }

    private void HandleSequenceCompleted()
    {
        //OnExerciseEnd();
    }
}