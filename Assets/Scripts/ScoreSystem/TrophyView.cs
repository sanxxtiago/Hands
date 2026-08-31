using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

public class TrophyView : MonoBehaviour
{
    [Header("Trofeo 3D")]
    [SerializeField] private GameObject goldTrophy;
    [SerializeField] private GameObject silverTrophy;
    [SerializeField] private GameObject bronzeTrophy;

    [Header("RenderTexture")]
    [SerializeField] private Camera trophyCamera;
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private RawImage trophyImage;

    [Header("Anclaje al score UI")]
    [Tooltip("RectTransform de TotalScoreText")]
    [SerializeField] private RectTransform scoreAnchorTarget;

    [Header("Datos del ejercicio")]
    [SerializeField] private ScoreManager scoreManager;

    [Header("Animación")]
    [SerializeField] private float animationDuration = 0.6f;
    [SerializeField] private float anchorYOffset = -80f;

    [Header("Emisión")]
    [SerializeField] private Color goldEmission = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private Color silverEmission = new Color(0.75f, 0.78f, 0.85f, 1f);
    [SerializeField] private Color bronzeEmission = new Color(0.8f, 0.5f, 0.2f, 1f);
    [SerializeField] private float emissionDuration = 0.4f;

    private Tween _scaleTween;
    private bool _isInitialized;

    public static event Action<TrophyTier> OnTrophyLanded;

    private void OnEnable()
    {
        GameManager.OnShowResults += HandleShowResults;
    }

    private void OnDisable()
    {
        GameManager.OnShowResults -= HandleShowResults;
    }

    private void Start()
    {
        Initialize();
    }

    private void LateUpdate()
    {
        if (!_isInitialized || scoreAnchorTarget == null)
            return;

        UpdateScreenPosition();
    }

    private void Initialize()
    {
        if (goldTrophy != null) goldTrophy.SetActive(false);
        if (silverTrophy != null) silverTrophy.SetActive(false);
        if (bronzeTrophy != null) bronzeTrophy.SetActive(false);

        if (trophyImage != null)
            trophyImage.enabled = false;

        _isInitialized = true;
    }

    private void HandleShowResults()
    {
        if (scoreManager == null)
        {
            Debug.LogWarning("[TrophyView] ScoreManager no asignado. No se puede mostrar el trofeo.");
            return;
        }

        Show(scoreManager.LastScore?.trophyTier ?? TrophyTier.None);
    }

    public void Show(TrophyTier tier)
    {
        ResetState();

        if (tier == TrophyTier.None)
        {
            if (trophyImage != null)
                trophyImage.enabled = false;
            return;
        }

        GameObject trophy = GetTrophyGameObject(tier);
        if (trophy == null)
        {
            Debug.LogWarning("[TrophyView] Trofeo no asignado para tier: " + tier);
            return;
        }

        UpdateScreenPosition();

        trophy.SetActive(true);
        if (trophyImage != null)
            trophyImage.enabled = true;

        PlayScaleAnimation(trophy, tier);
    }

    public void Hide()
    {
        ResetState();
        if (trophyImage != null)
            trophyImage.enabled = false;
    }

    private void PlayScaleAnimation(GameObject trophy, TrophyTier tier)
    {
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one;

        if (_scaleTween != null)
            _scaleTween.Kill();

        trophy.transform.localScale = startScale;

        _scaleTween = trophy.transform
            .DOScale(endScale, animationDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() => OnLandingComplete(tier));
    }

    private void OnLandingComplete(TrophyTier tier)
    {
        PlayEmissionPulse(tier);
        OnTrophyLanded?.Invoke(tier);
    }

    private void PlayEmissionPulse(TrophyTier tier)
    {
        GameObject trophy = GetTrophyGameObject(tier);
        if (trophy == null)
            return;

        Renderer renderer = trophy.GetComponentInChildren<Renderer>();
        if (renderer == null)
            return;

        Material mat = renderer.material;
        if (mat == null)
            return;

        Color emissionColor = GetEmissionColor(tier);
        Color initialEmission = Color.black;

        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", initialEmission);

        mat.DOColor(emissionColor * 1.5f, "_EmissionColor", emissionDuration * 0.3f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                mat.DOColor(emissionColor * 0.5f, "_EmissionColor", emissionDuration * 0.7f)
                    .SetEase(Ease.OutQuad);
            });
    }

    private void UpdateScreenPosition()
    {
        Vector3 worldPos = new Vector3(
            scoreAnchorTarget.position.x,
            scoreAnchorTarget.position.y + anchorYOffset,
            scoreAnchorTarget.position.z);

        transform.position = worldPos;
    }

    private void ResetState()
    {
        _scaleTween?.Kill();
        _scaleTween = null;

        if (goldTrophy != null) { goldTrophy.SetActive(false); goldTrophy.transform.localScale = Vector3.zero; }
        if (silverTrophy != null) { silverTrophy.SetActive(false); silverTrophy.transform.localScale = Vector3.zero; }
        if (bronzeTrophy != null) { bronzeTrophy.SetActive(false); bronzeTrophy.transform.localScale = Vector3.zero; }
    }

    private GameObject GetTrophyGameObject(TrophyTier tier)
    {
        return tier switch
        {
            TrophyTier.Gold => goldTrophy,
            TrophyTier.Silver => silverTrophy,
            TrophyTier.Bronze => bronzeTrophy,
            _ => null
        };
    }

    private Color GetEmissionColor(TrophyTier tier)
    {
        return tier switch
        {
            TrophyTier.Gold => goldEmission,
            TrophyTier.Silver => silverEmission,
            TrophyTier.Bronze => bronzeEmission,
            _ => Color.black
        };
    }
}
