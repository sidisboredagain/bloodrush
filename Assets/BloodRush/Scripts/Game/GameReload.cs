using UnityEngine;

namespace BR
{
    // Ammo and reloads. The revolver (6), shotgun (4 shells) and sniper (4) have magazines; the minigun runs on heat
    // and BANG never runs dry. Empty = an automatic reload, R reloads early, firing the shotgun mid-reload stops loading.
    // The left hand (the hook arm) does the work: it brings a speedloader to the swung-out cylinder, feeds shells into
    // the shotgun one by one, and swaps the sniper's magazine, while the gun tilts and flicks in the right hand.
    public partial class Game
    {
        static readonly int[] MAG = { 6, 4, -1, 4, -1 };
        readonly int[] ammo = { 6, 4, -1, 4, -1 };
        float reloadT, reloadDur, reloadDelay; int reloadW = -1, reloadShells, reloadInserted; bool[] reloadCue = new bool[8]; bool dryClicked;
        N sniperMag; GameObject propLoader, propShell, propMag;
        bool reloadHandOn; Vector3 reloadHandPos; Quaternion reloadHandRot;

        void RefillAmmo() { for (int i = 0; i < ammo.Length; i++) ammo[i] = MAG[i]; CancelReload(); }
        bool NeedsAmmo(int w) => MAG[w] > 0 && P.cheatT <= 0;
        void StartReload()
        {
            int w = P.weapon;
            if (!NeedsAmmo(w) || reloadT > 0 || ammo[w] >= MAG[w] || DE.active || P.dead || swapT > 0) return;
            inspectT = 0; P.coreCharging = false;
            reloadW = w; reloadShells = MAG[w] - ammo[w]; reloadInserted = 0;
            reloadDur = w == 0 ? 1.45f : w == 1 ? 0.4f + reloadShells * 0.34f + 0.42f : 1.75f;
            reloadT = reloadDur; for (int i = 0; i < reloadCue.Length; i++) reloadCue[i] = false;
        }
        void CancelReload()
        {
            reloadT = 0; reloadW = -1; reloadHandOn = false;
            if (revCyl != null) revCyl.px = 0;
            if (sniperMag != null) { sniperMag.py = -0.115f; sniperMag.go.SetActive(true); }
            if (propLoader) { propLoader.SetActive(false); propShell.SetActive(false); propMag.SetActive(false); }
        }
        // called after a shot: spend a round, start reloading when the magazine runs dry
        void SpendAmmo(int w)
        {
            if (!NeedsAmmo(w)) return;
            ammo[w] = Mathf.Max(0, ammo[w] - 1);
            if (ammo[w] == 0) reloadDelay = 0.32f;
        }
        // false = this weapon can't shoot right now (empty or reloading); handles the dry click and the shotgun interrupt
        bool AmmoGate(int w)
        {
            if (!NeedsAmmo(w)) return true;
            if (reloadT > 0)
            {
                if (w == 1 && ammo[1] > 0 && reloadT < reloadDur - 0.3f) { CancelReload(); return true; } // shotgun: shoot what's loaded
                return false;
            }
            if (ammo[w] <= 0) { if (!dryClicked) { Sfx("dryFire"); dryClicked = true; } StartReload(); return false; }
            dryClicked = false; return true;
        }

        void BuildReloadProps()
        {
            float PI = Mathf.PI;
            var palm = V(0, -0.06f, -0.27f);
            // revolver speedloader: six rounds in a dark ring
            propLoader = Mk.Piv(hookArm, palm.x, palm.y, palm.z).go;
            VMsh(propLoader.transform, Geo.Cyl(0.034f, 0.034f, 0.022f, 16), blackMat, 0, 0, 0.012f, PI / 2);
            VMsh(propLoader.transform, Geo.Cyl(0.012f, 0.012f, 0.02f, 8), steel, 0, 0, 0.03f, PI / 2);
            for (int i = 0; i < 6; i++) { float a = i * PI / 3; VMsh(propLoader.transform, Geo.Cyl(0.0095f, 0.0095f, 0.045f, 8), brassGun, Mathf.Cos(a) * 0.022f, Mathf.Sin(a) * 0.022f, -0.018f, PI / 2); }
            // shotgun shell: red hull, brass head
            propShell = Mk.Piv(hookArm, palm.x, palm.y, palm.z).go;
            VMsh(propShell.transform, Geo.Cyl(0.014f, 0.014f, 0.055f, 10), shellMat, 0, 0, -0.01f, PI / 2);
            VMsh(propShell.transform, Geo.Cyl(0.0148f, 0.0148f, 0.016f, 10), brassGun, 0, 0, 0.026f, PI / 2);
            // sniper magazine
            propMag = Mk.Piv(hookArm, palm.x, palm.y + 0.02f, palm.z).go;
            VMsh(propMag.transform, VR(0.06f, 0.13f, 0.1f, 0.012f), blackMat, 0, 0.03f, 0);
            VMsh(propMag.transform, VR(0.064f, 0.012f, 0.104f, 0.004f), steel, 0, -0.035f, 0);
            foreach (var g in new[] { propLoader, propShell, propMag })
            {
                Mk.SetLayerRecursive(g, Mk.LayerIgnore);
                foreach (var r in g.GetComponentsInChildren<MeshRenderer>(true)) r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
                g.SetActive(false);
            }
        }

