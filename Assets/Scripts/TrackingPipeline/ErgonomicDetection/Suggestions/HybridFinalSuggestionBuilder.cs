using System;

public static class HybridFinalSuggestionBuilder
{
    public static string Build(HybridExerciseProfile profile, ExerciseSummary exercise,
        HandErgonomicExposureSummary left, HandErgonomicExposureSummary right, bool hasPerformance = true)
    {
        if (profile == null || !profile.TryValidate(out string _) || exercise == null ||
            profile.ExerciseType != exercise.exerciseType)
            throw new ArgumentException("Se requiere un perfil válido del mismo ejercicio.");

        Candidate selected = default;
        ConsiderHand(profile, left, HandType.LEFT, ref selected);
        ConsiderHand(profile, right, HandType.RIGHT, ref selected);
        string prevention = string.Empty;
        if (selected.priority > 0)
        {
            bool isLeft = selected.hand == HandType.LEFT;
            string hand = isLeft ? "izquierda" : "derecha";
            string exposure = selected.priority == 2 ? "mantuvo exposición postural sostenida" : "acumuló exposición postural";
            bool twist = selected.dimension == ErgonomicPostureDimension.WristPronationSupination;
            prevention = twist
                ? $"La mano {hand} {exposure} de giro; vuelve gradualmente a una posición intermedia."
                : $"La mano {hand} {exposure} en la muñeca; procura mantenerla más neutra.";

            HandErgonomicExposureSummary ergonomic = isLeft ? left : right;
            HandUsageSummary usage = isLeft ? exercise.leftHand : exercise.rightHand;
            HybridHandUsageGoal goal = isLeft ? profile.LeftHand : profile.RightHand;
            if (!twist && profile.CoordinationEnabled && KnownHand(profile, ergonomic, selected.hand) &&
                ergonomic.wristPronationSupination.cumulativeExposureSeconds == 0f &&
                TryUsage(profile, usage, selected.hand, out float wrist, out float forearm) &&
                wrist > goal.WristTarget + goal.WristTolerance && forearm < goal.ForearmTarget - goal.ForearmTolerance)
                prevention = $"La mano {hand} {exposure} en la muñeca y concentró allí el movimiento; mantén la muñeca más neutra y redistribuye parte del movimiento hacia el antebrazo.";
        }
        else if (profile.CoordinationEnabled && NoRecordedExposure(left) && NoRecordedExposure(right) &&
            (UsageMismatch(profile, exercise.leftHand, left, HandType.LEFT, profile.LeftHand) ||
             UsageMismatch(profile, exercise.rightHand, right, HandType.RIGHT, profile.RightHand)))
        {
            prevention = "El patrón de movimiento se alejó del objetivo configurado para este ejercicio; revisa la coordinación manteniendo una postura cómoda.";
        }

        string performance = hasPerformance ? BuildPerformance(profile, exercise, selected.priority > 0) : string.Empty;
        if (prevention.Length == 0) return performance;
        return performance.Length == 0 ? prevention : prevention + "\n" + performance;
    }

    private static void ConsiderHand(HybridExerciseProfile profile, HandErgonomicExposureSummary hand,
        HandType expected, ref Candidate selected)
    {
        if (hand.handType != expected || hand.calibrationProfileId != profile.CalibrationProfile.GetInstanceID()) return;
        for (int i = 0; i < 3; i++)
        {
            ErgonomicPostureDimension dimension = (ErgonomicPostureDimension)i;
            ErgonomicExposureDimensionSummary data = Dimension(hand, dimension);
            if (!KnownDimension(profile, dimension, data)) continue;
            int priority = data.maximumSustainedExposureSeconds >= profile.CalibrationProfile.SustainedExposureThresholdSeconds ? 2 :
                data.cumulativeExposureSeconds >= profile.CalibrationProfile.CumulativeExposureAlertSeconds ? 1 : 0;
            float duration = priority == 2 ? data.maximumSustainedExposureSeconds : data.cumulativeExposureSeconds;
            // Orden lexicográfico: condición, duración de esa condición, mano y dimensión. No hay score mixto.
            if (priority > selected.priority || (priority > 0 && priority == selected.priority && duration > selected.duration))
                selected = new Candidate { hand = expected, dimension = dimension, priority = priority, duration = duration };
        }
    }

    private static bool UsageMismatch(HybridExerciseProfile profile, HandUsageSummary usage,
        HandErgonomicExposureSummary exposure, HandType hand, HybridHandUsageGoal goal)
    {
        if (!KnownHand(profile, exposure, hand) ||
            exposure.wristFlexionExtension.cumulativeExposureSeconds > 0f ||
            exposure.wristRadialUlnarDeviation.cumulativeExposureSeconds > 0f ||
            exposure.wristPronationSupination.cumulativeExposureSeconds > 0f ||
            !TryUsage(profile, usage, hand, out float wrist, out float forearm)) return false;
        return Math.Abs(wrist - goal.WristTarget) > goal.WristTolerance ||
            Math.Abs(forearm - goal.ForearmTarget) > goal.ForearmTolerance;
    }

    private static bool NoRecordedExposure(HandErgonomicExposureSummary hand) =>
        hand.wristFlexionExtension.cumulativeExposureSeconds == 0f &&
        hand.wristRadialUlnarDeviation.cumulativeExposureSeconds == 0f &&
        hand.wristPronationSupination.cumulativeExposureSeconds == 0f;

