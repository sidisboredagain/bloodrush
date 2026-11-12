using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    // One mechanic per level, so each arena plays differently:
    //  1 THE PIT          lava geysers - erupt from the vents, hurt, and launch whoever stands on them
    //  2 BONE CORRIDORS   spike traps - plates flash red, then bones punch up through them
    //  3 FURNACE TERRACES flame sweep - a fire arm circles the altar, jump it
    //  4 CHAIN TOWERS     wrecking balls - spiked balls swing on chains between the towers
    //  5 THE CRUCIBLE     meteor rain - burning rocks fall where the red circles mark
    //  6 GATES OF DAWN    updrafts - pillars of light carry you (and enemies) into the air
    //  7 CLOISTER         the great bell - each toll sends a shockwave rolling out, jump it
    //  8 SKY BRIDGES      low gravity - everything jumps higher and falls slower
    //  9 THE CHOIR        sanctuaries - wandering circles of light that heal you
    // 10 THE SANCTUM      halo lances - beams of light sweep the floor during the fight
    // Everything visible is built with the level (so the editor preview shows it); the motion and the
    // damage run in anims, during play only.
    public partial class Game
    {
        static float gravK = 1; // low-gravity levels
        bool HazardLive => started && P != null && !P.dead && !won && !WARP.active && trapsArmed && !CineLock; // (no enemies left: the traps are harmless)
        bool PlayerIn(Vector3 c, float r, float y0, float y1) => Hypot(P.pos.x - c.x, P.pos.z - c.z) < r && P.pos.y >= y0 && P.pos.y <= y1;
        void HazardHitPlayer(float dmg, Vector3 from, Vector3 push)
        {
            if (!HazardLive || P.iframes > 0) return;
            DamagePlayer(dmg, from, false, true); P.vel += push;
            if (push.y > 0) { P.onGround = false; P.coyote = 0; }
        }
        // enemies caught by a hazard (bosses shrug them off); a per-enemy cooldown stops it hitting every frame
        readonly Dictionary<Ent, float> hazCd = new Dictionary<Ent, float>();
        void HazardHitEnemies(System.Func<Ent, bool> inside, float dmg, System.Func<Ent, Vector3> push, float cd = 0.8f)
        {
            if (!HazardLive || !Auth) return; // co-op: the host's hazards hurt enemies; every machine's hurt only its own player
            if (Act == "ruin" || Act == "limbo") return; // in Dead Earth and Purgatory the traps only ever hurt the player: trains, presses, cranes, debris pass through enemies
            foreach (var e in enemies.ToArray())
            {
                if (e.dead || e.type == "boss" || e.spawnT > 0 || !inside(e)) continue;
                if (hazCd.TryGetValue(e, out var t) && time < t) continue;
                hazCd[e] = time + cd;
                var pv = push(e); e.vel += pv; e.stun = Mathf.Max(e.stun, 0.4f); e.leaping = false;
                DamageEnemy(e, dmg, new Hit { point = BodyPoint(e), dir = pv.sqrMagnitude > 0 ? pv.normalized : (Vector3?)null, kb = 0.01f });
            }
        }
        Material HazMat(int color, float alpha, bool additive)
        {
            var m = additive ? Mats.Add(color, alpha, null, true, true) : Mats.Trans(color, alpha, true);
            return m;
        }
        GameObject Flat(Mesh mesh, Material m, float x, float y, float z) { var g = Deco(mesh, m, x, y, z, -Mathf.PI / 2); g.layer = Mk.LayerIgnore; return g; }

        // ---------- 1. lava geysers ----------
        void MechGeysers(Vector3[] vents)
        {
            for (int i = 0; i < vents.Length; i++)
            {
                var c = vents[i]; float off = i * 1.7f;
                Deco(Geo.Torus(2.2f, 0.35f, 24, 6), M.metal, c.x, 0.15f, c.z, -Mathf.PI / 2);
                Deco(Geo.Cyl(1.9f, 2.1f, 0.25f, 16), M.pool, c.x, 0.06f, c.z);
                var warnM = HazMat(0xff3000, 0, false); Flat(Geo.Circle(3.2f, 28), warnM, c.x, 0.08f, c.z);
                var colM = HazMat(0xff7a20, 0.85f, true); var col = Deco(Geo.Cyl(1.5f, 2.3f, 1, 14, true), colM, c.x, 0, c.z); col.layer = Mk.LayerIgnore;
                var core = Deco(Geo.Cyl(0.7f, 1.2f, 1, 10, true), HazMat(0xffe0a0, 0.9f, true), c.x, 0, c.z); core.layer = Mk.LayerIgnore;
                var glow = Mk.Glow(levelRoot, 0xff5010, 7, 0.0f); glow.transform.localPosition = V(c.x, 2, c.z); var glowM = glow.GetComponent<Renderer>().sharedMaterial;
                col.SetActive(false); core.SetActive(false);
                bool hitP = false; var hitE = new HashSet<Ent>();
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 6.5f;
                    bool warn = u < 1.3f, erupt = u >= 1.3f && u < 2.9f;
                    Mats.SetAlpha(warnM, warn ? 0.25f + 0.25f * Mathf.Sin(u * 22) : 0);
                    Mats.SetAlpha(glowM, warn ? u / 1.3f * 0.6f : erupt ? 0.9f : 0.15f);
                    if (warn && rand < 0.4f) SpawnParticles(c + V(0, 0.3f, 0), 1, 3, emberMat, 0.08f, 0.6f);
                    col.SetActive(erupt); core.SetActive(erupt);
                    if (!erupt) { hitP = false; hitE.Clear(); return; }
                    float k = Smoother((u - 1.3f) / 0.2f) * Smoother((2.9f - u) / 0.35f), h = 24 * k;
                    col.transform.localScale = V(1 + Mathf.Sin(t * 30) * 0.06f, Mathf.Max(0.01f, h), 1 + Mathf.Cos(t * 27) * 0.06f); col.transform.localPosition = V(c.x, h / 2, c.z);
                    core.transform.localScale = V(1, Mathf.Max(0.01f, h * 0.9f), 1); core.transform.localPosition = V(c.x, h * 0.45f, c.z);
                    if (rand < 0.7f) SpawnParticles(c + V(0, h * rand, 0), 2, 6, emberMat, 0.12f, 0.9f, -4);
                    if (!HazardLive) return;
                    if (PlayerIn(c, 2.6f, -1, h)) { if (!hitP) { hitP = true; HazardHitPlayer(14, c, Vector3.zero); } P.vel.y = Mathf.Max(P.vel.y, 24); P.onGround = false; } // rides the geyser up
                    HazardHitEnemies(e => Hypot(e.pos.x - c.x, e.pos.z - c.z) < 2.6f && !hitE.Contains(e) && hitE.Add(e), 3, e => V(0, 22, 0));
                });
            }
        }

        // ---------- 2. spike traps ----------
        void MechSpikes((float x, float z, float w, float d)[] plates, Material spikes = null)
        {
            var spikeM = spikes ?? spikeMat;
            for (int i = 0; i < plates.Length; i++)
            {
                var pl = plates[i]; float off = (i % 2) * 2.25f;
                Deco(Geo.Box(pl.w, 0.12f, pl.d), M.metal, pl.x, 0.06f, pl.z);
                var warnM = HazMat(0xff2000, 0, false); Flat(Geo.Plane(pl.w - 0.4f, pl.d - 0.4f), warnM, pl.x, 0.14f, pl.z);
                var sp = Mk.Piv(levelRoot, pl.x, 0.1f, pl.z); sp.t.localScale = V(1, 0.05f, 1);
                int nx = Mathf.Max(2, Mathf.RoundToInt(pl.w / 1.6f)), nz = Mathf.Max(2, Mathf.RoundToInt(pl.d / 1.6f));
                for (int a = 0; a < nx; a++) for (int b = 0; b < nz; b++)
                        Mk.Msh(sp.t, Geo.Cone(0.32f, 1.8f, 5), spikeM, (a + 0.5f) / nx * (pl.w - 0.6f) - (pl.w - 0.6f) / 2, 0.9f, (b + 0.5f) / nz * (pl.d - 0.6f) - (pl.d - 0.6f) / 2);
                bool hitP = false; var hitE = new HashSet<Ent>();
                var c = V(pl.x, 0, pl.z);
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 4.5f;
                    Mats.SetAlpha(warnM, u < 1 ? 0.3f + 0.3f * Mathf.Sin(u * 28) : 0);
                    float up = u < 1 ? 0 : u < 1.12f ? (u - 1) / 0.12f : u < 2.1f ? 1 : u < 2.4f ? 1 - (u - 2.1f) / 0.3f : 0;
                    sp.t.localScale = V(1, Mathf.Max(0.05f, up), 1);
                    if (u >= 1 && u < 1.05f && rand < 0.5f) SpawnParticles(c + V(0, 0.3f, 0), 3, 4, dustMat, 0.1f, 0.4f);
                    if (up < 0.6f) { hitP = false; hitE.Clear(); return; }
                    if (!HazardLive) return;
                    if (!hitP && Mathf.Abs(P.pos.x - pl.x) < pl.w / 2 && Mathf.Abs(P.pos.z - pl.z) < pl.d / 2 && P.pos.y < 1.7f) { hitP = true; HazardHitPlayer(18, c, V(0, 9, 0)); }
                    HazardHitEnemies(e => Mathf.Abs(e.pos.x - pl.x) < pl.w / 2 && Mathf.Abs(e.pos.z - pl.z) < pl.d / 2 && e.pos.y < 1.5f && hitE.Add(e), 4, e => V(0, 8, 0));
                });
            }
        }

        // ---------- 3. flame sweep ----------
        void MechFlameSweep(float r0, float r1, float speed)
        {
            var piv = Mk.Piv(levelRoot, 0, 0, 0);
            var fireM = HazMat(0xff6a18, 0.8f, true); var hotM = HazMat(0xffe2a0, 0.9f, true);
            var jets = new List<Transform>();
            foreach (var s in new[] { 1, -1 })
            {
                float mid = (r0 + r1) / 2 * s, len = r1 - r0;
                Mk.Msh(piv.t, Geo.Box(len, 0.5f, 0.7f), M.metal, mid, 0.45f, 0);
                Mk.Msh(piv.t, Geo.Box(len, 0.08f, 0.2f), M.trim, mid, 0.72f, 0);
                for (int i = 0; i <= 8; i++)
                {
                    float x = (r0 + len * i / 8f) * s;
                    jets.Add(Mk.Msh(piv.t, Geo.Cone(0.55f, 1, 8), fireM, x, 0.7f, 0).transform);
                    jets.Add(Mk.Msh(piv.t, Geo.Cone(0.25f, 1, 6), hotM, x, 0.7f, 0).transform);
                }
            }
            Mk.Msh(piv.t, Geo.Cyl(1.2f, 1.4f, 1.2f, 12), M.metal, 0, 0.6f, 0); // hub, hidden under the altar
            foreach (var j in jets) j.gameObject.layer = Mk.LayerIgnore;
            trapAnims.Add((t, dt) =>
            {
                float ang = t * speed; piv.SetRot(0, ang, 0);
                for (int i = 0; i < jets.Count; i++) { float h = 1.8f + Mathf.Sin(t * 19 + i * 1.3f) * 0.5f + Mathf.Sin(t * 41 + i) * 0.2f; jets[i].localScale = V(1, h * (i % 2 == 0 ? 1 : 0.7f), 1); var p = jets[i].localPosition; p.y = 0.7f + h * 0.5f; jets[i].localPosition = p; }
                if (rand < 0.6f) { float rr = Mathf.Lerp(r0, r1, rand) * (rand < 0.5f ? 1 : -1); var q = Quaternion.Euler(0, ang * Mathf.Rad2Deg, 0) * V(rr, 2.2f, 0); SpawnParticles(q, 1, 2, emberMat, 0.09f, 0.7f, -3); }
                if (!HazardLive) return;
                bool On(Vector3 p, float y)
                {
                    float r = Hypot(p.x, p.z); if (r < r0 - 0.5f || r > r1 + 0.5f || y > 2.6f) return false;
                    var arm = Quaternion.Euler(0, ang * Mathf.Rad2Deg, 0) * Vector3.right; var flat = V(p.x, 0, p.z);
                    return Vector3.Cross(arm, flat).magnitude < 1.3f; // distance from the arm's line
                }
                Vector3 Push(Vector3 p) { var arm = Quaternion.Euler(0, ang * Mathf.Rad2Deg, 0) * Vector3.right; var tan = Vector3.Cross(Vector3.up, arm) * Mathf.Sign(speed) * Mathf.Sign(Vector3.Dot(arm, p)); return tan * 12 + V(0, 7, 0); }
                if (On(P.pos, P.pos.y)) HazardHitPlayer(15, V(0, 1, 0), Push(P.pos));
                HazardHitEnemies(e => On(e.pos, e.pos.y), 2, e => Push(e.pos), 0.6f);
            });
        }

        // ---------- 4. wrecking balls ----------
        void MechWreckingBalls(int n, float radius, float anchorY, float len, float amp)
        {
            var chainM = G(Mats.Std(0x2a2a30, 0.4f, 0.7f)); var link = Geo.Torus(0.26f, 0.08f, 12, 5);
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.PI / 4 + i * Mathf.PI * 2 / n, ph = i * 1.3f;
                var anchor = V(Mathf.Cos(a) * radius, anchorY, Mathf.Sin(a) * radius); var radial = V(Mathf.Cos(a), 0, Mathf.Sin(a));
                var piv = new GameObject("wreck").transform; piv.SetParent(levelRoot, false); piv.localPosition = anchor;
                // a real chain of links (each turned 90° from the last) down to the ball...
                int li = 0; for (float y = 0.1f; y < len - 1.6f; y += 0.62f, li++) { var l = Mk.Msh(piv, link, chainM, 0, -y, 0).transform; l.localRotation = Quaternion.Euler(0, (li & 1) * 90, 90); l.localScale = V(1, 1.5f, 1); }
                // ...gripped by a winged demon hovering at the top, beating its wings as it swings the ball
                ChainDemon(anchor, ph, radial, amp);
                var ball = Mk.Msh(piv, Geo.Sphere(2.1f, 16, 12), M.metal, 0, -len, 0).transform;
                for (int k = 0; k < 10; k++) { var d = Random.onUnitSphere; Mk.Msh(ball, Geo.Cone(0.35f, 1.2f, 5), M.bone, d.x * 2, d.y * 2, d.z * 2).transform.localRotation = Quaternion.FromToRotation(Vector3.up, d); }
                var glow = Mk.Glow(ball, THEME.holy ? 0xfff0c0 : 0xff4000, 3, 0.25f);
                Vector3 last = ball.position;
                piv.localRotation = Quaternion.AngleAxis(amp * Mathf.Rad2Deg * Mathf.Sin(ph), radial);
                trapAnims.Add((t, dt) =>
                {
                    piv.localRotation = Quaternion.AngleAxis(amp * Mathf.Rad2Deg * Mathf.Sin(t * 0.85f + ph), radial);
                    var bp = ball.position; var vel = dt > 0 ? (bp - last) / dt : Vector3.zero; last = bp;
                    if (!HazardLive) return;
                    var push = vel.normalized * 18 + V(0, 7, 0);
                    if (Vector3.Distance(bp, ChestPos) < 2.9f) HazardHitPlayer(22, bp, push);
                    HazardHitEnemies(e => Vector3.Distance(bp, BodyPoint(e)) < 2.4f + e.hitR, 4, e => push * 1.3f);
                });
            }
        }

        // ---------- the wyverns that carry the wrecking balls (decoration, not enemies) ----------
        // a mesh stood along the segment a-b in a parent's local space (meshes are built along +y)
        static Transform LimbSeg(Transform parent, Mesh m, Material mat, Vector3 a, Vector3 b)
        {
            var g = Mk.Msh(parent, m, mat, (a.x + b.x) / 2, (a.y + b.y) / 2, (a.z + b.z) / 2).transform;
            var d = b - a; g.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized); g.localScale = V(1, d.magnitude, 1); return g;
        }
        // A horned wyvern hovering over a wrecking ball's anchor, hind claws locked on the top of the chain: S-curved neck,
        // a head that turns to watch you and opens its jaws to roar embers, glowing ember veins, ridge spines, a whipping
        // spade tail and huge bat wings with finger bones and scalloped membranes that beat with a two-joint flap.
        void ChainDemon(Vector3 at, float ph, Vector3 radial, float amp)
        {
            var scale = Mats.Std(0x45201a, 0.38f, 0.45f); var belly = Mats.Std(0x7a4c32, 0.55f, 0.1f); var hornM = M.bone;
            var memb = Mats.Std(0x8a1a12, 0.6f, 0.05f, 0, null, true); var vein = Mats.Add(0xff5a14, 0.9f, null, true, true);
            var unit = Geo.Cyl(1, 1, 1, 16); var unitCone = Geo.Cone(1, 1, 12);
            const float S = 2.2f;
            var root = Mk.Piv(levelRoot, at.x, at.y, at.z); var rt = root.t; rt.localScale = Vector3.one * S;
            root.SetRot(0, Mathf.Atan2(-at.x, -at.z), 0);
            var body = Mk.Piv(rt, 0, 0, 0); var b = body.t;
            Transform Seg(Transform p, Material m, Vector3 a, Vector3 c, float r0) { var s = LimbSeg(p, unit, m, a, c); var ls = s.localScale; s.localScale = V(r0, ls.y, r0); return s; }
            Transform Spike(Transform p, Material m, Vector3 a, Vector3 c, float r0) { var s = LimbSeg(p, unitCone, m, a, c); var ls = s.localScale; s.localScale = V(r0, ls.y, r0); return s; }
            // torso, belly plates, ridge spines, glowing veins
            Mk.Msh(b, Geo.Sphere(1, 28, 20), scale, 0, 1.5f, -0.1f).transform.localScale = V(0.42f, 0.42f, 1.4f);  // lean, tapering body
            Mk.Msh(b, Geo.Sphere(1, 24, 18), scale, 0, 1.72f, 0.75f).transform.localScale = V(0.47f, 0.46f, 0.62f);  // chest
            for (int k = 0; k < 6; k++) Mk.Msh(b, Geo.Sphere(1, 16, 10), belly, 0, 1.2f + k * 0.02f, 0.9f - k * 0.34f).transform.localScale = V(0.26f - k * 0.025f, 0.08f, 0.17f); // belly scales
            for (int k = 0; k < 9; k++) Spike(b, hornM, V(0, 2.1f - k * 0.07f, 1.0f - k * 0.3f), V(0, 2.4f - k * 0.08f, 0.8f - k * 0.34f), 0.05f);
            foreach (var sx in new[] { -1f, 1f }) { Seg(b, vein, V(sx * 0.4f, 1.6f, 0.7f), V(sx * 0.36f, 1.5f, -0.9f), 0.016f); Seg(b, vein, V(sx * 0.33f, 1.28f, 0.5f), V(sx * 0.28f, 1.3f, -1.1f), 0.013f); }
            // hind legs clutching the chain (they hang from the hips and meet at the chain top, the local origin)
            foreach (var sx in new[] { -1f, 1f })
            {
                var hip = V(sx * 0.3f, 1.25f, -0.3f); var knee = V(sx * 0.4f, 0.75f, 0.3f); var ankle = V(sx * 0.14f, 0.25f, 0.05f);
                Seg(rt, scale, hip, knee, 0.11f); Seg(rt, scale, knee, ankle, 0.07f);
                Mk.Msh(rt, Geo.Sphere(0.12f, 14, 10), scale, hip.x, hip.y, hip.z); Mk.Msh(rt, Geo.Sphere(0.08f, 12, 8), scale, knee.x, knee.y, knee.z); // smooth joints
                for (int c = 0; c < 3; c++) Spike(rt, hornM, ankle, V(sx * 0.03f + (c - 1) * 0.06f, -0.08f, (c - 1) * 0.08f), 0.035f);
            }
            // neck (animated), head with horns and a hinged jaw
            var neck = new List<Transform>(); var neckBase = new List<Vector3>();
            const int NK = 14;
            for (int k = 0; k < NK; k++)
            {
                float u = k / (NK - 1f); var p = V(0, 1.85f + Mathf.Sin(u * Mathf.PI * 0.9f) * 1.15f, 1.0f + u * 1.45f);
                var g = Mk.Msh(b, Geo.Sphere(0.24f - u * 0.11f, 16, 12), scale, p.x, p.y, p.z).transform; neck.Add(g); neckBase.Add(p);
                if (k % 2 == 0) Spike(g, hornM, V(0, 0.16f - u * 0.06f, -0.03f), V(0, 0.36f - u * 0.12f, -0.15f), 0.035f);
            }
            var headP = Mk.Piv(b, 0, 3.1f, 2.5f); var h = headP.t; h.localScale = Vector3.one * 0.8f; // a narrow, sleek head
            Mk.Msh(h, Geo.Sphere(1, 22, 16), scale, 0, 0, 0).transform.localScale = V(0.3f, 0.27f, 0.48f);           // cranium
            var snout = Mk.Msh(h, Geo.Cone(0.25f, 1.0f, 14), scale, 0, -0.02f, 0.62f, Mathf.PI / 2, 0, 0); snout.transform.localScale = V(0.9f, 1, 0.62f);
            foreach (var sx in new[] { -1f, 1f })
            {
                Mk.Msh(h, Geo.Box(0.16f, 0.07f, 0.34f), scale, sx * 0.17f, 0.17f, 0.25f, -0.25f, sx * 0.3f, 0);            // brow ridge
                Mk.Glow(h, 0xff4010, 0.42f, 1f).transform.localPosition = V(sx * 0.19f, 0.09f, 0.33f);                  // eyes
                Mk.Msh(h, Geo.Sphere(0.05f, 8, 6), vein, sx * 0.19f, 0.09f, 0.33f);
                // swept-back horns in three curving segments, and cheek spikes
                Vector3 h0 = V(sx * 0.2f, 0.22f, -0.12f), h1 = V(sx * 0.34f, 0.42f, -0.5f), h2 = V(sx * 0.38f, 0.48f, -0.95f), h3 = V(sx * 0.3f, 0.36f, -1.35f);
                Seg(h, hornM, h0, h1, 0.085f); Seg(h, hornM, h1, h2, 0.065f); Spike(h, hornM, h2, h3, 0.055f);
                Spike(h, hornM, V(sx * 0.28f, -0.05f, -0.05f), V(sx * 0.48f, -0.08f, -0.42f), 0.045f);
                Spike(h, hornM, V(sx * 0.22f, -0.12f, 0.15f), V(sx * 0.36f, -0.2f, -0.15f), 0.035f);
                for (int k = 0; k < 4; k++) Spike(h, hornM, V(sx * 0.12f, -0.12f, 0.45f + k * 0.13f), V(sx * 0.12f, -0.22f, 0.45f + k * 0.13f), 0.02f); // upper teeth
                Mk.Glow(h, 0xff7020, 0.25f, 0.8f).transform.localPosition = V(sx * 0.07f, 0.06f, 1.02f);                // smouldering nostrils
            }
            for (int k = 0; k < 4; k++) Spike(h, hornM, V(0, 0.3f, -0.1f - k * 0.18f), V(0, 0.55f - k * 0.05f, -0.3f - k * 0.2f), 0.045f); // frill
            var jaw = Mk.Piv(h, 0, -0.16f, 0.12f); var jt = jaw.t;
            var lower = Mk.Msh(jt, Geo.Cone(0.2f, 0.88f, 14), scale, 0, -0.03f, 0.42f, Mathf.PI / 2, 0, 0); lower.transform.localScale = V(0.85f, 1, 0.4f);
            foreach (var sx in new[] { -1f, 1f }) for (int k = 0; k < 3; k++) Spike(jt, hornM, V(sx * 0.1f, 0.0f, 0.3f + k * 0.13f), V(sx * 0.1f, 0.1f, 0.3f + k * 0.13f), 0.018f);
            var mouthGlow = Mk.Glow(h, 0xff6a10, 0.9f, 0); mouthGlow.transform.localPosition = V(0, -0.12f, 0.5f); var mouthM = mouthGlow.GetComponent<Renderer>().sharedMaterial;
            // tail
            var tail = new List<Transform>();
            const int TK = 22;
            for (int k = 0; k < TK; k++)
            {
                float u = k / (TK - 1f); var g = Mk.Msh(b, Geo.Sphere(0.22f - u * 0.18f, 14, 10), scale, 0, 1.3f - u * 1.5f, -1.4f - u * 3.4f).transform; tail.Add(g);
                if (k % 3 == 0 && k < TK - 3) Spike(g, hornM, V(0, 0.13f - u * 0.08f, 0), V(0, 0.3f - u * 0.16f, -0.14f), 0.035f);
            }
            foreach (var sx in new[] { -1f, 1f }) { var sp = Mk.Msh(tail[TK - 1], Geo.Cone(0.18f, 0.5f, 10), hornM, sx * 0.1f, 0, -0.18f, -Mathf.PI / 2 + 0.2f, 0, sx * 0.6f); sp.transform.localScale = V(1, 1, 0.22f); }
            // wings: an inner section on the shoulder, an outer section (fingers + membrane) on the wrist
            var wings = new List<(N sh, N wr, float sx)>();
            foreach (var sx in new[] { -1f, 1f })
            {
                var sh = Mk.Piv(b, sx * 0.36f, 1.95f, 0.6f); const float W = 2.2f;
                Seg(sh.t, scale, V(0, 0, 0), V(sx * W, 0.05f, 0.2f), 0.065f); Mk.Msh(sh.t, Geo.Sphere(0.1f, 14, 10), scale, 0, 0, 0);                                             // upper wing bone
                var inner = GeoX.ShapeMB(new GeoX.Shape().MoveTo(0, 0.1f).LineTo(sx * W, 0.25f).LineTo(sx * W * 0.96f, -1.75f).QuadTo(sx * W * 0.5f, -1.0f, 0, -1.25f).LineTo(0, 0.1f)).Build();
                Mk.Msh(sh.t, inner, memb, 0, 0, 0, Mathf.PI / 2, 0, 0);
                var wr = Mk.Piv(sh.t, sx * W, 0.05f, 0.2f);
                float[] fl = { 2.9f, 3.2f, 2.8f, 2.1f }, fa = { 0.25f, -0.25f, -0.75f, -1.25f };
                var tips = new Vector2[4];
                for (int f = 0; f < 4; f++) { tips[f] = new Vector2(Mathf.Cos(fa[f]) * fl[f] * sx, Mathf.Sin(fa[f]) * fl[f]); Seg(wr.t, scale, V(0, 0, 0), V(tips[f].x, 0, tips[f].y), 0.034f - f * 0.004f); Spike(wr.t, hornM, V(tips[f].x, 0, tips[f].y), V(tips[f].x * 1.08f, 0, tips[f].y * 1.08f), 0.03f); }
                Spike(wr.t, hornM, V(0, 0, 0), V(sx * 0.25f, 0.05f, 0.42f), 0.05f);                                     // thumb claw
                var outer = new GeoX.Shape().MoveTo(0, 0.12f).LineTo(tips[0].x, tips[0].y);
                for (int f = 0; f < 3; f++) { var m = (tips[f] + tips[f + 1]) * 0.5f * 0.62f; outer.QuadTo(m.x, m.y, tips[f + 1].x, tips[f + 1].y); }
                outer.QuadTo(tips[3].x * 0.35f, tips[3].y * 0.7f, -sx * 0.09f, -1.8f).LineTo(0, 0.12f);
                Mk.Msh(wr.t, GeoX.ShapeMB(outer).Build(), memb, 0, 0, 0, Mathf.PI / 2, 0, 0);
                for (int f = 0; f < 4; f++) Seg(wr.t, vein, V(tips[f].x * 0.12f, 0.012f, tips[f].y * 0.12f), V(tips[f].x * 0.8f, 0.012f, tips[f].y * 0.8f), 0.02f); // glowing veins
                Seg(sh.t, vein, V(sx * 0.3f, 0.012f, -0.3f), V(sx * W * 0.85f, 0.012f, -0.9f), 0.018f);
                wings.Add((sh, wr, sx));
            }
            // lit from below by its own fire, with a burning halo behind so it stands out against the dark sky
            var lgo = new GameObject("dragon light"); lgo.transform.SetParent(b, false); lgo.transform.localPosition = V(0, 0.2f, 1.2f);
            var lt = lgo.AddComponent<Light>(); lt.type = LightType.Point; lt.color = new Color(1f, 0.45f, 0.18f); lt.range = 16; lt.intensity = 4;
            var halo = Mk.Glow(b, 0xff4a10, 9, 0.28f); halo.transform.localPosition = V(0, 2, -1.5f);
            Mk.SetLayerRecursive(rt.gameObject, Mk.LayerIgnore);
            float roarT = 3 + ph * 2;
            anims.Add((t, dt) =>
            {
                float swing = amp * Mathf.Sin(t * 0.85f + ph), flap = Mathf.Sin(t * 4.2f + ph), lag = Mathf.Sin(t * 4.2f + ph - 1.0f);
                body.pos = V(0, -flap * 0.05f, 0); body.SetRot(-swing * 0.45f, 0, 0);
                foreach (var w in wings)
                {
                    w.sh.SetRot(0, w.sx * (-0.15f + 0.2f * lag), w.sx * (0.12f + 0.62f * flap));
                    w.wr.SetRot(0, w.sx * (0.25f - 0.3f * Mathf.Max(0, -lag)), w.sx * 0.45f * lag);
                }
                for (int k = 0; k < neck.Count; k++) { float u = k / (neck.Count - 1f); neck[k].localPosition = neckBase[k] + V(Mathf.Sin(t * 1.2f + ph + u * 2.3f) * 0.35f * u, Mathf.Sin(t * 2.1f + ph + u * 1.5f) * 0.15f * u, 0); }
                for (int k = 0; k < tail.Count; k++) { float u = k / (tail.Count - 1f); tail[k].localPosition = V(Mathf.Sin(t * 1.6f + ph - u * 4.5f) * 0.6f * u * u, 1.3f - u * 1.5f + Mathf.Sin(t * 1.1f + ph - u * 3.6f) * 0.22f * u, -1.4f - u * 3.4f); }
                // the head follows you, and every few seconds it roars, jaws wide, spitting embers
                var toP = rt.InverseTransformPoint(P != null ? P.pos : Vector3.zero) - V(0, 3, 2.5f);
                float yaw = Mathf.Clamp(Mathf.Atan2(toP.x, toP.z), -0.8f, 0.8f), pitch = Mathf.Clamp(-Mathf.Atan2(toP.y, Hypot(toP.x, toP.z)), -0.2f, 0.9f);
                var nl = neckBase[neck.Count - 1] + V(Mathf.Sin(t * 1.2f + ph + 2.3f) * 0.35f, 0.15f, 0.15f);
                headP.pos = nl; headP.SetRot(pitch * 0.8f, yaw, Mathf.Sin(t * 0.9f + ph) * 0.1f);
                roarT -= dt; float open = roarT < 1.2f && roarT > 0 ? Mathf.Sin(Mathf.Clamp01((1.2f - roarT) / 1.2f) * Mathf.PI) : 0;
                if (roarT <= 0) roarT = 5 + rand * 4;
                jaw.SetRot(0.55f * open + 0.04f * Mathf.Sin(t * 3), 0, 0); Mats.SetAlpha(mouthM, open); lt.intensity = 4 + open * 5;
                if (open > 0.4f && rand < 0.8f)
                {
                    var mp = h.TransformPoint(V(0, -0.1f, 1.0f)); var fwd = h.forward;
                    particles.Add(new Pt { pos = mp, scale = Vector3.one * (0.2f + rand * 0.25f), mat = rand < 0.7f ? emberMat : sparkMat, v = fwd * (10 + rand * 6) + Random.insideUnitSphere * 2.5f, life = 0.9f, grav = -2 });
                }
            });
        }

        // ---------- 5. meteor rain ----------
        class Meteor { public Vector3 at; public float t; public Transform rock, warn; public Material warnM; }
        void MechMeteors()
        {
            // scorched craters from earlier impacts (visible in the editor preview as well)
            var scorch = HazMat(0x120806, 0.75f, false);
            for (int i = 0; i < 9; i++) { float a = i * 2.4f, r = 12 + (i * 7) % 40; Flat(Geo.Circle(2.5f + (i % 3), 20), scorch, Mathf.Cos(a) * r, 0.05f, Mathf.Sin(a) * r); }
            var live = new List<Meteor>(); float next = 3;
            trapAnims.Add((t, dt) =>
            {
                if (HazardLive && phase == "fight")
                {
                    next -= dt;
                    if (next <= 0)
                    {
                        next = 2.2f + rand * 1.4f;
                        var at = rand < 0.6f ? V(P.pos.x + (rand - 0.5f) * 18, 0, P.pos.z + (rand - 0.5f) * 18) : V((rand * 2 - 1) * (LB - 8), 0, (rand * 2 - 1) * (LB - 8));
                        at.x = clamp(at.x, -(LB - 6), LB - 6); at.z = clamp(at.z, -(LB - 6), LB - 6);
                        var m = new Meteor { at = at, warnM = HazMat(0xff2000, 0.5f, false) };
                        m.warn = Flat(Geo.Circle(4.5f, 28), m.warnM, at.x, 0.07f, at.z).transform; m.warn.localScale = Vector3.one * 0.05f;
                        var rock = new GameObject("meteor").transform; rock.SetParent(levelRoot, false); rock.localPosition = at + V(8, 70, -6);
                        Mk.Msh(rock, Geo.Icosahedron(1.3f), M.pool); Mk.Glow(rock, 0xff6010, 7, 0.9f); m.rock = rock;
                        live.Add(m);
                    }
                }
                for (int i = live.Count - 1; i >= 0; i--)
                {
                    var m = live[i]; m.t += dt;
                    m.warn.localScale = Vector3.one * Mathf.Max(0.05f, Smoother(m.t / 1.6f)); Mats.SetAlpha(m.warnM, 0.35f + 0.25f * Mathf.Sin(m.t * 20));
                    float f = Mathf.Clamp01((m.t - 0.9f) / 0.7f); m.rock.localPosition = Vector3.Lerp(m.at + V(8, 70, -6), m.at + V(0, 1, 0), f * f);
                    m.rock.Rotate(200 * dt, 140 * dt, 0);
                    if (f > 0 && rand < 0.8f) SpawnParticles(m.rock.position, 2, 2, emberMat, 0.18f, 0.6f, -2);
                    if (f < 1) continue;
                    // impact
                    var p = m.at;
                    Blast(p + V(0, 1, 0), 5, 0xff6010); ShockRing(p, 6, 0xff4000, 0.45f);
                    SpawnParticles(p + V(0, 0.5f, 0), 26, 12, emberMat, 0.14f, 1.0f); SpawnParticles(p + V(0, 0.5f, 0), 14, 6, ashMat, 0.3f, 1.4f, -2);
                    Sfx("boom"); if (started) P.shake = Mathf.Max(P.shake, Mathf.Clamp01(1 - Vector3.Distance(P.pos, p) / 30) * 0.6f);
                    if (PlayerIn(p, 4.5f, -1, 4)) { var d = (P.pos - p); d.y = 0; HazardHitPlayer(18, p, d.normalized * 10 + V(0, 8, 0)); }
                    HazardHitEnemies(e => Hypot(e.pos.x - p.x, e.pos.z - p.z) < 4.5f, 4, e => { var d = e.pos - p; d.y = 0; return d.normalized * 10 + V(0, 9, 0); }, 0.1f);
                    Mk.DestroyTree(m.rock.gameObject); Mk.DestroyTree(m.warn.gameObject); live.RemoveAt(i);
                }
            });
        }

        // ---------- 5b. tower eruptions (Crucible): a tower's runes flare, then a ring of fire rolls out from its foot ----------
        void MechEruptions(Vector3[] towers, float towerH)
        {
            for (int i = 0; i < towers.Length; i++)
            {
                var c = towers[i]; float off = i * 1.55f;
                var crown = Mk.Glow(levelRoot, 0xff5010, 9, 0); crown.transform.localPosition = c + V(0, towerH + 1, 0); var crownM = crown.GetComponent<Renderer>().sharedMaterial;
                var footM = HazMat(0xff4000, 0, false); Flat(Geo.Ring(3.2f, 4.2f, 32), footM, c.x, 0.09f, c.z);
                var waveM = HazMat(0xff6a18, 0.85f, true); var wave = Deco(wallRingGeo, waveM, c.x, 0.6f, c.z); wave.layer = Mk.LayerIgnore; wave.SetActive(false);
                bool hitP = false; var hitE = new HashSet<Ent>();
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 9.3f;
                    bool warn = u < 1.5f, rolling = u >= 1.5f && u < 2.7f;
                    Mats.SetAlpha(crownM, warn ? u / 1.5f * 0.9f : rolling ? 0.9f - (u - 1.5f) * 0.6f : 0.08f);
                    Mats.SetAlpha(footM, warn ? 0.35f + 0.3f * Mathf.Sin(u * 24) : 0);
                    if (warn && rand < 0.5f) SpawnParticles(c + V(rand - 0.5f, towerH * rand, rand - 0.5f) * 1, 1, 3, emberMat, 0.1f, 0.6f, -3);
                    wave.SetActive(rolling);
                    if (!rolling) { hitP = false; hitE.Clear(); return; }
                    if (u - dt < 1.5f) { SpawnParticles(c + V(0, 0.5f, 0), 20, 10, emberMat, 0.14f, 0.8f); if (started) Sfx("boomSmall"); }
                    float R = 3 + (u - 1.5f) * 18; // rolls out to ~24 m
                    wave.transform.localScale = V(R, 1.2f + Mathf.Sin(t * 30) * 0.15f, R); Mats.SetAlpha(waveM, 0.85f * (1 - (u - 1.5f) / 1.2f));
                    if (!HazardLive) return;
                    float pd = Hypot(P.pos.x - c.x, P.pos.z - c.z);
                    if (!hitP && Mathf.Abs(pd - R) < 1.2f && P.pos.y < 1.3f) { hitP = true; var d = V(P.pos.x - c.x, 0, P.pos.z - c.z).normalized; HazardHitPlayer(15, c, d * 12 + V(0, 7, 0)); }
                    HazardHitEnemies(e => Mathf.Abs(Hypot(e.pos.x - c.x, e.pos.z - c.z) - R) < 1.2f && e.pos.y < 1.3f && hitE.Add(e), 3, e => V(e.pos.x - c.x, 0, e.pos.z - c.z).normalized * 10 + V(0, 6, 0), 0.1f);
                });
            }
        }
        // Crucible dressing: lava pouring down the towers, embers boiling off the moat, a burning chandelier overhead
        void CrucibleDressing(Vector3[] towers, float towerH)
        {
            var lavaM = Mats.Basic(0xffffff, Tex.Lava, true); lavaM.SetTextureScale("_BaseMap", new Vector2(0.6f, 4));
            anims.Add((t, dt) => lavaM.SetTextureOffset("_BaseMap", new Vector2(0, t * 0.35f))); // pouring down
            foreach (var c in towers)
            {
                var toC = V(-c.x, 0, -c.z).normalized; float ry = Mathf.Atan2(toC.x, toC.z);
                var fall = Deco(Geo.Plane(2.2f, towerH - 1), lavaM, c.x + toC.x * 2.62f, towerH / 2, c.z + toC.z * 2.62f, 0, ry, 0); fall.layer = Mk.LayerIgnore;
                var splash = Mk.Glow(levelRoot, 0xff6010, 6, 0.6f); splash.transform.localPosition = c + toC * 3.2f + V(0, 0.6f, 0);
            }
            SolidRing(V(0, 34, 0), 14, 0.8f, 28); SolidRing(V(0, 31, 0), 9, 0.6f, 20); // the chandelier is solid and grabbable (it barely sways)
            var chand = Mk.Piv(levelRoot, 0, 34, 0);
            Mk.Msh(chand.t, Geo.Torus(14, 0.7f, 64, 8), M.metal, 0, 0, 0, Mathf.PI / 2);
            Mk.Msh(chand.t, Geo.Torus(9, 0.5f, 48, 8), M.metal, 0, -3, 0, Mathf.PI / 2);
            for (int k = 0; k < 4; k++) { float a = k * Mathf.PI / 2; Mk.Msh(chand.t, Geo.Cyl(0.25f, 0.25f, 22, 6), M.metal, Mathf.Cos(a) * 7, 11, Mathf.Sin(a) * 7, 0, 0, Mathf.Cos(a) * 0.55f).transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 30, 0, -Mathf.Cos(a) * 30); }
            var flames = new List<Transform>();
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; var f = Mk.Glow(chand.t, 0xff7020, 3.5f, 0.85f); f.transform.localPosition = V(Mathf.Cos(a) * 14, 1.2f, Mathf.Sin(a) * 14); flames.Add(f.transform); }
            anims.Add((t, dt) =>
            {
                chand.SetRot(Mathf.Sin(t * 0.35f) * 0.04f, t * 0.12f, Mathf.Cos(t * 0.29f) * 0.04f);
                for (int k = 0; k < flames.Count; k++) flames[k].localScale = Vector3.one * (3.2f + Mathf.Sin(t * 11 + k * 1.3f) * 0.5f);
                if (rand < 0.6f) { float a = rand * Mathf.PI * 2, r = LB - 5; SpawnParticles(V(Mathf.Cos(a) * r, 0.3f, Mathf.Sin(a) * r), 1, 3, emberMat, 0.12f, 1.6f, -4); }
                if (rand < 0.15f) { float a = rand * Mathf.PI * 2; SpawnParticles(chand.t.position + V(Mathf.Cos(a) * 14, 0, Mathf.Sin(a) * 14), 1, 1, emberMat, 0.1f, 1.2f); } // dripping fire
            });
        }

        // ---------- boss phase 2 (Crucible): the floor cracks apart into lava islands ----------
        bool arenaChanged; readonly List<GameObject> crucibleWalls = new List<GameObject>();
        void CrucibleShatter()
        {
            if (arenaChanged || LEVEL.boss != 1) return; arenaChanged = true;
            Sfx("boom"); Sfx("slam"); P.shake = Mathf.Max(P.shake, 1.2f);
            Banner("THE CRUCIBLE BREAKS", "THE FLOOR IS LAVA - STAY ON THE ISLANDS, CLIMB THE ROCKS", "#ff6020");
            // Four rivers in a # shape split the floor into nine islands. The floor's mesh is swapped for separate slabs with
            // broken edges and molten seams, and the lava is revealed sunk below them. The floor's collider stays, so the
            // rivers burn (pools) but nothing about movement changes.
            const float RX = 16, RW = 5, LAVA_Y = -0.4f; float E = LB + 2, half = RW / 2;
            var rivers = new[] { (x: RX, z: 0f, alongX: false), (x: -RX, z: 0f, alongX: false), (x: 0f, z: RX, alongX: true), (x: 0f, z: -RX, alongX: true) };
            MeshRenderer floorR = null;
            foreach (Transform c in levelRoot)
            {
                var mf = c.GetComponent<MeshFilter>();
                if (mf && mf.sharedMesh && c.GetComponent<BoxCollider>() && mf.sharedMesh.bounds.size.x > LB * 2 && mf.sharedMesh.bounds.size.y < 2) { floorR = c.GetComponent<MeshRenderer>(); break; }
            }
            var lavaM = Mats.Basic(0xffffff, Tex.Lava); lavaM.SetTextureScale("_BaseMap", new Vector2(14, 14));
            var flowM = Mats.Add(0xffa040, 0.45f, Tex.Lava, false, true); flowM.SetTextureScale("_BaseMap", new Vector2(9, 9));
            var crackM = Mats.Add(0xffe080, 1, null, true, true); var seamM = Mats.Add(0xff7a20, 0.95f, null, true, true);
            var crustM = Mats.Std(0x1c0c08, 0.95f);
            var revealed = new List<GameObject>();
            // the sunken lava (one sheet under everything, seen only between the islands) and a flowing glow layer over it
            var lava = Deco(Geo.Plane(E * 2, E * 2), lavaM, 0, LAVA_Y, 0, -Mathf.PI / 2); lava.layer = Mk.LayerIgnore; revealed.Add(lava);
            var flow = Deco(Geo.Plane(E * 2, E * 2), flowM, 0, LAVA_Y + 0.03f, 0, -Mathf.PI / 2); flow.layer = Mk.LayerIgnore; revealed.Add(flow);
            // the nine islands
            float[] lo = { -E, -RX + half, RX + half }, hi = { -RX - half, RX - half, E };
            var islands = new List<(Transform t, Vector3 p, float ph)>();
            for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++)
            {
                float x0 = lo[i], x1 = hi[i], z0 = lo[j], z1 = hi[j], w = x1 - x0, d = z1 - z0, cx = (x0 + x1) / 2, cz = (z0 + z1) / 2;
                var piv = Mk.Piv(levelRoot, cx, 0, cz); var it = piv.t;
                var mesh = Geo.TiledBox(w, 4, d); levelMeshes.Add(mesh); Mk.Msh(it, mesh, M.floor, 0, -2, 0);
                // each side that faces a river: a glowing molten seam just under the lip, and broken chunks jutting into the lava
                foreach (var (sx, sz, len) in new[] { (i > 0 ? -1 : 0, 0, d), (i < 2 ? 1 : 0, 0, d), (0, j > 0 ? -1 : 0, w), (0, j < 2 ? 1 : 0, w) })
                {
                    if (sx == 0 && sz == 0) continue;
                    bool sideX = sx != 0; float ex = sx * w / 2, ez = sz * d / 2;
                    Mk.Msh(it, Geo.Box(sideX ? 0.14f : len, 0.12f, sideX ? len : 0.14f), seamM, ex + sx * 0.04f, -0.16f, ez + sz * 0.04f).layer = Mk.LayerIgnore;
                    for (float s = -len / 2 + 1.2f; s < len / 2 - 1.2f; s += 1.8f + rand * 1.6f)
                    {
                        float L = Mathf.Round((1.2f + rand * 1.8f) * 5) / 5, outward = 0.2f + rand * 0.7f, sink = rand * 0.14f;
                        var cp = sideX ? V(ex + sx * outward, -0.36f - sink, s) : V(s, -0.36f - sink, ez + sz * outward);
                        var ch = Mk.Msh(it, Geo.Box(sideX ? 1.3f : L, 0.72f, sideX ? L : 1.3f), M.floor, cp.x, cp.y, cp.z, (rand - 0.5f) * 0.3f, (rand - 0.5f) * 0.7f, (rand - 0.5f) * 0.3f);
                        ch.layer = Mk.LayerIgnore;
                        if (rand < 0.45f) Mk.Msh(it, Geo.Box(sideX ? 0.1f : L * 0.8f, 0.08f, sideX ? L * 0.8f : 0.1f), seamM, cp.x + sx * 0.66f, cp.y + 0.1f, cp.z + sz * 0.66f).layer = Mk.LayerIgnore;
                    }
                }
                // a few glowing fissures running across the island itself
                for (int k = 0; k < 3; k++)
                {
                    float fx = (rand - 0.5f) * (w - 6), fz = (rand - 0.5f) * (d - 6), fl = Mathf.Min(w, d) * (0.15f + rand * 0.2f);
                    Mk.Msh(it, Geo.Box(0.16f, 0.03f, fl), crackM, fx, 0.012f, fz, 0, rand * Mathf.PI, 0).layer = Mk.LayerIgnore;
                }
                it.gameObject.SetActive(false); revealed.Add(it.gameObject);
                islands.Add((it, it.localPosition, rand * 6));
            }
            // crust rafts drifting down the rivers, and heat glows along them
            var rafts = new List<(Transform t, int r, float s, float v)>();
            for (int k = 0; k < 16; k++)
            {
                int ri = k % 4; float s = (rand - 0.5f) * E * 2, v = (1 + rand) * (k % 2 == 0 ? 1 : -1);
                var raft = Deco(Geo.Box(1.2f + rand, 0.16f, 0.9f + rand * 0.6f), crustM, 0, LAVA_Y + 0.06f, 0, 0, rand * 3, 0); raft.layer = Mk.LayerIgnore;
                raft.SetActive(false); revealed.Add(raft); rafts.Add((raft.transform, ri, s, v));
            }
            var heat = new List<(Transform t, float ph)>();
            foreach (var rv in rivers) for (float s = -E + 7; s < E - 6; s += 13)
            {
                var g = Mk.Glow(levelRoot, 0xff5010, 9, 0.35f); g.transform.localPosition = rv.alongX ? V(s, LAVA_Y + 0.6f, rv.z) : V(rv.x, LAVA_Y + 0.6f, s);
                g.SetActive(false); revealed.Add(g); heat.Add((g.transform, s * 0.37f + rv.x));
            }
            Vector3 RiverPoint(int ri, float s, float y) { var r = rivers[ri]; return r.alongX ? V(s, y, r.z + (rand - 0.5f) * (RW - 1)) : V(r.x + (rand - 0.5f) * (RW - 1), y, s); }
            // cracks that race out from the middle before the floor gives way
            var cracks = new List<(GameObject g, bool alongX)>();
            foreach (var rv in rivers)
            {
                var cg = Deco(Geo.Box(rv.alongX ? E * 2 : 0.4f, 0.06f, rv.alongX ? 0.4f : E * 2), crackM, rv.x, 0.03f, rv.z); cg.layer = Mk.LayerIgnore;
                cg.transform.localScale = Vector3.zero; cracks.Add((cg, rv.alongX));
            }
            // the four cover walls burst apart, and torn-off slabs of the arena floor rise out of the lava to hover above it:
            // broken land with a rock root hanging beneath, a glowing molten underside and lava dripping off it
            var debris = new List<(Transform t, Vector3 v, Vector3 spin)>();
            var floaters = new List<(Transform t, Vector3 p, AABB box, float ph)>();
            foreach (var wall in crucibleWalls)
            {
                if (!wall) continue;
                var wb = wall.GetComponent<Renderer>().bounds; Vector3 wc = wb.center; wc.y = 0;
                bool alongX = wb.size.x > wb.size.z; Vector3 ax = alongX ? Vector3.right : Vector3.forward, perp = alongX ? Vector3.forward : Vector3.right;
                float sgn = Mathf.Sign(alongX ? wc.z : wc.x); // step outward, away from the middle
                float[] off = { -4.2f, 0, 4.2f }, side = { 1.2f, -0.6f, 2.0f }, hgt = { 2.0f, 3.3f, 4.6f };
                for (int k = 0; k < 3; k++)
                {
                    float w = 3.2f + rand * 1.1f, d = 3.0f + rand * 1.0f, y = hgt[k];
                    var c = wc + ax * off[k] + perp * side[k] * sgn;
                    var piv = Mk.Piv(levelRoot, c.x, LAVA_Y - 4, c.z); var it = piv.t;
                    var slab = Geo.TiledBox(w, 0.5f, d); levelMeshes.Add(slab);
                    var top = Mk.Msh(it, slab, M.floor, 0, -0.25f, 0);
                    // ragged outline: smaller broken fragments fused to the edges, a hair lower than the walkable top
                    for (int f = 0; f < 3; f++)
                    {
                        float fa = rand * Mathf.PI * 2, fw = 1.0f + rand * 1.0f;
                        Mk.Msh(it, Geo.Box(fw, 0.55f, fw * (0.6f + rand * 0.5f)), M.floor, Mathf.Cos(fa) * w * 0.45f, -0.32f - rand * 0.08f, Mathf.Sin(fa) * d * 0.45f, (rand - 0.5f) * 0.25f, rand * 3, (rand - 0.5f) * 0.25f).layer = Mk.LayerIgnore;
                    }
                    // the torn-out rock root hanging underneath, with smaller spikes around it
                    float R = Mathf.Min(w, d) * 0.55f;
                    Mk.Msh(it, Geo.Cone(R, 2.6f + rand, 6), M.tower, 0, -0.5f - 1.4f, 0, Mathf.PI, rand * 6, 0).layer = Mk.LayerIgnore;
                    for (int s = 0; s < 4; s++)
                    {
                        float sa = s / 4f * Mathf.PI * 2 + rand, sh = 0.9f + rand * 1.2f;
                        Mk.Msh(it, Geo.Cone(0.35f + rand * 0.35f, sh, 5), M.tower, Mathf.Cos(sa) * w * 0.32f, -0.5f - sh / 2, Mathf.Sin(sa) * d * 0.32f, Mathf.PI + (rand - 0.5f) * 0.4f, rand * 6, (rand - 0.5f) * 0.4f).layer = Mk.LayerIgnore;
                    }
                    // molten seam glowing round the underside lip, and a fissure across the top
                    Mk.Msh(it, Geo.Box(w + 0.1f, 0.1f, d + 0.1f), seamM, 0, -0.52f, 0).layer = Mk.LayerIgnore;
                    Mk.Msh(it, Geo.Box(0.14f, 0.03f, Mathf.Min(w, d) * 0.6f), crackM, (rand - 0.5f) * w * 0.3f, 0.012f, (rand - 0.5f) * d * 0.3f, 0, rand * Mathf.PI, 0).layer = Mk.LayerIgnore;
                    var under = Mk.Glow(it, 0xff5010, R * 2.4f, 0.4f); under.transform.localPosition = V(0, -1.2f, 0);
                    it.gameObject.SetActive(false); revealed.Add(it.gameObject);
                    top.layer = Mk.LayerIgnore; // solid only once it has risen into place
                    floaters.Add((it, V(c.x, y, c.z), new AABB(V(c.x - w / 2, y - 0.5f, c.z - d / 2), V(c.x + w / 2, y, c.z + d / 2)), rand * 6));
                }
            }
            float t0 = -1; bool opened = false, registered = false, floatSolid = false;
            anims.Add((t, dt) =>
            {
                if (t0 < 0) t0 = t; float u = t - t0;
                float run = Smoother(u / 0.75f);
                if (!opened)
                {
                    foreach (var c in cracks) c.g.transform.localScale = c.alongX ? V(run, 1 + 4 * run, 1) : V(1, 1 + 4 * run, run);
                    if (rand < 0.9f) { int ri = (int)(rand * 4) % 4; SpawnParticles(RiverPoint(ri, (rand - 0.5f) * E * 2 * run, 0.2f), 2, 7, rand < 0.5f ? ashMat : emberMat, 0.22f, 0.9f); }
                    if (u > 0.8f)
                    {
                        // the floor gives way: islands drop and settle, lava floods up between them
                        opened = true; if (floorR) floorR.enabled = false;
                        foreach (var g in revealed) g.SetActive(true);
                        foreach (var c in cracks) c.g.SetActive(false);
                        // the cover walls blow apart: their chunks tumble out and sink into the lava
                        foreach (var wall in crucibleWalls)
                        {
                            if (!wall || !wall.activeSelf) continue;
                            var wb = wall.GetComponent<Renderer>().bounds; var wmat = wall.GetComponent<Renderer>().sharedMaterial;
                            colliders.RemoveAll(cb => (cb.min - wb.min).sqrMagnitude < 0.01f && (cb.max - wb.max).sqrMagnitude < 0.01f);
                            wall.SetActive(false);
                            for (int k = 0; k < 12; k++)
                            {
                                var cp = wb.center + Vector3.Scale(wb.extents, V(rand * 2 - 1, rand * 2 - 1, rand * 2 - 1));
                                float s = 0.6f + rand * 0.9f;
                                var ch = Deco(Geo.Box(s, s * (0.6f + rand * 0.6f), s * (0.7f + rand * 0.5f)), wmat, cp.x, cp.y, cp.z, rand * 3, rand * 3, rand * 3); ch.layer = Mk.LayerIgnore;
                                var dir = (cp - wb.center); dir.y = 0; dir = dir.normalized + V((rand - 0.5f) * 0.6f, 0, (rand - 0.5f) * 0.6f);
                                debris.Add((ch.transform, dir * (3 + rand * 6) + V(0, 5 + rand * 7, 0), V(rand - 0.5f, rand - 0.5f, rand - 0.5f) * 720));
                            }
                            SpawnParticles(wb.center, 30, 9, ashMat, 0.3f, 1.6f, -1); SpawnParticles(wb.center, 24, 12, emberMat, 0.16f, 1.2f, 4);                        }
                        Sfx("boom"); Sfx("boomSmall"); P.shake = Mathf.Max(P.shake, 1.0f);
                        for (int ri = 0; ri < 4; ri++) for (int k = 0; k < 14; k++) { var p = RiverPoint(ri, (rand - 0.5f) * E * 2, 0); SpawnParticles(p, 2, 10, k % 3 == 0 ? ashMat : emberMat, 0.28f, 1.3f); }
                    }
                    return;
                }
                float o = u - 0.8f;
                // islands lurch down, bob and settle
                foreach (var isl in islands)
                {
                    float k = Mathf.Exp(-3.2f * o), wob = Mathf.Sin(o * 8 + isl.ph);
                    isl.t.localPosition = isl.p + V(0, -0.45f * k * (0.6f + 0.4f * wob), 0);
                    isl.t.localRotation = Quaternion.Euler(1.6f * k * Mathf.Sin(o * 7 + isl.ph * 2), 0, 1.6f * k * Mathf.Cos(o * 6 + isl.ph));
                }
                // wall chunks tumble and sink
                for (int k = debris.Count - 1; k >= 0; k--)
                {
                    var db = debris[k]; if (!db.t) { debris.RemoveAt(k); continue; }
                    db.v.y -= 22 * dt; db.t.localPosition += db.v * dt; db.t.Rotate(db.spin * dt, Space.Self);
                    if (db.t.localPosition.y < LAVA_Y - 1.5f) { SpawnParticles(db.t.localPosition + V(0, 1.4f, 0), 6, 5, emberMat, 0.14f, 0.8f, -8); Destroy(db.t.gameObject); debris.RemoveAt(k); }
                    else debris[k] = db;
                }
                // the broken land rises out of the lava (staggered), overshoots a little and hangs there, dripping
                for (int k = 0; k < floaters.Count; k++)
                {
                    var fl = floaters[k]; float q = Mathf.Clamp01((o - 0.25f - (k % 3) * 0.25f) / 1.3f);
                    float rise = q < 1 ? 1 - Mathf.Pow(1 - q, 3) * Mathf.Cos(q * 5) : 1;
                    float bob = q >= 1 ? 0.04f * Mathf.Sin(t * 1.3f + fl.ph) : 0;
                    fl.t.localPosition = V(fl.p.x, Mathf.LerpUnclamped(LAVA_Y - 4, fl.p.y, rise) + bob, fl.p.z);
                    fl.t.localRotation = Quaternion.Euler((1 - q) * 14 * Mathf.Sin(fl.ph), fl.ph * 20, (1 - q) * 10 * Mathf.Cos(fl.ph));
                    if (q > 0 && q < 0.35f && rand < 0.5f) SpawnParticles(V(fl.p.x, LAVA_Y + 0.2f, fl.p.z), 3, 7, rand < 0.5f ? emberMat : ashMat, 0.2f, 1.0f, -6);
                    if (q >= 1 && rand < 0.06f) SpawnParticles(fl.t.position + V((rand - 0.5f) * 1.5f, -2.2f, (rand - 0.5f) * 1.5f), 1, 0.4f, emberMat, 0.12f, 1.0f, -12);
                }
                if (!floatSolid && o > 2.4f)
                {
                    floatSolid = true;
                    foreach (var fl in floaters) { colliders.Add(fl.box); var top = fl.t.GetChild(0).gameObject; top.layer = Mk.LayerLevel; top.AddComponent<BoxCollider>(); }
                }
                if (!registered && o > 0.5f) { registered = true; foreach (var rv in rivers) pools.Add(new Pool { x = rv.x, z = rv.z, w = rv.alongX ? E * 2 : RW, d = rv.alongX ? RW : E * 2 }); } // now it burns
                lavaM.SetTextureOffset("_BaseMap", new Vector2(t * 0.02f, Mathf.Sin(t * 0.21f) * 0.05f));
                flowM.SetTextureOffset("_BaseMap", new Vector2(-t * 0.045f, t * 0.03f));
                Mats.SetAlpha(flowM, 0.35f + 0.15f * Mathf.Sin(t * 1.3f));
                for (int k = 0; k < rafts.Count; k++)
                {
                    var rf = rafts[k]; float s = Mathf.Repeat(rf.s + rf.v * t + E, E * 2) - E; var r = rivers[rf.r];
                    rf.t.localPosition = r.alongX ? V(s, LAVA_Y + 0.06f + Mathf.Sin(t * 1.4f + k) * 0.03f, r.z + Mathf.Sin(t * 0.5f + k) * 1.2f) : V(r.x + Mathf.Sin(t * 0.5f + k) * 1.2f, LAVA_Y + 0.06f + Mathf.Sin(t * 1.4f + k) * 0.03f, s);
                }
                foreach (var h in heat) h.t.localScale = Vector3.one * (8 + 2.5f * Mathf.Sin(t * 1.7f + h.ph));
                // bubbling, and now and then a spurt of molten rock
                if (rand < 0.55f) SpawnParticles(RiverPoint((int)(rand * 4) % 4, (rand - 0.5f) * E * 2, LAVA_Y + 0.1f), 1, 2.5f, emberMat, 0.13f, 0.9f, -6);
                if (rand < 0.03f) { var p = RiverPoint((int)(rand * 4) % 4, (rand - 0.5f) * E * 2, LAVA_Y + 0.1f); SpawnParticles(p, 9, 6, emberMat, 0.2f, 1.3f, -14); SpawnParticles(p, 3, 3, ashMat, 0.25f, 1.2f, -2); }
            });
            var glow = Mk.Glow(levelRoot, 0xff4000, 120, 0.25f); glow.transform.localPosition = V(0, 3, 0);
        }

        // ---------- boss phase 2 (Sanctum): the ceiling opens to a blinding sky ----------
        readonly List<N> sanctumPetals = new List<N>(); Transform sanctumSky; Material sanctumSkyM, sanctumShaftM;
        // while the dome is shut it is a real roof: you can't fly up through it (see RoofClamp)
        bool roofShut; float roofRimR, roofRimY;
        float RoofY(float r) => roofRimY + Mathf.Max(0, roofRimR - r) * Mathf.Tan(0.16f);
        void RoofClamp()
        {
            if (!roofShut) return;
            float top = RoofY(Hypot(P.pos.x, P.pos.z)) - 0.6f;
            if (P.pos.y + P.height > top) { P.pos.y = top - P.height; if (P.vel.y > 0) P.vel.y = 0; if (HK.state == "pull" && HK.enemy == null && HK.pos.y > top - 1) ReleaseHook(); }
        }
        void BuildSanctumCeiling(float rimR, float rimY)
        {
            sanctumPetals.Clear(); roofShut = true; roofRimR = rimR; roofRimY = rimY;
            Physics.queriesHitBackfaces = true; // the petals are single-sided: let rays from below hit them
            var stone = Mats.Std(0xf2ead8, 0.55f, 0.1f, 0, null, true); var seam = Mats.Add(0xffd070, 0.8f, null, true, true);
            int n = 12; float half = Mathf.PI * rimR / n + 0.6f;
            var petal = GeoX.ShapeMB(new GeoX.Shape().MoveTo(-half, 0).LineTo(half, 0).LineTo(0.6f, rimR - 0.5f).LineTo(-0.6f, rimR - 0.5f).LineTo(-half, 0)).Build();
            for (int k = 0; k < n; k++)
            {
                float a = (k + 0.5f) * Mathf.PI * 2 / n; var rim = V(Mathf.Cos(a) * rimR, rimY, Mathf.Sin(a) * rimR);
                var piv = Mk.Piv(levelRoot, rim.x, rim.y, rim.z); piv.xyz = false; // yaw toward the centre, then tilt about the rim
                piv.SetRot(-0.16f, Mathf.Atan2(-rim.x, -rim.z), 0);
                var pg = Mk.Msh(piv.t, petal, stone, 0, 0, 0, Mathf.PI / 2, 0, 0);
                pg.layer = Mk.LayerLevel; pg.AddComponent<MeshCollider>().sharedMesh = petal; // the grapple bites the roof
                Mk.Msh(piv.t, Geo.Box(0.25f, 0.12f, rimR - 1), seam, 0, -0.1f, (rimR - 1) / 2).layer = Mk.LayerIgnore; // glowing seam down the middle
                Mk.Msh(piv.t, Geo.Box(half * 2, 0.8f, 1.2f), M.metal, 0, 0, 0.4f);                                 // rim beam
                sanctumPetals.Add(piv);
            }
            // the blinding sky waiting above (dark until it opens)
            var sky = Mk.Glow(levelRoot, 0xffffff, 280, 0, false); sky.transform.localPosition = V(0, rimY + 70, 0); sanctumSky = sky.transform; sanctumSkyM = sky.GetComponent<Renderer>().sharedMaterial;
            sanctumShaftM = Mats.Add(0xfff6e0, 0, null, false, true);
            var shaft = Mk.Msh(levelRoot, Geo.Cyl(20, 26, 1, 32, true), sanctumShaftM, 0, rimY / 2, 0); shaft.transform.localScale = V(1, rimY + 40, 1); shaft.layer = Mk.LayerIgnore;
        }
        void SanctumOpen()
        {
            if (arenaChanged || LEVEL.boss != 2 || sanctumPetals.Count == 0) return; arenaChanged = true; roofShut = false;
            Sfx("holy"); Sfx("portalOpen"); Sfx("boom"); whiteFlash = Mathf.Max(whiteFlash, 0.5f); P.shake = Mathf.Max(P.shake, 0.8f);
            Banner("THE HEAVENS OPEN", "THE SERAPH CALLS DOWN THE LIGHT", "#fff0b0");
            float t0 = -1, sun0 = sunLight.intensity; var amb0 = RenderSettings.ambientSkyColor; var eq0 = RenderSettings.ambientEquatorColor;
            anims.Add((t, dt) =>
            {
                if (t0 < 0) t0 = t; float u = t - t0, open = Smoother(u / 3.5f);
                for (int i = 0; i < sanctumPetals.Count; i++) sanctumPetals[i].rx = -0.16f - open * 1.95f * (0.92f + 0.08f * Mathf.Sin(i * 2.3f)); // swing up and out
                Mats.SetAlpha(sanctumSkyM, open); sanctumSky.localScale = Vector3.one * (220 + open * 80 + Mathf.Sin(t * 0.8f) * 10);
                Mats.SetAlpha(sanctumShaftM, open * (0.22f + 0.04f * Mathf.Sin(t * 1.3f)));
                // the whole arena brightens under the open sky
                sunLight.intensity = Mathf.Lerp(sun0, sun0 * 1.8f, open);
                RenderSettings.ambientSkyColor = Color.Lerp(amb0, amb0 * 1.6f, open); RenderSettings.ambientEquatorColor = Color.Lerp(eq0, eq0 * 1.5f, open);
                if (u < 3.5f && rand < 0.8f) { var pv = sanctumPetals[Random.Range(0, sanctumPetals.Count)].t.position; SpawnParticles(pv + V((rand - 0.5f) * 10, 0, (rand - 0.5f) * 10), 2, 5, rand < 0.6f ? dustMat : goldSpark, 0.25f, 2.5f); }
                if (open > 0.5f && rand < 0.6f) particles.Add(new Pt { pos = V((rand - 0.5f) * 40, 40 + rand * 20, (rand - 0.5f) * 40), scale = Vector3.one * 0.1f, mat = moteMat, v = V(0, -2 - rand * 2, 0), life = 10, grav = 0 });
            });
        }

        // ---------- 10b. judgement sigils (Sanctum): a golden sigil locks onto where you're going, then light smites it ----------
        class Sigil { public Vector3 at; public float t; public Transform ring, beam; public Material ringM, beamM; }
        void MechSigils()
        {
            var live = new List<Sigil>(); float next = 4;
            trapAnims.Add((t, dt) =>
            {
                if (HazardLive && phase == "fight")
                {
                    next -= dt;
                    if (next <= 0)
                    {
                        next = 3.2f + rand * 1.5f;
                        var at = P.pos + V(P.vel.x, 0, P.vel.z) * 0.7f; at.y = 0; at.x = clamp(at.x, -(LB - 4), LB - 4); at.z = clamp(at.z, -(LB - 4), LB - 4);
                        var s = new Sigil { at = at, ringM = HazMat(0xffd060, 0.8f, true), beamM = HazMat(0xfff4d0, 0, true) };
                        var root = new GameObject("sigil").transform; root.SetParent(levelRoot, false); root.localPosition = at + V(0, 0.1f, 0);
                        var r1 = Mk.Msh(root, Geo.Ring(3.0f, 3.3f, 48), s.ringM, 0, 0, 0, -Mathf.PI / 2); r1.layer = Mk.LayerIgnore;
                        for (int k = 0; k < 6; k++) { float a = k * Mathf.PI / 3; var spoke = Mk.Msh(root, Geo.Box(0.18f, 0.02f, 2.6f), s.ringM, Mathf.Cos(a) * 1.4f, 0.01f, Mathf.Sin(a) * 1.4f); spoke.transform.localRotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg + 90, 0); spoke.layer = Mk.LayerIgnore; }
                        var inner = Mk.Msh(root, Geo.Ring(1.2f, 1.45f, 32), s.ringM, 0, 0.01f, 0, -Mathf.PI / 2); inner.layer = Mk.LayerIgnore;
                        s.ring = root;
                        var beam = Mk.Msh(levelRoot, Geo.Cyl(3, 3, 60, 24, true), s.beamM, at.x, 30, at.z); beam.layer = Mk.LayerIgnore; s.beam = beam.transform;
                        live.Add(s); if (started) Sfx("charge");
                    }
                }
                for (int i = live.Count - 1; i >= 0; i--)
                {
                    var s = live[i]; s.t += dt;
                    float lock_ = Smoother(s.t / 1.5f);
                    s.ring.localScale = Vector3.one * Mathf.Lerp(1.8f, 1, lock_); s.ring.localRotation = Quaternion.Euler(0, s.t * 160, 0);
                    Mats.SetAlpha(s.ringM, 0.4f + 0.5f * lock_ * (0.7f + 0.3f * Mathf.Sin(s.t * 30)));
                    if (s.t < 1.6f) continue;
                    // smite
                    float b = s.t - 1.6f;
                    if (b - dt <= 0)
                    {
                        if (started) { Sfx("snipeHit"); Sfx("boomSmall"); }
                        ShockRing(s.at, 5, 0xffe0a0, 0.4f); SpawnParticles(s.at + V(0, 0.5f, 0), 24, 10, goldSpark, 0.1f, 0.8f);
                        if (PlayerIn(s.at, 3.2f, -1, 30)) { var d = P.pos - s.at; d.y = 0; HazardHitPlayer(20, s.at, d.normalized * 8 + V(0, 9, 0)); }
                        HazardHitEnemies(e => Hypot(e.pos.x - s.at.x, e.pos.z - s.at.z) < 3.2f, 5, e => V(0, 10, 0), 0.1f);
                    }
                    Mats.SetAlpha(s.beamM, Mathf.Clamp01(1 - b / 0.45f) * 0.8f); s.beam.localScale = V(1 - b, 1, 1 - b);
                    if (b > 0.45f) { Mk.DestroyTree(s.ring.gameObject); Mk.DestroyTree(s.beam.gameObject); live.RemoveAt(i); }
                }
            });
        }
        // Sanctum dressing: rune rings orbiting every column, golden feathers drifting down, light falling from the halo
        void SanctumDressing(int columns, float radius, float colH)
        {
            BuildSanctumCeiling(radius * 1.24f, 36);
            var rings = new List<(N n, float ph)>();
            for (int k = 0; k < columns; k++)
            {
                float a = k * Mathf.PI * 2 / columns + Mathf.PI / 12; var c = V(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
                var n = Mk.Piv(levelRoot, c.x, colH * 0.62f, c.z);
                Mk.Msh(n.t, Geo.Torus(3.4f, 0.12f, 40, 6), M.trim, 0, 0, 0, Mathf.PI / 2);
                for (int j = 0; j < 4; j++) { float b = j * Mathf.PI / 2; Mk.Msh(n.t, Geo.Box(0.5f, 0.5f, 0.08f), M.trim, Mathf.Cos(b) * 3.4f, 0, Mathf.Sin(b) * 3.4f, 0, -b, Mathf.PI / 4); }
                rings.Add((n, k * 0.7f));
            }
            for (int k = 0; k < 6; k++) { float a = k * Mathf.PI / 3; LightShaft(V(Mathf.Cos(a) * radius * 0.92f, 31, Mathf.Sin(a) * radius * 0.92f), V(Mathf.Cos(a) * radius * 0.36f, 0, Mathf.Sin(a) * radius * 0.36f), 1.5f, 5, 0xfff0c8, 0.11f); }
            anims.Add((t, dt) =>
            {
                foreach (var r in rings) r.n.SetRot(Mathf.Sin(t * 0.5f + r.ph) * 0.35f, t * 0.6f + r.ph, Mathf.Cos(t * 0.4f + r.ph) * 0.35f);
                if (rand < 0.18f) // feathers: flat golden flakes that tumble down
                {
                    float a = rand * Mathf.PI * 2, r = rand * (LB - 6);
                    particles.Add(new Pt { pos = V(Mathf.Cos(a) * r, 26 + rand * 8, Mathf.Sin(a) * r), scale = V(0.25f, 0.02f, 0.09f), mat = rand < 0.6f ? moteMat : goldSpark, v = V(rand - 0.5f, -1.4f, rand - 0.5f), life = 14, grav = 0, hasSpin = true, spin = V(rand * 3, rand * 2, rand * 3) });
                }
            });
        }

        // ---------- 8. storm clouds (Sky Bridges): clouds drift after you, charge up, and drop lightning ----------
        void MechStormClouds(int n)
        {
            for (int i = 0; i < n; i++)
            {
                float ph = i * 2.1f;
                var root = new GameObject("storm cloud").transform; root.SetParent(levelRoot, false);
                // a real cumulonimbus: a dark flattened base, billowing puffs that get lighter toward the top, all slowly churning
                // translucent: overlapping puffs build up a denser core while the edges stay see-through
                Material[] shade = { Mats.Trans(0x3c4254, 0.62f), Mats.Trans(0x5c6578, 0.5f), Mats.Trans(0x8a94aa, 0.42f), Mats.Trans(0xc0c8d6, 0.36f) };
                var rng = new System.Random(77 + i * 13); float R() => (float)rng.NextDouble();
                var puffs = new List<(Transform t, Vector3 s, float ph)>();
                void Puff(Vector3 p, Vector3 s, int m) { var g = Mk.Msh(root, Geo.Sphere(1, 14, 10), shade[m], p.x, p.y, p.z); g.transform.localScale = s; g.layer = Mk.LayerIgnore; puffs.Add((g.transform, s, R() * 6)); }
                for (int k = 0; k < 7; k++) { float a = k / 7f * Mathf.PI * 2, r = 4.5f + R() * 2.5f; Puff(V(Mathf.Cos(a) * r, -0.6f, Mathf.Sin(a) * r * 0.7f), V(3.6f + R(), 1.5f, 3.2f + R()), 0); } // base
                Puff(V(0, -0.8f, 0), V(6.5f, 1.6f, 5), 0);
                for (int k = 0; k < 8; k++) { float a = k / 8f * Mathf.PI * 2 + 0.3f, r = 2.5f + R() * 3; Puff(V(Mathf.Cos(a) * r, 1.0f + R(), Mathf.Sin(a) * r * 0.7f), Vector3.one * (2.6f + R() * 1.4f), 1); } // body
                for (int k = 0; k < 6; k++) { float a = k / 6f * Mathf.PI * 2, r = 1.2f + R() * 2.2f; Puff(V(Mathf.Cos(a) * r, 3.0f + R() * 1.2f, Mathf.Sin(a) * r * 0.7f), Vector3.one * (2.0f + R() * 1.2f), 2); } // billows
                for (int k = 0; k < 3; k++) Puff(V((R() - 0.5f) * 3, 5.0f + R(), (R() - 0.5f) * 2), Vector3.one * (1.5f + R()), 3); // sunlit tops
                var bellyM = HazMat(0x9ad8ff, 0, true); var belly = Mk.Msh(root, Geo.Circle(6, 28), bellyM, 0, -2.0f, 0, Mathf.PI / 2); belly.layer = Mk.LayerIgnore; // lightning lighting the underside
                var flash = Mk.Glow(root, 0xc8e8ff, 16, 0); var flashM = flash.GetComponent<Renderer>().sharedMaterial;
                trapAnims.Add((t, dt) =>
                {
                    root.localRotation = Quaternion.Euler(0, t * 3 + ph * 30, 0);
                    foreach (var pf in puffs) pf.t.localScale = pf.s * (1 + 0.05f * Mathf.Sin(t * 0.7f + pf.ph));
                });
                var markM = HazMat(0x9ad8ff, 0, false); var mark = Flat(Geo.Circle(4, 28), markM, 0, 0.08f, 0);
                var pos = V(Mathf.Cos(ph) * 30, 30, Mathf.Sin(ph) * 30); root.localPosition = pos;
                float cyc = 1.5f + i * 1.3f;
                trapAnims.Add((t, dt) =>
                {
                    // drift: a slow wander that leans toward the player
                    var wander = V(Mathf.Sin(t * 0.13f + ph) * 34, 30, Mathf.Sin(t * 0.09f + ph * 1.7f) * 34);
                    if (HazardLive) wander = Vector3.Lerp(wander, V(P.pos.x, 30, P.pos.z), 0.45f);
                    pos += (wander - pos) * (1 - Mathf.Exp(-0.5f * dt)); root.localPosition = pos;
                    mark.transform.localPosition = V(pos.x, 0.08f, pos.z);
                    cyc -= dt;
                    bool charging = cyc < 1.3f;
                    float fl = charging ? (Mathf.Sin(t * 40) > 0.3f ? 0.6f : 0.1f) * (1 - cyc / 1.3f) : 0;
                    Mats.SetAlpha(flashM, fl); Mats.SetAlpha(bellyM, fl * 0.7f); Mats.SetAlpha(markM, charging ? 0.25f + 0.3f * (1 - cyc / 1.3f) : 0);
                    if (charging && rand < 0.3f) SpawnParticles(pos + V((rand - 0.5f) * 8, -2, (rand - 0.5f) * 4), 1, 2, cyanMat, 0.08f, 0.3f, 0);
                    // rain pours from the belly while it builds up to a strike
                    if (charging) for (int r = 0; r < 3; r++) particles.Add(new Pt { pos = pos + V((rand - 0.5f) * 9, -2.2f, (rand - 0.5f) * 7), scale = V(0.03f, 0.55f, 0.03f), mat = steamMat, v = V(0, -26, 0), life = 1.1f, grav = 0 });
                    if (cyc > 0) return;
                    cyc = 3.6f + rand * 2;
                    // strike
                    var ground = V(pos.x, 0, pos.z);
                    Lightning(pos - V(0, 2, 0), ground, 0xc8e8ff); Lightning(pos - V(0, 2, 0), ground, 0xffffff);
                    Blast(ground + V(0, 1, 0), 3, 0xc8e8ff); ShockRing(ground, 5, 0x9ad8ff, 0.35f);
                    SpawnParticles(ground + V(0, 0.4f, 0), 18, 9, cyanMat, 0.08f, 0.5f);
                    Mats.SetAlpha(flashM, 1);
                    if (started) { Sfx("snipeHit", 0.6f); Sfx("boomSmall"); P.shake = Mathf.Max(P.shake, Mathf.Clamp01(1 - Vector3.Distance(P.pos, ground) / 30) * 0.5f); }
                    if (PlayerIn(ground, 4, -1, 6)) { var d = P.pos - ground; d.y = 0; HazardHitPlayer(18, ground, d.normalized * 9 + V(0, 8, 0)); }
                    HazardHitEnemies(e => Hypot(e.pos.x - ground.x, e.pos.z - ground.z) < 4, 4, e => V(0, 9, 0), 0.1f);
                });
            }
        }

        // ---------- 9. blade wheels (The Choir): golden saw wheels roll back and forth along their lanes ----------
        void MechBladeWheels((Vector3 a, Vector3 b, float speed)[] lanes)
        {
            foreach (var lane in lanes)
            {
                var dir = (lane.b - lane.a).normalized; float len = Vector3.Distance(lane.a, lane.b);
                // grooves cut in the floor show the lane
                var groove = Deco(Geo.Box(0.5f, 0.05f, len), M.trim, (lane.a.x + lane.b.x) / 2, 0.03f, (lane.a.z + lane.b.z) / 2, 0, Mathf.Atan2(dir.x, dir.z), 0);
                var root = new GameObject("blade wheel").transform; root.SetParent(levelRoot, false);
                var spin = Mk.Piv(root, 0, 0, 0);
                // the wheel lies in its local XY plane (axle along z)
                Mk.Msh(spin.t, Geo.Torus(2.1f, 0.22f, 40, 8), M.metal);
                Mk.Msh(spin.t, Geo.Cyl(0.5f, 0.5f, 0.5f, 12), M.metal, 0, 0, 0, Mathf.PI / 2);
                for (int k = 0; k < 12; k++)
                {
                    float a = k * Mathf.PI / 6;
                    Mk.Msh(spin.t, Geo.Box(0.12f, 2.0f, 0.08f), M.trim, Mathf.Cos(a) * 1.05f, Mathf.Sin(a) * 1.05f, 0, 0, 0, a - Mathf.PI / 2); // spokes
                    Mk.Msh(spin.t, Geo.Cone(0.32f, 0.8f, 4), M.metal, Mathf.Cos(a) * 2.45f, Mathf.Sin(a) * 2.45f, 0, 0, 0, a - Mathf.PI / 2);   // saw teeth
                }
                var glow = Mk.Glow(root, 0xffd060, 6, 0.5f);
                root.localRotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, dir)); // wheel plane holds the lane
                float ph = lane.a.x * 0.13f + lane.a.z * 0.07f; Vector3 last = lane.a;
                trapAnims.Add((t, dt) =>
                {
                    float u = 0.5f - 0.5f * Mathf.Cos(t * lane.speed + ph); // ease at each end of the lane
                    var p = Vector3.Lerp(lane.a, lane.b, u) + V(0, 2.4f, 0); root.localPosition = p;
                    float moved = Vector3.Dot(p - last, dir); last = p;
                    spin.rz += moved / 2.4f; // rolls along without slipping
                    if (Mathf.Abs(moved) > dt * 4 && rand < 0.6f) SpawnParticles(V(p.x, 0.1f, p.z), 1, 4, goldSpark, 0.06f, 0.3f);
                    if (!HazardLive) return;
                    var push = dir * Mathf.Sign(moved) * 16 + V(0, 7, 0);
                    if (Vector3.Distance(p, ChestPos) < 2.7f) HazardHitPlayer(20, p, push);
                    HazardHitEnemies(e => Vector3.Distance(p, BodyPoint(e)) < 2.4f + e.hitR, 5, e => push, 0.6f);
                });
            }
        }

        // ---------- 6. updrafts ----------
        void MechUpdrafts(Vector3[] spots)
        {
            foreach (var c in spots)
            {
                Deco(Geo.Torus(3.3f, 0.25f, 32, 6), M.trim, c.x, 0.12f, c.z, -Mathf.PI / 2);
                Flat(Geo.Circle(3.2f, 32), HazMat(0xfff0c0, 0.35f, true), c.x, 0.1f, c.z);
                var colM = HazMat(0xffe8b0, 0.16f, true); var col = Deco(Geo.Cyl(3.2f, 3.2f, 40, 24, true), colM, c.x, 20, c.z); col.layer = Mk.LayerIgnore;
                var inner = Deco(Geo.Cyl(1.4f, 1.4f, 40, 16, true), HazMat(0xffffff, 0.12f, true), c.x, 20, c.z); inner.layer = Mk.LayerIgnore;
                trapAnims.Add((t, dt) =>
                {
                    Mats.SetAlpha(colM, 0.13f + 0.05f * Mathf.Sin(t * 3 + c.x));
                    if (rand < 0.8f) { float a = rand * 6.28f, r = rand * 3; particles.Add(new Pt { pos = c + V(Mathf.Cos(a) * r, rand * 4, Mathf.Sin(a) * r), scale = Vector3.one * 0.12f, mat = moteMat, v = V(0, 9 + rand * 6, 0), life = 2.5f, grav = 0 }); }
                    if (!HazardLive) return;
                    if (PlayerIn(c, 3.2f, -1, 36))
                    {
                        float target = 17 * Mathf.Clamp01(1 - P.pos.y / 38); // lifts you up to the top, then lets you glide out
                        P.vel.y += (target - P.vel.y) * (1 - Mathf.Exp(-5 * dt)); P.onGround = false; P.slamming = false;
                    }
                    foreach (var e in enemies) if (!e.dead && e.type == "husk" && e.spawnT <= 0 && Hypot(e.pos.x - c.x, e.pos.z - c.z) < 3.2f)
                        { e.vel.y += (15 * Mathf.Clamp01(1 - e.pos.y / 30) - e.vel.y) * (1 - Mathf.Exp(-5 * dt)); e.ground = false; e.stun = Mathf.Max(e.stun, 0.2f); }
                });
            }
        }

        // ---------- 7. the great bell ----------
        void MechBell(float y)
        {
            var bronze = G(Mats.Std(0xd09a50, 0.38f, 0.5f, 0x1a0e04)); var bronzeDark = G(Mats.Std(0x7a5428, 0.5f, 0.45f)); var gold = G(Mats.Std(0xe8c050, 0.25f, 0.9f));
            // the belfry: two stone towers standing on the roof walks either side, a carved beam between them, braces and a yoke
            float px = Hv(26), top = y + 10.5f, foot = 12.6f, hh = top - foot;
            foreach (var sx in new[] { -1f, 1f })
            {
                float x = sx * px;
                Box(x, foot, 0, 3, hh, 3, M.tower); TrimRect(x, 0, 3, 3, top);
                Deco(Geo.Box(3.6f, 0.6f, 3.6f), M.block, x, foot + 0.3f, 0); Deco(Geo.Box(3.8f, 0.8f, 3.8f), M.block, x, top + 0.4f, 0);
                Deco(Geo.Cone(1.6f, 3.2f, 4), M.block, x, top + 2.4f, 0, 0, Mathf.PI / 4, 0);                    // spire cap
                Deco(Geo.Cone(0.35f, 1.2f, 8), gold, x, top + 4.6f, 0);
                for (int k = 0; k < 3; k++) Deco(Geo.Box(3.06f, 0.18f, 3.06f), M.trim, x, foot + hh * (0.3f + k * 0.22f), 0);
                Deco(Geo.Box(0.06f, hh * 0.5f, 1.2f), M.trim, x - sx * 1.53f, foot + hh * 0.55f, 0);              // inner face inlay
                // a diagonal brace from the tower into the beam
                var a0 = V(x - sx * 1.5f, top - 4.2f, 0); var a1 = V(x - sx * 5.2f, top - 0.4f, 0); var d = a1 - a0;
                var br = Deco(Geo.Box(0.6f, d.magnitude, 0.6f), M.metal, (a0.x + a1.x) / 2, (a0.y + a1.y) / 2, 0); br.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            }
            Deco(Geo.Box(px * 2, 1.4f, 1.8f), M.metal, 0, top, 0);                                                   // the beam
            Deco(Geo.Box(px * 2 + 0.4f, 0.25f, 2.0f), M.trim, 0, top + 0.82f, 0); Deco(Geo.Box(px * 2 + 0.4f, 0.25f, 2.0f), M.trim, 0, top - 0.82f, 0);
            Deco(Geo.Box(3.2f, 0.9f, 2.4f), bronzeDark, 0, top - 1.1f, 0);                                         // the yoke
            foreach (var sz in new[] { -1f, 1f }) Deco(Geo.Cyl(0.35f, 0.35f, 0.5f, 12), gold, 0, top - 1.2f, sz * 1.25f, Mathf.PI / 2, 0, 0); // bearings
            var piv = Mk.Piv(levelRoot, 0, top - 1.2f, 0);
            // the bell: a real bell profile (shoulder, waist, flared sound-bow and lip), bands and a crown of loops
            var prof = new[] { new Vector2(0.01f, 0), new Vector2(1.3f, -0.05f), new Vector2(1.75f, -0.5f), new Vector2(1.9f, -1.4f), new Vector2(2.0f, -2.6f),
                               new Vector2(2.25f, -3.6f), new Vector2(2.8f, -4.4f), new Vector2(3.35f, -4.95f), new Vector2(3.5f, -5.2f), new Vector2(3.3f, -5.25f), new Vector2(2.9f, -5.0f) };
            var bellMesh = GeoX.LatheMB(prof, 40, 0, Mathf.PI * 2).Build();
            Mk.Msh(piv.t, bellMesh, bronze, 0, -1.4f, 0); Mk.Msh(piv.t, bellMesh, bronzeDark, 0, -1.4f, 0, 0, 0, 0).transform.localScale = V(-0.985f, 0.995f, 0.985f); // inside face
            foreach (var (by, br) in new[] { (-1.85f, 1.78f), (-2.05f, 1.84f), (-5.05f, 3.38f), (-5.55f, 3.5f), (-4.4f, 2.62f) })
                Mk.Msh(piv.t, Geo.Torus(br, 0.07f, 48, 6), gold, 0, by, 0, Mathf.PI / 2, 0, 0);
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; Mk.Msh(piv.t, Geo.Box(0.22f, 0.5f, 0.06f), gold, Mathf.Cos(a) * 2.05f, -3.2f, Mathf.Sin(a) * 2.05f, 0, -a + Mathf.PI / 2, 0); } // inscription marks
            foreach (var a in new[] { 0f, Mathf.PI / 2 }) Mk.Msh(piv.t, Geo.Torus(0.42f, 0.11f, 20, 6, Mathf.PI), bronzeDark, 0, -1.1f, 0, 0, a, 0); // crown loops
            Mk.Msh(piv.t, Geo.Cyl(0.08f, 0.08f, 4.4f, 6), M.metal, 0, -3.6f, 0);                                     // clapper rod
            var clap = Mk.Piv(piv.t, 0, -5.8f, 0); Mk.Msh(clap.t, Geo.Sphere(0.55f, 14, 10), bronzeDark);
            var glow = Mk.Glow(piv.t, 0xffe8a0, 14, 0); glow.transform.localPosition = V(0, -5, 0); var glowM = glow.GetComponent<Renderer>().sharedMaterial;
            SolidAABB(V(-px, top - 0.7f, -0.9f), V(px, top + 0.7f, 0.9f));
            // the shockwave: a glowing wall of sound with a white-hot core, a ring of light on the floor and a fainter echo
            var wallM = HazMat(0xffd070, 0.8f, true); var coreM = HazMat(0xffffff, 0.9f, true); var floorM = HazMat(0xffe8a8, 0.6f, true); var echoM = HazMat(0xffc860, 0.4f, true);
            var wave = Deco(wallRingGeo, wallM, 0, 0.75f, 0); var core = Deco(wallRingGeo, coreM, 0, 0.3f, 0); var echo = Deco(wallRingGeo, echoM, 0, 0.5f, 0);
            var floorRing = Flat(Geo.Ring(0.9f, 1, 96), floorM, 0, 0.06f, 0);
            foreach (var g in new[] { wave, core, echo, floorRing }) { g.layer = Mk.LayerIgnore; g.SetActive(false); }
            bool rung = false, hitP = false; float t0 = -1;
            trapAnims.Add((t, dt) =>
            {
                // the first toll waits a while after you arrive (no free hit the moment you spawn), then every 10 s
                if (t0 < 0) t0 = t; float lt = t - t0;
                float u = lt < 6 ? 0 : (lt - 6) % 10;
                float swing = u < 1.4f ? Mathf.Sin(u * 9) * 0.22f * (u / 1.4f) : u < 3.5f ? Mathf.Sin(u * 9) * 0.22f * (1 - (u - 1.4f) / 2.1f) : 0;
                piv.SetRot(0, 0, swing); clap.SetRot(0, 0, -swing * 1.6f);
                Mats.SetAlpha(glowM, u < 1.4f ? u / 1.4f * 0.5f : Mathf.Max(0, 0.8f - (u - 1.4f)));
                if (u < 1.4f) { rung = false; hitP = false; foreach (var g in new[] { wave, core, echo, floorRing }) g.SetActive(false); return; }
                if (!rung)
                {
                    rung = true; var mouth = piv.t.TransformPoint(V(0, -6.4f, 0));
                    Blast(mouth, 5, 0xffe0a0); SpawnParticles(mouth, 26, 10, goldSpark, 0.1f, 0.9f, 2);
                    if (started) { Sfx("slam"); Sfx("boom"); P.shake = Mathf.Max(P.shake, 0.35f); }
                }
                float R = (u - 1.4f) * 26, lim = LB + 4, fade = Mathf.Clamp01(1 - R / lim); bool on = R < lim;
                foreach (var g in new[] { wave, core, floorRing }) g.SetActive(on);
                float Re = R - 5; echo.SetActive(on && Re > 0.5f);
                if (!on) return;
                float wob = 1 + 0.12f * Mathf.Sin(t * 30);
                wave.transform.localScale = V(R, 1.5f * wob, R); core.transform.localScale = V(R, 0.6f, R); floorRing.transform.localScale = V(R + 1.2f, R + 1.2f, 1);
                if (Re > 0.5f) echo.transform.localScale = V(Re, 1.0f, Re);
                Mats.SetAlpha(wallM, 0.75f * fade); Mats.SetAlpha(coreM, 0.9f * fade); Mats.SetAlpha(floorM, 0.55f * fade); Mats.SetAlpha(echoM, 0.35f * fade);
                for (int k = 0; k < 6; k++) { float a = rand * Mathf.PI * 2; SpawnParticles(V(Mathf.Cos(a) * R, 0.15f, Mathf.Sin(a) * R), 1, 3, rand < 0.6f ? goldSpark : dustMat, 0.08f, 0.5f); } // dust thrown up by the ring
                if (!HazardLive) return;
                float pd = Hypot(P.pos.x, P.pos.z);
                if (!hitP && Mathf.Abs(pd - R) < 1.3f && P.pos.y < 1.4f) { hitP = true; var d = V(P.pos.x, 0, P.pos.z).normalized; HazardHitPlayer(14, V(0, 1, 0), d * 14 + V(0, 6, 0)); }
            });
        }
        // ---------- 8. low gravity: floating rocks drifting round the arena ----------
        void MechLowGravity()
        {
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2 + 0.2f, r = LB + 14 + (i * 13) % 40, y = 4 + (i * 7) % 30, s = 2 + (i % 4) * 1.5f, ph = i * 0.9f;
                var g = Deco(Geo.Octahedron(s), i % 2 == 0 ? M.block : M.tower, Mathf.Cos(a) * r, y, Mathf.Sin(a) * r, i, i * 0.7f, 0);
                var tr = g.transform; var basePos = tr.localPosition;
                trapAnims.Add((t, dt) => { tr.localPosition = basePos + V(0, Mathf.Sin(t * 0.5f + ph) * 1.5f, 0); tr.Rotate(dt * 6, dt * 10, 0); });
            }
        }

        // ---------- 9. sanctuaries ----------
        void MechSanctuaries(int n)
        {
            for (int i = 0; i < n; i++)
            {
                float ph = i * 2.6f;
                var root = new GameObject("sanctuary").transform; root.SetParent(levelRoot, false);
                Mk.Msh(root, Geo.Torus(4, 0.22f, 40, 6), M.trim, 0, 0.12f, 0, -Mathf.PI / 2);
                var disc = Mk.Msh(root, Geo.Circle(4, 40), HazMat(0x90ffb0, 0.28f, true), 0, 0.09f, 0, -Mathf.PI / 2); disc.layer = Mk.LayerIgnore;
                var colM = HazMat(0xb0ffd0, 0.12f, true); var col = Mk.Msh(root, Geo.Cyl(4, 4, 14, 32, true), colM, 0, 7, 0); col.layer = Mk.LayerIgnore;
                Mk.Glow(root, 0x90ffc0, 6, 0.4f).transform.localPosition = V(0, 1, 0);
                Vector3 PosAt(float t) => V(Mathf.Sin(t * 0.11f + ph) * (LB - 18), 0, Mathf.Sin(t * 0.073f + ph * 1.7f) * (LB - 18));
                root.localPosition = PosAt(0);
                trapAnims.Add((t, dt) =>
                {
                    var c = PosAt(t); root.localPosition = c;
                    bool inside = HazardLive && PlayerIn(c, 4, -1, 6);
                    Mats.SetAlpha(colM, inside ? 0.28f : 0.12f);
                    if (rand < 0.5f) { float a = rand * 6.28f, r = rand * 4; particles.Add(new Pt { pos = c + V(Mathf.Cos(a) * r, 0.2f, Mathf.Sin(a) * r), scale = Vector3.one * 0.1f, mat = moteMat, v = V(0, 3, 0), life = 1.5f, grav = 0 }); }
                    if (inside) { Heal(16 * dt); P.stamina = Mathf.Min(3, P.stamina + dt * 0.8f); }
                });
            }
        }

        // ---------- 6. sentinel statues (Gates of Dawn): the angel statues lock on and fire beams of light ----------
        // One statue at a time wakes (its eyes flare), traces you with a thin beam for a moment, holds still, then fires
        // along that line. Columns and walls block it, so breaking line of sight is the counter. It hits enemies too.
        class Sentinel { public Vector3 eye; public Material eyeM; }
        readonly List<Sentinel> sentinels = new List<Sentinel>();
        static float SegDist(Vector3 p, Vector3 a, Vector3 b) { var ab = b - a; float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude)); return Vector3.Distance(p, a + ab * t); }
        void MechSentinels()
        {
            var traceM = HazMat(0xff7040, 0, true); var beamM = HazMat(0xfff2c8, 0, true); var beamM2 = HazMat(0xffc860, 0, true);
            var trace = Deco(Geo.Cyl(1, 1, 1, 8, true), traceM, 0, -50, 0); trace.layer = Mk.LayerIgnore;
            var beam = Deco(Geo.Cyl(1, 1, 1, 14, true), beamM, 0, -50, 0); beam.layer = Mk.LayerIgnore;
            var beam2 = Deco(Geo.Cyl(1, 1, 1, 14, true), beamM2, 0, -50, 0); beam2.layer = Mk.LayerIgnore;
            var hitGlow = Mk.Glow(levelRoot, 0xfff0c0, 5, 0); var hitGlowM = hitGlow.GetComponent<Renderer>().sharedMaterial;
            void Line(GameObject g, Vector3 a, Vector3 b, float r)
            {
                var d = b - a; float len = Mathf.Max(0.01f, d.magnitude);
                g.transform.localPosition = (a + b) * 0.5f; g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d / len); g.transform.localScale = V(r, len, r);
            }
            Sentinel cur = null; float st = 0, next = 3; Vector3 aim = Vector3.zero, end = Vector3.zero; bool fired = false;
            trapAnims.Add((t, dt) =>
            {
                foreach (var s in sentinels) if (s != cur) Mats.SetAlpha(s.eyeM, 0.25f + 0.1f * Mathf.Sin(t * 2 + s.eye.x));
                if (cur == null)
                {
                    Mats.SetAlpha(traceM, 0); Mats.SetAlpha(beamM, 0); Mats.SetAlpha(beamM2, 0); Mats.SetAlpha(hitGlowM, 0);
                    if (!HazardLive || phase != "fight" || sentinels.Count == 0) return;
                    next -= dt; if (next > 0) return;
                    // wake a statue that can see you (the nearest of those)
                    float best = 1e9f;
                    foreach (var s in sentinels) { float d = Vector3.Distance(s.eye, ChestPos); if (d < best && d < 80 && Los(s.eye, ChestPos)) { best = d; cur = s; } }
                    if (cur == null) { next = 0.5f; return; }
                    st = 0; fired = false; if (started) Sfx("laserCharge");
                }
                st += dt;
                var eye = cur.eye;
                if (st < 1.5f) aim = Vector3.Lerp(aim == Vector3.zero ? ChestPos : aim, ChestPos, st < 0.1f ? 1 : 1 - Mathf.Exp(-9 * dt)); // tracking
                var dir = (aim - eye).normalized; float far = 120;
                if (Physics.Raycast(eye + dir * 1.5f, dir, out var hit, far, LEVEL_MASK, QueryTriggerInteraction.Ignore)) end = hit.point; else end = eye + dir * far;
                Mats.SetAlpha(cur.eyeM, Mathf.Min(1, 0.4f + st));
                if (st < 1.85f)
                {
                    bool locked = st >= 1.5f;
                    Line(trace, eye, end, locked ? 0.07f : 0.04f);
                    Mats.SetAlpha(traceM, locked ? (Mathf.Sin(t * 50) > 0 ? 0.9f : 0.3f) : 0.35f + 0.25f * (st / 1.5f));
                    return;
                }
                Mats.SetAlpha(traceM, 0);
                float b = st - 1.85f;
                if (!fired)
                {
                    fired = true;
                    if (started) { Sfx("snipeHit"); Sfx("boomSmall"); P.shake = Mathf.Max(P.shake, 0.3f); }
                    if (SegDist(ChestPos, eye, end) < 1.3f) HazardHitPlayer(22, eye, (end - eye).normalized * 10 + V(0, 6, 0));
                    HazardHitEnemies(e => SegDist(BodyPoint(e), eye, end) < e.hitR + 0.7f, 6, e => (end - eye).normalized * 8 + V(0, 5, 0), 0.3f);
                    SpawnParticles(end, 22, 10, goldSpark, 0.1f, 0.6f); ShockRing(new Vector3(end.x, Mathf.Max(0.1f, end.y - 0.4f), end.z), 3, 0xffe0a0, 0.35f);
                    hitGlow.transform.localPosition = end;
                }
                float fade = Mathf.Clamp01(1 - b / 0.35f);
                Line(beam, eye, end, 0.22f * fade + 0.05f); Line(beam2, eye, end, 0.55f * fade + 0.1f);
                Mats.SetAlpha(beamM, fade); Mats.SetAlpha(beamM2, fade * 0.45f); Mats.SetAlpha(hitGlowM, fade);
                if (b > 0.35f) { cur = null; aim = Vector3.zero; next = 2.6f + rand * 1.6f; }
            });
        }

        // ---------- 9. spear tide (The Choir): golden spears erupt from the floor in a rolling wave down the hall ----------
        // The safe lane is lit in gold from the start; a burning band races ahead of the wave front; behind it the spears
        // burst up row by row and sink again. Only you get hurt - it's a hazard for the player, enemies are left alone.
        void MechSpearTide()
        {
            const int COLS = 34, ROWS = 7; const float GAP = 7, SPEED = 13, ROW_D = 2.4f, UP_T = 0.6f;
            var spearM = Mats.Std(0xf0c850, 0.25f, 0.85f, 0x2a1800); var tipM = Mats.Std(0xfff4d8, 0.2f, 0.6f, 0x3a3010);
            var laneM = HazMat(0xfff4c8, 0, true); var bandM = HazMat(0xffb040, 0, true);
            var lane = Flat(Geo.Plane(GAP, 1), laneM, 0, 0.05f, 0); var band = Flat(Geo.Plane(1, 3), bandM, 0, 0.06f, 0);
            var laneEdges = new[] { Flat(Geo.Plane(0.25f, 1), laneM, 0, 0.055f, 0), Flat(Geo.Plane(0.25f, 1), laneM, 0, 0.055f, 0) };
            var rows = new List<(Transform root, List<Transform> sp, Material glowM)>();
            for (int r = 0; r < ROWS; r++)
            {
                var root = new GameObject("spear row").transform; root.SetParent(levelRoot, false);
                var sps = new List<Transform>();
                for (int i = 0; i < COLS; i++)
                {
                    var sp = Mk.Piv(root, 0, 0, 0).t;
                    Mk.Msh(sp, Geo.Cyl(0.07f, 0.12f, 2.0f, 6), spearM, 0, 1.0f, 0);
                    Mk.Msh(sp, Geo.Cone(0.2f, 0.9f, 6), tipM, 0, 2.45f, 0);
                    Mk.Msh(sp, Geo.Cone(0.32f, 0.35f, 6), spearM, 0, 0.18f, 0);   // a cracked collar where it bursts through
                    sps.Add(sp);
                }
                var gm = HazMat(0xffe090, 0, true); var strip = Mk.Msh(root, Geo.Plane(1, ROW_D), gm, 0, 0.07f, 0, -Mathf.PI / 2); strip.name = "strip";
                Mk.SetLayerRecursive(root.gameObject, Mk.LayerIgnore); root.gameObject.SetActive(false);
                rows.Add((root, sps, gm));
            }
            var age = new float[ROWS]; var rz = new float[ROWS]; var hitRow = new bool[ROWS]; int nextRow = 0;
            float next = 3, front = 0, rowZ = 0, gx = 0, tele = 0, laneA = 0; int dir = 1; bool live = false;
            trapAnims.Add((t, dt) =>
            {
                float W = LB * 2;
                // rows already out: burst up, hold, sink
                for (int r = 0; r < ROWS; r++)
                {
                    var row = rows[r]; if (!row.root.gameObject.activeSelf) continue;
                    age[r] += dt; float a = age[r];
                    float h = a < 0.1f ? Smoother(a / 0.1f) * 1.15f : a < 0.16f ? Mathf.Lerp(1.15f, 1, (a - 0.1f) / 0.06f) : a < 0.42f ? 1 : 1 - Smoother((a - 0.42f) / (UP_T - 0.42f));
                    foreach (var sp in row.sp) sp.localScale = V(1, Mathf.Max(0.01f, h), 1);
                    Mats.SetAlpha(row.glowM, Mathf.Clamp01(1 - a / UP_T) * 0.5f);
                    if (!hitRow[r] && a > 0.04f && a < 0.42f && Mathf.Abs(P.pos.z - rz[r]) < ROW_D / 2 + 0.3f && Mathf.Abs(P.pos.x - gx) > GAP / 2 - 0.4f && P.pos.y < 2.9f)
                    { hitRow[r] = true; HazardHitPlayer(16, V(P.pos.x, 0, rz[r]), V(0, 11, 0) + V(0, 0, dir) * 6); }
                    if (a > UP_T) row.root.gameObject.SetActive(false);
                }
                if (!live)
                {
                    laneA = Mathf.Max(0, laneA - dt); Mats.SetAlpha(laneM, laneA); Mats.SetAlpha(bandM, 0);
                    if (!HazardLive || phase != "fight") return;
                    next -= dt; if (next > 0) return;
                    live = true; tele = 1.4f;
                    dir = P.pos.z > 0 ? -1 : 1; front = -dir * LB; rowZ = front + dir * ROW_D / 2;
                    gx = clamp(P.pos.x + (rand < 0.5f ? -1 : 1) * (6 + rand * 10), -(LB - GAP), LB - GAP);
                    lane.transform.localPosition = V(gx, 0.05f, 0); lane.transform.localScale = V(1, W, 1);
                    for (int e = 0; e < 2; e++) { laneEdges[e].transform.localPosition = V(gx + (e == 0 ? -1 : 1) * GAP / 2, 0.055f, 0); laneEdges[e].transform.localScale = V(1, W, 1); }
                    band.transform.localScale = V(W, 1, 1);
                    if (started) Sfx("charge");
                }
                laneA = 0.3f + 0.12f * Mathf.Sin(t * 6); Mats.SetAlpha(laneM, laneA);
                if (tele > 0) { tele -= dt; band.transform.localPosition = V(0, 0.06f, front + dir * 2); Mats.SetAlpha(bandM, Mathf.Sin(t * 30) > 0 ? 0.6f : 0.25f); return; }
                front += dir * SPEED * dt;
                band.transform.localPosition = V(0, 0.06f, front + dir * 3.5f); Mats.SetAlpha(bandM, 0.45f + 0.2f * Mathf.Sin(t * 25));
                if (rand < 0.6f) SpawnParticles(V((rand * 2 - 1) * LB, 0.15f, front + dir * 3.5f), 1, 2.5f, goldSpark, 0.07f, 0.35f);
                // the front crossed the next row line: burst a row of spears there (none in the safe lane)
                while ((rowZ - front) * dir < 0 && Mathf.Abs(rowZ) < LB)
                {
                    int r = nextRow; nextRow = (nextRow + 1) % ROWS; var row = rows[r];
                    row.root.gameObject.SetActive(true); row.root.localPosition = V(0, 0, rowZ); age[r] = 0; rz[r] = rowZ; hitRow[r] = false;
                    float step = W / COLS;
                    for (int i = 0; i < COLS; i++)
                    {
                        float x = -LB + step * (i + 0.5f) + (Mathf.Sin(i * 7.3f + rowZ) * 0.35f); bool inGap = Mathf.Abs(x - gx) < GAP / 2 + 0.2f;
                        var sp = row.sp[i]; sp.gameObject.SetActive(!inGap); sp.localPosition = V(x, 0, Mathf.Cos(i * 3.1f + rowZ) * 0.5f); sp.localRotation = Quaternion.Euler(Mathf.Sin(i * 2.7f) * 8, i * 40, Mathf.Cos(i * 1.9f) * 8);
                    }
                    var strip = row.root.Find("strip"); strip.localScale = V(W, 1, 1);
                    if (started && Mathf.Abs(rowZ - P.pos.z) < 30) Sfx("spike", 0.6f);
                    SpawnParticles(V((rand * 2 - 1) * LB, 0.3f, rowZ), 3, 5, goldSpark, 0.08f, 0.4f);
                    rowZ += dir * ROW_D;
                }
                if (Mathf.Abs(front) > LB + 4) { live = false; next = 2.5f + rand * 1.5f; }
            });
        }

        // ---------- 10. halo lances ----------
        void MechLances(int n)
        {
            for (int i = 0; i < n; i++)
            {
                float ph = i * Mathf.PI * 2 / n;
                var outerM = HazMat(0xffe0a0, 0.3f, true); var coreM = HazMat(0xffffff, 0.6f, true);
                var root = new GameObject("lance").transform; root.SetParent(levelRoot, false);
                var o = Mk.Msh(root, Geo.Cyl(1.6f, 1.6f, 34, 16, true), outerM, 0, 17, 0); o.layer = Mk.LayerIgnore;
                var cr = Mk.Msh(root, Geo.Cyl(0.5f, 0.5f, 34, 10, true), coreM, 0, 17, 0); cr.layer = Mk.LayerIgnore;
                var scorch = Mk.Msh(root, Geo.Circle(2.4f, 24), HazMat(0xffd070, 0.4f, true), 0, 0.08f, 0, -Mathf.PI / 2); scorch.layer = Mk.LayerIgnore;
                var gl = Mk.Glow(root, 0xffe0a0, 6, 0.5f); gl.transform.localPosition = V(0, 0.8f, 0); var glM = gl.GetComponent<Renderer>().sharedMaterial;
                Vector3 PosAt(float t) { float a = t * 0.3f + ph, r = 27 + 12 * Mathf.Sin(t * 0.4f + ph); return V(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r); }
                root.localPosition = PosAt(0);
                trapAnims.Add((t, dt) =>
                {
                    var c = PosAt(t); root.localPosition = c;
                    bool hot = HazardLive && phase == "fight";
                    float fl = 0.85f + 0.15f * Mathf.Sin(t * 25 + ph);
                    Mats.SetAlpha(outerM, (hot ? 0.32f : 0.08f) * fl); Mats.SetAlpha(coreM, (hot ? 0.7f : 0.15f) * fl); Mats.SetAlpha(glM, hot ? 0.6f : 0.15f);
                    if (hot && rand < 0.5f) SpawnParticles(c + V(0, 0.3f, 0), 1, 4, goldSpark, 0.08f, 0.4f);
                    if (!hot) return;
                    if (PlayerIn(c, 1.9f, -1, 34)) { var d = P.pos - c; d.y = 0; HazardHitPlayer(12, c, d.normalized * 12 + V(0, 5, 0)); }
                    HazardHitEnemies(e => Hypot(e.pos.x - c.x, e.pos.z - c.z) < 1.6f + e.hitR, 3, e => { var d = e.pos - c; d.y = 0; return d.normalized * 10; }, 0.5f);
                });
            }
        }
    }
}
