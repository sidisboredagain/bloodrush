using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    public partial class Game
    {
        static readonly (string l, string n, string c)[] RANKS = {
            ("D", "DIRTY", "#5ab4ff"), ("C", "CRUEL", "#5aff8a"), ("B", "BRUTAL", "#ffe35a"), ("A", "ANARCHIC", "#ffa25a"),
            ("S", "SAVAGE", "#ff5a5a"), ("SS", "SSADISTIC", "#ff3a3a"), ("SSS", "SSSLAUGHTER", "#ff1a1a"), ("X", "OVERKILL", "#ffd700") };

        // ---------- reset ----------
        void PlacePlayer()
        {
            var s = LEVEL.spawn; var c = LEVEL.center;
            P.pos = s; P.vel = Vector3.zero;
            P.yaw = Mathf.Atan2(-(c.x - s.x), -(c.z - s.z)); P.pitch = 0; P.onGround = false;
            if (P.slot > 0) P.pos += FlatRight * ((P.slot % 2 == 1 ? 2.2f : -2.2f) * ((P.slot + 1) / 2)); // co-op: teammates line up side by side
        }
        void ResetGame()
        {
            ClearTransient();
            P = new Player();
            CoopResetPlayers(); // co-op: the new local player takes our slot in the player list
            PlacePlayer();
            S = new StyleState();
            wave = levelNum - 1; phase = "pre"; waveDelay = LEVEL != null && LEVEL.boss == 3 ? 0.01f : 2.5f; freeze = 0; slowmoT = 0; timeScale = 1; victoryT = 0; runTime = 0; killChain = 0; displayScore = 0; won = false;
            HK.cd = 0; DE.active = false; DE.cd = 0; uiScope.SetActive(false);
            WARP.active = false; WARP.fov = 0; WARP.roll = 0; warpGroup.alpha = 0;
            audioMgr.Stop(0.3f); audioMgr.Hum(0);
            inspectT = 0; fistT = 0; RefillAmmo(); bossIntroT = 0; lastRank = -1;
            laserHeld = laserFiring = laserCooling = false; ShowLaser(false); P.airJumpCd = 0;
            SetWeapon(0);
        }

        // ---------- style / score ----------
        float StyleMult => 1 + Mathf.Max(0, S.rank) * 0.25f;
        void AddStyle(string txt, float pts)
        {
            int gain = Mathf.RoundToInt(pts * StyleMult);
            S.score += gain;
            if (S.rank < 0) { S.rank = 0; S.fill = 0; }
            S.fill += pts / (220 + S.rank * 110);
            while (S.fill >= 1 && S.rank < 7) { S.fill -= 1; S.rank++; }
            if (S.rank == 7) S.fill = Mathf.Min(S.fill, 1);
            S.feed.Insert(0, ("+ " + txt, 3)); if (S.feed.Count > 7) S.feed.RemoveAt(S.feed.Count - 1);
            S.dirty = true;
            if (gain > 0) ScorePop("+" + gain);
        }
        void LoseStyle(float amt)
        {
            if (S.rank < 0) return;
            S.fill -= amt;
            while (S.fill < 0) { if (S.rank > 0) { S.rank--; S.fill += 1; } else { S.rank = -1; S.fill = 0; break; } }
        }
        void UpdateStyle(float dt)
        {
            if (S.rank >= 0)
            {
                S.fill -= dt * (0.07f + S.rank * 0.035f);
                if (S.fill < 0) { if (S.rank > 0) { S.rank--; S.fill = 0.75f; } else { S.rank = -1; S.fill = 0; } }
            }
            int n = S.feed.Count;
            for (int i = 0; i < S.feed.Count; i++) S.feed[i] = (S.feed[i].txt, S.feed[i].t - dt);
            S.feed.RemoveAll(f => f.t <= 0);
            if (S.feed.Count != n) S.dirty = true;
        }

        // ---------- player helpers ----------
        Vector3 EyePos => V(P.pos.x, P.pos.y + P.eye, P.pos.z);
        Vector3 ChestPos => V(P.pos.x, P.pos.y + P.height * 0.55f, P.pos.z);
        Vector3 AimDir => V(-Mathf.Sin(P.yaw) * Mathf.Cos(P.pitch), Mathf.Sin(P.pitch), -Mathf.Cos(P.yaw) * Mathf.Cos(P.pitch));
        Vector3 FlatFwd => V(-Mathf.Sin(P.yaw), 0, -Mathf.Cos(P.yaw));
        Vector3 FlatRight => V(Mathf.Cos(P.yaw), 0, -Mathf.Sin(P.yaw));
        Vector3 GetWish()
        {
            if (BC.active) return Vector3.zero;
            float f = (Key(UnityEngine.InputSystem.Key.W) ? 1 : 0) - (Key(UnityEngine.InputSystem.Key.S) ? 1 : 0);
            float s = (Key(UnityEngine.InputSystem.Key.D) ? 1 : 0) - (Key(UnityEngine.InputSystem.Key.A) ? 1 : 0);
            if (testMode) { f = testWish.x; s = testWish.y; }
            float sy = Mathf.Sin(P.yaw), cy = Mathf.Cos(P.yaw);
            var w = V(-sy * f + cy * s, 0, -cy * f - sy * s);
            return w.sqrMagnitude > 0 ? w.normalized : w;
        }
        const float STEP_UP = 0.78f;
        bool BodyBlocked(Vector3 p, float h) { foreach (var c in colliders) if (Overlaps(p.x - R, p.y, p.z - R, p.x + R, p.y + h, p.z + R, c)) return true; return false; }
        void TryStepUp(Vector3 start, Vector3 v, ref MoveRes res, float dt)
        {
            var up = start + V(0, STEP_UP, 0);
            if (BodyBlocked(up, P.height)) return;
            var hv = V(v.x, 0, v.z); var p2 = up;
            MoveBody(ref p2, ref hv, R, P.height, dt);
            float moved = Hypot(p2.x - start.x, p2.z - start.z), movedLow = Hypot(P.pos.x - start.x, P.pos.z - start.z);
            if (moved <= movedLow + 0.005f) return;
            // settle back down onto whatever we stepped onto
            var down = V(0, -(STEP_UP + 0.05f) / dt, 0); var p3 = p2;
            var r3 = MoveBody(ref p3, ref down, R, P.height, dt);
            if (!r3.ground) return; // nothing to stand on up there: keep the normal blocked move
            P.pos = p3; P.vel = V(hv.x, 0, hv.z); res.ground = true; res.wall = false;
        }
        bool FindWall()
        {
            foreach (var c in colliders)
            {
                if (P.pos.y >= c.max.y - 0.05f || P.pos.y + P.height <= c.min.y) continue;
                float cx = clamp(P.pos.x, c.min.x, c.max.x), cz = clamp(P.pos.z, c.min.z, c.max.z);
                float dx = P.pos.x - cx, dz = P.pos.z - cz, d = Hypot(dx, dz);
                if (d < R + 0.4f && d > 1e-5f) { P.wallNormal = V(dx / d, 0, dz / d); return true; }
            }
            return false;
        }
        void TryDash()
        {
            if (P.dead || P.stamina < 1 || DE.active) return;
            if (HK.state == "pull") ReleaseHook();
            P.stamina -= 1; P.dashT = 0.17f; P.iframes = Mathf.Max(P.iframes, 0.22f);
            var w = GetWish(); P.dashDir = w.sqrMagnitude > 0 ? w : FlatFwd;
            P.sliding = false; P.slamming = false; Sfx("dash");
        }
        void Heal(float n) { if (P.downed) return; P.hp = Mathf.Min(100, P.hp + n); } // co-op: only a teammate gets you back up
        // from: where the hit came from (for the edge indicator); without it the nearest enemy is assumed, unless env (lava etc.)
        void DamagePlayer(float n, Vector3? from = null, bool env = false, bool raw = false)
        {
            if (!raw) n *= (1 + 0.012f * (levelNum - 1)) * CoopDamage; // progressive scaling, and a little more bite per extra co-op player (traps, trains and the Warden's punch pass raw: their damage is exact)
            if (P.dead || P.downed || P.inPortal || P.iframes > 0 || won || WARP.active || bossDeath != null || BossIntroActive) return;
            if (!env)
            {
                if (!from.HasValue) { float bd = 30; foreach (var e in enemies) { if (e.dead) continue; float d = Vector3.Distance(e.pos, P.pos); if (d < bd) { bd = d; from = e.pos; } } }
                if (from.HasValue) ShowDamageFrom(from.Value);
            }
            if (testMode && testOut != null) System.IO.File.AppendAllText(System.IO.Path.Combine(testOut, "dmg.txt"), $"{testT:F1} {n} {(from.HasValue ? from.Value.ToString() : "-")}\n");
            if (GOD) return;
            P.hp -= n; P.iframes = 0.35f; hurtFlash = 0.8f; P.shake = Mathf.Max(P.shake, 0.35f);
            LoseStyle(0.4f); Sfx("hurt");
            if (P.hp <= 0) { if (MP) GoDown(); else Die(); }
        }
        void Die()
        {
            P.hp = 0; P.dead = true;
            if (DE.active) EndDeadeye();
            ReleaseHook(); hookPullK = 0; foreach (var s in speedImgs) s.color = Color.clear;
            audioMgr.Stop(1.2f); audioMgr.Hum(0);
            // the death screen waits for the fall (DeathCamera)
            deathT = 0.0001f; deathEye = P.eye; deathSide = rand < 0.5f ? -1 : 1; deathThud = false;
            Sfx("hurt"); P.shake = Mathf.Max(P.shake, 0.5f);
        }
        float deathT, deathEye, deathSide; bool deathThud;
        const float DEATH_FALL = 0.75f, DEATH_HOLD = 1.9f;
        // the view drops to the floor and topples sideways while the world slows down and the screen bleeds red
        void DeathCamera(float rdt)
        {
            deathT += rdt;
            float k = Mathf.Clamp01(deathT / DEATH_FALL), ease = k * k; // accelerating fall
            float bounce = deathT > DEATH_FALL ? Mathf.Sin(Mathf.Clamp01((deathT - DEATH_FALL) / 0.22f) * Mathf.PI) * 0.07f : 0;
            if (k >= 1 && !deathThud) { deathThud = true; P.shake = 0.45f; Sfx("hurt"); }
            P.shake = Mathf.Max(0, P.shake - rdt * 2); float sh = setShake ? P.shake * P.shake * 0.3f : 0;
            camT.position = V(P.pos.x + (rand - 0.5f) * sh, P.pos.y + Mathf.Lerp(deathEye, 0.22f, ease) + bounce, P.pos.z + (rand - 0.5f) * sh);
            float tilt = Mathf.SmoothStep(0, 1, Mathf.Clamp01(deathT / (DEATH_FALL + 0.15f)));
            camT.rotation = Quaternion.Euler(Mathf.Lerp(P.pitch, 0.3f, tilt) * Mathf.Rad2Deg, P.yaw * Mathf.Rad2Deg, (P.roll + tilt * 1.35f * deathSide) * Mathf.Rad2Deg);
            cam.fieldOfView += (setFov - 12 - cam.fieldOfView) * (1 - Mathf.Exp(-3 * rdt));
            vm.localPosition += V(0, -Mathf.Clamp01(deathT / 0.5f) * 0.9f, 0); // the gun drops out of the hands
            hurtFlash = Mathf.Max(hurtFlash, 0.5f + tilt * 0.5f);
            muzzleLight.intensity *= Mathf.Exp(-30 * rdt);
            if (deathT >= DEATH_HOLD) { deathT = 0; ShowDeathScreen(); }
        }
        void ShowDeathScreen()
        {
            bool best = SaveHi();
            SetDeadStats($"FELL IN <color=#ffd24a>{LEVEL.name}</color> · WAVE <color=#ffd24a>{wave}</color> / {TOTAL_WAVES}\nSCORE <color=#ffd24a>{S.score}</color>{(best ? "  NEW BEST!" : "")}\nBEST <color=#ffd24a>{hiScore}</color>");
            locked = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            ShowScreen("deadScreen");
        }

        void UpdatePlayer(float dt)
        {
            if (P.downed || P.inPortal) { UpdateDowned(dt); return; } // co-op (GameCoop.cs)
            var wish = GetWish(); bool crouch = Key(UnityEngine.InputSystem.Key.C) || (testMode && testCrouch);
            // chilled by ice (frost enemies) or dragged by a hazard (the tide, a time bubble): you move slower
            if (chillT > 0) { chillT -= dt; wish *= 0.55f; if (rand < 0.35f) SpawnParticles(P.pos + V((rand - 0.5f) * 1.2f, 0.3f + rand * 1.4f, (rand - 0.5f) * 1.2f), 1, 0.6f, cyanMat, 0.05f, 0.5f, -1); }
            if (hazSlowT > 0) { hazSlowT -= dt; wish *= 0.6f; }
            slickT -= dt;
            if (PrisonUpdate(dt) || SnareHold(dt)) { wish = Vector3.zero; P.jumpBuf = 0; crouchPressed = false; P.sliding = false; P.slamming = false; P.dashT = 0; } // frozen in the Traitor's ice
            P.coyote -= dt; P.jumpBuf -= dt; P.slamBounceT -= dt; P.iframes -= dt; P.cd -= dt; P.punchCd -= dt; P.knockT -= dt; P.coreCd -= dt; P.airJumpCd -= dt; P.slamCd -= dt;
            P.stamina = Mathf.Min(3, P.stamina + dt * 0.9f);
            P.coins = Mathf.Min(4, P.coins + dt / 1.3f);
            if (P.coreCharging) P.coreChargeT += dt;

            if (crouchPressed && !P.onGround && P.dashT <= 0 && !P.slamming && HK.state != "pull" && P.slamCd <= 0)
            {
                P.slamming = true; P.slamStartY = P.pos.y; P.sliding = false; Sfx("slamStart");
            }
            crouchPressed = false;
            if (P.onGround && crouch && !P.sliding && !P.slamming && P.dashT <= 0)
            {
                float hs = Hypot(P.vel.x, P.vel.z);
                if (hs > 2) P.slideDir = V(P.vel.x, 0, P.vel.z).normalized; else P.slideDir = wish.sqrMagnitude > 0 ? wish : FlatFwd;
                P.sliding = true; Sfx("slide");
            }
            if (P.sliding && (!crouch || !P.onGround)) P.sliding = false;

            if (P.jumpBuf > 0 && HK.state == "pull")
            {
                ReleaseHook(); P.vel.y = Mathf.Max(P.vel.y, JUMP * 0.9f); P.jumpBuf = 0; Sfx("jump");
            }
            else if (P.jumpBuf > 0)
            {
                if (P.onGround || P.coyote > 0)
                {
                    if (P.dashT > 0 && P.stamina >= 1)
                    {
                        P.stamina -= 1; P.dashT = 0;
                        P.vel.x = P.dashDir.x * 30; P.vel.z = P.dashDir.z * 30; P.vel.y = JUMP * 0.8f;
                    }
                    else
                    {
                        P.dashT = 0;
                        float bounce = P.slamBounceT > 0 ? Mathf.Min(P.slamPower * 0.7f, 22) : 0;
                        P.vel.y = JUMP + bounce;
                        if (bounce > 0) AddStyle("SLAM BOUNCE", 0);
                    }
                    P.sliding = false; P.onGround = false; P.coyote = 0; P.jumpBuf = 0; P.slamBounceT = 0; Sfx("jump");
                }
                else if (P.wallJumps < 3 && FindWall())
                {
                    P.vel.x = P.wallNormal.x * 13 + wish.x * 5; P.vel.z = P.wallNormal.z * 13 + wish.z * 5; P.vel.y = JUMP * 0.95f;
                    P.wallJumps++; P.slamming = false; P.jumpBuf = 0; Sfx("jump");
                    var wp = P.pos - P.wallNormal * 0.4f; wp.y = P.pos.y + 1;
                    SpawnParticles(wp, 5, 3, dustMat, 0.12f, 0.4f);
                }
                else if (P.airJumpCd <= 0 && !P.slamming)
                {
                    // double jump: a burst off thin air, steering toward where you're pushing (2s recharge)
                    float keep = 0.6f; P.vel.x = P.vel.x * keep + wish.x * RUN * 0.6f; P.vel.z = P.vel.z * keep + wish.z * RUN * 0.6f;
                    P.vel.y = JUMP * 0.95f; P.airJumpCd = 2; P.jumpBuf = 0; P.dashT = 0; Sfx("jump"); Sfx("pad", 0.4f);
                    ShockRing(P.pos + V(0, 0.1f, 0), 1.6f, THEME.holy ? 0x9fe8ff : 0xff8a40, 0.4f);
                    SpawnParticles(P.pos + V(0, 0.2f, 0), 10, 4, THEME.holy ? cyanMat : emberMat, 0.09f, 0.4f, -2);
                }
            }

            if (HK.state == "pull")
            {
                var to = (HK.pos - ChestPos).normalized;
                P.vel = Vector3.Lerp(P.vel, to * 42, 1 - Mathf.Exp(-9 * dt));
                P.sliding = false; P.slamming = false;
            }
            else if (P.dashT > 0)
            {
                P.dashT -= dt;
                P.vel = V(P.dashDir.x * DASH, -0.5f, P.dashDir.z * DASH);
                if (P.dashT <= 0) { P.vel.x = P.dashDir.x * 17; P.vel.z = P.dashDir.z * 17; }
            }
            else if (P.slamming) P.vel = V(0, -SLAM, 0);
            else if (P.sliding)
            {
                P.vel.x = P.slideDir.x * SLIDE; P.vel.z = P.slideDir.z * SLIDE; P.vel.y -= GRAV * gravK * dt;
                if (rand < 0.5f) SpawnParticles(V(P.pos.x, P.pos.y + 0.1f, P.pos.z), 1, 2, dustMat, 0.1f, 0.3f);
            }
            else if (P.onGround)
            {
                float k = 1 - Mathf.Exp(-(P.knockT > 0 ? 2.5f : slickT > 0 ? 1.6f : 18) * dt); // slick ice: you slide on, and turn slowly
                P.vel.x += (wish.x * RUN - P.vel.x) * k; P.vel.z += (wish.z * RUN - P.vel.z) * k; P.vel.y -= GRAV * gravK * dt;
            }
            else
            {
                float prev = Hypot(P.vel.x, P.vel.z);
                P.vel.x += wish.x * 45 * dt; P.vel.z += wish.z * 45 * dt;
                float now = Hypot(P.vel.x, P.vel.z), cap = Mathf.Max(prev, RUN);
                if (now > cap) { P.vel.x *= cap / now; P.vel.z *= cap / now; }
                P.vel.y = Mathf.Max(P.vel.y - GRAV * gravK * dt, -50);
            }

            bool wasAir = !P.onGround; float fallV = P.vel.y;
            P.height = P.sliding ? 1.0f : 1.8f;
            var start = P.pos; var startV = P.vel; bool grounded = P.onGround;
            var res = MoveBody(ref P.pos, ref P.vel, R, P.height, dt);
            // step-up: walking into a ledge no higher than STEP_UP (a stair, a curb, a ramp's step), lift onto it instead of
            // stopping dead - so stairs, ramps and the broken bridge can be walked without jumping or snagging
            if (res.wall && grounded && startV.y <= 0.5f && Hypot(startV.x, startV.z) > 0.5f) TryStepUp(start, startV, ref res, dt);
            P.onGround = res.ground;
            RoofClamp();
            // hard arena boundary: nothing (hook, knockback, explosions) can carry the player outside the walls
            float lim = LB - R - 0.01f;
            if (P.pos.x > lim) { P.pos.x = lim; if (P.vel.x > 0) P.vel.x = 0; } else if (P.pos.x < -lim) { P.pos.x = -lim; if (P.vel.x < 0) P.vel.x = 0; }
            if (P.pos.z > lim) { P.pos.z = lim; if (P.vel.z > 0) P.vel.z = 0; } else if (P.pos.z < -lim) { P.pos.z = -lim; if (P.vel.z < 0) P.vel.z = 0; }
            if (P.onGround)
            {
                P.coyote = 0.1f; P.wallJumps = 0;
                if (P.slamming) SlamLand();
                else if (wasAir && fallV < -18) P.shake = Mathf.Max(P.shake, 0.1f);
            }
            foreach (var pd in pads)
                if (P.onGround && Mathf.Abs(P.pos.x - pd.x) < 1.4f && Mathf.Abs(P.pos.z - pd.z) < 1.4f && P.pos.y < 0.4f)
                { P.vel.y = pd.v; P.onGround = false; P.coyote = 0; P.sliding = false; Sfx("pad"); }
            if (P.pos.y < 0.25f && InPool(P.pos.x, P.pos.z))
            {
                if (P.iframes <= 0) { DamagePlayer(10, null, true); Sfx(THEME.holy ? "burnHoly" : "burnLava"); }
                P.vel.y = 10; P.onGround = false; P.coyote = 0; P.sliding = false;
                SpawnParticles(V(P.pos.x, 0.2f, P.pos.z), 6, 5, THEME.holy ? cyanMat : emberMat, 0.12f, 0.6f);
            }
            if (P.pos.y < -30) PlacePlayer();
        }
        void SlamLand()
        {
            P.slamming = false; P.slamCd = 2;
            P.slamPower = Mathf.Max(0, P.slamStartY - P.pos.y) + 4; P.slamBounceT = 0.3f;
            float rad = 4.5f + Mathf.Min(P.slamPower * 0.25f, 4);
            foreach (var e in enemies.ToArray())
            {
                if (e.dead) continue;
                if (Hypot(e.pos.x - P.pos.x, e.pos.z - P.pos.z) < rad + (e.type == "boss" ? 2 : 0) && Mathf.Abs(e.pos.y - P.pos.y) < 3)
                {
                    if (e.type != "boss") { e.vel.y = 12; Stagger(e); }
                    DamageEnemy(e, 1.5f, new Hit { slam = true });
                }
            }
            P.shake = Mathf.Max(P.shake, 0.6f); Sfx("slam");
            SpawnParticles(V(P.pos.x, P.pos.y + 0.2f, P.pos.z), 24, 9, dustMat, 0.18f, 0.6f);
            ShockRing(P.pos, rad, 0xffaa66);
        }

        // ---------- grappling hook ----------
        class HookState { public string state = "idle"; public Vector3 pos, dir, offset; public float travel, t, cd; public Ent enemy; }
        readonly HookState HK = new HookState();
        // the hook: a spinning three-pronged claw on a steel chain that whips out, then snaps taut and rattles
        const int CHAIN_LINKS = 420; const float LINK_PITCH = 0.105f;
        Transform hookRope, hookHead, hookSpin; readonly Transform[] chain = new Transform[CHAIN_LINKS];
        Material hookGlowMat; float ropeWave, ropeAge, hookPullK;
        void BuildHook()
        {
            hookRope = new GameObject("hookChain").transform; hookRope.SetParent(world, false);
            var linkMat = G(Mats.Std(0x8a8c94, 0.38f, 0.65f)); var link = Geo.Torus(0.031f, 0.009f, 12, 6);
            for (int i = 0; i < CHAIN_LINKS; i++)
            {
                var l = Mk.Msh(hookRope, link, linkMat).transform; l.localScale = V(1, 1.65f, 1); // oval link, long axis along the chain
                chain[i] = l;
            }
            hookRope.gameObject.SetActive(false);
            var hh = new GameObject("hook"); hookHead = hh.transform; hookHead.SetParent(world, false);
            hookSpin = new GameObject("spin").transform; hookSpin.SetParent(hookHead, false);
            var s = hookSpin; float PI = Mathf.PI;
            Mk.Msh(s, Geo.Cyl(0.05f, 0.065f, 0.2f, 12), gunmetal, 0, 0, -0.04f, PI / 2);                 // shaft
            Mk.Msh(s, Geo.Cone(0.05f, 0.14f, 12), steel, 0, 0, 0.13f, PI / 2);                             // piercing tip
            Mk.Msh(s, Geo.Torus(0.07f, 0.014f, 20, 6), glowOrange, 0, 0, 0.04f);                          // energy ring
            Mk.Msh(s, Geo.Torus(0.06f, 0.01f, 20, 6), brassGun, 0, 0, -0.12f);
            for (int i = 0; i < 3; i++)                                                                    // claws sweeping back
            {
                var c = Mk.Piv(s); c.rz = i * PI * 2 / 3;
                Mk.Msh(c.t, Geo.Cyl(0.012f, 0.022f, 0.2f, 6), steel, 0, 0.09f, -0.06f, PI / 2 + 0.75f);
                Mk.Msh(c.t, Geo.Cone(0.02f, 0.09f, 6), steel, 0, 0.16f, -0.12f, -0.5f);                   // barb pointing out and forward
                Mk.Msh(c.t, Geo.Box(0.008f, 0.008f, 0.12f), glowOrange, 0, 0.1f, -0.06f, 0.75f);           // glowing edge
            }
            hookGlowMat = Mk.Glow(hookHead, 0xffa040, 0.6f, 0.8f).GetComponent<Renderer>().sharedMaterial;
            hookHead.gameObject.SetActive(false);
            BuildHookArm(); BuildReloadProps();
        }
        // the left arm that fires the hook: coat sleeve, gloved hand and a wrist launcher the chain comes out of (points down -z).
        // Holding F, an armoured chainsaw pops out of the sleeve at the elbow: its bar, folded back toward the shoulder, swings
        // forward over the arm and locks along the forearm, reaching well past the fist. On release it folds back away.
        Transform hookArm, hookMuzzle; float hookArmK, hookArmKick;
        readonly List<Transform> sawTeeth = new List<Transform>(); float sawChain, sawRev, sawExt, sawPuffT; Transform sawGroup, sawHousing, sawFan, sawNose, sawExhaust; Material sawBlurM, sawTeethM, sawCoreM;
        const float SAW_REAR = -0.06f, SAW_NOSE = -0.86f, SAW_HB = 0.052f, SAW_DUR = 0.45f;
        bool sawHeld;
        void BuildHookArm()
        {
            hookArm = Mk.Piv(VmRoot()).t; var a = hookArm; float PI = Mathf.PI;
            VMsh(a, Geo.Cyl(0.078f, 0.066f, 0.6f, 16), sleeveMat, 0, 0, 0.22f, PI / 2);                 // sleeve
            VMsh(a, Geo.Torus(0.072f, 0.007f, 20, 6), brassGun, 0, 0, -0.07f);
            VMsh(a, Geo.Cyl(0.07f, 0.07f, 0.12f, 16), gloveMat, 0, 0, -0.12f, PI / 2);                  // leather bracer
            VMsh(a, VR(0.11f, 0.065f, 0.12f, 0.025f), gloveMat, 0, -0.01f, -0.23f);                     // open palm
            for (int i = 0; i < 4; i++) VMsh(a, VR(0.022f, 0.02f, 0.075f, 0.009f), gloveMat, -0.036f + i * 0.024f, -0.015f, -0.32f, -0.15f); // fingers
            VMsh(a, VR(0.022f, 0.02f, 0.06f, 0.009f), gloveMat, 0.062f, -0.01f, -0.24f, 0, -0.6f, 0);    // thumb
            // wrist launcher strapped on top
            VMsh(a, VR(0.06f, 0.03f, 0.16f, 0.01f), gunmetal, 0, 0.065f, -0.12f);
            VMsh(a, Geo.Cyl(0.03f, 0.03f, 0.2f, 14), gunmetal, 0, 0.095f, -0.15f, PI / 2);              // barrel
            VMsh(a, Geo.Cyl(0.036f, 0.033f, 0.03f, 14), steel, 0, 0.095f, -0.255f, PI / 2);             // muzzle
            VMsh(a, Geo.Cyl(0.02f, 0.02f, 0.032f, 12), holeMat, 0, 0.095f, -0.26f, PI / 2);
            VMsh(a, Geo.Torus(0.034f, 0.006f, 18, 6), glowOrange, 0, 0.095f, -0.22f);
            foreach (var z in new[] { -0.07f, -0.16f }) VMsh(a, Geo.Torus(0.072f, 0.008f, 20, 6), brassGun, 0, 0.0f, z);
            VMsh(a, Geo.Cyl(0.024f, 0.024f, 0.06f, 12), brassGun, 0, 0.095f, -0.03f, PI / 2);           // chain drum
            VMsh(a, VR(0.012f, 0.012f, 0.1f, 0.004f), glowOrange, 0.032f, 0.095f, -0.14f);
            hookMuzzle = Marker(a, 0, 0.095f, -0.28f);
            // the elbow chainsaw. The housing sits at the elbow on the side facing the camera; the bar hinges on it.
            sawHousing = Mk.Piv(a, 0.075f, 0.0f, 0.24f).t; var hs = sawHousing;
            VMsh(hs, VR(0.085f, 0.15f, 0.22f, 0.02f), obsidian, 0.012f, 0, 0.03f);                                  // motor block
            VMsh(hs, VR(0.095f, 0.032f, 0.2f, 0.01f), gunmetal, 0.012f, 0.088f, 0.03f);                             // top armour
            VMsh(hs, VR(0.096f, 0.006f, 0.17f, 0.002f), glowOrange, 0.012f, 0.106f, 0.03f);
            VMsh(hs, VR(0.09f, 0.03f, 0.2f, 0.01f), gunmetal, 0.012f, -0.088f, 0.03f);                              // bottom armour
            for (int i = 0; i < 4; i++) VMsh(hs, VR(0.008f, 0.12f, 0.018f, 0.003f), gunmetal, 0.058f, 0, 0.11f - i * 0.03f); // cooling fins
            VMsh(hs, Geo.Cyl(0.052f, 0.052f, 0.014f, 20), gunmetal, 0.057f, 0.0f, -0.045f, 0, 0, PI / 2);            // intake ring
            VMsh(hs, Geo.Cyl(0.04f, 0.04f, 0.008f, 18), glowOrange, 0.06f, 0.0f, -0.045f, 0, 0, PI / 2);             // hot intake behind the fan
            sawFan = Mk.Piv(hs, 0.066f, 0.0f, -0.045f).t;
            for (int i = 0; i < 5; i++) { var bl = VMsh(sawFan, VR(0.006f, 0.07f, 0.018f, 0.003f), obsidian, 0, 0, 0); bl.transform.localRotation = Quaternion.Euler(i * 72, 0, 0) * Quaternion.Euler(0, 25, 0); }
            VMsh(sawFan, Geo.Sphere(0.012f, 10, 8), steel, 0.004f, 0, 0);
            foreach (var ey in new[] { 0.03f, -0.02f }) VMsh(hs, Geo.Cyl(0.013f, 0.016f, 0.12f, 10), steel, -0.01f, 0.07f + ey * 0.4f, 0.16f + ey, -0.9f, 0, 0); // exhaust stacks
            VMsh(hs, Geo.Cyl(0.011f, 0.011f, 0.01f, 10), glowOrange, -0.01f, 0.12f, 0.2f, -0.9f, 0, 0);
            sawExhaust = Marker(hs, -0.01f, 0.13f, 0.21f);
            VMsh(hs, Geo.Torus(0.035f, 0.012f, 18, 6), steel, 0.05f, 0, -0.07f, 0, PI / 2, 0);                   // hinge collar
            // the bar, built along -z from the hinge
            sawGroup = Mk.Piv(hs, 0.05f, 0, -0.07f).t; var sg = sawGroup;
            var barM = G(Mats.Std(0x8c929c, 0.2f, 0.95f)); sawTeethM = G(Mats.Std(0xb4bac4, 0.22f, 0.95f)); sawCoreM = G(Mats.Add(0xff6a18, 0.6f, null, true, true));
            float L = SAW_REAR - SAW_NOSE, mid = (SAW_REAR + SAW_NOSE) / 2;
            VMsh(sg, VR(0.012f, 0.07f, 0.09f, 0.01f), gunmetal, 0, 0, -0.03f);                                       // clamp plate
            VMsh(sg, VR(0.018f, SAW_HB * 2, L, 0.008f), barM, 0, 0, mid);
            foreach (var sy in new[] { -1f, 1f }) VMsh(sg, VR(0.022f, 0.01f, L * 0.94f, 0.003f), gunmetal, 0, sy * (SAW_HB - 0.012f), mid); // rails
            VMsh(sg, VR(0.022f, 0.016f, L * 0.62f, 0.005f), sawCoreM, 0, 0, mid + 0.04f);                              // glowing core slot
            for (int i = 0; i < 4; i++) VMsh(sg, Geo.Cyl(0.011f, 0.011f, 0.026f, 10), holeMat, 0, 0.024f * (i % 2 == 0 ? 1 : -1), -0.2f - i * 0.15f, 0, 0, PI / 2); // cutouts
            VMsh(sg, Geo.Cyl(SAW_HB, SAW_HB, 0.018f, 20), barM, 0, 0, SAW_NOSE, 0, 0, PI / 2);                         // nose
            VMsh(sg, Geo.Torus(SAW_HB * 0.55f, 0.006f, 16, 4), sawCoreM, 0.012f, 0, SAW_NOSE, 0, PI / 2, 0);          // nose sprocket glow
            sawNose = Marker(sg, 0, 0, SAW_NOSE - SAW_HB);
            for (int i = 0; i < 40; i++)
            {
                var tp = Mk.Piv(sg).t; sawTeeth.Add(tp);
                VMsh(tp, Geo.Box(0.026f, 0.016f, 0.024f), sawTeethM, 0, 0.004f, 0);
                VMsh(tp, Geo.Cone(0.011f, 0.03f, 4), sawTeethM, 0, 0.024f, -0.005f, -0.5f, 0, 0);                      // raked cutter
            }
            sawBlurM = G(Mats.Add(0xffa060, 0, null, true, true));
            VMsh(sg, VR(0.03f, SAW_HB * 2 + 0.05f, L + 0.06f, 0.024f), sawBlurM, 0, 0, mid);
            hs.gameObject.SetActive(false);            UpdateSawChain(0);
            Mk.SetLayerRecursive(a.gameObject, Mk.LayerIgnore);
            foreach (var r in a.GetComponentsInChildren<MeshRenderer>(true)) r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
            a.gameObject.SetActive(false);
        }
        // the chain: each tooth rides a racetrack round the bar (top run forward, round the nose, back underneath)
        void UpdateSawChain(float dt)
        {
            float L = SAW_REAR - SAW_NOSE, per = 2 * L + 2 * Mathf.PI * SAW_HB; int n = sawTeeth.Count;
            sawChain = Mathf.Repeat(sawChain + dt * (0.25f + sawRev * 7f), per);
            for (int i = 0; i < n; i++)
            {
                float s = Mathf.Repeat(sawChain + i * per / n, per); float y, z, ny, nz;
                if (s < L) { y = SAW_HB; z = SAW_REAR - s; ny = 1; nz = 0; }
                else if (s < L + Mathf.PI * SAW_HB) { float a = (s - L) / SAW_HB; ny = Mathf.Cos(a); nz = -Mathf.Sin(a); y = ny * SAW_HB; z = SAW_NOSE + nz * SAW_HB; }
                else if (s < 2 * L + Mathf.PI * SAW_HB) { y = -SAW_HB; z = SAW_NOSE + (s - L - Mathf.PI * SAW_HB); ny = -1; nz = 0; }
                else { float a = (s - 2 * L - Mathf.PI * SAW_HB) / SAW_HB; ny = -Mathf.Cos(a); nz = Mathf.Sin(a); y = ny * SAW_HB; z = SAW_REAR + nz * SAW_HB; }
                var t = sawTeeth[i];
                t.localPosition = V(0, y + ny * 0.004f, z + nz * 0.004f);
                t.localRotation = Quaternion.Euler(Mathf.Atan2(nz, ny) * Mathf.Rad2Deg - 25, 0, 0); // raked like real cutters
            }
            if (sawBlurM) Mats.SetAlpha(sawBlurM, Mathf.Clamp01(sawRev - 0.3f) * 0.3f);
            if (sawTeethM) sawTeethM.SetColor("_BaseColor", Color.Lerp(new Color(0.7f, 0.73f, 0.77f), new Color(1f, 0.45f, 0.12f), Mathf.Clamp01(sawRev - 0.2f) * 0.85f)); // teeth glow hot as it screams
            if (sawCoreM) Mats.SetAlpha(sawCoreM, 0.45f + 0.5f * Mathf.Clamp01(sawRev));
        }
        // the arm thrusts out from the lower left while the hook is out (aiming at the hook), reaches for the gun on reloads,
        // and comes in with the saw while F is held; it drops away after
        void UpdateHookArm(float rdt)
        {
            bool inspectHand = false; // one-handed guns are inspected one-handed: an arm reaching across the screen looked wrong
            bool reloadHand = HK.state == "idle" && reloadT > 0; // the left hand does the reloading (see GameReload)
            bool sawing = fistT > 0 && !P.dead && !DE.active;
            bool want = (HK.state != "idle" || inspectHand || reloadHand || sawing) && !P.dead && !DE.active;
            hookArmK += ((want ? 1 : 0) - hookArmK) * (1 - Mathf.Exp(-(want ? 22 : 9) * rdt));
            hookArmKick = Mathf.Max(0, hookArmKick - rdt * 5);
            // F held: the housing pops out of the sleeve at the elbow, then the bar swings forward over the arm and locks
            sawExt = Mathf.MoveTowards(sawExt, sawing && sawHeld ? 1 : 0, rdt * (sawing && sawHeld ? 3.2f : 4f));
            sawRev += ((sawing && sawExt > 0.8f ? 1.4f : 0) - sawRev) * (1 - Mathf.Exp(-(sawing ? 20 : 3) * rdt));
            bool sawOut = sawExt > 0.01f;
            if (sawHousing.gameObject.activeSelf != sawOut) sawHousing.gameObject.SetActive(sawOut);
            if (sawOut)
            {
                float e1 = Smoother(sawExt / 0.3f), e2 = Mathf.Clamp01((sawExt - 0.22f) / 0.78f);
                sawHousing.localPosition = V(Mathf.Lerp(0.02f, 0.075f, e1), 0, 0.24f); sawHousing.localScale = Vector3.one * Mathf.Lerp(0.3f, 1, e1);
                float c1 = 1.70158f, c3 = c1 + 1, u = e2 - 1, back = 1 + c3 * u * u * u + c1 * u * u; // ease-out-back: overshoots, then locks
                sawGroup.localRotation = Quaternion.Euler(180 * (1 - back), 0, 0);
                if (sawGroup.gameObject.activeSelf != e2 > 0.01f) sawGroup.gameObject.SetActive(e2 > 0.01f);
                sawFan.localRotation = Quaternion.Euler(time * (300 + sawRev * 2400), 0, 0);
                UpdateSawChain(rdt);
                if (sawRev > 0.5f)
                {
                    sawPuffT -= rdt; if (sawPuffT <= 0) { sawPuffT = 0.05f; SpawnParticles(sawExhaust.position, 1, 0.6f, emberMat, 0.012f, 0.3f, -2); } // hot sparks from the exhaust
                    if (rand < 0.45f) SpawnParticles(sawNose.position, 1, 4, sparkMat, 0.025f, 0.2f);
                }
            }
            bool show = hookArmK > 0.02f;
            if (hookArm.gameObject.activeSelf != show) hookArm.gameObject.SetActive(show);
            if (!show) return;
            float k = hookArmK, kick = Mathf.Sin(hookArmKick * Mathf.PI) * 0.08f;
            hookArm.localPosition = Vector3.Lerp(V(-0.42f, -0.75f, -0.25f), V(-0.27f, -0.24f, -0.52f - kick), k);
            var baseRot = Quaternion.Euler(4, -6, -20); // a little inward, turned so the launcher shows
            if (HK.state != "idle")
            {
                var d = VmRoot().InverseTransformPoint(HK.pos) - hookArm.localPosition; // hook in viewmodel space
                if (d.sqrMagnitude > 1e-4f && d.z < 0)
                {
                    var aim = Quaternion.LookRotation(-d.normalized, Vector3.up) * Quaternion.Euler(0, 0, -20);
                    baseRot = Quaternion.Slerp(baseRot, aim, 0.7f);
                }
            }
            else if (sawing)
            {
                // the saw: the arm drives the bar forward and across, shuddering while it bites
                var shudder = V(rand - 0.5f, rand - 0.5f, rand - 0.5f) * 0.01f * sawRev;
                hookArm.localPosition = Vector3.Lerp(V(-0.42f, -0.75f, -0.25f), V(-0.2f, -0.29f, -0.56f) + shudder, k);
                baseRot = Quaternion.Euler(6 + Mathf.Sin(time * 40) * 1.5f * sawRev, -12, 40);
                if (EX.active) { ExecArmPose(out var ep, out var er); hookArm.localPosition = Vector3.Lerp(V(-0.42f, -0.75f, -0.25f), ep, k); baseRot = er; }
            }
            hookArm.localRotation = Quaternion.Slerp(Quaternion.Euler(-40, 10, -30), baseRot, k);
            if (inspectHand)
            {
                // the free hand reaches to the actual gun (the revolver's cylinder / BANG's slide) and flicks it into the spin
                float p = Mathf.Clamp01(1 - inspectT / INSPECT_DUR);
                float reach = Smoother((p - 0.18f) / 0.2f) * (1 - Smoother((p - 0.62f) / 0.22f));
                var target = P.weapon == 0 ? revCyl.t.position : bangSlide.t.position;
                HandTo(target, reach, -15); reloadHandOn = false;
                float flick = Mathf.Sin(Mathf.Clamp01((p - 0.38f) / 0.2f) * Mathf.PI);
                hookArm.localPosition = reloadHandPos + V(flick * 0.04f, flick * 0.03f, 0);
                hookArm.localRotation = reloadHandRot * Quaternion.Euler(0, 0, -flick * 25);
            }
            if (reloadHand && !sawing)
            {
                hookArm.localPosition = reloadHandOn ? reloadHandPos : V(-0.34f, -0.66f, -0.42f);
                hookArm.localRotation = reloadHandOn ? reloadHandRot : Quaternion.Euler(-40, 10, -30);
            }
        }
        void ThrowHook()
        {
            if (P == null || P.dead || HK.state != "idle" || HK.cd > 0 || DE.active || WARP.active) return;
            HK.state = "fly"; HK.pos = EyePos; HK.dir = AimDir; HK.travel = 0; HK.t = 0; HK.enemy = null; Sfx("hookThrow");
            ropeWave = 0.45f; ropeAge = 0; hookArmKick = 1;
        }
        void HookImpact(Vector3 p)
        {
            SpawnParticles(p, 14, 9, sparkMat, 0.07f, 0.35f);
            ShockRing(p, 1.4f, 0xffa040, 0.9f);
            P.shake = Mathf.Max(P.shake, 0.22f); P.kick += 0.02f;
            ropeWave = 0.12f; // the cable snaps taut with a last ripple
        }
        void ReleaseHook()
        {
            if (HK.state == "idle") return;
            if (HK.state == "pull" && P != null) P.vel.y += 3;
            HK.state = "idle"; HK.enemy = null; HK.cd = 0.3f;
        }
        void UpdateHook(float dt)
        {
            HK.cd -= dt;
            if (HK.state == "fly")
            {
                float step = 150 * dt;
                var h = Hitscan(HK.pos, HK.dir, step, false, false);
                if (h.enemy == null) // forgiving catch: a near miss still grabs an enemy (closer than any wall hit)
                {
                    float best = h.level ? Vector3.Distance(HK.pos, h.point) : step;
                    foreach (var o in enemies)
                    {
                        if (o.dead || o.spawnT > 0) continue;
                        var bp = BodyPoint(o); float along = Vector3.Dot(bp - HK.pos, HK.dir);
                        if (along < 0 || along > best) continue;
                        if ((HK.pos + HK.dir * along - bp).magnitude < o.hitR + 0.6f) { best = along; h.enemy = o; h.point = HK.pos + HK.dir * along; }
                    }
                }
                if (h.enemy != null)
                {
                    var e = h.enemy;
                    HK.pos = h.point; Sfx("hookHit"); HookImpact(h.point);
                    if (e.heavy) { HK.state = "pull"; HK.enemy = e; HK.offset = h.point - e.pos; HK.t = 0; }
                    else
                    {
                        // light enemies get reeled all the way in to you (see "yank")
                        e.attackT = 0; e.leaping = false; e.charging = false; e.vel.y += 5;
                        HK.state = "yank"; HK.enemy = e; HK.t = 1.2f;
                    }
                }
                else if (h.level)
                {
                    // only surfaces inside the arena can be grabbed (no hooking wall tops to escape)
                    if (!InBounds(h.point, 0.05f)) { HK.state = "idle"; HK.cd = 0.3f; Sfx("deny"); return; }
                    HK.state = "pull"; HK.enemy = null; HK.pos = h.point; HK.t = 0; Sfx("hookHit");
                    HookImpact(h.point);
                }
                else
                {
                    HK.pos += HK.dir * step; HK.travel += step;
                    if (HK.travel > 85 || !InBounds(HK.pos, 0.5f)) { HK.state = "idle"; HK.cd = 0.3f; }
                }
            }
            else if (HK.state == "yank")
            {
                HK.t -= dt;
                var e = HK.enemy;
                if (e != null && !e.dead)
                {
                    // reel: drag the enemy toward you, fast, until it's in front of you
                    var target = ChestPos + FlatFwd * (1.6f + e.hitR); var to = target - e.pos; float dist = to.magnitude;
                    e.stun = Mathf.Max(e.stun, 0.35f); e.attackT = 0; e.leaping = false;
                    if (dist > 0.5f)
                    {
                        float sp = Mathf.Min(38, 14 + dist * 2.2f);
                        if (e.type == "husk") { e.vel = to / dist * sp; e.vel.y = Mathf.Max(e.vel.y, to.y / dist * sp + 2); } // physics keeps it out of walls
                        else { e.pos += to / dist * Mathf.Min(dist, sp * dt); e.vel = Vector3.zero; }                   // drones fly straight in
                    }
                    HK.pos = BodyPoint(e);
                    if (dist < 0.8f || Vector3.Distance(BodyPoint(e), ChestPos) < 2.2f + e.hitR) { e.vel *= 0.15f; Stagger(e); HK.t = 0; } // arrives staggered, ready to be executed
                }
                if (HK.t <= 0 || e == null || e.dead) { HK.state = "idle"; HK.enemy = null; HK.cd = 0.3f; }
            }
            else if (HK.state == "pull")
            {
                HK.t += dt;
                if (HK.enemy != null) { if (HK.enemy.dead) { ReleaseHook(); return; } HK.pos = HK.enemy.pos + HK.offset; }
                float dist = Vector3.Distance(HK.pos, ChestPos);
                if (dist < (HK.enemy != null ? 2 + HK.enemy.hitR : 1.8f) || HK.t > 2.5f) ReleaseHook();
            }
        }
        // chain point at t (0 = hand, 1 = hook): a straight line plus a travelling whip wave while it flies, a taut buzz while pulling
        Vector3 ChainPoint(Vector3 a, Vector3 b, Vector3 side, Vector3 up, float t)
        {
            var p = Vector3.Lerp(a, b, t);
            float env = Mathf.Sin(t * Mathf.PI);
            float wave = ropeWave * env * Mathf.Sin(t * 13 - ropeAge * 26);
            float buzz = HK.state == "pull" ? 0.015f * env * Mathf.Sin(time * 70 + t * 40) : 0;
            float sag = HK.state != "pull" ? 0 : -0.25f * env * Mathf.Clamp01(1 - HK.t * 4); // brief slack right after it bites
            return p + side * (wave + buzz) + up * (wave * 0.5f + sag);
        }
        void DrawHook()
        {
            float rdt = testRec ? 1 / 60f : Mathf.Min(0.05f, Time.unscaledDeltaTime);
            if (P != null && started) UpdateHookArm(rdt); else hookArm.gameObject.SetActive(false);
            if (P == null || HK.state == "idle" || !started) { hookRope.gameObject.SetActive(false); hookHead.gameObject.SetActive(false); return; }
            ropeAge += rdt; ropeWave *= Mathf.Exp(-(HK.state == "fly" ? 3 : 9) * rdt);
            var hand = hookArm.gameObject.activeSelf ? hookMuzzle.position : camT.position + camT.rotation * V(-0.3f, -0.25f, -0.5f); // out of the wrist launcher
            var dir = HK.pos - hand; float len = dir.magnitude; var fwd = len > 1e-4f ? dir / len : Vector3.forward;
            var side = Vector3.Cross(fwd, Vector3.up); if (side.sqrMagnitude < 1e-4f) side = Vector3.right; side.Normalize();
            var up = Vector3.Cross(side, fwd);
            hookRope.gameObject.SetActive(true); hookHead.gameObject.SetActive(true);
            // links every LINK_PITCH (spread further apart only on very long throws), each turned 90° from the last
            float pitch = Mathf.Max(LINK_PITCH, len / CHAIN_LINKS); int n = Mathf.Min(CHAIN_LINKS, Mathf.CeilToInt(len / pitch));
            for (int i = 0; i < CHAIN_LINKS; i++)
            {
                var l = chain[i]; bool on = i < n; if (l.gameObject.activeSelf != on) l.gameObject.SetActive(on); if (!on) continue;
                float t0 = i * pitch / len, t1 = Mathf.Min(1, (i + 1) * pitch / len);
                Vector3 p0 = ChainPoint(hand, HK.pos, side, up, t0), p1 = ChainPoint(hand, HK.pos, side, up, t1), tan = p1 - p0;
                if (tan.sqrMagnitude < 1e-8f) tan = fwd; tan.Normalize();
                var s1 = Vector3.Cross(tan, up); if (s1.sqrMagnitude < 1e-4f) s1 = side; s1.Normalize();
                l.position = (p0 + p1) * 0.5f;
                l.rotation = Quaternion.LookRotation((i & 1) == 0 ? s1 : Vector3.Cross(tan, s1), tan); // ring plane holds the chain direction
            }
            hookHead.position = HK.pos; hookHead.rotation = Quaternion.LookRotation(HK.state == "fly" ? HK.dir : fwd);
            hookSpin.localRotation = Quaternion.Euler(0, 0, HK.state == "fly" ? time * 1400 : 0);
            float near = Mathf.Clamp01((len - 1.5f) / 4); // fades out as you arrive, so it never floods the screen
            Mats.SetAlpha(hookGlowMat, (HK.state == "pull" ? 0.55f + Mathf.Sin(time * 40) * 0.2f : 0.75f) * near);
        }
    }
}
