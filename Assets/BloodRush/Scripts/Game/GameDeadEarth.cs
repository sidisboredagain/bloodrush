using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    // ACT III, DEAD EARTH (levels 11-15), rebuilt:
    //   11 ASHFALL CITY         a burning downtown: apartment blocks, glass offices, brick shop rows, a parking garage, gutted
    //                           shells; real roads; wrecked cars, vans, buses, trucks. trap: DOWNED POWER LINES - the cable
    //                           crackles and sparks run along it, then lightning arcs the whole length of it
    //   12 THE DROWNED SUBWAY   a flooded station: two track trenches either side of an island platform, side platforms,
    //                           the concourse, stopped escalators, a collapsed roof. trap: TRAINS - three-car trains, every 6s
    //   13 OVERPASS GRAVEYARD   a highway interchange: two elevated decks on T-piers with ramps, gantry signs, a broken span;
    //                           a wreck-choked road below. trap: RUNAWAY TRUCKS - a burning truck races down the deck, then
    //                           flies off the broken end and crashes where the circle glows
    //   14 THE CATHEDRAL OF RUST  a foundry built like a cathedral: girder arches down the nave, an organ of pipes, the
    //                           great furnace for an altar, a conveyor loop round it. trap: STAMPING PRESSES over the belt
    //   15 THE BROKEN CROWN     the Hollow King's square (GameHollowKing)
    public partial class Game
    {
        Material concM, concDarkM, asphaltM, sidewalkM, laneWhiteM, laneYellowM, rustM, steelM, brickM, tarM, glassM, burntM, rebarM, tileM;
        void DeadMats()
        {
            // weathered: concrete, asphalt, brick and tar carry painted grime (stains, specks, cracks); steel and rust carry rust
            // blooms, drip streaks and bright scratches (GameRuinMobs). The level's boxes tile them every 4m.
            var gr = GrimeTex(); var ru = RustTex();
            concM = Mats.Std(0x8a847a, 0.92f, 0, 0, gr); concDarkM = Mats.Std(0x524e48, 0.95f, 0, 0, gr); asphaltM = Mats.Std(0x2e2c2a, 0.95f, 0, 0, gr); sidewalkM = Mats.Std(0x68645e, 0.95f, 0, 0, gr);
            laneWhiteM = Mats.Std(0xb8b4a8, 0.9f, 0, 0, gr); laneYellowM = Mats.Std(0xc8a040, 0.85f, 0, 0, gr); rustM = Mats.Std(0x7a4a2a, 0.7f, 0.5f, 0, ru); steelM = Mats.Std(0x585a5e, 0.45f, 0.75f, 0, ru);
            brickM = Mats.Std(0x7a4232, 0.92f, 0, 0, gr); tarM = Mats.Std(0x262220, 0.98f, 0, 0, gr); glassM = Mats.Std(0x141c22, 0.06f, 0.6f, 0x05090c); burntM = Mats.Std(0x1e1a18, 0.95f, 0.2f, 0, gr);
            rebarM = Mats.Std(0x6a4430, 0.6f, 0.6f, 0, ru); tileM = Mats.Std(0x8a8678, 0.6f, 0, 0, gr);
        }
        GameObject Ig(GameObject g) { g.layer = Mk.LayerIgnore; return g; }

        // ---------- vehicles: a sedan, a van, a pickup, a bus, a semi truck ----------
        // The bodies are lofted from real side profiles (a convex (z, y) outline extruded across the car's width): a raked nose
        // and hood, a sloped windscreen, a roofline, a rear window and trunk - with the glasshouse as its own glass loft, body-
        // coloured pillars and roof skin over it, wheel arches, bumpers, grille, lights, door seams, handles and mirrors.
        static readonly Dictionary<string, Mesh> loftCache = new Dictionary<string, Mesh>();
        static Mesh Loft(Vector2[] prof, float w)
        {
            string key = w.ToString("F2"); foreach (var q in prof) key += $"|{q.x:F2},{q.y:F2}";
            if (loftCache.TryGetValue(key, out var mm) && mm) return mm;
            var b = new Geo.MB(); float x0 = -w / 2, x1 = w / 2; var cen = Vector2.zero; foreach (var q in prof) cen += q; cen /= prof.Length;
            void Tri(Vector3 a, Vector3 c, Vector3 d, Vector3 n) { int s = b.v.Count; b.v.Add(a); b.v.Add(c); b.v.Add(d); for (int i = 0; i < 3; i++) { b.n.Add(n); } b.uv.Add(new Vector2(a.z, a.y) * 0.5f); b.uv.Add(new Vector2(c.z, c.y) * 0.5f); b.uv.Add(new Vector2(d.z, d.y) * 0.5f); b.Tri(s, s + 1, s + 2); b.Tri(s, s + 2, s + 1); }
            // the two side faces (fans) and the band of faces round the outline
            for (int i = 1; i + 1 < prof.Length; i++)
                foreach (var x in new[] { x0, x1 }) Tri(new Vector3(x, prof[0].y, prof[0].x), new Vector3(x, prof[i].y, prof[i].x), new Vector3(x, prof[i + 1].y, prof[i + 1].x), new Vector3(Mathf.Sign(x), 0, 0));
            for (int i = 0; i < prof.Length; i++)
            {
                var a = prof[i]; var c = prof[(i + 1) % prof.Length]; var e = c - a; var n2 = new Vector2(e.y, -e.x).normalized; if (Vector2.Dot(n2, (a + c) / 2 - cen) < 0) n2 = -n2;
                var n = new Vector3(0, n2.y, n2.x);
                Vector3 A0 = new Vector3(x0, a.y, a.x), A1 = new Vector3(x1, a.y, a.x), C0 = new Vector3(x0, c.y, c.x), C1 = new Vector3(x1, c.y, c.x);
                Tri(A0, A1, C1, n); Tri(A0, C1, C0, n);
            }
            var m = b.Build(); loftCache[key] = m; return m;
        }
        static Vector2 P2(float z, float y) => new Vector2(z, y);
        // a thin strip of material laid along the edge a-b of a profile, at side x (a pillar, a frame)
        void Strip(N p, Material m, Vector2 a, Vector2 b, float x, float t = 0.07f)
        {
            var d = b - a; var g = Mk.Msh(p, Geo.Box(t, d.magnitude, t), m, x, (a.y + b.y) / 2, (a.x + b.x) / 2);
            g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, new Vector3(0, d.y, d.x).normalized);
        }
        void Vehicle(float x, float z, float ry, int kind, bool burnt, float y0 = 0, Transform parent = null)
        {
            int[] paint = { 0x7a2a20, 0x2a4a6a, 0x5a5a50, 0x8a7a3a, 0x3a5a3a, 0xa8a8a0, 0x2a2a2e, 0x6a3a5a };
            int seed = Mathf.Abs((int)(x * 7 + z * 13)) % 997;
            var body = burnt ? Mats.Std(0x2a2018, 0.9f, 0.35f, 0, GrimeTex()) : Mats.Std(paint[seed % paint.Length], 0.42f, 0.55f, 0, RustTex()); // rust breaking through the paint
            var dark = Mats.Std(0x141414, 0.9f); var trim = Mats.Std(0x1e1e1e, 0.6f, 0.3f); var chrome = Mats.Std(0x9a9a9a, 0.25f, 0.8f); var gl = burnt ? Mats.Std(0x0a0a0a, 0.9f) : Mats.Std(0x18222a, 0.05f, 0.6f, 0x04080a);
            var head = Mats.Basic(burnt ? 0x2a2420 : 0xd8d0b0); var tail = Mats.Basic(burnt ? 0x2a1210 : 0x8a1810); var rustP = Mats.Std(0x5a3418, 0.85f, 0.3f, 0, RustTex()); var tyre = Mats.Std(0x161616, 0.85f, 0, 0, GrimeTex());
            float W, L, H; switch (kind) { case 1: W = 2.05f; L = 5.2f; H = 2.3f; break; case 2: W = 2.0f; L = 5.4f; H = 1.85f; break; case 3: W = 2.6f; L = 11f; H = 3.3f; break; case 4: W = 2.5f; L = 14f; H = 3.6f; break; default: W = 1.85f; L = 4.5f; H = 1.44f; break; }
            var p = Mk.Piv(parent ?? levelRoot, x, y0, z); p.SetRot(0, ry, 0);
            float wr = kind >= 3 ? 0.55f : kind == 0 ? 0.34f : 0.38f;
            // wheels: tyre, rim, hub, in dark arches cut into the body sides (a bus or a semi has more axles)
            var axles = kind == 3 ? new[] { -0.33f, 0.31f } : kind == 4 ? new[] { -0.42f, -0.33f, 0.36f } : kind == 1 ? new[] { -0.31f, 0.33f } : new[] { -0.31f, 0.32f };
            foreach (var az in axles)
            {
                Mk.Msh(p, Geo.Cyl(wr * 1.18f, wr * 1.18f, W + 0.02f, 16), dark, 0, wr, az * L, 0, 0, Mathf.PI / 2); // the arch
                foreach (var sx in new[] { -1f, 1f })
                {
                    float wx = sx * (W / 2 - 0.13f);
                    Mk.Msh(p, Geo.Cyl(wr, wr, 0.26f, 16), tyre, wx, wr, az * L, 0, 0, Mathf.PI / 2);
                    Mk.Msh(p, Geo.Cyl(wr * 0.6f, wr * 0.6f, 0.27f, 10), burnt ? rustP : chrome, wx + sx * 0.005f, wr, az * L, 0, 0, Mathf.PI / 2);
                    Mk.Msh(p, Geo.Cyl(wr * 0.2f, wr * 0.2f, 0.29f, 8), dark, wx + sx * 0.01f, wr, az * L, 0, 0, Mathf.PI / 2);
                    if (burnt && ((seed + (int)(az * 10)) & 1) == 0) Mk.Msh(p, Geo.Box(0.24f, 0.06f, wr * 1.6f), dark, wx, 0.03f, az * L); // a tyre burnt flat
                }
            }
            Mk.Msh(p, Geo.Box(W * 0.8f, 0.16f, L * 0.86f), dark, 0, wr * 0.8f, 0); // the floor pan
            float hw = W / 2;
            switch (kind)
            {
                case 0: // a sedan
                {
                    var lower = new[] { P2(-2.25f, 0.3f), P2(2.25f, 0.3f), P2(2.27f, 0.58f), P2(2.12f, 0.78f), P2(0.95f, 0.9f), P2(-1.35f, 0.93f), P2(-2.18f, 0.86f), P2(-2.27f, 0.6f) };
                    Mk.Msh(p, Loft(lower, W), body);
                    Vector2 a0 = P2(0.95f, 0.88f), a1 = P2(0.12f, 1.38f), c0 = P2(-0.88f, 1.4f), c1 = P2(-1.5f, 0.92f);
                    Mk.Msh(p, Loft(new[] { a0, a1, c0, c1 }, W * 0.86f), gl);
                    Mk.Msh(p, Loft(new[] { P2(0.18f, 1.36f), P2(0.12f, 1.43f), P2(-0.9f, 1.45f), P2(-0.86f, 1.38f) }, W * 0.88f), body); // the roof skin
                    foreach (var sx in new[] { -1f, 1f })
                    {
                        float px = sx * W * 0.43f;
                        Strip(p, body, a0, a1, px); Strip(p, body, c0, c1, px); Strip(p, body, P2(-0.33f, 0.92f), P2(-0.33f, 1.4f), px, 0.09f); Strip(p, trim, P2(0.95f, 0.9f), P2(-1.45f, 0.93f), sx * W * 0.435f, 0.05f); // A, C, B pillars; window sill
                        foreach (var sz in new[] { 0.85f, -0.33f, -1.3f }) Mk.Msh(p, Geo.Box(0.012f, 0.5f, 0.02f), trim, sx * (hw + 0.004f), 0.62f, sz); // door seams
                        foreach (var sz in new[] { 0.2f, -0.95f }) Mk.Msh(p, Geo.Box(0.03f, 0.04f, 0.16f), chrome, sx * (hw + 0.015f), 0.82f, sz); // handles
                        Mk.Msh(p, Geo.Box(0.06f, 0.1f, 0.18f), trim, sx * (hw + 0.08f), 0.98f, 0.82f); // mirrors
                        Mk.Msh(p, Geo.Box(0.34f, 0.12f, 0.06f), head, sx * 0.6f, 0.66f, 2.21f, -0.3f, 0, 0); Mk.Msh(p, Geo.Box(0.38f, 0.13f, 0.05f), tail, sx * 0.6f, 0.73f, -2.27f);
                    }
                    Mk.Msh(p, Geo.Box(W * 0.46f, 0.16f, 0.05f), dark, 0, 0.6f, 2.26f); // grille
                    foreach (var ez in new[] { -1f, 1f }) Mk.Msh(p, Geo.Box(W + 0.04f, 0.17f, 0.2f), burnt ? rustP : trim, 0, 0.42f, ez * 2.27f);
                    Mk.Msh(p, Geo.Box(0.5f, 0.12f, 0.03f), Mats.Std(0xd8d0b0, 0.6f), 0, 0.55f, -2.385f); // number plate
                    break;
                }
                case 1: // a van
                {
                    Mk.Msh(p, Loft(new[] { P2(-2.6f, 0.32f), P2(2.6f, 0.32f), P2(2.62f, 0.7f), P2(2.45f, 1.05f), P2(1.75f, 1.15f), P2(-2.6f, 1.15f) }, W), body);
                    Vector2 w0 = P2(1.75f, 1.13f), w1 = P2(1.02f, 2.08f);
                    Mk.Msh(p, Loft(new[] { w0, w1, P2(-2.55f, 2.16f), P2(-2.62f, 1.13f) }, W * 0.98f), body);
                    // the windscreen and the cab's side windows set into the body, the sliding door, the rear doors, a roof rack
                    var ws = Mk.Msh(p, Geo.Box(W * 0.84f, (w1 - w0).magnitude * 0.86f, 0.04f), gl, 0, (w0.y + w1.y) / 2, (w0.x + w1.x) / 2 + 0.03f); ws.transform.localRotation = Quaternion.Euler(-Mathf.Atan2(w1.x - w0.x, w1.y - w0.y) * Mathf.Rad2Deg, 0, 0);
                    foreach (var sx in new[] { -1f, 1f })
                    {
                        Mk.Msh(p, Geo.Box(0.03f, 0.55f, 0.85f), gl, sx * (W * 0.49f + 0.01f), 1.62f, 0.95f); Mk.Msh(p, Geo.Box(0.03f, 0.5f, 1.0f), gl, sx * (W * 0.49f + 0.01f), 1.62f, -0.2f);
                        foreach (var sz in new[] { 0.45f, -0.75f, -2.0f }) Mk.Msh(p, Geo.Box(0.012f, 1.5f, 0.02f), trim, sx * (hw + 0.004f), 1.05f, sz);
                        Mk.Msh(p, Geo.Box(0.05f, 0.05f, 4.2f), trim, sx * (hw + 0.02f), 1.95f, -0.6f); // the sliding door rail
                        Mk.Msh(p, Geo.Box(0.06f, 0.12f, 0.2f), trim, sx * (hw + 0.08f), 1.4f, 1.55f);
                        Mk.Msh(p, Geo.Box(0.3f, 0.14f, 0.06f), head, sx * 0.68f, 0.82f, 2.52f, -0.4f, 0, 0); Mk.Msh(p, Geo.Box(0.16f, 0.5f, 0.05f), tail, sx * 0.85f, 1.2f, -2.62f);
                        Mk.Msh(p, Geo.Box(0.06f, 0.06f, 3.6f), steelM, sx * W * 0.38f, 2.24f, -0.6f);
                    }
                    Mk.Msh(p, Geo.Box(0.012f, 1.7f, 0.02f), trim, 0, 1.2f, -2.625f); // the split of the rear doors
                    Mk.Msh(p, Geo.Box(W * 0.5f, 0.2f, 0.05f), dark, 0, 0.72f, 2.6f);
                    foreach (var ez in new[] { -1f, 1f }) Mk.Msh(p, Geo.Box(W + 0.04f, 0.2f, 0.2f), burnt ? rustP : trim, 0, 0.44f, ez * 2.62f);
                    for (int k = 0; k < 4; k++) Mk.Msh(p, Geo.Box(W * 0.8f, 0.05f, 0.05f), steelM, 0, 2.27f, -2.0f + k * 0.9f);
                    break;
                }
                case 2: // a pickup
                {
                    Mk.Msh(p, Loft(new[] { P2(-2.7f, 0.36f), P2(2.7f, 0.36f), P2(2.72f, 0.75f), P2(2.55f, 1.0f), P2(1.25f, 1.1f), P2(-2.7f, 1.12f) }, W), body);
                    Vector2 a0 = P2(1.25f, 1.08f), a1 = P2(0.6f, 1.72f), c0 = P2(-0.42f, 1.74f), c1 = P2(-0.52f, 1.1f);
                    Mk.Msh(p, Loft(new[] { a0, a1, c0, c1 }, W * 0.88f), gl);
                    Mk.Msh(p, Loft(new[] { P2(0.64f, 1.7f), P2(0.6f, 1.77f), P2(-0.46f, 1.78f), P2(-0.45f, 1.71f) }, W * 0.9f), body);
                    // the bed: liner, walls with their caps, the tailgate, junk in it
                    Mk.Msh(p, Geo.Box(W * 0.9f, 0.02f, 2.1f), Mats.Std(0x1e1c1a, 0.9f, 0, 0, GrimeTex()), 0, 1.13f, -1.62f);
                    foreach (var sx in new[] { -1f, 1f })
                    {
                        float px = sx * W * 0.44f;
                        Strip(p, body, a0, a1, px); Strip(p, body, c0, c1, px, 0.1f);
                        Mk.Msh(p, Geo.Box(0.08f, 0.42f, 2.2f), body, sx * (hw - 0.04f), 1.32f, -1.6f); Mk.Msh(p, Geo.Box(0.12f, 0.04f, 2.2f), trim, sx * (hw - 0.04f), 1.54f, -1.6f);
                        Mk.Msh(p, Geo.Box(0.012f, 0.55f, 0.02f), trim, sx * (hw + 0.004f), 0.75f, -0.5f); Mk.Msh(p, Geo.Box(0.012f, 0.55f, 0.02f), trim, sx * (hw + 0.004f), 0.75f, 1.2f);
                        Mk.Msh(p, Geo.Box(0.03f, 0.04f, 0.16f), chrome, sx * (hw + 0.015f), 0.98f, 0.35f);
                        Mk.Msh(p, Geo.Box(0.06f, 0.12f, 0.2f), trim, sx * (hw + 0.08f), 1.25f, 1.15f);
                        Mk.Msh(p, Geo.Box(0.36f, 0.14f, 0.06f), head, sx * 0.62f, 0.86f, 2.66f, -0.3f, 0, 0); Mk.Msh(p, Geo.Box(0.12f, 0.36f, 0.05f), tail, sx * (hw - 0.08f), 1.3f, -2.71f);
                    }
                    Mk.Msh(p, Geo.Box(W * 0.88f, 0.42f, 0.08f), body, 0, 1.32f, -2.68f); Mk.Msh(p, Geo.Box(W * 0.84f, 0.42f, 0.08f), body, 0, 1.32f, -0.55f);
                    Mk.Msh(p, Geo.Box(W * 0.55f, 0.3f, 0.05f), dark, 0, 0.74f, 2.7f); for (int k = 0; k < 3; k++) Mk.Msh(p, Geo.Box(W * 0.55f, 0.03f, 0.055f), chrome, 0, 0.64f + k * 0.1f, 2.71f);
                    foreach (var ez in new[] { -1f, 1f }) Mk.Msh(p, Geo.Box(W + 0.06f, 0.2f, 0.22f), burnt ? rustP : chrome, 0, 0.48f, ez * 2.72f);
                    if (!burnt) { Mk.Msh(p, Geo.Box(0.7f, 0.45f, 0.6f), Mats.Std(0x4a3a2a, 0.9f, 0, 0, GrimeTex()), -0.3f, 1.36f, -1.3f, 0, 0.3f, 0); Mk.Msh(p, Geo.Cyl(0.25f, 0.25f, 0.8f, 10), rustP, 0.35f, 1.4f, -2.0f, 0, 0, Mathf.PI / 2); }
                    break;
                }
                case 3: // a city bus: a long body with a rounded roof edge, a window band with pillars, a big windscreen, doors
                {
                    Mk.Msh(p, Loft(new[] { P2(-5.5f, 0.5f), P2(5.5f, 0.5f), P2(5.52f, 2.95f), P2(5.3f, 3.3f), P2(-5.35f, 3.3f), P2(-5.52f, 2.95f) }, W), body);
                    foreach (var sx in new[] { -1f, 1f }) { Mk.Msh(p, Geo.Box(0.03f, 1.0f, L * 0.84f), gl, sx * (hw + 0.01f), 2.3f, -0.4f); for (int k = 0; k < 9; k++) Mk.Msh(p, Geo.Box(0.05f, 1.02f, 0.1f), body, sx * (hw + 0.02f), 2.3f, -5.0f + k * 1.15f); Mk.Msh(p, Geo.Box(0.04f, 0.12f, L), Mats.Std(0xd8a020, 0.6f), sx * (hw + 0.015f), 1.1f, 0); }
                    Mk.Msh(p, Geo.Box(W * 0.9f, 1.45f, 0.04f), gl, 0, 2.15f, L / 2 + 0.02f); Mk.Msh(p, Geo.Box(W * 0.7f, 0.3f, 0.05f), Mats.Basic(burnt ? 0x1a1410 : 0xd88a20), 0, 3.05f, L / 2 + 0.03f);
                    Mk.Msh(p, Geo.Box(0.05f, 2.0f, 1.1f), dark, hw + 0.02f, 1.5f, 4.2f); Mk.Msh(p, Geo.Box(0.05f, 2.0f, 1.1f), dark, hw + 0.02f, 1.5f, -0.6f);
                    foreach (var sx in new[] { -1f, 1f }) { Mk.Msh(p, Geo.Box(0.34f, 0.18f, 0.05f), head, sx * 0.9f, 0.9f, L / 2 + 0.02f); Mk.Msh(p, Geo.Box(0.2f, 0.5f, 0.05f), tail, sx * 1.0f, 1.2f, -L / 2 - 0.02f); }
                    foreach (var ez in new[] { -1f, 1f }) Mk.Msh(p, Geo.Box(W + 0.04f, 0.3f, 0.2f), burnt ? rustP : trim, 0, 0.6f, ez * (L / 2 + 0.05f));
                    Mk.Msh(p, Geo.Box(W * 0.6f, 0.35f, L * 0.2f), steelM, 0, H + 0.17f, -0.25f * L);
                    break;
                }
                case 4: // a semi: a cab-over tractor with its sleeper and stacks, a corrugated container trailer
                {
                    float cz = 0.4f * L; // the cab's middle
                    Mk.Msh(p, Loft(new[] { P2(cz - 1.4f, 0.7f), P2(cz + 1.4f, 0.7f), P2(cz + 1.45f, 2.2f), P2(cz + 1.3f, 3.25f), P2(cz - 1.4f, 3.3f) }, W), body);
                    Mk.Msh(p, Geo.Box(W * 0.88f, 1.0f, 0.04f), gl, 0, 2.65f, cz + 1.39f, -0.13f, 0, 0);
                    foreach (var sx in new[] { -1f, 1f }) { Mk.Msh(p, Geo.Box(0.03f, 0.7f, 0.9f), gl, sx * (hw + 0.01f), 2.6f, cz + 0.8f); Mk.Msh(p, Geo.Cyl(0.11f, 0.11f, 2.2f, 8), chrome, sx * W * 0.45f, 3.4f, cz - 1.5f); Mk.Msh(p, Geo.Box(0.4f, 0.18f, 0.06f), head, sx * 0.85f, 1.1f, cz + 1.45f); Mk.Msh(p, Geo.Cyl(0.32f, 0.32f, 1.0f, 12), chrome, sx * (hw - 0.3f), 0.95f, cz - 0.6f, Mathf.PI / 2, 0, 0); }
                    Mk.Msh(p, Geo.Box(W * 0.7f, 0.8f, 0.06f), dark, 0, 1.4f, cz + 1.44f);
                    Mk.Msh(p, Geo.Box(W + 0.06f, 0.3f, 0.25f), burnt ? rustP : chrome, 0, 0.7f, cz + 1.5f);
                    var cont = burnt ? burntM : Mats.Std(new[] { 0x6a3a28, 0x2a4a5a, 0x5a5a2a }[seed % 3], 0.8f, 0.3f, 0, RustTex());
                    Mk.Msh(p, Geo.Box(W + 0.1f, H - 0.5f, L * 0.68f), cont, 0, 1.25f + (H - 0.5f) / 2 - 0.2f, -0.15f * L);
                    for (int k = 0; k < 12; k++) foreach (var sx in new[] { -1f, 1f }) Mk.Msh(p, Geo.Box(0.06f, H - 0.6f, 0.1f), cont, sx * (hw + 0.07f), 1.25f + (H - 0.5f) / 2 - 0.2f, (-0.48f + k * 0.06f) * L); // corrugations
                    Mk.Msh(p, Geo.Box(W * 0.9f, 0.3f, L * 0.7f), dark, 0, 1.0f, -0.15f * L); // the trailer chassis
                    break;
                }
            }
            if (burnt)
            {
                if (seed % 3 == 0) { var g = Mk.Glow(p.t, 0xff6010, 2.5f, 0.45f); g.transform.localPosition = V(0, H * 0.6f, 0); flames.Add(g.transform); var cp = p.t.position; anims.Add((t, dt) => { if (rand < 0.15f) SpawnParticles(cp + V((rand - 0.5f) * W, H, (rand - 0.5f) * L * 0.5f), 1, 1.5f, rand < 0.7f ? ashMat : emberMat, 0.18f, 1.6f, -2); }); }
            }
            foreach (var r in p.t.GetComponentsInChildren<Renderer>()) r.gameObject.layer = Mk.LayerIgnore;
            if (parent != null) return;
            float c = Mathf.Abs(Mathf.Cos(ry)), s = Mathf.Abs(Mathf.Sin(ry)); float hx = c * W / 2 + s * L / 2, hz = s * W / 2 + c * L / 2;
            SolidAABB(V(x - hx * 0.95f, y0, z - hz * 0.95f), V(x + hx * 0.95f, y0 + H * 0.9f, z + hz * 0.95f));
        }

        // ---------- buildings: five kinds, each with its own walls and a proper roof ----------
        // 0 apartment block   1 glass office tower   2 brick shop row   3 parking garage (open floors you can walk)   4 gutted shell
        void CityBuilding(float x, float z, float w, float d, float h, int type, int seed)
        {
            var rng = new System.Random(seed); float R() => (float)rng.NextDouble();
            int[] tints = { 0xb0a090, 0x9a8c80, 0xa89a84, 0x8a8480 };
            if (type == 3)
            {
                // a parking garage: concrete floor slabs on columns, ramps of steps between, cars left inside
                int floors = Mathf.Max(2, (int)(h / 3.4f));
                for (int f = 0; f <= floors; f++) { float y = f * 3.4f; if (f > 0) Box(x, y - 0.4f, z, w, 0.4f, d, concM); Deco(Geo.Box(w + 0.1f, 0.9f, 0.25f), concDarkM, x, y + 0.45f + (f > 0 ? 0 : 0.0f), z - d / 2); Deco(Geo.Box(w + 0.1f, 0.9f, 0.25f), concDarkM, x, y + 0.45f, z + d / 2); }
                for (float px = -w / 2 + 1; px <= w / 2 - 1 + 0.01f; px += (w - 2) / 3) foreach (var pz in new[] { -d / 2 + 1, d / 2 - 1 }) Box(x + px, 0, z + pz, 0.8f, floors * 3.4f, 0.8f, concM);
                for (int f = 0; f < floors; f++) { bool side = f % 2 == 0; Stairs(x + (side ? -w / 2 + 2.2f : w / 2 - 2.2f), z - d / 2 + 2.5f, 3.4f, V(0, 0, 1), 6, f * 3.4f, (f + 1) * 3.4f - 0.4f, 0.9f, concDarkM); }
                for (int f = 0; f < floors; f++) for (int k = 0; k < 2; k++) if (R() < 0.6f) Vehicle(x + (k - 0.5f) * w * 0.4f, z + (R() - 0.5f) * d * 0.3f, R() < 0.5f ? 0 : Mathf.PI, (int)(R() * 3), R() < 0.4f, f * 3.4f);
                Box(x, floors * 3.4f, z - d / 2 + 0.15f, w, 1.0f, 0.3f, concDarkM); Box(x, floors * 3.4f, z + d / 2 - 0.15f, w, 1.0f, 0.3f, concDarkM);
                return;
            }
            if (type == 4)
            {
                // a gutted shell: the ground floors standing, the upper walls broken off in jagged runs, floors hanging out, rebar
                float hb = h * 0.45f;
                Box(x, 0, z, w, hb, d, Mats.Basic(tints[seed % 4], FacadeTex()));
                foreach (var (ex, ez, alongX) in new[] { (0f, -d / 2 + 0.4f, true), (0f, d / 2 - 0.4f, true), (-w / 2 + 0.4f, 0f, false), (w / 2 - 0.4f, 0f, false) })
                {
                    float len = alongX ? w : d; int n = (int)(len / 2.5f);
                    for (int k = 0; k < n; k++) { if (R() < 0.25f) continue; float u = ((k + 0.5f) / n - 0.5f) * len, hh = (h - hb) * (0.2f + R() * 0.8f); Box(x + ex + (alongX ? u : 0), hb, z + ez + (alongX ? 0 : u), alongX ? len / n : 0.8f, hh, alongX ? 0.8f : len / n, concM); Deco(Geo.Box(alongX ? len / n : 0.85f, 0.25f, alongX ? 0.85f : len / n), burntM, x + ex + (alongX ? u : 0), hb + hh - 0.1f, z + ez + (alongX ? 0 : u)); }
                }
                for (int k = 0; k < 3; k++) { var sl = Deco(Geo.Box(w * 0.5f, 0.35f, d * 0.4f), concM, x + (R() - 0.5f) * w * 0.4f, hb + 3 + k * 3, z + (R() - 0.5f) * d * 0.3f, (R() - 0.5f) * 0.5f, R() * 3, (R() - 0.5f) * 0.4f); Ig(sl); }
                for (int k = 0; k < 14; k++) Ig(Deco(Geo.Cyl(0.04f, 0.04f, 1.5f + R() * 2, 4), rebarM, x + (R() - 0.5f) * w, hb + R() * (h - hb), z + (R() < 0.5f ? -d / 2 : d / 2), (R() - 0.5f) * 1.4f, 0, (R() - 0.5f) * 1.4f));
                var fire = Mk.Glow(levelRoot, 0xff5010, Mathf.Min(w, d) * 0.8f, 0.35f); fire.transform.localPosition = V(x, hb + 1.5f, z); flames.Add(fire.transform);
                anims.Add((t, dt) => { if (rand < 0.3f) SpawnParticles(V(x + (rand - 0.5f) * w * 0.6f, hb + 1, z + (rand - 0.5f) * d * 0.6f), 1, 2, rand < 0.6f ? ashMat : emberMat, 0.25f, 2.5f, -3); });
                return;
            }
            Material wallMat = type == 0 ? Mats.Basic(tints[seed % 4], FacadeTex()) : type == 1 ? glassM : brickM;
            Box(x, 0, z, w, h, d, wallMat);
            if (type == 1)
            {
                // glass tower: mullions, floor bands, a lobby of dark steel, a few windows burning
                for (float u = -w / 2; u <= w / 2 + 0.01f; u += 2.4f) foreach (var sz in new[] { -1f, 1f }) Deco(Geo.Box(0.14f, h, 0.14f), steelM, x + u, h / 2, z + sz * (d / 2 + 0.05f));
                for (float u = -d / 2; u <= d / 2 + 0.01f; u += 2.4f) foreach (var sx in new[] { -1f, 1f }) Deco(Geo.Box(0.14f, h, 0.14f), steelM, x + sx * (w / 2 + 0.05f), h / 2, z + u);
                for (float y = 3.6f; y < h; y += 3.6f) { Deco(Geo.Box(w + 0.25f, 0.3f, d + 0.25f), concDarkM, x, y, z); }
                for (int k = 0; k < 6; k++) { float y = 3.6f * (1 + (int)(R() * (h / 3.6f - 1))) + 1.6f; bool fx2 = R() < 0.5f; Ig(Deco(Geo.Box(fx2 ? 2.2f : 0.06f, 1.6f, fx2 ? 0.06f : 2.2f), Mats.Basic(0xff7020), x + (fx2 ? (R() - 0.5f) * w * 0.8f : (R() < 0.5f ? -1 : 1) * (w / 2 + 0.08f)), y, z + (fx2 ? (R() < 0.5f ? -1 : 1) * (d / 2 + 0.08f) : (R() - 0.5f) * d * 0.8f))); }
            }
            else if (type == 2)
            {
                // brick shop row: shopfronts with rolled shutters and torn awnings, windows above, a cornice
                foreach (var sz in new[] { -1f, 1f })
                {
                    int n = Mathf.Max(1, (int)(w / 5));
                    for (int k = 0; k < n; k++)
                    {
                        float u = ((k + 0.5f) / n - 0.5f) * w, zf = z + sz * (d / 2 + 0.06f);
                        bool open = R() < 0.4f;
                        Deco(Geo.Box(w / n - 0.8f, 2.6f, 0.08f), open ? glassM : Mats.Std(0x5a5450, 0.6f, 0.6f), x + u, 1.4f, zf);
                        if (!open) for (int s = 0; s < 6; s++) Deco(Geo.Box(w / n - 0.85f, 0.05f, 0.1f), steelM, x + u, 0.4f + s * 0.4f, zf + sz * 0.03f);
                        var awn = Mats.Std(new[] { 0x6a2a20, 0x2a4a3a, 0x5a4a20 }[(seed + k) % 3], 0.9f, 0, 0, null, true);
                        Deco(Geo.Box(w / n - 0.4f, 0.06f, 1.4f), awn, x + u, 3.2f, z + sz * (d / 2 + 0.7f), sz * 0.3f, 0, (R() - 0.5f) * 0.2f);
                        Deco(Geo.Box(w / n - 1.2f, 0.6f, 0.1f), Mats.Std(0x2a2420, 0.8f), x + u, 3.8f, zf);
                        for (float y = 5.2f; y < h - 1; y += 3.2f) Deco(Geo.Box(1.2f, 1.5f, 0.08f), R() < 0.15f ? Mats.Basic(0xffb050) : glassM, x + u, y, zf);
                    }
                }
                Deco(Geo.Box(w + 0.6f, 0.5f, d + 0.6f), concM, x, h - 0.25f, z);
            }
            else
            {
                // apartment block: balconies down the front, a fire escape up the side
                for (float y = 3.2f; y < h - 1; y += 3.2f) for (float u = -w / 2 + 2.5f; u <= w / 2 - 2.5f; u += 5) if (R() < 0.7f)
                { Deco(Geo.Box(2.4f, 0.18f, 1.0f), concM, x + u, y, z + d / 2 + 0.5f); Deco(Geo.Box(2.4f, 0.8f, 0.06f), steelM, x + u, y + 0.5f, z + d / 2 + 1.0f); }
                for (float y = 3.2f; y < h - 1; y += 3.2f) { Deco(Geo.Box(0.08f, 0.1f, 3.4f), steelM, x - w / 2 - 0.9f, y, z); Deco(Geo.Box(1.6f, 0.08f, 3.4f), steelM, x - w / 2 - 0.8f, y, z); Deco(Geo.Box(0.1f, 3.4f, 0.1f), steelM, x - w / 2 - 0.85f, y - 1.4f, z, 0.85f, 0, 0); }
            }
            if (type == 0 || type == 2) FacadeDamage(x, z, w, d, h, seed); // shell holes in the walls
            // the roof: tar, a parapet, and things on it (a stair house, AC units, vents, a water tank or an antenna mast)
            Deco(Geo.Box(w - 0.2f, 0.06f, d - 0.2f), tarM, x, h + 0.03f, z);
            foreach (var sz in new[] { -1f, 1f }) { Box(x, h, z + sz * (d / 2 - 0.2f), w, 0.9f, 0.4f, concDarkM); }
            foreach (var sx in new[] { -1f, 1f }) { Box(x + sx * (w / 2 - 0.2f), h, z, 0.4f, 0.9f, d - 0.8f, concDarkM); }
            Box(x + w * 0.25f, h, z - d * 0.25f, 3, 2.6f, 2.6f, concM); Deco(Geo.Box(1.0f, 2.0f, 0.06f), Mats.Std(0x3a2a20, 0.8f), x + w * 0.25f, h + 1.0f, z - d * 0.25f + 1.31f);
            for (int k = 0; k < 2 + (int)(R() * 3); k++) { float ax = x + (R() - 0.5f) * (w - 4), az = z + (R() - 0.5f) * (d - 4); Box(ax, h, az, 1.8f, 1.1f, 1.4f, steelM); Ig(Deco(Geo.Torus(0.45f, 0.06f, 14, 4), concDarkM, ax, h + 1.12f, az, Mathf.PI / 2, 0, 0)); }
            for (int k = 0; k < 3; k++) Ig(Deco(Geo.Cyl(0.25f, 0.25f, 1.2f, 8), steelM, x + (R() - 0.5f) * (w - 3), h + 0.6f, z + (R() - 0.5f) * (d - 3)));
            if (R() < 0.5f)
            {
                var tank = Mats.Std(0x5a3e2a, 0.85f); float tx = x - w * 0.22f, tz = z + d * 0.22f;
                foreach (var a in new[] { -0.9f, 0.9f }) foreach (var b in new[] { -0.9f, 0.9f }) Deco(Geo.Box(0.15f, 2.4f, 0.15f), steelM, tx + a, h + 1.2f, tz + b);
                Deco(Geo.Cyl(1.3f, 1.3f, 2.4f, 12), tank, tx, h + 3.6f, tz); Deco(Geo.Cone(1.5f, 0.9f, 12), tank, tx, h + 5.25f, tz); SolidAABB(V(tx - 1.3f, h, tz - 1.3f), V(tx + 1.3f, h + 5.7f, tz + 1.3f));
            }
            else { Deco(Geo.Cyl(0.08f, 0.12f, 7, 5), steelM, x - w * 0.3f, h + 3.5f, z + d * 0.3f); for (int k = 0; k < 3; k++) Deco(Geo.Box(1.2f - k * 0.3f, 0.06f, 0.06f), steelM, x - w * 0.3f, h + 4.5f + k * 1.2f, z + d * 0.3f); var bl = Mk.Glow(levelRoot, 0xff2010, 1.2f, 0.8f); bl.transform.localPosition = V(x - w * 0.3f, h + 7.1f, z + d * 0.3f); }
            if (seed % 3 == 0) anims.Add((t, dt) => { if (rand < 0.2f) SpawnParticles(V(x + w * 0.1f, h + 1, z), 1, 1.2f, ashMat, 0.3f, 3f, -2.5f); }); // smoke off the roof
        }
        // ---------- streets ----------
        // a road strip (along x or z) with its lane markings
        void Road(float x, float z, float len, float wid, bool alongX, bool center = true)
        {
            Flat(Geo.Plane(alongX ? len : wid, alongX ? wid : len), asphaltM, x, 0.02f, z);
            for (float u = -len / 2 + 2; u < len / 2 - 2; u += 6)
            {
                if (center) foreach (var o in new[] { -0.15f, 0.15f }) Ig(Deco(Geo.Box(alongX ? 3 : 0.15f, 0.03f, alongX ? 0.15f : 3), laneYellowM, x + (alongX ? u : o), 0.04f, z + (alongX ? o : u)));
                foreach (var o in new[] { -wid / 4, wid / 4 }) Ig(Deco(Geo.Box(alongX ? 2.2f : 0.15f, 0.03f, alongX ? 0.15f : 2.2f), laneWhiteM, x + (alongX ? u + 2 : o), 0.04f, z + (alongX ? o : u + 2)));
            }
            foreach (var s in new[] { -1f, 1f }) Ig(Deco(Geo.Box(alongX ? len : 0.3f, 0.14f, alongX ? 0.3f : len), concM, x + (alongX ? 0 : s * wid / 2), 0.07f, z + (alongX ? s * wid / 2 : 0))); // curbs
        }
        void Crosswalk(float x, float z, float wid, bool alongX) { for (int k = 0; k < 7; k++) { float u = (k - 3) * (wid / 7); Ig(Deco(Geo.Box(alongX ? 0.7f : 2.6f, 0.03f, alongX ? 2.6f : 0.7f), laneWhiteM, x + (alongX ? u : 0), 0.045f, z + (alongX ? 0 : u))); } }
        void Barrier(float x, float z, float ry) { var p = Mk.Piv(levelRoot, x, 0, z); p.SetRot(0, ry, 0); Mk.Msh(p, Geo.Box(0.6f, 0.4f, 3), concM, 0, 0.2f, 0); Mk.Msh(p, Prism(0.6f, 0.6f, 3), concM, 0, 0.4f, 0); Mk.Msh(p, Geo.Box(0.62f, 0.12f, 3.02f), Mats.Std(0xb8402a, 0.8f), 0, 0.75f, 0); foreach (var r in p.t.GetComponentsInChildren<Renderer>()) r.gameObject.layer = Mk.LayerIgnore; float c = Mathf.Abs(Mathf.Cos(ry)), s = Mathf.Abs(Mathf.Sin(ry)); SolidAABB(V(x - s * 1.5f - c * 0.3f, 0, z - c * 1.5f - s * 0.3f), V(x + s * 1.5f + c * 0.3f, 1.0f, z + c * 1.5f + s * 0.3f)); }
        void Debris(float x, float z, float s, bool solid = true)
        {
            for (int k = 0; k < 6; k++) { float a = k * 1.1f; var g = Deco(Geo.Icosahedron(1), k % 2 == 0 ? concM : concDarkM, x + Mathf.Cos(a) * s * 0.6f, s * 0.22f, z + Mathf.Sin(a) * s * 0.5f, k, a, k * 2); g.transform.localScale = V(s * 0.45f, s * 0.3f, s * 0.4f); }
            for (int k = 0; k < 4; k++) Ig(Deco(Geo.Cyl(0.04f, 0.04f, s * 1.2f, 4), rebarM, x + (k - 1.5f) * s * 0.3f, s * 0.4f, z, (k - 1.5f) * 0.5f, k, 0.6f));
            if (solid) SolidAABB(V(x - s * 0.7f, 0, z - s * 0.6f), V(x + s * 0.7f, s * 0.5f, z + s * 0.6f));
        }
        void BurnBarrel(float x, float z) { Deco(Geo.Cyl(0.42f, 0.4f, 1.1f, 10), rustM, x, 0.55f, z); var g = Mk.Glow(levelRoot, 0xff7020, 2.2f, 0.7f); g.transform.localPosition = V(x, 1.4f, z); flames.Add(g.transform); SolidAABB(V(x - 0.42f, 0, z - 0.42f), V(x + 0.42f, 1.1f, z + 0.42f)); anims.Add((t, dt) => { if (rand < 0.35f) SpawnParticles(V(x, 1.2f, z), 1, 1.2f, emberMat, 0.08f, 1, -3); }); }
        void TrafficLight(float x, float z, float ry, bool fallen)
        {
            var p = Mk.Piv(levelRoot, x, 0, z); p.SetRot(fallen ? 1.35f : 0, ry, 0);
            Mk.Msh(p, Geo.Cyl(0.12f, 0.15f, 6, 8), steelM, 0, 3, 0); Mk.Msh(p, Geo.Box(0.12f, 0.12f, 4), steelM, 0, 5.9f, 2);
            var hd = Mk.Piv(p, 0, 5.3f, 3.6f); Mk.Msh(hd, Geo.Box(0.45f, 1.3f, 0.4f), Mats.Std(0x2a2a1a, 0.7f)); foreach (var (y, c) in new[] { (0.4f, 0x401010), (0f, 0x403010), (-0.4f, 0x104010) }) Mk.Msh(hd, Geo.Cyl(0.14f, 0.14f, 0.06f, 10), Mats.Basic(c), 0, y, 0.22f, Mathf.PI / 2, 0, 0);
            foreach (var r in p.t.GetComponentsInChildren<Renderer>()) r.gameObject.layer = Mk.LayerIgnore;
            if (!fallen) SolidAABB(V(x - 0.18f, 0, z - 0.18f), V(x + 0.18f, 6, z + 0.18f));
        }
        // a utility pole, standing or snapped and lying across the street (returns where its cable starts)
        Vector3 UtilityPole(float x, float z, float ry, bool fallen)
        {
            var wood = Mats.Std(0x3a2a1c, 0.95f);
            var p = Mk.Piv(levelRoot, x, 0, z); p.SetRot(fallen ? 1.45f : 0, ry, 0);
            Mk.Msh(p, Geo.Cyl(0.16f, 0.22f, 9, 8), wood, 0, 4.5f, 0); Mk.Msh(p, Geo.Box(2.6f, 0.16f, 0.16f), wood, 0, 8.3f, 0);
            foreach (var sx in new[] { -1f, 1f }) Mk.Msh(p, Geo.Cyl(0.06f, 0.06f, 0.25f, 6), Mats.Std(0x6a8a6a, 0.3f), sx * 1.1f, 8.5f, 0);
            Mk.Msh(p, Geo.Cyl(0.35f, 0.35f, 0.9f, 10), steelM, 0.3f, 7.2f, 0.25f);
            foreach (var r in p.t.GetComponentsInChildren<Renderer>()) r.gameObject.layer = Mk.LayerIgnore;
            if (!fallen) SolidAABB(V(x - 0.22f, 0, z - 0.22f), V(x + 0.22f, 9, z + 0.22f)); else { var pd = V(Mathf.Sin(ry), 0, Mathf.Cos(ry)); for (int k = 0; k < 6; k++) { var pc = V(x, 0, z) + pd * (0.8f + k * 1.5f); SolidAABB(pc - V(0.35f, 0, 0.35f), pc + V(0.35f, 0.5f, 0.35f)); } } // a fallen pole is solid along its length
            return fallen ? V(x, 0.3f, z) : V(x, 8.4f, z);
        }

        // ================= 11. ASHFALL CITY =================
        void BuildAshfall()
        {
            DeadMats(); Arena(66);
            Altar(0, 0, 10, 2);
            // the avenues crossing in the middle, the sidewalks, crosswalks at the corners
            Flat(Geo.Plane(132, 132), sidewalkM, 0, 0.01f, 0);
            Road(0, 0, 132, 14, false); Road(0, 0, 132, 14, true, true);
            Flat(Geo.Plane(14, 14), asphaltM, 0, 0.025f, 0);
            foreach (var s in new[] { -1f, 1f }) { Crosswalk(s * 9.5f, 0, 13, false); Crosswalk(0, s * 9.5f, 13, true); }
            // the blocks: every quadrant its own mix of buildings, alleys between them
            var plan = new (float x, float z, float w, float d)[] { (18, 18, 11, 11), (33, 18, 12, 11), (51, 19, 16, 13), (18, 33, 11, 12), (18, 51, 11, 15), (36, 36, 15, 15), (55, 38, 13, 12), (36, 55, 14, 13), (55, 56, 12, 12) };
            int idx = 0;
            foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 })
            {
                int q = (sx + 1) + (sz + 1) / 2;
                foreach (var b in plan)
                {
                    idx++; int seed = idx * 37 + q * 11; var rr = new System.Random(seed); float R() => (float)rr.NextDouble();
                    int type = (idx * 7 + q * 3) % 5; if (b.x < 25 && b.z < 25) type = q % 2 == 0 ? 2 : 0; // corner shops on the crossroads
                    if (sx < 0 && sz > 0 && b.x > 30 && b.z > 30 && b.x < 40) type = 3; // one parking garage
                    float h = type == 2 ? 6 + R() * 5 : type == 1 ? 24 + R() * 14 : type == 3 ? 10.2f : 12 + R() * 14;
                    CityBuilding(sx * b.x, sz * b.z, b.w, b.d, h, type, seed);
                }
            }
            // the wrecks in the streets: cars, vans, a pickup, a bus slewed across a lane, a jackknifed semi
            var wrecks = new (float x, float z, float ry, int kind, bool burnt)[] {
                (-3.5f, 22, 0.15f, 0, true), (3.8f, 30, Mathf.PI + 0.1f, 1, false), (-4, -24, 0.3f, 2, true), (4, -38, Mathf.PI, 0, false), (3.6f, 46, Mathf.PI - 0.4f, 0, true),
                (22, 3.4f, Mathf.PI / 2 + 0.2f, 0, false), (34, -3.5f, -Mathf.PI / 2, 3, true), (-22, -3.4f, Mathf.PI / 2 - 0.3f, 1, true), (-40, 3.4f, -Mathf.PI / 2 + 0.6f, 4, false), (52, 3, Mathf.PI / 2, 0, true),
                (-54, -3, 1.2f, 2, false), (-4, 58, 0.05f, 0, true) };
            foreach (var wv in wrecks) Vehicle(wv.x, wv.z, wv.ry, wv.kind, wv.burnt);
            // street furniture and the mess of the fall: lights, barriers, debris, burning barrels, a checkpoint
            foreach (var (x, z, ry, f) in new[] { (9f, 9f, Mathf.PI * 1.25f, false), (-9f, -9f, Mathf.PI * 0.25f, true), (9f, -9f, Mathf.PI * 0.75f, false), (-9f, 9f, -Mathf.PI * 0.25f, false) }) TrafficLight(x, z, ry, f);
            foreach (var p in new[] { V(-8.5f, 0, 28), V(8.5f, 0, -30), V(28, 0, 8.5f), V(-30, 0, -8.5f), V(8.5f, 0, 50), V(-8.5f, 0, -50), V(48, 0, -8.5f), V(-48, 0, 8.5f) }) StreetLampS(p.x, p.z, Mathf.Atan2(-p.z, -p.x));
            for (int k = 0; k < 5; k++) Barrier(-5 + k * 2.6f, -14, Mathf.PI / 2 + (k % 2) * 0.2f); // the checkpoint south of the crossroads
            foreach (var p in new[] { V(26, 0, 26), V(-26, 0, 44), V(44, 0, -26), V(-44, 0, -44), V(12, 0, -60), V(-60, 0, 12), V(26, 0, -50) }) Debris(p.x, p.z, 2 + (p.x % 3 + 3) * 0.3f);
            foreach (var p in new[] { V(-11, 0, 15), V(12, 0, -16), V(15, 0, 11), V(-16, 0, -12), V(10, 0, 40), V(-41, 0, 10) }) BurnBarrel(p.x, p.z);
            foreach (var p in new[] { V(-3, 0, 12), V(5, 0, -44), V(-30, 0, 2), V(40, 0, -2) }) { Ig(Flat(Geo.Circle(0.6f, 14), Mats.Std(0x1a1816, 0.6f, 0.6f), p.x, 0.035f, p.z)); var mp = p; anims.Add((t, dt) => { if (rand < 0.25f) SpawnParticles(mp + V(0, 0.2f, 0), 1, 1, steamMat, 0.25f, 1.6f, -2); }); } // steaming manholes
            for (int k = 0; k < 30; k++) { float a = k * 2.399f, r = 8 + (k * 7 % 50); Ig(Flat(Geo.Plane(0.12f, 2 + k % 3), Mats.Trans(0x0a0806, 0.6f), Mathf.Cos(a) * r, 0.04f, Mathf.Sin(a) * r)).transform.localRotation = Quaternion.Euler(-90, k * 37, 0); } // cracks in the asphalt
            // the downed power lines: poles snapped across the streets, the cables live
            var lines = new List<(Vector3, Vector3)>(); var tfs = new List<Vector3>();
            foreach (var (x, z, ry, len) in new[] { (-8.5f, 20f, Mathf.PI / 2 + 0.2f, 15f), (8.5f, -22f, -Mathf.PI / 2 - 0.15f, 15f), (22f, 8.5f, Mathf.PI + 0.1f, 15f), (-24f, -8.5f, 0.15f, 15f), (25.5f, 40f, Mathf.PI, 22f), (-40f, -25.5f, Mathf.PI / 2, 22f) })
            {
                var a = UtilityPole(x, z, ry, true); var dir = V(Mathf.Sin(ry), 0, Mathf.Cos(ry)); var b = a + dir * len; b.y = 0.3f;
                UtilityPole(b.x + dir.x * 1.5f, b.z + dir.z * 1.5f, ry, false);
                lines.Add((a, b)); var tq = Quaternion.Euler(0, ry * Mathf.Rad2Deg, 0) * V(0.3f, 7.2f, 0.25f); tfs.Add(V(b.x + dir.x * 1.5f + tq.x, tq.y, b.z + dir.z * 1.5f + tq.z));
            }
            PadAt(44, 0, 26); PadAt(-44, 0, 26); PadAt(0, -44, 26); PadAt(0, 34, 24);
            DeadCables(lines); // the lines are down and dead (Dead Earth has no electric traps)
            // the trap: COLLAPSING FACADES - dust and grit trickle off a wall above the sidewalk, the spot below is marked, then a
            // slab of the facade comes down
            MechFallingDebris(new[] { V(10, 0, 18), V(-10, 0, -18), V(18, 0, -10), V(-18, 0, 10), V(10, 0, -33), V(-10, 0, 33), V(33, 0, 10), V(-33, 0, -10), V(10, 0, 51), V(-51, 0, -10) }, 9);
        }
        // ---------- DOWNED POWER LINES: the cable hums and sparks crawl along it, then lightning arcs its whole length ----------
        void MechPowerLines((Vector3 a, Vector3 b)[] cables, Vector3[] transformers = null)
        {
            var cabM = Mats.Std(0x141414, 0.7f, 0.3f);
            for (int i = 0; i < cables.Length; i++)
            {
                var (a, b) = cables[i]; float off = i * 1.7f; Vector3? myTf = transformers != null && i < transformers.Length ? transformers[i] : (Vector3?)null; var d = b - a; float L = d.magnitude; var mid = (a + b) / 2;
                var cab = Deco(Geo.Cyl(0.06f, 0.06f, 1, 6), cabM, mid.x, mid.y, mid.z); cab.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d / L); cab.transform.localScale = V(1, L, 1); Ig(cab);
                var glowM = Mats.Add(0x80c8ff, 0, null, true, true); var cg = Deco(Geo.Cyl(0.25f, 0.25f, 1, 6), glowM, mid.x, mid.y, mid.z); cg.transform.localRotation = cab.transform.localRotation; cg.transform.localScale = V(1, L, 1); Ig(cg);
                var warnM = HazMat(0x60a0ff, 0, false); var wp = Flat(Geo.Plane(3.4f, L), warnM, mid.x, 0.05f, mid.z); wp.transform.localRotation = Quaternion.Euler(-90, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 0);
                var arcs = new List<(Transform t, Material m)>();
                for (int k = 0; k < 10; k++) { var am = Mats.Add(0xd8f0ff, 0, null, true, true); var ag = Ig(Mk.Msh(levelRoot, Geo.Box(0.08f, 0.08f, 1), am)); arcs.Add((ag.transform, am)); }
                bool hitP = false;
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 9f; bool warn = u >= 5 && u < 6.6f, live = u >= 6.6f && u < 7.9f;
                    Mats.SetAlpha(glowM, warn ? 0.1f + 0.3f * (u - 5) / 1.6f * (Mathf.Sin(t * 40) > 0 ? 1 : 0.4f) : live ? 0.6f : 0.02f);
                    Mats.SetAlpha(warnM, warn ? 0.08f + 0.2f * (u - 5) / 1.6f : live ? 0.25f : 0);
                    if (warn && rand < 0.7f) { var sp = Vector3.Lerp(a, b, rand) + V(0, 0.2f, 0); SpawnParticles(sp, 2, 3, rand < 0.5f ? sparkMat : moteMat, 0.05f, 0.25f); }
                    if (u >= 5 && u - dt < 5 && HazardLive && Vector3.Distance(mid, P.pos) < 30) Sfx("charge", 0.5f);
                    for (int k = 0; k < arcs.Count; k++)
                    {
                        if (!live) { Mats.SetAlpha(arcs[k].m, 0); continue; }
                        // jagged bolts jumping along the cable, re-cut every frame
                        float s0 = (k + rand * 0.6f) / arcs.Count, s1 = s0 + 0.1f;
                        var p0 = Vector3.Lerp(a, b, s0) + V((rand - 0.5f) * 1.2f, 0.3f + rand * 1.6f, (rand - 0.5f) * 1.2f); var p1 = Vector3.Lerp(a, b, Mathf.Min(1, s1)) + V((rand - 0.5f) * 1.2f, 0.3f + rand * 1.6f, (rand - 0.5f) * 1.2f);
                        var dd = p1 - p0; arcs[k].t.localPosition = (p0 + p1) / 2; arcs[k].t.localRotation = Quaternion.LookRotation(dd); arcs[k].t.localScale = V(1, 1, dd.magnitude); Mats.SetAlpha(arcs[k].m, 0.6f + rand * 0.4f);
                    }
                    if (!live) { hitP = false; return; }
                    if (u - dt < 6.6f)
                    {
                        Sfx("laserTick"); Sfx("boomSmall", 0.6f); Blast(a + V(0, 0.5f, 0), 2.5f, 0x9ad8ff);
                        if (myTf.HasValue)
                        {
                            // the transformer on the standing pole blows: a fireball, a shower of sparks raining down round the pole
                            var tf = myTf.Value; Blast(tf, 3.5f, 0xffd080); Blast(tf, 1.6f, 0xffffff); Sfx("boom", 0.6f);
                            for (int j = 0; j < 30; j++) particles.Add(new Pt { pos = tf, scale = Vector3.one * 0.08f, mat = sparkMat, v = V((rand - 0.5f) * 9, rand * 5, (rand - 0.5f) * 9), life = 1.4f, grav = GRAV });
                            if (HazardLive && Hypot(P.pos.x - tf.x, P.pos.z - tf.z) < 4.5f) HazardHitPlayer(16, tf, V(0, 5, 0));
                        }
                    }
                    if (rand < 0.6f) SpawnParticles(Vector3.Lerp(a, b, rand) + V(0, 0.3f, 0), 2, 6, sparkMat, 0.05f, 0.3f);
                    if (!HazardLive) return;
                    float along = Mathf.Clamp01(Vector3.Dot(P.pos - a, d) / (L * L)); var near = a + d * along;
                    if (!hitP && Hypot(P.pos.x - near.x, P.pos.z - near.z) < 1.8f && P.pos.y < 2.6f) { hitP = true; HazardHitPlayer(24, near, V(0, 9, 0)); hurtFlash = Mathf.Max(hurtFlash, 0.3f); }
                    HazardHitEnemies(e => { float al = Mathf.Clamp01(Vector3.Dot(e.pos - a, d) / (L * L)); var n2 = a + d * al; return Hypot(e.pos.x - n2.x, e.pos.z - n2.z) < 1.8f && e.pos.y < 2.6f; }, 20, e => V(0, 7, 0), 1.2f);
                });
            }
        }

        // ================= 12. THE DROWNED SUBWAY =================
        void BuildSubway()
        {
            DeadMats(); Arena(60, 16);
            const float PH = 1.4f; // platform height
            var tileWall = Mats.Std(0xa8a490, 0.5f); var tileDark = Mats.Std(0x3a4a4a, 0.6f); var water = Mats.Std(0x18282a, 0.05f, 0.3f, 0x020606);
            // floors: the island platform, the side platforms and the concourse; the two track trenches between
            Box(0, 0, 0, 12, PH, 120, concM); TrimRect(0, 0, 12, 120, PH);
            foreach (var sx in new[] { -1, 1 })
            {
                // the east side has a third track cut through its platform (x 19..25): the platform there is in two pieces,
                // and everything that stood on its outer part is shifted 6m east (o)
                float o = sx > 0 ? 6 : 0;
                if (sx < 0) Box(-36, 0, 0, 48, PH, 120, concM);
                else { Box(15.5f, 0, 0, 7, PH, 120, concM); Box(42.5f, 0, 0, 35, PH, 120, concM); }
                foreach (var ex in new[] { 6f, 12f }) { Ig(Deco(Geo.Box(0.5f, 0.04f, 120), laneYellowM, sx * (ex == 6f ? 5.65f : 12.35f), PH + 0.02f, 0)); Ig(Deco(Geo.Box(0.6f, 0.03f, 120), Mats.Std(0xb0a070, 0.9f), sx * (ex == 6f ? 5.0f : 13.0f), PH + 0.02f, 0)); }
                // the trench(es): ballast, standing water, tiled walls with niches
                foreach (var lx in sx > 0 ? new[] { 9f, 22f } : new[] { -9f })
                {
                    Flat(Geo.Plane(6, 120), Mats.Std(0x3a3632, 0.95f, 0, 0, GrimeTex()), lx, 0.02f, 0);
                    for (int k = 0; k < 4; k++) Ig(Flat(Geo.Plane(5.6f, 9 + k * 3), water, lx, 0.07f, -45 + k * 30 + (lx == 22 ? 12 : 0)));
                    for (int z = -54; z <= 54; z += 12) foreach (var wx in new[] { -2.95f, 2.95f }) Ig(Deco(Geo.Box(0.1f, PH - 0.1f, 2), tileDark, lx + wx, PH / 2, z));
                    if (lx == 22) foreach (var ex in new[] { 19f, 25f }) { Ig(Deco(Geo.Box(0.5f, 0.04f, 120), laneYellowM, ex + (ex < 22 ? -0.35f : 0.35f), PH + 0.02f, 0)); }
                }
                // the side platform: tiled columns, benches, signs, bins, a kiosk, timetable boards, abandoned luggage
                for (int z = -48; z <= 48; z += 12) { Box(sx * 16, PH, z, 1.2f, 14.6f, 1.2f, tileWall); Deco(Geo.Box(1.4f, 0.4f, 1.4f), tileDark, sx * 16, PH + 0.2f, z); Deco(Geo.Box(1.4f, 0.8f, 1.4f), tileDark, sx * 16, 15.6f, z); }
                for (int z = -42; z <= 42; z += 12) { Box(sx * 20 + o, PH, z, 0.8f, 0.5f, 3.4f, Mats.Std(0x5a4a3a, 0.8f)); Deco(Geo.Box(0.15f, 0.7f, 3.4f), Mats.Std(0x5a4a3a, 0.8f), sx * 20.4f + o, PH + 0.85f, z); }
                foreach (var z in new[] { -30f, 6f, 30f }) { var sg = Mk.Piv(levelRoot, sx * 16, PH + 5.5f, z + 6); Mk.Msh(sg, Geo.Box(0.15f, 0.8f, 3.6f), Mats.Std(0x1a3a6a, 0.6f)); Mk.Msh(sg, Geo.Box(0.17f, 0.5f, 3.0f), Mats.Basic(0xd8d8d0), 0, 0, 0); foreach (var r in sg.t.GetComponentsInChildren<Renderer>()) r.gameObject.layer = Mk.LayerIgnore; } // station name boards
                Box(sx * 26 + o, PH, -10, 3, 2.6f, 5, Mats.Std(0x6a2a20, 0.6f)); Deco(Geo.Box(3.1f, 0.3f, 5.1f), Mats.Basic(0xd8a040), sx * 26 + o, PH + 2.3f, -10); // a kiosk
                foreach (var z in new[] { -20f, 22f }) { Box(sx * 24 + o, PH, z, 1.0f, 1.9f, 0.9f, Mats.Std(0x2a5a7a, 0.5f, 0.4f)); Ig(Deco(Geo.Box(0.06f, 0.9f, 0.7f), Mats.Basic(0x9ad8ff), sx * 23.48f + o, PH + 1.3f, z)); } // vending machines
                for (int k = 0; k < 4; k++) Ig(Deco(Geo.Box(0.6f, 0.5f, 0.8f), Mats.Std(new[] { 0x3a2a1a, 0x2a3a5a, 0x5a2a2a, 0x2a2a2a }[k], 0.8f), sx * (18 + k * 0.7f) + o * 1.3f, PH + 0.25f, 34 + k * 1.3f, 0, k, 0)); // abandoned luggage
                // the concourse beyond: a ticket hall with turnstiles, a row of shopfronts, the stopped escalators up to the street
                float tx = sx > 0 ? 48 : -30;
                for (int k = 0; k < 6; k++) { bool down = k == 2; Box(tx, PH, -6 + k * 2.4f, 0.4f, 1.0f, 0.5f, steelM); if (!down) Deco(Geo.Box(0.06f, 0.06f, 1.3f), steelM, tx, PH + 0.9f, -6 + k * 2.4f + 0.7f); }
                Box(tx, PH, -10.5f, 4, 1.1f, 3, Mats.Std(0x4a4440, 0.6f)); Box(tx, PH, 10.5f, 4, 1.1f, 3, Mats.Std(0x4a4440, 0.6f)); // ticket booths
                for (int k = 0; k < 5; k++) { float z = -44 + k * 22; Box(sx * 57, PH, z, 6, 6, 8, tileWall); Deco(Geo.Box(0.08f, 3.2f, 6.2f), k % 2 == 0 ? glassM : Mats.Std(0x5a5450, 0.6f, 0.6f), sx * 53.95f, PH + 1.8f, z); Deco(Geo.Box(0.1f, 0.6f, 6), Mats.Basic(new[] { 0xd84020, 0x30a0d8, 0xd8a020 }[k % 3]), sx * 53.9f, PH + 4.2f, z); }
                for (int k = 0; k < 10; k++) Box(sx * 40 + o, PH + k * 0.55f, 44 + k * 1.0f, 4, 0.55f, 1.0f, steelM); // escalator steps rising north
                foreach (var ex in new[] { -2.2f, 2.2f }) Deco(Geo.Box(0.3f, 1.2f, 11), Mats.Std(0x2a2a2a, 0.6f), sx * 40 + o + ex, PH + 3.4f, 48.5f, -0.5f, 0, 0);
                {
                    // the escalator comes up to a landing and the street exit - caved in: the shutter half down, rubble heaped against it
                    float ex0 = sx * 40 + o, top = PH + 10 * 0.55f;
                    Box(ex0, 0, 57, 7, top, 6, concM); TrimRect(ex0, 57, 7, 6, top);
                    Box(ex0, top, 59.6f, 7, 6, 0.8f, tileWall);
                    Deco(Geo.Box(5, 2.4f, 0.12f), steelM, ex0, top + 2.6f, 59.15f); for (int k = 0; k < 8; k++) Ig(Deco(Geo.Box(5, 0.05f, 0.14f), Mats.Std(0x3a3a3a, 0.6f, 0.6f), ex0, top + 1.6f + k * 0.3f, 59.12f));
                    Ig(Deco(Geo.Box(1.8f, 0.45f, 0.1f), Mats.Std(0x1e5a32, 0.6f), ex0, top + 4.3f, 59.15f)); Ig(Deco(Geo.Box(1.2f, 0.18f, 0.11f), Mats.Basic(0xe8f0e0), ex0, top + 4.32f, 59.15f));
                    for (int k = 0; k < 7; k++) { var rb = Deco(Geo.Icosahedron(1), k % 2 == 0 ? concM : concDarkM, ex0 + (k - 3) * 0.85f, top + 0.35f, 58.1f + (k % 2) * 0.5f, k, k * 1.3f, 0); rb.transform.localScale = V(0.9f + (k % 3) * 0.3f, 0.6f + (k % 2) * 0.4f, 0.8f); Ig(rb); }
                    SolidAABB(V(ex0 - 3.5f, top, 57.6f), V(ex0 + 3.5f, top + 0.9f, 59.2f));
                    foreach (var exs in new[] { -3.4f, 3.4f }) SolidAABB(V(ex0 + exs - 0.1f, top, 54), V(ex0 + exs + 0.1f, top + 1.1f, 59.2f));
                }
            }
            // the ceiling: concrete beams over everything, broken through in places where the roof came down; strip lights, some flickering red
            for (int z = -54; z <= 54; z += 9) { Deco(Geo.Box(120, 1.2f, 1.4f), concDarkM, 0, 15.4f, z); }
            for (int k = 0; k < 14; k++) { float z = -52 + k * 8; bool emerg = k % 4 == 1; var g = Mk.Glow(levelRoot, emerg ? 0xff2010 : 0xd8e0c8, 3, emerg ? 0.6f : 0.4f); g.transform.localPosition = V((k % 2 == 0 ? -1 : 1) * 16, 13.5f, z); if (emerg) { var gm = g.GetComponent<Renderer>().sharedMaterial; anims.Add((t, dt) => Mats.SetAlpha(gm, Mathf.Sin(t * 3 + z) > 0 ? 0.6f : 0.1f)); } }
            // the collapse: a heap of rubble and a broken slab where the street fell into the station, light pouring in
            foreach (var p in new[] { V(-38, PH, -40), V(-34, PH, -37), V(-41, PH, -35) }) { var g = Deco(Geo.Icosahedron(1), concM, p.x, p.y + 1, p.z, p.x, p.z, 0); g.transform.localScale = V(3, 2, 2.6f); }
            SolidAABB(V(-44, PH, -43), V(-31, PH + 2.4f, -32)); Deco(Geo.Box(14, 1, 10), concM, -37, PH + 4, -38, 0.5f, 0.4f, 0.3f); SolidAABB(V(-42, PH, -42), V(-32, PH + 4.5f, -34));
            LightShaft(V(-37, 40, -38), V(-37, PH, -38), 4, 7, 0xffd8a0, 0.14f);
            Box(-37, PH, -46, 1.6f, 1.2f, 1.6f, steelM); // a fallen lamp housing
            // the footbridge across the tracks, with stairs up from both side platforms
            // (it reaches over the third track too: west stairs come up from the west platform, east ones from beyond the third track)
            FootBridge(PH);
            Stairs(-25.5f, 0, 3.6f, V(1, 0, 0), 9, PH, 8, 1.0f, steelM); Stairs(39.6f, 0, 3.6f, V(-1, 0, 0), 9, PH, 8, 1.0f, steelM);
            Altar(0, 0, 6, 3.5f);
            foreach (var sx in new[] { -1, 1 }) { PadAt(sx * 36, 34, 22); PadAt(sx * 36, -34, 22); }
            SubwayExtras(PH);
            MechTrains2(new[] { -9f, 9f, 22f }, PH); // three tracks, three trains
        }
        // the footbridge over the three tracks: a riveted steel deck on two box girders, truss sides with solid railings, plate
        // supports on the platforms between the tracks, lamps slung under it, way-out signs at each end. It's broken in the
        // middle, right over the portal dais: the two halves end in torn, drooping deck plates and bent girders, cables
        // hanging into the gap - and the portal's beam rises up through it
        void FootBridge(float PH)
        {
            const float Y = 8, x0 = -17, x1 = 31, g0 = -3.2f, g1 = 3.6f; // (the gap: g0..g1)
            var plate = Mats.Std(0x5a5c60, 0.5f, 0.7f, 0, RustTex()); var dark = Mats.Std(0x2a2a2c, 0.5f, 0.7f, 0, RustTex()); var stripe = Mats.Std(0xd8a020, 0.7f, 0, 0, GrimeTex());
            var tread = Mats.Std(0x3a3c40, 0.6f, 0.6f, 0, GrimeTex()); var cab = Mats.Std(0x141414, 0.7f, 0.3f);
            foreach (var (a, b) in new[] { (x0, g0), (g1, x1) })
            {
                float L = b - a, cx = (a + b) / 2;
                Box(cx, Y, 0, L, 0.5f, 4, plate); Ig(Deco(Geo.Box(L, 0.04f, 3.6f), tread, cx, Y + 0.52f, 0));
                foreach (var sz in new[] { -1f, 1f })
                {
                    Deco(Geo.Box(L, 1.0f, 0.35f), dark, cx, Y - 0.45f, sz * 1.85f); Ig(Deco(Geo.Box(L, 0.06f, 0.2f), stripe, cx, Y + 0.53f, sz * 1.88f));
                    Deco(Geo.Box(L, 0.12f, 0.12f), plate, cx, Y + 1.5f, sz * 1.95f); Deco(Geo.Box(L, 0.08f, 0.08f), plate, cx, Y + 1.0f, sz * 1.95f);
                    for (float x = a; x <= b + 0.01f; x += 2) { Deco(Geo.Box(0.12f, 1.0f, 0.12f), plate, x, Y + 1.0f, sz * 1.95f); if (x + 2 <= b) Ig(Deco(Geo.Box(0.06f, 2.3f, 0.06f), dark, x + 1, Y + 1.0f, sz * 1.95f, 0, 0, (((int)Mathf.Abs(x) / 2) % 2 == 0 ? 1 : -1) * 1.1f)); }
                    SolidAABB(V(a, Y, sz * 1.95f - 0.12f), V(b, Y + 1.6f, sz * 1.95f + 0.12f));
                }
                // the torn end facing the gap: the deck plate bent down into it, the girders sheared and sagging, rails snapped
                bool left = a < 0; float ex = left ? b : a, s = left ? 1 : -1;
                var flap = Mk.Piv(levelRoot, ex, Y + 0.25f, 0); flap.SetRot(0, 0, s * -0.75f); Mk.Msh(flap, Geo.Box(1.6f, 0.12f, 3.6f), plate, s * 0.8f, 0, 0.1f); Mk.Msh(flap, Geo.Box(1.5f, 0.04f, 3.0f), tread, s * 0.8f, 0.07f, -0.2f);
                foreach (var sz in new[] { -1f, 1f }) { var gd = Mk.Piv(levelRoot, ex, Y - 0.45f, sz * 1.85f); gd.SetRot(0, 0, s * -(0.35f + (sz > 0 ? 0.2f : 0))); Mk.Msh(gd, Geo.Box(1.3f, 0.9f, 0.3f), dark, s * 0.65f, 0, 0); var rl = Mk.Piv(levelRoot, ex, Y + 1.5f, sz * 1.95f); rl.SetRot(0, sz * 0.4f, s * -(0.5f + (sz > 0 ? 0.3f : 0))); Mk.Msh(rl, Geo.Box(1.4f, 0.1f, 0.1f), plate, s * 0.7f, 0, 0); }
                for (int k = 0; k < 6; k++) Mk.Msh(flap, Geo.Cyl(0.03f, 0.03f, 0.9f, 4), rebarM, s * (1.55f + (k % 2) * 0.1f), 0.05f, -1.5f + k * 0.6f, 0, 0, s * (1.0f + (k % 3) * 0.3f));
                foreach (var rr in flap.t.GetComponentsInChildren<Renderer>()) rr.gameObject.layer = Mk.LayerIgnore;
                SagCable(V(ex, Y - 0.2f, -1.1f), V(ex + s * 1.6f, Y - 3.2f, -0.9f), -0.4f, cab, 0.04f); SagCable(V(ex, Y - 0.3f, 1.0f), V(ex + s * 1.0f, Y - 4.4f, 1.3f), -0.3f, cab, 0.035f);
                var sp = V(ex + s * 0.6f, Y - 0.5f, 0); anims.Add((t, dt) => { if (rand < 0.06f) SpawnParticles(sp + V(0, 0, (rand - 0.5f) * 3), 2, 2, sparkMat, 0.04f, 0.4f); }); // a severed lamp cable still sparking
            }
            // supports on the platforms between the tracks (never in a trench, clear of the portal dais)
            foreach (var px in new[] { -12.6f, -4.8f, 4.8f, 15.5f, 27.5f }) { Box(px, PH, 0, 1.2f, Y - PH, 2.4f, dark); Deco(Geo.Box(1.6f, 0.3f, 3.2f), plate, px, Y - 0.15f, 0); Deco(Geo.Box(1.6f, 0.3f, 2.8f), plate, px, PH + 0.15f, 0); }
            // lamps slung under it
            foreach (var x in new[] { -13f, -7f, 8f, 16f, 24f }) { Ig(Deco(Geo.Cone(0.45f, 0.35f, 8), dark, x, Y - 1.2f, 0)); var g = Mk.Glow(levelRoot, 0xffe0a0, 2.4f, 0.45f); g.transform.localPosition = V(x, Y - 1.4f, 0); }
            // way-out signs over each end
            foreach (var ex in new[] { x0 + 1.5f, x1 - 1.5f }) { var sg = Deco(Geo.Box(0.12f, 0.6f, 2.6f), Mats.Std(0x1e5a32, 0.6f), ex, Y + 2.6f, 0); Ig(sg); Ig(Deco(Geo.Box(0.13f, 0.18f, 1.6f), Mats.Basic(0xe8f0e0), ex, Y + 2.65f, 0)); foreach (var sz in new[] { -1f, 1f }) Ig(Deco(Geo.Box(0.06f, 0.9f, 0.06f), plate, ex, Y + 2.05f, sz * 1.95f)); }
        }
        // ---------- TRAINS: three-car trains, every six seconds a lane; they hit for exactly 35 ----------
        void MechTrains2(float[] lanes, float ph)
        {
            float L = LB * 2; const float SPEED = 50, CAR = 17, GAP = 1.2f; float TL = CAR * 3 + GAP * 2;
            var railM = Mats.Std(0x8a8a90, 0.3f, 0.8f); var sleeperM = Mats.Std(0x3a2a1a, 0.9f); var thirdM = Mats.Std(0x6a6a40, 0.5f, 0.6f);
            var bodyM = Mats.Std(0x8a8c90, 0.35f, 0.7f); var stripe = Mats.Std(0xc85020, 0.6f); var stripe2 = Mats.Std(0x2a4a8a, 0.6f); var win = Mats.Basic(0xffd890); var dark = Mats.Std(0x141414, 0.8f); var doorM = Mats.Std(0x6a6c70, 0.4f, 0.7f);
            for (int i = 0; i < lanes.Length; i++)
            {
                float lx = lanes[i], off = i * 2.1f; int dirZ = i % 2 == 0 ? 1 : -1; // (the three trains a third of a cycle apart)
                foreach (var s in new[] { -0.72f, 0.72f }) Ig(Deco(Geo.Box(0.12f, 0.18f, L), railM, lx + s, 0.1f, 0));
                Ig(Deco(Geo.Box(0.3f, 0.2f, L), thirdM, lx + 2.2f * (lx > 0 ? 1 : -1), 0.12f, 0));
                for (float z = -LB + 1; z < LB; z += 1.6f) Ig(Deco(Geo.Box(2.4f, 0.08f, 0.3f), sleeperM, lx, 0.04f, z));
                var laneM = HazMat(0xff2010, 0, false); Flat(Geo.Plane(5.4f, L), laneM, lx, 0.09f, 0);
                var sigs = new List<Material>();
                foreach (var sz in new[] { -1, 1 }) { Deco(Geo.Box(6, 6.4f, 0.4f), Mats.Basic(0x050404), lx, 3.2f, sz * (LB - 0.05f)); Deco(Geo.Arc(3.0f, 0.45f, 16), concDarkM, lx, 4.8f, sz * (LB - 0.25f)); Deco(Geo.Box(0.5f, 1.4f, 0.4f), Mats.Std(0x1a1a1a, 0.6f), lx + 2.6f, 4, sz * (LB - 0.7f)); var tl = Mk.Glow(levelRoot, 0x40ff60, 1.4f, 0.85f); tl.transform.localPosition = V(lx + 2.6f, 4, sz * (LB - 0.95f)); sigs.Add(tl.GetComponent<Renderer>().sharedMaterial); } // tunnel mouths, signal lamps (green; red while a train is due)
                // warning beacons along both platform edges of the trench: they spin up amber when a train is due
                var beacons = new List<Material>();
                foreach (var ex in new[] { -3.25f, 3.25f }) for (float bz = -LB + 8; bz < LB - 4; bz += 12) { Ig(Deco(Geo.Cyl(0.12f, 0.14f, 0.2f, 8), Mats.Std(0x2a2a2a, 0.6f), lx + ex, ph + 0.1f, bz)); var bg = Mk.Glow(levelRoot, 0xffa020, 1.1f, 0); bg.transform.localPosition = V(lx + ex, ph + 0.3f, bz); beacons.Add(bg.GetComponent<Renderer>().sharedMaterial); }
                var head = Mk.Glow(levelRoot, 0xfff0c0, 7, 0); head.transform.localPosition = V(lx, 2.2f, -dirZ * (LB - 1)); var headM = head.GetComponent<Renderer>().sharedMaterial;
                // the train: three cars with rounded cabs at each end, window bands, doors, roof units, bogies; coupled
                var tr = Mk.Piv(levelRoot, lx, 0, -dirZ * (LB + 60));
                for (int c = 0; c < 3; c++)
                {
                    float cz = (c - 1) * (CAR + GAP);
                    Mk.Msh(tr, Geo.Box(3.4f, 3.4f, CAR), bodyM, 0, 2.3f, cz); Mk.Msh(tr, Geo.Box(3.45f, 0.35f, CAR + 0.02f), stripe, 0, 1.25f, cz); Mk.Msh(tr, Geo.Box(3.45f, 0.12f, CAR + 0.02f), stripe2, 0, 3.75f, cz);
                    Mk.Msh(tr, Geo.Cyl(1.72f, 1.72f, CAR, 14, false), bodyM, 0, 3.6f, cz, Mathf.PI / 2, 0, 0).transform.localScale = V(1, 1, 0.3f); // the rounded roof
                    foreach (var sx in new[] { -1f, 1f })
                    {
                        Mk.Msh(tr, Geo.Box(0.04f, 0.9f, CAR * 0.86f), win, sx * 1.72f, 2.75f, cz);
                        for (int k = 0; k < 3; k++) { Mk.Msh(tr, Geo.Box(0.06f, 2.4f, 1.5f), doorM, sx * 1.73f, 2.1f, cz + (k - 1) * CAR * 0.33f); Mk.Msh(tr, Geo.Box(0.07f, 0.06f, 1.5f), dark, sx * 1.74f, 2.1f, cz + (k - 1) * CAR * 0.33f); }
                    }
                    foreach (var bz in new[] { -0.33f, 0.33f }) { Mk.Msh(tr, Geo.Box(2.6f, 0.6f, 3), dark, 0, 0.55f, cz + bz * CAR); foreach (var wz in new[] { -0.9f, 0.9f }) foreach (var sx in new[] { -1f, 1f }) Mk.Msh(tr, Geo.Cyl(0.45f, 0.45f, 0.2f, 10), steelM, sx * 0.72f, 0.45f, cz + bz * CAR + wz, 0, 0, Mathf.PI / 2); }
                    Mk.Msh(tr, Geo.Box(2.2f, 0.4f, 3.2f), steelM, 0, 4.4f, cz);
                    if (c < 2) Mk.Msh(tr, Geo.Box(1.2f, 2.6f, GAP + 0.4f), dark, 0, 2.3f, cz + (CAR + GAP) / 2); // the gangway between cars
                }
                float fz = dirZ * (CAR * 1.5f + GAP);
                Mk.Msh(tr, Geo.Box(3.2f, 1.6f, 0.1f), win, 0, 2.9f, fz + dirZ * 0.02f); Mk.Msh(tr, Geo.Box(2.4f, 0.35f, 0.08f), Mats.Basic(0xd88a20), 0, 3.95f, fz + dirZ * 0.04f);
                foreach (var sx in new[] { -1f, 1f }) Mk.Msh(tr, Geo.Box(0.4f, 0.25f, 0.08f), Mats.Basic(0xfff0c0), sx * 1.1f, 1.6f, fz + dirZ * 0.05f);
                var lamp = Mk.Glow(tr.t, 0xfff0c0, 5, 0.9f); lamp.transform.localPosition = V(0, 1.8f, fz + dirZ * 0.6f);
                foreach (var g in tr.t.GetComponentsInChildren<MeshRenderer>()) g.gameObject.layer = Mk.LayerIgnore;
                tr.t.gameObject.SetActive(false);
                float passT = (L + TL + 20) / SPEED, cycle = 1.6f + passT + 0.6f; bool hitP = false; // a train every ~6 s
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % cycle;
                    bool warn = u < 1.6f, pass = u >= 1.6f && u < 1.6f + passT;
                    Mats.SetAlpha(laneM, warn ? 0.1f + 0.22f * (Mathf.Sin(u * 22) > 0 ? 1 : 0) : pass ? 0.12f : 0);
                    Mats.SetAlpha(headM, warn ? u / 1.6f : 0);
                    foreach (var sm in sigs) Mats.SetColor(sm, warn || pass ? 0xff2010 : 0x40ff60);
                    for (int b = 0; b < beacons.Count; b++) Mats.SetAlpha(beacons[b], warn || pass ? (Mathf.Sin(t * 14 + b * 0.9f) > 0.2f ? 0.95f : 0.1f) : 0);
                    if (u < dt && HazardLive && Mathf.Abs(P.pos.x - lx) < 20) { Sfx("charge", 0.5f); Sfx("alarm", 0.35f); }
                    tr.t.gameObject.SetActive(pass);
                    if (!pass) { hitP = false; return; }
                    float zc = -dirZ * (LB + TL / 2 + 10) + dirZ * (u - 1.6f) * SPEED; tr.t.localPosition = V(lx, 0, zc);
                    if (u - dt < 1.6f) Sfx("boom", 0.7f);
                    if (Mathf.Abs(P.pos.z - zc) < 40 && rand < 0.6f) P.shake = Mathf.Max(P.shake, 0.12f);
                    if (rand < 0.6f) SpawnParticles(V(lx + (rand - 0.5f) * 3, 0.3f, zc - dirZ * TL / 2), 1, 3, sparkMat, 0.06f, 0.3f);
                    if (!HazardLive) return;
                    if (!hitP && Mathf.Abs(P.pos.x - lx) < 2.0f && P.pos.y < 4.6f && Mathf.Abs(P.pos.z - zc) < TL / 2 + 0.5f && P.iframes <= 0)
                    { hitP = true; DamagePlayer(35, V(lx, 1, zc), false, true); P.vel += V(Mathf.Sign(P.pos.x - lx + 0.01f) * 20, 9, dirZ * 16); P.onGround = false; P.coyote = 0; } // exactly 35
                    HazardHitEnemies(e => Mathf.Abs(e.pos.x - lx) < 2.2f && e.pos.y < 4.4f && Mathf.Abs(e.pos.z - zc) < TL / 2, 25, e => V(Mathf.Sign(e.pos.x - lx + 0.01f) * 18, 9, dirZ * 14), 1);
                });
            }
        }

        // ================= 13. OVERPASS GRAVEYARD =================
        void BuildOverpass()
        {
            DeadMats(); Arena(66);
            Altar(0, 0, 10, 3);
            const float DY = 8, DT = 1.6f; // deck top height and thickness
            Flat(Geo.Plane(132, 132), Mats.Std(0x3a3430, 0.95f), 0, 0.01f, 0);
            Road(0, 0, 132, 12, true); Road(0, 0, 132, 10, false, false);
            foreach (var sz in new[] { -1f, 1f })
            {
                float zc = sz * 30;
                // the deck: a solid slab, girders under it, jersey barriers along both edges, lane markings, lamp posts
                if (sz > 0) { Box(-14, DY - DT, zc, 104, DT, 14, concM); Box(59, DY - DT, zc, 14, DT, 14, concM); } else Box(0, DY - DT, zc, 132, DT, 14, concM); // (the north deck is broken between x 38 and 52)
                for (int k = 0; k < 5; k++)
                {
                    if (sz > 0) { Ig(Deco(Geo.Box(104, 0.9f, 0.4f), concDarkM, -14, DY - DT - 0.45f, zc - 5.6f + k * 2.8f)); Ig(Deco(Geo.Box(14, 0.9f, 0.4f), concDarkM, 59, DY - DT - 0.45f, zc - 5.6f + k * 2.8f)); }
                    else Ig(Deco(Geo.Box(132, 0.9f, 0.4f), concDarkM, 0, DY - DT - 0.45f, zc - 5.6f + k * 2.8f));
                }
                foreach (var e in new[] { -1f, 1f })
                {
                    bool inner = e * sz < 0; // the barrier on the side facing the middle has a gap where the stairs come up
                    foreach (var (x0, x1) in sz > 0 ? (inner ? new[] { (-66f, -42f), (-38f, 38f), (52f, 66f) } : new[] { (-66f, 38f), (52f, 66f) }) : (inner ? new[] { (-66f, -42f), (-38f, 66f) } : new[] { (-66f, 66f) }))
                    { Box((x0 + x1) / 2, DY, zc + e * 6.7f, x1 - x0, 1.0f, 0.6f, concM); Ig(Deco(Geo.Box(x1 - x0, 0.1f, 0.62f), Mats.Std(0xb8402a, 0.8f), (x0 + x1) / 2, DY + 0.95f, zc + e * 6.7f)); }
                }
                Stairs(-40, sz * 12, 3.6f, V(0, 0, sz), 16, 0, DY, 0.7f, concM); // stairs up from the road (half-metre steps)
                for (float x = -62; x < 62; x += 6) { if (sz > 0 && x > 35 && x < 54) continue; Ig(Deco(Geo.Box(3, 0.03f, 0.15f), laneWhiteM, x, DY + 0.02f, zc)); }
                for (float x = -50; x <= 50; x += 25) { if (sz > 0 && x > 35 && x < 54) continue; Deco(Geo.Cyl(0.12f, 0.15f, 6, 8), steelM, x, DY + 3, zc + 6.2f); SolidAABB(V(x - 0.18f, DY, zc + 6.0f), V(x + 0.18f, DY + 6, zc + 6.4f)); Deco(Geo.Box(1.4f, 0.12f, 0.12f), steelM, x, DY + 6, zc + 5.6f, 0, Mathf.PI / 2, 0); var lg = Mk.Glow(levelRoot, 0xffb060, 2.4f, 0.45f); lg.transform.localPosition = V(x, DY + 5.8f, zc + 5); }
                // T-piers: a column and a cap beam under the deck every 22m
                for (float x = -55; x <= 55; x += 22) { Box(x, 0, zc, 3, DY - DT - 1.2f, 3, concM); Box(x, DY - DT - 1.2f, zc, 4, 1.2f, 14, concDarkM); Ig(Deco(Geo.Box(3.2f, 0.4f, 3.2f), concDarkM, x, 0.2f, zc)); }
                // a green gantry sign over each deck
                Box(20 * sz, DY, zc - 6.4f, 0.5f, 7, 0.5f, steelM); Box(20 * sz, DY, zc + 6.4f, 0.5f, 7, 0.5f, steelM); Deco(Geo.Box(0.6f, 0.6f, 13.4f), steelM, 20 * sz, DY + 7, zc);
                var sg = Deco(Geo.Box(0.2f, 2.4f, 6), Mats.Std(0x1e5a32, 0.6f), 20 * sz + 0.4f, DY + 5.6f, zc - 2.5f, 0, 0, 0.12f * sz); Deco(Geo.Box(0.22f, 0.25f, 4.6f), laneWhiteM, 20 * sz + 0.45f, DY + 6.1f, zc - 2.5f, 0, 0, 0.12f * sz);
                // wrecks on the deck
                foreach (var (x, ry, kind) in new[] { (-30f, 0.2f, 0), (-12f, Mathf.PI / 2 + 0.3f, 2), (8f, Mathf.PI / 2 - 0.1f, 0), (34f, 1.1f, 1) }) Vehicle(x, zc + (kind == 1 ? 2.5f : -2.8f) * sz, Mathf.PI / 2 + ry * 0.4f, kind, x > 0, DY);
            }
            // the broken span on the north deck: a gap, the fallen slab leaning to the ground, rebar hanging off both ends
            {
                float zc = 30, gx0 = 38, gx1 = 52;
                // (the deck has no floor here: it fell in)
                var slab = Deco(Geo.Box(15, DT, 13.5f), concM, (gx0 + gx1) / 2, DY * 0.45f, zc, 0, 0, -0.62f); Ig(slab);
                // its walking surface: fine invisible steps (0.4m high, so the step-up carries you smoothly) right under the
                // slab's top face, from the road up to the deck edge
                { const float ang = 0.62f; float tn = Mathf.Tan(ang), cx = (gx0 + gx1) / 2, cy = DY * 0.45f, topOff = (DT / 2) / Mathf.Cos(ang);
                  for (float sx0 = gx0 + 0.6f; sx0 < gx1 - 0.6f; sx0 += 0.55f) { float top = Mathf.Min(DY, cy - (sx0 + 0.275f - cx) * tn + topOff - 0.05f); if (top < 0.15f) continue; SolidAABB(V(sx0, 0, zc - 6.6f), V(sx0 + 0.55f, top, zc + 6.6f)); }
                  foreach (var s in new[] { -1f, 1f }) SolidAABB(V(gx0, 0, zc + s * 6.6f - (s > 0 ? 0 : 0.4f)), V(gx1, 1.0f, zc + s * 6.6f + (s > 0 ? 0.4f : 0))); } // (low kerbs along its edges)
                for (int k = 0; k < 16; k++) Ig(Deco(Geo.Cyl(0.04f, 0.04f, 2.5f, 4), rebarM, (k % 2 == 0 ? gx0 : gx1) + (k % 2 == 0 ? 0.5f : -0.5f), DY - DT - 0.6f, zc - 6 + k * 0.8f, 0, 0, k % 2 == 0 ? -0.6f : 0.6f));
                Debris(gx0 + 4, zc + 9, 2.6f); Debris(gx1 - 2, zc - 9, 2.2f);
            }
            // the road below: a jackknifed semi, a bus on the median, crushed cars under the decks, cones, barriers, tyres
            foreach (var wv in new (float x, float z, float ry, int kind, bool burnt)[] { (-20, 3, 0.9f, 4, false), (24, -2, 1.7f, 3, true), (-44, -4, 1.4f, 0, true), (46, 3.5f, -1.5f, 1, false), (-3, 44, 0.2f, 0, true), (6, -46, 2.8f, 2, false), (-32, 42, 1.0f, 0, false), (30, -44, 0.4f, 1, true), (-50, 22, 0, 0, true), (52, -21, 0.3f, 2, true) })
                Vehicle(wv.x, wv.z, wv.ry, wv.kind, wv.burnt);
            for (int k = 0; k < 8; k++) Barrier(-30 + k * 8.5f, (k % 2 == 0 ? 7.5f : -7.5f), Mathf.PI / 2 + (k % 3 - 1) * 0.25f);
            var cone = Mats.Std(0xd86020, 0.7f);
            for (int k = 0; k < 18; k++) { float x = -55 + k * 6.4f, z = ((k * 7) % 5 - 2) * 3; if (Mathf.Abs(x) < 7 && Mathf.Abs(z) < 7) continue; Ig(Deco(Geo.Cone(0.25f, 0.7f, 8), cone, x, 0.35f, z, k % 4 == 0 ? 1.5f : 0, 0, 0)); if (k % 4 != 0) SolidAABB(V(x - 0.25f, 0, z - 0.25f), V(x + 0.25f, 0.7f, z + 0.25f)); }
            foreach (var p in new[] { V(-14, 0, 22), V(16, 0, -22), V(36, 0, 14), V(-38, 0, -16), V(0, 0, 58), V(0, 0, -58) }) Debris(p.x, p.z, 2.2f);
            for (int k = 0; k < 4; k++) { float x = -58 + k * 39; var bb = Mk.Piv(levelRoot, x, 0, k % 2 == 0 ? 52 : -52); Mk.Msh(bb, Geo.Box(0.5f, 10, 0.5f), steelM, 0, 5, 0); Mk.Msh(bb, Geo.Box(9, 4, 0.3f), Mats.Std(new[] { 0x3a2a20, 0x2a3a4a }[k % 2], 0.8f), 0, 11, 0); Mk.Msh(bb, Geo.Box(8.4f, 3.4f, 0.32f), Mats.Std(0x8a6a40, 0.9f), 0, 11, 0.02f); SolidAABB(V(x - 0.3f, 0, (k % 2 == 0 ? 52 : -52) - 0.3f), V(x + 0.3f, 10, (k % 2 == 0 ? 52 : -52) + 0.3f)); } // billboards
            // a crashed helicopter in the middle of the road, the landmark
            {
                var hp = Mk.Piv(levelRoot, -36, 0, 10); hp.SetRot(0.15f, 0.8f, 0.5f); var hm = Mats.Std(0x3a4a2a, 0.7f, 0.3f);
                Mk.Msh(hp, GeoX.Capsule(1.4f, 3.2f, 3, 12), hm, 0, 1.6f, 0, Mathf.PI / 2, 0, 0); Mk.Msh(hp, Geo.Box(0.4f, 0.6f, 6), hm, 0, 1.9f, -4.4f); Mk.Msh(hp, Geo.Box(0.15f, 1.6f, 1.0f), hm, 0, 2.6f, -7.2f);
                Mk.Msh(hp, Geo.Box(1.8f, 1.1f, 0.08f), glassM, 0, 1.9f, 2.6f, -0.4f, 0, 0); Mk.Msh(hp, Geo.Cyl(0.25f, 0.25f, 0.6f, 8), steelM, 0, 3.3f, 0);
                for (int k = 0; k < 4; k++) Mk.Msh(hp, Geo.Box(0.3f, 0.06f, 5 - k), steelM, 0, 3.6f, 0, 0, k * 1.6f, (k - 1.5f) * 0.15f);
                foreach (var r in hp.t.GetComponentsInChildren<Renderer>()) r.gameObject.layer = Mk.LayerIgnore;
                SolidAABB(V(-40, 0, 6), V(-32, 3, 14)); var fg = Mk.Glow(levelRoot, 0xff6010, 4, 0.5f); fg.transform.localPosition = V(-36, 2, 10); flames.Add(fg.transform);
                anims.Add((t, dt) => { if (rand < 0.4f) SpawnParticles(V(-36 + (rand - 0.5f) * 3, 3, 10 + (rand - 0.5f) * 3), 1, 1.5f, rand < 0.7f ? ashMat : emberMat, 0.25f, 3, -3); });
            }
            PadAt(-52, -8, 22); PadAt(52, 8, 22); PadAt(-8, 52, 22); PadAt(8, -52, 22);
            OverpassExtras(DY); // the wrecking cranes and the falling debris (GameDeadEarth2)
        }
        // ---------- RUNAWAY TRUCKS: headlights flare at the end of the deck and the horn blares; a burning truck races the
        // length of it and flies off the broken end (or the deck's far end) to crash where the circle glows ----------
        void MechRunawayTrucks((float z, float dir)[] decks, float deckY)
        {
            for (int i = 0; i < decks.Length; i++)
            {
                var (zc, dir) = decks[i]; float off = i * 6f, startX = -dir * 66, launchX = dir > 0 ? 38 : -66 + 2; bool broken = dir > 0;
                float lane = zc + (dir > 0 ? 2.8f : -2.8f) * Mathf.Sign(zc);
                var crash = broken ? V(46, 0, zc) : V(launchX - 10, 0, zc - Mathf.Sign(zc) * 8);
                if (!broken) { launchX = -40; crash = V(-48, 0, zc * 0.45f); } // the south deck: it veers off the side near the west end
                var truck = Mk.Piv(levelRoot, startX, deckY, lane);
                Vehicle(0, 0, dir > 0 ? Mathf.PI / 2 : -Mathf.PI / 2, 4, true, 0, truck.t);
                var tf = Mk.Glow(truck.t, 0xff6010, 6, 0.6f); tf.transform.localPosition = V(0, 2, 0);
                foreach (var r in truck.t.GetComponentsInChildren<Renderer>()) r.gameObject.layer = Mk.LayerIgnore;
                truck.t.gameObject.SetActive(false);
                var laneM = HazMat(0xff4010, 0, false); var lw = Flat(Geo.Plane(Mathf.Abs(launchX - startX), 4.4f), laneM, (launchX + startX) / 2, deckY + 0.05f, lane);
                var cm = HazMat(0xff3010, 0, false); var cw = Flat(Geo.Circle(6, 32), cm, crash.x, 0.06f, crash.z); var crm = HazMat(0xffd0a0, 0, true); Flat(Geo.Ring(5.7f, 6, 40), crm, crash.x, 0.07f, crash.z);
                var head = Mk.Glow(levelRoot, 0xfff0c0, 8, 0); head.transform.localPosition = V(startX + dir * 2, deckY + 1.5f, lane); var headM = head.GetComponent<Renderer>().sharedMaterial;
                bool hitP = false, crashed = false; Vector3 fv = Vector3.zero, fp = Vector3.zero;
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 12f; bool warn = u >= 6 && u < 7.6f, run = u >= 7.6f;
                    Mats.SetAlpha(laneM, warn ? 0.1f + 0.25f * (Mathf.Sin(u * 20) > 0 ? 1 : 0.3f) : run ? 0.15f : 0); Mats.SetAlpha(headM, warn ? (u - 6) / 1.6f : 0);
                    Mats.SetAlpha(cm, warn || run ? 0.12f + 0.25f * Mathf.Abs(Mathf.Sin(u * 8)) : 0); Mats.SetAlpha(crm, warn || run ? 0.6f : 0);
                    if (u >= 6 && u - dt < 6 && HazardLive) { Sfx("alarm", 0.5f); Sfx("charge", 0.4f); }
                    if (!run) { truck.t.gameObject.SetActive(false); hitP = false; crashed = false; return; }
                    truck.t.gameObject.SetActive(!crashed);
                    float k = u - 7.6f, x = startX + dir * k * 38;
                    if (dir * (x - launchX) < 0) { truck.t.localPosition = V(x, deckY, lane); fp = truck.t.localPosition; float tFly = 1.1f; fv = V((crash.x - launchX) / tFly, 0.5f * GRAV * tFly - deckY / tFly, (crash.z - lane) / tFly); if (rand < 0.8f) SpawnParticles(fp + V(0, 2.5f, 0), 1, 2, rand < 0.6f ? ashMat : emberMat, 0.25f, 1, -2); }
                    else if (!crashed)
                    {
                        fv.y -= GRAV * dt; fp += fv * dt; truck.t.localPosition = fp; truck.t.Rotate(V(0, 0, dir * 90) * dt, Space.Self);
                        if (fp.y <= 0.3f)
                        {
                            crashed = true; truck.t.gameObject.SetActive(false);
                            Blast(crash + V(0, 2, 0), 9, 0xff6010); Blast(crash + V(0, 2, 0), 4, 0xffffff); ShockRing(crash, 9, 0xff8020, 0.5f); FirePillar(crash, 3, 14, 0xff6010, 1);
                            SpawnParticles(crash + V(0, 1, 0), 50, 16, emberMat, 0.14f, 1.4f); SpawnParticles(crash + V(0, 1, 0), 30, 7, ashMat, 0.4f, 2.5f, -2); for (int j = 0; j < 8; j++) particles.Add(new Pt { pos = crash + V(0, 1, 0), scale = Vector3.one * (0.4f + rand * 0.5f), mat = burntM, v = V((rand - 0.5f) * 18, 6 + rand * 8, (rand - 0.5f) * 18), life = 2.5f, grav = GRAV, hasSpin = true, spin = V(rand * 10, rand * 10, rand * 10) });
                            Sfx("boom"); Sfx("slam"); P.shake = Mathf.Max(P.shake, 0.8f);
                            if (HazardLive && Hypot(P.pos.x - crash.x, P.pos.z - crash.z) < 6 && P.pos.y < 4) { var o = P.pos - crash; o.y = 0; HazardHitPlayer(32, crash, o.normalized * 16 + V(0, 10, 0)); }
                            HazardHitEnemies(e => Hypot(e.pos.x - crash.x, e.pos.z - crash.z) < 6 && e.pos.y < 4, 30, e => { var o = e.pos - crash; o.y = 0; return o.normalized * 15 + V(0, 9, 0); }, 1);
                        }
                    }
                    if (!HazardLive || crashed) return;
                    var tp = truck.t.position;
                    if (!hitP && Mathf.Abs(P.pos.x - tp.x) < 7.5f && Mathf.Abs(P.pos.z - tp.z) < 2.2f && Mathf.Abs(P.pos.y - tp.y) < 3.5f) { hitP = true; HazardHitPlayer(30, tp, V(0, 12, Mathf.Sign(P.pos.z - tp.z + 0.01f) * 18)); }
                    HazardHitEnemies(e => Mathf.Abs(e.pos.x - tp.x) < 7.5f && Mathf.Abs(e.pos.z - tp.z) < 2.4f && Mathf.Abs(e.pos.y - tp.y) < 3.5f, 30, e => V(0, 10, Mathf.Sign(e.pos.z - tp.z + 0.01f) * 16), 1);
                });
            }
        }

        // ================= 14. THE CATHEDRAL OF RUST =================
        void BuildCathedral()
        {
            DeadMats(); Arena(62, 26);
            var iron = Mats.Std(0x3a3430, 0.5f, 0.7f); var hot = Mats.Basic(0xff7020); var moltenM = Mats.Basic(0xffffff, Tex.Lava);
            // the altar: the great furnace, its mouth glowing, a crucible on chains above it, the organ of pipes behind
            Box(0, 0, 0, 12, 5, 12, M.tower); TrimRect(0, 0, 12, 12, 5);
            Deco(Geo.Cyl(4, 5, 10, 16), rustM, 0, 10, 0); Deco(Geo.Cyl(4.2f, 4.2f, 0.6f, 16), iron, 0, 12, 0); Deco(Geo.Cyl(4.3f, 4.3f, 0.6f, 16), iron, 0, 7.5f, 0);
            Deco(Geo.Box(3, 2.4f, 0.4f), hot, 0, 7, 5.05f); var core = Mk.Glow(levelRoot, 0xff6010, 10, 0.45f); core.transform.localPosition = V(0, 8, 0); anims.Add((t, dt) => core.transform.localScale = Vector3.one * (10 + Mathf.Sin(t * 2.3f) * 1.5f));
            SolidAABB(V(-5, 5, -5), V(5, 15, 5));
            Deco(Geo.Cyl(1.4f, 1.4f, 14, 10), rustM, -3, 22, 2); Deco(Geo.Cyl(1.2f, 1.2f, 12, 10), rustM, 3, 21, -2);
            for (int k = 0; k < 15; k++) { float x = (k - 7) * 2.6f, h = 10 + (7 - Mathf.Abs(k - 7)) * 2.4f; Deco(Geo.Cyl(0.8f, 0.9f, h, 12), k % 2 == 0 ? rustM : steelM, x, h / 2, -58); Deco(Geo.Cone(0.8f, 1.2f, 12), iron, x, h + 0.6f, -58); Deco(Geo.Box(0.5f, 0.2f, 0.1f), Mats.Basic(0x200800), x, h * 0.25f, -57.15f); } // the organ of pipes
            SolidAABB(V(-20, 0, -60), V(20, 28, -57));
            var gp = Mk.Piv(levelRoot, 0, 20, -60.5f); GearDeco(gp.t, 9, 18, rustM, out var hub); Mk.Msh(gp, Geo.Circle(7.5f, 32), Mats.Add(0xff7020, 0.4f, null, true, true), 0, 0, 0.35f, 0, Mathf.PI, 0); anims.Add((t, dt) => hub.localRotation = Quaternion.Euler(0, 0, t * 6)); // the rose window: a great gear lit from behind
            // the nave: pointed arches of riveted girders marching down both sides, a clerestory of glowing windows above
            foreach (var sx in new[] { -1f, 1f })
            {
                for (int k = 0; k < 6; k++)
                {
                    float z = -45 + k * 18;
                    Box(sx * 34, 0, z, 2, 20, 2, iron); Deco(Geo.Box(2.4f, 0.6f, 2.4f), rustM, sx * 34, 20, z);
                    var arch = Mk.Piv(levelRoot, sx * 34, 20, z); arch.SetRot(0, 0, -sx * 0.55f); Mk.Msh(arch, Geo.Box(1.2f, 22, 1.2f), iron, 0, 11, 0); // the rib rising inward
                    foreach (var r in arch.t.GetComponentsInChildren<Renderer>()) r.gameObject.layer = Mk.LayerIgnore;
                    for (int j = 0; j < 4; j++) Ig(Deco(Geo.Sphere(0.18f, 6, 4), rustM, sx * 34.95f, 3 + j * 4, z));
                    if (k < 5) { Ig(Deco(Geo.Box(0.1f, 9, 5), Mats.Add(0xff7a30, 0.45f, null, true, true), sx * 61.8f, 16, z + 9)); for (int j = 0; j < 3; j++) Ig(Deco(Geo.Box(0.15f, 9, 0.2f), iron, sx * 61.7f, 16, z + 9 + (j - 1) * 1.7f)); }
                }
                Deco(Geo.Box(0.8f, 0.8f, 112), iron, sx * 34, 20.4f, 0);
                // the side aisles: catwalks with railings and stairs, slag buckets on chains, a molten channel in the floor
                Box(sx * 50, 8, 0, 8, 1, 100, steelM); TrimRect(sx * 50, 0, 8, 100, 9);
                Deco(Geo.Box(0.08f, 1.0f, 100), iron, sx * 46.1f, 9.5f, 0); SolidAABB(V(sx * 46.1f - 0.1f, 9, -50), V(sx * 46.1f + 0.1f, 10, 50));
                for (int z = -40; z <= 40; z += 20) Box(sx * 50, 0, z, 1.2f, 8, 1.2f, M.metal);
                Stairs(sx * 50, -56, 4, V(0, 0, 1), 15, 0, 9, 0.4f, steelM);
                Flat(Geo.Plane(2, 90), moltenM, sx * 41, 0.04f, 0); pools.Add(new Pool { x = sx * 41, z = 0, w = 2.0f, d = 90 }); /* the molten channel burns */ var mg = Mk.Glow(levelRoot, 0xff5010, 30, 0.15f); mg.transform.localPosition = V(sx * 41, 0.5f, 0);
                foreach (var e in new[] { -1f, 1f }) Ig(Deco(Geo.Box(0.3f, 0.3f, 90), iron, sx * 41 + e * 1.15f, 0.15f, 0));
                for (int k = 0; k < 3; k++) { float z = -30 + k * 30; HangingChain(sx * 26, z, 30, 12); var bk = Deco(Geo.Cyl(1.2f, 0.9f, 1.8f, 12, true), iron, sx * 26, 11, z); var bg = Mk.Glow(levelRoot, 0xff6010, 2.5f, 0.6f); bg.transform.localPosition = V(sx * 26, 11.6f, z); }
                PadAt(sx * 46, 30, 24); PadAt(sx * 46, -30, 24);
                for (int k = 0; k < 3; k++) { Deco(Geo.Cyl(0.6f, 0.6f, 20, 10), rustM, sx * (28 + k * 3), 10, -58.5f); }
            }
            // the conveyor loop: ore comes in at the east dock, runs round the furnace and back out - one connected belt
            Box(56, 0, 24, 8, 2.4f, 10, steelM); Ig(Deco(Geo.Box(8.2f, 0.1f, 10.2f), laneYellowM, 56, 2.42f, 24)); // the loading dock
            var loop = new (float x, float z, float len, bool alongX, int dir)[] { (12, 24, 40, true, -1), (-16, 0, 52, false, -1), (12, -24, 40, true, 1), (36, 0, 52, false, 1) };
            MechBelts(loop);
            // the stamping presses straddle the belt: the belt carries you into them
            MechPresses(new[] { V(4, 0, 24), V(22, 0, 24), V(-16, 0, 10), V(-16, 0, -12), V(4, 0, -24), V(22, 0, -24), V(36, 0, -8), V(36, 0, 12) });
            // crates of ore along the dock, slag heaps, a wrecked crane on its rails, industrial lamps
            foreach (var p in new[] { V(52, 2.4f, 21), V(54, 2.4f, 27), V(58, 2.4f, 22) }) Box(p.x, p.y, p.z, 1.6f, 1.6f, 1.6f, rustM);
            foreach (var p in new[] { V(-50, 0, 46), V(48, 0, -46), V(-24, 0, 46), V(24, 0, 46) }) { var g = Deco(Geo.Sphere(1, 10, 6), Mats.Std(0x2a2420, 0.95f), p.x, 0, p.z); g.transform.localScale = V(3, 1.6f, 2.6f); SolidAABB(V(p.x - 2.4f, 0, p.z - 2), V(p.x + 2.4f, 1.4f, p.z + 2)); }
            Box(0, 24, 0, 2, 1.4f, 120, iron); Deco(Geo.Box(70, 1.2f, 2), iron, 0, 23, 20); HangingChain(0, 20, 22.4f, 16); Deco(Geo.Box(1.2f, 0.8f, 1.2f), iron, 0, 15.6f, 20); // the gantry crane overhead
            for (int k = 0; k < 8; k++) { float z = -42 + k * 12; foreach (var sx in new[] { -1f, 1f }) { var lg = Mk.Glow(levelRoot, 0xffa050, 3, 0.5f); lg.transform.localPosition = V(sx * 32.8f, 12, z); Ig(Deco(Geo.Cone(0.6f, 0.5f, 10), iron, sx * 32.8f, 12.4f, z)); } }
            Box(0, 0, 40, 20, 2, 4, M.metal); Box(0, 0, -40, 20, 2, 4, M.metal);
            CathedralExtras(); // steam vents, shredders, welding arms, pipes, cables, oil (GameDeadEarth2)
        }
        // ---------- conveyor belts: a loop of segments that meet at the corners; slats and crates ride round, rollers at the turns ----------
        void MechBelts((float x, float z, float len, bool alongX, int dir)[] segs)
        {
            var beltM = Mats.Std(0x1a1816, 0.85f); var slatM = Mats.Std(0x3a3632, 0.7f, 0.4f); var frameM = Mats.Std(0x4a4440, 0.5f, 0.7f); var stripe = Mats.Std(0xd8a020, 0.7f); var crateM = Mats.Std(0x6a3a20, 0.8f, 0.4f);
            const float WID = 5, SPEED = 6;
            foreach (var s in segs)
            {
                float lx = s.alongX ? s.len : WID, lz = s.alongX ? WID : s.len;
                Deco(Geo.Box(lx, 0.16f, lz), beltM, s.x, 0.08f, s.z);
                foreach (var e in new[] { -1f, 1f }) { Deco(Geo.Box(s.alongX ? s.len : 0.3f, 0.42f, s.alongX ? 0.3f : s.len), frameM, s.x + (s.alongX ? 0 : e * (WID / 2 + 0.15f)), 0.21f, s.z + (s.alongX ? e * (WID / 2 + 0.15f) : 0)); Ig(Deco(Geo.Box(s.alongX ? s.len : 0.32f, 0.08f, s.alongX ? 0.32f : s.len), stripe, s.x + (s.alongX ? 0 : e * (WID / 2 + 0.15f)), 0.44f, s.z + (s.alongX ? e * (WID / 2 + 0.15f) : 0))); }
                for (int k = 0; k <= 1; k++) { float u = (k == 0 ? -1 : 1) * s.len / 2; Ig(Deco(Geo.Cyl(0.3f, 0.3f, WID, 12), frameM, s.x + (s.alongX ? u : 0), 0.2f, s.z + (s.alongX ? 0 : u), s.alongX ? Mathf.PI / 2 : 0, 0, s.alongX ? 0 : Mathf.PI / 2)); } // the end rollers
                for (int k = 0; k < (int)(s.len / 10); k++) Ig(Deco(Geo.Cone(0.5f, 0.9f, 3), stripe, s.x + (s.alongX ? (k - 1.5f) * 10 : 0), 0.18f, s.z + (s.alongX ? 0 : (k - 1.5f) * 10), Mathf.PI / 2, s.alongX ? (s.dir > 0 ? Mathf.PI / 2 : -Mathf.PI / 2) : (s.dir > 0 ? 0 : Mathf.PI), 0)); // arrows painted on: which way it runs
                var slats = new List<Transform>(); int n = (int)(s.len / 2.2f);
                for (int k = 0; k < n; k++) { var sl = Ig(Deco(Geo.Box(s.alongX ? 0.35f : WID - 0.3f, 0.05f, s.alongX ? WID - 0.3f : 0.35f), slatM, s.x, 0.18f, s.z)); slats.Add(sl.transform); }
                var crates = new List<Transform>(); for (int k = 0; k < 2; k++) { var c = Ig(Deco(Geo.Box(1.4f, 1.1f, 1.4f), crateM, s.x, 0.72f, s.z, 0, k, 0)); crates.Add(c.transform); }
                var dirV = s.alongX ? V(s.dir, 0, 0) : V(0, 0, s.dir);
                trapAnims.Add((t, dt) =>
                {
                    for (int k = 0; k < slats.Count; k++) { float u = Mathf.Repeat(k * 2.2f + t * SPEED * s.dir, s.len) - s.len / 2; slats[k].localPosition = V(s.x + (s.alongX ? u : 0), 0.18f, s.z + (s.alongX ? 0 : u)); }
                    for (int k = 0; k < crates.Count; k++) { float u = Mathf.Repeat(k * s.len / 2 + t * SPEED * s.dir, s.len) - s.len / 2; crates[k].localPosition = V(s.x + (s.alongX ? u : 0), 0.72f, s.z + (s.alongX ? 0 : u)); }
                    if (!HazardLive) return;
                    if (P.onGround && P.pos.y < 0.6f && Mathf.Abs(P.pos.x - s.x) < lx / 2 && Mathf.Abs(P.pos.z - s.z) < lz / 2) P.pos += dirV * SPEED * dt;
                    foreach (var e in enemies) if (!e.dead && e.type == "husk" && e.ground && e.pos.y < 0.6f && Mathf.Abs(e.pos.x - s.x) < lx / 2 && Mathf.Abs(e.pos.z - s.z) < lz / 2) e.pos += dirV * SPEED * dt;
                });
            }
        }
        // ---------- STAMPING PRESSES: a steel frame standing on the floor over the belt, a hydraulic ram; it hisses, the ram
        // rises, then it stamps down onto the bed ----------
        void MechPresses(Vector3[] spots)
        {
            var frameM = Mats.Std(0x3a3632, 0.5f, 0.75f); var ramM = Mats.Std(0x5a5450, 0.4f, 0.8f); var rodM = Mats.Std(0x9a9a9e, 0.2f, 0.9f); var stripe = Mats.Std(0xd8a020, 0.7f); var hot = Mats.Basic(0xff6020);
            const float TOP = 9.5f, SZ = 4.4f, REST = 5.0f;
            for (int i = 0; i < spots.Length; i++)
            {
                var c = spots[i]; float off = i * 0.83f;
                // the frame: four columns on footings, a crown beam with the hydraulic cylinder, hazard stripes, a warning lamp
                foreach (var a in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) { Box(c.x + a * (SZ / 2 + 0.9f), 0, c.z + b * (SZ / 2 + 0.9f), 0.8f, TOP + 1, 0.8f, frameM); Ig(Deco(Geo.Box(1.3f, 0.4f, 1.3f), frameM, c.x + a * (SZ / 2 + 0.9f), 0.2f, c.z + b * (SZ / 2 + 0.9f))); }
                Box(c.x, TOP + 1, c.z, SZ + 2.6f, 1.2f, SZ + 2.6f, frameM); Deco(Geo.Cyl(1.0f, 1.0f, 2.2f, 14), ramM, c.x, TOP + 3.3f, c.z);
                for (int k = 0; k < 4; k++) Ig(Deco(Geo.Box(SZ + 2.7f, 0.2f, 0.1f), k % 2 == 0 ? stripe : frameM, c.x, TOP + 1.3f + k * 0.25f, c.z + SZ / 2 + 1.36f));
                var lampM = Mats.Add(0xff4010, 0, null, true, true); var lamp = Ig(Deco(Geo.Sphere(0.25f, 8, 6), lampM, c.x + SZ / 2 + 1.3f, TOP + 2.4f, c.z));
                var hd = Mk.Piv(levelRoot, c.x, REST, c.z);
                Mk.Msh(hd, Geo.Box(SZ, 1.4f, SZ), ramM, 0, 0.7f, 0); Mk.Msh(hd, Geo.Box(SZ + 0.05f, 0.25f, SZ + 0.05f), stripe, 0, 0.12f, 0);
                for (int k = 0; k < 4; k++) Mk.Msh(hd, Geo.Box(0.3f, 0.3f, SZ), frameM, (k - 1.5f) * 1.1f, 1.55f, 0);
                var rod = Mk.Msh(hd, Geo.Cyl(0.45f, 0.45f, 1, 12), rodM, 0, 0, 0);
                foreach (var r in hd.t.GetComponentsInChildren<Renderer>()) r.gameObject.layer = Mk.LayerIgnore;
                var shM = HazMat(0xff2000, 0, false); Flat(Geo.Plane(SZ, SZ), shM, c.x, 0.2f, c.z);
                bool hit = false; var hitE = new HashSet<Ent>();
                trapAnims.Add((t, dt) =>
                {
                    float u = (t + off) % 6.5f, y = REST;
                    if (u >= 3 && u < 4.3f) { y = REST + 2.6f * Smoother((u - 3) / 1.3f); if (rand < 0.6f) SpawnParticles(V(c.x + (rand - 0.5f) * 1.6f, TOP + 1, c.z + (rand - 0.5f) * 1.6f), 1, 2, steamMat, 0.25f, 0.8f, -1); }
                    else if (u >= 4.3f && u < 4.42f) y = Mathf.Lerp(REST + 2.6f, 0.2f, (u - 4.3f) / 0.12f);
                    else if (u >= 4.42f && u < 5.0f) y = 0.2f;
                    else if (u >= 5.0f) y = Mathf.Lerp(0.2f, REST, Smoother((u - 5) / 1.5f));
                    hd.t.localPosition = V(c.x, y, c.z);
                    float rl = TOP + 1 - (y + 1.4f); rod.transform.localScale = V(1, Mathf.Max(0.1f, rl), 1); rod.transform.localPosition = V(0, 1.4f + rl / 2, 0);
                    Mats.SetAlpha(shM, u >= 3 && u < 5 ? 0.15f + 0.35f * Mathf.Clamp01((u - 3) / 1.3f) * (Mathf.Sin(u * 22) > 0 ? 1 : 0.5f) : 0);
                    Mats.SetAlpha(lampM, u >= 3 && u < 5 ? (Mathf.Sin(u * 18) > 0 ? 0.9f : 0.2f) : 0.05f);
                    if (u >= 3 && u - dt < 3 && HazardLive && Hypot(P.pos.x - c.x, P.pos.z - c.z) < 25) Sfx("charge", 0.45f);
                    if (u >= 4.42f && u - dt < 4.42f) { Sfx("slam"); P.shake = Mathf.Max(P.shake, 0.35f); SpawnParticles(c + V(0, 0.4f, 0), 18, 7, dustMat, 0.25f, 0.8f); SpawnParticles(c + V(0, 0.4f, 0), 14, 9, sparkMat, 0.06f, 0.4f); }
                    if (y > 2.0f) { hit = false; hitE.Clear(); return; }
                    if (!HazardLive) return;
                    if (!hit && Mathf.Abs(P.pos.x - c.x) < SZ / 2 + 0.3f && Mathf.Abs(P.pos.z - c.z) < SZ / 2 + 0.3f && P.pos.y < y + 0.6f) { hit = true; var o = P.pos - c; o.y = 0; HazardHitPlayer(40, c, (o.sqrMagnitude > 0.01f ? o.normalized : Vector3.forward) * 14 + V(0, 10, 0)); }
                    HazardHitEnemies(e => Mathf.Abs(e.pos.x - c.x) < SZ / 2 + 0.3f && Mathf.Abs(e.pos.z - c.z) < SZ / 2 + 0.3f && e.pos.y < 2 && hitE.Add(e), 40, e => V(0, 4, 0), 1);
                });
            }
        }
    }
}
