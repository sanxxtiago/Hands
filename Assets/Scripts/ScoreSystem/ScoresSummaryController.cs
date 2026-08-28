using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public sealed class ScoresSummaryController : MonoBehaviour
{
    [Header("Session Source")]
    [SerializeField] private bool useLastSession = true;
    [SerializeField] private string explicitSessionGuid;

    [Header("Main Score Panel")]
    [SerializeField] private TMP_Text mainScoreText;
    [SerializeField] private TMP_Text mainGradeText;
    [SerializeField] private TMP_Text motivationalMessageText;
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private TMP_Text challengeNameText;
    [SerializeField] private TMP_Text trophyText;
    [SerializeField] private Button insertButton;
    [SerializeField] private Button osuButton;
    [SerializeField] private Button duckHunterButton;
    [SerializeField] private Color tabHighlightColor = new Color(0.8666667f, 0.9058824f, 1f, 1f);
    [SerializeField] private ScoresSummaryTrophyView trophyView;
    [SerializeField] private Slider rankProgressSlider;
    [SerializeField] private Image rankProgressFill;
    [SerializeField] private Image rankProgressNeedle;

    [Header("Breakdown Panel")]
    [SerializeField] private Transform breakdownContainer;
    [SerializeField] private GameObject breakdownRowPrefab;

    [Header("Exercise Cards")]
    [SerializeField] private ScoreCardUI insertCard;
    [SerializeField] private ScoreCardUI osuCard;
    [SerializeField] private ScoreCardUI duckHunterCard;

    [Header("Challenge Header")]
    [SerializeField] private TMP_Text challengeMetaText;

    [Header("Stat Cards (Score panel)")]
    [SerializeField] private GameObject statCardDuration;
    [SerializeField] private GameObject statCardHits;
    [SerializeField] private GameObject statCardMisses;
    [SerializeField] private TMP_Text statDurationValue;
    [SerializeField] private TMP_Text statHitsValue;
    [SerializeField] private TMP_Text statMissesValue;

    private ScoreRecord insertRecord = null;
    private ScoreRecord osuRecord = null;
    private ScoreRecord duckRecord = null;
    private Color insertTabBaseColor;
    private Color osuTabBaseColor;
    private Color duckHunterTabBaseColor;

    private void Start()
    {
        InitializeTabVisuals();
        AddButtonListener(insertButton, SelectInsert);
        AddButtonListener(osuButton, SelectOsu);
        AddButtonListener(duckHunterButton, SelectDuckHunter);
        SetSelectedTab(insertButton);

        IReadOnlyList<ScoreRecord> records = LoadAllSessionRecords();
        Debug.Log($"RECORDS: {records}");

        insertRecord = FindRecord(records, ScoreExerciseType.Insert);
        osuRecord = FindRecord(records, ScoreExerciseType.OSU);
        duckRecord = FindRecord(records, ScoreExerciseType.DuckHunter);

        ScoreRecord firstRecord = insertRecord ?? osuRecord ?? duckRecord;
        if (firstRecord == null)
        {
            Debug.LogWarning("[ScoresSummary] No hay ScoreRecords para la sesion activa.");
            ClearAll();
            return;
        }

        PopulateAverageScore(records);
        PopulateMainPanel(firstRecord);
        if (trophyView != null)
            trophyView.Show(firstRecord.trophyTier);
        PopulateExerciseCards(insertRecord, osuRecord, duckRecord);
        PopulateBreakdown(firstRecord);
        PopulateStatCards(firstRecord.statsData);
    }

    private void OnDestroy()
    {
        RemoveButtonListener(insertButton, SelectInsert);
        RemoveButtonListener(osuButton, SelectOsu);
        RemoveButtonListener(duckHunterButton, SelectDuckHunter);
    }

    public void SelectInsert()
    {
        SetSelectedTab(insertButton);
        SelectExercise(insertRecord);
    }

    public void SelectOsu()
    {
        SetSelectedTab(osuButton);
        SelectExercise(osuRecord);
    }

    public void SelectDuckHunter()
    {
        SetSelectedTab(duckHunterButton);
        SelectExercise(duckRecord);
    }

    private void SelectExercise(ScoreRecord record)
    {

        if (record == null)
        {
            Debug.LogWarning("[ScoresSummary] No hay datos para el ejercicio seleccionado.");
            return;
        }

        PopulateMainPanel(record);
        if (trophyView != null)
            trophyView.Show(record.trophyTier);
    }

    private static void AddButtonListener(Button button, UnityAction action)
    {
        if (button != null) button.onClick.AddListener(action);
    }

    private static void RemoveButtonListener(Button button, UnityAction action)
    {
        if (button != null) button.onClick.RemoveListener(action);
    }

    private void InitializeTabVisuals()
    {
        insertTabBaseColor = GetButtonColor(insertButton);
        osuTabBaseColor = GetButtonColor(osuButton);
        duckHunterTabBaseColor = GetButtonColor(duckHunterButton);

        DisableColorTransition(insertButton);
        DisableColorTransition(osuButton);
        DisableColorTransition(duckHunterButton);
    }

    private void SetSelectedTab(Button selectedButton)
    {
        SetButtonColor(insertButton, selectedButton == insertButton ? tabHighlightColor : insertTabBaseColor);
        SetButtonColor(osuButton, selectedButton == osuButton ? tabHighlightColor : osuTabBaseColor);
        SetButtonColor(duckHunterButton, selectedButton == duckHunterButton
            ? tabHighlightColor
            : duckHunterTabBaseColor);
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

    private static void DisableColorTransition(Button button)
    {
        if (button != null)
            button.transition = Selectable.Transition.None;
    }

    private void PopulateAverageScore(IReadOnlyList<ScoreRecord> records)
    {
        if (pointsText == null) return;
        float total = 0f;
        int count = 0;
        if (records != null)
        {
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i] == null || !records[i].isValid) continue;
                total += records[i].totalScore;
                count++;
            }
        }
        pointsText.text = count == 0 ? "-" : (total / count).ToString("F2", CultureInfo.InvariantCulture);
    }

    private ScoreRecord ResolveMainRecord()
    {
        if (PersistenceManager.Instance == null)
        {
            Debug.LogWarning("[ScoresSummary] PersistenceManager no disponible.");
            return null;
        }

        string sessionGuid = ResolveSessionGuid();
        IReadOnlyList<ScoreRecord> records = string.IsNullOrEmpty(sessionGuid)
            ? PersistenceManager.Instance.ScoreService.GetLastScores(1)
            : PersistenceManager.Instance.ScoreService.GetScoresForSession(sessionGuid);

        if (records == null || records.Count == 0)
            return null;

        return records[records.Count - 1];
    }

    private string ResolveSessionGuid()
    {
        if (!useLastSession)
            return explicitSessionGuid;

        if (SessionManager.Instance != null && SessionManager.Instance.CurrentSession != null)
            return SessionManager.Instance.CurrentSession.SessionGuid;

        return explicitSessionGuid;
    }

    private void PopulateMainPanel(ScoreRecord record)
    {
        if (mainScoreText != null)
            mainScoreText.text = Mathf.RoundToInt(record.totalScore).ToString();


        string trophy = FormatTrophy(record.scoreGrade);
        if (mainGradeText != null)
            mainGradeText.text = trophy;

        if (trophyText != null)
            trophyText.text = FormatTrophyTier(record.trophyTier);
        if (challengeNameText != null)
            challengeNameText.text = ScoreDisplayFormatter.FormatExerciseType(record.exerciseType);

        if (motivationalMessageText != null)
            motivationalMessageText.text = string.IsNullOrEmpty(record.motivationalMessage)
                ? "Continua practicando para mejorar tu desempeno."
                : record.motivationalMessage;

        if (rankProgressSlider != null)
        {
            rankProgressSlider.minValue = 0f;
            rankProgressSlider.maxValue = 100f;
            rankProgressSlider.value = Mathf.Clamp(record.totalScore, 0f, 100f);
        }

        float needleNormalized = Mathf.Clamp01(record.totalScore / 100f);
        if (rankProgressNeedle != null)
        {
            RectTransform rt = rankProgressNeedle.rectTransform;
            float width = rt.parent is RectTransform parentRt ? parentRt.rect.width : 500f;
            rt.anchoredPosition = new Vector2(width * needleNormalized, rt.anchoredPosition.y);
        }

        if (rankProgressFill != null)
        {
            rankProgressFill.fillAmount = needleNormalized;
        }
    }

    private void PopulateExerciseCards(ScoreRecord insertRecord, ScoreRecord osuRecord, ScoreRecord duckRecord)
    {
        ResetCard(insertCard);
        ResetCard(osuCard);
        ResetCard(duckHunterCard);

        bool anyPending = false;

        if (insertCard != null && insertRecord != null)
        {
            insertCard.SetData(insertRecord, true);
            MarkCompleted(insertCard);
        }
        else if (insertCard != null)
        {
            insertCard.ShowEmpty();
            anyPending = true;
        }

        if (osuCard != null && osuRecord != null)
        {
            osuCard.SetData(osuRecord, false);
            MarkCompleted(osuCard);
        }
        else if (osuCard != null)
        {
            osuCard.ShowEmpty();
            anyPending = true;
        }

        if (duckHunterCard != null && duckRecord != null)
        {
            duckHunterCard.SetData(duckRecord, false);
            MarkCompleted(duckHunterCard);
        }
        else if (duckHunterCard != null)
        {
            duckHunterCard.ShowEmpty();
            anyPending = true;
        }

        if (challengeMetaText != null)
        {
            if (anyPending)
            {
                ScoreRecord activeRecord = insertRecord ?? osuRecord ?? duckRecord;
                challengeMetaText.text = activeRecord == null
                    ? "RETO ACTIVO"
                    : "RETO ACTIVO: " + ScoreDisplayFormatter.FormatExerciseType(activeRecord.exerciseType);
            }
            else
                challengeMetaText.text = "SESION COMPLETADA";
        }
    }

    private void PopulateBreakdown(ScoreRecord record)
    {
        if (breakdownContainer == null)
            return;

        for (int i = breakdownContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(breakdownContainer.GetChild(i).gameObject);
        }

        if (record.breakdown == null || record.breakdown.Length == 0)
            return;

        if (breakdownRowPrefab == null)
        {
            Debug.LogWarning("[ScoresSummary] Falta breakdownRowPrefab; el breakdown se omite.");
            return;
        }

        for (int i = 0; i < record.breakdown.Length; i++)
        {
            ScoreBreakdown item = record.breakdown[i];
            GameObject row = Instantiate(breakdownRowPrefab, breakdownContainer);
            row.name = $"Breakdown_{item.metricId}";

         TMP_Text label = FindText(row, "Label", "Metric", "Name");
            TMP_Text value = FindText(row, "Value", "Score");

            if (label != null)
                label.text = FormatMetricId(item.metricId);

            if (value != null)
                value.text = Mathf.RoundToInt(item.metricScore).ToString() + "/100";
        }
    }

    private void PopulateStatCards(ScoreStatsData stats)
    {
        bool hasMisses = stats.misses > 0;
        SetActive(statCardDuration, true);
        SetActive(statCardHits, true);
        SetActive(statCardMisses, hasMisses);

        if (statDurationValue != null)
            statDurationValue.text = FormatDuration(stats.exerciseDuration);
        if (statHitsValue != null)
            statHitsValue.text = stats.hits.ToString();
        if (statMissesValue != null)
            statMissesValue.text = stats.misses.ToString();
    }

    private IReadOnlyList<ScoreRecord> LoadAllSessionRecords()
    {
        if (PersistenceManager.Instance == null || PersistenceManager.Instance.ScoreService == null)
        {
            Debug.LogWarning("[ScoresSummary] PersistenceManager o ScoreService no disponible.");
            return null;
        }

        string sessionGuid = ResolveSessionGuid();
        if (!string.IsNullOrEmpty(sessionGuid))
            return PersistenceManager.Instance.ScoreService.GetScoresForSession(sessionGuid);

        return PersistenceManager.Instance.ScoreService.GetLastScores(3);
    }

    private static ScoreRecord FindRecord(IReadOnlyList<ScoreRecord> records, ScoreExerciseType type)
    {
        if (records == null) return null;

        for (int i = 0; i < records.Count; i++)
        {
            if (records[i] != null && records[i].isValid && records[i].exerciseType == type)
                return records[i];
        }

        return null;
    }

    private static void MarkCompleted(ScoreCardUI card)
    {
        if (card == null) return;
        card.ShowCompleted();
    }

    private static void ResetCard(ScoreCardUI card)
    {
        if (card == null) return;
        card.Clear();
    }

    private void ClearAll()
    {
        ResetCard(insertCard);
        ResetCard(osuCard);
        ResetCard(duckHunterCard);
        if (insertCard != null) insertCard.ShowEmpty();
        if (osuCard != null) osuCard.ShowEmpty();
        if (duckHunterCard != null) duckHunterCard.ShowEmpty();

        if (mainScoreText != null) mainScoreText.text = "-";
        if (mainGradeText != null) mainGradeText.text = "";
        if (motivationalMessageText != null) motivationalMessageText.text = "Sin datos de sesion.";

        if (breakdownContainer != null)
        {
            for (int i = breakdownContainer.childCount - 1; i >= 0; i--)
                Destroy(breakdownContainer.GetChild(i).gameObject);
        }
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go == null) return;
        if (go.activeSelf != active) go.SetActive(active);
    }

    private static TMP_Text FindText(GameObject root, params string[] candidates)
    {
        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            string n = texts[i].gameObject.name;
            for (int j = 0; j < candidates.Length; j++)
            {
                if (n.IndexOf(candidates[j], System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return texts[i];
            }
        }
        return texts.Length > 0 ? texts[0] : null;
    }


    private static string FormatTrophy(string grade)
    {
        switch (grade)
        {
            case "Excellent": return "ORO";
            case "Good": return "PLATA";
            case "Fair": return "BRONCE";
            case "NeedsPractice": return "SIN TROFEO";
            default: return string.IsNullOrEmpty(grade) ? "-" : grade;
        }
    }

    private static string FormatTrophyTier(TrophyTier tier)
    {
        switch (tier)
        {
            case TrophyTier.Gold: return "ORO";
            case TrophyTier.Silver: return "PLATA";
            case TrophyTier.Bronze: return "BRONCE";
            default: return "-";
        }
    }

    private static string FormatDuration(float seconds)
    {
        if (!ScoreMath.IsFinite(seconds) || seconds < 0f) seconds = 0f;
        int totalSeconds = Mathf.FloorToInt(seconds);
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", minutes, remainingSeconds);
    }

    private static string FormatMetricId(string metricId)
    {
        if (string.IsNullOrEmpty(metricId)) return "Metrica";
        string[] words = metricId.Split('_');
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < words.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            string word = words[i];
            if (string.IsNullOrEmpty(word)) continue;
            sb.Append(char.ToUpperInvariant(word[0]));
            if (word.Length > 1) sb.Append(word.Substring(1));
        }
        return sb.ToString();
    }
}
