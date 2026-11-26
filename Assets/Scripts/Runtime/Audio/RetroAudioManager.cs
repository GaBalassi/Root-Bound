using System.Collections.Generic;
using UnityEngine;

namespace Rootbound.Audio
{
    public enum SoundEffectId
    {
        Jump,
        DoubleJump,
        Dash,
        Slash,
        HeavyHit,
        RootWhip,
        RootLatch,
        EnemyHit,
        EnemyDie,
        Pickup,
        Relic,
        BossSlam,
        PlayerHurt,
        Victory
    }

    /// <summary>
    /// Sintetizzatore procedurale di effetti sonori via AudioClip.Create.
    /// Zero asset audio esterni e compatibile con ambienti headless.
    /// </summary>
    public sealed class RetroAudioManager : MonoBehaviour
    {
        private static RetroAudioManager _instance;
        private AudioSource _source;
        private readonly Dictionary<SoundEffectId, AudioClip> _clips = new Dictionary<SoundEffectId, AudioClip>();

        public static void Play(SoundEffectId id, float volume = 0.45f)
        {
            if (!Application.isPlaying) return;
            EnsureInstance();
            if (_instance == null || _instance._source == null) return;
            if (!_instance._clips.TryGetValue(id, out var clip) || clip == null) return;

            _instance._source.pitch = Random.Range(0.95f, 1.06f);
            _instance._source.PlayOneShot(clip, volume);
        }

        private static void EnsureInstance()
        {
            if (_instance != null) return;
            if (FindAnyObjectByType<AudioListener>() == null) return;

            var go = new GameObject("RootboundAudioManager");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<RetroAudioManager>();
            _instance._source = go.AddComponent<AudioSource>();
            _instance._source.playOnAwake = false;
            _instance._source.spatialBlend = 0f;
            _instance.BuildClips();
        }

        private void BuildClips()
        {
            _clips[SoundEffectId.Jump] = CreateSweep("sfx_jump", 0.11f, 240f, 520f, 0);
            _clips[SoundEffectId.DoubleJump] = CreateSweep("sfx_djump", 0.13f, 380f, 760f, 0);
            _clips[SoundEffectId.Dash] = CreateSweep("sfx_dash", 0.12f, 620f, 180f, 2);
            _clips[SoundEffectId.Slash] = CreateSweep("sfx_slash", 0.1f, 740f, 220f, 2);
            _clips[SoundEffectId.HeavyHit] = CreateSweep("sfx_heavy", 0.2f, 260f, 70f, 1);
            _clips[SoundEffectId.RootWhip] = CreateSweep("sfx_root_whip", 0.14f, 320f, 680f, 0);
            _clips[SoundEffectId.RootLatch] = CreateSweep("sfx_root_latch", 0.12f, 540f, 880f, 1);
            _clips[SoundEffectId.EnemyHit] = CreateSweep("sfx_enemy_hit", 0.09f, 340f, 140f, 1);
            _clips[SoundEffectId.EnemyDie] = CreateSweep("sfx_enemy_die", 0.22f, 300f, 65f, 2);
            _clips[SoundEffectId.Pickup] = CreateSweep("sfx_pickup", 0.12f, 660f, 1180f, 0);
            _clips[SoundEffectId.Relic] = CreateArpeggio("sfx_relic", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.08f);
            _clips[SoundEffectId.BossSlam] = CreateSweep("sfx_boss_slam", 0.34f, 150f, 45f, 2);
            _clips[SoundEffectId.PlayerHurt] = CreateSweep("sfx_hurt", 0.18f, 220f, 95f, 1);
            _clips[SoundEffectId.Victory] = CreateArpeggio("sfx_victory", new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f }, 0.09f);
        }

        private static AudioClip CreateSweep(string clipName, float duration, float f0, float f1, int waveType)
        {
            const int sampleRate = 22050;
            int samples = Mathf.Max(64, Mathf.RoundToInt(duration * sampleRate));
            float[] data = new float[samples];
            float phase = 0f;
            uint rng = 1234567u;

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                float freq = Mathf.Lerp(f0, f1, t);
                phase += freq / sampleRate;
                float env = (1f - t) * (t < 0.05f ? t / 0.05f : 1f);

                float sample;
                if (waveType == 0)
                {
                    // Triangolo morbido
                    float frac = phase - Mathf.Floor(phase);
                    sample = 1f - 4f * Mathf.Abs(frac - 0.5f);
                }
                else if (waveType == 1)
                {
                    // Quadra attenuata
                    float frac = phase - Mathf.Floor(phase);
                    sample = frac < 0.5f ? 0.8f : -0.8f;
                }
                else
                {
                    // Rumore + sub tono
                    rng = rng * 1664525u + 1013904223u;
                    float noise = ((rng >> 16) / 32768f) - 1f;
                    float tone = Mathf.Sin(phase * Mathf.PI * 2f);
                    sample = noise * 0.55f + tone * 0.45f;
                }

                data[i] = sample * env * 0.65f;
            }

            var clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateArpeggio(string clipName, float[] notes, float noteLen)
        {
            const int sampleRate = 22050;
            int noteSamples = Mathf.RoundToInt(noteLen * sampleRate);
            int total = noteSamples * notes.Length;
            float[] data = new float[total];
            float phase = 0f;

            for (int n = 0; n < notes.Length; n++)
            {
                float freq = notes[n];
                for (int i = 0; i < noteSamples; i++)
                {
                    float t = i / (float)noteSamples;
                    phase += freq / sampleRate;
                    float env = Mathf.Sin(t * Mathf.PI);
                    float frac = phase - Mathf.Floor(phase);
                    float tri = 1f - 4f * Mathf.Abs(frac - 0.5f);
                    data[n * noteSamples + i] = tri * env * 0.55f;
                }
            }

            var clip = AudioClip.Create(clipName, total, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
