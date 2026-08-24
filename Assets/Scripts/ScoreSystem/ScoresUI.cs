using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ScoresUI : MonoBehaviour
{
    [SerializeField] private TMP_Text totalScoreText;
    [SerializeField] private TMP_Text scoreGradeText;
    [SerializeField] private TMP_Text motivationalMessageText;
    [SerializeField] private TMP_Text exerciseTypeText;
    [SerializeField] private TMP_Text breakdownText;
    [SerializeField] private Slider scoreSlider;
    [SerializeField] private Transform breakdownContainer;

    private void OnEnable()
    {
        ScoreEventBus.OnScoreCompleted += HandleScoreCompleted;
    }

    private void OnDisable()
    {
        ScoreEventBus.OnScoreCompleted -= HandleScoreCompleted;
    }

    private void HandleScoreCompleted(ExerciseScore score)
    {
        if (score == null || !score.isValid)
            return;

        SetText(totalScoreText, ScoreDisplayFormatter.FormatTotalScore(score.totalScore), "totalScoreText");
        SetText(scoreGradeText, score.scoreGrade, "scoreGradeText");
        SetText(motivationalMessageText, score.motivationalMessage, "motivationalMessageText");
        SetText(
            exerciseTypeText,
            ScoreDisplayFormatter.FormatExerciseType(score.exerciseType),
            "exerciseTypeText");
        SetText(
            breakdownText,
            ScoreDisplayFormatter.FormatBreakdown(score.breakdown),
            "breakdownText");

        if (scoreSlider != null)
        {
            scoreSlider.value = Mathf.Clamp(score.totalScore, 0f, 100f);
        }
        else
        {
            Debug.LogWarning("[ScoreSystem][ScoresUI] Falta la referencia opcional scoreSlider.");
        }

        if (breakdownContainer == null)
        {
            Debug.LogWarning("[ScoreSystem][ScoresUI] Falta la referencia opcional breakdownContainer.");
        }
    }

    private static void SetText(TMP_Text text, string value, string referenceName)
    {
        if (text == null)
        {
            Debug.LogWarning(
                "[ScoreSystem][ScoresUI] Falta la referencia opcional " + referenceName + ".");
            return;
        }

        text.text = value ?? string.Empty;
    }
}
