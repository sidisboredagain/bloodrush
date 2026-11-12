using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    // THE SERAPH, reworked (boss 2, the Sanctum):
    //  phase 1 - THE CELESTIAL SERAPH: white porcelain and gold armour (layered gold pauldrons, tassets, a gorget), six great
    //            wings, a blazing double halo with rays, a lance with golden wings at the guard, radiant motes rising off him
    //  phase 2 - THE FALLEN DIVINITY: a cinematic transformation (he rises, spreads his wings, the white feathers burn away and
    //            torn black-and-gold wings tear out in their place, the halo cracks into floating shards, corrupted gold runs
    //            over his armour, a cracked mask closes over his face, his lance becomes a vast dark greatsword, a burst of
    //            light and dark - and he comes down). The Sanctum is corrupted with him: a storm sky with golden lightning,
    //            cracked columns shedding stones that float, golden cracks spreading across the floor, angel statues breaking,
    //            black feathers falling, the light turned to black and dark gold. Layout, bounds and collision don't change.
    public partial class Game
    {
        // ================= extra parts for the Seraph (built hidden where they belong to phase 2) =================
        void SeraphParts(Ent e, SerRig r)
        {
            bool oc = Mk.colliders; Mk.colliders = false; // (decoration only: the hitboxes stay exactly as they were)
            SeraphPalette(false);
            var porcelain = r.mats[0]; var gold = r.mats[1]; var cloth = r.mats[2];
            gold.SetFloat("_Metallic", 0.55f); gold.SetFloat("_Smoothness", 0.7f); // (polished, but bright even where there's little to reflect)
            foreach (var rr in e.g.GetComponentsInChildren<Renderer>(true)) rr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
            var white = EM(e, Mats.Basic(0xffffff)); var sun = EM(e, Mats.Add(0xffe8a0, 0.55f, null, true, true));
            // ---- phase 1 detail ----
            // layered gold pauldrons, each with a gem, over the shoulders
            for (int i = 0; i < 2; i++)
            {
                float s = i == 0 ? -1 : 1; var sh = r.sh[i];
                for (int k = 0; k < 3; k++) MshS(sh, DOME(0.13f - k * 0.015f, Mathf.PI * 0.5f, 0, Mathf.PI * 2, 18, 8), gold, s * 0.02f, 0.05f - k * 0.055f, 0, 0, 0, s * (0.35f + k * 0.18f), 1.15f, 0.7f, 1.1f);
                Mk.Msh(sh, SP(0.03f, 10, 8), r.eyeMat, s * 0.05f, 0.1f, 0.06f);
                for (int k = 0; k < 3; k++) MshS(sh, FEATHER(0.2f - k * 0.03f, 0.05f, 0.04f), gold, s * 0.08f, 0.06f, -0.04f + k * 0.04f, 0, 0, -s * (1.8f + k * 0.3f), s, 1, 1);
            }
            // a gorget, gold tassets over the gown, a sash of light
            MshS(r.torso, RING(0.12f, 0.03f, 32, 8), gold, 0, 0.62f, 0, Mathf.PI / 2, 0, 0, 1.2f, 1, 1);
            for (int k = 0; k < 7; k++) { float a = (k / 6f - 0.5f) * 2.6f; MshS(r.torso, FEATHER(0.34f, 0.09f, 0.03f), gold, Mathf.Sin(a) * 0.2f, 0.02f, Mathf.Cos(a) * 0.17f, -Mathf.PI + 0.15f, a, 0, 1, 1, 1); }
            MshS(r.torso, CAPS(0.012f, 0.62f), white, 0, 0.3f, 0.16f, 0, 0, 0.6f, 1, 1, 1);
            // the halo: an outer ring of light and long rays (additive, slowly turning)
            Mk.Msh(r.halo, RING(0.82f, 0.012f, 96, 4), sun);
            for (int k = 0; k < 24; k++) { float a = k / 24f * Mathf.PI * 2; MshS(r.halo, CAPS(0.008f, k % 2 == 0 ? 0.5f : 0.3f), sun, Mathf.Cos(a) * (0.95f + (k % 2) * 0.05f), Mathf.Sin(a) * (0.95f + (k % 2) * 0.05f), -0.01f, 0, 0, a - Mathf.PI / 2, 1, 1, 1); }
            Gl(r.halo, 0xfff0c0, 4.2f, 0.25f);
            // the lance: golden wings at the guard, rings of light turning on the shaft
            foreach (var s in new[] { -1f, 1f }) for (int k = 0; k < 3; k++) MshS(r.spear, FEATHER(0.42f - k * 0.08f, 0.08f, 0.05f), gold, s * 0.05f, 0, 1.36f - k * 0.06f, -Mathf.PI / 2, 0, -s * (0.6f + k * 0.35f), s, 1, 1);
            // radiance behind him
            var aura = Gl(r.torso, 0xfff0c8, 5.5f, 0.12f); aura.transform.localPosition = V(0, 0.4f, -0.35f); r.p1.Add(aura);
            foreach (var w in r.wings) w.Scale(1.15f); // (bigger, prouder wings)
            // ---- phase 2: THE FALLEN DIVINITY ----
            var dark = EM(e, Mats.Std(0x18141c, 0.3f, 0.8f, 0x020102)); var dgold = EM(e, Mats.Std(0x8a5a10, 0.25f, 0.9f, 0x301400));
            var ember = EM(e, Mats.Basic(0xffa030)); var fire = EM(e, Mats.Add(0xff8a20, 0.6f, null, true, true)); var blackF = EM(e, Mats.Std(0x0e0c10, 0.6f, 0.2f, 0, null, true));
            r.p2Edge = fire; r.p2Ember = ember;
            var rng = new System.Random(13);
            // the wings: everything already on each wing pivot becomes the white set; the torn black-and-gold set grows beside it
            for (int i = 0; i < r.wings.Count; i++)
            {
                var wp = r.wings[i]; float s = r.wingS[i], sc = new[] { 1f, 0.9f, 0.76f }[i % 3];
                var w1 = Mk.Piv(wp); var kids = new List<Transform>(); foreach (Transform c in wp.t) if (c != w1.t) kids.Add(c); foreach (var c in kids) c.SetParent(w1.t, true);
                var w2 = Mk.Piv(wp);
                int n = 10;
                for (int f = 0; f < n; f++)
                {
                    if (f > 1 && rng.NextDouble() < 0.18) continue; // torn out
                    float len = r3((2.3f - f * 0.16f) * sc * (0.75f + (float)rng.NextDouble() * 0.45f)), ang = 0.25f + f * 0.15f, x = s * (0.04f + f * 0.17f * sc), y = f * 0.015f, z = -0.012f * f;
                    var fe = MshS(w2, FEATHER(len, r3(0.15f * sc), 0.1f), blackF, x, y, z, 0, 0, -s * ang, s, 1, 1); r.p2Feathers.Add(fe.transform);
                    MshS(w2, FEATHER(r3(len * 1.04f), r3(0.2f * sc), 0.1f), fire, x, y - 0.012f, z - 0.006f, 0, 0, -s * ang, s, 1, 1);
                    if (f % 3 == 0) MshS(w2, CAPS(0.008f, len * 0.8f), ember, x + s * 0.01f, y + len * 0.4f * Mathf.Cos(ang), z + 0.01f, 0, 0, -s * ang, 1, 1, 1);
                }
                for (int f = 0; f < 6; f++) { float len = r3((1.0f - f * 0.1f) * sc), ang = 0.4f + f * 0.2f; MshS(w2, FEATHER(len, r3(0.09f * sc), 0.06f), dgold, s * (0.04f + f * 0.15f * sc), 0.02f, 0.04f, 0, 0, -s * ang, s, 1, 1); }
                Mk.Msh(w2, LIMB(0.04f, 0.014f, r3(0.75f * sc)), dgold, 0.03f * s, 0, 0.05f, 0, 0, s * (Mathf.PI / 2 + 0.15f));
                for (int k = 0; k < 3; k++) MshS(w2, Geo.Cone(0.025f, 0.22f, 5), dgold, s * (0.2f + k * 0.22f) * sc, 0.06f + k * 0.03f, 0.05f, 0, 0, -s * (0.9f + k * 0.15f), 1, 1, 1); // bone spurs along the leading edge
                w2.Scale(0.001f); w2.go.SetActive(false);
                r.w1.Add(w1); r.w2.Add(w2);
            }
            // the shattered halo: eight arcs of the old ring and a few rays, hanging apart behind his head
            r.haloFrag = Mk.Piv(r.head, 0, 0.25f, -0.36f);
            for (int k = 0; k < 8; k++)
            {
                var fr = Mk.Piv(r.haloFrag); fr.SetRot(0, 0, k * Mathf.PI / 4 + (float)rng.NextDouble() * 0.3f);
                Mk.Msh(fr, RING(0.66f, 0.035f, 10, 6, 0.55f + (float)rng.NextDouble() * 0.25f), dgold); Mk.Msh(fr, RING(0.6f, 0.012f, 10, 4, 0.5f), fire);
                if (k % 2 == 0) MshS(fr, CAPS(0.02f, 0.3f), dgold, 0.75f, 0.08f, 0, 0, 0, -Mathf.PI / 2 + 0.2f, 1, 1, 1);
                r.frags.Add(fr);
            }
            var hg = Gl(r.haloFrag, 0xff9030, 3.4f, 0.3f); r.haloFrag.go.SetActive(false);
            // the cracked celestial mask, closing over the face; the eyes behind it burning
            r.mask = Mk.Piv(r.head, 0, 0.06f, 0.0f);
            MshS(r.mask, Geo.Sphere(0.275f, 22, 14, Mathf.PI / 2 - 1.15f, 2.3f, 0.2f * Mathf.PI, 0.62f * Mathf.PI), dgold, 0, 0, 0, 0, 0, 0, 0.82f, 1.12f, 0.9f);
            foreach (var (cx, cy, cr, cl) in new[] { (0.04f, 0.1f, 0.4f, 0.16f), (-0.07f, -0.05f, -0.6f, 0.2f), (0.09f, -0.08f, 0.9f, 0.12f), (0f, 0.18f, 0.1f, 0.12f) }) Mk.Msh(r.mask, Geo.Box(0.008f, cl, 0.008f), ember, cx, cy, 0.235f, -0.2f, 0, cr);
            foreach (var s in new[] { -1f, 1f }) { Mk.Msh(r.mask, CAPS(0.026f, 0.1f), ember, 0.085f * s, 0.05f, 0.235f, 0, 0, Mathf.PI / 2 + 0.35f * s); var eg = Gl(r.mask, 0xffb040, 0.55f, 0.9f); eg.transform.localPosition = V(0.085f * s, 0.05f, 0.28f); }
            for (int k = 0; k < 5; k++) { float a = (k / 4f - 0.5f) * 1.6f; MshS(r.mask, Geo.Cone(0.022f, 0.34f - Mathf.Abs(k - 2) * 0.06f, 5), dgold, Mathf.Sin(a) * 0.18f, 0.3f, Mathf.Cos(a) * 0.1f, -0.25f, 0, -a * 0.6f, 1, 1, 1); } // a crown of black thorns
            r.mask.go.SetActive(false);
            // corrupted armour: spiked pauldrons, a shard breastplate, armour fragments floating round the chest and shoulders
            for (int i = 0; i < 2; i++)
            {
                float s = i == 0 ? -1 : 1; var sp = Mk.Piv(r.sh[i]);
                for (int k = 0; k < 4; k++) MshS(sp, Geo.Cone(0.035f, 0.32f - k * 0.04f, 5), dgold, s * (0.04f + k * 0.03f), 0.1f, -0.06f + k * 0.05f, 0, 0, -s * (0.35f + k * 0.2f), 1, 1, 1);
                MshS(sp, DOME(0.15f, Mathf.PI * 0.5f, 0, Mathf.PI * 2, 18, 8), dark, s * 0.02f, 0.06f, 0, 0, 0, s * 0.4f, 1.2f, 0.7f, 1.15f);
                sp.go.SetActive(false); r.p2.Add(sp.go);
            }
            var plate = Mk.Piv(r.torso);
            for (int k = 0; k < 6; k++) { float a = (k / 5f - 0.5f) * 1.8f; MshS(plate, Geo.Box(0.09f, 0.22f, 0.02f), dark, Mathf.Sin(a) * 0.17f, 0.38f + (k % 2) * 0.05f, Mathf.Cos(a) * 0.15f + 0.03f, 0.1f, a, (k - 2.5f) * 0.12f, 1, 1, 1); }
            for (int k = 0; k < 4; k++) MshS(plate, Geo.Cone(0.03f, 0.18f, 5), dgold, (k - 1.5f) * 0.09f, 0.54f, 0.15f, -0.7f, 0, (k - 1.5f) * 0.25f, 1, 1, 1);
            plate.go.SetActive(false); r.p2.Add(plate.go);
            // the core: a bright unstable heart of light burning through cracks in the chest
            var core = Mk.Piv(r.torso, 0, 0.3f, 0.15f);
            Mk.Msh(core, SP(0.09f, 16, 12), white); r.coreGlow = Gl(core, 0xffd080, 1.6f, 0.9f);
            for (int k = 0; k < 7; k++) { float a = k * 0.9f; MshS(core, Geo.Box(0.012f, 0.16f + (k % 3) * 0.05f, 0.01f), ember, Mathf.Cos(a) * 0.1f, Mathf.Sin(a) * 0.1f, 0.03f, 0, 0, a + Mathf.PI / 2, 1, 1, 1); }
            r.core = core; core.go.SetActive(false);
            r.armorOrbit = Mk.Piv(r.torso, 0, 0.38f, 0);
            for (int k = 0; k < 9; k++) { float a = k / 9f * Mathf.PI * 2; MshS(r.armorOrbit, Geo.Box(0.07f, 0.14f, 0.025f), k % 3 == 0 ? dgold : dark, Mathf.Cos(a) * 0.6f, (k % 3 - 1) * 0.18f, Mathf.Sin(a) * 0.6f, (float)rng.NextDouble(), -a, (float)rng.NextDouble(), 1, 1, 1); }
            r.armorOrbit.go.SetActive(false);
            // the weapon: the lance is unmade, and a vast dark greatsword with gold running through it takes its place
            foreach (Transform c in r.spear.t) r.spearP1.Add(c.gameObject);
            r.sword = Mk.Piv(r.spear);
            var bladeShape = BLADE("sgreat", () => new GeoX.Shape().MoveTo(0, 0).LineTo(0.13f, 0.1f).LineTo(0.1f, 0.7f).LineTo(0.15f, 1.1f).LineTo(0.1f, 1.6f).LineTo(0.15f, 2.1f).LineTo(0.1f, 2.6f).LineTo(0.12f, 3.0f).LineTo(0, 3.6f).LineTo(-0.12f, 3.0f).LineTo(-0.1f, 2.6f).LineTo(-0.15f, 2.1f).LineTo(-0.1f, 1.6f).LineTo(-0.15f, 1.1f).LineTo(-0.1f, 0.7f).LineTo(-0.13f, 0.1f).LineTo(0, 0), 0.05f, 0.02f);
            Mk.Msh(r.sword, bladeShape, dark, 0, 0, 0.25f, Mathf.PI / 2);
            Mk.Msh(r.sword, Geo.Box(0.035f, 0.055f, 3.1f), ember, 0, 0, 1.85f);                   // the gold fire running down its fuller
            MshS(r.sword, bladeShape, fire, 0, 0, 0.22f, Mathf.PI / 2, 0, 0, 1.25f, 1.03f, 0.6f);
            foreach (var s in new[] { -1f, 1f }) { for (int k = 0; k < 3; k++) MshS(r.sword, FEATHER(0.5f - k * 0.1f, 0.1f, 0.06f), dgold, s * 0.08f, 0, 0.22f - k * 0.05f, -Mathf.PI / 2, 0, -s * (1.0f + k * 0.3f), s, 1, 1); }
            Mk.Msh(r.sword, SP(0.07f, 12, 10), ember, 0, 0, 0.22f); Mk.Msh(r.sword, LIMB(0.035f, 0.035f, 0.7f), dark, 0, 0, -0.1f, -Mathf.PI / 2); Mk.Msh(r.sword, SP(0.06f, 12, 10), dgold, 0, 0, -0.5f);
            Gl(r.sword, 0xff9030, 1.4f, 0.5f).transform.localPosition = V(0, 0, 3.4f);
            r.sword.go.SetActive(false);
            var p2aura = Gl(r.torso, 0xff6a20, 6, 0.0f); p2aura.transform.localPosition = V(0, 0.4f, -0.4f); r.p2AuraM = p2aura.GetComponent<Renderer>().sharedMaterial;
            foreach (var rr in e.g.GetComponentsInChildren<Renderer>(true)) rr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
            Mk.colliders = oc;
            r.colors = new Color[r.mats.Count]; for (int i = 0; i < r.mats.Count; i++) r.colors[i] = r.mats[i].GetColor("_BaseColor");
        }

        // the Seraph's light: cold blue in his radiant form, dark orange fire once he falls (beam, blades, rings, sparks)
        bool serOrange; Material SerFx => serOrange ? emberMat : cyanMat; int SerCol => serOrange ? 0xff7a20 : 0x80e8ff;
        void SeraphPalette(bool orange, Ent e = null)
        {
            serOrange = orange;
            Mats.SetColor(beamCore.GetComponent<Renderer>().sharedMaterial, orange ? 0xffd8a0 : 0xffffff);
            Mats.SetColor(beamOuter.GetComponent<Renderer>().sharedMaterial, orange ? 0xff6a10 : 0x60d8ff);
            Mats.SetColor(beamHit.GetComponent<Renderer>().sharedMaterial, orange ? 0xff8a30 : 0xa0f0ff);
            Mats.SetColor(bladeMat, orange ? 0xffa050 : 0xc0f4ff);
            if (orange && e != null && e.sr != null)
                foreach (var rr in e.g.GetComponentsInChildren<Renderer>(true))
                    if (rr.gameObject.name == "glow") { var c = rr.sharedMaterial.GetColor("_BaseColor"); if (c.b > c.r) { var o = Mats.Hex(0xff7a20); o.a = c.a; rr.sharedMaterial.SetColor("_BaseColor", o); } } // every blue light on him turns to fire
        }
        // ================= the transformation =================
        class SerCine { public bool on; public float t; public Vector3 lockPos, camPos; public Quaternion camRot; public float fov; public bool corrupted, burst, broke, haloBroke; }
        readonly SerCine SC = new SerCine();
        bool CineLock => WC.on || SC.on; // (the hazards hold off while a boss's cinematic plays)
        void SeraphFallBegin(Ent e)
        {
            SC.on = true; SC.t = 0; SC.lockPos = P.pos; SC.camPos = camT.position; SC.camRot = camT.rotation; SC.fov = cam.fieldOfView; SC.corrupted = SC.burst = SC.broke = SC.haloBroke = false;
            e.invuln = true; SrState(e, "fallRise", 99); e.vel = Vector3.zero; teleLine.SetActive(false); beamCore.SetActive(false); beamOuter.SetActive(false); beamHit.SetActive(false);
            foreach (var p in projectiles) if (!p.friendly) p.life = 0.0001f;
            Banner("THE FALL", "THE SERAPH FORSAKES THE LIGHT", "#ffb040"); Sfx("holy"); Sfx("rumble", 0.7f);
            TryBossCine(2, 1, e); // filmed as a cinematic (GameBossCine.cs)
        }
        static readonly float[] SER_PORCELAIN2 = { 0.16f, 0.13f, 0.16f }, SER_CLOTH2 = { 0.22f, 0.05f, 0.06f };
        void UpdateSeraphFall(Ent e, float dt)
        {
            var r = e.sr; SC.t += dt; float T = SC.t;
            P.pos = SC.lockPos; P.vel = Vector3.zero; mouseDown = false; P.iframes = Mathf.Max(P.iframes, 0.3f);
            var top = V(0, 15, 0);
            // 1 (0-2.0) he rises over the centre of the arena
            if (T < 2.0f) { float k = Smoother(T / 2.0f); e.pos = Vector3.Lerp(e.pos, top + V(0, k * 2, 0), 1 - Mathf.Exp(-2.2f * dt)); if (rand < 0.5f) SpawnParticles(e.pos + Random.insideUnitSphere * 3, 1, 1, goldSpark, 0.12f, 1.0f, -2); P.shake = Mathf.Max(P.shake, 0.12f); }
            // 2 (2.0-3.2) the white wings spread to their fullest, light gathering
            else if (T < 3.2f) { e.pos = Vector3.Lerp(e.pos, top + V(0, 2, 0), 1 - Mathf.Exp(-3 * dt)); if (T - dt < 2.0f) { Sfx("holy"); Sfx("portalCharge"); } }
            // 3 (3.2-5.6) the breaking: the wings tear and change, the halo cracks apart, corruption runs over his armour
            else if (T < 5.6f)
            {
                float u = (T - 3.2f) / 2.4f;
                if (!SC.corrupted) { SC.corrupted = true; SanctumCorrupt(); Sfx("roar"); Sfx("boom", 0.6f); }
                e.pos = top + V(0, 2, 0) + Random.insideUnitSphere * 0.12f * (1 - u);
                P.shake = Mathf.Max(P.shake, 0.35f);
                // white feathers burn off into embers; the black wings tear out
                if (rand < 0.9f && r.w1.Count > 0) { var w = r.w1[Random.Range(0, r.w1.Count)]; if (w.go.activeSelf && w.t.childCount > 0) { var c = w.t.GetChild(Random.Range(0, w.t.childCount)); SpawnParticles(c.position, 3, 3, rand < 0.5f ? goldSpark : emberMat, 0.1f, 1.1f, -1.5f); } }
                float g = Smoother(Mathf.Clamp01((u - 0.15f) / 0.6f));
                for (int i = 0; i < r.w2.Count; i++)
                {
                    if (!r.w2[i].go.activeSelf && u > 0.15f) { r.w2[i].go.SetActive(true); }
                    r.w2[i].Scale(Mathf.Max(0.001f, g) * (1 + Mathf.Sin(T * 40 + i) * 0.05f * (1 - g)));
                    r.w1[i].Scale(Mathf.Max(0.001f, 1 - Smoother(Mathf.Clamp01(u / 0.55f))));
                    if (u > 0.55f && r.w1[i].go.activeSelf) r.w1[i].go.SetActive(false);
                }
                if (u > 0.15f && !SC.broke) { SC.broke = true; Sfx("shatter"); Sfx("growlLow"); whiteFlash = Mathf.Max(whiteFlash, 0.2f); }
                // the halo cracks (u 0.3) and breaks into shards that drift apart
                if (u > 0.3f && !SC.haloBroke)
                {
                    SC.haloBroke = true; r.halo.go.SetActive(false); r.haloFrag.go.SetActive(true); Sfx("shatter"); Sfx("ding", 0.7f);
                    SpawnParticles(r.haloFrag.t.position, 40, 7, goldSpark, 0.12f, 1.0f, 0); Blast(r.haloFrag.t.position, 2.5f, 0xffd080);
                }
                if (SC.haloBroke) { float k = Smoother(Mathf.Clamp01((u - 0.3f) / 0.4f)); for (int i = 0; i < r.frags.Count; i++) { var fr = r.frags[i]; float a = i * Mathf.PI / 4; fr.t.localPosition = V(Mathf.Cos(a) * 0.25f * k, Mathf.Sin(a) * 0.25f * k, -0.08f * k * (i % 2)); } }
                // corruption runs over the armour: porcelain to obsidian, silk to dried blood, gold darkening
                float c2 = Smoother(Mathf.Clamp01((u - 0.2f) / 0.7f));
                Mats.SetColor(r.mats[0], Color.Lerp(r.colors[0], new Color(SER_PORCELAIN2[0], SER_PORCELAIN2[1], SER_PORCELAIN2[2]), c2));
                Mats.SetColor(r.mats[1], Color.Lerp(r.colors[1], new Color(0.55f, 0.36f, 0.07f), c2));
                Mats.SetColor(r.mats[2], Color.Lerp(r.colors[2], new Color(SER_CLOTH2[0], SER_CLOTH2[1], SER_CLOTH2[2]), c2));
                if (r.mats.Count > 4) Mats.SetColor(r.mats[4], Color.Lerp(r.colors[4], new Color(0.2f, 0.16f, 0.18f), c2));
                if (u > 0.45f) foreach (var g2 in r.p2) if (g2 && !g2.activeSelf) { g2.SetActive(true); SpawnParticles(g2.transform.position, 10, 3, emberMat, 0.08f, 0.6f, 0); }
                if (u > 0.5f && !r.mask.go.activeSelf) { r.mask.go.SetActive(true); r.core.go.SetActive(true); Sfx("slam", 0.7f); }
                // the lance comes apart and the greatsword forms out of the light
                if (u > 0.6f && !r.sword.go.activeSelf) { foreach (var sp in r.spearP1) if (sp) sp.SetActive(false); r.sword.go.SetActive(true); r.sword.Scale(0.2f); Sfx("blink"); SpawnParticles(r.spear.t.position, 30, 5, emberMat, 0.1f, 0.6f, 0); }
                if (r.sword.go.activeSelf) r.sword.Scale(Mathf.Lerp(r.sword.t.localScale.x, 1.35f, 1 - Mathf.Exp(-5 * dt)));
                Mats.SetColor(r.featherGlow, Color.Lerp(new Color(0.5f, 0.9f, 1f), new Color(1f, 0.55f, 0.15f), c2)); Mats.SetColor(r.eyeMat, Color.Lerp(new Color(0.5f, 0.9f, 1f), new Color(1f, 0.7f, 0.2f), c2));
            }
            // 4 (5.6-6.4) a massive burst of light and dark
            else if (T < 6.4f)
            {
                if (!SC.burst)
                {
                    SC.burst = true; r.armorOrbit.go.SetActive(true); SeraphPalette(true, e);
                    whiteFlash = 0.8f; P.shake = 1.4f; freeze = 0.1f; Sfx("boom"); Sfx("bossDeath", 0.5f); Sfx("toll", 0.8f);
                    var c = BodyPoint(e); Blast(c, 9, 0xffd080); Blast(c, 5, 0xffffff);
                    for (int i = 0; i < 4; i++) ShockRing(c, 10 + i * 9, i % 2 == 0 ? 0xffb040 : 0x302030, 0.6f + i * 0.2f);
                    ShockRing(V(c.x, 0.3f, c.z), 40, 0xffb040, 1.2f);
                    SpawnParticles(c, 120, 18, emberMat, 0.15f, 1.6f, -1); SpawnParticles(c, 80, 14, ashMat, 0.4f, 2.2f, -1);
                }
                e.pos = top + V(0, 2, 0);
            }
            // 5 (6.4-8.2) he comes down into the arena, transformed
            else
            {
                float k = Smoother(Mathf.Clamp01((T - 6.4f) / 1.8f)); var land = V(0, Mathf.Max(P.pos.y + 6, 7), 0) + (V(SC.lockPos.x, 0, SC.lockPos.z).normalized * -6);
                e.pos = Vector3.Lerp(top + V(0, 2, 0), land, k);
                if (rand < 0.7f) SpawnParticles(BodyPoint(e) + Random.insideUnitSphere * 2, 1, 2, emberMat, 0.12f, 0.8f, 0);
                if (T >= 8.2f)
                {
                    SC.on = false; e.invuln = false; SrState(e, "hover", 0.6f); vm.gameObject.SetActive(true); introGroup.alpha = 0;
                    Banner("THE FALLEN DIVINITY", "SERAPH OF RUIN", "#ffa030"); Sfx("roar"); audioMgr.Intensity();
                    if (Auth) for (int i = 0; i < 3; i++) { var s = FreeSpot(14); SpawnDrone(s.x, 10 + rand * 5, s.y); } // (co-op: the host's minions)
                    AddRing(V(e.pos.x, Mathf.Max(0, P.pos.y), e.pos.z), 22, 30, 15, 0xffa030);
                }
            }
            var toP = SC.lockPos - e.pos; TurnTo(e, Mathf.Atan2(toP.x, toP.z), 3, dt); e.pitch *= Mathf.Exp(-6 * dt);
        }
        // the transformation's camera (runs after UpdateCamera): low and wide as he rises, close on him as he breaks, pulled
        // back for the burst, then home behind your eyes as he comes down
        void UpdateSeraphCam(float rdt)
        {
            if (!SC.on || boss == null || boss.sr == null) return;
            var e = boss; float T = SC.t; var eye = SC.lockPos + V(0, 1.7f, 0);
            var flat = eye - e.pos; flat.y = 0; flat = flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.forward; var side = Vector3.Cross(Vector3.up, flat);
            var head = e.pos + V(0, 3.0f, 0); Vector3 pos, look; float fov;
            if (T < 2.0f) { pos = e.pos + flat * 24 + side * 6 + V(0, -6, 0); look = e.pos + V(0, 1, 0); fov = 58; pos.y = Mathf.Max(pos.y, 1.5f); }
            else if (T < 3.2f) { pos = e.pos + flat * 17 + V(0, -2.5f, 0); look = e.pos + V(0, 2, 0); fov = 64; }
            else if (T < 5.6f) { float u = (T - 3.2f) / 2.4f, a = Mathf.Lerp(-0.7f, 0.7f, u); var d = Quaternion.AngleAxis(a * Mathf.Rad2Deg, Vector3.up) * flat; pos = head + d * Mathf.Lerp(9, 6.5f, u) + V(0, -0.6f, 0); look = head; fov = 46; }
            else if (T < 6.4f) { pos = e.pos + flat * 30 + V(0, 1, 0); look = e.pos + V(0, 1.5f, 0); fov = 70; }
            else { float k = Smoother((T - 6.4f) / 1.8f); pos = Vector3.Lerp(e.pos + flat * 26 + V(0, 2, 0), eye, k); look = Vector3.Lerp(e.pos + V(0, 1.5f, 0), e.pos + V(0, 2, 0), k); fov = Mathf.Lerp(66, setFov > 0 ? setFov : 80, k); }
            pos.y = Mathf.Max(pos.y, 0.8f);
            var rot = Quaternion.LookRotation(pos - look); float s = 1 - Mathf.Exp(-4 * rdt);
            SC.camPos = Vector3.Lerp(SC.camPos, pos, T < 0.05f ? 0.15f : s); SC.camRot = Quaternion.Slerp(SC.camRot, rot, T < 0.05f ? 0.15f : s); SC.fov = Mathf.Lerp(SC.fov, fov, s);
            camT.position = SC.camPos + Random.insideUnitSphere * P.shake * 0.12f; camT.rotation = SC.camRot; cam.fieldOfView = SC.fov;
            vm.gameObject.SetActive(false);
            introGroup.alpha = 1; introName.anchoredPosition = V(-4000, 0, 0); float bar = 100 * Mathf.Min(1, T / 0.6f) * Mathf.Min(1, (8.2f - T) / 0.6f); barTop.sizeDelta = new Vector2(0, bar); barBot.sizeDelta = new Vector2(0, bar);
        }
        // every frame: the living effects of either form
        void SeraphFx(Ent e, float dt)
        {
            var r = e.sr; if (r == null) return;
            if (!e.phase2 || !SC.haloBroke && !r.haloFrag.go.activeSelf)
            {
                // the radiant form: motes of light rising off him
                if (rand < 0.35f) SpawnParticles(BodyPoint(e) + Random.insideUnitSphere * 2.2f, 1, 0.4f, goldSpark, 0.06f, 1.2f, -1.2f);
                return;
            }
            // the fallen form: feathers crumbling into embers, the halo shards turning, the core flickering, armour orbiting
            if (r.p2Feathers.Count > 0 && rand < 0.7f) { var f = r.p2Feathers[Random.Range(0, r.p2Feathers.Count)]; if (f && f.gameObject.activeInHierarchy) SpawnParticles(f.position, 1, 0.8f, rand < 0.6f ? emberMat : goldSpark, 0.08f, 1.3f, -1.4f); }
            for (int i = 0; i < r.frags.Count; i++) r.frags[i].rz += dt * (0.4f + (i % 3) * 0.3f) * (i % 2 == 0 ? 1 : -1);
            r.haloFrag.rz += dt * 0.6f;
            if (r.core.go.activeSelf) { float j = 1 + Mathf.Sin(time * 23) * 0.15f + (rand - 0.5f) * 0.25f; r.core.Scale(j); }
            if (r.armorOrbit.go.activeSelf) { r.armorOrbit.ry += dt * 1.2f; r.armorOrbit.py = 0.38f + Mathf.Sin(time * 1.7f) * 0.04f; }
            if (r.p2AuraM) Mats.SetAlpha(r.p2AuraM, 0.18f + 0.08f * Mathf.Sin(time * 3));
            if (rand < 0.3f) SpawnParticles(BodyPoint(e) + Random.insideUnitSphere * 2.5f, 1, 0.5f, ashMat, 0.1f, 1.6f, -0.8f);
            Mats.SetColor(r.p2Edge, Color.Lerp(new Color(1f, 0.45f, 0.1f), new Color(1f, 0.75f, 0.3f), 0.5f + 0.5f * Mathf.Sin(time * 4)));
        }

        // ================= the Sanctum corrupted =================
        readonly List<(Transform t, Vector3 basePos, float ph)> sanctumStatueHeads = new List<(Transform, Vector3, float)>();
        ReflectionProbe sanctumProbe; readonly List<N> sanctumStatueWings = new List<N>(); readonly List<Material> sanctumStatueMats = new List<Material>();
        // kneeling angels of white marble round the edge of the Sanctum, heads bowed over their swords
        void SanctumStatues()
        {
            sanctumStatueHeads.Clear(); sanctumStatueWings.Clear(); sanctumStatueMats.Clear();
            sanctumProbe = MakeMirrorProbe(V(0, 12, 0), V(160, 70, 160), 1); // (the gold has the Sanctum to shine with)
            var marble = Mats.Std(0xf0ece2, 0.4f, 0.05f); var goldM = Mats.Std(0xd8a828, 0.25f, 0.85f); sanctumStatueMats.Add(marble);
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI / 3, R = Hv(62); var p = V(Mathf.Cos(a) * R, 0, Mathf.Sin(a) * R); if (Mathf.Abs(p.x) > LB - 4 || Mathf.Abs(p.z) > LB - 4) { p = p.normalized * (LB - 6); p.y = 0; }
                // the plinth: two stepped tiers, gold-trimmed
                Box(p.x, 0, p.z, 3.6f, 0.5f, 3.6f, M.block); TrimRect(p.x, p.z, 3.6f, 3.6f, 0.5f);
                Box(p.x, 0.5f, p.z, 3.0f, 0.9f, 3.0f, M.block); TrimRect(p.x, p.z, 3.0f, 3.0f, 1.4f);
                var s = Mk.Piv(levelRoot, p.x, 1.4f, p.z); s.SetRot(0, Mathf.Atan2(-p.x, -p.z), 0);
                // a kneeling angel in a long robe, its folds pooling on the plinth
                Mk.Msh(s, Geo.Cyl(0.52f, 0.95f, 1.25f, 16), marble, 0, 0.62f, -0.15f);
                for (int f = 0; f < 10; f++) { float fa = f * Mathf.PI * 2 / 10; Mk.Msh(s, Geo.Box(0.07f, 1.15f, 0.09f), marble, Mathf.Sin(fa) * 0.74f, 0.55f, -0.15f + Mathf.Cos(fa) * 0.74f, Mathf.Cos(fa) * 0.32f, fa, 0).transform.localRotation = Quaternion.Euler(0, fa * Mathf.Rad2Deg, 0) * Quaternion.Euler(-17, 0, 0); }
                Mk.Msh(s, Geo.Box(0.75f, 0.32f, 0.7f), marble, 0, 0.18f, 0.45f, 0.1f, 0, 0); // the knee under the robe, forward
                var torso = Mk.Piv(s, 0, 1.2f, -0.15f); torso.SetRot(0.18f, 0, 0);
                Mk.Msh(torso, Geo.Cyl(0.36f, 0.5f, 0.95f, 14), marble, 0, 0.45f, 0);
                Mk.Msh(torso, Geo.Sphere(0.42f, 14, 10), marble, 0, 0.95f, 0.02f).transform.localScale = V(1.25f, 0.7f, 0.85f); // chest and shoulders
                Mk.Msh(torso, Geo.Torus(0.4f, 0.035f, 20, 4), goldM, 0, 0.05f, 0, Mathf.PI / 2, 0, 0); // a gold sash at the waist
                foreach (var sx in new[] { -1f, 1f })
                {
                    Mk.Msh(torso, Geo.Sphere(0.19f, 10, 8), marble, sx * 0.46f, 0.98f, 0);
                    var ua = Mk.Piv(torso, sx * 0.48f, 0.95f, 0.02f); ua.SetRot(-0.55f, 0, sx * 0.18f);
                    Mk.Msh(ua, Geo.Cyl(0.11f, 0.15f, 0.62f, 10), marble, 0, -0.3f, 0);
                    var fa2 = Mk.Piv(ua, 0, -0.6f, 0); fa2.SetRot(-1.05f, 0, -sx * 0.55f);
                    Mk.Msh(fa2, Geo.Cyl(0.13f, 0.1f, 0.55f, 10), marble, 0, -0.27f, 0); // (a wide sleeve over the forearm)
                    Mk.Msh(fa2, Geo.Sphere(0.08f, 8, 6), marble, 0, -0.58f, 0); // the hands, folded on the pommel
                }
                // the sword, point down on the plinth in front of it
                var sw = Mk.Piv(s, 0, 0, 0.72f);
                Mk.Msh(sw, Geo.Box(0.09f, 1.55f, 0.025f), goldM, 0, 0.78f, 0); Mk.Msh(sw, Geo.Cone(0.045f, 0.18f, 4), goldM, 0, 0.0f, 0, Mathf.PI, 0, 0);
                Mk.Msh(sw, Geo.Box(0.62f, 0.07f, 0.08f), goldM, 0, 1.58f, 0); Mk.Msh(sw, Geo.Cyl(0.035f, 0.035f, 0.26f, 6), goldM, 0, 1.75f, 0); Mk.Msh(sw, Geo.Sphere(0.06f, 8, 6), goldM, 0, 1.9f, 0);
                // the head: bowed in prayer under a hood, a halo standing behind it
                var head = Mk.Piv(torso, 0, 1.28f, 0.08f); head.SetRot(0.45f, 0, 0);
                Mk.Msh(head, Geo.Sphere(0.24f, 14, 12), marble, 0, 0.18f, 0).transform.localScale = V(0.9f, 1.05f, 0.95f);
                Mk.Msh(head, Geo.Sphere(0.31f, 14, 10, 0, Mathf.PI * 2, 0, Mathf.PI * 0.62f), marble, 0, 0.18f, -0.06f).transform.localScale = V(1, 1.1f, 1.05f); // the hood
                Mk.Msh(head, Geo.Cyl(0.3f, 0.42f, 0.55f, 14, true), marble, 0, -0.12f, -0.1f); // the veil falling to the shoulders
                Mk.Msh(head, Geo.Torus(0.36f, 0.028f, 28, 4), goldM, 0, 0.32f, -0.34f);
                for (int k2 = 0; k2 < 12; k2++) { float ha = k2 * Mathf.PI / 6; Mk.Msh(head, Geo.Box(0.025f, 0.09f, 0.02f), goldM, Mathf.Cos(ha) * 0.42f, 0.32f + Mathf.Sin(ha) * 0.42f, -0.34f, 0, 0, ha - Mathf.PI / 2); }
                // the wings: folded up behind it, feathered - long primaries, a row of shorter coverts over them
                var wings = new List<N>();
                foreach (var sx in new[] { -1f, 1f })
                {
                    var w = Mk.Piv(torso, sx * 0.25f, 0.9f, -0.32f); w.SetRot(-0.35f, sx * 0.35f, -sx * 0.2f);
                    Mk.Msh(w, Geo.Cyl(0.07f, 0.1f, 1.5f, 8), marble, sx * 0.35f, 0.65f, -0.02f, 0, 0, -sx * 0.5f); // the wing's leading edge
                    for (int f = 0; f < 8; f++)
                    {
                        float u = f / 7f, len = Mathf.Lerp(2.9f, 1.0f, u);
                        var fp = Mk.Piv(w, sx * Mathf.Lerp(0.75f, 0.05f, u), Mathf.Lerp(1.3f, 0.15f, u), -0.04f - u * 0.05f); fp.SetRot(0, 0, -sx * Mathf.Lerp(0.15f, 1.15f, u));
                        var fm = Mk.Msh(fp, Geo.Sphere(0.5f, 8, 6), marble, 0, len * 0.5f, 0); fm.transform.localScale = V(0.34f, len, 0.07f);
                        if (f < 6) { var cp = Mk.Piv(w, sx * Mathf.Lerp(0.6f, 0.1f, u), Mathf.Lerp(1.15f, 0.35f, u), 0.04f); cp.SetRot(0, 0, -sx * Mathf.Lerp(0.3f, 1.1f, u)); Mk.Msh(cp, Geo.Sphere(0.5f, 8, 6), marble, 0, 0.35f, 0).transform.localScale = V(0.3f, 0.75f, 0.08f); }
                    }
                    sanctumStatueWings.Add(w);
                }
                foreach (var rr in s.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                SolidAABB(V(p.x - 0.8f, 1.4f, p.z - 0.8f), V(p.x + 0.8f, 4.4f, p.z + 0.8f));
                sanctumStatueHeads.Add((head.t, p, k * 1.1f));
            }
        }
        // the storm sky, golden lightning, the light going black and gold, columns cracking and shedding floating stones,
        // golden cracks spreading over the floor, the statues breaking, feathers falling, waves of dark energy
        void SanctumCorrupt()
        {
            if (arenaChanged || LEVEL == null || LEVEL.boss != 2) return; arenaChanged = true; roofShut = false;
            float t0 = -1, sun0 = sunLight.intensity; var sunC0 = sunLight.color; var amb0 = RenderSettings.ambientSkyColor; var eq0 = RenderSettings.ambientEquatorColor; var gr0 = RenderSettings.ambientGroundColor;
            var bg0 = cam.backgroundColor; float fogE0 = RenderSettings.fogEndDistance;
            var bgDark = Mats.Hex(0x0e0806); var sunDark = Mats.Hex(0xc87a28);
            // the storm: churning dark clouds high above the open roof, lit from below by gold
            var clouds = new GameObject("storm").transform; clouds.SetParent(levelRoot, false);
            var cloudMs = new List<Material>(); var under = new List<Material>();
            for (int k = 0; k < 26; k++)
            {
                float a = k / 26f * Mathf.PI * 2 + rand * 0.3f, rr = 25 + rand * 70, y = 55 + rand * 25;
                var m = Mats.Trans(k % 3 == 0 ? 0x2a2026 : 0x18141a, 0, false, false); cloudMs.Add(m);
                var c = Mk.Msh(clouds, Geo.Sphere(1, 12, 8), m, Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr); c.transform.localScale = V(22 + rand * 18, 7 + rand * 6, 18 + rand * 14); c.layer = Mk.LayerIgnore;
                if (k % 2 == 0) { var g = Mk.Glow(clouds, 0xff9a30, 30, 0); g.transform.localPosition = V(Mathf.Cos(a) * rr, y - 6, Mathf.Sin(a) * rr); under.Add(g.GetComponent<Renderer>().sharedMaterial); }
            }
            // golden lightning: jagged bolts from the clouds to the columns and the floor
            var boltM = Mats.Add(0xffd070, 0, null, false, true); var bolt = new GameObject("bolt").transform; bolt.SetParent(levelRoot, false);
            var segs = new List<Transform>(); for (int k = 0; k < 14; k++) { var b = Mk.Msh(bolt, Geo.Box(1, 1, 1), boltM); b.layer = Mk.LayerIgnore; segs.Add(b.transform); }
            float boltT = 0, nextBolt = 0.5f; float flashT = 0;
            // the columns crack: gold seams up them and stones torn loose from their tops, floating
            var crackM = Mats.Add(0xffa030, 0, null, true, true); var floaters = new List<(Transform t, Vector3 p, float ph, float sp)>(); var stoneM = Mats.Std(0xd8d0c0, 0.6f, 0.05f, 0, GrimeTex());
            for (int k = 0; k < 12; k++)
            {
                float a = k * Mathf.PI / 6 + Mathf.PI / 12; var c = V(Mathf.Cos(a) * Hv(50), 0, Mathf.Sin(a) * Hv(50));
                for (int s = 0; s < 5; s++) { float b = a + Mathf.PI + (s - 2) * 0.35f; var cp = c + V(Mathf.Cos(b), 0, Mathf.Sin(b)) * 2.02f; Ig(Deco(Geo.Box(0.12f, 6 + s * 2.5f, 0.06f), crackM, cp.x, 4 + s * 3.5f, cp.z, 0, -b, (s - 2) * 0.15f)); }
                for (int s = 0; s < 5; s++) { var g = Deco(Geo.Box(1.2f + rand, 0.8f + rand * 0.8f, 1 + rand), stoneM, 0, 0, 0, rand * 3, rand * 3, rand * 3); Ig(g); var pp = c + V((rand - 0.5f) * 5, 27 + rand * 6, (rand - 0.5f) * 5); g.transform.localPosition = pp; g.SetActive(false); floaters.Add((g.transform, pp, rand * 6, 0.3f + rand * 0.5f)); }
            }
            // the raised platforms: shattered pieces hanging round their edges
            foreach (var sx in new[] { -1f, 1f }) foreach (var sz in new[] { -1f, 1f }) for (int s = 0; s < 6; s++)
                    { var g = Deco(Geo.Box(0.8f + rand * 1.2f, 0.5f + rand * 0.6f, 0.8f + rand), stoneM, 0, 0, 0, rand * 3, rand * 3, rand * 3); Ig(g); var pp = V(sx * Hv(30) + (rand - 0.5f) * 12, 5 + rand * 3, sz * Hv(30) + (rand - 0.5f) * 12); g.transform.localPosition = pp; g.SetActive(false); floaters.Add((g.transform, pp, rand * 6, 0.4f + rand * 0.5f)); }
            // the floor: golden cracks spreading out from the altar, dark energy welling under them
            var floorCrack = Mats.Add(0xffa030, 0.9f, null, true, true); var under2 = Mats.Trans(0x0c0408, 0.0f, true, false);
            var cracks = new List<(Transform t, float r0, float len)>();
            for (int k = 0; k < 18; k++)
            {
                float a = k / 18f * Mathf.PI * 2 + rand * 0.2f, rr = Hv(11);
                for (int s = 0; s < 7; s++)
                {
                    float len = 4 + rand * 4; a += (rand - 0.5f) * 0.5f; var p0 = V(Mathf.Cos(a) * rr, 0.06f, Mathf.Sin(a) * rr); rr += len * 0.9f; if (rr > LB - 3) break;
                    var p1 = V(Mathf.Cos(a) * rr, 0.06f, Mathf.Sin(a) * rr); var mid = (p0 + p1) / 2; var d = p1 - p0;
                    var g = Deco(Geo.Box(0.18f, 0.02f, d.magnitude), floorCrack, mid.x, mid.y, mid.z, 0, Mathf.Atan2(d.x, d.z), 0); Ig(g); g.SetActive(false);
                    var u = Deco(Geo.Box(1.4f, 0.02f, d.magnitude), under2, mid.x, 0.05f, mid.z, 0, Mathf.Atan2(d.x, d.z), 0); Ig(u);
                    cracks.Add((g.transform, rr, d.magnitude));
                }
            }
            var petalSeam = Mats.Add(0xff8a20, 0, null, true, true);
            anims.Add((t, dt) =>
            {
                if (t0 < 0) t0 = t; float u = t - t0, k = Smoother(u / 4.5f);
                // the roof petals swing open onto the storm
                for (int i = 0; i < sanctumPetals.Count; i++) sanctumPetals[i].rx = -0.16f - Smoother(u / 3f) * 1.95f * (0.92f + 0.08f * Mathf.Sin(i * 2.3f));
                // light, fog and sky turn black and dark gold
                var bg = Color.Lerp(bg0, bgDark, k); cam.backgroundColor = bg; RenderSettings.fogColor = bg; RenderSettings.fogEndDistance = Mathf.Lerp(fogE0, fogE0 * 0.8f, k);
                sunLight.color = Color.Lerp(sunC0, sunDark, k); sunLight.intensity = Mathf.Lerp(sun0, sun0 * 0.42f, k) + flashT * 2.5f;
                RenderSettings.ambientSkyColor = Color.Lerp(amb0, Mats.Hex(0x2c2216), k); RenderSettings.ambientEquatorColor = Color.Lerp(eq0, Mats.Hex(0x1c140e), k); RenderSettings.ambientGroundColor = Color.Lerp(gr0, Mats.Hex(0x0e0906), k);
                if (sanctumSkyM) Mats.SetAlpha(sanctumSkyM, 0); if (sanctumShaftM) { Mats.SetColor(sanctumShaftM, 0xff9030); Mats.SetAlpha(sanctumShaftM, 0.08f * k); }
                foreach (var m in cloudMs) Mats.SetAlpha(m, 0.85f * k); foreach (var m in under) Mats.SetAlpha(m, (0.1f + 0.08f * Mathf.Sin(t * 2.3f)) * k + flashT * 0.4f);
                clouds.localRotation = Quaternion.Euler(0, t * 2.5f, 0);
                // the arena shakes while it changes
                if (u < 4.5f) P.shake = Mathf.Max(P.shake, 0.25f * (1 - u / 4.5f) + 0.05f);
                // cracks spread
                foreach (var c in cracks) if (!c.t.gameObject.activeSelf && c.r0 < Hv(11) + u * 14) { c.t.gameObject.SetActive(true); if (rand < 0.2f) SpawnParticles(c.t.position, 4, 3, goldSpark, 0.08f, 0.6f); }
                Mats.SetAlpha(floorCrack, 0.6f + 0.3f * Mathf.Sin(t * 3)); Mats.SetAlpha(under2, 0.55f * k); Mats.SetAlpha(crackM, 0.8f * k * (0.75f + 0.25f * Mathf.Sin(t * 5))); Mats.SetAlpha(petalSeam, k);
                // stones torn loose, floating
                foreach (var f in floaters)
                {
                    if (!f.t.gameObject.activeSelf && u > 1 + f.ph * 0.4f) { f.t.gameObject.SetActive(true); SpawnParticles(f.p, 8, 4, dustMat, 0.4f, 1.2f); }
                    if (f.t.gameObject.activeSelf) { f.t.localPosition = f.p + V(0, Mathf.Sin(t * f.sp + f.ph) * 0.8f + Mathf.Min(u, 6) * 0.3f, 0); f.t.Rotate(dt * 10 * f.sp, dt * 14 * f.sp, 0); }
                }
                // the statues break: heads fall, wings snap, the marble goes dark
                if (u > 1.5f && sanctumStatueHeads.Count > 0)
                {
                    foreach (var h in sanctumStatueHeads) if (h.t && h.t.parent != world) { h.t.SetParent(world, true); particles.Add(new Pt { m = h.t, v = V((rand - 0.5f) * 4, 2, (rand - 0.5f) * 4), life = 30, grav = GRAV, hasSpin = true, spin = Random.insideUnitSphere * 4, owned = true }); SpawnParticles(h.t.position, 20, 5, dustMat, 0.3f, 1.4f); }
                    foreach (var w in sanctumStatueWings) w.rz += (rand < 0.5f ? -1 : 1) * 0.5f;
                    sanctumStatueHeads.Clear(); Sfx("shatter", 0.7f);
                }
                foreach (var m in sanctumStatueMats) Mats.SetColor(m, Color.Lerp(Mats.Hex(0xf0ece2), Mats.Hex(0x4a4038), k));
                // golden lightning
                flashT = Mathf.Max(0, flashT - dt * 4);
                nextBolt -= dt; boltT -= dt;
                if (nextBolt <= 0 && u > 0.6f)
                {
                    nextBolt = 0.6f + rand * 1.6f; boltT = 0.18f; flashT = 1;
                    float a = rand * Mathf.PI * 2; bool col = rand < 0.6f; var end = col ? V(Mathf.Cos(Mathf.Floor(rand * 12) * Mathf.PI / 6 + Mathf.PI / 12) * Hv(50), 28, Mathf.Sin(a) * 0) : V(Mathf.Cos(a) * rand * (LB - 6), 0.2f, Mathf.Sin(a) * rand * (LB - 6));
                    if (col) { int ci = (int)(rand * 12); float ca = ci * Mathf.PI / 6 + Mathf.PI / 12; end = V(Mathf.Cos(ca) * Hv(50), 28.5f, Mathf.Sin(ca) * Hv(50)); }
                    var start = V(end.x * 0.6f + (rand - 0.5f) * 30, 62, end.z * 0.6f + (rand - 0.5f) * 30); var prev = start;
                    for (int i = 0; i < segs.Count; i++)
                    {
                        float f = (i + 1f) / segs.Count; var nx = Vector3.Lerp(start, end, f) + (i < segs.Count - 1 ? Random.insideUnitSphere * 3.5f : Vector3.zero);
                        var d = nx - prev; segs[i].localPosition = (prev + nx) / 2; segs[i].localRotation = Quaternion.LookRotation(d.normalized); segs[i].localScale = V(0.35f, 0.35f, d.magnitude + 0.2f); prev = nx;
                    }
                    SpawnParticles(end, 20, 9, goldSpark, 0.12f, 0.6f); Blast(end, 2.5f, 0xffd070); if (started) Sfx("boom", 0.35f);
                }
                Mats.SetAlpha(boltM, boltT > 0 ? (Mathf.Sin(t * 90) > -0.3f ? 1 : 0.4f) : 0);
                // feathers falling - black and gold - and waves of dark energy rolling out from the altar
                if (rand < 0.35f * k) { float a = rand * Mathf.PI * 2, rr = rand * (LB - 6); particles.Add(new Pt { pos = V(Mathf.Cos(a) * rr, 24 + rand * 10, Mathf.Sin(a) * rr), scale = V(0.28f, 0.02f, 0.1f), mat = rand < 0.6f ? ashMat : goldSpark, v = V(rand - 0.5f, -1.6f, rand - 0.5f), life = 14, grav = 0, hasSpin = true, spin = V(rand * 3, rand * 2, rand * 3) }); }
                if ((int)(u / 2.6f) != (int)((u - dt) / 2.6f) && u > 1) ShockRing(V(0, 0.3f, 0), Hv(45), 0xff9030, 1.4f);
                if (u >= 5 && u - dt < 5 && sanctumProbe) sanctumProbe.RenderProbe(); // (reflections of the corrupted Sanctum)
            });
            Sfx("boom"); whiteFlash = Mathf.Max(whiteFlash, 0.3f);
        }
    }
}
