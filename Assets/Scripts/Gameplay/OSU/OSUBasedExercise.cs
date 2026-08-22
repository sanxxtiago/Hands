using UnityEngine;

public class OSUBasedExercise : ExerciseController
{
    public OSUSequenceRunner sequenceRunner;
    [SerializeField] private OSUSequence sequence;

    public float TotalInteractionTime => sequenceRunner.TotalInteractionTime;
    public int InteractionCount => sequenceRunner.InteractionCount;

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
        progressManager.Initialize(sequence != null
            ? sequence.GetPhaseTargets()
            : System.Array.Empty<int>());
    }
    protected override void OnExerciseStart()
    {
        sequenceRunner.StartSequence(sequence, this);
    }

    protected override void SetSpecificData()
    {
        sessionRecorder.SetOsuData(
            TotalInteractionTime,
            InteractionCount
        );
    }
}
