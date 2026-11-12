using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    // A particle: either an instanced cube (m == null) or a real object flung around (gibs, wings, casings with transforms).
    public class Pt
    {
        public Transform m; public bool owned;
        public Vector3 pos, v, scale = Vector3.one, spin; public Quaternion rot = Quaternion.identity; public Material mat;
        public float life, grav; public bool stuck, hasSpin, bleed;
    }
    public class Fx { public Transform m; public Material mat; public float life, max, grow, r; public bool pillar; }
    public class Portal
    {
        public float t, baseY; public Transform g, ring, spikesG, disc, disc2, glow, beam, beam2; public Material beamMat;
        public bool burst, active, finale; public Vector3 pos;
    }
    public class WarpState { public bool active, swapped; public float t, fov, roll; public int next = 1; }

    public partial class Game
    {
        // ---------- shared materials & meshes (persist across levels) ----------
        Material redBloodMat, redGibMat, frostBloodMat, frostGibMat; bool frostGore; // the Frozen Circle's creatures bleed blue and break into ice
        Material bloodMat, gibMat, sparkMat, dustMat, emberMat, ashMat, moteMat, brassMat, shellMat, steamMat, cyanMat, goldSpark, spikeMat, spikeWarnMat, stainedMat;
        Mesh ringGeo, wallRingGeo, unitBox;
        GameObject beamCore, beamOuter, beamHit, teleLine, introPillar; Material teleLineMat, introPillarMat;
        readonly List<Pt> particles = new List<Pt>();
        readonly List<(Vector3 p, float s)> goreQ = new List<(Vector3, float)>(); readonly List<Vector3> dripQ = new List<Vector3>();
        readonly List<Fx> fx = new List<Fx>();
        readonly WarpState WARP = new WarpState();

        void InitShared()
        {
            unitBox = Geo.Box(1, 1, 1); ringGeo = Geo.Ring(0.85f, 1, 40); wallRingGeo = Geo.Cyl(1, 1, 1, 48, true);
            bloodMat = redBloodMat = G(Mats.Basic(0xb00000)); gibMat = redGibMat = G(Mats.Std(0x701010, 0.6f));
            frostBloodMat = G(Mats.Basic(0x3a90ff)); frostGibMat = G(Mats.Std(0x9ad8ff, 0.25f, 0.1f, 0x0a3048)); sparkMat = G(Mats.Basic(0xffdd88));
            dustMat = G(Mats.Basic(0x998877)); emberMat = G(Mats.Basic(0xff7a20)); ashMat = G(Mats.Basic(0x555050)); moteMat = G(Mats.Basic(0xfff6d0));
            brassMat = G(Mats.Std(0xd0a040, 0.3f, 0.6f)); shellMat = G(Mats.Std(0xb02020, 0.5f)); steamMat = G(Mats.Trans(0xdddddd, 0.5f));
            cyanMat = G(Mats.Basic(0x80f0ff)); goldSpark = G(Mats.Basic(0xffe080));
            eprojMat = G(Mats.Basic(0xffe0a0)); friendlyMat = G(Mats.Basic(0xd0ffff)); bileMat = G(Mats.Basic(0xff5a20)); bladeMat = G(Mats.Basic(0xc0f4ff));
            spikeMat = G(Mats.Std(0xd8ccb0, 0.5f)); stainedMat = G(Mats.Basic(0xffffff, Tex.Stained, true)); spikeWarnMat = G(Mats.Trans(0xff2000, 0.7f, true));
            beamCore = Mk.Msh(world, Geo.Cyl(1, 1, 1, 8), G(Mats.Basic(0xffffff)));
            beamOuter = Mk.Msh(world, Geo.Cyl(1, 1, 1, 10), G(Mats.Add(0x60d8ff, 0.45f, null, true, false)));
            beamHit = Mk.Glow(world, 0xa0f0ff, 5, 0.9f); G(beamHit.GetComponent<Renderer>().sharedMaterial);
            teleLineMat = G(Mats.Trans(0xff2020, 0.6f)); teleLine = Mk.Msh(world, unitBox, teleLineMat);
            introPillarMat = G(Mats.Add(0xfff0c0, 0, null, true, false)); introPillar = Mk.Msh(world, Geo.Cyl(3, 3, 90, 16), introPillarMat);
            foreach (var g in new[] { beamCore, beamOuter, beamHit, teleLine, introPillar }) { g.layer = Mk.LayerIgnore; g.SetActive(false); }
        }

        // ---------- particles & effects ----------
        void SpawnParticles(Vector3 pos, int n, float speed, Material mat, float size, float life, float grav = GRAV)
        {
            for (int i = 0; i < n; i++)
            {
                float s = size * (0.6f + rand * 0.8f);
                var v = V(rand - 0.5f, rand - 0.3f, rand - 0.5f).normalized * (speed * (0.4f + rand));
                v.y += speed * 0.3f;
                particles.Add(new Pt { pos = pos, scale = V(s, s, s), mat = mat, v = v, life = life * (0.6f + rand * 0.6f), grav = grav });
            }
            TrimParticles();
        }
        void SpawnCasing(Vector3 pos, Vector3 vel, Material mat, float sx, float sy, float sz)
        {
            particles.Add(new Pt { pos = pos, scale = V(sx, sy, sz), mat = mat, v = vel, life = 1.2f, grav = GRAV, hasSpin = true, spin = V(rand * 30 - 15, rand * 30 - 15, rand * 30 - 15) });
            TrimParticles();
        }
        void KillPt(Pt p) { if (p.m) Destroy(p.m.gameObject); }
        void TrimParticles()
        {
            int extra = particles.Count - 700;
            if (extra <= 0) return;
            for (int i = 0; i < extra; i++) KillPt(particles[i]);
            particles.RemoveRange(0, extra);
        }
        void Tracer(Vector3 a, Vector3 b, int color, float width = 0.06f, float life = 0.09f)
        {
            float len = Vector3.Distance(a, b); if (len < 0.01f) return;
            var mat = Mats.Trans(color, 1);
            var m = Mk.Msh(world, unitBox, mat); m.layer = Mk.LayerIgnore;
            m.transform.localScale = V(width, width, len); m.transform.position = (a + b) * 0.5f; m.transform.rotation = Quaternion.LookRotation(b - a);
            fx.Add(new Fx { m = m.transform, mat = mat, life = life, max = life });
        }
        void Blast(Vector3 pos, float radius, int color)
        {
            var mat = Mats.Trans(color, 1);
            var m = Mk.Msh(world, Geo.Sphere(1, 12, 8), mat, pos.x, pos.y, pos.z); m.layer = Mk.LayerIgnore;
            m.transform.localScale = Vector3.one * radius * 0.3f;
            fx.Add(new Fx { m = m.transform, mat = mat, life = 0.3f, max = 0.3f, grow = radius });
        }
        void ShockRing(Vector3 pos, float radius, int color, float life = 0.45f)
        {
            var mat = Mats.Trans(color, 1, true);
            var m = Mk.Msh(world, ringGeo, mat, pos.x, pos.y + 0.08f, pos.z, -Mathf.PI / 2); m.layer = Mk.LayerIgnore;
            fx.Add(new Fx { m = m.transform, mat = mat, life = life, max = life, grow = radius / 1.2f });
        }
        void UpdateParticles(float dt)
        {
            foreach (var p in particles)
            {
                p.life -= dt;
                if (!p.stuck)
                {
                    p.v.y -= p.grav * dt;
                    if (p.m) p.pos = p.m.position;
                    p.pos += p.v * dt;
                    if (p.hasSpin)
                    {
                        var dq = Quaternion.Euler(p.spin * (dt * Mathf.Rad2Deg));
                        if (p.m) p.m.rotation = p.m.rotation * dq; else p.rot = p.rot * dq;
                    }
                    if (PointInLevel(p.pos, out var c))
                    {
                        if (p.bleed && p.v.y < -3) goreQ.Add((V(p.pos.x, c.max.y, p.pos.z), 0.45f + rand * 0.35f)); // splat where it hits
                        if (p.hasSpin && p.v.y < -4) { p.pos.y = c.max.y + 0.02f; p.v.y *= -0.3f; p.v.x *= 0.5f; p.v.z *= 0.5f; p.spin *= 0.5f; }
                        else { p.pos.y = c.max.y + 0.02f; p.stuck = true; if (p.mat == bloodMat && p.scale.x > 0.07f && rand < 0.5f) goreQ.Add((V(p.pos.x, c.max.y, p.pos.z), p.scale.x * (3 + rand * 3))); }
                    }
                    if (p.bleed && p.m && rand < 0.3f) dripQ.Add(p.pos);
                    if (p.m) p.m.position = p.pos;
                }
                if (p.life < 0.3f)
                {
                    float k = Mathf.Max(0, 1 - dt * 8);
                    if (p.m) p.m.localScale *= k; else p.scale *= k;
                }
                if (p.life <= 0) KillPt(p);
            }
            particles.RemoveAll(p => p.life <= 0);
            foreach (var g in goreQ) Decal(g.p, g.s); goreQ.Clear();
            foreach (var d in dripQ) particles.Add(new Pt { pos = d, scale = Vector3.one * (0.05f + rand * 0.04f), mat = bloodMat, v = V(rand - 0.5f, -1, rand - 0.5f), life = 1.2f, grav = GRAV });
            dripQ.Clear(); UpdateDecals();
            foreach (var f in fx)
            {
                f.life -= dt; float t = Mathf.Max(0, f.life / f.max);
                Mats.SetAlpha(f.mat, t);
                if (f.grow > 0) f.m.localScale = Vector3.one * (f.grow * (0.3f + (1 - t) * 0.9f));
                if (f.pillar) { var s = f.m.localScale; s.x = s.z = f.r * (0.4f + (1 - t) * 0.9f); f.m.localScale = s; }
                if (f.life <= 0) Mk.DestroyTree(f.m.gameObject);
            }
            fx.RemoveAll(f => f.life <= 0);
        }
        // instanced cubes, drawn every frame (also while frozen/paused)
        readonly Dictionary<Material, List<Matrix4x4>> ptBatches = new Dictionary<Material, List<Matrix4x4>>();
        readonly Matrix4x4[] ptBuf = new Matrix4x4[1023];
        void DrawParticles()
        {
            foreach (var l in ptBatches.Values) l.Clear();
            foreach (var p in particles)
            {
                if (p.m || p.mat == null) continue;
                if (!ptBatches.TryGetValue(p.mat, out var list)) ptBatches[p.mat] = list = new List<Matrix4x4>();
                list.Add(Matrix4x4.TRS(p.pos, p.rot, p.scale));
            }
            foreach (var kv in ptBatches)
            {
                var list = kv.Value;
                for (int i = 0; i < list.Count; i += 1023)
                {
                    int n = Mathf.Min(1023, list.Count - i);
                    list.CopyTo(i, ptBuf, 0, n);
                    Graphics.DrawMeshInstanced(unitBox, 0, kv.Key, ptBuf, n, null, UnityEngine.Rendering.ShadowCastingMode.Off, false, Mk.LayerIgnore);
                }
            }
        }

        // ---------- portal (crystal tears open after a wave) ----------
        void OpenPortal(bool finale = false)
        {
            if (portal != null || !crystal) return;
            var c = LEVEL.center; var T = THEME;
            var g = new GameObject("portal").transform; g.SetParent(levelRoot, false); g.localPosition = V(c.x, c.y + 2.6f, c.z); g.localScale = Vector3.one * 0.001f;
            var ringMat = Mats.Basic(T.portal);
            var discMat = Mats.Add(T.portal, 1, Tex.Swirl, true, true);
            var disc2Mat = Mats.Add(T.portal2, 0.8f, Tex.Swirl, true, true);
            var ring = Mk.Msh(g, Geo.Torus(2.3f, 0.16f, 48), ringMat);
            var spikesG = new GameObject("sp").transform; spikesG.SetParent(g, false);
            for (int i = 0; i < 16; i++) { float a = i / 16f * Mathf.PI * 2; Mk.Msh(spikesG, Geo.Cone(0.1f, 0.6f, 4), ringMat, Mathf.Cos(a) * 2.75f, Mathf.Sin(a) * 2.75f, 0, 0, 0, a - Mathf.PI / 2); }
            var disc = Mk.Msh(g, Geo.Circle(2.2f, 48), discMat);
            var disc2 = Mk.Msh(g, Geo.Circle(1.7f, 48), disc2Mat, 0, 0, 0.02f);
            var glow = Mk.Glow(g, T.portal, 10, 0.8f);
            foreach (var m in new[] { ring, disc, disc2 }) m.layer = Mk.LayerIgnore;
            var beamMat = Mats.Add(T.portal2, 0.6f, null, true, true);
            var beamMat2 = Mats.Add(T.portal, 0.3f, null, true, true);
            var beam = Mk.Msh(levelRoot, Geo.Cyl(1, 1, 1, 16), beamMat, c.x, c.y, c.z); var beam2 = Mk.Msh(levelRoot, Geo.Cyl(1, 1, 1, 16), beamMat2, c.x, c.y, c.z);
            beam.layer = beam2.layer = Mk.LayerIgnore;
            beam.transform.localScale = V(1.1f, 0.001f, 1.1f); beam2.transform.localScale = V(2.6f, 0.001f, 2.6f);
            portal = new Portal { g = g, ring = ring.transform, spikesG = spikesG, disc = disc.transform, disc2 = disc2.transform, glow = glow.transform, beam = beam.transform, beam2 = beam2.transform, beamMat = beamMat, finale = finale, baseY = crystal.localPosition.y, pos = g.position };
            Sfx("portalCharge");
            SetHint(null);
        }
        float discRot, disc2Rot, spikesRot, ringRot;
        void UpdatePortal(float dt)
        {
            var p = portal; if (p == null) return;
            p.t += dt;
            if (!p.burst)
            {
                float k = clamp(p.t / 1.4f, 0, 1);
                crystalN.ry += dt * (1 + k * k * 30); crystalN.rx += dt * k * 6;
                var cp = crystal.localPosition; cp.y = p.baseY + k * 1.5f + Mathf.Sin(p.t * 40) * 0.05f * k; crystal.localPosition = cp;
                crystal.localScale = Vector3.one * (1 + k * 0.4f);
                crystalGlow.localScale = Vector3.one * (10 + k * 18); Mats.SetAlpha(crystalGlow.GetComponent<Renderer>().sharedMaterial, 0.55f + k * 0.45f);
                crystalRingN.rz += dt * (1 + k * 20); crystalRing.localScale = Vector3.one * (1 - k * 0.5f);
                for (int i = 0; i < 3; i++) // light spirals into the crystal
                {
                    float a = rand * Mathf.PI * 2, rr = 7 + rand * 4; var src = crystal.position + V(Mathf.Cos(a) * rr, rand * 4 - 2, Mathf.Sin(a) * rr);
                    particles.Add(new Pt { pos = src, scale = Vector3.one * 0.12f, mat = THEME.holy ? moteMat : emberMat, v = (crystal.position - src) * 2.2f, life = 0.45f, grav = 0 });
                }
                TrimParticles();
                P.shake = Mathf.Max(P.shake, 0.12f * k);
                if (p.t >= 1.4f)
                {
                    p.burst = true;
                    SpawnParticles(crystal.position, 30, 14, THEME.holy ? goldSpark : emberMat, 0.25f, 1.2f);
                    crystal.gameObject.SetActive(false); crystalRing.gameObject.SetActive(false); crystalGlow.gameObject.SetActive(false);
                    ShockRing(LEVEL.center, 14, THEME.portal, 0.8f);
                    Blast(p.pos, 6, THEME.portal2); whiteFlash = Mathf.Max(whiteFlash, 0.45f); P.shake = Mathf.Max(P.shake, 0.7f);
                    Sfx("portalOpen");
                    if (!p.finale) { Banner("PORTAL OPEN", "ENTER THE LIGHT", THEME.banner); SetHint("◆ STEP INTO THE PORTAL ◆"); }
                }
                return;
            }
            {
                float k = clamp((p.t - 1.4f) / 0.6f, 0, 1), ob = 1 + 2.70158f * Mathf.Pow(k - 1, 3) + 1.70158f * Mathf.Pow(k - 1, 2);
                p.g.localScale = Vector3.one * Mathf.Max(0.001f, ob);
                float bh = clamp((p.t - 1.4f) / 0.35f, 0, 1) * 220;
                var s1 = p.beam.localScale; s1.y = Mathf.Max(0.001f, bh); p.beam.localScale = s1;
                float w2 = 2.6f + Mathf.Sin(p.t * 6) * 0.3f; p.beam2.localScale = V(w2, Mathf.Max(0.001f, bh), w2);
                var bp = p.beam.localPosition; bp.y = LEVEL.center.y + bh / 2; p.beam.localPosition = bp; p.beam2.localPosition = bp;
                Mats.SetAlpha(p.beamMat, 0.45f + Mathf.Sin(p.t * 25) * 0.12f);
                p.g.localRotation = Quaternion.Euler(0, Mathf.Atan2(P.pos.x - p.pos.x, P.pos.z - p.pos.z) * Mathf.Rad2Deg, 0);
                discRot -= dt * 3; disc2Rot += dt * 5; spikesRot += dt * 0.8f; ringRot -= dt * 0.4f;
                p.disc.localRotation = Quaternion.Euler(0, 0, discRot * Mathf.Rad2Deg); p.disc2.localRotation = Quaternion.Euler(0, 0, disc2Rot * Mathf.Rad2Deg);
                p.spikesG.localRotation = Quaternion.Euler(0, 0, spikesRot * Mathf.Rad2Deg); p.ring.localRotation = Quaternion.Euler(0, 0, ringRot * Mathf.Rad2Deg);
                p.glow.localScale = Vector3.one * (9 + Mathf.Sin(p.t * 5) * 1.5f);
                if (rand < 0.8f) // motes rising up the beam
                {
                    float a = rand * Mathf.PI * 2; var src = V(LEVEL.center.x + Mathf.Cos(a) * 1.5f, LEVEL.center.y + rand * 3, LEVEL.center.z + Mathf.Sin(a) * 1.5f);
                    particles.Add(new Pt { pos = src, scale = Vector3.one * 0.14f, mat = THEME.holy ? moteMat : emberMat, v = V(0, 12 + rand * 10, 0), life = 1.5f, grav = 0 });
                }
                if (p.finale) return;
                if (p.t > 2.0f) p.active = true;
                if (!p.active || P.dead) return;
                if (MP && (P.downed || P.inPortal)) { if (P.inPortal) P.vel = Vector3.zero; return; } // co-op: held in the light where you touched it, until everyone is in
                var to = p.pos - ChestPos; float dist = to.magnitude;
                if (dist < 14) P.vel += to.normalized * ((14 - dist) * dt * 2.2f);          // gentle suction near the portal
                if (p.t > 24) P.vel += (p.pos - ChestPos).normalized * (40 * dt);           // the beam claims you eventually
                if (dist < 2.7f) { if (MP) { P.inPortal = true; Sfx("portalCharge"); SetHint("◆ WAITING FOR YOUR TEAM ◆"); } else StartWarp(levelNum + 1); }
            }
        }

        // ---------- warp transition ----------
        void StartWarp(int next)
        {
            if (WARP.active) return;
            var T = THEMES[LEVELS[next - 1].theme];
            WARP.active = true; WARP.t = 0; WARP.swapped = false; WARP.next = next;
            SetWarpColors(T.warp[0], T.warp[1]);
            if (DE.active) EndDeadeye();
            ReleaseHook(); mouseDown = false; P.coreCharging = false; P.vel = Vector3.zero;
            SetHint(null);
            Sfx("warp");
        }
        // the host moves everyone on: solo this is just StartWarp
        void NetWarp(int next) { NetEvent(NetEv.Warp, next); StartWarp(next); }
        void UpdateWarp(float rdt)
        {
            WARP.t += rdt;
            const float T1 = 1.0f, T2 = 2.1f;
            if (WARP.t < T1)
            {
                float k = WARP.t / T1;
                WARP.fov = k * k * 75; WARP.roll = k * k * 1.4f;
                warpGroup.alpha = Mathf.Min(1, k * 1.25f);
                if (portal != null) P.pos = Vector3.Lerp(P.pos, portal.pos - V(0, 1, 0), 1 - Mathf.Exp(-4 * rdt));
                return;
            }
            if (!WARP.swapped) { WARP.swapped = true; ArriveLevel(WARP.next); }
            {
                float k = clamp((WARP.t - T1) / (T2 - T1), 0, 1);
                WARP.fov = (1 - k) * (1 - k) * 75; WARP.roll = -(1 - k) * (1 - k) * 1.4f;
                warpGroup.alpha = 1 - k;
                if (k >= 1) { WARP.active = false; WARP.fov = 0; WARP.roll = 0; warpGroup.alpha = 0; }
            }
        }
        // level music: one track for all hell levels, one for all heaven levels (the boss levels keep their own boss
        // tracks, so the level track bows out there); the fuller arrangement comes in while a wave is being fought
        void LevelMusic()
        {
            if (P.dead || won || CINE.active) return;
            int want = LEVEL.boss != 0 ? -1 : Act == "ruin" ? 7 : Act == "frost" ? 9 : THEME.deco == "limbo" ? (LEVEL.name == "CLOCKWORK HALLS" ? 13 : LEVEL.name == "THE LIBRARY OF SOULS" ? 14 : LEVEL.name == "MIRROR WASTES" ? 15 : 12) : THEME.holy ? 5 : 4, cur = audioMgr.Current; // purgatory takes the calmer heaven track; Dead Earth and the Frozen Circle have their own
            if (want < 0) { if (cur == 4 || cur == 5 || cur == 7 || cur == 9 || cur >= 12) audioMgr.Stop(1.5f); return; }
            if (cur != want) { audioMgr.Start(want); if (testMode && testOut != null) System.IO.File.AppendAllText(System.IO.Path.Combine(testOut, "music.txt"), $"{testT:F1} start track {want} (ready={audioMgr.Ready(want)})\n"); }
            if (testMode && testOut != null && audioMgr.Ready(want) && (int)(testT / 4) != (int)((testT - Time.unscaledDeltaTime) / 4)) System.IO.File.AppendAllText(System.IO.Path.Combine(testOut, "music.txt"), $"{testT:F1} current={audioMgr.Current} playing={audioMgr.Playing} peak={audioMgr.Peak(want):F2}\n");
            audioMgr.Fight(phase == "fight");
        }
        void ArriveLevel(int n)
        {
            bool wasHoly = THEME.holy;
            ClearTransient();
            BuildLevel(n);
            PlacePlayer(); P.hp = 100; P.slamming = false; P.sliding = false; P.dashT = 0;
            P.downed = false; P.inPortal = false; P.revive = 0; // co-op: everyone arrives on their feet
            phase = "pre"; waveDelay = LEVEL.boss == 3 ? 0.01f : 2.6f; // the Frozen Traitor is already there, asleep in his ice, when you arrive
            Sfx("arrive"); if (THEME.holy) Sfx("holy");
        }

        // ---------- transient cleanup ----------
        void ClearTransient()
        {
            foreach (var e in enemies) if (e.g) Destroy(e.g);
            foreach (var p in projectiles) Mk.DestroyTree(p.m.gameObject);
            foreach (var c in coins) if (c.m) Destroy(c.m.gameObject);
            foreach (var c in cores) Mk.DestroyTree(c.m.gameObject);
            foreach (var p in particles) KillPt(p);
            foreach (var f in fx) Mk.DestroyTree(f.m.gameObject);
            ClearHazards(); ClearBossDeath(); ClearStagger(); ClearDecals();
            beamCore.SetActive(false); beamOuter.SetActive(false); beamHit.SetActive(false); teleLine.SetActive(false); introPillar.SetActive(false);
            enemies.Clear(); projectiles.Clear(); coins.Clear(); cores.Clear(); particles.Clear(); fx.Clear(); boss = null;
            foreach (var m in pendingMats) if (m) Destroy(m);
            pendingMats.Clear();
            HideBossBar();
            HK.state = "idle"; HK.enemy = null;
        }

        // ---------- waves ----------
        struct WaveSpec { public int boss, husks, brutes, drones; }
        static WaveSpec GetWaveSpec(int w)
        {
            if (w == 5) return new WaveSpec { boss = 1 };
            if (w == 10) return new WaveSpec { boss = 3 }; // Throne of Frost: the Frozen Traitor
            if (w == 15) return new WaveSpec { boss = 4 }; // The Broken Crown: the Hollow King
            if (w == 20) return new WaveSpec { boss = 5 }; // The Hall of Hours: the Warden of Hours
            if (w == TOTAL_WAVES) return new WaveSpec { boss = 2 };
            int d = w < 5 ? w : w - 1;
            var s = new WaveSpec { husks = Mathf.Min(3 + d * 2, 18), brutes = d >= 2 ? Mathf.Min(Mathf.FloorToInt(d * 0.6f), 5) : 0, drones = Mathf.Min(Mathf.RoundToInt(d * 0.9f), 8) };
            // progressive scaling: past the old caps the swarm keeps growing slowly, one more husk every 3 levels, a brute every
            // 6, a drone every 5 (level 24: +5 husks, +2 brutes, +2 drones) - no sudden spikes
            if (w > 8) { s.husks += (w - 8) / 3; s.brutes += (w - 8) / 6; s.drones += (w - 10 > 0 ? (w - 10) / 5 : 0); }
            if (IsEliteWave(w)) { s.husks = 18; s.brutes = 7; s.drones = 9; } // the end of each middle act: everything that act has, at once
            return s;
        }
        static bool IsEliteWave(int w) => false; // (wave 20 is the Warden of Hours now)
        Vector2 FreeSpot(float minDist)
        {
            float range = LB - 6;
            for (int i = 0; i < 240; i++) // (dense maps like the Settlement need more tries now the check is stricter)
            {
                float x = (rand * 2 - 1) * range, z = (rand * 2 - 1) * range;
                if (FarFromPlayers(x, z, minDist) == false) continue;
                if (InPool(x, z, 2)) continue;
                // room for the biggest of them (brutes are ~3x a husk) all the way up, against both the AABB list and the real colliders
                bool hit = false; const float RM = 2.4f;
                foreach (var c in colliders) if (Overlaps(x - RM, 0.01f, z - RM, x + RM, 7f, z + RM, c)) { hit = true; break; }
                if (hit || Physics.CheckBox(V(x, 3.6f, z), V(RM, 3.5f, RM), Quaternion.identity, LEVEL_MASK, QueryTriggerInteraction.Ignore)) continue;
                return new Vector2(x, z);
            }
            return new Vector2(LEVEL.spawn.x, LEVEL.spawn.z - 12);
        }
        void StartWave()
        {
            wave++;
            phase = "fight";
            NetEvent(NetEv.Wave, wave); // before the spawns, so clients have the wave when its enemies arrive
            var spec = GetWaveSpec(wave);
            if (spec.boss > 0)
            {
                if (spec.boss == 3) { SpawnTraitor(); return; } // sleeps in the ice until you come close (its intro plays when it breaks free)
                if (spec.boss == 5) { SpawnWarden(); return; } // comes through the great gate - his title and bar come at the end of the entrance
                if (spec.boss == 4) { SpawnHollowKing(); return; } // falls from the sky - his title and intro come a second after he lands
                Sfx("alarm");
                if (spec.boss == 1) SpawnCarnifex(); else if (spec.boss == 4) SpawnHollowKing(); else SpawnSeraph();
                StartBossIntro(boss);
                if (!BossIntroActive) Banner($"WAVE {wave}", "", THEME.banner);
                return;
            }
            for (int i = 0; i < CoopCount(spec.husks); i++) { var s = FreeSpot(20); SpawnHusk(s.x, s.y, false); }
            for (int i = 0; i < CoopCount(spec.brutes); i++) { var s = FreeSpot(25); SpawnHusk(s.x, s.y, true); }
            if (Act == "ruin" && wave >= 12) for (int i = 0; i < CoopCount(wave - 10); i++) { var s = FreeSpot(24); SpawnHusk(s.x, s.y, false, "bomber"); } // Dead Earth from wave 12: Burners on top of the roll, more each wave
            int drones = Act == "frost" ? Mathf.Min(CoopCount(spec.drones), 8) : CoopCount(spec.drones); // in the Frozen Circle these are Rime Spitters on the walls
            for (int i = 0; i < drones; i++) { var s = FreeSpot(15); SpawnDrone(s.x, 8 + rand * 6, s.y); }
            foreach (var e in enemies) if (e.type == "husk") ShockRing(e.pos, 1.6f * e.s, THEME.holy ? 0x80e8ff : 0xff2000, 0.9f);
            Banner($"WAVE {wave}", "", THEME.banner); // entering a level shows only the wave number
            Sfx("wave"); Sfx("spawn");
            if (testMode) { int bad = 0; foreach (var e in enemies) if (e.type == "husk") foreach (var c in colliders) if (Overlaps(e.pos.x - 0.5f, e.pos.y + 0.2f, e.pos.z - 0.5f, e.pos.x + 0.5f, e.pos.y + 2, e.pos.z + 0.5f, c)) { bad++; break; } Debug.LogWarning($"BR_SPAWNCHECK L{levelNum} husks={enemies.FindAll(e => e.type == "husk").Count} insideBlocks={bad}"); }
        }

        // ---------- main update ----------
        float trapClock, trapIdle = 9; bool trapsArmed;
        void UpdateDecor(float t, float dt)
        {
            if (crystal && crystal.gameObject.activeSelf && !(portal != null && !portal.burst))
            {
                crystalN.ry += dt * 0.6f; crystalN.rx = Mathf.Sin(t * 0.5f) * 0.2f;
                var cp = crystal.localPosition; cp.y = LEVEL.center.y + 7 + Mathf.Sin(t * 1.2f) * 0.4f; crystal.localPosition = cp;
                crystalRingN.SetRot(Mathf.PI / 2 + Mathf.Sin(t * 0.7f) * 0.3f, 0, crystalRingN.rz + dt * 0.8f);
                crystalGlow.localScale = Vector3.one * (9 + Mathf.Sin(t * 2) * 1.2f);
            }
            for (int i = 0; i < flames.Count; i++) if (flames[i]) flames[i].localScale = Vector3.one * ((THEME.deco == "crucible" ? 5.5f : 2.2f) + Mathf.Sin(t * 13 + i * 1.7f) * 0.25f + Mathf.Sin(t * 29 + i) * 0.15f);
            if (M != null && M.pool)
                M.pool.SetTextureOffset("_BaseMap", THEME.pool == "lava" ? new Vector2(t * 0.03f, Mathf.Sin(t * 0.4f) * 0.05f) : new Vector2(-t * 0.02f, t * 0.015f));
            int padCol = THEME.holy ? 0x60d0ff : 0x20ff60;
            foreach (var pd in pads) Mats.Emissive(pd.m, padCol, 0.7f + Mathf.Sin(t * 6) * 0.4f);
            foreach (var a in anims) a(t, dt);
            // the traps run only while there are enemies to fight: once the last one dies they finish what they were doing and go still
            trapsArmed = started && enemies.Exists(e => !e.dead);
            if (trapsArmed) trapIdle = 0; else trapIdle += dt;
            if (trapIdle < 1.6f) { trapClock += dt; foreach (var a in trapAnims) a(trapClock, dt); }
            UpdatePgLater();
        }
        void UpdateAmbient(float dt, Vector3 center)
        {
            ambientT += dt * 14;
            while (ambientT >= 1)
            {
                ambientT -= 1;
                float a = rand * Mathf.PI * 2, r = 4 + rand * 30;
                float x = clamp(center.x + Mathf.Cos(a) * r, -LB, LB), z = clamp(center.z + Mathf.Sin(a) * r, -LB, LB);
                var kind = THEME.ambient; Material m; Vector3 pos, v; float life;
                if (kind == "ash") { m = rand < 0.3f ? emberMat : ashMat; pos = V(x, center.y + 14 + rand * 6, z); v = V(rand - 0.5f, -2 - rand * 2, rand - 0.5f); life = 6; }
                else if (kind == "ember") { m = emberMat; pos = V(x, rand * 2, z); v = V(rand - 0.5f, 2 + rand * 3, rand - 0.5f); life = 3; }
                else if (kind == "snow") { m = SnowMat; pos = V(x, center.y + 12 + rand * 8, z); v = V((rand - 0.5f) * 1.5f, -2.5f - rand * 1.5f, (rand - 0.5f) * 1.5f); life = 6; }
                else if (kind == "fogmote") { m = rand < 0.3f ? moteMat : ashMat; pos = V(x, rand * 6, z); v = V((rand - 0.5f) * 0.8f, 0.2f + rand * 0.4f, (rand - 0.5f) * 0.8f); life = 5; }
                else { m = moteMat; pos = V(x, rand * 8, z); v = V((rand - 0.5f) * 0.6f, 0.6f + rand, (rand - 0.5f) * 0.6f); life = 4; }
                float s = 0.06f + rand * 0.06f;
                particles.Add(new Pt { pos = pos, scale = V(s, s, s), mat = m, v = v, life = life, grav = 0 });
            }
            TrimParticles();
        }
        static readonly float[] RECOIL_Z = { 0.12f, 0.12f, 0.04f, 0.22f, 0.16f }, RECOIL_X = { 0.6f, 0.35f, 0.05f, 0.5f, 0.7f };
        // dt: this player's (slow-mo / hit-stop scaled) clock, wdt: the world's. Solo they are the same value; in co-op the
        // world runs at real time and the scaled clock only moves the viewmodel (see Update).
        void GameUpdate(float dt, float wdt)
        {
            time += wdt;
            if (!P.dead) { UpdatePlayer(wdt); UpdateHook(wdt); }
            UpdateWeapons(wdt);
            UpdateEnemies(wdt); UpdateBossDeath(wdt); UpdateProjectiles(wdt); UpdateCores(wdt); UpdateCoins(wdt); UpdateHazards(wdt); UpdateParticles(wdt); UpdateStyle(wdt);
            UpdateDecor(time, wdt); UpdateAmbient(wdt, P.pos); UpdatePortal(wdt); UpdateStagger(wdt); UpdateCoop(wdt);
            LevelMusic();
            if (victoryT > 0) { victoryT -= wdt; if (victoryT <= 0) ShowVictory(); }
            else if (!P.dead && !won && Auth) // co-op: only the host runs the wave clock; clients follow its phase
            {
                if (phase == "pre") { waveDelay -= wdt; if (waveDelay <= 0) StartWave(); }
                else if (phase == "fight" && enemies.Count == 0 && bossDeath == null)
                {
                    if (wave >= TOTAL_WAVES) phase = "done";
                    else { phase = "cleared"; waveDelay = 1.3f; }
                }
                else if (phase == "cleared") { waveDelay -= wdt; if (waveDelay <= 0) { phase = "portal"; OpenPortal(); } }
            }
            if (dt <= 0) return; // co-op hit-stop: the gun holds still while the world goes on
            // viewmodel motion
            recoil = Mathf.Max(0, recoil - dt * 6); swapT = Mathf.Max(0, swapT - dt);
            float hs = Hypot(P.vel.x, P.vel.z); bool walking = P.onGround && !P.sliding;
            if (walking) bobT += dt * hs * 0.6f;
            float bob = walking ? Mathf.Min(hs / RUN, 1) : 0;
            swayX *= Mathf.Exp(-10 * dt); swayY *= Mathf.Exp(-10 * dt);
            float jitter = P.weapon == 2 && P.spin >= 1 && mouseDown ? (rand - 0.5f) * 0.006f : 0;
            fistT = Mathf.Max(0, fistT - dt);
            bool punching = fistT > 0; float pk = punching ? Mathf.Sin((1 - fistT / SAW_DUR) * Mathf.PI) * 0.5f : 0; // the gun gives the saw room
            vm.localPosition = V(0.3f + Mathf.Cos(bobT) * 0.015f * bob - clamp(swayX, -30, 30) * 0.0012f + jitter + pk * 0.04f,
                -0.28f + Mathf.Abs(Mathf.Sin(bobT)) * 0.02f * bob - swapT * 1.5f + clamp(swayY, -30, 30) * 0.0012f + jitter + (P.onGround ? 0 : clamp(P.vel.y * 0.002f, -0.03f, 0.03f)) - pk * 0.05f,
                -0.72f + recoil * RECOIL_Z[P.weapon]);
            vmN.SetRot(recoil * RECOIL_X[P.weapon], clamp(swayX, -30, 30) * 0.002f, (P.sliding ? 0.25f : 0) - pk * 0.25f);
            armHoldW = 0;
            if (inspectT > 0) UpdateInspect(dt);
            UpdateReload(dt);
            CompensateArms(armHoldW);
            fist.gameObject.SetActive(false);
            bool leftBusy = punching || hookArmK > 0.02f; // the saw / hook arm is the left hand, not an extra one: the gun's own left hand only comes back once that arm has fully gone
            foreach (var g in guns) foreach (var m in g.left) if (m.activeSelf == leftBusy) m.SetActive(!leftBusy);
        }
        void UpdateCamera(float rdt)
        {
            if (P.dead) { if (deathT > 0) DeathCamera(rdt); return; }
            P.eye += ((P.downed ? 0.45f : P.sliding ? 0.75f : 1.6f) - P.eye) * (1 - Mathf.Exp(-15 * rdt));
            P.shake = Mathf.Max(0, P.shake - rdt * 2);
            P.kick *= Mathf.Exp(-7 * rdt);
            float sh = setShake ? P.shake * P.shake * 0.5f : 0;
            camT.position = V(P.pos.x + (rand - 0.5f) * sh, P.pos.y + P.eye + (rand - 0.5f) * sh, P.pos.z + (rand - 0.5f) * sh);
            float strafe = (Key(UnityEngine.InputSystem.Key.D) ? 1 : 0) - (Key(UnityEngine.InputSystem.Key.A) ? 1 : 0);
            if (testMode) strafe = testWish.y;
            P.roll += (-strafe * 0.025f + (P.sliding ? 0.04f : 0) + (P.downed ? 0.22f : 0) - P.roll) * (1 - Mathf.Exp(-10 * rdt));
            camT.rotation = Quaternion.Euler((P.pitch + P.kick) * Mathf.Rad2Deg, P.yaw * Mathf.Rad2Deg, (P.roll + WARP.roll + exRoll) * Mathf.Rad2Deg);
            float hs = Hypot(P.vel.x, P.vel.z);
            // grapple pull: speed lines rush in from the edges and the view widens
            hookPullK += ((HK.state == "pull" ? 1 : 0) - hookPullK) * (1 - Mathf.Exp(-(HK.state == "pull" ? 10 : 5) * rdt));
            int flick = (int)(time * 24) & 1;
            for (int i = 0; i < 2; i++)
            {
                speedImgs[i].color = new Color(1, 0.93f, 0.85f, hookPullK * (i == flick ? 0.55f : 0.2f));
                speedImgs[i].rectTransform.localScale = Vector3.one * (1 + (i == flick ? 0.04f : 0) + (1 - hookPullK) * 0.15f);
            }
            if (HK.state == "pull") P.shake = Mathf.Max(P.shake, 0.12f);
            float fov = DE.active ? 32 : setFov + Mathf.Min(Mathf.Max(hs - RUN, 0) * 0.7f, 18) + (P.slamming ? 8 : 0) + WARP.fov + hookPullK * 9 + exFov;
            cam.fieldOfView += (fov - cam.fieldOfView) * (1 - Mathf.Exp(-(DE.active || WARP.active ? 14 : 8) * rdt));
            muzzleLight.intensity *= Mathf.Exp(-30 * rdt);
            vm.gameObject.SetActive(!DE.active);
        }
        void MenuCamera(float rdt)
        {
            menuT += rdt;
            float r = LB * 0.72f; var c = LEVEL.center;
            camT.position = V(c.x + Mathf.Sin(menuT * 0.08f) * r, 15 + Mathf.Sin(menuT * 0.3f) * 2, c.z + Mathf.Cos(menuT * 0.08f) * r);
            camT.rotation = Quaternion.LookRotation(camT.position - V(c.x, 6, c.z)); // three.js cameras look down -Z
            cam.fieldOfView = 70;
            vm.gameObject.SetActive(false);
            UpdateDecor(menuT, rdt);
            var cp = camT.position; cp.y = 0;
            UpdateAmbient(rdt, cp);
            UpdateParticles(rdt);
        }

        // ---------- game flow ----------
        void StartGame()
        {
            bool fromMenu = !started; // (a new run - not a restart after dying)
            NetEvent(NetEv.Start, practiceWave); // co-op host: everyone starts with us
            ClearTransient();
            BuildLevel(practiceWave > 0 ? practiceWave : 1);
            ResetGame(); started = true; time = 0; deathT = 0; ResetTutorialRun();
            for (int i = 0; i < dmgArcs.Length; i++) dmgArcs[i].t = 0;
            if (practiceWave > 0) waveDelay = 1.5f;
            SetHudVisible(true); SetHint(null);
            ShowScreen(null);
            Banner($"LEVEL {levelNum}", LEVEL.name, THEME.banner);
            Lock();
            // the opening plays once, as a fresh run begins from the menu (not for boss practice or the tutorial, not after a death)
            bool intro = testMode ? fromMenu && Arg("-brintro") != null : fromMenu && practiceWave == 0 && !tutorialRun; IntroMayStart(ref intro);
            INTRO.done = false; if (fromMenu) bcDone.Clear(); if (intro) StartIntro(testMode && float.TryParse(Arg("-brintroat") ?? "", System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var iat) ? iat : 0);
        }
        void QuitToMenu()
        {
            NetLeave();
            started = false;
            ClearTransient();
            BuildLevel(1); ResetGame();
            SetHudVisible(false);
            AudioListener.pause = false;
            audioMgr.Stop(0.3f);
            locked = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            ShowScreen("mainMenu"); audioMgr.Start(6);
        }
        void ShowVictory()
        {
            won = true;
            bool best = SaveHi();
            int m = Mathf.FloorToInt(runTime / 60), s = Mathf.FloorToInt(runTime % 60);
            SetWinStats($"HELL AND HEAVEN BOTH FELL\nSCORE <color=#ffd24a>{S.score}</color>{(best ? "  NEW BEST!" : "")}\nTIME <color=#ffd24a>{m}:{s:00}</color>   BEST <color=#ffd24a>{hiScore}</color>");
            Sfx("victory");
            locked = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            ShowScreen("winScreen");
        }
    }
}
