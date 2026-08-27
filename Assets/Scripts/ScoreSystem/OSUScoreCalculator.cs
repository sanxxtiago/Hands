using System;

public sealed class OSUScoreCalculator : IScoreCalculator<OSUScoreData>
{
    private readonly OSUScoreConfig config;

    public OSUScoreConfig Config => config;

    public OSUScoreCalculator(OSUScoreConfig config = null)
    {
        this.config = config ?? new OSUScoreConfig();
    }

    public ExerciseScore Calculate(OSUScoreData input)
    {
        if (input == null)
            return ScoreResultFactory.Invalid(ScoreExerciseType.OSU, "Los datos de OSU son nulos.");

        OSUAggregates aggregates = ResolveAggregates(input);

        if (aggregates.totalTargets <= 0)
            return ScoreResultFactory.Invalid(ScoreExerciseType.OSU, "OSU debe contener al menos un objetivo.");

        if (aggregates.completedTargets < 0 || aggregates.missedTargets < 0)
            return ScoreResultFactory.Invalid(ScoreExerciseType.OSU, "Los contadores de objetivos de OSU no pueden ser negativos.");

        if (aggregates.totalReactionTime < 0f || !ScoreMath.IsFinite(aggregates.totalReactionTime))
            return ScoreResultFactory.Invalid(ScoreExerciseType.OSU, "El tiempo de reaccion de OSU no es valido.");

        if (aggregates.totalTimeOutsidePath < 0f || !ScoreMath.IsFinite(aggregates.totalTimeOutsidePath))
            return ScoreResultFactory.Invalid(ScoreExerciseType.OSU, "El tiempo fuera de trayectoria de OSU no es valido.");

        NormalizeTargetCounts(aggregates.totalTargets, ref aggregates.completedTargets, ref aggregates.missedTargets);
        ValidateTargetCount(input, aggregates.totalTargets);

        float expectedReactionTime = config.expectedReactionTime * aggregates.totalTargets;
        float expectedTrackingTime = config.expectedTrackingTime * aggregates.totalTargets;

        if (expectedReactionTime <= 0f || !ScoreMath.IsFinite(expectedReactionTime))
            return ScoreResultFactory.Invalid(ScoreExerciseType.OSU, "El tiempo de reaccion esperado de OSU es cero o no es valido.");

        if (expectedTrackingTime <= 0f || !ScoreMath.IsFinite(expectedTrackingTime))
            return ScoreResultFactory.Invalid(ScoreExerciseType.OSU, "El tiempo esperado de seguimiento de OSU es cero o no es valido.");

        float effectiveTime = CalculateEffectiveTime(
            aggregates.totalReactionTime,
            aggregates.totalTimeOutsidePath,
            aggregates.missedTargets);
        float effectiveReactionTime = effectiveTime - aggregates.totalTimeOutsidePath;
        float reactionScore = ScoreMath.NormalizeLowerIsBetter(
            effectiveReactionTime / expectedReactionTime,
            config.excellentRatio,
            config.maximumRatio);
        float trackingScore = ScoreMath.NormalizeLowerIsBetter(
            aggregates.totalTimeOutsidePath / expectedTrackingTime,
            config.excellentRatio,
            config.maximumRatio);
        float completionScore =
            (float)aggregates.completedTargets / aggregates.totalTargets * 100f;

        ScoreBreakdown[] breakdown =
        {
            new ScoreBreakdown(
                "reaction",
                aggregates.totalReactionTime,
                reactionScore,
                config.reactionWeight),
            new ScoreBreakdown(
                "tracking",
                aggregates.totalTimeOutsidePath,
                trackingScore,
                config.trackingWeight),
            new ScoreBreakdown(
                "completed_targets",
                aggregates.completedTargets,
                completionScore,
                config.completionWeight)
        };

        ScoreStatsData statsData = new ScoreStatsData
        {
            exerciseDuration = input.exerciseDuration,
            hits = aggregates.completedTargets,
            misses = aggregates.missedTargets
        };

        return ScoreResultFactory.Create(ScoreExerciseType.OSU, breakdown, statsData, true);
    }

    public float CalculateEffectiveTime(OSUScoreData input)
    {
        if (input == null)
            return 0f;

        OSUAggregates aggregates = ResolveAggregates(input);
        return CalculateEffectiveTime(
            aggregates.totalReactionTime,
            aggregates.totalTimeOutsidePath,
            Math.Max(0, aggregates.missedTargets));
    }

    private float CalculateEffectiveTime(
        float totalReactionTime,
        float totalTimeOutsidePath,
        int missedTargets)
    {
        float penalty = ScoreMath.NonNegativeFinite(config.missedTargetPenalty);
        double effectiveTime = totalReactionTime
            + totalTimeOutsidePath
            + (double)missedTargets * penalty;

        if (double.IsNaN(effectiveTime) || double.IsInfinity(effectiveTime))
            return float.MaxValue;

        if (effectiveTime > float.MaxValue)
            return float.MaxValue;

        return (float)Math.Max(0d, effectiveTime);
    }

    private static void NormalizeTargetCounts(
        int totalTargets,
        ref int completedTargets,
        ref int missedTargets)
    {
        if (missedTargets > totalTargets)
        {
            ScoreSystemLog.Warning("Los objetivos fallidos superan el total; se limitaran al total declarado.");
            missedTargets = totalTargets;
        }

        int maximumCompleted = totalTargets - missedTargets;
        if (completedTargets > maximumCompleted)
        {
            ScoreSystemLog.Warning("Los objetivos completados no pueden incluir objetivos fallidos; se ajustara el conteo.");
            completedTargets = maximumCompleted;
        }
    }

    private static void ValidateTargetCount(OSUScoreData input, int totalTargets)
    {
        if (input.targets != null
            && input.targets.Length > 0
            && input.targets.Length != totalTargets)
        {
            ScoreSystemLog.Warning("La cantidad de resultados de objetivos no coincide con totalTargets.");
        }
    }

    private static OSUAggregates ResolveAggregates(OSUScoreData input)
    {
        if (input.totalTargets != 0 || input.targets == null || input.targets.Length == 0)
        {
            return new OSUAggregates
            {
                totalTargets = input.totalTargets,
                completedTargets = input.completedTargets,
                missedTargets = input.missedTargets,
                totalReactionTime = input.totalReactionTime,
                totalTimeOutsidePath = input.totalTimeOutsidePath
            };
        }

        OSUAggregates aggregates = new OSUAggregates
        {
            totalTargets = input.targets.Length
        };

        for (int i = 0; i < input.targets.Length; i++)
        {
            OSUTargetScoreData target = input.targets[i];
            aggregates.totalReactionTime += target.reactionTime;
            aggregates.totalTimeOutsidePath += target.timeOutsidePath;

            if (target.wasCompleted)
                aggregates.completedTargets++;

            if (target.wasMissed)
                aggregates.missedTargets++;
        }

        return aggregates;
    }

    private struct OSUAggregates
    {
        public int totalTargets;
        public int completedTargets;
        public int missedTargets;
        public float totalReactionTime;
        public float totalTimeOutsidePath;
    }
}
