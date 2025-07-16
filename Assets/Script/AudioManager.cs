using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : Singleton<AudioManager>
{
    [Header("Music Settings")]
    [SerializeField] private List<AudioClip> lobbyMusic;
    [SerializeField] private AudioClip gameMusicClip;
    [SerializeField] private float musicFadeDuration = 1.5f;

    [Header("SFX")]
    [SerializeField] private AudioSource buttonClickSound;
    [SerializeField] private AudioSource collectSound;

    private AudioSource _musicSource;
    private AudioSource _gameMusicSource;
    private int _currentLobbyTrackIndex;
    private bool _isLobbyMusicActive = true;
    private Coroutine _activeTransition;

    protected override void Awake()
    {
        base.Awake();
        InitializeAudioSources();
    }

    private void InitializeAudioSources()
    {
        _musicSource = gameObject.AddComponent<AudioSource>();
        _gameMusicSource = gameObject.AddComponent<AudioSource>();
        
        _musicSource.loop = false;
        _gameMusicSource.loop = true;
        
        PlayLobbyMusic();
    }

    public void PlayButtonClickSound()
    {
        if (buttonClickSound != null)
        {
            buttonClickSound.Play();
        }
    }

    public void PlayCollectSound()
    {
        if (collectSound != null)
        {
            collectSound.Play();
        }
    }

    private void PlayLobbyMusic()
    {
        if (lobbyMusic.Count == 0) return;

        _isLobbyMusicActive = true;
        _musicSource.clip = lobbyMusic[_currentLobbyTrackIndex];
        _musicSource.Play();
    }

    private void PlayNextLobbyTrack()
    {
        _currentLobbyTrackIndex = (_currentLobbyTrackIndex + 1) % lobbyMusic.Count;
        _musicSource.clip = lobbyMusic[_currentLobbyTrackIndex];
        _musicSource.Play();
    }

    public void SwitchToGameMusic()
    {
        if (!_isLobbyMusicActive) return;
        if (_activeTransition != null) StopCoroutine(_activeTransition);
        _activeTransition = StartCoroutine(TransitionToGameMusic());
    }

    private IEnumerator TransitionToGameMusic()
    {
        yield return StartCoroutine(FadeOut(_musicSource, musicFadeDuration));
        
        _isLobbyMusicActive = false;
        _gameMusicSource.clip = gameMusicClip;
        _gameMusicSource.Play();
        yield return StartCoroutine(FadeIn(_gameMusicSource, musicFadeDuration));
    }

    private IEnumerator FadeOut(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        while (source.volume > 0)
        {
            source.volume -= startVolume * Time.deltaTime / duration;
            yield return null;
        }
        source.Stop();
        source.volume = startVolume;
    }

    private IEnumerator FadeIn(AudioSource source, float duration)
    {
        float targetVolume = source.volume;
        source.volume = 0;
        source.Play();
        
        while (source.volume < targetVolume)
        {
            source.volume += targetVolume * Time.deltaTime / duration;
            yield return null;
        }
    }

    public void ReturnToLobbyMusic()
    {
        if (_isLobbyMusicActive) return;
        if (_activeTransition != null) StopCoroutine(_activeTransition);
        _activeTransition = StartCoroutine(TransitionToLobbyMusic());
    }

    private IEnumerator TransitionToLobbyMusic()
    {
        yield return StartCoroutine(FadeOut(_gameMusicSource, musicFadeDuration));
        
        _isLobbyMusicActive = true;
        PlayLobbyMusic();
    }

    private void Update()
    {
        if (_isLobbyMusicActive && !_musicSource.isPlaying)
        {
            PlayNextLobbyTrack();
        }
    }

    // GameState integration
    public void HandleGameStateChange(GameState newState)
    {
        switch (newState)
        {
            case GameState.Playing:
                SwitchToGameMusic();
                break;
            case GameState.NameInput:
            case GameState.BallSelection:
                ReturnToLobbyMusic();
                break;
        }
    }
}