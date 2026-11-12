using System;
using System.Collections.Generic;

namespace BR
{
    // Offline re-implementation of the WebAudio nodes the HTML game used (oscillators, biquads, exponential
    // envelopes, waveshaper, feedback delay, reverb). Everything renders into float buffers on a worker thread.
    public sealed class Biquad
    {
        double b0, b1, b2, a1, a2, x1, x2, y1, y2;
        readonly string type; readonly double q; readonly int sr;
        public Biquad(string type, double freq, double q, int sr) { this.type = type; this.q = q; this.sr = sr; SetFreq(freq); }
        public void SetFreq(double f)
        {
            f = Math.Max(10, Math.Min(f, sr * 0.49));
            double w0 = 2 * Math.PI * f / sr, cs = Math.Cos(w0), sn = Math.Sin(w0);
            // WebAudio: lowpass/highpass Q is a resonance in dB, bandpass Q is linear
            double ql = type == "bandpass" ? Math.Max(0.0001, q) : Math.Pow(10, q / 20.0);
            double alpha = sn / (2 * ql), a0 = 1 + alpha;
            switch (type)
            {
                case "highpass": b0 = (1 + cs) / 2; b1 = -(1 + cs); b2 = (1 + cs) / 2; break;
                case "bandpass": b0 = alpha; b1 = 0; b2 = -alpha; break;
                default: b0 = (1 - cs) / 2; b1 = 1 - cs; b2 = (1 - cs) / 2; break;
            }
            a1 = -2 * cs; a2 = 1 - alpha;
            b0 /= a0; b1 /= a0; b2 /= a0; a1 /= a0; a2 /= a0;
        }
        public float Tick(float x)
        {
            double y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
            x2 = x1; x1 = x; y2 = y1; y1 = y;
            return (float)y;
        }
    }

    public static class Syn
    {
        public const int SR = 32000;
        public static double Wave(string type, double ph)
        {
            double f = ph - Math.Floor(ph);
            switch (type)
            {
                case "square": return f < 0.5 ? 1 : -1;
                case "sawtooth": return 2 * f - 1;
                case "triangle": return f < 0.25 ? 4 * f : f < 0.75 ? 2 - 4 * f : 4 * f - 4;
                default: return Math.Sin(2 * Math.PI * f);
            }
        }
        // WebAudio exponentialRampToValueAtTime: v0 at t=0 -> v1 at t=T, held afterwards
        public static double Exp(double v0, double v1, double t, double T) => t >= T ? v1 : t <= 0 ? v0 : v0 * Math.Pow(v1 / v0, t / T);
        public static void Add(float[] o, int i, double v) { if (i >= 0 && i < o.Length) o[i] += (float)v; }

