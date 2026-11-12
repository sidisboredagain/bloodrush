using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    // THE OPENING's ancient empire (GameIntro.cs): carved marble architecture, lit by its own low sun with shadows.
    //   fluted columns with moulded bases and capitals; temples on stepped platforms, colonnades all round, a frieze of
    //   triglyphs, pediments with statues on them; a monumental gateway - two carved pylons with statues in niches, a great
    //   arch with a keystone, gold doors, a crested attic; tiered spires with arcaded balconies, domes and gold needles; a
    //   domed basilica ringed with columns; an aqueduct of arches across the horizon; colossal columns crowned with
    //   statues; streets of houses with pitched roofs. Marble and stone are textured (veins, coursed blocks, weathering).
    // When the Void comes, red cracks open across every facade and the buildings crack, tilt, sink and fall, shedding stone.
    public partial class Game
    {
        Light iLight; float iShadowPrev = -1;
        Material iMarble, iStone, iStoneDk, iGold2, iRoof, iBronze, iCrackM, iDoorM;
        class IFall { public Transform t; public float at, ang, sink, h; public Vector3 axis, pos0; public Quaternion rot0; public bool shed; }
        readonly List<IFall> iFalls = new List<IFall>();

        Texture2D IStoneTex(int baseCol, int veinCol, bool blocks, int seed)
        {
            const int N = 256; var tx = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4 }; iTex.Add(tx);
            Color a = Mats.Hex(baseCol), v = Mats.Hex(veinCol); float o = seed * 17.3f;
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float n = Mathf.PerlinNoise(o + x * 0.02f, y * 0.02f) * 0.55f + Mathf.PerlinNoise(o + x * 0.07f, 3 + y * 0.07f) * 0.3f + Mathf.PerlinNoise(o + x * 0.3f, 7 + y * 0.3f) * 0.15f;
                float vein = Mathf.Abs(Mathf.PerlinNoise(o + x * 0.012f + n * 1.5f, 11 + y * 0.03f) - 0.5f) * 2; vein = Mathf.Pow(1 - vein, 14);
                var c = Color.Lerp(a, v, vein * 0.8f) * (0.82f + 0.25f * n);
                if (Mathf.PerlinNoise(o + x * 0.45f, 19 + y * 0.45f) > 0.72f) c *= 0.82f; // pitting, weathering
                c *= 1 - 0.18f * Mathf.Clamp01((1 - y / (float)N) * 1.4f - 0.6f) * n;    // grime settling low on each block
                if (blocks)
                {
                    int row = y / 64; int off = row % 2 == 0 ? 0 : 64;
                    bool joint = y % 64 < 2 || (x + off) % 128 < 2;
                    if (joint) c *= 0.55f; else if (y % 64 < 4 || (x + off) % 128 < 4) c *= 0.85f; // the joint, its worn edge
                }
                c.a = 1; tx.SetPixel(x, y, c);
            }
            tx.Apply(true); return tx;
        }
        void IArchMats()
        {
            iMarble = IM(Mats.Std(0xffffff, 0.55f, 0, 0, IStoneTex(0xe8e0d0, 0x9a8c7a, false, 1)));
            iStone = IM(Mats.Std(0xffffff, 0.85f, 0, 0, IStoneTex(0xc4b49a, 0x8a7a66, true, 2)));
            iStoneDk = IM(Mats.Std(0xffffff, 0.9f, 0, 0, IStoneTex(0x8a7c6a, 0x5a4e40, true, 3)));
            iGold2 = IM(Mats.Std(0xd8a848, 0.3f, 0.85f)); iBronze = IM(Mats.Std(0x6a5038, 0.45f, 0.7f));
            iRoof = IM(Mats.Std(0xffffff, 0.8f, 0, 0, IStoneTex(0x7a4a3a, 0x4a2a22, true, 4))); iDoorM = IM(Mats.Std(0x1a1410, 0.9f));
            iCrackM = IM(Mats.Add(0xff3010, 0f, Tex.Cracks, true)); iDebM = iStone;
            ITile();
        }
        // the stone repeats across big faces instead of stretching (no plank-like streaks on a wall or a stair)
        void ITile() { foreach (var m in new[] { iMarble, iStone, iStoneDk, iRoof }) if (m) m.SetTextureScale("_BaseMap", new Vector2(2, 2)); }
        // a lit, shadow-casting part
        GameObject IA(Transform p, Mesh mesh, Material m, Vector3 pos, Vector3 rot = default, Vector3? scale = null)
        {
            var g = IMsh(p, mesh, m, pos, rot, scale); var r = g.GetComponent<MeshRenderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; return g;
        }
        Transform IRoot(Transform p, Vector3 pos, float yaw = 0) { var t = new GameObject("arch").transform; t.SetParent(p, false); t.position = pos; t.rotation = Quaternion.Euler(0, yaw, 0); return t; }
        Vector3 L(Transform root, float x, float y, float z) => root.TransformPoint(V(x, y, z));
        // red cracks glowing across a face (they open as the Void comes)
        void ICrack(Transform root, float x, float y, float z, float w, float h) { var g = IMsh(root, Geo.Plane(1, 1), iCrackM, L(root, x, y, z), root.eulerAngles); g.transform.localScale = V(w, h, 1); }
        void IFallReg(Transform t, float at, float h, System.Random rng)
        {
            float a = (float)rng.NextDouble() * Mathf.PI * 2;
            iFalls.Add(new IFall { t = t, at = at, h = h, ang = 25 + (float)rng.NextDouble() * 55, sink = h * (0.15f + (float)rng.NextDouble() * 0.3f), axis = V(Mathf.Cos(a), 0, Mathf.Sin(a)), pos0 = t.localPosition, rot0 = t.localRotation });
        }

        // ---- pieces ----
        void IColumn(Transform root, float x, float z, float h, float r, float y0 = 0)
        {
            IA(root, Geo.Box(r * 2.7f, h * 0.035f, r * 2.7f), iStone, L(root, x, y0 + h * 0.0175f, z));
            IA(root, Geo.Box(r * 2.4f, h * 0.025f, r * 2.4f), iMarble, L(root, x, y0 + h * 0.047f, z));
            IA(root, Geo.Torus(r * 1.08f, r * 0.14f, 20, 6), iMarble, L(root, x, y0 + h * 0.066f, z), root.eulerAngles + V(90, 0, 0));
            IA(root, Geo.Cyl(r * 0.88f, r, h * 0.84f, 16), iMarble, L(root, x, y0 + h * 0.07f + h * 0.42f, z));
            for (int k = 0; k < 10; k++) { float a = k * Mathf.PI * 2 / 10; IA(root, Geo.Box(r * 0.13f, h * 0.8f, r * 0.13f), iStone, L(root, x + Mathf.Cos(a) * r * 0.93f, y0 + h * 0.49f, z + Mathf.Sin(a) * r * 0.93f), root.eulerAngles + V(0, -a * Mathf.Rad2Deg, 0)); } // the fluting
            IA(root, Geo.Torus(r * 0.98f, r * 0.12f, 20, 6), iMarble, L(root, x, y0 + h * 0.915f, z), root.eulerAngles + V(90, 0, 0));
            IA(root, Geo.Cyl(r * 1.4f, r * 0.95f, h * 0.05f, 16), iMarble, L(root, x, y0 + h * 0.945f, z));
            IA(root, Geo.Box(r * 2.9f, h * 0.03f, r * 2.9f), iStone, L(root, x, y0 + h * 0.985f, z));
            foreach (var (sx, sz) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) }) IA(root, Geo.Sphere(r * 0.3f, 8, 6), iMarble, L(root, x + sx * r * 1.15f, y0 + h * 0.95f, z + sz * r * 1.15f)); // the volutes
        }
        void IStatue(Transform root, float x, float y, float z, float h, float yaw = 0, bool wings = true)
        {
            var s = IRoot(root, L(root, x, y, z), root.eulerAngles.y + yaw);
            IA(s, Geo.Cyl(h * 0.09f, h * 0.2f, h * 0.62f, 12), iMarble, L(s, 0, h * 0.31f, 0));
            IA(s, Geo.Sphere(1, 10, 8), iMarble, L(s, 0, h * 0.66f, 0), s.eulerAngles, V(h * 0.15f, h * 0.11f, h * 0.1f)); // shoulders and chest
            IA(s, Geo.Sphere(h * 0.075f, 10, 8), iMarble, L(s, 0, h * 0.82f, 0.01f * h));
            IA(s, Geo.Box(h * 0.025f, h * 0.55f, h * 0.02f), iGold2, L(s, 0, h * 0.35f, h * 0.17f)); // a sword, held point down
            if (wings) foreach (var sx in new[] { -1f, 1f }) for (int f = 0; f < 5; f++) IA(s, Geo.Box(h * 0.06f, h * (0.6f - f * 0.08f), h * 0.02f), iMarble, L(s, sx * (h * 0.1f + f * h * 0.05f), h * (0.75f - f * 0.03f), -h * 0.08f), s.eulerAngles + V(-12, 0, -sx * (10 + f * 9)));
        }
        void IEntab(Transform root, float x0, float x1, float z, float y, float depth)
        {
            float w = x1 - x0, cx = (x0 + x1) / 2;
            IA(root, Geo.Box(w, 1.6f, depth), iMarble, L(root, cx, y + 0.8f, z));             // the architrave
            IA(root, Geo.Box(w, 1.8f, depth * 0.92f), iStone, L(root, cx, y + 2.5f, z));      // the frieze...
            int n = Mathf.Max(2, Mathf.RoundToInt(w / 3.2f)); for (int i = 0; i <= n; i++) IA(root, Geo.Box(0.9f, 1.7f, depth * 0.96f), iMarble, L(root, x0 + w * i / n, y + 2.5f, z)); // ...its triglyphs
            IA(root, Geo.Box(w + 1.6f, 0.9f, depth + 1.6f), iMarble, L(root, cx, y + 3.85f, z));  // the cornice
        }
        Transform ITemple(Transform p, Vector3 c, float yaw, float w, float d, int nx, int nz, float h)
        {
            var t = IRoot(p, c, yaw);
            for (int s = 0; s < 3; s++) IA(t, Geo.Box(w + 8 - s * 2.6f, 1.2f, d + 8 - s * 2.6f), iStone, L(t, 0, 0.6f + s * 1.2f, 0)); // the stepped platform
            float y0 = 3.6f, r = Mathf.Min(w / nx, d / nz) * 0.18f;
            for (int i = 0; i < nx; i++) for (int j = 0; j < nz; j++)
            {
                if (i != 0 && i != nx - 1 && j != 0 && j != nz - 1) continue;
                IColumn(t, -w / 2 + w * i / (nx - 1), -d / 2 + d * j / (nz - 1), h, r, y0);
            }
            // the cella inside the colonnade, its great door
            IA(t, Geo.Box(w * 0.62f, h * 0.92f, d * 0.7f), iStone, L(t, 0, y0 + h * 0.46f, -d * 0.05f));
            IA(t, Geo.Box(w * 0.16f, h * 0.5f, 0.6f), iDoorM, L(t, 0, y0 + h * 0.25f, d * 0.3f + 0.05f));
            IA(t, Geo.Box(w * 0.2f, 0.8f, 0.9f), iGold2, L(t, 0, y0 + h * 0.52f, d * 0.3f + 0.2f));
            // the entablature all round, the roof, the pediments with their statues
            float ye = y0 + h;
            IEntab(t, -w / 2 - 1, w / 2 + 1, d / 2, ye, 2.2f); IEntab(t, -w / 2 - 1, w / 2 + 1, -d / 2, ye, 2.2f);
            IA(t, Geo.Box(2.2f, 4.4f, d), iStone, L(t, -w / 2 - 0.5f, ye + 2.2f, 0)); IA(t, Geo.Box(2.2f, 4.4f, d), iStone, L(t, w / 2 + 0.5f, ye + 2.2f, 0));
            var gable = new GameObject("gable").transform; gable.SetParent(t, false); gable.position = L(t, 0, ye + 4.3f, 0); gable.rotation = t.rotation; gable.localScale = V(1, 0.32f, 1);
            float sd = (w + 3) / 1.4142f; IA(gable, Geo.Box(sd, sd, d + 2), iRoof, gable.position, t.eulerAngles + V(0, 0, 45));
            foreach (var sz in new[] { -1f, 1f }) { var tym = new GameObject("tym").transform; tym.SetParent(t, false); tym.position = L(t, 0, ye + 4.3f, sz * (d / 2 + 1.05f)); tym.rotation = t.rotation; tym.localScale = V(1, 0.3f, 1); IA(tym, Geo.Box(sd * 0.92f, sd * 0.92f, 0.4f), iMarble, tym.position, t.eulerAngles + V(0, 0, 45)); }
            float apex = ye + 4.3f + (w + 3) * 0.5f * 0.32f;
            IStatue(t, 0, apex - 0.4f, d / 2 + 0.8f, h * 0.45f); IStatue(t, -w / 2, ye + 4.3f, d / 2 + 0.8f, h * 0.3f, 0, false); IStatue(t, w / 2, ye + 4.3f, d / 2 + 0.8f, h * 0.3f, 0, false);
            ICrack(t, -w * 0.15f, y0 + h * 0.5f, d * 0.3f + 0.4f, w * 0.5f, h * 0.7f); ICrack(t, w * 0.3f, ye + 2, d / 2 + 1.3f, w * 0.4f, 4);
            return t;
        }
        Transform IGateway(Transform p, Vector3 c, float yaw, float w, float h)
        {
            var g = IRoot(p, c, yaw); float pw = w * 0.26f, pd = w * 0.22f;
            foreach (var sx in new[] { -1f, 1f })
            {
                float x = sx * (w / 2 - pw / 2);
                IA(g, Geo.Box(pw * 1.15f, h * 0.06f, pd * 1.15f), iStoneDk, L(g, x, h * 0.03f, 0));
                for (int k = 0; k < 4; k++) { float yy = h * 0.06f + k * h * 0.2f, sh = 1 - k * 0.05f; IA(g, Geo.Box(pw * sh, h * 0.2f, pd * sh), k % 2 == 0 ? iStone : iMarble, L(g, x, yy + h * 0.1f, 0)); IA(g, Geo.Box(pw * sh + 0.8f, h * 0.012f, pd * sh + 0.8f), iMarble, L(g, x, yy + h * 0.2f, 0)); } // tapering courses, moulded bands
                IA(g, Geo.Box(pw * 0.44f, h * 0.3f, 1.2f), iDoorM, L(g, x, h * 0.36f, pd / 2 + 0.2f)); // a niche...
                IStatue(g, x, h * 0.22f, pd / 2 + 1.6f, h * 0.3f);                                    // ...a guardian in it
                for (int k = 0; k < 2; k++) IColumn(g, x + (k == 0 ? -1 : 1) * pw * 0.36f, pd / 2 + 1.2f, h * 0.36f, pw * 0.05f, h * 0.18f);
                IA(g, Geo.Box(pw * 1.05f, h * 0.05f, pd * 1.05f), iMarble, L(g, x, h * 0.86f, 0));
                IA(g, Geo.Cone(pw * 0.32f, h * 0.18f, 4), iGold2, L(g, x, h * 0.98f, 0), g.eulerAngles + V(0, 45, 0)); // a gilt pyramidion
            }
            float ar = (w - 2 * pw) / 2 + pw * 0.15f;
            IA(g, Geo.Torus(ar, pw * 0.2f, 32, 6, Mathf.PI), iMarble, L(g, 0, h * 0.5f, 0), g.eulerAngles);
            IA(g, Geo.Torus(ar - pw * 0.18f, pw * 0.07f, 32, 5, Mathf.PI), iGold2, L(g, 0, h * 0.5f, pd * 0.25f), g.eulerAngles);
            IA(g, Geo.Box(pw * 0.25f, pw * 0.4f, pd * 0.6f), iGold2, L(g, 0, h * 0.5f + ar + pw * 0.05f, 0)); // the keystone
            IA(g, Geo.Box(w * 0.9f, h * 0.16f, pd * 0.7f), iStone, L(g, 0, h * 0.5f + ar + h * 0.12f, 0)); // the attic
            IEntab(g, -w * 0.45f, w * 0.45f, pd * 0.36f, h * 0.5f + ar + h * 0.04f, 1.4f);
            for (int k = 0; k < 7; k++) IA(g, Geo.Cone(1.4f, 5 + (k % 2) * 3, 6), iGold2, L(g, -w * 0.36f + k * w * 0.12f, h * 0.5f + ar + h * 0.2f + 2.5f, 0)); // a gilt crest
            foreach (var sx in new[] { -1f, 1f }) IA(g, Geo.Box(ar * 0.95f, h * 0.48f, 0.8f), iGold2, L(g, sx * ar * 0.5f, h * 0.24f, -2), g.eulerAngles + V(0, sx * 18, 0)); // the gold doors, ajar
            ICrack(g, -w * 0.32f, h * 0.45f, pd / 2 + 0.7f, pw * 0.9f, h * 0.6f); ICrack(g, w * 0.3f, h * 0.6f, pd / 2 + 0.7f, pw * 0.9f, h * 0.5f); ICrack(g, 0, h * 0.5f + ar + h * 0.1f, pd * 0.36f + 0.8f, w * 0.6f, h * 0.15f);
            return g;
        }
        Transform ISpire(Transform p, Vector3 c, float h, int seed)
        {
            var t = IRoot(p, c, seed * 23); float r = h * 0.075f, y = 0;
            IA(t, Geo.Box(r * 3.2f, h * 0.06f, r * 3.2f), iStoneDk, L(t, 0, h * 0.03f, 0)); y = h * 0.06f;
            for (int i = 0; i < 4; i++)
            {
                float th = h * (0.22f - i * 0.025f), rr = r * (1.25f - i * 0.2f);
                IA(t, Geo.Cyl(rr * 0.95f, rr, th, 8), i % 2 == 0 ? iStone : iMarble, L(t, 0, y + th / 2, 0));
                for (int k = 0; k < 8; k++) { float a = (k + 0.5f) * Mathf.PI / 4; IA(t, Geo.Box(rr * 0.12f, th * 0.9f, rr * 0.12f), iMarble, L(t, Mathf.Cos(a) * rr * 0.98f, y + th * 0.45f, Mathf.Sin(a) * rr * 0.98f)); } // pilasters at the corners
                for (int k = 0; k < 4; k++) { float a = k * Mathf.PI / 2 + 0.39f; IA(t, Geo.Box(rr * 0.3f, th * 0.3f, 0.3f), iDoorM, L(t, Mathf.Cos(a) * rr * 0.93f, y + th * 0.6f, Mathf.Sin(a) * rr * 0.93f), t.eulerAngles + V(0, -a * Mathf.Rad2Deg + 90, 0)); } // tall windows
                y += th;
                IA(t, Geo.Torus(rr * 1.18f, rr * 0.08f, 24, 4), iMarble, L(t, 0, y, 0), t.eulerAngles + V(90, 0, 0)); // a balcony...
                IA(t, Geo.Cyl(rr * 1.22f, rr * 1.22f, 0.6f, 16), iStone, L(t, 0, y - 0.3f, 0));
                for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; IA(t, Geo.Cyl(0.12f * rr, 0.12f * rr, h * 0.025f, 5), iMarble, L(t, Mathf.Cos(a) * rr * 1.15f, y + h * 0.0125f, Mathf.Sin(a) * rr * 1.15f)); } // ...its balustrade
            }
            IA(t, Geo.Sphere(r * 0.75f, 16, 10, 0, Mathf.PI * 2, 0, Mathf.PI / 2), iBronze, L(t, 0, y, 0)); // the dome
            IA(t, Geo.Cyl(r * 0.12f, r * 0.2f, h * 0.06f, 8), iGold2, L(t, 0, y + r * 0.7f + h * 0.03f, 0));
            IA(t, Geo.Cone(r * 0.18f, h * 0.16f, 8), iGold2, L(t, 0, y + r * 0.7f + h * 0.06f + h * 0.08f, 0)); // the gold needle
            ICrack(t, 0, h * 0.35f, r * 1.3f, r * 2, h * 0.35f);
            return t;
        }
        Transform IBasilica(Transform p, Vector3 c, float r)
        {
            var t = IRoot(p, c);
            IA(t, Geo.Box(r * 3, r * 0.5f, r * 3), iStone, L(t, 0, r * 0.25f, 0));
            IA(t, Geo.Box(r * 3.3f, r * 0.06f, r * 3.3f), iMarble, L(t, 0, r * 0.53f, 0));
            IA(t, Geo.Cyl(r, r, r * 0.6f, 24), iStone, L(t, 0, r * 0.86f, 0)); // the drum...
            for (int k = 0; k < 16; k++) { float a = k * Mathf.PI / 8; IColumn(t, Mathf.Cos(a) * r * 1.08f, Mathf.Sin(a) * r * 1.08f, r * 0.6f, r * 0.05f, r * 0.56f); } // ...ringed with columns
            IA(t, Geo.Torus(r * 1.1f, r * 0.05f, 32, 4), iMarble, L(t, 0, r * 1.18f, 0), t.eulerAngles + V(90, 0, 0));
            IA(t, Geo.Sphere(r * 0.98f, 28, 14, 0, Mathf.PI * 2, 0, Mathf.PI / 2), iBronze, L(t, 0, r * 1.16f, 0), default, V(1, 1.25f, 1)); // the dome
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; IA(t, Geo.Box(r * 0.04f, r * 0.04f, r * 1.0f), iGold2, L(t, Mathf.Cos(a) * r * 0.55f, r * 1.16f + r * 0.9f, Mathf.Sin(a) * r * 0.55f), t.eulerAngles + V(-50, -a * Mathf.Rad2Deg + 90, 0)); } // gilt ribs
            IA(t, Geo.Cyl(r * 0.14f, r * 0.16f, r * 0.3f, 10), iMarble, L(t, 0, r * 2.4f, 0)); IA(t, Geo.Cone(r * 0.12f, r * 0.6f, 10), iGold2, L(t, 0, r * 2.85f, 0));
            foreach (var (sx, sz) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) }) { IA(t, Geo.Cyl(r * 0.16f, r * 0.2f, r * 1.5f, 8), iStone, L(t, sx * r * 1.3f, r * 0.5f + r * 0.75f, sz * r * 1.3f)); IA(t, Geo.Cone(r * 0.2f, r * 0.55f, 8), iBronze, L(t, sx * r * 1.3f, r * 2.0f + r * 0.27f, sz * r * 1.3f)); }
            IA(t, Geo.Box(r * 0.5f, r * 0.42f, 0.8f), iDoorM, L(t, 0, r * 0.71f, r * 1.5f + 0.2f));
            ICrack(t, 0, r * 0.9f, r * 1.12f, r * 1.4f, r * 0.6f);
            return t;
        }
        Transform IAqueduct(Transform p, Vector3 a, Vector3 b, float h, int n)
        {
            var t = IRoot(p, (a + b) / 2, Mathf.Atan2(b.x - a.x, b.z - a.z) * Mathf.Rad2Deg - 90); float len = Vector3.Distance(a, b), span = len / n;
            for (int i = 0; i <= n; i++) { float x = -len / 2 + i * span; IA(t, Geo.Box(span * 0.22f, h * 0.62f, 6), iStone, L(t, x, h * 0.31f, 0)); }
            for (int i = 0; i < n; i++) { float x = -len / 2 + (i + 0.5f) * span; IA(t, Geo.Torus(span * 0.39f, span * 0.1f, 18, 5, Mathf.PI), iStone, L(t, x, h * 0.62f, 0)); IA(t, Geo.Box(span * 0.9f, h * 0.14f, 6), iStone, L(t, x, h * 0.62f + span * 0.42f, 0)); }
            IA(t, Geo.Box(len, h * 0.06f, 7), iMarble, L(t, 0, h * 0.62f + span * 0.52f, 0));
            for (int i = 0; i < n * 2; i++) { float x = -len / 2 + (i + 0.5f) * span / 2; IA(t, Geo.Torus(span * 0.17f, span * 0.05f, 12, 4, Mathf.PI), iStone, L(t, x, h * 0.62f + span * 0.55f, 0)); } // the upper tier of small arches
            return t;
        }
        Transform IHouse(Transform p, Vector3 c, float yaw, float w, float d, float h)
        {
            var t = IRoot(p, c, yaw);
            IA(t, Geo.Box(w, h, d), iStone, L(t, 0, h / 2, 0));
            IA(t, Geo.Box(w + 0.6f, 0.5f, d + 0.6f), iMarble, L(t, 0, h, 0));
            var gable = new GameObject("roof").transform; gable.SetParent(t, false); gable.position = L(t, 0, h + 0.25f, 0); gable.rotation = t.rotation; gable.localScale = V(1, 0.55f, 1);
            float sd = (w + 1) / 1.4142f; IA(gable, Geo.Box(sd, sd, d + 1), iRoof, gable.position, t.eulerAngles + V(0, 0, 45));
            for (int k = 0; k < Mathf.Max(1, (int)(w / 4)); k++) IA(t, Geo.Box(1.1f, h * 0.35f, 0.3f), iDoorM, L(t, -w / 2 + 2 + k * 4, h * 0.55f, d / 2 + 0.05f)); // windows
            return t;
        }
        // the whole empire, on the slabs of land (each building moves with the land it stands on, and falls in its turn)
        void IBuildEmpire(Transform s, Vector3 O, System.Random rng)
        {
            float R(float a, float b) => a + (b - a) * (float)rng.NextDouble();
            IArchMats();
            Transform SlabAt(Vector3 p) { var best = iSlabs[0].t; float bd = 1e9f; foreach (var sl in iSlabs) { float d = (sl.home.x - p.x) * (sl.home.x - p.x) + (sl.home.z - p.z) * (sl.home.z - p.z); if (d < bd) { bd = d; best = sl.t; } } return best; }
            Transform Put(Transform t, float fallAt) { var sl = SlabAt(t.position); t.SetParent(sl, true); IFallReg(t, fallAt, 60, rng); return t; }
            // an avenue of great columns along the road in, closing on the gateway
            for (float z = 150; z > -70; z -= 24) foreach (var x in new[] { -104f, 8f }) { var p = O + V(x, 0, z); if (INearPath(p - O)) continue; var col = IRoot(s, p); IColumn(col, 0, 0, 34, 2.2f); IStatue(col, 0, 34, 0, 7, x < 0 ? 90 : -90); Put(col, R(0.4f, 3.2f)); }
            Put(IGateway(s, O + V(-48, 0, -118), 0, 92, 80), 1.2f);
            Put(ITemple(s, O + V(150, 0, -10), -20, 46, 66, 6, 8, 22), R(0.5f, 2.5f));
            Put(ITemple(s, O + V(-215, 0, -30), 25, 50, 70, 6, 8, 24), R(0.5f, 2.5f));
            Put(ITemple(s, O + V(-120, 0, -270), 5, 70, 90, 8, 10, 30), R(0.8f, 2.8f));
            Put(IBasilica(s, O + V(60, 0, -300), 34), 2.0f);
            foreach (var (x, z, h) in new[] { (-270f, -190f, 110f), (240f, -150f, 125f), (-20f, -440f, 150f), (310f, -380f, 105f), (-340f, -330f, 95f), (170f, -470f, 120f) }) Put(ISpire(s, O + V(x, 0, z), h, (int)x), R(0.3f, 3.0f));
            Put(IAqueduct(s, O + V(-420, 0, -520), O + V(420, 0, -520), 70, 14), 2.6f);
            // streets of houses filling the land between
            for (int k = 0; k < 70; k++)
            {
                var p = O + V(R(-440, 440), 0, R(-480, 120)); if (INearPath(p - O)) continue; if (p.x - O.x > -116 && p.x - O.x < 20 && p.z - O.z > -150) continue; // (the avenue stays clear)
                bool clear = true; foreach (var f in iFalls) if (f.t && Vector3.Distance(f.t.position, p) < 40) { clear = false; break; } if (!clear) continue;
                var hs = IHouse(s, p, R(0, 360), R(8, 16), R(8, 14), R(7, 14)); hs.SetParent(SlabAt(p), true);
                if (rng.NextDouble() < 0.5) IFallReg(hs, R(0.5f, 3.5f), 12, rng);
            }
        }
        // the light for it all: a low sun raking across the stone; long shadows
        void IArchLight(bool on)
        {
            var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            if (on)
            {
                if (!iLight) { var g = new GameObject("introSun"); g.transform.SetParent(world, false); iLight = g.AddComponent<Light>(); iLight.type = LightType.Directional; iLight.cullingMask = 1 << CINE_LAYER; iLight.shadows = LightShadows.Soft; iLight.shadowStrength = 0.85f; }
                iLight.transform.rotation = Quaternion.LookRotation(V(-0.75f, -0.38f, -0.55f).normalized); iLight.enabled = true;
                if (urp && iShadowPrev < 0) { iShadowPrev = urp.shadowDistance; urp.shadowDistance = 700; }
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            }
            else
            {
                if (iLight) Destroy(iLight.gameObject); iLight = null;
                if (urp && iShadowPrev >= 0) urp.shadowDistance = iShadowPrev; iShadowPrev = -1;
            }
        }
        void IArchAmbient(Color sky, Color eq, Color ground) { RenderSettings.ambientSkyColor = sky; RenderSettings.ambientEquatorColor = eq; RenderSettings.ambientGroundColor = ground; }
        // the fall: the cracks glow, buildings tilt, sink and collapse, shedding stone
        void IArchCollapse(float lt, float rdt)
        {
            foreach (var f in iFalls)
            {
                if (!f.t) continue; float p = Mathf.Clamp01((lt - f.at) / 1.8f); if (p <= 0) continue; p = p * p;
                f.t.localRotation = f.rot0 * Quaternion.AngleAxis(f.ang * p, f.axis); f.t.localPosition = f.pos0 - V(0, f.sink * p, 0) + V(Mathf.Sin(lt * 37 + f.at) * 0.3f * (1 - p), 0, 0);
                if (!f.shed && p > 0.05f) { f.shed = true; if (iDebris.Count < 260) IDebrisAt(iSets[0], f.t.position + V(0, f.h * 0.6f, 0), f.h > 30 ? 14 : 5, 10, f.h > 30 ? 4 : 2, new System.Random((int)(f.at * 1000))); }
            }
        }
    }
}
