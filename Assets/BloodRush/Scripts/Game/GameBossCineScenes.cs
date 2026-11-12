using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BR
{
    // The twelve boss cinematics (GameBossCine.cs plays them): the worlds they remember and the shots that film them.
    public partial class Game
    {
        void DefineBossCine(int id)
        {
            switch (id)
            {
                case 10: DefCarnifexIntro(); break;
                case 12: DefCarnifexDefeat(); break;
                case 30: DefTraitorIntro(); break;
                case 32: DefTraitorDefeat(); break;
                case 40: DefHollowIntro(); break;
                case 42: DefHollowDefeat(); break;
                case 20: DefSeraphIntro(); break;
                case 21: DefSeraphPhase2(); break;
                case 22: DefSeraphDefeat(); break;
                case 50: DefWardenIntro(); break;
                case 51: DefWardenPhase2(); break;
                case 52: DefWardenDefeat(); break;
            }
        }
        // a curtain of light: soft additive cones (god rays through windows, through clouds, down into an arena)
        Material IRays(Transform p, Vector3 from, Vector3 dir, int n, float len, float w, int color, float op, System.Random rng)
        {
            var m = IM(Mats.Add(color, op, null, true, true));
            for (int i = 0; i < n; i++)
            {
                var o = from + V(((float)rng.NextDouble() - 0.5f) * w * 3, 0, ((float)rng.NextDouble() - 0.5f) * w * 3);
                var g = IMsh(p, Geo.Cone(1, 1, 10), m, o + dir.normalized * len * 0.5f); g.transform.rotation = Quaternion.FromToRotation(Vector3.down, dir.normalized);
                g.transform.localScale = V(w * (0.6f + (float)rng.NextDouble()), len, w * (0.6f + (float)rng.NextDouble()));
            }
            return m;
        }
        // an aurora: long ribbons of light waving slowly over a night sky
        void IAurora(Transform s, Vector3 c, int n, int color)
        {
            var m = IM(Mats.Add(color, 0.12f, null, false, true));
            for (int i = 0; i < n; i++)
            {
                var piv = IRoot(s, c + V((i - n / 2f) * 90, 60 + Mathf.Sin(i) * 30, -i * 25)); for (int k = 0; k < 6; k++) IMsh(piv, Geo.Box(70, 60 + k * 14, 0.5f), m, L(piv, k * 60 - 150, 0, 0), V(0, 0, Mathf.Sin(k + i) * 12));
                iSway.Add((piv, 6, 0.25f + i * 0.04f, i, Vector3.forward));
            }
        }

        // ================= CARNIFEX, THE FLESH TITAN =================
        // The Crucible, long ago: a cavern-forge round a great furnace, where the damned were brought in chains to be burned
        // clean. Its gaoler, a giant in iron, kept the gate so that nothing would ever crawl back out. The Void came up through
        // the fire as black flame and took him - and everyone in the fire with him: their flesh is his.
        class FurnaceSet { public Transform s, voidCore; public Vector3 O, mouth, gate; public IFig gaoler; public Material fireM, fireGlow, voidSmoke, lavaM, flameM; public Light fireLight; public List<Transform> tends = new List<Transform>(); public List<IFig> damned = new List<IFig>(); }
        FurnaceSet BuildFurnace(int set)
        {
            var fs = new FurnaceSet(); var s = ISet(set); fs.s = s; var O = s.position; fs.O = O; var rng = new System.Random(150 + set);
            float R(float a, float b) => a + (b - a) * (float)rng.NextDouble();
            IArchMats();
            iMarble = IM(Mats.Std(0xffffff, 0.75f, 0, 0, IStoneTex(0x5a4a40, 0x2a2018, false, 31)));
            iStone = IM(Mats.Std(0xffffff, 0.85f, 0, 0, IStoneTex(0x3a302a, 0x1a1410, true, 32)));
            iStoneDk = IM(Mats.Std(0xffffff, 0.9f, 0, 0, IStoneTex(0x221a16, 0x0e0a08, true, 33))); ITile();
            var iron = IM(Mats.Std(0x2e2a28, 0.45f, 0.85f)); var rust = IM(Mats.Std(0x5a3020, 0.7f, 0.5f));
            fs.lavaM = IM(Mats.Add(0xff5010, 0.85f, null, true));
            IBill(s, O + V(0, 160, -760), 3200, 1300, INF(Mats.Basic(0xffffff, IGrad(0x060102, 0x4a0e04), true)));
            IA(s, Geo.Box(1400, 2, 1400), iStoneDk, O + V(0, -1, -200));
            // rivers of molten iron either side of the causeway, glowing up into the smoke
            foreach (var x in new[] { -40f, -92f, 46f, 104f }) { IMsh(s, Geo.Box(R(10, 18), 0.3f, 700), fs.lavaM, O + V(x, 0.2f, -250), V(0, R(-5, 5), 0)); IGlow(s, O + V(x, 2, R(-60, 40)), 0xff5010, 40, 0.18f); }
            // the cavern: rock walls and columns, stalactites hanging out of the dark
            for (int k = 0; k < 26; k++) { float h = R(160, 340); var p = O + V(R(-520, 520), 0, R(-760, -240)); if (Mathf.Abs(p.x - O.x) < 110) p.x += Mathf.Sign(p.x - O.x + 0.01f) * 110; IA(s, Geo.Cone(R(30, 70), h, 7), iStoneDk, p + V(0, h / 2, 0)); }
            for (int k = 0; k < 10; k++) { float h = R(200, 300); foreach (var sx in new[] { -1f, 1f }) IA(s, Geo.Cone(R(40, 70), h, 7), iStoneDk, O + V(sx * R(150, 260), h / 2, R(-200, 260))); }
            for (int k = 0; k < 34; k++) { float h = R(40, 130); IA(s, Geo.Cone(R(6, 18), h, 6), iStoneDk, O + V(R(-320, 320), 280 - h / 2, R(-520, 160)), V(180, 0, 0)); }
            // the furnace: a great round tower of black stone banded in iron, its mouth a gate of fire
            var fur = IRoot(s, O + V(0, 0, -160));
            IA(fur, Geo.Cyl(58, 68, 140, 40), iStone, L(fur, 0, 70, 0)); IA(fur, Geo.Cyl(34, 46, 90, 32), iStone, L(fur, 0, 185, 0));
            for (int k = 0; k < 5; k++) IA(fur, Geo.Torus(67 - k * 2.2f, 1.6f, 64, 6), iron, L(fur, 0, 8 + k * 30, 0), V(90, 0, 0));
            for (int k = 0; k < 16; k++) { float a = k * Mathf.PI / 8; IA(fur, Geo.Box(5, 140, 5), iron, L(fur, Mathf.Cos(a) * 65, 70, Mathf.Sin(a) * 65), V(0, -a * Mathf.Rad2Deg, 0)); }
            var smoke = ISmoke(0x1a1010, 0.55f); IChimney(set, O + V(0, 240, -160), smoke, 30, 12); IChimney(set, O + V(-20, 236, -150), smoke, 24, 10);
            fs.mouth = O + V(0, 24, -92);
            foreach (var sx in new[] { -1f, 1f }) { IA(s, Geo.Box(9, 52, 12), iron, fs.mouth + V(sx * 27, 2, 2)); IA(s, Geo.Cone(3, 8, 4), iron, fs.mouth + V(sx * 27, 32, 4)); }
            IA(s, Geo.Box(64, 8, 14), iron, fs.mouth + V(0, 26, 2)); IA(s, Geo.Box(70, 3, 16), rust, fs.mouth + V(0, 31, 2));
            fs.fireM = IM(Mats.Add(0xe8601a, 0.45f, null, true)); IMsh(s, Geo.Box(46, 48, 0.6f), fs.fireM, fs.mouth + V(0, 0, -1));
            fs.fireGlow = IDiscGlow(s, fs.mouth + V(0, 0, 8), 0xff6a20, 80, 0.035f);
            fs.flameM = IM(Mats.Add(0xffa040, 0.45f, ISoft(), true));
            for (int k = 0; k < 26; k++) { var p = IBill(s, fs.mouth + V(R(-20, 20), R(-22, 10), R(2, 6)), R(8, 16), R(10, 20), fs.flameM).transform; IDrift(set, p, V(R(-1, 1), R(6, 12), 0), fs.mouth + V(0, -4, 4), V(20, 22, 3)); }
            ISnow(set, fs.mouth + V(0, 20, 40), V(80, 40, 80), 240, 0xff8a30, 0.45f, V(0.4f, 3.5f, 0.2f), 0.9f); // embers
            ISnow(set, O + V(0, 60, 40), V(160, 60, 160), 140, 0x6a5a50, 0.6f, V(0.6f, -0.4f, 0), 0.4f);           // ash
            // chains hanging out of the dark, swaying in the heat
            for (int k = 0; k < 14; k++) { var pv = IRoot(s, O + V(R(-120, 120), 200, R(-140, 80))); for (int j = 0; j < 22; j++) IA(pv, Geo.Torus(1.0f, 0.22f, 8, 4), iron, L(pv, 0, -j * 1.9f, 0), V(0, j % 2 == 0 ? 0 : 90, 0)); IA(pv, Geo.Box(0.6f, 6, 0.6f), iron, L(pv, 0, -46, 0)); iSway.Add((pv, 3, 0.4f + k * 0.05f, k, Vector3.forward)); }
            // the causeway to the gate, posts along it, cages either side
            IA(s, Geo.Box(18, 2, 190), iStone, O + V(0, 0, 0)); foreach (var sx in new[] { -1f, 1f }) for (int k = 0; k < 9; k++) IA(s, Geo.Box(1.2f, 4, 1.2f), iron, O + V(sx * 9.2f, 3, 80 - k * 20));
            for (int k = 0; k < 6; k++) { var cg = IRoot(s, O + V((k % 2 == 0 ? -1 : 1) * 24, 0, 60 - k * 24)); for (int b = 0; b < 10; b++) { float a = b * Mathf.PI / 5; IA(cg, Geo.Box(0.25f, 7, 0.25f), iron, L(cg, Mathf.Cos(a) * 3, 3.5f, Mathf.Sin(a) * 3)); } IA(cg, Geo.Cyl(3.4f, 3.4f, 0.5f, 12), iron, L(cg, 0, 7.2f, 0)); IA(cg, Geo.Cyl(3.4f, 3.4f, 0.5f, 12), iron, L(cg, 0, 0.25f, 0)); }
            fs.gate = O + V(0, 1, -90);
            fs.gaoler = IFigure(s, O + V(-17, 0, -78), 25, FigKind.Guard, 13f, new[] { 0x6a6058, 0x8a3a22, 0x5a4a40 }, rng);
            // the damned, in chains, shuffling to the fire
            for (int k = 0; k < 11; k++) { var f = IFigure(s, O + V(R(-1.6f, 1.6f), 1, 20 - k * 9.5f), 180, k % 4 == 3 ? FigKind.Woman : FigKind.Man, R(1.65f, 1.85f), new[] { 0x7a6450, 0x8a7460, 0x5a4a3c }, rng); iWalkers.Add(new IWalker { t = f.root, f = f, v = V(0, 0, -1.5f) }); IFigPose(f, 0, 24, 34, 34, 0.15f, 1.1f, 1.1f); fs.damned.Add(f); }
            // the Void, waiting at the furnace's heart
            fs.voidCore = IMsh(s, Geo.Sphere(1, 24, 16), BCBlack(), fs.mouth + V(0, 0, 1)).transform; fs.voidCore.localScale = Vector3.zero;
            var tm = BCBlack(); for (int i = 0; i < 9; i++) { var piv = IRoot(s, fs.mouth + V(-12 + i * 3, -6 + (i % 3) * 5, 2)); IMsh(piv, Geo.Box(0.9f, 0.9f, 1), tm, L(piv, 0, 0, 0.5f)); piv.localScale = V(1, 1, 0.001f); fs.tends.Add(piv); }
            fs.voidSmoke = ISmoke(0x050208, 0f); for (int k = 0; k < 18; k++) { var p = IBill(s, fs.mouth + V(R(-30, 30), R(-10, 50), R(0, 30)), R(20, 50), R(20, 50), fs.voidSmoke).transform; IDrift(set, p, V(R(-2, 2), R(2, 5), R(0, 2)), fs.mouth + V(0, 20, 14), V(40, 40, 20)); }
            BEnv(set, 0x2a0a04, 90, 900, 0xb07050, 0xc06838, 0x4a2410, 0xffa868, 2.6f, 0x050102, V(-0.55f, -0.45f, -0.7f), 0xffe4cc, 0.25f, 1.2f); // (a warm key from in front, so the figures read; the furnace rims them)
            // the fire's own light thrown out over the causeway, the gate and the gaoler (a real light, so faces and iron catch it)
            var fl = new GameObject("furnaceLight"); fl.transform.SetParent(s, false); fl.transform.position = fs.mouth + V(0, -10, 34); fs.fireLight = fl.AddComponent<Light>(); fs.fireLight.type = LightType.Point; fs.fireLight.color = Mats.Hex(0xff8a40); fs.fireLight.range = 140; fs.fireLight.intensity = 6; fs.fireLight.shadows = LightShadows.None;
            return fs;
        }
        // the black fire rising in the furnace: the Void's core swells in the mouth, the fire goes violet-black, the light dies
        void FurnaceVoid(FurnaceSet fs, float k)
        {
            fs.voidCore.localScale = Vector3.one * Mathf.Lerp(0.001f, 11, Smoother(k));
            Mats.SetColor(fs.fireM, Color.Lerp(Mats.Hex(0xe8601a), Mats.Hex(0x3a1050), k)); Mats.SetColor(fs.flameM, Color.Lerp(Mats.Hex(0xffa040), Mats.Hex(0x5a1890), k));
            Mats.SetAlpha(fs.fireGlow, 0.035f * (1 - k * 0.7f)); if (fs.fireLight) { fs.fireLight.color = Color.Lerp(Mats.Hex(0xff8a40), Mats.Hex(0x8a50ff), k); fs.fireLight.intensity = Mathf.Lerp(6, 3.5f, k) * (0.85f + 0.15f * Mathf.PerlinNoise(BC.t * 6, 1)); } Mats.SetAlpha(fs.voidSmoke, 0.8f * Mathf.Clamp01(k * 1.6f));
            if (iLight) { iLight.color = Color.Lerp(Mats.Hex(0xff9a58), Mats.Hex(0xa070ff), k); iLight.intensity = Mathf.Lerp(1.9f, 1.2f, k) * (k > 0.1f ? 0.8f + 0.4f * Mathf.PerlinNoise(BC.t * 9, 4) : 1); }
            RenderSettings.fogColor = Color.Lerp(Mats.Hex(0x1a0604), Mats.Hex(0x0a0412), k);
        }
        // its black flames reaching out of the mouth for someone, writhing
        void FurnaceReach(FurnaceSet fs, Vector3 target, float k)
        {
            for (int i = 0; i < fs.tends.Count; i++)
            {
                var t = fs.tends[i]; var to = target + V(Mathf.Sin(BC.t * 1.7f + i) * 2.5f, (i % 3 - 1) * 2.2f, Mathf.Cos(BC.t * 1.3f + i * 2) * 2) - t.position;
                t.rotation = Quaternion.LookRotation(to.normalized) * Quaternion.Euler(Mathf.Sin(BC.t * 3 + i) * 6 * (1 - k), Mathf.Cos(BC.t * 2.4f + i) * 6 * (1 - k), 0);
                float w = 0.6f + 0.8f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(k)); t.localScale = V(w, w, Mathf.Max(0.001f, to.magnitude * Mathf.Clamp01(k * (0.8f + (i % 4) * 0.08f))));
            }
        }
        Ent CarnE() => BC.e != null && BC.e.g ? BC.e : (boss != null && boss.boss == 1 && boss.g ? boss : null);
        void DefCarnifexIntro()
        {
            var fs = BuildFurnace(0); var O = fs.O; var gp = fs.gaoler.root.position; float GH = 13;
            // 1 - the Crucible as it was: down out of the dark, through the hanging chains and the embers, to the furnace's mouth
            float s1 = BShotAt(7.0f, 0, false, (u, lt, dt) =>
            {
                FurnaceVoid(fs, 0); IFigPose(fs.gaoler, 10, 6, 10, 20, 0, 0.4f, 0.6f);
                BPath(VA(O + V(-120, 180, 320), O + V(-60, 110, 200), O + V(-14, 48, 110), O + V(8, 14, 52)), VA(O + V(0, 60, -160), O + V(0, 40, -160), O + V(0, 26, -120), fs.mouth + V(0, -6, 0)), u);
            }, 2.0f, 0.05f, null, 48, 0);
            BCue(s1 + 0.2f, "bcForge", 0.9f); BCue(s1 + 3.6f, "bcForge", 0.7f); BCue(s1 + 0.4f, "iWind", 0.35f); BCue(s1 + 1.4f, "rumble", 0.4f);
            // 2 - the damned shuffle along the causeway in chains, into the fire; the gaoler stands over the gate, watching
            float s2 = BShotAt(5.8f, 0, false, (u, lt, dt) =>
            {
                FurnaceVoid(fs, 0);
                foreach (var w in iWalkers) if (w.f != null && fs.damned.Contains(w.f) && !w.fade && w.t.position.z < fs.mouth.z + 8) w.fade = true; // (into the fire)
                IFigPose(fs.gaoler, -12 + Mathf.Sin(BC.t * 0.4f) * 8, 14, 10, 20, 0.05f, 0.4f, 0.6f);
                BPath(VA(O + V(4, 1.8f, 30), O + V(3, 2.2f, 8)), VA(O + V(0, 2.0f, -10), gp + V(0, GH * 0.85f, 0)), u, 0, 0, 0.7f);
            }, 0.05f, 0.05f, null, 44);
            for (int i = 0; i < 7; i++) BCue(s2 + 0.3f + i * 0.8f, "clank", 0.18f); BCue(s2 + 0.2f, "iChatter", 0.25f); BCue(s2 + 3.2f, "iDrone", 0.4f);
            // 3 - the fire goes black from inside: the Void swells in the furnace's heart; the gaoler turns, his shield up
            float s3 = BShotAt(5.6f, 0, false, (u, lt, dt) =>
            {
                float k = Smoother(Mathf.Clamp01((lt - 0.6f) / 4.6f)); FurnaceVoid(fs, k * 0.75f); FurnaceReach(fs, gp + V(0, GH * 0.6f, 0), k * 0.35f);
                foreach (var w in iWalkers) if (w.f != null && fs.damned.Contains(w.f)) { w.v = Vector3.Lerp(w.v, Vector3.zero, 1 - Mathf.Exp(-3 * dt)); IFigPose(w.f, 0, -18, 0, 0, -0.1f); } // they stop and look up
                float turn = Smoother(Mathf.Clamp01((lt - 1.4f) / 1.6f)); IFigPose(fs.gaoler, -60 * turn, -4, 30 + 30 * turn, 20 + 80 * turn, -0.15f * turn, 0.4f, 0.9f);
                BPath(VA(O + V(26, 6, -38), O + V(19, 7, -47)), VA(O + V(-9, 13, -84), O + V(-11, 13, -84)), u, k * 0.25f);
            }, 0.05f, 0.05f, null, 46);
            BCue(s3 + 0.5f, "iVoid", 0.8f); BCue(s3 + 0.6f, "bcForgeDark", 0.9f); BCue(s3 + 1.4f, "iWhisper", 0.8f); BCue(s3 + 2.6f, "iDistort", 0.6f); BCue(s3 + 4.0f, "iWhisper", 0.6f);
            // 4 - the black fire takes him: it wraps him, the damned are torn from the fire into his body, and he swells
            var rends = new List<(Material m, Color c)>(); var dStart = new List<(IFig f, Vector3 p)>();
            float s4 = BShotAt(6.2f, 0, false, (u, lt, dt) =>
            {
                if (rends.Count == 0) { foreach (var rr in fs.gaoler.root.GetComponentsInChildren<Renderer>()) if (rr.sharedMaterial && rr.sharedMaterial.HasProperty("_BaseColor")) rends.Add((rr.sharedMaterial, rr.sharedMaterial.GetColor("_BaseColor"))); foreach (var w in iWalkers) if (w.f != null && fs.damned.Contains(w.f)) { w.v = Vector3.zero; if (w.t.gameObject.activeSelf) dStart.Add((w.f, w.t.position)); } }
                float grab = Smoother(Mathf.Clamp01(lt / 1.4f)), grow = Smoother(Mathf.Clamp01((lt - 1.6f) / 2.6f));
                FurnaceVoid(fs, 0.75f + 0.25f * grab); FurnaceReach(fs, gp + V(0, GH * (0.5f + 0.3f * grow), 0), 0.35f + 0.65f * grab);
                var chest = gp + V(0, GH * 0.68f * (1 + 0.45f * grow), 0);
                for (int i = 0; i < dStart.Count; i++) { var (f, p0) = dStart[i]; if (!f.root) continue; float pull = Smoother(Mathf.Clamp01((lt - 1.1f - i * 0.14f) / 1.3f)); f.root.position = Vector3.Lerp(p0, chest, pull) + V(0, Mathf.Sin(pull * Mathf.PI) * 6, 0); f.root.localScale = Vector3.one * Mathf.Max(0.001f, 1 - pull); IFigPose(f, 0, -30, 120, 120, -0.3f, 0.1f, 0.1f); if (pull >= 1) f.root.gameObject.SetActive(false); }
                fs.gaoler.root.localScale = Vector3.one * (1 + 0.45f * grow);
                foreach (var (m, c) in rends) if (m) Mats.SetColor(m, Color.Lerp(c, Mats.Hex(0x7c2420), grow * 0.85f));
                IFigPose(fs.gaoler, 0, -34 * grow, 150 * grow + 40, 150 * grow + 60, -0.35f * grow, 0.2f, 0.2f, 0.45f * grow, 30 * grow, 30 * grow);
                if (lt > 3.9f && iCues.Add("taken")) { iFlashA = 0.9f; bcShake = 3; }
                var g = gp; BPath(VA(g + V(19, 4, 34), g + V(14, 5, 28), g + V(10, 7, 23)), VA(g + V(0, GH * 0.55f, 0), g + V(0, GH * 0.62f, 0), g + V(0, GH * 0.75f * (1 + 0.45f * grow), 0)), u, 0.15f + grab * 0.25f);
            }, 0.05f, 0.4f, null, 46);
            BCue(s4 + 0.2f, "iDistort", 0.9f); BCue(s4 + 1.1f, "iWhisper", 0.9f); BCue(s4 + 1.6f, "iRoarFar", 0.9f); BCue(s4 + 2.6f, "iRoarFar", 0.6f); BCue(s4 + 3.9f, "slam", 1f); BCue(s4 + 3.92f, "iCollapse", 0.9f); BCue(s4 + 3.95f, "iCrack", 0.8f);
            BSay(s4 + 0.4f, 3.2f, "I kept the fire... so that nothing would ever crawl out.", "vCarnifex");
            // 5 - the Crucible today: the arena floor splits and glows, and the titan heaves himself up out of the pit, roaring
            var cracks = new List<(Transform t, float len, float a)>(); bool burst = false, roared = false;
            float s5 = BShotAt(8.2f, -1, false, (u, lt, dt) =>
            {
                var ce = CarnE(); if (ce == null) return; var r = ce.cr; var bp = ce.g.transform.position; bp.y = ce.pos.y; var f = BCFwd(ce); var side = Vector3.Cross(Vector3.up, f);
                if (cracks.Count == 0)
                {
                    var cm = Mats.Add(0xff6010, 0.95f, null, true, true); var rr = new System.Random(11);
                    for (int i = 0; i < 12; i++) { float a = i / 12f * Mathf.PI * 2 + (float)rr.NextDouble() * 0.3f, len = 6 + (float)rr.NextDouble() * 8; var g = Mk.Msh(world, Geo.Box(0.4f + (float)rr.NextDouble() * 0.3f, 0.04f, 1), cm, bp.x, bp.y + 0.04f, bp.z); g.layer = Mk.LayerIgnore; g.transform.rotation = Quaternion.Euler(0, a * Mathf.Rad2Deg, 0); bcLive.Add(g); cracks.Add((g.transform, len, a)); }
                }
                float open = Smoother(Mathf.Clamp01((lt - 0.4f) / 1.8f)); foreach (var cr in cracks) { float Ln = cr.len * open; cr.t.localScale = V(1, 1, Mathf.Max(0.01f, Ln)); cr.t.position = bp + V(Mathf.Sin(cr.a) * Ln / 2, 0.04f, Mathf.Cos(cr.a) * Ln / 2); }
                float rise = Smoother(Mathf.Clamp01((lt - 2.0f) / 2.6f)); if (r != null && r.root != null) r.root.py = -6 * (1 - rise) + Mathf.Sin(rise * Mathf.PI) * 0.3f;
                if (lt > 2.0f && !burst) { burst = true; bcShake = 1.8f; SpawnParticles(bp + V(0, 0.3f, 0), 40, 9, dustMat, 0.4f, 1.2f, -4); SpawnParticles(bp + V(0, 0.3f, 0), 30, 12, emberMat, 0.15f, 1.0f, -6); }
                if (lt > 0.4f && lt < 4.8f && rand < 0.8f) { var cr = cracks[Random.Range(0, cracks.Count)]; float d = rand * cr.len * open; SpawnParticles(bp + V(Mathf.Sin(cr.a) * d, 0.2f, Mathf.Cos(cr.a) * d), 2, 4, rand < 0.6f ? emberMat : dustMat, 0.12f, 0.9f, -5); }
                if (lt > 4.9f && !roared) { roared = true; bcShake = 2.6f; SpawnParticles(bp + V(0, 6, 0) + f * 2, 20, 6, emberMat, 0.12f, 1.0f, 2); }
                bcShake = Mathf.Max(bcShake, lt > 0.4f && lt < 4.6f ? 0.35f : 0);
                BCAnimateBoss(ce, dt);
                BPath(VA(bp + f * 19 + side * 6 + V(0, 1.3f, 0), bp + f * 15 + side * 4 + V(0, 1.2f, 0), bp + f * 11.5f + side * 2.5f + V(0, 1.1f, 0), bp + f * 8.5f + side * 1.4f + V(0, 1.4f, 0)),
                      VA(bp + V(0, 0.4f, 0), bp + V(0, 2.4f, 0), bp + V(0, 5.4f, 0), bp + V(0, 6.6f, 0)), u, 0.05f);
            }, 0.6f, 1.0f, null, 52, 1.2f);
            BCue(s5 + 0.3f, "rumble", 0.9f); BCue(s5 + 0.4f, "iCrack", 0.8f); BCue(s5 + 1.2f, "boomSmall", 0.6f); BCue(s5 + 2.0f, "slam", 1f); BCue(s5 + 2.05f, "iCollapse", 0.8f); BCue(s5 + 3.4f, "rumble", 0.7f);
            BCue(s5 + 4.9f, "roar", 1f); BCue(s5 + 4.95f, "iRoarFar", 0.8f);
            BSay(s5 + 5.0f, 2.8f, "Now... NOTHING LEAVES.", "vCarnifex");
            BTitle(s5 + 5.5f, "CARNIFEX, THE FLESH TITAN", 2.6f);
        }
        void DefCarnifexDefeat()
        {
            var d0 = bossDeath; var bp0 = d0 != null ? d0.basePos : bcBossPos0;
            var f0 = P.pos - bp0; f0.y = 0; f0 = f0.sqrMagnitude > 1 ? f0.normalized : Vector3.forward; var side0 = Vector3.Cross(Vector3.up, f0);
            // 1 - his death as the game plays it: to his knees, magma bursting out of him, cooling to stone, crumbling, and the
            // geyser where he knelt - the camera paced to it, held until the fire has gone up
            float camU = 0, gone = -1;
            float s1 = BShotAt(11.0f, -1, true, (u, lt, dt) =>
            {
                var d = bossDeath; float target = d != null && d.e != null && d.e.boss == 1 ? Mathf.Clamp01(d.t / 6.2f) : 1;
                camU = Mathf.Lerp(camU, target, 1 - Mathf.Exp(-2.5f * dt));
                if (target >= 1 && gone < 0) gone = lt; if (gone >= 0 && lt - gone > 2.6f) BCNextShot();
                var bp = bp0; BPath(VA(bp + f0 * 26 + side0 * 10 + V(0, 1.6f, 0), bp + f0 * 19 + side0 * 5 + V(0, 2.6f, 0), bp + f0 * 17 + side0 * 1 + V(0, 3.4f, 0), bp + f0 * 34 - side0 * 6 + V(0, 9, 0)),
                                    VA(bp + V(0, 6, 0), bp + V(0, 4.5f, 0), bp + V(0, 3, 0), bp + V(0, 18, 0)), camU, 0.15f);
            }, 0.3f, 0.6f, null, 52, 0);
            BCue(s1 + 0.2f, "iDrone", 0.5f);
            // 2 - the memory: the Crucible before the Void - the fire gold and calm, the causeway empty; the gaoler alone at
            // his gate, at last kneeling, his spear laid down
            var fs = BuildFurnace(0); var gp = fs.gaoler.root.position; float GH = 13;
            float s2 = BShotAt(7.2f, 0, false, (u, lt, dt) =>
            {
                FurnaceVoid(fs, 0); foreach (var f in fs.damned) if (f.root) f.root.gameObject.SetActive(false);
                if (iLight) iLight.intensity = 1.6f; Mats.SetAlpha(fs.fireGlow, 0.03f); if (fs.fireLight) fs.fireLight.intensity = 4.5f;
                float kn = Smoother(Mathf.Clamp01((lt - 2.2f) / 2.6f)); IFigPose(fs.gaoler, 20 - 20 * kn, 18 + 10 * kn, 20 - 20 * kn, 10, 0.12f + 0.2f * kn, 0.3f, 0.5f, 0.85f * kn);
                BPath(VA(gp + V(26, 5, 20), gp + V(16, 4.5f, 26), gp + V(7, 4, 27)), VA(gp + V(0, GH * 0.7f, 0), gp + V(0, GH * 0.6f, 0), gp + V(0, GH * 0.5f, 0)), u, 0, 0, 0.6f);
            }, 1.2f, 0.05f, null, 44, 1.4f);
            BCue(s2 + 0.3f, "bcForgeSad", 0.9f); BCue(s2 + 3.4f, "bcForgeSad", 0.6f); BCue(s2 + 0.4f, "iWind", 0.25f);
            BSay(s2 + 2.4f, 3.6f, "The fire... it's finally quiet.", "vCarnifex");
            // 3 - today: where he knelt, the last of the black fire - the Void that wore him - gutters up out of the rubble and
            // is gone into the dark; the embers settle
            float wispK = 0;
            float s3 = BShotAt(7.4f, -1, false, (u, lt, dt) =>
            {
                var bp = bp0; wispK = Smoother(Mathf.Clamp01((lt - 0.8f) / 4.4f)); var wp = bp + V(0, 0.8f + wispK * 16, 0);
                float live = 1 - Smoother(Mathf.Clamp01((lt - 4.4f) / 1.4f));
                if (live > 0 && rand < 0.95f) for (int i = 0; i < 3; i++) particles.Add(new Pt { pos = wp + Random.insideUnitSphere * (0.6f + wispK), scale = Vector3.one * (0.25f + rand * 0.35f) * live, mat = BCInk, v = V((rand - 0.5f) * 0.8f, 1.6f + rand * 1.6f, (rand - 0.5f) * 0.8f), life = 1.4f, grav = 0 });
                if (rand < 0.6f) SpawnParticles(bp + V((rand - 0.5f) * 14, 8 + rand * 6, (rand - 0.5f) * 14), 1, 0.4f, emberMat, 0.08f, 3, 0.4f);
                if (rand < 0.4f) SpawnParticles(bp + V((rand - 0.5f) * 6, 0.4f, (rand - 0.5f) * 6), 1, 0.6f, ashMat, 0.3f, 2, -0.6f);
                BPath(VA(bp + f0 * 15 + side0 * 5 + V(0, 1.4f, 0), bp + f0 * 12 + side0 * 3 + V(0, 2.6f, 0), bp + f0 * 10 + side0 * 2 + V(0, 5, 0)), VA(bp + V(0, 1, 0), bp + V(0, 6, 0), bp + V(0, 20, 0)), u, 0, 0, 0.5f);
            }, 0.8f, 2.2f, null, 44, 1.2f);
            BCue(s3 + 0.6f, "iWhisper", 0.5f); BCue(s3 + 1.0f, "iSwell", 0.35f); BCue(s3 + 4.6f, "iVoid", 0.35f);
            BSay(s3 + 1.6f, 3.8f, "Was I the one in chains... all along?", "vCarnifex");
        }

        // ================= THE FROZEN TRAITOR =================
        class FrostSet { public Transform s; public Vector3 O, balcony, square; public IFig king; public Material win, darkM, smokeM, marbleCast, aurora; public Transform dome; public List<Transform> houses = new List<Transform>(); public List<IFig> guards = new List<IFig>(); }
        FrostSet BuildFrostKingdom(int set, bool forMemory)
        {
            var fs = new FrostSet(); var s = ISet(set); fs.s = s; var O = s.position; fs.O = O; var rng = new System.Random(40 + set);
            float R(float a, float b) => a + (b - a) * (float)rng.NextDouble();
            IArchMatsIce(); fs.marbleCast = iMarble;
            IBill(s, O + V(0, 200, -900), 3600, 1300, INF(Mats.Basic(0xffffff, IGrad(0x0e1830, 0x6a88b0), true)));
            IDiscGlow(s, O + V(260, 220, -860), 0xd8e8ff, 60, 0.07f, 12); IMsh(s, Geo.Sphere(26, 20, 14), INF(Mats.Basic(0xeef4ff)), O + V(260, 220, -870)); // the moon
            IAurora(s, O + V(0, 260, -700), 7, 0x50ffb0); fs.aurora = iMats[iMats.Count - 1];
            var snowG = IM(Mats.Std(0xffffff, 0.9f, 0, 0, IStoneTex(0xf2f6fa, 0xd0dce8, false, 15))); snowG.SetTextureScale("_BaseMap", new Vector2(20, 20));
            IA(s, Geo.Box(1800, 2, 1800), snowG, O + V(0, -1, -200));
            var mtn = IM(Mats.Std(0xe8eef4, 0.8f)); var rockD = IM(Mats.Std(0x4a5260, 0.9f));
            foreach (var (x, z, r, h) in new[] { (-500f, -800f, 260f, 380f), (-120f, -900f, 300f, 460f), (300f, -820f, 240f, 360f), (650f, -700f, 220f, 300f), (-780f, -650f, 200f, 280f) }) { IA(s, Geo.Cone(r, h, 9), rockD, O + V(x, h / 2, z)); IA(s, Geo.Cone(r * 0.55f, h * 0.55f, 9), mtn, O + V(x, h * 0.73f, z)); }
            // the castle: spires, the great hall, the gateway, curtain walls and bridges, the keep with its balcony
            var keep = ISpire(s, O + V(0, 0, -160), 150, 1); ISpire(s, O + V(-62, 0, -180), 96, 2); ISpire(s, O + V(62, 0, -180), 96, 3); ISpire(s, O + V(-120, 0, -240), 80, 4); ISpire(s, O + V(130, 0, -250), 84, 5);
            IBasilica(s, O + V(0, 0, -250), 34);
            IGateway(s, O + V(0, 0, -60), 0, 64, 54);
            IAqueduct(s, O + V(-170, 0, -100), O + V(-34, 0, -70), 44, 5); IAqueduct(s, O + V(34, 0, -70), O + V(170, 0, -100), 44, 5);
            var bal = IRoot(s, O + V(0, 96, -132)); IA(bal, Geo.Box(26, 2.4f, 14), iStone, L(bal, 0, -1.2f, 0)); IA(bal, Geo.Cone(12, 14, 8), iStone, L(bal, 0, -9, -1), V(180, 0, 0));
            for (int k = 0; k < 9; k++) IColumn(bal, -12 + k * 3, 6.6f, 2.4f, 0.2f); IA(bal, Geo.Box(26, 0.4f, 0.5f), iMarble, L(bal, 0, 2.5f, 6.6f));
            fs.balcony = O + V(0, 96, -132);
            var banner = IM(Mats.Std(0x2a4a8a, 0.85f, 0, 0, fabricTex, true)); var iron = IM(Mats.Std(0x3a3e44, 0.5f, 0.8f));
            foreach (var x in new[] { -24f, 24f }) IBanner(s, O + V(x, 0, -50), 22, banner, iron, 1.4f);
            foreach (var x in new[] { -11f, 11f }) IBanner(bal, fs.balcony + V(x, 0, 4), 9, banner, iron, 1.6f);
            fs.win = IM(Mats.Add(0xffa040, 0.9f, null, true));
            var chimSmoke = ISmoke(0xc8ccd4, 0.35f);
            for (int k = 0; k < 110; k++)
            {
                var p = O + V(R(-320, 320), 0, R(-40, 380)); if (Mathf.Abs(p.x - O.x) < 22) continue; if (p.z - O.z > 70 && p.z - O.z < 170 && Mathf.Abs(p.x - O.x) < 60) continue; // (the main street, the square)
                float w = R(9, 16), d = R(8, 13), h = R(8, 15);
                var hs = IHouse(s, p, Mathf.Round(R(0, 4)) * 90 + R(-8, 8), w, d, h);
                for (int j = 0; j < Mathf.Max(1, (int)(w / 4)); j++) if (rng.NextDouble() < 0.7) IWindow(hs, -w / 2 + 2 + j * 4, h * 0.55f, d / 2 + 0.12f, 1.0f, h * 0.3f, fs.win);
                if (rng.NextDouble() < 0.25) IChimney(set, hs.position + V(0, h + 4, 0), chimSmoke, 3, 2);
                fs.houses.Add(hs);
            }
            for (int k = 0; k < 12; k++) foreach (var sx in new[] { -1f, 1f }) ILantern(s, O + V(sx * 16, 0, -30 + k * 30), iron);
            // the square in the town: a fountain frozen solid, a ring of lanterns
            fs.square = O + V(0, 0, 120);
            IA(s, Geo.Cyl(14, 14, 1.4f, 32), iStone, fs.square + V(0, 0.7f, 0)); IA(s, Geo.Cyl(2, 2.6f, 6, 12), iMarble, fs.square + V(0, 3, 0)); IStatue(IRoot(s, fs.square + V(0, 6, 0)), 0, 0, 0, 7);
            fs.king = IFigure(s, forMemory ? fs.square + V(4, 0, 18) : fs.balcony + V(0, 0, 2), forMemory ? 180 : 0, FigKind.King, 2.25f, new[] { 0x1a2a4a, 0x2a3a6a, 0x3a4a7a }, rng, false, iGold2);
            ICrowd(s, fs.square + V(0, 0, 10), 24, 70, forMemory ? 60 : 44, rng, true, 0.9f, new[] { 0x3a4a6a, 0x5a3a3a, 0x4a5a4a, 0x6a5a40, 0x3a3a4a, 0x7a6a60 });
            for (int k = 0; k < 10; k++) { var g = IFigure(s, O + V(-22 + k * 4.9f, 0, -34), 180, FigKind.Guard, 1.95f, new[] { 0x3a4a5a }, rng); iWalkers.Add(new IWalker { t = g.root, f = g, v = Vector3.zero }); fs.guards.Add(g); }
            IBirdFlock(s, O + V(0, 120, -120), 10, 70, 0x1a1e26, 1.6f);
            fs.darkM = BCBlack(); fs.dome = IMsh(s, Geo.Sphere(1, 32, 16, 0, Mathf.PI * 2, 0, Mathf.PI / 2), fs.darkM, O + V(-240, 0, 300)).transform; fs.dome.localScale = Vector3.zero;
            fs.smokeM = ISmoke(0x050308, 0f);
            for (int k = 0; k < 30; k++) { var p = IBill(s, O + V(R(-420, -40), R(10, 140), R(160, 440)), R(70, 170), R(60, 150), fs.smokeM).transform; IDrift(set, p, V(R(4, 10), R(1, 3), R(-6, -2)), O + V(-200, 60, 300), V(260, 70, 160)); }
            ISnow(set, O + V(0, 60, 120), V(220, 60, 260), 460, 0xffffff, 0.9f, V(-1.2f, -2.4f, 0));
            ISnow(set, fs.balcony, V(40, 20, 40), 140, 0xffffff, 0.35f, V(-2, -1.6f, 0));
            var mist = ISmoke(0xd8e4f0, 0.25f); for (int k = 0; k < 16; k++) { var p = IBill(s, O + V(R(-300, 300), R(1, 6), R(-60, 380)), R(60, 140), R(10, 20), mist).transform; IDrift(set, p, V(R(1, 3), 0, 0), O + V(0, 3, 160), V(320, 3, 240)); }
            IRays(s, O + V(0, 140, -150), V(0.25f, -1, 0.5f), 6, 180, 6, 0xffc888, 0.035f, rng);
            BEnv(set, 0x8aa0c0, 80, 900, 0x5a6a90, 0x4a5868, 0x2a3038, 0xffd8b0, 1.25f, 0x0e1830, V(-0.5f, -0.45f, -0.75f), 0xe8f0ff, 0.1f, 1.0f);
            return fs;
        }
        void FrostDarken(FrostSet fs, float k)
        {
            fs.dome.localScale = V(1, 0.62f, 1) * Mathf.Lerp(0.01f, 440, k);
            Mats.SetAlpha(fs.smokeM, 0.78f * Mathf.Clamp01(k * 2)); Mats.SetAlpha(fs.aurora, 0.12f * (1 - Mathf.Clamp01(k * 2.5f)));
            Mats.SetAlpha(fs.win, 0.9f * (1 - Mathf.Clamp01((k - 0.2f) * 1.6f)));
            Mats.SetColor(fs.marbleCast, Color.Lerp(Color.white, Mats.Hex(0x40485a), k));
            float rr = Mathf.Lerp(0, 540, k); var dc = V(fs.dome.position.x, 0, fs.dome.position.z);
            foreach (var h in fs.houses) if (h && h.gameObject.activeSelf && Vector3.Distance(V(h.position.x, 0, h.position.z), dc) < rr * 0.9f) h.gameObject.SetActive(false);
            foreach (var w in iWalkers) if (w.t && w.t.gameObject.activeSelf && !w.fade && Vector3.Distance(V(w.t.position.x, 0, w.t.position.z), dc) < rr * 0.95f) w.fade = true;
            if (iLight) iLight.intensity = Mathf.Lerp(1.25f, 0.35f, k);
            RenderSettings.fogColor = Color.Lerp(Mats.Hex(0x8aa0c0), Mats.Hex(0x10141c), k);
        }
        void DefTraitorIntro()
        {
            var fs = BuildFrostKingdom(0, false); var O = fs.O; var k0 = fs.balcony;
            // the ancient chamber: carved pillars round a source of Void energy, braziers of blue fire, frost on the floor
            var ch = ISet(1); var C0 = ch.position; var rng = new System.Random(99);
            IArchMatsIce(); var dkIce = IM(Mats.Std(0xffffff, 0.45f, 0.1f, 0, IStoneTex(0x4a5a6c, 0x2a3442, true, 16)));
            IA(ch, Geo.Cyl(48, 48, 2, 48), dkIce, C0 + V(0, -1, 0));
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; var col = IRoot(ch, C0 + V(Mathf.Cos(a) * 36, 0, Mathf.Sin(a) * 36)); IColumn(col, 0, 0, 46, 3.2f); IStatue(col, 0, 46, 0, 6, -a * Mathf.Rad2Deg - 90, false); }
            for (int k = 0; k < 12; k++) { float a = (k + 0.5f) * Mathf.PI / 6; var pan = IRoot(ch, C0 + V(Mathf.Cos(a) * 38, 0, Mathf.Sin(a) * 38), -a * Mathf.Rad2Deg + 90); IA(pan, Geo.Box(14, 30, 1.4f), dkIce, L(pan, 0, 15, 0)); for (int j = 0; j < 6; j++) IA(pan, Geo.Box(9 - (j % 2) * 3, 1.0f, 0.5f), iMarble, L(pan, 0, 4 + j * 4.4f, 0.8f)); IA(pan, Geo.Torus(3, 0.3f, 24, 4), iMarble, L(pan, 0, 20, 0.9f), pan.eulerAngles); }
            var rune = IM(Mats.Add(0x5aa8ff, 0.5f, null, true)); IMsh(ch, Geo.Ring(13, 14, 64), rune, C0 + V(0, 0.08f, 0), V(-90, 0, 0)); IMsh(ch, Geo.Ring(8.6f, 9.2f, 64), rune, C0 + V(0, 0.08f, 0), V(-90, 0, 0));
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; IMsh(ch, Geo.Box(1.2f, 0.05f, 2.6f), rune, C0 + V(Mathf.Cos(a) * 11.3f, 0.08f, Mathf.Sin(a) * 11.3f), V(0, -a * Mathf.Rad2Deg, 0)); }
            var iron = IM(Mats.Std(0x2a2e34, 0.5f, 0.8f));
            for (int k = 0; k < 6; k++) { float a = k * Mathf.PI / 3 + 0.3f; var bp = C0 + V(Mathf.Cos(a) * 26, 0, Mathf.Sin(a) * 26); IA(ch, Geo.Cyl(0.5f, 1.2f, 5, 8), iron, bp + V(0, 2.5f, 0)); IA(ch, Geo.Cyl(1.6f, 1.0f, 0.8f, 10), iron, bp + V(0, 5.2f, 0)); IGlow(ch, bp + V(0, 6.4f, 0), 0x60b0ff, 7, 0.6f); ISnow(1, bp + V(0, 8, 0), V(1, 2, 1), 10, 0x80c8ff, 0.5f, V(0, 2.5f, 0)); }
            var core = IMsh(ch, Geo.Sphere(3, 24, 16), BCBlack(), C0 + V(0, 7, 0)).transform;
            var vg = IDiscGlow(ch, C0 + V(0, 7, 0), 0x6a30c0, 12, 0.05f, 14);
            var vsm = ISmoke(0x08020c, 0.75f); var vparts = new List<Transform>(); for (int i = 0; i < 22; i++) vparts.Add(IBill(ch, C0 + V(0, 7, 0), 3, 3, vsm).transform);
            var tendril = BCBlack(); var tends = new List<Transform>(); for (int i = 0; i < 10; i++) { var piv = IRoot(ch, C0 + V(0, 7, 0), i * 36); IMsh(piv, Geo.Box(0.35f, 0.35f, 1), tendril, L(piv, 0, 0, 0.5f)); tends.Add(piv); }
            var ices = new List<Transform>(); var iceM = IM(Mats.Std(0xc8e8ff, 0.08f, 0.2f, 0x10304a));
            for (int i = 0; i < 90; i++) { float a = (float)rng.NextDouble() * Mathf.PI * 2, r = 3 + (float)rng.NextDouble() * 38; var g = IA(ch, Geo.Cone(1.0f + (float)rng.NextDouble() * 1.8f, 1, 5), iceM, C0 + V(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), V((float)rng.NextDouble() * 34 - 17, 0, (float)rng.NextDouble() * 34 - 17)).transform; g.localScale = V(1, 0.001f, 1); ices.Add(g); }
            var mist = ISmoke(0x8aa8c8, 0.3f); for (int i = 0; i < 12; i++) { var p = IBill(ch, C0 + V(Mathf.Sin(i) * 24, 0.8f, Mathf.Cos(i) * 24), 26, 5, mist).transform; IDrift(1, p, V(1.5f, 0, 0.5f), C0 + V(0, 0.8f, 0), V(30, 0.5f, 30)); }
            ISnow(1, C0 + V(0, 20, 0), V(36, 20, 36), 140, 0xa0c8ff, 0.3f, V(0.2f, -0.5f, 0));
            IRays(ch, C0 + V(0, 60, 0), V(0.15f, -1, 0.1f), 3, 60, 4, 0x6a8ac0, 0.05f, rng);
            var king2 = IFigure(ch, C0 + V(0, 0, 34), 180, FigKind.King, 2.25f, new[] { 0x1a2a4a, 0x2a3a6a, 0x3a4a7a }, rng, false, iGold2);
            BEnv(1, 0x0a0c14, 20, 160, 0x2a3048, 0x1a1e2a, 0x08080c, 0x8aa0d0, 0.8f, 0x020204, V(0.3f, -0.8f, -0.5f), 0xd0dcff, 0.1f, 1.3f);
            var e = BC.e; float H = 26;
            // 1 - the kingdom before the fall: from beyond the mountains, over the town's roofs and smoke, up to the castle
            float s1 = BShotAt(7.0f, 0, false, (u, lt, dt) =>
            {
                BPath(VA(O + V(-60, 260, 760), O + V(-20, 120, 420), O + V(30, 50, 230), O + V(14, 70, 40), k0 + V(12, 8, 60)),
                      VA(O + V(0, 80, 0), O + V(0, 40, 120), O + V(0, 40, 0), O + V(0, 90, -120), k0 + V(0, 4, 0)), u);
                IFigPose(fs.king, 0, 6, 0, 0);
            }, 2.2f, 0.05f, null, 46, 0);
            BCue(s1 + 0.1f, "iWind", 0.6f); BCue(s1 + 0.3f, "bcFrost", 0.9f); BCue(s1 + 3.3f, "bcFrost", 0.6f); BCue(s1 + 1.5f, "iBell", 0.5f); BCue(s1 + 3.0f, "iBell", 0.35f); BCue(s1 + 1.0f, "iChatter", 0.5f); BCue(s1 + 4.5f, "iDrone", 0.35f);
            // 2 - the king, alone and watchful: an arc round him on his balcony, his cape in the wind
            float s2 = BShotAt(4.0f, 0, false, (u, lt, dt) =>
            {
                var kp = fs.king.root.position;
                BPath(VA(kp + V(6, 2.0f, 6), kp + V(3, 1.6f, 7.5f), kp + V(-1, 1.7f, 6.5f)), VA(kp + V(0, 1.9f, 0), kp + V(0, 2.0f, 0)), u);
                IFigPose(fs.king, 0, 4, 0, 0, 0.05f); fs.king.wind = 1.6f;
            }, 0.05f, 0.05f, null, 40);
            BCue(s2 + 0.2f, "iWind", 0.7f);
            // 3 - the approaching darkness: over his shoulder, out over the town to where the black rises on its edge
            float s3 = BShotAt(4.6f, 0, false, (u, lt, dt) =>
            {
                var kp = fs.king.root.position;
                BPath(VA(kp + V(-5, 3.2f, -4), kp + V(-4.2f, 2.9f, -2.4f)), VA(kp + V(-24, -4, 60), O + V(-210, 20, 300)), u, 0.03f);
                float turn = Smoother(Mathf.Clamp01((lt - 1.0f) / 1.4f)); IFigPose(fs.king, -36 * turn, 6 * turn, 0, 0, 0.05f); fs.king.wind = 1.6f + turn;
                FrostDarken(fs, Smoother(Mathf.Clamp01(lt / 4.6f)) * 0.45f);
            }, 0.05f, 0.05f, null, 44);
            BCue(s3 + 0.5f, "rumble", 0.8f); BCue(s3 + 1.6f, "iCollapse", 0.6f); BCue(s3 + 2.6f, "iRoarFar", 0.45f); BCue(s3 + 0.2f, "bcFrostDark", 0.8f);
            // 4 - the flight: down in the street, the people running for the castle, the guards waving them in
            float s4 = BShotAt(4.2f, 0, false, (u, lt, dt) =>
            {
                FrostDarken(fs, 0.45f + Smoother(u) * 0.2f);
                foreach (var w in iWalkers) if (w.f != null && w.f.h < 1.95f && !w.fade) { var to = (O + V(0, 0, -40)) - w.t.position; to.y = 0; w.v = Vector3.Lerp(w.v, to.normalized * 4.8f, 1 - Mathf.Exp(-2 * dt)); IFigPose(w.f, 0, -4, 20, 30, 0.2f); }
                foreach (var g in fs.guards) IFigPose(g, 0, 0, 70 + Mathf.Sin(BC.t * 6 + g.root.position.x) * 35, 40, 0, 0.6f, 0.3f);
                BPath(VA(fs.square + V(-12, 1.5f, -40), fs.square + V(-6, 1.7f, -70)), VA(fs.square + V(0, 2, -10), O + V(0, 8, -40)), u, 0.25f);
            }, 0.05f, 0.05f, null, 50);
            BCue(s4 + 0.1f, "iChatter", 0.9f); BCue(s4 + 0.6f, "iCollapse", 0.7f); BCue(s4 + 1.8f, "rumble", 0.7f);
            // 5 - his face turns to the dark; the wind tears at him
            float s5 = BShotAt(3.0f, 0, false, (u, lt, dt) =>
            {
                FrostDarken(fs, 0.65f); var kp = fs.king.root.position;
                BPath(VA(kp + V(-2.6f, 2.0f, 2.4f), kp + V(-1.6f, 2.1f, 1.8f)), VA(kp + V(0, 2.0f, 0), kp + V(0, 2.02f, 0)), u, 0.04f);
                IFigPose(fs.king, -40, -2, 0, 0, 0.1f); fs.king.wind = 3;
            }, 0.05f, 0.4f, null, 34);
            BCue(s5 + 0.2f, "iWind", 1f); BCue(s5 + 1.6f, "iBeat", 0.7f);
            // 6 - the desperate decision: into the chamber; he walks to the Void, hesitates, reaches... and touches it
            float s6 = BShotAt(7.2f, 1, false, (u, lt, dt) =>
            {
                float walk = Mathf.Clamp01(lt / 2.6f); king2.root.position = C0 + V(0, 0, Mathf.Lerp(34, 8.4f, Smoother(walk))); king2.speed = walk < 1 ? 1.1f : 0;
                for (int i = 0; i < vparts.Count; i++) { float a = BC.t * (1.2f + i * 0.07f) + i; vparts[i].position = C0 + V(Mathf.Cos(a) * (3.4f + (i % 4)), 7 + Mathf.Sin(a * 1.3f) * 2.6f, Mathf.Sin(a) * (3.4f + (i % 4))); }
                for (int i = 0; i < tends.Count; i++) { tends[i].localRotation = Quaternion.Euler(Mathf.Sin(BC.t * 1.3f + i) * 40, i * 36 + BC.t * 20, 0); tends[i].localScale = V(1, 1, 4 + Mathf.Sin(BC.t * 2 + i * 1.7f) * 3); }
                float reach = Smoother(Mathf.Clamp01((lt - 3.2f) / 1.8f)), hes = lt > 4.0f && lt < 4.6f ? 0.72f : 1; // a pause before the touch
                IFigPose(king2, 0, -14 + 6 * reach, 75 * Mathf.Min(reach, hes), 8, 0.12f + reach * 0.1f, 0.15f, 0.3f);
                if (lt < 3.0f) BPath(VA(C0 + V(18, 6, 54), C0 + V(12, 5, 38), C0 + V(7, 4.2f, 24)), VA(C0 + V(0, 6, 0), C0 + V(0, 5, 20), king2.root.position + V(0, 1.6f, 0)), lt / 3.0f);
                else if (lt < 5.3f) BPath(VA(king2.root.position + V(-2.6f, 2.0f, 2.2f), king2.root.position + V(-1.8f, 1.9f, 1.4f)), VA(king2.root.position + V(0, 1.6f, -1.2f), king2.root.position + V(0, 1.7f, -1.8f)), (lt - 3.0f) / 2.3f);
                else BPath(VA(king2.root.position + V(3.2f, 2.6f, 3.4f), king2.root.position + V(5, 4, 6)), VA(king2.root.position + V(0, 2, -1.5f), C0 + V(0, 6, 0)), (lt - 5.3f) / 1.9f, lt > 5.3f ? 1.4f : 0);
                if (lt > 5.3f)
                {
                    if (iCues.Add("touch")) { iFlashA = 1.0f; bcShake = 3; }
                    float g = Mathf.Clamp01((lt - 5.3f) / 1.3f); foreach (var ic in ices) { float d = Vector3.Distance(V(ic.position.x, 0, ic.position.z), V(C0.x, 0, C0.z)); float local = Smoother(Mathf.Clamp01(g * 1.6f - d / 50f)); ic.localScale = V(1, Mathf.Lerp(0.001f, 4 + (ic.position.x * 13 % 7), local), 1); }
                    core.localScale = Vector3.one * (1 + g * 2.5f); Mats.SetAlpha(vg, 0.05f + g * 0.1f); if (iLight) { iLight.color = Mats.Hex(0xc8e8ff); iLight.intensity = 0.8f + g * 2.5f; }
                    Mats.SetAlpha(rune, 0.5f + g * 0.5f);
                }
            }, 0.6f, 0.05f, null, 46, 1.0f);
            BCue(s6 + 0.1f, "iCreak", 0.6f); BCue(s6 + 0.5f, "iWhisper", 0.8f); BCue(s6 + 2.0f, "iWhisper", 0.6f); BCue(s6 + 3.4f, "iBeat", 0.8f); BCue(s6 + 4.3f, "iBeat", 0.9f);
            BCue(s6 + 5.3f, "timeStop", 1f); BCue(s6 + 5.3f, "shatter", 1f); BCue(s6 + 5.35f, "iCrack", 1f); BCue(s6 + 5.5f, "iIce", 1f);
            // 7 - the fallen king: the present-day arena, quiet and cold; the floor cracks; ice bursts up; he rises
            var arenaIce = new List<Transform>();
            float s7 = BShotAt(7.4f, -1, false, (u, lt, dt) =>
            {
                if (e == null || !e.g) return;
                var f = BCFwd(e); var side = Vector3.Cross(Vector3.up, f); var bp = bcBossPos0;
                if (arenaIce.Count == 0)
                {
                    if (tBlock) { tBlock.SetActive(false); }
                    var im = Mats.Std(0xbfe4ff, 0.08f, 0.2f, 0x10304a); var rr = new System.Random(5);
                    for (int i = 0; i < 40; i++) { float a = (float)rr.NextDouble() * Mathf.PI * 2, r = 4 + (float)rr.NextDouble() * 14; var g = Mk.Msh(world, Geo.Cone(0.8f + (float)rr.NextDouble() * 1.2f, 1, 5), im, bp.x + Mathf.Cos(a) * r, bp.y, bp.z + Mathf.Sin(a) * r); g.transform.rotation = Quaternion.Euler((float)rr.NextDouble() * 40 - 20, 0, (float)rr.NextDouble() * 40 - 20); g.transform.localScale = V(1, 0.001f, 1); bcLive.Add(g); arenaIce.Add(g.transform); }
                }
                if (lt > 1.4f && iCues.Add("prison")) { var tb = tBlock ? tBlock.transform.position : bp; IceShards(tb + V(0, 6, 0), 50, 6, 14, Vector3.zero, 1.4f); ShockRing(bp + V(0, 0.2f, 0), 16, 0xc8f0ff, 0.6f); bcShake = 2; }
                float rise = Smoother(Mathf.Clamp01((lt - 1.6f) / 3.2f));
                e.g.transform.position = bp + V(0, -9 * (1 - rise), 0);
                if (e.tr != null && e.tr.head != null) e.tr.head.rx = Mathf.Lerp(0.7f, -0.15f, Smoother(Mathf.Clamp01((lt - 4.0f) / 1.4f)));
                for (int i = 0; i < arenaIce.Count; i++) { float d = (arenaIce[i].position - bp).magnitude; arenaIce[i].localScale = V(1, Mathf.Lerp(0.001f, 3 + (i % 5) * 1.8f, Smoother(Mathf.Clamp01((lt - 1.2f - d * 0.04f) / 0.5f))), 1); }
                if (rand < 0.9f) SpawnParticles(bp + V((rand - 0.5f) * 16, rand * 3, (rand - 0.5f) * 16), 2, 3, rand < 0.5f ? cyanMat : dustMat, 0.12f, 1.4f, -1);
                BPath(VA(bp + f * 30 + side * 10 + V(0, 0.6f, 0), bp + f * 22 + side * 6 + V(0, 0.9f, 0), bp + f * 16 + side * 2.5f + V(0, 1.3f, 0)), VA(bp + V(0, 1, 0), bp + V(0, H * 0.3f, 0), bp + V(0, H * 0.64f, 0)), u, lt > 1.4f && lt < 4.5f ? 0.45f : 0.06f);
            }, 0.05f, 1.0f, null, 50, 1.6f, true);
            BCue(s7 + 0.1f, "iWind", 0.5f); BCue(s7 + 1.0f, "iCrack", 0.7f); BCue(s7 + 1.4f, "shatter", 1f); BCue(s7 + 1.45f, "iIce", 0.9f); BCue(s7 + 1.6f, "rumble", 1f); BCue(s7 + 4.2f, "iBeat", 0.8f);
            BSay(s7 + 3.0f, 3.2f, "I did what I had to do... They would have all perished.", "vTraitor");
            BTitle(s7 + 4.6f, "THE FROZEN TRAITOR", 2.7f);
        }
        void DefTraitorDefeat()
        {
            var e = BC.e;
            float s1 = BShotAt(4.6f, -1, true, (u, lt, dt) =>
            {
                if (e == null || !e.g) return; var bp = e.g.transform.position; var f = BCFwd(e); var side = Vector3.Cross(Vector3.up, f);
                BPath(VA(bp + f * 28 + side * 12 + V(0, 5, 0), bp + f * 22 + side * 6 + V(0, 3, 0), bp + f * 18 + side * 2 + V(0, 2.2f, 0)), VA(bp + V(0, 11, 0), bp + V(0, 6, 0), bp + V(0, 3.5f, 0)), u, 0.15f);
            }, 0.3f, 0.05f, null, 50, 0);
            BCue(s1 + 0.5f, "iWind", 0.7f); BCue(s1 + 2.4f, "bcFrostSad", 0.9f);
            // the stillness after: snow falling on him; the camera drifting in
            float s2 = BShotAt(3.0f, -1, false, (u, lt, dt) =>
            {
                if (e == null || !e.g) return; var bp = e.g.transform.position; var f = BCFwd(e); var side = Vector3.Cross(Vector3.up, f);
                if (rand < 0.9f) SpawnParticles(bp + V((rand - 0.5f) * 30, 18, (rand - 0.5f) * 30), 2, 0.5f, SnowMat, 0.15f, 4, -1.5f);
                BPath(VA(bp + f * 34 + side * -8 + V(0, 10, 0), bp + f * 26 + side * -5 + V(0, 7, 0)), VA(bp + V(0, 2, 0), bp + V(0, 2.5f, 0)), u);
            }, 0.05f, 0.05f, null, 44);
            // a memory of what was lost: the king among his people in the square... then the darkness takes it all
            var fs = BuildFrostKingdom(0, true); var O = fs.O;
            float s3 = BShotAt(8.6f, 0, false, (u, lt, dt) =>
            {
                var kp = fs.king.root.position;
                float walk = Mathf.Clamp01(lt / 3.0f); fs.king.root.position = fs.square + V(4, 0, 18) + V(0, 0, -6 * Smoother(walk)); fs.king.speed = walk < 1 ? 0.9f : 0;
                float look = lt < 3 ? Mathf.Sin(BC.t * 0.8f) * 35 : -20 + Mathf.Sin(BC.t * 0.5f) * 10; IFigPose(fs.king, look, 6, lt > 3.2f && lt < 5 ? 55 : 10, 10, 0, 0.6f);
                float dark = Smoother(Mathf.Clamp01((lt - 4.4f) / 3.6f)); FrostDarken(fs, dark);
                if (dark > 0.05f) foreach (var w in iWalkers) if (w.f != null && w.f != fs.king && !w.fade) { w.v = Vector3.Lerp(w.v, Vector3.zero, 1 - Mathf.Exp(-3 * dt)); IFigPose(w.f, 0, -10, 70, 70, -0.1f, 0.1f, 0.1f, 0, 20, 20); }
                if (dark > 0.7f) { IFigPose(fs.king, -30, -6, 100, 60, 0.15f, 0.05f, 0.2f); }
                if (dark > 0.92f && fs.king.root.gameObject.activeSelf && iCues.Add("kingFade")) { var kw = new IWalker { t = fs.king.root, f = fs.king, fade = true }; iWalkers.Add(kw); }
                if (lt < 4.4f) BPath(VA(kp + V(7, 2.8f, 15), kp + V(4.5f, 2.3f, 8.5f), kp + V(3.2f, 2.0f, 5.5f)), VA(kp + V(0, 1.6f, 0), kp + V(0, 1.9f, 0), kp + V(0, 2.0f, 0)), lt / 4.4f);
                else BPath(VA(kp + V(3.2f, 2.0f, 5.5f), kp + V(6, 4, 14), kp + V(12, 9, 30)), VA(kp + V(0, 2, 0), kp + V(-10, 3, -20), fs.dome.position + V(0, 40, 0)), (lt - 4.4f) / 4.2f, dark * 0.3f);
            }, 0.05f, 0.9f, null, 44, 1.2f);
            BCue(s3 + 0.2f, "iSwell", 0.5f); BCue(s3 + 0.4f, "iChatter", 0.6f); BCue(s3 + 1.6f, "iBell", 0.4f); BCue(s3 + 4.4f, "iVoid", 0.7f); BCue(s3 + 5.5f, "iWhisper", 0.6f); BCue(s3 + 6.6f, "iWind", 0.7f);
            // the realisation: back in the snow, his hand reaching to the ground as the last of the frost light leaves him
            float s4 = BShotAt(6.0f, -1, false, (u, lt, dt) =>
            {
                if (e == null || !e.g) return; var r = e.tr;
                Vector3 hp = r != null && r.rHand != null ? r.rHand.t.position : e.g.transform.position + V(0, 2, 0);
                if (r != null && r.rSh != null) { r.rSh.rx = Mathf.Lerp(r.rSh.rx, -0.5f, 1 - Mathf.Exp(-0.9f * dt)); if (r.rEl != null) r.rEl.rx = Mathf.Lerp(r.rEl.rx, -0.2f, 1 - Mathf.Exp(-0.9f * dt)); }
                if (r != null && r.glow != null) Mats.SetColor(r.glow, Color.Lerp(Mats.Hex(0x8ad8ff), Color.black, Smoother(Mathf.Clamp01(lt / 4.5f))));
                var f = BCFwd(e); var side = Vector3.Cross(Vector3.up, f); var body = e.g.transform.position + V(0, 6, 0);
                BPath(VA(hp + f * 9 + side * 4 + V(0, 2.5f, 0), hp + f * 5.5f + side * 2 + V(0, 1.6f, 0), hp + f * 11 + side * -3 + V(0, 6, 0)), VA(hp, hp + V(0, 0.3f, 0), body), u);
                if (rand < 0.35f) SpawnParticles(hp + Random.insideUnitSphere * 1.5f, 1, 0.6f, cyanMat, 0.08f, 1.6f, -0.5f);
                if (rand < 0.7f) SpawnParticles(hp + V((rand - 0.5f) * 14, 10, (rand - 0.5f) * 14), 1, 0.4f, SnowMat, 0.12f, 4, -1.2f);
            }, 0.05f, 0.05f, null, 44);
            BSay(s4 + 1.4f, 3.6f, "All these years... and I saved no one.", "vTraitor");
            BCue(s4 + 0.2f, "iWind", 0.4f);
            // the last image: high and still over the arena, the snow falling, the dark closing in
            float s5 = BShotAt(3.4f, -1, false, (u, lt, dt) =>
            {
                if (e == null || !e.g) return; var bp = e.g.transform.position; var f = BCFwd(e);
                BPath(VA(bp + f * 20 + V(0, 16, 0), bp + f * 26 + V(0, 26, 0)), VA(bp, bp), u);
                if (rand < 0.9f) SpawnParticles(bp + V((rand - 0.5f) * 40, 20, (rand - 0.5f) * 40), 2, 0.4f, SnowMat, 0.15f, 4, -1.5f);
            }, 0.05f, 2.2f, null, 50);
        }

        // ================= THE HOLLOW KING =================
        class CitySet { public Transform s; public Vector3 O, steps; public IFig king; public List<IFall> falls; public Material sky, smokeM, fireM, sunG; public List<Transform> fires = new List<Transform>(); public List<IFig> guards = new List<IFig>(); }
        CitySet BuildHollowCity(int set)
        {
            var cs = new CitySet(); var s = ISet(set); cs.s = s; var O = s.position; cs.O = O; var rng = new System.Random(70 + set);
            float R(float a, float b) => a + (b - a) * (float)rng.NextDouble();
            IArchMats();
            cs.sky = INF(Mats.Basic(0xffffff, IGrad(0x2a3a6a, 0xf0c890), true)); IBill(s, O + V(0, 200, -900), 3600, 1300, cs.sky);
            cs.sunG = IDiscGlow(s, O + V(-160, 150, -820), 0xffe0a0, 220, 0.05f);
            var pave = IM(Mats.Std(0xffffff, 0.9f, 0, 0, IStoneTex(0x8a7a62, 0x5a4c3c, true, 25))); pave.SetTextureScale("_BaseMap", new Vector2(24, 24)); IA(s, Geo.Box(1800, 2, 1800), pave, O + V(0, -1, -200));
            var paveS = IM(Mats.Std(0xffffff, 0.85f, 0, 0, IStoneTex(0xb8a888, 0x8a7a62, true, 26))); paveS.SetTextureScale("_BaseMap", new Vector2(10, 2));
            // the palace on a terrace above a short flight of broad steps (the king stands at their head)
            IBasilica(s, O + V(0, 6, -190), 30);
            for (int k = 0; k < 4; k++) IA(s, Geo.Box(70 - k * 3, 1.5f, 6), paveS, O + V(0, 0.75f + k * 1.5f, -98 - k * 5));
            IA(s, Geo.Box(110, 6, 90), paveS, O + V(0, 3, -160));
            foreach (var sx in new[] { -1f, 1f }) { IStatue(IRoot(s, O + V(sx * 22, 6, -120)), 0, 0, 0, 10); IColumn(IRoot(s, O + V(sx * 34, 6, -118)), 0, 0, 20, 1.4f); }
            cs.steps = O + V(0, 6, -118);
            IGateway(s, O + V(0, 0, 60), 0, 90, 70);
            foreach (var (x, z, h) in new[] { (-140f, -120f, 120f), (140f, -110f, 130f), (-260f, -260f, 110f), (260f, -280f, 115f), (-60f, -420f, 150f), (90f, -440f, 140f), (-330f, 40f, 100f), (330f, 60f, 105f), (-180f, 220f, 90f), (190f, 240f, 95f) }) { var t = ISpire(s, O + V(x, 0, z), h, (int)x); IFallReg(t, R(0.6f, 3.0f), h, rng); }
            foreach (var (x, z, yaw) in new[] { (-170f, 160f, 20f), (170f, 170f, -20f), (-240f, -60f, 70f), (240f, -50f, -70f) }) { var t = ITemple(s, O + V(x, 0, z), yaw, 44, 60, 6, 8, 20); IFallReg(t, R(0.8f, 3.2f), 30, rng); }
            IAqueduct(s, O + V(-400, 0, -560), O + V(400, 0, -560), 70, 14);
            var awning = new[] { IM(Mats.Std(0xa83a2a, 0.85f, 0, 0, fabricTex, true)), IM(Mats.Std(0x2a6a8a, 0.85f, 0, 0, fabricTex, true)), IM(Mats.Std(0xc8a040, 0.85f, 0, 0, fabricTex, true)) };
            var wood = IM(Mats.Std(0x5a3a22, 0.85f));
            for (int k = 0; k < 18; k++) { var sp = O + V((k % 2 == 0 ? -1 : 1) * R(30, 44), 0, 120 - k * 10); var st = IRoot(s, sp, k % 2 == 0 ? 90 : -90); IA(st, Geo.Box(6, 1.2f, 3), wood, L(st, 0, 1.2f, 0)); foreach (var cx in new[] { -2.8f, 2.8f }) IA(st, Geo.Box(0.25f, 3.4f, 0.25f), wood, L(st, cx, 1.7f, 1.3f)); IA(st, Geo.Box(6.6f, 0.15f, 3.6f), awning[k % 3], L(st, 0, 3.5f, 0.3f), st.eulerAngles + V(-14, 0, 0)); for (int j = 0; j < 4; j++) IA(st, Geo.Sphere(0.4f, 8, 6), IM(Mats.Std(new[] { 0xc83a2a, 0xe0a030, 0x6a9a3a }[j % 3], 0.7f)), L(st, -2 + j * 1.3f, 2.1f, 0)); }
            for (int k = 0; k < 120; k++)
            {
                var p = O + V(R(-400, 400), 0, R(-480, 420)); if (Mathf.Abs(p.x - O.x) < 52 && p.z - O.z > -100) continue; if (Mathf.Abs(p.x - O.x) < 80 && p.z - O.z < -60 && p.z - O.z > -260) continue;
                var hs = IHouse(s, p, Mathf.Round(R(0, 4)) * 90 + R(-6, 6), R(9, 18), R(8, 14), R(8, 16)); if (rng.NextDouble() < 0.6) IFallReg(hs, R(0.3f, 3.5f), 12, rng);
            }
            cs.falls = ITakeFalls();
            var banner = IM(Mats.Std(0x8a1a2a, 0.85f, 0, 0, fabricTex, true)); var iron = IM(Mats.Std(0x3a3e44, 0.5f, 0.8f));
            for (int k = 0; k < 8; k++) foreach (var sx in new[] { -1f, 1f }) IBanner(s, O + V(sx * 26, 0, 40 - k * 18), 16, banner, iron);
            cs.king = IFigure(s, cs.steps, 0, FigKind.King, 3.0f, new[] { 0x4a1a3a, 0x6a2a4a, 0x8a1a1a }, rng, false, iGold2);
            ICrowd(s, O + V(0, 0, 70), 40, 120, 90, rng, true, 1.0f);
            for (int k = 0; k < 12; k++) { var g = IFigure(s, O + V(k < 6 ? -14 : 14, 6, -122 + (k % 6) * -6), k < 6 ? 90 : -90, FigKind.Guard, 1.95f, new[] { 0x6a5a3a }, rng); iWalkers.Add(new IWalker { t = g.root, f = g, v = Vector3.zero }); cs.guards.Add(g); }
            IBirdFlock(s, O + V(0, 90, -60), 14, 110, 0x2a2420, 1.5f);
            cs.fireM = IM(Mats.Add(0xff6020, 0.0f, null, true));
            for (int k = 0; k < 36; k++) { var p = O + V(R(-360, 360), R(2, 30), R(-460, 300)); if (Mathf.Abs(p.x - O.x) < 90 && p.z - O.z > -260 && p.z - O.z < 60) continue; var g = IBill(s, p, R(20, 60), R(26, 80), cs.fireM).transform; cs.fires.Add(g); } // (none in the cameras' faces by the steps)
            cs.smokeM = ISmoke(0x1a120e, 0f);
            for (int k = 0; k < 36; k++) { var p = IBill(s, O + V(R(-380, 380), R(20, 160), R(-460, 300)), R(80, 180), R(70, 160), cs.smokeM).transform; IDrift(set, p, V(R(-2, 3), R(5, 10), 0), O + V(0, 90, -80), V(400, 90, 400)); }
            ISnow(set, O + V(0, 60, -40), V(260, 60, 260), 220, 0xffd8a0, 0.5f, V(0.4f, 0.6f, 0), 0.6f); // dust motes glittering in the warm light
            IRays(s, O + V(-120, 200, -600), V(0.3f, -0.55f, 1), 8, 700, 12, 0xffe0b0, 0.03f, rng);
            BEnv(set, 0xc8a078, 90, 1000, 0x5a5a78, 0x6a5040, 0x2a1e16, 0xffcc94, 1.6f, 0x2a3a6a, null, 0xfff0e0, 0.1f, 1.0f);
            return cs;
        }
        void CityRuin(CitySet cs, float k)
        {
            Mats.SetColor(cs.sky, Color.Lerp(Color.white, Mats.Hex(0x3a1410), k)); Mats.SetAlpha(cs.fireM, 0.85f * Mathf.Clamp01(k * 1.6f)); Mats.SetAlpha(cs.smokeM, 0.72f * Mathf.Clamp01(k * 1.4f)); Mats.SetAlpha(iCrackM, 0.85f * k); Mats.SetAlpha(cs.sunG, 0.05f * (1 - k));
            if (iLight) { iLight.color = Color.Lerp(Mats.Hex(0xffcc94), Mats.Hex(0xff5a30), k); iLight.intensity = Mathf.Lerp(1.6f, 1.0f, k); }
            RenderSettings.fogColor = Color.Lerp(Mats.Hex(0xc8a078), Mats.Hex(0x3a1608), k);
            if (bcCA) bcCA.colorFilter.Override(Color.Lerp(Mats.Hex(0xfff0e0), Mats.Hex(0xffc8a0), k));
            foreach (var f in cs.fires) if (f) f.localScale = V(f.localScale.x, f.localScale.y * (0.96f + 0.08f * Mathf.PerlinNoise(BC.t * 6, f.position.x)), 1);
        }
        void DefHollowIntro()
        {
            var cs = BuildHollowCity(0); var O = cs.O; var e = BC.e; float H = 34;
            var tr = ISet(1); var T0 = tr.position; var rng = new System.Random(11);
            IArchMats(); var rust = IM(Mats.Std(0x6a3a20, 0.7f, 0.5f, 0, RustTex())); var steel = IM(Mats.Std(0x4a4c50, 0.45f, 0.75f));
            IA(tr, Geo.Box(600, 2, 600), IM(Mats.Std(0x5a4632, 0.95f)), T0 + V(0, -1, 0));
            for (int k = 0; k < 60; k++) { float a = (float)rng.NextDouble() * Mathf.PI * 2, r = 20 + (float)rng.NextDouble() * 90; IA(tr, Geo.Box(6 + (float)rng.NextDouble() * 20, 3 + (float)rng.NextDouble() * 18, 6 + (float)rng.NextDouble() * 14), k % 3 == 0 ? rust : iStoneDk, T0 + V(Mathf.Cos(a) * r, 3, Mathf.Sin(a) * r), V((float)rng.NextDouble() * 30, (float)rng.NextDouble() * 90, (float)rng.NextDouble() * 30)); }
            for (int k = 0; k < 8; k++) { float a = k * 0.8f; var t = ISpire(tr, T0 + V(Mathf.Cos(a) * 140, 0, Mathf.Sin(a) * 140), 90, k); t.localRotation = Quaternion.Euler(30 + k * 5, k * 40, 20); t.localPosition += V(0, -10, 0); }
            var bits = new List<(Transform t, Vector3 from, Vector3 to, float at)>();
            for (int k = 0; k < 90; k++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2, r = 10 + (float)rng.NextDouble() * 36; var from = T0 + V(Mathf.Cos(a) * r, 0.5f, Mathf.Sin(a) * r);
                var mesh = k % 5 == 0 ? Geo.Box(0.4f, 6, 0.4f) : k % 5 == 1 ? Geo.Box(2.4f, 1.4f, 1.8f) : k % 5 == 2 ? Geo.Cyl(0.5f, 0.5f, 3, 8) : k % 5 == 3 ? Geo.Box(3, 3, 0.4f) : Geo.Icosahedron(1.2f);
                var g = IA(tr, mesh, k % 3 == 0 ? rust : k % 3 == 1 ? steel : iStoneDk, from, V((float)rng.NextDouble() * 360, (float)rng.NextDouble() * 360, 0)).transform;
                var to = T0 + V(((float)rng.NextDouble() - 0.5f) * 6, 2 + (float)rng.NextDouble() * 15, ((float)rng.NextDouble() - 0.5f) * 4);
                bits.Add((g, from, to, (float)rng.NextDouble() * 2.6f));
            }
            var king2 = IFigure(tr, T0, 0, FigKind.King, 3.0f, new[] { 0x4a1a3a, 0x6a2a4a, 0x8a1a1a }, rng, false, iGold2);
            var hk = NewEnt("cineHK"); Mk.colliders = false; hk.g.transform.SetParent(tr, false); hk.g.transform.position = T0;
            var sv = (hkConc, hkSteel, hkRust, hkGold); try { hk.hk = BuildHollow(hk); } catch (Exception ex) { Debug.LogWarning("cine HK: " + ex.Message); } (hkConc, hkSteel, hkRust, hkGold) = sv;
            Mk.colliders = false; foreach (var c in hk.g.GetComponentsInChildren<Collider>(true)) Destroy(c); var rb = hk.g.GetComponent<Rigidbody>(); if (rb) Destroy(rb);
            hk.g.transform.localScale = Vector3.one * 4.4f; hk.g.transform.rotation = Quaternion.Euler(0, 180, 0); hk.g.SetActive(false); hk.boss = 4;
            var voidM = ISmoke(0x050208, 0f); var vsm = new List<Transform>(); for (int i = 0; i < 22; i++) vsm.Add(IBill(tr, T0 + V(0, 6, 0), 14, 14, voidM).transform);
            var fireT = IM(Mats.Add(0xff6020, 0.7f, null, true)); for (int k = 0; k < 20; k++) { float a = k * 0.6f; IBill(tr, T0 + V(Mathf.Cos(a) * (50 + k * 4), 8, Mathf.Sin(a) * (50 + k * 4)), 18, 30, fireT); }
            var smT = ISmoke(0x1a100c, 0.6f); for (int k = 0; k < 20; k++) { var p = IBill(tr, T0 + V(Mathf.Cos(k) * 80, 30 + k * 4, Mathf.Sin(k) * 80), 90, 80, smT).transform; IDrift(1, p, V(1, 5, 0), T0 + V(0, 70, 0), V(140, 50, 140)); }
            ISnow(1, T0 + V(0, 20, 0), V(60, 20, 60), 200, 0xff8a30, 0.4f, V(0.5f, 3, 0), 0.9f); // embers
            BEnv(1, 0x3a1a0c, 40, 360, 0x6a4050, 0x5a3420, 0x1a0c08, 0xff8a50, 2.0f, 0x1a0806, V(-0.5f, -0.5f, -0.7f), 0xffd8c0, 0.1f, 1.3f);
            float s1 = BShotAt(7.0f, 0, false, (u, lt, dt) =>
            {
                BPath(VA(O + V(320, 320, 700), O + V(180, 160, 380), O + V(40, 70, 160), O + V(14, 24, -20), cs.steps + V(6, 5, 22)), VA(O + V(0, 40, -120), O + V(0, 30, -60), O + V(0, 30, -120), cs.steps + V(0, 8, 0), cs.steps + V(0, 3, 0)), u);
                IFigPose(cs.king, 0, 4, 0, 0);
            }, 2.0f, 0.05f, null, 48, 0);
            BCue(s1 + 0.2f, "bcHollow", 0.9f); BCue(s1 + 3.4f, "bcHollow", 0.6f); BCue(s1 + 0.6f, "iChatter", 0.7f); BCue(s1 + 3.0f, "iBell", 0.4f); BCue(s1 + 0.5f, "iWind", 0.3f);
            var hRift = SerSkyRift(cs.s, O + V(-40, 300, -760)); // (the Void, coming: it opens over his city in the ruin)
            float s2 = BShotAt(5.4f, 0, false, (u, lt, dt) =>
            {
                SerRiftShow(hRift, 0); var kp = cs.king.root.position;
                BPath(VA(kp + V(2.2f, 3.6f, -4.5f), kp + V(4.5f, 3.2f, -1.5f), kp + V(4.6f, 2.8f, 3.2f)), VA(kp + V(-8, 0, 60), kp + V(0, 2.6f, 20), kp + V(0, 2.6f, 0)), u);
                IFigPose(cs.king, Mathf.Sin(BC.t * 0.6f) * 18, 6, 14, 6, -0.05f, 0.9f, 0.3f); cs.king.wind = 1.2f;
            }, 0.05f, 0.05f, null, 42);
            BCue(s2 + 0.3f, "iSwell", 0.5f);
            BSay(s2 + 0.9f, 4.0f, "This was my city. Every street, every soul in it... mine to protect.", "vHollow");
            float s3 = BShotAt(6.2f, 0, false, (u, lt, dt) =>
            {
                SerRiftShow(hRift, Smoother(Mathf.Clamp01(lt / 1.2f)));
                float k = Smoother(Mathf.Clamp01(lt / 1.6f)); CityRuin(cs, k); IFallsAt(cs.falls, lt * 0.85f);
                foreach (var w in iWalkers) if (w.f != null && w.f.h < 1.95f) { var away = w.t.position - (O + V(0, 0, -200)); away.y = 0; w.v = Vector3.Lerp(w.v, away.normalized * 5, 1 - Mathf.Exp(-2 * dt)); IFigPose(w.f, 0, -6, 40, 60, 0.25f); }
                BPath(VA(O + V(-260, 110, 300), O + V(-200, 80, 230), O + V(-150, 60, 180)), VA(O + V(0, 40, -100), O + V(0, 40, -140), O + V(-40, 30, -160)), u, 0.5f + k * 0.5f);
                if (rand < 0.25f) bcShake = Mathf.Max(bcShake, 0.9f);
            }, 0.05f, 0.05f, null, 50);
            BSay(s3 + 1.4f, 4.2f, "Then the Void came down out of the sky... and the walls I built meant nothing.", "vHollow");
            BCue(s3 + 0.0f, "iVoid", 0.9f); BCue(s3 + 0.05f, "iCrack", 1f); BCue(s3 + 0.3f, "iCollapse", 1f); BCue(s3 + 1.4f, "iCollapse", 0.9f); BCue(s3 + 2.2f, "boom", 0.7f); BCue(s3 + 3.0f, "iCollapse", 0.8f); BCue(s3 + 0.5f, "iRoarFar", 0.6f); BCue(s3 + 0.4f, "bcHollowWar", 0.9f); BCue(s3 + 4.2f, "iCollapse", 0.7f);
            float s4 = BShotAt(5.2f, 0, false, (u, lt, dt) =>
            {
                SerRiftShow(hRift, 1); CityRuin(cs, 1); IFallsAt(cs.falls, 4.9f + lt * 0.8f);
                var kp = cs.steps + V(0, -6 * Smoother(Mathf.Clamp01(lt / 1.8f)), 30 * Smoother(Mathf.Clamp01(lt / 1.8f)));
                cs.king.root.position = kp; cs.king.speed = lt < 1.8f ? 3.4f : 0;
                IFigPose(cs.king, -20, -18, lt > 1.8f ? 150 : 40, lt > 1.8f ? 120 : 30, lt > 1.8f ? -0.15f : 0.3f, 0.2f, 0.4f, 0, lt > 1.8f ? 20 : 0, lt > 1.8f ? 20 : 0); cs.king.wind = 2.4f;
                foreach (var g in cs.guards) IFigPose(g, 0, 10, 60, 40, -0.1f);
                if (rand < 0.18f) IDebrisAt(cs.s, kp + V((rand - 0.5f) * 20, 30 + rand * 20, (rand - 0.5f) * 20), 3, 4, 2.5f, new System.Random((int)(BC.t * 1000)));
                BPath(VA(kp + V(12, 6, 22), kp + V(8, 4.2f, 15), kp + V(5.5f, 3.2f, 10)), VA(kp + V(-4, 4, -4), kp + V(0, 2.6f, 0), kp + V(0, 2.8f, 0)), u, 0.8f);
            }, 0.05f, 0.05f, null, 46);
            BSay(s4 + 0.6f, 4.0f, "My people ran. I stayed. A king does not abandon his kingdom.", "vHollow");
            BCue(s4 + 0.2f, "iCollapse", 0.8f); BCue(s4 + 1.9f, "boom", 0.6f); BCue(s4 + 2.6f, "iCollapse", 0.7f); BCue(s4 + 1.8f, "iBeat", 0.8f);
            float s5 = BShotAt(7.0f, 1, false, (u, lt, dt) =>
            {
                float grow = Smoother(Mathf.Clamp01((lt - 0.8f) / 4.2f));
                king2.root.localScale = Vector3.one * Mathf.Lerp(1, 5, grow);
                IFigPose(king2, 0, lt < 1.4f ? -30 : -20 + 50 * grow, lt < 1.4f ? 30 : 140 * grow, lt < 1.4f ? 30 : 140 * grow, lt < 1.4f ? 0.5f : -0.2f * grow, 0.2f, 0.2f, lt < 1.4f ? 0.8f : Mathf.Lerp(0.8f, 0, grow), 30 * grow, 30 * grow); king2.wind = 1 + grow * 3;
                foreach (var b in bits) { float p = Smoother(Mathf.Clamp01((lt - 0.8f - b.at) / 1.4f)); var to = T0 + (b.to - T0) * Mathf.Lerp(1, 2.6f, grow); b.t.position = Vector3.Lerp(b.from, to, p) + V(0, Mathf.Sin(p * Mathf.PI) * 9, 0) + Vector3.Cross(Vector3.up, b.from - T0).normalized * Mathf.Sin(p * Mathf.PI) * 6; b.t.Rotate(V(90, 60, 0) * dt * (1 - p)); }
                Mats.SetAlpha(voidM, 0.72f * Mathf.Clamp01((lt - 1.6f) * 1.2f)); for (int i = 0; i < vsm.Count; i++) { float a = BC.t * 1.4f + i; vsm[i].position = T0 + V(Mathf.Cos(a) * (6 + grow * 12), 4 + i * 1.3f, Mathf.Sin(a) * (6 + grow * 12)); }
                if (lt > 5.1f && !hk.g.activeSelf) { hk.g.SetActive(true); king2.root.gameObject.SetActive(false); foreach (var b in bits) b.t.gameObject.SetActive(false); iFlashA = 0.8f; bcShake = 3; }
                if (hk.g.activeSelf) { try { AnimateHollow(hk, dt); } catch (Exception) { } }
                float a2 = 0.3f + lt * 0.3f, rad = 16 + grow * 26; var cp = T0 + V(Mathf.Sin(a2) * rad, 3 + lt * 3.2f, Mathf.Cos(a2) * rad);
                BPath(VA(cp, cp), VA(T0 + V(0, 4 + grow * 16, 0), T0 + V(0, 4 + grow * 16, 0)), 0, lt > 5 ? 1.6f : 0.4f);
            }, 0.05f, 0.05f, null, 50, 1.1f);
            BSay(s5 + 0.6f, 4.2f, "It offered me a body that could never fall... built from the city itself.", "vHollow");
            BCue(s5 + 0.2f, "iCreak", 0.9f); BCue(s5 + 1.0f, "iWhisper", 0.6f); BCue(s5 + 1.6f, "iCollapse", 0.7f); BCue(s5 + 2.4f, "iVoid", 0.9f); BCue(s5 + 3.6f, "iCreak", 0.9f); BCue(s5 + 5.1f, "roar", 1f); BCue(s5 + 5.1f, "boom", 1f); BCue(s5 + 5.15f, "iTitle", 0.6f);
            // today: he stands where he came down, in the crater he made, the dust still settling round him - the camera starts
            // at his feet and climbs his whole height, slowly, to the fire in his skull; he roars
            bool roared = false;
            float s6 = BShotAt(8.2f, -1, false, (u, lt, dt) =>
            {
                if (e == null || !e.g) return; var bp = bcBossPos0; var f = BCFwd(e); var side = Vector3.Cross(Vector3.up, f);
                e.g.transform.position = bp;
                // his pose: standing heavy over the crater, then the roar (his own animation, his own states)
                if (e.hk != null) { var st0 = e.state; float t0 = e.hk.t; e.state = lt > 4.6f ? "roar" : "chase"; e.vel = Vector3.zero; try { AnimateHollow(e, dt); } catch (Exception) { } e.state = st0; e.hk.t = t0; }
                if (lt > 4.6f && !roared) { roared = true; bcShake = 2.4f; SpawnParticles(bp + V(0, 1, 0), 60, 14, dustMat, 0.6f, 2.0f, -1); for (int i = 0; i < 2; i++) ShockRing(V(bp.x, bp.y + 0.2f, bp.z), 18 + i * 10, 0xff8040, 0.6f + i * 0.3f); }
                if (rand < 0.9f) SpawnParticles(bp + f * (rand * 16) + side * (rand - 0.5f) * 30 + V(0, 0.3f + rand * 2, 0), 1, 0.8f, rand < 0.7f ? dustMat : ashMat, 0.5f, 3.0f, -0.4f); // the dust of his landing, settling
                if (rand < 0.5f) SpawnParticles(bp + V((rand - 0.5f) * 12, H * (0.5f + rand * 0.5f), (rand - 0.5f) * 12), 1, 1.2f, emberMat, 0.12f, 1.6f, -1.5f);   // embers off his burning crown
                BPath(VA(bp + f * 11 + side * 4 + V(0, 0.6f, 0), bp + f * 16 + side * 7 + V(0, 1.6f, 0), bp + f * 26 + side * 9 + V(0, 3.2f, 0), bp + f * 36 + side * 8 + V(0, 4.5f, 0)),
                      VA(bp + V(0, 1.5f, 0), bp + V(0, H * 0.3f, 0), bp + V(0, H * 0.6f, 0), bp + V(0, H * 0.72f, 0)), u, lt > 4.6f && lt < 6.2f ? 0.6f : 0.08f);
            }, 0.6f, 1.0f, null, 50, 1.0f);
            BCue(s6 + 0.1f, "iWind", 0.6f); BCue(s6 + 0.4f, "rumble", 0.5f); BCue(s6 + 2.4f, "iCreak", 0.7f); BCue(s6 + 3.6f, "bcHollowWar", 0.8f); BCue(s6 + 4.6f, "roar", 1f); BCue(s6 + 4.65f, "iRoarFar", 0.8f); BCue(s6 + 4.7f, "boom", 0.6f);
            BSay(s6 + 4.8f, 2.8f, "MY KINGDOM... WILL NOT FALL!", "vHollow");
            BTitle(s6 + 5.2f, "THE HOLLOW KING", 2.8f);
        }
        // his defeat: the colossus comes down; and when the dust clears, what is left in the rubble is the man - the king he
        // was before the Void made him out of his city, kneeling, crownless. He remembers his people cheering him, and the city
        // he became instead of saving them; he reaches for his fallen crown, lets it lie, and goes to dust.
        void DefHollowDefeat()
        {
            var e = BC.e; float H = 34; var d0 = bossDeath; var bp0 = d0 != null ? d0.basePos : bcBossPos0;
            var f0 = P.pos - bp0; f0.y = 0; f0 = f0.sqrMagnitude > 1 ? f0.normalized : Vector3.forward; var side0 = Vector3.Cross(Vector3.up, f0);
            // 1 - the fall, as the game plays it: he staggers, the fire bursts out of his chest, he topples and comes apart
            float s1 = BShotAt(5.2f, -1, true, (u, lt, dt) =>
            {
                var bp = e != null && e.g && e.g.activeSelf ? e.g.transform.position : bp0;
                BPath(VA(bp + f0 * 36 + side0 * 14 + V(0, 1.4f, 0), bp + f0 * 30 + side0 * 8 + V(0, 1.0f, 0), bp + f0 * 26 + side0 * 3 + V(0, 0.8f, 0)), VA(bp + V(0, H * 0.6f, 0), bp + V(0, H * 0.35f, 0), bp + V(0, H * 0.15f, 0)), u, 0.35f);
            }, 0.3f, 0.05f, null, 54, 0);
            BCue(s1 + 1.5f, "iCollapse", 0.9f); BCue(s1 + 3.2f, "boom", 0.7f); BCue(s1 + 3.4f, "iCollapse", 0.8f);
            // the man in the rubble (made in the arena), and his crown lying a few steps from him
            var gp = bp0 + side0 * 11 + f0 * 5; gp.y = bp0.y;               // (on the open floor beside the heap, not on top of it)
            var cp = bp0 + side0 * 8 + f0 * 8.5f; cp.y = bp0.y + 0.3f;
            IFig man = null; GameObject crown = null, shade = null; var manMats = new List<(Material m, Color c)>();
            void Make()
            {
                if (man != null) return; var rng = new System.Random(4);
                man = IFigure(world, gp, Mathf.Atan2(-side0.x, -side0.z) * Mathf.Rad2Deg + 25, FigKind.King, 3.0f, new[] { 0x3a1a2a, 0x4a2a3a, 0x5a1a1a }, rng, false, iGold2);
                iFigs.Remove(man); bcLive.Add(man.root.gameObject);
                foreach (Transform c in man.head) if (c.position.y - man.head.position.y > 3.0f * 0.105f) c.gameObject.SetActive(false); // (no crown: it lies on the ground)
                foreach (var rr in man.root.GetComponentsInChildren<Renderer>(true)) { rr.gameObject.layer = 0; if (rr.sharedMaterial && rr.sharedMaterial.HasProperty("_BaseColor")) manMats.Add((rr.sharedMaterial, rr.sharedMaterial.GetColor("_BaseColor"))); }
                foreach (var (m, c) in manMats) Mats.SetColor(m, c * 0.75f + new Color(0.06f, 0.05f, 0.04f)); // dusted grey with the rubble
                shade = Mk.Glow(world, 0xffb070, 6, 0.0f); shade.transform.position = gp + V(0, 1.6f, 0); bcLive.Add(shade); // the last warmth of him
                var lg = new GameObject("manLight"); lg.transform.position = Vector3.Lerp(gp, cp, 0.5f) + f0 * 3 + V(0, 4, 0); var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = Mats.Hex(0xffb070); li.range = 18; li.intensity = 3.2f; li.shadows = LightShadows.None; bcLive.Add(lg); // (the embers' light on him and the crown)
                var gold = Mats.Std(0xd8a848, 0.22f, 0.92f); crown = new GameObject("fallenCrown"); bcLive.Add(crown);
                var ring = Mk.Msh(crown.transform, Geo.Torus(1.3f, 0.22f, 28, 6, Mathf.PI * 1.65f), gold); ring.transform.localRotation = Quaternion.Euler(90, 0, 0);
                for (int k = 0; k < 7; k++) { float a = k * Mathf.PI * 2 / 8; if (k == 5) continue; Mk.Msh(crown.transform, Geo.Cone(0.22f, k % 2 == 0 ? 1.1f : 0.75f, 4), gold, Mathf.Cos(a) * 1.3f, 0.45f, Mathf.Sin(a) * 1.3f); if (k % 2 == 0) Mk.Msh(crown.transform, Geo.Sphere(0.14f, 8, 6), Mats.Basic(0xff2a30), Mathf.Cos(a) * 1.32f, 0.1f, Mathf.Sin(a) * 1.32f); }
                crown.transform.position = cp; crown.transform.rotation = Quaternion.Euler(18, 40, 9);
                if (e?.hk != null && e.hk.crown) e.hk.crown.gameObject.SetActive(false); // (the one the game threw off him: this is it, at rest)
            }
            // 2 - the dust settles, and in the rubble kneels a man: no colossus, no fire - the king, as he was
            float s2 = BShotAt(6.0f, -1, false, (u, lt, dt) =>
            {
                Make(); IFigPose(man, -10, 26, 0, 0, 0.3f, 0.3f, 0.3f, 0.9f); IFigAnimate(man, dt);
                float clear = Smoother(Mathf.Clamp01(lt / 3.0f));
                if (rand < 1.2f - clear) SpawnParticles(gp + side0 * (rand - 0.5f) * 20 + f0 * (rand - 0.5f) * 16 + V(0, rand * 6, 0), 1, 0.4f, ashMat, 0.14f, 3.2f, -0.2f); // (fine dust, settling)
                if (shade) Mats.SetAlpha(shade.GetComponent<Renderer>().sharedMaterial, 0.25f * clear);
                var tg = gp + V(0, 1.4f, 0);
                BPath(VA(tg + f0 * 16 - side0 * 4 + V(0, 1.4f, 0), tg + f0 * 11 - side0 * 2.5f + V(0, 0.8f, 0), tg + f0 * 7.5f - side0 * 1.6f + V(0, 0.4f, 0)), VA(tg, tg, tg), u, 0, 0, 0.5f);
            }, 0.05f, 0.05f, null, 44, 0);
            BCue(s2 + 0.2f, "iWind", 0.6f); BCue(s2 + 0.8f, "bcHollowSad", 0.9f); BCue(s2 + 3.6f, "bcHollowSad", 0.5f);
            // 3 - the memory: his city alive, his people cheering him on the palace steps... and then all of it ruined under
            // the Void's rift, the people gone - the city he became, instead of the king who saved them
            var cs = BuildHollowCity(0); var O = cs.O; var mRift = SerSkyRift(cs.s, O + V(-40, 300, -760));
            float s3 = BShotAt(8.6f, 0, false, (u, lt, dt) =>
            {
                bool ruined = lt > 4.2f;
                if (ruined && iCues.Add("ruinSwap")) { CityRuin(cs, 1); IFallsAt(cs.falls, 9); foreach (var w in iWalkers) if (w.t) w.t.gameObject.SetActive(false); cs.king.root.gameObject.SetActive(false); bcXfT = 1.6f; bcXfDur = 1.6f; if (bcXfTex && bcXf) { bcXf.texture = bcXfTex; } }
                if (lt > 4.1f && lt - dt <= 4.1f && !bcCapturing) StartCoroutine(BCCapture());
                SerRiftShow(mRift, ruined ? 1 : 0);
                if (!ruined) { IFigPose(cs.king, Mathf.Sin(BC.t * 0.5f) * 22, 8, 120, 6, -0.08f, 0.3f, 0.3f); foreach (var w in iWalkers) if (w.f != null && w.f.h < 1.95f) IFigPose(w.f, 0, -14, 140 * (0.5f + 0.5f * Mathf.Sin(BC.t * 5 + w.t.position.x)), 30, -0.05f); } // he raises a hand to them; they cheer
                BPath(VA(cs.steps + V(-6, 3.4f, 13), cs.steps + V(-4.2f, 3.0f, 9), cs.steps + V(-9, 5, 19)), VA(cs.steps + V(0, 2.6f, 0), cs.steps + V(0, 2.7f, 0), cs.steps + V(0, 4, -10)), u);
            }, 1.0f, 0.05f, null, 42, 1.2f);
            BCue(s3 + 0.3f, "bcHollow", 0.6f); BCue(s3 + 0.5f, "iChatter", 0.8f); BCue(s3 + 1.6f, "iChatter", 0.5f); BCue(s3 + 4.2f, "iWind", 0.6f); BCue(s3 + 4.3f, "iVoid", 0.5f); BCue(s3 + 4.4f, "iWhisper", 0.4f);
            BSay(s3 + 0.6f, 3.2f, "They called me their wall. Their shield.", "vHollow");
            BSay(s3 + 4.6f, 3.8f, "When the Void came, I made myself out of the city... and forgot the people in it.", "vHollow");
            // 4 - the end of him: he lifts his head, sees his crown lying in the dust, reaches for it... lets his hand fall - and
            // goes to dust himself, drifting away on the wind
            float s4 = BShotAt(8.0f, -1, false, (u, lt, dt) =>
            {
                Make(); float reach = Smoother(Mathf.Clamp01((lt - 1.2f) / 1.4f)) * (1 - Smoother(Mathf.Clamp01((lt - 3.4f) / 1.0f)));
                var toC = cp - gp; float yawToC = Mathf.Atan2(toC.x, toC.z) * Mathf.Rad2Deg - man.root.eulerAngles.y;
                IFigPose(man, Mathf.DeltaAngle(0, yawToC) * Smoother(Mathf.Clamp01((lt - 0.4f) / 1.0f)), Mathf.Lerp(26, 10, Smoother(Mathf.Clamp01(lt / 1.2f))), 70 * reach, 0, 0.3f - 0.2f * reach, 0.2f, 0.3f, 0.9f - 0.2f * reach, 0, 0); IFigAnimate(man, dt);
                float dust = Smoother(Mathf.Clamp01((lt - 4.6f) / 2.4f));
                if (dust > 0)
                {
                    man.root.localScale = V(1, Mathf.Max(0.001f, 1 - dust), 1); // he crumbles from the top down
                    foreach (var (m, c) in manMats) if (m) Mats.SetColor(m, Color.Lerp(c * 0.75f + new Color(0.06f, 0.05f, 0.04f), new Color(0.35f, 0.32f, 0.3f), dust));
                    for (int i = 0; i < 3; i++) SpawnParticles(gp + V((rand - 0.5f) * 1.2f, 3.0f * (1 - dust) * rand + 0.2f, (rand - 0.5f) * 1.2f), 1, 1.2f, ashMat, 0.11f, 2.6f, -1.2f);
                    if (shade) Mats.SetAlpha(shade.GetComponent<Renderer>().sharedMaterial, 0.25f * (1 - dust));
                    if (dust >= 1 && man.root.gameObject.activeSelf) man.root.gameObject.SetActive(false);
                }
                var mid = Vector3.Lerp(gp + V(0, 1.4f, 0), cp, 0.4f);
                BPath(VA(mid + side0 * 2 + f0 * 6.5f + V(0, 2.2f, 0), mid + side0 * 1.4f + f0 * 5.4f + V(0, 1.9f, 0), mid + side0 * 1.8f + f0 * 6f + V(0, 3.0f, 0)), VA(mid, mid, gp + V(0, 2.2f, 0)), u, 0, 0, 0.4f); // (from the open side, above the rubble)
            }, 0.05f, 0.05f, null, 40, 1.2f);
            BCue(s4 + 1.2f, "iSwell", 0.35f); BCue(s4 + 4.6f, "iWind", 0.7f); BCue(s4 + 4.8f, "iCollapse", 0.25f);
            BSay(s4 + 1.6f, 3.8f, "My kingdom... was never the stone.", "vHollow");
            // 5 - the crown alone in the dust, the embers drifting down over it
            float s5 = BShotAt(4.6f, -1, false, (u, lt, dt) =>
            {
                Make(); float a = lt * 0.18f; var c = cp;
                BPath(VA(c + (f0 * Mathf.Cos(a) + side0 * Mathf.Sin(a)) * 6 + V(0, 1.4f, 0), c + (f0 * Mathf.Cos(a + 0.5f) + side0 * Mathf.Sin(a + 0.5f)) * 8 + V(0, 3.2f, 0)), VA(c, c), u, 0, 0, 0.5f);
                if (rand < 0.5f) SpawnParticles(c + V((rand - 0.5f) * 14, 0.5f, (rand - 0.5f) * 14), 1, 0.8f, emberMat, 0.06f, 2.5f, -1);
                if (rand < 0.4f) SpawnParticles(gp + V((rand - 0.5f) * 3, 2 + rand * 3, (rand - 0.5f) * 3), 1, 0.6f, ashMat, 0.1f, 2.4f, -0.6f);
            }, 0.05f, 2.0f, null, 38, 1.2f);
            BCue(s5 + 0.4f, "ding", 0.35f);
        }

        // ================= THE SERAPH =================
        class HeavenSet { public Transform s; public Vector3 O, ser; public Ent se; public List<IFall> falls; public Material sky, darkM, glowM, rayM; public Transform blemish, beam; public List<GameObject> bolts = new List<GameObject>(); }
        HeavenSet BuildHeaven(int set)
        {
            var hs = new HeavenSet(); var s = ISet(set); hs.s = s; var O = s.position; hs.O = O; var rng = new System.Random(90 + set);
            float R(float a, float b) => a + (b - a) * (float)rng.NextDouble();
            IArchMatsHeaven();
            hs.sky = INF(Mats.Basic(0xffffff, IGrad(0x3a6ac0, 0xfff0d0), true)); IBill(s, O + V(0, 200, -900), 3600, 1400, hs.sky);
            hs.glowM = IDiscGlow(s, O + V(0, 260, -820), 0xfff4d0, 280, 0.06f);
            var cloud = ISmoke(0xffffff, 0.78f);
            for (int k = 0; k < 70; k++) { var p = IBill(s, O + V(R(-760, 760), R(-30, 10), R(-820, 420)), R(130, 280), R(60, 130), cloud).transform; IDrift(set, p, V(R(2, 6), 0, 0), O + V(0, -10, -200), V(760, 20, 620)); }
            foreach (var (x, y, z, r) in new[] { (0f, 0f, -120f, 90f), (-220f, 40f, -240f, 60f), (230f, 60f, -260f, 64f), (-380f, 90f, -480f, 70f), (360f, 110f, -500f, 70f), (-110f, 140f, -560f, 50f), (140f, 170f, -620f, 46f), (-520f, 60f, -200f, 50f), (520f, 80f, -220f, 54f) })
            {
                var plat = IRoot(s, O + V(x, y, z)); IA(plat, Geo.Cyl(r, r * 0.92f, 8, 32), iStone, L(plat, 0, -4, 0)); IA(plat, Geo.Cone(r * 0.9f, r * 0.8f, 14), iStoneDk, L(plat, 0, -8 - r * 0.4f, 0), V(180, 0, 0));
                IA(plat, Geo.Torus(r, 1.2f, 48, 4), iGold2, L(plat, 0, 0, 0), V(90, 0, 0));
                var t = ITemple(plat, L(plat, 0, 0, 0), R(-20, 20), r * 0.7f, r * 0.9f, 6, 8, r * 0.36f); IFallReg(t, R(0.4f, 2.6f), 30, rng);
                for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; var col = IRoot(plat, L(plat, Mathf.Cos(a) * r * 0.85f, 0, Mathf.Sin(a) * r * 0.85f)); IColumn(col, 0, 0, r * 0.5f, r * 0.04f); IStatue(col, 0, r * 0.5f, 0, r * 0.12f); IFallReg(col, R(0.2f, 3.0f), r * 0.5f, rng); }
                var fallM = IM(Mats.Add(0xfff0c0, 0.12f, null, true)); IMsh(plat, Geo.Box(r * 0.3f, 220, 0.5f), fallM, L(plat, r * 0.6f, -110, r * 0.7f)); // a fall of light pouring off its edge
            }
            foreach (var (x, z, h) in new[] { (-120f, -320f, 200f), (120f, -330f, 210f), (-300f, -700f, 260f), (300f, -720f, 250f) }) { var t = ISpire(s, O + V(x, 0, z), h, (int)x); IFallReg(t, R(0.5f, 2.8f), h, rng); }
            hs.falls = ITakeFalls();
            hs.rayM = IRays(s, O + V(0, 360, -700), V(0.0f, -0.6f, 1), 12, 900, 16, 0xfff0c8, 0.03f, rng);
            hs.ser = O + V(0, 120, -60);
            var se = NewEnt("cineSer"); Mk.colliders = false; se.g.transform.SetParent(s, false); se.g.transform.position = hs.ser;
            try { se.sr = BuildSeraph(se); } catch (Exception ex) { Debug.LogWarning("cine seraph: " + ex.Message); }
            Mk.colliders = false; foreach (var c in se.g.GetComponentsInChildren<Collider>(true)) Destroy(c); var rb = se.g.GetComponent<Rigidbody>(); if (rb) Destroy(rb);
            se.g.transform.localScale = Vector3.one * 6f; se.g.transform.rotation = Quaternion.Euler(0, 0, 0); se.boss = 2; hs.se = se;
            hs.blemish = IRoot(s, O + V(-30, 0, -40)); IStatue(hs.blemish, 0, 0, 0, 16); IA(hs.blemish, Geo.Sphere(3, 10, 8), BCBlack(), L(hs.blemish, 0, 10, 2));
            hs.beam = IMsh(s, Geo.Cyl(1, 1, 1, 12), IM(Mats.Add(0xfff0b0, 0.9f, null, false)), O).transform; hs.beam.gameObject.SetActive(false);
            hs.darkM = ISmoke(0x0a0408, 0f);
            for (int k = 0; k < 46; k++) { var p = IBill(s, O + V(R(-400, 400), R(0, 220), R(-700, 100)), R(60, 170), R(60, 150), hs.darkM).transform; IDrift(set, p, V(R(-4, 4), R(-1, 2), R(-2, 2)), O + V(0, 100, -300), V(420, 120, 420)); }
            var boltM = IM(Mats.Add(0xffd070, 1f, null, false));
            for (int k = 0; k < 5; k++) { var b = new GameObject("bolt"); b.transform.SetParent(s, false); IJag(b.transform, O + V(R(-400, 400), R(260, 320), R(-500, -200)), V(R(-0.3f, 0.3f), -1, 0), 12, R(20, 34), 3, boltM, rng, 1.3f); b.SetActive(false); hs.bolts.Add(b); }
            ISnow(set, O + V(0, 100, -100), V(220, 100, 220), 220, 0xfff0c0, 0.9f, V(0, 0.6f, 0));
            IBirdFlock(s, O + V(0, 80, -100), 16, 90, 0xffffff, 1.4f);
            BEnv(set, 0xf0e8d8, 120, 1400, 0xb0c0e0, 0xf0d8b0, 0x8a7a60, 0xfff0d8, 1.7f, 0x3a6ac0, V(-0.3f, -0.6f, -0.7f), 0xfff8f0, 0.15f, 1.4f);
            return hs;
        }
        void HeavenCorrupt(HeavenSet hs, float k)
        {
            Mats.SetColor(hs.sky, Color.Lerp(Color.white, Mats.Hex(0x7a4048), k)); Mats.SetAlpha(hs.darkM, 0.7f * k); Mats.SetAlpha(hs.glowM, 0.06f * (1 - k)); Mats.SetAlpha(iCrackM, 0.9f * k); Mats.SetAlpha(hs.rayM, 0.03f * (1 - k));
            if (iLight) { iLight.color = Color.Lerp(Mats.Hex(0xfff0d8), Mats.Hex(0xd89a48), k); iLight.intensity = Mathf.Lerp(1.7f, 1.6f, k) * (k > 0.1f ? (0.8f + 0.4f * Mathf.PerlinNoise(BC.t * 10, 3)) : 1); }
            RenderSettings.fogColor = Color.Lerp(Mats.Hex(0xf0e8d8), Mats.Hex(0x6a4030), k);
            IArchAmbient(Color.Lerp(Mats.Hex(0xb0c0e0), Mats.Hex(0x8a6078), k), Color.Lerp(Mats.Hex(0xf0d8b0), Mats.Hex(0x9a6838), k), Mats.Hex(0x3a2418));
            if (bcCA) bcCA.colorFilter.Override(Color.Lerp(Mats.Hex(0xfff8f0), Mats.Hex(0xffd8a8), k));
            for (int i = 0; i < hs.bolts.Count; i++) { bool on = k > 0.2f && Mathf.PerlinNoise(BC.t * 3 + i * 7, 2) > 0.78f; if (hs.bolts[i].activeSelf != on) { hs.bolts[i].SetActive(on); if (on) iFlashA = Mathf.Max(iFlashA, 0.12f); } }
        }
        void SerFlicker(Ent se, float amount, float dt)
        {
            if (se?.sr == null) return; var r = se.sr;
            try { AnimateSeraph(se, dt); } catch (Exception) { }
            if (r.halo != null) r.halo.go.SetActive(amount <= 0 || Mathf.PerlinNoise(BC.t * 16, 1) > amount * 0.6f);
            for (int i = 0; i < r.wings.Count; i++) if (r.wings[i] != null) r.wings[i].rz += Mathf.Sin(BC.t * (1.2f + amount * 9) + i) * amount * 0.2f;
        }
        // The Seraph's story, told across her three cinematics: Heaven was perfect, and she kept it so. A rift of the Void
        // opened in its sky - a flaw - and she judged it as she judged every flaw, with her light. The light came back black:
        // the Void ran down her own beam into her halo, and every judgement after it spread the rot she meant to burn away.
        // When she breaks (phase 2) the Void answers her from below, pouring up into her; when she dies it leaves her, back
        // up through the rift, and the rift seals - and she is herself again, for a moment, at the end.

        // a rift in Heaven's sky (in a memory set): a black jag against the sky behind her, a violet bleed round it
        class SerRift { public List<GameObject> segs; public Material glow; public Vector3 c; }
        SerRift SerSkyRift(Transform hs, Vector3 c)
        {
            var r = new SerRift { c = c }; var rng = new System.Random(77); var m = BCBlack(); r.segs = new List<GameObject>();
            foreach (var dir in new[] { V(-0.8f, 0.6f, 0), V(0.9f, -0.4f, 0), V(0.2f, -1, 0) }) r.segs.AddRange(IJag(hs, c, dir, 8, 48, 20, m, rng, 1.1f));
            r.glow = IDiscGlow(hs, c + V(0, 0, -6), 0x7a30ff, 260, 0f, 14);
            foreach (var g in r.segs) g.SetActive(false);
            return r;
        }
        void SerRiftShow(SerRift r, float k)
        {
            int per = r.segs.Count / 3;
            for (int i = 0; i < r.segs.Count; i++) { bool on = (i % per) < Mathf.CeilToInt(k * per - 0.001f); if (r.segs[i].activeSelf != on) r.segs[i].SetActive(on); }
            Mats.SetAlpha(r.glow, 0.09f * k * (0.8f + 0.2f * Mathf.PerlinNoise(BC.t * 2, 5)));
        }
        // the same rift over the real Sanctum: lying flat in the sky overhead (seen from below), made in the arena
        List<GameObject> BCSkyCrack(Vector3 c, float size, int seed, out GameObject glow)
        {
            var rng = new System.Random(seed); var m = BCBlack(); var list = new List<GameObject>();
            for (int arm = 0; arm < 3; arm++)
            {
                float ang = arm * 2.1f + (float)rng.NextDouble(); var a = c;
                for (int i = 0; i < 7; i++)
                {
                    ang += ((float)rng.NextDouble() - 0.5f) * 1.1f; float len = size * (0.12f + (float)rng.NextDouble() * 0.1f);
                    var b = a + V(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * len;
                    var g = Mk.Msh(world, Geo.Box(1, 1, 1), m, (a.x + b.x) / 2, c.y, (a.z + b.z) / 2); g.layer = Mk.LayerIgnore;
                    g.transform.rotation = Quaternion.Euler(0, -ang * Mathf.Rad2Deg, 0); g.transform.localScale = V(len, 0.4f, size * 0.06f * (1 - i / 8f)); g.SetActive(false); list.Add(g); bcLive.Add(g); a = b;
                }
            }
            glow = Mk.Glow(world, 0x7a30ff, size * 1.1f, 0, false); glow.transform.position = c - V(0, 1, 0); glow.layer = Mk.LayerIgnore; bcLive.Add(glow);
            return list;
        }
        void BCCrackShow(List<GameObject> crack, GameObject glow, float k)
        {
            if (crack == null) return;
            for (int i = 0; i < crack.Count; i++) { bool on = (i % 7) < Mathf.CeilToInt(k * 7 - 0.001f); if (crack[i] && crack[i].activeSelf != on) crack[i].SetActive(on); }
            if (glow) Mats.SetAlpha(glow.GetComponent<Renderer>().sharedMaterial, 0.45f * k * (0.8f + 0.2f * Mathf.PerlinNoise(BC.t * 2, 9)));
        }
        float SanctumTop() => roofShut ? RoofY(0) - 1.2f : 70; // (the rift over the Sanctum: under its dome while the dome is shut)
        Material bcInk; Material BCInk => bcInk ? bcInk : (bcInk = Mats.Trans(0x050208, 0.85f)); // (kept: particles may outlive the cinematic)
        void BCInkAt(Vector3 p, Vector3 v, float size, float life) => particles.Add(new Pt { pos = p, scale = Vector3.one * size, mat = BCInk, v = v, life = life, grav = 0 });
        static void BCSpan(Transform t, Vector3 a, Vector3 b, float w) { var d = b - a; t.position = (a + b) / 2; t.rotation = Quaternion.FromToRotation(Vector3.up, d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.up); t.localScale = new Vector3(w, Mathf.Max(0.001f, d.magnitude), w); }

        void DefSeraphIntro()
        {
            var hs = BuildHeaven(0); var O = hs.O; var e = BC.e; var riftC = O + V(60, 290, -720);
            var rift = SerSkyRift(hs.s, riftC);
            var beamM = hs.beam.GetComponent<Renderer>().sharedMaterial;
            var vBeam = IMsh(hs.s, Geo.Cyl(1, 1, 1, 10), BCBlack(), hs.ser).transform; vBeam.gameObject.SetActive(false);  // the black running down her light
            var thread = IMsh(hs.s, Geo.Cyl(1, 1, 1, 8), BCBlack(), hs.ser).transform; thread.gameObject.SetActive(false); // the thread of the Void into her halo
            Vector3 Halo() => hs.se?.sr?.halo != null ? hs.se.sr.halo.t.position : hs.ser + V(0, 14, 0);
            Vector3 Hand() => hs.ser + V(4, 9, 6);
            // 1 - Heaven, perfect: in from beyond its clouds, past the floating temples, to her
            float s1 = BShotAt(7.2f, 0, false, (u, lt, dt) => { SerRiftShow(rift, 0); BPath(VA(O + V(-420, 30, 520), O + V(-260, 60, 300), O + V(-120, 80, 160), O + V(-40, 70, 70)), VA(O + V(-200, 40, 0), O + V(0, 60, -160), O + V(0, 90, -120), hs.ser + V(0, -10, 0)), u); SerFlicker(hs.se, 0, dt); }, 2.0f, 0.05f, null, 48, 0);
            BCue(s1 + 0.2f, "bcHeaven", 0.9f); BCue(s1 + 3.4f, "bcHeaven", 0.7f); BCue(s1 + 0.4f, "iWind", 0.3f); BCue(s1 + 2.0f, "iChoir", 0.6f);
            // 2 - the flaw: low before her, looking up past her into the sky - a hairline of black opens there, bleeding
            // violet; she turns from us to face it, her wings to the camera
            float s2 = BShotAt(6.2f, 0, false, (u, lt, dt) =>
            {
                SerFlicker(hs.se, 0, dt); float open = Smoother(Mathf.Clamp01((lt - 0.8f) / 3.2f)); SerRiftShow(rift, open);
                float turn = Smoother(Mathf.Clamp01((lt - 2.6f) / 2.2f)); if (hs.se?.g) hs.se.g.transform.rotation = Quaternion.Euler(0, 180 * turn, 0);
                if (hs.se?.sr?.head != null) hs.se.sr.head.rx = Mathf.Lerp(0, -0.35f, Smoother(Mathf.Clamp01((lt - 1.6f) / 1.2f)));
                var look = Vector3.Lerp(hs.ser + V(0, 6, 0), riftC, Smoother(Mathf.Clamp01((lt - 0.4f) / 2.8f)) * 0.18f);
                BPath(VA(hs.ser + V(18, -16, 44), hs.ser + V(12, -12, 36), hs.ser + V(8, -10, 32)), VA(hs.ser + V(0, 6, 0), look, look), u, 0, 0, 0.6f);
            }, 0.05f, 0.05f, null, 46);
            BCue(s2 + 0.8f, "iCrack", 0.7f); BCue(s2 + 1.0f, "iVoid", 0.6f); BCue(s2 + 1.6f, "iWhisper", 0.7f); BCue(s2 + 3.0f, "iDistort", 0.4f); BCue(s2 + 2.8f, "iSwell", 0.35f);
            // 3 - her judgement: she raises her light against it - the beam strikes the rift and the black comes back down it,
            // into her halo; and the light that goes on from her hand is black-gold, cutting through Heaven, and Heaven rots
            float s3 = BShotAt(7.0f, 0, false, (u, lt, dt) =>
            {
                SerRiftShow(rift, 1); if (hs.se?.g) hs.se.g.transform.rotation = Quaternion.Euler(0, 180, 0);
                float stain = Smoother(Mathf.Clamp01((lt - 2.6f) / 1.0f)), rot = Smoother(Mathf.Clamp01((lt - 3.0f) / 4.0f));
                SerFlicker(hs.se, stain * 0.5f + rot * 0.4f, dt); HeavenCorrupt(hs, rot * 0.75f); IFallsAt(hs.falls, (lt - 3.4f) * 1.1f);
                // the target: the rift, then sweeping down across the temples
                var tgt = lt < 3.1f ? riftC : Spline(VA(riftC, O + V(-220, 44, -240), O + V(-60, 30, -200), O + V(230, 64, -260), O + V(360, 112, -500)), Mathf.Clamp01((lt - 3.1f) / 3.6f));
                var a = Hand(); float reach = Smoother(Mathf.Clamp01((lt - 0.4f) / 0.6f));
                if (lt > 0.4f) { hs.beam.gameObject.SetActive(true); BCSpan(hs.beam, a, Vector3.Lerp(a, tgt, reach), 2.2f + Mathf.Sin(BC.t * 30) * 0.2f); } else hs.beam.gameObject.SetActive(false);
                Mats.SetColor(beamM, Color.Lerp(Mats.Hex(0xfff0b0), Mats.Hex(0xc07818), Smoother(Mathf.Clamp01((lt - 1.4f) / 1.4f))));
                float down = Smoother(Mathf.Clamp01((lt - 1.3f) / 1.4f)); // the black creeping back down the beam
                if (down > 0) { vBeam.gameObject.SetActive(true); var vb = lt < 3.1f ? Vector3.Lerp(tgt, a, down) : a; BCSpan(vBeam, lt < 3.1f ? tgt : Vector3.Lerp(a, tgt, reach), vb, 0.9f); }
                if (lt > 2.7f) { thread.gameObject.SetActive(true); BCSpan(thread, riftC, Halo(), 0.35f + 0.1f * Mathf.Sin(BC.t * 7)); if (iCues.Add("stain")) { iFlashA = 0.6f; bcShake = 2.2f; } }
                var mid = Vector3.Lerp(hs.ser, tgt, lt < 3.1f ? 0.12f : 0.3f);
                BPath(VA(hs.ser + V(48, -4, 34), hs.ser + V(40, 2, 24), hs.ser + V(52, 12, 40)), VA(hs.ser + V(0, 8, -20), mid, mid), u, lt > 2.6f && lt < 3.6f ? 0.6f : 0.05f + rot * 0.2f);
            }, 0.05f, 0.05f, null, 50, 0.6f);
            BCue(s3 + 0.4f, "holy", 1f); BCue(s3 + 0.5f, "laserCharge", 0.6f); BCue(s3 + 1.3f, "iDistort", 0.8f); BCue(s3 + 2.7f, "iCrack", 1f); BCue(s3 + 2.72f, "iVoid", 1f); BCue(s3 + 2.8f, "bcHeavenDark", 0.9f);
            BCue(s3 + 3.6f, "iCollapse", 0.8f); BCue(s3 + 4.4f, "iThunder", 0.7f); BCue(s3 + 5.2f, "iCollapse", 0.6f); BCue(s3 + 6.0f, "iThunder", 0.5f);
            // 4 - today: she comes down into the Sanctum under a rift in its sky, the light round her gold and black, the Void
            // trailing off her like ink
            List<GameObject> arenaRift = null; GameObject arenaGlow = null;
            float s4 = BShotAt(7.4f, -1, false, (u, lt, dt) =>
            {
                if (e == null || !e.g) return; var bp = bcBossPos0; var f = BCFwd(e); var side = Vector3.Cross(Vector3.up, f);
                float top = SanctumTop(); if (arenaRift == null) arenaRift = BCSkyCrack(V(bp.x, top, bp.z), 34, 20, out arenaGlow);
                BCCrackShow(arenaRift, arenaGlow, Smoother(Mathf.Clamp01(lt / 1.4f)));
                float d = Smoother(Mathf.Clamp01((lt - 0.8f) / 4.4f)); e.g.transform.position = V(bp.x, Mathf.Lerp(Mathf.Min(bp.y, top - 3), 22, d), bp.z); // (out of the rift under the dome, down into the Sanctum)
                var sp = e.g.transform.position;
                if (rand < 0.95f) SpawnParticles(sp + Random.insideUnitSphere * 7, 2, 2, goldSpark, 0.13f, 1.2f, -1);
                for (int i = 0; i < 2; i++) BCInkAt(sp + Random.insideUnitSphere * 3 + V(0, 2, 0), V((rand - 0.5f) * 1.2f, 1.5f + rand * 2.5f, (rand - 0.5f) * 1.2f), 0.35f + rand * 0.4f, 1.6f);
                var g = V(bp.x, Mathf.Max(0, P.pos.y), bp.z);
                BPath(VA(g + f * 30 + side * 8 + V(0, 1.2f, 0), g + f * 26 + side * 4 + V(0, 1.6f, 0), g + f * 22 + side * 2 + V(0, 2.2f, 0)), VA(V(bp.x, top - 2, bp.z), sp + V(0, 2, 0), sp + V(0, 1, 0)), u, 0.06f);
            }, 0.05f, 1.0f, () => { var bp = bcBossPos0; var pm = Mats.Add(0xd8a040, 0.1f, null, true, true); var km = Mats.Trans(0x0a0408, 0.25f); for (int i = 0; i < 6; i++) { var c = Mk.Msh(world, Geo.Cone(1, 1, 16), i % 2 == 0 ? pm : km, bp.x, 40, bp.z); c.transform.rotation = Quaternion.Euler(180, 0, 0); c.transform.localScale = V(6 + i * 3, 80, 6 + i * 3); c.layer = Mk.LayerIgnore; bcLive.Add(c); } }, 52, 1.1f, true);
            BCue(s4 + 0.3f, "iVoid", 0.5f); BCue(s4 + 0.4f, "holy", 1f); BCue(s4 + 0.6f, "iChoir", 0.8f); BCue(s4 + 1.4f, "iWhisper", 0.5f); BCue(s4 + 4.6f, "iBeat", 0.7f);
            BSay(s4 + 2.4f, 3.6f, "Perfection must be preserved. Even if everything else must perish.", "vSeraph");
            BTitle(s4 + 4.6f, "THE CELESTIAL SERAPH", 2.6f);
        }
        // her fall, in one unbroken move paced to the game's own (UpdateSeraphFall, SC.t: rise 0-2, wings 2-3.2, breaking
        // 3.2-5.6, burst 5.6-6.4, descent 6.4-8.2): the Void answers her from below - rifts tear open in the Sanctum floor under
        // her and its smoke and black tendrils pour up into her as she breaks; at the burst they snap, and as she comes down the
        // rifts close, the Void now in her - and the camera ends behind your own eyes, so the fight picks up with no cut
        void DefSeraphPhase2()
        {
            var e = BC.e;
            var cracks = new List<(Transform t, float len, float a)>(); var tends = new List<(Transform t, Vector3 root)>(); Transform pool = null, poolEdge = null; Material edgeM = null;
            Vector3 camP = Vector3.zero, camL = Vector3.zero, landP = Vector3.zero; float fovS = 56, offAt = -1; bool init = false, snapped = false;
            float s1 = BShotAt(9.5f, -1, true, (u, lt, dt) =>
            {
                if (e == null || !e.g) return;
                // the cinematic's clock follows the fall's own (which runs faster as she weakens), so her words and every
                // sound land on their moment; after the fall it runs on in real time for the hold
                if (SC.on) BC.t = Mathf.Max(BC.t, SC.t);
                if (!SC.on && offAt < 0) { offAt = lt; landP = e.pos; } float T = SC.on ? SC.t : 8.2f + (lt - offAt);
                if (!SC.on) { e.pos = landP; e.vel = Vector3.zero; if (e.state == "hover") e.stateT = Mathf.Max(e.stateT, 0.5f); SyncEnt(e); } // (she waits, in place, while her words hang there)
                if (T > 9.35f) { BC.t = Mathf.Max(BC.t, bcTotal); return; }
                var m = e.g.transform.position; var eye = V(SC.lockPos.x, SC.lockPos.y + P.eye, SC.lockPos.z);
                var flat = eye - m; flat.y = 0; flat = flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.forward; var side = Vector3.Cross(Vector3.up, flat); var head = m + V(0, 3, 0);
                float fy = Mathf.Max(Mathf.Max(0, FloorBelow(V(m.x, 30, m.z))), SC.lockPos.y); var c = V(m.x, fy + 0.15f, m.z);
                // the rifts in the floor
                if (cracks.Count == 0)
                {
                    var bm = BCBlack(); var rr = new System.Random(21);
                    var bleed = Mats.Add(0x7a30ff, 0.35f, null, true, true);
                    for (int i = 0; i < 14; i++) { float a = i / 14f * Mathf.PI * 2 + (float)rr.NextDouble() * 0.3f, len = 10 + (float)rr.NextDouble() * 14; var g = Mk.Msh(world, Geo.Box(1.4f + (float)rr.NextDouble() * 0.8f, 0.08f, 1), bm, c.x, c.y, c.z); g.layer = Mk.LayerIgnore; g.transform.rotation = Quaternion.Euler(0, a * Mathf.Rad2Deg, 0); var gl = Mk.Msh(g.transform, Geo.Box(2.6f, 0.5f, 1.04f), bleed, 0, 0.1f, 0); gl.layer = Mk.LayerIgnore; bcLive.Add(g); cracks.Add((g.transform, len, a)); }
                    var pg = Mk.Msh(world, Geo.Cyl(1, 1, 0.08f, 32), bm, c.x, c.y + 0.02f, c.z); pg.layer = Mk.LayerIgnore; bcLive.Add(pg); pool = pg.transform;
                    edgeM = Mats.Add(0x8a40ff, 0.6f, null, true, true); var eg = Mk.Msh(world, Geo.Torus(1, 0.06f, 48, 4), edgeM, c.x, c.y + 0.1f, c.z); eg.transform.rotation = Quaternion.Euler(90, 0, 0); eg.layer = Mk.LayerIgnore; bcLive.Add(eg); poolEdge = eg.transform;
                    for (int i = 0; i < 9; i++) { var tg = Mk.Msh(world, Geo.Box(0.5f, 0.5f, 1), bm, c.x, c.y, c.z); tg.layer = Mk.LayerIgnore; tg.SetActive(false); bcLive.Add(tg); float a = i * 0.7f; tends.Add((tg.transform, c + V(Mathf.Cos(a) * (3 + i % 3 * 2.5f), 0.1f, Mathf.Sin(a) * (3 + i % 3 * 2.5f)))); }
                }
                float open = Smoother(Mathf.Clamp01((T - 0.8f) / 2.4f)) * (1 - Smoother(Mathf.Clamp01((T - 6.6f) / 1.5f)));
                foreach (var cr in cracks) { float Ln = cr.len * open; cr.t.localScale = V(1, 1, Mathf.Max(0.01f, Ln)); cr.t.position = c + V(Mathf.Sin(cr.a) * Ln / 2, 0, Mathf.Cos(cr.a) * Ln / 2); cr.t.gameObject.SetActive(open > 0.01f); }
                float pr = Mathf.Max(0.01f, 7 * open); pool.localScale = V(pr, 1, pr); poolEdge.localScale = V(pr, pr, pr); Mats.SetAlpha(edgeM, 0.6f * open * (0.75f + 0.25f * Mathf.Sin(BC.t * 6)));
                // the Void pouring up out of them into her
                if (open > 0.05f && T < 5.6f) for (int i = 0; i < 3; i++) { var cr = cracks[Random.Range(0, cracks.Count)]; float dd = rand * cr.len * open; var p = c + V(Mathf.Sin(cr.a) * dd, 0.3f, Mathf.Cos(cr.a) * dd); var to = m - p; float sp = 10 + rand * 6; BCInkAt(p, to.normalized * sp, 0.4f + rand * 0.6f, to.magnitude / sp); }
                float reach = Smoother(Mathf.Clamp01((T - 3.0f) / 1.4f));
                for (int i = 0; i < tends.Count; i++)
                {
                    var (tt, root) = tends[i]; bool on = T > 3.0f && T < 5.6f; if (tt.gameObject.activeSelf != on) tt.gameObject.SetActive(on); if (!on) continue;
                    var to = m + V(Mathf.Sin(BC.t * 2 + i) * 1.2f, Mathf.Cos(BC.t * 1.6f + i) * 1.2f, 0) - root; tt.position = root + to * reach * 0.5f; tt.rotation = Quaternion.LookRotation(to.normalized) * Quaternion.Euler(Mathf.Sin(BC.t * 5 + i) * 4, Mathf.Cos(BC.t * 4 + i) * 4, 0); float w = 0.5f + 0.3f * Mathf.Sin(BC.t * 9 + i); tt.localScale = V(w, w, Mathf.Max(0.01f, to.magnitude * reach));
                }
                if (T >= 5.6f && !snapped) { snapped = true; for (int i = 0; i < 40; i++) BCInkAt(m + Random.insideUnitSphere * 2, Random.onUnitSphere * (8 + rand * 10), 0.5f + rand * 0.6f, 1.2f); }
                if (T > 6.4f) for (int i = 0; i < 2; i++) BCInkAt(m + Random.insideUnitSphere * 2 + V(0, 1.5f, 0), V((rand - 0.5f) * 1, 2 + rand * 2, (rand - 0.5f) * 1), 0.3f + rand * 0.35f, 1.4f); // trailing ink as she comes down
                // the camera: each stage's framing, eased into one another - nothing jumps
                Vector3 pos, look; float fov;
                if (T < 2.0f) { pos = m + flat * 26 + side * 8 + V(0, -5, 0); look = Vector3.Lerp(m + V(0, 1, 0), c, 0.5f); fov = 58; }   // (her and the floor under her, where the Void breaks through)
                else if (T < 3.2f) { pos = m + flat * 19 + side * 4 + V(0, -3, 0); look = Vector3.Lerp(m + V(0, 2, 0), c, 0.3f); fov = 60; }
                else if (T < 5.6f) { float k = (T - 3.2f) / 2.4f, a = Mathf.Lerp(-0.7f, 0.7f, k); var d = Quaternion.AngleAxis(a * Mathf.Rad2Deg, Vector3.up) * flat; pos = head + d * Mathf.Lerp(11, 7.5f, k) + V(0, -1.2f, 0); look = Vector3.Lerp(head, c, 0.15f * (1 - k)); fov = 46; }
                else if (T < 6.4f) { pos = m + flat * 30 - side * 5 + V(0, 1, 0); look = m + V(0, 1.5f, 0); fov = 64; }
                else { float k = Smoother(Mathf.Clamp01((T - 6.4f) / 2.85f)); pos = Vector3.Lerp(m + flat * 24 + V(0, 2, 0), eye, k); look = m + V(0, 2, 0); fov = Mathf.Lerp(62, setFov > 0 ? setFov : 80, k); } // (down with her and home to your eyes by the end of the hold - one unhurried move)
                pos.y = Mathf.Max(pos.y, fy + 1.0f); bcCamFree = T > 7.6f; // (the last of the move goes straight to your eyes, columns or not)
                float rate = T > 6.4f ? 2.6f + (T - 6.4f) * 2.4f : 2.6f, s = init ? 1 - Mathf.Exp(-rate * dt) : 1; init = true;
                camP = Vector3.Lerp(camP, pos, s); camL = Vector3.Lerp(camL, look, s); fovS = Mathf.Lerp(fovS, fov, s); cineCam.fieldOfView = fovS;
                BPath(VA(camP, camP), VA(camL, camL), u, T > 3.2f && T < 6.4f ? 0.25f : 0.08f, 0, T > 7.4f ? 0.2f : 0.7f);
            }, 0.3f, 0.05f, null, 56, 0);
            BCue(s1 + 0.9f, "iCrack", 0.8f); BCue(s1 + 1.0f, "iVoid", 0.8f); BCue(s1 + 1.8f, "iWhisper", 0.8f); BCue(s1 + 3.0f, "iDistort", 0.8f); BCue(s1 + 3.2f, "bcHeavenDark", 0.9f);
            BCue(s1 + 4.2f, "iWhisper", 0.6f); BCue(s1 + 5.6f, "iCollapse", 0.9f); BCue(s1 + 6.0f, "iThunder", 0.7f); BCue(s1 + 6.8f, "iVoid", 0.4f);
            BSay(s1 + 5.9f, 3.3f, "If perfection cannot exist... THEN ALL SHALL FALL!", "vSeraph"); // (held long enough to read; the camera waits at your eyes until it fades)
        }
        void DefSeraphDefeat()
        {
            var e = BC.e; var d0 = bossDeath; var base0 = d0 != null ? d0.basePos : (e != null ? e.pos : V(0, 10, 0));
            // the rift she brought down over the Sanctum, still there: the Void leaves her through it as she dies
            var riftC = V(base0.x, Mathf.Max(Mathf.Max(base0.y, 10) + 14, Mathf.Min(SanctumTop(), base0.y + 60)), base0.z); List<GameObject> rift = null; GameObject riftGlow = null;
            void Rift() { if (rift == null) rift = BCSkyCrack(riftC, 44, 22, out riftGlow); }
            // 1 - her death as the game plays it (she rises screaming, god-rays tear out of her, the halo shatters, the wings rip
            // away, everything collapses inward) - and the Void streaming out of her, up into its rift - to the supernova; the
            // white then holds for her story
            float s1 = BShotAt(9.0f, -1, true, (u, lt, dt) =>
            {
                Rift(); BCCrackShow(rift, riftGlow, Smoother(Mathf.Clamp01(lt / 1.2f)));
                var d = bossDeath; var m = e != null && e.g && e.g.activeSelf ? e.g.transform.position : (d != null ? d.basePos + V(0, 8, 0) : V(0, 10, 0));
                var f = BCFwd(e ?? new Ent()); var side = Vector3.Cross(Vector3.up, f); var g = V(m.x, 0, m.z);
                int n = 1 + (int)(lt * 0.6f); for (int i = 0; i < n; i++) { var p = m + Random.insideUnitSphere * 2.5f; var to = riftC - p; float sp = 14 + rand * 8; BCInkAt(p, to.normalized * sp, 0.4f + rand * 0.5f, to.magnitude / sp); }
                BPath(VA(g + f * 28 + side * 8 + V(0, 1.4f, 0), g + f * 24 + side * 4 + V(0, 3, 0), g + f * 22 + side * 2 + V(0, 5, 0)), VA(m, m + V(0, 3, 0), m + V(0, 4, 0)), Mathf.Clamp01(lt / 5.6f), 0.2f + lt * 0.05f);
                if (d != null && d.nova && lt > 0.5f) { if (iCues.Add("nova")) { iFlashA = 1; } if (d.novaT > 0.6f) BCNextShot(); }
            }, 0.3f, 0.05f, null, 54, 0);
            BCue(s1 + 0.1f, "iChoir", 0.6f); BCue(s1 + 0.6f, "iVoid", 0.7f); BCue(s1 + 2.0f, "iWhisper", 0.6f);
            // 2 - the white clears: the Sanctum after; she lies broken where she burned; the last thread of the Void draws up out
            // of her into the rift, and the rift seals
            Ent rem = null; GameObject lastLight = null; Vector3 ground = Vector3.zero; Transform thread = null; var remCols = new List<(Material m, Color c)>();
            void Remnant()
            {
                if (rem != null) return; var d = bossDeath; var c = d != null ? d.basePos : base0; ground = V(c.x, Mathf.Max(0, FloorBelow(V(c.x, 30, c.z))) + 0.6f, c.z);
                int pl = Mk.layer; rem = NewEnt("seraphRemnant"); Mk.colliders = false; Mk.layer = 0; try { rem.sr = BuildSeraph(rem); } catch (Exception) { } Mk.layer = pl;
                Mk.colliders = false; foreach (var col in rem.g.GetComponentsInChildren<Collider>(true)) Destroy(col); var rb = rem.g.GetComponent<Rigidbody>(); if (rb) Destroy(rb);
                rem.g.transform.position = ground; rem.g.transform.rotation = Quaternion.Euler(-78, 30, 12); rem.g.transform.localScale = Vector3.one * (e != null && e.g ? e.g.transform.localScale.x : 3);
                if (rem.sr != null) { foreach (var m2 in rem.sr.mats) if (m2) { var c0 = m2.GetColor("_BaseColor"); remCols.Add((m2, c0)); Mats.SetColor(m2, c0 * 0.3f); } for (int i = 0; i < rem.sr.wings.Count; i++) if (rem.sr.wings[i] != null) rem.sr.wings[i].Scale(i % 3 == 0 ? 0.5f : 0.001f); if (rem.sr.halo != null) rem.sr.halo.go.SetActive(false); if (rem.sr.eyeGlow) rem.sr.eyeGlow.SetActive(false); }
                bcLive.Add(rem.g);
                lastLight = Mk.Glow(world, 0xfff0c0, 10, 0.6f); lastLight.transform.position = ground + V(0, 2, 0); bcLive.Add(lastLight);
                var tg = Mk.Msh(world, Geo.Cyl(1, 1, 1, 8), BCBlack(), ground.x, ground.y, ground.z); tg.layer = Mk.LayerIgnore; bcLive.Add(tg); thread = tg.transform;
            }
            float s2 = BShotAt(6.4f, -1, false, (u, lt, dt) =>
            {
                Rift(); Remnant(); var f = BCFwd(e ?? rem); var side = Vector3.Cross(Vector3.up, f); var chest = ground + V(0, 1.2f, 0);
                if (lt < 0.1f) iFlashA = 1;
                float pull = Smoother(Mathf.Clamp01((lt - 1.4f) / 2.4f)), seal = Smoother(Mathf.Clamp01((lt - 3.4f) / 1.6f));
                if (thread) { thread.gameObject.SetActive(pull < 1); BCSpan(thread, Vector3.Lerp(chest, riftC, pull), riftC, 0.5f * (1 - pull * 0.6f)); }
                if (pull < 1 && rand < 0.8f) { var p = Vector3.Lerp(chest, riftC, pull + rand * (1 - pull)); BCInkAt(p, V(0, 6, 0), 0.4f, 0.8f); }
                BCCrackShow(rift, riftGlow, 1 - seal);
                if (seal >= 1 && iCues.Add("sealed")) { iFlashA = Mathf.Max(iFlashA, 0.25f); }
                if (rand < 0.9f) particles.Add(new Pt { pos = ground + V((rand - 0.5f) * 30, 18 + rand * 10, (rand - 0.5f) * 30), scale = V(0.25f, 0.05f, 0.5f), mat = rand < 0.7f ? goldSpark : ashMat, v = V((rand - 0.5f) * 1, -0.8f - rand * 0.6f, (rand - 0.5f) * 1), life = 6, grav = 0, hasSpin = true, spin = V(rand * 4, rand * 4, rand * 4) });
                BPath(VA(ground + f * 13 + side * 5 + V(0, 2.4f, 0), ground + f * 11 + side * 4.5f + V(0, 2.8f, 0), ground + f * 9 + side * 4 + V(0, 3.4f, 0)), VA(riftC, Vector3.Lerp(chest, riftC, 0.4f), ground + V(0, 1, 0)), u, 0, 0, 0.5f);
            }, 0.05f, 0.05f, null, 50, 0);
            BCue(s2 + 0.3f, "iSwell", 0.4f); BCue(s2 + 1.4f, "iWhisper", 0.7f); BCue(s2 + 1.5f, "iVoid", 0.6f); BCue(s2 + 4.8f, "iCrack", 0.6f); BCue(s2 + 5.0f, "bcHeavenSad", 0.9f);
            // 3 - the memory: Heaven, flawless, and her in it, whole and shining - the hairline rift opening in the sky behind
            // her, where it all began - and then all of it fallen
            var hs = BuildHeaven(0); var O = hs.O; var mRift = SerSkyRift(hs.s, O + V(60, 290, -720));
            float s3 = BShotAt(7.6f, 0, false, (u, lt, dt) =>
            {
                bool fallen = lt > 3.8f;
                if (lt > 3.7f && lt - dt <= 3.7f && !bcCapturing) StartCoroutine(BCCapture());
                if (fallen && iCues.Add("heavenFall")) { HeavenCorrupt(hs, 1); IFallsAt(hs.falls, 9); hs.se.g.SetActive(false); bcXfT = 1.8f; bcXfDur = 1.8f; if (bcXfTex && bcXf) bcXf.texture = bcXfTex; }
                SerRiftShow(mRift, fallen ? 1 : Smoother(Mathf.Clamp01((lt - 1.4f) / 2.0f)));
                if (fallen) HeavenCorrupt(hs, 1); else SerFlicker(hs.se, 0, dt);
                BPath(VA(O + V(-50, 56, 130), O + V(-30, 66, 100), O + V(-20, 72, 80)), VA(hs.ser + V(0, -12, 0), hs.ser + V(0, -6, 0), hs.ser + V(0, -2, 0)), u);
            }, 0.05f, 0.05f, null, 48, 1.2f);
            BCue(s3 + 0.2f, "bcHeaven", 0.6f); BCue(s3 + 0.4f, "iChoir", 0.5f); BCue(s3 + 1.5f, "iCrack", 0.35f); BCue(s3 + 3.8f, "iThunder", 0.7f); BCue(s3 + 3.9f, "iWind", 0.7f); BCue(s3 + 4.2f, "bcHeavenDark", 0.6f);
            // 4 - herself again, at the end: the black gone out of her, porcelain and gold coming back; the last light fading
            float s4 = BShotAt(7.4f, -1, false, (u, lt, dt) =>
            {
                Remnant(); var r = rem.sr; float pure = Smoother(Mathf.Clamp01((lt - 0.4f) / 3.2f));
                float fade = 1 - Smoother(Mathf.Clamp01((lt - 4.4f) / 2.6f)); // (and at the very end her light goes out)
                foreach (var (m, c) in remCols) if (m) { Mats.SetColor(m, Color.Lerp(c * 0.3f, c, pure)); Mats.Emissive(m, 0xffe2b0, 0.32f * pure * fade); } // porcelain and gold again, warm with her own light
                // her halo coming back: a flicker, then whole, held through her last words
                if (r?.halo != null) { bool on = pure > 0.55f && (pure > 0.8f || Mathf.PerlinNoise(BC.t * 14, 3) > 0.45f) && fade > 0.15f; if (r.halo.go.activeSelf != on) r.halo.go.SetActive(on); if (on && iCues.Add("haloBack")) iFlashA = Mathf.Max(iFlashA, 0.12f); }
                if (lastLight) { Mats.SetAlpha(lastLight.GetComponent<Renderer>().sharedMaterial, 0.85f * (0.35f + 0.65f * pure) * fade); }
                if (r?.head != null) r.head.rx = Mathf.Lerp(r.head.rx, -0.8f, 1 - Mathf.Exp(-0.8f * dt));
                var f = BCFwd(e ?? rem); var side = Vector3.Cross(Vector3.up, f); var hp = r?.head != null ? r.head.t.position : ground + V(0, 1, 0);
                BPath(VA(hp + f * 9 + side * 4 + V(0, 3, 0), hp + f * 4.6f + side * 2 + V(0, 1.4f, 0), hp + f * 4 + side * 1.5f + V(0, 4, 0)), VA(hp, hp, hp + V(0, 30, -6)), u, 0, 0, 0.5f);
                if (rand < 0.5f + pure * 0.4f) particles.Add(new Pt { pos = hp + V((rand - 0.5f) * 10, 8, (rand - 0.5f) * 10), scale = V(0.25f, 0.05f, 0.5f), mat = goldSpark, v = V(0, -0.6f, 0), life = 6, grav = 0, hasSpin = true, spin = V(rand * 4, rand * 4, rand * 4) });
                if (pure > 0.3f && rand < 0.4f) SpawnParticles(hp + Random.insideUnitSphere * 1.5f, 1, 0.3f, goldSpark, 0.06f, 1.6f, -1.2f); // motes of her old light rising off her
            }, 0.05f, 0.05f, null, 42);
            BCue(s4 + 0.6f, "iChoir", 0.35f); BCue(s4 + 2.2f, "holy", 0.35f); BCue(s4 + 2.4f, "iSwell", 0.4f);
            BSay(s4 + 2.0f, 3.6f, "I only wanted... perfection.", "vSeraph");
            // the white takes everything: into the ending
            float s5 = BShotAt(1.6f, -1, false, (u, lt, dt) => { iFlashA = Mathf.Max(iFlashA, Smoother(u)); var hp = rem?.sr?.head != null ? rem.sr.head.t.position : ground; BPath(VA(hp + V(0, 6, 4), hp + V(0, 10, 4)), VA(hp + V(0, 30, -6), hp + V(0, 50, -6)), u); }, 0.05f, 0.05f, null, 42);
            BCue(s5 + 0.2f, "iSwell", 0.5f);
        }

        // ================= THE WARDEN OF HOURS =================
        class ClockSet { public Transform s, face, hourH, minH, diorama, dVoid; public Vector3 O, warden, dio; public Ent we; public List<IFall> dFalls; public List<(Transform t, float sp)> gears = new List<(Transform, float)>(); public Material faceM, ringM; }
        ClockSet BuildClockwork(int set)
        {
            var cs = new ClockSet(); var s = ISet(set); cs.s = s; var O = s.position; cs.O = O; var rng = new System.Random(130 + set);
            float R(float a, float b) => a + (b - a) * (float)rng.NextDouble();
            IArchMats(); var brass = IM(Mats.Std(0xb08840, 0.32f, 0.88f)); var iron = IM(Mats.Std(0x3a3a40, 0.5f, 0.8f)); var copper = IM(Mats.Std(0xa85a38, 0.4f, 0.8f));
            for (int k = 0; k < 30; k++)
            {
                float r = R(6, 70); var c = O + V(R(-280, 280), R(-60, 240), R(-560, -30)); if (k == 0) { r = 14; c = O + V(-14, 26, 10); }
                var g = new GameObject("gear").transform; g.SetParent(s, false); g.position = c; g.rotation = k == 0 ? Quaternion.identity : Quaternion.Euler(R(-15, 15), R(-30, 30), 0); if (k == 0) IGlow(s, c + V(6, 6, 14), 0xffc080, 40, 0.3f);
                var m = k % 3 == 0 ? iron : k % 3 == 1 ? brass : copper;
                IA(g, Geo.Cyl(r, r, r * 0.12f, 48), m, c, g.eulerAngles + V(90, 0, 0)); IA(g, Geo.Cyl(r * 0.25f, r * 0.25f, r * 0.2f, 16), iron, c, g.eulerAngles + V(90, 0, 0));
                for (int i = 0; i < 6; i++) IA(g, Geo.Box(r * 0.06f, r * 1.4f, r * 0.13f), iron, c, g.eulerAngles + V(0, 0, i * 30));
                int teeth = Mathf.Clamp(Mathf.RoundToInt(r * 1.2f), 10, 70);
                for (int i = 0; i < teeth; i++) { float a = i * 360f / teeth; var tt = IA(g, Geo.Box(r * 0.12f, r * 0.16f, r * 0.1f), m, c, g.eulerAngles + V(0, 0, a)).transform; tt.position = g.TransformPoint(Quaternion.Euler(0, 0, a) * V(0, r * 1.05f, 0)); }
                cs.gears.Add((g, R(-0.4f, 0.4f) * (30 / r)));
            }
            for (int k = 0; k < 16; k++) IA(s, Geo.Box(R(3, 6), R(80, 260), R(3, 6)), iron, O + V(R(-240, 240), R(40, 160), R(-480, -60)), V(R(-20, 20), 0, R(-30, 30)));
            for (int k = 0; k < 5; k++) { var pv = IRoot(s, O + V(-120 + k * 60, 180, -200 - k * 30)); IA(pv, Geo.Box(1, 90, 1), iron, L(pv, 0, -45, 0)); IA(pv, Geo.Cyl(8, 8, 2, 24), brass, L(pv, 0, -92, 0), V(90, 0, 0)); iSway.Add((pv, 22, 0.8f + k * 0.1f, k, Vector3.forward)); } // pendulums
            for (int k = 0; k < 10; k++) { var pv = IRoot(s, O + V(R(-200, 200), 220, R(-400, -60))); for (int j = 0; j < 14; j++) IA(pv, Geo.Torus(1.2f, 0.25f, 8, 4), iron, L(pv, 0, -j * 2.2f, 0), V(0, j % 2 == 0 ? 0 : 90, 0)); iSway.Add((pv, 4, 0.5f + k * 0.07f, k, Vector3.forward)); } // chains
            var steam = ISmoke(0xe0e0e0, 0.3f); for (int k = 0; k < 8; k++) IChimney(set, O + V(R(-160, 160), R(0, 60), R(-300, -40)), steam, 5, 6);
            var face = new GameObject("clock").transform; face.SetParent(s, false); face.position = O + V(0, 70, -140); cs.face = face;
            cs.faceM = IM(Mats.Std(0xffffff, 0.5f, 0.2f, 0, ClockTex())); IA(face, Geo.Cyl(48, 48, 3, 64), cs.faceM, face.position, V(90, 0, 0));
            IA(face, Geo.Torus(49, 2.4f, 64, 6), brass, face.position + V(0, 0, 1.6f)); IA(face, Geo.Torus(54, 1.4f, 64, 4), iron, face.position + V(0, 0, 0.4f));
            cs.ringM = IM(Mats.Add(0xffd080, 0.5f, null, true)); for (int i = 0; i < 12; i++) { float a = i * Mathf.PI / 6; IMsh(face, Geo.Box(2, 6, 0.3f), cs.ringM, face.position + V(Mathf.Sin(a) * 40, Mathf.Cos(a) * 40, 2.2f), V(0, 0, -a * Mathf.Rad2Deg)); }
            cs.hourH = new GameObject("h").transform; cs.hourH.SetParent(face, false); cs.hourH.position = face.position + V(0, 0, 2.5f); IA(cs.hourH, Geo.Box(2.6f, 26, 0.6f), iron, cs.hourH.position + V(0, 12, 0));
            cs.minH = new GameObject("m").transform; cs.minH.SetParent(face, false); cs.minH.position = face.position + V(0, 0, 3.2f); IA(cs.minH, Geo.Box(1.6f, 38, 0.5f), iron, cs.minH.position + V(0, 18, 0));
            IRays(s, O + V(60, 240, -100), V(-0.3f, -1, 0.4f), 6, 300, 10, 0xffe0a0, 0.03f, rng);
            cs.warden = O + V(0, 0, -60);
            var we = NewEnt("cineWarden"); Mk.colliders = false; we.g.transform.SetParent(s, false); we.g.transform.position = cs.warden;
            try { var r = BuildWarden(we); we.wd = r; foreach (var sp in r.spins) iSpins.Add(sp); } catch (Exception ex) { Debug.LogWarning("cine warden: " + ex.Message); }
            Mk.colliders = false; foreach (var c in we.g.GetComponentsInChildren<Collider>(true)) Destroy(c); var rb = we.g.GetComponent<Rigidbody>(); if (rb) Destroy(rb);
            we.g.transform.rotation = Quaternion.Euler(0, 0, 0); we.boss = 5; cs.we = we;
            cs.dio = O + V(0, 30, 40); var dio = IRoot(s, cs.dio); cs.diorama = dio;
            IA(dio, Geo.Cyl(26, 24, 3, 48), iStone, L(dio, 0, -1.5f, 0)); IA(dio, Geo.Cone(22, 18, 12), iStoneDk, L(dio, 0, -12, 0), V(180, 0, 0));
            IA(dio, Geo.Torus(26.5f, 0.5f, 56, 4), cs.ringM, L(dio, 0, 0, 0), V(90, 0, 0));
            for (int k = 0; k < 22; k++) { float a = k * 0.29f, r = R(4, 22); var p = L(dio, Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r); var t = k % 3 == 0 ? ISpire(dio, p, R(12, 24), k) : IHouse(dio, p, R(0, 360), R(2.4f, 4), R(2.4f, 3.6f), R(2, 4)); IFallReg(t, R(0.3f, 1.6f), 10, rng); }
            for (int k = 0; k < 4; k++) { float a = k * Mathf.PI / 2 + 0.4f; var g = IRoot(dio, L(dio, Mathf.Cos(a) * 16, 0, Mathf.Sin(a) * 16)); IStatue(g, 0, 0, 0, 7); IFallReg(g, 1.0f + k * 0.2f, 7, rng); }
            cs.dFalls = ITakeFalls();
            cs.dVoid = IMsh(dio, Geo.Sphere(1, 24, 16), BCBlack(), L(dio, -10, 16, -16)).transform; cs.dVoid.localScale = Vector3.zero;
            ISnow(set, O + V(0, 60, -60), V(180, 90, 140), 220, 0xffd890, 0.5f, V(0, 0.4f, 0));
            BEnv(set, 0x0a0a0c, 60, 700, 0x2a2a34, 0x2a2420, 0x0a0806, 0xffd8a0, 1.3f, 0x030304, V(-0.5f, -0.4f, -0.75f), 0xfff0d8, 0.15f, 1.4f);
            return cs;
        }
        void DioFate(ClockSet cs, float k) { IFallsAt(cs.dFalls, k * 3.4f); cs.dVoid.localScale = Vector3.one * Mathf.Lerp(0.001f, 11, Smoother(k)); }
        void ClockSpin(ClockSet cs, float speed, float dt)
        {
            foreach (var g in cs.gears) if (g.t) g.t.Rotate(0, 0, g.sp * 60 * speed * dt, Space.Self);
            cs.minH.Rotate(0, 0, -36 * speed * dt, Space.Self); cs.hourH.Rotate(0, 0, -3 * speed * dt, Space.Self);
            foreach (var sp in iSpins) if (sp.t) sp.t.Rotate(sp.axis, sp.sp * 57.3f * speed * dt, Space.Self);
        }
        void RewindSparks(Vector3 c, float r)
        {
            for (int i = 0; i < 3; i++) { var d = Random.onUnitSphere; particles.Add(new Pt { pos = c + d * r, scale = Vector3.one * 0.25f, mat = goldSpark, v = -d * r * 1.2f, life = 0.8f, grav = 0 }); }
        }
        void DefWardenIntro()
        {
            var cs = BuildClockwork(0); var O = cs.O; var e = BC.e; float H = 34;
            var wr = cs.we.wd; Color face0 = wr != null && wr.faceM ? wr.faceM.GetColor("_BaseColor") : Color.white;
            float s1 = BShotAt(7.4f, 0, false, (u, lt, dt) =>
            {
                ClockSpin(cs, 0.3f, dt); var g0 = cs.gears[0].t.position;
                float rev = Smoother(Mathf.Clamp01((lt - 1.6f) / 5.8f));
                BPath(VA(g0 + V(5, 7, 15), g0 + V(16, 12, 30), O + V(40, 40, 110), O + V(20, 46, 140)), VA(g0 + V(0, 8, 0), g0 + V(-4, 6, 0), O + V(0, 50, -80), O + V(0, 56, -120)), rev);
                if (wr != null && wr.faceM) Mats.SetColor(wr.faceM, face0 * 0.12f);
            }, 3.0f, 0.05f, null, 46, 0);
            for (int i = 0; i < 8; i++) { BCue(0.4f + i * 0.9f, "tick", 0.75f - i * 0.04f); BCue(0.4f + i * 0.9f + 0.28f, "tick", 0.2f); } BCue(s1 + 3.2f, "iCreak", 0.5f); BCue(s1 + 4.2f, "rumble", 0.4f); BCue(s1 + 5.0f, "bcClock", 0.7f);
            float s2 = BShotAt(5.6f, 0, false, (u, lt, dt) =>
            {
                ClockSpin(cs, 0.3f, dt); try { AnimateWarden(cs.we, dt, 1); } catch (Exception) { }
                if (wr != null && wr.faceM) Mats.SetColor(wr.faceM, Color.Lerp(face0 * 0.12f, face0 * 1.4f, Smoother(Mathf.Clamp01((lt - 0.6f) / 1.4f))));
                if (wr?.root != null) wr.root.t.localRotation = Quaternion.Euler(8, 0, 0);
                int v = Mathf.Clamp((int)((lt - 2.0f) / 1.2f), 0, 2); DioFate(cs, v == 0 ? 0 : v == 1 ? 0.55f : 1);
                if ((int)((lt - 2.0f) / 1.2f) != (int)((lt - 2.0f - dt) / 1.2f) && lt > 2.0f) { iFlashA = 0.25f; }
                if (lt < 2.0f) BPath(VA(cs.warden + V(4, 18, 64), cs.warden + V(2, 22, 50)), VA(cs.warden + V(0, 24, 0), cs.warden + V(0, 26, 0)), lt / 2.0f);
                else BPath(VA(cs.warden + V(-30, 34, 72), cs.warden + V(-24, 34, 64)), VA(cs.dio + V(0, 6, 0), cs.dio + V(0, 6, 0)), (lt - 2.0f) / 3.6f);
            }, 0.05f, 0.05f, null, 46);
            BCue(s2 + 0.6f, "iSwell", 0.4f); BCue(s2 + 2.0f, "iWhisper", 0.6f); BCue(s2 + 2.0f, "iDistort", 0.3f); BCue(s2 + 3.2f, "iDistort", 0.3f); BCue(s2 + 4.4f, "iDistort", 0.3f);
            float s3 = BShotAt(9.0f, 0, false, (u, lt, dt) =>
            {
                float[] seg = { 0, 3.4f, 6.4f, 9.0f }; int a = lt < seg[1] ? 0 : lt < seg[2] ? 1 : 2; float la = lt - seg[a], len = seg[a + 1] - seg[a];
                float destroy = Mathf.Clamp01(la / (len * 0.5f)), rewind = Mathf.Clamp01((la - len * 0.55f) / (len * 0.4f));
                float fate = a < 2 ? destroy * (1 - rewind) : destroy * (1 - rewind * 0.35f); DioFate(cs, fate);
                bool rew = la > len * 0.55f && (a < 2 || rewind < 0.35f);
                ClockSpin(cs, rew ? -14 : 0.6f, dt); if (rew) RewindSparks(cs.dio + V(0, 8, 0), 26);
                try { AnimateWarden(cs.we, dt, 1); } catch (Exception) { }
                float tired = 1 - a * 0.3f; if (wr?.rSh != null) wr.rSh.rx = rew ? Mathf.Lerp(wr.rSh.rx, -2.4f * tired, 1 - Mathf.Exp(-4 * tired * dt)) : Mathf.Lerp(wr.rSh.rx, 0, 1 - Mathf.Exp(-2 * dt));
                if (wr?.root != null) wr.root.t.localRotation = Quaternion.Euler(8 + a * 7, 0, 0);
                if (la < dt * 1.5f) iFlashA = 0.3f;
                if (a % 2 == 0) BPath(VA(cs.dio + V(28, 20, 36), cs.dio + V(20, 15, 28)), VA(cs.dio + V(0, 4, 0), cs.dio + V(0, 5, 0)), la / len, rew ? 0.3f : 0.1f);
                else BPath(VA(cs.warden + V(36, 16, 56), cs.warden + V(28, 18, 46)), VA(cs.warden + V(0, 26, 0), cs.dio), la / len, rew ? 0.3f : 0.1f);
            }, 0.05f, 0.05f, null, 46);
            foreach (var (at, isRew) in new[] { (0.1f, false), (1.9f, true), (3.5f, false), (5.0f, true), (6.5f, false), (7.9f, true) }) { BCue(s3 + at, isRew ? "rewind" : "iCollapse", 0.75f); if (isRew) BCue(s3 + at + 0.4f, "rewind", 0.3f); }
            for (int i = 0; i < 10; i++) BCue(s3 + 0.2f + i * 0.9f, "tick", 0.5f);
            float s4 = BShotAt(7.0f, -1, false, (u, lt, dt) =>
            {
                if (e == null || !e.g) return; var r = e.wd; var bp = bcBossPos0; var f = BCFwd(e); var side = Vector3.Cross(Vector3.up, f);
                float turn = Smoother(Mathf.Clamp01((lt - 0.8f) / 2.6f)); e.g.transform.rotation = Quaternion.AngleAxis(Mathf.Lerp(150, 0, turn), Vector3.up) * bcBossRot0;
                if (r != null && r.faceM) Mats.SetColor(r.faceM, Color.Lerp(Color.white * 0.2f, Color.white * 1.5f, Smoother(Mathf.Clamp01((lt - 2.6f) / 1.2f))));
                BPath(VA(bp + f * 40 + side * 10 + V(0, 4, 0), bp + f * 30 + side * 6 + V(0, 6, 0), bp + f * 21 + side * 2 + V(0, 9, 0)), VA(bp + V(0, H * 0.55f, 0), bp + V(0, H * 0.62f, 0), bp + V(0, H * 0.72f, 0)), u, 0.05f);
            }, 0.05f, 1.0f, null, 50, 1.0f, true);
            for (int i = 0; i < 9; i++) BCue(s4 + 0.2f + i * 0.72f, "tick", 0.45f + i * 0.08f);
            BSay(s4 + 2.6f, 3.4f, "I've watched this world die more times than you could ever understand.", "vWarden");
            BTitle(s4 + 4.4f, "THE WARDEN OF HOURS", 2.5f);
        }
        void DefWardenPhase2()
        {
            var e = BC.e; var r = e?.wd; Color face0 = r != null && r.faceM ? r.faceM.GetColor("_BaseColor") : Color.white;
            Vector3 Fp() => r?.face != null ? r.face.t.position : (e != null && e.g ? e.g.transform.position + V(0, 26, 0) : Vector3.zero);
            Vector3 Bp() => e != null && e.g ? e.g.transform.position : Vector3.zero;
            // the camera stands off toward the middle of the hall (the most room) and low, so he towers whole in frame
            Vector3 WAway() { var d = -Bp(); d.y = 0; var f0 = e != null ? BCFwd(e) : Vector3.forward; return d.magnitude < 12 ? f0 : Vector3.Slerp(f0, d.normalized, 0.6f).normalized; }
            float s1 = BShotAt(2.8f, -1, false, (u, lt, dt) =>
            {
                if (r == null) return; float sp = 1 + u * u * 16; foreach (var s in r.spins) if (s.t) s.t.Rotate(s.axis, s.sp * 57.3f * sp * dt, Space.Self);
                if (r.faceM) Mats.SetColor(r.faceM, face0 * (1 + u * 1.8f)); if (r.root != null) r.root.t.localPosition += Random.insideUnitSphere * 0.02f * u;
                var f = WAway(); var side = Vector3.Cross(Vector3.up, f); var bp = Bp(); BPath(VA(WardenInHall(bp + f * 58 + side * 16 + V(0, 3, 0)), WardenInHall(bp + f * 47 + side * 9 + V(0, 5, 0))), VA(bp + V(0, 16, 0), bp + V(0, 18, 0)), Smoother(u), u * 0.5f);
            }, 0.2f, 0.05f, null, 48, 0);
            float tk = 0; for (int i = 0; i < 18; i++) { BCue(s1 + tk, "tick", 0.6f); tk += Mathf.Max(0.05f, 0.34f - i * 0.02f); }
            GameObject crack = null;
            float s2 = BShotAt(2.6f, -1, false, (u, lt, dt) =>
            {
                if (lt > 1.2f && crack == null && r?.face != null) { var cm = Mats.Add(0xffe0a0, 1, Tex.Cracks, true); crack = Mk.Msh(r.face.t, Geo.Plane(1, 1), cm); crack.transform.localPosition = V(0, 0, 0.12f); crack.transform.localRotation = Quaternion.identity; crack.transform.localScale = V(2.2f, 2.2f, 1); iFlashA = 0.35f; bcShake = 1.4f; }
                var f = WAway(); var side = Vector3.Cross(Vector3.up, f); var bp = Bp(); var fp = Fp(); BPath(VA(WardenInHall(bp + f * 38 - side * 8 + V(0, 11, 0)), WardenInHall(bp + f * 33 - side * 6 + V(0, 12, 0))), VA(Vector3.Lerp(bp + V(0, 18, 0), fp, 0.5f), Vector3.Lerp(bp + V(0, 18, 0), fp, 0.6f)), u, 0, 0, 0.2f);
            }, 0.05f, 0.05f, null, 44, 0);
            BCue(s2 + 1.2f, "iCrack", 1f); BCue(s2 + 1.22f, "slam", 1f); BCue(s2 + 1.25f, "iCollapse", 0.7f); BCue(s2 + 1.6f, "iCollapse", 0.25f);
            float s3 = BShotAt(3.6f, -1, true, (u, lt, dt) =>
            {
                sunLight.intensity = sSunI * (0.4f + 1.2f * Mathf.PerlinNoise(BC.t * 14, 7));
                if (r != null) { float dir = Mathf.Sin(BC.t * 5) > 0 ? -6 : 4; foreach (var s in r.spins) if (s.t) s.t.Rotate(s.axis, s.sp * 57.3f * dir * dt, Space.Self); }
                var c = Bp() + V(0, 20, 0); for (int i = 0; i < 4; i++) { var d = Random.onUnitSphere; particles.Add(new Pt { pos = c + d * 16, scale = Vector3.one * 0.18f, mat = goldSpark, v = -d * 12, life = 1.2f, grav = 0 }); }
                var wa = WAway(); float a = Mathf.Atan2(wa.x, wa.z) + Mathf.Sin(BC.t * 0.35f) * 0.55f; var bp = Bp(); BPath(VA(WardenInHall(bp + V(Mathf.Sin(a) * 50, 9, Mathf.Cos(a) * 50)), WardenInHall(bp + V(Mathf.Sin(a + 0.3f) * 47, 11, Mathf.Cos(a + 0.3f) * 47))), /* (an arc on the open side of the hall: never pressed in against a wall) */ VA(bp + V(0, 17, 0), bp + V(0, 18, 0)), u, 0.3f + Mathf.PerlinNoise(BC.t * 4, 2) * 0.5f);
                if (rand < 0.06f) bcShake = 1;
            }, 0.05f, 0.05f, null, 54);
            BCue(s3 + 0.1f, "rewind", 0.8f); BCue(s3 + 0.9f, "timeStop", 0.8f); BCue(s3 + 1.6f, "rewind", 0.7f); BCue(s3 + 2.4f, "iDistort", 0.7f);
            float s4 = BShotAt(3.0f, -1, true, (u, lt, dt) =>
            {
                sunLight.intensity = sSunI * 0.7f;
                if (lt < dt * 1.5f) { var c = Bp() + V(0, 18, 0); Blast(c, 8, 0xffd080); for (int i = 0; i < 3; i++) ShockRing(V(c.x, 0.3f, c.z), 20 + i * 12, 0xffd080, 0.6f + i * 0.2f); whiteFlash = 0.4f; P.shake = 1.2f; }
                var f = WAway(); var side = Vector3.Cross(Vector3.up, f); var bp = Bp(); BPath(VA(WardenInHall(bp + f * 60 + side * 14 + V(0, 4, 0)), WardenInHall(bp + f * 50 + side * 8 + V(0, 7, 0))), VA(bp + V(0, 17, 0), bp + V(0, 19, 0)), Smoother(u), 0.2f);
            }, 0.05f, 0.7f, null, 52);
            BCue(s4, "boom", 1f); BCue(s4, "toll", 0.8f);
            BSay(s4 + 0.4f, 2.4f, "I've run out of time...", "vWarden");
        }
        void DefWardenDefeat()
        {
            var e = BC.e; var r = e?.wd; audioMgr.Stop(0.05f); audioMgr.Hush(); WardenFxClear(); // (his sand columns and clocks go with him, not hang in the hall)
            Vector3 Fp() => r?.face != null ? r.face.t.position : (e != null && e.g ? e.g.transform.position + V(0, 26, 0) : Vector3.zero);
            float s1 = BShotAt(5.0f, -1, false, (u, lt, dt) =>
            {
                if (e == null || !e.g) return; float sag = Smoother(u);
                e.g.transform.position = bcBossPos0 + V(0, -3 * sag, 0); e.g.transform.rotation = bcBossRot0 * Quaternion.Euler(12 * sag, 0, 4 * sag);
                if (r != null) { r.secH.rz += dt * (0.5f + u * 4); r.minH.rz += dt * (0.1f + u); if (r.lSh != null) r.lSh.rx = Mathf.Lerp(r.lSh.rx, 0.3f, 1 - Mathf.Exp(-dt)); if (r.rSh != null) r.rSh.rx = Mathf.Lerp(r.rSh.rx, 0.3f, 1 - Mathf.Exp(-dt)); }
                var f = BCFwd(e); var side = Vector3.Cross(Vector3.up, f); var fp = Fp();
                BPath(VA(fp + f * 44 + side * 12 + V(0, -16, 0), fp + f * 26 + side * 6 + V(0, -8, 0), fp + f * 12 + side * 1 + V(0, -2, 0)), VA(fp + V(0, -8, 0), fp + V(0, -3, 0), fp), u, 0, 0, 0.4f);
            }, 0.1f, 0.05f, null, 48, 0);
            var cs = BuildClockwork(0); var O = cs.O;
            var pr = ISet(1); var P0 = pr.position; IArchMats();
            var rock = IM(Mats.Std(0x4a3a30, 0.9f)); var lava = IM(Mats.Add(0xff4010, 0.35f, null, true)); IGlow(pr, P0 + V(-30, 14, 16), 0xffc090, 70, 0.25f);
            for (int k = 0; k < 10; k++) { var pp = P0 + V(-60 + k * 16, Mathf.Sin(k) * 4, -k * 6); IA(pr, Geo.Cyl(9, 7, 4, 12), rock, pp); IA(pr, Geo.Cone(7, 10, 8), rock, pp + V(0, -7, 0), V(180, 0, 0)); }
            IA(pr, Geo.Box(600, 1, 600), lava, P0 + V(0, -40, 0));
            IMsh(pr, Geo.Torus(8, 0.8f, 48, 5), IM(Mats.Add(0x9a40ff, 0.9f, null, true)), P0 + V(96, 12, -56)); IDiscGlow(pr, P0 + V(96, 12, -56), 0x7a30ff, 9, 0.06f, 10);
            ISnow(1, P0 + V(0, 10, -20), V(90, 20, 40), 160, 0xff8a30, 0.4f, V(0.5f, 3, 0), 0.9f);
            var heroes = new List<HeroRig>(); var heroRoots = new List<Transform>();
            for (int k = 0; k < 2; k++) { var hr = new GameObject("visionHero").transform; hr.SetParent(pr, false); hr.position = P0 + V(-58 + k * 3, 2, 2 + k * 2); var hrig = BuildHero(hr); hr.localScale = Vector3.one * 1.4f; heroes.Add(hrig); heroRoots.Add(hr); }
            var watcher = IRoot(pr, P0 + V(20, -10, -260)); var wm = BCBlack(); IA(watcher, Geo.Cyl(16, 46, 150, 16), wm, L(watcher, 0, 75, 0)); IA(watcher, Geo.Cyl(24, 24, 3, 40), wm, L(watcher, 0, 170, 0), V(90, 0, 0)); IA(watcher, Geo.Torus(26, 1.2f, 48, 4), IM(Mats.Add(0xffd080, 0.35f, null, false)), L(watcher, 0, 170, 2));
            BEnv(1, 0x3a1008, 60, 600, 0x7a5060, 0x6a4030, 0x2a140c, 0xffb080, 2.0f, 0x1a0606, V(-0.5f, -0.6f, -0.6f), 0xffe0d0, 0.15f, 1.4f);
            float s2 = BShotAt(2.2f, 0, false, (u, lt, dt) => { DioFate(cs, 0); ClockSpin(cs, 0.3f, dt); BPath(VA(cs.dio + V(32, 15, 36), cs.dio + V(24, 12, 28)), VA(cs.dio + V(0, 5, 0), cs.dio + V(0, 5, 0)), u); }, 0.05f, 0.05f, null, 46, 0.8f);
            BCue(s2 + 0.05f, "tick", 0.6f); BCue(s2 + 0.2f, "iSwell", 0.35f);
            float s3 = BShotAt(2.2f, 0, false, (u, lt, dt) => { DioFate(cs, Smoother(u)); BPath(VA(cs.dio + V(-28, 22, 32), cs.dio + V(-20, 16, 26)), VA(cs.dio + V(0, 6, 0), cs.dio + V(0, 6, 0)), u, 0.3f); }, 0.05f, 0.05f, null, 46, 0.5f);
            BCue(s3 + 0.05f, "tick", 0.6f); BCue(s3 + 0.2f, "iVoid", 0.6f); BCue(s3 + 0.8f, "iCollapse", 0.5f);
            float s4 = BShotAt(2.4f, 0, false, (u, lt, dt) =>
            {
                float cyc = (lt * 2.2f) % 1; DioFate(cs, cyc < 0.5f ? Smoother(cyc * 2) : Smoother((1 - cyc) * 2)); ClockSpin(cs, cyc < 0.5f ? 1 : -16, dt); if (cyc > 0.5f) RewindSparks(cs.dio + V(0, 8, 0), 24);
                if (cs.we.wd?.rSh != null) cs.we.wd.rSh.rx = cyc > 0.5f ? -2.2f : 0;
                BPath(VA(cs.warden + V(42, 22, 72), cs.warden + V(34, 24, 62)), VA(cs.dio + V(0, 6, 0), cs.dio + V(0, 6, 0)), u);
            }, 0.05f, 0.05f, null, 46, 0.5f);
            for (int i = 0; i < 5; i++) { BCue(s4 + i * 0.45f, "rewind", 0.5f); BCue(s4 + 0.2f + i * 0.45f, "tick", 0.5f); }
            // the heroes, long ago, crossing the lava on the rocks: a sprint over each rock's flat top, a leap from its far
            // edge, a landing on the next one's near edge - feet always on stone, never through it
            Vector3 RockTop(int i) => P0 + V(-60 + i * 16, Mathf.Sin(i) * 4 + 2, -i * 6);
            var trackD = V(16, 0, -6).normalized; var trackS = -Vector3.Cross(Vector3.up, trackD); Vector3 camC = Vector3.zero; bool camInit = false;
            float s5 = BShotAt(3.6f, 1, false, (u, lt, dt) =>
            {
                var mid = Vector3.zero;
                for (int k = 0; k < heroes.Count; k++)
                {
                    var h = heroes[k]; var root = heroRoots[k];
                    float tt = Mathf.Max(0, lt + 0.4f - k * 0.32f); int i = Mathf.Min((int)tt, 7); float f = Mathf.Clamp01(tt - i);
                    Vector3 a = RockTop(i), b = RockTop(i + 1), d = b - a; d.y = 0; var dn = d.normalized; var sd = Vector3.Cross(Vector3.up, dn) * (k == 0 ? -1.6f : 1.6f);
                    Vector3 from = a + dn * 2.5f + sd, to = b - dn * 2.5f + sd; bool air = f >= 0.3f;
                    if (!air) root.position = Vector3.Lerp(a - dn * 2.5f + sd, from, f / 0.3f);
                    else { float s = (f - 0.3f) / 0.7f; root.position = Vector3.Lerp(from, to, s) + V(0, 20 * s * (1 - s), 0); }
                    root.rotation = Quaternion.LookRotation(dn);
                    if (air) { float s = (f - 0.3f) / 0.7f; float tuck = Mathf.Sin(Mathf.PI * s); h.thL.rx = -0.4f - 0.8f * tuck; h.thR.rx = 0.5f - 0.3f * tuck; h.knL.rx = 0.4f + 0.9f * tuck; h.knR.rx = 0.5f + 0.5f * tuck; h.shL.rx = 0.9f; h.shR.rx = -1.3f; }
                    else { float w = Mathf.Sin(f / 0.3f * Mathf.PI * 2 + k); h.thL.rx = -w * 0.9f; h.thR.rx = w * 0.9f; h.knL.rx = Mathf.Max(0, w) * 1.2f + (f > 0.24f ? 0.6f : 0); h.knR.rx = Mathf.Max(0, -w) * 1.2f + (f > 0.24f ? 0.6f : 0); h.shL.rx = w * 0.7f; h.shR.rx = -1.3f; }
                    mid += root.position / heroes.Count;
                }
                // a side-on tracking shot from the open side (the far rocks and the watcher behind them), smoothed so the leaps don't bounce it
                if (!camInit) { camC = mid; camInit = true; } else camC = Vector3.Lerp(camC, mid, 1 - Mathf.Exp(-4 * dt));
                BPath(VA(camC + trackS * 15 + V(0, 4, 0) - trackD * 5, camC + trackS * 15 + V(0, 4, 0) - trackD * 5), VA(camC + trackD * 5 + V(0, 1.5f, 0), camC + trackD * 5 + V(0, 1.5f, 0)), u, 0, 0, 0.6f);
            }, 0.05f, 0.05f, null, 46, 0.5f);
            BCue(s5 + 0.1f, "iWind", 0.5f); BCue(s5 + 0.3f, "iWhisper", 0.6f);
            float s6 = BShotAt(2.2f, 0, false, (u, lt, dt) =>
            {
                if (lt < 1.1f) { DioFate(cs, 1 - Smoother(lt / 1.1f) * 0.6f); ClockSpin(cs, -16, dt); RewindSparks(cs.dio + V(0, 8, 0), 22); }
                else { if (iCues.Add("glitch")) { iFlashA = 0.7f; bcShake = 3; } if (rand < 0.4f) iFlashA = Mathf.Max(iFlashA, rand * 0.4f); if (bcCA) bcCA.colorFilter.Override(rand < 0.3f ? Mats.Hex(0xff80a0) : Mats.Hex(0xfff0d8)); }
                var jit = lt > 1.1f ? Random.insideUnitSphere * 2 : Vector3.zero;
                BPath(VA(cs.dio + V(20, 12, 30) + jit, cs.dio + V(16, 10, 26) + jit), VA(cs.dio + V(0, 6, 0), cs.dio + V(0, 6, 0)), u, 0, 0, lt > 1.1f ? 4 : 1);
            }, 0.05f, 0.05f, null, 46, 0.5f);
            BCue(s6 + 0.05f, "rewind", 0.7f); BCue(s6 + 1.1f, "iDistort", 1f); BCue(s6 + 1.12f, "iCrack", 0.6f); BCue(s6 + 1.12f, "iGlitch", 1f);
            float s7 = BShotAt(6.6f, -1, false, (u, lt, dt) =>
            {
                if (e == null || !e.g) return;
                if (r != null && r.faceM) Mats.SetColor(r.faceM, Color.Lerp(Color.black, Color.white * 0.6f, Smoother(Mathf.Clamp01((lt - 0.6f) / 1.6f))));
                float lift = Smoother(Mathf.Clamp01((lt - 1.4f) / 2.0f)); e.g.transform.rotation = bcBossRot0 * Quaternion.Euler(12 * (1 - lift) - 4 * lift, 0, 4 * (1 - lift));
                var eye = V(P.pos.x, P.pos.y + 1.6f, P.pos.z); var fp = Fp(); var dir = (eye - fp).normalized;
                BPath(VA(fp + dir * 18, fp + dir * 12, fp + dir * 8.5f), VA(fp, fp, fp), u, 0, 0, 0.3f);
            }, 0.05f, 0.05f, null, 40, 0);
            BSay(s7 + 2.4f, 1.6f, "Wait...", "vWarden"); BSay(s7 + 4.4f, 2.1f, "You weren't supposed to remember.", "vWarden");
            float s8 = BShotAt(3.6f, -1, false, (u, lt, dt) =>
            {
                if (r != null && r.faceM) Mats.SetColor(r.faceM, lt < 0.4f ? Color.white * 0.6f : Color.black);
                var eye = V(P.pos.x, P.pos.y + 1.6f, P.pos.z); var fp = Fp(); var dir = (eye - fp).normalized; BPath(VA(fp + dir * 8.5f, fp + dir * 8.2f), VA(fp, fp), u, 0, 0, 0.2f);
            }, 0.05f, 1.4f, null, 40, 0);
            BCue(s8 + 0.4f, "iCollapse", 1f); BCue(s8 + 0.42f, "slam", 0.8f); BCue(s8 + 0.9f, "iCollapse", 0.3f); BCue(s8 + 1.5f, "iCollapse", 0.12f); // (and its echo)
            // and back in the hall, the game running: his hands spin wild and stop at midnight, the toll, and the dial bursts -
            // his own death (UpdateWardenDeath), filmed whole from the middle of the hall
            bool boomGo = false, boomHit = false; Color faceLit = new Color(1f, 0.8f, 0.55f);
            Vector3 wbp = bcBossPos0, wf = -wbp; wf.y = 0; wf = wf.magnitude < 12 ? (e != null ? BCFwd(e) : Vector3.forward) : Vector3.Slerp(e != null ? BCFwd(e) : Vector3.forward, wf.normalized, 0.6f).normalized;
            var wside = Vector3.Cross(Vector3.up, wf);
            BShotAt(4.6f, -1, true, (u, lt, dt) =>
            {
                var d = bossDeath;
                if (d != null && d.e == e)
                {
                    if (!boomGo) { boomGo = true; slowmoT = 0; d.t = Mathf.Max(d.t, 1.5f); if (e.g) { e.g.transform.position = bcBossPos0; e.g.transform.rotation = bcBossRot0; } if (r != null && r.faceM) Mats.SetColor(r.faceM, faceLit); }
                    if (!boomHit && d.t >= 3.0f) { boomHit = true; bcShake = 2.6f; iFlashA = 0.55f; }
                }
                BPath(VA(WardenInHall(wbp + wf * 50 + wside * 16 + V(0, 4, 0)), WardenInHall(wbp + wf * 40 + wside * 9 + V(0, 6, 0))), VA(wbp + V(0, 16, 0), wbp + V(0, 11, 0)), Smoother(u), 0.1f);
            }, 0.9f, 0.6f, null, 50, 0);
        }
        Vector3 WardenInHall(Vector3 p) => V(Mathf.Clamp(p.x, -LB + 4, LB - 4), p.y, Mathf.Clamp(p.z, -LB + 4, LB - 4));
    }
}
