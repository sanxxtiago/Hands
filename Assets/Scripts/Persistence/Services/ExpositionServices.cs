using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ExpositionServices
{
    private string userId;
    private ExpositionsData expositionsData = new();

    public ExpositionServices()
    {
    }

    public ExpositionServices(string userId)
    {
        SetUserContext(userId);
    }

    public bool IsReady => !string.IsNullOrWhiteSpace(userId);

    public IReadOnlyList<ExpositionSummary> Records => expositionsData.Records;

    public int TotalRecords => expositionsData.Records.Count;

    public void SetUserContext(string userId)
    {
        this.userId = userId;
        expositionsData = new ExpositionsData();
    }

    public void Load()
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            expositionsData = new ExpositionsData();
            return;
        }

        expositionsData = SaveSystem.Load<ExpositionsData>(
            userId,
            SaveFiles.Expositions);

        if (expositionsData == null)
        {
            expositionsData = new ExpositionsData();
            return;
        }

        expositionsData.Records ??= new List<ExpositionSummary>();
    }

    public void Save()
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        SaveSystem.Save(userId, SaveFiles.Expositions, expositionsData);
    }

    public bool Upsert(ExpositionSummary exposition)
    {
        if (!IsReady || !IsValidSummary(exposition))
        {
            Debug.LogWarning(
                "[ExpositionServices] Se rechazo una exposicion invalida.");
            return false;
        }

        int existingIndex = expositionsData.Records.FindIndex(
            existing => existing != null
                && existing.sessionGuid == exposition.sessionGuid
                && existing.exerciseType == exposition.exerciseType);

        if (existingIndex < 0)
        {
            expositionsData.Records.Add(exposition);
            Save();
            return true;
        }

        ExpositionSummary existingSummary = expositionsData.Records[existingIndex];
        return ExerciseResultIdentity.AreEquivalent(existingSummary, exposition);
    }

    public ExpositionSummary GetExposition(
        string sessionGuid,
        ExerciseType exerciseType)
    {
        if (string.IsNullOrWhiteSpace(sessionGuid))
            return null;

        return expositionsData.Records.FirstOrDefault(
            exposition => exposition != null
                && exposition.sessionGuid == sessionGuid
                && exposition.exerciseType == exerciseType);
    }

    public IReadOnlyList<ExpositionSummary> GetExpositionsForSession(
        string sessionGuid)
    {
        if (string.IsNullOrWhiteSpace(sessionGuid))
            return Array.Empty<ExpositionSummary>();

        return expositionsData.Records
            .Where(exposition => exposition != null
                && exposition.sessionGuid == sessionGuid)
            .OrderBy(exposition => exposition.exerciseIndex)
            .ToList();
    }

    public void DeleteAll()
    {
        if (!string.IsNullOrWhiteSpace(userId))
            SaveSystem.Delete(userId, SaveFiles.Expositions);

        expositionsData = new ExpositionsData();
    }

    public static bool IsValidSummary(ExpositionSummary exposition)
    {
        return exposition != null
            && !string.IsNullOrWhiteSpace(exposition.sessionGuid)
            && exposition.sessionId > 0
            && exposition.exerciseIndex >= 0
            && exposition.exerciseIndex < ExerciseResultIdentity.RequiredExerciseCount
            && Enum.IsDefined(typeof(ExerciseType), exposition.exerciseType)
            && IsFiniteNonNegative(exposition.exerciseDuration)
            && IsValidHand(exposition.leftHand, HandType.LEFT)
            && IsValidHand(exposition.rightHand, HandType.RIGHT);
    }

    private static bool IsValidHand(
        HandExpositionSummary hand,
        HandType expectedHandType)
    {
        return hand != null
            && hand.handType == expectedHandType
            && IsValidDimension(hand.wristFlexionExtension)
            && IsValidDimension(hand.wristRadialUlnarDeviation)
            && IsValidDimension(hand.wristPronationSupination);
    }

    private static bool IsValidDimension(ExpositionDimensionSummary dimension)
    {
        return IsFiniteNonNegative(dimension.validObservationSeconds)
            && IsFiniteNonNegative(dimension.maximumSustainedExposureSeconds)
            && IsFiniteNonNegative(dimension.cumulativeExposureSeconds)
            && IsFiniteNonNegative(dimension.sustainedExposureSeconds);
    }

    private static bool IsFiniteNonNegative(float value)
    {
        return !float.IsNaN(value)
            && !float.IsInfinity(value)
            && value >= 0f;
    }
}
