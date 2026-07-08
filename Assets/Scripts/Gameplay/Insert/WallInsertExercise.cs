using UnityEngine;

public class WallInsertExercise : ExerciseController
{
    [SerializeField] private int piecesCount;
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

    void Start()
    {
        progressManager.Initialize(piecesCount);
    }

    protected override void OnExerciseStart()
    {
    }

    public void OnPieceSnapped()
    {
        progressManager.AddStep(1);
    }
}