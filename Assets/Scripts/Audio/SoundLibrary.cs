using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SoundLibrary",
    menuName = "UNSELF/Audio/Sound Library"
)]
public class SoundLibrary : ScriptableObject
{
    [Serializable]
    private class SoundEntry
    {
        public string id;
        public List<AudioClip> clips = new();

        [Range(0f, 1f)]
        public float volume = 1f;
    }

    [SerializeField]
    private List<SoundEntry> sounds = new();

    public bool TryGetSound(string id, out AudioClip clip, out float volume)
    {
        foreach (SoundEntry sound in sounds)
        {
            if (sound.id != id)
                continue;

            if (sound.clips == null || sound.clips.Count == 0)
            {
                clip = null;
                volume = sound.volume;
                return true;
            }

            clip = sound.clips[UnityEngine.Random.Range(0, sound.clips.Count)];
            volume = sound.volume;
            return true;
        }

        clip = null;
        volume = 1f;
        return false;
    }
}