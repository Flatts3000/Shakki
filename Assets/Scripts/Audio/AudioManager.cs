using UnityEngine;
using System.Collections.Generic;

namespace Shakki.Audio
{
    /// <summary>
    /// Manages game audio: music and sound effects.
    /// Provides simple API for playing sounds from anywhere in the game.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float musicVolume = 0.7f;
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private int sfxPoolSize = 10;

        private AudioSource musicSource;
        private List<AudioSource> sfxPool;
        private int currentSfxIndex;

        private static AudioManager instance;
        public static AudioManager Instance => instance;

        // Sound effect types for easy access
        public enum SoundEffect
        {
            PieceMove,
            PieceCapture,
            Check,
            Checkmate,
            ButtonClick,
            CoinEarn,
            LevelUp,
            MatchWin,
            MatchLose,
            ShopOpen,
            ShopPurchase
        }

        private Dictionary<SoundEffect, AudioClip> sfxClips = new Dictionary<SoundEffect, AudioClip>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            CreateAudioSources();
            LoadDefaultSounds();
        }

        private void CreateAudioSources()
        {
            // Music source
            var musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume * masterVolume;

            // SFX pool
            sfxPool = new List<AudioSource>();
            for (int i = 0; i < sfxPoolSize; i++)
            {
                var sfxObj = new GameObject($"SFXSource_{i}");
                sfxObj.transform.SetParent(transform);
                var source = sfxObj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                sfxPool.Add(source);
            }
        }

        private void LoadDefaultSounds()
        {
            // Try to load sounds from Resources/Audio/
            // If not found, we'll generate placeholder sounds
            foreach (SoundEffect sfx in System.Enum.GetValues(typeof(SoundEffect)))
            {
                var clip = Resources.Load<AudioClip>($"Audio/{sfx}");
                if (clip != null)
                {
                    sfxClips[sfx] = clip;
                }
            }

            Debug.Log($"[AudioManager] Loaded {sfxClips.Count} sound effects from Resources");
        }

        /// <summary>
        /// Plays a sound effect.
        /// </summary>
        public void PlaySFX(SoundEffect effect, float volumeMultiplier = 1f)
        {
            if (!sfxClips.TryGetValue(effect, out var clip))
            {
                // No clip loaded - just log (don't spam)
                return;
            }

            PlayClip(clip, sfxVolume * masterVolume * volumeMultiplier);
        }

        /// <summary>
        /// Plays a custom audio clip.
        /// </summary>
        public void PlayClip(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;

            var source = GetNextSfxSource();
            source.clip = clip;
            source.volume = volume;
            source.pitch = Random.Range(0.95f, 1.05f); // Slight variation
            source.Play();
        }

        private AudioSource GetNextSfxSource()
        {
            var source = sfxPool[currentSfxIndex];
            currentSfxIndex = (currentSfxIndex + 1) % sfxPool.Count;
            return source;
        }

        /// <summary>
        /// Plays background music.
        /// </summary>
        public void PlayMusic(AudioClip clip, bool fade = true)
        {
            if (fade && musicSource.isPlaying)
            {
                StartCoroutine(CrossfadeMusic(clip));
            }
            else
            {
                musicSource.clip = clip;
                musicSource.volume = musicVolume * masterVolume;
                musicSource.Play();
            }
        }

        /// <summary>
        /// Stops the current music.
        /// </summary>
        public void StopMusic(bool fade = true)
        {
            if (fade)
            {
                StartCoroutine(FadeOutMusic());
            }
            else
            {
                musicSource.Stop();
            }
        }

        private System.Collections.IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            float duration = 1f;
            float elapsed = 0f;
            float startVolume = musicSource.volume;

            // Fade out
            while (elapsed < duration / 2)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (duration / 2));
                yield return null;
            }

            // Switch clip
            musicSource.clip = newClip;
            musicSource.Play();

            // Fade in
            elapsed = 0f;
            while (elapsed < duration / 2)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0f, musicVolume * masterVolume, elapsed / (duration / 2));
                yield return null;
            }

            musicSource.volume = musicVolume * masterVolume;
        }

        private System.Collections.IEnumerator FadeOutMusic()
        {
            float duration = 0.5f;
            float elapsed = 0f;
            float startVolume = musicSource.volume;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            musicSource.Stop();
        }

        /// <summary>
        /// Sets the master volume (0-1).
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            musicSource.volume = musicVolume * masterVolume;
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        }

        /// <summary>
        /// Sets the music volume (0-1).
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            musicSource.volume = musicVolume * masterVolume;
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        }

        /// <summary>
        /// Sets the SFX volume (0-1).
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        }

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SFXVolume => sfxVolume;

        private void Start()
        {
            // Load saved volume settings
            if (PlayerPrefs.HasKey("MasterVolume"))
                masterVolume = PlayerPrefs.GetFloat("MasterVolume");
            if (PlayerPrefs.HasKey("MusicVolume"))
                musicVolume = PlayerPrefs.GetFloat("MusicVolume");
            if (PlayerPrefs.HasKey("SFXVolume"))
                sfxVolume = PlayerPrefs.GetFloat("SFXVolume");

            musicSource.volume = musicVolume * masterVolume;
        }
    }
}
