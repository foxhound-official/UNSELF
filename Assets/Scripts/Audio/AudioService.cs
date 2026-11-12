using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioService : MonoBehaviour
{
    public static AudioService Instance { get; private set; }

    [SerializeField]
    private SoundLibrary soundLibrary;

    private AudioSource audioSource;

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

        // Отдельные каналы позволяют менять тон шагов независимо от остальных звуков.
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

    public static void Play(string soundId, float volumeMultiplier = 1f)
    {
        if (Instance == null)
        {
            Debug.LogWarning($"AudioService is not available. Cannot play sound '{soundId}'.");
            return;
        }

        Instance.PlayInternal(soundId, volumeMultiplier);
    }

    private void PlayInternal(string soundId, float volumeMultiplier)
    {
        if (!soundLibrary.TryGetSound(soundId, out AudioClip clip, out float volume))
        {
            Debug.LogWarning($"Sound '{soundId}' was not found in SoundLibrary.");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning($"Sound '{soundId}' has no AudioClip.");
            return;
        }

        audioSource.PlayOneShot(clip, volume * Mathf.Clamp01(volumeMultiplier));
    }

    public static void PlayFootstep(string soundId, float volumeMultiplier, float pitch)
    {
        if (Instance == null)
            return;

        Instance.PlayFootstepInternal(soundId, volumeMultiplier, pitch);
    }

    private void PlayFootstepInternal(string soundId, float volumeMultiplier, float pitch)
    {
        if (soundLibrary == null || !soundLibrary.TryGetSound(soundId, out AudioClip clip, out float volume))
            return;

        if (clip == null)
            return;

        AudioSource source = footstepSources[nextFootstepSource];
        nextFootstepSource = (nextFootstepSource + 1) % footstepSources.Length;

        source.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
        source.PlayOneShot(clip, volume * Mathf.Clamp01(volumeMultiplier));
    }
}