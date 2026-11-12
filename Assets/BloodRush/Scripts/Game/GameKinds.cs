using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    // New enemy kinds for the Frozen Circle, Dead Earth and Purgatory acts. Each one is a husk, brute or drone underneath
    // (so executions, x-ray, gore and hazards all work on it) with its own look and a behaviour layered on top:
    //   FROZEN CIRCLE  frost      frostbitten husk - its melee and its death burst chill you (slow)
    //                  wraith     pale ghost - blinks in behind you and strikes at once
    //                  glacier    brute with an ice shield: shots from the front barely scratch it; slam or shotgun it off
    //                  spitter    drone that spits arcing ice balls (chill)
    //   DEAD EARTH     scav       small, fast, comes in packs of three
    //                  rust       plain rust-red husk
    //                  bomber     runs at you with a lit charge, fuses and blows up (it hurts its friends too)
    //                  tank       brute with a fuel tank on its back - it explodes when it dies, so kill it near the pack
    //                  sniper     drone that hangs far back, paints you with a red laser, then fires a fast slug
    //                  rustdrone  plain rusted drone
    //   PURGATORY      hollow     plain hooded grey husk
    //                  twin       mirror-black copy with a revolver: keeps its distance and shoots
    //                  pilgrim    slow hooded brute with a lantern - enemies near it take far less damage; kill it first
    //                  clock      drone that rewinds to full health unless you kill it within two seconds of hurting it
    //                  bell       drone with a bell - each toll hastes every enemy nearby
    //                  greydrone  plain grey-gold drone
    public partial class Game
    {
        float chillT, hazSlowT; // player slowed by ice, or by a hazard (tide, time bubble) - see UpdatePlayer
        string Act => LEVEL == null ? "" : IsFrostTheme(LEVEL.theme) ? "frost" : LEVEL.theme == "ruin" ? "ruin" : LEVEL.theme == "limbo" ? "limbo" : "";
        int ActLevel => ((levelNum - 1) % 5) + 1; // 1..5 within the act
        bool bellThisWave; int bellWave = -1;

        string RollHuskKind(bool brute)
        {
            float r = rand; int al = ActLevel;
            if (testMode && (!brute || Arg("-brkindall") != null) && Arg("-brkind") != null) return Arg("-brkind"); // (test: force a kind)
            switch (Act)
            {
                case "frost": return brute ? "glacier" : "frost"; // no wraiths in the Frozen Circle
                case "ruin": return brute ? "tank" : (al >= 2 && r < 0.2f + 0.06f * (al - 2) ? "bomber" : r < 0.55f ? "scav" : "rust"); // (Burners grow commoner from wave 12)
                case "limbo": return brute ? "pilgrim" : (al >= 2 && r < 0.25f ? "twin" : "hollow");
            }
            return "";
        }
        string RollDroneKind()
        {
            float r = rand; int al = ActLevel;
            if (bellWave != wave) { bellWave = wave; bellThisWave = false; }
            if (testMode && Arg("-brdrone") != null) return Arg("-brdrone"); // (test: force a kind)
            switch (Act)
            {
                case "frost": return "spitter"; // the only "drone" here, and it doesn't fly: it clings to a wall (see SpitterAnchor)
                case "ruin": return "rustdrone"; // (no laser-sighted snipers in Dead Earth)
                case "limbo":
                    if (al >= 3 && !bellThisWave) { bellThisWave = true; return "bell"; }
                    return r < 0.6f ? "clock" : "greydrone";
            }
            return "";
        }

        // ---------- the look ----------
        void Tint(Ent e, int skin, int bone, int dark, int glow)
        {
            if (e.mats.Count > 0) Mats.SetColor(e.mats[0], skin);
            if (e.mats.Count > 1) Mats.SetColor(e.mats[1], bone);
            if (e.mats.Count > 2) Mats.SetColor(e.mats[2], dark);
            if (e.hr != null && e.hr.glow) Mats.SetColor(e.hr.glow, glow);
        }
        void DressHusk(Ent e, string kind)
        {
            var r = e.hr; e.kind = kind;
            if (r.ruin) return; // Dead Earth's bodies are dressed already (GameRuinMobs)
            switch (kind)
            {
                case "frost":
                {
                    if (r.frost) break; // the Rimebound Thrall has its own body (GameFrostMobs)
                    Tint(e, 0x7aa0c0, 0xe0f4ff, 0x203040, 0x60e8ff);
                    var ice = EM(e, Mats.Std(0xc8ecff, 0.15f, 0.1f, 0x0a3048));
                    foreach (var sx in new[] { -1, 1 }) for (int k = 0; k < 3; k++) Mk.Msh(r.sh[sx < 0 ? 0 : 1], Geo.Cone(0.05f, 0.22f + k * 0.06f, 5), ice, sx * 0.04f, 0.1f + k * 0.02f, (k - 1) * 0.08f, 0, 0, -sx * (0.4f + k * 0.2f));
                    for (int k = 0; k < 4; k++) Mk.Msh(r.torso, Geo.Cone(0.05f, 0.2f, 5), ice, (k - 1.5f) * 0.1f, 0.5f, -0.16f, -2.4f);
                    Mk.Head(Mk.Msh(r.head, Geo.Cone(0.06f, 0.2f, 5), ice, 0.08f, 0.28f, 0, -0.2f), Mk.Msh(r.head, Geo.Cone(0.05f, 0.16f, 5), ice, -0.07f, 0.27f, 0.02f, 0.2f));
                    break;
                }
                case "wraith":
                {
                    Tint(e, 0xc8d4e0, 0x8aa0b8, 0x405060, 0xa0f0ff);
                    var cloth = EM(e, Mats.Std(0x9aaabb, 0.9f, 0, 0x0a1a2a, null, true));
                    Mk.Head(Mk.Msh(r.head, Geo.Cone(0.3f, 0.55f, 8), cloth, 0, 0.22f, -0.06f));
                    for (int k = 0; k < 6; k++) Mk.Msh(r.hips, Geo.Box(0.09f, 0.7f + (k % 3) * 0.15f, 0.02f), cloth, (k - 2.5f) * 0.08f, -0.4f, k % 2 == 0 ? 0.13f : -0.13f, (k % 2 == 0 ? 0.15f : -0.15f), 0, 0);
                    e.tpT = 3 + rand * 2;
                    break;
                }
                case "glacier":
                {
                    if (!r.frost) Tint(e, 0x5a7890, 0xd8f0ff, 0x1a2838, 0x60e8ff);
                    // a kite-shaped tower shield cut from glacier ice in an iron frame, carried in front of the body on a grip
                    // (it hangs off the torso, not the swinging forearm, and the left arm holds a guard pose - see AnimateHusk)
                    var ice = EM(e, Mats.Std(0x7ab8e0, 0.06f, 0.2f, 0x0a2a48)); var pale = EM(e, Mats.Std(0xd8f2ff, 0.12f, 0.1f, 0x103a58));
                    var iron = EM(e, Mats.Std(0x2a2e36, 0.45f, 0.75f)); var rune = EM(e, Mats.Basic(0x7af0ff));
                    var sh = Mk.Piv(r.torso, -0.16f, 0.36f, 0.92f); sh.SetRot(0.05f, 0.14f, 0);
                    const float W = 0.78f, H = 0.92f, T = 0.09f;
                    Mk.Msh(sh, Geo.Box(W, H, T), ice, 0, 0.08f, 0);                                              // the slab
                    Mk.Msh(sh, Prism(W, 0.42f, T), ice, 0, -H / 2 + 0.08f, 0, 0, 0, Mathf.PI);                    // the kite point
                    Mk.Msh(sh, Geo.Box(W - 0.14f, H - 0.16f, 0.02f), pale, 0, 0.1f, T / 2 + 0.005f);              // the frosted face
                    // the iron frame: rim along the top and sides, two straps down the point, a cross-band
                    Mk.Msh(sh, Geo.Box(W + 0.06f, 0.07f, T + 0.04f), iron, 0, 0.08f + H / 2, 0);
                    foreach (var sx in new[] { -1f, 1f })
                    {
                        Mk.Msh(sh, Geo.Box(0.06f, H, T + 0.04f), iron, sx * (W / 2 + 0.01f), 0.08f, 0);
                        float ang = Mathf.Atan2(W / 2, 0.42f); var st = Mk.Piv(sh, sx * W / 4, -H / 2 + 0.08f - 0.21f, 0); st.SetRot(0, 0, -sx * ang);
                        Mk.Msh(st, Geo.Box(0.06f, 0.5f, T + 0.04f), iron, 0, 0, 0);
                        for (int k = 0; k < 3; k++) Mk.Msh(sh, Geo.Sphere(0.018f, 6, 4), iron, sx * (W / 2 + 0.01f), 0.08f + H / 2 - 0.15f - k * 0.3f, T / 2 + 0.03f); // rivets
                    }
                    Mk.Msh(sh, Geo.Box(W, 0.06f, 0.03f), iron, 0, -0.08f, T / 2 + 0.02f);
                    // the boss: an iron dome with a frost rune ring round it and a crystal at its heart; cracks of light in the ice
                    Mk.Msh(sh, Geo.Sphere(0.12f, 12, 6, 0, Mathf.PI * 2, 0, Mathf.PI / 2), iron, 0, 0.12f, T / 2 + 0.01f, Mathf.PI / 2, 0, 0);
                    Mk.Msh(sh, Geo.Torus(0.19f, 0.012f, 20, 4), rune, 0, 0.12f, T / 2 + 0.025f);
                    Mk.Msh(sh, Crystal(0.035f, 0.14f), pale, 0, 0.12f, T / 2 + 0.11f, Mathf.PI / 2, 0, 0);
                    foreach (var (cx, cy, cr, cl) in new[] { (-0.22f, 0.34f, 0.6f, 0.22f), (0.2f, -0.18f, -0.5f, 0.26f), (-0.12f, -0.3f, 0.2f, 0.16f) }) Mk.Msh(sh, Geo.Box(0.008f, cl, 0.005f), rune, cx, cy, T / 2 + 0.024f, 0, 0, cr);
                    // frost grown up off the top edge, leaning outward
                    for (int k = 0; k < 4; k++) { float u = (k - 1.5f) / 1.5f; Mk.Msh(sh, Crystal(0.035f + (k % 2) * 0.015f, 0.16f + (1 - Mathf.Abs(u)) * 0.12f), pale, u * W * 0.4f, 0.08f + H / 2 + 0.03f, 0, -0.15f, 0, -u * 0.45f); }
                    // the back: a grip bar where the hand closes, on two iron brackets
                    Mk.Msh(sh, Geo.Cyl(0.02f, 0.02f, 0.22f, 6), iron, -0.14f, -0.02f, -T / 2 - 0.06f, 0, 0, Mathf.PI / 2);
                    foreach (var bx in new[] { -0.25f, -0.03f }) Mk.Msh(sh, Geo.Box(0.025f, 0.05f, 0.07f), iron, bx, -0.02f, -T / 2 - 0.03f);
                    e.shield = sh.t.gameObject; e.shieldHp = 10;
                    break;
                }
                case "scav":
                {
                    Tint(e, 0x9a6a40, 0x5a4a3a, 0x2a2018, 0xffa020);
                    var metal = EM(e, Mats.Std(0x6a5a4a, 0.5f, 0.6f));
                    foreach (var sx in new[] { -1, 1 }) Mk.Head(Mk.Msh(r.head, Geo.Cyl(0.05f, 0.05f, 0.06f, 8), metal, sx * 0.075f, 0.1f, 0.2f, Mathf.PI / 2));
                    Mk.Msh(r.sh[0], Geo.Box(0.24f, 0.1f, 0.24f), metal, -0.03f, 0.04f, 0, 0, 0, 0.3f);
                    break;
                }
                case "rust": Tint(e, 0x7a4a34, 0x9a8a78, 0x2a1a14, 0xff7a20); break;
                case "bomber":
                {
                    Tint(e, 0x8a5a3a, 0x6a5040, 0x2a1a10, 0xff3010);
                    var red = EM(e, Mats.Std(0xa02010, 0.6f)); var lit = EM(e, Mats.Basic(0xff3010));
                    for (int k = 0; k < 3; k++) Mk.Msh(r.torso, Geo.Cyl(0.06f, 0.06f, 0.4f, 8), red, (k - 1) * 0.13f, 0.32f, 0.17f);
                    Mk.Msh(r.torso, Geo.Box(0.42f, 0.05f, 0.04f), r.glow, 0, 0.4f, 0.2f);
                    var blink = Mk.Msh(r.torso, Geo.Sphere(0.06f, 8, 6), lit, 0, 0.56f, 0.18f);
                    var bg = Gl(r.torso, 0xff3010, 0.8f, 0.8f); bg.transform.localPosition = V(0, 0.56f, 0.2f);
                    r.extra = (en, dt) => { float sp = en.fuse > 0 ? 30 : 8; bool on = Mathf.Sin(time * sp) > 0; blink.SetActive(on); bg.SetActive(on); };
                    e.cd = 999;
                    break;
                }
                case "tank":
                {
                    Tint(e, 0x5a4030, 0x7a6a5a, 0x221a14, 0xff8a20);
                    var steel = EM(e, Mats.Std(0x4a4440, 0.45f, 0.7f)); var hot = EM(e, Mats.Basic(0xff7a20));
                    var tank = Mk.Piv(r.torso, 0, 0.32f, -0.28f);
                    Mk.Msh(tank, Geo.Cyl(0.2f, 0.2f, 0.62f, 12), steel); Mk.Msh(tank, Geo.Cyl(0.205f, 0.205f, 0.06f, 12), hot, 0, 0.12f, 0);
                    Mk.Msh(tank, Geo.Sphere(0.2f, 12, 6), steel, 0, 0.31f, 0); Mk.Msh(tank, Geo.Sphere(0.2f, 12, 6), steel, 0, -0.31f, 0);
                    foreach (var sx in new[] { -1, 1 }) Mk.Msh(r.torso, Geo.Cyl(0.04f, 0.05f, 0.4f, 6), steel, sx * 0.18f, 0.72f, -0.25f);
                    var g = Gl(tank, 0xff6010, 0.7f, 0.5f); g.transform.localPosition = V(0, 0.12f, -0.2f);
                    break;
                }
                case "hollow":
                {
                    Tint(e, 0x8a8880, 0xe0d0a0, 0x3a3834, 0xffe0a0);
                    var cloth = EM(e, Mats.Std(0x7a7872, 0.95f, 0, 0, null, true));
                    Mk.Head(Mk.Msh(r.head, Geo.Cone(0.28f, 0.5f, 8), cloth, 0, 0.2f, -0.05f));
                    break;
                }
                case "twin":
                {
                    // MIRROR TWIN: a dark copy of you - the hunter's long coat, pauldrons, helmet and T-visor in black mirror-glass,
                    // a pale scarf, and in its hand a black-glass copy of whatever gun you're holding (it fights with that gun too)
                    Tint(e, 0x0c0c10, 0x2a2a30, 0x050506, 0xffffff);
                    foreach (var m in e.mats) { m.SetFloat("_Smoothness", 0.95f); m.SetFloat("_Metallic", 0.8f); }
                    var glassBlack = EM(e, Mats.Std(0x0a0a0e, 0.05f, 0.95f, 0x040408)); var visor = EM(e, Mats.Basic(0xe8f4ff)); var scarf = EM(e, Mats.Std(0xd8dce4, 0.6f, 0, 0x101014, null, true));
                    Mk.Head(Mk.Msh(r.head, Geo.Sphere(0.25f, 14, 10), glassBlack, 0, 0.08f, 0), Mk.Msh(r.head, Geo.Box(0.05f, 0.06f, 0.36f), glassBlack, 0, 0.3f, -0.04f),
                        Mk.Msh(r.head, Geo.Box(0.3f, 0.035f, 0.02f), visor, 0, 0.12f, 0.24f), Mk.Msh(r.head, Geo.Box(0.035f, 0.14f, 0.02f), visor, 0, 0.05f, 0.245f));
                    var vg = Gl(r.head, 0xe8f4ff, 0.5f, 0.7f); vg.transform.localPosition = V(0, 0.1f, 0.3f);
                    foreach (var i in new[] { 0, 1 }) { float sx = i == 0 ? -1 : 1; Mk.Msh(r.sh[i], Geo.Sphere(0.17f, 10, 6, 0, Mathf.PI * 2, 0, Mathf.PI / 2), glassBlack, 0, 0.04f, 0); Mk.Msh(r.sh[i], Geo.Cone(0.04f, 0.16f, 5), glassBlack, sx * 0.05f, 0.18f, -0.03f, 0, 0, -sx * 0.4f); }
                    Mk.Msh(r.torso, Geo.Box(0.44f, 0.36f, 0.08f), glassBlack, 0, 0.42f, 0.15f);
                    Mk.Msh(r.hips, Geo.OpenCone(0.36f, 1.05f, 10), glassBlack, 0, -0.45f, -0.02f);
                    for (int k = 0; k < 2; k++) Mk.Msh(r.torso, Geo.Box(0.09f, 0.55f, 0.02f), scarf, -0.06f + k * 0.1f, 0.5f, -0.2f, 0.25f, 0, (k - 0.5f) * 0.3f);
                    Mk.Msh(r.torso, Geo.Torus(0.13f, 0.05f, 12, 5), scarf, 0, 0.75f, 0, Mathf.PI / 2, 0, 0);
                    e.dashes = P != null ? Mathf.Clamp(P.weapon, 0, 4) : 0; // the gun it copied
                    if (!TwinGun(e, r.hand[1], glassBlack))
                    { var hand = r.hand[1]; Mk.Msh(hand, Geo.Box(0.06f, 0.08f, 0.32f), glassBlack, 0, -0.08f, 0.14f); Mk.Msh(hand, Geo.Cyl(0.05f, 0.05f, 0.08f, 8), glassBlack, 0, -0.06f, 0.06f, Mathf.PI / 2); }
                    r.extra = (en, dt) => { if (rand < 0.04f) SpawnParticles(BodyPoint(en) + Random.insideUnitSphere * 0.5f, 1, 0.2f, goldSpark, 0.04f, 0.3f, 0); };
                    e.shootT = 1.5f + rand;
                    break;
                }
                case "pilgrim":
                {
                    // HOLLOW PILGRIM: carries a holy lantern on a crook; its light shields every enemy round it (gold threads show
                    // who) - break the pilgrim and the lantern shatters, and every shield with it
                    Tint(e, 0x6a6660, 0xd8b860, 0x2a2826, 0xffd080);
                    var cloth = EM(e, Mats.Std(0x5a5650, 0.95f, 0, 0, null, true)); var gold = EM(e, Mats.Std(0xd8b860, 0.3f, 0.85f)); var glassM = EM(e, Mats.Trans(0xffe8b0, 0.45f)); var wood = EM(e, Mats.Std(0x3a2a1a, 0.8f));
                    Mk.Head(Mk.Msh(r.head, Geo.Cone(0.36f, 0.7f, 8), cloth, 0, 0.28f, -0.04f));
                    Mk.Msh(r.hips, Geo.OpenCone(0.5f, 1.0f, 10), cloth, 0, -0.42f, 0);
                    for (int k = 0; k < 6; k++) Mk.Msh(r.hips, Geo.Box(0.12f, 0.5f, 0.02f), cloth, Mathf.Sin(k) * 0.42f, -0.95f, Mathf.Cos(k) * 0.42f, 0, k, 0);
                    // the crook (a tall staff hooked at the top) and the lantern hanging from it on a short chain
                    var crook = Mk.Piv(r.hand[0], 0, -0.05f, 0.05f);
                    Mk.Msh(crook, Geo.Cyl(0.025f, 0.03f, 2.0f, 6), wood, 0, 0.55f, 0); Mk.Msh(crook, Geo.Torus(0.14f, 0.025f, 12, 4, Mathf.PI), wood, 0.14f, 1.55f, 0);
                    Mk.Msh(crook, Geo.Cyl(0.006f, 0.006f, 0.22f, 4), gold, 0.28f, 1.44f, 0);
                    var lantern = Mk.Piv(crook, 0.28f, 1.2f, 0);
                    Mk.Msh(lantern, Geo.Cyl(0.13f, 0.15f, 0.04f, 8), gold, 0, -0.16f, 0); Mk.Msh(lantern, Geo.Cyl(0.11f, 0.11f, 0.26f, 8), glassM, 0, -0.02f, 0);
                    for (int k = 0; k < 4; k++) { float a = k * Mathf.PI / 2 + 0.4f; Mk.Msh(lantern, Geo.Box(0.018f, 0.28f, 0.018f), gold, Mathf.Cos(a) * 0.12f, -0.02f, Mathf.Sin(a) * 0.12f); }
                    Mk.Msh(lantern, Geo.Cone(0.14f, 0.12f, 8), gold, 0, 0.17f, 0); Mk.Msh(lantern, Geo.Torus(0.04f, 0.01f, 8, 4), gold, 0, 0.26f, 0);
                    Mk.Msh(lantern, Geo.Sphere(0.05f, 8, 6), EM(e, Mats.Basic(0xfff0c0)), 0, -0.03f, 0);
                    var lg = Gl(lantern, 0xffd080, 1.6f, 0.95f); lg.transform.localPosition = Vector3.zero;
                    e.lantern = lantern.t; e.links = new List<GameObject>();
                    var linkM = EM(e, Mats.Add(0xffd080, 0.5f, null, true, true));
                    bool oc = Mk.colliders; int ol = Mk.layer; Mk.colliders = false; Mk.layer = 0; // (the threads are scenery: no hitbox)
                    for (int k = 0; k < 6; k++) { var lk = Mk.Msh(levelRoot, Geo.Cyl(1, 1, 1, 5), linkM); lk.layer = Mk.LayerIgnore; lk.SetActive(false); e.links.Add(lk); }
                    Mk.colliders = oc; Mk.layer = ol;
                    // the protected ring: a faint gold circle on the ground, the radius it shields
                    var ringM = EM(e, Mats.Add(0xffd080, 0.35f, null, true, true));
                    var ring = Mk.Msh(r.root, Geo.Torus(PILGRIM_R / e.s, 0.04f / e.s, 48, 4), ringM, 0, 0.06f / e.s, 0, Mathf.PI / 2); ring.layer = Mk.LayerIgnore;
                    r.extra = (en, dt) => { Mats.SetAlpha(ringM, 0.2f + 0.15f * Mathf.Sin(time * 3)); Mats.SetAlpha(linkM, 0.3f + 0.25f * Mathf.Sin(time * 9)); PilgrimLinks(en); };
                    break;
                }
            }
        }
        const float PILGRIM_R = 8, BELL_R = 16;
        bool BellWarded(Ent e, float dmg, Hit info) => e.kind == "bell" && dmg < 5000 && !info.cheat && !info.exec && !(HK.state == "pull" && HK.enemy == e) && Vector3.Distance(e.pos, EyePos) > 11;
        float bellWardT; bool bellHinted;
        // is o shielded by a pilgrim other than "except"?
        bool PilgrimShielded(Ent o, Ent except)
        {
            foreach (var p in enemies) if (!p.dead && p != except && p.kind == "pilgrim" && p != o && Hypot(p.pos.x - o.pos.x, p.pos.z - o.pos.z) < PILGRIM_R) return true;
            return false;
        }
        // the gold threads from a pilgrim's lantern to everything it's shielding (the nearest six)
        void PilgrimLinks(Ent e)
        {
            if (e.links == null || !e.lantern) return;
            if (e.dead || e.spawnT > 0) { foreach (var l in e.links) if (l) l.SetActive(false); if (e.bubbles != null) foreach (var b in e.bubbles) if (b) b.SetActive(false); }
            var lp = e.lantern.position; int n = 0;
            if (!e.dead && e.spawnT <= 0)
                foreach (var o in enemies)
                {
                    if (n >= e.links.Count) break;
                    if (o.dead || o == e || o.kind == "pilgrim" || o.type == "boss" || o.spawnT > 0 || Hypot(o.pos.x - e.pos.x, o.pos.z - e.pos.z) >= PILGRIM_R) continue;
                    var l = e.links[n++]; var d = BodyPoint(o) - lp; float L = d.magnitude; if (L < 0.1f) { l.SetActive(false); continue; }
                    l.SetActive(true); l.transform.localPosition = lp + d / 2; l.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d / L); l.transform.localScale = V(0.035f, L, 0.035f);
                    if (rand < 0.05f) SpawnParticles(BodyPoint(o), 1, 0.5f, goldSpark, 0.05f, 0.3f, 0);
                    if (e.bubbles != null && n - 1 < e.bubbles.Count && e.bubbles[n - 1]) { var bb = e.bubbles[n - 1]; bb.SetActive(true); bb.transform.localPosition = BodyPoint(o); bb.transform.localScale = Vector3.one * (o.hitR * 1.25f + 0.3f); bb.transform.localRotation = Quaternion.Euler(time * 40, time * 60 + n * 30, 0); } // the shield it lends, a shell of gold light
                }
            for (int i = n; i < e.links.Count; i++) if (e.links[i]) e.links[i].SetActive(false);
            if (e.bubbles != null) for (int i = n; i < e.bubbles.Count; i++) if (e.bubbles[i]) e.bubbles[i].SetActive(false);
        }
        // the Mirror Twin's gun: a black-glass copy of the player's own viewmodel gun (the hands and the muzzle flash left off)
        bool TwinGun(Ent e, N hand, Material glassM)
        {
            int w = e.dashes; if (w < 0 || w >= guns.Count || guns[w].g == null) return false;
            var src = guns[w];
            bool wasActive = src.g.gameObject.activeSelf;
            var copy = Instantiate(src.g.gameObject, hand.t, false); copy.SetActive(true); copy.name = "twinGun";
            foreach (var c in copy.GetComponentsInChildren<Collider>(true)) Destroy(c);
            foreach (var l in copy.GetComponentsInChildren<Light>(true)) Destroy(l);
            foreach (var mb in copy.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(mb);
            foreach (var rr in copy.GetComponentsInChildren<Renderer>(true))
            {
                var sm = rr.sharedMaterial;
                if (sm == gloveMat || sm == sleeveMat || (src.flashMat && sm == src.flashMat) || rr.gameObject.name.Contains("glow")) { rr.enabled = false; continue; }
                var mats = rr.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = glassM; rr.sharedMaterials = mats;
                rr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            copy.transform.localPosition = V(0, -0.1f, 0.12f); copy.transform.localRotation = Quaternion.Euler(0, 180, 0); copy.transform.localScale = Vector3.one;
            // size it to a hand gun in the twin's grip
            var b0 = new Bounds(); bool any = false;
            foreach (var rr in copy.GetComponentsInChildren<MeshRenderer>()) { if (!rr.enabled) continue; if (!any) { b0 = rr.bounds; any = true; } else b0.Encapsulate(rr.bounds); }
            if (!any) { Destroy(copy); return false; }
            float big = Mathf.Max(b0.size.x, Mathf.Max(b0.size.y, b0.size.z)), want = (w == 2 ? 1.0f : w == 3 ? 1.2f : w == 1 ? 0.85f : 0.55f) * e.s;
            if (big > 1e-3f) copy.transform.localScale = Vector3.one * (want / big) * (copy.transform.localScale.x);
            return true;
        }
        void DressDrone(Ent e, string kind)
        {
            var r = e.dr; e.kind = kind;
            void Col(int metal, int plate) { if (e.mats.Count > 0) Mats.SetColor(e.mats[0], metal); if (e.mats.Count > 1) Mats.SetColor(e.mats[1], plate); }
            switch (kind)
            {
                case "spitter":
                {
                    Col(0x405870, 0xc8e8ff); e.hp = 2.5f;
                    // the same eye, but the cold has grown over it: crystals out of the shell, icicles off the tentacles, a frost rim on the lid
                    var ice = EM(e, Mats.Std(0xc8ecff, 0.08f, 0.2f, 0x0c3a5a)); var crust = EM(e, Mats.Trans(0xb8e4ff, 0.55f));
                    for (int k = 0; k < 7; k++) { float a = k * 0.9f, el = 0.35f + (k % 3) * 0.35f; Mk.Msh(r.lidTop, Geo.Cyl(0.012f, 0.07f + (k % 2) * 0.03f, 0.22f + (k % 3) * 0.1f, 6), ice, Mathf.Cos(a) * Mathf.Sin(el) * 0.5f, Mathf.Cos(el) * 0.5f, Mathf.Sin(a) * Mathf.Sin(el) * 0.5f - 0.1f, -Mathf.Sin(a) * el, 0, Mathf.Cos(a) * el); }
                    Mk.Msh(r.lidTop, Geo.Icosahedron(1), crust, 0.2f, 0.42f, -0.15f).transform.localScale = V(0.18f, 0.12f, 0.16f);
                    Mk.Msh(r.root, Geo.Torus(0.3f, 0.035f, 20, 5), ice, 0, 0, 0.42f);
                    foreach (var seg in r.tentacles) { var tip = seg[seg.Count - 1]; Mk.Msh(tip, Geo.Cone(0.035f, 0.26f, 5), ice, 0, -0.18f, 0, Mathf.PI, 0, 0); Mk.Msh(seg[1], Geo.Cyl(0.01f, 0.05f, 0.14f, 6), ice, 0.05f, -0.1f, 0, 0, 0, 1.2f); }
                    break;
                }
                case "rustdrone": Col(0x5a4a3e, 0x9a8a78); if (Act == "ruin") DressScrapDrone(e); break;
                case "greydrone":
                {
                    // CENSER WATCHER: a grey-gold eye under a hood of tattered grey cloth, a censer swinging beneath it on three chains
                    Col(0x6a6862, 0xc8b070); foreach (var seg in r.tentacles) seg[0].go.SetActive(false);
                    var cloth = EM(e, Mats.Std(0x6a6862, 0.95f, 0, 0x020202, ClothTex2(), true)); var gold = EM(e, Mats.Std(0xc8a050, 0.3f, 0.85f)); var iron = EM(e, Mats.Std(0x3a3634, 0.5f, 0.7f));
                    Mk.Msh(r.lidTop, Geo.Sphere(0.66f, 14, 8, 0, Mathf.PI * 2, 0, Mathf.PI * 0.42f), cloth, 0, 0.04f, -0.05f);
                    for (int k = 0; k < 10; k++) { float a = k * Mathf.PI / 5; Mk.Msh(r.root, Geo.Box(0.16f, 0.5f + (k % 3) * 0.2f, 0.02f), cloth, Mathf.Sin(a) * 0.6f, -0.35f - (k % 3) * 0.1f, Mathf.Cos(a) * 0.6f, 0, a, 0); }
                    var cens = Mk.Piv(r.root, 0, -0.6f, 0);
                    for (int k = 0; k < 3; k++) { float a = k * 2.09f; Mk.Msh(cens, Geo.Cyl(0.006f, 0.006f, 0.6f, 4), iron, Mathf.Sin(a) * 0.08f, -0.3f, Mathf.Cos(a) * 0.08f); }
                    Mk.Msh(cens, Geo.Sphere(0.16f, 10, 8), gold, 0, -0.66f, 0); Mk.Msh(cens, Geo.Cone(0.1f, 0.14f, 8), gold, 0, -0.5f, 0); Mk.Msh(cens, Geo.Torus(0.16f, 0.02f, 14, 4), gold, 0, -0.66f, 0, Mathf.PI / 2, 0, 0);
                    var cg = Gl(cens, 0xffb060, 0.6f, 0.7f); cg.transform.localPosition = V(0, -0.66f, 0);
                    r.extra = (en, dt) => { cens.rx = Mathf.Sin(time * 2.1f + en.phase) * 0.4f; cens.rz = Mathf.Cos(time * 1.6f + en.phase) * 0.25f; if (rand < 0.3f) SpawnParticles(cens.t.TransformPoint(V(0, -0.66f, 0)), 1, 0.3f, ashMat, 0.1f, 1.4f, -1); };
                    break;
                }
                case "sniper":
                {
                    Col(0x3a3430, 0x8a7a6a); e.hp = 3;
                    var steel = EM(e, Mats.Std(0x2a2a2e, 0.4f, 0.7f));
                    Mk.Msh(r.root, Geo.Cyl(0.06f, 0.06f, 1.2f, 8), steel, 0, -0.32f, 0.5f, Mathf.PI / 2);
                    var lm = EM(e, Mats.Add(0xff2020, 0, null, true, true));
                    var laser = Mk.Msh(e.g.transform, Geo.Cyl(1, 1, 1, 6), lm, 0, 0, 0, Mathf.PI / 2); laser.layer = Mk.LayerIgnore;
                    e.laser = laser.transform; e.laserM = lm; e.shootT = 2.5f + rand * 1.5f;
                    break;
                }
                case "clock":
                {
                    Col(0x5a5650, 0xd8b860); e.hp = e.maxHp = 3.5f;
                    var face = EM(e, Mats.Basic(0xf0e8d8)); var hand = EM(e, Mats.Basic(0x1a1612));
                    var dial = Mk.Piv(r.ring2); Mk.Msh(dial, Geo.Circle(0.42f, 24), face, 0, 0, -0.62f, 0, Mathf.PI, 0);
                    var h1 = Mk.Piv(dial, 0, 0, -0.64f); Mk.Msh(h1, Geo.Box(0.03f, 0.3f, 0.01f), hand, 0, 0.13f, 0);
                    var h2 = Mk.Piv(dial, 0, 0, -0.645f); Mk.Msh(h2, Geo.Box(0.04f, 0.2f, 0.01f), hand, 0, 0.09f, 0);
                    e.dr.extraHands = new[] { h1, h2 };
                    // CLOCKWORK EYE: brass gears turning at its sides, a numeral ring, a pendulum swinging under it (no tentacles)
                    foreach (var seg in r.tentacles) seg[0].go.SetActive(false);
                    var brass = EM(e, Mats.Std(0x9a7a40, 0.35f, 0.85f)); var numM = EM(e, Mats.Basic(0xffd890));
                    var gears = new List<Transform>();
                    foreach (var sx in new[] { -1f, 1f }) { var gp = Mk.Piv(r.root, sx * 0.62f, 0, 0); gp.SetRot(0, Mathf.PI / 2, 0); GearDeco(gp.t, 0.34f, 10, brass, out var gh); gears.Add(gh); }
                    for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; Mk.Msh(r.ring1, Geo.Box(0.05f, 0.03f, 0.14f), numM, Mathf.Cos(a) * 0.85f, 0.04f, Mathf.Sin(a) * 0.85f, 0, -a, 0); }
                    var pend = Mk.Piv(r.root, 0, -0.45f, 0); Mk.Msh(pend, Geo.Cyl(0.015f, 0.015f, 0.7f, 5), brass, 0, -0.35f, 0); Mk.Msh(pend, Geo.Cyl(0.16f, 0.16f, 0.04f, 16), brass, 0, -0.75f, 0, Mathf.PI / 2, 0, 0); Mk.Msh(pend, Geo.Circle(0.13f, 16), face, 0, -0.75f, 0.025f);
                    r.extra = (en, dt) => { foreach (var gt in gears) if (gt) gt.Rotate(0, 0, dt * 90, Space.Self); pend.rz = Mathf.Sin(time * 3 + en.phase) * 0.5f; };
                    break;
                }
                case "bell":
                {
                    // BELL WRAITH: a spectre high over the fight - a deep hood with one burning eye and a jaw of bone under it, a
                    // shroud of long tatters streaming below it into a spectral tail, bony arms holding a great bell by its chains.
                    // Each toll hastes every enemy below. It's warded - shots from the ground don't touch it: grapple up to it.
                    Col(0x6a6660, 0xd8b860); e.hp = 6; e.heavy = true; e.hitR = 1.4f;
                    r.lidTop.go.SetActive(false); r.lidBot.go.SetActive(false); r.ring1.go.SetActive(false); r.ring2.go.SetActive(false); r.jet.SetActive(false);
                    foreach (var seg in r.tentacles) seg[0].go.SetActive(false);
                    var gold = EM(e, Mats.Std(0xd8b860, 0.28f, 0.9f, 0x100800)); var shroud = EM(e, Mats.Std(0x4a4a58, 0.95f, 0, 0x04060a, ClothTex2(), true)); var bone = EM(e, Mats.Std(0xd8d0bc, 0.6f));
                    var spect = EM(e, Mats.Add(0x9affe0, 0.18f, null, true, true)); var tail = EM(e, Mats.Add(0x9affe0, 0.16f, Tex.RayFade, true, true)); var iron = EM(e, Mats.Std(0x2a2826, 0.5f, 0.7f));
                    Mk.Msh(r.root, Geo.Sphere(0.82f, 16, 12, Mathf.PI / 2 + 0.75f, Mathf.PI * 2 - 1.5f, 0, Mathf.PI * 0.72f), shroud, 0, 0.12f, -0.12f).transform.localScale = V(1, 1.12f, 1.1f);
                    Mk.Msh(r.root, Geo.Cone(0.45f, 1.1f, 10), shroud, 0, 1.1f, -0.45f, -0.6f, 0, 0);
                    Mk.Msh(r.root, Geo.Box(0.6f, 0.1f, 0.2f), bone, 0, 0.34f, 0.38f, 0.3f, 0, 0); // the brow ridge over the eye
                    var jaw = Mk.Piv(r.root, 0, -0.42f, 0.25f); Mk.Msh(jaw, Geo.Box(0.42f, 0.1f, 0.3f), bone); for (int k = 0; k < 6; k++) Mk.Msh(jaw, Geo.Cone(0.025f, 0.08f, 4), bone, (k - 2.5f) * 0.06f, 0.08f, 0.13f);
                    bool oc = Mk.colliders; Mk.colliders = false;
                    // the shroud: two rings of long tatters, each on its own hinge so the wind can take it
                    var tatters = new List<(N n, float ph)>();
                    for (int k = 0; k < 16; k++)
                    {
                        float a = k * Mathf.PI / 8 + (k % 2) * 0.2f, rr = k % 2 == 0 ? 0.75f : 0.55f; var tp = Mk.Piv(r.root, Mathf.Sin(a) * rr, -0.3f, Mathf.Cos(a) * rr - 0.05f); tp.SetRot(0, a, 0);
                        float L = 1.6f + (k % 3) * 0.5f; Mk.Msh(tp, Geo.Box(0.24f, L, 0.025f), shroud, 0, -L / 2, 0); Mk.Msh(tp, Geo.Box(0.27f, L * 0.3f, 0.03f), spect, 0, -L * 0.9f, 0);
                        tatters.Add((tp, k * 0.8f));
                    }
                    Mk.Msh(r.root, Geo.Cyl(0.7f, 0.05f, 3.2f, 14, true), tail, 0, -2.2f, 0);
                    var aura = Mk.Msh(r.root, Geo.Torus(2.4f, 0.06f, 40, 4), EM(e, Mats.Add(0xffd060, 0.3f, null, true, true)), 0, -1.8f, 0, Mathf.PI / 2, 0, 0); aura.layer = Mk.LayerIgnore; var auraM = aura.GetComponent<Renderer>().sharedMaterial;
                    var ag = Gl(r.root, 0xffd060, 4.5f, 0.25f); ag.transform.localPosition = V(0, -1.8f, 0.3f); var agM = ag.GetComponent<Renderer>().sharedMaterial;
                    var sg = Gl(r.root, 0x9affe0, 3.2f, 0.12f); sg.transform.localPosition = V(0, -0.4f, 0); var sgM = sg.GetComponent<Renderer>().sharedMaterial;
                    Mk.colliders = oc;
                    // arms of bone reaching down to the bell's chains
                    foreach (var sx in new[] { -1f, 1f })
                    {
                        var arm = Mk.Piv(r.root, sx * 0.55f, -0.25f, 0.3f); arm.SetRot(0.35f, 0, -sx * 0.25f);
                        Mk.Msh(arm, Geo.Cyl(0.05f, 0.06f, 0.9f, 6), bone, 0, -0.45f, 0); Mk.Msh(arm, Geo.Sphere(0.07f, 6, 5), bone, 0, -0.92f, 0);
                        Mk.Msh(arm, Geo.Cyl(0.04f, 0.05f, 0.8f, 6), bone, -sx * 0.14f, -1.3f, 0.1f, 0, 0, sx * 0.35f);
                        for (int k = 0; k < 4; k++) Mk.Msh(arm, Geo.Cone(0.018f, 0.14f, 4), bone, -sx * 0.28f + (k - 1.5f) * 0.03f, -1.72f, 0.12f, Mathf.PI, 0, 0);
                        for (int l = 0; l < 5; l++) Mk.Msh(r.root, Geo.Torus(0.05f, 0.015f, 8, 4), iron, sx * 0.25f, -2.0f - l * 0.11f, 0.6f, 0, l % 2 == 0 ? 0 : Mathf.PI / 2, 0);
                    }
                    // the bell: a cast bronze profile, bands, burning runes, a crown loop and a swinging clapper
                    var bell = Mk.Piv(r.root, 0, -2.6f, 0.6f);
                    Mk.Msh(bell, LATHE("wbell", new[] { 0.55f, 0.0f, 0.5f, 0.22f, 0.3f, 0.3f, 0.0f, 0.34f, -0.3f, 0.44f, -0.5f, 0.62f, -0.6f, 0.78f, -0.66f, 0.8f }, 24), gold);
                    foreach (var y in new[] { 0.2f, -0.28f, -0.56f }) Mk.Msh(bell, Geo.Torus(y > 0 ? 0.31f : y > -0.4f ? 0.45f : 0.71f, 0.03f, 24, 4), gold, 0, y, 0, Mathf.PI / 2, 0, 0);
                    var rune = EM(e, Mats.Basic(0xffd890));
                    for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; Mk.Msh(bell, Geo.Box(0.06f, 0.14f, 0.02f), rune, Mathf.Sin(a) * 0.42f, -0.14f, Mathf.Cos(a) * 0.42f, 0, a, 0.3f); }
                    Mk.Msh(bell, Geo.Torus(0.12f, 0.035f, 12, 4), gold, 0, 0.62f, 0);
                    var clap = Mk.Piv(bell, 0, -0.1f, 0); Mk.Msh(clap, Geo.Cyl(0.03f, 0.03f, 0.5f, 6), iron, 0, -0.25f, 0); Mk.Msh(clap, Geo.Sphere(0.1f, 8, 6), iron, 0, -0.52f, 0);
                    r.extra = (en, dt) =>
                    {
                        float w = en.ringT < 1.2f ? 1 - en.ringT / 1.2f : 0;
                        Mats.SetAlpha(auraM, 0.25f + 0.6f * w); Mats.SetAlpha(agM, 0.2f + 0.5f * w); aura.transform.localScale = Vector3.one * (1 + 0.3f * w + 0.05f * Mathf.Sin(time * 4));
                        Mats.SetAlpha(spect, 0.12f + 0.1f * Mathf.Sin(time * 2.2f)); Mats.SetAlpha(sgM, 0.08f + 0.06f * Mathf.Sin(time * 1.7f));
                        var lv = en.g.transform.InverseTransformDirection(en.vel);
                        foreach (var tt in tatters) tt.n.SetRot(Mathf.Sin(time * 2.4f + tt.ph) * 0.18f - lv.z * 0.04f, tt.n.ry, Mathf.Cos(time * 1.9f + tt.ph) * 0.14f + lv.x * 0.04f);
                        clap.rz = -bell.rz * 1.6f; jaw.rx = w > 0.5f ? 0.4f : Mathf.Sin(time * 3) * 0.05f;
                        r.root.SetRot(Mathf.Clamp(lv.z * 0.03f, -0.25f, 0.25f), 0, Mathf.Clamp(-lv.x * 0.03f, -0.25f, 0.25f)); // leaning into its drift
                        if (rand < 0.3f) SpawnParticles(en.g.transform.TransformPoint(V((rand - 0.5f) * 1.2f, -1.8f - rand * 1.5f, (rand - 0.5f) * 1.2f)), 1, 0.3f, cyanMat, 0.08f, 1.0f, 0.5f);
                    };
                    e.dr.bell = bell; e.shootT = 3 + rand * 2; e.ringT = 2.5f + rand;
                    break;
                }
            }
        }
        Color KindIris(Ent e) => e.kind switch
        {
            "spitter" => new Color(0.4f, 0.9f, 1f), "sniper" => new Color(1f, 0.1f, 0.1f), "clock" => new Color(1f, 0.85f, 0.4f),
            "bell" => new Color(1f, 0.8f, 0.3f), "greydrone" => new Color(1f, 0.9f, 0.6f), _ => new Color(1f, 0.53f, 0f)
        };

        // ---------- husk behaviour (runs every frame, before the shared AI); true = the enemy is gone ----------
        bool KindPre(Ent e, float dt, float dx, float dz, float dist, float dy)
        {
            if (e.timeSlowT > 0) { e.timeSlowT -= dt; e.attackT += dt * 0.65f; e.cd += dt * 0.65f; }
            if (e.hasteT > 0) { e.hasteT -= dt; if (rand < 0.3f) SpawnParticles(BodyPoint(e), 1, 1.5f, goldSpark, 0.06f, 0.4f, -2); }
            switch (e.kind)
            {
                case "wraith":
                    e.tpT -= dt;
                    if (Auth && e.tpT <= 0 && dist > 7 && !e.leaping && e.attackT <= 0 && e.stun <= 0 && !Down(Tgt)) // co-op: the host decides; clients get the jump in the snapshot
                    {
                        var fwd = V(-Mathf.Sin(Tgt.yaw), 0, -Mathf.Cos(Tgt.yaw)); // three.js view: the camera looks down -z
                        var to = Tgt.pos - fwd * 3.2f; float lim = LB - 3;
                        to.x = clamp(to.x, -lim, lim); to.z = clamp(to.z, -lim, lim);
                        if (!PointInLevel(to + V(0, 1, 0)) && !InPool(to.x, to.z, 1))
                        {
                            SpawnParticles(BodyPoint(e), 16, 4, cyanMat, 0.12f, 0.6f, -1); Blast(BodyPoint(e), 1.5f, 0xa0f0ff);
                            e.pos = V(to.x, Tgt.pos.y, to.z); e.vel = Vector3.zero; SyncEnt(e);
                            SpawnParticles(BodyPoint(e), 16, 4, cyanMat, 0.12f, 0.6f, -1); Blast(BodyPoint(e), 1.5f, 0xa0f0ff);
                            e.attackT = e.windup * 1.4f; e.cd = 0; Sfx("growl");
                        }
                        e.tpT = 4.5f + rand * 3;
                    }
                    break;
                case "bomber":
                    e.cd = 999; // it never swings: it explodes
                    if (e.fuse > 0)
                    {
                        e.fuse -= dt; float k = Mathf.Exp(-8 * dt); e.vel.x *= k; e.vel.z *= k;
                        if (rand < 0.6f) SpawnParticles(BodyPoint(e) + V(0, 0.4f, 0), 2, 4, emberMat, 0.08f, 0.4f);
                        if (e.fuse <= 0) { BomberBlast(e, 30); return true; }
                    }
                    else if (dist < 3.2f && Mathf.Abs(dy) < 2.5f && e.stun <= 0) { e.fuse = 0.7f; Sfx("charge"); }
                    break;
                case "twin":
                    e.shootT -= dt;
                    if (e.shootT <= 0 && !Down(Tgt) && dist > 3)
                    {
                        var from = e.pos + V(0, e.headY * 0.72f, 0) + V(Mathf.Sin(e.yaw), 0, Mathf.Cos(e.yaw)) * 0.6f;
                        var at = ChestOf(Tgt); var dir = (at - from).normalized;
                        if (!Los(from, at)) { e.shootT = 0.4f; break; }
                        // it fights with the gun it copied from you
                        switch (e.dashes)
                        {
                            case 1: // shotgun: a spread at close range
                                if (dist > 20) { e.shootT = 0.5f; break; }
                                for (int k = 0; k < 6; k++) MakeProj("slug", from, (dir + Random.insideUnitSphere * 0.09f).normalized * 40, 4, 1.2f);
                                Sfx("shotgun", 0.7f); Blast(from, 0.9f, 0xffffff); e.shootT = 1.9f + rand * 0.6f; break;
                            case 2: // minigun: a burst
                                e.vidx++; MakeProj("slug", from, (dir + Random.insideUnitSphere * 0.04f).normalized * 50, 3, 3); Sfx("minigun", 0.6f); Blast(from, 0.4f, 0xffffff);
                                if (e.vidx >= 7) { e.vidx = 0; e.shootT = 2.2f + rand * 0.8f; } else e.shootT = 0.09f; break;
                            case 3: // sniper: a warning glint, then a fast heavy shot
                                if (e.vidx == 0) { e.vidx = 1; e.shootT = 0.9f; Sfx("charge"); Blast(from, 1.2f, 0xe8f4ff); break; }
                                e.vidx = 0; MakeProj("slug", from, dir * 90, 18, 3); Sfx("sniper", 0.6f); Blast(from, 1.0f, 0xffffff); e.shootT = 3.4f + rand; break;
                            case 4: // BANG: slow, heavy
                                MakeProj("slug", from, dir * 60, 16, 3); Sfx("revolver"); Sfx("boomSmall", 0.5f); Blast(from, 1.2f, 0xffffff); e.shootT = 2.8f + rand; break;
                            default: // revolver
                                MakeProj("slug", from, dir * 46, 9, 4); Sfx("revolver"); Blast(from, 0.6f, 0xffffff); e.shootT = 1.3f + rand * 0.9f; break;
                        }
                    }
                    break;
                case "pilgrim":
                    break;
            }
            return false;
        }
        // steering on top of the shared chase: the twin keeps its range, hasted enemies run, time bubbles crawl
        void KindSteer(Ent e, float dist, ref float mx, ref float mz, ref float sp)
        {
            if (e.kind == "twin")
            {
                float px = -mz, pz = mx;
                if (dist < 9) { mx = -mx; mz = -mz; }
                else if (dist < 15) { float s = Mathf.Sin(time * 0.7f + e.phase) > 0 ? 1 : -1; mx = px * s; mz = pz * s; sp *= 0.8f; }
            }
            if (e.kind == "scav") sp *= 1.25f;
            if (e.hasteT > 0) sp *= 1.45f;
            if (e.timeSlowT > 0) sp *= 0.3f;
        }

        // ---------- drone behaviour; true = it handled its own shooting this frame ----------
        float DroneOrbit(Ent e) => e.kind == "sniper" ? 30 : e.kind == "bell" ? 11 : 15;
        bool DroneKind(Ent e, float dt, Vector3 head)
        {
            if (e.timeSlowT > 0) { e.timeSlowT -= dt; e.vel *= Mathf.Exp(-5 * dt); e.shootT += dt * 0.65f; }
            if (e.dr.extraHands != null) { e.dr.extraHands[0].rz -= dt * 6; e.dr.extraHands[1].rz -= dt * 0.5f; }
            if (e.kind == "clock" && e.regenT > 0)
            {
                e.regenT -= dt;
                if (e.regenT <= 0 && e.hp > 0 && e.hp < e.maxHp)
                {
                    e.hp = e.maxHp; // it rewinds
                    ShockRing(e.pos, 2.5f, 0xffd080, 0.4f); SpawnParticles(e.pos, 18, 5, goldSpark, 0.08f, 0.6f, 0); Sfx("holy");
                }
            }
            switch (e.kind)
            {
                case "bell":
                    e.ringT -= dt;
                    if (e.dr.bell != null) e.dr.bell.rz = Mathf.Sin(time * 3) * 0.15f + (e.ringT < 0.6f ? Mathf.Sin(time * 30) * 0.4f : 0);
                    if (e.ringT < 1.2f && rand < 0.5f) SpawnParticles(e.pos + Random.insideUnitSphere * 2.5f, 1, 0.2f, goldSpark, 0.07f, 0.5f, 0);
                    if (e.ringT <= 0)
                    {
                        // the toll: rings rolling out from the bell, a column of light down to the ground, a ring sweeping out over the
                        // floor to the edge of its reach, and a thread of gold to every enemy it quickens
                        e.ringT = 6 + rand * 2;
                        var ground = V(e.pos.x, Mathf.Max(0.2f, FloorBelow(e.pos) + 0.2f), e.pos.z);
                        ShockRing(e.pos, 9, 0xffd060, 0.6f); ShockRing(e.pos, 5, 0xffffff, 0.4f); ShockRing(ground, BELL_R, 0xffd060, 0.9f); ShockRing(ground, BELL_R * 0.5f, 0xffffff, 0.6f);
                        FirePillar(ground, 0.6f, e.pos.y - ground.y, 0xffd060, 0.6f);
                        Sfx("toll", 0.8f); Sfx("chant");
                        foreach (var o in enemies)
                            if (!o.dead && o != e && o.type != "boss" && Hypot(o.pos.x - e.pos.x, o.pos.z - e.pos.z) < BELL_R)
                            {
                                o.hasteT = 4.5f; var bp = BodyPoint(o);
                                for (int k = 0; k < 8; k++) SpawnParticles(Vector3.Lerp(e.pos, bp, k / 7f), 1, 0.3f, goldSpark, 0.08f, 0.5f, 0);
                                ShockRing(V(o.pos.x, o.pos.y + 0.1f, o.pos.z), 1.4f * o.s, 0xffd060, 0.5f);
                            }
                    }
                    return false;
                case "spitter":
                    // clings to its wall spot (it never flies), turning to watch you; a frost crust marks where it holds on
                    if (!e.anchored) SpitterAnchor(e);
                    e.vel = Vector3.zero; e.pos = e.anchor; e.g.transform.localPosition = e.pos;
                    e.shootT -= dt;
                    if (!e.charging && e.shootT < 0.6f) { e.charging = true; Sfx("charge"); }
                    if (e.shootT <= 0)
                    {
                        e.shootT = 2.4f + rand * 1.4f; e.charging = false;
                        var from = e.pos + (head - e.pos).normalized * 0.7f;
                        if (!Down(Tgt) && Los(from, head))
                            for (int k = -1; k <= 1; k += 2)
                            {
                                var aim = head + V(rand - 0.5f, 1.2f, rand - 0.5f) * 1.2f + V(k * 1.2f, 0, 0);
                                var v = (aim - from).normalized * 22;
                                MakeProj("ice", from, v, 10, 5, 6); Sfx("eshot");
                            }
                    }
                    return true;
                case "sniper":
                {
                    e.shootT -= dt; bool aiming = e.shootT < 1.3f && !Down(Tgt); var tch = ChestOf(Tgt);
                    var from = e.pos + (head - e.pos).normalized * 0.7f; float L = Vector3.Distance(from, tch);
                    if (e.laser)
                    {
                        e.laser.gameObject.SetActive(aiming);
                        if (aiming)
                        {
                            float k = 1 - e.shootT / 1.3f;
                            e.laser.localPosition = V(0, 0, L / 2); e.laser.localScale = V(0.02f + k * 0.03f, L, 0.02f + k * 0.03f);
                            Mats.SetAlpha(e.laserM, 0.25f + 0.6f * k * (Mathf.Sin(time * 40) > 0 || k < 0.7f ? 1 : 0.4f));
                        }
                    }
                    if (aiming && !e.charging) { e.charging = true; Sfx("charge"); }
                    if (e.shootT <= 0)
                    {
                        e.shootT = 3.2f + rand * 1.5f; e.charging = false;
                        if (!Down(Tgt) && Los(from, tch)) { MakeProj("slug", from, (tch - from).normalized * 80, 22, 3); Sfx("revolver"); Blast(from, 1, 0xff3020); }
                    }
                    return true;
                }
            }
            return false;
        }

        // the Rime Spitter latches onto the nearest arena wall (or a tall solid it can see you from), 3-7 m up
        void SpitterAnchor(Ent e)
        {
            e.anchored = true;
            float w = LB - 0.9f; var p = e.pos; Vector3 best = p, bestN = Vector3.up; float bd = 1e9f;
            foreach (var (n, d) in new[] { (V(1, 0, 0), w + p.x), (V(-1, 0, 0), w - p.x), (V(0, 0, 1), w + p.z), (V(0, 0, -1), w - p.z) })
                if (d < bd) { bd = d; bestN = n; }
            best = V(bestN.x != 0 ? -bestN.x * w : clamp(p.x, -w + 4, w - 4), 3.5f + rand * 3.5f, bestN.z != 0 ? -bestN.z * w : clamp(p.z, -w + 4, w - 4));
            e.anchor = best; e.anchorN = bestN; e.pos = best;
            // the frost crust it clings to (scenery, not part of its body: it stays after the spitter dies)
            var crust = Mk.Msh(levelRoot, Geo.Circle(1.3f, 16), Mats.Std(0xd8f0ff, 0.2f, 0.1f, 0x0a2a40, null, true), best.x - bestN.x * 0.55f, best.y, best.z - bestN.z * 0.55f, 0, Mathf.Atan2(bestN.x, bestN.z), 0);
            crust.layer = Mk.LayerIgnore;
            for (int k = 0; k < 5; k++) { float a = k * 1.256f; var o = Quaternion.Euler(0, Mathf.Atan2(bestN.x, bestN.z) * Mathf.Rad2Deg, 0) * V(Mathf.Cos(a) * 1.1f, Mathf.Sin(a) * 1.1f, 0); Mk.Msh(levelRoot, Geo.Cone(0.12f, 0.7f, 5), Mats.Std(0xc8ecff, 0.15f), best.x + o.x - bestN.x * 0.4f, best.y + o.y, best.z + o.z - bestN.z * 0.4f, Mathf.Atan2(o.y, Mathf.Sqrt(o.x * o.x + o.z * o.z)) + Mathf.PI / 2, Mathf.Atan2(o.x, o.z), 0).layer = Mk.LayerIgnore; }
            SpawnParticles(best, 14, 4, cyanMat, 0.1f, 0.6f);
        }

        // ---------- damage and death ----------
        // returns the damage that gets through
        float KindDamage(Ent e, float dmg, Hit info)
        {
            if (e.kind == "glacier" && e.shieldHp > 0 && e.shield)
            {
                bool front = true;
                if (info.dir.HasValue) { var f = V(Mathf.Sin(e.yaw), 0, Mathf.Cos(e.yaw)); front = Vector3.Dot(-info.dir.Value, f) > 0.25f; }
                if (info.slam || info.exec || info.cheat) { e.shieldHp = 0; }
                else if (front) { e.shieldHp -= dmg * (info.close ? 3 : 1); dmg *= 0.12f; SpawnParticles(info.point ?? BodyPoint(e), 3, 5, cyanMat, 0.07f, 0.3f); }
                if (e.shieldHp <= 0)
                {
                    var p = e.shield.transform.position; e.shield.SetActive(false); e.shield = null;
                    SpawnParticles(p, 30, 9, cyanMat, 0.16f, 0.9f); Blast(p, 2, 0xa0e8ff); Sfx("slam"); AddStyle("SHIELD BREAK", 90);
                }
            }
            // a lantern-bearing pilgrim shields everything around it (not itself)
            if (e.kind != "pilgrim")
                foreach (var o in enemies)
                    if (!o.dead && o.kind == "pilgrim" && o != e && Hypot(o.pos.x - e.pos.x, o.pos.z - e.pos.z) < PILGRIM_R)
                    { dmg *= 0.35f; SpawnParticles(info.point ?? BodyPoint(e), 2, 3, goldSpark, 0.06f, 0.3f); break; }
            if (e.kind == "clock" && e.regenT <= 0 && dmg < e.hp) e.regenT = 2.0f; // two seconds to finish it, or it rewinds
            // the Bell Wraith is warded: nothing from the ground reaches it - grapple up to it
            if (e.kind == "bell" && !netRemoteHit && BellWarded(e, dmg, info)) // (co-op: a teammate's hit was checked on their own machine)
            {
                SpawnParticles(info.point ?? e.pos, 4, 4, goldSpark, 0.07f, 0.35f); if (time - bellWardT > 0.4f) { bellWardT = time; Sfx("ding", 0.35f); ShockRing(e.pos, 2.2f, 0xffd060, 0.3f); }
                if (!bellHinted) { bellHinted = true; Banner("WARDED", "GRAPPLE UP TO THE BELL WRAITH TO HURT IT", "#ffd080"); }
                return 0;
            }
            return dmg;
        }
        void KindDeath(Ent e, Hit info)
        {
            switch (e.kind)
            {
                case "frost":
                case "wraith":
                {
                    var c = BodyPoint(e);
                    SpawnParticles(c, 26, 9, cyanMat, 0.12f, 0.8f); ShockRing(e.pos, 3.5f, 0xa0e8ff, 0.4f);
                    if (e.kind == "frost" && Vector3.Distance(c, ChestPos) < 3.5f) chillT = Mathf.Max(chillT, 1.8f);
                    break;
                }
                case "bomber": BomberBlast(e, 20); break;
                case "tank": if (!e.exploded) { e.exploded = true; Explode(BodyPoint(e), 6.5f, 10, 0xff7a20, 22, false); SpawnParticles(BodyPoint(e), 20, 6, ashMat, 0.3f, 1.6f, -2); } break;
                case "clock": SpawnParticles(e.pos, 20, 7, goldSpark, 0.08f, 0.7f, 0); break;
                case "pilgrim":
                {
                    // the lantern shatters: glass and light burst out of it, the shield ring collapses, and every enemy it was
                    // shielding loses it at once (a flash on each, a moment's stagger)
                    var lp = e.lantern ? e.lantern.position : BodyPoint(e);
                    Blast(lp, 2.6f, 0xffd080); Blast(lp, 1.2f, 0xffffff); SpawnParticles(lp, 30, 9, goldSpark, 0.1f, 0.8f); SpawnParticles(lp, 16, 6, cyanMat, 0.06f, 0.5f);
                    ShockRing(V(e.pos.x, e.pos.y + 0.2f, e.pos.z), PILGRIM_R, 0xffd080, 0.5f); Sfx("shatter"); Sfx("ding", 0.6f);
                    int freed = 0;
                    foreach (var o in enemies)
                        if (!o.dead && o != e && o.kind != "pilgrim" && Hypot(o.pos.x - e.pos.x, o.pos.z - e.pos.z) < PILGRIM_R && !PilgrimShielded(o, e))
                        { freed++; Blast(BodyPoint(o), 0.9f, 0xffd080); SpawnParticles(BodyPoint(o), 8, 4, goldSpark, 0.06f, 0.4f); o.stun = Mathf.Max(o.stun, 0.45f); }
                    if (freed > 0) AddStyle("LANTERN BROKEN", 60 + freed * 20);
                    if (e.lantern) e.lantern.gameObject.SetActive(false);
                    if (e.links != null) { foreach (var l in e.links) if (l) Destroy(l); e.links = null; }
                    if (e.bubbles != null) { foreach (var b in e.bubbles) if (b) { SpawnParticles(b.transform.position, 8, 4, goldSpark, 0.06f, 0.4f); Destroy(b); } e.bubbles = null; }
                    break;
                }
            }
        }
        void BomberBlast(Ent e, float selfDmg)
        {
            if (e.exploded) return; e.exploded = true;
            var c = BodyPoint(e);
            Explode(c, 5.5f, 9, 0xff6010, selfDmg, false);
            SpawnParticles(c, 24, 7, ashMat, 0.3f, 1.4f, -2); SpawnParticles(c, 30, 14, emberMat, 0.1f, 0.8f);
            if (!e.dead) KillEnemy(e, new Hit { explosion = true, point = c });
        }
    }
}
