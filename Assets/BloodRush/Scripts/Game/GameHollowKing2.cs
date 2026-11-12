using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    // THE HOLLOW KING, cooler and crazier: the dead city's skyline grows out of his back (broken tower blocks, windows still
    // lit), smokestacks puffing black smoke, fire breathing out of ember cracks all over his concrete, a jaw that burns
    // inside, four eyes, horns of bent street lamps still glowing, traffic lights and chains swinging from his arms, a
    // wrecking-ball fist, a floating crown of fire over the broken one. In his second phase the fire breaks loose: it pours
    // out of every crack, the crown blazes, the windows on his back burn red. (Decoration only: his hitboxes don't change.)
    public partial class Game
    {
        void HollowExtras(Ent e, HollowRig r)
        {
            bool oc = Mk.colliders; Mk.colliders = false;
            var conc = Mats.Std(0x5e5850, 0.92f); var steel = Mats.Std(0x3a3c40, 0.45f, 0.75f); var rust = Mats.Std(0x6a3a20, 0.75f, 0.5f);
            var ember = EM(e, Mats.Basic(0xff6a18)); var fire = EM(e, Mats.Add(0xff6010, 0.75f, null, true, true)); var windows = EM(e, Mats.Basic(0xffc060, FacadeTex()));
            var lampM = EM(e, Mats.Basic(0xffd890)); var red = EM(e, Mats.Basic(0xff2010)); var amber = EM(e, Mats.Basic(0xffb000)); var green = EM(e, Mats.Basic(0x30ff60)); var dark = Mats.Std(0x141414, 0.9f);
            r.xEmber = ember; r.xFire = fire; r.xWin = windows;
            // the skyline on his back: five broken tower blocks, leaning, their windows lit
            for (int k = 0; k < 5; k++)
            {
                float x = (k - 2) * 0.36f, h = 1.2f + (k % 3) * 0.5f + (k == 2 ? 0.6f : 0);
                var tb = Mk.Piv(r.torso, x, 1.35f, -0.55f); tb.SetRot(-0.35f - (k % 2) * 0.12f, 0, (k - 2) * 0.16f);
                Mk.Msh(tb, Geo.Box(0.34f, h, 0.3f), conc, 0, h / 2, 0); Mk.Msh(tb, Geo.Box(0.35f, h * 0.85f, 0.31f), windows, 0, h * 0.45f, 0.002f);
                for (int j = 0; j < 3; j++) Mk.Msh(tb, Geo.Box(0.05f + j * 0.02f, 0.12f, 0.05f), steel, (j - 1) * 0.08f, h + 0.05f, (j % 2) * 0.06f, 0.3f * (j - 1), 0, 0.4f * (j - 1)); // snapped girders at the top
                r.xTowers.Add(tb);
            }
            // two smokestacks between his shoulders
            foreach (var sx in new[] { -1f, 1f }) { var st = Mk.Piv(r.torso, sx * 0.55f, 1.6f, -0.35f); st.SetRot(-0.25f, 0, -sx * 0.25f); Mk.Msh(st, Geo.Cyl(0.08f, 0.1f, 0.9f, 10), rust, 0, 0.45f, 0); Mk.Msh(st, Geo.Torus(0.09f, 0.02f, 10, 4), steel, 0, 0.88f, 0, Mathf.PI / 2, 0, 0); r.xStacks.Add(st); }
            // ember cracks all over the concrete - body, legs, arms, head
            void Cracks(N p, int n, float rx, float ry, float rz, float y0, float y1, int seed)
            {
                var rng = new System.Random(seed);
                for (int k = 0; k < n; k++)
                {
                    float a = (float)rng.NextDouble() * 6.28f, y = Mathf.Lerp(y0, y1, (float)rng.NextDouble());
                    Mk.Msh(p, Geo.Box(0.018f, 0.12f + (float)rng.NextDouble() * 0.2f, 0.012f), ember, Mathf.Sin(a) * rx, y, Mathf.Cos(a) * rz, 0, a, ((float)rng.NextDouble() - 0.5f) * 1.8f);
                }
            }
            Cracks(r.torso, 22, 0.78f, 0, 0.47f, 0.2f, 1.5f, 3); Cracks(r.head, 8, 0.36f, 0, 0.32f, 0.05f, 0.5f, 5);
            for (int i = 0; i < 2; i++) { Cracks(r.thigh[i], 6, 0.26f, 0, 0.28f, -0.75f, -0.1f, 7 + i); Cracks(r.knee[i], 6, 0.24f, 0, 0.26f, -0.8f, -0.1f, 9 + i); }
            Cracks(r.rEl, 5, 0.22f, 0, 0.2f, -0.8f, -0.2f, 11); Cracks(r.lEl, 5, 0.22f, 0, 0.2f, -0.8f, -0.2f, 12);
            // the head: two more eyes, a burning mouth, horns of bent street lamps still lit
            foreach (var sx in new[] { -1f, 1f }) { Mk.Msh(r.head, Geo.Sphere(0.04f, 6, 5), ember, sx * 0.3f, 0.17f, 0.3f); Mk.Msh(r.head, Geo.Box(0.07f, 0.03f, 0.02f), ember, sx * 0.27f, 0.22f, 0.31f, 0, 0, sx * 0.4f); }
            var mouth = Mk.Msh(r.jaw, Geo.Box(0.48f, 0.06f, 0.3f), ember, 0, 0.03f, 0.1f); var mg = Gl(r.jaw, 0xff5010, 0.9f, 0.6f); mg.transform.localPosition = V(0, 0.05f, 0.35f);
            foreach (var sx in new[] { -1f, 1f })
            {
                var horn = Mk.Piv(r.head, sx * 0.33f, 0.5f, -0.05f); horn.SetRot(-0.3f, 0, -sx * 0.55f);
                Mk.Msh(horn, Geo.Cyl(0.025f, 0.035f, 0.7f, 6), steel, 0, 0.35f, 0);
                var bend = Mk.Piv(horn, 0, 0.7f, 0); bend.SetRot(0.9f, 0, sx * 0.5f); Mk.Msh(bend, Geo.Cyl(0.022f, 0.025f, 0.35f, 6), steel, 0, 0.17f, 0);
                Mk.Msh(bend, Geo.Box(0.1f, 0.05f, 0.16f), dark, 0, 0.36f, 0.04f); Mk.Msh(bend, Geo.Box(0.08f, 0.02f, 0.13f), lampM, 0, 0.33f, 0.04f);
                var lg = Gl(bend, 0xffd890, 0.6f, 0.75f); lg.transform.localPosition = V(0, 0.3f, 0.04f);
            }
            // the crown of fire floating over the broken one
            r.xCrownFire = Mk.Piv(r.head, 0.04f, 0.85f, 0);
            for (int k = 0; k < 9; k++) { float a = k * Mathf.PI * 2 / 9; Mk.Msh(r.xCrownFire, Geo.Cone(0.06f, 0.32f + (k % 3) * 0.12f, 5), fire, Mathf.Cos(a) * 0.36f, 0.12f, Mathf.Sin(a) * 0.36f); }
            Mk.Msh(r.xCrownFire, Geo.Torus(0.36f, 0.02f, 24, 4), ember, 0, 0, 0, Mathf.PI / 2, 0, 0);
            // a traffic light swinging from the right forearm on a chain, chains and a car door hanging off the left
            var tl = Mk.Piv(r.rEl, 0.2f, -0.35f, 0.1f);
            for (int l = 0; l < 4; l++) Mk.Msh(tl, Geo.Torus(0.03f, 0.008f, 8, 4), steel, 0, -0.04f - l * 0.06f, 0, 0, l % 2 == 0 ? 0 : Mathf.PI / 2, 0);
            Mk.Msh(tl, Geo.Box(0.12f, 0.32f, 0.1f), dark, 0, -0.42f, 0);
            Mk.Msh(tl, Geo.Sphere(0.035f, 6, 5), red, 0, -0.32f, 0.055f); Mk.Msh(tl, Geo.Sphere(0.035f, 6, 5), amber, 0, -0.42f, 0.055f); Mk.Msh(tl, Geo.Sphere(0.035f, 6, 5), green, 0, -0.52f, 0.055f);
            r.xSwing.Add(tl);
            var door = Mk.Piv(r.lEl, -0.22f, -0.3f, 0.05f);
            for (int l = 0; l < 3; l++) Mk.Msh(door, Geo.Torus(0.03f, 0.008f, 8, 4), steel, 0, -0.04f - l * 0.06f, 0, 0, l % 2 == 0 ? 0 : Mathf.PI / 2, 0);
            Mk.Msh(door, Geo.Box(0.03f, 0.3f, 0.36f), Mats.Std(0x5a2a1a, 0.6f, 0.5f), 0, -0.36f, 0); Mk.Msh(door, Geo.Box(0.032f, 0.12f, 0.28f), Mats.Std(0x8aa0b0, 0.2f, 0.3f), 0, -0.26f, 0);
            r.xSwing.Add(door);
            // a wrecking-ball fist: the left hand clenched round a spiked concrete ball
            var ball = Mk.Piv(r.lHand, 0, -0.32f, 0.05f);
            Mk.Msh(ball, Geo.Icosahedron(0.24f), conc); for (int k = 0; k < 8; k++) { var d = Random.onUnitSphere; Mk.Msh(ball, Geo.Cone(0.04f, 0.16f, 4), rust, d.x * 0.22f, d.y * 0.22f, d.z * 0.22f).transform.localRotation = Quaternion.FromToRotation(Vector3.up, d); }
            Cracks(ball, 5, 0.2f, 0, 0.2f, -0.15f, 0.15f, 13);
            // spikes of rebar down his spine
            for (int k = 0; k < 6; k++) Mk.Msh(r.torso, Geo.Cyl(0.012f, 0.03f, 0.35f + (k % 2) * 0.15f, 5), rust, (k % 2 - 0.5f) * 0.12f, 0.25f + k * 0.2f, -0.56f, -1.9f, 0, (k % 2 - 0.5f) * 0.5f);
            Mk.colliders = oc;
        }
        void HollowExtrasAnim(Ent e, float dt)
        {
            var r = e.hk; if (r == null || r.xEmber == null) return;
            bool p2 = e.phase2; float f = 0.5f + 0.5f * Mathf.Sin(time * (p2 ? 11 : 5));
            Mats.SetColor(r.xEmber, Color.Lerp(Mats.Hex(p2 ? 0xff4010 : 0xc84a10), Mats.Hex(p2 ? 0xffd040 : 0xff8a30), f));
            Mats.SetAlpha(r.xFire, (p2 ? 0.75f : 0.45f) + 0.25f * f);
            Mats.SetColor(r.xWin, p2 ? Color.Lerp(Mats.Hex(0xff3010), Mats.Hex(0xff8040), f) : Mats.Hex(0xffc060));
            if (r.xCrownFire != null) { r.xCrownFire.ry += dt * (p2 ? 4 : 1.5f); r.xCrownFire.t.localScale = Vector3.one * (p2 ? 1.4f + 0.2f * f : 1); }
            for (int i = 0; i < r.xSwing.Count; i++) { var s = r.xSwing[i]; s.rx = Mathf.Sin(time * 2.2f + i * 1.7f) * 0.35f - e.vel.magnitude * 0.01f; s.rz = Mathf.Cos(time * 1.7f + i) * 0.25f; }
            foreach (var st in r.xStacks) if (rand < (p2 ? 0.6f : 0.3f)) SpawnParticles(st.t.TransformPoint(V(0, 0.95f, 0)), 1, 1.5f, ashMat, 0.4f + rand * 0.4f, 2.2f, -2.5f);
            if (rand < (p2 ? 0.9f : 0.35f)) { var c = r.torso.t.TransformPoint(V((rand - 0.5f) * 1.4f, 0.2f + rand * 1.3f, (rand - 0.5f) * 0.9f)); SpawnParticles(c, 1, p2 ? 3 : 1.5f, emberMat, 0.1f + rand * 0.12f, 0.9f, -3); } // fire breathing out of the cracks
            if (p2 && rand < 0.4f) { var c = r.head.t.TransformPoint(V((rand - 0.5f) * 0.5f, 0.9f, 0)); SpawnParticles(c, 1, 2, emberMat, 0.2f, 0.7f, -4); }
        }
    }
}
