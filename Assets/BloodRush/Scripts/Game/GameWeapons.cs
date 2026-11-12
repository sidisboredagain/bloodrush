using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    public class Hit
    {
        public bool head, coin, slam, punch, parry, close, explosion, sniper, deadeye, cheat, exec;
        public Vector3? point, dir; public float kb = -1;
        public int wpn = -1; // co-op: the shooter's weapon when it isn't the local player's (picks the death gore)
    }

    public partial class Game
    {
        // ---------- viewmodel materials ----------
        Material steel, gunmetal, blackMat, woodMat, gloveMat, sleeveMat, brassGun, blueMetal, glowBlue, glowOrange, coreGlowMat, barrelHeatMat, goldGun, runeMat, glassMat, obsidian, holeMat;
        Material[] coinMats;
        Transform vm; N vmN;
        class Gun { public Transform g; public string name, tip; public GameObject flash; public Material flashMat; public List<GameObject> left = new List<GameObject>(); }
        readonly List<Gun> guns = new List<Gun>();
        N revCyl, revHammer, pump, spinG, sniperBolt, bangSlide; GameObject shotgunCore, bangGlow; N shotgunN;
        Transform muzzleR, muzzleM, muzzleSn, muzzleBang, ejectS, ejectM, ejectSn; readonly List<Transform> shotgunMuzzles = new List<Transform>();
        Transform fist;
        class GAState { public float cyl, cylTarget, hammer, pumpT = 9, boltT = 9, coreTilt; public bool shellOut = true, boltOut = true; }
        readonly GAState GA = new GAState();
        const int VM_ENV_LAYER = 12; // only the viewmodel reflection probe sees this layer

        static Material G(Material m) { m.name = m.name.Replace("(BR)", "(G)"); return m; }
        GameObject VMsh(Transform p, Mesh mesh, Material mat, float x = 0, float y = 0, float z = 0, float rx = 0, float ry = 0, float rz = 0)
        {
            var g = Mk.Msh(p, mesh, mat, x, y, z, rx, ry, rz); g.layer = Mk.LayerIgnore; return g;
        }
        GameObject VS(Transform p, Mesh mesh, Material mat, float x, float y, float z, float rx, float ry, float rz, float sx, float sy, float sz)
        {
            var g = VMsh(p, mesh, mat, x, y, z, rx, ry, rz); g.transform.localScale = new Vector3(sx, sy, sz); return g;
        }
        GameObject MuzzleFlash(Transform parent, float z, float size, float y, out Material mat)
        {
            mat = G(Mats.Add(0xffffff, 1, Tex.Flash, false));
            var f = VMsh(parent, Geo.Plane(size, size), mat, 0, y, z); f.SetActive(false); return f;
        }
        static Mesh CY(float rt, float rb, float h, int s = 10) => Geo.Cyl(rt, rb, h, s);
        static Mesh VTOR(float r, float t, int seg = 28, int rad = 8, float arc = Mathf.PI * 2) => Geo.Torus(r, t, seg, rad, arc);
        // rounded box: soft bevelled edges instead of razor-sharp cube corners (extruded along z, centred)
        static Mesh VR(float w, float h, float d, float r = 0.008f) => GeoX.Cached($"vr{w},{h},{d},{r}", () =>
        {
            float b = Mathf.Min(r, Mathf.Min(w / 4, Mathf.Min(h / 4, d / 4))), hw = w / 2 - b, hh = h / 2 - b, c = Mathf.Min(b, Mathf.Min(hw, hh)) * 0.8f;
            var s = new GeoX.Shape().MoveTo(-hw + c, -hh).LineTo(hw - c, -hh).QuadTo(hw, -hh, hw, -hh + c).LineTo(hw, hh - c).QuadTo(hw, hh, hw - c, hh)
                .LineTo(-hw + c, hh).QuadTo(-hw, hh, -hw, hh - c).LineTo(-hw, -hh + c).QuadTo(-hw, -hh, -hw + c, -hh);
            float dep = Mathf.Max(1e-4f, d - 2 * b);
            var g = GeoX.ExtrudeMB(s, dep, 3, true, b, b, 2); g.Translate(new Vector3(0, 0, -dep / 2)); return g.Build();
        });
        // an extruded side profile (drawn in the z/y plane as x=z), for stocks; thickness along x
        static Mesh VPROF(string key, System.Func<GeoX.Shape> draw, float thick, float bevel = 0.006f) => GeoX.Cached("vp" + key, () =>
        {
            var g = GeoX.ExtrudeMB(draw(), thick - 2 * bevel, 10, true, bevel, bevel, 2);
            g.Translate(new Vector3(0, 0, -(thick - 2 * bevel) / 2)); g.Transform(Matrix4x4.Rotate(Quaternion.AngleAxis(-90, Vector3.up))); return g.Build();
        });
        // sleeve: rounded coat sleeve with a leather bracer and brass buckle (axis along local z); YXZ like the HTML game
        GameObject Arm(Transform parent, float x, float y, float z, float rx, float ry)
        {
            var a = Mk.Piv(parent, x, y, z); a.xyz = false; a.SetRot(rx, ry, 0);
            VMsh(a.t, CY(0.078f, 0.068f, 0.6f, 16), sleeveMat, 0, 0, 0, Mathf.PI / 2);
            VMsh(a.t, CY(0.072f, 0.072f, 0.08f, 16), gloveMat, 0, 0, -0.25f, Mathf.PI / 2);
            VMsh(a.t, VTOR(0.073f, 0.007f, 20, 6), brassGun, 0, 0, -0.215f);
            VMsh(a.t, VR(0.025f, 0.02f, 0.03f, 0.004f), brassGun, 0.07f, 0, -0.25f);
            gunArms.Add((a.t, a.t.localPosition, a.t.localRotation, x < 0));
            return a.go;
        }
        // When the gun is turned over (inspect, reload tilts) its arms would swing round with it, sleeves sweeping
        // across the screen. Instead each sleeve stays fixed at the wrist and keeps pointing back to its shoulder.
        readonly List<(Transform t, Vector3 p, Quaternion r, bool left)> gunArms = new List<(Transform, Vector3, Quaternion, bool)>();
        float armHoldW;
        Material heatTipMat, heatTipCore, heatGlowMat; GameObject heatGlow;
        void CompensateArms(float w)
        {
            foreach (var a in gunArms)
            {
                a.t.localPosition = a.p; a.t.localRotation = a.r;
                if (w <= 0.001f || !a.t.gameObject.activeInHierarchy) continue;
                var wrist = a.t.TransformPoint(V(0, 0, -0.25f));
                var shoulder = VmRoot().TransformPoint(a.left ? V(-0.55f, -0.85f, 0.3f) : V(0.5f, -0.85f, 0.3f));
                var dir = (shoulder - wrist).normalized;
                var rot = Quaternion.LookRotation(dir, camT.up);
                a.t.position = Vector3.Lerp(a.t.position, wrist + dir * 0.25f, w);
                a.t.rotation = Quaternion.Slerp(a.t.rotation, rot, w);
            }
        }
        // gloved hand wrapped round a grip: palm, four curled fingers and a thumb along the side
        void GripHand(Transform parent, float x, float y, float z, float rx)
        {
            var h = Mk.Piv(parent, x, y, z); h.rx = rx;
            VMsh(h.t, VR(0.1f, 0.12f, 0.1f, 0.03f), gloveMat, 0.012f, 0, 0.015f);
            for (int i = 0; i < 4; i++) VS(h.t, Geo.Sphere(0.026f, 12, 8), gloveMat, -0.005f, 0.035f - i * 0.026f, -0.05f, 0, 0, 0, 1.6f, 0.85f, 1);
            VMsh(h.t, CY(0.016f, 0.018f, 0.08f, 10), gloveMat, -0.052f, 0.035f, -0.015f, 0.3f, 0, 0);
            VMsh(h.t, VR(0.104f, 0.02f, 0.07f, 0.008f), blackMat, 0.012f, 0.045f, 0.035f); // knuckle strap
        }
        // compact grapple launcher strapped on top of a left wrist (barrel pointing down -z)
        void WristLauncher(Transform p, float x, float y, float z)
        {
            float PI = Mathf.PI;
            VMsh(p, VR(0.06f, 0.026f, 0.16f, 0.01f), gunmetal, x, y - 0.018f, z);                      // mount plate
            VMsh(p, Geo.Cyl(0.028f, 0.028f, 0.19f, 14), gunmetal, x, y + 0.012f, z - 0.02f, PI / 2);  // barrel
            VMsh(p, Geo.Cyl(0.034f, 0.031f, 0.028f, 14), steel, x, y + 0.012f, z - 0.12f, PI / 2);    // muzzle
            VMsh(p, Geo.Cyl(0.018f, 0.018f, 0.03f, 12), holeMat, x, y + 0.012f, z - 0.125f, PI / 2);
            VMsh(p, Geo.Torus(0.032f, 0.006f, 18, 6), glowOrange, x, y + 0.012f, z - 0.09f);
            VMsh(p, Geo.Cyl(0.022f, 0.022f, 0.05f, 12), brassGun, x, y + 0.012f, z + 0.09f, PI / 2);   // chain drum
            VMsh(p, VR(0.01f, 0.01f, 0.09f, 0.004f), glowOrange, x + 0.03f, y + 0.012f, z - 0.02f);
        }
        Transform Marker(Transform parent, float x, float y, float z) { var t = new GameObject("mk").transform; t.SetParent(parent, false); t.localPosition = V(x, y, z); return t; }

        // a small reflection map so polished metal, brass and gold catch light instead of reading as flat colour:
        // the HTML game's gradient environment, captured by a probe that rides with the camera (only the viewmodel uses it)
        void BuildVmEnv()
        {
            var env = new Cv(256, 128);
            (float t, string c)[] stops = { (0, "#fff6e6"), (0.38f, "#c9b8a0"), (0.5f, "#6a5a50"), (0.62f, "#2c2222"), (1, "#0c0808") };
            for (int y = 0; y < 128; y++)
            {
                float v = y / 127f; Color col = Mats.Css(stops[4].c);
                for (int k = 1; k < stops.Length; k++) if (v <= stops[k].t) { col = Color.Lerp(Mats.Css(stops[k - 1].c), Mats.Css(stops[k].c), (v - stops[k - 1].t) / (stops[k].t - stops[k - 1].t)); break; }
                for (int x = 0; x < 256; x++) env.p[y * 256 + x] = col;
            }
            env.Rect(30, 16, 46, 16, Color.white); env.Rect(150, 22, 30, 10, Color.white); env.Rect(205, 12, 22, 22, Color.white); // soft boxes
            env.Rect(0, 56, 256, 6, new Color(1, 170 / 255f, 120 / 255f, 0.5f));
            var envMat = G(Mats.Basic(0xffffff, env.ToTex(false, true), true));
            var probeGo = new GameObject("VM Reflection"); probeGo.transform.SetParent(camT, false);
            var sky = Mk.Msh(probeGo.transform, Geo.Sphere(40, 48, 24), envMat); sky.layer = VM_ENV_LAYER;
            var rp = probeGo.AddComponent<ReflectionProbe>();
            rp.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime; rp.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            rp.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing; rp.resolution = 64; rp.hdr = true;
            rp.cullingMask = 1 << VM_ENV_LAYER; rp.size = Vector3.one * 4; rp.importance = 10; rp.nearClipPlane = 0.5f; rp.farClipPlane = 100;
            rp.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor; rp.backgroundColor = Color.black;
            QualitySettings.realtimeReflectionProbes = true;
            rp.RenderProbe();
        }
        // coins: minted gold with an embossed winged-eye face, a knurled edge and a glint so they read in flight
        void BuildCoinMats()
        {
            var face = new Cv(256, 256);
            face.Radial(118, 112, 136, (0, Mats.Css("#fff4c0")), (0.55f, Mats.Css("#e8b444")), (0.9f, Mats.Css("#a8701c")), (1, Mats.Css("#6a4410")));
            void Ring(float r, float lw, string c) { for (int i = 0; i < 160; i++) { float a0 = i / 160f * Mathf.PI * 2, a1 = (i + 1) / 160f * Mathf.PI * 2; face.Line(128 + Mathf.Cos(a0) * r, 128 + Mathf.Sin(a0) * r, 128 + Mathf.Cos(a1) * r, 128 + Mathf.Sin(a1) * r, lw, Mats.Css(c)); } }
            Ring(112, 10, "#ffe28a"); Ring(104, 3, "#7a4e12");
            for (int i = 0; i < 24; i++) { float a = i / 24f * Mathf.PI * 2; face.Circle(128 + Mathf.Cos(a) * 94, 128 + Mathf.Sin(a) * 94, 3, Mats.Css("#7a4e12")); }
            foreach (var sx in new[] { -1f, 1f }) // wings
            {
                var s = new GeoX.Shape().MoveTo(18 * sx, -4).QuadTo(60 * sx, -50, 78 * sx, -30).QuadTo(62 * sx, -8, 70 * sx, 4).QuadTo(52 * sx, 8, 58 * sx, 22).QuadTo(36 * sx, 20, 18 * sx, 10).LineTo(18 * sx, -4);
                var pts = s.GetPoints(12); FillPoly(face, pts, 128, 128, Mats.Css("#ffeaa0"));
                for (int i = 0; i < pts.Count; i++) { var a = pts[i]; var b = pts[(i + 1) % pts.Count]; face.Line(128 + a.x, 128 + a.y, 128 + b.x, 128 + b.y, 5, Mats.Css("#6a4010")); }
            }
            face.Ellipse(128, 130, 32.5f, 20.5f, 0, Mats.Css("#6a4010")); face.Ellipse(128, 130, 27.5f, 15.5f, 0, Mats.Css("#ffeaa0"));
            face.Circle(128, 130, 10, Mats.Css("#6a4010")); face.Circle(125, 127, 3, Mats.Css("#fff8dc"));
            var edge = new Cv(128, 8);
            for (int i = 0; i < 128; i++) edge.Rect(i, 0, 1, 8, Mats.Css(i % 4 < 2 ? "#f0c060" : "#8a5a18"));
            var faceM = G(Mats.Std(0xffffff, 0.28f, 0.85f, 0, face.ToTex(false, false))); Mats.Emissive(faceM, 0x4a3008, 0.6f);
            var edgeM = G(Mats.Std(0xffffff, 0.3f, 0.9f, 0x3a2404, edge.ToTex(true, true))); edgeM.SetTextureScale("_BaseMap", new Vector2(4, 1));
            coinMats = new[] { edgeM, faceM, faceM };
        }
        static void FillPoly(Cv c, List<Vector2> pts, float ox, float oy, Color col)
        {
            float minx = float.MaxValue, miny = float.MaxValue, maxx = float.MinValue, maxy = float.MinValue;
            foreach (var p in pts) { minx = Mathf.Min(minx, p.x); maxx = Mathf.Max(maxx, p.x); miny = Mathf.Min(miny, p.y); maxy = Mathf.Max(maxy, p.y); }
            for (int y = Mathf.FloorToInt(miny); y <= Mathf.CeilToInt(maxy); y++)
                for (int x = Mathf.FloorToInt(minx); x <= Mathf.CeilToInt(maxx); x++)
                {
                    float px = x + 0.5f, py = y + 0.5f; bool inside = false;
                    for (int i = 0, j = pts.Count - 1; i < pts.Count; j = i++)
                        if ((pts[i].y > py) != (pts[j].y > py) && px < (pts[j].x - pts[i].x) * (py - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x) inside = !inside;
                    if (inside) c.Blend((int)(x + ox), (int)(y + oy), col);
                }
        }
        // three CylinderGeometry with its three material groups (side, top, bottom)
        static Mesh CoinMesh() => GeoX.Cached("coin3", () =>
        {
            var b = Geo.CylMB(0.22f, 0.22f, 0.04f, 40); var t = b.UnityTriangles(); int side = 40 * 2 * 3, cap = 40 * 3;
            var m = new Mesh(); m.SetVertices(b.v); m.SetNormals(b.n); m.SetUVs(0, b.uv); m.subMeshCount = 3;
            var tl = new List<int>(t);
            m.SetTriangles(tl.GetRange(0, side), 0); m.SetTriangles(tl.GetRange(side, cap), 1); m.SetTriangles(tl.GetRange(side + cap, tl.Count - side - cap), 2); m.RecalculateBounds(); return m;
        });
        GameObject MakeCoin(Vector3 pos)
        {
            var g = new GameObject("coin"); g.transform.SetParent(world, false); g.transform.localPosition = pos;
            g.AddComponent<MeshFilter>().sharedMesh = CoinMesh(); var r = g.AddComponent<MeshRenderer>(); r.sharedMaterials = coinMats;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Mk.Glow(g.transform, 0xffe08a, 0.9f, 0.55f);
            return g;
        }

        void BuildViewmodels()
        {
            steel = G(Mats.Std(0xb8bcc6, 0.26f, 0.9f)); gunmetal = G(Mats.Std(0x50545f, 0.34f, 0.8f)); blackMat = G(Mats.Std(0x17171b, 0.5f, 0.35f));
            var wood = new Cv(64, 256); wood.Fill(Mats.Css("#8a5a34"));
            float rnd() => UnityEngine.Random.value;
            for (int i = 0; i < 46; i++)
            {
                var col = new Color((40 + rnd() * 30) / 255f, (18 + rnd() * 12) / 255f, 8 / 255f, 0.25f + rnd() * 0.35f); float lw = 1 + rnd() * 2.5f, px = rnd() * 64;
                float c1 = px + rnd() * 12 - 6, c2 = px + rnd() * 12 - 6, e = px + rnd() * 8 - 4; Vector2 last = new Vector2(px, 0);
                for (int k = 1; k <= 24; k++) { float t = k / 24f, u = 1 - t; var p = new Vector2(u * u * u * px + 3 * u * u * t * c1 + 3 * u * t * t * c2 + t * t * t * e, t * 256); wood.Line(last.x, last.y, p.x, p.y, lw, col); last = p; }
            }
            woodMat = G(Mats.Std(0xffffff, 0.55f, 0.05f, 0, wood.ToTex(false, true))); woodMat.SetTextureScale("_BaseMap", new Vector2(10, 4));
            gloveMat = G(Mats.Std(0x2a2220, 0.62f, 0.1f)); sleeveMat = G(Mats.Std(0x5a1a1a, 0.85f));
            brassGun = G(Mats.Std(0xd0a24c, 0.24f, 0.9f)); blueMetal = G(Mats.Std(0x2f64b8, 0.3f, 0.7f));
            glowBlue = G(Mats.Basic(0x40c8ff)); glowOrange = G(Mats.Basic(0xff7020)); coreGlowMat = G(Mats.Basic(0xbff8ff));
            glassMat = G(Mats.Trans(0x9ad8ff, 0.35f, false, false));
            barrelHeatMat = G(Mats.Std(0x5a5d66, 0.3f, 0.8f)); goldGun = G(Mats.Std(0xe0b040, 0.2f, 0.95f, 0x2a1800)); runeMat = G(Mats.Basic(0xff2a2a));
            obsidian = G(Mats.Std(0x0e0a10, 0.15f, 0.6f)); holeMat = G(Mats.Basic(0x050508));
            vmN = Mk.Piv(VmRoot()); vm = vmN.t;
            BuildVmEnv(); BuildCoinMats();
            const float PI = Mathf.PI;

            // ---- Revolver (one-handed: the punch uses the free left hand) - a heavy six-shooter with a full-lug barrel,
            // blue energy channels, a fluted cylinder and a walnut grip
            var revolverG = Mk.Piv(vm).t;
            {
                var g = revolverG;
                VMsh(g, VR(0.084f, 0.12f, 0.22f, 0.014f), gunmetal, 0, 0, 0);                                    // frame
                VMsh(g, VR(0.068f, 0.032f, 0.22f, 0.01f), gunmetal, 0, 0.074f, -0.06f);                          // top strap
                VMsh(g, VR(0.02f, 0.016f, 0.32f, 0.006f), steel, 0, 0.083f, -0.3f);                              // vent rib
                for (int i = 0; i < 6; i++) VMsh(g, VR(0.022f, 0.006f, 0.018f, 0.002f), blackMat, 0, 0.088f, -0.17f - i * 0.045f);
                VMsh(g, CY(0.028f, 0.028f, 0.36f, 22), steel, 0, 0.045f, -0.29f, PI / 2);                       // barrel
                VMsh(g, VR(0.054f, 0.072f, 0.3f, 0.014f), gunmetal, 0, 0.012f, -0.27f);                          // full underlug
                foreach (var sx in new[] { -1f, 1f })
                {
                    VMsh(g, VR(0.005f, 0.012f, 0.27f, 0.002f), glowBlue, 0.027f * sx, 0.03f, -0.27f);            // energy channels
                    VMsh(g, VR(0.005f, 0.006f, 0.2f, 0.002f), glowBlue, 0.027f * sx, -0.005f, -0.25f);
                    VMsh(g, VR(0.004f, 0.05f, 0.004f, 0.0015f), brassGun, 0.043f * sx, 0.0f, 0.07f);            // gilded frame lines
                    VMsh(g, VR(0.004f, 0.004f, 0.12f, 0.0015f), brassGun, 0.043f * sx, -0.045f, 0.01f);
                    VMsh(g, Geo.Sphere(0.009f, 10, 8), brassGun, 0.043f * sx, 0.03f, 0.06f);
                }
                VMsh(g, CY(0.035f, 0.031f, 0.03f, 22), blackMat, 0, 0.045f, -0.465f, PI / 2);                   // crown
                VMsh(g, CY(0.015f, 0.015f, 0.032f, 14), holeMat, 0, 0.045f, -0.47f, PI / 2);
                VMsh(g, VR(0.012f, 0.032f, 0.026f, 0.004f), steel, 0, 0.1f, -0.44f);                             // front sight
                VMsh(g, VR(0.004f, 0.006f, 0.005f, 0.001f), glowBlue, 0, 0.115f, -0.437f);
                foreach (var sx in new[] { -1f, 1f }) VMsh(g, VR(0.012f, 0.018f, 0.02f, 0.003f), steel, 0.013f * sx, 0.098f, 0.03f);
                VMsh(g, VTOR(0.036f, 0.007f, 16, 6, PI), steel, 0, -0.07f, -0.025f, PI, PI / 2, 0);              // trigger guard
                VMsh(g, VR(0.012f, 0.046f, 0.014f, 0.005f), steel, 0, -0.07f, -0.02f, 0.35f);                    // trigger
                VMsh(g, VR(0.072f, 0.21f, 0.086f, 0.028f), woodMat, 0, -0.145f, 0.1f, -0.35f);                   // walnut grip
                foreach (var sx in new[] { -1f, 1f }) { VMsh(g, CY(0.016f, 0.016f, 0.006f, 14), brassGun, 0.038f * sx, -0.14f, 0.098f, 0, 0, PI / 2); VMsh(g, Geo.Sphere(0.008f, 10, 8), glowBlue, 0.041f * sx, -0.14f, 0.098f); }
                VMsh(g, VR(0.078f, 0.03f, 0.094f, 0.01f), steel, 0, -0.24f, 0.137f, -0.35f);                     // butt cap
                GripHand(g, 0.0f, -0.14f, 0.105f, -0.35f);
                Arm(g, 0.04f, -0.27f, 0.42f, 0.5f, 0);
            }
            revCyl = Mk.Piv(revolverG, 0, 0.02f, -0.07f);
            {
                var rc = revCyl.t;
                VMsh(rc, CY(0.066f, 0.066f, 0.11f, 28), steel, 0, 0, 0, PI / 2);
                VMsh(rc, CY(0.06f, 0.066f, 0.012f, 28), steel, 0, 0, -0.061f, PI / 2);
                VMsh(rc, CY(0.066f, 0.06f, 0.012f, 28), steel, 0, 0, 0.061f, PI / 2);
                VMsh(rc, CY(0.012f, 0.012f, 0.14f, 10), gunmetal, 0, 0, 0, PI / 2);
                for (int i = 0; i < 6; i++)
                {
                    float a = i * PI / 3;
                    VMsh(rc, CY(0.015f, 0.015f, 0.02f, 12), blackMat, Mathf.Cos(a) * 0.04f, Mathf.Sin(a) * 0.04f, -0.062f, PI / 2);
                    VMsh(rc, CY(0.011f, 0.011f, 0.022f, 10), brassGun, Mathf.Cos(a) * 0.04f, Mathf.Sin(a) * 0.04f, 0.061f, PI / 2);  // cartridge bases
                    VMsh(rc, VR(0.012f, 0.012f, 0.085f, 0.004f), blackMat, Mathf.Cos(a + 0.5f) * 0.063f, Mathf.Sin(a + 0.5f) * 0.063f, 0, 0, 0, a + 0.5f); // flutes
                }
            }
            revHammer = Mk.Piv(revolverG, 0, 0.07f, 0.07f);
            VMsh(revHammer.t, VR(0.022f, 0.052f, 0.026f, 0.006f), steel, 0, 0.02f, 0.01f);
            VMsh(revHammer.t, VR(0.026f, 0.01f, 0.034f, 0.004f), gunmetal, 0, 0.046f, 0.02f);
            muzzleR = Marker(revolverG, 0, 0.045f, -0.5f);
            var flashR = MuzzleFlash(revolverG, -0.52f, 0.35f, 0.045f, out var fmR);

            // ---- Shotgun: chunky blue core-eject shotgun, four barrels in a vented heat shroud, a glass core chamber
            shotgunN = Mk.Piv(vm, 0, 0, 0.04f); var shotgunG = shotgunN.t;
            {
                var g = shotgunG;
                VMsh(g, VR(0.13f, 0.15f, 0.34f, 0.02f), blueMetal, 0, 0, 0);                                     // receiver
                VMsh(g, VR(0.134f, 0.02f, 0.3f, 0.006f), gunmetal, 0, 0.078f, 0.0f);                             // top rail
                for (int i = 0; i < 7; i++) VMsh(g, VR(0.06f, 0.008f, 0.012f, 0.002f), blackMat, 0, 0.09f, -0.12f + i * 0.04f);
                foreach (var bx in new[] { -0.032f, 0.032f }) foreach (var by in new[] { -0.032f, 0.032f })
                    {
                        VMsh(g, CY(0.026f, 0.026f, 0.52f, 18), steel, bx, 0.04f + by, -0.44f, PI / 2);          // four barrels
                        VMsh(g, CY(0.018f, 0.018f, 0.022f, 14), holeMat, bx, 0.04f + by, -0.728f, PI / 2);
                        shotgunMuzzles.Add(Marker(g, bx, 0.04f + by, -0.74f));
                    }
                foreach (var sx in new[] { -1f, 1f }) VMsh(g, VR(0.012f, 0.15f, 0.4f, 0.005f), gunmetal, 0.074f * sx, 0.04f, -0.44f);   // shroud side plates
                for (int i = 0; i < 5; i++) foreach (var sx in new[] { -1f, 1f }) VMsh(g, VR(0.006f, 0.07f, 0.03f, 0.002f), glowOrange, 0.081f * sx, 0.04f, -0.3f - i * 0.075f); // vents
                VMsh(g, VR(0.15f, 0.014f, 0.42f, 0.005f), gunmetal, 0, 0.112f, -0.44f);
                for (int i = 0; i < 4; i++) VMsh(g, VR(0.13f, 0.008f, 0.036f, 0.003f), glowOrange, 0, 0.12f, -0.28f - i * 0.1f);  // heat sinks
                VMsh(g, VR(0.17f, 0.16f, 0.07f, 0.02f), blueMetal, 0, 0.04f, -0.69f);                            // muzzle block
                VMsh(g, VR(0.174f, 0.03f, 0.074f, 0.008f), gunmetal, 0, 0.12f, -0.69f);
                foreach (var sx in new[] { -1f, 1f }) VMsh(g, VR(0.008f, 0.06f, 0.04f, 0.003f), glowOrange, 0.086f * sx, 0.04f, -0.69f);
                VMsh(g, CY(0.026f, 0.026f, 0.42f, 14), gunmetal, 0, -0.045f, -0.4f, PI / 2);                    // magazine tube
                VMsh(g, CY(0.03f, 0.03f, 0.02f, 14), steel, 0, -0.045f, -0.61f, PI / 2);
                // core chamber: a ring with a glass dome over the glowing core
                VMsh(g, VTOR(0.045f, 0.01f, 24, 8), steel, 0.066f, 0.0f, -0.04f, 0, PI / 2, 0);
                VS(g, Geo.Sphere(0.045f, 18, 12), glassMat, 0.066f, 0, -0.04f, 0, 0, -PI / 2, 1, 0.6f, 1);
                VMsh(g, VR(0.06f, 0.15f, 0.08f, 0.02f), blackMat, 0, -0.125f, 0.1f, -0.35f);                     // pistol grip
                for (int i = 0; i < 4; i++) VMsh(g, VR(0.064f, 0.008f, 0.06f, 0.002f), gunmetal, 0, -0.08f - i * 0.03f, 0.09f - i * 0.01f, -0.35f);
                VMsh(g, VTOR(0.04f, 0.008f, 16, 6, PI), gunmetal, 0, -0.08f, 0.0f, PI, PI / 2, 0);
                // skeletal stock
                VMsh(g, VPROF("sgstock", () => new GeoX.Shape().MoveTo(0.14f, 0.03f).LineTo(0.5f, 0.0f).LineTo(0.52f, -0.16f).LineTo(0.47f, -0.17f).QuadTo(0.33f, -0.09f, 0.2f, -0.08f).LineTo(0.14f, -0.06f).LineTo(0.14f, 0.03f), 0.06f), blueMetal, 0, -0.02f, 0);
                VMsh(g, VR(0.07f, 0.19f, 0.035f, 0.012f), blackMat, 0, -0.09f, 0.51f, 0.08f);                    // butt pad
                VMsh(g, VR(0.004f, 0.012f, 0.2f, 0.002f), glowOrange, 0.031f, -0.025f, 0.33f);
                GripHand(g, 0, -0.12f, 0.11f, -0.35f);
                Arm(g, 0.04f, -0.26f, 0.42f, 0.5f, 0);
            }
            shotgunCore = VMsh(shotgunG, Geo.Sphere(0.032f, 14, 10), coreGlowMat, 0.066f, 0.0f, -0.04f);
            pump = Mk.Piv(shotgunG, 0, -0.05f, -0.37f);
            VMsh(pump.t, VR(0.1f, 0.075f, 0.22f, 0.02f), blackMat);
            for (int i = 0; i < 5; i++) VMsh(pump.t, VR(0.104f, 0.01f, 0.014f, 0.003f), blueMetal, 0, -0.012f, -0.09f + i * 0.045f);
            var shotgunLeft = new List<GameObject> { VMsh(pump.t, VR(0.11f, 0.08f, 0.14f, 0.03f), gloveMat, 0, -0.06f, 0), Arm(pump.t, -0.16f, -0.2f, 0.2f, 0.5f, -0.6f) };
            ejectS = Marker(shotgunG, 0.08f, 0.03f, -0.03f);
            var flashS = MuzzleFlash(shotgunG, -0.76f, 0.6f, 0.04f, out var fmS);

            // ---- Minigun: armoured rotary cannon - finned motor housing, belt-fed brass, glowing drum and six heat-coloured barrels
            var minigunG = Mk.Piv(vm, 0.02f, -0.06f, 0.06f).t; List<GameObject> minigunLeft;
            {
                var g = minigunG;
                VMsh(g, VR(0.19f, 0.19f, 0.36f, 0.03f), gunmetal, 0, 0, 0.06f);                                  // body
                VMsh(g, VR(0.2f, 0.035f, 0.24f, 0.01f), blackMat, 0, 0.11f, 0.06f);
                for (int i = 0; i < 4; i++) VMsh(g, VR(0.006f, 0.12f, 0.022f, 0.002f), glowBlue, 0.098f, 0, -0.04f + i * 0.06f);
                for (int i = 0; i < 3; i++) VMsh(g, CY(0.004f, 0.004f, 0.012f, 8), steel, -0.098f, 0.06f - i * 0.06f, 0.18f, 0, 0, PI / 2); // rivets
                VMsh(g, CY(0.105f, 0.105f, 0.12f, 28), blackMat, 0, 0, -0.17f, PI / 2);                         // motor housing
                for (int i = 0; i < 5; i++) VMsh(g, CY(0.115f, 0.115f, 0.008f, 28), gunmetal, 0, 0, -0.12f - i * 0.025f, PI / 2); // cooling fins
                VMsh(g, VTOR(0.106f, 0.008f, 32, 6), glowBlue, 0, 0, -0.232f);
                // carry handle
                foreach (var z in new[] { -0.04f, 0.14f }) VMsh(g, VR(0.03f, 0.09f, 0.03f, 0.008f), steel, 0, 0.15f, z);
                VMsh(g, CY(0.02f, 0.02f, 0.24f, 12), blackMat, 0, 0.2f, 0.05f, PI / 2);
                for (int i = 0; i < 6; i++) VMsh(g, CY(0.022f, 0.022f, 0.012f, 12), gunmetal, 0, 0.2f, -0.04f + i * 0.035f, PI / 2);
                // ammo drum with a window onto the glowing core
                VMsh(g, CY(0.13f, 0.13f, 0.11f, 30), gunmetal, -0.16f, -0.12f, 0.08f, 0, 0, PI / 2);
                foreach (var dx in new[] { -0.055f, 0.055f }) VMsh(g, VTOR(0.13f, 0.008f, 32, 6), steel, -0.16f + dx, -0.12f, 0.08f, 0, PI / 2, 0);
                VMsh(g, CY(0.07f, 0.07f, 0.115f, 20), glowOrange, -0.16f, -0.12f, 0.08f, 0, 0, PI / 2);
                VMsh(g, CY(0.076f, 0.076f, 0.004f, 20), glassMat, -0.218f, -0.12f, 0.08f, 0, 0, PI / 2);
                // belt of brass rounds feeding the gun
                for (int i = 0; i < 6; i++)
                {
                    float t = i / 5f, bx = -0.1f + t * 0.07f, by = -0.04f + t * 0.06f;
                    VMsh(g, CY(0.011f, 0.011f, 0.05f, 10), brassGun, bx, by, 0.05f, PI / 2);
                    VMsh(g, CY(0.002f, 0.011f, 0.016f, 10), brassGun, bx, by, 0.017f, PI / 2);
                    VMsh(g, VR(0.026f, 0.006f, 0.05f, 0.002f), blackMat, bx, by - 0.012f, 0.05f);
                }
                VMsh(g, VR(0.06f, 0.15f, 0.08f, 0.02f), blackMat, 0, -0.16f, 0.2f, -0.3f);
                GripHand(g, 0, -0.16f, 0.21f, -0.3f);
                Arm(g, 0.04f, -0.3f, 0.52f, 0.5f, 0);
                VMsh(g, VR(0.05f, 0.12f, 0.06f, 0.015f), blackMat, -0.11f, -0.08f, -0.1f);
                minigunLeft = new List<GameObject> { VMsh(g, VR(0.1f, 0.11f, 0.12f, 0.03f), gloveMat, -0.13f, -0.1f, -0.1f), Arm(g, -0.29f, -0.3f, 0.1f, 0.5f, -0.6f) };
            }
            spinG = Mk.Piv(minigunG, 0, 0, -0.24f);
            {
                var sg = spinG.t;
                for (int i = 0; i < 6; i++)
                {
                    float a = i * PI / 3, cx = Mathf.Cos(a) * 0.058f, cy = Mathf.Sin(a) * 0.058f;
                    VMsh(sg, CY(0.019f, 0.019f, 0.62f, 12), barrelHeatMat, cx, cy, -0.28f, PI / 2);
                    VMsh(sg, CY(0.023f, 0.021f, 0.03f, 12), steel, cx, cy, -0.585f, PI / 2);   // muzzle rings
                    VMsh(sg, CY(0.009f, 0.009f, 0.032f, 10), holeMat, cx, cy, -0.59f, PI / 2);
                }
                VMsh(sg, CY(0.092f, 0.092f, 0.022f, 28), steel, 0, 0, -0.55f, PI / 2);
                VMsh(sg, CY(0.088f, 0.088f, 0.03f, 28), steel, 0, 0, -0.3f, PI / 2);
                VMsh(sg, CY(0.09f, 0.09f, 0.04f, 28), gunmetal, 0, 0, -0.02f, PI / 2);
                for (int i = 0; i < 6; i++) { float a = i * PI / 3 + PI / 6; VMsh(sg, CY(0.012f, 0.012f, 0.024f, 10), blackMat, Mathf.Cos(a) * 0.072f, Mathf.Sin(a) * 0.072f, -0.55f, PI / 2); }
                VMsh(sg, CY(0.025f, 0.025f, 0.6f, 10), blackMat, 0, 0, -0.29f, PI / 2);
                // heat: the barrel ends glow red-hot as it fires and fade back as it cools
                heatTipMat = G(Mats.Add(0xff3010, 0, null, true, false)); heatTipCore = G(Mats.Add(0xffc060, 0, null, true, false));
                for (int i = 0; i < 6; i++)
                {
                    float a = i * PI / 3, cx = Mathf.Cos(a) * 0.058f, cy = Mathf.Sin(a) * 0.058f;
                    VMsh(sg, Geo.Cyl(0.0205f, 0.0205f, 0.2f, 12, true), heatTipMat, cx, cy, -0.5f, PI / 2);
                    VMsh(sg, Geo.Cyl(0.0235f, 0.0215f, 0.032f, 12, true), heatTipCore, cx, cy, -0.585f, PI / 2);
                }
                heatGlow = Mk.Glow(sg, 0xff4010, 0.5f, 0); heatGlow.transform.localPosition = V(0, 0, -0.6f); heatGlowMat = G(heatGlow.GetComponent<Renderer>().sharedMaterial);
            }
            muzzleM = Marker(minigunG, 0, 0, -0.84f); ejectM = Marker(minigunG, 0.1f, -0.02f, 0);
            var flashM = MuzzleFlash(minigunG, -0.86f, 0.5f, 0, out var fmM);

            // ---- Sniper: long fluted barrel with a ported brake, octagonal glowing handguard, a proper scope and a skeleton stock
            var sniperG = Mk.Piv(vm, -0.02f, 0.0f, 0.12f).t; List<GameObject> sniperLeft;
            {
                var g = sniperG;
                VMsh(g, VR(0.1f, 0.12f, 0.44f, 0.016f), gunmetal, 0, 0, 0);                                      // receiver
                VMsh(g, VR(0.06f, 0.012f, 0.4f, 0.004f), blackMat, 0, 0.066f, -0.02f);                           // picatinny base
                for (int i = 0; i < 12; i++) VMsh(g, VR(0.064f, 0.006f, 0.012f, 0.002f), gunmetal, 0, 0.075f, -0.2f + i * 0.033f);
                VMsh(g, CY(0.022f, 0.027f, 0.95f, 18), steel, 0, 0.03f, -0.68f, PI / 2);                        // barrel
                for (int i = 0; i < 6; i++) { float a = i * PI / 3; VMsh(g, VR(0.006f, 0.006f, 0.5f, 0.002f), blackMat, Mathf.Cos(a) * 0.023f, 0.03f + Mathf.Sin(a) * 0.023f, -0.9f); } // flutes
                VMsh(g, CY(0.042f, 0.042f, 0.42f, 8), blackMat, 0, 0.03f, -0.42f, PI / 2, 0, PI / 8);         // octagonal handguard
                for (int i = 0; i < 4; i++) VMsh(g, VR(0.012f, 0.006f, 0.05f, 0.002f), glowBlue, 0, 0.073f, -0.28f - i * 0.08f);
                foreach (var sx in new[] { -1f, 1f }) for (int i = 0; i < 3; i++) VMsh(g, VR(0.004f, 0.02f, 0.05f, 0.002f), glowBlue, 0.042f * sx, 0.03f, -0.3f - i * 0.1f);
                // muzzle brake with side ports
                VMsh(g, VR(0.072f, 0.058f, 0.14f, 0.012f), blackMat, 0, 0.03f, -1.18f);
                foreach (var sx in new[] { -1f, 1f }) for (int i = 0; i < 3; i++) VMsh(g, VR(0.004f, 0.032f, 0.022f, 0.002f), cyanMat, 0.037f * sx, 0.03f, -1.14f - i * 0.035f);
                VMsh(g, CY(0.014f, 0.014f, 0.02f, 12), holeMat, 0, 0.03f, -1.252f, PI / 2);
                // scope
                VMsh(g, CY(0.036f, 0.036f, 0.38f, 22), blackMat, 0, 0.125f, -0.1f, PI / 2);
                VMsh(g, CY(0.054f, 0.036f, 0.09f, 22), blackMat, 0, 0.125f, -0.33f, PI / 2);
                VMsh(g, CY(0.054f, 0.054f, 0.02f, 22), gunmetal, 0, 0.125f, -0.37f, PI / 2);
                VMsh(g, CY(0.042f, 0.046f, 0.07f, 22), blackMat, 0, 0.125f, 0.12f, PI / 2);
                VMsh(g, CY(0.047f, 0.047f, 0.005f, 22), cyanMat, 0, 0.125f, -0.381f, PI / 2);
                VMsh(g, CY(0.033f, 0.033f, 0.004f, 18), glassMat, 0, 0.125f, 0.157f, PI / 2);
                foreach (var z in new[] { -0.2f, 0.02f }) { VMsh(g, VR(0.03f, 0.06f, 0.03f, 0.008f), gunmetal, 0, 0.08f, z); VMsh(g, VTOR(0.039f, 0.008f, 22, 6), gunmetal, 0, 0.125f, z); }
                VMsh(g, CY(0.02f, 0.02f, 0.04f, 14), steel, 0.045f, 0.125f, -0.1f, 0, 0, PI / 2);               // windage turret
                VMsh(g, CY(0.02f, 0.02f, 0.04f, 14), steel, 0, 0.17f, -0.1f);                                   // elevation turret
                VMsh(g, VTOR(0.02f, 0.003f, 14, 4), glowBlue, 0, 0.19f, -0.1f, PI / 2);
                sniperMag = Mk.Piv(g, 0, -0.115f, -0.04f);                                                        // magazine (comes out on reload)
                VMsh(sniperMag.t, VR(0.06f, 0.13f, 0.1f, 0.012f), blackMat, 0, 0, 0);
                VMsh(sniperMag.t, VR(0.064f, 0.012f, 0.104f, 0.004f), steel, 0, -0.065f, 0);
                // skeleton stock with cheek riser
                VMsh(g, VPROF("snstock", () => new GeoX.Shape().MoveTo(0.18f, 0.05f).LineTo(0.62f, 0.03f).LineTo(0.64f, -0.13f).LineTo(0.58f, -0.14f).QuadTo(0.42f, -0.06f, 0.3f, -0.07f).LineTo(0.18f, -0.06f).LineTo(0.18f, 0.05f), 0.07f), gunmetal, 0, -0.01f, 0);
                VMsh(g, VR(0.066f, 0.04f, 0.22f, 0.012f), blackMat, 0, 0.06f, 0.4f);
                VMsh(g, VR(0.08f, 0.18f, 0.035f, 0.012f), blackMat, 0, -0.05f, 0.63f);
                VMsh(g, VR(0.004f, 0.03f, 0.16f, 0.002f), glowBlue, 0.036f, 0.0f, 0.42f);
                VMsh(g, VR(0.06f, 0.15f, 0.08f, 0.02f), blackMat, 0, -0.12f, 0.12f, -0.35f);                     // pistol grip
                VMsh(g, VTOR(0.04f, 0.008f, 16, 6, PI), gunmetal, 0, -0.075f, 0.03f, PI, PI / 2, 0);
                foreach (var sx in new[] { -1f, 1f }) VMsh(g, VR(0.014f, 0.014f, 0.38f, 0.004f), steel, 0.02f * sx, -0.03f, -0.75f); // folded bipod
                VMsh(g, VR(0.06f, 0.02f, 0.04f, 0.006f), gunmetal, 0, -0.03f, -0.56f);
                GripHand(g, 0, -0.13f, 0.13f, -0.35f);
                Arm(g, 0.04f, -0.27f, 0.44f, 0.5f, 0);
                sniperLeft = new List<GameObject> { VMsh(g, VR(0.11f, 0.08f, 0.14f, 0.03f), gloveMat, 0, -0.04f, -0.42f), Arm(g, -0.16f, -0.18f, -0.2f, 0.5f, -0.6f) };
            }
            sniperBolt = Mk.Piv(sniperG, 0.06f, 0.03f, 0.1f);
            VMsh(sniperBolt.t, CY(0.012f, 0.012f, 0.08f, 12), steel, 0.03f, 0, 0, 0, 0, PI / 2);
            VMsh(sniperBolt.t, Geo.Sphere(0.024f, 14, 10), blackMat, 0.07f, 0, 0);
            VMsh(sniperBolt.t, VTOR(0.024f, 0.004f, 16, 4), brassGun, 0.07f, 0, 0, 0, PI / 2, 0);
            muzzleSn = Marker(sniperG, 0, 0.03f, -1.26f); ejectSn = Marker(sniperG, 0.07f, 0.03f, 0.05f);
            var flashSn = MuzzleFlash(sniperG, -1.28f, 0.9f, 0.03f, out var fmSn);

            // ---- BANG: the cheat hand cannon - gold and obsidian with glowing blood-red runes and a horned compensator
            var pistolG = Mk.Piv(vm, 0, 0.01f, 0.02f).t;
            {
                var g = pistolG;
                VMsh(g, VR(0.1f, 0.11f, 0.3f, 0.016f), goldGun, 0, 0, -0.02f);                                  // frame
                VMsh(g, VTOR(0.036f, 0.008f, 16, 6, PI), goldGun, 0, -0.075f, -0.035f, PI, PI / 2, 0);
                VMsh(g, VR(0.012f, 0.044f, 0.014f, 0.005f), steel, 0, -0.07f, -0.02f, 0.3f);
                VMsh(g, VR(0.08f, 0.21f, 0.092f, 0.026f), obsidian, 0, -0.145f, 0.09f, -0.3f);                  // grip
                foreach (var sx in new[] { -1f, 1f })
                {
                    VMsh(g, VR(0.006f, 0.17f, 0.064f, 0.003f), goldGun, 0.041f * sx, -0.145f, 0.09f, -0.3f);   // gilded grip frame
                    VMsh(g, VR(0.007f, 0.13f, 0.044f, 0.003f), obsidian, 0.043f * sx, -0.145f, 0.09f, -0.3f);
                    VMsh(g, Geo.Sphere(0.02f, 14, 10), runeMat, 0.047f * sx, -0.13f, 0.085f);
                    VMsh(g, VTOR(0.022f, 0.004f, 16, 4), goldGun, 0.048f * sx, -0.13f, 0.085f, 0, PI / 2, 0);
                    for (int i = 0; i < 4; i++) VMsh(g, VR(0.004f, 0.008f, 0.035f, 0.0015f), runeMat, 0.051f * sx, -0.005f + (i % 2) * 0.02f, -0.13f + i * 0.06f);
                }
                VMsh(g, CY(0.03f, 0.03f, 0.2f, 16), obsidian, 0, 0.03f, -0.27f, PI / 2);                       // barrel
                // horned compensator
                VMsh(g, VR(0.094f, 0.094f, 0.09f, 0.02f), goldGun, 0, 0.03f, -0.4f);
                foreach (var sx in new[] { -1f, 1f })
                {
                    VMsh(g, VR(0.005f, 0.034f, 0.05f, 0.002f), runeMat, 0.048f * sx, 0.03f, -0.4f);
                    VMsh(g, CY(0.002f, 0.016f, 0.07f, 10), goldGun, 0.034f * sx, 0.085f, -0.4f, -0.5f, 0, -0.5f * sx);   // horns
                }
                for (int i = 0; i < 3; i++) VMsh(g, VR(0.05f, 0.005f, 0.012f, 0.002f), runeMat, 0, 0.078f, -0.37f - i * 0.025f);
                VMsh(g, CY(0.024f, 0.024f, 0.01f, 16), runeMat, 0, 0.03f, -0.446f, PI / 2);
                VMsh(g, VTOR(0.03f, 0.006f, 20, 6), goldGun, 0, 0.03f, -0.447f);
                VMsh(g, VR(0.1f, 0.03f, 0.06f, 0.01f), goldGun, 0, -0.06f, -0.12f);                              // accessory rail
                GripHand(g, 0, -0.14f, 0.105f, -0.3f);
                Arm(g, 0.04f, -0.27f, 0.42f, 0.5f, 0);
            }
            bangSlide = Mk.Piv(pistolG, 0, 0.075f, -0.05f);
            VMsh(bangSlide.t, VR(0.095f, 0.052f, 0.34f, 0.012f), obsidian);
            for (int i = 0; i < 6; i++) VMsh(bangSlide.t, VR(0.098f, 0.03f, 0.008f, 0.002f), goldGun, 0, 0.0f, 0.06f + i * 0.018f);   // serrations
            VMsh(bangSlide.t, VR(0.099f, 0.006f, 0.24f, 0.002f), runeMat, 0, -0.018f, -0.03f);
            VMsh(bangSlide.t, VR(0.03f, 0.006f, 0.26f, 0.002f), goldGun, 0, 0.027f, -0.03f);
            VMsh(bangSlide.t, VR(0.012f, 0.025f, 0.02f, 0.004f), goldGun, 0, 0.037f, -0.15f);
            VMsh(bangSlide.t, CY(0.0f, 0.022f, 0.05f, 10), goldGun, 0, 0.036f, 0.12f);                         // rear spike
            muzzleBang = Marker(pistolG, 0, 0.03f, -0.46f);
            var flashBang = MuzzleFlash(pistolG, -0.48f, 0.7f, 0.03f, out var fmB);
            bangGlow = Mk.Glow(pistolG, 0xff3020, 0.3f, 0.8f); bangGlow.transform.localPosition = V(0, 0.03f, -0.45f);

            revolverG.localRotation = Quaternion.Euler(0, 0.1f * Mathf.Rad2Deg, 0); shotgunN.SetRot(0, 0.08f, 0); minigunG.localRotation = Quaternion.Euler(0, 0.07f * Mathf.Rad2Deg, 0);
            sniperG.localRotation = Quaternion.Euler(0, 0.06f * Mathf.Rad2Deg, 0); pistolG.localRotation = Quaternion.Euler(0, 0.1f * Mathf.Rad2Deg, 0);
            guns.Add(new Gun { g = revolverG, name = "REVOLVER", tip = "headshots x2 · RMB coin toss", flash = flashR, flashMat = fmR });
            guns.Add(new Gun { g = shotgunG, name = "SHOTGUN", tip = "RMB: lob a core · SHOOT IT to detonate", flash = flashS, flashMat = fmS, left = shotgunLeft });
            guns.Add(new Gun { g = minigunG, name = "MINIGUN", tip = "hold to spin · watch the heat · RMB MELTDOWN LASER", flash = flashM, flashMat = fmM, left = minigunLeft });
            guns.Add(new Gun { g = sniperG, name = "SNIPER", tip = "one shot, one kill · RMB DEADEYE", flash = flashSn, flashMat = fmSn, left = sniperLeft });
            guns.Add(new Gun { g = pistolG, name = "BANG", tip = "kills anything · RMB: JUDGEMENT (all but one)", flash = flashBang, flashMat = fmB });
            Mk.SetLayerRecursive(vm.gameObject, Mk.LayerIgnore);

            // the punching fist IS the left hand: the gun's own left hand lets go while it punches
            fist = Mk.Piv(camT, -0.28f, -0.25f, -0.4f).t;
            VMsh(fist, Geo.Box(0.15f, 0.13f, 0.16f), gloveMat); VMsh(fist, Geo.Box(0.155f, 0.05f, 0.05f), steel, 0, 0.03f, -0.07f);
            VMsh(fist, Geo.Box(0.13f, 0.13f, 0.5f), sleeveMat, 0.02f, -0.04f, 0.32f);
            fist.gameObject.SetActive(false);
            // the grapple launcher lives on the left wrist, so it's there on every left hand: the guns' support hands and the punch
            foreach (var gun in guns) foreach (var l in gun.left) if (l.transform.childCount > 1) WristLauncher(l.transform, 0, 0.078f, -0.17f);
            WristLauncher(fist, 0.02f, 0.078f, 0.13f);
            // the viewmodel reflects the probe that rides with the camera
            foreach (var r in vm.GetComponentsInChildren<MeshRenderer>(true)) r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
            foreach (var r in fist.GetComponentsInChildren<MeshRenderer>(true)) r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
            BuildHook(); BuildLaser();
        }

        // ---------- weapons ----------
        void SetWeapon(int i)
        {
            if (DE.active) return;
            if (P != null && P.cheatT > 0 && i != 4) return; // the BANG cannon is locked in until it expires
            if (P != null && P.weapon == i && swapT > 0) return;
            if (P != null) { P.weapon = i; P.cd = Mathf.Max(P.cd, 0.15f); P.coreCharging = false; }
            inspectT = 0; CancelReload();
            for (int k = 0; k < guns.Count; k++) guns[k].g.gameObject.SetActive(k == i);
            swapT = 0.15f;
            SetWeaponHud(i);
        }
        struct HS { public float dist; public Vector3 point; public Ent enemy; public bool head, level; public Coin coin; public Core core; }
        static float RaySphere(Vector3 o, Vector3 d, Vector3 c, float r)
        {
            float ox = o.x - c.x, oy = o.y - c.y, oz = o.z - c.z;
            float b = ox * d.x + oy * d.y + oz * d.z, cc = ox * ox + oy * oy + oz * oz - r * r, disc = b * b - cc;
            if (disc < 0) return -1; float t = -b - Mathf.Sqrt(disc); return t > 0 ? t : -1;
        }
        HS Hitscan(Vector3 o, Vector3 d, float far, bool withCoins, bool withCores)
        {
            var best = new HS { dist = far, point = o + d * far };
            if (Physics.Raycast(o, d, out var lh, far, LEVEL_MASK, QueryTriggerInteraction.Ignore)) best = new HS { dist = lh.distance, point = lh.point, level = true };
            if (Physics.Raycast(o, d, out var eh, far, ENEMY_MASK, QueryTriggerInteraction.Ignore) && eh.distance < best.dist)
            {
                var part = eh.collider.GetComponent<Part>();
                if (part && part.e != null && !part.e.dead) best = new HS { dist = eh.distance, point = eh.point, enemy = part.e, head = part.head };
            }
            if (withCoins) foreach (var c in coins)
                {
                    if (c.dead) continue; float t = RaySphere(o, d, c.pos, 0.75f);
                    if (t > 0 && t < best.dist) best = new HS { dist = t, point = c.pos, coin = c };
                }
            if (withCores) foreach (var c in cores)
                {
                    if (c.dead) continue; float t = RaySphere(o, d, c.pos, 0.9f);
                    if (t > 0 && t < best.dist) best = new HS { dist = t, point = c.pos, core = c };
                }
            return best;
        }
        // where to aim at an enemy. A boss is aimed at its actual head (the head part's bounds), so Deadeye, the coin and the
        // aim-assists follow it when it bows its head while stunned, rears up or is lying down, instead of firing over it
        Vector3 TargetPoint(Ent e)
        {
            if (e.type == "boss" && e.g)
            {
                if (e.tr != null && e.tr.headless && e.tr.torso != null && e.tr.torso.t) return e.tr.torso.t.TransformPoint(V(0, 0.45f, 0.05f)); // the headless Traitor: his chest (his head is lying on the floor)
                if (!e.headT) { foreach (var p in e.g.GetComponentsInChildren<Part>()) if (p.head && p.GetComponent<Renderer>()) { e.headT = p.transform; break; } }
                if (e.headT && e.headT.gameObject.activeInHierarchy) { var rr = e.headT.GetComponent<Renderer>(); if (rr) return rr.bounds.center; }
            }
            return V(e.pos.x, e.pos.y + e.headY, e.pos.z);
        }
        Vector3 BodyPoint(Ent e) => V(e.pos.x, e.pos.y + e.bodyY, e.pos.z);
        void ShowFlash(int i, float light = 30)
        {
            var f = guns[i].flash; f.SetActive(true);
            f.transform.localRotation = Quaternion.Euler(0, 0, rand * 360); float s = 0.8f + rand * 0.5f; f.transform.localScale = V(s, s, s);
            flashT = 0.045f; muzzleLight.intensity = light * 0.12f;
        }
        void FireRevolver()
        {
            Vector3 o = EyePos, d = AimDir, mz = muzzleR.position;
            P.cd = 0.32f; recoil = 1; Sfx("revolver"); ShowFlash(0);
            GA.cylTarget += Mathf.PI / 3; GA.hammer = 1;
            var h = Hitscan(o, d, 400, true, true);
            Tracer(mz, h.point, 0xfff0a0, 0.05f);
            if (h.coin != null) Ricochet(h.coin, 3, 0);
            else if (h.core != null) DetonateCore(h.core, true);
            else if (h.enemy != null) DamageEnemy(h.enemy, h.head ? 3 : 1.5f, new Hit { head = h.head, point = h.point, dir = d, kb = 2 });
            else if (h.level) SpawnParticles(h.point, 4, 4, sparkMat, 0.06f, 0.25f);
        }
        void FireShotgun()
        {
            Vector3 o = EyePos, d = AimDir;
            P.cd = 0.85f; recoil = 1; Sfx("shotgun"); ShowFlash(1, 50); P.shake = Mathf.Max(P.shake, 0.15f);
            GA.pumpT = 0; GA.shellOut = false;
            // the blast shoves you back the way it came, in any direction: fire at your feet for a hop, at the sky to drop
            var kb = -d * 11;
            P.vel.x += kb.x; P.vel.z += kb.z;
            P.vel.y = kb.y > 0 ? Mathf.Max(P.vel.y, 0) + kb.y : P.vel.y + kb.y;
            if (kb.y > 2) { P.onGround = false; P.coyote = 0; }
            P.knockT = Mathf.Max(P.knockT, 0.3f); // ground friction eases off so the push carries
            for (int i = 0; i < 12; i++)
            {
                var pd = (d + V(rand - 0.5f, rand - 0.5f, rand - 0.5f) * 0.17f).normalized;
                var h = Hitscan(o, pd, 45, false, true);
                Tracer(shotgunMuzzles[i % 4].position, h.point, 0xffb060, 0.035f, 0.07f);
                if (h.core != null) DetonateCore(h.core, true);
                else if (h.enemy != null)
                {
                    float fall = h.dist < 10 ? 1 : Mathf.Max(0.25f, 1 - (h.dist - 10) / 30);
                    DamageEnemy(h.enemy, 0.55f * fall, new Hit { point = h.point, dir = pd, kb = 2.5f, close = h.dist < 4 });
                    if (h.enemy.type != "boss") { h.enemy.stun = Mathf.Max(h.enemy.stun, 0.15f); if (h.dist < 4.5f && !h.enemy.dead) Stagger(h.enemy); } // point-blank blast staggers
                }
                else if (h.level && i % 3 == 0) SpawnParticles(h.point, 2, 4, sparkMat, 0.06f, 0.2f);
            }
        }
        void FireMinigun()
        {
            P.cd = 0.05f; recoil = Mathf.Min(1, recoil + 0.25f);
            P.heat += 0.009f; P.shake = Mathf.Max(P.shake, 0.09f); // ~110 rounds before it overheats
            Sfx("minigun"); ShowFlash(2, 22);
            Vector3 o = EyePos, d = AimDir, mz = muzzleM.position;
            float spread = 0.018f + P.heat * 0.025f; // tighter, so it still lands at range
            var pd = (d + V(rand - 0.5f, rand - 0.5f, rand - 0.5f) * spread * 2).normalized;
            var h = Hitscan(o, pd, 320, true, true);
            Tracer(mz, h.point, 0xffc070, 0.035f, 0.05f);
            if (h.coin != null) Ricochet(h.coin, 1.5f, 0);
            else if (h.core != null) DetonateCore(h.core, true);
            else if (h.enemy != null) { DamageEnemy(h.enemy, h.head ? 0.85f : 0.5f, new Hit { head = h.head, point = h.point, dir = pd, kb = 0.7f }); h.enemy.flinch = Mathf.Min(1, h.enemy.flinch + 0.1f); }
            else if (h.level && rand < 0.5f) SpawnParticles(h.point, 2, 4, sparkMat, 0.05f, 0.2f);
            SpawnCasing(ejectM.position, FlatRight * (4 + rand * 2) + V(0, 3 + rand * 2, 0) + P.vel * 0.8f, brassMat, 0.03f, 0.03f, 0.07f);
            if (P.heat >= 1) { P.heat = 1; P.overheat = 1.8f; Sfx("overheat"); }
        }
        // ---------- minigun alt-fire: MELTDOWN LASER (hold RMB) ----------
        // The barrels spin up and a searing beam pours out, piercing everything in its line for heavy damage. It heats
        // the gun to its limit in ~1.5s; once the beam stops (released or maxed out) the minigun locks for 10 seconds.
        const float LASER_HEAT_TIME = 1.5f, LASER_LOCK = 10, LASER_TICK = 0.05f, LASER_DMG = 3.2f, LASER_BOSS_DMG = 0.9f; // one tick melts husks and drones
        bool laserHeld, laserFiring, laserCooling; float laserFiredT, laserTickT, laserSfxT;
        Transform laserOuter, laserCore, laserEnd, laserStart; Material laserOuterM, laserCoreM;
        void BuildLaser()
        {
            laserOuterM = G(Mats.Add(0xff4a18, 0.55f, null, true, true)); laserCoreM = G(Mats.Add(0xfff0d0, 1, null, true, true));
            laserOuter = Mk.Msh(world, Geo.Cyl(1, 1, 1, 14, true), laserOuterM).transform;
            laserCore = Mk.Msh(world, Geo.Cyl(1, 1, 1, 10, true), laserCoreM).transform;
            laserEnd = Mk.Glow(world, 0xff7030, 2.4f, 0.9f).transform; laserStart = Mk.Glow(world, 0xffb060, 0.9f, 0.9f).transform;
            foreach (var t in new[] { laserOuter, laserCore, laserEnd, laserStart }) { Mk.SetLayerRecursive(t.gameObject, Mk.LayerIgnore); t.gameObject.SetActive(false); }
        }
        void ShowLaser(bool on) { foreach (var t in new[] { laserOuter, laserCore, laserEnd, laserStart }) if (t && t.gameObject.activeSelf != on) t.gameObject.SetActive(on); }
        void UpdateLaser(float dt)
        {
            bool can = laserHeld && P.weapon == 2 && P.overheat <= 0 && swapT <= 0 && !P.dead && !DE.active && P.cheatT <= 0;
            if (can && P.spin >= 0.6f)
            {
                if (!laserFiring) { laserFiring = true; laserFiredT = 0; Sfx("laserCharge"); }
                laserFiredT += dt; P.heat = Mathf.Min(1, P.heat + dt / LASER_HEAT_TIME);
                Vector3 o = EyePos, d = AimDir, mz = muzzleM.position;
                laserTickT -= dt; bool tick = laserTickT <= 0; if (tick) laserTickT = LASER_TICK;
                PierceShot(o, d, out var end, out var lvl, (e, h) =>
                {
                    if (!tick) return true;
                    DamageEnemy(e, e.type == "boss" ? LASER_BOSS_DMG : LASER_DMG, new Hit { point = h.point, dir = d, kb = 0.5f, close = false });
                    e.flinch = Mathf.Min(1, e.flinch + 0.2f);
                    if (rand < 0.5f) SpawnParticles(h.point, 2, 5, emberMat, 0.08f, 0.35f);
                    return true;
                });
                // the beam: white-hot core inside a flickering orange sheath, from the muzzle to whatever stops it
                var dir = end - mz; float len = Mathf.Max(0.01f, dir.magnitude); var rot = Quaternion.FromToRotation(Vector3.up, dir / len);
                float fl = 1 + 0.18f * Mathf.Sin(time * 60) + (rand - 0.5f) * 0.15f;
                laserOuter.position = (mz + end) * 0.5f; laserOuter.rotation = rot; laserOuter.localScale = V(0.17f * fl, len, 0.17f * fl);
                laserCore.position = laserOuter.position; laserCore.rotation = rot; laserCore.localScale = V(0.055f, len, 0.055f);
                laserEnd.position = end - dir / len * 0.2f; laserEnd.localScale = Vector3.one * (2 + rand * 1.2f);
                laserStart.position = mz; laserStart.localScale = Vector3.one * (0.8f + rand * 0.4f);
                Mats.SetAlpha(laserOuterM, 0.45f + 0.15f * P.heat);
                ShowLaser(true);
                if (lvl && rand < 0.7f) SpawnParticles(end, 2, 6, sparkMat, 0.07f, 0.3f);
                laserSfxT -= dt; if (laserSfxT <= 0) { laserSfxT = 0.09f; Sfx("laserTick"); }
                P.shake = Mathf.Max(P.shake, 0.16f); recoil = Mathf.Min(1, recoil + 0.15f);
                P.vel -= d * 6 * dt; // a steady shove back
                if (P.heat >= 1) StopLaser();
            }
            else if (laserFiring) StopLaser();
            else ShowLaser(false);
            if (laserCooling)
            {
                P.heat = Mathf.Max(0, P.heat - dt / LASER_LOCK);           // the glow fades over the whole lockout
                if (P.overheat <= 0) { laserCooling = false; Sfx("pump"); }
            }
        }
        void StopLaser()
        {
            laserFiring = false; ShowLaser(false);
            if (laserFiredT > 0.05f) { P.overheat = LASER_LOCK; laserCooling = true; Sfx("overheat"); P.heat = Mathf.Max(P.heat, 0.85f); }
        }

        // pierces: every enemy along the line takes the hit
        void PierceShot(Vector3 o, Vector3 d, out Vector3 end, out bool hitLevel, System.Func<Ent, RaycastHit, bool> onHit)
        {
            float L = 400; end = o + d * 400; hitLevel = false;
            if (Physics.Raycast(o, d, out var lh, 400, LEVEL_MASK, QueryTriggerInteraction.Ignore)) { L = lh.distance; end = lh.point; hitLevel = true; }
            foreach (var c in cores.ToArray()) { if (c.dead) continue; float t = RaySphere(o, d, c.pos, 0.9f); if (t > 0 && t < L) DetonateCore(c, true); }
            var hits = Physics.RaycastAll(o, d, L, ENEMY_MASK, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            var seen = new HashSet<Ent>();
            foreach (var h in hits)
            {
                if (!h.collider) continue;
                var part = h.collider.GetComponent<Part>(); if (!part || part.e == null || part.e.dead || seen.Contains(part.e)) continue;
                seen.Add(part.e); onHit(part.e, h);
            }
        }
        void FireSniper(bool deadeye)
        {
            P.cd = deadeye ? 0 : 1.3f; recoil = 1; GA.boltT = 0; GA.boltOut = false;
            Sfx("sniper"); ShowFlash(3, 70);
            Vector3 o = EyePos, d = AimDir, mz = muzzleSn.position;
            bool any = false; int kills = 0;
            PierceShot(o, d, out var end, out var lvl, (e, h) =>
            {
                any = true; bool head = h.collider.GetComponent<Part>().head;
                float dmg = e.type == "boss" ? (deadeye ? 12 : 20) : 999;
                DamageEnemy(e, dmg, new Hit { head = head, point = h.point, dir = d, kb = 12, sniper = true, deadeye = deadeye });
                if (e.dead) kills++;
                SpawnParticles(h.point, 10, 10, bloodMat, 0.15f, 0.8f);
                return true;
            });
            var from = deadeye ? o + d * 0.5f + V(0, -0.2f, 0) : mz;
            Tracer(from, end, 0x9ff0ff, 0.14f, 0.35f); Tracer(from, end, 0xffffff, 0.05f, 0.22f);
            for (int k = 1; k <= 8; k++) SpawnParticles(Vector3.Lerp(from, end, k / 9f), 1, 0.4f, steamMat, 0.25f, 0.9f, -1);
            if (lvl) SpawnParticles(end, 8, 6, sparkMat, 0.08f, 0.3f);
            if (any) { Sfx("snipeHit"); freeze = Mathf.Max(freeze, deadeye ? 0.03f : 0.08f); whiteFlash = Mathf.Max(whiteFlash, 0.12f); }
            if (kills >= 2) AddStyle($"COLLATERAL x{kills}", 150 * kills);
            float push = deadeye ? 4 : 15;
            P.vel -= d * push;
            if (!deadeye) { P.vel.y += 3.5f; P.knockT = 0.35f; P.onGround = false; P.coyote = 0; }
            P.kick = deadeye ? 0.03f : 0.16f; P.shake = Mathf.Max(P.shake, deadeye ? 0.2f : 0.4f);
        }
        void UpdateWeapons(float dt)
        {
            if (P.cheatT > 0)
            {
                P.cheatT -= dt;
                if (P.cheatT <= 0) { P.cheatT = 0; SetWeapon(P.prevWeapon); Sfx("deny"); S.feed.Insert(0, ("BANG EXPIRED", 2)); S.dirty = true; }
            }
            bangSlide.pz = -0.05f + (P.weapon == 4 ? recoil * 0.07f : 0);
            float bg = 0.3f + Mathf.Sin(time * 12) * 0.08f + (P.weapon == 4 ? recoil * 0.5f : 0); bangGlow.transform.localScale = V(bg, bg, bg);
            bool wantSpin = (mouseDown || laserHeld) && P.weapon == 2 && P.overheat <= 0 && swapT <= 0 && !P.dead && !DE.active;
            P.spin = clamp(P.spin + (wantSpin ? dt / 0.35f : -dt / 0.6f), 0, 1);
            UpdateLaser(dt); UpdateSaw(dt);
            if (P.overheat > 0)
            {
                P.overheat -= dt; if (!laserCooling) P.heat = Mathf.Max(0, P.heat - dt * 0.5f);
                if (P.weapon == 2 && rand < (laserCooling ? 0.06f : 0.4f)) SpawnParticles(muzzleM.position, 1, 1.5f, steamMat, laserCooling ? 0.07f : 0.12f, 0.6f, -3);
            }
            else if (!(wantSpin && P.spin >= 1) && !laserFiring) P.heat = Mathf.Max(0, P.heat - dt * 0.4f);
            if (!P.dead && !DE.active && mouseDown && P.cd <= 0 && swapT <= 0 && AmmoGate(P.weapon))
            {
                if (P.weapon != 2 || (wantSpin && P.spin >= 1 && !laserFiring)) P.shots = (P.shots + 1) & 255; // co-op: teammates draw our shots
                if (P.weapon == 2) { if (wantSpin && P.spin >= 1 && !laserFiring) FireMinigun(); }
                else if (P.weapon == 4) FireBang();
                else if (P.weapon == 3) { FireSniper(false); SpendAmmo(3); }
                else if (P.weapon == 1) { if (!P.coreCharging) { FireShotgun(); SpendAmmo(1); } }
                else { FireRevolver(); SpendAmmo(0); }
            }
            audioMgr.Hum(P.spin);
            GA.cyl += (GA.cylTarget - GA.cyl) * (1 - Mathf.Exp(-25 * dt)); revCyl.rz = GA.cyl;
            GA.hammer = Mathf.Max(0, GA.hammer - dt * 4); revHammer.rx = 0.6f * (1 - GA.hammer);
            GA.pumpT += dt;
            float pt = (GA.pumpT - 0.18f) / 0.32f;
            pump.pz = -0.37f + (pt > 0 && pt < 1 ? Mathf.Sin(pt * Mathf.PI) * 0.14f : 0);
            if (!GA.shellOut && pt > 0.5f)
            {
                GA.shellOut = true; Sfx("pump");
                if (P.weapon == 1) SpawnCasing(ejectS.position, FlatRight * 4 + V(0, 4, 0) + P.vel * 0.8f, shellMat, 0.04f, 0.04f, 0.09f);
            }
            float charge = P.coreCharging ? clamp(P.coreChargeT / 0.8f, 0, 1) : 0;
            GA.coreTilt += (charge - GA.coreTilt) * (1 - Mathf.Exp(-12 * dt));
            shotgunN.rz = -GA.coreTilt * 0.35f;
            float cs = 1 + GA.coreTilt * 0.8f + (P.coreCd <= 0 ? Mathf.Sin(time * 8) * 0.15f : -0.5f); shotgunCore.transform.localScale = V(cs, cs, cs);
            GA.boltT += dt;
            float bt = (GA.boltT - 0.3f) / 0.5f;
            sniperBolt.pz = 0.1f + (bt > 0 && bt < 1 ? Mathf.Sin(bt * Mathf.PI) * 0.13f : 0);
            sniperBolt.rz = bt > 0 && bt < 1 ? Mathf.Min(1, Mathf.Sin(bt * Mathf.PI) * 2) * 0.9f : 0;
            if (!GA.boltOut && bt > 0.5f)
            {
                GA.boltOut = true; Sfx("bolt");
                if (P.weapon == 3) SpawnCasing(ejectSn.position, FlatRight * 4 + V(0, 4, 0) + P.vel * 0.8f, brassMat, 0.035f, 0.035f, 0.11f);
            }
            spinG.rz += P.spin * 40 * dt;
            barrelHeatMat.SetColor("_EmissionColor", new Color(P.heat * 1.2f, P.heat * 0.25f, 0));
            float hot = Mathf.Pow(P.heat, 1.3f), shimmer = 0.9f + 0.1f * Mathf.Sin(time * 23);
            Mats.SetAlpha(heatTipMat, Mathf.Clamp01(hot * 1.1f) * shimmer); Mats.SetAlpha(heatTipCore, Mathf.Clamp01((P.heat - 0.45f) * 1.8f) * shimmer);
            Mats.SetAlpha(heatGlowMat, hot * 0.7f); heatGlow.transform.localScale = Vector3.one * (0.3f + hot * 0.5f);
            if (P.weapon == 2 && P.heat > 0.6f && rand < (P.heat - 0.6f) * 0.5f) SpawnParticles(muzzleM.position, 1, 1, steamMat, 0.08f, 0.5f, -2); // heat shimmer smoke
            flashT -= dt; if (flashT <= 0) foreach (var w in guns) w.flash.SetActive(false);
        }

        // ---------- coins ----------
        public class Coin { public Transform m; public Vector3 pos, vel; public float life; public bool dead; public N n; }
        readonly List<Coin> coins = new List<Coin>();
        void ThrowCoin()
        {
            if (P.dead || P.coins < 1) return;
            P.coins -= 1; Sfx("coin");
            Vector3 d = AimDir, f = FlatFwd;
            var pos = EyePos + d * 0.6f + V(0, -0.2f, 0);
            var g = MakeCoin(pos);
            var vel = f * 9 + d * 3; vel.y += 9; vel.x += P.vel.x * 0.6f; vel.z += P.vel.z * 0.6f;
            coins.Add(new Coin { m = g.transform, pos = pos, vel = vel, life = 4, n = new N(g.transform) });
        }
        void Ricochet(Coin coin, float dmg, int depth)
        {
            coin.dead = true; Mk.DestroyTree(coin.m.gameObject);
            Sfx("ding"); freeze = Mathf.Max(freeze, 0.06f);
            SpawnParticles(coin.pos, 6, 6, sparkMat, 0.08f, 0.3f, 0);
            Coin next = null; float nd = 1e9f;
            foreach (var c in coins) { if (c.dead) continue; float dd = Vector3.Distance(c.pos, coin.pos); if (dd < 45 && dd < nd && Los(coin.pos, c.pos)) { nd = dd; next = c; } }
            if (next != null && depth < 6) { Tracer(coin.pos, next.pos, 0xffee88, 0.1f, 0.15f); Ricochet(next, dmg * 1.5f, depth + 1); return; }
            Ent tgt = null; nd = 1e9f;
            foreach (var e in enemies) { if (e.dead || e.spawnT > 0) continue; var tp = TargetPoint(e); float dd = Vector3.Distance(tp, coin.pos); if (dd < 90 && dd < nd && Los(coin.pos, tp)) { nd = dd; tgt = e; } }
            if (tgt != null)
            {
                var tp = TargetPoint(tgt);
                Tracer(coin.pos, tp, 0xffee88, 0.1f, 0.15f);
                AddStyle(depth > 0 ? $"RICOSHOT x{depth + 1}" : "RICOSHOT", 100 * (depth + 1));
                DamageEnemy(tgt, dmg, new Hit { head = true, coin = true, point = tp, dir = (tp - coin.pos).normalized, kb = 6 });
            }
            else
            {
                var d = V(rand - 0.5f, rand * 0.4f, rand - 0.5f).normalized;
                Tracer(coin.pos, Hitscan(coin.pos, d, 60, false, false).point, 0xffee88, 0.1f, 0.15f);
            }
        }
        void UpdateCoins(float dt)
        {
            foreach (var c in coins)
            {
                if (c.dead) continue;
                c.vel.y -= 18 * dt; c.pos += c.vel * dt; c.m.position = c.pos;
                c.n.rx += dt * 20; c.n.rz += dt * 7; c.life -= dt;
                if (c.life <= 0 || PointInLevel(c.pos)) { c.dead = true; Mk.DestroyTree(c.m.gameObject); }
            }
            coins.RemoveAll(c => c.dead);
        }

        // ---------- shotgun cores ----------
        // the core is an unstable energy orb: lob it, then SHOOT it mid-air - it implodes (sucking enemies in) and
        // detonates with chain lightning. Left alone it just pops on contact. It also works as a rocket jump.
        public class Core { public Transform m; public Transform halo; public Material haloMat; public Vector3 pos, vel; public float life, age; public bool dead; public Transform ringA, ringB, reticle; }
        readonly List<Core> cores = new List<Core>();
        void LaunchCore()
        {
            if (!P.coreCharging) return;
            P.coreCharging = false;
            if (P.coreCd > 0 || P.dead) return;
            float ch = clamp(P.coreChargeT / 0.8f, 0, 1);
            P.coreCd = 5.0f; recoil = 0.8f; GA.pumpT = 0; GA.shellOut = false; Sfx("coreLaunch");
            Vector3 o = EyePos, d = AimDir;
            var root = new GameObject("core").transform; root.SetParent(world, false); root.position = o + d * 0.8f + V(0, -0.15f, 0);
            var c = Mk.Msh(root, Geo.Sphere(1, 14, 10), coreGlowMat); c.transform.localScale = V(0.15f, 0.15f, 0.15f);              // white-hot heart
            var shell = Mk.Msh(root, Geo.Sphere(1, 16, 12), Mats.Add(0x40c8ff, 0.55f, null, true, false)); shell.transform.localScale = V(0.26f, 0.26f, 0.26f);
            var ringA = Mk.Msh(root, Geo.Torus(0.34f, 0.018f, 28, 6), Mats.Add(0x9ff0ff, 1, null, true, true)).transform;      // spinning containment rings
            var ringB = Mk.Msh(root, Geo.Torus(0.4f, 0.012f, 28, 6), Mats.Add(0x40c8ff, 0.9f, null, true, true)).transform;
            var halo = Mk.Glow(root, 0x60e0ff, 1.5f, 0.9f);
            // "shoot me" target ring that always faces you
            var ret = new GameObject("reticle") { layer = Mk.LayerIgnore }.transform; ret.SetParent(root, false);
            ret.gameObject.AddComponent<MeshFilter>().sharedMesh = Geo.Plane(1, 1);
            var rr = ret.gameObject.AddComponent<MeshRenderer>(); rr.sharedMaterial = Mats.Add(0xbff8ff, 0.85f, Tex.Ring, false);
            rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; Mk.billboards.Add(ret);
            foreach (var r in root.GetComponentsInChildren<Transform>()) r.gameObject.layer = Mk.LayerIgnore;
            var vel = d * (16 + ch * 26) + V(0, 3, 0) + P.vel * 0.5f;
            cores.Add(new Core { m = root, halo = halo.transform, haloMat = halo.GetComponent<Renderer>().sharedMaterial, pos = root.position, vel = vel, life = 2.2f, ringA = ringA, ringB = ringB, reticle = ret });
        }
        void UpdateCores(float dt)
        {
            foreach (var c in cores.ToArray())
            {
                if (c.dead) continue;
                c.vel.y -= 22 * dt; c.pos += c.vel * dt; c.life -= dt; c.age += dt; c.m.position = c.pos;
                float unstable = 1 + Mathf.Max(0, 0.8f - c.life) * 3; // flickers harder as it's about to pop
                float hs = 1.3f + Mathf.Sin(time * 30 * unstable) * 0.3f; c.halo.localScale = V(hs, hs, hs);
                c.ringA.localRotation = Quaternion.Euler(time * 520, time * 230, 0); c.ringB.localRotation = Quaternion.Euler(-time * 310, 90, time * 600);
                float rs = Mathf.Min(1, c.age * 6) * (1.25f + Mathf.Sin(time * 14) * 0.15f); c.reticle.localScale = V(rs, rs, rs);
                SpawnParticles(c.pos, 1, 0.6f, cyanMat, 0.07f, 0.3f, 0);
                if (rand < 0.35f) SpawnParticles(c.pos + Random.insideUnitSphere * 0.3f, 1, 3, sparkMat, 0.04f, 0.15f, 0); // crackle
                bool hitE = false;
                foreach (var e in enemies) if (!e.dead && e.spawnT <= 0 && Vector3.Distance(c.pos, BodyPoint(e)) < e.hitR) { hitE = true; break; }
                if (hitE || PointInLevel(c.pos) || c.life <= 0 || !InBounds(c.pos, 2)) DetonateCore(c, false);
            }
            cores.RemoveAll(c => c.dead);
        }
        void DetonateCore(Core c, bool shot)
        {
            if (c.dead) return;
            c.dead = true; Mk.DestroyTree(c.m.gameObject);
            if (shot)
            {
                // implosion: drag everything nearby into the blast
                foreach (var e in enemies) if (!e.dead && e.type != "boss" && Vector3.Distance(BodyPoint(e), c.pos) < 13)
                    { var to = c.pos - e.pos; e.vel += to.normalized * Mathf.Min(16, to.magnitude * 2.2f); e.vel.y += 3; Stagger(e); }
                Blast(c.pos, 3, 0x40c8ff);
            }
            Explode(c.pos, shot ? 8 : 5, shot ? 7 : 4, shot ? 0xffffff : 0x60e0ff, shot ? 20 : 12, false);
            if (shot)
            {
                AddStyle("CORE DETONATION", 150); freeze = Mathf.Max(freeze, 0.08f); whiteFlash = Mathf.Max(whiteFlash, 0.07f);
                ShockRing(c.pos - V(0, 0.5f, 0), 11, 0x9ff0ff, 0.55f);
                // chain lightning to the closest enemies it can see
                var near = new List<Ent>();
                foreach (var e in enemies) if (!e.dead && e.spawnT <= 0 && Vector3.Distance(TargetPoint(e), c.pos) < 18 && Los(c.pos, TargetPoint(e))) near.Add(e);
                near.Sort((a, b) => Vector3.Distance(TargetPoint(a), c.pos).CompareTo(Vector3.Distance(TargetPoint(b), c.pos)));
                for (int i = 0; i < near.Count && i < 5; i++)
                {
                    var e = near[i]; var tp = TargetPoint(e);
                    Lightning(c.pos, tp, 0x9ff0ff); Lightning(c.pos, tp, 0xffffff);
                    if (!e.dead) DamageEnemy(e, 3, new Hit { explosion = true, point = tp, dir = (tp - c.pos).normalized, kb = 4 });
                    SpawnParticles(tp, 6, 6, cyanMat, 0.08f, 0.3f);
                }
            }
        }
        // a jagged bolt: a few tracer segments with random kinks
        void Lightning(Vector3 a, Vector3 b, int color)
        {
            const int N = 6; var prev = a; float len = Vector3.Distance(a, b);
            for (int i = 1; i <= N; i++)
            {
                var p = i == N ? b : Vector3.Lerp(a, b, i / (float)N) + Random.insideUnitSphere * len * 0.07f;
                Tracer(prev, p, color, 0.07f, 0.22f); prev = p;
            }
        }
        void Explode(Vector3 pos, float radius, float dmg, int color, float selfDmg, bool parry)
        {
            Blast(pos, radius, color); ShockRing(pos - V(0, 0.5f, 0), radius, color, 0.35f);
            Sfx("boom"); P.shake = Mathf.Max(P.shake, 0.5f);
            SpawnParticles(pos, 18, 12, sparkMat, 0.1f, 0.5f);
            foreach (var e in enemies.ToArray())
            {
                if (e.dead) continue;
                var bp = BodyPoint(e); float dist = Vector3.Distance(bp, pos) - e.hitR * 0.5f;
                if (dist < radius) DamageEnemy(e, dmg * (1 - Mathf.Max(0, dist) / radius * 0.5f), new Hit { explosion = true, parry = parry, dir = (bp - pos).normalized, kb = 10 });
            }
            if (selfDmg > 0 && !P.dead)
            {
                var c = ChestPos; float dist = Vector3.Distance(c, pos);
                if (dist < radius)
                {
                    var dir = (c - pos).normalized;
                    P.vel += dir * ((radius - dist) * 3.5f + 8); P.vel.y += 6; P.onGround = false; P.coyote = 0;
                    DamagePlayer(selfDmg * (1 - dist / radius * 0.5f), pos);
                }
            }
        }
        // ---------- the chainsaw arm (F): replaces the punch ----------
        // Hold F: the saw slides out of the elbow and grinds into everything in reach, each bite healing a little; let go
        // and it gives a final shove and retracts. Pressing it still parries orange shots (heals half your health).
        float sawTickT, sawAge, sawSfxT; bool sawParried; readonly HashSet<Ent> sawHit = new HashSet<Ent>();
        void Punch()
        {
            if (P.dead || DE.active || EX.active) return;
            if (TryExecute()) return;
            if (P.punchCd > 0) return;
            P.punchCd = 0.35f; fistT = SAW_DUR; sawHeld = true; inspectT = 0; sawTickT = 0.12f; sawParried = false; sawHit.Clear();
            Sfx("saw"); SawParry();
        }
        bool SawParry()
        {
            Vector3 o = EyePos, d = AimDir; bool parried = false;
            foreach (var pr in projectiles)
            {
                if (pr.friendly || !pr.parry) continue;
                var to = pr.pos - o; float dist = to.magnitude;
                if (dist < 4.8f && Vector3.Dot(to.normalized, d) > 0.3f)
                {
                    pr.friendly = true; pr.vel = d * 60; pr.grav = 0; pr.homing = 0;
                    pr.coreR.sharedMaterial = friendlyMat; Mats.SetColor(pr.haloMat, 0x40e0ff); pr.life = 3; parried = true;
                }
            }
            if (parried && !sawParried) { sawParried = true; Heal(50); freeze = 0.14f; whiteFlash = 0.35f; AddStyle("PARRY", 200); Sfx("parry"); }
            return parried;
        }
        void UpdateSaw(float dt)
        {
            if (EX.active) return; // the execution does its own cutting

            if (sawHeld && !P.dead && !DE.active) { fistT = SAW_DUR; sawAge += dt; } // keeps running while F is held
            if (fistT <= 0 || P.dead) { sawAge = 0; return; }
            if (!sawHeld && fistT > 0.12f) fistT = 0.12f;                             // let go: one last bite, then it retracts
            float age = sawAge;
            if (age < 0.25f) SawParry();
            if (sawHeld) { sawSfxT -= dt; if (sawSfxT <= 0) { sawSfxT = 0.42f; if (age > 0.3f) Sfx("saw", 0.7f); } }
            sawTickT -= dt; if (sawTickT > 0) return;
            if (sawExt < 0.8f) return;                                                 // the blade has to be out to cut
            sawTickT = 0.08f;
            Vector3 o = EyePos, d = AimDir; bool last = fistT < 0.08f, any = false;
            foreach (var e in enemies.ToArray())
            {
                if (e.dead || e.spawnT > 0) continue;
                var bp = e.boss == 5 ? V(e.pos.x, Mathf.Clamp(o.y, e.pos.y + 1, e.pos.y + 14), e.pos.z) : BodyPoint(e); var to = bp - o; float dist = to.magnitude; /* (the Warden's robes reach the floor: you can saw him from the ground) */
                if (dist > 2.9f + e.hitR * 0.6f || Vector3.Dot(to.normalized, d) < 0.25f) continue;
                any = true; bool first = sawHit.Add(e);
                if (e.type != "boss")
                {
                    if (last) { e.vel += d * (e.heavy ? 6 : 15); e.vel.y += e.heavy ? 2 : 5; e.stun = e.heavy ? 0.3f : 0.6f; }
                    else { e.vel *= 0.3f; e.stun = Mathf.Max(e.stun, e.heavy ? 0.15f : 0.3f); }
                }
                var hp = bp - to.normalized * Mathf.Min(dist, e.hitR * 0.8f);
                DamageEnemy(e, e.type == "boss" ? (first ? 1.5f : 0.5f) : (first ? 3.5f : 1.6f), new Hit { punch = true, close = true, point = hp, dir = d, kb = 0.3f });
                Heal(e.type == "boss" ? 4 : 1.2f); // (sawing into a boss heals properly)
                SpawnParticles(hp, 6, 7, bloodMat, 0.1f, 0.5f); SpawnParticles(hp, 3, 6, sparkMat, 0.05f, 0.25f);
            }
            if (any) { Sfx("sawHit"); P.shake = Mathf.Max(P.shake, 0.22f); P.kick += 0.006f; if (sawHit.Count > 0 && last) AddStyle(sawHit.Count > 1 ? $"CHAINSAWED x{sawHit.Count}" : "CHAINSAWED", 60 * sawHit.Count); }
        }

        // ---------- Deadeye (sniper RMB) ----------
        class DeadeyeState { public bool active; public List<Ent> list = new List<Ent>(); public int idx; public float t, cd; }
        readonly DeadeyeState DE = new DeadeyeState();
        void StartDeadeye()
        {
            if (DE.active || P.dead) return;
            if (DE.cd > 0) { Sfx("deny"); return; }
            // one target per round in the magazine (4 at most) - an empty sniper reloads instead
            int shots = NeedsAmmo(3) ? ammo[3] : 4;
            if (shots <= 0 || reloadT > 0) { Sfx("dryFire"); StartReload(); return; }
            Vector3 o = EyePos, f = AimDir;
            var cands = enemies.FindAll(e => !e.dead && e.spawnT <= 0 && !e.invuln && Vector3.Distance(TargetPoint(e), o) < 170 && Los(o, TargetPoint(e)));
            if (cands.Count == 0) { Sfx("deny"); return; }
            float Ang(Ent e) => Mathf.Acos(clamp(Vector3.Dot((TargetPoint(e) - o).normalized, f), -1, 1));
            cands.Sort((a, b) => Ang(a).CompareTo(Ang(b)));
            var list = cands.GetRange(0, Mathf.Min(shots, cands.Count));
            var bossE = list.Find(e => e.type == "boss");
            if (bossE != null) while (list.Count < shots) list.Add(bossE);
            if (HK.state != "idle") ReleaseHook();
            DE.list = list; DE.idx = 0; DE.t = 0.4f; DE.active = true;
            mouseDown = false;
            uiScope.SetActive(true); Sfx("scopeIn");
        }
        void UpdateDeadeye(float rdt)
        {
            if (!DE.active) { DE.cd = Mathf.Max(0, DE.cd - rdt); return; }
            while (DE.idx < DE.list.Count && DE.list[DE.idx].dead) DE.idx++;
            if (DE.idx >= DE.list.Count || P.dead) { EndDeadeye(); return; }
            var e = DE.list[DE.idx]; var d = TargetPoint(e) - EyePos;
            float yaw = Mathf.Atan2(-d.x, -d.z), pitch = Mathf.Atan2(d.y, Hypot(d.x, d.z));
            float k = 1 - Mathf.Exp(-22 * rdt);
            float dy = yaw - P.yaw; dy = Mathf.Atan2(Mathf.Sin(dy), Mathf.Cos(dy));
            P.yaw += dy * k; P.pitch += (pitch - P.pitch) * k;
            DE.t -= rdt;
            if (DE.t <= 0)
            {
                if (NeedsAmmo(3) && ammo[3] <= 0) { EndDeadeye(); return; } // out of rounds
                P.yaw = yaw; P.pitch = clamp(pitch, -1.55f, 1.55f); FireSniper(true); SpendAmmo(3); DE.idx++; DE.t = 0.22f;
            }
        }
        void EndDeadeye()
        {
            DE.active = false; DE.cd = 10; uiScope.SetActive(false);
            if (P != null) { P.cd = Mathf.Max(P.cd, 0.4f); if (P.weapon == 3 && NeedsAmmo(3) && ammo[3] == 0) reloadDelay = 0.35f; }
        }

        // ---------- weapon inspect (V) ----------
        const float INSPECT_DUR = 2.8f;
        static float Smoother(float x) { x = Mathf.Clamp01(x); return x * x * x * (x * (x * 6 - 15) + 10); }
        float inspectT; bool inspectMid;
        void StartInspect()
        {
            if (P == null || P.dead || DE.active || swapT > 0 || fistT > 0 || inspectT > 0 || P.coreCharging || WARP.active || reloadT > 0) return;
            inspectT = INSPECT_DUR; inspectMid = false; inspFresh = true; Sfx("inspect");
        }
        // the inspect is a smooth curve through a few key poses (Catmull-Rom, so it never stops dead at a key),
        // eased in and out of the normal pose, and the gun follows that curve on a spring - weighty, no snapping
        Vector3 inspPos, inspRot; bool inspFresh;
        static readonly float[] INSP_T = { 0.00f, 0.22f, 0.50f, 0.74f, 1.00f };
        static readonly Vector3[] INSP_P = { new Vector3(0.20f, -0.26f, -0.62f), new Vector3(0.12f, -0.19f, -0.50f), new Vector3(0.09f, -0.16f, -0.47f), new Vector3(0.06f, -0.18f, -0.52f), new Vector3(0.20f, -0.26f, -0.62f) };
        static readonly Vector3[] INSP_R = { new Vector3(0.05f, -0.15f, 0.05f), new Vector3(0.18f, -1.10f, 0.32f), new Vector3(0.30f, -0.55f, -0.55f), new Vector3(-0.10f, -0.22f, 0.12f), new Vector3(0.05f, -0.15f, 0.05f) };
        static Vector3 CatRom(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2 * b + (c - a) * t + (2 * a - 5 * b + 4 * c - d) * t2 + (3 * b - a - 3 * c + d) * t3);
        }
        static Vector3 InspCurve(Vector3[] k, float p)
        {
            int i = 0; while (i < INSP_T.Length - 2 && p > INSP_T[i + 1]) i++;
            float u = Mathf.Clamp01((p - INSP_T[i]) / (INSP_T[i + 1] - INSP_T[i]));
            return CatRom(k[Mathf.Max(0, i - 1)], k[i], k[i + 1], k[Mathf.Min(k.Length - 1, i + 2)], u);
        }
        void UpdateInspect(float dt)
        {
            if (mouseDown || P.dead || DE.active) { inspectT = 0; return; } // firing cancels the inspect
            inspectT -= dt;
            float p = clamp(1 - inspectT / INSPECT_DUR, 0, 1), sw = Mathf.Sin(p * Mathf.PI);
            var basePos = vmN.pos; var baseRot = vmN.r;
            if (inspFresh) { inspFresh = false; inspPos = basePos; inspRot = baseRot; }
            int wpn = P.weapon;
            float sy = wpn == 2 ? 0.55f : wpn == 3 ? 0.7f : 1, oz = wpn == 2 ? -0.13f : wpn == 3 ? -0.11f : 0, oy = wpn == 2 ? -0.05f : wpn == 1 ? -0.04f : 0;
            float pt = p * p * (3 - 2 * p) * 0.35f + p * 0.65f; // gently slower at the start and end of the whole move
            var pos = InspCurve(INSP_P, pt); var rot = InspCurve(INSP_R, pt);
            rot.y *= sy; pos.z += oz; pos.y += oy;
            // living hand: a little weighing bounce and wrist wobble, so it never looks frozen
            pos.y += Mathf.Sin(p * Mathf.PI * 4) * 0.007f * sw; rot.z += Mathf.Sin(p * Mathf.PI * 3 + 0.6f) * 0.045f * sw; rot.x += Mathf.Sin(p * Mathf.PI * 5) * 0.02f * sw;
            float env = Smoother(p / 0.2f) * Smoother((1 - p) / 0.24f);
            armHoldW = Mathf.Max(armHoldW, env);
            var tp = Vector3.Lerp(basePos, pos, env); var tr = Vector3.Lerp(baseRot, rot, env);
            float a = 1 - Mathf.Exp(-11 * dt);
            inspPos += (tp - inspPos) * a; inspRot += (tr - inspRot) * a;
            // spins ride on top of the spring (a full turn ends where it began, so nothing unwinds)
            float twirlX = wpn == 0 ? Smoother((p - 0.4f) / 0.3f) * Mathf.PI * 2 : 0;   // revolver: twirl
            float rollZ = wpn == 4 ? Smoother((p - 0.36f) / 0.3f) * Mathf.PI * 2 : 0;    // BANG: barrel roll
            vmN.pos = inspPos;
            vmN.SetRot(inspRot.x + twirlX, inspRot.y, inspRot.z + rollZ);
            if (wpn == 2) spinG.rz += dt * 30 * sw;
            if (wpn == 4) { float s = 0.3f + sw * 0.6f; bangGlow.transform.localScale = V(s, s, s); }
            if (!inspectMid && p > 0.5f)
            {
                inspectMid = true;
                if (wpn == 0) { GA.cylTarget += Mathf.PI * 2; Sfx("click"); }
                else if (wpn == 1) { GA.pumpT = 0; GA.shellOut = true; Sfx("pump"); }
                else if (wpn == 2) Sfx("inspSpin");
                else if (wpn == 3) { GA.boltT = 0; GA.boltOut = true; Sfx("bolt"); }
                else { Sfx("click"); Sfx("inspBang"); }
            }
            if (inspectT <= 0) inspectT = 0;
        }

        // ---------- BANG cheat: type B-A-N-G during play ----------
        void ActivateBang()
        {
            if (P == null || P.dead || !started || won) return;
            if (P.cheatT <= 0) P.prevWeapon = P.weapon;
            if (DE.active) EndDeadeye();
            P.cheatT = 10; P.coreCharging = false;
            SetWeapon(4); swapT = 0.3f;
            Banner("BANG!", "CHEAT ACTIVATED · 10 SECONDS", "#ffcc40");
            whiteFlash = Mathf.Max(whiteFlash, 0.4f); Sfx("cheat");
            bangWipeUsed = false;
        }
        // BANG right-click: judgement from above - every enemy in the level dies except one, left for you to finish.
        // The boss (if there is one) is the survivor; otherwise the nearest enemy is. Once per BANG.
        bool bangWipeUsed;
        void BangWipe()
        {
            if (P.dead || bangWipeUsed) { Sfx("deny"); return; }
            var alive = enemies.FindAll(e => !e.dead && e.spawnT <= 0);
            if (alive.Count <= 1) { Sfx("deny"); return; }
            var keep = alive.Find(e => e.type == "boss");
            if (keep == null) { float bd = 1e9f; foreach (var e in alive) { float d = Vector3.Distance(e.pos, P.pos); if (d < bd) { bd = d; keep = e; } } }
            bangWipeUsed = true;
            Sfx("cheat"); Sfx("nova"); whiteFlash = Mathf.Max(whiteFlash, 0.6f); P.shake = Mathf.Max(P.shake, 0.9f); freeze = Mathf.Max(freeze, 0.12f);
            ShockRing(P.pos, 40, 0xffd040, 0.7f); ShockRing(P.pos, 25, 0xff2a2a, 0.5f);
            int n = 0;
            foreach (var e in alive)
            {
                if (e == keep) continue;
                var tp = TargetPoint(e);
                Lightning(tp + V((rand - 0.5f) * 6, 45, (rand - 0.5f) * 6), tp, 0xffd040); Lightning(tp + V(0, 45, 0), tp, 0xff3a2a); // struck from the sky
                DamageEnemy(e, 99999, new Hit { cheat = true, point = tp, dir = V(0, -1, 0), kb = 0.01f });
                n++;
            }
            if (keep != null && keep.type != "boss") { keep.stun = Mathf.Max(keep.stun, 1.2f); Mk.Glow(keep.g.transform, 0xffd040, 3, 0.6f).transform.localPosition = V(0, keep.headY + 1.2f, 0); }
            Banner("JUDGEMENT", $"{n} ERASED · ONE LEFT", "#ffcc40");
            AddStyle("JUDGEMENT", 80 * n);
        }
        void FireBang()
        {
            P.cd = 0.14f; recoil = 1; ShowFlash(4, 60); Sfx("bang"); P.shake = Mathf.Max(P.shake, 0.22f);
            Vector3 o = EyePos, d = AimDir, mz = muzzleBang.position;
            int n = 0;
            PierceShot(o, d, out var end, out var lvl, (e, h) =>
            {
                n++;
                DamageEnemy(e, 1e6f, new Hit { cheat = true, head = h.collider.GetComponent<Part>().head, point = h.point, dir = d, kb = 15 });
                Blast(h.point, 1.6f, 0xff4020);
                return true;
            });
            Tracer(mz, end, 0xffc040, 0.1f, 0.18f); Tracer(mz, end, 0xff2020, 0.04f, 0.25f);
            if (lvl) SpawnParticles(end, 8, 7, sparkMat, 0.1f, 0.4f);
            if (n > 0) { Sfx("snipeHit"); freeze = Mathf.Max(freeze, 0.04f); }
        }
    }
}
