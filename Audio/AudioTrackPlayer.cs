using System.Collections;
using UnityEngine;

public class AudioTrackPlayer : MonoBehaviour
{
    // Each audio track on the game object will fade-in at the assigned rate.
    // NOTE : tracks must be set to play on awake.

    [Tooltip("Desired time of the fade-in.")]
    [Range(1.0f, 16.0f)]
    [SerializeField] private float fadeLength = 8.0f;

    private void OnEnable()
    {
        foreach(AudioSource track in GetComponents<AudioSource>())
        {
            StartCoroutine(BeginTrackPlayback(track));
        }
    }

    private IEnumerator BeginTrackPlayback(AudioSource track)
    {
        float targetVolume = track.volume;

        float time_Counter = 0f;
        while(time_Counter < fadeLength)
        {
            track.volume = targetVolume * (time_Counter / fadeLength);
            time_Counter += Time.deltaTime;
            yield return null;
        }

        track.volume = targetVolume;
    }
}