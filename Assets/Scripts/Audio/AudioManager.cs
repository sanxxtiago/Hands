using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioLibrary audioLibrary;
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField] private AudioSource audioSource;

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

    public void Play(AudioType type)
    {
        if (type == AudioType.None)
            return;

        if (audioLibrary == null)
        {
            Debug.LogWarning("AudioManager: no hay AudioLibrary asignada.", this);
            return;
        }

        if (!audioLibrary.TryGet(type, out AudioDefinition sound))
        {
            Debug.LogWarning($"AudioManager: no hay clip configurado para {type}.", this);
            return;
        }

        EnsureAudioSource();
        audioSource.PlayOneShot(sound.Clip, sound.Volume * masterVolume);
    }

    public void Stop()
    {
        if (audioSource != null)
            audioSource.Stop();
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
