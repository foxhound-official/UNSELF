using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Управляет воспроизведением музыки и плавными переходами между композициями.
/// Сохраняется при смене сцен.
/// </summary>
public class MusicService : MonoBehaviour
{
    public static MusicService Instance { get; private set; }

    [Header("Music Library")]
    [SerializeField] private MusicLibrary musicLibrary;

    [Header("Audio")]
    [SerializeField] private AudioMixerGroup musicOutput;
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;
    [SerializeField, Min(0f)] private float fadeDuration = 1f;

    private readonly AudioSource[] sources = new AudioSource[2];

    private int currentSource;
    private Coroutine transition;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.outputAudioMixerGroup = musicOutput;

            sources[i] = source;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Play(string trackId)
    {
        if (musicLibrary == null || !musicLibrary.TryGetTrack(trackId, out AudioClip clip, out bool loop, out float trackVolume))
        {
            Debug.LogWarning($"Music track '{trackId}' was not found in MusicLibrary.");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning($"Music track '{trackId}' has no AudioClip.");
            return;
        }

        Play(clip, loop, trackVolume);
    }

    public void Play(AudioClip clip, bool loop = true, float trackVolume = 1f)
    {
        if (clip == null)
            return;

        CancelTransition();

        AudioSource current = sources[currentSource];
        AudioSource previous = sources[1 - currentSource];
        float targetVolume = volume * Mathf.Clamp01(trackVolume);

        // Если композиция уже играет, восстанавливаем её громкость без перезапуска.
        if (current.isPlaying && current.clip == clip)
        {
            current.loop = loop;

            if (!previous.isPlaying && Mathf.Approximately(current.volume, targetVolume))
                return;

            transition = StartCoroutine(Crossfade(previous, current, targetVolume));
            return;
        }

        // Подготавливаем свободный канал для следующей композиции.
        previous.Stop();
        previous.clip = clip;
        previous.loop = loop;
        previous.volume = 0f;
        previous.Play();

        currentSource = 1 - currentSource;
        transition = StartCoroutine(Crossfade(current, previous, targetVolume));
    }

    public void Stop()
    {
        CancelTransition();

        if (!sources[0].isPlaying && !sources[1].isPlaying)
            return;

        transition = StartCoroutine(FadeOut());
    }

    private IEnumerator Crossfade(AudioSource outgoing, AudioSource incoming, float targetVolume)
    {
        float outgoingVolume = outgoing.volume;
        float incomingVolume = incoming.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeDuration);

            outgoing.volume = Mathf.Lerp(outgoingVolume, 0f, progress);
            incoming.volume = Mathf.Lerp(incomingVolume, targetVolume, progress);

            yield return null;
        }

        outgoing.Stop();
        outgoing.clip = null;
        outgoing.volume = 0f;

        incoming.volume = targetVolume;
        transition = null;
    }

    private IEnumerator FadeOut()
    {
        float firstVolume = sources[0].volume;
        float secondVolume = sources[1].volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeDuration);

            sources[0].volume = Mathf.Lerp(firstVolume, 0f, progress);
            sources[1].volume = Mathf.Lerp(secondVolume, 0f, progress);

            yield return null;
        }

        foreach (AudioSource source in sources)
        {
            source.Stop();
            source.clip = null;
            source.volume = 0f;
        }

        transition = null;
    }

    private void CancelTransition()
    {
        if (transition == null)
            return;

        StopCoroutine(transition);
        transition = null;
    }
}