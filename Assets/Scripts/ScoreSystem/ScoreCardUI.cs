using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ScoreCardUI : MonoBehaviour
{
//     [SerializeField] private GameObject activeIndicator;
//     [SerializeField] private GameObject completedIndicator;
//     [SerializeField] private GameObject emptyIndicator;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text trophyTierText;
    [SerializeField] private Image accentImage;

    public void SetData(ScoreRecord record, bool isMain)
    {
        if (record == null)
        {
            ShowEmpty();
            return;
        }

        ShowCompleted();
        SetScore(record.totalScore);
        SetTrophyTier(FormatTrophyTier(record.trophyTier));

        if (accentImage != null && isMain)
            accentImage.enabled = true;
    }

    // public void SetActive()
    // {
    //     if (activeIndicator != null) activeIndicator.SetActive(true);
    //     if (completedIndicator != null) completedIndicator.SetActive(false);
    //     if (emptyIndicator != null) emptyIndicator.SetActive(false);
    // }

    public void SetScore(float score)
    {
        if (scoreText != null)
            scoreText.text = Mathf.RoundToInt(score).ToString();
    }

    public void SetTrophyTier(string trophyTier)
    {
        if (trophyTierText != null)
            trophyTierText.text = trophyTier;
    }

    private static string FormatTrophyTier(TrophyTier tier)
    {
        return tier switch
        {
            TrophyTier.Gold => "ORO",
            TrophyTier.Silver => "PLATA",
            TrophyTier.Bronze => "BRONCE",
            _ => "-",
        };
    }

    public void ShowEmpty()
    {
        // if (activeIndicator != null) activeIndicator.SetActive(false);
        // if (completedIndicator != null) completedIndicator.SetActive(false);
        // if (emptyIndicator != null) emptyIndicator.SetActive(true);
        if (scoreText != null) scoreText.text = "-";
        if (trophyTierText != null) trophyTierText.text = "";
    }

    public void ShowCompleted()
    {
        // if (activeIndicator != null) activeIndicator.SetActive(false);
        // if (completedIndicator != null) completedIndicator.SetActive(true);
        // if (emptyIndicator != null) emptyIndicator.SetActive(false);
    }

    public void Clear()
    {
        // if (activeIndicator != null) activeIndicator.SetActive(false);
        // if (completedIndicator != null) completedIndicator.SetActive(false);
        // if (emptyIndicator != null) emptyIndicator.SetActive(false);
        if (scoreText != null) scoreText.text = "-";
        if (trophyTierText != null) trophyTierText.text = "";
        if (accentImage != null) accentImage.enabled = false;
    }
}
