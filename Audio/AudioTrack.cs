using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Song", menuName = "Audio/Audio Track")]
public class AudioTrack : ScriptableObject
{
    public AudioClip song;
    [Range(0,1)]
    public float volume = 1;
    public bool loop;
    public float fadeInTime;
    public float fadeOutTime;

    public void PlayMusic()
    {
        if (AudioManager.Instance)
            AudioManager.Instance.PlayMusic(this);
        else
            Debug.LogWarning($"{name} music track could not play because there was no Audio Manager in the scene!");
    }

    public void PlayAmbience()
    {
        if (AudioManager.Instance)
            AudioManager.Instance.PlayAmbience(this);
        else
            Debug.LogWarning($"{name} ambience track could not play because there was no Audio Manager in the scene!");
    }
}
