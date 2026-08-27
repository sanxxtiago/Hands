using System.Collections.Generic;
using UnityEngine;

public sealed class OSUScoreAdapter : MonoBehaviour
{
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private OSUSequenceRunner sequenceRunner;
    [SerializeField] private bool enableDebugLogs = true;

    private readonly Dictionary<int, OSUTargetScoreData> targets =
        new Dictionary<int, OSUTargetScoreData>();
    private readonly List<int> targetOrder = new List<int>();
    private readonly HashSet<int> closedTargets = new HashSet<int>();

    private int expectedTargetCount;
    private float totalReactionTime;
    private float totalTimeOutsidePath;
    private int completedTargets;
    private int missedTargets;
    private bool exerciseActive;
    private bool exerciseCompleted;

    public bool IsExerciseActive => exerciseActive;

    private void Awake()
    {
        if (scoreManager == null)
            scoreManager = GetComponent<ScoreManager>();
    }

    private void OnEnable()
    {
        if (sequenceRunner == null)
            sequenceRunner = GetComponent<OSUSequenceRunner>();

        if (sequenceRunner == null)
        {
            LogWarning("No hay un OSUSequenceRunner asignado.");
            return;
        }

        sequenceRunner.OnSequenceStarted += OnSequenceStarted;
        sequenceRunner.OnTargetSpawned += OnTargetSpawned;
        sequenceRunner.OnTargetTouched += OnTargetTouched;
        sequenceRunner.OnTargetCompleted += OnTargetCompleted;
        sequenceRunner.OnTargetMissed += OnTargetMissed;
        sequenceRunner.OnTargetFailed += OnTargetFailed;
        sequenceRunner.OnTargetTrackingStateChanged += OnTargetTrackingStateChanged;
    }

