using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class InsertPhaseDefinition
{
    [SerializeField] private GameObject prefab;
    [SerializeField, Min(1)] private int expectedPieces = 1;

    public GameObject Prefab => prefab;
    public int ExpectedPieces => expectedPieces;
}

public class WallInsertExercise : ExerciseController
{
    // Composición de piezas de la fase que comienza; la UI de progreso la consume para pintar los iconos.
    public static event Action<int, PieceStepDescriptor[]> OnPhaseCompositionChanged;

    [Tooltip("Fases ordenadas de menor a mayor dificultad.")]
    [SerializeField] private List<InsertPhaseDefinition> phases = new();

    [Tooltip("Tiempo de espera antes de mostrar la siguiente fase.")]
    [SerializeField, Min(0f)] private float phaseTransitionDelay = 0.75f;

    [Tooltip("Duracion del fade al mostrar y retirar una fase.")]
    [SerializeField, Min(0f)] private float phaseFadeDuration = 0.35f;
    [Tooltip("Adaptador opcional para el score de gamificacion de Insert.")]
    [SerializeField] private InsertScoreAdapter scoreAdapter;

    private int currentPhaseIndex;
    private GameObject currentPhaseInstance;
    private RendererFadeData[] currentPhaseFadeData;
    private Coroutine phaseFadeCoroutine;
    private bool invalidConfiguration;
    private bool phaseTransitionInProgress;

    public float CompletionTime => elapsedTime;

    protected override void OnEnable()
    {
        base.OnEnable();
        PieceBehaviour.OnPieceSnapped += OnPieceSnapped;
    }
    protected override void OnDisable()
    {
        StopAllCoroutines();
        phaseFadeCoroutine = null;
        phaseTransitionInProgress = false;
        DeactivateCurrentPhase();
        PieceBehaviour.OnPieceSnapped -= OnPieceSnapped;
        base.OnDisable();
    }

    void Start()
    {
        int[] phaseTargets = new int[phases.Count];

        for (int i = 0; i < phases.Count; i++)
            phaseTargets[i] = phases[i]?.ExpectedPieces ?? 0;

        progressManager.Initialize(phaseTargets);
    }

    protected override void OnExerciseStart()
    {
        scoreAdapter?.Reset();

        if (phases.Count == 0)
        {
            return;
        }

        int totalPieces = 0;
        for (int i = 0; i < phases.Count; i++)
            totalPieces += Mathf.Max(0, phases[i]?.ExpectedPieces ?? 0);

        scoreAdapter?.BeginExercise(totalPieces, phases.Count);

        currentPhaseIndex = -1;
        invalidConfiguration = false;
        AdvanceToNextPhase();
    }

    protected override bool IsExerciseCompleted()
    {
        return invalidConfiguration || base.IsExerciseCompleted();
    }

    public void OnPieceSnapped(PieceBehaviour piece)
    {
        if (phases.Count == 0 || invalidConfiguration)
            return;

        if (phaseTransitionInProgress)
            return;

        if (currentPhaseInstance == null ||
            piece == null ||
            !piece.transform.IsChildOf(currentPhaseInstance.transform))
        {
            return;
        }

        progressManager.AddCompletedStep(
            new PieceStepDescriptor(piece.pieceType, piece.requiredHand));

        if (!progressManager.IsCompleted())
            return;

        scoreAdapter?.EndPhase();

        if (currentPhaseIndex >= phases.Count - 1)
            return;

        StartCoroutine(AdvanceToNextPhaseAfterDelay());
    }

    protected override void SetSpecificData()
    {
        scoreAdapter?.CompleteExercise(CompletionTime);
        sessionRecorder.SetInsertPiecesData(CompletionTime);
    }

    private void AdvanceToNextPhase()
    {
        DeactivateCurrentPhase();

        currentPhaseIndex++;

        if (currentPhaseIndex >= phases.Count)
        {
            return;
        }

        InsertPhaseDefinition phase = phases[currentPhaseIndex];

        if (phase == null || phase.Prefab == null)
        {
            Debug.LogError(
                $"Insert: la fase {currentPhaseIndex + 1} no tiene un prefab asignado.");
            invalidConfiguration = true;
            return;
        }

        currentPhaseInstance = Instantiate(phase.Prefab);
        currentPhaseFadeData = PreparePhaseFade(currentPhaseInstance);

        PieceBehaviour[] phasePieces =
            currentPhaseInstance.GetComponentsInChildren<PieceBehaviour>(true);

        if (phasePieces.Length != phase.ExpectedPieces)
        {
            Debug.LogWarning(
                $"Insert: la fase {currentPhaseIndex + 1} espera " +
                $"{phase.ExpectedPieces} piezas, pero contiene {phasePieces.Length}.");
        }

        foreach (PieceBehaviour piece in phasePieces)
        {
            piece.SetScorePhaseIndex(currentPhaseIndex);
            piece.ApplyChirality();
        }

        progressManager.BeginPhase(currentPhaseIndex);
        scoreAdapter?.BeginPhase(currentPhaseIndex);
        OnPhaseCompositionChanged?.Invoke(
            currentPhaseIndex,
            BuildComposition(phasePieces));
        StartPhaseFade(0f, 1f);
    }

