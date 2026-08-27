using System;

public sealed class DuckHunterScoreCalculator : IScoreCalculator<DuckHunterScoreData>
{
    private readonly DuckHunterScoreConfig config;

    public DuckHunterScoreConfig Config => config;

    public DuckHunterScoreCalculator(DuckHunterScoreConfig config = null)
    {
        this.config = config ?? new DuckHunterScoreConfig();
    }

    public ExerciseScore Calculate(DuckHunterScoreData input)
    {
        if (input == null)
            return ScoreResultFactory.Invalid(ScoreExerciseType.DuckHunter, "Los datos de DuckHunter son nulos.");

        DuckAggregates aggregates = ResolveAggregates(input);

        if (aggregates.totalDucks <= 0)
            return ScoreResultFactory.Invalid(ScoreExerciseType.DuckHunter, "DuckHunter debe contener al menos un pato.");

        if (aggregates.ducksHit < 0 || aggregates.ducksMissed < 0)
            return ScoreResultFactory.Invalid(ScoreExerciseType.DuckHunter, "Los contadores de DuckHunter no pueden ser negativos.");

        if (aggregates.totalReactionTime < 0f || !ScoreMath.IsFinite(aggregates.totalReactionTime))
            return ScoreResultFactory.Invalid(ScoreExerciseType.DuckHunter, "El tiempo de reaccion de DuckHunter no es valido.");

        NormalizeDuckCounts(aggregates.totalDucks, ref aggregates.ducksHit, ref aggregates.ducksMissed);
        ValidateDuckCount(input, aggregates.totalDucks);

        float expectedReactionTime = config.expectedReactionTime * aggregates.totalDucks;
        if (expectedReactionTime <= 0f || !ScoreMath.IsFinite(expectedReactionTime))
            return ScoreResultFactory.Invalid(ScoreExerciseType.DuckHunter, "El tiempo de reaccion esperado de DuckHunter es cero o no es valido.");

        float penalty = ScoreMath.NonNegativeFinite(config.missedDuckPenalty);
        float effectiveReactionTime = aggregates.totalReactionTime
            + aggregates.ducksMissed * penalty;
        float reactionScore = ScoreMath.NormalizeLowerIsBetter(
            effectiveReactionTime / expectedReactionTime,
            config.excellentRatio,
            config.maximumRatio);
        float accuracy = (float)aggregates.ducksHit / aggregates.totalDucks;
        float accuracyScore = accuracy * 100f;

        ScoreBreakdown[] breakdown =
        {
            new ScoreBreakdown(
                "reaction",
                aggregates.totalReactionTime,
                reactionScore,
                config.reactionWeight),
            new ScoreBreakdown(
                "accuracy",
                accuracy,
                accuracyScore,
                config.accuracyWeight)
        };

        ScoreStatsData statsData = new ScoreStatsData
        {
            exerciseDuration = input.exerciseDuration,
            hits = aggregates.ducksHit,
            misses = aggregates.ducksMissed
        };

        return ScoreResultFactory.Create(ScoreExerciseType.DuckHunter, breakdown, statsData, true);
    }

    public float CalculateAccuracy(DuckHunterScoreData input)
    {
        if (input == null)
            return 0f;

        DuckAggregates aggregates = ResolveAggregates(input);
        if (aggregates.totalDucks <= 0)
            return 0f;

        NormalizeDuckCounts(aggregates.totalDucks, ref aggregates.ducksHit, ref aggregates.ducksMissed);
        return (float)aggregates.ducksHit / aggregates.totalDucks;
    }

    private static void NormalizeDuckCounts(
        int totalDucks,
        ref int ducksHit,
        ref int ducksMissed)
    {
        if (ducksMissed > totalDucks)
        {
            ScoreSystemLog.Warning("Los patos fallidos superan el total; se limitaran al total declarado.");
            ducksMissed = totalDucks;
        }

        int maximumHit = totalDucks - ducksMissed;
        if (ducksHit > maximumHit)
        {
            ScoreSystemLog.Warning("Los patos cazados no pueden incluir patos fallidos; se ajustara el conteo.");
            ducksHit = maximumHit;
        }
    }

    private static void ValidateDuckCount(DuckHunterScoreData input, int totalDucks)
    {
        if (input.ducks != null
            && input.ducks.Length > 0
            && input.ducks.Length != totalDucks)
        {
            ScoreSystemLog.Warning("La cantidad de resultados de patos no coincide con totalDucks.");
        }
    }

    private static DuckAggregates ResolveAggregates(DuckHunterScoreData input)
    {
        if (input.totalDucks != 0 || input.ducks == null || input.ducks.Length == 0)
        {
            return new DuckAggregates
            {
                totalDucks = input.totalDucks,
                ducksHit = input.ducksHit,
                ducksMissed = input.ducksMissed,
                totalReactionTime = input.totalReactionTime
            };
        }

        DuckAggregates aggregates = new DuckAggregates
        {
            totalDucks = input.ducks.Length
        };

        for (int i = 0; i < input.ducks.Length; i++)
        {
            DuckScoreData duck = input.ducks[i];
            aggregates.totalReactionTime += duck.reactionTime;

            if (duck.wasHit)
                aggregates.ducksHit++;

            if (duck.wasMissed)
                aggregates.ducksMissed++;
        }

        return aggregates;
    }

    private struct DuckAggregates
    {
        public int totalDucks;
        public int ducksHit;
        public int ducksMissed;
        public float totalReactionTime;
    }
}
