using System.Collections.Generic;
using UnityEngine;

public sealed class InsertScoreAdapter : MonoBehaviour
{
    [SerializeField] private ScoreManager scoreManager;
    private readonly HashSet<PieceBehaviour> completedPieces = new HashSet<PieceBehaviour>();
    private readonly List<float> phaseTimes = new List<float>();
    private readonly List<InsertPieceResult> pieceResults = new List<InsertPieceResult>();
    private int totalPieces;
    private int phaseCount;
    private int completedPieceCount;
    private int activePhaseIndex = -1;
    private float phaseStartTime;
    private float lastPlacementTime;
    private bool phaseActive;
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
        PieceBehaviour.OnPieceSnapped += OnPieceSnapped;
    }

    private void OnDisable()
    {
        PieceBehaviour.OnPieceSnapped -= OnPieceSnapped;
        Reset();
    }

    public void BeginExercise(int totalPieces, int phaseCount)
    {
        if (scoreManager == null)
            scoreManager = GetComponent<ScoreManager>();

        if (scoreManager == null)
        {
            ScoreSystemLog.Warning("[InsertScoreAdapter] No hay un ScoreManager asignado.");
            return;
        }

        if (exerciseActive)
        {
            ScoreSystemLog.Warning("[InsertScoreAdapter] Se reinicio una sesion de ejercicio activa.");
            Reset();
        }

        this.totalPieces = Mathf.Max(0, totalPieces);
        this.phaseCount = Mathf.Max(0, phaseCount);
        completedPieceCount = 0;
        activePhaseIndex = -1;
        phaseTimes.Clear();
        completedPieces.Clear();
        pieceResults.Clear();
        exerciseCompleted = false;
        exerciseActive = true;
        scoreManager.BeginExercise(ScoreExerciseType.Insert);

        if (this.totalPieces <= 0)
            ScoreSystemLog.Warning("[InsertScoreAdapter] El ejercicio no tiene piezas declaradas.");
    }

    public void BeginPhase(int phaseIndex)
    {
        if (!exerciseActive || exerciseCompleted)
        {
            ScoreSystemLog.Warning("[InsertScoreAdapter] Se ignoro el inicio de una fase sin ejercicio activo.");
            return;
        }

        if (phaseActive)
            EndPhase();

        if (phaseIndex < 0 || phaseIndex >= phaseCount)
        {
            ScoreSystemLog.Warning("[InsertScoreAdapter] El indice de fase no es valido.");
            activePhaseIndex = -1;
            return;
        }

        activePhaseIndex = phaseIndex;
        phaseStartTime = Time.time;
        lastPlacementTime = phaseStartTime;
        phaseActive = true;
    }

    public void EndPhase()
    {
        if (!phaseActive)
            return;

        float duration = Mathf.Max(0f, Time.time - phaseStartTime);
        if (!ScoreMath.IsFinite(duration))
        {
            ScoreSystemLog.Warning("[InsertScoreAdapter] Se descarto un tiempo de fase invalido.");
            duration = 0f;
        }

        phaseTimes.Add(duration);
        Debug.Log(
            $"[ScoreSystem][InsertScoreAdapter] Fase {activePhaseIndex}: " +
            $"tiempo={duration:F2}s, piezas completadas={completedPieceCount}/{totalPieces}.");
        phaseActive = false;
        activePhaseIndex = -1;
    }

    public ExerciseScore CompleteExercise(float completionTime)
    {
        if (exerciseCompleted || !exerciseActive)
        {
            ScoreSystemLog.Warning("[InsertScoreAdapter] La finalizacion se ignoro porque no hay una sesion activa.");
            return null;
        }

        EndPhase();
        exerciseCompleted = true;
        exerciseActive = false;

        InsertScoreData data = new InsertScoreData
        {
            completionTime = ScoreMath.IsFinite(completionTime) ? Mathf.Max(0f, completionTime) : 0f,
            totalPieces = totalPieces,
            completedPieces = completedPieceCount,
            phaseCount = phaseCount,
            phaseTimes = phaseTimes.ToArray()
        };

        if (!ScoreMath.IsFinite(completionTime))
            ScoreSystemLog.Warning("[InsertScoreAdapter] El tiempo total no es valido.");

        if (scoreManager == null)
        {
            ScoreSystemLog.Warning("[InsertScoreAdapter] No hay un ScoreManager asignado.");
            return null;
        }

        ExerciseScore score = scoreManager.CompleteInsert(data);
        if (score == null)
        {
            Debug.LogWarning("[ScoreSystem][InsertScoreAdapter] El score final es nulo.");
            return null;
        }

        Debug.Log(
            $"[ScoreSystem][InsertScoreAdapter] Ejercicio finalizado: " +
            $"score={score.totalScore:F2}, grado={score.scoreGrade}, " +
            $"valido={score.isValid}, piezas={data.completedPieces}/{data.totalPieces}, " +
            $"tiempo={data.completionTime:F2}s.");

        for (int i = 0; i < score.breakdown.Length; i++)
        {
            ScoreBreakdown item = score.breakdown[i];
            Debug.Log(
                $"[ScoreSystem][InsertScoreAdapter] Breakdown {item.metricId}: " +
                $"raw={item.rawValue:F2}, score={item.metricScore:F2}, peso={item.weight:F2}.");
        }

        return score;
    }

    public void Reset()
    {
        if (scoreManager != null && scoreManager.IsSessionActive)
            scoreManager.Reset();

        totalPieces = 0;
        phaseCount = 0;
        completedPieceCount = 0;
        activePhaseIndex = -1;
        phaseStartTime = 0f;
        lastPlacementTime = 0f;
        phaseActive = false;
        exerciseActive = false;
        exerciseCompleted = false;
        completedPieces.Clear();
        phaseTimes.Clear();
        pieceResults.Clear();
    }

    private void OnPieceSnapped(PieceBehaviour piece)
    {
        if (!exerciseActive || exerciseCompleted || !phaseActive || piece == null)
            return;

        if (piece.ScorePhaseIndex != activePhaseIndex)
            return;

        if (!completedPieces.Add(piece))
        {
            ScoreSystemLog.Warning("[InsertScoreAdapter] Se ignoro una pieza duplicada.");
            return;
        }

        float now = Time.time;
        float placementTime = Mathf.Max(0f, now - lastPlacementTime);
        if (!ScoreMath.IsFinite(placementTime))
            placementTime = 0f;

        lastPlacementTime = now;
        completedPieceCount++;
        pieceResults.Add(new InsertPieceResult
        {
            placementTime = placementTime,
            wasPlaced = true
        });
    }

}
