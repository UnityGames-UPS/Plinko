using UnityEngine;
using System.Collections.Generic;
namespace PlinkoGame
{
    /// <summary>
    /// Centralized audio management system for Plinko game
    /// Handles background music and SFX separately with independent toggle controls
    /// Works with Run in Background enabled/disabled
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Background Music")]
        [SerializeField] private AudioClip backgroundMusic;

        [Header("Button Sounds")]
        [SerializeField] private AudioClip playbuttonClickSound;
        [SerializeField] private AudioClip increaseBetSound;
        [SerializeField] private AudioClip dropdownSound;
        [SerializeField] private AudioClip otherbtnSound;
        [SerializeField] private AudioClip hoverbtnSound;

        [Header("Ball Sounds")]
        [SerializeField] private AudioClip ballSpawnSound;
        [SerializeField] private AudioClip ballCatchSound;

        [Header("Popup Sounds")]
        [SerializeField] private AudioClip errorSound;
        [SerializeField] private AudioClip winSound;

        [Header("Settings")]
        [SerializeField] private bool musicEnabledByDefault = true;
        [SerializeField] private bool sfxEnabledByDefault = true;
        [SerializeField] private float musicVolume = 0.5f;
        [SerializeField] private float sfxVolume = 1f;

        // State tracking
        private bool isMusicEnabled;
        private bool isSFXEnabled;
        private bool wasMusicPlayingBeforePause;
        private bool isApplicationFocused = true;
        private bool isBeingDestroyed = false;

        private bool isForceMuted = false;
        private readonly Dictionary<AudioSource, bool> preFocusMuteState = new Dictionary<AudioSource, bool>();

