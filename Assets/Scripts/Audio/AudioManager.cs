using UnityEngine;

public class AudioManager : MonoBehaviour {
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip musicClip;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.5f;

    [Header("Sound Effects")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip brickHitClip;
    [SerializeField] private AudioClip brickDestroyedClip;
    [SerializeField] private AudioClip wallHitClip;
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip ballShootClip;
    [SerializeField] private AudioClip ballOffScreenClip;

    [Header("Pitch Randomization")]
    [Range(0f, 0.3f)]
    [SerializeField] private float pitchVariation = 0.1f;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupAudioSources();
    }

    private void OnEnable() {
        GameEvents.OnBrickHit += HandleBrickHit;
        GameEvents.OnBrickDestroyed += HandleBrickDestroyed;
        GameEvents.OnWallHit += HandleWallHit;
        GameEvents.OnButtonClicked += HandleButtonClicked;
        GameEvents.OnBallShot += HandleBallShot;
        GameEvents.OnBallDestroyed += HandleBallDestroyed;
    }

    private void OnDisable() {
        GameEvents.OnBrickHit -= HandleBrickHit;
        GameEvents.OnBrickDestroyed -= HandleBrickDestroyed;
        GameEvents.OnWallHit -= HandleWallHit;
        GameEvents.OnButtonClicked -= HandleButtonClicked;
        GameEvents.OnBallShot -= HandleBallShot;
        GameEvents.OnBallDestroyed -= HandleBallDestroyed;
    }

    private void Start() {
        PlayMusic();
    }

    private void SetupAudioSources() {
        if (musicSource == null) {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
        }
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        if (sfxSource == null) {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
        }
        sfxSource.playOnAwake = false;
    }

    #region Music
    public void PlayMusic() {
        if (musicSource == null || musicClip == null)
            return;

        musicSource.clip = musicClip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic() {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void SetMusicVolume(float volume) {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float volume) {
        sfxVolume = Mathf.Clamp01(volume);
    }
    #endregion Music

    #region Sound Effects
    private void PlaySound(AudioClip clip) {
        if (sfxSource == null || clip == null)
            return;

        float randomPitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        sfxSource.pitch = randomPitch;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayButtonClick() {
        PlaySound(buttonClickClip);
    }
    #endregion Sound Effects

    #region Event Handlers
    private void HandleBrickHit(int brickId, int remainingHealth) {
        if (remainingHealth > 0) {
            PlaySound(brickHitClip);
        }
    }

    private void HandleBrickDestroyed(int brickId) {
        PlaySound(brickDestroyedClip);
    }

    private void HandleWallHit() {
        PlaySound(wallHitClip);
    }

    private void HandleButtonClicked() {
        PlaySound(buttonClickClip);
    }

    private void HandleBallShot() {
        PlaySound(ballShootClip);
    }

    private void HandleBallDestroyed() {
        PlaySound(ballOffScreenClip);
    }
    #endregion Event Handlers
}