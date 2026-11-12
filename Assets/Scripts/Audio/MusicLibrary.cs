using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MusicLibrary", menuName = "UNSELF/Audio/Music Library")]
public class MusicLibrary : ScriptableObject
{
    [Serializable]
    private class MusicEntry
    {
        public string id;
        public AudioClip clip;
        public bool loop = true;

        [Range(0f, 1f)]
        public float volume = 1f;
    }

    [SerializeField] private List<MusicEntry> tracks = new();

    public bool TryGetTrack(string id, out AudioClip clip, out bool loop, out float volume)
    {
        foreach (MusicEntry track in tracks)
        {
            if (track.id != id)
                continue;

            clip = track.clip;
            loop = track.loop;
            volume = track.volume;
            return true;
        }

        clip = null;
        loop = false;
        volume = 1f;
        return false;
    }
}