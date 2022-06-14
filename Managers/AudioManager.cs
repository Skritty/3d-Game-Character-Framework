// Written by: Trevor Thacker
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    //public AudioSettings settings;

    [SerializeField]
    private TMPro.TextMeshProUGUI subtitles;
    [SerializeField]
    private UnityEngine.UI.Image subsBackground;

    // FX
    [SerializeField]
    private AudioSource defaultSFXSource;
    private List<AudioSource> audioSources = new List<AudioSource>();
    public int initialAudioSourcePool = 4;

    // Music
    [SerializeField]
    private Queue<AudioSource> musicPlayers = new Queue<AudioSource>();
    private AudioTrack currentlyPlayingMusic;
    private bool fadingMusic = false;

    // Ambient
    [SerializeField]
    private Queue<AudioSource> ambiencePlayers = new Queue<AudioSource>();
    private AudioTrack currentlyPlayingAmbience;
    private bool fadingAmbience = false;

    private void Start()
    {
        for (int i = 0; i < initialAudioSourcePool; i++)
            CreateNewAudioSource();

        musicPlayers.Enqueue(gameObject.AddComponent<AudioSource>());
        musicPlayers.Enqueue(gameObject.AddComponent<AudioSource>());
        ambiencePlayers.Enqueue(gameObject.AddComponent<AudioSource>());
        ambiencePlayers.Enqueue(gameObject.AddComponent<AudioSource>());
        if (subtitles)
        {
            subtitles.text = "";
            subsBackground.gameObject.SetActive(false);
        }
    }

    public void ToggleAllSounds(bool paused)
    {
        if (paused)
        {
            if(musicPlayers.Count > 0)
                musicPlayers.Peek().Pause();
            if (ambiencePlayers.Count > 0)
                ambiencePlayers.Peek().Pause();
            foreach (AudioSource s in audioSources)
                s.Pause();
        }
        else
        {
            if (musicPlayers.Count > 0)
                musicPlayers.Peek()?.UnPause();
            if (ambiencePlayers.Count > 0)
                ambiencePlayers.Peek()?.UnPause();
            foreach (AudioSource s in audioSources)
                s?.UnPause();
        }
    }

    public void StopAllSounds()
    {
        PlayMusic(null, true);
        PlayAmbience(null, true);
        foreach (AudioSource s in audioSources)
        {
            s.Stop();
            s.clip = null;
        }
           
    }

    public void PlayMusic(AudioTrack track, bool noFade = false)
    {
        if(currentlyPlayingMusic != track)
            StartCoroutine(FadeMusic(currentlyPlayingMusic, track, noFade));
    }

    public void PlayAmbience(AudioTrack track, bool noFade = false)
    {
        if (currentlyPlayingAmbience != track)
            StartCoroutine(FadeAmbience(currentlyPlayingAmbience, track, noFade));
    }

    public AudioSource PlaySound(AudioClip clip, Vector3 position)
    {
        AudioSource source = GetPooledAudioSource();
        source.transform.position = position;
        source.clip = clip;
        source.Play();
        return source;
    }

    public AudioSource PlaySFX(SoundEffect SFX, Vector3 position)
    {
        AudioSource source = GetPooledAudioSource();
        if (!SFX) return source;
        source.transform.position = position;
        source.loop = SFX.loop;
        source.volume = SFX.volume;
        StartCoroutine(DoPlaySound(SFX, source));
        StartCoroutine(DoSubtitles(SFX, source));
        return source;
    }

    public AudioSource PlaySFXFollow(SoundEffect SFX, Transform position)
    {
        AudioSource source = GetPooledAudioSource();
        if (!SFX) return source;
        source.transform.position = position.position;
        source.loop = SFX.loop;
        source.volume = SFX.volume;
        StartCoroutine(DoPlaySound(SFX, source, position));
        StartCoroutine(DoSubtitles(SFX, source));
        StartCoroutine(DoFollowTransform(source, position));
        return source;
    }

    private IEnumerator DoFollowTransform(AudioSource source, Transform position)
    {
        while (source.isPlaying && position != null)
        {
            source.transform.position = position.position;
            yield return new WaitForEndOfFrame();
        }
    }
    private IEnumerator DoPlaySound(SoundEffect SFX, AudioSource source, Transform position = null)
    {
        if (SFX.initialDelay > 0)
            yield return new WaitForSeconds(SFX.initialDelay);
        if (SFX.chainSoundsInstead)
        {
            foreach (AudioClip c in SFX.sounds)
            {
                source.clip = c;
                source.pitch = Mathf.Pow(2f, Random.Range(SFX.pitchVariance.x, SFX.pitchVariance.y) * 12 / 12);
                source.Play();
                yield return new WaitWhile(() => source.isPlaying);
            }
        }
        else if(SFX.sounds.Length > 0)
        {
            source.clip = SFX.sounds[Random.Range(0, SFX.sounds.Length)];
            source.pitch = Mathf.Pow(2f, Random.Range(SFX.pitchVariance.x, SFX.pitchVariance.y) * 12 / 12);
            source.Play();
            yield return new WaitForEndOfFrame();
        }

        //if (SFX.loop)
        //{
        //    yield return new WaitWhile(() => source.isPlaying);
        //    if (position == null)
        //        position = source.transform;
        //    PlaySFXFollow(SFX, position);
        //}
    }

    private IEnumerator DoSubtitles(SoundEffect SFX, AudioSource source)
    {
        if(SFX.initialDelay > 0)
            yield return new WaitForSeconds(SFX.initialDelay);
        float maxDuration = 0;
        float currentTime = 0f;
        if (SFX.chainSoundsInstead)
        {
            // Figure out how long to keep the coroutine running for
            foreach (AudioClip c in SFX.sounds)
                maxDuration += c.length;
            foreach (Subtitles subs in SFX.subtitles)
                if (subs.startTime + subs.duration > maxDuration)
                    maxDuration = subs.startTime + subs.duration;
        }
        else
        {
            // Figure out how long to keep the coroutine running for
            if(source && source.clip)
                maxDuration = source.clip.length;
            foreach (Subtitles subs in SFX.subtitles)
                if (subs.startTime + subs.duration > maxDuration)
                    maxDuration = subs.startTime + subs.duration;
        }

        while (currentTime < maxDuration)
        {
            foreach (Subtitles subs in SFX.subtitles)
            {
                // Check to see if its time to play this subtitle
                if (subs.startTime > currentTime - Time.deltaTime && subs.startTime <= currentTime)
                {
                    if (subs.subtitles != "" && (subtitles.text == "" || subs.overrideOtherSubs))
                    {
                        subtitles.text = subs.subtitles;
                        subsBackground.gameObject.SetActive(true);
                    }
                }

                // Check to see if its time to end this subtitle
                if (subs.customDuration && subs.startTime + subs.duration > currentTime - Time.deltaTime && subs.startTime + subs.duration <= currentTime)
                {
                    if (subtitles.text == subs.subtitles)
                    {
                        subtitles.text = "";
                        subsBackground.gameObject.SetActive(false);
                    }
                }
            }
            yield return new WaitForEndOfFrame();
            currentTime += Time.deltaTime;
        }

        // Clear any active subtitles
        foreach (Subtitles subs in SFX.subtitles)
            if (subtitles.text == subs.subtitles)
            {
                subtitles.text = "";
                subsBackground.gameObject.SetActive(false);
            }
    }

    private IEnumerator FadeMusic(AudioTrack current, AudioTrack next, bool skipFades = false)
    {
        yield return new WaitWhile(() => fadingMusic);
        
        fadingMusic = true;

        AudioSource currentSource = musicPlayers.Dequeue();
        AudioSource nextSource = musicPlayers.Peek();
        musicPlayers.Enqueue(currentSource);

        // Play next
        currentlyPlayingMusic = next;

        float maxVolume = 1f;
        if (next != null)
        {
            nextSource.clip = next.song;
            nextSource.loop = next.loop;
            maxVolume = next.volume;
            nextSource.Play();
        }
        else
            nextSource.Stop();

        // Find rates
        float outRate = 0;
        float inRate = 0;
        if (current == null || current.fadeOutTime == 0 || skipFades)
            outRate = 10000f;
        else
            outRate = currentSource.volume / current.fadeOutTime;

        if (next == null || next.fadeOutTime == 0 || skipFades)
            inRate = 10000f;
        else
            inRate = maxVolume / next.fadeInTime;

        // Fade volumes
        nextSource.volume = 0f;
        while (nextSource.volume < maxVolume || currentSource.volume > 0)
        {
            if(currentSource.volume > 0)
                currentSource.volume = Mathf.Clamp01(currentSource.volume - outRate * Time.deltaTime);
            if (nextSource.volume < maxVolume)
                nextSource.volume = Mathf.Clamp01(nextSource.volume + inRate * Time.deltaTime);

            yield return new WaitForEndOfFrame();
        }

        currentSource.volume = 0f;
        nextSource.volume = maxVolume;

        fadingMusic = false;
    }

    private IEnumerator FadeAmbience(AudioTrack current, AudioTrack next, bool skipFades = false)
    {
        yield return new WaitWhile(() => fadingAmbience);

        fadingAmbience = true;

        AudioSource currentSource = ambiencePlayers.Dequeue();
        AudioSource nextSource = ambiencePlayers.Peek();
        ambiencePlayers.Enqueue(currentSource);

        // Play next
        currentlyPlayingAmbience = next;

        float maxVolume = 1f;
        if (next != null)
        {
            nextSource.clip = next.song;
            nextSource.loop = next.loop;
            maxVolume = next.volume;
            nextSource.Play();
        }
        else
            nextSource.Stop();

        // Find rates
        float outRate = 0;
        float inRate = 0;
        if (current == null || current.fadeOutTime == 0 || skipFades)
            outRate = 10000f;
        else
            outRate = currentSource.volume / current.fadeOutTime;

        if (next == null || next.fadeOutTime == 0 || skipFades)
            inRate = 10000f;
        else
            inRate = maxVolume / next.fadeInTime;

        // Fade volumes
        nextSource.volume = 0f;
        while (nextSource.volume < maxVolume || currentSource.volume > 0)
        {
            //Debug.Log("music");
            currentSource.volume = Mathf.Clamp01(currentSource.volume - outRate * Time.deltaTime);
            nextSource.volume = Mathf.Clamp01(nextSource.volume + inRate * Time.deltaTime);

            yield return new WaitForEndOfFrame();
        }

        fadingAmbience = false;
    }

    private AudioSource GetPooledAudioSource()
    {
        foreach (AudioSource s in audioSources)
        {
            if (!s.isPlaying)
            {
                return s;
            }
        }
        return CreateNewAudioSource();
    }

    private AudioSource CreateNewAudioSource()
    {
        //AudioSource source = (new GameObject()).AddComponent<AudioSource>();
        //source.spatialBlend = 1f;
        //source.rolloffMode = AudioRolloffMode.Linear;
        AudioSource source = Instantiate(defaultSFXSource.gameObject).GetComponent<AudioSource>();
        source.transform.parent = transform;
        audioSources.Add(source);
        return source;
    }
}
