using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BR
{
    // The ending cutscene. The two in-arena shots are drawn by the gameplay renderer (half-res, pixelated); the
    // cinematic views (planet, city, hero) by a full-resolution HDR camera with ACES tone mapping, bloom, soft shadows
    // and sky reflections. Ported from the HTML game's cinematic cutscene.
    public partial class Game
    {
        const int CINE_LAYER = 10, CINE_SKY = 11;
        class CineState { public bool active, cap2, capOff; public float t; public int shot = -1; }
        readonly CineState CINE = new CineState();
        bool cineHQ; Volume cineVolume; Bloom cineBloom; ColorAdjustments cineCA;
        static float damp(float k, float dt) => 1 - Mathf.Exp(-k * dt);
        static float ease3(float t) => t <= 0 ? 0 : t >= 1 ? 1 : t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
        static float wander(float t, float seed) => Noise.Wander(t, seed);
        static float sstep(float a, float b, float x) => Noise.SStep(a, b, x);
        static float shortAng(float a) => Mathf.Atan2(Mathf.Sin(a), Mathf.Cos(a));
        // seeded generator (same sequence as the HTML game, so the crowd and city come out identical)
        long cineSeed = 1;
        float crand() { cineSeed = cineSeed * 16807 % 2147483647; return (float)(cineSeed / 2147483647.0); }
        T pick<T>(T[] a) => a[Mathf.Min(a.Length - 1, (int)(crand() * a.Length))];
        float cr(float a, float b) => a + (b - a) * crand();
        static Material CMat(string res) { var m = new Material(Resources.Load<Material>(res)); m.name = res + "(G)"; return m; }

        // ---------- render mode: half-res gameplay view or full-resolution cinematic view ----------
        void SetCineHQ(bool hq)
        {
            cineHQ = hq;
            cineCam.targetTexture = hq ? null : rt;
            var d = cineCam.GetComponent<UniversalAdditionalCameraData>();
            d.renderPostProcessing = hq; d.antialiasing = hq ? AntialiasingMode.SubpixelMorphologicalAntiAliasing : AntialiasingMode.None;
            d.antialiasingQuality = AntialiasingQuality.High; d.renderShadows = hq;
            cineCam.allowHDR = hq;
            screenImg.enabled = !hq; uiCam.enabled = !hq;
            if (cineVolume) cineVolume.enabled = hq;
            sunLight.enabled = !hq;
            if (!hq) { RenderSettings.sun = sunLight; var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset; if (urp) urp.shadowDistance = 50; }
        }
        void EnsureVolume()
        {
            if (cineVolume) return;
            var prof = Resources.Load<VolumeProfile>("BR_CineVolume");
            var go = new GameObject("CineVolume"); cineVolume = go.AddComponent<Volume>(); cineVolume.isGlobal = true; cineVolume.priority = 10;
            cineVolume.sharedProfile = prof ? prof : ScriptableObject.CreateInstance<VolumeProfile>();
            var p = cineVolume.sharedProfile;
            if (!p.TryGet(out cineBloom)) cineBloom = p.Add<Bloom>(true);
            if (!p.TryGet(out Tonemapping tm)) tm = p.Add<Tonemapping>(true);
            if (!p.TryGet(out cineCA)) cineCA = p.Add<ColorAdjustments>(true);
            tm.active = true; tm.mode.Override(TonemappingMode.ACES);
            cineBloom.active = true; cineBloom.scatter.Override(0.7f);
            cineCA.active = true;
            cineVolume.enabled = false;
        }
        // three.js toneMappingExposure (its ACES fit pre-scales by 1/0.6) and UnrealBloomPass(strength, radius, threshold)
        void SetPost(float exposure, float strength, float threshold)
        {
            cineCA.postExposure.Override(Mathf.Log(exposure / 0.6f, 2));
            cineBloom.intensity.Override(strength); cineBloom.threshold.Override(Mathf.LinearToGammaSpace(threshold));
        }

        // ---------- materials (cached, physically based; fabric gets a subtle weave) ----------
        Texture2D fabricTex, denimTex, knitTex;
        static Texture2D NoiseTex(float b, float amp, Func<int, int, float> lines)
        {
            var t = new Texture2D(128, 128, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, anisoLevel = 4 };
            var px = new Color32[128 * 128];
            for (int j = 0; j < 128; j++) for (int i = 0; i < 128; i++) { byte v = (byte)Mathf.Clamp(b + (UnityEngine.Random.value - 0.5f) * amp + lines(i, j), 0, 255); px[j * 128 + i] = new Color32(v, v, v, 255); }
            t.SetPixels32(px); t.Apply(true); return t;
        }
        void ClothTextures()
        {
            if (fabricTex) return;
            fabricTex = NoiseTex(232, 26, (i, j) => (i + j) % 4 < 2 ? 6 : -6);
            denimTex = NoiseTex(225, 40, (i, j) => (i + j * 2) % 6 < 3 ? 14 : -10);
            knitTex = NoiseTex(228, 22, (i, j) => Mathf.Sin(i * 0.9f) * 10 + ((j % 6) < 3 ? 6 : -6));
        }
        readonly Dictionary<string, Material> cmCache = new Dictionary<string, Material>();
        Material CM(int color, string kind = "cloth", float rough = -1, float metal = -1, bool dbl = false, int emis = 0, float ei = 1)
        {
            ClothTextures();
            string key = $"{color}{kind}{rough}{metal}{dbl}{emis}{ei}";
            if (cmCache.TryGetValue(key, out var m)) return m;
            float r = 0.85f, mt = 0; Texture tex = null;
            switch (kind)
            {
                case "cloth": tex = fabricTex; break;
                case "denim": tex = denimTex; r = 0.9f; break;
                case "knit": tex = knitTex; r = 0.95f; break;
                case "skin": r = 0.72f; break;
                case "hair": r = 0.45f; mt = 0.05f; break;
                case "leather": r = 0.5f; break;
                case "metal": r = 0.32f; mt = 0.85f; break;
                case "paint": r = 0.28f; mt = 0.55f; break;
                case "plain": r = 0.7f; break;
            }
            if (rough >= 0) r = rough; if (metal >= 0) mt = metal;
            m = G(Mats.Std(color, r, mt, 0, tex, dbl));
            if (tex) m.SetTextureScale("_BaseMap", new Vector2(3, 3));
            if (emis != 0) Mats.Emissive(m, emis, ei);
            return cmCache[key] = m;
        }

        // ---------- character kit: smooth tapered limbs, shaped torsos, sculpted heads ----------
        bool kitHi; // the hero is built at full tessellation; the crowd uses a lighter kit
        Mesh LimbG(float r1, float r2, float len, int seg = -1) { if (seg < 0) seg = kitHi ? 18 : 12; return GeoX.Limb(r1, r2, len, seg, seg >= 16 ? 6 : 4); }
        static readonly float[] TORSO_M = { -0.11f, 0.0f, -0.1f, 0.09f, -0.05f, 0.14f, 0.03f, 0.148f, 0.12f, 0.132f, 0.21f, 0.138f, 0.3f, 0.158f, 0.38f, 0.166f, 0.44f, 0.158f, 0.49f, 0.125f, 0.525f, 0.07f, 0.54f, 0.0f };
        static readonly float[] TORSO_F = { -0.11f, 0.0f, -0.1f, 0.1f, -0.05f, 0.158f, 0.03f, 0.152f, 0.12f, 0.118f, 0.2f, 0.122f, 0.27f, 0.142f, 0.34f, 0.142f, 0.41f, 0.132f, 0.47f, 0.104f, 0.505f, 0.055f, 0.515f, 0.0f };
        static float[] Torso(string k) => k == "m" ? TORSO_M : TORSO_F;
        Mesh TorsoG(string kind) => GeoX.Lathe("torso" + kind, GeoX.YR(Torso(kind)), kitHi ? 32 : 20);
        static Mesh CurveG(float[] yr, int seg, float p0, float pl, string key) => GeoX.Lathe("cv" + key, GeoX.YR(yr), seg, p0, pl);
        // head: an egg that narrows into a jaw and chin, slightly flattened face, fuller back of the skull
        Mesh HeadG() => GeoX.Cached("head" + kitHi, () =>
        {
            var g = kitHi ? Geo.SphereMB(0.1f, 36, 28) : Geo.SphereMB(0.1f, 26, 20);
            for (int i = 0; i < g.v.Count; i++)
            {
                var v = g.v[i]; float yn = v.y / 0.1f;
                if (yn < 0) { float k = 1 - 0.3f * Mathf.Pow(-yn, 1.7f); v.x *= k; v.z *= 1 - 0.18f * Mathf.Pow(-yn, 1.5f); v.z += 0.012f * Mathf.Pow(-yn, 2) * Mathf.Max(0, v.z / 0.1f); }
                if (v.z < 0) v.z *= 1.06f;
                if (v.z > 0.06f) v.z -= (v.z - 0.06f) * 0.35f;
                v.y *= 1.17f; v.x *= 0.93f; g.v[i] = v;
            }
            g.ComputeNormals(); return g.Build();
        });
        Mesh SphereG(float r, int a = -1, int b = -1) => Geo.Sphere(r, a < 0 ? (kitHi ? 22 : 14) : a, b < 0 ? (kitHi ? 16 : 10) : b);
        static Mesh CapG(float r, float l) => GeoX.Capsule(r, l, 6, 12);
        Mesh DomeG(float r, float theta) => Geo.Sphere(r, kitHi ? 32 : 22, kitHi ? 18 : 11, 0, Mathf.PI * 2, 0, theta);
        Mesh TorG(float r, float t, float arc = Mathf.PI * 2) => Geo.Torus(r, t, kitHi ? 28 : 18, kitHi ? 10 : 6, arc);
        static Mesh CylG(float rt, float rb, float h, int s = 20) => Geo.Cyl(rt, rb, h, s);
        static Mesh BoxG(float w, float h, float d) => Geo.Box(w, h, d);

        GameObject Part(N parent, Mesh geo, Material mat, float x = 0, float y = 0, float z = 0, float rx = 0, float ry = 0, float rz = 0, float sx = 1, float sy = 1, float sz = 1)
        {
            var m = Mk.Msh(parent, geo, mat, x, y, z); m.transform.localRotation = Mk.XYZ(rx, ry, rz); m.transform.localScale = new Vector3(sx, sy, sz); // part() uses three's default XYZ order
            var r = m.GetComponent<MeshRenderer>(); r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; r.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            return m;
        }
        GameObject Part(Transform parent, Mesh geo, Material mat, float x = 0, float y = 0, float z = 0, float rx = 0, float ry = 0, float rz = 0, float sx = 1, float sy = 1, float sz = 1) =>
            Part(new N(parent), geo, mat, x, y, z, rx, ry, rz, sx, sy, sz);
        static N Joint(N parent, float x = 0, float y = 0, float z = 0) => Mk.Piv(parent, x, y, z);
        static N Joint(Transform parent, float x = 0, float y = 0, float z = 0) => Mk.Piv(parent, x, y, z);
        GameObject CGlow(Transform parent, int color, float size, float op)
        {
            var g = Mk.Glow(parent, color, size, op, false); g.layer = CINE_LAYER; G(g.GetComponent<Renderer>().sharedMaterial); return g;
        }
        static void NoShadow(GameObject g) { foreach (var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off; }

        // =====================================================================================================
        // Civilians. Each person is collapsed into ONE skinned mesh (its joints become the bones) with one shared
        // material, like the HTML game's skinRig; part colours go into a small palette texture.
        // =====================================================================================================
        static readonly int[] SKINS = { 0xf6d3b8, 0xeec09c, 0xe0a982, 0xc98d62, 0xa66e47, 0x7f4f30, 0x5c3a24 };
        static readonly int[] HAIRS = { 0x16100c, 0x2a1a10, 0x45291a, 0x6a4326, 0x94663a, 0xc79a5c, 0xe3cc94, 0x8c2c14, 0x3c3c40, 0xbdb8b2 };
        static readonly int[] TOPS = { 0xb8322a, 0x2f78b5, 0x2a9a5c, 0xe8b830, 0x7d4aa0, 0xe07a2a, 0xece6dc, 0x2f3d55, 0x1c8f84, 0xc9513a, 0xf08fb0, 0x8cb8de, 0x4c5a32, 0x9a3050 };
        static readonly int[] BOTTOMS = { 0x2a3446, 0x1c2230, 0x55402e, 0x3c5476, 0x2e3a52, 0x6e5a3e, 0x4a4e58, 0x22252b };
        static readonly int[] SHOES = { 0x121212, 0xf0f0ee, 0x3a2a1c, 0x7a1e1e, 0x2a4470, 0x5a5a60 };
        static readonly int[] FLAGS = { 0xd8262a, 0x2a62c8, 0xf2c230, 0x2aa05a, 0xffffff };
        class PPart { public N joint; public Mesh mesh; public int color; public Matrix4x4 local; }
        class Walk { public List<Vector3> path; public int i; public float speed, max, phase, amt, delay; public bool run, loop, done; public string then; }
        class Person
        {
            public N root, hips, spine, torso, neck, head, thL, knL, ftL, thR, knR, ftR, shL, elL, wrL, shR, elR, wrR, mouthO, pony;
            public List<N> eyes = new List<N>(); public List<PPart> parts = new List<PPart>();
            public string prop, beh = "idle", prevBeh; public bool kid, female, clasp; public float s, ph, tempo, blinkT, lookY, lookP, blend = 1, behT, hipBase, ponyV, ponyA, yaw, lookT;
            public float[] cur; public Walk walk; public Vector3? look; public DynMesh flag; public GameObject phone;
        }
        class PersonOpt { public bool? kid, female; public float? scale, yaw; public string top, hair, prop; }
        Person curPerson;
        void PP(N parent, Mesh geo, int color, float x = 0, float y = 0, float z = 0, float rx = 0, float ry = 0, float rz = 0, float sx = 1, float sy = 1, float sz = 1) =>
            curPerson.parts.Add(new PPart { joint = parent, mesh = geo, color = color, local = Matrix4x4.TRS(new Vector3(x, y, z), Mk.XYZ(rx, ry, rz), new Vector3(sx, sy, sz)) });
        static float R3(float v) => Mathf.Round(v * 1000) / 1000;

        Person BuildPerson(Transform parent, float x, float z, PersonOpt opt = null)
        {
            opt = opt ?? new PersonOpt();
            bool kid = opt.kid ?? crand() < 0.13f, female = opt.female ?? crand() < 0.5f;
            float s = opt.scale ?? (kid ? cr(0.58f, 0.68f) : cr(0.94f, 1.06f) * (female ? 0.95f : 1));
            float b = kid ? 0.95f : cr(0.9f, 1.12f);
            int skin = pick(SKINS), hairC = pick(HAIRS), hair = hairC;
            var topKinds = female ? new[] { "tee", "tee", "long", "hoodie", "jacket", "dress", "tank" } : new[] { "tee", "tee", "long", "hoodie", "jacket", "tank" };
            string topKind = opt.top ?? pick(kid ? new[] { "tee", "hoodie", "tee" } : topKinds);
            int top = pick(TOPS);
            string botKind = topKind == "dress" ? "dress" : pick(female ? new[] { "jeans", "jeans", "trousers", "shorts", "skirt" } : new[] { "jeans", "jeans", "trousers", "shorts" });
            int bot = botKind == "jeans" ? pick(new[] { 0x3c5476, 0x2e3a56, 0x4b6488, 0x23293a }) : pick(BOTTOMS);
            int shoeCol = pick(SHOES), shoe = shoeCol, sole = shoeCol == 0xf0f0ee || crand() < 0.5f ? 0xf4f4f0 : 0x1a1a1a;
            int jacket = -1; if (topKind == "jacket") { jacket = pick(new[] { 0x2b2118, 0x1f2a36, 0x3a3f46, 0x5b2a24, 0x34402c, 0x6b5a46 }); crand(); }
            var p = new Person { prop = opt.prop, kid = kid, female = female, s = s };
            curPerson = p;
            p.ph = crand() * 100; p.tempo = cr(0.85f, 1.2f); p.blinkT = cr(1, 4); p.behT = cr(0.5f, 3);
            p.root = Joint(parent, x, 0, z); p.root.Scale(s);
            p.hips = Joint(p.root, 0, 0.93f, 0); p.hipBase = 0.93f;
            // ---- legs ----
            float legHip = female ? 0.09f : 0.086f, thighR = R3((female ? 0.085f : 0.082f) * b);
            foreach (var (sd, L) in new[] { (1f, "L"), (-1f, "R") })
            {
                var th = Joint(p.hips, legHip * b * sd, -0.04f, 0);
                bool bareThigh = botKind == "dress" || botKind == "skirt", bareShin = bareThigh || botKind == "shorts";
                PP(th, LimbG(thighR, 0.058f, 0.43f), bareThigh ? skin : bot);
                if (botKind == "shorts") PP(th, LimbG(R3(thighR * 1.08f), 0.07f, 0.24f), bot);
                var kn = Joint(th, 0, -0.43f, 0);
                PP(kn, LimbG(0.058f, 0.04f, 0.4f), bareShin ? skin : bot);
                if (!bareShin) PP(kn, LimbG(0.061f, 0.05f, 0.36f), bot, 0, -0.03f, 0);
                var ft = Joint(kn, 0, -0.41f, 0);
                PP(ft, SphereG(1, 14, 10), shoe, 0, -0.035f, 0.045f, 0, 0, 0, 0.05f, 0.042f, 0.122f);
                PP(ft, SphereG(1, 14, 10), sole, 0, -0.066f, 0.045f, 0, 0, 0, 0.053f, 0.014f, 0.126f);
                if (L == "L") { p.thL = th; p.knL = kn; p.ftL = ft; } else { p.thR = th; p.knR = kn; p.ftR = ft; }
            }
            // ---- torso ----
            p.spine = Joint(p.hips, 0, 0.02f, 0);
            string tk = female ? "f" : "m"; float tz = female ? 0.72f : 0.68f; var TP = Torso(tk);
            p.torso = Joint(p.spine);
            PP(p.torso, TorsoG(tk), top, 0, 0, 0, 0, 0, 0, b, 1, tz);
            if (botKind != "dress")
            {
                var wp = new List<float>();
                for (int i = 0; i < TP.Length / 2; i++) if (TP[i * 2] <= 0.03f) { wp.Add(TP[i * 2]); wp.Add(TP[i * 2 + 1] * 1.035f); }
                wp.Add(0.085f); wp.Add(TP[7] * 1.03f - 0.004f);
                PP(p.spine, CurveG(wp.ToArray(), 28, 0, Mathf.PI * 2, "waist" + tk), bot, 0, 0, 0, 0, 0, 0, b, 1, tz * 1.02f);
                if (botKind != "skirt" && crand() < 0.6f) PP(p.spine, TorG(0.15f, 0.01f), 0x2a1f18, 0, 0.075f, 0, Mathf.PI / 2, 0, 0, b * (female ? 1.0f : 0.985f), 1, tz * 1.03f);
            }
            if (botKind == "dress" || botKind == "skirt")
                PP(p.hips, CurveG(new[] { 0.05f, 0.15f, -0.05f, 0.17f, -0.2f, 0.21f, -0.36f, 0.25f, -0.42f, 0.262f }, 28, 0, Mathf.PI * 2, "skirt" + b), botKind == "dress" ? top : bot, 0, 0, 0, 0, 0, 0, b, 1, 0.8f);
            if (jacket >= 0)
            {
                var jp = new float[TP.Length]; for (int i = 0; i < TP.Length; i += 2) { jp[i] = TP[i]; jp[i + 1] = TP[i + 1] * 1.07f; }
                PP(p.spine, CurveG(jp, 28, 0.42f, Mathf.PI * 2 - 0.84f, "jacket" + tk), jacket, 0, -0.01f, 0, 0, 0, 0, b, 1, tz * 1.04f);
                PP(p.spine, TorG(0.085f, 0.022f, Mathf.PI * 1.3f), jacket, 0, 0.5f, -0.01f, Mathf.PI / 2 - 0.25f, 0, Mathf.PI * 0.35f, b, 1, 0.9f);
            }
            if (topKind == "hoodie")
            {
                PP(p.spine, SphereG(0.09f), top, 0, 0.49f, -0.09f, 0, 0, 0, 1.3f * b, 0.7f, 0.8f);
                PP(p.spine, SphereG(0.1f), top, 0, 0.13f, 0.093f * b, -0.12f, 0, 0, 1.15f * b, 0.42f, 0.16f);
            }
            if (!kid && crand() < 0.16f) PP(p.spine, CapG(0.1f, 0.14f), pick(new[] { 0x2c3e50, 0x7a2a2a, 0x2f4f2f, 0x3a3a3a, 0xc08a2a }), 0, 0.29f, -0.17f * tz - 0.02f, 0, 0, 0, 1.05f * b, 1, 0.55f);
            // ---- arms ----
            float shX = (female ? 0.165f : 0.182f) * b, shY = female ? 0.43f : 0.455f;
            foreach (var (sd, L) in new[] { (1f, "L"), (-1f, "R") })
            {
                var sh = Joint(p.spine, shX * sd, shY, 0);
                bool sleeveLong = topKind == "long" || topKind == "hoodie" || topKind == "jacket";
                int armMat = jacket >= 0 ? jacket : top;
                float ua = R3((female ? 0.05f : 0.056f) * b);
                PP(sh, SphereG(R3((ua + 0.004f) / 0.9f)), topKind == "tank" ? skin : armMat, -0.008f * sd, -0.022f, 0, 0, 0, 0, 0.84f, 0.8f, 0.86f); // shoulder cap matched to the arm, no bulge
                PP(sh, LimbG(ua, 0.042f, 0.27f), topKind == "tank" ? skin : (sleeveLong ? armMat : skin));
                if (topKind == "tee" || topKind == "dress") PP(sh, LimbG(R3(ua * 1.2f), 0.054f, 0.13f), top);
                var el = Joint(sh, 0, -0.27f, 0);
                PP(el, LimbG(0.043f, 0.031f, 0.245f), sleeveLong ? armMat : skin);
                if (sleeveLong) PP(el, LimbG(0.045f, 0.041f, 0.035f), armMat, 0, -0.21f, 0);
                var wr = Joint(el, 0, -0.255f, 0);
                PP(wr, SphereG(0.036f), skin, 0, -0.038f, 0, 0, 0, 0, 0.72f, 1.0f, 0.4f);
                PP(wr, LimbG(0.024f, 0.02f, 0.036f), skin, 0, -0.064f, 0.004f, 0, 0, 0, 1, 1, 0.55f);
                PP(wr, CapG(0.0095f, 0.024f), skin, 0.019f * sd * -1, -0.04f, 0.018f, 0.5f, 0, -0.5f * sd);
                if (L == "L") { p.shL = sh; p.elL = el; p.wrL = wr; } else { p.shR = sh; p.elR = el; p.wrR = wr; }
            }
            // ---- neck & head ----
            p.neck = Joint(p.spine, 0, female ? 0.48f : 0.5f, 0);
            PP(p.neck, LimbG(0.042f, 0.05f, 0.09f), skin, 0, 0.085f, 0);
            p.head = Joint(p.neck, 0, 0.075f, 0);
            if (kid) p.head.Scale(1.18f);
            float hc = 0.105f;
            PP(p.head, HeadG(), skin, 0, hc, 0.005f);
            foreach (var sd in new[] { 1f, -1f }) PP(p.head, SphereG(0.022f, 10, 7), skin, 0.091f * sd, hc - 0.002f, -0.004f, 0, 0.3f * sd, 0, 0.42f, 1, 0.72f);
            PP(p.head, SphereG(0.016f, 10, 8), skin, 0, hc - 0.012f, 0.094f, -0.15f, 0, 0, 0.8f, 1.15f, 1.0f); // nose
            int eyeWhite = 0xf4f1ea, irisM = pick(new[] { 0x3a2414, 0x2a4a6a, 0x3a5a2a, 0x1a1a1a, 0x5a3a1a });
            var eg = Joint(p.head, 0, hc + 0.012f, 0.074f); p.eyes.Add(eg);
            foreach (var sd in new[] { 1f, -1f })
            {
                PP(eg, SphereG(0.0145f, 10, 8), eyeWhite, 0.035f * sd, 0, 0, 0, 0, 0, 1, 0.82f, 0.62f);
                PP(eg, SphereG(0.0078f, 8, 6), irisM, 0.035f * sd, 0, 0.0085f, 0, 0, 0, 1, 1, 0.5f);
                PP(p.head, CapG(0.0045f, 0.026f), hair, 0.035f * sd, hc + 0.038f, 0.08f, 0, 0, Mathf.PI / 2 + 0.12f * sd, 1, 1, 0.8f); // brow
            }
            PP(p.head, TorG(0.016f, 0.0034f, 2.1f), 0x8a3a34, 0, hc - 0.034f, 0.08f, 0, 0, -Mathf.PI / 2 - 1.05f);
            p.mouthO = Joint(p.head, 0, hc - 0.05f, 0.079f); p.mouthO.t.localScale = new Vector3(1, 0.1f, 1);
            PP(p.mouthO, SphereG(0.012f, 10, 6), 0x3a1414, 0, 0, 0, 0, 0, 0, 1.2f, 1, 0.4f);
            if (!female && !kid && crand() < 0.25f) PP(p.head, Geo.Sphere(0.1f, 24, 16, 0, Mathf.PI * 2, Mathf.PI * 0.55f, Mathf.PI * 0.45f), hair, 0, hc + 0.004f, 0.012f, 0, 0, 0, 0.9f, 1.1f, 0.92f); // beard
            if (!kid && crand() < 0.18f)
            {
                int gm = pick(new[] { 0x111111, 0x5a3a1a, 0x8a8a90 });
                foreach (var sd in new[] { 1f, -1f }) PP(p.head, TorG(0.018f, 0.0025f), gm, 0.035f * sd, hc + 0.012f, 0.09f);
                PP(p.head, CapG(0.002f, 0.016f), gm, 0, hc + 0.014f, 0.093f, 0, 0, Mathf.PI / 2);
            }
            // ---- hair / hats ----
            string style = opt.hair ?? (kid ? pick(new[] { "short", "pony", "curly", "short" }) : female ? pick(new[] { "long", "pony", "bun", "long", "short", "curly" }) : pick(new[] { "short", "short", "buzz", "bald", "curly", "short" }));
            string hat = !kid && crand() < 0.16f ? pick(new[] { "cap", "beanie" }) : (kid && crand() < 0.2f ? "cap" : null);
            if (style != "bald" || hat != null)
            {
                float r = style == "buzz" ? 0.1015f : 0.106f;
                if (style != "bald") PP(p.head, DomeG(r, Mathf.PI * 0.5f), hair, 0, hc + 0.008f, -0.008f, -0.32f, 0, 0, 0.95f, 1.2f, 1.07f);
            }
            if (style == "long") PP(p.head, CurveG(new[] { 0.05f, 0.1f, 0.0f, 0.106f, -0.08f, 0.11f, -0.17f, 0.115f, -0.24f, 0.1f }, 24, Mathf.PI * 0.55f, Mathf.PI * 0.9f, "longhair"), hair, 0, hc, -0.004f, 0, 0, 0, 0.95f, 1, 0.95f);
            if (style == "pony")
            {
                PP(p.head, SphereG(0.022f), hair, 0, hc + 0.03f, -0.1f);
                p.pony = Joint(p.head, 0, hc + 0.03f, -0.108f); p.ponyV = 0; p.ponyA = 0.35f;
                PP(p.pony, LimbG(0.028f, 0.012f, 0.16f), hair);
            }
            if (style == "bun") PP(p.head, SphereG(0.042f), hair, 0, hc + 0.095f, -0.055f);
            if (style == "curly") for (int i = 0; i < 9; i++)
                {
                    float a = i / 9f * Mathf.PI * 2, rr = 0.08f;
                    PP(p.head, SphereG(Mathf.Round(cr(0.032f, 0.042f) * 200) / 200, 10, 7), hair, Mathf.Cos(a) * rr * 0.95f, hc + 0.06f + Mathf.Sin(i * 1.7f) * 0.02f, Mathf.Sin(a) * rr - 0.015f);
                }
            if (hat == "cap")
            {
                int hm = pick(new[] { 0xb8322a, 0x2a5aa8, 0x1e7a46, 0x151515, 0xe8e2d4 });
                PP(p.head, DomeG(0.11f, Mathf.PI * 0.48f), hm, 0, hc + 0.012f, -0.004f, -0.12f, 0, 0, 0.96f, 1.08f, 1.04f);
                PP(p.head, CylG(0.07f, 0.07f, 0.006f, 24), hm, 0, hc + 0.035f, 0.1f, 0.18f, 0, 0, 1, 1, 1.25f);
            }
            else if (hat == "beanie")
            {
                int hm = pick(new[] { 0x7b241c, 0x1a4a76, 0x5a3478, 0x2a2a2a, 0xd8a030 });
                PP(p.head, DomeG(0.112f, Mathf.PI * 0.52f), hm, 0, hc + 0.016f, -0.004f, -0.15f, 0, 0, 0.97f, 1.2f, 1.05f);
                PP(p.head, TorG(0.098f, 0.013f), hm, 0, hc + 0.012f, -0.002f, Mathf.PI / 2 - 0.15f, 0, 0, 0.97f, 1.05f, 1);
            }
            // ---- props (kept as separate objects: they are animated or emissive) ----
            if (opt.prop == "phone")
            {
                p.phone = Part(p.wrR, BoxG(0.038f, 0.075f, 0.008f), CM(0x111114, "plain", 0.3f), 0, -0.07f, 0.03f, -0.2f, 0, 0);
                Part(p.phone.transform, BoxG(0.032f, 0.065f, 0.002f), CM(0x9ad0ff, "plain", -1, -1, false, 0x6aa8ff, 1.6f), 0, 0, 0.005f);
            }
            if (opt.prop == "flag")
            {
                var fg = Joint(p.wrR, 0, -0.07f, 0.02f); fg.rx = -Mathf.PI / 2 + 0.25f;
                Part(fg, CylG(0.006f, 0.006f, 0.6f, 8), CM(0xd8c8a0, "plain"), 0, 0.22f, 0);
                var geo = GeoX.PlaneMB(0.34f, 0.22f, 12, 6); geo.Translate(new Vector3(0.17f, 0, 0));
                p.flag = new DynMesh(geo, true);
                Part(fg, p.flag.mesh, CM(pick(FLAGS), "cloth"), 0.006f, 0.4f, 0);
            }
            p.cur = CrowdPose.Rest();
            p.root.ry = opt.yaw ?? Mathf.PI + cr(-0.35f, 0.35f);
            p.yaw = p.root.ry;
            SkinRig(p);
            return p;
        }
        // collapse a person's parts into one skinned mesh bound to its joints
        static Texture2D paletteTex; static readonly Dictionary<int, int> paletteIdx = new Dictionary<int, int>(); static Material crowdMat;
        static Vector2 PaletteUV(int color)
        {
            if (!paletteIdx.TryGetValue(color, out int i)) { i = paletteIdx.Count; paletteIdx[color] = i; paletteTex.SetPixel(i % 64, i / 64, Mats.Hex(color)); }
            return new Vector2((i % 64 + 0.5f) / 64, (i / 64 + 0.5f) / 64);
        }
        void SkinRig(Person p)
        {
            if (!crowdMat)
            {
                paletteTex = new Texture2D(64, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                crowdMat = G(Mats.Std(0xffffff, 0.78f, 0, 0, paletteTex));
            }
            var root = p.root.t; var bones = new List<Transform>();
            void Collect(Transform t) { bones.Add(t); foreach (Transform c in t) if (!c.GetComponent<MeshFilter>() && !c.GetComponent<Renderer>()) Collect(c); }
            Collect(root);
            var skinT = new GameObject("skin").transform; skinT.SetParent(root, false); skinT.gameObject.layer = CINE_LAYER;
            var toSkin = skinT.worldToLocalMatrix;
            var vs = new List<Vector3>(); var ns = new List<Vector3>(); var uvs = new List<Vector2>(); var bw = new List<BoneWeight>(); var tris = new List<int>();
            foreach (var pt in p.parts)
            {
                var m = pt.mesh; var M = toSkin * pt.joint.t.localToWorldMatrix * pt.local; var NM = M.inverse.transpose;
                var mv = m.vertices; var mn = m.normals; var mt = m.triangles; int bi = bones.IndexOf(pt.joint.t); var uv = PaletteUV(pt.color);
                bool flip = M.determinant < 0;
                for (int pass = 0; pass < 2; pass++) // front, then a mirrored back copy (the crowd material is double sided)
                {
                    int o = vs.Count;
                    for (int i = 0; i < mv.Length; i++) { vs.Add(M.MultiplyPoint3x4(mv[i])); var nn = NM.MultiplyVector(mn[i]).normalized; ns.Add(pass == 0 ? nn : -nn); uvs.Add(uv); bw.Add(new BoneWeight { boneIndex0 = bi, weight0 = 1 }); }
                    bool rev = (pass == 1) ^ flip;
                    for (int i = 0; i < mt.Length; i += 3) { tris.Add(o + mt[i]); tris.Add(o + (rev ? mt[i + 2] : mt[i + 1])); tris.Add(o + (rev ? mt[i + 1] : mt[i + 2])); }
                }
            }
            paletteTex.Apply(false);
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vs); mesh.SetNormals(ns); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0); mesh.boneWeights = bw.ToArray();
            var bp = new Matrix4x4[bones.Count]; for (int i = 0; i < bones.Count; i++) bp[i] = bones[i].worldToLocalMatrix * skinT.localToWorldMatrix;
            mesh.bindposes = bp; mesh.RecalculateBounds();
            var smr = skinT.gameObject.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh; smr.bones = bones.ToArray(); smr.rootBone = root; smr.sharedMaterial = crowdMat; smr.quality = SkinQuality.Bone1;
            smr.localBounds = new Bounds(new Vector3(0, 1, 0) / Mathf.Max(0.01f, p.s), Vector3.one * 3 / Mathf.Max(0.01f, p.s));
            smr.shadowCastingMode = ShadowCastingMode.On; smr.receiveShadows = true; smr.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            p.parts.Clear();
        }
        void SetBehavior(Person p, string name) { if (p.beh == name) return; p.prevBeh = p.beh; p.beh = name; p.blend = 0; }
        static readonly float[] tmpPrev = new float[CrowdPose.N], tmpTgt = new float[CrowdPose.N];
        void AnimatePerson(Person p, float t, float dt, Vector3? lookWorld)
        {
            // behaviour scheduling (people change what they are doing every few seconds)
            if (p.walk == null || p.walk.done)
            {
                p.behT -= dt;
                if (p.behT <= 0)
                {
                    p.behT = cr(3, 6.5f);
                    var choices = p.prop == "phone" ? new[] { "phone", "phone", "cheer", "point" } : p.prop == "flag" ? new[] { "flag", "flag", "cheer" } :
                        p.kid ? new[] { "jump", "cheer", "jump", "wave" } : new[] { "cheer", "cheer", "clap", "wave", "jump", "idle", "point" };
                    SetBehavior(p, pick(choices));
                }
            }
            p.blend = Mathf.Min(1, p.blend + dt / 0.7f);
            var tgt = tmpTgt; CrowdPose.Rest(tgt);
            CrowdPose.Beh(p.beh, p.clasp, p.tempo, p.ph, p.kid, t, tgt);
            if (p.blend < 1 && p.prevBeh != null)
            {
                var prev = tmpPrev; CrowdPose.Rest(prev); CrowdPose.Beh(p.prevBeh, p.clasp, p.tempo, p.ph, p.kid, t, prev);
                float k = ease(p.blend); for (int i = 0; i < CrowdPose.N; i++) tgt[i] = prev[i] + (tgt[i] - prev[i]) * k;
                // arms fold at the elbow while they swing between poses (no stiff T-pose on the way)
                float mid = 4 * k * (1 - k); tgt[CrowdPose.eLx] -= 0.75f * mid; tgt[CrowdPose.eRx] -= 0.75f * mid;
            }
            // idle layer: breathing, weight shift with contrapposto, micro sway
            float br = Mathf.Sin(t * 1.55f + p.ph), ws = wander(t * 0.17f, p.ph);
            tgt[CrowdPose.spx] += 0.012f * br; tgt[CrowdPose.hx] += 0.022f * ws; tgt[CrowdPose.hpz] -= 0.035f * ws; tgt[CrowdPose.spz] += 0.03f * ws;
            tgt[CrowdPose.kLx] += 0.09f * Mathf.Max(0, -ws); tgt[CrowdPose.kRx] += 0.09f * Mathf.Max(0, ws);
            tgt[CrowdPose.sLz] += 0.02f * br; tgt[CrowdPose.sRz] -= 0.02f * br;
            if (p.walk != null && p.walk.amt > 0.001f) CrowdPose.Gait(tgt, p.walk.phase, p.walk.amt, p.walk.run);
            // eye-led look-at: the target is converted into the body's frame and shared between neck and head
            if (lookWorld.HasValue)
            {
                var v = p.root.t.InverseTransformPoint(lookWorld.Value); v.y -= 1.55f;
                // pitch kept modest: looking up at the fireworks tilts the head back a little, never folds the neck
                float yaw = clamp(Mathf.Atan2(v.x, v.z), -1.1f, 1.1f), pit = clamp(-Mathf.Atan2(v.y, Hypot(v.x, v.z)), -0.42f, 0.32f);
                p.lookY += (yaw - p.lookY) * damp(3.2f, dt); p.lookP += (pit - p.lookP) * damp(3.2f, dt);
                tgt[CrowdPose.nky] += 0.38f * p.lookY; tgt[CrowdPose.hdy] += 0.55f * p.lookY; tgt[CrowdPose.spy] += 0.12f * p.lookY;
                tgt[CrowdPose.nkx] += 0.3f * p.lookP; tgt[CrowdPose.hdx] += 0.6f * p.lookP;
            }
            // springs toward the target
            var c = p.cur; float kf = damp(16, dt), ks = damp(9, dt);
            for (int i = 0; i < CrowdPose.N; i++) c[i] += (tgt[i] - c[i]) * (CrowdPose.Slow(i) ? ks : kf);
            p.hips.pos = new Vector3(c[CrowdPose.hx], p.hipBase + c[CrowdPose.hy], 0); p.hips.SetRot(c[CrowdPose.hpx], c[CrowdPose.hpy], c[CrowdPose.hpz]);
            p.root.py = c[CrowdPose.ry];
            p.spine.SetRot(c[CrowdPose.spx], c[CrowdPose.spy], c[CrowdPose.spz]);
            p.torso.t.localScale = new Vector3(1, 1 + 0.01f * br, 1);
            p.neck.SetRot(c[CrowdPose.nkx], c[CrowdPose.nky], 0); p.head.SetRot(c[CrowdPose.hdx], c[CrowdPose.hdy], c[CrowdPose.hdz]);
            p.shL.SetRot(c[CrowdPose.sLx], c[CrowdPose.sLy], c[CrowdPose.sLz]); p.elL.SetRot(c[CrowdPose.eLx], 0, c[CrowdPose.eLz]); p.wrL.rx = c[CrowdPose.wLx];
            p.shR.SetRot(c[CrowdPose.sRx], c[CrowdPose.sRy], c[CrowdPose.sRz]); p.elR.SetRot(c[CrowdPose.eRx], 0, c[CrowdPose.eRz]); p.wrR.rx = c[CrowdPose.wRx];
            p.thL.SetRot(c[CrowdPose.tLx], 0, c[CrowdPose.tLz]); p.knL.rx = c[CrowdPose.kLx]; p.ftL.rx = c[CrowdPose.fLx];
            p.thR.SetRot(c[CrowdPose.tRx], 0, c[CrowdPose.tRz]); p.knR.rx = c[CrowdPose.kRx]; p.ftR.rx = c[CrowdPose.fRx];
            var ms = p.mouthO.t.localScale; ms.y = 0.1f + 0.9f * Mathf.Clamp01(c[CrowdPose.mouth]); p.mouthO.t.localScale = ms;
            // blinking
            p.blinkT -= dt; float bl = p.blinkT < 0.12f && p.blinkT > 0 ? 0.12f : 1;
            if (p.blinkT <= 0) p.blinkT = cr(2.2f, 5.5f);
            foreach (var e in p.eyes) { var s = e.t.localScale; s.y = bl; e.t.localScale = s; }
            // secondary motion: ponytail is a damped spring driven by the head's vertical motion
            if (p.pony != null)
            {
                float target = 0.35f - (c[CrowdPose.hy] + c[CrowdPose.ry]) * 6 + 0.25f * c[CrowdPose.hdx];
                p.ponyV += ((target - p.ponyA) * 90 - p.ponyV * 9) * dt; p.ponyA += p.ponyV * dt;
                p.pony.rx = p.ponyA;
            }
            if (p.flag != null)
            {
                var mb = p.flag.mb; var bp = p.flag.basePos;
                for (int i = 0; i < bp.Length; i++)
                {
                    float x = bp[i].x, y = bp[i].y, k = x / 0.34f;
                    mb.v[i] = new Vector3(x, y, Mathf.Sin(x * 16 - t * 9 + y * 4) * 0.035f * k + Mathf.Sin(x * 7 - t * 4.3f) * 0.02f * k);
                }
                p.flag.Apply();
            }
        }
        // moves a walker along its waypoints with smooth acceleration, turning and arrival
        void UpdateWalker(Person p, float dt, float faceYaw)
        {
            var w = p.walk; if (w == null || w.done) return;
            if (w.delay > 0) { w.delay -= dt; return; }
            var target = w.path[w.i]; var pos = p.root.pos;
            float dx = target.x - pos.x, dz = target.z - pos.z, dist = Hypot(dx, dz);
            bool last = w.i == w.path.Count - 1 && !w.loop;
            float remain = dist; for (int k = w.i + 1; k < w.path.Count; k++) remain += Vector3.Distance(w.path[k], w.path[k - 1]);
            float want = last || !w.loop ? w.max * clamp(remain / (w.run ? 3.5f : 2.2f), 0, 1) : w.max;
            w.speed += (want - w.speed) * damp(w.run ? 2.2f : 1.8f, dt);
            float desired = Mathf.Atan2(dx, dz);
            p.yaw += shortAng(desired - p.yaw) * damp(3.5f, dt);
            p.root.ry = p.yaw;
            pos.x += Mathf.Sin(p.yaw) * w.speed * dt; pos.z += Mathf.Cos(p.yaw) * w.speed * dt; p.root.pos = pos;
            w.phase += w.speed * dt / (w.run ? 2.3f : 1.45f) * Mathf.PI * 2;
            w.amt = clamp(w.speed / (w.run ? 2.6f : 1.25f), 0, 1);
            if (dist < (w.run ? 1.2f : 0.7f))
            {
                if (w.i < w.path.Count - 1) w.i++;
                else if (w.loop) w.i = 0;
                else if (w.speed < 0.08f) { w.done = true; w.amt = 0; SetBehavior(p, w.then ?? "cheer"); p.behT = cr(3, 5); }
            }
            if (w.done) return;
            if (last && dist < 1.5f) { p.yaw += shortAng(faceYaw - p.yaw) * damp(2.5f, dt); p.root.ry = p.yaw; }
        }

        // =====================================================================================================
        // The hero: armoured long-coat hunter, glowing visor, red scarf (front faces local +z)
        // =====================================================================================================
        class HeroRig
        {
            public N H, hips, spine, neck, head, thL, knL, ftL, thR, knR, ftR, shL, elL, wrL, shR, elR, wrR, gun; public Transform torso;
            public DynMesh skirt; public GameObject visorGlow; public Material visorGlowMat;
            public List<(DynMesh g, float len, float w, float off)> scarves = new List<(DynMesh, float, float, float)>();
            public List<(DynMesh g, float len, float w, float off, float x)> tails = new List<(DynMesh, float, float, float, float)>(); // torn coat tails
            public List<(DynMesh g, float len, float w, float off, float x)> cape = new List<(DynMesh, float, float, float, float)>();  // tattered cape strips
            public Material coreMat, runeMat, auraMat; public GameObject aura;
            public float lookY, lookP, sp;
        }
        static Geo.MB RibbonMB(int segs)
        {
            var b = new Geo.MB();
            for (int i = 0; i <= segs; i++)
            {
                b.v.Add(Vector3.zero); b.v.Add(Vector3.zero); b.n.Add(Vector3.up); b.n.Add(Vector3.up);
                b.uv.Add(new Vector2(0, (float)i / segs)); b.uv.Add(new Vector2(1, (float)i / segs));
                if (i < segs) { int a = i * 2; b.Tri(a, a + 1, a + 2); b.Tri(a + 1, a + 3, a + 2); }
            }
            return b;
        }
        // The hero: an armoured demon-hunter. Layered spiked pauldrons, a plated chest with a burning core, a helmet with a
        // glowing T-visor and swept-back crest, a greatsword across the back, a long torn coat and a red scarf in the wind.
        // unlit HDR colour: the lit template ships without the _EMISSION variant, so emission alone never showed
        Material Glowing(int col, float intensity) { var m = G(Mats.Basic(0xffffff)); SetGlow(m, col, intensity); return m; }
        // the hero's left arm carries the elbow chainsaw from the game, deployed: an armoured motor housing on the outside
        // of the elbow (fins, a glowing intake fan, exhaust stacks, the hinge) and the bar locked along the forearm, running
        // past the fist with raked teeth round it and a glowing core slot (the forearm hangs along -y from the elbow)
        void HeroChainsaw(N el, Material plate, Material plate2, Material steel)
        {
            var orange = Glowing(0xff7020, 3.2f); var dark = CM(0x0e0c10, "metal", 0.3f); var toothM = CM(0xb4bac4, "metal", 0.22f); var bar = CM(0x8c929c, "metal", 0.2f);
            float PI = Mathf.PI, hx = 0.078f;
            Part(el, BoxG(0.06f, 0.15f, 0.11f), dark, hx, -0.03f, 0);                                   // motor housing at the elbow
            Part(el, BoxG(0.066f, 0.025f, 0.1f), plate2, hx, 0.055f, 0); Part(el, BoxG(0.067f, 0.005f, 0.085f), orange, hx, 0.069f, 0);
            for (int k = 0; k < 4; k++) Part(el, BoxG(0.006f, 0.022f, 0.1f), plate, hx + 0.033f, 0.02f - k * 0.03f, 0);  // cooling fins
            Part(el, CylG(0.038f, 0.038f, 0.01f, 18), plate, hx + 0.034f, -0.07f, 0, 0, 0, PI / 2);    // intake ring
            Part(el, CylG(0.028f, 0.028f, 0.006f, 16), orange, hx + 0.036f, -0.07f, 0, 0, 0, PI / 2);  // hot intake
            for (int k = 0; k < 5; k++) Part(el, BoxG(0.004f, 0.05f, 0.012f), dark, hx + 0.041f, -0.07f, 0, k * PI * 2 / 5, 0, 0); // fan blades
            foreach (var ez in new[] { -0.025f, 0.025f }) Part(el, CylG(0.009f, 0.011f, 0.08f, 8), steel, hx - 0.01f, 0.09f, ez - 0.03f, -0.5f, 0, 0); // exhausts
            Part(el, TorG(0.026f, 0.008f), steel, hx + 0.012f, -0.1f, 0, 0, PI / 2, 0);                // hinge collar
            // the bar, locked down the forearm past the fist
            float x = hx + 0.012f, top = -0.11f, nose = -0.72f, L = top - nose, mid = (top + nose) / 2, hb = 0.04f;
            Part(el, BoxG(0.014f, L, hb * 2), bar, x, mid, 0);
            foreach (var sz in new[] { -1f, 1f }) Part(el, BoxG(0.017f, L * 0.94f, 0.008f), plate2, x, mid, sz * (hb - 0.01f)); // rails
            Part(el, BoxG(0.016f, L * 0.6f, 0.012f), orange, x, mid - 0.03f, 0);                       // glowing core slot
            for (int k = 0; k < 4; k++) Part(el, CylG(0.008f, 0.008f, 0.018f, 8), dark, x, -0.22f - k * 0.11f, 0.018f * (k % 2 == 0 ? 1 : -1), 0, 0, PI / 2); // cut-outs
            Part(el, CylG(hb, hb, 0.014f, 18), bar, x, nose, 0, 0, 0, PI / 2);                          // nose
            for (int k = 0; k < 17; k++) foreach (var sz in new[] { -1f, 1f })
            {
                float y = top - 0.01f - k * (L - 0.02f) / 16;
                Part(el, BoxG(0.018f, 0.016f, 0.014f), toothM, x, y, sz * (hb + 0.006f));
                Part(el, Geo.Cone(0.008f, 0.024f, 4), toothM, x, y - 0.006f, sz * (hb + 0.018f), sz * (PI / 2 + 0.5f), 0, 0); // raked cutter
            }
            var g = CGlow(el.t, 0xff6a20, 0.12f, 0.5f); g.transform.localPosition = V(x + 0.02f, -0.07f, 0);
        }
        static void SetGlow(Material m, int col, float intensity) { var c = Mats.Hex(col) * (0.45f * intensity); c.a = 1; m.SetColor("_BaseColor", c); }
        HeroRig BuildHero(Transform parent)
        {
            kitHi = true;
            var r = new HeroRig();
            r.H = Joint(parent); r.H.Scale(1.1f);
            float PI = Mathf.PI;
            Material coatM = CM(0x221f28, "cloth"), coat2 = CM(0x332d3c, "cloth"), pantsM = CM(0x18181e, "cloth"), inner = CM(0x2a2528, "cloth");
            Material bootM = CM(0x0e0e10, "leather"), leather = CM(0x3e2416, "leather"), glove = CM(0x141012, "leather");
            Material plate = CM(0x2a2d34, "metal", 0.3f), plate2 = CM(0x3c414c, "metal", 0.26f), steel = CM(0xb0b6c2, "metal", 0.2f);
            Material gold = CM(0xd4a83a, "metal", 0.22f), red = CM(0x9e0f16, "cloth", -1, -1, true), wood = CM(0x4a2614, "leather");
            var rune = Glowing(0xff2a14, 3.2f); r.runeMat = rune;
            var core = Glowing(0xff3a1a, 5); r.coreMat = core;
            var visorM = Glowing(0xff2a14, 4.5f);
            r.hips = Joint(r.H, 0, 0.95f, 0);
            // ---- legs: armoured greaves, spiked knees, heavy boots ----
            foreach (var (sd, L) in new[] { (1f, "L"), (-1f, "R") })
            {
                var th = Joint(r.hips, 0.098f * sd, -0.04f, 0);
                Part(th, LimbG(0.084f, 0.06f, 0.44f), pantsM);
                Part(th, SphereG(0.07f, 20, 14), plate, 0.01f * sd, -0.17f, 0.04f, 0, 0, 0, 1.0f, 1.9f, 0.62f);        // thigh plate
                var kn = Joint(th, 0, -0.44f, 0);
                Part(kn, LimbG(0.06f, 0.045f, 0.42f), pantsM);
                Part(kn, SphereG(0.058f, 20, 14), plate2, 0, -0.01f, 0.05f, 0, 0, 0, 1, 1.15f, 0.6f);                   // knee cop
                Part(kn, Geo.Cone(0.02f, 0.08f, 6), steel, 0, 0.0f, 0.1f, PI / 2 - 0.3f, 0, 0);                          // knee spike
                Part(kn, LimbG(0.07f, 0.064f, 0.3f), plate, 0, -0.1f, 0.004f);                                            // greave
                Part(kn, BoxG(0.008f, 0.22f, 0.006f), rune, 0, -0.24f, 0.07f);                                           // glowing seam
                Part(kn, TorG(0.068f, 0.009f), gold, 0, -0.1f, 0.004f, PI / 2);
                var ft = Joint(kn, 0, -0.43f, 0);
                Part(ft, SphereG(1, 22, 16), bootM, 0, -0.03f, 0.05f, 0, 0, 0, 0.064f, 0.054f, 0.145f);
                Part(ft, SphereG(1, 22, 16), plate2, 0, -0.012f, 0.1f, 0, 0, 0, 0.052f, 0.032f, 0.07f);                // steel toe cap
                Part(ft, SphereG(1, 22, 16), CM(0x070707, "plain"), 0, -0.07f, 0.05f, 0, 0, 0, 0.066f, 0.016f, 0.15f);
                if (L == "L") { r.thL = th; r.knL = kn; r.ftL = ft; } else { r.thR = th; r.knR = kn; r.ftR = ft; }
            }
            // ---- torso: plated chest with a burning core ----
            r.spine = Joint(r.hips, 0, 0.02f, 0);
            r.torso = Part(r.spine, TorsoG("m"), inner, 0, 0, 0, 0, 0, 0, 1.1f, 1, 0.72f).transform;
            var coatD = CM(0x221f28, "cloth", -1, -1, true);
            var shellP = new float[TORSO_M.Length]; for (int i = 0; i < TORSO_M.Length; i += 2) { shellP[i] = TORSO_M[i]; shellP[i + 1] = TORSO_M[i + 1] * 1.12f; }
            Part(r.spine, CurveG(shellP, 32, 0.62f, Mathf.PI * 2 - 1.24f, "herocoat2"), coatD, 0, -0.01f, 0, 0, 0, 0, 1.1f, 1, 0.74f);
            Part(r.spine, SphereG(0.17f, 28, 20), plate, 0, 0.35f, 0.055f, 0, 0, 0, 1.18f, 1.05f, 0.56f);                 // breastplate
            Part(r.spine, SphereG(0.15f, 28, 20), plate2, 0, 0.37f, 0.07f, 0, 0, 0, 1.0f, 0.82f, 0.5f);
            for (int k = 0; k < 3; k++) Part(r.spine, BoxG(0.21f - k * 0.015f, 0.045f, 0.03f), plate, 0, 0.21f - k * 0.05f, 0.105f - k * 0.004f, 0.08f, 0, 0); // ab plates
            // burning core set into the breastplate (a flattened lens inside a flush gold bezel, not a ball on top)
            Part(r.spine, SphereG(0.042f, 20, 14), CM(0x0a0606, "metal", 0.4f), 0, 0.36f, 0.139f, 0, 0, 0, 1, 1, 0.3f); // dark socket
            Part(r.spine, SphereG(0.031f, 20, 14), core, 0, 0.36f, 0.15f, 0, 0, 0, 1, 1, 0.55f);                       // burning core
            Part(r.spine, TorG(0.036f, 0.006f), gold, 0, 0.36f, 0.152f, 0, 0, 0, 1, 1, 0.6f);
            var coreGlow = CGlow(r.spine.t, 0xff4020, 0.12f, 0.42f); coreGlow.transform.localPosition = V(0, 0.36f, 0.165f);
            foreach (var sd in new[] { 1f, -1f })
            {
                Part(r.spine, BoxG(0.006f, 0.13f, 0.006f), rune, 0.07f * sd, 0.36f, 0.135f, 0, 0, 0.55f * sd);                 // rune lines
                Part(r.spine, BoxG(0.006f, 0.08f, 0.006f), rune, 0.12f * sd, 0.27f, 0.115f, 0, 0, -0.3f * sd);
                Part(r.spine, BoxG(0.075f, 0.32f, 0.012f), coat2, 0.085f * sd, 0.36f, 0.13f, -0.1f, 0.3f * sd, 0.16f * sd);    // coat lapels
            }
            // gorget: a high armoured collar
            Part(r.spine, TorG(0.1f, 0.04f), plate2, 0, 0.5f, 0, PI / 2 - 0.15f, 0, 0, 1.12f, 1.05f, 1.4f);
            Part(r.spine, BoxG(0.13f, 0.08f, 0.03f), plate, 0, 0.53f, 0.08f, -0.35f, 0, 0);
            // belt with a glowing buckle, hanging tassets
            Part(r.spine, TorG(0.165f, 0.02f), leather, 0, 0.07f, 0, PI / 2, 0, 0, 1.1f, 1, 0.76f);
            Part(r.spine, BoxG(0.06f, 0.05f, 0.016f), gold, 0, 0.07f, 0.13f); Part(r.spine, BoxG(0.03f, 0.022f, 0.01f), rune, 0, 0.07f, 0.14f);
            foreach (var (ang, w) in new[] { (0.35f, 0.1f), (-0.35f, 0.1f), (1.25f, 0.11f), (-1.25f, 0.11f) })
            {
                var tj = Joint(r.spine, Mathf.Sin(ang) * 0.17f, 0.05f, Mathf.Cos(ang) * 0.12f); tj.SetRot(-0.12f, ang, 0);
                Part(tj, BoxG(w, 0.17f, 0.014f), plate, 0, -0.08f, 0.01f);
                Part(tj, BoxG(w, 0.012f, 0.016f), rune, 0, -0.165f, 0.012f);
                Part(tj, BoxG(w * 0.9f, 0.12f, 0.012f), plate2, 0, -0.17f, 0.002f, -0.08f, 0, 0);
            }
            Part(r.spine, BoxG(0.08f, 0.2f, 0.08f), leather, -0.215f, -0.04f, 0.03f, 0, 0, 0.1f);                          // holster
            // long coat skirt, open at the front; animated by the wind every frame
            var skirtMB = GeoX.LatheMB(new[] { new Vector2(0.172f, 0.06f), new Vector2(0.195f, -0.08f), new Vector2(0.232f, -0.26f), new Vector2(0.272f, -0.48f), new Vector2(0.315f, -0.7f), new Vector2(0.33f, -0.78f) }, 44, 0.45f, Mathf.PI * 2 - 0.9f);
            r.skirt = new DynMesh(skirtMB, true);
            Part(r.hips, r.skirt.mesh, CM(0x221f28, "cloth"), 0, 0, 0, 0, 0, 0, 1.1f, 1, 0.84f);
            // ---- arms: layered spiked pauldrons, plated gauntlets ----
            foreach (var (sd, L) in new[] { (1f, "L"), (-1f, "R") })
            {
                var sh = Joint(r.spine, 0.205f * sd, 0.455f, 0);
                Part(sh, SphereG(0.07f), coatM, 0, -0.01f, 0);
                Part(sh, LimbG(0.06f, 0.05f, 0.28f), coatM);
                Part(sh, LimbG(0.064f, 0.056f, 0.12f), plate, 0, -0.12f, 0);                                              // rerebrace
                // pauldron: three layered plates, red trim, two spikes
                for (int k = 0; k < 3; k++)
                    Part(sh, SphereG(0.11f - k * 0.012f, 24, 16), k == 0 ? plate2 : plate, 0.028f * sd, 0.045f - k * 0.045f, 0, 0, 0, -0.32f * sd, 1.2f, 0.62f, 1.2f);
                Part(sh, TorG(0.104f, 0.009f), rune, 0.028f * sd, 0.03f, 0, PI / 2, 0, -0.32f * sd, 1.18f, 1.18f, 1);
                Part(sh, Geo.Cone(0.022f, 0.12f, 6), steel, 0.05f * sd, 0.1f, -0.02f, -0.25f, 0, -0.55f * sd);
                Part(sh, Geo.Cone(0.017f, 0.085f, 6), steel, 0.08f * sd, 0.07f, 0.04f, 0.2f, 0, -0.85f * sd);
                var el = Joint(sh, 0, -0.28f, 0);
                Part(el, LimbG(0.052f, 0.045f, 0.25f), coatM);
                Part(el, LimbG(0.06f, 0.053f, 0.17f), plate, 0, -0.06f, 0);                                               // gauntlet cuff
                Part(el, BoxG(0.006f, 0.12f, 0.006f), rune, 0.0f, -0.13f, 0.058f);
                Part(el, Geo.Cone(0.016f, 0.06f, 6), steel, 0.05f * sd, -0.06f, 0, 0, 0, -PI / 2 * sd);                    // elbow fin
                var wr = Joint(el, 0, -0.255f, 0);
                Part(wr, SphereG(0.046f), glove, 0, -0.045f, 0, 0, 0, 0, 0.8f, 1.0f, 0.5f);
                Part(wr, BoxG(0.06f, 0.03f, 0.03f), plate2, 0, -0.05f, 0.022f);                                           // knuckle plate
                Part(wr, LimbG(0.031f, 0.027f, 0.045f), glove, 0, -0.075f, 0.006f, 0, 0, 0, 1, 1, 0.6f);
                if (L == "L") HeroChainsaw(el, plate, plate2, steel);
                if (L == "L") { r.shL = sh; r.elL = el; r.wrL = wr; } else { r.shR = sh; r.elR = el; r.wrR = wr; }
            }
            // the revolver from the game, held low in the right hand
            r.gun = Joint(r.wrR, 0, -0.09f, 0.025f); r.gun.rx = 1.3f;
            var blueGlow = Glowing(0x40c8ff, 3);
            Part(r.gun, BoxG(0.034f, 0.06f, 0.12f), plate, 0, 0, 0.02f);
            Part(r.gun, CylG(0.014f, 0.014f, 0.22f, 14), steel, 0, 0.014f, 0.18f, PI / 2);
            Part(r.gun, BoxG(0.026f, 0.034f, 0.18f), plate, 0, 0.002f, 0.17f);                                            // underlug
            Part(r.gun, CylG(0.03f, 0.03f, 0.055f, 16), steel, 0, 0.004f, 0.045f, PI / 2);
            Part(r.gun, BoxG(0.03f, 0.09f, 0.04f), wood, 0, -0.06f, -0.038f, 0.35f);
            foreach (var sd in new[] { 1f, -1f }) Part(r.gun, BoxG(0.004f, 0.007f, 0.15f), blueGlow, 0.016f * sd, 0.012f, 0.15f);
            // ---- head: helmet with a glowing T-visor, crest fins and horns, under a hood ----
            r.neck = Joint(r.spine, 0, 0.505f, 0);
            Part(r.neck, LimbG(0.048f, 0.056f, 0.09f), pantsM, 0, 0.085f, 0);
            Part(r.neck, TorG(0.076f, 0.036f), red, 0, 0.03f, 0, PI / 2 - 0.1f, 0, 0, 1, 1.15f, 1);                       // scarf wrap
            Part(r.neck, TorG(0.066f, 0.032f), red, 0, 0.075f, 0.005f, PI / 2 + 0.15f, 0, 0, 1, 1.1f, 1);
            r.head = Joint(r.neck, 0, 0.08f, 0);
            Part(r.head, HeadG(), plate, 0, 0.105f, 0.004f, 0, 0, 0, 1.14f, 1.08f, 1.12f);                             // helmet shell
            // face pieces follow the helmet's curve and sit flush with it: arcs wrapped around the front, not boxes stuck on
            Part(r.head, SphereG(0.07f, 24, 16), plate, 0, 0.068f, 0.04f, 0.3f, 0, 0, 1.25f, 0.9f, 0.72f);              // faceplate
            Part(r.head, TorG(0.106f, 0.012f, PI * 0.72f), plate2, 0, 0.145f, 0.006f, PI / 2, 0, PI / 2 - PI * 0.36f, 1, 1, 0.8f); // brow ridge
            Part(r.head, TorG(0.104f, 0.0085f, PI * 0.5f), visorM, 0, 0.118f, 0.006f, PI / 2, 0, PI / 4);                // visor slit
            Part(r.head, BoxG(0.009f, 0.034f, 0.01f), visorM, 0, 0.098f, 0.106f, 0.2f, 0, 0);                             // ...and its T
            Part(r.head, SphereG(0.048f, 20, 14), plate2, 0, 0.032f, 0.062f, -0.2f, 0, 0, 1.2f, 0.75f, 0.75f);            // jaw guard
            for (int k = 0; k < 3; k++) Part(r.head, BoxG(0.05f - k * 0.008f, 0.004f, 0.006f), CM(0x0a0a0c, "plain"), 0, 0.044f - k * 0.012f, 0.096f - k * 0.002f, -0.2f, 0, 0); // breathing grille
            foreach (var sd in new[] { 1f, -1f })
            {
                Part(r.head, SphereG(0.03f, 16, 12), plate, 0.06f * sd, 0.06f, 0.06f, 0, 0.5f * sd, 0, 1, 0.7f, 0.5f);   // cheek guards
                var horn = Joint(r.head, 0.075f * sd, 0.17f, -0.01f); horn.SetRot(-0.9f, 0, -0.35f * sd);                // swept-back horns
                Part(horn, Geo.Cone(0.024f, 0.16f, 8), plate2, 0, 0.07f, 0);
                Part(horn, Geo.Cone(0.012f, 0.1f, 6), gold, 0, 0.18f, -0.02f, -0.4f, 0, 0);
            }
            Part(r.head, Geo.Cone(0.015f, 0.2f, 4), plate2, 0, 0.215f, -0.03f, -1.1f, 0, 0, 0.3f, 1, 1.6f);           // crest fin
            r.visorGlow = CGlow(r.head.t, 0xff3020, 0.16f, 0.1f); r.visorGlow.transform.localPosition = V(0, 0.118f, 0.11f); // soft halo, not a dot
            r.visorGlowMat = r.visorGlow.GetComponent<Renderer>().sharedMaterial;
            Part(r.head, DomeG(0.138f, Mathf.PI * 0.6f), CM(0x2a2532, "cloth", -1, -1, true), 0, 0.112f, -0.03f, -0.62f, 0, 0, 1.02f, 1.06f, 1.08f); // hood
            Part(r.head, SphereG(0.1f, 24, 16), coat2, 0, 0.03f, -0.09f, 0.45f, 0, 0, 1.2f, 0.58f, 0.5f);
            // a faint burning aura behind him
            r.aura = CGlow(r.spine.t, 0xff2a10, 1.6f, 0.18f); r.aura.transform.localPosition = V(0, 0.35f, -0.25f); r.auraMat = r.aura.GetComponent<Renderer>().sharedMaterial;
            // scarf tails and torn coat tails: dynamic ribbons streaming in the wind
            for (int i = 0; i < 2; i++)
            {
                var g = new DynMesh(RibbonMB(i == 0 ? 34 : 26), true);
                Part(r.H, g.mesh, CM(0x9e0f16, "cloth"));
                r.scarves.Add((g, i == 1 ? 0.85f : 1.35f, i == 1 ? 0.13f : 0.18f, i * 1.7f));
            }
            for (int i = 0; i < 3; i++)
            {
                var g = new DynMesh(RibbonMB(18), true);
                Part(r.H, g.mesh, CM(i == 1 ? 0x2a2530 : 0x221f28, "cloth"));
                r.tails.Add((g, 0.55f + i * 0.12f, 0.12f - i * 0.015f, i * 2.3f, (i - 1) * 0.12f));
            }
            // a big tattered cape from the shoulders: overlapping strips of uneven length, so the hem looks torn and worn
            float[] capeLen = { 1.18f, 1.34f, 1.05f, 1.42f, 1.22f, 1.1f, 1.3f };
            for (int i = 0; i < capeLen.Length; i++)
            {
                var g = new DynMesh(RibbonMB(22), true);
                Part(r.H, g.mesh, CM(i % 3 == 1 ? 0x2c1c24 : i % 2 == 0 ? 0x241a22 : 0x1e1820, "cloth"));
                r.cape.Add((g, capeLen[i], 0.1f, i * 1.37f, (i - 3) * 0.05f));
            }
            Part(r.spine, SphereG(0.2f, 20, 14), CM(0x241a22, "cloth"), 0, 0.44f, -0.07f, 0, 0, 0, 1.12f, 0.36f, 0.72f); // mantle joining the cape to the shoulders
            kitHi = false;
            return r;
        }
        void AnimateHero(float t, float dt, Vector3 camPos, float turnAt = 6.2f)
        {
            var r = CS.heroRig;
            float br = Mathf.Sin(t * 1.25f), ws = wander(t * 0.12f, 3.3f);
            // stance: weight on the left leg, relaxed arms, revolver held low; breathing and a slow weight shift
            r.hips.pos = V(0.02f * ws, 0.95f - 0.006f * (1 + br), 0); r.hips.SetRot(0, 0.04f * ws, -0.035f + 0.02f * ws);
            r.thL.SetRot(-0.02f, 0, 0.05f); r.knL.rx = 0.04f; r.ftL.rx = -0.02f;
            r.thR.SetRot(-0.1f, 0, -0.07f); r.knR.rx = 0.2f + 0.04f * ws; r.ftR.rx = -0.1f;
            r.spine.SetRot(0.02f + 0.014f * br, r.spine.ry, 0.03f - 0.02f * ws); r.torso.localScale = V(1.06f, 1 + 0.012f * br, 0.7f);
            r.shL.SetRot(0.04f + 0.02f * br, 0, 0.16f + 0.015f * br); r.elL.SetRot(-0.22f, 0, 0); r.wrL.rx = -0.1f;
            r.shR.SetRot(-0.14f + 0.015f * br, 0, -0.13f - 0.015f * br); r.elR.SetRot(-0.4f, 0, 0); r.wrR.rx = 0.15f;
            // gaze: scanning the horizon, then turning to look back over the shoulder toward the camera
            float ty = 0.14f * wander(t * 0.25f, 7), tp = -0.04f + 0.03f * wander(t * 0.2f, 9);
            if (t > turnAt)
            {
                var p0 = r.H.t.InverseTransformPoint(camPos); p0.y -= 1.62f;
                float k = ease3((t - turnAt) / 1.5f);
                ty += (clamp(Mathf.Atan2(p0.x, p0.z), -1.25f, 1.25f) - ty) * k; tp += (clamp(-Mathf.Atan2(p0.y, Hypot(p0.x, p0.z)), -0.3f, 0.3f) - tp) * k;
            }
            r.lookY += (ty - r.lookY) * damp(2.4f, dt); r.lookP += (tp - r.lookP) * damp(2.4f, dt);
            r.sp += (r.lookY * 0.25f - r.sp) * damp(1.6f, dt);  // the body follows the head a moment later
            r.spine.ry = r.sp; r.neck.SetRot(0.35f * r.lookP, 0.35f * (r.lookY - r.sp), 0); r.head.SetRot(0.65f * r.lookP - 0.05f, 0.65f * (r.lookY - r.sp), 0.02f * ws);
            Mats.SetAlpha(r.visorGlowMat, 0.1f + 0.03f * Mathf.Sin(t * 5));
            // coat skirt in the wind: the back and the hem move most, with travelling ripples
            var sk = r.skirt; float gust = 0.75f + 0.25f * wander(t * 0.6f, 2);
            for (int i = 0; i < sk.basePos.Length; i++)
            {
                var bpos = sk.basePos[i];
                float h = Mathf.Pow(clamp(-bpos.y / 0.7f, 0, 1), 1.6f), phi = Mathf.Atan2(bpos.x, bpos.z), back = 0.55f - 0.45f * Mathf.Cos(phi);
                sk.mb.v[i] = new Vector3(bpos.x + h * 0.05f * Mathf.Sin(t * 2.3f + phi * 3 + bpos.y * 4) * back, bpos.y + h * 0.03f * back * gust, bpos.z - h * back * gust * (0.16f + 0.05f * Mathf.Sin(t * 3.4f + phi * 2 + bpos.y * 6)));
            }
            sk.Apply();
            // scarf: a ribbon whose centreline curls back and sideways in the wind, twisting and tapering
            foreach (var s in r.scarves)
            {
                var g = s.g; int n = g.mb.v.Count / 2 - 1; float seg = s.len / n;
                var p = V(-0.05f + s.off * 0.03f, 1.5f, -0.09f);
                for (int i = 0; i <= n; i++)
                {
                    float u = (float)i / n, ph = t + s.off;
                    float yaw = 0.42f + 0.22f * Mathf.Sin(ph * 2.9f - u * 5.5f) + 0.08f * Mathf.Sin(ph * 6.7f - u * 12) + 0.15f * wander(ph * 0.5f, 4);
                    float pit = -0.2f + 0.18f * Mathf.Sin(ph * 3.8f - u * 6.5f) - 0.3f * (1 - u) * (1 - u);
                    var d = V(Mathf.Sin(yaw) * Mathf.Cos(pit), Mathf.Sin(pit), -Mathf.Cos(yaw) * Mathf.Cos(pit));
                    var w = Vector3.Cross(d, Vector3.up).normalized; var up = Vector3.Cross(w, d);
                    float tw = 0.7f * Mathf.Sin(ph * 2.4f - u * 4.2f), wd = s.w * (1 - 0.4f * u) * 0.5f;
                    w = w * Mathf.Cos(tw) + up * Mathf.Sin(tw);
                    g.mb.v[i * 2] = p - w * wd; g.mb.v[i * 2 + 1] = p + w * wd;
                    p += d * seg;
                }
                g.Apply();
            }
            // torn coat tails: hang from the back of the belt and stream out behind, fluttering harder at the tips
            foreach (var s in r.tails)
            {
                var g = s.g; int n = g.mb.v.Count / 2 - 1; float seg = s.len / n;
                var p = V(s.x, 1.0f, -0.2f);
                for (int i = 0; i <= n; i++)
                {
                    float u = (float)i / n, ph = t * 1.1f + s.off;
                    float yaw = 0.25f + 0.18f * Mathf.Sin(ph * 2.3f - u * 4.5f) + 0.1f * wander(ph * 0.4f, 5);
                    float pit = -1.15f + 0.65f * u + 0.18f * Mathf.Sin(ph * 3.1f - u * 6);   // drops from the belt, then lifts in the wind
                    var d = V(Mathf.Sin(yaw) * Mathf.Cos(pit), Mathf.Sin(pit), -Mathf.Cos(yaw) * Mathf.Cos(pit));
                    var w = Vector3.Cross(d, Vector3.up).normalized; var up = Vector3.Cross(w, d);
                    float tw = 0.5f * Mathf.Sin(ph * 2 - u * 3.5f), wd = s.w * (1 - 0.55f * u) * 0.5f;
                    w = w * Mathf.Cos(tw) + up * Mathf.Sin(tw);
                    g.mb.v[i * 2] = p - w * wd; g.mb.v[i * 2 + 1] = p + w * wd;
                    p += d * seg;
                }
                g.Apply();
            }
            // the cape: falls from the shoulders down the back, then the wind lifts and ripples the torn hem
            float capeGust = 0.7f + 0.3f * wander(t * 0.5f, 3);
            // the cape hangs from the shoulders as the body turns (anchors and heading follow the spine)
            var capeBack = r.H.t.InverseTransformDirection(r.spine.t.TransformDirection(Vector3.back)); float capeYaw = Mathf.Atan2(capeBack.x, -capeBack.z);
            foreach (var s in r.cape)
            {
                var g = s.g; int n = g.mb.v.Count / 2 - 1; float seg = s.len / n;
                var p = r.H.t.InverseTransformPoint(r.spine.t.TransformPoint(V(s.x, 0.44f, -0.19f + Mathf.Abs(s.x) * 0.15f)));
                for (int i = 0; i <= n; i++)
                {
                    float u = (float)i / n, ph = t * 1.05f + s.off;
                    float yaw = capeYaw + 0.04f * u + s.x * 0.15f * u + (0.03f + 0.08f * u) * Mathf.Sin(ph * 2.6f - u * 5) + 0.03f * wander(ph * 0.4f, 6);
                    float pit = -1.42f + 0.5f * u * capeGust + 0.09f * u * Mathf.Sin(ph * 3.3f - u * 7); // hangs down the back, lifting a little at the hem
                    var d = V(Mathf.Sin(yaw) * Mathf.Cos(pit), Mathf.Sin(pit), -Mathf.Cos(yaw) * Mathf.Cos(pit));
                    var w = Vector3.Cross(d, Vector3.up).normalized; var up = Vector3.Cross(w, d);
                    float tw = 0.22f * u * Mathf.Sin(ph * 2.2f - u * 4), wd = s.w * (1 - 0.15f * u) * 0.5f;
                    w = w * Mathf.Cos(tw) + up * Mathf.Sin(tw);
                    g.mb.v[i * 2] = p - w * wd; g.mb.v[i * 2 + 1] = p + w * wd;
                    p += d * seg;
                }
                g.Apply();
            }
            // the core, runes and aura breathe with a slow heartbeat
            float beat = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(t * 2.4f), 3);
            SetGlow(r.coreMat, 0xff3a1a, 3.5f + 3f * beat); SetGlow(r.runeMat, 0xff2a14, 2.4f + 1.6f * beat);
            Mats.SetAlpha(r.auraMat, 0.12f + 0.1f * beat);
        }

        // =====================================================================================================
        // Environments: sets are built in small tasks while the first two in-arena shots play
        // =====================================================================================================
        class CineScene
        {
            public Transform root; public N world, city, hero; public List<Action> tasks = new List<Action>();
            // world
            public N planet, clouds; public Transform wSun; public Material planetMat, cloudMat, atmoMat, hazeMat;
            // city
            public Light citySun; public ReflectionProbe cityProbe; public List<Person> people = new List<Person>();
            public List<(N crown, float ph)> trees = new List<(N, float)>(); public List<(N g, float ph)> bunting = new List<(N, float)>(); public List<Material> blinkers = new List<Material>();
            public List<Bird> cityBirds, heroBirds; public float fwT; public List<Firework> fwList = new List<Firework>(); public Vector3 lastBurstPos; public float lastBurstT = -1; public int frameN;
            public Confetti confetti; public SphericalHarmonicsL2 cityAmb, heroAmb; public bool cityEnvDone, heroEnvDone;
            public Material citySky, heroSky; public SkyParams citySkyP, heroSkyP;
            // hero
            public Light heroSun; public ReflectionProbe heroProbe; public HeroRig heroRig; public Vector3 heroPos;
            public GameObject cliff; public Material seaMat; public List<(Material m, float ph)> foam = new List<(Material, float)>(); public Grass grass;
        }
        CineScene CS;
        System.Threading.Tasks.Task<CinePre> cinePre;
        void RunCineTasks(float budgetMs)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int prevLayer = Mk.layer; Mk.layer = CINE_LAYER; bool prevCol = Mk.colliders; Mk.colliders = false;
            while (CS.tasks.Count > 0) { var t = CS.tasks[0]; CS.tasks.RemoveAt(0); t(); if (sw.Elapsed.TotalMilliseconds > budgetMs) break; }
            Mk.layer = prevLayer; Mk.colliders = prevCol;
        }
        void BuildCineScene()
        {
            if (cinePre == null) cinePre = System.Threading.Tasks.Task.Run(() => CinePre.Compute());
            EnsureVolume();
            CS = new CineScene { root = new GameObject("Cine").transform };
            cineSeed = 20260;
            int prevLayer = Mk.layer; Mk.layer = CINE_LAYER;
            CS.world = Joint(CS.root); CS.city = Joint(CS.root); CS.hero = Joint(CS.root);
            foreach (var g in new[] { CS.world, CS.city, CS.hero }) g.go.SetActive(false);
            BuildWorld(); BuildCity(); BuildCliff();
            Mk.layer = prevLayer;
        }
        // Editor preview (see LevelPreview): the ending's city and the hero's sea cliff, built with the cutscene's own code.
        // Lights, probes and the sky domes are switched off so they don't take over the Scene view; the grass (drawn
        // instanced at runtime) is baked into a few meshes so the plateau isn't bare.
        public Transform BuildCinePreview()
        {
            Mats.Init(); if (Tex.Glow == null) Tex.Init();
            if (cinePre == null) cinePre = System.Threading.Tasks.Task.Run(() => CinePre.Compute());
            CS = new CineScene { root = new GameObject("Cutscene sets (preview)").transform };
            cineSeed = 20260;
            int prevLayer = Mk.layer; Mk.layer = CINE_LAYER;
            CS.world = Joint(CS.root); CS.city = Joint(CS.root); CS.hero = Joint(CS.root);
            CS.city.go.name = "City (ending)"; CS.hero.go.name = "Hero's sea cliff (ending)";
            BuildCity(); BuildCliff();
            cinePre.Wait(); RunCineTasks(float.MaxValue);
            Mk.layer = prevLayer;
            UnityEngine.Object.DestroyImmediate(CS.world.go);
            var gr = CS.grass;
            if (gr != null)
            {
                for (int g = 0; g < gr.gm.Length; g++)
                {
                    var ci = new List<CombineInstance>();
                    foreach (var s in gr.st) if (s.g == g) ci.Add(new CombineInstance { mesh = gr.grass, transform = Matrix4x4.TRS(V(s.x, s.y, s.z), Mk.XYZ(-0.2f, s.ry, 0), V(s.s, s.s * 1.1f, s.s)) });
                    if (ci.Count > 0) BakeMesh(ci, gr.gm[g], "grass");
                }
                for (int c = 0; c < gr.fm.Length; c++)
                {
                    var ci = new List<CombineInstance>(); foreach (var m in gr.fmtx[c]) ci.Add(new CombineInstance { mesh = gr.flower, transform = m });
                    if (ci.Count > 0) BakeMesh(ci, gr.fm[c], "flowers");
                }
            }
            foreach (var l in CS.root.GetComponentsInChildren<Light>(true)) l.enabled = false;
            foreach (var p in CS.root.GetComponentsInChildren<ReflectionProbe>(true)) p.enabled = false;
            foreach (var r in CS.root.GetComponentsInChildren<Renderer>(true)) if (r.sharedMaterial && r.sharedMaterial.shader && r.sharedMaterial.shader.name == "BR/Sky") r.gameObject.SetActive(false);
            CS.hero.t.localPosition = V(0, 0, -3000); // well away from the city (the sea is 3 km wide)
            return CS.root;
        }
        void BakeMesh(List<CombineInstance> ci, Material m, string name)
        {
            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, name = name };
            mesh.CombineMeshes(ci.ToArray(), true, true); mesh.RecalculateBounds();
            var g = new GameObject(name); g.transform.SetParent(CS.hero.t, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterial = m;
        }
        // sky params, CPU evaluation (for the ambient light probe), and SH from sky + hemisphere
        public class SkyParams { public Color top, mid, horizon, bottom, sunColor; public Vector3 sunDir; public float glow = 1, sunSize = 0.035f, sunPower = 9; }
        static Color Lin(int hex) => Mats.Hex(hex).linear;
        static float Smooth(float a, float b, float x) => sstep(a, b, x);
        static Vector3 SkyEval(SkyParams s, Vector3 d)
        {
            float h = d.y; Color col;
            if (h > 0) col = Color.Lerp(Color.Lerp(s.horizon, s.mid, Smooth(0, 0.22f, h)), s.top, Smooth(0.18f, 0.85f, h));
            else col = Color.Lerp(s.horizon, s.bottom, Smooth(0, -0.25f, h));
            var sd = s.sunDir.normalized; float c = Mathf.Max(Vector3.Dot(d, sd), 0);
            float disc = Smooth(Mathf.Cos(s.sunSize), Mathf.Cos(s.sunSize * 0.82f), c);
            float halo = Mathf.Pow(c, 6) * 0.45f + Mathf.Pow(c, 48) * 1.1f + Mathf.Pow(c, 600) * 2.5f;
            float haze = Mathf.Exp(-Mathf.Abs(h - sd.y) * 9) * Mathf.Pow(c, 2.5f) * 0.6f;
            return new Vector3(col.r, col.g, col.b) + new Vector3(s.sunColor.r, s.sunColor.g, s.sunColor.b) * (halo * s.glow + haze * s.glow + disc * s.sunPower);
        }
        static void ApplySky(Material m, SkyParams s)
        {
            // the shader receives linear colours as vectors (no sRGB conversion on SetVector)
            m.SetVector("_Top", s.top); m.SetVector("_Mid", s.mid); m.SetVector("_Horizon", s.horizon); m.SetVector("_Bottom", s.bottom);
            m.SetVector("_SunColor", s.sunColor); m.SetVector("_SunDir", s.sunDir.normalized);
            m.SetFloat("_Glow", s.glow); m.SetFloat("_SunSize", s.sunSize); m.SetFloat("_SunPower", s.sunPower);
        }
        static SkyParams MakeSkyParams(int top, int mid, int horizon, int bottom, Vector3 sunDir, int sunColor, float glow) =>
            new SkyParams { top = Lin(top), mid = Lin(mid), horizon = Lin(horizon), bottom = Lin(bottom), sunDir = sunDir.normalized, sunColor = Lin(sunColor), glow = glow };
        static int shSign;
        // ambient light probe = hemisphere light + the sky as an environment (three.js scene.environment, diffuse part)
        static SphericalHarmonicsL2 AmbientSH(SkyParams sky, int hemiSky, int hemiGround, float hemiI)
        {
            if (shSign == 0)
            {
                var test = new SphericalHarmonicsL2(); test.AddDirectionalLight(Vector3.up, Color.white, 1);
                var res = new Color[2]; test.Evaluate(new[] { Vector3.up, Vector3.down }, res);
                shSign = res[0].r >= res[1].r ? 1 : -1;
            }
            var sh = new SphericalHarmonicsL2();
            Color hs = Lin(hemiSky) * hemiI / Mathf.PI, hg = Lin(hemiGround) * hemiI / Mathf.PI;
            int NT = 24, NP = 48;
            for (int i = 0; i < NT; i++)
            {
                float th = (i + 0.5f) / NT * Mathf.PI, dth = Mathf.PI / NT;
                for (int j = 0; j < NP; j++)
                {
                    float ph = (j + 0.5f) / NP * Mathf.PI * 2, dph = Mathf.PI * 2 / NP;
                    var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                    float dw = Mathf.Sin(th) * dth * dph;
                    var L = SkyEval(sky, d); var hemi = d.y > 0 ? hs : hg;
                    var col = new Color(Mathf.Min(L.x, 40) + hemi.r, Mathf.Min(L.y, 40) + hemi.g, Mathf.Min(L.z, 40) + hemi.b);
                    sh.AddDirectionalLight(d * shSign, col, dw / Mathf.PI);
                }
            }
            return sh;
        }
        ReflectionProbe MakeProbe(N parent)
        {
            var go = new GameObject("probe"); go.transform.SetParent(parent.t, false);
            var rp = go.AddComponent<ReflectionProbe>();
            rp.mode = ReflectionProbeMode.Realtime; rp.refreshMode = ReflectionProbeRefreshMode.ViaScripting; rp.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            rp.resolution = 128; rp.hdr = true; rp.cullingMask = 1 << CINE_SKY; rp.size = Vector3.one * 8000; rp.boxProjection = false;
            rp.clearFlags = ReflectionProbeClearFlags.SolidColor; rp.backgroundColor = Color.black; rp.nearClipPlane = 1; rp.farClipPlane = 4000; rp.importance = 1;
            return rp;
        }
        GameObject SkyDome(N parent, Material m)
        {
            var g = Mk.Msh(parent, Geo.Sphere(1800, 48, 24), m); g.layer = CINE_SKY; NoShadow(g); return g;
        }
        void SetView(string v)
        {
            CS.world.go.SetActive(v == "world"); CS.city.go.SetActive(v == "city"); CS.hero.go.SetActive(v == "hero");
            cineCam.backgroundColor = Color.black; RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Custom; RenderSettings.ambientProbe = new SphericalHarmonicsL2();
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (v == "city")
            {
                RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = Mats.Hex(0xd8a080); RenderSettings.fogStartDistance = 90; RenderSettings.fogEndDistance = 620;
                RenderSettings.ambientProbe = CS.cityAmb; RenderSettings.sun = CS.citySun; if (urp) urp.shadowDistance = 70;
                SetPost(0.9f, 0.45f, 0.97f);
                if (!CS.cityEnvDone) { CS.cityEnvDone = true; CS.cityProbe.RenderProbe(); }
            }
            if (v == "hero")
            {
                RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = Mats.Hex(0xe8b48c); RenderSettings.fogStartDistance = 160; RenderSettings.fogEndDistance = 1600;
                RenderSettings.ambientProbe = CS.heroAmb; RenderSettings.sun = CS.heroSun; if (urp) urp.shadowDistance = 30;
                SetPost(0.95f, 0.78f, 0.85f);
                if (!CS.heroEnvDone) { CS.heroEnvDone = true; CS.heroProbe.RenderProbe(); }
            }
            if (v == "world") SetPost(1.05f, 0.85f, 0.82f);
            if (v == "title") SetPost(1, 0, 1);
        }
        // camera move along Catmull-Rom splines with gentle easing and a subtle hand-held drift
        Action<float, float, float> CamRig(Vector3[] points, Vector3[] looks)
        {
            var P = new GeoX.CatmullRom(points); var L = new GeoX.CatmullRom(looks);
            return (u, t, shake) =>
            {
                var p = P.GetPoint(u); var l = L.GetPoint(u);
                p.x += wander(t * 0.35f, 1) * shake; p.y += wander(t * 0.3f, 2) * shake; l.x += wander(t * 0.25f, 3) * shake * 2;
                CineLook(p, l);
            };
        }
        void CineLook(Vector3 pos, Vector3 target) { cineCam.transform.position = pos; if ((pos - target).sqrMagnitude > 1e-8f) cineCam.transform.rotation = Quaternion.LookRotation(pos - target); }

        void RainMotes(float dt)
        {
            var c = LEVEL.center;
            for (int i = 0; i < 3; i++)
            {
                float s = 0.12f + rand * 0.1f;
                particles.Add(new Pt { pos = V(c.x + rand * 70 - 35, 30 + rand * 15, c.z + rand * 70 - 35), scale = V(s, s, s), mat = rand < 0.5f ? moteMat : goldSpark, v = V(rand - 0.5f, -3 - rand * 3, rand - 0.5f), life = 8, grav = 0 });
            }
            TrimParticles();
        }

        // ----- the shots -----
        class Shot { public float dur, cap2At; public bool game, title; public string view, cap, cap2; public (string c, float t)? fadeIn, fadeOut; public Action setup; public Action<float, float> update; }
        List<Shot> SHOTS; Action<float, float, float> cityCam, heroCam;
        void DefineShots()
        {
            SHOTS = new List<Shot>
            {
                new Shot { dur = 6, game = true, fadeIn = ("#ffffff", 1.6f), cap = "THE SERAPH OF RUIN IS NO MORE.",
                    setup = () => { ApplyTheme(THEME); if (portal == null) OpenPortal(true); },
                    update = (t, dt) => { var c = LEVEL.center; float a = 0.5f + t * 0.16f; cineCam.fieldOfView = 50; CineLook(V(c.x + Mathf.Sin(a) * 34, 9 + t * 0.6f, c.z + Mathf.Cos(a) * 34), V(c.x, c.y + 6, c.z)); RainMotes(dt); } },
                new Shot { dur = 5, game = true, fadeOut = ("#ffffff", 1.3f), cap = "THE LAST GATE OF HEAVEN FALLS SILENT.",
                    update = (t, dt) => { var c = LEVEL.center; float k = ease(t / 5); CineLook(V(c.x, lerp(13, 170, k), c.z + lerp(30, 12, k)), c); RainMotes(dt); } },
                new Shot { dur = 8, view = "world", fadeIn = ("#ffffff", 1.3f), fadeOut = ("#000000", 1.2f), cap = "FAR BELOW, A DYING WORLD SEES THE SUN", cap2 = "FOR THE FIRST TIME IN A THOUSAND YEARS.", cap2At = 3.2f,
                    setup = () => cineCam.fieldOfView = 38,
                    update = (t, dt) =>
                    {
                        float k = ease3(t / 8);
                        CineLook(V(lerp(14, 5, k), lerp(5, 13, k), lerp(98, 80, k)), V(lerp(-5, -3, k), lerp(1, 4, k), 0));
                        CS.planet.ry += dt * 0.018f; CS.clouds.ry += dt * 0.03f;
                        float s = ease(clamp((t - 0.4f) / 7, 0, 1)); var sp = V(lerp(-60, -620, s), lerp(-40, 160, s), lerp(-800, -420, s));
                        CS.wSun.localPosition = sp; SetWorldLight(sp.normalized);
                    } },
                new Shot { dur = 8, view = "city", fadeIn = ("#000000", 1.2f), fadeOut = ("#000000", 1.2f), cap = "THEY WILL NEVER KNOW WHO SAVED THEM.",
                    setup = () =>
                    {
                        cineCam.fieldOfView = 42;
                        // street level among the crowd, then up over the sidewalk (clear of trees and lamps) to look down the avenue
                        cityCam = CamRig(new[] { V(-2.6f, 1.45f, -34), V(-7.6f, 2.7f, -30), V(-7.2f, 8.8f, -18), V(-4.5f, 10.5f, 1), V(-1.5f, 12, 10) },
                                         new[] { V(0.4f, 1.55f, -21), V(-1, 1.7f, -16), V(-0.5f, 2.6f, -24), V(0, 3.5f, -40), V(0, 9, -170) });
                    },
                    update = (t, dt) => { cityCam(ease3(t / 8), t, 0.04f); UpdateCityLife(t, dt); } },
                new Shot { dur = 9, view = "hero", fadeIn = ("#000000", 1.2f), fadeOut = ("#000000", 1.6f), cap = "BUT EVERY HEARTBEAT ON EARTH", cap2 = "IS PROOF OF WHAT YOU DID.", cap2At = 3.8f,
                    setup = () =>
                    {
                        cineCam.fieldOfView = 36;
                        var h = CS.heroPos; Vector3 o(float x, float y, float z) => V(h.x + x, h.y + y, h.z + z);
                        heroCam = CamRig(new[] { o(2.8f, 1.3f, 4.6f), o(-1.2f, 1.55f, 4.4f), o(-4.4f, 1.75f, 1.2f), o(-3.2f, 1.72f, -1.5f) }, new[] { o(0.2f, 1.3f, -30), o(0, 1.4f, -12), o(0, 1.55f, -1.5f), o(0, 1.66f, 0) });
                    },
                    update = (t, dt) => { heroCam(ease3(t / 9), t, 0.025f); UpdateHeroLife(t, dt); } },
                new Shot { dur = 6.5f, view = "title", fadeIn = ("#000000", 0.8f), title = true, update = (t, dt) => { } },
            };
        }
        void SetupShot(Shot sh)
        {
            SetCineCaption(sh.cap, sh.cap2);
            CINE.cap2 = false; CINE.capOff = false;
            SetCineTitle(sh.title);
            if (sh.game)
            {
                SetCineHQ(false); CS.world.go.SetActive(false); CS.city.go.SetActive(false); CS.hero.go.SetActive(false);
                cineCam.cullingMask = ~((1 << CINE_LAYER) | (1 << CINE_SKY) | (1 << VM_ENV_LAYER)); RenderSettings.fog = true; cineCam.backgroundColor = cam.backgroundColor;
            }
            else
            {
                if (cinePre != null && !cinePre.IsCompleted) cinePre.Wait();
                RunCineTasks(float.MaxValue);
                SetCineHQ(true); cineCam.cullingMask = (1 << CINE_LAYER) | (1 << CINE_SKY);
                SetView(sh.view);
            }
            sh.setup?.Invoke();
        }
        void StartCutscene()
        {
            won = true; CINE.active = true; CINE.t = 0; CINE.shot = -1;
            // the opening shots film the live arena: it must be the Seraph's Sanctum (heaven), whatever level is loaded
            if (LEVEL == null || LEVEL.boss != 2) { ClearTransient(); BuildLevel(LEVELS.FindIndex(l => l.boss == 2) + 1); levelCardPending = false; }
            if (CS == null) BuildCineScene();
            if (SHOTS == null) DefineShots();
            ReleaseHook(); mouseDown = false; audioMgr.Hum(0);
            if (DE.active) EndDeadeye();
            SetHudVisible(false); CineOverlay(true);
            vm.gameObject.SetActive(false);
            locked = false; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            audioMgr.Start(3);
        }
        void UpdateCine(float rdt)
        {
            CINE.t += rdt;
            float acc = 0; int idx = SHOTS.Count;
            for (int i = 0; i < SHOTS.Count; i++) { if (CINE.t < acc + SHOTS[i].dur) { idx = i; break; } acc += SHOTS[i].dur; }
            if (idx >= SHOTS.Count) { EndCutscene(); return; }
            var sh = SHOTS[idx]; float lt = CINE.t - acc;
            if (idx != CINE.shot) { CINE.shot = idx; SetupShot(sh); }
            if (sh.game && CS.tasks.Count > 0 && (cinePre == null || cinePre.IsCompleted)) RunCineTasks(6); // build the sets a little each frame
            if (sh.cap2 != null && !CINE.cap2 && lt > sh.cap2At) { CINE.cap2 = true; cineSmallOn = true; }
            if (!CINE.capOff && lt > sh.dur - 1.1f) { CINE.capOff = true; cineCapOn = false; cineSmallOn = false; }
            if (testRec) { cineCapOn = false; cineSmallOn = false; } // -brrec: clean footage, no captions
            float fo = 0; string fc = "#000000";
            if (sh.fadeIn.HasValue && lt < sh.fadeIn.Value.t) { fo = 1 - lt / sh.fadeIn.Value.t; fc = sh.fadeIn.Value.c; }
            if (sh.fadeOut.HasValue && lt > sh.dur - sh.fadeOut.Value.t) { fo = (lt - (sh.dur - sh.fadeOut.Value.t)) / sh.fadeOut.Value.t; fc = sh.fadeOut.Value.c; }
            SetCineFade(C(fc), fo);
            sh.update(lt, Mathf.Min(rdt, 0.05f));
            if (sh.game) { time += rdt; UpdateParticles(rdt); UpdateDecor(time, rdt); UpdatePortal(rdt); }
            if (sh.view == "city" || sh.view == "hero") DrawCineInstances(sh.view);
        }
        void EndCutscene()
        {
            if (!CINE.active) return;
            CINE.active = false;
            if (CS != null) { foreach (var f in CS.fwList) f.Destroy(); CS.fwList.Clear(); CS.world.go.SetActive(false); CS.city.go.SetActive(false); CS.hero.go.SetActive(false); }
            SetCineHQ(false);
            CineOverlay(false); SetCineTitle(false);
            RenderSettings.fog = true; ApplyTheme(THEME);
            cineCam.enabled = false; cam.enabled = true;
            audioMgr.Stop(2.5f);
            ShowVictory();
        }
    }

    // ----- pose library (all values are joint angles; smooth functions of time, no discontinuities) -----
    public static class CrowdPose
    {
        public const int hy = 0, hx = 1, hpx = 2, hpy = 3, hpz = 4, spx = 5, spy = 6, spz = 7, nkx = 8, nky = 9, hdx = 10, hdy = 11, hdz = 12,
            sLx = 13, sLy = 14, sLz = 15, eLx = 16, eLz = 17, wLx = 18, sRx = 19, sRy = 20, sRz = 21, eRx = 22, eRz = 23, wRx = 24,
            tLx = 25, tLz = 26, kLx = 27, fLx = 28, tRx = 29, tRz = 30, kRx = 31, fRx = 32, ry = 33, mouth = 34, N = 35;
        public static bool Slow(int i) => i == hdx || i == hdy || i == hdz || i == nkx || i == nky;
        public static float[] Rest() { var o = new float[N]; Rest(o); return o; }
        public static void Rest(float[] o)
        {
            Array.Clear(o, 0, N);
            // relaxed arms: a little forward and out, elbows softly bent (straight arms read as a mannequin)
            o[sLz] = 0.13f; o[sRz] = -0.13f; o[sLx] = -0.07f; o[sRx] = -0.07f; o[eLx] = -0.34f; o[eRx] = -0.34f; o[wLx] = -0.12f; o[wRx] = -0.12f; o[tLz] = 0.03f; o[tRz] = -0.03f; o[kLx] = 0.05f; o[kRx] = 0.05f; o[fLx] = -0.02f; o[fRx] = -0.02f;
        }
        // lower the hips by d while keeping the feet planted
        static void Crouch(float[] o, float d)
        {
            float a = Mathf.Acos(Mathf.Clamp(1 - d / 0.84f, 0.3f, 1));
            o[hy] -= d; o[tLx] -= a; o[tRx] -= a; o[kLx] += 2 * a; o[kRx] += 2 * a; o[fLx] -= a; o[fRx] -= a;
        }
        public static void Beh(string name, bool clasp, float tempo, float ph, bool kid, float t, float[] o)
        {
            switch (name)
            {
                case "idle":
                    if (clasp) { o[sLx] = o[sRx] = -0.32f; o[sLz] = 0.28f; o[sRz] = -0.28f; o[eLx] = o[eRx] = -0.95f; }
                    break;
                case "cheer":
                    {
                        float w = t * tempo * Mathf.PI * 2 * 1.05f, a = 0.5f + 0.5f * Mathf.Sin(w), b2 = 0.5f + 0.5f * Mathf.Sin(w + 0.5f);
                        o[sLz] = 2.5f + 0.16f * a; o[sRz] = -(2.5f + 0.16f * b2); o[sLx] = o[sRx] = -0.28f;
                        o[eLx] = -0.2f - 0.55f * a; o[eRx] = -0.2f - 0.55f * b2; o[wLx] = o[wRx] = -0.3f;
                        o[spx] -= 0.05f; o[hdx] -= 0.08f; o[mouth] = 0.55f + 0.45f * a;
                        Crouch(o, 0.03f * a);
                        break;
                    }
                case "wave":
                    o[sRz] = -2.4f; o[sRx] = -0.35f; o[eRx] = -0.55f; o[eRz] = 0.42f * Mathf.Sin(t * 6.5f + ph);
                    o[sLz] = 0.32f; o[sLx] = 0.18f; o[eLx] = -1.05f; o[hdz] = 0.06f * Mathf.Sin(t * 2 + ph); o[mouth] = 0.4f;
                    break;
                case "clap":
                    {
                        float r = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2 * 2.1f * tempo + ph), 2.4f);
                        o[sLx] = o[sRx] = -0.95f; o[sLz] = 0.05f + 0.34f * (1 - r); o[sRz] = -(0.05f + 0.34f * (1 - r));
                        o[eLx] = o[eRx] = -1.25f; o[hdx] += 0.05f * r; o[mouth] = 0.3f; Crouch(o, 0.012f * r);
                        break;
                    }
                case "jump":
                    {
                        float u = (t * tempo * 0.95f + ph) % 1; if (u < 0) u += 1;
                        if (u < 0.24f) { float c = Mathf.Sin(u / 0.24f * Mathf.PI / 2); Crouch(o, 0.11f * c); o[sLx] = o[sRx] = 0.55f * c; o[spx] += 0.18f * c; }
                        else if (u < 0.64f)
                        {
                            float a = (u - 0.24f) / 0.4f, h = Mathf.Sin(a * Mathf.PI);
                            o[ry] = 0.3f * h * (kid ? 1.1f : 1); Crouch(o, 0.11f * Mathf.Max(0, 1 - a * 4));
                            o[tLx] -= 0.3f * h; o[tRx] -= 0.3f * h; o[kLx] += 0.55f * h; o[kRx] += 0.55f * h;
                            o[sLz] = 0.1f + 2.3f * h; o[sRz] = -(0.1f + 2.3f * h); o[sLx] = o[sRx] = -0.4f * h; o[eLx] = o[eRx] = -0.3f; o[mouth] = 0.9f; o[hdx] -= 0.08f * h;
                        }
                        else if (u < 0.86f) { float l = Mathf.Sin((u - 0.64f) / 0.22f * Mathf.PI); Crouch(o, 0.1f * l); o[spx] += 0.12f * l; o[sLz] = 0.5f * l + 0.1f; o[sRz] = -(0.5f * l + 0.1f); }
                        break;
                    }
                case "point":
                    o[sRx] = -2.05f + 0.05f * Mathf.Sin(t * 1.3f); o[sRz] = -0.22f; o[eRx] = -0.08f; o[wRx] = 0.2f;
                    o[sLz] = 0.14f; o[eLx] = -0.25f; o[mouth] = 0.5f;
                    break;
                case "phone":
                    o[sRx] = -1.45f + 0.03f * Noise.Wander(t * 0.8f, ph); o[sRz] = -0.12f; o[eRx] = -1.0f; o[wRx] = 0.35f;
                    o[sLz] = 0.12f; o[eLx] = -0.3f;
                    break;
                case "flag":
                    o[sRz] = -2.15f - 0.32f * Mathf.Sin(t * 3.4f + ph); o[sRx] = -0.35f; o[eRx] = -0.35f; o[mouth] = 0.6f;
                    o[sLz] = 0.4f + 0.3f * (0.5f + 0.5f * Mathf.Sin(t * 3.4f + ph + 1)); o[eLx] = -0.6f;
                    break;
            }
        }
        // walking / running cycle (phase advances with distance travelled, so feet don't slide)
        public static void Gait(float[] o, float ph, float amt, bool run)
        {
            if (amt <= 0.001f) return;
            float sw = Mathf.Sin(ph), cw = Mathf.Cos(ph), A = run ? 0.62f : 0.42f;
            o[tLx] += -A * amt * sw; o[tRx] += A * amt * sw;
            o[kLx] += amt * ((run ? 0.25f : 0.08f) + (run ? 1.25f : 0.62f) * Mathf.Pow(Mathf.Max(0, cw), 1.5f));
            o[kRx] += amt * ((run ? 0.25f : 0.08f) + (run ? 1.25f : 0.62f) * Mathf.Pow(Mathf.Max(0, -cw), 1.5f));
            o[fLx] += amt * (0.22f * sw - 0.25f * Mathf.Max(0, cw)); o[fRx] += amt * (-0.22f * sw - 0.25f * Mathf.Max(0, -cw));
            o[hy] += -(run ? 0.06f : 0.03f) * amt * Mathf.Abs(sw) + (run ? 0.04f * amt : 0);
            o[hpy] += 0.12f * amt * sw; o[spy] -= 0.16f * amt * sw; o[hx] += 0.018f * amt * cw; o[hpz] += 0.045f * amt * cw;
            o[spx] += (run ? 0.2f : 0.05f) * amt;
            float armA = run ? 0.75f : 0.36f;
            o[sLx] = armA * amt * sw + o[sLx] * (1 - amt); o[sRx] = -armA * amt * sw + o[sRx] * (1 - amt);
            o[sLz] = 0.12f; o[sRz] = -0.12f;
            o[eLx] = -(run ? 1.35f : 0.3f) * amt - 0.15f * amt * Mathf.Max(0, -sw); o[eRx] = -(run ? 1.35f : 0.3f) * amt - 0.15f * amt * Mathf.Max(0, sw);
        }
    }
}
