using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class AudioService : MonoBehaviour
{
    public static AudioService Instance { get; private set; }

    [SerializeField] private SoundLibrary soundLibrary;

    private AudioSource audioSource;
    private readonly Dictionary<AudioMixerGroup, AudioSource> sources = new();

    private AudioSource[] footstepSources;
    private int nextFootstepSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        // Для шагов сохраняем отдельные источники с независимой настройкой Pitch.
        footstepSources = new AudioSource[2];

        for (int i = 0; i < footstepSources.Length; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;

            footstepSources[i] = source;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void Play(string soundId, float volumeMultiplier = 1f)
    {
        if (Instance == null)
        {
            Debug.LogWarning($"AudioService is not available. Cannot play sound '{soundId}'.");
            return;
        }

        Instance.PlayInternal(soundId, volumeMultiplier);
    }

    public static void PlayUI(string soundId)
    {
        Play(soundId);
    }

    private void PlayInternal(string soundId, float volumeMultiplier)
    {
        if (soundLibrary == null || !soundLibrary.TryGetSound(soundId, out AudioClip clip, out float volume, out AudioMixerGroup output))
        {
            Debug.LogWarning($"Sound '{soundId}' was not found in SoundLibrary.");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning($"Sound '{soundId}' has no AudioClip.");
            return;
        }

        AudioSource source = GetSource(output);
        source.PlayOneShot(clip, volume * Mathf.Clamp01(volumeMultiplier));
    }

    private AudioSource GetSource(AudioMixerGroup output)
    {
        // Используем основной источник для звуков без группы и общих SFX.
        if (output == null || output == audioSource.outputAudioMixerGroup)
            return audioSource;

        if (sources.TryGetValue(output, out AudioSource source))
            return source;

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = output;

        sources.Add(output, source);
        return source;
    }

    public static void PlayFootstep(string soundId, float volumeMultiplier, float pitch)
    {
        if (Instance == null)
            return;

        Instance.PlayFootstepInternal(soundId, volumeMultiplier, pitch);
    }

    private void PlayFootstepInternal(string soundId, float volumeMultiplier, float pitch)
    {
        if (soundLibrary == null || !soundLibrary.TryGetSound(soundId, out AudioClip clip, out float volume, out AudioMixerGroup output))
            return;

        if (clip == null)
            return;

        AudioSource source = footstepSources[nextFootstepSource];
        nextFootstepSource = (nextFootstepSource + 1) % footstepSources.Length;

        source.outputAudioMixerGroup = output != null ? output : audioSource.outputAudioMixerGroup;
        source.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
        source.PlayOneShot(clip, volume * Mathf.Clamp01(volumeMultiplier));
    }
}