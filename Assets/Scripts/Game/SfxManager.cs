using UnityEngine;

namespace Universes.Game
{
    [RequireComponent(typeof(AudioSource))]
    public class SfxManager : MonoBehaviour
    {
        [Header("SFX Source")]
        [SerializeField] private AudioSource audioSource;
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
        [Range(0.5f, 1.5f)] [SerializeField] private float minPitch = 0.96f;
        [Range(0.5f, 1.5f)] [SerializeField] private float maxPitch = 1.04f;

        [Header("Music")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip mainMusicClip;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.6f;
        [SerializeField] private bool playMusicOnStart = true;

        [Header("Stars")]
        [SerializeField] private AudioClip[] starClickClips;

        [Header("Planets")]
        [SerializeField] private AudioClip[] planetClickClips;
        [SerializeField] private AudioClip[] planetCreatedClips;
        [SerializeField] private AudioClip[] planetDeathClips;
        [SerializeField] private AudioClip[] planetCrossStarCollisionClips;
        [SerializeField] private AudioClip[] lifeEmergedClips;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            audioSource.playOnAwake = false;

            if (musicSource == null)
                musicSource = audioSource;

            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
        }

        private void Start()
        {
            if (playMusicOnStart)
                PlayMainMusic();
        }

        public void PlayStarClick() => PlayRandom(starClickClips);
        public void PlayPlanetClick() => PlayRandom(planetClickClips);
        public void PlayPlanetCreated() => PlayRandom(planetCreatedClips);
        public void PlayPlanetDeath() => PlayRandom(planetDeathClips);
        public void PlayPlanetCrossStarCollision() => PlayRandom(planetCrossStarCollisionClips);
        public void PlayLifeEmerged() => PlayRandom(lifeEmergedClips);

        public void PlayMainMusic()
        {
            if (musicSource == null || mainMusicClip == null)
                return;

            musicSource.clip = mainMusicClip;
            musicSource.volume = musicVolume;
            musicSource.loop = true;

            if (!musicSource.isPlaying)
                musicSource.Play();
        }

        public void StopMainMusic()
        {
            if (musicSource != null)
                musicSource.Stop();
        }

        private void PlayRandom(AudioClip[] clips)
        {
            if (audioSource == null || clips == null || clips.Length == 0 || masterVolume <= 0f)
                return;

            var clip = clips[Random.Range(0, clips.Length)];
            if (clip == null)
                return;

            var originalPitch = audioSource.pitch;
            audioSource.pitch = Random.Range(Mathf.Min(minPitch, maxPitch), Mathf.Max(minPitch, maxPitch));
            audioSource.PlayOneShot(clip, masterVolume);
            audioSource.pitch = originalPitch;
        }
    }
}
