using UnityEngine;

public class HunterExercise : ExerciseController
{
    [SerializeField] private DuckSequenceRunner sequenceRunner;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private DuckHunterScoreAdapter scoreAdapter;
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
        scoreManager?.BeginExercise(ScoreExerciseType.DuckHunter);
        scoreAdapter?.BeginExercise();
        sequenceRunner.StartSequence(sequence, this);
    }

    private void HandleDuckHit(DuckScoreContext context)
    {
        // métricas

        progressManager.AddCompletedStep();
    }

    private void HandleDuckMissed(DuckScoreContext context)
    {
        progressManager.AddMissedStep();

    }

    private void HandleSequenceCompleted()
    {
        //OnExerciseEnd();
    }

    protected override ExerciseScore SetSpecificData()
    {
        ExerciseScore score = scoreAdapter?.CompleteExercise(elapsedTime);
        sessionRecorder.SetDuckHunterData(
            DucksHit,
            DucksMissed
        );
        return score;
    }
}
