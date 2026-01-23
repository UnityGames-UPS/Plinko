using UnityEngine;

namespace PlinkoGame
{
    /// <summary>
    /// Centralized audio management system for Plinko game
    /// Handles background music and SFX separately with independent toggle controls
    /// WebGL-optimized with proper focus handling
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
        private bool wasPlayingBeforeFocusLost;
        private bool isInitialized;

        // WebGL focus detection
        private bool hadFocusLastFrame;

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

            isInitialized = true;
            hadFocusLastFrame = Application.isFocused;
        }

        private bool hasPlayedInitialMusic = false;

        private void Start()
        {
     
                PlayBackgroundMusic();
            

            Debug.Log($"[AudioManager] Start - Music enabled: {isMusicEnabled}, Playing: {musicSource != null && musicSource.isPlaying}");
        }



        private void Update()
        {
            HandleWebGLFocus();
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

            Debug.Log($"[AudioManager] Fresh start - Music: {isMusicEnabled}, SFX: {isSFXEnabled}");
        }

       

        // ============================================
        // WEBGL FOCUS HANDLING
        // ============================================

        private void HandleWebGLFocus()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            bool hasFocus = Application.isFocused;

            // Focus lost
            if (hadFocusLastFrame && !hasFocus)
            {
                OnFocusLost();
            }
            // Focus regained
            else if (!hadFocusLastFrame && hasFocus)
            {
                OnFocusRegained();
            }

            hadFocusLastFrame = hasFocus;
#endif
        }

        private void OnFocusLost()
        {
            wasPlayingBeforeFocusLost = musicSource.isPlaying;

            if (musicSource.isPlaying)
            {
                musicSource.Pause();
                Debug.Log("[AudioManager] Music paused - focus lost");
            }
        }

        private void OnFocusRegained()
        {
            if (wasPlayingBeforeFocusLost && isMusicEnabled && !musicSource.isPlaying)
            {
                musicSource.UnPause();
                Debug.Log("[AudioManager] Music resumed - focus regained");
            }
        }

        // ============================================
        // BACKGROUND MUSIC CONTROL
        // ============================================

        public void PlayBackgroundMusic()
        {
            if (backgroundMusic == null || musicSource == null) return;

            musicSource.clip = backgroundMusic;
            musicSource.Play();
            Debug.Log("[AudioManager] Background music started");
        }

        public void StopBackgroundMusic()
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Stop();
                Debug.Log("[AudioManager] Background music stopped");
            }
        }

        public void ToggleMusic(bool enabled)
        {
            isMusicEnabled = enabled;
           

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

        /// <summary>
        /// Get current music enabled state (for UI initialization)
        /// </summary>
        public bool GetMusicEnabledState()
        {
            return isMusicEnabled;
        }

        /// <summary>
        /// Get current SFX enabled state (for UI initialization)
        /// </summary>
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
          
            Debug.Log($"[AudioManager] SFX toggled: {enabled}");
        }

        private void PlaySFX(AudioClip clip)
        {
            if (!isSFXEnabled || clip == null || sfxSource == null) return;

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
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            // No saving needed
        }
    }
}