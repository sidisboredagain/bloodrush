using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    public class CarnRig
    {
        public N root, hips, torso, neck, head, jaw; public N[] thigh = new N[2], knee = new N[2];
        public N rSh, rEl, rHand, lSh, lEl, lHand; public List<N> hooks = new List<N>(); public GameObject coreGlow; public Material glow; public List<Material> mats = new List<Material>();
    }
    public class SerRig
    {
        public N root, torso, head, halo, spear, orbit; public N[] sh = new N[2], el = new N[2];
        public List<N> wings = new List<N>(); public List<float> wingS = new List<float>(), wingBase = new List<float>();
        public List<List<N>> ribbons = new List<List<N>>();
        public Material eyeMat, featherGlow; public GameObject eyeGlow, gemGlow; public Transform eyeMarker; public List<Material> mats = new List<Material>();
        // the rework (GameSeraph2): phase-1 extras, and the fallen form built hidden
        public List<GameObject> p1 = new List<GameObject>(), p2 = new List<GameObject>(), spearP1 = new List<GameObject>();
        public List<N> w1 = new List<N>(), w2 = new List<N>(), frags = new List<N>(); public List<Transform> p2Feathers = new List<Transform>();
        public N haloFrag, mask, core, armorOrbit, sword; public GameObject coreGlow; public Material p2Edge, p2Ember, p2AuraM; public Color[] colors;
    }

    public partial class Game
    {
        static void Sm(N n, int ax, float v, float k)
        {
            if (n == null || !n.t) return; // the part may already be gone (torn off and cleaned up as debris)
            var r = n.r; r[ax] += (v - r[ax]) * k; n.r = r; n.Apply();
        }

        // ---------- Boss 1: CARNIFEX, THE FLESH TITAN ----------
        CarnRig BuildCarnifex(Ent e)
        {
            var flesh = EM(e, Mats.Std(0x7c2420, 0.7f));
            var bone = EM(e, Mats.Std(0xd8ccb0, 0.5f));
            var iron = EM(e, Mats.Std(0x45454e, 0.4f, 0.6f));
            var glow = EM(e, Mats.Basic(0xff3000));
            var r = new CarnRig { glow = glow };
            r.root = Mk.Piv(e.g.transform);
            r.hips = Mk.Piv(r.root, 0, 1.1f, 0);
            Mk.Msh(r.hips, Geo.Box(0.72f, 0.3f, 0.46f), flesh);
            Mk.Msh(r.hips, Geo.Box(0.78f, 0.1f, 0.5f), iron, 0, 0.12f, 0);
            for (int i = 0; i < 5; i++) Mk.Msh(r.hips, Geo.Box(0.14f, 0.34f, 0.03f), iron, -0.28f + i * 0.14f, -0.2f, 0.25f, 0.12f);
            foreach (var hx in new[] { -0.3f, 0.3f })
            {
                var hk = Mk.Piv(r.hips, hx, -0.05f, -0.26f);
                Mk.Msh(hk, Geo.Box(0.02f, 0.4f, 0.02f), iron, 0, -0.2f, 0);
                Mk.Msh(hk, Geo.Cone(0.05f, 0.16f, 4), iron, 0, -0.44f, -0.03f, Mathf.PI - 0.6f);
                r.hooks.Add(hk);
            }
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var thigh = Mk.Piv(r.hips, 0.27f * sx, -0.1f, 0);
                Mk.Msh(thigh, Geo.Box(0.3f, 0.56f, 0.32f), flesh, 0, -0.28f, 0);
                var knee = Mk.Piv(thigh, 0, -0.56f, 0);
                Mk.Msh(knee, Geo.Box(0.25f, 0.52f, 0.27f), flesh, 0, -0.26f, 0);
                Mk.Msh(knee, Geo.Box(0.27f, 0.36f, 0.08f), iron, 0, -0.22f, 0.14f);
                Mk.Msh(knee, Geo.Cone(0.06f, 0.2f, 5), bone, 0, 0.0f, 0.17f, 1.2f);
                Mk.Msh(knee, Geo.Box(0.32f, 0.12f, 0.46f), bone, 0, -0.56f, 0.06f);
                for (int k = -1; k <= 1; k++) Mk.Msh(knee, Geo.Cone(0.04f, 0.16f, 4), bone, k * 0.1f, -0.58f, 0.33f, Mathf.PI / 2);
                r.thigh[i] = thigh; r.knee[i] = knee;
            }
            r.torso = Mk.Piv(r.hips, 0, 0.12f, 0);
            Mk.Msh(r.torso, Geo.Box(0.95f, 0.75f, 0.56f), flesh, 0, 0.42f, 0);
            Mk.Msh(r.torso, Geo.Box(1.2f, 0.38f, 0.62f), flesh, 0, 0.86f, 0);
            for (int i = 0; i < 4; i++) foreach (var sx in new[] { -1, 1 }) Mk.Msh(r.torso, Geo.Box(0.36f, 0.05f, 0.05f), bone, 0.22f * sx, 0.24f + i * 0.14f, 0.3f, 0, 0, -0.28f * sx);
            Mk.Msh(r.torso, Geo.Box(0.07f, 0.6f, 0.05f), bone, 0, 0.45f, 0.3f);
            Mk.Msh(r.torso, Geo.Sphere(0.12f, 10, 8), glow, 0, 0.52f, 0.29f);
            r.coreGlow = Gl(r.torso, 0xff3000, 1.1f, 0.8f); r.coreGlow.transform.localPosition = V(0, 0.52f, 0.38f);
            for (int i = 0; i < 5; i++) Mk.Msh(r.torso, Geo.Cone(0.07f + i * 0.01f, 0.36f, 5), bone, 0, 0.25f + i * 0.17f, -0.33f, -1.6f);
            for (int i = 0; i < 7; i++)
            {
                float t = i / 6f;
                Mk.Msh(r.torso, Geo.Torus(0.05f, 0.015f, 8), iron, lerp(-0.52f, 0.45f, t), lerp(1.0f, 0.15f, t), 0.32f + Mathf.Sin(t * Mathf.PI) * 0.03f, 0, i % 2 == 1 ? Mathf.PI / 2 : 0, 0);
            }
            r.neck = Mk.Piv(r.torso, 0, 1.02f, 0.12f);
            Mk.Msh(r.neck, Geo.Box(0.3f, 0.2f, 0.3f), flesh, 0, 0.05f, 0);
            r.head = Mk.Piv(r.neck, 0, 0.16f, 0.1f);
            var skull = Mk.Msh(r.head, Geo.Sphere(0.3f, 14, 10), bone, 0, 0.1f, 0); skull.transform.localScale = V(1, 0.95f, 1.1f);
            var heads = new List<GameObject> { skull };
            foreach (var xy in new[] { V(-0.11f, 0.14f, 0), V(0.11f, 0.14f, 0), V(-0.18f, 0.05f, 0), V(0.18f, 0.05f, 0) }) heads.Add(Mk.Msh(r.head, Geo.Box(0.07f, 0.04f, 0.03f), glow, xy.x, xy.y, 0.31f));
            for (int i = 0; i < 7; i++) { float a = (i / 6f - 0.5f) * 2.4f; heads.Add(Mk.Msh(r.head, Geo.Cone(0.045f, 0.34f, 5), bone, Mathf.Sin(a) * 0.22f, 0.36f, Mathf.Cos(a) * 0.1f - 0.06f, -0.2f, 0, -a * 0.5f)); }
            r.jaw = Mk.Piv(r.head, 0, -0.04f, 0.06f);
            heads.Add(Mk.Msh(r.jaw, Geo.Box(0.44f, 0.1f, 0.36f), bone, 0, -0.06f, 0.1f));
            foreach (var fx in new[] { -0.15f, -0.05f, 0.05f, 0.15f }) heads.Add(Mk.Msh(r.jaw, Geo.Cone(0.025f, 0.12f, 4), bone, fx, 0.04f, 0.25f));
            foreach (var fx in new[] { -0.12f, 0.12f }) heads.Add(Mk.Msh(r.head, Geo.Cone(0.03f, 0.16f, 4), bone, fx, -0.08f, 0.27f, Mathf.PI));
            Mk.Head(heads.ToArray());
            N MkArm(int sx, out N elbow, out N hand)
            {
                var sh = Mk.Piv(r.torso, 0.72f * sx, 0.9f, 0);
                Mk.Msh(sh, Geo.Box(0.48f, 0.26f, 0.48f), iron, 0.04f * sx, 0.06f, 0);
                for (int k = 0; k < 3; k++) Mk.Msh(sh, Geo.Cone(0.05f, 0.28f, 5), iron, 0.04f * sx + (k - 1) * 0.13f, 0.3f, 0);
                Mk.Msh(sh, Geo.Box(0.3f, 0.62f, 0.3f), flesh, 0, -0.32f, 0);
                elbow = Mk.Piv(sh, 0, -0.64f, 0);
                Mk.Msh(elbow, Geo.Box(0.27f, 0.56f, 0.27f), flesh, 0, -0.28f, 0);
                Mk.Msh(elbow, Geo.Box(0.3f, 0.3f, 0.3f), iron, 0, -0.36f, 0);
                hand = Mk.Piv(elbow, 0, -0.6f, 0);
                Mk.Msh(hand, Geo.Box(0.24f, 0.18f, 0.24f), flesh, 0, -0.06f, 0);
                return sh;
            }
            r.rSh = MkArm(1, out r.rEl, out r.rHand); r.lSh = MkArm(-1, out r.lEl, out r.lHand);
            Mk.Msh(r.rHand, Geo.Cyl(0.04f, 0.04f, 0.3f, 6), iron, 0, -0.12f, 0);
            Mk.Msh(r.rHand, Geo.Box(0.09f, 0.2f, 0.3f), bone, 0, -0.3f, 0.05f);
            Mk.Msh(r.rHand, Geo.Box(0.07f, 1.5f, 0.72f), iron, 0, -0.95f, 0.2f);
            Mk.Msh(r.rHand, Geo.Box(0.08f, 1.5f, 0.05f), glow, 0, -0.95f, 0.57f);
            for (int k = 0; k < 3; k++) Mk.Msh(r.rHand, Geo.Cyl(0.05f, 0.05f, 0.09f, 6), bone, 0, -0.6f - k * 0.4f, 0.0f, 0, 0, Mathf.PI / 2);
            for (int k = 0; k < 4; k++) Mk.Msh(r.lHand, Geo.Cone(0.045f, 0.52f, 4), bone, -0.09f + k * 0.06f, -0.36f, 0.06f, Mathf.PI - 0.15f);
            r.mats.Add(flesh); r.mats.Add(bone); r.mats.Add(iron);
            e.mats.Clear(); e.mats.AddRange(r.mats);
            return r;
        }

        // ---------- Boss 2: SERAPH OF RUIN ----------
        // a porcelain angel in a gold cuirass and scalloped armoured gown, a faceless three-eyed mask under a spired crown,
        // an ornate halo, six great feathered wings edged with light and a winged lance
        static Texture2D hemTex;
        SerRig BuildSeraph(Ent e)
        {
            var porcelain = EM(e, Mats.Std(0xf3eee4, 0.22f, 0.08f));
            var gold = EM(e, Mats.Std(0xd8a828, 0.22f, 0.85f, 0x2a1800, null, true));
            var cloth = EM(e, Mats.Std(0xc9d4ea, 0.75f, 0, 0, null, true)); // pale sky-blue silk
            var featherM = EM(e, Mats.Std(0xfffcf6, 0.4f, 0.05f, 0, null, true));
            var eyeMat = EM(e, Mats.Basic(0x80e8ff));
            var featherGlow = EM(e, Mats.Basic(0x80e8ff, null, true));
            var white = EM(e, Mats.Basic(0xffffff));
            var r = new SerRig { eyeMat = eyeMat, featherGlow = featherGlow };
            r.root = Mk.Piv(e.g.transform);
            // gown, with an armoured overskirt of scalloped porcelain plates over a gold under-layer
            Mk.Msh(r.root, LATHE("sgown", new[] { 0.8f, 0.18f, 0.62f, 0.22f, 0.3f, 0.32f, 0.0f, 0.43f, -0.5f, 0.58f, -0.95f, 0.72f, -1.02f, 0.74f }, 40), cloth);
            var over = GeoX.Cached("sOverskirt", () =>
            {
                var g = GeoX.LatheMB(GeoX.YR(0.8f, 0.215f, 0.6f, 0.285f, 0.35f, 0.4f, 0.1f, 0.51f, -0.15f, 0.62f, -0.35f, 0.71f, -0.4f, 0.73f), 60);
                for (int i = 0; i < g.v.Count; i++) // petals: the lower edge dips between six points
                {
                    var p = g.v[i]; float t = clamp((0.8f - p.y) / 1.2f, 0, 1), phi = Mathf.Atan2(p.x, p.z);
                    p.y -= t * t * 0.22f * (0.5f + 0.5f * Mathf.Cos(phi * 6)); g.v[i] = p;
                }
                g.ComputeNormals(); return g.Build();
            });
            if (!hemTex) // the gilded hem (vertex colours in the HTML game) as a gradient along the lathe's v
            {
                hemTex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (int y = 0; y < 64; y++)
                {
                    float v = y / 63f, k = clamp((v - 5f / 6f) * 6f, 0, 1);
                    hemTex.SetPixel(0, y, new Color(1 - k * 0.12f, 1 - k * 0.36f, 1 - k * 0.82f));
                }
                hemTex.Apply();
            }
            var plateM = EM(e, Mats.Std(0xf6f2ea, 0.2f, 0.25f, 0, hemTex));
            Mk.Msh(r.root, over, plateM);
            MshS(r.root, RING(0.205f, 0.03f, 40, 8), gold, 0, 0.79f, 0, Mathf.PI / 2, 0, 0, 1, 1, 1);
            // ribbons trailing from the hem
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2 + 0.3f;
                var p = Mk.Piv(r.root, Mathf.Sin(a) * 0.7f, -0.98f, Mathf.Cos(a) * 0.7f); p.ry = a; var segs = new List<N> { p };
                for (int j = 0; j < 3; j++) { MshS(p, CAPS(0.045f, 0.3f), cloth, 0, -0.18f, 0, 0, 0, 0, 1.5f - j * 0.25f, 1, 0.3f); Mk.Msh(p, CAPS(0.006f, 0.32f), gold, 0, -0.18f, 0.016f); p = Mk.Piv(p, 0, -0.36f, 0); segs.Add(p); }
                MshS(p, FEATHER(0.16f, 0.06f, 0), gold, 0, 0.02f, 0, 0, 0, Mathf.PI, 1, 1, 1);
                r.ribbons.Add(segs);
            }
            // torso: sculpted porcelain with a gold cuirass, chest gem, feathered collar and pauldrons
            r.torso = Mk.Piv(r.root, 0, 0.75f, 0);
            float[] TP = { -0.1f, 0.16f, 0.02f, 0.155f, 0.14f, 0.17f, 0.28f, 0.22f, 0.4f, 0.24f, 0.48f, 0.225f, 0.55f, 0.16f, 0.6f, 0.07f, 0.61f, 0.0f };
            MshS(r.torso, LATHE("storso", TP, 32), porcelain, 0, 0, 0, 0, 0, 0, 1, 1, 0.7f);
            MshS(r.torso, LATHE("sbreast", ScaleR(TP, 1.035f, 1, 7), 28, -1.15f, 2.3f), gold, 0, 0, 0, 0, 0, 0, 1, 1, 0.7f);
            MshS(r.torso, RING(0.165f, 0.022f, 40, 8), gold, 0, 0.02f, 0, Mathf.PI / 2, 0, 0, 1, 0.72f, 1);
            Mk.Msh(r.torso, SP(0.075f, 18, 14), eyeMat, 0, 0.3f, 0.165f);
            Mk.Msh(r.torso, RING(0.085f, 0.016f, 32, 6), gold, 0, 0.3f, 0.168f);
            r.gemGlow = Gl(r.torso, 0x80e8ff, 0.8f, 0.8f); r.gemGlow.transform.localPosition = V(0, 0.3f, 0.24f);
            foreach (var s in new[] { -1f, 1f })
                for (int k = 0; k < 3; k++) MshS(r.torso, FEATHER(r3(0.34f - k * 0.05f), 0.1f, 0.06f), gold, 0.28f * s, 0.53f - k * 0.025f, -0.03f + k * 0.05f, 0, 0, -s * (2.3f + k * 0.22f), s, 1, 1);
            Mk.Msh(r.torso, LIMB(0.06f, 0.07f, 0.16f), porcelain, 0, 0.72f, 0);
            // head: faceless porcelain mask with three burning eyes, gold tear lines, veil and a spired crown
            r.head = Mk.Piv(r.torso, 0, 0.72f, 0);
            var face = MshS(r.head, SP(0.26f, 32, 24), porcelain, 0, 0.06f, 0, 0, 0, 0, 0.8f, 1.1f, 0.86f);
            var heads = new List<GameObject> { face };
            foreach (var s in new[] { -1f, 1f }) heads.Add(Mk.Msh(r.head, CAPS(0.021f, 0.12f), eyeMat, 0.085f * s, 0.1f, 0.2f, 0, 0, Mathf.PI / 2 + 0.25f * s));
            heads.Add(Mk.Msh(r.head, CAPS(0.014f, 0.09f), eyeMat, 0, 0.21f, 0.185f, -0.45f, 0, 0));
            foreach (var s in new[] { -1f, 1f }) heads.Add(Mk.Msh(r.head, CAPS(0.006f, 0.14f), gold, 0.088f * s, -0.02f, 0.19f, -0.25f, 0, 0));
            heads.Add(Mk.Msh(r.head, CAPS(0.006f, 0.07f), gold, 0, -0.12f, 0.172f, 0, 0, Mathf.PI / 2));
            Mk.Msh(r.head, LATHE("sveil", new[] { 0.26f, 0.12f, 0.14f, 0.25f, -0.1f, 0.28f, -0.4f, 0.27f, -0.72f, 0.22f }, 22, Mathf.PI - 1.4f, 2.8f), cloth, 0, 0.06f, -0.02f);
            var crown = Mk.Piv(r.head, 0, 0.21f, 0);
            MshS(crown, RING(0.18f, 0.02f, 40, 6), gold, 0, 0, 0, Mathf.PI / 2, 0, 0, 0.95f, 1.05f, 1);
            var SPIRE = LATHE("sspire", new[] { 0, 0.025f, 0.07f, 0.03f, 0.11f, 0.018f, 0.35f, 0.0f }, 10);
            for (int i = 0; i < 7; i++)
            {
                float a = (i / 6f - 0.5f) * 2.4f, h = 1.3f - Mathf.Abs(i / 6f - 0.5f) * 1.4f;
                MshS(crown, SPIRE, gold, Mathf.Sin(a) * 0.171f, 0, Mathf.Cos(a) * 0.189f, 0.18f * Mathf.Cos(a), 0, -0.3f * Mathf.Sin(a), 1, h, 1);
            }
            Mk.Msh(crown, SP(0.03f, 12, 10), eyeMat, 0, 0.03f, 0.2f);
            MarkAllHead(r.head);
            r.eyeMarker = new GameObject("eye").transform; r.eyeMarker.SetParent(r.head.t, false); r.eyeMarker.localPosition = V(0, 0.1f, 0.35f);
            r.eyeGlow = Gl(r.head, 0x80e8ff, 0.6f, 0.7f); r.eyeGlow.transform.localPosition = V(0, 0.1f, 0.28f);
            // halo: double gold ring with a channel of light, sun-ray spires and pearls
            r.halo = Mk.Piv(r.head, 0, 0.25f, -0.34f);
            Mk.Msh(r.halo, RING(0.62f, 0.032f, 80, 10), gold); Mk.Msh(r.halo, RING(0.5f, 0.016f, 80, 6), gold); Mk.Msh(r.halo, RING(0.56f, 0.008f, 80, 4), featherGlow);
            var RAY = LATHE("sray", new[] { 0, 0.03f, 0.04f, 0.035f, 0.3f, 0.0f }, 10);
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2, b = a + Mathf.PI / 12;
                MshS(r.halo, RAY, gold, Mathf.Cos(a) * 0.64f, Mathf.Sin(a) * 0.64f, 0, 0, 0, a - Mathf.PI / 2, 1, i % 3 != 0 ? 0.7f : 1.15f, 1);
                Mk.Msh(r.halo, SP(0.028f, 10, 8), white, Mathf.Cos(b) * 0.56f, Mathf.Sin(b) * 0.56f, 0);
            }
            Gl(r.halo, 0xffe0a0, 2.4f, 0.35f);
            // arms: porcelain with gold bracers, flowing sleeves and slender hands
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var sh = Mk.Piv(r.torso, 0.31f * sx, 0.46f, 0);
                Mk.Msh(sh, SP(0.075f, 16, 12), gold);
                Mk.Msh(sh, LIMB(0.065f, 0.055f, 0.44f), porcelain);
                Mk.Msh(sh, LATHE("ssleeve", new[] { 0.0f, 0.08f, -0.18f, 0.11f, -0.36f, 0.16f, -0.4f, 0.17f }, 20), cloth, 0, -0.02f, 0);
                var el = Mk.Piv(sh, 0, -0.44f, 0);
                Mk.Msh(el, LIMB(0.055f, 0.042f, 0.42f), porcelain);
                Mk.Msh(el, LATHE("sbracer", new[] { 0, 0.064f, -0.2f, 0.058f, -0.3f, 0.052f }, 20), gold, 0, -0.08f, 0);
                MshS(el, SP(0.05f, 14, 10), porcelain, 0, -0.47f, 0.01f, 0, 0, 0, 0.75f, 1.15f, 0.5f);
                for (int k = 0; k < 4; k++) Mk.Msh(el, LIMB(0.011f, 0.008f, 0.08f), porcelain, (k - 1.5f) * 0.018f, -0.51f, 0.012f);
                r.sh[i] = sh; r.el[i] = el;
            }
            // the lance: gold shaft, a blade of pure light with a glowing rim, swept golden wings at the guard
            r.spear = Mk.Piv(r.torso, 0.5f, 0.0f, 0.25f);
            Mk.Msh(r.spear, LIMB(0.03f, 0.03f, 3.0f), gold, 0, 0, -1.5f, -Mathf.PI / 2);
            foreach (var z in new[] { -0.35f, 0, 0.35f }) Mk.Msh(r.spear, RING(0.036f, 0.011f, 20, 6), gold, 0, 0, z);
            var HEAD = BLADE("sspearhead", () => new GeoX.Shape().MoveTo(0, 0).BezierTo(0.16f, 0.12f, 0.13f, 0.5f, 0, 0.85f).BezierTo(-0.13f, 0.5f, -0.16f, 0.12f, 0, 0), 0.025f, 0.014f);
            Mk.Msh(r.spear, HEAD, white, 0, 0, 1.5f, Mathf.PI / 2);
            MshS(r.spear, HEAD, featherGlow, 0, 0, 1.47f, Mathf.PI / 2, 0, 0, 1.18f, 1.08f, 0.6f);
            foreach (var s in new[] { -1f, 1f }) Mk.Msh(r.spear, SP(0.035f, 12, 10), gold, 0.08f * s, 0, 1.47f);
            Mk.Msh(r.spear, RING(0.06f, 0.016f, 24, 6), gold, 0, 0, 1.47f);
            Mk.Msh(r.spear, SP(0.065f, 14, 10), gold, 0, 0, -1.52f);
            Mk.Msh(r.spear, LATHE("spommel", new[] { 0, 0.04f, 0.14f, 0.0f }, 10), gold, 0, 0, -1.55f, -Mathf.PI / 2);
            Gl(r.spear, 0xc0f8ff, 0.9f, 0.8f).transform.localPosition = V(0, 0, 2.35f);
            // six great wings: long primaries edged in light, a layer of coverts and a golden leading edge
            foreach (var s in new[] { -1f, 1f }) for (int w = 0; w < 3; w++)
                {
                    var wing = Mk.Piv(r.torso, 0.15f * s, 0.38f, -0.18f);
                    float sc = new[] { 1f, 0.9f, 0.76f }[w];
                    FeatherFan(wing, s, featherM, featherGlow, 8, r3(1.75f * sc), r3(0.12f * sc), 0.3f, 0.15f, r3(0.17f * sc), r3(0.075f * sc), 0);
                    FeatherFan(wing, s, featherM, null, 6, r3(0.8f * sc), r3(0.07f * sc), 0.42f, 0.2f, r3(0.15f * sc), r3(0.06f * sc), 0.04f);
                    Mk.Msh(wing, LIMB(0.03f, 0.012f, r3(0.62f * sc)), gold, 0.03f * s, 0, 0.05f, 0, 0, s * (Mathf.PI / 2 + 0.15f));
                    r.wings.Add(wing); r.wingS.Add(s); r.wingBase.Add(0.35f - w * 0.5f);
                }
            // three orbiting motes of light, each in a small gold ring
            r.orbit = Mk.Piv(r.root, 0, 0.6f, 0);
            for (int i = 0; i < 3; i++)
            {
                float a = i / 3f * Mathf.PI * 2, x = Mathf.Cos(a) * 1.25f, z = Mathf.Sin(a) * 1.25f;
                Mk.Msh(r.orbit, SP(0.09f, 16, 12), white, x, 0, z); Mk.Msh(r.orbit, RING(0.14f, 0.012f, 28, 6), gold, x, 0, z, 0, a, 0);
                Gl(r.orbit, 0x80e8ff, 0.6f, 0.8f).transform.localPosition = V(x, 0, z);
            }
            Mk.Head(heads.ToArray());
            r.mats.Add(porcelain); r.mats.Add(gold); r.mats.Add(cloth); r.mats.Add(featherM); r.mats.Add(plateM);
            SeraphParts(e, r);
            e.mats.Clear(); e.mats.AddRange(r.mats);
            return r;
        }

        Ent SpawnCarnifex()
        {
            var e = NewEnt("boss"); var sp = LEVEL.bossSpawn;
            e.cr = BuildCarnifex(e);
            e.g.transform.localScale = V(2.4f, 2.4f, 2.4f);
            var near = NearestPlayer(V(sp.x, 0, sp.y)); e.pos = V(sp.x, 0, sp.y); e.yaw = Mathf.Atan2(near.pos.x - sp.x, near.pos.z - sp.y);
            e.cr.root.py = -6;
            e.boss = 1; e.name = "CARNIFEX, THE FLESH TITAN"; e.heavy = true; e.s = 2.4f;
            e.hp = e.maxHp = 260 * CoopBossHp; e.headY = 5.9f; e.bodyY = 4.0f; e.hitR = 2.8f; e.r = 1.3f; e.h = 6;
            e.state = "intro"; e.stateT = 2.8f; e.invuln = true;
            FinishEnt(e); SyncEnt(e); boss = e;
            ShowBossBar(e); audioMgr.Start(1); Sfx("roar"); return e;
        }
        Ent SpawnSeraph()
        {
            var e = NewEnt("boss");
            e.sr = BuildSeraph(e);
            e.g.transform.localScale = V(2.2f, 2.2f, 2.2f); e.pos = V(0, 45, -20);
            e.boss = 2; e.name = "SERAPH OF RUIN"; e.heavy = true; e.s = 2.2f;
            e.hp = e.maxHp = 430 * CoopBossHp; /* (430: a touch longer than 380, still in step with the other bosses) */ e.headY = 3.2f; e.bodyY = 2.0f; e.hitR = 2.4f;
            e.state = "intro"; e.stateT = 3.2f; e.invuln = true; e.orbit = rand * 6;
            FinishEnt(e); SyncEnt(e); boss = e;
            introPillar.SetActive(true); introPillar.transform.position = V(0, 45, -20); Mats.SetAlpha(introPillarMat, 0.6f);
            ShowBossBar(e); audioMgr.Start(2); Sfx("holy"); return e;
        }

        // ---------- boss death ----------
        class BossDeath
        {
            public Ent e; public float t, next, ringT, novaT; public Hit info; public Vector3 basePos; public int wingIdx; public bool haloGone, imploded, nova; public Color bg0;
            public List<GodRay> beams = new List<GodRay>();
        }
        class GodRay { public Transform m; public Material mat; public Vector3 dir; public float len, w, spin; }
        BossDeath bossDeath;
        void KillBoss(Ent e, Hit info)
        {
            e.dead = true; boss = null; // removed from the enemy list, but its body stays for the death sequence
            Mk.StripColliders(e.g);
            ClearHazards();
            foreach (var p in projectiles) if (!p.friendly) p.life = 0.0001f;
            foreach (var o in enemies.ToArray()) if (!o.dead && o != e) KillEnemy(o, new Hit()); // its minions die with it
            HideBossBar();
            AddStyle(e.boss == 1 ? "TITAN FELLED" : e.boss == 3 ? "TRAITOR SHATTERED" : e.boss == 4 ? "KING DETHRONED" : e.boss == 5 ? "TIME'S UP" : "SERAPH SLAIN", 1500);
            Heal(100); freeze = 0.35f; whiteFlash = 0.7f; P.shake = 1; slowmoT = e.boss == 1 ? 1.6f : e.boss == 3 ? 2.2f : 5;
            audioMgr.Stop(e.boss == 2 ? 3.5f : 2);
            if (e.boss == 2) phase = "done";
            bossDeath = new BossDeath { e = e, info = info, basePos = e.pos, bg0 = cam.backgroundColor };
            Sfx(e.boss >= 3 ? "roar" : "scream" + e.boss);
            teleLine.SetActive(false); beamCore.SetActive(false); beamOuter.SetActive(false); beamHit.SetActive(false);
            TryBossCine(e.boss, 2, e); // its defeat, as a cinematic (GameBossCine.cs)
        }
        void FirePillar(Vector3 pos, float r, float h, int color, float life = 1.2f)
        {
            var mat = Mats.Add(color, 1, null, true, false);
            var m = Mk.Msh(world, Geo.Cyl(1, 1, 1, 16), mat, pos.x, pos.y + h / 2, pos.z); m.layer = Mk.LayerIgnore;
            m.transform.localScale = V(r, h, r);
            fx.Add(new Fx { m = m.transform, mat = mat, life = life, max = life, pillar = true, r = r });
        }
        void ShatterGroup(N group, float speed)
        {
            foreach (Transform child in new List<Transform>(group.t.Cast()))
            {
                if (Mk.billboards.Contains(child)) { Destroy(child.gameObject); continue; }
                child.SetParent(world, true);
                particles.Add(new Pt { m = child, v = V(rand - 0.5f, rand * 0.8f, rand - 0.5f) * speed, life = 3 + rand, grav = GRAV * 0.4f, hasSpin = true, spin = V(rand * 16 - 8, rand * 16 - 8, rand * 16 - 8), owned = true });
            }
            Destroy(group.go);
        }
        void SpawnGodRay(BossDeath d)
        {
            var dir = V(rand - 0.5f, rand - 0.15f, rand - 0.5f).normalized;
            var mat = Mats.Add(rand < 0.5f ? 0xffffff : 0xfff0b0, 0.75f, null, true, true);
            var m = Mk.Msh(world, Geo.Cyl(1, 0.15f, 1, 8), mat); m.layer = Mk.LayerIgnore;
            d.beams.Add(new GodRay { m = m.transform, mat = mat, dir = dir, w = 0.6f + rand * 1.4f, spin = (rand - 0.5f) * 0.6f });
            Sfx("godRay" + Random.Range(0, 4));
        }
        readonly List<(Transform t, float len, float a)> carnCracks = new List<(Transform, float, float)>(); Material carnCrackM;
        struct Chunk { public Transform t; public float delay; public bool loose, landed; public Vector3 v, spin; }
        readonly List<Chunk> carnChunks = new List<Chunk>();
        void ClearBossDeath()
        {
            foreach (var cr in carnCracks) if (cr.t) Mk.DestroyTree(cr.t.gameObject); carnCracks.Clear();
            foreach (var ch in carnChunks) if (ch.t) Destroy(ch.t.gameObject); carnChunks.Clear();
            if (bossDeath == null) return;
            foreach (var b in bossDeath.beams) Mk.DestroyTree(b.m.gameObject);
            Destroy(bossDeath.e.g); bossDeath = null;
        }
        void UpdateBossDeath(float dt)
        {
            var d = bossDeath; if (d == null) return;
            var e = d.e; float k = 1 - Mathf.Exp(-8 * dt);
            d.t += dt;
            if (e.boss == 3) { UpdateTraitorDeath(d, dt); return; }
            if (e.boss == 4) { UpdateHollowDeath(d, dt); return; }
            if (e.boss == 5) { UpdateWardenDeath(d, dt); return; }
            if (e.boss == 1)
            {
                // CARNIFEX: falls to its knees roaring as the floor cracks open beneath it, magma bursts out of its back,
                // it cools into blackened stone with glowing seams... then crumbles, and a lava geyser erupts where it knelt
                var r = e.cr; float TT = d.t;
                if (carnCracks.Count == 0)
                {
                    var cm = Mats.Add(0xff6010, 0.95f, null, true, true); carnCrackM = cm;
                    for (int i = 0; i < 14; i++)
                    {
                        float a = i / 14f * Mathf.PI * 2 + rand * 0.3f, len = 7 + rand * 9;
                        var g = Mk.Msh(world, Geo.Box(0.35f + rand * 0.3f, 0.04f, 1), cm, d.basePos.x, d.basePos.y + 0.03f, d.basePos.z); g.layer = Mk.LayerIgnore;
                        g.transform.rotation = Quaternion.Euler(0, a * Mathf.Rad2Deg, 0); carnCracks.Add((g.transform, len, a));
                    }
                }
                float kneel = Smoother(TT / 1.0f), stone = Smoother((TT - 1.6f) / 1.6f);
                float shake = TT < 3.2f ? 0.05f + TT * 0.03f : 0;
                e.pos = V(d.basePos.x + (rand - 0.5f) * shake, d.basePos.y - 1.1f * kneel, d.basePos.z + (rand - 0.5f) * shake); SyncEnt(e);
                Sm(r.torso, 0, 0.35f, k); Sm(r.head, 0, -0.75f, k); Sm(r.jaw, 0, TT < 3.2f ? 1.0f + Mathf.Sin(TT * 30) * 0.1f : 0.5f, k); // head thrown back, roaring
                Sm(r.rSh, 2, 1.1f, k); Sm(r.lSh, 2, -1.1f, k); Sm(r.rSh, 0, -1.2f, k); Sm(r.lSh, 0, -1.2f, k);                    // arms flung up and out
                foreach (var m in r.mats)
                {
                    Mats.SetColor(m, Color.Lerp(Color.white, Mats.Hex(0x1a1410), stone));                                          // cooling to black rock
                    Mats.Emissive(m, TT < 3.2f ? (Mathf.Sin(TT * 14) > 0 ? 0xff5a10 : 0x6a1800) : 0x401000);                         // glowing seams pulse, then dim
                }
                float cg = TT < 3.2f ? 1.2f + TT * 1.6f : Mathf.Max(0.01f, 6 - (TT - 3.2f) * 12); r.coreGlow.transform.localScale = V(cg, cg, cg);
                foreach (var cr in carnCracks) { float L = cr.len * Smoother((TT - 0.2f) / 2.4f); cr.t.localScale = V(1, 1, Mathf.Max(0.01f, L)); cr.t.position = d.basePos + V(Mathf.Sin(cr.a) * L / 2, 0.03f, Mathf.Cos(cr.a) * L / 2); }
                Mats.SetAlpha(carnCrackM, 0.6f + 0.35f * Mathf.Sin(TT * 9));
                d.next -= dt;
                if (TT < 3.2f && d.next <= 0)
                {
                    // magma jets bursting out of its back and shoulders
                    d.next = Mathf.Max(0.07f, 0.22f - TT * 0.05f);
                    var p = e.pos + V((rand - 0.5f) * 2.8f, 3 + rand * 3.5f, (rand - 0.5f) * 2.8f);
                    FirePillar(p, 0.35f + rand * 0.3f, 4 + rand * 5, rand < 0.5f ? 0xff5010 : 0xffb030, 0.5f);
                    SpawnParticles(p, 12, 10, emberMat, 0.13f, 1.4f, 4); SpawnParticles(p, 4, 6, bloodMat, 0.12f, 1);
                    var fc = carnCracks[Random.Range(0, carnCracks.Count)]; float u = rand * fc.len * Smoother((TT - 0.2f) / 2.4f);
                    SpawnParticles(d.basePos + V(Mathf.Sin(fc.a) * u, 0.2f, Mathf.Cos(fc.a) * u), 3, 5, emberMat, 0.1f, 0.8f, -6); // the cracks spit
                    Sfx("boomSmall"); P.shake = Mathf.Max(P.shake, 0.35f);
                }
                if (TT >= 3.2f && TT - dt < 3.2f) { Sfx("slam"); freeze = 0.1f; P.shake = 0.6f; SpawnParticles(e.pos + V(0, 4, 0), 30, 4, ashMat, 0.25f, 2.5f, -1); } // turns to stone
                // the stone statue crumbles from the top down: pieces crack off one by one, drop and break into rubble,
                // dust pouring off it as what's left sags into a heap
                const float CR0 = 3.7f, CRLEN = 1.9f;
                if (TT >= CR0 && TT - dt < CR0)
                {
                    xrayOn.Remove(e); StripXRay(e.g);
                    var rs = e.g.GetComponentsInChildren<MeshRenderer>();
                    float y0 = float.MaxValue, y1 = float.MinValue;
                    foreach (var mr in rs) { y0 = Mathf.Min(y0, mr.bounds.center.y); y1 = Mathf.Max(y1, mr.bounds.center.y); }
                    foreach (var mr in rs)
                    {
                        if (mr.bounds.size.magnitude > 9 || mr.transform == r.coreGlow.transform) continue; // glows and huge fx quads stay with the body
                        float h = Mathf.InverseLerp(y1, y0, mr.bounds.center.y);
                        carnChunks.Add(new Chunk { t = mr.transform, delay = h * CRLEN * 0.8f + rand * 0.15f });
                    }
                    foreach (var m in r.mats) Mats.Emissive(m, 0x120400);
                    Sfx("slam");
                }
                if (TT >= CR0)
                {
                    float u = TT - CR0, floorY = d.basePos.y + 0.15f;
                    e.pos = V(d.basePos.x, d.basePos.y - 1.1f - 1.6f * Smoother(u / CRLEN), d.basePos.z); SyncEnt(e); // what's left sags down
                    r.coreGlow.transform.localScale = Vector3.one * 0.01f;
                    for (int i = 0; i < carnChunks.Count; i++)
                    {
                        var ch = carnChunks[i]; if (!ch.t) continue;
                        if (!ch.loose)
                        {
                            if (u < ch.delay) { if (rand < 0.02f) SpawnParticles(ch.t.position, 2, 1.5f, ashMat, 0.15f, 1.2f, -3); continue; } // trickling dust before it goes
                            // crack off: detach, shed grit, and fall
                            var p0 = ch.t.position; ch.t.SetParent(world, true); ch.loose = true;
                            var outv = p0 - d.basePos; outv.y = 0; outv = outv.normalized;
                            ch.v = outv * (0.8f + rand * 2.2f) + V((rand - 0.5f) * 1.5f, 0.5f + rand * 1.5f, (rand - 0.5f) * 1.5f);
                            ch.spin = V(rand - 0.5f, rand - 0.5f, rand - 0.5f) * 260;
                            SpawnParticles(p0, 8, 2.5f, ashMat, 0.22f, 1.6f, -2); SpawnParticles(p0, 5, 4, M.tower, 0.22f, 1.4f);
                            if (rand < 0.35f) Sfx("boomSmall");
                        }
                        else if (!ch.landed)
                        {
                            ch.v.y -= 24 * dt; ch.t.position += ch.v * dt; ch.t.Rotate(ch.spin * dt, Space.World);
                            if (ch.t.position.y <= floorY)
                            {
                                // it shatters on impact: a puff of dust, grit spraying out, and it lies there as rubble
                                var p = ch.t.position; p.y = floorY; ch.t.position = p; ch.landed = true;
                                ch.t.localScale *= 0.75f;
                                SpawnParticles(p, 10, 4, ashMat, 0.28f, 1.8f, -1); SpawnParticles(p, 6, 6, M.tower, 0.2f, 1.2f);
                                P.shake = Mathf.Max(P.shake, 0.2f);
                            }
                        }
                        carnChunks[i] = ch;
                    }
                    if (rand < 0.5f) SpawnParticles(e.pos + V((rand - 0.5f) * 3, 2 + rand * 4, (rand - 0.5f) * 3), 2, 1.2f, ashMat, 0.3f, 2.2f, -1.5f); // dust pouring off
                }
                if (TT > CR0 + CRLEN + 0.5f)
                {
                    var c = d.basePos + V(0, 2, 0);
                    // the heap of rubble is all that's left... then the ground erupts beneath it
                    Destroy(e.g);
                    FirePillar(d.basePos, 6, 110, 0xff5010, 2.2f); FirePillar(d.basePos, 3, 130, 0xffe080, 1.6f);
                    Blast(c, 14, 0xff6020);
                    for (int i = 0; i < 3; i++) ShockRing(d.basePos, 14 + i * 9, i > 0 ? 0xff7a20 : 0xffd060, 0.7f + i * 0.3f);
                    SpawnParticles(c, 80, 6, ashMat, 0.3f, 3, -1); SpawnParticles(d.basePos + V(0, 1, 0), 90, 26, emberMat, 0.18f, 3, 6);
                    SpawnParticles(c, 40, 12, M.tower, 0.4f, 3);
                    foreach (var cr in carnCracks) if (cr.t) Mk.DestroyTree(cr.t.gameObject); carnCracks.Clear();
                    Sfx("bossDeath"); Sfx("boom"); whiteFlash = 0.5f; P.shake = 1.5f; freeze = 0.15f; slowmoT = Mathf.Max(slowmoT, 1.2f);
                    Banner("TITAN FELLED", "", "#ffd24a");
                    bossDeath = null;
                }
                return;
            }            // SERAPH: rises screaming as god-rays tear out of it, its halo shatters, its wings rip away,
            // the sky bleeds, everything collapses inward... and it goes supernova.
            var sr = e.sr;
            // after the supernova only the white-out remains: hand over to the ending (nothing below may stop this)
            if (d.nova) { whiteFlash = 1; d.novaT += dt; if (d.novaT > 0.9f && !BCHoldsNova) { ClearBossDeath(); StartCutscene(); } return; } // (her story holds the white: GameBossCine)
            float T = d.t, jit = Mathf.Min(0.5f, T * 0.08f);
            e.pos = V(d.basePos.x + (rand - 0.5f) * jit, d.basePos.y + T * 1.4f, d.basePos.z + (rand - 0.5f) * jit);
            e.pitch *= 0.9f; SyncEnt(e);
            Sm(sr.sh[0], 2, -1.5f, k); Sm(sr.sh[1], 2, 1.5f, k); Sm(sr.sh[0], 0, -0.3f, k); Sm(sr.sh[1], 0, -0.3f, k);
            Sm(sr.head, 0, -0.8f, k); Sm(sr.torso, 0, -0.35f, k); Sm(sr.spear, 0, -1.6f, k);
            for (int i = d.wingIdx; i < sr.wings.Count; i++) Sm(sr.wings[i], 2, sr.wingS[i] * (sr.wingBase[i] + 0.6f + Mathf.Sin(T * 30) * 0.08f), k);
            if (!d.haloGone && sr.halo.t) sr.halo.rz += dt * (8 + T * 14);
            if (sr.orbit.t) sr.orbit.ry += dt * 12;
            foreach (var m in sr.mats) Mats.Emissive(m, Mathf.Sin(T * 40) > 0 ? 0xffffff : 0x8090a0);
            float es = 2 + T * 3, gs = 1 + T * 4;
            if (sr.eyeGlow) sr.eyeGlow.transform.localScale = V(es, es, es);
            if (sr.gemGlow) sr.gemGlow.transform.localScale = V(gs, gs, gs);
            float sky = clamp(T / 4.2f, 0, 1);
            var bgc = Color.Lerp(d.bg0, Mats.Hex(0x2a0012), sky); cam.backgroundColor = bgc; RenderSettings.fogColor = bgc;
            var origin = BodyPoint(e);
            d.next -= dt;
            if (d.next <= 0 && T < 4.7f) { d.next = Mathf.Max(0.08f, 0.3f - T * 0.05f); SpawnGodRay(d); whiteFlash = Mathf.Max(whiteFlash, 0.18f); }
            foreach (var b in d.beams)
            {
                b.dir = Quaternion.AngleAxis(b.spin * dt * Mathf.Rad2Deg, UP) * b.dir;
                b.len = d.imploded ? b.len * Mathf.Exp(-6 * dt) : Mathf.Min(90, b.len + dt * 260);
                float w = d.imploded ? b.w * Mathf.Max(0, 1 - (T - 4.7f) * 2) : b.w * (1 + Mathf.Sin(T * 50 + b.spin * 10) * 0.2f);
                b.m.position = origin + b.dir * (b.len / 2);
                b.m.rotation = Quaternion.FromToRotation(UP, b.dir); b.m.localScale = V(Mathf.Max(0.01f, w), Mathf.Max(0.01f, b.len), Mathf.Max(0.01f, w));
            }
            d.ringT -= dt;
            if (d.ringT <= 0 && T < 4.7f) { d.ringT = 0.55f; ShockRing(V(origin.x, origin.y - 1, origin.z), 26 + T * 4, rand < 0.5f ? 0xffe8a0 : 0x80e8ff, 0.9f); Sfx("boomSmall"); }
            if (!d.haloGone && T > 1.0f) { d.haloGone = true; ShatterGroup(sr.haloFrag != null && sr.haloFrag.go.activeSelf ? sr.haloFrag : sr.halo, 16); Sfx("shatter"); whiteFlash = Mathf.Max(whiteFlash, 0.5f); SpawnParticles(origin, 30, 14, goldSpark, 0.12f, 1.5f); }
            if (T > 1.7f && d.wingIdx < sr.wings.Count && T > 1.7f + d.wingIdx * 0.32f)
            {
                int wi = d.wingIdx++; var w = sr.wings[wi]; float s = sr.wingS[wi];
                w.t.SetParent(world, true);
                particles.Add(new Pt { m = w.t, v = V(s * (10 + rand * 8), 4 + rand * 8, (rand - 0.5f) * 8), life = 3.5f, grav = GRAV * 0.5f, hasSpin = true, spin = V(rand * 8 - 4, rand * 8 - 4, s * (6 + rand * 6)), owned = true });
                SpawnParticles(w.t.position, 18, 10, cyanMat, 0.12f, 1.2f); Sfx("shatter"); P.shake = Mathf.Max(P.shake, 0.6f);
            }
            P.shake = Mathf.Max(P.shake, 0.2f + T * 0.12f);
            if (T > 4.7f && !d.imploded) { d.imploded = true; Sfx("implode"); }
            if (d.imploded && !d.nova && sr.eyeGlow && sr.gemGlow)
            {
                float a = 6 + (T - 4.7f) * 40, b2 = 6 + (T - 4.7f) * 50;
                sr.eyeGlow.transform.localScale = V(a, a, a); sr.gemGlow.transform.localScale = V(b2, b2, b2);
            }
            if (T > 5.3f && !d.nova)
            {
                d.nova = true;
                Dismember(new List<N> { sr.spear, sr.head, sr.torso }, e, d.info, 30);
                e.g.SetActive(false);
                foreach (var b in d.beams) Mk.DestroyTree(b.m.gameObject);
                d.beams.Clear();
                Blast(origin, 160, 0xffffff); Blast(origin, 60, 0xfff0c0);
                for (int i = 0; i < 4; i++) ShockRing(V(origin.x, Mathf.Max(0, origin.y - 6), origin.z), 40 + i * 25, i % 2 == 1 ? 0x80e8ff : 0xffffff, 1 + i * 0.3f);
                FirePillar(V(origin.x, 0, origin.z), 6, 220, 0xffffff, 2); FirePillar(V(origin.x, 0, origin.z), 14, 220, 0xfff0b0, 2.4f);
                SpawnParticles(origin, 80, 30, goldSpark, 0.2f, 3); SpawnParticles(origin, 60, 26, cyanMat, 0.15f, 2.5f);
                Sfx("nova"); P.shake = 2; freeze = 0.2f;
            }
        }

        // ---------- boss hazards: shock rings and ground spikes ----------
        class Ring { public Vector3 c; public float r, speed, max, dmg, delay; public bool hit; public GameObject mesh; public Material mat; }
        class Spike { public float x, y, z, t, dmg = 25; public int st; public bool ice; public GameObject mesh, warn; }
        Material iceSpikeMat, iceWarnMat;
        readonly List<Ring> rings = new List<Ring>();
        readonly List<Spike> spikes = new List<Spike>();
        void AddRing(Vector3 c, float speed, float max, float dmg, int color, float delay = 0)
        {
            var mat = Mats.Add(color, 0.8f, null, true, true);
            var mesh = Mk.Msh(world, wallRingGeo, mat, c.x, c.y + 0.45f, c.z); mesh.layer = Mk.LayerIgnore;
            mesh.transform.localScale = V(0.5f, 0.9f, 0.5f); mesh.SetActive(delay <= 0);
            rings.Add(new Ring { c = c, r = 0.5f, speed = speed, max = max, dmg = dmg, mesh = mesh, mat = mat, delay = delay });
        }
        void AddSpike(float x, float y, float z, float delay, bool ice = false, float dmg = 25)
        {
            if (ice && !iceSpikeMat) { iceSpikeMat = G(Mats.Std(0xc8ecff, 0.12f, 0.1f, 0x0a3048)); iceWarnMat = G(Mats.Trans(0x80e0ff, 0.75f, true)); }
            spikes.Add(new Spike { x = x, y = y, z = z, t = delay, ice = ice, dmg = dmg });
        }
        void ClearHazards()
        {
            foreach (var r in rings) Mk.DestroyTree(r.mesh);
            foreach (var s in spikes) { if (s.mesh) Destroy(s.mesh); if (s.warn) Destroy(s.warn); }
            rings.Clear(); spikes.Clear();
        }
        void UpdateHazards(float dt)
        {
            foreach (var r in rings)
            {
                if (r.delay > 0) { r.delay -= dt; if (r.delay <= 0) r.mesh.SetActive(true); continue; }
                r.r += r.speed * dt;
                r.mesh.transform.localScale = V(r.r, 0.9f, r.r); Mats.SetAlpha(r.mat, 0.85f * (1 - r.r / r.max) + 0.15f);
                if (!r.hit && !P.dead && r.dmg > 0) // (a purely visual ring - e.g. a boss landing - never throws you)
                {
                    float d = Hypot(P.pos.x - r.c.x, P.pos.z - r.c.z), dy = P.pos.y - r.c.y;
                    if (Mathf.Abs(d - r.r) < 1.0f && dy < 0.9f && dy > -1.5f) { r.hit = true; DamagePlayer(r.dmg); P.vel.y = Mathf.Max(P.vel.y, 9); P.onGround = false; }
                }
            }
            rings.RemoveAll(r => { if (r.r >= r.max) { Mk.DestroyTree(r.mesh); return true; } return false; });
            foreach (var s in spikes)
            {
                s.t -= dt;
                if (s.st == 0 && s.t <= 0)
                {
                    s.st = 1; s.t = 0.45f;
                    s.warn = Mk.Msh(world, ringGeo, s.ice ? iceWarnMat : spikeWarnMat, s.x, s.y + 0.06f, s.z, -Mathf.PI / 2); s.warn.layer = Mk.LayerIgnore; s.warn.transform.localScale = V(1.1f, 1.1f, 1.1f);
                }
                else if (s.st == 1 && s.t <= 0)
                {
                    s.st = 2; s.t = 0.1f; Destroy(s.warn); s.warn = null;
                    s.mesh = Mk.Msh(world, Geo.Cone(0.5f, 2.8f, 6), s.ice ? iceSpikeMat : spikeMat, s.x, s.y - 1.6f, s.z, 0, rand * 3); s.mesh.layer = Mk.LayerIgnore;
                    Sfx("spike"); SpawnParticles(V(s.x, s.y + 0.2f, s.z), 4, 5, dustMat, 0.15f, 0.4f);
                    float d = Hypot(P.pos.x - s.x, P.pos.z - s.z), dy = P.pos.y - s.y;
                    if (s.dmg > 0 && d < 1.2f && dy > -0.5f && dy < 2.4f) { DamagePlayer(s.dmg); P.vel.y = 13; P.onGround = false; if (s.ice) chillT = Mathf.Max(chillT, 1.2f); }
                }
                else if (s.st == 2)
                {
                    var p = s.mesh.transform.localPosition; p.y = s.y - 1.6f + 2.7f * clamp(1 - s.t / 0.1f, 0, 1); s.mesh.transform.localPosition = p;
                    if (s.t <= 0) { s.st = 3; s.t = 0.7f; }
                }
                else if (s.st == 3 && s.t <= 0) { s.st = 4; s.t = 0.3f; }
                else if (s.st == 4)
                {
                    var p = s.mesh.transform.localPosition; p.y = s.y + 1.1f - 2.7f * clamp(1 - s.t / 0.3f, 0, 1); s.mesh.transform.localPosition = p;
                    if (s.t <= 0) { Destroy(s.mesh); s.st = 5; }
                }
            }
            spikes.RemoveAll(s => s.st == 5);
        }

        // ---------- boss 1 AI ----------
        static void CfxState(Ent e, string s, float t) { e.state = s; e.stateT = t; e.vidx = 0; e.hitPlayer = false; if (s == "chase") e.chained = false; }
        void PickCarnifexAttack(Ent e, float dist, float dy)
        {
            string[] opts;
            if (dy > 2.5f) opts = new[] { "leap", "leap", "bile" };
            else if (dist < 10) opts = new[] { "slam", "slam", "stomp", "leap" };
            else if (dist > 30) opts = new[] { "charge", "bile", "burrow", "burrow", "leap" };
            else opts = new[] { "charge", "charge", "bile", "stomp", "leap", "burrow" };
            var pick = opts[Mathf.Min(opts.Length - 1, (int)(rand * opts.Length))];
            if (pick == e.last) { foreach (var o in opts) if (o != e.last) { pick = o; break; } }
            e.last = pick;
            if (pick == "slam") { CfxState(e, "slamWind", 0.95f); Sfx("growlLow"); }
            else if (pick == "charge") { CfxState(e, "chargeWind", 1.0f); Sfx("roarShort"); }
            else if (pick == "bile") CfxState(e, "bile", 1.6f);
            else if (pick == "stomp") CfxState(e, "stompWind", 0.8f);
            else if (pick == "burrow") { CfxState(e, "burrow", 0.9f); Sfx("growlLow"); }
            else { CfxState(e, "leapWind", 0.6f); Sfx("growlLow"); }
        }
        Vector3 Fwd(Ent e) => V(Mathf.Sin(e.yaw), 0, Mathf.Cos(e.yaw));
        void CarnifexSlam(Ent e)
        {
            var p = e.pos + Fwd(e) * 4.8f;
            AddRing(p, e.phase2 ? 28 : 24, 36, 20, 0xff6020);
            if (e.phase2) AddRing(p, 20, 30, 20, 0xffaa40, 0.35f);
            if (Hypot(P.pos.x - p.x, P.pos.z - p.z) < 3.8f && Mathf.Abs(P.pos.y - e.pos.y) < 3) DamagePlayer(40);
            P.shake = Mathf.Max(P.shake, 0.7f); Sfx("slam"); ShockRing(p, 5, 0xffffff, 0.3f);
            SpawnParticles(V(p.x, p.y + 0.3f, p.z), 30, 11, dustMat, 0.25f, 0.8f);
        }
        void FireBile(Ent e)
        {
            var m = e.cr.jaw.t.position;
            int n = e.phase2 ? 5 : 3; float T = 1.1f; var side = V(-Mathf.Cos(e.yaw), 0, Mathf.Sin(e.yaw));
            for (int k = 0; k < n; k++)
            {
                var tgt = Tgt.pos + Tgt.vel * (T * 0.6f) + side * ((k - (n - 1) / 2f) * 3.2f);
                var v = (tgt - m) / T; v.y += 0.5f * 20 * T;
                MakeProj("bile", m, v, 15, 6, 20, 2.8f);
            }
            Sfx("growlLow"); Sfx("bile");
        }
        void CarnifexSpikes(Ent e)
        {
            float dx = Tgt.pos.x - e.pos.x, dz = Tgt.pos.z - e.pos.z, d = Hypot(dx, dz); if (d == 0) d = 1;
            float bse = Mathf.Atan2(dz / d, dx / d);
            var lines = e.phase2 ? new[] { -0.5f, -0.25f, 0, 0.25f, 0.5f } : new[] { -0.35f, 0, 0.35f };
            foreach (var a in lines)
            {
                var dir = V(Mathf.Cos(bse + a), 0, Mathf.Sin(bse + a));
                for (int k = 1; k <= 26; k++)
                {
                    float x = e.pos.x + dir.x * (3 + k * 1.7f), z = e.pos.z + dir.z * (3 + k * 1.7f);
                    if (Mathf.Abs(x) > LB - 1 || Mathf.Abs(z) > LB - 1) break;
                    if (PointInLevel(V(x, e.pos.y + 0.5f, z)) || !PointInLevel(V(x, e.pos.y - 0.2f, z))) break;
                    AddSpike(x, e.pos.y, z, k * 0.055f);
                }
            }
            P.shake = Mathf.Max(P.shake, 0.5f); Sfx("slam");
        }
        void Enrage(Ent e)
        {
            e.phase2 = true; audioMgr.Intensity();
            Banner("ENRAGED", e.name, "#ff2020"); Sfx("roar");
            P.shake = Mathf.Max(P.shake, 0.8f);
            if (e.boss == 2) { SeraphFallBegin(e); return; } // (the Seraph's fall: a cinematic transformation, the Sanctum corrupted with him - GameSeraph2)
            if (e.boss == 1) CrucibleShatter(); else SanctumOpen(); // the arena itself transforms
            if (e.boss == 1)
            {
                Mats.SetColor(e.cr.glow, 0xffd000);
                AddRing(e.pos, 22, 30, 15, 0xff2000);
                if (Auth) for (int i = 0; i < 4; i++) { var s = FreeSpot(14); SpawnHusk(s.x, s.y, false); } // co-op: minions come from the host
            }
            else
            {
                Mats.SetColor(e.sr.featherGlow, 0xff3040); Mats.SetColor(e.sr.eyeMat, 0xff5060);
                AddRing(V(e.pos.x, Mathf.Max(0, Tgt.pos.y), e.pos.z), 22, 30, 15, 0xff4060);
                if (Auth) for (int i = 0; i < 3; i++) { var s = FreeSpot(14); SpawnDrone(s.x, 10 + rand * 5, s.y); }
            }
        }
        static readonly HashSet<string> NO_ENRAGE = new HashSet<string> { "intro", "charge", "leap", "roar", "burrow", "emerge" };
        void UpdateCarnifex(Ent e, float dt)
        {
            var r = e.cr; float sp = e.phase2 ? 1.35f : 1;
            float dx = Tgt.pos.x - e.pos.x, dz = Tgt.pos.z - e.pos.z, dist = Hypot(dx, dz), dy = Tgt.pos.y - e.pos.y; if (dist == 0) dist = 1;
            e.stateT -= dt * sp; e.flash -= dt;
            bool face = true; float move = 0;
            if (!e.phase2 && e.hp <= e.maxHp * 0.5f && !NO_ENRAGE.Contains(e.state)) { Enrage(e); CfxState(e, "roar", 2.0f); }
            switch (e.state)
            {
                case "intro":
                    {
                        // it bursts up out of the floor in the first 0.75 s (so the intro card shows the whole titan), then roars
                        float t = clamp((2.8f - e.stateT) / 0.75f, 0, 1), ez = 1 - (1 - t) * (1 - t) * (1 - t);
                        r.root.py = -6 * (1 - ez) + Mathf.Sin(t * Mathf.PI) * 0.35f;
                        if (t >= 1 && !e.risen) { e.risen = true; Sfx("slam"); P.shake = Mathf.Max(P.shake, 0.7f); SpawnParticles(e.pos + V(0, 0.2f, 0), 30, 9, dustMat, 0.35f, 0.6f, -6); SpawnParticles(e.pos + V(0, 0.2f, 0), 16, 7, M.floor, 0.3f, 0.6f, -14); }
                        P.shake = Mathf.Max(P.shake, 0.25f);
                        if (!e.risen && rand < 0.6f) SpawnParticles(e.pos + V(rand * 6 - 3, 0.3f, rand * 6 - 3), 2, 6, dustMat, 0.25f, 0.45f, -12); // only while it digs out: after that the legs stay clear
                        if (e.stateT <= 0.8f && !e.roared) { e.roared = true; Sfx("roar"); P.shake = 0.8f; }
                        if (e.stateT <= 0) { e.invuln = false; r.root.py = 0; CfxState(e, "chase", 1.2f); }
                        break;
                    }
                case "chase": move = 6.5f; if (e.stateT <= 0 && Auth) PickCarnifexAttack(e, dist, dy); break; // co-op clients wait for the host's pick
                case "slamWind": if (e.stateT <= 0) { CarnifexSlam(e); CfxState(e, "slamRecover", 0.8f); } break;
                case "slamRecover": face = false; if (e.stateT <= 0) CfxState(e, "chase", 0.8f + rand); break;
                case "chargeWind":
                    if (rand < 0.3f) SpawnParticles(V(e.pos.x, e.pos.y + 0.2f, e.pos.z), 1, 4, dustMat, 0.2f, 0.4f);
                    if (e.stateT <= 0)
                    {
                        e.chargeDir = V(dx / dist, 0, dz / dist);
                        var a = V(e.pos.x, e.pos.y + 2, e.pos.z);
                        if (Los(a, a + e.chargeDir * 9)) { CfxState(e, "charge", 2.4f); e.chargeFrom = e.pos; e.hasChargeFrom = true; }
                        else CfxState(e, "leapWind", 0.3f);
                    }
                    break;
                case "charge":
                    face = false;
                    e.vel.x = e.chargeDir.x * 30 * sp; e.vel.z = e.chargeDir.z * 30 * sp;
                    if (rand < 0.7f) SpawnParticles(V(e.pos.x, e.pos.y + 0.3f, e.pos.z), 2, 5, dustMat, 0.25f, 0.5f);
                    if (!e.hitPlayer && MeIn(e.pos, 3.3f, 5))
                    {
                        e.hitPlayer = true; DamagePlayer(35);
                        P.vel += e.chargeDir * 25; P.vel.y += 10; P.onGround = false; P.coyote = 0;
                    }
                    if (e.stateT <= 0) { if (e.phase2 && !e.chained) { e.chained = true; e.state = "chargeWind"; e.stateT = 0.5f; } else CfxState(e, "chase", 1.2f); }
                    break;
                case "stunned":
                    face = false;
                    if (rand < 0.2f) SpawnParticles(V(e.pos.x, e.pos.y + e.headY + 0.8f, e.pos.z), 1, 2, sparkMat, 0.12f, 0.5f, -2);
                    if (e.stateT <= 0) CfxState(e, "chase", 1);
                    break;
                case "bile":
                    {
                        var th = new[] { 1.2f, 0.8f, 0.4f };
                        if (e.vidx < 3 && e.stateT < th[e.vidx]) { FireBile(e); e.vidx++; }
                        if (e.stateT <= 0) CfxState(e, "chase", 1.2f);
                        break;
                    }
                case "stompWind": if (e.stateT <= 0) { CarnifexSpikes(e); CfxState(e, "stompRecover", 0.7f); } break;
                case "stompRecover": if (e.stateT <= 0) CfxState(e, "chase", 1); break;
                case "leapWind":
                    if (e.stateT <= 0) { BallisticLeap(e, dx, dz, dist, dy, 7, 30); CfxState(e, "leap", 3.5f); e.leftGround = false; e.ground = false; e.leapFrom = e.pos; }
                    break;
                case "leap":
                    face = false;
                    e.vel.x = e.leapVX; e.vel.z = e.leapVZ;
                    if (!e.ground) e.leftGround = true;
                    if (e.ground && e.leftGround && Vector3.Distance(e.pos, e.leapFrom) < 3) { CfxState(e, "burrow", 0.9f); Sfx("growlLow"); }
                    else if (e.ground && e.leftGround)
                    {
                        AddRing(e.pos, 24, 32, 20, 0xff6020);
                        if (e.phase2) AddRing(e.pos, 16, 26, 20, 0xffaa40, 0.3f);
                        if (MeIn(e.pos, 5, 3)) DamagePlayer(35);
                        P.shake = Mathf.Max(P.shake, 0.8f); Sfx("slam"); SpawnParticles(V(e.pos.x, e.pos.y + 0.3f, e.pos.z), 30, 11, dustMat, 0.25f, 0.8f);
                        CfxState(e, "slamRecover", 0.7f);
                    }
                    else if (e.stateT <= 0) CfxState(e, "chase", 1);
                    break;
                case "roar": if (e.stateT <= 0) CfxState(e, "chase", 0.8f); break;
                case "burrow":
                    {
                        e.invuln = true; face = false; e.vel = Vector3.zero;
                        r.root.py = -6 * clamp(1 - e.stateT / 0.9f, 0, 1);
                        if (rand < 0.7f) SpawnParticles(e.pos + V(rand * 5 - 2.5f, 0.3f, rand * 5 - 2.5f), 2, 6, dustMat, 0.25f, 0.6f);
                        P.shake = Mathf.Max(P.shake, 0.2f);
                        if (e.stateT <= 0)
                        {
                            for (int i = 0; i < 40 && Auth; i++) // co-op: where it surfaces is the host's call (clients get it in the snapshot)
                            {
                                float a = rand * Mathf.PI * 2, rr = 6 + rand * 7;
                                float x = clamp(Tgt.pos.x + Mathf.Cos(a) * rr, -(LB - 6), LB - 6), z = clamp(Tgt.pos.z + Mathf.Sin(a) * rr, -(LB - 6), LB - 6);
                                if (InPool(x, z, 2)) continue;
                                bool blocked = false;
                                foreach (var c in colliders) if (Overlaps(x - 1.6f, 0.01f, z - 1.6f, x + 1.6f, 6.5f, z + 1.6f, c)) { blocked = true; break; }
                                if (blocked) continue;
                                e.pos = V(x, 0, z); break;
                            }
                            ShockRing(e.pos, 5, 0xff2000, 0.8f);
                            CfxState(e, "emerge", 1.0f); Sfx("roarShort");
                        }
                        break;
                    }
                case "emerge":
                    {
                        face = true; e.vel = Vector3.zero;
                        r.root.py = -6 * clamp(e.stateT / 1.0f, 0, 1);
                        if (rand < 0.7f) SpawnParticles(e.pos + V(rand * 5 - 2.5f, 0.3f, rand * 5 - 2.5f), 2, 7, dustMat, 0.25f, 0.6f);
                        if (e.stateT <= 0)
                        {
                            r.root.py = 0; e.invuln = false;
                            AddRing(e.pos, 22, 30, 20, 0xff6020); if (e.phase2) CarnifexSpikes(e);
                            if (MeIn(e.pos, 4.5f, 3)) DamagePlayer(30);
                            P.shake = Mathf.Max(P.shake, 0.8f); Sfx("slam");
                            CfxState(e, "chase", 1.0f);
                        }
                        break;
                    }
            }
            if (e.state != "intro" && e.state != "burrow" && e.state != "emerge")
            {
                if (e.state != "charge" && e.state != "leap")
                {
                    float k = 1 - Mathf.Exp(-6 * dt);
                    e.vel.x += (dx / dist * move * sp - e.vel.x) * k; e.vel.z += (dz / dist * move * sp - e.vel.z) * k;
                }
                e.vel.y = Mathf.Max(e.vel.y - GRAV * dt, -60);
                var res = MoveBody(ref e.pos, ref e.vel, e.r, e.h, dt);
                e.ground = res.ground;
                e.blockT = res.wall && e.state == "chase" ? e.blockT + dt : 0;
                if (e.state == "charge" && res.wall)
                {
                    if (e.hasChargeFrom && Vector3.Distance(e.pos, e.chargeFrom) > 4)
                    {
                        CfxState(e, "stunned", 3.3f); e.vel.x = e.vel.z = 0;
                        P.shake = Mathf.Max(P.shake, 0.9f); Sfx("slam"); SpawnParticles(BodyPoint(e), 30, 12, dustMat, 0.3f, 0.8f);
                    }
                    else CfxState(e, "leapWind", 0.3f);
                }
                else if (e.state == "chase" && e.blockT > 0.5f) { e.blockT = 0; CfxState(e, "leapWind", 0.4f); }
            }
            if (face) TurnTo(e, Mathf.Atan2(dx, dz), e.state == "chase" ? 5 : 3, dt);
            else if (e.state == "charge") TurnTo(e, Mathf.Atan2(e.chargeDir.x, e.chargeDir.z), 12, dt);
            e.pos.x = clamp(e.pos.x, -(LB - 2), LB - 2); e.pos.z = clamp(e.pos.z, -(LB - 2), LB - 2);
            AnimateCarnifex(e, dt);
            SyncEnt(e);
        }
        void AnimateCarnifex(Ent e, float dt)
        {
            var r = e.cr; float k = 1 - Mathf.Exp(-12 * dt), hsp = Hypot(e.vel.x, e.vel.z); var st = e.state;
            e.walk += dt * hsp * (st == "charge" ? 0.55f : 0.9f);
            float sw = Mathf.Sin(e.walk), amp = Mathf.Min(1, hsp / 4);
            e.flinch = Mathf.Max(0, e.flinch - dt * 3);
            float Prog(float dur) => 1 - clamp(e.stateT / dur, 0, 1);
            float lean = 0.25f * amp, headX = 0, jaw = 0.1f + Mathf.Sin(time * 6) * 0.05f, rSh = -0.3f + sw * 0.35f * amp, rEl = -0.5f, lSh = 0.2f - sw * 0.35f * amp, lEl = -0.6f, armZ = 0.25f;
            float legL = -sw * 0.6f * amp, legR = sw * 0.6f * amp, kneeL = 0.15f + Mathf.Max(0, sw) * 0.9f * amp, kneeR = 0.15f + Mathf.Max(0, -sw) * 0.9f * amp, hipY = 1.1f - Mathf.Abs(Mathf.Cos(e.walk)) * 0.06f * amp;
            switch (st)
            {
                case "intro": case "roar": case "burrow": case "emerge": lean = -0.35f; headX = -0.5f; jaw = 0.8f + Mathf.Sin(time * 30) * 0.08f; armZ = 1.3f; rSh = -0.6f; lSh = -0.6f; break;
                case "slamWind": { float t = Prog(0.95f); rSh = lerp(-0.3f, -3.1f, t); rEl = -0.9f; lean = lerp(0.2f, -0.35f, t); jaw = 0.5f; lSh = -0.8f; break; }
                case "slamRecover": rSh = -0.5f; rEl = -0.05f; lean = 0.75f; jaw = 0.3f; lSh = -0.2f; break;
                case "chargeWind": lean = 0.6f; headX = 0.3f; jaw = 0.7f + Mathf.Sin(time * 40) * 0.1f; rSh = 0.7f; lSh = 0.7f; legL = Mathf.Sin(time * 18) * 0.4f; legR = -legL; break;
                case "charge": lean = 0.85f; headX = 0.4f; jaw = 0.6f; rSh = 0.9f + sw * 0.3f; lSh = 0.9f - sw * 0.3f; armZ = 0.4f; break;
                case "stunned": lean = 1.0f; headX = 0.7f + Mathf.Sin(time * 3) * 0.2f; jaw = 0.4f; rSh = 0.3f; lSh = 0.3f; rEl = -0.1f; lEl = -0.1f; kneeL = kneeR = 0.6f; hipY = 0.85f; break;
                case "bile": { float t = Prog(1.6f), pulse = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3)); headX = -0.5f + pulse * 0.6f; jaw = 0.9f; lean = -0.1f + 0.3f * pulse; lSh = -1.0f; break; }
                case "stompWind": { float t = Prog(0.8f); legR = lerp(0, -1.3f, t); kneeR = lerp(0.15f, 1.4f, t); lean = -0.2f; armZ = 0.6f; break; }
                case "stompRecover": legR = 0.2f; lean = 0.4f; jaw = 0.5f; break;
                case "leapWind": lean = 0.7f; kneeL = kneeR = 1.2f; legL = legR = -0.9f; hipY = 0.8f; rSh = 0.8f; lSh = 0.8f; break;
                case "leap": lean = 0.2f; legL = legR = -0.6f; kneeL = kneeR = 0.9f; rSh = -2.8f; lSh = -2.8f; rEl = -0.4f; jaw = 0.7f; break;
            }
            Sm(r.torso, 0, lean - e.flinch * 0.3f, k); Sm(r.head, 0, headX - e.flinch * 0.3f, k); Sm(r.jaw, 0, jaw, k);
            Sm(r.rSh, 0, rSh, k); Sm(r.rEl, 0, rEl, k); Sm(r.rSh, 2, armZ, k);
            Sm(r.lSh, 0, lSh, k); Sm(r.lEl, 0, lEl, k); Sm(r.lSh, 2, -armZ, k);
            Sm(r.thigh[0], 0, legL, k); Sm(r.thigh[1], 0, legR, k); Sm(r.knee[0], 0, kneeL, k); Sm(r.knee[1], 0, kneeR, k);
            r.hips.py += (hipY - r.hips.py) * k;
            for (int i = 0; i < r.hooks.Count; i++) r.hooks[i].rx = Mathf.Sin(time * 3 + i * 2) * 0.35f - lean * 0.5f;
            float cg = 1.0f + Mathf.Sin(time * (e.phase2 ? 12 : 5)) * 0.25f; r.coreGlow.transform.localScale = V(cg, cg, cg);
            int em = e.flash > 0 ? 0xffffff : st == "stunned" ? 0x333300 : e.phase2 ? (Mathf.Sin(time * 8) > 0 ? 0x2a0000 : 0x120000) : 0x000000;
            foreach (var m in r.mats) Mats.Emissive(m, em);
        }

        // ---------- boss 2 AI ----------
        static void SrState(Ent e, string s, float t) { e.state = s; e.stateT = t; e.vidx = 0; e.hitPlayer = false; }
        void PickSeraph(Ent e)
        {
            var opts = new List<string> { "dash", "laser", "feathers", "nova" }; opts.Remove(e.last);
            var pick = opts[Mathf.Min(opts.Count - 1, (int)(rand * opts.Count))]; e.last = pick;
            if (pick == "dash") { e.dashes = e.phase2 ? 3 + Mathf.RoundToInt(2 * SeraphFury(e)) : 2; SrState(e, "blinkOut", 0.25f); Sfx("blink"); } // up to 5 dashes in a row near death
            else if (pick == "laser") { SrState(e, "laserWind", 1.1f); Sfx("laserCharge"); }
            else if (pick == "feathers") SrState(e, "feathers", 1.9f);
            else SrState(e, "novaRise", 1.0f);
        }
        void SeraphVolley(Ent e)
        {
            var from = BodyPoint(e); var bdir = (ChestOf(Tgt) - from).normalized;
            int n = e.phase2 ? 11 : 8; float spread = 1.0f;
            for (int i = 0; i < n; i++)
            {
                var dir = Quaternion.AngleAxis((i / (float)(n - 1) - 0.5f) * spread * Mathf.Rad2Deg, UP) * bdir;
                MakeProj("blade", from, dir * 24, 12, 6, 0, 0, 0.6f);
            }
            MakeProj("orb", from, bdir * 20, 15);
            Sfx("eshot"); Sfx("volley");
        }
        static readonly int[] SER_ENRAGE_OK = { };
        // 0 when the Seraph enrages at half health, rising to 1 as it nears death: it gets faster and faster
        static float SeraphFury(Ent e) => e.phase2 ? Mathf.Clamp01((0.5f - e.hp / e.maxHp) / 0.5f) : 0;
        void UpdateSeraph(Ent e, float dt)
        {
            var r = e.sr; float sp = e.phase2 ? 1.3f : 1, lim = LB - 8;
            e.stateT -= dt * sp; e.flash -= dt;
            var chest = ChestOf(Tgt); var toP = chest - e.pos; // aims at its target; hits are tested against the local ChestPos
            if (testMode && Arg("-brsp2") != null && !e.phase2 && !e.invuln && e.state == "hover" && e.hp > e.maxHp * 0.5f && Auth) e.hp = e.maxHp * 0.49f; // -brsp2: straight to the fall (test)
            if (!e.phase2 && e.hp <= e.maxHp * 0.5f && (e.state == "hover" || e.state == "feathers") && Auth) Enrage(e); // (sets up the fall; co-op: the host's call, clients follow the state)
            if (e.state == "fallRise") { UpdateSeraphFall(e, dt); AnimateSeraph(e, dt); SyncEnt(e); return; }
            bool face = true, free = true;
            teleLine.SetActive(false);
            switch (e.state)
            {
                case "intro":
                    {
                        float t = clamp(1 - e.stateT / 3.2f, 0, 1), ez = 1 - (1 - t) * (1 - t);
                        e.pos.y = lerp(45, 10, ez); free = false; e.vel = Vector3.zero;
                        Mats.SetAlpha(introPillarMat, 0.6f * (1 - t));
                        if (rand < 0.5f) SpawnParticles(e.pos + V(rand * 4 - 2, -2, rand * 4 - 2), 1, 3, sparkMat, 0.12f, 0.8f, -3);
                        if (e.stateT <= 0) { e.invuln = false; introPillar.SetActive(false); SrState(e, "hover", 1.2f); Sfx("roarShort"); }
                        break;
                    }
                case "hover":
                    {
                        e.orbit += dt * 0.6f * sp;
                        float tx = clamp(Tgt.pos.x + Mathf.Cos(e.orbit) * 18, -lim, lim), tz = clamp(Tgt.pos.z + Mathf.Sin(e.orbit) * 18, -lim, lim);
                        float ty = Mathf.Max(Tgt.pos.y + 7, 5);
                        e.vel = Vector3.Lerp(e.vel, V(tx - e.pos.x, ty - e.pos.y, tz - e.pos.z) * 1.6f, 1 - Mathf.Exp(-3 * dt));
                        if (e.stateT <= 0 && Auth) PickSeraph(e);
                        break;
                    }
                case "blinkOut":
                    {
                        e.vel *= 0.8f; float s = 2.2f * clamp(e.stateT / 0.25f, 0.05f, 1); e.g.transform.localScale = V(s, s, s);
                        if (e.stateT <= 0)
                        {
                            SpawnParticles(e.pos, 14, 6, SerFx, 0.12f, 0.5f, 0);
                            float a = rand * Mathf.PI * 2;
                            if (Auth) e.pos = V(clamp(Tgt.pos.x + Mathf.Cos(a) * 15, -lim, lim), Mathf.Max(2.5f, Tgt.pos.y + 2 + rand * 4), clamp(Tgt.pos.z + Mathf.Sin(a) * 15, -lim, lim));
                            e.g.transform.localScale = V(2.2f, 2.2f, 2.2f); e.vel = Vector3.zero;
                            SpawnParticles(e.pos, 14, 6, SerFx, 0.12f, 0.5f, 0); Sfx("blink");
                            e.dashDir = (ChestOf(Tgt) - e.pos).normalized;
                            SrState(e, "dashWind", 0.7f + 0.2f * SeraphFury(e)); // a touch more warning, since everything else runs faster
                        }
                        break;
                    }
                case "dashWind":
                    {
                        e.vel *= Mathf.Exp(-8 * dt);
                        if (e.stateT > 0.22f) e.dashDir = (chest - e.pos).normalized;
                        teleLine.SetActive(true);
                        var a = BodyPoint(e); var b = a + e.dashDir * 40;
                        teleLine.transform.localScale = V(0.12f, 0.12f, 40); teleLine.transform.position = (a + b) * 0.5f;
                        teleLine.transform.rotation = Quaternion.LookRotation(b - teleLine.transform.position);
                        Mats.SetAlpha(teleLineMat, 0.3f + 0.5f * Mathf.Abs(Mathf.Sin(time * 30)));
                        if (e.stateT <= 0) { SrState(e, "dash", 0.6f); Sfx("dashGo"); }
                        break;
                    }
                case "dash":
                    face = false; e.vel = e.dashDir * 50 * sp * (1 + 0.9f * SeraphFury(e)); // dashes get much faster as it weakens
                    if (rand < 0.8f) SpawnParticles(BodyPoint(e), 1 + (int)(3 * SeraphFury(e)), 1, SerFx, 0.2f, 0.4f, 0);
                    if (!e.hitPlayer && Vector3.Distance(BodyPoint(e), ChestPos) < 3)
                    {
                        e.hitPlayer = true; DamagePlayer(30); P.vel += e.dashDir * 20; P.vel.y += 6; P.onGround = false;
                    }
                    if (e.stateT <= 0) { e.vel *= 0.2f; if (--e.dashes > 0) SrState(e, "blinkOut", 0.25f); else SrState(e, "hover", 1.4f); }
                    break;
                case "laserWind":
                    {
                        e.vel = Vector3.Lerp(e.vel, V(0, (Tgt.pos.y + 8 - e.pos.y) * 1.5f, 0), 1 - Mathf.Exp(-4 * dt));
                        float s = 0.6f + (1 - e.stateT / 1.1f) * 3; r.eyeGlow.transform.localScale = V(s, s, s);
                        if (e.stateT <= 0)
                        {
                            var eye = r.eyeMarker.position;
                            e.beamDir = (chest - eye).normalized;
                            SrState(e, "laser", 2.8f);
                        }
                        break;
                    }
                case "laser":
                    {
                        face = false; e.vel *= Mathf.Exp(-6 * dt);
                        var eye = r.eyeMarker.position;
                        var want = (chest - eye).normalized; float ang = Vector3.Angle(e.beamDir, want) * Mathf.Deg2Rad, step = (e.phase2 ? 1.25f : 0.85f) * dt;
                        if (ang > 1e-4f) e.beamDir = Vector3.Slerp(e.beamDir, want, Mathf.Min(1, step / ang)).normalized;
                        var end = Physics.Raycast(eye, e.beamDir, out var lh, 160, LEVEL_MASK, QueryTriggerInteraction.Ignore) ? lh.point : eye + e.beamDir * 160;
                        float len = Vector3.Distance(eye, end);
                        var mid = (eye + end) * 0.5f; var q = Quaternion.FromToRotation(UP, e.beamDir);
                        float fl = 1 + Mathf.Sin(time * 60) * 0.15f;
                        beamCore.SetActive(true); beamOuter.SetActive(true); beamHit.SetActive(true);
                        beamCore.transform.position = mid; beamCore.transform.rotation = q; beamCore.transform.localScale = V(0.16f * fl, len, 0.16f * fl);
                        beamOuter.transform.position = mid; beamOuter.transform.rotation = q; beamOuter.transform.localScale = V(0.5f * fl, len, 0.5f * fl);
                        float hs = 4 + Mathf.Sin(time * 40); beamHit.transform.position = end; beamHit.transform.localScale = V(hs, hs, hs);
                        if (rand < 0.6f) SpawnParticles(end, 2, 7, SerFx, 0.1f, 0.4f);
                        if (DistPointSeg(ChestPos, eye, end) < 0.95f) DamagePlayer(12); // (co-op: the beam burns whoever it crosses, on their own machine)
                        e.sfxT -= dt; if (e.sfxT <= 0) { Sfx("laserTick"); e.sfxT = 0.08f; }
                        e.yaw = Mathf.Atan2(e.beamDir.x, e.beamDir.z);
                        if (e.stateT <= 0) { beamCore.SetActive(false); beamOuter.SetActive(false); beamHit.SetActive(false); SrState(e, "hover", 1.2f); }
                        break;
                    }
                case "feathers":
                    {
                        e.vel *= Mathf.Exp(-3 * dt);
                        var th = new[] { 1.5f, 1.0f, 0.5f };
                        if (e.vidx < 3 && e.stateT < th[e.vidx]) { SeraphVolley(e); e.vidx++; }
                        if (e.stateT <= 0) SrState(e, "hover", 1.2f);
                        break;
                    }
                case "novaRise":
                    {
                        var tgt = V(clamp(Tgt.pos.x, -lim, lim), Tgt.pos.y + 14, clamp(Tgt.pos.z, -lim, lim));
                        e.vel = (tgt - e.pos) * 4;
                        e.groundY = Tgt.onGround ? Tgt.pos.y : Mathf.Max(0, Tgt.pos.y - 3);
                        if (rand < 0.4f) SpawnParticles(V(e.pos.x + rand * 4 - 2, e.groundY + 0.1f, e.pos.z + rand * 4 - 2), 1, 1, spikeWarnMat, 0.2f, 0.4f, 0);
                        if (e.stateT <= 0) { SrState(e, "novaFall", 2); Sfx("novaDive"); }
                        break;
                    }
                case "novaFall":
                    face = false; e.vel = V(0, -55, 0);
                    if (e.pos.y - 1.8f <= e.groundY || e.stateT <= 0)
                    {
                        e.pos.y = e.groundY + 1.8f; e.vel = Vector3.zero;
                        var c = V(e.pos.x, e.groundY, e.pos.z);
                        AddRing(c, 22, 34, 20, SerCol); AddRing(c, 15, 28, 20, 0xffffff, 0.3f);
                        if (e.phase2)
                        {
                            AddRing(c, 10, 22, 20, SerCol, 0.6f);
                            for (int i = 0; i < 8; i++) MakeProj("orb", V(Tgt.pos.x + rand * 18 - 9, Tgt.pos.y + 22 + rand * 6, Tgt.pos.z + rand * 18 - 9), V(0, -18, 0), 15);
                        }
                        if (Hypot(P.pos.x - c.x, P.pos.z - c.z) < 4 && Mathf.Abs(P.pos.y - c.y) < 3) DamagePlayer(30);
                        P.shake = Mathf.Max(P.shake, 0.9f); Sfx("slam"); SpawnParticles(V(c.x, c.y + 0.3f, c.z), 30, 11, SerFx, 0.2f, 0.7f);
                        SrState(e, "novaRecover", 0.9f);
                    }
                    break;
                case "novaRecover": e.vel = V(0, 6, 0); if (e.stateT <= 0) SrState(e, "hover", 1.0f); break;
                case "enrage": e.vel = V(0, 3, 0); if (e.stateT <= 0) SrState(e, "hover", 0.8f); break;
            }
            if (free)
            {
                e.pos += e.vel * dt;
                e.pos.x = clamp(e.pos.x, -(LB - 4), LB - 4); e.pos.z = clamp(e.pos.z, -(LB - 4), LB - 4);
                if (e.state != "novaFall") e.pos.y = clamp(e.pos.y, 1.8f, 30);
            }
            if (e.state == "dash" || e.state == "dashWind")
            {
                e.yaw = Mathf.Atan2(e.dashDir.x, e.dashDir.z);
                e.pitch = -Mathf.Asin(clamp(e.dashDir.y, -1, 1));
            }
            else
            {
                e.pitch *= Mathf.Exp(-6 * dt);
                if (face) TurnTo(e, Mathf.Atan2(toP.x, toP.z), 4, dt);
            }
            AnimateSeraph(e, dt);
            SyncEnt(e);
        }
        void AnimateSeraph(Ent e, float dt)
        {
            var r = e.sr; float k = 1 - Mathf.Exp(-10 * dt); var st = e.state;
            e.flinch = Mathf.Max(0, e.flinch - dt * 3);
            r.halo.rz += dt * (1.2f + (e.phase2 ? 2.5f : 0) + (st == "laser" ? 4 : 0));
            bool dashing = st == "dash" || st == "dashWind"; float fold = dashing ? 0.9f : st == "novaFall" ? 1.1f : 0.15f;
            for (int i = 0; i < r.wings.Count; i++)
            {
                float s = r.wingS[i], flap = Mathf.Sin(time * (dashing ? 6 : 2.4f) + i * 0.4f) * (dashing ? 0.05f : 0.2f);
                float spread = st == "laser" || st == "laserWind" || st == "enrage" ? 0.35f : st == "novaRise" ? 0.5f : 0;
                if (st == "fallRise") { spread = SC.t > 1.8f && SC.t < 6.4f ? 0.65f : 0.3f; if (SC.t > 3.2f && SC.t < 5.6f) flap += Mathf.Sin(time * 31 + i) * 0.12f; }
                Sm(r.wings[i], 2, s * (r.wingBase[i] + flap + spread), k); Sm(r.wings[i], 1, s * fold, k);
            }
            float vl = e.vel.magnitude;
            for (int i = 0; i < r.ribbons.Count; i++)
                for (int j = 0; j < r.ribbons[i].Count; j++)
                    r.ribbons[i][j].SetRot(Mathf.Sin(time * 2.6f + i + j * 0.8f) * (0.2f + j * 0.1f) - vl * 0.01f, r.ribbons[i][j].ry, Mathf.Cos(time * 2 + i * 1.3f + j * 0.6f) * 0.15f);
            // the lance is carried upright; the fallen form's greatsword is held low and forward, point toward the ground,
            // angled out from the body - ready to swing - and levelled like a lance when it dashes
            bool great = r.sword != null && r.sword.go.activeSelf;
            Sm(r.spear, 0, dashing ? 0 : great ? 1.25f + Mathf.Sin(time * 1.3f) * 0.05f : -1.35f, k); Sm(r.spear, 1, great && !dashing ? 0.55f : 0, k); Sm(r.spear, 2, 0, k);
            Sm(r.sh[1], 0, dashing ? -1.4f : great ? -0.25f : -0.5f, k); Sm(r.el[1], 0, dashing ? 0 : great ? -0.55f : -0.8f, k); if (great) Sm(r.sh[1], 2, dashing ? 0 : 0.25f, k);
            Sm(r.sh[0], 0, st == "feathers" || st == "laserWind" ? -1.6f : -0.2f, k); Sm(r.sh[0], 2, st == "enrage" ? -1.2f : -0.2f, k);
            Sm(r.torso, 0, -e.flinch * 0.3f + (st == "novaFall" ? 0.4f : 0), k);
            Sm(r.head, 0, st == "laser" ? 0.1f : -e.flinch * 0.4f, k);
            r.orbit.ry += dt * (2 + (e.phase2 ? 3 : 0));
            r.root.py = Mathf.Sin(time * 1.5f) * 0.12f;
            if (st != "laserWind") { float s = st == "laser" ? 2.6f : 0.6f + Mathf.Sin(time * 4) * 0.1f; r.eyeGlow.transform.localScale = V(s, s, s); }
            float g = 0.8f + Mathf.Sin(time * 5) * 0.2f; r.gemGlow.transform.localScale = V(g, g, g);
            bool fallen = r.haloFrag != null && r.haloFrag.go.activeSelf;
            // each material's own glow: the radiant form lit from within (porcelain, gold, silk, feathers, plates), the fallen
            // form dark but for the gold, which pulses
            int[] em1 = { 0x2a2824, 0x4a3000, 0x141820, 0x4a4844, 0x2a2824 }, em2 = { 0x040206, 0x4a2400, 0x140204, 0x1a1008, 0x060406 };
            for (int i = 0; i < r.mats.Count; i++) Mats.Emissive(r.mats[i], e.flash > 0 ? 0xffffff : fallen ? (i == 1 ? (Mathf.Sin(time * 5) > 0 ? 0x5a2c00 : 0x341600) : em2[Mathf.Min(i, 4)]) : em1[Mathf.Min(i, 4)]);
            SeraphFx(e, dt);
        }
    }

    static class TransformExt
    {
        public static IEnumerable<Transform> Cast(this Transform t) { foreach (Transform c in t) yield return c; }
    }
}
