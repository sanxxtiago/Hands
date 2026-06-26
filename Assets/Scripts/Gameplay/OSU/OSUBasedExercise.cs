public class OSUBasedExercise : ExerciseController
{
    public OSUSequenceRunner sequenceRunner;

    protected override void OnEnable()
    {
        base.OnEnable();
        PieceBehaviour.OnPieceSnapped += OnDotCompleted;
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        PieceBehaviour.OnPieceSnapped -= OnDotCompleted;
    }
    protected override void OnExerciseStart()
    {
        sequenceRunner.StartSequence();
        progressManager.Initialize(progressManager.targetSteps);

    }
    public void OnDotCompleted()
    {
        progressManager.AddStep();
    }

}
