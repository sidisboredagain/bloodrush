using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    // Dead Earth, second pass: the hazards and set dressing added on top of GameDeadEarth.
    //   SUBWAY      LIVE WATER - the flood in the trenches and on the platforms is fed by severed cables: it buzzes and
    //               sparks crawl over it, then the whole pool arcs
    //   OVERPASS    WRECKING CRANES - two tower cranes left standing over the decks, their wrecking balls swinging across them
    //               FALLING DEBRIS - chunks of the decks' undersides break away onto the road below
    //   CATHEDRAL   STEAM VENTS (floor grates that blast scalding steam), SHREDDERS (toothed drums set in the floor that spin
    //               up), WELDING ARMS (robot arms on pedestals that sweep across the belts)
    // Every one warns (light, sound, a marked area), has an attack and a reset, hits once per attack, and stops with the
    // rest of the traps when the wave is dead (trapAnims / HazardLive).
    public partial class Game
    {
        // Dead Earth's street lamps are solid (you used to walk through the posts)
        void StreetLampS(float x, float z, float ry) { StreetLamp(x, z, ry); SolidAABB(V(x - 0.18f, 0, z - 0.18f), V(x + 0.18f, 6, z + 0.18f)); }
        // a sagging cable from a to b in short straight pieces
        void SagCable(Vector3 a, Vector3 b, float sag, Material m, float r = 0.04f)
        {
            const int N = 8; Vector3 prev = a;
            for (int i = 1; i <= N; i++)
            {
                float t = i / (float)N; var p = Vector3.Lerp(a, b, t) - V(0, Mathf.Sin(t * Mathf.PI) * sag, 0); var d = p - prev;
                var g = Deco(Geo.Cyl(r, r, d.magnitude, 5), m, (p.x + prev.x) / 2, (p.y + prev.y) / 2, (p.z + prev.z) / 2); g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized); Ig(g);
                prev = p;
            }
        }
        // jagged lightning: n boxes re-cut between random points of a set, per frame
        List<(Transform t, Material m)> ArcSet(int n, int col = 0xd8f0ff)
        {
            var l = new List<(Transform, Material)>();
            for (int k = 0; k < n; k++) { var am = Mats.Add(col, 0, null, true, true); var ag = Ig(Mk.Msh(levelRoot, Geo.Box(0.07f, 0.07f, 1), am)); l.Add((ag.transform, am)); }
            return l;
        }
        void ArcBetween((Transform t, Material m) a, Vector3 p0, Vector3 p1)
        {
            var dd = p1 - p0; if (dd.sqrMagnitude < 0.0001f) dd = Vector3.up * 0.01f;
            a.t.localPosition = (p0 + p1) / 2; a.t.localRotation = Quaternion.LookRotation(dd); a.t.localScale = V(1, 1, dd.magnitude); Mats.SetAlpha(a.m, 0.6f + rand * 0.4f);
        }

        // the dead power lines lying across Ashfall's streets (just cable, no current)
        void DeadCables(List<(Vector3 a, Vector3 b)> lines)
        {
            var cabM = Mats.Std(0x141414, 0.7f, 0.3f);
            foreach (var (a, b) in lines) SagCable(a, b, -0.05f, cabM, 0.06f);
        }
        // ================= SUBWAY: LIVE WATER (unused - Dead Earth has no electric traps) =================
        void MechLiveWater((float x, float z, float w, float d, float y, Vector3 src)[] pools)
        {
            var cabM = Mats.Std(0x141414, 0.7f, 0.3f); var copper = Mats.Std(0xb87a40, 0.3f, 0.9f);
            for (int i = 0; i < pools.Length; i++)
            {
                var pl = pools[i]; float off = i * 2.3f; var src = pl.src;
                var glowM = Mats.Add(0x5ab0ff, 0, null, true, true); Flat(Geo.Plane(pl.w, pl.d), glowM, pl.x, pl.y + 0.03f, pl.z);
                var rimM = HazMat(0x60c0ff, 0.1f, true); foreach (var s in new[] { -1f, 1f }) { Flat(Geo.Plane(pl.w, 0.15f), rimM, pl.x, pl.y + 0.035f, pl.z + s * pl.d / 2); Flat(Geo.Plane(0.15f, pl.d), rimM, pl.x + s * pl.w / 2, pl.y + 0.035f, pl.z); }
                // the cable that feeds it: down from the ceiling (or off the third rail), its stripped copper end in the water
                var end = V(Mathf.Clamp(src.x, pl.x - pl.w / 2 + 0.4f, pl.x + pl.w / 2 - 0.4f), pl.y + 0.05f, Mathf.Clamp(src.z, pl.z - pl.d / 2 + 0.4f, pl.z + pl.d / 2 - 0.4f));
                SagCable(src, end + V(0, 0.15f, 0), src.y > pl.y + 3 ? 0.6f : 0.2f, cabM, 0.05f);
                Ig(Deco(Geo.Cyl(0.03f, 0.05f, 0.3f, 6), copper, end.x, end.y + 0.1f, end.z, 0.6f, 0, 0.3f));
                var arcs = ArcSet(9); bool hitP = false; var hitE = new HashSet<Ent>();
                Vector3 RandIn() => V(pl.x + (rand - 0.5f) * pl.w * 0.95f, pl.y + 0.06f, pl.z + (rand - 0.5f) * pl.d * 0.95f);
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 7f; bool warn = u >= 4.4f && u < 5.9f, live = u >= 5.9f && u < 7f;
                    Mats.SetAlpha(glowM, warn ? 0.05f + 0.2f * (u - 4.4f) / 1.5f * (Mathf.Sin(t * 30) > 0 ? 1 : 0.5f) : live ? 0.35f + rand * 0.2f : 0);
                    Mats.SetAlpha(rimM, warn || live ? 0.5f + 0.3f * Mathf.Sin(t * 20) : 0.1f);
                    if (rand < 0.15f) SpawnParticles(end + V(0, 0.15f, 0), 1, 2, sparkMat, 0.04f, 0.25f); // the bare end always spits
                    if (warn && rand < 0.6f) SpawnParticles(RandIn(), 1, 1.5f, rand < 0.5f ? sparkMat : moteMat, 0.04f, 0.2f);
                    if (u >= 4.4f && u - dt < 4.4f && HazardLive && Hypot(P.pos.x - pl.x, P.pos.z - pl.z) < 25) Sfx("charge", 0.45f);
                    for (int k = 0; k < arcs.Count; k++)
                    {
                        if (!live) { Mats.SetAlpha(arcs[k].m, 0); continue; }
                        var p0 = k == 0 ? end : RandIn(); var p1 = RandIn(); p0.y += rand * 0.4f; p1.y += rand * 0.3f; ArcBetween(arcs[k], p0, p1);
                    }
                    if (!live) { hitP = false; hitE.Clear(); return; }
                    if (u - dt < 5.9f) { Sfx("laserTick"); Sfx("boomSmall", 0.5f); Blast(end + V(0, 0.4f, 0), 1.6f, 0x9ad8ff); }
                    if (rand < 0.5f) SpawnParticles(RandIn(), 2, 4, sparkMat, 0.05f, 0.3f);
                    if (!HazardLive) return;
                    bool inP = Mathf.Abs(P.pos.x - pl.x) < pl.w / 2 && Mathf.Abs(P.pos.z - pl.z) < pl.d / 2 && P.pos.y < pl.y + 0.45f;
                    if (!hitP && inP) { hitP = true; HazardHitPlayer(18, V(P.pos.x, pl.y, P.pos.z), V(0, 7, 0)); hurtFlash = Mathf.Max(hurtFlash, 0.3f); }
                    HazardHitEnemies(e => Mathf.Abs(e.pos.x - pl.x) < pl.w / 2 && Mathf.Abs(e.pos.z - pl.z) < pl.d / 2 && e.pos.y < pl.y + 0.45f && hitE.Add(e), 18, e => V(0, 6, 0), 1);
                });
            }
        }
        // the station's extra wreckage and the flood: water stains up the walls, fallen ceiling panels, a burst main, cables
        void SubwayExtras(float PH)
        {
            var stain = Mats.Trans(0x0a1210, 0.45f); var panel = Mats.Std(0x8a8a80, 0.8f, 0, 0, GrimeTex()); var cabM = Mats.Std(0x141414, 0.7f, 0.3f);
            var water = Mats.Std(0x16282a, 0.04f, 0.4f, 0x020606);
            foreach (var sx in new[] { -1f, 1f })
            {
                // the tide line along the trench walls and the far walls, where the water stood for months
                Ig(Deco(Geo.Box(0.02f, 0.6f, 116), stain, sx * 12.02f, PH * 0.6f, 0)); Ig(Deco(Geo.Box(0.02f, 0.6f, 116), stain, sx * 5.98f, PH * 0.6f, 0));
                Ig(Deco(Geo.Box(0.02f, 1.1f, 116), stain, sx * 59.3f, PH + 0.6f, 0));
                if (sx > 0) { Ig(Deco(Geo.Box(0.02f, 0.6f, 116), stain, 19.02f, PH * 0.6f, 0)); Ig(Deco(Geo.Box(0.02f, 0.6f, 116), stain, 24.98f, PH * 0.6f, 0)); }
                // the flooded corner of each side platform, under a burst water main
                Flat(Geo.Plane(9, 7), water, sx * 30, PH + 0.03f, -30);
                Deco(Geo.Cyl(0.5f, 0.5f, 30, 12), Mats.Std(0x4a4a46, 0.6f, 0.5f, 0, RustTex()), sx * 30, 14.2f, -30, Mathf.PI / 2, 0, 0);
                Ig(Deco(Geo.Box(0.9f, 0.4f, 0.9f), Mats.Std(0x3a3a36, 0.5f, 0.6f, 0, RustTex()), sx * 30, 13.7f, -31, 0.3f, 0, 0.2f)); var drip = V(sx * 30, 13.4f, -31);
                anims.Add((t, dt) => { if (rand < 0.5f) particles.Add(new Pt { pos = drip + V((rand - 0.5f) * 0.4f, 0, (rand - 0.5f) * 0.4f), scale = Vector3.one * 0.05f, mat = steamMat, v = V(0, -2, 0), life = 1.2f, grav = GRAV }); });
                // ceiling panels down on the platforms, a hanging one, loose cables
                for (int k = 0; k < 4; k++) { float z = -40 + k * 26; var pn = Deco(Geo.Box(3, 0.08f, 2), panel, sx * (22 + k % 2 * 8) + (sx > 0 ? 8 : 0), PH + 0.15f, z, (k - 1.5f) * 0.1f, k, 0.2f); Ig(pn); }
                var hang = Mk.Piv(levelRoot, sx * 20, 14.4f, 12); hang.SetRot(0.9f, 0, 0.2f); Mk.Msh(hang, Geo.Box(3, 0.08f, 2), panel, 0, 0, -1); foreach (var rr in hang.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                for (int k = 0; k < 3; k++) SagCable(V(sx * (18 + k * 9), 14.6f, -10 + k * 14), V(sx * (24 + k * 9), 14.6f, -4 + k * 14), 2.2f + k * 0.6f, cabM, 0.035f);
            }
            // (no live water: Dead Earth has no electric traps)
        }

        // ================= OVERPASS: WRECKING CRANES and FALLING DEBRIS =================
        // a tower crane: a lattice mast, a jib reaching out over the deck, a counter-jib with its weights, the cab; the
        // wrecking ball swings on its cable from the trolley out on the jib
        void MechWreckingCrane(Vector3 mastBase, Vector3 pivot, float len, Vector3 swing, float amp, float period, float off)
        {
            var yellow = Mats.Std(0xc89a20, 0.6f, 0.4f, 0, RustTex()); var iron = Mats.Std(0x3a3632, 0.5f, 0.75f, 0, RustTex()); var cab = Mats.Std(0x2a2a2a, 0.6f, 0.4f);
            float top = pivot.y + 1.2f;
            // the mast: four corner chords with cross-bracing, solid core
            Box(mastBase.x, 0, mastBase.z, 2.2f, top, 2.2f, iron);
            foreach (var a in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) Deco(Geo.Box(0.25f, top, 0.25f), yellow, mastBase.x + a * 1.2f, top / 2, mastBase.z + b * 1.2f);
            for (float y = 1.5f; y < top - 1; y += 3) foreach (var sd in new[] { 0, 1 }) { var g = Deco(Geo.Box(sd == 0 ? 3.3f : 0.12f, 0.12f, sd == 0 ? 0.12f : 3.3f), yellow, mastBase.x, y + 1.5f, mastBase.z + (sd == 0 ? 1.25f : 0) - (sd == 1 ? 0 : 0), sd == 0 ? 0 : 0.8f, 0, sd == 0 ? 0.8f : 0); Ig(g); }
            // the jib from the mast top out over the deck, and the counter-jib behind
            var dir = V(pivot.x - mastBase.x, 0, pivot.z - mastBase.z); float jl = dir.magnitude + 3; dir.Normalize(); float jy = Mathf.Atan2(dir.x, dir.z);
            var jm = V(mastBase.x, top + 0.6f, mastBase.z) + dir * jl / 2; Deco(Geo.Box(1.4f, 1.2f, jl), yellow, jm.x, jm.y, jm.z, 0, jy, 0);
            var cm = V(mastBase.x, top + 0.6f, mastBase.z) - dir * 5; Deco(Geo.Box(1.4f, 1.0f, 10), yellow, cm.x, cm.y, cm.z, 0, jy, 0);
            var cw = cm - dir * 3.5f; Deco(Geo.Box(2.4f, 2.2f, 2.4f), Mats.Std(0x6a645a, 0.9f, 0, 0, GrimeTex()), cw.x, cw.y - 1.4f, cw.z, 0, jy, 0); // the counterweights
            Deco(Geo.Box(2.0f, 2.0f, 2.2f), cab, mastBase.x + dir.x * 1.6f, top - 1.0f, mastBase.z + dir.z * 1.6f, 0, jy, 0);
            Deco(Geo.Box(1.9f, 0.8f, 0.05f), glassM, mastBase.x + dir.x * 2.72f, top - 0.8f, mastBase.z + dir.z * 2.72f, 0, jy, 0);
            Deco(Geo.Cyl(0.15f, 0.15f, 4, 6), yellow, mastBase.x, top + 2.6f, mastBase.z); // the apex
            SagCable(V(mastBase.x, top + 4.5f, mastBase.z), V(jm.x + dir.x * jl / 2, top + 1.2f, jm.z + dir.z * jl / 2), 0.3f, iron, 0.05f);
            SagCable(V(mastBase.x, top + 4.5f, mastBase.z), cw + V(0, 0.5f, 0), 0.2f, iron, 0.05f);
            var lampM = Mats.Add(0xff3010, 0.2f, null, true, true); var lamp = Ig(Deco(Geo.Sphere(0.3f, 8, 6), lampM, pivot.x, pivot.y + 1.9f, pivot.z));
            Deco(Geo.Box(1.6f, 0.6f, 1.4f), iron, pivot.x, pivot.y + 0.3f, pivot.z, 0, jy, 0); // the trolley
            // the ball, its cable, the hook block
            var arm = Mk.Piv(levelRoot, pivot.x, pivot.y, pivot.z);
            var cable = Mk.Msh(arm, Geo.Cyl(0.05f, 0.05f, len - 1.8f, 5), iron, 0, -(len - 1.8f) / 2, 0);
            Mk.Msh(arm, Geo.Box(0.6f, 0.8f, 0.4f), yellow, 0, -len + 2.2f, 0);
            var ballM = Mats.Std(0x2a2826, 0.55f, 0.8f, 0, RustTex());
            Mk.Msh(arm, Geo.Sphere(1.6f, 18, 14), ballM, 0, -len, 0);
            Mk.Msh(arm, Geo.Torus(1.62f, 0.12f, 24, 5), iron, 0, -len, 0, Mathf.PI / 2, 0, 0); Mk.Msh(arm, Geo.Cyl(0.3f, 0.4f, 0.6f, 8), iron, 0, -len + 1.6f, 0);
            foreach (var rr in arm.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
            // the path it sweeps, marked on the deck
            var sw = swing.normalized; var axis = Vector3.Cross(Vector3.up, sw); float reach = len * Mathf.Sin(amp);
            var pathM = HazMat(0xff4010, 0, false); var pf = Flat(Geo.Plane(3.4f, reach * 1.4f), pathM, pivot.x, pivot.y - len - 1.55f, pivot.z); pf.transform.localRotation = Quaternion.Euler(-90, Mathf.Atan2(sw.x, sw.z) * Mathf.Rad2Deg, 0);
            bool hitP = false; var hitE = new HashSet<Ent>(); float lastA = 0;
            trapAnims.Add((t, dt) =>
            {
                float ph = (t + off) / period * Mathf.PI * 2, ang = amp * Mathf.Sin(ph), angV = amp * Mathf.Cos(ph);
                arm.t.localRotation = Quaternion.AngleAxis(ang * Mathf.Rad2Deg, axis);
                var ball = pivot + Quaternion.AngleAxis(ang * Mathf.Rad2Deg, axis) * V(0, -len, 0);
                float fast = Mathf.Abs(angV) / amp; // 1 at the bottom of the swing
                Mats.SetAlpha(pathM, 0.06f + 0.22f * fast * (Mathf.Sin(t * 16) > 0 ? 1 : 0.6f)); Mats.SetAlpha(lampM, fast > 0.6f ? 0.9f : 0.15f);
                if (Mathf.Sign(ang) != Mathf.Sign(lastA)) { hitP = false; hitE.Clear(); if (HazardLive && Vector3.Distance(ball, P.pos) < 30) Sfx("dashGo", 0.55f); } // the whoosh through the bottom
                if (Mathf.Abs(ang) > amp * 0.97f && Mathf.Abs(lastA) <= amp * 0.97f && HazardLive && Vector3.Distance(ball, P.pos) < 40) Sfx("slamStart", 0.35f); // the cable creaks at the top
                lastA = ang;
                if (!HazardLive || fast < 0.3f) return;
                var vel = (Quaternion.AngleAxis(ang * Mathf.Rad2Deg, axis) * Vector3.Cross(axis, V(0, -len, 0))).normalized * Mathf.Sign(angV);
                if (!hitP && Vector3.Distance(P.pos + V(0, 0.9f, 0), ball) < 2.5f) { hitP = true; HazardHitPlayer(30, ball, vel * 24 + V(0, 8, 0)); P.shake = Mathf.Max(P.shake, 0.6f); Sfx("slam"); }
                HazardHitEnemies(e => Vector3.Distance(e.pos + V(0, 0.9f, 0), ball) < 2.6f && hitE.Add(e), 30, e => vel * 20 + V(0, 8, 0), 1);
            });
        }
        // chunks of the deck's underside break away: dust trickles and the spot glows, then a slab of concrete falls
        void MechFallingDebris(Vector3[] spots, float fromY)
        {
            var chunkM = Mats.Std(0x7a746a, 0.92f, 0, 0, GrimeTex());
            for (int i = 0; i < spots.Length; i++)
            {
                var c = spots[i]; float off = i * 1.9f;
                var warnM = HazMat(0xff5010, 0, false); Flat(Geo.Circle(2.6f, 24), warnM, c.x, c.y + 0.06f, c.z); var ringM = HazMat(0xffd0a0, 0, true); Flat(Geo.Ring(2.4f, 2.6f, 32), ringM, c.x, c.y + 0.07f, c.z);
                var chunk = Mk.Piv(levelRoot, c.x, fromY, c.z); Mk.Msh(chunk, Geo.Box(2.6f, 0.9f, 2.2f), chunkM, 0, -0.45f, 0); for (int k = 0; k < 4; k++) Mk.Msh(chunk, Geo.Cyl(0.04f, 0.04f, 1.4f, 4), rebarM, (k - 1.5f) * 0.6f, -0.2f, 0.9f, 0.5f, 0, 0.3f * k);
                foreach (var rr in chunk.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                chunk.t.gameObject.SetActive(false);
                bool hitP = false;
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 9f; bool warn = u >= 5.5f && u < 7.0f, fall = u >= 7.0f && u < 7.55f;
                    Mats.SetAlpha(warnM, warn ? 0.08f + 0.25f * (u - 5.5f) / 1.5f * (Mathf.Sin(u * 20) > 0 ? 1 : 0.5f) : fall ? 0.3f : 0); Mats.SetAlpha(ringM, warn || fall ? 0.6f : 0);
                    if (warn && rand < 0.5f) SpawnParticles(V(c.x + (rand - 0.5f) * 2, fromY - 0.2f, c.z + (rand - 0.5f) * 2), 1, 0.6f, dustMat, 0.12f, 1.2f, 4); // dust trickling down
                    if (u >= 5.5f && u - dt < 5.5f && HazardLive && Vector3.Distance(c, P.pos) < 22) Sfx("growlLow", 0.4f); // the groan of the concrete
                    chunk.t.gameObject.SetActive(fall);
                    if (!fall) { hitP = false; return; }
                    float k = (u - 7.0f) / 0.55f; chunk.t.localPosition = V(c.x, Mathf.Lerp(fromY, c.y + 0.9f, k * k), c.z); chunk.t.localRotation = Quaternion.Euler(k * 25, 0, k * 15);
                    if (u + dt >= 7.55f)
                    {
                        Sfx("slam"); Sfx("boomSmall", 0.6f); P.shake = Mathf.Max(P.shake, Vector3.Distance(c, P.pos) < 15 ? 0.45f : 0.15f);
                        SpawnParticles(c + V(0, 0.6f, 0), 30, 8, dustMat, 0.35f, 1.0f);
                        for (int j = 0; j < 10; j++) particles.Add(new Pt { pos = c + V((rand - 0.5f) * 2, 0.8f, (rand - 0.5f) * 2), scale = Vector3.one * (0.25f + rand * 0.45f), mat = chunkM, v = V((rand - 0.5f) * 10, 3 + rand * 5, (rand - 0.5f) * 10), life = 1.8f, grav = GRAV, hasSpin = true, spin = V(rand * 8, rand * 8, 0) });
                        if (HazardLive && !hitP && Hypot(P.pos.x - c.x, P.pos.z - c.z) < 2.6f && P.pos.y < c.y + 2.5f) { hitP = true; var o = P.pos - c; o.y = 0; HazardHitPlayer(24, c, (o.sqrMagnitude > 0.01f ? o.normalized : Vector3.forward) * 10 + V(0, 7, 0)); }
                        if (HazardLive) HazardHitEnemies(e => Hypot(e.pos.x - c.x, e.pos.z - c.z) < 2.6f && e.pos.y < c.y + 2.5f, 24, e => V(0, 6, 0), 1);
                    }
                });
            }
        }
        void TyrePile(float x, float z, int n) { var ty = Mats.Std(0x161616, 0.85f, 0, 0, GrimeTex()); for (int k = 0; k < n; k++) Deco(Geo.Torus(0.42f, 0.17f, 16, 6), ty, x + (k % 2) * 0.2f, 0.17f + k * 0.32f, z, Mathf.PI / 2 + (k % 3 - 1) * 0.1f, 0, 0); SolidAABB(V(x - 0.6f, 0, z - 0.6f), V(x + 0.6f, 0.34f * n, z + 0.6f)); }
        void RoadSign(float x, float z, float ry, int col, bool bent)
        {
            var p = Mk.Piv(levelRoot, x, 0, z); p.SetRot(bent ? 0.35f : 0, ry, bent ? 0.2f : 0);
            Mk.Msh(p, Geo.Cyl(0.06f, 0.06f, 3, 6), steelM, 0, 1.5f, 0); Mk.Msh(p, Geo.Box(1.4f, 1.0f, 0.05f), Mats.Std(col, 0.6f, 0.3f, 0, RustTex()), 0, 3.1f, 0.06f); Mk.Msh(p, Geo.Box(1.0f, 0.12f, 0.055f), laneWhiteM, 0, 3.2f, 0.07f);
            foreach (var rr in p.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore; SolidAABB(V(x - 0.1f, 0, z - 0.1f), V(x + 0.1f, 3, z + 0.1f));
        }
        void FenceRun(Vector3 a, Vector3 b)
        {
            var mesh = Mats.Trans(0x4a4a46, 0.2f, true); /* chain-link: mostly see-through */ var d = b - a; float len = d.magnitude; var m = (a + b) / 2; float ry = Mathf.Atan2(d.x, d.z);
            Ig(Deco(Geo.Box(0.03f, 2.2f, len), mesh, m.x, 1.1f, m.z, 0, ry, 0)); Ig(Deco(Geo.Box(0.06f, 0.06f, len), steelM, m.x, 2.2f, m.z, 0, ry, 0));
            int n = (int)(len / 3); for (int k = 0; k <= n; k++) { var p = Vector3.Lerp(a, b, k / (float)Mathf.Max(1, n)); Deco(Geo.Cyl(0.05f, 0.05f, 2.3f, 6), steelM, p.x, 1.15f, p.z); }
            float c = Mathf.Abs(Mathf.Cos(ry)), s = Mathf.Abs(Mathf.Sin(ry)); SolidAABB(V(m.x - s * len / 2 - 0.1f, 0, m.z - c * len / 2 - 0.1f), V(m.x + s * len / 2 + 0.1f, 2.2f, m.z + c * len / 2 + 0.1f));
        }
        void OverpassExtras(float DY)
        {
            // street lamps down the road below, a few bent over
            for (int k = 0; k < 8; k++) { float x = -56 + k * 16; StreetLampS(x, k % 2 == 0 ? 7.6f : -7.6f, k % 2 == 0 ? -Mathf.PI / 2 : Mathf.PI / 2); }
            // signs and fences along the roadside, tyre piles, more wrecks piled up on the decks
            RoadSign(-24, 8.2f, 0, 0x1e5a32, false); RoadSign(14, -8.2f, Mathf.PI, 0x2a3a7a, true); RoadSign(42, 8.2f, 0, 0xa02818, false); RoadSign(-48, -8.2f, Mathf.PI, 0xd8b020, true);
            FenceRun(V(-62, 0, 14), V(-44, 0, 14)); /* (stops short of the stairs up to the deck) */ FenceRun(V(14, 0, -14), V(30, 0, -14)); FenceRun(V(40, 0, 16), V(60, 0, 16)); FenceRun(V(-60, 0, -16), V(-46, 0, -16));
            foreach (var p in new[] { V(-28, 0, 18), V(22, 0, -18), V(56, 0, -12), V(-58, 0, 6), V(4, 0, 22) }) TyrePile(p.x, p.z, 3 + ((int)p.x & 1));
            foreach (var (x, z, ry) in new[] { (-50f, 27f, 1.3f), (-46f, 33f, 1.9f), (48f, -27f, -1.4f), (52f, -33f, -1.1f), (-6f, -33f, 1.6f) }) Vehicle(x, z, ry, (int)Mathf.Abs(x) % 3, true, DY);
            // a survivors' camp under the south deck: a tarp lean-to on the pier, mattresses, a fire barrel, a shopping cart, crates
            {
                float cx = -22, cz = -27; var tarp = Mats.Std(0x2a4a6a, 0.9f, 0, 0, ClothTex2(), true); var matt = Mats.Std(0x8a7a62, 0.95f, 0, 0, ClothTex2());
                Deco(Geo.Box(5, 0.04f, 3.4f), tarp, cx, 2.2f, cz, 0.55f, 0, 0); foreach (var sx in new[] { -2.3f, 2.3f }) Deco(Geo.Cyl(0.05f, 0.05f, 2.6f, 6), steelM, cx + sx, 1.3f, cz + 1.5f);
                for (int k = 0; k < 2; k++) Deco(Geo.Box(1.8f, 0.2f, 0.9f), matt, cx - 1.2f + k * 2.2f, 0.1f, cz - 0.4f, 0, k * 0.3f, 0);
                BurnBarrel(cx + 3.4f, cz + 2.6f);
                var cart = Mk.Piv(levelRoot, cx - 3.6f, 0, cz + 2.4f); cart.SetRot(0, 0.6f, 0); for (int k = 0; k < 5; k++) Mk.Msh(cart, Geo.Box(0.03f, 0.6f, 0.9f), steelM, -0.3f + k * 0.15f, 0.7f, 0); Mk.Msh(cart, Geo.Box(0.7f, 0.03f, 0.9f), steelM, 0, 0.4f, 0); foreach (var a in new[] { -0.3f, 0.3f }) foreach (var b in new[] { -0.4f, 0.4f }) Mk.Msh(cart, Geo.Cyl(0.06f, 0.06f, 0.04f, 8), Mats.Std(0x141414, 0.8f), a, 0.06f, b, 0, 0, Mathf.PI / 2); foreach (var rr in cart.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore; SolidAABB(V(cx - 4.2f, 0, cz + 1.8f), V(cx - 3.0f, 1.0f, cz + 3.0f));
                Box(cx + 2, 0, cz - 2.2f, 1.2f, 1.0f, 1.2f, Mats.Std(0x6a4a2a, 0.9f, 0, 0, GrimeTex())); Box(cx + 3.1f, 0, cz - 2.0f, 0.9f, 0.8f, 0.9f, Mats.Std(0x5a3e24, 0.9f, 0, 0, GrimeTex()));
            }
            // oil and scorch on the asphalt below, cracks
            for (int k = 0; k < 14; k++) { float a = k * 2.4f, r = 10 + (k * 11) % 50; Ig(Flat(Geo.Circle(0.8f + (k % 3) * 0.6f, 16), Mats.Std(0x080706, 0.15f, 0.3f), Mathf.Cos(a) * r, 0.03f, Mathf.Sin(a) * r * 0.25f)); }
            for (int k = 0; k < 24; k++) { float a = k * 2.399f, r = 6 + (k * 7 % 56); Ig(Flat(Geo.Plane(0.12f, 2 + k % 4), Mats.Trans(0x0a0806, 0.6f), Mathf.Cos(a) * r, 0.035f, Mathf.Sin(a) * r)).transform.localRotation = Quaternion.Euler(-90, k * 41, 0); }
            // the hazards
            MechWreckingCrane(V(10, 0, 48), V(10, 31, 30), 21.2f, V(1, 0, 0), 0.75f, 5.2f, 0);
            MechWreckingCrane(V(-24, 0, -48), V(-24, 31, -30), 21.2f, V(0, 0, 1), 0.85f, 5.8f, 1.4f);
            MechFallingDebris(new[] { V(-42, 0, 28), V(-18, 0, 33), V(24, 0, 27), V(-40, 0, -32), V(4, 0, -27), V(28, 0, -32) }, DY - 1.6f);
        }

        // ================= CATHEDRAL: STEAM VENTS, SHREDDERS, WELDING ARMS =================
        void MechSteamVents(Vector3[] spots)
        {
            var grateM = Mats.Std(0x2a2826, 0.5f, 0.75f, 0, RustTex());
            for (int i = 0; i < spots.Length; i++)
            {
                var c = spots[i]; float off = i * 1.37f;
                Deco(Geo.Box(2.4f, 0.08f, 2.4f), grateM, c.x, 0.04f, c.z); for (int k = 0; k < 7; k++) Ig(Deco(Geo.Box(0.08f, 0.1f, 2.3f), Mats.Std(0x141210, 0.6f, 0.6f), c.x - 1.05f + k * 0.35f, 0.06f, c.z));
                var heatM = Mats.Add(0xff6020, 0, null, true, true); Flat(Geo.Plane(2.2f, 2.2f), heatM, c.x, 0.03f, c.z);
                var colM = Mats.Trans(0xe8eef4, 0, true); var col = Ig(Deco(Geo.Cyl(0.9f, 1.2f, 1, 14, true), colM, c.x, 0, c.z));
                var ringM = HazMat(0xffa060, 0.1f, true); Flat(Geo.Ring(1.4f, 1.6f, 28), ringM, c.x, 0.08f, c.z);
                bool hitP = false; var hitE = new HashSet<Ent>();
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 6f; bool warn = u >= 3.6f && u < 4.8f, blast = u >= 4.8f && u < 5.8f;
                    Mats.SetAlpha(heatM, warn ? 0.1f + 0.4f * (u - 3.6f) / 1.2f : blast ? 0.5f : 0.04f); Mats.SetAlpha(ringM, warn || blast ? 0.6f + 0.3f * Mathf.Sin(u * 24) : 0.1f);
                    if (rand < (warn ? 0.6f : 0.06f)) SpawnParticles(c + V((rand - 0.5f) * 1.6f, 0.1f, (rand - 0.5f) * 1.6f), 1, 1.2f, steamMat, 0.2f, 0.8f, -3); // wisps, then the hiss
                    if (u >= 3.6f && u - dt < 3.6f && HazardLive && Vector3.Distance(c, P.pos) < 22) Sfx("slide", 0.5f);
                    float h = blast ? Mathf.Min(1, (u - 4.8f) / 0.15f) * 9 * (1 - Mathf.Max(0, (u - 5.5f) / 0.3f)) : 0;
                    col.SetActive(h > 0.1f); if (h > 0.1f) { col.transform.localScale = V(1 + Mathf.Sin(t * 30) * 0.05f, h, 1 + Mathf.Cos(t * 27) * 0.05f); col.transform.localPosition = V(c.x, h / 2, c.z); Mats.SetAlpha(colM, 0.35f); }
                    if (!blast) { hitP = false; hitE.Clear(); return; }
                    if (u - dt < 4.8f) { Sfx("bile", 0.6f); Sfx("dash", 0.6f); }
                    if (rand < 0.9f) SpawnParticles(c + V((rand - 0.5f) * 1.2f, 1 + rand * 6, (rand - 0.5f) * 1.2f), 2, 3, steamMat, 0.4f, 0.9f, -4);
                    if (!HazardLive) return;
                    if (!hitP && Hypot(P.pos.x - c.x, P.pos.z - c.z) < 1.4f && P.pos.y < 8) { hitP = true; HazardHitPlayer(18, c, V(0, 16, 0)); P.onGround = false; }
                    HazardHitEnemies(e => Hypot(e.pos.x - c.x, e.pos.z - c.z) < 1.4f && e.pos.y < 8 && hitE.Add(e), 18, e => V(0, 14, 0), 1);
                });
            }
        }
        // a shredder: two toothed drums set into the floor in a steel frame; they idle, then the lamps flash, they spin up
        // and chew anything standing on them (damage in ticks, and they drag you in toward the seam)
        void MechShredders((Vector3 c, bool alongX)[] spots)
        {
            var frameM = Mats.Std(0x3a3632, 0.5f, 0.75f, 0, RustTex()); var drumM = Mats.Std(0x5a5650, 0.35f, 0.85f, 0, RustTex()); var toothM = Mats.Std(0x8a8a88, 0.25f, 0.9f); var stripe = Mats.Std(0xd8a020, 0.7f, 0, 0, GrimeTex());
            const float LEN = 5.0f, R = 0.75f;
            for (int i = 0; i < spots.Length; i++)
            {
                var (c, ax) = spots[i]; float off = i * 2.6f; float ry = ax ? Mathf.PI / 2 : 0; var q = Quaternion.Euler(0, ry * Mathf.Rad2Deg, 0);
                // the frame round the pit: thick sills, hazard-striped, a motor housing at one end with a warning beacon
                foreach (var s in new[] { -1f, 1f }) { var sp = c + q * V(s * (R * 2 + 0.35f), 0, 0); Deco(Geo.Box(0.6f, 0.3f, LEN + 1.2f), frameM, sp.x, 0.15f, sp.z, 0, ry, 0); Ig(Deco(Geo.Box(0.62f, 0.06f, LEN + 1.2f), stripe, sp.x, 0.32f, sp.z, 0, ry, 0)); }
                foreach (var s in new[] { -1f, 1f }) { var ep = c + q * V(0, 0, s * (LEN / 2 + 0.4f)); Deco(Geo.Box(R * 4 + 1.3f, 0.3f, 0.6f), frameM, ep.x, 0.15f, ep.z, 0, ry, 0); }
                var mp = c + q * V(0, 0, LEN / 2 + 1.6f); Box(mp.x, 0, mp.z, ax ? 2.0f : 3.4f, 2.0f, ax ? 3.4f : 2.0f, frameM);
                var lampM = Mats.Add(0xff4010, 0.1f, null, true, true); Ig(Deco(Geo.Sphere(0.25f, 8, 6), lampM, mp.x, 2.3f, mp.z));
                Flat(Geo.Plane(ax ? LEN : R * 4, ax ? R * 4 : LEN), Mats.Std(0x060504, 0.9f), c.x, 0.02f, c.z); // the dark of the pit between the drums
                var drums = new List<Transform>();
                foreach (var s in new[] { -1f, 1f })
                {
                    var dp = Mk.Piv(levelRoot, c.x + (q * V(s * R, 0, 0)).x, 0.05f, c.z + (q * V(s * R, 0, 0)).z); dp.t.localRotation = q;
                    var spin = Mk.Piv(dp); Mk.Msh(spin, Geo.Cyl(R * 0.85f, R * 0.85f, LEN, 14), drumM, 0, 0, 0, Mathf.PI / 2, 0, 0);
                    for (int k = 0; k < 7; k++) for (int j = 0; j < 6; j++) { float a = j * Mathf.PI / 3 + k * 0.5f; var tp = Mk.Msh(spin, Geo.Cone(0.12f, 0.32f, 4), toothM, Mathf.Cos(a) * R * 0.92f, Mathf.Sin(a) * R * 0.92f, (k - 3) * LEN / 7.5f); tp.transform.localRotation = Quaternion.FromToRotation(Vector3.up, V(Mathf.Cos(a), Mathf.Sin(a), 0)); }
                    foreach (var rr in dp.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                    drums.Add(spin.t);
                }
                float ang = 0, tick = 0;
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 8f; bool warn = u >= 4.4f && u < 5.6f, on = u >= 5.6f && u < 8f;
                    float speed = !HazardLive ? 0 : on ? 720 : warn ? 120 + 500 * (u - 4.4f) / 1.2f : 90; // (dead still once the wave is cleared)
                    ang += speed * dt; drums[0].localRotation = Quaternion.Euler(0, 0, ang); drums[1].localRotation = Quaternion.Euler(0, 0, -ang);
                    Mats.SetAlpha(lampM, warn || on ? (Mathf.Sin(u * 18) > 0 ? 0.95f : 0.2f) : 0.08f);
                    if (u >= 4.4f && u - dt < 4.4f && HazardLive && Vector3.Distance(c, P.pos) < 24) { Sfx("saw", 0.5f); Sfx("charge", 0.3f); }
                    if (on && rand < 0.6f) SpawnParticles(c + V((rand - 0.5f) * 1.2f, 0.5f, (rand - 0.5f) * 1.2f) + q * V(0, 0, (rand - 0.5f) * LEN), 2, 5, sparkMat, 0.05f, 0.3f, 2);
                    if (!HazardLive) return;
                    // the drums always bite while they turn: a graze while they idle, a mauling once they've spun up
                    var lp = Quaternion.Inverse(q) * (P.pos - c);
                    if (Mathf.Abs(lp.x) < R * 2 + 0.2f && Mathf.Abs(lp.z) < LEN / 2 + 0.2f && P.pos.y < 1.6f)
                    {
                        if (on) P.pos += (q * V(-Mathf.Sign(lp.x) * 2.5f, 0, 0)) * dt; // dragged toward the seam
                        tick -= dt; if (tick <= 0) { tick = on ? 0.35f : 0.7f; DamagePlayer(on ? 12 : 5, c, false, true); if (!on) P.vel += V(0, 6, 0) + (q * V(Mathf.Sign(lp.x) * 6, 0, 0)); SpawnParticles(P.pos + V(0, 0.5f, 0), 10, 5, bloodMat, 0.1f, 0.5f); P.shake = Mathf.Max(P.shake, 0.35f); Sfx("sawHit", 0.6f); }
                    }
                    else tick = 0;
                    HazardHitEnemies(e => { var l = Quaternion.Inverse(q) * (e.pos - c); return Mathf.Abs(l.x) < R * 2 && Mathf.Abs(l.z) < LEN / 2 && e.pos.y < 1.2f; }, 8, e => Vector3.zero, 0.4f);
                });
            }
        }
        // a robot welding arm on a pedestal: idles welding (sparks at the torch), then its beacon turns red, it draws back
        // and sweeps the full arc over the belt beside it
        void MechWeldArms((Vector3 c, float face)[] arms)
        {
            var orange = Mats.Std(0xc86a20, 0.5f, 0.4f, 0, RustTex()); var dark = Mats.Std(0x2a2826, 0.5f, 0.7f, 0, RustTex()); var hose = Mats.Std(0x141414, 0.8f);
            const float REACH = 6.4f, ARC = 1.35f;
            for (int i = 0; i < arms.Length; i++)
            {
                var (c, face) = arms[i]; float off = i * 1.9f;
                Box(c.x, 0, c.z, 1.8f, 1.4f, 1.8f, dark); Ig(Deco(Geo.Box(1.9f, 0.08f, 1.9f), Mats.Std(0xd8a020, 0.7f), c.x, 1.44f, c.z));
                var turn = Mk.Piv(levelRoot, c.x, 1.45f, c.z); Mk.Msh(turn, Geo.Cyl(0.8f, 0.9f, 0.6f, 14), orange, 0, 0.3f, 0);
                var sh = Mk.Piv(turn, 0, 0.8f, 0); sh.SetRot(0.6f, 0, 0); Mk.Msh(sh, Geo.Cyl(0.35f, 0.35f, 0.9f, 12), dark, 0, 0, 0, 0, 0, Mathf.PI / 2);
                Mk.Msh(sh, Geo.Box(0.5f, 3.4f, 0.5f), orange, 0, 1.7f, 0);
                var el = Mk.Piv(sh, 0, 3.4f, 0); el.SetRot(1.5f, 0, 0); Mk.Msh(el, Geo.Cyl(0.28f, 0.28f, 0.7f, 12), dark, 0, 0, 0, 0, 0, Mathf.PI / 2);
                Mk.Msh(el, Geo.Box(0.38f, 3.0f, 0.38f), orange, 0, 1.5f, 0);
                var wr = Mk.Piv(el, 0, 3.0f, 0); Mk.Msh(wr, Geo.Cyl(0.2f, 0.26f, 0.5f, 10), dark, 0, 0.25f, 0); Mk.Msh(wr, Geo.Cyl(0.06f, 0.1f, 0.6f, 8), Mats.Std(0x9a9a9e, 0.2f, 0.9f), 0, 0.75f, 0);
                var tip = Mk.Piv(wr, 0, 1.05f, 0);
                var bm = Mats.Add(0xff3010, 0.1f, null, true, true); Mk.Msh(turn, Geo.Sphere(0.22f, 8, 6), bm, 0, 1.0f, -0.6f);
                SagCable(V(c.x - 0.8f, 1.4f, c.z), V(c.x, 2.6f, c.z), 0.3f, hose, 0.06f);
                foreach (var rr in turn.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                var arcM = HazMat(0xff4010, 0, false); var ring = Flat(Geo.Circle(REACH + 0.6f, 40), arcM, c.x, 0.05f, c.z);
                bool hitP = false; var hitE = new HashSet<Ent>();
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 7.5f; bool warn = u >= 4.2f && u < 5.4f, sweep = u >= 5.4f && u < 6.4f;
                    float yaw;
                    if (warn) yaw = face - ARC * Smoother((u - 4.2f) / 1.2f);            // draws back to one end of its arc
                    else if (sweep) yaw = face - ARC + 2 * ARC * Smoother((u - 5.4f) / 1.0f); // the sweep
                    else if (u >= 6.4f) yaw = Mathf.Lerp(face + ARC, face, Smoother((u - 6.4f) / 1.1f));
                    else yaw = face + Mathf.Sin(t * 1.3f + i) * 0.15f;                      // welding, fidgeting at its seam
                    turn.ry = yaw; sh.rx = sweep ? 0.75f : 0.6f + Mathf.Sin(t * 2 + i) * 0.05f; el.rx = sweep ? 1.7f : 1.5f + Mathf.Sin(t * 2.7f + i) * 0.06f; // (the torch dips to chest height for the sweep)
                    Mats.SetAlpha(bm, warn || sweep ? (Mathf.Sin(u * 20) > 0 ? 0.95f : 0.3f) : 0.12f); Mats.SetAlpha(arcM, warn ? 0.05f + 0.15f * (u - 4.2f) / 1.2f : sweep ? 0.18f : 0);
                    if (!warn && !sweep && rand < 0.35f) SpawnParticles(tip.t.position, 2, 3, sparkMat, 0.04f, 0.3f, 2); // welding sparks
                    if (u >= 4.2f && u - dt < 4.2f && HazardLive && Vector3.Distance(c, P.pos) < 22) Sfx("laserCharge", 0.4f);
                    if (u >= 5.4f && u - dt < 5.4f && HazardLive && Vector3.Distance(c, P.pos) < 22) Sfx("dashGo", 0.6f);
                    if (!sweep) { hitP = false; hitE.Clear(); return; }
                    if (rand < 0.5f) SpawnParticles(tip.t.position, 2, 4, sparkMat, 0.05f, 0.3f);
                    if (!HazardLive) return;
                    // anything inside the reach, under the arm's height, at the arm's current bearing
                    bool Hit(Vector3 p) { var o = p - c; o.y = 0; float d = o.magnitude; if (d > REACH + 0.6f || d < 1.0f || p.y > 3.2f) return false; float b = Mathf.Atan2(o.x, o.z), db = Mathf.Abs(Mathf.DeltaAngle(b * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg)); return db < 14; }
                    var side = V(Mathf.Cos(yaw), 0, -Mathf.Sin(yaw)); // the way the arm is moving
                    if (!hitP && Hit(P.pos)) { hitP = true; HazardHitPlayer(26, c + V(0, 2, 0), side * 16 + V(0, 7, 0)); Sfx("slam", 0.7f); }
                    HazardHitEnemies(e => Hit(e.pos) && hitE.Add(e), 26, e => side * 14 + V(0, 6, 0), 1);
                });
            }
        }
        void CathedralExtras()
        {
            var pipeM = Mats.Std(0x6a4a30, 0.6f, 0.6f, 0, RustTex()); var valve = Mats.Std(0xa02818, 0.6f, 0.4f); var cabM = Mats.Std(0x141414, 0.7f, 0.3f); var oil = Mats.Std(0x060505, 0.08f, 0.5f);
            // pipe runs along both walls with flanges and red valve wheels, a steam leak at a broken joint
            foreach (var sx in new[] { -1f, 1f })
                for (int r = 0; r < 3; r++)
                {
                    float y = 3 + r * 2.2f, x = sx * (60.8f - r * 0.5f), rad = 0.35f - r * 0.06f;
                    Deco(Geo.Cyl(rad, rad, 110, 10), pipeM, x, y, 0, Mathf.PI / 2, 0, 0);
                    for (int k = 0; k < 10; k++) { float z = -50 + k * 11; Ig(Deco(Geo.Cyl(rad + 0.08f, rad + 0.08f, 0.2f, 10), pipeM, x, y, z, Mathf.PI / 2, 0, 0)); if ((k + r) % 4 == 0) { Ig(Deco(Geo.Torus(0.32f, 0.04f, 12, 4), valve, x - sx * (rad + 0.25f), y, z + 0.5f, 0, Mathf.PI / 2, 0)); } }
                    if (r == 1) { var lp = V(x - sx * rad, y, sx * 14); anims.Add((t, dt) => { if (rand < 0.4f) SpawnParticles(lp, 1, 1.4f, steamMat, 0.2f, 1.0f, -2); }); }
                }
            // cable bundles slung between the nave columns
            for (int k = 0; k < 5; k++) foreach (var sx in new[] { -1f, 1f }) { float z = -45 + k * 18; SagCable(V(sx * 33, 17, z), V(sx * 33, 17, z + 18), 2.2f, cabM, 0.05f); SagCable(V(sx * 33, 16.4f, z), V(sx * 33, 16.4f, z + 18), 2.8f, cabM, 0.04f); }
            // oil pooled round the machines and the belt, scrap and drums
            foreach (var p in new[] { V(18, 0, 30), V(-24, 0, 12), V(28, 0, -30), V(42, 0, 4), V(-6, 0, -32), V(8, 0, 32), V(-40, 0, -20), V(40, 0, 36) }) Ig(Flat(Geo.Circle(1.0f + (Mathf.Abs(p.x) % 3) * 0.4f, 18), oil, p.x, 0.03f, p.z));
            foreach (var p in new[] { V(-44, 0, 40), V(44, 0, -40), V(-30, 0, -48), V(30, 0, 48) }) { for (int k = 0; k < 3; k++) Deco(Geo.Cyl(0.4f, 0.4f, 1.1f, 10), Mats.Std(new[] { 0x5a2a1a, 0x2a3a4a, 0x4a4a2a }[k], 0.7f, 0.4f, 0, RustTex()), p.x + (k - 1) * 0.9f, 0.55f, p.z + (k % 2) * 0.6f); SolidAABB(V(p.x - 1.4f, 0, p.z - 0.5f), V(p.x + 1.4f, 1.1f, p.z + 1.1f)); }
            // the hazards: steam vents in the aisles and round the nave, shredders under the slag buckets, welding arms by the belts
            MechSteamVents(new[] { V(-26, 0, -40), V(26, 0, -40), V(-26, 0, 40), V(26, 0, 40), V(-44, 0, -10), V(44, 0, 20), V(-44, 0, 22), V(44, 0, -22) });
            MechShredders(new[] { (V(-26, 0, 0), false), (V(26, 0, 0), false) });
            MechWeldArms(new[] { (V(13, 0, 29.2f), Mathf.PI), (V(13, 0, -29.2f), 0f), (V(-21.6f, 0, 14), Mathf.PI / 2), (V(41.6f, 0, -12), -Mathf.PI / 2) });
        }

        // ================= ASHFALL: blast-damaged facades =================
        // holes punched through a wall (shell hits): a dark cavity, a ragged rim of broken masonry, rebar, scorch above
        void FacadeDamage(float x, float z, float w, float d, float h, int seed)
        {
            var rng = new System.Random(seed * 3 + 1); float R() => (float)rng.NextDouble();
            var hole = Mats.Std(0x060504, 0.95f); var rim = Mats.Std(0x6a645a, 0.92f, 0, 0, GrimeTex()); var scorch = Mats.Trans(0x0a0806, 0.6f);
            int n = 1 + (int)(R() * 3);
            for (int k = 0; k < n; k++)
            {
                bool fz = R() < 0.6f; float s = R() < 0.5f ? -1 : 1; float hw = 1.6f + R() * 2.2f, hh = 1.4f + R() * 1.8f, y = 3 + R() * Mathf.Max(1, h - 7);
                float px = fz ? x + (R() - 0.5f) * (w - hw - 1) : x + s * (w / 2 + 0.03f), pz = fz ? z + s * (d / 2 + 0.03f) : z + (R() - 0.5f) * (d - hw - 1);
                float ry = fz ? 0 : Mathf.PI / 2;
                Ig(Deco(Geo.Box(hw, hh, 0.06f), hole, px, y, pz, 0, ry, 0));
                for (int j = 0; j < 7; j++) { float a = j * 0.9f + R(); var o = Quaternion.Euler(0, ry * Mathf.Rad2Deg, 0) * V(Mathf.Cos(a) * hw * 0.5f, Mathf.Sin(a) * hh * 0.5f, s * (fz ? 0.12f : 0) ); var g = Deco(Geo.Icosahedron(0.35f + R() * 0.25f), rim, px + o.x + (fz ? 0 : s * 0.12f), y + o.y, pz + o.z, R() * 3, R() * 3, 0); Ig(g); }
                for (int j = 0; j < 3; j++) Ig(Deco(Geo.Cyl(0.03f, 0.03f, 0.9f, 4), rebarM, px + (fz ? (j - 1) * hw * 0.3f : s * 0.3f), y - hh * 0.45f, pz + (fz ? s * 0.3f : (j - 1) * hw * 0.3f), fz ? s * 0.9f : 0, 0, fz ? 0 : -s * 0.9f));
                Ig(Deco(Geo.Box(hw * 0.8f, hh * 1.6f, 0.02f), scorch, px + (fz ? 0 : s * 0.02f), y + hh * 1.1f, pz + (fz ? s * 0.02f : 0), 0, ry, 0)); // smoke-blackened above it
                Debris(px + (fz ? 0 : s * 2.2f), pz + (fz ? s * 2.2f : 0), 1.2f + R() * 0.6f); // what came down
            }
        }
    }
}
