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
    void Start()
    {
        progressManager.Initialize(sequence.steps.Count);
    }
    protected override void OnExerciseStart()
    {
        sequenceRunner.StartSequence(sequence, this);

    }
    public void OnDotCompleted()
    {
        progressManager.AddStep(1);
    }

}
