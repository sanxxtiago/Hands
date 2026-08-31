using System;
using System.Collections.Generic;
using UnityEngine;

public enum ScoreGrade
{
    Invalid,
    NeedsPractice,
    Fair,
    Good,
    Excellent
}

[Serializable]
public struct ScoreClassification
{
    public ScoreGrade Grade { get; }
    public TrophyTier TrophyTier { get; }
    public int ProfileVersion { get; }

    public ScoreClassification(
        ScoreGrade grade,
        TrophyTier trophyTier,
        int profileVersion)
    {
        Grade = grade;
        TrophyTier = trophyTier;
        ProfileVersion = profileVersion;
    }

    public static ScoreClassification Invalid =>
        new ScoreClassification(ScoreGrade.Invalid, TrophyTier.None, 0);
}

[Serializable]
public sealed class ScoreClassificationRange
{
    [SerializeField, Range(0f, 100f)] private float minimumScore;
    [SerializeField] private ScoreGrade grade = ScoreGrade.NeedsPractice;
    [SerializeField] private TrophyTier trophyTier = TrophyTier.None;

    public float MinimumScore => minimumScore;
    public ScoreGrade Grade => grade;
    public TrophyTier TrophyTier => trophyTier;

    public ScoreClassificationRange()
    {
    }

    public ScoreClassificationRange(
        float minimumScore,
        ScoreGrade grade,
        TrophyTier trophyTier)
    {
        this.minimumScore = minimumScore;
        this.grade = grade;
        this.trophyTier = trophyTier;
    }
}

[CreateAssetMenu(
    fileName = "ScoreClassificationProfile",
    menuName = "Score System/Score Classification Profile")]
public sealed class ScoreClassificationProfile : ScriptableObject
{
    [SerializeField, Min(1)] private int profileVersion = 1;
    [SerializeField] private List<ScoreClassificationRange> ranges =
        new List<ScoreClassificationRange>();

    public int ProfileVersion => profileVersion;

    public bool TryResolve(
        float score,
        out ScoreClassification classification)
    {
        classification = ScoreClassification.Invalid;

        if (!TryValidate(out string validationError))
        {
            Debug.LogError(
                $"[ScoreSystem] Perfil de clasificacion invalido: {validationError}.",
                this);
            return false;
        }

        if (!ScoreMath.IsFinite(score))
        {
            Debug.LogError(
                "[ScoreSystem] No se puede clasificar un score no finito.",
                this);
            return false;
        }

        float normalizedScore = Mathf.Clamp(score, 0f, 100f);
        ScoreClassificationRange selectedRange = null;

        for (int i = 0; i < ranges.Count; i++)
        {
            ScoreClassificationRange range = ranges[i];
            if (range.MinimumScore > normalizedScore)
                continue;

            if (selectedRange == null
                || range.MinimumScore > selectedRange.MinimumScore)
            {
                selectedRange = range;
            }
        }

        if (selectedRange == null)
        {
            Debug.LogError(
                "[ScoreSystem] El perfil no contiene una banda aplicable al score.",
                this);
            return false;
        }

        classification = new ScoreClassification(
            selectedRange.Grade,
            selectedRange.TrophyTier,
            profileVersion);
        return true;
    }

    public bool TryValidate(out string validationError)
    {
        if (profileVersion <= 0)
        {
            validationError = "profileVersion debe ser mayor que cero";
            return false;
        }

        if (ranges == null || ranges.Count == 0)
        {
            validationError = "debe existir al menos una banda";
            return false;
        }

        HashSet<float> minimumScores = new HashSet<float>();
        bool containsZero = false;

        for (int i = 0; i < ranges.Count; i++)
        {
            ScoreClassificationRange range = ranges[i];
            if (range == null)
            {
                validationError = $"la banda {i} es nula";
                return false;
            }

            if (!ScoreMath.IsFinite(range.MinimumScore)
                || range.MinimumScore < 0f
                || range.MinimumScore > 100f)
            {
                validationError = $"la banda {i} tiene un corte fuera de 0-100";
                return false;
            }

            if (!minimumScores.Add(range.MinimumScore))
            {
                validationError =
                    $"existe mas de una banda con corte {range.MinimumScore}";
                return false;
            }

            if (range.MinimumScore == 0f)
                containsZero = true;

            if (range.Grade == ScoreGrade.Invalid)
            {
                validationError = $"la banda {i} no puede usar el grade Invalid";
                return false;
            }
        }

        if (!containsZero)
        {
            validationError = "debe existir una banda con corte 0";
            return false;
        }

        validationError = null;
        return true;
    }

    private void OnValidate()
    {
        if (!TryValidate(out string validationError))
        {
            Debug.LogError(
                $"[ScoreSystem] Perfil de clasificacion invalido: {validationError}.",
                this);
        }
    }
}
