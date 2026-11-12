using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace BR
{
    public class NetState
    {
        public bool active, isHost, connecting, relay; public int mySlot; public string code = "", status = "", lanAddr = "";
        public NetworkManager nm; public UnityTransport utp; public ISession session;
        public readonly Dictionary<ulong, int> slotOf = new Dictionary<ulong, int>(); // host: NGO client id -> player slot
        public float sendT, snapT, age;
    }
    public enum NetEv : byte { Start = 1, Wave, Warp, Cutscene, GameOver, Intro, IntroEnd, IntroVotes, IntroVote, BossCine, BossCineAt, BossCineEnd, BossCineVotes, BossCineVote }

    // CO-OP NETWORKING: Unity Netcode for GameObjects used as a message pipe (no NetworkObjects or prefabs - the game builds
    // everything in code). One named message "br"; its first byte is a Msg. Online games run over Unity Relay + Lobby
    // (Multiplayer Services sessions, joined with a short code); LAN / direct IP uses Unity Transport directly.
    //
    //   client -> host : PState 30 Hz (own movement/aim/flags), Hit / Stagger claims (reliable), Hold (enemies it is
    //                    reeling in with the hook or executing), Hello.
    //   host -> client : PState 30 Hz (everyone), Snap 15 Hz (every enemy + phase/wave/clock), Spawn / Kill / Event /
    //                    Welcome / Roster (reliable).
    // Every machine simulates every enemy; the host's snapshot keeps the copies in line and only the host makes random
    // decisions, spawns, applies damage and kills (see GameCoop.cs for the rules enemy code follows).
    public partial class Game
    {
        enum Msg : byte { PState = 1, Snap, Spawn, Kill, Hit, Stagger, Hold, Event, Hello, Welcome, Roster, Fx }
        const string MSG = "br"; const ushort LAN_PORT = 7777;
        static readonly string[] PHASES = { "pre", "fight", "cleared", "portal", "done" };
        NetState net; bool netRemoteHit; int netKiller, netHits; ushort nidNext; // netHits: claims sent (client) / applied (host)
        readonly Dictionary<Player, List<(float t, Vector3 pos, Vector3 vel, float yaw, float pitch)>> pbuf = new Dictionary<Player, List<(float, Vector3, Vector3, float, float)>>();

        // ---------- setup ----------
        void EnsureNet()
        {
            if (net == null) net = new NetState();
            if (net.nm) return;
            var go = new GameObject("Net"); DontDestroyOnLoad(go);
            net.utp = go.AddComponent<UnityTransport>();
            net.nm = go.AddComponent<NetworkManager>();
            net.nm.NetworkConfig = new NetworkConfig { NetworkTransport = net.utp, ConnectionApproval = true, EnableSceneManagement = false, TickRate = 30, ClientConnectionBufferTimeout = 15 };
            net.nm.ConnectionApprovalCallback = Approve;
            net.nm.OnClientConnectedCallback += OnConnected;
            net.nm.OnClientDisconnectCallback += OnDisconnected;
            // the messaging manager is created when the NetworkManager starts (by us, or by the Relay session)
            net.nm.OnServerStarted += RegisterHandler; net.nm.OnClientStarted += RegisterHandler;
            Application.runInBackground = true;
        }
        void RegisterHandler() { net.nm.CustomMessagingManager?.RegisterNamedMessageHandler(MSG, OnMsg); }
        // drop-in only between levels, at most 4
        void Approve(NetworkManager.ConnectionApprovalRequest req, NetworkManager.ConnectionApprovalResponse res)
        {
            res.CreatePlayerObject = false;
            if (req.ClientNetworkId == NetworkManager.ServerClientId) { res.Approved = true; return; }
            if (net.slotOf.Count >= 4) { res.Approved = false; res.Reason = "SESSION FULL (4 PLAYERS)"; return; }
            bool between = !started || WARP.active || phase == "pre" || phase == "portal" || phase == "cleared";
            if (!between || won || CINE.active || (started && P.dead)) { res.Approved = false; res.Reason = "A WAVE IS IN PROGRESS - JOIN BETWEEN LEVELS"; return; }
            res.Approved = true;
        }
        async Task EnsureServices()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                // a profile per process, so two copies of the game on one PC sign in as two different players
                var o = new InitializationOptions(); o.SetProfile("p" + (System.Diagnostics.Process.GetCurrentProcess().Id % 1000000));
                await UnityServices.InitializeAsync(o);
            }
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        void BeginSession(bool host)
        {
            net.active = true; net.isHost = host; net.connecting = !host;
            net.slotOf.Clear(); nidNext = 0;
            foreach (var p in players.ToArray()) if (p.remote) players.Remove(p);
            pbuf.Clear(); ClearBodies();
            if (host) { net.mySlot = 0; net.slotOf[NetworkManager.ServerClientId] = 0; P.slot = 0; P.name = "P1"; }
        }

        // ---------- menu actions ----------
        async void NetHostRelay()
        {
            if (net != null && (net.active || net.connecting)) return;
            EnsureNet(); net.connecting = true; net.relay = true; net.status = "CREATING ONLINE SESSION...";
            try
            {
                await EnsureServices();
                var opts = new SessionOptions { MaxPlayers = 4, IsPrivate = true }.WithRelayNetwork();
                // the session starts the NetworkManager as host itself; register before it can receive anything
                net.session = await MultiplayerService.Instance.CreateSessionAsync(opts);
                net.code = net.session.Code; BeginSession(true); net.connecting = false;
                net.status = "SESSION READY · SHARE THE CODE";
                ShowScreen("coopLobby");
            }
            catch (Exception ex) { NetFail("ONLINE HOST FAILED: " + Short(ex) + "\nUSE HOST LAN, OR LINK THE PROJECT TO UNITY GAMING SERVICES (SEE COOP_NOTES.md)"); }
        }
        async void NetJoinRelay(string code)
        {
            if (net != null && (net.active || net.connecting)) return;
            code = (code ?? "").Trim().ToUpperInvariant(); if (code.Length < 4) { if (net != null) net.status = "TYPE THE HOST'S CODE FIRST"; return; }
            EnsureNet(); net.connecting = true; net.relay = true; net.status = "JOINING " + code + "...";
            try
            {
                await EnsureServices();
                BeginSession(false); // before the connection completes: the welcome may arrive straight away
                net.session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
                net.code = code;
            }
            catch (Exception ex) { NetFail("JOIN FAILED: " + Short(ex)); }
        }
        void NetHostLan()
        {
            if (net != null && (net.active || net.connecting)) return;
            EnsureNet(); net.relay = false;
            net.utp.SetConnectionData("0.0.0.0", LAN_PORT, "0.0.0.0");
            if (!net.nm.StartHost()) { NetFail("COULD NOT HOST ON PORT " + LAN_PORT); return; }
            BeginSession(true); net.code = ""; net.lanAddr = LocalIp() + ":" + LAN_PORT;
            net.status = "HOSTING ON LAN · FRIENDS JOIN " + net.lanAddr;
            ShowScreen("coopLobby");
        }
        void NetJoinLan(string addr)
        {
            if (net != null && (net.active || net.connecting)) return;
            EnsureNet(); net.relay = false;
            addr = string.IsNullOrWhiteSpace(addr) ? "127.0.0.1" : addr.Trim(); ushort port = LAN_PORT;
            int c = addr.LastIndexOf(':'); if (c > 0 && ushort.TryParse(addr.Substring(c + 1), out var pp)) { port = pp; addr = addr.Substring(0, c); }
            net.utp.SetConnectionData(addr, port);
            if (!net.nm.StartClient()) { NetFail("COULD NOT CONNECT TO " + addr); return; }
            BeginSession(false);
            net.status = "CONNECTING TO " + addr + ":" + port + "...";
        }
        void NetFail(string why)
        {
            Debug.LogWarning("BR_NET " + why);
            NetShutdown(); if (net != null) net.status = why;
            if (started) QuitToMenu();
            ShowScreen("coopMenu");
        }
        static string Short(Exception ex) { var m = (ex.InnerException ?? ex).Message ?? ex.GetType().Name; m = m.Replace('\n', ' '); return (m.Length > 90 ? m.Substring(0, 90) : m).ToUpperInvariant(); }
        static string LocalIp()
        {
            try { foreach (var a in System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList) if (a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(a)) return a.ToString(); } catch { }
            return "127.0.0.1";
        }
        void NetShutdown()
        {
            if (net == null) return;
            var s = net.session; net.session = null;
            if (s != null) { try { _ = s.LeaveAsync(); } catch { } }
            if (net.nm && !net.nm.ShutdownInProgress && (net.nm.IsListening || net.nm.IsClient || net.nm.IsServer)) net.nm.Shutdown();
            net.active = false; net.isHost = false; net.connecting = false; net.slotOf.Clear();
            foreach (var p in players.ToArray()) if (p.remote) players.Remove(p);
            pbuf.Clear(); ClearBodies();
        }
        void NetLeave() { if (MP || (net != null && net.connecting)) { NetShutdown(); net.status = "LEFT THE SESSION"; } }

        // ---------- connection events ----------
        void OnConnected(ulong id)
        {
            if (net.isHost) return; // the host waits for Hello
            if (id == net.nm.LocalClientId) { net.connecting = false; net.status = "CONNECTED · WAITING FOR THE HOST"; Send(Msg.Hello, w => w.WriteValueSafe(P.name), true); }
        }
        void OnDisconnected(ulong id)
        {
            if (net == null || !net.active) return;
            if (net.isHost)
            {
                if (!net.slotOf.TryGetValue(id, out var slot)) return;
                net.slotOf.Remove(id);
                var p = PlayerBySlot(slot); if (p != null) { players.Remove(p); pbuf.Remove(p); DropBody(p); Banner($"{p.name} LEFT", "", "#ff8a40"); }
                SendRoster();
            }
            else if (id == net.nm.LocalClientId || id == NetworkManager.ServerClientId)
            {
                var why = string.IsNullOrEmpty(net.nm.DisconnectReason) ? "DISCONNECTED FROM THE HOST" : net.nm.DisconnectReason;
                NetFail(why);
            }
        }
        Player PlayerBySlot(int slot) { foreach (var p in players) if (p.slot == slot) return p; return null; }
        Player RemoteSlot(int slot)
        {
            var p = PlayerBySlot(slot); if (p != null) return p.remote ? p : null;
            p = new Player { remote = true, slot = slot, name = "P" + (slot + 1) }; players.Add(p); return p;
        }

        // ---------- sending ----------
        delegate void Writer(FastBufferWriter w);
        void Send(Msg m, Writer body, bool reliable, ulong? to = null)
        {
            if (net == null || !net.active || !net.nm || !net.nm.IsListening && !net.nm.IsConnectedClient) return;
            var w = new FastBufferWriter(1200, Allocator.Temp, 60000);
            try
            {
                w.WriteValueSafe((byte)m); body(w);
                var d = reliable ? (w.Length > 1100 ? NetworkDelivery.ReliableFragmentedSequenced : NetworkDelivery.ReliableSequenced) : NetworkDelivery.UnreliableSequenced;
                var cm = net.nm.CustomMessagingManager;
                if (!net.isHost) cm.SendNamedMessage(MSG, NetworkManager.ServerClientId, w, d);
                else if (to.HasValue) cm.SendNamedMessage(MSG, to.Value, w, d);
                else { var ids = new List<ulong>(); foreach (var id in net.slotOf.Keys) if (id != NetworkManager.ServerClientId) ids.Add(id); if (ids.Count > 0) cm.SendNamedMessage(MSG, ids, w, d); }
            }
            catch (Exception ex) { Debug.LogWarning("BR_NET send " + m + ": " + ex.Message); }
            finally { w.Dispose(); }
        }
        static void WH(FastBufferWriter w, float v) => w.WriteValueSafe(Mathf.FloatToHalf(v));
        static float RH(FastBufferReader r) { r.ReadValueSafe(out ushort h); return Mathf.HalfToFloat(h); }
        static Vector3 RV(FastBufferReader r) { r.ReadValueSafe(out Vector3 v); return v; }
        static float RF(FastBufferReader r) { r.ReadValueSafe(out float v); return v; }
        static byte RB(FastBufferReader r) { r.ReadValueSafe(out byte v); return v; }
        static ushort RU(FastBufferReader r) { r.ReadValueSafe(out ushort v); return v; }

        // host -> clients events (no-ops solo and on clients)
        void NetEvent(NetEv ev, int arg = 0) { if (MP && Auth) Send(Msg.Event, w => { w.WriteValueSafe((byte)ev); w.WriteValueSafe(arg); }, true); }
        // world changes that must look the same on every screen (a building or clock tower coming down...). Whoever causes it
        // sends it: a client to the host, which passes it on to everyone else; the sender has already applied it
        public enum WFx : byte { Break = 1, Tower }
        void NetFx(WFx k, int a, Vector3 v) { if (!MP) return; Send(Msg.Fx, w => { w.WriteValueSafe((byte)k); w.WriteValueSafe(a); w.WriteValueSafe(v); }, true); }
        void RecvFx(ulong sender, FastBufferReader r)
        {
            var k = (WFx)RB(r); int a = ReadInt(r); var v = RV(r);
            if (net.isHost) foreach (var id in new List<ulong>(net.slotOf.Keys)) if (id != sender && id != NetworkManager.ServerClientId) Send(Msg.Fx, w => { w.WriteValueSafe((byte)k); w.WriteValueSafe(a); w.WriteValueSafe(v); }, true, id);
            if (!started) return;
            if (k == WFx.Break && a >= 0 && a < hkBuilds.Count && !hkBuilds[a].broken) { BreakBuilding(hkBuilds[a]); for (int q = 0; q < 4; q++) HollowDrop(v + V((rand - 0.5f) * 10, 0, (rand - 0.5f) * 10), 0.6f + q * 0.2f); } // (and the wall he hit comes down round him)
            if (k == WFx.Tower && a >= 0 && a < wTowers.Count && !wTowers[a].broken) WardenTowerBreak(wTowers[a], v);
        }

        // ---------- per frame ----------
        void NetUpdate(float rdt)
        {
            if (net == null) return;
            if (!net.active) { UpdateLobbyText(); return; } // still shows connect errors on the CO-OP menu
            if (net.nm && !net.isHost && net.utp && net.nm.IsConnectedClient) // half the round trip: how old the host's news is when it lands
                try { net.age = Mathf.Clamp(net.utp.GetCurrentRtt(NetworkManager.ServerClientId) * 0.0005f, 0, 0.3f); } catch { }
            InterpRemotes();
            UpdateBodies(rdt);
            UpdateLobbyText();
            if (!started) return;
            CoopTestLog(rdt);
            P.hooking = HK.state != "idle"; P.hookPt = HK.pos;
            P.act = (byte)((reloadT > 0 ? 1 : 0) | (fistT > 0 ? 2 : 0) | (EX.active ? 4 : 0) | (swapT > 0 ? 8 : 0) | (P.dashT > 0 ? 16 : 0) | (HK.state == "pull" || HK.state == "yank" ? 32 : 0));
            P.execNid = EX.active && EX.e != null ? EX.e.nid : (ushort)0; P.execKind = (byte)(EX.active ? EX.kind : 0);
            net.sendT += rdt;
            if (net.sendT >= 1 / 30f)
            {
                net.sendT = 0;
                if (net.isHost) Send(Msg.PState, w => { w.WriteValueSafe((byte)players.Count); foreach (var p in players) WritePState(w, p); }, false);
                else
                {
                    Send(Msg.PState, w => { w.WriteValueSafe((byte)1); WritePState(w, P); }, false);
                    // enemies this player is physically moving (reeling in with the hook, or executing): it sends where they are
                    Ent held = HK.state == "yank" ? HK.enemy : EX.active ? EX.e : null;
                    if (held != null && !held.dead && held.nid != 0) Send(Msg.Hold, w => { w.WriteValueSafe(held.nid); w.WriteValueSafe(held.pos); w.WriteValueSafe(held.vel); }, false);
                }
            }
            if (net.isHost) { net.snapT += rdt; if (net.snapT >= 1 / 15f) { net.snapT = 0; SendSnapshot(); } }
        }
        void WritePState(FastBufferWriter w, Player p)
        {
            w.WriteValueSafe((byte)p.slot); w.WriteValueSafe(p.pos); w.WriteValueSafe(p.vel); w.WriteValueSafe(p.yaw); w.WriteValueSafe(p.pitch);
            byte f = (byte)((p.onGround ? 1 : 0) | (p.sliding ? 2 : 0) | (p.downed ? 4 : 0) | (p.dead ? 8 : 0) | (p.inPortal ? 16 : 0) | (p.hooking ? 32 : 0) | (p.slamming ? 64 : 0));
            w.WriteValueSafe(f); w.WriteValueSafe((byte)Mathf.Clamp(p.weapon, 0, 255)); w.WriteValueSafe((byte)Mathf.Clamp(Mathf.CeilToInt(p.hp), 0, 255));
            w.WriteValueSafe((byte)Mathf.Clamp(Mathf.RoundToInt(p.revive * 255), 0, 255)); w.WriteValueSafe((byte)(p.shots & 255));
            w.WriteValueSafe(p.act); w.WriteValueSafe(p.hookPt); w.WriteValueSafe(p.execNid); w.WriteValueSafe(p.execKind);
        }
        void ReadPState(FastBufferReader r, int fromSlot)
        {
            int slot = RB(r); var pos = RV(r); var vel = RV(r); float yaw = RF(r), pitch = RF(r);
            byte f = RB(r); int wpn = RB(r), hp = RB(r), rev = RB(r), shots = RB(r); byte act = RB(r); var hookPt = RV(r); ushort execNid = RU(r); byte execKind = RB(r);
            if (slot == net.mySlot || (fromSlot >= 0 && slot != fromSlot)) return; // our own echo, or a client speaking for someone else
            var p = RemoteSlot(slot); if (p == null) return;
            p.onGround = (f & 1) != 0; p.sliding = (f & 2) != 0; p.downed = (f & 4) != 0; p.dead = (f & 8) != 0; p.inPortal = (f & 16) != 0; p.hooking = (f & 32) != 0; p.slamming = (f & 64) != 0;
            p.weapon = wpn; p.hp = hp; p.revive = rev / 255f; p.shots = shots; p.act = act; p.hookPt = hookPt; p.execNid = execNid; p.execKind = execKind;
            p.height = p.downed ? 1.0f : p.sliding ? 1.0f : 1.8f; p.eye = p.downed ? 0.45f : p.sliding ? 0.75f : 1.6f;
            if (!pbuf.TryGetValue(p, out var l)) pbuf[p] = l = new List<(float, Vector3, Vector3, float, float)>();
            l.Add((Time.unscaledTime, pos, vel, yaw, pitch)); if (l.Count > 20) l.RemoveAt(0);
        }
        // teammates are drawn (and targeted) 100 ms in the past, interpolated between their last two states
        void InterpRemotes()
        {
            float rt = Time.unscaledTime - 0.1f;
            foreach (var kv in pbuf)
            {
                var p = kv.Key; var l = kv.Value; if (l.Count == 0) continue;
                int i = l.Count - 1; while (i > 0 && l[i - 1].t > rt) i--;
                if (i == 0 || l[i].t <= rt)
                {
                    var s = l[l.Count - 1]; float ex = Mathf.Clamp(rt - s.t, 0, 0.2f);
                    p.pos = s.pos + s.vel * ex; p.vel = s.vel; p.yaw = s.yaw; p.pitch = s.pitch;
                }
                else
                {
                    var a = l[i - 1]; var b = l[i]; float k = Mathf.InverseLerp(a.t, b.t, rt);
                    p.pos = Vector3.Lerp(a.pos, b.pos, k); p.vel = Vector3.Lerp(a.vel, b.vel, k);
                    p.yaw = Mathf.LerpAngle(a.yaw * Mathf.Rad2Deg, b.yaw * Mathf.Rad2Deg, k) * Mathf.Deg2Rad; p.pitch = Mathf.Lerp(a.pitch, b.pitch, k);
                }
            }
        }

        // ---------- enemies: spawn / snapshot / kill ----------
        // 0 husk, 1 brute, 2 drone, 10 + n boss n; the act kind (frost, scav, twin, bell...) travels with it as a string
        static byte KindOf(Ent e) => (byte)(e.type == "husk" ? (e.brute ? 1 : 0) : e.type == "drone" ? 2 : 10 + e.boss);
        void NetOnSpawn(Ent e)
        {
            if (!MP || !Auth) return;
            e.nid = ++nidNext; if (e.nid == 0) e.nid = ++nidNext;
            Send(Msg.Spawn, w => { w.WriteValueSafe(e.nid); w.WriteValueSafe(KindOf(e)); w.WriteValueSafe(e.pos); w.WriteValueSafe(e.hp); w.WriteValueSafe(e.maxHp); w.WriteValueSafe(e.kind ?? ""); w.WriteValueSafe(e.emergeDir); }, true);
        }
        // host: a boss picked an attack - its name goes out in the snapshots, with a counter so each pick is seen once
        void NetAct(Ent e, string a) { if (!MP || !Auth) return; e.netAct = a ?? ""; e.netActSeq++; }
        // client: the host's boss picked an attack - run its setup here too (warnings, aim, sounds)
        void NetBossBegin(Ent e, string a)
        {
            if (e.boss == 4) HollowBegin(e, a); else if (e.boss == 3) TraitorBegin(e, a); else if (e.boss == 5) WardenBegin(e, a);
        }
        // client: the host's boss went into its second phase (or its fall) - make the same change here
        void NetPhase2(Ent e)
        {
            if (e.phase2) return;
            if (e.boss == 1 || e.boss == 2) Enrage(e); // (the Seraph's fall, with its cinematic, starts from here)
            else if (e.boss == 3) { if (e.state != "fall") TraitorFall(e); }
            else e.phase2 = true; // (the Hollow King and the Warden make the change themselves from the health bar)
        }
        Ent FindNid(ushort nid) { if (nid == 0) return null; foreach (var e in enemies) if (e.nid == nid) return e; return null; }
        void RecvSpawn(FastBufferReader r)
        {
            ushort nid = RU(r); byte kind = RB(r); var pos = RV(r); float hp = RF(r), maxHp = RF(r); r.ReadValueSafe(out string ak); var emerge = RV(r);
            if (!started || FindNid(nid) != null) return;
            Ent e;
            if (kind <= 1)
            {
                e = SpawnHusk(pos.x, pos.z, kind == 1, ak.Length > 0 ? ak : null);
                if (emerge.sqrMagnitude > 0.01f) { e.pos = pos; e.yaw = Mathf.Atan2(emerge.x, emerge.z); e.emerging = true; e.emergeDir = emerge; e.spawnT = 1.3f; Sfx("blink", 0.7f); } // a Mirror Twin stepping out of its mirror
                else ShockRing(e.pos, 1.6f * e.s, THEME.holy ? 0x80e8ff : 0xff2000, 0.9f);
            }
            else if (kind == 2) { e = SpawnDrone(pos.x, pos.y, pos.z, ak.Length > 0 ? ak : null); if (e.anchored) e.anchor = pos; }
            else
            {
                int bn = kind - 10;
                e = bn == 1 ? SpawnCarnifex() : bn == 3 ? SpawnTraitor() : bn == 4 ? SpawnHollowKing() : bn == 5 ? SpawnWarden() : SpawnSeraph(); phase = "fight";
                if (bn == 1 || bn == 2) { StartBossIntro(boss); if (!BossIntroActive) Banner("WARNING", boss?.name ?? "", "#ff2020"); Sfx("alarm"); } // (the others have entrances of their own)
            }
            e.nid = nid; e.hp = hp; e.maxHp = maxHp; if (e.type != "boss" || (kind - 10) <= 2) e.pos = pos;
        }
        void SendSnapshot()
        {
            var live = new List<Ent>(); foreach (var e in enemies) if (!e.dead && e.nid != 0) live.Add(e);
            int i = 0;
            do
            {
                int n = Mathf.Min(live.Count - i, 22), start = i; i += n;
                Send(Msg.Snap, w =>
                {
                    w.WriteValueSafe((byte)levelNum); w.WriteValueSafe((byte)Mathf.Max(0, Array.IndexOf(PHASES, phase))); w.WriteValueSafe((byte)wave); w.WriteValueSafe(time); w.WriteValueSafe(trapClock);
                    w.WriteValueSafe((byte)n);
                    for (int k = start; k < start + n; k++) WriteEnt(w, live[k]);
                }, false);
            } while (i < live.Count);
        }
        void WriteEnt(FastBufferWriter w, Ent e)
        {
            bool b = e.type == "boss";
            w.WriteValueSafe(e.nid); w.WriteValueSafe(e.pos); WH(w, e.vel.x); WH(w, e.vel.y); WH(w, e.vel.z);
            WH(w, Mathf.Repeat(e.yaw + Mathf.PI, Mathf.PI * 2) - Mathf.PI); WH(w, e.pitch); w.WriteValueSafe(e.hp);
            byte f = (byte)((e.leaping ? 1 : 0) | (e.charging ? 2 : 0) | (e.ground ? 4 : 0) | (e.invuln ? 8 : 0) | (b ? 16 : 0) | (e.phase2 ? 32 : 0));
            w.WriteValueSafe(f);
            WH(w, e.attackT); WH(w, e.strikeT); WH(w, Mathf.Min(e.stun, 60)); WH(w, e.shootT); WH(w, e.spawnT); WH(w, e.stagT); WH(w, e.cd);
            if (!b) return;
            w.WriteValueSafe(e.state ?? ""); WH(w, e.stateT); w.WriteValueSafe((byte)e.vidx); w.WriteValueSafe((byte)Mathf.Clamp(e.dashes, 0, 255));
            w.WriteValueSafe(e.chargeDir); w.WriteValueSafe(e.dashDir); w.WriteValueSafe(e.beamDir); w.WriteValueSafe(e.groundY); WH(w, e.orbit % 1000); WH(w, e.leapVX); WH(w, e.leapVZ);
            w.WriteValueSafe(e.netAct ?? ""); w.WriteValueSafe(e.netActSeq);
        }
        void RecvSnap(FastBufferReader r)
        {
            int lvl = RB(r), ph = RB(r), wv = RB(r); float hostTime = RF(r), hostTrap = RF(r); int n = RB(r);
            float age = net.age;
            if (started && !WARP.active && !CINE.active && !P.dead)
            {
                if (lvl != levelNum) { StartWarp(lvl); return; } // missed a warp: catch up
                var hp = PHASES[Mathf.Clamp(ph, 0, PHASES.Length - 1)];
                if (hp != phase) { phase = hp; if (phase == "portal" && portal == null) OpenPortal(); }
                wave = wv;
                if (Mathf.Abs(hostTime + age - time) > 0.3f) time = hostTime + age; // keeps the level's hazard cycles in step
                if (Mathf.Abs(hostTrap + age - trapClock) > 0.25f) trapClock = hostTrap + age; // ...and the traps (hammers, mirrors, fog tide...) firing together
            }
            for (int k = 0; k < n; k++) ReadEnt(r, age);
        }
        void ReadEnt(FastBufferReader r, float age)
        {
            ushort nid = RU(r); var pos = RV(r); var vel = V(RH(r), RH(r), RH(r)); float yaw = RH(r), pitch = RH(r), hp = RF(r);
            byte f = RB(r); float attackT = RH(r), strikeT = RH(r), stun = RH(r), shootT = RH(r), spawnT = RH(r), stagT = RH(r), cd = RH(r);
            bool b = (f & 16) != 0;
            string st = null, act = null; float stateT = 0, groundY = 0, orbit = 0, lvx = 0, lvz = 0; int vidx = 0, dashes = 0; byte actSeq = 0; Vector3 cdir = default, ddir = default, bdir = default;
            if (b) { r.ReadValueSafe(out st); stateT = RH(r); vidx = RB(r); dashes = RB(r); cdir = RV(r); ddir = RV(r); bdir = RV(r); groundY = RF(r); orbit = RH(r); lvx = RH(r); lvz = RH(r); r.ReadValueSafe(out act); actSeq = RB(r); }
            var e = FindNid(nid); if (e == null || e.dead || !started) return;
            bool mine = (HK.state == "yank" && HK.enemy == e) || (EX.active && EX.e == e); // we're moving it ourselves
            if (!mine)
            {
                var target = pos + vel * age; var err = target - e.pos;
                if (err.sqrMagnitude > 16) { e.pos = target; e.netErr = Vector3.zero; if (e.g) e.g.transform.localPosition = e.pos; } else e.netErr = err;
                e.vel = vel;
            }
            e.yaw = Mathf.LerpAngle(e.yaw * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg, 0.6f) * Mathf.Deg2Rad; if (b) e.pitch = pitch;
            e.hp = hp; e.invuln = (f & 8) != 0; e.cd = cd;
            if (Mathf.Abs(spawnT - e.spawnT) > 0.2f) e.spawnT = spawnT;
            if (stagT > e.stagT + 0.25f) e.stagT = stagT;
            if (!mine && Mathf.Abs(stun - e.stun) > 0.25f) e.stun = stun;
            if (e.type == "husk")
            {
                if (attackT > 0 && e.attackT <= 0 && e.strikeT <= 0 && !mine) { e.attackT = Mathf.Max(0.02f, attackT - age); if (e.holy) Sfx("chant"); Sfx(e.brute ? "growlLow" : "growl"); }
                if ((f & 1) != 0 && !e.leaping && !mine) { e.leaping = true; e.ground = false; e.leapVX = vel.x; e.leapVZ = vel.z; Sfx("growl"); }
            }
            else if (e.type == "drone")
            {
                if (Mathf.Abs(shootT - age - e.shootT) > 0.3f) { e.shootT = shootT - age; e.charging = (f & 2) != 0; }
            }
            else if (b)
            {
                if ((f & 32) != 0 && !e.phase2) NetPhase2(e);
                if (actSeq != e.netActSeen) { e.netActSeen = actSeq; if (!string.IsNullOrEmpty(act) && (e.state == "chase" || e.state == "hover" || e.state == "frozen")) { e.netAct = act; NetBossBegin(e, act); } }
                e.chargeDir = cdir; e.dashDir = ddir; e.groundY = groundY; e.orbit = orbit; e.dashes = dashes; e.leapVX = lvx; e.leapVZ = lvz;
                if (e.state != "laser") e.beamDir = bdir;
                if (st != e.state)
                {
                    // the client's copy runs the same state machine and usually gets there itself; when it is idling waiting
                    // for the host's pick, or has stayed off for a moment, it is put into the host's state
                    bool idle = e.state == "chase" || e.state == "hover"; e.netMismatchT += 1 / 15f;
                    if (idle || e.netMismatchT > 0.3f)
                    {
                        if (e.state == "laser") { beamCore.SetActive(false); beamOuter.SetActive(false); beamHit.SetActive(false); }
                        e.state = st; e.stateT = stateT - age; e.vidx = vidx; e.hitPlayer = false; e.netMismatchT = 0;
                        if (st == "slamWind") Sfx("growlLow"); else if (st == "chargeWind") Sfx("roarShort"); else if (st == "laserWind") Sfx("laserCharge"); else if (st == "blinkOut") Sfx("blink");
                        if (e.sr != null && st != "blinkOut" && e.g) e.g.transform.localScale = V(2.2f, 2.2f, 2.2f);
                    }
                }
                else { e.netMismatchT = 0; if (Mathf.Abs(stateT - age - e.stateT) > 0.3f) e.stateT = stateT - age; }
            }
        }
        // host: tell everyone an enemy died (called just before KillEnemy / KillBoss)
        void NetKill(Ent e, Hit info)
        {
            if (!MP || !Auth || e.nid == 0) return;
            int killer = netRemoteHit ? netKiller : net.mySlot;
            Send(Msg.Kill, w => { w.WriteValueSafe(e.nid); w.WriteValueSafe((byte)killer); WriteHit(w, 0, info); }, true);
        }
        void RecvKill(FastBufferReader r)
        {
            ushort nid = RU(r); int killer = RB(r); var info = ReadHit(r, out _);
            var e = FindNid(nid); if (e == null || e.dead) return;
            bool me = killer == net.mySlot;
            if (info.exec && !me) { info.exec = false; info.punch = true; } // someone else's execution: we don't have their cut pieces
            e.hp = 0; netRemoteHit = !me;
            try { if (e.type == "boss") KillBoss(e, info); else KillEnemy(e, info); }
            finally { netRemoteHit = false; }
        }
        void WriteHit(FastBufferWriter w, float dmg, Hit h)
        {
            w.WriteValueSafe(dmg);
            ushort f = (ushort)((h.head ? 1 : 0) | (h.coin ? 2 : 0) | (h.slam ? 4 : 0) | (h.punch ? 8 : 0) | (h.parry ? 16 : 0) | (h.close ? 32 : 0) | (h.explosion ? 64 : 0) |
                (h.sniper ? 128 : 0) | (h.deadeye ? 256 : 0) | (h.cheat ? 512 : 0) | (h.exec ? 1024 : 0) | (h.point.HasValue ? 2048 : 0) | (h.dir.HasValue ? 4096 : 0));
            w.WriteValueSafe(f); w.WriteValueSafe(h.kb); w.WriteValueSafe((sbyte)Mathf.Clamp(h.wpn >= 0 ? h.wpn : P.weapon, -1, 100));
            if (h.point.HasValue) w.WriteValueSafe(h.point.Value);
            if (h.dir.HasValue) w.WriteValueSafe(h.dir.Value);
        }
        Hit ReadHit(FastBufferReader r, out float dmg)
        {
            dmg = RF(r); ushort f = RU(r); var h = new Hit { kb = RF(r) }; r.ReadValueSafe(out sbyte wp); h.wpn = wp;
            h.head = (f & 1) != 0; h.coin = (f & 2) != 0; h.slam = (f & 4) != 0; h.punch = (f & 8) != 0; h.parry = (f & 16) != 0; h.close = (f & 32) != 0; h.explosion = (f & 64) != 0;
            h.sniper = (f & 128) != 0; h.deadeye = (f & 256) != 0; h.cheat = (f & 512) != 0; h.exec = (f & 1024) != 0;
            if ((f & 2048) != 0) h.point = RV(r);
            if ((f & 4096) != 0) h.dir = RV(r);
            return h;
        }
        bool NetMaySoak(Ent e)
        {
            if (e.kind == "glacier" && e.shieldHp > 0) return true;
            if (e.kind == "bell" || e.kind == "clock") return true;
            if (e.kind != "pilgrim") foreach (var o in enemies) if (!o.dead && o.kind == "pilgrim" && o != e && Hypot(o.pos.x - e.pos.x, o.pos.z - e.pos.z) < PILGRIM_R) return true;
            return false;
        }
        // client: a shot of ours hit an enemy - show it now, and let the host apply it. Kills that are certain (not a boss,
        // the damage covers what's left) are played straight away; the host's own kill message then finds nothing to do.
        bool NetClaimHit(Ent e, float dmg, Hit info)
        {
            if (!MP || Auth || netRemoteHit) return false;
            if (e.nid == 0) return true;
            netHits++;
            Send(Msg.Hit, w => { w.WriteValueSafe(e.nid); WriteHit(w, dmg, info); }, true);
            if (e.invuln && !info.cheat) { SpawnParticles(info.point ?? BodyPoint(e), 3, 4, sparkMat, 0.06f, 0.2f); return true; }
            e.flash = 0.07f; e.flinch = Mathf.Min(1, e.flinch + (e.type == "boss" ? 0.15f : 0.5f));
            SpawnParticles(info.point ?? BodyPoint(e), Mathf.Min(14, 3 + Mathf.RoundToInt(dmg * 5)), 7, bloodMat, 0.12f, 0.7f);
            CombatHeal(HitHeal(e, dmg), e.type == "boss");
            Sfx(info.head ? "hitHead" : "hit");
            // a sure kill plays at once - unless something may soak the hit on the host (a pilgrim's lantern, an ice shield, a
            // clock that rewinds): then we wait for the host's word
            if (e.type != "boss" && dmg >= e.hp && !NetMaySoak(e)) { e.hp = 0; KillEnemy(e, info); }
            return true;
        }
        void NetClaimStagger(Ent e) { if (MP && !Auth && !netRemoteHit && e.nid != 0) Send(Msg.Stagger, w => w.WriteValueSafe(e.nid), true); }

        // ---------- receiving ----------
        void OnMsg(ulong sender, FastBufferReader r)
        {
            if (net == null || !net.active) return;
            try
            {
                var m = (Msg)RB(r);
                int from = -1; if (net.isHost) { if (!net.slotOf.TryGetValue(sender, out from)) { if (m != Msg.Hello) return; } }
                switch (m)
                {
                    case Msg.PState: { int c = RB(r); for (int i = 0; i < c; i++) ReadPState(r, net.isHost ? from : -1); break; }
                    case Msg.Snap: if (!net.isHost) RecvSnap(r); break;
                    case Msg.Spawn: if (!net.isHost) RecvSpawn(r); break;
                    case Msg.Kill: if (!net.isHost) RecvKill(r); break;
                    case Msg.Event: if (!net.isHost) RecvEvent(RB(r), ReadInt(r)); else RecvClientEvent(from, RB(r), ReadInt(r)); break;
                    case Msg.Welcome: if (!net.isHost) RecvWelcome(r); break;
                    case Msg.Roster: if (!net.isHost) RecvRoster(r); break;
                    case Msg.Fx: RecvFx(sender, r); break;
                    case Msg.Hello: if (net.isHost) RecvHello(sender, r); break;
                    case Msg.Hit:
                        if (net.isHost)
                        {
                            ushort nid = RU(r); var info = ReadHit(r, out float dmg); var e = FindNid(nid);
                            if (info.exec) { info.exec = false; info.punch = true; } // their cut-up corpse exists only on their screen: everyone else sees it torn apart
                            if (e != null && !e.dead && started) { netHits++; netRemoteHit = true; netKiller = from; try { DamageEnemy(e, dmg, info); } finally { netRemoteHit = false; } }
                        }
                        break;
                    case Msg.Stagger:
                        if (net.isHost) { var e = FindNid(RU(r)); if (e != null && started) { netRemoteHit = true; try { Stagger(e); } finally { netRemoteHit = false; } } }
                        break;
                    case Msg.Hold:
                        if (net.isHost)
                        {
                            var e = FindNid(RU(r)); var pos = RV(r); var vel = RV(r);
                            if (e != null && !e.dead && started)
                            {
                                if (e.type == "boss") { e.stateT = Mathf.Max(e.stateT, 0.6f); e.vel = Vector3.zero; }
                                else { e.pos = pos; e.vel = vel; e.stun = Mathf.Max(e.stun, 0.4f); e.attackT = 0; e.leaping = false; if (e.g) e.g.transform.localPosition = pos; }
                            }
                        }
                        break;
                }
            }
            catch (Exception ex) { Debug.LogWarning("BR_NET recv: " + ex); }
        }
        static int ReadInt(FastBufferReader r) { r.ReadValueSafe(out int v); return v; }

        void RecvHello(ulong sender, FastBufferReader r)
        {
            r.ReadValueSafe(out string name);
            int slot = 1; while (slot < 4 && net.slotOf.ContainsValue(slot)) slot++;
            if (slot >= 4) { net.nm.DisconnectClient(sender, "SESSION FULL"); return; }
            net.slotOf[sender] = slot; RemoteSlot(slot);
            Send(Msg.Welcome, w =>
            {
                w.WriteValueSafe((byte)slot); w.WriteValueSafe(started); w.WriteValueSafe((byte)levelNum); w.WriteValueSafe((byte)practiceWave);
                w.WriteValueSafe((byte)Mathf.Max(0, Array.IndexOf(PHASES, phase))); w.WriteValueSafe((byte)wave); w.WriteValueSafe(time);
            }, true, sender);
            SendRoster();
            Banner($"P{slot + 1} JOINED", started ? "THEY ARRIVE WITH THE NEXT LEVEL" : "", "#5aff8a"); Sfx("arrive");
        }
        void SendRoster() => Send(Msg.Roster, w => { w.WriteValueSafe((byte)net.slotOf.Count); foreach (var s in net.slotOf.Values) w.WriteValueSafe((byte)s); }, true);
        void RecvRoster(FastBufferReader r)
        {
            int n = RB(r); var slots = new HashSet<int>(); for (int i = 0; i < n; i++) slots.Add(RB(r));
            foreach (var s in slots) if (s != net.mySlot) RemoteSlot(s);
            foreach (var p in players.ToArray()) if (p.remote && !slots.Contains(p.slot)) { players.Remove(p); pbuf.Remove(p); DropBody(p); }
        }
        void RecvWelcome(FastBufferReader r)
        {
            int slot = RB(r); r.ReadValueSafe(out bool hostStarted); int lvl = RB(r), pw = RB(r), ph = RB(r), wv = RB(r); float t = RF(r);
            net.mySlot = slot; net.connecting = false; P.slot = slot; P.name = "P" + (slot + 1);
            if (!hostStarted) { net.status = "IN THE LOBBY AS " + P.name + " · WAITING FOR THE HOST TO START"; ShowScreen("coopLobby"); return; }
            // dropping in between levels: build the host's level and start at its spawn
            practiceWave = lvl; StartGame(); practiceWave = pw;
            phase = PHASES[Mathf.Clamp(ph, 0, PHASES.Length - 1)]; wave = wv; time = t;
            if (phase == "portal") OpenPortal();
        }
        void RecvEvent(int ev, int arg)
        {
            switch ((NetEv)ev)
            {
                case NetEv.Start: practiceWave = arg; StartGame(); break;
                case NetEv.Wave:
                    if (!started) break;
                    wave = arg; phase = "fight";
                    if (GetWaveSpec(arg).boss == 0) { Banner($"WAVE {wave}", wave == 4 || wave == 9 ? "SOMETHING STIRS BEYOND THE PORTAL" : "", THEME.banner); Sfx("wave"); Sfx("spawn"); }
                    break;
                case NetEv.Warp: if (started && !WARP.active) { P.inPortal = false; StartWarp(arg); } break;
                case NetEv.Cutscene: if (started && !CINE.active) { foreach (var e in enemies) if (e.g) Destroy(e.g); enemies.Clear(); boss = null; HideBossBar(); audioMgr.Stop(1); StartCutscene(); } break;
                case NetEv.GameOver: if (started) CoopGameOver(); break;
                case NetEv.Intro: case NetEv.IntroEnd: case NetEv.IntroVotes: RecvIntroEvent((NetEv)ev, arg); break;
                case NetEv.BossCine: case NetEv.BossCineAt: case NetEv.BossCineEnd: case NetEv.BossCineVotes: RecvBossCineEvent((NetEv)ev, arg); break;
            }
        }
    }
}
