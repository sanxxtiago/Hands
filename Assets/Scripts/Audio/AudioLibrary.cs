using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioLibrary", menuName = "Hands/Audio Library")]
public class AudioLibrary : ScriptableObject
{
    [SerializeField] private List<AudioDefinition> sounds = new();

    public IReadOnlyList<AudioDefinition> Sounds => sounds;

    public bool TryGet(AudioType type, out AudioDefinition definition)
    {
        foreach (AudioDefinition sound in sounds)
        {
            if (sound.Type == type && sound.Clip != null)
            {
                definition = sound;
                return true;
            }
        }

        definition = default;
        return false;
    }

    private void OnValidate()
    {
        HashSet<AudioType> configuredTypes = new();

        foreach (AudioDefinition sound in sounds)
        {
            if (sound.Type == AudioType.None)
            {
                Debug.LogWarning($"{name}: AudioType.None no debe configurarse en la librería.", this);
                continue;
            }

            if (!configuredTypes.Add(sound.Type))
                Debug.LogWarning($"{name}: AudioType duplicado: {sound.Type}.", this);
        }
    }
}

[Serializable]
public struct AudioDefinition
{
    [SerializeField] private AudioType type;
    [SerializeField] private AudioClip clip;
    [SerializeField, Range(0f, 1f)] private float volume;

    public AudioType Type => type;
    public AudioClip Clip => clip;
    public float Volume => volume;
}
