using UnityEngine;

public sealed class ScoreManager : MonoBehaviour
{
    [SerializeField] private InsertScoreConfig insertConfig = new InsertScoreConfig();
    [SerializeField] private OSUScoreConfig osuConfig = new OSUScoreConfig();
    [SerializeField] private DuckHunterScoreConfig duckHunterConfig = new DuckHunterScoreConfig();

    private ScoreCoordinator coordinator;

    public ExerciseScore LastScore { get; private set; }
    public bool IsSessionActive => coordinator != null && coordinator.IsSessionActive;

    private void Awake()
    {
        coordinator = new ScoreCoordinator(
            insertConfig,
            osuConfig,
            duckHunterConfig);
    }

    public void BeginExercise(ScoreExerciseType exerciseType)
    {
        EnsureCoordinator();
        LastScore = null;
        coordinator.BeginExercise(exerciseType);
    }

    public ExerciseScore CompleteInsert(InsertScoreData data)
    {
        EnsureCoordinator();
        LastScore = coordinator.CompleteInsert(data);
        return LastScore;
    }

    public ExerciseScore CompleteOSU(OSUScoreData data)
    {
        EnsureCoordinator();
        LastScore = coordinator.CompleteOSU(data);
        return LastScore;
    }

    public ExerciseScore CompleteDuckHunter(DuckHunterScoreData data)
    {
        EnsureCoordinator();
        LastScore = coordinator.CompleteDuckHunter(data);
        return LastScore;
    }

    public void Reset()
    {
        EnsureCoordinator();
        coordinator.Reset();
        LastScore = null;
    }

    private void EnsureCoordinator()
    {
        if (coordinator != null)
            return;

        coordinator = new ScoreCoordinator(
            insertConfig ?? new InsertScoreConfig(),
            osuConfig ?? new OSUScoreConfig(),
            duckHunterConfig ?? new DuckHunterScoreConfig());
    }
}
