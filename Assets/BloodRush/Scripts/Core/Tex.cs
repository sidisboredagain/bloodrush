using UnityEngine;

namespace BR
{
    // Minimal CPU 2D canvas (source-over blending) used to recreate the HTML game's canvas textures.
    // Row 0 is the TOP of the image, like an HTML canvas; it is flipped on upload (three.js flipY).
    public class Cv
    {
        public readonly int w, h; public readonly Color[] p;
        public Cv(int w, int h) { this.w = w; this.h = h; p = new Color[w * h]; }
        public void Blend(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= w || y >= h || c.a <= 0) return;
            int i = y * w + x; var d = p[i]; float a = c.a, oa = a + d.a * (1 - a);
            if (oa <= 0) { p[i] = new Color(0, 0, 0, 0); return; }
            p[i] = new Color((c.r * a + d.r * d.a * (1 - a)) / oa, (c.g * a + d.g * d.a * (1 - a)) / oa, (c.b * a + d.b * d.a * (1 - a)) / oa, oa);
        }
        public void Fill(Color c) { for (int i = 0; i < p.Length; i++) p[i] = c; }
        public void Rect(float x, float y, float rw, float rh, Color c)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y), x1 = Mathf.CeilToInt(x + rw), y1 = Mathf.CeilToInt(y + rh);
            for (int yy = y0; yy < y1; yy++) for (int xx = x0; xx < x1; xx++) Blend(xx, yy, c);
        }
        public void StrokeRect(float x, float y, float rw, float rh, float lw, Color c)
        {
            Rect(x - lw / 2, y - lw / 2, rw + lw, lw, c); Rect(x - lw / 2, y + rh - lw / 2, rw + lw, lw, c);
            Rect(x - lw / 2, y + lw / 2, lw, rh - lw, c); Rect(x + rw - lw / 2, y + lw / 2, lw, rh - lw, c);
        }
        public void Ellipse(float cx, float cy, float rx, float ry, float rot, Color c)
        {
            float rr = Mathf.Max(rx, ry), cs = Mathf.Cos(rot), sn = Mathf.Sin(rot);
            for (int y = Mathf.FloorToInt(cy - rr); y <= Mathf.CeilToInt(cy + rr); y++)
                for (int x = Mathf.FloorToInt(cx - rr); x <= Mathf.CeilToInt(cx + rr); x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy, u = dx * cs + dy * sn, v = -dx * sn + dy * cs;
                    if (u * u / (rx * rx) + v * v / (ry * ry) <= 1) Blend(Wrap(x, w), y, c);
                }
        }
        static int Wrap(int x, int w) => ((x % w) + w) % w;
        public void Circle(float cx, float cy, float r, Color c) => Ellipse(cx, cy, r, r, 0, c);
        // radial gradient between colour stops (t in 0..1 of radius), painted over a circle
        public void Radial(float cx, float cy, float r, params (float t, Color c)[] stops)
        {
            for (int y = Mathf.FloorToInt(cy - r); y <= Mathf.CeilToInt(cy + r); y++)
                for (int x = Mathf.FloorToInt(cx - r); x <= Mathf.CeilToInt(cx + r); x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy)) / r;
                    if (d > 1) continue;
                    Blend(x, y, Stop(stops, d));
                }
        }
        static Color Stop((float t, Color c)[] s, float d)
        {
            if (d <= s[0].t) return s[0].c;
            for (int i = 1; i < s.Length; i++) if (d <= s[i].t) return Color.Lerp(s[i - 1].c, s[i].c, (d - s[i - 1].t) / Mathf.Max(1e-5f, s[i].t - s[i - 1].t));
            return s[s.Length - 1].c;
        }
        public void Line(float x0, float y0, float x1, float y1, float lw, Color c)
        {
            float minx = Mathf.Min(x0, x1) - lw, maxx = Mathf.Max(x0, x1) + lw, miny = Mathf.Min(y0, y1) - lw, maxy = Mathf.Max(y0, y1) + lw;
            var a = new Vector2(x0, y0); var ab = new Vector2(x1 - x0, y1 - y0); float len2 = Mathf.Max(ab.sqrMagnitude, 1e-5f);
            for (int y = Mathf.FloorToInt(miny); y <= Mathf.CeilToInt(maxy); y++)
                for (int x = Mathf.FloorToInt(minx); x <= Mathf.CeilToInt(maxx); x++)
                {
                    var pp = new Vector2(x + 0.5f, y + 0.5f); float t = Mathf.Clamp01(Vector2.Dot(pp - a, ab) / len2);
                    if ((a + ab * t - pp).magnitude <= lw / 2) Blend(x, y, c);
                }
        }
        public void Tri(Vector2 a, Vector2 b, Vector2 c, Color col)
        {
            int x0 = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), x1 = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
            int y0 = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), y1 = Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
            for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
                {
                    var q = new Vector2(x + 0.5f, y + 0.5f);
                    float d1 = Cross(q, a, b), d2 = Cross(q, b, c), d3 = Cross(q, c, a);
                    bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
                    if (!(neg && pos)) Blend(x, y, col);
                }
        }
        static float Cross(Vector2 p, Vector2 a, Vector2 b) => (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);

        public Texture2D ToTex(bool point, bool repeat, bool mips = true)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, mips, false);
            var flipped = new Color[w * h];
            for (int y = 0; y < h; y++) System.Array.Copy(p, y * w, flipped, (h - 1 - y) * w, w);
            t.SetPixels(flipped); t.Apply(mips);
            t.filterMode = point ? FilterMode.Point : FilterMode.Bilinear;
            t.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            t.anisoLevel = 0;
            return t;
        }
    }

    public static class Tex
    {
        static float R => Random.value;
        static Color A(Color c, float a) { c.a = a; return c; }
        public static Texture2D Glow, Flash, Lava, Holy, Swirl, Stained, Circle, Vignette, ScreenBg, Scan, Rays, ScopeMask, Ring;

        public static void Init()
        {
            Glow = MakeGlow(); Flash = MakeFlash(); Swirl = MakeSwirl(); Stained = MakeStained();
            Lava = Blob("#8a0c00", new[] { "#ffe070", "#ff6a10", "#ff6a10" });
            Holy = Blob("#7fd8f0", new[] { "#ffffff", "#fff2c0", "#c8f4ff" });
            Circle = MakeCircle(); Vignette = MakeVignette(); ScreenBg = MakeScreenBg(); Scan = MakeScan(); Rays = MakeRays(); ScopeMask = MakeScopeMask(); Ring = MakeRing(); DamageArc = MakeDamageArc(); Cracks = MakeCracks(); RayFade = MakeRayFade();
        }

        // a blood splat on transparent: a lumpy pool, satellite drops and a few flung streaks (tinted by the material)
        static Texture2D bloodSplat;
        public static Texture2D BloodSplat => bloodSplat ? bloodSplat : (bloodSplat = MakeBloodSplat());
        static Texture2D MakeBloodSplat()
        {
            var c = new Cv(128, 128); var w = new Color(1, 1, 1, 1);
            for (int i = 0; i < 9; i++) { float a = R * 6.28f, r = R * 14; c.Ellipse(64 + Mathf.Cos(a) * r, 64 + Mathf.Sin(a) * r, 14 + R * 14, 11 + R * 10, R * 3, w); }
            for (int i = 0; i < 9; i++)
            {
                float a = R * 6.28f, len = 22 + R * 34; float x0 = 64 + Mathf.Cos(a) * 18, y0 = 64 + Mathf.Sin(a) * 18;
                c.Line(x0, y0, 64 + Mathf.Cos(a) * len, 64 + Mathf.Sin(a) * len, 2 + R * 3, w);
                c.Circle(64 + Mathf.Cos(a) * (len + 3), 64 + Mathf.Sin(a) * (len + 3), 2 + R * 3, w);
            }
            for (int i = 0; i < 26; i++) { float a = R * 6.28f, r = 30 + R * 30; c.Circle(64 + Mathf.Cos(a) * r, 64 + Mathf.Sin(a) * r, 0.8f + R * 2.2f, w); }
            for (int i = 0; i < c.p.Length; i++) { var p = c.p[i]; if (p.a > 0) { float shade = 0.75f + 0.25f * R; c.p[i] = new Color(shade, shade, shade, p.a); } }
            return c.ToTex(false, false);
        }
        // old bone: ivory with long fibres, pores, hairline cracks and brown stains (tiles both ways)
        static Texture2D bone;
        public static Texture2D Bone => bone ? bone : (bone = MakeBone());
        static Texture2D MakeBone()
        {
            var c = new Cv(128, 128);
            c.Fill(new Color(0.84f, 0.79f, 0.66f));
            for (int i = 0; i < 26; i++) { float x = R * 128, y = R * 128; c.Ellipse(x, y, 10 + R * 22, 6 + R * 12, R * 3, new Color(0.62f, 0.5f, 0.34f, 0.18f)); } // stains
            for (int i = 0; i < 70; i++) { float y = R * 128, x = R * 128, l = 20 + R * 60; c.Line(x, y, x + l, y + (R - 0.5f) * 4, 1, R < 0.5f ? new Color(1, 0.97f, 0.88f, 0.25f) : new Color(0.55f, 0.46f, 0.33f, 0.22f)); } // fibres
            for (int i = 0; i < 160; i++) c.Circle(R * 128, R * 128, 0.6f + R * 1.1f, new Color(0.35f, 0.27f, 0.18f, 0.45f)); // pores
            for (int i = 0; i < 7; i++) { float x = R * 128, y = R * 128; for (int k = 0; k < 5; k++) { float nx = x + (R - 0.5f) * 18, ny = y + (R - 0.3f) * 14; c.Line(x, y, nx, ny, 1, new Color(0.28f, 0.2f, 0.13f, 0.6f)); x = nx; y = ny; } } // cracks
            return c.ToTex(false, true);
        }
        // tiled surface texture (makeTex in the HTML game)
        public static Texture2D Surface(string bg, string line, string accent, string kind)
        {
            var c = new Cv(64, 64);
            c.Fill(Mats.Css(bg));
            for (int i = 0; i < 260; i++) c.Rect((int)(R * 64), (int)(R * 64), 2, 2, new Color(0, 0, 0, R * 0.14f));
            if (kind == "veins")
            {
                var vc = new Color(120 / 255f, 110 / 255f, 100 / 255f, 0.35f);
                for (int v = 0; v < 3; v++) { float px = R * 64, py = 0; while (py < 64) { float nx = px + R * 10 - 5, ny = py + 4 + R * 6; c.Line(px, py, nx, ny, 1, vc); px = nx; py = ny; } }
            }
            else if (kind == "cracks" || kind == "icecracks" || kind == "crackdark")
            {
                // molten (hell), frosted white (the frozen circle) or plain dark (broken asphalt)
                var halo = kind == "cracks" ? new Color(1, 0.19f, 0, 0.35f) : kind == "icecracks" ? new Color(0.85f, 0.97f, 1, 0.3f) : new Color(0, 0, 0, 0.25f);
                var core = kind == "cracks" ? Mats.Css("#ff5a10") : kind == "icecracks" ? Mats.Css("#eaf8ff") : new Color(0.06f, 0.05f, 0.05f, 0.85f);
                for (int v = 0; v < 3; v++)
                {
                    float px = R * 64, py = R * 64;
                    for (int s = 0; s < 6; s++)
                    {
                        float nx = px + R * 16 - 8, ny = py + R * 16 - 8;
                        c.Line(px, py, nx, ny, 4, halo);
                        c.Line(px, py, nx, ny, 1.5f, core);
                        px = nx; py = ny;
                    }
                }
            }
            c.StrokeRect(1, 1, 62, 62, 2, Mats.Css(line));
            if (!string.IsNullOrEmpty(accent)) c.Rect(0, 0, 64, 4, Mats.Css(accent));
            return c.ToTex(true, true);
        }
        static Texture2D MakeGlow()
        {
            var c = new Cv(64, 64);
            c.Radial(32, 32, 32, (0, new Color(1, 1, 1, 1)), (0.25f, new Color(1, 1, 1, 0.7f)), (1, new Color(1, 1, 1, 0)));
            return c.ToTex(false, false);
        }
        static Texture2D MakeFlash()
        {
            var c = new Cv(64, 64);
            for (int i = 0; i < 6; i++)
            {
                float a = (i + 1) * Mathf.PI / 3, len = 30 + R * 2;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var n = new Vector2(-d.y, d.x);
                var o = new Vector2(32, 32);
                for (int k = 0; k < 10; k++)
                {
                    float t0 = k / 10f, t1 = (k + 1) / 10f;
                    var col = Color.Lerp(new Color(1, 240 / 255f, 180 / 255f, 1), new Color(1, 120 / 255f, 0, 0), t0);
                    float w0 = 4 * (1 - t0), w1 = 4 * (1 - t1);
                    var p0 = o + d * len * t0; var p1 = o + d * len * t1;
                    c.Tri(p0 + n * w0, p0 - n * w0, p1 + n * w1, col); c.Tri(p0 - n * w0, p1 - n * w1, p1 + n * w1, col);
                }
            }
            c.Radial(32, 32, 14, (0, Color.white), (1, new Color(1, 200 / 255f, 80 / 255f, 0)));
            return c.ToTex(false, false);
        }
        static Texture2D Blob(string bg, string[] cols)
        {
            var c = new Cv(64, 64);
            c.Fill(Mats.Css(bg));
            for (int i = 0; i < 70; i++)
            {
                float px = R * 64, py = R * 64, r = 3 + R * 9; var col = Mats.Css(cols[(int)(R * cols.Length) % cols.Length]);
                c.Radial(px, py, r, (0, col), (1, A(col, 0)));
            }
            return c.ToTex(true, true);
        }
        static Texture2D MakeSwirl()
        {
            var c = new Cv(128, 128);
            c.Radial(64, 64, 64, (0, new Color(1, 1, 1, 1)), (0.35f, new Color(1, 1, 1, 0.35f)), (1, new Color(1, 1, 1, 0)));
            for (int arm = 0; arm < 6; arm++)
                for (float t = 0; t < 1; t += 0.01f)
                {
                    float r = t * 60, a = t * 7 + arm * Mathf.PI / 3;
                    c.Circle(64 + Mathf.Cos(a) * r, 64 + Mathf.Sin(a) * r, 1 + (1 - t) * 5, new Color(1, 1, 1, 0.9f * (1 - t)));
                }
            return c.ToTex(false, false);
        }
        static Texture2D MakeStained()
        {
            var c = new Cv(32, 64);
            string[] cols = { "#3a6ed8", "#e8c040", "#d83a3a", "#f4f0e0", "#40b0d0", "#9050c0" };
            for (int y = 0; y < 64; y += 8) for (int x = 0; x < 32; x += 8) c.Rect(x, y, 8, 8, Mats.Css(cols[(int)(R * cols.Length) % cols.Length]));
            var lead = Mats.Css("#222222");
            for (int y = 0; y <= 64; y += 8) c.Rect(0, y - 1, 32, 1.5f, lead);
            for (int x = 0; x <= 32; x += 8) c.Rect(x - 1, 0, 1.5f, 64, lead);
            return c.ToTex(true, false);
        }
        static Texture2D MakeCircle()
        {
            var c = new Cv(64, 64);
            c.Circle(32, 32, 30, Color.white);
            return c.ToTex(false, false);
        }
        // god-ray fade: bright at the top (the window), dissolving toward the floor, soft at the edges
        public static Texture2D RayFade;
        static Texture2D MakeRayFade()
        {
            const int W = 32, H = 128; var c = new Cv(W, H);
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                {
                    float v = 1 - (y + 0.5f) / H, u = (x + 0.5f) / W; // v = 1 at the top row
                    float along = Mathf.Pow(Mathf.Sin(v * Mathf.PI), 1.5f) * 0.85f; // fades out at both ends: no hard edge where it meets the floor
                    float streak = 0.75f + 0.25f * Mathf.Sin(u * Mathf.PI * 2 * 5 + Mathf.Sin(u * 37) * 0.8f);
                    c.p[y * W + x] = new Color(1, 1, 1, along * streak);
                }
            return c.ToTex(false, true, false);
        }
        // low-health screen damage: shattered-glass cracks spreading from a few impact points near the corners
        public static Texture2D Cracks;
        static Texture2D MakeCracks()
        {
            const int W = 1024, H = 576; var c = new Cv(W, H); var rng = new System.Random(7);
            float Rf() => (float)rng.NextDouble();
            void Crack(Vector2 p, float ang, float len, float lw, int depth)
            {
                float done = 0;
                while (done < len)
                {
                    float seg = 7 + Rf() * 12; ang += (Rf() - 0.5f) * 0.7f;
                    var q = p + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * seg;
                    c.Line(p.x, p.y, q.x, q.y, lw + 2.2f, new Color(0, 0, 0, 0.45f));
                    c.Line(p.x, p.y, q.x, q.y, lw, new Color(0.92f, 0.95f, 1f, 0.85f));
                    if (depth < 2 && Rf() < 0.09f) Crack(q, ang + (Rf() < 0.5f ? -1 : 1) * (0.5f + Rf() * 0.7f), (len - done) * (0.3f + Rf() * 0.4f), lw * 0.7f, depth + 1);
                    p = q; done += seg; lw = Mathf.Max(0.7f, lw * 0.985f);
                }
            }
            foreach (var o in new[] { new Vector2(70, 80), new Vector2(950, 500), new Vector2(980, 60), new Vector2(40, 520) })
            {
                int spokes = 7 + rng.Next(5);
                for (int i = 0; i < spokes; i++) Crack(o, i / (float)spokes * Mathf.PI * 2 + Rf() * 0.4f, 90 + Rf() * 260, 2.2f, 0);
                for (int ring = 1; ring <= 2; ring++) // concentric shatter rings around the impact
                {
                    float r = ring * (16 + Rf() * 10); int n = 14;
                    for (int i = 0; i < n; i++) { if (Rf() < 0.3f) continue; float a0 = i / (float)n * Mathf.PI * 2, a1 = (i + 1) / (float)n * Mathf.PI * 2; c.Line(o.x + Mathf.Cos(a0) * r, o.y + Mathf.Sin(a0) * r, o.x + Mathf.Cos(a1) * r * 1.05f, o.y + Mathf.Sin(a1) * r * 1.05f, 1.2f, new Color(0.9f, 0.95f, 1, 0.7f)); }
                }
                c.Circle(o.x, o.y, 6, new Color(1, 1, 1, 0.6f));
            }
            return c.ToTex(false, false);
        }
        // damage direction: a red crescent hugging the top edge of a square (the square is rotated to point it)
        public static Texture2D DamageArc;
        static Texture2D MakeDamageArc()
        {
            const int S = 256; var c = new Cv(S, S);
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    float dx = (x + 0.5f) / (S / 2) - 1, dy = 1 - (y + 0.5f) / (S / 2), r = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Atan2(dx, dy);
                    float band = Mathf.Clamp01(1 - Mathf.Abs(r - 0.9f) / 0.07f), spread = Mathf.Clamp01(1 - Mathf.Abs(a) / 0.5f);
                    c.p[y * S + x] = new Color(1, 0.08f, 0.05f, Mathf.Pow(band, 1.5f) * Mathf.SmoothStep(0, 1, spread));
                }
            return c.ToTex(false, false, false);
        }
        // anime speed lines: thin white streaks rushing in from the screen edges, clear in the middle
        public static Texture2D SpeedLines(int seed)
        {
            const int S = 512, BINS = 2048; var rng = new System.Random(seed);
            var alpha = new float[BINS]; var start = new float[BINS];
            for (int b = 0; b < BINS; b++) start[b] = 9;
            for (int l = 0; l < 110; l++)
            {
                float c = (float)rng.NextDouble() * BINS, w = 1.5f + (float)rng.NextDouble() * 5, s0 = 0.5f + (float)rng.NextDouble() * 0.35f, a = 0.45f + (float)rng.NextDouble() * 0.55f;
                for (int k = -8; k <= 8; k++)
                {
                    int b = ((int)c + k + BINS) % BINS; float f = Mathf.Clamp01(1 - Mathf.Abs(k) / w); if (f <= 0) continue;
                    if (f * a > alpha[b]) alpha[b] = f * a; start[b] = Mathf.Min(start[b], s0 + (1 - f) * 0.15f); // tapered tips
                }
            }
            var cv = new Cv(S, S);
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    float dx = (x + 0.5f) / (S / 2) - 1, dy = (y + 0.5f) / (S / 2) - 1, r = Mathf.Sqrt(dx * dx + dy * dy) / 1.2f;
                    int b = (int)((Mathf.Atan2(dy, dx) / (Mathf.PI * 2) + 0.5f) * BINS) % BINS;
                    float a = alpha[b] * Mathf.SmoothStep(0, 1, (r - start[b]) / 0.25f);
                    cv.p[y * S + x] = new Color(1, 1, 1, a);
                }
            return cv.ToTex(false, false, false);
        }
        static Texture2D MakeVignette() // radial-gradient(ellipse, transparent 40%, red 130%)
        {
            var c = new Cv(128, 128);
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
                {
                    float dx = (x + 0.5f) / 64 - 1, dy = (y + 0.5f) / 64 - 1, d = Mathf.Sqrt(dx * dx + dy * dy) / Mathf.Sqrt(2);
                    float a = Mathf.Clamp01((d - 0.4f) / 0.9f);
                    c.p[y * 128 + x] = new Color(1, 0, 0, a);
                }
            return c.ToTex(false, false, false);
        }
        static Texture2D MakeScreenBg() // radial-gradient(rgba(40,0,0,.35) 0%, rgba(8,0,0,.88) 75%)
        {
            var c = new Cv(128, 128);
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
                {
                    float dx = (x + 0.5f) / 64 - 1, dy = (y + 0.5f) / 64 - 1, d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / 0.75f / Mathf.Sqrt(2) * 1.4f);
                    c.p[y * 128 + x] = Color.Lerp(new Color(40 / 255f, 0, 0, 0.35f), new Color(8 / 255f, 0, 0, 0.88f), d);
                }
            return c.ToTex(false, false, false);
        }
        static Texture2D MakeScan()
        {
            var c = new Cv(4, 4);
            for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++) c.p[y * 4 + x] = y < 2 ? new Color(0, 0, 0, 0.18f) : new Color(0, 0, 0, 0);
            var t = c.ToTex(true, true, false); return t;
        }
        static Texture2D MakeRays() // repeating-conic-gradient streaks with a transparent centre
        {
            var c = new Cv(256, 256);
            for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
                {
                    float dx = x + 0.5f - 128, dy = y + 0.5f - 128, d = Mathf.Sqrt(dx * dx + dy * dy) / 128f;
                    float ang = (Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 360) % 11f;
                    float stripe = ang >= 5 && ang < 6 ? 0.55f : 0;
                    float mask = Mathf.Clamp01((d - 0.08f) / 0.37f);
                    c.p[y * 256 + x] = new Color(1, 1, 1, stripe * mask);
                }
            return c.ToTex(false, false, false);
        }
        static Texture2D MakeScopeMask() // transparent circle (30vmin) with a 92% black surround
        {
            var c = new Cv(256, 256);
            for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
                {
                    float dx = x + 0.5f - 128, dy = y + 0.5f - 128, d = Mathf.Sqrt(dx * dx + dy * dy) / 128f;
                    c.p[y * 256 + x] = new Color(0, 0, 0, d < 0.58f ? 0 : d < 0.6f ? (d - 0.58f) / 0.02f * 0.92f : 0.92f);
                }
            return c.ToTex(false, false, false);
        }
        static Texture2D MakeRing()
        {
            var c = new Cv(256, 256);
            for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
                {
                    float dx = x + 0.5f - 128, dy = y + 0.5f - 128, d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1 - Mathf.Abs(d - 124) / 2.5f);
                    c.p[y * 256 + x] = new Color(0.6f, 1, 1, a);
                }
            return c.ToTex(false, false, false);
        }
        // radial warp backdrop: wa centre -> wb at 38% -> near black at 85%
        public static Texture2D WarpBg(string wa, string wb)
        {
            var c = new Cv(128, 128);
            Color ca = Mats.Css(wa), cb = Mats.Css(wb), cc = new Color(0, 0, 0, 0.95f);
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
                {
                    float dx = (x + 0.5f) / 64 - 1, dy = (y + 0.5f) / 64 - 1, d = Mathf.Sqrt(dx * dx + dy * dy) / Mathf.Sqrt(2);
                    c.p[y * 128 + x] = d < 0.38f ? Color.Lerp(ca, cb, d / 0.38f) : Color.Lerp(cb, cc, Mathf.Clamp01((d - 0.38f) / 0.47f));
                }
            return c.ToTex(false, false, false);
        }
    }
}