    private static PieceStepDescriptor[] BuildComposition(PieceBehaviour[] pieces)
    {
        PieceStepDescriptor[] composition = new PieceStepDescriptor[pieces.Length];

        for (int i = 0; i < pieces.Length; i++)
        {
            PieceBehaviour piece = pieces[i];
            composition[i] = piece != null
                ? new PieceStepDescriptor(piece.pieceType, piece.requiredHand)
                : default;
        }

        return composition;
    }

    private IEnumerator AdvanceToNextPhaseAfterDelay()
    {
        phaseTransitionInProgress = true;

        // Mantenemos la fase visible para que el usuario pueda ver las piezas encajadas.

        if (phaseTransitionDelay > 0f)
            yield return new WaitForSeconds(phaseTransitionDelay);

        StopPhaseFade();

        if (currentPhaseFadeData != null)
            yield return FadePhase(currentPhaseFadeData, 1f, 0f);

        phaseTransitionInProgress = false;
        AdvanceToNextPhase();
    }

    private RendererFadeData[] PreparePhaseFade(GameObject phaseInstance)
    {
        Renderer[] renderers =
            phaseInstance.GetComponentsInChildren<Renderer>(true);

        RendererFadeData[] fadeData = new RendererFadeData[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].materials;
            MaterialFadeData[] materialData = new MaterialFadeData[materials.Length];

            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];
                ConfigureTransparentMaterial(material);

                materialData[j] = new MaterialFadeData(
                    material,
                    GetMaterialColor(material, "_BaseColor"),
                    GetMaterialColor(material, "_Color"));
            }

            fadeData[i] = new RendererFadeData(materialData);
        }

        return fadeData;
    }

    private void StartPhaseFade(float from, float to)
    {
        StopPhaseFade();
        phaseFadeCoroutine = StartCoroutine(FadePhase(currentPhaseFadeData, from, to));
    }

    private void StopPhaseFade()
    {
        if (phaseFadeCoroutine == null)
            return;

        StopCoroutine(phaseFadeCoroutine);
        phaseFadeCoroutine = null;
    }

    private IEnumerator FadePhase(
        RendererFadeData[] fadeData,
        float from,
        float to)
    {
        if (fadeData == null)
        {
            phaseFadeCoroutine = null;
            yield break;
        }

        float elapsed = 0f;

        ApplyFadeAlpha(fadeData, from);

        if (phaseFadeDuration <= 0f)
        {
            ApplyFadeAlpha(fadeData, to);
            phaseFadeCoroutine = null;
            yield break;
        }

        while (elapsed < phaseFadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / phaseFadeDuration);
            ApplyFadeAlpha(fadeData, Mathf.SmoothStep(from, to, progress));
            yield return null;
        }

        ApplyFadeAlpha(fadeData, to);
        phaseFadeCoroutine = null;
    }

    private void ApplyFadeAlpha(RendererFadeData[] fadeData, float alpha)
    {
        foreach (RendererFadeData rendererData in fadeData)
        {
            foreach (MaterialFadeData materialData in rendererData.Materials)
            {
                SetMaterialColor(
                    materialData.Material,
                    "_BaseColor",
                    WithAlpha(materialData.BaseColor, alpha));

                SetMaterialColor(
                    materialData.Material,
                    "_Color",
                    WithAlpha(materialData.LegacyColor, alpha));
            }
        }
    }

    private static void ConfigureTransparentMaterial(Material material)
    {
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);

        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);

        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);

        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    }

    private static Color GetMaterialColor(Material material, string property)
    {
        return material.HasProperty(property)
            ? material.GetColor(property)
            : Color.white;
    }

    private static void SetMaterialColor(
        Material material,
        string property,
        Color color)
    {
        if (material.HasProperty(property))
            material.SetColor(property, color);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a *= alpha;
        return color;
    }

    private void DeactivateCurrentPhase()
    {
        if (currentPhaseInstance == null)
            return;

        currentPhaseFadeData = null;
        currentPhaseInstance.SetActive(false);
        Destroy(currentPhaseInstance);
        currentPhaseInstance = null;
    }

    private sealed class RendererFadeData
    {
        public readonly MaterialFadeData[] Materials;

        public RendererFadeData(MaterialFadeData[] materials)
        {
            Materials = materials;
        }
    }

    private sealed class MaterialFadeData
    {
        public readonly Material Material;
        public readonly Color BaseColor;
        public readonly Color LegacyColor;

        public MaterialFadeData(
            Material material,
            Color baseColor,
            Color legacyColor)
        {
            Material = material;
            BaseColor = baseColor;
            LegacyColor = legacyColor;
        }
    }

}
