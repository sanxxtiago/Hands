using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LeaderboardView : MonoBehaviour
{
    private const int ExpectedRowCount = 6;

    [Header("Visual")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Color currentUserTextColor = new Color(0.9456509f, 0.9811321f, 0.6618013f, 1f);
    [SerializeField, Min(0f)] private float currentUserRankFontSizeIncrease = 2f;

    private RowView[] rows = Array.Empty<RowView>();
    private string defaultStatusText = string.Empty;

    private void Awake()
    {
        CacheReferences();
    }

    public void Render(LeaderboardDisplayData data)
    {
        CacheReferences();

        for (int i = 0; i < rows.Length; i++)
        {
            LeaderboardRowData rowData = data != null
                && data.Rows != null
                && i < data.Rows.Count
                ? data.Rows[i]
                : null;

            rows[i].Render(rowData, currentUserTextColor, currentUserRankFontSizeIncrease);
        }

        if (statusText != null)
        {
            string message = data?.StatusMessage;
            statusText.text = string.IsNullOrEmpty(message)
                ? defaultStatusText
                : message;
        }
    }

    private void CacheReferences()
    {
        if (rows.Length > 0)
            return;

        CacheStatusText();
        CacheRows();
    }

    private void CacheStatusText()
    {
        if (statusText == null)
        {
            Transform subtitle = transform.Find("Subtitle");
            if (subtitle != null)
                statusText = subtitle.GetComponent<TMP_Text>();
        }

        if (statusText != null)
            defaultStatusText = statusText.text;
    }

    private void CacheRows()
    {
        List<RowView> cachedRows = new List<RowView>(ExpectedRowCount);

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (!child.name.StartsWith("LeaderBoardCard", StringComparison.OrdinalIgnoreCase))
                continue;

            cachedRows.Add(new RowView(child));
        }

        rows = cachedRows.ToArray();

        if (rows.Length != ExpectedRowCount)
        {
            Debug.LogWarning(
                $"[LeaderboardView] Se esperaban {ExpectedRowCount} filas y se encontraron {rows.Length}.");
        }
    }

    private sealed class RowView
    {
        private readonly GameObject root;
        private readonly TMP_Text positionText;
        private readonly TMP_Text initialsText;
        private readonly TMP_Text userNameText;
        private readonly TMP_Text scoreText;
        private readonly TMP_Text durationText;
        private readonly Image topAccent;
        private readonly Color defaultAccentColor;
        private readonly Color defaultPositionColor;
        private readonly Color defaultInitialsColor;
        private readonly Color defaultUserNameColor;
        private readonly Color defaultScoreColor;
        private readonly FontStyles defaultUserNameFontStyle;
        private readonly float defaultPositionFontSize;

        public RowView(Transform rowRoot)
        {
            root = rowRoot.gameObject;
            positionText = FindText(rowRoot, "Rank");
            initialsText = FindText(rowRoot, "Initials");
            userNameText = FindText(rowRoot, "UserName");
            scoreText = FindText(rowRoot, "Score");
            durationText = FindText(rowRoot, "Duration");
            topAccent = FindImage(rowRoot, "TopAccent");

            defaultAccentColor = topAccent != null ? topAccent.color : Color.white;
            defaultPositionColor = positionText != null ? positionText.color : Color.white;
            defaultInitialsColor = initialsText != null ? initialsText.color : Color.white;
            defaultUserNameColor = userNameText != null ? userNameText.color : Color.white;
            defaultScoreColor = scoreText != null ? scoreText.color : Color.white;
            defaultUserNameFontStyle = userNameText != null
                ? userNameText.fontStyle
                : FontStyles.Normal;
            defaultPositionFontSize = positionText != null
                ? positionText.fontSize
                : 0f;
        }

        public void Render(
            LeaderboardRowData data,
            Color currentTextColor,
            float rankFontSizeIncrease)
        {
            bool isVisible = data != null && data.IsVisible;
            root.SetActive(isVisible);

            if (!isVisible)
                return;

            bool isCurrentUser = data.IsCurrentUser;
            Color textColor = isCurrentUser ? currentTextColor : defaultUserNameColor;

            if (positionText != null)
            {
                positionText.text = $"#{data.Position}";
                positionText.color = isCurrentUser ? currentTextColor : defaultPositionColor;
                positionText.fontSize = isCurrentUser
                    ? defaultPositionFontSize + rankFontSizeIncrease
                    : defaultPositionFontSize;
            }

            if (initialsText != null)
            {
                initialsText.text = BuildInitials(data.UserName);
                initialsText.color = defaultInitialsColor;
            }

            if (userNameText != null)
            {
                userNameText.text = data.UserName;
                userNameText.color = textColor;
                userNameText.fontStyle = isCurrentUser
                    ? FontStyles.Bold
                    : defaultUserNameFontStyle;
            }

            if (scoreText != null)
            {
                scoreText.text = Mathf.RoundToInt(data.Score).ToString(CultureInfo.InvariantCulture);
                scoreText.color = isCurrentUser ? currentTextColor : defaultScoreColor;
            }

            if (durationText != null)
                durationText.text = FormatDuration(data.DurationSeconds);

            if (topAccent != null)
                topAccent.color = defaultAccentColor;
        }

        private static TMP_Text FindText(Transform root, params string[] names)
        {
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < names.Length; i++)
            {
                for (int j = 0; j < texts.Length; j++)
                {
                    if (string.Equals(texts[j].gameObject.name, names[i], StringComparison.OrdinalIgnoreCase))
                        return texts[j];
                }
            }

            return null;
        }

        private static Image FindImage(Transform root, string name)
        {
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (string.Equals(images[i].gameObject.name, name, StringComparison.OrdinalIgnoreCase))
                    return images[i];
            }

            return null;
        }

        private static string FormatDuration(float? durationSeconds)
        {
            if (!durationSeconds.HasValue
                || !ScoreMath.IsFinite(durationSeconds.Value)
                || durationSeconds.Value < 0f)
            {
                return "---";
            }

            int totalSeconds = Mathf.FloorToInt(durationSeconds.Value);
            int minutes = totalSeconds / 60;
            int remainingSeconds = totalSeconds % 60;
            string unit = minutes > 0 ? "mins" : "seg";

            return $"{minutes:D2}:{remainingSeconds:D2} {unit}";
        }

        private static string BuildInitials(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return "?";

            string[] words = userName.Trim().Split(
                new[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 1)
            {
                string word = words[0];
                return word.Length == 1
                    ? word.ToUpperInvariant()
                    : word.Substring(0, 2).ToUpperInvariant();
            }

            return (words[0][0].ToString() + words[1][0]).ToUpperInvariant();
        }
    }
}
