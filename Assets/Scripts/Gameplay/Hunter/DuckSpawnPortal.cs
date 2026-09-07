using System.Collections;
using UnityEngine;

public sealed class DuckSpawnPortal : MonoBehaviour
{
    [SerializeField] private Renderer barRenderer;
    [SerializeField, Min(0.01f)] private float spawnLeadTime = 0.14f;
    [SerializeField, Min(0.01f)] private float pulseDuration = 0.6f;
    [SerializeField] private Color inactiveColor = new Color(0.13f, 0.15f, 0.17f, 1f);
    [SerializeField] private Color activeColor = new Color(0.68f, 0.82f, 0.95f, 1f);
    [SerializeField, Min(0f)] private float idleEmission = 0.03f;
    [SerializeField, Min(0f)] private float peakEmission = 2.4f;

    private MaterialPropertyBlock propertyBlock;
    private float pulseElapsed;
    private bool isPulsing;

    private void Awake()
    {
        EnsurePropertyBlock();

        if (barRenderer == null)
            barRenderer = GetComponent<Renderer>();

        ApplyVisual(0f);
    }

    private void Update()
    {
        if (!isPulsing)
            return;

        pulseElapsed += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(pulseElapsed / pulseDuration);
        ApplyVisual(Mathf.Sin(normalizedTime * Mathf.PI));

        if (normalizedTime < 1f)
            return;

        isPulsing = false;
        ApplyVisual(0f);
    }

    public IEnumerator PlaySpawnCue()
    {
        pulseElapsed = 0f;
        isPulsing = true;
        ApplyVisual(0f);
        yield return new WaitForSeconds(spawnLeadTime);
    }

    private void ApplyVisual(float pulse)
    {
        if (barRenderer == null)
            return;

        EnsurePropertyBlock();
        barRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BarInactiveTint", inactiveColor);
        propertyBlock.SetColor("_BarActiveTint", activeColor);
        propertyBlock.SetColor("_BarGlowTint", activeColor);
        propertyBlock.SetFloat("_BarIdleGlow", idleEmission);
        propertyBlock.SetFloat("_BarPeakGlow", peakEmission);
        propertyBlock.SetFloat("_BarPulse", pulse);
        barRenderer.SetPropertyBlock(propertyBlock);
    }

    private void EnsurePropertyBlock()
    {
        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();
    }
}
