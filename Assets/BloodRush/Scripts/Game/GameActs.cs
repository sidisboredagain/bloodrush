using System;
using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    // The middle acts, between Hell (1-5) and Heaven (21-25):
    //   ACT II  THE FROZEN CIRCLE (6-10)  the iced-over pit where traitors are frozen - black ice, blue light, blizzards
    //    6 THE FROZEN LAKE       thin ice - plates crack, then give way to freezing water
    //    7 GLACIER CRYPTS        falling icicles - a blue shadow marks where they land
    //    8 WHITEOUT PASS         blizzard gusts - the wind shoves you and the far end goes white; shelter behind rock
    //    9 THE SHATTERED FORTRESS shard bursts - ice crystals swell, then burst in a ring of shards; jump it
    //   10 THRONE OF FROST       frost spikes - plates flash, then ice spears punch up
    //   ACT III DEAD EARTH (11-15)        the surface after the end - rust, ash snow, a red sky, dead neon
    //   11 ASHFALL CITY          burning wrecks - cars burn hotter, then explode
    //   12 THE DROWNED SUBWAY    trains - headlights flare at the tunnel mouth, then a train tears through
    //   13 OVERPASS GRAVEYARD    collapsing road - the elevated decks crack, drop away, then grind back up
    //   14 THE CATHEDRAL OF RUST piston crushers + conveyor belts
    //   15 SKYLINE ROOFTOPS      lightning storm
    //   ACT IV PURGATORY (16-20)          between death and judgement - endless fog, grey stone, old gold
    //   16 THE WAITING SHORE     the tide - grey water floods the low ground, slowing and dragging you
    //   17 CLOCKWORK HALLS       pendulums - bladed pendulums sweep the hall
    //   18 THE LIBRARY OF SOULS  time bubbles - inside, everything crawls (enemies and their shots too)
    //   19 MIRROR WASTES         mirror glints - the great mirrors trace you, then fire the sun at you
    //   20 THE HALL OF HOURS     boss: the Warden of Hours (GameWarden) - no hazard
    public partial class Game
    {
        static void AddActThemes()
        {
            if (THEMES.ContainsKey("frost")) return;
            THEMES["frost"] = new Theme { holy = false, bg = 0x0b1824, fogNear = 30, fogFar = 160, hemiSky = 0xa8d4ff, hemiGround = 0x14202c, hemiI = 1.55f, sunC = 0xd8ecff, sunI = 1.9f,
                floor = new[] { "#8eaec4", "#5a7c94", null, "icecracks" }, wall = new[] { "#34505f", "#182a36", null, "icecracks" }, block = new[] { "#b4d0e2", "#6a8ca2", "#60d0ff", null }, tower = new[] { "#2a3e50", "#121e2a", "#60c8ff", "icecracks" },
                trim = 0x60d0ff, pool = "ice", crystal = 0x90e4ff, crystalEm = 0x104060, portal = 0x60c8ff, portal2 = 0xffffff, warp = new[] { "#ffffff", "#60c8ff" },
                ambient = "snow", wallH = 22, deco = "frost", sky = "aurora", banner = "#80d8ff" };
            THEMES["ruin"] = new Theme { holy = false, bg = 0x2a1410, fogNear = 26, fogFar = 150, hemiSky = 0xffa888, hemiGround = 0x302018, hemiI = 1.55f, sunC = 0xffc090, sunI = 2.0f,
                floor = new[] { "#4a4542", "#2a2624", null, "crackdark" }, wall = new[] { "#5a4a40", "#2a2018", null, null }, block = new[] { "#6a5a4c", "#3a2e24", "#ff8a20", null }, tower = new[] { "#3a3430", "#1a1614", "#ff7a20", "crackdark" },
                trim = 0xff8a20, pool = "toxic", crystal = 0xff9030, crystalEm = 0x803000, portal = 0xff7020, portal2 = 0xffe0a0, warp = new[] { "#ffe0a0", "#ff6010" },
                ambient = "ash", wallH = 18, deco = "ruin", sky = "smog", banner = "#ff9a40" };
            THEMES["limbo"] = new Theme { holy = false, bg = 0x8c8e92, fogNear = 20, fogFar = 125, hemiSky = 0xeae6de, hemiGround = 0x50505a, hemiI = 1.35f, sunC = 0xfff0d0, sunI = 1.6f,
                floor = new[] { "#9c9a92", "#6c6a62", null, "veins" }, wall = new[] { "#7a7872", "#4a4844", "#c8a050", "veins" }, block = new[] { "#acaaa2", "#6a6860", "#d8b050", null }, tower = new[] { "#6a6862", "#3a3834", "#d8b050", "veins" },
                trim = 0xe0b850, pool = "void", crystal = 0xffe8a0, crystalEm = 0x806020, portal = 0xe8d8a0, portal2 = 0xffffff, warp = new[] { "#ffffff", "#d8c890" },
                ambient = "fogmote", wallH = 14, deco = "limbo", sky = "fog", banner = "#e8d8a0" };
        }

        // ---------- textures painted once ----------
        static Texture2D facadeTex, booksTex, clockTex;
        static Texture2D FacadeTex()
        {
            if (facadeTex) return facadeTex;
            var c = new Cv(64, 64); var rng = new System.Random(5);
            c.Fill(Mats.Css("#2a2420"));
            for (int fy = 0; fy < 2; fy++) for (int fx = 0; fx < 2; fx++)
            {
                float x = 4 + fx * 32, y = 6 + fy * 32; double r = rng.NextDouble();
                var col = r < 0.45 ? Mats.Css("#120e0c") : r < 0.75 ? Mats.Css("#ffb050") : r < 0.9 ? Mats.Css("#ffd890") : Mats.Css("#60c0ff");
                c.Rect(x, y, 24, 18, Mats.Css("#3a322c")); c.Rect(x + 2, y + 2, 20, 14, col);
                if (r < 0.75 && r >= 0.45) c.Rect(x + 11, y + 2, 2, 14, Mats.Css("#3a322c"));
                if (rng.NextDouble() < 0.3) c.Line(x + 3, y + 3, x + 20, y + 14, 1.5f, Mats.Css("#1a1410")); // a cracked pane
            }
            c.Rect(0, 30, 64, 2, Mats.Css("#1a1612"));
            return facadeTex = c.ToTex(true, true);
        }
        static Texture2D BooksTex()
        {
            if (booksTex) return booksTex;
            var c = new Cv(64, 64); var rng = new System.Random(9);
            string[] cols = { "#5a1a14", "#1a3a5a", "#3a4a1a", "#6a5a2a", "#2a1a3a", "#7a6a5a", "#4a2a14" };
            c.Fill(Mats.Css("#2a1e14"));
            for (int row = 0; row < 2; row++)
            {
                float y = 4 + row * 32; float x = 2;
                while (x < 62) { float w = 2 + (float)rng.NextDouble() * 4, h = 20 + (float)rng.NextDouble() * 6; var col = Mats.Css(cols[rng.Next(cols.Length)]); c.Rect(x, y + 26 - h, w, h, col); c.Rect(x, y + 26 - h + 3, w, 1, Mats.Css("#c8a050")); x += w + 0.5f; }
                c.Rect(0, y + 26, 64, 4, Mats.Css("#3a2a1a"));
            }
            return booksTex = c.ToTex(true, true);
        }
        static Texture2D ClockTex()
        {
            if (clockTex) return clockTex;
            var c = new Cv(128, 128);
            c.Circle(64, 64, 62, Mats.Css("#c8a050")); c.Circle(64, 64, 56, Mats.Css("#ece4d0"));
            for (int i = 0; i < 12; i++) { float a = i / 12f * Mathf.PI * 2; c.Line(64 + Mathf.Cos(a) * 44, 64 + Mathf.Sin(a) * 44, 64 + Mathf.Cos(a) * 52, 64 + Mathf.Sin(a) * 52, i % 3 == 0 ? 5 : 2.5f, Mats.Css("#2a2018")); }
            c.Circle(64, 64, 5, Mats.Css("#2a2018"));
            return clockTex = c.ToTex(false, false);
        }

        // ---------- walls, sky and backdrop for the new themes ----------
        // returns true when it dressed the arena walls (the caller falls back to heaven otherwise)
        bool ActWalls(string d, float Bd, float wallH)
        {
            if (d == "frost")
            {
                var ice = Mats.Std(0xc0e6ff, 0.1f, 0.1f, 0x0a2a40); var frozenBanner = Mats.Std(0x23384c, 0.9f, 0, 0x02080e, null, true); var bannerTrim = Mats.Std(0x8ab4d0, 0.5f, 0.3f);
                OnWalls(Bd, 10, (px, pz, nx, nz) =>
                {
                    var g = WallG(px, pz, nx, nz);
                    for (int k = 0; k < 4; k++) { float h = 1.5f + ((int)(px * 7 + pz * 3 + k * 11) & 3) * 0.9f; Mk.Msh(g, Geo.Cone(0.35f, h, 5), ice, (k - 1.5f) * 1.8f, wallH - h / 2, 0.4f, Mathf.PI, 0, 0); }
                    for (int k = 0; k < 3; k++) { float h = 1.2f + k * 0.8f; Mk.Msh(g, Geo.Cone(0.5f, h, 5), ice, (k - 1) * 1.1f, h / 2, 0.6f, 0.2f * (k - 1), 0, 0); }
                });
                OnWalls(Bd, 28, (px, pz, nx, nz) =>
                {
                    var g = WallG(px, pz, nx, nz);
                    Mk.Msh(g, Geo.Box(0.5f, 1.2f, 0.4f), M.metal, 0, 7, 0.2f); Mk.Msh(g, Geo.Octahedron(0.4f), ice, 0, 7.9f, 0.45f);
                    var f = Mk.Glow(g, 0x80d8ff, 2.4f, 0.8f); f.transform.localPosition = V(0, 8, 0.6f); flames.Add(f.transform);
                    Mk.Msh(g, Geo.Box(3.4f, wallH * 0.5f, 0.08f), frozenBanner, 0, wallH * 0.62f, 0.1f); // frozen-stiff banners, frosted dark blue
                    Mk.Msh(g, Geo.Box(0.9f, 0.9f, 0.06f), bannerTrim, 0, wallH * 0.66f, 0.15f, 0, 0, Mathf.PI / 4);
                    for (int k = 0; k < 3; k++) Mk.Msh(g, Geo.Cone(0.18f, 0.6f + k % 2 * 0.4f, 5), ice, (k - 1) * 1.1f, wallH * 0.37f - 0.3f, 0.12f, Mathf.PI, 0, 0); // icicles off the hem
                    Mk.Msh(g, Geo.Box(3.7f, 0.3f, 0.14f), M.metal, 0, wallH * 0.87f, 0.12f);
                }, 14);
                return true;
            }
            if (d == "ruin")
            {
                var pipeM = Mats.Std(0x5a3a28, 0.55f, 0.6f); var stripeM = Mats.Std(0xd8a020, 0.7f); var neonC = new[] { 0xff3080, 0x30e0ff, 0xff9020 };
                foreach (var s in new[] { -1, 1 })
                {
                    Deco(Geo.Cyl(0.35f, 0.35f, Bd * 2, 10), pipeM, s * (Bd - 0.4f), 3.2f, 0, Mathf.PI / 2, 0, 0); Deco(Geo.Cyl(0.25f, 0.25f, Bd * 2, 10), pipeM, s * (Bd - 0.35f), 9.5f, 0, Mathf.PI / 2, 0, 0);
                    Deco(Geo.Cyl(0.35f, 0.35f, Bd * 2, 10), pipeM, 0, 3.2f, s * (Bd - 0.4f), 0, 0, Mathf.PI / 2); Deco(Geo.Cyl(0.25f, 0.25f, Bd * 2, 10), pipeM, 0, 9.5f, s * (Bd - 0.35f), 0, 0, Mathf.PI / 2);
                }
                int i = 0;
                var cloths = new[] { Mats.Std(0x4a1410, 0.95f, 0, 0, null, true), Mats.Std(0x1c1a18, 0.95f, 0, 0, null, true), Mats.Std(0x3a3020, 0.95f, 0, 0, null, true) };
                var paint = Mats.Std(0xc8b890, 0.9f, 0, 0, null, true); var scorch = Mats.Std(0x0a0806, 0.95f, 0, 0, null, true); var ply = Mats.Std(0x6a5030, 0.95f); var poster = Mats.Std(0x8a7a60, 0.95f);
                OnWalls(Bd, 22, (px, pz, nx, nz) =>
                {
                    var g = WallG(px, pz, nx, nz); int k = i++;
                    if (k % 2 == 0)
                    {
                        // a torn cloth banner hung from a rusted bracket: a faded hazard emblem, scorched along one edge
                        var cl = cloths[k / 2 % 3]; float top = 15, hgt = 8;
                        Mk.Msh(g, Geo.Box(0.18f, 0.18f, 1.6f), pipeM, 0, top + 0.3f, 0.8f); Mk.Msh(g, Geo.Box(4.2f, 0.16f, 0.16f), pipeM, 0, top + 0.3f, 1.55f); Mk.Msh(g, Geo.Box(0.12f, 0.9f, 0.12f), pipeM, 0, top - 0.1f, 0.4f, 0.8f, 0, 0);
                        Mk.Msh(g, Geo.Box(3.6f, hgt, 0.05f), cl, 0, top - hgt / 2, 1.6f, 0.04f, 0, (k % 3 - 1) * 0.02f);
                        Mk.Msh(g, Prism(1.2f, 1.1f, 0.05f), cl, -1.2f, top - hgt + 0.02f, 1.6f, 0, 0, Mathf.PI); Mk.Msh(g, Prism(1.2f, 0.7f, 0.05f), cl, 1.2f, top - hgt + 0.02f, 1.6f, 0, 0, Mathf.PI); // the torn hem hanging in tongues
                        Mk.Msh(g, Geo.Torus(0.9f, 0.12f, 24, 4), paint, 0, top - 3.4f, 1.64f);
                        for (int j = 0; j < 3; j++) { float a = j * Mathf.PI * 2 / 3 + Mathf.PI / 2; Mk.Msh(g, Geo.Box(0.32f, 0.9f, 0.02f), paint, Mathf.Cos(a) * 0.55f, top - 3.4f + Mathf.Sin(a) * 0.55f, 1.65f, 0, 0, a - Mathf.PI / 2); }
                        Mk.Msh(g, Geo.Box(0.3f, 0.3f, 0.02f), paint, 0, top - 3.4f, 1.66f);
                        Mk.Msh(g, Geo.Box(1.0f, hgt * 0.6f, 0.06f), scorch, 1.35f, top - hgt * 0.55f, 1.61f);
                        Mk.Msh(g, Geo.Box(2.6f, 0.25f, 0.02f), paint, 0, top - 6.4f, 1.65f); Mk.Msh(g, Geo.Box(1.8f, 0.2f, 0.02f), paint, -0.2f, top - 6.9f, 1.65f);
                    }
                    else
                    {
                        // a boarded-up hoarding: plywood nailed over a peeling poster
                        Mk.Msh(g, Geo.Box(6.4f, 4.4f, 0.12f), poster, 0, 5.2f, 0.12f);
                        for (int j = 0; j < 5; j++) Mk.Msh(g, Geo.Box(6.6f, 0.7f, 0.08f), ply, 0, 3.6f + j * 0.85f, 0.22f, 0, 0, ((j * 5 + k) % 7 - 3) * 0.02f);
                        Mk.Msh(g, Geo.Box(0.2f, 4.6f, 0.1f), pipeM, -3.2f, 5.2f, 0.25f); Mk.Msh(g, Geo.Box(0.2f, 4.6f, 0.1f), pipeM, 3.2f, 5.2f, 0.25f);
                        Mk.Msh(g, Geo.Box(2.2f, 0.4f, 0.02f), paint, (k % 3 - 1) * 1.4f, 5.6f, 0.27f, 0, 0, 0.1f);
                    }
                    Mk.Msh(g, Geo.Box(8, 0.5f, 0.12f), stripeM, 0, 0.25f, 0.08f);
                    for (int j = 0; j < 4; j++) Mk.Msh(g, Geo.Box(0.55f, 0.52f, 0.13f), M.tower, -3 + j * 2, 0.25f, 0.1f, 0, 0, 0.7f); // hazard stripes
                });
                return true;
            }
            if (d == "limbo")
            {
                var gold = Mats.Std(0xd8b860, 0.3f, 0.85f);
                OnWalls(Bd, 12, (px, pz, nx, nz) =>
                {
                    var g = WallG(px, pz, nx, nz);
                    Mk.Msh(g, Geo.Box(0.7f, wallH - 2, 0.7f), M.tower, -2.4f, (wallH - 2) / 2, 0.35f); Mk.Msh(g, Geo.Box(0.7f, wallH - 2, 0.7f), M.tower, 2.4f, (wallH - 2) / 2, 0.35f);
                    Mk.Msh(g, Geo.Arc(2.4f, 0.35f, 16), M.tower, 0, wallH - 2, 0.35f);
                    Mk.Msh(g, Geo.Box(3.4f, 0.12f, 0.12f), gold, 0, wallH - 0.4f, 0.2f);
                    Mk.Msh(g, Geo.Box(0.06f, 3, 0.06f), M.metal, 0, wallH - 4.5f, 0.9f); Mk.Msh(g, Geo.Box(0.4f, 0.55f, 0.4f), gold, 0, wallH - 6.2f, 0.9f);
                    var f = Mk.Glow(g, 0xffd890, 2.2f, 0.75f); f.transform.localPosition = V(0, wallH - 6.2f, 0.9f); flames.Add(f.transform);
                });
                return true;
            }
            return false;
        }
        bool ActSky(string kind, float Bd)
        {
            if (FrostSky(kind, Bd)) return true;
            if (kind == "aurora")
            {
                for (int i = 0; i < 70; i++) { var dir = Quaternion.Euler(-10 - rand * 70, rand * 360, 0) * Vector3.forward; var s = Mk.Glow(levelRoot, 0xe8f4ff, 1.2f + rand * 1.6f, 0.9f, false); s.transform.localPosition = dir * 320; }
                var moon = Mk.Glow(levelRoot, 0xc8e4ff, 60, 0.85f, false); moon.transform.localPosition = V(-80, 120, -230);
                var mc = Mk.Glow(levelRoot, 0xffffff, 22, 1, false); mc.transform.localPosition = V(-80, 120, -230);
                var curtains = new List<(Transform t, Material m, float ph)>();
                for (int i = 0; i < 5; i++)
                {
                    float a = -0.9f + i * 0.45f; var m = Mats.Add(i % 2 == 0 ? 0x40ffb0 : 0x60b0ff, 0.22f, Tex.RayFade, false, true);
                    var c = Mk.Msh(levelRoot, Geo.Plane(220, 70), m, Mathf.Sin(a) * 210, 120 + i * 6, -Mathf.Cos(a) * 210, 0, -a, 0); c.layer = Mk.LayerIgnore;
                    curtains.Add((c.transform, m, i * 1.3f));
                }
                anims.Add((t, dt) => { foreach (var c in curtains) { Mats.SetAlpha(c.m, 0.14f + 0.1f * Mathf.Sin(t * 0.4f + c.ph)); c.t.localScale = V(1 + 0.05f * Mathf.Sin(t * 0.3f + c.ph), 1, 1); } });
                return true;
            }
            if (kind == "smog")
            {
                var sun = Mk.Glow(levelRoot, 0xff5020, 150, 0.75f, false); sun.transform.localPosition = V(70, 30, -260);
                var core = Mk.Glow(levelRoot, 0xffa060, 50, 0.9f, false); core.transform.localPosition = V(70, 30, -260);
                for (int i = 0; i < 8; i++) { float a = i / 8f * Mathf.PI * 2 + 0.3f; var g = Mk.Glow(levelRoot, 0xff4010, 110, 0.28f, false); g.transform.localPosition = V(Mathf.Cos(a) * 220, 10, Mathf.Sin(a) * 220); }
                return true;
            }
            if (kind == "fog")
            {
                var s = Mk.Glow(levelRoot, 0xfff6e8, 180, 0.6f, false); s.transform.localPosition = V(0, 160, -120);
                for (int i = 0; i < 4; i++) { var sh = Mk.Msh(levelRoot, Geo.Cyl(4, 4, 200, 12), Mats.Add(0xfff4e0, 0.05f, null, true), -50 + i * 32, 70, -30 + (i % 2) * 40, 0.2f, 0, 0.3f); sh.layer = Mk.LayerIgnore; }
                return true;
            }
            return false;
        }
        bool ActBackdrop(string kind, float Bd)
        {
            if (FrostBackdrop(kind, Bd)) return true;
            if (kind == "aurora")
            {
                var rock = Silhouette(0x30455a); var snow = Silhouette(0x8aa6be); var rng = new System.Random(3); float R() => (float)rng.NextDouble(); // the mountains must read against the night sky, or their snow caps seem to float
                for (int i = 0; i < 18; i++)
                {
                    float a = i / 18f * Mathf.PI * 2 + R() * 0.2f, r = 170 + R() * 60, h = 60 + R() * 90, w = 30 + R() * 30;
                    var p = V(Mathf.Cos(a) * r, -5, Mathf.Sin(a) * r);
                    Deco(Geo.Cone(w, h, 6), rock, p.x, h / 2 - 5, p.z, 0, R() * 3, 0);
                    Deco(Geo.Cone(w * 0.32f, h * 0.32f, 6), snow, p.x, h - 5 - h * 0.16f + 0.5f, p.z, 0, R() * 3, 0);
                }
                // a titan frozen into a glacier on the horizon
                var ice = Mats.Trans(0x9ad0f0, 0.25f, false, false); var body = Silhouette(0x0c141c);
                var tp = V(40, 0, -240);
                Deco(Geo.Box(36, 70, 26), body, tp.x, 40, tp.z); Deco(Geo.Sphere(13, 12, 10), body, tp.x, 86, tp.z);
                Deco(Geo.Box(12, 50, 12), body, tp.x - 26, 70, tp.z, 0, 0, 0.5f); Deco(Geo.Box(12, 50, 12), body, tp.x + 26, 70, tp.z, 0, 0, -0.5f);
                Deco(Geo.Box(80, 120, 50), ice, tp.x, 50, tp.z);
                return true;
            }
            if (kind == "smog")
            {
                var bm = Mats.Basic(0x8a7a6a, FacadeTex()); var dark = Silhouette(0x1a1210); var rng = new System.Random(11); float R() => (float)rng.NextDouble();
                for (int i = 0; i < 46; i++)
                {
                    float a = i / 46f * Mathf.PI * 2 + R() * 0.1f, r = 150 + R() * 110, h = 30 + R() * 110, w = 14 + R() * 18;
                    var b = Deco(Geo.TiledBox(w, h, w), R() < 0.5f ? bm : dark, Mathf.Cos(a) * r, h / 2 - 2, Mathf.Sin(a) * r, 0, R() * 2, R() < 0.15f ? 0.12f : 0);
                    levelMeshes.Add(b.GetComponent<MeshFilter>().sharedMesh);
                }
                for (int i = 0; i < 4; i++) { float a = 0.6f + i * 1.5f, r = 190; var p = V(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r); Deco(Geo.Box(3, 90, 3), dark, p.x, 45, p.z); Deco(Geo.Box(60, 3, 3), dark, p.x + 20, 88, p.z, 0, a, 0); } // cranes
                return true;
            }
            if (kind == "fog")
            {
                var rock = Silhouette(0x5a5a60, 0.85f); var rng = new System.Random(17); float R() => (float)rng.NextDouble();
                var isles = new List<(Transform t, float y, float ph)>();
                for (int i = 0; i < 14; i++)
                {
                    float a = i / 14f * Mathf.PI * 2, r = 140 + R() * 100, y = 15 + R() * 70, w = 12 + R() * 22;
                    var root = Mk.Piv(levelRoot, Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                    Mk.Msh(root, Geo.Cone(w, w * 1.6f, 7), rock, 0, -w * 0.8f, 0, Mathf.PI, 0, 0); Mk.Msh(root, Geo.Cyl(w, w, 2, 7), rock, 0, 0.5f, 0);
                    isles.Add((root.t, y, R() * 6));
                }
                anims.Add((t, dt) => { foreach (var s in isles) { var p = s.t.localPosition; p.y = s.y + Mathf.Sin(t * 0.25f + s.ph) * 3; s.t.localPosition = p; } });
                var gate = Silhouette(0x4a4a50, 0.9f);
                Deco(Geo.Box(14, 160, 14), gate, -40, 80, -250); Deco(Geo.Box(14, 160, 14), gate, 40, 80, -250); Deco(Geo.Arc(40, 7, 24), gate, 0, 160, -250);
                var ring = new N(Deco(Geo.Torus(70, 1.5f, 96), Silhouette(0xd8c890, 0.5f), 0, 120, -200).transform);
                anims.Add((t, dt) => ring.rz -= dt * 0.02f);
                return true;
            }
            return false;
        }

        // ---------- props ----------
        Material iceM, iceTransM, snowMat;
        Material SnowMat => snowMat ? snowMat : (snowMat = Mats.Basic(0xeef6ff));
        void ActMats() { iceM = Mats.Std(0xbfe6ff, 0.12f, 0.1f, 0x0a2a40); iceTransM = Mats.Trans(0xa8dcff, 0.55f); }
        void IceSpire(float x, float z, float h, float r = 1.2f) { Deco(Geo.Cone(r, h, 6), iceM, x, h / 2, z, (rand - 0.5f) * 0.2f, rand * 3, (rand - 0.5f) * 0.2f); SolidAABB(V(x - r * 0.5f, 0, z - r * 0.5f), V(x + r * 0.5f, h * 0.6f, z + r * 0.5f)); }
        void FrozenSoul(float x, float z, float ry)
        {
            var p = Mk.Piv(levelRoot, x, -0.4f, z); p.SetRot(0, ry, 0); var skin = Mats.Std(0x5a6a7a, 0.6f);
            Mk.Msh(p, Geo.Box(0.5f, 0.9f, 0.3f), skin, 0, 0.45f, 0); Mk.Msh(p, Geo.Sphere(0.22f, 10, 8), skin, 0, 1.1f, 0);
            Mk.Msh(p, Geo.Box(0.14f, 0.8f, 0.14f), skin, -0.35f, 1.1f, 0, 0, 0, 0.4f); Mk.Msh(p, Geo.Box(0.14f, 0.8f, 0.14f), skin, 0.35f, 1.2f, 0.1f, 0.2f, 0, -0.3f);
            Mk.Msh(p, Geo.Box(1.3f, 1.4f, 1.0f), iceTransM, 0, 0.6f, 0);
        }
        void Rock(float x, float z, float w, float h, float d, Material m = null)
        {
            Box(x, 0, z, w, h, d, m ?? M.tower);
            for (int k = 0; k < 3; k++) Deco(Geo.Box(w * (0.3f + rand * 0.3f), h * 0.3f, d * (0.3f + rand * 0.3f)), m ?? M.tower, x + (rand - 0.5f) * w * 0.5f, h + 0.1f, z + (rand - 0.5f) * d * 0.5f, (rand - 0.5f) * 0.4f, rand * 3, (rand - 0.5f) * 0.4f);
        }
        GameObject Car(float x, float z, float ry, bool burnt)
        {
            int[] cols = { 0x7a2a20, 0x2a4a6a, 0x5a5a50, 0x8a7a3a };
            var body = burnt ? Mats.Std(0x1c1814, 0.9f, 0.3f) : Mats.Std(cols[Mathf.Abs((int)(x * 3 + z)) % 4], 0.55f, 0.5f);
            bool alongX = Mathf.Abs(Mathf.Sin(ry)) < 0.5f;
            var g = Box(x, 0, z, alongX ? 4.4f : 2.0f, 1.2f, alongX ? 2.0f : 4.4f, body);
            var cab = Deco(Geo.Box(2.4f, 0.8f, 1.8f), body, x, 1.6f, z, 0, ry + Mathf.PI / 2, 0);
            Deco(Geo.Box(2.2f, 0.6f, 1.82f), Mats.Std(0x10181c, 0.1f, 0.4f), x, 1.62f, z, 0, ry + Mathf.PI / 2, 0); // glass
            var tire = Mats.Std(0x111111, 0.9f);
            for (int a = -1; a <= 1; a += 2) for (int b = -1; b <= 1; b += 2)
            {
                var o = Quaternion.Euler(0, (ry + Mathf.PI / 2) * Mathf.Rad2Deg, 0) * V(a * 1.4f, 0.4f, b * 0.95f);
                Deco(Geo.Cyl(0.42f, 0.42f, 0.3f, 10), tire, x + o.x, 0.42f, z + o.z, Mathf.PI / 2, ry + Mathf.PI / 2, 0);
            }
            return g;
        }
        void Building(float x, float z, float w, float d, float h, bool roofProps = true)
        {
            var fm = Mats.Basic(0xb0a090, FacadeTex());
            Box(x, 0, z, w, h, d, fm);
            Box(x, h, z, w + 0.4f, 0.8f, 0.4f, M.tower); Box(x, h, z, 0.4f, 0.8f, d + 0.4f, M.tower);
            Deco(Geo.Box(w + 0.4f, 0.8f, 0.4f), M.tower, x, h + 0.4f, z - d / 2); Deco(Geo.Box(w + 0.4f, 0.8f, 0.4f), M.tower, x, h + 0.4f, z + d / 2);
            Deco(Geo.Box(0.4f, 0.8f, d + 0.4f), M.tower, x - w / 2, h + 0.4f, z); Deco(Geo.Box(0.4f, 0.8f, d + 0.4f), M.tower, x + w / 2, h + 0.4f, z);
            SolidAABB(V(x - w / 2 - 0.2f, h, z - d / 2 - 0.2f), V(x + w / 2 + 0.2f, h + 0.8f, z - d / 2 + 0.2f)); SolidAABB(V(x - w / 2 - 0.2f, h, z + d / 2 - 0.2f), V(x + w / 2 + 0.2f, h + 0.8f, z + d / 2 + 0.2f));
            if (!roofProps) return;
            Box(x + w * 0.2f, h, z - d * 0.2f, 2.2f, 1.4f, 1.6f, M.metal); // AC unit
            var tank = Mats.Std(0x6a4a30, 0.8f);
            Deco(Geo.Cyl(1.3f, 1.3f, 2.4f, 12), tank, x - w * 0.25f, h + 3.6f, z + d * 0.2f); Deco(Geo.Cone(1.5f, 0.9f, 12), tank, x - w * 0.25f, h + 5.25f, z + d * 0.2f);
            foreach (var a in new[] { -0.9f, 0.9f }) foreach (var b in new[] { -0.9f, 0.9f }) Deco(Geo.Box(0.15f, 2.4f, 0.15f), M.metal, x - w * 0.25f + a, h + 1.2f, z + d * 0.2f + b);
            SolidAABB(V(x - w * 0.25f - 1.3f, h, z + d * 0.2f - 1.3f), V(x - w * 0.25f + 1.3f, h + 5.7f, z + d * 0.2f + 1.3f));
            Deco(Geo.Box(0.08f, 4, 0.08f), M.metal, x + w * 0.35f, h + 2, z + d * 0.3f); // antenna
        }
        void StreetLamp(float x, float z, float ry)
        {
            Deco(Geo.Cyl(0.12f, 0.16f, 6, 8), M.metal, x, 3, z); Deco(Geo.Box(1.6f, 0.12f, 0.12f), M.metal, x + Mathf.Cos(ry) * 0.7f, 6, z + Mathf.Sin(ry) * 0.7f, 0, -ry, 0);
            var g = Mk.Glow(levelRoot, 0xffb060, 3.2f, 0.6f); g.transform.localPosition = V(x + Mathf.Cos(ry) * 1.4f, 5.8f, z + Mathf.Sin(ry) * 1.4f);
            SolidAABB(V(x - 0.2f, 0, z - 0.2f), V(x + 0.2f, 6, z + 0.2f));
        }
        void Lantern(float x, float z, float h)
        {
            var gold = Mats.Std(0xd8b860, 0.3f, 0.85f);
            Deco(Geo.Cyl(0.08f, 0.1f, h, 6), M.metal, x, h / 2, z); Deco(Geo.Box(0.4f, 0.5f, 0.4f), gold, x, h + 0.2f, z); Deco(Geo.Cone(0.32f, 0.25f, 4), gold, x, h + 0.55f, z, 0, Mathf.PI / 4, 0);
            var g = Mk.Glow(levelRoot, 0xffd890, 2.6f, 0.75f); g.transform.localPosition = V(x, h + 0.2f, z); flames.Add(g.transform);
        }
        void Shelf(float x, float z, float len, float h, bool alongX)
        {
            var bm = Mats.Basic(0xd8c8b0, BooksTex());
            Box(x, 0, z, alongX ? len : 1.6f, h, alongX ? 1.6f : len, bm);
            Deco(Geo.Box(alongX ? len + 0.3f : 1.9f, 0.3f, alongX ? 1.9f : len + 0.3f), M.tower, x, h + 0.15f, z);
        }
        void GearDeco(Transform parent, float r, int teeth, Material m, out Transform hub)
        {
            hub = Mk.Piv(parent).t;
            Mk.Msh(hub, Geo.Cyl(r, r, 0.6f, 24), m, 0, 0, 0, Mathf.PI / 2); Mk.Msh(hub, Geo.Cyl(r * 0.3f, r * 0.3f, 0.8f, 12), M.tower, 0, 0, 0, Mathf.PI / 2);
            for (int k = 0; k < teeth; k++) { float a = k * Mathf.PI * 2 / teeth; Mk.Msh(hub, Geo.Box(r * 0.22f, r * 0.24f, 0.6f), m, Mathf.Cos(a) * (r + r * 0.1f), Mathf.Sin(a) * (r + r * 0.1f), 0, 0, 0, a); }
        }

        // ---------- the fifteen act levels ----------
        List<LevelDef> ActLevels() => new List<LevelDef>
        {
            // ===== ACT III: DEAD EARTH =====
            new LevelDef { name = "ASHFALL CITY", theme = "ruin", hazard = "COLLAPSING FACADES", hazardInfo = "grit trickles off a wall and the sidewalk below is marked - then a slab of the facade comes down", B = 66, center = V(0, 2.2f, 0), spawn = V(0, 0, 54), build = BuildAshfall },
            new LevelDef { name = "THE DROWNED SUBWAY", theme = "ruin", hazard = "TRAINS", hazardInfo = "alarms, beacons and headlights - then a three-car train tears through on one of the three tracks - get off the rails", B = 60, center = V(0, 3.5f, 0), spawn = V(0, 1.4f, 46), build = BuildSubway },
            new LevelDef { name = "OVERPASS GRAVEYARD", theme = "ruin", hazard = "WRECKING CRANES & FALLING DEBRIS", hazardInfo = "the wrecking balls swing across the decks - watch the marked path; below, dust trickles from the deck, then a slab comes down", B = 66, center = V(0, 3, 0), spawn = V(0, 0, 54), build = BuildOverpass },
            new LevelDef { name = "THE CATHEDRAL OF RUST", theme = "ruin", hazard = "THE MACHINES", hazardInfo = "presses stamp the belt, steam vents blast, shredders spin up, welding arms sweep - every one flashes and hisses first", B = 62, center = V(0, 0.2f, 47), spawn = V(0, 0, 57), build = BuildCathedral },
            new LevelDef { name = "THE BROKEN CROWN", theme = "ruin", hazard = null, B = 72, boss = 4, center = V(0, 0.2f, 0), spawn = V(0, 0, 42), bossSpawn = new Vector2(0, -20), build = BuildBrokenCrown },
            // ===== ACT IV: PURGATORY =====
            new LevelDef { name = "THE WAITING SHORE", theme = "limbo", hazard = "THE FOG TIDE", hazardInfo = "the shore bell tolls and the fog rolls in over the low ground - it drags at you and bites harder the longer you stay in it; get up on the piers, the ferry, the rocks", B = 68, center = V(0, 4, 0), spawn = V(0, 1.6f, 54), build = BuildWaitingShore },
            new LevelDef { name = "CLOCKWORK HALLS", theme = "limbo", hazard = "THE MECHANISM", hazardInfo = "three chimes and the floor clock's hands sweep the dial - jump them; the trip-hammers ratchet up and slam down on their glowing marks", B = 60, center = V(0, 3, 0), spawn = V(0, 0, 48), build = BuildClockworkHalls },
            new LevelDef { name = "THE LIBRARY OF SOULS", theme = "limbo", hazard = "HAUNTED STACKS", hazardInfo = "grimoires trace you with a thread of gold, then hurl a volley of books - sidestep; page storms wander the aisles, keep out of them", B = 62, center = V(0, 3, 0), spawn = V(0, 0, 50), build = BuildLibrary },
            new LevelDef { name = "MIRROR WASTES", theme = "limbo", hazard = "MIRROR GLINTS", hazardInfo = "the great mirrors turn to follow you, trace you with light, then fire it - break their line of sight", B = 70, center = V(0, 3, 0), spawn = V(0, 0, 56), build = BuildMirrorWastes },
            new LevelDef { name = "THE HALL OF HOURS", theme = "limbo", hazard = null, B = 66, boss = 5, center = V(0, 0.2f, 0), spawn = V(0, 0, 50), bossSpawn = new Vector2(0, -30), build = BuildHallOfHours },
        };

        // ================= hazards =================
        // ---------- 6. thin ice ----------
        void MechThinIce(Vector3[] zones, float r)
        {
            var crackGeo = Geo.Box(r * 0.9f, 0.03f, 0.1f);
            for (int i = 0; i < zones.Length; i++)
            {
                var c = zones[i]; float off = i * 1.9f;
                var crackM = HazMat(0xeaf8ff, 0, true); var piv = Mk.Piv(levelRoot, c.x, 0.03f, c.z);
                for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4 + rand * 0.3f; Mk.Msh(piv, crackGeo, crackM, Mathf.Cos(a) * r * 0.42f, 0, Mathf.Sin(a) * r * 0.42f, 0, -a, 0).layer = Mk.LayerIgnore; }
                Flat(Geo.Ring(r - 0.25f, r, 40), crackM, c.x, 0.035f, c.z);
                var water = Flat(Geo.Circle(r, 36), Mats.Basic(0x9ad8ff, Tex.Holy), c.x, 0.05f, c.z); var waterM = water.GetComponent<Renderer>().sharedMaterial; waterM.SetTextureScale("_BaseMap", V(2, 2, 0));
                var chunks = new List<Transform>(); for (int k = 0; k < 6; k++) { float a = rand * 6.3f, rr = rand * r * 0.8f; var ch = Deco(Geo.Box(1 + rand, 0.25f, 0.8f + rand), iceM, c.x + Mathf.Cos(a) * rr, 0.12f, c.z + Mathf.Sin(a) * rr, 0, rand * 3, 0); ch.layer = Mk.LayerIgnore; chunks.Add(ch.transform); }
                water.SetActive(false); foreach (var ch in chunks) ch.gameObject.SetActive(false);
                float hitT = 0;
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 9f; bool warn = u >= 5 && u < 6.6f, broken = u >= 6.6f;
                    Mats.SetAlpha(crackM, warn ? 0.3f + 0.5f * (u - 5) / 1.6f * (Mathf.Sin(u * 30) > 0 ? 1 : 0.6f) : broken ? 0.15f : 0.06f);
                    if (warn && rand < 0.3f) SpawnParticles(c + V((rand - 0.5f) * r, 0.1f, (rand - 0.5f) * r), 1, 2, cyanMat, 0.06f, 0.4f);
                    if (water.activeSelf != broken) { water.SetActive(broken); foreach (var ch in chunks) ch.gameObject.SetActive(broken); if (broken) { SpawnParticles(c + V(0, 0.3f, 0), 24, 6, cyanMat, 0.12f, 0.7f); Sfx("splash"); } }
                    if (!broken) return;
                    waterM.SetTextureOffset("_BaseMap", V(t * 0.03f, t * 0.02f, 0));
                    for (int k = 0; k < chunks.Count; k++) { var p = chunks[k].localPosition; p.y = 0.12f + Mathf.Sin(t * 2 + k) * 0.05f; chunks[k].localPosition = p; }
                    if (!HazardLive) return;
                    hitT -= dt;
                    if (PlayerIn(c, r * 0.9f, -1, 0.7f)) { hazSlowT = 0.2f; chillT = Mathf.Max(chillT, 1.5f); if (hitT <= 0) { hitT = 0.6f; HazardHitPlayer(9, c, V(0, 5, 0)); } }
                    HazardHitEnemies(e => Hypot(e.pos.x - c.x, e.pos.z - c.z) < r * 0.9f && e.pos.y < 0.7f, 3, e => V(0, 6, 0), 0.6f);
                });
            }
        }
        // ---------- 7. falling icicles ----------
        void MechIcicles()
        {
            var drops = new List<(Vector3 p, GameObject shadow, Material sm, Transform ice)>(); var dropT = new List<float>();
            float sc = 2.5f;
            trapAnims.Add((t, dt) =>
            {
                if (HazardLive && phase == "fight")
                {
                    sc -= dt;
                    if (sc <= 0)
                    {
                        sc = 0.75f + rand * 0.6f; float a = rand * Mathf.PI * 2, rr = rand * 9, lim = LB - 3;
                        var p = V(clamp(P.pos.x + Mathf.Cos(a) * rr + P.vel.x * 0.6f, -lim, lim), 0, clamp(P.pos.z + Mathf.Sin(a) * rr + P.vel.z * 0.6f, -lim, lim));
                        var sm = HazMat(0x60c8ff, 0, false); var sh = Flat(Geo.Circle(1.7f, 24), sm, p.x, 0.07f, p.z);
                        var ice = Deco(Geo.Cone(0.5f, 3.4f, 6), iceM, p.x, 40, p.z, Mathf.PI, 0, 0); ice.layer = Mk.LayerIgnore;
                        drops.Add((p, sh, sm, ice.transform)); dropT.Add(0);
                    }
                }
                for (int i = drops.Count - 1; i >= 0; i--)
                {
                    var d = drops[i]; float u = dropT[i] += dt;
                    if (u < 1.3f)
                    {
                        Mats.SetAlpha(d.sm, 0.15f + 0.5f * u / 1.3f); d.shadow.transform.localScale = Vector3.one * (0.5f + 0.5f * u / 1.3f);
                        float fy = u < 0.95f ? 40 : Mathf.Lerp(40, 1.7f, (u - 0.95f) / 0.35f); d.ice.localPosition = V(d.p.x, fy, d.p.z);
                    }
                    else if (u - dt < 1.3f)
                    {
                        d.ice.localPosition = V(d.p.x, 1.2f, d.p.z); d.ice.localRotation = Quaternion.Euler(180 + (rand - 0.5f) * 30, 0, (rand - 0.5f) * 30);
                        SpawnParticles(d.p + V(0, 0.5f, 0), 18, 7, cyanMat, 0.1f, 0.6f); ShockRing(d.p, 2.4f, 0xa0e8ff, 0.3f); Sfx("boomSmall");
                        if (HazardLive && PlayerIn(d.p, 2.1f, -1, 2.6f)) { HazardHitPlayer(18, d.p, V(0, 4, 0)); chillT = Mathf.Max(chillT, 1.6f); }
                        HazardHitEnemies(e => Hypot(e.pos.x - d.p.x, e.pos.z - d.p.z) < 2.2f && e.pos.y < 2.5f, 5, e => V(0, 5, 0));
                        Mats.SetAlpha(d.sm, 0);
                    }
                    else if (u > 3.2f)
                    {
                        float k = 1 - (u - 3.2f) / 0.5f; d.ice.localScale = Vector3.one * Mathf.Max(0.01f, k);
                        if (k <= 0) { Destroy(d.shadow); Destroy(d.ice.gameObject); drops.RemoveAt(i); dropT.RemoveAt(i); }
                    }
                }
            });
        }
        // ---------- 8. blizzard gusts ----------
        void MechBlizzard()
        {
            Vector3 dir = V(1, 0, 0); float baseFar = THEME.fogFar; bool gusting = false;
            var dirs = new[] { V(1, 0, 0), V(-1, 0, 0), V(0, 0, 1), V(0, 0, -1) };
            trapAnims.Add((t, dt) =>
            {
                float u = (t + 3) % 11f; bool warn = u >= 5.5f && u < 7.1f, gust = u >= 7.1f && u < 10.6f;
                if (u < dt * 2) dir = dirs[(int)(rand * 4) % 4];
                if (!started) return;
                float k = gust ? Smoother((u - 7.1f) / 0.6f) * Smoother((10.6f - u) / 0.6f) : 0;
                RenderSettings.fogEndDistance = Mathf.Lerp(baseFar, 48, k);
                if (warn || gust)
                {
                    int n = gust ? 6 : 2;
                    for (int i = 0; i < n; i++)
                    {
                        var side = Vector3.Cross(Vector3.up, dir);
                        var p = P.pos - dir * 20 + side * (rand - 0.5f) * 40 + V(0, rand * 8, 0);
                        particles.Add(new Pt { pos = p, scale = Vector3.one * (0.06f + rand * 0.08f), mat = SnowMat, v = dir * (gust ? 30 : 12) + V(0, -1, 0), life = 1.6f, grav = 0 });
                    }
                    TrimParticles();
                }
                if (gust != gusting) { gusting = gust; if (gust) P.shake = Mathf.Max(P.shake, 0.25f); }
                if (!gust || !HazardLive) return;
                bool sheltered = Physics.Raycast(ChestPos, -dir, 9, LEVEL_MASK, QueryTriggerInteraction.Ignore);
                if (!sheltered) { P.vel += dir * 26 * k * dt; P.knockT = Mathf.Max(P.knockT, 0.1f); }
                foreach (var e in enemies) if (!e.dead && e.type != "boss" && e.spawnT <= 0) e.vel += dir * 9 * k * dt;
            });
        }
        // ---------- 9. shard bursts ----------
        void MechShardBursts(Vector3[] spots)
        {
            for (int i = 0; i < spots.Length; i++)
            {
                var c = spots[i]; float off = i * 1.15f;
                var cl = Mk.Piv(levelRoot, c.x, 0, c.z);
                for (int k = 0; k < 5; k++) { float a = k * 1.256f; Mk.Msh(cl, Geo.Octahedron(1), iceM, Mathf.Cos(a) * 0.7f, 1.4f + (k % 2) * 0.6f, Mathf.Sin(a) * 0.7f).transform.localScale = V(0.6f, 1.8f + (k % 3) * 0.5f, 0.6f); }
                Mk.Msh(cl, Geo.Octahedron(1), iceM, 0, 2.4f, 0).transform.localScale = V(0.9f, 2.6f, 0.9f);
                SolidAABB(V(c.x - 1.2f, 0, c.z - 1.2f), V(c.x + 1.2f, 4.5f, c.z + 1.2f));
                var gl = Mk.Glow(levelRoot, 0x80e0ff, 6, 0.2f); gl.transform.localPosition = V(c.x, 2.5f, c.z); var glM = gl.GetComponent<Renderer>().sharedMaterial;
                var ringM = HazMat(0xb0ecff, 0, true); var ring = Deco(wallRingGeo, ringM, c.x, 0.6f, c.z); ring.layer = Mk.LayerIgnore; ring.SetActive(false);
                bool hitP = false; var hitE = new HashSet<Ent>();
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 7.5f; bool charge = u >= 4 && u < 5.6f, burst = u >= 5.6f && u < 7.1f;
                    float pulse = charge ? (u - 4) / 1.6f : 0;
                    cl.t.localScale = Vector3.one * (1 + pulse * 0.25f + (charge ? Mathf.Sin(t * 40) * 0.04f * pulse : 0));
                    Mats.SetAlpha(glM, 0.2f + pulse * 0.7f);
                    if (charge && rand < 0.4f) SpawnParticles(c + V(0, 2, 0), 1, 3, cyanMat, 0.07f, 0.4f, 2);
                    ring.SetActive(burst);
                    if (!burst) { hitP = false; hitE.Clear(); return; }
                    float k = (u - 5.6f) / 1.5f, R = 1.5f + k * 22;
                    if (u - dt < 5.6f) { SpawnParticles(c + V(0, 2, 0), 30, 12, cyanMat, 0.12f, 0.7f); Sfx("boomSmall"); }
                    ring.transform.localScale = V(R, 1.2f, R); Mats.SetAlpha(ringM, 0.8f * (1 - k));
                    if (!HazardLive) return;
                    float dP = Hypot(P.pos.x - c.x, P.pos.z - c.z);
                    if (!hitP && Mathf.Abs(dP - R) < 1.0f && P.pos.y < 1.3f) { hitP = true; var o = (P.pos - c); o.y = 0; HazardHitPlayer(16, c, o.normalized * 9 + V(0, 6, 0)); chillT = Mathf.Max(chillT, 1.5f); }
                    HazardHitEnemies(e => { float de = Hypot(e.pos.x - c.x, e.pos.z - c.z); return Mathf.Abs(de - R) < 1.2f && e.pos.y < 1.3f && hitE.Add(e); }, 5, e => { var o = e.pos - c; o.y = 0; return o.normalized * 8 + V(0, 6, 0); });
                });
            }
        }
        // ---------- 11. burning wrecks ----------
        void MechCarBombs(Vector3[] cars)
        {
            for (int i = 0; i < cars.Length; i++)
            {
                var c = cars[i]; float off = i * 1.37f;
                var gl = Mk.Glow(levelRoot, 0xff6010, 4, 0.3f); gl.transform.localPosition = V(c.x, 2.2f, c.z); var glM = gl.GetComponent<Renderer>().sharedMaterial;
                var warnM = HazMat(0xff2000, 0, false); Flat(Geo.Circle(6.5f, 32), warnM, c.x, 0.06f, c.z);
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 11f; bool flare = u >= 6.5f && u < 8.7f, charred = u >= 8.7f;
                    float f = flare ? (u - 6.5f) / 2.2f : 0;
                    Mats.SetAlpha(glM, charred ? 0.05f : 0.3f + f * 0.7f * (Mathf.Sin(t * 30) > 0 ? 1 : 0.7f));
                    gl.transform.localScale = Vector3.one * (4 + f * 5);
                    Mats.SetAlpha(warnM, flare ? 0.12f + 0.25f * f * (Mathf.Sin(u * 24) > 0 ? 1 : 0.5f) : 0);
                    if (rand < (charred ? 0.1f : 0.3f + f)) SpawnParticles(c + V((rand - 0.5f) * 2, 2, (rand - 0.5f) * 1.2f), 1, 2, rand < 0.6f ? ashMat : emberMat, 0.15f, 1.2f, -3);
                    if (u - dt < 8.7f && u >= 8.7f)
                    {
                        Blast(c + V(0, 1.5f, 0), 6, 0xff6010); ShockRing(c, 7, 0xff8020, 0.45f); Sfx("boom"); P.shake = Mathf.Max(P.shake, 0.4f);
                        SpawnParticles(c + V(0, 1.5f, 0), 30, 14, emberMat, 0.12f, 1); SpawnParticles(c + V(0, 1.5f, 0), 16, 5, ashMat, 0.35f, 1.8f, -2);
                        if (HazardLive && Vector3.Distance(ChestPos, c + V(0, 1, 0)) < 6.5f) { var o = P.pos - c; o.y = 0; HazardHitPlayer(28, c, o.normalized * 15 + V(0, 8, 0)); }
                        HazardHitEnemies(e => Hypot(e.pos.x - c.x, e.pos.z - c.z) < 6.5f && e.pos.y < 4, 8, e => { var o = e.pos - c; o.y = 0; return o.normalized * 14 + V(0, 8, 0); }, 1);
                    }
                });
            }
        }
        // ---------- 12. trains ----------
        void MechTrains(float[] lanes)
        {
            float L = LB * 2;
            var railM = Mats.Std(0x8a8a90, 0.3f, 0.8f); var sleeperM = Mats.Std(0x3a2a1a, 0.9f);
            var trainM = Mats.Std(0x5a5a62, 0.45f, 0.6f); var stripe = Mats.Std(0xc85020, 0.6f); var win = Mats.Basic(0xffd890);
            for (int i = 0; i < lanes.Length; i++)
            {
                float lx = lanes[i], off = i * 4.6f; int dirZ = i % 2 == 0 ? 1 : -1;
                foreach (var s in new[] { -0.75f, 0.75f }) Deco(Geo.Box(0.15f, 0.2f, L), railM, lx + s, 0.1f, 0);
                for (float z = -LB + 1; z < LB; z += 2.2f) Deco(Geo.Box(2.6f, 0.08f, 0.4f), sleeperM, lx, 0.04f, z);
                var laneM = HazMat(0xff2010, 0, false); Flat(Geo.Plane(4.4f, L), laneM, lx, 0.07f, 0);
                // the tunnel mouths in the end walls
                foreach (var sz in new[] { -1, 1 }) { Deco(Geo.Box(6, 6, 0.4f), Mats.Basic(0x050404), lx, 3, sz * (LB - 0.05f)); Deco(Geo.Arc(3.2f, 0.4f, 16), M.metal, lx, 6, sz * (LB - 0.2f)); }
                var head = Mk.Glow(levelRoot, 0xfff0c0, 6, 0); head.transform.localPosition = V(lx, 2.2f, -dirZ * (LB - 1)); var headM = head.GetComponent<Renderer>().sharedMaterial;
                var tr = Mk.Piv(levelRoot, lx, 0, -dirZ * (LB + 40));
                Mk.Msh(tr, Geo.Box(4, 3.8f, 26), trainM, 0, 2.3f, 0); Mk.Msh(tr, Geo.Box(4.05f, 0.5f, 26.05f), stripe, 0, 1.4f, 0);
                for (int k = 0; k < 6; k++) foreach (var sx in new[] { -1, 1 }) Mk.Msh(tr, Geo.Box(0.06f, 1.0f, 2.6f), win, sx * 2.03f, 2.9f, -10.5f + k * 4.2f);
                Mk.Msh(tr, Geo.Box(3.6f, 1.2f, 0.06f), win, 0, 2.9f, dirZ * 13.03f);
                var lamp = Mk.Glow(tr.t, 0xfff0c0, 4, 0.9f); lamp.transform.localPosition = V(0, 1.6f, dirZ * 13.4f);
                foreach (var g in tr.t.GetComponentsInChildren<MeshRenderer>()) g.gameObject.layer = Mk.LayerIgnore;
                tr.t.gameObject.SetActive(false);
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 10f; bool warn = u >= 4.5f && u < 6.5f, pass = u >= 6.5f && u < 6.5f + (L + 60) / 55f;
                    Mats.SetAlpha(laneM, warn ? 0.12f + 0.2f * (Mathf.Sin(u * 20) > 0 ? 1 : 0) : 0);
                    Mats.SetAlpha(headM, warn ? (u - 4.5f) / 2f : 0);
                    tr.t.gameObject.SetActive(pass);
                    if (!pass) return;
                    float zc = -dirZ * (LB + 30) + dirZ * (u - 6.5f) * 55; tr.t.localPosition = V(lx, 0, zc);
                    if (u - dt < 6.5f) Sfx("boom");
                    if (rand < 0.5f) SpawnParticles(V(lx + (rand - 0.5f) * 3, 0.3f, zc - dirZ * 13), 1, 3, sparkMat, 0.06f, 0.3f);
                    if (!HazardLive) return;
                    if (Mathf.Abs(P.pos.x - lx) < 2.4f && P.pos.y < 4.4f && Mathf.Abs(P.pos.z - zc) < 13.5f)
                        HazardHitPlayer(32, V(lx, 1, zc), V(Mathf.Sign(P.pos.x - lx + 0.01f) * 20, 9, dirZ * 16));
                    HazardHitEnemies(e => Mathf.Abs(e.pos.x - lx) < 2.4f && e.pos.y < 4.4f && Mathf.Abs(e.pos.z - zc) < 13.5f, 25, e => V(Mathf.Sign(e.pos.x - lx + 0.01f) * 18, 9, dirZ * 14), 1);
                });
            }
        }
        // ---------- 13. collapsing road ----------
        void MechCollapse((float x, float z, float w, float d)[] segs, float deckY, float thick)
        {
            for (int i = 0; i < segs.Length; i++)
            {
                var s = segs[i]; float off = i * 2.1f;
                var g = Box(s.x, deckY, s.z, s.w, thick, s.d, M.block); var box = colliders[colliders.Count - 1]; var bc = g.GetComponent<BoxCollider>();
                var lines = Deco(Geo.Box(s.w, 0.03f, 0.3f), Mats.Std(0xd8c070, 0.8f), 0, 0, 0); lines.transform.SetParent(g.transform, false); lines.transform.localPosition = V(0, thick / 2 + 0.02f, 0);
                var crackM = HazMat(0xff8020, 0, true); var cr = Deco(Geo.Box(s.w * 0.9f, 0.04f, 0.2f), crackM, 0, 0, 0); cr.transform.SetParent(g.transform, false); cr.transform.localPosition = V(0, thick / 2 + 0.04f, s.d * 0.2f); cr.transform.localRotation = Quaternion.Euler(0, 8, 0); cr.layer = Mk.LayerIgnore;
                var home = g.transform.localPosition; bool solid = true;
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 13f; float y = deckY + thick / 2, shake = 0;
                    if (u >= 7 && u < 8.5f) { shake = (u - 7) / 1.5f; if (rand < 0.4f) SpawnParticles(V(s.x + (rand - 0.5f) * s.w, deckY - 0.2f, s.z + (rand - 0.5f) * s.d), 1, 1, dustMat, 0.2f, 0.8f); }
                    else if (u >= 8.5f && u < 9.3f) { float k = (u - 8.5f) / 0.8f; y = Mathf.Lerp(deckY, thick * 0.5f, k * k) + thick / 2; }
                    else if (u >= 9.3f && u < 11.6f) y = thick;
                    else if (u >= 11.6f) { float k = (u - 11.6f) / 1.4f; y = Mathf.Lerp(thick, deckY + thick / 2, Smoother(k)); }
                    Mats.SetAlpha(crackM, shake > 0 ? 0.3f + 0.6f * shake * (Mathf.Sin(t * 30) > 0 ? 1 : 0.5f) : 0);
                    g.transform.localPosition = V(home.x + (rand - 0.5f) * shake * 0.2f, y, home.z + (rand - 0.5f) * shake * 0.2f);
                    bool want = u < 8.5f;
                    if (want != solid) { solid = want; if (want) colliders.Add(box); else colliders.Remove(box); bc.enabled = want; }
                    if (u - dt < 9.3f && u >= 9.3f)
                    {
                        Sfx("slam"); P.shake = Mathf.Max(P.shake, 0.5f);
                        for (int k = 0; k < 10; k++) SpawnParticles(V(s.x + (rand - 0.5f) * s.w, 0.5f, s.z + (rand - 0.5f) * s.d), 2, 5, dustMat, 0.3f, 1);
                        if (HazardLive && Mathf.Abs(P.pos.x - s.x) < s.w / 2 && Mathf.Abs(P.pos.z - s.z) < s.d / 2 && P.pos.y < 2.5f) HazardHitPlayer(30, V(s.x, 1, s.z), V(0, 10, Mathf.Sign(P.pos.z - s.z) * 14));
                        HazardHitEnemies(e => Mathf.Abs(e.pos.x - s.x) < s.w / 2 && Mathf.Abs(e.pos.z - s.z) < s.d / 2 && e.pos.y < 2.5f, 20, e => V(0, 8, Mathf.Sign(e.pos.z - s.z) * 12), 1);
                    }
                });
            }
        }
        // ---------- 14. piston crushers and conveyor belts ----------
        void MechCrushers(Vector3[] spots)
        {
            var headM = Mats.Std(0x4a4440, 0.45f, 0.75f); var rodM = Mats.Std(0x8a8a90, 0.25f, 0.9f); var stripe = Mats.Std(0xd8a020, 0.7f);
            for (int i = 0; i < spots.Length; i++)
            {
                var c = spots[i]; float off = i * 0.97f; const float TOP = 13, SZ = 4.6f;
                Deco(Geo.Box(SZ + 1.2f, 0.6f, SZ + 1.2f), M.metal, c.x, 25.7f, c.z); Deco(Geo.Cyl(1.2f, 1.2f, 3, 12), headM, c.x, 24, c.z);
                var hd = Mk.Piv(levelRoot, c.x, TOP, c.z);
                Mk.Msh(hd, Geo.Box(SZ, 1.6f, SZ), headM, 0, 0.8f, 0); Mk.Msh(hd, Geo.Box(SZ + 0.05f, 0.3f, SZ + 0.05f), stripe, 0, 0.15f, 0);
                var rod = Mk.Msh(hd, Geo.Cyl(0.5f, 0.5f, 1, 10), rodM, 0, 0, 0);
                var shM = HazMat(0x000000, 0, false); Flat(Geo.Plane(SZ, SZ), shM, c.x, 0.06f, c.z);
                Deco(Geo.Box(SZ + 0.8f, 0.12f, SZ + 0.8f), stripe, c.x, 0.06f, c.z);
                bool hit = false; var hitE = new HashSet<Ent>();
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 6f, y = TOP;
                    if (u >= 3 && u < 4.2f) { y = TOP + 1.2f * ((u - 3) / 1.2f); if (rand < 0.4f) SpawnParticles(V(c.x + (rand - 0.5f) * SZ, TOP + 1.8f, c.z + (rand - 0.5f) * SZ), 1, 2, dustMat, 0.25f, 0.8f, -2); }
                    else if (u >= 4.2f && u < 4.35f) y = Mathf.Lerp(TOP + 1.2f, 0, (u - 4.2f) / 0.15f);
                    else if (u >= 4.35f && u < 5) y = 0;
                    else if (u >= 5) y = Mathf.Lerp(0, TOP, (u - 5));
                    hd.t.localPosition = V(c.x, y, c.z);
                    float rl = 24 - 1.5f - (y + 1.6f); rod.transform.localScale = V(1, Mathf.Max(0.1f, rl), 1); rod.transform.localPosition = V(0, 1.6f + rl / 2, 0);
                    Mats.SetAlpha(shM, u >= 3 && u < 5 ? 0.25f + 0.4f * Mathf.Clamp01((u - 3) / 1.2f) : 0.08f);
                    if (u >= 4.35f && u - dt < 4.35f) { Sfx("slam"); P.shake = Mathf.Max(P.shake, 0.4f); SpawnParticles(c + V(0, 0.3f, 0), 20, 7, dustMat, 0.25f, 0.8f); SpawnParticles(c + V(0, 0.3f, 0), 10, 8, sparkMat, 0.06f, 0.4f); }
                    if (y > 2.2f) { hit = false; hitE.Clear(); return; }
                    if (!HazardLive) return;
                    if (!hit && Mathf.Abs(P.pos.x - c.x) < SZ / 2 + 0.3f && Mathf.Abs(P.pos.z - c.z) < SZ / 2 + 0.3f && P.pos.y < y + 0.5f) { hit = true; var o = P.pos - c; o.y = 0; HazardHitPlayer(45, c, o.normalized * 14 + V(0, 10, 0)); }
                    HazardHitEnemies(e => Mathf.Abs(e.pos.x - c.x) < SZ / 2 + 0.3f && Mathf.Abs(e.pos.z - c.z) < SZ / 2 + 0.3f && e.pos.y < 2 && hitE.Add(e), 30, e => V(0, 4, 0), 1);
                });
            }
        }
        void MechConveyors((float x, float z, float w, float d, Vector3 dir)[] belts)
        {
            var beltM = Mats.Std(0x1a1816, 0.85f); var slatM = Mats.Std(0x3a3632, 0.7f, 0.4f);
            foreach (var b in belts)
            {
                Deco(Geo.Box(b.w, 0.14f, b.d), beltM, b.x, 0.07f, b.z);
                foreach (var s in new[] { -1, 1 }) Deco(Geo.Box(b.w, 0.4f, 0.3f), M.metal, b.x, 0.2f, b.z + s * (b.d / 2 + 0.15f));
                var slats = new List<Transform>(); int n = (int)(b.w / 2.5f);
                for (int k = 0; k < n; k++) { var sl = Deco(Geo.Box(0.4f, 0.05f, b.d - 0.3f), slatM, b.x - b.w / 2 + k * 2.5f, 0.16f, b.z); sl.layer = Mk.LayerIgnore; slats.Add(sl.transform); }
                const float SPEED = 7;
                trapAnims.Add((t, dt) =>
                {
                    for (int k = 0; k < slats.Count; k++) { var p = slats[k].localPosition; p.x = b.x - b.w / 2 + Mathf.Repeat(k * 2.5f + t * SPEED * b.dir.x, b.w); slats[k].localPosition = p; }
                    if (!HazardLive) return;
                    if (P.onGround && P.pos.y < 0.5f && Mathf.Abs(P.pos.x - b.x) < b.w / 2 && Mathf.Abs(P.pos.z - b.z) < b.d / 2) P.pos += b.dir * SPEED * dt;
                    foreach (var e in enemies) if (!e.dead && e.type == "husk" && e.ground && e.pos.y < 0.5f && Mathf.Abs(e.pos.x - b.x) < b.w / 2 && Mathf.Abs(e.pos.z - b.z) < b.d / 2) e.pos += b.dir * SPEED * dt;
                });
            }
        }
        // ---------- 16. the tide ----------
        void MechTide()
        {
            var wm = Mats.Trans(0x6a7a80, 0.72f); var water = Deco(Geo.Plane(LB * 2, LB * 2), wm, 0, -0.4f, 0, -Mathf.PI / 2); water.layer = Mk.LayerIgnore;
            var foam = Mats.Add(0xe8f0f0, 0.15f, Tex.Holy, true, true); var fo = Deco(Geo.Plane(LB * 2, LB * 2), foam, 0, -0.38f, 0, -Mathf.PI / 2); fo.layer = Mk.LayerIgnore; foam.SetTextureScale("_BaseMap", V(6, 6, 0));
            float hitT = 0;
            trapAnims.Add((t, dt) =>
            {
                float u = (t + 2) % 15f, h;
                if (u < 7) h = -0.4f; else if (u < 9) h = Mathf.Lerp(-0.4f, 1.15f, Smoother((u - 7) / 2)); else if (u < 12.5f) h = 1.15f; else h = Mathf.Lerp(1.15f, -0.4f, Smoother((u - 12.5f) / 2.5f));
                h += Mathf.Sin(t * 1.3f) * 0.06f;
                water.transform.localPosition = V(0, h, 0); fo.transform.localPosition = V(0, h + 0.02f, 0);
                foam.SetTextureOffset("_BaseMap", V(t * 0.02f, Mathf.Sin(t * 0.3f) * 0.05f, 0));
                if (h > 0 && rand < 0.4f) SpawnParticles(V((rand - 0.5f) * LB * 1.8f, h + 0.1f, (rand - 0.5f) * LB * 1.8f), 1, 0.5f, ashMat, 0.2f, 1.6f, -0.3f);
                if (!HazardLive || h < 0.1f) return;
                hitT -= dt;
                if (P.pos.y < h - 0.15f) { hazSlowT = 0.2f; P.vel.y -= 6 * dt; if (hitT <= 0) { hitT = 0.9f; HazardHitPlayer(5, P.pos, Vector3.zero); } }
                foreach (var e in enemies) if (!e.dead && e.type == "husk" && e.pos.y < h - 0.15f) { float k = Mathf.Exp(-4 * dt); e.vel.x *= k; e.vel.z *= k; }
            });
        }
        // ---------- 17. pendulums ----------
        void MechPendulums(float[] xs, float anchorY, float len)
        {
            var gold = Mats.Std(0xd8b860, 0.3f, 0.85f); var edge = Mats.Std(0xe8ecf0, 0.15f, 0.9f);
            for (int i = 0; i < xs.Length; i++)
            {
                float x = xs[i], ph = i * 2.2f;
                Deco(Geo.Box(2, 1.2f, 70), M.tower, x, anchorY + 0.6f, 0); SolidAABB(V(x - 1, anchorY, -35), V(x + 1, anchorY + 1.2f, 35));
                var piv = Mk.Piv(levelRoot, x, anchorY, 0);
                Mk.Msh(piv, Geo.Box(0.4f, len, 0.4f), gold, 0, -len / 2, 0);
                var blade = Mk.Piv(piv, 0, -len, 0);
                Mk.Msh(blade, Geo.Cyl(3, 3, 0.3f, 32), gold, 0, 0, 0, Mathf.PI / 2, 0, 0); Mk.Msh(blade, Geo.Torus(3, 0.12f, 40, 4), edge, 0, 0, 0); // the disc swings edge-first
                Mk.Msh(blade, Geo.Sphere(0.6f, 12, 8), M.tower);
                foreach (var g in piv.t.GetComponentsInChildren<MeshRenderer>()) g.gameObject.layer = Mk.LayerIgnore;
                Vector3 last = blade.t.position;
                trapAnims.Add((t, dt) =>
                {
                    piv.SetRot(0, 0, 0.95f * Mathf.Sin(t * 1.35f + ph));
                    var bp = blade.t.position; var vel = dt > 0 ? (bp - last) / dt : Vector3.zero; last = bp;
                    if (Mathf.Abs(vel.x) > 6 && rand < 0.3f) SpawnParticles(bp + V(0, -2.8f, 0), 1, 2, sparkMat, 0.05f, 0.3f);
                    if (!HazardLive) return;
                    var c = ChestPos; var d = c - bp;
                    if (Mathf.Abs(d.z) < 0.9f && V(d.x, d.y, 0).magnitude < 3.3f) HazardHitPlayer(25, bp, vel.normalized * 16 + V(0, 6, 0));
                    HazardHitEnemies(e => { var de = BodyPoint(e) - bp; return Mathf.Abs(de.z) < 1 + e.hitR * 0.5f && V(de.x, de.y, 0).magnitude < 3 + e.hitR; }, 12, e => vel.normalized * 16 + V(0, 6, 0));
                });
            }
        }
        // ---------- 18. time bubbles ----------
        void MechTimeBubbles(int n)
        {
            for (int i = 0; i < n; i++)
            {
                float ph = i * 2.1f; const float R = 6.5f;
                var bm = Mats.Trans(0xffd890, 0.12f); var bub = Deco(Geo.Sphere(1, 24, 16), bm, 0, 3, 0); bub.layer = Mk.LayerIgnore; bub.transform.localScale = Vector3.one * R;
                var rimM = Mats.Add(0xffe0a0, 0.25f, null, true, true); var rim = Deco(Geo.Torus(R, 0.05f, 48, 4), rimM, 0, 3, 0); rim.layer = Mk.LayerIgnore;
                var gl = Mk.Glow(levelRoot, 0xffd890, R * 1.6f, 0.15f);
                trapAnims.Add((t, dt) =>
                {
                    float lim = LB - R - 2;
                    var c = V(Mathf.Sin(t * 0.11f + ph) * lim * 0.8f, 3 + Mathf.Sin(t * 0.5f + ph) * 0.8f, Mathf.Sin(t * 0.083f + ph * 1.7f) * lim * 0.8f);
                    bub.transform.localPosition = c; rim.transform.localPosition = c; rim.transform.localRotation = Quaternion.Euler(t * 20, t * 31 + ph * 40, 0); gl.transform.localPosition = c;
                    bub.transform.localScale = Vector3.one * R * (1 + 0.03f * Mathf.Sin(t * 2 + ph));
                    if (rand < 0.3f) SpawnParticles(c + UnityEngine.Random.onUnitSphere * R * 0.9f, 1, 0.2f, goldSpark, 0.05f, 1.2f, 0);
                    if (!HazardLive) return;
                    if (Vector3.Distance(ChestPos, c) < R) hazSlowT = 0.15f;
                    foreach (var e in enemies) if (!e.dead && e.type != "boss" && Vector3.Distance(BodyPoint(e), c) < R) e.timeSlowT = 0.2f;
                    foreach (var p in projectiles) if (!p.friendly && !p.slowed && Vector3.Distance(p.pos, c) < R) { p.slowed = true; p.vel *= 0.3f; p.life += 4; }
                });
            }
        }
        // ---------- 19. mirror glints ----------
        void MechMirrors(Vector3[] spots)
        {
            var glass = Mats.Std(0xe8f0f8, 0.03f, 1f); var frame = Mats.Std(0xd8b860, 0.3f, 0.85f); var unit = Geo.Cyl(1, 1, 1, 8);
            for (int i = 0; i < spots.Length; i++)
            {
                var c = spots[i]; float off = i * 1.5f; var face = V(-c.x, 0, -c.z).normalized; float ry = Mathf.Atan2(face.x, face.z);
                var m = Mk.Piv(levelRoot, c.x, 0, c.z); m.SetRot(0, ry, 0);
                Mk.Msh(m, Geo.Box(7, 11, 0.3f), glass, 0, 7.5f, 0); Mk.Msh(m, Geo.Box(7.8f, 0.6f, 0.5f), frame, 0, 13.2f, 0); Mk.Msh(m, Geo.Box(7.8f, 0.6f, 0.5f), frame, 0, 1.8f, 0);
                foreach (var sx in new[] { -1, 1 }) Mk.Msh(m, Geo.Box(0.6f, 12, 0.5f), frame, sx * 3.7f, 7.5f, 0);
                Mk.Msh(m, Geo.Box(2, 1.6f, 2), M.tower, 0, 0.8f, -0.6f);
                SolidAABB(V(c.x - 4, 0, c.z - 4), V(c.x + 4, 13.5f, c.z + 4));
                var src = V(c.x, 7.5f, c.z) + face * 0.6f;
                var beamM = HazMat(0xfff4d0, 0, true); var beam = Deco(unit, beamM, 0, 0, 0); beam.layer = Mk.LayerIgnore; beam.SetActive(false);
                var flare = Mk.Glow(levelRoot, 0xfff4d0, 8, 0); flare.transform.localPosition = src; var flareM = flare.GetComponent<Renderer>().sharedMaterial;
                Vector3 aim = Vector3.zero; bool fired = false;
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 7.5f; bool tracing = u >= 4.5f && u < 6.3f, firing = u >= 6.3f && u < 6.6f;
                    if (!HazardLive || (!tracing && !firing)) { beam.SetActive(false); Mats.SetAlpha(flareM, 0); fired = false; if (!tracing) aim = ChestPos; return; }
                    if (tracing) { aim = Vector3.Lerp(aim, ChestPos, 1 - Mathf.Exp(-3 * dt)); if (!Los(src, ChestPos)) { beam.SetActive(false); Mats.SetAlpha(flareM, 0.1f); return; } }
                    var dir = (aim - src).normalized; float L = 160;
                    if (Physics.Raycast(src, dir, out var hit, L, LEVEL_MASK, QueryTriggerInteraction.Ignore)) L = hit.distance;
                    beam.SetActive(true);
                    float w = tracing ? 0.04f + 0.05f * (u - 4.5f) / 1.8f : 0.5f;
                    beam.transform.localPosition = src + dir * L / 2; beam.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir); beam.transform.localScale = V(w, L, w);
                    Mats.SetAlpha(beamM, tracing ? 0.3f + 0.4f * (Mathf.Sin(t * 30) > 0 ? 1 : 0.5f) : 0.95f);
                    Mats.SetAlpha(flareM, tracing ? 0.4f : 1);
                    if (firing && !fired)
                    {
                        fired = true; Sfx("holy"); Blast(src, 3, 0xfff4d0);
                        var cp = ChestPos; float along = Vector3.Dot(cp - src, dir);
                        if (along > 0 && along < L && Vector3.Distance(src + dir * along, cp) < 1.2f) HazardHitPlayer(24, src, dir * 10 + V(0, 4, 0));
                        HazardHitEnemies(e => { var bp = BodyPoint(e); float a = Vector3.Dot(bp - src, dir); return a > 0 && a < L && Vector3.Distance(src + dir * a, bp) < e.hitR + 0.8f; }, 10, e => dir * 10 + V(0, 4, 0));
                    }
                });
            }
        }
        // ---------- 20. the scales ----------
        void MechScales(float px, float size)
        {
            var gold = Mats.Std(0xd8b860, 0.3f, 0.85f); var unit = Geo.Cyl(1, 1, 1, 6);
            var beam = Mk.Piv(levelRoot, 0, 22, 0); Mk.Msh(beam, Geo.Box(px * 2 + 2, 1.2f, 1.6f), gold); Mk.Msh(beam, Geo.Sphere(1.6f, 14, 10), gold);
            Deco(Geo.Box(2, 2, 2), gold, 0, 20.5f, 0);
            var pans = new List<(GameObject g, int idx, float sx)>(); var chains = new List<(Transform t, float sx, Vector3 corner)>();
            foreach (var sx in new[] { -1f, 1f })
            {
                var g = Box(sx * px, 5, 0, size, 1, size, M.block); pans.Add((g, colliders.Count - 1, sx));
                foreach (var cx in new[] { -1f, 1f }) foreach (var cz in new[] { -1f, 1f }) { var ch = Deco(unit, gold, 0, 0, 0); ch.layer = Mk.LayerIgnore; chains.Add((ch.transform, sx, V(cx * size / 2 * 0.9f, 1, cz * size / 2 * 0.9f))); }
                var trim = Deco(Geo.Torus(size * 0.55f, 0.2f, 40, 4), gold, 0, 0, 0, Mathf.PI / 2); trim.transform.SetParent(g.transform, false); trim.transform.localPosition = V(0, 0.55f, 0);
            }
            trapAnims.Add((t, dt) =>
            {
                float s = Mathf.Sin(t * 0.55f) * 4.5f;
                foreach (var p in pans)
                {
                    float y = 5 + s * p.sx;
                    p.g.transform.localPosition = V(p.sx * px, y + 0.5f, 0);
                    colliders[p.idx] = new AABB(V(p.sx * px - size / 2, y, -size / 2), V(p.sx * px + size / 2, y + 1, size / 2));
                }
                beam.SetRot(0, 0, Mathf.Atan2(s * 2, px * 2));
                foreach (var c in chains)
                {
                    float y = 5 + s * c.sx + 1; var a = beam.t.TransformPoint(V(c.sx * px, 0, 0)); var b = V(c.sx * px + c.corner.x, y, c.corner.z);
                    var d = b - a; c.t.localPosition = (a + b) / 2; c.t.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized); c.t.localScale = V(0.12f, d.magnitude, 0.12f);
                }
            });
        }
    }
}
