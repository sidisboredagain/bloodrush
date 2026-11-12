using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BR
{
    public class TraitorRig
    {
        public N root, hips, torso, neck, head, jaw, lSh, lEl, lHand, rSh, rEl, rHand; public N[] thigh = new N[2], knee = new N[2];
        public GameObject stump, coreGlow, eyeGlow, mouthGlow, collar; public Material glow, frost, skin, armour, horn, stumpM;
        public List<Material> mats = new List<Material>(); public float t; public float aimYaw; public Vector3 aim, chargeDir; public bool headless; public int combo;
    }

    // ---------- Boss 3: THE FROZEN TRAITOR (Throne of Frost, level 10) ----------
    // He sleeps in a block of ice before his throne. Come close and the ice cracks, he stirs, chunks fall away and he breaks free.
    // Phase 1: Frost Cleave, Glacial Slam, Frozen Prison, Frost Breath, Icefall, Demonic Charge.
    // At 0 HP he staggers, kneels and his frozen neck snaps: the head drops and rolls, the body collapses, a portal opens...
    // then the headless body rises, tears a block of ice out of the air and hurls it through the portal, which shatters.
    // Phase 2 (the same bar refills): faster and harder, frost jets from the neck, Frozen Cataclysm and Bloodfrost Rampage.
    // His final death: frost light escapes, he freezes solid, cracks and collapses; the portal's pieces rise, drink the
    // frost energy he releases and rebuild the portal - the way on.
    public partial class Game
    {
        const float TRAITOR_HP = 520;
        class TWarn { public GameObject g; public Material m; public float t, dur, a; public bool keep; }
        class TDrop { public Vector3 p; public float t, delay, r, dmg; public GameObject warn, chunk; public Material wm; public bool done; }
        class TPatch { public Vector3 c; public float r, life, max, tick; public GameObject g; public Material m; public bool armed; public float armT; }
        class TPiece { public Transform t; public Vector3 v, spin, home; public Quaternion homeRot, restRot; public Vector3 restPos; public bool rest; }
        readonly List<TWarn> tWarns = new List<TWarn>(); readonly List<TDrop> tDrops = new List<TDrop>(); readonly List<TPatch> tPatches = new List<TPatch>(); readonly List<TPiece> tPieces = new List<TPiece>();
        TWarn tPrisonWarn, tBreathWarn, tCleaveWarn; GameObject tBlockSolid;
        GameObject tBlock; readonly List<Transform> tCracks = new List<Transform>(); Material tCrackM, tIceM, tIceSolidM, tPieceM, tFrostBloodM;
        Transform tHead; Vector3 tHeadV, tHeadSpin; bool tHeadRest;
        Transform tThrow; Vector3 tThrowV, tThrowDir; int tThrowSt; float tThrowT;
        Vector3 tPortalHome; Quaternion tPortalRot = Quaternion.identity; bool tPortalBroken; float tCrystalY;
        float prisonT, prisonHp, prisonTick; GameObject prisonG;
        float slickT;

        void TraitorReset()
        {
            tWarns.Clear(); tDrops.Clear(); tPatches.Clear(); tPieces.Clear(); tCracks.Clear(); tPanels.Clear(); tBaseIce.Clear(); tBlockMist = null;
            tBlock = null; tBlockSolid = null; tPrisonWarn = tBreathWarn = null; tHead = null; tThrow = null; tThrowSt = 0; tPortalBroken = false; tHeadRest = false;
            prisonT = 0; prisonG = null; slickT = 0;
        }
        void TraitorMats()
        {
            if (tIceM) return;
            tIceM = G(Mats.Trans(0xa8dcff, 0.42f, true)); tIceSolidM = G(Mats.Std(0xc0e8ff, 0.1f, 0.15f, 0x0a2a40));
            tCrackM = G(Mats.Add(0xe8f8ff, 0, null, true, true)); tPieceM = G(Mats.Std(0x2a3a4a, 0.35f, 0.6f, 0x60c8ff));
            tFrostBloodM = G(Mats.Basic(0x3a90ff));
        }

        // ================= the model =================
        TraitorRig BuildTraitor(Ent e)
        {
            var skin = EM(e, Mats.Std(0x3a4a62, 0.55f));
            var armour = EM(e, Mats.Std(0xb8e6ff, 0.1f, 0.15f, 0x0c3654));
            var horn = EM(e, Mats.Std(0x101216, 0.32f, 0.35f));
            var bone = EM(e, Mats.Std(0xc8d8e0, 0.4f));
            var glow = EM(e, Mats.Basic(0x80f0ff));
            var frost = EM(e, Mats.Add(0x70e0ff, 0.85f, null, true, true));
            var iron = EM(e, Mats.Std(0x2a3038, 0.45f, 0.7f));
            var r = new TraitorRig { glow = glow, frost = frost, skin = skin, armour = armour, horn = horn };
            r.root = Mk.Piv(e.g.transform);
            r.hips = Mk.Piv(r.root, 0, 1.35f, 0);
            Mk.Msh(r.hips, Geo.Box(0.95f, 0.36f, 0.6f), skin);
            for (int i = 0; i < 6; i++) { float a = (i / 5f - 0.5f) * 2.6f; Mk.Msh(r.hips, Geo.Box(0.3f, 0.5f, 0.08f), armour, Mathf.Sin(a) * 0.48f, -0.28f, Mathf.Cos(a) * 0.32f, 0.15f, a, 0); } // a skirt of ice plates
            for (int i = 0; i < 5; i++) Mk.Msh(r.hips, Geo.Torus(0.09f, 0.025f, 8, 4), iron, -0.36f + i * 0.18f, 0.12f, 0.31f, 0, i % 2 * 1.57f, 0); // a frozen chain still round his waist
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var thigh = Mk.Piv(r.hips, 0.34f * sx, -0.1f, 0);
                Mk.Msh(thigh, Geo.Box(0.38f, 0.66f, 0.42f), skin, 0, -0.33f, 0);
                Mk.Msh(thigh, Geo.Box(0.4f, 0.3f, 0.12f), armour, 0, -0.2f, 0.2f);
                var knee = Mk.Piv(thigh, 0, -0.66f, 0);
                Mk.Msh(knee, Geo.Box(0.32f, 0.62f, 0.34f), skin, 0, -0.31f, 0);
                Mk.Msh(knee, Geo.Box(0.36f, 0.46f, 0.1f), armour, 0, -0.26f, 0.17f);
                Mk.Msh(knee, Geo.Cone(0.08f, 0.32f, 5), armour, 0, 0.02f, 0.2f, 1.1f, 0, 0); // ice spur on the knee
                Mk.Msh(knee, Geo.Box(0.44f, 0.16f, 0.66f), horn, 0, -0.66f, 0.08f);
                for (int k = -1; k <= 1; k++) Mk.Msh(knee, Geo.Cone(0.05f, 0.24f, 4), bone, k * 0.13f, -0.68f, 0.44f, Mathf.PI / 2, 0, 0);
                r.thigh[i] = thigh; r.knee[i] = knee;
            }
            r.torso = Mk.Piv(r.hips, 0, 0.14f, 0);
            Mk.Msh(r.torso, Geo.Box(1.15f, 0.86f, 0.66f), skin, 0, 0.45f, 0);
            Mk.Msh(r.torso, Geo.Box(1.5f, 0.5f, 0.76f), skin, 0, 0.98f, 0);
            Mk.Msh(r.torso, Geo.Box(1.22f, 0.62f, 0.12f), armour, 0, 0.82f, 0.38f, -0.1f, 0, 0); // frozen breastplate
            for (int i = 0; i < 3; i++) Mk.Msh(r.torso, Geo.Box(0.8f - i * 0.1f, 0.12f, 0.1f), armour, 0, 0.42f - i * 0.15f, 0.34f);
            Mk.Msh(r.torso, Geo.Sphere(0.13f, 10, 8), glow, 0, 0.62f, 0.42f);
            r.coreGlow = Gl(r.torso, 0x80f0ff, 1.2f, 0.85f); r.coreGlow.transform.localPosition = V(0, 0.62f, 0.5f);
            for (int i = 0; i < 7; i++) Mk.Msh(r.torso, Geo.Cone(0.08f + (i % 3) * 0.03f, 0.55f + (i % 2) * 0.3f, 5), armour, (i - 3) * 0.17f, 0.55f + Mathf.Abs(i - 3) * -0.06f + 0.5f, -0.38f, -2.3f, 0, (i - 3) * 0.15f); // ice shards growing out of his back
            for (int i = 0; i < 9; i++) { float t = i / 8f; Mk.Msh(r.torso, Geo.Torus(0.07f, 0.02f, 8, 4), iron, lerp(-0.6f, 0.55f, t), lerp(1.15f, 0.2f, t), 0.39f, 0, i % 2 * 1.57f, 0); } // a chain across the chest
            // neck, and the stump that phase 2 reveals (built now, hidden)
            r.neck = Mk.Piv(r.torso, 0, 1.22f, 0.1f);
            Mk.Msh(r.neck, Geo.Box(0.44f, 0.3f, 0.44f), skin, 0, 0.06f, 0);
            r.collar = new GameObject("collar"); r.collar.transform.SetParent(r.neck.t, false);
            Mk.Msh(r.collar.transform, Geo.Torus(0.32f, 0.07f, 20, 6), frost, 0, 0.12f, 0, Mathf.PI / 2, 0, 0); r.collar.SetActive(false);
            r.stump = new GameObject("stump"); r.stump.transform.SetParent(r.neck.t, false); var st = r.stump.transform;
            r.stumpM = frost;
            for (int k = 0; k < 9; k++) { float a = k / 9f * Mathf.PI * 2; Mk.Msh(st, Geo.Cone(0.07f, 0.28f + (k % 3) * 0.14f, 5), armour, Mathf.Cos(a) * 0.22f, 0.32f, Mathf.Sin(a) * 0.22f, Mathf.Sin(a) * 0.35f, 0, -Mathf.Cos(a) * 0.35f); } // a ragged collar of ice
            Mk.Msh(st, Geo.Cyl(0.2f, 0.24f, 0.12f, 12), Mats.Std(0x3a0a1a, 0.6f), 0, 0.24f, 0); // the wound
            for (int k = 0; k < 3; k++) Mk.Msh(st, Geo.Cone(0.05f, 0.22f, 4), bone, 0, 0.32f + k * 0.12f, -0.04f); // exposed vertebrae
            r.stump.SetActive(false);
            // the head: horned, brow-ridged, eyes burning cold, an icicle beard
            r.head = Mk.Piv(r.neck, 0, 0.22f, 0.12f);
            var hs = new List<GameObject>();
            hs.Add(Mk.Msh(r.head, Geo.Box(0.52f, 0.44f, 0.58f), skin, 0, 0.12f, 0));
            hs.Add(Mk.Msh(r.head, Geo.Box(0.62f, 0.13f, 0.22f), horn, 0, 0.26f, 0.24f, -0.25f, 0, 0));
            hs.Add(Mk.Msh(r.head, Geo.Box(0.38f, 0.22f, 0.32f), skin, 0, 0.0f, 0.36f));
            foreach (var sx in new[] { -1f, 1f })
            {
                hs.Add(Mk.Msh(r.head, Geo.Box(0.12f, 0.06f, 0.04f), glow, sx * 0.14f, 0.16f, 0.37f, 0, 0, sx * 0.25f));
                // the great horns: out, back, then sweeping forward and up
                var hp = Mk.Piv(r.head, sx * 0.26f, 0.3f, 0.02f); hp.SetRot(-0.3f, 0, -sx * 1.1f);
                for (int k = 0; k < 8; k++)
                {
                    float r0 = 0.12f * (1 - k / 8f) + 0.015f, r1 = 0.12f * (1 - (k + 1) / 8f) + 0.01f;
                    hs.Add(Mk.Msh(hp, Geo.Cyl(r1, r0, 0.26f, 8), k > 5 ? armour : horn, 0, 0.13f, 0));
                    hp = Mk.Piv(hp, 0, 0.26f, 0); hp.SetRot(k < 3 ? 0.15f : -0.42f, 0, sx * (k < 3 ? 0.45f : 0.1f));
                }
                hs.Add(Mk.Msh(r.head, Geo.Cone(0.04f, 0.22f, 4), horn, sx * 0.18f, 0.36f, 0.2f, -0.2f, 0, -sx * 0.3f));
            }
            for (int k = 0; k < 5; k++) hs.Add(Mk.Msh(r.head, Geo.Cone(0.035f, 0.2f + (k % 2) * 0.12f, 5), armour, (k - 2) * 0.1f, 0.4f, -0.1f, -0.25f, 0, (k - 2) * 0.2f)); // a frost crown
            r.jaw = Mk.Piv(r.head, 0, -0.1f, 0.12f);
            hs.Add(Mk.Msh(r.jaw, Geo.Box(0.4f, 0.1f, 0.4f), skin, 0, -0.05f, 0.14f));
            foreach (var fx in new[] { -0.14f, -0.05f, 0.05f, 0.14f }) hs.Add(Mk.Msh(r.jaw, Geo.Cone(0.025f, 0.12f, 4), bone, fx, 0.03f, 0.32f));
            for (int k = 0; k < 6; k++) hs.Add(Mk.Msh(r.jaw, Geo.Cone(0.03f, 0.18f + (k % 3) * 0.08f, 5), armour, (k - 2.5f) * 0.07f, -0.16f, 0.24f - (k % 2) * 0.06f, Mathf.PI, 0, 0)); // the icicle beard
            Mk.Head(hs.ToArray());
            r.eyeGlow = Gl(r.head, 0x80f0ff, 0.9f, 0.85f); r.eyeGlow.transform.localPosition = V(0, 0.16f, 0.42f);
            r.mouthGlow = Gl(r.jaw, 0xc0f8ff, 0.6f, 0); r.mouthGlow.transform.localPosition = V(0, 0.02f, 0.36f);
            // arms: frozen pauldrons, massive forearms in ice bracers, long claws
            N MkArm(int sx, out N elbow, out N hand)
            {
                var sh = Mk.Piv(r.torso, 0.88f * sx, 1.02f, 0);
                Mk.Msh(sh, Geo.Box(0.62f, 0.36f, 0.62f), armour, 0.05f * sx, 0.08f, 0);
                for (int k = 0; k < 3; k++) Mk.Msh(sh, Geo.Cone(0.07f, 0.4f + k % 2 * 0.15f, 5), armour, 0.05f * sx + (k - 1) * 0.16f, 0.42f, 0, 0, 0, -sx * 0.35f);
                Mk.Msh(sh, Geo.Box(0.38f, 0.74f, 0.38f), skin, 0, -0.38f, 0);
                elbow = Mk.Piv(sh, 0, -0.76f, 0);
                Mk.Msh(elbow, Geo.Box(0.36f, 0.7f, 0.36f), skin, 0, -0.35f, 0);
                Mk.Msh(elbow, Geo.Box(0.42f, 0.42f, 0.42f), armour, 0, -0.32f, 0);
                Mk.Msh(elbow, Geo.Cone(0.06f, 0.3f, 5), armour, sx * 0.2f, -0.2f, 0, 0, 0, -sx * 1.2f);
                hand = Mk.Piv(elbow, 0, -0.72f, 0);
                Mk.Msh(hand, Geo.Box(0.34f, 0.22f, 0.32f), skin, 0, -0.08f, 0);
                for (int k = 0; k < 4; k++) Mk.Msh(hand, Geo.Cone(0.045f, 0.5f, 4), bone, -0.12f + k * 0.08f, -0.42f, 0.08f, Mathf.PI - 0.25f, 0, 0);
                Mk.Msh(hand, Geo.Cone(0.045f, 0.34f, 4), bone, sx * 0.17f, -0.25f, 0.12f, Mathf.PI - 0.5f, 0, sx * 0.4f);
                return sh;
            }
            r.rSh = MkArm(1, out r.rEl, out r.rHand); r.lSh = MkArm(-1, out r.lEl, out r.lHand);
            r.mats.Add(skin); r.mats.Add(armour); r.mats.Add(horn);
            e.mats.Clear(); e.mats.AddRange(r.mats);
            return r;
        }

        // ================= spawning: asleep in the ice =================
        Ent SpawnTraitor()
        {
            TraitorMats();
            var e = NewEnt("boss"); var sp = LEVEL.bossSpawn;
            e.tr = BuildTraitor(e);
            e.g.transform.localScale = Vector3.one * 3.2f;
            var near = NearestPlayer(V(sp.x, 0, sp.y)); e.pos = V(sp.x, 0, sp.y); e.yaw = Mathf.Atan2(near.pos.x - sp.x, near.pos.z - sp.y);
            e.boss = 3; e.name = "FROZEN TRAITOR, THE BETRAYER BOUND IN ICE"; e.heavy = true; e.s = 3.2f;
            e.hp = e.maxHp = TRAITOR_HP * CoopBossHp; e.headY = 8.6f; e.bodyY = 5.6f; e.hitR = 3.4f; e.r = 1.8f; e.h = 8;
            e.invuln = true; TState(e, "frozen", 999);
            FinishEnt(e); SyncEnt(e); boss = e;
            // the prison: a vast block of ice - four walls and a lid, each its own slab so they can crack, burst and topple
            var c = V(e.pos.x, 0, e.pos.z);
            tBlock = new GameObject("traitorIce"); tBlock.transform.SetParent(levelRoot, false); tBlock.transform.localPosition = c;
            var bt = tBlock.transform; tPanels.Clear();
            const float BW = 11, BH = 13.5f, BD = 9.5f, TH = 0.9f;
            var rng = new System.Random(31); float R() => (float)rng.NextDouble();
            void Panel(Vector3 pivot, Vector3 meshOff, Vector3 size, Vector3 axis, float faceZ, bool sideways)
            {
                var pv = new GameObject("icePanel").transform; pv.SetParent(bt, false); pv.localPosition = pivot;
                Mk.Msh(pv, Geo.Box(size.x, size.y, size.z), tIceM, meshOff.x, meshOff.y, meshOff.z).layer = Mk.LayerIgnore;
                // frosted, thicker rims where the slab meets its neighbours
                Mk.Msh(pv, Geo.Box(sideways ? size.x * 1.05f : 0.6f, size.y, sideways ? 0.6f : size.z * 1.05f), tIceSolidM, meshOff.x + (sideways ? 0 : -size.x / 2 + 0.3f), meshOff.y, meshOff.z + (sideways ? -size.z / 2 + 0.3f : 0)).layer = Mk.LayerIgnore;
                // the cracks, waiting in the ice (they grow while he wakes)
                for (int k = 0; k < 6; k++)
                {
                    float u = (R() - 0.5f) * (sideways ? size.z : size.x) * 0.8f, h = 1.5f + R() * (size.y - 3);
                    var cr = Mk.Msh(pv, Geo.Box(0.1f, 2.4f + R() * 3.6f, 0.1f), tCrackM, meshOff.x + (sideways ? faceZ : u), h, meshOff.z + (sideways ? u : faceZ), 0, 0, (R() - 0.5f) * 2.4f);
                    if (sideways) cr.transform.localRotation = Quaternion.Euler((R() - 0.5f) * 140, 0, 0);
                    cr.layer = Mk.LayerIgnore; cr.transform.localScale = V(1, 0.01f, 1); tCracks.Add(cr.transform);
                }
                tPanels.Add(new TPanel { t = pv, axis = axis, size = size, off = meshOff });
            }
            Panel(V(0, 0, BD / 2), V(0, BH / 2, -TH / 2), V(BW, BH, TH), V(1, 0, 0), 0.47f, false);        // front (toward the hall)
            Panel(V(0, 0, -BD / 2), V(0, BH / 2, TH / 2), V(BW, BH, TH), V(-1, 0, 0), -0.47f, false);      // back
            Panel(V(-BW / 2, 0, 0), V(TH / 2, BH / 2, 0), V(TH, BH, BD - TH * 2), V(0, 0, 1), -0.47f, true); // left
            Panel(V(BW / 2, 0, 0), V(-TH / 2, BH / 2, 0), V(TH, BH, BD - TH * 2), V(0, 0, -1), 0.47f, true); // right
            { var lid = new GameObject("iceLid").transform; lid.SetParent(bt, false); lid.localPosition = V(0, BH - TH / 2, 0); Mk.Msh(lid, Geo.Box(BW, TH, BD), tIceM).layer = Mk.LayerIgnore; for (int k = 0; k < 7; k++) Mk.Msh(lid, Geo.Cone(0.6f + R() * 0.5f, 1.6f + R() * 2.4f, 6), tIceSolidM, (R() - 0.5f) * (BW - 2), TH / 2 + 0.8f, (R() - 0.5f) * (BD - 2), (R() - 0.5f) * 0.4f, 0, (R() - 0.5f) * 0.4f).layer = Mk.LayerIgnore; tPanels.Add(new TPanel { t = lid, lid = true, size = V(BW, TH, BD) }); }
            // the frozen mist inside, and the crust of ice heaped round the base
            var mist = Mk.Msh(bt, Geo.Box(BW - 2, BH - 2, BD - 2), Mats.Trans(0xd8f2ff, 0.16f), 0, BH / 2, 0); mist.layer = Mk.LayerIgnore; tBlockMist = mist;
            for (int k = 0; k < 16; k++) { float a = k / 16f * Mathf.PI * 2; var cn = Mk.Msh(bt, Geo.Cone(0.8f + R() * 0.7f, 2.2f + R() * 3.4f, 6), tIceSolidM, Mathf.Cos(a) * (BW / 2 + 0.6f), 0.9f + R(), Mathf.Sin(a) * (BD / 2 + 0.6f), Mathf.Sin(a) * 0.45f, R(), -Mathf.Cos(a) * 0.45f); cn.layer = Mk.LayerIgnore; tBaseIce.Add(cn.transform); }
            var gl = Mk.Glow(bt, 0x60c8ff, 26, 0.3f); gl.transform.localPosition = V(0, 7, 0);
            tBlockSolid = SolidAABB(V(c.x - BW / 2, 0, c.z - BD / 2), V(c.x + BW / 2, BH, c.z + BD / 2)); tBlockC = c;
            foreach (var m in e.tr.mats) Mats.Emissive(m, 0x000000);
            Mats.SetAlpha(e.tr.frost, 0.85f); Mats.SetColor(e.tr.glow, 0x1a3a4a); // asleep: the eyes are dark
            Banner($"WAVE {wave}", "", THEME.banner);
            return e;
        }
        // one slab of the Traitor's ice prison: it topples outward about its bottom edge (the lid is thrown off) and shatters on the floor
        class TPanel { public Transform t; public Vector3 axis, size, off; public bool lid, falling, done; public float t0, ang, delay; public Vector3 lv, lspin; }
        Vector3 tBlockC; bool tMatLogged; readonly List<TPanel> tPanels = new List<TPanel>(); readonly List<Transform> tBaseIce = new List<Transform>(); GameObject tBlockMist;
        // a burst of faceted ice shards
        void IceShards(Vector3 p, int n, float spread, float speed, Vector3 push, float size = 1)
        {
            for (int k = 0; k < n; k++)
            {
                var q = p + V((rand - 0.5f) * spread, (rand - 0.5f) * spread, (rand - 0.5f) * spread);
                var ch = Mk.Msh(world, rand < 0.5f ? Geo.Octahedron(1) : Geo.Icosahedron(1), rand < 0.55f ? tIceSolidM : tIceM, q.x, q.y, q.z, rand * 3, rand * 3, rand * 3); ch.layer = Mk.LayerIgnore;
                ch.transform.localScale = V(0.35f + rand * 0.6f, 0.5f + rand * 1.1f, 0.35f + rand * 0.6f) * size;
                var o = q - p; o.y = Mathf.Abs(o.y) * 0.5f;
                particles.Add(new Pt { m = ch.transform, v = o.normalized * speed * (0.5f + rand) + push + V(0, 2 + rand * 4, 0), life = 3 + rand * 2, grav = GRAV, hasSpin = true, spin = V(rand * 10 - 5, rand * 10 - 5, rand * 10 - 5), owned = true });
            }
        }
        // the prison breaks: the walls start to fall outward, the lid is hurled up, the base crust bursts
        void BreakIceBlock()
        {
            if (!tBlock) return;
            var c = tBlock.transform.position;
            colliders.RemoveAll(cb => Mathf.Abs(cb.min.x - (tBlockC.x - 5.5f)) < 0.01f && Mathf.Abs(cb.min.z - (tBlockC.z - 4.75f)) < 0.01f && Mathf.Abs(cb.max.y - 13.5f) < 0.01f); if (tBlockSolid) Destroy(tBlockSolid); tBlockSolid = null; // (by where it was built: the block itself is shaking)
            for (int i = 0; i < tPanels.Count; i++)
            {
                var pn = tPanels[i]; pn.falling = true; pn.t0 = time; pn.delay = pn.lid ? 0 : 0.04f + i * 0.05f;
                if (pn.lid) { pn.lv = V((rand - 0.5f) * 6, 17, -4); pn.lspin = V(140 + rand * 80, rand * 40, 60 + rand * 60); }
            }
            foreach (var cn in tBaseIce) if (cn) { IceShards(cn.position, 3, 1.5f, 8, Vector3.zero, 0.8f); Destroy(cn.gameObject); }
            tBaseIce.Clear(); foreach (var cr in tCracks) if (cr) cr.localScale = V(1, 1, 1); tCracks.Clear();
            if (tBlockMist) { var mp = tBlockMist.transform.position; Destroy(tBlockMist); tBlockMist = null; SpawnParticles(mp, 70, 9, SnowMat, 0.35f, 2.2f, 1); }
            SpawnParticles(c + V(0, 8, 0), 80, 16, cyanMat, 0.16f, 1.6f);
            Blast(c + V(0, 7, 0), 15, 0xc8f0ff); ShockRing(c, 24, 0x80e0ff, 0.7f); FirePillar(c, 5, 30, 0xc8f0ff, 0.9f);
            Sfx("shatter"); Sfx("boom"); Sfx("roar"); P.shake = 1.4f; whiteFlash = Mathf.Max(whiteFlash, 0.35f);
            if (!P.dead && Hypot(P.pos.x - c.x, P.pos.z - c.z) < 11) { var push = V(P.pos.x - c.x, 0, P.pos.z - c.z).normalized; P.vel += push * 16 + V(0, 7, 0); P.onGround = false; DamagePlayer(12, c); }
        }
        void UpdateIcePanels(float dt)
        {
            bool any = false;
            foreach (var pn in tPanels)
            {
                if (pn.done || !pn.t) continue; any = true;
                if (!pn.falling) continue;
                float a = time - pn.t0 - pn.delay; if (a < 0) continue;
                if (pn.lid)
                {
                    pn.lv.y -= GRAV * dt; pn.t.position += pn.lv * dt; pn.t.Rotate(pn.lspin * dt, Space.World);
                    if (pn.t.position.y < 1 && pn.lv.y < 0) { pn.done = true; IceShards(pn.t.position, 18, 5, 9, Vector3.zero, 1.3f); SpawnParticles(pn.t.position, 30, 8, SnowMat, 0.3f, 1.4f); ShockRing(V(pn.t.position.x, 0, pn.t.position.z), 9, 0xc8f0ff, 0.4f); Sfx("shatter", 0.8f); P.shake = Mathf.Max(P.shake, 0.5f); Destroy(pn.t.gameObject); }
                    continue;
                }
                // topple: slow to start, then accelerating like a falling slab
                pn.ang = Mathf.Min(90, 0.5f * 640 * a * a + 12 * a);
                pn.t.localRotation = Quaternion.AngleAxis(pn.ang, pn.axis);
                if (pn.ang >= 88)
                {
                    pn.done = true;
                    // it slams flat and bursts into shards along its length
                    var mid = pn.t.TransformPoint(pn.off); var outward = pn.t.TransformDirection(V(0, 1, 0)); outward.y = 0; outward = outward.sqrMagnitude > 0.01f ? outward.normalized : Vector3.forward;
                    for (int k = 0; k < 5; k++) { var along = Vector3.Lerp(pn.t.TransformPoint(pn.off - V(0, pn.size.y * 0.45f, 0)), pn.t.TransformPoint(pn.off + V(0, pn.size.y * 0.45f, 0)), k / 4f); IceShards(along + V(0, 0.6f, 0), 5, 3, 6, outward * 4, 1.4f); }
                    SpawnParticles(mid + V(0, 0.5f, 0), 45, 10, SnowMat, 0.32f, 1.6f); SpawnParticles(mid, 30, 10, cyanMat, 0.14f, 1);
                    ShockRing(V(mid.x, 0, mid.z), 10, 0xc8f0ff, 0.45f); Sfx("shatter"); Sfx("slam", 0.7f); P.shake = Mathf.Max(P.shake, 0.8f);
                    if (!P.dead && Mathf.Abs(Vector3.Dot(V(P.pos.x - mid.x, 0, P.pos.z - mid.z), outward)) < 2 && Vector3.Distance(V(P.pos.x, 0, P.pos.z), V(mid.x, 0, mid.z)) < 8 && P.pos.y < 2) DamagePlayer(18, mid);
                    Destroy(pn.t.gameObject);
                }
            }
            if (!any && tPanels.Count > 0) { tPanels.Clear(); if (tBlock) { Destroy(tBlock); tBlock = null; } }
        }
        static void TState(Ent e, string s, float dur) { e.state = s; e.stateT = dur; e.vidx = 0; e.hitPlayer = false; if (e.tr != null) e.tr.t = 0; }

        // ================= warnings and attack effects =================
        static readonly Dictionary<int, Mesh> fanCache = new Dictionary<int, Mesh>();
        static Mesh FanMesh(float r, float half)
        {
            int key = (int)(r * 100) * 1000 + (int)(half * 100);
            if (fanCache.TryGetValue(key, out var m) && m) return m;
            int seg = 28; var v = new List<Vector3> { Vector3.zero }; var n = new List<Vector3> { Vector3.up }; var tri = new List<int>();
            for (int i = 0; i <= seg; i++) { float a = -half + 2 * half * i / seg; v.Add(new Vector3(Mathf.Sin(a) * r, 0, Mathf.Cos(a) * r)); n.Add(Vector3.up); }
            for (int i = 1; i <= seg; i++) { tri.Add(0); tri.Add(i); tri.Add(i + 1); tri.Add(0); tri.Add(i + 1); tri.Add(i); }
            m = new Mesh(); m.SetVertices(v); m.SetNormals(n); m.SetTriangles(tri, 0); m.RecalculateBounds();
            return fanCache[key] = m;
        }
        TWarn AddWarn(GameObject g, Material m, float dur, float a) { g.layer = Mk.LayerIgnore; var w = new TWarn { g = g, m = m, dur = dur, a = a }; Mats.SetAlpha(m, 0); tWarns.Add(w); return w; }
        TWarn WarnFan(Vector3 p, float yaw, float r, float half, float dur, int col = 0x80e0ff)
        { var m = Mats.Trans(col, 0, true); var g = Mk.Msh(levelRoot, FanMesh(r, half), m, p.x, p.y + 0.09f, p.z, 0, yaw, 0); return AddWarn(g, m, dur, 0.42f); }
        TWarn WarnDisc(Vector3 p, float r, float dur, int col = 0x80e0ff)
        {
            var m = Mats.Trans(col, 0, true); var g = Mk.Msh(levelRoot, Geo.Circle(r, 40), m, p.x, p.y + 0.09f, p.z, -Mathf.PI / 2);
            var rim = Mk.Msh(g.transform, Geo.Ring(0.94f, 1, 48), m, 0, 0, 0.01f); rim.transform.localScale = Vector3.one * r; rim.layer = Mk.LayerIgnore;
            return AddWarn(g, m, dur, 0.4f);
        }
        TWarn WarnRect(Vector3 from, Vector3 dir, float len, float w, float dur, int col = 0x80e0ff)
        { var c = from + dir * (len / 2); var m = Mats.Trans(col, 0, true); var g = Mk.Msh(levelRoot, Geo.Plane(w, len), m, c.x, from.y + 0.1f, c.z, -Mathf.PI / 2, Mathf.Atan2(dir.x, dir.z), 0); return AddWarn(g, m, dur, 0.38f); }
        void ClearTraitorAttacks()
        {
            foreach (var w in tWarns) if (w.g) Destroy(w.g); tWarns.Clear();
            foreach (var d in tDrops) { if (d.warn) Destroy(d.warn); if (d.chunk) Destroy(d.chunk); } tDrops.Clear();
            foreach (var p in tPatches) if (p.g) Destroy(p.g); tPatches.Clear();
            ClearHazards();
        }
        void IceDrop(Vector3 p, float delay, float r = 2.6f, float dmg = 22)
        {
            var wm = Mats.Trans(0x80e0ff, 0, true); var w = Mk.Msh(levelRoot, Geo.Circle(r, 32), wm, p.x, p.y + 0.1f, p.z, -Mathf.PI / 2); w.layer = Mk.LayerIgnore;
            var rim = Mk.Msh(w.transform, Geo.Ring(0.92f, 1, 40), wm, 0, 0, 0.01f); rim.transform.localScale = Vector3.one * r; rim.layer = Mk.LayerIgnore;
            tDrops.Add(new TDrop { p = p, delay = delay, r = r, dmg = dmg, warn = w, wm = wm });
        }
        void FrostPatch(Vector3 c, float r, float life, float armDelay = 0)
        {
            var m = Mats.Add(0x9ae8ff, 0, null, true, true); var g = Mk.Msh(levelRoot, Geo.Circle(r, 28), m, c.x, c.y + 0.07f, c.z, -Mathf.PI / 2); g.layer = Mk.LayerIgnore;
            tPatches.Add(new TPatch { c = c, r = r, life = life + armDelay, max = life + armDelay, g = g, m = m, armT = armDelay });
        }

        // world-side effects of the fight (they keep running through the transition and the final death)
        void UpdateTraitorFx(float dt)
        {
            UpdateIcePanels(dt);
            for (int i = tWarns.Count - 1; i >= 0; i--)
            {
                var w = tWarns[i]; w.t += dt;
                if (!w.g) { tWarns.RemoveAt(i); continue; }
                float k = Mathf.Min(1, w.t / 0.25f) * (0.75f + 0.25f * Mathf.Sin(w.t * 22)) * (w.t > w.dur - 0.15f ? Mathf.Max(0, (w.dur - w.t) / 0.15f) : 1);
                Mats.SetAlpha(w.m, w.a * k);
                if (w.t >= w.dur && !w.keep) { Destroy(w.g); tWarns.RemoveAt(i); }
            }
            for (int i = tDrops.Count - 1; i >= 0; i--)
            {
                var d = tDrops[i]; d.t += dt;
                if (d.t < d.delay) { Mats.SetAlpha(d.wm, 0.15f + 0.4f * d.t / d.delay * (Mathf.Sin(d.t * 26) > 0 ? 1 : 0.7f)); if (d.t > d.delay - 0.4f && !d.chunk) { d.chunk = Mk.Msh(levelRoot, Geo.Icosahedron(1), tIceSolidM, d.p.x, 40, d.p.z); d.chunk.layer = Mk.LayerIgnore; d.chunk.transform.localScale = Vector3.one * d.r * 0.7f; } }
                if (d.chunk && !d.done) { float k = clamp((d.t - (d.delay - 0.4f)) / 0.4f, 0, 1); d.chunk.transform.localPosition = V(d.p.x, Mathf.Lerp(40, d.p.y + d.r * 0.5f, k * k), d.p.z); d.chunk.transform.Rotate(dt * 300, 0, dt * 200); }
                if (!d.done && d.t >= d.delay)
                {
                    d.done = true; if (d.warn) Destroy(d.warn); if (d.chunk) Destroy(d.chunk);
                    SpawnParticles(d.p + V(0, 0.6f, 0), 22, 8, cyanMat, 0.14f, 0.8f); ShockRing(d.p, d.r + 1.5f, 0xb0e8ff, 0.4f);
                    for (int j = 0; j < 3; j++) particles.Add(new Pt { pos = d.p + V(0, 0.5f, 0), scale = Vector3.one * (0.4f + rand * 0.4f), mat = tIceSolidM, v = V((rand - 0.5f) * 9, 4 + rand * 4, (rand - 0.5f) * 9), life = 2, grav = GRAV, hasSpin = true, spin = V(rand * 10, rand * 10, rand * 10) });
                    Sfx("shatter", 0.6f); P.shake = Mathf.Max(P.shake, 0.25f);
                    if (!P.dead && Hypot(P.pos.x - d.p.x, P.pos.z - d.p.z) < d.r && P.pos.y - d.p.y < 3) { DamagePlayer(d.dmg, d.p); chillT = Mathf.Max(chillT, 1.4f); }
                    tDrops.RemoveAt(i);
                }
            }
            for (int i = tPatches.Count - 1; i >= 0; i--)
            {
                var p = tPatches[i]; p.life -= dt; p.armT -= dt;
                if (!p.g) { tPatches.RemoveAt(i); continue; }
                bool armed = p.armT <= 0;
                float fade = Mathf.Min(1, p.life / 0.5f);
                Mats.SetAlpha(p.m, (armed ? 0.5f + 0.15f * Mathf.Sin(p.life * 9) : 0.18f + 0.2f * Mathf.Sin(p.life * 22)) * fade);
                if (armed && rand < 0.25f) SpawnParticles(p.c + V((rand - 0.5f) * p.r * 1.5f, 0.2f, (rand - 0.5f) * p.r * 1.5f), 1, 1, cyanMat, 0.07f, 0.6f, -1);
                if (armed && !P.dead && Hypot(P.pos.x - p.c.x, P.pos.z - p.c.z) < p.r && P.pos.y - p.c.y < 1.4f)
                { chillT = Mathf.Max(chillT, 1.0f); p.tick -= dt; if (p.tick <= 0) { p.tick = 0.45f; DamagePlayer(7, p.c, true); } }
                if (p.life <= 0) { Destroy(p.g); tPatches.RemoveAt(i); }
            }
            // the severed head: falls, bounces, rolls to a stop and stays where it lies
            if (tHead && !tHeadRest)
            {
                tHeadV.y -= GRAV * dt; tHead.position += tHeadV * dt; tHead.Rotate(tHeadSpin * dt, Space.World);
                if (tHead.position.y < 1.2f)
                {
                    var p = tHead.position; p.y = 1.2f; tHead.position = p;
                    if (Mathf.Abs(tHeadV.y) > 4) { tHeadV.y = -tHeadV.y * 0.35f; tHeadV.x *= 0.6f; tHeadV.z *= 0.6f; tHeadSpin *= 0.6f; Sfx("slam", 0.6f); SpawnParticles(p, 12, 5, SnowMat, 0.2f, 0.9f); P.shake = Mathf.Max(P.shake, 0.3f); }
                    else { tHeadV *= Mathf.Exp(-4 * dt); tHeadSpin *= Mathf.Exp(-4 * dt); if (tHeadV.sqrMagnitude < 0.1f) { tHeadRest = true; var e = tHead.eulerAngles; tHead.rotation = Quaternion.Euler(0, e.y, 70); } }
                }
            }
            // the ice block he hurls through the portal
            if (tThrow && tThrowSt >= 2)
            {
                tThrowT += dt;
                if (tThrowSt == 2)
                {
                    tThrowV.y -= GRAV * dt; tThrow.position += tThrowV * dt; tThrow.Rotate(V(190, 70, 40) * dt, Space.Self);
                    if (rand < 0.8f) SpawnParticles(tThrow.position, 2, 2, cyanMat, 0.15f, 0.6f, -1);
                    if (Vector3.Distance(tThrow.position, tPortalHome) < 3.2f || tThrowT > 2.4f) { BreakPortal(); tThrowSt = 3; tThrowV = tThrowDir * 9; tThrowV.y = 0; }
                }
                else if (tThrowSt == 3)
                {
                    // it drops out of the shattered portal, slams down and skids to a stop
                    tThrowV.y -= GRAV * dt; var p = tThrow.position + tThrowV * dt;
                    if (p.y < 2.2f) { p.y = 2.2f; if (tThrowV.y < -6) { Sfx("boom"); P.shake = Mathf.Max(P.shake, 0.8f); SpawnParticles(p - V(0, 2, 0), 40, 10, SnowMat, 0.3f, 1.2f); ShockRing(p - V(0, 2, 0), 9, 0xc8f0ff, 0.5f); if (!P.dead && Hypot(P.pos.x - p.x, P.pos.z - p.z) < 5.5f && P.pos.y < 5) DamagePlayer(28, p); } tThrowV.y = 0; tThrowV.x *= Mathf.Exp(-2.5f * dt); tThrowV.z *= Mathf.Exp(-2.5f * dt); if (rand < 0.6f) SpawnParticles(p - V(0, 2, 0), 1, 3, SnowMat, 0.2f, 0.6f); }
                    tThrow.position = p;
                    tThrow.rotation = Quaternion.Slerp(tThrow.rotation, Quaternion.Euler(8, Mathf.Atan2(tThrowDir.x, tThrowDir.z) * Mathf.Rad2Deg + 25, -12), 1 - Mathf.Exp(-3 * dt));
                    if (p.y <= 2.21f && new Vector2(tThrowV.x, tThrowV.z).sqrMagnitude < 0.3f) { tThrowSt = 4; SettleBlock(); }
                }
            }
            // the portal's broken pieces, tumbling to the floor
            foreach (var pc in tPieces)
            {
                if (pc.rest || !pc.t) continue;
                pc.v.y -= GRAV * dt; var p = pc.t.position + pc.v * dt; pc.t.Rotate(pc.spin * dt, Space.World);
                if (p.y < 0.25f) { p.y = 0.25f; if (pc.v.y < -5) { pc.v.y *= -0.3f; pc.v.x *= 0.5f; pc.v.z *= 0.5f; pc.spin *= 0.5f; } else { pc.rest = true; pc.restPos = p; pc.restRot = pc.t.rotation; } }
                pc.t.position = p;
            }
        }
        // the hurled block comes to rest as part of the arena: a cracked, tilted monolith with shards round it (solid)
        void SettleBlock()
        {
            if (!tThrow) return;
            var p = tThrow.position;
            for (int k = 0; k < 8; k++) { float a = k * 0.8f; Deco(Geo.Cone(0.5f + (k % 3) * 0.3f, 1.6f + (k % 4) * 0.7f, 5), tIceSolidM, p.x + Mathf.Cos(a) * 3.4f, 0.6f, p.z + Mathf.Sin(a) * 3.4f, Mathf.Sin(a) * 0.5f, 0, -Mathf.Cos(a) * 0.5f).layer = Mk.LayerIgnore; }
            for (int k = 0; k < 5; k++) Deco(Geo.Box(1 + rand, 0.6f, 0.8f + rand), tIceSolidM, p.x + (rand - 0.5f) * 8, 0.3f, p.z + (rand - 0.5f) * 8, rand, rand * 3, rand).layer = Mk.LayerIgnore;
            SolidAABB(V(p.x - 2.6f, 0, p.z - 2.6f), V(p.x + 2.6f, 4.2f, p.z + 2.6f));
            var g = Mk.Glow(levelRoot, 0x80d8ff, 9, 0.3f); g.transform.localPosition = p;
        }

        // ================= the portal: torn apart, then rebuilt =================
        void BreakPortal()
        {
            if (portal == null || tPortalBroken) return;
            tPortalBroken = true;
            var p = portal; tPortalHome = p.g.position; tPortalRot = p.g.rotation;
            var dir = tThrowDir.sqrMagnitude > 0 ? tThrowDir : Vector3.forward;
            // the ring and its spikes break into pieces that scatter across the floor (kept, so they can come back)
            int segs = 12;
            for (int i = 0; i < segs; i++)
            {
                float a = i / (float)segs * Mathf.PI * 2;
                var home = tPortalHome + tPortalRot * V(Mathf.Cos(a) * 2.3f, Mathf.Sin(a) * 2.3f, 0);
                var homeRot = tPortalRot * Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
                var g = Mk.Msh(levelRoot, Geo.Torus(2.3f, 0.2f, 6, 5, Mathf.PI * 2 / segs), tPieceM); g.layer = Mk.LayerIgnore;
                // the arc mesh is centred on the ring's centre: offset it so the piece sits (and spins) about its own middle
                var t = new GameObject("piece").transform; t.SetParent(levelRoot, false); t.position = home; t.rotation = homeRot;
                g.transform.SetParent(t, false); g.transform.localPosition = V(-2.3f, 0, 0); g.transform.localRotation = Quaternion.Euler(0, 0, -(180f / segs) * 0 );
                var spike = Mk.Msh(t, Geo.Cone(0.12f, 0.7f, 4), tPieceM, 0.45f, 0, 0, 0, 0, -Mathf.PI / 2); spike.layer = Mk.LayerIgnore;
                var outDir = (home - tPortalHome).normalized;
                tPieces.Add(new TPiece { t = t, home = home, homeRot = homeRot, v = outDir * (8 + rand * 8) + dir * (6 + rand * 6) + V(0, 4 + rand * 6, 0), spin = V(rand - 0.5f, rand - 0.5f, rand - 0.5f) * 400 });
            }
            for (int i = 0; i < 8; i++) // shards of the light disc
            {
                var g = Mk.Msh(levelRoot, Geo.Octahedron(0.35f), tPieceM, tPortalHome.x, tPortalHome.y, tPortalHome.z); g.layer = Mk.LayerIgnore; g.transform.localScale = V(0.6f, 1.6f, 0.3f);
                float a = i / 8f * Mathf.PI * 2; var home = tPortalHome + tPortalRot * V(Mathf.Cos(a) * 1.0f, Mathf.Sin(a) * 1.0f, 0);
                tPieces.Add(new TPiece { t = g.transform, home = home, homeRot = tPortalRot * Quaternion.Euler(0, 0, a * Mathf.Rad2Deg), v = V(rand - 0.5f, rand * 0.6f, rand - 0.5f) * 16 + dir * 8, spin = V(rand - 0.5f, rand - 0.5f, rand - 0.5f) * 600 });
            }
            Mk.DestroyTree(p.g.gameObject); Mk.DestroyTree(p.beam.gameObject); Mk.DestroyTree(p.beam2.gameObject); portal = null;
            Blast(tPortalHome, 9, 0xc8f0ff); Blast(tPortalHome, 4, 0xffffff); ShockRing(V(tPortalHome.x, 0, tPortalHome.z), 16, THEME.portal, 0.7f);
            SpawnParticles(tPortalHome, 60, 18, cyanMat, 0.14f, 1.4f); SpawnParticles(tPortalHome, 30, 12, SnowMat, 0.2f, 1.2f);
            Sfx("shatter"); Sfx("boom"); Sfx("implode"); whiteFlash = Mathf.Max(whiteFlash, 0.45f); P.shake = Mathf.Max(P.shake, 1.0f); freeze = Mathf.Max(freeze, 0.08f);
            Banner("THE PORTAL SHATTERS", "THERE IS NO WAY OUT", "#80d8ff"); SetHint(null);
            if (!P.dead && Vector3.Distance(ChestPos, tPortalHome) < 5.5f) DamagePlayer(26, tPortalHome);
        }

        // ================= phase 1 -> 2: the beheading, the portal, the throw =================
        void TraitorFall(Ent e)
        {
            if (e.state == "fall") return;
            e.hp = 0; e.invuln = true; e.vel = Vector3.zero;
            ClearTraitorAttacks(); PrisonBreak(false);
            foreach (var pr in projectiles) if (!pr.friendly) pr.life = 0.0001f;
            TState(e, "fall", 999);
            Sfx("roar"); freeze = 0.2f; slowmoT = Mathf.Max(slowmoT, 1.0f); P.shake = Mathf.Max(P.shake, 0.8f); whiteFlash = Mathf.Max(whiteFlash, 0.3f);
            AddStyle("TRAITOR BROKEN", 600);
        }
        void LookAssist(Vector3 target, float strength, float dt)
        {
            if (testMode || P.dead) return;
            var d = target - EyePos; if (d.sqrMagnitude < 1) return;
            float yaw = Mathf.Atan2(-d.x, -d.z), pitch = Mathf.Asin(Mathf.Clamp(d.normalized.y, -1, 1));
            float k = 1 - Mathf.Exp(-strength * dt);
            P.yaw += Mathf.Atan2(Mathf.Sin(yaw - P.yaw), Mathf.Cos(yaw - P.yaw)) * k; P.pitch += (pitch - P.pitch) * k;
        }
        void TraitorFallUpdate(Ent e, float dt)
        {
            var r = e.tr; float T = r.t;
            // the bar sits empty while he falls
            if (T >= 2.2f && T - dt < 2.2f) { r.collar.SetActive(true); Sfx("charge"); }
            if (r.collar.activeSelf) { float k = clamp((T - 2.2f) / 0.8f, 0, 1); r.collar.transform.localScale = Vector3.one * (0.6f + k * 0.6f); Mats.SetAlpha(r.frost, 0.5f + 0.5f * Mathf.Sin(T * 30)); if (rand < 0.6f) SpawnParticles(r.neck.t.position, 2, 2, cyanMat, 0.1f, 0.6f); }
            if (T < 3.4f) LookAssist(r.neck.t.position, 2.5f, dt);
            if (T >= 3.0f && !r.headless)
            {
                // the frozen neck snaps: the head tears free in a spray of blue frost-blood
                r.headless = true; r.collar.SetActive(false);
                var hp = r.head.t.position; var fwd = Fwd(e);
                Mk.StripColliders(r.head.go); r.head.t.SetParent(levelRoot, true); tHead = r.head.t; tHeadRest = false;
                tHeadV = fwd * 7 + V(0, 9, 0) + V(-fwd.z, 0, fwd.x) * 3; tHeadSpin = V(220, 80, 140);
                for (int i = 0; i < 3; i++) Spray(hp, (V(0, 1, 0) + V(rand - 0.5f, 0, rand - 0.5f)).normalized, 30, 12, 0.5f, tFrostBloodM, 0.16f, 1.6f);
                SpawnParticles(hp, 40, 10, cyanMat, 0.14f, 1.2f); Blast(hp, 4, 0x80e0ff); ShockRing(V(e.pos.x, 0, e.pos.z), 14, 0x80e0ff, 0.6f);
                r.eyeGlow.SetActive(false); if (r.mouthGlow) r.mouthGlow.SetActive(false);
                Sfx("shatter"); Sfx("boom"); whiteFlash = Mathf.Max(whiteFlash, 0.5f); P.shake = Mathf.Max(P.shake, 1.2f); freeze = Mathf.Max(freeze, 0.12f); slowmoT = Mathf.Max(slowmoT, 1.4f);
                Banner("BEHEADED", "THE FROZEN TRAITOR FALLS", "#8ad8ff");
            }
            if (r.headless && T < 8.6f && rand < 0.3f) Spray(r.neck.t.position, V(0, 1, 0), 2, 6, 0.4f, tFrostBloodM, 0.12f, 1.0f);
            if (T >= 4.2f && T - dt < 4.2f) { Sfx("slam"); P.shake = Mathf.Max(P.shake, 0.9f); SpawnParticles(e.pos + Fwd(e) * 5, 40, 9, SnowMat, 0.35f, 1.2f); }
            if (T >= 4.6f && T - dt < 4.6f)
            {
                OpenPortal(true); // looks like the way out... it isn't, not yet (finale: it can't be entered)
                Banner("THE PORTAL OPENS", "GET OUT - NOW", "#8ad8ff"); SetHint("◆ RUN FOR THE PORTAL ◆");
            }
            if (T >= 8.6f && T - dt < 8.6f) { Sfx("growlLow"); P.shake = Mathf.Max(P.shake, 0.4f); SetHint(null); }
            // the headless body forms a block of ice in its hand...
            if (T >= 9.8f && !tThrow)
            {
                var g = new GameObject("iceThrow").transform; g.SetParent(r.rHand.t, false); g.localPosition = V(0, -0.9f, 0.2f); g.localScale = Vector3.one * 0.01f;
                Mk.Msh(g, Geo.Box(1.5f, 1.25f, 1.35f), tIceSolidM).layer = Mk.LayerIgnore; Mk.Msh(g, Geo.Box(1.7f, 1.4f, 1.5f), tIceM).layer = Mk.LayerIgnore;
                for (int k = 0; k < 6; k++) Mk.Msh(g, Geo.Cone(0.18f, 0.6f, 5), tIceSolidM, (k % 3 - 1) * 0.5f, 0.75f, (k / 3 - 0.5f) * 0.5f).layer = Mk.LayerIgnore;
                Gl(new N(g), 0x80e0ff, 3.2f, 0.6f);
                tThrow = g; tThrowSt = 1; Sfx("laserCharge");
                if (portal != null) { var ground = V(portal.pos.x, 0, portal.pos.z); WarnDisc(ground, 6, 3.5f, 0xb0e8ff); }
            }
            if (tThrow && tThrowSt == 1)
            {
                float k = clamp((T - 9.8f) / 1.0f, 0, 1); tThrow.localScale = Vector3.one * Mathf.Max(0.01f, k) / 3.2f * 3.0f;
                if (rand < 0.8f) { var src = tThrow.position + Random.onUnitSphere * 5; particles.Add(new Pt { pos = src, scale = Vector3.one * 0.12f, mat = cyanMat, v = (tThrow.position - src) * 3, life = 0.35f, grav = 0 }); }
                if (portal != null) LookAssist(Vector3.Lerp(tThrow.position, portal.pos, 0.35f), 1.4f, dt);
                // ...and hurls it at the portal
                if (T >= 11.5f)
                {
                    var target = portal != null ? portal.pos : LEVEL.center + V(0, 2.6f, 0);
                    var p0 = tThrow.position; tThrow.SetParent(levelRoot, true);
                    float tf = 1.15f; var v = (target - p0) / tf; v.y += 0.5f * GRAV * tf;
                    tThrowV = v; tThrowDir = V(target.x - p0.x, 0, target.z - p0.z).normalized; tThrowSt = 2; tThrowT = 0; tPortalHome = target;
                    Sfx("dashGo"); Sfx("roarShort"); P.shake = Mathf.Max(P.shake, 0.6f);
                }
            }
            if (T >= 14.0f && T - dt < 14.0f)
            {
                r.stump.SetActive(true); Sfx("nova"); Blast(r.neck.t.position, 5, 0xc8f0ff); P.shake = Mathf.Max(P.shake, 0.8f);
            }
            // the same bar floods back up with frost energy, full again for phase 2
            if (T >= 14.4f && T - dt < 14.4f) Sfx("laserCharge");
            if (T >= 14.4f)
            {
                float k = clamp((T - 14.4f) / 2.4f, 0, 1);
                e.hp = e.maxHp * Smoother(k);
                float pulse = 0.5f + 0.5f * Mathf.Sin(T * 18);
                BossBarTint(Color.Lerp(Mats.Css("#c8f6ff"), Color.white, pulse), Mats.Css("#2a7ab8"));
                if (rand < 0.6f) SpawnParticles(r.neck.t.position + V(0, 2, 0), 2, 4, cyanMat, 0.12f, 0.8f, -4);
            }
            if (T >= 16.8f)
            {
                e.hp = e.maxHp; e.phase2 = true; e.invuln = false; r.headless = true;
                e.headY = 6.4f; e.bodyY = 5.2f; e.name = "THE HEADLESS TRAITOR"; SetBossName(e.name);
                BossBarTint(Mats.Css("#9ae8ff"), Mats.Css("#1a5a90"));
                Sfx("shatter"); Sfx("roar"); ShockRing(V(e.pos.x, 0, e.pos.z), 22, 0x80e0ff, 0.6f); AddRing(V(e.pos.x, 0, e.pos.z), 20, 26, 0, 0x80e0ff);
                audioMgr.Intensity();
                TState(e, "chase", 1.0f);
            }
        }

        // ================= the fight =================
        string PickTraitor(Ent e, float dist)
        {
            List<string> o;
            if (!e.phase2)
                o = dist < 13 ? new List<string> { "cleave", "cleave", "gslam", "prison", "breath" }
                  : dist < 28 ? new List<string> { "breath", "prison", "icefall", "dcharge", "gslam", "spears" }
                  : new List<string> { "dcharge", "icefall", "prison", "spears", "dcharge" };
            else
                o = dist < 13 ? new List<string> { "cleave", "gslam", "cataclysm", "breath", "cataclysm" }
                  : dist < 28 ? new List<string> { "rampage", "breath", "icefall", "cataclysm", "prison", "dcharge", "spears" }
                  : new List<string> { "rampage", "dcharge", "icefall", "rampage" };
            o.RemoveAll(s => s == e.last); var pick = o[Mathf.Min(o.Count - 1, (int)(rand * o.Count))]; e.last = pick; return pick;
        }
        float TD(Ent e, float v) => v * (e.phase2 ? 1.45f : 1.15f);
        Vector3 TraitorMouth(Ent e) => e.tr.headless ? e.tr.neck.t.position + V(0, 1.5f, 0) : e.tr.jaw.t.position;
        bool InFan(Vector3 c, float yaw, float r, float half, Vector3 p)
        {
            var d = V(p.x - c.x, 0, p.z - c.z); float dist = d.magnitude; if (dist > r) return false; if (dist < 1.5f) return true;
            float a = Mathf.Atan2(d.x, d.z) - yaw; a = Mathf.Atan2(Mathf.Sin(a), Mathf.Cos(a)); return Mathf.Abs(a) < half;
        }

        void UpdateTraitor(Ent e, float dt)
        {
            var r = e.tr; UpdateTraitorFx(dt);
            float sp = e.phase2 ? 1.45f : 1.1f;
            bool scripted = e.state == "frozen" || e.state == "awaken" || e.state == "fall";
            float sdt = scripted ? dt : dt * sp;
            r.t += sdt; e.stateT -= sdt; e.flash -= dt;
            float dx = Tgt.pos.x - e.pos.x, dz = Tgt.pos.z - e.pos.z, dist = Hypot(dx, dz), dy = Tgt.pos.y - e.pos.y; if (dist == 0) dist = 1; // (co-op: he hunts Tgt; hits are tested on the local P)
            float myDy = P.pos.y - e.pos.y;
            var fwd = Fwd(e); bool face = true; float move = 0;
            switch (e.state)
            {
                case "frozen":
                    face = false;
                    if (Auth && (dist < 34 || r.t > 16)) { NetAct(e, "awaken"); TState(e, "awaken", 5.0f); SetHint(null); Sfx("charge"); Sfx("growlLow"); }
                    break;
                case "awaken":
                {
                    face = false; float T = r.t;
                    // the ice cracks wider and wider, he moves inside it, chunks fall away... and he breaks out
                    float ck = Smoother(T / 4.2f);
                    for (int i = 0; i < tCracks.Count; i++) { if (!tCracks[i]) continue; float kk = clamp((ck * tCracks.Count - i) / 3f, 0, 1); tCracks[i].localScale = V(1, Mathf.Max(0.01f, kk), 1); }
                    Mats.SetAlpha(tCrackM, 0.4f + 0.5f * ck * (Mathf.Sin(T * 30) > 0 ? 1 : 0.7f));
                    if (T > 1.0f) Mats.SetColor(r.glow, Color.Lerp(Mats.Hex(0x1a3a4a), Mats.Hex(0x80f0ff), clamp((T - 1) / 0.8f, 0, 1)));
                    P.shake = Mathf.Max(P.shake, 0.1f + ck * 0.35f);
                    if (tBlock) tBlock.transform.localPosition = V(e.pos.x + (rand - 0.5f) * ck * 0.3f, 0, e.pos.z + (rand - 0.5f) * ck * 0.3f);
                    // frost light leaks through the widening cracks; flakes and chips spit off the faces
                    if (tBlock && rand < 0.3f + ck) { var bp = tBlock.transform.position; SpawnParticles(bp + V((rand - 0.5f) * 11, 1 + rand * 12, rand < 0.5f ? 4.9f : -4.9f), 1, 2 + ck * 4, rand < 0.5f ? cyanMat : SnowMat, 0.1f, 0.6f); }
                    foreach (var at in new[] { 1.4f, 2.2f, 3.4f, 3.9f })
                        if (T >= at && T - dt < at && tBlock) { Sfx("shatter", 0.5f); IceShards(tBlock.transform.position + V((rand - 0.5f) * 9, 4 + rand * 8, 4.9f), 4, 1.5f, 4, V(0, 0, 3), 0.8f); P.shake = Mathf.Max(P.shake, 0.4f); }
                    // his fist punches out through the front wall
                    if (T >= 2.8f && T - dt < 2.8f && tBlock)
                    {
                        var fp = tBlock.transform.position + V(1.6f, 5.6f, 4.9f);
                        IceShards(fp, 16, 2.2f, 9, Fwd(e) * 6, 1.1f); SpawnParticles(fp, 40, 10, cyanMat, 0.14f, 0.9f); SpawnParticles(fp, 24, 7, SnowMat, 0.25f, 1.1f);
                        Blast(fp, 4, 0xc8f0ff); Sfx("slam"); Sfx("shatter"); P.shake = Mathf.Max(P.shake, 0.9f); freeze = Mathf.Max(freeze, 0.06f);
                    }
                    if (T >= 4.2f && tBlock && tBlockSolid)
                    {
                        BreakIceBlock();
                        ShowBossBar(e); audioMgr.Start(10); StartBossIntro(e);
                        if (!BossIntroActive) Banner("THE FROZEN TRAITOR", "THE BETRAYER BOUND IN ICE", "#8ad8ff");
                    }
                    if (e.stateT <= 0 && !BossIntroActive) { e.invuln = false; TState(e, "chase", 1.0f); }
                    break;
                }
                case "fall": face = false; TraitorFallUpdate(e, dt); break;
                case "chase":
                    move = e.phase2 ? 9.6f : 7.2f;
                    // you're up out of his reach (a gallery, the stairs, the fallen block): he leaps up after you
                    if (dy > 2.2f && Tgt.onGround && !Down(Tgt)) e.leapCd += dt; else e.leapCd = Mathf.Max(0, e.leapCd - dt);
                    if (Auth && e.leapCd > 0.8f && dist < 48) { e.leapCd = 0; NetAct(e, "tleap"); TraitorBegin(e, "tleap"); break; }
                    if (e.stateT <= 0 && Auth) // co-op: the host picks; clients run the same setup when its pick arrives (NetBossBegin)
                    {
                        string a = PickTraitor(e, dist);
                        NetAct(e, a); TraitorBegin(e, a);
                    }
                    break;
                // ---- 1. FROST CLEAVE: a wide sweep of the arm ----
                case "cleaveWind":
                    face = e.stateT > 0.18f; // he keeps turning to follow you until the last instant
                    if (tCleaveWarn != null && tCleaveWarn.g) { tCleaveWarn.g.transform.localPosition = V(e.pos.x, e.pos.y + 0.09f, e.pos.z); tCleaveWarn.g.transform.localRotation = Quaternion.Euler(0, e.yaw * Mathf.Rad2Deg, 0); }
                    if (e.stateT <= 0) { TState(e, "cleave", 0.32f); Sfx("dashGo"); }
                    break;
                case "cleave":
                    face = false;
                    if (r.t < 0.22f) { e.vel.x = fwd.x * 15; e.vel.z = fwd.z * 15; } // he lunges into the sweep
                    if (!e.hitPlayer && r.t > 0.08f)
                    {
                        e.hitPlayer = true;
                        for (int k = 0; k < 18; k++) { float a = e.yaw + (k / 17f - 0.5f) * 2.7f; SpawnParticles(e.pos + V(Mathf.Sin(a), 0, Mathf.Cos(a)) * (6 + rand * 8) + V(0, 1 + rand * 3, 0), 1, 4, cyanMat, 0.12f, 0.5f); }
                        if (!P.dead && InFan(e.pos, e.yaw, 15 + 0.5f, 1.35f, P.pos) && myDy < 5) { DamagePlayer(TD(e, 30), e.pos); var side = V(-fwd.z, 0, fwd.x); P.vel += side * 14 + fwd * 8 + V(0, 6, 0); P.onGround = false; }
                        P.shake = Mathf.Max(P.shake, 0.5f); Sfx("slam", 0.7f);
                    }
                    if (e.stateT <= 0)
                    {
                        // a backhand straight after the first sweep (more often once he's headless)
                        if (r.combo < (e.phase2 ? 2 : 1) && rand < (e.phase2 ? 0.75f : 0.55f)) { r.combo++; e.yaw = Mathf.Atan2(dx, dz); TState(e, "cleaveWind", 0.5f); tCleaveWarn = WarnFan(e.pos, e.yaw, 15, 1.35f, 0.5f / sp + 0.2f, 0xff6060); Sfx("growlLow"); }
                        else TState(e, "recover", 0.55f);
                    }
                    break;
                // ---- 2. GLACIAL SLAM: both fists into the floor; an ice shockwave and spike lines ----
                case "gslamWind": face = false; if (e.stateT <= 0) { TraitorSlam(e, r.aim, 7, TD(e, 34), e.phase2 ? 8 : 5); TState(e, "recover", 1.0f); } break;
                // ---- 3. FROZEN PRISON: a frost circle hunts you, then locks and erupts in ice that traps you ----
                case "prisonWind":
                {
                    var w = tPrisonWarn;
                    if (e.stateT > 0.35f) r.aim = Vector3.Lerp(r.aim, V(Tgt.pos.x, 0, Tgt.pos.z), 1 - Mathf.Exp(-6 * dt));
                    if (w != null && w.g) w.g.transform.localPosition = V(r.aim.x, 0.09f, r.aim.z);
                    if (e.stateT <= 0)
                    {
                        if (w != null && w.g) { w.keep = false; w.dur = w.t + 0.15f; } tPrisonWarn = null;
                        for (int k = 0; k < 9; k++) { float a = k * 0.7f; var p = r.aim + V(Mathf.Cos(a), 0, Mathf.Sin(a)) * 2.4f; AddSpike(p.x, 0, p.z, 0, true, 0); }
                        SpawnParticles(r.aim + V(0, 1, 0), 30, 8, cyanMat, 0.14f, 0.8f); Sfx("shatter");
                        if (!P.dead && Hypot(P.pos.x - r.aim.x, P.pos.z - r.aim.z) < 3.2f && P.pos.y < 3) PrisonStart();
                        TState(e, "recover", 0.7f);
                    }
                    break;
                }
                // ---- 4. FROST BREATH: a cone of killing cold (from the jaws - or, headless, from the neck) ----
                case "breathWind":
                {
                    if (e.stateT > 0.3f) { r.aimYaw = Mathf.Atan2(dx, dz); }
                    if (e.vidx == 0) { e.vidx = 1; tBreathWarn = WarnFan(e.pos, r.aimYaw, 26, 0.48f, 0.95f / sp + 1.9f, 0xb0f0ff); }
                    else if (tBreathWarn != null && tBreathWarn.g && e.stateT > 0.3f) { tBreathWarn.g.transform.localRotation = Quaternion.Euler(0, r.aimYaw * Mathf.Rad2Deg, 0); tBreathWarn.g.transform.localPosition = V(e.pos.x, e.pos.y + 0.09f, e.pos.z); }
                    TurnTo(e, r.aimYaw, 6, dt); face = false;
                    if (r.mouthGlow) { r.mouthGlow.transform.localScale = Vector3.one * (0.6f + (1 - e.stateT / 0.95f) * 2); Mats.SetAlpha(r.mouthGlow.GetComponent<Renderer>().sharedMaterial, 0.9f); }
                    if (e.stateT <= 0) { TState(e, "breath", 1.8f); Sfx("laserCharge"); }
                    break;
                }
                case "breath":
                {
                    face = false;
                    { float want = Mathf.Atan2(dx, dz), dd = Mathf.DeltaAngle(r.aimYaw * Mathf.Rad2Deg, want * Mathf.Rad2Deg) * Mathf.Deg2Rad; r.aimYaw += Mathf.Clamp(dd, -0.95f * dt * sp, 0.95f * dt * sp); } // the breath sweeps after you
                    if (tBreathWarn != null && tBreathWarn.g) { tBreathWarn.g.transform.localRotation = Quaternion.Euler(0, r.aimYaw * Mathf.Rad2Deg, 0); tBreathWarn.g.transform.localPosition = V(e.pos.x, e.pos.y + 0.09f, e.pos.z); }
                    TurnTo(e, r.aimYaw, 6, dt);
                    var m = TraitorMouth(e); var dir = V(Mathf.Sin(r.aimYaw), -0.18f, Mathf.Cos(r.aimYaw)).normalized;
                    for (int k = 0; k < 6; k++) { var v = (dir + Random.insideUnitSphere * 0.28f).normalized * (26 + rand * 10); particles.Add(new Pt { pos = m, scale = Vector3.one * (0.16f + rand * 0.2f), mat = rand < 0.6f ? cyanMat : SnowMat, v = v, life = 0.8f, grav = 0 }); }
                    TrimParticles();
                    if (r.t % 0.22f < sdt) Sfx("laserTick", 0.5f);
                    r.chargeDir.x -= dt; // damage tick timer
                    if (r.chargeDir.x <= 0) { r.chargeDir.x = 0.2f; if (!P.dead && InFan(e.pos, r.aimYaw, 26, 0.48f, P.pos) && myDy < 7 && Los(m, ChestPos)) { DamagePlayer(TD(e, 7), m); chillT = Mathf.Max(chillT, 1.6f); } }
                    if (e.stateT <= 0) { if (r.mouthGlow) Mats.SetAlpha(r.mouthGlow.GetComponent<Renderer>().sharedMaterial, 0); TState(e, "recover", 0.8f); }
                    break;
                }
                // ---- 5. ICEFALL: he calls the ceiling down - marked circles, then the ice ----
                case "icefallWind":
                    face = false;
                    if (rand < 0.8f) SpawnParticles(e.pos + V((rand - 0.5f) * 8, 12 + rand * 6, (rand - 0.5f) * 8), 1, 2, cyanMat, 0.12f, 0.6f, -3);
                    if (e.stateT <= 0)
                    {
                        int n = e.phase2 ? 16 : 12; float lim = LB - 3;
                        for (int k = 0; k < n; k++)
                        {
                            Vector3 p = k < 5 ? Tgt.pos + V(Tgt.vel.x, 0, Tgt.vel.z) * (1.0f + k * 0.12f) + V((rand - 0.5f) * 3, 0, (rand - 0.5f) * 3) : Tgt.pos + V((rand - 0.5f) * 32, 0, (rand - 0.5f) * 32);
                            p.x = clamp(p.x, -lim, lim); p.z = clamp(p.z, -lim, lim); p.y = Mathf.Max(0, Tgt.onGround ? Tgt.pos.y : 0);
                            IceDrop(p, 1.1f + k * 0.1f, 2.8f, TD(e, 24));
                        }
                        Sfx("roar"); TState(e, "recover", 0.9f);
                    }
                    break;
                // ---- 6. DEMONIC CHARGE: horns down, straight at you ----
                case "dchargeWind":
                {
                    if (e.stateT > 0.15f) { r.chargeDir = V(dx / dist, 0, dz / dist); }
                    if (e.vidx == 0 && e.stateT <= 0.6f) { e.vidx = 1; WarnRect(e.pos, r.chargeDir, 50, 6, 0.6f / sp + 0.3f, 0xff6060); }
                    face = e.stateT > 0.3f;
                    if (rand < 0.4f) SpawnParticles(e.pos + V(0, 0.3f, 0), 1, 4, SnowMat, 0.2f, 0.5f);
                    if (e.stateT <= 0) { TState(e, "dcharge", 2.2f); e.chargeFrom = e.pos; }
                    break;
                }
                case "dcharge":
                    face = false;
                    if (dist > 7) r.chargeDir = Vector3.RotateTowards(r.chargeDir, V(dx / dist, 0, dz / dist), 0.55f * dt, 0).normalized; // it curves after you
                    TurnTo(e, Mathf.Atan2(r.chargeDir.x, r.chargeDir.z), 12, dt);
                    e.vel.x = r.chargeDir.x * 36 * sp; e.vel.z = r.chargeDir.z * 36 * sp;
                    if (rand < 0.8f) SpawnParticles(e.pos + V(0, 0.4f, 0), 2, 5, SnowMat, 0.25f, 0.5f);
                    if (!e.hitPlayer && MeIn(e.pos, 4.2f, 6)) { e.hitPlayer = true; DamagePlayer(TD(e, 38), e.pos); P.vel += r.chargeDir * 26 + V(0, 11, 0); P.onGround = false; P.coyote = 0; }
                    if (e.stateT <= 0) TState(e, "recover", 0.7f);
                    break;
                case "stunned":
                    face = false;
                    if (rand < 0.25f) SpawnParticles(BodyPoint(e) + V(0, 2, 0), 1, 2, cyanMat, 0.14f, 0.6f, -2);
                    if (e.stateT <= 0) TState(e, "chase", 0.8f);
                    break;
                // ---- 7. FROZEN CATACLYSM (phase 2): both arms down - a vast ring, spike lines everywhere, freezing ground ----
                case "cataWind":
                    face = false;
                    if (rand < 0.9f) { var src = e.pos + Random.onUnitSphere * 14; src.y = Mathf.Abs(src.y) + 2; particles.Add(new Pt { pos = src, scale = Vector3.one * 0.14f, mat = cyanMat, v = (BodyPoint(e) + V(0, 4, 0) - src) * 2, life = 0.5f, grav = 0 }); }
                    if (e.stateT <= 0)
                    {
                        TraitorSlam(e, V(e.pos.x, 0, e.pos.z), 10, TD(e, 40), 10, true);
                        foreach (var p in tPatches) if (p.armT > 50) { p.armT = 0; p.life = p.max = 4.2f; } // the frosted ground it marked freezes now
                        TState(e, "recover", 1.3f);
                    }
                    break;
                // ---- 8. BLOODFROST RAMPAGE (phase 2): charge after charge, each leaving a burning-cold trail, then a crushing slam ----
                case "rampWind":
                    if (e.stateT > 0.2f) r.chargeDir = V(dx / dist, 0, dz / dist);
                    if (e.vidx == 0) { e.vidx = 1; WarnRect(e.pos, r.chargeDir, 34, 5, 0.6f / sp + 0.2f, 0xff5070); }
                    face = e.stateT > 0.2f;
                    if (e.stateT <= 0) { TState(e, "ramp", 0.95f); Sfx("dashGo"); }
                    break;
                case "ramp":
                    face = false; TurnTo(e, Mathf.Atan2(r.chargeDir.x, r.chargeDir.z), 14, dt);
                    e.vel.x = r.chargeDir.x * 38; e.vel.z = r.chargeDir.z * 38;
                    r.aim.y -= dt; if (r.aim.y <= 0) { r.aim.y = 0.12f; FrostPatch(V(e.pos.x, 0, e.pos.z), 2.4f, 3.4f); }
                    if (!e.hitPlayer && MeIn(e.pos, 4.2f, 6)) { e.hitPlayer = true; DamagePlayer(TD(e, 30), e.pos); P.vel += r.chargeDir * 22 + V(0, 10, 0); P.onGround = false; }
                    if (e.stateT <= 0) { if (--e.dashes > 0) TState(e, "rampWind", 0.42f); else { TState(e, "rampSlamWind", 0.75f); r.aim = V(e.pos.x, 0, e.pos.z) + fwd * 4; WarnDisc(r.aim, 9, 0.75f + 0.2f, 0xff5070); } }
                    break;
                case "rampSlamWind": face = false; if (e.stateT <= 0) { TraitorSlam(e, r.aim, 9, TD(e, 40), 6); AddRing(r.aim, 18, 30, 18, 0xc8f0ff, 0.3f); TState(e, "recover", 1.4f); } break;
                // ---- ICE SPEARS: he tears spears of ice from the air and hurls a fan of them at you ----
                case "spearWind":
                    if (rand < 0.8f) SpawnParticles(r.rHand.t.position, 1, 2, cyanMat, 0.1f, 0.4f, -1);
                    if (e.stateT <= 0)
                    {
                        var from = r.rHand.t.position + V(0, 1, 0); int n = e.phase2 ? 7 : 5; float tof = Vector3.Distance(from, ChestOf(Tgt)) / 46f;
                        var tgt = ChestOf(Tgt) + V(Tgt.vel.x, 0, Tgt.vel.z) * tof * 0.85f;
                        for (int k = 0; k < n; k++) { var d = Quaternion.Euler(0, (k - (n - 1) / 2f) * 5.5f, 0) * (tgt - from).normalized; MakeProj("ice", from, d * 46, TD(e, 14), 4); }
                        Sfx("volley"); SpawnParticles(from, 14, 6, cyanMat, 0.1f, 0.5f); TState(e, "recover", 0.6f);
                    }
                    break;
                // ---- LEAP: up onto whatever you're standing on, landing in a slam ----
                case "tleapWind":
                    if (e.stateT <= 0)
                    {
                        float tx = r.aim.x - e.pos.x, tz = r.aim.z - e.pos.z, td = Mathf.Max(0.5f, Hypot(tx, tz));
                        BallisticLeap(e, tx, tz, td, r.aim.y - e.pos.y, 5, 40); TState(e, "tleap", 3.2f); e.leftGround = false; e.ground = false; Sfx("dashGo"); Sfx("roar");
                        SpawnParticles(e.pos + V(0, 0.4f, 0), 30, 9, SnowMat, 0.3f, 0.8f); P.shake = Mathf.Max(P.shake, 0.5f);
                    }
                    break;
                case "tleap":
                    face = false; e.vel.x = e.leapVX; e.vel.z = e.leapVZ;
                    if (!e.ground) e.leftGround = true;
                    if (e.ground && e.leftGround) { TraitorSlam(e, e.pos, 7.5f, TD(e, 34), e.pos.y > 1 ? 0 : 6); TState(e, "recover", 0.7f); }
                    else if (e.stateT <= 0) TState(e, "chase", 0.5f);
                    break;
                case "recover": face = e.stateT < 0.3f; if (e.stateT <= 0) TState(e, "chase", (e.phase2 ? 0.25f : 0.45f) + rand * 0.35f); break;
            }
            // movement
            if (e.state != "frozen" && e.state != "awaken")
            {
                if (e.state != "dcharge" && e.state != "ramp" && e.state != "tleap")
                {
                    float k = 1 - Mathf.Exp(-6 * dt);
                    e.vel.x += (dx / dist * move - e.vel.x) * k; e.vel.z += (dz / dist * move - e.vel.z) * k;
                }
                e.vel.y = Mathf.Max(e.vel.y - GRAV * dt, -60);
                var res = MoveBody(ref e.pos, ref e.vel, e.r, e.h, dt);
                e.ground = res.ground;
                if ((e.state == "dcharge" || e.state == "ramp") && res.wall)
                {
                    if (e.state == "dcharge" && Vector3.Distance(e.pos, e.chargeFrom) > 5)
                    {
                        TState(e, "stunned", 2.8f); e.vel.x = e.vel.z = 0; Sfx("slam"); P.shake = Mathf.Max(P.shake, 0.9f);
                        SpawnParticles(BodyPoint(e), 40, 12, SnowMat, 0.3f, 1); IceShards(BodyPoint(e) + Fwd(e) * 2, 10, 3, 7, -Fwd(e) * 3, 1);
                    }
                    else if (e.state == "ramp") { e.stateT = 0; }
                    else TState(e, "recover", 0.5f);
                }
            }
            if (face) TurnTo(e, Mathf.Atan2(dx, dz), e.state == "chase" ? 4 : 3, dt);
            e.pos.x = clamp(e.pos.x, -(LB - 4), LB - 4); e.pos.z = clamp(e.pos.z, -(LB - 4), LB - 4);
            AnimateTraitor(e, dt);
            SyncEnt(e);
        }
        // the slam: a hit where the fists land, an ice shockwave ring you can jump, and lines of ice spikes racing out
        void TraitorSlam(Ent e, Vector3 p, float r, float dmg, int lines, bool cataclysm = false)
        {
            if (!P.dead && Hypot(P.pos.x - p.x, P.pos.z - p.z) < r && P.pos.y - p.y < 3) { DamagePlayer(dmg, p); P.vel.y = Mathf.Max(P.vel.y, 10); P.onGround = false; }
            AddRing(p, cataclysm ? 26 : 22, cataclysm ? 46 : 34, cataclysm ? 22 : 18, 0x80e0ff);
            if (cataclysm) AddRing(p, 18, 34, 18, 0xc8f0ff, 0.35f);
            float bse = Mathf.Atan2(P.pos.z - p.z, P.pos.x - p.x);
            for (int l = 0; l < lines; l++)
            {
                float a = bse + (lines == 1 ? 0 : (l / (float)lines) * Mathf.PI * 2);
                var dir = V(Mathf.Cos(a), 0, Mathf.Sin(a));
                for (int k = 1; k <= 18; k++)
                {
                    float x = p.x + dir.x * (r * 0.6f + k * 1.8f), z = p.z + dir.z * (r * 0.6f + k * 1.8f);
                    if (Mathf.Abs(x) > LB - 1 || Mathf.Abs(z) > LB - 1 || PointInLevel(V(x, 0.5f, z))) break;
                    AddSpike(x, 0, z, k * 0.05f, true, 22);
                }
            }
            P.shake = Mathf.Max(P.shake, cataclysm ? 1.1f : 0.8f); Sfx("slam"); Sfx("boom", 0.7f); ShockRing(p, r, 0xffffff, 0.35f);
            SpawnParticles(p + V(0, 0.4f, 0), 40, 12, SnowMat, 0.3f, 1.0f); SpawnParticles(p + V(0, 0.4f, 0), 30, 9, cyanMat, 0.12f, 0.7f);
        }
        void CataclysmWarn(Ent e)
        {
            var c = V(e.pos.x, 0, e.pos.z);
            WarnDisc(c, 10, 1.5f / 1.35f + 0.2f, 0xff5070);
            for (int k = 0; k < 7; k++) { float a = rand * Mathf.PI * 2, d = 12 + rand * 14; var p = c + V(Mathf.Cos(a), 0, Mathf.Sin(a)) * d; float lim = LB - 4; p.x = clamp(p.x, -lim, lim); p.z = clamp(p.z, -lim, lim); FrostPatch(p, 3.6f, 4.2f, 99); }
            if (!P.dead) FrostPatch(V(P.pos.x, 0, P.pos.z), 3.4f, 4.2f, 99); // one right under you: move!
        }

        // ================= the player frozen in ice =================
        void PrisonStart()
        {
            if (prisonT > 0 || P.dead) return;
            prisonT = 3.4f; prisonHp = 7; prisonTick = 0.4f; P.vel = Vector3.zero; ReleaseHook();
            prisonG = new GameObject("prison"); prisonG.transform.SetParent(levelRoot, false); prisonG.transform.position = P.pos;
            Mk.Msh(prisonG.transform, Geo.Box(1.8f, 2.6f, 1.8f), tIceM, 0, 1.3f, 0).layer = Mk.LayerIgnore;
            for (int k = 0; k < 7; k++) { float a = k * 0.9f; Mk.Msh(prisonG.transform, Geo.Octahedron(1), tIceSolidM, Mathf.Cos(a) * 1.0f, 0.8f + (k % 3) * 0.6f, Mathf.Sin(a) * 1.0f, Mathf.Sin(a) * 0.3f, a, 0).transform.localScale = V(0.35f, 1.1f, 0.35f); }
            foreach (var rr in prisonG.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
            Sfx("shatter"); hurtFlash = Mathf.Max(hurtFlash, 0.4f); P.shake = Mathf.Max(P.shake, 0.5f);
        }
        // true while the player is held in the ice (UpdatePlayer then ignores movement input)
        bool PrisonUpdate(float dt)
        {
            if (prisonT <= 0) return false;
            prisonT -= dt; P.vel = Vector3.zero; P.cd = Mathf.Max(P.cd, 0.12f); chillT = Mathf.Max(chillT, 0.5f);
            if (prisonG) prisonG.transform.position = P.pos;
            prisonTick -= dt; if (prisonTick <= 0) { prisonTick = 0.6f; DamagePlayer(6, null, true); }
            bool mash = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) || (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame)) || (testMode && rand < dt * 4);
            if (mash) { prisonHp -= 1; Sfx("hit", 0.7f); SpawnParticles(ChestPos, 6, 4, cyanMat, 0.1f, 0.5f); if (prisonG) prisonG.transform.localScale = Vector3.one * (1 + (5 - prisonHp) * 0.04f); }
            if (prisonHp <= 0 || prisonT <= 0 || P.dead) { PrisonBreak(true); return false; }
            return true;
        }
        void PrisonBreak(bool fx)
        {
            if (prisonT <= 0 && !prisonG) return;
            prisonT = 0;
            if (prisonG)
            {
                if (fx)
                {
                    var p = prisonG.transform.position;
                    for (int k = 0; k < 8; k++) particles.Add(new Pt { pos = p + V((rand - 0.5f) * 1.6f, 0.5f + rand * 2, (rand - 0.5f) * 1.6f), scale = Vector3.one * (0.25f + rand * 0.3f), mat = tIceSolidM, v = V((rand - 0.5f) * 9, 3 + rand * 5, (rand - 0.5f) * 9), life = 1.8f, grav = GRAV, hasSpin = true, spin = V(rand * 10, rand * 10, rand * 10) });
                    SpawnParticles(p + V(0, 1.2f, 0), 24, 7, cyanMat, 0.12f, 0.6f); Sfx("shatter"); P.iframes = Mathf.Max(P.iframes, 0.6f);
                }
                Destroy(prisonG); prisonG = null;
            }
            SetHint(null);
        }

        // ================= animation =================
        void AnimateTraitor(Ent e, float dt)
        {
            var r = e.tr; float k = 1 - Mathf.Exp(-10 * dt), hsp = Hypot(e.vel.x, e.vel.z); var st = e.state; float T = r.t;
            e.walk += dt * hsp * (st == "dcharge" || st == "ramp" ? 0.5f : 0.8f);
            float sw = Mathf.Sin(e.walk), amp = Mathf.Min(1, hsp / 4);
            e.flinch = Mathf.Max(0, e.flinch - dt * 3);
            float Prog(float dur) => 1 - clamp(e.stateT / dur, 0, 1);
            float lean = 0.2f * amp, twist = 0, headX = 0, jaw = 0.1f + Mathf.Sin(time * 5) * 0.05f;
            float rShX = -0.25f + sw * 0.35f * amp, rShZ = 0.3f, rEl = -0.45f, lShX = -0.25f - sw * 0.35f * amp, lShZ = 0.3f, lEl = -0.45f;
            float legL = -sw * 0.55f * amp, legR = sw * 0.55f * amp, kneeL = 0.15f + Mathf.Max(0, sw) * 0.8f * amp, kneeR = 0.15f + Mathf.Max(0, -sw) * 0.8f * amp, hipY = 1.35f - Mathf.Abs(Mathf.Cos(e.walk)) * 0.05f * amp;
            switch (st)
            {
                case "frozen": lean = 0.15f; headX = 0.45f; rShX = -0.6f; lShX = -0.6f; rShZ = lShZ = 0.15f; rEl = lEl = -1.3f; jaw = 0; break; // hunched, arms bound to his chest
                case "awaken":
                {
                    float a = Smoother(T / 4.2f);
                    lean = lerp(0.15f, -0.25f, a); headX = lerp(0.45f, -0.55f, a) + Mathf.Sin(T * 17) * 0.05f * a; jaw = T > 3.6f ? 0.9f : 0.1f;
                    rShX = lerp(-0.6f, -1.3f, a); lShX = rShX; rShZ = lShZ = lerp(0.15f, 1.1f, a) + Mathf.Sin(T * 9) * 0.1f * a; rEl = lEl = lerp(-1.3f, -0.3f, a);
                    if (T > 2.55f && T < 3.5f) { float p = Smoother((T - 2.55f) / 0.25f); rShX = lerp(-0.6f, -1.6f, p); rShZ = 0.15f; rEl = lerp(-1.3f, -0.05f, p); twist = 0.35f * p; lean = 0.25f; } // the punch through the ice
                    if (T > 4.1f) { rShZ = lShZ = 1.5f; rShX = lShX = -0.6f; rEl = lEl = -0.1f; lean = -0.35f; headX = -0.6f; jaw = 1; } // arms flung wide as the walls fall
                    break;
                }
                case "chase": break;
                case "cleaveWind": { float t = Prog(1.0f); rShX = lerp(-0.3f, 0.7f, t); rShZ = lerp(0.3f, 1.5f, t); rEl = -0.2f; twist = lerp(0, -0.7f, t); lean = 0.1f; jaw = 0.4f; break; }
                case "cleave": { float t = clamp(T / 0.3f, 0, 1); rShX = lerp(0.7f, -1.4f, t); rShZ = lerp(1.5f, 0.2f, t); rEl = -0.1f; twist = lerp(-0.7f, 0.8f, t); lean = 0.45f; jaw = 0.6f; break; }
                case "gslamWind": case "rampSlamWind": case "cataWind": { float t = Prog(st == "cataWind" ? 1.5f : st == "gslamWind" ? 1.1f : 0.75f); rShX = lShX = lerp(-0.3f, -3.0f, t); rShZ = lShZ = st == "cataWind" ? 0.12f : 0.4f; rEl = lEl = -0.35f; lean = lerp(0.2f, -0.35f, t); jaw = 0.7f; break; }
                case "recover":
                    if (e.last == "gslam" || e.last == "cataclysm" || e.last == "rampage") { rShX = lShX = -0.55f; rEl = lEl = -0.05f; lean = 0.85f; kneeL = kneeR = 0.7f; hipY = 1.05f; }
                    else { lean = 0.35f; }
                    break;
                case "spearWind": { float t = Prog(0.7f); rShX = lerp(-0.4f, -2.8f, t); rShZ = 0.3f; rEl = -0.6f; twist = lerp(0, -0.5f, t); lean = -0.1f; jaw = 0.5f; break; }
                case "tleapWind": { float t = Prog(0.6f); lean = 0.6f * t; kneeL = kneeR = 0.15f + 1.0f * t; legL = legR = -0.6f * t; hipY = 1.35f - 0.4f * t; rShX = lShX = 0.8f * t; jaw = 0.8f; break; }
                case "tleap": lean = -0.2f; kneeL = kneeR = 1.1f; legL = legR = -0.9f; rShX = lShX = -2.6f; rShZ = lShZ = 0.4f; jaw = 1; break;
                case "prisonWind": rShX = -1.55f; rShZ = 0.15f; rEl = -0.05f; lean = 0.1f; headX = 0.2f; jaw = 0.5f + Mathf.Sin(time * 20) * 0.1f; break;
                case "breathWind": { float t = Prog(0.95f); headX = lerp(0, -0.7f, t); jaw = lerp(0.2f, 0.6f, t); lean = lerp(0.1f, -0.35f, t); rShZ = lShZ = 0.7f; break; }
                case "breath": headX = 0.2f + Mathf.Sin(time * 25) * 0.03f; jaw = 1.0f; lean = 0.3f; rShZ = lShZ = 0.9f; rShX = lShX = 0.2f; break;
                case "icefallWind": rShX = lShX = -2.7f; rShZ = lShZ = 0.6f; rEl = lEl = -0.2f; headX = -0.5f; jaw = 0.9f; lean = -0.3f; break;
                case "dchargeWind": lean = 0.9f; headX = 0.45f; jaw = 0.6f; rShX = lShX = 0.7f; legL = Mathf.Sin(time * 16) * 0.4f; legR = -legL; break;
                case "dcharge": case "ramp": case "rampWind": lean = st == "rampWind" ? 0.8f : 1.0f; headX = 0.5f; jaw = 0.6f; rShX = 0.9f + sw * 0.3f; lShX = 0.9f - sw * 0.3f; rShZ = lShZ = 0.4f; break;
                case "stunned": lean = 1.1f; headX = 0.8f + Mathf.Sin(time * 3) * 0.2f; jaw = 0.4f; rShX = lShX = 0.3f; rEl = lEl = -0.1f; kneeL = kneeR = 0.7f; hipY = 1.05f; break;
                case "fall":
                {
                    // stagger -> kneel -> (beheaded) -> collapse forward -> lie still -> rise to a knee -> throw -> stand
                    if (T < 1.2f) { lean = -0.3f + Mathf.Sin(T * 9) * 0.25f; headX = -0.4f; jaw = 0.9f; rShZ = lShZ = 1.2f + Mathf.Sin(T * 12) * 0.3f; rShX = lShX = -0.8f; legL = Mathf.Sin(T * 8) * 0.4f; legR = -legL; }
                    else if (T < 3.0f) { float a = Smoother((T - 1.2f) / 0.8f); lean = lerp(-0.3f, 0.35f, a); headX = -0.5f; jaw = 0.8f; kneeL = kneeR = lerp(0.2f, 1.6f, a); legL = legR = lerp(0, -1.1f, a); hipY = lerp(1.35f, 0.55f, a); rShX = lShX = -0.2f; rShZ = lShZ = 0.5f; rEl = lEl = -0.2f; }
                    else if (T < 8.6f) { float a = Smoother((T - 3.0f) / 1.3f); lean = lerp(0.35f, 1.45f, a); kneeL = kneeR = 1.6f; legL = legR = -1.1f; hipY = lerp(0.55f, 0.35f, a); rShX = lShX = lerp(-0.2f, -2.6f, a); rShZ = lShZ = lerp(0.5f, 0.9f, a); rEl = lEl = -0.1f; }
                    else if (T < 14.0f)
                    {
                        float a = Smoother((T - 8.6f) / 1.2f); lean = lerp(1.45f, 0.25f, a); kneeL = 1.6f; kneeR = lerp(1.6f, 0.6f, a); legL = -1.1f; legR = lerp(-1.1f, -0.6f, a); hipY = lerp(0.35f, 0.75f, a);
                        lShX = lerp(-2.6f, -0.6f, a); lShZ = 0.5f; lEl = -0.3f;
                        float b = clamp((T - 9.8f) / 1.0f, 0, 1), c = clamp((T - 10.8f) / 0.7f, 0, 1), d = clamp((T - 11.5f) / 0.3f, 0, 1);
                        rShX = T < 9.8f ? lerp(-2.6f, -0.8f, a) : T < 11.5f ? lerp(-1.3f, -3.0f, Smoother(c)) - b * 0.2f : lerp(-3.0f, -0.6f, d); rShZ = 0.4f; rEl = T < 11.5f ? -0.6f * c : 0; twist = T < 11.5f ? -0.5f * c : lerp(-0.5f, 0.6f, d);
                    }
                    else { float a = Smoother((T - 14.0f) / 1.6f); lean = lerp(0.25f, -0.25f, a); kneeL = lerp(1.6f, 0.15f, a); kneeR = lerp(0.6f, 0.15f, a); legL = lerp(-1.1f, 0, a); legR = lerp(-0.6f, 0, a); hipY = lerp(0.75f, 1.35f, a); rShX = lShX = lerp(-0.6f, -0.3f, a); rShZ = lShZ = lerp(0.4f, 1.3f, a); }
                    break;
                }
            }
            Sm(r.torso, 0, lean - e.flinch * 0.25f, k); Sm(r.torso, 1, twist, k);
            if (!r.headless) { Sm(r.head, 0, headX - e.flinch * 0.25f, k); Sm(r.jaw, 0, jaw, k); }
            Sm(r.rSh, 0, rShX, k); Sm(r.rSh, 2, rShZ, k); Sm(r.rEl, 0, rEl, k);
            Sm(r.lSh, 0, lShX, k); Sm(r.lSh, 2, -lShZ, k); Sm(r.lEl, 0, lEl, k);
            Sm(r.thigh[0], 0, legL, k); Sm(r.thigh[1], 0, legR, k); Sm(r.knee[0], 0, kneeL, k); Sm(r.knee[1], 0, kneeR, k);
            r.hips.py += (hipY - r.hips.py) * k;
            float cg = 1.1f + Mathf.Sin(time * (e.phase2 ? 11 : 4)) * 0.25f; if (r.coreGlow) r.coreGlow.transform.localScale = Vector3.one * cg;
            if (r.headless && r.stump.activeSelf) { Mats.SetAlpha(r.stumpM, 0.6f + 0.35f * Mathf.Sin(time * 14)); if (rand < 0.2f) SpawnParticles(r.neck.t.position + V(0, 0.5f, 0), 1, 1.5f, tFrostBloodM, 0.08f, 0.5f, 2); } // it drips frost-blood
            if (st == "frozen") return;
            int em = e.flash > 0 ? 0xffffff : st == "stunned" ? 0x223344 : e.phase2 ? (Mathf.Sin(time * 9) > 0 ? 0x0a3050 : 0x041828) : 0x000000;
            foreach (var m in r.mats) Mats.Emissive(m, em);
            Mats.Emissive(r.armour, e.flash > 0 ? 0xffffff : e.phase2 ? 0x1a5a80 : 0x0c3654);
            if (e.flash <= 0) Mats.Emissive(r.skin, e.phase2 ? (Mathf.Sin(time * 9) > 0 ? 0x0c2a44 : 0x061826) : 0x060e18); // a faint cold sheen so he reads against the dark hall
        }

        // ================= the final death, and the portal reborn =================
        void UpdateTraitorDeath(BossDeath d, float dt)
        {
            var e = d.e; var r = e.tr; float T = d.t, k = 1 - Mathf.Exp(-6 * dt);
            UpdateTraitorFx(dt);
            if (T < dt * 1.5f) { ClearTraitorAttacks(); PrisonBreak(false); Banner("THE TRAITOR FALTERS", "", "#8ad8ff"); }
            var origin = r.neck.t ? r.neck.t.position : e.pos + V(0, 6, 0);
            if (e.g && e.g.activeSelf)
            {
                // stagger, frost light bursting out of the neck and the cracks
                float jit = T < 3.9f ? Mathf.Min(0.5f, 0.1f + T * 0.12f) * (T > 3.2f ? 1.6f : 1) : 0;
                e.pos = V(d.basePos.x + (rand - 0.5f) * jit, d.basePos.y, d.basePos.z + (rand - 0.5f) * jit); SyncEnt(e);
                float fz = Smoother((T - 1.4f) / 1.8f); // freezing solid
                Sm(r.torso, 0, T < 1.4f ? -0.3f + Mathf.Sin(T * 10) * 0.25f : -0.2f, k * (1 - fz * 0.9f));
                Sm(r.rSh, 2, T < 1.4f ? 1.4f + Mathf.Sin(T * 13) * 0.3f : 1.2f, k * (1 - fz * 0.9f)); Sm(r.lSh, 2, T < 1.4f ? -1.4f - Mathf.Sin(T * 11) * 0.3f : -1.2f, k * (1 - fz * 0.9f));
                Sm(r.rSh, 0, -1.0f, k); Sm(r.lSh, 0, -1.0f, k);
                // he sinks to his knees, one arm reaching up for the portal he broke, and freezes like that
                float kn = Smoother((T - 1.0f) / 0.9f), kk = k * (1 - fz * 0.85f);
                r.hips.py += (lerp(1.35f, 0.62f, kn) - r.hips.py) * kk;
                for (int i = 0; i < 2; i++) { Sm(r.thigh[i], 0, -1.15f * kn, kk); Sm(r.knee[i], 0, 1.6f * kn, kk); }
                if (T > 1.2f) { Sm(r.rSh, 0, -2.9f, kk); Sm(r.rSh, 2, 0.25f, kk); Sm(r.rEl, 0, -0.25f, kk); Sm(r.lSh, 0, -0.5f, kk); Sm(r.lSh, 2, -0.5f, kk); Sm(r.torso, 0, 0.25f, kk); }
                if (T > 1.8f && T < 3.8f && rand < 0.35f) // ice crystals growing out over the body
                {
                    var parts2 = new[] { r.torso, r.rSh, r.lSh, r.rEl, r.lEl, r.thigh[0], r.thigh[1], r.knee[0], r.knee[1], r.hips };
                    var pt = parts2[Random.Range(0, parts2.Length)];
                    if (pt != null && pt.t) { var cr = Mk.Msh(pt, Geo.Cyl(0.01f, 0.08f + rand * 0.08f, 0.3f + rand * 0.4f, 6), tIceSolidM, (rand - 0.5f) * 0.4f, -0.1f - rand * 0.4f, (rand - 0.5f) * 0.4f, (rand - 0.5f) * 2.4f, 0, (rand - 0.5f) * 2.4f); cr.layer = Mk.LayerIgnore; }
                }
                // frozen solid, cracked through, he topples forward off his knees...
                if (T >= 3.9f) { float tp = Mathf.Clamp01((T - 3.9f) / 0.6f); r.root.rx = 1.5f * tp * tp; if (T - dt < 3.9f) { Sfx("growlLow"); Sfx("shatter", 0.5f); } }
                foreach (var m in r.mats) { Mats.SetColor(m, Color.Lerp(m == r.armour ? Mats.Hex(0xb8e6ff) : m == r.horn ? Mats.Hex(0x101216) : Mats.Hex(0x3a4a62), Mats.Hex(0xe8f6ff), fz)); Mats.Emissive(m, Mathf.Sin(T * (8 + T * 6)) > 0 ? 0x2a80b0 : 0x082840, 0.4f + fz); }
                if (T < 3.6f && rand < 0.5f)
                {
                    var dir = Random.onUnitSphere; dir.y = Mathf.Abs(dir.y) * 0.8f + 0.2f;
                    var m2 = Mats.Add(0xc8f6ff, 0.8f, null, true, true); var ray = Mk.Msh(world, Geo.Cyl(1, 0.1f, 1, 8), m2); ray.layer = Mk.LayerIgnore;
                    ray.transform.position = origin + dir * 6; ray.transform.rotation = Quaternion.FromToRotation(UP, dir); ray.transform.localScale = V(0.5f + rand * 0.6f, 12, 0.5f + rand * 0.6f);
                    fx.Add(new Fx { m = ray.transform, mat = m2, life = 0.5f, max = 0.5f });
                    Sfx("godRay" + Random.Range(0, 4), 0.5f);
                }
                if (T < 3.9f && rand < 0.6f) SpawnParticles(origin, 3, 8, cyanMat, 0.12f, 0.9f, -2);
                if (T >= 1.4f && T - dt < 1.4f) Sfx("charge");
                if (T > 1.6f && T < 3.6f && rand < 0.25f) // cracks spreading over the frozen body
                {
                    var parts = new[] { r.torso, r.rSh, r.lSh, r.rEl, r.lEl, r.thigh[0], r.thigh[1], r.hips };
                    var pt = parts[Random.Range(0, parts.Length)]; if (pt != null && pt.t) { var c = Mk.Msh(pt, Geo.Box(0.05f, 0.4f + rand * 0.5f, 0.05f), r.frost, (rand - 0.5f) * 0.5f, -0.2f - rand * 0.4f, 0.2f, rand - 0.5f, 0, (rand - 0.5f) * 2); c.layer = Mk.LayerIgnore; }
                    Sfx("hit", 0.4f);
                }
                if (T >= 3.2f && T - dt < 3.2f) { Sfx("shatter"); P.shake = Mathf.Max(P.shake, 0.6f); }
                P.shake = Mathf.Max(P.shake, 0.15f + T * 0.08f);
                if (T >= 4.5f)
                {
                    // ...and hits the floor and bursts into frozen pieces in a storm of ice
                    IceShards(BodyPoint(e), 30, 6, 10, Fwd(e) * 4, 2.2f); IceShards(e.pos + Fwd(e) * 5 + V(0, 1, 0), 20, 5, 7, Vector3.zero, 1.6f);
                    var c = BodyPoint(e);
                    Dismember(new List<N> { r.rSh, r.lSh, r.thigh[0], r.thigh[1], r.torso }, e, d.info, 5);
                    e.g.SetActive(false);
                    for (int i = 0; i < 40; i++) { var p = c + V((rand - 0.5f) * 6, (rand - 0.3f) * 10, (rand - 0.5f) * 6); var ch = Mk.Msh(world, Geo.Box(0.6f + rand * 1.4f, 0.6f + rand, 0.6f + rand * 1.2f), rand < 0.5f ? tIceSolidM : tIceM, p.x, p.y, p.z, rand, rand, rand); ch.layer = Mk.LayerIgnore; var o = p - c; o.y = 0; particles.Add(new Pt { m = ch.transform, v = o.normalized * (4 + rand * 8) + V(0, 2 + rand * 8, 0), life = 5 + rand * 2, grav = GRAV, hasSpin = true, spin = V(rand * 8 - 4, rand * 8 - 4, rand * 8 - 4), owned = true }); }
                    SpawnParticles(c, 90, 16, cyanMat, 0.16f, 2); SpawnParticles(c, 60, 10, SnowMat, 0.3f, 2.5f, 2);
                    Blast(c, 18, 0xc8f0ff); Blast(c, 8, 0xffffff); for (int i = 0; i < 3; i++) ShockRing(d.basePos, 14 + i * 10, i == 1 ? 0xffffff : 0x80e0ff, 0.7f + i * 0.3f);
                    FirePillar(d.basePos, 3, 90, 0xc8f0ff, 1.6f);
                    Sfx("bossDeath"); Sfx("boom"); Sfx("shatter"); whiteFlash = 0.6f; P.shake = 1.6f; freeze = 0.15f;
                    Banner("THE TRAITOR SHATTERS", "THE THRONE OF FROST IS EMPTY", "#8ad8ff");
                }
                return;
            }
            // the frost energy he released flows into the broken portal: the pieces glow, rise, fly home and lock together
            float k2 = clamp((T - 4.5f) / 1.2f, 0, 1);
            Mats.Emissive(tPieceM, THEME.portal, 1 + k2 * 4 + (T > 6.8f ? Mathf.Sin(T * 30) * 1.5f : 0));
            if (T > 4.5f && T < 7.0f && tPieces.Count > 0 && rand < 0.9f)
            {
                var pc = tPieces[Random.Range(0, tPieces.Count)]; if (pc.t) { var src = d.basePos + V((rand - 0.5f) * 4, 1 + rand * 4, (rand - 0.5f) * 4); particles.Add(new Pt { pos = src, scale = Vector3.one * 0.16f, mat = cyanMat, v = (pc.t.position - src) * 1.6f, life = 0.62f, grav = 0 }); }
            }
            if (T >= 4.5f && T - dt < 4.5f) { Sfx("portalCharge"); Banner("THE PORTAL STIRS", "HIS DEATH BREAKS THE SEAL", "#8ad8ff"); }
            foreach (var pc in tPieces)
            {
                if (!pc.t) continue;
                if (!pc.rest) { pc.rest = true; pc.restPos = pc.t.position; pc.restRot = pc.t.rotation; }
                if (T < 5.4f) continue;
                float lift = Smoother((T - 5.4f) / 1.4f), home = Smoother((T - 6.8f) / 1.2f);
                var up = pc.restPos + V(0, lift * 1.6f + Mathf.Sin(T * 6 + pc.home.x) * 0.15f * lift, 0);
                pc.t.position = Vector3.Lerp(up, pc.home, home);
                pc.t.rotation = Quaternion.Slerp(pc.restRot * Quaternion.Euler(0, T * 60 * lift * (1 - home), 0), pc.homeRot, home);
                if (home < 1 && rand < 0.05f) SpawnParticles(pc.t.position, 1, 1, cyanMat, 0.1f, 0.4f);
            }
            if (T >= 8.0f && T - dt < 8.0f)
            {
                // reassembled: a flash, and the crystal that holds the way on shines again
                foreach (var pc in tPieces) if (pc.t) Destroy(pc.t.gameObject); tPieces.Clear();
                var home = tPortalBroken ? tPortalHome : LEVEL.center + V(0, 2.6f, 0);
                Blast(home, 8, 0xc8f0ff); Blast(home, 4, 0xffffff); ShockRing(V(home.x, 0, home.z), 16, THEME.portal, 0.7f); SpawnParticles(home, 50, 12, cyanMat, 0.14f, 1.2f);
                whiteFlash = Mathf.Max(whiteFlash, 0.4f); P.shake = Mathf.Max(P.shake, 0.6f); Sfx("portalOpen");
                if (crystal)
                {
                    crystal.gameObject.SetActive(true); crystalRing.gameObject.SetActive(true); crystalGlow.gameObject.SetActive(true);
                    crystal.localScale = Vector3.one; var cp = crystal.localPosition; cp.y = LEVEL.center.y + 7; crystal.localPosition = cp;
                    crystalRing.localScale = Vector3.one; crystalGlow.localScale = Vector3.one * 10;
                }
                Banner("THE PORTAL AWAKENS", "THE WAY ON IS OPEN", "#8ad8ff");
            }
            if (T >= 8.6f) bossDeath = null; // the wave clears now: the crystal tears open into the real portal
        }
            // an attack's setup (warnings, aim, sounds) - run by the host when it picks it, and by each client when the
        // host's pick arrives, so everyone sees the same attack aimed at the same player
        void TraitorBegin(Ent e, string a)
        {
            var r = e.tr; if (r == null) return; Tgt = TargetOf(e, 0); float sp = e.phase2 ? 1.45f : 1.1f; var fwd = Fwd(e);
            float dx = Tgt.pos.x - e.pos.x, dz = Tgt.pos.z - e.pos.z, dist = Hypot(dx, dz), dy = Tgt.pos.y - e.pos.y; if (dist == 0) dist = 1;
switch (a)
            {
                case "cleave": r.combo = 0; TState(e, "cleaveWind", 0.9f); tCleaveWarn = WarnFan(e.pos, e.yaw, 15, 1.35f, 0.9f / sp + 0.25f, 0xff6060); Sfx("growlLow"); break;
                case "spears": TState(e, "spearWind", 0.7f); Sfx("laserCharge"); break;
                case "gslam": { TState(e, "gslamWind", 1.1f); var to = V(dx, 0, dz); var p = e.pos + (dist > 10 ? to / dist * 10 : to); r.aim = V(p.x, 0, p.z); WarnDisc(r.aim, 7, 1.1f / sp + 0.2f, 0xff7060); Sfx("growlLow"); break; }
                case "tleap": r.aim = Tgt.pos; TState(e, "tleapWind", 0.6f); WarnDisc(V(Tgt.pos.x, Tgt.pos.y, Tgt.pos.z), 6.5f, 0.6f / sp + 1.1f, 0xff7060); Sfx("roarShort"); break;
                case "awaken": if (e.state == "frozen") { TState(e, "awaken", 5.0f); SetHint(null); Sfx("charge"); Sfx("growlLow"); } break;
                case "prison": TState(e, "prisonWind", 1.4f); r.aim = V(Tgt.pos.x, 0, Tgt.pos.z); tPrisonWarn = WarnDisc(r.aim, 3.2f, 99, 0xb0f0ff); tPrisonWarn.keep = true; Sfx("charge"); break;
                case "breath": TState(e, "breathWind", 0.95f); r.aimYaw = Mathf.Atan2(dx, dz); Sfx("growlLow"); break;
                case "icefall": TState(e, "icefallWind", 0.85f); Sfx("roarShort"); break;
                case "dcharge": TState(e, "dchargeWind", 1.1f); Sfx("roarShort"); break;
                case "cataclysm": TState(e, "cataWind", 1.5f); CataclysmWarn(e); Sfx("laserCharge"); break;
                case "rampage": e.dashes = e.hp < e.maxHp * 0.5f ? 4 : 3; TState(e, "rampWind", 0.6f); Sfx("roarShort"); break;
            }
        }
    }
}