        // the hand: from out of sight (lower left) to a point on the gun, with a little arc
        void HandTo(Vector3 worldTarget, float a, float roll = -30)
        {
            var tgt = VmRoot().InverseTransformPoint(worldTarget);
            var pocket = V(-0.34f, -0.66f, -0.42f);
            var shoulder = V(-0.38f, -0.48f, 0.05f);
            var dir = tgt - shoulder; var rot = Quaternion.LookRotation(-dir.normalized, Vector3.up) * Quaternion.Euler(0, 0, roll);
            var at = tgt - rot * V(0, -0.06f, -0.27f); // put the palm prop on the target
            a = Smoother(a);
            reloadHandPos = Vector3.Lerp(pocket, at, a) + V(0, Mathf.Sin(a * Mathf.PI) * 0.05f, 0);
            reloadHandRot = Quaternion.Slerp(Quaternion.Euler(-40, 10, -30), rot, a);
            reloadHandOn = true;
        }
        bool Cue(int i, float p, float at) { if (reloadCue[i] || p < at) return false; reloadCue[i] = true; return true; }

        void UpdateReload(float dt)
        {
            if (reloadDelay > 0) { reloadDelay -= dt; if (reloadDelay <= 0 && ammo[P.weapon] == 0) StartReload(); }
            reloadHandOn = false;
            if (reloadT <= 0) return;
            if (P.weapon != reloadW || DE.active || P.dead) { CancelReload(); return; }
            reloadT -= dt;
            float p = Mathf.Clamp01(1 - reloadT / reloadDur), t = reloadDur - reloadT;
            float env = Smoother(p / 0.12f) * Smoother((1 - p) / 0.14f);
            Vector3 dp = Vector3.zero, dr = Vector3.zero;
            propLoader.SetActive(false); propShell.SetActive(false); propMag.SetActive(false);
            if (reloadW == 0)
            {
                // tilt left and up, swing the cylinder out, flick the empties out, speedloader in, snap it shut
                dp = V(-0.07f, 0.05f, 0.06f) * env; dr = V(0.1f, 0.38f, 0.62f) * env;
                float open = Smoother((p - 0.1f) / 0.1f) * (1 - Smoother((p - 0.76f) / 0.07f));
                revCyl.px = -0.08f * open;
                float flick = Mathf.Sin(Mathf.Clamp01((p - 0.22f) / 0.16f) * Mathf.PI); dr.x -= 0.65f * flick; dp.y += 0.04f * flick;
                float snap = Mathf.Sin(Mathf.Clamp01((p - 0.76f) / 0.1f) * Mathf.PI); dr.z -= 0.45f * snap;
                if (Cue(0, p, 0.14f)) Sfx("cylOut");
                if (Cue(1, p, 0.3f)) { Sfx("click"); for (int i = 0; i < 6; i++) SpawnCasing(revCyl.t.position + Random.insideUnitSphere * 0.03f, V(rand - 0.5f, -2 - rand, rand - 0.5f) + P.vel * 0.8f, brassMat, 0.012f, 0.012f, 0.045f); }
                float reach = Mathf.Clamp01((p - 0.34f) / 0.16f) * (1 - Mathf.Clamp01((p - 0.66f) / 0.14f));
                if (reach > 0) { HandTo(revCyl.t.position + revCyl.t.TransformDirection(V(0, 0, 0.09f)), reach, -10); propLoader.SetActive(p < 0.64f); }
                if (p > 0.5f && p < 0.64f) GA.cylTarget += dt * 9;
                if (Cue(2, p, 0.56f)) { Sfx("shellIn"); ammo[0] = MAG[0]; }
                if (Cue(3, p, 0.8f)) Sfx("cylIn");
            }
            else if (reloadW == 1)
            {
                // roll the gun over to show the loading port, feed the shells one at a time, rack the pump
                float tin = 0.4f, tout = 0.42f, per = 0.34f;
                float rollIn = Smoother(t / tin), rollOut = 1 - Smoother((t - (reloadDur - tout)) / 0.3f);
                float hold = Mathf.Min(rollIn, rollOut);
                dp = V(-0.04f, 0.06f, 0.06f) * hold; dr = V(0.12f, -0.3f, -0.75f) * hold;
                float st = t - tin; int k = Mathf.FloorToInt(st / per);
                if (st > 0 && k < reloadShells)
                {
                    float u = (st - k * per) / per; // 0..1 for this shell
                    float reach = u < 0.55f ? u / 0.55f : 1 - (u - 0.55f) / 0.45f;
                    var port = ejectS.position + guns[1].g.TransformDirection(V(-0.02f, -0.07f, 0.02f));
                    HandTo(port, Mathf.Clamp01(reach * 1.25f), -70); propShell.SetActive(u < 0.6f);
                    dp.y -= Mathf.Sin(Mathf.Clamp01((u - 0.5f) / 0.2f) * Mathf.PI) * 0.012f; // the push nudges the gun
                    if (u >= 0.58f && reloadInserted < k + 1) { reloadInserted++; ammo[1] = Mathf.Min(MAG[1], ammo[1] + 1); Sfx("shellIn"); }
                }
                if (Cue(0, t, reloadDur - tout + 0.12f)) { GA.pumpT = 0.0f; GA.shellOut = true; Sfx("pump"); } // rack it
            }
            else if (reloadW == 3)
            {
                // tilt, drop the old magazine, fetch a new one, seat it, work the bolt
                dp = V(-0.05f, 0.05f, 0.04f) * env; dr = V(0.08f, 0.22f, 0.32f) * env;
                float drop = Mathf.Clamp01((p - 0.12f) / 0.12f);
                bool magGone = p >= 0.24f && p < 0.62f;
                sniperMag.go.SetActive(!magGone);
                if (p < 0.24f) sniperMag.py = -0.115f - drop * drop * 0.3f;
                else if (p >= 0.62f) sniperMag.py = -0.115f - 0.12f * (1 - Smoother((p - 0.62f) / 0.06f));
                if (Cue(0, p, 0.13f)) Sfx("magOut");
                float reach = Mathf.Clamp01((p - 0.3f) / 0.26f) * (1 - Mathf.Clamp01((p - 0.68f) / 0.14f));
                if (reach > 0) { var well = sniperG().TransformPoint(V(0, -0.115f - 0.12f * (1 - Smoother((p - 0.56f) / 0.12f)), -0.04f)); HandTo(well, reach, -20); propMag.SetActive(p < 0.62f); }
                float seat = Mathf.Sin(Mathf.Clamp01((p - 0.62f) / 0.08f) * Mathf.PI); dp.y += 0.015f * seat;
                if (Cue(1, p, 0.64f)) { Sfx("magIn"); ammo[3] = MAG[3]; }
                if (Cue(2, p, 0.72f)) { GA.boltT = 0.28f; GA.boltOut = true; } // bolt runs its rack animation (no casing)
            }
            vmN.pos += dp; vmN.SetRot(vmN.rx + dr.x, vmN.ry + dr.y, vmN.rz + dr.z);
            armHoldW = Mathf.Max(armHoldW, Mathf.Clamp01(dr.magnitude / 0.5f) * 0.85f); // keep the sleeves pointing home while it tilts
            if (reloadT <= 0) { ammo[reloadW] = MAG[reloadW]; CancelReload(); }
        }
        Transform sniperG() => guns[3].g;

        // HUD label next to the pips: count / RELOADING / R RELOAD
        string AmmoText()
        {
            int w = P.weapon;
            if (w == 2) return laserFiring ? "<color=#ffd0a0>MELTDOWN</color>" : "";
            if (!NeedsAmmo(w)) return w == 4 ? "<color=#ffd24a>INFINITE</color>" : "";
            if (reloadT > 0) return "<color=#9ad8ff>RELOADING</color>";
            if (ammo[w] == 0) return Mathf.Sin(time * 10) > 0 ? "<color=#ff4040>R  RELOAD</color>" : "<color=#902020>R  RELOAD</color>";
            return $"<color=#ffffff>{ammo[w]}</color><color=#888888> / {MAG[w]}</color>";
        }
    }
}
