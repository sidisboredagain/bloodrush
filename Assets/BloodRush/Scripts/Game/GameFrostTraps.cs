using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    // The Frozen Circle's traps. Each belongs to a place on its map and makes sense there, and each one warns before it hurts:
    //  WILDERNESS  avalanche chutes off the cliffs - snow pours off the cornice, the chute rumbles, then a wall of snow comes down
    //              hunters' snares in the forest and round the camp - glinting iron jaws in the snow: step in one and you're held
    //              blizzard gusts over the frozen lake - streaks of snow race in, then the wind shoves you across the glass
    //  SETTLEMENT  icicles on the eaves - the roof edge shakes and sheds snow, the strip below glows, then they drop
    //              the frozen bell - it swings, then tolls, and a ring of frost rolls out across the town (jump it)
    //  RUINS       the guardians' gaze - a statue's eyes blaze, then a freezing beam sweeps the avenue in front of it
    //              rune tiles on the temple floor - a glowing rune under your feet means ice spears in half a second
    //  PRISON      pendulum blades swinging under the gantries over the yard
    //              the wardens' searchlights - a sweeping light; stay in it and it locks on, turns red and fires on you
    public partial class Game
    {
        float snareT;
        // held in a hunter's snare: you can still aim and shoot, but you can't move
        bool SnareHold(float dt)
        {
            if (snareT <= 0) return false;
            snareT -= dt; if (P.dead) { snareT = 0; return false; }
            P.vel.x = 0; P.vel.z = 0; return true;
        }

        // ---------- slick ice: a glassy sheet you slide across (no damage, but you can't stop or turn quickly) ----------
        void MechSlick(Vector3 c, float r, float rInner = 0)
        {
            Flat(Geo.Ring(r - 0.3f, r, 64), HazMat(0xd8f4ff, 0.3f, true), c.x, 0.045f, c.z); // a bright rim marks where the glass starts
            bool on = false;
            trapAnims.Add((t, dt) =>
            {
                if (rand < 0.25f) { float a = rand * Mathf.PI * 2, rr = rInner + rand * (r - rInner); particles.Add(new Pt { pos = c + V(Mathf.Cos(a) * rr, 0.08f, Mathf.Sin(a) * rr), scale = Vector3.one * 0.05f, mat = moteMat, v = V(0, 0.4f, 0), life = 0.6f, grav = 0 }); } // glints
                if (!HazardLive) return;
                float d = Hypot(P.pos.x - c.x, P.pos.z - c.z);
                bool inside = P.onGround && P.pos.y < 0.6f && d < r && d > rInner;
                if (inside) { slickT = 0.15f; if (!on) Sfx("slide", 0.6f); if (rand < 0.3f && Hypot(P.vel.x, P.vel.z) > 4) SpawnParticles(P.pos + V(0, 0.1f, 0), 1, 1.5f, SnowMat, 0.06f, 0.4f); }
                on = inside;
            });
        }

        // ---------- AVALANCHE: a chute down the cliff face; snow pours off the cornice and the ground rumbles, then it comes down ----------
        void MechAvalanche((float x, float zTop, float zEnd, float w)[] lanes, float cliffY)
        {
            var packed = Mats.Std(0xe8eef4, 0.88f, 0, 0x040608); var packed2 = Mats.Std(0xc8d4e0, 0.9f); var rockM = Mats.Std(0x3a4048, 0.92f);
            var crackM = Mats.Trans(0x2a4a6a, 0, true); var groovM = Mats.Trans(0xb8c8d8, 0, true); var dome = Geo.Sphere(1, 14, 8, 0, Mathf.PI * 2, 0, Mathf.PI / 2);
            for (int i = 0; i < lanes.Length; i++)
            {
                var L = lanes[i]; float off = i * 3.1f, len = L.zEnd - L.zTop; const float R = 2.3f;
                var rng = new System.Random(500 + i); float Rn() => (float)rng.NextDouble();
                // the cornice: a lip of wind-packed snow overhanging the cliff top, icicles under it, and the cracks that open in
                // it before it breaks. The front of it is what comes away (hidden while the ball is running, back after)
                var corn = Mk.Piv(levelRoot, L.x, cliffY, L.zTop - 0.6f); var lip = Mk.Piv(corn);
                for (int k = 0; k < 5; k++) Mk.Msh(corn, dome, snowM, (k - 2) * L.w * 0.22f, -0.05f, -1.2f - (k % 2) * 0.4f).transform.localScale = V(L.w * 0.26f, 0.9f + (k % 2) * 0.3f, 1.8f);
                for (int k = 0; k < 4; k++) Mk.Msh(lip, dome, snowM, (k - 1.5f) * L.w * 0.24f, -0.15f, 0.55f).transform.localScale = V(L.w * 0.2f, 0.75f, 1.3f);
                Mk.Msh(lip, Geo.Box(L.w * 0.95f, 0.35f, 1.4f), snowM, 0, -0.1f, 0.5f);
                for (int k = 0; k < 9; k++) Mk.Msh(lip, Geo.Cone(0.08f + Rn() * 0.08f, 0.4f + Rn() * 0.9f, 5), iceM, (k - 4) * L.w * 0.11f, -0.4f, 1.0f, Mathf.PI, 0, 0);
                for (int k = 0; k < 3; k++) Mk.Msh(corn, Geo.Box(0.06f, 0.02f, 1.6f + k * 0.4f), crackM, (k - 1) * L.w * 0.25f, 0.62f, -0.3f, 0, (k - 1) * 0.5f, 0);
                foreach (var r in corn.t.GetComponentsInChildren<Renderer>()) r.gameObject.layer = Mk.LayerIgnore;
                // the scar down the face: a smooth apron of old avalanche snow from the lip to the floor (solid at its foot), and
                // the debris at the bottom, low enough to wade through
                { float run0 = 2.4f, sl = Mathf.Sqrt(cliffY * cliffY + run0 * run0); var ap = Deco(Geo.Box(L.w * 0.8f, 0.5f, sl), snowM, L.x, cliffY / 2, L.zTop + run0 / 2 - 0.1f, -Mathf.Atan2(cliffY, run0), 0, 0); ap.layer = Mk.LayerIgnore; }
                SolidAABB(V(L.x - L.w * 0.4f, 0, L.zTop - 0.6f), V(L.x + L.w * 0.4f, 2.6f, L.zTop + 1.25f));
                for (int k = 0; k < 7; k++) { var g = Deco(dome, k % 3 == 0 ? packed2 : snowM, L.x + (Rn() - 0.5f) * L.w * 0.8f, 0, L.zTop + 2.6f + Rn() * len * 0.8f, 0, Rn() * 3, 0); g.transform.localScale = V(0.8f + Rn() * 1.2f, 0.18f + Rn() * 0.14f, 0.6f + Rn() * 0.9f); g.layer = Mk.LayerIgnore; }
                for (int k = 0; k < 10; k++) Ignore(Deco(Geo.Icosahedron(0.16f + Rn() * 0.2f), packed2, L.x + (Rn() - 0.5f) * L.w, 0.08f, L.zTop + 2.5f + Rn() * len, Rn() * 3, Rn() * 3, 0));
                var warnM = HazMat(0xffffff, 0, false); Flat(Geo.Plane(L.w, len), warnM, L.x, 0.06f, L.zTop + len / 2);
                var edgeM = HazMat(0xbfe8ff, 0.12f, true); foreach (var s in new[] { -1f, 1f }) Flat(Geo.Plane(0.25f, len), edgeM, L.x + s * L.w / 2, 0.07f, L.zTop + len / 2);
                // the groove the ball ploughs down the chute (stretches behind it, fades after)
                var groove = Flat(Geo.Plane(1, 1), groovM, L.x, 0.065f, L.zTop); groove.transform.localScale = V(R * 1.5f, 1, 0.01f);
                // THE SNOWBALL: a great packed boulder of snow, lumpy, with rocks, a broken branch and shards of ice rolled into it;
                // a cloud of powder snow billowing round it
                var ballP = Mk.Piv(levelRoot, L.x, R, L.zTop); var spin = Mk.Piv(ballP);
                Mk.Msh(spin, Geo.Sphere(R, 22, 16), packed);
                for (int k = 0; k < 16; k++) { var d = new Vector3(Rn() - 0.5f, Rn() - 0.5f, Rn() - 0.5f).normalized; Mk.Msh(spin, Geo.Icosahedron(1), k % 3 == 0 ? packed2 : packed, d.x * R * 0.92f, d.y * R * 0.92f, d.z * R * 0.92f, Rn() * 3, Rn() * 3, Rn() * 3).transform.localScale = Vector3.one * (0.45f + Rn() * 0.4f); }
                for (int k = 0; k < 4; k++) { var d = new Vector3(Rn() - 0.5f, Rn() - 0.5f, Rn() - 0.5f).normalized; Mk.Msh(spin, Geo.Icosahedron(0.38f + Rn() * 0.2f), rockM, d.x * R, d.y * R, d.z * R, Rn() * 3, Rn() * 3, 0); }
                for (int k = 0; k < 2; k++) { var d = new Vector3(Rn() - 0.5f, Rn() - 0.5f, Rn() - 0.5f).normalized; var br = Mk.Piv(spin, d.x * R * 0.9f, d.y * R * 0.9f, d.z * R * 0.9f); br.t.localRotation = Quaternion.FromToRotation(Vector3.up, d) * Quaternion.Euler(30, 0, 0); Mk.Msh(br, Geo.Cyl(0.05f, 0.09f, 1.6f, 6), darkWoodM, 0, 0.6f, 0); Mk.Msh(br, Geo.Cyl(0.02f, 0.04f, 0.6f, 5), darkWoodM, 0.15f, 1.0f, 0, 0, 0, -0.7f); }
                for (int k = 0; k < 4; k++) { var d = new Vector3(Rn() - 0.5f, Rn() - 0.5f, Rn() - 0.5f).normalized; var cr = Mk.Piv(spin, d.x * R * 0.95f, d.y * R * 0.95f, d.z * R * 0.95f); cr.t.localRotation = Quaternion.FromToRotation(Vector3.up, d); Mk.Msh(cr, Geo.Cyl(0.04f, 0.14f, 0.55f, 6), iceM, 0, 0.2f, 0); }
                var cloudM = Mats.Trans(0xf4f8ff, 0.5f); var puffs = new List<Transform>();
                for (int k = 0; k < 9; k++) { float a = k * 0.7f; var pf = Mk.Msh(ballP, Geo.Sphere(1, 12, 8), cloudM, Mathf.Cos(a) * R * 0.9f, -R * 0.4f + (k % 3) * 0.5f, -R * 0.6f - (k % 4) * 0.6f).transform; pf.localScale = Vector3.one * (1.2f + (k % 3) * 0.5f); puffs.Add(pf); }
                foreach (var r in ballP.t.GetComponentsInChildren<Renderer>()) r.gameObject.layer = Mk.LayerIgnore;
                ballP.t.gameObject.SetActive(false);
                bool hitP = false, landed = false, burst = false; var hitE = new HashSet<Ent>(); float rolled = 0; Vector3 last = Vector3.zero;
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 12f; bool warn = u >= 6.6f && u < 8.4f, run = u >= 8.4f && u < 10.0f;
                    Mats.SetAlpha(warnM, warn ? 0.06f + 0.22f * (u - 6.6f) / 1.8f * (Mathf.Sin(u * 18) > 0 ? 1 : 0.5f) : run ? 0.18f : 0);
                    Mats.SetAlpha(edgeM, warn || run ? 0.5f + 0.3f * Mathf.Sin(u * 20) : 0.12f);
                    Mats.SetAlpha(crackM, warn ? 0.9f * (u - 6.6f) / 1.8f : 0);
                    // the groove fades away over the next few seconds
                    Mats.SetAlpha(groovM, u >= 8.4f ? Mathf.Clamp01(1 - (u - 10.0f) / 2.0f) * 0.55f : 0);
                    lip.t.localPosition = warn ? V((rand - 0.5f) * 0.06f, (rand - 0.5f) * 0.04f, 0) : Vector3.zero; // the lip trembling
                    lip.t.gameObject.SetActive(!run && !(u >= 10.0f && u < 11.5f)); // broken off, then built back up
                    if (warn)
                    {
                        if (rand < 0.6f) SpawnParticles(V(L.x + (rand - 0.5f) * L.w, cliffY + 0.3f, L.zTop + 0.4f), 2, 1.2f, SnowMat, 0.14f, 1.4f, 6); // snow sifting off the lip
                        if (HazardLive && Mathf.Abs(P.pos.x - L.x) < 20 && P.pos.z < L.zEnd + 10) P.shake = Mathf.Max(P.shake, 0.08f + 0.12f * (u - 6.6f) / 1.8f);
                    }
                    if (u >= 6.6f && u - dt < 6.6f && HazardLive && Hypot(P.pos.x - L.x, P.pos.z - L.zTop) < 45) { Sfx("growlLow", 0.7f); Sfx("charge", 0.4f); }
                    if (u >= 7.7f && u - dt < 7.7f && HazardLive && Hypot(P.pos.x - L.x, P.pos.z - L.zTop) < 45) Sfx("shatter", 0.5f); // the crack
                    ballP.t.gameObject.SetActive(run && !burst);
                    if (!run) { hitP = false; landed = false; burst = false; rolled = 0; hitE.Clear(); return; }
                    float k = (u - 8.4f) / 1.6f; Vector3 bp;
                    if (u - dt < 8.4f)
                    {
                        Sfx("boom", 0.6f); Sfx("slam", 0.6f); // the cornice gives way
                        for (int j = 0; j < 14; j++) particles.Add(new Pt { pos = V(L.x + (rand - 0.5f) * L.w, cliffY, L.zTop + 0.5f), scale = Vector3.one * (0.3f + rand * 0.5f), mat = snowM, v = V((rand - 0.5f) * 4, 2 + rand * 3, 3 + rand * 4), life = 1.6f, grav = GRAV, hasSpin = true, spin = V(rand * 6, rand * 6, 0) });
                        last = V(L.x, cliffY + R, L.zTop - 0.6f);
                    }
                    if (k < 0.2f)
                    {
                        // off the lip and down the apron
                        float a = k / 0.2f; bp = V(L.x, Mathf.Lerp(cliffY + R * 0.6f, R, a * a), Mathf.Lerp(L.zTop - 0.4f, L.zTop + 3.2f, a));
                    }
                    else
                    {
                        float a = Mathf.Clamp01((k - 0.2f) / 0.68f); float bounce = Mathf.Abs(Mathf.Sin(a * 9)) * 0.35f * (1 - a);
                        bp = V(L.x + Mathf.Sin(a * 5 + i) * 0.4f, R + bounce, Mathf.Lerp(L.zTop + 3.2f, L.zEnd - 1, a));
                        if (!landed) { landed = true; Sfx("slam", 0.8f); P.shake = Mathf.Max(P.shake, 0.35f); SpawnParticles(bp - V(0, R - 0.3f, 0), 30, 7, SnowMat, 0.25f, 1.0f, 2); }
                    }
                    rolled += Vector3.Distance(bp, last); last = bp;
                    ballP.t.localPosition = bp; spin.t.localRotation = Quaternion.Euler(rolled / R * Mathf.Rad2Deg, 0, 0);
                    for (int j = 0; j < puffs.Count; j++) { float s = (1.2f + (j % 3) * 0.5f) * (1 + 0.25f * Mathf.Sin(u * 7 + j)); puffs[j].localScale = Vector3.one * s * (0.7f + 0.5f * k); }
                    Mats.SetAlpha(cloudM, 0.45f * (1 - k * 0.5f));
                    groove.transform.localPosition = V(L.x, 0.065f, (L.zTop + 2.6f + bp.z) / 2); groove.transform.localScale = V(R * 1.5f, 1, Mathf.Max(0.01f, bp.z - L.zTop - 2.6f));
                    if (rand < 0.9f) SpawnParticles(bp + V((rand - 0.5f) * R * 2, -R * 0.6f, -R * 0.5f), 3, 5, SnowMat, 0.2f, 0.8f, 2); // powder thrown up behind
                    if (k >= 0.88f && !burst)
                    {
                        // it bursts against the end of the chute in an explosion of snow
                        burst = true; Sfx("boom", 0.8f); Sfx("slam"); P.shake = Mathf.Max(P.shake, 0.5f);
                        SpawnParticles(bp, 50, 10, SnowMat, 0.35f, 1.3f, 2); ShockRing(V(bp.x, 0.1f, bp.z), 6, 0xe8f4ff, 0.4f);
                        for (int j = 0; j < 18; j++) particles.Add(new Pt { pos = bp + Random.insideUnitSphere * R * 0.8f, scale = Vector3.one * (0.3f + rand * 0.6f), mat = rand < 0.2f ? rockM : snowM, v = V((rand - 0.5f) * 12, 4 + rand * 7, rand * 8), life = 2.2f, grav = GRAV, hasSpin = true, spin = V(rand * 8, rand * 8, rand * 8) });
                        if (HazardLive && !hitP && Hypot(P.pos.x - bp.x, P.pos.z - bp.z) < R + 2.5f && P.pos.y < 3) { hitP = true; HazardHitPlayer(18, bp, V(0, 7, 10)); chillT = Mathf.Max(chillT, 1.4f); }
                    }
                    if (!HazardLive || burst) return;
                    // hit: anything the ball rolls over
                    if (!hitP && Hypot(P.pos.x - bp.x, P.pos.z - bp.z) < R + 0.45f && P.pos.y < bp.y + R) { hitP = true; HazardHitPlayer(26, bp - V(0, 0, 3), V((P.pos.x - bp.x) * 3, 8, 18)); chillT = Mathf.Max(chillT, 1.8f); P.shake = Mathf.Max(P.shake, 0.6f); }
                    HazardHitEnemies(e => Hypot(e.pos.x - bp.x, e.pos.z - bp.z) < R + 0.4f && e.pos.y < bp.y + R && hitE.Add(e), 18, e => V(0, 8, 16));
                });
            }
        }

        // ---------- HUNTERS' SNARES: iron jaws half buried in the snow, glinting; step in one and it snaps shut and holds you ----------
        void MechSnares(Vector3[] pts)
        {
            var iron = Mats.Std(0x3a3430, 0.5f, 0.7f); var teethM = Mats.Std(0x8a8a88, 0.3f, 0.8f);
            foreach (var c in pts)
            {
                Ignore(Deco(Geo.Cyl(1.0f, 1.1f, 0.04f, 14), Mats.Std(0xd8e2ea, 0.95f), c.x, 0.02f, c.z)); // trodden snow
                Ignore(Deco(Geo.Box(1.2f, 0.06f, 0.18f), iron, c.x, 0.05f, c.z)); Ignore(Deco(Geo.Cyl(0.16f, 0.16f, 0.05f, 10), iron, c.x, 0.07f, c.z)); // the base plate and trigger
                ChainLine(c + V(0.6f, 0.05f, 0), c + V(1.8f, 0.05f, 0.6f), 0, 0.22f); Ignore(Deco(Geo.Cyl(0.06f, 0.04f, 0.5f, 5), darkWoodM, c.x + 1.85f, 0.15f, c.z + 0.62f)); // staked to the ground
                var jaws = new List<N>();
                foreach (var s in new[] { -1f, 1f })
                {
                    var hinge = Mk.Piv(levelRoot, c.x, 0.06f, c.z); var jaw = Mk.Piv(hinge, 0, 0, 0);
                    var arc = Mk.Msh(jaw, Geo.Torus(0.55f, 0.045f, 14, 4, Mathf.PI), iron, 0, 0, 0, 0, s > 0 ? 0 : Mathf.PI, 0); arc.transform.localRotation = Quaternion.Euler(0, s > 0 ? 90 : -90, 0);
                    for (int k = 0; k < 6; k++) { float a = (k + 0.5f) / 6 * Mathf.PI; var tc = Mk.Msh(jaw, Geo.Cone(0.04f, 0.16f, 4), teethM, s * Mathf.Sin(a) * 0.55f, 0.08f, Mathf.Cos(a) * 0.55f); }
                    foreach (var r in hinge.t.GetComponentsInChildren<Renderer>()) r.gameObject.layer = Mk.LayerIgnore;
                    jaw.SetRot(0, 0, s * 1.45f); jaws.Add(jaw);
                }
                float cd = 0, snap = 0, glint = rand * 2;
                trapAnims.Add((t, dt) =>
                {
                    cd -= dt; snap -= dt; glint -= dt;
                    float open = cd > 0 ? (cd < 1 ? 1 - cd : 0) : 1; // closed while it re-arms, then creaks open again
                    if (snap > 0) open = 0;
                    for (int j = 0; j < 2; j++) jaws[j].SetRot(0, 0, (j == 0 ? -1 : 1) * Mathf.Lerp(0.05f, 1.45f, open));
                    if (glint <= 0 && cd <= 0) { glint = 1.6f + rand * 1.6f; SpawnParticles(c + V((rand - 0.5f) * 0.8f, 0.15f, (rand - 0.5f) * 0.8f), 1, 0.2f, moteMat, 0.08f, 0.35f, 0); } // a glint of steel in the snow
                    if (cd > 0) return;
                    if (HazardLive && P.pos.y < 0.7f && Hypot(P.pos.x - c.x, P.pos.z - c.z) < 0.95f && P.iframes <= 0)
                    {
                        cd = 5.5f; snap = 1.2f; snareT = 1.3f; P.vel = V(0, Mathf.Min(P.vel.y, 0), 0);
                        DamagePlayer(14, c); Sfx("slam", 0.7f); Sfx("hit"); hurtFlash = Mathf.Max(hurtFlash, 0.3f); P.shake = Mathf.Max(P.shake, 0.5f);
                        SpawnParticles(c + V(0, 0.3f, 0), 12, 3, SnowMat, 0.12f, 0.6f); SpawnParticles(c + V(0, 0.4f, 0), 6, 3, sparkMat, 0.05f, 0.3f);
                        return;
                    }
                    // co-op: a teammate caught in it (their own machine holds and hurts them) - we see it snap shut on them
                    if (MP && HazardLive) foreach (var q in players)
                        if (q.remote && !q.downed && !q.dead && q.pos.y < 0.7f && Hypot(q.pos.x - c.x, q.pos.z - c.z) < 0.95f)
                        { cd = 5.5f; snap = 1.2f; Sfx("slam", 0.6f); SpawnParticles(c + V(0, 0.3f, 0), 12, 3, SnowMat, 0.12f, 0.6f); SpawnParticles(c + V(0, 0.4f, 0), 6, 3, sparkMat, 0.05f, 0.3f); return; }
                    if (Auth) foreach (var e in enemies) // (the host's enemies trip it; it's re-armed again sooner after one does)
                        if (!e.dead && e.type == "husk" && e.spawnT <= 0 && Hypot(e.pos.x - c.x, e.pos.z - c.z) < 1.0f && e.pos.y < 0.6f)
                        { cd = MP ? 3.0f : 5.5f; snap = 1.2f; e.stun = Mathf.Max(e.stun, 2.2f); e.vel = Vector3.zero; DamageEnemy(e, 25, new Hit { point = c + V(0, 0.5f, 0), kb = 0.01f }); Sfx("slam", 0.5f); SpawnParticles(c + V(0, 0.3f, 0), 10, 3, SnowMat, 0.12f, 0.6f); break; }
                });
            }
        }

        // ---------- BLIZZARD GUSTS over the open lake: streaks of snow come racing in, then the wind shoves you across the ice ----------
        void MechGusts(Vector3 c, float r, Vector3 dir)
        {
            dir.y = 0; dir.Normalize(); var side = V(-dir.z, 0, dir.x);
            trapAnims.Add((t, dt) =>
            {
                float u = t % 10f; bool warn = u >= 4 && u < 5.6f, gust = u >= 5.6f && u < 8.6f;
                if (warn || gust)
                {
                    int n = gust ? 4 : 1;
                    for (int k = 0; k < n; k++) { var src = c - dir * r + side * (rand - 0.5f) * r * 2 + V(0, 0.3f + rand * 3, 0); particles.Add(new Pt { pos = src, scale = V(0.05f, 0.05f, 0.5f), mat = SnowMat, v = dir * (gust ? 34 : 24) + V(0, -0.5f, 0), life = r * 2 / (gust ? 34 : 24), grav = 0 }); }
                }
                if (u >= 4 && u - dt < 4 && HazardLive && Hypot(P.pos.x - c.x, P.pos.z - c.z) < r + 15) Sfx("slide", 0.8f);
                if (!gust || !HazardLive) return;
                if (u - dt < 5.6f && Hypot(P.pos.x - c.x, P.pos.z - c.z) < r + 10) Sfx("dash", 0.6f);
                float k2 = Smoother((u - 5.6f) / 0.4f) * Smoother((8.6f - u) / 0.5f);
                if (Hypot(P.pos.x - c.x, P.pos.z - c.z) < r && P.pos.y < 5) { P.vel += dir * (P.onGround ? 26 : 16) * k2 * dt; chillT = Mathf.Max(chillT, 0.3f); }
                foreach (var e in enemies) if (!e.dead && e.type != "boss" && e.spawnT <= 0 && Hypot(e.pos.x - c.x, e.pos.z - c.z) < r) e.vel += dir * 10 * k2 * dt;
            });
        }

        // ---------- ICICLES ON THE EAVES: the roof edge shakes and sheds snow, the strip below lights up, then they drop ----------
        // each eave: a line (centre x,z, length, along x?) at height y; out = which side of the line the street is (the strip is under it)
        void MechEaves((float x, float z, float len, bool alongX, float y)[] eaves)
        {
            for (int i = 0; i < eaves.Length; i++)
            {
                var E = eaves[i]; float off = i * 1.9f;
                int n = Mathf.Max(3, (int)(E.len / 0.9f));
                var row = Mk.Piv(levelRoot, E.x, E.y, E.z); var spikes = new List<Transform>();
                for (int k = 0; k < n; k++)
                {
                    float u = ((k + 0.5f) / n - 0.5f) * E.len, il = 1.2f + ((k * 5) % 4) * 0.35f;
                    var ic = Mk.Msh(row, Geo.Cone(0.16f, il, 6), iceM, E.alongX ? u : 0, -il / 2, E.alongX ? 0 : u, Mathf.PI, 0, 0); ic.layer = Mk.LayerIgnore; spikes.Add(ic.transform);
                }
                var markM = HazMat(0x60c8ff, 0, false); Flat(Geo.Plane(E.alongX ? E.len : 2.2f, E.alongX ? 2.2f : E.len), markM, E.x, 0.07f, E.z);
                var rimM = HazMat(0xe0f6ff, 0, true); Flat(Geo.Ring(0, 0.01f, 4), rimM, E.x, 0.08f, E.z);
                var drops = new List<Pt>(); bool fell = false;
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 10f; bool warn = u >= 5.4f && u < 7.0f, falling = u >= 7.0f && u < 7.35f, regrow = u >= 7.35f && u < 9.5f;
                    float shake = warn ? (u - 5.4f) / 1.6f : 0;
                    row.t.localPosition = V(E.x + (rand - 0.5f) * shake * 0.12f, E.y, E.z + (rand - 0.5f) * shake * 0.12f);
                    Mats.SetAlpha(markM, warn ? 0.1f + 0.35f * shake * (Mathf.Sin(u * 26) > 0 ? 1 : 0.6f) : falling ? 0.45f : 0);
                    if (warn && rand < 0.6f) SpawnParticles(V(E.x + (E.alongX ? (rand - 0.5f) * E.len : 0), E.y + 0.2f, E.z + (E.alongX ? 0 : (rand - 0.5f) * E.len)), 1, 0.4f, SnowMat, 0.09f, 1.0f, 5); // snow sliding off the roof edge
                    if (u >= 5.4f && u - dt < 5.4f && HazardLive && Hypot(P.pos.x - E.x, P.pos.z - E.z) < 22) Sfx("charge", 0.45f);
                    float grow = regrow ? Smoother((u - 7.35f) / 2.1f) : falling ? 0 : 1;
                    float fallK = falling ? (u - 7.0f) / 0.35f : 0;
                    foreach (var sp in spikes) { sp.localScale = Vector3.one * Mathf.Max(0.02f, falling ? 1 : grow); var lp = sp.localPosition; lp.y = -sp.localScale.y * 0.6f - (falling ? fallK * fallK * (E.y - 0.4f) : 0); sp.localPosition = lp; }
                    if (u < 7.35f) fell = false;
                    if (u >= 7.35f && !fell)
                    {
                        fell = true;
                        var cpos = V(E.x, 0.3f, E.z);
                        for (int k = 0; k < 6; k++) particles.Add(new Pt { pos = cpos + V(E.alongX ? (rand - 0.5f) * E.len : (rand - 0.5f) * 1.5f, 0.3f, E.alongX ? (rand - 0.5f) * 1.5f : (rand - 0.5f) * E.len), scale = Vector3.one * (0.15f + rand * 0.2f), mat = iceM, v = V((rand - 0.5f) * 6, 3 + rand * 3, (rand - 0.5f) * 6), life = 1.6f, grav = GRAV, hasSpin = true, spin = V(rand * 10, rand * 10, rand * 10) });
                        SpawnParticles(cpos, 18, 5, cyanMat, 0.1f, 0.6f); SpawnParticles(cpos, 10, 4, SnowMat, 0.15f, 0.9f);
                        if (Hypot(P.pos.x - E.x, P.pos.z - E.z) < 30) Sfx("shatter", 0.7f);
                        bool inStrip = Mathf.Abs(P.pos.x - E.x) < (E.alongX ? E.len / 2 : 1.2f) && Mathf.Abs(P.pos.z - E.z) < (E.alongX ? 1.2f : E.len / 2) && P.pos.y < 2.6f;
                        if (HazardLive && inStrip) { HazardHitPlayer(22, cpos + V(0, 3, 0), V(0, 4, 0)); chillT = Mathf.Max(chillT, 1.4f); }
                        HazardHitEnemies(e => Mathf.Abs(e.pos.x - E.x) < (E.alongX ? E.len / 2 : 1.2f) && Mathf.Abs(e.pos.z - E.z) < (E.alongX ? 1.2f : E.len / 2) && e.pos.y < 2.5f, 18, e => V(0, 4, 0));
                    }
                });
            }
        }
        // the eave of one side of a House (matches the roof House builds): side = +1/-1 across the ridge
        (float x, float z, float len, bool alongX, float y) EaveOf(float x, float z, float w, float d, float h, bool ridgeX, int side)
        {
            float span = ridgeX ? d : w, len = ridgeX ? w : d, rh = span * 0.42f, a = Mathf.Atan2(rh, span / 2), ov = 0.7f;
            float e = span / 2 + Mathf.Cos(a) * ov * 0.9f, y = h - Mathf.Sin(a) * ov * 0.9f - 0.1f;
            return ridgeX ? (x, z + side * e, len, true, y) : (x + side * e, z, len, false, y);
        }

        // ---------- THE FROZEN BELL: it swings, faster and faster, then tolls - and a ring of frost rolls out across the town ----------
        void MechBellToll(Vector3 belfry, N bell, Material bellGlow)
        {
            trapAnims.Add((t, dt) =>
            {
                float u = t % 14f; bool swing = u >= 9.8f && u < 12f;
                float amp = swing ? Smoother((u - 9.8f) / 2.2f) * 0.55f : u >= 12f && u < 13.5f ? 0.55f * (1 - (u - 12f) / 1.5f) : 0;
                bell.SetRot(Mathf.Sin(u * 5.2f) * amp, 0, 0);
                Mats.SetAlpha(bellGlow, swing ? 0.15f + 0.5f * (u - 9.8f) / 2.2f : u >= 12f && u < 12.6f ? 1 - (u - 12f) / 0.6f : 0.05f);
                if (swing && Mathf.Sin(u * 5.2f) > 0 && Mathf.Sin((u - dt) * 5.2f) <= 0 && HazardLive) Sfx("ding", 0.35f);
                if (u >= 12f && u - dt < 12f)
                {
                    Sfx("ding"); Sfx("boom", 0.8f); Blast(belfry, 6, 0x9ae8ff);
                    var g = V(belfry.x, 0, belfry.z + 4);
                    AddRing(g, 15, 80, 18, 0x9ae8ff); AddRing(g, 15, 80, 14, 0xd8f6ff, 0.9f);
                    if (HazardLive) P.shake = Mathf.Max(P.shake, 0.35f);
                    for (int k = 0; k < 40; k++) SpawnParticles(V((rand - 0.5f) * LB * 1.8f, 7 + rand * 4, (rand - 0.5f) * LB * 1.8f), 1, 0.6f, SnowMat, 0.1f, 1.6f, 4); // snow shaken off the roofs
                }
            });
        }

        // ---------- THE GUARDIANS' GAZE: a statue's eyes blaze, a line lights across the avenue, then a freezing beam sweeps it ----------
        // each: the eyes, and the ground line it sweeps (from -> to)
        void MechStatueGaze((Vector3 eye, Vector3 from, Vector3 to)[] gazes)
        {
            for (int i = 0; i < gazes.Length; i++)
            {
                var G = gazes[i]; float off = i * 2.6f;
                var lineM = HazMat(0x9ae8ff, 0, false); var mid = (G.from + G.to) / 2; float L = Vector3.Distance(G.from, G.to);
                var line = Flat(Geo.Plane(2.2f, L), lineM, mid.x, 0.07f, mid.z); line.transform.localRotation = Quaternion.Euler(-90, Mathf.Atan2(G.to.x - G.from.x, G.to.z - G.from.z) * Mathf.Rad2Deg, 0);
                var eyeGlowM = Mats.Add(0x9af0ff, 0, null, true, true); var eg = Mk.Msh(levelRoot, Geo.Sphere(0.9f, 10, 8), eyeGlowM, G.eye.x, G.eye.y, G.eye.z); eg.layer = Mk.LayerIgnore;
                var beamM = Mats.Add(0xc8f6ff, 0.85f, null, true, true); var beam = Mk.Msh(levelRoot, Geo.Cyl(1, 1, 1, 10, true), beamM); beam.layer = Mk.LayerIgnore; beam.SetActive(false);
                var core = Mk.Msh(beam.transform, Geo.Cyl(0.4f, 0.4f, 1, 8, true), Mats.Add(0xffffff, 0.9f, null, true, true)); core.layer = Mk.LayerIgnore;
                var spotM = Mats.Add(0xd8f8ff, 0.8f, null, true, true); var spot = Flat(Geo.Circle(1.3f, 20), spotM, 0, 0.09f, 0); spot.SetActive(false);
                bool hitP = false; var hitE = new HashSet<Ent>(); float trail = 0;
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 11f; bool warn = u >= 6 && u < 7.4f, fire = u >= 7.4f && u < 8.9f;
                    Mats.SetAlpha(lineM, warn ? 0.1f + 0.35f * (u - 6) / 1.4f * (Mathf.Sin(u * 24) > 0 ? 1 : 0.55f) : fire ? 0.25f : 0);
                    Mats.SetAlpha(eyeGlowM, warn ? 0.2f + 0.6f * (u - 6) / 1.4f : fire ? 0.9f : 0.06f);
                    eg.transform.localScale = Vector3.one * (warn ? 0.6f + (u - 6) / 1.4f * 0.8f : fire ? 1.5f + Mathf.Sin(t * 40) * 0.2f : 0.5f);
                    if (u >= 6 && u - dt < 6 && HazardLive && Hypot(P.pos.x - mid.x, P.pos.z - mid.z) < 30) Sfx("laserCharge", 0.5f);
                    beam.SetActive(fire); spot.SetActive(fire);
                    if (!fire) { hitP = false; hitE.Clear(); return; }
                    float k = Smoother((u - 7.4f) / 1.5f); var gp = Vector3.Lerp(G.from, G.to, k); gp.y = 0.05f;
                    var d = gp - G.eye; float len = d.magnitude;
                    beam.transform.localPosition = (G.eye + gp) / 2; beam.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d / len); beam.transform.localScale = V(0.28f + Mathf.Sin(t * 50) * 0.04f, len, 0.28f);
                    spot.transform.localPosition = V(gp.x, 0.09f, gp.z);
                    if (rand < 0.9f) SpawnParticles(gp + V(0, 0.3f, 0), 2, 4, rand < 0.5f ? cyanMat : SnowMat, 0.1f, 0.6f, -1);
                    trail -= dt; if (trail <= 0) { trail = 0.08f; var fm = Mats.Add(0x9ae8ff, 0.4f, null, true, true); var fg = Flat(Geo.Circle(0.9f, 12), fm, gp.x, 0.08f, gp.z); fx.Add(new Fx { m = fg.transform, mat = fm, life = 2.5f, max = 2.5f }); } // frost left on the stones
                    if (u - dt < 7.4f) Sfx("laserTick");
                    if (!HazardLive) return;
                    if (!hitP && Hypot(P.pos.x - gp.x, P.pos.z - gp.z) < 1.5f && P.pos.y < 2.4f) { hitP = true; HazardHitPlayer(20, gp, V(0, 5, 0)); chillT = Mathf.Max(chillT, 1.8f); }
                    HazardHitEnemies(e => Hypot(e.pos.x - gp.x, e.pos.z - gp.z) < 1.6f && e.pos.y < 2.4f && hitE.Add(e), 16, e => V(0, 4, 0));
                });
            }
        }

        // ---------- THE GUARDIAN STATUES: when they can see you, their visors flare, a cold aiming line finds you, and they fire ice ----------
        void MechStatueShooters(Vector3[] eyes)
        {
            for (int i = 0; i < eyes.Length; i++)
            {
                var eye = eyes[i]; float cd = 2.5f + i * 1.3f, charge = 0; int shots = 0; float shotT = 0;
                var glowM = Mats.Add(0x9af0ff, 0, null, true, true); var eg = Mk.Msh(levelRoot, Geo.Sphere(0.8f, 10, 8), glowM, eye.x, eye.y, eye.z); eg.layer = Mk.LayerIgnore;
                var lineM = Mats.Add(0x9ae8ff, 0, null, true, true); var line = Mk.Msh(levelRoot, Geo.Cyl(1, 1, 1, 6, true), lineM); line.layer = Mk.LayerIgnore; line.SetActive(false);
                trapAnims.Add((t, dt) =>
                {
                    cd -= dt;
                    var tgt = ChestPos; var muzzle = eye + (tgt - eye).normalized * 1.9f; bool sees = HazardLive && Vector3.Distance(eye, tgt) < 52 && Los(muzzle, tgt);
                    if (shots > 0)
                    {
                        shotT -= dt;
                        if (shotT <= 0)
                        {
                            shotT = 0.22f; shots--;
                            float tof = Vector3.Distance(eye, tgt) / 42f; var aim = tgt + V(P.vel.x, 0, P.vel.z) * tof * 0.8f; // leads you a little
                            MakeProj("ice", muzzle, (aim - muzzle).normalized * 42, 15, 4);
                            SpawnParticles(eye, 6, 4, cyanMat, 0.08f, 0.4f); Sfx("eshot", 0.6f);
                        }
                        Mats.SetAlpha(glowM, 0.9f); eg.transform.localScale = Vector3.one * 1.4f; line.SetActive(false);
                        if (shots == 0) cd = 3.2f + rand * 1.6f;
                        return;
                    }
                    if (charge > 0)
                    {
                        charge += dt; float k = charge / 1.0f;
                        Mats.SetAlpha(glowM, 0.2f + 0.7f * k); eg.transform.localScale = Vector3.one * (0.5f + k);
                        var d = tgt - eye; float len = d.magnitude; line.SetActive(true);
                        line.transform.localPosition = (eye + tgt) / 2; line.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d / len); line.transform.localScale = V(0.035f + k * 0.03f, len, 0.035f + k * 0.03f);
                        Mats.SetAlpha(lineM, (0.25f + 0.5f * k) * (Mathf.Sin(t * 30) > -0.2f ? 1 : 0.4f));
                        if (!sees) { charge = 0; line.SetActive(false); cd = 1.0f; return; } // you broke its line of sight
                        if (charge >= 1.0f) { charge = 0; shots = 3; shotT = 0; Sfx("laserTick"); }
                        return;
                    }
                    Mats.SetAlpha(glowM, 0.06f); eg.transform.localScale = Vector3.one * 0.5f; line.SetActive(false);
                    if (cd <= 0 && sees) { charge = 0.001f; Sfx("laserCharge", 0.45f); }
                });
            }
        }
        // ---------- RUNE TILES: some flagstones of the temple floor carry a ward; step on one and it lights - ice spears in half a second ----------
        void MechRuneTiles(float x0, float z0, int nx, int nz, float tile, float y, int seed, float share = 0.28f)
        {
            var rng = new System.Random(seed); var groutM = Mats.Std(0x3a3e44, 0.9f);
            for (int a = 0; a <= nx; a++) Ignore(Deco(Geo.Box(0.08f, 0.02f, nz * tile), groutM, x0 + a * tile, y + 0.02f, z0 + nz * tile / 2));
            for (int b = 0; b <= nz; b++) Ignore(Deco(Geo.Box(nx * tile, 0.02f, 0.08f), groutM, x0 + nx * tile / 2, y + 0.02f, z0 + b * tile));
            for (int a = 0; a < nx; a++) for (int b = 0; b < nz; b++)
            {
                if (rng.NextDouble() > share) continue;
                var c = V(x0 + (a + 0.5f) * tile, y, z0 + (b + 0.5f) * tile);
                var runeM = Mats.Add(0x60e0ff, 0.18f, null, true, true);
                var gp = Mk.Piv(levelRoot, c.x, y + 0.03f, c.z); gp.SetRot(0, (a * 3 + b) * 0.7f, 0);
                Mk.Msh(gp, Geo.Ring(tile * 0.28f, tile * 0.33f, 20), runeM, 0, 0, 0, -Mathf.PI / 2, 0, 0); Mk.Msh(gp, Geo.Box(tile * 0.5f, 0.01f, 0.06f), runeM, 0, 0, 0); Mk.Msh(gp, Geo.Box(0.06f, 0.01f, tile * 0.5f), runeM, 0, 0, 0, 0, 0.785f, 0);
                foreach (var r in gp.t.GetComponentsInChildren<Renderer>()) r.gameObject.layer = Mk.LayerIgnore;
                float cd = 0, lit = 0;
                trapAnims.Add((t, dt) =>
                {
                    cd -= dt; lit -= dt;
                    Mats.SetAlpha(runeM, lit > 0 ? 0.6f + 0.4f * Mathf.Sin(t * 40) : 0.14f + 0.06f * Mathf.Sin(t * 2 + a + b));
                    if (cd > 0 || !HazardLive) return;
                    bool pOn = Mathf.Abs(P.pos.x - c.x) < tile / 2 && Mathf.Abs(P.pos.z - c.z) < tile / 2 && P.pos.y >= y - 0.2f && P.pos.y < y + 0.6f;
                    bool eOn = false; foreach (var e in enemies) if (!e.dead && e.type == "husk" && e.spawnT <= 0 && Mathf.Abs(e.pos.x - c.x) < tile / 2 && Mathf.Abs(e.pos.z - c.z) < tile / 2 && e.pos.y < y + 0.6f) { eOn = true; break; }
                    if (!pOn && !eOn) return;
                    cd = 4.5f; lit = 0.6f; Sfx("charge", 0.6f); SpawnParticles(c + V(0, 0.2f, 0), 10, 2, cyanMat, 0.08f, 0.5f, -2);
                    AddSpike(c.x, y, c.z, 0.1f, true, 24);
                    for (int k = 0; k < 4; k++) { float an = k * Mathf.PI / 2 + 0.785f; AddSpike(c.x + Mathf.Cos(an) * tile * 0.3f, y, c.z + Mathf.Sin(an) * tile * 0.3f, 0.12f, true, 0); }
                });
            }
        }

        // ---------- PENDULUM BLADES: great frost-rimed blades on iron rods, swinging under the gantries ----------
        void MechPendulums((Vector3 pivot, float len, float yaw, float phase)[] pends)
        {
            var bladeM = Mats.Std(0x9aa8b4, 0.25f, 0.85f); var edgeM = Mats.Std(0xd8f0ff, 0.1f, 0.6f, 0x0a3048);
            foreach (var Pd in pends)
            {
                var outer = Mk.Piv(levelRoot, Pd.pivot.x, Pd.pivot.y, Pd.pivot.z); outer.SetRot(0, Pd.yaw, 0);
                var arm = Mk.Piv(outer, 0, 0, 0);
                Mk.Msh(outer, Geo.Cyl(0.35f, 0.35f, 1.2f, 10), darkIronM, 0, 0, 0, 0, 0, Mathf.PI / 2); // the axle
                Mk.Msh(arm, Geo.Cyl(0.12f, 0.12f, Pd.len - 0.8f, 6), darkIronM, 0, -(Pd.len - 0.8f) / 2, 0);
                var bl = Mk.Piv(arm, 0, -Pd.len, 0);
                // a crescent blade: a heavy hub and two curved wings sweeping up, the edge white with frost
                Mk.Msh(bl, Geo.Box(0.8f, 1.1f, 0.4f), darkIronM, 0, 0.3f, 0);
                for (int k = 0; k < 7; k++)
                {
                    float u = (k - 3) / 3f, x = u * 1.9f, yy = u * u * 0.9f;
                    Mk.Msh(bl, Geo.Box(0.65f, 0.9f - Mathf.Abs(u) * 0.35f, 0.09f), bladeM, x, yy, 0, 0, 0, u * 0.5f);
                    Mk.Msh(bl, Geo.Box(0.66f, 0.12f, 0.05f), edgeM, x - Mathf.Sin(u * 0.5f) * 0.42f, yy - 0.42f * Mathf.Cos(u * 0.5f) + Mathf.Abs(u) * 0.17f, 0, 0, 0, u * 0.5f);
                }
                for (int k = 0; k < 4; k++) Mk.Msh(bl, Geo.Cone(0.07f, 0.4f, 4), iceM, -1.2f + k * 0.8f, 0.9f, 0, 0, 0, 0);
                foreach (var r in outer.t.GetComponentsInChildren<Renderer>()) r.gameObject.layer = Mk.LayerIgnore;
                var q = Quaternion.Euler(0, Pd.yaw * Mathf.Rad2Deg, 0); float cd = 0, lastS = 0;
                trapAnims.Add((t, dt) =>
                {
                    float w = Mathf.PI * 2 / 3.6f, th = 0.95f * Mathf.Sin(t * w + Pd.phase), om = 0.95f * w * Mathf.Cos(t * w + Pd.phase);
                    arm.SetRot(th, 0, 0);
                    var bp = bl.t.position; cd -= dt;
                    if (Mathf.Sign(th) != Mathf.Sign(lastS) && HazardLive && Vector3.Distance(bp, P.pos) < 16) Sfx("dash", 0.4f);
                    lastS = th;
                    if (Mathf.Abs(om) > 0.8f && rand < 0.4f) SpawnParticles(bp + V(0, -0.4f, 0), 1, 1, SnowMat, 0.07f, 0.4f);
                    if (!HazardLive) return;
                    var lp = Quaternion.Inverse(q) * (P.pos + V(0, 0.9f, 0) - bp);
                    var swingDir = q * V(0, 0, -Mathf.Sign(om));
                    if (cd <= 0 && Mathf.Abs(lp.x) < 2.1f && lp.y > -1.4f && lp.y < 1.6f && Mathf.Abs(lp.z) < 1.0f && Mathf.Abs(om) > 0.4f)
                    { cd = 0.9f; HazardHitPlayer(30, bp, swingDir * 16 + V(0, 7, 0)); Sfx("slam", 0.6f); SpawnParticles(P.pos + V(0, 1, 0), 10, 5, redBloodMat, 0.08f, 0.5f); }
                    HazardHitEnemies(e => { var le = Quaternion.Inverse(q) * (e.pos + V(0, 1, 0) - bp); return Mathf.Abs(le.x) < 2.1f && Mathf.Abs(le.y) < 1.6f && Mathf.Abs(le.z) < 1.0f && Mathf.Abs(om) > 0.4f; }, 40, e => swingDir * 14 + V(0, 6, 0), 1.0f);
                });
            }
        }

        // ---------- THE WARDENS' SEARCHLIGHTS: a cold light sweeps the yard; stay in it and it locks on, goes red, and fires ----------
        void MechSearchlights((Vector3 lamp, Vector3 c, float rx, float rz, float speed, float phase)[] lights)
        {
            foreach (var Lt in lights)
            {
                var discM = Mats.Add(0xd8f0ff, 0.35f, null, true, true); var disc = Flat(Geo.Circle(3.6f, 28), discM, Lt.c.x, 0.1f, Lt.c.z);
                var rimM = Mats.Add(0xffffff, 0.5f, null, true, true); var rim = Mk.Msh(disc.transform, Geo.Ring(0.92f, 1, 32), rimM, 0, 0, 0.01f); rim.transform.localScale = Vector3.one * 3.6f; rim.layer = Mk.LayerIgnore;
                var coneM = Mats.Add(0xc8e8ff, 0.12f, null, true, true); var cone = Mk.Msh(levelRoot, Geo.Cyl(0.12f, 1, 1, 18, true), coneM); cone.layer = Mk.LayerIgnore;
                var lampGlow = Mk.Glow(levelRoot, 0xd8f0ff, 4, 0.8f); lampGlow.transform.localPosition = Lt.lamp; var lampM = lampGlow.GetComponent<Renderer>().sharedMaterial;
                var boltM = Mats.Add(0xd8f8ff, 0.9f, null, true, true); var bolt = Mk.Msh(levelRoot, Geo.Cyl(1, 1, 1, 8, true), boltM); bolt.layer = Mk.LayerIgnore; bolt.SetActive(false);
                float caught = 0, lockT = 0, cool = 0, tt = Lt.phase, boltT = 0; Vector3 spot = Lt.c, lockAt = Vector3.zero;
                trapAnims.Add((t, dt) =>
                {
                    cool -= dt; boltT -= dt;
                    if (lockT <= 0) { tt += dt * (cool > 0 ? 0.6f : 1); spot = Lt.c + V(Mathf.Sin(tt * Lt.speed) * Lt.rx, 0, Mathf.Sin(tt * Lt.speed * 2 + 0.7f) * Lt.rz); }
                    else spot = Vector3.Lerp(spot, lockAt, 1 - Mathf.Exp(-8 * dt));
                    disc.transform.localPosition = V(spot.x, 0.1f, spot.z);
                    var d = V(spot.x, 0, spot.z) - Lt.lamp; float len = d.magnitude;
                    cone.transform.localPosition = (Lt.lamp + V(spot.x, 0, spot.z)) / 2; cone.transform.localRotation = Quaternion.FromToRotation(Vector3.down, d / len); cone.transform.localScale = V(3.6f, len, 3.6f);
                    bool red = lockT > 0; int col = red ? 0xff5050 : 0xd8f0ff;
                    Mats.SetColor(discM, col); Mats.SetColor(rimM, red ? 0xff8080 : 0xffffff); Mats.SetColor(coneM, red ? 0xff6060 : 0xc8e8ff); Mats.SetColor(lampM, col);
                    Mats.SetAlpha(discM, red ? 0.35f + 0.3f * Mathf.Sin(t * 30) : 0.3f); Mats.SetAlpha(coneM, red ? 0.2f : 0.1f);
                    bolt.SetActive(boltT > 0); if (boltT > 0) Mats.SetAlpha(boltM, boltT / 0.25f);
                    if (lockT > 0)
                    {
                        lockT -= dt;
                        if (lockT <= 0)
                        {
                            // it fires: a bolt of frost from the lamp, and a blast where you were standing
                            var bp = V(lockAt.x, 0, lockAt.z); var bd = bp - Lt.lamp; float bl = bd.magnitude;
                            bolt.transform.localPosition = (Lt.lamp + bp) / 2; bolt.transform.localRotation = Quaternion.FromToRotation(Vector3.up, bd / bl); bolt.transform.localScale = V(0.35f, bl, 0.35f); boltT = 0.25f;
                            SpawnParticles(bp + V(0, 0.5f, 0), 30, 9, cyanMat, 0.14f, 0.8f); SpawnParticles(bp, 16, 6, SnowMat, 0.2f, 1); ShockRing(bp, 4.5f, 0xc8f0ff, 0.4f); FirePillar(bp, 1.6f, 9, 0xc8f0ff, 0.6f);
                            Sfx("boomSmall"); Sfx("shatter", 0.6f);
                            if (HazardLive && Hypot(P.pos.x - bp.x, P.pos.z - bp.z) < 3.6f && P.pos.y < 4) { HazardHitPlayer(24, bp, V(0, 8, 0)); chillT = Mathf.Max(chillT, 1.6f); }
                            cool = 3.0f; caught = 0;
                        }
                        return;
                    }
                    if (!HazardLive || cool > 0) { caught = 0; return; }
                    if (Hypot(P.pos.x - spot.x, P.pos.z - spot.z) < 3.6f && P.pos.y < 6) caught += dt; else caught = Mathf.Max(0, caught - dt * 2);
                    if (caught > 0.45f) { lockT = 1.0f; lockAt = V(P.pos.x, 0, P.pos.z); Sfx("alarm", 0.6f); }
                });
            }
        }
    }
}
