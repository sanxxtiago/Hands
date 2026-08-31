#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ScoreClassificationProfileTests
{
    private ScoreClassificationProfile profile;

    [SetUp]
    public void SetUp()
    {
        profile = ScriptableObject.CreateInstance<ScoreClassificationProfile>();
        SetPrivateField(profile, "profileVersion", 1);
        SetPrivateField(
            profile,
            "ranges",
            new List<ScoreClassificationRange>
            {
                new ScoreClassificationRange(0f, ScoreGrade.NeedsPractice, TrophyTier.None),
                new ScoreClassificationRange(60f, ScoreGrade.Fair, TrophyTier.Bronze),
                new ScoreClassificationRange(75f, ScoreGrade.Good, TrophyTier.Silver),
                new ScoreClassificationRange(90f, ScoreGrade.Excellent, TrophyTier.Gold)
            });
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(profile);
    }

    [TestCase(0f, ScoreGrade.NeedsPractice, TrophyTier.None)]
    [TestCase(59.99f, ScoreGrade.NeedsPractice, TrophyTier.None)]
    [TestCase(60f, ScoreGrade.Fair, TrophyTier.Bronze)]
    [TestCase(74.99f, ScoreGrade.Fair, TrophyTier.Bronze)]
    [TestCase(75f, ScoreGrade.Good, TrophyTier.Silver)]
    [TestCase(89.99f, ScoreGrade.Good, TrophyTier.Silver)]
    [TestCase(90f, ScoreGrade.Excellent, TrophyTier.Gold)]
    [TestCase(100f, ScoreGrade.Excellent, TrophyTier.Gold)]
    public void Resolve_ReturnsConfiguredPair(
        float score,
        ScoreGrade expectedGrade,
        TrophyTier expectedTier)
    {
        bool resolved = profile.TryResolve(score, out ScoreClassification classification);

        Assert.That(resolved, Is.True);
        Assert.That(classification.Grade, Is.EqualTo(expectedGrade));
        Assert.That(classification.TrophyTier, Is.EqualTo(expectedTier));
        Assert.That(classification.ProfileVersion, Is.EqualTo(1));
    }

    [Test]
    public void Resolve_EightyFive_ReturnsGoodSilver()
    {
        Assert.That(profile.TryResolve(85f, out ScoreClassification classification), Is.True);
        Assert.That(classification.Grade, Is.EqualTo(ScoreGrade.Good));
        Assert.That(classification.TrophyTier, Is.EqualTo(TrophyTier.Silver));
    }

    [Test]
    public void Resolve_NonFiniteScoreFails()
    {
        LogAssert.Expect(
            LogType.Error,
            "[ScoreSystem] No se puede clasificar un score no finito.");

        Assert.That(profile.TryResolve(float.NaN, out ScoreClassification classification), Is.False);
        Assert.That(classification.Grade, Is.EqualTo(ScoreGrade.Invalid));
        Assert.That(classification.TrophyTier, Is.EqualTo(TrophyTier.None));
    }

    [Test]
    public void Validate_RequiresZeroCut()
    {
        SetPrivateField(
            profile,
            "ranges",
            new List<ScoreClassificationRange>
            {
                new ScoreClassificationRange(60f, ScoreGrade.Fair, TrophyTier.Bronze)
            });

        Assert.That(profile.TryValidate(out _), Is.False);
    }

    private static void SetPrivateField<T>(
        ScoreClassificationProfile target,
        string fieldName,
        T value)
    {
        FieldInfo field = typeof(ScoreClassificationProfile).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }
}
#endif
