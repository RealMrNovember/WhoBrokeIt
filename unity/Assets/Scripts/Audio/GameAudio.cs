using System.Collections.Generic;
using UnityEngine;

namespace WhoBrokeIt.Audio
{
    public enum Sfx { Step, Bonk, Whoosh, Swing, Pickup, Mount, Spark, UiHover, UiClick, UiBack, WoodCrack, WoodSmash, MetalClang, GlassShatter, ConcreteThud, CeramicSmash, PlasticKnock, CanvasFlap, Creak, RepairTap }
    public enum LoopKind { ForkliftEngine, SkateRoll, BikeTick }

    /// <summary>
    /// First sound pass: every effect is synthesised from code at start (no audio files yet), played through a small pool of 3D AudioSources.
    /// Volumes: master and effects (saved, edited in the Esc menu; master is applied as a squared curve, as the audio guidance suggests).
    /// Dedicated servers play nothing.
    /// </summary>
    public static class GameAudio
    {
        const int Rate = 22050;
        static AudioClip[] clips;
        static AudioClip[] loops;
        static readonly List<AudioSource> pool = new List<AudioSource>();
        static int next;

        public static float Master
        {
            get { return PlayerPrefs.GetFloat("wbi_vol_master", 0.8f); }
            set { PlayerPrefs.SetFloat("wbi_vol_master", value); Apply(); }
        }

        public static float Effects
        {
            get { return PlayerPrefs.GetFloat("wbi_vol_sfx", 0.9f); }
            set { PlayerPrefs.SetFloat("wbi_vol_sfx", value); }
        }

        public static void Apply() { AudioListener.volume = Master * Master; }

        static bool Silent => Net.DedicatedServer.IsDedicated;

        // ---------- synthesis ----------
        static uint seed = 2463534242u;
        static float Noise()
        {
            seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
            return (seed & 0xFFFFFF) / (float)0x7FFFFF - 1f;
        }

        delegate float Gen(float t, float u);

