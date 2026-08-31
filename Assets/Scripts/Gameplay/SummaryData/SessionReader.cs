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

    private enum SummaryContentMode
    {
        Usage,
        Exposition
    }

    [Header("Controls")]
    [SerializeField] private Button insertTab;
    [SerializeField] private Button osuTab;
    [SerializeField] private Button duckHunterTab;
    [SerializeField] private Button absoluteButton;
    [SerializeField] private Button relativeButton;
    [SerializeField] private Color selectedButtonColor = new Color(0.8666667f, 0.9058824f, 1f, 1f);

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
    private SummaryContentMode currentContentMode = SummaryContentMode.Usage;
    private bool isInitialized;
    private Color insertTabBaseColor;
    private Color osuTabBaseColor;
    private Color duckHunterTabBaseColor;
    private Color absoluteButtonBaseColor;
    private Color relativeButtonBaseColor;
    private Color usageAbsoluteButtonBaseColor;
    private Color usageRelativeButtonBaseColor;
    [SerializeField] private TMP_Text sessionText;
    [SerializeField] private TMP_Text totalTimeText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text userName;
    [SerializeField] private TMP_Text generalSuggestionText;

    [Header("Summary panels")]
    [SerializeField] private Button usageAbsoluteButton;
    [SerializeField] private Button usageRelativeButton;
    [SerializeField] private GameObject usagePanel;
    [SerializeField] private ExpositionPanel expositionPanel;


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
        InitializeButtonVisuals();

        AddButtonListener(absoluteButton, ShowUsage);
        AddButtonListener(relativeButton, ShowExposition);
        AddButtonListener(usageAbsoluteButton, ShowAbsolute);
        AddButtonListener(usageRelativeButton, ShowRelative);

        if (absoluteButton != null) absoluteButton.interactable = true;
        if (relativeButton != null) relativeButton.interactable = true;
        if (usageAbsoluteButton != null) usageAbsoluteButton.interactable = true;
        if (usageRelativeButton != null) usageRelativeButton.interactable = true;

        isInitialized = true;
        SelectExercise(ExerciseType.Insert);
    }

    private void OnDestroy()
    {
        RemoveButtonListener(insertTab, ShowInsert);
        RemoveButtonListener(osuTab, ShowOsu);
        RemoveButtonListener(duckHunterTab, ShowDuckHunter);
        RemoveButtonListener(absoluteButton, ShowUsage);
        RemoveButtonListener(relativeButton, ShowExposition);
        RemoveButtonListener(usageAbsoluteButton, ShowAbsolute);
        RemoveButtonListener(usageRelativeButton, ShowRelative);
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
        UpdateExerciseButtonVisuals();
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
        currentContentMode = SummaryContentMode.Usage;
        UpdateModeButtonVisuals();
        UpdateContentButtonVisuals();
        RefreshUI();
    }

    private void ShowRelative()
    {
        currentMode = SummaryMode.Relative;
        currentContentMode = SummaryContentMode.Usage;
        UpdateModeButtonVisuals();
        UpdateContentButtonVisuals();
        RefreshUI();
    }

    private void ShowUsage()
    {
        currentContentMode = SummaryContentMode.Usage;
        UpdateContentButtonVisuals();
        RefreshUI();
    }

    private void ShowExposition()
    {
        currentContentMode = SummaryContentMode.Exposition;
        UpdateContentButtonVisuals();
        RefreshUI();
    }

    private void InitializeButtonVisuals()
    {
        insertTabBaseColor = GetButtonColor(insertTab);
        osuTabBaseColor = GetButtonColor(osuTab);
        duckHunterTabBaseColor = GetButtonColor(duckHunterTab);
        absoluteButtonBaseColor = GetButtonColor(absoluteButton);
        relativeButtonBaseColor = GetButtonColor(relativeButton);
        usageAbsoluteButtonBaseColor = GetButtonColor(usageAbsoluteButton);
        usageRelativeButtonBaseColor = GetButtonColor(usageRelativeButton);

        UpdateExerciseButtonVisuals();
        UpdateContentButtonVisuals();
        UpdateModeButtonVisuals();
    }

    private void UpdateExerciseButtonVisuals()
    {
        SetButtonColor(insertTab, selectedExerciseType == ExerciseType.Insert
            ? selectedButtonColor
            : insertTabBaseColor);
        SetButtonColor(osuTab, selectedExerciseType == ExerciseType.OSU
            ? selectedButtonColor
            : osuTabBaseColor);
        SetButtonColor(duckHunterTab, selectedExerciseType == ExerciseType.DuckHunter
            ? selectedButtonColor
            : duckHunterTabBaseColor);
    }

    private void UpdateModeButtonVisuals()
    {
        SetButtonColor(usageAbsoluteButton, currentMode == SummaryMode.Absolute
            ? selectedButtonColor
            : usageAbsoluteButtonBaseColor);
        SetButtonColor(usageRelativeButton, currentMode == SummaryMode.Relative
            ? selectedButtonColor
            : usageRelativeButtonBaseColor);
    }

    private void UpdateContentButtonVisuals()
    {
        SetButtonColor(absoluteButton, currentContentMode == SummaryContentMode.Usage
            ? selectedButtonColor
            : absoluteButtonBaseColor);
        SetButtonColor(relativeButton, currentContentMode == SummaryContentMode.Exposition
            ? selectedButtonColor
            : relativeButtonBaseColor);
    }

    private static Color GetButtonColor(Button button)
    {
        return button != null && button.targetGraphic != null
            ? button.targetGraphic.color
            : Color.white;
    }

    private static void SetButtonColor(Button button, Color color)
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = color;
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
        UpdateContentPanelVisuals();

        if (currentContentMode == SummaryContentMode.Exposition)
        {
            if (expositionPanel != null)
                expositionPanel.SetData(FindCurrentExposition());
        }
        else
        {
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

    private ExpositionSummary FindCurrentExposition()
    {
        if (PersistenceManager.Instance == null
            || PersistenceManager.Instance.ExpositionServices == null
            || session == null)
        {
            return null;
        }

        return PersistenceManager.Instance.ExpositionServices.GetExposition(
            session.SessionGuid,
            selectedExerciseType);
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
        if (usageAbsoluteButton != null) usageAbsoluteButton.interactable = false;
        if (usageRelativeButton != null) usageRelativeButton.interactable = false;

        SetActive(usagePanel, false);
        if (expositionPanel != null)
            expositionPanel.gameObject.SetActive(false);
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

    private void UpdateContentPanelVisuals()
    {
        bool showUsage = currentContentMode == SummaryContentMode.Usage;
        SetActive(usagePanel, showUsage);

        if (expositionPanel != null)
            expositionPanel.gameObject.SetActive(!showUsage);

        SetActive(usageAbsoluteButton, showUsage);
        SetActive(usageRelativeButton, showUsage);
    }

    private static void SetActive(GameObject gameObject, bool active)
    {
        if (gameObject != null && gameObject.activeSelf != active)
            gameObject.SetActive(active);
    }

    private static void SetActive(Component component, bool active)
    {
        if (component != null)
            SetActive(component.gameObject, active);
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
