using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    public class HollowRig
    {
        public N root, hips, torso, neck, head, jaw, lSh, lEl, lHand, rSh, rEl, rHand; public N[] thigh = new N[2], knee = new N[2];
        public Transform crown; public GameObject core; public Material glow, fire; public float t; public Vector3 aim; public float aimYaw;
        // the rework (GameHollowKing2): the skyline on his back, smokestacks, things swinging off his arms, the crown of fire
        public List<N> xTowers = new List<N>(), xStacks = new List<N>(), xSwing = new List<N>(); public N xCrownFire; public Material xEmber, xFire, xWin;
    }

    // ---------- Boss 4: THE HOLLOW KING (The Broken Crown, level 15) ----------
    // A colossus the dead city built out of itself: concrete pillars for legs, a truck chassis for hips, a ribcage of bent
    // girders round a burning hollow, a car wedged in one shoulder, a skull of concrete with fire for eyes, a broken crown.
    // He climbs out of the rubble. Moves: tears chunks off the buildings round the square and hurls them; picks up cars and
    // throws them; slams the ground (shockwave rings you jump, debris thrown up, the cars nearby flung); a sweep of his
    // arm; a charge (into a wall, he's stunned - and can be executed); a rain of rubble called down on you; a shrapnel volley.
    // At half health he roars and speeds up. He dies collapsing into a heap of wreckage; his crown rolls away.
    public partial class Game
    {
        const float HOLLOW_HP = 1000; // (co-op: tougher, faster, harder-hitting than solo's 820)
        class HCar { public Transform t; public Vector3 pos; public float ry; public bool gone; public GameObject solid; public Vector3 mn, mx; }
        class HChunk { public Transform t; public bool gone; }
        class HThrow { public Transform t; public Vector3 v, spin; public float r, dmg; public bool car; }
        class HDrop { public Vector3 p; public float t, delay; public GameObject warn, chunk; public Material wm; }
        readonly List<HCar> hkCars = new List<HCar>(); readonly List<HChunk> hkChunks = new List<HChunk>(); readonly List<HThrow> hkThrows = new List<HThrow>(); readonly List<HDrop> hkDrops = new List<HDrop>();
        HCar hkHeldCar; Transform hkHeld; Material hkConc, hkSteel, hkRust, hkGold;
        bool hkHeldIsCar; Vector3 hkLiftFrom; Quaternion hkLiftRot; const float HK_TOF = 0.78f; // (throws are fast: ~0.8s in the air)
        Vector3 HandsTop(Ent e) { var r = e.hk; return (r.rHand.t.position + r.lHand.t.position) / 2 + V(0, 0.6f, 0); }
        // the buildings round the square: a charge into one stuns him - and brings it down for good
        class HBuild { public Vector3 mn, mx, c; public float w, d; public bool broken; public List<GameObject> objs = new List<GameObject>(); public List<AABB> cols = new List<AABB>(); public List<HChunk> chunks = new List<HChunk>(); }
        readonly List<HBuild> hkBuilds = new List<HBuild>();
        HBuild BuildingAt(Vector3 p, float pad) { foreach (var b in hkBuilds) if (!b.broken && p.x > b.mn.x - pad && p.x < b.mx.x + pad && p.z > b.mn.z - pad && p.z < b.mx.z + pad) return b; return null; }
        // ---------- the building coming down ----------
        // A charge into a building doesn't just knock it over - it brings it down ON him:
        //  IMPACT    the face he hits blows in (its ground-floor blocks burst into rubble), every window on that side bursts in a
        //            spray of glass, a shock of dust, the hit freezes the frame for an instant
        //  BUCKLE    the whole tower shudders and groans; grit and dust pour down its face, the first blocks drop off the roof;
        //            the strip where it'll land lights up on the square
        //  TOPPLE    it goes over towards him, pivoting on its front edge like a felled tree, gathering speed - shedding blocks
        //            off its back and roofline, fireballs bursting out of its floors as the gas goes up
        //  SNAP      halfway down it breaks in two: the top floors tear free and fly on ahead, tumbling
        //  IMPACT    the rest slams into the square: the frame freezes in slow motion, a white flash, a wall of dust rolls
        //            out along the length of it, and every block of it smashes apart, bouncing, rolling, kicking up dust
        //  AFTER     a broken stump where it stood, its blocks strewn across the square, the heap burning and smoking
        // The building really is broken into those blocks: its own walls are cut into a shell of pieces (UVs laid out in world
        // space, so the windows line up exactly where they were) and everything on it - roof units, tanks, balconies, signs -
        // rides on the piece it was standing on.
        class FChunk { public Transform t; public Vector3 v, w; public bool dyn, rest; public float r, hitT; public int row; }
        static Material glassShardM;
        Mesh FracMesh(Vector3 size, Vector3 worldMin)
        {
            var b = new Geo.MB(); var h = size / 2;
            void Face(Vector3 n, Vector3 u, Vector3 vv, float fu, float fv)
            {
                int s0 = b.v.Count; var c = Vector3.Scale(n, h);
                var q = new[] { c - u * fu / 2 - vv * fv / 2, c + u * fu / 2 - vv * fv / 2, c + u * fu / 2 + vv * fv / 2, c - u * fu / 2 + vv * fv / 2 };
                foreach (var p in q)
                {
                    var wp = worldMin + h + p; // world-aligned UVs: 4m per tile, like the level's tiled boxes
                    b.v.Add(p); b.n.Add(n);
                    b.uv.Add(Mathf.Abs(n.x) > 0.5f ? new Vector2(wp.z, wp.y) / 4 : Mathf.Abs(n.z) > 0.5f ? new Vector2(wp.x, wp.y) / 4 : new Vector2(wp.x, wp.z) / 4);
                }
                b.Tri(s0, s0 + 1, s0 + 2); b.Tri(s0, s0 + 2, s0 + 3); b.Tri(s0, s0 + 2, s0 + 1); b.Tri(s0, s0 + 3, s0 + 2); // both windings: never culled
            }
            Face(Vector3.right, Vector3.forward, Vector3.up, size.z, size.y); Face(Vector3.left, Vector3.forward, Vector3.up, size.z, size.y);
            Face(Vector3.forward, Vector3.right, Vector3.up, size.x, size.y); Face(Vector3.back, Vector3.right, Vector3.up, size.x, size.y);
            Face(Vector3.up, Vector3.right, Vector3.forward, size.x, size.z); Face(Vector3.down, Vector3.right, Vector3.forward, size.x, size.z);
            var m = b.Build(); levelMeshes.Add(m); return m;
        }
        void BreakBuilding(HBuild b, bool hurt = true)
        {
            b.broken = true;
            foreach (var cb in b.cols) { var mn0 = cb.min; var mx0 = cb.max; colliders.RemoveAll(x => (x.min - mn0).sqrMagnitude < 1e-4f && (x.max - mx0).sqrMagnitude < 1e-4f); }
            foreach (var ch in b.chunks) ch.gone = true;
            if (!glassShardM) glassShardM = G(Mats.Basic(0xb8d8e8));
            // the building's main block: its bounds are what gets fractured, its material is what the pieces are made of
            MeshRenderer main = null; float best = 0;
            foreach (var g in b.objs) if (g) foreach (var mr in g.GetComponentsInChildren<MeshRenderer>(true)) { var sz = mr.bounds.size; float vol = sz.x * sz.y * sz.z; if (vol > best) { best = vol; main = mr; } }
            foreach (var g in b.objs) if (g) Mk.StripColliders(g);
            if (!main) { foreach (var g in b.objs) if (g) g.SetActive(false); return; }
            var mb = main.bounds; var mat = main.sharedMaterial; var bmin = mb.min; var bsz = mb.size;
            var dir = !hurt ? mb.center - P.pos : boss != null ? boss.pos - mb.center : -mb.center; dir.y = 0; dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward; // it falls ON him
            float ext = Mathf.Abs(dir.x) * mb.extents.x + Mathf.Abs(dir.z) * mb.extents.z, wide = Mathf.Abs(dir.z) * bsz.x + Mathf.Abs(dir.x) * bsz.z;
            var hinge = V(mb.center.x, 0, mb.center.z) + dir * ext; var axis = Vector3.Cross(Vector3.up, dir);
            // cut it into a shell of blocks: ~4.5m across, a floor (3.4m) high
            int nx = Mathf.Max(2, Mathf.RoundToInt(bsz.x / 4.5f)), nz = Mathf.Max(2, Mathf.RoundToInt(bsz.z / 4.5f)), ny = Mathf.Max(3, Mathf.RoundToInt(bsz.y / 3.4f));
            var cell = V(bsz.x / nx, bsz.y / ny, bsz.z / nz);
            var group = new GameObject("topple").transform; group.SetParent(levelRoot, false); group.position = hinge;
            var chunks = new List<FChunk>();
            for (int iy = 0; iy < ny; iy++) for (int ix = 0; ix < nx; ix++) for (int iz = 0; iz < nz; iz++)
            {
                if (!(ix == 0 || ix == nx - 1 || iz == 0 || iz == nz - 1 || iy == ny - 1)) continue; // a shell (the inside is never seen)
                var cmin = bmin + Vector3.Scale(cell, V(ix, iy, iz));
                var go = new GameObject("block"); go.layer = Mk.LayerIgnore; go.AddComponent<MeshFilter>().sharedMesh = FracMesh(cell, cmin); go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                go.transform.SetParent(iy == 0 ? levelRoot : group, true); go.transform.position = cmin + cell / 2;
                chunks.Add(new FChunk { t = go.transform, r = Mathf.Min(cell.x, Mathf.Min(cell.y, cell.z)) * 0.5f, row = iy });
            }
            // everything that stood on the building rides on the block it was on; the original block goes
            main.gameObject.SetActive(false);
            foreach (var g in b.objs)
            {
                if (!g || g == main.gameObject || !g.activeSelf) continue;
                var gp = g.transform.position; var rr = g.GetComponentInChildren<Renderer>(); if (rr) gp = rr.bounds.center;
                FChunk near = null; float nd = 1e9f; foreach (var c in chunks) { float dd = (c.t.position - gp).sqrMagnitude; if (dd < nd) { nd = dd; near = c; } }
                if (near != null) g.transform.SetParent(near.t, true);
            }
            float T = 0, th = 0, om = 0, shedT = 0, Hg = bsz.y - cell.y; bool toppling = false, snapped = false, shattered = false, fx1 = false, fx2 = false;
            float g0 = 26; // the fall's gravity (a touch heavy - it reads as huge)
            void Release(FChunk c, Vector3 extra)
            {
                if (c.dyn) return; c.dyn = true; var wp = c.t.position;
                var tang = Vector3.Cross(axis * om, wp - hinge); // the velocity it had as part of the falling tower
                c.t.SetParent(levelRoot, true); c.v = tang + extra; c.w = Random.insideUnitSphere * (2 + rand * 4);
            }
            // ---- IMPACT: the face he hit blows in, the glass bursts, the ground floor there turns to rubble ----
            Sfx("boom"); Sfx("slam"); Sfx("shatter"); P.shake = Mathf.Max(P.shake, 1.3f); freeze = Mathf.Max(freeze, 0.07f);
            var face = hinge + V(0, 2.5f, 0);
            SpawnParticles(face, 70, 14, dustMat, 0.7f, 2.2f); Blast(face, 4, 0xffc890);
            foreach (var c in chunks.ToArray())
            {
                var cp = c.t.position; float fd = Vector3.Dot(cp - hinge, dir);
                if (fd > -cell.x * 0.75f && c.row <= 1 && Mathf.Abs(Vector3.Dot(cp - hinge, axis)) < wide * 0.3f)
                {
                    // the blocks he hit: gone in a burst of concrete
                    for (int j = 0; j < 6; j++) particles.Add(new Pt { pos = cp + Random.insideUnitSphere * 1.2f, scale = Vector3.one * (0.4f + rand * 0.8f), mat = concDarkM, v = -dir * (4 + rand * 8) + Random.insideUnitSphere * 5 + V(0, 3, 0), life = 2.5f, grav = GRAV, hasSpin = true, spin = Random.insideUnitSphere * 8 });
                    Destroy(c.t.gameObject); chunks.Remove(c);
                }
            }
            for (int j = 0; j < 90; j++) // every window on the struck face bursts: glass sprays out over him
            {
                var gp = hinge + axis * ((rand - 0.5f) * wide) + V(0, rand * bsz.y, 0);
                particles.Add(new Pt { pos = gp, scale = V(0.12f + rand * 0.2f, 0.02f, 0.1f + rand * 0.2f), mat = glassShardM, v = dir * (3 + rand * 9) + Random.insideUnitSphere * 3, life = 1.8f + rand, grav = GRAV, hasSpin = true, spin = Random.insideUnitSphere * 14 });
            }
            WarnRect(hinge, dir, bsz.y + 2, wide + 2, 3.4f, 0xff5020); // where it's going to land
            anims.Add((t, dt) =>
            {
                T += dt;
                // ---- the blocks in flight: gravity, tumbling, slamming into the square, bouncing, coming to rest ----
                foreach (var c in chunks)
                {
                    if (!c.dyn || c.rest || !c.t) continue;
                    c.v.y -= g0 * dt; c.t.position += c.v * dt; c.t.Rotate(c.w * Mathf.Rad2Deg * dt, Space.World);
                    var p = c.t.position; float floor = c.r * 0.8f;
                    if (p.y < floor)
                    {
                        p.y = floor; c.t.position = p;
                        if (c.v.y < -5 && T - c.hitT > 0.2f) { c.hitT = T; SpawnParticles(p, 6, 5, dustMat, 0.6f, 1.8f, -0.5f); if (rand < 0.25f) Sfx("slam", 0.35f); for (int j = 0; j < 2; j++) particles.Add(new Pt { pos = p + V(0, 0.5f, 0), scale = Vector3.one * (0.3f + rand * 0.4f), mat = concDarkM, v = Random.insideUnitSphere * 5 + V(0, 4, 0), life = 1.5f, grav = GRAV, hasSpin = true, spin = Random.insideUnitSphere * 8 }); }
                        c.v.y = -c.v.y * 0.22f; c.v.x *= 0.5f; c.v.z *= 0.5f; c.w *= 0.55f;
                        if (c.v.sqrMagnitude < 0.8f) { c.rest = true; var e2 = c.t.eulerAngles; c.t.rotation = Quaternion.Euler(Mathf.Round(e2.x / 90) * 90 + (rand - 0.5f) * 20, e2.y, Mathf.Round(e2.z / 90) * 90 + (rand - 0.5f) * 20); } // it settles, roughly flat
                    }
                }
                if (shattered)
                {
                    if (rand < 0.35f) SpawnParticles(hinge + dir * (rand * Hg) + axis * ((rand - 0.5f) * wide) + V(0, 1, 0), 1, 1.2f, rand < 0.5f ? ashMat : dustMat, 0.6f, 3.5f, -1.2f); // the wreck smoking
                    return;
                }
                // ---- BUCKLE: it shudders and groans, grit pours down its face ----
                if (!toppling)
                {
                    group.position = hinge + Random.insideUnitSphere * 0.18f;
                    if (rand < 0.6f) SpawnParticles(hinge + axis * ((rand - 0.5f) * wide) + V(0, rand * bsz.y, 0) - dir * 0.3f, 2, 1.5f, dustMat, 0.35f, 1.4f, 6);
                    P.shake = Mathf.Max(P.shake, 0.25f);
                    if (T >= 0.25f && T - dt < 0.25f) { Sfx("growlLow"); Sfx("bossDeath", 0.35f); }
                    if (T >= 0.55f && T - dt < 0.55f) foreach (var c in chunks) if (c.row == ny - 1 && rand < 0.25f) Release(c, Random.insideUnitSphere * 2 + V(0, 1, 0)); // the first blocks drop off the roof
                    if (T >= 0.9f) { toppling = true; group.position = hinge; Sfx("growlLow"); Sfx("roarShort", 0.5f); }
                    return;
                }
                // ---- TOPPLE: over it goes, pivoting on its front edge, faster and faster ----
                om += (3 * g0 / (2 * Mathf.Max(6, Hg))) * Mathf.Sin(Mathf.Max(0.06f, th)) * dt; th += om * dt;
                group.rotation = Quaternion.AngleAxis(th * Mathf.Rad2Deg, axis);
                P.shake = Mathf.Max(P.shake, 0.3f + th * 0.25f);
                shedT -= dt;
                if (shedT <= 0)
                {
                    shedT = 0.07f;
                    // blocks shed off its back and roofline, dust streaming off it
                    foreach (var c in chunks) if (!c.dyn && c.row >= ny - 2 && rand < 0.06f) Release(c, -dir * (2 + rand * 3) + Random.insideUnitSphere * 2);
                    var anyC = chunks[Random.Range(0, chunks.Count)]; if (anyC.t) SpawnParticles(anyC.t.position, 3, 3, dustMat, 0.6f, 2.0f, -0.5f);
                }
                if (!fx1 && th > 0.35f) { fx1 = true; var c = chunks[Random.Range(0, chunks.Count)]; if (c.t) { Blast(c.t.position, 6, 0xff7a20); SpawnParticles(c.t.position, 40, 12, emberMat, 0.15f, 1.4f); Sfx("boom", 0.8f); } }   // a gas main goes up
                if (!fx2 && th > 0.75f) { fx2 = true; var c = chunks[Random.Range(0, chunks.Count)]; if (c.t) { Blast(c.t.position, 7, 0xff7a20); SpawnParticles(c.t.position, 50, 14, emberMat, 0.15f, 1.4f); Sfx("boom", 0.9f); } }
                // ---- SNAP: halfway down it breaks in two, the top floors tearing free ----
                if (!snapped && th > 0.62f)
                {
                    snapped = true; Sfx("shatter"); Sfx("slam", 0.8f);
                    int cut = Mathf.Max(2, (ny * 2) / 3);
                    foreach (var c in chunks) if (!c.dyn && c.row >= cut) Release(c, dir * (2 + rand * 2) + V(0, 1.5f + rand * 2, 0) + Random.insideUnitSphere * 1.5f);
                    var mid = hinge + Quaternion.AngleAxis(th * Mathf.Rad2Deg, axis) * V(0, cut * cell.y, 0);
                    SpawnParticles(mid, 80, 12, dustMat, 0.8f, 2.4f, -0.3f); SpawnParticles(mid, 30, 10, emberMat, 0.12f, 1.0f);
                }
                // ---- IMPACT: it slams into the square ----
                if (th >= 1.38f)
                {
                    shattered = true; Sfx("slam"); Sfx("boom"); Sfx("bossDeath", 0.6f);
                    P.shake = Mathf.Max(P.shake, 1.8f); slowmoT = Mathf.Max(slowmoT, 0.55f); whiteFlash = Mathf.Max(whiteFlash, 0.12f);
                    foreach (var c in chunks) if (!c.dyn) Release(c, V(0, 2 + rand * 4, 0) + axis * ((rand - 0.5f) * 6) + dir * rand * 3);
                    // the wall of dust rolling out along the length of it, and a ring of it bursting out
                    for (int k = 0; k <= 10; k++) { var lp = hinge + dir * (Hg * k / 10f); SpawnParticles(lp + V(0, 1.5f, 0), 22, 14, dustMat, 1.0f, 3.4f, -0.4f); }
                    ShockRing(hinge + dir * Hg * 0.5f, Hg * 0.8f, 0xb0a090, 1.0f); ShockRing(hinge + dir * Hg * 0.5f, Hg * 1.3f, 0xc8b8a0, 1.4f);
                    Blast(hinge + dir * Hg * 0.6f + V(0, 2, 0), 10, 0xff8030);
                    // anyone under it is hit (the strip was marked) - and the blast of air shoves everyone near
                    if (!P.dead)
                    {
                        var o = P.pos - hinge; float along = Vector3.Dot(o, dir), lat = Mathf.Abs(Vector3.Dot(o, axis));
                        if (hurt && along > 0 && along < Hg + 2 && lat < wide / 2 + 1 && P.pos.y < 6) { DamagePlayer(26, hinge + dir * along); P.vel += axis * Mathf.Sign(Vector3.Dot(o, axis) + 0.01f) * 16 + V(0, 9, 0); P.onGround = false; }
                        else if (along > -10 && along < Hg + 14 && lat < wide / 2 + 14) { P.vel += (axis * Mathf.Sign(Vector3.Dot(o, axis) + 0.01f) + dir * 0.3f) * 9 + V(0, 4, 0); P.onGround = false; }
                    }
                    // the stump where it stood, and its heap: burning
                    for (int q = 0; q < 10; q++) { var g = Deco(Geo.Icosahedron(1), q % 2 == 0 ? concM : concDarkM, mb.center.x + (rand - 0.5f) * bsz.x * 0.85f, 0.6f, mb.center.z + (rand - 0.5f) * bsz.z * 0.85f, rand * 3, rand * 3, rand * 3); g.transform.localScale = V(2.0f + rand * 2, 1.0f + rand * 1.4f, 2.0f + rand * 2); Ig(g); }
                    for (int q = 0; q < 8; q++) Ig(Deco(Geo.Box(0.3f, 0.3f, 4 + rand * 4), rebarM, mb.center.x + (rand - 0.5f) * bsz.x * 0.7f, 1.4f, mb.center.z + (rand - 0.5f) * bsz.z * 0.7f, (rand - 0.5f) * 1.2f, rand * 3, (rand - 0.5f) * 1.2f));
                    var fg = Mk.Glow(levelRoot, 0xff6010, Mathf.Min(bsz.x, bsz.z), 0.3f); fg.transform.localPosition = V(mb.center.x, 1.5f, mb.center.z); flames.Add(fg.transform);
                    var fg2 = Mk.Glow(levelRoot, 0xff6010, 8, 0.25f); fg2.transform.localPosition = hinge + dir * Hg * 0.6f + V(0, 1.5f, 0); flames.Add(fg2.transform);
                }
            });
            SolidAABB(V(b.mn.x + 1.5f, 0, b.mn.z + 1.5f), V(b.mx.x - 1.5f, 1.6f, b.mx.z - 1.5f)); // the stump (low: no wall to stun him on any more)
            AddStyle("DEMOLITION", 250);
            if (testMode && testOut != null) System.IO.File.AppendAllText(System.IO.Path.Combine(testOut, "events.txt"), $"{testT:F1} BUILDING DOWN at {b.c}\n");
        }
        void HollowReset() { hkBuilds.Clear(); hkCars.Clear(); hkChunks.Clear(); hkThrows.Clear(); hkDrops.Clear(); hkHeldCar = null; hkHeld = null; }

        // ================= the arena: the old civic square, ringed by the buildings he tears apart =================
        void BuildBrokenCrown()
        {
            HollowReset(); DeadMats(); Arena(72, 20);
            Flat(Geo.Plane(144, 144), sidewalkM, 0, 0.01f, 0);
            Flat(Geo.Circle(44, 64), Mats.Std(0x6a645a, 0.95f), 0, 0.02f, 0); Flat(Geo.Ring(43, 44.5f, 64), concDarkM, 0, 0.03f, 0);
            for (int k = 0; k < 16; k++) { float a = k * Mathf.PI / 8; Ig(Deco(Geo.Box(0.2f, 0.03f, 40), concDarkM, Mathf.Cos(a) * 20, 0.035f, Mathf.Sin(a) * 20, 0, -a + Mathf.PI / 2, 0)); }
            Road(0, 58, 144, 12, true); Road(0, -58, 144, 12, true); Road(58, 0, 100, 12, false); Road(-58, 0, 100, 12, false);
            // the buildings round the square; loose chunks on their tops are what he tears off
            var ring = new (float x, float z, float w, float d, float h, int type)[] { (-44, -44, 18, 16, 26, 0), (0, -62, 22, 14, 32, 1), (44, -44, 18, 16, 22, 2), (62, 0, 14, 24, 28, 0), (44, 44, 18, 16, 18, 4), (0, 64, 24, 12, 12, 2), (-44, 44, 16, 16, 24, 1), (-62, 0, 14, 24, 20, 0) };
            int si = 0;
            foreach (var b in ring)
            {
                int ch0 = levelRoot.childCount, c0 = colliders.Count; var hb = new HBuild { mn = V(b.x - b.w / 2, 0, b.z - b.d / 2), mx = V(b.x + b.w / 2, b.h, b.z + b.d / 2), c = V(b.x, 0, b.z), w = b.w, d = b.d };
                CityBuilding(b.x, b.z, b.w, b.d, b.h, b.type, 900 + si++);
                float top = b.type == 4 ? b.h * 0.45f : b.h; var toward = V(-b.x, 0, -b.z).normalized;
                for (int k = 0; k < 3; k++)
                {
                    float u = (k - 1) * 0.3f; var side = V(-toward.z, 0, toward.x);
                    var cp = V(b.x, 0, b.z) + toward * (Mathf.Min(b.w, b.d) / 2 - 1.5f) + side * u * Mathf.Max(b.w, b.d);
                    var ch = Deco(Geo.Box(3.2f, 2.2f, 2.6f), b.type == 1 ? glassM : b.type == 2 ? brickM : concM, cp.x, top + 1.1f, cp.z, 0, Mathf.Atan2(toward.x, toward.z), 0);
                    Ig(Deco(Geo.Box(3.4f, 0.3f, 2.8f), concDarkM, cp.x, top + 2.3f, cp.z, 0, Mathf.Atan2(toward.x, toward.z), 0)).transform.SetParent(ch.transform, true);
                    Ig(ch); var hc0 = new HChunk { t = ch.transform }; hkChunks.Add(hc0); hb.chunks.Add(hc0);
                }
                for (int i = ch0; i < levelRoot.childCount; i++) hb.objs.Add(levelRoot.GetChild(i).gameObject);
                hb.cols.AddRange(colliders.GetRange(c0, colliders.Count - c0));
                hkBuilds.Add(hb);
            }
            // the cars left in the square - and that he picks up
            foreach (var (x, z, ry, kind) in new[] { (-20f, -6f, 0.4f, 0), (18f, -10f, 1.9f, 1), (-26f, 18f, 2.6f, 0), (24f, 16f, 0.9f, 2), (-8f, 26f, 1.4f, 0), (10f, -30f, 0.2f, 0), (-30f, -26f, 1.0f, 1), (32f, -26f, 2.2f, 0), (0f, 12f, 0.7f, 2), (-36f, 4f, 1.6f, 0) })
            {
                var piv = Mk.Piv(levelRoot, x, 0, z); var hc = new HCar { t = piv.t, pos = V(x, 0, z), ry = ry };
                Vehicle(0, 0, ry, kind, ((int)(x + z) & 1) == 0, 0, piv.t);
                float c = Mathf.Abs(Mathf.Cos(ry)), s = Mathf.Abs(Mathf.Sin(ry)); float W = kind == 1 ? 2.1f : 1.95f, L = kind == 1 ? 5.2f : kind == 2 ? 5.4f : 4.5f; float hx = c * W / 2 + s * L / 2, hz = s * W / 2 + c * L / 2;
                hc.mn = V(x - hx * 0.95f, 0, z - hz * 0.95f); hc.mx = V(x + hx * 0.95f, 1.6f, z + hz * 0.95f); hc.solid = SolidAABB(hc.mn, hc.mx);
                hkCars.Add(hc);
            }
            // the dry fountain at the very centre of the square, its basin broken open on two sides: the portal rises from it
            {
                var basin = Mats.Std(0x8a847a, 0.9f, 0, 0, GrimeTex()); var cap = Mats.Std(0xa8a294, 0.85f, 0, 0, GrimeTex());
                Flat(Geo.Circle(5.2f, 40), Mats.Std(0x3a3632, 0.95f, 0, 0, GrimeTex()), 0, 0.04f, 0);
                for (int k = 0; k < 20; k++)
                {
                    float a = k * Mathf.PI / 10; if (k == 4 || k == 5 || k == 14 || k == 15) { Ig(Deco(Geo.Icosahedron(0.45f), basin, Mathf.Cos(a) * 6.4f, 0.2f, Mathf.Sin(a) * 6.4f, k, k, 0)); continue; } // the breaks, their rubble
                    var px = Mathf.Cos(a) * 5.6f; var pz = Mathf.Sin(a) * 5.6f;
                    Deco(Geo.Box(1.85f, 0.5f, 0.7f), basin, px, 0.25f, pz, 0, -a + Mathf.PI / 2, 0); Deco(Geo.Box(1.95f, 0.12f, 0.85f), cap, px, 0.56f, pz, 0, -a + Mathf.PI / 2, 0);
                    SolidAABB(V(px - 0.6f, 0, pz - 0.6f), V(px + 0.6f, 0.62f, pz + 0.6f)); // (low enough to step over)
                }
                Deco(Geo.Cyl(0.8f, 1.0f, 1.4f, 12), basin, 0, 0.7f, -3.8f); Deco(Geo.Cyl(0.5f, 0.75f, 0.6f, 10), cap, 0, 1.6f, -3.8f, 0.3f, 0, 0.2f); // the broken centre column, pushed to one side
                for (int k = 0; k < 7; k++) { float a = k * 0.9f; Ig(Flat(Geo.Plane(0.08f, 1.2f + k % 3), Mats.Trans(0x0a0806, 0.7f), Mathf.Cos(a) * 2.4f, 0.05f, Mathf.Sin(a) * 2.4f)).transform.localRotation = Quaternion.Euler(-90, k * 51, 0); }
            }
            // the monument: a plinth whose statue is gone, its crowned head lying cracked beside it
            {
                float mx = -28, mz = -12;
                Box(mx, 0, mz, 8, 2.4f, 8, concDarkM); Box(mx, 2.4f, mz, 6, 1.2f, 6, concM); Deco(Geo.Box(5.4f, 0.8f, 0.1f), Mats.Std(0x6a5a3a, 0.5f, 0.6f), mx, 1.4f, mz + 4.06f);
                foreach (var sx in new[] { -1f, 1f }) Deco(Geo.Box(1.0f, 2.2f, 1.2f), concM, mx + sx * 1.7f, 4.7f, mz, 0.1f * sx, 0, 0.08f * sx); // the statue's feet, snapped at the shins
                var gold = Mats.Std(0x8a7038, 0.4f, 0.7f, 0, RustTex());
                Deco(Geo.Sphere(1.6f, 14, 10), concM, mx + 5.6f, 1.3f, mz + 3.5f, 0.4f, 0.8f, 1.2f); Deco(Geo.Cyl(1.5f, 1.4f, 0.6f, 14), gold, mx + 5.0f, 2.4f, mz + 4.1f, 0.6f, 0.5f, 1.3f); SolidAABB(V(mx + 4, 0, mz + 1.8f), V(mx + 7.4f, 2.6f, mz + 5.2f));
                var arm = Deco(Geo.Box(0.9f, 0.9f, 4.4f), concM, mx - 5, 0.45f, mz + 2, 0, 0.6f, 0.1f); SolidAABB(V(mx - 7, 0, mz), V(mx - 3, 0.9f, mz + 4)); // a fallen arm, the sceptre still in its fist
                Deco(Geo.Cyl(0.15f, 0.15f, 3.2f, 8), gold, mx - 6.6f, 0.5f, mz + 4.2f, Mathf.PI / 2, 0.6f, 0);
            }
            // a bus thrown on its side, the square's barricades of sandbags, scorch marks from the fighting
            { var bp = Mk.Piv(levelRoot, 36, 1.3f, 2); bp.SetRot(0, 0.15f, Mathf.PI / 2); Vehicle(0, 0, 0, 3, true, 0, bp.t); SolidAABB(V(34.4f, 0, -4), V(38.6f, 2.8f, 8)); }
            var bag = Mats.Std(0x6a5e44, 0.95f, 0, 0, ClothTex2());
            foreach (var (cx, cz, a0) in new[] { (-10f, 30f, 0.2f), (18f, 26f, -0.4f), (-34f, 20f, 1.1f) })
                for (int k = 0; k < 7; k++) { float a = a0 + (k - 3) * 0.22f; float x = cx + Mathf.Sin(a) * 4, z = cz + Mathf.Cos(a) * 4 - 4; for (int l = 0; l < 2; l++) Deco(GeoX.Capsule(0.28f, 0.6f, 3, 8), bag, x, 0.25f + l * 0.42f, z, 0, a, Mathf.PI / 2).transform.localScale = V(1, 1, 0.75f); SolidAABB(V(x - 0.55f, 0, z - 0.55f), V(x + 0.55f, 0.8f, z + 0.55f)); }
            for (int k = 0; k < 9; k++) { float a = k * 2.2f, r = 12 + (k * 7) % 26; Ig(Flat(Geo.Circle(1.4f + (k % 3) * 0.8f, 18), Mats.Trans(0x0a0806, 0.55f), Mathf.Cos(a) * r, 0.045f, Mathf.Sin(a) * r)); }
            // the mess: debris, burning barrels, lamps
            foreach (var p in new[] { V(-14, 0, 36), V(16, 0, 38), V(-40, 0, -24), V(22, 0, -40), V(-24, 0, -38), V(30, 0, 14) }) Debris(p.x, p.z, 2.4f);
            foreach (var p in new[] { V(-12, 0, -16), V(14, 0, 4), V(-6, 0, 40), V(30, 0, 30), V(-30, 0, 30) }) BurnBarrel(p.x, p.z);
            for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4 + 0.39f; StreetLampS(Mathf.Cos(a) * 42, Mathf.Sin(a) * 42, a + Mathf.PI); }
            foreach (var sx in new[] { -1f, 1f }) { PadAt(sx * 34, 40, 26); PadAt(sx * 40, -34, 26); }
        }

        // ================= the model =================
        HollowRig BuildHollow(Ent e)
        {
            var conc = EM(e, Mats.Std(0x6e6860, 0.92f)); var conc2 = EM(e, Mats.Std(0x4a4640, 0.95f)); var steel = EM(e, Mats.Std(0x4a4c50, 0.45f, 0.75f)); var rust = EM(e, Mats.Std(0x6a3a20, 0.75f, 0.5f));
            var glow = EM(e, Mats.Basic(0xff8030)); var fire = EM(e, Mats.Add(0xff6010, 0.8f, null, true, true)); var gold = EM(e, Mats.Std(0x8a6a30, 0.35f, 0.8f, 0x100800)); var tire = EM(e, Mats.Std(0x141414, 0.9f));
            var facade = EM(e, Mats.Basic(0x9a8a7a, FacadeTex()));
            hkConc = conc; hkSteel = steel; hkRust = rust; hkGold = gold;
            var r = new HollowRig { glow = glow, fire = fire };
            r.root = Mk.Piv(e.g.transform);
            r.hips = Mk.Piv(r.root, 0, 1.6f, 0);
            // the hips: a crushed truck chassis, concrete heaped on it
            Mk.Msh(r.hips, Geo.Box(1.6f, 0.4f, 0.9f), steel, 0, 0, 0); Mk.Msh(r.hips, Geo.Box(1.4f, 0.35f, 0.8f), rust, 0, 0.3f, 0.05f, 0.1f, 0, 0.05f);
            foreach (var sx in new[] { -1f, 1f }) Mk.Msh(r.hips, Geo.Cyl(0.26f, 0.26f, 0.2f, 12), tire, sx * 0.85f, 0, 0.2f, 0, 0, Mathf.PI / 2);
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var th = Mk.Piv(r.hips, sx * 0.5f, -0.1f, 0);
                Mk.Msh(th, Geo.Box(0.5f, 0.8f, 0.55f), conc, 0, -0.4f, 0); Mk.Msh(th, Geo.Box(0.12f, 0.9f, 0.12f), steel, sx * 0.25f, -0.4f, 0.2f); for (int k = 0; k < 3; k++) Mk.Msh(th, Geo.Cyl(0.015f, 0.015f, 0.35f, 4), rust, (k - 1) * 0.15f, -0.05f, -0.28f, -0.6f, 0, (k - 1) * 0.4f);
                var kn = Mk.Piv(th, 0, -0.8f, 0);
                Mk.Msh(kn, Geo.Cyl(0.26f, 0.26f, 0.28f, 14), tire, 0, 0, 0.02f, 0, 0, Mathf.PI / 2); Mk.Msh(kn, Geo.Cyl(0.14f, 0.14f, 0.3f, 8), steel, 0, 0, 0.02f, 0, 0, Mathf.PI / 2);
                Mk.Msh(kn, Geo.Box(0.45f, 0.75f, 0.5f), conc2, 0, -0.42f, 0); Mk.Msh(kn, Geo.Box(0.5f, 0.12f, 0.55f), steel, 0, -0.3f, 0);
                Mk.Msh(kn, Geo.Box(0.6f, 0.2f, 0.85f), conc, 0, -0.82f, 0.12f); for (int k = 0; k < 3; k++) Mk.Msh(kn, Geo.Icosahedron(0.1f), conc2, (k - 1) * 0.18f, -0.85f, 0.55f);
                if (i == 0) r.thigh[0] = th; else r.thigh[1] = th; r.knee[i] = kn;
            }
            // the torso: a ribcage of bent girders round a burning hollow, a slab of a building's front for a back, concrete plates
            r.torso = Mk.Piv(r.hips, 0, 0.25f, 0);
            Mk.Msh(r.torso, Geo.Box(1.5f, 1.4f, 0.25f), facade, 0, 0.8f, -0.45f, 0.08f, 0, 0);
            for (int k = 0; k < 5; k++) { var rib = Mk.Msh(r.torso, Geo.Torus(0.62f, 0.06f, 12, 4, Mathf.PI * 1.15f), k % 2 == 0 ? steel : rust, 0, 0.3f + k * 0.26f, -0.12f, Mathf.PI / 2, 0, -0.3f); rib.transform.localScale = V(1.2f - Mathf.Abs(k - 2) * 0.06f, 0.85f, 1); }
            Mk.Msh(r.torso, Geo.Box(0.16f, 1.3f, 0.16f), steel, 0, 0.85f, 0.42f); // the sternum: an I-beam
            foreach (var (x, y, rz) in new[] { (-0.45f, 1.3f, 0.2f), (0.5f, 1.25f, -0.25f), (0.3f, 0.45f, 0.1f) }) Mk.Msh(r.torso, Geo.Box(0.5f, 0.4f, 0.12f), conc, x, y, 0.45f, 0.1f, 0, rz);
            r.core = Mk.Msh(r.torso, Geo.Icosahedron(0.32f), glow, 0, 0.85f, 0); var cg = Gl(r.torso, 0xff6010, 2.5f, 0.75f); cg.transform.localPosition = V(0, 0.85f, 0.2f);
            for (int k = 0; k < 4; k++) Mk.Msh(r.torso, Geo.Cyl(0.02f, 0.02f, 0.9f + k * 0.2f, 4), tire, (k - 1.5f) * 0.3f, 0.2f, 0.4f, 0.15f, 0, 0); // cables hanging out of him
            // the shoulders: a car wedged into the left, a heap of concrete and girders on the right
            var carP = Mk.Piv(r.torso, -0.95f, 1.55f, -0.05f); carP.SetRot(0.3f, 0.4f, 0.9f); carP.t.localScale = Vector3.one * 0.2f;
            Vehicle(0, 0, 0, 0, true, 0, carP.t);
            Mk.Msh(r.torso, Geo.Icosahedron(0.4f), conc, 0.85f, 1.6f, 0, 0.5f, 0.3f, 0.2f); for (int k = 0; k < 3; k++) Mk.Msh(r.torso, Geo.Box(0.08f, 0.7f, 0.08f), steel, 0.75f + k * 0.12f, 1.85f, (k - 1) * 0.15f, 0.2f, 0, -0.5f + k * 0.2f);
            // the head: a block of concrete, hollow eyes with fire in them, a jaw of rebar teeth, the broken crown
            r.neck = Mk.Piv(r.torso, 0, 1.6f, 0.05f);
            for (int k = 0; k < 3; k++) Mk.Msh(r.neck, Geo.Cyl(0.05f, 0.05f, 0.3f, 6), steel, (k - 1) * 0.12f, 0.1f, 0);
            r.head = Mk.Piv(r.neck, 0, 0.25f, 0.05f);
            var skull = Mk.Msh(r.head, Geo.Box(0.7f, 0.6f, 0.62f), conc, 0, 0.25f, 0); var brow = Mk.Msh(r.head, Geo.Box(0.78f, 0.14f, 0.2f), conc2, 0, 0.42f, 0.25f, 0.2f, 0, 0);
            var eyes = new List<GameObject>();
            foreach (var sx in new[] { -1f, 1f }) { eyes.Add(Mk.Msh(r.head, Geo.Box(0.18f, 0.14f, 0.08f), Mats.Std(0x0a0606, 0.9f), sx * 0.17f, 0.3f, 0.29f)); eyes.Add(Mk.Msh(r.head, Geo.Sphere(0.06f, 8, 6), glow, sx * 0.17f, 0.3f, 0.3f)); }
            var eg = Gl(r.head, 0xff6010, 1.15f, 0.8f); eg.transform.localPosition = V(0, 0.3f, 0.4f);
            r.jaw = Mk.Piv(r.head, 0, 0.0f, 0.05f);
            var jaw = Mk.Msh(r.jaw, Geo.Box(0.6f, 0.15f, 0.5f), conc2, 0, -0.05f, 0.05f);
            var teeth = new List<GameObject> { skull, brow, jaw }; teeth.AddRange(eyes);
            for (int k = 0; k < 6; k++) teeth.Add(Mk.Msh(r.jaw, Geo.Cyl(0.015f, 0.025f, 0.16f, 4), rust, (k - 2.5f) * 0.09f, 0.06f, 0.28f));
            Mk.Head(teeth.ToArray());
            var crown = Mk.Piv(r.head, 0.04f, 0.6f, 0); crown.SetRot(0.12f, 0, -0.18f); r.crown = crown.t;
            Mk.Msh(crown, Geo.Torus(0.42f, 0.06f, 20, 5, Mathf.PI * 1.65f), gold, 0, 0, 0, Mathf.PI / 2, 0, 0);
            for (int k = 0; k < 7; k++) { float a = k * Mathf.PI * 2 / 8; if (k == 5) continue; Mk.Msh(crown, Geo.Cone(0.07f, k % 2 == 0 ? 0.36f : 0.24f, 4), gold, Mathf.Cos(a) * 0.42f, 0.16f, Mathf.Sin(a) * 0.42f, 0, 0, (k == 3 ? 0.5f : 0)); }
            Mk.Msh(crown, Geo.Octahedron(0.06f), glow, 0, 0.05f, 0.43f);
            // arms: a crane boom for the upper arm, a bundle of girders below, hands of rebar claws
            N Arm(int sx, out N elbow, out N hand)
            {
                var sh = Mk.Piv(r.torso, sx * 0.95f, 1.45f, 0);
                Mk.Msh(sh, Geo.Box(0.3f, 0.9f, 0.3f), sx < 0 ? rust : steel, 0, -0.45f, 0);
                for (int k = 0; k < 4; k++) Mk.Msh(sh, Geo.Box(0.03f, 0.4f, 0.03f), steel, 0.12f * (k % 2 == 0 ? 1 : -1), -0.15f - k * 0.2f, 0.15f, 0, 0, k % 2 == 0 ? 0.6f : -0.6f); // the boom's lattice
                elbow = Mk.Piv(sh, 0, -0.9f, 0);
                Mk.Msh(elbow, Geo.Cyl(0.18f, 0.18f, 0.34f, 10), tire, 0, 0, 0, 0, 0, Mathf.PI / 2);
                for (int k = 0; k < 3; k++) Mk.Msh(elbow, Geo.Box(0.11f, 0.85f, 0.11f), k == 1 ? rust : steel, (k - 1) * 0.12f, -0.45f, (k % 2) * 0.08f);
                Mk.Msh(elbow, Geo.Box(0.4f, 0.3f, 0.35f), conc, 0, -0.5f, 0);
                hand = Mk.Piv(elbow, 0, -0.9f, 0);
                Mk.Msh(hand, Geo.Box(0.38f, 0.22f, 0.3f), conc2, 0, -0.05f, 0);
                for (int k = 0; k < 4; k++) { var f = Mk.Piv(hand, (k - 1.5f) * 0.1f, -0.12f, 0.1f); f.SetRot(0.5f, 0, (k - 1.5f) * 0.12f); Mk.Msh(f, Geo.Cyl(0.02f, 0.035f, 0.45f, 5), rust, 0, -0.22f, 0); }
                var th = Mk.Piv(hand, sx * 0.2f, -0.05f, 0.1f); th.SetRot(0.3f, 0, sx * 0.8f); Mk.Msh(th, Geo.Cyl(0.02f, 0.035f, 0.32f, 5), rust, 0, -0.16f, 0);
                return sh;
            }
            r.rSh = Arm(1, out r.rEl, out r.rHand); r.lSh = Arm(-1, out r.lEl, out r.lHand);
            HollowExtras(e, r);
            e.mats.Clear(); e.mats.Add(conc); e.mats.Add(conc2); e.mats.Add(steel); e.mats.Add(rust);
            return r;
        }

        Ent SpawnHollowKing()
        {
            var e = NewEnt("boss"); var sp = LEVEL.bossSpawn;
            e.hk = BuildHollow(e);
            e.g.transform.localScale = Vector3.one * 4.4f;
            var near = NearestPlayer(V(sp.x, 0, sp.y)); e.pos = V(sp.x, 0, sp.y); e.yaw = Mathf.Atan2(near.pos.x - sp.x, near.pos.z - sp.y);
            e.boss = 4; e.name = "THE HOLLOW KING"; e.heavy = true; e.s = 4.4f;
            e.hp = e.maxHp = HOLLOW_HP * CoopBossHp; e.headY = 15f; e.bodyY = 10.5f; e.hitR = 4.6f; e.r = 2.6f; e.h = 15;
            // the entrance: he comes down out of the sky like a burning meteor; a warning ring grows where he'll land
            e.invuln = true; HState(e, "fall", 9);
            e.pos.y = 260; e.vel = V(0, -35, 0); // (from far up - out of sight above the smoke - so you watch him come down the whole way)
            FinishEnt(e); SyncEnt(e); boss = e;
            WarnDisc(V(sp.x, 0, sp.y), 11, 4.0f, 0xff5020);
            Sfx("alarm"); Sfx("charge"); Sfx("growlLow"); P.shake = Mathf.Max(P.shake, 0.3f);
            return e;
        }
        static void HState(Ent e, string s, float dur) { e.state = s; e.stateT = dur; e.vidx = 0; e.hitPlayer = false; if (e.hk != null) e.hk.t = 0; }
        Vector3 HFwd(Ent e) => V(Mathf.Sin(e.yaw), 0, Mathf.Cos(e.yaw));

        // ================= the fight =================
        void UpdateHollowKing(Ent e, float dt)
        {
            var r = e.hk; UpdateTraitorFx(dt); UpdateHollowFx(dt);
            float sp = e.phase2 ? 1.55f : 1.15f;
            float sdt = e.state == "fall" || e.state == "land" ? dt : dt * sp;
            r.t += sdt; e.stateT -= sdt; e.flash -= dt;
            float dx = Tgt.pos.x - e.pos.x, dz = Tgt.pos.z - e.pos.z, dist = Hypot(dx, dz), dy = Tgt.pos.y - e.pos.y; if (dist == 0) dist = 1; // (co-op: he hunts Tgt; hits are tested on the local P)
            float myDy = P.pos.y - e.pos.y;
            var fwd = HFwd(e); bool face = true; float move = 0;
            // the enraged roar at half health
            if (!e.phase2 && e.hp < e.maxHp * 0.5f && e.state != "fall" && e.state != "land" && e.state != "carHold")
            { e.phase2 = true; ReleaseHeld(e, false); HState(e, "roar", 1.6f); Sfx("roar"); Banner("ENRAGED", "THE HOLLOW KING", "#ff6020"); P.shake = 1; AddRing(V(e.pos.x, 0, e.pos.z), 20, 36, 14, 0xff6020); BossBarTint(Mats.Css("#ff8040"), Mats.Css("#901800")); audioMgr.Intensity(); }
            switch (e.state)
            {
                case "fall":
                {
                    // plunging out of the sky trailing fire and smoke; the air roars round him; he hits the square feet first
                    face = false; e.vel.x = e.vel.z = 0; e.vel.y = Mathf.Max(e.vel.y - GRAV * 1.6f * dt, -95); e.pos.y += e.vel.y * dt;
                    P.shake = Mathf.Max(P.shake, 0.12f + 0.4f * (1 - Mathf.Clamp01(e.pos.y / 260)));
                    for (int j = 0; j < 3; j++) SpawnParticles(e.pos + V((rand - 0.5f) * 6, 6 + rand * 12, (rand - 0.5f) * 6), 1, 3, rand < 0.5f ? emberMat : ashMat, 0.4f + rand * 0.4f, 1.4f, 4);
                    if (r.t % 0.4f < dt) Sfx("dashGo", 0.6f);
                    if (e.pos.y <= 0) { e.pos.y = 0; e.vel.y = 0; HollowImpact(e); HState(e, "land", 3.2f); }
                    break;
                }
                case "land":
                {
                    // the impact crouch - one fist down in the crater - then he hauls himself up and roars
                    face = false; P.shake = Mathf.Max(P.shake, 0.3f * (1 - Mathf.Clamp01(r.t / 1.2f)));
                    if (r.t >= 1.0f && r.t - dt < 1.0f) { ShowBossBar(e); audioMgr.Start(8); StartBossIntro(e); if (!BossIntroActive) Banner($"WAVE {wave}", "", THEME.banner); } // his title a second after he lands
                    if (rand < 0.6f) SpawnParticles(V(e.pos.x + (rand - 0.5f) * 18, 1, e.pos.z + (rand - 0.5f) * 18), 2, 5, dustMat, 0.4f, 1.8f);
                    if (r.t >= 1.7f && r.t - dt < 1.7f) { Sfx("roar"); AddRing(V(e.pos.x, 0, e.pos.z), 18, 30, 10, 0xff6020); P.shake = 1.0f; }
                    if (e.stateT <= 0 && !BossIntroActive) { e.invuln = false; HState(e, "chase", 0.8f); }
                    break;
                }
                case "roar": face = false; if (e.stateT <= 0) HState(e, "chase", 0.4f); break;
                case "chase":
                    move = e.phase2 ? 8.4f : 6.6f;
                    if (e.stateT <= 0 && Auth) // co-op: the host picks; clients run the same setup when its pick arrives (NetBossBegin)
                    {
                        string a = PickHollow(e, dist);
                        NetAct(e, a); HollowBegin(e, a);
                    }
                    break;
                // ---- GROUND SLAM: both fists into the square; the ground bursts, rings roll out, cars nearby are flung ----
                case "slamWind": face = e.stateT > 0.4f; if (e.stateT <= 0) { HollowSlam(e, r.aim, 10.5f, e.phase2 ? 56 : 46); HState(e, "recover", 0.77f); } break;
                // ---- SWEEP: a great backhand of girders across everything in front of him ----
                case "sweepWind": face = false; TurnTo(e, r.aimYaw, 3, dt); if (e.stateT <= 0) { HState(e, "sweep", 0.4f); Sfx("dashGo"); } break;
                case "sweep":
                    face = false;
                    if (!e.hitPlayer && r.t > 0.12f)
                    {
                        e.hitPlayer = true; Sfx("slam", 0.8f); P.shake = Mathf.Max(P.shake, 0.6f);
                        for (int k = 0; k < 20; k++) { float a = r.aimYaw + (k / 19f - 0.5f) * 2.5f; SpawnParticles(e.pos + V(Mathf.Sin(a), 0, Mathf.Cos(a)) * (6 + rand * 12) + V(0, 1 + rand * 2, 0), 1, 5, dustMat, 0.3f, 0.6f); }
                        if (!P.dead && InFan(e.pos, r.aimYaw, 20.5f, 1.25f, P.pos) && myDy < 6) { DamagePlayer(e.phase2 ? 47 : 40, e.pos); var side = V(-fwd.z, 0, fwd.x); P.vel += side * 18 + fwd * 10 + V(0, 8, 0); P.onGround = false; }
                    }
                    if (e.stateT <= 0) HState(e, "recover", 0.63f);
                    break;
                // ---- CHARGE: head down, straight at you - into a wall and he's stunned ----
                case "chargeWind":
                    if (e.stateT > 0.25f) r.aim = V(dx / dist, 0, dz / dist);
                    if (e.vidx == 0 && e.stateT <= 0.6f) { e.vidx = 1; WarnRect(e.pos, r.aim, 60, 9, 0.6f / sp + 0.3f, 0xff5020); }
                    face = e.stateT > 0.25f; if (rand < 0.5f) SpawnParticles(e.pos + V(0, 0.5f, 0), 1, 4, dustMat, 0.3f, 0.5f);
                    if (e.stateT <= 0) { HState(e, "charge", 2.4f); e.chargeFrom = e.pos; Sfx("roar"); }
                    break;
                case "charge":
                    face = false; TurnTo(e, Mathf.Atan2(r.aim.x, r.aim.z), 10, dt);
                    e.vel.x = r.aim.x * 26 * sp; e.vel.z = r.aim.z * 26 * sp; P.shake = Mathf.Max(P.shake, 0.25f);
                    if (rand < 0.9f) SpawnParticles(e.pos + V((rand - 0.5f) * 4, 0.5f, (rand - 0.5f) * 4), 2, 6, dustMat, 0.35f, 0.6f);
                    if (r.t % 0.45f < sdt) Sfx("slam", 0.5f);
                    if (!e.hitPlayer && MeIn(e.pos, 6, 8)) { e.hitPlayer = true; DamagePlayer(e.phase2 ? 54 : 46, e.pos); P.vel += r.aim * 28 + V(0, 12, 0); P.onGround = false; P.coyote = 0; }
                    foreach (var c in hkCars) if (!c.gone && Hypot(c.pos.x - e.pos.x, c.pos.z - e.pos.z) < 5) FlingCar(c, r.aim * 14 + V(0, 10, 0)); // cars in his way are thrown aside
                    if (e.stateT <= 0) HState(e, "recover", 0.56f);
                    break;
                // ---- CAR THROW: he walks to the nearest car, squats and gets both hands under it, heaves it up over his head,
                // draws back and hurls it ----
                case "carWalk":
                {
                    var c = hkHeldCar; if (c == null || c.gone) { HState(e, "chase", 0.3f); break; }
                    float cx = c.pos.x - e.pos.x, cz = c.pos.z - e.pos.z, cd = Hypot(cx, cz);
                    face = false; TurnTo(e, Mathf.Atan2(cx, cz), 5, dt); e.vel.x = cx / cd * 8; e.vel.z = cz / cd * 8;
                    if (cd < 9 || e.stateT <= 0) HState(e, "carReach", 0.85f);
                    break;
                }
                case "carReach":
                {
                    var c = hkHeldCar; face = false; if (c == null || c.gone) { HState(e, "chase", 0.3f); break; }
                    TurnTo(e, Mathf.Atan2(c.pos.x - e.pos.x, c.pos.z - e.pos.z), 6, dt);
                    if (r.t > 0.5f && rand < 0.4f) SpawnParticles(c.pos + V((rand - 0.5f) * 3, 0.3f, (rand - 0.5f) * 3), 1, 2, dustMat, 0.2f, 0.6f);
                    if (e.stateT <= 0)
                    {
                        // tear it off the ground: its collider goes, and it starts up into his hands
                        c.gone = true; colliders.RemoveAll(cb => (cb.min - c.mn).sqrMagnitude < 0.01f && (cb.max - c.mx).sqrMagnitude < 0.01f); if (c.solid) Destroy(c.solid);
                        hkHeld = c.t; hkHeldIsCar = true; hkLiftFrom = c.t.position; hkLiftRot = c.t.rotation; Sfx("slam", 0.6f); SpawnParticles(c.pos + V(0, 0.5f, 0), 16, 5, dustMat, 0.3f, 0.8f);
                        HState(e, "carLift", 0.75f);
                    }
                    break;
                }
                // ---- SLAB: he drives both hands into the paving in front of him and wrenches up a slab of it ----
                case "slabReach":
                    face = false;
                    if (rand < 0.6f) SpawnParticles(e.pos + HFwd(e) * 7 + V((rand - 0.5f) * 5, 0.3f, (rand - 0.5f) * 5), 1, 3, dustMat, 0.25f, 0.6f);
                    if (e.stateT <= 0)
                    {
                        var sp0 = e.pos + HFwd(e) * 7; sp0.y = 0;
                        var slab = Mk.Piv(levelRoot, sp0.x, 0, sp0.z); slab.SetRot(0, e.yaw, 0);
                        Mk.Msh(slab, Geo.Box(4.2f, 0.9f, 3.2f), Mats.Std(0x6a645a, 0.92f, 0, 0, GrimeTex()), 0, 0.45f, 0); Mk.Msh(slab, Geo.Box(4.3f, 0.12f, 3.3f), sidewalkM, 0, 0.93f, 0);
                        for (int k = 0; k < 5; k++) Mk.Msh(slab, Geo.Cyl(0.05f, 0.05f, 1.2f, 4), rebarM, (k - 2) * 0.8f, 0.1f, 1.6f, 0.6f, 0, (k - 2) * 0.2f);
                        foreach (var rr in slab.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                        Flat(Geo.Plane(4.6f, 3.6f), Mats.Std(0x0c0a08, 0.95f), sp0.x, 0.05f, sp0.z).transform.localRotation = Quaternion.Euler(-90, e.yaw * Mathf.Rad2Deg, 0); // the hole it leaves
                        SpawnParticles(sp0 + V(0, 0.6f, 0), 40, 9, dustMat, 0.4f, 1.1f); Sfx("boom", 0.7f); Sfx("slam"); P.shake = Mathf.Max(P.shake, 0.5f);
                        hkHeld = slab.t; hkHeldIsCar = false; hkLiftFrom = slab.t.position; hkLiftRot = slab.t.rotation;
                        HState(e, "carLift", 0.75f);
                    }
                    break;
                case "carLift":
                {
                    // up it comes, between both hands, to rest overhead
                    face = false; float k = Smoother(Mathf.Clamp01(r.t / 0.7f));
                    if (hkHeld) { var top = HandsTop(e); hkHeld.position = Vector3.Lerp(hkLiftFrom, top, k) + V(0, Mathf.Sin(k * Mathf.PI) * 2.5f, 0); hkHeld.rotation = Quaternion.Slerp(hkLiftRot, Quaternion.Euler(0, e.yaw * Mathf.Rad2Deg + 90, 8), k); if (rand < 0.4f) SpawnParticles(hkHeld.position, 1, 2, dustMat, 0.25f, 0.5f, 3); }
                    if (e.stateT <= 0) HState(e, "carHold", 0.65f);
                    break;
                }
                case "carHold":
                    face = true; if (hkHeld) { hkHeld.position = HandsTop(e); hkHeld.rotation = Quaternion.Euler(0, e.yaw * Mathf.Rad2Deg + 90, 8); }
                    if (e.vidx == 0 && e.stateT < 0.5f) { e.vidx = 1; r.aim = PredictLand(HK_TOF); WarnDisc(r.aim, 6, 0.5f / sp + HK_TOF, 0xff5020); }
                    if (e.stateT <= 0) { ThrowHeld(e, r.aim, HK_TOF, 6, e.phase2 ? 40 : 34, hkHeldIsCar); HState(e, "throw", 0.45f); }
                    break;
                // ---- RIP: he turns to a building, reaches out and tears a chunk off its roofline; it flies to his hand; he hurls it ----
                case "ripReach":
                    face = false;
                    if (hkHeld)
                    {
                        var to = hkHeld.position - e.pos; TurnTo(e, Mathf.Atan2(to.x, to.z), 6, dt);
                        hkHeld.position += Random.insideUnitSphere * 0.08f; // it shudders as his grip closes on it
                        if (rand < 0.5f) SpawnParticles(hkHeld.position - V(0, 1, 0), 1, 3, dustMat, 0.25f, 0.8f, 4);
                    }
                    if (e.stateT <= 0) { HState(e, "rip", 1.0f); Sfx("boom", 0.6f); if (hkHeld) { SpawnParticles(hkHeld.position, 30, 9, dustMat, 0.35f, 1.2f); for (int j = 0; j < 8; j++) particles.Add(new Pt { pos = hkHeld.position, scale = Vector3.one * (0.3f + rand * 0.4f), mat = concDarkM, v = V((rand - 0.5f) * 8, rand * 6, (rand - 0.5f) * 8), life = 2.4f, grav = GRAV, hasSpin = true, spin = V(rand * 8, rand * 8, 0) }); } }
                    break;
                case "rip":
                    face = true;
                    if (hkHeld && hkHeld.parent != r.rHand.t)
                    {
                        float k = Mathf.Clamp01(r.t / 0.5f); var tgt = r.rHand.t.position; hkHeld.position = Vector3.Lerp(hkHeld.position, tgt, k * k * 0.3f + dt * 2.5f); hkHeld.Rotate(V(90, 40, 60) * dt);
                        if (Vector3.Distance(hkHeld.position, tgt) < 3 || r.t > 0.6f) { hkHeld.SetParent(r.rHand.t, true); hkHeld.localPosition = V(0, -0.35f, 0.1f); Sfx("slam", 0.5f); }
                    }
                    if (e.vidx == 0 && e.stateT < 0.5f) { e.vidx = 1; r.aim = PredictLand(HK_TOF); WarnDisc(r.aim, 5.5f, 0.5f / sp + HK_TOF, 0xff5020); }
                    if (e.stateT <= 0) { ThrowHeld(e, r.aim, HK_TOF, 5.5f, e.phase2 ? 36 : 30, false); HState(e, "throw", 0.45f); }
                    break;
                case "throw": face = false; if (e.stateT <= 0) HState(e, "recover", 0.38f); break; // the follow-through
                // ---- RUBBLE RAIN: a stamp that shakes the buildings; marked circles round you, then the rubble comes down ----
                case "rainWind":
                    face = false; if (rand < 0.8f) SpawnParticles(e.pos + V((rand - 0.5f) * 10, 14 + rand * 4, (rand - 0.5f) * 10), 1, 2, dustMat, 0.25f, 0.6f, 3);
                    if (e.stateT <= 0)
                    {
                        Sfx("slam"); P.shake = 0.8f; AddRing(V(e.pos.x, 0, e.pos.z), 16, 22, 10, 0xff6020);
                        int n = e.phase2 ? 14 : 10; float lim = LB - 4;
                        for (int k = 0; k < n; k++) { var p = k < 3 ? Tgt.pos + V(Tgt.vel.x, 0, Tgt.vel.z) * (1.2f + k * 0.15f) + V((rand - 0.5f) * 3, 0, (rand - 0.5f) * 3) : Tgt.pos + V((rand - 0.5f) * 30, 0, (rand - 0.5f) * 30); p.x = Mathf.Clamp(p.x, -lim, lim); p.z = Mathf.Clamp(p.z, -lim, lim); p.y = k < 3 ? Mathf.Max(0, FloorBelow(V(p.x, P.pos.y + 0.2f, p.z))) : 0; HollowDrop(p, 1.2f + k * 0.12f); } // (the first three land wherever you stand - a roof too)
                        HState(e, "recover", 0.56f);
                    }
                    break;
                // ---- SHRAPNEL VOLLEY: the fire in his chest flares and he coughs out a spray of burning scrap ----
                case "volleyWind":
                    face = true; if (rand < 0.8f) SpawnParticles(r.torso.t.TransformPoint(V(0, 0.85f, 0.3f)), 1, 2, emberMat, 0.15f, 0.4f);
                    if (e.stateT <= 0)
                    {
                        var from = r.torso.t.TransformPoint(V(0, 0.85f, 0.6f)); int n = e.phase2 ? 11 : 8; float tof = Vector3.Distance(from, ChestOf(Tgt)) / 40;
                        var tgt = ChestOf(Tgt) + V(Tgt.vel.x, 0, Tgt.vel.z) * tof * 0.8f;
                        for (int k = 0; k < n; k++) { var d = Quaternion.Euler((rand - 0.5f) * 6, (k - (n - 1) / 2f) * 4.5f, 0) * (tgt - from).normalized; MakeProj(null, from, d * 40, 14, 4); }
                        Sfx("volley"); Blast(from, 3, 0xff6010); HState(e, "recover", 0.49f);
                    }
                    break;
                case "stunned":
                    face = false; if (rand < 0.3f) SpawnParticles(BodyPoint(e) + V(0, 3, 0), 1, 2, sparkMat, 0.15f, 0.6f, -2);
                    if (e.stateT <= 0) HState(e, "chase", 0.8f);
                    break;
                case "recover": face = e.stateT < 0.3f; if (e.stateT <= 0) HState(e, "chase", (e.phase2 ? 0.25f : 0.5f) + rand * 0.35f); break;
            }
            // movement
            if (e.state != "fall")
            {
                if (e.state != "charge" && e.state != "carWalk") { float k = 1 - Mathf.Exp(-5 * dt); e.vel.x += (dx / dist * move - e.vel.x) * k; e.vel.z += (dz / dist * move - e.vel.z) * k; }
                e.vel.y = Mathf.Max(e.vel.y - GRAV * dt, -60);
                var res = MoveBody(ref e.pos, ref e.vel, e.r, e.h, dt, 3.2f); e.ground = res.ground; // (he wades through rubble, wrecks and debris)
                if (e.state == "charge" && res.wall && Vector3.Distance(e.pos, e.chargeFrom) > 6 && BuildingAt(e.pos, e.r + 1.5f) == null)
                {
                    // a car, a heap of rubble, a lamp post: he just barges to a halt - only a standing building stuns him
                    HState(e, "recover", 0.49f); e.vel.x = e.vel.z = 0; Sfx("slam", 0.7f); P.shake = Mathf.Max(P.shake, 0.4f); SpawnParticles(BodyPoint(e) + HFwd(e) * 3, 20, 8, dustMat, 0.3f, 0.8f);
                }
                else if (Auth && e.state == "charge" && res.wall && Vector3.Distance(e.pos, e.chargeFrom) > 6) // co-op: the host's copy decides it (and tells the others)
                {
                    // into a building: it comes down on him and he's stunned - each one he brings down leaves fewer to use, and the
                    // stuns get shorter as they run out (3.5s down to 1.5s)
                    var bld = BuildingAt(e.pos, e.r + 1.5f); int total = Mathf.Max(2, hkBuilds.Count), left = 0; foreach (var hb in hkBuilds) if (!hb.broken) left++;
                    float stunT = 1.5f + 2.0f * Mathf.Clamp01((left - 1) / (float)(total - 1));
                    BreakBuilding(bld); NetFx(WFx.Break, hkBuilds.IndexOf(bld), e.pos);
                    HState(e, "stunned", stunT); e.vel.x = e.vel.z = 0; Sfx("slam"); Sfx("boom"); P.shake = Mathf.Max(P.shake, 1.0f);
                    SpawnParticles(BodyPoint(e) + HFwd(e) * 4, 50, 14, dustMat, 0.4f, 1.2f);
                    for (int k = 0; k < 4; k++) HollowDrop(e.pos + HFwd(e) * 3 + V((rand - 0.5f) * 10, 0, (rand - 0.5f) * 10), 0.6f + k * 0.2f); // the wall he hit comes down round him
                }
            }
            if (face) TurnTo(e, Mathf.Atan2(dx, dz), e.state == "chase" ? 3 : 2, dt);
            e.pos.x = Mathf.Clamp(e.pos.x, -(LB - 5), LB - 5); e.pos.z = Mathf.Clamp(e.pos.z, -(LB - 5), LB - 5);
            AnimateHollow(e, dt);
            SyncEnt(e);
        }
        string PickHollow(Ent e, float dist)
        {
            // up on a roof or a ledge out of his reach he throws: cars, chunks of the buildings, slabs of the square, scrap, rubble
            var o = PlayerUp(e) ? new List<string> { "car", "rip", "slab", "volley", "rain", "volley" }
                  : dist < 14 ? new List<string> { "slam", "sweep", "sweep", "slam", "rain", "slab" } : dist < 32 ? new List<string> { "car", "rip", "charge", "rain", "volley", "slam", "slab" } : new List<string> { "rip", "car", "charge", "volley" };
            if (e.phase2) o.Add(dist < 14 ? "rain" : "charge");
            o.RemoveAll(s => s == e.last); var pick = o[Mathf.Min(o.Count - 1, (int)(rand * o.Count))]; e.last = pick; return pick;
        }
        HCar NearestCar(Vector3 p) { HCar best = null; float bd = 40; foreach (var c in hkCars) { if (c.gone) continue; float d = Hypot(c.pos.x - p.x, c.pos.z - p.z); if (d < bd) { bd = d; best = c; } } return best; }
        // where a throw should come down: just ahead of you, at the height of whatever you're standing on (a roof too)
        Vector3 PredictLand(float tof) { var q = Tgt ?? P; var p = q.pos + V(q.vel.x, 0, q.vel.z) * tof * 0.7f; float lim = LB - 3; p.x = Mathf.Clamp(p.x, -lim, lim); p.z = Mathf.Clamp(p.z, -lim, lim); p.y = Mathf.Max(0, FloorBelow(V(p.x, q.pos.y + 0.2f, p.z))); return p; }
        bool PlayerUp(Ent e) => (Tgt ?? P).pos.y - e.pos.y > 3.5f; // on a roof or a ledge he can't reach with his fists
        void ThrowHeld(Ent e, Vector3 target, float tof, float r, float dmg, bool car)
        {
            if (!hkHeld) return;
            var p0 = hkHeld.position; hkHeld.SetParent(levelRoot, true);
            var v = (target - p0) / tof; v.y += 0.5f * GRAV * tof;
            hkThrows.Add(new HThrow { t = hkHeld, v = v, spin = V(rand - 0.5f, rand - 0.5f, rand - 0.5f) * 300, r = r, dmg = dmg, car = car });
            hkHeld = null; hkHeldCar = null; Sfx("dashGo"); Sfx("roarShort"); P.shake = Mathf.Max(P.shake, 0.5f);
        }
        void ReleaseHeld(Ent e, bool fx) { if (!hkHeld) return; hkThrows.Add(new HThrow { t = hkHeld, v = V(0, -2, 0), spin = Vector3.zero, r = 4, dmg = 0 }); hkHeld.SetParent(levelRoot, true); hkHeld = null; hkHeldCar = null; }
        void FlingCar(HCar c, Vector3 v)
        {
            c.gone = true; colliders.RemoveAll(cb => (cb.min - c.mn).sqrMagnitude < 0.01f && (cb.max - c.mx).sqrMagnitude < 0.01f); if (c.solid) Destroy(c.solid);
            hkThrows.Add(new HThrow { t = c.t, v = v, spin = V(rand - 0.5f, rand - 0.5f, rand - 0.5f) * 200, r = 4, dmg = 20, car = true }); Sfx("slam", 0.6f);
        }
        // the landing: a crater punched into the square, the paving thrown up in a ring, a shockwave that hurls everyone
        // back, the cars nearby flung; then the boss bar and the intro
        void HollowImpact(Ent e)
        {
            var c = V(e.pos.x, 0, e.pos.z);
            Sfx("boom"); Sfx("slam"); Sfx("bossDeath", 0.5f); P.shake = 1.6f; whiteFlash = 0.25f; freeze = 0.06f;
            // the crater: a scorched pit, a raised lip of broken slabs leaning outward, cracks running off across the square
            Flat(Geo.Circle(8.5f, 40), Mats.Trans(0x0a0806, 0.85f), c.x, 0.05f, c.z);
            Flat(Geo.Circle(5.0f, 32), Mats.Std(0x1a1614, 0.95f, 0, 0x100400, GrimeTex()), c.x, 0.055f, c.z);
            var lipM = Mats.Std(0x6a645a, 0.92f, 0, 0, GrimeTex());
            for (int k = 0; k < 16; k++)
            {
                float a = k * Mathf.PI / 8 + rand * 0.2f, rr = 7.2f + rand * 1.4f;
                var sl = Deco(Geo.Box(2.6f + rand * 1.2f, 0.4f, 1.6f + rand), lipM, c.x + Mathf.Cos(a) * rr, 0.35f, c.z + Mathf.Sin(a) * rr, 0, -a + Mathf.PI / 2, 0); sl.transform.localRotation = Quaternion.Euler(0, (-a + Mathf.PI / 2) * Mathf.Rad2Deg, 0) * Quaternion.Euler(-(18 + rand * 20), 0, 0); Ig(sl);
            }
            for (int k = 0; k < 10; k++) { float a = k * 0.63f + rand * 0.3f; var cr = Flat(Geo.Plane(0.3f, 7 + rand * 9), Mats.Trans(0x0a0806, 0.7f), c.x + Mathf.Cos(a) * 11, 0.05f, c.z + Mathf.Sin(a) * 11); cr.transform.localRotation = Quaternion.Euler(-90, -a * Mathf.Rad2Deg + 90, 0); }
            var cg = Mk.Glow(levelRoot, 0xff5010, 10, 0.35f); cg.transform.localPosition = V(c.x, 0.6f, c.z); flames.Add(cg.transform);
            anims.Add((t, dt) => { if (rand < 0.25f) SpawnParticles(V(c.x + (rand - 0.5f) * 8, 0.4f, c.z + (rand - 0.5f) * 8), 1, 1.2f, rand < 0.6f ? ashMat : emberMat, 0.3f, 2.5f, -2); }); // the crater smouldering
            // the blast itself
            Blast(c + V(0, 2, 0), 14, 0xff7030); Blast(c + V(0, 1, 0), 6, 0xffffff);
            for (int i = 0; i < 3; i++) ShockRing(c, 14 + i * 12, 0xffa060, 0.5f + i * 0.25f);
            AddRing(c, 26, 46, 12, 0xff6020);
            SpawnParticles(c + V(0, 1, 0), 140, 22, dustMat, 0.55f, 2.4f); SpawnParticles(c + V(0, 1, 0), 60, 18, emberMat, 0.15f, 1.4f);
            for (int j = 0; j < 26; j++) particles.Add(new Pt { pos = c + V((rand - 0.5f) * 8, 1, (rand - 0.5f) * 8), scale = Vector3.one * (0.6f + rand * 1.2f), mat = rand < 0.5f ? concDarkM : lipM, v = V((rand - 0.5f) * 26, 10 + rand * 16, (rand - 0.5f) * 26), life = 3.5f, grav = GRAV, hasSpin = true, spin = V(rand * 8, rand * 8, rand * 8) });
            // the shockwave throws you back (and hurts if you were standing under him)
            if (!P.dead)
            {
                var o = P.pos - c; o.y = 0; float d = o.magnitude; if (d < 0.1f) { o = Vector3.forward; d = 0.1f; }
                if (d < 46) { float k = 1 - d / 46; P.vel += o / d * (10 + 30 * k) + V(0, 6 + 10 * k, 0); P.onGround = false; P.coyote = 0; }
                if (d < 11) DamagePlayer(25, c);
            }
            foreach (var hc in hkCars) if (!hc.gone && Hypot(hc.pos.x - c.x, hc.pos.z - c.z) < 16) { var o = hc.pos - c; o.y = 0; FlingCar(hc, o.normalized * 14 + V(0, 12, 0)); }
            HazardHitEnemies(x => x != e && Hypot(x.pos.x - c.x, x.pos.z - c.z) < 12, 40, x => V(0, 10, 0), 1);
        }
        void HollowDrop(Vector3 p, float delay)
        {
            var wm = Mats.Trans(0xff5020, 0, true); var w = Mk.Msh(levelRoot, Geo.Circle(3, 28), wm, p.x, p.y + 0.1f, p.z, -Mathf.PI / 2); w.layer = Mk.LayerIgnore;
            hkDrops.Add(new HDrop { p = p, delay = delay, warn = w, wm = wm });
        }
        // the ground slam: a hit where the fists land, rings to jump, the paving thrown up, the cars nearby flung
        void HollowSlam(Ent e, Vector3 p, float r, float dmg)
        {
            if (!P.dead && Hypot(P.pos.x - p.x, P.pos.z - p.z) < r && P.pos.y - p.y < 3) { DamagePlayer(dmg, p); P.vel.y = Mathf.Max(P.vel.y, 12); P.onGround = false; }
            AddRing(p, 20, 40, 20, 0xff6020); if (e.phase2) AddRing(p, 14, 34, 18, 0xffa040, 0.4f);
            Blast(p + V(0, 1, 0), 8, 0xff8030); ShockRing(p, r, 0xffffff, 0.4f); Sfx("slam"); Sfx("boom"); P.shake = Mathf.Max(P.shake, 1.1f);
            SpawnParticles(p + V(0, 0.5f, 0), 50, 14, dustMat, 0.4f, 1.3f);
            for (int k = 0; k < 10; k++) particles.Add(new Pt { pos = p + V((rand - 0.5f) * r, 0.5f, (rand - 0.5f) * r), scale = Vector3.one * (0.5f + rand * 0.8f), mat = concDarkM, v = V((rand - 0.5f) * 10, 8 + rand * 10, (rand - 0.5f) * 10), life = 2.5f, grav = GRAV, hasSpin = true, spin = V(rand * 10, rand * 10, rand * 10) });
            for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4 + rand; var cr = Flat(Geo.Plane(0.3f, 4 + rand * 5), Mats.Trans(0x0a0806, 0.7f), p.x + Mathf.Cos(a) * 3, 0.05f, p.z + Mathf.Sin(a) * 3); cr.transform.localRotation = Quaternion.Euler(-90, -a * Mathf.Rad2Deg + 90, 0); } // the square cracks
            foreach (var c in hkCars) if (!c.gone && Hypot(c.pos.x - p.x, c.pos.z - p.z) < r + 3) { var o = c.pos - p; o.y = 0; FlingCar(c, o.normalized * 10 + V(0, 14, 0)); }
            HazardHitEnemies(x => Hypot(x.pos.x - p.x, x.pos.z - p.z) < r, 30, x => V(0, 10, 0), 1);
        }
        void UpdateHollowFx(float dt)
        {
            for (int i = hkThrows.Count - 1; i >= 0; i--)
            {
                var h = hkThrows[i]; if (!h.t) { hkThrows.RemoveAt(i); continue; }
                h.v.y -= GRAV * dt; h.t.position += h.v * dt; h.t.Rotate(h.spin * dt, Space.World);
                if (rand < 0.7f) SpawnParticles(h.t.position, 1, 2, rand < 0.5f ? dustMat : emberMat, 0.2f, 0.6f);
                if (h.t.position.y > 0.6f && !PointInLevel(h.t.position)) continue;
                // it lands: an explosion of dust and fire, damage round it; a car stays as a burnt wreck, a chunk as rubble
                var p = V(h.t.position.x, h.t.position.y > 0.6f ? h.t.position.y : 0, h.t.position.z); // (on the ground, or on a roof)
                Blast(p + V(0, 1.2f, 0), h.r + 1, 0xff7030); ShockRing(p, h.r + 2, 0xffa060, 0.45f); SpawnParticles(p + V(0, 1, 0), 30, 10, dustMat, 0.35f, 1.2f); SpawnParticles(p + V(0, 1, 0), 20, 12, emberMat, 0.1f, 0.8f);
                Sfx("boom"); Sfx("slam", 0.7f); P.shake = Mathf.Max(P.shake, 0.7f);
                if (h.dmg > 0 && !P.dead && Hypot(P.pos.x - p.x, P.pos.z - p.z) < h.r && Mathf.Abs(P.pos.y - p.y) < 4) { var o = P.pos - p; o.y = 0; DamagePlayer(h.dmg, p); P.vel += (o.sqrMagnitude > 0.01f ? o.normalized : Vector3.forward) * 14 + V(0, 9, 0); P.onGround = false; }
                if (h.dmg > 0) HazardHitEnemies(x => Hypot(x.pos.x - p.x, x.pos.z - p.z) < h.r, 25, x => V(0, 8, 0), 1);
                if (h.car) { h.t.position = p; h.t.rotation = Quaternion.Euler(0, rand * 360, rand < 0.4f ? 180 : 0); var g = Mk.Glow(h.t, 0xff6010, 2.5f, 0.5f); g.transform.localPosition = V(0, 1, 0); SolidAABB(p - V(2, 0, 2), p + V(2, 1.4f, 2)); }
                else { Destroy(h.t.gameObject); if (p.y < 1) Debris(p.x, p.z, 2.2f); }
                hkThrows.RemoveAt(i);
            }
            for (int i = hkDrops.Count - 1; i >= 0; i--)
            {
                var d = hkDrops[i]; d.t += dt;
                if (d.t < d.delay) { Mats.SetAlpha(d.wm, 0.15f + 0.35f * d.t / d.delay * (Mathf.Sin(d.t * 24) > 0 ? 1 : 0.6f)); if (d.t > d.delay - 0.45f && !d.chunk) { d.chunk = Mk.Msh(levelRoot, Geo.Icosahedron(1.3f), concDarkM, d.p.x, d.p.y + 40, d.p.z); d.chunk.layer = Mk.LayerIgnore; } }
                if (d.chunk) { float k = Mathf.Clamp01((d.t - (d.delay - 0.45f)) / 0.45f); d.chunk.transform.localPosition = V(d.p.x, d.p.y + Mathf.Lerp(40, 1, k * k), d.p.z); d.chunk.transform.Rotate(dt * 200, dt * 100, 0); }
                if (d.t >= d.delay)
                {
                    if (d.warn) Destroy(d.warn); if (d.chunk) Destroy(d.chunk);
                    SpawnParticles(d.p + V(0, 0.6f, 0), 18, 7, dustMat, 0.3f, 0.9f); ShockRing(d.p, 4, 0xffa060, 0.35f); Sfx("slam", 0.5f); P.shake = Mathf.Max(P.shake, 0.25f);
                    if (!P.dead && Hypot(P.pos.x - d.p.x, P.pos.z - d.p.z) < 3 && Mathf.Abs(P.pos.y - d.p.y) < 3) DamagePlayer(22, d.p);
                    Ig(Deco(Geo.Icosahedron(1), concDarkM, d.p.x, d.p.y + 0.35f, d.p.z, rand * 3, rand * 3, rand * 3)).transform.localScale = V(1.2f, 0.5f, 1.0f);
                    hkDrops.RemoveAt(i);
                }
            }
        }

        // ================= animation =================
        void AnimateHollow(Ent e, float dt)
        {
            HollowExtrasAnim(e, dt);
            var r = e.hk; float k = 1 - Mathf.Exp(-8 * dt), hsp = Hypot(e.vel.x, e.vel.z); var st = e.state; float T = r.t;
            e.walk += dt * hsp * 0.45f; float sw = Mathf.Sin(e.walk), amp = Mathf.Min(1, hsp / 4);
            float Prog(float dur) => 1 - Mathf.Clamp01(e.stateT / dur);
            float lean = 0.15f * amp, twist = 0, headX = 0, jaw = 0.05f + Mathf.Sin(time * 3) * 0.04f;
            float rShX = -0.15f + sw * 0.4f * amp, rShZ = 0.25f, rEl = -0.35f, lShX = -0.15f - sw * 0.4f * amp, lShZ = 0.25f, lEl = -0.35f;
            float legL = -sw * 0.5f * amp, legR = sw * 0.5f * amp, kneeL = 0.1f + Mathf.Max(0, sw) * 0.7f * amp, kneeR = 0.1f + Mathf.Max(0, -sw) * 0.7f * amp, hipY = 1.6f - Mathf.Abs(Mathf.Cos(e.walk)) * 0.06f * amp;
            if (amp > 0.3f && Mathf.Abs(Mathf.Cos(e.walk)) > 0.98f && rand < 0.5f) { P.shake = Mathf.Max(P.shake, 0.15f); } // the ground shakes as he walks
            switch (st)
            {
                case "fall": lean = -0.2f; headX = -0.4f; jaw = 0.7f; rShX = lShX = -2.6f; rShZ = lShZ = 1.1f; rEl = lEl = -0.6f; legL = -0.5f; legR = 0.3f; kneeL = 0.9f; kneeR = 0.4f; break; // arms flung up, falling
                case "land":
                {
                    // the impact crouch: knees buckled, the right fist planted in the crater; he straightens, then roars with his arms thrown wide
                    float a = Smoother(Mathf.Clamp01((T - 0.4f) / 1.2f)); bool roar = T > 1.6f;
                    lean = Mathf.Lerp(1.0f, roar ? -0.35f : 0.1f, a); headX = Mathf.Lerp(0.5f, roar ? -0.6f : 0, a); jaw = roar ? 1 : 0.3f;
                    rShX = Mathf.Lerp(-0.9f, roar ? -0.5f : -0.2f, a); rEl = Mathf.Lerp(-0.1f, -0.3f, a); lShX = Mathf.Lerp(0.3f, roar ? -0.5f : -0.2f, a);
                    rShZ = lShZ = roar ? 1.3f : Mathf.Lerp(0.5f, 0.3f, a);
                    kneeL = kneeR = Mathf.Lerp(1.5f, 0.1f, a); legL = legR = Mathf.Lerp(-1.0f, 0, a); hipY = Mathf.Lerp(0.95f, 1.6f, a); break;
                }
                case "roar": lean = -0.3f; headX = -0.6f; jaw = 1; rShZ = lShZ = 1.4f; rShX = lShX = -0.6f; break;
                case "slamWind": { float t = Prog(1.15f); rShX = lShX = Mathf.Lerp(-0.3f, -3.0f, t); rShZ = lShZ = 0.3f; rEl = lEl = -0.3f; lean = Mathf.Lerp(0.15f, -0.4f, t); jaw = 0.6f; break; }
                case "recover": if (e.last == "slam" || e.last == "rain") { rShX = lShX = -0.6f; rEl = lEl = -0.1f; lean = 0.8f; kneeL = kneeR = 0.6f; hipY = 1.3f; } else lean = 0.3f; break;
                case "sweepWind": { float t = Prog(0.95f); rShX = 0.4f; rShZ = Mathf.Lerp(0.3f, 1.6f, t); rEl = -0.2f; twist = Mathf.Lerp(0, -0.8f, t); break; }
                case "sweep": { float t = Mathf.Clamp01(T / 0.35f); rShX = Mathf.Lerp(0.4f, -1.4f, t); rShZ = Mathf.Lerp(1.6f, 0.2f, t); rEl = -0.1f; twist = Mathf.Lerp(-0.8f, 0.9f, t); lean = 0.4f; jaw = 0.5f; break; }
                case "chargeWind": lean = 0.9f; headX = 0.5f; jaw = 0.6f; rShX = lShX = 0.6f; legL = Mathf.Sin(time * 12) * 0.3f; legR = -legL; break;
                case "charge": lean = 1.0f; headX = 0.5f; rShX = 0.8f + sw * 0.3f; lShX = 0.8f - sw * 0.3f; rShZ = lShZ = 0.5f; break;
                case "carWalk": rShX = lShX = -0.7f; rEl = lEl = -0.4f; lean = 0.4f; break;
                case "carReach": case "slabReach":
                {
                    // a deep squat, both arms down and forward, fingers under the car / into the paving
                    float t = Smoother(Prog(st == "slabReach" ? 0.9f : 0.85f));
                    lean = Mathf.Lerp(0.3f, 1.15f, t); headX = Mathf.Lerp(0, 0.4f, t); rShX = lShX = Mathf.Lerp(-0.6f, -1.25f, t); rShZ = lShZ = Mathf.Lerp(0.25f, 0.05f, t); rEl = lEl = -0.1f;
                    kneeL = kneeR = 1.2f * t; legL = legR = -0.6f * t; hipY = 1.6f - 0.55f * t; jaw = 0.4f; break;
                }
                case "carLift":
                {
                    // the heave: legs drive him up, the arms carry it from the ground to straight overhead
                    float t = Smoother(Mathf.Clamp01(T / 0.7f));
                    lean = Mathf.Lerp(1.15f, -0.25f, t); headX = Mathf.Lerp(0.4f, -0.5f, t); rShX = lShX = Mathf.Lerp(-1.25f, -2.95f, t); rShZ = lShZ = Mathf.Lerp(0.05f, 0.3f, t); rEl = lEl = Mathf.Lerp(-0.1f, -0.55f, t);
                    kneeL = kneeR = Mathf.Lerp(1.2f, 0.15f, t); legL = legR = Mathf.Lerp(-0.6f, 0, t); hipY = Mathf.Lerp(1.05f, 1.6f, t); jaw = 0.8f; break;
                }
                case "carHold":
                {
                    // drawn back for the throw, the weight held high behind his head
                    float t = Smoother(Prog(0.65f));
                    lean = Mathf.Lerp(-0.25f, -0.45f, t); rShX = lShX = Mathf.Lerp(-2.95f, -3.25f, t); rShZ = lShZ = 0.3f; rEl = lEl = Mathf.Lerp(-0.55f, -0.9f, t); legL = 0.35f; legR = -0.3f; kneeR = 0.4f; jaw = 0.6f; break;
                }
                case "ripReach":
                {
                    // the right arm reaches out to the roofline, fingers hooked into it, the left braced
                    float t = Smoother(Prog(0.6f));
                    rShX = Mathf.Lerp(-0.4f, -2.2f, t); rShZ = Mathf.Lerp(0.25f, 0.15f, t); rEl = -0.05f; lShX = -0.5f; lShZ = 0.5f; lean = Mathf.Lerp(0.15f, -0.1f, t); twist = 0.25f * t; jaw = 0.6f; legL = -0.3f; break;
                }
                case "rip": { float t = Smoother(Mathf.Clamp01(T / 0.6f)); rShX = Mathf.Lerp(-2.2f, -2.9f, t); rShZ = 0.35f; rEl = Mathf.Lerp(-0.05f, -0.6f, t); twist = Mathf.Lerp(0.25f, -0.45f, t); lean = -0.15f; jaw = 0.5f; break; }
                case "throw":
                {
                    // the follow-through: arms whipped forward and down, his weight onto the front foot
                    float t = Smoother(Mathf.Clamp01(T / 0.2f));
                    rShX = lShX = Mathf.Lerp(-3.1f, -0.6f, t); rEl = lEl = -0.2f; lean = Mathf.Lerp(-0.4f, 0.65f, t); twist = Mathf.Lerp(-0.4f, 0.3f, t); legL = -0.5f; kneeL = 0.5f; legR = 0.4f; jaw = 0.7f; break;
                }
                case "rainWind": { float t = Prog(1.0f); rShX = lShX = -2.6f; rShZ = lShZ = 0.7f; headX = -0.6f; jaw = 1; legR = -0.7f * Mathf.Sin(t * Mathf.PI); kneeR = 0.8f * Mathf.Sin(t * Mathf.PI); break; }
                case "volleyWind": lean = -0.25f; headX = -0.3f; jaw = 1; rShZ = lShZ = 1.1f; rShX = lShX = 0.2f; break;
                case "stunned": lean = 1.1f; headX = 0.8f + Mathf.Sin(time * 2) * 0.15f; jaw = 0.5f; rShX = lShX = 0.3f; rEl = lEl = -0.1f; kneeL = kneeR = 0.9f; hipY = 1.15f; break;
            }
            Sm(r.torso, 0, lean - e.flinch * 0.2f, k); Sm(r.torso, 1, twist, k); Sm(r.head, 0, headX, k); Sm(r.jaw, 0, jaw, k);
            Sm(r.rSh, 0, rShX, k); Sm(r.rSh, 2, rShZ, k); Sm(r.rEl, 0, rEl, k); Sm(r.lSh, 0, lShX, k); Sm(r.lSh, 2, -lShZ, k); Sm(r.lEl, 0, lEl, k);
            Sm(r.thigh[0], 0, legL, k); Sm(r.thigh[1], 0, legR, k); Sm(r.knee[0], 0, kneeL, k); Sm(r.knee[1], 0, kneeR, k);
            r.hips.py += (hipY - r.hips.py) * k;
            e.flinch = Mathf.Max(0, e.flinch - dt * 2);
            if (r.core) r.core.transform.localScale = Vector3.one * (1 + Mathf.Sin(time * (e.phase2 ? 10 : 4)) * 0.15f);
            if (rand < 0.25f) SpawnParticles(r.torso.t.TransformPoint(V((rand - 0.5f) * 0.8f, 0.4f + rand * 1.2f, 0.2f)), 1, 1.5f, rand < 0.6f ? ashMat : emberMat, 0.2f, 1.4f, -2); // smoke and embers out of him
            int em = e.flash > 0 ? 0xffffff : st == "stunned" ? 0x332200 : e.phase2 ? (Mathf.Sin(time * 8) > 0 ? 0x2a0a00 : 0x120400) : 0x000000;
            foreach (var m in e.mats) Mats.Emissive(m, em);
        }

        // ================= the death: he comes apart =================
        void UpdateHollowDeath(BossDeath d, float dt)
        {
            var e = d.e; var r = e.hk; float T = d.t, k = 1 - Mathf.Exp(-5 * dt);
            if (T < dt * 1.5f) { ReleaseHeld(e, false); foreach (var h in hkDrops) { if (h.warn) Destroy(h.warn); if (h.chunk) Destroy(h.chunk); } hkDrops.Clear(); Banner("THE HOLLOW KING FALLS", "", "#ff8040"); }
            UpdateHollowFx(dt); UpdateTraitorFx(dt);
            if (e.g && e.g.activeSelf)
            {
                // he staggers, fire bursting out of his chest, pieces falling off him; sinks to his knees; topples forward
                float jit = T < 2.4f ? 0.25f : 0; e.pos = V(d.basePos.x + (rand - 0.5f) * jit, d.basePos.y, d.basePos.z + (rand - 0.5f) * jit); SyncEnt(e);
                float kn = Smoother((T - 1.0f) / 1.2f);
                Sm(r.torso, 0, T < 1.0f ? -0.3f + Mathf.Sin(T * 8) * 0.2f : Mathf.Lerp(-0.2f, 0.6f, kn), k); Sm(r.head, 0, Mathf.Lerp(-0.4f, 0.7f, kn), k); Sm(r.jaw, 0, 0.9f, k);
                Sm(r.rSh, 2, 1.2f, k); Sm(r.lSh, 2, -1.2f, k); Sm(r.rSh, 0, -0.5f, k); Sm(r.lSh, 0, -0.5f, k);
                for (int i = 0; i < 2; i++) { Sm(r.thigh[i], 0, -1.1f * kn, k); Sm(r.knee[i], 0, 1.5f * kn, k); }
                r.hips.py += (Mathf.Lerp(1.6f, 0.7f, kn) - r.hips.py) * k;
                // the fire in his chest bursts out of him, and the crown is knocked from his head as he reels
                if (T >= 1.7f && T - dt < 1.7f)
                {
                    var ch = r.torso.t.TransformPoint(V(0, 0.85f, 0.3f)); Blast(ch, 7, 0xff7030); Blast(ch, 3, 0xffffff); SpawnParticles(ch, 80, 16, emberMat, 0.18f, 1.6f); SpawnParticles(ch, 40, 10, ashMat, 0.5f, 2.2f, -2);
                    Sfx("boom"); Sfx("roar"); whiteFlash = Mathf.Max(whiteFlash, 0.2f); slowmoT = Mathf.Max(slowmoT, 0.9f); if (r.core) r.core.SetActive(false);
                    if (r.crown && r.crown.parent != world) { r.crown.SetParent(world, true); particles.Add(new Pt { m = r.crown, v = HFwd(e) * 7 + V((rand - 0.5f) * 4, 9, 0), life = 30, grav = GRAV, hasSpin = true, spin = V(4, 2, 9), owned = true }); Sfx("ding", 0.6f); }
                }
                if (T >= 2.4f) { float tp = Mathf.Clamp01((T - 2.4f) / 0.8f); r.root.rx = 1.45f * tp * tp; }
                if (T < 3.2f && rand < 0.5f) { SpawnParticles(BodyPoint(e), 3, 10, emberMat, 0.2f, 1); Blast(BodyPoint(e) + Random.insideUnitSphere * 4, 1.5f + rand * 2, 0xff6010); if (rand < 0.3f) Sfx("boomSmall", 0.6f); }
                if (T > 0.5f && T < 3.0f && rand < 0.2f) { var parts = new[] { r.lSh, r.rSh, r.torso, r.thigh[0], r.thigh[1] }; var pt = parts[Random.Range(0, parts.Length)]; if (pt.t.childCount > 2) { var ch = pt.t.GetChild(Random.Range(0, pt.t.childCount)); if (ch.GetComponent<MeshRenderer>()) { ch.SetParent(world, true); particles.Add(new Pt { m = ch, v = V((rand - 0.5f) * 8, 3 + rand * 4, (rand - 0.5f) * 8), life = 4, grav = GRAV, hasSpin = true, spin = Random.insideUnitSphere * 6, owned = true }); Sfx("slam", 0.4f); } } }
                P.shake = Mathf.Max(P.shake, 0.2f + T * 0.08f);
                if (T >= 3.2f)
                {
                    // he hits the ground and comes apart in a storm of dust and wreckage; the crown rolls free
                    var c = BodyPoint(e);
                    if (r.crown && r.crown.parent != world) { r.crown.SetParent(world, true); particles.Add(new Pt { m = r.crown, v = HFwd(e) * 10 + V(0, 6, 0), life = 30, grav = GRAV, hasSpin = true, spin = V(0, 0, 8), owned = true }); }
                    foreach (var n in new[] { r.lSh, r.rSh, r.thigh[0], r.thigh[1], r.head, r.torso }) ShatterGroup(n, 10);
                    e.g.SetActive(false);
                    SpawnParticles(c, 120, 18, dustMat, 0.5f, 2.5f); SpawnParticles(c, 60, 16, emberMat, 0.15f, 1.4f); Blast(c, 16, 0xff7030); for (int i = 0; i < 3; i++) ShockRing(d.basePos, 16 + i * 10, 0xff8040, 0.7f + i * 0.3f);
                    foreach (var p in new[] { V(0, 0, 0), V(3, 0, 4), V(-4, 0, 3), V(2, 0, 9), V(-2, 0, 13) }) { var q = d.basePos + Quaternion.Euler(0, e.yaw * Mathf.Rad2Deg, 0) * p; Debris(q.x, q.z, 3.6f, false); } // (not solid: nothing may wall off the portal)
                    Sfx("bossDeath"); Sfx("boom"); Sfx("slam"); whiteFlash = 0.4f; P.shake = 1.6f; freeze = 0.12f;
                    Banner("THE KING IS DEAD", "THE DEAD CITY IS SILENT", "#ff8040");
                }
                return;
            }
            if (T > 6.0f) { Destroy(e.g); bossDeath = null; }
        }
            // an attack's setup (warnings, aim, sounds) - run by the host when it picks it, and by each client when the
        // host's pick arrives, so everyone sees the same attack aimed at the same player
        void HollowBegin(Ent e, string a)
        {
            var r = e.hk; if (r == null) return; Tgt = TargetOf(e, 0); float sp = e.phase2 ? 1.55f : 1.15f; var fwd = HFwd(e);
            float dx = Tgt.pos.x - e.pos.x, dz = Tgt.pos.z - e.pos.z, dist = Hypot(dx, dz), dy = Tgt.pos.y - e.pos.y; if (dist == 0) dist = 1;
switch (a)
            {
                case "slam": { HState(e, "slamWind", 1.15f); var to = V(dx, 0, dz); r.aim = e.pos + (dist > 11 ? to / dist * 11 : to); r.aim.y = 0; WarnDisc(r.aim, 9.5f, 1.15f / sp + 0.2f, 0xff5020); Sfx("growlLow"); break; }
                case "sweep": HState(e, "sweepWind", 0.95f); r.aimYaw = Mathf.Atan2(dx, dz); WarnFan(e.pos, r.aimYaw, 20, 1.25f, 0.95f / sp + 0.25f, 0xff5020); Sfx("growlLow"); break;
                case "charge": HState(e, "chargeWind", 1.05f); Sfx("roarShort"); break;
                case "car": { var c = NearestCar(e.pos); if (c == null) goto case "rip"; hkHeldCar = c; HState(e, Hypot(c.pos.x - e.pos.x, c.pos.z - e.pos.z) > 12 ? "carWalk" : "carReach", 3.0f); break; }
                case "rip": { var ch = hkChunks.Find(x => !x.gone && x.t); if (ch == null) goto case "volley"; ch.gone = true; hkHeld = ch.t; hkHeldIsCar = false; HState(e, "ripReach", 0.6f); Sfx("charge"); Sfx("growlLow"); break; }
                case "slab": HState(e, "slabReach", 0.9f); Sfx("growlLow"); break;
                case "rain": HState(e, "rainWind", 1.0f); Sfx("roar"); break;
                case "volley": HState(e, "volleyWind", 0.8f); Sfx("laserCharge"); break;
            }
        }
    }
}
