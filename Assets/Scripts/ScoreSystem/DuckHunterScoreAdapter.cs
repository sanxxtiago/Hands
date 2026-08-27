using System.Collections.Generic;
using UnityEngine;

public sealed class DuckHunterScoreAdapter : MonoBehaviour
{
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private DuckSequenceRunner sequenceRunner;
    [SerializeField] private bool enableDebugLogs = true;

    private readonly Dictionary<int, DuckScoreData> ducks = new Dictionary<int, DuckScoreData>();
    private readonly List<int> duckOrder = new List<int>();
    private readonly HashSet<int> closedDucks = new HashSet<int>();
    private bool exerciseActive;
    private bool exerciseCompleted;
    private int expectedDuckCount;
    private int ducksHit;
    private int ducksMissed;
    private float totalReactionTime;

    public bool IsExerciseActive => exerciseActive;

    private void Awake()
    {
        if (scoreManager == null)
            scoreManager = GetComponent<ScoreManager>();
    }

    private void OnEnable()
    {
        if (sequenceRunner == null)
            sequenceRunner = GetComponent<DuckSequenceRunner>();

        if (sequenceRunner == null)
        {
            LogWarning("No hay un DuckSequenceRunner asignado.");
            return;
        }

        sequenceRunner.OnSequenceStarted += OnSequenceStarted;
        sequenceRunner.OnDuckSpawned += RegisterDuckSpawned;
        sequenceRunner.OnDuckHit += RegisterDuckHit;
        sequenceRunner.OnDuckMissed += RegisterDuckMissed;
    }

    private void OnDisable()
    {
        if (sequenceRunner != null)
        {
            sequenceRunner.OnSequenceStarted -= OnSequenceStarted;
            sequenceRunner.OnDuckSpawned -= RegisterDuckSpawned;
            sequenceRunner.OnDuckHit -= RegisterDuckHit;
            sequenceRunner.OnDuckMissed -= RegisterDuckMissed;
        }

        Reset();
    }

    public void BeginExercise()
    {
        if (scoreManager == null)
            scoreManager = GetComponent<ScoreManager>();

        if (scoreManager == null)
        {
            LogWarning("No hay un ScoreManager asignado.");
            return;
        }

        ResetState();
        exerciseActive = true;
        Log("Inicio de sesion de score de DuckHunter.");
    }

    public DuckHunterScoreData BuildData(float exerciseDuration)
    {
        DuckScoreData[] duckData = new DuckScoreData[duckOrder.Count];
        for (int i = 0; i < duckOrder.Count; i++)
            duckData[i] = ducks[duckOrder[i]];

        return new DuckHunterScoreData
        {
            exerciseDuration = exerciseDuration,
            totalDucks = ducksHit + ducksMissed,
            ducksHit = ducksHit,
            ducksMissed = ducksMissed,
            totalReactionTime = totalReactionTime,
            ducks = duckData
        };
    }

    public ExerciseScore CompleteExercise(float exerciseDuration)
    {
        if (exerciseCompleted || !exerciseActive)
        {
            LogWarning("La finalizacion se ignoro porque no hay una sesion activa.");
            return null;
        }

        exerciseCompleted = true;
        exerciseActive = false;
        DuckHunterScoreData data = BuildData(exerciseDuration);
        if (expectedDuckCount != data.totalDucks)
            LogWarning($"La secuencia esperaba {expectedDuckCount} patos, pero se procesaron {data.totalDucks}.");
        Log(
            $"Datos finales: totalDucks={data.totalDucks}, ducksHit={data.ducksHit}, " +
            $"ducksMissed={data.ducksMissed}, totalReactionTime={data.totalReactionTime:F2}s.");

        ExerciseScore score = scoreManager.CompleteDuckHunter(data);
        if (score == null)
        {
            LogWarning("El score final es nulo.");
            return null;
        }

        Log(
            $"Score calculado: totalScore={score.totalScore:F2}, scoreGrade={score.scoreGrade}, " +
            $"isValid={score.isValid}, motivationalMessage={score.motivationalMessage}");
        for (int i = 0; i < score.breakdown.Length; i++)
        {
            ScoreBreakdown item = score.breakdown[i];
            Log($"Breakdown {item.metricId}: raw={item.rawValue:F2}, score={item.metricScore:F2}, weight={item.weight:F2}.");
        }

        return score;
    }

    public void Reset()
    {
        if (scoreManager != null && scoreManager.IsSessionActive)
            scoreManager.Reset();

        ResetState();
    }

    private void ResetState()
    {
        ducks.Clear();
        duckOrder.Clear();
        closedDucks.Clear();
        expectedDuckCount = 0;
        ducksHit = 0;
        ducksMissed = 0;
        totalReactionTime = 0f;
        exerciseActive = false;
        exerciseCompleted = false;
    }

    private void OnSequenceStarted(int totalDucks)
    {
        expectedDuckCount = Mathf.Max(0, totalDucks);
    }

    private void RegisterDuckSpawned(DuckScoreContext context)
    {
        if (!exerciseActive || ducks.ContainsKey(context.duckIndex))
        {
            LogWarning($"Se ignoro un pato generado duplicado: {context.duckIndex}.");
            return;
        }

        ducks.Add(context.duckIndex, new DuckScoreData
        {
            duckIndex = context.duckIndex,
            availableTime = SanitizeTime(context.availableTime)
        });
        duckOrder.Add(context.duckIndex);
        Log(
            $"Pato generado: indice={context.duckIndex}, mano requerida={context.requiredHand}, " +
            $"tiempo de aparicion={context.spawnTime:F2}, tiempo disponible={context.availableTime:F2}s.");
    }

    private void RegisterDuckHit(DuckScoreContext context)
    {
        if (!TryGetOpenDuck(context.duckIndex, out DuckScoreData duck))
            return;

        float reactionTime = SanitizeTime(context.reactionTime);
        duck.reactionTime = reactionTime;
        duck.wasHit = true;
        duck.wasMissed = false;
        ducks[context.duckIndex] = duck;
        closedDucks.Add(context.duckIndex);
        ducksHit++;
        totalReactionTime += reactionTime;
        Log(
            $"Pato cazado: indice={context.duckIndex}, tiempo de reaccion={reactionTime:F2}s, " +
            $"mano utilizada={context.hitHand}.");
    }

    private void RegisterDuckMissed(DuckScoreContext context)
    {
        if (!TryGetOpenDuck(context.duckIndex, out DuckScoreData duck))
            return;

        duck.availableTime = SanitizeTime(context.availableTime);
        duck.wasHit = false;
        duck.wasMissed = true;
        ducks[context.duckIndex] = duck;
        closedDucks.Add(context.duckIndex);
        ducksMissed++;
        Log(
            $"Pato no cazado: indice={context.duckIndex}, tiempo disponible={duck.availableTime:F2}s, " +
            "motivo=el pato llego a su destino.");
    }

    private bool TryGetOpenDuck(int duckIndex, out DuckScoreData duck)
    {
        duck = default(DuckScoreData);
        if (!exerciseActive || !ducks.TryGetValue(duckIndex, out duck))
            return false;

        return !closedDucks.Contains(duckIndex);
    }

    private static float SanitizeTime(float value)
    {
        return ScoreMath.IsFinite(value) ? Mathf.Max(0f, value) : 0f;
    }

    private void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log("[ScoreSystem][DuckHunter] " + message);
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning("[ScoreSystem][DuckHunter] " + message);
    }
}
