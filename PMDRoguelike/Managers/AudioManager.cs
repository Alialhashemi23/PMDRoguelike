using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using PMDRoguelike.Constants;
using PMDRoguelike.Core;
using System;
using System.Collections.Generic;

namespace PMDRoguelike.Managers
{
    /// <summary>
    /// Plays SFX (drained from the AudioCues bus) and looping music. Volumes come
    /// from GameConstants; mute and master volume are adjustable at runtime (pause
    /// menu). Fully tolerant of missing audio hardware or assets — every failure
    /// just disables sound instead of crashing.
    /// </summary>
    public class AudioManager
    {
        private static readonly string[] SfxNames =
        {
            "attack", "hit_normal", "hit_super", "hit_weak", "miss", "faint", "levelup",
            "pickup", "money", "stairs", "chest", "buy", "denied", "status", "menu", "boss"
        };
        private static readonly string[] MusicNames = { "title", "verdant", "caverns", "spire", "boss" };

        private readonly ContentManager _content;
        private readonly Dictionary<string, SoundEffect> _sfx = new();
        private readonly Dictionary<string, SoundEffect> _music = new();

        private SoundEffectInstance _musicInstance;
        private string _currentTrack;
        private bool _disabled;

        public bool Muted { get; private set; }
        public float MasterVolume { get; private set; }

        public AudioManager(ContentManager content)
        {
            _content = content;
        }

        public void LoadContent()
        {
            MasterVolume = GameConstants.Instance.Data.Audio?.MasterVolume ?? 0.8f;

            try
            {
                foreach (string name in SfxNames) _sfx[name] = _content.Load<SoundEffect>($"Audio/Sfx/{name}");
                foreach (string name in MusicNames) _music[name] = _content.Load<SoundEffect>($"Audio/Music/{name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Audio disabled: {ex.Message}");
                _disabled = true;
            }
        }

        /// <summary>Drain the cue bus. Call once per frame.</summary>
        public void Update()
        {
            while (AudioCues.TryDequeue(out string cue))
            {
                PlaySfx(cue);
            }
        }

        public void PlaySfx(string name)
        {
            if (_disabled || Muted) return;
            if (!_sfx.TryGetValue(name, out SoundEffect effect)) return;

            float sfxVolume = GameConstants.Instance.Data.Audio?.SfxVolume ?? 0.8f;
            try { effect.Play(MasterVolume * sfxVolume, 0f, 0f); }
            catch (Exception) { _disabled = true; }
        }

        /// <summary>Start (or keep) a looping music track; null/unknown stops music.</summary>
        public void PlayMusic(string track)
        {
            if (_disabled || track == _currentTrack) return;

            StopMusic();
            _currentTrack = track;
            if (track == null || !_music.TryGetValue(track, out SoundEffect effect)) return;

            try
            {
                _musicInstance = effect.CreateInstance();
                _musicInstance.IsLooped = true;
                _musicInstance.Volume = MusicInstanceVolume();
                _musicInstance.Play();
            }
            catch (Exception) { _disabled = true; }
        }

        public void StopMusic()
        {
            _musicInstance?.Stop();
            _musicInstance?.Dispose();
            _musicInstance = null;
            _currentTrack = null;
        }

        public void ToggleMute()
        {
            Muted = !Muted;
            ApplyMusicVolume();
        }

        /// <summary>Nudge master volume (pause menu -/+), clamped 0..1.</summary>
        public void AdjustMasterVolume(float delta)
        {
            MasterVolume = Math.Clamp(MasterVolume + delta, 0f, 1f);
            ApplyMusicVolume();
        }

        private void ApplyMusicVolume()
        {
            if (_musicInstance == null) return;
            try { _musicInstance.Volume = MusicInstanceVolume(); }
            catch (Exception) { _disabled = true; }
        }

        private float MusicInstanceVolume()
        {
            if (Muted) return 0f;
            float musicVolume = GameConstants.Instance.Data.Audio?.MusicVolume ?? 0.6f;
            return Math.Clamp(MasterVolume * musicVolume, 0f, 1f);
        }
    }
}
