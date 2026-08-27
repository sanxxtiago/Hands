using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioLibrary audioLibrary;
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField] private AudioSource audioSource;

    private readonly Dictionary<AudioType, AudioSource> loopSources =
        new Dictionary<AudioType, AudioSource>();

    private Dictionary<AudioType, AudioDefinition> lookup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureAudioSource();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void StopAll()
    {
        if (audioSource != null)
            audioSource.Stop();

        foreach (KeyValuePair<AudioType, AudioSource> pair in loopSources)
        {
            if (pair.Value != null)
                pair.Value.Stop();
        }
    }

    // =========================
    // Fachada estática (null-safe)
    // =========================

    public static void Play(AudioType type)
    {
        if (!IsTypePlayable(type) || !TryGetDefinition(type, out AudioDefinition definition))
            return;

        Instance.EnsureAudioSource();
        Instance.audioSource.PlayOneShot(
            definition.Clip,
            definition.Volume * Instance.masterVolume);
    }

    public static void PlayLoop(AudioType type)
    {
        if (!IsTypePlayable(type) || !TryGetDefinition(type, out AudioDefinition definition))
            return;

        AudioSource loopSource = Instance.GetOrCreateLoopSource(type);

        // Idempotente: reiniciar el mismo loop no lo corta
        if (loopSource.clip == definition.Clip && loopSource.isPlaying)
            return;

        loopSource.clip = definition.Clip;
        loopSource.volume = definition.Volume * Instance.masterVolume;
        loopSource.Play();
    }

    public static void StopLoop(AudioType type)
    {
        if (Instance == null ||
            !Instance.loopSources.TryGetValue(type, out AudioSource loopSource) ||
            loopSource == null ||
            !loopSource.isPlaying)
        {
            return;
        }

        loopSource.Stop();
    }

    // =========================
    // Internos
    // =========================

    private static bool IsTypePlayable(AudioType type)
    {
        if (type == AudioType.None)
            return false;

        if (Instance == null)
        {
            Debug.LogWarning($"[Audio] No hay AudioManager activo para reproducir '{type}'.");
            return false;
        }

        return true;
    }

    private static bool TryGetDefinition(AudioType type, out AudioDefinition definition)
    {
        if (Instance.lookup == null)
            Instance.BuildLookup();

        return Instance.lookup.TryGetValue(type, out definition);
    }

    private void BuildLookup()
    {
        lookup = new Dictionary<AudioType, AudioDefinition>();

        if (audioLibrary == null)
        {
            Debug.LogWarning("[Audio] No hay AudioLibrary asignada.", this);
            return;
        }

        IReadOnlyList<AudioDefinition> sounds = audioLibrary.Sounds;

        for (int i = 0; i < sounds.Count; i++)
        {
            AudioDefinition sound = sounds[i];

            if (sound.Type == AudioType.None || sound.Clip == null)
                continue;

            lookup[sound.Type] = sound;
        }
    }

    private AudioSource GetOrCreateLoopSource(AudioType type)
    {
        if (loopSources.TryGetValue(type, out AudioSource loopSource) && loopSource != null)
            return loopSource;

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
        loopSource.loop = true;
        loopSources[type] = loopSource;

        return loopSource;
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }
}
