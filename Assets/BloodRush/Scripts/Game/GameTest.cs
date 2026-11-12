using System;
using System.IO;
using UnityEngine;

namespace BR
{
    // Command-line autopilot used to verify builds:  BloodRush.exe -brtest <seconds> -brout <dir> [-brlevel n] [-brcine] [-brshot s]
    // Starts a run, auto-aims and fires at the nearest enemy (god mode), saves screenshots and quits.
    public partial class Game
    {
        bool walkInit; float walkLogT, bxNext = 4; int bxStun;
        bool testMode, testCrouch, testCine, testKill, testGuns, testDie, testLast2, testHook, testCore; float testHookT; int testHookN; Vector2 testWish; float testDur, testT, testShotT, testShotEvery = 2; int testShotN, testLevel; string testOut;
        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length; i++) if (a[i] == name) return i + 1 < a.Length ? a[i + 1] : "";
            return null;
        }
        void TestAwake()
        {
            var t = Arg("-brtest"); if (t == null) return;
            testMode = true; testDur = float.TryParse(t, out var d) ? d : 20;
            testOut = Arg("-brout") ?? Path.Combine(Application.persistentDataPath, "shots");
            Directory.CreateDirectory(testOut);
            testLevel = int.TryParse(Arg("-brlevel") ?? "0", out var l) ? l : 0;
            testCine = Arg("-brcine") != null; testKill = Arg("-brkill") != null; testGuns = Arg("-brguns") != null; testDie = Arg("-brdie") != null; testLast2 = Arg("-brlast2") != null; testHook = Arg("-brhook") != null; testCore = Arg("-brcore") != null;
            if (float.TryParse(Arg("-brshot") ?? "", out var se)) testShotEvery = se;
            if (Arg("-brrec") != null) { testRec = true; Time.captureFramerate = 60; Directory.CreateDirectory(Path.Combine(testOut, "rec")); }
            GOD = true;
            Application.logMessageReceived += (msg, st, type) =>
            {
                if (type == LogType.Exception || type == LogType.Error)
                    File.AppendAllText(Path.Combine(testOut, "errors.txt"), $"[{type}] {msg}\n{st}\n");
            };
            File.WriteAllText(Path.Combine(testOut, "errors.txt"), "");
        }
        void TestShot(string name) => ScreenCapture.CaptureScreenshot(Path.Combine(testOut, $"{testShotN++:00}_{name}.png"));
        // -brrec: fixed 60 fps clock, and every frame of a boss intro (plus a second either side) saved to <out>/rec for video
        bool testRec; int testRecN; float testRecTail;
        void TestUpdate(float rdt)
        {
            if (!testMode) return;
            testT += rdt;
            if (float.TryParse(Arg("-brfov") ?? "", out var tf)) setFov = tf; // -brfov n: force the FOV setting
            if (testRec && CINE.active && CINE.shot >= 0 && CINE.shot < SHOTS.Count && (SHOTS[CINE.shot].view == "world" || SHOTS[CINE.shot].view == "hero"))
                ScreenCapture.CaptureScreenshot(Path.Combine(testOut, "rec", $"{SHOTS[CINE.shot].view}_{testRecN++:00000}.png")); // the space and hero shots of the ending
            else if (testRec && started && float.TryParse(Arg("-brrecfrom") ?? "", out var rf)) // -brrecfrom a -brrecto b: plain gameplay between those test times
            {
                float rt = float.TryParse(Arg("-brrecto") ?? "", out var rr) ? rr : rf + 10;
                if (testT >= rf && testT <= rt) ScreenCapture.CaptureScreenshot(Path.Combine(testOut, "rec", $"play_{testRecN++:00000}.png"));
            }
            else if (testRec && started)
            {
                if (BossIntroActive) testRecTail = 1.0f; else testRecTail -= rdt;
                if (testRecTail > 0 || (boss != null && testRecN == 0 && boss.state == "intro")) ScreenCapture.CaptureScreenshot(Path.Combine(testOut, "rec", $"{testRecN++:00000}.png"));
            }
            if (!started && testT > 1.0f && testShotN == 0) TestShot("menu");
            if (CoopTest()) return; // -brhost / -brjoin: co-op over LAN (GameCoopTest.cs)
            if (!started && testT > 1.5f)
            {
                practiceWave = testLevel;
                StartGame(); locked = true;
                if (testCine) { won = true; StartCutscene(); }
                return;
            }
            if (!started) return;
            if (P.dead) { P = P; }
            locked = (!P.dead || deathT > 0) && !won;
            if (testDie && testT > 6 && !P.dead) { GOD = false; P.iframes = 0; DamagePlayer(999); } // -brdie: check the death animation
            if (Arg("-brbossexec") != null && boss != null && !boss.dead && !boss.invuln) // -brbossexec: force a stun every 7s, then spam F at it (logs every execution that starts)
            {
                if (!EX.active && boss.state != "stunned" && testT > bxNext) { bxNext = testT + 7; boss.state = "stunned"; boss.stateT = 3; if (boss.hk != null) boss.hk.t = 0; bxStun++; File.AppendAllText(Path.Combine(testOut, "events.txt"), $"{testT:F1} stun #{bxStun}\n"); }
                if (boss.state == "stunned" && !EX.active)
                {
                    var away = P.pos - boss.pos; away.y = 0; away = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward;
                    P.pos = boss.pos + away * (boss.hitR + 2.5f); P.pos.y = boss.pos.y; P.vel = Vector3.zero;
                    var dd = BodyPoint(boss) - EyePos; P.yaw = Mathf.Atan2(-dd.x, -dd.z); P.pitch = Mathf.Atan2(dd.y, Hypot(dd.x, dd.z)) * 0.5f;
                }
                if (TryExecute()) File.AppendAllText(Path.Combine(testOut, "events.txt"), $"{testT:F1} EXECUTION on stun #{bxStun} hp={boss.hp:F0}\n");
            }
            if (Arg("-brcharge") != null && boss != null && boss.hk != null && !boss.dead && boss.state == "chase" && testT > bxNext) // -brcharge: send the Hollow King charging at the nearest standing building every 8s
            {
                HBuild nb = null; float nbd = 1e9f; foreach (var hb in hkBuilds) if (!hb.broken) { float dd = Vector3.Distance(hb.c, boss.pos); if (dd < nbd) { nbd = dd; nb = hb; } }
                if (nb != null) { bxNext = testT + 8; var to = nb.c - boss.pos; to.y = 0; boss.hk.aim = to.normalized; HState(boss, "charge", 4f); boss.chargeFrom = boss.pos; File.AppendAllText(Path.Combine(testOut, "events.txt"), $"{testT:F1} charge at {nb.c}\n"); }
            }
            if (Arg("-brcharge") != null && boss != null && boss.state == "stunned") File.AppendAllText(Path.Combine(testOut, "events.txt"), $"{testT:F1} stunned t={boss.stateT:F2}\n");
            if (testLast2 && phase == "fight") // -brlast2: leave two enemies alive and just look at them (checks the see-through outline)
            {
                int alive = 0;
                foreach (var e in enemies.ToArray()) if (!e.dead && e.spawnT <= 0 && e.type == "husk") { if (++alive > 2) DamageEnemy(e, 99999, new Hit()); }
                foreach (var e in enemies.ToArray()) if (!e.dead && e.spawnT <= 0 && e.type != "husk") DamageEnemy(e, 99999, new Hit());
                testWish = Vector2.zero; mouseDown = false;
                foreach (var e in enemies) if (!e.dead) { var dir = (TargetPoint(e) - EyePos).normalized; P.yaw = Mathf.Atan2(-dir.x, -dir.z); P.pitch = Mathf.Asin(Mathf.Clamp(dir.y, -1, 1)); break; }
                testShotT += rdt;
                if (testShotT >= testShotEvery) { testShotT = 0; TestShot($"last2_{enemies.Count}"); }
                if (testT > testDur) Application.Quit();
                return;
            }
            if (P.dead)
            {
                mouseDown = false; testShotT += rdt;
                if (testShotT >= testShotEvery) { testShotT = 0; TestShot(deathT > 0 ? "dying" : "dead"); }
                if (testT > testDur) Application.Quit();
                return;
            }
            if (Arg("-brinspect") != null || Arg("-brheat") != null) // inspect each weapon in turn / spin the minigun up hot then let it cool
            {
                testWish = Vector2.zero; P.pitch = 0.02f;
                foreach (var e in enemies.ToArray()) if (!e.dead && e.spawnT <= 0) DamageEnemy(e, 99999, new Hit());
                if (Arg("-brheat") != null) { if (P.weapon != 2) SetWeapon(2); mouseDown = testT % 7 < 4.5f; P.overheat = 0; P.heat = Mathf.Min(P.heat, 0.97f); }
                else
                {
                    mouseDown = false; int wi = (int)(testT / 3.2f) % 5;
                    if (wi == 4 && P.cheatT <= 0) ActivateBang(); else if (wi != 4 && P.weapon != wi) { P.cheatT = 0; SetWeapon(wi); }
                    if (inspectT <= 0 && swapT <= 0 && testT % 3.2f > 0.2f && testT % 3.2f < 0.4f) StartInspect();
                }
                testShotT += rdt;
                if (testShotT >= testShotEvery) { testShotT = 0; TestShot($"insp_w{P.weapon}"); }
                if (testT > testDur) Application.Quit();
                return;
            }
            if (Arg("-brwalk") != null) // -brwalk "x,y,z,yaw": start there and walk forward (W held), logging the path to walk.txt
            {
                var v = System.Array.ConvertAll(Arg("-brwalk").Split(','), x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture));
                GOD = true; mouseDown = false;
                if (!walkInit) { walkInit = true; P.pos = V(v[0], v[1], v[2]); P.vel = Vector3.zero; }
                P.yaw = v[3]; P.pitch = -0.15f; testWish = new Vector2(1, 0);
                foreach (var pm in enemies.ToArray()) if (!pm.dead && pm.spawnT <= 0 && pm.type != "boss") DamageEnemy(pm, 99999, new Hit());
                walkLogT += rdt; if (walkLogT > 0.25f) { walkLogT = 0; File.AppendAllText(Path.Combine(testOut, "walk.txt"), $"{testT:F2} {P.pos.x:F2} {P.pos.y:F2} {P.pos.z:F2} g={P.onGround}\n"); }
                testShotT += rdt; if (testShotT >= testShotEvery) { testShotT = 0; TestShot($"walk_L{levelNum}"); }
                if (testT > testDur) Application.Quit();
                return;
            }
            if (Arg("-brpose") != null) // -brpose "x,y,z,tx,ty,tz+...": stand at each point in turn (one per shot) looking at its target
            {
                var ps = Arg("-brpose").Split('+'); int n = ps.Length; int idx = Mathf.Min(n - 1, (int)(testT / Mathf.Max(0.1f, testShotEvery)));
                var v = System.Array.ConvertAll(ps[idx].Split(','), x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture));
                GOD = true; testWish = Vector2.zero; mouseDown = false; P.pos = V(v[0], v[1], v[2]); P.vel = Vector3.zero;
                var d = V(v[3], v[4], v[5]) - EyePos; P.yaw = Mathf.Atan2(-d.x, -d.z); P.pitch = Mathf.Asin(Mathf.Clamp(d.normalized.y, -1, 1));
                if (Arg("-brmobs") == null) { foreach (var pm in enemies.ToArray()) if (!pm.dead && pm.spawnT <= 0 && pm.type != "boss") DamageEnemy(pm, 99999, new Hit()); }
                else { int i = 0; var ord = new System.Collections.Generic.List<Ent>(enemies); ord.Sort((x1, x2) => (x2.brute ? 1 : 0) - (x1.brute ? 1 : 0)); foreach (var pm in ord) { if (pm.dead || pm.type != "husk") continue; var f = FlatFwd; var sd = V(f.z, 0, -f.x); var spot = V(v[3], 0, v[5]) + V(((i % 3) - 1) * (pm.brute ? 3.4f : 2.0f), 0, -(i / 3) * 3.5f - (pm.brute ? 2.5f : 0)); pm.pos = V(spot.x, 0, spot.z); pm.vel = Vector3.zero; pm.stun = 99; pm.yaw = Mathf.Atan2(P.pos.x - pm.pos.x, P.pos.z - pm.pos.z); i++; if (i > 5) pm.pos = V(0, -50, 0); } }
                testShotT += rdt; if (testShotT >= testShotEvery) { testShotT = 0; TestShot($"pose_L{levelNum}"); }
                if (testT > testDur) Application.Quit();
                return;
            }
            if (Arg("-brlook") != null) // -brlook: no enemies, slowly pan round the skyline (backdrops, god rays)
            {
                testWish = Vector2.zero; mouseDown = false; P.pitch = (Arg("-brlow") != null ? -0.12f : 0.22f) + Mathf.Sin(testT * 0.4f) * 0.12f; P.yaw = testT * 0.45f;
                foreach (var e in enemies.ToArray()) if (!e.dead && e.spawnT <= 0) DamageEnemy(e, 99999, new Hit());
                testShotT += rdt;
                if (testShotT >= testShotEvery) { testShotT = 0; TestShot($"look_L{levelNum}"); }
                if (testT > testDur) Application.Quit();
                return;
            }
            if (Arg("-brreload") != null) // -brreload: empty and reload the revolver, shotgun and sniper in turn (low HP shows the crack)
            {
                testWish = Vector2.zero; mouseDown = false; P.hp = 22; P.pitch = -0.05f;
                foreach (var e in enemies.ToArray()) if (!e.dead && e.spawnT <= 0) DamageEnemy(e, 99999, new Hit());
                int[] ws = { 0, 1, 3 }; int wi = ws[(int)(testT / 3.2f) % 3];
                if (P.weapon != wi) SetWeapon(wi);
                else if (reloadT <= 0 && swapT <= 0 && ammo[wi] == MAG[wi] && (testT % 3.2f) > 0.4f && (testT % 3.2f) < 1) { ammo[wi] = wi == 1 ? 1 : 0; StartReload(); }
                testShotT += rdt;
                if (testShotT >= testShotEvery) { testShotT = 0; TestShot($"reload_w{P.weapon}"); }
                if (testT > testDur) Application.Quit();
                return;
            }
            if (testGuns) // -brguns: stand still and show each viewmodel in turn (then a tossed coin)
            {
                testWish = Vector2.zero; mouseDown = false;
                foreach (var e in enemies.ToArray()) if (!e.dead && e.spawnT <= 0) DamageEnemy(e, 99999, new Hit()); // keep the view clear
                int wi = (int)(testT / 2.5f) % 6; P.pitch = wi == 5 ? 0.35f : -0.05f;
                if (wi < 5 && P.weapon != wi) SetWeapon(wi);
                if (wi == 5 && P.weapon != 0) SetWeapon(0);
                if (wi == 5 && testT % 2.5f > 1.2f && coins.Count == 0) { P.coins = Mathf.Max(P.coins, 1); ThrowCoin(); }
                testShotT += rdt;
                if (testShotT >= 2.5f) { testShotT = 0; TestShot(wi == 5 ? "coin" : guns[P.weapon].name); }
                if (testT > testDur) Application.Quit();
                return;
            }
            if (Arg("-brsky") != null) // -brsky: stand still, look up and pan slowly round the sky (wave 4 wyverns, wave 8 clouds)
            {
                testWish = Vector2.zero; mouseDown = false; GOD = true;
                foreach (var e in enemies.ToArray()) if (!e.dead && e.spawnT <= 0) DamageEnemy(e, 99999, new Hit());
                P.pitch = float.TryParse(Arg("-brpitch") ?? "", out var pp) ? pp : 0.6f; P.yaw += rdt * 0.4f;
                testShotT += rdt; if (testShotT >= testShotEvery) { testShotT = 0; TestShot($"sky{levelNum}"); }
                if (testT > testDur) Application.Quit();
                return;
            }
            // autopilot: strafe around, aim at the nearest enemy and hold the trigger
            testWish = new Vector2(Mathf.Sin(testT * 0.7f) > -0.3f ? 1 : 0, Mathf.Sin(testT * 1.3f));
            Ent best = null; float bd = 1e9f;
            foreach (var e in enemies) { if (e.dead || e.spawnT > 0) continue; float dd = Vector3.Distance(e.pos, P.pos); if (dd < bd) { bd = dd; best = e; } }
            if (best != null && !DE.active && !EX.active)
            {
                var dir = (TargetPoint(best) - EyePos).normalized;
                P.yaw = Mathf.Atan2(-dir.x, -dir.z); P.pitch = Mathf.Asin(Mathf.Clamp(dir.y, -1, 1));
                mouseDown = true;
                bool fixedGun = Arg("-brsaw") != null || Arg("-brlaser") != null;
                if (int.TryParse(Arg("-brgun") ?? "", out var fg)) { if (P.weapon != fg && swapT <= 0) SetWeapon(fg); } // -brgun n: stick to one gun
                else if (!fixedGun && (int)(testT / 6) % 4 == 3 && P.weapon != 2) SetWeapon(2);
                else if (!fixedGun && (int)(testT / 6) % 4 == 1 && P.weapon != 1) SetWeapon(1);
            }
            else mouseDown = false;
            if (testCore) // -brcore: lob a core every 3 s and detonate it 0.45 s later as if shot
            {
                testHookT += rdt; mouseDown = false;
                if (P.weapon != 1) SetWeapon(1);
                if (testHookT > 3) { testHookT = 0; P.coreCd = 0; P.coreCharging = true; P.coreChargeT = 0.4f; P.pitch += 0.15f; LaunchCore(); }
                if (testHookT > 0.45f && testHookT < 0.6f) foreach (var c in cores.ToArray()) DetonateCore(c, true);
            }
            if (Arg("-brsaw") != null) // -brsaw: rip in with the chainsaw every 1.2 s (revolver out)
            {
                if (P.weapon != 0) SetWeapon(0);
                mouseDown = false; testHookT += rdt; if (testHookT > 1.6f) { testHookT = 0; P.punchCd = 0; Punch(); }
                if (testHookT > 1.0f) sawHeld = false;
            }
            if (Arg("-brlaser") != null) // -brlaser: minigun out, hold the meltdown laser whenever it's ready
            {
                if (P.weapon != 2) SetWeapon(2);
                mouseDown = false; laserHeld = P.overheat <= 0;
            }
            if (testHook) // -brhook: every 3 s throw the hook, alternately at a wall (turned away) and at the nearest enemy
            {
                testHookT += rdt;
                if (testHookT > 3) { testHookT = 0; if ((testHookN++ & 1) == 0) { P.yaw += 2.2f; P.pitch = 0.08f; } HK.cd = 0; ThrowHook(); }
                if (HK.state != "idle") { mouseDown = false; testWish = Vector2.zero; }
            }
            CoopTestSteer(rdt);
            if (portal != null && portal.active && !WARP.active) { var to = portal.pos - P.pos; P.yaw = Mathf.Atan2(-to.x, -to.z); testWish = new Vector2(1, 0); }
            if (Arg("-brwatch") != null) // -brwatch: stand back and watch the boss's phase-2 arena / death (camera held on a point)
            {
                Vector3 at = bossDeath != null ? bossDeath.basePos + V(0, 2, 0) : V(18, 2.5f, 18);
                if (bossDeath != null || (boss != null && boss.phase2))
                {
                    GOD = true; testWish = Vector2.zero;
                    if (bossDeath != null) { P.pos = bossDeath.basePos + V(9, 1.5f, 9); P.vel = Vector3.zero; } mouseDown = bossDeath == null && boss != null && testT % 4 < 0.3f;
                    var to = at - EyePos; P.yaw = Mathf.Atan2(-to.x, -to.z); P.pitch = Mathf.Asin(Mathf.Clamp(to.normalized.y, -1, 1));
                }
            }
            if (Arg("-brview3") != null && LEVEL != null && LEVEL.boss == 3) // -brview3: stand still at the side of the throne room and watch the Frozen Traitor fight
            {
                GOD = true; testWish = Vector2.zero; P.pos = V(60, 5, 2); P.vel = Vector3.zero; // up on the east gallery, out of his reach
                Vector3 at = boss != null ? BodyPoint(boss) : bossDeath != null ? (bossDeath.t > 4.4f && tPortalBroken ? tPortalHome - V(0, 3, 0) : bossDeath.basePos + V(0, 4, 0)) : LEVEL.center + V(0, 4, 0);
                if (boss != null && boss.state == "fall" && tThrowSt >= 1 && tThrowSt < 4) at = Vector3.Lerp(BodyPoint(boss), LEVEL.center + V(0, 4, 0), 0.6f);
                if (boss != null && (boss.state == "frozen" || boss.state == "awaken")) P.pos = boss.pos + V(4, 0, 27); // in front of the ice, close enough to wake him
                if (boss != null && boss.state == "fall") P.pos = boss.pos + V(17, 0, 14);
                if (bossDeath != null) P.pos = bossDeath.t < 4.4f ? bossDeath.basePos + V(17, 0, 14) : (tPortalBroken ? V(tPortalHome.x + 12, 0, tPortalHome.z + 14) : P.pos);
                P.pos.x = clamp(P.pos.x, -(LB - 2), LB - 2); P.pos.z = clamp(P.pos.z, -(LB - 2), LB - 2);
                var to = at - EyePos; P.yaw = Mathf.Atan2(-to.x, -to.z); P.pitch = Mathf.Asin(Mathf.Clamp(to.normalized.y, -1, 1));
                mouseDown = boss != null && !boss.invuln && testT % 2.5f < 0.7f;
            }
            if (Arg("-brstatue") != null && sanctumStatueHeads.Count > 0) // -brstatue: stand before a Sanctum statue, circling it a little (model shots)
            {
                GOD = true; testWish = Vector2.zero; mouseDown = false; var sp0 = sanctumStatueHeads[0].Item2; var toC = (-sp0).normalized; toC.y = 0;
                float ang = Mathf.Sin(testT * 0.35f) * 1.3f; var off = Quaternion.Euler(0, ang * Mathf.Rad2Deg, 0) * toC;
                P.pos = sp0 + off * 6.5f; P.pos.y = 0; P.vel = Vector3.zero; var to = sp0 + V(0, 3.3f, 0) - EyePos; P.yaw = Mathf.Atan2(-to.x, -to.z); P.pitch = Mathf.Asin(Mathf.Clamp(to.normalized.y, -1, 1));
            }
            if (Arg("-brbossview") != null && boss != null) // -brbossview [dist]: hold fire and circle the boss slowly, looking at it (model shots)
            {
                GOD = true; testWish = Vector2.zero; mouseDown = false; float dist = float.TryParse(Arg("-brbossview"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var bvd) ? bvd : 16;
                float ang = testT * 0.45f; P.pos = boss.pos + V(Mathf.Sin(ang), 0, Mathf.Cos(ang)) * dist; P.pos.y = boss.pos.y; P.vel = Vector3.zero;
                var to = boss.pos + V(0, Mathf.Max(2, boss.bodyY), 0) - EyePos; P.yaw = Mathf.Atan2(-to.x, -to.z); P.pitch = Mathf.Asin(Mathf.Clamp(to.normalized.y, -1, 1));
            }
            if (testKill && boss != null && !boss.invuln && boss.hp > 1) boss.hp = 1; // the next hit kills: lets the death sequence be checked
            if (Arg("-brenrage") != null && boss != null && !boss.invuln && !boss.phase2 && boss.hp > boss.maxHp * 0.5f) boss.hp = boss.maxHp * 0.49f; // jump to phase 2
            if (Arg("-brexec") != null) // -brexec: every 3 s put the nearest enemy in front of you, stagger it, then execute it
            {
                mouseDown = false; testWish = Vector2.zero; testHookT += rdt;
                if (testHookT > 3 && !EX.active)
                {
                    testHookT = 0; Ent n = null; float nd = 1e9f;
                    foreach (var e in enemies) if (!e.dead && e.spawnT <= 0 && e.type == "husk" && Vector3.Distance(e.pos, P.pos) < nd) { nd = Vector3.Distance(e.pos, P.pos); n = e; }
                    if (n != null) { var p = P.pos + FlatFwd * 2.6f; n.pos = V(p.x, n.pos.y, p.z); n.vel = Vector3.zero; Stagger(n); }
                }
                if (testHookT > 0.9f && testHookT - rdt <= 0.9f) Punch();
                if (!EX.active && testHookT > 0.2f) sawHeld = false;
            }
            if (Arg("-brlol") != null && testT > 5 && testT - rdt <= 5) SkipWave(); // the L-O-L cheat, once
            if (Arg("-brjudge") != null && testT > 7 && !bangWipeUsed && enemies.Count > 2) { ActivateBang(); BangWipe(); }
            testShotT += rdt;
            if (testShotT >= testShotEvery) { testShotT = 0; TestShot(CINE.active ? "cine" : $"L{levelNum}_w{wave}"); }
            if (testT > testDur) Application.Quit();
        }
    }
}