        // Focus-driven — called from BOTH OnFocusChanged (via UIManager) and OnApplicationFocus.
        internal void SetMuteAll(bool forceMute)
        {
            if (forceMute == isForceMuted) return;
            isForceMuted = forceMute;

            AudioSource[] sources = new AudioSource[] { musicSource, sfxSource };
            foreach (var source in sources)
            {
                if (source == null) continue;
                if (forceMute)
                {
                    preFocusMuteState[source] = source.mute;
                    source.mute = true;
                }
                else
                {
                    source.mute = preFocusMuteState.TryGetValue(source, out bool prevMuted) ? prevMuted : source.mute;
                }
            }
        }

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            InitializeAudioSources();
            LoadAudioSettings();
        }

        private void Start()
        {
            PlayBackgroundMusic();
            Debug.Log($"[AudioManager] Start - Music enabled: {isMusicEnabled}, Playing: {musicSource != null && musicSource.isPlaying}");
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        private void InitializeAudioSources()
        {
            // Create music source if not assigned
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
            }

            // Create SFX source if not assigned
            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFXSource");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
            }

            // Configure music source
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume;

            // Configure SFX source
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume;

            Debug.Log("[AudioManager] Audio sources initialized");
        }

        private void LoadAudioSettings()
        {
            // Always start fresh - no saved settings
            isMusicEnabled = musicEnabledByDefault;
            isSFXEnabled = sfxEnabledByDefault;

            if (musicSource != null) musicSource.mute = !isMusicEnabled;
            if (sfxSource != null) sfxSource.mute = !isSFXEnabled;

            Debug.Log($"[AudioManager] Fresh start - Music: {isMusicEnabled}, SFX: {isSFXEnabled}");
        }

        // ============================================
        // FOCUS HANDLING - Works with Run in Background
        // ============================================

        private void OnApplicationFocus(bool hasFocus)
        {
            if (isBeingDestroyed) return;

            isApplicationFocused = hasFocus;
            SetMuteAll(!hasFocus);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (isBeingDestroyed) return;
            SetMuteAll(pauseStatus);
        }

        private void StopAllSFX()
        {
            // Stop the main SFX source
            if (sfxSource != null && sfxSource.isPlaying)
            {
                sfxSource.Stop();
            }
        }

        // ============================================
        // BACKGROUND MUSIC CONTROL
        // ============================================

        public void PlayBackgroundMusic()
        {
            if (backgroundMusic == null || musicSource == null) return;
            // Only play if application is focused
            if (!isApplicationFocused)
            {
                Debug.Log("[AudioManager] Skipping music play - application not focused");
                return;
            }
            musicSource.clip = backgroundMusic;
            musicSource.Play();
            Debug.Log("[AudioManager] Background music started");
        }
        public void StopBackgroundMusic()
        {
            if (isBeingDestroyed || musicSource == null) return;

            try
            {
                if (musicSource != null && musicSource.isPlaying)
                {
                    musicSource.Stop();
                    Debug.Log("[AudioManager] Background music stopped");
                }
            }
            catch (MissingReferenceException) { }
        }

        public void ToggleMusic(bool enabled)
        {
            isMusicEnabled = enabled;
            isForceMuted = false;

            if (musicSource != null)
            {
                preFocusMuteState[musicSource] = !enabled;
                musicSource.mute = !enabled;
            }

            if (enabled)
            {
                PlayBackgroundMusic();
            }
            else
            {
                StopBackgroundMusic();
            }

            Debug.Log($"[AudioManager] Music toggled: {enabled}");
        }

        public bool GetMusicEnabledState()
        {
            return isMusicEnabled;
        }

        public bool GetSFXEnabledState()
        {
            return isSFXEnabled;
        }

        // ============================================
        // SFX CONTROL
        // ============================================

        public void ToggleSFX(bool enabled)
        {
            isSFXEnabled = enabled;
            isForceMuted = false;

            if (sfxSource != null)
            {
                preFocusMuteState[sfxSource] = !enabled;
                sfxSource.mute = !enabled;
            }

            Debug.Log($"[AudioManager] SFX toggled: {enabled}");
        }

        private void PlaySFX(AudioClip clip)
        {
            if (isBeingDestroyed || !isSFXEnabled || clip == null || sfxSource == null) return;

            // Only play SFX if application is focused
            if (!isApplicationFocused)
            {
                return;
            }

            sfxSource.PlayOneShot(clip);
        }

        // ============================================
        // BUTTON SOUNDS
        // ============================================

        public void PlayStartButtonClick()
        {
            PlaySFX(playbuttonClickSound);
        }

        public void PlayButtonClick()
        {
            PlaySFX(otherbtnSound);
        }

        public void PlayIncreaseBet()
        {
            PlaySFX(increaseBetSound ?? playbuttonClickSound);
        }

        public void PlayDecreaseBet()
        {
            PlaySFX(increaseBetSound ?? playbuttonClickSound);
        }

        public void PlayDropdownClick()
        {
            PlaySFX(dropdownSound ?? playbuttonClickSound);
        }

        public void PlayHoverSound()
        {
            PlaySFX(hoverbtnSound ?? otherbtnSound);
        }

        // ============================================
        // BALL SOUNDS
        // ============================================

        public void PlayBallSpawn()
        {
            PlaySFX(ballSpawnSound);
        }

        public void PlayBallCollision()
        {
            PlaySFX(ballSpawnSound);
        }

        public void PlayBallCatch()
        {
            PlaySFX(ballCatchSound);
        }

        // ============================================
        // POPUP SOUNDS
        // ============================================

        public void PlayErrorSound()
        {
            PlaySFX(errorSound);
        }

        public void PlayWinSound()
        {
            PlaySFX(winSound);
        }

        // ============================================
        // PUBLIC GETTERS
        // ============================================

        public bool IsMusicEnabled() => isMusicEnabled;
        public bool IsSFXEnabled() => isSFXEnabled;

        // ============================================
        // CLEANUP
        // ============================================

        private void OnDestroy()
        {
            isBeingDestroyed = true;

            if (Instance == this)
            {
                StopBackgroundMusic();
                musicSource = null;
                sfxSource = null;
                Instance = null;
            }
        }
    }
}