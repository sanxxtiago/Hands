using UnityEngine;

public class HunterExercise : ExerciseController
{
    [SerializeField] private DuckSequenceRunner sequenceRunner;
    [SerializeField] private DuckSequence sequence;

    public int DucksHit => sequenceRunner.DucksHit;
    public int DucksMissed => sequenceRunner.DucksMissed;
    
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

    private void HandleDuckHit()
    {
        // métricas

        progressManager.AddCompletedStep();
    }

    private void HandleDuckMissed()
    {
        progressManager.AddMissedStep();

    }

    private void HandleSequenceCompleted()
    {
        //OnExerciseEnd();
    }

    protected override void SetSpecificData()
    {
        sessionRecorder.SetDuckHunterData(
            DucksHit,
            DucksMissed
        );
    }
}
