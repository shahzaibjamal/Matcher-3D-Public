using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private SoundLibrary library;

    private bool _isUsingSourceA = true;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    // Call this when your Loading Screen starts
    public void FadeOutMusic(float duration)
    {
        StartCoroutine(FadeVolume(ActiveSource(), 0, duration));
    }

    // Call this when the Loading Screen finishes
    public void TransitionToMusic(AudioClip newClip, float fadeTime = 1.0f)
    {
        StartCoroutine(Crossfade(newClip, fadeTime));
    }

    private IEnumerator Crossfade(AudioClip nextClip, float time)
    {
        AudioSource offSource = _isUsingSourceA ? musicSourceB : musicSourceA;
        AudioSource onSource = _isUsingSourceA ? musicSourceA : musicSourceB;

        offSource.clip = nextClip;
        offSource.volume = 0;
        offSource.Play();

        float t = 0;
        while (t < time)
        {
            t += Time.deltaTime;
            offSource.volume = t / time;
            onSource.volume = 1 - (t / time);
            yield return null;
        }

        onSource.Stop();
        _isUsingSourceA = !_isUsingSourceA;
    }

    private IEnumerator FadeVolume(AudioSource source, float target, float time)
    {
        float start = source.volume;
        float t = 0;
        while (t < time)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(start, target, t / time);
            yield return null;
        }
    }

    private AudioSource ActiveSource() => _isUsingSourceA ? musicSourceA : musicSourceB;

    public void PlaySFX(string sfxName)
    {
        var sfx = library.GetSFX(sfxName);
        if (sfx.Clip != null) sfxSource.PlayOneShot(sfx.Clip, sfx.Volume);
    }
}