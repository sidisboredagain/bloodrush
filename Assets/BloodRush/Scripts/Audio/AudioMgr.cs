using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace BR
{
    // Sound-effect bank (rendered at startup), minigun hum, and the boss/ending music player.
    public class AudioMgr
    {
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        AudioSource[] pool; int next;
        AudioSource hum, mIntro, mA, mB;
        public float musicVol = 0.7f;
        float lastHit, lastSpike;

        // ---------- music state ----------
        class TrackClips { public AudioClip intro, loopA, loopB; }
        readonly Dictionary<int, TrackClips> tracks = new Dictionary<int, TrackClips>();
        Task<Dictionary<string, float[]>> renderTask;
        int pending = -1; int cur = -1; float fade = 1, fadeTarget = 1, fadeRate = 1; float aMul = 1, bMul = 0, bTarget = 0; bool stopping;
        public bool MusicOn => cur >= 0 || pending >= 0;
        public int Current => stopping ? -1 : (pending >= 0 ? pending : cur); // the track playing (or queued), -1 while silent or fading out
        public void Fight(bool on) => bTarget = on ? 1 : 0;
        public bool Ready(int id) => tracks.ContainsKey(id);
        public float Peak(int id) { if (!tracks.ContainsKey(id) || tracks[id].loopA == null) return -1; var c = tracks[id].loopA; var d = new float[c.samples]; c.GetData(d, 0); float pk = 0; for (int i = 0; i < d.Length; i += 37) pk = Mathf.Max(pk, Mathf.Abs(d[i])); return pk; }
        public bool Playing => mIntro.isPlaying || mA.isPlaying || mB.isPlaying;                   // level music: fuller arrangement during a wave

        public void Init(GameObject host)
        {
            pool = new AudioSource[32];
            for (int i = 0; i < pool.Length; i++) pool[i] = Src(host);
            BuildSfx();
            hum = Src(host); hum.clip = clips["hum"]; hum.loop = true; hum.volume = 0; hum.Play();
            mIntro = Src(host); mA = Src(host); mB = Src(host);
            // render the music on worker threads while the player is in the menus
            renderTask = Task.Run(() =>
            {
                var d = new Dictionary<string, float[]>();
                var jobs = new List<Task>();
                void Job(string key, Func<float[]> f) => jobs.Add(Task.Run(() => { var r = f(); lock (d) d[key] = r; }));
                Job("1i", () => Tracks.Render(Tracks.Track1, 160, 0.25, 0, 0, 64, false, true, 11));
                Job("1a", () => Tracks.Render(Tracks.Track1, 160, 0.25, 0, 64, 320, true, false, 12));
                Job("1b", () => Tracks.Render(Tracks.Track1, 160 * 1.05, 0.25, 1, 64, 320, true, false, 13));
                Job("6i", () => Tracks.Render(Tracks.Track6, 84, 0.7, 0, 0, 16, false, false, 61));
                Job("6a", () => Tracks.Render(Tracks.Track6, 84, 0.7, 0, 16, 272, true, false, 62));
                Job("2i", () => Tracks.Render(Tracks.Track2, 132, 0.6, 0, 0, 256, false, true, 21));
                Job("2a", () => Tracks.Render(Tracks.Track2, 132, 0.6, 0, 256, 448, true, false, 22));
                Job("2b", () => Tracks.Render(Tracks.Track2, 132 * 1.05, 0.6, 1, 256, 448, true, false, 23));
                Job("3i", () => Tracks.Render(Tracks.Track3, 76, 0.65, 0, 0, 224, false, true, 31));
                // level music: hell (4) and heaven (5); loop B is the fuller "fight" arrangement
                Job("4i", () => Tracks.Render(Tracks.Track4, 150, 0.2, 0, 0, 64, false, false, 41));
                Job("4a", () => Tracks.Render(Tracks.Track4, 150, 0.2, 0, 64, 320, true, false, 42));
                Job("4b", () => Tracks.Render(Tracks.Track4, 150, 0.2, 1, 64, 320, true, false, 43));
                Job("5i", () => Tracks.Render(Tracks.Track5, 124, 0.5, 0, 0, 64, false, false, 51));
                Job("5a", () => Tracks.Render(Tracks.Track5, 124, 0.5, 0, 64, 320, true, false, 52));
                Job("5b", () => Tracks.Render(Tracks.Track5, 124, 0.5, 1, 64, 320, true, false, 53));
                // Dead Earth (7) and its boss, the Hollow King (8); the Frozen Circle (9) and its boss, the Frozen Traitor (10)
                Job("7i", () => Tracks.Render(Tracks.Track7, 136, 0.35, 0, 0, 64, false, false, 71));
                Job("7a", () => Tracks.Render(Tracks.Track7, 136, 0.35, 0, 64, 320, true, false, 72));
                Job("7b", () => Tracks.Render(Tracks.Track7, 136, 0.35, 1, 64, 320, true, false, 73));
                Job("8i", () => Tracks.Render(Tracks.Track8, 176, 0.25, 0, 0, 64, false, true, 81));
                Job("8a", () => Tracks.Render(Tracks.Track8, 176, 0.25, 0, 64, 320, true, false, 82));
                Job("8b", () => Tracks.Render(Tracks.Track8, 176, 0.25, 1, 64, 320, true, false, 83));
                Job("9i", () => Tracks.Render(Tracks.Track9, 108, 0.65, 0, 0, 64, false, false, 91));
                Job("9a", () => Tracks.Render(Tracks.Track9, 108, 0.65, 0, 64, 320, true, false, 92));
                Job("9b", () => Tracks.Render(Tracks.Track9, 108, 0.65, 1, 64, 320, true, false, 93));
                Job("10i", () => Tracks.Render(Tracks.Track10, 152, 0.5, 0, 0, 64, false, true, 101));
                Job("10a", () => Tracks.Render(Tracks.Track10, 152, 0.5, 0, 64, 320, true, false, 102));
                Job("10b", () => Tracks.Render(Tracks.Track10, 152 * 1.05, 0.5, 1, 64, 320, true, false, 103));
                // the Warden of Hours (11)
                Job("11i", () => Tracks.Render(Tracks.Track11, 152, 0.5, 0, 0, 64, false, true, 111));
                Job("11a", () => Tracks.Render(Tracks.Track11, 152, 0.5, 0, 64, 320, true, false, 112));
                Job("11b", () => Tracks.Render(Tracks.Track11, 152, 0.5, 1, 64, 320, true, false, 113));
                // the Purgatory levels (12)
                Job("12i", () => Tracks.Render(Tracks.Track12, 104, 0.7, 0, 0, 64, false, false, 121));
                Job("12a", () => Tracks.Render(Tracks.Track12, 104, 0.7, 0, 64, 320, true, false, 122));
                Job("12b", () => Tracks.Render(Tracks.Track12, 104, 0.7, 1, 64, 320, true, false, 123));
                // one for each Purgatory map: Clockwork Halls (13), the Library of Souls (14), the Mirror Wastes (15)
                Job("13i", () => Tracks.Render(Tracks.Track13, 132, 0.45, 0, 0, 64, false, false, 131));
                Job("13a", () => Tracks.Render(Tracks.Track13, 132, 0.45, 0, 64, 320, true, false, 132));
                Job("13b", () => Tracks.Render(Tracks.Track13, 132, 0.45, 1, 64, 320, true, false, 133));
                Job("14i", () => Tracks.Render(Tracks.Track14, 92, 0.75, 0, 0, 64, false, false, 141));
                Job("14a", () => Tracks.Render(Tracks.Track14, 92, 0.75, 0, 64, 320, true, false, 142));
                Job("14b", () => Tracks.Render(Tracks.Track14, 92, 0.75, 1, 64, 320, true, false, 143));
                Job("15i", () => Tracks.Render(Tracks.Track15, 118, 0.7, 0, 0, 64, false, false, 151));
                Job("15a", () => Tracks.Render(Tracks.Track15, 118, 0.7, 0, 64, 320, true, false, 152));
                Job("15b", () => Tracks.Render(Tracks.Track15, 118, 0.7, 1, 64, 320, true, false, 153));
                Task.WaitAll(jobs.ToArray());
                return d;
            });
        }
        static AudioSource Src(GameObject host) { var s = host.AddComponent<AudioSource>(); s.playOnAwake = false; s.spatialBlend = 0; return s; }
        static AudioClip Clip(string name, float[] data)
        {
            var c = AudioClip.Create(name, Mathf.Max(1, data.Length), 1, Syn.SR, false);
            c.SetData(data, 0); return c;
        }

        public void Play(string name, float vol = 1)
        {
            if (!clips.TryGetValue(name, out var c)) { Debug.LogWarning("missing sfx " + name); return; }
            if (name == "hit" || name == "hitHead") { if (Time.unscaledTime - lastHit < 0.03f) return; lastHit = Time.unscaledTime; }
            if (name == "spike") { if (Time.unscaledTime - lastSpike < 0.06f) return; lastSpike = Time.unscaledTime; }
            var s = pool[next++ % pool.Length];
            s.clip = c; s.volume = vol * 0.55f; s.pitch = 1; s.Play();
        }
        public void Hush() { foreach (var s in pool) if (s) s.Stop(); } // every sound effect, at once (a cinematic's silence)
        public void Hum(float spin) { hum.volume = spin * 0.05f * 0.55f * 1.6f; hum.pitch = (50 + spin * 110) / 60f; }

        // ---------- music ----------
        public void Tick(float dt)
        {
            if (renderTask != null && renderTask.IsCompleted)
            {
                if (renderTask.IsFaulted) Debug.LogError("music render failed: " + renderTask.Exception);
                else
                {
                    var d = renderTask.Result;
                    for (int id = 1; id <= 15; id++)
                        tracks[id] = new TrackClips
                        {
                            intro = Clip($"m{id}i", d[$"{id}i"]),
                            loopA = d.ContainsKey($"{id}a") ? Clip($"m{id}a", d[$"{id}a"]) : null,
                            loopB = d.ContainsKey($"{id}b") ? Clip($"m{id}b", d[$"{id}b"]) : null,
                        };
                }
                renderTask = null;
                if (pending >= 0) { int p = pending; pending = -1; Start(p); }
            }
            fade = Mathf.MoveTowards(fade, fadeTarget, dt * fadeRate);
            aMul = Mathf.MoveTowards(aMul, 1 - bTarget, dt * 2.5f); bMul = Mathf.MoveTowards(bMul, bTarget, dt * 2.5f);
            float v = musicVol * 0.8f * 0.55f * 1.5f * fade;
            mIntro.volume = v; mA.volume = v * aMul; mB.volume = v * bMul;
            if (stopping && fade <= 0.001f) { mIntro.Stop(); mA.Stop(); mB.Stop(); stopping = false; cur = -1; }
        }
        public void Start(int id)
        {
            if (!tracks.ContainsKey(id)) { pending = id; return; }
            mIntro.Stop(); mA.Stop(); mB.Stop();
            var tc = tracks[id]; cur = id; stopping = false; fade = 1; fadeTarget = 1; aMul = 1; bMul = 0; bTarget = 0;
            double t0 = AudioSettings.dspTime + 0.06;
            mIntro.clip = tc.intro; mIntro.loop = false; mIntro.PlayScheduled(t0);
            if (tc.loopA != null)
            {
                double introLen = (double)tc.intro.samples / tc.intro.frequency;
                mA.clip = tc.loopA; mA.loop = true; mA.PlayScheduled(t0 + introLen);
                mB.clip = tc.loopB; mB.loop = true;
                if (tc.loopB != null) mB.PlayScheduled(t0 + introLen); // runs silently in step with loop A, so the switch to it stays on the beat
            }
        }
        public void Intensity()
        {
            if (cur < 0 || tracks[cur].loopB == null) return;
            if (!mB.isPlaying) mB.Play();
            if (mIntro.isPlaying) mIntro.Stop();
            if (!mA.isPlaying && !mB.isPlaying) mB.Play();
            bTarget = 1;
        }
        public void Stop(float fadeSec)
        {
            pending = -1;
            if (cur < 0) return;
            stopping = true; fadeTarget = 0; fadeRate = 1f / Mathf.Max(0.05f, fadeSec);
        }

        // ---------- sound effect definitions (ported 1:1 from the HTML game's sfx object) ----------
        void Def(string name, Action<SfxCtx> f) { var c = new SfxCtx(name.GetHashCode()); f(c); clips[name] = Clip(name, c.Render()); }
        static double Mtof(double m) => 440 * Math.Pow(2, (m - 69) / 12);
        void BuildSfx()
        {
            Def("ui", c => c.T(600, 900, 0.06, "square", 0.05));
            Def("revolver", c => { c.N(0.25, 3000, 0.6); c.T(260, 60, 0.12, "square", 0.18); });
            Def("shotgun", c => { c.N(0.45, 1400, 0.9); c.T(130, 35, 0.25, "sawtooth", 0.3); });
            Def("pump", c => { c.N(0.06, 2500, 0.25, "bandpass", 3); c.N(0.06, 1800, 0.25, "bandpass", 3, 0.11); });
            Def("minigun", c => { c.N(0.07, 2600, 0.3); c.T(200, 90, 0.04, "square", 0.05); });
            Def("overheat", c => { c.N(1.0, 5000, 0.35, "highpass"); c.T(900, 300, 0.4, "square", 0.06); });
            Def("sniper", c => { c.N(0.9, 1800, 1.0); c.T(150, 35, 0.6, "sawtooth", 0.35); c.T(70, 28, 0.9, "sine", 0.8); c.N(0.7, 900, 0.3, "lowpass", 1, 0.14); c.N(0.5, 700, 0.15, "lowpass", 1, 0.32); });
            Def("snipeHit", c => { c.T(1568, 1568, 0.55, "sine", 0.22); c.T(2093, 2093, 0.7, "sine", 0.16, 0.03); c.T(3136, 3136, 0.35, "triangle", 0.07, 0.05); c.N(0.08, 6000, 0.45, "highpass"); c.T(130, 40, 0.4, "sine", 0.7); });
            Def("bolt", c => { c.N(0.05, 3000, 0.3, "bandpass", 4); c.N(0.05, 2000, 0.3, "bandpass", 4, 0.16); });
            Def("scopeIn", c => { c.T(400, 1600, 0.25, "sine", 0.12); c.N(0.3, 6000, 0.12, "highpass"); });
            Def("deny", c => c.T(200, 150, 0.15, "square", 0.08));
            Def("hookThrow", c => { c.N(0.15, 3500, 0.2, "bandpass", 3); c.T(500, 900, 0.1, "triangle", 0.06); });
            Def("hookHit", c => { c.T(1400, 700, 0.08, "square", 0.12); c.N(0.06, 4000, 0.25, "highpass"); });
            Def("coreLaunch", c => { c.T(300, 900, 0.2, "sine", 0.2); c.N(0.25, 1500, 0.4); });
            Def("hit", c => c.T(900, 500, 0.06, "square", 0.08));
            Def("hitHead", c => c.T(1400, 500, 0.06, "square", 0.08));
            Def("kill", c => { c.T(320, 70, 0.22, "sawtooth", 0.2); c.N(0.25, 900, 0.35); });
            Def("dash", c => c.N(0.22, 2800, 0.3, "bandpass", 2));
            Def("jump", c => c.T(220, 440, 0.08, "triangle", 0.08));
            Def("slide", c => c.N(0.3, 1800, 0.15, "bandpass", 1));
            Def("slam", c => { c.N(0.6, 280, 1.0); c.T(90, 28, 0.45, "sine", 0.7); });
            Def("coin", c => c.T(1700, 2100, 0.12, "triangle", 0.08));
            Def("ding", c => c.T(2600, 3400, 0.3, "sine", 0.22));
            Def("parry", c => { c.T(1300, 2600, 0.35, "square", 0.15); c.N(0.3, 4000, 0.4, "highpass"); });
            Def("punch", c => c.N(0.12, 600, 0.4));
            Def("saw", c => { c.T(70, 150, 0.5, "sawtooth", 0.2); c.T(140, 300, 0.5, "square", 0.07); c.N(0.5, 2600, 0.3, "bandpass", 2); c.T(40, 60, 0.5, "sawtooth", 0.15); });
            Def("sawHit", c => { c.N(0.12, 1300, 0.45, "bandpass", 1.5); c.T(260, 170, 0.1, "sawtooth", 0.12); });
            Def("hurt", c => c.T(160, 70, 0.2, "sawtooth", 0.3));
            Def("burnLava", c => c.N(0.4, 2500, 0.3, "bandpass", 1));
            Def("burnHoly", c => c.N(0.4, 5000, 0.3, "bandpass", 1));
            Def("pad", c => c.T(200, 1000, 0.35, "sine", 0.25));
            Def("eshot", c => c.T(700, 220, 0.18, "square", 0.05));
            Def("charge", c => c.T(300, 1400, 0.5, "sine", 0.04));
            Def("growl", c => { c.T(120, 70, 0.45, "sawtooth", 0.07); c.N(0.4, 600, 0.12, "bandpass", 2); });
            Def("growlLow", c => { c.T(70, 40, 0.45, "sawtooth", 0.07); c.N(0.4, 300, 0.12, "bandpass", 2); });
            Def("chant", c => { c.T(330, 300, 0.4, "triangle", 0.05); c.T(495, 450, 0.4, "sine", 0.03); });
            Def("spawn", c => c.T(60, 180, 0.6, "sawtooth", 0.05));
            Def("boom", c => { c.N(0.7, 500, 0.9); c.T(70, 25, 0.5, "sine", 0.5); });
            Def("wave", c => c.T(110, 55, 0.8, "sawtooth", 0.25));
            Def("alarm", c => { for (int i = 0; i < 4; i++) c.T(880, 440, 0.35, "square", 0.08, i * 0.45); });
            Def("roar", c => { c.T(90, 40, 1.6, "sawtooth", 0.4); c.T(60, 30, 1.6, "square", 0.25); c.N(1.6, 400, 0.6, "bandpass", 1.5); });
            Def("roarShort", c => { c.T(90, 40, 0.8, "sawtooth", 0.4); c.T(60, 30, 0.8, "square", 0.25); c.N(0.8, 400, 0.6, "bandpass", 1.5); });
            Def("bossDeath", c => { c.N(2.5, 300, 1.0); c.T(120, 20, 2.5, "sawtooth", 0.5); c.T(1200, 200, 1.5, "sine", 0.15); });
            Def("spike", c => { c.N(0.15, 900, 0.35, "bandpass", 2); c.T(200, 80, 0.1, "square", 0.08); });
            Def("laserCharge", c => { c.T(200, 2000, 1.1, "sawtooth", 0.08); c.T(100, 1000, 1.1, "sine", 0.15); });
            Def("blink", c => { c.T(2000, 300, 0.2, "sine", 0.12); c.N(0.2, 5000, 0.15, "highpass"); });
            Def("holy", c => { c.T(523, 523, 1.2, "sine", 0.1); c.T(659, 659, 1.2, "sine", 0.08); c.T(784, 784, 1.2, "sine", 0.08); });
            Def("portalCharge", c => { c.T(70, 420, 1.4, "sawtooth", 0.1); c.T(200, 1800, 1.4, "sine", 0.07); c.N(1.4, 1200, 0.25, "bandpass", 1.5); });
            Def("portalOpen", c => { c.N(1.4, 3000, 0.5, "bandpass", 0.7); c.T(1400, 300, 1.2, "sine", 0.15); c.T(60, 30, 1.0, "sine", 0.7); int k = 0; foreach (var s in new[] { 0, 4, 7, 12 }) { double f = 440 * Math.Pow(2, s / 12.0); c.T(f, f, 1.6, "triangle", 0.05, k++ * 0.06); } });
            Def("warp", c => { c.T(90, 2400, 1.0, "sawtooth", 0.12); c.N(1.0, 1800, 0.45, "bandpass", 1); c.T(40, 900, 1.0, "sine", 0.35); });
            Def("arrive", c => { c.N(0.9, 600, 0.6); c.T(220, 55, 0.9, "sine", 0.5); });
            Def("scream1", c => { c.T(200, 40, 2.6, "sawtooth", 0.4); c.T(90, 30, 2.6, "square", 0.25); c.N(2.6, 500, 0.6, "bandpass", 1.2); });
            Def("scream2", c => { c.T(1600, 200, 5, "sawtooth", 0.1); c.T(800, 1800, 5, "sine", 0.1); c.T(400, 900, 5, "triangle", 0.08); c.N(5, 3000, 0.25, "highpass"); });
            // the Warden of Hours: clockwork, bells, the great gate
            Def("tick", c => { c.N(0.03, 4500, 0.35, "bandpass", 8); c.T(2400, 2200, 0.03, "square", 0.05); });
            Def("tock", c => { c.N(0.05, 2000, 0.4, "bandpass", 6); c.T(900, 780, 0.05, "square", 0.06); });
            Def("toll", c => { c.T(98, 98, 3.6, "sine", 0.5); c.T(196, 195, 3.0, "sine", 0.18); c.T(233, 233, 2.6, "sine", 0.12); c.T(392, 390, 2.0, "triangle", 0.06); c.T(588, 586, 1.2, "sine", 0.04); c.N(0.12, 1800, 0.35, "bandpass", 3); });
            Def("creak", c => { c.T(90, 68, 1.4, "sawtooth", 0.12); c.T(132, 96, 1.4, "square", 0.05); c.N(1.4, 700, 0.22, "bandpass", 4); });
            Def("rumble", c => { c.N(2.4, 160, 0.9, "lowpass"); c.T(42, 30, 2.4, "sine", 0.5); });
            // the opening (GameIntro.cs)
            Def("iWind", c => { c.N(3.2, 520, 0.32, "bandpass", 0.7); c.N(3.2, 1500, 0.08, "bandpass", 2.5, 0.4); c.N(3.0, 260, 0.2, "lowpass", 1, 0.2); });
            Def("iDrone", c => { c.T(55, 53, 9, "sawtooth", 0.11); c.T(82.4, 81, 9, "sine", 0.16); c.T(41, 40, 9, "sine", 0.3); c.T(110, 109, 7, "triangle", 0.035); c.N(9, 110, 0.45, "lowpass"); });
            Def("iSwell", c => { c.T(220, 220, 3.4, "sine", 0.06); c.T(277, 277, 3.4, "sine", 0.045); c.T(330, 331, 3.4, "triangle", 0.04); c.T(440, 441, 2.8, "sine", 0.025); c.T(110, 110, 3.4, "sine", 0.08); });
            Def("iVoid", c => { c.T(70, 30, 3.6, "sawtooth", 0.24); c.T(74, 31, 3.6, "square", 0.1); c.N(3.6, 240, 0.7, "lowpass"); c.T(1900, 160, 3.0, "sine", 0.05); c.T(36, 24, 3.6, "sine", 0.6); });
            Def("iWhisper", c => { double[] fq = { 2300, 3100, 2700, 3500, 2500, 2900, 3300, 2200, 3000, 2600 }; double[] at = { 0, 0.21, 0.37, 0.62, 0.8, 1.05, 1.3, 1.46, 1.71, 1.95 }; for (int i = 0; i < 10; i++) c.N(0.16 + (i % 3) * 0.07, fq[i], 0.05, "bandpass", 7, at[i]); c.N(2.4, 1800, 0.02, "bandpass", 1.5); });
            Def("iCreak", c => { c.T(150, 96, 1.8, "sawtooth", 0.05); c.T(157, 101, 1.8, "sawtooth", 0.04); c.N(1.8, 950, 0.14, "bandpass", 9); c.T(620, 590, 1.2, "triangle", 0.015, 0.3); });
            Def("iDistort", c => { c.T(58, 64, 4, "square", 0.07); c.T(61, 55, 4, "square", 0.07); c.T(3400, 2300, 3.6, "sine", 0.012); c.N(4, 4200, 0.05, "highpass"); c.T(29, 27, 4, "sine", 0.35); });
            Def("iCrack", c => { c.N(0.28, 5200, 0.8, "highpass"); c.N(0.7, 320, 0.8); c.T(85, 28, 0.9, "sine", 0.7); c.N(0.4, 1600, 0.3, "bandpass", 2, 0.12); });
            Def("iThunder", c => { c.N(0.12, 3200, 0.55, "highpass"); c.N(3.6, 170, 1.0, "lowpass", 1, 0.07); c.T(40, 26, 3.2, "sine", 0.45, 0.1); });
            Def("iCollapse", c => { c.N(3.0, 210, 1.0, "lowpass"); c.N(1.4, 1300, 0.3, "bandpass", 1.5, 0.35); c.T(46, 24, 3.0, "sine", 0.6); c.N(0.3, 2400, 0.25, "bandpass", 3, 0.9); c.N(0.3, 2000, 0.2, "bandpass", 3, 1.5); });
            Def("iRoarFar", c => { c.T(68, 44, 2.6, "sawtooth", 0.12); c.T(51, 38, 2.6, "square", 0.06); c.N(2.6, 300, 0.4, "bandpass", 2); });
            Def("iBeat", c => { c.T(52, 38, 0.42, "sine", 0.9); c.N(0.12, 140, 0.4, "lowpass"); c.T(50, 36, 0.48, "sine", 0.7, 0.38); c.N(0.12, 130, 0.3, "lowpass", 1, 0.38); });
            // the boss stories (GameBossCine.cs): a bed of music for each, and the voice under each line of dialogue
            Def("bcFrost", c => { foreach (var (f, d) in new[] { (220.0, 0.0), (261.6, 0.6), (329.6, 1.2), (293.7, 2.0), (246.9, 2.8) }) c.T(f, f, 3.4, "sine", 0.05, d); c.T(110, 110, 5, "triangle", 0.05); c.N(5, 900, 0.05, "bandpass", 3); });
            Def("bcFrostSad", c => { foreach (var (f, d) in new[] { (196.0, 0.0), (233.1, 0.9), (293.7, 1.8), (261.6, 2.7), (196.0, 3.6) }) c.T(f, f, 3.0, "sine", 0.06, d); c.T(98, 98, 6, "sine", 0.06); });
            Def("bcHollow", c => { c.T(65.4, 65.4, 5, "sawtooth", 0.08); c.T(98, 98, 5, "sawtooth", 0.05); c.T(130.8, 130.8, 4.5, "triangle", 0.05, 0.5); c.T(196, 196, 3.5, "triangle", 0.035, 1.5); c.N(5, 200, 0.2, "lowpass"); });
            Def("bcHollowSad", c => { c.T(73.4, 73.4, 5, "sine", 0.12); c.T(110, 110, 4.5, "sine", 0.06, 0.4); c.T(146.8, 146.8, 4, "triangle", 0.04, 1.2); c.T(174.6, 174.6, 3, "sine", 0.03, 2.2); });
            Def("bcHeaven", c => { foreach (var (f, d) in new[] { (261.6, 0.0), (329.6, 0.1), (392.0, 0.2), (523.3, 0.6), (659.3, 1.1), (784.0, 1.6) }) c.T(f, f * 1.002, 4.5 - d, "sine", 0.04, d); c.T(130.8, 130.8, 5, "triangle", 0.05); c.N(4, 6000, 0.015, "highpass"); });
            Def("bcClock", c => { c.T(55, 55, 6, "sine", 0.12); c.T(82.4, 82.6, 6, "triangle", 0.04); for (int i = 0; i < 6; i++) c.T(1800, 1700, 0.03, "square", 0.06, i * 1.0); c.N(6, 120, 0.25, "lowpass"); });
            Def("iBell", c => { c.T(392, 392, 3.2, "sine", 0.06); c.T(784, 783, 2.4, "sine", 0.03); c.T(1176, 1174, 1.6, "sine", 0.015); c.T(523, 522, 2.6, "triangle", 0.02, 0.01); });
            Def("iChatter", c => { for (int i = 0; i < 14; i++) c.N(0.18 + (i % 4) * 0.06, 500 + (i * 137) % 900, 0.035, "bandpass", 5, i * 0.21); c.N(3.2, 700, 0.05, "bandpass", 1.2); });
            Def("bcFrostDark", c => { c.T(110, 108, 5, "sawtooth", 0.05); c.T(116.5, 114, 5, "sawtooth", 0.04); c.T(55, 54, 5, "sine", 0.18); c.N(5, 300, 0.2, "lowpass"); c.T(233, 220, 4, "sine", 0.03, 1); });
            Def("iIce", c => { c.N(0.6, 6000, 0.5, "highpass"); for (int i = 0; i < 8; i++) c.T(2400 + i * 330, 1800 + i * 200, 0.25, "sine", 0.05, i * 0.05); c.N(1.2, 2500, 0.2, "bandpass", 3, 0.2); });
            Def("bcHollowWar", c => { c.T(55, 55, 5, "sawtooth", 0.1); c.T(82.4, 82.4, 5, "sawtooth", 0.07); for (int i = 0; i < 8; i++) c.N(0.25, 120, 0.35, "lowpass", 1, i * 0.6); c.T(110, 104, 4.5, "square", 0.03, 0.5); });
            Def("iChoir", c => { foreach (var (f, d) in new[] { (261.6, 0.0), (329.6, 0.15), (392.0, 0.3), (493.9, 0.6), (523.3, 0.9) }) { c.T(f, f * 1.003, 4.2 - d, "triangle", 0.03, d); c.T(f * 0.997, f, 4.2 - d, "sine", 0.03, d); } c.N(4, 3200, 0.02, "bandpass", 2); });
            Def("bcHeavenDark", c => { foreach (var (f, d) in new[] { (261.6, 0.0), (311.1, 0.2), (369.9, 0.4), (415.3, 0.8) }) c.T(f, f * 0.98, 4.6 - d, "sawtooth", 0.025, d); c.T(65.4, 64, 5, "sine", 0.2); c.N(5, 200, 0.2, "lowpass"); });
            Def("bcHeavenSad", c => { foreach (var (f, d) in new[] { (440.0, 0.0), (392.0, 1.0), (349.2, 2.0), (329.6, 3.0) }) c.T(f, f, 2.4, "sine", 0.05, d); c.T(220, 220, 5.4, "sine", 0.05); c.T(110, 110, 5.4, "triangle", 0.03); });
            Def("iGlitch", c => { for (int i = 0; i < 10; i++) c.T(200 + (i * 731) % 3000, 100 + (i * 377) % 2000, 0.04, "square", 0.08, i * 0.045); c.N(0.5, 4000, 0.3, "highpass"); });
            Def("vTraitor", c => { c.T(98, 92, 2.4, "sawtooth", 0.05); c.T(147, 140, 2.4, "sine", 0.05); c.N(2.4, 2400, 0.04, "bandpass", 4); c.N(0.4, 5000, 0.08, "highpass"); });
            Def("vHollow", c => { c.T(55, 46, 2.4, "sawtooth", 0.14); c.T(41, 36, 2.4, "square", 0.08); c.N(2.4, 250, 0.4, "bandpass", 2); c.N(0.5, 120, 0.6, "lowpass"); });
            Def("vSeraph", c => { c.T(440, 436, 2.6, "sine", 0.04); c.T(659, 650, 2.6, "sine", 0.03); c.T(880, 860, 2.2, "sine", 0.02); c.T(220, 214, 2.6, "triangle", 0.035); });
            Def("vWarden", c => { c.T(73, 70, 2.2, "square", 0.05); c.T(110, 104, 2.2, "sawtooth", 0.035); c.N(2.2, 1500, 0.05, "bandpass", 6); for (int i = 0; i < 3; i++) c.T(2200, 2100, 0.02, "square", 0.05, i * 0.6); });
            // CARNIFEX: the furnace's theme (a deep drone, the hammer's clang far off), the Void in it, its going out; his voice
            Def("bcForge", c => { c.T(49, 49, 5, "sawtooth", 0.09); c.T(73.4, 73.4, 5, "sawtooth", 0.05); c.N(5, 160, 0.3, "lowpass"); for (int i = 0; i < 4; i++) { c.N(0.12, 2400, 0.12, "bandpass", 8, 0.4 + i * 1.2); c.T(880, 860, 0.5, "triangle", 0.02, 0.4 + i * 1.2); } });
            Def("bcForgeDark", c => { c.T(46.2, 44, 5, "sawtooth", 0.1); c.T(65.4, 61, 5, "square", 0.04); c.T(92.5, 87, 4.5, "sawtooth", 0.035, 0.6); c.N(5, 90, 0.4, "lowpass"); c.N(4, 600, 0.06, "bandpass", 3, 1); });
            Def("bcForgeSad", c => { c.T(61.7, 61.7, 5, "sine", 0.12); c.T(92.5, 92.5, 4.5, "sine", 0.06, 0.4); c.T(123.5, 123.5, 4, "triangle", 0.04, 1.2); c.T(155.6, 155.6, 3, "sine", 0.025, 2.2); c.N(5, 120, 0.12, "lowpass"); });
            Def("vCarnifex", c => { c.T(42, 34, 2.4, "sawtooth", 0.16); c.T(63, 50, 2.4, "square", 0.07); c.N(2.4, 180, 0.5, "lowpass", 2); c.N(2.0, 700, 0.12, "bandpass", 3); });
            Def("iWhoosh", c => { c.N(0.9, 700, 0.6, "bandpass", 1.2); c.T(300, 60, 0.9, "sine", 0.1); });
            Def("iHit", c => { c.N(1.3, 200, 1.0); c.T(52, 22, 1.8, "sine", 0.9); c.T(104, 38, 1.1, "sawtooth", 0.16); c.N(0.08, 3000, 0.3, "highpass"); });
            Def("iTitle", c => { c.N(2.6, 260, 1.1); c.T(42, 18, 3.2, "sine", 1.0); c.T(98, 97, 3.0, "sine", 0.22); c.T(147, 146, 2.6, "triangle", 0.1); c.T(196, 195, 2.0, "sine", 0.06); c.N(0.18, 2600, 0.5, "bandpass", 2); });
            Def("gate", c => { c.N(4.0, 220, 0.9, "lowpass"); c.T(55, 38, 4.0, "sawtooth", 0.3); c.T(82, 58, 3.6, "square", 0.1); c.N(3.6, 900, 0.25, "bandpass", 3); c.T(70, 30, 1.2, "sine", 0.6); });
            Def("clank", c => { c.N(0.25, 2600, 0.6, "bandpass", 5); c.T(620, 400, 0.25, "square", 0.1); c.T(300, 280, 0.8, "triangle", 0.1); c.N(0.6, 400, 0.5, "lowpass", 1, 0.05); });
            Def("timeStop", c => { c.T(2200, 80, 0.9, "sine", 0.25); c.T(1100, 40, 0.9, "sawtooth", 0.1); c.N(0.6, 3000, 0.4, "highpass"); c.T(60, 30, 0.8, "sine", 0.5); });
            Def("rewind", c => { c.T(200, 2400, 1.3, "sawtooth", 0.07); c.T(1200, 300, 1.3, "sine", 0.12); c.N(1.3, 2000, 0.25, "bandpass", 2); });
            Def("boomSmall", c => { c.N(0.35, 700, 0.5); c.T(90, 40, 0.3, "sine", 0.35); });
            for (int i = 0; i < 4; i++) { double f = 900 + i * 260; Def("godRay" + i, c => c.T(f, f * 2, 0.5, "sine", 0.05)); }
            Def("shatter", c => { c.N(0.7, 6000, 0.55, "highpass"); int k = 0; foreach (var f in new[] { 2600.0, 3300, 4100 }) c.T(f, f * 0.7, 0.4, "triangle", 0.05, k++ * 0.04); });
            Def("implode", c => { c.T(80, 3000, 0.6, "sawtooth", 0.15); c.N(0.6, 1500, 0.4, "bandpass", 1); });
            Def("nova", c => { c.N(3, 220, 1.2); c.T(45, 18, 3, "sine", 1); c.T(3000, 90, 2, "sawtooth", 0.15); });
            Def("inspect", c => c.N(0.12, 1800, 0.12, "bandpass", 2));
            Def("click", c => c.N(0.04, 4000, 0.25, "bandpass", 5));
            Def("dryFire", c => { c.N(0.03, 3200, 0.3, "bandpass", 6); c.T(1100, 900, 0.03, "square", 0.03); });
            Def("cylOut", c => { c.N(0.05, 3200, 0.28, "bandpass", 4); c.T(900, 650, 0.06, "square", 0.04); });
            Def("cylIn", c => { c.N(0.04, 2600, 0.35, "bandpass", 5); c.T(650, 520, 0.05, "square", 0.05); c.N(0.03, 4200, 0.2, "bandpass", 6, 0.07); });
            Def("shellIn", c => { c.N(0.05, 1500, 0.32, "bandpass", 3); c.T(190, 140, 0.06, "square", 0.06); });
            Def("magOut", c => { c.N(0.06, 1800, 0.3, "bandpass", 3); c.T(400, 300, 0.05, "square", 0.04); });
            Def("magIn", c => { c.N(0.05, 2400, 0.36, "bandpass", 4); c.T(320, 250, 0.06, "square", 0.07); });
            Def("bang", c => { c.N(0.5, 2200, 0.9); c.T(180, 40, 0.35, "sawtooth", 0.35); c.T(60, 25, 0.5, "sine", 0.7); c.T(2400, 1200, 0.15, "square", 0.05); });
            Def("cheat", c => { int k = 0; foreach (var f in new[] { 523.0, 659, 784, 1047, 1319 }) c.T(f, f, 0.3, "square", 0.06, k++ * 0.06); c.N(0.6, 4000, 0.3, "highpass"); c.T(80, 40, 0.6, "sine", 0.5); });
            // one-off sounds that the HTML game played inline
            Def("slamStart", c => c.N(0.2, 1200, 0.2, "bandpass"));
            Def("bile", c => c.N(0.3, 700, 0.4, "bandpass", 2));
            Def("dashGo", c => { c.T(300, 1200, 0.3, "sawtooth", 0.12); c.N(0.4, 3000, 0.3, "bandpass"); });
            Def("laserTick", c => c.N(0.12, 2500, 0.2, "bandpass", 2));
            Def("volley", c => c.T(1800, 900, 0.2, "sine", 0.06));
            Def("novaDive", c => c.T(1500, 100, 0.5, "sawtooth", 0.12));
            Def("splash", c => c.N(0.3, 500, 0.4));
            Def("inspSpin", c => c.T(60, 180, 0.6, "sawtooth", 0.05));
            Def("inspBang", c => c.T(1200, 1800, 0.2, "triangle", 0.05));
            Def("firework", c => { c.T(220, 60, 0.5, "sine", 0.12); c.N(0.45, 1200, 0.18); });
            Def("bruteHit", c => c.N(0.3, 300, 0.5));
            Def("victory", c => { int k = 0; foreach (var n in new[] { 62, 66, 69, 74, 78, 81, 86 }) { double f = Mtof(n); c.T(f, f, 0.6, "square", 0.06, k++ * 0.09); } c.T(Mtof(50), Mtof(50), 2, "sawtooth", 0.12, 0.6); });
            // minigun motor whine: exactly 60 sawtooth cycles through a lowpass so it loops seamlessly
            var hb = new float[Syn.SR]; var lp = new Biquad("lowpass", 900, 1, Syn.SR);
            for (int pass = 0; pass < 2; pass++) for (int i = 0; i < hb.Length; i++) { float y = lp.Tick((float)Syn.Wave("sawtooth", 60.0 * i / Syn.SR)); if (pass == 1) hb[i] = y; }
            clips["hum"] = Clip("hum", hb);
        }
    }
}