    private void OnDisable()
    {
        if (sequenceRunner != null)
        {
            sequenceRunner.OnSequenceStarted -= OnSequenceStarted;
            sequenceRunner.OnTargetSpawned -= OnTargetSpawned;
            sequenceRunner.OnTargetTouched -= OnTargetTouched;
            sequenceRunner.OnTargetCompleted -= OnTargetCompleted;
            sequenceRunner.OnTargetMissed -= OnTargetMissed;
            sequenceRunner.OnTargetFailed -= OnTargetFailed;
            sequenceRunner.OnTargetTrackingStateChanged -= OnTargetTrackingStateChanged;
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
        scoreManager.BeginExercise(ScoreExerciseType.OSU);
        Log("Inicio de sesion OSU.");
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

        OSUTargetScoreData[] targetData = new OSUTargetScoreData[targetOrder.Count];
        for (int i = 0; i < targetOrder.Count; i++)
            targetData[i] = targets[targetOrder[i]];

        OSUScoreData data = new OSUScoreData
        {
            exerciseDuration = exerciseDuration,
            totalTargets = expectedTargetCount,
            completedTargets = completedTargets,
            missedTargets = missedTargets,
            totalReactionTime = totalReactionTime,
            totalTimeOutsidePath = totalTimeOutsidePath,
            targets = targetData
        };

        Log(
            $"Datos finales: totalTargets={data.totalTargets}, " +
            $"completedTargets={data.completedTargets}, missedTargets={data.missedTargets}, " +
            $"totalReactionTime={data.totalReactionTime:F2}s, " +
            $"totalTimeOutsidePath={data.totalTimeOutsidePath:F2}s.");

        ExerciseScore score = scoreManager.CompleteOSU(data);
        if (score == null)
        {
            LogWarning("El score final es nulo.");
            return null;
        }

        Log(
            $"Score calculado: totalScore={score.totalScore:F2}, " +
            $"scoreGrade={score.scoreGrade}, isValid={score.isValid}.");

        for (int i = 0; i < score.breakdown.Length; i++)
        {
            ScoreBreakdown item = score.breakdown[i];
            Log(
                $"Breakdown {item.metricId}: raw={item.rawValue:F2}, " +
                $"score={item.metricScore:F2}, weight={item.weight:F2}.");
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

        targets.Clear();
        targetOrder.Clear();
        closedTargets.Clear();
        expectedTargetCount = 0;
        totalReactionTime = 0f;
        totalTimeOutsidePath = 0f;
        completedTargets = 0;
        missedTargets = 0;
        exerciseActive = false;
        exerciseCompleted = false;
    }

    private void OnSequenceStarted(int totalTargets)
    {
        expectedTargetCount = Mathf.Max(0, totalTargets);
    }

    private void OnTargetSpawned(OSUTargetScoreContext context)
    {
        if (!exerciseActive || targets.ContainsKey(context.targetIndex))
        {
            LogWarning($"Se ignoro un objetivo generado duplicado: {context.targetIndex}.");
            return;
        }

        targets.Add(context.targetIndex, new OSUTargetScoreData
        {
            targetIndex = context.targetIndex,
            hadPath = context.hasPath
        });
        targetOrder.Add(context.targetIndex);
        Log($"Objetivo generado: indice={context.targetIndex}, tiene trayectoria={context.hasPath}.");
    }

    private void OnTargetTouched(OSUTargetScoreContext context)
    {
        if (!TryGetOpenTarget(context.targetIndex, out OSUTargetScoreData target))
            return;

        if (target.wasTouched)
            return;

        float reactionTime = Mathf.Max(0f, context.reactionTime);
        if (!ScoreMath.IsFinite(reactionTime))
            reactionTime = 0f;

        target.reactionTime = reactionTime;
        target.wasTouched = true;
        targets[context.targetIndex] = target;
        totalReactionTime += reactionTime;
        Log($"Objetivo tocado: indice={context.targetIndex}, tiempo de reaccion={reactionTime:F2}s.");
    }

    private void OnTargetTrackingStateChanged(OSUTargetScoreContext context)
    {
        if (!exerciseActive || !targets.ContainsKey(context.targetIndex))
            return;

        Log(
            $"Trayectoria: indice={context.targetIndex}, " +
            $"estado={(context.isFollowing ? "recuperada" : "perdida")}, " +
            $"tiempo fuera acumulado={context.timeOutsidePath:F2}s.");
    }

    private void OnTargetCompleted(OSUTargetScoreContext context)
    {
        CloseTarget(context, false, true, "completado");
    }

    private void OnTargetMissed(OSUTargetScoreContext context)
    {
        CloseTarget(context, true, false, "sin interaccion");
    }

    private void OnTargetFailed(OSUTargetScoreContext context)
    {
        CloseTarget(context, true, false, context.failureReason);
    }

    private void CloseTarget(
        OSUTargetScoreContext context,
        bool wasMissed,
        bool wasCompleted,
        string reason)
    {
        if (!TryGetOpenTarget(context.targetIndex, out OSUTargetScoreData target))
            return;

        closedTargets.Add(context.targetIndex);
        target.wasMissed = wasMissed;
        target.wasCompleted = wasCompleted;
        target.timeOutsidePath = Mathf.Max(0f, context.timeOutsidePath);
        if (!ScoreMath.IsFinite(target.timeOutsidePath))
            target.timeOutsidePath = 0f;

        targets[context.targetIndex] = target;
        if (wasCompleted)
            completedTargets++;
        if (wasMissed)
            missedTargets++;
        if (target.hadPath)
            totalTimeOutsidePath += target.timeOutsidePath;

        Log(
            $"Objetivo {(wasCompleted ? "completado" : "fallido")}: " +
            $"indice={context.targetIndex}, tiempo fuera={target.timeOutsidePath:F2}s" +
            (wasMissed ? $", motivo={reason}." : "."));
    }

    private bool TryGetOpenTarget(int targetIndex, out OSUTargetScoreData target)
    {
        target = default(OSUTargetScoreData);
        if (!exerciseActive || !targets.TryGetValue(targetIndex, out target))
            return false;

        return !closedTargets.Contains(targetIndex);
    }

    private void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log("[ScoreSystem][OSU] " + message);
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning("[ScoreSystem][OSU] " + message);
    }
}
