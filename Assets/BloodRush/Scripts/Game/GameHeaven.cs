using System;
using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    // The heavenly host: a smooth sculpting kit (lathed shells, tapered limbs, curved feathers, bevelled blades)
    // and the Zealot, Templar and Ophan built from it. Ported 1:1 from the HTML game.
    public partial class Game
    {
        static float r3(float v) => Mathf.Round(v * 1000) / 1000;
        static Mesh LIMB(float r1, float r2, float len, int seg = 12) => GeoX.Limb(r1, r2, len, seg, 4);
        static Mesh LATHE(string key, float[] yr, int seg = 24, float p0 = 0, float pl = Mathf.PI * 2) => GeoX.Lathe(key, GeoX.YR(yr), seg, p0, pl);
        static Mesh DOME(float r, float theta, float p0 = 0, float pl = Mathf.PI * 2, int w = 24, int h = 10) => Geo.Sphere(r, w, h, p0, pl, 0, theta);
        static Mesh RING(float r, float t, int seg = 40, int rad = 8, float arc = Mathf.PI * 2) => Geo.Torus(r, t, seg, rad, arc);
        static Mesh CAPS(float r, float len) => GeoX.Capsule(r, len, 4, 10);
        static Mesh SP(float r, int w, int h) => Geo.Sphere(r, w, h);
        // a curved, pointed feather in the XY plane: quill at the origin, tip at +y, cupped slightly backwards
        static Mesh FEATHER(float len, float w, float curl = 0.12f) => GeoX.Cached($"fe{len},{w},{curl}", () =>
        {
            var s = new GeoX.Shape().MoveTo(0, 0).BezierTo(w * 0.95f, len * 0.12f, w * 0.8f, len * 0.72f, 0, len).BezierTo(-w * 0.4f, len * 0.72f, -w * 0.55f, len * 0.12f, 0, 0);
            var g = GeoX.ShapeMB(s, 10);
            for (int i = 0; i < g.v.Count; i++) { var p = g.v[i]; float y = p.y / len; p.z = -curl * len * y * y - 0.35f * p.x * p.x / w; g.v[i] = p; }
            g.ComputeNormals(); return g.Build();
        });
        // a thin bevelled solid from a 2D outline, centred on z
        static Mesh BLADE(string key, Func<GeoX.Shape> draw, float depth = 0.012f, float bevel = 0.008f) => GeoX.Cached("bl" + key, () =>
        {
            var g = GeoX.ExtrudeMB(draw(), depth, 12, true, bevel, bevel, 2);
            g.Translate(new Vector3(0, 0, -depth / 2)); return g.Build();
        });
        static GameObject MshS(N parent, Mesh geo, Material mat, float x, float y, float z, float rx, float ry, float rz, float sx, float sy, float sz)
        {
            var m = Mk.Msh(parent, geo, mat, x, y, z, rx, ry, rz); m.transform.localScale = new Vector3(sx, sy, sz);
            if (sx < 0 || sy < 0 || sz < 0) { var c = m.GetComponent<Collider>(); if (c) DestroyImmediate(c); var p = m.GetComponent<Part>(); if (p) DestroyImmediate(p); }
            return m;
        }
        Material HolyMat(Ent e, int color, float rough = 0.5f, float metal = 0, int emissive = 0, bool dbl = false) => EM(e, Mats.Std(color, rough, metal, emissive, null, dbl));
        static void MarkAllHead(N o) { foreach (var p in o.t.GetComponentsInChildren<Part>(true)) p.head = true; }
        // a fan of feathers along a wing's leading edge (s = side, mirrored for the left); optional glowing outline behind each
        static void FeatherFan(N wing, float s, Material mat, Material glowMat, int n, float len0, float dl, float ang0, float da, float width, float step, float z0 = 0)
        {
            for (int f = 0; f < n; f++)
            {
                float len = r3(len0 - f * dl), ang = ang0 + f * da, x = s * (0.04f + f * step), y = f * 0.015f, z = z0 - 0.012f * f;
                MshS(wing, FEATHER(len, r3(width)), mat, x, y, z, 0, 0, -s * ang, s, 1, 1);
                if (glowMat) MshS(wing, FEATHER(r3(len * 1.05f), r3(width * 1.3f)), glowMat, x, y - 0.012f, z - 0.006f, 0, 0, -s * ang, s, 1, 1);
            }
        }
        static readonly float[] ZRP = { 0.02f, 0.19f, -0.1f, 0.24f, -0.35f, 0.36f, -0.65f, 0.47f, -0.92f, 0.55f, -1.0f, 0.56f };
        static float[] ScaleR(float[] yr, float k, int from = 0, int to = -1)
        {
            if (to < 0) to = yr.Length / 2;
            var o = new float[(to - from) * 2];
            for (int i = from; i < to; i++) { o[(i - from) * 2] = yr[i * 2]; o[(i - from) * 2 + 1] = yr[i * 2 + 1] * k; }
            return o;
        }

        // ZEALOT: a hooded acolyte in flowing white and gold, porcelain mask with weeping light, halo and small wings,
        // wielding twin sickles of light
        HuskRig BuildZealot(Ent e)
        {
            var robe = HolyMat(e, 0xf4efe4, 0.75f, 0, 0, true);
            var gold = HolyMat(e, 0xd8a828, 0.28f, 0.85f);
            var porc = HolyMat(e, 0xfbf8f2, 0.16f, 0.05f);
            var blue = HolyMat(e, 0x26306a, 0.8f, 0, 0, true);
            var featherM = HolyMat(e, 0xffffff, 0.45f, 0, 0, true);
            var glow = EM(e, Mats.Basic(0x80e8ff));
            var edge = EM(e, Mats.Std(0xeafcff, 0.2f, 0.3f)); Mats.Emissive(edge, 0x60d8ff, 1.4f);
            var r = new HuskRig { glow = glow };
            r.root = Mk.Piv(e.g.transform);
            r.hips = Mk.Piv(r.root, 0, 1.0f, 0);
            MshS(r.hips, RING(0.2f, 0.026f, 32, 8), gold, 0, 0.03f, 0, Mathf.PI / 2, 0, 0, 1, 0.8f, 1);
            // robe: flares from the belt, a blue stole with gold edges runs down the front; it sways as the zealot moves
            var robeP = Mk.Piv(r.hips, 0, 0.03f, 0);
            MshS(robeP, LATHE("zrobe", ZRP, 32), robe, 0, 0, 0, 0, 0, 0, 1, 1, 0.94f);
            MshS(robeP, LATHE("zstole", ScaleR(ZRP, 1.012f), 6, -0.19f, 0.38f), blue, 0, 0, 0, 0, 0, 0, 1, 1, 0.94f);
            foreach (var p0 in new[] { -0.23f, 0.19f }) MshS(robeP, LATHE("zstoleE" + p0, ScaleR(ZRP, 1.016f), 2, p0, 0.04f), gold, 0, 0, 0, 0, 0, 0, 1, 1, 0.94f);
            MshS(robeP, RING(0.56f, 0.02f, 48, 6), gold, 0, -0.985f, 0, Mathf.PI / 2, 0, 0, 1, 0.94f, 1);
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var thigh = Mk.Piv(r.hips, 0.11f * sx, -0.06f, 0);
                Mk.Msh(thigh, LIMB(0.078f, 0.062f, 0.48f), blue);
                var knee = Mk.Piv(thigh, 0, -0.48f, 0);
                Mk.Msh(knee, LIMB(0.062f, 0.046f, 0.44f), blue);
                MshS(knee, SP(0.07f, 16, 12), gold, 0, -0.43f, 0.05f, 0, 0, 0, 0.85f, 0.55f, 1.7f);
                r.thigh[i] = thigh; r.knee[i] = knee;
            }
            r.torso = Mk.Piv(r.hips, 0, 0.06f, 0);
            float[] TP = { -0.12f, 0.0f, -0.1f, 0.17f, 0.1f, 0.18f, 0.28f, 0.2f, 0.42f, 0.235f, 0.52f, 0.24f, 0.59f, 0.19f, 0.63f, 0.09f, 0.64f, 0.0f };
            MshS(r.torso, LATHE("ztorso", TP, 24), robe, 0, 0, 0, 0, 0, 0, 1, 1, 0.74f);
            MshS(r.torso, LATHE("ztstole", ScaleR(TP, 1.025f, 2, 7), 6, -0.17f, 0.34f), blue, 0, 0, 0, 0, 0, 0, 1, 1, 0.74f);
            Mk.Msh(r.torso, CAPS(0.013f, 0.22f), gold, 0, 0.34f, 0.19f); Mk.Msh(r.torso, CAPS(0.013f, 0.11f), gold, 0, 0.4f, 0.192f, 0, 0, Mathf.PI / 2); // golden cross
            Mk.Msh(r.torso, SP(0.032f, 12, 10), glow, 0, 0.4f, 0.2f);
            MshS(r.torso, LATHE("zmantle", new[] { 0.66f, 0.07f, 0.6f, 0.2f, 0.5f, 0.31f, 0.38f, 0.37f, 0.34f, 0.375f }, 28), robe, 0, 0, 0, 0, 0, 0, 1, 1, 0.8f);
            MshS(r.torso, RING(0.375f, 0.018f, 40, 6), gold, 0, 0.345f, 0, Mathf.PI / 2, 0, 0, 1, 0.8f, 1);
            // small wings folded behind the mantle
            var wings = new List<(N w, float s)>();
            foreach (var s in new[] { -1f, 1f })
            {
                var w = Mk.Piv(r.torso, 0.1f * s, 0.5f, -0.2f); w.ry = s * 0.5f;
                FeatherFan(w, s, featherM, null, 6, 0.85f, 0.08f, 0.22f, 0.22f, 0.12f, 0.045f);
                wings.Add((w, s));
            }
            r.neck = Mk.Piv(r.torso, 0, 0.62f, 0.02f);
            Mk.Msh(r.neck, LIMB(0.05f, 0.058f, 0.12f), porc, 0, 0.12f, 0);
            r.head = Mk.Piv(r.neck, 0, 0.12f, 0.02f);
            MshS(r.head, SP(0.15f, 24, 18), porc, 0, 0.09f, 0.03f, 0, 0, 0, 0.86f, 1.12f, 0.92f);                           // porcelain mask
            float hTheta = Mathf.PI * 0.66f, hP0 = Mathf.PI / 2 + 0.75f, hPl = Mathf.PI * 2 - 1.5f;
            MshS(r.head, DOME(0.23f, hTheta, hP0, hPl, 24, 12), robe, 0, 0.06f, -0.02f, 0, 0, 0, 1, 1.18f, 1.08f);          // deep cowl
            MshS(r.head, DOME(0.222f, hTheta, hP0, hPl, 24, 12), blue, 0, 0.06f, -0.02f, 0, 0, 0, 1, 1.18f, 1.08f);         // its shadowed lining
            Mk.Msh(r.head, LATHE("zpeak", new[] { 0, 0.11f, 0.08f, 0.075f, 0.16f, 0.03f, 0.2f, 0.0f }, 16), robe, 0, 0.27f, -0.1f, -0.55f, 0, 0); // cowl peak
            MshS(r.head, RING(0.205f, 0.012f, 40, 6, Mathf.PI * 2 - 1.5f), gold, 0, 0.06f, -0.02f, 0, -Mathf.PI / 2 + 0.75f, Mathf.PI / 2, 1.08f, 1.18f, 1); // gold hood rim
            foreach (var sx in new[] { -1f, 1f })
            {
                Mk.Msh(r.head, CAPS(0.011f, 0.05f), glow, 0.05f * sx, 0.115f, 0.16f, 0, 0, Mathf.PI / 2 + 0.28f * sx);          // slanted eyes
                Mk.Msh(r.head, CAPS(0.006f, 0.06f), glow, 0.052f * sx, 0.05f, 0.155f, -0.2f, 0, 0);                             // tears of light
            }
            Mk.Msh(r.head, CAPS(0.007f, 0.1f), gold, 0, 0.19f, 0.145f, -0.45f, 0, 0);
            var halo = Mk.Piv(r.head, 0, 0.42f, -0.1f); halo.rx = -0.35f;
            Mk.Msh(halo, RING(0.17f, 0.012f, 40, 6), gold, 0, 0, 0, Mathf.PI / 2);
            Mk.Msh(halo, RING(0.2f, 0.006f, 40, 4), glow, 0, 0, 0, Mathf.PI / 2);
            r.jaw = Mk.Piv(r.head, 0, 0.0f, 0.03f);
            MshS(r.jaw, SP(0.08f, 16, 10), porc, 0, -0.06f, 0.05f, 0, 0, 0, 1, 0.6f, 0.85f);
            MarkAllHead(r.head);
            var SICKLE = BLADE("zsickle", () => new GeoX.Shape().MoveTo(-0.02f, 0).LineTo(0.03f, 0).QuadTo(0.22f, -0.32f, 0.1f, -0.8f).QuadTo(0.07f, -0.4f, -0.02f, 0), 0.01f, 0.007f);
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var sh = Mk.Piv(r.torso, 0.33f * sx, 0.55f, 0);
                Mk.Msh(sh, SP(0.075f, 14, 10), robe);
                Mk.Msh(sh, LATHE("zsleeve1", new[] { 0.04f, 0.075f, -0.2f, 0.085f, -0.42f, 0.1f }, 16), robe);
                var elbow = Mk.Piv(sh, 0, -0.42f, 0);
                Mk.Msh(elbow, LATHE("zsleeve2", new[] { 0.03f, 0.085f, -0.15f, 0.09f, -0.27f, 0.11f, -0.29f, 0.112f }, 18), robe);
                Mk.Msh(elbow, RING(0.11f, 0.011f, 32, 6), gold, 0, -0.285f, 0, Mathf.PI / 2);
                Mk.Msh(elbow, LIMB(0.042f, 0.034f, 0.4f), porc);
                var hand = Mk.Piv(elbow, 0, -0.42f, 0);
                MshS(hand, SP(0.045f, 14, 10), porc, 0, -0.03f, 0, 0, 0, 0, 0.75f, 1.1f, 0.55f);
                Mk.Msh(hand, LIMB(0.016f, 0.016f, 0.1f), gold, 0, 0.0f, 0.02f);
                Mk.Msh(hand, RING(0.04f, 0.01f, 20, 6), gold, 0, -0.07f, 0.02f, Mathf.PI / 2);
                Mk.Msh(hand, SICKLE, edge, 0, -0.07f, 0.02f, 0, -Mathf.PI / 2, 0);
                r.sh[i] = sh; r.elbow[i] = elbow; r.hand[i] = hand;
            }
            e.mats.Clear(); e.mats.AddRange(new[] { robe, gold, porc, blue, featherM });
            r.extra = (en, dt) =>
            {
                float hsp = Hypot(en.vel.x, en.vel.z), k = Mathf.Min(1, dt * 8);
                robeP.SetRot(robeP.rx + (-Mathf.Min(hsp, 8) * 0.025f + Mathf.Sin(time * 2.2f + en.walk) * 0.02f - robeP.rx) * k, robeP.ry, Mathf.Sin(en.walk) * 0.04f * Mathf.Min(1, hsp / 4));
                halo.ry += dt * 1.6f;
                foreach (var (w, s) in wings) w.rz = s * (0.15f + Mathf.Sin(time * 3 + en.walk) * 0.08f + (en.attackT > 0 ? 0.3f : 0));
                Mats.Emissive(edge, 0x60d8ff, en.attackT > 0 ? 3.2f : 1.3f + Mathf.Sin(time * 4) * 0.3f);
            };
            return r;
        }

        // TEMPLAR: a towering paladin in polished plate and gold, winged great helm with a burning cross visor,
        // royal-blue cape, tower shield and a flanged mace with a core of light
        HuskRig BuildTemplar(Ent e)
        {
            var plate = HolyMat(e, 0xe6e8f0, 0.26f, 0.75f);
            var gold = HolyMat(e, 0xd8a828, 0.26f, 0.85f, 0, true);
            var cloth = HolyMat(e, 0xf3eee2, 0.8f, 0, 0, true);
            var mail = HolyMat(e, 0x3a3f52, 0.45f, 0.6f);
            var cape = HolyMat(e, 0x24307a, 0.75f, 0, 0, true);
            var glow = EM(e, Mats.Basic(0x80e8ff));
            var r = new HuskRig { glow = glow };
            r.root = Mk.Piv(e.g.transform);
            r.hips = Mk.Piv(r.root, 0, 1.0f, 0);
            MshS(r.hips, LATHE("tfauld", new[] { 0.08f, 0.2f, 0.0f, 0.235f, -0.16f, 0.27f, -0.3f, 0.3f }, 28), plate, 0, 0, 0, 0, 0, 0, 1, 1, 0.8f);
            MshS(r.hips, RING(0.24f, 0.03f, 32, 8), gold, 0, 0.03f, 0, Mathf.PI / 2, 0, 0, 1, 0.82f, 1);
            MshS(r.hips, RING(0.3f, 0.016f, 32, 6), gold, 0, -0.29f, 0, Mathf.PI / 2, 0, 0, 1, 0.8f, 1);
            // tabard panels front and back: they swing with the legs so knees never punch through
            var TAB = LATHE("ttab", new[] { 0.0f, 0.25f, -0.3f, 0.315f, -0.68f, 0.35f }, 8, -0.36f, 0.72f);
            var tabF = Mk.Piv(r.hips, 0, -0.02f, 0); var tabB = Mk.Piv(r.hips, 0, -0.02f, 0);
            MshS(tabF, TAB, cloth, 0, 0, 0, 0, 0, 0, 1, 1, 0.84f);
            MshS(tabB, TAB, cloth, 0, 0, 0, 0, Mathf.PI, 0, 1, 1, 0.84f);
            Mk.Msh(tabF, CAPS(0.013f, 0.3f), gold, 0, -0.38f, 0.27f); Mk.Msh(tabF, CAPS(0.013f, 0.13f), gold, 0, -0.3f, 0.262f, 0, 0, Mathf.PI / 2);
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var thigh = Mk.Piv(r.hips, 0.13f * sx, -0.06f, 0);
                Mk.Msh(thigh, LIMB(0.1f, 0.085f, 0.48f), mail);
                Mk.Msh(thigh, LIMB(0.112f, 0.095f, 0.3f), plate, 0, -0.08f, 0);
                var knee = Mk.Piv(thigh, 0, -0.48f, 0);
                Mk.Msh(knee, SP(0.085f, 16, 12), plate, 0, 0, 0.02f);
                MshS(knee, DOME(0.07f, Mathf.PI / 2), gold, 0, 0, 0.07f, Mathf.PI / 2, 0, 0, 1, 1, 0.5f);
                Mk.Msh(knee, LIMB(0.092f, 0.072f, 0.42f), plate, 0, -0.04f, 0);
                MshS(knee, SP(0.1f, 16, 12), plate, 0, -0.45f, 0.07f, 0, 0, 0, 0.95f, 0.6f, 1.7f);
                r.thigh[i] = thigh; r.knee[i] = knee;
            }
            r.torso = Mk.Piv(r.hips, 0, 0.06f, 0);
            float[] TP = { -0.08f, 0.2f, 0.05f, 0.235f, 0.2f, 0.27f, 0.36f, 0.31f, 0.48f, 0.32f, 0.56f, 0.29f, 0.62f, 0.2f, 0.66f, 0.1f, 0.67f, 0.0f };
            MshS(r.torso, LATHE("tchest", TP, 32), plate, 0, 0, 0, 0, 0, 0, 1, 1, 0.78f);
            MshS(r.torso, LATHE("tridge", ScaleR(TP, 1.012f, 0, 7), 3, -0.05f, 0.1f), gold, 0, 0, 0, 0, 0, 0, 1, 1, 0.78f);
            Mk.Msh(r.torso, CAPS(0.02f, 0.24f), gold, 0, 0.33f, 0.255f); Mk.Msh(r.torso, CAPS(0.02f, 0.15f), gold, 0, 0.38f, 0.258f, 0, 0, Mathf.PI / 2);
            var core = Mk.Msh(r.torso, SP(0.05f, 14, 10), glow, 0, 0.38f, 0.27f);
            MshS(r.torso, LATHE("tgorget", new[] { 0.55f, 0.22f, 0.62f, 0.2f, 0.7f, 0.13f, 0.72f, 0.11f }, 24), plate, 0, 0, 0, 0, 0, 0, 1, 1, 0.85f);
            MshS(r.torso, RING(0.21f, 0.015f, 32, 6), gold, 0, 0.57f, 0, Mathf.PI / 2, 0, 0, 1, 0.84f, 1);
            var capeP = Mk.Piv(r.torso, 0, 0.6f, -0.04f);
            MshS(capeP, LATHE("tcape", new[] { 0.02f, 0.26f, -0.25f, 0.34f, -0.7f, 0.42f, -1.15f, 0.48f }, 18, Mathf.PI - 1.0f, 2.0f), cape, 0, 0, 0, 0, 0, 0, 1, 1, 0.75f);
            MshS(capeP, RING(0.48f, 0.016f, 24, 5, 2.0f), gold, 0, -1.15f, 0, Mathf.PI / 2, Mathf.PI / 2 + 1.0f, 0, 1, 0.75f, 1);
            r.neck = Mk.Piv(r.torso, 0, 0.62f, 0.02f);
            Mk.Msh(r.neck, LIMB(0.08f, 0.09f, 0.12f), mail, 0, 0.12f, 0);
            r.head = Mk.Piv(r.neck, 0, 0.12f, 0.02f);
            MshS(r.head, LATHE("thelm", new[] { -0.15f, 0.0f, -0.15f, 0.14f, -0.1f, 0.175f, 0.05f, 0.19f, 0.17f, 0.185f, 0.25f, 0.15f, 0.3f, 0.08f, 0.32f, 0.0f }, 32), plate, 0, 0.05f, 0, 0, 0, 0, 1, 1, 1.05f);
            MshS(r.head, RING(0.19f, 0.018f, 40, 6), gold, 0, 0.13f, 0, Mathf.PI / 2, 0, 0, 1, 1.05f, 1);
            Mk.Msh(r.head, CAPS(0.015f, 0.2f), glow, 0, 0.1f, 0.2f, 0, 0, Mathf.PI / 2);   // burning cross visor
            Mk.Msh(r.head, CAPS(0.013f, 0.12f), glow, 0, 0.02f, 0.188f);
            Mk.Msh(r.head, BLADE("tcrest", () => new GeoX.Shape().MoveTo(-0.22f, 0).QuadTo(-0.16f, 0.22f, 0.05f, 0.3f).QuadTo(0.2f, 0.25f, 0.2f, 0.0f).LineTo(-0.22f, 0), 0.02f, 0.01f), gold, 0, 0.33f, 0, 0, -Mathf.PI / 2, 0);
            foreach (var s in new[] { -1f, 1f }) { var wp = Mk.Piv(r.head, 0.18f * s, 0.2f, -0.02f); wp.ry = s * 0.7f; FeatherFan(wp, s, gold, null, 3, 0.34f, 0.06f, 0.3f, 0.32f, 0.07f, 0.02f); }
            r.jaw = Mk.Piv(r.head, 0, 0.0f, 0.03f);
            MshS(r.jaw, SP(0.12f, 16, 10), plate, 0, -0.07f, 0.03f, 0, 0, 0, 1.2f, 0.6f, 1.1f);
            MarkAllHead(r.head);
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var sh = Mk.Piv(r.torso, 0.36f * sx, 0.55f, 0);
                var pd = Mk.Piv(sh, 0.03f * sx, 0.07f, 0); pd.rz = -sx * 0.25f;     // layered pauldron
                for (int k = 0; k < 3; k++) MshS(pd, DOME(r3(0.21f - k * 0.015f), Mathf.PI / 2, 0, Mathf.PI * 2, 24, 8), plate, 0, -k * 0.07f, 0, 0, 0, 0, 1.05f, 0.75f, 1);
                MshS(pd, RING(0.21f, 0.014f, 36, 6), gold, 0, 0, 0, Mathf.PI / 2, 0, 0, 1.05f, 1, 1);
                MshS(pd, RING(0.18f, 0.012f, 36, 6), gold, 0, -0.14f, 0, Mathf.PI / 2, 0, 0, 1.05f, 1, 1);
                Mk.Msh(sh, LIMB(0.092f, 0.08f, 0.42f), mail);
                Mk.Msh(sh, LIMB(0.1f, 0.088f, 0.24f), plate, 0, -0.16f, 0);
                var elbow = Mk.Piv(sh, 0, -0.42f, 0);
                Mk.Msh(elbow, SP(0.09f, 14, 10), plate);
                MshS(elbow, DOME(0.06f, Mathf.PI / 2), gold, 0, 0, -0.07f, -Mathf.PI / 2, 0, 0, 1, 1, 0.6f);
                Mk.Msh(elbow, LIMB(0.088f, 0.075f, 0.4f), plate);
                Mk.Msh(elbow, RING(0.08f, 0.012f, 24, 6), gold, 0, -0.33f, 0, Mathf.PI / 2);
                var hand = Mk.Piv(elbow, 0, -0.42f, 0);
                MshS(hand, SP(0.09f, 14, 10), plate, 0, -0.04f, 0.01f, 0, 0, 0, 0.95f, 1.05f, 1);
                r.sh[i] = sh; r.elbow[i] = elbow; r.hand[i] = hand;
            }
            // flanged mace with a ring of light
            var mace = r.hand[1];
            Mk.Msh(mace, LIMB(0.03f, 0.03f, 0.62f), mail, 0, 0.2f, 0);
            foreach (var y in new[] { 0.12f, -0.3f }) Mk.Msh(mace, RING(0.036f, 0.012f, 20, 6), gold, 0, y, 0, Mathf.PI / 2);
            var mh = Mk.Piv(mace, 0, -0.55f, 0);
            Mk.Msh(mh, SP(0.14f, 20, 14), gold);
            var FL = BLADE("tflange", () => new GeoX.Shape().MoveTo(0, -0.17f).LineTo(0.05f, -0.17f).QuadTo(0.21f, -0.05f, 0.19f, 0.06f).QuadTo(0.12f, 0.17f, 0, 0.17f).LineTo(0, -0.17f), 0.026f, 0.01f);
            for (int k = 0; k < 8; k++) Mk.Msh(mh, FL, gold, 0, 0, 0, 0, k * Mathf.PI / 4, 0);
            Mk.Msh(mh, RING(0.16f, 0.02f, 36, 6), glow, 0, 0, 0, Mathf.PI / 2);
            Mk.Msh(mh, LATHE("tspike", new[] { 0, 0.05f, 0.12f, 0.02f, 0.2f, 0.0f }, 12), gold, 0, -0.14f, 0, Mathf.PI);
            // tower shield on the left forearm
            var sp = Mk.Piv(r.elbow[0], -0.13f, -0.24f, 0.04f); sp.ry = -Mathf.PI / 2 + 0.5f;
            var SH = BLADE("tshield", () => new GeoX.Shape().MoveTo(-0.3f, 0.3f).QuadTo(0, 0.4f, 0.3f, 0.3f).LineTo(0.3f, 0.05f).QuadTo(0.28f, -0.32f, 0, -0.52f).QuadTo(-0.28f, -0.32f, -0.3f, 0.05f).LineTo(-0.3f, 0.3f), 0.05f, 0.02f);
            Mk.Msh(sp, SH, plate);
            MshS(sp, SH, gold, 0, -0.01f, -0.016f, 0, 0, 0, 1.09f, 1.07f, 1);
            Mk.Msh(sp, CAPS(0.02f, 0.5f), gold, 0, -0.07f, 0.048f); Mk.Msh(sp, CAPS(0.02f, 0.36f), gold, 0, 0.12f, 0.048f, 0, 0, Mathf.PI / 2);
            Mk.Msh(sp, RING(0.085f, 0.014f, 32, 6), glow, 0, 0.12f, 0.05f);
            Mk.Msh(sp, SP(0.04f, 12, 10), glow, 0, 0.12f, 0.05f);
            e.mats.Clear(); e.mats.AddRange(new[] { plate, gold, cloth, mail, cape });
            r.extra = (en, dt) =>
            {
                float hsp = Hypot(en.vel.x, en.vel.z), k = Mathf.Min(1, dt * 10), a = r.thigh[0].rx, b = r.thigh[1].rx;
                tabF.rx += (Mathf.Min(Mathf.Min(a, b), 0) * 0.9f - tabF.rx) * k;
                tabB.rx += (Mathf.Max(Mathf.Max(a, b), 0) * 0.9f - tabB.rx) * k;
                capeP.rx += (Mathf.Min(hsp, 6) * 0.05f + Mathf.Sin(time * 1.7f + en.walk) * 0.03f - capeP.rx) * Mathf.Min(1, dt * 5);
                core.transform.localScale = Vector3.one * (1 + Mathf.Sin(time * 5) * 0.15f + (en.attackT > 0 ? 0.6f : 0));
            };
            return r;
        }

        // OPHAN: a "wheel within a wheel", golden rings studded with watching eyes around one great eye,
        // porcelain lids with gold filigree, four feathered wings and a halo
        DroneRig BuildOphan(Ent e)
        {
            var porc = HolyMat(e, 0xf4f0e8, 0.22f, 0.1f);
            var gold = HolyMat(e, 0xd8a828, 0.25f, 0.85f, 0x2a1800);
            var featherM = HolyMat(e, 0xffffff, 0.45f, 0, 0, true);
            var sclera = EM(e, Mats.Std(0xf2ece0, 0.3f, 0, 0x302820));
            var irisMat = EM(e, Mats.Basic(0x60d8ff));
            var glow = EM(e, Mats.Basic(0xc8f4ff, null, true));
            var small = EM(e, Mats.Basic(0x60d8ff));
            var black = EM(e, Mats.Basic(0x000000));
            var r = new DroneRig { holy = true, irisMat = irisMat };
            r.root = Mk.Piv(e.g.transform);
            var eyeball = Mk.Msh(r.root, SP(0.42f, 28, 20), sclera);
            r.iris = Mk.Msh(r.root, SP(0.2f, 24, 16), irisMat, 0, 0, 0.34f); r.iris.transform.localScale = V(1, 1, 0.4f);
            var pupil = Mk.Msh(r.root, SP(0.09f, 16, 12), black, 0, 0, 0.42f); pupil.transform.localScale = V(1, 1, 0.4f);
            Mk.Head(eyeball, r.iris, pupil);
            r.lidTop = Mk.Piv(r.root); r.lidBot = Mk.Piv(r.root);
            foreach (var (lid, sg) in new[] { (r.lidTop, 1f), (r.lidBot, -1f) })
            {
                Mk.Msh(lid, DOME(0.5f, Mathf.PI / 2, 0, Mathf.PI * 2, 32, 12), porc, 0, 0, 0, sg > 0 ? 0 : Mathf.PI);
                Mk.Msh(lid, RING(0.5f, 0.03f, 56, 8), gold, 0, 0, 0, Mathf.PI / 2);
                Mk.Msh(lid, RING(0.355f, 0.014f, 44, 6), gold, 0, 0.355f * sg, 0, Mathf.PI / 2);
                Mk.Msh(lid, SP(0.06f, 14, 10), gold, 0, 0.5f * sg, 0);
            }
            // the wheels, covered in eyes that look outwards
            r.ring1 = Mk.Piv(r.root); r.ring2 = Mk.Piv(r.root);
            Mk.Msh(r.ring1, RING(0.86f, 0.045f, 72, 10), gold, 0, 0, 0, Mathf.PI / 2);
            Mk.Msh(r.ring1, RING(0.77f, 0.014f, 72, 6), gold, 0, 0, 0, Mathf.PI / 2);
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2, x = Mathf.Cos(a), z = Mathf.Sin(a);
                Mk.Msh(r.ring1, SP(0.075f, 14, 10), sclera, x * 0.86f, 0, z * 0.86f);
                MshS(r.ring1, SP(0.042f, 12, 8), small, x * 0.925f, 0, z * 0.925f, 0, -a, 0, 0.5f, 1, 1);
            }
            Mk.Msh(r.ring2, RING(1.04f, 0.04f, 80, 10), gold);
            Mk.Msh(r.ring2, RING(0.96f, 0.013f, 80, 6), gold);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2 + 0.26f, x = Mathf.Cos(a), y = Mathf.Sin(a);
                Mk.Msh(r.ring2, SP(0.07f, 14, 10), sclera, x * 1.04f, y * 1.04f, 0);
                MshS(r.ring2, SP(0.04f, 12, 8), small, x * 1.1f, y * 1.1f, 0, 0, 0, a, 0.5f, 1, 1);
            }
            // four wings: an upper pair sweeping up and a lower pair sweeping down, edged with light
            foreach (var s in new[] { -1f, 1f }) foreach (var lower in new[] { false, true })
                {
                    var w = Mk.Piv(r.root, 0.34f * s, lower ? -0.12f : 0.14f, -0.28f);
                    var fan = Mk.Piv(w); fan.rz = lower ? -s * 1.55f : 0;
                    FeatherFan(fan, s, featherM, glow, 6, lower ? 0.66f : 0.92f, 0.08f, 0.22f, 0.17f, 0.11f, 0.035f);
                    r.wings.Add(w); r.wingS.Add(s);
                }
            Mk.Msh(r.root, RING(0.34f, 0.022f, 44, 6), gold, 0, 0.92f, 0, Mathf.PI / 2);
            Mk.Msh(r.root, RING(0.4f, 0.008f, 44, 4), glow, 0, 0.92f, 0, Mathf.PI / 2);
            Mk.Msh(r.root, LATHE("ospire", new[] { 0, 0.15f, -0.1f, 0.12f, -0.32f, 0.05f, -0.55f, 0.0f }, 20), gold, 0, -0.46f, -0.04f);
            r.jet = Gl(r.root, 0xc8f0ff, 0.7f, 0.8f); r.jet.transform.localPosition = V(0, -1.05f, -0.04f);
            r.eyeGlow = Gl(r.root, 0x60d8ff, 0.8f, 0.6f); r.eyeGlow.transform.localPosition = V(0, 0, 0.55f);
            r.eyeGlowMat = r.eyeGlow.GetComponent<Renderer>().sharedMaterial;
            e.mats.Clear(); e.mats.AddRange(new[] { porc, gold, featherM });
            return r;
        }
    }
}
