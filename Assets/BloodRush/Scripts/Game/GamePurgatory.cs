using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    // ACT IV: PURGATORY - the four map levels, rebuilt (the fifth, the Hall of Hours, is in GameWarden):
    //   16 THE WAITING SHORE   the grey shore where the dead queue for the ferry: piers, the black ferry moored at the west
    //                          pier, a lighthouse, a stone arch, a wreck; THE FOG TIDE rolls in over the low ground
    //   17 CLOCKWORK HALLS     the inside of a clock: gear trains on every wall, pistons, pipes, girders on clockwork pillars;
    //                          THE MECHANISM - the floor clock's hands sweep the dial, trip-hammers pound the floor
    //   18 THE LIBRARY OF SOULS  aisles of real bookcases, a rotunda reading room, stairs onto the stacks, pages everywhere;
    //                          HAUNTED STACKS - grimoires that hurl books at you, page storms wandering the aisles
    //   19 MIRROR WASTES       a glass desert of great ornate mirrors (they turn to track you, then fire), a colossal broken
    //                          mirror, a sunken colonnade, crystal spires
    public partial class Game
    {
        // ---------- mesh batching: thousands of little boxes (books, planks, pages) as one mesh per material ----------
        readonly Dictionary<Material, Geo.MB> pgBatch = new Dictionary<Material, Geo.MB>();
        Geo.MB Batch(Material m) { if (!pgBatch.TryGetValue(m, out var mb)) pgBatch[m] = mb = new Geo.MB(); return mb; }
        static readonly Vector3[] FN = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        static readonly Vector3[] FU = { Vector3.up, Vector3.forward, Vector3.forward, Vector3.right, Vector3.right, Vector3.up };
        static readonly Vector3[] FV = { Vector3.forward, Vector3.up, Vector3.right, Vector3.forward, Vector3.up, Vector3.right };
        static void MBBox(Geo.MB mb, Vector3 c, Vector3 s, Quaternion q)
        {
            var h = s * 0.5f;
            for (int f = 0; f < 6; f++)
            {
                Vector3 n = FN[f], u = FU[f], v = FV[f]; int b = mb.v.Count; var wn = q * n;
                for (int k = 0; k < 4; k++)
                {
                    float su = k == 0 || k == 3 ? -1 : 1, sv = k < 2 ? -1 : 1;
                    var lp = Vector3.Scale(n + u * su + v * sv, h);
                    mb.v.Add(c + q * lp); mb.n.Add(wn); mb.uv.Add(new Vector2(Vector3.Dot(lp, u) * 0.5f + 0.5f, Vector3.Dot(lp, v) * 0.5f + 0.5f));
                }
                mb.Tri(b, b + 1, b + 2); mb.Tri(b, b + 2, b + 3);
            }
        }
        // a double-sided quad: centre, its two half-axes
        static void MBQuad(Geo.MB mb, Vector3 c, Vector3 hu, Vector3 hv)
        {
            var n = Vector3.Cross(hu, hv).normalized;
            for (int side = 0; side < 2; side++)
            {
                int b = mb.v.Count; var nn = side == 0 ? n : -n;
                mb.v.Add(c - hu - hv); mb.v.Add(c + hu - hv); mb.v.Add(c + hu + hv); mb.v.Add(c - hu + hv);
                for (int k = 0; k < 4; k++) mb.n.Add(nn);
                mb.uv.Add(new Vector2(0, 0)); mb.uv.Add(new Vector2(1, 0)); mb.uv.Add(new Vector2(1, 1)); mb.uv.Add(new Vector2(0, 1));
                if (side == 0) { mb.Tri(b, b + 1, b + 2); mb.Tri(b, b + 2, b + 3); } else { mb.Tri(b, b + 2, b + 1); mb.Tri(b, b + 3, b + 2); }
            }
        }
        void FlushBatches()
        {
            foreach (var kv in pgBatch)
            {
                if (kv.Value.v.Count == 0) continue;
                var mesh = kv.Value.Build(); levelMeshes.Add(mesh);
                Deco(mesh, kv.Key, 0, 0, 0).layer = Mk.LayerIgnore;
            }
            pgBatch.Clear();
        }
        // a box as a world-aligned solid with a rotated look: the solid is the AABB of the turned box shrunk a little
        void SolidTurned(Vector3 c, Vector3 s, float ry, float shrink = 0.85f)
        {
            float cs = Mathf.Abs(Mathf.Cos(ry)), sn = Mathf.Abs(Mathf.Sin(ry));
            float ex = (s.x * cs + s.z * sn) * 0.5f * shrink, ez = (s.x * sn + s.z * cs) * 0.5f * shrink;
            SolidAABB(V(c.x - ex, c.y, c.z - ez), V(c.x + ex, c.y + s.y, c.z + ez));
        }

        // ================= 16. THE WAITING SHORE =================
        void BuildWaitingShore()
        {
            Arena(68);
            Altar(0, 0, 12, 4);
            var wood = Mats.Std(0x4a4238, 0.9f, 0, 0, GrimeTex()); var woodD = Mats.Std(0x2e2924, 0.92f, 0, 0, GrimeTex()); var rope = Mats.Std(0x6a5a44, 1f);
            var iron = Mats.Std(0x3a3a3c, 0.5f, 0.7f, 0, RustTex()); var gold = Mats.Std(0xd8b860, 0.3f, 0.85f); var stone = Mats.Std(0x8a8880, 0.92f, 0, 0, GrimeTex());
            // the piers: plank decks (each plank its own board, gaps between), posts sunk into the shallows, rope rails, bollards
            foreach (var (x, z, w, d) in new[] { (0f, 44f, 10f, 40f), (-34f, -10f, 34f, 8f), (34f, 10f, 34f, 8f), (-20f, -44f, 8f, 30f), (40f, -40f, 20f, 8f) })
            {
                Box(x, 0, z, w, 1.45f, d, woodD);
                bool alongX = w > d; float L = alongX ? w : d, Wd = alongX ? d : w; var mb = Batch(wood); var mbD = Batch(woodD);
                for (float s = -L / 2 + 0.45f; s < L / 2; s += 0.9f)
                {
                    float jog = ((int)(s * 7 + x) % 3) * 0.02f;
                    var c = alongX ? V(x + s, 1.5f + jog, z) : V(x, 1.5f + jog, z + s);
                    MBBox(mb, c, alongX ? V(0.82f, 0.12f, Wd) : V(Wd, 0.12f, 0.82f), Quaternion.identity);
                }
                for (float s = -L / 2 + 0.3f; s <= L / 2; s += 4)
                    foreach (var side in new[] { -1f, 1f })
                    {
                        var c = alongX ? V(x + s, 0.4f, z + side * (Wd / 2 + 0.15f)) : V(x + side * (Wd / 2 + 0.15f), 0.4f, z + s);
                        MBBox(mbD, c, V(0.45f, 3.2f, 0.45f), Quaternion.Euler(0, s * 40, 0));
                        MBBox(mbD, c + V(0, 1.85f, 0), V(0.5f, 0.12f, 0.5f), Quaternion.identity);
                    }
                // rope rails along the long sides (sagging between posts), on short posts
                for (float s = -L / 2 + 0.3f; s + 4 <= L / 2; s += 4)
                    foreach (var side in new[] { -1f, 1f })
                    {
                        Vector3 a = alongX ? V(x + s, 2.6f, z + side * (Wd / 2 - 0.2f)) : V(x + side * (Wd / 2 - 0.2f), 2.6f, z + s);
                        Vector3 b = alongX ? V(x + s + 4, 2.6f, z + side * (Wd / 2 - 0.2f)) : V(x + side * (Wd / 2 - 0.2f), 2.6f, z + s + 4);
                        if (s > -L / 2 + 1) SagCable(a, b, -0.35f, rope, 0.04f);
                        Ig(Deco(Geo.Cyl(0.09f, 0.11f, 1.1f, 6), woodD, a.x, 2.05f, a.z));
                    }
                for (int k = 0; k < 3; k++) { float s = (k - 1) * L * 0.3f; var c = alongX ? V(x + s, 1.56f, z + Wd / 2 - 0.6f) : V(x + Wd / 2 - 0.6f, 1.56f, z + s); Deco(Geo.Cyl(0.28f, 0.34f, 0.6f, 10), iron, c.x, c.y + 0.3f, c.z); Deco(Geo.Cyl(0.4f, 0.4f, 0.1f, 10), iron, c.x, c.y + 0.62f, c.z); }
            }
            foreach (var p in new[] { V(-46, 0, 30), V(-40, 0, -42), V(18, 0, -26), V(-14, 0, 22) }) Rock(p.x, p.z, 6, 3.5f, 6);
            // the stone arch at the root of the long pier: the way the dead come down to the water
            foreach (var sx in new[] { -1f, 1f })
            {
                Box(sx * 7, 0, 26, 2.6f, 1.2f, 2.6f, stone); Box(sx * 7, 1.2f, 26, 2.0f, 10.5f, 2.0f, stone);
                for (int k = 0; k < 4; k++) Ig(Deco(Geo.Box(2.15f, 0.25f, 2.15f), M.trim, sx * 7, 3 + k * 2.4f, 26));
            }
            Deco(Geo.Arc(7, 1.0f, 24), stone, 0, 11.7f, 26); Deco(Geo.Box(3, 1.6f, 1.2f), stone, 0, 18.3f, 26);
            Ig(Deco(Geo.Box(5.2f, 1.0f, 0.12f), gold, 0, 16.6f, 26.7f)); Ig(Deco(Geo.Box(5.2f, 1.0f, 0.12f), gold, 0, 16.6f, 25.3f));
            SolidArc(V(0, 11.7f, 26), 7, 1.0f, 0, 8);
            { var ch = Deco(Geo.Cyl(0.04f, 0.04f, 3, 4), iron, 0, 15.2f, 26); Ig(ch); Deco(Geo.Box(0.6f, 0.8f, 0.6f), gold, 0, 13.3f, 26); var lg = Mk.Glow(levelRoot, 0xd8ffe8, 3.2f, 0.8f); lg.transform.localPosition = V(0, 13.3f, 26); flames.Add(lg.transform); }
            BuildFerry(V(-36, 0, -22.2f));
            BuildLighthouse(48, 34);
            // the waiting house at the end of the east pier: an open shelter, benches, a bell to call the ferry
            {
                float hx = 46, hz = 10;
                foreach (var cx in new[] { -1f, 1f }) foreach (var cz in new[] { -1f, 1f }) { Box(hx + cx * 3.6f, 1.56f, hz + cz * 3.2f, 0.5f, 4.4f, 0.5f, woodD); }
                foreach (var cz in new[] { -1f, 1f }) Deco(Geo.Box(8.6f, 0.2f, 4.4f), woodD, hx, 6.9f, hz + cz * 1.9f, cz * 0.42f, 0, 0);
                Deco(Geo.Box(8.8f, 0.3f, 0.3f), wood, hx, 7.75f, hz);
                foreach (var cz in new[] { -1f, 1f }) { Box(hx, 1.56f, hz + cz * 2.4f, 6, 0.55f, 0.7f, wood); }
                var bell = Mk.Piv(levelRoot, hx - 3.6f, 5.6f, hz - 3.6f); Mk.Msh(bell, Geo.Cyl(0.25f, 0.5f, 0.7f, 14, true), gold, 0, -0.4f, 0); Mk.Msh(bell, Geo.Box(1.2f, 0.12f, 0.12f), woodD, 0.4f, 0.1f, 0);
                anims.Add((t, dt) => bell.rz = Mathf.Sin(t * 0.9f) * 0.12f);
                var lg = Mk.Glow(levelRoot, 0xffd890, 2.4f, 0.7f); lg.transform.localPosition = V(hx, 5.9f, hz); flames.Add(lg.transform);
                for (int k = 0; k < 5; k++) WaitingSoul(V(hx - 2.2f + k * 1.1f, 1.56f, hz + (k % 2 == 0 ? 2.4f : -2.4f)), k % 2 == 0 ? Mathf.PI : 0, k, true);
            }
            // the queue of the dead along the west pier, facing the ferry
            for (int k = 0; k < 12; k++) WaitingSoul(V(-48 + k * 2.3f, 1.56f, -8.4f + (k % 3) * 0.5f), Mathf.PI, k + 7, false);
            for (int k = 0; k < 8; k++) WaitingSoul(V(-1.5f + (k % 2) * 3, 1.56f, 30 + k * 2.4f), Mathf.PI, k + 21, false);
            // channel markers in the shallows, each with a little lamp
            foreach (var p in new[] { V(-14, 0, -24), V(14, 0, -10), V(-28, 0, 14), V(22, 0, 34), V(56, 0, -6), V(-56, 0, -26) })
            { Deco(Geo.Cyl(0.18f, 0.22f, 5, 8), woodD, p.x, 2.5f, p.z); Deco(Geo.Box(0.4f, 0.5f, 0.4f), gold, p.x, 5.2f, p.z); var g = Mk.Glow(levelRoot, 0xd8ffe8, 2.0f, 0.75f); g.transform.localPosition = V(p.x, 5.2f, p.z); flames.Add(g.transform); SolidAABB(V(p.x - 0.2f, 0, p.z - 0.2f), V(p.x + 0.2f, 5.5f, p.z + 0.2f)); }
            // a wreck, half sunk: a broken keel and its ribs, planks strewn about, the mast fallen across it
            {
                float wx = -48, wz = -48; var rot = Mats.Std(0x2a2620, 0.95f, 0, 0, GrimeTex());
                Deco(Geo.Box(18, 0.8f, 1.0f), rot, wx, 0.2f, wz, 0, 0.5f, 0.05f);
                for (int k = 0; k < 7; k++) { float s = -7 + k * 2.3f; var c = V(wx + Mathf.Cos(0.5f) * s, 0, wz - Mathf.Sin(0.5f) * s); var r = Deco(Geo.Torus(3.6f - Mathf.Abs(k - 3) * 0.3f, 0.22f, 16, 4, Mathf.PI * (0.7f + (k % 2) * 0.2f)), rot, c.x, 0.1f, c.z, 0, 0.5f + Mathf.PI / 2, Mathf.PI * 0.15f + (k % 3) * 0.1f); Ig(r); }
                Deco(Geo.Cyl(0.28f, 0.34f, 14, 8), rot, wx + 4, 2.2f, wz + 3, 0.3f, 0, 1.35f);
                for (int k = 0; k < 10; k++) Ig(Deco(Geo.Box(2.6f, 0.1f, 0.4f), rot, wx + (rand - 0.5f) * 16, 0.08f, wz + (rand - 0.5f) * 12, (rand - 0.5f) * 0.2f, rand * 3, 0));
                SolidAABB(V(wx - 7, 0, wz - 4), V(wx + 7, 1.2f, wz + 4));
            }
            // ghost ships drifting out in the fog, never reaching the shore
            // (spectral: a pale hull glowing at the seams, a torn sail, a lamp burning green at the bow, mist pouring off them)
            foreach (var (p, ry, ph) in new[] { (V(56, 0, -46), 1.2f, 0f), (V(-58, 0, 50), -0.5f, 2f) })
            {
                var ghost = Mats.Trans(0xb8e0d4, 0.22f, true); var seam = Mats.Add(0x9affd8, 0.35f, null, true, true); var gs = Mats.Trans(0xd8f0e8, 0.2f, true);
                var b = Mk.Piv(levelRoot, p.x, 0, p.z); b.SetRot(0, ry, 0);
                Mk.Msh(b, HullMesh(16, 5, 2.2f, 1.1f), ghost);
                { var band = Mk.Msh(b, HullMesh(16, 5, 2.2f, 1.1f), seam); band.transform.localScale = V(1.02f, 1.03f, 1.07f); } // (a glowing skin just outside the hull: its edges burn)
                Mk.Msh(b, Geo.Cyl(0.1f, 0.14f, 9, 6), ghost, -1, 6.5f, 0); Mk.Msh(b, Geo.Box(0.1f, 0.1f, 5), ghost, -1, 9.5f, 0);
                Mk.Msh(b, SailMesh(4.6f, 4.4f, 0.7f, (int)(ph * 10) + 3, 0.12f), gs, -0.9f, 9.4f, 0, 0, Mathf.PI / 2, 0);
                Mk.Msh(b, Geo.Cyl(0.06f, 0.06f, 3.2f, 5), ghost, 8.6f, 3.0f, 0, 0, 0, -1.1f);
                var lg = Mk.Glow(b.t, 0x9affd8, 3.4f, 0.8f); lg.transform.localPosition = V(9.8f, 3.6f, 0);
                var hg = Mk.Glow(b.t, 0x7affc8, 12, 0.12f); hg.transform.localPosition = V(0, 1.2f, 0);
                foreach (var rr in b.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                anims.Add((t, dt) =>
                {
                    b.py = Mathf.Sin(t * 0.6f + ph) * 0.25f; b.rz = Mathf.Sin(t * 0.5f + ph) * 0.05f; b.px = p.x + Mathf.Sin(t * 0.05f + ph) * 4;
                    Mats.SetAlpha(seam, 0.22f + 0.18f * Mathf.Sin(t * 1.3f + ph)); Mats.SetAlpha(ghost, 0.18f + 0.08f * Mathf.Sin(t * 0.7f + ph));
                    if (rand < 0.25f) SpawnParticles(b.t.TransformPoint(V((rand - 0.5f) * 14, 0.3f, (rand - 0.5f) * 4)), 1, 0.4f, ashMat, 0.5f, 2.5f, -0.3f);
                });
            }
            for (int k = 0; k < 12; k++) { float a = k / 12f * Mathf.PI * 2; Lantern(Mathf.Cos(a) * 30, Mathf.Sin(a) * 30, 3.5f); }
            PadAt(-34, -10, 24); PadAt(34, 10, 24); PadAt(0, 30, 22);
            FlushBatches();
            MechFogTide();
        }
        // a robed, hooded soul waiting in line: translucent, swaying, fading in and out
        // a soul waiting in line: a hooded mourner in a long robe that thins away into mist below the knees - stooped
        // shoulders, sleeves hanging to the hands clasped in front (some hold a candle), a dark hood with two faint points of
        // light for eyes; pale and glowing at the edges, it floats a little off the boards, sways, and fades in and out
        void WaitingSoul(Vector3 p, float ry, int seed, bool sitting)
        {
            var m = Mats.Trans(0xc8e0dc, 0.34f, true); var edge = Mats.Add(0x9affe0, 0.18f, null, true, true); var voidM = Mats.Trans(0x060a0c, 0.85f);
            var r = Mk.Piv(levelRoot, p.x, p.y, p.z); r.SetRot(0, ry + (seed % 5 - 2) * 0.12f, 0);
            var body = Mk.Piv(r, 0, sitting ? -0.35f : 0.25f, 0); body.SetRot(0.08f + (seed % 3) * 0.04f, 0, 0); // a slight forward stoop
            float s = 0.95f + (seed % 4) * 0.05f; body.Scale(s);
            // the robe: wide at the hem, falling in folds (a ring of narrow panels), the hem trailing off into wisps
            Mk.Msh(body, Geo.Cyl(0.24f, 0.44f, 1.25f, 14, true), m, 0, 0.95f, 0);
            for (int k = 0; k < 10; k++) { float a = k * Mathf.PI / 5; Mk.Msh(body, Geo.Box(0.14f, 1.1f, 0.03f), m, Mathf.Sin(a) * 0.38f, 0.85f, Mathf.Cos(a) * 0.38f, -0.18f * Mathf.Cos(a), a, 0.18f * Mathf.Sin(a)); }
            Mk.Msh(body, Geo.Cone(0.42f, 0.9f, 12), m, 0, -0.1f, 0, Mathf.PI, 0, 0); // fading away below
            Mk.Msh(body, Geo.Cone(0.45f, 1.0f, 12), edge, 0, -0.12f, 0, Mathf.PI, 0, 0);
            // shoulders and a mantle, the hood (open at the front) and the dark inside it
            Mk.Msh(body, Geo.Sphere(0.3f, 12, 8, 0, Mathf.PI * 2, 0, Mathf.PI / 2), m, 0, 1.55f, 0); body.t.GetChild(body.t.childCount - 1).localScale = V(1.25f, 0.7f, 0.9f);
            Mk.Msh(body, Geo.Sphere(0.24f, 14, 10, Mathf.PI / 2 + 0.7f, Mathf.PI * 2 - 1.4f, 0, Mathf.PI * 0.7f), m, 0, 1.78f, -0.02f);
            Mk.Msh(body, Geo.Cone(0.16f, 0.34f, 10), m, 0, 2.05f, -0.1f, -0.45f, 0, 0); // the drooping point of the hood
            Mk.Msh(body, Geo.Sphere(0.17f, 10, 8), voidM, 0, 1.76f, 0.04f);
            foreach (var sx in new[] { -1f, 1f }) { var eye = Mk.Glow(body.t, 0xb8fff0, 0.09f, 0.95f); eye.transform.localPosition = V(sx * 0.055f, 1.79f, 0.2f); }
            // sleeves falling from the shoulders to the clasped hands
            foreach (var sx in new[] { -1f, 1f }) Mk.Msh(body, Geo.Cyl(0.07f, 0.13f, 0.7f, 8, true), m, sx * 0.18f, 1.25f, 0.2f, 0.85f, 0, -sx * 0.45f);
            Mk.Msh(body, Geo.Sphere(0.08f, 8, 6), m, 0, 1.08f, 0.42f);
            if (seed % 3 == 0) { Mk.Msh(body, Geo.Cyl(0.025f, 0.03f, 0.22f, 6), Mats.Trans(0xf0f0e0, 0.6f), 0, 1.24f, 0.44f); var g = Mk.Glow(body.t, 0xd8ffe8, 0.55f, 0.85f); g.transform.localPosition = V(0, 1.4f, 0.44f); }
            var aura = Mk.Glow(body.t, 0x9affe0, 1.6f, 0.12f); aura.transform.localPosition = V(0, 1.2f, 0);
            var aM = aura.GetComponent<Renderer>().sharedMaterial;
            foreach (var rr in r.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
            float ph = seed * 1.7f, y0 = body.py;
            anims.Add((t, dt) =>
            {
                float f = 0.5f + 0.5f * Mathf.Sin(t * 0.4f + ph);
                body.py = y0 + Mathf.Sin(t * 0.9f + ph) * 0.06f; body.rz = Mathf.Sin(t * 0.7f + ph) * 0.04f;
                Mats.SetAlpha(m, 0.2f + 0.16f * f); Mats.SetAlpha(edge, 0.08f + 0.12f * f); Mats.SetAlpha(aM, 0.06f + 0.08f * f);
                if (rand < 0.03f) SpawnParticles(r.t.TransformPoint(V((rand - 0.5f) * 0.5f, 0.1f, (rand - 0.5f) * 0.5f)), 1, 0.2f, ashMat, 0.12f, 1.4f, -0.6f);
            });
        }
        // a sail: a grid bowed out by the wind, with a few panels torn away (local x across, y hanging down from the yard)
        static Mesh SailMesh(float w, float h, float bulge, int seed, float tears)
        {
            var mb = new Geo.MB(); const int NX = 10, NY = 8; var rng = new System.Random(seed);
            for (int j = 0; j <= NY; j++) for (int i = 0; i <= NX; i++)
            {
                float u = (float)i / NX, v = (float)j / NY;
                float z = bulge * Mathf.Sin(u * Mathf.PI) * Mathf.Sin(Mathf.Min(1, v * 1.3f) * Mathf.PI * 0.8f) + (j == NY ? 0 : (float)(rng.NextDouble() - 0.5) * 0.06f);
                mb.v.Add(new Vector3((u - 0.5f) * w * (1 - 0.08f * v), -v * h, z)); mb.uv.Add(new Vector2(u, 1 - v));
            }
            int R = NX + 1;
            for (int j = 0; j < NY; j++) for (int i = 0; i < NX; i++)
            {
                if (j > 1 && rng.NextDouble() < tears) continue; // a tear
                int a = j * R + i, b = a + 1, c = a + R, d = c + 1; mb.Tri(a, c, d); mb.Tri(a, d, b);
            }
            mb.ComputeNormals(); return mb.Build();
        }
        static Texture2D plankTex;
        static Texture2D PlankTex()
        {
            if (plankTex) return plankTex;
            var c = new Cv(64, 64); var rng = new System.Random(4);
            c.Fill(Mats.Css("#6a5644"));
            for (int k = 0; k < 11; k++)
            {
                float y = k * 64f / 11; var tone = Mats.Css(k % 3 == 0 ? "#5a4838" : k % 3 == 1 ? "#72604c" : "#665240");
                c.Rect(0, y, 64, 64f / 11 - 1, tone); c.Rect(0, y + 64f / 11 - 1, 64, 1.2f, Mats.Css("#2a2018"));
                float seamX = (float)rng.NextDouble() * 60; c.Rect(seamX, y, 1, 64f / 11, Mats.Css("#2a2018"));
                for (int g = 0; g < 3; g++) c.Rect((float)rng.NextDouble() * 64, y + 1 + (float)rng.NextDouble() * 3, 6 + (float)rng.NextDouble() * 10, 0.6f, Mats.Css("#4e3e30"));
            }
            return plankTex = c.ToTex(true, true);
        }
        // a hull: bow at +x, beam along z, the deck edge at y=D (rising at bow and stern), keel at y=-K
        static Mesh HullMesh(float L, float W, float D, float K)
        {
            var mb = new Geo.MB(); const int N = 22, M = 10;
            for (int i = 0; i <= N; i++)
            {
                float t = -1 + 2f * i / N, x = t * L / 2;
                float hw = t >= 0 ? W / 2 * Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow(t, 2.4f))) : W / 2 * (1 - 0.35f * t * t);
                float yd = D + 0.9f * Mathf.Pow(Mathf.Max(0, t), 2) * D * 0.5f + 0.6f * Mathf.Pow(Mathf.Max(0, -t), 3) * D * 0.4f;
                float yk = -K * (1 - 0.6f * Mathf.Pow(Mathf.Max(0, t), 3));
                for (int j = 0; j <= M; j++)
                {
                    float ph = Mathf.PI * j / M, cz = Mathf.Cos(ph), sy = Mathf.Sin(ph);
                    float z = -hw * Mathf.Sign(cz) * Mathf.Pow(Mathf.Abs(cz), 0.7f), y = yd - (yd - yk) * Mathf.Pow(Mathf.Max(0, sy), 0.8f);
                    mb.v.Add(new Vector3(x, y, z)); mb.uv.Add(new Vector2(x / 4, y / 4));
                }
            }
            int R = M + 1;
            for (int i = 0; i < N; i++) for (int j = 0; j < M; j++) { int a = i * R + j, b = a + 1, c = a + R, d = c + 1; mb.Tri(a, c, d); mb.Tri(a, d, b); } // (one winding, facing out: the materials are two-sided, and doubled faces would cancel the normals)
            // the transom (the flat stern)
            for (int j = 1; j < M; j++) mb.Tri(0, j, j + 1);
            mb.ComputeNormals(); return mb.Build();
        }
        // the ferry: a long black hull moored beside the west pier, a gangplank onto its deck, a raised stern with the
        // ferryman's cabin, a mast with tattered sails and rigging, oars shipped, a hooded figurehead holding the bow lamp
        void BuildFerry(Vector3 c)
        {
            const float L = 30, W = 9, D = 3.0f;
            var hullM = Mats.Std(0xb09478, 0.8f, 0, 0x16120c, PlankTex(), true); var deckM = Mats.Std(0x6a5a48, 0.88f, 0, 0, PlankTex()); var trimM = Mats.Std(0xb8a060, 0.4f, 0.7f);
            var sail = Mats.Std(0xe0d8c0, 0.95f, 0, 0x201e18, ClothTex2(), true); var rope = Mats.Std(0x5a4a38, 1f); var woodD = Mats.Std(0x3a2e24, 0.88f, 0, 0, GrimeTex());
            var greenGlow = Mats.Basic(0xb8ffd8); var seamM = Mats.Add(0x7affc8, 0.4f, null, true, true);
            var hull = Deco(HullMesh(L, W, D, 0.8f), hullM, c.x, 0, c.z); levelMeshes.Add(hull.GetComponent<MeshFilter>().sharedMesh); hull.layer = Mk.LayerIgnore;
            float HW(float t) => t >= 0 ? W / 2 * Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow(t, 2.4f))) : W / 2 * (1 - 0.35f * t * t);
            float YD(float t) => D + 0.45f * D * Mathf.Pow(Mathf.Max(0, t), 2) + 0.24f * D * Mathf.Pow(Mathf.Max(0, -t), 3);
            // rub-rails along the hull, a ghostly green seam glowing at the waterline, portholes lit from inside
            for (int i = 0; i < 19; i++)
            {
                float t0 = -0.95f + i * 0.1f, t1 = Mathf.Min(0.97f, t0 + 0.1f);
                foreach (var sz in new[] { -1f, 1f })
                {
                    foreach (var (yo, k, mat, th) in new[] { (-0.55f, 1.0f, woodD, 0.16f), (-1.6f, 0.97f, woodD, 0.12f), (-2.45f, 0.86f, (Material)seamM, 0.07f) })
                    {
                        var a = c + V(t0 * L / 2, YD(t0) + yo, sz * HW(t0) * k); var b = c + V(t1 * L / 2, YD(t1) + yo, sz * HW(t1) * k); var d = b - a;
                        if (d.sqrMagnitude < 1e-4f) continue;
                        MBBox(Batch(mat), (a + b) / 2 + V(0, 0, sz * 0.04f), V(d.magnitude + 0.06f, th, 0.12f), Quaternion.LookRotation(d.normalized) * Quaternion.Euler(0, 90, 0));
                    }
                }
            }
            for (float x = -10; x <= 9; x += 3.2f) foreach (var sz in new[] { -1f, 1f })
            {
                float t = x / (L / 2); var pp = c + V(x, YD(t) - 1.05f, sz * (HW(t) * 0.985f + 0.03f));
                Ig(Deco(Geo.Circle(0.22f, 14), greenGlow, pp.x, pp.y, pp.z, 0, sz > 0 ? 0 : Mathf.PI, 0)); Ig(Deco(Geo.Torus(0.24f, 0.05f, 14, 4), trimM, pp.x, pp.y, pp.z + sz * 0.01f));
            }
            // the stem rising at the bow and a bowsprit reaching out over the water, the sternpost
            Ig(Deco(Geo.Box(0.4f, 5.2f, 0.4f), woodD, c.x + L / 2 - 0.4f, D + 0.4f, c.z, 0, 0, -0.42f));
            Ig(Deco(Geo.Cyl(0.1f, 0.18f, 7, 8), woodD, c.x + L / 2 + 1.6f, D + 2.4f, c.z, 0, 0, -1.2f));
            Ig(Deco(Geo.Box(0.4f, 4.4f, 0.5f), woodD, c.x - L / 2 + 0.1f, D + 0.9f, c.z, 0, 0, 0.12f));
            // deck: planks, tapering into the bow; solids under it so the deck is walkable
            var mb = Batch(deckM);
            for (float x = -L / 2 + 1; x < L / 2 - 1.5f; x += 0.8f)
            {
                float t = (x + 0.4f) / (L / 2), hw = t >= 0 ? W / 2 * Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow(t, 2.4f))) : W / 2 * (1 - 0.35f * t * t);
                MBBox(mb, c + V(x + 0.4f, D - 0.05f, 0), V(0.74f, 0.1f, Mathf.Max(0.4f, hw * 2 - 0.5f)), Quaternion.identity);
            }
            SolidAABB(c + V(-L / 2 + 1, 0, -W / 2 + 0.6f), c + V(L / 2 - 6, D, W / 2 - 0.6f));
            SolidAABB(c + V(L / 2 - 6, 0, -W / 2 + 1.6f), c + V(L / 2 - 2.5f, D, W / 2 - 1.6f));
            // the gunwale: a rail along the deck edge, with posts
            for (int i = 0; i < 18; i++)
            {
                float t0 = -0.92f + i * 0.1f, t1 = t0 + 0.1f;
                foreach (var sz in new[] { -1f, 1f })
                {
                    Vector3 P0(float t) { float hw = t >= 0 ? W / 2 * Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow(t, 2.4f))) : W / 2 * (1 - 0.35f * t * t); float yd = D + 0.45f * D * Mathf.Pow(Mathf.Max(0, t), 2) + 0.24f * D * Mathf.Pow(Mathf.Max(0, -t), 3); return c + V(t * L / 2, yd + 0.9f, sz * (hw - 0.15f)); }
                    var a = P0(t0); var b = P0(t1); var d = b - a; if (sz > 0 && (a.x + b.x) / 2 - c.x > -4 && (a.x + b.x) / 2 - c.x < 0) continue;
                    MBBox(Batch(trimM), (a + b) / 2, V(d.magnitude + 0.05f, 0.14f, 0.18f), Quaternion.LookRotation(d.normalized) * Quaternion.Euler(0, 90, 0));
                    MBBox(Batch(woodD), a - V(0, 0.45f, 0), V(0.14f, 0.9f, 0.14f), Quaternion.identity);
                }
            }
            SolidAABB(c + V(-L / 2 + 1, D, -(W / 2 - 0.7f) - 0.25f), c + V(L / 2 - 6, D + 1.1f, -(W / 2 - 0.7f) + 0.25f));
            SolidAABB(c + V(-L / 2 + 1, D, (W / 2 - 0.7f) - 0.25f), c + V(-3.5f, D + 1.1f, (W / 2 - 0.7f) + 0.25f)); SolidAABB(c + V(-0.5f, D, (W / 2 - 0.7f) - 0.25f), c + V(L / 2 - 6, D + 1.1f, (W / 2 - 0.7f) + 0.25f)); // (a gap in the rail for the gangplank)
            // the gangplank from the pier (y 1.6) up to the deck (y 3.0), on two steps
            Ig(Deco(Geo.Box(2.6f, 0.14f, 4.6f), deckM, c.x - 2, 2.3f, c.z + W / 2 + 1.7f, 0.37f, 0, 0));
            SolidAABB(c + V(-3.3f, 0, W / 2 + 2.1f), c + V(-0.7f, 2.1f, W / 2 + 3.75f)); SolidAABB(c + V(-3.3f, 0, W / 2 + 0.7f), c + V(-0.7f, 2.6f, W / 2 + 2.1f)); SolidAABB(c + V(-3.3f, 0, W / 2 - 0.9f), c + V(-0.7f, D, W / 2 + 0.7f));
            // the raised stern deck and the ferryman's cabin on it
            Box(c.x - L / 2 + 3.5f, D, c.z, 5.5f, 0.7f, W - 2.4f, deckM); Box(c.x - L / 2 + 6.6f, D, c.z, 0.8f, 0.35f, W - 3, deckM);
            Box(c.x - L / 2 + 3.2f, D + 0.7f, c.z, 3.6f, 2.6f, 4.4f, woodD); Deco(Geo.Box(4.4f, 0.3f, 5.4f), hullM, c.x - L / 2 + 3.2f, D + 3.45f, c.z);
            foreach (var sz in new[] { -1f, 1f }) { Ig(Deco(Geo.Box(1.1f, 0.8f, 0.06f), Mats.Basic(0xb8ffd8), c.x - L / 2 + 3.2f, D + 2.2f, c.z + sz * 2.23f)); }
            { var cg = Mk.Glow(levelRoot, 0xb8ffd8, 3.0f, 0.5f); cg.transform.localPosition = c + V(-L / 2 + 3.2f, D + 2.2f, 2.6f); flames.Add(cg.transform); }
            // mast, yards, the tattered sails (in strips, holes torn through them) and the rigging
            float mx = c.x - 1; Deco(Geo.Cyl(0.24f, 0.32f, 17, 10), woodD, mx, D + 8.5f, c.z); SolidAABB(V(mx - 0.3f, D, c.z - 0.3f), V(mx + 0.3f, D + 17, c.z + 0.3f));
            Deco(Geo.Box(0.2f, 0.2f, 11), woodD, mx, D + 15, c.z); Deco(Geo.Box(0.18f, 0.18f, 10), woodD, mx, D + 8.5f, c.z);
            // the sails: a great square mainsail bellied out by a wind that isn't there, a topsail over it, a jib to the bowsprit -
            // all old canvas, torn through in places
            var sails = Mk.Piv(levelRoot, mx + 0.35f, D + 15, c.z);
            Mk.Msh(sails, SailMesh(10.4f, 6.3f, 1.5f, 7, 0.08f), sail, 0, 0, 0, 0, Mathf.PI / 2, 0);
            var top = Mk.Msh(sails, SailMesh(7.4f, 1.6f, 0.6f, 9, 0.0f), sail, 0, 1.75f, 0, 0, Mathf.PI / 2, 0);
            Deco(Geo.Box(0.16f, 0.16f, 7.6f), woodD, mx, D + 16.75f, c.z);
            foreach (var rr in sails.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
            anims.Add((t, dt) => { sails.ry = Mathf.Sin(t * 0.7f) * 0.05f; sails.rz = Mathf.Sin(t * 0.9f + 1) * 0.03f; sails.t.localScale = V(1 + 0.06f * Mathf.Sin(t * 1.1f), 1, 1); });
            {
                // the jib: a triangle from the mast to the end of the bowsprit
                var jb = new Geo.MB(); var a0 = V(mx + 0.3f, D + 14.5f, c.z); var a1 = V(c.x + L / 2 + 4.2f, D + 4.6f, c.z); var a2 = V(mx + 2.0f, D + 4.2f, c.z);
                jb.v.Add(a0); jb.v.Add(a1); jb.v.Add(a2); jb.v.Add((a0 + a1 + a2) / 3 + V(0, 0, 0.9f)); for (int k = 0; k < 4; k++) jb.uv.Add(Vector2.zero);
                jb.Tri(0, 1, 3); jb.Tri(1, 2, 3); jb.Tri(2, 0, 3); jb.ComputeNormals(); var jm = jb.Build(); levelMeshes.Add(jm); Ig(Deco(jm, sail, 0, 0, 0));
                SagCable(a0, a1, -0.1f, rope, 0.04f);
            }
            // a crow's nest, a pennant streaming from the masthead, lamps at the stern and on the mast
            Deco(Geo.Cyl(0.95f, 0.8f, 0.9f, 14, true), woodD, mx, D + 13.4f, c.z); Deco(Geo.Cyl(0.9f, 0.9f, 0.12f, 14), woodD, mx, D + 13.0f, c.z);
            var pen = Mk.Piv(levelRoot, mx, D + 17.1f, c.z); var pm = new Geo.MB(); pm.v.Add(V(0, 0.3f, 0)); pm.v.Add(V(0, -0.3f, 0)); pm.v.Add(V(-3.2f, 0, 0)); for (int k = 0; k < 3; k++) pm.uv.Add(Vector2.zero); pm.Tri(0, 1, 2); pm.ComputeNormals(); var pmm = pm.Build(); levelMeshes.Add(pmm);
            Mk.Msh(pen, pmm, Mats.Std(0x3a6a5a, 0.8f, 0, 0x041008, null, true)).layer = Mk.LayerIgnore; anims.Add((t, dt) => { pen.ry = 0.4f + Mathf.Sin(t * 1.7f) * 0.25f; pen.rx = Mathf.Sin(t * 2.3f) * 0.15f; });
            foreach (var lp in new[] { V(-L / 2 + 0.6f, D + 4.4f, 1.6f), V(-L / 2 + 0.6f, D + 4.4f, -1.6f), V(-1, D + 10.2f, 0.5f) })
            {
                Ig(Deco(Geo.Box(0.4f, 0.55f, 0.4f), trimM, c.x + lp.x, lp.y, c.z + lp.z)); Ig(Deco(Geo.Box(0.32f, 0.4f, 0.42f), greenGlow, c.x + lp.x, lp.y, c.z + lp.z));
                var g = Mk.Glow(levelRoot, 0xb8ffd8, 2.6f, 0.8f); g.transform.localPosition = c + lp; flames.Add(g.transform);
            }
            // the ship's wheel on the stern deck, in front of the cabin
            {
                var wp = c + V(-L / 2 + 6.0f, D + 1.6f, 0); Deco(Geo.Box(0.3f, 1.0f, 0.3f), woodD, wp.x - 0.1f, D + 1.05f, wp.z);
                var wh = Mk.Piv(levelRoot, wp.x + 0.15f, wp.y, wp.z); wh.SetRot(0, Mathf.PI / 2, 0);
                Mk.Msh(wh, Geo.Torus(0.75f, 0.06f, 24, 4), woodD); for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; Mk.Msh(wh, Geo.Box(0.06f, 1.9f, 0.06f), woodD, 0, 0, 0, 0, 0, a); }
                Mk.Msh(wh, Geo.Cyl(0.14f, 0.14f, 0.2f, 10), trimM, 0, 0, 0, Mathf.PI / 2, 0, 0);
                foreach (var rr in wh.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                anims.Add((t, dt) => wh.rz = Mathf.Sin(t * 0.3f) * 0.4f);
            }
            // mist pouring off the hull
            { var cc = c; anims.Add((t, dt) => { if (rand < 0.35f) SpawnParticles(cc + V((rand - 0.5f) * L * 0.9f, 0.4f + rand * 0.6f, (rand < 0.5f ? -1 : 1) * W * 0.5f), 1, 0.5f, ashMat, 0.4f, 2.4f, -0.3f); }); }
            foreach (var to in new[] { V(L / 2 - 1, D + 2, 0), V(-L / 2 + 1, D + 4, 0), V(0, D + 1, W / 2 - 0.3f), V(0, D + 1, -W / 2 + 0.3f), V(-4, D + 1, W / 2 - 0.4f), V(-4, D + 1, -W / 2 + 0.4f) })
                SagCable(V(mx, D + 16.4f, c.z), c + to, -0.2f, rope, 0.035f);
            // the oars shipped along both sides, dipping to the water
            foreach (var sz in new[] { -1f, 1f }) for (int k = 0; k < 5; k++) Ig(Deco(Geo.Box(0.14f, 0.14f, 6.5f), woodD, c.x - 8 + k * 3.6f, D - 0.5f, c.z + sz * (W / 2 + 2.0f), sz * 0.55f, 0, 0));
            // the bow: a hooded figurehead leaning out over the water with the lamp, mooring ropes to the pier
            var fh = Mk.Piv(levelRoot, c.x + L / 2 - 0.6f, D + 1.3f, c.z); fh.SetRot(0, 0, -0.55f);
            Mk.Msh(fh, Geo.Cyl(0.3f, 0.6f, 2.4f, 10, true), hullM, 0, 0.6f, 0); Mk.Msh(fh, Geo.Cone(0.42f, 0.8f, 10), hullM, 0, 2.2f, 0); Mk.Msh(fh, Geo.Box(0.12f, 1.6f, 0.12f), trimM, 0.45f, 1.4f, 0, 0, 0, -0.9f);
            var bl = Mk.Glow(fh.t, 0xb8ffd8, 3.6f, 0.85f); bl.transform.localPosition = V(1.2f, 2.0f, 0); flames.Add(bl.transform);
            foreach (var rr in fh.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
            SagCable(c + V(L / 2 - 4, D + 0.6f, W / 2 - 1), c + V(L / 2 - 6, 2.2f, W / 2 + 3.6f), -0.5f, rope, 0.05f);
            SagCable(c + V(-L / 2 + 3, D + 0.9f, W / 2 - 1), c + V(-L / 2 + 5, 2.2f, W / 2 + 3.6f), -0.5f, rope, 0.05f);
            // it rides the swell a little (only the parts that aren't standing on the deck solids)
            var hn = new N(hull.transform); anims.Add((t, dt) => { hn.rx = Mathf.Sin(t * 0.5f) * 0.012f; });
        }
        // the lighthouse on its rock: banded stone, a gallery, the lamp room, two beams sweeping the fog
        void BuildLighthouse(float x, float z)
        {
            var white = Mats.Std(0xd8d4c8, 0.9f, 0, 0, GrimeTex()); var band = Mats.Std(0x3a3a40, 0.85f, 0, 0, GrimeTex()); var iron = Mats.Std(0x2a2a2c, 0.5f, 0.6f);
            Rock(x, z, 11, 2.6f, 11);
            const float H = 24;
            for (int k = 0; k < 6; k++) { float y0 = 2.6f + k * H / 6, r0 = 3.2f - k * 0.14f; Deco(Geo.Cyl(r0 - 0.14f, r0, H / 6, 18), k % 2 == 0 ? white : band, x, y0 + H / 12, z); }
            SolidAABB(V(x - 2.4f, 2.6f, z - 2.4f), V(x + 2.4f, H + 2.6f, z + 2.4f));
            float top = H + 2.6f;
            Deco(Geo.Cyl(3.8f, 3.8f, 0.4f, 20), iron, x, top + 0.2f, z);
            for (int k = 0; k < 16; k++) { float a = k * Mathf.PI / 8; Deco(Geo.Box(0.08f, 1.1f, 0.08f), iron, x + Mathf.Cos(a) * 3.6f, top + 0.95f, z + Mathf.Sin(a) * 3.6f); }
            Ig(Deco(Geo.Torus(3.6f, 0.05f, 32, 4), iron, x, top + 1.5f, z, Mathf.PI / 2));
            Ig(Deco(Geo.Cyl(1.9f, 1.9f, 2.8f, 12, true), Mats.Add(0xe8fff0, 0.35f, null, false, true), x, top + 1.8f, z));
            for (int k = 0; k < 6; k++) { float a = k * Mathf.PI / 3; Deco(Geo.Box(0.12f, 2.8f, 0.12f), iron, x + Mathf.Cos(a) * 1.95f, top + 1.8f, z + Mathf.Sin(a) * 1.95f); }
            Deco(Geo.Cone(2.4f, 2.0f, 12), iron, x, top + 4.2f, z); Deco(Geo.Sphere(0.3f, 8, 6), iron, x, top + 5.3f, z);
            var core = Mk.Glow(levelRoot, 0xf0fff0, 6, 0.95f, false); core.transform.localPosition = V(x, top + 1.8f, z);
            var beam = Mk.Piv(levelRoot, x, top + 1.8f, z); var bm = Mats.Add(0xf0fff0, 0.12f, Tex.RayFade, false, true);
            foreach (var s in new[] { 1f, -1f }) { var b = Mk.Msh(beam, Geo.Cyl(0.6f, 7, 90, 12, true), bm, s * 45, 0, 0, 0, 0, s * Mathf.PI / 2); b.layer = Mk.LayerIgnore; }
            anims.Add((t, dt) => beam.ry = t * 0.45f);
        }
        // ---------- THE FOG TIDE ----------
        // every 15s the grey fog-water rolls in over the low ground: the bell on the shore tolls, the fog banks gather and
        // rise, hands reach up out of it; standing in it drags at you and it bites harder the longer you stay (7 -> 13 a tick)
        void MechFogTide()
        {
            var wm = Mats.Trans(0x3e4a50, 0.8f, true, false); var water = Deco(Geo.Plane(LB * 2, LB * 2), wm, 0, -0.4f, 0, -Mathf.PI / 2); water.layer = Mk.LayerIgnore;
            var foam = Mats.Add(0xb0bcc0, 0.08f, Tex.Holy, true, true); var fo = Deco(Geo.Plane(LB * 2, LB * 2), foam, 0, -0.38f, 0, -Mathf.PI / 2); fo.layer = Mk.LayerIgnore; foam.SetTextureScale("_BaseMap", V(6, 6, 0));
            var mist = Mats.Add(0x9aa4aa, 0.0f, Tex.Holy, true, true); var mi = Deco(Geo.Plane(LB * 2, LB * 2), mist, 0, 0.2f, 0, -Mathf.PI / 2); mi.layer = Mk.LayerIgnore; mist.SetTextureScale("_BaseMap", V(3, 3, 0));
            // fog banks: big soft billboards that sit on the surface, drifting
            var banks = new List<(Transform t, Material m, Vector3 p, float ph, float a)>();
            for (int i = 0; i < 34; i++)
            {
                float x = (rand - 0.5f) * (LB * 2 - 8), z = (rand - 0.5f) * (LB * 2 - 8); var g = Mk.Glow(levelRoot, 0x4a5458, 9 + rand * 8, 0.0f);
                banks.Add((g.transform, g.GetComponent<Renderer>().sharedMaterial, V(x, 0, z), rand * 6, 0.06f + rand * 0.06f));
            }
            // the hands of the drowned, reaching up out of the fog
            var handM = Mats.Trans(0xc8d4d4, 0.45f); var hands = new List<(N n, Vector3 p, float ph)>();
            for (int i = 0; i < 14; i++)
            {
                var p = V((rand - 0.5f) * (LB * 2 - 14), 0, (rand - 0.5f) * (LB * 2 - 14)); if (PointInLevel(p + V(0, 0.5f, 0))) continue;
                var h = Mk.Piv(levelRoot, p.x, -2, p.z); h.SetRot(0, rand * 6, 0);
                Mk.Msh(h, Geo.Cyl(0.09f, 0.12f, 1.6f, 6), handM, 0, 0.8f, 0); Mk.Msh(h, Geo.Box(0.26f, 0.3f, 0.08f), handM, 0, 1.75f, 0);
                for (int k = 0; k < 4; k++) { var f = Mk.Piv(h, (k - 1.5f) * 0.07f, 1.9f, 0); f.SetRot(0.4f, 0, (k - 1.5f) * 0.12f); Mk.Msh(f, Geo.Cyl(0.02f, 0.03f, 0.3f, 4), handM, 0, 0.15f, 0); }
                foreach (var rr in h.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                hands.Add((h, p, rand * 6));
            }
            float hitT = 0; int stacks = 0; bool tolled = false;
            trapAnims.Add((t, dt) =>
            {
                float u = (t + 2) % 15f, h;
                if (u < 7) h = -0.4f; else if (u < 9) h = Mathf.Lerp(-0.4f, 1.15f, Smoother((u - 7) / 2)); else if (u < 12.5f) h = 1.15f; else h = Mathf.Lerp(1.15f, -0.4f, Smoother((u - 12.5f) / 2.5f));
                h += Mathf.Sin(t * 1.3f) * 0.06f;
                // the warning: the shore bell tolls, the fog starts gathering a couple of seconds before the water comes
                if (u >= 5.2f && !tolled) { tolled = true; Sfx("toll", 0.55f); Sfx("rumble", 0.4f); }
                if (u < 5) tolled = false;
                float gather = u < 5 ? 0 : u < 9 ? Smoother((u - 5) / 4) : u < 12.5f ? 1 : 1 - Smoother((u - 12.5f) / 2.5f);
                water.transform.localPosition = V(0, h, 0); fo.transform.localPosition = V(0, h + 0.02f, 0); mi.transform.localPosition = V(0, h + 0.55f, 0);
                foam.SetTextureOffset("_BaseMap", V(t * 0.02f, Mathf.Sin(t * 0.3f) * 0.05f, 0)); mist.SetTextureOffset("_BaseMap", V(-t * 0.035f, t * 0.01f, 0));
                Mats.SetAlpha(mist, 0.05f * gather);
                foreach (var b in banks)
                {
                    var p = b.p + V(Mathf.Sin(t * 0.07f + b.ph) * 4, 0, Mathf.Cos(t * 0.05f + b.ph) * 4);
                    b.t.localPosition = V(p.x, Mathf.Max(h, -0.2f) + 0.9f + Mathf.Sin(t * 0.4f + b.ph) * 0.3f, p.z); Mats.SetAlpha(b.m, b.a * gather);
                }
                foreach (var hd in hands) { float k = gather * Mathf.Clamp01(Mathf.Sin(t * 0.8f + hd.ph) * 1.5f + 0.2f); hd.n.py = Mathf.Lerp(-2.4f, h - 0.6f, k); hd.n.rx = Mathf.Sin(t * 1.6f + hd.ph) * 0.25f; }
                if (h > 0 && rand < 0.6f) SpawnParticles(V((rand - 0.5f) * LB * 1.8f, h + 0.1f, (rand - 0.5f) * LB * 1.8f), 1, 0.5f, ashMat, 0.25f, 2.2f, -0.4f);
                if (!HazardLive || h < 0.1f) { stacks = 0; return; }
                hitT -= dt;
                if (P.pos.y < h - 0.15f)
                {
                    hazSlowT = 0.2f; P.vel.y -= 6 * dt;
                    if (hitT <= 0) { hitT = 0.7f; HazardHitPlayer(Mathf.Min(13, 7 + stacks * 1.5f), P.pos, Vector3.zero); stacks++; SpawnParticles(ChestPos, 6, 2, ashMat, 0.2f, 0.8f, -1); }
                }
                else if (hitT < -1.5f) stacks = 0;
                foreach (var e in enemies) if (!e.dead && e.type == "husk" && e.pos.y < h - 0.15f) { float k = Mathf.Exp(-4 * dt); e.vel.x *= k; e.vel.z *= k; }
            });
        }

        // ================= 17. CLOCKWORK HALLS =================
        readonly List<(Transform t, float sp)> pgGears = new List<(Transform, float)>();
        // a gear train climbing a wall: each gear meshes with the last (spacing from the radii) and turns the other way, at
        // the speed its size gives it
        void GearTrain(Vector3 origin, float ry, (float r, float ang)[] train, float sp0, Material a, Material b)
        {
            var q = Quaternion.Euler(0, ry * Mathf.Rad2Deg, 0); Vector2 c = Vector2.zero; float sp = sp0, lastR = 0;
            for (int i = 0; i < train.Length; i++)
            {
                var (r, ang) = train[i];
                if (i > 0) { c += new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (lastR + r) * 1.13f; sp = -sp * lastR / r; }
                var wp = origin + q * V(c.x, c.y, 0);
                var p = Mk.Piv(levelRoot, wp.x, wp.y, wp.z); p.SetRot(0, ry, 0); GearDeco(p.t, r, Mathf.Max(8, (int)(r * 3.2f)), i % 2 == 0 ? a : b, out var hub);
                Mk.Msh(p, Geo.Cyl(r * 0.18f, r * 0.18f, 1.6f, 10), M.metal, 0, 0, -0.4f, Mathf.PI / 2, 0, 0); // the axle into the wall
                pgGears.Add((hub, sp)); lastR = r;
            }
        }
        void BuildClockworkHalls()
        {
            pgGears.Clear(); Arena(60, 30);
            Altar(0, 0, 10, 3);
            var gold = Mats.Std(0xd8b860, 0.3f, 0.85f); var brass = Mats.Std(0x9a7a40, 0.4f, 0.8f, 0, RustTex()); var copper = Mats.Std(0xa0603a, 0.45f, 0.8f, 0, RustTex());
            var steelM = Mats.Std(0x5a5c60, 0.45f, 0.75f, 0, RustTex()); var darkM = Mats.Std(0x2a2826, 0.6f, 0.6f, 0, GrimeTex());
            // gear trains on all four walls
            GearTrain(V(-59.4f, 14, -40), Mathf.PI / 2, new[] { (9f, 0f), (4f, 0.4f), (6f, 1.4f), (3f, 0.2f), (5.5f, -0.5f) }, 0.3f, brass, copper);
            GearTrain(V(-59.4f, 6, 24), Mathf.PI / 2, new[] { (4.5f, 0f), (7f, 0.9f), (3.5f, -0.3f) }, -0.5f, copper, brass);
            GearTrain(V(59.4f, 16, 34), -Mathf.PI / 2, new[] { (10f, 0f), (4.5f, -0.6f), (6f, -1.6f), (3f, -0.2f) }, -0.25f, brass, gold);
            GearTrain(V(59.4f, 7, -26), -Mathf.PI / 2, new[] { (4.5f, 0f), (6.5f, 0.7f), (3.5f, 1.8f), (5f, 0.3f) }, 0.6f, copper, brass);
            GearTrain(V(-44, 9, 59.4f), Mathf.PI, new[] { (6f, 0f), (3.5f, 0.3f), (8f, 0.9f), (4f, 0.1f), (6f, -0.6f), (3f, 0.4f) }, 0.4f, brass, copper);
            GearTrain(V(-40, 8, -59.4f), 0, new[] { (5f, 0f), (3.5f, 0.6f), (6f, -0.2f) }, -0.35f, copper, brass);
            GearTrain(V(40, 8, -59.4f), 0, new[] { (5f, 0f), (3.5f, 2.5f), (6f, 3.3f) }, 0.35f, copper, brass);
            // the great clock over the far end: face, numerals, rim, hands, and the escapement rocking above it
            Deco(Geo.Circle(14, 48), Mats.Basic(0xffffff, ClockTex()), 0, 17, -59.5f, 0, Mathf.PI, 0);
            Deco(Geo.Torus(14.2f, 0.7f, 64, 6), gold, 0, 17, -59.3f); Deco(Geo.Torus(11.5f, 0.15f, 64, 4), brass, 0, 17, -59.35f);
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; Deco(Geo.Box(0.9f, 2.4f, 0.25f), darkM, Mathf.Sin(a) * 12.6f, 17 + Mathf.Cos(a) * 12.6f, -59.3f, 0, 0, -a); }
            var hh = Mk.Piv(levelRoot, 0, 17, -59.0f); Mk.Msh(hh, Geo.Box(0.9f, 8, 0.2f), darkM, 0, 4, 0); Mk.Msh(hh, Geo.Cone(0.9f, 1.4f, 4), darkM, 0, 8.6f, 0);
            var mh = Mk.Piv(levelRoot, 0, 17, -58.8f); Mk.Msh(mh, Geo.Box(0.55f, 12, 0.2f), darkM, 0, 6, 0); Mk.Msh(mh, Geo.Cone(0.6f, 1.2f, 4), darkM, 0, 12.5f, 0);
            Deco(Geo.Sphere(0.8f, 12, 8), gold, 0, 17, -58.6f);
            var esc = Mk.Piv(levelRoot, 0, 33, -59f); Mk.Msh(esc, Geo.Box(9, 0.6f, 0.4f), brass, 0, -2.2f, 0, 0, 0, 0); Mk.Msh(esc, Geo.Box(0.6f, 3, 0.4f), brass, -4.2f, -3.4f, 0, 0, 0, 0.5f); Mk.Msh(esc, Geo.Box(0.6f, 3, 0.4f), brass, 4.2f, -3.4f, 0, 0, 0, -0.5f); Mk.Msh(esc, Geo.Sphere(0.6f, 10, 8), gold);
            var escW = Mk.Piv(levelRoot, 0, 27.6f, -59.2f); GearDeco(escW.t, 2.6f, 15, gold, out var escHub);
            anims.Add((t, dt) =>
            {
                foreach (var g in pgGears) if (g.t) g.t.localRotation = Quaternion.Euler(0, 0, t * g.sp * Mathf.Rad2Deg);
                hh.rz = -t * 0.05f; mh.rz = -t * 0.6f;
                float sw = Mathf.Sin(t * 2.4f); esc.rz = sw * 0.16f; escHub.localRotation = Quaternion.Euler(0, 0, -Mathf.Floor(t * 2.4f / Mathf.PI) * 24);
            });
            // pistons pumping in niches along the side walls, and brass pipes with valve wheels venting steam
            foreach (var (x, z, ry) in new[] { (-59.2f, -8f, Mathf.PI / 2), (-59.2f, 40f, Mathf.PI / 2), (59.2f, 8f, -Mathf.PI / 2), (59.2f, -44f, -Mathf.PI / 2), (-10f, 59.2f, Mathf.PI), (24f, 59.2f, Mathf.PI) })
            {
                var q = Quaternion.Euler(0, ry * Mathf.Rad2Deg, 0); var f = q * Vector3.forward; var b = V(x, 0, z) + f * 0.9f;
                Deco(Geo.Box(3.6f, 0.8f, 1.8f), darkM, b.x, 0.4f, b.z, 0, ry, 0); Deco(Geo.Box(3.6f, 0.8f, 1.8f), darkM, b.x, 13.6f, b.z, 0, ry, 0);
                foreach (var s in new[] { -1f, 1f }) { var pp = b + q * V(s * 1.5f, 0, 0); Deco(Geo.Box(0.4f, 13, 0.4f), steelM, pp.x, 7, pp.z); }
                var cyl = Deco(Geo.Cyl(0.9f, 0.9f, 4, 14), copper, b.x, 2.8f, b.z); var rod = Mk.Piv(levelRoot, b.x, 4.8f, b.z); Mk.Msh(rod, Geo.Cyl(0.3f, 0.3f, 7, 10), steelM, 0, 3.5f, 0); Mk.Msh(rod, Geo.Box(2.2f, 0.6f, 1.2f), brass, 0, 7.2f, 0, 0, ry, 0);
                float ph = x * 0.13f + z * 0.07f; var vent = b + V(0, 4.8f, 0);
                anims.Add((t, dt) => { float k = 0.5f + 0.5f * Mathf.Sin(t * 1.6f + ph); rod.py = 4.8f + k * 1.6f; if (k < 0.06f && rand < 0.5f) SpawnParticles(vent, 3, 2, dustMat, 0.25f, 0.8f, -2); });
                SolidTurned(b, V(3.6f, 14, 1.8f), ry, 0.95f);
            }
            foreach (var (z0, z1, y, xw) in new[] { (-56f, 56f, 4.5f, -59.0f), (-56f, 56f, 22f, -59.0f), (-56f, 56f, 4.5f, 59.0f), (-56f, 56f, 22f, 59.0f) })
            {
                Deco(Geo.Cyl(0.35f, 0.35f, z1 - z0, 10), copper, xw, y, (z0 + z1) / 2, Mathf.PI / 2, 0, 0);
                for (float z = z0 + 6; z < z1; z += 12) { Deco(Geo.Torus(0.6f, 0.08f, 16, 4), brass, xw + Mathf.Sign(-xw) * 0.4f, y, z, 0, Mathf.PI / 2, 0); Deco(Geo.Cyl(0.45f, 0.45f, 0.4f, 10), brass, xw, y, z, Mathf.PI / 2, 0, 0); }
                var sp = V(xw + Mathf.Sign(-xw) * 0.5f, y, 0); float ph2 = y + xw;
                anims.Add((t, dt) => { if (Mathf.Sin(t * 0.5f + ph2) > 0.96f && rand < 0.6f) SpawnParticles(sp + V(0, 0, (rand - 0.5f) * 80), 2, 3, dustMat, 0.3f, 1.0f, -2); });
            }
            // the clockwork pillars: plinth, banded shaft, a gear collar turning round it, a capital carrying the girders
            foreach (var sx in new[] { -1f, 1f })
                for (int z = -40; z <= 40; z += 20) ClockPillar(sx * 22, z, gold, brass, darkM);
            foreach (var sx in new[] { -1f, 1f })
            {
                Box(sx * 22, 26, 0, 1.6f, 1.4f, 86, darkM); TrimRect(sx * 22, 0, 1.6f, 86, 27.4f);
                for (int z = -40; z <= 40; z += 20) { Box(sx * 41, 26.2f, z, 36, 1.0f, 1.2f, darkM); Ig(Deco(Geo.Box(0.4f, 5, 0.4f), steelM, sx * 30, 24, z, 0, 0, sx * 0.9f)); }
                Box(0, 27.4f, sx * 30, 45.6f, 0.9f, 1.0f, darkM);
            }
            // the raised gear platforms, the balconies (now on brackets and posts, with railings)
            foreach (var sx in new[] { -1, 1 })
            {
                Box(sx * 36, 0, 0, 13.6f, 4, 13.6f, M.tower); TrimRect(sx * 36, 0, 13.6f, 13.6f, 4);
                SolidAABB(V(sx * 36 - 5.4f, 4, -5.4f), V(sx * 36 + 5.4f, 4.6f, 5.4f)); // (the gear lying on top is solid: you stand on it, not in it)
                var gp = Mk.Piv(levelRoot, sx * 36, 4.3f, 0); gp.SetRot(Mathf.PI / 2, 0, 0); GearDeco(gp.t, 5.4f, 14, gold, out var gh); pgGears.Add((gh, sx * 0.4f));
                Box(sx * 48, 8, 30, 10, 1, 10, steelM); TrimRect(sx * 48, 30, 10, 10, 9);
                foreach (var cx in new[] { -1f, 1f }) foreach (var cz in new[] { -1f, 1f }) Box(sx * 48 + cx * 4.4f, 0, 30 + cz * 4.4f, 0.7f, 8, 0.7f, darkM);
                foreach (var cz in new[] { -1f, 1f }) Ig(Deco(Geo.Box(0.3f, 0.3f, 9), steelM, sx * 48 + sx * 4.4f, 4, 30, 0.8f * cz, 0, 0));
                for (int k = 0; k < 6; k++) { Ig(Deco(Geo.Box(0.1f, 1.0f, 0.1f), brass, sx * 48 - sx * 4.9f, 9.5f, 25.4f + k * 1.85f)); }
                Deco(Geo.Box(0.12f, 0.12f, 10), brass, sx * 48 - sx * 4.9f, 10.0f, 30); SolidAABB(V(sx * 48 - sx * 4.9f - 0.1f, 9, 25), V(sx * 48 - sx * 4.9f + 0.1f, 10.1f, 35));
                var bg = Mk.Piv(levelRoot, sx * 48, 9.15f, 30 + 3.5f); bg.SetRot(Mathf.PI / 2, 0, 0); GearDeco(bg.t, 1.6f, 10, brass, out var bgh); pgGears.Add((bgh, -sx * 1.0f)); SolidAABB(V(sx * 48 - 1.6f, 9, 31.9f), V(sx * 48 + 1.6f, 9.5f, 35.1f));
                PadAt(sx * 36, 16, 24);
            }
            PadAt(0, -44, 24);
            MechClockHands(19, 13);
            MechHammers(new[] { (V(-54, 0, -24), Mathf.PI / 2), (V(54, 0, -24), -Mathf.PI / 2), (V(-54, 0, 12), Mathf.PI / 2), (V(54, 0, 12), -Mathf.PI / 2), (V(-8, 0, 54), Mathf.PI), (V(8, 0, 54), Mathf.PI) });
        }
        void ClockPillar(float x, float z, Material gold, Material brass, Material dark)
        {
            Box(x, 0, z, 3.6f, 1.2f, 3.6f, dark); TrimRect(x, z, 3.6f, 3.6f, 1.2f);
            var g = Mk.Msh(levelRoot, Geo.Cyl(1.3f, 1.4f, 24.8f, 14), M.tower, x, 1.2f + 12.4f, z); g.layer = Mk.LayerLevel; g.AddComponent<BoxCollider>(); colliders.Add(new AABB(V(x - 1.4f, 1.2f, z - 1.4f), V(x + 1.4f, 26, z + 1.4f)));
            foreach (var y in new[] { 4f, 15f, 22f }) Ig(Deco(Geo.Torus(1.42f, 0.16f, 24, 4), brass, x, y, z, Mathf.PI / 2));
            Deco(Geo.Box(3.4f, 1.0f, 3.4f), dark, x, 25.5f, z); Deco(Geo.Box(2.8f, 0.5f, 2.8f), brass, x, 24.8f, z);
            var collar = Mk.Piv(levelRoot, x, 9.5f, z); collar.SetRot(Mathf.PI / 2, 0, 0); GearDeco(collar.t, 2.3f, 16, gold, out var ch); SolidAABB(V(x - 2.3f, 9.2f, z - 2.3f), V(x + 2.3f, 9.8f, z + 2.3f));
            foreach (var rr in collar.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
            pgGears.Add((ch, (x > 0 ? 1 : -1) * (z / 20 % 2 == 0 ? 0.5f : -0.5f)));
        }
        // ---------- THE MECHANISM (1): the floor clock's hands ----------
        // the dial's two hands lie at XII; every 14s a chime counts down three, the dial lights up, and the hands sweep a full
        // turn at knee height - the long one clockwise, the short one the other way. Jump them, or get up on something.
        void MechClockHands(float lenM, float lenH)
        {
            var gold = Mats.Std(0xd8b860, 0.3f, 0.85f); var dark = Mats.Std(0x1e1c1a, 0.5f, 0.6f);
            Deco(Geo.Torus(7, 0.35f, 48, 6), gold, 0, 0.2f, 0, Mathf.PI / 2); Ig(Deco(Geo.Ring(lenM - 0.2f, lenM + 0.4f, 72), gold, 0, 0.03f, 0, -Mathf.PI / 2));
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; Ig(Deco(Geo.Box(0.6f, 0.05f, 2.2f), dark, Mathf.Sin(a) * (lenM - 1.5f), 0.04f, Mathf.Cos(a) * (lenM - 1.5f), 0, a, 0)); }
            var ringM = Mats.Add(0xffd080, 0, null, true, true); var ring = Deco(Geo.Ring(7.4f, lenM + 0.2f, 72), ringM, 0, 0.06f, 0, -Mathf.PI / 2); ring.layer = Mk.LayerIgnore;
            var glowM = Mats.Add(0xffe0a0, 0, null, true, true);
            N Hand(float len, float w, float y)
            {
                var p = Mk.Piv(levelRoot, 0, y, 0);
                Mk.Msh(p, Geo.Box(w, 0.6f, len - 7.5f), dark, 0, 0, 7.5f + (len - 7.5f) / 2); Mk.Msh(p, Geo.Cone(w * 0.9f, 1.8f, 4), dark, 0, 0, len + 0.7f, Mathf.PI / 2, 0, 0);
                Mk.Msh(p, Geo.Box(w + 0.06f, 0.08f, len - 7.5f), gold, 0, 0.32f, 7.5f + (len - 7.5f) / 2); Mk.Msh(p, Geo.Box(w + 0.1f, 0.62f, len - 7.4f), glowM, 0, 0, 7.5f + (len - 7.5f) / 2);
                Mk.Msh(p, Geo.Torus(w * 1.2f, 0.12f, 16, 4), gold, 0, 0, 7.5f + (len - 7.5f) * 0.3f, Mathf.PI / 2, 0, 0);
                foreach (var rr in p.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                return p;
            }
            var hm = Hand(lenM, 1.1f, 0.75f); var hh = Hand(lenH, 1.6f, 0.8f); hh.SetRot(0, Mathf.PI, 0);
            float lastM = 0, lastH = Mathf.PI; int chimes = 0; bool hitM = false, hitH = false;
            trapAnims.Add((t, dt) =>
            {
                float u = t % 14f; const float W0 = 8.5f, S0 = 10.5f;
                int ch = u >= W0 ? Mathf.Min(3, 1 + (int)((u - W0) / 0.7f)) : 0;
                if (ch > chimes) { chimes = ch; Sfx("toll", 0.35f + ch * 0.1f); }
                if (u < W0) chimes = 0;
                float warn = u >= W0 && u < S0 ? (Mathf.Sin(t * 14) > 0 ? 1 : 0.5f) : 0, sweeping = u >= S0 && u < S0 + 4.2f ? 1 : 0;
                Mats.SetAlpha(ringM, warn * 0.22f + sweeping * 0.1f); Mats.SetAlpha(glowM, warn * 0.5f + sweeping * 0.35f);
                float am = u < S0 ? 0 : u < S0 + 3.6f ? Smoother((u - S0) / 3.6f) * Mathf.PI * 2 : 0;
                float ah = u < S0 ? Mathf.PI : u < S0 + 4.2f ? Mathf.PI - Smoother((u - S0) / 4.2f) * Mathf.PI * 2 : Mathf.PI;
                if (u < S0) { am = Mathf.Sin(t * 3) * 0.004f; hitM = hitH = false; }
                hm.SetRot(0, am, 0); hh.SetRot(0, ah, 0);
                if (sweeping > 0 && rand < 0.7f) { SpawnParticles(V(Mathf.Sin(am), 0, Mathf.Cos(am)) * (8 + rand * (lenM - 8)) + V(0, 0.2f, 0), 1, 2, sparkMat, 0.05f, 0.3f); }
                if (sweeping > 0 && (int)(u * 3) != (int)((u - dt) * 3)) Sfx("tick", 0.6f);
                float dM = am - lastM, dH = ah - lastH; lastM = am; lastH = ah;
                if (!HazardLive || sweeping <= 0) return;
                var o = P.pos; o.y = 0; float d = o.magnitude, pa = Mathf.Atan2(o.x, o.z);
                bool Swept(float a, float da, float len) => d > 7.2f && d < len + 1 && Mathf.Abs(Mathf.DeltaAngle(pa * Mathf.Rad2Deg, a * Mathf.Rad2Deg)) < Mathf.Max(6, Mathf.Abs(da) * Mathf.Rad2Deg + 4) * (8 / Mathf.Max(8, d)) * 1.6f;
                if (!hitM && P.pos.y < 1.2f && Swept(am, dM, lenM)) { hitM = true; var tan = V(Mathf.Cos(am), 0, -Mathf.Sin(am)); HazardHitPlayer(22, P.pos, -tan * 12 + V(0, 8, 0)); Sfx("slam", 0.6f); }
                if (!hitH && P.pos.y < 1.3f && Swept(ah, dH, lenH)) { hitH = true; var tan = V(Mathf.Cos(ah), 0, -Mathf.Sin(ah)); HazardHitPlayer(22, P.pos, tan * 12 + V(0, 8, 0)); Sfx("slam", 0.6f); }
                HazardHitEnemies(e => { var oe = e.pos; oe.y = 0; float de = oe.magnitude, ea = Mathf.Atan2(oe.x, oe.z); return e.pos.y < 1.5f && de > 7 && ((de < lenM + 1 && Mathf.Abs(Mathf.DeltaAngle(ea * Mathf.Rad2Deg, am * Mathf.Rad2Deg)) < 8) || (de < lenH + 1 && Mathf.Abs(Mathf.DeltaAngle(ea * Mathf.Rad2Deg, ah * Mathf.Rad2Deg)) < 8)); }, 12, e => V(0, 8, 0), 1.2f);
            });
        }
        // ---------- THE MECHANISM (2): trip-hammers ----------
        // great bell-hammers on iron frames near the walls: each one ratchets its arm up (clicking, the strike circle
        // lighting up on the floor), hangs a moment, and slams down - a shockwave, dust, and 26 to anything under it
        void MechHammers((Vector3 p, float ry)[] spots)
        {
            var iron = Mats.Std(0x2e2e30, 0.5f, 0.7f, 0, RustTex()); var brass = Mats.Std(0x9a7a40, 0.4f, 0.8f, 0, RustTex()); var gold = Mats.Std(0xd8b860, 0.3f, 0.85f);
            for (int i = 0; i < spots.Length; i++)
            {
                var (p, ry) = spots[i]; var q = Quaternion.Euler(0, ry * Mathf.Rad2Deg, 0); var f = q * Vector3.forward; var side = q * Vector3.right;
                const float AX = 3.4f, ARM = 9.5f;
                Box(p.x, 0, p.z, Mathf.Abs(side.x) * 5 + Mathf.Abs(f.x) * 3.4f, 1.4f, Mathf.Abs(side.z) * 5 + Mathf.Abs(f.z) * 3.4f, iron);
                foreach (var s in new[] { -1.9f, 1.9f }) { var pp = p + side * s; Box(pp.x, 1.4f, pp.z, 0.8f, AX + 0.6f, 0.8f, iron); }
                Deco(Geo.Cyl(0.35f, 0.35f, 4.6f, 10), brass, p.x, AX + 1.4f, p.z, 0, ry, Mathf.PI / 2);
                var arm = Mk.Piv(levelRoot, p.x, AX + 1.4f, p.z); arm.SetRot(0, ry, 0);
                var lift = Mk.Piv(arm); // rx: 0 = lying forward on the floor, negative = raised
                Mk.Msh(lift, Geo.Box(0.9f, 0.9f, ARM), iron, 0, 0, ARM / 2); Mk.Msh(lift, Geo.Box(0.95f, 0.2f, ARM), brass, 0, 0.5f, ARM / 2);
                Mk.Msh(lift, Geo.Cyl(1.7f, 1.7f, 3.4f, 18), iron, 0, -0.3f, ARM, 0, 0, Mathf.PI / 2); Mk.Msh(lift, Geo.Cyl(1.75f, 1.75f, 0.3f, 18), gold, 1.6f, -0.3f, ARM, 0, 0, Mathf.PI / 2); Mk.Msh(lift, Geo.Cyl(1.75f, 1.75f, 0.3f, 18), gold, -1.6f, -0.3f, ARM, 0, 0, Mathf.PI / 2);
                var ratchet = Mk.Piv(levelRoot, p.x + side.x * 2.6f, AX + 1.4f, p.z + side.z * 2.6f); ratchet.SetRot(0, ry + Mathf.PI / 2, 0); GearDeco(ratchet.t, 1.4f, 12, gold, out var rh); { var rp = p + side * 2.6f; SolidAABB(V(rp.x - 0.9f, AX + 1.4f - 1.6f, rp.z - 0.9f), V(rp.x + 0.9f, AX + 1.4f + 1.6f, rp.z + 0.9f)); }
                foreach (var rr in arm.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                const float SLAM = 0.30f; var strike = p + f * (ARM * Mathf.Cos(SLAM) - 0.3f * Mathf.Sin(SLAM)); strike.y = 0; // (at SLAM the head rests on the anvil plate: axle 4.8 up, head radius 1.7)
                var wm = Mats.Add(0xffb040, 0, null, true, true); var warn = Deco(Geo.Ring(3.4f, 4.2f, 40), wm, strike.x, 0.07f, strike.z, -Mathf.PI / 2); warn.layer = Mk.LayerIgnore;
                var wfill = Mats.Trans(0xffb040, 0, true); var wf = Deco(Geo.Circle(4.2f, 40), wfill, strike.x, 0.06f, strike.z, -Mathf.PI / 2); wf.layer = Mk.LayerIgnore;
                Ig(Deco(Geo.Circle(3.0f, 28), Mats.Std(0x2a2622, 0.95f), strike.x, 0.035f, strike.z, -Mathf.PI / 2)); // the anvil plate it pounds
                float off = i * 2.3f; bool slammed = false; int clicks = 0;
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 7.5f; float a;
                    // 0-2.6 ratchet up . 2.6-3.1 hold . 3.1-3.25 SLAM . 3.25-4.2 rest on the floor . 4.2-7.5 settle back to half-raised
                    if (u < 2.6f) a = Mathf.Lerp(-0.55f, -1.15f, Mathf.Floor(u / 0.26f) / 10f);
                    else if (u < 3.1f) a = -1.15f + Mathf.Sin(t * 40) * 0.01f;
                    else if (u < 3.25f) a = Mathf.Lerp(-1.15f, SLAM, (u - 3.1f) / 0.15f);
                    else if (u < 4.2f) a = SLAM + (u < 3.35f ? -Mathf.Sin((u - 3.25f) / 0.1f * Mathf.PI) * 0.05f : 0); // (a small bounce on the plate)
                    else a = Mathf.Lerp(SLAM, -0.55f, Smoother((u - 4.2f) / 3.3f));
                    lift.rx = a; rh.localRotation = Quaternion.Euler(0, 0, -a * 140);
                    int cl = u < 2.6f ? (int)(u / 0.26f) : -1; if (cl >= 0 && cl != clicks) { clicks = cl; Sfx("tick", 0.5f); }
                    float w = u >= 1.2f && u < 3.2f ? (u - 1.2f) / 2.0f : 0;
                    Mats.SetAlpha(wm, w > 0 ? 0.25f + 0.5f * w * (Mathf.Sin(t * 20) > 0 ? 1 : 0.6f) : 0); Mats.SetAlpha(wfill, w * 0.18f);
                    if (u < 3.1f) slammed = false;
                    if (u >= 3.25f && !slammed)
                    {
                        slammed = true; Sfx("slam", 0.9f); Sfx("clank", 0.6f); ShockRing(strike + V(0, 0.2f, 0), 6.5f, 0xffb040, 0.4f); SpawnParticles(strike + V(0, 0.4f, 0), 28, 9, dustMat, 0.4f, 1.0f); SpawnParticles(strike + V(0, 0.4f, 0), 14, 12, sparkMat, 0.06f, 0.5f);
                        float dd = Vector3.Distance(P.pos, strike); if (dd < 18) P.shake = Mathf.Max(P.shake, 0.6f * (1 - dd / 18));
                        if (HazardLive && PlayerIn(strike, 4.2f, -1, 3.2f)) { var o = P.pos - strike; o.y = 0; HazardHitPlayer(26, strike, o.normalized * 13 + V(0, 9, 0)); }
                        HazardHitEnemies(e => Hypot(e.pos.x - strike.x, e.pos.z - strike.z) < 4.2f + e.hitR * 0.4f && e.pos.y < 3, 14, e => { var o = e.pos - strike; o.y = 0; return o.normalized * 10 + V(0, 8, 0); });
                    }
                });
            }
        }

        // ================= 18. THE LIBRARY OF SOULS =================
        Material[] pgBookM; Material pgWoodM, pgWoodDM, pgGoldM, pgPageM, pgUpperM;
        void LibMats()
        {
            int[] cols = { 0x6a1e16, 0x1e3a5e, 0x3a4a1e, 0x6e5a2a, 0x2e1e40, 0x7a6a56, 0x4e2a16, 0x1a1a1a, 0x8a2a2a, 0x2a4a4a };
            pgBookM = new Material[cols.Length]; for (int i = 0; i < cols.Length; i++) pgBookM[i] = Mats.Std(cols[i], 0.85f);
            pgWoodM = Mats.Std(0x4a3020, 0.75f, 0, 0, GrimeTex()); pgWoodDM = Mats.Std(0x2a1a10, 0.7f, 0, 0, GrimeTex()); pgGoldM = Mats.Std(0xd8b860, 0.3f, 0.85f);
            pgPageM = Mats.Std(0xe8e0cc, 0.9f, 0, 0x101008, null, true); pgUpperM = Mats.Basic(0xb8a890, BooksTex());
        }
        // a bookcase: carcass (sides, crown, plinth, back, shelves) and real books on every shelf up to eye height - leaning,
        // stacked, gaps here and there; the shelves above are painted. Local x = its length, z = its depth.
        void Bookcase(float x, float z, float len, float h, float ry, float depth = 1.5f, bool both = true, bool solid = true, int seed = 0)
        {
            // every case is built the same way, top to bottom: a plinth, real books on every shelf, fluted pilasters with gilt
            // capitals between the bays, a dentilled crown with a gilt plate over each bay
            var q = Quaternion.Euler(0, ry * Mathf.Rad2Deg, 0); var c = V(x, 0, z); var rng = new System.Random(seed + (int)(x * 31 + z * 17));
            float R() => (float)rng.NextDouble();
            var wd = Batch(pgWoodM); var wdD = Batch(pgWoodDM); var gd = Batch(pgGoldM);
            MBBox(wdD, c + q * V(0, 0.2f, 0), V(len + 0.2f, 0.4f, depth + 0.2f), q);                        // plinth
            MBBox(wd, c + q * V(0, 0.42f, 0), V(len + 0.28f, 0.06f, depth + 0.26f), q);
            MBBox(wd, c + q * V(0, h + 0.12f, 0), V(len + 0.3f, 0.24f, depth + 0.3f), q);                  // crown
            MBBox(wdD, c + q * V(0, h + 0.34f, 0), V(len + 0.45f, 0.2f, depth + 0.45f), q);
            MBBox(wdD, c + q * V(0, h / 2, 0), V(len, h, 0.08f), q);                                         // the back (or the spine between two faces)
            int bays = Mathf.Max(1, Mathf.RoundToInt(len / 2.6f)); float bw = len / bays;
            for (int fs = both ? -1 : 1; fs <= 1; fs += 2)
            {
                float fz = fs * (depth / 2 + 0.06f);
                MBBox(gd, c + q * V(0, h - 0.02f, fs * (depth / 2 + 0.16f)), V(len + 0.3f, 0.05f, 0.03f), q);
                for (int k = 0; k < (int)(len / 0.35f); k++) MBBox(wdD, c + q * V(-len / 2 + 0.17f + k * 0.35f, h + 0.08f, fs * (depth / 2 + 0.17f)), V(0.14f, 0.1f, 0.06f), q); // dentils
                for (int b = 0; b <= bays; b++)
                {
                    float px = -len / 2 + b * bw;
                    MBBox(wd, c + q * V(px, h / 2, fz), V(0.22f, h - 0.5f, 0.12f), q);                    // pilaster
                    MBBox(wdD, c + q * V(px, h / 2, fz + fs * 0.05f), V(0.06f, h - 0.9f, 0.04f), q);      // its flute
                    MBBox(gd, c + q * V(px, h - 0.3f, fz), V(0.3f, 0.14f, 0.16f), q);                     // gilt capital
                    MBBox(gd, c + q * V(px, 0.55f, fz), V(0.28f, 0.08f, 0.15f), q);
                }
                for (int b = 0; b < bays; b++) MBBox(gd, c + q * V(-len / 2 + (b + 0.5f) * bw, h + 0.12f, fs * (depth / 2 + 0.16f)), V(0.5f, 0.14f, 0.02f), q); // the plate over each bay
            }
            for (int b = 0; b <= bays; b++) MBBox(wd, c + q * V(-len / 2 + b * bw, h / 2, 0), V(0.16f, h, depth + 0.06f), q);
            const float RH = 1.45f; int rows = Mathf.Max(1, (int)((h - 0.5f) / RH));
            float rh = (h - 0.5f) / rows; // (the rows share the height evenly, so the top row reaches the crown)
            for (int rI = 0; rI <= rows; rI++) MBBox(wd, c + q * V(0, 0.4f + rI * rh, 0), V(len, 0.08f, depth), q);
            for (int fside = both ? -1 : 1; fside <= 1; fside += 2)
                for (int rI = 0; rI < rows; rI++)
                {
                    float y0 = 0.44f + rI * rh, dz = depth / 2 - 0.1f;
                    for (int b = 0; b < bays; b++)
                    {
                        float x0 = -len / 2 + b * bw + 0.14f, x1 = x0 + bw - 0.28f, bx = x0;
                        while (bx < x1 - 0.07f)
                        {
                            float roll = R();
                            if (roll < 0.04f) { bx += 0.25f + R() * 0.35f; continue; } // a gap
                            var m = Batch(pgBookM[rng.Next(pgBookM.Length)]);
                            if (roll < 0.1f && x1 - bx > 0.5f)
                            {
                                // a stack lying flat
                                int n = 2 + rng.Next(4); float sy = y0;
                                for (int k = 0; k < n; k++) { float th = 0.06f + R() * 0.05f, w = 0.36f + R() * 0.12f; MBBox(Batch(pgBookM[rng.Next(pgBookM.Length)]), c + q * V(bx + 0.25f, sy + th / 2, fside * (dz * 0.5f)), V(w, th, dz * 0.85f), q * Quaternion.Euler(0, (R() - 0.5f) * 12, 0)); sy += th; }
                                bx += 0.55f; continue;
                            }
                            float bwid = 0.07f + R() * 0.11f, bh = rh * (0.62f + R() * 0.28f), bd = dz * (0.78f + R() * 0.18f);
                            float lean = roll > 0.95f ? 14 + R() * 10 : 0;
                            var ctr = V(bx + bwid / 2 + (lean > 0 ? bh * 0.15f : 0), y0 + bh / 2 * (lean > 0 ? 0.97f : 1), fside * (dz - bd / 2 + 0.02f));
                            MBBox(m, c + q * ctr, V(bwid, bh, bd), q * Quaternion.Euler(0, 0, -lean));
                            if (roll > 0.5f && roll < 0.72f) { MBBox(gd, c + q * (ctr + V(0, bh * 0.3f, fside * bd / 2)), V(bwid + 0.004f, 0.022f, 0.01f), q); MBBox(gd, c + q * (ctr + V(0, -bh * 0.3f, fside * bd / 2)), V(bwid + 0.004f, 0.022f, 0.01f), q); }
                            bx += bwid + 0.006f + (lean > 0 ? bh * 0.3f : 0);
                        }
                    }
                }
            if (solid) SolidTurned(c, V(len + 0.45f, h + 0.45f, depth + 0.45f), ry, 1f);
        }        void BuildLibrary()
        {
            LibMats(); Arena(62, 24);
            Altar(0, 0, 10, 3);
            var gold = pgGoldM; var cloth = Mats.Std(0x5a1a14, 0.95f, 0, 0, null, true);
            // the floor: a long carpet down each aisle, a round one in the rotunda
            Flat(Geo.Circle(15, 48), Mats.Std(0x4a1a14, 0.95f, 0, 0, GrimeTex()), 0, 0.03f, 0); Flat(Geo.Ring(14.6f, 15.2f, 48), gold, 0, 0.04f, 0);
            foreach (var sz in new[] { -1f, 1f }) Ig(Deco(Geo.Box(5, 0.02f, 40), Mats.Std(0x4a1a14, 0.95f, 0, 0, GrimeTex()), 0, 0.03f, sz * 36));
            Ig(Deco(Geo.Box(96, 0.02f, 4.4f), Mats.Std(0x3a1a14, 0.95f, 0, 0, GrimeTex()), 0, 0.03f, 0));
            // the rotunda reading room: a ring of low cases round the altar, lecterns, a reading table
            for (int k = 0; k < 8; k++)
            {
                if (k == 0 || k == 4) continue; // (gaps on the axis for the aisles)
                float a = k * Mathf.PI / 4 + Mathf.PI / 8, rr = 13; float cx = Mathf.Sin(a) * rr, cz = Mathf.Cos(a) * rr;
                Bookcase(cx, cz, 6.5f, 2.6f, a + Mathf.PI / 2, 1.2f, true, false, k);
                for (int s = -1; s <= 1; s++) { var sp = V(cx, 0, cz) + Quaternion.Euler(0, (a + Mathf.PI / 2) * Mathf.Rad2Deg, 0) * V(s * 2.1f, 0, 0); SolidAABB(sp - V(1.0f, 0, 1.0f), sp + V(1.0f, 2.9f, 1.0f)); }
            }
            foreach (var (x, z, ry) in new[] { (-8.5f, 6f, 0.9f), (8.5f, 6f, -0.9f), (-8.5f, -6f, 2.2f), (8.5f, -6f, -2.2f) }) Lectern(x, z, ry);
            // the aisles: tall double-sided cases running the length of the hall, taller toward the walls; the outermost are deep
            // enough to walk along the top of, with stairs up to them from the cross aisle
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                {
                    Bookcase(sx * 20, sz * 25, 34, 6.2f, Mathf.PI / 2, 1.5f, true, true, (int)(sx * 3 + sz * 7));
                    Bookcase(sx * 30, sz * 25, 34, 8.2f, Mathf.PI / 2, 1.5f, true, true, (int)(sx * 5 + sz * 11));

                    // rolling ladders against the cases
                    var lp = V(sx * 30 - sx * 1.1f, 0, sz * (14 + (sx > 0 ? 8 : 0))); var lad = Mk.Piv(levelRoot, lp.x, 0, lp.z); lad.SetRot(0, 0, sx * 0.2f);
                    foreach (var s in new[] { -0.35f, 0.35f }) Mk.Msh(lad, Geo.Box(0.08f, 8.4f, 0.08f), pgWoodM, 0, 4.2f, s);
                    for (int k = 0; k < 14; k++) Mk.Msh(lad, Geo.Box(0.06f, 0.06f, 0.7f), pgWoodM, 0, 0.4f + k * 0.58f, 0);
                    foreach (var rr in lad.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                }
            foreach (var sx in new[] { -1f, 1f })
            {
                // the stair from the cross aisle up onto the outer stacks (every step built down to the floor: solid, no gaps)
                for (int k = 0; k < 16; k++)
                {
                    float x = sx * (25.4f + k * 1.08f), top = 0.625f * (k + 1);
                    Box(x, 0, 0, 1.1f, top, 4.4f, pgWoodDM); Ig(Deco(Geo.Box(1.12f, 0.08f, 4.5f), pgWoodM, x, top + 0.02f, 0));
                }
                foreach (var sz in new[] { -1f, 1f }) for (int k = 0; k < 16; k += 2) Ig(Deco(Geo.Box(0.08f, 1.1f, 0.08f), gold, sx * (25.4f + k * 1.08f), 0.625f * (k + 1) + 0.55f, sz * 2.3f));
                foreach (var sz in new[] { -1f, 1f }) { var a = V(sx * 25.4f, 0.625f + 1.1f, sz * 2.3f); var b = V(sx * (25.4f + 14 * 1.08f), 0.625f * 15 + 1.1f, sz * 2.3f); var d = b - a; var rail = Deco(Geo.Box(0.1f, 0.1f, d.magnitude), gold, (a.x + b.x) / 2, (a.y + b.y) / 2, sz * 2.3f); rail.transform.localRotation = Quaternion.LookRotation(d.normalized); Ig(rail); } // (the handrail along the line of the post tops)
                // the great stacks along each side wall: one solid mass of shelving from the aisle right back to the wall (nothing
                // to slip behind), faced with cases on the aisle side and both ends, and a gallery walk along its top
                Bookcase(sx * 43.25f, 0, 84, 10, sx > 0 ? -Mathf.PI / 2 : Mathf.PI / 2, 1.5f, false, true, (int)(sx * 9));
                foreach (var sz in new[] { -1f, 1f }) Bookcase(sx * 52.25f, sz * 42.75f, 19.5f, 10, sz > 0 ? 0 : Mathf.PI, 1.5f, false, true, (int)(sx * 5 + sz * 17));
                Box(sx * 53, 0, 0, 18, 10, 84, pgWoodDM);
                Ig(Deco(Geo.Box(19, 0.04f, 86), Mats.Std(0x4a1a14, 0.95f, 0, 0, GrimeTex()), sx * 52.6f, 10.52f, 0));
                foreach (var (z0, z1) in new[] { (-43.4f, -2.6f), (2.6f, 43.4f) })
                {
                    float rx = sx * 42.75f; Ig(Deco(Geo.Box(0.1f, 0.1f, z1 - z0), pgGoldM, rx, 11.55f, (z0 + z1) / 2));
                    for (float zz = z0; zz <= z1 + 0.01f; zz += 2.05f) Ig(Deco(Geo.Box(0.08f, 1.0f, 0.08f), pgGoldM, rx, 11.05f, zz));
                    SolidAABB(V(rx - 0.1f, 10.5f, z0), V(rx + 0.1f, 11.6f, z1));
                }
                foreach (var zz in new[] { -30f, -14f, 14f, 30f }) Candelabra(V(sx * 56, 10.5f, zz));
                PadAt(sx * 10, -30, 24);
            }
            foreach (var sz in new[] { -1f, 1f }) { Bookcase(0, sz * 60.6f, 84, 14, sz > 0 ? Mathf.PI : 0, 1.2f, false, false, (int)(sz * 41)); SolidAABB(V(-62, 0, sz > 0 ? 59.6f : -62), V(62, 14.9f, sz > 0 ? 62 : -59.6f)); } // (solid right back to the wall)
            // a fallen case, books spilled across the floor
            {
                var fc = Mk.Piv(levelRoot, -8, 0.8f, -38); fc.SetRot(0, 0.5f, Mathf.PI / 2 - 0.06f);
                Mk.Msh(fc, Geo.Box(7, 1.5f, 1.4f), pgWoodM, 0, 0, 0); Mk.Msh(fc, Geo.Box(7, 1.5f, 0.08f), pgWoodDM, 0, 0, 0.75f);
                SolidTurned(V(-8, 0, -38), V(7, 1.5f, 1.6f), 0.5f, 0.9f);
                for (int k = 0; k < 40; k++) { var bp = V(-8 + (rand - 0.5f) * 9, 0, -38 + 2 + rand * 4); MBBox(Batch(pgBookM[k % pgBookM.Length]), bp + V(0, 0.05f, 0), V(0.4f, 0.08f + rand * 0.04f, 0.28f), Quaternion.Euler((rand - 0.5f) * 30, rand * 360, (rand - 0.5f) * 30)); }
            }
            // book piles, globes, candelabra, scroll racks, study tables
            foreach (var p in new[] { V(-14, 0, 16), V(13, 0, -18), V(-36, 0, 50), V(36, 0, -50), V(-25, 0, -2), V(25, 0, 3), V(-4, 0, 52), V(6, 0, -52) }) BookPile(p);
            foreach (var p in new[] { V(-18, 0, -44), V(18, 0, 46), V(-48, 0, 51) }) Globe(p);
            foreach (var p in new[] { V(-14, 0, 0), V(14, 0, 0), V(0, 0, 14), V(0, 0, -14), V(-36, 0, 52), V(36, 0, 52), V(-36, 0, -52), V(36, 0, -52) }) Candelabra(p);
            foreach (var p in new[] { V(-26, 0, 52), V(26, 0, -52) }) ScrollRack(p);
            foreach (var p in new[] { V(-10, 0, 46), V(10, 0, -46) }) StudyTable(p);
            // pages: a drift of them across the floor, and some still adrift in the air
            var pg = Batch(pgPageM);
            for (int i = 0; i < 360; i++)
            {
                float x = (rand - 0.5f) * 116, z = (rand - 0.5f) * 116; var p = V(x, 0.05f + rand * 0.02f, z); if (PointInLevel(p + V(0, 0.2f, 0))) continue;
                var rq = Quaternion.Euler((rand - 0.5f) * 10, rand * 360, (rand - 0.5f) * 10);
                MBQuad(pg, p, rq * V(0.11f, 0, 0), rq * V(0, 0, 0.15f));
            }
            for (int i = 0; i < 40; i++)
            {
                var b = Deco(Geo.Box(0.5f, 0.12f, 0.7f), pgBookM[i % pgBookM.Length], (rand - 0.5f) * 100, 6 + rand * 14, (rand - 0.5f) * 100, rand, rand * 3, rand); b.layer = Mk.LayerIgnore;
                var bt = b.transform; float ph = rand * 6, y0 = bt.localPosition.y; anims.Add((t, dt) => { var p = bt.localPosition; p.y = y0 + Mathf.Sin(t * 0.6f + ph) * 0.6f; bt.localPosition = p; bt.Rotate(0, dt * 15, 0); });
            }
            for (int i = 0; i < 50; i++)
            {
                var pp = Deco(Geo.Plane(0.22f, 0.3f), pgPageM, (rand - 0.5f) * 100, 3 + rand * 16, (rand - 0.5f) * 100, rand * 3, rand * 3, rand * 3); pp.layer = Mk.LayerIgnore;
                var pt = pp.transform; var p0 = pt.localPosition; float ph = rand * 6;
                anims.Add((t, dt) => { pt.localPosition = p0 + V(Mathf.Sin(t * 0.3f + ph) * 2, Mathf.Sin(t * 0.7f + ph) * 0.8f, Mathf.Cos(t * 0.25f + ph) * 2); pt.Rotate(dt * 40, dt * 25, dt * 10); });
            }
            FlushBatches();
            MechGrimoires(3);
        }
        void Lectern(float x, float z, float ry)
        {
            var p = Mk.Piv(levelRoot, x, 0, z); p.SetRot(0, ry, 0);
            Mk.Msh(p, Geo.Box(1.0f, 0.2f, 1.0f), pgWoodDM, 0, 0.1f, 0); Mk.Msh(p, Geo.Cyl(0.12f, 0.16f, 1.1f, 8), pgWoodM, 0, 0.75f, 0);
            Mk.Msh(p, Geo.Box(0.9f, 0.08f, 0.7f), pgWoodM, 0, 1.35f, 0, -0.35f, 0, 0);
            foreach (var s in new[] { -1f, 1f }) Mk.Msh(p, Geo.Box(0.4f, 0.03f, 0.52f), pgPageM, s * 0.21f, 1.42f, 0, -0.35f, 0, s * 0.08f);
            var g = Mk.Glow(p.t, 0xffe0a0, 1.2f, 0.55f); g.transform.localPosition = V(0, 1.55f, 0.05f);
            SolidAABB(V(x - 0.5f, 0, z - 0.5f), V(x + 0.5f, 1.4f, z + 0.5f));
        }
        void BookPile(Vector3 p)
        {
            int n = 3 + (int)(rand * 3);
            for (int s = 0; s < n; s++) { float y = 0; var o = V((rand - 0.5f) * 1.6f, 0, (rand - 0.5f) * 1.6f); int k = 3 + (int)(rand * 7); for (int i = 0; i < k; i++) { float th = 0.07f + rand * 0.05f; MBBox(Batch(pgBookM[(int)(rand * pgBookM.Length) % pgBookM.Length]), p + o + V(0, y + th / 2, 0), V(0.42f + rand * 0.15f, th, 0.3f + rand * 0.08f), Quaternion.Euler(0, rand * 40 - 20 + s * 30, 0)); y += th; } }
        }
        void Globe(Vector3 p)
        {
            Deco(Geo.Cyl(0.3f, 0.45f, 0.2f, 12), pgWoodDM, p.x, 0.1f, p.z); Deco(Geo.Cyl(0.08f, 0.1f, 1.0f, 8), pgWoodM, p.x, 0.7f, p.z);
            var g = Mk.Piv(levelRoot, p.x, 1.55f, p.z); g.SetRot(0, 0, 0.4f); Mk.Msh(g, Geo.Sphere(0.55f, 18, 12), Mats.Std(0x3a5a4a, 0.6f, 0, 0, GrimeTex())); Mk.Msh(g, Geo.Torus(0.62f, 0.03f, 28, 4), pgGoldM, 0, 0, 0, 0, Mathf.PI / 2, 0);
            anims.Add((t, dt) => g.t.Rotate(0, dt * 8, 0, Space.Self)); SolidAABB(V(p.x - 0.5f, 0, p.z - 0.5f), V(p.x + 0.5f, 2.1f, p.z + 0.5f));
        }
        void Candelabra(Vector3 p)
        {
            float y = p.y;
            Deco(Geo.Cyl(0.35f, 0.5f, 0.15f, 10), pgGoldM, p.x, y + 0.08f, p.z); Deco(Geo.Cyl(0.06f, 0.08f, 2.4f, 8), pgGoldM, p.x, y + 1.25f, p.z);
            Ig(Deco(Geo.Torus(0.5f, 0.04f, 20, 4, Mathf.PI), pgGoldM, p.x, y + 2.5f, p.z, Mathf.PI, 0, 0));
            foreach (var dx in new[] { -0.5f, 0f, 0.5f }) { Deco(Geo.Cyl(0.05f, 0.05f, 0.3f, 6), pgPageM, p.x + dx, y + (dx == 0 ? 2.65f : 2.55f), p.z); var g = Mk.Glow(levelRoot, 0xffd890, 0.9f, 0.85f); g.transform.localPosition = V(p.x + dx, y + (dx == 0 ? 2.9f : 2.8f), p.z); flames.Add(g.transform); }
            SolidAABB(V(p.x - 0.3f, y, p.z - 0.3f), V(p.x + 0.3f, y + 2.6f, p.z + 0.3f));
        }
        void ScrollRack(Vector3 p)
        {
            Box(p.x, 0, p.z, 3.2f, 3.0f, 1.2f, pgWoodDM);
            for (int r = 0; r < 4; r++) for (int c = 0; c < 6; c++) Ig(Deco(Geo.Cyl(0.16f, 0.16f, 1.3f, 8), pgPageM, p.x - 1.3f + c * 0.52f, 0.5f + r * 0.7f, p.z, Mathf.PI / 2, 0, 0));
        }
        void StudyTable(Vector3 p)
        {
            Box(p.x, 0.85f, p.z, 4, 0.12f, 2, pgWoodM);
            foreach (var cx in new[] { -1.8f, 1.8f }) foreach (var cz in new[] { -0.8f, 0.8f }) Deco(Geo.Box(0.14f, 0.85f, 0.14f), pgWoodDM, p.x + cx, 0.42f, p.z + cz);
            SolidAABB(V(p.x - 2, 0, p.z - 1), V(p.x + 2, 0.85f, p.z + 1));
            BookPile(p + V(-1.2f, 0.97f, 0)); Ig(Deco(Geo.Box(0.9f, 0.02f, 0.6f), pgPageM, p.x + 0.8f, 0.98f, p.z, 0, 0.3f, 0));
            var g = Mk.Glow(levelRoot, 0xffd890, 1.0f, 0.8f); g.transform.localPosition = p + V(1.5f, 1.3f, 0.5f); flames.Add(g.transform); Ig(Deco(Geo.Cyl(0.05f, 0.05f, 0.3f, 6), pgPageM, p.x + 1.5f, 1.1f, p.z + 0.5f));
            foreach (var cz in new[] { -1.6f, 1.6f }) { Deco(Geo.Box(0.6f, 0.08f, 0.6f), pgWoodM, p.x, 0.55f, p.z + cz); Deco(Geo.Box(0.6f, 0.9f, 0.08f), pgWoodM, p.x, 1.0f, p.z + cz * 1.18f); }
        }
        // ---------- HAUNTED STACKS (1): the grimoires ----------
        // great open books drifting high over the stacks; every ~9s one turns to you: its pages riffle, its runes light, a
        // thread of gold traces you - then it hurls a volley of six books (dodge sideways; 9 each, they hit enemies too)
        class FlyBook { public N t, l, r; public Vector3 v; public float life; }
        readonly List<FlyBook> pgBooks = new List<FlyBook>();
        void MechGrimoires(int n)
        {
            pgBooks.Clear();
            var cover = Mats.Std(0x3a0e0a, 0.7f, 0, 0x100000); var runeM = Mats.Add(0xffc060, 0.0f, null, true, true); var lineM = Mats.Add(0xffd080, 0, null, true, true);
            var unit = Geo.Cyl(1, 1, 1, 6);
            for (int i = 0; i < n; i++)
            {
                var g = Mk.Piv(levelRoot, 0, 8, 0); float ph = i * 2.1f, off = i * 3.0f;
                foreach (var s in new[] { -1f, 1f }) { var cv = Mk.Piv(g); cv.SetRot(0, 0, s * 0.35f); Mk.Msh(cv, Geo.Box(1.5f, 0.1f, 2.1f), cover, s * 0.75f, 0, 0); Mk.Msh(cv, Geo.Box(1.4f, 0.22f, 1.95f), pgPageM, s * 0.72f, 0.15f, 0); Mk.Msh(cv, Geo.Box(1.2f, 0.02f, 1.6f), runeM, s * 0.72f, 0.27f, 0); Mk.Msh(cv, Geo.Box(0.06f, 0.12f, 2.15f), pgGoldM, s * 1.48f, 0, 0); }
                var flips = new List<N>(); for (int k = 0; k < 4; k++) { var fp = Mk.Piv(g, 0, 0.28f, 0); Mk.Msh(fp, Geo.Box(1.35f, 0.01f, 1.9f), pgPageM, 0.7f, 0, 0); flips.Add(fp); }
                var gl = Mk.Glow(g, 0xffc060, 4, 0.0f); var glM = gl.GetComponent<Renderer>().sharedMaterial;
                foreach (var rr in g.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                var line = Deco(unit, lineM, 0, 0, 0); line.layer = Mk.LayerIgnore; line.SetActive(false);
                int fired = 0; Vector3 aim = Vector3.zero;
                trapAnims.Add((t, dt) =>
                {
                    float lim = LB - 14; var c = V(Mathf.Sin(t * 0.09f + ph) * lim, 8 + Mathf.Sin(t * 0.6f + ph) * 0.8f, Mathf.Sin(t * 0.13f + ph * 1.7f) * lim);
                    g.t.localPosition = c;
                    float u = (t + off) % 9f; bool reading = u >= 5.4f && u < 7.0f, volley = u >= 7.0f && u < 8.6f;
                    var to = ChestPos - c; float yaw = Mathf.Atan2(to.x, to.z);
                    g.t.localRotation = Quaternion.Slerp(g.t.localRotation, Quaternion.Euler(-25, yaw * Mathf.Rad2Deg + 90, Mathf.Sin(t + ph) * 6), 1 - Mathf.Exp(-3 * dt));
                    for (int k = 0; k < flips.Count; k++) flips[k].rz = reading || volley ? Mathf.PingPong(t * 6 + k * 0.4f, Mathf.PI * 0.9f) : 0.1f * k;
                    float glow = reading ? (u - 5.4f) / 1.6f : volley ? 1 : 0; Mats.SetAlpha(runeM, 0.15f + 0.85f * glow); Mats.SetAlpha(glM, 0.5f * glow);
                    if (u < 5.4f) fired = 0;
                    bool sees = HazardLive && Los(c, ChestPos);
                    if (reading && sees)
                    {
                        if ((int)(u * 4) != (int)((u - dt) * 4)) Sfx("chant", 0.4f);
                        aim = Vector3.Lerp(aim, ChestPos, 1 - Mathf.Exp(-4 * dt)); var d = aim - c; float L = d.magnitude;
                        line.SetActive(true); line.transform.localPosition = c + d / 2; line.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d / L); line.transform.localScale = V(0.03f, L, 0.03f);
                        Mats.SetAlpha(lineM, 0.25f + 0.35f * glow * (Mathf.Sin(t * 30) > 0 ? 1 : 0.5f));
                    }
                    else { line.SetActive(false); if (!reading) aim = ChestPos; }
                    if (volley && sees && fired < 6 && u >= 7.0f + fired * 0.25f)
                    {
                        fired++; var lead = ChestPos + V(P.vel.x, 0, P.vel.z) * 0.4f * (fired % 2) + V((rand - 0.5f) * 1.2f, 0, (rand - 0.5f) * 1.2f);
                        LaunchBook(c, (lead - c).normalized * 24, cover); Sfx("dash", 0.6f);
                    }
                });
            }
            trapAnims.Add((t, dt) =>
            {
                for (int i = pgBooks.Count - 1; i >= 0; i--)
                {
                    var b = pgBooks[i]; if (b.t == null || !b.t.t) { pgBooks.RemoveAt(i); continue; }
                    b.life += dt; var p = b.t.t.localPosition + b.v * dt; b.t.t.localPosition = p;
                    float flap = Mathf.Sin(b.life * 26) * 0.6f; b.l.rz = 0.5f + flap; b.r.rz = -0.5f - flap;
                    bool hit = false;
                    if (HazardLive && Vector3.Distance(p, ChestPos) < 1.0f) { HazardHitPlayer(9, p, b.v.normalized * 5 + V(0, 2, 0)); hit = true; }
                    if (!hit) foreach (var e in enemies) if (!e.dead && e.type != "boss" && e.spawnT <= 0 && Vector3.Distance(p, BodyPoint(e)) < e.hitR + 0.4f) { HazardHitEnemies(o => o == e, 6, o => b.v.normalized * 6, 0.2f); hit = true; break; }
                    if (hit || b.life > 4 || PointInLevel(p) || p.y < 0)
                    {
                        SpawnParticles(p, 10, 5, ashMat, 0.15f, 0.8f, -1); for (int k = 0; k < 4; k++) SpawnParticles(p, 1, 4, goldSpark, 0.08f, 0.5f);
                        Destroy(b.t.go); pgBooks.RemoveAt(i);
                    }
                }
            });
        }
        void LaunchBook(Vector3 from, Vector3 v, Material cover)
        {
            var root = Mk.Piv(levelRoot, from.x, from.y, from.z); root.t.localRotation = Quaternion.LookRotation(v) * Quaternion.Euler(0, 90, 0);
            var l = Mk.Piv(root); Mk.Msh(l, Geo.Box(0.5f, 0.05f, 0.7f), cover, 0.25f, 0, 0); var r = Mk.Piv(root); Mk.Msh(r, Geo.Box(0.5f, 0.05f, 0.7f), cover, -0.25f, 0, 0);
            Mk.Msh(root, Geo.Box(0.06f, 0.2f, 0.66f), pgPageM, 0, 0, 0);
            foreach (var rr in root.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
            pgBooks.Add(new FlyBook { t = root, l = l, r = r, v = v });
        }
        // ---------- HAUNTED STACKS (2): page storms ----------
        // two whirlwinds of torn pages wander set paths - one circling the rotunda, one sweeping the far ends; inside, the
        // pages cut (6 every half second), the wind spins you round and lifts you, and you're slowed
        void MechPageStorms()
        {
            for (int i = 0; i < 2; i++)
            {
                const float R = 4.2f, H = 8; int idx = i;
                var root = Mk.Piv(levelRoot, 0, 0, 0); var pages = new List<(N n, float r, float y, float a, float sp)>();
                for (int k = 0; k < 90; k++) { var pn = Mk.Piv(root); Mk.Msh(pn, Geo.Plane(0.24f, 0.32f), pgPageM, 0, 0, 0, rand * 3, rand * 3, rand * 3); pages.Add((pn, 0.6f + rand * R, rand * H, rand * 6.28f, 2.5f + rand * 3)); }
                var vm2 = Mats.Add(0xe8e0cc, 0.07f, Tex.Holy, true, true); Mk.Msh(root, Geo.Cyl(R, R * 0.4f, H, 20, true), vm2, 0, H / 2, 0);
                foreach (var rr in root.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                float hitT = 0, sndT = 0;
                trapAnims.Add((t, dt) =>
                {
                    Vector3 c = idx == 0 ? V(Mathf.Sin(t * 0.16f) * 17, 0, Mathf.Cos(t * 0.16f) * 17) : V(Mathf.Sin(t * 0.12f) * 40, 0, (Mathf.Sin(t * 0.06f) > 0 ? 1 : -1) * 51);
                    root.t.localPosition = c;
                    foreach (var p in pages) { float a = p.a + t * p.sp; p.n.t.localPosition = V(Mathf.Cos(a) * p.r, (p.y + t * 1.5f) % H, Mathf.Sin(a) * p.r); p.n.t.Rotate(dt * 200, dt * 130, 0); }
                    vm2.SetTextureOffset("_BaseMap", V(t * 0.6f, t * 0.2f, 0));
                    float dd = Hypot(P.pos.x - c.x, P.pos.z - c.z);
                    sndT -= dt; if (dd < 18 && sndT <= 0) { sndT = 0.7f; Sfx("dash", 0.25f * (1 - dd / 18)); }
                    if (!HazardLive) return;
                    hitT -= dt;
                    if (dd < R && P.pos.y < H)
                    {
                        hazSlowT = 0.2f; var o = P.pos - c; o.y = 0; var tan = V(-o.z, 0, o.x).normalized;
                        P.vel += (tan * 14 - o.normalized * 4) * dt + V(0, 9 * dt, 0);
                        if (hitT <= 0) { hitT = 0.5f; HazardHitPlayer(6, c, Vector3.zero); SpawnParticles(ChestPos, 4, 4, ashMat, 0.08f, 0.4f); }
                    }
                    HazardHitEnemies(e => Hypot(e.pos.x - c.x, e.pos.z - c.z) < R && e.pos.y < H, 4, e => { var o = e.pos - c; o.y = 0; return V(-o.z, 0, o.x).normalized * 6 + V(0, 4, 0); }, 0.6f);
                });
            }
        }

        // ================= 19. MIRROR WASTES =================
        readonly List<(Vector3 pos, Vector3 face)> pgMirrors = new List<(Vector3, Vector3)>();
        void BuildMirrorWastes()
        {
            pgMirrors.Clear(); Arena(70, 8);
            Altar(0, 0, 10, 3);
            var sand = Mats.Std(0xb8b0a0, 0.95f, 0, 0, GrimeTex()); var sandL = Mats.Std(0xc8c0ae, 0.95f);
            var gold = Mats.Std(0xd8b860, 0.3f, 0.85f); var stone = Mats.Std(0xb0a898, 0.9f, 0, 0, GrimeTex());
            // dunes: solid, with soft mounds blending them into the sand
            foreach (var p in new[] { V(-30, 0, 20), V(28, 0, -24), V(-20, 0, -40), V(34, 0, 32), V(0, 0, -20), V(-46, 0, -6), V(48, 0, 4) })
            {
                float w = 12 + rand * 6, h = 1.2f + rand * 1.4f, d = 8 + rand * 4; Box(p.x, 0, p.z, w, h, d, sand);
                var m = Deco(Geo.Sphere(1, 18, 8, 0, Mathf.PI * 2, 0, Mathf.PI / 2), sandL, p.x, 0, p.z); m.transform.localScale = V(w * 0.72f, h * 0.9f, d * 0.72f); Ig(m);
                for (int k = 0; k < 2; k++) { var s = Deco(Geo.Sphere(1, 14, 6, 0, Mathf.PI * 2, 0, Mathf.PI / 2), sandL, p.x + (k == 0 ? -1 : 1) * w * 0.5f, 0, p.z); s.transform.localScale = V(3, h * 0.6f, d * 0.4f); Ig(s); }
            }
            // ripples in the sand
            for (int i = 0; i < 60; i++) { float a = rand * Mathf.PI * 2, r = 10 + rand * 55; Ig(Deco(Geo.Box(4 + rand * 4, 0.06f, 0.25f), sandL, Mathf.Cos(a) * r, 0.03f, Mathf.Sin(a) * r, 0, 0.6f + (rand - 0.5f) * 0.3f, 0)); }
            var glass = Mats.Trans(0xe0f0ff, 0.4f);
            for (int i = 0; i < 60; i++) { float a = rand * Mathf.PI * 2, r = 14 + rand * 50; Deco(Geo.Cone(0.3f + rand * 0.4f, 1 + rand * 3, 4), glass, Mathf.Cos(a) * r, 0.6f, Mathf.Sin(a) * r, (rand - 0.5f) * 0.6f, rand * 3, (rand - 0.5f) * 0.6f); }
            // glints twinkling in the glass sand
            var glints = new List<(Material m, float ph)>();
            for (int i = 0; i < 40; i++) { float a = rand * Mathf.PI * 2, r = 8 + rand * 58; var g = Mk.Glow(levelRoot, 0xffffff, 0.5f, 0); g.transform.localPosition = V(Mathf.Cos(a) * r, 0.25f, Mathf.Sin(a) * r); glints.Add((g.GetComponent<Renderer>().sharedMaterial, rand * 20)); }
            anims.Add((t, dt) => { foreach (var g in glints) Mats.SetAlpha(g.m, Mathf.Max(0, Mathf.Sin(t * 1.3f + g.ph)) > 0.97f ? 1 : 0); });
            // empty ornate frames half sunk in the sand (cover)
            foreach (var (p, ry) in new[] { (V(-14, 0, 6), 0.2f), (V(14, 0, -6), -0.3f), (V(-6, 0, 28), 1.5f), (V(6, 0, -30), 1.7f), (V(30, 0, 8), 0.9f), (V(-30, 0, -8), -0.8f) })
            {
                var f = Mk.Piv(levelRoot, p.x, -0.6f, p.z); f.SetRot(0.08f, ry, 0.12f);
                foreach (var s in new[] { -1f, 1f }) Mk.Msh(f, Geo.Box(0.5f, 6, 0.5f), gold, s * 2, 3, 0);
                Mk.Msh(f, Geo.Arc(2, 0.25f, 16), gold, 0, 6, 0); Mk.Msh(f, Geo.Box(4.4f, 0.5f, 0.5f), gold, 0, 0.6f, 0);
                Mk.Msh(f, Geo.Box(3.6f, 5, 0.06f), Mats.Trans(0xe8f4ff, 0.18f), 0, 3.4f, 0);
                SolidAABB(V(p.x - 0.8f, 0, p.z - 0.8f), V(p.x + 0.8f, 5.4f, p.z + 0.8f));
                Deco(Geo.Sphere(1, 12, 6, 0, Mathf.PI * 2, 0, Mathf.PI / 2), sandL, p.x, 0, p.z).transform.localScale = V(3, 0.8f, 2.2f);
            }
            // the colossus: a gigantic mirror frame, its glass mostly gone - jagged teeth of it left round the edges, the rest
            // lying in great shards around its feet
            {
                float cx = 40, cz = -56; var cf = Mk.Piv(levelRoot, cx, 0, cz); cf.SetRot(0, -0.35f, 0);
                Mk.Msh(cf, Geo.Box(24, 2.4f, 7), stone, 0, 1.2f, 0); Mk.Msh(cf, Geo.Box(21, 1.0f, 5), stone, 0, 2.9f, 0);
                foreach (var s in new[] { -1f, 1f }) { Mk.Msh(cf, Geo.Box(2.6f, 26, 2.6f), gold, s * 9.5f, 16, 0); Mk.Msh(cf, Geo.Sphere(1.6f, 12, 8), gold, s * 9.5f, 29.6f, 0); Mk.Msh(cf, Geo.Cone(0.9f, 3, 8), gold, s * 9.5f, 32.4f, 0); }
                Mk.Msh(cf, Geo.Arc(9.5f, 1.3f, 32), gold, 0, 29, 0); Mk.Msh(cf, Geo.Circle(2.6f, 24), gold, 0, 40.2f, 0.4f); for (int k = 0; k < 10; k++) { float a = k * Mathf.PI / 5; Mk.Msh(cf, Geo.Box(0.4f, 2.6f, 0.3f), gold, Mathf.Sin(a) * 3.8f, 40.2f + Mathf.Cos(a) * 3.8f, 0.4f, 0, 0, -a); }
                var shardM = Mats.Std(0xe8f4ff, 0.02f, 1f, 0x0a1018);
                for (int k = 0; k < 18; k++) { float u = k / 17f; var edge = k < 6 ? V(-8.2f, 4 + u * 3 * 22 / 3, 0) : k < 12 ? V(8.2f, 4 + (u - 0.35f) * 60, 0) : V(-6 + (u - 0.7f) * 40, 4.2f, 0); var sh = Mk.Msh(cf, Geo.Cone(0.9f + rand * 0.9f, 3 + rand * 4, 3), shardM, edge.x, Mathf.Min(edge.y, 27), edge.z, 0, 0, (edge.x < 0 ? -1 : 1) * (Mathf.PI / 2 + (rand - 0.5f) * 0.8f)); sh.transform.localScale = V(1, 1, 0.12f); }
                foreach (var rr in cf.t.GetComponentsInChildren<Renderer>()) { rr.gameObject.layer = Mk.LayerIgnore; rr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes; }
                SolidTurned(V(cx, 0, cz), V(24, 3.4f, 7), -0.35f, 0.9f);
                foreach (var s in new[] { -1f, 1f }) { var pp = V(cx, 0, cz) + Quaternion.Euler(0, -0.35f * Mathf.Rad2Deg, 0) * V(s * 9.5f, 0, 0); SolidAABB(pp - V(1.4f, 0, 1.4f), pp + V(1.4f, 29, 1.4f)); }
                for (int k = 0; k < 9; k++) { var sp = V(cx + (rand - 0.5f) * 30, 0.4f, cz + 6 + rand * 10); var sh = Deco(Geo.Cone(1.2f + rand * 1.6f, 4 + rand * 5, 3), shardM, sp.x, sp.y, sp.z, Mathf.PI / 2 - 0.15f, rand * 6, 0); sh.transform.localScale = V(1, 1, 0.14f); sh.GetComponent<Renderer>().reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes; Ig(sh); }
                var pr = MakeMirrorProbe(V(cx, 14, cz + 6), V(40, 40, 30));
            }
            // a sunken colonnade: a row of fluted columns, most broken, a lintel still across two of them, sand drifted over
            {
                float bx = -46, bz = 50; float[] hs = { 9, 5.5f, 9, 2.5f, 9, 7 };
                for (int k = 0; k < 6; k++)
                {
                    float x = bx - 12 + k * 5; Box(x, 0, bz, 2.4f, 0.6f, 2.4f, stone); var col = Mk.Msh(levelRoot, Geo.Cyl(0.85f, 0.95f, hs[k], 12), stone, x, 0.6f + hs[k] / 2, bz); col.layer = Mk.LayerLevel; col.AddComponent<BoxCollider>(); colliders.Add(new AABB(V(x - 0.9f, 0, bz - 0.9f), V(x + 0.9f, hs[k] + 0.6f, bz + 0.9f)));
                    if (hs[k] < 8) for (int j = 0; j < 3; j++) Ig(Deco(Geo.Cyl(0.85f, 0.85f, 1.2f, 12), stone, x + 2 + rand * 3, 0.85f, bz + 3 + rand * 4, Mathf.PI / 2, rand * 3, 0));
                    else Deco(Geo.Box(2.2f, 0.6f, 2.2f), stone, x, hs[k] + 0.9f, bz);
                }
                Box(bx - 7, 10.2f, bz, 11.6f, 1.4f, 2.4f, stone); Box(bx + 3, 10.2f, bz, 6, 1.4f, 2.4f, stone);
                var mound = Deco(Geo.Sphere(1, 18, 8, 0, Mathf.PI * 2, 0, Mathf.PI / 2), sandL, bx, 0, bz + 1); mound.transform.localScale = V(18, 2.2f, 6); Ig(mound);
            }
            // spires of crystal grown out of the glass sand
            foreach (var p in new[] { V(-38, 0, -24), V(20, 0, 24), V(-16, 0, 46), V(44, 0, -12) })
            {
                var cm = Mats.Std(0xd8ecff, 0.05f, 0.3f, 0x102030);
                for (int k = 0; k < 6; k++) { float h = 3 + rand * 7; var o = V((rand - 0.5f) * 3, 0, (rand - 0.5f) * 3); Deco(Geo.Cone(0.5f + rand * 0.6f, h, 6), cm, p.x + o.x, h / 2, p.z + o.z, (rand - 0.5f) * 0.5f, rand * 3, (rand - 0.5f) * 0.5f); }
                SolidAABB(V(p.x - 1.4f, 0, p.z - 1.4f), V(p.x + 1.4f, 6, p.z + 1.4f)); var g = Mk.Glow(levelRoot, 0xc8e8ff, 4, 0.4f); g.transform.localPosition = V(p.x, 3, p.z);
            }
            PadAt(-30, 34, 24); PadAt(30, -34, 24); PadAt(0, 44, 22);
            MakeMirrorProbe(V(0, 6, 0), V(150, 50, 150), 1); // (the whole waste: every pillar pane and pool reflects something)
            MirrorWasteDetails(new[] { V(-56, 0, -40), V(56, 0, 40), V(-58, 0, 30), V(58, 0, -30), V(0, 0, -60), V(-30, 0, 34), V(30, 0, -34), V(0, 0, 44), V(0, 0, 56) });
            MechMirrors2(new[] { V(-56, 0, -40), V(56, 0, 40), V(-58, 0, 30), V(58, 0, -30), V(0, 0, -60) });
        }
        // ---------- the Mirror Twin's entrance ----------
        readonly List<(float at, System.Action a)> pgLater = new List<(float, System.Action)>();
        void UpdatePgLater()
        {
            for (int i = pgLater.Count - 1; i >= 0; i--) if (time >= pgLater[i].at) { var a = pgLater[i].a; pgLater.RemoveAt(i); a(); }
        }
        // in the Mirror Wastes it steps out of one of the great mirrors; anywhere else a mirror of its own forms in the air
        // (an oval glass in a gold rim), it steps out of that, and the mirror shatters behind it
        void TwinEmerge(Ent e)
        {
            Vector3 at, face; float emerge = 1.6f;
            if (LEVEL != null && LEVEL.name == "MIRROR WASTES" && pgMirrors.Count > 0)
            {
                var m = pgMirrors[Random.Range(0, pgMirrors.Count)]; face = m.face; at = m.pos + Vector3.Cross(Vector3.up, face) * (rand - 0.5f) * 3;
                var glassC = m.pos - face * 6.3f + V(0, 7.5f, 0); emerge = 5.6f; Blast(glassC, 3, 0xe8f4ff); SpawnParticles(glassC, 20, 4, cyanMat, 0.08f, 0.6f, 0);
            }
            else
            {
                at = e.pos; var toP = P.pos - at; toP.y = 0; face = toP.sqrMagnitude > 0.01f ? toP.normalized : Vector3.forward;
                var mp = at - face * 1.7f; var piv = Mk.Piv(levelRoot, mp.x, 0, mp.z); piv.SetRot(0, Mathf.Atan2(face.x, face.z), 0);
                var glassM = Mats.Std(0xe8f0f8, 0.02f, 1f, 0x101820); var rimM = Mats.Std(0xd8b860, 0.3f, 0.85f);
                var gl = Mk.Msh(piv, Geo.Circle(1, 28), glassM, 0, 1.4f, 0); gl.transform.localScale = V(0.95f, 1.45f, 1);
                var rim = Mk.Msh(piv, Geo.Torus(1, 0.06f, 32, 4), rimM, 0, 1.4f, 0); rim.transform.localScale = V(0.97f, 1.47f, 1);
                Mk.Msh(piv, Geo.Circle(1, 28), Mats.Std(0x0a0a0e, 0.5f), 0, 1.4f, -0.02f, 0, Mathf.PI, 0).transform.localScale = V(0.95f, 1.45f, 1);
                var glow = Mk.Glow(piv.t, 0xe8f4ff, 3.2f, 0.6f); glow.transform.localPosition = V(0, 1.4f, 0.1f);
                foreach (var rr in piv.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                piv.Scale(0.01f); float born = time;
                // it opens out of nothing, then shatters behind the twin
                pgLater.Add((time + 0.05f, () => { if (piv.go) piv.Scale(0.5f); }));
                pgLater.Add((time + 0.12f, () => { if (piv.go) piv.Scale(1f); Sfx("blink", 0.6f); }));
                pgLater.Add((time + 1.9f, () =>
                {
                    if (!piv.go) return; var c = piv.t.position + V(0, 1.4f, 0);
                    SpawnParticles(c, 26, 6, cyanMat, 0.1f, 0.8f); for (int k = 0; k < 10; k++) particles.Add(new Pt { pos = c + Random.insideUnitSphere * 0.8f, scale = V(0.2f, 0.3f, 0.02f), mat = glassM, v = Random.insideUnitSphere * 5 + V(0, 2, 0), life = 1.5f, grav = GRAV, hasSpin = true, spin = Random.insideUnitSphere * 10 });
                    Blast(c, 1.6f, 0xe8f4ff); Sfx("shatter", 0.5f); Destroy(piv.go);
                }));
            }
            e.pos = V(at.x, e.pos.y, at.z); e.yaw = Mathf.Atan2(face.x, face.z); e.emerging = true; e.emergeDir = face * emerge; e.spawnT = 1.3f; e.vel = Vector3.zero;
            SyncEnt(e); Sfx("blink", 0.7f);
        }
        // more of the glass desert: a ring of mirror-faced pillars crowned with floating crystals, mirror pools set in the sand,
        // a giant stone hand and a fallen statue's head half buried, dead trees of glass, sand blowing across it all. Every
        // piece is placed only where there's open ground (never on a dune, a frame, the mirrors' plinths or the paths)
        void MirrorWasteDetails(Vector3[] keepClear)
        {
            var stone = Mats.Std(0xb0a898, 0.9f, 0, 0, GrimeTex()); var stoneD = Mats.Std(0x6e665a, 0.92f, 0, 0, GrimeTex()); var gold = Mats.Std(0xd8b860, 0.3f, 0.85f);
            var glass = Mats.Std(0xe8f0f8, 0.02f, 1f); var crystal = Mats.Std(0xd8ecff, 0.05f, 0.3f, 0x182840); var sandL = Mats.Std(0xc8c0ae, 0.95f);
            var placed = new List<Vector3>();
            bool Open(Vector3 p, float r)
            {
                if (Hypot(p.x, p.z) < 14 || Mathf.Abs(p.x) > LB - 6 || Mathf.Abs(p.z) > LB - 6) return false;
                foreach (var k in keepClear) if (Hypot(p.x - k.x, p.z - k.z) < 10) return false;
                foreach (var q in placed) if (Hypot(p.x - q.x, p.z - q.z) < r + 6) return false;
                for (int i = 0; i < 9; i++) { var o = i == 0 ? Vector3.zero : Quaternion.Euler(0, i * 45, 0) * V(r, 0, 0); if (PointInLevel(p + o + V(0, 0.5f, 0)) || PointInLevel(p + o + V(0, 3, 0))) return false; }
                return true;
            }
            Vector3? Find(float radius, float r, float a0)
            {
                for (int i = 0; i < 24; i++) { float a = a0 + i * Mathf.PI / 12; for (float rr = radius; rr < radius + 14; rr += 3.5f) { var p = V(Mathf.Sin(a) * rr, 0, Mathf.Cos(a) * rr); if (Open(p, r)) { placed.Add(p); return p; } } }
                return null;
            }
            // the mirror pillars
            int pillars = 0;
            for (int k = 0; k < 6; k++)
            {
                var pp = Find(21, 2.4f, k * Mathf.PI / 3 + 0.3f); if (pp == null) continue; var p = pp.Value; pillars++;
                Box(p.x, 0, p.z, 3.4f, 0.9f, 3.4f, stoneD); Box(p.x, 0.9f, p.z, 2.7f, 0.5f, 2.7f, stone); TrimRect(p.x, p.z, 2.7f, 2.7f, 1.4f);
                Box(p.x, 1.4f, p.z, 1.5f, 9.2f, 1.5f, stoneD);
                for (int f = 0; f < 4; f++)
                {
                    var q = Quaternion.Euler(0, f * 90, 0); var n = q * Vector3.forward; var sdir = q * Vector3.right;
                    var pc = p + n * 0.77f + V(0, 6.0f, 0);
                    var pane = Deco(Geo.Box(1.2f, 8.2f, 0.04f), glass, pc.x, pc.y, pc.z, 0, f * Mathf.PI / 2, 0); Ig(pane); pane.GetComponent<Renderer>().reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
                    var cor = p + (n + sdir) * 0.76f; Ig(Deco(Geo.Box(0.14f, 9.2f, 0.14f), gold, cor.x, 6.0f, cor.z));
                }
                foreach (var y in new[] { 1.55f, 4.6f, 7.6f, 10.5f }) Ig(Deco(Geo.Box(1.64f, 0.16f, 1.64f), gold, p.x, y, p.z));
                Deco(Geo.Cyl(1.3f, 0.85f, 0.7f, 4), gold, p.x, 10.95f, p.z, 0, Mathf.PI / 4, 0); Deco(Geo.Box(2.6f, 0.4f, 2.6f), stone, p.x, 11.5f, p.z);
                SolidAABB(V(p.x - 1.3f, 10.6f, p.z - 1.3f), V(p.x + 1.3f, 11.7f, p.z + 1.3f));
                var cr = Mk.Piv(levelRoot, p.x, 13.6f, p.z); var cm = Mk.Msh(cr, Geo.Icosahedron(1), crystal); cm.transform.localScale = V(0.8f, 1.5f, 0.8f); cm.layer = Mk.LayerIgnore;
                var shards = new List<N>(); for (int s = 0; s < 3; s++) { var sp = Mk.Piv(cr); var sh = Mk.Msh(sp, Geo.Cone(0.16f, 0.6f, 4), crystal, 1.5f, 0, 0); sh.layer = Mk.LayerIgnore; sp.SetRot(0, s * 2.09f, 0); shards.Add(sp); }
                var gl = Mk.Glow(levelRoot, 0xc8e8ff, 4.5f, 0.45f); gl.transform.localPosition = V(p.x, 13.6f, p.z);
                float ph = k * 1.3f; anims.Add((t, dt) => { cr.py = 13.6f + Mathf.Sin(t * 0.9f + ph) * 0.35f; cr.ry = t * 0.5f + ph; foreach (var s in shards) s.t.Rotate(0, dt * 70, 0, Space.Self); });
            }
            // mirror pools set into the sand, gold-rimmed, the sky in them
            for (int k = 0; k < 6; k++)
            {
                var pp = Find(30, 2.6f, k * 1.05f + 0.7f); if (pp == null) continue; var p = pp.Value;
                var pool = Deco(Geo.Circle(2.4f, 28), glass, p.x, 0.06f, p.z, -Mathf.PI / 2); Ig(pool); pool.GetComponent<Renderer>().reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
                Ig(Deco(Geo.Torus(2.45f, 0.12f, 32, 4), gold, p.x, 0.08f, p.z, Mathf.PI / 2));
                var mound = Deco(Geo.Torus(2.9f, 0.5f, 28, 6), sandL, p.x, 0.0f, p.z, Mathf.PI / 2); mound.transform.localScale = V(1, 1, 0.5f); Ig(mound);
            }
            // a giant stone hand reaching up out of the sand
            {
                var pp = Find(38, 4, 2.2f);
                if (pp != null)
                {
                    var p = pp.Value; var h = Mk.Piv(levelRoot, p.x, -0.6f, p.z); h.SetRot(0.25f, Hypot(p.x, p.z) > 0 ? Mathf.Atan2(-p.x, -p.z) : 0, 0.1f);
                    Mk.Msh(h, Geo.Cyl(1.6f, 1.9f, 4, 10), stone, 0, 2, 0); Mk.Msh(h, Geo.Box(3.6f, 3.4f, 1.5f), stone, 0, 5.4f, 0);
                    for (int f = 0; f < 4; f++) { var fp = Mk.Piv(h, -1.3f + f * 0.87f, 7.0f, 0.1f); fp.SetRot(0.35f + f * 0.12f, 0, (f - 1.5f) * 0.06f); Mk.Msh(fp, Geo.Cyl(0.36f, 0.42f, 2.6f, 8), stone, 0, 1.3f, 0); var tip = Mk.Piv(fp, 0, 2.6f, 0); tip.SetRot(0.7f, 0, 0); Mk.Msh(tip, Geo.Cyl(0.3f, 0.36f, 1.8f, 8), stone, 0, 0.9f, 0); }
                    var th = Mk.Piv(h, 1.9f, 5.0f, 0.3f); th.SetRot(0.3f, 0, -0.9f); Mk.Msh(th, Geo.Cyl(0.4f, 0.48f, 2.4f, 8), stone, 0, 1.2f, 0);
                    foreach (var rr in h.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                    SolidAABB(V(p.x - 2, 0, p.z - 1.6f), V(p.x + 2, 7.5f, p.z + 1.6f));
                    var md = Deco(Geo.Sphere(1, 16, 6, 0, Mathf.PI * 2, 0, Mathf.PI / 2), sandL, p.x, 0, p.z); md.transform.localScale = V(4.5f, 1.2f, 4.5f); Ig(md);
                }
            }
            // a fallen colossus's head, half sunk: brow, nose, a crack through it, one eye still glowing faintly
            {
                var pp = Find(40, 4.5f, 4.4f);
                if (pp != null)
                {
                    var p = pp.Value; var hd = Mk.Piv(levelRoot, p.x, 1.4f, p.z); hd.SetRot(0.1f, Mathf.Atan2(-p.x, -p.z) + 0.6f, 0.5f);
                    Mk.Msh(hd, Geo.Sphere(3.4f, 18, 14), stone).transform.localScale = V(1, 1.15f, 1.05f);
                    Mk.Msh(hd, Geo.Box(3.6f, 0.7f, 1.2f), stone, 0, 1.0f, 3.0f); Mk.Msh(hd, Geo.Cone(0.8f, 1.8f, 4), stone, 0, -0.1f, 3.6f, Mathf.PI / 2, 0, 0);
                    foreach (var sx in new[] { -1f, 1f }) Mk.Msh(hd, Geo.Sphere(0.6f, 10, 8), stoneD, sx * 1.2f, 0.4f, 3.05f);
                    Mk.Msh(hd, Geo.Box(0.12f, 4.5f, 0.2f), stoneD, 0.8f, 0.5f, 3.3f, 0.3f, 0, 0.4f);
                    var eg = Mk.Glow(hd.t, 0xc8e8ff, 1.6f, 0.7f); eg.transform.localPosition = V(-1.2f, 0.4f, 3.5f);
                    foreach (var rr in hd.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                    SolidAABB(V(p.x - 3, 0, p.z - 3), V(p.x + 3, 4.2f, p.z + 3));
                    var md = Deco(Geo.Sphere(1, 16, 6, 0, Mathf.PI * 2, 0, Mathf.PI / 2), sandL, p.x, 0, p.z); md.transform.localScale = V(5.5f, 1.4f, 5.5f); Ig(md);
                }
            }
            // dead trees of glass
            for (int k = 0; k < 5; k++)
            {
                var pp = Find(46, 1.2f, k * 1.25f + 0.2f); if (pp == null) continue; var p = pp.Value;
                Deco(Geo.Cone(0.45f, 6, 6), crystal, p.x, 3, p.z, (rand - 0.5f) * 0.15f, rand * 3, (rand - 0.5f) * 0.15f);
                for (int b = 0; b < 5; b++) { float a = b * 1.26f + k, y = 2.2f + b * 0.7f; Ig(Deco(Geo.Cone(0.12f, 2.4f - b * 0.25f, 4), crystal, p.x + Mathf.Sin(a) * 0.7f, y + 0.6f, p.z + Mathf.Cos(a) * 0.7f, Mathf.Cos(a) * 0.9f, 0, -Mathf.Sin(a) * 0.9f)); }
                SolidAABB(V(p.x - 0.35f, 0, p.z - 0.35f), V(p.x + 0.35f, 5, p.z + 0.35f));
            }
            // sand blowing across the waste
            anims.Add((t, dt) => { if (rand < 0.1f) { var p = V(-LB + 2, 0.3f + rand * 2.5f, (rand - 0.5f) * LB * 1.8f); particles.Add(new Pt { pos = p, scale = Vector3.one * (0.05f + rand * 0.08f), mat = dustMat, v = V(9 + rand * 6, rand * 0.6f, (rand - 0.5f) * 2), life = 7, grav = 0.3f }); } });
            if (testMode && testOut != null) System.IO.File.AppendAllText(System.IO.Path.Combine(testOut, "events.txt"), $"mirror wastes: {pillars} pillars, {placed.Count} details placed\n");
        }
        ReflectionProbe MakeMirrorProbe(Vector3 at, Vector3 size, int importance = 2)
        {
            var go = new GameObject("mirrorProbe"); go.transform.SetParent(levelRoot, false); go.transform.localPosition = at;
            var rp = go.AddComponent<ReflectionProbe>();
            rp.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime; rp.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            rp.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.AllFacesAtOnce; rp.resolution = 128; rp.hdr = true; rp.size = size; rp.importance = importance; rp.boxProjection = true;
            rp.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor; rp.backgroundColor = cam ? cam.backgroundColor : Color.gray; rp.nearClipPlane = 0.5f; rp.farClipPlane = 260;
            rp.cullingMask = ~((1 << Mk.LayerEnemy)); QualitySettings.realtimeReflectionProbes = true;
            bool done = false; anims.Add((t, dt) => { if (!done && rp) { done = true; rp.RenderProbe(); } });
            return rp;
        }
        // ---------- MIRROR GLINTS ----------
        // ornate standing mirrors that turn on their plinths to follow you: the glass brightens and a thread of light traces
        // you for ~2s (break line of sight to lose it), then a lance of light fires down it - blast, shockwave, a scorch
        void MechMirrors2(Vector3[] spots)
        {
            var glass = Mats.Std(0xe8f0f8, 0.02f, 1f); var frame = Mats.Std(0xd8b860, 0.3f, 0.85f); var stone = Mats.Std(0xb0a898, 0.9f, 0, 0, GrimeTex()); var back = Mats.Std(0x2a2620, 0.8f);
            var unit = Geo.Cyl(1, 1, 1, 10); var crack = Mats.Std(0x405060, 0.4f, 0.5f);
            for (int i = 0; i < spots.Length; i++)
            {
                var c = spots[i]; float off = i * 1.5f; var face = V(-c.x, 0, -c.z).normalized; float ry0 = Mathf.Atan2(face.x, face.z);
                Deco(Geo.Box(7.4f, 0.9f, 4.4f), stone, c.x, 0.45f, c.z, 0, ry0, 0); Deco(Geo.Cyl(3.2f, 3.4f, 0.7f, 20), stone, c.x, 1.25f, c.z); Deco(Geo.Torus(3.3f, 0.12f, 32, 4), frame, c.x, 1.6f, c.z, Mathf.PI / 2);
                var m = Mk.Piv(levelRoot, c.x, 1.6f, c.z); m.SetRot(0, ry0, 0);
                var gl = Mk.Msh(m, Geo.Box(7, 10.6f, 0.15f), glass, 0, 6.3f, 0); var gl2 = Mk.Msh(m, Geo.Circle(3.5f, 32), glass, 0, 11.6f, 0.09f);
                Mk.Msh(m, Geo.Box(7.6f, 11.2f, 0.2f), back, 0, 6.3f, -0.18f); Mk.Msh(m, Geo.Circle(3.8f, 32), back, 0, 11.6f, -0.2f, 0, Mathf.PI, 0);
                foreach (var sx in new[] { -1f, 1f }) { Mk.Msh(m, Geo.Box(0.7f, 12.4f, 0.6f), frame, sx * 3.85f, 6.2f, 0); Mk.Msh(m, Geo.Sphere(0.5f, 10, 8), frame, sx * 3.85f, 12.9f, 0); Mk.Msh(m, Geo.Cone(0.25f, 1.1f, 8), frame, sx * 3.85f, 13.8f, 0); for (int k = 0; k < 5; k++) Mk.Msh(m, Geo.Sphere(0.12f, 6, 4), frame, sx * 3.85f, 2 + k * 2.2f, 0.32f); }
                Mk.Msh(m, Geo.Arc(3.85f, 0.35f, 24), frame, 0, 11.6f, 0); Mk.Msh(m, Geo.Box(8.4f, 0.6f, 0.7f), frame, 0, 0.8f, 0);
                Mk.Msh(m, Geo.Circle(1.0f, 20), frame, 0, 16.2f, 0.1f); for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; Mk.Msh(m, Geo.Box(0.18f, 1.0f, 0.15f), frame, Mathf.Sin(a) * 1.5f, 16.2f + Mathf.Cos(a) * 1.5f, 0.1f, 0, 0, -a); }
                for (int k = 0; k < 5; k++) { float a = k * 1.25f + i; Mk.Msh(m, Geo.Box(0.04f, 1.2f + (k % 3) * 0.6f, 0.02f), crack, 1.6f + Mathf.Sin(a) * 0.6f, 4 + Mathf.Cos(a) * 0.6f, 0.09f, 0, 0, a); }
                var rimM = Mats.Add(0xfff4d0, 0, null, true, true); Mk.Msh(m, Geo.Box(7.2f, 10.8f, 0.05f), rimM, 0, 6.3f, 0.11f); Mk.Msh(m, Geo.Torus(3.6f, 0.15f, 32, 4, Mathf.PI), rimM, 0, 11.6f, 0.12f);
                foreach (var rr in m.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                gl.GetComponent<Renderer>().reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes; gl2.GetComponent<Renderer>().reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
                MakeMirrorProbe(V(c.x, 7, c.z) + face * 3, V(12, 18, 12));
                SolidAABB(V(c.x - 4, 0, c.z - 4), V(c.x + 4, 14, c.z + 4));
                pgMirrors.Add((V(c.x, 0, c.z) + face * 6.5f, face));
                var core = Mats.Add(0xffffff, 0, null, true, true); var outer = Mats.Add(0xffd890, 0, null, true, true);
                var beam = Deco(unit, core, 0, 0, 0); beam.layer = Mk.LayerIgnore; beam.SetActive(false); var beamO = Deco(unit, outer, 0, 0, 0); beamO.layer = Mk.LayerIgnore; beamO.SetActive(false);
                var flare = Mk.Glow(levelRoot, 0xfff4d0, 9, 0); var flareM = flare.GetComponent<Renderer>().sharedMaterial;
                var tgtM = Mats.Add(0xfff0c0, 0, null, true, true); var tgt = Deco(Geo.Torus(1.0f, 0.025f, 32, 4), tgtM, 0, 0, 0); tgt.layer = Mk.LayerIgnore;
                Vector3 aim = Vector3.zero; bool fired = false; float yaw = ry0, fade = 0; Vector3 lastSrc = Vector3.zero, lastDir = Vector3.forward; float lastL = 0;
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 7.5f; bool tracing = u >= 4.5f && u < 6.3f, firing = u >= 6.3f && u < 6.4f;
                    // the mirror turns to follow you while it's awake
                    var toP = P.pos - c; toP.y = 0; float want = u >= 3.6f && u < 6.4f ? Mathf.Atan2(toP.x, toP.z) : ry0;
                    float dy = Mathf.DeltaAngle(yaw * Mathf.Rad2Deg, want * Mathf.Rad2Deg) * Mathf.Deg2Rad; dy = Mathf.Clamp(dy, -1.0f, 1.0f);
                    yaw += Mathf.Clamp(dy, -0.9f * dt, 0.9f * dt) * (Mathf.Abs(dy) > 0.01f ? 1 : 0); m.SetRot(0, yaw, 0);
                    var fdir = V(Mathf.Sin(yaw), 0, Mathf.Cos(yaw)); var src = V(c.x, 7.9f, c.z) + fdir * 0.5f;
                    flare.transform.localPosition = src + fdir * 0.3f;
                    float wake = u >= 3.6f && u < 6.4f ? Mathf.Clamp01((u - 3.6f) / 2.7f) : 0; Mats.SetAlpha(rimM, 0.05f + wake * 0.4f * (tracing && Mathf.Sin(t * 26) > 0 ? 1 : 0.7f));
                    if (wake > 0 && rand < wake * 0.6f) { var rp = src + Random.insideUnitSphere * 4; SpawnParticles(rp, 1, 0.1f, goldSpark, 0.06f, 0.4f, 0); }
                    // the lance fades out after it fires
                    if (fade > 0)
                    {
                        fade -= dt * 2.6f; float k = Mathf.Max(0, fade);
                        Mats.SetAlpha(core, k); Mats.SetAlpha(outer, 0.45f * k); beamO.transform.localScale = V(1.6f + (1 - k) * 1.4f, lastL, 1.6f + (1 - k) * 1.4f); beam.transform.localScale = V(0.5f * k + 0.05f, lastL, 0.5f * k + 0.05f);
                        if (fade <= 0) { beam.SetActive(false); beamO.SetActive(false); }
                    }
                    if (!HazardLive || (!tracing && !firing)) { if (fade <= 0) { beam.SetActive(false); beamO.SetActive(false); } Mats.SetAlpha(flareM, wake * 0.3f); Mats.SetAlpha(tgtM, 0); if (u < 6.3f) fired = false; if (!tracing) aim = ChestPos; return; }
                    if (tracing) { aim = Vector3.Lerp(aim, ChestPos, 1 - Mathf.Exp(-3 * dt)); if (!Los(src, ChestPos)) { beam.SetActive(false); Mats.SetAlpha(flareM, 0.15f); Mats.SetAlpha(tgtM, 0); return; } }
                    var dir = (aim - src).normalized; float L = 160;
                    if (Physics.Raycast(src, dir, out var hit, L, LEVEL_MASK, QueryTriggerInteraction.Ignore)) L = hit.distance;
                    if (tracing)
                    {
                        beam.SetActive(true); float w = 0.03f + 0.06f * (u - 4.5f) / 1.8f;
                        beam.transform.localPosition = src + dir * L / 2; beam.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir); beam.transform.localScale = V(w, L, w);
                        Mats.SetAlpha(core, 0.3f + 0.4f * (Mathf.Sin(t * 30) > 0 ? 1 : 0.5f)); Mats.SetAlpha(flareM, 0.45f + 0.3f * (u - 4.5f) / 1.8f);
                        tgt.transform.localPosition = V(P.pos.x, Mathf.Max(0.05f, FloorBelow(P.pos + V(0, 0.3f, 0)) + 0.05f), P.pos.z); tgt.transform.localRotation = Quaternion.Euler(90, 0, 0); tgt.transform.localScale = Vector3.one * (2.2f - (u - 4.5f) / 1.8f * 1.0f); Mats.SetAlpha(tgtM, 0.35f);
                        if (rand < 0.8f) { float a = rand * L; SpawnParticles(src + dir * a, 1, 0.3f, goldSpark, 0.05f, 0.3f, 0); }
                    }
                    if (firing && !fired)
                    {
                        fired = true; Sfx("holy"); Sfx("boomSmall", 0.6f); Blast(src, 3.5f, 0xfff4d0); fade = 1; lastL = L; lastSrc = src; lastDir = dir;
                        beam.SetActive(true); beamO.SetActive(true);
                        foreach (var b in new[] { beam, beamO }) { b.transform.localPosition = src + dir * L / 2; b.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir); }
                        Mats.SetAlpha(flareM, 1); Mats.SetAlpha(tgtM, 0);
                        var hp = src + dir * L; Blast(hp, 2.8f, 0xfff4d0); ShockRing(hp, 4, 0xffd890, 0.4f); SpawnParticles(hp, 24, 9, goldSpark, 0.1f, 0.7f);
                        for (int k = 0; k < 16; k++) { float a = k / 16f * L; SpawnParticles(src + dir * a + Quaternion.AngleAxis(k * 60, dir) * Vector3.Cross(dir, Vector3.up).normalized * 0.8f, 1, 1, goldSpark, 0.08f, 0.5f, 0); }
                        if (hp.y < 1.5f) { var sc = Mats.Trans(0x1a1410, 0.6f); var s = Deco(Geo.Circle(1.6f, 18), sc, hp.x, 0.05f, hp.z, -Mathf.PI / 2); s.layer = Mk.LayerIgnore; float st = 0; anims.Add((t2, dt2) => { if (!s) return; st += dt2; Mats.SetAlpha(sc, Mathf.Max(0, 0.6f - st * 0.12f)); if (st > 5) s.SetActive(false); }); }
                        float dd = Vector3.Distance(P.pos, hp); if (dd < 14) P.shake = Mathf.Max(P.shake, 0.5f * (1 - dd / 14));
                        var cp = ChestPos; float along = Vector3.Dot(cp - src, dir);
                        if (along > 0 && along < L + 1 && Vector3.Distance(src + dir * along, cp) < 1.3f) HazardHitPlayer(24, src, dir * 10 + V(0, 4, 0));
                        HazardHitEnemies(e => { var bp = BodyPoint(e); float a = Vector3.Dot(bp - src, dir); return a > 0 && a < L && Vector3.Distance(src + dir * a, bp) < e.hitR + 0.8f; }, 10, e => dir * 10 + V(0, 4, 0));
                    }
                });
            }
        }
    }
}
