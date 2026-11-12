using System;
using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    // ACT II, THE FROZEN CIRCLE (levels 6-10)
    //    6 FROZEN WILDERNESS     a pine forest round a frozen lake, ice cliffs with a camp on top, a hunters' camp by the trail
    //                            traps: avalanches down the cliff chutes, hunters' snares, blizzard gusts over the slick lake
    //    7 THE FROZEN SETTLEMENT a dead village: streets of timber houses, a market square, the chapel and its clock tower
    //                            traps: icicles dropping off the eaves over the lanes, the frozen bell's frost toll
    //    8 ANCIENT RUINS         a processional avenue of guardian statues, a temple on its podium, a ruined bath-hall, the ridge
    //                            traps: the guardians' freezing gaze across the avenue, warded rune tiles on the temple floor
    //    9 THE PRISON OF FROST   a fortress prison: cell blocks with a gallery, watchtowers, the warden's keep, and in the
    //                            yard the broken Great Cage where the Traitor was held
    //                            traps: pendulum blades under the gantries, the wardens' searchlights
    //   10 THRONE OF FROST       the demon throne room: the Frozen Traitor sleeps in the ice before his throne
    // Every map's portal opens on a frost shrine at its centre (FrostShrine), standing on the ground.
    public partial class Game
    {
        List<LevelDef> FrostLevels() => new List<LevelDef>
        {
            new LevelDef { name = "FROZEN WILDERNESS", theme = "frost", hazard = "AVALANCHES + SNARES", hazardInfo = "snow pours off the cliffs before an avalanche comes down the chutes; snares glint in the snow; gusts sweep the frozen lake",
                B = 70, center = V(0, 0.3f, -8), spawn = V(0, 0, 56), build = BuildWilderness },
            new LevelDef { name = "THE FROZEN SETTLEMENT", theme = "frost_town", hazard = "FALLING ICICLES + THE BELL", hazardInfo = "eaves shake and shed snow before their icicles drop; when the chapel bell tolls, a ring of frost rolls through town - jump it",
                B = 64, center = V(0, 0.3f, 0), spawn = V(0, 0, 54), build = BuildSettlement },
            new LevelDef { name = "ANCIENT RUINS", theme = "frost_ruin", hazard = "GUARDIAN STATUES + RUNE TILES", hazardInfo = "the guardians' visors flare and they fire ice at you - break their line of sight; warded tiles on the temple floor light up under you - move",
                B = 68, center = V(0, 0.3f, -6), spawn = V(0, 0, 56), build = BuildRuins },
            new LevelDef { name = "THE PRISON OF FROST", theme = "frost_prison", hazard = "PENDULUMS + SEARCHLIGHTS", hazardInfo = "blades swing under the gantries; the wardens' lights sweep the yard - stay in one and it turns red and fires where you stand",
                B = 70, center = V(0, 0.3f, 24), spawn = V(0, 0, 58), build = BuildPrison },
            new LevelDef { name = "THRONE OF FROST", theme = "frost_throne", hazard = null, B = 74, boss = 3, center = V(0, 0.3f, 24), spawn = V(0, 0, 62), bossSpawn = new Vector2(0, -30), build = BuildThrone },
        };

        // ================= 6. FROZEN WILDERNESS =================
        void BuildWilderness()
        {
            FrostMats(); Arena(70, 22);
            var rng = new System.Random(606); float R() => (float)rng.NextDouble();
            var lake = V(0, 0, -8); const float lakeR = 24;
            // the frozen lake: glassy ice with pale cracks in it, an islet of snow and boulders in the middle holding the shrine
            Flat(Geo.Circle(lakeR, 64), slickM, lake.x, 0.03f, lake.z);
            var crackM = Mats.Trans(0xffffff, 0.3f);
            for (int i = 0; i < 18; i++) { float a = R() * 6.28f, r = 8 + R() * 14; var g = Flat(Geo.Plane(0.07f, 2 + R() * 5), crackM, lake.x + Mathf.Cos(a) * r, 0.04f, lake.z + Mathf.Sin(a) * r); g.transform.localRotation = Quaternion.Euler(-90, R() * 180, 0); }
            Flat(Geo.Circle(7.2f, 40), snowM, lake.x, 0.05f, lake.z);
            for (int i = 0; i < 10; i++) { if (i % 3 == 1) continue; float a = i / 10f * 6.28f + 0.3f; Boulder(lake.x + Mathf.Cos(a) * 7.6f, lake.z + Mathf.Sin(a) * 7.6f, 0.9f + R() * 0.7f); }
            FrostShrine(lake.x, lake.z);
            for (int i = 0; i < 34; i++) { if (i % 6 == 0) continue; float a = i / 34f * 6.28f, r = lakeR + 1.2f + R() * 2; Snowbank(lake.x + Mathf.Cos(a) * r, lake.z + Mathf.Sin(a) * r, 1.6f + R() * 2.4f, 0.5f + R() * 0.7f); }
            var reed = Mats.Std(0x6a6450, 0.9f);
            for (int i = 0; i < 46; i++) { float a = R() * 6.28f, r = lakeR - 0.5f + R() * 1.5f; Ignore(Deco(Geo.Cone(0.04f, 0.7f + R() * 0.9f, 3), reed, lake.x + Mathf.Cos(a) * r, 0.45f, lake.z + Mathf.Sin(a) * r, (R() - 0.5f) * 0.3f, 0, (R() - 0.5f) * 0.3f)); }
            // an ice-fishing hut out on the lake, its fisherman frozen at his hole
            { float hx = 15, hz = -20; Box(hx, 0, hz, 3, 2.4f, 3, woodM); Deco(Prism(3.8f, 1.3f, 3.8f), roofM, hx, 2.4f, hz); Deco(Prism(3.5f, 0.6f, 3.6f), snowM, hx, 2.9f, hz); Deco(Geo.Box(1.0f, 1.8f, 0.06f), darkWoodM, hx, 0.9f, hz + 1.52f); Deco(Geo.Box(0.5f, 0.4f, 0.06f), litGlassM, hx + 1.52f, 1.6f, hz, 0, Mathf.PI / 2, 0);
              Flat(Geo.Circle(0.6f, 14), Mats.Std(0x0a1a28, 0.1f, 0.3f), hx - 2.2f, 0.045f, hz + 2.4f); Barrel(hx + 2.1f, hz - 1.2f); FrozenFigure(hx - 2.4f, hz + 3.4f, Mathf.PI, 3, 0, 3); }
            // the ice cliffs along the north: a wall of rock and ice, snow lying on top, icicles off the lip, a frozen waterfall
            Box(0, 0, -59, 140, 6, 22, M.tower);
            for (int k = 0; k < 30; k++) Deco(Geo.Box(5, 0.35f, 22), snowM, -70 + k * 4.8f, 6.1f, -59, 0, 0, (R() - 0.5f) * 0.06f);
            for (int k = 0; k < 28; k++)
            {
                float x = -68 + k * 5 + R() * 2; if (Mathf.Abs(x) < 6) continue;
                Deco(Geo.Box(3.6f + R() * 2, 5.4f + R() * 0.6f, 0.8f), k % 2 == 0 ? iceM : M.tower, x, 2.8f, -47.7f, R() * 0.1f, 0, 0);
                for (int j = 0; j < 3; j++) Ignore(Deco(Geo.Cone(0.25f + R() * 0.25f, 0.9f + R() * 1.8f, 5), iceM, x + (j - 1) * 1.6f, 5.6f - R() * 0.3f, -47.2f, Mathf.PI, 0, 0));
            }
            for (int k = 0; k < 7; k++) Deco(Geo.Box(1.1f + R() * 0.6f, 6.2f, 1.0f + R() * 0.6f), k % 2 == 0 ? iceM : iceTransM, -4 + k * 1.35f, 3.1f, -47.5f + R() * 0.4f);
            Flat(Geo.Circle(4.2f, 28), slickM, 0, 0.04f, -43.5f); IceFormation(-6.5f, -45, 4, 0.6f); IceFormation(6.5f, -45, 5, 0.7f);
            foreach (var sx in new[] { -1, 1 }) Stairs(sx * 32, -36, 7, V(0, 0, -1), 6, 0, 6, 2, M.tower);
            // the camp on the cliff top, and pines along it
            Tent(-24, -60, 0.4f, 6); Tent(-14, -63, -0.3f, 6); Campfire(-18, -55, 6); Crate(-30, -54, 1.4f, 6); Crate(-31.4f, -55.6f, 1.1f, 6, 0.4f);
            FrozenFigure(-20, -57, 0.8f, 3, 6, 3);
            for (int k = 0; k < 9; k++) PineTree(12 + k * 6.5f + R() * 2, -60 + R() * 9, 9 + R() * 5, 6);
            for (int k = 0; k < 6; k++) PineTree(-66 + k * 6 + R() * 2, -62 + R() * 8, 9 + R() * 4, 6);
            // the east ledge: a hunters' lookout over the lake
            Box(56, 0, 2, 24, 3.6f, 32, M.tower); Deco(Geo.Box(24.4f, 0.3f, 32.4f), snowM, 56, 3.7f, 2); Stairs(39.2f, 2, 6, V(1, 0, 0), 3, 0, 3.6f, 1.6f, M.tower);
            var lookout = V(60, 3.6f, 10);
            foreach (var a in new[] { -1.2f, 1.2f }) foreach (var b in new[] { -1.2f, 1.2f }) { Deco(Geo.Cyl(0.15f, 0.15f, 6, 6), woodM, lookout.x + a, lookout.y + 3, lookout.z + b); SolidAABB(V(lookout.x + a - 0.15f, lookout.y, lookout.z + b - 0.15f), V(lookout.x + a + 0.15f, lookout.y + 6, lookout.z + b + 0.15f)); }
            Box(lookout.x, lookout.y + 5.8f, lookout.z, 3.4f, 0.3f, 3.4f, woodM); Deco(Geo.Box(3.6f, 0.2f, 3.6f), snowM, lookout.x, lookout.y + 6.2f, lookout.z);
            Deco(Prism(4.2f, 1.6f, 4.2f), roofM, lookout.x, lookout.y + 7.6f, lookout.z); Deco(Prism(3.8f, 0.6f, 4.0f), snowM, lookout.x, lookout.y + 8.5f, lookout.z);
            for (int k = 0; k < 4; k++) Crate(50 + k * 2.2f, -8 + (k % 2) * 1.4f, 1.1f, 3.6f, k * 0.3f);
            // the old camp by the trail: hunters frozen huddled round the dead fire
            var fire = V(-31, 0, 35);
            Tent(-37, 42, 1.2f); Tent(-26, 45, 2.2f); Campfire(fire.x, fire.z); Sled(-21, 31, 0.6f); Crate(-40, 30, 1.3f); Crate(-41.2f, 28.6f, 1.0f, 0, 0.3f); Barrel(-24, 39); Barrel(-23.2f, 40.1f);
            for (int k = 0; k < 3; k++) { float a = k * 2.1f + 0.4f; var pp = fire + V(Mathf.Cos(a), 0, Mathf.Sin(a)) * 2.0f; FrozenFigure(pp.x, pp.z, Mathf.Atan2(fire.x - pp.x, fire.z - pp.z), 3, 0, 3); }
            FrozenFigure(5, 30, Mathf.PI * 0.95f, 2, 0, 0); // someone running for the camp who didn't make it
            FrozenFigure(-12, 18, 2.6f, 0, 0, 3);
            // avalanche chutes, snares and pads (the forest keeps clear of them)
            var lanes = new (float x, float zTop, float zEnd, float w)[] { (-50, -47.5f, -26, 8), (-16, -47.5f, -31, 8), (16, -47.5f, -31, 8), (48, -47.5f, -24, 8) };
            var snares = new[] { V(-25, 0, 29), V(-38, 0, 24), V(-45, 0, 40), V(-19, 0, 50), V(30, 0, 38), V(43, 0, 30), V(-52, 0, -6), V(-44, 0, -28), V(34, 0, -32), V(24, 0, 52) };
            var pads = new[] { V(-58, 0, -30), V(58, 0, -34), V(0, 0, 32) };
            foreach (var p in pads) PadAt(p.x, p.z, p.x == 0 ? 22 : 24);
            // the forest: dense pine woods to the west and south, thinning toward the lake; dead trees, stumps, logs, boulders between
            var trees = new List<Vector2>();
            bool Free(float x, float z)
            {
                if (Mathf.Abs(x) > 66 || Mathf.Abs(z) > 66) return false;
                if (Hypot(x - lake.x, z - lake.z) < lakeR + 4) return false;
                if (z < -44) return false;
                if (Mathf.Abs(x) < 9 && z > 18) return false;                               // the trail from the south
                if (Hypot(x - fire.x, z - fire.z) < 12) return false;                       // the camp clearing
                if (x > 40 && z > -17 && z < 21) return false;                              // the ledge
                if (Mathf.Abs(Mathf.Abs(x) - 32) < 5.5f && z < -28) return false;          // the cliff stairs
                foreach (var L in lanes) if (z < L.zEnd + 4 && Mathf.Abs(x - L.x) < L.w / 2 + 2.5f) return false;
                foreach (var p in pads) if (Hypot(x - p.x, z - p.z) < 4) return false;
                foreach (var s in snares) if (Hypot(x - s.x, z - s.z) < 2.5f) return false;
                if (Hypot(x - 15, z + 20) < 6) return false;
                foreach (var t in trees) if ((t - new Vector2(x, z)).sqrMagnitude < 4.6f * 4.6f) return false;
                return true;
            }
            for (float gx = -66; gx <= 66; gx += 4.6f)
                for (float gz = -43; gz <= 66; gz += 4.6f)
                {
                    float x = gx + (R() - 0.5f) * 3.2f, z = gz + (R() - 0.5f) * 3.2f;
                    float dens = x < -28 ? 0.92f : z > 28 ? 0.85f : x > 24 ? 0.7f : 0.45f;
                    if (R() > dens || !Free(x, z)) continue;
                    trees.Add(new Vector2(x, z));
                    float roll = R();
                    if (roll < 0.07f) DeadTree(x, z, 6 + R() * 4);
                    else if (roll < 0.1f) Stump(x, z);
                    else PineTree(x, z, 8 + R() * 7.5f);
                }
            for (int i = 0; i < 9; i++) { float x = -60 + R() * 120, z = -40 + R() * 100; if (!Free(x, z)) continue; trees.Add(new Vector2(x, z)); FallenLog(x, z, 4 + R() * 3, R() * 3); }
            for (int i = 0; i < 12; i++) { float x = -62 + R() * 124, z = -40 + R() * 100; if (!Free(x, z)) continue; trees.Add(new Vector2(x, z)); Boulder(x, z, 1.0f + R() * 1.2f); }
            // traps
            MechSlick(lake, lakeR, 7.4f);
            MechGusts(lake, lakeR, V(1, 0, 0.35f));
            MechAvalanche(lanes, 6);
            MechSnares(snares);
        }

        // ================= 7. THE FROZEN SETTLEMENT =================
        void BuildSettlement()
        {
            FrostMats(); Arena(64, 18);
            var rng = new System.Random(707); float R() => (float)rng.NextDouble();
            // the cobbled square and streets
            Flat(Geo.Circle(14, 48), cobbleM, 0, 0.03f, 0); Flat(Geo.Plane(9, 118), cobbleM, 0, 0.025f, -4); Flat(Geo.Plane(118, 8), cobbleM, 0, 0.026f, 0);
            Flat(Geo.Ring(13.6f, 14.2f, 48), stoneDarkM, 0, 0.035f, 0);
            FrostShrine(0, 0);
            // round the edge of the square: the well, market stalls, and the people who were there when the frost came
            Well(-10, 8.5f); Stall(10.5f, 7.5f, -2.3f); Stall(-10.5f, -7.8f, 0.85f); Stall(9.5f, -9.5f, -0.75f);
            FrozenFigure(-7.6f, 10.6f, 2.4f, 1, 0, 0); FrozenFigure(-12.4f, 6.2f, 0.7f, 0, 0, 0); FrozenFigure(12.2f, 10.4f, -2.2f, 1, 0, 0);
            FrozenFigure(2.2f, 25, 3.0f, 2, 0, 0); FrozenFigure(-2.6f, -24, 0.3f, 2, 0, 0); FrozenFigure(23, 1.5f, -1.6f, 0, 0, 0); FrozenFigure(-36, 3, 1.4f, 2, 0, 0);
            for (int k = 0; k < 5; k++) Barrel(-12.6f + (k % 2) * 0.9f, -10.6f + k * 0.4f);
            Crate(12.8f, -11.6f, 1.2f); Crate(13.3f, -10.2f, 0.9f, 1.2f, 0.4f);
            // the houses, packed tight so the lanes between them are narrow
            var houses = new (float x, float z, float w, float d, float h, bool rx, bool broken)[]
            {
                (-15, -15, 9, 8, 5.5f, true, false), (15, -15, 9, 8, 6, true, true), (-15, 15, 9, 8, 6, true, true), (15, 15, 9, 8, 5.5f, true, false),
                (-28, -14, 8, 9, 6.5f, false, false), (28, -14, 8, 9, 5, false, false), (-28, 15, 8, 9, 5, false, false), (28, 15, 8, 9, 6.5f, false, true),
                (-15, -29, 9, 8, 6.5f, true, false), (15, -29, 9, 8, 5, true, false), (-15, 29, 9, 8, 5.5f, true, false), (15, 29, 9, 8, 6, true, true),
                (-30, -30, 10, 9, 7, true, true), (30, -30, 10, 9, 6, true, false), (-30, 31, 10, 9, 6, true, false), (31, 31, 9, 9, 7, true, false),
                (-45, -16, 9, 10, 6, false, false), (45, -18, 9, 10, 5.5f, false, true), (45, 18, 9, 10, 6, false, false), (-44, 32, 9, 9, 5, true, true),
                (44, 44, 10, 8, 5.5f, true, false), (-16, 46, 9, 8, 5, true, false), (16, 46, 9, 8, 6, true, false),
            };
            foreach (var h in houses) House(h.x, h.z, h.w, h.d, h.h, h.rx, h.broken);
            // the chapel at the head of the street: a stone nave with buttresses and a rose window, and its clock tower
            {
                float nz = -47, NW = 14, NL = 16, NH = 10; // the nave: a tall stone hall, buttressed, lit by lancet windows
                Box(0, 0, nz, NW, NH, NL, stoneM); Deco(Geo.Box(NW + 0.8f, 0.7f, NL + 0.8f), stoneDarkM, 0, 0.35f, nz);
                Deco(Geo.Box(NW + 0.3f, 0.35f, NL + 0.3f), stoneDarkM, 0, NH - 0.2f, nz); // the cornice
                var glassA = Mats.Add(0x80c8ff, 0.7f, null, true, true); var glassB = Mats.Add(0xffb060, 0.55f, null, true, true);
                foreach (var sx in new[] { -1f, 1f })
                {
                    for (int k = 0; k < 5; k++)
                    {
                        float bz = nz - NL / 2 + 0.8f + k * (NL - 1.6f) / 4;
                        Box(sx * (NW / 2 + 0.6f), 0, bz, 1.2f, 6.5f, 1.4f, stoneDarkM); Box(sx * (NW / 2 + 0.4f), 6.5f, bz, 0.8f, 2.4f, 1.1f, stoneDarkM);
                        Deco(Prism(1.2f, 1.2f, 1.4f), stoneDarkM, sx * (NW / 2 + 0.6f), 6.5f, bz, 0, Mathf.PI / 2, 0);
                        Deco(Geo.Cone(0.35f, 2.2f, 4), stoneM, sx * (NW / 2 + 0.4f), 8.9f + 1.1f, bz, 0, Mathf.PI / 4, 0); Deco(Geo.Cone(0.3f, 0.6f, 4), snowM, sx * (NW / 2 + 0.4f), 9.1f, bz, 0, Mathf.PI / 4, 0); // a pinnacle
                        if (k == 4) continue;
                        float wz = bz + (NL - 1.6f) / 8; // a lancet window between the buttresses
                        Deco(Geo.Box(0.06f, 4.2f, 1.1f), k % 2 == 0 ? glassA : glassB, sx * (NW / 2 + 0.03f), 4.6f, wz); Deco(Prism(1.1f, 0.8f, 0.06f), k % 2 == 0 ? glassA : glassB, sx * (NW / 2 + 0.03f), 6.7f, wz, 0, Mathf.PI / 2, 0);
                        Deco(Geo.Box(0.2f, 4.6f, 0.2f), stoneDarkM, sx * (NW / 2 + 0.08f), 4.6f, wz - 0.65f); Deco(Geo.Box(0.2f, 4.6f, 0.2f), stoneDarkM, sx * (NW / 2 + 0.08f), 4.6f, wz + 0.65f); Deco(Geo.Box(0.25f, 0.15f, 1.5f), snowM, sx * (NW / 2 + 0.12f), 2.4f, wz);
                        var wg = Mk.Glow(levelRoot, k % 2 == 0 ? 0x80c8ff : 0xffb060, 3, 0.25f); wg.transform.localPosition = V(sx * (NW / 2 + 0.5f), 4.8f, wz);
                    }
                }
                // the steep slate roof under snow, iron cresting along the ridge
                float span = NW + 2.4f, rh = 9;
                Deco(Prism(span, rh, NL + 1.2f), roofM, 0, NH, nz); Deco(Prism(span - 0.8f, rh - 0.4f, NL + 1.0f), snowM, 0, NH + 0.34f, nz);
                for (int k = 0; k < 9; k++) Deco(Geo.Cone(0.12f, 0.6f, 4), darkIronM, 0, NH + rh + 0.2f, nz - NL / 2 + k * NL / 8);
                SolidGable(0, nz, NH, span, rh, NL + 1.2f, false);
                // the west front: a stone gable rising above the roof, a rose window, a deep pointed portal under its own gable
                float fz = nz + NL / 2;
                Deco(Prism(NW + 0.4f, rh + 0.8f, 1.0f), stoneM, 0, NH, fz - 0.3f); Deco(Geo.Cone(0.5f, 2.6f, 4), stoneDarkM, 0, NH + rh + 2.0f, fz - 0.3f, 0, Mathf.PI / 4, 0); Deco(Geo.Box(1.2f, 1.6f, 0.2f), stoneDarkM, 0, NH + rh + 4f, fz - 0.3f); Deco(Geo.Box(0.2f, 2.6f, 0.2f), stoneDarkM, 0, NH + rh + 4.4f, fz - 0.3f);
                foreach (var sx in new[] { -1f, 1f }) { Box(sx * (NW / 2 - 0.2f), 0, fz + 0.4f, 1.6f, NH + 2, 1.4f, stoneDarkM); Deco(Geo.Cone(0.6f, 3.4f, 4), stoneM, sx * (NW / 2 - 0.2f), NH + 2 + 1.7f, fz + 0.4f, 0, Mathf.PI / 4, 0); }
                Deco(Geo.Torus(2.1f, 0.32f, 32, 6), stoneDarkM, 0, NH + 2.6f, fz + 0.25f); Deco(Geo.Circle(1.95f, 32), glassA, 0, NH + 2.6f, fz + 0.27f);
                for (int k = 0; k < 8; k++) Deco(Geo.Box(0.12f, 3.8f, 0.12f), stoneDarkM, 0, NH + 2.6f, fz + 0.3f, 0, 0, k * Mathf.PI / 8);
                var rw = Mk.Glow(levelRoot, 0x80c8ff, 5, 0.35f); rw.transform.localPosition = V(0, NH + 2.6f, fz + 0.8f);
                Box(0, 0, fz + 1.2f, 6.4f, 6.8f, 2.4f, stoneM); Deco(Prism(7.2f, 2.6f, 2.6f), roofM, 0, 6.8f, fz + 1.2f); Deco(Prism(6.6f, 2.3f, 2.5f), snowM, 0, 7.0f, fz + 1.2f); // the porch
                for (int k = 0; k < 3; k++) Deco(Geo.Arc(1.75f - k * 0.18f, 0.2f, 18), k % 2 == 0 ? stoneDarkM : stoneM, 0, 3.0f, fz + 2.45f - k * 0.25f);
                Deco(Geo.Box(3.2f, 3.0f, 0.1f), darkWoodM, 0, 1.5f, fz + 2.42f); Deco(Geo.Cyl(1.55f, 1.55f, 0.1f, 18), darkWoodM, 0, 3.0f, fz + 2.42f, Mathf.PI / 2, 0, 0); Deco(Geo.Box(0.08f, 4.4f, 0.12f), darkIronM, 0, 2.2f, fz + 2.48f);
                foreach (var sx in new[] { -1f, 1f }) Lantern(levelRoot, sx * 2.0f, 3.6f, fz + 2.6f, true);
                Stairs(0, fz + 5.2f, 5, V(0, 0, -1), 2, 0, 0.5f, 0.8f, stoneDarkM);
                // the clock tower behind it: a square shaft, clock faces, an open belfry with the bell, a slate spire
                float tz = -59.6f, H = 18;
                Box(0, 0, tz, 7, H, 7, stoneM);
                foreach (var sx in new[] { -1f, 1f }) foreach (var sz in new[] { -1f, 1f }) Deco(Geo.Box(0.9f, H, 0.9f), stoneDarkM, sx * 3.3f, H / 2, tz + sz * 3.3f);
                foreach (var y in new[] { 7.5f, H }) Deco(Geo.Box(7.7f, 0.45f, 7.7f), stoneDarkM, 0, y, tz);
                for (int f = 0; f < 4; f++)
                {
                    float a = f * Mathf.PI / 2; var n = V(Mathf.Sin(a), 0, Mathf.Cos(a)); var c = V(0, H - 3.8f, tz) + n * 3.55f;
                    var face = Mk.Piv(levelRoot, c.x, c.y, c.z); face.SetRot(0, a, 0);
                    Mk.Msh(face, Geo.Cyl(1.7f, 1.7f, 0.12f, 24), Mats.Std(0xd8d2c4, 0.6f), 0, 0, 0, Mathf.PI / 2, 0, 0);
                    Mk.Msh(face, Geo.Torus(1.72f, 0.12f, 24, 5), bronzeM, 0, 0, 0.06f);
                    for (int k = 0; k < 12; k++) { float m = k * Mathf.PI / 6; Mk.Msh(face, Geo.Box(0.08f, k % 3 == 0 ? 0.35f : 0.2f, 0.04f), darkIronM, Mathf.Sin(m) * 1.45f, Mathf.Cos(m) * 1.45f, 0.08f, 0, 0, -m); }
                    Mk.Msh(face, Geo.Box(0.12f, 0.95f, 0.04f), darkIronM, 0.2f, 0.42f, 0.1f, 0, 0, -0.45f); Mk.Msh(face, Geo.Box(0.09f, 1.35f, 0.04f), darkIronM, -0.3f, 0.6f, 0.11f, 0, 0, 0.45f); // frozen at eight minutes to midnight
                    Mk.Msh(face, Geo.Box(3.6f, 0.15f, 0.4f), snowM, 0, -1.9f, 0.1f);
                    for (int k = 0; k < 4; k++) Mk.Msh(face, Geo.Cone(0.07f, 0.4f + k % 2 * 0.3f, 5), iceM, -1 + k * 0.66f, -2.2f, 0.12f, Mathf.PI, 0, 0);
                }
                // the belfry: four piers and an arch on each side, open, the bell hung inside
                foreach (var sx in new[] { -1f, 1f }) foreach (var sz in new[] { -1f, 1f }) Box(sx * 2.9f, H, tz + sz * 2.9f, 1.3f, 5, 1.3f, stoneM);
                for (int f = 0; f < 4; f++) { float a = f * Mathf.PI / 2; var n = V(Mathf.Sin(a), 0, Mathf.Cos(a)); Deco(Geo.Arc(2.25f, 0.35f, 16), stoneM, n.x * 2.9f, H + 2.7f, tz + n.z * 2.9f, 0, a + Mathf.PI / 2, 0); Deco(Geo.Box(5.2f, 0.4f, 0.5f), stoneDarkM, n.x * 3.0f, H + 0.2f, tz + n.z * 3.0f, 0, a + Mathf.PI / 2, 0); }
                Box(0, H + 5, tz, 7.6f, 0.8f, 7.6f, stoneDarkM);
                var bell = Mk.Piv(levelRoot, 0, H + 4.6f, tz);
                Mk.Msh(bell, GeoX.Lathe("frostbell", GeoX.YR(0, 0.35f, -0.25f, 0.72f, -1.1f, 0.95f, -1.8f, 1.35f, -2.15f, 1.62f, -2.3f, 1.66f), 24), bronzeM);
                Mk.Msh(bell, Geo.Cyl(1.3f, 0.6f, 1.6f, 16), Mats.Std(0x1a120a, 0.6f), 0, -1.3f, 0); Mk.Msh(bell, Geo.Sphere(0.28f, 10, 8), darkIronM, 0, -2.0f, 0);
                Mk.Msh(bell, Geo.Torus(1.6f, 0.06f, 24, 4), snowM, 0, -2.2f, 0, Mathf.PI / 2, 0, 0);
                Deco(Geo.Box(6.4f, 0.4f, 0.4f), darkWoodM, 0, H + 4.7f, tz);
                var bellGlow = Mk.Glow(levelRoot, 0x9ae8ff, 7, 0.05f); bellGlow.transform.localPosition = V(0, H + 3.4f, tz); var bellGlowM = bellGlow.GetComponent<Renderer>().sharedMaterial;
                Deco(Geo.Cyl(0.12f, 3.9f, 14, 8), roofM, 0, H + 5.8f + 7, tz, 0, Mathf.PI / 8, 0); Deco(Geo.Cyl(0.12f, 3.6f, 6, 8), snowM, 0, H + 5.85f + 3, tz, 0, Mathf.PI / 8, 0); for (int f = 0; f < 4; f++) { float a = f * Mathf.PI / 2; var n = V(Mathf.Sin(a), 0, Mathf.Cos(a)); var lp = V(0, H + 7.6f, tz) + n * 3.0f; Deco(Prism(1.2f, 1.4f, 1.4f), roofM, lp.x, lp.y, lp.z, 0, a, 0); Deco(Geo.Box(0.7f, 1.0f, 0.1f), litGlassM, lp.x + n.x * 0.66f, lp.y - 0.3f, lp.z + n.z * 0.66f, 0, a, 0); } // dormer lucarnes on the spire
                Deco(Geo.Cyl(0.05f, 0.05f, 2.4f, 5), darkIronM, 0, H + 20.8f, tz); Deco(Geo.Box(1.0f, 0.08f, 0.08f), darkIronM, 0, H + 21.2f, tz);
                foreach (var sx in new[] { -1f, 1f }) foreach (var sz in new[] { -1f, 1f }) { Deco(Geo.Cone(0.45f, 3.4f, 4), stoneDarkM, sx * 3.5f, H + 7.1f, tz + sz * 3.5f, 0, Mathf.PI / 4, 0); Deco(Geo.Box(1.0f, 1.0f, 1.0f), stoneDarkM, sx * 3.5f, H + 5.9f, tz + sz * 3.5f); }
                SolidAABB(V(-3.6f, H + 5.8f, tz - 3.6f), V(3.6f, H + 10, tz + 3.6f));
                MechBellToll(V(0, H + 3, tz), bell, bellGlowM);
                // the graveyard beside the chapel, headstones leaning under the snow
                for (int k = 0; k < 12; k++) { float gx = (k % 2 == 0 ? -1 : 1) * (11 + (k / 2 % 3) * 2.6f), gz = nz - 4 + (k / 6) * 5 + R(); Box(gx, 0, gz, 1.0f, 1.2f + R() * 0.5f, 0.3f, stoneDarkM); Deco(Geo.Box(1.05f, 0.12f, 0.35f), snowM, gx, 1.5f, gz); }
            }
            // the old town hall on its raised terrace to the west: a stone hall that burned and fell in. Its walls still stand round
            // empty windows, the roof timbers came down inside, the clock lies in the rubble, the mayor is frozen at his lectern.
            {
                const float Y = 2.5f; var wallM = stoneM; var soot = charM;
                Box(-52, 0, 0, 16, Y, 22, M.block); TrimRect(-52, 0, 16, 22, Y); Stairs(-39.8f, 0, 6, V(-1, 0, 0), 3, 0, Y, 1.4f, M.block);
                Flat(Geo.Plane(12, 16), cobbleM, -53, Y + 0.03f, 0);
                // the balustrade round the terrace (open at the stairs)
                for (int k = 0; k <= 10; k++) { float z = -10.5f + k * 2.1f; if (Mathf.Abs(z) < 3.2f) continue; Box(-44.4f, Y, z, 0.35f, 1.0f, 0.35f, stoneDarkM); }
                foreach (var (z0, z1) in new[] { (-10.6f, -3.0f), (3.0f, 10.6f) }) { Box(-44.4f, Y + 1.0f, (z0 + z1) / 2, 0.45f, 0.2f, z1 - z0, stoneM); Deco(Geo.Box(0.5f, 0.1f, z1 - z0), snowM, -44.4f, Y + 1.25f, (z0 + z1) / 2); }
                // a wall with window bays: piers, sills and the stone above each window; broken walls have lost their tops
                void WallRun(float x0, float z0, float x1, float z1, float h, int bays, bool broken, int seed)
                {
                    var rng2 = new System.Random(seed); float R2() => (float)rng2.NextDouble();
                    bool alongX = Mathf.Abs(x1 - x0) > Mathf.Abs(z1 - z0); float len = alongX ? Mathf.Abs(x1 - x0) : Mathf.Abs(z1 - z0), bay = len / bays;
                    for (int k = 0; k <= bays; k++)
                    {
                        float u = k * bay - len / 2, cx = (x0 + x1) / 2 + (alongX ? u : 0), cz = (z0 + z1) / 2 + (alongX ? 0 : u);
                        float ph = broken ? h * (0.4f + R2() * 0.6f) : h - R2() * 0.4f;
                        Box(cx, Y, cz, alongX ? 1.0f : 1.2f, ph, alongX ? 1.2f : 1.0f, wallM); Deco(Geo.Box(alongX ? 1.05f : 1.25f, 0.35f, alongX ? 1.25f : 1.05f), soot, cx, Y + ph - 0.15f, cz); Deco(Geo.Box(alongX ? 1.1f : 1.3f, 0.12f, alongX ? 1.3f : 1.1f), snowM, cx, Y + ph + 0.06f, cz);
                        if (k == bays) break;
                        float wx = cx + (alongX ? bay / 2 : 0), wz = cz + (alongX ? 0 : bay / 2), ow = bay - 1.0f;
                        Box(wx, Y, wz, alongX ? ow : 0.9f, 1.0f, alongX ? 0.9f : ow, wallM); Deco(Geo.Box(alongX ? ow : 1.0f, 0.1f, alongX ? 1.0f : ow), snowM, wx, Y + 1.05f, wz);
                        float top = broken ? (R2() < 0.5f ? 0 : h) : h;
                        if (top > 0) { Box(wx, Y + 3.4f, wz, alongX ? ow : 1.0f, top - 3.4f, alongX ? 1.0f : ow, wallM); Deco(Prism(ow, 0.7f, 1.05f), wallM, wx, Y + 3.4f - 0.01f, wz, Mathf.PI, alongX ? 0 : Mathf.PI / 2, 0); } // an arched head
                        else for (int j = 0; j < 3; j++) Ignore(Deco(Geo.Box(0.5f + R2() * 0.5f, 0.35f, 0.5f), wallM, wx + (R2() - 0.5f) * 2, Y + 0.18f, wz + (R2() - 0.5f) * 2, R2(), R2() * 3, R2()));
                    }
                }
                WallRun(-59.4f, -8, -59.4f, 8, 7.2f, 5, false, 11);
                WallRun(-59.4f, -8, -47.5f, -8, 5.5f, 4, true, 12); WallRun(-59.4f, 8, -47.5f, 8, 6.2f, 4, true, 13);
                Box(-47.4f, Y, -5.6f, 1.2f, 4.2f, 4.8f, wallM); Box(-47.4f, Y, 5.6f, 1.2f, 2.6f, 4.8f, wallM); // the front, mostly fallen
                Deco(Geo.Box(1.25f, 0.12f, 4.85f), snowM, -47.4f, Y + 4.26f, -5.6f); Deco(Geo.Box(1.25f, 0.12f, 4.85f), snowM, -47.4f, Y + 2.66f, 5.6f);
                Ignore(Deco(Geo.Arc(2.2f, 0.45f, 16), wallM, -46.2f, Y + 0.45f, 1.2f, Mathf.PI / 2 - 0.15f, Mathf.PI / 2, 0.4f)); // the fallen door arch
                // the roof timbers, burned through and come down inside, a heap of slates and the rafters lying across them
                foreach (var (bz, ang) in new[] { (-5.5f, 0.55f), (-1.5f, 0.62f), (2.5f, 0.5f), (6f, 0.7f) })
                {
                    float len = 9.5f; var beam = Deco(Geo.Box(0.45f, 0.45f, len), soot, -54.6f, Y + Mathf.Sin(ang) * len / 2, bz, 0, Mathf.PI / 2, 0); beam.transform.localRotation = Quaternion.Euler(0, 90, 0) * Quaternion.Euler(ang * Mathf.Rad2Deg, 0, 0); // high end on the back wall, low end on the floor
                    SolidAABB(V(-51.2f, Y, bz - 0.3f), V(-49.8f, Y + 0.8f, bz + 0.3f));
                }
                for (int k = 0; k < 14; k++) { var g = Deco(Geo.Icosahedron(1), k % 3 == 0 ? soot : roofM, -54 + (R() - 0.5f) * 6, Y + 0.25f, (R() - 0.5f) * 10, R() * 3, R() * 3, R() * 3); g.transform.localScale = V(0.5f + R() * 0.5f, 0.3f, 0.5f + R() * 0.5f); Ignore(g); }
                Box(-54.5f, Y, -2, 4.5f, 0.6f, 3.2f, roofM); Deco(Geo.Box(4.6f, 0.15f, 3.3f), snowM, -54.5f, Y + 0.66f, -2);
                // the clock off the tower, lying face-up in the rubble, stopped
                var cl = Mk.Piv(levelRoot, -55.5f, Y + 0.9f, 4.2f); cl.SetRot(-1.1f, 0.4f, 0);
                Mk.Msh(cl, Geo.Cyl(1.5f, 1.5f, 0.2f, 24), Mats.Std(0xd8d2c4, 0.6f), 0, 0, 0, Mathf.PI / 2, 0, 0); Mk.Msh(cl, Geo.Torus(1.55f, 0.13f, 24, 5), bronzeM);
                for (int k = 0; k < 12; k++) { float mk = k * Mathf.PI / 6; Mk.Msh(cl, Geo.Box(0.07f, k % 3 == 0 ? 0.3f : 0.18f, 0.03f), darkIronM, Mathf.Sin(mk) * 1.25f, Mathf.Cos(mk) * 1.25f, 0.12f, 0, 0, -mk); }
                Mk.Msh(cl, Geo.Box(0.1f, 0.8f, 0.03f), darkIronM, 0.15f, 0.35f, 0.14f, 0, 0, -0.4f); Mk.Msh(cl, Geo.Box(0.07f, 1.1f, 0.03f), darkIronM, -0.25f, 0.5f, 0.15f, 0, 0, 0.45f);
                SolidAABB(V(-57, Y, 3), V(-54, Y + 1.4f, 5.4f));
                // the lectern and the mayor frozen behind it; two councillors huddled by the back wall; an overturned table
                Box(-50.2f, Y, 0, 0.8f, 1.2f, 1.2f, darkWoodM); Deco(Geo.Box(1.0f, 0.12f, 1.4f), darkWoodM, -50.2f, Y + 1.3f, 0, 0, 0, 0.25f); Deco(Geo.Box(0.6f, 0.04f, 0.8f), Mats.Std(0xc8bfa8, 0.9f), -50.25f, Y + 1.39f, 0, 0, 0, 0.25f);
                FrozenFigure(-51.2f, 0, Mathf.PI / 2, 1, Y, 0);
                FrozenFigure(-57.6f, -3.5f, Mathf.PI / 2, 3, Y, 0); FrozenFigure(-57.4f, 2.0f, Mathf.PI / 2 + 0.5f, 3, Y, 0);
                var tb = Deco(Geo.Box(3.4f, 0.15f, 1.6f), darkWoodM, -52.5f, Y + 0.8f, -6.0f, 0, 0.3f, 1.35f); SolidAABB(V(-53.3f, Y, -7), V(-51.7f, Y + 1.6f, -5));
                for (int k = 0; k < 3; k++) Ignore(Deco(Geo.Box(0.5f, 0.06f, 0.5f), darkWoodM, -50.5f - k * 1.6f, Y + 0.2f, -4.5f + k * 0.7f, k, k * 1.3f, 0.6f)); // smashed chairs
                Deco(Geo.Box(0.08f, 3.4f, 2.4f), Mats.Std(0x23384c, 0.9f, 0, 0x02080e, null, true), -58.75f, Y + 4.4f, 0); Deco(Geo.Box(0.2f, 0.2f, 2.8f), darkIronM, -58.7f, Y + 6.15f, 0); // the town's banner, frozen stiff
                for (int k = 0; k < 5; k++) Ignore(Deco(Geo.Sphere(1, 10, 6), snowM, -57.5f + R() * 9, Y, (R() - 0.5f) * 14)).transform.localScale = V(1.3f + R(), 0.22f, 1.1f + R()); // snow blown in through the windows
            }            // lamp posts along the streets, carts and barrels by the doors, snow drifted against the walls
            foreach (var p in new[] { V(-5.5f, 0, 20), V(5.5f, 0, -20), V(20, 0, 5.5f), V(-20, 0, -5.5f), V(5.5f, 0, 38), V(-5.5f, 0, -32), V(-5.5f, 0, 50), V(5.5f, 0, 6 + 14) }) FrostLampPost(p.x, p.z);
            Sled(22, -6, 1.5f); Barrel(-21.5f, 8); Barrel(-21.5f, 9.2f); Barrel(21.5f, -21.5f); Crate(-22, -22, 1.2f); Crate(22.5f, 22.5f, 1.1f);
            for (int i = 0; i < 40; i++) { var hh = houses[i % houses.Length]; float s = R() < 0.5f ? -1 : 1; bool sideX = R() < 0.5f; float x = hh.x + (sideX ? s * (hh.w / 2 + 0.6f) : (R() - 0.5f) * hh.w), z = hh.z + (sideX ? (R() - 0.5f) * hh.d : s * (hh.d / 2 + 0.6f)); Snowbank(x, z, 1.0f + R() * 1.2f, 0.4f + R() * 0.4f, true); }
            PadAt(32, 52, 24); PadAt(-40, -46, 24); PadAt(48, 0, 22);
            // traps: icicles over the narrow lanes and round the square
            MechEaves(new[] {
                EaveOf(-28, -14, 8, 9, 6.5f, false, 1), EaveOf(28, -14, 8, 9, 5, false, -1), EaveOf(-28, 15, 8, 9, 5, false, 1),
                EaveOf(-15, -29, 9, 8, 6.5f, true, 1), EaveOf(15, -29, 9, 8, 5, true, 1), EaveOf(-15, 29, 9, 8, 5.5f, true, -1),
                EaveOf(15, 15, 9, 8, 5.5f, true, -1), EaveOf(-15, -15, 9, 8, 5.5f, true, 1),
            });
        }

        // ================= 8. ANCIENT RUINS =================
        void BuildRuins()
        {
            FrostMats(); Arena(68, 16);
            var rng = new System.Random(808); float R() => (float)rng.NextDouble();
            // the processional avenue from the south gate to the temple, paved, its edges broken
            Flat(Geo.Plane(14, 86), cobbleM, 0, 0.03f, 13);
            for (int k = 0; k < 30; k++) { float s = k % 2 == 0 ? -1 : 1, z = -28 + k * 2.9f + R(); Ignore(Deco(Geo.Box(1.0f + R() * 0.8f, 0.12f, 0.8f + R() * 0.6f), stoneDarkM, s * (7.2f + R() * 0.8f), 0.05f, z, 0, R(), 0)); }
            // the plaza before the temple, holding the shrine, four frost braziers round it
            Flat(Geo.Circle(10.5f, 48), stoneDarkM, 0, 0.035f, -6); Flat(Geo.Ring(10.2f, 10.8f, 48), stoneM, 0, 0.04f, -6);
            FrostShrine(0, -6);
            foreach (var p in new[] { V(-8.5f, 0, -14.5f), V(8.5f, 0, -14.5f), V(-8.5f, 0, 2.5f), V(8.5f, 0, 2.5f) }) FrostBrazier(p.x, p.z);
            // the temple: a two-tier podium, proper stairs, a colonnade front and back, the cella, the seated colossus within
            Box(0, 0, -47, 60, 1.5f, 30, M.block); Box(0, 1.5f, -49, 48, 1.5f, 24, M.block);
            TrimRect(0, -47, 60, 30, 1.5f); TrimRect(0, -49, 48, 24, 3);
            Stairs(0, -28, 22, V(0, 0, -1), 3, 0, 1.5f, 1.34f, M.block); Stairs(0, -32, 16, V(0, 0, -1), 4, 1.5f, 3, 1.24f, M.block);
            for (int k = 0; k < 7; k++)
            {
                float x = -21 + k * 7;
                if (k != 3) FrostColumn(x, -38.5f, 1.2f, 12, 3, k == 1 || k == 4, k == 1 ? 0.6f : -0.5f);
                FrostColumn(x, -59.5f, 1.2f, 12, 3, k == 1 || k == 5, k == 1 ? 2.4f : 3.6f);
            }
            Box(17.5f, 16.2f, -38.5f, 9.5f, 1.4f, 3, M.block); Box(0, 16.2f, -59.5f, 17, 1.4f, 3, M.block); Deco(Geo.Box(17.2f, 0.2f, 3.1f), snowM, 0, 17.7f, -59.5f); Deco(Geo.Box(9.7f, 0.2f, 3.1f), snowM, 17.5f, 17.7f, -38.5f);
            Deco(Prism(12, 3.6f, 1.6f), M.block, -14, 1.6f, -27.5f, 0.15f, 0.5f, 1.25f); // a fallen piece of the pediment by the stairs
            // the cella: thick walls with a doorway, roofless, its walls broken along the top
            foreach (var sx in new[] { -1f, 1f }) Box(sx * 9, 3, -50, 1.5f, 8, 16, M.tower);
            Box(0, 3, -58.25f, 19.5f, 8, 1.5f, M.tower);
            Box(-6.2f, 3, -42.3f, 7.1f, 8, 1.2f, M.tower); Box(6.2f, 3, -42.3f, 7.1f, 8, 1.2f, M.tower); Box(0, 9, -42.3f, 5.4f, 2, 1.2f, M.tower);
            for (int k = 0; k < 14; k++) { float a = k / 14f; var p = a < 0.25f ? V(-9, 0, -57 + a * 4 * 14) : a < 0.5f ? V(9, 0, -57 + (a - 0.25f) * 4 * 14) : a < 0.75f ? V(-9 + (a - 0.5f) * 4 * 18, 0, -58.25f) : V(-9 + (a - 0.75f) * 4 * 18, 0, -42.3f); Deco(Geo.Cone(0.6f, 0.8f + R() * 1.6f, 4), M.tower, p.x, 11 + 0.4f, p.z, 0, R() * 3, 0); }
            for (int k = 0; k < 3; k++) Deco(Geo.Box(0.5f, 0.5f, 17), darkWoodM, -5 + k * 5, 3.3f + k * 0.2f, -50, 0.05f * k, 0, 0.1f * (k - 1));
            SeatedFallenKing(0, -53, 0, 0.92f, 3);
            // the guardian statues down the avenue, facing across it; each one's gaze sweeps the paving in front of it
            var gazes = new List<(Vector3, Vector3, Vector3)>();
            foreach (var z in new[] { 36f, 22f, 8f })
                foreach (var sx in new[] { -1f, 1f })
                {
                    var eye = GuardianStatue(sx * 13.5f, z, sx < 0 ? Mathf.PI / 2 : -Mathf.PI / 2, 1);
                    gazes.Add((eye, V(sx * 8.5f, 0, z), V(-sx * 8.5f, 0, z)));
                    Snowbank(sx * 13.5f + sx * 3, z + 2, 1.6f, 0.6f, true);
                }
            // the east grounds: a ruined bath-hall round a frozen pool, its arches still standing
            WallStub(42, -21, 26, 5, true); WallStub(42, 13, 26, 4.2f, true); WallStub(29, -15, 12, 4.5f, false); WallStub(29, 7, 12, 4.5f, false); WallStub(55, -4, 30, 5.5f, false);
            Flat(Geo.Circle(7, 40), slickM, 42, 0.04f, -4); Ignore(Deco(Geo.Torus(7.3f, 0.35f, 40, 6), stoneM, 42, 0.15f, -4, Mathf.PI / 2, 0, 0));
            RuinArch(36, 6, 0, 3.4f); RuinArch(48, -16, 0, 3.4f); RuinArch(52, 6, Mathf.PI / 2, 3);
            FrozenFigure(44.5f, -1.5f, 2.6f, 0, 0.04f, 0); FrozenFigure(39, -7, 0.7f, 3, 0.04f, 0);
            FrostColumn(36, -10, 0.9f, 7, 0, true, 0.8f); FrostColumn(48, 2, 0.9f, 9, 0, false); FrostColumn(36, 2, 0.9f, 4, 0, true, 2.1f);
            Obelisk(40, -36, 9); Obelisk(56, 26, 11); Obelisk(32, 42, 8); Obelisk(-26, -36, 8);
            // the west ridge: a raised shelf of old masonry with passages cut under it, a roofed corridor behind
            float rx = -48, rw = 24;
            foreach (var (z0, z1) in new[] { (-34f, -9f), (-3f, 19f), (25f, 44f) }) Box(rx, 0, (z0 + z1) / 2, rw, 7, z1 - z0, M.tower);
            foreach (var zt in new[] { -6f, 22f }) Box(rx, 4, zt, rw, 3, 6, M.tower);
            Box(-64, 4, 5, 8, 3, 80, M.tower);
            for (int k = 0; k < 6; k++) { var g = Mk.Glow(levelRoot, 0x60e0ff, 3.5f, 0.6f); g.transform.localPosition = V(-64, 2.5f, -28 + k * 13); flames.Add(g.transform); Deco(Geo.Box(0.06f, 1.6f, 0.4f), frostRuneM, -67.9f, 2, -28 + k * 13); }
            Deco(Geo.Box(24.5f, 0.3f, 79), snowM, rx, 7.15f, 5); Stairs(-28, 36, 5, V(-1, 0, 0), 5, 0, 7, 1.6f, M.block);
            for (int k = 0; k < 5; k++) FrostColumn(-54 + (k % 2) * 10, -26 + k * 14, 0.9f, 2 + R() * 3, 7, true, k);
            WallStub(-44, 34, 10, 2.6f, true, 7); WallStub(-52, -18, 12, 2.2f, false, 7); WallStub(-40, 8, 8, 2.0f, false, 7);
            // scattered across the grounds: rubble, the dead trees, ice pushing up through the cracks, drums and drifts
            foreach (var p in new[] { V(-20, 0, -18), V(22, 0, -26), V(-24, 0, 48), V(24, 0, 50), V(-30, 0, 8), V(18, 0, 30) }) RubbleHeap(p.x, p.z, 1.8f + R() * 0.8f);
            foreach (var p in new[] { V(-22, 0, 30), V(26, 0, 36), V(-28, 0, -24), V(-18, 0, 56), V(20, 0, 60) }) DeadTree(p.x, p.z, 6 + R() * 3);
            foreach (var p in new[] { V(34, 0, -50), V(-34, 0, -52), V(60, 0, 52), V(-56, 0, 54), V(60, 0, -56) }) IceFormation(p.x, p.z, 6 + R() * 5, 0.9f + R() * 0.4f);
            FrostDrum(V(-18, 1.1f, 14), 1.1f, 2.6f, 0.9f); FrostDrum(V(19, 1.1f, 4), 1.1f, 2.6f, 2.0f); FrostDrum(V(-34, 1.0f, -10), 1.0f, 2.4f, 0.3f);
            for (int i = 0; i < 24; i++) Snowbank(-60 + R() * 120, -26 + R() * 90, 1.4f + R() * 2.2f, 0.4f + R() * 0.5f);
            PadAt(-30, -14, 24); PadAt(28, -42, 24); PadAt(0, -22, 22);
            // traps
            MechStatueShooters(gazes.ConvertAll(g => g.Item1).ToArray());
            MechRuneTiles(-8, -48.4f, 8, 3, 2, 3, 81);
            MechRuneTiles(-30, -37, 10, 2, 2, 1.5f, 82); MechRuneTiles(10, -37, 10, 2, 2, 1.5f, 83);
        }

        // ================= 9. THE PRISON OF FROST =================
        void BuildPrison()
        {
            FrostMats(); Arena(70, 24);
            var rng = new System.Random(909); float R() => (float)rng.NextDouble();
            // the south gatehouse you come in by: two towers, the arch, the portcullis jammed half up
            foreach (var sx in new[] { -1f, 1f }) { Box(sx * 10, 0, 64, 7, 16, 12, M.tower); for (int k = 0; k < 3; k++) Box(sx * 10 + (k - 1) * 2.4f, 16, 58.6f, 1.2f, 1.4f, 1.2f, M.tower); }
            Box(0, 11, 64, 13, 3.5f, 6, M.tower);
            for (int k = 0; k < 13; k++) { float x = -4.8f + k * 0.8f; bool bent = k == 4 || k == 9; var bar = Deco(Geo.Cyl(0.07f, 0.07f, 5, 6), darkIronM, x, 8.5f, 64, bent ? 0.4f : 0, 0, 0); Ignore(bar); Ignore(Deco(Geo.Cone(0.1f, 0.4f, 5), darkIronM, x, 5.8f + (bent ? 0.5f : 0), 64 + (bent ? 1 : 0), Mathf.PI, 0, 0)); }
            foreach (var y in new[] { 7f, 10f }) Ignore(Deco(Geo.Box(10.4f, 0.16f, 0.16f), darkIronM, 0, y, 64));
            FrostBrazier(-5.5f, 54); FrostBrazier(5.5f, 54);
            // the watchtowers at the four corners, each with its cabin and lamp
            Vector3 Tower(float x, float z)
            {
                Box(x, 0, z, 7, 15, 7, M.tower); TrimRect(x, z, 7, 7, 15);
                Box(x, 15, z, 8.6f, 0.6f, 8.6f, darkIronM);
                foreach (var a in new[] { -3.9f, 3.9f }) foreach (var b in new[] { -3.9f, 3.9f }) Deco(Geo.Box(0.3f, 3.6f, 0.3f), darkIronM, x + a, 17.4f, z + b);
                foreach (var a in new[] { -4.1f, 4.1f }) { Deco(Geo.Box(0.14f, 1.0f, 8.4f), darkIronM, x + a, 16.1f, z); Deco(Geo.Box(8.4f, 1.0f, 0.14f), darkIronM, x, 16.1f, z + a); }
                Deco(Geo.Cyl(0.2f, 6.6f, 3.4f, 4), roofM, x, 20.9f, z, 0, Mathf.PI / 4, 0); Deco(Geo.Cyl(0.2f, 6.0f, 3.0f, 4), snowM, x, 21.2f, z, 0, Mathf.PI / 4, 0);
                for (int k = 0; k < 3; k++) Deco(Geo.Box(7.2f, 0.25f, 7.2f), stoneDarkM, x, 4 + k * 4.5f, z);
                var lamp = V(x - Mathf.Sign(x) * 3.2f, 17.6f, z - Mathf.Sign(z) * 3.2f);
                Deco(Geo.Cyl(0.6f, 0.9f, 1.0f, 10), darkIronM, lamp.x, lamp.y, lamp.z, Mathf.Sign(z) * 0.6f, 0, -Mathf.Sign(x) * 0.6f);
                return lamp;
            }
            var lampNW = Tower(-60, -60); Tower(60, -60); Tower(-60, 60); var lampSE = Tower(60, 60);
            // the cell blocks down the west and east walls: barred cells, prisoners frozen inside, a gallery walkway on the roof
            foreach (var s in new[] { -1f, 1f })
            {
                float back = s * 70, front = s * 61, mid = s * 65.5f;
                Box(mid, 6, -6, 9, 0.8f, 72, M.block); Deco(Geo.Box(9.2f, 0.2f, 72.2f), snowM, mid, 6.9f, -6);
                for (int k = 0; k <= 12; k++) Box(mid, 0, -42 + k * 6, 9, 6, 0.8f, M.tower);
                for (int k = 0; k < 12; k++)
                {
                    float zc = -39 + k * 6; bool broken = k % 4 == 1 || k == 7;
                    for (int b = 0; b < 9; b++)
                    {
                        float bz = zc - 2.4f + b * 0.6f;
                        if (broken && (b == 3 || b == 4 || b == 5)) { if (b == 4) continue; var bent = Deco(Geo.Cyl(0.06f, 0.06f, 5.6f, 6), darkIronM, front + s * 0.9f, 2.8f, bz + (b - 4) * 0.5f, 0, 0, -s * 0.35f); Ignore(bent); continue; } // bars torn outward
                        Ignore(Deco(Geo.Cyl(0.06f, 0.06f, 6, 6), darkIronM, front, 3, bz));
                    }
                    foreach (var y in new[] { 1.0f, 5.0f }) Ignore(Deco(Geo.Box(0.1f, 0.12f, 5.4f), darkIronM, front, y, zc));
                    if (!broken) SolidAABB(V(Mathf.Min(front, front + s * 0.2f) - 0.1f, 0, zc - 2.7f), V(Mathf.Max(front, front + s * 0.2f) + 0.1f, 6, zc + 2.7f));
                    Box(s * 68.6f, 0, zc + 1.2f, 1.6f, 0.7f, 3.2f, woodM); Ignore(Deco(Geo.Box(1.4f, 0.12f, 2.8f), Mats.Std(0x4a4436, 0.95f), s * 68.6f, 0.76f, zc + 1.2f)); // a plank bunk and its blanket
                    Ignore(Deco(Geo.Cyl(0.25f, 0.2f, 0.4f, 8), woodM, s * 66.6f, 0.2f, zc - 2.2f));
                    if (k % 3 != 2)
                    {
                        bool chained = k % 2 == 0; float px = s * 67.4f;
                        FrozenFigure(px, zc - 0.8f, s > 0 ? -Mathf.PI / 2 : Mathf.PI / 2, chained ? 4 : 3, 0, 2, k % 4 != 3);
                        if (chained) foreach (var dz in new[] { -0.55f, 0.55f }) ChainLine(V(px + s * 0.1f, 2.2f, zc - 0.8f + dz), V(back - s * 0.6f, 3.6f, zc - 0.8f + dz * 1.6f), 0.15f, 0.22f);
                    }
                    if (k % 2 == 0) Lantern(levelRoot, front - s * 0.6f, 4.6f, zc + 3, k % 4 == 0);
                }
                // the gallery: a railing along the roof edge, stairs up at both ends
                Deco(Geo.Box(0.12f, 0.12f, 72), darkIronM, front + s * 0.2f, 7.8f, -6); SolidAABB(V(Mathf.Min(front, front + s * 0.4f), 6.8f, -40), V(Mathf.Max(front, front + s * 0.4f), 7.9f, 28));
                for (int k = 0; k < 25; k++) Ignore(Deco(Geo.Box(0.1f, 1.1f, 0.1f), darkIronM, front + s * 0.2f, 7.35f, -42 + k * 3));
                Stairs(mid, 38, 6, V(0, 0, -1), 5, 0, 6.8f, 1.6f, M.block); Stairs(mid, -50, 6, V(0, 0, 1), 5, 0, 6.8f, 1.6f, M.block);
            }
            // the yard: the Great Cage where the Traitor was held, torn open from inside
            var vault = V(0, 0, -24);
            Flat(Geo.Ring(15, 16.2f, 80), frostRuneM, vault.x, 0.05f, vault.z); Flat(Geo.Ring(10.5f, 11.1f, 64), frostRuneM, vault.x, 0.05f, vault.z);
            for (int k = 0; k < 16; k++) { float a = k / 16f * Mathf.PI * 2; Ignore(Deco(Geo.Box(0.25f, 0.03f, 2.6f), frostRuneM, vault.x + Mathf.Cos(a) * 13, 0.06f, vault.z + Mathf.Sin(a) * 13, 0, -a, 0)); }
            var gouge = Mats.Std(0x05080c, 0.9f);
            for (int k = 0; k < 4; k++) Ignore(Deco(Geo.Box(0.6f, 0.05f, 12), gouge, vault.x - 3 + k * 2, 0.04f, vault.z + 18, 0, 0.3f, 0)); // claw marks where it dragged itself out
            var tops = new List<Vector3>();
            for (int k = 0; k < 24; k++)
            {
                float a = k / 24f * Mathf.PI * 2, x = vault.x + Mathf.Cos(a) * 12, z = vault.z + Mathf.Sin(a) * 12; var outward = V(Mathf.Cos(a), 0, Mathf.Sin(a));
                bool torn = Mathf.Sin(a) > 0.7f;
                if (!torn)
                {
                    Deco(Geo.Cyl(0.32f, 0.38f, 16, 8), darkIronM, x, 8, z); SolidAABB(V(x - 0.38f, 0, z - 0.38f), V(x + 0.38f, 16, z + 0.38f)); tops.Add(V(x, 16, z));
                    if (k % 3 == 0) Deco(Geo.Box(0.9f, 0.5f, 0.9f), darkIronM, x, 0.25f, z);
                }
                else
                {
                    // torn open: the bars bent outward and snapped
                    Deco(Geo.Cyl(0.32f, 0.38f, 5, 8), darkIronM, x, 2.5f, z); SolidAABB(V(x - 0.38f, 0, z - 0.38f), V(x + 0.38f, 5, z + 0.38f));
                    var up = Deco(Geo.Cyl(0.3f, 0.32f, 6, 8), darkIronM, 0, 0, 0); var dir = (Vector3.up + outward * 1.3f).normalized;
                    up.transform.localPosition = V(x, 5, z) + dir * 3; up.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
                    Ignore(Deco(Geo.Cyl(0.3f, 0.32f, 2.2f, 8), darkIronM, x + outward.x * 5 + (R() - 0.5f), 0.3f, z + outward.z * 5 + (R() - 0.5f), Mathf.PI / 2, a, 0)); // a snapped length lying in the snow
                }
            }
            for (int i = 0; i < tops.Count - 1; i++) if (Vector3.Distance(tops[i], tops[i + 1]) < 4) { var m = (tops[i] + tops[i + 1]) / 2; var d = tops[i + 1] - tops[i]; var ring = Deco(Geo.Box(0.7f, 0.7f, d.magnitude + 0.3f), darkIronM, m.x, m.y, m.z); ring.transform.localRotation = Quaternion.LookRotation(d); }
            // inside: the binding frame he was strapped to - four iron posts braced in an X, its bands burst open from within -
            // on a raised rune dais, the shattered crystal of the ice that held him blown outward through the torn bars
            {
                var c = vault; var ironB = Mats.Std(0x30343c, 0.4f, 0.75f); var rust = Mats.Std(0x4a3a32, 0.7f, 0.5f);
                Ignore(Deco(Geo.Cyl(6.6f, 7.0f, 0.45f, 24), stoneDarkM, c.x, 0.22f, c.z)); Ignore(Deco(Geo.Cyl(4.6f, 4.8f, 0.2f, 20), Mats.Std(0x3a4a5c, 0.5f, 0.3f), c.x, 0.52f, c.z));
                Flat(Geo.Ring(5.6f, 5.85f, 48), frostRuneM, c.x, 0.46f, c.z); Flat(Geo.Ring(3.9f, 4.05f, 40), frostRuneM, c.x, 0.63f, c.z);
                for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; Ignore(Deco(Geo.Box(0.16f, 0.02f, 1.1f), frostRuneM, c.x + Mathf.Cos(a) * 4.9f, 0.47f, c.z + Mathf.Sin(a) * 4.9f, 0, -a, 0)); }
                // the frame: posts, a top ring of beams, X-braces across the back and sides
                var posts = new[] { V(-2.6f, 0, -2.6f), V(2.6f, 0, -2.6f), V(-2.6f, 0, 2.6f), V(2.6f, 0, 2.6f) };
                foreach (var pp in posts)
                {
                    bool front = pp.z > 0; var w = c + pp;
                    if (!front) { Box(w.x, 0.6f, w.z, 0.7f, 9.4f, 0.7f, ironB); Deco(Geo.Cone(0.5f, 1.4f, 4), ironB, w.x, 10.7f, w.z, 0, Mathf.PI / 4, 0); }
                    else
                    {
                        // the front posts were wrenched outward when he tore free
                        Box(w.x, 0.6f, w.z, 0.7f, 4.4f, 0.7f, ironB);
                        var up = Deco(Geo.Box(0.65f, 5.4f, 0.65f), ironB, 0, 0, 0); var dir = (Vector3.up + V(Mathf.Sign(pp.x) * 0.5f, 0, 0.9f)).normalized;
                        up.transform.localPosition = V(w.x, 5.0f, w.z) + dir * 2.7f; up.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
                    }
                    for (int k = 0; k < 3; k++) Deco(Geo.Box(0.95f, 0.3f, 0.95f), rust, w.x, 1.4f + k * 3.0f, w.z);
                }
                foreach (var (ax, az, len, rot) in new[] { (0f, -2.6f, 5.9f, 0f), (-2.6f, 0f, 5.9f, Mathf.PI / 2), (2.6f, 0f, 5.9f, Mathf.PI / 2) }) Deco(Geo.Box(len, 0.45f, 0.45f), ironB, c.x + ax, 9.8f, c.z + az, 0, rot, 0);
                foreach (var s2 in new[] { -1f, 1f }) { Deco(Geo.Box(0.25f, 9.6f, 0.25f), ironB, c.x, 5.2f, c.z - 2.6f, 0, 0, s2 * 0.5f); Deco(Geo.Box(0.25f, 9.6f, 0.25f), ironB, c.x + s2 * 2.6f, 5.2f, c.z, s2 * 0.5f, 0, 0); }
                SolidAABB(V(c.x - 3.0f, 0, c.z - 3.0f), V(c.x + 3.0f, 0.6f, c.z - 2.2f));
                // the iron bands that went round him, burst and bent open, and the great shackles hanging off the frame
                for (int k = 0; k < 3; k++) { var band = Deco(Geo.Torus(2.0f + k * 0.15f, 0.12f, 24, 5, Mathf.PI * (1.1f + k * 0.15f)), rust, c.x, 2.4f + k * 2.4f, c.z, Mathf.PI / 2 + (k - 1) * 0.25f, k * 1.3f + 2.2f, (k - 1) * 0.3f); Ignore(band); }
                foreach (var (sx, sy) in new[] { (-1f, 7.5f), (1f, 7.2f), (-1f, 3.4f) })
                {
                    var top = c + V(sx * 2.25f, sy, -2.3f); var bot = top + V(-sx * 0.6f, -2.2f, 0.9f);
                    ChainLine(top, bot, 0.15f, 0.28f); Ignore(Deco(Geo.Torus(0.42f, 0.11f, 16, 6), ironB, bot.x, bot.y - 0.35f, bot.z, 0.4f, sx, 0.2f)); Ignore(Deco(Geo.Box(0.3f, 0.2f, 0.2f), ironB, bot.x, bot.y - 0.75f, bot.z));
                }
                Ignore(Deco(Geo.Torus(0.42f, 0.11f, 16, 6), ironB, c.x + 1.2f, 0.75f, c.z + 1.0f, Mathf.PI / 2, 0.6f, 0)); ChainLine(c + V(2.2f, 2.6f, -2.3f), c + V(1.3f, 0.7f, 0.8f), 0.3f, 0.28f); // one shackle torn down to the dais
                // the shattered ice of his prison: great crystals blown outward, mostly through the torn south side
                var deep = Mats.Std(0x8ac4e8, 0.05f, 0.25f, 0x0a3050);
                for (int k = 0; k < 14; k++)
                {
                    float a = k / 14f * Mathf.PI * 2 + 0.2f, south = Mathf.Max(0, Mathf.Sin(a)), d = 3.6f + R() * 1.6f + south * 2.5f, h = 1.6f + R() * 2.6f - south * 0.8f;
                    var cp = c + V(Mathf.Cos(a) * d, 0.3f, Mathf.Sin(a) * d); float lean = 0.5f + south * 0.6f + R() * 0.2f;
                    HexCrystal(cp, 0.35f + R() * 0.35f, h, Mathf.Sin(a) * lean, -Mathf.Cos(a) * lean, k % 3 == 0 ? iceTransM : k % 3 == 1 ? iceM : deep, R() * 3);
                    if (k % 2 == 0) SolidAABB(V(cp.x - 0.4f, 0, cp.z - 0.4f), V(cp.x + 0.4f, h * 0.6f, cp.z + 0.4f));
                }
                // what's left of the block itself: jagged stumps of ice round where he stood, frost-blood frozen on the dais
                for (int k = 0; k < 9; k++) { float a = k / 9f * Mathf.PI * 2; if (Mathf.Sin(a) > 0.5f) continue; var cp = c + V(Mathf.Cos(a) * 1.7f, 0.6f, Mathf.Sin(a) * 1.7f); HexCrystal(cp, 0.45f, 0.9f + R() * 1.4f, Mathf.Sin(a) * 0.2f, -Mathf.Cos(a) * 0.2f, iceTransM, R() * 3); }
                var fbM = Mats.Trans(0x1a5aa8, 0.8f); for (int k = 0; k < 6; k++) { float a = R() * 6.28f, d = R() * 3.5f; Flat(Geo.Circle(0.4f + R() * 0.7f, 12), fbM, c.x + Mathf.Cos(a) * d, 0.64f, c.z + Mathf.Sin(a) * d); }
                var hg = Mk.Glow(levelRoot, 0x60c8ff, 14, 0.3f); hg.transform.localPosition = V(c.x, 3, c.z);
                // the chains that held the frame to the anchor pillars lie snapped across the yard
                ChainLine(c + V(0.5f, 0.7f, 2.4f), c + V(4.5f, 0.25f, 15), 0, 0.42f); ChainLine(c + V(-0.5f, 0.7f, 2.4f), c + V(-6, 0.25f, 13), 0, 0.42f);
            }            // the four anchor pillars round the cage, their chains snapped
            for (int k = 0; k < 4; k++)
            {
                float a = k * Mathf.PI / 2 + Mathf.PI / 4, x = vault.x + Mathf.Cos(a) * 28, z = vault.z + Mathf.Sin(a) * 28;
                Box(x, 0, z, 4, 16, 4, M.tower); TrimRect(x, z, 4, 4, 16); Deco(Geo.Cone(1.8f, 3, 4), M.tower, x, 17.5f, z, 0, Mathf.PI / 4, 0);
                foreach (var y in new[] { 4f, 9f }) Deco(Geo.Box(4.3f, 0.5f, 4.3f), stoneDarkM, x, y, z);
                var top = V(x, 15, z); var cage = V(vault.x + Mathf.Cos(a) * 12.5f, 15.5f, vault.z + Mathf.Sin(a) * 12.5f);
                if (k % 2 == 0) ChainLine(top, cage, 2.5f, 0.75f);
                else { ChainLine(top, Vector3.Lerp(top, cage, 0.45f) - V(0, 2.4f, 0), 1.2f, 0.75f); HangingChain(cage.x, cage.z, 15.5f, 8); }
            }
            // the warden's keep along the north wall: battlements, a gate, frozen guards drawn up before it
            Box(0, 0, -62, 34, 12, 14, M.tower); TrimRect(0, -62, 34, 14, 12);
            for (int k = 0; k < 9; k++) Box(-16 + k * 4, 12, -55.6f, 1.6f, 1.6f, 1.2f, M.tower);
            Deco(Geo.Arc(3.4f, 0.5f, 18), stoneDarkM, 0, 4.5f, -54.9f); Deco(Geo.Box(6.6f, 4.5f, 0.2f), Mats.Std(0x0a0e12, 0.9f), 0, 2.25f, -54.95f);
            for (int k = 0; k < 9; k++) Ignore(Deco(Geo.Cyl(0.08f, 0.08f, 6.4f, 6), darkIronM, -3.2f + k * 0.8f, 4.2f, -54.6f));
            foreach (var sx in new[] { -1f, 1f }) { FrostBrazier(sx * 6, -52); Deco(Geo.Box(3.6f, 9, 0.08f), Mats.Std(0x23384c, 0.9f, 0, 0x02080e, null, true), sx * 11, 7, -54.9f); }
            for (int r = 0; r < 2; r++) for (int c = 0; c < 5; c++) { if ((r * 5 + c) % 4 == 2) { for (int j = 0; j < 4; j++) Ignore(Deco(Geo.Octahedron(0.4f + R() * 0.3f), iceM, -10 + c * 5 + (R() - 0.5f) * 2, 0.2f, -46 - r * 3.4f + (R() - 0.5f) * 2, R(), R() * 3, R())); continue; } FrozenFigure(-10 + c * 5, -46 - r * 3.4f, 0, 5, 0, 1); }
            // gantries over the yard: iron beams on lattice towers, gibbet cages and the pendulum blades hanging off them
            var pends = new List<(Vector3, float, float, float)>();
            foreach (var (gz, xs) in new[] { (12f, new[] { -24f, -11f, 11f, 24f }), (36f, new[] { -18f, 18f }) })
            {
                foreach (var sx in new[] { -1f, 1f }) { Box(sx * 38, 0, gz, 2.4f, 16, 2.4f, darkIronM); for (int k = 0; k < 4; k++) Ignore(Deco(Geo.Box(0.15f, 4.2f, 0.15f), darkIronM, sx * 38, 2 + k * 3.8f, gz + 1.3f, 0.75f, 0, 0)); }
                Box(0, 15, gz, 78, 1.2f, 1.6f, darkIronM); Deco(Geo.Box(78, 0.2f, 1.7f), snowM, 0, 16.3f, gz);
                for (int i = 0; i < xs.Length; i++) pends.Add((V(xs[i], 14.6f, gz), 13.2f, 0f, i * 1.6f + gz * 0.1f));
                foreach (var cx in gz < 20 ? new[] { -4.5f, 4.5f } : new[] { -6f, 6f })
                {
                    HangingChain(cx, gz, 15, 11.5f); int kk = (int)cx; var cg = Mk.Piv(levelRoot, cx, 10, gz);
                    for (int b = 0; b < 10; b++) { float a = b * Mathf.PI / 5; Mk.Msh(cg, Geo.Cyl(0.05f, 0.05f, 2.6f, 4), darkIronM, Mathf.Cos(a) * 0.85f, 0, Mathf.Sin(a) * 0.85f); }
                    Mk.Msh(cg, Geo.Cyl(0.95f, 0.95f, 0.12f, 12), darkIronM, 0, -1.3f, 0); Mk.Msh(cg, Geo.Cone(0.95f, 0.6f, 10), darkIronM, 0, 1.6f, 0);
                    foreach (var r in cg.t.GetComponentsInChildren<Renderer>()) r.gameObject.layer = Mk.LayerIgnore;
                    float czz = gz; anims.Add((t, dt) => cg.SetRot(Mathf.Sin(t * 0.7f + kk + czz) * 0.06f, t * 0.08f, 0));
                }
            }
            FrostShrine(0, 24);
            // the yard's dressing: the gallows, the stocks, guard huts, weapon racks, supplies, drains, bones, chains on the walls
            {
                // the gallows: a platform, its steps, the beam and the ropes, a trapdoor hanging open
                float gx = -30, gz = 50;
                Box(gx, 0, gz, 7, 2.2f, 5, woodM); Deco(Geo.Box(7.2f, 0.12f, 5.2f), snowM, gx, 2.26f, gz); Stairs(gx + 3.5f, gz, 2.4f, V(1, 0, 0), 3, 0, 2.2f, 0.8f, darkWoodM);
                foreach (var sx in new[] { -2.4f, 2.4f }) { Deco(Geo.Box(0.35f, 4.4f, 0.35f), darkWoodM, gx + sx, 4.4f, gz - 1.6f); SolidAABB(V(gx + sx - 0.2f, 2.2f, gz - 1.8f), V(gx + sx + 0.2f, 6.6f, gz - 1.4f)); }
                Deco(Geo.Box(5.6f, 0.35f, 0.35f), darkWoodM, gx, 6.5f, gz - 1.6f); Deco(Geo.Box(5.7f, 0.12f, 0.4f), snowM, gx, 6.72f, gz - 1.6f);
                foreach (var rx in new[] { -1.4f, 0f, 1.4f }) { Ignore(Deco(Geo.Cyl(0.03f, 0.03f, 2.0f, 5), Mats.Std(0x6a5a40, 0.95f), gx + rx, 5.4f, gz - 1.6f)); Ignore(Deco(Geo.Torus(0.18f, 0.03f, 10, 4), Mats.Std(0x6a5a40, 0.95f), gx + rx, 4.3f, gz - 1.6f)); }
                Ignore(Deco(Geo.Box(1.6f, 0.08f, 1.4f), darkWoodM, gx, 1.5f, gz - 0.2f, 1.2f, 0, 0)); // the trapdoor, hanging open
                // the stocks, a prisoner frozen in them
                float sx0 = -16, sz0 = 48;
                foreach (var px in new[] { -1.4f, 1.4f }) Box(sx0 + px, 0, sz0, 0.3f, 1.2f, 0.3f, darkWoodM);
                Box(sx0, 1.0f, sz0, 3.2f, 0.5f, 0.3f, woodM); Deco(Geo.Box(3.3f, 0.1f, 0.35f), snowM, sx0, 1.55f, sz0);
                FrozenFigure(sx0, sz0 + 0.7f, Mathf.PI, 3, 0, 2, false);
                // guard huts at the corners of the yard
                foreach (var (hx, hz) in new[] { (-48f, 42f), (48f, 42f), (-48f, -48f), (48f, -48f) })
                {
                    Box(hx, 0, hz, 4, 3, 4, woodM); Deco(Prism(5, 1.6f, 5), roofM, hx, 3, hz); Deco(Prism(4.6f, 1.2f, 4.8f), snowM, hx, 3.25f, hz); SolidGable(hx, hz, 3, 5, 1.6f, 5, false);
                    Deco(Geo.Box(1.0f, 1.9f, 0.06f), darkWoodM, hx - Mathf.Sign(hx) * 2.03f, 0.95f, hz, 0, Mathf.PI / 2, 0); Deco(Geo.Box(0.7f, 0.5f, 0.06f), litGlassM, hx, 1.9f, hz - Mathf.Sign(hz) * 2.03f);
                    var lg = Mk.Glow(levelRoot, 0xffb060, 3, 0.3f); lg.transform.localPosition = V(hx, 1.9f, hz - Mathf.Sign(hz) * 2.6f);
                    Barrel(hx + 2.8f, hz + 1.2f); Crate(hx - 2.9f, hz - 1.4f, 1.1f);
                }
                // weapon racks: spears and a shield frozen in place
                foreach (var (wx, wz, ry) in new[] { (-24f, -50f, 0f), (24f, -50f, 0f), (-44f, 20f, Mathf.PI / 2), (44f, -20f, Mathf.PI / 2) })
                {
                    var rk = Mk.Piv(levelRoot, wx, 0, wz); rk.SetRot(0, ry, 0);
                    foreach (var e in new[] { -1.6f, 1.6f }) Mk.Msh(rk, Geo.Box(0.2f, 2.2f, 0.2f), darkWoodM, e, 1.1f, 0);
                    Mk.Msh(rk, Geo.Box(3.4f, 0.15f, 0.25f), darkWoodM, 0, 2.0f, 0); Mk.Msh(rk, Geo.Box(3.4f, 0.15f, 0.25f), darkWoodM, 0, 0.4f, 0);
                    for (int k = 0; k < 5; k++) { Mk.Msh(rk, Geo.Cyl(0.03f, 0.03f, 2.8f, 5), darkWoodM, -1.2f + k * 0.6f, 1.4f, 0.1f, 0.1f, 0, 0); Mk.Msh(rk, Geo.Cone(0.06f, 0.3f, 4), darkIronM, -1.2f + k * 0.6f, 2.9f, 0.25f, 0.1f, 0, 0); }
                    Mk.Msh(rk, Geo.Cyl(0.5f, 0.5f, 0.08f, 12), darkIronM, 0, 0.9f, 0.3f, Mathf.PI / 2, 0, 0); Mk.Msh(rk, Geo.Box(3.5f, 0.1f, 0.3f), snowM, 0, 2.12f, 0);
                    foreach (var r in rk.t.GetComponentsInChildren<Renderer>()) r.gameObject.layer = Mk.LayerIgnore;
                    var ex = Quaternion.Euler(0, ry * Mathf.Rad2Deg, 0) * V(1.8f, 0, 0.4f); SolidAABB(V(wx - Mathf.Abs(ex.x), 0, wz - Mathf.Abs(ex.z)), V(wx + Mathf.Abs(ex.x), 2.2f, wz + Mathf.Abs(ex.z)));
                }
                // supplies piled by the gate and the keep; carts
                foreach (var (cx, cz) in new[] { (-14f, 56f), (14f, 54f), (-29f, -47f), (29f, -47f) }) { Crate(cx, cz, 1.3f); Crate(cx + 1.4f, cz + 0.3f, 1.0f, 0, 0.4f); Crate(cx + 0.5f, cz, 0.9f, 1.3f, 0.2f); Barrel(cx - 1.5f, cz + 0.8f); Barrel(cx - 1.4f, cz - 0.6f); }
                Sled(20, 46, 0.4f); Sled(-38, -20, 1.9f);
                // a guards' table by the keep, two of them frozen sitting at it
                Box(-30, 0, -40, 3.2f, 1.0f, 1.6f, woodM); Deco(Geo.Box(3.3f, 0.1f, 1.7f), snowM, -30, 1.05f, -40);
                foreach (var sz in new[] { -1.4f, 1.4f }) { Box(-30, 0, -40 + sz, 3.0f, 0.55f, 0.5f, darkWoodM); FrozenFigure(-29.2f, -40 + sz * 1.05f, sz > 0 ? Mathf.PI : 0, 3, 0.1f, 1, false); }
                for (int k = 0; k < 3; k++) Ignore(Deco(Geo.Cyl(0.08f, 0.07f, 0.2f, 8), darkIronM, -31 + k * 0.9f, 1.16f, -40 + (k - 1) * 0.3f));
                // iron drain grates in the flagstones, frozen blood round the cage, bones in the snow
                var grateM = Mats.Std(0x10141a, 0.6f, 0.5f);
                foreach (var (dx, dz) in new[] { (-18f, 20f), (18f, 20f), (-26f, -8f), (26f, -8f), (0f, 40f), (-36f, 34f), (36f, 30f) })
                { Flat(Geo.Plane(1.8f, 1.8f), grateM, dx, 0.03f, dz); for (int k = 0; k < 5; k++) Ignore(Deco(Geo.Box(0.08f, 0.05f, 1.8f), darkIronM, dx - 0.72f + k * 0.36f, 0.05f, dz)); }
                var bloodM = Mats.Trans(0x4a0808, 0.75f);
                for (int k = 0; k < 9; k++) { float a = R() * 6.28f, r = 14 + R() * 6; Flat(Geo.Circle(0.6f + R() * 1.2f, 14), bloodM, vault.x + Mathf.Cos(a) * r, 0.045f, vault.z + Mathf.Sin(a) * r); }
                var boneM = Mats.Std(0xd8d4c4, 0.7f);
                for (int k = 0; k < 18; k++) { float bx = -50 + R() * 100, bz = -40 + R() * 90; Ignore(Deco(Geo.Cyl(0.05f, 0.05f, 0.5f + R() * 0.4f, 5), boneM, bx, 0.06f, bz, Mathf.PI / 2, R() * 3, 0)); if (k % 3 == 0) Ignore(Deco(Geo.Sphere(0.14f, 8, 6), boneM, bx + 0.4f, 0.12f, bz + 0.2f)); }
                // shackles along the south wall, either side of the gate, some still holding their prisoners
                foreach (var sx in new[] { -1f, 1f }) for (int k = 0; k < 6; k++) { float wx = sx * (18 + k * 7); ChainLine(V(wx - 0.6f, 5, 69.6f), V(wx + 0.6f, 5, 69.6f), 1.4f, 0.25f); Ignore(Deco(Geo.Torus(0.14f, 0.04f, 10, 4), darkIronM, wx, 3.6f, 69.6f)); if (k % 2 == 0) FrozenFigure(wx, 68.8f, Mathf.PI, 4, 0, 2, k % 4 == 0); }
                // frost braziers lighting the yard
                foreach (var (bx, bz) in new[] { (-30f, 24f), (30f, 24f), (-30f, -2f), (30f, -2f), (-12f, -44f), (12f, -44f) }) FrostBrazier(bx, bz);
            }
            // ice grown through the flagstones, drifts against the walls
            foreach (var p in new[] { V(-30, 0, -60), V(30, 0, -62), V(-44, 0, 48), V(44, 0, 50), V(-40, 0, -30), V(42, 0, -34) }) IceFormation(p.x, p.z, 6 + R() * 6, 1 + R() * 0.5f);
            for (int i = 0; i < 26; i++) { float a = R() * 6.28f; Snowbank(Mathf.Clamp(Mathf.Cos(a) * 66, -52, 52), Mathf.Sin(a) * 66, 1.6f + R() * 2.4f, 0.5f + R() * 0.6f, true); }
            PadAt(-44, 0, 24); PadAt(44, -10, 24); PadAt(0, 46, 22);
            // traps
            MechPendulums(pends.ToArray());
            MechSearchlights(new[] { (lampNW, V(-16, 0, 8), 20f, 24f, 0.32f, 0f), (lampSE, V(16, 0, 6), 20f, 24f, 0.27f, 2.2f) });
        }

        // ================= 10. THRONE OF FROST =================
        void BuildThrone()
        {
            FrostMats(); Arena(74, 34);
            var rng = new System.Random(1010); float R() => (float)rng.NextDouble();
            // the aisle: a long inlay of black ice with runes, running to the throne
            Flat(Geo.Plane(12, 130), Mats.Std(0x0a1220, 0.05f, 0.4f, 0x020810), 0, 0.03f, 0);
            for (int k = 0; k < 20; k++) Flat(Geo.Plane(1.2f, 0.3f), frostRuneM, (k % 2 == 0 ? -5.4f : 5.4f), 0.05f, -60 + k * 6.5f);
            Flat(Geo.Ring(13, 14, 80), frostRuneM, 0, 0.06f, -30); Flat(Geo.Ring(8, 8.5f, 64), frostRuneM, 0, 0.06f, -30);
            // the dais: five steps with frost runes glowing in their risers, iron on their edges, up to the throne platform
            ironM = darkIronM; obsM = Mats.Std(0x18202c, 0.18f, 0.6f, 0x040a14); // (DemonStatue's materials are normally made by the hell levels)
            for (int k = 0; k < 5; k++)
            {
                float w = 34 - k * 4, fz = -50 - k * 2.4f + 1.2f, h = 1.2f * (k + 1);
                Box(0, 0, -50 - k * 2.4f, w, h, 2.4f, M.block);
                Ignore(Deco(Geo.Box(w - 1.2f, 0.12f, 0.04f), frostRuneM, 0, h - 0.55f, fz + 0.03f)); Deco(Geo.Box(w, 0.08f, 0.12f), darkIronM, 0, h + 0.02f, fz - 0.05f);
            }
            Box(0, 0, -66, 30, 6, 10, M.block); TrimRect(0, -66, 30, 10, 6);
            Flat(Geo.Ring(4.2f, 4.5f, 48), frostRuneM, 0, 6.05f, -63.5f); Flat(Geo.Circle(4.2f, 40), Mats.Std(0x0a1220, 0.05f, 0.4f, 0x020810), 0, 6.04f, -63.5f);
            // the throne: a seat of black ice under frozen furs, a backrest stepping up in three tiers with a frost-heart set in it,
            // a fan of black-ice spikes rising behind like a crown, beast-skull armrests holding glowing orbs, skulls heaped at its foot
            var blackIce = Mats.Std(0x2a3a58, 0.05f, 0.55f, 0x10306a); var paleIce = Mats.Std(0xc8ecff, 0.06f, 0.2f, 0x2070b0); // (lit from within, or the fan vanishes against the dark hall)
            Box(0, 6, -67, 10, 2.6f, 6, M.tower); Deco(Geo.Box(8.6f, 0.5f, 5.0f), Mats.Std(0x8a96a4, 0.95f), 0, 8.85f, -66.6f); Deco(Geo.Box(9, 0.3f, 1.2f), Mats.Std(0x8a96a4, 0.95f), 0, 8.4f, -63.9f, 0.6f, 0, 0);
            Box(0, 6, -70, 12, 10, 2.4f, M.tower); Box(0, 16, -70, 9, 6, 2.2f, M.tower); Box(0, 22, -70, 6, 4, 2.0f, M.tower);
            foreach (var (y, w) in new[] { (16f, 12.4f), (22f, 9.4f), (26f, 6.4f) }) Deco(Geo.Box(w, 0.4f, 2.6f), darkIronM, 0, y, -70);
            HexCrystal(V(0, 12.2f, -68.7f), 1.0f, 5.6f, 0, 0, Mats.Std(0x9ae8ff, 0.05f, 0.1f, 0x2a90d0)); Deco(Geo.Torus(2.4f, 0.14f, 32, 5), frostRuneM, 0, 15, -68.75f);
            var heartGlow = Mk.Glow(levelRoot, 0x80e0ff, 9, 0.55f); heartGlow.transform.localPosition = V(0, 15, -68); flames.Add(heartGlow.transform);
            for (int k = 0; k < 11; k++)
            {
                float a = (k - 5) * 0.26f, len = 9 + (5 - Mathf.Abs(k - 5)) * 1.6f;
                HexCrystal(V(Mathf.Sin(a) * 2.5f, 23 + Mathf.Cos(a) * 1.0f, -71.2f), 0.75f - Mathf.Abs(k - 5) * 0.04f, len, 0, -a, k % 2 == 0 ? blackIce : paleIce, k);
            }
            for (int k = 0; k < 8; k++) { float a = (k - 3.5f) * 0.32f; HexCrystal(V(Mathf.Sin(a) * 1.5f, 24.5f, -70.4f), 0.3f, 4 + (k % 3) * 1.5f, 0.15f, -a, paleIce, k * 2); }
            foreach (var sx in new[] { -1f, 1f })
            {
                Box(sx * 5.6f, 6, -67, 1.6f, 5, 6, M.tower); DemonRelief(V(sx * 5.6f, 11.6f, -63.9f), 0, 0.7f);
                Deco(Geo.Cyl(0.5f, 0.7f, 0.6f, 8), darkIronM, sx * 5.6f, 11.3f, -66.2f); var orb = Deco(Geo.Sphere(0.6f, 14, 10), Mats.Basic(0x9af0ff), sx * 5.6f, 12.2f, -66.2f);
                var og = Mk.Glow(levelRoot, 0x80e8ff, 4, 0.7f); og.transform.localPosition = V(sx * 5.6f, 12.2f, -66.2f); flames.Add(og.transform);
                var horn = Mk.Piv(levelRoot, sx * 3.2f, 25.6f, -70); horn.SetRot(0, 0, -sx * 0.55f);
                for (int k = 0; k < 8; k++) { Mk.Msh(horn, Geo.Cyl(1.0f * (1 - (k + 1) / 8.5f) + 0.05f, 1.0f * (1 - k / 8.5f) + 0.05f, 2.3f, 8), k < 6 ? M.tower : blackIce, 0, 1.15f, 0); horn = Mk.Piv(horn, 0, 2.3f, 0); horn.SetRot(0.06f, 0, -sx * 0.26f); }
                ChainLine(V(sx * 6.6f, 10.5f, -64), V(sx * 12, 6.05f, -58), 1.2f, 0.45f); ChainLine(V(sx * 6.0f, 9.0f, -64.2f), V(sx * 9, 6.05f, -61.5f), 0.6f, 0.4f);
                // skulls heaped either side of the seat
                for (int k = 0; k < 9; k++) { float a = k * 1.3f; var sk = Deco(Geo.Sphere(0.32f, 10, 8), M.bone, sx * (3.4f + Mathf.Cos(a) * 0.9f), 6.3f + (k % 3) * 0.25f, -62.8f + Mathf.Sin(a) * 0.7f, k, k * 2, 0); sk.transform.localScale = V(1, 1.1f, 1.15f); Ignore(sk); }
                // a demon idol at each front corner of the platform, a frost brazier by each step
                DemonStatue(sx * 12.5f, -67.5f, sx < 0 ? 0.35f : -0.35f, 6);
                FrostBrazier(sx * 8.8f, -62.3f, 6);
                var bn = Deco(Geo.Box(3.4f, 14, 0.08f), Mats.Std(0x23384c, 0.9f, 0, 0x02080e, null, true), sx * 9.5f, 18, -73.8f); bn.layer = Mk.LayerIgnore; Deco(Geo.Box(3.8f, 0.3f, 0.2f), darkIronM, sx * 9.5f, 25.1f, -73.7f);
                Deco(Geo.Box(0.9f, 0.9f, 0.06f), Mats.Std(0x8ab4d0, 0.5f, 0.3f), sx * 9.5f, 19, -73.7f, 0, 0, Mathf.PI / 4);
            }
            var crownGlow = Mk.Glow(levelRoot, 0x60c8ff, 30, 0.42f); crownGlow.transform.localPosition = V(0, 27, -72.5f); flames.Add(crownGlow.transform);
            var fanGlow = Mk.Glow(levelRoot, 0x9ae8ff, 14, 0.5f); fanGlow.transform.localPosition = V(0, 30, -71.5f);
            SolidAABB(V(-4, 26, -71.5f), V(4, 33, -69)); // the base of the crown fan            // the great pillars down both sides of the hall
            foreach (var sx in new[] { -1f, 1f }) for (int k = 0; k < 6; k++)
            {
                float x = sx * 28, z = -48 + k * 18;
                ThronePillar(x, z, 32, -sx);
            }
            // the bones of the fallen roof: iron girders still bridging the pillar capitals - one along each row of pillars, and
            // cross-girders spanning the hall between them. The chains hang off the cross-girders (each one ends in something:
            // a hook with a broken shackle, or a frost lantern still burning)
            {
                const float GY = 31.6f; // the girders sit on the capitals' abaci
                void Girder(Vector3 a, Vector3 b)
                {
                    var d = b - a; float len = d.magnitude; var m = (a + b) / 2; float ry = Mathf.Atan2(d.x, d.z);
                    Deco(Geo.Box(0.26f, 1.1f, len), darkIronM, m.x, m.y, m.z, 0, ry, 0);                                   // the web
                    foreach (var fy in new[] { -0.55f, 0.55f }) Deco(Geo.Box(0.9f, 0.16f, len), darkIronM, m.x, m.y + fy, m.z, 0, ry, 0); // the flanges
                    Ignore(Deco(Geo.Box(0.92f, 0.18f, len), snowM, m.x, m.y + 0.72f, m.z, 0, ry, 0));                        // snow along the top
                    int n = (int)(len / 2.2f);
                    for (int k = 1; k < n; k++) { var p = Vector3.Lerp(a, b, k / (float)n); Ignore(Deco(Geo.Box(0.3f, 1.0f, 0.06f), darkIronM, p.x, p.y, p.z, 0, ry, 0)); if (k % 2 == 0) Ignore(Deco(Geo.Cone(0.07f, 0.5f + (k % 3) * 0.35f, 5), iceM, p.x + 0.2f, p.y - 0.62f - 0.3f, p.z, Mathf.PI, 0, 0)); } // stiffeners, icicles
                }
                foreach (var sx in new[] { -1f, 1f }) Girder(V(sx * 28, GY, -48), V(sx * 28, GY, 42));
                for (int k = 0; k < 5; k++)
                {
                    float z = -48 + k * 18 + 9;
                    Girder(V(-28, GY + 1.1f, z), V(28, GY + 1.1f, z)); // laid across the two row girders
                    foreach (var sx in new[] { -1f, 1f })
                    {
                        float x = sx * 14, bot = 20 + R() * 6;
                        Ignore(Deco(Geo.Torus(0.3f, 0.07f, 12, 5), darkIronM, x, GY + 0.35f, z, 0, Mathf.PI / 2, 0)); // the ring round the girder's lower flange
                        HangingChain(x, z, GY + 0.2f, bot);
                        if ((k + (sx > 0 ? 1 : 0)) % 2 == 0)
                        {
                            // a frost lantern: an iron cage round a cold blue flame
                            var ln = Mk.Piv(levelRoot, x, bot - 0.5f, z);
                            Mk.Msh(ln, Geo.Cone(0.45f, 0.35f, 6), darkIronM, 0, 0.55f, 0); Mk.Msh(ln, Geo.Cyl(0.4f, 0.4f, 0.08f, 6), darkIronM, 0, -0.45f, 0);
                            for (int b = 0; b < 6; b++) { float a = b * Mathf.PI / 3; Mk.Msh(ln, Geo.Box(0.05f, 0.95f, 0.05f), darkIronM, Mathf.Cos(a) * 0.38f, 0, Mathf.Sin(a) * 0.38f); }
                            Mk.Msh(ln, Geo.Sphere(0.2f, 10, 8), Mats.Basic(0x9af0ff), 0, 0, 0);
                            foreach (var rr in ln.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                            var lg = Mk.Glow(levelRoot, 0x80e0ff, 3.2f, 0.6f); lg.transform.localPosition = V(x, bot - 0.5f, z); flames.Add(lg.transform);
                        }
                        else
                        {
                            // a meat hook with a broken shackle swinging off it
                            Ignore(Deco(Geo.Torus(0.32f, 0.06f, 12, 5, Mathf.PI * 1.3f), darkIronM, x, bot - 0.45f, z, 0, Mathf.PI / 2, Mathf.PI * 0.85f));
                            Ignore(Deco(Geo.Torus(0.16f, 0.04f, 10, 4), darkIronM, x + 0.05f, bot - 0.95f, z, 0.4f, 0, 0.3f));
                        }
                    }
                }
            }
            foreach (var z in new[] { -40f, -14f, 12f, 38f }) { FrostBrazier(-13, z); FrostBrazier(13, z); }
            FrostShrine(0, 24);
            // the side galleries, raised, with frozen demon idols watching the floor
            ironM = darkIronM; obsM = Mats.Std(0x18202c, 0.18f, 0.6f, 0x040a14); // (DemonStatue's materials are normally made by the hell levels)
            foreach (var sx in new[] { -1f, 1f })
            {
                Box(sx * 58, 0, -4, 24, 5, 84, M.block); TrimRect(sx * 58, -4, 24, 84, 5);
                Stairs(sx * 40.4f, 30, 6, V(sx, 0, 0), 4, 0, 5, 1.4f, M.block);
                Stairs(sx * 40.4f, -36, 6, V(sx, 0, 0), 4, 0, 5, 1.4f, M.block);
                foreach (var z in new[] { -30f, 0f, 26f }) DemonStatue(sx * 60, z, sx < 0 ? Mathf.PI / 2 : -Mathf.PI / 2, 5);
                IceFormation(sx * 64, -56, 16, 2); IceFormation(sx * 64, 50, 14, 1.8f);
            }
            // torn banners hanging over the hall
            foreach (var z in new[] { -40f, -10f, 20f, 48f }) foreach (var sx in new[] { -1f, 1f }) { var b = Deco(Geo.Box(4, 12, 0.08f), M.banner, sx * 72.8f, 22, z, 0, Mathf.PI / 2, 0); b.layer = Mk.LayerIgnore; Deco(Geo.Box(4.4f, 0.3f, 0.2f), darkIronM, sx * 72.6f, 28.2f, z, 0, Mathf.PI / 2, 0); }
            PadAt(-40, 52, 24); PadAt(40, 52, 24); PadAt(-40, -54, 26); PadAt(40, -54, 26);
        }
        // one of the throne room's great pillars: a stepped base, a clustered shaft banded in iron and frost runes, a demon skull
        // staring at the aisle, a flared capital with horns curling off it, icicles hanging from the capital
        void ThronePillar(float x, float z, float h, float face)
        {
            Box(x, 0, z, 6.4f, 1.0f, 6.4f, stoneDarkM); Box(x, 1.0f, z, 5.2f, 1.2f, 5.2f, M.block); Deco(Geo.Box(5.4f, 0.15f, 5.4f), M.trim, x, 2.25f, z);
            float s0 = 2.2f, sh = h - 7;
            Box(x, s0, z, 3.0f, sh, 3.0f, M.tower);
            foreach (var a in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) { Deco(Geo.Cyl(0.72f, 0.8f, sh, 12), M.tower, x + a * 1.55f, s0 + sh / 2, z + b * 1.55f); Deco(Geo.Cyl(0.95f, 0.95f, 0.5f, 12), stoneDarkM, x + a * 1.55f, s0 + 0.25f, z + b * 1.55f); }
            foreach (var y in new[] { s0 + sh * 0.28f, s0 + sh * 0.7f }) { Deco(Geo.Box(4.9f, 0.7f, 4.9f), darkIronM, x, y, z); for (int k = 0; k < 4; k++) { float a = k * Mathf.PI / 2; Deco(Geo.Cone(0.18f, 0.7f, 4), darkIronM, x + Mathf.Sin(a) * 2.6f, y, z + Mathf.Cos(a) * 2.6f, Mathf.Cos(a) * Mathf.PI / 2, 0, -Mathf.Sin(a) * Mathf.PI / 2); } }
            // the aisle face: a carved demon skull, frost runes running up to it and above it
            DemonRelief(V(x + face * 1.75f, s0 + sh * 0.5f, z), face > 0 ? Mathf.PI / 2 : -Mathf.PI / 2, 1.15f);
            foreach (var (y0, len) in new[] { (s0 + 1, sh * 0.25f), (s0 + sh * 0.74f, sh * 0.22f) }) Ignore(Deco(Geo.Box(0.06f, len, 0.35f), frostRuneM, x + face * 1.53f, y0 + len / 2, z));
            // the capital: an inverted pyramid flaring out, an abacus slab, horns curling up off two corners
            float c0 = s0 + sh;
            Deco(Geo.Cyl(3.7f, 2.2f, 2.6f, 4), M.tower, x, c0 + 1.3f, z, 0, Mathf.PI / 4, 0);
            Box(x, c0 + 2.6f, z, 5.6f, 1.0f, 5.6f, M.block); Deco(Geo.Box(5.8f, 0.25f, 5.8f), snowM, x, c0 + 3.7f, z);
            foreach (var b in new[] { -1f, 1f })
            {
                var horn = Mk.Piv(levelRoot, x + face * 2.4f, c0 + 3.4f, z + b * 2.4f); horn.SetRot(b * 0.4f, 0, face * -0.5f);
                for (int k = 0; k < 6; k++) { Mk.Msh(horn, Geo.Cyl(0.5f * (1 - k / 6f) + 0.04f, 0.5f * (1 - (k + 1) / 6f) + 0.04f, 1.3f, 8), M.tower, 0, 0.65f, 0); horn = Mk.Piv(horn, 0, 1.3f, 0); horn.SetRot(b * 0.12f, 0, face * 0.32f); }
            }
            for (int k = 0; k < 10; k++) { float a = k * Mathf.PI / 5; Ignore(Deco(Geo.Cone(0.16f, 0.8f + (k % 3) * 0.5f, 5), iceM, x + Mathf.Sin(a) * 2.7f, c0 + 2.3f - 0.5f, z + Mathf.Cos(a) * 2.7f, Mathf.PI, 0, 0)); }
            var g = Mk.Glow(levelRoot, 0x60c8ff, 4, 0.35f); g.transform.localPosition = V(x + face * 2.5f, c0 + 1.2f, z); flames.Add(g.transform);
            // ice heaped round the foot
            for (int k = 0; k < 5; k++) { float a = k * 1.3f + x; Ignore(Deco(Geo.Cone(0.45f, 1.2f + k % 3 * 0.6f, 5), iceM, x + Mathf.Sin(a) * 3.4f, 0.5f, z + Mathf.Cos(a) * 3.4f, Mathf.Cos(a) * 0.3f, 0, -Mathf.Sin(a) * 0.3f)); }
        }
    }
}
