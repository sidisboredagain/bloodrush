using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    public class Ent
    {
        public string type, state = "", last = "", name; public int boss;
        public bool brute, holy, heavy, dead, invuln, charging, leaping, ground, roared, phase2, chained, hitPlayer, leftGround, triedVault;
        public bool risen; // the Carnifex's intro: fully out of the ground
        public bool execDone, scaled; public Transform headT; // a boss executed during this stun (cleared once it's back up); the head bone (Deadeye aims at it)
        // the act enemies (see GameKinds): which kind, and the state their behaviours keep
        public string kind = ""; public GameObject shield; public float shieldHp, tpT, fuse, timeSlowT, hasteT, regenT, ringT; public bool exploded;
        public Transform laser; public Material laserM;
        public Vector3 anchor, anchorN; public bool anchored; // Rime Spitter: the wall spot it clings to, and the wall's normal
        public HollowRig hk; public TraitorRig tr; public WardenRig wd;
        public Transform lantern; public List<GameObject> links, bubbles; public bool emerging; public Vector3 emergeDir; // Purgatory: the pilgrim's lantern and its threads; a Mirror Twin stepping out of a mirror // boss 3, the Frozen Traitor (GameTraitor)
        public float s = 1, hp, maxHp, headY, bodyY, hitR, r, h, speed, dmg, windup, attackT, strikeT, cd, stun, flash, flinch, walk, leapCd, spawnT,
            shootT, sign, phase, blink, stateT, sfxT, orbit, groundY, blockT, sideT, leapVX, leapVZ, yaw, pitch, gScale = 1, stuckT, stagT, stagHitT = -99; public int stagHits;
        public Vector3 stuckFrom;
        public int sideSign, vidx, dashes;
        public GameObject g; public Vector3 pos, vel, chargeDir, dashDir, beamDir = new Vector3(0, 0, 1), leapFrom, chargeFrom; public bool hasChargeFrom;
        public HuskRig hr; public DroneRig dr; public CarnRig cr; public SerRig sr;
        public List<Material> mats = new List<Material>();
        // co-op: network id, the player it's after (see TargetOf), smoothing of host corrections (GameNet.cs)
        public ushort nid; public Player tgt; public float tgtT, netMismatchT, heldT; public Vector3 netErr;
        public string netAct = ""; public byte netActSeq, netActSeen; public float coopK; // co-op: the attack the host last picked (bosses), and how many it has picked
    }
    public class HuskRig { public N root, hips, torso, neck, head, jaw; public N[] thigh = new N[2], knee = new N[2], sh = new N[2], elbow = new N[2], hand = new N[2]; public Material glow; public System.Action<Ent, float> extra; public bool frost, ruin; }
    public class DroneRig { public N root, lidTop, lidBot, ring1, ring2; public List<List<N>> tentacles = new List<List<N>>(); public List<N> wings = new List<N>(); public List<float> wingS = new List<float>(); public GameObject iris, eyeGlow, jet; public Material irisMat, eyeGlowMat; public bool holy; public N[] extraHands; public N bell; public System.Action<Ent, float> extra; }

    public partial class Game
    {
        readonly List<Ent> enemies = new List<Ent>();
        readonly List<Material> pendingMats = new List<Material>(); // enemy materials, freed when the level is cleared
        Ent boss;
        Material eprojMat, friendlyMat, bileMat, bladeMat;

        Material EM(Ent e, Material m) { e.mats.Add(m); pendingMats.Add(m); return m; }
        GameObject Gl(N p, int color, float size, float op, bool fog = true)
        {
            var g = Mk.Glow(p.t, color, size, op, fog); pendingMats.Add(g.GetComponent<Renderer>().sharedMaterial); return g;
        }
        Ent NewEnt(string type)
        {
            var e = new Ent { type = type };
            e.g = new GameObject(type); e.g.transform.SetParent(world, false);
            var rb = e.g.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
            Mk.colliders = true; Mk.layer = Mk.LayerEnemy;
            return e;
        }
        // healing from blood and kills, with diminishing returns: the more you've healed in the last few seconds the less each
        // new heal gives (so mowing through a big swarm can't keep you topped up for free); the meter drains 25 hp/s
        float recentHeal, recentHealT;
        // close enough to a target for its blood to heal you: an ordinary enemy within 7 m; a boss within reach of its bulk
        // (the Warden, the Hollow King and the Traitor are measured from their bodies, not their feet; the Seraph flies, so it's
        // a sphere round her - close under her or level with her, not anywhere in the sanctum)
        bool InHealRange(Ent e)
        {
            if (e.type != "boss") return Vector3.Distance(P.pos, e.pos) < 7;
            if (e.boss == 2) return Vector3.Distance(ChestPos, BodyPoint(e)) < 28;
            return Hypot(P.pos.x - e.pos.x, P.pos.z - e.pos.z) < 10 + e.hitR * 2.5f && Mathf.Abs(P.pos.y - e.pos.y) < 6 + e.headY;
        }
        // blood heals only up close: a hit from outside the range heals nothing, whatever the weapon and whatever the boss
        float HealScale(Ent e) => InHealRange(e) ? 1 : 0;
        // what one hit is worth: on a boss 12 hp per point of damage (up to 10), at least 1.5 a hit - a per-pellet floor of 5
        // used to let one shotgun blast or an SMG burst on the armoured Seraph fill the falloff meter, so the sniper shot after
        // it healed only a few hp
        float HitHeal(Ent e, float dmg) => (e.type == "boss" ? Mathf.Max(1.5f, Mathf.Min(dmg, 10) * 12) : Mathf.Min(dmg, 10) * 6) * HealScale(e);
        void CombatHeal(float n, bool boss = false)
        {
            if (n <= 0) return;
            recentHeal = Mathf.Max(0, recentHeal - (time - recentHealT) * 25); recentHealT = time;
            float k = 1 / (1 + recentHeal / (boss ? 110 : 55)); // (a boss fight is long: its returns fall off half as fast)
            if (n >= 60) k = Mathf.Max(k, 0.5f); // a single heavy hit (the sniper) comes once a second or so: it always keeps half its worth
            Heal(n * k); recentHeal += n * k;
        }
        // progressive scaling per level: enemies get a little tougher each level (2.5% hp/level), bosses a little too (1.5%/level,
        // at most +25%); damage dealt to the player rises 1.2%/level (DamagePlayer)
        float LevelHpK(Ent e) => e.type == "boss" ? 1 + Mathf.Min(0.25f, 0.015f * (levelNum - 1)) : (1 + 0.025f * (levelNum - 1)) * CoopEnemyHp; // (co-op: a touch tougher per extra player)
        void FinishEnt(Ent e)
        {
            if (!e.scaled) { e.scaled = true; float k = LevelHpK(e); e.hp *= k; if (e.maxHp > 0) e.maxHp *= k; }
            Mk.colliders = false; Mk.layer = 0;
            foreach (var p in e.g.GetComponentsInChildren<Part>(true)) p.e = e;
            foreach (var r in e.g.GetComponentsInChildren<Renderer>(true)) if (r.gameObject.layer != Mk.LayerEnemy && r.gameObject.layer != Mk.LayerIgnore) r.gameObject.layer = Mk.LayerEnemy;
            enemies.Add(e);
            NetOnSpawn(e); // co-op host: give it an id and tell the clients
        }
        void SyncEnt(Ent e)
        {
            e.g.transform.localPosition = e.pos;
            e.g.transform.localRotation = Quaternion.Euler(e.pitch * Mathf.Rad2Deg, e.yaw * Mathf.Rad2Deg, 0);
        }

        // ---------- Husk / Brute (white-robed Zealots and armoured Templars in heaven) ----------
        HuskRig BuildHusk(Ent e, bool brute, bool holy)
        {
            if (holy) return brute ? BuildTemplar(e) : BuildZealot(e);
            var skin = EM(e, Mats.Std(holy ? (brute ? 0xdcdce6 : 0xece6d8) : (brute ? 0x4a3a40 : 0x8e3426), holy ? 0.55f : 0.75f, holy && brute ? 0.45f : 0));
            var bone = EM(e, Mats.Std(holy ? 0xd8a828 : (brute ? 0x7a7c88 : 0xd8c8a8), holy ? 0.3f : 0.55f, holy || brute ? 0.55f : 0));
            var dark = EM(e, Mats.Std(holy ? 0x34346e : 0x2a1212, 0.85f));
            var mask = holy ? EM(e, Mats.Std(0xf6f2ea, 0.25f)) : bone;
            var glow = EM(e, Mats.Basic(holy ? 0x80e8ff : (brute ? 0xffaa00 : 0xff2a00)));
            var r = new HuskRig { glow = glow };
            r.root = Mk.Piv(e.g.transform);
            r.hips = Mk.Piv(r.root, 0, 1.0f, 0);
            Mk.Msh(r.hips, Geo.Box(0.4f, 0.18f, 0.24f), dark);
            if (holy) Mk.Msh(r.hips, Geo.OpenCone(0.42f, 0.95f, 10), skin, 0, -0.38f, 0);
            else { Mk.Msh(r.hips, Geo.Box(0.44f, 0.22f, 0.06f), dark, 0, -0.12f, 0.1f, 0.1f); Mk.Msh(r.hips, Geo.Box(0.44f, 0.2f, 0.06f), dark, 0, -0.12f, -0.1f, -0.1f); }
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var thigh = Mk.Piv(r.hips, 0.13f * sx, -0.06f, 0);
                Mk.Msh(thigh, Geo.Box(0.15f, 0.48f, 0.17f), skin, 0, -0.24f, 0);
                var knee = Mk.Piv(thigh, 0, -0.48f, 0);
                Mk.Msh(knee, Geo.Box(0.12f, 0.46f, 0.13f), holy ? dark : skin, 0, -0.23f, 0);
                Mk.Msh(knee, Geo.Box(0.13f, 0.11f, 0.06f), bone, 0, -0.02f, 0.08f);
                Mk.Msh(knee, Geo.Box(0.14f, 0.07f, 0.26f), bone, 0, -0.46f, 0.05f);
                r.thigh[i] = thigh; r.knee[i] = knee;
            }
            r.torso = Mk.Piv(r.hips, 0, 0.06f, 0);
            Mk.Msh(r.torso, Geo.Box(0.44f, 0.5f, 0.27f), skin, 0, 0.32f, 0);
            Mk.Msh(r.torso, Geo.Box(0.58f, 0.12f, 0.24f), skin, 0, 0.56f, 0);
            if (holy)
            {
                Mk.Msh(r.torso, Geo.Box(0.06f, 0.42f, 0.02f), bone, 0, 0.32f, 0.142f); Mk.Msh(r.torso, Geo.Box(0.26f, 0.06f, 0.02f), bone, 0, 0.42f, 0.142f);
                Mk.Msh(r.torso, Geo.Box(0.12f, 0.6f, 0.02f), dark, 0, 0.3f, 0.143f);
            }
            else
            {
                for (int i = 0; i < 3; i++) Mk.Msh(r.torso, Geo.Box(0.36f - i * 0.04f, 0.025f, 0.02f), bone, 0, 0.42f - i * 0.09f, 0.14f);
                Mk.Msh(r.torso, Geo.Box(0.05f, 0.3f, 0.02f), bone, 0, 0.32f, 0.142f);
                for (int i = 0; i < 4; i++) Mk.Msh(r.torso, Geo.Cone(0.035f, 0.12f, 4), bone, 0, 0.18f + i * 0.12f, -0.15f, -1.9f);
            }
            var core = Mk.Msh(r.torso, Geo.Sphere(0.06f, 8, 6), glow, 0, holy ? 0.48f : 0.3f, 0.13f);
            r.neck = Mk.Piv(r.torso, 0, 0.62f, 0.02f);
            Mk.Msh(r.neck, Geo.Box(0.1f, 0.12f, 0.1f), skin, 0, 0.04f, 0);
            r.head = Mk.Piv(r.neck, 0, 0.12f, 0.02f);
            var skull = Mk.Msh(r.head, Geo.Sphere(0.2f, 12, 10), mask, 0, 0.08f, 0); skull.transform.localScale = V(1, 1.12f, 1.05f);
            var brow = Mk.Msh(r.head, Geo.Box(0.3f, 0.06f, 0.1f), bone, 0, 0.14f, 0.13f, 0.25f);
            var eye0 = Mk.Msh(r.head, Geo.Box(0.07f, 0.035f, 0.03f), glow, -0.075f, 0.095f, 0.19f);
            var eye1 = Mk.Msh(r.head, Geo.Box(0.07f, 0.035f, 0.03f), glow, 0.075f, 0.095f, 0.19f);
            r.jaw = Mk.Piv(r.head, 0, 0.0f, 0.03f);
            var jawM = Mk.Msh(r.jaw, Geo.Box(0.22f, 0.06f, 0.2f), mask, 0, -0.03f, 0.05f);
            var teeth = Mk.Msh(r.jaw, Geo.Box(0.2f, 0.03f, 0.02f), holy ? bone : glow, 0, 0.01f, 0.14f);
            Mk.Head(skull, brow, jawM, teeth, eye0, eye1);
            if (!brute)
            {
                if (holy)
                {
                    Mk.Head(Mk.Msh(r.head, Geo.Cone(0.27f, 0.5f, 8), skin, 0, 0.2f, -0.05f));
                    Mk.Head(Mk.Msh(r.head, Geo.Torus(0.2f, 0.018f, 20), glow, 0, 0.5f, -0.06f, Mathf.PI / 2));
                }
                else foreach (var sx in new[] { -1, 1 }) Mk.Head(Mk.Msh(r.head, Geo.Cone(0.035f, 0.16f, 5), bone, 0.12f * sx, 0.24f, -0.02f, -0.4f, 0, -0.5f * sx));
            }
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var sh = Mk.Piv(r.torso, 0.33f * sx, 0.55f, 0);
                Mk.Msh(sh, Geo.Box(0.13f, 0.42f, 0.13f), skin, 0, -0.2f, 0);
                var elbow = Mk.Piv(sh, 0, -0.42f, 0);
                Mk.Msh(elbow, Geo.Box(0.11f, 0.4f, 0.11f), skin, 0, -0.2f, 0);
                Mk.Msh(elbow, Geo.Box(0.13f, 0.12f, 0.13f), holy ? bone : dark, 0, -0.32f, 0);
                var hand = Mk.Piv(elbow, 0, -0.42f, 0);
                for (int k = -1; k <= 1; k++) Mk.Msh(hand, Geo.Cone(holy ? 0.026f : 0.022f, holy ? 0.36f : 0.24f, 4), bone, k * 0.04f, holy ? -0.16f : -0.1f, 0.03f, Mathf.PI + 0.25f, 0, 0);
                r.sh[i] = sh; r.elbow[i] = elbow; r.hand[i] = hand;
            }
            if (brute)
            {
                if (holy)
                {
                    var helm = Mk.Msh(r.head, Geo.Box(0.38f, 0.32f, 0.36f), skin, 0, 0.1f, 0);
                    var mitre = Mk.Msh(r.head, Geo.Cone(0.24f, 0.62f, 4), skin, 0, 0.55f, 0, 0, Mathf.PI / 4);
                    var band = Mk.Msh(r.head, Geo.Box(0.4f, 0.06f, 0.38f), bone, 0, 0.27f, 0);
                    var vh = Mk.Msh(r.head, Geo.Box(0.28f, 0.04f, 0.02f), glow, 0, 0.1f, 0.185f); var vv = Mk.Msh(r.head, Geo.Box(0.04f, 0.2f, 0.02f), glow, 0, 0.1f, 0.186f);
                    Mk.Head(helm, mitre, band, vh, vv);
                    Mk.Msh(r.torso, Geo.Box(0.5f, 0.36f, 0.08f), skin, 0, 0.36f, 0.15f);
                    Mk.Msh(r.torso, Geo.Box(0.07f, 0.3f, 0.02f), bone, 0, 0.36f, 0.2f); Mk.Msh(r.torso, Geo.Box(0.24f, 0.07f, 0.02f), bone, 0, 0.42f, 0.2f);
                }
                else
                {
                    var helm = Mk.Msh(r.head, Geo.Box(0.38f, 0.32f, 0.36f), bone, 0, 0.1f, 0);
                    var visor = Mk.Msh(r.head, Geo.Box(0.3f, 0.04f, 0.02f), glow, 0, 0.1f, 0.185f);
                    var hornL = Mk.Msh(r.head, Geo.Cone(0.06f, 0.36f, 6), dark, -0.22f, 0.28f, 0, 0, 0, 0.9f);
                    var hornR = Mk.Msh(r.head, Geo.Cone(0.06f, 0.36f, 6), dark, 0.22f, 0.28f, 0, 0, 0, -0.9f);
                    Mk.Head(helm, visor, hornL, hornR);
                    Mk.Msh(r.torso, Geo.Box(0.5f, 0.36f, 0.08f), bone, 0, 0.36f, 0.15f);
                }
                core.transform.localScale = V(1.6f, 1.6f, 1.6f); var cp = core.transform.localPosition; cp.z = 0.2f; core.transform.localPosition = cp;
                for (int i = 0; i < 2; i++)
                {
                    int sx = i == 0 ? -1 : 1;
                    Mk.Msh(r.sh[i], Geo.Box(0.32f, 0.16f, 0.32f), holy ? skin : bone, 0.04f * sx, 0.03f, 0);
                    for (int k = 0; k < 3; k++) Mk.Msh(r.sh[i], Geo.Cone(0.04f, 0.2f, 5), holy ? bone : dark, 0.04f * sx + (k - 1) * 0.08f, 0.18f, 0);
                }
                var hammer = r.hand[1];
                if (holy)
                {
                    Mk.Msh(hammer, Geo.Cyl(0.04f, 0.04f, 0.3f, 6), bone, 0, -0.1f, 0);
                    Mk.Msh(hammer, Geo.Sphere(0.22f, 10, 8), bone, 0, -0.38f, 0);
                    for (int k = 0; k < 6; k++) { float a = k * Mathf.PI / 3; Mk.Msh(hammer, Geo.Box(0.05f, 0.3f, 0.16f), bone, Mathf.Cos(a) * 0.2f, -0.38f, Mathf.Sin(a) * 0.2f, 0, -a, 0); }
                    Mk.Msh(hammer, Geo.Sphere(0.1f, 8, 6), glow, 0, -0.38f, 0.2f);
                }
                else
                {
                    Mk.Msh(hammer, Geo.Box(0.34f, 0.3f, 0.42f), bone, 0, -0.17f, 0);
                    Mk.Msh(hammer, Geo.Box(0.36f, 0.06f, 0.44f), glow, 0, -0.17f, 0);
                    for (int k = 0; k < 4; k++) Mk.Msh(hammer, Geo.Cone(0.04f, 0.14f, 4), dark, (k % 2 - 0.5f) * 0.18f, -0.35f, (k / 2 - 0.5f) * 0.24f, Mathf.PI);
                }
            }
            e.mats.Clear(); e.mats.Add(skin); e.mats.Add(bone); e.mats.Add(dark); if (holy) e.mats.Add(mask);
            return r;
        }

        // ---------- Watcher drone (a winged, haloed Ophan in heaven) ----------
        DroneRig BuildDrone(Ent e, bool holy)
        {
            if (holy) return BuildOphan(e);
            var metal = EM(e, Mats.Std(holy ? 0xf2eee6 : 0x4a3a5e, 0.35f, holy ? 0.15f : 0.6f));
            var plate = EM(e, Mats.Std(holy ? 0xd8a828 : 0x8a8090, 0.35f, 0.65f, holy ? 0x2a1800 : 0));
            var sclera = EM(e, Mats.Std(0xe8dcc8, 0.4f, 0, 0x302018));
            var irisMat = EM(e, Mats.Basic(holy ? 0x60d8ff : 0xff8800));
            var glow = EM(e, Mats.Basic(holy ? 0xc8f4ff : 0xff6600));
            var pupilM = EM(e, Mats.Basic(0x000000));
            var r = new DroneRig { holy = holy, irisMat = irisMat };
            r.root = Mk.Piv(e.g.transform);
            var eyeball = Mk.Msh(r.root, Geo.Sphere(0.45f, 16, 12), sclera);
            r.iris = Mk.Msh(r.root, Geo.Sphere(0.2f, 12, 8), irisMat, 0, 0, 0.36f); r.iris.transform.localScale = V(1, 1, 0.4f);
            var pupil = Mk.Msh(r.root, Geo.Sphere(0.09f, 8, 6), pupilM, 0, 0, 0.44f); pupil.transform.localScale = V(1, 1, 0.4f);
            Mk.Head(eyeball, r.iris, pupil);
            var shell = Geo.Sphere(0.58f, 16, 8, 0, Mathf.PI * 2, 0, Mathf.PI / 2);
            r.lidTop = Mk.Piv(r.root); Mk.Msh(r.lidTop, shell, metal); Mk.Msh(r.lidTop, Geo.Box(0.9f, 0.04f, 0.06f), plate, 0, 0.02f, 0.54f);
            r.lidBot = Mk.Piv(r.root); Mk.Msh(r.lidBot, shell, metal, 0, 0, 0, Mathf.PI); Mk.Msh(r.lidBot, Geo.Box(0.9f, 0.04f, 0.06f), plate, 0, -0.02f, 0.54f);
            foreach (var sx in new[] { -1, 1 }) Mk.Msh(r.root, Geo.Cyl(0.12f, 0.12f, 0.12f, 10), plate, 0.6f * sx, 0, 0, 0, 0, Mathf.PI / 2);
            r.ring1 = Mk.Piv(r.root); r.ring2 = Mk.Piv(r.root);
            Mk.Msh(r.ring1, Geo.Torus(0.85f, 0.035f, 32), plate, 0, 0, 0, Mathf.PI / 2);
            Mk.Msh(r.ring2, Geo.Torus(1.0f, 0.03f, 32), plate);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI / 2;
                Mk.Msh(r.ring1, Geo.Box(0.1f, 0.06f, 0.1f), glow, Mathf.Cos(a) * 0.85f, 0, Mathf.Sin(a) * 0.85f);
                Mk.Msh(r.ring2, Geo.Cone(0.05f, 0.18f, 4), plate, Mathf.Cos(a) * 1.08f, Mathf.Sin(a) * 1.08f, 0, 0, 0, a - Mathf.PI / 2);
            }
            if (holy)
            {
                foreach (var s in new[] { -1, 1 })
                {
                    var w = Mk.Piv(r.root, 0.5f * s, 0.15f, -0.15f);
                    for (int f = 0; f < 4; f++)
                    {
                        float len = 0.9f - f * 0.12f, ang = 0.4f + f * 0.25f;
                        Mk.Msh(w, Geo.Box(0.14f, len, 0.02f), metal, s * (0.1f + Mathf.Sin(ang) * len / 2), Mathf.Cos(ang) * len / 2, -0.02f * f, 0, 0, -s * ang);
                        Mk.Msh(w, Geo.Box(0.03f, len * 1.05f, 0.025f), glow, s * (0.1f + Mathf.Sin(ang) * len / 2), Mathf.Cos(ang) * len / 2, -0.02f * f - 0.01f, 0, 0, -s * ang);
                    }
                    r.wings.Add(w); r.wingS.Add(s);
                }
                Mk.Msh(r.root, Geo.Torus(0.42f, 0.03f, 24), glow, 0, 0.9f, 0, Mathf.PI / 2);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    float a = i * Mathf.PI / 2 + Mathf.PI / 4;
                    var p = Mk.Piv(r.root, Mathf.Cos(a) * 0.3f, -0.45f, Mathf.Sin(a) * 0.3f); var segs = new List<N> { p };
                    for (int j = 0; j < 4; j++)
                    {
                        Mk.Msh(p, Geo.Box(0.07f - j * 0.012f, 0.24f, 0.07f - j * 0.012f), metal, 0, -0.12f, 0);
                        p = Mk.Piv(p, 0, -0.24f, 0); segs.Add(p);
                    }
                    Mk.Msh(p, Geo.Cone(0.04f, 0.18f, 4), glow, 0, -0.06f, 0, Mathf.PI);
                    r.tentacles.Add(segs);
                }
            }
            Mk.Msh(r.root, Geo.Cone(0.18f, 0.25f, 8), metal, 0, -0.55f, -0.1f, Mathf.PI);
            r.jet = Gl(r.root, holy ? 0xc8f0ff : 0xff6020, 0.7f, 0.8f); r.jet.transform.localPosition = V(0, -0.75f, -0.1f);
            r.eyeGlow = Gl(r.root, holy ? 0x60d8ff : 0xff8800, 0.8f, 0.6f); r.eyeGlow.transform.localPosition = V(0, 0, 0.55f);
            r.eyeGlowMat = r.eyeGlow.GetComponent<Renderer>().sharedMaterial;
            e.mats.Clear(); e.mats.Add(metal); e.mats.Add(plate);
            return r;
        }

        // ---------- spawning ----------
        int Difficulty => wave < 5 ? wave : wave - 1;
        Ent SpawnHusk(float x, float z, bool brute, string kind = null)
        {
            bool holy = HolyLevel;
            kind ??= RollHuskKind(brute);
            bool packMate = kind == "scavpack"; if (packMate) kind = "scav";
            var e = NewEnt("husk"); float s = brute ? 1.65f : kind == "scav" ? 0.82f : 1;
            e.hr = (kind == "frost" || kind == "glacier") && Act == "frost" ? BuildFrostHusk(e, brute) // the Frozen Circle's own creatures
                : Act == "ruin" && (kind == "rust" || kind == "scav" || kind == "bomber" || kind == "tank") ? BuildRuinHusk(e, brute, kind) // Dead Earth's (GameRuinMobs)
                : Act == "limbo" && (kind == "hollow" || kind == "twin" || kind == "pilgrim") ? BuildLimboHusk(e, brute, kind) // Purgatory's (GamePurgatoryMobs)
                : BuildHusk(e, brute, holy);
            e.g.transform.localScale = V(s, s, s);
            e.brute = brute; e.holy = holy; e.heavy = brute; e.s = s; e.pos = V(x, 0, z); e.hp = brute ? 14 : 3;
            e.headY = 1.85f * s; e.bodyY = 1.2f * s; e.hitR = 0.9f * s;
            e.speed = Mathf.Min(10, (brute ? 4.8f : 7) + Mathf.Max(0, Difficulty) * 0.3f); e.dmg = brute ? 35 : 20; e.windup = brute ? 0.8f : 0.42f;
            e.walk = rand * 6; e.leapCd = 2 + rand * 3; e.spawnT = 0.9f;
            if (kind.Length > 0)
            {
                DressHusk(e, kind);
                switch (kind)
                {
                    case "scav": e.hp = 1.8f; e.dmg = 12; e.leapCd = 0.5f + rand; break;
                    case "wraith": e.hp = 2.6f; break;
                    case "bomber": e.hp = 2; e.speed = Mathf.Min(11, e.speed + 2); break;
                    case "twin": e.hp = 3.5f; e.dmg = 15; break;
                    case "glacier": e.hp = 16; break;
                    case "tank": e.hp = 18; e.speed *= 0.85f; e.dmg = 40; break;
                    case "pilgrim": e.hp = 16; e.speed *= 0.7f; break;
                }
            }
            if (kind == "twin" && Auth) TwinEmerge(e); // it steps out of a mirror (co-op: where is the host's call; before FinishEnt, so the spawn message carries it)
            FinishEnt(e); SyncEnt(e);

            // scavengers hunt in packs
            if (kind == "scav" && !packMate && enemies.Count < 28 && Auth) for (int k = 0; k < 2; k++) { var o = V(rand - 0.5f, 0, rand - 0.5f) * 5; float lim = LB - 4; SpawnHusk(clamp(x + o.x, -lim, lim), clamp(z + o.z, -lim, lim), false, "scavpack"); }
            return e;
        }
        void Unstick(Ent e)
        {
            SpawnParticles(BodyPoint(e), 8, 4, e.holy ? goldSpark : bloodMat, 0.12f, 0.5f);
            var s = FreeSpot(12);
            e.pos = V(s.x, 0, s.y); e.vel = Vector3.zero; e.leaping = false; e.sideT = 0; e.blockT = 0; e.stuckT = 0; e.stuckFrom = e.pos;
            e.spawnT = 0.9f; SyncEnt(e); // climbs out of the ground again, like a fresh spawn
            ShockRing(e.pos, 1.6f * e.s, THEME.holy ? 0x80e8ff : 0xff2000, 0.9f);
        }
        Ent SpawnDrone(float x, float y, float z, string forceKind = null)
        {
            bool holy = HolyLevel;
            var e = NewEnt("drone");
            e.dr = BuildDrone(e, holy);
            e.holy = holy; e.pos = V(x, y, z); e.hp = 2.5f; e.headY = 0; e.bodyY = 0; e.hitR = 1.0f;
            e.shootT = 1.5f + rand * 2; e.sign = rand < 0.5f ? -1 : 1; e.phase = rand * 6; e.spawnT = 0.9f;
            var kind = forceKind ?? RollDroneKind(); if (kind.Length > 0) DressDrone(e, kind);
            if (kind == "spitter") SpitterAnchor(e); // it climbs out of the wall, not out of thin air
            FinishEnt(e); e.g.transform.localPosition = e.pos; return e;
        }

        // ---------- damage & death ----------
        void DamageEnemy(Ent e, float dmg, Hit info)
        {
            if (e.dead) return;
            if (MP && !Auth && !netRemoteHit && BellWarded(e, dmg, info)) { SpawnParticles(info.point ?? e.pos, 4, 4, goldSpark, 0.07f, 0.35f); return; } // co-op: the ward is judged where the shot was fired
            if (NetClaimHit(e, dmg, info)) return; // co-op client: the host applies the damage (GameNet.cs)
            if (e.invuln && !info.cheat) { SpawnParticles(info.point ?? BodyPoint(e), 3, 4, sparkMat, 0.06f, 0.2f); return; }
            if (e.type == "boss") { if (info.parry) dmg *= 1.6f; if (e.state == "stunned") dmg *= 1.5f; }
            else dmg = KindDamage(e, dmg, info);
            e.hp -= dmg; e.flash = 0.07f; e.flinch = Mathf.Min(1, e.flinch + (e.type == "boss" ? 0.15f : 0.5f));
            if (info.dir.HasValue && e.type != "boss") e.vel += info.dir.Value * ((info.kb > 0 ? info.kb : 3) * (e.heavy ? 0.3f : 1));
            SpawnParticles(info.point ?? BodyPoint(e), Mathf.Min(14, 3 + Mathf.RoundToInt(dmg * 5)), 7, bloodMat, 0.12f, 0.7f);
            if (frostGore) SpawnParticles(info.point ?? BodyPoint(e), Mathf.Min(8, 2 + Mathf.RoundToInt(dmg * 2)), 6, cyanMat, 0.07f, 0.5f); // chips of ice off a frozen creature
            if (!netRemoteHit) CombatHeal(HitHeal(e, dmg), e.type == "boss"); // (co-op: a teammate's hit heals them, on their machine)
            Sfx(info.head ? "hitHead" : "hit");
            if (e.hp <= 0)
            {
                bool traitorFall = e.type == "boss" && e.boss == 3 && !e.phase2;
                if (!traitorFall) NetKill(e, info);
                if (e.type != "boss") KillEnemy(e, info);
                else if (traitorFall) TraitorFall(e); // the Frozen Traitor's first "death" only ends phase 1 (clients follow its state)
                else KillBoss(e, info);
            }
            else if (info.parry && e.type != "boss") Stagger(e); // a parried orb's blast leaves survivors reeling
        }
        void StripXRay(GameObject g)
        {
            if (!g || !xrayMat) return;
            foreach (var r in g.GetComponentsInChildren<Renderer>(true)) { var ms = new List<Material>(r.sharedMaterials); if (ms.Remove(xrayMat) | ms.Remove(xrayMask)) r.sharedMaterials = ms.ToArray(); }
        }
        void Dismember(List<N> parts, Ent e, Hit info, float speed)
        {
            xrayOn.Remove(e); StripXRay(e.g);
            foreach (var m in e.mats) Mats.Emissive(m, 0x1a0000);
            foreach (var part in parts)
            {
                if (part == null || !part.t) continue;
                part.t.SetParent(world, true);
                Mk.StripColliders(part.go);
                if (xrayMat) foreach (var r in part.go.GetComponentsInChildren<Renderer>(true)) { var ms = new List<Material>(r.sharedMaterials); if (ms.Remove(xrayMat) | ms.Remove(xrayMask)) r.sharedMaterials = ms.ToArray(); }
                var v = V(rand - 0.5f, rand * 0.7f + 0.5f, rand - 0.5f) * speed;
                if (info.dir.HasValue) v += info.dir.Value * speed * 0.6f;
                v += e.vel * 0.5f;
                var stump = Mk.Msh(part.t, Geo.Sphere(0.09f * Mathf.Max(1, e.s), 8, 6), gibMat); stump.layer = Mk.LayerIgnore; // a bloody stump where it tore off
                particles.Add(new Pt { m = part.t, v = v, life = 6 + rand * 2.5f, grav = GRAV, hasSpin = true, spin = V(rand * 12 - 6, rand * 12 - 6, rand * 12 - 6), owned = true, bleed = true });
            }
            TrimParticles();
        }
        void RemoveEntBody(Ent e) { Mk.StripColliders(e.g); Destroy(e.g); }
        void KillEnemy(Ent e, Hit info)
        {
            e.dead = true;
            KindDeath(e, info);
            xrayOn.Remove(e); StripXRay(e.g); // the gibs must not keep the see-through outline
            var c = BodyPoint(e);
            // which weapon finished it decides how it dies
            int wk = info.exec ? 9 : info.cheat ? 4 : info.explosion ? 5 : info.punch ? 6 : info.slam ? 7 : info.coin ? 8 : info.sniper ? 3 : P.weapon;
            float fling = wk == 9 ? 2.4f : wk == 1 ? 2.2f : wk == 3 ? 2.8f : wk == 5 ? 1.8f : wk == 6 ? 2.4f : wk == 2 ? 0.7f : 1;
            if (wk == 5) foreach (var m in e.mats) { Mats.SetColor(m, Mats.Hex(0x1c1410)); } // charred
            if (e.type == "husk")
            {
                var r = e.hr;
                var parts = new List<N> { r.head, r.sh[0], r.sh[1], r.thigh[0], r.thigh[1], r.torso };
                foreach (var extra in new[] { r.elbow[0], r.elbow[1], r.knee[0], r.knee[1], r.jaw }) if (rand < 0.65f) parts.Add(extra); // limbs come apart at the joints too
                if (!info.exec) { Dismember(parts, e, info, (e.brute ? 6 : 9) * fling); MeatChunks(c, e.brute ? 10 : 6, 7 * fling, info.dir); }
                SpawnParticles(c, e.brute ? 26 : 14, 11, e.holy ? goldSpark : gibMat, e.brute ? 0.35f : 0.22f, 2.5f);
            }
            else
            {
                var r = e.dr; var parts = new List<N> { r.lidTop, r.lidBot, r.ring1, r.ring2 };
                foreach (var t in r.tentacles) parts.Add(t[0]);
                parts.AddRange(r.wings);
                Dismember(parts, e, info, 10 * fling);
                Blast(c, 2.5f, e.holy ? 0x80e8ff : 0xff8800); SpawnParticles(c, 14, 10, e.holy ? goldSpark : sparkMat, 0.1f, 0.6f);
            }
            KillFx(wk, e, info, c);
            if (e.type == "husk") Decal(e.pos, (e.brute ? 2.6f : 1.7f) + rand * 0.6f, true);
            RemoveEntBody(e);
            SpawnParticles(c, 22, 9, bloodMat, 0.14f, 1.2f);
            if (Vector3.Distance(P.pos, e.pos) < 8) CombatHeal(16); // co-op: whoever stands in the spray, on their own machine
            if (!LocalHit) { Sfx("kill"); return; }           // a teammate's kill: the gore, but not their hit-stop or style
            freeze = Mathf.Max(freeze, e.brute ? 0.12f : 0.045f); P.shake = Mathf.Max(P.shake, e.brute ? 0.5f : 0.25f); Sfx("kill");
            AddStyle(e.brute ? "BIG KILL" : "KILL", e.brute ? 220 : e.type == "drone" ? 70 : 80);
            if (info.head && !info.coin && !info.sniper) AddStyle(e.type == "drone" ? "EYE POP" : "HEADSHOT", 60);
            if (info.slam) AddStyle("GROUND SLAM", 120);
            if (info.punch && !info.exec) AddStyle("DISRESPECT", 130);
            if (info.parry) AddStyle("RETURN TO SENDER", 150);
            if (info.close) AddStyle("POINT BLANK", 50);
            if (info.explosion && !info.parry) AddStyle("EXPLODED", 60);
            if (info.deadeye) AddStyle("DEADEYE", 90); else if (info.sniper) AddStyle("SNIPED", 60);
            if (info.cheat) AddStyle("BANG!", 100);
            if (!P.onGround) AddStyle("AIRBORNE", 50);
            killChain = killChainT > 0 ? killChain + 1 : 1; killChainT = 0.7f;
            if (killChain >= 2) AddStyle($"MULTIKILL x{killChain}", 60 * killChain);
        }
        // weapon-flavoured deaths: 0 revolver, 1 shotgun, 2 minigun, 3 sniper, 4 BANG, 5 explosion/core, 6 punch, 7 slam, 8 coin
        void KillFx(int wk, Ent e, Hit info, Vector3 c)
        {
            var dir = info.dir ?? (c - EyePos).normalized; var hp = e.type == "husk" ? e.pos + V(0, e.headY, 0) : c;
            switch (wk)
            {
                case 0: // revolver: headshots pop the head
                    if (info.head) { Spray(hp, dir, 28, 11, 0.6f, bloodMat, 0.13f, 1.1f); Blast(hp, 1.4f, 0xff2a10); SpawnParticles(hp, 8, 6, sparkMat, 0.06f, 0.3f, 0); }
                    else Spray(c, dir, 12, 8, 0.5f, bloodMat, 0.12f, 0.9f);
                    break;
                case 1: // shotgun: blown backwards in a cone of blood
                    Spray(c, dir, 46, 16, 0.45f, bloodMat, 0.15f, 1.2f); Spray(c, dir, 14, 12, 0.5f, gibMat, 0.22f, 1.6f);
                    ShockRing(e.pos, 2.5f, 0xff4020, 0.3f); break;
                case 2: // minigun: shredded to confetti
                    for (int i = 0; i < 4; i++) Spray(c + Random.insideUnitSphere * 0.5f, dir, 12, 7, 0.9f, i % 2 == 0 ? gibMat : bloodMat, 0.08f, 0.9f);
                    SpawnParticles(c, 12, 6, sparkMat, 0.05f, 0.25f); break;
                case 3: // sniper: punched clean through - a streak of blood along the shot
                    Tracer(c, c + dir * 7, 0xb00000, 0.22f, 0.5f); Tracer(c, c + dir * 4, 0xff3020, 0.1f, 0.35f);
                    Spray(c, dir, 50, 26, 0.18f, bloodMat, 0.13f, 1.0f); freeze = Mathf.Max(freeze, 0.09f); break;
                case 4: // BANG: erased in gold and blood-red light
                    Blast(c, 4, 0xffd040); ShockRing(e.pos, 6, 0xff2a2a, 0.5f); ShockRing(e.pos, 3, 0xffd040, 0.35f);
                    SpawnParticles(c, 30, 12, goldSpark, 0.1f, 0.8f, -2); SpawnParticles(c, 20, 9, bloodMat, 0.12f, 0.8f); break;
                case 5: // explosion / core: charred, smoking, crackling
                    SpawnParticles(c, 16, 3, ashMat, 0.25f, 1.6f, -3); SpawnParticles(c, 16, 9, emberMat, 0.07f, 1.0f, -1);
                    SpawnParticles(c, 10, 10, cyanMat, 0.05f, 0.35f, 0); break;
                case 6: // punch: launched with a white impact ring
                    ShockRing(c - V(0, 0.8f, 0), 3, 0xffffff, 0.3f); Spray(c, dir, 20, 13, 0.35f, bloodMat, 0.13f, 1.0f); break;
                case 7: // slam: crushed into the floor, dust everywhere
                    SpawnParticles(e.pos + V(0, 0.2f, 0), 22, 7, dustMat, 0.2f, 0.8f); ShockRing(e.pos, 3, 0xffaa66, 0.35f); break;
                case 8: // coin ricochet: a golden ping
                    SpawnParticles(hp, 18, 8, goldSpark, 0.07f, 0.5f, 0); Blast(hp, 1.2f, 0xffe080); break;
                case 9: // chainsaw execution: ripped apart in a geyser of blood
                    if (e.type != "husk") { Spray(c, Vector3.up, 60, 15, 0.55f, bloodMat, 0.15f, 1.8f); MeatChunks(c, 14, 9, dir); }
                    if (e.type != "husk") { Blast(c, 0.8f, 0xb00000); ShockRing(e.pos, 4.5f, 0xff1a1a, 0.45f); }
                    for (int i = 0; i < 6; i++) Decal(e.pos + V((rand - 0.5f) * 4, 0, (rand - 0.5f) * 4), 1.2f + rand * 1.4f, true);
                    break;
            }
        }
        // particles thrown in a cone around dir
        void Spray(Vector3 pos, Vector3 dir, int n, float speed, float spread, Material mat, float size, float life)
        {
            for (int i = 0; i < n; i++)
            {
                float s = size * (0.6f + rand * 0.8f);
                var v = (dir + Random.insideUnitSphere * spread).normalized * speed * (0.5f + rand * 0.8f); v.y += speed * 0.15f;
                particles.Add(new Pt { pos = pos, scale = V(s, s, s), mat = mat, v = v, life = life * (0.6f + rand * 0.6f), grav = GRAV });
            }
            TrimParticles();
        }
        void BruteSlam(Ent e)
        {
            var f = V(Mathf.Sin(e.yaw), 0, Mathf.Cos(e.yaw));
            var p = e.pos + f * 1.6f * e.s;
            ShockRing(p, 8, e.holy ? 0x80e8ff : 0xffaa00, 0.5f); ShockRing(p, 4, 0xffffff, 0.3f);
            SpawnParticles(p + V(0, 0.2f, 0), 26, 9, e.holy ? goldSpark : dustMat, 0.2f, 0.7f);
            Sfx("slam"); P.shake = Mathf.Max(P.shake, 0.45f);
            float d = Hypot(P.pos.x - p.x, P.pos.z - p.z), dy = P.pos.y - e.pos.y;
            if (d < 2.8f && Mathf.Abs(dy) < 3) DamagePlayer(e.dmg);
            else if (d < 8 && dy < 0.8f && dy > -1 && P.onGround) DamagePlayer(20);
        }
        static void BallisticLeap(Ent e, float dx, float dz, float dist, float dy, float extra, float maxH)
        {
            float h = Mathf.Max(dy, 0) + extra, vy = Mathf.Sqrt(2 * GRAV * gravK * h);
            float T = vy / (GRAV * gravK) + Mathf.Sqrt(2 * Mathf.Max(0.3f, h - dy) / (GRAV * gravK));
            float hs = Mathf.Min(maxH, dist / T);
            e.vel = V(dx / dist * hs, vy, dz / dist * hs);
            e.leapVX = e.vel.x; e.leapVZ = e.vel.z; // re-applied every frame so a wall can't kill the jump's forward motion
        }
        void SetEmis(Ent e, int hex) { foreach (var m in e.mats) Mats.Emissive(m, hex); }

        void AnimateHusk(Ent e, float dt)
        {
            var r = e.hr; float hsp = Hypot(e.vel.x, e.vel.z);
            e.walk += dt * hsp * (r.ruin ? 1.45f / Mathf.Max(0.5f, e.s) : e.brute ? 1.2f : 1.9f); // (ruin bodies: the stride matches the leg, so the planted foot doesn't slide)
            float sw = Mathf.Sin(e.walk), amp = Mathf.Min(1, hsp / 4);
            e.flinch = Mathf.Max(0, e.flinch - dt * 4);
            bool air = !e.ground;
            for (int i = 0; i < 2; i++)
            {
                float sg = i == 1 ? -1 : 1;
                if (air) { r.thigh[i].rx = -0.9f; r.knee[i].rx = 1.4f; }
                else { r.thigh[i].rx = -sw * sg * 0.75f * amp; r.knee[i].rx = 0.1f + Mathf.Max(0, sw * sg) * 1.1f * amp; }
            }
            r.hips.py = 1.0f - (air ? 0 : Mathf.Abs(Mathf.Cos(e.walk)) * 0.07f * amp);
            float shX, elX, lean = 0.3f * amp, jaw = 0.08f + Mathf.Sin(time * 18 + e.walk) * 0.04f, spread = 0.15f;
            if (e.attackT > 0)
            {
                float t = 1 - e.attackT / e.windup, ez = 1 - (1 - t) * (1 - t);
                shX = lerp(-1.0f, -3.0f, ez); elX = lerp(-0.4f, -0.9f, ez); lean = lerp(0.3f, -0.25f, ez); jaw = 0.55f; spread = 0.25f;
            }
            else if (e.strikeT > 0)
            {
                float t = 1 - e.strikeT / 0.3f, ez = Mathf.Min(1, t * 3);
                shX = lerp(-3.0f, -0.5f, ez); elX = lerp(-0.9f, -0.1f, ez); lean = lerp(-0.25f, 0.7f, ez); jaw = 0.4f;
            }
            else if (e.leaping || e.stun > 0) { shX = -1.7f + Mathf.Sin(time * 25) * (e.stun > 0 ? 0.6f : 0.1f); elX = -0.2f; jaw = 0.6f; spread = 0.5f; lean = 0.5f; }
            else if (e.brute) { shX = -0.2f + sw * 0.45f * amp; elX = -0.5f; spread = 0.25f; }
            else if (e.holy) { shX = -0.9f + sw * 0.2f * amp; elX = -1.3f; spread = 0.05f; }
            else { shX = -1.15f + sw * 0.25f * amp; elX = -0.45f + Mathf.Sin(time * 9 + e.walk) * 0.1f; }
            for (int i = 0; i < 2; i++)
            {
                float sg = i == 1 ? -1 : 1;
                float swing = (e.attackT > 0 || e.strikeT > 0 || e.leaping) ? 0 : sg * sw * 0.25f * amp;
                r.sh[i].SetRot(shX + swing, 0, (i == 1 ? 1 : -1) * spread); r.elbow[i].rx = elX;
            }
            if (e.kind == "glacier" && e.shield)
            {
                // the shield arm holds a guard: upper arm forward and down, forearm raised, the fist on the grip behind the shield.
                // On the strike it shoves the shield forward; stunned, it sags.
                float shove = e.strikeT > 0 ? Mathf.Sin(Mathf.Clamp01(1 - e.strikeT / 0.3f) * Mathf.PI) : 0, low = e.stun > 0 ? 1 : 0;
                r.sh[0].SetRot(-0.8f + Mathf.Sin(e.walk) * 0.05f * amp + low * 0.5f - shove * 0.25f, 0, 0.1f); r.elbow[0].rx = -1.0f + low * 0.35f + shove * 0.3f;
                e.shield.transform.localPosition = V(-0.16f, 0.36f - low * 0.22f, 0.92f + shove * 0.16f);
            }
            float idle = r.ruin ? 1 - amp : 0; // (ruin bodies breathe when they stand, and roll their shoulders as they walk)
            r.torso.SetRot(lean - e.flinch * 0.6f + idle * Mathf.Sin(time * 1.7f + e.walk) * 0.05f, Mathf.Sin(e.walk) * 0.15f * amp, r.ruin ? -Mathf.Sin(e.walk) * 0.06f * amp : 0);
            r.head.SetRot(-lean * 0.7f - e.flinch * 0.5f, 0, Mathf.Sin(time * 3 + e.walk) * 0.12f);
            r.jaw.rx = e.holy ? jaw * 0.3f : jaw;
            SetEmis(e, e.flash > 0 ? 0xffffff : e.attackT > 0 ? (e.holy ? 0x103040 : 0x660000) : 0x000000);
            r.extra?.Invoke(e, dt);
        }
        void AnimateDrone(Ent e, float dt)
        {
            var r = e.dr; float charge = e.charging ? clamp(1 - e.shootT / 0.6f, 0, 1) : 0;
            e.flinch = Mathf.Max(0, e.flinch - dt * 4);
            e.blink -= dt; if (e.blink < -3 - rand * 4) e.blink = 0.15f;
            float open = (e.blink > 0 ? 0.05f : 0.38f) + charge * 0.7f + e.flinch * 0.4f;
            r.lidTop.rx = -open; r.lidBot.rx = open;
            r.ring1.ry += dt * (1.5f + charge * 14); r.ring2.SetRot(Mathf.Sin(time * 0.7f + e.phase) * 0.5f, 0, r.ring2.rz + dt * (1 + charge * 12));
            r.iris.transform.localScale = V(1 + charge * 0.5f, 1 + charge * 0.5f, 0.4f);
            if (r.holy) r.irisMat.SetColor("_BaseColor", new Color(0.38f + charge * 0.62f, 0.85f + charge * 0.15f, 1));
            else if (e.kind.Length > 0) r.irisMat.SetColor("_BaseColor", Color.Lerp(KindIris(e), Color.white, charge * 0.6f));
            else r.irisMat.SetColor("_BaseColor", new Color(1, 0.53f + charge * 0.47f, charge));
            float eg = 0.8f * (0.7f + charge * 1.8f) / 0.8f; r.eyeGlow.transform.localScale = V(eg, eg, eg); Mats.SetAlpha(r.eyeGlowMat, 0.4f + charge * 0.6f);
            float js = 0.6f + Mathf.Sin(time * 30) * 0.1f + Mathf.Min(e.vel.magnitude / 14, 1) * 0.4f; r.jet.transform.localScale = V(js, js, js);
            r.root.py = Mathf.Sin(time * 2 + e.phase) * 0.15f;
            r.root.rz = Mathf.Sin(time * 1.3f + e.phase) * 0.15f;
            for (int i = 0; i < r.tentacles.Count; i++)
                for (int j = 0; j < r.tentacles[i].Count; j++)
                    r.tentacles[i][j].SetRot(Mathf.Sin(time * 3 + i * 1.7f + j * 0.7f) * (0.25f + j * 0.08f) + e.vel.magnitude * 0.02f, 0, Mathf.Cos(time * 2.3f + i + j * 0.5f) * 0.2f);
            for (int i = 0; i < r.wings.Count; i++) { float s = r.wingS[i]; r.wings[i].SetRot(0, s * 0.3f, s * (Mathf.Sin(time * 11 + e.phase) * 0.5f - 0.1f)); }
            for (int i = 0; i < e.mats.Count; i++) Mats.Emissive(e.mats[i], e.flash > 0 ? 0xffffff : (r.holy && i == 1 ? 0x2a1800 : 0x000000));
            r.extra?.Invoke(e, dt);
        }
        void TurnTo(Ent e, float yaw, float rate, float dt)
        {
            float d = yaw - e.yaw; d = Mathf.Atan2(Mathf.Sin(d), Mathf.Cos(d));
            e.yaw += d * (1 - Mathf.Exp(-rate * dt));
        }

        // ---------- husk & drone AI ----------
        void UpdateEnemies(float dt)
        {
            killChainT -= dt;
            float lim = LB - 2;
            for (int idx = 0; idx < enemies.Count; idx++)
            {
                var e = enemies[idx];
                if (e.dead) continue;
                Tgt = TargetOf(e, dt); // co-op: who it hunts (solo: always P). Attacks only ever HURT the local player P.
                if (e.type == "boss")
                {
                    // enraged bosses move and attack faster; the Seraph keeps speeding up as its health drains (1.2x -> 1.9x)
                    float bdt = e.phase2 ? dt * (e.boss == 2 ? 1.2f + 0.7f * SeraphFury(e) : 1.2f) : dt;
                    if (e.state != "stunned") e.execDone = false; // a fresh stun can be executed again
                    if (e.boss == 1) UpdateCarnifex(e, bdt); else if (e.boss == 3) UpdateTraitor(e, dt); else if (e.boss == 4) UpdateHollowKing(e, dt); else if (e.boss == 5) UpdateWarden(e, dt); else UpdateSeraph(e, bdt);
                    if (testMode && Arg("-brbosshold") != null) // -brbosshold x,y,z: keep the boss idling at a spot (to look at it, test)
                    {
                        var hv = System.Array.ConvertAll(Arg("-brbosshold").Split(','), x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture));
                        if ((e.boss == 2 && e.state == "hover") || ((e.boss == 5 || e.boss == 4) && e.state == "chase")) { e.stateT = 5; e.pos = V(hv[0], hv[1], hv[2]); e.vel = Vector3.zero; var tp = P.pos - e.pos; e.yaw = Arg("-brbossview") != null ? 0 : Mathf.Atan2(tp.x, tp.z); SyncEnt(e); }
                    }
                    continue;
                }
                e.flash -= dt;
                if (e.spawnT > 0)
                {
                    e.spawnT -= dt; float t = Mathf.Max(0, e.spawnT / 0.9f);
                    if (e.emerging && e.type == "husk")
                    {
                        // out of the glass: flattened against the mirror's face, then stepping forward into the world
                        float k = Mathf.Max(0, e.spawnT / 1.3f); e.hr.root.py = 0;
                        e.g.transform.localPosition = e.pos - e.emergeDir * k; e.g.transform.localScale = V(e.s, e.s, e.s * Mathf.Max(0.08f, 1 - k));
                        if (rand < 0.6f) SpawnParticles(e.pos - e.emergeDir * k + V((rand - 0.5f) * 1.2f, rand * 2 * e.s, (rand - 0.5f) * 1.2f), 1, 0.6f, rand < 0.5f ? goldSpark : cyanMat, 0.06f, 0.4f, 0);
                        if (e.spawnT <= dt) { e.emerging = false; e.g.transform.localScale = V(e.s, e.s, e.s); SyncEnt(e); }
                        AnimateHusk(e, dt); continue;
                    }
                    if (e.type == "husk") e.hr.root.py = -2.3f * t * t; else e.dr.root.Scale(Mathf.Max(0.001f, 1 - t));
                    if (rand < 0.5f) SpawnParticles(V(e.pos.x, e.pos.y + rand * 2 * e.s, e.pos.z), 1, 2, e.holy ? goldSpark : bloodMat, 0.1f, 0.4f, -4);
                    if (e.type == "husk") AnimateHusk(e, dt); else AnimateDrone(e, dt);
                    continue;
                }
                float dx = Tgt.pos.x - e.pos.x, dz = Tgt.pos.z - e.pos.z, dist = Hypot(dx, dz); if (dist == 0) dist = 1; float dy = Tgt.pos.y - e.pos.y;
                if (frostGore && rand < 0.07f) SpawnParticles(BodyPoint(e) + V((rand - 0.5f) * e.s, (rand - 0.3f) * e.s, (rand - 0.5f) * e.s), 1, 0.5f, rand < 0.5f ? SnowMat : cyanMat, 0.05f, 0.9f, -1); // frost mist coming off it
                if (e.type == "husk")
                {
                    e.cd -= dt; e.leapCd -= dt; e.strikeT -= dt;
                    if (KindPre(e, dt, dx, dz, dist, dy)) continue;
                    if (e.stun > 0) { e.stun -= dt; float k = Mathf.Exp(-3 * dt); e.vel.x *= k; e.vel.z *= k; e.attackT = 0; }
                    else if (e.attackT > 0)
                    {
                        e.attackT -= dt; float k = Mathf.Exp(-10 * dt); e.vel.x *= k; e.vel.z *= k;
                        if (e.attackT <= 0)
                        {
                            e.strikeT = 0.3f; e.cd = e.brute ? 1.6f : 0.9f;
                            if (e.brute) BruteSlam(e);
                            else
                            {
                                e.vel.x += dx / dist * 6; e.vel.z += dz / dist * 6;
                                if (MeIn(e.pos, 2.5f, 2.2f)) { DamagePlayer(e.dmg, e.pos); if (e.kind == "frost" || e.kind == "wraith") chillT = Mathf.Max(chillT, 1.6f); }
                            }
                        }
                    }
                    else if (e.leaping)
                    {
                        e.vel.x = e.leapVX; e.vel.z = e.leapVZ;
                        if (MeIn(e.pos, 1.6f, 2)) { DamagePlayer(15); e.leaping = false; }
                        if (e.ground && e.vel.y <= 0) e.leaping = false;
                    }
                    else
                    {
                        float k = 1 - Mathf.Exp(-5 * dt), sp = e.strikeT > 0 ? 0 : e.speed;
                        float mx = dx / dist, mz = dz / dist;
                        if (e.sideT > 0) { e.sideT -= dt; float t = mx; mx = -mz * e.sideSign; mz = t * e.sideSign; }
                        KindSteer(e, dist, ref mx, ref mz, ref sp);
                        e.vel.x += (mx * sp - e.vel.x) * k; e.vel.z += (mz * sp - e.vel.z) * k;
                        // decisions are the host's: a client's copy only starts attacks and leaps when the host's snapshot says so
                        if (Auth && dist < 1.9f * e.s && Mathf.Abs(dy) < 2 * e.s && e.cd <= 0 && !Down(Tgt)) { e.attackT = e.windup; if (e.holy) Sfx("chant"); Sfx(e.brute ? "growlLow" : "growl"); }
                        else if (Auth && !e.brute && e.ground && e.leapCd <= 0 && !Down(Tgt) &&
                            ((dist > 5 && dist < 11 && rand < dt * 1.5f) || (dy > 2.5f && dist < 16 && Tgt.onGround && rand < dt * 2)))
                        {
                            BallisticLeap(e, dx, dz, dist, dy, 1.5f, 22);
                            e.leaping = true; e.ground = false; e.leapCd = 3 + rand * 3; Sfx("growl");
                        }
                    }
                    e.vel.y = Mathf.Max(e.vel.y - GRAV * gravK * dt, -50);
                    var res = MoveBody(ref e.pos, ref e.vel, 0.4f * e.s, 2.2f * e.s, dt);
                    e.ground = res.ground;
                    if (res.wall && res.ground && e.stun <= 0 && e.attackT <= 0) e.vel.y = 10;
                    e.blockT = res.wall && !e.leaping && e.attackT <= 0 ? e.blockT + dt : Mathf.Max(0, e.blockT - dt);
                    if (Auth && e.blockT > 0.45f && e.ground && e.stun <= 0 && !Down(Tgt)) // stuck behind cover: vault it (husk) or walk around it (brute)
                    {
                        e.blockT = 0;
                        if (!e.brute && !e.triedVault) { BallisticLeap(e, dx, dz, dist, dy, 6.5f, 14); e.leaping = true; e.ground = false; e.triedVault = true; }
                        else { e.sideT = 2.0f; if (e.sideSign == 0) e.sideSign = rand < 0.5f ? -1 : 1; e.triedVault = false; }
                    }
                    e.pos.x = clamp(e.pos.x, -lim, lim); e.pos.z = clamp(e.pos.z, -lim, lim);
                    // stuck for good (wedged on a ledge, trapped below the player, fallen off): re-emerge somewhere open
                    bool chasing = !e.leaping && e.attackT <= 0 && e.stun <= 0 && e.strikeT <= 0 && (dist > 3 || Mathf.Abs(dy) > 3);
                    if (!chasing || (e.pos - e.stuckFrom).sqrMagnitude > 2.25f) { e.stuckFrom = e.pos; e.stuckT = 0; }
                    else e.stuckT += dt;
                    if (Auth && (e.stuckT > 3.5f || e.pos.y < -12)) { Unstick(e); continue; }
                    TurnTo(e, Mathf.Atan2(dx, dz), e.attackT > 0 ? 3 : 10, dt);
                    AnimateHusk(e, dt);
                    SyncEnt(e);
                }
                else
                {
                    float ang = Mathf.Atan2(-dz, -dx) + e.sign * 0.6f;
                    float orbitR = DroneOrbit(e);
                    float tx = clamp(Tgt.pos.x + Mathf.Cos(ang) * orbitR, -(LB - 6), LB - 6);
                    float tz = clamp(Tgt.pos.z + Mathf.Sin(ang) * orbitR, -(LB - 6), LB - 6);
                    float ty = e.kind == "bell" ? clamp(Mathf.Max(17, Tgt.pos.y + 9) + Mathf.Sin(time * 0.8f + e.phase) * 1.0f, 3, 27) // the Bell Wraith keeps high above the fight
                        : clamp(Tgt.pos.y + 6 + Mathf.Sin(time * 1.3f + e.phase) * 1.5f, 3, 24);
                    if (e.stun > 0) e.stun -= dt;
                    else { e.vel.x += (tx - e.pos.x) * 2 * dt; e.vel.y += (ty - e.pos.y) * 2 * dt; e.vel.z += (tz - e.pos.z) * 2 * dt; }
                    e.vel *= Mathf.Exp(-1.5f * dt); if (e.vel.magnitude > 14) e.vel = e.vel.normalized * 14;
                    e.pos += e.vel * dt;
                    e.pos.x = clamp(e.pos.x, -lim, lim); e.pos.z = clamp(e.pos.z, -lim, lim); e.pos.y = clamp(e.pos.y, 1, 30);
                    var head = EyeOf(Tgt); head.y -= 0.3f;
                    e.g.transform.localPosition = e.pos;
                    if (e.kind == "bell") { var fl = head - e.pos; fl.y = 0; if (fl.sqrMagnitude > 1e-4f) e.g.transform.rotation = Quaternion.Slerp(e.g.transform.rotation, Quaternion.LookRotation(fl), 1 - Mathf.Exp(-3 * dt)); }
                    else if ((head - e.pos).sqrMagnitude > 1e-4f) e.g.transform.rotation = Quaternion.LookRotation(head - e.pos);
                    if (DroneKind(e, dt, head)) { AnimateDrone(e, dt); continue; }
                    e.shootT -= dt;
                    if (!e.charging && e.shootT < 0.6f) { e.charging = true; Sfx("charge"); }
                    if (e.shootT <= 0)
                    {
                        e.shootT = 2.2f + rand * 1.5f; e.charging = false;
                        var from = e.pos + (head - e.pos).normalized * 0.7f;
                        if (!Down(Tgt) && Los(from, head))
                        {
                            var v = (head - from).normalized * Mathf.Min(30, 19 + Mathf.Max(0, Difficulty) * 0.8f);
                            MakeProj("orb", from, v, 15, 5); Sfx("eshot");
                            e.vel -= v * 0.15f;
                        }
                    }
                    AnimateDrone(e, dt);
                }
            }
            for (int i = 0; i < enemies.Count; i++) for (int j = i + 1; j < enemies.Count; j++)
                {
                    var a = enemies[i]; var b = enemies[j]; if (a.dead || b.dead || a.type != "husk" || b.type != "husk") continue;
                    float dx = b.pos.x - a.pos.x, dz = b.pos.z - a.pos.z, d = Hypot(dx, dz), min = 0.5f * (a.s + b.s) + 0.2f;
                    if (d < min && d > 1e-4f) { float p = (min - d) / 2 / d; a.pos.x -= dx * p; a.pos.z -= dz * p; b.pos.x += dx * p; b.pos.z += dz * p; SyncEnt(a); SyncEnt(b); }
                }
            UpdateXRay();
            enemies.RemoveAll(e => e.dead);
        }

        // ---------- last enemies: see-through outline ----------
        // when only two are left, the parts of them hidden behind walls glow so they can be found (visible parts don't)
        Material xrayMat, xrayMask; readonly HashSet<Ent> xrayOn = new HashSet<Ent>();
        void UpdateXRay()
        {
            if (!xrayMat)
            {
                var t = Resources.Load<Material>("BR_XRay"); var tm = Resources.Load<Material>("BR_XRayMask"); if (!t || !tm) return;
                xrayMat = new Material(t) { name = "BR_XRay(G)" }; xrayMask = new Material(tm) { name = "BR_XRayMask(G)" };
            }
            int alive = 0; foreach (var e in enemies) if (!e.dead && e.type != "boss") alive++;
            bool want = phase == "fight" && boss == null && alive > 0 && alive <= 2;
            foreach (var e in enemies) { bool on = want && !e.dead && e.type != "boss" && e.spawnT <= 0; if (on != xrayOn.Contains(e)) SetXRay(e, on); }
            foreach (var e in new List<Ent>(xrayOn)) if (e.dead || !enemies.Contains(e)) SetXRay(e, false);
        }
        void SetXRay(Ent e, bool on)
        {
            if (on) xrayOn.Add(e); else xrayOn.Remove(e);
            if (!e.g) return;
            foreach (var r in e.g.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.gameObject.name == "glow") continue;
                var ms = new List<Material>(r.sharedMaterials);
                if (on == ms.Contains(xrayMat)) continue;
                if (on) { ms.Add(xrayMask); ms.Add(xrayMat); } else { ms.Remove(xrayMask); ms.Remove(xrayMat); }
                r.sharedMaterials = ms.ToArray();
            }
        }

        // ---------- enemy projectiles ----------
        public class Proj { public Player tgt; /* co-op: who it was fired at */ public Transform m; public MeshRenderer coreR; public Transform halo; public Material haloMat; public string kind; public Vector3 pos, vel; public bool friendly, parry, slowed; public float life, dmg, grav, splash, homing, size; }
        readonly List<Proj> projectiles = new List<Proj>();
        Proj MakeProj(string kind, Vector3 from, Vector3 vel, float dmg = 15, float life = 6, float grav = 0, float splash = 0, float homing = 0)
        {
            var root = new GameObject("proj").transform; root.SetParent(world, false); root.position = from;
            GameObject core, halo;
            if (kind == "blade") { core = Mk.Msh(root, Geo.Box(0.07f, 0.07f, 0.9f), bladeMat); halo = Mk.Glow(root, serOrange ? 0xff7a20 : 0x60d0ff, 0.9f, 0.7f); }
            else if (kind == "bile") { core = Mk.Msh(root, Geo.Sphere(1, 12, 8), bileMat); core.transform.localScale = V(0.35f, 0.35f, 0.35f); halo = Mk.Glow(root, 0xff4000, 1.8f, 0.8f); }
            else if (kind == "ice") { core = Mk.Msh(root, Geo.Icosahedron(1), cyanMat); core.transform.localScale = V(0.22f, 0.22f, 0.22f); halo = Mk.Glow(root, 0x80e8ff, 1.4f, 0.9f); }
            else if (kind == "slug") { core = Mk.Msh(root, Geo.Sphere(1, 8, 6), eprojMat); core.transform.localScale = V(0.1f, 0.1f, 0.1f); halo = Mk.Glow(root, 0xff3020, 0.9f, 1f); }
            else { core = Mk.Msh(root, Geo.Sphere(1, 12, 8), eprojMat); core.transform.localScale = V(0.18f, 0.18f, 0.18f); halo = Mk.Glow(root, HolyLevel ? 0xffb020 : 0xff7000, 1.3f, 0.9f); }
            var p = new Proj { m = root, coreR = core.GetComponent<MeshRenderer>(), halo = halo.transform, haloMat = halo.GetComponent<Renderer>().sharedMaterial, kind = kind, pos = from, vel = vel,
                life = life, dmg = dmg, parry = kind != "blade", grav = grav, splash = splash, homing = homing, size = kind == "bile" ? 0.45f : 0.2f, tgt = Tgt ?? P };
            if (kind == "blade" && vel.sqrMagnitude > 0) root.rotation = Quaternion.LookRotation(vel);
            projectiles.Add(p); return p;
        }
        void UpdateProjectiles(float dt)
        {
            var chest = ChestPos;
            foreach (var p in projectiles.ToArray())
            {
                if (p.life <= 0) continue;
                p.life -= dt;
                if (p.homing > 0 && !p.friendly)
                {
                    p.homing -= dt;
                    var want = ((p.tgt == null || p.tgt == P ? chest : ChestOf(p.tgt)) - p.pos).normalized * p.vel.magnitude;
                    p.vel = Vector3.Lerp(p.vel, want, Mathf.Min(1, 2.5f * dt));
                }
                if (p.grav > 0) p.vel.y -= p.grav * dt;
                p.pos += p.vel * dt; p.m.position = p.pos;
                if (p.kind == "blade" && p.vel.sqrMagnitude > 0) p.m.rotation = Quaternion.LookRotation(p.vel);
                float hs = (p.kind == "bile" ? 1.8f : 1.2f) + Mathf.Sin(time * 40) * 0.2f; p.halo.localScale = V(hs, hs, hs);
                if (rand < 0.35f) SpawnParticles(p.pos, 1, 0.5f, p.friendly ? friendlyMat : p.kind == "blade" ? SerFx : glowOrange, 0.07f, 0.25f, 0);
                bool boom = false;
                if (!p.friendly)
                {
                    if (Vector3.Distance(p.pos, chest) < 0.75f + p.size)
                    {
                        DamagePlayer(p.dmg, p.pos - p.vel.normalized * 3); p.life = 0;
                        if (p.kind == "ice") { chillT = Mathf.Max(chillT, 2.2f); SpawnParticles(p.pos, 12, 5, cyanMat, 0.1f, 0.5f); }
                        if (p.splash > 0) Blast(p.pos, p.splash, 0xff3000);
                    }
                }
                else foreach (var e in enemies) if (!e.dead && Vector3.Distance(p.pos, BodyPoint(e)) < e.hitR + 0.4f) { boom = true; break; }
                if (p.life > 0 && (PointInLevel(p.pos) || p.pos.y < -2 || !InBounds(p.pos, 3)))
                {
                    if (p.friendly) boom = true;
                    else if (p.splash > 0)
                    {
                        Blast(p.pos, p.splash, 0xff3000); SpawnParticles(p.pos, 10, 6, bileMat, 0.15f, 0.6f); Sfx("splash");
                        if (Vector3.Distance(p.pos, chest) < p.splash + 0.6f) DamagePlayer(p.dmg, p.pos);
                        p.life = 0;
                    }
                    else { SpawnParticles(p.pos, 4, 3, sparkMat, 0.08f, 0.3f); p.life = 0; }
                }
                if (boom) { p.life = 0; Explode(p.pos, 4.5f, 5, 0x60e0ff, 0, true); }
                if (p.life <= 0) Mk.DestroyTree(p.m.gameObject);
            }
            projectiles.RemoveAll(p => p.life <= 0);
        }
    }
}