        static AudioClip Make(string name, float seconds, bool loop, Gen g)
        {
            var n = Mathf.Max(1, (int)(seconds * Rate));
            var d = new float[n];
            for (var i = 0; i < n; i++)
            {
                var t = i / (float)Rate;
                var u = i / (float)n;
                d[i] = Mathf.Clamp(g(t, u), -1f, 1f);
            }
            var c = AudioClip.Create(name, n, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }

        static void Build()
        {
            if (clips != null) return;
            const float TwoPi = Mathf.PI * 2f;
            var lp = 0f;
            clips = new AudioClip[20];
            // soft footstep: a dull thump plus a short scuff
            clips[(int)Sfx.Step] = Make("step", 0.13f, false, (t, u) => Mathf.Sin(TwoPi * (95f - 55f * u) * t) * Mathf.Exp(-u * 9f) * 0.7f + Noise() * Mathf.Exp(-u * 25f) * 0.22f);
            // cartoon bonk: a falling boing with a knock on top
            clips[(int)Sfx.Bonk] = Make("bonk", 0.4f, false, (t, u) => Mathf.Sin(TwoPi * (340f - 250f * u) * t) * Mathf.Exp(-u * 5f) * 0.75f + Noise() * Mathf.Exp(-u * 14f) * 0.35f);
            // throw / swing: filtered noise swelling and fading
            lp = 0f;
            clips[(int)Sfx.Whoosh] = Make("whoosh", 0.36f, false, (t, u) => { lp += (Noise() - lp) * (0.04f + 0.30f * Mathf.Sin(Mathf.PI * u)); var a = Mathf.Sin(Mathf.PI * u); return lp * a * a * 1.9f; });
            lp = 0f;
            clips[(int)Sfx.Swing] = Make("swing", 0.26f, false, (t, u) => { lp += (Noise() - lp) * (0.08f + 0.40f * Mathf.Sin(Mathf.PI * u)); var a = Mathf.Sin(Mathf.PI * u); return lp * a * a * 1.8f; });
            clips[(int)Sfx.Pickup] = Make("pickup", 0.18f, false, (t, u) => Mathf.Sin(TwoPi * (520f + 760f * u) * t) * Mathf.Exp(-u * 6f) * 0.5f);
            // getting on a vehicle: a clunk
            clips[(int)Sfx.Mount] = Make("mount", 0.16f, false, (t, u) => Noise() * Mathf.Exp(-u * 40f) * 0.55f + Mathf.Sin(TwoPi * 170f * t) * Mathf.Exp(-u * 18f) * 0.45f);
            // electric crackle
            clips[(int)Sfx.Spark] = Make("spark", 0.42f, false, (t, u) => Noise() * Mathf.Exp(-u * 6f) * (Mathf.Sin(TwoPi * 36f * t) > 0f ? 1f : 0.25f) * 0.6f);

            // menu sounds: soft tick, bright two-note click, low back thud
            clips[(int)Sfx.UiHover] = Make("ui_hover", 0.07f, false, (t, u) => Mathf.Sin(TwoPi * 880f * t) * Mathf.Exp(-u * 7f) * 0.28f);
            clips[(int)Sfx.UiClick] = Make("ui_click", 0.16f, false, (t, u) => (Mathf.Sin(TwoPi * 660f * t) * (u < 0.45f ? 1f : 0f) + Mathf.Sin(TwoPi * 990f * t) * (u >= 0.35f ? 1f : 0f)) * Mathf.Exp(-u * 5f) * 0.42f);
            clips[(int)Sfx.UiBack] = Make("ui_back", 0.18f, false, (t, u) => Mathf.Sin(TwoPi * (300f - 140f * u) * t) * Mathf.Exp(-u * 7f) * 0.5f);

            // ---- material impacts, breaking and repair (all synthesised) ----
            var hpPrev = 0f; var lpc = 0f;
            clips[(int)Sfx.WoodCrack] = Make("wood_crack", 0.22f, false, (t, u) => Noise() * Mathf.Exp(-u * 28f) * 0.7f + Mathf.Sin(TwoPi * (210f - 80f * u) * t) * Mathf.Exp(-u * 14f) * 0.55f);
            clips[(int)Sfx.WoodSmash] = Make("wood_smash", 0.6f, false, (t, u) =>
            {
                var sm = Mathf.Sin(TwoPi * 95f * t) * Mathf.Exp(-u * 8f) * 0.6f;
                var k = 0f;
                for (var i = 0; i < 4; i++) { if (u >= k) sm += Noise() * Mathf.Exp(-(u - k) * 32f) * (0.75f - k); k += 0.13f; }
                return sm;
            });
            clips[(int)Sfx.MetalClang] = Make("metal_clang", 0.8f, false, (t, u) =>
                (Mathf.Sin(TwoPi * 523f * t) * 0.4f + Mathf.Sin(TwoPi * 1318f * t) * 0.3f + Mathf.Sin(TwoPi * 2217f * t) * 0.2f + Mathf.Sin(TwoPi * 3391f * t) * 0.12f) * Mathf.Exp(-u * 5.5f) + Noise() * Mathf.Exp(-u * 60f) * 0.3f);
            clips[(int)Sfx.GlassShatter] = Make("glass_shatter", 0.7f, false, (t, u) =>
            {
                var n = Noise(); var hp = n - hpPrev; hpPrev = n;
                var cell = u * 40f;
                return hp * Mathf.Exp(-u * 5f) * 0.5f + Mathf.Sin(TwoPi * (2500f + ((int)cell % 7) * 700f) * t) * Mathf.Exp(-(cell % 1f) * 6f) * 0.25f * Mathf.Exp(-u * 3f);
            });
            clips[(int)Sfx.ConcreteThud] = Make("concrete_thud", 0.35f, false, (t, u) => Mathf.Sin(TwoPi * (70f - 30f * u) * t) * Mathf.Exp(-u * 10f) * 0.8f + Noise() * Mathf.Exp(-u * 30f) * 0.45f);
            clips[(int)Sfx.CeramicSmash] = Make("ceramic_smash", 0.45f, false, (t, u) =>
            {
                var cell = u * 30f;
                return Noise() * Mathf.Exp(-u * 26f) * 0.55f + Mathf.Sin(TwoPi * 520f * t) * Mathf.Exp(-u * 18f) * 0.35f + Mathf.Sin(TwoPi * (1800f + ((int)cell % 5) * 500f) * t) * Mathf.Exp(-(cell % 1f) * 7f) * 0.3f * Mathf.Exp(-u * 3f);
            });
            clips[(int)Sfx.PlasticKnock] = Make("plastic_knock", 0.16f, false, (t, u) => Mathf.Sin(TwoPi * (430f - 120f * u) * t) * Mathf.Exp(-u * 16f) * 0.6f + Noise() * Mathf.Exp(-u * 50f) * 0.2f);
            clips[(int)Sfx.CanvasFlap] = Make("canvas_flap", 0.3f, false, (t, u) => { lpc += (Noise() - lpc) * 0.12f; var a = Mathf.Sin(Mathf.PI * u); return lpc * a * a * 2.2f; });
            clips[(int)Sfx.Creak] = Make("creak", 0.6f, false, (t, u) => Mathf.Sin(TwoPi * (230f + 25f * Mathf.Sin(TwoPi * 9f * t)) * t) * 0.25f * Mathf.Sin(Mathf.PI * u) + Noise() * 0.05f * Mathf.Sin(Mathf.PI * u));
            clips[(int)Sfx.RepairTap] = Make("repair_tap", 0.14f, false, (t, u) => Mathf.Sin(TwoPi * 820f * t) * Mathf.Exp(-u * 20f) * 0.5f + Noise() * Mathf.Exp(-u * 70f) * 0.3f);

            loops = new AudioClip[3];
            // loops use whole numbers of cycles so they join without a click (1 second long)
            loops[(int)LoopKind.ForkliftEngine] = Make("engine", 1f, true, (t, u) => Mathf.Sin(TwoPi * 54f * t) * 0.45f + Mathf.Sin(TwoPi * 108f * t) * 0.28f + Mathf.Sin(TwoPi * 162f * t) * 0.14f + Mathf.Sin(TwoPi * 27f * t) * 0.2f);
            loops[(int)LoopKind.SkateRoll] = Make("skate", 1f, true, (t, u) => (Mathf.Sin(TwoPi * 41f * t) * 0.3f + Mathf.Sin(TwoPi * 83f * t) * 0.25f + Mathf.Sin(TwoPi * 167f * t) * 0.12f) * (0.65f + 0.35f * Mathf.Sin(TwoPi * 9f * t)));
            loops[(int)LoopKind.BikeTick] = Make("bike", 1f, true, (t, u) => (Mathf.Repeat(t * 14f, 1f) < 0.04f ? 0.5f : 0f) + Mathf.Sin(TwoPi * 95f * t) * 0.05f);
        }

        static void EnsurePool()
        {
            if (pool.Count > 0 && pool[0] != null) return;
            pool.Clear();
            var root = new GameObject("GameAudio");
            Object.DontDestroyOnLoad(root);
            for (var i = 0; i < 14; i++)
            {
                var s = new GameObject("Sfx" + i).AddComponent<AudioSource>();
                s.transform.SetParent(root.transform, false);
                s.playOnAwake = false;
                s.spatialBlend = 1f;
                s.rolloffMode = AudioRolloffMode.Linear;
                s.minDistance = 2f;
                s.maxDistance = 38f;
                pool.Add(s);
            }
            Apply();
        }

        /// <summary>One-shot effect at a world position (pitch 1 = as built; vary it a little to avoid the machine-gun effect).</summary>
        public static void Play(Sfx sfx, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (Silent) return;
            Build();
            EnsureUi();
            EnsureListener();
            EnsurePool();
            var s = pool[next++ % pool.Count];
            s.transform.position = position;
            s.clip = clips[(int)sfx];
            s.volume = Mathf.Clamp01(volume * Effects);
            s.pitch = pitch;
            s.Play();
        }

        /// <summary>Material impact: louder with speed, deeper with mass (heavy things sound heavy), small random pitch so repeats do not machine-gun.</summary>
        public static void PlayImpact(Props.MaterialKind material, Vector3 position, float intensity, float mass)
        {
            var info = Props.MaterialTable.Get(material);
            var pitch = Mathf.Clamp(1.55f - 0.16f * Mathf.Log(Mathf.Max(1f, mass) + 1f, 2f), 0.55f, 1.6f) * Random.Range(0.94f, 1.06f);
            Play(info.hit, position, Mathf.Clamp01(0.15f + 0.85f * intensity), pitch);
        }

        // ---------- menu: 2D sounds and music ----------
        static AudioSource uiSource, musicSource;
        static AudioClip menuMusic;

        public static float Music
        {
            get { return PlayerPrefs.GetFloat("wbi_vol_music", 0.5f); }
            set { PlayerPrefs.SetFloat("wbi_vol_music", value); if (musicSource != null) musicSource.volume = value * 0.5f; }
        }

        static void EnsureUi()
        {
            if (uiSource != null) return;
            var root = new GameObject("GameAudioUi");
            Object.DontDestroyOnLoad(root);
            uiSource = root.AddComponent<AudioSource>();
            uiSource.spatialBlend = 0f;
            uiSource.playOnAwake = false;
            musicSource = root.AddComponent<AudioSource>();
            musicSource.spatialBlend = 0f;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            uiListener = root.AddComponent<AudioListener>();
            uiListener.enabled = false;
            Apply();
        }

        static AudioListener uiListener;
        static float listenerCheckAt;

        /// <summary>Unity plays nothing without an enabled AudioListener, and the menu has no camera with one (the player camera only exists in a match). Keep exactly one alive: ours while no other is enabled.</summary>
        static void EnsureListener()
        {
            if (uiListener == null || Time.unscaledTime < listenerCheckAt) return;
            listenerCheckAt = Time.unscaledTime + 0.5f;
            var other = false;
            foreach (var l in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (l != uiListener && l.enabled && l.gameObject.activeInHierarchy) { other = true; break; }
            if (uiListener.enabled == other) uiListener.enabled = !other;
        }

        /// <summary>Non-positional menu sound.</summary>
        public static void PlayUi(Sfx sfx, float volume = 1f)
        {
            if (Silent) return;
            Build();
            EnsureUi();
            EnsureListener();
            uiSource.pitch = 1f;
            uiSource.PlayOneShot(clips[(int)sfx], Mathf.Clamp01(volume * Effects));
        }


        /// <summary>Test/debug: what would be audible right now.</summary>
        public static string Diag()
        {
            var n = 0; var names = "";
            foreach (var l in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) if (l.enabled && l.gameObject.activeInHierarchy) { n++; names += l.name + ","; }
            var playing = 0; foreach (var s in pool) if (s != null && s.isPlaying) playing++;
            return "dinleyici=" + n + " (" + names + ") ana=" + AudioListener.volume.ToString("0.00") + " duraklat=" + AudioListener.pause + " muzik=" + (musicSource != null && musicSource.isPlaying) + " muzikSes=" + (musicSource != null ? musicSource.volume.ToString("0.00") : "-") + " efektHavuz=" + pool.Count + " calan=" + playing + " muzikAyar=" + Music.ToString("0.00") + " efektAyar=" + Effects.ToString("0.00");
        }
        /// <summary>Gentle looping menu music (a 4-chord pad with a pentatonic pluck line, 16 seconds), made from code. Off while playing.</summary>
        public static void MenuMusic(bool on, float gain = 0.5f)
        {
            if (Silent) return;
            EnsureUi();
            EnsureListener();
            if (on)
            {
                if (menuMusic == null) menuMusic = BuildMusic();
                if (musicSource.clip != menuMusic) musicSource.clip = menuMusic;
                musicSource.volume = Music * gain;
                if (!musicSource.isPlaying) musicSource.Play();
            }
            else if (musicSource.isPlaying) musicSource.Stop();
        }

        static AudioClip BuildMusic()
        {
            const float TwoPi = Mathf.PI * 2f;
            const int rate = 22050;
            const float bar = 4f, len = bar * 4f;
            var chords = new[] { new[] { 261.6f, 329.6f, 392.0f }, new[] { 220.0f, 261.6f, 329.6f }, new[] { 174.6f, 220.0f, 261.6f }, new[] { 196.0f, 246.9f, 392.0f } };
            var line = new[] { 523.3f, 659.3f, 784.0f, 659.3f, 587.3f, 659.3f, 523.3f, 392.0f };
            var n = (int)(len * rate);
            var d = new float[n];
            for (var i = 0; i < n; i++)
            {
                var t = i / (float)rate;
                var b = Mathf.Min(3, (int)(t / bar));
                var tb = t - b * bar;
                var env = Mathf.Min(1f, tb / 0.6f) * Mathf.Min(1f, (bar - tb) / 0.6f);
                var v = 0f;
                foreach (var f in chords[b]) v += Mathf.Sin(TwoPi * f * 0.5f * t) * 0.07f * env + Mathf.Sin(TwoPi * f * t) * 0.03f * env;
                var step = Mathf.FloorToInt(t / 0.5f);
                var ts = t - step * 0.5f;
                var f2 = line[(step + b) % line.Length] * ((step % 4) == 3 ? 1f : 1f);
                v += Mathf.Sin(TwoPi * f2 * t) * Mathf.Exp(-ts * 7f) * 0.09f;
                d[i] = v;
            }
            // fade the seam so the loop is click free
            var fade = rate / 20;
            for (var i = 0; i < fade; i++) { var k = i / (float)fade; d[i] *= k; d[n - 1 - i] *= k; }
            var c = AudioClip.Create("menu_music", n, 1, rate, false);
            c.SetData(d, 0);
            return c;
        }

        /// <summary>A looping source on a vehicle; the caller sets volume and pitch from the speed.</summary>
        public static AudioSource AttachLoop(GameObject host, LoopKind kind)
        {
            if (Silent) return null;
            Build();
            EnsureUi();
            EnsureListener();
            var s =host.AddComponent<AudioSource>();
            s.clip = loops[(int)kind];
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 1f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = 2f;
            s.maxDistance = 30f;
            s.volume = 0f;
            s.Play();
            return s;
        }

        /// <summary>Per-frame update for a vehicle loop: silent when parked, louder and higher with speed while somebody drives.</summary>
        public static void UpdateLoop(AudioSource s, bool driven, float speed, float topSpeed, float baseVolume, float basePitch)
        {
            if (s == null) return;
            var k = Mathf.Clamp01(Mathf.Abs(speed) / Mathf.Max(topSpeed, 0.01f));
            var target = driven ? baseVolume * Effects * (0.35f + 0.65f * k) : 0f;
            s.volume = Mathf.Lerp(s.volume, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
            s.pitch = basePitch * (0.8f + 0.9f * k);
        }
    }
}
