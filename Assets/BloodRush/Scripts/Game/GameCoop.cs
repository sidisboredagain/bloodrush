using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    // CO-OP: the player list, the enemy targeting API, downed / revive, player-count scaling and teammates' bodies.
    //
    // P is always the LOCAL player: camera, HUD, viewmodel, hook, weapons and every "did that hit me" test read P.
    // players = everyone in the session (P first); remote entries are filled in by GameNet.cs. Solo: players == [P].
    //
    // Enemy code follows one rule - "aim at T, hurt P":
    //   * Tgt = TargetOf(e, dt) is the player an enemy hunts. Use T (Tgt.pos, EyeOf(Tgt), ChestOf(Tgt), Down(Tgt)) for movement,
    //     facing, aiming and attack decisions.
    //   * damage is only ever dealt to the local player with the existing DamagePlayer(), tested against P / ChestPos /
    //     MeIn(). Every machine runs every enemy, so each player is hit by exactly what they see on their own screen.
    //   * random decisions (attack picks, leaps, teleports, spawns) are taken only when Auth (host or solo); clients get
    //     them from the host's snapshot.
    public partial class Game
    {
        readonly List<Player> players = new List<Player>();
        Player Tgt; // the player the enemy being updated is after (set per enemy by UpdateEnemies)
        bool MP => net != null && net.active;               // in a networked session (host or client)
        bool Auth => net == null || !net.active || net.isHost; // this machine decides enemies, waves, kills and the portal
        bool LocalHit => !netRemoteHit;                     // false while applying a teammate's hit or kill
        static bool Down(Player p) => p == null || p.dead || p.downed;

        // ---------- targeting API (keep it small: new enemy code only needs these) ----------
        public Player NearestPlayer(Vector3 pos)
        {
            Player best = null; float bd = float.MaxValue;
            foreach (var p in players) { if (Down(p) || p.inPortal) continue; float d = (p.pos - pos).sqrMagnitude; if (d < bd) { bd = d; best = p; } }
            return best ?? P;
        }
        public List<Player> PlayersInRadius(Vector3 c, float r)
        {
            var l = new List<Player>();
            foreach (var p in players) if (!Down(p) && !p.inPortal && (p.pos - c).sqrMagnitude < r * r) l.Add(p);
            return l;
        }
        // the enemy's current target: the nearest standing player, re-checked twice a second, sticky unless another is clearly closer
        Player TargetOf(Ent e, float dt)
        {
            if (players.Count <= 1) return P;
            e.tgtT -= dt;
            bool ok = e.tgt != null && !Down(e.tgt) && !e.tgt.inPortal && players.Contains(e.tgt);
            if (!ok || e.tgtT <= 0)
            {
                var n = NearestPlayer(e.pos);
                if (ok && n != e.tgt && (n.pos - e.pos).sqrMagnitude > 0.6f * (e.tgt.pos - e.pos).sqrMagnitude) n = e.tgt;
                e.tgt = n; e.tgtT = 0.5f;
            }
            return e.tgt;
        }
        Vector3 EyeOf(Player p) => p == P ? EyePos : V(p.pos.x, p.pos.y + p.eye, p.pos.z);
        Vector3 ChestOf(Player p) => p == P ? ChestPos : V(p.pos.x, p.pos.y + p.height * 0.55f, p.pos.z);
        // is the LOCAL player inside the cylinder (radius r, half-height h) around c? - the test for an attack landing
        bool MeIn(Vector3 c, float r, float h) => Hypot(P.pos.x - c.x, P.pos.z - c.z) < r && Mathf.Abs(P.pos.y - c.y) < h;
        bool FarFromPlayers(float x, float z, float minDist)
        {
            foreach (var p in players) if (Hypot(x - p.pos.x, z - p.pos.z) < minDist) return false;
            return true;
        }

        // ---------- scaling for 2/3/4 players ----------
        // solo is unchanged. Each extra player brings more enemies (wave sizes), a little more health on each of them and a
        // little more bite, and a boss with much more health - gentle steps, so 4 players is hard but never a damage sponge.
        // Wave sizes are capped so a 4-player wave never floods the arena (performance).
        static readonly float[] COOP_ENEMIES = { 1, 1, 1.6f, 2.1f, 2.6f }, COOP_BOSS = { 1, 1, 1.7f, 2.3f, 2.8f },
            COOP_EHP = { 1, 1, 1.12f, 1.22f, 1.3f }, COOP_DMG = { 1, 1, 1.08f, 1.15f, 1.2f };
        const int COOP_WAVE_CAP = 22; // (most extra enemies of one kind a wave can get from co-op)
        int CoopPlayers => Mathf.Clamp(players.Count, 1, 4);
        int CoopCount(int n) => CoopPlayers <= 1 ? n : Mathf.Min(Mathf.RoundToInt(n * COOP_ENEMIES[CoopPlayers]), n + COOP_WAVE_CAP);
        float CoopBossHp => COOP_BOSS[CoopPlayers];
        float CoopEnemyHp => COOP_EHP[CoopPlayers];
        float CoopDamage => COOP_DMG[CoopPlayers];
        // players joining or leaving mid-fight: the boss's health is rescaled to the new team (the host's call; clients get it)
        void CoopRescaleBoss()
        {
            if (!Auth || boss == null || boss.dead) return;
            if (boss.coopK <= 0) { boss.coopK = CoopBossHp; return; }
            float want = CoopBossHp; if (Mathf.Approximately(want, boss.coopK)) return;
            float k = want / boss.coopK; boss.hp *= k; boss.maxHp *= k; boss.coopK = want;
            if (testMode && testOut != null) System.IO.File.AppendAllText(System.IO.Path.Combine(testOut, "coop.txt"), $"boss rescaled x{k:F2} for {CoopPlayers} players: hp {boss.hp:F0}/{boss.maxHp:F0}\n");
        }

        void CoopResetPlayers()
        {
            P.slot = MP ? net.mySlot : 0; P.name = "P" + (P.slot + 1);
            players.RemoveAll(p => !p.remote);
            players.Insert(0, P);
        }

        // ---------- downed / revive (co-op only: solo dies as before) ----------
        const float REVIVE_TIME = 2.5f, REVIVE_R = 2.6f;
        void GoDown()
        {
            P.hp = 0; P.downed = true; P.revive = 0;
            if (DE.active) EndDeadeye();
            ReleaseHook(); hookPullK = 0; foreach (var s in speedImgs) s.color = Color.clear;
            P.sliding = P.slamming = P.coreCharging = false; P.dashT = 0; laserHeld = false; sawHeld = false; mouseDown = false;
            if (P.cheatT <= 0) SetWeapon(0); // the revolver only
            Sfx("hurt"); P.shake = Mathf.Max(P.shake, 0.6f); hurtFlash = 1;
            Banner("DOWN", "A TEAMMATE CAN REVIVE YOU", "#ff2a2a");
        }
        void Revive()
        {
            P.downed = false; P.hp = 40; P.iframes = 2; P.revive = 0;
            Banner("REVIVED", "", "#5aff8a"); Sfx("arrive"); SetHint(null);
        }
        // crawling: slow, no jumping, dashing or sliding (or standing still inside the portal)
        void UpdateDowned(float dt)
        {
            P.iframes -= dt; P.cd -= dt; P.punchCd -= dt; P.coreCd -= dt; P.airJumpCd -= dt; P.slamCd -= dt; P.jumpBuf = 0; crouchPressed = false;
            if (P.inPortal) return;
            var wish = GetWish();
            if (P.onGround) { float k = 1 - Mathf.Exp(-10 * dt); P.vel.x += (wish.x * 2.2f - P.vel.x) * k; P.vel.z += (wish.z * 2.2f - P.vel.z) * k; }
            P.vel.y = Mathf.Max(P.vel.y - GRAV * gravK * dt, -50);
            P.height = 1.0f;
            var res = MoveBody(ref P.pos, ref P.vel, R, P.height, dt);
            P.onGround = res.ground;
            float lim = LB - R - 0.01f; P.pos.x = clamp(P.pos.x, -lim, lim); P.pos.z = clamp(P.pos.z, -lim, lim);
            if (P.pos.y < 0.25f && InPool(P.pos.x, P.pos.z)) { P.vel.y = 8; P.onGround = false; } // bobs out of the lava, no more damage
            if (P.pos.y < -30) PlacePlayer();
        }

        // ---------- per-frame co-op upkeep (runs inside GameUpdate) ----------
        void UpdateCoop(float dt)
        {
            if (!MP) return;
            CoopRescaleBoss();
            // revive: the downed player's own machine counts the time a standing teammate spends next to them
            string hint = null;
            if (P.downed)
            {
                bool helped = false;
                foreach (var p in players) if (p != P && !Down(p) && !p.inPortal && Hypot(p.pos.x - P.pos.x, p.pos.z - P.pos.z) < REVIVE_R && Mathf.Abs(p.pos.y - P.pos.y) < 2.5f) helped = true;
                P.revive = helped ? P.revive + dt / REVIVE_TIME : Mathf.Max(0, P.revive - dt * 0.5f);
                if (P.revive >= 1) Revive();
                else hint = helped ? $"BEING REVIVED  {Mathf.FloorToInt(P.revive * 100)}%" : "DOWN · CRAWL TO A TEAMMATE (REVOLVER ONLY)";
            }
            else if (!P.inPortal)
                foreach (var p in players)
                    if (p != P && p.downed && Hypot(p.pos.x - P.pos.x, p.pos.z - P.pos.z) < REVIVE_R + 0.4f) { hint = $"REVIVING {p.name}  {Mathf.FloorToInt(p.revive * 100)}%"; break; }
            if (hint != null) { SetHint(hint); coopHint = true; }
            else if (coopHint) { coopHint = false; SetHint(P.inPortal ? "◆ WAITING FOR YOUR TEAM ◆" : null); }
            if (Auth) HostRules();
            // clients: smooth the host's corrections into the local copies of the enemies
            if (!Auth)
                foreach (var e in enemies)
                {
                    if (e.dead || e.netErr == Vector3.zero || !e.g) continue;
                    var step = e.netErr * (1 - Mathf.Exp(-12 * dt)); e.pos += step; e.netErr -= step;
                    if (e.netErr.sqrMagnitude < 1e-6f) e.netErr = Vector3.zero;
                    e.g.transform.localPosition = e.pos;
                }
        }
        bool coopHint;
        // the host's co-op rules: everyone down = game over; everyone standing in the portal = next level
        void HostRules()
        {
            if (won || CINE.active || WARP.active || P.dead) return;
            bool allDown = true; foreach (var p in players) if (!Down(p)) allDown = false;
            if (allDown) { NetEvent(NetEv.GameOver); CoopGameOver(); return; }
            if (portal != null && portal.active && !portal.finale && phase == "portal")
            {
                bool any = false, all = true;
                foreach (var p in players) { if (p.downed) continue; any = true; if (!p.inPortal) all = false; }
                if (any && all) NetWarp(levelNum + 1);
            }
        }
        void CoopGameOver()
        {
            if (P.dead) return;
            P.downed = false; P.inPortal = false; Die();
            if (!locked) ShowDeathScreen(); // sitting in a menu: no death-camera fall to wait for
        }

        // ---------- teammates' bodies: the cutscene hero, animated from the network state ----------
        // Every joint eases toward a target pose (no snapping); the pose is built up from what the player is doing: walk /
        // run, airborne rising or falling, the landing, slides, dashes, the grapple (an arm out to the hook, the rope, the
        // body pulled along it), shooting (recoil, a muzzle flash), reloading, swapping guns, the chainsaw, executions,
        // flinching when hit, downed, dead. They hold the gun they're really using (a copy of its viewmodel).
        class Body
        {
            public Transform root; public HeroRig rig; public Transform marker; public Material markerM; public float walk; public int shots = -1;
            public float recoil, land, hurt, lastHp = 100, airT, execT, flashT, deadT; public bool wasGround = true;
            public Transform rope, flash; public Material flashM; public GameObject[] gunCopies = new GameObject[5]; public int shownGun = -1;
            // the grapple seen from outside: a rope of segments that whips out and snaps taut, a claw at its end; the chainsaw
            // in their hand (for the saw and for executions)
            public Transform[] seg; public Transform claw, saw, sawTip; public Material chainM; public bool wasHook, wasPull; public float hookT, sawSfxT, ripDone;
        }
        const int ROPE_SEGS = 12;
        readonly Dictionary<Player, Body> bodies = new Dictionary<Player, Body>();
        static readonly int[] SLOT_COL = { 0xff3a3a, 0x40c8ff, 0x5aff8a, 0xffd24a };
        Body BodyOf(Player p)
        {
            if (bodies.TryGetValue(p, out var b) && b.root) return b;
            b = new Body { root = new GameObject("teammate " + p.name).transform };
            b.root.SetParent(world, false);
            int pl = Mk.layer; bool pc = Mk.colliders; Mk.layer = Mk.LayerIgnore; Mk.colliders = false;
            b.rig = BuildHero(b.root);
            var ropeM = Mats.Std(0x2a2622, 0.6f, 0.6f);
            var rope = Mk.Msh(world, Geo.Cyl(1, 1, 1, 6), ropeM); b.rope = rope.transform; rope.SetActive(false);
            b.seg = new Transform[ROPE_SEGS]; for (int i = 0; i < ROPE_SEGS; i++) { var sg = Mk.Msh(rope.transform.parent, Geo.Cyl(1, 1, 1, 5), ropeM); sg.layer = Mk.LayerIgnore; sg.SetActive(false); b.seg[i] = sg.transform; }
            {   // the claw: a hub and three hooked tines
                var cl = Mk.Piv(world); var steel = Mats.Std(0x6a6c70, 0.35f, 0.85f); Mk.Msh(cl, Geo.Cyl(0.06f, 0.08f, 0.16f, 8), steel, 0, 0, 0, Mathf.PI / 2, 0, 0);
                for (int k = 0; k < 3; k++) { var tn = Mk.Piv(cl, 0, 0, 0.06f); tn.SetRot(0, 0, k * Mathf.PI * 2 / 3); var a = Mk.Piv(tn, 0, 0.05f, 0); a.SetRot(0.7f, 0, 0); Mk.Msh(a, Geo.Cone(0.025f, 0.2f, 4), steel, 0, 0.1f, 0); }
                Mk.SetLayerRecursive(cl.t.gameObject, Mk.LayerIgnore); cl.t.gameObject.SetActive(false); b.claw = cl.t;
            }
            {   // the chainsaw in the left hand, its bar out along the arm
                var sp = Mk.Piv(b.rig.wrL, 0, -0.06f, 0.04f); var body = Mats.Std(0x8a1a14, 0.5f, 0.4f); var bar = Mats.Std(0x9a9ca0, 0.3f, 0.85f); b.chainM = Mats.Basic(0xff6a20);
                Mk.Msh(sp, Geo.Box(0.13f, 0.2f, 0.24f), body, 0, -0.04f, 0); Mk.Msh(sp, Geo.Box(0.15f, 0.06f, 0.12f), Mats.Std(0x1a1a1a, 0.8f), 0, 0.08f, -0.04f);
                Mk.Msh(sp, Geo.Box(0.03f, 0.72f, 0.1f), bar, 0, -0.5f, 0.02f); Mk.Msh(sp, Geo.Box(0.036f, 0.74f, 0.118f), b.chainM, 0, -0.5f, 0.02f);
                b.sawTip = Mk.Piv(sp, 0, -0.86f, 0.02f).t; Mk.SetLayerRecursive(sp.t.gameObject, Mk.LayerIgnore); sp.t.gameObject.SetActive(false); b.saw = sp.t;
            }
            Mk.layer = pl; Mk.colliders = pc;
            Mk.SetLayerRecursive(b.root.gameObject, Mk.LayerIgnore); // also pulls the hero's glows off the cutscene layer
            rope.layer = Mk.LayerIgnore;
            // toned down for teammates: no cutscene aura, a dimmer core and runes, a small beacon
            if (b.rig.aura) b.rig.aura.SetActive(false);
            if (b.rig.coreMat) SetGlow(b.rig.coreMat, 0xff3a1a, 1.4f); if (b.rig.runeMat) SetGlow(b.rig.runeMat, 0xff2a14, 0.9f);
            if (b.rig.visorGlowMat) Mats.SetAlpha(b.rig.visorGlowMat, 0.05f);
            var mk = Mk.Glow(b.root, SLOT_COL[p.slot & 3], 0.9f, 0.9f); mk.layer = Mk.LayerIgnore; b.marker = mk.transform; b.markerM = mk.GetComponent<Renderer>().sharedMaterial;
            b.marker.localPosition = V(0, 2.45f, 0);
            var fl = Mk.Glow(b.rig.gun.t, 0xffd890, 0.7f, 0); fl.layer = Mk.LayerIgnore; b.flash = fl.transform; b.flashM = fl.GetComponent<Renderer>().sharedMaterial; b.flash.localPosition = V(0, 0, 0.55f);
            bodies[p] = b; return b;
        }
        // the gun a teammate holds: a copy of that weapon's own viewmodel model, in the hero's hand
        void ShowTeamGun(Body b, int w)
        {
            if (b.shownGun == w) return; b.shownGun = w; w = Mathf.Clamp(w, 0, 4);
            for (int i = 0; i < 5; i++) if (b.gunCopies[i]) b.gunCopies[i].SetActive(i == w);
            if (!b.gunCopies[w] && w < guns.Count && guns[w].g)
            {
                var src = guns[w]; var copy = Instantiate(src.g.gameObject, b.rig.gun.t, false); copy.SetActive(true); copy.name = "teamGun" + w;
                foreach (var c in copy.GetComponentsInChildren<Collider>(true)) Destroy(c);
                foreach (var l in copy.GetComponentsInChildren<Light>(true)) Destroy(l);
                foreach (var mb in copy.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(mb);
                foreach (var rr in copy.GetComponentsInChildren<Renderer>(true))
                {
                    var sm = rr.sharedMaterial;
                    if (sm == gloveMat || sm == sleeveMat || (src.flashMat && sm == src.flashMat) || rr.gameObject.name.Contains("glow")) { rr.enabled = false; continue; }
                    rr.gameObject.layer = Mk.LayerIgnore;
                }
                copy.layer = Mk.LayerIgnore; copy.transform.localPosition = V(0, 0, 0.05f); copy.transform.localRotation = Quaternion.Euler(0, 180, 0); copy.transform.localScale = Vector3.one;
                var bb = new Bounds(); bool any = false;
                foreach (var rr in copy.GetComponentsInChildren<MeshRenderer>()) { if (!rr.enabled) continue; if (!any) { bb = rr.bounds; any = true; } else bb.Encapsulate(rr.bounds); }
                float big = any ? Mathf.Max(bb.size.x, Mathf.Max(bb.size.y, bb.size.z)) : 1, want = w == 2 ? 1.05f : w == 3 ? 1.25f : w == 1 ? 0.95f : 0.5f;
                if (big > 1e-3f) copy.transform.localScale = Vector3.one * (want / big);
                b.gunCopies[w] = copy;
            }
            // the hero's own carbine is hidden once a real copy is in hand
            foreach (Transform c in b.rig.gun.t) if (c != b.flash && !c.name.StartsWith("teamGun")) c.gameObject.SetActive(!b.gunCopies[w]);
        }
        void KillBodyBits(Body b) { if (b.rope) Destroy(b.rope.gameObject); if (b.claw) Destroy(b.claw.gameObject); if (b.seg != null) foreach (var s in b.seg) if (s) Destroy(s.gameObject); }
        void DropBody(Player p) { if (bodies.TryGetValue(p, out var b)) { if (b.root) Mk.DestroyTree(b.root.gameObject); KillBodyBits(b); bodies.Remove(p); } }
        void ClearBodies() { foreach (var b in bodies.Values) { if (b.root) Mk.DestroyTree(b.root.gameObject); KillBodyBits(b); } bodies.Clear(); }
        static void SmJ(N n, float x, float y, float z, float k) { Sm(n, 0, x, k); Sm(n, 1, y, k); Sm(n, 2, z, k); }
        void UpdateBodies(float rdt)
        {
            foreach (var p in players)
            {
                if (!p.remote) continue;
                var b = BodyOf(p);
                bool show = started && !p.inPortal && !CINE.active;
                if (b.root.gameObject.activeSelf != show) b.root.gameObject.SetActive(show);
                if (!show) { HideRope(b); continue; }
                var r = b.rig; float hs = Hypot(p.vel.x, p.vel.z), amp = Mathf.Min(1, hs / 7), run = Mathf.Clamp01((hs - 7) / 6);
                float k = 1 - Mathf.Exp(-14 * rdt), kf = 1 - Mathf.Exp(-24 * rdt);
                ShowTeamGun(b, p.weapon);
                bool reload = (p.act & 1) != 0, saw = (p.act & 2) != 0, exec = (p.act & 4) != 0, swap = (p.act & 8) != 0, dash = (p.act & 16) != 0, pull = (p.act & 32) != 0;
                bool air = !p.onGround && !p.downed && !p.sliding;
                // the landing: a dip in the knees, harder the faster they came down
                if (air) b.airT += rdt; else { if (!b.wasGround && b.airT > 0.25f) b.land = Mathf.Clamp01(b.airT * 0.8f); b.airT = 0; }
                b.wasGround = !air; b.land = Mathf.Max(0, b.land - rdt * 3.5f);
                // hit: a flinch whenever their health drops
                if (p.hp < b.lastHp - 0.5f) b.hurt = 1; b.lastHp = p.hp; b.hurt = Mathf.Max(0, b.hurt - rdt * 4);
                b.recoil = Mathf.Max(0, b.recoil - rdt * 9); b.flashT = Mathf.Max(0, b.flashT - rdt);
                b.execT = exec ? b.execT + rdt : 0; b.deadT = p.dead ? b.deadT + rdt : 0;
                // where the body goes and which way it faces (the hero faces +z; the player looks down -z at yaw 0)
                float tilt = p.dead ? Mathf.Min(1, b.deadT * 2.5f) * 85 : p.downed ? 80 : p.sliding ? -18 : dash ? 12 : 0;
                b.root.position = p.pos + (p.downed ? V(0, 0.25f, 0) : Vector3.zero);
                var want = Quaternion.Euler(0, p.yaw * Mathf.Rad2Deg + 180, 0) * Quaternion.Euler(tilt, 0, 0);
                bool hooked = p.hooking && (p.hookPt - p.pos).sqrMagnitude > 1;
                Ent exE = exec && p.execNid != 0 ? FindNid(p.execNid) : null; int K = p.execKind; float tE = b.execT;
                bool ride = K == 4 && exE != null && exE.boss == 4; float rip = K == 0 ? 1.15f : K == 1 ? 0.74f : K == 2 ? 1.05f : ride ? 1.5f : 1.0f;
                // on the rope the whole body leans into it, toward the hook (harder once it bites and hauls them in)
                if (hooked && !p.downed && !p.dead) { var dd0 = (p.hookPt - p.pos).normalized; var up = Vector3.Slerp(Vector3.up, dd0, pull ? 0.55f : 0.2f); var fl = V(dd0.x, 0, dd0.z); if (fl.sqrMagnitude < 0.01f) fl = want * Vector3.forward; var fw = Vector3.ProjectOnPlane(fl, up); if (fw.sqrMagnitude > 1e-4f) want = Quaternion.LookRotation(fw.normalized, up); }
                // an execution: square up to the victim
                else if (exec && exE != null) { var to = exE.pos - p.pos; to.y = 0; if (to.sqrMagnitude > 0.01f) want = Quaternion.LookRotation(to.normalized, Vector3.up); }
                b.root.rotation = Quaternion.Slerp(b.root.rotation, want, 1 - Mathf.Exp(-16 * rdt));
                b.walk += rdt * hs * (p.downed ? 0.9f : 0.5f + run * 0.15f);
                float sw = Mathf.Sin(b.walk), cw = Mathf.Cos(b.walk), rising = p.vel.y > 1 ? 1 : 0;
                // ---- legs ----
                float thL, thR, knL, knR, hipY = 0.95f - Mathf.Abs(cw) * (0.04f + 0.04f * run) * amp - b.land * 0.28f;
                if (p.sliding) { thL = -1.35f; knL = 0.15f; thR = -0.35f; knR = 1.6f; hipY = 0.5f; }                         // front leg out, the back one tucked
                else if (air) { thL = rising > 0 ? -1.1f : -0.5f; knL = rising > 0 ? 1.6f : 0.6f; thR = rising > 0 ? -0.4f : 0.1f; knR = rising > 0 ? 0.9f : 0.4f; } // tucked going up, reaching for the ground coming down
                else if (p.downed) { thL = sw * 0.4f; thR = -sw * 0.4f; knL = 0.6f + Mathf.Max(0, sw) * 0.6f; knR = 0.6f + Mathf.Max(0, -sw) * 0.6f; }
                else { float st = 0.8f + 0.35f * run; thL = -sw * st * amp - b.land * 0.6f; thR = sw * st * amp - b.land * 0.6f; knL = 0.1f + Mathf.Max(0, sw) * (1.1f + run * 0.5f) * amp + b.land * 1.0f; knR = 0.1f + Mathf.Max(0, -sw) * (1.1f + run * 0.5f) * amp + b.land * 1.0f; }
                // zipping along the rope: one knee driven up, the other leg trailing, a little pendulum in them; flying out: tucked
                if (hooked && air) { float wv = Mathf.Sin(Time.unscaledTime * 5 + p.slot); thL = pull ? 0.4f + wv * 0.12f : -0.3f; knL = pull ? 0.85f : 1.0f; thR = pull ? -0.9f - wv * 0.1f : 0.2f; knR = pull ? 1.45f : 0.6f; }
                // an execution: a lunge into a planted stance (or kneeling on the Hollow King's back)
                if (exec) { if (ride) { thL = -1.2f; knL = 1.9f; thR = 0.25f; knR = 1.9f; hipY = 0.55f; } else { float lg = Smoother(tE / 0.2f); thL = Mathf.Lerp(thL, -0.7f, lg); knL = Mathf.Lerp(knL, 0.75f, lg); thR = Mathf.Lerp(thR, 0.55f, lg); knR = Mathf.Lerp(knR, 0.45f, lg); hipY = Mathf.Lerp(hipY, 0.8f, lg); } }
                r.hips.pos = Vector3.Lerp(r.hips.pos, V(0, hipY, 0), kf);
                SmJ(r.thL, thL, 0, 0.05f, kf); SmJ(r.thR, thR, 0, -0.07f, kf); Sm(r.knL, 0, knL, kf); Sm(r.knR, 0, knR, kf);
                // ---- spine and head: a forward lean when running or dashing, back on a slide, a jolt when hit ----
                float lean = 0.08f * amp + 0.2f * run + (dash ? 0.25f : 0) + (p.sliding ? -0.35f : 0) - b.hurt * 0.35f + b.land * 0.25f;
                float twist = saw ? Mathf.Sin(Time.unscaledTime * 30) * 0.06f : 0;
                bool bite = false;
                if (exec)
                {
                    // the saw biting: the whole body shudders with it; the waist cut swings the torso through the sweep
                    bite = tE < rip && tE > (K == 0 ? 0.4f : K == 1 ? 0.45f : K == 2 ? 0.5f : 0.3f);
                    float jit = bite ? (Mathf.PerlinNoise(Time.unscaledTime * 30, p.slot) - 0.5f) * 0.14f : 0;
                    lean = 0.3f + (K == 2 && tE > 0.38f ? 0.25f : 0) + jit + (tE < 0.2f ? 0.35f * Mathf.Sin(tE / 0.2f * Mathf.PI) : 0);
                    twist = K == 1 ? Mathf.Lerp(Mathf.Lerp(0, -0.55f, Smoother(Seg01(tE, 0.2f, 0.42f))), 0.6f, Smoother(Seg01(tE, 0.42f, rip))) * (1 - Smoother(Seg01(tE, rip + 0.1f, rip + 0.5f))) : jit;
                }
                if (hooked && pull && air) lean = 0.05f;
                SmJ(r.spine, lean, twist, b.hurt * 0.15f, k);
                SmJ(r.neck, -p.pitch * 0.3f, 0, 0, k); SmJ(r.head, -p.pitch * 0.4f + b.hurt * 0.3f, 0, 0, k);
                // ---- the gun arm: along the aim, kicked back by each shot; lowered and turned for a reload; dropped out of
                // sight and brought back up for a swap ----
                float shR = -1.45f - p.pitch + b.recoil * 0.35f, elR = -0.15f - b.recoil * 0.25f, zR = -0.1f, wrR = 0.1f;
                if (reload) { shR = -0.7f; elR = -1.2f; zR = 0.35f; wrR = -0.6f + Mathf.Sin(Time.unscaledTime * 10) * 0.1f; }
                if (swap) { shR = -0.2f; elR = -0.6f; }
                if (p.downed) { shR = -1.2f - p.pitch * 0.5f; elR = -0.3f; }
                if (exec) { shR = -0.95f; elR = -1.1f; zR = -0.25f; wrR = 0.3f; } // the gun tucked in while the saw works
                SmJ(r.shR, shR, 0, zR, kf); SmJ(r.elR, elR, 0, 0, kf); Sm(r.wrR, 0, wrR, kf);
                // ---- the free arm: swinging with the stride; out to the hook, along the rope; thrust forward with the
                // chainsaw (which grinds); up to the gun to reload; both arms down in an execution's blow ----
                float shL = sw * 0.5f * amp - 0.2f, elL = -0.6f, zL = 0.18f, yL = 0;
                if (air) { shL = -0.9f; zL = 0.5f; }
                if (reload) { shL = -1.0f; elL = -1.5f; zL = -0.1f; }
                if (saw) { shL = -1.5f - p.pitch + Mathf.Sin(Time.unscaledTime * 40) * 0.05f; elL = -0.05f; zL = 0.05f; }
                if (exec)
                {
                    // the saw arm, the same blows as their own screen: 0 stab the chest and haul it up through the head, 1 one
                    // sweep through the waist, 2 raised high and driven down into the skull, 3/4 carving in (4: a boss)
                    float sj = bite ? (Mathf.PerlinNoise(Time.unscaledTime * 40, 3 + p.slot) - 0.5f) * 0.18f : 0, outT = Smoother(Seg01(tE, rip, rip + 0.3f));
                    switch (K)
                    {
                        case 0: shL = Mathf.Lerp(Mathf.Lerp(-0.5f, -1.45f, Smoother(Seg01(tE, 0.2f, 0.4f))), -2.55f, Smoother(Seg01(tE, 0.42f, rip))); elL = -0.15f; zL = 0.05f; break;
                        case 1: shL = -1.45f; yL = Mathf.Lerp(Mathf.Lerp(0, -1.0f, Smoother(Seg01(tE, 0.2f, 0.42f))), 1.1f, Smoother(Seg01(tE, 0.42f, rip))); elL = -0.1f; zL = 0.1f; break;
                        case 2: { float up = Smoother(Seg01(tE, 0.2f, 0.38f)), dn = Seg01(tE, 0.38f, 0.5f); dn *= dn; shL = Mathf.Lerp(Mathf.Lerp(-0.6f, -2.9f, up), -1.15f, dn); elL = Mathf.Lerp(-1.0f, -0.25f, dn); zL = 0.05f; break; }
                        default: shL = ride ? -0.75f : -1.3f + Smoother(Seg01(tE, 0.2f, rip)) * 0.25f; elL = -0.2f; zL = 0.05f; break;
                    }
                    shL = Mathf.Lerp(shL + sj, -0.5f, outT); yL *= 1 - outT;
                }
                if (hooked)
                {
                    // the arm points at the hook: work out the direction in the body's own frame
                    var d = b.root.InverseTransformDirection((p.hookPt - r.shL.t.position).normalized);
                    shL = -Mathf.Atan2(d.z, -d.y); yL = 0; zL = Mathf.Clamp(Mathf.Atan2(d.x, Mathf.Sqrt(d.y * d.y + d.z * d.z)), -1.2f, 1.2f) + 0.05f; elL = -0.05f;
                    if (pull) { lean += 0.2f; }
                }
                SmJ(r.shL, shL, yL, zL, kf); SmJ(r.elL, elL, 0, 0, kf);
                // the grapple's rope, from the hand to the claw
                DrawRope(b, r, p, hooked, pull, rdt);
                // a coloured beacon over their head; a pulsing red one while downed (it fills as they're revived)
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8);
                Mats.SetColor(b.markerM, p.downed ? 0xff2020 : SLOT_COL[p.slot & 3]); Mats.SetAlpha(b.markerM, p.downed ? 0.35f + 0.35f * pulse : 0.3f);
                b.marker.localScale = Vector3.one * (p.downed ? 0.7f + p.revive * 0.8f : 0.3f);
                Mats.SetAlpha(b.flashM, b.flashT > 0 ? b.flashT / 0.06f : 0); b.flash.localScale = Vector3.one * (0.5f + 0.6f * b.flashT / 0.06f);
                if (b.shots < 0) b.shots = p.shots;
                if (p.shots != b.shots) { int n = Mathf.Min(3, (p.shots - b.shots + 256) % 256); b.shots = p.shots; for (int i = 0; i < n; i++) RemoteShot(p, r); b.recoil = 1; b.flashT = 0.06f; }
                // an execution: the enemy held in front of them, staggered (their machine moves it; ours shows the blows land)
                if (b.saw) { bool sh = saw || exec; if (b.saw.gameObject.activeSelf != sh) b.saw.gameObject.SetActive(sh); if (sh) Mats.SetColor(b.chainM, Color.Lerp(Mats.Hex(0xff4010), Mats.Hex(0xffc060), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 60))); }
                if (!exec) b.ripDone = 0; else RemoteExecFx(b, p, exE, K, tE, rip, bite, rdt);
            }
            foreach (var p in new List<Player>(bodies.Keys)) if (!players.Contains(p)) DropBody(p);
        }        void HideRope(Body b)
        {
            if (b.rope && b.rope.gameObject.activeSelf) b.rope.gameObject.SetActive(false);
            if (b.seg != null) foreach (var s in b.seg) if (s && s.gameObject.activeSelf) s.gameObject.SetActive(false);
            if (b.claw && b.claw.gameObject.activeSelf) b.claw.gameObject.SetActive(false);
        }
        // the rope: segments from the hand to the claw. Flying out it whips behind the claw; when it bites, one hard wave runs
        // down it and dies away as it snaps taut and hauls them in (wind streaming past them)
        void DrawRope(Body b, HeroRig r, Player p, bool hooked, bool pull, float rdt)
        {
            if (b.rope && b.rope.gameObject.activeSelf) b.rope.gameObject.SetActive(false);
            if (!hooked || b.seg == null) { HideRope(b); b.wasHook = b.wasPull = false; b.hookT = 0; return; }
            float vol = Mathf.Clamp01(1 - Vector3.Distance(p.pos, P.pos) / 60);
            if (!b.wasHook) { b.hookT = 0; if (vol > 0.02f) Sfx("hookThrow", 0.5f * vol); }
            if (pull && !b.wasPull) { b.hookT = 0; if (vol > 0.02f) Sfx("hookHit", 0.6f * vol); SpawnParticles(p.hookPt, 8, 5, sparkMat, 0.05f, 0.3f); }
            b.wasHook = true; b.wasPull = pull; b.hookT += rdt;
            var a = r.wrL.t.position; var c = p.hookPt; var d = c - a; float len = d.magnitude; if (len < 0.05f) { HideRope(b); return; }
            var dn = d / len; var side = Vector3.Cross(dn, Vector3.up); if (side.sqrMagnitude < 1e-4f) side = Vector3.right; side.Normalize();
            float amp = pull ? 0.45f * Mathf.Exp(-b.hookT * 7) : 0.2f, sag = pull ? 0.03f * len * Mathf.Exp(-b.hookT * 5) : 0.05f * len;
            var prev = a;
            for (int i = 0; i < ROPE_SEGS; i++)
            {
                float u = (i + 1f) / ROPE_SEGS, env = Mathf.Sin(u * Mathf.PI);
                var pt = a + d * u + side * (Mathf.Sin(u * Mathf.PI * 3 - Time.unscaledTime * 28) * amp * env) + Vector3.down * (sag * env);
                var sg = b.seg[i]; if (!sg.gameObject.activeSelf) sg.gameObject.SetActive(true);
                var sd = pt - prev; float sl = sd.magnitude; sg.position = (prev + pt) * 0.5f; if (sl > 1e-4f) sg.rotation = Quaternion.FromToRotation(Vector3.up, sd / sl); sg.localScale = V(0.024f, sl * 0.5f + 0.01f, 0.024f);
                prev = pt;
            }
            if (!b.claw.gameObject.activeSelf) b.claw.gameObject.SetActive(true);
            b.claw.position = c; b.claw.rotation = Quaternion.LookRotation(dn);
            if (pull && p.vel.sqrMagnitude > 100 && rand < 0.7f) SpawnParticles(p.pos + V(0, 1.1f, 0) + Random.insideUnitSphere * 0.6f, 1, 0.5f, moteMat, 0.04f, 0.25f, 0);
        }
        // an execution seen from outside: blood and sparks off the bar while it bites, the grinding saw, the burst at the rip
        void RemoteExecFx(Body b, Player p, Ent en, int K, float t, float rip, bool bite, float rdt)
        {
            float vol = Mathf.Clamp01(1 - Vector3.Distance(p.pos, P.pos) / 55);
            var tip = b.sawTip ? b.sawTip.position : p.pos + V(0, 1.2f, 0);
            bool alive = en != null && !en.dead;
            if (alive) { en.stun = Mathf.Max(en.stun, 0.3f); if (bite) en.flash = 0.07f; }
            if (t <= rdt + 1e-4f && vol > 0.02f) Sfx("saw", 0.5f * vol);
            if (bite)
            {
                var at = alive ? Vector3.Lerp(tip, K == 2 ? en.pos + V(0, en.headY, 0) : BodyPoint(en), 0.4f) : tip;
                var sideV = b.root.right * (rand < 0.5f ? -1 : 1);
                if (rand < 0.7f) Spray(at, (sideV + V(0, K == 0 ? 1.6f : 1.0f, 0) + Random.insideUnitSphere * 0.3f).normalized, 4, 9, 0.45f, bloodMat, 0.09f, 1.2f);
                if (rand < 0.25f) Spray(at, Vector3.up, 2, 10, 0.6f, gibMat, 0.1f, 1.3f);
                SpawnParticles(at, 2, 7, sparkMat, 0.04f, 0.2f);
                b.sawSfxT -= rdt; if (b.sawSfxT <= 0) { b.sawSfxT = 0.09f; if (vol > 0.02f) Sfx("sawHit", 0.45f * vol); }
            }
            if (b.ripDone == 0 && t >= rip) { b.ripDone = 1; var c = alive ? BodyPoint(en) : tip; SpawnParticles(c, 40, 10, bloodMat, 0.13f, 1.0f); Spray(c, Vector3.up, 10, 12, 0.8f, gibMat, 0.12f, 1.4f); if (vol > 0.02f) Sfx("punch", 0.7f * vol); }
        }
        // a teammate fired: draw it along their aim and play it, quieter with distance (hits are theirs, already sent)
        void RemoteShot(Player p, HeroRig r)
        {
            var aim = V(-Mathf.Sin(p.yaw) * Mathf.Cos(p.pitch), Mathf.Sin(p.pitch), -Mathf.Cos(p.yaw) * Mathf.Cos(p.pitch));
            var eye = EyeOf(p); var from = r.gun != null && r.gun.t ? r.gun.t.position + aim * 0.3f : eye;
            var end = Physics.Raycast(eye, aim, out var h, 300, LEVEL_MASK, QueryTriggerInteraction.Ignore) ? h.point : eye + aim * 300;
            int w = Mathf.Clamp(p.weapon, 0, 4);
            Tracer(from, end, w == 3 ? 0x9ff0ff : 0xfff0a0, w == 3 ? 0.1f : 0.05f, w == 3 ? 0.3f : 0.08f);
            float vol = Mathf.Clamp01(1 - Vector3.Distance(eye, P.pos) / 70) * 0.55f;
            if (vol > 0.02f) Sfx(w == 1 ? "shotgun" : w == 2 ? "minigun" : w == 3 ? "sniper" : "revolver", vol);
        }
    }
}
