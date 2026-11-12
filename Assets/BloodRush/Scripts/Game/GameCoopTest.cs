using System.IO;
using UnityEngine;

namespace BR
{
    // Co-op autopilot flags (with -brtest):
    //   -brhost [n]   host on LAN and start once n players are in (default 2)     -brjoin [ip]  join a LAN host (default this PC)
    //   -brmortal     no god mode                                                  -brdown s     go down after s seconds (tests reviving)
    // The autopilot crawls to teammates when downed and walks to downed teammates to revive them.
    public partial class Game
    {
        float coopJoinTry = -99; bool coopDownDone;
        bool CoopTest()
        {
            if (Arg("-brmortal") != null) GOD = false;
            if (Arg("-brcoopui") != null) // -brcoopui: screenshots of the CO-OP menu and the LAN lobby, then quit
            {
                if (testT > 1.5f && currentScreen != "coopMenu" && !MP) { HandleAct("coop"); if (net == null) EnsureNet(); net.status = "TEST STATUS LINE"; }
                if (testT > 2.5f && testShotN == 1) TestShot("coopMenu");
                if (testT > 3.5f && !MP) NetHostLan();
                if (testT > 4.5f && testShotN == 2) TestShot("coopLobby");
                if (testT > 5.5f) Application.Quit();
                return true;
            }
            if (started) return false;
            var h = Arg("-brhost"); var j = Arg("-brjoin");
            if (h == null && j == null) return false;
            if (testT < 1.2f) return true;
            // -brrelay: host / join online through Relay + Lobby instead; the host writes its code to <out>/relay_code.txt and
            // the client (-brjoin <path to that file>) reads it from there
            bool relay = Arg("-brrelay") != null;
            if (relay && h != null)
            {
                if ((net == null || (!net.active && !net.connecting)) && testT - coopJoinTry > 15) { coopJoinTry = testT; NetHostRelay(); }
                if (MP && !string.IsNullOrEmpty(net.code)) File.WriteAllText(Path.Combine(testOut, "relay_code.txt"), net.code);
                File.WriteAllText(Path.Combine(testOut, "relay_status.txt"), net?.status ?? "");
            }
            else if (relay)
            {
                File.WriteAllText(Path.Combine(testOut, "relay_status.txt"), net?.status ?? "");
                if ((net == null || (!net.active && !net.connecting)) && testT - coopJoinTry > 8 && File.Exists(j)) { coopJoinTry = testT; NetJoinRelay(File.ReadAllText(j)); }
                if (testT > testDur) Application.Quit();
                return true;
            }
            if (h != null)
            {
                if (!MP && !relay) NetHostLan();
                int want = int.TryParse(h, out var n) ? n : 2;
                if (players.Count >= want && testT > 2.5f) { practiceWave = testLevel; StartGame(); locked = true; }
            }
            else if ((net == null || (!net.active && !net.connecting)) && testT - coopJoinTry > 4) { coopJoinTry = testT; NetJoinLan(j); }
            if (testT > testDur) Application.Quit();
            return true;
        }
        // -brmateshow (host): no enemies; run through every action in turn so a teammate (-brwatchmate) can be screenshotted
        // doing it: walk, run, jump, slide, shoot, reload, swap guns, chainsaw, grapple, take a hit
        static readonly string[] SHOW = { "walk", "run", "jump", "slide", "shoot", "reload", "swap", "saw", "hook", "exec", "hurt", "hook", "exec", "idle" };
        string mateShowAct = ""; int mateShowStep = -1; Ent mateExecE;
        void MateShow(float rdt)
        {
            foreach (var e in enemies) if (!e.dead && e.type != "boss" && e != mateExecE && e != EX.e) { e.pos = V(LB - 4, e.type == "drone" ? 6 : 0, -(LB - 4)); e.vel = Vector3.zero; e.stun = 99; e.attackT = 0; e.shootT = 99; } // (kept alive, parked in a corner: the wave stays on)
            int step = (int)(testT / 3f) % SHOW.Length; string a = SHOW[step]; bool fresh = step != mateShowStep; mateShowStep = step; mateShowAct = a;
            float u = testT % 3f; testWish = Vector2.zero; mouseDown = false; testCrouch = false; sawHeld = false;
            P.yaw = Mathf.Sin(testT * 0.4f) * 0.8f; P.pitch = 0.05f;
            switch (a)
            {
                case "walk": testWish = new Vector2(0.5f, 0); break;
                case "run": testWish = new Vector2(1, 0); break;
                case "jump": testWish = new Vector2(0.6f, 0); if (P.onGround && u > 0.3f) P.jumpBuf = 0.12f; break;
                case "slide": testWish = new Vector2(1, 0); testCrouch = u > 0.6f && u < 2.2f; break;
                case "shoot": mouseDown = true; break;
                case "reload": if (fresh) StartReload(); break;
                case "swap": if (fresh || (u > 1.5f && u - rdt <= 1.5f)) SetWeapon((P.weapon + 1) % 4); break;
                case "saw": sawHeld = true; if (fresh) Punch(); break;
                case "hook": P.yaw = -Mathf.PI / 2; P.pitch = 0.3f; if (fresh) HK.cd = 0; if (HK.state == "idle" && HK.cd <= 0 && u < 2) ThrowHook(); break; // (at the arena wall, so it bites)
                case "exec":
                    P.yaw = 0; if (fresh) { mateExecE = null; foreach (var e in enemies) if (!e.dead && e.type == "husk" && e.spawnT <= 0) { mateExecE = e; break; } if (mateExecE != null) { var fp = P.pos + FlatFwd * 2.6f; mateExecE.pos = V(fp.x, mateExecE.pos.y, fp.z); mateExecE.vel = Vector3.zero; } }
                    if (mateExecE != null && !mateExecE.dead && !EX.active && u > 0.15f && u < 0.4f) { var fp = P.pos + FlatFwd * 2.6f; mateExecE.pos = V(fp.x, mateExecE.pos.y, fp.z); Stagger(mateExecE); TryExecute(); }
                    break;
                case "hurt": if (fresh) { bool gd = GOD; GOD = false; P.iframes = 0; DamagePlayer(15); P.hp = Mathf.Max(P.hp, 60); GOD = gd; } break;
            }
            if (fresh) File.AppendAllText(Path.Combine(testOut, "coop.txt"), $"{testT:0.0} SHOW {a}\n");
        }
        void CoopTestSteer(float rdt)
        {
            if (!MP) return;
            if (Arg("-brmateshow") != null && Auth && started) { MateShow(rdt); return; }
            // the autopilot walks at the portal in a straight line and can wedge on scenery: after a while, set it down by the portal
            if (portal != null && portal.active && !P.inPortal && !P.downed && !WARP.active)
            {
                coopPortalT = Hypot(P.vel.x, P.vel.z) < 1.5f ? coopPortalT + rdt : 0;
                if (coopPortalT > 3) { coopPortalT = 0; var d = P.pos - portal.pos; d.y = 0; P.pos = portal.pos + d.normalized * 4 - V(0, 1.5f, 0); P.vel = Vector3.zero; }
            }
            if (float.TryParse(Arg("-brdown") ?? "", out var ds) && testT > ds && !coopDownDone && !P.downed && phase == "fight")
            {
                coopDownDone = true; bool g = GOD; GOD = false; P.iframes = 0; DamagePlayer(999); GOD = g;
                File.AppendAllText(Path.Combine(testOut, "coop.txt"), $"{testT:0.0} went down\n");
            }
            Player goal = null; float gd = 45;
            foreach (var p in players)
            {
                if (p == P || p.inPortal) continue;
                bool want = P.downed ? !Down(p) : p.downed;
                float d = Vector3.Distance(p.pos, P.pos); if (want && d < gd) { gd = d; goal = p; }
            }
            if (Arg("-brwatchmate") != null && goal == null) // -brwatchmate: hold fire and look at the nearest teammate (screenshots of their body)
                foreach (var p in players) if (p != P) { var fd = p.pos - P.pos; fd.y = 0; if (fd.magnitude > 14) { var toC = V(-p.pos.x, 0, -p.pos.z); toC = toC.sqrMagnitude > 1 ? toC.normalized : Vector3.forward; P.pos = V(p.pos.x, 0, p.pos.z) + toC * 8 + Vector3.Cross(Vector3.up, toC) * 3; P.vel = Vector3.zero; } var d = EyeOf(p) - V(0, 0.5f, 0) - EyePos; P.yaw = Mathf.Atan2(-d.x, -d.z); P.pitch = Mathf.Asin(Mathf.Clamp(d.normalized.y, -1, 1)); mouseDown = false; testWish = d.magnitude > 7 ? new Vector2(1, 0) : Vector2.zero; return; }
            if (goal == null) return;
            var to = goal.pos - P.pos; P.yaw = Mathf.Atan2(-to.x, -to.z);
            testWish = new Vector2(gd > 1.2f ? 1 : 0, 0); if (!P.downed) mouseDown = false;
            // blocked (the autopilot walks in straight lines): hop and side-step round it
            coopStuckT = Hypot(P.vel.x, P.vel.z) < 1 && gd > 1.2f ? coopStuckT + rdt : 0;
            if (coopStuckT > 0.4f) { testWish.y = Mathf.Sin(testT * 0.8f) > 0 ? 1 : -1; if (!P.downed && P.onGround) P.jumpBuf = 0.12f; }
        }
        float coopStuckT, coopPortalT;
        // logged once a second while a co-op test runs: what this machine sees
        float coopLogT;
        void CoopTestLog(float rdt)
        {
            if (!testMode || !MP || !started) return;
            coopLogT += rdt; if (coopLogT < (Arg("-brwatchmate") != null ? 0.25f : 1)) return; coopLogT = 0;
            var sb = new System.Text.StringBuilder($"{testT:0.0} slot{net.mySlot} L{levelNum} w{wave} {phase} hp{Mathf.CeilToInt(P.hp)}{(P.downed ? " DOWN" : "")}{(P.inPortal ? " PORTAL" : "")} enemies{enemies.Count} hits{netHits} score{S.score}");
            if (boss != null) sb.Append($" boss {boss.state} {boss.hp:0}/{boss.maxHp:0}");
            foreach (var p in players) if (p != P) sb.Append($" | {p.name} {p.pos.x:0.0},{p.pos.y:0.0},{p.pos.z:0.0} hp{p.hp:0}{(p.downed ? " DOWN" : "")}{(p.inPortal ? " PORTAL" : "")} rev{p.revive:0.00}{(p.hooking ? " HOOKING" : "")}{((p.act & 32) != 0 ? " PULL" : "")}{(bodies.TryGetValue(p, out var bb) && bb.claw && bb.claw.gameObject.activeSelf ? $" ROPE{Vector3.Distance(bb.claw.position, p.pos):0}" : "")}{((p.act & 4) != 0 ? $" EXEC{p.execKind}" : "")}{(bodies.TryGetValue(p, out var b2) && b2.saw && b2.saw.gameObject.activeSelf ? " SAW" : "")}");
            File.AppendAllText(Path.Combine(testOut, "coop.txt"), sb.Append('\n').ToString());
        }
    }
}
