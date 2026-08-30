using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionReader : MonoBehaviour
{
    private const int HistoryLength = 7;

    public enum SummaryMode
    {
        Absolute,
        Relative
    }

    [Header("Controls")]
    [SerializeField] private Button insertTab;
    [SerializeField] private Button osuTab;
    [SerializeField] private Button duckHunterTab;
    [SerializeField] private Button absoluteButton;
    [SerializeField] private Button relativeButton;

    [Header("Charts")]
    [SerializeField] private RadarChart leftRadarChart;
    [SerializeField] private RadarChart rightRadarChart;
    [SerializeField] private LineChart leftHandChart;
    [SerializeField] private LineChart leftWristChart;
    [SerializeField] private LineChart leftForearmChart;
    [SerializeField] private LineChart rightHandChart;
    [SerializeField] private LineChart rightWristChart;
    [SerializeField] private LineChart rightForearmChart;

    private SessionSummary session;
    private ExerciseSummary CurrentSummary =>
        HasValidSession
            ? FindExerciseSummary(session, selectedExerciseType)
            : null;
    private bool HasValidSession => IsCompleteSession(session);
    private ExerciseType selectedExerciseType = ExerciseType.Insert;
    private SummaryMode currentMode = SummaryMode.Absolute;
    private bool isInitialized;
    [SerializeField] private TMP_Text sessionText;
    [SerializeField] private TMP_Text totalTimeText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text userName;
    [SerializeField] private TMP_Text generalSuggestionText;


    private void Start()
    {
        if (PersistenceManager.Instance == null)
        {
            Debug.LogWarning("[SessionReader] No existe un PersistenceManager.");
            ShowEmptyState();
            return;
        }

        if (PersistenceManager.Instance.SessionService == null)
        {
            Debug.LogWarning("[SessionReader] No existe un SessionService.");
            ShowEmptyState();
            return;
        }

        IReadOnlyList<SessionSummary> lastSessions =
            PersistenceManager.Instance.SessionService.GetLastSessions(1);

        if (lastSessions == null || lastSessions.Count == 0)
        {
            Debug.LogWarning("[SessionReader] No hay sesiones persistidas para mostrar.");
            ShowEmptyState();
            return;
        }

        session = lastSessions[lastSessions.Count - 1];
        if (!IsCompleteSession(session))
        {
            Debug.LogWarning("[SessionReader] La última sesión persistida no está completa.");
            session = null;
            ShowEmptyState();
            return;
        }

        UpdateSessionInfo();
        ConfigureExerciseTabs();

        AddButtonListener(absoluteButton, ShowAbsolute);
        AddButtonListener(relativeButton, ShowRelative);

        if (absoluteButton != null) absoluteButton.interactable = true;
        if (relativeButton != null) relativeButton.interactable = true;

        isInitialized = true;
        SelectExercise(ExerciseType.Insert);
    }

    private void OnDestroy()
    {
        RemoveButtonListener(insertTab, ShowInsert);
        RemoveButtonListener(osuTab, ShowOsu);
        RemoveButtonListener(duckHunterTab, ShowDuckHunter);
        RemoveButtonListener(absoluteButton, ShowAbsolute);
        RemoveButtonListener(relativeButton, ShowRelative);
    }

    private void UpdateSessionInfo()
    {
        float duration = GetSessionDuration();
        if (sessionText != null)
            sessionText.text = $"Sesión #{session.SessionId}";

        if (userName != null)
        {
            userName.text = PersistenceManager.Instance.UserService == null
                ? string.Empty
                : PersistenceManager.Instance.UserService.UserName;
        }

        if (totalTimeText != null)
            totalTimeText.text = FormatDuration(duration);

        if (dateText != null)
            dateText.text = FormatSessionDate(session.date);
    }

    private void ConfigureExerciseTabs()
    {
        AddButtonListener(insertTab, ShowInsert);
        AddButtonListener(osuTab, ShowOsu);
        AddButtonListener(duckHunterTab, ShowDuckHunter);
        SetExerciseTabsInteractable(true);
    }

    private void ShowInsert()
    {
        SelectExercise(ExerciseType.Insert);
    }

    private void ShowOsu()
    {
        SelectExercise(ExerciseType.OSU);
    }

    private void ShowDuckHunter()
    {
        SelectExercise(ExerciseType.DuckHunter);
    }

    private void SelectExercise(ExerciseType exerciseType)
    {
        if (!isInitialized)
            return;

        selectedExerciseType = exerciseType;
        RefreshUI();
    }

    void SetGeneralSuggestion()
    {
        if (generalSuggestionText == null)
            return;

        ExerciseSummary currentSummary = CurrentSummary;
        if (currentSummary == null)
            return;

        string suggestion = currentSummary.generalSuggestion;
        generalSuggestionText.text = string.IsNullOrWhiteSpace(suggestion)
            ? "Continúa practicando para mejorar tu desempeño."
            : suggestion;
    }

    private void ShowAbsolute()
    {
        currentMode = SummaryMode.Absolute;
        RefreshUI();
    }

    private void ShowRelative()
    {
        currentMode = SummaryMode.Relative;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (!isInitialized || !HasValidSession)
        {
            ShowEmptyState();
            return;
        }

        ExerciseSummary currentSummary = CurrentSummary;
        if (currentSummary == null)
        {
            ShowEmptyState();
            return;
        }

        SetGeneralSuggestion();

        switch (currentMode)
        {
            case SummaryMode.Absolute:

                if (leftRadarChart != null)
                {
                    leftRadarChart.SetValues(
                        currentSummary.leftHand.absoluteUsage);
                }

                if (rightRadarChart != null)
                {
                    rightRadarChart.SetValues(
                        currentSummary.rightHand.absoluteUsage);
                }

                break;

            case SummaryMode.Relative:

                if (leftRadarChart != null)
                {
                    leftRadarChart.SetValues(
                        currentSummary.leftHand.relativeUsage);
                }

                if (rightRadarChart != null)
                {
                    rightRadarChart.SetValues(
                        currentSummary.rightHand.relativeUsage);
                }

                break;
        }

        //<-------------LINE CHARTS--------------->
        if (leftHandChart != null)
        {
            leftHandChart.SetValues(
                BuildCurrentSessionSeries(MotionZone.Hand, true));
        }

        if (leftWristChart != null)
        {
            leftWristChart.SetValues(
                BuildCurrentSessionSeries(MotionZone.Wrist, true));
        }

        if (leftForearmChart != null)
        {
            leftForearmChart.SetValues(
                BuildCurrentSessionSeries(MotionZone.Forearm, true));
        }

        if (rightHandChart != null)
        {
            rightHandChart.SetValues(
                BuildCurrentSessionSeries(MotionZone.Hand, false));
        }

        if (rightWristChart != null)
        {
            rightWristChart.SetValues(
                BuildCurrentSessionSeries(MotionZone.Wrist, false));
        }

        if (rightForearmChart != null)
        {
            rightForearmChart.SetValues(
                BuildCurrentSessionSeries(MotionZone.Forearm, false));
        }
    }
    private float GetUsageValue(HandUsageSummary summary, MotionZone zone)
    {
        if (summary.zones == null)
            return 0f;

        float[] values = currentMode == SummaryMode.Absolute
            ? summary.absoluteUsage
            : summary.relativeUsage;

        if (values == null)
            return 0f;

        for (int i = 0; i < summary.zones.Length; i++)
        {
            if (summary.zones[i] == zone && i < values.Length)
                return values[i];
        }

        return 0f;
    }

    private float[] BuildCurrentSessionSeries(MotionZone zone, bool isLeftHand)
    {
        IReadOnlyList<SessionSummary> history =
            PersistenceManager.Instance == null
                || PersistenceManager.Instance.SessionService == null
                ? null
                : PersistenceManager.Instance.SessionService.GetLastSessions(HistoryLength);

        float[] series = new float[HistoryLength];

        if (history == null || history.Count == 0)
            return series;

        int historyCount = Mathf.Min(history.Count, HistoryLength);
        int historyStart = history.Count - historyCount;
        int seriesOffset = HistoryLength - historyCount;

        for (int i = 0; i < historyCount; i++)
        {
            ExerciseSummary exercise = FindExerciseSummary(
                history[historyStart + i],
                selectedExerciseType);

            if (exercise == null)
                continue;

            HandUsageSummary hand =
                isLeftHand
                    ? exercise.leftHand
                    : exercise.rightHand;

            series[seriesOffset + i] = GetUsageValue(hand, zone);
        }

        return series;
    }

    private static ExerciseSummary FindExerciseSummary(
        SessionSummary summary,
        ExerciseType exerciseType)
    {
        if (summary == null || summary.Summaries == null)
            return null;

        foreach (ExerciseSummary exercise in summary.Summaries)
        {
            if (exercise != null && exercise.exerciseType == exerciseType)
                return exercise;
        }

        return null;
    }

    private static bool IsCompleteSession(SessionSummary summary)
    {
        if (summary == null || summary.Summaries == null || summary.Summaries.Count != 3)
            return false;

        HashSet<ExerciseType> exerciseTypes = new();
        foreach (ExerciseSummary exercise in summary.Summaries)
        {
            if (exercise == null || !exerciseTypes.Add(exercise.exerciseType))
                return false;
        }

        return exerciseTypes.Contains(ExerciseType.Insert)
            && exerciseTypes.Contains(ExerciseType.OSU)
            && exerciseTypes.Contains(ExerciseType.DuckHunter);
    }

    private void ShowEmptyState()
    {
        if (sessionText != null) sessionText.text = "Sin sesión";
        if (userName != null) userName.text = string.Empty;
        if (totalTimeText != null) totalTimeText.text = string.Empty;
        if (dateText != null) dateText.text = string.Empty;
        if (generalSuggestionText != null)
            generalSuggestionText.text = "No hay datos de sesión para mostrar.";

        SetExerciseTabsInteractable(false);

        if (absoluteButton != null) absoluteButton.interactable = false;
        if (relativeButton != null) relativeButton.interactable = false;
    }

    private void SetExerciseTabsInteractable(bool interactable)
    {
        if (insertTab != null) insertTab.interactable = interactable;
        if (osuTab != null) osuTab.interactable = interactable;
        if (duckHunterTab != null) duckHunterTab.interactable = interactable;
    }

    private static void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    private static void RemoveButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.RemoveListener(action);
    }

    private float GetSessionDuration()
    {
        float total = 0f;

        foreach (ExerciseSummary summary in session.Summaries)
            total += summary.exerciseDuration;

        return total;
    }
    private string FormatDuration(float seconds)
    {
        return $"Duracion: {TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss")} min";
    }
    private string FormatSessionDate(DateTime date)
    {
        CultureInfo culture = new("es-ES");

        string day = date.ToString("dd", culture);
        string month = culture.TextInfo.ToTitleCase(
            date.ToString("MMMM", culture));
        string year = date.ToString("yyyy", culture);

        return $"{day} de {month} {year}";
    }
}
