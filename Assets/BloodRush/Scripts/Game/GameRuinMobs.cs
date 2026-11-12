using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    // Dead Earth's own people (levels 11-15). Same husk skeleton as every other husk (so AnimateHusk, hitboxes, executions,
    // gore and hazards work unchanged), same kinds, stats and behaviour (GameKinds) - only the bodies are new:
    //   THE ASHEN    (kind "rust")    a survivor the fire got: charred skin split by ember-lit cracks, a torn hoodie, jeans, one
    //                                 boot, a respirator with a cracked filter, goggles pushed up, a length of rebar through one
    //                                 shoulder, a lead pipe in its fist
    //   SCRAPPER     (kind "scav")    small, wiry, hunched: a welder's bucket helmet with goggle lenses and an amber slit, a rag
    //                                 poncho, a hubcap pauldron, taped forearms, a shiv in each hand
    //   BURNER       (kind "bomber")  a faded hazmat coverall, a full gas mask with red eyepieces, a harness of pipe bombs and
    //                                 dynamite wired to a blinking detonator, a lit road flare
    //   HAULER       (kind "tank")    a giant in welded scrap: a car door for a breastplate, tyres for pauldrons, a road sign
    //                                 strapped to each shin, a welding helmet, the fuel tank on his back, a concrete sledgehammer
    //   SCRAP DRONE  (kind "rustdrone") the eye in a welded cage of scrap plate, two rotors on outriggers, an antenna, cables
    // All of it is weathered with painted grime / rust textures (below), which Dead Earth's maps use too.
    public partial class Game
    {
        // ================= weathering textures (painted once; they tile) =================
        static Texture2D grimeTex, rustTex, clothTex2, skinTex2;
        static float WHash(int x, int y, int s) { unchecked { int h = x * 374761393 + y * 668265263 + s * 1442695041; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffff) / 65535f; } }
        static float WNoise(float x, float y, int period, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y); float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            int x0 = ((xi % period) + period) % period, y0 = ((yi % period) + period) % period, x1 = (x0 + 1) % period, y1 = (y0 + 1) % period;
            return Mathf.Lerp(Mathf.Lerp(WHash(x0, y0, seed), WHash(x1, y0, seed), fx), Mathf.Lerp(WHash(x0, y1, seed), WHash(x1, y1, seed), fx), fy);
        }
        static float WFbm(float u, float v, int seed, int p0 = 4, int oct = 4) { float a = 0, amp = 0.5f; int p = p0; for (int o = 0; o < oct; o++) { a += amp * WNoise(u * p, v * p, p, seed + o * 17); p *= 2; amp *= 0.5f; } return a / 0.94f; }
        static Texture2D PaintTex(System.Func<float, float, int, int, Color> f, int N = 256)
        {
            var t = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, anisoLevel = 4, filterMode = FilterMode.Trilinear };
            var px = new Color32[N * N];
            for (int j = 0; j < N; j++) for (int i = 0; i < N; i++) px[j * N + i] = f(i / (float)N, j / (float)N, i, j);
            t.SetPixels32(px); t.Apply(true); return t;
        }
        // scratches / cracks: thin random-walk lines darkened (or lightened) into a buffer
        static void Scribble(float[] buf, int N, int seed, int count, int len, float amt, bool wander)
        {
            var rng = new System.Random(seed);
            for (int c = 0; c < count; c++)
            {
                float x = (float)rng.NextDouble() * N, y = (float)rng.NextDouble() * N, a = (float)rng.NextDouble() * 6.28f;
                for (int s = 0; s < len; s++)
                {
                    int xi = ((int)x % N + N) % N, yi = ((int)y % N + N) % N; buf[yi * N + xi] += amt;
                    if (wander) a += ((float)rng.NextDouble() - 0.5f) * 0.9f;
                    x += Mathf.Cos(a); y += Mathf.Sin(a);
                }
            }
        }
        // concrete, plaster, asphalt, skin of the buildings: stains, specks, cracks
        static Texture2D GrimeTex()
        {
            if (grimeTex) return grimeTex;
            const int N = 256; var cr = new float[N * N]; Scribble(cr, N, 11, 14, 90, -0.35f, true);
            return grimeTex = PaintTex((u, v, i, j) =>
            {
                float n = WFbm(u, v, 3), m = WFbm(u, v, 40, 16, 2), streak = WFbm(u * 0.5f, v * 0.125f + u * 0.02f, 77, 8, 3);
                float g = 0.92f - Mathf.SmoothStep(0.42f, 0.75f, n) * 0.28f - (m - 0.5f) * 0.12f - Mathf.SmoothStep(0.55f, 0.8f, streak) * 0.12f;
                if (WHash(i, j, 5) > 0.986f) g -= 0.3f; else if (WHash(i, j, 6) > 0.99f) g += 0.12f;
                g += cr[j * N + i]; g = Mathf.Clamp01(g);
                float warm = Mathf.SmoothStep(0.5f, 0.8f, n) * 0.05f;
                return new Color(g + warm, g, g - warm, 1);
            });
        }
        // steel and painted metal: rust blooming in patches and running in streaks, bright scratches through the paint
        static Texture2D RustTex()
        {
            if (rustTex) return rustTex;
            const int N = 256; var sc = new float[N * N]; Scribble(sc, N, 23, 40, 26, 0.45f, false);
            return rustTex = PaintTex((u, v, i, j) =>
            {
                float n = WFbm(u, v, 9), drip = WFbm(u, v * 0.25f, 31, 16, 2), pit = WFbm(u, v, 55, 32, 2);
                float r = Mathf.Clamp01(Mathf.SmoothStep(0.5f, 0.72f, n) + Mathf.SmoothStep(0.62f, 0.8f, drip) * 0.6f);
                var bas = new Color(0.9f, 0.9f, 0.9f) * (0.92f + (pit - 0.5f) * 0.18f);
                var rust = Color.Lerp(new Color(1.15f, 0.62f, 0.34f), new Color(0.55f, 0.3f, 0.18f), pit);
                var c = Color.Lerp(bas, rust, r);
                float s = sc[j * N + i]; if (s > 0) c = Color.Lerp(c, new Color(1.2f, 1.2f, 1.2f), Mathf.Clamp01(s) * (1 - r));
                c.a = 1; return c;
            });
        }
        // fabric: a weave, dirt soaked in at the bottom, scorch and stains
        static Texture2D ClothTex2()
        {
            if (clothTex2) return clothTex2;
            return clothTex2 = PaintTex((u, v, i, j) =>
            {
                float weave = ((i + j) % 4 < 2 ? 0.04f : -0.04f) + ((i * 3 + j) % 7 < 2 ? -0.03f : 0);
                float n = WFbm(u, v, 13), d = WFbm(u, v, 71, 8, 3);
                float g = 0.95f + weave - Mathf.SmoothStep(0.5f, 0.8f, n) * 0.3f - Mathf.SmoothStep(0.62f, 0.75f, d) * 0.35f;
                return new Color(g, g * 0.97f, g * 0.92f, 1);
            }, 128);
        }
        // skin: mottled, scarred
        static Texture2D SkinTex2()
        {
            if (skinTex2) return skinTex2;
            const int N = 128; var sc = new float[N * N]; Scribble(sc, N, 37, 10, 22, -0.25f, true);
            return skinTex2 = PaintTex((u, v, i, j) =>
            {
                float n = WFbm(u, v, 19, 4, 4), m = WFbm(u, v, 61, 16, 2);
                float g = Mathf.Clamp01(0.9f - (n - 0.5f) * 0.35f - (m - 0.5f) * 0.15f + sc[j * N + i]);
                return new Color(g * 1.02f, g, g * 0.97f, 1);
            }, N);
        }

        // ================= the bodies =================
        HuskRig BuildRuinHusk(Ent e, bool brute, string kind)
        {
            var r = new HuskRig { ruin = true };
            r.root = Mk.Piv(e.g.transform);
            r.hips = Mk.Piv(r.root, 0, 1.0f, 0);
            switch (kind)
            {
                case "scav": BuildScrapper(e, r); break;
                case "bomber": BuildBurner(e, r); break;
                case "tank": BuildHauler(e, r); break;
                default: BuildAshen(e, r); break;
            }
            return r;
        }
        // a pair of legs: thigh + shin of the given materials, kneecap, and a foot (boot or bare); returns nothing - fills r
        void RuinLegs(HuskRig r, float tr, float sr, Material thighM, Material shinM, Material bootL, Material bootR, float footW = 0.1f)
        {
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var thigh = Mk.Piv(r.hips, 0.13f * sx, -0.06f, 0);
                Mk.Msh(thigh, GeoX.Limb(tr, tr * 0.8f, 0.48f), thighM);
                var knee = Mk.Piv(thigh, 0, -0.48f, 0);
                Mk.Msh(knee, Geo.Sphere(sr * 1.1f, 8, 6), thighM, 0, 0, 0.02f);
                Mk.Msh(knee, GeoX.Limb(sr, sr * 0.72f, 0.4f), shinM);
                var boot = i == 0 ? bootL : bootR;
                Mk.Msh(knee, Geo.Cyl(sr * 0.95f, sr * 1.05f, 0.14f, 10), boot, 0, -0.38f, 0); // the boot's ankle
                Mk.Msh(knee, Geo.Box(footW, 0.08f, 0.24f), boot, 0, -0.45f, 0.06f);
                Mk.Msh(knee, Geo.Box(footW + 0.01f, 0.025f, 0.25f), Mats.Std(0x141210, 0.9f), 0, -0.48f, 0.06f); // sole
                r.thigh[i] = thigh; r.knee[i] = knee;
            }
        }
        // the ember cracks in burnt skin: short glowing slivers on a pivot
        void EmberCracks(N p, Material glow, int n, float rad, float y0, float y1, int seed)
        {
            var rng = new System.Random(seed);
            for (int k = 0; k < n; k++) { float a = (float)rng.NextDouble() * 6.28f, y = Mathf.Lerp(y0, y1, (float)rng.NextDouble()); Mk.Msh(p, Geo.Box(0.008f, 0.05f + (float)rng.NextDouble() * 0.08f, 0.006f), glow, Mathf.Sin(a) * rad, y, Mathf.Cos(a) * rad, 0, a, ((float)rng.NextDouble() - 0.5f) * 1.6f); }
        }

        // ---------- THE ASHEN ----------
        void BuildAshen(Ent e, HuskRig r)
        {
            var skin = EM(e, Mats.Std(0x6a4a3e, 0.8f, 0, 0x080302, SkinTex2())); var bone = EM(e, Mats.Std(0x9a8a78, 0.6f, 0, 0, GrimeTex()));
            var hood = EM(e, Mats.Std(0x7a7a6e, 0.95f, 0, 0x040404, ClothTex2(), true)); var denim = EM(e, Mats.Std(0x46587a, 0.95f, 0, 0x020304, ClothTex2()));
            var leather = EM(e, Mats.Std(0x4a3626, 0.7f, 0, 0, GrimeTex())); var metal = EM(e, Mats.Std(0x5a5650, 0.45f, 0.7f, 0, RustTex()));
            var glow = EM(e, Mats.Basic(0xff6a18)); r.glow = glow;
            // hips: jeans, a belt with a rusted buckle
            Mk.Msh(r.hips, Geo.Sphere(1, 12, 8), denim, 0, 0, 0).transform.localScale = V(0.2f, 0.12f, 0.13f);
            Mk.Msh(r.hips, Geo.Torus(0.19f, 0.022f, 16, 4), leather, 0, 0.06f, 0, Mathf.PI / 2, 0, 0).transform.localScale = V(1, 0.7f, 1);
            Mk.Msh(r.hips, Geo.Box(0.06f, 0.05f, 0.02f), metal, 0, 0.06f, 0.13f);
            RuinLegs(r, 0.085f, 0.064f, denim, denim, leather, skin);
            Mk.Msh(r.thigh[1], Geo.Box(0.1f, 0.12f, 0.02f), skin, 0.02f, -0.3f, 0.075f); // the jeans torn open over one knee
            EmberCracks(r.knee[1], glow, 5, 0.05f, -0.42f, -0.15f, 3);
            // the torso: charred, the hoodie burnt half away; the hood hanging behind the neck; ember cracks across the chest
            r.torso = Mk.Piv(r.hips, 0, 0.06f, 0);
            Mk.Msh(r.torso, GeoX.Capsule(0.12f, 0.12f, 3, 12), skin, 0, 0.16f, 0).transform.localScale = V(1.15f, 1, 0.82f);
            Mk.Msh(r.torso, GeoX.Capsule(0.16f, 0.2f, 3, 12), hood, 0, 0.42f, -0.01f).transform.localScale = V(1.28f, 1, 0.88f);
            Mk.Msh(r.torso, Geo.Box(0.22f, 0.24f, 0.03f), skin, 0.06f, 0.4f, 0.135f, 0, 0, 0.15f); // burnt through over the chest
            EmberCracks(r.torso, glow, 7, 0.15f, 0.3f, 0.5f, 5);
            Mk.Msh(r.torso, Geo.Sphere(0.13f, 12, 6, 0, Mathf.PI * 2, 0, Mathf.PI / 2), hood, 0, 0.62f, -0.12f, -1.9f, 0, 0).transform.localScale = V(1.1f, 0.9f, 0.7f);
            for (int k = 0; k < 4; k++) Mk.Msh(r.torso, Geo.Box(0.07f, 0.12f + k % 2 * 0.05f, 0.015f), hood, -0.16f + k * 0.1f, 0.14f, 0.12f - (k % 2) * 0.03f, 0.1f, 0, (k - 1.5f) * 0.2f); // the ragged hem
            // the rebar through the left shoulder
            Mk.Msh(r.torso, Geo.Cyl(0.014f, 0.014f, 0.9f, 6), metal, -0.2f, 0.55f, 0.0f, Mathf.PI / 2 - 0.35f, 0, 0.25f);
            // neck and head: charred skull-face, a respirator with a cracked filter, goggles pushed up on the brow
            r.neck = Mk.Piv(r.torso, 0, 0.62f, 0.02f); r.neck.SetRot(0.15f, 0, 0);
            Mk.Msh(r.neck, Geo.Cyl(0.045f, 0.06f, 0.14f, 8), skin, 0, 0.05f, 0);
            r.head = Mk.Piv(r.neck, 0, 0.12f, 0.02f);
            var hs = new List<GameObject>();
            var sk = Mk.Msh(r.head, Geo.Sphere(0.14f, 14, 10), skin, 0, 0.09f, 0); sk.transform.localScale = V(0.92f, 1.05f, 1.08f); hs.Add(sk);
            hs.Add(Mk.Msh(r.head, Geo.Box(0.2f, 0.035f, 0.05f), bone, 0, 0.14f, 0.12f, 0.2f, 0, 0)); // the brow, burnt to the bone
            foreach (var sx in new[] { -1f, 1f }) { hs.Add(Mk.Msh(r.head, Geo.Sphere(0.022f, 8, 6), glow, sx * 0.045f, 0.1f, 0.135f)); hs.Add(Mk.Msh(r.head, Geo.Cyl(0.038f, 0.038f, 0.03f, 10), metal, sx * 0.05f, 0.2f, 0.11f, 1.2f, 0, 0)); }
            hs.Add(Mk.Msh(r.head, Geo.Torus(0.14f, 0.012f, 18, 4), leather, 0, 0.18f, 0, Mathf.PI / 2 - 0.25f, 0, 0)); // the goggle strap
            hs.Add(Mk.Msh(r.head, Geo.Box(0.13f, 0.09f, 0.08f), Mats.Std(0x2a2a2a, 0.7f), 0, 0.02f, 0.12f)); // the respirator
            foreach (var sx in new[] { -1f, 1f }) hs.Add(Mk.Msh(r.head, Geo.Cyl(0.035f, 0.035f, 0.06f, 10), sx < 0 ? metal : Mats.Std(0x6a5a40, 0.7f), sx * 0.085f, 0.0f, 0.13f, Mathf.PI / 2, 0, sx * 0.5f));
            hs.Add(Mk.Msh(r.head, Geo.Box(0.004f, 0.05f, 0.002f), glow, 0.085f, 0.01f, 0.165f, 0, 0, 0.5f)); // the crack in the filter, glowing
            EmberCracks(r.head, glow, 4, 0.13f, 0.08f, 0.2f, 9);
            Mk.Head(hs.ToArray());
            r.jaw = Mk.Piv(r.head, 0, -0.02f, 0.03f); Mk.Head(Mk.Msh(r.jaw, Geo.Box(0.11f, 0.035f, 0.08f), bone, 0, -0.03f, 0.05f));
            // arms: the right sleeve still on, the left burnt away to the charred arm; a lead pipe in the right fist
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1; var armM = sx > 0 ? hood : skin;
                var sh = Mk.Piv(r.torso, 0.31f * sx, 0.55f, 0);
                Mk.Msh(sh, Geo.Sphere(0.075f, 8, 6), armM, 0, 0, 0);
                Mk.Msh(sh, GeoX.Limb(0.065f, 0.05f, 0.4f), armM);
                var elbow = Mk.Piv(sh, 0, -0.4f, 0);
                Mk.Msh(elbow, GeoX.Limb(0.052f, 0.04f, 0.38f), skin);
                if (sx < 0) { EmberCracks(sh, glow, 4, 0.055f, -0.35f, -0.05f, 11); EmberCracks(elbow, glow, 3, 0.045f, -0.3f, -0.05f, 12); }
                else for (int k = 0; k < 3; k++) Mk.Msh(elbow, Geo.Torus(0.045f, 0.01f, 10, 4), Mats.Std(0x8a7a60, 0.95f), 0, -0.22f - k * 0.04f, 0, Mathf.PI / 2, 0, k * 0.2f); // a rag wrapped round the wrist
                var hand = Mk.Piv(elbow, 0, -0.4f, 0);
                Mk.Msh(hand, Geo.Box(0.08f, 0.09f, 0.05f), skin, 0, 0.0f, 0);
                for (int k = 0; k < 4; k++) Mk.Msh(hand, Geo.Cyl(0.011f, 0.009f, 0.08f, 5), skin, (k - 1.5f) * 0.02f, -0.07f, 0.015f, 0.3f, 0, 0);
                if (sx > 0) { var pipe = Mk.Piv(hand, 0, -0.04f, 0.02f); pipe.SetRot(1.35f, 0, 0); Mk.Msh(pipe, Geo.Cyl(0.022f, 0.022f, 0.75f, 8), metal, 0, 0.25f, 0); Mk.Msh(pipe, Geo.Cyl(0.034f, 0.034f, 0.07f, 8), metal, 0, 0.6f, 0); }
                r.sh[i] = sh; r.elbow[i] = elbow; r.hand[i] = hand;
            }
            e.mats.Clear(); e.mats.Add(skin); e.mats.Add(bone); e.mats.Add(hood); e.mats.Add(denim);
            r.extra = (en, dt) =>
            {
                if (en.dead) return;
                Mats.SetColor(glow, Mathf.Sin(time * 3 + en.walk * 0.5f) > 0 ? 0xff7a20 : 0xd84a10); // the embers breathing
                if (rand < dt * 2.5f) SpawnParticles(r.torso.t.TransformPoint(V((rand - 0.5f) * 0.3f, 0.3f + rand * 0.3f, 0.1f)), 1, 0.6f, rand < 0.5f ? emberMat : ashMat, 0.03f, 0.9f, -1.5f);
            };
        }

        // ---------- SCRAPPER ----------
        void BuildScrapper(Ent e, HuskRig r)
        {
            var skin = EM(e, Mats.Std(0xb08a68, 0.7f, 0, 0x060402, SkinTex2())); var rag = EM(e, Mats.Std(0x86704e, 0.95f, 0, 0x040302, ClothTex2(), true));
            var metal = EM(e, Mats.Std(0x9a9086, 0.45f, 0.55f, 0, RustTex())); var tape = EM(e, Mats.Std(0x6a6a62, 0.8f, 0, 0, ClothTex2()));
            var dark = EM(e, Mats.Std(0x1e1a16, 0.9f)); var glow = EM(e, Mats.Basic(0xffa020)); r.glow = glow;
            Mk.Msh(r.hips, Geo.Sphere(1, 10, 8), rag, 0, 0, 0).transform.localScale = V(0.18f, 0.12f, 0.12f);
            Mk.Msh(r.hips, Geo.Torus(0.18f, 0.025f, 14, 4), dark, 0, 0.05f, 0, Mathf.PI / 2, 0, 0).transform.localScale = V(1, 0.72f, 1);
            foreach (var (px, pz) in new[] { (-0.15f, 0.08f), (0.16f, 0.06f), (0.12f, -0.12f) }) Mk.Msh(r.hips, Geo.Box(0.07f, 0.08f, 0.05f), tape, px, 0.0f, pz); // pouches
            RuinLegs(r, 0.072f, 0.056f, rag, skin, dark, dark, 0.09f);
            foreach (var kn in r.knee) Mk.Msh(kn, Geo.Box(0.11f, 0.1f, 0.05f), dark, 0, -0.03f, 0.06f); // tyre-rubber knee pads
            r.torso = Mk.Piv(r.hips, 0, 0.06f, 0);
            Mk.Msh(r.torso, GeoX.Capsule(0.1f, 0.1f, 3, 10), skin, 0, 0.15f, 0).transform.localScale = V(1.1f, 1, 0.8f);
            Mk.Msh(r.torso, GeoX.Capsule(0.14f, 0.18f, 3, 10), rag, 0, 0.4f, -0.01f).transform.localScale = V(1.25f, 1, 0.85f);
            // the poncho: strips of rag off the shoulders, longer at the back
            for (int k = 0; k < 9; k++) { float a = (k / 8f) * Mathf.PI * 2; Mk.Msh(r.torso, Geo.Box(0.1f, 0.32f + (k % 3) * 0.07f + (Mathf.Cos(a) < 0 ? 0.1f : 0), 0.012f), rag, Mathf.Sin(a) * 0.2f, 0.36f, Mathf.Cos(a) * 0.15f, 0.15f * Mathf.Cos(a), a, (k % 2 - 0.5f) * 0.15f); }
            Mk.Msh(r.torso, Geo.Cyl(0.11f, 0.13f, 0.03f, 14), metal, -0.27f, 0.62f, 0, 0, 0, 0.5f); // the hubcap pauldron
            Mk.Msh(r.torso, Geo.Cyl(0.04f, 0.04f, 0.035f, 10), dark, -0.29f, 0.645f, 0, 0, 0, 0.5f);
            Mk.Msh(r.torso, Geo.Box(0.03f, 0.45f, 0.02f), dark, 0, 0.45f, 0.13f, 0, 0, 0.55f); // the strap across the chest
            // the head: a welder's bucket helmet, two goggle lenses, a glowing slit, a stub of antenna
            r.neck = Mk.Piv(r.torso, 0, 0.6f, 0.03f); r.neck.SetRot(0.45f, 0, 0);
            Mk.Msh(r.neck, Geo.Cyl(0.04f, 0.05f, 0.12f, 8), skin, 0, 0.05f, 0);
            r.head = Mk.Piv(r.neck, 0, 0.11f, 0.02f);
            var hs = new List<GameObject>();
            hs.Add(Mk.Msh(r.head, Geo.Cyl(0.13f, 0.15f, 0.26f, 12), metal, 0, 0.11f, 0));
            hs.Add(Mk.Msh(r.head, Geo.Cyl(0.135f, 0.135f, 0.02f, 12), dark, 0, 0.245f, 0));
            foreach (var sx in new[] { -1f, 1f }) { hs.Add(Mk.Msh(r.head, Geo.Cyl(0.045f, 0.045f, 0.06f, 10), dark, sx * 0.062f, 0.12f, 0.14f, Mathf.PI / 2)); hs.Add(Mk.Msh(r.head, Geo.Cyl(0.034f, 0.034f, 0.02f, 10), glow, sx * 0.062f, 0.12f, 0.17f, Mathf.PI / 2)); }
            hs.Add(Mk.Msh(r.head, Geo.Box(0.16f, 0.012f, 0.01f), glow, 0, 0.05f, 0.152f));
            hs.Add(Mk.Msh(r.head, Geo.Cyl(0.006f, 0.006f, 0.22f, 4), dark, 0.07f, 0.33f, -0.04f, -0.3f, 0, -0.2f));
            for (int k = 0; k < 5; k++) hs.Add(Mk.Msh(r.head, Geo.Sphere(0.012f, 5, 4), metal, Mathf.Cos(k * 1.25f) * 0.14f, 0.2f, Mathf.Sin(k * 1.25f) * 0.14f)); // rivets
            Mk.Head(hs.ToArray());
            r.jaw = Mk.Piv(r.head, 0, -0.02f, 0.03f); Mk.Head(Mk.Msh(r.jaw, Geo.Box(0.12f, 0.04f, 0.09f), skin, 0, -0.03f, 0.05f));
            // arms: taped forearms, a shiv in each hand
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var sh = Mk.Piv(r.torso, 0.3f * sx, 0.55f, 0);
                Mk.Msh(sh, Geo.Sphere(0.06f, 8, 6), rag, 0, 0, 0);
                Mk.Msh(sh, GeoX.Limb(0.055f, 0.042f, 0.42f), skin);
                var elbow = Mk.Piv(sh, 0, -0.42f, 0);
                Mk.Msh(elbow, GeoX.Limb(0.045f, 0.035f, 0.4f), skin);
                for (int k = 0; k < 4; k++) Mk.Msh(elbow, Geo.Torus(0.042f - k * 0.002f, 0.01f, 10, 4), tape, 0, -0.14f - k * 0.05f, 0, Mathf.PI / 2, 0, k * 0.15f);
                var hand = Mk.Piv(elbow, 0, -0.41f, 0);
                Mk.Msh(hand, Geo.Box(0.07f, 0.08f, 0.05f), skin, 0, 0.0f, 0);
                var kn = Mk.Piv(hand, 0, -0.05f, 0.03f); kn.SetRot(1.2f, 0, 0);
                Mk.Msh(kn, Geo.Box(0.03f, 0.08f, 0.03f), tape, 0, 0.02f, 0); Mk.Msh(kn, Geo.Box(0.012f, 0.24f, 0.04f), metal, 0, 0.18f, 0); Mk.Msh(kn, Geo.Cone(0.022f, 0.06f, 4), metal, 0, 0.33f, 0);
                r.sh[i] = sh; r.elbow[i] = elbow; r.hand[i] = hand;
            }
            e.mats.Clear(); e.mats.Add(skin); e.mats.Add(rag); e.mats.Add(metal); e.mats.Add(tape);
            float twitch = 0;
            r.extra = (en, dt) =>
            {
                if (en.dead) return;
                // a scavenger's twitchy, darting head
                twitch -= dt; if (twitch < 0) twitch = 0.4f + rand * 1.2f;
                if (twitch > 0.25f) r.head.SetRot(r.head.rx, Mathf.Sin(time * 23 + en.walk) * 0.35f, r.head.rz);
            };
        }

        // ---------- BURNER ----------
        void BuildBurner(Ent e, HuskRig r)
        {
            var suit = EM(e, Mats.Std(0xc0a848, 0.85f, 0, 0x060502, ClothTex2())); var rubber = EM(e, Mats.Std(0x1e1e1c, 0.75f, 0, 0, GrimeTex()));
            var metal = EM(e, Mats.Std(0x5a5650, 0.45f, 0.7f, 0, RustTex())); var red = EM(e, Mats.Std(0xa02010, 0.6f, 0, 0, GrimeTex()));
            var dyn = EM(e, Mats.Std(0xb83a20, 0.8f)); var glow = EM(e, Mats.Basic(0xff3010)); var lit = EM(e, Mats.Basic(0xff3010)); r.glow = glow;
            var wire = EM(e, Mats.Std(0x2a6a2a, 0.7f));
            Mk.Msh(r.hips, Geo.Sphere(1, 12, 8), suit, 0, 0, 0).transform.localScale = V(0.2f, 0.12f, 0.13f);
            Mk.Msh(r.hips, Geo.Torus(0.19f, 0.024f, 16, 4), rubber, 0, 0.05f, 0, Mathf.PI / 2, 0, 0).transform.localScale = V(1, 0.7f, 1);
            RuinLegs(r, 0.088f, 0.066f, suit, suit, rubber, rubber, 0.11f);
            foreach (var kn in r.knee) Mk.Msh(kn, Geo.Box(0.15f, 0.03f, 0.15f), Mats.Std(0x3a3a30, 0.9f), 0, -0.05f, 0); // tape where the suit's torn
            r.torso = Mk.Piv(r.hips, 0, 0.06f, 0);
            Mk.Msh(r.torso, GeoX.Capsule(0.12f, 0.12f, 3, 12), suit, 0, 0.16f, 0).transform.localScale = V(1.18f, 1, 0.85f);
            Mk.Msh(r.torso, GeoX.Capsule(0.16f, 0.2f, 3, 12), suit, 0, 0.42f, -0.01f).transform.localScale = V(1.3f, 1, 0.9f);
            Mk.Msh(r.torso, Geo.Box(0.4f, 0.05f, 0.24f), rubber, 0, 0.62f, 0); // the hood collar
            // the harness: straps crossing the chest, pipe bombs and dynamite on it, all wired to a detonator with a blinking light
            foreach (var sx in new[] { -1f, 1f }) Mk.Msh(r.torso, Geo.Box(0.035f, 0.5f, 0.015f), rubber, 0, 0.4f, 0.15f, 0, 0, sx * 0.6f);
            for (int k = 0; k < 3; k++) { Mk.Msh(r.torso, Geo.Cyl(0.035f, 0.035f, 0.22f, 8), k == 1 ? metal : dyn, (k - 1) * 0.1f, 0.3f, 0.17f); Mk.Msh(r.torso, Geo.Cyl(0.038f, 0.038f, 0.025f, 8), metal, (k - 1) * 0.1f, 0.41f, 0.17f); }
            for (int k = 0; k < 2; k++) Mk.Msh(r.torso, Geo.Cyl(0.03f, 0.03f, 0.2f, 8), dyn, (k - 0.5f) * 0.3f, 0.5f, 0.16f, 0, 0, Mathf.PI / 2 * 0.85f);
            Mk.Msh(r.torso, Geo.Box(0.12f, 0.08f, 0.05f), metal, 0, 0.5f, 0.19f);
            Mk.Msh(r.torso, Geo.Box(0.42f, 0.04f, 0.04f), glow, 0, 0.42f, 0.2f);
            var blink = Mk.Msh(r.torso, Geo.Sphere(0.03f, 8, 6), lit, 0.03f, 0.53f, 0.22f);
            var bg = Gl(r.torso, 0xff3010, 0.8f, 0.8f); bg.transform.localPosition = V(0.03f, 0.53f, 0.24f);
            foreach (var (a, b) in new[] { (V(-0.1f, 0.42f, 0.18f), V(-0.03f, 0.5f, 0.2f)), (V(0.1f, 0.42f, 0.18f), V(0.04f, 0.48f, 0.2f)), (V(0, 0.42f, 0.19f), V(0, 0.47f, 0.21f)) })
            { var d = b - a; var w = Mk.Msh(r.torso, Geo.Cyl(0.006f, 0.006f, d.magnitude, 4), wire, (a.x + b.x) / 2, (a.y + b.y) / 2, (a.z + b.z) / 2); w.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized); }
            // the head: a full gas mask, two red eyepieces, a filter canister on the snout
            r.neck = Mk.Piv(r.torso, 0, 0.62f, 0.02f);
            Mk.Msh(r.neck, Geo.Cyl(0.05f, 0.06f, 0.12f, 8), rubber, 0, 0.05f, 0);
            r.head = Mk.Piv(r.neck, 0, 0.12f, 0.02f);
            var hs = new List<GameObject>();
            var hood = Mk.Msh(r.head, Geo.Sphere(0.15f, 14, 10), suit, 0, 0.1f, -0.02f); hood.transform.localScale = V(1, 1.05f, 1.05f); hs.Add(hood);
            var face = Mk.Msh(r.head, Geo.Sphere(0.12f, 12, 8), rubber, 0, 0.07f, 0.06f); face.transform.localScale = V(1, 1.05f, 0.85f); hs.Add(face);
            foreach (var sx in new[] { -1f, 1f }) { hs.Add(Mk.Msh(r.head, Geo.Cyl(0.04f, 0.04f, 0.03f, 12), metal, sx * 0.05f, 0.11f, 0.15f, Mathf.PI / 2)); hs.Add(Mk.Msh(r.head, Geo.Cyl(0.032f, 0.032f, 0.02f, 12), glow, sx * 0.05f, 0.11f, 0.162f, Mathf.PI / 2)); }
            hs.Add(Mk.Msh(r.head, Geo.Cyl(0.045f, 0.05f, 0.1f, 12), metal, 0, -0.0f, 0.18f, Mathf.PI / 2 + 0.4f, 0, 0));
            hs.Add(Mk.Msh(r.head, Geo.Cyl(0.05f, 0.05f, 0.02f, 12), red, 0, -0.03f, 0.225f, Mathf.PI / 2 + 0.4f, 0, 0));
            Mk.Head(hs.ToArray());
            r.jaw = Mk.Piv(r.head, 0, -0.02f, 0.03f); Mk.Head(Mk.Msh(r.jaw, Geo.Box(0.1f, 0.03f, 0.06f), rubber, 0, -0.03f, 0.05f));
            // arms: thick rubber gloves; a lit road flare in the right fist
            Transform flareTip = null;
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var sh = Mk.Piv(r.torso, 0.33f * sx, 0.55f, 0);
                Mk.Msh(sh, Geo.Sphere(0.08f, 8, 6), suit, 0, 0, 0);
                Mk.Msh(sh, GeoX.Limb(0.07f, 0.055f, 0.42f), suit);
                var elbow = Mk.Piv(sh, 0, -0.42f, 0);
                Mk.Msh(elbow, GeoX.Limb(0.058f, 0.045f, 0.4f), suit);
                Mk.Msh(elbow, Geo.Cyl(0.06f, 0.055f, 0.16f, 10), rubber, 0, -0.33f, 0); // the glove cuff
                var hand = Mk.Piv(elbow, 0, -0.42f, 0);
                Mk.Msh(hand, Geo.Box(0.09f, 0.1f, 0.06f), rubber, 0, 0.0f, 0);
                if (sx > 0)
                {
                    var fl = Mk.Piv(hand, 0, -0.04f, 0.02f); fl.SetRot(1.3f, 0, 0);
                    Mk.Msh(fl, Geo.Cyl(0.02f, 0.02f, 0.26f, 8), red, 0, 0.08f, 0); Mk.Msh(fl, Geo.Sphere(0.03f, 8, 6), lit, 0, 0.23f, 0);
                    var fg = Gl(fl, 0xff5020, 0.9f, 0.9f); fg.transform.localPosition = V(0, 0.24f, 0); flareTip = fg.transform;
                }
                r.sh[i] = sh; r.elbow[i] = elbow; r.hand[i] = hand;
            }
            e.mats.Clear(); e.mats.Add(suit); e.mats.Add(rubber); e.mats.Add(metal); e.mats.Add(red);
            r.extra = (en, dt) =>
            {
                float sp = en.fuse > 0 ? 30 : 8; bool on = Mathf.Sin(time * sp) > 0; blink.SetActive(on); bg.SetActive(on);
                if (en.dead || !flareTip) return;
                flareTip.localScale = Vector3.one * (0.85f + Mathf.Sin(time * 40) * 0.15f);
                if (rand < dt * 18) SpawnParticles(flareTip.position, 1, 1.5f, rand < 0.7f ? sparkMat : emberMat, 0.03f, 0.35f, -3);
            };
            e.cd = 999;
        }

        // ---------- HAULER ----------
        void BuildHauler(Ent e, HuskRig r)
        {
            var skin = EM(e, Mats.Std(0x9a6a52, 0.75f, 0, 0x060302, SkinTex2())); var cloth = EM(e, Mats.Std(0x5e5e50, 0.95f, 0, 0x030302, ClothTex2()));
            var plate = EM(e, Mats.Std(0x9a3a2a, 0.55f, 0.45f, 0, RustTex())); var steel = EM(e, Mats.Std(0x76746e, 0.45f, 0.6f, 0, RustTex()));
            var tyre = EM(e, Mats.Std(0x181818, 0.85f, 0, 0, GrimeTex())); var hot = EM(e, Mats.Basic(0xff7a20)); var glow = EM(e, Mats.Basic(0xff8a20)); r.glow = glow;
            var sign = EM(e, Mats.Std(0xc8a030, 0.7f, 0.3f, 0, RustTex())); var conc = EM(e, Mats.Std(0x7a746a, 0.92f, 0, 0, GrimeTex()));
            // hips: heavy work trousers, a chain for a belt, a tyre-rubber kilt
            Mk.Msh(r.hips, Geo.Box(0.46f, 0.24f, 0.3f), cloth, 0, 0, 0);
            for (int k = 0; k < 6; k++) { float a = (k / 5f - 0.5f) * 3.2f; Mk.Msh(r.hips, Geo.Box(0.15f, 0.3f, 0.03f), tyre, Mathf.Sin(a) * 0.24f, -0.15f, Mathf.Cos(a) * 0.16f, 0.15f * Mathf.Cos(a), a, 0); }
            for (int k = 0; k < 10; k++) { float a = k * Mathf.PI / 5; Mk.Msh(r.hips, Geo.Torus(0.035f, 0.01f, 8, 4), steel, Mathf.Sin(a) * 0.24f, 0.1f, Mathf.Cos(a) * 0.16f, 0, a + (k & 1) * 1.57f, Mathf.PI / 2); }
            RuinLegs(r, 0.11f, 0.09f, cloth, cloth, tyre, tyre, 0.17f);
            foreach (var kn in r.knee)
            {
                // a road sign strapped to each shin: a yellow diamond with its black stripe, rusted at the edges
                var sg = Mk.Piv(kn, 0, -0.2f, 0.1f); sg.SetRot(0.05f, 0, Mathf.PI / 4);
                Mk.Msh(sg, Geo.Box(0.18f, 0.18f, 0.015f), sign, 0, 0, 0); Mk.Msh(sg, Geo.Box(0.03f, 0.2f, 0.016f), tyre, 0, 0, 0.002f, 0, 0, Mathf.PI / 4);
                foreach (var y in new[] { -0.08f, 0.08f }) Mk.Msh(kn, Geo.Torus(0.1f, 0.012f, 12, 4), tyre, 0, -0.2f + y, 0, Mathf.PI / 2, 0, 0);
            }
            // the torso: a car door welded on for a breastplate (window frame, handle, mirror stub), tyre pauldrons, chains
            r.torso = Mk.Piv(r.hips, 0, 0.06f, 0);
            Mk.Msh(r.torso, GeoX.Capsule(0.24f, 0.18f, 3, 12), skin, 0, 0.34f, -0.02f).transform.localScale = V(1.3f, 1, 0.95f);
            var door = Mk.Piv(r.torso, 0, 0.36f, 0.22f); door.SetRot(-0.08f, 0, 0);
            Mk.Msh(door, Geo.Box(0.56f, 0.44f, 0.05f), plate, 0, 0, 0);
            Mk.Msh(door, Geo.Box(0.5f, 0.03f, 0.055f), steel, 0, 0.21f, 0); Mk.Msh(door, Geo.Box(0.4f, 0.02f, 0.055f), steel, 0, 0.03f, 0); // the window frame and the trim line
            Mk.Msh(door, Geo.Box(0.08f, 0.025f, 0.03f), steel, 0.16f, -0.02f, 0.03f); // the handle
            Mk.Msh(door, Geo.Box(0.06f, 0.04f, 0.07f), steel, -0.29f, 0.17f, 0.02f); // the mirror stub
            foreach (var (x, y) in new[] { (-0.26f, 0.19f), (0.26f, 0.19f), (-0.26f, -0.19f), (0.26f, -0.19f) }) Mk.Msh(door, Geo.Sphere(0.02f, 6, 4), steel, x, y, 0.03f); // weld bolts
            Mk.Msh(r.torso, Geo.Box(0.5f, 0.34f, 0.05f), steel, 0, 0.36f, -0.22f); // a plate across the back, under the tank
            foreach (var sx in new[] { -1f, 1f }) Mk.Msh(r.torso, Geo.Box(0.04f, 0.6f, 0.02f), cloth, sx * 0.17f, 0.38f, 0.25f, 0, 0, sx * 0.1f);
            // the fuel tank on the back (it blows when he dies), hoses round to a regulator on his chest
            var tank = Mk.Piv(r.torso, 0, 0.34f, -0.3f);
            Mk.Msh(tank, Geo.Cyl(0.2f, 0.2f, 0.62f, 14), steel); Mk.Msh(tank, Geo.Cyl(0.205f, 0.205f, 0.06f, 14), hot, 0, 0.12f, 0);
            Mk.Msh(tank, Geo.Sphere(0.2f, 14, 6), steel, 0, 0.31f, 0); Mk.Msh(tank, Geo.Sphere(0.2f, 14, 6), steel, 0, -0.31f, 0);
            Mk.Msh(tank, Geo.Cyl(0.04f, 0.04f, 0.1f, 8), steel, 0, 0.5f, 0); Mk.Msh(tank, Geo.Cyl(0.06f, 0.06f, 0.02f, 8), sign, 0, 0.55f, 0);
            Mk.Msh(tank, Geo.Box(0.15f, 0.1f, 0.01f), sign, 0, 0, -0.205f); // the hazard label
            var tg = Gl(tank, 0xff6010, 0.7f, 0.5f); tg.transform.localPosition = V(0, 0.12f, -0.2f);
            foreach (var sx in new[] { -1f, 1f })
            {
                Mk.Msh(r.torso, Geo.Torus(0.18f, 0.025f, 14, 5, Mathf.PI), tyre, sx * 0.28f, 0.55f, 0.0f, 0, Mathf.PI / 2, 0).transform.localScale = V(1, 1, 1.2f);
                Mk.Msh(r.torso, Geo.Cyl(0.02f, 0.02f, 0.5f, 6), tyre, sx * 0.24f, 0.5f, -0.05f, 0.2f, 0, sx * 0.7f);
            }
            Mk.Msh(r.torso, Geo.Cyl(0.05f, 0.05f, 0.04f, 10), steel, 0.12f, 0.5f, 0.27f, Mathf.PI / 2);
            // the head, sunk between the shoulders: a welding helmet with a scorched visor and a slit of light
            r.neck = Mk.Piv(r.torso, 0, 0.6f, 0.02f);
            Mk.Msh(r.neck, Geo.Cyl(0.1f, 0.13f, 0.12f, 10), skin, 0, 0.03f, 0);
            r.head = Mk.Piv(r.neck, 0, 0.12f, 0.02f);
            var hs = new List<GameObject>();
            var hm = Mk.Msh(r.head, Geo.Sphere(0.17f, 14, 10), steel, 0, 0.1f, -0.01f); hm.transform.localScale = V(1, 1.05f, 1.05f); hs.Add(hm);
            hs.Add(Mk.Msh(r.head, Geo.Box(0.26f, 0.28f, 0.06f), plate, 0, 0.08f, 0.15f, -0.08f, 0, 0));
            hs.Add(Mk.Msh(r.head, Geo.Box(0.17f, 0.05f, 0.02f), Mats.Std(0x0a0806, 0.1f, 0.3f), 0, 0.12f, 0.185f));
            hs.Add(Mk.Msh(r.head, Geo.Box(0.15f, 0.012f, 0.01f), glow, 0, 0.12f, 0.197f));
            foreach (var sx in new[] { -1f, 1f }) hs.Add(Mk.Msh(r.head, Geo.Cyl(0.03f, 0.03f, 0.03f, 8), steel, sx * 0.165f, 0.1f, 0.02f, 0, 0, Mathf.PI / 2));
            Mk.Head(hs.ToArray());
            r.jaw = Mk.Piv(r.head, 0, -0.02f, 0.03f); Mk.Head(Mk.Msh(r.jaw, Geo.Box(0.2f, 0.06f, 0.16f), skin, 0, -0.03f, 0.07f));
            // arms: thick, scrap bracers; the right fist round a sledgehammer of pipe and a concrete block
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var sh = Mk.Piv(r.torso, 0.33f * sx, 0.55f, 0);
                Mk.Msh(sh, Geo.Sphere(0.11f, 10, 8), skin, 0, 0, 0);
                Mk.Msh(sh, GeoX.Limb(0.1f, 0.085f, 0.42f), skin);
                var elbow = Mk.Piv(sh, 0, -0.42f, 0);
                Mk.Msh(elbow, GeoX.Limb(0.088f, 0.075f, 0.4f), skin);
                Mk.Msh(elbow, Geo.Cyl(0.1f, 0.095f, 0.24f, 8), plate, 0, -0.26f, 0);
                foreach (var y in new[] { -0.17f, -0.35f }) Mk.Msh(elbow, Geo.Torus(0.098f, 0.012f, 10, 4), steel, 0, y, 0, Mathf.PI / 2, 0, 0);
                var hand = Mk.Piv(elbow, 0, -0.42f, 0);
                Mk.Msh(hand, Geo.Box(0.14f, 0.12f, 0.11f), skin, 0, -0.02f, 0);
                if (sx > 0)
                {
                    var hm2 = Mk.Piv(hand, 0, -0.04f, 0.02f); hm2.SetRot(1.4f, 0, 0);
                    Mk.Msh(hm2, Geo.Cyl(0.035f, 0.035f, 1.0f, 8), steel, 0, 0.3f, 0);
                    Mk.Msh(hm2, Geo.Box(0.28f, 0.22f, 0.22f), conc, 0, 0.82f, 0);
                    for (int k = 0; k < 4; k++) Mk.Msh(hm2, Geo.Cyl(0.01f, 0.01f, 0.18f, 4), steel, (k % 2 - 0.5f) * 0.2f, 0.82f + (k / 2 - 0.5f) * 0.12f, 0.12f, 0.5f, 0, (k - 1.5f) * 0.4f);
                    Mk.Msh(hm2, Geo.Torus(0.05f, 0.015f, 10, 4), tyre, 0, -0.12f, 0, Mathf.PI / 2, 0, 0);
                }
                r.sh[i] = sh; r.elbow[i] = elbow; r.hand[i] = hand;
            }
            e.mats.Clear(); e.mats.Add(skin); e.mats.Add(cloth); e.mats.Add(plate); e.mats.Add(steel);
            r.extra = (en, dt) =>
            {
                if (en.dead) return;
                if (rand < dt * 1.2f) { var hp = r.head.t.TransformPoint(V(0, 0.0f, 0.22f)); particles.Add(new Pt { pos = hp, scale = Vector3.one * 0.08f, mat = ashMat, v = r.head.t.forward * 0.6f + V(0, 0.6f, 0), life = 0.9f, grav = 0 }); } // breath steaming out of the helmet
                Mats.Emissive(hot, Mathf.Sin(time * 4) > 0.6f ? 0x401000 : 0);
            };
        }

        // ---------- SCRAP DRONE ----------
        void DressScrapDrone(Ent e)
        {
            var r = e.dr;
            var plate = EM(e, Mats.Std(0x6a5a4a, 0.55f, 0.55f, 0, RustTex())); var dark = EM(e, Mats.Std(0x222020, 0.6f, 0.5f));
            var blade = EM(e, Mats.Std(0x3a3836, 0.4f, 0.7f)); var lamp = EM(e, Mats.Basic(0xff3010));
            // the welded cage: four curved plates round the eye, bolted at the seams
            for (int k = 0; k < 4; k++) { float a = k * Mathf.PI / 2 + Mathf.PI / 4; var pl = Mk.Msh(r.root, Geo.Torus(0.56f, 0.07f, 10, 4, Mathf.PI * 0.32f), plate, 0, 0, -0.05f, 0, 0, a - Mathf.PI * 0.16f); pl.transform.localScale = V(1, 1, 2.2f); for (int b = 0; b < 2; b++) Mk.Msh(r.root, Geo.Sphere(0.03f, 6, 4), dark, Mathf.Cos(a + (b - 0.5f) * 0.9f) * 0.6f, Mathf.Sin(a + (b - 0.5f) * 0.9f) * 0.6f, 0.06f); }
            // two outriggers with rotors (they spin), an antenna with a blinking tip, a camera housing, cables hanging
            var rotors = new List<N>();
            foreach (var sx in new[] { -1f, 1f })
            {
                Mk.Msh(r.root, Geo.Box(0.6f, 0.06f, 0.08f), dark, sx * 0.8f, 0.15f, -0.1f);
                Mk.Msh(r.root, Geo.Cyl(0.08f, 0.1f, 0.16f, 10), plate, sx * 1.1f, 0.2f, -0.1f);
                var rot = Mk.Piv(r.root, sx * 1.1f, 0.3f, -0.1f);
                for (int b = 0; b < 3; b++) Mk.Msh(rot, Geo.Box(0.5f, 0.012f, 0.07f), blade, Mathf.Cos(b * 2.094f) * 0.25f, 0, Mathf.Sin(b * 2.094f) * 0.25f, 0, -b * 2.094f, 0.1f);
                Mk.Msh(rot, Geo.Torus(0.52f, 0.012f, 24, 4), Mats.Trans(0x8a8a8a, 0.18f), 0, 0, 0, Mathf.PI / 2, 0, 0); // the blur of the blades
                rotors.Add(rot);
            }
            Mk.Msh(r.root, Geo.Cyl(0.008f, 0.008f, 0.55f, 4), dark, 0.2f, 0.75f, -0.2f, -0.2f, 0, -0.2f);
            var tip = Mk.Msh(r.root, Geo.Sphere(0.03f, 6, 4), lamp, 0.25f, 1.02f, -0.25f);
            Mk.Msh(r.root, Geo.Box(0.16f, 0.12f, 0.22f), dark, -0.3f, -0.45f, 0.2f); Mk.Msh(r.root, Geo.Cyl(0.04f, 0.05f, 0.06f, 10), Mats.Std(0x101418, 0.1f, 0.4f), -0.3f, -0.45f, 0.33f, Mathf.PI / 2);
            for (int k = 0; k < 3; k++) Mk.Msh(r.root, Geo.Cyl(0.012f, 0.012f, 0.4f + k * 0.15f, 4), dark, (k - 1) * 0.14f, -0.6f - k * 0.07f, -0.25f, 0.15f * (k - 1), 0, 0.2f * (k - 1));
            r.extra = (en, dt) =>
            {
                foreach (var ro in rotors) ro.ry += dt * 38;
                tip.SetActive(Mathf.Sin(time * 5 + en.phase) > 0.7f);
            };
        }
    }
}
