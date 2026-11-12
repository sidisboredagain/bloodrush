using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace BR
{
    // Heavy procedural data for the cutscene (planet maps, cliff and rock meshes, wave normals), computed on worker
    // threads. Pure math only: Unity objects are created from it on the main thread.
    public class CinePre
    {
        public const int TW = 1024, TH = 512, CW = 512, CHh = 256;
        public Color32[] col, lit, rough, cloud, waterN;
        public Geo.MB cliff; public Color[] cliffCol; public int cliffW, cliffH;
        public List<(Geo.MB mb, Color[] colr, int w, int h)> rocks = new List<(Geo.MB, Color[], int, int)>();
        public List<Geo.MB> heads = new List<Geo.MB>();
        public Geo.MB foliage;
        static float Lerp(float a, float b, float t) => a + (b - a) * t;
        static float[] L3(float[] a, float[] b, float t) => new[] { Lerp(a[0], b[0], t), Lerp(a[1], b[1], t), Lerp(a[2], b[2], t) };
        static float[] RGB(int h) => new float[] { (h >> 16) & 255, (h >> 8) & 255, h & 255 };
        static float Cl(float v, float a, float b) => v < a ? a : v > b ? b : v;
        static float SS(float a, float b, float x) => Noise.SStep(a, b, x);
        public static float ToLin(float c) => c <= 0.04045f ? c / 12.92f : (float)Math.Pow((c + 0.055) / 1.055, 2.4);
        public static Color LinHex(int h) => new Color(ToLin(((h >> 16) & 255) / 255f), ToLin(((h >> 8) & 255) / 255f), ToLin((h & 255) / 255f), 1);
        static Vector3 Dir(float u, float v) { float ph = u * Mathf.PI * 2, th = v * Mathf.PI; return new Vector3(-Mathf.Cos(ph) * Mathf.Sin(th), Mathf.Cos(th), Mathf.Sin(ph) * Mathf.Sin(th)); }
        public static CinePre Compute()
        {
            var o = new CinePre();
            o.col = new Color32[TW * TH]; o.lit = new Color32[TW * TH]; o.rough = new Color32[TW * TH]; o.cloud = new Color32[CW * CHh];
            float[] DEEP = RGB(0x071a38), SHAL = RGB(0x1f6a8e), SAND = RGB(0xc8b27e), GRN = RGB(0x3f6a2c), GRN2 = RGB(0x2b4f22), DES = RGB(0xb0915c), DES2 = RGB(0x8c6c40), MTN = RGB(0x6a5a48), RCK = RGB(0x8a8680), SNW = RGB(0xf0f2f6);
            Parallel.For(0, TH, j =>
            {
                for (int i = 0; i < TW; i++)
                {
                    var d = Dir((float)i / TW, (j + 0.5f) / TH); float x = d.x, y = d.y, z = d.z;
                    float wq = (Noise.Fbm(x * 2.2f + 11, y * 2.2f, z * 2.2f, 3) - 0.5f) * 0.7f;
                    float e = Noise.Fbm(x * 1.7f + wq, y * 1.7f + wq, z * 1.7f - wq, 6), lat = Mathf.Abs(y), sea = 0.535f;
                    float[] c; float r, l = 0;
                    if (e < sea) { float dd = Mathf.Pow((sea - e) / sea, 0.55f); c = L3(SHAL, DEEP, Cl(dd * 1.6f, 0, 1)); r = 60; }
                    else
                    {
                        float h = (e - sea) / (1 - sea), m = Noise.Fbm(x * 3 + 5, y * 3 + 5, z * 3, 3);
                        c = m > 0.5f - lat * 0.2f ? L3(GRN, GRN2, Cl(h * 2.5f + (m - 0.5f), 0, 1)) : L3(DES, DES2, Cl(h * 3, 0, 1));
                        if (h < 0.025f) c = L3(SAND, c, h / 0.025f);
                        if (h > 0.25f) c = L3(c, MTN, SS(0.25f, 0.42f, h));
                        if (h > 0.45f) c = L3(c, RCK, SS(0.45f, 0.6f, h));
                        if (h > 0.6f || lat > 0.8f) c = L3(c, SNW, Mathf.Max(SS(0.6f, 0.72f, h), SS(0.8f, 0.88f, lat + (m - 0.5f) * 0.1f)));
                        r = 225;
                        l = (0.25f + 0.75f * SS(0.46f, 0.64f, Noise.Fbm(x * 5 + 3, y * 5, z * 5, 3))) * (1 - SS(0.12f, 0.34f, h)) * (1 - SS(0.62f, 0.76f, lat)) * SS(0, 0.012f, h);
                    }
                    if (lat > 0.88f) { c = L3(c, SNW, SS(0.88f, 0.93f, lat)); r = 200; }
                    int k = (TH - 1 - j) * TW + i; // canvas rows run top-down; Unity textures bottom-up
                    o.col[k] = new Color32((byte)Cl(c[0], 0, 255), (byte)Cl(c[1], 0, 255), (byte)Cl(c[2], 0, 255), 255);
                    o.rough[k] = new Color32((byte)r, (byte)r, (byte)r, 255);
                    o.lit[k] = new Color32((byte)(255 * l), (byte)(200 * l), (byte)(120 * l), 255);
                }
            });
            Parallel.For(0, CHh, j =>
            {
                for (int i = 0; i < CW; i++)
                {
                    var d = Dir((float)i / CW, (j + 0.5f) / CHh);
                    float n = Noise.Fbm(d.x * 2.8f + 40, d.y * 5 + 40, d.z * 2.8f, 6), a = SS(0.5f, 0.74f, n) * (0.95f - Mathf.Abs(d.y) * 0.25f);
                    o.cloud[(CHh - 1 - j) * CW + i] = new Color32(255, 255, 255, (byte)Cl(255 * a, 0, 255));
                }
            });
            // sea cliff, boulders and distant headlands
            var cliff = RockGeo(170, new Vector3(18, 26, 30), 0.55f, 1.3f, 0.62f * 26, out o.cliffW, out o.cliffH);
            o.cliffCol = PaintRock(cliff, 0.5f * 26); o.cliff = cliff;
            foreach (var (x, y, z, sx, sy, sz) in new[] { (-14f, -20f, -14f, 6f, 9f, 6f), (12f, -22f, -10f, 5f, 7f, 6f), (-6f, -24f, -24f, 4f, 5f, 4f), (22f, -16f, 6f, 7f, 12f, 8f), (-24f, -14f, 10f, 8f, 14f, 9f) })
            {
                var g = RockGeo(48, new Vector3(sx, sy, sz), 0.6f, x * 0.37f, null, out int w, out int h);
                o.rocks.Add((g, PaintRock(g, null), w, h));
            }
            foreach (var (x, z, sx, sy, sz) in new[] { (-380f, -620f, 200f, 60f, 90f), (420f, -760f, 260f, 80f, 110f), (-900f, -1100f, 300f, 120f, 140f), (160f, -1300f, 380f, 90f, 160f) })
                o.heads.Add(RockGeo(40, new Vector3(sx, sy, sz), 0.5f, x * 0.01f, null, out _, out _));
            o.waterN = WaterNormals();
            // lumpy but smooth-shaded tree crown
            var fol = Geo.SphereMB(1, 40, 28);
            for (int i = 0; i < fol.v.Count; i++) { var v = fol.v[i]; float n = Noise.Fbm(v.x * 2.2f + 5, v.y * 2.2f, v.z * 2.2f, 4); v *= 0.8f + n * 0.42f; if (v.y < 0) v.y *= 0.7f; fol.v[i] = v; }
            fol.ComputeNormals(); o.foliage = fol;
            return o;
        }
        static Geo.MB RockGeo(int seg, Vector3 scale, float amp, float seed, float? flatTop, out int w, out int h)
        {
            int hs = (int)Math.Floor(seg * 0.66 + 0.5);
            var g = Geo.SphereMB(1, seg, hs); w = seg + 1; h = hs + 1;
            for (int i = 0; i < g.v.Count; i++)
            {
                var v = g.v[i]; var n = v;
                float d = (Noise.Fbm(v.x * 1.6f + seed, v.y * 1.6f, v.z * 1.6f, 5) - 0.5f) * amp + (Noise.Fbm(v.x * 6 + seed, v.y * 6, v.z * 6, 3) - 0.5f) * amp * 0.25f;
                v += n * d; v = Vector3.Scale(v, scale);
                v.x += Mathf.Sin(v.y * 1.1f + seed) * 0.25f; v.z += Mathf.Cos(v.y * 1.3f + seed) * 0.25f; // strata
                if (flatTop.HasValue && v.y > flatTop.Value) v.y = flatTop.Value + (v.y - flatTop.Value) * 0.12f + (Noise.Fbm(v.x * 0.3f, 0, v.z * 0.3f, 3) - 0.5f) * 0.7f;
                g.v[i] = v;
                // texel-centred UVs so the baked vertex colours interpolate exactly like three's vertex colours
                int iy = i / w, ix = i % w; g.uv[i] = new Vector2((ix + 0.5f) / w, (hs - iy + 0.5f) / h);
            }
            g.ComputeNormals(); return g;
        }
        // vertex colours, laid out as a texture indexed by the sphere grid (linear values)
        static Color[] PaintRock(Geo.MB g, float? grassAbove)
        {
            Color R1 = LinHex(0x5a4c40), R2 = LinHex(0x7a6a58), R3 = LinHex(0x3a322c), G1 = LinHex(0x4a5e2a), G2 = LinHex(0x6a7a36);
            var outc = new Color[g.v.Count];
            int w = 0; while (w < g.uv.Count && Mathf.Abs(g.uv[w].y - g.uv[0].y) < 1e-6f) w++;
            int h = g.v.Count / w;
            for (int i = 0; i < g.v.Count; i++)
            {
                var p = g.v[i]; float ny = g.n[i].y;
                float strata = 0.5f + 0.5f * Mathf.Sin(p.y * 1.7f + Noise.Fbm(p.x * 0.2f, p.y * 0.2f, p.z * 0.2f, 2) * 6), nz = Noise.Fbm(p.x * 0.8f, p.y * 0.8f, p.z * 0.8f, 3);
                var c = Color.LerpUnclamped(Color.LerpUnclamped(R1, R2, strata * 0.7f), R3, SS(0.55f, 0.8f, nz) * 0.6f);
                if (grassAbove.HasValue && ny > 0.62f && p.y > grassAbove.Value) c = Color.LerpUnclamped(Color.LerpUnclamped(c, G1, SS(0.62f, 0.8f, ny)), G2, nz * 0.5f);
                int iy = i / w, ix = i % w; outc[(h - 1 - iy) * w + ix] = c;
            }
            return outc;
        }
        static Color32[] WaterNormals()
        {
            long sd = 91; double R() { sd = sd * 16807 % 2147483647; return sd / 2147483647.0; }
            var W = new List<double[]>();
            for (int i = 0; i < 22; i++) { double f = 2 + i * 0.9 + R() * 2, a = R() * Math.PI * 2; W.Add(new[] { Math.Floor(Math.Cos(a) * f + 0.5), Math.Floor(Math.Sin(a) * f * 1.4 + 0.5), 1.4 / Math.Pow(f, 1.15), R() * 6.28 }); }
            double H(int i, int j) { double s = 0, u = i / 256.0 * Math.PI * 2, v = j / 256.0 * Math.PI * 2; foreach (var w in W) s += w[2] * Math.Sin(w[0] * u + w[1] * v + w[3]); return s; }
            var px = new Color32[256 * 256];
            for (int j = 0; j < 256; j++) for (int i = 0; i < 256; i++)
                {
                    double dx = H(i + 1, j) - H(i - 1, j), dy = H(i, j + 1) - H(i, j - 1);
                    var n = new Vector3((float)(-dx * 0.9), (float)(-dy * 0.9), 1).normalized;
                    px[(255 - j) * 256 + i] = new Color32((byte)((n.x * 0.5f + 0.5f) * 255), (byte)((n.y * 0.5f + 0.5f) * 255), (byte)((n.z * 0.5f + 0.5f) * 255), 255);
                }
            return px;
        }
    }

    public partial class Game
    {
        // ---------- small helpers for the sets ----------
        static Color HSL(float h, float s, float l) // three.js setHSL (values in the working, i.e. linear, colour space)
        {
            h = ((h % 1) + 1) % 1; float q = l <= 0.5f ? l * (1 + s) : l + s - l * s, p = 2 * l - q;
            float H2R(float t) { if (t < 0) t += 1; if (t > 1) t -= 1; if (t < 1f / 6) return p + (q - p) * 6 * t; if (t < 0.5f) return q; if (t < 2f / 3) return p + (q - p) * 6 * (2f / 3 - t); return p; }
            return new Color(H2R(h + 1f / 3), H2R(h), H2R(h - 1f / 3), 1);
        }
        static int HexOf(Color linear) { var g = linear.gamma; return ((int)(Mathf.Clamp01(g.r) * 255 + 0.5f) << 16) | ((int)(Mathf.Clamp01(g.g) * 255 + 0.5f) << 8) | (int)(Mathf.Clamp01(g.b) * 255 + 0.5f); }
        static Texture2D TexFrom(Color32[] px, int w, int h, bool srgb, bool mips, bool repeat, FilterMode f = FilterMode.Bilinear)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, mips, !srgb) { wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp, filterMode = f, anisoLevel = 8 };
            t.SetPixels32(px); t.Apply(mips); return t;
        }
        static Texture2D CvTex(int w, int h, Action<Cv> draw, bool repeat = true, bool mips = true) { var c = new Cv(w, h); draw(c); var t = c.ToTex(false, repeat, mips); t.anisoLevel = 4; return t; }
        // a three.js Points cloud as quads expanded in the vertex shader
        static Mesh PointsMesh(Vector3[] pos, Color[] col)
        {
            int n = pos.Length; var v = new Vector3[n * 4]; var uv = new Vector2[n * 4]; var c = new Color[n * 4]; var t = new int[n * 6];
            var corners = new[] { new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f), new Vector2(0.5f, 0.5f), new Vector2(-0.5f, 0.5f) };
            for (int i = 0; i < n; i++)
            {
                for (int k = 0; k < 4; k++) { v[i * 4 + k] = pos[i]; uv[i * 4 + k] = corners[k]; c[i * 4 + k] = col[i]; }
                t[i * 6] = i * 4; t[i * 6 + 1] = i * 4 + 2; t[i * 6 + 2] = i * 4 + 1; t[i * 6 + 3] = i * 4; t[i * 6 + 4] = i * 4 + 3; t[i * 6 + 5] = i * 4 + 2;
            }
            var m = new Mesh { indexFormat = IndexFormat.UInt32 }; m.vertices = v; m.uv = uv; m.colors = c; m.triangles = t;
            m.bounds = new Bounds(Vector3.zero, Vector3.one * 5000); return m;
        }
        Material PointsMat(float size, bool atten, Texture map, bool additive, float opacity)
        {
            var m = CMat("BR_Points"); m.SetFloat("_Size", size); m.SetFloat("_Atten", atten ? 1 : 0); if (map) m.SetTexture("_MainTex", map);
            m.SetFloat("_Src", additive ? 5 : 5); m.SetFloat("_Dst", additive ? 1 : 10); m.SetFloat("_Opacity", opacity);
            m.SetFloat("_PxScale", 1.25f * Mathf.Max(1, Screen.height / 1080f)); return m;
        }
        Light CineLight(N parent, int color, float intensity, Vector3 pos, bool shadows)
        {
            var go = new GameObject("light"); go.transform.SetParent(parent.t, false);
            var l = go.AddComponent<Light>(); l.type = LightType.Directional; l.color = Mats.Hex(color); l.intensity = intensity / Mathf.PI;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None; l.shadowStrength = 1;
            AimLight(l, pos, Vector3.zero); return l;
        }
        static void AimLight(Light l, Vector3 pos, Vector3 target) { if ((target - pos).sqrMagnitude > 1e-6f) l.transform.rotation = Quaternion.LookRotation(target - pos); } // three.js lights shine from position to target
        Texture2D cloudTexture(int seed)
        {
            long sd = seed; float r() { sd = sd * 16807 % 2147483647; return (float)(sd / 2147483647.0); }
            return CvTex(256, 128, x =>
            {
                for (int i = 0; i < 60; i++)
                {
                    float px = 256 * (0.15f + r() * 0.7f), py = 128 * (0.45f + (r() - 0.5f) * 0.35f * Mathf.Sin(px / 256 * Mathf.PI)), rad = 10 + r() * 26;
                    x.Radial(px, py, rad, (0, new Color(1, 1, 1, 0.55f)), (1, new Color(1, 1, 1, 0)));
                }
            }, false, true);
        }
        GameObject CloudSprite(N parent, Texture2D tex, int color, float w, float h, float opacity)
        {
            var m = G(Mats.Trans(color, opacity, true, false)); m.SetTexture("_BaseMap", tex); m.renderQueue = 2990;
            var g = Mk.Msh(parent, Geo.Plane(1, 1), m); g.transform.localScale = new Vector3(w, h, 1); Mk.billboards.Add(g.transform); NoShadow(g); return g;
        }

        // ----- the world from orbit -----
        void SetWorldLight(Vector3 dir)
        {
            CS.planetMat.SetVector("_LightDir", dir); CS.cloudMat.SetVector("_LightDir", dir);
            CS.atmoMat.SetVector("_SunDir", dir); CS.hazeMat.SetVector("_SunDir", dir);
        }
        void BuildWorld()
        {
            var W = CS.world;
            CS.planetMat = CMat("BR_Planet"); CS.cloudMat = CMat("BR_Planet");
            foreach (var m in new[] { CS.planetMat, CS.cloudMat })
            {
                m.SetColor("_LightCol", Mats.Hex(0xfff0dc)); m.SetFloat("_LightI", 3.4f); m.SetColor("_AmbCol", Mats.Hex(0x1a2438)); m.SetFloat("_AmbI", 0.25f);
                m.SetColor("_EmisColor", Mats.Hex(0xffc27a)); m.SetFloat("_EmisI", 1.3f);
            }
            CS.cloudMat.SetFloat("_Mode", 1); CS.cloudMat.SetFloat("_Src", 5); CS.cloudMat.SetFloat("_Dst", 10); CS.cloudMat.SetFloat("_ZW", 0); CS.cloudMat.renderQueue = 3000;
            var planet = Mk.Msh(W, Geo.Sphere(30, 160, 110), CS.planetMat); NoShadow(planet); CS.planet = new N(planet.transform, true);
            var clouds = Mk.Msh(W, Geo.Sphere(30.32f, 128, 90), CS.cloudMat); NoShadow(clouds); CS.clouds = new N(clouds.transform, true);
            CS.tasks.Add(() =>
            {
                var pre = cinePre.Result;
                CS.planetMat.SetTexture("_MainTex", TexFrom(pre.col, CinePre.TW, CinePre.TH, true, true, true));
                CS.planetMat.SetTexture("_LitTex", TexFrom(pre.lit, CinePre.TW, CinePre.TH, true, true, true));
                CS.planetMat.SetTexture("_RoughTex", TexFrom(pre.rough, CinePre.TW, CinePre.TH, false, true, true));
                CS.cloudMat.SetTexture("_MainTex", TexFrom(pre.cloud, CinePre.CW, CinePre.CHh, true, true, true));
            });
            // atmosphere: rim glow brighter on the sunlit side
            CS.atmoMat = CMat("BR_Atmo"); CS.atmoMat.SetFloat("_Mode", 0); CS.atmoMat.SetFloat("_Cull", 1); CS.atmoMat.SetColor("_Col", Mats.Hex(0x4f9cff));
            CS.hazeMat = CMat("BR_Atmo"); CS.hazeMat.SetFloat("_Mode", 1); CS.hazeMat.SetFloat("_Cull", 2); CS.hazeMat.SetColor("_Col", Mats.Hex(0x4f9cff));
            NoShadow(Mk.Msh(W, Geo.Sphere(31.9f, 96, 64), CS.atmoMat)); NoShadow(Mk.Msh(W, Geo.Sphere(30.55f, 96, 64), CS.hazeMat));
            SetWorldLight(new Vector3(-1, 0, -1).normalized);
            // stars: three size classes with colour variation and a denser galactic band
            foreach (var (n, size, band) in new[] { (5000, 1.2f, false), (1400, 2.0f, false), (3500, 1.0f, true) })
            {
                var pos = new Vector3[n]; var colr = new Color[n];
                var axis = new Vector3(1, 0, 0.3f).normalized;
                for (int i = 0; i < n; i++)
                {
                    var v = new Vector3(crand() - 0.5f, crand() - 0.5f, crand() - 0.5f).normalized;
                    if (band) { v.y *= 0.12f; v = (Quaternion.AngleAxis(0.9f * Mathf.Rad2Deg, axis) * v).normalized; }
                    pos[i] = v * 1500;
                    float k = crand(); colr[i] = HSL(k < 0.3f ? 0.6f : k < 0.5f ? 0.08f : 0.12f, k < 0.5f ? 0.6f : 0.1f, 0.65f + crand() * 0.35f);
                }
                var g = new GameObject("stars"); g.layer = CINE_LAYER; g.transform.SetParent(W.t, false);
                g.AddComponent<MeshFilter>().sharedMesh = PointsMesh(pos, colr);
                var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = PointsMat(size, false, null, false, band ? 0.55f : 0.95f); mr.shadowCastingMode = ShadowCastingMode.Off;
            }
            // the sun: layered glows and an anamorphic streak
            var sun = Joint(W);
            CGlow(sun.t, 0xffffff, 34, 1); CGlow(sun.t, 0xfff1c8, 110, 0.9f); CGlow(sun.t, 0xffb066, 330, 0.35f);
            var streak = CGlow(sun.t, 0xffd8a0, 1, 0.5f); streak.transform.localScale = new Vector3(900, 14, 1);
            CS.wSun = sun.t;
        }

        // ----- city -----
        public class Bird { public Transform g; public N wl, wr, wl2, wr2; public float cx, cy, cz, rad, ang, speed, ph; public bool glide; }
        // a gull: shaped body and head, beak, tail, and two-part wings (inner + outer at the wrist) with dark tips
        Bird MakeBird(N parent, int color)
        {
            var b = new Bird(); var root = Joint(parent); b.g = root.t;
            bool light = ((color >> 16) & 255) > 120;
            var m = G(Mats.Std(color, 0.75f, 0, 0, null, true)); var belly = light ? G(Mats.Std(0xf4f4f2, 0.7f)) : m;
            var beak = G(Mats.Std(light ? 0xf0a020 : 0x2a2420, 0.5f)); var tip = G(Mats.Std(0x18181c, 0.7f, 0, 0, null, true));
            Part(root, SphereG(1, 16, 12), m, 0, 0.01f, 0, 0, 0, 0, 0.058f, 0.055f, 0.22f);                // back
            Part(root, SphereG(1, 16, 12), belly, 0, -0.01f, 0.01f, 0, 0, 0, 0.052f, 0.05f, 0.2f);        // belly
            Part(root, SphereG(0.058f, 14, 10), belly, 0, 0.04f, 0.2f);                                    // head
            Part(root, Geo.Cone(0.016f, 0.075f, 8), beak, 0, 0.032f, 0.28f, Mathf.PI / 2, 0, 0);
            Part(root, BoxG(0.11f, 0.012f, 0.1f), m, 0, 0.0f, -0.24f, 0.1f, 0, 0);                        // tail
            var inner = GeoX.Cached("gullwingIn", () =>
            {
                var sh = new GeoX.Shape().MoveTo(0, 0.07f).QuadTo(0.24f, 0.1f, 0.44f, 0.05f).LineTo(0.44f, -0.06f).QuadTo(0.22f, -0.11f, 0, -0.09f).LineTo(0, 0.07f);
                var g = GeoX.ShapeMB(sh, 8); g.Transform(Matrix4x4.Rotate(Mk.XYZ(-Mathf.PI / 2, 0, 0))); return g.Build();
            });
            var outer = GeoX.Cached("gullwingOut", () =>
            {
                var sh = new GeoX.Shape().MoveTo(0, 0.05f).QuadTo(0.32f, 0.06f, 0.62f, -0.04f).QuadTo(0.3f, -0.07f, 0, -0.06f).LineTo(0, 0.05f);
                var g = GeoX.ShapeMB(sh, 8); g.Transform(Matrix4x4.Rotate(Mk.XYZ(-Mathf.PI / 2, 0, 0))); return g.Build();
            });
            var tipMesh = GeoX.Cached("gullwingTip", () =>
            {
                var sh = new GeoX.Shape().MoveTo(0.4f, 0.045f).QuadTo(0.52f, 0.03f, 0.62f, -0.04f).QuadTo(0.5f, -0.06f, 0.42f, -0.06f).LineTo(0.4f, 0.045f);
                var g = GeoX.ShapeMB(sh, 6); g.Transform(Matrix4x4.Rotate(Mk.XYZ(-Mathf.PI / 2, 0, 0))); return g.Build();
            });
            foreach (var side in new[] { 1f, -1f })
            {
                var w = Joint(root, side * 0.05f, 0.03f, 0.02f); Part(w, inner, m, 0, 0, 0, 0, 0, 0, side, 1, 1);
                var w2 = Joint(w, side * 0.44f, 0, 0); Part(w2, outer, m, 0, 0, 0, 0, 0, 0, side, 1, 1); Part(w2, tipMesh, tip, 0, 0.002f, 0, 0, 0, 0, side, 1, 1);
                if (side > 0) { b.wl = w; b.wl2 = w2; } else { b.wr = w; b.wr2 = w2; }
            }
            return b;
        }        List<Bird> MakeFlock(N parent, int n, float cx, float cy, float cz, float rad, int color, bool glide)
        {
            var list = new List<Bird>();
            for (int i = 0; i < n; i++)
            {
                var b = MakeBird(parent, color); float s = cr(2.4f, 3.2f); b.g.localScale = Vector3.one * s;
                b.cx = cx + cr(-20, 20); b.cy = cy + cr(-6, 6); b.cz = cz + cr(-20, 20); b.rad = rad * cr(0.8f, 1.2f); b.ang = crand() * 6.28f; b.speed = cr(5, 8); b.ph = crand() * 6; b.glide = glide;
                list.Add(b);
            }
            return list;
        }
        void FlapBirds(List<Bird> list, float t, float dt)
        {
            foreach (var d in list)
            {
                d.ang += dt * d.speed / d.rad;
                d.g.localPosition = V(d.cx + Mathf.Cos(d.ang) * d.rad, d.cy + Mathf.Sin(t * 0.7f + d.ph) * 1.5f, d.cz + Mathf.Sin(d.ang) * d.rad * 0.4f);
                d.g.localRotation = Mk.XYZ(0, -d.ang + (d.speed > 0 ? 0 : Mathf.PI), 0);
                // flap from the shoulder with the wrist trailing a beat behind (gliders just soar with small corrections)
                float f = d.glide ? 0.08f + Mathf.Sin(t * 1.2f + d.ph) * 0.12f : Mathf.Sin(t * 8 + d.ph) * 0.6f;
                float f2 = d.glide ? -0.1f + Mathf.Sin(t * 1.2f + d.ph - 0.6f) * 0.08f : Mathf.Sin(t * 8 + d.ph - 1.0f) * 0.5f;
                d.wl.rz = f; d.wr.rz = -f; d.wl2.rz = f2; d.wr2.rz = -f2;
            }
        }
        readonly Dictionary<Material, Material> windowGlow = new Dictionary<Material, Material>();
        // a box whose side UVs follow its real size (8 m per tile) and whose top and bottom use the roof material
        GameObject CityBox(N parent, float w, float h, float d, Material mat, Material roof, float x, float y, float z)
        {
            var src = Geo.Box(w, h, d); var v = src.vertices; var n = src.normals; var uv = src.uv; var t = src.triangles;
            for (int i = 0; i < v.Length; i++)
            {
                int f = i / 4; if (f == 2 || f == 3) continue;
                float su = f < 2 ? d / 8 : w / 8, sv = h / 8; uv[i] = new Vector2(uv[i].x * su, uv[i].y * sv);
            }
            var walls = new List<int>(); var tops = new List<int>();
            for (int i = 0; i < t.Length; i += 3) { int f = t[i] / 4; (f == 2 || f == 3 ? tops : walls).AddRange(new[] { t[i], t[i + 1], t[i + 2] }); }
            windowGlow.TryGetValue(mat, out var glow);
            var m = new Mesh(); m.vertices = v; m.normals = n; m.uv = uv; m.subMeshCount = glow ? 3 : 2; m.SetTriangles(walls, 0); m.SetTriangles(tops, 1); if (glow) m.SetTriangles(walls, 2); m.RecalculateBounds();
            var g = new GameObject("box") { layer = CINE_LAYER }; g.transform.SetParent(parent.t, false); g.transform.localPosition = V(x, y, z);
            g.AddComponent<MeshFilter>().sharedMesh = m; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterials = glow ? new[] { mat, roof, glow } : new[] { mat, roof };
            r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; r.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            return g;
        }
        public class Firework
        {
            public GameObject pts, rocket, flash; public Mesh mesh; public Vector3[] verts, vel; public Color[] cols, baseC; public Material mat, rocketMat, flashMat;
            public Vector3 from, target; public float t, rise, life; public bool willow, burst;
            public void Destroy() { foreach (var o in new[] { pts, rocket, flash }) if (o) UnityEngine.Object.Destroy(o); }
        }
        void LaunchFirework(N C, Vector3 target)
        {
            float hue = crand(); var pal = new[] { HSL(hue, 0.9f, 0.6f), HSL((hue + 0.08f + crand() * 0.2f) % 1, 0.95f, 0.7f) };
            bool willow = crand() < 0.3f; int n = willow ? 140 : 190;
            var f = new Firework { willow = willow, verts = new Vector3[n * 4], vel = new Vector3[n], cols = new Color[n * 4], baseC = new Color[n] };
            var pos = new Vector3[n]; var col = new Color[n];
            for (int i = 0; i < n; i++)
            {
                pos[i] = target;
                var dir = new Vector3(crand() - 0.5f, crand() - 0.5f, crand() - 0.5f).normalized;
                f.vel[i] = dir * ((willow ? 9 : 15) * (0.75f + crand() * 0.35f));
                col[i] = willow ? CinePre.LinHex(0xffc870) : pal[i % 2]; f.baseC[i] = col[i];
            }
            f.mesh = PointsMesh(pos, col); f.mesh.MarkDynamic(); f.mesh.vertices.CopyTo(f.verts, 0); f.mesh.colors.CopyTo(f.cols, 0);
            f.pts = new GameObject("fw") { layer = CINE_LAYER }; f.pts.transform.SetParent(C.t, false);
            f.pts.AddComponent<MeshFilter>().sharedMesh = f.mesh; f.mat = PointsMat(willow ? 1.3f : 1.7f, true, Tex.Glow, true, 1);
            var mr = f.pts.AddComponent<MeshRenderer>(); mr.sharedMaterial = f.mat; mr.shadowCastingMode = ShadowCastingMode.Off; f.pts.SetActive(false);
            f.rocket = CGlow(C.t, 0xffe0b0, 1.6f, 1); f.rocket.transform.localPosition = V(target.x + cr(-4, 4), 4, target.z); f.rocketMat = f.rocket.GetComponent<Renderer>().sharedMaterial;
            f.flash = CGlow(C.t, HexOf(pal[0]), 26, 0); f.flash.transform.localPosition = target; f.flashMat = f.flash.GetComponent<Renderer>().sharedMaterial;
            f.from = f.rocket.transform.localPosition; f.target = target; f.rise = cr(0.9f, 1.3f); f.life = willow ? 3.2f : 2.3f;
            CS.fwList.Add(f);
        }
        void UpdateFireworks(float dt)
        {
            foreach (var f in CS.fwList)
            {
                f.t += dt;
                if (f.t < f.rise) { float k = ease3(f.t / f.rise); f.rocket.transform.localPosition = Vector3.Lerp(f.from, f.target, k); Mats.SetAlpha(f.rocketMat, 0.7f + 0.3f * Mathf.Sin(f.t * 60)); continue; }
                if (!f.burst) { f.pts.SetActive(true); f.rocket.SetActive(false); f.burst = true; CS.lastBurstPos = f.target; CS.lastBurstT = 0; }
                float a = f.t - f.rise, dr = Mathf.Pow(f.willow ? 0.965f : 0.975f, dt * 60);
                for (int i = 0; i < f.vel.Length; i++)
                {
                    var v = f.vel[i] * dr; v.y -= (f.willow ? 4.5f : 3.0f) * dt; f.vel[i] = v;
                    var p = f.verts[i * 4] + v * dt;
                    float tw = a > f.life * 0.55f ? (Mathf.Sin(a * 40 + i * 7) > 0.2f ? 1 : 0.25f) : 1;
                    var c = f.baseC[i] * tw; c.a = 1;
                    for (int k = 0; k < 4; k++) { f.verts[i * 4 + k] = p; f.cols[i * 4 + k] = c; }
                }
                f.mesh.vertices = f.verts; f.mesh.colors = f.cols;
                f.mat.SetFloat("_Opacity", clamp(1 - a / f.life, 0, 1)); f.mat.SetFloat("_Size", (f.willow ? 1.3f : 1.7f) * (1 - 0.35f * a / f.life));
                Mats.SetAlpha(f.flashMat, Mathf.Max(0, 0.9f - a * 5));
            }
            CS.fwList.RemoveAll(f => { if (f.t - f.rise < f.life) return false; f.Destroy(); return true; });
        }
        public class Confetti { public Mesh mesh; public Material[] mats; public int[] grp; public float[] x, y, z, vy, ph, sp; public List<Matrix4x4>[] mtx; }
        public class Grass { public Mesh grass, flower; public Material[] gm, fm; public List<(float x, float y, float z, float s, float ry, float ph, int g)> st = new List<(float, float, float, float, float, float, int)>(); public List<Matrix4x4>[] fmtx; public List<Matrix4x4>[] gmtx; }

        void BuildCity()
        {
            var C = CS.city;
            CS.citySkyP = MakeSkyParams(0x1c2a5a, 0x6a6a9a, 0xffa070, 0x2a2020, new Vector3(0, 0.04f, -1), 0xffc890, 1.15f);
            CS.citySky = CMat("BR_Sky"); ApplySky(CS.citySky, CS.citySkyP); SkyDome(C, CS.citySky);
            CS.cityProbe = MakeProbe(C);
            CS.citySun = CineLight(C, 0xffc48a, 2.8f, V(0, 30, -160), true);
            CineLight(C, 0x9ab4ff, 0.55f, V(10, 20, 40), false);
            // ground: asphalt road with markings, sidewalks with paving, curbs
            var road = CvTex(256, 512, x =>
            {
                for (int i = 0; i < 256 * 512; i++) { float v = (52 + UnityEngine.Random.value * 22 + (UnityEngine.Random.value < 0.02f ? 25 : 0)) / 255f; x.p[i] = new Color(v, v * 0.98f, v * 0.96f, 1); }
                var wl = new Color(235 / 255f, 225 / 255f, 200 / 255f, 0.85f); x.Rect(10, 0, 5, 512, wl); x.Rect(256 - 15, 0, 5, 512, wl);
                var yl = new Color(240 / 255f, 200 / 255f, 80 / 255f, 0.85f); for (int y = 0; y < 512; y += 64) { x.Rect(128 - 6, y, 4, 36, yl); x.Rect(128 + 2, y, 4, 36, yl); }
            });
            var roadM = G(Mats.Std(0xffffff, 0.92f, 0, 0, road)); roadM.SetTextureScale("_BaseMap", new Vector2(1, 420f / 32));
            Part(C, Geo.Plane(16, 420), roadM, 0, 0, -140, -Mathf.PI / 2);
            var pave = CvTex(256, 256, x =>
            {
                for (int i = 0; i < 256 * 256; i++) { float v = (150 + UnityEngine.Random.value * 25) / 255f; x.p[i] = new Color(v, v * 0.97f, v * 0.93f, 1); }
                var lc = new Color(70 / 255f, 64 / 255f, 60 / 255f, 0.6f);
                for (int k = 0; k <= 4; k++) { x.Line(0, k * 64, 256, k * 64, 2, lc); x.Line(k * 64, 0, k * 64, 256, 2, lc); }
            });
            var paveM = G(Mats.Std(0xffffff, 0.85f, 0, 0, pave)); paveM.SetTextureScale("_BaseMap", new Vector2(1.5f, 420f / 4)); var curbM = CM(0xb8b0a4, "plain", 0.8f);
            foreach (var sd in new[] { -1f, 1f })
            {
                Part(C, Geo.Box(6, 0.16f, 420), paveM, sd * 11, 0.08f, -140);
                Part(C, Geo.Box(0.3f, 0.18f, 420), curbM, sd * 8.1f, 0.09f, -140);
            }
            Part(C, Geo.Plane(1400, 1400), G(Mats.Std(0x2a2624, 1)), 0, -0.02f, 0, -Mathf.PI / 2);
            // buildings: tiered towers with cornices, rooftop tanks/antennas, lit windows mapped to their real size
            var facade = CvTex(128, 128, x =>
            {
                x.Fill(Game.C("#d8d2c8"));
                for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) { x.Rect(i * 32 + 6, j * 32 + 7, 20, 20, Game.C("#20262e")); x.Rect(i * 32 + 7, j * 32 + 8, 18, 6, Game.C("#3a4656")); }
            });
            var lit = CvTex(128, 512, x =>
            {
                x.Fill(Color.black);
                for (int i = 0; i < 4; i++) for (int j = 0; j < 16; j++) if (UnityEngine.Random.value < 0.38f) x.Rect(i * 32 + 6, j * 32 + 7, 20, 20, UnityEngine.Random.value < 0.75f ? Game.C("#ffcf8a") : Game.C("#fff1d8"));
            });
            // lit windows (an emissive map in the HTML game) are drawn as an additive overlay of the same walls
            Material Fac(int c, float rough, float metal, int emis, float ei) { var m = G(Mats.Std(c, rough, metal, 0, facade)); windowGlow[m] = G(Mats.Add(emis, ei, lit, true, false)); return m; }
            var facM = new[] { 0xb8664a, 0x9a8a78, 0xc8b8a0, 0x6c727c, 0x8a5a44, 0xa8a49c }.Select(c => Fac(c, 0.85f, 0, 0xffc888, 0.55f)).ToArray();
            var glassM = new[] { 0x23394e, 0x2c3e3a, 0x332c40 }.Select(c => Fac(c, 0.12f, 0.75f, 0xffd6a0, 0.4f)).ToArray();
            Material roofM = CM(0x3a3634, "plain", 0.95f), corniceM = CM(0x4a4440, "plain"), tankM = CM(0x5a4a3c, "plain");
            void Tower(float x, float z, float w, float d, float H, Material mat)
            {
                float y = 0, ww = w, dd = d; int tiers = H > 40 ? 3 : H > 22 ? 2 : 1;
                for (int k = 0; k < tiers; k++)
                {
                    float hh = k == tiers - 1 ? H - y : H * (k == 0 ? 0.62f : 0.26f);
                    CityBox(C, ww, hh, dd, mat, roofM, x, y + hh / 2, z);
                    Part(C, BoxG(ww + 0.6f, 0.5f, dd + 0.6f), corniceM, x, y + hh, z);
                    y += hh; ww *= 0.78f; dd *= 0.78f;
                }
                if (crand() < 0.5f) // water tank on legs
                {
                    var tg = Joint(C, x + cr(-ww / 4, ww / 4), y + 0.25f, z + cr(-dd / 4, dd / 4));
                    foreach (var (lx, lz) in new[] { (-0.9f, -0.9f), (0.9f, -0.9f), (-0.9f, 0.9f), (0.9f, 0.9f) }) Part(tg, CylG(0.08f, 0.08f, 2, 6), corniceM, lx, 1, lz);
                    Part(tg, CylG(1.4f, 1.4f, 2.6f, 20), tankM, 0, 3.3f, 0); Part(tg, Geo.Cyl(0, 1.5f, 0.9f, 20), tankM, 0, 5.05f, 0);
                }
                if (H > 50 && crand() < 0.7f)
                {
                    Part(C, CylG(0.12f, 0.18f, 9, 8), corniceM, x, y + 4.5f, z);
                    var bm = G(Mats.Std(0x400000)); Mats.Emissive(bm, 0xff2010, 4); Part(C, SphereG(0.35f), bm, x, y + 9.2f, z); CS.blinkers.Add(bm);
                }
                for (int k = 0; k < 3; k++) if (crand() < 0.6f) Part(C, BoxG(1.6f, 1, 1.2f), roofM, x + cr(-ww / 3, ww / 3), y + 0.5f, z + cr(-dd / 3, dd / 3));
            }
            foreach (var sd in new[] { -1f, 1f })
            {
                float z = 70;
                while (z > -360)
                {
                    float w = cr(12, 22), d = cr(12, 20), H = z < -150 ? cr(40, 95) : cr(18, 70);
                    float gx = sd * (14 + w / 2 + cr(0, 3)), zz = z - d / 2; var mat = crand() < 0.3f ? pick(glassM) : pick(facM);
                    CS.tasks.Add(() => Tower(gx, zz, w, d, H, mat));
                    z -= d + cr(1.5f, 5);
                }
            }
            for (int i = 0; i < 26; i++) // distant skyline either side of the sunrise
            {
                float sd = i % 2 == 1 ? 1 : -1, x = sd * cr(45, 260), z = cr(-380, -620), H = cr(40, 140);
                CS.tasks.Add(() => { float bw = cr(18, 40), bd = cr(18, 40); CityBox(C, bw, H, bd, pick(glassM), roofM, x, H / 2, z); });
            }
            // street furniture
            var poleM = CM(0x22262c, "metal", 0.5f); var lampM = G(Mats.Std(0xfff0d0)); Mats.Emissive(lampM, 0xffd590, 2.2f);
            ClothTextures();
            var leafM = new[] { 0x3e6a2e, 0x4a7a34, 0x355c28 }.Select(c => { var m = G(Mats.Std(c, 0.82f, 0, 0, knitTex)); m.SetTextureScale("_BaseMap", new Vector2(3, 3)); return m; }).ToArray();
            var barkM = CM(0x4a3626, "plain", 1);
            Mesh foliage = null;
            foreach (var sd in new[] { -1f, 1f }) for (int zi = 40; zi > -200; zi -= 16)
                {
                    float lz = zi + (sd > 0 ? 8 : 0);
                    CS.tasks.Add(() =>
                    {
                        if (foliage == null) foliage = cinePre.Result.foliage.Build();
                        var lp = Joint(C, sd * 8.9f, 0.16f, lz);
                        Part(lp, CylG(0.07f, 0.11f, 7, 10), poleM, 0, 3.5f, 0);
                        Part(lp, CylG(0.05f, 0.05f, 1.6f, 8), poleM, -sd * 0.7f, 6.9f, 0, 0, 0, Mathf.PI / 2);
                        Part(lp, SphereG(0.22f, 16, 10), lampM, -sd * 1.45f, 6.75f, 0, 0, 0, 0, 1.3f, 0.6f, 1.3f);
                        float tz = lz - 8; var tr = Joint(C, sd * 11.6f, 0.16f, tz);
                        Part(tr, BoxG(1.6f, 0.5f, 1.6f), curbM, 0, 0.25f, 0);
                        Part(tr, CylG(0.12f, 0.2f, 3.6f, 10), barkM, 0, 1.9f, 0);
                        var crown = Joint(tr, 0, 3.9f, 0);
                        for (int k = 0; k < 4; k++)
                        {
                            var lm = pick(leafM); float px = cr(-0.9f, 0.9f), py = cr(-0.2f, 1.2f), pz = cr(-0.9f, 0.9f), rx = crand() * 3, ry = crand() * 3, sx = cr(1.3f, 1.9f), sy = cr(1.1f, 1.5f), sz = cr(1.3f, 1.9f);
                            Part(crown, foliage, lm, px, py, pz, rx, ry, 0, sx, sy, sz);
                        }
                        CS.trees.Add((crown, crand() * 6));
                    });
                }
            // parked cars: smooth extruded bodies, tinted glass, wheels with rims
            var carShape = new GeoX.Shape().MoveTo(-2.2f, 0.35f).LineTo(-2.25f, 0.75f).QuadTo(-2.2f, 0.95f, -1.9f, 0.98f).LineTo(-0.9f, 1.05f)
                .QuadTo(-0.55f, 1.42f, 0.0f, 1.45f).LineTo(0.85f, 1.43f).QuadTo(1.35f, 1.4f, 1.65f, 1.0f).LineTo(2.15f, 0.92f)
                .QuadTo(2.3f, 0.8f, 2.25f, 0.5f).LineTo(2.2f, 0.35f).LineTo(-2.2f, 0.35f);
            var carMB = GeoX.ExtrudeMB(carShape, 1.7f, 10, true, 0.08f, 0.08f, 4); carMB.Translate(V(0, 0, -0.85f)); var carGeo = carMB.Build();
            var winShape = new GeoX.Shape().MoveTo(-0.82f, 1.06f).QuadTo(-0.5f, 1.36f, 0, 1.39f).LineTo(0.82f, 1.37f).QuadTo(1.25f, 1.33f, 1.5f, 1.04f).LineTo(-0.82f, 1.06f);
            var winMB = GeoX.ExtrudeMB(winShape, 1.86f, 10, false); winMB.Translate(V(0, 0, -0.93f)); var winGeo = winMB.Build();
            var glassC = G(Mats.Std(0x0d1418, 0.05f, 0.9f)); var tireM = CM(0x141414, "plain", 0.9f); var rimM = CM(0xb8bcc4, "metal");
            foreach (var (sd, z) in new[] { (-1f, -40f), (-1f, -52f), (1f, -46f), (1f, -64f), (-1f, -78f), (1f, -90f), (-1f, 10f), (1f, 18f), (-1f, 28f), (1f, 34f), (-1f, -110f), (1f, -126f) })
                CS.tasks.Add(() =>
                {
                    var car = Joint(C, sd * 6.9f, 0, z); car.ry = Mathf.PI / 2 * (sd > 0 ? 1 : -1) + cr(-0.03f, 0.03f);
                    Part(car, carGeo, CM(pick(new[] { 0xa01818, 0x1d3f7a, 0xe8e8ea, 0x1a1a1c, 0x8a8f96, 0x2c5a3a, 0xc89a2a }), "paint"));
                    Part(car, winGeo, glassC);
                    foreach (var (wx, wz) in new[] { (-1.35f, -0.86f), (1.35f, -0.86f), (-1.35f, 0.86f), (1.35f, 0.86f) })
                    { Part(car, CylG(0.36f, 0.36f, 0.24f, 20), tireM, wx, 0.36f, wz, Mathf.PI / 2); Part(car, CylG(0.22f, 0.22f, 0.26f, 16), rimM, wx, 0.36f, wz, Mathf.PI / 2); }
                });
            // bunting strung across the avenue
            foreach (var z in new[] { -34f, -50f, -68f, -88f, 14f })
                CS.tasks.Add(() =>
                {
                    var g = Joint(C, 0, 7.6f, z); var pts = new List<Vector3>();
                    for (int i = 0; i <= 24; i++) { float u = i / 24f, x = -9 + u * 18; pts.Add(V(x, -1.6f * (1 - Mathf.Pow(2 * u - 1, 2)), 0)); }
                    Part(g, GeoX.TubeMB(new GeoX.CatmullRom(pts), 40, 0.015f, 5).Build(), CM(0x2a2a2a, "plain"));
                    var tri = GeoX.Cached("flagtri", () => GeoX.ShapeMB(new GeoX.Shape().MoveTo(-0.2f, 0).LineTo(0.2f, 0).LineTo(0, -0.5f)).Build());
                    for (int i = 1; i < 24; i++) { var f = Part(g, tri, CM(FLAGS[i % FLAGS.Length], "cloth", -1, -1, true), pts[i].x, pts[i].y, 0); NoShadow(f); }
                    CS.bunting.Add((g, crand() * 6));
                });
            // the crowd: rows facing the sunrise, with phones, flags and families
            var spots = new List<Vector2>();
            for (double zd = -27.5; zd < -7; zd += 1.05) for (double xd = -7.4; xd <= 7.4; xd += 1.0)
                {
                    if (crand() < 0.32f) continue;
                    float px = (float)xd + cr(-0.32f, 0.32f), pz = (float)zd + cr(-0.3f, 0.3f);
                    bool close = false; foreach (var s in spots) if (Hypot(s.x - px, s.y - pz) < 0.78f) { close = true; break; }
                    if (close) continue;
                    spots.Add(new Vector2(px, pz));
                }
            foreach (var sp in spots)
            {
                float r = crand(); string prop = r < 0.1f ? "phone" : r < 0.17f ? "flag" : null;
                CS.tasks.Add(() => { var p = BuildPerson(C.t, sp.x, sp.y, new PersonOpt { prop = prop }); p.prop = prop; CS.people.Add(p); if (prop != null) SetBehavior(p, prop); });
            }
            // walkers arriving to join the crowd, and children running around a tree
            var walkDefs = new[] {
                (from: V(-10.2f, 0, 22), via: new[] { V(-9.6f, 0, 8), V(-7.6f, 0, -2) }, to: V(-6.2f, 0, -6.5f), max: 1.35f, delay: 0.0f, run: false),
                (from: V(10.4f, 0, 26), via: new[] { V(9.8f, 0, 10) }, to: V(5.8f, 0, -5.8f), max: 1.25f, delay: 0.6f, run: false),
                (from: V(-2, 0, 30), via: new[] { V(-1, 0, 12) }, to: V(-0.5f, 0, -5.4f), max: 1.4f, delay: 1.2f, run: false),
                (from: V(3, 0, 34), via: new[] { V(2.2f, 0, 14) }, to: V(2.6f, 0, -5.6f), max: 2.7f, delay: 1.8f, run: true),
                (from: V(-11, 0, 34), via: new[] { V(-10.5f, 0, 14), V(-8.8f, 0, 2) }, to: V(-7.5f, 0, -4.2f), max: 2.6f, delay: 0.3f, run: true) };
            foreach (var d in walkDefs)
                CS.tasks.Add(() =>
                {
                    bool kidW = d.run && crand() < 0.5f;
                    var p = BuildPerson(C.t, d.from.x, d.from.z, new PersonOpt { yaw = Mathf.PI, kid = kidW });
                    var path = new List<Vector3>(d.via) { d.to };
                    p.walk = new Walk { path = path, max = d.max, run = d.run, delay = d.delay, then = "cheer" };
                    CS.people.Add(p);
                });
            for (int k = 0; k < 2; k++)
            {
                int kk = k;
                CS.tasks.Add(() =>
                {
                    var p = BuildPerson(C.t, 11.6f, -24 + kk * 3, new PersonOpt { kid = true, yaw = 0 }); var loop = new List<Vector3>();
                    for (int i = 0; i < 10; i++) { float a = i / 10f * Mathf.PI * 2 + kk * Mathf.PI; loop.Add(V(11.6f + Mathf.Cos(a) * 1.9f, 0, -24 + Mathf.Sin(a) * 2.6f)); }
                    p.walk = new Walk { path = loop, max = 2.4f, run = true, loop = true };
                    CS.people.Add(p);
                });
            }
            // confetti
            CS.tasks.Add(() =>
            {
                int n = 900; var cf = new Confetti { mesh = Mk.TwoSided(Geo.Plane(0.07f, 0.045f)), grp = new int[n], x = new float[n], y = new float[n], z = new float[n], vy = new float[n], ph = new float[n], sp = new float[n] };
                const int G2 = 12; cf.mats = new Material[G2]; cf.mtx = new List<Matrix4x4>[G2];
                for (int g = 0; g < G2; g++) { cf.mats[g] = G(Mats.Std(HexOf(HSL((g + 0.5f) / G2, 0.85f, 0.6f)), 0.5f, 0.35f)); cf.mats[g].enableInstancing = true; cf.mtx[g] = new List<Matrix4x4>(); }
                for (int i = 0; i < n; i++)
                {
                    float hue = crand(); cf.grp[i] = Mathf.Min(G2 - 1, (int)(hue * G2));
                    cf.x[i] = cr(-11, 11); cf.y[i] = cr(0, 16); cf.z[i] = cr(-34, 6); cf.vy[i] = cr(0.5f, 1.1f); cf.ph[i] = crand() * 6; cf.sp[i] = cr(2, 6);
                }
                CS.confetti = cf;
            });
            BuildCityExtras(C);
            CS.cityBirds = MakeFlock(C, 12, 0, 46, -120, 40, 0x2a2a30, false);
            var ct = cloudTexture(7);
            for (int i = 0; i < 9; i++) { float w = cr(160, 320), h = cr(40, 70), op = cr(0.35f, 0.6f); var s = CloudSprite(C, ct, 0xffc8a8, w, h, op); float px = cr(-500, 500), py = cr(70, 190), pz = cr(-900, -700); s.transform.localPosition = V(px, py, pz); }
            CS.tasks.Add(() => { CS.cityAmb = AmbientSH(CS.citySkyP, 0xffd6bc, 0x3a2e36, 0.9f); });
            CS.fwT = 0.4f;
        }

        // ----- city celebration extras: sky lanterns, released balloons, string lights, vendor carts, searchlights -----
        readonly List<(Transform t, float vy, float ph, Vector3 p0)> lanterns = new List<(Transform, float, float, Vector3)>();
        readonly List<(Transform t, float vy, float ph, Vector3 p0)> balloons = new List<(Transform, float, float, Vector3)>();
        readonly List<(N n, float ph, float sp)> searchlights = new List<(N, float, float)>();
        readonly List<Material> bulbMats = new List<Material>();
        void BuildCityExtras(N C)
        {
            // floating sky lanterns rising over the avenue: warm paper glowing from inside
            var paper = CM(0xffb060, "plain", 0.9f, 0, false, 0xff9a40, 2.4f); var rim = CM(0x6a3a1a, "plain");
            for (int i = 0; i < 46; i++)
            {
                var p0 = V(cr(-14, 14), cr(3, 55), cr(-110, 10));
                var l = Joint(C, p0.x, p0.y, p0.z);
                NoShadow(Part(l, CylG(0.24f, 0.17f, 0.46f, 10), paper));
                NoShadow(Part(l, CylG(0.175f, 0.175f, 0.03f, 10), rim, 0, -0.22f, 0));
                CGlow(l.t, 0xffa040, 2.2f, 0.55f);
                lanterns.Add((l.t, cr(0.5f, 1.1f), crand() * 6, p0));
            }
            // balloon bunches let go by the crowd, drifting up between the towers
            int[] bc = { 0xe8262a, 0x2a6ae8, 0xf2c230, 0x2ab05a, 0xe85ab0, 0xffffff, 0x8a4ae8 };
            for (int i = 0; i < 34; i++)
            {
                var p0 = V(cr(-7, 7), cr(1, 40), cr(-30, -6));
                var b = Joint(C, p0.x, p0.y, p0.z);
                Part(b, SphereG(0.26f, 14, 10), CM(pick(bc), "paint", 0.25f, 0.05f), 0, 0, 0, 0, 0, 0, 1, 1.18f, 1);
                Part(b, Geo.Cone(0.04f, 0.06f, 6), CM(0xdddddd, "plain"), 0, -0.32f, 0, Mathf.PI, 0, 0);
                NoShadow(Part(b, CylG(0.004f, 0.004f, 1.1f, 4), CM(0xeeeeee, "plain"), 0, -0.9f, 0));
                balloons.Add((b.t, cr(1.4f, 2.4f), crand() * 6, p0));
            }
            // warm string lights zig-zagging across the avenue
            var bulb = CM(0xfff0c0, "plain", 0.5f, 0, false, 0xffc070, 3);
            foreach (var z in new[] { -20f, -42f, -60f, -78f, 2f })
            {
                var g = Joint(C, 0, 9.4f, z); var pts = new List<Vector3>();
                for (int i = 0; i <= 26; i++) { float u = i / 26f, x = -13 + u * 26; pts.Add(V(x, -1.9f * (1 - Mathf.Pow(2 * u - 1, 2)), Mathf.Sin(u * Mathf.PI * 3) * 0.4f)); }
                NoShadow(Part(g, GeoX.TubeMB(new GeoX.CatmullRom(pts), 40, 0.012f, 4).Build(), CM(0x1a1a1a, "plain")));
                for (int i = 1; i < 26; i += 2)
                {
                    NoShadow(Part(g, SphereG(0.07f, 8, 6), bulb, pts[i].x, pts[i].y - 0.08f, pts[i].z));
                    var gl = CGlow(g.t, i % 4 == 1 ? 0xffd080 : 0xffb060, 1.1f, 0.6f); gl.transform.localPosition = pts[i] - V(0, 0.08f, 0);
                    bulbMats.Add(gl.GetComponent<Renderer>().sharedMaterial);
                }
            }
            // vendor carts on the sidewalks, striped awnings and a lamp under each
            foreach (var (sd, z, col) in new[] { (-1f, -8f, 0xc83a2a), (1f, 0f, 0x2a7ac8), (-1f, -40f, 0x2aa05a), (1f, -32f, 0xe8a020) })
            {
                var cart = Joint(C, sd * 11.2f, 0.16f, z); cart.ry = sd > 0 ? -Mathf.PI / 2 : Mathf.PI / 2;
                Part(cart, BoxG(2.2f, 1.0f, 1.1f), CM(col, "paint"), 0, 0.75f, 0);
                Part(cart, BoxG(2.3f, 0.08f, 1.2f), CM(0xd8d0c0, "plain"), 0, 1.29f, 0);
                foreach (var wx in new[] { -0.75f, 0.75f }) Part(cart, CylG(0.24f, 0.24f, 0.1f, 14), CM(0x1a1a1a, "plain"), wx, 0.24f, 0.58f, Mathf.PI / 2);
                foreach (var px in new[] { -1.05f, 1.05f }) Part(cart, CylG(0.03f, 0.03f, 1.1f, 6), CM(0x2a2a2a, "metal"), px, 1.85f, 0.5f);
                for (int k = 0; k < 6; k++) Part(cart, BoxG(0.4f, 0.05f, 1.5f), CM(k % 2 == 0 ? col : 0xf4f0e8, "cloth", -1, -1, true), -1.0f + k * 0.4f, 2.42f, 0.15f, 0.22f, 0, 0);
                var lamp = CGlow(cart.t, 0xffd890, 2.4f, 0.7f); lamp.transform.localPosition = V(0, 2.1f, 0.3f);
            }
            // searchlights sweeping the sky from the rooftops
            var beamM = Mats.Add(0xfff2d8, 0.07f, null, true, true); var headM = CM(0x2a2a30, "metal");
            foreach (var (x, y, z) in new[] { (-34f, 62f, -120f), (36f, 70f, -150f), (-40f, 55f, -60f), (42f, 58f, -90f) })
            {
                var sl = Joint(C, x, y, z);
                Part(sl, CylG(0.9f, 1.1f, 1.2f, 12), headM);
                var beam = Mk.Msh(sl.t, Geo.Cyl(7, 0.5f, 160, 20, true), beamM, 0, 80, 0); beam.layer = CINE_LAYER; NoShadow(beam);
                searchlights.Add((sl, crand() * 6, cr(0.25f, 0.45f)));
            }
            // low sunbeams pouring down the avenue from the sunrise
            foreach (var (x0, x1, r0, r1, a) in new[] { (0f, 0f, 6f, 16f, 0.09f), (-14f, -5f, 3f, 9f, 0.06f), (13f, 4f, 3f, 8f, 0.06f) })
            {
                var src = V(x0, 34, -280); var dst = V(x1, 0, -26); var dir = src - dst; float len = dir.magnitude;
                var m = Mats.Add(0xffd8a8, a, Tex.RayFade, true, true);
                var g = Mk.Msh(C.t, Geo.Cyl(r0, r1, 1, 24, true), m); g.layer = CINE_LAYER; NoShadow(g);
                g.transform.localPosition = (src + dst) * 0.5f; g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir / len); g.transform.localScale = V(1, len, 1);
            }
        }
        void UpdateCityExtras(float t, float dt)
        {
            foreach (var l in lanterns)
            {
                float y = l.p0.y + (t * l.vy) % 60; if (y > 62) y -= 60;
                l.t.localPosition = V(l.p0.x + Mathf.Sin(t * 0.35f + l.ph) * 1.5f, y, l.p0.z + Mathf.Cos(t * 0.28f + l.ph) * 1.2f);
                l.t.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.8f + l.ph) * 5, t * 12 + l.ph * 40, Mathf.Cos(t * 0.7f + l.ph) * 5);
            }
            foreach (var b in balloons)
            {
                float y = l2(b.p0.y + t * b.vy);
                b.t.localPosition = V(b.p0.x + Mathf.Sin(t * 0.9f + b.ph) * 0.6f + (y - b.p0.y) * 0.05f, y, b.p0.z + Mathf.Cos(t * 0.7f + b.ph) * 0.5f);
                b.t.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.7f + b.ph) * 8, 0, Mathf.Cos(t * 1.3f + b.ph) * 8);
            }
            float l2(float y) => y > 70 ? 1 + (y - 1) % 69 : y;
            for (int i = 0; i < bulbMats.Count; i++) Mats.SetAlpha(bulbMats[i], 0.45f + 0.2f * Mathf.Sin(t * 2.2f + i * 1.7f));
            foreach (var s in searchlights) s.n.SetRot(Mathf.Sin(t * s.sp + s.ph) * 0.55f, 0, Mathf.Cos(t * s.sp * 0.8f + s.ph) * 0.55f);
        }

        // ----- the hero on a sea cliff at sunrise -----
        void BuildCliff()
        {
            var Hg = CS.hero;
            // a blazing sunrise: deep blue overhead fading through rose into a molten gold horizon
            CS.heroSkyP = MakeSkyParams(0x1a2860, 0xc8789a, 0xffa448, 0x1a2a3a, new Vector3(0.1f, 0.03f, -1), 0xffd8a0, 2.1f);
            CS.heroSkyP.sunSize = 0.055f; CS.heroSkyP.sunPower = 26;
            var sky = CMat("BR_Sky"); ApplySky(sky, CS.heroSkyP); SkyDome(Hg, sky); CS.heroSky = sky;
            CS.heroProbe = MakeProbe(Hg);
            CS.heroSun = CineLight(Hg, 0xffb878, 4.6f, V(0, 38, -120), true);
            BuildSunrise(Hg);
            CineLight(Hg, 0x8eaaff, 0.6f, V(-20, 15, 30), false);
            Material RockMat(Color[] colr, int w, int h)
            {
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                t.SetPixels(colr); t.Apply(false); return G(Mats.Std(0xffffff, 0.95f, 0, 0, t));
            }
            CS.tasks.Add(() =>
            {
                var pre = cinePre.Result; var mesh = pre.cliff.Build();
                CS.cliff = Part(Hg, mesh, RockMat(pre.cliffCol, pre.cliffW, pre.cliffH), 0, -26 * 0.62f - 0.4f, 12);
                CS.cliff.AddComponent<MeshCollider>().sharedMesh = mesh;
            });
            CS.tasks.Add(() =>
            {
                var pre = cinePre.Result;
                var pos = new[] { V(-14, -20, -14), V(12, -22, -10), V(-6, -24, -24), V(22, -16, 6), V(-24, -14, 10) };
                for (int i = 0; i < pre.rocks.Count; i++) Part(Hg, pre.rocks[i].mb.Build(), RockMat(pre.rocks[i].colr, pre.rocks[i].w, pre.rocks[i].h), pos[i].x, pos[i].y, pos[i].z);
                var headM = G(Mats.Std(0x3a4250, 1));
                var hp = new[] { V(-380, -26, -620), V(420, -26, -760), V(-900, -26, -1100), V(160, -26, -1300) };
                for (int i = 0; i < pre.heads.Count; i++) Part(Hg, pre.heads[i].Build(), headM, hp[i].x, hp[i].y, hp[i].z);
                // more headlands along the right-hand horizon (the hero's side view looks out that way)
                var hp2 = new[] { V(820, -26, -420), V(1150, -26, 120), V(760, -26, 520), V(1400, -26, -900) };
                var headM2 = G(Mats.Std(0x4a4658, 1));
                for (int i = 0; i < pre.heads.Count && i < hp2.Length; i++) Part(Hg, pre.heads[i].Build(), i % 2 == 0 ? headM2 : headM, hp2[i].x, hp2[i].y, hp2[i].z, 0, i * 1.3f, 0, 1.1f, 0.9f + i * 0.15f, 1.1f);
            });
            // sea: normal-mapped water that reflects the dawn sky, scrolling in two directions
            CS.seaMat = CMat("BR_LitNormal"); CS.seaMat.SetColor("_BaseColor", Mats.Hex(0x0b2c42)); CS.seaMat.SetFloat("_Smoothness", 1 - 0.16f); CS.seaMat.SetFloat("_Metallic", 0.05f);
            CS.seaMat.SetFloat("_BumpScale", 0.55f); CS.seaMat.SetTextureScale("_BaseMap", new Vector2(46, 46));
            CS.tasks.Add(() => CS.seaMat.SetTexture("_BumpMap", TexFrom(cinePre.Result.waterN, 256, 256, false, true, true)));
            var seaMesh = GeoX.PlaneMB(3000, 3000).Build(); seaMesh.RecalculateTangents();
            Part(Hg, seaMesh, CS.seaMat, 0, -26, 0, -Mathf.PI / 2);
            var foamT = CvTex(256, 256, x =>
            {
                for (int i = 0; i < 260; i++)
                {
                    float px = UnityEngine.Random.value * 256, py = UnityEngine.Random.value * 256, r = 4 + UnityEngine.Random.value * 16, d = Hypot(px - 128, py - 128) / 128;
                    if (d > 1) continue;
                    x.Radial(px, py, r, (0, new Color(1, 1, 1, 0.5f * (1 - d))), (1, new Color(1, 1, 1, 0)));
                }
            }, false);
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2, w = cr(10, 18), h = cr(8, 14);
                var m = G(Mats.Trans(0xf0e4da, 0.5f, false, true)); m.SetTexture("_BaseMap", foamT);
                var f = Mk.Msh(Hg, GeoX.PlaneMB(w, h).Build(), m, Mathf.Cos(a) * 17, -25.92f, 12 + Mathf.Sin(a) * 26, -Mathf.PI / 2); NoShadow(f);
                CS.foam.Add((m, crand() * 6));
            }
            CS.heroBirds = MakeFlock(Hg, 7, -30, 10, -80, 45, 0xb4b8c0, true); // gulls over the sea
            var ct = cloudTexture(19);
            for (int i = 0; i < 10; i++) { float w = cr(260, 520), h = cr(50, 90), op = cr(0.35f, 0.65f); var s = CloudSprite(Hg, ct, 0xffb48c, w, h, op); float px = cr(-900, 900), py = cr(60, 200), pz = cr(-1300, -1000); s.transform.localPosition = V(px, py, pz); }
            BuildSideSea(Hg, ct);
            for (int i = 0; i < 12; i++) // rose-lit clouds wrapping round the sides, so every angle has a sky worth looking at
            {
                float a = cr(0.35f, 1.6f) * (i % 2 == 0 ? 1 : -1), d = cr(900, 1250), w = cr(240, 480), h = cr(40, 80);
                var s = CloudSprite(Hg, ct, i % 3 == 0 ? 0xffc0a0 : 0xe898b0, w, h, cr(0.3f, 0.55f));
                s.transform.localPosition = V(Mathf.Sin(a) * d, cr(80, 260), -Mathf.Cos(a) * d);
            }
            CS.tasks.Add(() =>
            {
                // place the hero near the seaward edge of the plateau (found by raycasting the real cliff surface)
                bool wasActive = Hg.go.activeSelf; Hg.go.SetActive(true); Physics.SyncTransforms(); // colliders in an inactive set cannot be hit
                bool hitBack = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true; // the mesh is wound for the mirrored camera: hit the first surface either way
                int mask = 1 << CINE_LAYER; float edgeZ = 0;
                bool Cast(float x, float z, out RaycastHit h) => Physics.Raycast(V(x, 60, z), Vector3.down, out h, 200, mask, QueryTriggerInteraction.Ignore);
                for (float z = 10; z > -40; z -= 0.25f) { if (!Cast(0, z, out var h) || h.point.y < -1.5f) break; edgeZ = z; }
                float hz = edgeZ + 2.4f; float hy = Cast(0, hz, out var hh) ? hh.point.y : 0;
                var hero = Joint(Hg, 0, hy, hz); hero.ry = Mathf.PI; CS.heroPos = hero.t.position;
                CS.heroRig = BuildHero(hero.t);
                // grass and wildflowers on the plateau around the hero, sampled on the actual surface
                var gr = new Grass(); int n = 1400, gi = 0, fi = 0;
                var flowers = new List<(Matrix4x4 m, int c)>(); var FC = new[] { 0xffe066, 0xffffff, 0xd88ad8, 0xffb070 };
                for (int k = 0; k < 4000 && (gi < n || fi < 160); k++)
                {
                    float a = crand() * Mathf.PI * 2, rr = Mathf.Sqrt(crand()) * 14, x = Mathf.Cos(a) * rr, z = hz + 4 + Mathf.Sin(a) * rr;
                    if (Hypot(x, z - hz) < 0.55f) continue;
                    if (!Cast(x, z, out var h) || Mathf.Abs(h.normal.y) < 0.6f || h.point.y < hy - 2.5f) continue;
                    if (gi < n) { float s = cr(0.6f, 1.2f); gr.st.Add((x, h.point.y, z, s, crand() * 6, crand() * 6, 0)); gi++; }
                    else if (fi < 160) { float ry = crand() * 6.28f, sc = cr(0.8f, 1.25f); int c = Array.IndexOf(FC, pick(FC)); flowers.Add((Matrix4x4.TRS(V(x, h.point.y, z), Mk.XYZ(0, ry, 0), Vector3.one * sc), c)); fi++; }
                }
                const int GB = 16; gr.gm = new Material[GB]; gr.gmtx = new List<Matrix4x4>[GB];
                var grassTex = GradTex(LinLerpColors(0x26381a, 0x8aa84a, 16));
                for (int g = 0; g < GB; g++)
                {
                    float hue = 0.16f + (g / 4 + 0.5f) / 4 * 0.08f, li = 0.7f + (g % 4 + 0.5f) / 4 * 0.25f;
                    gr.gm[g] = G(Mats.Std(HexOf(HSL(hue, 0.35f, li)), 0.85f, 0, 0, grassTex)); gr.gm[g].enableInstancing = true; gr.gmtx[g] = new List<Matrix4x4>();
                }
                for (int i = 0; i < gr.st.Count; i++)
                {
                    float hh2 = crand(), ll = crand(); var st = gr.st[i];
                    st.g = Mathf.Min(3, (int)(hh2 * 4)) * 4 + Mathf.Min(3, (int)(ll * 4)); gr.st[i] = st;
                }
                gr.grass = Mk.TwoSided(GrassClump()); gr.flower = Mk.TwoSided(FlowerGeo());
                var flowerTex = FlowerTex();
                gr.fm = new Material[4]; gr.fmtx = new List<Matrix4x4>[4];
                for (int c = 0; c < 4; c++) { gr.fm[c] = G(Mats.Std(FC[c], 0.6f, 0, 0, flowerTex)); gr.fm[c].enableInstancing = true; gr.fmtx[c] = new List<Matrix4x4>(); }
                var heroT = Hg.t; foreach (var f in flowers) gr.fmtx[f.c].Add(heroT.localToWorldMatrix * f.m);
                CS.grass = gr;
                AimLight(CS.heroSun, CS.heroPos + V(0.1f, 0.32f, -1).normalized * 120, CS.heroPos);
                Hg.go.SetActive(wasActive); Physics.queriesHitBackfaces = hitBack;
            });
            CS.tasks.Add(() => { CS.heroAmb = AmbientSH(CS.heroSkyP, 0xffd8c0, 0x2a3440, 0.85f); });
        }
        static Color[] LinLerpColors(int a, int b, int n)
        {
            Color ca = CinePre.LinHex(a), cb = CinePre.LinHex(b); var o = new Color[n];
            for (int i = 0; i < n; i++) o[i] = Color.Lerp(ca, cb, Mathf.Pow(i / (float)(n - 1), 0.8f));
            return o;
        }
        static Texture2D GradTex(Color[] linearCols)
        {
            var t = new Texture2D(1, linearCols.Length, TextureFormat.RGBA32, false, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            t.SetPixels(linearCols); t.Apply(false); return t;
        }
        static Texture2D FlowerTex()
        {
            var t = new Texture2D(3, 1, TextureFormat.RGBA32, false, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
            t.SetPixels(new[] { CinePre.LinHex(0x4a6a28), CinePre.LinHex(0xc89020), Color.white }); t.Apply(false); return t;
        }
        // a clump of five curved, tapering grass blades (colour gradient from the root to the sunlit tip via v)
        static Mesh GrassClump() => GeoX.Cached("grassClump", () =>
        {
            var b = new Geo.MB(); const int S = 5;
            for (int k = 0; k < 5; k++)
            {
                float a = k / 5f * Mathf.PI * 2 + k * 0.7f, lean = 0.12f + (k % 3) * 0.08f, hgt = 0.32f + (k % 2) * 0.16f, w = 0.022f; int o = b.v.Count;
                float dx = Mathf.Cos(a), dz = Mathf.Sin(a), px = -dz, pz = dx;
                for (int i = 0; i <= S; i++)
                {
                    float u = (float)i / S, y = hgt * u, bend = lean * u * u * hgt * 1.6f, hw = w * (1 - u * 0.92f);
                    float cx = dx * (0.02f + bend), cz = dz * (0.02f + bend);
                    b.v.Add(new Vector3(cx - px * hw, y, cz - pz * hw)); b.v.Add(new Vector3(cx + px * hw, y, cz + pz * hw));
                    b.uv.Add(new Vector2(0.5f, u)); b.uv.Add(new Vector2(0.5f, u));
                    if (i < S) { int kk = o + i * 2; b.Tri(kk, kk + 1, kk + 2); b.Tri(kk + 1, kk + 3, kk + 2); }
                }
            }
            b.ComputeNormals(); return b.Build();
        });
        // a wildflower: thin stem with a five-petal head (stem / centre / petals picked from a 3-texel strip)
        static Mesh FlowerGeo() => GeoX.Cached("flower", () =>
        {
            var b = new Geo.MB(); const float H = 0.3f;
            int Push(float x, float y, float z, float u) { b.v.Add(new Vector3(x, y, z)); b.uv.Add(new Vector2(u, 0.5f)); return b.v.Count - 1; }
            for (int i = 0; i < 3; i++)
            {
                float a = i / 3f * Mathf.PI * 2, bb = (i + 1) / 3f * Mathf.PI * 2, r = 0.005f;
                int v0 = Push(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r, 1f / 6), v1 = Push(Mathf.Cos(bb) * r, 0, Mathf.Sin(bb) * r, 1f / 6), v2 = Push(Mathf.Cos(a) * r, H, Mathf.Sin(a) * r, 1f / 6), v3 = Push(Mathf.Cos(bb) * r, H, Mathf.Sin(bb) * r, 1f / 6);
                b.Tri(v0, v1, v2); b.Tri(v1, v3, v2);
            }
            int cIdx = Push(0, H + 0.004f, 0, 0.5f); const int N2 = 30;
            for (int i = 0; i < N2; i++) { float a = i / (float)N2 * Mathf.PI * 2, r = 0.03f + 0.034f * Mathf.Abs(Mathf.Cos(a * 2.5f)); Push(Mathf.Cos(a) * r, H + 0.008f * Mathf.Abs(Mathf.Cos(a * 2.5f)), Mathf.Sin(a) * r, 5f / 6); }
            for (int i = 0; i < N2; i++) b.Tri(cIdx, cIdx + 1 + i, cIdx + 1 + (i + 1) % N2);
            b.ComputeNormals(); return b.Build();
        });

        // ----- per-frame life -----
        void UpdateCityLife(float t, float dt)
        {
            float sunY = 0.035f + 0.09f * ease(t / 8); var sd = V(0, sunY, -1).normalized;
            CS.citySky.SetVector("_SunDir", sd);
            var tgt = V(0, 0, -16); AimLight(CS.citySun, tgt + V(0.05f, Mathf.Max(0.16f, sunY * 2.2f), -1).normalized * 160, tgt);
            // fireworks over both ends of the avenue, so every camera angle catches some
            CS.fwT -= dt;
            if (CS.fwT <= 0)
            {
                CS.fwT = cr(0.35f, 0.8f);
                bool behind = crand() < 0.4f;
                float fx = cr(-45, 45), fy = cr(38, 70), fz = behind ? cr(40, 90) : cr(-170, -70);
                LaunchFirework(CS.city, V(fx, fy, fz));
            }
            UpdateFireworks(dt);
            if (CS.lastBurstT >= 0) CS.lastBurstT += dt;
            var sunPoint = V(0, 30, -400); var camP = cineCam.transform.position;
            for (int i = 0; i < CS.people.Count; i++)
            {
                var p = CS.people[i];
                UpdateWalker(p, dt, Mathf.PI);
                if (p.walk != null && p.walk.done) { p.yaw += shortAng(Mathf.PI - p.yaw) * damp(1.5f, dt); p.root.ry = p.yaw; }
                // where is this person looking: the sunrise, a firework that just burst, a neighbour, or (rarely) the camera
                p.lookT -= dt;
                if (CS.lastBurstT >= 0 && CS.lastBurstT < dt * 1.5f && crand() < 0.55f) { p.look = CS.lastBurstPos; p.lookT = cr(1.3f, 2.6f); if (p.beh == "idle" && crand() < 0.5f) SetBehavior(p, "point"); }
                if (p.lookT <= 0)
                {
                    p.lookT = cr(1.5f, 4);
                    float r = crand(); var pos = p.root.pos;
                    if (r < 0.55f) { float ax = cr(-60, 60), ay = cr(-10, 40); p.look = sunPoint + V(ax, ay, 0); }
                    else if (r < 0.8f) { float ax = cr(-3, 3), az = cr(-2, 2); p.look = V(pos.x + ax, 1.5f, pos.z + az); }
                    else if (r < 0.9f) p.look = camP;
                    else { float ax = cr(-60, 60), az = cr(-160, 80); p.look = V(ax, 55, az); }
                }
                if (p.walk != null && !p.walk.done) p.look = V(p.root.pos.x + Mathf.Sin(p.yaw) * 10, 1.6f, p.root.pos.z + Mathf.Cos(p.yaw) * 10);
                AnimatePerson(p, t + p.ph, dt, p.look);
            }
            if (CS.confetti != null)
            {
                var cf = CS.confetti; foreach (var l in cf.mtx) l.Clear();
                var parent = CS.city.t.localToWorldMatrix;
                for (int i = 0; i < cf.x.Length; i++)
                {
                    cf.y[i] -= cf.vy[i] * dt; if (cf.y[i] < 0.02f) { cf.y[i] = cr(10, 16); cf.x[i] = cr(-11, 11); cf.z[i] = cr(-34, 6); }
                    var q = Mk.XYZ(t * cf.sp[i] + cf.ph[i], t * cf.sp[i] * 0.7f, cf.ph[i]);
                    var v = V(cf.x[i] + Mathf.Sin(t * 1.3f + cf.ph[i]) * 0.4f, cf.y[i], cf.z[i] + Mathf.Cos(t * 1.1f + cf.ph[i]) * 0.3f);
                    cf.mtx[cf.grp[i]].Add(parent * Matrix4x4.TRS(v, q, Vector3.one));
                }
            }
            foreach (var b in CS.bunting) b.g.rx = 0.06f * Mathf.Sin(t * 1.4f + b.ph);
            foreach (var tr in CS.trees) tr.crown.SetRot(0.02f * Mathf.Sin(t * 0.7f + tr.ph), 0, 0.025f * Mathf.Sin(t * 0.9f + tr.ph));
            foreach (var bl in CS.blinkers) Mats.Emissive(bl, 0xff2010, Mathf.Sin(t * 3) > 0 ? 4 : 0.3f);
            FlapBirds(CS.cityBirds, t, dt);
            UpdateCityExtras(t, dt);
        }
        void UpdateHeroLife(float t, float dt)
        {
            if (CS.heroRig != null) AnimateHero(t, dt, cineCam.transform.position, 6.0f);
            if (CS.grass != null)
            {
                var gr = CS.grass; foreach (var l in gr.gmtx) l.Clear();
                var parent = CS.hero.t.localToWorldMatrix;
                foreach (var g in gr.st)
                {
                    float gust = 0.18f + 0.1f * Mathf.Sin(t * 2.1f + g.x * 0.4f + g.z * 0.3f);
                    var q = Mk.XYZ(-gust * 0.9f + 0.08f * Mathf.Sin(t * 5 + g.ph), g.ry, 0.06f * Mathf.Sin(t * 3.3f + g.ph));
                    gr.gmtx[g.g].Add(parent * Matrix4x4.TRS(V(g.x, g.y, g.z), q, V(g.s, g.s * 1.1f, g.s)));
                }
            }
            CS.seaMat.SetTextureOffset("_BaseMap", new Vector2(t * 0.004f, -t * 0.006f));
            foreach (var f in CS.foam) Mats.SetAlpha(f.m, 0.35f + 0.3f * Mathf.Sin(t * 0.9f + f.ph));
            FlapBirds(CS.heroBirds, t, dt);
            UpdateSunrise(t); UpdateSideSea(t);
        }

        // ----- what the side view looks out on: a lighthouse on a sea stack and a sky full of cloud -----
        Material lampMat;
        void BuildSideSea(N Hg, Texture2D ct)
        {
            float PI = Mathf.PI;
            // sea stack with a lighthouse, catching the dawn light
            var rock = G(Mats.Std(0x5a4a48, 0.95f)); var white = G(Mats.Std(0xf2ece4, 0.6f)); var red = G(Mats.Std(0xb02a2a, 0.55f));
            var dark = G(Mats.Std(0x2a2a30, 0.5f, 0.6f)); var glass = G(Mats.Basic(0xffe6a0));
            var stack = Joint(Hg, 330, -26, -70);
            Part(stack, Geo.Cyl(16, 26, 34, 9), rock, 0, 17, 0, 0, 0.3f, 0);
            Part(stack, Geo.Cyl(11, 16, 10, 9), rock, 2, 38, 1, 0.08f, 0.9f, 0.05f);
            Part(stack, Geo.Cyl(6, 9, 6, 8), rock, -9, 3, 14, 0, 0.5f, 0);
            var lh = Joint(stack, 1, 43, 1);
            Part(lh, Geo.Cyl(2.1f, 3.0f, 18, 20), white, 0, 9, 0);
            for (int i = 0; i < 3; i++) Part(lh, Geo.Cyl(2.35f - i * 0.25f, 2.55f - i * 0.25f, 2.2f, 20), red, 0, 3.5f + i * 5, 0);
            Part(lh, Geo.Cyl(3.0f, 3.0f, 0.6f, 20), dark, 0, 18.3f, 0);
            Part(lh, Geo.Cyl(1.7f, 1.7f, 2.6f, 16), glass, 0, 19.9f, 0);
            Part(lh, Geo.Cone(2.2f, 2.4f, 16), red, 0, 22.4f, 0);
            var lamp = CGlow(lh.t, 0xffd890, 30, 0.9f); lamp.transform.localPosition = V(0, 20, 0); lampMat = lamp.GetComponent<Renderer>().sharedMaterial;
            // clouds piled up on the right-hand horizon
            for (int i = 0; i < 10; i++)
            {
                float a = cr(1.0f, 2.3f), d = cr(800, 1200), w = cr(220, 460), h = cr(40, 90);
                var s = CloudSprite(Hg, ct, i % 2 == 0 ? 0xf0a0b8 : 0xffc0a0, w, h, cr(0.35f, 0.6f));
                s.transform.localPosition = V(Mathf.Sin(a) * d, cr(50, 240), -Mathf.Cos(a) * d);
            }
        }
        void UpdateSideSea(float t)
        {
            if (lampMat) Mats.SetAlpha(lampMat, 0.65f + 0.3f * Mathf.Sin(t * 2.4f));
        }
        // ----- the rising sun: blazing disc, layered glows, slow god rays and a glittering path on the sea -----
        Transform sunFx, sunRays; readonly List<(Material m, float ph, float sp)> glints = new List<(Material, float, float)>();
        static Vector3 SunDirAt(float t) => new Vector3(0.1f, Mathf.Lerp(0.012f, 0.07f, Mathf.Clamp01(t / 9)), -1).normalized;
        void BuildSunrise(N Hg)
        {
            sunFx = Mk.Piv(Hg).t; Mk.billboards.Add(sunFx);
            Material Hot(Texture tex, Color c) { var m = G(Mats.Add(0xffffff, 1, tex, false)); m.SetColor("_BaseColor", c); return m; }
            GameObject Sprite(Transform p, Texture tex, Color c, float size)
            {
                var g = Mk.Msh(p, Geo.Plane(1, 1), Hot(tex, c)); g.layer = CINE_LAYER; g.transform.localScale = V(size, size, 1); NoShadow(g); return g;
            }
            Sprite(sunFx, Tex.Glow, new Color(1.1f, 0.5f, 0.18f, 0.55f), 1400);   // wide warm bloom over the horizon
            Sprite(sunFx, Tex.Glow, new Color(1.6f, 1.0f, 0.5f, 0.9f), 420);      // golden halo
            Sprite(sunFx, Tex.Glow, new Color(5, 4.2f, 3.2f, 1), 150);           // white-hot core (HDR, so it blooms)
            sunRays = Mk.Piv(sunFx).t;
            Sprite(sunRays, Tex.Rays, new Color(1.4f, 0.9f, 0.5f, 0.32f), 1500);  // god rays, turning slowly
            Sprite(sunRays, Tex.Rays, new Color(1.2f, 0.75f, 0.4f, 0.2f), 1100).transform.localRotation = Quaternion.Euler(0, 0, 17);
            // sun glitter: sparkles scattered along the sun's path on the water, each group twinkling on its own beat
            var gm = new Material[10];
            for (int i = 0; i < gm.Length; i++) { gm[i] = Hot(Tex.Glow, new Color(3, 2.4f, 1.6f, 1)); glints.Add((gm[i], cr(0, 6.3f), cr(5, 11))); }
            var path = SunDirAt(4); var flat = new Vector3(path.x, 0, path.z).normalized; var side = Vector3.Cross(Vector3.up, flat);
            for (int i = 0; i < 260; i++)
            {
                float d = Mathf.Lerp(30, 1100, Mathf.Pow(crand(), 1.6f)), off = (crand() - 0.5f) * d * 0.09f;
                var p = flat * d + side * off; float s = Mathf.Max(0.35f, d * 0.006f) * cr(0.6f, 1.4f);
                var g = Mk.Msh(Hg, Geo.Plane(1, 1), gm[i % gm.Length]); g.layer = CINE_LAYER; NoShadow(g);
                g.transform.localPosition = V(p.x, -25.8f, p.z + 12); g.transform.localScale = V(s * 2.2f, s, 1); Mk.billboards.Add(g.transform);
            }
        }
        void UpdateSunrise(float t)
        {
            if (sunFx == null) return;
            var d = SunDirAt(t);
            CS.heroSky.SetVector("_SunDir", d);
            sunFx.localPosition = d * 1500;
            sunRays.localRotation = Quaternion.Euler(0, 0, t * 2.5f);
            foreach (var g in glints) Mats.SetAlpha(g.m, Mathf.Pow(0.5f + 0.5f * Mathf.Sin(t * g.sp + g.ph), 3));
        }
        static readonly Matrix4x4[] instBuf = new Matrix4x4[1023];
        void DrawInst(Mesh mesh, Material mat, List<Matrix4x4> list, bool shadows)
        {
            for (int i = 0; i < list.Count; i += 1023)
            {
                int n = Mathf.Min(1023, list.Count - i); list.CopyTo(i, instBuf, 0, n);
                Graphics.DrawMeshInstanced(mesh, 0, mat, instBuf, n, null, shadows ? ShadowCastingMode.On : ShadowCastingMode.Off, true, CINE_LAYER, cineCam);
            }
        }
        void DrawCineInstances(string view)
        {
            if (view == "city" && CS.confetti != null) for (int g = 0; g < CS.confetti.mats.Length; g++) DrawInst(CS.confetti.mesh, CS.confetti.mats[g], CS.confetti.mtx[g], false);
            if (view == "hero" && CS.grass != null)
            {
                for (int g = 0; g < CS.grass.gm.Length; g++) DrawInst(CS.grass.grass, CS.grass.gm[g], CS.grass.gmtx[g], true);
                for (int c = 0; c < CS.grass.fm.Length; c++) DrawInst(CS.grass.flower, CS.grass.fm[c], CS.grass.fmtx[c], false);
            }
        }
    }
}
