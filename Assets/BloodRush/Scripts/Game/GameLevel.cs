using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BR
{
    public class Theme
    {
        public bool holy; public int bg; public float fogNear, fogFar; public int hemiSky, hemiGround; public float hemiI; public int sunC; public float sunI;
        public string[] floor, wall, block, tower; // bg, line, accent, kind
        public int trim, crystal, crystalEm, portal, portal2; public string pool, ambient, deco, sky, banner; public string[] warp; public float wallH;
    }
    public class ThemeMats { public Material floor, wall, block, tower, trim, metal, bone, banner, pool, crystal; }
    public class LevelDef { public string name, theme, hazard, hazardInfo; public float B; public Vector3 center, spawn; public Vector2 bossSpawn = new Vector2(0, -40); public int boss; public bool lowGrav; public Action build; }

    public partial class Game
    {
        static readonly Dictionary<string, Theme> THEMES = new Dictionary<string, Theme>
        {
            ["hell"] = new Theme { holy = false, bg = 0x1a0606, fogNear = 35, fogFar = 175, hemiSky = 0xffb090, hemiGround = 0x301010, hemiI = 1.6f, sunC = 0xffe0cc, sunI = 2.0f,
                floor = new[] { "#4a3a36", "#2a1e1c", null, null }, wall = new[] { "#5a2222", "#2a0c0c", null, null }, block = new[] { "#6a6670", "#34303a", "#e06020", null }, tower = new[] { "#3e2c2c", "#1c0e0e", "#c03010", null },
                trim = 0xff5a10, pool = "lava", crystal = 0xff2010, crystalEm = 0x990000, portal = 0xff3020, portal2 = 0xffc060, warp = new[] { "#ffe0a0", "#ff2000" },
                ambient = "ember", wallH = 33, deco = "hell", sky = "moon", banner = "#ff3333" },
            ["crucible"] = new Theme { holy = false, bg = 0x2a0700, fogNear = 25, fogFar = 150, hemiSky = 0xff9060, hemiGround = 0x400800, hemiI = 1.7f, sunC = 0xffa060, sunI = 2.2f,
                floor = new[] { "#221616", "#120808", null, "cracks" }, wall = new[] { "#2a1010", "#120404", null, "cracks" }, block = new[] { "#3a2a2a", "#1a0e0e", "#ff5000", null }, tower = new[] { "#1c1010", "#0a0404", "#ff4000", "cracks" },
                trim = 0xff4000, pool = "lava", crystal = 0xff4010, crystalEm = 0xaa1000, portal = 0xff2000, portal2 = 0xffe070, warp = new[] { "#ffffff", "#ffa020" },
                ambient = "ash", wallH = 22, deco = "crucible", sky = "volcano", banner = "#ff6020" },
            ["heaven"] = new Theme { holy = true, bg = 0xdde5ef, fogNear = 45, fogFar = 200, hemiSky = 0xffffff, hemiGround = 0x8a94a8, hemiI = 1.25f, sunC = 0xfff2dc, sunI = 1.9f,
                floor = new[] { "#ece8e0", "#c9b98c", null, "veins" }, wall = new[] { "#dcd6ca", "#b8a878", "#d8a828", "veins" }, block = new[] { "#f2eee6", "#c9b98c", "#d8a828", "veins" }, tower = new[] { "#d0c8b8", "#a89870", "#d8a828", "veins" },
                trim = 0xffc850, pool = "holy", crystal = 0xe0f8ff, crystalEm = 0x3a6a8a, portal = 0x80e8ff, portal2 = 0xffffff, warp = new[] { "#ffffff", "#a0e8ff" },
                ambient = "mote", wallH = 10, deco = "heaven", sky = "sun", banner = "#ffe9a0" },
            ["sanctum"] = new Theme { holy = true, bg = 0xf1e4c4, fogNear = 45, fogFar = 220, hemiSky = 0xfff4e0, hemiGround = 0xa08860, hemiI = 1.3f, sunC = 0xffe6b0, sunI = 2.0f,
                floor = new[] { "#f6f0e0", "#d4af37", null, "veins" }, wall = new[] { "#e6dfcf", "#c8a850", "#e0b040", "veins" }, block = new[] { "#f8f4ea", "#d4af37", "#e0b040", "veins" }, tower = new[] { "#e0d6c0", "#b89850", "#e0b040", "veins" },
                trim = 0xffd060, pool = "holy", crystal = 0xfff0c0, crystalEm = 0x806020, portal = 0xffd890, portal2 = 0xffffff, warp = new[] { "#ffffff", "#ffd070" },
                ambient = "mote", wallH = 6, deco = "sanctum", sky = "halo", banner = "#ffd870" },
        };
        readonly Dictionary<string, ThemeMats> matCache = new Dictionary<string, ThemeMats>();
        static Material T(Material m) { m.name = m.name.Replace("(BR)", "(T)"); return m; } // theme materials outlive levels
        ThemeMats GetThemeMats(string key)
        {
            if (matCache.TryGetValue(key, out var tm)) return tm;
            var th = THEMES[key];
            Material Std(string[] s) { var m = Mats.Std(0xffffff, th.holy ? 0.55f : 0.85f, 0, 0, Tex.Surface(s[0], s[1], s[2], s[3])); return T(m); }
            tm = new ThemeMats
            {
                floor = Std(th.floor), wall = Std(th.wall), block = Std(th.block), tower = Std(th.tower),
                trim = T(Mats.Basic(th.trim)),
                metal = T(Mats.Std(th.holy ? 0xd8a828 : 0x3a3a40, 0.3f, 0.7f, th.holy ? 0x2a1800 : 0)),
                bone = T(th.holy ? Mats.Std(0xf4f0e8, 0.5f) : Mats.Std(0xffffff, 0.62f, 0, 0, Tex.Bone)), // hell bone is textured: fibres, pores, cracks
                banner = T(Mats.Std(th.holy ? 0xf4f0ff : 0x6a0a0a, 0.9f, 0, 0, null, true)),
                pool = T(Mats.Basic(th.pool == "toxic" ? 0x70ff30 : th.pool == "void" ? 0x5a5480 : th.pool == "ice" ? 0x9ad8ff : 0xffffff, th.pool == "lava" || th.pool == "toxic" ? Tex.Lava : Tex.Holy)),
                crystal = T(Mats.Std(th.crystal, 0.25f, 0.3f, th.crystalEm)),
            };
            tm.pool.SetTextureScale("_BaseMap", new Vector2(3, 3));
            if (!th.holy) tm.bone.SetTextureScale("_BaseMap", new Vector2(6, 1.5f));
            return matCache[key] = tm;
        }

        // ---------- level state ----------
        Transform levelRoot; LevelDef LEVEL; Theme THEME; ThemeMats M; int levelNum = 1; float LB = 70;
        readonly List<AABB> colliders = new List<AABB>();
        readonly List<Mesh> levelMeshes = new List<Mesh>();
        class Pad { public float x, z, v; public Material m; }
        class Pool { public float x, z, w, d; }
        List<Pad> pads = new List<Pad>(); List<Pool> pools = new List<Pool>(); List<Action<float, float>> anims = new List<Action<float, float>>(), trapAnims = new List<Action<float, float>>();
        List<Transform> flames = new List<Transform>();
        Transform crystal, crystalRing, crystalGlow; Portal portal;

        // ---------- builders ----------
        GameObject Box(float x, float y, float z, float w, float h, float d, Material mat = null)
        {
            var mesh = Geo.TiledBox(w, h, d); levelMeshes.Add(mesh);
            var g = Mk.Msh(levelRoot, mesh, mat ?? M.block, x, y + h / 2, z);
            g.layer = Mk.LayerLevel; g.AddComponent<BoxCollider>();
            colliders.Add(new AABB(V(x - w / 2, y, z - d / 2), V(x + w / 2, y + h, z + d / 2)));
            return g;
        }
        GameObject Deco(Mesh mesh, Material mat, float x, float y, float z, float rx = 0, float ry = 0, float rz = 0) => Mk.Msh(levelRoot, mesh, mat, x, y, z, rx, ry, rz);
        // invisible solid volume: you collide with it and the grapple can bite it (decor that used to be fly-through)
        GameObject SolidAABB(Vector3 mn, Vector3 mx)
        {
            var g = new GameObject("solid") { layer = Mk.LayerLevel }; g.transform.SetParent(levelRoot, false);
            g.transform.localPosition = (mn + mx) * 0.5f; var bc = g.AddComponent<BoxCollider>(); bc.size = mx - mn;
            colliders.Add(new AABB(mn, mx)); return g;
        }
        // a Geo.Arc / torus arc (XY plane, angles a0..a1, turned by ry) as a chain of solid boxes along the tube
        void SolidArc(Vector3 c, float R, float tube, float ry, int segs, float a0 = 0, float a1 = Mathf.PI)
        {
            var q = Quaternion.Euler(0, ry * Mathf.Rad2Deg, 0); var pad = Vector3.one * tube;
            for (int i = 0; i < segs; i++)
            {
                float u0 = Mathf.Lerp(a0, a1, i / (float)segs), u1 = Mathf.Lerp(a0, a1, (i + 1) / (float)segs);
                var p0 = c + q * V(Mathf.Cos(u0) * R, Mathf.Sin(u0) * R, 0); var p1 = c + q * V(Mathf.Cos(u1) * R, Mathf.Sin(u1) * R, 0);
                SolidAABB(Vector3.Min(p0, p1) - pad, Vector3.Max(p0, p1) + pad);
            }
        }
        // a flat ring (torus lying in XZ at height c.y) as solid boxes
        void SolidRing(Vector3 c, float R, float tube, int segs)
        {
            var pad = Vector3.one * tube;
            for (int i = 0; i < segs; i++)
            {
                float u0 = i * Mathf.PI * 2 / segs, u1 = (i + 1) * Mathf.PI * 2 / segs;
                var p0 = c + V(Mathf.Cos(u0) * R, 0, Mathf.Sin(u0) * R); var p1 = c + V(Mathf.Cos(u1) * R, 0, Mathf.Sin(u1) * R);
                SolidAABB(Vector3.Min(p0, p1) - pad, Vector3.Max(p0, p1) + pad);
            }
        }
        void Column(float x, float z, float r, float h, float y0 = 0)
        {
            var g = Mk.Msh(levelRoot, Geo.Cyl(r, r * 1.08f, h, 12), THEME.holy ? M.bone : M.tower, x, y0 + h / 2, z);
            g.layer = Mk.LayerLevel; g.AddComponent<BoxCollider>();
            colliders.Add(new AABB(V(x - r, y0, z - r), V(x + r, y0 + h, z + r)));
            Deco(Geo.Box(r * 2.6f, 0.6f, r * 2.6f), M.metal, x, y0 + 0.3f, z);
            Deco(Geo.Box(r * 2.8f, 0.8f, r * 2.8f), M.metal, x, y0 + h - 0.4f, z);
        }
        void TrimRect(float cx, float cz, float w, float d, float y)
        {
            Deco(Geo.Box(w, 0.08f, 0.15f), M.trim, cx, y + 0.04f, cz - d / 2); Deco(Geo.Box(w, 0.08f, 0.15f), M.trim, cx, y + 0.04f, cz + d / 2);
            Deco(Geo.Box(0.15f, 0.08f, d), M.trim, cx - w / 2, y + 0.04f, cz); Deco(Geo.Box(0.15f, 0.08f, d), M.trim, cx + w / 2, y + 0.04f, cz);
        }
        void Altar(float cx, float cz, float S, float H)
        {
            Box(cx, 0, cz, S, H, S, M.block); TrimRect(cx, cz, S, S, H);
            for (int k = 1; k * 1.5f < H; k++)
            {
                float h = H - 1.5f * k, off = S / 2 + 2 * (k - 0.5f);
                foreach (var s in new[] { -1, 1 }) { Box(cx, 0, cz + s * off, 6, h, 2, M.block); Box(cx + s * off, 0, cz, 2, h, 6, M.block); }
            }
        }
        void PadAt(float x, float z, float v)
        {
            // themed launch pads: a hellish rune plate with a molten core and horns, or a marble seal with gold, wings and a halo
            bool holy = THEME.holy, sanct = THEME.deco == "sanctum";
            int col = holy ? (sanct ? 0xffd070 : 0x70d8ff) : THEME.deco switch { "crucible" => 0xff7a10, "frost" => 0x60d0ff, "ruin" => 0xff9a20, "limbo" => 0xe8c060, _ => 0xff4a18 };
            var glowM = Mats.Add(col, 0.9f, null, true, true); var mat = Mats.Std(col, 1, 0, col);
            bool frost = THEME.deco == "frost";
            if (frost)
            {
                // the Frozen Circle: a frost-rune seal of dark iron with a glowing ice core and ice spurs instead of horns
                var iron = Mats.Std(0x262c34, 0.45f, 0.7f); var ice = Mats.Std(0xc8ecff, 0.1f, 0.1f, 0x0a3048);
                Deco(Geo.Cyl(1.55f, 1.75f, 0.32f, 8), iron, x, 0.16f, z);
                Deco(Geo.Cyl(1.0f, 1.0f, 0.06f, 20), Mats.Add(0x9ae8ff, 0.95f, Tex.Swirl, true, true), x, 0.33f, z);
                Deco(Geo.Cyl(1.0f, 1.0f, 0.05f, 20), Mats.Std(0x60b8e8, 0.05f, 0.2f, 0x1a6090), x, 0.31f, z);
                Deco(Geo.Torus(1.22f, 0.07f, 32, 4), glowM, x, 0.34f, z, Mathf.PI / 2);
                for (int k = 0; k < 6; k++)
                {
                    float a = k * Mathf.PI / 3;
                    Deco(Geo.Cone(0.16f, 0.7f + (k % 2) * 0.3f, 5), ice, x + Mathf.Cos(a) * 1.5f, 0.6f, z + Mathf.Sin(a) * 1.5f, Mathf.Sin(a) * 0.3f, 0, -Mathf.Cos(a) * 0.3f);
                    Deco(Geo.Box(0.5f, 0.03f, 0.08f), glowM, x + Mathf.Cos(a) * 0.62f, 0.37f, z + Mathf.Sin(a) * 0.62f, 0, -a, 0);
                }
            }
            else if (!holy)
            {
                var iron = Mats.Std(0x241e20, 0.45f, 0.7f);
                Deco(Geo.Cyl(1.55f, 1.75f, 0.32f, 8), iron, x, 0.16f, z);
                Deco(Geo.Cyl(1.0f, 1.0f, 0.06f, 20), Mats.Basic(0xffffff, Tex.Lava), x, 0.33f, z);
                Deco(Geo.Torus(1.22f, 0.07f, 32, 4), glowM, x, 0.34f, z, Mathf.PI / 2);
                for (int k = 0; k < 6; k++)
                {
                    float a = k * Mathf.PI / 3;
                    Deco(Geo.Cone(0.13f, 0.55f, 5), M.bone, x + Mathf.Cos(a) * 1.5f, 0.55f, z + Mathf.Sin(a) * 1.5f, Mathf.Sin(a) * 0.35f, 0, -Mathf.Cos(a) * 0.35f);
                    Deco(Geo.Box(0.5f, 0.03f, 0.08f), glowM, x + Mathf.Cos(a) * 0.62f, 0.37f, z + Mathf.Sin(a) * 0.62f, 0, -a, 0);
                }
            }
            else
            {
                var marble = Mats.Std(0xf6f2ea, 0.45f); var gold = Mats.Std(0xe0b040, 0.3f, 0.85f);
                Deco(Geo.Cyl(1.5f, 1.7f, 0.3f, 24), marble, x, 0.15f, z);
                Deco(Geo.Torus(1.5f, 0.07f, 32, 4), gold, x, 0.3f, z, Mathf.PI / 2);
                Deco(Geo.Cyl(1.1f, 1.1f, 0.05f, 24), glowM, x, 0.32f, z);
                foreach (var sx in new[] { -1f, 1f }) for (int f = 0; f < 3; f++)                       // little wings on the rim
                    Deco(Geo.Box(0.12f, 0.06f, 0.75f - f * 0.15f), marble, x + sx * (1.55f + f * 0.12f), 0.42f + f * 0.12f, z, 0, 0, sx * (0.5f + f * 0.25f));
            }
            var halo = Deco(Geo.Torus(0.85f, 0.04f, 28, 4), glowM, x, 1.0f, z, Mathf.PI / 2); halo.layer = Mk.LayerIgnore;
            var gl = Mk.Glow(levelRoot, col, 3.5f, 0.5f); gl.transform.localPosition = V(x, 0.7f, z);
            float ph = x * 0.3f + z * 0.2f;
            anims.Add((t, dt) =>
            {
                float p = 0.5f + 0.5f * Mathf.Sin(t * 4 + ph), u = Mathf.Repeat(t * 0.9f + ph, 1.6f);
                halo.transform.localPosition = V(x, 0.6f + u, z); halo.transform.localScale = Vector3.one * (1 - u / 2.2f);
                Mats.SetAlpha(glowM, 0.55f + 0.4f * p);
                if (rand < 0.25f) SpawnParticles(V(x + (rand - 0.5f) * 2, 0.4f, z + (rand - 0.5f) * 2), 1, 1.5f, holy ? moteMat : frost ? SnowMat : emberMat, 0.07f, 1.0f, -6);
            });
            pads.Add(new Pad { x = x, z = z, v = v, m = mat });
        }
        void PoolAt(float x, float z, float w, float d)
        {
            Deco(Geo.Plane(w, d), M.pool, x, 0.03f, z, -Mathf.PI / 2);
            foreach (var s in new[] { -1, 1 }) { Deco(Geo.Box(w + 1, 0.18f, 0.5f), M.metal, x, 0.09f, z + s * (d / 2 + 0.25f)); Deco(Geo.Box(0.5f, 0.18f, d), M.metal, x + s * (w / 2 + 0.25f), 0.09f, z); }
            var gl = Mk.Glow(levelRoot, THEME.pool == "lava" ? 0xff4000 : THEME.pool == "toxic" ? 0x60ff30 : THEME.pool == "void" ? 0x9080ff : 0x80e0ff, Mathf.Max(w, d) * 1.1f, 0.35f); gl.transform.localPosition = V(x, 1.5f, z);
            pools.Add(new Pool { x = x, z = z, w = w, d = d });
        }
        bool InPool(float x, float z, float m = 0) { foreach (var l in pools) if (Mathf.Abs(x - l.x) < l.w / 2 + m && Mathf.Abs(z - l.z) < l.d / 2 + m) return true; return false; }
        void OnWalls(float Bd, float step, Action<float, float, float, float> fn, float offset = 0)
        {
            for (float p = -Bd + step / 2 + offset; p <= Bd - step / 2 + 1e-4f; p += step) { fn(p, -Bd, 0, 1); fn(p, Bd, 0, -1); fn(-Bd, p, 1, 0); fn(Bd, p, -1, 0); }
        }
        Transform WallG(float px, float pz, float nx, float nz)
        {
            var n = Mk.Piv(levelRoot, px, 0, pz); n.SetRot(0, Mathf.Atan2(nx, nz), 0); return n.t;
        }
        void Arena(float Bd, float wallH = -1)
        {
            if (wallH < 0) wallH = THEME.wallH;
            Box(0, -1, 0, Bd * 2 + 4, 1, Bd * 2 + 4, M.floor);
            Box(0, -1, -(Bd + 1), Bd * 2 + 4, wallH + 1, 2, M.wall); Box(0, -1, Bd + 1, Bd * 2 + 4, wallH + 1, 2, M.wall);
            Box(-(Bd + 1), -1, 0, 2, wallH + 1, Bd * 2 + 4, M.wall); Box(Bd + 1, -1, 0, 2, wallH + 1, Bd * 2 + 4, M.wall);
            string d = THEME.deco;
            if (d == "hell")
            {
                OnWalls(Bd, 20, (px, pz, nx, nz) =>
                {
                    var g = WallG(px, pz, nx, nz);
                    Mk.Msh(g, Geo.Box(0.3f, 0.8f, 0.5f), M.metal, 0, 7, 0.25f); Mk.Msh(g, Geo.Box(0.25f, 0.25f, 0.25f), M.trim, 0, 7.55f, 0.4f);
                    var f = Mk.Glow(g, 0xff7020, 2.4f, 0.9f); f.transform.localPosition = V(0, 7.9f, 0.45f); flames.Add(f.transform);
                });
                OnWalls(Bd, 20, (px, pz, nx, nz) =>
                {
                    var g = WallG(px, pz, nx, nz);
                    Mk.Msh(g, Geo.Box(3.2f, 9, 0.08f), M.banner, 0, 16, 0.1f);
                    Mk.Msh(g, Geo.Box(3.5f, 0.25f, 0.14f), M.metal, 0, 20.6f, 0.12f); Mk.Msh(g, Geo.Box(3.5f, 0.25f, 0.14f), M.metal, 0, 11.4f, 0.12f);
                    Mk.Msh(g, Geo.Box(0.9f, 0.9f, 0.06f), M.trim, 0, 16.5f, 0.16f, 0, 0, Mathf.PI / 4);
                }, 10);
            }
            else if (d == "crucible")
            {
                OnWalls(Bd, 24, (px, pz, nx, nz) =>
                {
                    var g = WallG(px, pz, nx, nz);
                    Mk.Msh(g, Geo.Plane(3, wallH), M.pool, 0, wallH / 2 - 0.5f, 0.06f);
                    var f = Mk.Glow(g, 0xff5000, 6, 0.6f); f.transform.localPosition = V(0, 1.2f, 1); flames.Add(f.transform);
                });
                OnWalls(Bd, 8, (px, pz, nx, nz) => { var g = WallG(px, pz, nx, nz); Mk.Msh(g, Geo.Sphere(0.7f, 8, 6), M.bone, 0, wallH - 0.2f, -0.6f); Mk.Msh(g, Geo.Box(0.3f, 0.12f, 0.05f), M.trim, 0, wallH - 0.2f, 0.1f); });
            }
            else if (!ActWalls(d, Bd, wallH)) // the act themes dress their own walls (GameActs)
            {
                float step = d == "sanctum" ? 12 : 16;
                OnWalls(Bd, step, (px, pz, nx, nz) =>
                {
                    var g = WallG(px, pz, nx, nz);
                    Mk.Msh(g, Geo.Cone(0.45f, 1.6f, 6), M.metal, 0, wallH + 0.8f, -1);
                    Mk.Msh(g, Geo.Box(0.35f, wallH, 0.06f), M.trim, 0, wallH / 2, 0.04f);
                    var f = Mk.Glow(g, 0xfff0c0, 2.6f, 0.7f); f.transform.localPosition = V(0, wallH + 2.2f, -1); flames.Add(f.transform);
                    if (wallH >= 20)
                    {
                        Mk.Msh(g, Geo.Plane(3.4f, 9), stainedMat, step / 2, 14, 0.07f); Mk.Msh(g, Geo.Box(3.8f, 0.3f, 0.12f), M.metal, step / 2, 18.6f, 0.08f);
                        // god ray streaming in through the window, angled down toward the middle
                        if (((int)Mathf.Abs(px + pz) / 16) % 2 == 0)
                        {
                            var src = g.TransformPoint(V(step / 2, 14.5f, 0.4f)); var inward = g.forward;
                            LightShaft(src, src + inward * 24 + V(0, -14.5f, 0) + g.right * 3, 1.5f, 5.5f, 0xffe8c0, 0.16f);
                        }
                    }
                });
                if (wallH < 20) SunRaysOverWalls(Bd, wallH, 7); // no windows: the sun pours in over the walls
            }
            BuildSky(THEME.sky, Bd);
            BuildBackdrop(THEME.sky, Bd);
        }
        void BuildSky(string kind, float Bd)
        {
            if (ActSky(kind, Bd)) return;
            if (kind == "moon" || kind == "volcano")
            {
                var moon = Mk.Glow(levelRoot, kind == "volcano" ? 0xff6020 : 0xff3a2a, 90, 0.9f, false); moon.transform.localPosition = V(60, 110, -230);
                var core = Mk.Glow(levelRoot, 0xffc0a0, 30, 0.9f, false); core.transform.localPosition = V(60, 110, -230);
                if (kind == "volcano") for (int i = 0; i < 6; i++) { float a = i / 6f * Mathf.PI * 2; var g = Mk.Glow(levelRoot, 0xff4000, 120, 0.5f, false); g.transform.localPosition = V(Mathf.Cos(a) * 200, 5, Mathf.Sin(a) * 200); }
            }
            else
            {
                var s = Mk.Glow(levelRoot, 0xfff6e0, 70, 1, false); s.transform.localPosition = V(-90, 130, -210);
                var s2 = Mk.Glow(levelRoot, 0xffffff, 26, 1, false); s2.transform.localPosition = V(-90, 130, -210);
                for (int i = 0; i < 34; i++)
                {
                    float a = rand * Mathf.PI * 2, r = Bd + 25 + rand * 80;
                    var c = Mk.Glow(levelRoot, 0xffffff, 40 + rand * 50, 0.55f, false); c.transform.localPosition = V(Mathf.Cos(a) * r, -12 + rand * 20, Mathf.Sin(a) * r);
                }
                for (int i = 0; i < 5; i++)
                {
                    var sh = Mk.Msh(levelRoot, Geo.Cyl(3, 3, 160, 12), Mats.Add(0xfff0c8, 0.07f, null, true), -40 + i * 22, 60, -20 + (i % 2) * 30, 0.25f, 0, 0.35f);
                    sh.layer = Mk.LayerIgnore;
                }
                if (kind == "halo")
                {
                    var h1 = new N(Deco(Geo.Torus(70, 2.2f, 96), M.metal, 0, 95, 0, Mathf.PI / 2).transform); h1.SetRot(Mathf.PI / 2, 0, 0);
                    var h2 = new N(Deco(Geo.Torus(58, 0.8f, 96), M.trim, 0, 95, 0, Mathf.PI / 2).transform); h2.SetRot(Mathf.PI / 2, 0, 0);
                    var hg = Mk.Glow(levelRoot, 0xffe0a0, 120, 0.25f, false); hg.transform.localPosition = V(0, 95, 0);
                    anims.Add((t, dt) => { h1.rz += dt * 0.05f; h2.rz -= dt * 0.08f; });
                }
            }
        }
        void ApplyTheme(Theme th)
        {
            var bg = Mats.Hex(th.bg);
            cam.backgroundColor = bg;
            RenderSettings.fogColor = bg; RenderSettings.fogStartDistance = th.fogNear; RenderSettings.fogEndDistance = th.fogFar;
            // three.js lights are physically based (/PI); Unity's are not
            float hi = th.hemiI / Mathf.PI;
            Color sky = Mats.Hex(th.hemiSky) * hi, ground = Mats.Hex(th.hemiGround) * hi;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky; RenderSettings.ambientEquatorColor = (sky + ground) * 0.5f; RenderSettings.ambientGroundColor = ground;
            DynamicGI.UpdateEnvironment();
            sunLight.color = Mats.Hex(th.sunC); sunLight.intensity = th.sunI / Mathf.PI;
        }
        void MakeCrystal(Vector3 c)
        {
            crystal = Mk.Msh(levelRoot, Geo.Octahedron(2.2f), M.crystal, c.x, c.y + 7, c.z).transform;
            crystalRing = Deco(Geo.Torus(3.6f, 0.08f, 40), M.metal, c.x, c.y + 7, c.z).transform;
            crystalGlow = Mk.Glow(levelRoot, THEME.crystal, 10, 0.55f).transform; crystalGlow.localPosition = V(c.x, c.y + 7, c.z);
            crystalRingN = new N(crystalRing); crystalN = new N(crystal);
        }
        N crystalRingN, crystalN;

        // ---------- the ten levels ----------
        const float HVN = 0.85f; static float Hv(float v) => v * HVN; // heaven footprint scale
        List<LevelDef> LEVELS;
        void DefineLevels()
        {
            AddActThemes(); AddFrostThemes();
            LEVELS = new List<LevelDef>
            {
                new LevelDef { name = "THE PIT", theme = "hell", hazard = "LAVA GEYSERS", hazardInfo = "vents flash red, then erupt - they burn, but ride one to fly", B = 70, center = V(0, 4, 0), spawn = V(6, 0, 46), build = () => {
                    Arena(70);
                    Altar(0, 0, 18, 4);
                    foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 }) Box(52 * sx, 0, 52 * sz, 12, 10, 12, M.tower);
                    Box(0, 9, -52, 92, 1, 5); Box(0, 9, 52, 92, 1, 5); Box(-52, 9, 0, 5, 1, 92); Box(52, 9, 0, 5, 1, 92);
                    Box(0, 10, -54.25f, 92, 0.6f, 0.5f, M.wall); Box(0, 10, 54.25f, 92, 0.6f, 0.5f, M.wall);
                    Box(-54.25f, 10, 0, 0.5f, 0.6f, 92, M.wall); Box(54.25f, 10, 0, 0.5f, 0.6f, 92, M.wall);
                    foreach (var s in new[] { -1, 1 }) { Deco(Geo.Box(92, 0.08f, 0.15f), M.trim, 0, 10.04f, s * 49.6f); Deco(Geo.Box(0.15f, 0.08f, 92), M.trim, s * 49.6f, 10.04f, 0); }
                    foreach (var t in new[] { -23, 0, 23 }) { Box(t, 0, -52, 1.5f, 9, 1.5f, M.tower); Box(t, 0, 52, 1.5f, 9, 1.5f, M.tower); Box(-52, 0, t, 1.5f, 9, 1.5f, M.tower); Box(52, 0, t, 1.5f, 9, 1.5f, M.tower); }
                    for (int i = 0; i < 6; i++) { Box(-47.5f, 0, 21 - i * 2, 4, 1.5f * (i + 1), 2); Box(47.5f, 0, -21 + i * 2, 4, 1.5f * (i + 1), 2); }
                    foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 }) Box(28 * sx, 0, 28 * sz, 3, 18, 3, M.wall);
                    Box(-26, 0, 0, 2, 5, 14); Box(26, 0, 0, 2, 5, 14); Box(0, 0, -26, 14, 5, 2); Box(0, 0, 26, 14, 5, 2);
                    Box(-38, 6, 0, 8, 1, 8); Box(38, 6, 0, 8, 1, 8); Box(0, 7, -30, 8, 1, 8); Box(0, 7, 30, 8, 1, 8);
                    PoolAt(-38, 0, 12, 14); PoolAt(38, 0, 12, 14);
                    PadAt(-40, -40, 27); PadAt(40, -40, 27); PadAt(-40, 40, 27); PadAt(40, 40, 27); PadAt(0, -40, 22); PadAt(0, 40, 22);
                    MechGeysers(new[] { V(-31, 0, 14), V(31, 0, -14), V(20, 0, 40), V(-20, 0, -40) }); // open floor, nothing overhead
                } },
                new LevelDef { name = "BONE CORRIDORS", theme = "hell", hazard = "SPIKE TRAPS", hazardInfo = "plates flash red before bones punch up - lure enemies onto them", B = 62, center = V(0, 3, 0), spawn = V(0, 0, 54), build = () => {
                    Arena(62);
                    Altar(0, 0, 12, 3);
                    foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 }) { Box(sx * 24, 0, sz * 16, 2, 6, 14, M.wall); Box(sx * 17, 0, sz * 24, 16, 6, 2, M.wall); }
                    foreach (var p in new[] { V(40, 0, 15), V(-40, 0, 15), V(40, 0, -15), V(-40, 0, -15), V(15, 0, 40), V(-15, 0, 40), V(15, 0, -40), V(-15, 0, -40) }) { Box(p.x, 0, p.z, 3, 14, 3, M.tower); Deco(Geo.Cone(0.5f, 2, 5), M.bone, p.x, 15, p.z); }
                    foreach (var s in new[] { -1, 1 }) { Box(s * 59, 0, 0, 6, 8, 60, M.tower); Deco(Geo.Box(0.15f, 0.08f, 60), M.trim, s * 55.9f, 8.04f, 0); }
                    PadAt(50, 20, 24); PadAt(-50, 20, 24); PadAt(50, -20, 24); PadAt(-50, -20, 24);
                    MechSpikes(new[] { (40f, 0f, 8f, 10f), (-40f, 0f, 8f, 10f), (0f, 32f, 8f, 8f), (0f, -32f, 8f, 8f), (16f, 0f, 6f, 6f), (-16f, 0f, 6f, 6f) });
                    foreach (var s in new[] { -1, 1 }) { PoolAt(0, s * 44, 50, 7); foreach (var x in new[] { -20, -10, 0, 10, 20 }) BoneArc(V(x, 0, s * 44), 5, 0.38f, Mathf.PI / 2, 10); }
                } },
                new LevelDef { name = "FURNACE TERRACES", theme = "hell", hazard = "FLAME SWEEP", hazardInfo = "a fire arm circles the altar - jump it", B = 64, center = V(0, 4, 0), spawn = V(0, 2.2f, 54), build = () => {
                    Arena(64);
                    Altar(0, 0, 14, 4);
                    PoolAt(0, -15, 36, 6); PoolAt(0, 15, 36, 6); PoolAt(-15, 0, 6, 24); PoolAt(15, 0, 6, 24);
                    Box(0, 0, -15, 5, 0.5f, 6.4f); Box(0, 0, 15, 5, 0.5f, 6.4f); Box(-15, 0, 0, 6.4f, 0.5f, 5); Box(15, 0, 0, 6.4f, 0.5f, 5);
                    Box(0, 0, -52, 128, 3, 24, M.tower); Box(0, 3, -58, 128, 3, 12, M.tower);
                    Deco(Geo.Box(128, 0.08f, 0.15f), M.trim, 0, 3.04f, -39.9f); Deco(Geo.Box(128, 0.08f, 0.15f), M.trim, 0, 6.04f, -51.9f);
                    foreach (var s in new[] { -1, 1 }) { Box(s * 30, 0, -38.5f, 8, 1.5f, 3); Box(s * 30, 3, -50.5f, 8, 1.5f, 3); Box(s * 20, 0, 42.5f, 8, 1, 3); }
                    Box(0, 0, 54, 128, 2, 20, M.tower); Deco(Geo.Box(128, 0.08f, 0.15f), M.trim, 0, 2.04f, 43.9f);
                    foreach (var p in new[] { V(34, 0, 20), V(-34, 0, 20), V(34, 0, -20), V(-34, 0, -20) }) Box(p.x, 0, p.z, 3, 16, 3, M.wall);
                    Box(40, 7, 0, 8, 1, 8); Box(-40, 7, 0, 8, 1, 8);
                    PadAt(33, 10, 22); PadAt(-33, 10, 22); PadAt(33, -10, 22); PadAt(-33, -10, 22);
                    PoolAt(50, 0, 8, 10); PoolAt(-50, 0, 8, 10);
                    MechFlameSweep(11, 31, 0.55f);
                } },
                new LevelDef { name = "CHAIN TOWERS", theme = "hell", hazard = "WRECKING BALLS", hazardInfo = "spiked balls swing between the towers - they smash enemies too", B = 60, center = V(0, 3, 0), spawn = V(0, 0, 46), build = () => {
                    Arena(60);
                    Altar(0, 0, 10, 3);
                    for (int k = 0; k < 8; k++)
                    {
                        float a = k * Mathf.PI / 4, x = Mathf.Cos(a) * 30, z = Mathf.Sin(a) * 30, h = k % 2 == 1 ? 13 : 8;
                        Box(x, 0, z, 6, h, 6, M.tower); TrimRect(x, z, 6, 6, h);
                        if (k % 2 == 0) PadAt(Mathf.Cos(a) * 21, Mathf.Sin(a) * 21, 24);
                        float b = a + Mathf.PI / 8; Box(Mathf.Cos(b) * 30, 9, Mathf.Sin(b) * 30, 5, 1, 5);
                    }
                    PoolAt(45, 45, 14, 14); PoolAt(-45, 45, 14, 14); PoolAt(45, -45, 14, 14); PoolAt(-45, -45, 14, 14);
                    MechWreckingBalls(4, 17, 44, 42, 0.5f);
                } },
                new LevelDef { name = "THE CRUCIBLE", theme = "crucible", hazard = "METEOR RAIN + ERUPTIONS", hazardInfo = "rocks fall where red circles grow; towers flare, then fire rings roll out - jump them", B = 72, boss = 1, center = V(0, 1.5f, 0), spawn = V(0, 0, 48), bossSpawn = new Vector2(0, -40), build = () => {
                    Arena(72);
                    PoolAt(0, -67, 144, 10); PoolAt(0, 67, 144, 10); PoolAt(-67, 0, 10, 124); PoolAt(67, 0, 10, 124);
                    foreach (var p in new[] { V(30, 0, 30), V(-30, 0, 30), V(30, 0, -30), V(-30, 0, -30), V(44, 0, 0), V(-44, 0, 0) })
                    {
                        Box(p.x, 0, p.z, 5, 22, 5, M.tower);
                        Deco(Geo.Box(0.1f, 18, 0.35f), M.trim, p.x + 2.55f, 11, p.z); Deco(Geo.Box(0.1f, 18, 0.35f), M.trim, p.x - 2.55f, 11, p.z);
                        Deco(Geo.Box(0.35f, 18, 0.1f), M.trim, p.x, 11, p.z + 2.55f); Deco(Geo.Box(0.35f, 18, 0.1f), M.trim, p.x, 11, p.z - 2.55f);
                        Deco(Geo.Cone(1.2f, 4, 5), M.bone, p.x, 24, p.z);
                    }
                    // the four cover walls: in boss phase 2 they burst apart into floating rocks (see CrucibleShatter)
                    crucibleWalls.Add(Box(18, 0, 18, 8, 3, 2)); crucibleWalls.Add(Box(-18, 0, -18, 8, 3, 2)); crucibleWalls.Add(Box(18, 0, -18, 2, 3, 8)); crucibleWalls.Add(Box(-18, 0, 18, 2, 3, 8));
                    Altar(0, 0, 16, 1.5f);
                    foreach (var z in new[] { -45, -25, -5, 15, 35 }) BoneArc(V(0, 0, z), 52, 1.3f, 0, 40);
                    Vertebrae(0, 52, -54, 44, 1); SolidAABB(V(-3, 50, -55), V(3, 54, 45));
                    MechMeteors();
                    var towers = new[] { V(30, 0, 30), V(-30, 0, 30), V(30, 0, -30), V(-30, 0, -30), V(44, 0, 0), V(-44, 0, 0) };
                    MechEruptions(towers, 22); CrucibleDressing(towers, 22);
                } },
                // heaven levels are laid out at HVN (85%) of their original footprint - same layout, heights unchanged
                new LevelDef { name = "GATES OF DAWN", theme = "heaven", hazard = "SENTINEL STATUES", hazardInfo = "the angel statues lock on - a beam traces you, then it fires; break line of sight behind a column", B = Hv(64), center = V(0, 3, 0), spawn = V(0, 0, Hv(48)), build = () => {
                    Arena(Hv(64));
                    Altar(0, 0, Hv(12), 3);
                    foreach (var s in new[] { -1, 1 }) { for (int z = -40; z <= 40; z += 10) Column(s * Hv(22), Hv(z), 1.2f, 14); Deco(Geo.Box(2.6f, 1, Hv(84)), M.metal, s * Hv(22), 14.5f, 0); SolidAABB(V(s * Hv(22) - 1.3f, 14, -Hv(42)), V(s * Hv(22) + 1.3f, 15, Hv(42))); }
                    Box(Hv(12), 0, Hv(-56), 6, 26, 6, M.tower); Box(-Hv(12), 0, Hv(-56), 6, 26, 6, M.tower);
                    Deco(Geo.Arc(Hv(12), 1, 32), M.metal, 0, 20, Hv(-56)); SolidArc(V(0, 20, Hv(-56)), Hv(12), 1, 0, 20);
                    var gg = Mk.Glow(levelRoot, 0xfff4d0, 30, 0.5f); gg.transform.localPosition = V(0, 18, Hv(-58));
                    LightShaft(V(0, 21, Hv(-60)), V(0, 0, Hv(-24)), 4, 10, 0xfff4d0, 0.2f); LightShaft(V(-6, 19, Hv(-60)), V(-Hv(10), 0, Hv(-30)), 1.8f, 5, 0xfff0c0, 0.12f); LightShaft(V(6, 19, Hv(-60)), V(Hv(10), 0, Hv(-30)), 1.8f, 5, 0xfff0c0, 0.12f); // light through the gate
                    foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 }) { Box(sx * Hv(40), 6, sz * Hv(25), 8, 1, 8); TrimRect(sx * Hv(40), sz * Hv(25), 8, 8, 7); PadAt(sx * Hv(40), sz * Hv(12), 22); }
                    PoolAt(Hv(44), 0, 10, 12); PoolAt(-Hv(44), 0, 10, 12);
                    GatesOfDawnDressing();
                } },
                new LevelDef { name = "CLOISTER OF ECHOES", theme = "heaven", hazard = "THE GREAT BELL", hazardInfo = "every toll sends a shockwave rolling out - jump it", B = Hv(58), center = V(0, 3, 0), spawn = V(0, 0, Hv(37)), build = () => {
                    Arena(Hv(58), 12);
                    Altar(0, 0, Hv(10), 3);
                    foreach (var s in new[] { -1, 1 })
                    {
                        Box(Hv(17), 0, s * Hv(26), Hv(24), 5, Hv(6)); Box(-Hv(17), 0, s * Hv(26), Hv(24), 5, Hv(6));
                        Box(s * Hv(26), 0, Hv(17), Hv(6), 5, Hv(20)); Box(s * Hv(26), 0, -Hv(17), Hv(6), 5, Hv(20));
                        Box(0, 12, s * Hv(26), Hv(58), 0.6f, Hv(7)); Box(s * Hv(26), 12, 0, Hv(7), 0.6f, Hv(44));
                        foreach (var p in new[] { 8, 16, 24 }) foreach (var q in new[] { -1, 1 }) { Column(q * Hv(p), s * Hv(26), 0.7f, 7, 5); Column(s * Hv(26), q * Hv(p), 0.7f, 7, 5); }
                        TrimRect(0, 0, Hv(58), Hv(58), 12.6f);
                    }
                    foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 }) { PadAt(sx * Hv(14), sz * Hv(14), 22); PadAt(sx * Hv(36), sz * Hv(36), 30); }
                    PoolAt(Hv(44), 0, 10, 12); PoolAt(-Hv(44), 0, 10, 12);
                    Column(0, Hv(44), 2, 18); Column(0, -Hv(44), 2, 18);
                    foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 }) Column(sx * Hv(46), sz * Hv(46), 2, 18);
                    MechBell(22);
                    CloisterDressing();
                } },
                new LevelDef { name = "SKY BRIDGES", theme = "heaven", hazard = "STORM CLOUDS", hazardInfo = "clouds drift after you and drop lightning where the blue circle glows", B = Hv(66), center = V(0, 7, 0), spawn = V(0, 8, Hv(42)), build = () => {
                    Arena(Hv(66));
                    Altar(0, 0, Hv(16), 7);
                    foreach (var p in new[] { V(Hv(40), 0, 0), V(-Hv(40), 0, 0), V(0, 0, Hv(40)), V(0, 0, -Hv(40)) })
                    {
                        Box(p.x, 0, p.z, Hv(14), 8, Hv(14), M.tower); TrimRect(p.x, p.z, Hv(14), Hv(14), 8);
                        foreach (var a in new[] { -Hv(5.5f), Hv(5.5f) }) foreach (var b in new[] { -Hv(5.5f), Hv(5.5f) }) Column(p.x + a, p.z + b, 0.6f, 6, 8);
                    }
                    Box(Hv(20.5f), 7, Hv(5.5f), Hv(25), 1, 4); Box(-Hv(20.5f), 7, -Hv(5.5f), Hv(25), 1, 4); Box(-Hv(5.5f), 7, Hv(20.5f), 4, 1, Hv(25)); Box(Hv(5.5f), 7, -Hv(20.5f), 4, 1, Hv(25));
                    foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 }) { PoolAt(sx * Hv(40), sz * Hv(40), Hv(22), Hv(22)); PadAt(sx * Hv(22), sz * Hv(22), 24); }
                    MechStormClouds(3);
                } },
                new LevelDef { name = "THE CHOIR", theme = "heaven", hazard = "SPEAR TIDE", hazardInfo = "walls of light spears sweep down the hall - get into the gap before the wall reaches you", B = Hv(62), center = V(0, 3, 0), spawn = V(0, 0, Hv(52)), build = () => {
                    Arena(Hv(62), 24);
                    Altar(0, 0, Hv(12), 3);
                    foreach (var s in new[] { -1, 1 })
                    {
                        for (int z = -48; z <= 48; z += 12) { Column(s * Hv(16), Hv(z), 1.6f, 20); if (z < 48) { Deco(Geo.Arc(Hv(6), 0.5f, 16), M.metal, s * Hv(16), 20, Hv(z + 6), 0, Mathf.PI / 2, 0); SolidArc(V(s * Hv(16), 20, Hv(z + 6)), Hv(6), 0.5f, Mathf.PI / 2, 8); } }
                        Box(s * Hv(55), 0, 0, Hv(14), 7, Hv(90), M.tower); Box(s * (Hv(55) - Hv(7) + 0.25f), 7, 0, 0.5f, 0.8f, Hv(90), M.metal);
                        for (int k = 1; k <= 4; k++) { Box(s * Hv(52), 0, Hv(45 + 2 * k - 1), Hv(8), 7 - 1.5f * k, Hv(2)); Box(s * Hv(52), 0, -Hv(45 + 2 * k - 1), Hv(8), 7 - 1.5f * k, Hv(2)); }
                        foreach (var z in new[] { 14, 19, 24, 29, 34, 39 }) { Box(s * Hv(7), 0, Hv(z), Hv(8), 1, 1.4f, M.tower); Box(s * Hv(7), 0, -Hv(z), Hv(8), 1, 1.4f, M.tower); }
                    }
                    for (int i = 0; i < 15; i++) { float h = 18 - Mathf.Abs(7 - i), px = Hv(-14 + i * 2), pz = -Hv(62) + 0.6f; Deco(Geo.Cyl(0.4f, 0.4f, h, 8), M.metal, px, h / 2 + 1, pz); SolidAABB(V(px - 0.45f, 1, pz - 0.45f), V(px + 0.45f, h + 1, pz + 0.45f)); }
                    Deco(Geo.Circle(7, 24), stainedMat, 0, 15, Hv(62) - 0.1f, 0, Mathf.PI, 0);
                    var rg = Mk.Glow(levelRoot, 0xfff0d0, 22, 0.35f); rg.transform.localPosition = V(0, 15, Hv(62) - 2);
                    MechSpearTide();
                } },
                new LevelDef { name = "THE SANCTUM", theme = "sanctum", hazard = "HALO LANCES + JUDGEMENT", hazardInfo = "light beams sweep the floor; golden sigils lock on where you're headed - dodge before they smite", B = Hv(74), boss = 2, center = V(0, 4.5f, 0), spawn = V(0, 0, Hv(58)), bossSpawn = new Vector2(0, -Hv(40)), build = () => {
                    Arena(Hv(74));
                    Altar(0, 0, Hv(20), 4.5f);
                    for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6 + Mathf.PI / 12; Column(Mathf.Cos(a) * Hv(50), Mathf.Sin(a) * Hv(50), 2, 28); }
                    var ring = new N(Deco(Geo.Torus(Hv(50), 0.6f, 96), M.metal, 0, 30, 0, Mathf.PI / 2).transform); ring.SetRot(Mathf.PI / 2, 0, 0);
                    anims.Add((t, dt) => ring.rz += dt * 0.04f);
                    SolidRing(V(0, 30, 0), Hv(50), 0.7f, 48); // it only spins about its own axis, so its shape stays put
                    foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 }) { Box(sx * Hv(30), 9, sz * Hv(30), 9, 1, 9); TrimRect(sx * Hv(30), sz * Hv(30), 9, 9, 10); PadAt(sx * Hv(30), sz * Hv(19), 26); }
                    Box(0, 0, Hv(28), Hv(10), 3, 2); Box(0, 0, -Hv(28), Hv(10), 3, 2); Box(Hv(28), 0, 0, 2, 3, Hv(10)); Box(-Hv(28), 0, 0, 2, 3, Hv(10));
                    MechLances(3); MechSigils(); SanctumDressing(12, Hv(50), 28); SanctumStatues();
                } },
            };
            LEVELS.InsertRange(5, ActLevels()); // Dead Earth and Purgatory sit between Hell and Heaven (11-20)...
            LEVELS.InsertRange(5, FrostLevels()); // ...after the Frozen Circle (6-10, GameFrost)
        }

        void ClearLevel()
        {
            if (levelRoot)
            {
                foreach (var r in levelRoot.GetComponentsInChildren<Renderer>(true))
                    if (r.sharedMaterial && r.sharedMaterial.name.EndsWith("(BR)")) Destroy(r.sharedMaterial);
                Destroy(levelRoot.gameObject);
            }
            foreach (var m in levelMeshes) Destroy(m);
            levelMeshes.Clear(); colliders.Clear();
            pads = new List<Pad>(); pools = new List<Pool>(); anims = new List<Action<float, float>>(); trapAnims = new List<Action<float, float>>(); trapClock = 0; trapIdle = 9; flames = new List<Transform>();
            crystal = crystalRing = crystalGlow = null; portal = null; roofShut = false; crucibleWalls.Clear(); TraitorReset();
        }
        // a renderer with no material, or one whose shader failed, draws magenta: give it plain stone and say which one it was
        void FixBrokenMaterials(Transform root)
        {
            Material fallback = null;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var ms = r.sharedMaterials; bool bad = false;
                for (int i = 0; i < ms.Length; i++) if (ms[i] == null || ms[i].shader == null || !ms[i].shader.isSupported || ms[i].shader.name.Contains("InternalError")) { bad = true; ms[i] = fallback ? fallback : (fallback = Mats.Std(0x6a6e74, 0.9f)); }
                if (bad) { r.sharedMaterials = ms; Debug.LogWarning($"BR_BADMAT {LEVEL.name}: {r.name} at {r.transform.position}"); }
            }
        }
        void BuildLevel(int n)
        {
            if (LEVELS == null) DefineLevels();
            ClearLevel();
            levelNum = n; LEVEL = LEVELS[n - 1]; THEME = THEMES[LEVEL.theme]; M = GetThemeMats(LEVEL.theme); LB = LEVEL.B;
            ApplyTheme(THEME);
            levelRoot = new GameObject("Level " + n).transform;
            gravK = LEVEL.lowGrav ? 0.5f : 1; hazCd.Clear(); arenaChanged = false; SC.on = false; WC.on = false;
            frostGore = IsFrostTheme(LEVEL.theme); if (redBloodMat) { bloodMat = frostGore ? frostBloodMat : redBloodMat; gibMat = frostGore ? frostGibMat : redGibMat; }
            LEVEL.build();
            MakeCrystal(LEVEL.center);
            if (IsFrostTheme(LEVEL.theme)) FixBrokenMaterials(levelRoot);
            if (testMode && Arg("-brfloaters") != null) { var counts = new Dictionary<string, int>(); foreach (var r in levelRoot.GetComponentsInChildren<MeshRenderer>()) { var b = r.bounds; if (b.min.y < 9 || b.max.y > 70 || Mathf.Abs(b.center.x) > LB || Mathf.Abs(b.center.z) > LB || r.name == "glow") continue; string k = (r.GetComponent<MeshFilter>()?.sharedMesh?.name ?? "?") + " | " + r.sharedMaterial?.name + " | " + r.transform.parent?.name; counts[k] = counts.TryGetValue(k, out var c) ? c + 1 : 1; } foreach (var kv in counts) Debug.LogWarning($"BR_FLOAT {kv.Value}x {kv.Key}"); }
            levelCardPending = LEVEL.hazard != null;
        }
        // Editor preview (see LevelPreview): builds level n with the exact same code, but without the camera,
        // lights, fog or game loop. Returns the level's root.
        public Transform BuildPreview(int n)
        {
            Mats.Init(); if (Tex.Glow == null) Tex.Init();
            if (LEVELS == null) DefineLevels();
            // the few shared assets the level builders use (normally made by InitShared at game start)
            if (!spikeMat) spikeMat = Mats.Std(0xd8ccb0, 0.5f);
            if (!stainedMat) stainedMat = Mats.Basic(0xffffff, Tex.Stained, true);
            if (!wallRingGeo) wallRingGeo = Geo.Cyl(1, 1, 1, 48, true);
            levelNum = n; LEVEL = LEVELS[n - 1]; THEME = THEMES[LEVEL.theme]; M = GetThemeMats(LEVEL.theme); LB = LEVEL.B;
            levelRoot = new GameObject($"Level {n} - {LEVEL.name}").transform;
            LEVEL.build();
            MakeCrystal(LEVEL.center);
            return levelRoot;
        }
        bool HolyLevel => THEME.holy;

        // ---------- collision (identical to the HTML game's AABB solver) ----------
        static bool Overlaps(float ax, float ay, float az, float bx, float by, float bz, AABB c) =>
            ax < c.max.x && bx > c.min.x && ay < c.max.y && by > c.min.y && az < c.max.z && bz > c.min.z;
        bool PointInLevel(Vector3 p, out AABB hit)
        {
            foreach (var c in colliders) if (c.Contains(p)) { hit = c; return true; }
            hit = default; return false;
        }
        bool PointInLevel(Vector3 p) => PointInLevel(p, out _);
        struct MoveRes { public bool ground, wall; }
        // stepOver > 0 (the Hollow King): anything narrower than 1.5m (posts, lamps, cones, the fountain blocks) is ignored, and
        // anything lower than stepOver doesn't stop him sideways - he wades through rubble, wrecks and debris and over them
        MoveRes MoveBody(ref Vector3 pos, ref Vector3 vel, float r, float h, float dt, float stepOver = 0)
        {
            var res = new MoveRes();
            float sp = Mathf.Abs(vel.x) + Mathf.Abs(vel.y) + Mathf.Abs(vel.z);
            int steps = Mathf.Min(24, Mathf.Max(1, Mathf.CeilToInt(sp * dt / 0.25f)));
            float sdt = dt / steps;
            for (int s = 0; s < steps; s++)
            {
                for (int ax = 0; ax < 3; ax++)
                {
                    int a = ax == 0 ? 0 : ax == 1 ? 2 : 1; // x, z, y
                    float v = vel[a]; if (v == 0) continue;
                    pos[a] += v * sdt;
                    foreach (var c in colliders)
                    {
                        if (stepOver > 0 && ((c.max.x - c.min.x < 1.5f && c.max.z - c.min.z < 1.5f) || (c.max.y - pos.y < stepOver && (a != 1 || (c.max.y > 0.3f && c.min.y > -0.1f))))) continue; // (the floor itself still holds him up)
                        if (!Overlaps(pos.x - r, pos.y, pos.z - r, pos.x + r, pos.y + h, pos.z + r, c)) continue;
                        if (a == 1)
                        {
                            if (v < 0) { pos.y = c.max.y; res.ground = true; } else pos.y = c.min.y - h;
                            vel.y = 0;
                        }
                        else
                        {
                            pos[a] = v > 0 ? c.min[a] - r - 1e-4f : c.max[a] + r + 1e-4f;
                            vel[a] = 0; res.wall = true;
                        }
                    }
                }
            }
            return res;
        }
        const int LEVEL_MASK = 1 << Mk.LayerLevel, ENEMY_MASK = 1 << Mk.LayerEnemy;
        bool Los(Vector3 a, Vector3 b)
        {
            var d = b - a; float len = d.magnitude; if (len < 0.01f) return true;
            return !Physics.Raycast(a, d / len, len, LEVEL_MASK, QueryTriggerInteraction.Ignore);
        }
        static float DistPointSeg(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = b - a; float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
            return Vector3.Distance(a + ab * t, p);
        }
        bool InBounds(Vector3 p, float m = 0) => Mathf.Abs(p.x) <= LB + m && Mathf.Abs(p.z) <= LB + m;
    }
}