    private static bool KnownHand(HybridExerciseProfile profile, HandErgonomicExposureSummary hand, HandType expected)
    {
        if (hand.handType != expected || hand.calibrationProfileId != profile.CalibrationProfile.GetInstanceID()) return false;
        for (int i = 0; i < 3; i++)
        {
            ErgonomicExposureDimensionSummary data = Dimension(hand, (ErgonomicPostureDimension)i);
            if (!KnownDimension(profile, (ErgonomicPostureDimension)i, data) ||
                data.validObservationSeconds < profile.MinimumFinalObservationSeconds) return false;
        }
        return true;
    }

    private static bool KnownDimension(HybridExerciseProfile profile, ErgonomicPostureDimension dimension,
        ErgonomicExposureDimensionSummary data)
    {
        return profile.CalibrationProfile.TryGetCalibration(dimension, out ErgonomicAngleCalibration angle) && angle.IsEnabled &&
            Finite(data.validObservationSeconds) && data.validObservationSeconds > 0f &&
            Finite(data.cumulativeExposureSeconds) && data.cumulativeExposureSeconds >= 0f &&
            data.cumulativeExposureSeconds <= data.validObservationSeconds &&
            Finite(data.maximumSustainedExposureSeconds) && data.maximumSustainedExposureSeconds >= 0f &&
            data.maximumSustainedExposureSeconds <= data.cumulativeExposureSeconds;
    }

    private static bool TryUsage(HybridExerciseProfile profile, HandUsageSummary usage, HandType hand,
        out float wrist, out float forearm)
    {
        wrist = forearm = 0f;
        if (usage.handType != hand || usage.zones == null || usage.relativeUsage == null ||
            usage.zones.Length != 3 || usage.relativeUsage.Length != 3 ||
            !HybridExerciseProfile.Unit(usage.activityRatio) || usage.activityRatio < profile.MinimumActivityRatio ||
            !Finite(usage.totalActiveSeconds) || usage.totalActiveSeconds <= 0f) return false;
        int seen = 0;
        float total = 0f;
        for (int i = 0; i < 3; i++)
        {
            float value = usage.relativeUsage[i];
            if (!HybridExerciseProfile.Unit(value)) return false;
            int bit;
            switch (usage.zones[i])
            {
                case MotionZone.Hand: bit = 1; break;
                case MotionZone.Wrist: bit = 2; wrist = value; break;
                case MotionZone.Forearm: bit = 4; forearm = value; break;
                default: return false;
            }
            if ((seen & bit) != 0) return false;
            seen |= bit;
            total += value;
        }
        return seen == 7 && Math.Abs(total - 1f) <= 0.001f;
    }

    private static string BuildPerformance(HybridExerciseProfile profile, ExerciseSummary exercise, bool posturePriority)
    {
        float value;
        switch (exercise.exerciseType)
        {
            case ExerciseType.Insert: value = exercise.completionTime; break;
            case ExerciseType.OSU:
                if (exercise.interactionCount <= 0) return string.Empty;
                value = exercise.totalInteractionDelay;
                break;
            case ExerciseType.DuckHunter:
                if (exercise.ducksHit < 0 || exercise.ducksMissed < 0) return string.Empty;
                long total = (long)exercise.ducksHit + exercise.ducksMissed;
                if (total == 0) return "No hubo objetivos suficientes para evaluar la precisión.";
                value = (float)exercise.ducksMissed / total;
                break;
            default: return string.Empty;
        }
        if (!Finite(value) || value < 0f) return string.Empty;
        int level = value <= profile.GoodPerformanceThreshold ? 0 : value <= profile.IntermediatePerformanceThreshold ? 1 : 2;
        switch (exercise.exerciseType)
        {
            case ExerciseType.Insert:
                if (level == 0) return "Muy buen ritmo en la inserción de piezas.";
                if (posturePriority) return "Practica una inserción fluida, priorizando una postura cómoda sobre la rapidez.";
                return level == 1 ? "Buen trabajo. Intenta mantener un ritmo más constante." :
                    "Practica movimientos más fluidos para reducir el tiempo de inserción.";
            case ExerciseType.OSU:
                if (level == 0) return "Muy buenos tiempos de reacción en los objetivos.";
                if (posturePriority) return "Practica la anticipación de los objetivos, priorizando una postura cómoda sobre la rapidez.";
                return level == 1 ? "Buen trabajo. Intenta reducir gradualmente el tiempo de interacción." :
                    "Concéntrate en anticipar la posición de los objetivos para reaccionar más rápido.";
            default:
                if (posturePriority && level > 0) return "Practica la precisión de los disparos sin prolongar una postura incómoda.";
                return level == 0 ? "Muy buena precisión al cazar los objetivos." : level == 1 ?
                    "Buen trabajo. Intenta mejorar la precisión de los disparos." :
                    "Mantén la mira sobre el objetivo antes de disparar para reducir los fallos.";
        }
    }

    private static ErgonomicExposureDimensionSummary Dimension(HandErgonomicExposureSummary hand, ErgonomicPostureDimension dimension)
    {
        return dimension == ErgonomicPostureDimension.WristFlexionExtension ? hand.wristFlexionExtension :
            dimension == ErgonomicPostureDimension.WristRadialUlnarDeviation ? hand.wristRadialUlnarDeviation : hand.wristPronationSupination;
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private struct Candidate
    {
        public HandType hand;
        public ErgonomicPostureDimension dimension;
        public int priority;
        public float duration;
    }
}
