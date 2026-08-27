using System;

public sealed class InsertScoreCalculator : IScoreCalculator<InsertScoreData>
{
    private readonly InsertScoreConfig config;

    public InsertScoreConfig Config => config;

    public InsertScoreCalculator(InsertScoreConfig config = null)
    {
        this.config = config ?? new InsertScoreConfig();
    }

    public ExerciseScore Calculate(InsertScoreData input)
    {
        if (input == null)
            return ScoreResultFactory.Invalid(ScoreExerciseType.Insert, "Los datos de Insert son nulos.");

        if (input.completionTime < 0f || !ScoreMath.IsFinite(input.completionTime))
            return ScoreResultFactory.Invalid(ScoreExerciseType.Insert, "El tiempo de finalizacion de Insert no es valido.");

        if (input.totalPieces <= 0)
            return ScoreResultFactory.Invalid(ScoreExerciseType.Insert, "Insert debe contener al menos una pieza.");

        if (input.completedPieces < 0)
            return ScoreResultFactory.Invalid(ScoreExerciseType.Insert, "El numero de piezas completadas no puede ser negativo.");

        if (input.phaseCount < 0)
            return ScoreResultFactory.Invalid(ScoreExerciseType.Insert, "El numero de fases de Insert no puede ser negativo.");

        ValidatePhaseTimes(input);

        int completedPieces = input.completedPieces;
        if (completedPieces > input.totalPieces)
        {
            ScoreSystemLog.Warning("Las piezas completadas superan el total; se limitaran al total declarado.");
            completedPieces = input.totalPieces;
        }

        float expectedTime = CalculateExpectedTime(input);
        if (expectedTime <= 0f || !ScoreMath.IsFinite(expectedTime))
            return ScoreResultFactory.Invalid(ScoreExerciseType.Insert, "El tiempo esperado de Insert es cero o no es valido.");

        float timeRatio = input.completionTime / expectedTime;
        float timeScore = ScoreMath.NormalizeLowerIsBetter(
            timeRatio,
            config.excellentRatio,
            config.maximumRatio);

        float completionRatio = (float)completedPieces / input.totalPieces;
        float completionScore = completionRatio * 100f;

        if (completedPieces < input.totalPieces)
            timeScore *= completionRatio;

        ScoreBreakdown[] breakdown =
        {
            new ScoreBreakdown(
                "completion_time",
                input.completionTime,
                timeScore,
                config.timeWeight),
            new ScoreBreakdown(
                "completed_pieces",
                completedPieces,
                completionScore,
                config.completionWeight)
        };

        ScoreStatsData statsData = new ScoreStatsData
        {
            exerciseDuration = input.completionTime,
            hits = input.totalPieces,
            misses = 0
        };

        return ScoreResultFactory.Create(ScoreExerciseType.Insert, breakdown, statsData, true);
    }

    public float CalculateExpectedTime(InsertScoreData input)
    {
        if (input == null || input.totalPieces <= 0)
            return 0f;

        float baseTime = ScoreMath.NonNegativeFinite(config.baseTime);
        float timePerPiece = ScoreMath.NonNegativeFinite(config.timePerPiece);
        float rotationExtraTime = ScoreMath.NonNegativeFinite(config.rotationExtraTime);
        int phaseCount = Math.Max(0, input.phaseCount);

        double expectedTime = baseTime
            + (double)input.totalPieces * timePerPiece
            + (double)phaseCount * rotationExtraTime;

        if (double.IsNaN(expectedTime) || double.IsInfinity(expectedTime))
            return 0f;

        if (expectedTime > float.MaxValue)
            return float.MaxValue;

        return (float)expectedTime;
    }

    private static void ValidatePhaseTimes(InsertScoreData input)
    {
        if (input.phaseTimes == null)
            return;

        if (input.phaseTimes.Length != input.phaseCount)
        {
            ScoreSystemLog.Warning("La cantidad de tiempos de fase no coincide con phaseCount.");
        }

        for (int i = 0; i < input.phaseTimes.Length; i++)
        {
            if (input.phaseTimes[i] < 0f || !ScoreMath.IsFinite(input.phaseTimes[i]))
                ScoreSystemLog.Warning("Se recibio un tiempo de fase invalido; no se usara para el score.");
        }
    }
}
