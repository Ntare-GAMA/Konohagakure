using System.Collections.Generic;
using UnityEngine;

namespace Konohagakure
{
    /// <summary>
    /// Central audio system. Keeps ambient/background audio and action SFX on separate
    /// sources so they never overlap incorrectly, and exposes volume/mute controls for the UI.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public enum CueType { Footstep, Jump, DrillStart, Checkpoint, DrillComplete, SenseiYell }

        [Header("Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Ambient")]
        [SerializeField] private AudioClip dojoAmbience;

        [Header("Cue Clips")]
        [SerializeField] private CueClip[] cueClips;

        [System.Serializable]
        public struct CueClip
        {
            public CueType type;
            public AudioClip clip;
        }

        private Dictionary<CueType, AudioClip> cueMap;
        private bool muted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            cueMap = new Dictionary<CueType, AudioClip>();
            foreach (var c in cueClips)
            {
                cueMap[c.type] = c.clip;
            }
        }

        private void Start()
        {
            if (dojoAmbience != null)
            {
                bgmSource.clip = dojoAmbience;
                bgmSource.loop = true;
                bgmSource.Play();
            }
        }

        /// <summary>Plays a one-shot SFX for the given cue, if a clip is assigned. Called from
        /// Animation Events (footstep/jump) and from KonohaManager (checkpoint/drill events).</summary>
        public void PlayCue(CueType type)
        {
            if (cueMap.TryGetValue(type, out var clip) && clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        /// <summary>Wired to a UI volume slider (0-1).</summary>
        public void SetMasterVolume(float value)
        {
            bgmSource.volume = value;
            sfxSource.volume = value;
        }

        /// <summary>Wired to a UI mute toggle.</summary>
        public void ToggleMute(bool isMuted)
        {
            muted = isMuted;
            bgmSource.mute = muted;
            sfxSource.mute = muted;
        }
    }
}