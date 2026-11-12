using System;
using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    // ACT II, THE FROZEN CIRCLE (levels 6-10): five hand-built maps (GameFrostMaps), each with its own theme, layout and
    // traps (GameFrostTraps). This file: the five themes, the shared frost materials, the props, the skies and backdrops.
    public partial class Game
    {
        // the five maps share the frost walls/props (deco "frost") but each has its own light, fog, colours and sky
        static void AddFrostThemes()
        {
            if (THEMES.ContainsKey("frost_town")) return;
            Theme F(int bg, float fn, float ff, int sky, int ground, float hemi, int sun, float sunI, string[] floor, string[] wall, string[] block, string[] tower, int trim, string skyKind, string banner)
                => new Theme { holy = false, bg = bg, fogNear = fn, fogFar = ff, hemiSky = sky, hemiGround = ground, hemiI = hemi, sunC = sun, sunI = sunI, floor = floor, wall = wall, block = block, tower = tower,
                    trim = trim, pool = "ice", crystal = 0x90e4ff, crystalEm = 0x104060, portal = 0x60c8ff, portal2 = 0xffffff, warp = new[] { "#ffffff", "#60c8ff" }, ambient = "snow", wallH = 22, deco = "frost", sky = skyKind, banner = banner };
            THEMES["frost"] = F(0x0b1824, 34, 170, 0xb0d8ff, 0x18242e, 1.6f, 0xe0f0ff, 2.0f,
                new[] { "#dfeaf2", "#a8bfd0", null, "veins" }, new[] { "#34505f", "#182a36", null, "icecracks" }, new[] { "#b4d0e2", "#6a8ca2", "#60d0ff", null }, new[] { "#3a4e5e", "#1a2632", "#60c8ff", "icecracks" }, 0x60d0ff, "aurora", "#80d8ff");
            THEMES["frost_town"] = F(0x5a6670, 18, 118, 0xd8e2ea, 0x3a4048, 1.45f, 0xeaf0f4, 1.5f,
                new[] { "#c8ccd0", "#8a9096", null, "crackdark" }, new[] { "#4a4a52", "#2a2a30", null, null }, new[] { "#7a6a5a", "#4a3a2c", "#c8a070", null }, new[] { "#3e3e46", "#1e1e24", "#90a8c0", null }, 0xffc070, "snowstorm", "#dfe8f0");
            THEMES["frost_ruin"] = F(0x1a2a3a, 26, 150, 0xa8c8e8, 0x2a2026, 1.5f, 0xffc8a0, 1.9f,
                new[] { "#9aa6b0", "#6a747e", null, "icecracks" }, new[] { "#5a6066", "#30363c", null, "veins" }, new[] { "#a8a49a", "#6a665c", "#70d8ff", "veins" }, new[] { "#7a7c80", "#44464c", "#70d8ff", "veins" }, 0x70d8ff, "dusk", "#a8e0ff");
            THEMES["frost_prison"] = F(0x06101c, 24, 140, 0x80b8ff, 0x0a1420, 1.55f, 0xa0d0ff, 1.8f,
                new[] { "#6c8aa4", "#3c5268", null, "icecracks" }, new[] { "#22323e", "#0e1820", null, "icecracks" }, new[] { "#8ab8d8", "#4a6a86", "#40c8ff", null }, new[] { "#2a3846", "#101a24", "#40c0ff", "icecracks" }, 0x40c8ff, "glacier", "#60c8ff");
            THEMES["frost_throne"] = F(0x101428, 22, 150, 0x8aa0ff, 0x140a18, 1.9f, 0xb8c8ff, 1.6f,
                new[] { "#3a4656", "#1a2230", null, "icecracks" }, new[] { "#221c2a", "#0e0a14", null, "icecracks" }, new[] { "#5a6a80", "#2a3242", "#60c8ff", null }, new[] { "#1c1824", "#0a080e", "#5ab8ff", "icecracks" }, 0x5ab8ff, "abyss", "#8ad8ff");
        }
        bool IsFrostTheme(string theme) => theme != null && theme.StartsWith("frost");

        // ---------- shared frost materials ----------
        Material slickM, snowM, woodM, darkWoodM, plasterM, roofM, pineM, pineDarkM, darkIronM, frostRuneM, stoneM, stoneDarkM, cobbleM, bronzeM, frostGlassM, litGlassM, charM;
        void FrostMats()
        {
            ActMats();
            slickM = Mats.Std(0xcfeeff, 0.02f, 0.35f, 0x0a2030); snowM = Mats.Std(0xf2f6fa, 0.95f); woodM = Mats.Std(0x4a3426, 0.9f); darkWoodM = Mats.Std(0x2a1e18, 0.9f);
            plasterM = Mats.Std(0xa8a49c, 0.92f); roofM = Mats.Std(0x2e3038, 0.75f); pineM = Mats.Std(0x1e3a2c, 0.9f); pineDarkM = Mats.Std(0x15291f, 0.9f); darkIronM = Mats.Std(0x2a2c32, 0.45f, 0.7f);
            frostRuneM = Mats.Add(0x60e0ff, 0.85f, null, true, true);
            stoneM = Mats.Std(0x8a8c90, 0.9f); stoneDarkM = Mats.Std(0x55585e, 0.9f); cobbleM = Mats.Std(0x6c7076, 0.95f); bronzeM = Mats.Std(0x7a5a30, 0.4f, 0.8f, 0x0a0602);
            frostGlassM = Mats.Std(0x9ac8e0, 0.1f, 0.2f, 0x081820); litGlassM = Mats.Basic(0xffb060); charM = Mats.Std(0x161210, 0.95f);
            portalGround = -1;
        }
        float portalGround = -1;

        // a triangular prism: the triangle (-w/2,0) (w/2,0) (0,h) in XY, run along Z (a gable roof, a ridge of snow, a wedge)
        static readonly Dictionary<string, Mesh> prismCache = new Dictionary<string, Mesh>();
        static Mesh Prism(float w, float h, float len)
        {
            string key = $"{w:F2},{h:F2},{len:F2}";
            if (prismCache.TryGetValue(key, out var mm) && mm) return mm;
            var b = new Geo.MB(); float z0 = -len / 2, z1 = len / 2;
            Vector3 A(float z) => new Vector3(-w / 2, 0, z); Vector3 B(float z) => new Vector3(w / 2, 0, z); Vector3 C(float z) => new Vector3(0, h, z);
            void Face(Vector3[] q, Vector3 n)
            {
                int s = b.v.Count; foreach (var p in q) { b.v.Add(p); b.n.Add(n); b.uv.Add(new Vector2(p.x + p.z, p.y + p.z) * 0.25f); }
                for (int i = 1; i + 1 < q.Length; i++) { b.Tri(s, s + i, s + i + 1); b.Tri(s, s + i + 1, s + i); } // both windings: never culled
            }
            Face(new[] { A(z1), B(z1), C(z1) }, Vector3.forward); Face(new[] { A(z0), B(z0), C(z0) }, Vector3.back);
            Face(new[] { A(z0), A(z1), C(z1), C(z0) }, new Vector3(-h, w / 2, 0).normalized); Face(new[] { B(z0), B(z1), C(z1), C(z0) }, new Vector3(h, w / 2, 0).normalized);
            Face(new[] { A(z0), B(z0), B(z1), A(z1) }, Vector3.down);
            return prismCache[key] = b.Build();
        }
        GameObject Ignore(GameObject g) { g.layer = Mk.LayerIgnore; return g; }
        // the height of the highest solid surface under (x, z) that is no higher than maxY (0 = the arena floor)
        float GroundAt(float x, float z, float maxY)
        {
            float g = 0;
            foreach (var c in colliders) if (x >= c.min.x && x <= c.max.x && z >= c.min.z && z <= c.max.z && c.max.y <= maxY + 0.01f && c.max.y > g) g = c.max.y;
            return g;
        }

        // ---------- nature ----------
        // a snow-laden pine: a trunk, four drooping tiers, snow lying on each
        void PineTree(float x, float z, float h, float y0 = 0, float lean = 0)
        {
            var p = Mk.Piv(levelRoot, x, y0, z); p.SetRot(lean * 0.6f, (x * 7 + z * 3) % 6, lean * 0.4f);
            Mk.Msh(p, Geo.Cyl(0.16f * h / 8, 0.3f * h / 8, h * 0.34f, 7), woodM, 0, h * 0.17f, 0);
            for (int k = 0; k < 4; k++)
            {
                float t = k / 4f, r = h * (0.27f - t * 0.055f), y = h * (0.22f + t * 0.19f), hh = h * (0.36f - t * 0.04f);
                Mk.Msh(p, Geo.Cone(r, hh, 9), k == 0 ? pineDarkM : pineM, 0, y + hh / 2, 0, 0, k * 0.7f, 0);
                Mk.Msh(p, Geo.Cone(r * 0.78f, hh * 0.38f, 9), snowM, 0, y + hh * 0.8f, 0, 0, k * 0.7f + 0.3f, 0);
            }
            float top = h * (0.22f + 0.75f * 0.19f) + h * (0.36f - 0.75f * 0.04f); // where the top tier ends
            Mk.Msh(p, Geo.Cone(h * 0.05f, h * 0.12f, 7), snowM, 0, top - h * 0.05f, 0); // the snowy tip, sitting on it
            SolidAABB(V(x - 0.4f, y0, z - 0.4f), V(x + 0.4f, y0 + h * 0.62f, z + 0.4f));
        }
        void DeadTree(float x, float z, float h)
        {
            var p = Mk.Piv(levelRoot, x, 0, z); p.SetRot(0, (x * 5 + z) % 6, 0); var bark = Mats.Std(0x2e2620, 0.95f);
            Mk.Msh(p, Geo.Cyl(0.14f, 0.3f, h, 7), bark, 0, h / 2, 0);
            for (int k = 0; k < 5; k++)
            {
                float y = h * (0.45f + k * 0.11f), a = k * 2.3f; var br = Mk.Piv(p, 0, y, 0); br.SetRot(0, a, 0.9f + (k % 2) * 0.3f);
                Mk.Msh(br, Geo.Cyl(0.03f, 0.09f, h * 0.35f, 5), bark, 0, h * 0.17f, 0); Mk.Msh(br, Geo.Box(0.1f, h * 0.3f, 0.1f), snowM, 0.05f, h * 0.17f, 0);
            }
            SolidAABB(V(x - 0.3f, 0, z - 0.3f), V(x + 0.3f, h, z + 0.3f));
        }
        void FallenLog(float x, float z, float len, float ry, float r = 0.45f)
        {
            var p = Mk.Piv(levelRoot, x, r, z); p.SetRot(0, ry, 0);
            Mk.Msh(p, Geo.Cyl(r, r * 1.1f, len, 9), woodM, 0, 0, 0, Mathf.PI / 2, 0, 0); Mk.Msh(p, Geo.Box(r * 1.2f, 0.18f, len * 0.96f), snowM, 0, r * 0.88f, 0);
            foreach (var e in new[] { -1f, 1f }) Mk.Msh(p, Geo.Cyl(r * 0.85f, r * 0.85f, 0.03f, 9), Mats.Std(0x8a6a48, 0.9f), 0, 0, e * len / 2, Mathf.PI / 2, 0, 0);
            var ex = Quaternion.Euler(0, ry * Mathf.Rad2Deg, 0) * V(0, 0, len / 2); float hx = Mathf.Abs(ex.x) + r, hz = Mathf.Abs(ex.z) + r;
            SolidAABB(V(x - hx * 0.9f, 0, z - hz * 0.9f), V(x + hx * 0.9f, r * 1.8f, z + hz * 0.9f));
        }
        void Stump(float x, float z) { Deco(Geo.Cyl(0.45f, 0.6f, 0.8f, 8), woodM, x, 0.4f, z); Deco(Geo.Cyl(0.47f, 0.47f, 0.14f, 8), snowM, x, 0.85f, z); SolidAABB(V(x - 0.45f, 0, z - 0.45f), V(x + 0.45f, 0.9f, z + 0.45f)); }
        void Boulder(float x, float z, float s, float y0 = 0)
        {
            var rock = Mats.Std(0x4a5058, 0.92f); var rock2 = Mats.Std(0x3a4048, 0.95f);
            var rng = new System.Random((int)(x * 13 + z * 29)); float R() => (float)rng.NextDouble();
            Deco(Geo.Icosahedron(1), rock, x, y0 + s * 0.42f, z, R() * 3, R() * 3, R() * 3).transform.localScale = V(s * 1.15f, s * 0.72f, s);
            for (int k = 0; k < 3; k++) { float a = k * 2.1f + R(); Deco(Geo.Icosahedron(1), k % 2 == 0 ? rock2 : rock, x + Mathf.Cos(a) * s * 0.6f, y0 + s * 0.25f, z + Mathf.Sin(a) * s * 0.5f, R() * 3, R() * 3, R() * 3).transform.localScale = V(s * 0.55f, s * 0.42f, s * 0.5f); }
            Deco(Geo.Sphere(1, 10, 6, 0, Mathf.PI * 2, 0, Mathf.PI * 0.42f), snowM, x, y0 + s * 0.68f, z, 0, R() * 3, 0).transform.localScale = V(s * 0.95f, s * 0.5f, s * 0.82f);
            for (int k = 0; k < 2; k++) Ignore(Deco(Geo.Cone(0.04f * s + 0.03f, 0.25f * s, 5), iceM, x + (k - 0.5f) * s * 0.7f, y0 + s * 0.3f, z + s * 0.75f, Mathf.PI, 0, 0));
            SolidAABB(V(x - s * 0.95f, y0, z - s * 0.8f), V(x + s * 0.95f, y0 + s * 1.0f, z + s * 0.8f));
        }
        // a wind-sculpted drift: a long dome with a crest blown up one side and a tail running off the other. Out in the open
        // (solid = false) it's kept low, a swell in the snow you wade over; against a wall or building it's solid
        static Mesh driftDome;
        void Snowbank(float x, float z, float w, float h, bool solid = false)
        {
            if (!driftDome) driftDome = Geo.Sphere(1, 16, 8, 0, Mathf.PI * 2, 0, Mathf.PI / 2);
            if (!solid) h = Mathf.Min(h, 0.32f);
            float ry = (x * 0.37f + z * 0.61f) % 6.28f; var q = Quaternion.Euler(0, ry * Mathf.Rad2Deg, 0);
            var main = Deco(driftDome, snowM, x, 0, z, 0, ry, 0); main.transform.localScale = V(w, h, w * 0.62f);
            var crest = q * V(0.18f * w, 0, 0.12f * w); var c = Deco(driftDome, snowM, x + crest.x, 0, z + crest.z, 0.12f, ry + 0.3f, 0); c.transform.localScale = V(w * 0.55f, h * 1.18f, w * 0.32f);
            var tail = q * V(-0.75f * w, 0, -0.05f * w); var t = Deco(driftDome, snowM, x + tail.x, 0, z + tail.z, 0, ry - 0.15f, 0); t.transform.localScale = V(w * 0.6f, h * 0.55f, w * 0.38f);
            main.layer = c.layer = t.layer = Mk.LayerIgnore;
            if (solid) { var ext = q * V(w * 0.62f, 0, w * 0.4f); float ex = Mathf.Max(Mathf.Abs(ext.x), Mathf.Abs((q * V(w * 0.62f, 0, -w * 0.4f)).x)), ez = Mathf.Max(Mathf.Abs(ext.z), Mathf.Abs((q * V(w * 0.62f, 0, -w * 0.4f)).z)); SolidAABB(V(x - ex, 0, z - ez), V(x + ex, h * 0.82f, z + ez)); }
        }
        // one six-sided ice crystal standing from p, leaning along (tx, tz): a faceted body and a pointed tip
        void HexCrystal(Vector3 p, float r, float h, float tx, float tz, Material m, float spin = 0)
        {
            var pv = Mk.Piv(levelRoot, p.x, p.y, p.z); pv.SetRot(tx, spin, tz);
            Mk.Msh(pv, Geo.Cyl(r * 0.88f, r, h * 0.78f, 6), m, 0, h * 0.39f, 0);
            Mk.Msh(pv, Geo.Cyl(0, r * 0.88f, h * 0.22f, 6), m, 0, h * 0.78f + h * 0.11f, 0);
        }
        // ice formation: a cluster of hexagonal crystals bursting out of the ground, the big one in the middle (solid core)
        void IceFormation(float x, float z, float h, float s = 1, float y0 = 0)
        {
            var rng = new System.Random((int)(x * 31 + z * 17)); float R() => (float)rng.NextDouble();
            var deep = Mats.Std(0x8ac4e8, 0.05f, 0.25f, 0x0a3050);
            HexCrystal(V(x, y0 - 0.3f, z), 1.0f * s, h, (R() - 0.5f) * 0.15f, (R() - 0.5f) * 0.15f, iceM, R() * 3);
            int n = 6 + (int)(R() * 3);
            for (int k = 0; k < n; k++)
            {
                float a = k * Mathf.PI * 2 / n + R() * 0.5f, d = (0.9f + R() * 0.9f) * s, hh = h * (0.25f + R() * 0.45f), lean = 0.3f + R() * 0.45f;
                HexCrystal(V(x + Mathf.Cos(a) * d, y0 - 0.2f, z + Mathf.Sin(a) * d), (0.35f + R() * 0.35f) * s, hh, Mathf.Sin(a) * lean, -Mathf.Cos(a) * lean, k % 3 == 0 ? iceTransM : k % 3 == 1 ? iceM : deep, R() * 3);
            }
            for (int k = 0; k < 4; k++) { float a = R() * 6.28f, d = (1.8f + R()) * s; Ignore(Deco(Geo.Icosahedron(1), iceM, x + Mathf.Cos(a) * d, y0 + 0.15f, z + Mathf.Sin(a) * d, R() * 3, R() * 3, R() * 3)).transform.localScale = Vector3.one * (0.2f + R() * 0.25f) * s; } // chips at the foot
            Ignore(Deco(Geo.Sphere(1, 10, 6), snowM, x, y0, z)).transform.localScale = V(2.2f * s, 0.35f * s, 2.0f * s);
            var gl = Mk.Glow(levelRoot, 0x80d8ff, h * 0.6f, 0.18f); gl.transform.localPosition = V(x, y0 + h * 0.5f, z);
            SolidAABB(V(x - 1.2f * s, y0, z - 1.2f * s), V(x + 1.2f * s, y0 + h * 0.85f, z + 1.2f * s));
        }
        // ---------- the shrine the portal stands on: a frost-rune dais ringed by leaning ice obelisks ----------
        void FrostShrine(float x, float z, float y0 = 0)
        {
            var dais = Mats.Std(0x3a4858, 0.6f, 0.3f);
            Ignore(Deco(Geo.Cyl(4.6f, 4.9f, 0.18f, 24), dais, x, y0 + 0.09f, z)); Ignore(Deco(Geo.Cyl(3.6f, 3.8f, 0.14f, 24), Mats.Std(0x506478, 0.4f, 0.4f), x, y0 + 0.25f, z));
            Flat(Geo.Ring(3.9f, 4.15f, 48), frostRuneM, x, y0 + 0.2f, z); Flat(Geo.Ring(2.4f, 2.55f, 40), frostRuneM, x, y0 + 0.33f, z);
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; Ignore(Deco(Geo.Box(0.12f, 0.02f, 0.9f), frostRuneM, x + Mathf.Cos(a) * 3.15f, y0 + 0.33f, z + Mathf.Sin(a) * 3.15f, 0, -a, 0)); }
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI / 3 + 0.5f, px = x + Mathf.Cos(a) * 5.6f, pz = z + Mathf.Sin(a) * 5.6f, h = 4.2f + (k % 3) * 1.1f;
                var ob = Deco(Geo.Octahedron(1), iceM, px, y0 + h * 0.48f, pz, Mathf.Sin(a) * 0.16f, a, -Mathf.Cos(a) * 0.16f); ob.transform.localScale = V(0.55f, h * 0.52f, 0.55f);
                Ignore(Deco(Geo.Cyl(0.8f, 1.0f, 0.4f, 6), stoneDarkM, px, y0 + 0.2f, pz));
                SolidAABB(V(px - 0.5f, y0, pz - 0.5f), V(px + 0.5f, y0 + h * 0.85f, pz + 0.5f));
            }
            var g = Mk.Glow(levelRoot, 0x80e0ff, 9, 0.3f); g.transform.localPosition = V(x, y0 + 1, z); flames.Add(g.transform);
            portalGround = y0 + 0.3f;
            anims.Add((t, dt) => { if (rand < 0.3f) { float a = rand * Mathf.PI * 2, r = 1 + rand * 3; SpawnParticles(V(x + Mathf.Cos(a) * r, y0 + 0.4f, z + Mathf.Sin(a) * r), 1, 0.6f, cyanMat, 0.05f, 1.2f, -2); } });
        }

        // ---------- people frozen solid where they stood ----------
        // kind: 0 villager, 1 soldier, 2 prisoner, 3 hunter. pose: 0 shielding the face, 1 reaching out, 2 running, 3 huddled on the
        // knees, 4 chained with arms up, 5 standing guard. Each is sealed in a clear six-sided crystal of ice, frost on the clothes.
        void FrozenFigure(float x, float z, float ry, int pose, float y0 = 0, int kind = 0, bool encased = true)
        {
            var root = Mk.Piv(levelRoot, x, y0, z); root.SetRot(0, ry, 0);
            int seed = (int)Mathf.Abs(x * 13 + z * 7);
            Material Fm(int c, float rough = 0.85f) => Mats.Std(c, rough, 0, 0x04080c);
            var skin = Fm(0x9aaab6, 0.45f);
            int[] coats = { 0x5a4636, 0x3e4c60, 0x64403a, 0x4a5642 };
            var coat = Fm(kind == 1 ? 0x3c4654 : kind == 2 ? 0x56544e : coats[seed % 4]);
            var dark = Fm(kind == 3 ? 0x9a8a72 : 0x2e2c2c, 0.95f); var boot = Fm(0x2a2420, 0.8f); var frost = Mats.Std(0xe8f4fc, 0.6f);
            bool kneel = pose == 3, run = pose == 2;
            float hipY = kneel ? 0.5f : 0.98f;
            var hips = Mk.Piv(root, 0, hipY, run ? 0.05f : 0); if (run) hips.SetRot(0.15f, 0, 0);
            Mk.Msh(hips, Geo.Box(0.34f, 0.2f, 0.22f), coat, 0, 0, 0);
            for (int s = -1; s <= 1; s += 2)
            {
                var th = Mk.Piv(hips, s * 0.1f, -0.04f, 0);
                float tx = kneel ? (s < 0 ? -1.5f : -0.2f) : run ? (s < 0 ? -0.8f : 0.6f) : pose == 0 ? (s < 0 ? -0.1f : 0.15f) : 0;
                float kx = kneel ? (s < 0 ? 1.5f : 1.7f) : run ? (s < 0 ? 0.5f : 1.0f) : 0.05f;
                th.SetRot(tx, 0, s * 0.05f);
                Mk.Msh(th, GeoX.Limb(0.095f, 0.075f, 0.45f), kind == 2 ? dark : coat, 0, 0, 0);
                var kn = Mk.Piv(th, 0, -0.45f, 0); kn.SetRot(kx, 0, 0);
                Mk.Msh(kn, GeoX.Limb(0.075f, 0.06f, 0.42f), kind == 1 ? darkIronM : dark, 0, 0, 0);
                Mk.Msh(kn, Geo.Box(0.13f, 0.11f, 0.27f), boot, 0, -0.45f, 0.05f); Mk.Msh(kn, Geo.Box(0.15f, 0.04f, 0.29f), frost, 0, -0.38f, 0.05f);
            }
            var torso = Mk.Piv(hips, 0, 0.08f, 0); torso.SetRot(run ? 0.35f : kneel ? 0.4f : pose == 0 ? -0.12f : pose == 4 ? -0.08f : 0.04f, pose == 1 ? -0.25f : 0, 0);
            var chest = Mk.Msh(torso, GeoX.Capsule(0.16f, 0.22f, 3, 12), coat, 0, 0.3f, 0); chest.transform.localScale = V(1.35f, 1, 0.82f);
            Mk.Msh(torso, GeoX.Capsule(0.14f, 0.1f, 3, 10), coat, 0, 0.08f, 0).transform.localScale = V(1.2f, 1, 0.8f);
            if (kind == 0 || kind == 3) Mk.Msh(torso, GeoX.Lathe("coatHem", GeoX.YR(0.05f, 0.19f, -0.2f, 0.24f, -0.45f, 0.29f, -0.52f, 0.3f), 14), coat, 0, -0.02f, 0); // the coat hanging to the knees
            if (kind == 3) Mk.Msh(torso, Geo.Torus(0.17f, 0.08f, 12, 6), dark, 0, 0.52f, 0, Mathf.PI / 2, 0, 0);
            if (kind == 1) { Mk.Msh(torso, Geo.Box(0.44f, 0.4f, 0.32f), darkIronM, 0, 0.32f, 0.01f); Mk.Msh(torso, Geo.Box(0.5f, 0.1f, 0.34f), darkWoodM, 0, 0.02f, 0); Mk.Msh(torso, Geo.Box(0.5f, 0.75f, 0.06f), Fm(0x2a3a4e, 0.95f), 0, 0.15f, -0.18f, 0.1f, 0, 0); }
            else Mk.Msh(torso, Geo.Torus(0.2f, 0.025f, 12, 4), dark, 0, 0.0f, 0, Mathf.PI / 2, 0, 0); // a belt
            Mk.Msh(torso, Geo.Box(0.42f, 0.05f, 0.26f), frost, 0, 0.51f, 0); // frost on the shoulders
            var neck = Mk.Piv(torso, 0, 0.55f, 0); neck.SetRot(kneel ? 0.5f : pose == 0 ? 0.25f : run ? -0.2f : pose == 4 ? -0.3f : 0, pose == 1 ? 0.3f : 0, 0);
            Mk.Msh(neck, Geo.Cyl(0.05f, 0.06f, 0.1f, 8), skin, 0, 0.04f, 0);
            if (kind != 2) Mk.Msh(neck, Geo.Torus(0.075f, 0.035f, 10, 5), kind == 1 ? darkIronM : dark, 0, 0.04f, 0, Mathf.PI / 2, 0, 0); // a scarf / gorget
            var head = Mk.Piv(neck, 0, 0.19f, 0.01f);
            Mk.Msh(head, Geo.Sphere(0.11f, 14, 10), skin, 0, 0, 0).transform.localScale = V(0.92f, 1.1f, 1.02f);
            Mk.Msh(head, Geo.Cone(0.022f, 0.06f, 5), skin, 0, -0.01f, 0.11f, Mathf.PI / 2, 0, 0); // nose
            Mk.Msh(head, Geo.Box(0.15f, 0.05f, 0.08f), skin, 0, -0.075f, 0.05f); // jaw
            foreach (var sx in new[] { -1f, 1f }) Mk.Msh(head, Geo.Box(0.035f, 0.012f, 0.01f), Fm(0x1a2026), sx * 0.04f, 0.015f, 0.105f); // eyes shut
            if (kind == 0) { var hood = Mk.Msh(head, Geo.Sphere(0.135f, 12, 8, 0, Mathf.PI * 2, 0, Mathf.PI * 0.62f), coat, 0, 0.015f, -0.02f); hood.transform.localRotation = Quaternion.Euler(-25, 0, 0); }
            else if (kind == 1) { Mk.Msh(head, Geo.Sphere(0.13f, 12, 8, 0, Mathf.PI * 2, 0, Mathf.PI * 0.5f), darkIronM, 0, 0.03f, 0); Mk.Msh(head, Geo.Cyl(0.15f, 0.15f, 0.02f, 14), darkIronM, 0, 0.03f, 0); Mk.Msh(head, Geo.Box(0.025f, 0.11f, 0.03f), darkIronM, 0, 0.0f, 0.12f); }
            else if (kind == 3) { Mk.Msh(head, Geo.Cyl(0.12f, 0.13f, 0.15f, 12), dark, 0, 0.1f, 0); Mk.Msh(head, Geo.Torus(0.125f, 0.035f, 12, 5), dark, 0, 0.04f, 0, Mathf.PI / 2, 0, 0); Mk.Msh(head, Geo.Sphere(0.06f, 8, 6), dark, 0, 0.19f, 0); }
            else Mk.Msh(head, Geo.Sphere(0.115f, 10, 6, 0, Mathf.PI * 2, 0, Mathf.PI * 0.45f), Fm(0x3a3632, 0.95f), 0, 0.02f, -0.01f);
            Mk.Msh(head, Geo.Sphere(0.08f, 8, 5, 0, Mathf.PI * 2, 0, Mathf.PI * 0.35f), frost, 0, kind == 3 ? 0.2f : 0.08f, 0);
            for (int s = -1; s <= 1; s += 2)
            {
                float sx, sz, ex;
                switch (pose)
                {
                    case 0: sx = -2.0f; sz = -s * 0.4f; ex = -1.8f; break;
                    case 1: sx = s > 0 ? -1.45f : -0.3f; sz = s * 0.15f; ex = s > 0 ? -0.12f : -0.6f; break;
                    case 2: sx = s < 0 ? -0.9f : 0.7f; sz = s * 0.15f; ex = -1.4f; break;
                    case 3: sx = -1.0f; sz = -s * 0.6f; ex = -1.7f; break;
                    case 4: sx = -0.2f; sz = s * 2.55f; ex = -0.3f; break;
                    default: sx = s > 0 ? -0.35f : 0.05f; sz = s * 0.1f; ex = s > 0 ? -0.9f : -0.15f; break;
                }
                var sh = Mk.Piv(torso, s * 0.24f, 0.47f, 0); sh.SetRot(sx, 0, sz);
                Mk.Msh(sh, Geo.Sphere(0.08f, 10, 8), kind == 1 ? darkIronM : coat, 0, 0, 0);
                Mk.Msh(sh, GeoX.Limb(0.065f, 0.055f, 0.29f), kind == 2 ? skin : coat, 0, 0, 0);
                var el = Mk.Piv(sh, 0, -0.29f, 0); el.SetRot(ex, 0, 0);
                Mk.Msh(el, GeoX.Limb(0.055f, 0.045f, 0.26f), kind == 2 ? skin : coat, 0, 0, 0);
                Mk.Msh(el, Geo.Box(0.075f, 0.11f, 0.05f), kind == 3 || kind == 1 ? dark : skin, 0, -0.31f, 0);
                if (kind == 2) Mk.Msh(el, Geo.Torus(0.065f, 0.022f, 10, 4), darkIronM, 0, -0.22f, 0, Mathf.PI / 2, 0, 0);
                if (kind == 1 && s > 0) { Mk.Msh(el, Geo.Cyl(0.022f, 0.022f, 2.3f, 5), darkWoodM, 0, -0.31f, 0, Mathf.PI / 2 - 0.15f, 0, 0); Mk.Msh(el, Geo.Cone(0.05f, 0.28f, 4), darkIronM, 0, -0.31f + 0.17f, 1.28f, Mathf.PI / 2, 0, 0); }
                if (kind == 1 && s < 0) { Mk.Msh(el, Geo.Cyl(0.3f, 0.3f, 0.05f, 14), darkIronM, -0.06f, -0.18f, 0.05f, 0, 0, Mathf.PI / 2); Mk.Msh(el, Geo.Sphere(0.06f, 8, 6), Fm(0x8a6a30, 0.4f), -0.09f, -0.18f, 0.05f); }
            }
            float bodyH = kneel ? 1.3f : 1.9f;
            if (encased)
            {
                // the crystal: a clean six-sided column of clear ice, its top grown into points, shards round the foot
                var col = Mk.Msh(root, Geo.Cyl(0.5f, 0.56f, bodyH, 6), iceTransM, 0, bodyH / 2, 0); col.transform.localRotation = Quaternion.Euler(0, seed % 60, 0);
                Mk.Msh(root, Geo.Cyl(0.04f, 0.5f, 0.45f, 6), iceTransM, 0, bodyH + 0.22f, 0, 0, (seed % 60) * Mathf.Deg2Rad, 0);
                for (int k = 0; k < 3; k++) { float a = k * 2.1f + seed; Mk.Msh(root, Geo.Cyl(0.02f, 0.13f, 0.55f + k * 0.12f, 6), iceM, Mathf.Cos(a) * 0.28f, bodyH + 0.1f + k * 0.04f, Mathf.Sin(a) * 0.28f, Mathf.Sin(a) * 0.35f, 0, -Mathf.Cos(a) * 0.35f); }
                for (int k = 0; k < 6; k++) { float a = k * Mathf.PI / 3; Mk.Msh(root, Geo.Box(0.03f, bodyH * 0.9f, 0.03f), Mats.Std(0xe8f8ff, 0.2f, 0, 0x0a2030), Mathf.Cos(a) * 0.53f, bodyH * 0.47f, Mathf.Sin(a) * 0.53f); } // the bright edges of the crystal
            }
            Mk.Msh(root, Geo.Cyl(0.62f, 0.72f, 0.1f, 8), iceM, 0, 0.05f, 0);
            for (int k = 0; k < 5; k++) { float a = k * 1.26f + seed; Mk.Msh(root, Geo.Cyl(0.02f, 0.1f, 0.3f + (k % 3) * 0.14f, 6), iceM, Mathf.Cos(a) * 0.62f, 0.14f, Mathf.Sin(a) * 0.62f, Mathf.Sin(a) * 0.5f, 0, -Mathf.Cos(a) * 0.5f); }
            foreach (var r in root.t.GetComponentsInChildren<Renderer>()) r.gameObject.layer = Mk.LayerIgnore;
            SolidAABB(V(x - 0.5f, y0, z - 0.5f), V(x + 0.5f, y0 + bodyH, z + 0.5f));
        }

        // ---------- camp props ----------
        void Tent(float x, float z, float ry, float y0 = 0)
        {
            // a hunters' ridge tent of patched canvas: A-frame poles at each end, a ridge pole, the front flaps tied back on a dark
            // doorway, guy lines out to stakes, snow lying on the roof and drifted up the windward side
            var p = Mk.Piv(levelRoot, x, y0, z); p.SetRot(0, ry, 0);
            var canvas = Mats.Std(0x9a8a68, 0.95f, 0, 0x060504, null, true); var canvas2 = Mats.Std(0x7a3a2a, 0.95f, 0, 0, null, true); var rope = Mats.Std(0x6a5a40, 0.9f);
            const float TW = 2.8f, TH = 2.1f, TL = 3.6f; float slope = Mathf.Atan2(TH, TW / 2), side = Mathf.Sqrt(TH * TH + TW * TW / 4);
            Mk.Msh(p, Prism(TW, TH, TL), canvas, 0, 0, 0);
            foreach (var sx in new[] { -1f, 1f })
            {
                // a faded stripe and a sewn-on patch along each roof slope, and the snow lying on it
                var sp = Mk.Piv(p, sx * TW / 4, TH / 2, 0); sp.SetRot(0, 0, sx * (Mathf.PI / 2 - slope));
                Mk.Msh(sp, Geo.Box(0.03f, side * 0.18f, TL + 0.02f), canvas2, sx * 0.012f, -side * 0.28f, 0);
                Mk.Msh(sp, Geo.Box(0.03f, 0.5f, 0.6f), Mats.Std(0x6a6450, 0.95f), sx * 0.014f, side * 0.1f, sx * 0.7f);
                Mk.Msh(sp, Geo.Box(0.12f, side * 0.5f, TL * 0.92f), snowM, sx * 0.06f, side * 0.2f, 0);
                // A-frame poles, crossing above the ridge at each end
                foreach (var ez in new[] { -1f, 1f }) { var pl = Mk.Piv(p, sx * TW / 2, 0, ez * (TL / 2 + 0.06f)); pl.SetRot(0, 0, sx * (Mathf.PI / 2 - slope)); Mk.Msh(pl, Geo.Cyl(0.04f, 0.045f, side + 0.35f, 6), woodM, 0, (side + 0.35f) / 2, 0); }
                // guy lines from the ridge ends out to stakes in the snow
                foreach (var ez in new[] { -1f, 1f })
                {
                    var top = V(0, TH + 0.1f, ez * (TL / 2 + 0.1f)); var stake = V(sx * 0.8f, 0.1f, ez * (TL / 2 + 1.6f)); var d = stake - top;
                    var gl = Mk.Msh(p, Geo.Cyl(0.012f, 0.012f, d.magnitude, 4), rope, (top.x + stake.x) / 2, (top.y + stake.y) / 2, (top.z + stake.z) / 2); gl.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
                    Mk.Msh(p, Geo.Cyl(0.03f, 0.02f, 0.35f, 5), darkWoodM, stake.x, 0.1f, stake.z, 0.3f * -ez, 0, 0);
                }
                // the flaps tied back either side of the doorway
                Mk.Msh(p, Prism(0.55f, 1.4f, 0.03f), canvas, sx * 0.62f, 0, TL / 2 + 0.12f, 0, -sx * 0.5f, 0);
            }
            Mk.Msh(p, Geo.Cyl(0.04f, 0.04f, TL + 0.5f, 6), woodM, 0, TH + 0.02f, 0, Mathf.PI / 2, 0, 0); // the ridge pole
            Mk.Msh(p, Prism(1.1f, 1.55f, 0.03f), Mats.Std(0x0e0a08, 0.95f), 0, 0.01f, TL / 2 + 0.02f); // the dark doorway
            Mk.Msh(p, Geo.Sphere(1, 12, 6, 0, Mathf.PI * 2, 0, Mathf.PI / 2), snowM, -TW / 2 - 0.1f, 0, 0).transform.localScale = V(0.9f, 0.75f, TL * 0.5f); // drifted up the windward side
            Mk.Msh(p, Geo.Sphere(1, 12, 6, 0, Mathf.PI * 2, 0, Mathf.PI / 2), snowM, TW / 2 + 0.05f, 0, 0.4f).transform.localScale = V(0.5f, 0.35f, TL * 0.35f);
            var w = Quaternion.Euler(0, ry * Mathf.Rad2Deg, 0) * V(1.4f, 0, 1.8f); SolidAABB(V(x - Mathf.Abs(w.x), y0, z - Mathf.Abs(w.z)), V(x + Mathf.Abs(w.x), y0 + 1.8f, z + Mathf.Abs(w.z)));
        }
        void Campfire(float x, float z, float y0 = 0)
        {
            for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; Deco(Geo.Icosahedron(0.32f), stoneDarkM, x + Mathf.Cos(a) * 0.9f, y0 + 0.15f, z + Mathf.Sin(a) * 0.9f, k, k * 2, 0); }
            for (int k = 0; k < 3; k++) Deco(Geo.Cyl(0.11f, 0.11f, 1.4f, 6), charM, x, y0 + 0.2f, z, Mathf.PI / 2, k * 1.05f, 0);
            Deco(Geo.Sphere(0.5f, 8, 6), snowM, x + 0.2f, y0 + 0.05f, z - 0.1f).transform.localScale = V(1, 0.25f, 1); // snowed over: it went out long ago
            foreach (var s in new[] { -1f, 1f }) Deco(Geo.Cyl(0.04f, 0.04f, 1.3f, 5), darkWoodM, x + s * 0.9f, y0 + 0.65f, z, 0, 0, s * 0.15f);
            Deco(Geo.Cyl(0.03f, 0.03f, 2.0f, 5), darkWoodM, x, y0 + 1.25f, z, 0, 0, Mathf.PI / 2); Deco(Geo.Cyl(0.22f, 0.18f, 0.3f, 10), darkIronM, x, y0 + 0.95f, z); // the spit and its pot
        }
        void Crate(float x, float z, float s, float y0 = 0, float ry = 0)
        {
            Box(x, y0, z, s, s, s, woodM); Deco(Geo.Box(s * 1.02f, 0.12f, s * 1.02f), snowM, x, y0 + s + 0.04f, z, 0, ry, 0);
            Deco(Geo.Box(s * 1.04f, 0.1f, 0.1f), darkIronM, x, y0 + s * 0.5f, z - s * 0.52f); Deco(Geo.Box(s * 1.04f, 0.1f, 0.1f), darkIronM, x, y0 + s * 0.5f, z + s * 0.52f);
        }
        void Barrel(float x, float z, float y0 = 0) { Deco(Geo.Cyl(0.42f, 0.38f, 1.1f, 10), woodM, x, y0 + 0.55f, z); foreach (var y in new[] { 0.2f, 0.9f }) Deco(Geo.Torus(0.42f, 0.03f, 12, 4), darkIronM, x, y0 + y, z, Mathf.PI / 2); Deco(Geo.Cyl(0.4f, 0.4f, 0.1f, 10), snowM, x, y0 + 1.12f, z); SolidAABB(V(x - 0.42f, y0, z - 0.42f), V(x + 0.42f, y0 + 1.1f, z + 0.42f)); }
        void Sled(float x, float z, float ry)
        {
            var p = Mk.Piv(levelRoot, x, 0, z); p.SetRot(0, ry, 0);
            Mk.Msh(p, Geo.Box(1.4f, 0.12f, 2.8f), woodM, 0, 0.5f, 0); foreach (var s in new[] { -0.6f, 0.6f }) { Mk.Msh(p, Geo.Box(0.08f, 0.08f, 3.0f), darkIronM, s, 0.1f, 0.1f); Mk.Msh(p, Geo.Box(0.08f, 0.45f, 0.08f), darkIronM, s, 0.3f, -0.9f); Mk.Msh(p, Geo.Box(0.08f, 0.45f, 0.08f), darkIronM, s, 0.3f, 0.9f); }
            Mk.Msh(p, Geo.Box(1.0f, 0.5f, 1.2f), Mats.Std(0x5a4a3a, 0.95f), 0, 0.82f, -0.3f); Mk.Msh(p, Geo.Box(1.05f, 0.12f, 1.25f), snowM, 0, 1.1f, -0.3f);
            var w = Quaternion.Euler(0, ry * Mathf.Rad2Deg, 0) * V(0.7f, 0, 1.5f); SolidAABB(V(x - Mathf.Abs(w.x), 0, z - Mathf.Abs(w.z)), V(x + Mathf.Abs(w.x), 1.1f, z + Mathf.Abs(w.z)));
        }
        void Lantern(Transform parent, float x, float y, float z, bool lit)
        {
            Mk.Msh(parent, Geo.Box(0.32f, 0.42f, 0.32f), darkIronM, x, y, z); Mk.Msh(parent, Geo.Box(0.24f, 0.32f, 0.34f), lit ? litGlassM : frostGlassM, x, y, z);
            if (lit) { var g = Mk.Glow(parent, 0xffb060, 2.4f, 0.55f); g.transform.localPosition = V(x, y, z); flames.Add(g.transform); }
        }

        // ---------- buildings ----------
        // a gabled house: a stone footing, plaster walls with a timber frame, shuttered windows, a proper pitched roof of dark
        // slate under snow, a chimney, icicles off the eaves. broken = burned out: the roof gone, walls standing as jagged stumps.
        // The whole house is solid (walls, and the roof volume) so nothing about it can be walked through.
        void House(float x, float z, float w, float d, float h, bool ridgeX, bool broken)
        {
            Box(x, 0, z, w + 0.5f, 0.7f, d + 0.5f, stoneDarkM);
            if (broken)
            {
                Box(x, 0.7f, z, w, 1.9f, d, plasterM);
                Deco(Geo.Box(w + 0.1f, 0.14f, d + 0.1f), snowM, x, 2.65f, z);
                // the wall stumps left standing, black with soot at the top
                var rng = new System.Random((int)(x * 11 + z * 5)); float R() => (float)rng.NextDouble();
                foreach (var (wx, wz, sw, sd) in new[] { (x - w / 2 + 0.4f, z, 0.8f, d), (x + w / 2 - 0.4f, z, 0.8f, d), (x, z - d / 2 + 0.4f, w, 0.8f), (x, z + d / 2 - 0.4f, w, 0.8f) })
                {
                    bool alongX = sw > sd; int n = 4;
                    for (int k = 0; k < n; k++)
                    {
                        if (R() < 0.3f) continue;
                        float u = (k + 0.5f) / n - 0.5f, hh = 0.6f + R() * (h - 3.2f);
                        float bx = alongX ? wx + u * sw : wx, bz = alongX ? wz : wz + u * sd, bw = alongX ? sw / n : 0.8f, bd = alongX ? 0.8f : sd / n;
                        Box(bx, 2.6f, bz, bw, hh, bd, plasterM); Deco(Geo.Box(bw * 1.02f, 0.3f, bd * 1.02f), charM, bx, 2.6f + hh - 0.1f, bz); Deco(Geo.Box(bw * 1.04f, 0.1f, bd * 1.04f), snowM, bx, 2.6f + hh + 0.08f, bz);
                    }
                }
                for (int k = 0; k < 4; k++) Deco(Geo.Box(ridgeX ? w * 0.9f : 0.25f, 0.25f, ridgeX ? 0.25f : d * 0.9f), charM, x + (ridgeX ? 0 : (k - 1.5f) * w / 4), 2.75f, z + (ridgeX ? (k - 1.5f) * d / 4 : 0), ridgeX ? 0 : 0.1f * (k - 1.5f), 0, ridgeX ? 0.1f * (k - 1.5f) : 0); // burned rafters fallen across
                for (int k = 0; k < 10; k++) Deco(Geo.Box(0.5f + R() * 0.5f, 0.1f, 0.4f + R() * 0.3f), roofM, x + (R() - 0.5f) * (w + 4), 0.06f, z + (R() - 0.5f) * (d + 4), R() * 0.3f, R() * 3, R() * 0.3f).layer = Mk.LayerIgnore; // slates shed in the snow
                Stairs(x + w / 2 + 0.25f, z, 2.4f, V(1, 0, 0), 3, 0.7f, 2.6f, 0.9f, stoneDarkM); // rubble steps up onto it
                return;
            }
            Box(x, 0.7f, z, w, h - 0.7f, d, plasterM);
            // timber frame: corner posts, a band at the upper floor, diagonal braces, a sill
            foreach (var sx in new[] { -1f, 1f }) foreach (var sz in new[] { -1f, 1f }) Deco(Geo.Box(0.3f, h - 0.7f, 0.3f), darkWoodM, x + sx * (w / 2 - 0.05f), 0.7f + (h - 0.7f) / 2, z + sz * (d / 2 - 0.05f));
            foreach (var s in new[] { -1f, 1f })
            {
                Deco(Geo.Box(w + 0.12f, 0.22f, 0.12f), darkWoodM, x, h * 0.55f, z + s * (d / 2 + 0.03f)); Deco(Geo.Box(0.12f, 0.22f, d + 0.12f), darkWoodM, x + s * (w / 2 + 0.03f), h * 0.55f, z);
                Deco(Geo.Box(w + 0.12f, 0.2f, 0.12f), darkWoodM, x, h - 0.1f, z + s * (d / 2 + 0.03f)); Deco(Geo.Box(0.12f, 0.2f, d + 0.12f), darkWoodM, x + s * (w / 2 + 0.03f), h - 0.1f, z);
                float bh = h * 0.45f - 0.4f, bl = Mathf.Sqrt(bh * bh + 1.6f);
                foreach (var u in new[] { -0.36f, 0.36f }) { Deco(Geo.Box(0.14f, bl, 0.08f), darkWoodM, x + u * w, h * 0.55f + 0.2f + bh / 2, z + s * (d / 2 + 0.05f), 0, 0, u > 0 ? 0.6f : -0.6f); Deco(Geo.Box(0.08f, bl, 0.14f), darkWoodM, x + s * (w / 2 + 0.05f), h * 0.55f + 0.2f + bh / 2, z + u * d, u > 0 ? -0.6f : 0.6f, 0, 0); }
                // windows on both storeys of the long faces, shutters and snowy sills; one in eight lit by a dying lamp
                for (int k = 0; k < 2; k++)
                {
                    float u = (k - 0.5f) * w * 0.42f; bool lit = ((int)Mathf.Abs(x * 7 + z * 3 + k + s) & 7) == 0;
                    foreach (var wy in new[] { h * 0.32f, h * 0.76f })
                    {
                        Deco(Geo.Box(0.9f, 0.95f, 0.06f), lit && wy > h * 0.5f ? litGlassM : frostGlassM, x + u, wy, z + s * (d / 2 + 0.05f));
                        foreach (var sh in new[] { -0.62f, 0.62f }) Deco(Geo.Box(0.32f, 1.0f, 0.06f), woodM, x + u + sh, wy, z + s * (d / 2 + 0.07f));
                        Deco(Geo.Box(1.25f, 0.1f, 0.24f), snowM, x + u, wy - 0.52f, z + s * (d / 2 + 0.12f));
                    }
                }
            }
            // the door: a frame, the door, a step and a lamp over it
            Deco(Geo.Box(1.5f, 2.3f, 0.1f), darkWoodM, x, 0.7f + 1.15f, z + d / 2 + 0.05f); Deco(Geo.Box(1.15f, 2.05f, 0.1f), woodM, x, 0.7f + 1.02f, z + d / 2 + 0.09f);
            Deco(Geo.Box(1.8f, 0.35f, 0.7f), stoneDarkM, x, 0.17f, z + d / 2 + 0.6f); Lantern(levelRoot, x + 1.05f, 3.2f, z + d / 2 + 0.22f, ((int)Mathf.Abs(x + z) & 3) == 0);
            // the roof: a plaster gable (a prism) with slate slabs laid on it, overhanging, snow lying on top, a ridge of snow
            float span = ridgeX ? d : w, len = ridgeX ? w : d, rh = span * 0.42f, a = Mathf.Atan2(rh, span / 2), sl = Mathf.Sqrt(rh * rh + span * span / 4), ov = 0.7f;
            var gable = Deco(Prism(span, rh, len), plasterM, x, h, z, 0, ridgeX ? Mathf.PI / 2 : 0, 0);
            foreach (var s in new[] { -1f, 1f })
            {
                // slope mid-point, pushed out by half the overhang and lifted onto the gable
                float across = s * (span / 4 + Mathf.Cos(a) * ov / 2) + s * Mathf.Sin(a) * 0.14f, up = h + rh / 2 - Mathf.Sin(a) * ov / 2 + Mathf.Cos(a) * 0.14f;
                float across2 = across + s * Mathf.Sin(a) * 0.17f, up2 = up + Mathf.Cos(a) * 0.17f;
                if (ridgeX)
                {
                    Deco(Geo.Box(len + 0.9f, 0.26f, sl + ov), roofM, x, up, z + across, s * a, 0, 0);
                    Deco(Geo.Box(len + 0.8f, 0.14f, (sl + ov) * 0.92f), snowM, x, up2, z + across2, s * a, 0, 0);
                }
                else
                {
                    Deco(Geo.Box(sl + ov, 0.26f, len + 0.9f), roofM, x + across, up, z, 0, 0, -s * a);
                    Deco(Geo.Box((sl + ov) * 0.92f, 0.14f, len + 0.8f), snowM, x + across2, up2, z, 0, 0, -s * a);
                }
                // icicles along this eave
                float eave = span / 2 + Mathf.Cos(a) * ov * 0.9f, ey = h - Mathf.Sin(a) * ov * 0.9f - 0.05f;
                for (int k = 0; k < 7; k++)
                {
                    float u = (k / 6f - 0.5f) * (len + 0.4f), il = 0.35f + ((k * 7 + (int)Mathf.Abs(x)) % 5) * 0.16f;
                    Ignore(Deco(Geo.Cone(0.08f, il, 5), iceM, x + (ridgeX ? u : s * eave), ey - il / 2, z + (ridgeX ? s * eave : u), Mathf.PI, 0, 0));
                }
            }
            Deco(Geo.Box(ridgeX ? len + 0.9f : 0.5f, 0.3f, ridgeX ? 0.5f : len + 0.9f), snowM, x, h + rh + 0.12f, z);
            // the chimney
            float cx = x + (ridgeX ? w * 0.28f : w * 0.15f), cz = z + (ridgeX ? d * 0.12f : -d * 0.28f);
            Deco(Geo.Box(0.9f, rh + 1.6f, 0.9f), stoneDarkM, cx, h + (rh + 1.6f) / 2, cz); Deco(Geo.Box(1.1f, 0.2f, 1.1f), stoneM, cx, h + rh + 1.7f, cz); Deco(Geo.Box(1.0f, 0.18f, 1.0f), snowM, cx, h + rh + 1.86f, cz);
            SolidGable(x, z, h, span + Mathf.Cos(a) * ov * 2, rh + Mathf.Sin(a) * ov, len + 0.9f, ridgeX, Mathf.Sin(a) * ov);
        }
        // the volume of a pitched roof as stacked solid slabs, each as wide as the roof is at that height (so it can't be walked through)
        void SolidGable(float x, float z, float h, float span, float rh, float len, bool ridgeX, float drop = 0, int layers = 5)
        {
            float y0 = h - drop;
            for (int i = 0; i < layers; i++)
            {
                float a = (float)i / layers, b = (float)(i + 1) / layers, wdt = span * (1 - (a + b) / 2);
                float hw = ridgeX ? len / 2 : wdt / 2, hd = ridgeX ? wdt / 2 : len / 2;
                SolidAABB(V(x - hw, y0 + rh * a, z - hd), V(x + hw, y0 + rh * b, z + hd));
            }
        }
        void Well(float x, float z)
        {
            Deco(Geo.Cyl(1.5f, 1.6f, 1.1f, 14), stoneM, x, 0.55f, z); Deco(Geo.Cyl(1.6f, 1.6f, 0.15f, 14), stoneDarkM, x, 1.12f, z); Deco(Geo.Cyl(1.25f, 1.25f, 0.05f, 16), slickM, x, 1.0f, z);
            SolidAABB(V(x - 1.5f, 0, z - 1.5f), V(x + 1.5f, 1.15f, z + 1.5f));
            foreach (var s in new[] { -1f, 1f }) { Deco(Geo.Box(0.2f, 2.6f, 0.2f), woodM, x + s * 1.35f, 2.4f, z); SolidAABB(V(x + s * 1.35f - 0.12f, 1.1f, z - 0.12f), V(x + s * 1.35f + 0.12f, 3.7f, z + 0.12f)); }
            Deco(Geo.Cyl(0.12f, 0.12f, 2.6f, 8), woodM, x, 3.1f, z, 0, 0, Mathf.PI / 2); Deco(Geo.Cyl(0.2f, 0.16f, 0.35f, 8), woodM, x, 2.3f, z); Deco(Geo.Cyl(0.015f, 0.015f, 0.7f, 4), darkIronM, x, 2.75f, z); // the windlass, a frozen bucket
            Deco(Prism(3.2f, 1.1f, 2.2f), roofM, x, 3.6f, z, 0, Mathf.PI / 2, 0); Deco(Prism(2.9f, 0.5f, 2.1f), snowM, x, 4.25f, z, 0, Mathf.PI / 2, 0);
        }
        void Stall(float x, float z, float ry)
        {
            var p = Mk.Piv(levelRoot, x, 0, z); p.SetRot(0, ry, 0); var awn = Mats.Std(0x6a2a24, 0.9f, 0, 0, null, true);
            Mk.Msh(p, Geo.Box(3.2f, 1.0f, 1.4f), woodM, 0, 0.5f, 0);
            foreach (var a in new[] { -1.5f, 1.5f }) foreach (var b in new[] { -0.6f, 0.6f }) Mk.Msh(p, Geo.Box(0.12f, 2.4f, 0.12f), woodM, a, 1.2f, b);
            Mk.Msh(p, Geo.Box(3.5f, 0.08f, 1.9f), awn, 0, 2.45f, 0.1f, 0.2f, 0, 0); Mk.Msh(p, Geo.Box(3.5f, 0.12f, 1.9f), snowM, 0, 2.58f, 0.1f, 0.2f, 0, 0);
            for (int k = 0; k < 4; k++) Mk.Msh(p, Geo.Box(0.45f, 0.35f, 0.4f), Mats.Std(k % 2 == 0 ? 0x8a6a40 : 0x6a7a8a, 0.9f), -1.1f + k * 0.72f, 1.2f, 0);
            var w = Quaternion.Euler(0, ry * Mathf.Rad2Deg, 0) * V(1.75f, 0, 1.0f); SolidAABB(V(x - Mathf.Abs(w.x), 0, z - Mathf.Abs(w.z)), V(x + Mathf.Abs(w.x), 2.6f, z + Mathf.Abs(w.z)));
        }
        void FrostLampPost(float x, float z)
        {
            Deco(Geo.Cyl(0.1f, 0.15f, 4.4f, 6), darkIronM, x, 2.2f, z); Deco(Geo.Box(0.9f, 0.08f, 0.08f), darkIronM, x + 0.4f, 4.3f, z);
            Lantern(levelRoot, x + 0.8f, 3.95f, z, false); Deco(Geo.Box(0.36f, 0.08f, 0.36f), snowM, x + 0.8f, 4.22f, z);
            SolidAABB(V(x - 0.16f, 0, z - 0.16f), V(x + 0.16f, 4.4f, z + 0.16f));
        }
        void Stairs(float x, float z, float w, Vector3 dir, int n, float y0, float y1, float stepD = 1.6f, Material m = null)
        {
            for (int k = 0; k < n; k++)
            {
                float h = y0 + (y1 - y0) * (k + 1) / n; var c = V(x, 0, z) + dir * (k * stepD + stepD / 2);
                bool alongX = Mathf.Abs(dir.x) > 0.5f;
                Box(c.x, 0, c.z, alongX ? stepD : w, h, alongX ? w : stepD, m ?? M.block);
            }
        }
        void ChainLine(Vector3 a, Vector3 b, float sag, float link = 0.5f)
        {
            if (!darkIronM) darkIronM = Mats.Std(0x2a2a2e, 0.5f, 0.6f); // built outside the frost act (Dead Earth) too
            var mesh = Geo.Torus(0.28f * link / 0.5f, 0.07f * link / 0.5f, 10, 5);
            float len = Vector3.Distance(a, b); int n = Mathf.Max(2, (int)(len / (link * 1.1f)));
            for (int i = 0; i < n; i++)
            {
                float t = (i + 0.5f) / n; var p = Vector3.Lerp(a, b, t) - V(0, Mathf.Sin(t * Mathf.PI) * sag, 0);
                var t2 = Mathf.Min(1, t + 0.01f); var q = Vector3.Lerp(a, b, t2) - V(0, Mathf.Sin(t2 * Mathf.PI) * sag, 0);
                var g = Deco(mesh, darkIronM, p.x, p.y, p.z); g.layer = Mk.LayerIgnore;
                g.transform.localRotation = Quaternion.LookRotation((q - p).normalized) * Quaternion.Euler(0, 90, (i & 1) * 90);
            }
        }
        void HangingChain(float x, float z, float top, float bottom) => ChainLine(V(x, top, z), V(x + 0.001f, bottom, z + 0.001f), 0, 0.6f);

        // ---------- ruins ----------
        // a fluted column with a base and a capital; broken = snapped off, a jagged top and its drum lying beside it
        void FrostColumn(float x, float z, float r, float h, float y0 = 0, bool broken = false, float fallYaw = 0)
        {
            Deco(Geo.Box(r * 2.7f, 0.5f, r * 2.7f), stoneDarkM, x, y0 + 0.25f, z); Deco(Geo.Cyl(r * 1.2f, r * 1.3f, 0.4f, 16), stoneM, x, y0 + 0.7f, z);
            Deco(Geo.Cyl(r, r * 1.06f, h - 0.9f, 16), M.tower, x, y0 + 0.9f + (h - 0.9f) / 2, z);
            for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; Ignore(Deco(Geo.Box(0.1f, h - 1.3f, 0.1f), stoneDarkM, x + Mathf.Cos(a) * r * 0.98f, y0 + 0.9f + (h - 0.9f) / 2, z + Mathf.Sin(a) * r * 0.98f, 0, -a, 0)); }
            if (!broken)
            {
                Deco(Geo.Cyl(r * 1.35f, r, 0.7f, 16), stoneM, x, y0 + h + 0.35f, z); Deco(Geo.Box(r * 2.9f, 0.5f, r * 2.9f), stoneM, x, y0 + h + 0.95f, z); Deco(Geo.Box(r * 2.95f, 0.18f, r * 2.95f), snowM, x, y0 + h + 1.28f, z);
            }
            else
            {
                for (int k = 0; k < 6; k++) { float a = k * 1.05f + x; Deco(Geo.Cone(r * 0.42f, r * (0.5f + (k % 3) * 0.45f), 4), M.tower, x + Mathf.Cos(a) * r * 0.55f, y0 + h + 0.15f, z + Mathf.Sin(a) * r * 0.55f, 0, a, 0); }
                Deco(Geo.Cyl(r * 1.02f, r * 1.02f, 0.16f, 16), snowM, x, y0 + h + 0.03f, z);
                var fy = Quaternion.Euler(0, fallYaw * Mathf.Rad2Deg, 0) * V(0, 0, r * 2.6f + 1.4f);
                FrostDrum(V(x + fy.x, GroundAt(x + fy.x, z + fy.z, y0 + 0.5f) + r, z + fy.z), r, 2.6f, fallYaw + Mathf.PI / 2); // it lies on whatever it fell onto (off the podium edge it drops to the ground)
            }
            SolidAABB(V(x - r, y0, z - r), V(x + r, y0 + h + (broken ? 0.3f : 1.2f), z + r));
        }
        // a column drum lying on its side (solid)
        void FrostDrum(Vector3 c, float r, float len, float yaw)
        {
            var piv = Mk.Piv(levelRoot, c.x, c.y, c.z); piv.SetRot(0, yaw, 0);
            Mk.Msh(piv, Geo.Cyl(r, r, len, 16), M.tower, 0, 0, 0, Mathf.PI / 2, 0, 0);
            foreach (var ez in new[] { -len / 2, len / 2 }) Mk.Msh(piv, Geo.Cyl(r * 0.96f, r * 0.96f, 0.05f, 16), stoneDarkM, 0, 0, ez + Mathf.Sign(ez) * 0.02f, Mathf.PI / 2, 0, 0);
            Mk.Msh(piv, Geo.Box(r * 1.1f, 0.16f, len * 0.9f), snowM, 0, r * 0.92f, 0);
            var ext = Quaternion.Euler(0, yaw * Mathf.Rad2Deg, 0) * new Vector3(0, 0, len / 2); var hx = Mathf.Abs(ext.x) + r * 0.8f; var hz = Mathf.Abs(ext.z) + r * 0.8f;
            SolidAABB(V(c.x - hx, c.y - r, c.z - hz), V(c.x + hx, c.y + r * 0.9f, c.z + hz));
        }
        // a ruined wall: a run of masonry with a jagged, broken top
        void WallStub(float x, float z, float len, float h, bool alongX, float y0 = 0, Material m = null)
        {
            m = m ?? M.tower; int n = Mathf.Max(2, (int)(len / 1.6f)); var rng = new System.Random((int)(x * 3 + z * 11)); float R() => (float)rng.NextDouble();
            for (int k = 0; k < n; k++)
            {
                float u = ((k + 0.5f) / n - 0.5f) * len, hh = h * (0.45f + 0.55f * (float)Math.Abs(Math.Sin(k * 1.7f + x))) + R() * 0.6f, seg = len / n;
                Box(x + (alongX ? u : 0), y0, z + (alongX ? 0 : u), alongX ? seg : 1.1f, hh, alongX ? 1.1f : seg, m);
                Deco(Geo.Box(alongX ? seg * 1.02f : 1.15f, 0.14f, alongX ? 1.15f : seg * 1.02f), snowM, x + (alongX ? u : 0), y0 + hh + 0.05f, z + (alongX ? 0 : u));
            }
            for (int k = 0; k < 4; k++) Ignore(Deco(Geo.Box(0.5f + R() * 0.6f, 0.4f, 0.5f + R() * 0.5f), m, x + (alongX ? (R() - 0.5f) * len : (R() < 0.5f ? -1.2f : 1.2f)), y0 + 0.2f, z + (alongX ? (R() < 0.5f ? -1.2f : 1.2f) : (R() - 0.5f) * len), R(), R() * 3, R()));
        }
        void RubbleHeap(float x, float z, float s)
        {
            for (int k = 0; k < 6; k++) { float a = k * 1.1f; var g = Deco(Geo.Icosahedron(1), k % 2 == 0 ? M.tower : stoneDarkM, x + Mathf.Cos(a) * s * 0.5f, s * 0.25f, z + Mathf.Sin(a) * s * 0.5f, k, a, 0); g.transform.localScale = V(s * 0.45f, s * 0.3f, s * 0.4f); }
            Deco(Geo.Sphere(1, 10, 6), snowM, x, 0, z).transform.localScale = V(s * 0.9f, s * 0.35f, s * 0.8f);
            SolidAABB(V(x - s * 0.7f, 0, z - s * 0.7f), V(x + s * 0.7f, s * 0.5f, z + s * 0.7f));
        }
        void Obelisk(float x, float z, float h)
        {
            Box(x, 0, z, 2.6f, 0.6f, 2.6f, stoneDarkM); Box(x, 0.6f, z, 2.0f, 0.6f, 2.0f, M.block);
            Deco(Geo.Cyl(0.55f, 0.85f, h, 4), M.tower, x, 1.2f + h / 2, z, 0, Mathf.PI / 4, 0); Deco(Geo.Cone(0.55f * 1.41f / 1.41f, 1.2f, 4), M.tower, x, 1.2f + h + 0.6f, z, 0, Mathf.PI / 4, 0);
            for (int k = 0; k < 4; k++) { float a = k * Mathf.PI / 2; Ignore(Deco(Geo.Box(0.14f, h * 0.6f, 0.04f), frostRuneM, x + Mathf.Sin(a) * 0.5f, 1.2f + h * 0.45f, z + Mathf.Cos(a) * 0.5f, 0.03f * Mathf.Cos(a), a, -0.03f * Mathf.Sin(a))); }
            var g = Mk.Glow(levelRoot, 0x60e0ff, 3, 0.4f); g.transform.localPosition = V(x, 1.2f + h * 0.5f, z); flames.Add(g.transform);
            SolidAABB(V(x - 0.8f, 1.2f, z - 0.8f), V(x + 0.8f, 1.2f + h, z + 0.8f));
        }
        void RuinArch(float x, float z, float ry, float R, float y0 = 0)
        {
            foreach (var s in new[] { -1f, 1f }) { var o = Quaternion.Euler(0, ry * Mathf.Rad2Deg, 0) * V(s * R, 0, 0); Box(x + o.x, y0, z + o.z, 1.8f, R * 0.9f, 1.8f, M.tower); }
            Deco(Geo.Arc(R, 0.85f, 20), M.tower, x, y0 + R * 0.9f, z, 0, ry, 0); SolidArc(V(x, y0 + R * 0.9f, z), R, 0.85f, ry, 10);
            Deco(Geo.Box(1.9f, 0.8f, 1.9f), stoneM, x, y0 + R * 1.9f + 0.4f, z, 0, ry, 0); Deco(Geo.Box(2.0f, 0.2f, 2.0f), snowM, x, y0 + R * 1.9f + 0.9f, z, 0, ry, 0);
        }
        // a guardian of the old temple: a towering knight of grey stone on a stepped plinth, armoured skirt, a cloak, a great helm
        // with a burning visor slit, a kite shield and a spear. Returns the point of its visor (the statue fires from there).
        Vector3 GuardianStatue(float x, float z, float ry, float s = 1, float y0 = 0)
        {
            var st = Mats.Std(0xa4a6a8, 0.8f); var st2 = Mats.Std(0x80848a, 0.8f); var st3 = Mats.Std(0x5c6066, 0.85f); var eyeM = Mats.Basic(0x9af0ff);
            Box(x, y0, z, 5.0f * s, 0.6f * s, 5.0f * s, stoneDarkM); Box(x, y0 + 0.6f * s, z, 4.2f * s, 1.5f * s, 4.2f * s, M.block);
            Deco(Geo.Box(4.5f * s, 0.3f * s, 4.5f * s), st2, x, y0 + 2.25f * s, z);
            var q = Quaternion.Euler(0, ry * Mathf.Rad2Deg, 0); var pf = V(x, y0 + 1.35f * s, z) + q * V(0, 0, 2.12f * s);
            var plaque = Deco(Geo.Box(2.2f * s, 0.7f * s, 0.06f), frostRuneM, pf.x, pf.y, pf.z, 0, ry, 0); Ignore(plaque);
            float top = y0 + 2.4f * s;
            var p = Mk.Piv(levelRoot, x, top, z); p.SetRot(0, ry, 0);
            // legs: sabatons, greaves, knee cops
            foreach (var sx in new[] { -1f, 1f })
            {
                Mk.Msh(p, Geo.Box(0.8f * s, 0.45f * s, 1.3f * s), st3, sx * 0.6f * s, 0.22f * s, 0.2f * s);
                Mk.Msh(p, GeoX.Limb(0.4f * s, 0.34f * s, 1.5f * s), st, sx * 0.6f * s, 1.95f * s, 0);
                Mk.Msh(p, Geo.Sphere(0.45f * s, 10, 8), st2, sx * 0.6f * s, 2.0f * s, 0.12f * s);
                Mk.Msh(p, GeoX.Limb(0.46f * s, 0.4f * s, 1.3f * s), st, sx * 0.6f * s, 3.3f * s, 0);
            }
            // the armoured skirt: tassets hanging all round the hips, a heavy belt
            for (int k = 0; k < 10; k++) { float a = k * Mathf.PI / 5; Mk.Msh(p, Geo.Box(0.75f * s, 1.3f * s, 0.12f * s), k % 2 == 0 ? st2 : st3, Mathf.Sin(a) * 0.95f * s, 2.75f * s, Mathf.Cos(a) * 0.75f * s, 0.15f * Mathf.Cos(a), a, -0.15f * Mathf.Sin(a)); }
            Mk.Msh(p, Geo.Torus(1.0f * s, 0.16f * s, 20, 6), st3, 0, 3.45f * s, 0, Mathf.PI / 2, 0, 0).transform.localScale = V(1, 0.8f, 1);
            // the cuirass, its plates and a frost emblem on the chest; the cloak falling behind
            var tor = Mk.Msh(p, GeoX.Capsule(0.9f * s, 1.2f * s, 3, 14), st, 0, 4.55f * s, 0); tor.transform.localScale = V(1.25f, 1, 0.78f);
            for (int k = 0; k < 3; k++) Mk.Msh(p, Geo.Box((1.6f - k * 0.15f) * s, 0.12f * s, 0.2f * s), st2, 0, (3.75f + k * 0.28f) * s, 0.6f * s);
            Mk.Msh(p, Geo.Octahedron(0.35f * s), frostRuneM, 0, 4.8f * s, 0.74f * s).transform.localScale = V(1, 1.4f, 0.4f);
            Mk.Msh(p, Geo.Box(2.6f * s, 4.6f * s, 0.18f * s), st3, 0, 3.4f * s, -0.82f * s, -0.08f, 0, 0);
            Mk.Msh(p, Geo.Box(2.9f * s, 0.35f * s, 0.3f * s), st3, 0, 1.15f * s, -1.0f * s, -0.08f, 0, 0);
            // pauldrons in two layers, a gorget, the great helm with its visor slit and a crest
            foreach (var sx in new[] { -1f, 1f }) { Mk.Msh(p, Geo.Sphere(0.7f * s, 12, 8), st2, sx * 1.25f * s, 5.4f * s, 0).transform.localScale = V(1, 0.7f, 1); Mk.Msh(p, Geo.Sphere(0.6f * s, 12, 8), st, sx * 1.35f * s, 5.05f * s, 0).transform.localScale = V(1, 0.6f, 1); }
            Mk.Msh(p, Geo.Cyl(0.5f * s, 0.62f * s, 0.45f * s, 12), st2, 0, 5.85f * s, 0);
            Mk.Msh(p, Geo.Cyl(0.5f * s, 0.52f * s, 0.85f * s, 12), st, 0, 6.45f * s, 0); Mk.Msh(p, Geo.Sphere(0.51f * s, 12, 8, 0, Mathf.PI * 2, 0, Mathf.PI / 2), st, 0, 6.88f * s, 0);
            Mk.Msh(p, Geo.Box(0.7f * s, 0.07f * s, 0.05f), eyeM, 0, 6.55f * s, 0.5f * s); Mk.Msh(p, Geo.Box(0.07f * s, 0.4f * s, 0.05f), st3, 0, 6.35f * s, 0.51f * s);
            Mk.Msh(p, Geo.Box(0.14f * s, 0.55f * s, 1.1f * s), st3, 0, 7.3f * s, -0.05f * s);
            foreach (var sx in new[] { -1f, 1f }) { var wg = Mk.Piv(p, sx * 0.48f * s, 6.8f * s, -0.05f * s); wg.SetRot(-0.25f, 0, -sx * 0.75f); for (int k = 0; k < 3; k++) Mk.Msh(wg, Geo.Box(0.07f * s, (0.9f - k * 0.18f) * s, 0.28f * s), st2, 0, 0.42f * s, (-0.1f - k * 0.22f) * s, -k * 0.2f, 0, 0); }
            // the spear arm: hanging, forearm forward, the spear upright beside him
            var rs = Mk.Piv(p, 1.35f * s, 5.2f * s, 0); rs.SetRot(-0.1f, 0, 0.12f); Mk.Msh(rs, GeoX.Limb(0.34f * s, 0.29f * s, 1.3f * s), st, 0, 0, 0);
            var re = Mk.Piv(rs, 0, -1.3f * s, 0); re.SetRot(-1.45f, 0, 0); Mk.Msh(re, GeoX.Limb(0.29f * s, 0.27f * s, 1.15f * s), st, 0, 0, 0); Mk.Msh(re, Geo.Cyl(0.32f * s, 0.32f * s, 0.5f * s, 10), st2, 0, -0.55f * s, 0); Mk.Msh(re, Geo.Box(0.45f * s, 0.4f * s, 0.4f * s), st, 0, -1.25f * s, 0);
            var spearX = 1.5f * s; var sz0 = 1.4f * s;
            Mk.Msh(p, Geo.Cyl(0.11f * s, 0.11f * s, 9.4f * s, 8), st3, spearX, 4.5f * s, sz0); Mk.Msh(p, Geo.Cone(0.32f * s, 1.3f * s, 4), st2, spearX, 9.85f * s, sz0); Mk.Msh(p, Geo.Box(0.9f * s, 0.15f * s, 0.15f * s), st2, spearX, 9.2f * s, sz0);
            // the shield arm across the body, a long kite shield with a rim and a boss
            var ls = Mk.Piv(p, -1.35f * s, 5.2f * s, 0); ls.SetRot(-0.55f, 0, -0.1f); Mk.Msh(ls, GeoX.Limb(0.34f * s, 0.29f * s, 1.3f * s), st, 0, 0, 0);
            var le = Mk.Piv(ls, 0, -1.3f * s, 0); le.SetRot(-1.0f, 0, 0); Mk.Msh(le, GeoX.Limb(0.29f * s, 0.27f * s, 1.1f * s), st, 0, 0, 0);
            var sh = Mk.Piv(p, -1.55f * s, 3.9f * s, 1.15f * s); sh.SetRot(0.08f, 0.25f, 0);
            Mk.Msh(sh, Geo.Box(1.7f * s, 2.2f * s, 0.22f * s), st2, 0, 0.3f * s, 0); Mk.Msh(sh, Geo.Cyl(0.02f, 0.85f * s, 1.3f * s, 4), st2, 0, -1.45f * s, 0, Mathf.PI, Mathf.PI / 4, 0).transform.localScale = V(1, 1, 0.26f);
            Mk.Msh(sh, Geo.Box(1.85f * s, 0.16f * s, 0.26f * s), st3, 0, 1.45f * s, 0); foreach (var sx in new[] { -1f, 1f }) Mk.Msh(sh, Geo.Box(0.16f * s, 2.3f * s, 0.26f * s), st3, sx * 0.88f * s, 0.3f * s, 0);
            Mk.Msh(sh, Geo.Sphere(0.3f * s, 10, 8), st3, 0, 0.4f * s, 0.14f * s); Mk.Msh(sh, Geo.Box(0.08f * s, 1.6f * s, 0.04f), frostRuneM, 0, 0.1f * s, 0.13f * s);
            // snow on the shoulders, the helm, the shield top; frost creeping up a leg
            foreach (var sx in new[] { -1f, 1f }) Mk.Msh(p, Geo.Sphere(0.55f * s, 10, 5, 0, Mathf.PI * 2, 0, Mathf.PI * 0.38f), snowM, sx * 1.25f * s, 5.62f * s, 0);
            Mk.Msh(p, Geo.Sphere(0.42f * s, 10, 5, 0, Mathf.PI * 2, 0, Mathf.PI * 0.35f), snowM, 0, 7.05f * s, 0); Mk.Msh(sh, Geo.Box(1.75f * s, 0.12f * s, 0.3f * s), snowM, 0, 1.56f * s, 0);
            for (int k = 0; k < 4; k++) Mk.Msh(p, Geo.Cyl(0.02f, 0.2f * s, (0.7f + k * 0.25f) * s, 6), iceM, -0.6f * s + (k % 2) * 0.25f * s, (0.6f + k * 0.15f) * s, 0.42f * s, (k - 1.5f) * 0.2f, 0, 0);
            Deco(Geo.Box(4.3f * s, 0.16f * s, 4.3f * s), snowM, x, y0 + 2.46f * s, z);
            SolidAABB(V(x - 1.8f * s, top, z - 1.8f * s), V(x + 1.8f * s, top + 7.4f * s, z + 1.8f * s));
            return V(x, top, z) + q * V(0, 6.55f * s, 0.6f * s);
        }
        // the king of the old temple, carved kneeling on one knee, head bowed under his crown, both hands on a great sword
        // planted point-down before him; ice has grown over him where the roof fell in
        void KneelingKing(float x, float z, float ry, float s, float y0)
        {
            var st = Mats.Std(0xa29e94, 0.8f); var st2 = Mats.Std(0x7c7a74, 0.8f); var st3 = Mats.Std(0x5a5a58, 0.85f); var crownM = Mats.Std(0x9a9478, 0.5f, 0.3f);
            Box(x, y0, z, 8 * s, 0.6f * s, 7 * s, stoneDarkM); Deco(Geo.Box(8.3f * s, 0.2f * s, 7.3f * s), st2, x, y0 + 0.6f * s, z);
            float b = y0 + 0.7f * s;
            var p = Mk.Piv(levelRoot, x, b, z); p.SetRot(0, ry, 0);
            // the robe spread on the ground behind, the cloak falling from the shoulders
            Mk.Msh(p, GeoX.Lathe("kingRobe", GeoX.YR(2.4f, 0.9f, 1.6f, 1.4f, 0.6f, 2.1f, 0.0f, 2.5f), 16), st2, 0, 0, -0.5f * s).transform.localScale = V(s, s, s * 0.9f);
            Mk.Msh(p, Geo.Box(3.2f * s, 3.4f * s, 0.25f * s), st3, 0, 2.6f * s, -1.0f * s, -0.22f, 0, 0);
            // legs: the left knee up, foot planted; the right knee on the ground, shin behind
            Mk.Msh(p, GeoX.Capsule(0.45f * s, 1.3f * s, 3, 10), st, -0.65f * s, 1.55f * s, 0.6f * s, Mathf.PI / 2, 0, 0);
            Mk.Msh(p, GeoX.Limb(0.42f * s, 0.36f * s, 1.5f * s), st, -0.65f * s, 1.6f * s, 1.3f * s); Mk.Msh(p, Geo.Box(0.6f * s, 0.35f * s, 1.0f * s), st3, -0.65f * s, 0.17f * s, 1.5f * s);
            Mk.Msh(p, GeoX.Capsule(0.45f * s, 0.9f * s, 3, 10), st, 0.65f * s, 0.95f * s, 0.25f * s, -0.7f, 0, 0);
            Mk.Msh(p, GeoX.Capsule(0.4f * s, 1.1f * s, 3, 10), st, 0.65f * s, 0.35f * s, -0.7f * s, Mathf.PI / 2, 0, 0);
            // the body: a breastplate over the robe, a belt, the mantle
            var tor = Mk.Msh(p, GeoX.Capsule(0.95f * s, 1.1f * s, 3, 14), st, 0, 3.1f * s, 0.05f * s); tor.transform.localScale = V(1.22f, 1, 0.8f); tor.transform.localRotation = Quaternion.Euler(10, 0, 0);
            Mk.Msh(p, Geo.Torus(1.0f * s, 0.14f * s, 18, 5), st3, 0, 2.15f * s, 0.05f * s, Mathf.PI / 2, 0, 0);
            Mk.Msh(p, Geo.Cyl(0.95f * s, 1.25f * s, 0.6f * s, 14, true), st2, 0, 4.0f * s, -0.05f * s);
            // the head, bowed, bearded, crowned
            var hd = Mk.Piv(p, 0, 4.55f * s, 0.25f * s); hd.SetRot(0.45f, 0, 0);
            Mk.Msh(hd, Geo.Sphere(0.55f * s, 14, 10), st, 0, 0.35f * s, 0).transform.localScale = V(0.9f, 1.1f, 1);
            Mk.Msh(hd, Geo.Cyl(0.3f * s, 0.05f * s, 0.8f * s, 8), st, 0, -0.15f * s, 0.32f * s, 0.4f, 0, 0); // the beard
            Mk.Msh(hd, Geo.Cyl(0.5f * s, 0.48f * s, 0.25f * s, 14), crownM, 0, 0.78f * s, 0);
            for (int k = 0; k < 7; k++) { float a = k * Mathf.PI * 2 / 7; Mk.Msh(hd, Geo.Cone(0.1f * s, 0.45f * s, 4), crownM, Mathf.Sin(a) * 0.47f * s, 1.1f * s, Mathf.Cos(a) * 0.47f * s); }
            Mk.Msh(hd, Geo.Octahedron(0.12f * s), frostRuneM, 0, 0.8f * s, 0.5f * s);
            // the arms reaching down to the sword hilt, the sword planted before him
            foreach (var sx in new[] { -1f, 1f })
            {
                var sh = Mk.Piv(p, sx * 1.25f * s, 3.9f * s, 0.1f * s); sh.SetRot(-0.75f, 0, -sx * 0.25f);
                Mk.Msh(sh, Geo.Sphere(0.55f * s, 10, 8), st2, 0, 0, 0).transform.localScale = V(1, 0.75f, 1);
                Mk.Msh(sh, GeoX.Limb(0.33f * s, 0.28f * s, 1.3f * s), st, 0, 0, 0);
                var el = Mk.Piv(sh, 0, -1.3f * s, 0); el.SetRot(-0.75f, 0, sx * 0.2f); Mk.Msh(el, GeoX.Limb(0.28f * s, 0.24f * s, 1.2f * s), st, 0, 0, 0); Mk.Msh(el, Geo.Box(0.42f * s, 0.42f * s, 0.42f * s), st, 0, -1.25f * s, 0);
            }
            float swz = 2.3f * s;
            Mk.Msh(p, Geo.Box(0.5f * s, 3.4f * s, 0.12f * s), st2, 0, 1.0f * s, swz); Mk.Msh(p, Geo.Cyl(0.02f, 0.25f * s, 0.5f * s, 4), st2, 0, -0.85f * s, swz, Mathf.PI, Mathf.PI / 4, 0).transform.localScale = V(1, 1, 0.24f);
            Mk.Msh(p, Geo.Box(2.0f * s, 0.25f * s, 0.3f * s), st3, 0, 2.75f * s, swz); Mk.Msh(p, Geo.Cyl(0.14f * s, 0.14f * s, 0.9f * s, 8), st3, 0, 3.3f * s, swz); Mk.Msh(p, Geo.Sphere(0.24f * s, 10, 8), st2, 0, 3.85f * s, swz);
            Mk.Msh(p, Geo.Box(0.06f * s, 2.6f * s, 0.04f), frostRuneM, 0, 1.2f * s, swz + 0.08f * s);
            // snow on him, ice grown over the shoulder and the robe
            foreach (var sx in new[] { -1f, 1f }) Mk.Msh(p, Geo.Sphere(0.5f * s, 10, 5, 0, Mathf.PI * 2, 0, Mathf.PI * 0.35f), snowM, sx * 1.25f * s, 4.2f * s, 0.1f * s);
            Mk.Msh(p, Geo.Sphere(0.5f * s, 10, 5, 0, Mathf.PI * 2, 0, Mathf.PI * 0.35f), snowM, 0, 5.0f * s, 0.55f * s);
            for (int k = 0; k < 7; k++) { float a = k * 0.9f; Mk.Msh(p, Geo.Cyl(0.02f, 0.3f * s, (1.2f + (k % 3) * 0.6f) * s, 6), iceM, 1.7f * s + Mathf.Cos(a) * 0.5f * s, (0.6f + (k % 3) * 0.3f) * s, -1.2f * s + Mathf.Sin(a) * 0.6f * s, Mathf.Sin(a) * 0.4f, 0, -Mathf.Cos(a) * 0.4f); }
            var gl = Mk.Glow(levelRoot, 0x80d8ff, 6 * s, 0.25f); gl.transform.localPosition = V(x, b + 3 * s, z);
            var hw = 2.4f * s; SolidAABB(V(x - hw, b, z - hw), V(x + hw, b + 5.8f * s, z + hw));
        }
        // the king of the old temple, carved seated on his throne. His head broke off when the roof came down: it lies at his feet,
        // crown and all, half buried in the snow; ice has grown out of the broken neck.
        void SeatedFallenKing(float x, float z, float ry, float s, float y0)
        {
            var st = Mats.Std(0xa6a298, 0.82f); var st2 = Mats.Std(0x7e7b74, 0.85f); var st3 = Mats.Std(0x5c5a56, 0.9f); var crownM = Mats.Std(0x9e9476, 0.45f, 0.35f);
            var q = Quaternion.Euler(0, ry * Mathf.Rad2Deg, 0);
            Box(x, y0, z, 9.2f * s, 0.5f * s, 8.4f * s, stoneDarkM); Box(x, y0 + 0.5f * s, z - 0.6f * s, 7.6f * s, 0.4f * s, 6.6f * s, st2);
            float b = y0 + 0.9f * s;
            var p = Mk.Piv(levelRoot, x, b, z); p.SetRot(0, ry, 0);
            // the throne: a carved seat, a tall back with a gabled crest and finials, a relief panel, armrests ending in beasts' heads
            Mk.Msh(p, Geo.Box(5.4f * s, 2.2f * s, 3.8f * s), st2, 0, 1.1f * s, -0.9f * s);
            Mk.Msh(p, Geo.Box(5.8f * s, 7.6f * s, 0.9f * s), st2, 0, 3.8f * s, -2.7f * s);
            Mk.Msh(p, Prism(5.8f * s, 1.6f * s, 0.9f * s), st2, 0, 7.6f * s, -2.7f * s);
            foreach (var sx in new[] { -1f, 1f }) { Mk.Msh(p, Geo.Cyl(0.3f * s, 0.4f * s, 8.4f * s, 8), st3, sx * 2.9f * s, 4.2f * s, -2.7f * s); Mk.Msh(p, Geo.Cone(0.45f * s, 1.3f * s, 8), st3, sx * 2.9f * s, 9.05f * s, -2.7f * s); Mk.Msh(p, Geo.Sphere(0.28f * s, 8, 6), st3, sx * 2.9f * s, 8.4f * s, -2.7f * s); }
            Mk.Msh(p, Geo.Box(3.6f * s, 4.2f * s, 0.1f * s), st3, 0, 4.6f * s, -2.22f * s);
            Mk.Msh(p, Geo.Box(0.12f * s, 3.6f * s, 0.04f), frostRuneM, 0, 4.6f * s, -2.16f * s); Mk.Msh(p, Geo.Torus(0.8f * s, 0.06f * s, 20, 4), frostRuneM, 0, 5.4f * s, -2.16f * s);
            foreach (var sx in new[] { -1f, 1f })
            {
                Mk.Msh(p, Geo.Box(0.9f * s, 1.3f * s, 4.0f * s), st2, sx * 2.75f * s, 2.85f * s, -0.8f * s);
                var bh = Mk.Piv(p, sx * 2.75f * s, 3.3f * s, 1.35f * s);
                Mk.Msh(bh, Geo.Sphere(0.55f * s, 10, 8), st, 0, 0, 0).transform.localScale = V(0.9f, 0.85f, 1.1f);
                Mk.Msh(bh, Geo.Box(0.5f * s, 0.3f * s, 0.5f * s), st, 0, -0.15f * s, 0.45f * s);
                foreach (var ex in new[] { -1f, 1f }) Mk.Msh(bh, Geo.Cone(0.1f * s, 0.45f * s, 5), st3, ex * 0.3f * s, 0.45f * s, -0.1f * s, -0.5f, 0, -ex * 0.4f);
                for (int k = 0; k < 3; k++) Mk.Msh(p, Geo.Cone(0.06f * s, (0.4f + k * 0.2f) * s, 5), iceM, sx * 2.75f * s + (k - 1) * 0.25f * s, 2.0f * s, 0.9f * s, Mathf.PI, 0, 0); // icicles off the armrest
            }
            // the seated figure: feet in sandals, the robe falling over the shins, the lap, the belted body, the mantle
            foreach (var sx in new[] { -1f, 1f })
            {
                Mk.Msh(p, Geo.Box(0.75f * s, 0.35f * s, 1.3f * s), st3, sx * 0.85f * s, 0.18f * s, 1.55f * s);
                Mk.Msh(p, GeoX.Capsule(0.62f * s, 1.6f * s, 3, 12), st, sx * 0.85f * s, 2.55f * s, 0.15f * s, Mathf.PI / 2, 0, 0);
            }
            Mk.Msh(p, GeoX.Lathe("kingShinRobe", GeoX.YR(0, 1.25f, -0.9f, 1.4f, -1.9f, 1.62f, -2.2f, 1.66f), 18), st2, 0, 2.3f * s, 1.25f * s).transform.localScale = V(s * 1.15f, s, s * 0.55f);
            Mk.Msh(p, Geo.Box(2.6f * s, 0.3f * s, 2.0f * s), st2, 0, 3.05f * s, 0.5f * s, 0.05f, 0, 0); // the robe over the lap
            var tor = Mk.Msh(p, GeoX.Capsule(1.1f * s, 1.9f * s, 3, 14), st, 0, 4.7f * s, -1.1f * s); tor.transform.localScale = V(1.22f, 1, 0.78f);
            Mk.Msh(p, Geo.Torus(1.15f * s, 0.16f * s, 20, 5), st3, 0, 3.75f * s, -1.05f * s, Mathf.PI / 2, 0, 0).transform.localScale = V(1.15f, 0.78f, 1);
            Mk.Msh(p, Geo.Octahedron(0.28f * s), frostRuneM, 0, 3.75f * s, -0.15f * s).transform.localScale = V(1, 1.3f, 0.4f);
            Mk.Msh(p, Geo.Cyl(1.05f * s, 1.55f * s, 2.2f * s, 14, true), st2, 0, 5.6f * s, -1.15f * s); // the mantle
            foreach (var sx in new[] { -1f, 1f }) Mk.Msh(p, Geo.Sphere(0.7f * s, 12, 8), st2, sx * 1.35f * s, 6.1f * s, -1.1f * s).transform.localScale = V(1, 0.7f, 1);
            // the arms resting on the armrests, hands curled round the beasts' heads
            foreach (var sx in new[] { -1f, 1f })
            {
                var sh = Mk.Piv(p, sx * 1.55f * s, 6.0f * s, -1.1f * s); sh.SetRot(-0.25f, 0, sx * 0.18f);
                Mk.Msh(sh, GeoX.Limb(0.5f * s, 0.42f * s, 2.3f * s), st, 0, 0, 0);
                var el = Mk.Piv(sh, 0, -2.3f * s, 0); el.SetRot(-1.3f, 0, -sx * 0.18f);
                Mk.Msh(el, GeoX.Limb(0.42f * s, 0.36f * s, 2.2f * s), st, 0, 0, 0);
                Mk.Msh(el, Geo.Box(0.6f * s, 0.35f * s, 0.75f * s), st, 0, -2.3f * s, 0.1f * s);
                for (int k = 0; k < 4; k++) Mk.Msh(el, Geo.Cyl(0.08f * s, 0.07f * s, 0.4f * s, 6), st, (k - 1.5f) * 0.14f * s, -2.45f * s, 0.35f * s, 0.6f, 0, 0);
            }
            // the broken neck: a ragged stump, darker where it snapped, ice grown out of the break, snow lying on it
            Mk.Msh(p, Geo.Cyl(0.7f * s, 0.85f * s, 0.7f * s, 12), st, 0, 6.85f * s, -1.05f * s);
            Mk.Msh(p, Geo.Cyl(0.68f * s, 0.68f * s, 0.05f * s, 12), st3, 0, 7.22f * s, -1.05f * s);
            for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; Mk.Msh(p, Geo.Cone(0.16f * s, (0.25f + (k % 3) * 0.2f) * s, 4), st, Mathf.Cos(a) * 0.55f * s, 7.3f * s, -1.05f * s + Mathf.Sin(a) * 0.55f * s, 0, a, 0); }
            for (int k = 0; k < 4; k++) { float a = k * 1.6f; Mk.Msh(p, Geo.Cyl(0.02f, 0.13f * s, (0.7f + (k % 2) * 0.5f) * s, 6), iceM, Mathf.Cos(a) * 0.25f * s, 7.5f * s, -1.05f * s + Mathf.Sin(a) * 0.25f * s, Mathf.Sin(a) * 0.4f, 0, -Mathf.Cos(a) * 0.4f); }
            foreach (var sx in new[] { -1f, 1f }) Mk.Msh(p, Geo.Sphere(0.6f * s, 10, 5, 0, Mathf.PI * 2, 0, Mathf.PI * 0.38f), snowM, sx * 1.35f * s, 6.45f * s, -1.1f * s);
            Mk.Msh(p, Geo.Box(2.4f * s, 0.16f * s, 1.8f * s), snowM, 0, 3.25f * s, 0.6f * s);
            // the head, fallen: crowned, bearded, a stern face, lying on its side by his feet, sunk in a drift; the crown's points snapped
            var hp = Mk.Piv(p, 2.6f * s, -0.9f * s + 0.86f * s, 4.4f * s); // resting on the floor (b is the dais top; the floor is 0.9s below) hp.SetRot(0.15f, -0.7f, 1.45f);
            Mk.Msh(hp, Geo.Sphere(1.0f * s, 16, 12), st, 0, 0, 0).transform.localScale = V(0.88f, 1.08f, 1.0f);
            Mk.Msh(hp, Geo.Box(1.4f * s, 0.3f * s, 0.5f * s), st, 0, 0.32f * s, 0.75f * s, 0.25f, 0, 0);                    // brow
            Mk.Msh(hp, Geo.Cone(0.2f * s, 0.6f * s, 5), st, 0, 0.0f, 1.0f * s, Mathf.PI / 2 + 0.25f, 0, 0);                    // nose
            foreach (var sx in new[] { -1f, 1f }) { Mk.Msh(hp, Geo.Sphere(0.17f * s, 8, 6), st3, sx * 0.36f * s, 0.12f * s, 0.86f * s).transform.localScale = V(1.2f, 0.55f, 0.5f); Mk.Msh(hp, Geo.Sphere(0.25f * s, 8, 6), st, sx * 0.6f * s, -0.15f * s, 0.62f * s); }
            Mk.Msh(hp, Geo.Box(0.6f * s, 0.08f * s, 0.15f * s), st3, 0, -0.38f * s, 0.88f * s);                                // the mouth set hard
            Mk.Msh(hp, Geo.Cyl(0.55f * s, 0.05f * s, 1.3f * s, 10), st, 0, -0.95f * s, 0.55f * s, 0.35f, 0, 0);                 // the beard
            for (int k = 0; k < 5; k++) Mk.Msh(hp, Geo.Box(0.12f * s, 0.9f * s, 0.06f * s), st2, (k - 2) * 0.18f * s, -0.9f * s, 0.82f * s, 0.3f, 0, (k - 2) * 0.06f);
            Mk.Msh(hp, Geo.Cyl(0.98f * s, 0.92f * s, 0.42f * s, 16), crownM, 0, 0.82f * s, -0.05f * s);
            for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; if (k == 2 || k == 5) continue; Mk.Msh(hp, Geo.Cone(0.16f * s, (k % 2 == 0 ? 0.7f : 0.45f) * s, 4), crownM, Mathf.Sin(a) * 0.9f * s, 1.25f * s, Mathf.Cos(a) * 0.9f * s); }
            Mk.Msh(hp, Geo.Octahedron(0.16f * s), frostRuneM, 0, 0.9f * s, 0.92f * s);
            var hw = V(x, y0, z) + q * V(2.6f * s, 0, 4.4f * s);
            Ignore(Deco(Geo.Sphere(1, 10, 6), snowM, hw.x, y0 + 0.05f, hw.z)).transform.localScale = V(2.2f * s, 0.55f * s, 1.8f * s);
            foreach (var (ox, oz) in new[] { (1.4f, 1.0f), (-0.8f, 1.6f) }) { var cp = hw + q * V(ox * s, 0, oz * s); Ignore(Deco(Geo.Cone(0.16f * s, 0.7f * s, 4), crownM, cp.x, y0 + 0.15f * s, cp.z, Mathf.PI / 2, ox, 0)); } // the snapped crown points
            for (int k = 0; k < 6; k++) { var cp = hw + q * V((k - 2.5f) * 0.8f * s, 0, -1.4f * s + (k % 2) * 0.5f * s); Ignore(Deco(Geo.Icosahedron(1), st, cp.x, y0 + 0.15f * s, cp.z, k, k * 2, 0)).transform.localScale = Vector3.one * (0.2f + (k % 3) * 0.1f) * s; } // chips of the neck
            var gl = Mk.Glow(levelRoot, 0x80d8ff, 7 * s, 0.22f); gl.transform.localPosition = V(x, b + 4 * s, z);
            var hd = q * V(3.2f * s, 0, 3.2f * s); SolidAABB(V(x - Mathf.Abs(hd.x), b, z - 4.2f * s), V(x + Mathf.Abs(hd.x), b + 8 * s, z + 2.2f * s));
            SolidAABB(V(hw.x - 1.05f * s, y0, hw.z - 1.05f * s), V(hw.x + 1.05f * s, y0 + 1.9f * s, hw.z + 1.05f * s));
        }        // a demon skull carved in relief, staring out from a pillar or wall (eyes burn cold blue)
        void DemonRelief(Vector3 p, float ry, float s)
        {
            var piv = Mk.Piv(levelRoot, p.x, p.y, p.z); piv.SetRot(0, ry, 0); var stone = M.tower; var eye = Mats.Basic(0x80f0ff);
            Mk.Msh(piv, Geo.Sphere(0.9f * s, 12, 10), stone, 0, 0, 0.3f * s).transform.localScale = V(1, 1.1f, 0.6f) * 1;
            Mk.Msh(piv, Geo.Box(1.3f * s, 0.35f * s, 0.6f * s), stone, 0, 0.35f * s, 0.75f * s);
            foreach (var sx in new[] { -1f, 1f })
            {
                Mk.Msh(piv, Geo.Box(0.3f * s, 0.16f * s, 0.1f * s), eye, sx * 0.32f * s, 0.12f * s, 0.86f * s);
                var h = Mk.Piv(piv, sx * 0.7f * s, 0.6f * s, 0.4f * s); h.SetRot(0, 0, -sx * 0.6f);
                for (int k = 0; k < 4; k++) { Mk.Msh(h, Geo.Cyl(0.18f * s * (1 - k / 4f), 0.18f * s * (1 - (k + 1) / 4f) + 0.01f, 0.6f * s, 6), stone, 0, 0.3f * s, 0); h = Mk.Piv(h, 0, 0.6f * s, 0); h.SetRot(0, 0, -sx * 0.35f); }
            }
            for (int k = 0; k < 4; k++) Mk.Msh(piv, Geo.Cone(0.08f * s, 0.35f * s, 4), M.bone, (k - 1.5f) * 0.22f * s, -0.55f * s, 0.85f * s, Mathf.PI, 0, 0);
            var g = Mk.Glow(levelRoot, 0x60d8ff, 2.2f * s, 0.45f); g.transform.localPosition = p + Quaternion.Euler(0, ry * Mathf.Rad2Deg, 0) * V(0, 0.12f * s, 1.1f * s);
        }
        void FrostBrazier(float x, float z, float y0 = 0)
        {
            Deco(Geo.Cyl(0.35f, 0.6f, 2.2f, 8), darkIronM, x, y0 + 1.1f, z); Deco(Geo.Cyl(1.0f, 0.5f, 0.6f, 10, true), darkIronM, x, y0 + 2.4f, z);
            foreach (var a in new[] { 0f, 2.1f, 4.2f }) Deco(Geo.Cone(0.08f, 0.6f, 4), darkIronM, x + Mathf.Cos(a) * 0.95f, y0 + 2.9f, z + Mathf.Sin(a) * 0.95f);
            var f = Mk.Glow(levelRoot, 0x60c8ff, 2.6f, 0.9f); f.transform.localPosition = V(x, y0 + 3.1f, z); flames.Add(f.transform);
            var f2 = Mk.Glow(levelRoot, 0xe0f8ff, 1.2f, 0.9f); f2.transform.localPosition = V(x, y0 + 2.9f, z); flames.Add(f2.transform);
            SolidAABB(V(x - 0.6f, y0, z - 0.6f), V(x + 0.6f, y0 + 2.7f, z + 0.6f));
            anims.Add((t, dt) => { if (rand < 0.35f) SpawnParticles(V(x + (rand - 0.5f) * 0.6f, y0 + 3.0f, z + (rand - 0.5f) * 0.6f), 1, 1.2f, cyanMat, 0.07f, 0.9f, -3); });
        }

        // ---------- the frost skies and backdrops ----------
        bool FrostSky(string kind, float Bd)
        {
            switch (kind)
            {
                case "snowstorm":
                {
                    var s = Mk.Glow(levelRoot, 0xe0e8f0, 160, 0.35f, false); s.transform.localPosition = V(-40, 90, -220);
                    for (int i = 0; i < 26; i++) { float a = i / 26f * Mathf.PI * 2, r = Bd + 30 + rand * 60; var c = Mk.Glow(levelRoot, 0xb8c2cc, 60 + rand * 50, 0.5f, false); c.transform.localPosition = V(Mathf.Cos(a) * r, 30 + rand * 30, Mathf.Sin(a) * r); }
                    return true;
                }
                case "dusk":
                {
                    var sun = Mk.Glow(levelRoot, 0xffa070, 120, 0.75f, false); sun.transform.localPosition = V(120, 18, -230);
                    var core = Mk.Glow(levelRoot, 0xffe0c0, 40, 0.9f, false); core.transform.localPosition = V(120, 18, -230);
                    for (int i = 0; i < 50; i++) { var dir = Quaternion.Euler(-25 - rand * 60, rand * 360, 0) * Vector3.forward; var st = Mk.Glow(levelRoot, 0xe8f4ff, 1 + rand * 1.4f, 0.8f, false); st.transform.localPosition = dir * 320; }
                    return true;
                }
                case "glacier":
                {
                    for (int i = 0; i < 60; i++) { var dir = Quaternion.Euler(-15 - rand * 70, rand * 360, 0) * Vector3.forward; var st = Mk.Glow(levelRoot, 0xd8ecff, 1.2f + rand * 1.6f, 0.9f, false); st.transform.localPosition = dir * 320; }
                    var moon = Mk.Glow(levelRoot, 0x80c8ff, 80, 0.7f, false); moon.transform.localPosition = V(60, 140, -220);
                    return true;
                }
                case "abyss":
                {
                    var eye = Mk.Glow(levelRoot, 0x4060ff, 220, 0.35f, false); eye.transform.localPosition = V(0, 120, -260);
                    var eye2 = Mk.Glow(levelRoot, 0x90e0ff, 60, 0.6f, false); eye2.transform.localPosition = V(0, 120, -260);
                    for (int i = 0; i < 8; i++) { float a = i / 8f * Mathf.PI * 2; var g = Mk.Glow(levelRoot, 0x2a1840, 140, 0.5f, false); g.transform.localPosition = V(Mathf.Cos(a) * 230, 40, Mathf.Sin(a) * 230); }
                    return true;
                }
            }
            return false;
        }
        bool FrostBackdrop(string kind, float Bd)
        {
            var rng = new System.Random(kind.GetHashCode()); float R() => (float)rng.NextDouble();
            switch (kind)
            {
                case "snowstorm":
                {
                    var hill = Silhouette(0x3a424c, 0.85f); var roofs = Silhouette(0x2a2e34, 0.9f);
                    for (int i = 0; i < 16; i++) { float a = i / 16f * Mathf.PI * 2, r = 160 + R() * 50; Deco(Geo.Cone(40 + R() * 30, 30 + R() * 40, 6), hill, Mathf.Cos(a) * r, 10, Mathf.Sin(a) * r, 0, R() * 3, 0); }
                    for (int i = 0; i < 22; i++) { float a = i / 22f * Mathf.PI * 2, r = 110 + R() * 30; var p = V(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r); Deco(Geo.Box(10 + R() * 8, 8 + R() * 8, 10), roofs, p.x, 5, p.z, 0, a, 0); Deco(Prism(11, 6, 10), roofs, p.x, 9 + R() * 4, p.z, 0, a, 0); }
                    return true;
                }
                case "dusk":
                {
                    var mt = Silhouette(0x3c4a5c); var snow = Silhouette(0x8aa0b8);
                    for (int i = 0; i < 16; i++) { float a = i / 16f * Mathf.PI * 2 + R() * 0.3f, r = 170 + R() * 60, h = 60 + R() * 70, w = 30 + R() * 25; var p = V(Mathf.Cos(a) * r, -5, Mathf.Sin(a) * r); Deco(Geo.Cone(w, h, 6), mt, p.x, h / 2 - 5, p.z, 0, R() * 3, 0); Deco(Geo.Cone(w * 0.3f, h * 0.3f, 6), snow, p.x, h - 5 - h * 0.15f, p.z, 0, R() * 3, 0); }
                    var ruin = Silhouette(0x2a2a30, 0.9f);
                    for (int i = 0; i < 10; i++) { float a = i / 10f * Mathf.PI * 2, r = 120; Deco(Geo.Cyl(3, 3, 20 + R() * 25, 8), ruin, Mathf.Cos(a) * r, 15, Mathf.Sin(a) * r); }
                    return true;
                }
                case "glacier":
                {
                    var ice = Silhouette(0x2a4a68, 0.9f); var ice2 = Silhouette(0x5a8ab0, 0.7f);
                    for (int i = 0; i < 24; i++) { float a = i / 24f * Mathf.PI * 2, r = 150 + R() * 70, h = 50 + R() * 110; Deco(Geo.Octahedron(1), i % 2 == 0 ? ice : ice2, Mathf.Cos(a) * r, h * 0.4f, Mathf.Sin(a) * r, 0, R() * 3, 0).transform.localScale = V(14 + R() * 10, h * 0.6f, 14 + R() * 10); }
                    return true;
                }
                case "abyss":
                {
                    var dark = Silhouette(0x0c0812); var ice = Silhouette(0x203048, 0.8f);
                    for (int i = 0; i < 20; i++) { float a = i / 20f * Mathf.PI * 2, r = 150 + R() * 60, h = 70 + R() * 90; Deco(Geo.Cone(18 + R() * 12, h, 5), dark, Mathf.Cos(a) * r, h / 2 - 10, Mathf.Sin(a) * r, 0, R() * 3, 0); }
                    // something vast, chained in the dark behind the throne
                    var ch = Silhouette(0x14101c, 0.95f);
                    Deco(Geo.Box(50, 90, 30), ch, 0, 50, -250); Deco(Geo.Sphere(22, 12, 10), ch, 0, 110, -250);
                    foreach (var sx in new[] { -1f, 1f }) { Deco(Geo.Cone(8, 50, 6), ch, sx * 20, 140, -250, 0, 0, -sx * 0.7f); Deco(Geo.Box(80, 6, 6), ice, sx * 70, 70, -240, 0, 0, sx * 0.3f); }
                    return true;
                }
            }
            return false;
        }
    }
}
