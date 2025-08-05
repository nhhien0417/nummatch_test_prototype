using System;
using UnityEngine;

[Serializable]
public class Sound
{
    public string Name;
    public AudioClip Clip;
}

public class AudioManager : SingletonPersistent<AudioManager>
{
    [SerializeField] private Sound[] _sfx;
    [SerializeField] private AudioSource _sfxSource;

    // Plays a sound effect by name with an optional volume.
    public void PlaySFX(string name, float volume = 1)
    {
        _sfxSource.volume = volume;

        var s = Array.Find(_sfx, s => s.Name == name);
        if (s != null)
        {
            _sfxSource.PlayOneShot(s.Clip);
        }
    }
}
