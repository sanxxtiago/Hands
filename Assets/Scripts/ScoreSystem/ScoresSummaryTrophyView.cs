using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class ScoresSummaryTrophyView : MonoBehaviour
{
    [Header("Trofeos 3D")]
    [SerializeField] private GameObject goldTrophy;
    [SerializeField] private GameObject silverTrophy;
    [SerializeField] private GameObject bronzeTrophy;

    [Header("Render")]
    [SerializeField] private Camera trophyCamera;
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private RawImage trophyImage;

    [Header("Animacion")]
    [SerializeField, Min(0f)] private float animationDuration = 0.6f;
    [SerializeField] private Ease animationEase = Ease.OutBack;

    private Tween scaleTween;
    private void Awake()
    {
        ValidateConfiguration();
        HideAllTrophies();

        if (trophyImage != null)
        {
            trophyImage.texture = renderTexture;
            trophyImage.enabled = false;
        }
    }

    private void OnDestroy()
    {
        scaleTween?.Kill();
    }

    public void Show(TrophyTier tier)
    {
        GameObject trophy = GetTrophy(tier);
        if (trophy == null)
        {
            Debug.LogWarning("[ScoresSummaryTrophyView] No hay modelo para el tier " + tier + ".");
            Hide();
            return;
        }

        scaleTween?.Kill();
        HideAllTrophies();

        trophy.SetActive(true);
        trophy.transform.localScale = Vector3.zero;

        if (trophyImage != null)
        {
            trophyImage.texture = renderTexture;
            trophyImage.enabled = true;
        }

        scaleTween = trophy.transform
            .DOScale(Vector3.one, animationDuration)
            .SetEase(animationEase);
    }

    public void Hide()
    {
        scaleTween?.Kill();
        scaleTween = null;
        HideAllTrophies();

        if (trophyImage != null)
            trophyImage.enabled = false;
    }

    private void HideAllTrophies()
    {
        SetTrophyState(goldTrophy, false);
        SetTrophyState(silverTrophy, false);
        SetTrophyState(bronzeTrophy, false);
    }

    private static void SetTrophyState(GameObject trophy, bool active)
    {
        if (trophy == null) return;
        trophy.SetActive(active);
        if (!active) trophy.transform.localScale = Vector3.zero;
    }

    private GameObject GetTrophy(TrophyTier tier)
    {
        switch (tier)
        {
            case TrophyTier.Gold: return goldTrophy;
            case TrophyTier.Silver: return silverTrophy;
            case TrophyTier.Bronze: return bronzeTrophy;
            default: return null;
        }
    }

    private void ValidateConfiguration()
    {
        if (goldTrophy == null || silverTrophy == null || bronzeTrophy == null)
            Debug.LogWarning("[ScoresSummaryTrophyView] Falta uno o mas modelos de trofeo.");
        if (trophyCamera == null)
            Debug.LogWarning("[ScoresSummaryTrophyView] Falta la camara del trofeo.");
        if (renderTexture == null)
            Debug.LogWarning("[ScoresSummaryTrophyView] Falta la RenderTexture del trofeo.");
        if (trophyImage == null)
            Debug.LogWarning("[ScoresSummaryTrophyView] Falta la RawImage del trofeo.");
    }
}
