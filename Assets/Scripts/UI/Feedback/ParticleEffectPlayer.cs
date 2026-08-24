using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEffectPlayer : MonoBehaviour
{
    [SerializeField] private bool playAsChild;
    [SerializeField, Min(0.1f)] private float fallbackDuration = 2f;

    private readonly List<GameObject> activeEffects = new List<GameObject>();

    public void Play(ParticleSystem effectPrefab, Vector3 position, Transform parent = null)
    {
        if (effectPrefab == null)
            return;

        ParticleSystem effect = playAsChild && parent != null
            ? Instantiate(effectPrefab, position, Quaternion.identity, parent)
            : Instantiate(effectPrefab, position, Quaternion.identity);

        GameObject effectObject = effect.gameObject;
        effectObject.SetActive(true);
        activeEffects.Add(effectObject);
        effect.Play(true);
        StartCoroutine(DestroyWhenFinished(effect, effectObject));
    }

    public void ClearEffects()
    {
        StopAllCoroutines();

        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (activeEffects[i] != null)
                Destroy(activeEffects[i]);
        }

        activeEffects.Clear();
    }

    private void OnDisable()
    {
        ClearEffects();
    }

    private IEnumerator DestroyWhenFinished(
        ParticleSystem effect,
        GameObject effectObject)
    {
        float elapsed = 0f;
        float maximumDuration = Mathf.Max(0.1f, fallbackDuration);

        while (effect != null && elapsed < maximumDuration)
        {
            if (!effect.IsAlive(true))
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (effect != null)
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (effectObject == null)
            yield break;

        activeEffects.Remove(effectObject);
        Destroy(effectObject);
    }
}
