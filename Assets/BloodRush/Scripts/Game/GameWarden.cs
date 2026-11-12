using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    public class WardenRig
    {
        public N root, body, chest, hood, face, hourH, minH, secH, halo, lSh, lEl, lHand, rSh, rEl, rHand, staff, xL, xR, core;
        public List<N> orbit = new List<N>(); public List<GameObject> robe1 = new List<GameObject>(), phase2 = new List<GameObject>();
        public Material faceM, glowM, robeM, trimM, goldM, eyeM; public float t, handSpin = 1; public Vector3 aim; public float aimYaw;
        public List<(Transform t, Vector3 axis, float sp)> spins = new List<(Transform, Vector3, float)>(); public List<N> chains = new List<N>(); public N pendulum, bladeRing, vortex, halo2; // (the rework: GamePurgatoryMobs)
    }

    // ---------- Boss 5: THE WARDEN OF HOURS (level 20, THE HALL OF HOURS) ----------
    // A towering robed keeper of time: a hood with a clock dial for a face, its hands turning; a halo of numerals behind its
    // head; a pendulum staff; gears orbiting it. He comes through the great gate on the horizon (see the cinematic, below).
    //  TIME STOP      a dial spreads round him; if you're inside when it closes, time stops for you - he steps through the
    //                 frozen moment to stand at your back and punches you for exactly 50, flinging you across the hall (a clock
    //                 tower you're thrown into breaks and comes down, leaving a crater)
    //  TIME REWIND    his hands spin backwards: you're dragged back to where you stood a few seconds ago - your health and
    //                 ammo with you - and his own health runs back to what it was then
    //  EXPLODING CLOCKS  pocket watches lobbed round you that tick, then burst three times in a row
    //  HOUR HAND      a giant clock hand sweeps a full circle round him at knee height - jump it
    //  SANDS OF TIME  columns of golden sand pour down where you're headed, leaving slowing drifts
    //  PENDULUM       his staff swung through everything in front of him, close up
    //  phase 2 (half health): the robe burns away from the clockwork beneath, two brass arms bearing clock-hand blades unfold,
    //  his face cracks and blazes, the hall's gears start turning; he's faster and hits harder, and adds
    //  MIDNIGHT TOLL  twelve tolls of a bell - twelve rings rolling out of him, one after another
    //  SHATTERED HOURS  twelve clock-hand lances ring you like the numerals of a dial, then all fly in at once
    public partial class Game
    {
        const float WARDEN_HP = 980;
        // the hall's set pieces the fight reaches into: its gears, its clock towers, the gate's doors
        readonly List<(Transform hub, float sp)> wGears = new List<(Transform, float)>();
        readonly List<HBuild> wTowers = new List<HBuild>();
        N wDoorL, wDoorR; GameObject wPortal; Material wPortalM, wSeamM; float wGearSpeed = 0.25f;
        void WardenReset() { wGears.Clear(); wTowers.Clear(); wProj.Clear(); wSnaps.Clear(); wClocks.Clear(); wSand.Clear(); wDoorL = wDoorR = null; wPortal = null; wGearSpeed = 0.25f; wFreezeT = 0; wLaunchT = 0; WC.on = false; }

        // ================= the arena: THE HALL OF HOURS =================
        void BuildHallOfHours()
        {
            WardenReset(); DeadMats(); Arena(66, 16); MakeMirrorProbe(V(0, 12, 0), V(150, 60, 150), 1); // (something for the brass and gold to shine with)
            // (the falling-building rubble draws on these: here they're the towers' own stone)
            concM = Mats.Std(0xb8b2a4, 0.9f, 0, 0, GrimeTex()); concDarkM = Mats.Std(0x6a665e, 0.92f, 0, 0, GrimeTex()); rebarM = Mats.Std(0x9a7a40, 0.4f, 0.8f, 0, RustTex());
            var gold = Mats.Std(0xd8b860, 0.3f, 0.85f, 0, RustTex()); var brass = Mats.Std(0x9a7a40, 0.35f, 0.8f, 0, RustTex()); var dark = Mats.Std(0x2a2620, 0.6f, 0.6f, 0, GrimeTex());
            var stone = Mats.Std(0xb8b2a4, 0.9f, 0, 0, GrimeTex()); var stoneD = Mats.Std(0x6a665e, 0.92f, 0, 0, GrimeTex());
            // the floor is a vast clock dial: concentric bands, twelve numeral plates, minute ticks, a brass centre boss
            Flat(Geo.Circle(46, 72), Mats.Std(0xd8d0bc, 0.85f, 0, 0, GrimeTex()), 0, 0.02f, 0);
            foreach (var (r0, r1) in new[] { (45f, 46.5f), (38f, 38.8f), (22f, 22.6f), (8f, 8.5f) }) Flat(Geo.Ring(r0, r1, 72), brass, 0, 0.03f, 0);
            for (int k = 0; k < 60; k++) { float a = k * Mathf.PI / 30; bool hour = k % 5 == 0; Ig(Deco(Geo.Box(hour ? 1.0f : 0.3f, 0.04f, hour ? 6 : 2.2f), hour ? gold : dark, Mathf.Sin(a) * (hour ? 41.5f : 42.5f), 0.04f, Mathf.Cos(a) * (hour ? 41.5f : 42.5f), 0, a, 0)); }
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; var np = Mk.Piv(levelRoot, Mathf.Sin(a) * 31, 0.05f, Mathf.Cos(a) * 31); np.SetRot(0, a + Mathf.PI, 0); int n = k == 0 ? 12 : k; NumeralPlate(np, n, gold); }
            // the floor hands, frozen at midnight
            Ig(Deco(Geo.Box(1.4f, 0.06f, 20), dark, 0, 0.05f, 11)); Ig(Deco(Geo.Box(2.2f, 0.06f, 13), dark, 0, 0.055f, 7.5f));
            Altar(0, 0, 8, 1.2f);
            // the great gears: half sunk into the floor round the rim, and set into the walls - they turn (slowly, until he stops them)
            foreach (var (x, z, r, ry, sp) in new[] { (-52f, -40f, 9f, 0.8f, 0.3f), (52f, -40f, 9f, -0.8f, -0.3f), (-58f, 10f, 7f, Mathf.PI / 2, -0.4f), (58f, 10f, 7f, -Mathf.PI / 2, 0.4f), (-44f, 50f, 8f, 2.4f, 0.35f), (44f, 50f, 8f, -2.4f, -0.35f) })
            {
                var p = Mk.Piv(levelRoot, x, r * 0.55f, z); p.SetRot(0, ry, 0); GearDeco(p.t, r, 18, brass, out var hub); wGears.Add((hub, sp));
                SolidAABB(V(x - r * 0.6f, 0, z - r * 0.6f), V(x + r * 0.6f, r * 1.4f, z + r * 0.6f));
            }
            foreach (var (x, y, z, r, ry, sp) in new[] { (-65.6f, 9f, -24f, 6f, Mathf.PI / 2, 0.5f), (-65.6f, 6f, -10f, 3.5f, Mathf.PI / 2, -0.85f), (65.6f, 10f, 26f, 7f, -Mathf.PI / 2, -0.45f), (65.6f, 5.5f, 12f, 3f, -Mathf.PI / 2, 1.0f), (-20f, 10f, 65.6f, 6f, Mathf.PI, 0.5f), (20f, 9f, 65.6f, 5f, Mathf.PI, -0.6f), (-30f, 11f, -65.6f, 6.5f, 0, -0.4f), (30f, 11f, -65.6f, 6.5f, 0, 0.4f), (0f, 13f, -65.6f, 9f, 0, 0.22f) })
            { var p = Mk.Piv(levelRoot, x, y, z); p.SetRot(0, ry, 0); GearDeco(p.t, r, 16, ry == 0 || ry == Mathf.PI ? gold : brass, out var hub); wGears.Add((hub, sp)); }
            anims.Add((t, dt) => { foreach (var g in wGears) if (g.hub) g.hub.Rotate(0, 0, g.sp * wGearSpeed * 60 * dt, Space.Self); });
            // six clock towers round the dial: stone shafts, a clock face on each side, a bell in an open belfry, a spire
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI / 3 + Mathf.PI / 6, x = Mathf.Sin(a) * 52, z = Mathf.Cos(a) * 52; const float TW = 7, TH = 20;
                int ch0 = levelRoot.childCount, c0 = colliders.Count;
                Box(x, 0, z, TW + 1.4f, 1.6f, TW + 1.4f, stoneD); Box(x, 1.6f, z, TW, TH, TW, stone); TrimRect(x, z, TW, TW, TH + 1.6f);
                for (int s = 0; s < 4; s++)
                {
                    float fa = s * Mathf.PI / 2; var fp = V(x, 0, z) + V(Mathf.Sin(fa), 0, Mathf.Cos(fa)) * (TW / 2 + 0.06f);
                    Ig(Deco(Geo.Circle(2.3f, 28), Mats.Basic(0xffffff, ClockTex()), fp.x, TH - 2.5f, fp.z, 0, fa, 0)); Ig(Deco(Geo.Torus(2.35f, 0.18f, 28, 5), gold, fp.x, TH - 2.5f, fp.z, 0, fa, 0));
                    for (int b = 0; b < 3; b++) Ig(Deco(Geo.Box(0.3f, TH - 6, 0.3f), stoneD, fp.x + Mathf.Cos(fa) * (b - 1) * 2.4f, (TH - 6) / 2 + 1.6f, fp.z - Mathf.Sin(fa) * (b - 1) * 2.4f)); // pilasters
                }
                foreach (var cx in new[] { -1f, 1f }) foreach (var cz in new[] { -1f, 1f }) Deco(Geo.Box(0.7f, 4, 0.7f), stone, x + cx * (TW / 2 - 0.4f), TH + 3.6f, z + cz * (TW / 2 - 0.4f));
                Deco(Geo.Box(TW + 0.6f, 0.6f, TW + 0.6f), stoneD, x, TH + 5.9f, z); Deco(Geo.Cone(TW * 0.62f, 7, 4), dark, x, TH + 9.7f, z, 0, Mathf.PI / 4, 0);
                Deco(Geo.Cyl(0.9f, 1.6f, 1.8f, 14, true), gold, x, TH + 3.6f, z); // the bell
                var hb = new HBuild { mn = V(x - TW / 2 - 0.7f, 0, z - TW / 2 - 0.7f), mx = V(x + TW / 2 + 0.7f, TH + 1.6f, z + TW / 2 + 0.7f), c = V(x, 0, z), w = TW, d = TW };
                for (int i = ch0; i < levelRoot.childCount; i++) hb.objs.Add(levelRoot.GetChild(i).gameObject);
                hb.cols.AddRange(colliders.GetRange(c0, colliders.Count - c0)); wTowers.Add(hb);
            }
            // hourglass monuments between the towers (cover), sand still falling in them
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI / 3, x = Mathf.Sin(a) * 34, z = Mathf.Cos(a) * 34; if (k == 0) continue; // (the north one would stand in his way)
                Box(x, 0, z, 3.6f, 0.6f, 3.6f, gold); Box(x, 5.4f, z, 3.6f, 0.6f, 3.6f, gold);
                foreach (var cx in new[] { -1f, 1f }) foreach (var cz in new[] { -1f, 1f }) Deco(Geo.Cyl(0.15f, 0.15f, 4.8f, 6), gold, x + cx * 1.5f, 3.0f, z + cz * 1.5f);
                var gl = Mats.Trans(0xe8f0f4, 0.3f); Ig(Deco(Geo.Cyl(0.15f, 1.4f, 2.4f, 16), gl, x, 4.2f, z)); Ig(Deco(Geo.Cyl(1.4f, 0.15f, 2.4f, 16), gl, x, 1.8f, z));
                var sandM = Mats.Std(0xd8b060, 0.9f); Ig(Deco(Geo.Cone(1.1f, 0.9f, 16), sandM, x, 1.05f, z)); Ig(Deco(Geo.Cyl(0.04f, 0.04f, 2.2f, 4), sandM, x, 2.4f, z));
                SolidAABB(V(x - 1.6f, 0, z - 1.6f), V(x + 1.6f, 6, z + 1.6f));
            }
            // braziers of pale fire round the inner dial, and lanterns on the walls
            for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4 + Mathf.PI / 8; Lantern(Mathf.Sin(a) * 24, Mathf.Cos(a) * 24, 3.2f); }
            foreach (var (x, z) in new[] { (-30f, 30f), (30f, 30f), (-30f, -30f), (30f, -30f) }) PadAt(x, z, 24);
            // the GATE on the horizon (it's the fog backdrop's great gate): fit it with its doors, sealed, seams faintly glowing
            wSeamM = Mats.Add(0x80e8ff, 0.15f, null, false, true); var doorM = Silhouette(0x3a3a42, 0.97f); var doorD = Silhouette(0x2a2a30, 0.97f);
            wPortalM = Mats.Add(0x90e8ff, 0.0f, Tex.RayFade, false, true);
            wPortal = Mk.Msh(levelRoot, Geo.Plane(66, 150), wPortalM, 0, 76, -253); wPortal.layer = Mk.LayerIgnore;
            foreach (var sx in new[] { -1f, 1f })
            {
                var d = Mk.Piv(levelRoot, sx * 33, 0, -250);
                Mk.Msh(d, Geo.Box(33, 150, 3), doorM, -sx * 16.5f, 75, 0);
                for (int k = 0; k < 6; k++) Mk.Msh(d, Geo.Box(30, 1.6f, 3.6f), doorD, -sx * 16.5f, 10 + k * 26, 0); // iron bands
                Mk.Msh(d, Geo.Circle(9, 24), doorD, -sx * 16.5f, 80, 1.9f); Mk.Msh(d, Geo.Torus(9, 0.8f, 24, 4), wSeamM, -sx * 16.5f, 80, 2.0f); // a dial on each leaf
                Mk.Msh(d, Geo.Box(1.2f, 150, 3.8f), wSeamM, -sx * 0.6f, 75, 0); // the seam where the leaves meet
                foreach (var rr in d.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                if (sx < 0) wDoorL = d; else wDoorR = d;
            }
            anims.Add((t, dt) => { if (!WC.on && boss == null) Mats.SetAlpha(wSeamM, 0.12f + 0.06f * Mathf.Sin(t * 0.8f)); });
        }
        void NumeralPlate(N p, int n, Material m)
        {
            // Roman numerals laid in brass into the dial (bars of I, V and X)
            string s = n switch { 1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V", 6 => "VI", 7 => "VII", 8 => "VIII", 9 => "IX", 10 => "X", 11 => "XI", _ => "XII" };
            float w = s.Length * 1.5f, x = -w / 2 + 0.75f;
            foreach (var c in s)
            {
                if (c == 'I') Ig(Mk.Msh(p, Geo.Box(0.35f, 0.05f, 2.6f), m, x, 0, 0));
                else if (c == 'V') { Ig(Mk.Msh(p, Geo.Box(0.3f, 0.05f, 2.7f), m, x - 0.35f, 0, 0, 0, 0.3f, 0)); Ig(Mk.Msh(p, Geo.Box(0.3f, 0.05f, 2.7f), m, x + 0.35f, 0, 0, 0, -0.3f, 0)); }
                else { Ig(Mk.Msh(p, Geo.Box(0.3f, 0.05f, 3.0f), m, x, 0, 0, 0, 0.45f, 0)); Ig(Mk.Msh(p, Geo.Box(0.3f, 0.05f, 3.0f), m, x, 0, 0, 0, -0.45f, 0)); }
                x += 1.5f;
            }
        }

        // ================= the model =================
        WardenRig BuildWarden(Ent e)
        {
            var robe = EM(e, Mats.Std(0x6a6878, 0.9f, 0, 0x040408, ClothTex2(), true)); var robeD = EM(e, Mats.Std(0x484656, 0.92f, 0, 0x020204, ClothTex2(), true));
            var trim = EM(e, Mats.Std(0xd8b860, 0.3f, 0.85f, 0x100a00)); var gold = EM(e, Mats.Std(0xc8a050, 0.3f, 0.9f)); var bone = EM(e, Mats.Std(0xd8d0bc, 0.5f));
            var faceM = EM(e, Mats.Basic(0x404448, ClockTex())); var glow = EM(e, Mats.Basic(0x9af0ff)); var brass = EM(e, Mats.Std(0x9a7a40, 0.35f, 0.8f)); var dark = EM(e, Mats.Std(0x18181c, 0.8f));
            var r = new WardenRig { faceM = faceM, glowM = glow, robeM = robe, trimM = trim, goldM = gold };
            r.root = Mk.Piv(e.g.transform);
            r.body = Mk.Piv(r.root, 0, 1.6f, 0); // he floats: the hem never touches the floor
            // the robe: a long open cone falling from the waist, its hem torn into strips, gold bands and a dial-chain belt
            r.robe1.Add(Mk.Msh(r.body, Geo.Cyl(2.3f, 4.8f, 9, 24, true), robe, 0, 4.5f, 0));
            for (int k = 0; k < 18; k++) { float a = k * Mathf.PI / 9; r.robe1.Add(Mk.Msh(r.body, Geo.Box(1.5f, 2.0f + (k % 3) * 0.7f, 0.12f), robeD, Mathf.Sin(a) * 4.7f, -0.6f - (k % 3) * 0.35f, Mathf.Cos(a) * 4.7f, -0.12f * Mathf.Cos(a), a, 0)); }
            foreach (var y in new[] { 1.2f, 2.9f, 6.5f }) r.robe1.Add(Mk.Msh(r.body, Geo.Torus(4.8f - y * 0.278f + 0.06f, y < 2 ? 0.16f : 0.12f, 40, 4), trim, 0, y, 0, Mathf.PI / 2, 0, 0));
            // twelve glowing numerals round the hem, and gold seams running up the robe
            var numM = EM(e, Mats.Basic(0xffd890));
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6, rr = 4.8f - 2.05f * 0.278f + 0.08f; var np = Mk.Piv(r.body, Mathf.Sin(a) * rr, 2.05f, Mathf.Cos(a) * rr); np.SetRot(-0.27f, a, 0); r.robe1.Add(np.go); Mk.Msh(np, Geo.Box(0.18f, 0.9f, 0.05f), numM, k % 3 == 0 ? -0.18f : 0, 0, 0); if (k % 3 == 0) Mk.Msh(np, Geo.Box(0.18f, 0.9f, 0.05f), numM, 0.18f, 0, 0); }
            for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4 + Mathf.PI / 8, rr = 4.8f - 4.5f * 0.278f + 0.05f; r.robe1.Add(Mk.Msh(r.body, Geo.Box(0.1f, 9.3f, 0.06f), trim, Mathf.Sin(a) * rr, 4.5f, Mathf.Cos(a) * rr, -0.27f * Mathf.Cos(a), 0, 0.27f * Mathf.Sin(a))); }
            // the clockwork beneath (shown when the robe burns away): a cage of brass ribs round a stack of turning gears
            for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; r.phase2.Add(Mk.Msh(r.body, Geo.Box(0.25f, 9, 0.25f), brass, Mathf.Sin(a) * 2.6f, 4.5f, Mathf.Cos(a) * 2.6f, Mathf.Cos(a) * 0.12f, 0, -Mathf.Sin(a) * 0.12f)); }
            for (int k = 0; k < 4; k++) { var gp = Mk.Piv(r.body, 0, 1.5f + k * 2.1f, 0); GearDeco(gp.t, 1.6f - k * 0.15f, 12, k % 2 == 0 ? gold : brass, out var hub); gp.SetRot(Mathf.PI / 2, 0, 0); r.phase2.Add(gp.go); r.orbit.Add(new N(hub)); }
            var p2glow = Gl(r.body, 0xffb040, 7, 0.6f); p2glow.transform.localPosition = V(0, 5, 0); r.phase2.Add(p2glow);
            // chest, mantle, shoulders
            r.chest = Mk.Piv(r.body, 0, 9, 0);
            Mk.Msh(r.chest, Geo.Cyl(1.9f, 2.4f, 4.2f, 18), robe, 0, 2.1f, 0);
            Mk.Msh(r.chest, Geo.Cyl(2.6f, 3.6f, 1.5f, 20, true), robeD, 0, 3.9f, 0); Mk.Msh(r.chest, Geo.Torus(3.55f, 0.14f, 32, 4), trim, 0, 3.2f, 0, Mathf.PI / 2, 0, 0);
            Mk.Msh(r.chest, Geo.Box(0.5f, 4.0f, 0.15f), trim, 0, 2.0f, 2.05f); // the stole
            r.core = Mk.Piv(r.chest, 0, 2.2f, 1.6f); GearDeco(r.core.t, 0.9f, 10, gold, out var coreHub); r.core.SetRot(0, 0, 0); var coreG = Gl(r.core, 0x9af0ff, 2.4f, 0.7f); coreG.transform.localPosition = V(0, 0, 0.4f); r.orbit.Add(new N(coreHub));
            foreach (var sx in new[] { -1f, 1f }) { Mk.Msh(r.chest, Geo.Sphere(1.2f, 14, 10, 0, Mathf.PI * 2, 0, Mathf.PI / 2), robeD, sx * 2.6f, 3.9f, 0); Mk.Msh(r.chest, Geo.Torus(1.2f, 0.1f, 20, 4), trim, sx * 2.6f, 3.9f, 0, Mathf.PI / 2, 0, 0); }
            // the head: a deep hood, inside it a dial for a face - numerals, three hands, two pale eyes of light above the dial
            r.hood = Mk.Piv(r.chest, 0, 4.6f, 0.2f);
            var hood = Mk.Msh(r.hood, Geo.Sphere(2.0f, 18, 14, Mathf.PI / 2 + 0.62f, Mathf.PI * 2 - 1.24f, 0, Mathf.PI * 0.62f), robe, 0, 1.2f, -0.2f); // (open at the front, where the dial is) hood.transform.localScale = V(1, 1.2f, 1.05f);
            var hoodTip = Mk.Msh(r.hood, Geo.Cone(1.1f, 2.0f, 12), robe, 0, 3.4f, -0.9f, -0.5f, 0, 0);
            var hoodRim = Mk.Msh(r.hood, Geo.Torus(1.7f, 0.12f, 28, 4, Mathf.PI * 1.5f), trim, 0, 1.3f, 1.3f, 0, 0, -Mathf.PI * 0.25f);
            var hollow = Mk.Msh(r.hood, Geo.Circle(1.75f, 28), dark, 0, 1.3f, 1.2f);
            r.face = Mk.Piv(r.hood, 0, 1.15f, 1.35f);
            var dial = Mk.Msh(r.face, Geo.Circle(1.35f, 32), faceM, 0, 0, 0); var dialRim = Mk.Msh(r.face, Geo.Torus(1.38f, 0.1f, 32, 4), gold, 0, 0, 0.02f);
            r.hourH = Mk.Piv(r.face, 0, 0, 0.06f); Mk.Msh(r.hourH, Geo.Box(0.12f, 0.75f, 0.04f), dark, 0, 0.37f, 0);
            r.minH = Mk.Piv(r.face, 0, 0, 0.08f); Mk.Msh(r.minH, Geo.Box(0.08f, 1.15f, 0.04f), dark, 0, 0.57f, 0);
            r.secH = Mk.Piv(r.face, 0, 0, 0.1f); Mk.Msh(r.secH, Geo.Box(0.035f, 1.25f, 0.03f), Mats.Basic(0xb02020), 0, 0.5f, 0);
            var pin = Mk.Msh(r.face, Geo.Sphere(0.12f, 8, 6), gold, 0, 0, 0.12f);
            var eyes = new List<GameObject>(); foreach (var sx in new[] { -1f, 1f }) eyes.Add(Mk.Msh(r.hood, Geo.Sphere(0.16f, 8, 6), glow, sx * 0.55f, 2.75f, 1.0f));
            var eg = Gl(r.hood, 0x9af0ff, 0.9f, 0.5f); eg.transform.localPosition = V(0, 2.8f, 1.15f);
            Mk.Head(hood, hoodTip, hoodRim, hollow, dial, dialRim, pin, eyes[0], eyes[1]);
            // the halo of numerals turning behind his head
            r.halo = Mk.Piv(r.hood, 0, 1.6f, -1.6f);
            Mk.Msh(r.halo, Geo.Torus(3.6f, 0.1f, 48, 4), trim); Mk.Msh(r.halo, Geo.Torus(3.1f, 0.05f, 48, 4), glow);
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; Mk.Msh(r.halo, Geo.Box(0.18f, 0.7f, 0.08f), gold, Mathf.Sin(a) * 3.35f, Mathf.Cos(a) * 3.35f, 0, 0, 0, -a); }
            // arms: wide sleeves, hands of gold bone; the right holds the pendulum staff
            N Arm(int sx, out N el, out N hand)
            {
                var sh = Mk.Piv(r.chest, sx * 2.7f, 3.5f, 0);
                Mk.Msh(sh, Geo.Cyl(0.8f, 1.1f, 3.6f, 14, true), robe, 0, -1.8f, 0);
                el = Mk.Piv(sh, 0, -3.4f, 0);
                Mk.Msh(el, Geo.Cyl(0.9f, 1.5f, 3.2f, 14, true), robeD, 0, -1.6f, 0); Mk.Msh(el, Geo.Torus(1.5f, 0.1f, 20, 4), trim, 0, -3.15f, 0, Mathf.PI / 2, 0, 0);
                Mk.Msh(el, Geo.Cyl(0.32f, 0.36f, 2.6f, 8), bone, 0, -1.6f, 0);
                hand = Mk.Piv(el, 0, -3.3f, 0);
                Mk.Msh(hand, Geo.Box(0.7f, 0.5f, 0.45f), gold, 0, 0, 0);
                for (int k = 0; k < 4; k++) { var f = Mk.Piv(hand, (k - 1.5f) * 0.17f, -0.2f, 0.12f); f.SetRot(0.4f, 0, 0); Mk.Msh(f, Geo.Cyl(0.05f, 0.07f, 0.65f, 5), gold, 0, -0.32f, 0); }
                return sh;
            }
            r.rSh = Arm(1, out r.rEl, out r.rHand); r.lSh = Arm(-1, out r.lEl, out r.lHand);
            r.staff = Mk.Piv(r.rHand, 0, -0.1f, 0.1f);
            Mk.Msh(r.staff, Geo.Cyl(0.16f, 0.16f, 15, 8), gold, 0, 1.5f, 0);
            var bob = Mk.Piv(r.staff, 0, 9.4f, 0); Mk.Msh(bob, Geo.Cyl(1.3f, 1.3f, 0.3f, 24), gold, 0, 0, 0, Mathf.PI / 2, 0, 0); Mk.Msh(bob, Geo.Circle(1.1f, 24), faceM, 0, 0, 0.17f); Mk.Msh(bob, Geo.Torus(1.35f, 0.08f, 24, 4), glow, 0, 0, 0);
            Mk.Msh(r.staff, Geo.Sphere(0.4f, 10, 8), gold, 0, -6.1f, 0);
            // gears orbiting him
            for (int k = 0; k < 6; k++) { var o = Mk.Piv(r.body, 0, 6 + (k % 3) * 2.6f, 0); var g = Mk.Piv(o, 6.2f + (k % 2) * 1.2f, 0, 0); GearDeco(g.t, 0.7f + (k % 3) * 0.25f, 9, k % 2 == 0 ? gold : brass, out var gh); r.orbit.Add(o); }
            // phase 2's extra arms: brass, unfolding from his back, each bearing a clock-hand blade
            foreach (var sx in new[] { -1f, 1f })
            {
                var xa = Mk.Piv(r.chest, sx * 1.6f, 3.2f, -1.6f); xa.SetRot(-0.6f, 0, sx * 1.2f);
                Mk.Msh(xa, Geo.Box(0.4f, 4.5f, 0.4f), brass, 0, 2.2f, 0); var xh = Mk.Piv(xa, 0, 4.5f, 0); xh.SetRot(0.6f, 0, 0);
                Mk.Msh(xh, Geo.Box(0.6f, 7, 0.12f), gold, 0, 3.5f, 0); Mk.Msh(xh, Geo.Cone(0.35f, 1.4f, 4), gold, 0, 7.6f, 0); Mk.Msh(xh, Geo.Box(0.65f, 7, 0.02f), glow, 0, 3.5f, 0.08f);
                foreach (var rr in xa.t.GetComponentsInChildren<Renderer>()) r.phase2.Add(rr.gameObject);
                if (sx < 0) r.xL = xa; else r.xR = xa;
            }
            WardenParts(e, r);
            foreach (var g in r.phase2) g.SetActive(false);
            e.mats.Clear(); e.mats.Add(robe); e.mats.Add(robeD); e.mats.Add(trim); e.mats.Add(bone);
            return r;
        }

        Ent SpawnWarden()
        {
            var e = NewEnt("boss"); var sp = LEVEL.bossSpawn;
            e.wd = BuildWarden(e);
            e.boss = 5; e.name = "THE WARDEN OF HOURS"; e.heavy = true; e.s = 3.2f;
            e.hp = e.maxHp = WARDEN_HP * CoopBossHp; e.headY = 15.5f; e.bodyY = 11f; e.hitR = 4.2f; e.r = 3.0f; e.h = 17;
            e.invuln = true; e.state = "entrance"; e.stateT = 99;
            e.pos = V(0, 52, -262); e.yaw = 0; // he waits behind the sealed gate
            FinishEnt(e); SyncEnt(e); boss = e;
            foreach (var m in e.mats) Mats.Emissive(m, 0);
            StartWardenCine(e);
            return e;
        }
        static void WState(Ent e, string s, float dur) { e.state = s; e.stateT = dur; e.vidx = 0; e.hitPlayer = false; if (e.wd != null) e.wd.t = 0; }
        Vector3 WFwd(Ent e) => V(Mathf.Sin(e.yaw), 0, Mathf.Cos(e.yaw));

        // ================= THE ENTRANCE (a cinematic) =================
        class WardenCine { public bool on; public float t, sun0, fog0; public Vector3 lockPos; public Vector3 camPos; public Quaternion camRot; public float fov; public bool gearsStopped, titled; }
        readonly WardenCine WC = new WardenCine();
        Quad wDarkOv, wFreezeOv;
        static readonly Vector3 W_ARRIVE = new Vector3(0, 0, -30);
        void StartWardenCine(Ent e)
        {
            WC.on = true; WC.t = 0; WC.lockPos = P.pos; WC.sun0 = sunLight ? sunLight.intensity : 1; WC.fog0 = RenderSettings.fogEndDistance; WC.gearsStopped = false; WC.titled = false;
            WC.camPos = camT.position; WC.camRot = camT.rotation; WC.fov = cam.fieldOfView;
            RenderSettings.fogEndDistance = Mathf.Max(WC.fog0, 420); // (so the gate and what comes out of it can be seen)
            audioMgr.Stop(1.2f); Sfx("rumble", 0.6f);
            if (!wDarkOv) { wDarkOv = Qd(hudRT, new Color(0, 0, 0, 0), "wdark"); Fill(wDarkOv.rectTransform); wDarkOv.raycastTarget = false; wDarkOv.transform.SetAsFirstSibling(); }
        }
        bool WardenCineOn => WC.on;
        // the timeline (seconds): 0 warning . 2.6 gate opens . 6.4 his silhouette lights up . 7.8 he comes through . 13.6 arrival
        // . 16.0 title card . then the fight
        void UpdateWardenEntrance(Ent e, float dt)
        {
            var r = e.wd; WC.t += dt; float T = WC.t;
            P.pos = WC.lockPos; P.vel = Vector3.zero; mouseDown = false; // (nothing can be done in the meantime: he can't be hurt either)
            // the warning: the light goes, the hall falls silent but for a slow, deep ticking; the gate's seams start to glow
            float dark = Mathf.Clamp01(T / 2.0f) * (1 - Mathf.Clamp01((T - 15.5f) / 1.2f));
            if (sunLight) sunLight.intensity = WC.sun0 * (1 - 0.5f * dark);
            if (wDarkOv) wDarkOv.Solid(new Color(0.02f, 0.03f, 0.05f, 0.32f * dark));
            if (T < 13.6f && (int)(T / 0.75f) != (int)((T - dt) / 0.75f)) Sfx(((int)(T / 0.75f) & 1) == 0 ? "tick" : "tock", 0.9f);
            if (T >= 1.0f && T - dt < 1.0f) Sfx("creak", 0.8f);
            Mats.SetAlpha(wSeamM, Mathf.Lerp(0.12f, 0.95f, Mathf.Clamp01(T / 2.6f)) * (0.85f + 0.15f * Mathf.Sin(T * 9)));
            // the gate opens: the leaves swing back, slow and enormous, dust and stone falling from the arch, light pouring through
            float open = Smoother(Mathf.Clamp01((T - 2.6f) / 3.8f));
            if (wDoorL != null) wDoorL.SetRot(0, open * 1.75f, 0); if (wDoorR != null) wDoorR.SetRot(0, -open * 1.75f, 0);
            if (wPortalM) Mats.SetAlpha(wPortalM, 0.55f * open);
            if (T >= 2.6f && T - dt < 2.6f) { Sfx("gate"); Sfx("boom", 0.6f); }
            if (T > 2.6f && T < 6.6f)
            {
                P.shake = Mathf.Max(P.shake, 0.18f);
                if (rand < 0.8f) SpawnParticles(V((rand - 0.5f) * 70, 150 + rand * 8, -248), 1, 3, dustMat, 3 + rand * 3, 4, 8); // dust and grit off the arch
                if (rand < 0.5f) SpawnParticles(V((rand - 0.5f) * 66 * open, 4, -246), 2, 6, dustMat, 4 + rand * 4, 3, -1);
                if (rand < 0.3f) particles.Add(new Pt { pos = V((rand - 0.5f) * 60, 150, -248), scale = Vector3.one * (1.5f + rand * 2.5f), mat = concDarkM ? concDarkM : dustMat, v = V(0, -5, 4), life = 6, grav = GRAV, hasSpin = true, spin = Random.insideUnitSphere * 2 });
                if (rand < 0.4f) SpawnParticles(V((rand - 0.5f) * 50 * open, rand * 140, -251), 1, 4, cyanMat, 1.5f, 2.5f, -2); // time-light leaking through
            }
            // the reveal: his silhouette behind the gate; the dial in his hood lights up and its hands begin to turn
            float lit = Smoother(Mathf.Clamp01((T - 6.4f) / 1.6f));
            Mats.SetColor(r.faceM, Color.Lerp(new Color(0.2f, 0.21f, 0.22f), Color.white, lit)); Mats.SetColor(r.glowM, Color.Lerp(new Color(0.1f, 0.2f, 0.25f), new Color(0.6f, 0.95f, 1f), lit));
            r.handSpin = lit * 1;
            if (T >= 6.4f && T - dt < 6.4f) { Sfx("toll", 0.7f); Sfx("growlLow"); }
            // the entrance: he glides out through the gate, over the wall and down into the hall, time-light streaming off him
            Vector3 p;
            if (T < 7.8f) p = V(0, 52, -262);
            else if (T < 13.6f)
            {
                float k = Smoother((T - 7.8f) / 5.8f);
                var a = V(0, 52, -262); var b = V(0, 34, -120); var c = W_ARRIVE + V(0, 6, 0); // a curve: out of the gate, over the wall, down
                p = Vector3.Lerp(Vector3.Lerp(a, b, k), Vector3.Lerp(b, c, k), k);
                if (rand < 0.9f) SpawnParticles(p + V((rand - 0.5f) * 8, 2 + rand * 14, (rand - 0.5f) * 8), 2, 3, rand < 0.5f ? cyanMat : goldSpark, 0.3f + rand * 0.4f, 1.6f, 0);
                if ((int)(T / 1.1f) != (int)((T - dt) / 1.1f)) { Sfx("rumble", 0.7f); P.shake = Mathf.Max(P.shake, 0.35f); }
                if (T >= 7.8f && T - dt < 7.8f) Sfx("dashGo");
            }
            else
            {
                float k = Smoother(Mathf.Clamp01((T - 13.6f) / 0.8f)); p = W_ARRIVE + V(0, 6 * (1 - k), 0);
                if (T >= 14.4f && T - dt < 14.4f)
                {
                    // he settles: the floor rings, dust rolls out
                    Sfx("slam"); Sfx("boom"); P.shake = Mathf.Max(P.shake, 1.0f); AddRing(W_ARRIVE, 22, 30, 0, 0x9af0ff); ShockRing(W_ARRIVE, 18, 0x9af0ff, 0.6f);
                    SpawnParticles(W_ARRIVE + V(0, 1, 0), 90, 16, dustMat, 0.8f, 2.4f);
                }
            }
            e.pos = p; e.yaw = Mathf.Atan2(WC.lockPos.x - p.x, WC.lockPos.z - p.z);
            // arrival: he raises his arm - and every gear in the hall grinds to a stop; then he turns to you
            float raise = T > 14.4f ? Smoother(Mathf.Clamp01((T - 14.4f) / 0.6f)) * (1 - Smoother(Mathf.Clamp01((T - 15.6f) / 0.5f))) : 0;
            if (!WC.gearsStopped && T >= 15.0f) { WC.gearsStopped = true; wGearSpeed = 0; Sfx("clank"); Sfx("toll"); P.shake = Mathf.Max(P.shake, 0.6f); foreach (var g in wGears) if (g.hub) SpawnParticles(g.hub.position, 10, 5, sparkMat, 0.1f, 0.5f); }
            r.aim = V(raise, 0, 0);
            AnimateWarden(e, dt, raise);
            SyncEnt(e);
            if (T >= 16.0f && !WC.titled)
            {
                WC.titled = true; WC.on = false; RenderSettings.fogEndDistance = WC.fog0; if (sunLight) sunLight.intensity = WC.sun0; if (wDarkOv) wDarkOv.Solid(new Color(0, 0, 0, 0));
                if (wPortalM) Mats.SetAlpha(wPortalM, 0.25f);
                var to = e.pos - P.pos; P.yaw = Mathf.Atan2(-to.x, -to.z); P.pitch = 0.15f;
                ShowBossBar(e); BossBarTint(C("#e8d8a0"), C("#7a6a30")); audioMgr.Start(11); StartBossIntro(e); Sfx("toll"); Sfx("roar", 0.6f); whiteFlash = Mathf.Max(whiteFlash, 0.15f);
                if (!BossIntroActive) { Banner($"WAVE {wave}", "", THEME.banner); vm.gameObject.SetActive(true); }
                WState(e, "opening", 0.6f);
            }
        }
        // runs after UpdateCamera: the entrance's camera
        void UpdateWardenCam(float rdt)
        {
            if (!WC.on || boss == null || boss.wd == null) return;
            var e = boss; float T = WC.t;
            var eye = WC.lockPos + V(0, 1.7f, 0); var gate = V(0, 70, -250); Vector3 pos, look; float fov;
            // (the camera stands off to one side of you, so the hall's centre crystal never sits between it and the gate)
            var off = V(9, 0.4f, -4);
            if (T < 6.4f) { pos = eye + off + (gate - eye).normalized * T * 0.9f; look = gate + V(0, -12, 0); fov = Mathf.Lerp(78, 50, Smoother(T / 6.4f)); }
            else if (T < 7.8f) { pos = eye + off + (gate - eye).normalized * 5.8f; look = e.pos + V(0, 12, 0); fov = Mathf.Lerp(50, 24, Smoother((T - 6.4f) / 1.4f)); }
            else if (T < 13.6f)
            {
                float k = Smoother((T - 7.8f) / 5.8f);
                var side = Vector3.Cross(Vector3.up, (e.pos - eye).normalized);
                pos = eye + off + side * (4 * k) + V(0, 1 + 3 * k, 0); look = e.pos + V(0, 11, 0); fov = Mathf.Lerp(24, 62, k);
            }
            else
            {
                var fwd = (WC.lockPos - e.pos); fwd.y = 0; fwd = fwd.normalized; var side = Vector3.Cross(Vector3.up, fwd);
                pos = e.pos + fwd * 22 + side * 6 + V(0, 3.5f, 0); look = e.pos + V(0, 11, 0); fov = 50;
            }
            var rot = Quaternion.LookRotation(pos - look);
            float s = 1 - Mathf.Exp(-5 * rdt);
            WC.camPos = Vector3.Lerp(WC.camPos, pos, T < 0.1f ? 1 : s); WC.camRot = Quaternion.Slerp(WC.camRot, rot, T < 0.1f ? 0.2f : s); WC.fov = Mathf.Lerp(WC.fov, fov, s);
            camT.position = WC.camPos + Random.insideUnitSphere * P.shake * 0.12f; camT.rotation = WC.camRot; cam.fieldOfView = WC.fov;
            vm.gameObject.SetActive(false);
            // letterbox bars
            introGroup.alpha = 1; introName.anchoredPosition = V(-4000, 0, 0); float bar = 110 * Mathf.Min(1, T / 0.8f); barTop.sizeDelta = new Vector2(0, bar); barBot.sizeDelta = new Vector2(0, bar);
        }

        // ================= the fight =================
        class WProj { public Transform t; public Vector3 v; public float dmg, r, life; public bool lance; }
        readonly List<WProj> wProj = new List<WProj>();
        class WClock { public Transform t; public Vector3 v; public float fuse; public int bursts; public bool landed; public float nextBurst; public GameObject warn; public Material wm; }
        readonly List<WClock> wClocks = new List<WClock>();
        class WSand { public Vector3 p; public float t, delay, life, colT; public bool hit, poured; public GameObject warn, col, drift; public Transform colP; public Material wm, cm, coreM; }
        readonly List<WSand> wSand = new List<WSand>();
        readonly List<(float t, Vector3 pos, float hp, int[] ammo, float bossHp)> wSnaps = new List<(float, Vector3, float, int[], float)>();
        Vector3 wLaunchV; float wSnapT, wFreezeT, wLaunchT, wRewindCd; Vector3 wFreezePos; int wTestIdx; string wTestState;
        void UpdateWarden(Ent e, float dt)
        {
            var r = e.wd; UpdateTraitorFx(dt); UpdateWardenFx(e, dt);
            if (testMode && e.state != wTestState) { wTestState = e.state; System.IO.File.AppendAllText(System.IO.Path.Combine(testOut, "events.txt"), $"{testT:F1} warden {e.state} hp={e.hp:F0}/{e.maxHp:F0} p2={e.phase2} player={P.hp:F0} at {P.pos}\n"); }
            // you can't stand inside him: pushed out of his robes
            if (e.state != "entrance" && wFreezeT <= 0) { var o = P.pos - e.pos; o.y = 0; float od = o.magnitude; if (od < 5.2f && P.pos.y < e.pos.y + 14) { o = od > 0.01f ? o / od : -WFwd(e); P.pos += o * (5.2f - od); var vr = Vector3.Dot(P.vel, -o); if (vr > 0) P.vel += o * vr; } }
            if (e.state == "entrance") { UpdateWardenEntrance(e, dt); return; }
            float sp = e.phase2 ? 1.6f : 1.3f, dmgK = e.phase2 ? 1.2f : 1; // (he works fast: every wind-up and recovery runs at this rate)
            float sdt = dt * sp; r.t += sdt; e.stateT -= sdt; e.flash -= dt;
            float dx = Tgt.pos.x - e.pos.x, dz = Tgt.pos.z - e.pos.z, dist = Hypot(dx, dz); if (dist == 0) dist = 1; // (co-op: he hunts Tgt; every hit test is on the local P)
            bool face = true; float move = 0;
            // the past, recorded: where you were, your health and ammo, and his health (the rewind reaches back into this)
            wSnapT -= dt; if (wSnapT <= 0) { wSnapT = 0.5f; wSnaps.Add((time, P.pos, P.hp, (int[])ammo.Clone(), e.hp)); if (wSnaps.Count > 20) wSnaps.RemoveAt(0); }
            wRewindCd -= dt;
            // phase 2 at half health
            if (!e.phase2 && e.hp < e.maxHp * 0.5f && e.state != "stopFreeze" && e.state != "stopPunch")
            { e.phase2 = true; WState(e, "transform", 3.4f); Sfx("toll"); Sfx("roar"); Banner("THE LAST HOUR", "THE WARDEN OF HOURS", "#ffd080"); P.shake = 1.2f; audioMgr.Intensity(); TryBossCine(5, 1, e); }
            switch (e.state)
            {
                case "opening": face = true; if (e.stateT <= 0 && !BossIntroActive) { e.invuln = false; WState(e, "chase", 1.0f); if (testMode && Arg("-brwp2") != null) e.hp = e.maxHp * 0.49f; } break;
                case "chase":
                    move = dist > 14 ? (e.phase2 ? 12 : 9.5f) : dist < 8 ? -5 : 0;
                    if (e.stateT <= 0 && Auth) // co-op: the host picks; clients run the same setup when its pick arrives (NetBossBegin)
                    {
                        var opts = dist < 11 ? new List<string> { "pendulum", "sweep", "stop", "clocks" } : new List<string> { "clocks", "sand", "stop", "sweep", "clocks" };
                        if (wRewindCd <= 0 && wSnaps.Count > 8) opts.Add("rewind");
                        if (e.phase2) { opts.Add("midnight"); opts.Add("lances"); opts.Add("lances"); }
                        opts.RemoveAll(s => s == e.last); string a = opts[Mathf.Min(opts.Count - 1, (int)(rand * opts.Count))]; e.last = a;
                        if (testMode && Arg("-brwmove") != null) { var ms = Arg("-brwmove").Split(','); a = ms[wTestIdx++ % ms.Length]; } // -brwmove a,b,..: force his attacks in turn (test)
                        NetAct(e, a); WardenBegin(e, a);
                    }
                    break;
                // ---- TIME STOP ----
                case "stopWind":
                    face = false; if ((int)(r.t / 0.12f) != (int)((r.t - sdt) / 0.12f)) Sfx("tick", 0.6f); // ticking faster and faster
                    r.handSpin = 1 + r.t * 8;
                    if (rand < 0.8f) { float a = rand * Mathf.PI * 2; SpawnParticles(V(e.pos.x + Mathf.Sin(a) * StopR(e), 0.3f, e.pos.z + Mathf.Cos(a) * StopR(e)), 1, 1, cyanMat, 0.15f, 0.8f, -2); }
                    if (e.stateT <= 0)
                    {
                        Sfx("timeStop"); ShockRing(V(e.pos.x, 0.2f, e.pos.z), StopR(e), 0x9af0ff, 0.5f); whiteFlash = Mathf.Max(whiteFlash, 0.15f);
                        // co-op: whoever is inside his reach is caught (on their own machine); he goes for his target
                        if (!P.dead && !P.downed && Hypot(P.pos.x - e.pos.x, P.pos.z - e.pos.z) < StopR(e) && P.pos.y < e.pos.y + 12) WardenFreezeMe();
                        if (!Down(Tgt) && Hypot(dx, dz) < StopR(e) && Tgt.pos.y < e.pos.y + 12) WardenCatch(e);
                        else if (!Down(Tgt))
                        {
                            // out of his reach - so the stillness comes for you: a dial of frozen time closes on the spot where you
                            // stand. Get out of it before it shuts, or you're caught all the same.
                            wSnareAt = V(Tgt.pos.x, Mathf.Max(0, FloorBelow(Tgt.pos + V(0, 0.3f, 0))), Tgt.pos.z);
                            float snT = e.phase2 ? 0.85f : 1.05f; WState(e, "stopSnare", snT);
                            WarnDisc(wSnareAt, SNARE_R, snT / sp, 0x9af0ff); Sfx("creak", 0.8f); Sfx("charge");
                            SnareRing(wSnareAt, snT / sp);
                        }
                        else { WState(e, "recover", 1.0f); Sfx("deny"); }
                    }
                    break;
                case "stopSnare":
                    face = true; r.handSpin = 1 + r.t * 10;
                    if ((int)(r.t / 0.1f) != (int)((r.t - sdt) / 0.1f)) Sfx("tick", 0.5f);
                    if (rand < 0.8f) { float a = rand * Mathf.PI * 2; SpawnParticles(wSnareAt + V(Mathf.Sin(a) * SNARE_R, 0.2f, Mathf.Cos(a) * SNARE_R), 1, 1, cyanMat, 0.12f, 0.6f, -2); }
                    if (e.stateT <= 0)
                    {
                        Sfx("timeStop"); ShockRing(wSnareAt + V(0, 0.2f, 0), SNARE_R, 0x9af0ff, 0.4f);
                        if (!P.dead && !P.downed && Hypot(P.pos.x - wSnareAt.x, P.pos.z - wSnareAt.z) < SNARE_R && Mathf.Abs(P.pos.y - wSnareAt.y) < 3.5f) WardenFreezeMe();
                        if (!Down(Tgt) && Hypot(Tgt.pos.x - wSnareAt.x, Tgt.pos.z - wSnareAt.z) < SNARE_R && Mathf.Abs(Tgt.pos.y - wSnareAt.y) < 3.5f) WardenCatch(e);
                        else { WState(e, "recover", 0.9f); Sfx("deny"); }
                        if (testMode) System.IO.File.AppendAllText(System.IO.Path.Combine(testOut, "events.txt"), $"{testT:F1} snare closed, player {(e.state == "stopFreeze" ? "CAUGHT" : "dodged")} dist={Hypot(P.pos.x - wSnareAt.x, P.pos.z - wSnareAt.z):F1}\n");
                    }
                    break;
                case "stopFreeze": face = true; if (e.stateT <= 0) { WState(e, "stopPunch", 0.55f); Sfx("charge"); } break;
                case "stopPunch":
                    face = true;
                    if (e.stateT <= 0)
                    {
                        // the punch: exactly 50, an impact frame, and you're thrown across the hall
                        bool me = wCaught; wCaught = false; wFreezeT = 0; var dir = (P.pos - e.pos); dir.y = 0; dir = dir.sqrMagnitude > 0.01f ? dir.normalized : WFwd(e);
                        if (!me) { Sfx("slam"); Sfx("boom"); Blast(ChestOf(Tgt), 3, 0xffffff); WState(e, "recover", 1.3f); break; } // (a teammate takes the punch: we see it land)
                        float ifr = P.iframes; P.iframes = 0; DamagePlayer(50, e.pos, false, true); P.iframes = Mathf.Max(ifr, 0.6f);
                        P.vel = dir * 46 + V(0, 13, 0); P.onGround = false; P.coyote = 0; wLaunchT = 1.8f; wLaunchV = dir * 46; P.knockT = 1.4f; P.dashT = 0;
                        freeze = Mathf.Max(freeze, 0.16f); whiteFlash = Mathf.Max(whiteFlash, 0.4f); P.shake = 1.6f; hurtFlash = Mathf.Max(hurtFlash, 0.5f);
                        if (wFreezeOv) wFreezeOv.Solid(new Color(0, 0, 0, 0.85f)); // the impact frame: the world goes black for the held instant of the hit, then the flash
                        if (testMode) System.IO.File.AppendAllText(System.IO.Path.Combine(testOut, "events.txt"), $"{testT:F1} TIME STOP PUNCH launch dir={dir}\n");
                        Sfx("slam"); Sfx("boom"); Sfx("bang"); Blast(ChestPos, 3, 0xffffff); ShockRing(ChestPos, 6, 0x9af0ff, 0.35f);
                        WState(e, "recover", 1.3f);
                    }
                    break;
                // ---- TIME REWIND ----
                case "rewindWind":
                    face = false; r.handSpin = -10; if (rand < 0.9f) SpawnParticles(BodyPoint(e) + Random.insideUnitSphere * 8, 1, 2, goldSpark, 0.2f, 0.8f, 0);
                    if (e.stateT <= 0)
                    {
                        // reach back 3-5 seconds
                        float back = 3.6f; if (wSnaps.Count == 0) wSnaps.Add((time, P.pos, P.hp, (int[])ammo.Clone(), e.hp)); var snap = wSnaps[0]; foreach (var s in wSnaps) if (time - s.t >= back) snap = s; // (every player is pulled back along their own past)
                        for (int k = 0; k < 10; k++) SpawnParticles(Vector3.Lerp(P.pos, snap.pos, k / 9f) + V(0, 1, 0), 4, 1, goldSpark, 0.2f, 1.2f, 0); // the ghost trail of where you're dragged back along
                        P.pos = snap.pos; P.vel = Vector3.zero; P.hp = Mathf.Max(1, snap.hp); for (int i = 0; i < ammo.Length && i < snap.ammo.Length; i++) ammo[i] = snap.ammo[i];
                        float was = e.hp; if (Auth) e.hp = Mathf.Max(e.hp, Mathf.Min(e.maxHp, snap.bossHp)); // his health runs back to what it was then (the host's record: clients get it in the snapshot)
                        if (e.phase2 && e.hp > e.maxHp * 0.5f) e.hp = e.maxHp * 0.5f - 1; // (he can't rewind out of his last hour)
                        Sfx("rewind"); Sfx("toll", 0.6f); whiteFlash = Mathf.Max(whiteFlash, 0.3f); P.shake = Mathf.Max(P.shake, 0.6f);
                        ShockRing(P.pos, 4, 0xffd080, 0.5f); ShockRing(V(e.pos.x, 0.2f, e.pos.z), 10, 0xffd080, 0.6f);
                        if (e.hp > was + 1) SpawnParticles(BodyPoint(e), 60, 8, goldSpark, 0.2f, 1.0f, 0);
                        WState(e, "recover", 1.2f);
                    }
                    break;
                // ---- EXPLODING CLOCKS ----
                case "clocksWind":
                    face = true;
                    if (e.stateT <= 0)
                    {
                        int n = e.phase2 ? 10 : 7; var from = e.wd.lHand.t.position;
                        for (int k = 0; k < n; k++)
                        {
                            var tgt = Tgt.pos + V(Tgt.vel.x, 0, Tgt.vel.z) * 0.6f + (k == 0 ? Vector3.zero : Quaternion.Euler(0, k * 360f / n, 0) * V(0, 0, 5 + rand * 4));
                            float lim = LB - 3; tgt.x = Mathf.Clamp(tgt.x, -lim, lim); tgt.z = Mathf.Clamp(tgt.z, -lim, lim); tgt.y = 0;
                            float tof = 0.6f + rand * 0.22f; var v = (tgt - from) / tof; v.y += 0.5f * GRAV * tof;
                            var ct = Mk.Piv(levelRoot, from.x, from.y, from.z); Mk.Msh(ct, Geo.Cyl(0.45f, 0.45f, 0.18f, 16), e.wd.goldM, 0, 0, 0, Mathf.PI / 2, 0, 0); Mk.Msh(ct, Geo.Circle(0.4f, 16), e.wd.faceM, 0, 0, 0.1f); Mk.Msh(ct, Geo.Torus(0.12f, 0.04f, 8, 4), e.wd.goldM, 0, 0.55f, 0);
                            foreach (var rr in ct.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                            wClocks.Add(new WClock { t = ct.t, v = v, fuse = 0, bursts = 3 }); // (they go off the moment they land)
                        }
                        Sfx("dashGo"); WState(e, "recover", 0.8f);
                    }
                    break;
                // ---- HOUR HAND SWEEP ----
                case "sweepWind": face = false; if (e.stateT <= 0) { WState(e, "sweep", 1.1f); Sfx("dashGo"); SpawnSweepHand(e); } break;
                case "sweep": face = false; if (e.stateT <= 0) WState(e, "recover", 0.7f); break;
                // ---- SANDS OF TIME ----
                case "sandWind":
                    face = false;
                    if (e.stateT <= 0)
                    {
                        int n = e.phase2 ? 7 : 5; float lim = LB - 4;
                        for (int k = 0; k < n; k++) { var p = Tgt.pos + V(Tgt.vel.x, 0, Tgt.vel.z) * (0.8f + k * 0.25f) + (k == 0 ? Vector3.zero : V((rand - 0.5f) * 16, 0, (rand - 0.5f) * 16)); p.x = Mathf.Clamp(p.x, -lim, lim); p.z = Mathf.Clamp(p.z, -lim, lim); p.y = Mathf.Max(0, FloorBelow(V(p.x, Tgt.pos.y + 0.2f, p.z))); WardenSand(p, 1.1f + k * 0.18f); }
                        WState(e, "recover", 0.8f);
                    }
                    break;
                // ---- PENDULUM ----
                case "pendWind": face = false; TurnTo(e, r.aimYaw, 4, dt); if (e.stateT <= 0) { WState(e, "pend", 0.4f); Sfx("dashGo"); } break;
                case "pend":
                    face = false;
                    if (!e.hitPlayer && r.t > 0.12f)
                    {
                        e.hitPlayer = true; Sfx("slam", 0.8f); P.shake = Mathf.Max(P.shake, 0.5f);
                        for (int k = 0; k < 14; k++) { float a = r.aimYaw + (k / 13f - 0.5f) * 2.2f; SpawnParticles(e.pos + V(Mathf.Sin(a), 0, Mathf.Cos(a)) * (5 + rand * 9) + V(0, 1, 0), 1, 4, goldSpark, 0.2f, 0.6f); }
                        if (!P.dead && InFan(e.pos, r.aimYaw, 15.5f, 1.1f, P.pos) && P.pos.y < e.pos.y + 6) { DamagePlayer(30 * dmgK, e.pos); P.vel += WFwd(e) * 16 + V(0, 8, 0); P.onGround = false; }
                    }
                    if (e.stateT <= 0) WState(e, "recover", 0.8f);
                    break;
                // ---- phase 2: the transformation ----
                case "transform":
                {
                    face = false; float k = Mathf.Clamp01(r.t / 3.0f);
                    if (r.t < 0.05f) { e.invuln = true; WState(e, "transform", 3.4f); r.t = 0.06f; }
                    if (rand < 0.9f) SpawnParticles(BodyPoint(e) + Random.insideUnitSphere * 5, 2, 4, emberMat, 0.2f, 1.0f, -1); // the robe burning away
                    if (r.t > 1.2f && r.robe1.Count > 0 && r.robe1[0].activeSelf)
                    {
                        foreach (var g in r.robe1) if (g) { particles.Add(new Pt { pos = g.transform.position, scale = Vector3.one * 0.8f, mat = ashMat, v = Random.insideUnitSphere * 4 + V(0, 3, 0), life = 2, grav = -2 }); g.SetActive(false); }
                        foreach (var g in r.phase2) if (g) g.SetActive(true);
                        Blast(BodyPoint(e), 10, 0xffb040); Sfx("boom"); Sfx("shatter"); P.shake = 1.2f; whiteFlash = 0.3f;
                        Mats.SetColor(r.faceM, new Color(1f, 0.8f, 0.55f)); Mats.SetColor(r.glowM, new Color(1f, 0.7f, 0.3f));
                        wGearSpeed = 1.6f; Sfx("clank"); // the hall's gears start turning
                        BossBarTint(C("#ffb040"), C("#902800"));
                    }
                    if (e.stateT <= 0) { e.invuln = false; WState(e, "stunned", 2.6f); } // (and he reels for a moment - executable)
                    break;
                }
                // ---- MIDNIGHT TOLL ----
                case "midWind":
                    face = false; if (e.stateT <= 0) { WState(e, "midnight", 12 * 0.42f); e.vidx = 0; }
                    break;
                case "midnight":
                    face = false;
                    if (r.t >= e.vidx * 0.42f && e.vidx < 12) { e.vidx++; bool big = e.vidx % 2 == 0; AddRing(V(e.pos.x, 0, e.pos.z), 20, 46, (big ? 18 : 12) * dmgK, big ? 0xffd080 : 0xd0a050); /* (every second toll sends a ring that hurts - jump them on the beat) */ Sfx("toll", 0.5f + 0.04f * e.vidx); P.shake = Mathf.Max(P.shake, 0.3f); }
                    if (e.stateT <= 0) WState(e, "recover", 1.2f);
                    break;
                // ---- SHATTERED HOURS ----
                case "lanceWind":
                    face = false;
                    if (e.stateT <= 0)
                    {
                        var c = Tgt.pos + V(0, 1.2f, 0); // the lances ring his target (and hurt whoever they reach)
                        for (int k = 0; k < 12; k++)
                        {
                            float a = k * Mathf.PI / 6; var p0 = c + V(Mathf.Sin(a), 0, Mathf.Cos(a)) * 11 + V(0, 2 + (k % 2) * 1.5f, 0);
                            var lt = Mk.Piv(levelRoot, p0.x, p0.y, p0.z); Mk.Msh(lt, Geo.Box(0.3f, 0.12f, 4.5f), r.goldM, 0, 0, 0); Mk.Msh(lt, Geo.Cone(0.3f, 1.2f, 4), r.goldM, 0, 0, 2.8f, Mathf.PI / 2, 0, 0); Mk.Msh(lt, Geo.Box(0.32f, 0.02f, 4.5f), r.glowM, 0, 0.08f, 0);
                            lt.t.rotation = Quaternion.LookRotation(c - p0);
                            foreach (var rr in lt.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                            wProj.Add(new WProj { t = lt.t, v = (c - p0).normalized * 30, dmg = 16 * dmgK, r = 1.1f, life = -1.1f, lance = true }); // (they hang 1.1s first)
                        }
                        WarnDisc(V(c.x, Mathf.Max(0, c.y - 1.2f), c.z), 11, 1.4f, 0xffd080); Sfx("charge");
                        WState(e, "recover", 1.4f);
                    }
                    break;
                case "stunned": face = false; if (rand < 0.3f) SpawnParticles(BodyPoint(e) + V(0, 4, 0), 1, 2, goldSpark, 0.15f, 0.6f, -2); if (e.stateT <= 0) WState(e, "chase", 0.8f); break;
                case "recover": face = e.stateT < 0.4f; r.handSpin = Mathf.Lerp(r.handSpin, 1, 0.1f); if (e.stateT <= 0) WState(e, "chase", (e.phase2 ? 0.12f : 0.3f) + rand * 0.25f); break;
            }
            // he floats: drifting to keep his distance, bobbing
            var to = V(dx / dist, 0, dz / dist); float kk = 1 - Mathf.Exp(-3 * dt);
            e.vel.x += (to.x * move - e.vel.x) * kk; e.vel.z += (to.z * move - e.vel.z) * kk;
            e.pos += V(e.vel.x, 0, e.vel.z) * dt; e.pos.y = 0;
            float lim2 = LB - 8; e.pos.x = Mathf.Clamp(e.pos.x, -lim2, lim2); e.pos.z = Mathf.Clamp(e.pos.z, -lim2, lim2);
            if (face) TurnTo(e, Mathf.Atan2(dx, dz), 3, dt);
            AnimateWarden(e, dt, 0);
            SyncEnt(e);
        }
        float StopR(Ent e) => e.phase2 ? 40 : 32;
        // a clock tower a thrown player smashed into: it comes down, and leaves a crater where they hit (every screen)
        void WardenTowerBreak(HBuild b, Vector3 hp)
        {
            BreakBuilding(b, false);
            Flat(Geo.Circle(4, 24), Mats.Trans(0x0a0806, 0.8f), b.c.x + (hp.x - b.c.x) * 0.6f, 0.05f, b.c.z + (hp.z - b.c.z) * 0.6f);
            for (int k = 0; k < 10; k++) { float a = k * 0.63f; Ig(Deco(Geo.Box(1.4f, 0.3f, 1.0f), concDarkM ? concDarkM : M.block, hp.x + Mathf.Cos(a) * 3.6f, 0.3f, hp.z + Mathf.Sin(a) * 3.6f, -0.4f, -a, 0)); }
            SpawnParticles(hp, 60, 12, dustMat, 0.6f, 1.6f); Sfx("boom");
        }
        const float SNARE_R = 4.2f; Vector3 wSnareAt;
        // caught in stopped time: frozen; he steps through the stillness to stand at your back (then the punch)
        bool wCaught;
        // co-op: the local player is caught in stopped time (each machine freezes only its own player)
        void WardenFreezeMe() { wCaught = true; wFreezeT = 2.0f; wFreezePos = P.pos; Sfx("timeStop"); }
        void WardenCatch(Ent e)
        {
            if (!MP) WardenFreezeMe(); WState(e, "stopFreeze", 0.5f);
            SpawnParticles(BodyPoint(e), 40, 6, cyanMat, 0.25f, 0.8f, 0); Blast(BodyPoint(e), 4, 0x9af0ff);
            var punchDir = -V(Mathf.Sin(Tgt.yaw), 0, Mathf.Cos(Tgt.yaw)); // (the way they face - unless a clock tower stands near enough to throw them into)
            HBuild tgtT = null; float tb = 46; foreach (var b in wTowers) { if (b.broken) continue; float d = Hypot(b.c.x - Tgt.pos.x, b.c.z - Tgt.pos.z); if (d < tb && d > 9) { tb = d; tgtT = b; } }
            if (tgtT != null) { punchDir = V(tgtT.c.x - Tgt.pos.x, 0, tgtT.c.z - Tgt.pos.z).normalized; }
            var behind = Tgt.pos - punchDir * 7; behind.y = 0; float lim = LB - 6; behind.x = Mathf.Clamp(behind.x, -lim, lim); behind.z = Mathf.Clamp(behind.z, -lim, lim);
            e.pos = behind; e.yaw = Mathf.Atan2(Tgt.pos.x - e.pos.x, Tgt.pos.z - e.pos.z);
            SpawnParticles(BodyPoint(e), 40, 6, cyanMat, 0.25f, 0.8f, 0); Blast(BodyPoint(e), 4, 0x9af0ff); Sfx("blink");
        }
        // the closing dial round the snare: a ring of numerals shrinking in, its hands sweeping to midnight
        void SnareRing(Vector3 c, float dur)
        {
            var root = Mk.Piv(levelRoot, c.x, c.y + 0.08f, c.z); var m = Mats.Add(0x9af0ff, 0.8f, null, true, true);
            Mk.Msh(root, Geo.Torus(SNARE_R, 0.08f, 48, 4), m, 0, 0, 0, Mathf.PI / 2, 0, 0);
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; Mk.Msh(root, Geo.Box(0.12f, 0.03f, 0.6f), m, Mathf.Sin(a) * (SNARE_R - 0.4f), 0, Mathf.Cos(a) * (SNARE_R - 0.4f), 0, a, 0); }
            var hand = Mk.Piv(root); Mk.Msh(hand, Geo.Box(0.15f, 0.03f, SNARE_R - 0.6f), m, 0, 0.02f, (SNARE_R - 0.6f) / 2);
            var col = Mk.Msh(root, Geo.Cyl(SNARE_R, SNARE_R, 6, 32, true), Mats.Add(0x9af0ff, 0.0f, Tex.RayFade, true, true), 0, 3, 0); var colM = col.GetComponent<Renderer>().sharedMaterial;
            foreach (var rr in root.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
            float T = 0;
            var rgo = root.go; pgLater.Add((time + dur + 0.5f, () => { if (rgo) Destroy(rgo); }));
            anims.Add((t, dt) =>
            {
                if (!rgo) return; T += dt; float k = Mathf.Clamp01(T / dur);
                hand.ry = k * Mathf.PI * 2; root.ry -= dt * 0.8f;
                Mats.SetAlpha(m, k < 1 ? 0.4f + 0.5f * (Mathf.Sin(T * (10 + k * 30)) > 0 ? 1 : 0.5f) : Mathf.Max(0, 1 - (T - dur) * 3));
                Mats.SetAlpha(colM, k < 1 ? 0.15f * k : Mathf.Max(0, 0.6f - (T - dur) * 2)); // the column of frozen time slamming down at the end
            });
        }
        // the sweeping hour hand: a giant blade spun once round him at knee height
        void SpawnSweepHand(Ent e)
        {
            var hand = Mk.Piv(levelRoot, e.pos.x, 0.9f, e.pos.z); Mk.Msh(hand, Geo.Box(0.8f, 0.5f, 17), e.wd.goldM, 0, 0, 8.5f); Mk.Msh(hand, Geo.Cone(0.8f, 2.4f, 4), e.wd.goldM, 0, 0, 18, Mathf.PI / 2, 0, 0); Mk.Msh(hand, Geo.Box(0.82f, 0.05f, 17), e.wd.glowM, 0, 0.27f, 8.5f);
            foreach (var rr in hand.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
            float a0 = e.yaw, T = 0; bool hit = false; var c = e.pos; float dmg = 28 * (e.phase2 ? 1.2f : 1);
            anims.Add((t, dt) =>
            {
                if (!hand.t) return; T += dt; float k = Mathf.Clamp01(T / 1.0f), a = a0 + k * Mathf.PI * 2;
                hand.t.rotation = Quaternion.Euler(0, a * Mathf.Rad2Deg, 0);
                if (rand < 0.8f) SpawnParticles(c + V(Mathf.Sin(a), 0, Mathf.Cos(a)) * (4 + rand * 13) + V(0, 0.6f, 0), 1, 2, goldSpark, 0.15f, 0.4f);
                if (!hit && !P.dead && k < 1)
                {
                    var o = P.pos - c; o.y = 0; float d = o.magnitude, pa = Mathf.Atan2(o.x, o.z);
                    if (d < 18.5f && d > 1 && Mathf.Abs(Mathf.DeltaAngle(pa * Mathf.Rad2Deg, a * Mathf.Rad2Deg)) < 10 && P.pos.y < 1.5f) { hit = true; DamagePlayer(dmg, c); P.vel += o.normalized * 12 + V(0, 9, 0); P.onGround = false; Sfx("slam", 0.7f); }
                }
                if (k >= 1) { Destroy(hand.go); }
            });
        }
        void WardenSand(Vector3 p, float delay)
        {
            var wm = Mats.Trans(0xffd080, 0, true); var w = Mk.Msh(levelRoot, Geo.Circle(2.6f, 24), wm, p.x, p.y + 0.08f, p.z, -Mathf.PI / 2); w.layer = Mk.LayerIgnore;
            wSand.Add(new WSand { p = p, delay = delay, warn = w, wm = wm });
        }
        void UpdateWardenFx(Ent e, float dt)
        {
            // the frozen moment: you can't move, the world is drained of colour
            if (wFreezeT > 0)
            {
                wFreezeT -= dt; P.pos = wFreezePos; P.vel = Vector3.zero; mouseDown = false;
                if (!wFreezeOv) { wFreezeOv = Qd(hudRT, new Color(0, 0, 0, 0), "wfreeze"); Fill(wFreezeOv.rectTransform); wFreezeOv.raycastTarget = false; wFreezeOv.transform.SetAsFirstSibling(); }
            }
            if (wFreezeOv) wFreezeOv.Solid(new Color(0.55f, 0.7f, 0.85f, wFreezeT > 0 ? 0.28f : 0));
            // thrown: a clock tower you're flung into comes down (leaving a crater where you hit)
            if (wLaunchT > 0)
            {
                wLaunchT -= dt;
                if (!P.onGround && wLaunchT > 0.2f) { P.vel.x = wLaunchV.x; P.vel.z = wLaunchV.z; P.dashT = 0; if (rand < 0.6f) SpawnParticles(ChestPos, 1, 1, cyanMat, 0.12f, 0.4f, 0); } // thrown: no steering it
                else if (P.onGround && wLaunchT < 1.5f) wLaunchT = Mathf.Min(wLaunchT, 0.15f);
                foreach (var b in wTowers)
                    if (!b.broken && P.pos.x > b.mn.x - 1.2f && P.pos.x < b.mx.x + 1.2f && P.pos.z > b.mn.z - 1.2f && P.pos.z < b.mx.z + 1.2f && P.pos.y < b.mx.y)
                    {
                        var hp = P.pos; WardenTowerBreak(b, hp); NetFx(WFx.Tower, wTowers.IndexOf(b), hp); P.vel *= 0.35f; wLaunchT = 0; break;
                    }
            }
            // lances (they hang a moment, then fly)
            for (int i = wProj.Count - 1; i >= 0; i--)
            {
                var q = wProj[i]; if (!q.t) { wProj.RemoveAt(i); continue; }
                q.life += dt; if (q.life < 0) { q.t.position += Random.insideUnitSphere * 0.02f; continue; }
                if (q.life < dt * 1.5f) Sfx("dashGo", 0.4f);
                q.t.position += q.v * dt;
                if (!P.dead && Vector3.Distance(q.t.position, ChestPos) < q.r + 0.4f) { DamagePlayer(q.dmg, q.t.position); P.vel += q.v.normalized * 6; Destroy(q.t.gameObject); wProj.RemoveAt(i); continue; }
                if (q.life > 1.2f || PointInLevel(q.t.position)) { SpawnParticles(q.t.position, 6, 4, goldSpark, 0.12f, 0.4f); Destroy(q.t.gameObject); wProj.RemoveAt(i); }
            }
            // the pocket watches: fly, land, tick - then burst three times in a row
            for (int i = wClocks.Count - 1; i >= 0; i--)
            {
                var c = wClocks[i]; if (!c.t) { wClocks.RemoveAt(i); continue; }
                if (!c.landed)
                {
                    c.v.y -= GRAV * dt; c.t.position += c.v * dt; c.t.Rotate(V(400, 200, 0) * dt);
                    if (c.t.position.y <= 0.3f || PointInLevel(c.t.position)) { c.landed = true; var lp = c.t.position; lp.y = Mathf.Max(0.3f, FloorBelow(lp) + 0.3f); c.t.position = lp; c.t.rotation = Quaternion.Euler(90, rand * 360, 0); c.wm = Mats.Trans(0xffd080, 0, true); c.warn = Mk.Msh(levelRoot, Geo.Circle(4.6f, 24), c.wm, lp.x, lp.y - 0.2f, lp.z, -Mathf.PI / 2); c.warn.layer = Mk.LayerIgnore; Sfx("coin"); }
                    continue;
                }
                c.fuse -= dt;
                if (c.fuse > 0) { if (c.wm) Mats.SetAlpha(c.wm, 0.15f + 0.3f * (Mathf.Sin(c.fuse * 30) > 0 ? 1 : 0.4f)); if ((int)(c.fuse / 0.2f) != (int)((c.fuse + dt) / 0.2f)) Sfx("tick", 0.4f); continue; }
                c.nextBurst -= dt;
                if (c.nextBurst <= 0 && c.bursts > 0)
                {
                    int n = 3 - c.bursts; float rr = 2.6f + n * 1.0f; var p = c.t.position; c.bursts--; c.nextBurst = 0.2f;
                    Blast(p + V(0, 0.6f, 0), rr, n == 2 ? 0xffffff : 0xffb040); ShockRing(p, rr, 0xffd080, 0.3f); SpawnParticles(p, 14, 8, goldSpark, 0.12f, 0.6f); Sfx(n == 2 ? "boom" : "boomSmall", 0.7f);
                    if (!P.dead && Vector3.Distance(P.pos + V(0, 0.9f, 0), p) < rr + 0.5f) { DamagePlayer(12 * (e.phase2 ? 1.2f : 1), p); P.vel += (P.pos - p).normalized * 7 + V(0, 4, 0); }
                }
                if (c.bursts <= 0) { if (c.warn) Destroy(c.warn); Destroy(c.t.gameObject); wClocks.RemoveAt(i); }
            }
            // the sand: a marked circle, then a column of gold pouring down, then a drift that drags at you
            for (int i = wSand.Count - 1; i >= 0; i--)
            {
                var s = wSand[i]; s.t += dt;
                if (s.t < s.delay) { Mats.SetAlpha(s.wm, 0.12f + 0.35f * s.t / s.delay * (Mathf.Sin(s.t * 22) > 0 ? 1 : 0.6f)); if (rand < 0.4f) SpawnParticles(s.p + V((rand - 0.5f) * 2, 20, (rand - 0.5f) * 2), 1, 1, goldSpark, 0.1f, 1.0f, 12); continue; }
                if (!s.poured) // (a flag, not "!s.col": once the column fades and is destroyed, !s.col reads true again - it used to pour itself over and over, for ever)
                {
                    s.poured = true;
                    // the column: hung from a pivot 26 m up so it pours DOWN - a soft sheath of falling sand round a bright core
                    s.colP = Mk.Piv(levelRoot, s.p.x, s.p.y + 26, s.p.z).t; s.colP.localScale = V(1, 0.02f, 1);
                    s.cm = Mats.Trans(0xe8c070, 0.55f); s.col = Mk.Msh(s.colP, Geo.Cyl(1.9f, 2.4f, 26, 16, true), s.cm, 0, -13, 0); s.col.layer = Mk.LayerIgnore;
                    s.coreM = Mats.Add(0xfff0b0, 0.9f, null, true, true); var core = Mk.Msh(s.colP, Geo.Cyl(0.7f, 1.0f, 26, 10, true), s.coreM, 0, -13, 0); core.layer = Mk.LayerIgnore;
                    s.drift = Mk.Msh(levelRoot, Geo.Sphere(1, 14, 6, 0, Mathf.PI * 2, 0, Mathf.PI / 2), Mats.Std(0xd8b060, 0.95f), s.p.x, s.p.y, s.p.z); s.drift.layer = Mk.LayerIgnore; s.drift.transform.localScale = V(0.3f, 0.1f, 0.3f);
                    if (s.warn) s.warn.SetActive(false); s.life = 4; s.colT = 0;
                }
                s.colT += dt; s.life -= dt;
                if (s.colP)
                {
                    // pours down in 0.15 s, slams, holds a moment, then the stream thins to a trickle and fades
                    float pour = Mathf.Clamp01(s.colT / 0.15f), thin = Mathf.Clamp01((s.colT - 0.55f) / 0.6f), w = Mathf.Lerp(1, 0.12f, thin * thin);
                    s.colP.localScale = V(w, Mathf.Max(0.02f, pour), w); Mats.SetAlpha(s.cm, 0.55f * (1 - thin)); Mats.SetAlpha(s.coreM, 0.9f * (1 - thin) * (0.85f + 0.15f * Mathf.Sin(time * 40)));
                    if (pour >= 1 && !s.hit)
                    {
                        s.hit = true; Sfx("slam", 0.5f); Sfx("boomSmall", 0.4f); P.shake = Mathf.Max(P.shake, 0.3f);
                        SpawnParticles(s.p + V(0, 0.5f, 0), 30, 8, goldSpark, 0.15f, 0.8f); ShockRing(s.p + V(0, 0.15f, 0), 3.4f, 0xffd080, 0.35f);
                        if (!P.dead && Hypot(P.pos.x - s.p.x, P.pos.z - s.p.z) < 2.6f && Mathf.Abs(P.pos.y - s.p.y) < 3) DamagePlayer(20 * (e.phase2 ? 1.2f : 1), s.p);
                    }
                    if (pour >= 1 && thin < 1 && rand < 0.8f) SpawnParticles(s.p + V((rand - 0.5f) * 3 * w, 0.3f, (rand - 0.5f) * 3 * w), 1, 3, goldSpark, 0.12f, 0.5f); // the splash at its foot
                    if (thin >= 1) { Destroy(s.colP.gameObject); s.colP = null; }
                }
                if (s.drift) { float g = Mathf.Clamp01(s.colT / 0.8f), sink = Mathf.Clamp01((0.6f - s.life) / 0.6f); s.drift.transform.localScale = V(2.6f * Mathf.Max(0.1f, g), 0.6f * Mathf.Max(0.15f, g) * (1 - sink), 2.6f * Mathf.Max(0.1f, g)); }
                if (Hypot(P.pos.x - s.p.x, P.pos.z - s.p.z) < 2.8f && Mathf.Abs(P.pos.y - s.p.y) < 1.5f) hazSlowT = 0.2f; // the drift drags at you
                if (s.life <= 0) { Destroy(s.warn); if (s.colP) Destroy(s.colP.gameObject); Destroy(s.drift); wSand.RemoveAt(i); }
            }
        }

        // ================= animation =================
        void AnimateWarden(Ent e, float dt, float raise)
        {
            var r = e.wd; float k = 1 - Mathf.Exp(-6 * dt), T = r.t; var st = e.state;
            r.body.py = 1.6f + Mathf.Sin(time * 1.1f) * 0.35f;
            // the dial's hands: the second hand ticks, the others creep (spinning wild when he works time)
            float hs = r.handSpin;
            r.secH.rz = -(Mathf.Floor(time * Mathf.Abs(hs) * 1.0f) * (Mathf.PI / 30) * Mathf.Sign(hs + 0.001f)); r.minH.rz -= dt * 0.1f * hs; r.hourH.rz -= dt * 0.01f * hs;
            r.halo.rz += dt * 0.2f * (e.phase2 ? 3 : 1);
            for (int i = 0; i < r.orbit.Count; i++) { var o = r.orbit[i]; if (o.t) o.t.Rotate(0, (i % 2 == 0 ? 1 : -1) * dt * (30 + i * 6) * (e.phase2 ? 2.5f : 1), 0, i < 5 ? Space.Self : Space.Self); }
            float lean = 0, headX = 0, rShX = 0.25f, rShZ = 0.3f, rEl = -0.4f, lShX = 0.1f, lShZ = 0.35f, lEl = -0.3f, twist = 0;
            switch (st)
            {
                case "entrance": rShX = 0.15f; lShX = -2.6f * raise + 0.1f; lShZ = 0.35f + 0.4f * raise; lEl = -0.2f; headX = 0.1f; break;
                case "stopWind": lShX = -1.4f; lShZ = 0.9f; rShX = -1.4f; rShZ = 0.9f; lean = -0.15f; headX = -0.2f; break;
                case "stopSnare": lShX = -1.8f; lShZ = 0.5f; rShX = -0.6f; lean = 0.1f; headX = 0.1f; break;
                case "stopFreeze": lean = 0.2f; rShX = -0.6f; rEl = -1.6f; break;
                case "stopPunch": { float t = Mathf.Clamp01(T / 0.5f); rShX = Mathf.Lerp(0.6f, -1.7f, t * t); rEl = Mathf.Lerp(-1.8f, -0.1f, t * t); lean = Mathf.Lerp(-0.2f, 0.35f, t); twist = Mathf.Lerp(-0.5f, 0.3f, t); break; }
                case "rewindWind": lShX = -2.4f; rShX = -2.4f; lShZ = rShZ = 0.4f; headX = -0.4f; lean = -0.2f; break;
                case "clocksWind": { float t = Mathf.Clamp01(T / 0.8f); lShX = Mathf.Lerp(0.3f, -2.6f, t); lEl = -0.5f; twist = 0.3f; break; }
                case "sweepWind": rShX = -1.0f; rShZ = 1.4f; twist = -0.6f; break;
                case "sweep": { float t = Mathf.Clamp01(T / 1.0f); twist = Mathf.Lerp(-0.6f, 0.6f, t); rShZ = 1.2f; break; }
                case "sandWind": lShX = rShX = -2.8f; lShZ = rShZ = 0.6f; headX = -0.5f; break;
                case "pendWind": rShX = -2.6f; rEl = -0.3f; twist = -0.5f; lean = -0.1f; break;
                case "pend": { float t = Mathf.Clamp01(T / 0.3f); rShX = Mathf.Lerp(-2.6f, 0.6f, t); twist = Mathf.Lerp(-0.5f, 0.5f, t); lean = 0.3f * t; break; }
                case "transform": lean = -0.3f + Mathf.Sin(time * 20) * 0.05f; headX = -0.6f; lShX = rShX = -1.2f; lShZ = rShZ = 1.4f; break;
                case "midWind": case "midnight": lShX = rShX = -2.9f; lShZ = rShZ = 0.2f; headX = -0.5f; break;
                case "lanceWind": lShX = rShX = -1.6f; lShZ = rShZ = 1.2f; headX = 0.2f; break;
                case "stunned": lean = 0.6f; headX = 0.6f; rShX = lShX = 0.4f; rEl = lEl = -0.1f; break;
            }
            Sm(r.chest, 0, lean - e.flinch * 0.1f, k); Sm(r.chest, 1, twist, k); Sm(r.hood, 0, headX, k);
            Sm(r.rSh, 0, rShX, k); Sm(r.rSh, 2, -rShZ, k); Sm(r.rEl, 0, rEl, k); Sm(r.lSh, 0, lShX, k); Sm(r.lSh, 2, lShZ, k); Sm(r.lEl, 0, lEl, k);
            WardenPartsAnim(e, dt);
            if (r.xL != null && e.phase2) { Sm(r.xL, 0, -0.6f + Mathf.Sin(time * 3) * 0.3f, k); Sm(r.xR, 0, -0.6f - Mathf.Sin(time * 3) * 0.3f, k); }
            e.flinch = Mathf.Max(0, e.flinch - dt * 2);
            if (rand < 0.3f) SpawnParticles(r.body.t.TransformPoint(V((rand - 0.5f) * 6, rand * 2, (rand - 0.5f) * 6)), 1, 1, e.phase2 ? emberMat : cyanMat, 0.15f, 1.2f, -1.5f);
            int em = e.flash > 0 ? 0xffffff : st == "stunned" ? 0x2a2a08 : e.phase2 ? (Mathf.Sin(time * 6) > 0 ? 0x2a1406 : 0x140802) : 0x16161e; // (a little light of their own, so the robes read in the Hall's grey light)
            foreach (var m in e.mats) Mats.Emissive(m, em);
        }

        // everything of his still in the air - pouring sand columns and their drifts, clock bombs, lances - goes when he does
        // (UpdateWarden, which runs and clears them, stops with his death: left alone they'd hang in the hall for good)
        void WardenFxClear()
        {
            wFreezeT = 0;
            foreach (var c in wClocks) { if (c.warn) Destroy(c.warn); if (c.t) Destroy(c.t.gameObject); } wClocks.Clear();
            foreach (var q in wProj) if (q.t) Destroy(q.t.gameObject); wProj.Clear();
            foreach (var s in wSand) { if (s.warn) Destroy(s.warn); if (s.colP) Destroy(s.colP.gameObject); if (s.drift) Destroy(s.drift); } wSand.Clear();
        }

        // ================= the death: his hour comes =================
        void UpdateWardenDeath(BossDeath d, float dt)
        {
            var e = d.e; var r = e.wd; float T = d.t;
            if (T < dt * 1.5f) { Banner("TIME RUNS OUT", "THE WARDEN OF HOURS FALLS", "#ffd080"); WardenFxClear(); }
            UpdateTraitorFx(dt);
            if (e.g && e.g.activeSelf)
            {
                // his hands spin wild, then all stop at midnight; light cracks through the dial; he sags, gears flying off him
                r.handSpin = T < 2.2f ? 30 : 0; if (T >= 2.2f) { r.secH.rz = 0; r.minH.rz = 0; r.hourH.rz = 0; }
                r.body.py = 1.6f - Smoother(Mathf.Clamp01((T - 1.0f) / 2.0f)) * 1.4f; Sm(r.chest, 0, 0.5f, 0.05f); Sm(r.hood, 0, 0.7f, 0.05f);
                if (T < 2.8f && rand < 0.5f) { SpawnParticles(BodyPoint(e) + Random.insideUnitSphere * 5, 3, 6, goldSpark, 0.2f, 1); if (rand < 0.3f) Blast(BodyPoint(e) + Random.insideUnitSphere * 4, 1.5f + rand * 2, 0xffd080); }
                if ((int)(T / 0.6f) != (int)((T - dt) / 0.6f) && T < 2.2f) Sfx("tick", 0.8f);
                if (T >= 2.2f && T - dt < 2.2f) { Sfx("toll"); Sfx("toll", 0.6f); whiteFlash = 0.3f; }
                if (T >= 3.0f)
                {
                    // the dial shatters; he comes apart into sand and brass and ash
                    var c = BodyPoint(e);
                    foreach (var n in new[] { r.lSh, r.rSh, r.hood, r.chest }) if (n != null && n.t) ShatterGroup(n, 10);
                    e.g.SetActive(false);
                    SpawnParticles(c, 140, 16, goldSpark, 0.2f, 2.2f); SpawnParticles(c, 80, 10, dustMat, 0.6f, 2.6f); Blast(c, 14, 0xffd080);
                    for (int i = 0; i < 3; i++) ShockRing(V(d.basePos.x, 0.2f, d.basePos.z), 14 + i * 10, 0xffd080, 0.6f + i * 0.3f);
                    for (int k = 0; k < 10; k++) { var g = Deco(Geo.Sphere(1, 12, 6, 0, Mathf.PI * 2, 0, Mathf.PI / 2), Mats.Std(0xd8b060, 0.95f), d.basePos.x + (rand - 0.5f) * 8, 0, d.basePos.z + (rand - 0.5f) * 8); g.transform.localScale = V(2 + rand * 2, 0.6f + rand * 0.6f, 2 + rand * 2); Ig(g); } // the sand he leaves
                    Sfx("bossDeath"); Sfx("shatter"); Sfx("boom"); P.shake = 1.4f; freeze = 0.12f;
                    wGearSpeed = 0;
                }
                return;
            }
            if (T > 6.0f) { Destroy(e.g); bossDeath = null; }
        }
            // an attack's setup (warnings, aim, sounds) - run by the host when it picks it, and by each client when the
        // host's pick arrives, so everyone sees the same attack aimed at the same player
        void WardenBegin(Ent e, string a)
        {
            var r = e.wd; if (r == null) return; Tgt = TargetOf(e, 0); float sp = e.phase2 ? 1.6f : 1.3f; float dmgK = e.phase2 ? 1.2f : 1;
            float dx = Tgt.pos.x - e.pos.x, dz = Tgt.pos.z - e.pos.z, dist = Hypot(dx, dz), dy = Tgt.pos.y - e.pos.y; if (dist == 0) dist = 1;
switch (a)
            {
                case "stop": WState(e, "stopWind", 1.7f); WarnDisc(V(e.pos.x, 0, e.pos.z), StopR(e), 1.7f / sp, 0x9af0ff); Sfx("charge"); Sfx("creak", 0.7f); break;
                case "rewind": WState(e, "rewindWind", 1.5f); wRewindCd = e.phase2 ? 18 : 26; Sfx("rewind"); break;
                case "clocks": WState(e, "clocksWind", 0.8f); Sfx("tick"); break;
                case "sweep": WState(e, "sweepWind", 1.0f); WarnDisc(V(e.pos.x, 0, e.pos.z), 17, 1.0f / sp + 0.1f, 0xffd080); Sfx("growlLow"); break;
                case "sand": WState(e, "sandWind", 0.9f); Sfx("chant"); break;
                case "pendulum": WState(e, "pendWind", 0.85f); r.aimYaw = Mathf.Atan2(dx, dz); WarnFan(e.pos, r.aimYaw, 15, 1.1f, 0.85f / sp + 0.2f, 0xffd080); Sfx("growlLow"); break;
                case "midnight": WState(e, "midWind", 1.4f); Sfx("toll"); break;
                case "lances": WState(e, "lanceWind", 1.3f); Sfx("laserCharge"); break;
            }
        }
    }
}
