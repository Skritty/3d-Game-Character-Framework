using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SFX", menuName = "Audio/SFX")]
public class SoundEffect : ScriptableObject
{
    [Tooltip("Causes sounds in the audio clip array to chain in order rather than picked randomly once")]
    public bool chainSoundsInstead = false;
    public AudioClip[] sounds;
    [Range(0, 1)]
    public float volume = 1;
    //[MinMaxSlider(-2f, 2f)]
    public Vector2 pitchVariance;
    public float initialDelay = 0f;
    public bool loop;
    public Subtitles[] subtitles;

    public void Play(Transform position)
    {
        if (AudioManager.Instance)
            AudioManager.Instance.PlaySFX(this, position.position);
        else
            Debug.LogWarning($"{name} SFX could not play because there was no Audio Manager in the scene!");
    }

    public void Play(Vector3 position)
    {
        if (AudioManager.Instance)
            AudioManager.Instance.PlaySFX(this, position);
        else
            Debug.LogWarning($"{name} SFX could not play because there was no Audio Manager in the scene!");
    }

    public void PlayFollowing(Transform position)
    {
        if (AudioManager.Instance)
            AudioManager.Instance.PlaySFXFollow(this, position);
        else
            Debug.LogWarning($"{name} SFX could not play because there was no Audio Manager in the scene!");
    }
}