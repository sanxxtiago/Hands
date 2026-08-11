using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionReader : MonoBehaviour
{
    public enum SummaryMode
    {
        Absolute,
        Relative
    }

    [Header("Controls")]
    [SerializeField] private TMP_Dropdown exerciseDropdown;
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
        session.Summaries[exerciseDropdown.value];
    private SummaryMode currentMode = SummaryMode.Absolute;
    [SerializeField] private TMP_Text sessionText;
    [SerializeField] private TMP_Text totalTimeText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text userName;
    [SerializeField] private TMP_Text generalSuggestionText;


    private readonly List<ExerciseSummary> exercises = new();

    private void Start()
    {
        if (SessionManager.Instance == null)
        {
            Debug.LogWarning("No existe un SessionManager.");
            return;
        }
        else
        {
            Debug.Log("Si hay xs");
        }

        //Guarda la sesión actual y obtiene esa misma para una única fuente de verdad
        session = PersistenceManager.Instance
            .SessionService
            .AddSession(SessionManager.Instance.CurrentSession);

        if (session == null || session.Summaries.Count == 0)
        {
            Debug.LogWarning("No hay datos de sesión para mostrar.");
            return;
        }

        UpdateSessionInfo();
        ConfigureDropdown();

        absoluteButton.onClick.AddListener(ShowAbsolute);
        relativeButton.onClick.AddListener(ShowRelative);

        exerciseDropdown.value = 0;

        RefreshUI();
    }

    private void UpdateSessionInfo()
    {
        float duration = GetSessionDuration();
        sessionText.text = $"Sesión #{session.SessionId}";
        userName.text = PersistenceManager.Instance.UserService.UserName;
        totalTimeText.text = FormatDuration(duration);
        dateText.text = FormatSessionDate(session.date);
    }

    private void ConfigureDropdown()
    {
        exerciseDropdown.ClearOptions();
        exercises.Clear();

        List<string> options = new();

        foreach (ExerciseSummary summary in session.Summaries)
        {
            exercises.Add(summary);

            options.Add(summary.exerciseType switch
            {
                ExerciseType.Insert => "Inserción de piezas",
                ExerciseType.Duck => "Cazador de patos",
                ExerciseType.OSU => "Precisión",
                _ => summary.exerciseType.ToString()
            });
        }

        exerciseDropdown.AddOptions(options);
        exerciseDropdown.onValueChanged.AddListener(OnExerciseChanged);
    }

    private void OnExerciseChanged(int index)
    {
        RefreshUI();
    }

    void SetGeneralSuggestion()
    {
        if (generalSuggestionText == null)
            return;

        string suggestion = CurrentSummary.generalSuggestion;
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
        SetGeneralSuggestion();

        switch (currentMode)
        {
            case SummaryMode.Absolute:

                leftRadarChart.SetValues(
                    CurrentSummary.leftHand.absoluteUsage);

                rightRadarChart.SetValues(
                    CurrentSummary.rightHand.absoluteUsage);

                break;

            case SummaryMode.Relative:

                leftRadarChart.SetValues(
                    CurrentSummary.leftHand.relativeUsage);

                rightRadarChart.SetValues(
                    CurrentSummary.rightHand.relativeUsage);

                break;
        }

        //<-------------LINE CHARTS--------------->
        leftHandChart.SetValues(
       BuildCurrentSessionSeries(MotionZone.Hand, true));

        leftWristChart.SetValues(
            BuildCurrentSessionSeries(MotionZone.Wrist, true));

        leftForearmChart.SetValues(
            BuildCurrentSessionSeries(MotionZone.Forearm, true));

        rightHandChart.SetValues(
            BuildCurrentSessionSeries(MotionZone.Hand, false));

        rightWristChart.SetValues(
            BuildCurrentSessionSeries(MotionZone.Wrist, false));

        rightForearmChart.SetValues(
            BuildCurrentSessionSeries(MotionZone.Forearm, false));
    }
    private float GetUsageValue(HandUsageSummary summary, MotionZone zone)
    {
        float[] values = currentMode == SummaryMode.Absolute
            ? summary.absoluteUsage
            : summary.relativeUsage;

        for (int i = 0; i < summary.zones.Length; i++)
        {
            if (summary.zones[i] == zone)
                return values[i];
        }

        return 0f;
    }

    private float[] BuildCurrentSessionSeries(MotionZone zone, bool isLeftHand)
    {
        IReadOnlyList<SessionSummary> history =
            PersistenceManager.Instance
                .SessionService
                .GetLastSessions();

        float[] series = new float[7];

        int offset = 7 - history.Count;

        for (int i = 0; i < history.Count; i++)
        {
            ExerciseSummary exercise =
                history[i].Summaries[exerciseDropdown.value];

            HandUsageSummary hand =
                isLeftHand
                    ? exercise.leftHand
                    : exercise.rightHand;

            series[offset + i] = GetUsageValue(hand, zone);
        }

        return series;
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