using UnityEngine;

namespace Queenless
{
    public sealed class HiveAudio : MonoBehaviour
    {
        const string MuteKey = "Queenless.Mute";

        AudioSource bed;
        AudioSource sfx;
        AudioClip hum;
        AudioClip endingHum;
        AudioClip step;
        AudioClip follow;
        AudioClip mind;
        AudioClip doorOpen;
        AudioClip doorShut;
        AudioClip thud;
        AudioClip skitter;
        AudioClip caught;
        AudioClip clear;

        public bool Muted { get; private set; }

        public void Init()
        {
            bed = gameObject.AddComponent<AudioSource>();
            sfx = gameObject.AddComponent<AudioSource>();
            bed.loop = true;
            bed.playOnAwake = false;
            sfx.playOnAwake = false;
            bed.spatialBlend = 0f;
            sfx.spatialBlend = 0f;

            hum = Hum(new[] { 110f, 164.8f }, 0.22f, 2f);
            endingHum = Hum(new[] { 110f, 164.8f, 220f, 329.6f }, 0.35f, 3f);
            step = Burst(740f, 0.045f, 0.35f, 0.15f);
            follow = Burst(620f, 0.04f, 0.22f, 0.05f);
            mind = Sweep(240f, 680f, 0.16f, 0.28f);
            doorOpen = Burst(196f, 0.42f, 0.32f, 0f);
            doorShut = Burst(120f, 0.22f, 0.16f, 0f);
            thud = Burst(78f, 0.14f, 0.45f, 0.1f);
            skitter = Noise(0.05f, 0.18f);
            caught = Dissonant(52f, 57f, 0.55f, 0.4f);
            clear = Chord(new[] { 523.25f, 659.25f, 783.99f }, 0.55f, 0.3f);

            Muted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
            bed.mute = Muted;
            PlayBed();
        }

        public void ToggleMute()
        {
            SetMuted(!Muted);
        }

        public void SetMuted(bool muted)
        {
            Muted = muted;
            bed.mute = muted;
            PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void PlayBed()
        {
            if (bed.clip == hum && bed.isPlaying)
                return;
            bed.clip = hum;
            bed.volume = 0.07f;
            bed.Play();
        }

        public void PlayEndingBed()
        {
            bed.clip = endingHum;
            bed.volume = 0.16f;
            bed.Play();
        }

        public void Step() { Shot(step, 0.45f); }
        public void FollowStep() { Shot(follow, 0.35f); }
        public void Switch() { Shot(mind, 0.4f); }
        public void Door(bool open) { Shot(open ? doorOpen : doorShut, open ? 0.4f : 0.3f); }
        public void Thud() { Shot(thud, 0.5f); }
        public void Skitter() { Shot(skitter, 0.25f); }
        public void Caught() { Shot(caught, 0.55f); }
        public void Clear() { Shot(clear, 0.4f); }

        void Shot(AudioClip clip, float volume)
        {
            if (Muted || clip == null)
                return;
            sfx.PlayOneShot(clip, volume);
        }

        static AudioClip Hum(float[] freqs, float amp, float seconds)
        {
            int rate = 44100;
            int n = Mathf.RoundToInt(rate * seconds);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float s = 0f;
                for (int f = 0; f < freqs.Length; f++)
                    s += Mathf.Sin(2f * Mathf.PI * freqs[f] * t);
                s /= freqs.Length;
                float seam = 1f;
                float edge = 0.03f;
                if (t < edge) seam = t / edge;
                else if (t > seconds - edge) seam = (seconds - t) / edge;
                data[i] = s * amp * seam;
            }
            return Clip("hum", data, rate);
        }

        static AudioClip Burst(float freq, float seconds, float amp, float noise)
        {
            int rate = 44100;
            int n = Mathf.Max(8, Mathf.RoundToInt(rate * seconds));
            var data = new float[n];
            var rng = new System.Random(freq.GetHashCode());
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-6f * t / seconds) * Mathf.Clamp01(t * rate / 40f);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float nse = ((float)rng.NextDouble() * 2f - 1f) * noise;
                data[i] = (tone + nse) * env * amp;
            }
            return Clip("burst", data, rate);
        }

        static AudioClip Sweep(float from, float to, float seconds, float amp)
        {
            int rate = 44100;
            int n = Mathf.RoundToInt(rate * seconds);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float freq = Mathf.Lerp(from, to, t);
                phase += 2f * Mathf.PI * freq / rate;
                float env = Mathf.Sin(Mathf.PI * t);
                data[i] = Mathf.Sin(phase) * env * amp;
            }
            return Clip("sweep", data, rate);
        }

        static AudioClip Noise(float seconds, float amp)
        {
            int rate = 44100;
            int n = Mathf.RoundToInt(rate * seconds);
            var data = new float[n];
            var rng = new System.Random(17);
            for (int i = 0; i < n; i++)
            {
                float env = Mathf.Exp(-8f * i / (float)n);
                data[i] = ((float)rng.NextDouble() * 2f - 1f) * env * amp;
            }
            return Clip("noise", data, rate);
        }

        static AudioClip Dissonant(float a, float b, float seconds, float amp)
        {
            int rate = 44100;
            int n = Mathf.RoundToInt(rate * seconds);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-2.2f * t / seconds);
                float s = Mathf.Sin(2f * Mathf.PI * a * t) + Mathf.Sin(2f * Mathf.PI * b * t);
                data[i] = s * 0.5f * env * amp;
            }
            return Clip("sting", data, rate);
        }

        static AudioClip Chord(float[] freqs, float seconds, float amp)
        {
            int rate = 44100;
            int n = Mathf.RoundToInt(rate * seconds);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-2.4f * t / seconds) * Mathf.Clamp01(i / 80f);
                float s = 0f;
                for (int f = 0; f < freqs.Length; f++)
                    s += Mathf.Sin(2f * Mathf.PI * freqs[f] * t);
                data[i] = s / freqs.Length * env * amp;
            }
            return Clip("chord", data, rate);
        }

        static AudioClip Clip(string name, float[] data, int rate)
        {
            var clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
