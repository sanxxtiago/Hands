using UnityEngine;

public class OSUBasedExercise : ExerciseController
{
    public OSUSequenceRunner sequenceRunner;
    [SerializeField] private OSUSequence sequence;

    protected override void OnEnable()
    {
        base.OnEnable();
    }
    protected override void OnDisable()
    {
        base.OnDisable();
    }
    protected override void OnExerciseStart()
    {
        sequenceRunner.StartSequence(sequence);
        progressManager.Initialize(progressManager.targetSteps);

    }
    public void OnDotCompleted()
    {
        progressManager.AddStep();
    }

}
