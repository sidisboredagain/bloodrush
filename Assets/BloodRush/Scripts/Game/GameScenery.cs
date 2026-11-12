using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    // Living backdrops beyond the arena walls, and god rays in the heaven levels.
    //  hell:   chained titans swaying on the horizon (eyes burning), fire storms flickering under the moon
    //  heaven: cathedrals floating among the clouds, angels gliding round the arena on slow wingbeats
    // The silhouettes ignore fog (they stand far beyond the fog distance); everything is built with the level,
    // so the editor preview shows it too.
    public partial class Game
    {
        Material Silhouette(int color, float alpha = 1) => Mats.Trans(color, alpha, false, false);

        void BuildBackdrop(string kind, float Bd)
        {
            if (ActBackdrop(kind, Bd)) return;
            if (kind == "moon" || kind == "volcano") { HellTitans(kind == "volcano"); FireStorms(kind == "volcano"); }
            else { FloatingCathedrals(kind == "halo"); Angels(kind == "halo" ? 4 : 6); }
        }

        // ---------- hell: chained titans ----------
        void HellTitans(bool crucible)
        {
            var body = Silhouette(crucible ? 0x120302 : 0x080202);
            var chainM = Silhouette(crucible ? 0x1a0604 : 0x0e0404);
            float[] angs = { -2.3f, -0.55f, 1.15f, 2.55f };
            for (int i = 0; i < angs.Length; i++)
            {
                // huge, so they tower over the tall hell walls from anywhere in the arena
                // hung in the sky by their chains, high enough to loom over the walls (and inside the camera's 460 m reach)
                float a = angs[i], r = 320 + (i % 2) * 30, ph = i * 1.9f, s = 2.3f + (i % 3) * 0.25f;
                var root = new GameObject("titan").transform; root.SetParent(levelRoot, false);
                var flat = V(Mathf.Cos(a), 0, Mathf.Sin(a));
                root.localPosition = flat * r + V(0, crucible ? 45 : 85, 0); root.localRotation = Quaternion.LookRotation(flat); // eyes (local -z) face the arena
                root.localScale = Vector3.one * s;
                var torso = Mk.Piv(root, 0, 0, 0);
                Mk.Msh(torso.t, Geo.Box(30, 46, 16), body, 0, 23, 0);
                Mk.Msh(torso.t, Geo.Box(40, 10, 18), body, 0, 44, 0);                      // shoulders
                var head = Mk.Piv(torso.t, 0, 52, 0);
                Mk.Msh(head.t, Geo.Sphere(9, 12, 10), body);
                Mk.Msh(head.t, Geo.Cone(2.2f, 9, 5), body, -6, 7, 0, 0, 0, 0.5f);           // horns
                Mk.Msh(head.t, Geo.Cone(2.2f, 9, 5), body, 6, 7, 0, 0, 0, -0.5f);
                var eyes = new List<Material>();
                foreach (var sx in new[] { -3.2f, 3.2f }) { var eg = Mk.Glow(head.t, 0xff3010, 7, 0.9f, false); eg.transform.localPosition = V(sx, 1, -8.5f); eyes.Add(eg.GetComponent<Renderer>().sharedMaterial); }
                var arms = new List<N>();
                foreach (var sx in new[] { -1, 1 })
                {
                    var arm = Mk.Piv(torso.t, 22 * sx, 44, 0); arm.SetRot(0, 0, 0.35f * sx); arms.Add(arm);
                    Mk.Msh(arm.t, Geo.Box(9, 34, 9), body, 0, -17, 0);
                    // wrist chains pulled up into the dark sky and out to the sides
                    Mk.Msh(arm.t, Geo.Cyl(0.9f, 0.9f, 170, 5), chainM, 0, -34 + 85, 0, 0, 0, -0.12f * sx);
                    Mk.Msh(arm.t, Geo.Cyl(0.7f, 0.7f, 120, 5), chainM, 50 * sx, -34, 0, 0, 0, Mathf.PI / 2 * sx * 0.9f);
                }
                Mk.Msh(torso.t, Geo.Cyl(1.2f, 1.2f, 160, 5), chainM, 0, 30, 6, 0.9f, 0, 0);    // collar chain
                // a fire glow behind each titan, so its silhouette stands out against the dark sky
                var back = Mk.Glow(levelRoot, crucible ? 0xff4a10 : 0xff2a10, 120 * s, 0.5f, false); back.transform.localPosition = flat * (r + 25) + root.localPosition.y * Vector3.up + V(0, 40 * s, 0);
                var backM = back.GetComponent<Renderer>().sharedMaterial;
                anims.Add((t, dt) => Mats.SetAlpha(backM, 0.38f + 0.12f * Mathf.Sin(t * 0.9f + ph) + 0.06f * Mathf.Sin(t * 4.3f + ph)));
                anims.Add((t, dt) =>
                {
                    torso.SetRot(Mathf.Sin(t * 0.21f + ph) * 0.04f, Mathf.Sin(t * 0.13f + ph) * 0.05f, Mathf.Sin(t * 0.17f + ph) * 0.06f); // straining against the chains
                    head.SetRot(Mathf.Sin(t * 0.3f + ph) * 0.12f, Mathf.Sin(t * 0.11f + ph * 2) * 0.35f, 0);
                    for (int k = 0; k < arms.Count; k++) arms[k].rz = (k == 0 ? -1 : 1) * (0.35f + Mathf.Sin(t * 0.4f + ph + k) * 0.12f);
                    float glow = 0.55f + 0.35f * Mathf.Sin(t * 1.3f + ph) + (Mathf.Sin(t * 7 + ph) > 0.97f ? 0.4f : 0);
                    foreach (var m in eyes) Mats.SetAlpha(m, Mathf.Clamp01(glow));
                });
            }
        }
        // ---------- hell: fire storms on the horizon ----------
        void FireStorms(bool crucible)
        {
            var storms = new List<(Transform t, Material m, float ph, float s)>();
            for (int i = 0; i < 9; i++)
            {
                float a = i / 9f * Mathf.PI * 2 + 0.3f, r = 240 + (i % 3) * 30, s = 70 + (i * 37) % 70;
                var g = Mk.Glow(levelRoot, crucible ? 0xff5000 : (i % 2 == 0 ? 0xff3a10 : 0xff7020), s, 0.45f, false);
                g.transform.localPosition = V(Mathf.Cos(a) * r, 8 + (i % 4) * 9, Mathf.Sin(a) * r);
                storms.Add((g.transform, g.GetComponent<Renderer>().sharedMaterial, i * 1.7f, s));
            }
            // drifting ember clouds that rise and fade, then start again
            var drift = new List<(Transform t, Material m, float ph, Vector3 p0)>();
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2, r = 200 + (i % 2) * 50;
                var g = Mk.Glow(levelRoot, 0xff9040, 30, 0.3f, false); var p0 = V(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                g.transform.localPosition = p0; drift.Add((g.transform, g.GetComponent<Renderer>().sharedMaterial, i * 0.13f, p0));
            }
            anims.Add((t, dt) =>
            {
                foreach (var s in storms)
                {
                    float f = 0.75f + 0.25f * Mathf.Sin(t * 2.3f + s.ph) * Mathf.Sin(t * 5.1f + s.ph * 2) + (Mathf.Sin(t * 0.7f + s.ph) > 0.9f ? 0.35f : 0);
                    s.t.localScale = Vector3.one * s.s * (0.9f + 0.2f * f); Mats.SetAlpha(s.m, 0.3f + 0.25f * f);
                }
                foreach (var d in drift)
                {
                    float u = (t * 0.035f + d.ph) % 1;
                    d.t.localPosition = d.p0 + V(Mathf.Sin(t * 0.2f + d.ph * 9) * 15, u * 140, 0);
                    Mats.SetAlpha(d.m, Mathf.Sin(u * Mathf.PI) * 0.35f);
                }
            });
        }

        // ---------- heaven: floating cathedrals ----------
        void FloatingCathedrals(bool sanctum)
        {
            var stone = Silhouette(sanctum ? 0xd8c8a0 : 0xbfcadc, 0.92f); var gold = Silhouette(0xe8c060, 0.95f);
            float[] angs = { -1.9f, -0.2f, 1.3f, 2.7f };
            for (int i = 0; i < angs.Length; i++)
            {
                float a = angs[i], r = 270 + (i % 2) * 45, y = 45 + (i * 23) % 50, ph = i * 1.4f, s = 0.9f + (i % 3) * 0.2f;
                var root = new GameObject("floating cathedral").transform; root.SetParent(levelRoot, false);
                var basePos = V(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r); root.localPosition = basePos; root.localScale = Vector3.one * s;
                Mk.Msh(root, Geo.Cone(22, 40, 7), stone, 0, -20, 0, Mathf.PI, 0, 0);       // the rock it stands on, hanging below
                Mk.Msh(root, Geo.Box(26, 14, 16), stone, 0, 7, 0);                         // nave
                Mk.Msh(root, Geo.Box(28, 2, 18), stone, 0, 14.5f, 0);
                Mk.Msh(root, Geo.Cone(15, 9, 4), stone, 0, 19, 0, 0, Mathf.PI / 4, 0);
                foreach (var (sx, h) in new[] { (-11f, 34f), (11f, 34f), (0f, 46f) })
                {
                    Mk.Msh(root, Geo.Box(5, h, 5), stone, sx, h / 2, sx == 0 ? 6 : -4);
                    Mk.Msh(root, Geo.Cone(3.6f, 10, 6), gold, sx, h + 5, sx == 0 ? 6 : -4);
                }
                var win = Mk.Glow(root, 0xfff0c0, 14, 0.5f, false); win.transform.localPosition = V(0, 8, 9);
                Mk.Glow(root, 0xffffff, 60, 0.18f, false).transform.localPosition = V(0, -10, 0); // soft cloud bank under it
                anims.Add((t, dt) =>
                {
                    root.localPosition = basePos + V(0, Mathf.Sin(t * 0.25f + ph) * 3, 0);
                    root.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.18f + ph) * 1.5f, t * 1.2f + ph * 30, Mathf.Sin(t * 0.21f + ph) * 1.5f);
                });
            }
        }
        // ---------- heaven: angels gliding round the arena ----------
        void Angels(int n)
        {
            var white = Silhouette(0xf8f6f0, 0.95f); var wingM = Mats.Trans(0xffffff, 0.85f, true, false); // wings are seen from both sides
            var wingMesh = GeoX.ShapeMB(new GeoX.Shape().MoveTo(0, 0).QuadTo(6, 4, 14, 2).LineTo(11, -1).LineTo(8, -0.5f).LineTo(5, -2).LineTo(0, -1.2f).LineTo(0, 0)).Build();
            for (int i = 0; i < n; i++)
            {
                float r = 170 + (i * 29) % 80, h = 55 + (i * 17) % 50, speed = (0.05f + (i % 3) * 0.015f) * (i % 2 == 0 ? 1 : -1), ph = i * 1.1f;
                var root = new GameObject("angel").transform; root.SetParent(levelRoot, false); root.localScale = Vector3.one * 1.4f;
                Mk.Msh(root, Geo.Cone(1.6f, 8, 6), white, 0, -2, 0, Mathf.PI, 0, 0);         // robe
                Mk.Msh(root, Geo.Sphere(1.1f, 8, 6), white, 0, 2.6f, 0);
                var halo = Mk.Glow(root, 0xffe08a, 6, 0.7f, false); halo.transform.localPosition = V(0, 4.2f, 0);
                var wings = new List<Transform>();
                foreach (var sx in new[] { -1, 1 })
                {
                    var w = Mk.Piv(root, 0, 1.4f, 0.6f);
                    Mk.Msh(w.t, wingMesh, wingM, 0, 0, 0, 0, sx < 0 ? Mathf.PI : 0, 0);
                    wings.Add(w.t);
                }
                System.Action<float, float> fly = (t, dt) =>
                {
                    float a = t * speed + ph;
                    var pos = V(Mathf.Cos(a) * r, h + Mathf.Sin(t * 0.4f + ph) * 6, Mathf.Sin(a) * r);
                    var vel = V(-Mathf.Sin(a), 0, Mathf.Cos(a)) * Mathf.Sign(speed);
                    root.localPosition = pos; root.localRotation = Quaternion.LookRotation(vel) * Quaternion.Euler(0, 0, -Mathf.Sign(speed) * 12);
                    float flap = Mathf.Sin(t * 2.2f + ph * 3) * 0.6f;
                    for (int k = 0; k < wings.Count; k++) wings[k].localRotation = Quaternion.Euler(0, 0, (k == 0 ? -1 : 1) * (flap * Mathf.Rad2Deg));
                };
                fly(0, 0); // on its flight path from the start (otherwise it sits on the altar until the level starts updating)
                anims.Add(fly);
            }
        }

        // ---------- god rays ----------
        void LightShaft(Vector3 src, Vector3 dst, float r0, float r1, int color, float alpha)
        {
            var dir = src - dst; float len = dir.magnitude; var rot = Quaternion.FromToRotation(Vector3.up, dir / len);
            // on the bright heaven sky an additive beam saturates to solid white, so the beams are much fainter there
            if (THEME.holy) alpha *= 0.35f;
            var outer = Mats.Add(color, alpha, Tex.RayFade, true, true); var inner = Mats.Add(0xffffff, alpha * 0.9f, Tex.RayFade, true, true);
            var g = Mk.Msh(levelRoot, Geo.Cyl(r0, r1, 1, 18, true), outer); g.layer = Mk.LayerIgnore;
            g.transform.localPosition = (src + dst) * 0.5f; g.transform.localRotation = rot; g.transform.localScale = V(1, len, 1);
            var c = Mk.Msh(levelRoot, Geo.Cyl(r0 * 0.4f, r1 * 0.45f, 1, 14, true), inner); c.layer = Mk.LayerIgnore;
            c.transform.localPosition = (src + dst) * 0.5f; c.transform.localRotation = rot; c.transform.localScale = V(1, len, 1);
            float ph = src.x * 0.1f + src.z * 0.07f;
            anims.Add((t, dt) =>
            {
                float b = 0.82f + 0.18f * Mathf.Sin(t * 0.6f + ph) * Mathf.Sin(t * 0.23f + ph * 2);
                Mats.SetAlpha(outer, alpha * b); Mats.SetAlpha(inner, alpha * 0.9f * b);
                if (rand < 0.25f) // dust motes drifting in the light
                {
                    float u = 0.2f + rand * 0.75f; var p = Vector3.Lerp(src, dst, u) + Random.insideUnitSphere * Mathf.Lerp(r0, r1, u) * 0.7f;
                    particles.Add(new Pt { pos = p, scale = Vector3.one * 0.07f, mat = moteMat, v = V(rand - 0.5f, -0.3f - rand * 0.4f, rand - 0.5f) * 0.6f, life = 3, grav = 0 });
                }
            });
        }
        // ---------- heaven set dressing (Gates of Dawn, Cloister of Echoes) ----------
        Material marbleM, goldM, clothM, flameM;
        void HolyMats()
        {
            marbleM = Mats.Std(0xf4f0e6, 0.45f); goldM = Mats.Std(0xe0b040, 0.3f, 0.8f);
            clothM = Mats.Std(0xf6f2ea, 0.9f, 0, 0, null, true); flameM = Mats.Basic(0xfff4d8);
        }
        // a praying angel on a pedestal, wings half raised, a gold halo
        void AngelStatue(float x, float z, float ry, float s = 1)
        {
            Box(x, 0, z, 4, 2.4f, 4, M.block); TrimRect(x, z, 4, 4, 2.4f);
            var p = Mk.Piv(levelRoot, x, 2.4f, z); p.SetRot(0, ry, 0); p.t.localScale = Vector3.one * s; var t = p.t;
            Mk.Msh(t, Geo.Cyl(0.38f, 1.0f, 3.2f, 14), marbleM, 0, 1.6f, 0);                           // robe
            Mk.Msh(t, Geo.Cyl(0.3f, 0.38f, 1.0f, 12), marbleM, 0, 3.7f, 0);                           // torso
            Mk.Msh(t, Geo.Sphere(0.32f, 14, 10), marbleM, 0, 4.5f, 0.05f);
            // eyes that flare when the statue wakes (Sentinel Statues trap) and a solid body
            var eyeG = Mk.Glow(t, 0xffb050, 0.9f, 0.25f); eyeG.transform.localPosition = V(0, 4.55f, 0.32f);
            sentinels.Add(new Sentinel { eye = t.TransformPoint(V(0, 4.55f, 0.5f)) - levelRoot.position, eyeM = eyeG.GetComponent<Renderer>().sharedMaterial });
            SolidAABB(V(x - 1.1f, 2.4f, z - 1.1f), V(x + 1.1f, 2.4f + 5.4f * s, z + 1.1f));
            Mk.Msh(t, Geo.Torus(0.42f, 0.05f, 24, 6), goldM, 0, 5.05f, -0.1f, Mathf.PI / 2 - 0.25f, 0, 0);
            Mk.Glow(t, 0xfff0c0, 2.2f, 0.45f).transform.localPosition = V(0, 5.05f, -0.1f);
            foreach (var sx in new[] { -1f, 1f })
            {
                Mk.Msh(t, Geo.Cyl(0.11f, 0.09f, 0.95f, 8), marbleM, sx * 0.17f, 3.75f, 0.32f, -1.15f, 0, sx * 0.2f); // hands joined in prayer
                Mk.Msh(t, Geo.Box(0.14f, 3.4f, 1.3f), marbleM, sx * 0.7f, 4.3f, -0.45f, 0.15f, sx * 0.45f, -sx * 0.42f); // wings
                Mk.Msh(t, Geo.Box(0.12f, 2.5f, 1.0f), marbleM, sx * 1.05f, 3.55f, -0.55f, 0.2f, sx * 0.5f, -sx * 0.75f);
                Mk.Msh(t, Geo.Box(0.1f, 1.6f, 0.8f), marbleM, sx * 1.3f, 2.9f, -0.6f, 0.25f, sx * 0.55f, -sx * 1.0f);
            }
        }
        // a gold brazier burning with pale holy fire
        void Brazier(float x, float z)
        {
            Deco(Geo.Cyl(0.16f, 0.26f, 1.6f, 8), M.metal, x, 0.8f, z);
            Deco(Geo.Cyl(0.75f, 0.32f, 0.5f, 14), goldM, x, 1.85f, z);
            Deco(Geo.Torus(0.75f, 0.06f, 24, 6), goldM, x, 2.1f, z, Mathf.PI / 2);
            var f1 = Mk.Glow(levelRoot, 0xfff0c0, 2.4f, 0.75f); f1.transform.localPosition = V(x, 2.6f, z);
            var f2 = Mk.Glow(levelRoot, 0xffffff, 1.2f, 0.9f); f2.transform.localPosition = V(x, 2.4f, z);
            colliders.Add(new AABB(V(x - 0.75f, 0, z - 0.75f), V(x + 0.75f, 2.1f, z + 0.75f)));
            float ph = x * 0.37f + z * 0.21f;
            anims.Add((t, dt) =>
            {
                f1.transform.localScale = Vector3.one * (2.4f + 0.35f * Mathf.Sin(t * 9 + ph) + 0.2f * Mathf.Sin(t * 23 + ph));
                f2.transform.localScale = Vector3.one * (1.2f + 0.15f * Mathf.Sin(t * 13 + ph));
                if (rand < 0.12f) SpawnParticles(V(x + (rand - 0.5f) * 0.6f, 2.5f, z + (rand - 0.5f) * 0.6f), 1, 0.6f, moteMat, 0.07f, 1.6f, -1.5f);
            });
        }
        void LampPost(float x, float z)
        {
            Deco(Geo.Cyl(0.09f, 0.14f, 4.2f, 8), M.metal, x, 2.1f, z);
            Deco(Geo.Cyl(0.32f, 0.2f, 0.2f, 10), goldM, x, 0.1f, z);
            Deco(Geo.Sphere(0.32f, 12, 8), flameM, x, 4.45f, z);
            Deco(Geo.Torus(0.36f, 0.04f, 20, 5), goldM, x, 4.45f, z, Mathf.PI / 2);
            Mk.Glow(levelRoot, 0xfff2d0, 3.2f, 0.55f).transform.localPosition = V(x, 4.45f, z);
            colliders.Add(new AABB(V(x - 0.15f, 0, z - 0.15f), V(x + 0.15f, 4.2f, z + 0.15f)));
        }
        // white tapestries with a gold rod, stripe and emblem, hung on the inside of the arena walls between the trims
        void WallTapestries(float Bd, float step, float wallH)
        {
            float h = wallH * 0.55f, y = wallH * 0.52f;
            OnWalls(Bd, step, (px, pz, nx, nz) =>
            {
                var g = WallG(px, pz, nx, nz);
                Mk.Msh(g, Geo.Box(2.6f, h, 0.06f), clothM, 0, y, 0.1f);
                Mk.Msh(g, Geo.Box(3.1f, 0.16f, 0.16f), goldM, 0, y + h / 2 + 0.1f, 0.14f);
                Mk.Msh(g, Geo.Box(2.6f, 0.22f, 0.08f), goldM, 0, y - h / 2 + 0.45f, 0.12f);
                Mk.Msh(g, Geo.Box(0.9f, 0.9f, 0.08f), goldM, 0, y + 0.3f, 0.13f, 0, 0, Mathf.PI / 4);
                Mk.Msh(g, Geo.Box(0.08f, h * 0.8f, 0.08f), goldM, 0, y - 0.2f, 0.12f);
            }, step / 2);
        }
        // lanterns drifting high over the arena
        void FloatingLanterns(int n, float r0, float r1, float y0)
        {
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2 + 0.4f, r = Mathf.Lerp(r0, r1, (i * 0.618f) % 1), ph = i * 1.7f, lift = (i % 3) * 2;
                var root = Mk.Piv(levelRoot, Mathf.Cos(a) * r, y0, Mathf.Sin(a) * r);
                Mk.Msh(root.t, Geo.Sphere(0.55f, 6, 4), flameM, 0, 0, 0);
                Mk.Msh(root.t, Geo.Torus(0.55f, 0.05f, 16, 4), goldM, 0, 0, 0, Mathf.PI / 2);
                Mk.Glow(root.t, 0xfff0c0, 4, 0.5f);
                var basePos = root.t.localPosition;
                anims.Add((t, dt) => { root.t.localPosition = basePos + V(Mathf.Sin(t * 0.3f + ph) * 1.5f, Mathf.Sin(t * 0.6f + ph) * 1.2f + lift, Mathf.Cos(t * 0.25f + ph) * 1.5f); root.SetRot(0, t * 0.4f + ph, 0); });
            }
        }
        // a broken marble column drum lying on its side: veined stone, fluting, worn end rings, cracked faces; solid and grabbable
        void ColumnDrum(Vector3 c, float r, float len, float yaw)
        {
            var piv = Mk.Piv(levelRoot, c.x, c.y, c.z); piv.SetRot(0, yaw, 0); var t = piv.t;
            Mk.Msh(t, Geo.Cyl(r, r, len, 20), M.block, 0, 0, 0, Mathf.PI / 2, 0, 0);
            var groove = Mats.Std(0xb8ac94, 0.7f);
            for (int k = 0; k < 16; k++) { float a = k * Mathf.PI / 8; Mk.Msh(t, Geo.Box(0.12f, 0.12f, len * 0.96f), groove, Mathf.Cos(a) * r * 0.97f, Mathf.Sin(a) * r * 0.97f, 0, 0, 0, a); }
            foreach (var ez in new[] { -len / 2, len / 2 }) { Mk.Msh(t, Geo.Torus(r * 0.98f, 0.1f, 28, 5), goldM, 0, 0, ez); Mk.Msh(t, Geo.Cyl(r * 0.8f, r * 0.8f, 0.04f, 18), M.tower, 0, 0, ez + Mathf.Sign(ez) * 0.01f, Mathf.PI / 2, 0, 0); }
            for (int k = 0; k < 6; k++) Mk.Msh(t, Geo.Box(0.5f + rand * 0.6f, 0.4f, 0.4f), M.block, (rand - 0.5f) * r * 2.4f, -r + 0.15f, (rand - 0.5f) * len * 1.6f, rand, rand * 3, rand); // broken chips beside it
            var ext = Quaternion.Euler(0, yaw * Mathf.Rad2Deg, 0) * new Vector3(0, 0, len / 2); var hx = Mathf.Abs(ext.x) + r * 0.8f; var hz = Mathf.Abs(ext.z) + r * 0.8f;
            SolidAABB(V(c.x - hx, 0, c.z - hz), V(c.x + hx, c.y + r * 0.9f, c.z + hz));
        }
        void GatesOfDawnDressing()
        {
            HolyMats(); sentinels.Clear();
            foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 }) AngelStatue(sx * Hv(54), sz * Hv(54), Mathf.Atan2(-sx, -sz));
            foreach (var sx in new[] { -1, 1 }) AngelStatue(sx * Hv(20), Hv(-53), 0, 1.15f);                     // flanking the gate
            Box(0, 0, Hv(-48.5f), 14, 0.4f, 3); Box(0, 0, Hv(-51), 14, 0.8f, 2.5f);                              // steps up to the gate
            TrimRect(0, Hv(-48.5f), 14, 3, 0.4f); TrimRect(0, Hv(-51), 14, 2.5f, 0.8f);
            // braziers on the outer side of the colonnades, lamps along the path from the spawn
            foreach (var sx in new[] { -1, 1 }) foreach (var z in new[] { -30, -14, 14, 30 }) Brazier(sx * Hv(26), Hv(z));
            foreach (var sx in new[] { -1, 1 }) foreach (var z in new[] { 18, 26, 42, 52 }) LampPost(sx * 5, Hv(z));
            // banners hung from the colonnade lintels, swaying a little
            foreach (var sx in new[] { -1, 1 }) foreach (var z in new[] { -35, -15, 15, 35 })
            {
                var b = Mk.Piv(levelRoot, sx * (Hv(22) - 1.4f), 14, Hv(z)); var bt = b.t;
                Mk.Msh(bt, Geo.Box(0.06f, 5, 2.2f), clothM, 0, -2.6f, 0);
                Mk.Msh(bt, Geo.Box(0.1f, 0.3f, 2.3f), goldM, 0, -0.25f, 0); Mk.Msh(bt, Geo.Box(0.1f, 0.25f, 2.2f), goldM, 0, -4.8f, 0);
                Mk.Msh(bt, Geo.Box(0.1f, 0.75f, 0.75f), goldM, 0, -2.4f, 0, Mathf.PI / 4, 0, 0);
                float ph = z * 0.13f + sx;
                anims.Add((t, dt) => b.SetRot(0, 0, Mathf.Sin(t * 0.9f + ph) * 0.05f * sx));
            }
            // gold inlays round the altar
            Deco(Geo.Torus(Hv(14), 0.14f, 96, 4), M.trim, 0, 0.0f, 0, Mathf.PI / 2); Deco(Geo.Torus(Hv(19), 0.1f, 96, 4), M.trim, 0, 0.0f, 0, Mathf.PI / 2);
            for (int k = 0; k < 16; k++) { float a = k * Mathf.PI / 8; Deco(Geo.Box(0.18f, 0.06f, Hv(5)), M.trim, Mathf.Sin(a) * Hv(16.5f), 0.0f, Mathf.Cos(a) * Hv(16.5f), 0, a, 0); }
            // ruins: fallen column drums and a broken stump in the side yards
            foreach (var sx in new[] { -1, 1 })
            {
                Box(sx * Hv(55), 0, Hv(-36), 2.6f, 3.2f, 2.6f, M.tower);
                ColumnDrum(V(sx * Hv(52), 1.2f, Hv(-30)), 1.25f, 4, 0.4f * sx);
                ColumnDrum(V(sx * Hv(56), 1.2f, Hv(-24)), 1.2f, 3, -0.9f * sx);
                ColumnDrum(V(sx * Hv(54), 1.2f, Hv(32)), 1.28f, 2.4f, 1.2f);
                Box(sx * Hv(57), 0, Hv(38), 3, 1.4f, 3, M.tower); Deco(Geo.Box(1.4f, 0.8f, 1.1f), M.tower, sx * Hv(52), 0.4f, Hv(37), 0, 0.6f, 0);
            }
            WallTapestries(Hv(64), 16, THEME.wallH);
            MechSentinels();
        }
        // a pale tree with golden blossom in a marble planter; petals drift down from it
        // a mesh stood along the segment a-b (meshes are built along +y)
        GameObject Rod(Mesh m, Material mat, Vector3 a, Vector3 b) { var d = b - a; var g = Deco(m, mat, (a.x + b.x) / 2, (a.y + b.y) / 2, (a.z + b.z) / 2); g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized); return g; }
        void BlossomTree(float x, float z, float s)
        {
            // octagonal marble planter with a gold rim and little white flowers round the soil
            var rng = new System.Random((int)(x * 73 + z * 191));
            float R() => (float)rng.NextDouble();
            Deco(Geo.Cyl(2.1f, 2.3f, 0.9f, 8), M.block, x, 0.45f, z); Deco(Geo.Torus(2.1f, 0.09f, 32, 4), goldM, x, 0.92f, z, Mathf.PI / 2);
            Deco(Geo.Cyl(1.95f, 1.95f, 0.06f, 16), Mats.Std(0x6a5a3a, 0.95f), x, 0.9f, z);
            var petalM = Mats.Std(0xfff8f0, 0.7f);
            for (int k = 0; k < 14; k++) { float a = k / 14f * Mathf.PI * 2 + R() * 0.3f, rr = 1.4f + R() * 0.4f; Deco(Geo.Sphere(0.09f, 6, 4), petalM, x + Mathf.Cos(a) * rr, 0.97f, z + Mathf.Sin(a) * rr); }
            colliders.Add(new AABB(V(x - 2.2f, 0, z - 2.2f), V(x + 2.2f, 0.9f, z + 2.2f)));
            var bark = Mats.Std(0xece4d4, 0.75f); var bark2 = Mats.Std(0xcfc2a8, 0.8f);
            // a softly leaning trunk in tapering segments, with flared roots
            Vector3 p = V(x, 0.9f, z); float lean = R() * Mathf.PI * 2; float tr = 0.42f * s;
            for (int k = 0; k < 4; k++) { float a = lean + R(); Deco(Geo.Cone(0.16f * s, 0.9f * s, 5), bark2, x + Mathf.Cos(a) * 0.35f * s, 1.05f, z + Mathf.Sin(a) * 0.35f * s, 0, -a, 1.25f); }
            for (int k = 0; k < 4; k++)
            {
                float h = 1.25f * s, tx = Mathf.Cos(lean) * 0.14f * s * (k % 2 == 0 ? 1 : -0.6f), tz = Mathf.Sin(lean) * 0.14f * s * (k % 2 == 0 ? 1 : -0.6f);
                var q = p + V(tx, h, tz); var d = q - p;
                Rod(Geo.Cyl(tr * 0.8f, tr, d.magnitude + 0.06f, 9), k % 2 == 0 ? bark : bark2, p, q);
                p = q; tr *= 0.8f;
            }
            float top = p.y;
            // branches fan out from the crown, each carrying a blossom cloud in three soft shades
            Material[] leaf = { Mats.Std(0xf8eab0, 0.85f, 0, 0x201806), Mats.Std(0xfbe8e2, 0.85f, 0, 0x201410), Mats.Std(0xecd27e, 0.85f, 0, 0x1a1204) };
            for (int b = 0; b < 5; b++)
            {
                float a = b / 5f * Mathf.PI * 2 + R() * 0.6f, up = 0.55f + R() * 0.35f, len = (1.6f + R() * 0.8f) * s;
                var dir = V(Mathf.Cos(a) * Mathf.Cos(up), Mathf.Sin(up), Mathf.Sin(a) * Mathf.Cos(up)); var tip = p + dir * len;
                Rod(Geo.Cyl(0.06f * s, 0.14f * s, len, 6), bark, p, tip);
                for (int c = 0; c < 4; c++)
                {
                    var o = V(R() - 0.5f, R() * 0.6f - 0.1f, R() - 0.5f) * 1.6f * s; float r = (0.75f + R() * 0.55f) * s;
                    var blob = Deco(Geo.Sphere(r, 9, 6), leaf[(b + c) % 3], tip.x + o.x, tip.y + o.y + 0.2f, tip.z + o.z); blob.transform.localScale = V(1.15f, 0.78f, 1.15f);
                }
                // glowing blossom strands hanging under the cloud
                for (int k = 0; k < 2; k++)
                {
                    var hp = tip + V(R() - 0.5f, 0, R() - 0.5f) * 1.6f * s; float hl = 0.8f + R() * 0.9f;
                    Deco(Geo.Cyl(0.015f, 0.015f, hl, 3), bark, hp.x, hp.y - hl / 2 - 0.2f, hp.z);
                    Mk.Glow(levelRoot, 0xfff2c0, 0.9f, 0.7f).transform.localPosition = V(hp.x, hp.y - hl - 0.25f, hp.z);
                }
            }
            Mk.Glow(levelRoot, 0xfff4d0, 7 * s, 0.18f).transform.localPosition = V(x, top + 1.2f * s, z);
            float spread = 3.2f * s;
            anims.Add((t, dt) => { if (rand < 0.1f) SpawnParticles(V(x + (rand - 0.5f) * spread * 2, top + 0.5f, z + (rand - 0.5f) * spread * 2), 1, 0.4f, rand < 0.5f ? goldSpark : moteMat, 0.08f, 4f, -0.5f); });
        }
        void CloisterDressing()
        {
            HolyMats();
            foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 }) { BlossomTree(sx * Hv(40), sz * Hv(22), 1); BlossomTree(sx * Hv(22), sz * Hv(40), 0.9f); }
            // benches in the outer walk, facing the cloister
            foreach (var s in new[] { -1, 1 }) foreach (var q in new[] { -1, 1 })
            {
                Box(s * Hv(33), 0, q * Hv(11), 1.2f, 0.55f, 3.6f, M.block); Deco(Geo.Box(0.25f, 0.9f, 3.6f), M.block, s * (Hv(33) + 0.7f), 1.0f, q * Hv(11));
                Box(q * Hv(11), 0, s * Hv(33), 3.6f, 0.55f, 1.2f, M.block); Deco(Geo.Box(3.6f, 0.9f, 0.25f), M.block, q * Hv(11), 1.0f, s * (Hv(33) + 0.7f));
            }
            // candle clusters on top of the low cloister walls
            var candles = new List<(Transform t, float ph)>();
            void Candles(float cx, float cz)
            {
                for (int k = 0; k < 3; k++)
                {
                    float h = 0.35f + k * 0.17f, ox = (k - 1) * 0.28f, oz = (k % 2) * 0.22f - 0.1f;
                    Deco(Geo.Cyl(0.07f, 0.07f, h, 8), marbleM, cx + ox, 5 + h / 2, cz + oz);
                    var f = Mk.Glow(levelRoot, 0xffe0a0, 0.55f, 0.85f); f.transform.localPosition = V(cx + ox, 5 + h + 0.12f, cz + oz); candles.Add((f.transform, k * 2.1f + cx));
                }
            }
            foreach (var s in new[] { -1, 1 }) foreach (var q in new[] { -1, 1 }) { Candles(q * Hv(12), s * Hv(26)); Candles(q * Hv(20), s * Hv(26)); Candles(s * Hv(26), q * Hv(12)); Candles(s * Hv(26), q * Hv(20)); }
            anims.Add((t, dt) => { foreach (var c in candles) c.t.localScale = Vector3.one * (0.55f + 0.08f * Mathf.Sin(t * 12 + c.ph) + 0.05f * Mathf.Sin(t * 29 + c.ph)); });
            // lanterns hanging from the inner edge of the roof walk
            foreach (var s in new[] { -1, 1 }) foreach (var q in new[] { -20, -12, -4, 4, 12, 20 })
                foreach (var alongX in new[] { true, false })
                {
                    float x = alongX ? Hv(q) : s * Hv(22.8f), z = alongX ? s * Hv(22.8f) : Hv(q);
                    var l = Mk.Piv(levelRoot, x, 12, z); var lt = l.t;
                    Mk.Msh(lt, Geo.Cyl(0.03f, 0.03f, 2.4f, 4), M.metal, 0, -1.2f, 0);
                    Mk.Msh(lt, Geo.Box(0.5f, 0.7f, 0.5f), flameM, 0, -2.75f, 0);
                    Mk.Msh(lt, Geo.Cone(0.42f, 0.35f, 4), goldM, 0, -2.25f, 0, 0, Mathf.PI / 4, 0);
                    Mk.Msh(lt, Geo.Box(0.6f, 0.08f, 0.6f), goldM, 0, -3.12f, 0);
                    Mk.Glow(lt, 0xfff0c8, 2.6f, 0.5f).transform.localPosition = V(0, -2.75f, 0);
                    float ph = x * 0.3f + z * 0.17f;
                    anims.Add((t, dt) => l.SetRot(Mathf.Sin(t * 0.8f + ph) * 0.05f, 0, Mathf.Cos(t * 0.7f + ph) * 0.05f));
                }
            // a gold mosaic star in the courtyard
            Deco(Geo.Torus(Hv(8), 0.12f, 64, 4), M.trim, 0, 0, 0, Mathf.PI / 2); Deco(Geo.Torus(Hv(18.5f), 0.12f, 96, 4), M.trim, 0, 0, 0, Mathf.PI / 2);
            for (int k = 0; k < 16; k++) { float a = k * Mathf.PI / 8, len = Hv(k % 2 == 0 ? 10 : 6), r0 = Hv(8); Deco(Geo.Box(0.2f, 0.06f, len), M.trim, Mathf.Sin(a) * (r0 + len / 2), 0, Mathf.Cos(a) * (r0 + len / 2), 0, a, 0); }
            WallTapestries(Hv(58), 16, 12);
        }

        // ---------- hell set dressing (levels 1-4) ----------
        Material ironM, obsM, runeM, emberGlowM;
        void HellMats()
        {
            ironM = Mats.Std(0x2c2628, 0.5f, 0.7f); obsM = Mats.Std(0x140c0c, 0.25f, 0.55f);
            runeM = Mats.Add(0xff3414, 0.75f, null, true, true); emberGlowM = Mats.Add(0xff6a20, 0.9f, null, true, true);
        }
        // an iron brazier full of fire
        void HellBrazier(float x, float z, float y0 = 0)
        {
            Deco(Geo.Cyl(0.16f, 0.3f, 1.5f, 6), ironM, x, y0 + 0.75f, z);
            Deco(Geo.Cyl(0.85f, 0.4f, 0.55f, 8), ironM, x, y0 + 1.75f, z);
            for (int k = 0; k < 4; k++) { float a = k * Mathf.PI / 2 + 0.4f; Deco(Geo.Cone(0.1f, 0.5f, 4), ironM, x + Mathf.Cos(a) * 0.82f, y0 + 2.2f, z + Mathf.Sin(a) * 0.82f); }
            var f1 = Mk.Glow(levelRoot, 0xff7020, 2.8f, 0.85f); f1.transform.localPosition = V(x, y0 + 2.5f, z);
            var f2 = Mk.Glow(levelRoot, 0xffd080, 1.3f, 0.9f); f2.transform.localPosition = V(x, y0 + 2.3f, z);
            colliders.Add(new AABB(V(x - 0.85f, y0, z - 0.85f), V(x + 0.85f, y0 + 2.05f, z + 0.85f)));
            float ph = x * 0.31f + z * 0.17f;
            anims.Add((t, dt) =>
            {
                f1.transform.localScale = Vector3.one * (2.8f + 0.5f * Mathf.Sin(t * 11 + ph) + 0.3f * Mathf.Sin(t * 27 + ph));
                f2.transform.localScale = Vector3.one * (1.3f + 0.2f * Mathf.Sin(t * 15 + ph));
                if (rand < 0.2f) SpawnParticles(V(x + (rand - 0.5f) * 0.8f, y0 + 2.4f, z + (rand - 0.5f) * 0.8f), 1, 1.5f, emberMat, 0.07f, 1.4f, -3);
            });
        }
        // a heap of skulls and bones
        void SkullPile(float x, float z, int n, float y0 = 0)
        {
            var hole = Mats.Basic(0x0a0404);
            for (int i = 0; i < n; i++)
            {
                float a = i * 2.4f, r = 0.35f * Mathf.Sqrt(i), y = y0 + 0.28f + (i < n / 3 ? 0.4f : 0) + (i < n / 8 ? 0.4f : 0);
                float sx = x + Mathf.Cos(a) * r, sz = z + Mathf.Sin(a) * r, ry = a * 1.7f;
                Deco(Geo.Sphere(0.3f, 10, 8), M.bone, sx, y, sz);
                Deco(Geo.Box(0.32f, 0.16f, 0.24f), M.bone, sx + Mathf.Sin(ry) * 0.12f, y - 0.2f, sz + Mathf.Cos(ry) * 0.12f, 0, ry, 0);
                foreach (var e in new[] { -0.1f, 0.1f }) Deco(Geo.Sphere(0.075f, 6, 5), hole, sx + Mathf.Sin(ry) * 0.25f + Mathf.Cos(ry) * e, y + 0.03f, sz + Mathf.Cos(ry) * 0.25f - Mathf.Sin(ry) * e);
                if (i % 2 == 0) Deco(Geo.Cyl(0.06f, 0.06f, 1.1f, 6), M.bone, sx + 0.3f, y0 + 0.1f, sz - 0.2f, Mathf.PI / 2, a, 0);
            }
            colliders.Add(new AABB(V(x - 0.5f, y0, z - 0.5f), V(x + 0.5f, y0 + 0.9f, z + 0.5f)));
        }
        // a horned demon idol on a plinth, wings spread, eyes burning; solid
        void DemonStatue(float x, float z, float ry, float y0 = 0)
        {
            Box(x, y0, z, 4.4f, 2.2f, 4.4f, M.tower); TrimRect(x, z, 4.4f, 4.4f, y0 + 2.2f);
            var p = Mk.Piv(levelRoot, x, y0 + 2.2f, z); p.SetRot(0, ry, 0); var t = p.t;
            Mk.Msh(t, Geo.Cyl(0.7f, 1.1f, 2.4f, 8), obsM, 0, 1.2f, 0);                              // legs / robe
            Mk.Msh(t, Geo.Cyl(1.05f, 0.75f, 1.8f, 8), obsM, 0, 3.2f, 0);                            // chest
            Mk.Msh(t, Geo.Sphere(0.55f, 12, 8), obsM, 0, 4.5f, 0.1f);
            foreach (var sx in new[] { -1f, 1f })
            {
                Mk.Msh(t, Geo.Cone(0.16f, 1.1f, 6), M.bone, sx * 0.4f, 5.1f, 0, -0.3f, 0, -sx * 0.6f);   // horns
                Mk.Msh(t, Geo.Box(0.18f, 2.4f, 0.4f), obsM, sx * 1.15f, 3.0f, 0.3f, -0.5f, 0, sx * 0.25f); // arms on a blade
                Mk.Msh(t, Geo.Box(0.14f, 3.4f, 2.2f), obsM, sx * 1.6f, 4.0f, -0.8f, 0.2f, sx * 0.6f, -sx * 0.7f); // bat wings
                Mk.Msh(t, Geo.Box(0.12f, 0.12f, 2.6f), M.bone, sx * 2.0f, 5.3f, -1.1f, 0.2f, sx * 0.6f, -sx * 0.7f);
                var eg = Mk.Glow(t, 0xff3010, 0.7f, 0.9f); eg.transform.localPosition = V(sx * 0.18f, 4.55f, 0.55f);
            }
            Mk.Msh(t, Geo.Box(0.25f, 3.6f, 0.12f), ironM, 0, 2.4f, 1.1f, 0.15f, 0, 0);                   // a great sword planted before it
            Mk.Msh(t, Geo.Box(1.2f, 0.18f, 0.2f), ironM, 0, 3.6f, 1.25f);
            var fg = Mk.Glow(t, 0xff4010, 3.5f, 0.35f); fg.transform.localPosition = V(0, 0.4f, 1.2f);
            SolidAABB(V(x - 1.3f, y0 + 2.2f, z - 1.3f), V(x + 1.3f, y0 + 7.4f, z + 1.3f));
        }
        // an iron cage on a chain from the dark, a skeleton slumped inside, swaying
        void HangingCage(float x, float z, float y)
        {
            var p = Mk.Piv(levelRoot, x, y + 3, z); var t = p.t;
            Deco(Geo.Cyl(0.06f, 0.06f, 70 - y, 4), ironM, x, y + 3 + (70 - y) / 2, z);
            Mk.Msh(t, Geo.Cyl(0.05f, 0.05f, 1.2f, 4), ironM, 0, -0.6f, 0);
            foreach (var cy in new[] { -1.2f, -3.6f }) Mk.Msh(t, Geo.Torus(0.9f, 0.06f, 16, 4), ironM, 0, cy, 0, Mathf.PI / 2);
            Mk.Msh(t, Geo.Cone(1.0f, 0.6f, 10), ironM, 0, -0.9f, 0);
            for (int k = 0; k < 10; k++) { float a = k * Mathf.PI / 5; Mk.Msh(t, Geo.Cyl(0.04f, 0.04f, 2.4f, 4), ironM, Mathf.Cos(a) * 0.9f, -2.4f, Mathf.Sin(a) * 0.9f); }
            Mk.Msh(t, Geo.Cyl(0.9f, 0.9f, 0.08f, 10), ironM, 0, -3.6f, 0);
            Mk.Msh(t, Geo.Sphere(0.24f, 10, 8), M.bone, 0.15f, -2.7f, 0.1f);                              // skeleton
            Mk.Msh(t, Geo.Cyl(0.18f, 0.12f, 0.7f, 8, true), M.bone, 0.05f, -3.2f, 0, 0, 0, 0.3f);
            for (int k = 0; k < 3; k++) Mk.Msh(t, Geo.Torus(0.17f, 0.025f, 12, 4, Mathf.PI), M.bone, 0.05f, -3.0f - k * 0.12f, 0.02f, Mathf.PI / 2);
            float ph = x * 0.21f + z * 0.13f;
            anims.Add((tt, dt) => p.SetRot(Mathf.Sin(tt * 0.7f + ph) * 0.06f, tt * 0.1f + ph, Mathf.Cos(tt * 0.6f + ph) * 0.06f));
        }
        // a glowing summoning circle on the floor (a ring and a five-pointed star), slowly breathing
        void FloorRune(float x, float z, float r, float y0 = 0)
        {
            var m = Mats.Add(0xff3414, 0.6f, null, true, true);
            Deco(Geo.Torus(r, 0.12f, 96, 4), m, x, y0 + 0.02f, z, Mathf.PI / 2);
            Deco(Geo.Torus(r * 0.82f, 0.08f, 80, 4), m, x, y0 + 0.02f, z, Mathf.PI / 2);
            for (int k = 0; k < 5; k++)
            {
                float a0 = k * Mathf.PI * 2 / 5 + Mathf.PI / 2, a1 = a0 + Mathf.PI * 4 / 5;
                var p0 = V(Mathf.Cos(a0) * r * 0.82f, 0, Mathf.Sin(a0) * r * 0.82f); var p1 = V(Mathf.Cos(a1) * r * 0.82f, 0, Mathf.Sin(a1) * r * 0.82f);
                var mid = (p0 + p1) / 2; var d = p1 - p0;
                Deco(Geo.Box(0.16f, 0.04f, d.magnitude), m, x + mid.x, y0 + 0.02f, z + mid.z, 0, Mathf.Atan2(d.x, d.z), 0);
            }
            float ph = x + z;
            anims.Add((t, dt) => Mats.SetAlpha(m, 0.4f + 0.25f * Mathf.Sin(t * 1.4f + ph)));
        }
        // glowing cracks in the floor, breathing out embers
        void LavaCrack(float x, float z, float len, float ang, float y0 = 0)
        {
            float px = x, pz = z, a = ang;
            for (int i = 0; i < 5; i++)
            {
                float l = len / 5 * (0.8f + rand * 0.4f); var d = V(Mathf.Sin(a), 0, Mathf.Cos(a));
                Deco(Geo.Box(0.28f + rand * 0.15f, 0.03f, l), emberGlowM, px + d.x * l / 2, y0 + 0.015f, pz + d.z * l / 2, 0, a, 0);
                px += d.x * l; pz += d.z * l; a += (rand - 0.5f) * 0.9f;
            }
            var g = Mk.Glow(levelRoot, 0xff5010, len * 0.9f, 0.25f); g.transform.localPosition = V((x + px) / 2, y0 + 0.4f, (z + pz) / 2);
            float ex = x, ez = z, fx = px, fz = pz;
            anims.Add((t, dt) => { if (rand < 0.12f) { float u = rand; SpawnParticles(V(Mathf.Lerp(ex, fx, u), y0 + 0.1f, Mathf.Lerp(ez, fz, u)), 1, 2, emberMat, 0.07f, 1.0f, -3); } });
        }
        // a bone shaped like an arch (Geo.Arc in the XY plane, turned by ry): knobbly joint ends, gentle swellings along
        // the shaft and a ridge running down it, so it reads as bone rather than a pipe; solid
        void BoneArc(Vector3 c, float R, float tube, float ry, int solidSegs)
        {
            var q = Quaternion.Euler(0, ry * Mathf.Rad2Deg, 0);
            Vector3 At(float a, float rr = 0) => c + q * V(Mathf.Cos(a) * (R + rr), Mathf.Sin(a) * (R + rr), 0);
            Deco(Geo.Arc(R, tube, Mathf.Max(16, (int)(R * 1.2f))), M.bone, c.x, c.y, c.z, 0, ry, 0);
            Deco(Geo.Arc(R + tube * 0.55f, tube * 0.35f, Mathf.Max(16, (int)(R * 1.2f))), M.bone, c.x, c.y, c.z, 0, ry, 0); // ridge on the outer curve
            int n = Mathf.Clamp((int)(R / 3), 4, 14);
            for (int i = 1; i < n; i++) { var p = At(i * Mathf.PI / n); var g = Deco(Geo.Sphere(tube * 1.22f, 10, 8), M.bone, p.x, p.y, p.z); g.transform.localScale = V(1, 1, 1.1f); }
            foreach (var a in new[] { 0.06f, Mathf.PI - 0.06f })
            {
                var p = At(a); Deco(Geo.Sphere(tube * 1.7f, 12, 9), M.bone, p.x, p.y, p.z);
                var p2 = At(a, tube * 0.9f); Deco(Geo.Sphere(tube * 1.3f, 10, 8), M.bone, p2.x, p2.y + tube * 0.4f, p2.z);
            }
            SolidArc(c, R, tube, ry, solidSegs);
        }
        // a spine of vertebrae along z at height y (discs, a spike on top, wings to the sides)
        void Vertebrae(float x, float y, float z0, float z1, float s)
        {
            var dark = Mats.Std(0x5a4632, 0.8f);
            for (float z = z0; z <= z1; z += 3.6f * s)
            {
                Deco(Geo.Cyl(2.0f * s, 2.2f * s, 2.6f * s, 12), M.bone, x, y, z, Mathf.PI / 2, 0, 0);
                Deco(Geo.Cyl(1.7f * s, 1.7f * s, 0.9f * s, 12), dark, x, y, z + 1.75f * s, Mathf.PI / 2, 0, 0);
                Deco(Geo.Cone(0.75f * s, 3.2f * s, 6), M.bone, x, y + 2.8f * s, z - 0.4f * s, -0.35f, 0, 0);
                foreach (var sx in new[] { -1f, 1f }) Deco(Geo.Cone(0.55f * s, 2.6f * s, 6), M.bone, x + sx * 2.6f * s, y - 0.3f * s, z, 0, 0, -sx * 1.35f);
            }
        }
        // giant ribs curving out of the floor, as if the arena sits inside some dead colossus (solid)
        void BoneRibs(float x, float z, float r, float ry)
        {
            BoneArc(V(x, 0, z), r, 0.55f, ry, 10);
            BoneArc(V(x, 0, z), r * 0.78f, 0.4f, ry, 8);
        }
        void PitDressing()
        {
            HellMats();
            FloorRune(0, 0, 14);
            foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 })
            {
                HellBrazier(sx * 15, sz * 15);
                HangingCage(sx * 20, sz * 35, 15);
                SkullPile(sx * 44, sz * 30, 14);
                LavaCrack(sx * 8, sz * 34, 10, sx * 1.2f);
            }
            DemonStatue(60, 0, -Mathf.PI / 2); DemonStatue(-60, 0, Mathf.PI / 2); DemonStatue(0, 60, Mathf.PI); DemonStatue(0, -60, 0);
            foreach (var sx in new[] { -1, 1 }) { SkullPile(sx * 62, sx * 40, 10); LavaCrack(sx * 58, -20, 12, 0.3f); }
        }
        void BoneCorridorsDressing()
        {
            HellMats();
            FloorRune(0, 0, 9.5f);
            foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 })
            {
                HellBrazier(sx * 9.5f, sz * 9.5f);
                HangingCage(sx * 30, sz * 30, 14);
                DemonStatue(sx * 50, sz * 50, Mathf.Atan2(-sx, -sz));
                SkullPile(sx * 33, sz * 52, 12);
            }
            foreach (var sx in new[] { -1, 1 }) { BoneRibs(sx * 28, 0, 7, Mathf.PI / 2); BoneRibs(sx * 50, 0, 6, Mathf.PI / 2); LavaCrack(sx * 30, -14, 10, 0.4f * sx); LavaCrack(sx * 30, 18, 9, 2.5f * sx); }
        }
        void FurnaceDressing()
        {
            HellMats();
            FloorRune(0, 0, 10);
            // furnace mouths glowing in the back wall above the terraces
            foreach (var fx in new[] { -34f, 0f, 34f })
            {
                Deco(Geo.Box(9, 7, 0.6f), ironM, fx, 10, -63.6f);
                Deco(Geo.Box(7.6f, 5.6f, 0.2f), emberGlowM, fx, 10, -63.25f);
                for (int k = 0; k < 6; k++) Deco(Geo.Box(0.25f, 5.6f, 0.3f), ironM, fx - 3.2f + k * 1.28f, 10, -63.1f);
                var g = Mk.Glow(levelRoot, 0xff5010, 12, 0.45f); g.transform.localPosition = V(fx, 10, -62);
                float ph = fx;
                anims.Add((t, dt) => { g.transform.localScale = Vector3.one * (12 + 1.5f * Mathf.Sin(t * 2.3f + ph)); if (rand < 0.3f) SpawnParticles(V(fx + (rand - 0.5f) * 7, 8, -62.5f), 1, 2, emberMat, 0.08f, 1.6f, -2); });
            }
            // molten metal pouring out of each furnace mouth, down over the terraces
            var lavaM = Mats.Basic(0xffffff, Tex.Lava, true); lavaM.SetTextureScale("_BaseMap", new Vector2(0.6f, 3));
            anims.Add((t, dt) => lavaM.SetTextureOffset("_BaseMap", new Vector2(0, t * 0.5f)));
            foreach (var fx in new[] { -34f, 0f, 34f })
            {
                var fall = Deco(Geo.Plane(3.2f, 7.2f), lavaM, fx, 9.9f, -62.9f); fall.layer = Mk.LayerIgnore;
                Deco(Geo.Box(4, 0.5f, 2.2f), ironM, fx, 13.1f, -62.4f);                                  // spout lip
                var pool = Deco(Geo.Plane(4.5f, 3.5f), lavaM, fx, 6.06f, -61.5f, -Mathf.PI / 2); pool.layer = Mk.LayerIgnore;
                var sp = Mk.Glow(levelRoot, 0xff7020, 5, 0.6f); sp.transform.localPosition = V(fx, 6.6f, -61);
            }
            // heavy pipes along the back wall with glowing joints, dropping down into the terraces
            Deco(Geo.Cyl(0.9f, 0.9f, 126, 10), ironM, 0, 18, -63, 0, 0, Mathf.PI / 2);
            for (int k = -3; k <= 3; k++)
            {
                if (k == 0 || k == 2 || k == -2) continue; float px = k * 17; // clear of the furnace mouths
                Deco(Geo.Cyl(0.6f, 0.6f, 12, 8), ironM, px, 12, -63.2f);
                Deco(Geo.Torus(0.95f, 0.18f, 16, 4), emberGlowM, px, 18, -63, 0, Mathf.PI / 2, 0);
                Deco(Geo.Torus(0.68f, 0.14f, 14, 4), ironM, px, 8, -63.2f, Mathf.PI / 2);
            }
            // great cogwheels turning slowly on both side walls
            foreach (var sx in new[] { -1, 1 }) foreach (var (gz, gy, gr) in new[] { (-30f, 14f, 6f), (-16f, 21f, 4f), (14f, 13f, 5f) })
            {
                var g = Mk.Piv(levelRoot, sx * 63.3f, gy, gz); g.SetRot(0, Mathf.PI / 2, 0); var gt = g.t;
                Mk.Msh(gt, Geo.Torus(gr, gr * 0.12f, 36, 6), ironM);
                Mk.Msh(gt, Geo.Cyl(gr * 0.25f, gr * 0.25f, 0.8f, 12), obsM, 0, 0, 0, Mathf.PI / 2);
                Mk.Msh(gt, Geo.Cyl(gr * 0.12f, gr * 0.12f, 0.9f, 10), emberGlowM, 0, 0, 0, Mathf.PI / 2);
                int teeth = (int)(gr * 3);
                for (int k = 0; k < teeth; k++) { float a = k * Mathf.PI * 2 / teeth; Mk.Msh(gt, Geo.Box(gr * 0.22f, gr * 0.22f, 0.5f), ironM, Mathf.Cos(a) * gr * 1.1f, Mathf.Sin(a) * gr * 1.1f, 0, 0, 0, a); }
                for (int k = 0; k < 4; k++) { float a = k * Mathf.PI / 2; Mk.Msh(gt, Geo.Box(gr * 1.8f, gr * 0.12f, 0.4f), ironM, 0, 0, 0, 0, 0, a); }
                float spd = (gr == 4f ? -1 : 1) * 0.25f / gr * 6;
                anims.Add((t, dt) => g.rz = t * spd);
            }
            // forges on the front terrace: anvils with a glowing ingot, and bubbling cauldrons on the lower terrace
            foreach (var sx in new[] { -1, 1 })
            {
                float ax = sx * 30, az = 50;
                Deco(Geo.Cyl(0.7f, 1.0f, 1.0f, 8), obsM, ax, 2.5f, az); Deco(Geo.Box(2.4f, 0.6f, 0.9f), ironM, ax, 3.3f, az);
                Deco(Geo.Cone(0.45f, 1.0f, 4), ironM, ax + 1.6f, 3.3f, az, 0, 0, -Mathf.PI / 2);
                Deco(Geo.Box(0.9f, 0.18f, 0.35f), emberGlowM, ax - 0.3f, 3.7f, az);
                Mk.Glow(levelRoot, 0xff6020, 2.5f, 0.6f).transform.localPosition = V(ax - 0.3f, 3.9f, az);
                colliders.Add(new AABB(V(ax - 1.4f, 2, az - 0.6f), V(ax + 2f, 3.6f, az + 0.6f)));
                float cx = sx * 52, cz = -46;
                Deco(Geo.Cyl(2.0f, 1.4f, 2.2f, 12, true), ironM, cx, 4.1f, cz);
                Deco(Geo.Torus(2.0f, 0.2f, 24, 6), ironM, cx, 5.2f, cz, Mathf.PI / 2);
                var melt = Deco(Geo.Circle(1.9f, 20), lavaM, cx, 5.0f, cz, -Mathf.PI / 2); melt.layer = Mk.LayerIgnore;
                foreach (var lx in new[] { -1.2f, 1.2f }) Deco(Geo.Cyl(0.2f, 0.25f, 1.2f, 6), ironM, cx + lx, 3.4f, cz);
                Mk.Glow(levelRoot, 0xff6a20, 6, 0.5f).transform.localPosition = V(cx, 6, cz);
                SolidAABB(V(cx - 2, 3, cz - 2), V(cx + 2, 5.2f, cz + 2));
                float ccx = cx, ccz = cz;
                anims.Add((t, dt) => { if (rand < 0.15f) SpawnParticles(V(ccx + (rand - 0.5f) * 2.5f, 5.2f, ccz + (rand - 0.5f) * 2.5f), 1, 2, emberMat, 0.1f, 0.8f, -6); });
            }
            // glowing floor grates between the pools
            foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 })
            {
                float gx = sx * 24, gz = sz * 24;
                Deco(Geo.Box(5, 0.04f, 5), emberGlowM, gx, 0.02f, gz);
                for (int k = 0; k < 6; k++) Deco(Geo.Box(0.25f, 0.1f, 5.2f), ironM, gx - 2.2f + k * 0.88f, 0.05f, gz);
                Deco(Geo.Box(5.4f, 0.12f, 0.3f), ironM, gx, 0.06f, gz - 2.6f); Deco(Geo.Box(5.4f, 0.12f, 0.3f), ironM, gx, 0.06f, gz + 2.6f);
                Mk.Glow(levelRoot, 0xff5010, 6, 0.3f).transform.localPosition = V(gx, 0.6f, gz);
            }
            foreach (var sx in new[] { -1, 1 })
            {
                DemonStatue(sx * 52, -58, 0, 6);
                HellBrazier(sx * 20, -43, 3); HellBrazier(sx * 44, -43, 3); HellBrazier(sx * 20, -55, 6);
                SkullPile(sx * 42, 52, 12, 2); SkullPile(sx * 18, 56, 9, 2);
                HangingCage(sx * 50, 25, 14); HangingCage(sx * 16, 26, 16);
                LavaCrack(sx * 50, -26, 11, 0.5f * sx); LavaCrack(sx * 54, 28, 9, 2.6f * sx);
            }
        }
        void ChainTowersDressing()
        {
            HellMats();
            FloorRune(0, 0, 8);
            for (int k = 0; k < 4; k++)
            {
                float a = k * Mathf.PI / 2 + Mathf.PI / 8, b = a + Mathf.PI / 4;
                HellBrazier(Mathf.Cos(a) * 18, Mathf.Sin(a) * 18);
                HangingCage(Mathf.Cos(b) * 30, Mathf.Sin(b) * 30, 17);
            }
            DemonStatue(52, 0, -Mathf.PI / 2); DemonStatue(-52, 0, Mathf.PI / 2); DemonStatue(0, 52, Mathf.PI); DemonStatue(0, -52, 0);
            foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 })
            {
                SkullPile(sx * 54, sz * 22, 11); SkullPile(sx * 22, sz * 54, 11);
                LavaCrack(sx * 40, sz * 14, 9, sx * sz * 0.8f);
            }
        }

        // light pouring in over the walls from the sun's side (levels without windows)
        void SunRaysOverWalls(float Bd, float wallH, int n)
        {
            var sun = V(-90, 130, -210); var down = (V(0, 0, 0) - sun).normalized;
            for (int i = 0; i < n; i++)
            {
                float x = Mathf.Lerp(-Bd * 0.75f, Bd * 0.75f, (i + 0.5f) / n) + Mathf.Sin(i * 3.7f) * 6;
                var src = V(x, wallH + 6, -Bd - 1); var dst = src + down * ((wallH + 6) / -down.y);
                LightShaft(src, dst, 1.6f + (i % 3) * 0.5f, 5 + (i % 2) * 2, 0xfff2d0, 0.13f + (i % 3) * 0.02f);
            }
        }
    }
}
