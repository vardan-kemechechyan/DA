using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using System;

public class SoundManager : MonoBehaviour
{
    public AudioSource soundSource;
    public AudioSource musicSource;

    public AudioClip[] sounds;
    public AudioClip[] musicTracks;

    public static bool music;
    public static bool sound;

    [SerializeField] float musicVolume = 0.25f;
    [SerializeField] float musicFadeSpeed = 1.0f;

    bool isMusicFading;
    float targetFadeValue;

    private AnalyticEvents _analyticEvents;

    public int currentTrack;

    DateTime breakSound;

    [SerializeField] int nextTrackCount;

    public bool IsMusicPlaying()
    {
        if (musicSource.isPlaying)
            return true;

        return false;
    }

    public bool IsSoundPlaying(int sound)
    {
        if (soundSource.clip == sounds[sound] && soundSource.isPlaying)
            return true;

        return false;
    }

    public void Initialize()
    {
        if (!PlayerPrefs.HasKey("sound"))
            PlayerPrefs.SetInt("sound", 1);

        if (!PlayerPrefs.HasKey("music"))
            PlayerPrefs.SetInt("music", 1);

        PlayerPrefs.Save();

        LoadSettings();

        MuteSources();

        var random = new System.Random();

        currentTrack = random.Next(0, musicTracks.Length);
    }

    private void Update()
    {
        soundSource.pitch = Time.timeScale;

        if (isMusicFading) 
        {
            musicSource.volume = Mathf.Lerp(musicSource.volume, targetFadeValue, musicFadeSpeed * Time.unscaledDeltaTime);

            if (targetFadeValue <= 0)
            {
                if (musicSource.volume <= 0)
                {
                    if (musicSource.isPlaying)
                    {
                        musicSource.Stop();
                        isMusicFading = false;
                    }
                }
            }
            else 
            {
                if (musicSource.volume >= targetFadeValue)
                {
                    isMusicFading = false;
                }
            }
        }
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt("sound", Convert.ToInt32(sound));
        PlayerPrefs.SetInt("music", Convert.ToInt32(music));
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        sound = Convert.ToBoolean(PlayerPrefs.GetInt("sound"));
        music = Convert.ToBoolean(PlayerPrefs.GetInt("music"));

        soundSource.mute = !sound;

        foreach (var m in musicTracks)
        {
            musicSource.mute = !music;
        }
    }

    public void ToggleSound()
    {
        sound = !sound;

        if (sound)
        {
            AnalyticEvents.ReportEvent("sounds_on");
        }
        else
        {
            AnalyticEvents.ReportEvent("sounds_off");
        }

        SaveSettings();
        LoadSettings();

        MuteSources();
    }

    private void MuteSources() 
    {
        soundSource.mute = !sound;
        musicSource.mute = sound ? !music : true;
    }

    public void ToggleMusic()
    {
        music = !music;

        if (music)
        {
            AnalyticEvents.ReportEvent("music_on");
        }
        else
        {
            AnalyticEvents.ReportEvent("music_off");
        }

        musicSource.mute = sound ? !music : true;

        SaveSettings();
        LoadSettings();

        MuteSources();
    }

    public void PlayMusic(AudioClip track)
    {
        if (musicSource.isPlaying && musicSource.clip.Equals(track))
            return;

        musicSource.clip = track;
        musicSource.Play();

        musicSource.volume = 0;
        targetFadeValue = musicVolume;
        isMusicFading = true;
    }

    public void StopMusic()
    {
        musicSource.clip = null;
        targetFadeValue = 0;
        isMusicFading = true;
    }

    public void PlaySound(int sound)
    {
        soundSource.clip = sounds[sound];
        soundSource.Play();
    }

    public void PlaySound(int sound, float pitch)
    {
        soundSource.clip = sounds[sound];
        soundSource.pitch = pitch;
        soundSource.Play();
    }

    int delayedSound;

    public async void PlaySound(float delay, int sound)
    {
        await Task.Delay(Mathf.CeilToInt(delay * 1000));
        PlaySound(sound);
    }

    public void StopSound()
    {
        soundSource.Stop();
    }

    public void StopSound(int sound)
    {
        StopSound();
    }
}