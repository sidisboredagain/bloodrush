using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    // Purgatory's own people (levels 16-20): the same husk skeleton as every other husk (AnimateHusk, hitboxes, executions,
    // gore and hazards work unchanged), the same kinds, stats and behaviour (GameKinds) - new bodies:
    //   THE HOLLOW      (kind "hollow")   a penitent in a long hooded robe gone grey with waiting, a cracked porcelain mask with
    //                                     empty eyes and a gold tear, bandaged shins, broken manacles, a rusted sickle
    //   MIRROR TWIN     (kind "twin")     your dark copy in black mirror-glass: long coat, spiked pauldrons studded with shards,
    //                                     a cracked T-visor helm, fractures of white light across it, glass shards hanging round
    //                                     its head that glitch and jitter; it holds a copy of your gun
    //   HOLLOW PILGRIM  (kind "pilgrim")  a hulking pilgrim in three tiers of heavy robe, a veil of chains over the face, a shrine
    //                                     carried on its back, prayer beads, a censer on a chain - and the holy lantern on a crook
    // plus the Warden of Hours' rework (both phases).
    public partial class Game
    {
        HuskRig BuildLimboHusk(Ent e, bool brute, string kind)
        {
            var r = new HuskRig { ruin = true }; e.kind = kind; e.s = brute ? 1.65f : 1; // (ruin: dressed already - DressHusk leaves it be)
            r.root = Mk.Piv(e.g.transform);
            r.hips = Mk.Piv(r.root, 0, 1.0f, 0);
            switch (kind)
            {
                case "twin": BuildMirrorTwin(e, r); break;
                case "pilgrim": BuildPilgrim(e, r); break;
                default: BuildHollow(e, r); break;
            }
            return r;
        }
        void LimboLegs(HuskRig r, float tr, float sr, Material thighM, Material shinM, Material footM, Material wrapM)
        {
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var thigh = Mk.Piv(r.hips, 0.13f * sx, -0.06f, 0);
                Mk.Msh(thigh, GeoX.Limb(tr, tr * 0.8f, 0.48f), thighM);
                var knee = Mk.Piv(thigh, 0, -0.48f, 0);
                Mk.Msh(knee, Geo.Sphere(sr * 1.1f, 8, 6), shinM, 0, 0, 0.02f);
                Mk.Msh(knee, GeoX.Limb(sr, sr * 0.75f, 0.42f), shinM);
                if (wrapM) for (int k = 0; k < 4; k++) Mk.Msh(knee, Geo.Torus(sr * 0.95f, 0.012f, 10, 4), wrapM, 0, -0.12f - k * 0.075f, 0, Mathf.PI / 2 + 0.2f, 0, k * 0.3f);
                Mk.Msh(knee, Geo.Box(0.09f, 0.06f, 0.22f), footM, 0, -0.45f, 0.06f);
                for (int k = 0; k < 3; k++) Mk.Msh(knee, Geo.Cyl(0.012f, 0.01f, 0.05f, 4), footM, (k - 1) * 0.025f, -0.46f, 0.18f, Mathf.PI / 2, 0, 0);
                r.thigh[i] = thigh; r.knee[i] = knee;
            }
        }

        // ---------- THE HOLLOW ----------
        void BuildHollow(Ent e, HuskRig r)
        {
            var robe = EM(e, Mats.Std(0x6e6a62, 0.95f, 0, 0x020202, ClothTex2(), true)); var robeD = EM(e, Mats.Std(0x48443e, 0.95f, 0, 0, ClothTex2(), true));
            var skin = EM(e, Mats.Std(0xa8a49a, 0.75f, 0, 0x030303, SkinTex2())); var mask = EM(e, Mats.Std(0xe8e2d4, 0.28f, 0.05f));
            var rope = Mats.Std(0x8a7650, 0.95f); var iron = Mats.Std(0x4a4440, 0.5f, 0.7f, 0, RustTex()); var wrap = Mats.Std(0xb8ae98, 0.95f, 0, 0, ClothTex2());
            var glow = EM(e, Mats.Basic(0xffe0a0)); var voidM = Mats.Std(0x050505, 1f); var goldM = Mats.Std(0xd8b860, 0.3f, 0.85f); r.glow = glow;
            LimboLegs(r, 0.07f, 0.055f, skin, skin, skin, wrap);
            // the robe: long, falling from the waist nearly to the floor, its hem torn into strips
            Mk.Msh(r.hips, Geo.Box(0.36f, 0.16f, 0.24f), robeD);
            Mk.Msh(r.hips, Geo.OpenCone(0.4f, 0.98f, 14), robe, 0, -0.36f, 0);
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; Mk.Msh(r.hips, Geo.Box(0.13f, 0.22f + (k % 3) * 0.07f, 0.015f), robe, Mathf.Sin(a) * 0.39f, -0.93f - (k % 3) * 0.035f, Mathf.Cos(a) * 0.39f, -0.32f * Mathf.Cos(a), a, 0.32f * Mathf.Sin(a)); }
            Mk.Msh(r.hips, Geo.Torus(0.2f, 0.022f, 18, 5), rope, 0, 0.05f, 0, Mathf.PI / 2, 0, 0).transform.localScale = V(1, 0.75f, 1);
            for (int k = 0; k < 2; k++) { Mk.Msh(r.hips, Geo.Cyl(0.012f, 0.012f, 0.42f, 5), rope, -0.08f + k * 0.06f, -0.16f, 0.15f, 0.05f, 0, (k - 0.5f) * 0.2f); Mk.Msh(r.hips, Geo.Sphere(0.022f, 6, 5), rope, -0.08f + k * 0.06f, -0.37f, 0.16f); }
            // torso: the robe over a starved chest, a tear on one side showing the ribs, a chain worn as penance
            r.torso = Mk.Piv(r.hips, 0, 0.06f, 0);
            Mk.Msh(r.torso, GeoX.Capsule(0.15f, 0.22f, 3, 12), robe, 0, 0.34f, 0).transform.localScale = V(1.2f, 1, 0.85f);
            for (int k = 0; k < 4; k++) Mk.Msh(r.torso, Geo.Box(0.12f, 0.018f, 0.02f), skin, 0.1f, 0.24f + k * 0.06f, 0.125f, 0, 0, -0.2f);
            Mk.Msh(r.torso, Geo.Box(0.16f, 0.24f, 0.02f), Mats.Std(0x1a1816, 1f), 0.1f, 0.33f, 0.118f);
            for (int k = 0; k < 9; k++) { float t = k / 8f; Mk.Msh(r.torso, Geo.Torus(0.025f, 0.007f, 8, 4), iron, Mathf.Lerp(-0.18f, 0.16f, t), Mathf.Lerp(0.56f, 0.12f, t), 0.135f, 0, k % 2 == 0 ? 0 : Mathf.PI / 2, 0); }
            Mk.Msh(r.torso, Geo.Cyl(0.2f, 0.28f, 0.14f, 14, true), robeD, 0, 0.56f, -0.01f); // the mantle over the shoulders
            // head: the deep hood, and inside it the cracked porcelain mask with empty eyes - two points of light far back
            r.neck = Mk.Piv(r.torso, 0, 0.62f, 0.02f); r.neck.SetRot(0.2f, 0, 0);
            Mk.Msh(r.neck, Geo.Cyl(0.04f, 0.05f, 0.14f, 8), skin, 0, 0.05f, 0);
            r.head = Mk.Piv(r.neck, 0, 0.12f, 0.02f);
            var hs = new List<GameObject>();
            var hood = Mk.Msh(r.head, Geo.Sphere(0.22f, 16, 12, Mathf.PI / 2 + 0.75f, Mathf.PI * 2 - 1.5f, 0, Mathf.PI * 0.72f), robe, 0, 0.08f, -0.02f); hood.transform.localScale = V(1, 1.15f, 1.1f); hs.Add(hood);
            hs.Add(Mk.Msh(r.head, Geo.Cone(0.13f, 0.3f, 10), robe, 0, 0.34f, -0.12f, -0.6f, 0, 0));
            hs.Add(Mk.Msh(r.head, Geo.Sphere(0.16f, 10, 8), voidM, 0, 0.07f, 0.0f));
            var m = Mk.Msh(r.head, Geo.Sphere(0.145f, 16, 12, Mathf.PI / 2 - 1.0f, 2.0f, 0.15f * Mathf.PI, 0.7f * Mathf.PI), mask, 0, 0.06f, 0.035f); m.transform.localScale = V(0.92f, 1.12f, 1); hs.Add(m);
            foreach (var sx in new[] { -1f, 1f }) { hs.Add(Mk.Msh(r.head, Geo.Sphere(0.03f, 8, 6), voidM, sx * 0.05f, 0.09f, 0.16f)); hs.Add(Mk.Msh(r.head, Geo.Sphere(0.008f, 6, 4), glow, sx * 0.05f, 0.09f, 0.145f)); }
            hs.Add(Mk.Msh(r.head, Geo.Box(0.008f, 0.08f, 0.004f), goldM, 0.05f, 0.03f, 0.172f, 0.1f, 0, 0.05f)); // the gold tear
            foreach (var (cx, cy, cr, cl) in new[] { (-0.02f, 0.13f, 0.5f, 0.07f), (0.06f, -0.02f, -0.4f, 0.06f) }) hs.Add(Mk.Msh(r.head, Geo.Box(0.004f, cl, 0.004f), Mats.Std(0x2a2620, 0.8f), cx, cy, 0.178f, 0, 0, cr));
            Mk.Head(hs.ToArray());
            r.jaw = Mk.Piv(r.head, 0, -0.03f, 0.03f); Mk.Head(Mk.Msh(r.jaw, Geo.Box(0.1f, 0.03f, 0.07f), mask, 0, -0.02f, 0.06f));
            // arms: long sleeves, bony forearms, broken manacles with links of chain hanging, long fingers; a rusted sickle
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var sh = Mk.Piv(r.torso, 0.3f * sx, 0.55f, 0);
                Mk.Msh(sh, Geo.Sphere(0.07f, 8, 6), robe);
                Mk.Msh(sh, Geo.Cyl(0.07f, 0.1f, 0.42f, 10, true), robe, 0, -0.2f, 0);
                var elbow = Mk.Piv(sh, 0, -0.42f, 0);
                Mk.Msh(elbow, Geo.Cyl(0.1f, 0.13f, 0.24f, 10, true), robe, 0, -0.08f, 0);
                Mk.Msh(elbow, GeoX.Limb(0.04f, 0.032f, 0.38f), skin);
                Mk.Msh(elbow, Geo.Torus(0.045f, 0.014f, 10, 4), iron, 0, -0.3f, 0, Mathf.PI / 2, 0, 0);
                for (int k = 0; k < 3; k++) Mk.Msh(elbow, Geo.Torus(0.022f, 0.007f, 8, 4), iron, 0.01f * sx, -0.34f - k * 0.04f, 0.02f, 0, k % 2 == 0 ? 0 : Mathf.PI / 2, 0);
                var hand = Mk.Piv(elbow, 0, -0.4f, 0);
                Mk.Msh(hand, Geo.Box(0.07f, 0.07f, 0.04f), skin);
                for (int k = 0; k < 4; k++) { Mk.Msh(hand, Geo.Cyl(0.009f, 0.006f, 0.12f, 5), skin, (k - 1.5f) * 0.018f, -0.09f, 0.015f, 0.35f, 0, 0); Mk.Msh(hand, Geo.Cone(0.006f, 0.03f, 4), mask, (k - 1.5f) * 0.018f, -0.15f, 0.04f, Mathf.PI + 0.4f, 0, 0); }
                if (sx > 0) { var sk = Mk.Piv(hand, 0, -0.05f, 0.02f); sk.SetRot(1.3f, 0, 0); Mk.Msh(sk, Geo.Cyl(0.016f, 0.016f, 0.4f, 6), Mats.Std(0x3a2a1a, 0.9f), 0, 0.15f, 0); Mk.Msh(sk, Geo.Torus(0.17f, 0.018f, 14, 4, Mathf.PI * 0.8f), iron, 0.1f, 0.38f, 0, 0, 0, 0.6f); }
                r.sh[i] = sh; r.elbow[i] = elbow; r.hand[i] = hand;
            }
            e.mats.Clear(); e.mats.Add(robe); e.mats.Add(skin); e.mats.Add(robeD); e.mats.Add(mask);
            r.extra = (en, dt) => { if (!en.dead) Mats.SetColor(glow, Mathf.Sin(time * 2 + en.walk) > 0.6f ? 0xfff0c0 : 0xc8a060); };
        }

        // ---------- MIRROR TWIN ----------
        void BuildMirrorTwin(Ent e, HuskRig r)
        {
            var glass = EM(e, Mats.Std(0x07070b, 0.02f, 0.95f, 0x020206)); var glass2 = EM(e, Mats.Std(0x14141c, 0.06f, 0.9f, 0x030308));
            var crack = EM(e, Mats.Basic(0xe8f6ff)); var visor = EM(e, Mats.Basic(0xffffff)); var scarf = EM(e, Mats.Std(0xd8dce4, 0.6f, 0, 0x101014, null, true));
            var shardM = EM(e, Mats.Std(0xd8ecff, 0.02f, 0.9f, 0x0a1420)); r.glow = crack;
            LimboLegs(r, 0.08f, 0.06f, glass, glass2, glass, null);
            for (int i = 0; i < 2; i++) { Mk.Msh(r.knee[i], Geo.Box(0.1f, 0.2f, 0.04f), glass2, 0, -0.15f, 0.06f); Mk.Msh(r.knee[i], Geo.Cone(0.02f, 0.1f, 4), shardM, 0, 0.02f, 0.08f, 1.2f, 0, 0); }
            // the long coat, open at the front, its tails split
            Mk.Msh(r.hips, Geo.Box(0.38f, 0.16f, 0.24f), glass2);
            foreach (var sx in new[] { -1f, 1f }) Mk.Msh(r.hips, Geo.Box(0.2f, 0.85f, 0.02f), glass, sx * 0.12f, -0.4f, -0.14f, -0.12f, sx * 0.2f, 0);
            foreach (var sx in new[] { -1f, 1f }) Mk.Msh(r.hips, Geo.Box(0.03f, 0.85f, 0.18f), glass, sx * 0.21f, -0.4f, 0, 0, 0, sx * 0.08f);
            // torso: a breastplate, fractures of light across it, glass shards grown out of the shoulders and back
            r.torso = Mk.Piv(r.hips, 0, 0.06f, 0);
            Mk.Msh(r.torso, GeoX.Capsule(0.15f, 0.2f, 3, 12), glass, 0, 0.34f, 0).transform.localScale = V(1.2f, 1, 0.85f);
            Mk.Msh(r.torso, Geo.Box(0.34f, 0.3f, 0.06f), glass2, 0, 0.4f, 0.11f);
            foreach (var (cx, cy, cr, cl) in new[] { (-0.06f, 0.45f, 0.6f, 0.2f), (0.05f, 0.36f, -0.5f, 0.16f), (0.1f, 0.48f, 1.2f, 0.1f), (-0.1f, 0.3f, -1.0f, 0.12f), (0.0f, 0.22f, 0.2f, 0.14f) }) Mk.Msh(r.torso, Geo.Box(0.006f, cl, 0.004f), crack, cx, cy, 0.145f, 0, 0, cr);
            for (int k = 0; k < 5; k++) Mk.Msh(r.torso, Geo.Cone(0.03f + (k % 2) * 0.015f, 0.2f + (k % 3) * 0.08f, 4), shardM, (k - 2) * 0.07f, 0.5f - (k % 2) * 0.06f, -0.14f, -2.2f + (k - 2) * 0.1f, 0, (k - 2) * 0.25f);
            for (int k = 0; k < 2; k++) Mk.Msh(r.torso, Geo.Box(0.08f, 0.5f, 0.015f), scarf, -0.05f + k * 0.09f, 0.4f, -0.17f, 0.25f, 0, (k - 0.5f) * 0.3f);
            Mk.Msh(r.torso, Geo.Torus(0.12f, 0.045f, 14, 5), scarf, 0, 0.62f, 0, Mathf.PI / 2, 0, 0);
            // head: a helm of black glass, a cracked T-visor burning white, a crest
            r.neck = Mk.Piv(r.torso, 0, 0.62f, 0.02f);
            Mk.Msh(r.neck, Geo.Cyl(0.05f, 0.06f, 0.12f, 8), glass2, 0, 0.04f, 0);
            r.head = Mk.Piv(r.neck, 0, 0.12f, 0.02f); r.head.SetRot(0, 0, 0.12f); // (a wrong tilt to it)
            var hs = new List<GameObject>();
            var helm = Mk.Msh(r.head, Geo.Sphere(0.16f, 16, 12), glass, 0, 0.09f, 0); helm.transform.localScale = V(0.95f, 1.1f, 1.08f); hs.Add(helm);
            hs.Add(Mk.Msh(r.head, Geo.Box(0.03f, 0.08f, 0.3f), glass2, 0, 0.26f, -0.03f, 0.2f, 0, 0));
            hs.Add(Mk.Msh(r.head, Geo.Box(0.2f, 0.03f, 0.02f), visor, 0, 0.11f, 0.165f)); hs.Add(Mk.Msh(r.head, Geo.Box(0.03f, 0.12f, 0.02f), visor, 0, 0.05f, 0.168f));
            hs.Add(Mk.Msh(r.head, Geo.Box(0.004f, 0.12f, 0.004f), crack, 0.05f, 0.14f, 0.17f, 0, 0, 0.6f)); hs.Add(Mk.Msh(r.head, Geo.Box(0.004f, 0.08f, 0.004f), crack, -0.06f, 0.04f, 0.168f, 0, 0, -0.8f));
            var vg = Gl(r.head, 0xffffff, 0.45f, 0.8f); vg.transform.localPosition = V(0, 0.1f, 0.2f);
            Mk.Head(hs.ToArray());
            r.jaw = Mk.Piv(r.head, 0, -0.02f, 0.03f); Mk.Head(Mk.Msh(r.jaw, Geo.Box(0.12f, 0.04f, 0.1f), glass2, 0, -0.03f, 0.05f));
            // shards hanging in the air round its head - they jitter, as if the reflection can't hold
            var halo = Mk.Piv(r.head, 0, 0.12f, 0); var shards = new List<(N n, Vector3 p)>();
            bool oc = Mk.colliders; Mk.colliders = false;
            for (int k = 0; k < 6; k++) { float a = k * Mathf.PI / 3; var p = V(Mathf.Cos(a) * 0.3f, Mathf.Sin(k * 1.7f) * 0.08f, Mathf.Sin(a) * 0.3f); var sn = Mk.Piv(halo, p.x, p.y, p.z); Mk.Msh(sn, Geo.Cone(0.03f, 0.12f, 3), shardM).transform.localScale = V(1, 1, 0.25f); sn.SetRot(a, a * 2, 0.5f); shards.Add((sn, p)); }
            Mk.colliders = oc;
            // arms: armoured, gauntlets, spiked pauldrons studded with glass
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var sh = Mk.Piv(r.torso, 0.31f * sx, 0.55f, 0);
                Mk.Msh(sh, Geo.Sphere(0.17f, 12, 6, 0, Mathf.PI * 2, 0, Mathf.PI / 2), glass2, 0, 0.04f, 0);
                for (int k = 0; k < 3; k++) Mk.Msh(sh, Geo.Cone(0.03f, 0.2f - k * 0.04f, 4), shardM, sx * (0.05f + k * 0.04f), 0.15f, (k - 1) * 0.06f, 0, 0, -sx * (0.3f + k * 0.25f));
                Mk.Msh(sh, GeoX.Limb(0.06f, 0.05f, 0.4f), glass);
                var elbow = Mk.Piv(sh, 0, -0.4f, 0);
                Mk.Msh(elbow, GeoX.Limb(0.052f, 0.045f, 0.36f), glass2);
                Mk.Msh(elbow, Geo.Cyl(0.06f, 0.065f, 0.14f, 10), glass, 0, -0.28f, 0);
                Mk.Msh(elbow, Geo.Box(0.004f, 0.2f, 0.004f), crack, 0.05f, -0.15f, 0.04f, 0, 0, 0.3f);
                var hand = Mk.Piv(elbow, 0, -0.4f, 0);
                Mk.Msh(hand, Geo.Box(0.08f, 0.09f, 0.05f), glass2);
                for (int k = 0; k < 4; k++) Mk.Msh(hand, Geo.Cyl(0.011f, 0.009f, 0.08f, 5), glass, (k - 1.5f) * 0.02f, -0.07f, 0.015f, 0.3f, 0, 0);
                r.sh[i] = sh; r.elbow[i] = elbow; r.hand[i] = hand;
            }
            e.dashes = P != null ? Mathf.Clamp(P.weapon, 0, 4) : 0; // the gun it copied
            if (!TwinGun(e, r.hand[1], glass)) { Mk.Msh(r.hand[1], Geo.Box(0.06f, 0.08f, 0.32f), glass, 0, -0.08f, 0.14f); }
            foreach (var rr in e.g.GetComponentsInChildren<Renderer>(true)) rr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes; // (it really is a mirror)
            e.mats.Clear(); e.mats.Add(glass); e.mats.Add(glass2); e.mats.Add(scarf);
            e.shootT = 1.5f + rand;
            float glitch = 0;
            r.extra = (en, dt) =>
            {
                if (en.dead) return;
                halo.ry += dt * 1.4f; glitch -= dt;
                if (glitch <= 0 && rand < dt * 0.8f) glitch = 0.12f;
                bool g = glitch > 0;
                foreach (var s in shards) s.n.t.localPosition = s.p + (g ? Random.insideUnitSphere * 0.08f : V(0, Mathf.Sin(time * 3 + s.p.x * 9) * 0.02f, 0));
                { var lp = r.root.t.localPosition; lp.x = g ? (rand - 0.5f) * 0.12f : 0; lp.z = g ? (rand - 0.5f) * 0.12f : 0; r.root.t.localPosition = lp; } // its reflection slipping
                Mats.SetColor(crack, g ? new Color(1, 1, 1) : Color.Lerp(new Color(0.6f, 0.8f, 1f), new Color(0.9f, 0.97f, 1f), 0.5f + 0.5f * Mathf.Sin(time * 6)));
                if ((g || Hypot(en.vel.x, en.vel.z) > 3) && rand < 0.4f) SpawnParticles(BodyPoint(en) + Random.insideUnitSphere * 0.4f, 1, 0.3f, cyanMat, 0.05f, 0.35f, 0);
            };
        }

        // ---------- HOLLOW PILGRIM ----------
        void BuildPilgrim(Ent e, HuskRig r)
        {
            var robe = EM(e, Mats.Std(0x5a5248, 0.95f, 0, 0x020202, ClothTex2(), true)); var robe2 = EM(e, Mats.Std(0x3e3830, 0.95f, 0, 0, ClothTex2(), true));
            var skin = EM(e, Mats.Std(0x6a6058, 0.8f, 0, 0, SkinTex2())); var gold = EM(e, Mats.Std(0xd8b860, 0.3f, 0.85f));
            var iron = Mats.Std(0x3a3634, 0.5f, 0.7f, 0, RustTex()); var wood = EM(e, Mats.Std(0x3a2a1a, 0.8f, 0, 0, GrimeTex())); var glassM = EM(e, Mats.Trans(0xffe8b0, 0.45f));
            var glow = EM(e, Mats.Basic(0xffd080)); var voidM = Mats.Std(0x060504, 1f); r.glow = glow;
            LimboLegs(r, 0.085f, 0.07f, robe2, skin, wood, Mats.Std(0x8a7a5a, 0.95f, 0, 0, ClothTex2()));
            // three tiers of heavy robe
            Mk.Msh(r.hips, Geo.Box(0.42f, 0.18f, 0.28f), robe2);
            Mk.Msh(r.hips, Geo.OpenCone(0.5f, 1.02f, 16), robe, 0, -0.4f, 0);
            for (int k = 0; k < 14; k++) { float a = k * Mathf.PI / 7; Mk.Msh(r.hips, Geo.Box(0.13f, 0.2f + (k % 3) * 0.08f, 0.015f), robe, Mathf.Sin(a) * 0.49f, -0.98f - (k % 3) * 0.04f, Mathf.Cos(a) * 0.49f, -0.45f * Mathf.Cos(a), a, 0.45f * Mathf.Sin(a)); }
            Mk.Msh(r.hips, Geo.OpenCone(0.38f, 0.5f, 14), robe2, 0, -0.1f, 0);
            Mk.Msh(r.hips, Geo.Torus(0.24f, 0.03f, 18, 5), Mats.Std(0x6a5438, 0.9f), 0, 0.06f, 0, Mathf.PI / 2, 0, 0).transform.localScale = V(1, 0.75f, 1);
            r.torso = Mk.Piv(r.hips, 0, 0.06f, 0); r.torso.SetRot(0.15f, 0, 0); // (stooped under its burden)
            Mk.Msh(r.torso, GeoX.Capsule(0.18f, 0.22f, 3, 12), robe, 0, 0.34f, 0).transform.localScale = V(1.25f, 1, 0.9f);
            Mk.Msh(r.torso, Geo.Cyl(0.24f, 0.4f, 0.32f, 16, true), robe2, 0, 0.5f, 0); // the cape over the shoulders
            // prayer beads, and a small gold icon hanging on them
            for (int k = 0; k < 14; k++) { float a = k / 14f * Mathf.PI * 2; Mk.Msh(r.torso, Geo.Sphere(0.02f, 6, 5), wood, Mathf.Sin(a) * 0.17f, 0.52f - (1 - Mathf.Cos(a)) * 0.14f, Mathf.Cos(a) * 0.12f + 0.06f); }
            Mk.Msh(r.torso, Geo.Box(0.05f, 0.08f, 0.015f), gold, 0, 0.24f, 0.2f);
            // the shrine on its back: a wooden reliquary trimmed in gold, candles burning on top
            var shrine = Mk.Piv(r.torso, 0, 0.42f, -0.25f);
            Mk.Msh(shrine, Geo.Box(0.34f, 0.46f, 0.2f), wood); Mk.Msh(shrine, Geo.Box(0.36f, 0.04f, 0.22f), gold, 0, 0.24f, 0); Mk.Msh(shrine, Geo.Box(0.36f, 0.04f, 0.22f), gold, 0, -0.24f, 0);
            Mk.Msh(shrine, Geo.Cone(0.24f, 0.18f, 4), wood, 0, 0.34f, 0, 0, Mathf.PI / 4, 0); Mk.Msh(shrine, Geo.Box(0.16f, 0.22f, 0.01f), gold, 0, 0, -0.105f);
            bool oc = Mk.colliders; Mk.colliders = false;
            for (int k = 0; k < 3; k++) { Mk.Msh(shrine, Geo.Cyl(0.015f, 0.015f, 0.1f, 6), Mats.Std(0xf0e8d0, 0.8f), (k - 1) * 0.1f, 0.3f + (k % 2) * 0.03f, 0.05f); var cg = Gl(shrine, 0xffd080, 0.25f, 0.9f); cg.transform.localPosition = V((k - 1) * 0.1f, 0.38f + (k % 2) * 0.03f, 0.05f); }
            Mk.colliders = oc;
            // head: a deep hood with a veil of chains hanging over the face, two gold eyes burning behind
            r.neck = Mk.Piv(r.torso, 0, 0.64f, 0.06f);
            r.head = Mk.Piv(r.neck, 0, 0.1f, 0.02f);
            var hs = new List<GameObject>();
            var hood = Mk.Msh(r.head, Geo.Sphere(0.25f, 16, 12, Mathf.PI / 2 + 0.7f, Mathf.PI * 2 - 1.4f, 0, Mathf.PI * 0.75f), robe, 0, 0.08f, -0.02f); hood.transform.localScale = V(1.05f, 1.15f, 1.12f); hs.Add(hood);
            hs.Add(Mk.Msh(r.head, Geo.Cone(0.18f, 0.36f, 10), robe, 0, 0.38f, -0.1f, -0.35f, 0, 0));
            hs.Add(Mk.Msh(r.head, Geo.Sphere(0.18f, 10, 8), voidM, 0, 0.07f, 0.01f));
            foreach (var sx in new[] { -1f, 1f }) hs.Add(Mk.Msh(r.head, Geo.Sphere(0.022f, 8, 6), glow, sx * 0.06f, 0.1f, 0.16f));
            for (int k = 0; k < 7; k++) { float x = (k - 3) * 0.035f; hs.Add(Mk.Msh(r.head, Geo.Cyl(0.005f, 0.005f, 0.22f - Mathf.Abs(k - 3) * 0.02f, 4), iron, x, 0.04f, 0.2f - Mathf.Abs(k - 3) * 0.012f)); }
            hs.Add(Mk.Msh(r.head, Geo.Torus(0.13f, 0.01f, 16, 4, Mathf.PI), gold, 0, 0.16f, 0.17f, 0, 0, 0));
            Mk.Head(hs.ToArray());
            r.jaw = Mk.Piv(r.head, 0, -0.03f, 0.03f); Mk.Head(Mk.Msh(r.jaw, Geo.Box(0.12f, 0.03f, 0.08f), skin, 0, -0.02f, 0.06f));
            // arms: heavy sleeves, big hands; the crook with the lantern in the left, a censer swinging from the right
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var sh = Mk.Piv(r.torso, 0.34f * sx, 0.56f, 0);
                Mk.Msh(sh, Geo.Sphere(0.1f, 8, 6), robe2);
                Mk.Msh(sh, Geo.Cyl(0.09f, 0.13f, 0.42f, 10, true), robe, 0, -0.2f, 0);
                var elbow = Mk.Piv(sh, 0, -0.42f, 0);
                Mk.Msh(elbow, Geo.Cyl(0.13f, 0.17f, 0.3f, 10, true), robe, 0, -0.1f, 0);
                Mk.Msh(elbow, GeoX.Limb(0.055f, 0.048f, 0.38f), skin);
                var hand = Mk.Piv(elbow, 0, -0.42f, 0);
                Mk.Msh(hand, Geo.Box(0.1f, 0.1f, 0.06f), skin);
                for (int k = 0; k < 4; k++) Mk.Msh(hand, Geo.Cyl(0.014f, 0.012f, 0.1f, 5), skin, (k - 1.5f) * 0.025f, -0.08f, 0.02f, 0.3f, 0, 0);
                r.sh[i] = sh; r.elbow[i] = elbow; r.hand[i] = hand;
            }
            // the crook and its lantern (see GameKinds: the light shields everything round it)
            var crook = Mk.Piv(r.hand[0], 0, -0.05f, 0.05f);
            Mk.Msh(crook, Geo.Cyl(0.025f, 0.03f, 2.0f, 6), wood, 0, 0.55f, 0); Mk.Msh(crook, Geo.Torus(0.14f, 0.025f, 12, 4, Mathf.PI), wood, 0.14f, 1.55f, 0);
            Mk.Msh(crook, Geo.Cyl(0.006f, 0.006f, 0.22f, 4), gold, 0.28f, 1.44f, 0);
            var lantern = Mk.Piv(crook, 0.28f, 1.2f, 0);
            Mk.Msh(lantern, Geo.Cyl(0.13f, 0.15f, 0.04f, 8), gold, 0, -0.16f, 0); Mk.Msh(lantern, Geo.Cyl(0.11f, 0.11f, 0.26f, 8), glassM, 0, -0.02f, 0);
            for (int k = 0; k < 4; k++) { float a = k * Mathf.PI / 2 + 0.4f; Mk.Msh(lantern, Geo.Box(0.018f, 0.28f, 0.018f), gold, Mathf.Cos(a) * 0.12f, -0.02f, Mathf.Sin(a) * 0.12f); }
            Mk.Msh(lantern, Geo.Cone(0.14f, 0.12f, 8), gold, 0, 0.17f, 0); Mk.Msh(lantern, Geo.Torus(0.04f, 0.01f, 8, 4), gold, 0, 0.26f, 0);
            Mk.Msh(lantern, Geo.Sphere(0.05f, 8, 6), EM(e, Mats.Basic(0xfff0c0)), 0, -0.03f, 0);
            oc = Mk.colliders; Mk.colliders = false;
            var lg = Gl(lantern, 0xffd080, 1.8f, 0.95f); lg.transform.localPosition = Vector3.zero;
            var beamM = EM(e, Mats.Add(0xffd890, 0.12f, Tex.RayFade, true, true)); Mk.Msh(lantern, Geo.Cyl(0.08f, 0.9f, 2.2f, 12, true), beamM, 0, -1.15f, 0).layer = Mk.LayerIgnore; // its light spilling down
            var censer = Mk.Piv(r.hand[1], 0, -0.06f, 0.02f);
            Mk.Msh(censer, Geo.Cyl(0.006f, 0.006f, 0.5f, 4), iron, 0, -0.25f, 0); Mk.Msh(censer, Geo.Sphere(0.09f, 10, 8), gold, 0, -0.55f, 0); Mk.Msh(censer, Geo.Cone(0.06f, 0.08f, 8), gold, 0, -0.46f, 0);
            var smokeAt = Mk.Piv(censer, 0, -0.55f, 0);
            Mk.colliders = oc;
            e.lantern = lantern.t; e.links = new List<GameObject>(); e.bubbles = new List<GameObject>();
            var linkM = EM(e, Mats.Add(0xffd080, 0.5f, null, true, true)); var bubM = EM(e, Mats.Add(0xffe0a0, 0.12f, null, true, true));
            oc = Mk.colliders; int ol = Mk.layer; Mk.colliders = false; Mk.layer = 0;
            for (int k = 0; k < 6; k++)
            {
                var lk = Mk.Msh(levelRoot, Geo.Cyl(1, 1, 1, 5), linkM); lk.layer = Mk.LayerIgnore; lk.SetActive(false); e.links.Add(lk);
                var bb = new GameObject("shield"); bb.transform.SetParent(levelRoot, false); bb.SetActive(false);
                Mk.Msh(bb.transform, Geo.Sphere(1, 16, 12), bubM).layer = Mk.LayerIgnore; Mk.Msh(bb.transform, Geo.Torus(1.02f, 0.02f, 32, 4), linkM, 0, 0, 0, Mathf.PI / 2).layer = Mk.LayerIgnore; Mk.Msh(bb.transform, Geo.Torus(1.02f, 0.02f, 32, 4), linkM).layer = Mk.LayerIgnore;
                e.bubbles.Add(bb);
            }
            // the protected ring: a gold circle with rune marks on the ground, the radius it shields
            var ringM = EM(e, Mats.Add(0xffd080, 0.35f, null, true, true));
            var ringP = Mk.Piv(r.root, 0, 0.06f / e.s, 0);
            Mk.Msh(ringP, Geo.Torus(PILGRIM_R / e.s, 0.04f / e.s, 64, 4), ringM, 0, 0, 0, Mathf.PI / 2).layer = Mk.LayerIgnore;
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; Mk.Msh(ringP, Geo.Box(0.25f / e.s, 0.01f, 0.08f / e.s), ringM, Mathf.Sin(a) * (PILGRIM_R - 0.35f) / e.s, 0, Mathf.Cos(a) * (PILGRIM_R - 0.35f) / e.s, 0, a, 0).layer = Mk.LayerIgnore; }
            Mk.colliders = oc; Mk.layer = ol;
            e.mats.Clear(); e.mats.Add(robe); e.mats.Add(skin); e.mats.Add(robe2); e.mats.Add(gold);
            r.extra = (en, dt) =>
            {
                Mats.SetAlpha(ringM, 0.2f + 0.15f * Mathf.Sin(time * 3)); Mats.SetAlpha(linkM, 0.3f + 0.25f * Mathf.Sin(time * 9)); Mats.SetAlpha(bubM, 0.08f + 0.05f * Mathf.Sin(time * 5));
                ringP.ry += dt * 0.3f; censer.rx = Mathf.Sin(time * 2.2f + en.walk) * 0.45f; censer.rz = Mathf.Cos(time * 1.7f) * 0.2f;
                if (rand < 0.25f) SpawnParticles(smokeAt.t.position, 1, 0.3f, ashMat, 0.08f, 1.2f, -1);
                PilgrimLinks(en);
            };
        }

        // ================= the Warden of Hours, reworked =================
        void WardenParts(Ent e, WardenRig r)
        {
            bool oc = Mk.colliders; Mk.colliders = false; // (decoration: his hitboxes stay as they were)
            r.goldM.SetFloat("_Metallic", 0.55f); r.trimM.SetFloat("_Metallic", 0.55f);
            var robeD = EM(e, Mats.Std(0x34323e, 0.95f, 0, 0x020204, ClothTex2(), true)); var gold = r.goldM; var trim = r.trimM;
            var brass = EM(e, Mats.Std(0x8a6a38, 0.4f, 0.8f, 0, RustTex())); var iron = EM(e, Mats.Std(0x2a2826, 0.5f, 0.7f, 0, RustTex())); var bone = EM(e, Mats.Std(0xd8d0bc, 0.5f));
            var glassM = EM(e, Mats.Trans(0xd8f0ff, 0.18f, true)); var numM = EM(e, Mats.Basic(0xffd890)); var ember = EM(e, Mats.Basic(0xffa040)); var fire = EM(e, Mats.Add(0xff8a20, 0.55f, null, true, true));
            var charred = EM(e, Mats.Std(0x1c1814, 0.95f, 0, 0x0a0400, ClothTex2(), true)); var vortexM = EM(e, Mats.Add(0xffc060, 0.2f, Tex.RayFade, true, true));
            // ---------------- phase 1: ancient, vast ----------------
            // a towering collar behind the hood, its rim gilded and spiked
            var collar = Mk.Msh(r.chest, Geo.Cyl(2.4f, 3.4f, 2.6f, 20, true), robeD, 0, 5.0f, -0.6f, -0.25f, 0, 0); r.robe1.Add(collar);
            for (int k = 0; k < 9; k++) { float a = (k / 8f - 0.5f) * 2.4f; r.robe1.Add(Mk.Msh(r.chest, Geo.Cone(0.14f, 1.0f + (k % 2) * 0.5f, 5), gold, Mathf.Sin(a) * 2.9f, 6.5f, -0.9f - Mathf.Cos(a) * 0.6f, -0.3f, 0, -a * 0.4f)); }
            // a cloak of long tattered strips falling from the shoulders behind him
            for (int k = 0; k < 11; k++) { float x = (k - 5) * 0.55f; r.robe1.Add(Mk.Msh(r.chest, Geo.Box(0.5f, 9 + (k % 3) * 1.2f, 0.08f), robeD, x, -1.0f - (k % 3) * 0.6f, -2.0f - Mathf.Abs(k - 5) * 0.08f, 0.08f, 0, (k - 5) * 0.03f)); }
            // the dial, enormous and ornate: a broad bronze bezel, Roman numeral bars, a turning gear seen behind glass
            r.face.Scale(1.25f);
            Mk.Msh(r.face, Geo.Torus(1.5f, 0.2f, 40, 6), brass, 0, 0, -0.02f);
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; Mk.Msh(r.face, Geo.Box(k % 3 == 0 ? 0.16f : 0.08f, 0.34f, 0.04f), numM, Mathf.Sin(a) * 1.12f, Mathf.Cos(a) * 1.12f, 0.05f, 0, 0, -a); }
            var dialGear = Mk.Piv(r.face, 0.45f, -0.45f, 0.03f); GearDeco(dialGear.t, 0.38f, 12, brass, out var dgh); r.spins.Add((dgh, Vector3.forward, 40));
            Mk.Msh(r.face, Geo.Sphere(3.0f, 24, 6, 0, Mathf.PI * 2, 0, Mathf.PI * 0.15f), glassM, 0, 0, -2.7f, Mathf.PI / 2, 0, 0); // (a shallow dome of glass over it)
            // gears turning at his shoulders, a pendulum swinging under his chest, an hourglass at his belt
            foreach (var sx in new[] { -1f, 1f }) { var sg = Mk.Piv(r.chest, sx * 3.1f, 3.9f, 0.2f); sg.SetRot(0, sx * 0.5f, 0); GearDeco(sg.t, 0.9f, 14, brass, out var sgh); r.spins.Add((sgh, Vector3.forward, sx * 50)); r.robe1.Add(sg.go); }
            r.pendulum = Mk.Piv(r.chest, 0, 0.6f, 2.0f);
            Mk.Msh(r.pendulum, Geo.Cyl(0.06f, 0.06f, 3.2f, 6), gold, 0, -1.6f, 0); Mk.Msh(r.pendulum, Geo.Cyl(0.6f, 0.6f, 0.14f, 20), gold, 0, -3.3f, 0, Mathf.PI / 2, 0, 0); Mk.Msh(r.pendulum, Geo.Circle(0.5f, 20), r.faceM, 0, -3.3f, 0.08f);
            r.robe1.Add(r.pendulum.go);
            var hg = Mk.Piv(r.body, 2.3f, 7.6f, 1.2f); hg.SetRot(0, 0, 0.15f);
            Mk.Msh(hg, Geo.Cyl(0.05f, 0.45f, 0.7f, 12), glassM, 0, 0.35f, 0); Mk.Msh(hg, Geo.Cyl(0.45f, 0.05f, 0.7f, 12), glassM, 0, -0.35f, 0); Mk.Msh(hg, Geo.Cone(0.32f, 0.3f, 12), numM, 0, -0.55f, 0);
            foreach (var y in new[] { 0.74f, -0.74f }) Mk.Msh(hg, Geo.Cyl(0.55f, 0.55f, 0.08f, 12), gold, 0, y, 0);
            r.robe1.Add(hg.go);
            // chains of pocket watches hanging from his belt, swinging as he moves
            for (int k = 0; k < 7; k++)
            {
                float a = (k / 6f - 0.5f) * 2.8f; var ch = Mk.Piv(r.body, Mathf.Sin(a) * 2.5f, 8.2f, Mathf.Cos(a) * 2.2f);
                for (int l = 0; l < 6; l++) Mk.Msh(ch, Geo.Torus(0.09f, 0.025f, 8, 4), iron, 0, -0.16f - l * 0.17f, 0, 0, l % 2 == 0 ? 0 : Mathf.PI / 2, 0);
                Mk.Msh(ch, Geo.Cyl(0.3f, 0.3f, 0.08f, 16), gold, 0, -1.3f, 0, Mathf.PI / 2, 0, 0); Mk.Msh(ch, Geo.Circle(0.25f, 16), r.faceM, 0, -1.3f, 0.045f);
                r.chains.Add(ch); r.robe1.Add(ch.go);
            }
            // long skeletal fingers, clawed and ringed in gold
            foreach (var hand in new[] { r.lHand, r.rHand })
            {
                for (int k = 0; k < 4; k++) { Mk.Msh(hand, Geo.Cone(0.06f, 0.4f, 5), bone, (k - 1.5f) * 0.17f, -0.95f, 0.22f, Mathf.PI + 0.5f, 0, 0); Mk.Msh(hand, Geo.Torus(0.08f, 0.025f, 10, 4), gold, (k - 1.5f) * 0.17f, -0.45f, 0.16f, Mathf.PI / 2 + 0.4f, 0, 0); }
                Mk.Msh(hand, Geo.Cyl(0.06f, 0.08f, 0.55f, 6), bone, 0.36f, -0.2f, 0.12f, 0.4f, 0, -0.9f);
            }
            // ---------------- phase 2: THE LAST HOUR ----------------
            // what's left of the robe: charred, torn short, its edges still burning
            var rb = Mk.Msh(r.body, Geo.Cyl(2.4f, 4.0f, 4.2f, 24, true), charred, 0, 6.8f, 0); r.phase2.Add(rb);
            r.phase2.Add(Mk.Msh(r.body, Geo.Torus(4.0f - 0.1f, 0.09f, 40, 4), fire, 0, 4.72f, 0, Mathf.PI / 2, 0, 0));
            for (int k = 0; k < 16; k++) { float a = k * Mathf.PI / 8; r.phase2.Add(Mk.Msh(r.body, Geo.Box(0.9f, 1.4f + (k % 3) * 0.8f, 0.06f), charred, Mathf.Sin(a) * 3.9f, 4.2f - (k % 3) * 0.4f, Mathf.Cos(a) * 3.9f, -0.15f * Mathf.Cos(a), a, 0)); }
            // the dial cracked open, burning through the cracks
            foreach (var (cx, cy, cr, cl) in new[] { (0.2f, 0.3f, 0.5f, 1.2f), (-0.4f, -0.2f, -0.7f, 0.9f), (0.5f, -0.5f, 1.1f, 0.7f), (-0.1f, 0.6f, -0.2f, 0.6f) })
                r.phase2.Add(Mk.Msh(r.face, Geo.Box(0.06f, cl, 0.03f), ember, cx, cy, 0.14f, 0, 0, cr));
            // two great clock hands rising from the hood like horns
            foreach (var sx in new[] { -1f, 1f }) { var hn = Mk.Piv(r.hood, sx * 1.1f, 2.6f, -0.3f); hn.SetRot(-0.3f, 0, -sx * 0.45f); Mk.Msh(hn, Geo.Box(0.32f, 3.4f, 0.1f), gold, 0, 1.7f, 0); Mk.Msh(hn, Geo.Cone(0.32f, 0.8f, 4), gold, 0, 3.8f, 0); Mk.Msh(hn, Geo.Box(0.34f, 3.3f, 0.02f), ember, 0, 1.7f, 0.06f); r.phase2.Add(hn.go); }
            // a second halo of burning numerals behind the first, turning the other way
            var h2 = Mk.Piv(r.hood, 0, 1.6f, -2.0f); Mk.Msh(h2, Geo.Torus(5.0f, 0.1f, 64, 4), fire);
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; Mk.Msh(h2, Geo.Box(0.3f, 1.0f, 0.08f), ember, Mathf.Sin(a) * 4.6f, Mathf.Cos(a) * 4.6f, 0, 0, 0, -a); }
            r.halo2 = h2; r.phase2.Add(h2.go);
            // a ring of clock-hand blades turning round him, a spine of gears up his back
            r.bladeRing = Mk.Piv(r.body, 0, 7.5f, 0);
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; var bl = Mk.Piv(r.bladeRing, Mathf.Sin(a) * 6.4f, (k % 2) * 1.2f - 0.6f, Mathf.Cos(a) * 6.4f); bl.SetRot(Mathf.PI / 2, a, 0); Mk.Msh(bl, Geo.Box(0.24f, 2.2f, 0.06f), gold); Mk.Msh(bl, Geo.Cone(0.24f, 0.6f, 4), gold, 0, 1.4f, 0); Mk.Msh(bl, Geo.Box(0.26f, 2.1f, 0.02f), ember, 0, 0, 0.04f); }
            r.phase2.Add(r.bladeRing.go);
            for (int k = 0; k < 5; k++) { var sg = Mk.Piv(r.chest, 0, 0.4f + k * 0.9f, -1.9f); GearDeco(sg.t, 0.55f - k * 0.04f, 10, k % 2 == 0 ? gold : brass, out var sgh); r.spins.Add((sgh, Vector3.forward, (k % 2 == 0 ? 1 : -1) * 120)); r.phase2.Add(sg.go); }
            // and where his robe was, a vortex of burning time pouring round him
            r.vortex = Mk.Piv(r.body, 0, 1.8f, 0); Mk.Msh(r.vortex, Geo.Cyl(4.5f, 1.2f, 4.5f, 24, true), vortexM, 0, 0, 0); Mk.Msh(r.vortex, Geo.Cyl(3.2f, 0.8f, 3.5f, 20, true), vortexM, 0, -0.3f, 0, 0, 0.7f, 0);
            foreach (var rr in r.vortex.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
            r.phase2.Add(r.vortex.go);
            foreach (var rr in e.g.GetComponentsInChildren<Renderer>(true)) rr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
            Mk.colliders = oc;
        }
        // every frame (AnimateWarden): the moving parts of the rework
        void WardenPartsAnim(Ent e, float dt)
        {
            var r = e.wd; if (r == null) return;
            foreach (var s in r.spins) if (s.t) s.t.Rotate(s.axis, s.sp * dt * (e.phase2 ? 2.2f : 1), Space.Self);
            float sway = Mathf.Sin(time * 1.3f);
            if (r.pendulum != null) r.pendulum.rz = Mathf.Sin(time * 1.8f) * 0.35f;
            for (int i = 0; i < r.chains.Count; i++) { r.chains[i].rx = Mathf.Sin(time * 1.6f + i * 0.9f) * 0.2f - e.vel.magnitude * 0.02f; r.chains[i].rz = Mathf.Cos(time * 1.2f + i) * 0.12f; }
            if (r.bladeRing != null && r.bladeRing.go.activeSelf) { r.bladeRing.ry += dt * 1.6f; r.bladeRing.py = 7.5f + sway * 0.3f; }
            if (r.halo2 != null && r.halo2.go.activeSelf) r.halo2.rz -= dt * 0.9f;
            if (r.vortex != null && r.vortex.go.activeSelf) { r.vortex.ry += dt * 3.5f; if (rand < 0.8f) { float a = rand * Mathf.PI * 2; SpawnParticles(r.body.t.TransformPoint(V(Mathf.Sin(a) * 3.5f, rand * 3, Mathf.Cos(a) * 3.5f)), 1, 1.5f, emberMat, 0.12f, 0.9f, -2); } }
        }
    }
}
