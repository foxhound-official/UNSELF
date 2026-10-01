using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioService : MonoBehaviour
{
    public static AudioService Instance { get; private set; }

    [SerializeField]
    private SoundLibrary soundLibrary;

    private AudioSource audioSource;

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
    }

    public static void Play(string soundId)
    {
        if (Instance == null)
        {
            Debug.LogWarning(
                $"AudioService is not available. " +
                $"Cannot play sound '{soundId}'."
            );

            return;
        }

        Instance.PlayInternal(soundId);
    }

    private void PlayInternal(string soundId)
    {
        if (!soundLibrary.TryGetSound(
                soundId,
                out AudioClip clip,
                out float volume))
        {
            Debug.LogWarning(
                $"Sound '{soundId}' was not found " +
                $"in SoundLibrary."
            );

            return;
        }

        if (clip == null)
        {
            Debug.LogWarning(
                $"Sound '{soundId}' has no AudioClip."
            );

            return;
        }

        audioSource.PlayOneShot(
            clip,
            volume
        );
    }
}