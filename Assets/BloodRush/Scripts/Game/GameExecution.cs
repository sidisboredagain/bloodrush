using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BR
{
    // STAGGER + CHAINSAW EXECUTIONS, and persistent gore (blood decals, bleeding body parts, meat chunks).
    //  Staggered: a ground slam, a parried orb's blast, a point-blank shotgun blast, a core implosion or being yanked in by
    //  the grapple leaves an enemy reeling for a few seconds (a spinning red ring over its head). Walk up and press F:
    //  the world drops into slow motion, you lunge in, the chainsaw bites and grinds through it in a fountain of blood,
    //  then rips it apart - a big heal and an EXECUTION bonus.
    public partial class Game
    {
        const float STAG = 1.5f, EX_LUNGE = 0.2f;
        // kind: 0 = stab the chest and rip upward (split in two), 1 = horizontal cut at the waist, 2 = saw through the skull, 3 = drones
        class ExecState { public bool active, ripped; public Ent e; public int kind; public float t, rip, dur, sprayT, sfxT; public Vector3 from, anchor, hold; }
        class Corpse { public Transform h, spurt; public int mode; public float age, delay, maxAng, spurtT, ground; public Vector3 axis, v, spin, slide, p0; public Quaternion r0; public bool landed; public float fallY, fallV; }
        readonly List<Corpse> corpses = new List<Corpse>();
        Material cutMat;
        readonly ExecState EX = new ExecState();
        readonly Dictionary<Ent, (Transform ring, Transform glow, Material m)> stagFx = new Dictionary<Ent, (Transform, Transform, Material)>();
        float exFov, exRoll; Text execPrompt;

        void Stagger(Ent e)
        {
            if (e == null || e.dead || e.type == "boss" || e.spawnT > 0) return;
            NetClaimStagger(e); // co-op client: the host staggers its copy too
            if (e.heavy && e.stagT <= 0)
            {
                // the big brutes shrug off the first hit: it takes two stagger hits within 5 seconds
                if (time - e.stagHitT > 5) e.stagHits = 0;
                e.stagHitT = time; e.stagHits++;
                if (e.stagHits < 2) { e.stun = Mathf.Max(e.stun, 0.25f); SpawnParticles(TargetPoint(e), 5, 3, goldSpark, 0.06f, 0.3f, 0); return; }
                e.stagHits = 0;
            }
            bool fresh = e.stagT <= 0;
            e.stagT = STAG; e.stun = Mathf.Max(e.stun, STAG); e.attackT = 0; e.leaping = false; e.charging = false;
            if (fresh) { SpawnParticles(TargetPoint(e) + V(0, 0.5f, 0), 10, 4, goldSpark, 0.07f, 0.5f, 0); Sfx("growlLow", 0.6f); }
        }
        float ExecReach(Ent e) => 3.4f + e.hitR * 0.6f;
        // a boss knocked senseless (the Carnifex or the Frozen Traitor after charging into a wall): it can be executed
        static bool BossStunned(Ent e) => e.type == "boss" && (e.boss == 1 || e.boss == 3 || e.boss == 4 || e.boss == 5) && e.state == "stunned" && !e.execDone && !e.invuln && !e.dead;
        Ent ExecTarget()
        {
            Ent best = null; float bd = 1e9f; Vector3 o = EyePos, f = AimDir;
            foreach (var e in enemies)
            {
                bool bossOk = BossStunned(e);
                if (e.dead || (e.stagT <= 0 && !bossOk) || (e.type == "boss" && !bossOk)) continue;
                // a boss is a tall target: measure to the nearest point of its body's axis (and give it the width of its
                // body), so a stunned giant can be executed from the ground in front of, beside or behind it
                var bp = BodyPoint(e); if (e.type == "boss") bp = V(e.pos.x, Mathf.Clamp(o.y, e.pos.y + 1, e.pos.y + Mathf.Max(1, e.bodyY)), e.pos.z);
                var to = bp - o; float d = Mathf.Max(0.01f, to.magnitude - (e.type == "boss" ? e.hitR * 0.55f : 0));
                if (d > ExecReach(e) || Vector3.Dot(to / Mathf.Max(d, 1e-3f), f) < 0.3f || d >= bd) continue;
                best = e; bd = d;
            }
            return best;
        }
        bool TryExecute()
        {
            if (EX.active || P.dead || DE.active) return false;
            var e = ExecTarget(); if (e == null) return false;
            exAimSet = false; EX.active = true; EX.ripped = false; EX.e = e; EX.t = 0; EX.sprayT = 0; EX.sfxT = 0; EX.from = P.pos;
            EX.kind = e.type == "boss" ? 4 : e.type == "husk" && e.hr != null ? Random.Range(0, 3) : 3;
            if (EX.kind < 3 && int.TryParse(Arg("-brexeck") ?? "", out var fk)) EX.kind = fk;
            EX.rip = EX.kind == 0 ? 1.15f : EX.kind == 1 ? 0.74f : EX.kind == 2 ? 1.05f : 1.0f;
            EX.dur = EX.kind == 3 ? 1.4f : EX.rip + 0.9f;
            if (EX.kind == 4) { EX.rip = 1.0f; EX.dur = 1.4f; if (e.boss == 4) { EX.rip = 1.5f; EX.dur = 2.0f; } } // (the Hollow King: you leap onto his back first)
            var away = P.pos - e.pos; away.y = 0; away = away.sqrMagnitude > 1e-3f ? away.normalized : -FlatFwd;
            EX.anchor = e.pos + away * (e.type == "boss" ? 0.5f + e.hitR * 0.75f : 0.72f + e.hitR * 0.3f); EX.anchor.y = P.pos.y; EX.hold = e.pos; // close enough that the bar goes right through it
            if (e.type != "boss") e.stun = 99; e.vel = Vector3.zero;
            if (HK.state != "idle") ReleaseHook();
            fistT = SAW_DUR; sawHeld = true; inspectT = 0; mouseDown = false; P.iframes = Mathf.Max(P.iframes, EX.dur + 0.4f);
            Sfx("saw"); Sfx("dash"); P.shake = Mathf.Max(P.shake, 0.3f);
            return true;
        }
        static float Seg01(float t, float a, float b) => Mathf.Clamp01((t - a) / (b - a));
        // the left arm through each execution (camera space). The bar is aimed AT a point inside the body, so it really goes in.
        Vector3 exAim; bool exAimSet;
        void ExecArmPose(out Vector3 p, out Quaternion r)
        {
            float t = EX.t, rip = EX.rip; var e = EX.e; int K = EX.kind;
            if (e != null && !e.dead)
            {
                float rise = Smoother(Seg01(t, 0.42f, rip)), sw = Smoother(Seg01(t, 0.42f, rip));
                Vector3 w = K == 0 ? e.pos + V(0, Mathf.Lerp(e.bodyY, e.headY + 0.35f, rise), 0)
                          : K == 1 ? e.pos + V(0, e.bodyY - 0.25f, 0) + FlatRight * Mathf.Lerp(0.7f, -0.7f, sw)
                          : K == 2 ? e.pos + V(0, e.headY + (t < 0.38f ? 0.9f * Smoother(Seg01(t, EX_LUNGE, 0.38f)) : 0.05f), 0)
                          : BodyPoint(e);
                exAim = VmRoot().InverseTransformPoint(w); exAimSet = true;
            }
            if (!exAimSet) exAim = V(0, -0.2f, -1.2f);
            float roll; float jud;
            switch (K)
            {
                case 0: // thrust into the chest, then haul the buried bar up through the head
                {
                    float stab = Smoother(Seg01(t, EX_LUNGE, 0.4f)), rise = Smoother(Seg01(t, 0.42f, rip)), out_ = Smoother(Seg01(t, rip, rip + 0.3f));
                    p = Vector3.Lerp(Vector3.Lerp(V(-0.24f, -0.34f, -0.45f), V(-0.12f, -0.24f, -0.64f), stab), V(-0.1f, 0.02f, -0.6f), rise) + V(-0.15f, -0.3f, 0.25f) * out_;
                    roll = 35; jud = t > 0.4f && t < rip ? 1.2f : 0.3f; break;
                }
                case 1: // one savage sweep, the bar cleaving through the waist
                {
                    float sw = Smoother(Seg01(t, 0.42f, rip)), wind = Smoother(Seg01(t, EX_LUNGE, 0.42f)), out_ = Smoother(Seg01(t, rip, rip + 0.3f));
                    p = Vector3.Lerp(Vector3.Lerp(V(-0.24f, -0.34f, -0.45f), V(0.1f, -0.26f, -0.5f), wind), V(-0.42f, -0.26f, -0.52f), sw) + V(-0.1f, -0.25f, 0.2f) * out_;
                    roll = 80; jud = t > 0.45f && t < rip ? 0.8f : 0.2f; break;
                }
                case 2: // raised high, then driven down into the skull to grind
                {
                    float up = Smoother(Seg01(t, EX_LUNGE, 0.38f)), down = Seg01(t, 0.38f, 0.5f), back = Smoother(Seg01(t, rip, rip + 0.3f)); down *= down;
                    p = Vector3.Lerp(Vector3.Lerp(V(-0.24f, -0.34f, -0.45f), V(-0.14f, 0.1f, -0.42f), up), V(-0.1f, -0.06f, -0.6f), down) + V(-0.22f, -0.35f, 0.3f) * back;
                    roll = 30; jud = t > 0.5f && t < rip ? 1.6f : 0.3f; break;
                }
                default:
                {
                    float carve = Smoother(Seg01(t, EX_LUNGE, rip));
                    p = Vector3.Lerp(V(-0.19f, -0.2f, -0.57f), V(-0.13f, -0.31f, -0.66f), carve); roll = 40; jud = 0.6f; break;
                }
            }
            var j = V(rand - 0.5f, rand - 0.5f, rand - 0.5f);
            p += j * 0.016f * jud;
            var aimDir = (exAim - p); if (aimDir.sqrMagnitude < 1e-4f) aimDir = V(0, 0, -1);
            if (t > rip) aimDir = Vector3.Slerp(aimDir.normalized, V(0.2f, -0.5f, -1).normalized, Smoother(Seg01(t, rip, rip + 0.3f))); // it pulls free
            r = Quaternion.LookRotation(-aimDir.normalized, Vector3.up) * Quaternion.Euler(j.x * 4 * jud, j.y * 4 * jud, roll + j.z * 6 * jud);
        }        // runs on real time (the world itself is slowed while it plays)
        void UpdateExecution(float rdt)
        {
            UpdateCorpses(rdt);
            if (!EX.active) { exFov += (0 - exFov) * (1 - Mathf.Exp(-6 * rdt)); exRoll += (0 - exRoll) * (1 - Mathf.Exp(-6 * rdt)); return; }
            EX.t += rdt; var e = EX.e; float t = EX.t; int K = EX.kind;
            fistT = SAW_DUR; sawExt = Mathf.Min(1, sawExt + rdt * 8); P.vel = Vector3.zero;
            bool alive = e != null && !e.dead;
            bool onBack = K == 4 && e != null && e.boss == 4 && e.hk != null;
            if (onBack)
            {
                // the Hollow King: a leap up onto his back (an arc), then you ride him there, sawing into the base of his neck
                var back = e.hk.torso.t.TransformPoint(V(0, 1.75f, -0.6f)); float j = Smoother(Mathf.Clamp01(t / 0.5f));
                P.pos = Vector3.Lerp(EX.from, back, j) + V(0, Mathf.Sin(j * Mathf.PI) * 5, 0) + (t > 0.5f ? Random.insideUnitSphere * 0.06f : Vector3.zero);
            }
            else P.pos = Vector3.Lerp(EX.from, EX.anchor, Smoother(t / EX_LUNGE));
            float build = Seg01(t, EX_LUNGE, EX.rip), rise = Smoother(Seg01(t, 0.42f, EX.rip));
            if (alive)
            {
                // the camera locks on: the chest (sliding up to the head as the split rises), the waist, or the skull
                float lookH = K == 0 ? Mathf.Lerp(e.bodyY, e.headY + 0.5f, rise) : K == 1 ? e.bodyY - 0.2f : K == 2 ? e.headY + 0.1f : e.bodyY;
                var d = (onBack ? e.hk.neck.t.position : e.pos + V(0, lookH, 0)) - EyePos;
                float yaw = Mathf.Atan2(-d.x, -d.z), pitch = Mathf.Atan2(d.y, Hypot(d.x, d.z)), k = 1 - Mathf.Exp(-16 * rdt);
                float dy = Mathf.Atan2(Mathf.Sin(yaw - P.yaw), Mathf.Cos(yaw - P.yaw)); P.yaw += dy * k; P.pitch += (pitch - P.pitch) * k;
            }
            exFov = t < EX.rip ? Mathf.Lerp(0, -18, Smoother(t / 0.3f)) : Mathf.Lerp(-18, 10, Smoother((t - EX.rip) / 0.12f)) * (1 - Smoother((t - EX.rip - 0.2f) / 0.35f));
            exRoll = K == 1 ? Mathf.Lerp(0.16f, -0.24f, Smoother(Seg01(t, 0.42f, EX.rip))) * (1 - Smoother(Seg01(t, EX.rip + 0.2f, EX.rip + 0.6f))) : Mathf.Sin(build * Mathf.PI) * 0.1f;
            if (alive && K == 4)
            {
                // the boss: it stays down while the saw tears into its chest (it is far too big to hoist)
                if (!EX.ripped) e.stateT = Mathf.Max(e.stateT, 0.6f); e.vel = Vector3.zero; // (held down only until the rip - after it he gets up)
                if (t > 0.3f && t < EX.rip)
                {
                    e.flash = 0.07f; var cpt = onBack ? e.hk.neck.t.position : BodyPoint(e); var contact = Vector3.Lerp(cpt, sawNose ? sawNose.position : cpt, 0.5f);
                    EX.sprayT -= rdt; if (EX.sprayT <= 0) { EX.sprayT = 0.03f; Spray(contact, (FlatRight * (rand < 0.5f ? -1 : 1) + Vector3.up * 1.2f).normalized, 8, 11, 0.45f, bloodMat, 0.11f, 1.3f); SpawnParticles(contact, 4, 8, sparkMat, 0.04f, 0.2f); }
                    EX.sfxT -= rdt; if (EX.sfxT <= 0) { EX.sfxT = 0.08f; Sfx("sawHit"); }
                    P.shake = Mathf.Max(P.shake, 0.4f);
                }
            }
            else if (alive && t > EX_LUNGE && t < EX.rip)
            {
                e.stun = 99; e.vel = Vector3.zero; e.flash = 0.07f; e.flinch = 1;
                float lift = K == 0 ? 0.2f + 0.7f * rise : K == 2 ? 0.08f : 0.12f;
                bool biting = K == 0 ? t > 0.4f : K == 2 ? t > 0.5f : K == 3 ? true : t > 0.5f;
                var shake = V(rand - 0.5f, rand - 0.5f, rand - 0.5f) * (biting ? 0.13f : 0.03f);
                e.pos = EX.hold + V(0, lift * Smoother(build / 0.25f), 0) + shake;
                if (biting)
                {
                    var target = K == 0 ? e.pos + V(0, Mathf.Lerp(e.bodyY, e.headY + 0.25f, rise), 0) : K == 2 ? e.pos + V(0, e.headY, 0) : BodyPoint(e);
                    var contact = Vector3.Lerp(target, sawNose ? sawNose.position : target, 0.5f); // where the bar is buried
                    EX.sprayT -= rdt;
                    if (EX.sprayT <= 0)
                    {
                        EX.sprayT = 0.03f; var side = FlatRight * (rand < 0.5f ? -1 : 1); // sprays out to the sides and up, never into the lens
                        Spray(contact, (side + V(0, K == 0 ? 1.6f : 1.0f, 0) + Random.insideUnitSphere * 0.3f).normalized, 6, 10, 0.4f, bloodMat, 0.09f, 1.3f);
                        if (rand < 0.5f) Spray(contact, Vector3.up, 3, 11, 0.6f, gibMat, 0.1f, 1.4f);
                        SpawnParticles(contact, K == 2 ? 6 : 3, 8, sparkMat, 0.04f, 0.2f);
                        if (K == 2 && rand < 0.4f) Spray(contact, Vector3.up, 2, 7, 0.7f, M.bone, 0.07f, 1.2f); // bone chips off the skull
                    }
                    EX.sfxT -= rdt; if (EX.sfxT <= 0) { EX.sfxT = 0.08f; Sfx("sawHit"); }
                    P.shake = Mathf.Max(P.shake, 0.38f); hurtFlash = Mathf.Max(hurtFlash, 0.08f);
                }
                Heal(rdt * 12);
            }
            if (!EX.ripped && (t >= EX.rip || !alive)) { EX.ripped = true; if (alive) Rip(e, K); }
            if (t >= EX.dur)
            {
                if (onBack) { var bf = V(Mathf.Sin(e.yaw), 0, Mathf.Cos(e.yaw)); P.vel = -bf * 14 + V(0, 12, 0); P.onGround = false; P.coyote = 0; P.iframes = Mathf.Max(P.iframes, 0.8f); } // you kick off his back
                EX.active = false; EX.e = null; P.iframes = Mathf.Max(P.iframes, 0.3f); fistT = 0; sawHeld = false;
            } // the saw stops: no stray bite on whoever is standing nearby
        }
        // the kill: the body is cloned into the pieces it's cut into, the real one is removed, and the pieces play out
        void Rip(Ent e, int K)
        {
            var c = BodyPoint(e); var fwd = FlatFwd; var right = FlatRight; var dir = (AimDir + V(0, 1.1f, 0)).normalized;
            if (K == 4)
            {
                // a brutal chunk out of the boss: 15% of its max health (the stunned x1.5 is already counted in)
                DamageEnemy(e, e.maxHp * 0.15f / 1.5f, new Hit { punch = true, close = true, point = c, dir = dir, kb = 0 });
                e.execDone = true; // one execution per stun: spamming F can't start another, and he gets straight up
                if (!e.dead && e.state == "stunned") { e.stateT = 0; e.stagT = 0; }
                Spray(c, Vector3.up, 60, 14, 0.5f, bloodMat, 0.12f, 1.6f); MeatChunks(c, 12, 9, Vector3.up);
                Heal(30); AddStyle("BOSS EXECUTION", 600);
                freeze = Mathf.Max(freeze, 0.15f); whiteFlash = Mathf.Max(whiteFlash, 0.12f); P.shake = 1.0f; P.kick += 0.08f;
                Sfx("slam"); Sfx("roarShort"); return;
            }
            if (cutMat == null) cutMat = G(Mats.Std(0x8a0808, 0.45f, 0, 0x2a0000, null, true));
            string title = "EXECUTION";
            if (K < 3 && e.hr != null)
            {
                if (xrayOn.Contains(e)) SetXRay(e, false);
                var src = e.hr.root.t; float s = Mathf.Max(1, e.s), gY = FloorBelow(e.pos);
                if (K == 0)
                {
                    // split down the middle: two halves peel apart and fall to either side
                    title = "SPLIT IN TWO";
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        var h = CloneBody(src, e.pos, out var clone);
                        // keep only this side's arm and leg; the trunk (and the head on it) is halved, its cut face on the centre line
                        foreach (var lim in new[] { e.hr.sh[0], e.hr.sh[1], e.hr.thigh[0], e.hr.thigh[1] })
                        {
                            if (lim == null) continue; var tw = Twin(src, clone, lim.t);
                            if (Mathf.Sign(lim.t.localPosition.x) != sd) { Destroy(tw.gameObject); continue; }
                            if (lim == e.hr.sh[0] || lim == e.hr.sh[1]) { var lp = tw.localPosition; lp.x = (lp.x - sd * 0.075f) / 0.66f; tw.localPosition = lp; var ls = tw.localScale; ls.x /= 0.66f; tw.localScale = ls; }
                        }
                        var tt = Twin(src, clone, e.hr.torso.t); var tsc = tt.localScale; tsc.x *= 0.66f; tsc.z *= 1.05f; tt.localScale = tsc; tt.localPosition += V(sd * 0.075f, 0, 0);
                        foreach (Transform ch in Twin(src, clone, e.hr.hips.t)) if (ch.GetComponent<MeshFilter>()) { var cs = ch.localScale; cs.x *= 0.66f; ch.localScale = cs; ch.localPosition += V(sd * 0.075f, 0, 0); }
                        var outward = right * sd;
                        var sp = new GameObject("spurt").transform; sp.SetParent(h, false); sp.position = c + outward * 0.05f + V(0, 0.3f, 0);
                        corpses.Add(new Corpse { h = h, mode = 0, delay = 0.08f, maxAng = 86, axis = Vector3.Cross(Vector3.up, outward), slide = outward * 1.3f, p0 = h.position, r0 = h.rotation, spurt = sp, spurtT = 1.2f, ground = gY });
                    }
                    Spray(c + V(0, 0.6f, 0), Vector3.up, 60, 16, 0.3f, bloodMat, 0.11f, 1.8f);
                    for (int i = 0; i < 10; i++) { var p = c + V(0, (i - 5) * 0.2f, 0); Spray(p, (right * (i % 2 == 0 ? 1 : -1) + Vector3.up * 0.4f).normalized, 5, 9, 0.4f, bloodMat, 0.12f, 1.2f); }
                    MeatChunks(c, 10, 8, Vector3.up);
                }                else if (K == 1)
                {
                    // cut in half at the waist: the top half is hurled up and back spinning, the legs stand a moment, then fold
                    title = "CUT IN HALF";
                    var torso = e.hr.torso.t; var waist = torso.position;
                    var up = CloneBody(src, waist, out var upClone);
                    foreach (var th in e.hr.thigh) if (th != null) Destroy(Twin(src, upClone, th.t).gameObject);
                    foreach (Transform ch in Twin(src, upClone, e.hr.hips.t)) if (ch != Twin(src, upClone, torso)) ch.gameObject.SetActive(false); // nothing below the cut
                    var lo = CloneBody(src, e.pos, out var loClone);
                    Destroy(Twin(src, loClone, torso).gameObject);
                    var sp = new GameObject("spurt").transform; sp.SetParent(loClone, false); sp.position = waist + V(0, 0.05f, 0);
                    corpses.Add(new Corpse { h = up, mode = 1, v = Vector3.up * 8 + fwd * 5 + right * (rand - 0.5f) * 2, spin = right * -11 + Vector3.up * (rand - 0.5f) * 4, ground = gY, p0 = up.position, r0 = up.rotation });
                    corpses.Add(new Corpse { h = lo, mode = 0, delay = 0.55f, maxAng = 86, axis = Vector3.Cross(Vector3.up, fwd), slide = fwd * 0.5f, p0 = lo.position, r0 = lo.rotation, spurt = sp, spurtT = 1.8f, ground = gY });
                    Spray(waist, fwd + Vector3.up * 0.6f, 40, 13, 0.5f, bloodMat, 0.11f, 1.5f); Spray(waist, right, 25, 13, 0.4f, bloodMat, 0.13f, 1.3f); Spray(waist, -right, 25, 13, 0.4f, bloodMat, 0.13f, 1.3f);
                    MeatChunks(waist, 10, 9, fwd);
                }
                else
                {
                    // the skull bursts; the headless body stands twitching, the neck fountains blood, then it drops
                    title = "SKULL BREAKER";
                    var head = e.hr.head.t; var hp = head.position;
                    var b = CloneBody(src, e.pos, out var bClone);
                    Destroy(Twin(src, bClone, head).gameObject);
                    var sp = new GameObject("spurt").transform; sp.SetParent(bClone, false); sp.position = e.hr.neck.t.position + V(0, 0.08f, 0);
                    corpses.Add(new Corpse { h = b, mode = 0, delay = 0.95f, maxAng = 88, axis = Vector3.Cross(Vector3.up, fwd), slide = fwd * 0.5f, p0 = b.position, r0 = b.rotation, spurt = sp, spurtT = 2.0f, ground = gY });
                    Spray(hp, Vector3.up, 60, 13, 0.6f, bloodMat, 0.11f, 1.6f); Spray(hp, right, 20, 11, 0.5f, bloodMat, 0.1f, 1.3f); Spray(hp, -right, 20, 11, 0.5f, bloodMat, 0.1f, 1.3f);
                    Blast(hp, 0.7f, 0xb00000);
                    for (int i = 0; i < 9; i++) // skull fragments and the two burning eyes
                    {
                        var g = Mk.Msh(world, Geo.Sphere(1, 6, 4), M.bone).transform; g.gameObject.layer = Mk.LayerIgnore; float sz = 0.06f + rand * 0.08f; g.localScale = V(sz, sz * 0.5f, sz); g.position = hp;
                        particles.Add(new Pt { m = g, v = (Random.onUnitSphere + Vector3.up) * (6 + rand * 6), life = 5, grav = GRAV, hasSpin = true, spin = Random.insideUnitSphere * 14, owned = true, bleed = true });
                    }
                    for (int i = 0; i < 2; i++) { var g = Mk.Glow(world, 0xff3010, 0.35f, 1).transform; g.position = hp; particles.Add(new Pt { m = g, v = (Random.onUnitSphere + Vector3.up) * 7, life = 1.6f, grav = GRAV, owned = true }); }
                    MeatChunks(hp, 6, 9, Vector3.up);
                }
                foreach (var cr in corpses) if (cr.h) Mk.SetLayerRecursive(cr.h.gameObject, Mk.LayerIgnore);
            }
            e.stun = 0;
            DamageEnemy(e, 99999, new Hit { punch = true, exec = true, close = true, point = c, dir = dir, kb = 4 });
            Heal(40); AddStyle(title, 400);
            freeze = Mathf.Max(freeze, 0.15f); whiteFlash = Mathf.Max(whiteFlash, 0.12f); hurtFlash = Mathf.Max(hurtFlash, 0.25f); P.shake = 1.0f; P.kick += 0.08f;
            Sfx("slam"); Sfx("boomSmall"); Sfx("kill");
        }
        // a copy of an enemy's body (meshes only), parented under a holder at pivot so it can be toppled or flung about that point
        Transform CloneBody(Transform src, Vector3 pivot, out Transform clone)
        {
            var h = new GameObject("corpse").transform; h.SetParent(world, false); h.position = pivot; h.rotation = src.rotation;
            clone = Instantiate(src.gameObject, src.position, src.rotation, h).transform; clone.localScale = src.lossyScale;
            Mk.StripColliders(clone.gameObject); StripXRay(clone.gameObject);
            foreach (var r in clone.GetComponentsInChildren<Renderer>(true)) // the see-through outline must not carry over to the corpse
            {
                var ms = r.sharedMaterials; if (System.Array.IndexOf(ms, xrayMat) < 0 && System.Array.IndexOf(ms, xrayMask) < 0) continue;
                var keep = new List<Material>(); foreach (var m in ms) if (m && m != xrayMat && m != xrayMask) keep.Add(m); r.sharedMaterials = keep.ToArray();
            }
            return h;
        }
        float FloorBelow(Vector3 p)
        {
            bool hb = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
            float y = Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out var hit, 60, LEVEL_MASK, QueryTriggerInteraction.Ignore) ? hit.point.y : 0;
            Physics.queriesHitBackfaces = hb; return y;
        }
        // the node in a clone that matches node in the original
        static Transform Twin(Transform srcRoot, Transform cloneRoot, Transform node)
        {
            var path = new List<int>(); for (var x = node; x != null && x != srcRoot; x = x.parent) path.Add(x.GetSiblingIndex());
            var y = cloneRoot; for (int i = path.Count - 1; i >= 0; i--) y = y.GetChild(path[i]); return y;
        }
        void UpdateCorpses(float rdt)
        {
            float dt = rdt * (EX.active ? 0.6f : 1);
            for (int i = corpses.Count - 1; i >= 0; i--)
            {
                var c = corpses[i]; if (!c.h) { corpses.RemoveAt(i); continue; }
                c.age += dt;
                if (c.mode == 0)
                {
                    // topple about the feet: slow to start, accelerating like a falling body, a small bounce as it lands
                    float a = c.age - c.delay;
                    if (a > 0)
                    {
                        float k = Mathf.Clamp01(a / 0.6f), ang = c.maxAng * k * k;
                        if (k >= 1) ang = c.maxAng - Mathf.Abs(Mathf.Sin(Mathf.Clamp01((a - 0.6f) / 0.3f) * Mathf.PI)) * 7;
                        if (k >= 1 && !c.landed) { c.landed = true; Decal(c.p0 + c.slide + V(0, 0, 0), 1.6f, true); Sfx("bruteHit", 0.6f); SpawnParticles(c.p0 + c.slide, 8, 4, dustMat, 0.12f, 0.5f); }
                        c.h.rotation = Quaternion.AngleAxis(ang, c.axis) * c.r0;
                    }
                    else c.h.rotation = Quaternion.Euler(Mathf.Sin(c.age * 40) * 1.5f, 0, Mathf.Cos(c.age * 33) * 1.5f) * c.r0; // twitching before it drops
                    if (c.p0.y + c.fallY > c.ground + 0.001f) { c.fallV -= GRAV * dt; c.fallY = Mathf.Max(c.ground - c.p0.y, c.fallY + c.fallV * dt); } // drops if it was killed in the air
                    c.h.position = c.p0 + c.slide * Smoother(Mathf.Clamp01((c.age - c.delay) / 0.6f)) + Vector3.up * c.fallY;
                }
                else
                {
                    // flung: tumbles through the air, bounces and slides to a stop
                    c.v.y -= GRAV * dt; c.h.position += c.v * dt; c.h.rotation = Quaternion.Euler(c.spin * (dt * Mathf.Rad2Deg)) * c.h.rotation;
                    if (c.h.position.y < c.ground + 0.25f && c.v.y < 0)
                    {
                        var p = c.h.position; p.y = c.ground + 0.25f; c.h.position = p;
                        if (!c.landed) { c.landed = true; Decal(p, 1.5f, true); Sfx("bruteHit", 0.6f); }
                        c.v = V(c.v.x * 0.4f, -c.v.y * 0.25f, c.v.z * 0.4f); c.spin *= 0.4f; if (Mathf.Abs(c.v.y) < 1) { c.v.y = 0; c.spin = Vector3.zero; c.v *= 0.8f; }
                    }
                    if (rand < 0.5f) particles.Add(new Pt { pos = c.h.position, scale = Vector3.one * 0.08f, mat = bloodMat, v = V(rand - 0.5f, -1, rand - 0.5f), life = 1, grav = GRAV });
                }
                if (c.spurt && c.spurtT > 0)
                {
                    c.spurtT -= dt; float pump = 0.6f + 0.4f * Mathf.Sin(c.age * 18); // pulsing with the last heartbeats
                    if (rand < 0.85f) Spray(c.spurt.position, (c.spurt.parent ? c.spurt.parent.up : Vector3.up) * 0.8f + Vector3.up * 0.5f, 2 + (int)(pump * 3), 6 + pump * 5, 0.3f, bloodMat, 0.09f, 1.1f);
                }
                if (c.age > 7) c.h.position += Vector3.down * dt * 0.4f; // sink away
                if (c.age > 9.5f) { Mk.DestroyTree(c.h.gameObject); corpses.RemoveAt(i); }
            }
        }        // staggered enemies: a spinning red ring over the head; it flares up when you're in execution range
        void UpdateStagger(float dt)
        {
            Ent target = EX.active ? null : ExecTarget();
            foreach (var e in enemies)
            {
                if (e.stagT <= 0) continue;
                if (!EX.active || EX.e != e) e.stagT -= dt;
                if (e.type == "drone") e.vel *= Mathf.Exp(-4 * dt);
            }
            var gone = new List<Ent>();
            foreach (var kv in stagFx) if (kv.Key.dead || (kv.Key.stagT <= 0 && !BossStunned(kv.Key)) || !enemies.Contains(kv.Key)) gone.Add(kv.Key);
            foreach (var e in gone) { var f = stagFx[e]; if (f.ring) Mk.DestroyTree(f.ring.gameObject); if (f.glow) Mk.DestroyTree(f.glow.gameObject); stagFx.Remove(e); }
            foreach (var e in enemies)
            {
                bool bs = BossStunned(e);
                if (e.dead || (e.stagT <= 0 && !bs)) continue;
                if (!stagFx.TryGetValue(e, out var fx))
                {
                    var m = Mats.Trans(0xff2010, 0.95f, true, false); // alpha-blended so it reads on the bright heaven sky too
                    var ring = Mk.Msh(world, Geo.Torus(0.24f, 0.035f, 28, 5), m).transform; ring.gameObject.layer = Mk.LayerIgnore;
                    var core = Mk.Msh(ring, Geo.Torus(0.24f, 0.016f, 28, 4), Mats.Trans(0x200000, 0.9f, true, false), 0, 0, 0); core.layer = Mk.LayerIgnore; core.transform.localScale = V(1.12f, 1.12f, 1.6f);
                    for (int k = 0; k < 3; k++) { var sp = Mk.Msh(ring, Geo.Cone(0.04f, 0.13f, 5), m, Mathf.Cos(k * 2.09f) * 0.24f, 0, Mathf.Sin(k * 2.09f) * 0.24f).transform; sp.localRotation = Quaternion.Euler(0, -k * 120f, -90); sp.gameObject.layer = Mk.LayerIgnore; }
                    if (bs) // a stunned boss: little stars circle its head as well
                    {
                        var star = Mats.Basic(0xffd84a);
                        for (int k = 0; k < 5; k++) { float a = k * Mathf.PI * 2 / 5; var st = Mk.Msh(ring, Geo.Octahedron(0.06f), star, Mathf.Cos(a) * 0.34f, Mathf.Sin(a * 2) * 0.04f, Mathf.Sin(a) * 0.34f); st.layer = Mk.LayerIgnore; st.transform.localScale = V(1, 1.5f, 1); }
                    }
                    var glow = Mk.Glow(world, 0xff4020, 1.6f, 0.6f).transform;
                    fx = (ring, glow, m); stagFx[e] = fx;
                }
                bool hot = e == target; float pulse = 0.5f + 0.5f * Mathf.Sin(time * (hot ? 22 : 9 + (STAG - e.stagT) * 3));
                var top = bs ? e.pos + V(0, e.headY + 1.6f + Mathf.Sin(time * 2) * 0.15f, 0) : e.pos + V(0, (e.type == "husk" ? e.headY : 0) + 0.9f * Mathf.Max(1, e.s * 0.8f), 0);
                float sc = bs ? 4.2f : Mathf.Max(1, e.s * 0.7f);
                fx.ring.position = top; fx.ring.rotation = Quaternion.Euler(Mathf.Sin(time * 3) * 12, time * 240, 0);
                fx.ring.localScale = Vector3.one * (hot ? 1.25f + pulse * 0.15f : 1 + pulse * 0.1f) * sc;
                fx.glow.position = top; fx.glow.localScale = Vector3.one * (hot ? 1.3f : 0.8f + pulse * 0.25f) * (bs ? 3 : 1);
                Mats.SetColor(fx.m, hot ? 0xff6a10 : 0xff1a0a); Mats.SetAlpha(fx.m, 0.75f + 0.25f * pulse);
            }
            if (execPrompt == null && hudRT)
            {
                execPrompt = Txt(hudRT, "", 24, C("#ff3a3a"), TextAnchor.MiddleCenter, false, 6, 3);
                Place(execPrompt.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, -64, 600, 32);
            }
            if (execPrompt) execPrompt.text = target != null ? (Mathf.Sin(time * 12) > -0.3f ? "F   EXECUTE" : "") : "";
        }
        void ClearStagger()
        {
            foreach (var kv in stagFx) { if (kv.Value.ring) Mk.DestroyTree(kv.Value.ring.gameObject); if (kv.Value.glow) Mk.DestroyTree(kv.Value.glow.gameObject); }
            stagFx.Clear(); foreach (var cr in corpses) if (cr.h) Mk.DestroyTree(cr.h.gameObject); corpses.Clear(); EX.active = false; EX.e = null; exFov = 0; exRoll = 0; if (execPrompt) execPrompt.text = "";
        }

        // ---------- gore: blood decals that stay on the floor ----------
        const int DECALS = 170; const float DECAL_LIFE = 16;
        readonly List<(Transform t, float born, float size)> decals = new List<(Transform, float, float)>();
        Material[] decalMats, decalBlue; float decalT;
        void Decal(Vector3 p, float size, bool force = false)
        {
            if (!force) { if (time - decalT < 0.012f) return; decalT = time; }
            // splats always lie on the floor, even for a kill in mid-air - but never on an invisible collision box (the box round a
            // car, a boulder, a wreck doesn't follow the visible shape, so a splat there would float): those get none
            {
                bool hb = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
                bool hit = Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out var rh, 60, LEVEL_MASK, QueryTriggerInteraction.Ignore);
                Physics.queriesHitBackfaces = hb;
                if (hit && rh.collider.gameObject.name == "solid") return;
                p.y = hit ? rh.point.y : 0;
            }
            if (decalMats == null)
            {
                decalMats = new[] { G(Mats.Trans(0x8a0606, 0.92f)), G(Mats.Trans(0x5a0303, 0.9f)), G(Mats.Trans(0xa01010, 0.85f)) }; // shared by every splat: kept (G) so a splat drying up can't destroy it
                foreach (var m in decalMats) m.SetTexture("_BaseMap", Tex.BloodSplat);
            }
            if (decalBlue == null) { decalBlue = new[] { G(Mats.Trans(0x1a5aa8, 0.85f)), G(Mats.Trans(0x2a7ad0, 0.8f)), G(Mats.Trans(0x0e3a78, 0.85f)) }; foreach (var m in decalBlue) m.SetTexture("_BaseMap", Tex.BloodSplat); }
            var dm = frostGore ? decalBlue : decalMats;
            Transform t;
            if (decals.Count >= DECALS) { t = decals[0].t; decals.RemoveAt(0); if (!t) return; t.GetComponent<MeshRenderer>().sharedMaterial = dm[decals.Count % 3]; }
            else { t = Mk.Msh(world, Geo.Plane(1, 1), dm[decals.Count % 3]).transform; t.gameObject.layer = Mk.LayerIgnore; t.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            t.position = new Vector3(p.x, p.y + 0.012f + (decals.Count % 20) * 0.0007f, p.z);
            t.rotation = Quaternion.Euler(90, rand * 360, 0); t.localScale = Vector3.one * size;
            decals.Add((t, time, size));
        }
        void UpdateDecals()
        {
            for (int i = decals.Count - 1; i >= 0; i--)
            {
                var d = decals[i]; if (!d.t) { decals.RemoveAt(i); continue; }
                float age = time - d.born;
                if (age > DECAL_LIFE) { Mk.DestroyTree(d.t.gameObject); decals.RemoveAt(i); continue; }
                float k = age < 0.15f ? Mathf.Lerp(0.5f, 1, age / 0.15f) : age > DECAL_LIFE - 1.5f ? (DECAL_LIFE - age) / 1.5f : 1; // splats spread in, then dry up
                d.t.localScale = Vector3.one * d.size * k;
            }
        }
        void ClearDecals() { foreach (var d in decals) if (d.t) Mk.DestroyTree(d.t.gameObject); decals.Clear(); }
        // chunks of meat torn loose on a kill: real little pieces that tumble, bleed and bounce
        void MeatChunks(Vector3 c, int n, float speed, Vector3? dir)
        {
            for (int i = 0; i < n; i++)
            {
                var g = Mk.Msh(world, i % 3 == 0 ? Geo.Box(1, 0.7f, 0.8f) : Geo.Sphere(1, 7, 5), i % 2 == 0 ? gibMat : bloodMat).transform;
                g.gameObject.layer = Mk.LayerIgnore; float s = 0.08f + rand * 0.14f; g.localScale = V(s, s * (0.7f + rand * 0.6f), s); g.position = c + Random.insideUnitSphere * 0.3f;
                var v = V(rand - 0.5f, rand * 0.8f + 0.4f, rand - 0.5f) * speed; if (dir.HasValue) v += dir.Value * speed * 0.5f;
                particles.Add(new Pt { m = g, v = v, life = 5 + rand * 3, grav = GRAV, hasSpin = true, spin = V(rand * 16 - 8, rand * 16 - 8, rand * 16 - 8), owned = true, bleed = true });
            }
            TrimParticles();
        }
    }
}
