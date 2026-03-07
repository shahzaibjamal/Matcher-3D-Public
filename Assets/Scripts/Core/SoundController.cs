using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class SoundController : MonoBehaviour
{
    public static SoundController instance;

    [Header("References")]
    public GameObject AudioListenerObject;
    public SoundLibrary library;

    [Header("Settings")]
    [SerializeField] private int sfxSlotCount = 10;

    private Dictionary<string, SoundEffect> _soundsMap;
    private AudioSource[] audioSourceSlots;
    private int audioSlotIndex = -1;

    private AudioSource _bgmSourceA;
    private AudioSource _bgmSourceB;
    private bool _isSourceAActive = true;

    private bool _isMusicEnabled = true;
    private bool _isSfxEnabled = true;
    private string _lastPlayedBgmName;

    private float _resumeTime = 0f;
    void Awake()
    {
        instance = this;

        _soundsMap = new Dictionary<string, SoundEffect>();
        if (library != null)
        {
            foreach (var sfx in library.sfxList)
            {
                if (!_soundsMap.ContainsKey(sfx.Name))
                    _soundsMap.Add(sfx.Name, sfx);
            }
        }

        audioSourceSlots = new AudioSource[sfxSlotCount];
        for (int i = 0; i < sfxSlotCount; i++)
        {
            audioSourceSlots[i] = AudioListenerObject.AddComponent<AudioSource>();
        }

        _bgmSourceA = AudioListenerObject.AddComponent<AudioSource>();
        _bgmSourceB = AudioListenerObject.AddComponent<AudioSource>();
        _bgmSourceA.loop = true;
        _bgmSourceB.loop = true;
    }

    /// <summary>
    /// Call this when the game starts or when your settings data is loaded.
    /// </summary>
    public void Init(bool musicEnabled, bool sfxEnabled)
    {
        _isMusicEnabled = musicEnabled;
        _isSfxEnabled = sfxEnabled;

        // If music is disabled at start, make sure sources are silent
        if (!_isMusicEnabled)
        {
            _bgmSourceA.volume = 0;
            _bgmSourceB.volume = 0;
        }
    }

    #region Toggle Logic

    public void ToggleMusic(bool isEnabled)
    {
        _isMusicEnabled = isEnabled;
        _bgmSourceA.DOKill();
        _bgmSourceB.DOKill();

        if (!_isMusicEnabled)
        {
            // 1. Capture the current time from the active source before fading/stopping
            AudioSource activeSource = _isSourceAActive ? _bgmSourceA : _bgmSourceB;
            if (activeSource.isPlaying)
            {
                _resumeTime = activeSource.time;
            }

            _bgmSourceA.DOFade(0, 0.4f).OnComplete(() => _bgmSourceA.Stop());
            _bgmSourceB.DOFade(0, 0.4f).OnComplete(() => _bgmSourceB.Stop());
        }
        else
        {
            if (!string.IsNullOrEmpty(_lastPlayedBgmName))
            {
                // 2. Play the BGM and pass the resume time
                PlayBGM(_lastPlayedBgmName, 1.5f, -1f, _resumeTime);
            }
        }
    }

    public void ToggleSfx(bool isEnabled)
    {
        _isSfxEnabled = isEnabled;

        if (!_isSfxEnabled)
        {
            // Kill all currently playing SFX immediately
            foreach (var source in audioSourceSlots)
            {
                source.Stop();
            }
        }
    }

    #endregion

    #region SFX Logic

    public AudioSource PlaySoundEffect(string name, bool loop = false, float volume = -1f)
    {
        if (!_isSfxEnabled) return null;

        SoundEffect soundEffect = GetSoundByName(name);
        if (soundEffect == null || soundEffect.Clip == null) return null;

        AudioSource source = _GetAudioSource();
        source.Stop();
        source.clip = soundEffect.Clip;
        source.loop = loop;

        float soundVolume = volume == -1.0f ? soundEffect.Volume : volume;
        source.volume = soundVolume;
        source.Play();

        return source;
    }

    private AudioSource _GetAudioSource()
    {
        audioSlotIndex++;
        if (audioSlotIndex >= audioSourceSlots.Length)
            audioSlotIndex = 0;
        return audioSourceSlots[audioSlotIndex];
    }

    #endregion

    #region BGM Logic

    public void PlayBGM(string name, float fadeDuration = 1.5f, float targetVolume = -1.0f, float startAt = 0f)
    {
        _lastPlayedBgmName = name;

        if (!_isMusicEnabled) return;

        SoundEffect soundEffect = GetSoundByName(name);
        if (soundEffect == null || soundEffect.Clip == null) return;

        AudioSource activeSource = _isSourceAActive ? _bgmSourceA : _bgmSourceB;
        AudioSource newSource = _isSourceAActive ? _bgmSourceB : _bgmSourceA;
        AudioClip clip = soundEffect.Clip;

        if (activeSource.clip == clip && activeSource.isPlaying) return;

        newSource.clip = clip;
        newSource.volume = 0;

        // Set the playback position before hitting Play
        newSource.time = startAt % clip.length;
        newSource.Play();

        float volume = targetVolume == -1.0f ? soundEffect.Volume : targetVolume;

        newSource.DOFade(volume, fadeDuration);
        activeSource.DOFade(0, fadeDuration).OnComplete(() =>
        {
            activeSource.Stop();
            _resumeTime = 0; // Reset after a successful transition
        });

        _isSourceAActive = !_isSourceAActive;
    }

    #endregion

    public SoundEffect GetSoundByName(string name)
    {
        if (!string.IsNullOrEmpty(name) && _soundsMap.ContainsKey(name))
            return _soundsMap[name];
        return null;
    }

    public void StopAllSounds()
    {
        foreach (var source in audioSourceSlots) source.Stop();
        _bgmSourceA.Stop();
        _bgmSourceB.Stop();
    }
}