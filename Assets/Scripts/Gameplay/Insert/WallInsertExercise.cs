public class WallInsertExercise : ExerciseController
{
    protected override void OnEnable()
    {
        base.OnEnable();
        PieceBehaviour.OnPieceSnapped += OnPieceSnapped;
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        PieceBehaviour.OnPieceSnapped -= OnPieceSnapped;
    }
    
    protected override void OnExerciseStart()
    {
        progressManager.Initialize(progressManager.targetSteps);
    }

    public void OnPieceSnapped()
    {
        progressManager.AddStep();
    }
}