        // noise(dur, freq, gain, type, q): filtered white noise with an exponential decay to silence
        public static void Noise(float[] o, Random rng, double t0, double dur, double freq, double gain, string type = "lowpass", double q = 1, float[] o2 = null, double g2 = 0)
        {
            var bq = new Biquad(type, freq, q, SR); int s0 = (int)(t0 * SR), n = (int)(dur * SR);
            for (int i = 0; i < n; i++)
            {
                double y = bq.Tick((float)(rng.NextDouble() * 2 - 1)) * Exp(gain, 0.0001, (double)i / SR, dur);
                Add(o, s0 + i, y); if (o2 != null) Add(o2, s0 + i, y * g2);
            }
        }
        // tone(f0, f1, dur, type, gain): oscillator with exponential pitch glide and exponential decay
        public static void Tone(float[] o, double t0, double f0, double f1, double dur, string type, double gain)
        {
            int s0 = (int)(t0 * SR), n = (int)(dur * SR); double ph = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SR;
                ph += Exp(f0, Math.Max(1, f1), t, dur) / SR;
                Add(o, s0 + i, Wave(type, ph) * Exp(gain, 0.0001, t, dur));
            }
        }
        public static void SoftClip(float[] o, float drive = 1f)
        {
            for (int i = 0; i < o.Length; i++) o[i] = (float)Math.Tanh(o[i] * drive);
        }
    }

    // Collects the noise()/tone() calls of one sound effect, then renders them.
    public class SfxCtx
    {
        readonly List<(double end, Action<float[]> r)> ops = new List<(double, Action<float[]>)>();
        readonly Random rng;
        public SfxCtx(int seed) { rng = new Random(seed); }
        public void N(double dur, double freq, double gain, string type = "lowpass", double q = 1, double delay = 0) =>
            ops.Add((delay + dur, o => Syn.Noise(o, rng, delay, dur, freq, gain, type, q)));
        public void T(double f0, double f1, double dur, string type, double gain, double delay = 0) =>
            ops.Add((delay + dur, o => Syn.Tone(o, delay, f0, f1, dur, type, gain)));
        public float[] Render()
        {
            double len = 0.02; foreach (var op in ops) len = Math.Max(len, op.end + 0.02);
            var o = new float[(int)(len * Syn.SR)];
            foreach (var op in ops) op.r(o);
            Syn.SoftClip(o, 1.1f);
            return o;
        }
    }

    // Offline music renderer: dry bus, distortion bus, delay send and reverb send, like the WebAudio graph.
    public class MusicRender
    {
        public readonly float[] bus, dist, delay, rev; readonly Random rng; readonly double wet;
        public double bpm; public int I;
        public MusicRender(double seconds, double bpm, double wet, int seed)
        {
            int n = (int)(seconds * Syn.SR);
            bus = new float[n]; dist = new float[n]; delay = new float[n]; rev = new float[n];
            this.bpm = bpm; this.wet = wet; rng = new Random(seed);
        }
        static double Mtof(double m) => 440 * Math.Pow(2, (m - 69) / 12);
        void Out(int i, double v, bool w) { Syn.Add(bus, i, v); if (w) Syn.Add(rev, i, v * wet); }

        public void MNoise(double t, double dur, string type, double freq, double v, double q = 0.8, bool w = false)
        {
            var bq = new Biquad(type, freq, q, Syn.SR); int s0 = (int)(t * Syn.SR), n = (int)((dur + 0.02) * Syn.SR);
            for (int i = 0; i < n; i++) { double y = bq.Tick((float)(rng.NextDouble() * 2 - 1)) * Syn.Exp(v, 0.001, (double)i / Syn.SR, dur); Out(s0 + i, y, w); }
        }
        public void MKick(double t, double v)
        {
            int s0 = (int)(t * Syn.SR), n = (int)(0.35 * Syn.SR); double ph = 0;
            for (int i = 0; i < n; i++) { double tt = (double)i / Syn.SR; ph += Syn.Exp(170, 42, tt, 0.11) / Syn.SR; Out(s0 + i, Math.Sin(2 * Math.PI * ph) * Syn.Exp(v, 0.001, tt, 0.32), false); }
            MNoise(t, 0.02, "highpass", 3000, v * 0.3);
        }
        public void MSnare(double t, double v, bool w = false)
        {
            MNoise(t, w ? 0.35 : 0.2, "bandpass", 1800, v * 0.9, 0.8, w);
            int s0 = (int)(t * Syn.SR), n = (int)(0.15 * Syn.SR); double ph = 0;
            for (int i = 0; i < n; i++) { double tt = (double)i / Syn.SR; ph += Syn.Exp(240, 140, tt, 0.08) / Syn.SR; Out(s0 + i, Syn.Wave("triangle", ph) * Syn.Exp(v * 0.5, 0.001, tt, 0.12), w); }
        }
        public void MHat(double t, double v, bool open) => MNoise(t, open ? 0.18 : 0.045, "highpass", 8000, v * 0.3);
        public void MCrash(double t, double v) => MNoise(t, 1.4, "highpass", 5000, v * 0.35, 0.8, true);
        public void MBass(double t, double note, double dur, double v)
        {
            double f = Mtof(note); var lp = new Biquad("lowpass", 2600, 6, Syn.SR);
            int s0 = (int)(t * Syn.SR), n = (int)((dur + 0.02) * Syn.SR); double p1 = 0, p2 = 0; double det = Math.Pow(2, 8 / 1200.0);
            for (int i = 0; i < n; i++)
            {
                double tt = (double)i / Syn.SR;
                if ((i & 15) == 0) lp.SetFreq(Syn.Exp(2600, 240, tt, dur));
                p1 += f / Syn.SR; p2 += f * 0.5 * det / Syn.SR;
                double g = tt < dur * 0.75 ? v : Syn.Exp(v, 0.001, tt - dur * 0.75, dur * 0.25);
                Syn.Add(dist, s0 + i, lp.Tick((float)(Syn.Wave("sawtooth", p1) + Syn.Wave("square", p2))) * g);
            }
        }
        public void MLead(double t, double note, double dur, double v)
        {
            double f = Mtof(note), det = Math.Pow(2, 12 / 1200.0); var lp = new Biquad("lowpass", 3800, 1, Syn.SR);
            int s0 = (int)(t * Syn.SR), n = (int)((dur + 0.02) * Syn.SR); double p1 = 0, p2 = 0;
            for (int i = 0; i < n; i++)
            {
                double tt = (double)i / Syn.SR; p1 += f / Syn.SR; p2 += f * det / Syn.SR;
                double g = tt < 0.005 ? Syn.Exp(0.0001, v, tt, 0.005) : Syn.Exp(v, 0.001, tt - 0.005, dur - 0.005);
                double y = lp.Tick((float)(Syn.Wave("square", p1) + Syn.Wave("sawtooth", p2))) * g;
                Syn.Add(bus, s0 + i, y); Syn.Add(delay, s0 + i, y);
            }
        }
        public void MStab(double t, double[] notes, double dur, double v)
        {
            foreach (var nn in notes)
            {
                double f = Mtof(nn); var lp = new Biquad("lowpass", 2600, 1, Syn.SR); int s0 = (int)(t * Syn.SR), n = (int)((dur + 0.02) * Syn.SR); double ph = 0;
                for (int i = 0; i < n; i++) { double tt = (double)i / Syn.SR; ph += f / Syn.SR; Out(s0 + i, lp.Tick((float)Syn.Wave("sawtooth", ph)) * Syn.Exp(v, 0.001, tt, dur), false); }
            }
        }
        public void MImpact(double t)
        {
            int s0 = (int)(t * Syn.SR), n = (int)(1.5 * Syn.SR); double ph = 0;
            for (int i = 0; i < n; i++) { double tt = (double)i / Syn.SR; ph += Syn.Exp(110, 28, tt, 1.2) / Syn.SR; Out(s0 + i, Math.Sin(2 * Math.PI * ph) * Syn.Exp(0.9, 0.001, tt, 1.4), true); }
            MNoise(t, 1.2, "lowpass", 500, 0.6, 0.8, true);
        }
        void Voice(double t, double note, double dur, double v, double att, double f1, double f2)
        {
            foreach (var detC in new[] { -7.0, 7.0 })
            {
                double f = Mtof(note), lfoF = 4.5 + rng.NextDouble();
                var a = new Biquad("bandpass", f1, 5, Syn.SR); var b = new Biquad("bandpass", f2, 6, Syn.SR);
                int s0 = (int)(t * Syn.SR), n = (int)((dur + 0.05) * Syn.SR); double ph = 0;
                for (int i = 0; i < n; i++)
                {
                    double tt = (double)i / Syn.SR, cents = detC + 5 * Math.Sin(2 * Math.PI * lfoF * tt);
                    ph += f * Math.Pow(2, cents / 1200) / Syn.SR;
                    float x = (float)Syn.Wave("sawtooth", ph);
                    double g = tt < att ? Syn.Exp(0.0001, v, tt, att) : tt < dur * 0.85 ? v : Syn.Exp(v, 0.001, tt - dur * 0.85, dur * 0.15);
                    Out(s0 + i, (a.Tick(x) + b.Tick(x)) * g, true);
                }
            }
        }
        public void MChoir(double t, double[] notes, double dur, double v) { foreach (var n in notes) Voice(t, n, dur, v, 0.45, 750, 1150); }
        public void MChoirLead(double t, double n, double dur, double v) => Voice(t, n, dur, v, 0.08, 620, 1050);
        public void MOrgan(double t, double[] notes, double dur, double v)
        {
            foreach (var nn in notes)
                foreach (var (mul, gv) in new[] { (0.5, 0.4), (1.0, 1.0), (2.0, 0.5), (4.0, 0.22) })
                {
                    double f = Mtof(nn) * mul; int s0 = (int)(t * Syn.SR), n = (int)((dur + 0.05) * Syn.SR); double ph = 0;
                    for (int i = 0; i < n; i++)
                    {
                        double tt = (double)i / Syn.SR; ph += f / Syn.SR;
                        double g = tt < 0.08 ? Syn.Exp(0.0001, v * gv, tt, 0.08) : tt < dur * 0.9 ? v * gv : Syn.Exp(v * gv, 0.001, tt - dur * 0.9, dur * 0.1);
                        Out(s0 + i, Math.Sin(2 * Math.PI * ph) * g, true);
                    }
                }
        }
        public void MBell(double t, double note, double v)
        {
            foreach (var (mul, gv, d) in new[] { (1.0, 1.0, 2.6), (2.76, 0.5, 1.8), (5.4, 0.25, 1.2), (8.93, 0.12, 0.8) })
            {
                double f = Mtof(note) * mul; int s0 = (int)(t * Syn.SR), n = (int)((d + 0.05) * Syn.SR); double ph = 0;
                for (int i = 0; i < n; i++) { double tt = (double)i / Syn.SR; ph += f / Syn.SR; Out(s0 + i, Math.Sin(2 * Math.PI * ph) * Syn.Exp(v * gv, 0.001, tt, d), true); }
            }
        }
        public void MTimp(double t, double note, double v)
        {
            double f = Mtof(note); int s0 = (int)(t * Syn.SR), n = (int)(1.0 * Syn.SR); double ph = 0;
            for (int i = 0; i < n; i++) { double tt = (double)i / Syn.SR; ph += Syn.Exp(f, f * 0.85, tt, 0.5) / Syn.SR; Out(s0 + i, Math.Sin(2 * Math.PI * ph) * Syn.Exp(v, 0.001, tt, 0.9), true); }
            MNoise(t, 0.3, "lowpass", 300, v * 0.5, 0.8, true);
        }
        public void MString(double t, double note, double dur, double v)
        {
            double f = Mtof(note), det = Math.Pow(2, 10 / 1200.0); var lp = new Biquad("lowpass", 2400, 1, Syn.SR);
            int s0 = (int)(t * Syn.SR), n = (int)((dur + 0.02) * Syn.SR); double p1 = 0, p2 = 0;
            for (int i = 0; i < n; i++)
            {
                double tt = (double)i / Syn.SR; p1 += f / Syn.SR; p2 += f * det / Syn.SR;
                double g = tt < 0.01 ? Syn.Exp(0.0001, v, tt, 0.01) : Syn.Exp(v, 0.001, tt - 0.01, dur - 0.01);
                Out(s0 + i, lp.Tick((float)(Syn.Wave("sawtooth", p1) + Syn.Wave("sawtooth", p2))) * g, true);
            }
        }
        public void MBrass(double t, double[] notes, double v)
        {
            foreach (var nn in notes)
            {
                double f = Mtof(nn); var lp = new Biquad("lowpass", 300, 1, Syn.SR); int s0 = (int)(t * Syn.SR), n = (int)(1.2 * Syn.SR); double ph = 0;
                for (int i = 0; i < n; i++)
                {
                    double tt = (double)i / Syn.SR; ph += f / Syn.SR;
                    if ((i & 15) == 0) lp.SetFreq(tt < 0.08 ? Syn.Exp(300, 2600, tt, 0.08) : Syn.Exp(2600, 700, tt - 0.08, 0.82));
                    double g = tt < 0.03 ? Syn.Exp(0.0001, v, tt, 0.03) : Syn.Exp(v, 0.001, tt - 0.03, 1.07);
                    Out(s0 + i, lp.Tick((float)Syn.Wave("sawtooth", ph)) * g, true);
                }
            }
            MNoise(t, 0.6, "lowpass", 900, 0.5, 0.8, true);
        }

        // ---- added for the Dead Earth / Purgatory boss and level tracks ----
        // a distorted power chord (root, fifth, octave; two detuned saws each) into the distortion bus; palm-muted = short and dark
        public void MGuitar(double t, double note, double dur, double v, bool mute)
        {
            foreach (var (iv, gv) in new[] { (0.0, 1.0), (7.0, 0.75), (12.0, 0.45) })
            {
                double f = Mtof(note + iv), det = Math.Pow(2, 9 / 1200.0); var lp = new Biquad("lowpass", mute ? 1100 : 3200, 1.2, Syn.SR);
                int s0 = (int)(t * Syn.SR), n = (int)((dur + 0.03) * Syn.SR); double p1 = 0, p2 = 0;
                for (int i = 0; i < n; i++)
                {
                    double tt = (double)i / Syn.SR; p1 += f / Syn.SR; p2 += f * det / Syn.SR;
                    double g = tt < 0.004 ? Syn.Exp(0.0001, v * gv, tt, 0.004) : mute ? Syn.Exp(v * gv, 0.001, tt, dur) : (tt < dur * 0.8 ? v * gv : Syn.Exp(v * gv, 0.001, tt - dur * 0.8, dur * 0.2));
                    Syn.Add(dist, s0 + i, lp.Tick((float)(Syn.Wave("sawtooth", p1) + Syn.Wave("sawtooth", p2))) * g * 1.6);
                }
            }
        }
        // an 808: a sine dropping into its note, long tail, a little grit
        public void MSub(double t, double note, double dur, double v)
        {
            double f = Mtof(note); int s0 = (int)(t * Syn.SR), n = (int)((dur + 0.05) * Syn.SR); double ph = 0;
            for (int i = 0; i < n; i++) { double tt = (double)i / Syn.SR; ph += Syn.Exp(f * 2.2, f, tt, 0.05) / Syn.SR; double y = Math.Sin(2 * Math.PI * ph) * Syn.Exp(v, 0.001, tt, dur); Syn.Add(bus, s0 + i, y); Syn.Add(dist, s0 + i, y * 0.15); }
        }
        // a noise sweep rising (or falling) over dur - builds and drops
        public void MRiser(double t, double dur, double v, bool down = false)
        {
            var bq = new Biquad("bandpass", 400, 2.5, Syn.SR); int s0 = (int)(t * Syn.SR), n = (int)(dur * Syn.SR);
            for (int i = 0; i < n; i++)
            {
                double tt = (double)i / Syn.SR, k = tt / dur; if ((i & 31) == 0) bq.SetFreq(down ? Syn.Exp(9000, 200, tt, dur) : Syn.Exp(300, 9000, tt, dur));
                double g = v * (down ? (1 - k) : k * k); Out(s0 + i, bq.Tick((float)(rng.NextDouble() * 2 - 1)) * g, true);
            }
        }
        // a plucked string (harp / harpsichord): bright attack, quick decay, into the delay
        public void MPluck(double t, double note, double v, double dec = 0.5)
        {
            double f = Mtof(note); var lp = new Biquad("lowpass", 5000, 1, Syn.SR); int s0 = (int)(t * Syn.SR), n = (int)((dec + 0.05) * Syn.SR); double ph = 0, ph2 = 0;
            for (int i = 0; i < n; i++)
            {
                double tt = (double)i / Syn.SR; ph += f / Syn.SR; ph2 += f * 2.003 / Syn.SR; if ((i & 31) == 0) lp.SetFreq(Syn.Exp(6000, 700, tt, dec));
                double y = lp.Tick((float)(Syn.Wave("triangle", ph) * 0.8 + Syn.Wave("sawtooth", ph2) * 0.25)) * Syn.Exp(v, 0.001, tt, dec);
                Syn.Add(bus, s0 + i, y); Syn.Add(delay, s0 + i, y * 0.7); Syn.Add(rev, s0 + i, y * wet);
            }
        }
        // a clock tick (hi) or tock (lo)
        public void MTick(double t, double v, bool hi) { MNoise(t, 0.025, "bandpass", hi ? 4200 : 2100, v, 9); }

        // Mix the buses down        // Mix the buses down: waveshaper, feedback delay and a Schroeder reverb, then soft-clip like the compressor.
        public float[] Mixdown(bool loop, int loopLen)
        {
            int n = bus.Length;
            var o = new float[n];
            // distortion bus (distCurve(40)) -> gain 0.32
            double k = 40;
            for (int i = 0; i < n; i++) { double x = Math.Max(-1, Math.Min(1, dist[i])); o[i] = bus[i] + (float)((3 + k) * x * 20 * Math.PI / 180 / (Math.PI + k * Math.Abs(x)) * 0.32); }
            // delay: 3/16 note, feedback 0.3, wet 0.35
            int D = (int)(60.0 / bpm * 0.75 * Syn.SR); var dl = new float[n];
            for (int i = 0; i < n; i++) { double y = (i >= D ? delay[i - D] + 0.3 * dl[i - D] : 0); dl[i] = (float)y; o[i] += (float)(y * 0.35); }
            // reverb
            int[] combs = { 1557, 1617, 1491, 1422, 1277, 1356 }; int[] aps = { 225, 556, 441 };
            var wetBuf = new float[n];
            foreach (var cd0 in combs)
            {
                int cd = cd0 * Syn.SR / 44100; var line = new float[cd]; int p = 0; double lpS = 0;
                for (int i = 0; i < n; i++) { double y = line[p]; lpS = y * 0.75 + lpS * 0.25; line[p] = (float)(rev[i] + lpS * 0.86); p = (p + 1) % cd; wetBuf[i] += (float)y; }
            }
            foreach (var ad0 in aps)
            {
                int ad = ad0 * Syn.SR / 44100; var line = new float[ad]; int p = 0;
                for (int i = 0; i < n; i++) { double bufOut = line[p]; double x = wetBuf[i]; double y = -x + bufOut; line[p] = (float)(x + bufOut * 0.5); p = (p + 1) % ad; wetBuf[i] = (float)y; }
            }
            for (int i = 0; i < n; i++) o[i] += wetBuf[i] * 0.16f;
            if (!loop) { Syn.SoftClip(o, 0.9f); return o; }
            // fold the tail past the loop point back onto the start so the loop is seamless
            var l = new float[loopLen];
            for (int i = 0; i < n; i++) l[i % loopLen] += o[i];
            Syn.SoftClip(l, 0.9f);
            return l;
        }
    }

    public static class Tracks
    {
        static readonly int[] R1 = { 40, 40, 43, 38 };
        // Boss 1: "Flesh Titan" - 160 bpm, E phrygian industrial metal: galloping double kicks, a chugging palm-muted riff,
        // war brass and a huge low choir on every phrase, a screaming lead motif, timpani hits and half-time breakdowns
        public static void Track1(MusicRender m, int i, double t, double spb)
        {
            int bar = i / 16, s = i % 16, I = m.I, ph = bar % 8; int root = R1[bar % 4];
            bool breakdown = I == 0 && bar % 16 >= 12;               // the last 4 bars of every 16 crash into half-time
            if (s == 0 && (bar % 4 == 0)) { m.MCrash(t, 0.9); m.MTimp(t, root - 12, 0.9); }
            if (s == 0 && bar % 2 == 0) m.MChoir(t, new double[] { root, root + 3, root + 7, root + 12 }, spb * 32, I == 1 ? 0.16 : 0.12);
            if (s == 0 && (bar % 4 == 0 || I == 1 && bar % 2 == 0)) m.MBrass(t, new double[] { root - 12, root - 5, root }, 0.12);
            if (bar >= 1 || I == 1)
            {
                string kp = breakdown ? "x.......x.x....." : I == 1 ? "xxxxxxxxxxxxxxxx" : (ph >= 6 ? "xx.xxx.xxx.xxxxx" : "x.xxx.xxx.xx.x.x");
                if (kp[s] == 'x') m.MKick(t, I == 1 ? 0.7 : 0.9);
                if (breakdown ? s == 8 : (s == 4 || s == 12)) m.MSnare(t, 1.0, breakdown);
                if (!breakdown && ph == 7 && s >= 12) m.MSnare(t, 0.5 + (s - 12) * 0.13);
                m.MHat(t, s % 2 == 0 ? 0.5 : 0.2, s % 4 == 2 && !breakdown);
            }
            // the riff: chugs on the root with phrygian stabs (b2, b5) - the heart of the track
            string chug = breakdown ? "x..x..x.x..x.x.." : "xx.xxx.xxx.x.xxx"; int[] off = { 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 6, 0, 0, 3, 1, 0 };
            if (chug[s] == 'x') m.MBass(t, root - 12 + off[s], spb * (breakdown ? 2.2 : 0.8), 0.6);
            if ((bar >= 4 || I == 1) && !breakdown)
            {
                int[] sc = { 0, 1, 3, 5, 7, 8, 10, 12, 13, 15 };
                int[] mot = bar % 2 == 0 ? new[] { 7, -1, 8, 7, 5, -1, 4, -1, 5, -1, 4, 3, 1, -1, 0, -1 } : new[] { 7, -1, 9, -1, 8, -1, 7, -1, 8, 7, 5, -1, 4, -1, -1, -1 };
                if (mot[s] >= 0) m.MLead(t, 52 + sc[mot[s]] + (root - 40), spb * 1.8, I == 1 ? 0.09 : 0.075);
            }
            if (breakdown && (s == 0 || s == 6 || s == 10)) m.MStab(t, new double[] { root + 12, root + 13, root + 19 }, 0.3, 0.08); // dissonant stabs
            if (I == 1 && s % 4 == 0) m.MStab(t, new double[] { root + 12, root + 19, root + 24 }, 0.12, 0.05);
        }        static readonly int[] R2 = { 50, 46, 48, 45 }; static readonly int[][] T2 = { new[] { 0, 3, 7 }, new[] { 0, 4, 7 }, new[] { 0, 4, 7 }, new[] { 0, 4, 7 } };
        // Boss 2: "Requiem of Ruin" - 132 bpm, D minor: choir, pipe organ, bells, timpani, string ostinato, cathedral drums
        public static void Track2(MusicRender m, int i, double t, double spb)
        {
            int bar = i / 16, s = i % 16, I = m.I;
            int sec = bar < 16 ? bar : 4 + ((bar - 16) % 12);
            int root = R2[bar % 4]; var tri = T2[bar % 4];
            bool full = sec >= 8 || I == 1;
            if (s == 0)
            {
                m.MChoir(t, new double[] { root + 12 + tri[0], root + 12 + tri[1], root + 12 + tri[2], root + 24 }, spb * 16, 0.2);
                if (sec >= 4 || I == 1) m.MOrgan(t, new double[] { root - 12, root - 5, root }, spb * 16, 0.06);
                m.MTimp(t, root - 12, 0.7);
                if (bar % 8 == 0) m.MCrash(t, 0.6);
                if (bar % 4 == 0 && full) m.MBrass(t, new double[] { root - 12, root, root + 7 }, 0.1);
            }
            if (sec >= 2 && (s == 0 || s == 8)) m.MBell(t, root + 36 + (s != 0 ? tri[2] : tri[0]), 0.12);
            if (bar % 4 == 3 && s >= 12) m.MTimp(t, root - 12, 0.35 + (s - 12) * 0.12);
            if (sec >= 4 || I == 1) { int[] seq = { 0, tri[2], 12, tri[2], 0, tri[2], 12, tri[1] + 12 }; m.MString(t, root + 12 + seq[s % 8], spb * 0.9, 0.05); }
            if (full)
            {
                string kp = I == 1 ? "x.xxx.xxx.xxx.xx" : "x.....x.x.......";
                if (kp[s] == 'x') m.MKick(t, I == 1 ? 0.75 : 0.95);
                if (s == 4 || s == 12) m.MSnare(t, 0.9, true);
                if (I == 1 && s % 2 == 0) m.MHat(t, 0.35, false);
            }
            if (sec >= 12 || I == 1)
            {
                int[] scale = { 0, 2, 3, 5, 7, 8, 11, 12, 14, 15 };
                int[] mel = bar % 2 == 1 ? new[] { 7, -1, -1, -1, 8, -1, 7, -1, 6, -1, -1, -1, 4, -1, -1, -1 } : new[] { 4, -1, -1, -1, 5, -1, 6, -1, 7, -1, -1, -1, 9, -1, 8, -1 };
                if (mel[s] >= 0) m.MChoirLead(t, 62 + scale[mel[s]], spb * 3.6, 0.22);
            }
        }
        static readonly int[] R3 = { 50, 45, 47, 43 }; static readonly int[][] T3 = { new[] { 0, 4, 7 }, new[] { 0, 4, 7 }, new[] { 0, 3, 7 }, new[] { 0, 4, 7 } };
        // Ending: "First Dawn" - 76 bpm, D major (I-V-vi-IV)
        public static void Track3(MusicRender m, int i, double t, double spb)
        {
            int bar = i / 16, s = i % 16; int root = R3[bar % 4]; var tri = T3[bar % 4];
            if (s == 0)
            {
                m.MChoir(t, new double[] { root + 12 + tri[0], root + 12 + tri[1], root + 12 + tri[2] }, spb * 16, 0.16);
                if (bar >= 2) m.MOrgan(t, new double[] { root - 12, root }, spb * 16, 0.04);
                if (bar >= 4 && bar % 2 == 0) m.MTimp(t, root - 12, 0.4);
                if (bar >= 8 && bar % 4 == 0) m.MBrass(t, new double[] { root - 12, root, root + 7 }, 0.07);
            }
            if (bar >= 1 && s % 2 == 0) { int[] seq = { 0, tri[1], tri[2], 12, tri[2] + 12, tri[2], tri[1], 12 }; m.MString(t, root + 12 + seq[(s / 2) % 8], spb * 1.8, 0.035); }
            if (s == 0 || s == 8) m.MBell(t, root + 36 + (s != 0 ? tri[2] : tri[0]), 0.08);
            if (bar >= 5)
            {
                int[] scale = { 0, 2, 4, 5, 7, 9, 11, 12, 14 };
                int[] mel = bar % 2 == 1 ? new[] { 4, -1, -1, -1, 5, -1, 4, -1, 2, -1, -1, -1, -1, -1, -1, -1 } : new[] { 7, -1, -1, -1, 8, -1, 7, -1, 4, -1, -1, -1, 5, -1, -1, -1 };
                if (mel[s] >= 0) m.MChoirLead(t, 62 + scale[mel[s]], spb * 3.5, 0.15);
            }
        }

        static readonly int[] R4 = { 40, 41, 43, 41 };
        // Hell levels: "Hellfire" - 150 bpm, E phrygian: chugging reese bass, double kicks, a snarling lead and a low choir underneath
        public static void Track4(MusicRender m, int i, double t, double spb)
        {
            int bar = i / 16, s = i % 16, I = m.I; int root = R4[bar % 4];
            if (s == 0 && bar % 8 == 0) m.MCrash(t, 0.8);
            if (s == 0 && bar % 2 == 0) m.MChoir(t, new double[] { root + 12, root + 15, root + 19 }, spb * 32, 0.07);
            if (bar >= 2 || I == 1)
            {
                string kp = I == 1 ? "xxxxx.xxxxxx.xxx" : (bar % 4 == 3 ? "x..x..x.x.xxxxxx" : "x..x..x.x..x..x.");
                if (kp[s] == 'x') m.MKick(t, I == 1 ? 0.75 : 0.95);
                if (s == 4 || s == 12) m.MSnare(t, 0.9, bar % 8 == 7);
                if (bar % 4 == 3 && s >= 13) m.MSnare(t, 0.45 + (s - 13) * 0.15);
            }
            m.MHat(t, s % 2 == 0 ? 0.45 : 0.2, s % 8 == 6);
            string bp = "xxx.xxx.xx.xxx.x";
            if (bp[s] == 'x') m.MBass(t, root - 12 + (s == 14 ? 1 : s == 10 ? 12 : 0), spb * 0.9, 0.55);
            if (bar >= 4 || I == 1)
            {
                int[] scale = { 0, 1, 3, 5, 7, 8, 10, 12, 13, 15 };
                int[] riff = bar % 2 == 0 ? new[] { 7, -1, 6, -1, 7, -1, 5, -1, 4, -1, 5, 4, 2, -1, 1, -1 } : new[] { 7, -1, 9, -1, 8, -1, 7, -1, 5, -1, 6, -1, 4, -1, -1, -1 };
                if (riff[s] >= 0) m.MLead(t, 52 + scale[riff[s]] + (root - 40), spb * 1.7, 0.07);
            }
            if ((I == 1 || bar % 8 >= 4) && (s == 0 || s == 6 || s == 12)) m.MStab(t, new double[] { root + 12, root + 19, root + 24 }, 0.16, 0.06);
        }
        static readonly int[] R5 = { 45, 42, 38, 40 }; static readonly int[][] T5 = { new[] { 0, 4, 7 }, new[] { 0, 3, 7 }, new[] { 0, 4, 7 }, new[] { 0, 4, 7 } };
        // Heaven levels: "Halls of Light" - 124 bpm, A major: choir pads, sparkling bells, a delayed pluck arp, four-on-the-floor
        public static void Track5(MusicRender m, int i, double t, double spb)
        {
            int bar = i / 16, s = i % 16, I = m.I; int root = R5[bar % 4]; var tri = T5[bar % 4];
            if (s == 0)
            {
                m.MChoir(t, new double[] { root + 12 + tri[0], root + 12 + tri[1], root + 12 + tri[2] }, spb * 16, 0.13);
                m.MOrgan(t, new double[] { root - 12, root }, spb * 16, 0.035);
                if (bar % 8 == 0) m.MCrash(t, 0.5);
            }
            if (s % 4 == 0) { int[] up = { 0, 1, 2, 1 }; m.MBell(t, root + 36 + tri[up[(s / 4) % 4]], 0.06); }
            int[] arp = { tri[0], tri[1], tri[2], 12, tri[2], tri[1] };
            if (bar >= 2 || I == 1) m.MLead(t, root + 24 + arp[(i) % 6], spb * 0.7, 0.045);
            if (bar >= 4 || I == 1)
            {
                if (s % 4 == 0) m.MKick(t, 0.8);
                if (s == 4 || s == 12) m.MSnare(t, 0.6, true);
                if (s % 4 == 2) m.MHat(t, 0.45, true); else if (s % 2 == 1) m.MHat(t, 0.15, false);
                if (s % 4 == 2) m.MBass(t, root - 12, spb * 1.6, 0.32);
            }
            if (I == 1)
            {
                int[] seq = { 0, tri[2], 12, tri[2] }; m.MString(t, root + 12 + seq[s % 4], spb * 0.9, 0.035);
                int[] scale = { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16 };
                int[] mel = bar % 2 == 0 ? new[] { 4, -1, -1, -1, 5, -1, 4, -1, 2, -1, -1, -1, 4, -1, -1, -1 } : new[] { 7, -1, -1, -1, 6, -1, 5, -1, 4, -1, -1, -1, -1, -1, -1, -1 };
                if (mel[s] >= 0) m.MChoirLead(t, 57 + scale[mel[s]], spb * 3.6, 0.13);
            }
        }

        static readonly int[] R6 = { 45, 41, 43, 40 };
        // Main menu: "Before the Rush" - 84 bpm, A minor: a slow organ and choir, a lonely bell melody, a heartbeat pulse
        public static void Track6(MusicRender m, int i, double t, double spb)
        {
            int bar = i / 16, s = i % 16; int root = R6[bar % 4];
            if (s == 0)
            {
                m.MOrgan(t, new double[] { root - 12, root - 5, root }, spb * 16, 0.05);
                m.MChoir(t, new double[] { root + 12, root + 15, root + 19 }, spb * 16, 0.11);
                if (bar % 4 == 0) m.MTimp(t, root - 12, 0.45);
            }
            if (bar >= 2 && (s == 0 || s == 3)) m.MKick(t, s == 0 ? 0.55 : 0.35); // heartbeat
            int[] sc = { 0, 2, 3, 5, 7, 8, 10, 12, 14, 15 };
            int[] mel = bar % 4 == 0 ? new[] { 7, -1, -1, -1, 6, -1, 4, -1, 5, -1, -1, -1, -1, -1, -1, -1 } : bar % 4 == 1 ? new[] { 4, -1, -1, -1, 3, -1, 2, -1, 0, -1, -1, -1, -1, -1, -1, -1 }
                      : bar % 4 == 2 ? new[] { 2, -1, -1, -1, 4, -1, 5, -1, 7, -1, -1, -1, 9, -1, -1, -1 } : new[] { 8, -1, -1, -1, 7, -1, -1, -1, 4, -1, -1, -1, -1, -1, -1, -1 };
            if (bar >= 1 && mel[s] >= 0) m.MBell(t, 69 + sc[mel[s]], 0.07);
            if (bar >= 4 && s % 2 == 0) { int[] arp = { 0, 7, 12, 15, 12, 7, 3, 7 }; m.MString(t, root + 12 + arp[(s / 2) % 8], spb * 1.8, 0.022); }
        }
        static readonly int[] R7 = { 38, 34, 36, 33 };
        // Dead Earth levels: "Ash and Rust" - 136 bpm, D minor industrial: wind through the ruins, metal clanging somewhere in
        // the dark, a chugging distorted bass, broken-beat drums, a lonely bell melody; the fight loop adds a snarling lead
        public static void Track7(MusicRender m, int i, double t, double spb)
        {
            int bar = i / 16, s = i % 16, I = m.I; int root = R7[bar % 4];
            if (s == 0 && bar % 2 == 0) m.MNoise(t, spb * 32, "bandpass", 420, 0.10, 0.6, true);                      // the wind
            if ((s == 0 && bar % 4 == 0) || (s == 10 && bar % 4 == 2)) { m.MNoise(t, 0.35, "bandpass", 2600, 0.35, 6, true); m.MBell(t, root + 25, 0.05); } // a clang of metal
            if (s == 0) m.MChoir(t, new double[] { root + 12, root + 15, root + 19 }, spb * 16, 0.06);
            if (bar >= 2 || I == 1)
            {
                string kp = I == 1 ? "x..x..x.x.x..x.." : "x.....x...x.....";
                if (kp[s] == 'x') m.MKick(t, 0.85);
                if (s == 4 || s == 12) m.MSnare(t, I == 1 ? 0.85 : 0.6, bar % 4 == 3);
                if (I == 1 && bar % 4 == 3 && s >= 12) m.MSnare(t, 0.4 + (s - 12) * 0.12);
                m.MHat(t, s % 2 == 0 ? (I == 1 ? 0.4 : 0.25) : 0.12, s % 8 == 6);
            }
            string bp = I == 1 ? "xx.x.xx.xx.x.x.x" : "x..x..x...x..x..";
            if (bp[s] == 'x') m.MBass(t, root - 12 + (s == 13 ? 3 : 0), spb * 1.2, 0.5);
            int[] sc = { 0, 2, 3, 5, 7, 8, 10, 12, 14, 15 };
            if (bar >= 4 || I == 1)
            {
                int[] mel = bar % 4 == 0 ? new[] { 4, -1, -1, -1, 3, -1, 2, -1, -1, -1, 0, -1, -1, -1, -1, -1 } : bar % 4 == 1 ? new[] { 2, -1, -1, -1, 3, -1, 4, -1, 5, -1, -1, -1, 4, -1, -1, -1 }
                          : bar % 4 == 2 ? new[] { 7, -1, -1, -1, 6, -1, 5, -1, 4, -1, -1, -1, 2, -1, -1, -1 } : new[] { 3, -1, -1, -1, 2, -1, 1, -1, 0, -1, -1, -1, -1, -1, -1, -1 };
                if (mel[s] >= 0) m.MBell(t, 62 + sc[mel[s]], 0.06);
            }
            if (I == 1 && bar % 2 == 1 && (s == 0 || s == 6 || s == 12)) m.MStab(t, new double[] { root + 12, root + 19, root + 24 }, 0.14, 0.05);
            if (I == 1 && bar % 8 >= 4) { int[] lead = { 7, -1, 8, -1, 7, -1, 5, -1, 3, -1, 5, -1, 2, -1, -1, -1 }; if (lead[s] >= 0) m.MLead(t, 50 + sc[lead[s]] + (root - 38), spb * 1.6, 0.05); }
        }
        static readonly int[] R8 = { 38, 38, 39, 36, 38, 38, 41, 39 };
        // The Hollow King: "CROWN OF WRECKAGE" - 176 bpm, D phrygian hardcore metal. Every 16 bars: a shattering intro hit,
        // a galloping chug riff over blast-beat double kicks and an 808 that shakes the floor, the screaming lead motif, a
        // half-time breakdown with crushing open chords and a low choir, a snare roll and riser into the DROP - everything at
        // once, the lead an octave up, war brass on every beat. The fight loop (I=1) never lets up.
        public static void Track8(MusicRender m, int i, double t, double spb)
        {
            int bar = i / 16, s = i % 16, I = m.I, sec = bar % 16; int root = R8[bar % 8];
            bool intro = sec < 2, brk = sec >= 8 && sec < 10, build = sec >= 10 && sec < 12, drop = sec >= 12;
            if (I == 1) { brk = sec >= 8 && sec < 9; build = sec == 9 || sec == 10 || sec == 11; }
            int[] sc = { 0, 1, 3, 5, 7, 8, 10, 12, 13, 15, 17, 19 };
            // hits and crashes
            if (s == 0 && (sec == 0 || sec == 12 || sec == 4 || (drop && bar % 2 == 0))) { m.MCrash(t, 1.0); m.MTimp(t, root - 12, 1.0); m.MSub(t, root - 24, spb * 16, 0.9); }
            if (s == 0 && (sec == 0 || sec == 12)) { m.MBrass(t, new double[] { root - 12, root - 6, root, root + 1 }, 0.16); m.MChoir(t, new double[] { root, root + 1, root + 7, root + 12 }, spb * 32, 0.16); }
            // drums
            if (build)
            {
                int stepN = sec == 11 || (I == 1 && sec == 11) ? 1 : 2; if (s % stepN == 0) m.MSnare(t, 0.35 + 0.6 * ((sec - 10) * 16 + s) / 32.0);
                if (s % 4 == 0) m.MKick(t, 0.9); if (s == 0 && (sec == 10 || (I == 1 && sec == 9))) m.MRiser(t, spb * 32, 0.5);
            }
            else if (brk)
            {
                if ("x.......x..x..x.".Substring(s, 1) == "x") m.MKick(t, 1.0);
                if (s == 8) m.MSnare(t, 1.0, true); m.MHat(t, s % 4 == 0 ? 0.5 : 0.18, s % 8 == 4);
            }
            else if (!intro || I == 1)
            {
                string kp = drop || I == 1 ? "xxxxxxxxxxxxxxxx" : "x.xxx.xxx.xxx.xx"; if (kp[s] == 'x') m.MKick(t, drop ? 0.75 : 0.85);
                if (s == 4 || s == 12) m.MSnare(t, 1.0, s == 12 && bar % 4 == 3); if (drop && (s == 6 || s == 14)) m.MSnare(t, 0.45);
                m.MHat(t, s % 2 == 0 ? 0.55 : 0.25, s % 4 == 2); if (s % 8 == 0) m.MNoise(t, 0.2, "bandpass", 3400, 0.3, 9, true); // the anvil
            }
            else { if (s % 4 == 0) m.MKick(t, 0.7); if (s == 8) m.MSnare(t, 0.6, true); }
            // the riff: galloping palm-mutes with open-chord stabs; the breakdown holds huge open chords
            if (brk) { if (s == 0 || s == 11 || s == 14) m.MGuitar(t, root - 12 + (s == 14 ? 1 : 0), spb * (s == 0 ? 10 : 3), 0.55, false); if (s == 0) m.MSub(t, root - 24, spb * 8, 0.9); }
            else if (!intro || s % 4 == 0)
            {
                string rp = "x.xxx.xxx.xx.xx."; int[] off = { 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 6, 0, 1, 0 };
                if (rp[s] == 'x') { bool open = s == 0 || s == 12; m.MGuitar(t, root - 12 + off[s], spb * (open ? 1.8 : 0.8), open ? 0.5 : 0.42, !open); }
                if (s == 0 || s == 6 || s == 10) m.MSub(t, root - 24, spb * 3, 0.65);
            }
            // the lead: a screaming phrygian motif in the main riff, an octave up and doubled by the choir in the drop
            if ((sec >= 4 && sec < 8) || drop || (I == 1 && !brk && !build))
            {
                int[] a = { 7, -1, 8, 7, 5, -1, 4, -1, 5, -1, 4, 3, 1, -1, 0, -1 }, b = { 7, -1, 9, -1, 8, -1, 7, -1, 10, 9, 8, -1, 7, -1, 5, -1 };
                var mel = bar % 2 == 0 ? a : b; int up = drop ? 12 : 0;
                if (mel[s] >= 0) { m.MLead(t, 62 + sc[mel[s]] + (root - 38) + up, spb * 1.7, drop ? 0.1 : 0.085); if (drop && s % 4 == 0) m.MChoirLead(t, 62 + sc[mel[s]] + (root - 38), spb * 3.5, 0.12); }
            }
            if (drop && s % 4 == 0) m.MBrass(t, new double[] { root - 12, root, root + 7 }, 0.09);
            if (brk && (s == 0 || s == 8)) m.MChoir(t, new double[] { root - 12, root - 11, root - 5 }, spb * 8, 0.14);
            if (sec == 7 && s == 8) m.MRiser(t, spb * 8, 0.35);
        }        static readonly int[] R9 = { 47, 43, 45, 42 };
        // Frozen Circle levels: "Frostbound" - 108 bpm, B minor: a choir like wind over ice, glassy bell arpeggios, a slow
        // heartbeat kick, a string ostinato coming in, a cold choir melody; the fight loop adds a fuller kit and timpani
        public static void Track9(MusicRender m, int i, double t, double spb)
        {
            int bar = i / 16, s = i % 16, I = m.I; int root = R9[bar % 4]; var tri = bar % 4 == 0 ? new[] { 0, 3, 7 } : new[] { 0, 4, 7 };
            if (s == 0) { m.MChoir(t, new double[] { root + tri[0], root + tri[1], root + tri[2] }, spb * 16, 0.09); if (bar % 2 == 0) m.MNoise(t, spb * 32, "highpass", 3000, 0.08, 0.5, true); }
            if (s % 2 == 0) { int[] arp = { 0, tri[1], tri[2], 12, tri[2] + 12, 12, tri[2], tri[1] }; m.MBell(t, root + 24 + arp[(s / 2) % 8], 0.035); }
            if (bar >= 2 || I == 1)
            {
                if (s == 0 || s == 10) m.MKick(t, 0.7);
                if (s == 8) m.MSnare(t, 0.5, true);
                if (s % 4 == 2) m.MHat(t, 0.25, false); if (I == 1 && s % 2 == 1) m.MHat(t, 0.12, false);
                if (s == 0 || s == 6 || s == 10) m.MBass(t, root - 12, spb * 2.4, 0.35);
            }
            if (bar >= 4 || I == 1) { int[] seq = { 0, tri[2], 12, tri[2] }; m.MString(t, root + 12 + seq[s % 4], spb * 0.9, I == 1 ? 0.035 : 0.022); }
            if (bar >= 6 || I == 1)
            {
                int[] sc = { 0, 2, 3, 5, 7, 8, 10, 12, 14, 15 };
                int[] mel = bar % 2 == 0 ? new[] { 4, -1, -1, -1, 3, -1, 2, -1, 0, -1, -1, -1, 2, -1, -1, -1 } : new[] { 7, -1, -1, -1, 5, -1, 4, -1, 3, -1, -1, -1, -1, -1, -1, -1 };
                if (mel[s] >= 0) m.MChoirLead(t, 59 + sc[mel[s]], spb * 3.4, 0.12);
            }
            if (I == 1 && s == 0 && bar % 4 == 0) m.MTimp(t, root - 12, 0.5);
        }
        static readonly int[] R10 = { 42, 38, 40, 37 };
        // The Frozen Traitor: "Throne of Ice" - 152 bpm, F# harmonic minor: choir and pipe organ, a racing string ostinato,
        // shards of bells, timpani on every bar, war brass, double kicks, a soaring lead doubled by the choir in the fury,
        // and breakdowns of dissonant stabs
        public static void Track10(MusicRender m, int i, double t, double spb)
        {
            int bar = i / 16, s = i % 16, I = m.I; int root = R10[bar % 4]; bool brk = I == 0 && bar % 16 >= 12;
            if (s == 0) { m.MChoir(t, new double[] { root + 12, root + 15, root + 19, root + 24 }, spb * 16, I == 1 ? 0.17 : 0.13); m.MOrgan(t, new double[] { root - 12, root - 5, root }, spb * 16, 0.05); m.MTimp(t, root - 12, 0.8); if (bar % 4 == 0) m.MCrash(t, 0.8); }
            if (s == 8 && bar % 2 == 1) m.MBrass(t, new double[] { root - 12, root, root + 7 }, 0.11);
            if (s % 4 == 0) m.MBell(t, root + 36 + (s == 0 ? 0 : s == 4 ? 7 : s == 8 ? 3 : 10), 0.05);
            { int[] seq = { 0, 7, 12, 7, 3, 7, 12, 15 }; m.MString(t, root + 12 + seq[s % 8], spb * 0.8, brk ? 0.03 : 0.045); }
            if (bar >= 1 || I == 1)
            {
                string kp = brk ? "x.......x......." : I == 1 ? "xxxxxxxxxxxxxxxx" : "x.xxx.xxx.xxx.xx";
                if (kp[s] == 'x') m.MKick(t, I == 1 ? 0.7 : 0.9);
                if (brk ? s == 8 : (s == 4 || s == 12)) m.MSnare(t, 0.95, true);
                m.MHat(t, s % 2 == 0 ? 0.45 : 0.2, s % 8 == 6);
            }
            if (!brk) { string bp = "x.xx.xx.x.xx.x.x"; if (bp[s] == 'x') m.MBass(t, root - 12 + (s == 14 ? 1 : 0), spb * 0.9, 0.5); }
            if ((bar >= 4 || I == 1) && !brk)
            {
                int[] sc = { 0, 2, 3, 5, 7, 8, 11, 12, 14, 15 };
                int[] mel = bar % 2 == 0 ? new[] { 7, -1, -1, 8, 7, -1, 6, -1, 4, -1, -1, -1, 6, -1, 7, -1 } : new[] { 9, -1, 8, -1, 7, -1, 6, -1, 7, -1, -1, -1, -1, -1, -1, -1 };
                if (mel[s] >= 0) { m.MLead(t, 66 + sc[mel[s]] + (root - 42), spb * 1.8, 0.07); if (I == 1) m.MChoirLead(t, 66 + sc[mel[s]] + (root - 42), spb * 1.8, 0.1); }
            }
            if (brk && (s == 0 || s == 6 || s == 12)) m.MStab(t, new double[] { root + 12, root + 15, root + 18 }, 0.3, 0.08);
        }
        static readonly int[] R11 = { 38, 34, 36, 33, 38, 34, 41, 40 };
        // The Warden of Hours: "THE LAST HOUR" - 152 bpm, D harmonic minor clockwork metal. A clock ticks through all of it
        // (tick . tock, then racing), a racing harpsichord ostinato, pipe organ and choir, a bell tolling the bar, a riff that
        // lurches in 3-3-2 like gears catching, 808s, double kicks - and TIME STOPS: at the end of a phrase everything cuts out
        // but a reversed swell and a single tick... then slams back in. The drop brings war brass, the choir and a soaring lead.
        public static void Track11(MusicRender m, int i, double t, double spb)
        {
            int bar = i / 16, s = i % 16, I = m.I, sec = bar % 16; int root = R11[bar % 8];
            bool intro = sec < 2, brk = sec >= 8 && sec < 10, build = sec >= 10 && sec < 12, drop = sec >= 12;
            bool stop = (sec == 7 || sec == 15) && s >= 10; // the time stop
            int[] sc = { 0, 2, 3, 5, 7, 8, 11, 12, 14, 15, 17, 19 };
            if (stop) { if (s == 10) m.MRiser(t, spb * 6, 0.45); if (s == 15) m.MTick(t, 0.7, true); if (s == 12) m.MBell(t, root + 24, 0.06); return; }
            // the clock
            if (s % 4 == 0) m.MTick(t, 0.32, s % 8 == 0); if ((drop || build || I == 1) && s % 2 == 1) m.MTick(t, 0.14, true);
            if (s == 0) { m.MBell(t, root + 12, bar % 2 == 0 ? 0.12 : 0.07); if (sec % 4 == 0) { m.MCrash(t, 0.8); m.MTimp(t, root - 12, 0.9); m.MSub(t, root - 24, spb * 16, 0.8); } }
            if (s == 0 && (bar % 2 == 0)) { m.MOrgan(t, new double[] { root - 12, root - 5, root, root + 3 }, spb * 32, 0.05); m.MChoir(t, new double[] { root + 12, root + 15, root + 19 }, spb * 32, drop ? 0.16 : 0.11); }
            // the racing ostinato (harpsichord)
            { int[] seq = { 0, 12, 7, 12, 3, 12, 7, 15, 0, 12, 8, 12, 3, 12, 11, 12 }; if (!brk) m.MPluck(t, root + 24 + seq[s], drop ? 0.07 : 0.055, 0.25); }
            // drums
            if (build) { if (s % (sec == 11 ? 1 : 2) == 0) m.MSnare(t, 0.3 + 0.6 * ((sec - 10) * 16 + s) / 32.0); if (s % 4 == 0) m.MKick(t, 0.9); if (sec == 10 && s == 0) m.MRiser(t, spb * 32, 0.5); }
            else if (brk) { if ("x.....x...x.....".Substring(s, 1) == "x") m.MKick(t, 1.0); if (s == 8) m.MSnare(t, 1.0, true); }
            else if (!intro || I == 1)
            {
                string kp = drop || I == 1 ? "xx.xxx.xxx.xxx.x" : "x..x..x.x..x..x."; if (kp[s] == 'x') m.MKick(t, 0.8);
                if (s == 4 || s == 12) m.MSnare(t, 0.95, s == 12); m.MHat(t, s % 2 == 0 ? 0.4 : 0.15, s % 8 == 6);
            }
            // the riff: 3-3-2, gears catching
            if (brk) { if (s == 0 || s == 6 || s == 10) { m.MGuitar(t, root - 12, spb * (s == 0 ? 6 : 4), 0.5, false); m.MSub(t, root - 24, spb * 4, 0.8); } }
            else if (!intro)
            {
                string rp = "x..x..x.x..x..x."; if (rp[s] == 'x') m.MGuitar(t, root - 12 + (s == 14 ? 1 : 0), spb * (s % 8 == 0 ? 1.6 : 0.9), 0.45, s % 8 != 0);
                if (s == 0 || s == 6 || s == 8 || s == 14) m.MSub(t, root - 24, spb * 2.5, 0.55);
            }
            // the lead
            if ((sec >= 4 && sec < 8) || drop)
            {
                int[] a = { 7, -1, -1, 8, 7, -1, 6, -1, 4, -1, 5, -1, 6, -1, 7, -1 }, b = { 9, -1, 8, -1, 7, -1, 6, -1, 7, -1, 8, 9, 10, -1, -1, -1 };
                var mel = bar % 2 == 0 ? a : b;
                if (mel[s] >= 0) { m.MLead(t, 62 + sc[mel[s]] + (root - 38) + (drop ? 12 : 0), spb * 1.8, drop ? 0.09 : 0.075); if (drop) m.MChoirLead(t, 62 + sc[mel[s]] + (root - 38), spb * 1.8, 0.1); }
            }
            if (drop && (s == 0 || s == 8)) m.MBrass(t, new double[] { root - 12, root, root + 7 }, 0.1);
            if (brk && s == 0) m.MChoir(t, new double[] { root - 12, root - 9, root - 5 }, spb * 16, 0.14);
        }
        static readonly int[] R12 = { 38, 34, 41, 36, 38, 34, 33, 33 };
        // Purgatory levels: "THE WAITING SHORE" - 104 bpm, D minor. The grey between: a cold choir breathing in and out like
        // the tide, a harp turning slowly in the fog, a distant bell, a clock ticking somewhere, a heartbeat kick; then a low
        // string ostinato and a lonely, singing lead over it. The fight loop (I=1) brings in a driving kit, a deep pulse of
        // bass, war drums and the choir carrying the melody.
        public static void Track12(MusicRender m, int i, double t, double spb)
        {
            int bar = i / 16, s = i % 16, I = m.I, sec = bar % 16; int root = R12[bar % 8];
            bool minor = root == 38 || root == 33; var tri = minor ? new[] { 0, 3, 7 } : new[] { 0, 4, 7 };
            if (s == 0) { m.MChoir(t, new double[] { root + 12 + tri[0], root + 12 + tri[1], root + 12 + tri[2] }, spb * 16, I == 1 ? 0.12 : 0.1); if (bar % 2 == 0) m.MNoise(t, spb * 32, "bandpass", 500, 0.08, 0.6, true); } // the tide
            if (s == 0 && bar % 4 == 0) m.MBell(t, root + 24, 0.07);
            if (s % 8 == 4) m.MTick(t, 0.12, s == 4);
            // the harp, turning in the fog
            { int[] arp = { 0, 1, 2, 4, 2, 1, 3, 1 }; int[] notes = { root + 12 + tri[0], root + 12 + tri[1], root + 12 + tri[2], root + 24 + tri[1], root + 24 }; if (s % 2 == 0) m.MPluck(t, notes[arp[(s / 2) % 8] % 5], 0.05, 0.9); }
            // heartbeat, then the kit
            if (I == 0) { if (s == 0 || s == 2) m.MKick(t, s == 0 ? 0.55 : 0.35); if (bar >= 4 && s == 8) m.MSnare(t, 0.3, true); }
            else
            {
                string kp = "x..x..x...x..x.."; if (kp[s] == 'x') m.MKick(t, 0.8);
                if (s == 4 || s == 12) m.MSnare(t, 0.75, s == 12); m.MHat(t, s % 2 == 0 ? 0.28 : 0.12, s % 8 == 6);
                if (s == 0 && bar % 2 == 0) m.MTimp(t, root - 12, 0.6);
                if ("x..x..x.x..x..x.".Substring(s, 1) == "x") m.MBass(t, root - 12, spb * 1.4, 0.4);
            }
            if (sec >= 4 || I == 1) { int[] seq = { 0, 7, 12, 7 }; if (s % 2 == 0) m.MString(t, root + seq[(s / 2) % 4], spb * 1.8, I == 1 ? 0.035 : 0.025); }
            // the lonely lead (and in the fight the choir sings it with him)
            if (sec >= 6 || I == 1)
            {
                int[] dsc = { 0, 2, 3, 5, 7, 8, 10, 12, 14, 15 };
                int[][] phr = { new[] { 4, -1, -1, -1, 3, -1, 2, -1, 1, -1, -1, -1, 0, -1, -1, -1 }, new[] { 2, -1, -1, -1, 3, -1, 4, -1, 5, -1, -1, -1, 4, -1, -1, -1 },
                                new[] { 7, -1, -1, -1, 6, -1, 5, -1, 4, -1, -1, -1, 2, -1, 3, -1 }, new[] { 1, -1, -1, -1, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 } };
                int n = phr[bar % 4][s];
                if (n >= 0) { m.MLead(t, 62 + dsc[n], spb * 3.5, 0.05); if (I == 1) m.MChoirLead(t, 62 + dsc[n], spb * 3.5, 0.09); }
            }
        }        static readonly int[] R13 = { 45, 41, 43, 40, 45, 41, 38, 40 };
        // CLOCKWORK HALLS: "MAINSPRING" - 132 bpm, A minor. The inside of a clock: tick-tock percussion locked to the grid, a
        // ratchet rattling up, a staccato harpsichord ostinato like an escapement, a lurching 3-3-2 bass of grinding gears, a
        // bell striking the quarters, brass motifs; the fight loop adds a mechanical kit and a driving lead.
        public static void Track13(MusicRender m, int i, double t, double spb)
        {
            int bar = i / 16, s = i % 16, I = m.I, sec = bar % 8; int root = R13[bar % 8];
            m.MTick(t, s % 4 == 0 ? 0.5 : 0.18, s % 8 == 0);
            if (s >= 12 && bar % 4 == 3) m.MNoise(t, 0.02, "bandpass", 3000 + s * 200, 0.3, 6); // the ratchet winding
            if (s == 0 && bar % 4 == 0) { m.MBell(t, root + 24, 0.1); m.MTimp(t, root - 12, 0.6); }
            if (s == 0 && bar % 2 == 0) m.MOrgan(t, new double[] { root - 12, root, root + 3, root + 7 }, spb * 32, 0.04);
            { int[] seq = { 0, 12, 7, 12, 3, 12, 7, 12, 0, 12, 8, 12, 3, 12, 11, 12 }; m.MPluck(t, root + 24 + seq[s], 0.075, 0.22); }
            if ("x..x..x.x..x..x.".Substring(s, 1) == "x") m.MBass(t, root - 12 + (s == 14 ? 2 : 0), spb * 1.2, 0.5);
            if (I == 1 || bar >= 4) { if ("x..x..x...x.x...".Substring(s, 1) == "x") m.MKick(t, 0.85); if (s == 4 || s == 12) m.MSnare(t, 0.8, s == 12); if (I == 1) m.MHat(t, s % 2 == 0 ? 0.35 : 0.14, s % 8 == 6); }
            if (sec >= 2 || I == 1)
            {
                int[] sc = { 0, 2, 3, 5, 7, 8, 11, 12, 14, 15 }; int[] a = { 7, -1, 6, -1, 7, -1, 4, -1, 5, -1, 4, -1, 2, -1, -1, -1 }, b = { 3, -1, 4, -1, 5, -1, 7, -1, 8, -1, 7, -1, 6, -1, 7, -1 };
                var mel = bar % 2 == 0 ? a : b; if (mel[s] >= 0) m.MLead(t, 57 + sc[mel[s]] + (root - 45), spb * 1.5, I == 1 ? 0.075 : 0.06);
            }
            if (I == 1 && s == 0 && bar % 2 == 1) m.MBrass(t, new double[] { root - 12, root, root + 7 }, 0.1);
        }
        static readonly int[] R14 = { 41, 38, 46, 45, 41, 38, 43, 45 };
        // THE LIBRARY OF SOULS: "THE UNREAD" - 92 bpm, D minor/F. Hushed and haunted: a whispering choir, pages turning (soft
        // noise sweeps), a music-box melody, a celesta of bells, a slow waltz-like pulse in 6, an old organ; the fight loop
        // adds a heavy, slow-swinging kit, cellos and the choir crying out the theme.
        public static void Track14(MusicRender m, int i, double t, double spb)
        {
            int bar = i / 16, s = i % 16, I = m.I; int root = R14[bar % 8];
            bool maj = root == 41 || root == 46 || root == 43; var tri = maj ? new[] { 0, 4, 7 } : new[] { 0, 3, 7 };
            if (s == 0) { m.MChoir(t, new double[] { root + 12 + tri[0], root + 12 + tri[1], root + 12 + tri[2] }, spb * 16, I == 1 ? 0.14 : 0.11); m.MOrgan(t, new double[] { root - 12, root }, spb * 16, 0.04); }
            if (s == 6 && bar % 2 == 1) m.MRiser(t, spb * 6, 0.12, true); // a page turning
            // the music box
            { int[] mb = { 12, -1, 19, -1, 24, -1, 19, 22, 24, -1, 15 + tri[1] - 3, -1, 19, -1, 12, -1 }; if (mb[s] >= 0) m.MBell(t, root + 24 + (mb[s] - 12), 0.045); }
            if (s % 4 == 0) m.MPluck(t, root + (s % 8 == 0 ? 0 : 7), 0.07, 0.9);
            if (I == 0) { if (s == 0) m.MKick(t, 0.5); if (s == 8 && bar >= 2) m.MSnare(t, 0.25, true); }
            else { if (s == 0 || s == 10) m.MKick(t, 0.9); if (s == 8) m.MSnare(t, 0.8, true); m.MHat(t, s % 4 == 0 ? 0.25 : 0.1, false); if (s % 4 == 0) m.MString(t, root - 12 + tri[s / 4 % 3], spb * 3.6, 0.05); if (s == 0 && bar % 4 == 0) m.MTimp(t, root - 12, 0.7); }
            if (bar >= 4 || I == 1)
            {
                int[] sc = { 0, 2, 3, 5, 7, 8, 10, 12, 14, 15 }; int[][] ph = { new[] { 4, -1, -1, -1, 3, -1, 2, -1, 3, -1, -1, -1, -1, -1, -1, -1 }, new[] { 5, -1, -1, -1, 4, -1, 3, -1, 2, -1, -1, -1, 0, -1, -1, -1 } };
                int n = ph[bar % 2][s]; if (n >= 0) { m.MLead(t, 62 + sc[n] + (root - 38) % 12, spb * 3.5, 0.05); if (I == 1) m.MChoirLead(t, 62 + sc[n] + (root - 38) % 12, spb * 3.5, 0.12); }
            }
        }
        static readonly int[] R15 = { 44, 40, 43, 47, 44, 40, 41, 43 };
        // MIRROR WASTES: "GLASS HORIZON" - 118 bpm, G# minor. Shimmering and desolate: glassy bells in canon (each phrase echoed
        // a beat later, a fifth up - a reflection), a wind of noise across the sand, a deep drone, a reversed swell into each
        // phrase, a hollow tom pulse; the fight loop drives it with a tribal kit, sub bass and a lead mirrored by the choir.
        public static void Track15(MusicRender m, int i, double t, double spb)
        {
            int bar = i / 16, s = i % 16, I = m.I; int root = R15[bar % 8];
            if (s == 0) { m.MNoise(t, spb * 16, "bandpass", 900 + (bar % 4) * 300, 0.12, 0.5, true); m.MChoir(t, new double[] { root, root + 7, root + 15 }, spb * 16, 0.08); if (bar % 2 == 0) m.MSub(t, root - 24, spb * 16, 0.5); }
            if (s == 12) m.MRiser(t, spb * 4, 0.2);
            int[] sc = { 0, 3, 5, 7, 10, 12, 15, 17, 19 }; int[] a = { 5, -1, 3, -1, 4, -1, 2, -1, 3, -1, 1, -1, 0, -1, -1, -1 };
            if (a[s] >= 0) { m.MBell(t, root + 24 + sc[a[s]], 0.07); if (s + 4 < 16 || true) m.MBell(t + spb * 3, root + 31 + sc[a[s]], 0.035); } // the reflection
            if (s % 2 == 0) m.MPluck(t, root + 12 + sc[(s / 2 + bar) % 5], 0.035, 0.35);
            if (I == 0) { if (s == 0 || s == 7) m.MKick(t, 0.5); if (s == 10) m.MNoise(t, 0.15, "lowpass", 300, 0.4, 2, true); }
            else { if ("x..x..x.x.x..x..".Substring(s, 1) == "x") m.MKick(t, 0.85); if (s == 4 || s == 12) m.MSnare(t, 0.7, true); if (s % 2 == 1) m.MNoise(t, 0.06, "bandpass", 1200, 0.25, 4); if ("x..x..x...x.....".Substring(s, 1) == "x") m.MSub(t, root - 24, spb * 2, 0.6); }
            if (bar >= 4 || I == 1)
            {
                int[] l = bar % 2 == 0 ? new[] { 3, -1, -1, 4, 5, -1, 4, -1, 3, -1, 2, -1, 1, -1, -1, -1 } : new[] { 5, -1, -1, 6, 7, -1, 6, -1, 5, -1, -1, -1, 3, -1, -1, -1 };
                if (l[s] >= 0) { m.MLead(t, 56 + sc[l[s]] + (root - 44), spb * 2.5, 0.06); if (I == 1) m.MChoirLead(t, 56 + sc[l[s]] + (root - 44) + 7, spb * 2.5, 0.09); }
            }
        }        // Render steps [i0, i1) of a track. loop=true folds the reverb tail back onto the start.
        public static float[] Render(Action<MusicRender, int, double, double> fn, double bpm, double wet, int I, int i0, int i1, bool loop, bool impact, int seed)
        {
            double spb = 60 / bpm / 4, len = (i1 - i0) * spb;
            var m = new MusicRender(len + 3.2, bpm, wet, seed) { I = I };
            if (impact) m.MImpact(0);
            for (int i = i0; i < i1; i++) fn(m, i, (i - i0) * spb, spb);
            return m.Mixdown(loop, (int)(len * Syn.SR));
        }
    }
}
