using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BR
{
    // THE OPENING: plays once when a new run starts from the menu, before the first wave. ~45 s.
    //   darkness and sound alone: a rumble far off, whispers, metal creaking, a distortion as if reality were tearing
    //   the ancient world, revealed slowly - a flight between white towers at dawn, then the wide view: colossal pillars, a
    //   great arch, a kingdom floating in the sky, four haloed guardians round a radiant sun
    //   the disturbance: the sky cracks, lightning, a great tower breaks and falls, debris raining down
    //   the Void: a black mass swallows the sun and the floating kingdom, the halos turn red, the land tears into realms
    //   the ruins: a burning, broken city - then silence and darkness
    //   four silhouettes, one at a time, each out of and back into darkness: the real boss models, every surface flat
    //   black, every glow off, against DIM backgrounds - only a flicker of light (lightning in a blizzard, fire through
    //   smoke, a slow swell of light, the beat of a clock) ever shows their outline, and only just
    //   darkness, three lines of narration, the title
    // It is filmed by the cutscene camera on its own layer, far from the level; the game itself does not run underneath it
    // (nobody moves, shoots or takes damage). Only the host (or a solo player) has the SKIP button; co-op
    // (GameCoopIntro.cs): the host starts it for everyone, keeps everyone in step, a late joiner picks it up where it is,
    // and the host's skip ends it for everyone.
    public partial class Game
    {
        class IntroState { public bool active, done; public float t, fadeOut, shake; public int scene = -1; }
        readonly IntroState INTRO = new IntroState();
        partial void IntroMayStart(ref bool ok);
        partial void IntroMaySkip(ref bool ok);
        partial void IntroNetStart();
        partial void IntroNetTick(float rdt);
        partial void IntroNetEnd();

        // the timeline (seconds)
        const float IN_A = 3.0f, IN_A2 = 6.4f, IN_B = 10.0f, IN_B2 = 13.4f, IN_C = 17.4f, IN_C2 = 20.0f, IN_D = 21.0f, IN_BOSS = 3.0f,
            IN_DE = IN_D + 5 * IN_BOSS, IN_N = IN_DE + 1.0f, IN_LINE = 2.6f, IN_T = IN_N + 3 * IN_LINE, IN_END = IN_T + 3.6f;

        Transform introRoot; readonly List<Transform> iSets = new List<Transform>(); readonly List<Material> iMats = new List<Material>(); readonly List<Texture2D> iTex = new List<Texture2D>();
        class IPart { public Transform t; public Vector3 v, home, box; }
        readonly List<IPart>[] iParts = new List<IPart>[8];
        class IDeb { public Transform t; public Vector3 v, spin; public float life; }
        readonly List<IDeb> iDebris = new List<IDeb>();
        // the world set
        readonly List<(Transform t, Vector3 home, Vector3 dir, float sink, Vector3 rot)> iSlabs = new List<(Transform, Vector3, Vector3, float, Vector3)>();
        readonly List<Material> iHalos = new List<Material>(); readonly List<Transform> iTendrils = new List<Transform>();
        readonly List<List<GameObject>> iCracks = new List<List<GameObject>>(); readonly List<GameObject> iBolts = new List<GameObject>();
        Material iSkyM, iSunGlowM, iGuardM, iCoronaM, iRiseSmokeM, iDebM, iBoltM; Transform iVoid, iCorona, iKingdom, iTower; Vector3 iKingdomHome;
        // the silhouettes
        Transform iRays, iSerHalo; Material iSerHaloM;
        readonly List<(Transform t, float sp)> iGears = new List<(Transform, float)>();
        readonly List<(Transform t, Vector3 axis, float sp)> iSpins = new List<(Transform, Vector3, float)>();
        // (index = the boss: 0 Traitor, 1 Hollow King, 2 Seraph, 3 Warden, 4 Carnifex; ISIL is the order they come out of the dark)
        readonly Transform[] iBoss = new Transform[5]; readonly float[] iBossH = new float[5];
        readonly Material[] iBack = new Material[5], iBloomM = new Material[5], iMistM = new Material[5];
        static readonly int[] ISIL = { 4, 0, 1, 2, 3 };
        static readonly Vector3[] IO = { new Vector3(20000, 0, 0), new Vector3(23000, 0, 0), new Vector3(26000, 0, 0), new Vector3(29000, 0, 0), new Vector3(32000, 0, 0), new Vector3(35000, 0, 0), new Vector3(38000, 0, 0) };
        // the overlay
        RectTransform introUI, iSkipRT; RawImage iFade, iFlash; Text iNarrT, iTitleT, iSkipT; RectTransform iBarTop, iBarBot, iTitleRT, iRule;
        bool iCanSkip; float iFlashA;
        readonly HashSet<string> iCues = new HashSet<string>();

        // ================= start / end =================
        void StartIntro(float at)
        {
            if (INTRO.active || INTRO.done) return;
            INTRO.active = true; INTRO.done = true; INTRO.t = at; INTRO.scene = -1; INTRO.fadeOut = 0; iCues.Clear(); iFlashA = 0;
            iCanSkip = true; IntroMaySkip(ref iCanSkip);
            BuildIntro(); IntroOverlay(); IArchLight(true);
            ReleaseHook(); mouseDown = false; sawHeld = false; laserHeld = false; audioMgr.Hum(0); audioMgr.Stop(0.4f);
            SetHudVisible(false); if (vm) vm.gameObject.SetActive(false); ShowScreen(null);
            locked = true;
            // the host gets a pointer for the SKIP button; everyone else just watches
            if (iCanSkip) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; } else { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            cineCam.cullingMask = 1 << CINE_LAYER; cineCam.clearFlags = CameraClearFlags.SolidColor; cineCam.backgroundColor = Color.black; cineCam.fieldOfView = 46;
            IntroNetStart();
        }
        void EndIntro()
        {
            if (!INTRO.active) return;
            INTRO.active = false; INTRO.fadeOut = 1;
            if (introRoot) Mk.DestroyTree(introRoot.gameObject); introRoot = null;
            foreach (var m in iMats) if (m) Destroy(m); iMats.Clear(); foreach (var t in iTex) if (t) Destroy(t); iTex.Clear();
            iSets.Clear(); iSlabs.Clear(); iHalos.Clear(); iTendrils.Clear(); iGears.Clear(); iSpins.Clear(); iCracks.Clear(); iBolts.Clear(); iDebris.Clear();
            for (int i = 0; i < iParts.Length; i++) iParts[i] = null;
            IArchLight(false); iFalls.Clear();
            RenderSettings.fog = true; ApplyTheme(THEME);
            cineCam.enabled = false; cam.enabled = true; cineCam.fieldOfView = 50;
            SetHudVisible(true); if (vm) vm.gameObject.SetActive(true);
            iNarrT.text = iTitleT.text = ""; iFlash.color = new Color(1, 1, 1, 0); iSkipRT.gameObject.SetActive(false);
            iBarTop.gameObject.SetActive(false); iBarBot.gameObject.SetActive(false); iRule.gameObject.SetActive(false);
            if (started)
            {
                if (LEVEL != null) Banner($"LEVEL {levelNum}", LEVEL.name, THEME.banner);
                locked = true; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; mouseDown = false;
            }
            skipDelta = true; IntroNetEnd();
        }
        // called each frame from Update: true while the opening owns the screen
        bool UpdateIntro(float rdt)
        {
            if (!INTRO.active)
            {
                INTRO.fadeOut = Mathf.Max(0, INTRO.fadeOut - rdt * 1.2f); if (iFade) iFade.color = new Color(0, 0, 0, INTRO.fadeOut * INTRO.fadeOut); // the level fades up out of the black
                return false;
            }
            if (iCanSkip && (KeyDown(UnityEngine.InputSystem.Key.Enter) || KeyDown(UnityEngine.InputSystem.Key.Escape))) { IntroSkip(); return false; }
            INTRO.t += rdt; float t = INTRO.t;
            if (testMode && float.TryParse(Arg("-brintroskip") ?? "", System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var tsk) && t >= tsk) { IntroSkip(); if (!INTRO.active) return false; } // (test: the host presses SKIP)
            IntroNetTick(rdt); if (!INTRO.active) return false;
            if (t >= IN_END) { EndIntro(); return false; }
            int sc = t < IN_A ? 0 : t < IN_A2 ? 1 : t < IN_B ? 2 : t < IN_B2 ? 3 : t < IN_C ? 4 : t < IN_C2 ? 5 : t < IN_D ? 6
                   : t < IN_DE ? 7 + Mathf.Min(4, (int)((t - IN_D) / IN_BOSS)) : t < IN_N ? 12 : t < IN_T ? 13 : 14;
            if (sc != INTRO.scene) { INTRO.scene = sc; IntroScene(sc); }
            IntroSound(t);
            IntroAnimate(sc, t, rdt);
            IntroText(t, rdt);
            if (iCanSkip && !Cursor.visible) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            cam.enabled = false; cineCam.enabled = true;
            return true;
        }
        void IntroSkip() { if (!INTRO.active || !iCanSkip) return; Sfx("ui"); EndIntro(); } // (co-op: ends it for everyone)

        // ================= building =================
        Material IM(Material m) { iMats.Add(m); return m; }
        Material INF(Material m) { m.SetFloat("_UseFog", 0); return IM(m); } // (the sky and the backdrops stay clear of the fog)
        Texture2D IGrad(int top, int bottom)
        {
            var tx = new Texture2D(2, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; iTex.Add(tx);
            Color a = Mats.Hex(top), b = Mats.Hex(bottom);
            for (int y = 0; y < 128; y++) { var c = Color.Lerp(b, a, Mathf.Pow(y / 127f, 0.8f)); tx.SetPixel(0, y, c); tx.SetPixel(1, y, c); }
            tx.Apply(); return tx;
        }
        Texture2D IRadial(int core, int mid, int edge)
        {
            var tx = new Texture2D(128, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; iTex.Add(tx);
            Color a = Mats.Hex(core), b = Mats.Hex(mid), c = Mats.Hex(edge);
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float r = Mathf.Sqrt((x - 63.5f) * (x - 63.5f) + (y - 63.5f) * (y - 63.5f)) / 64f;
                tx.SetPixel(x, y, r < 0.28f ? Color.Lerp(a, b, r / 0.28f) : Color.Lerp(b, c, Mathf.Pow(Mathf.Clamp01((r - 0.28f) / 0.72f), 0.7f)));
            }
            tx.Apply(); return tx;
        }
        Texture2D iSoftTex;
        Texture2D ISoft()
        {
            if (iSoftTex) return iSoftTex;
            var tx = new Texture2D(64, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; iTex.Add(tx);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float r = Mathf.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f)) / 32f;
                float n = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.5f + 0.5f;
                tx.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1 - r) * 1.6f) * Mathf.Clamp01(1 - r) * n));
            }
            tx.Apply(); return iSoftTex = tx;
        }
        Material ISmoke(int color, float alpha) { var m = IM(Mats.Trans(color, alpha, true, false)); m.SetTexture("_BaseMap", ISoft()); m.mainTexture = ISoft(); return m; }
        GameObject IBill(Transform p, Vector3 pos, float w, float h, Material m)
        {
            var g = new GameObject("ib"); g.transform.SetParent(p, false); g.transform.position = pos; g.transform.localScale = V(w, h, 1);
            g.AddComponent<MeshFilter>().sharedMesh = Geo.Plane(1, 1); var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; Mk.billboards.Add(g.transform); return g;
        }
        GameObject IMsh(Transform p, Mesh mesh, Material m, Vector3 pos, Vector3 rot = default, Vector3? scale = null)
        {
            var g = new GameObject("im"); g.transform.SetParent(p, false); g.transform.position = pos; g.transform.rotation = Quaternion.Euler(rot); if (scale.HasValue) g.transform.localScale = scale.Value;
            g.AddComponent<MeshFilter>().sharedMesh = mesh; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; return g;
        }
        GameObject IGlow(Transform p, Vector3 pos, int color, float size, float op) { var g = Mk.Glow(p, color, size, op, false); g.transform.position = pos; iMats.Add(g.GetComponent<Renderer>().sharedMaterial); return g; }
        // a soft round glow from stacked faint discs (no texture edge to show)
        Material IDiscGlow(Transform p, Vector3 pos, int color, float radius, float perDisc, int n = 18)
        {
            var m = IM(Mats.Add(color, perDisc, null, false));
            for (int i = 0; i < n; i++) { var d = new GameObject("bloom"); d.transform.SetParent(p, false); d.transform.position = pos + V(0, 0, i * 0.05f); d.transform.localScale = Vector3.one * radius * (1 - i * (0.85f / n)); d.AddComponent<MeshFilter>().sharedMesh = Geo.Circle(1, 48); var mr = d.AddComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; Mk.billboards.Add(d.transform); }
            return m;
        }
        Transform ISet(int k) { var s = new GameObject("introSet" + k).transform; s.SetParent(introRoot, false); s.position = IO[Mathf.Min(k, IO.Length - 1)]; iSets.Add(s); return s; }
        void IDrift(int set, Transform t, Vector3 v, Vector3 home, Vector3 box) { (iParts[set] ??= new List<IPart>()).Add(new IPart { t = t, v = v, home = home, box = box }); }
        // a jagged line of boxes (a crack across the sky, a lightning bolt)
        List<GameObject> IJag(Transform p, Vector3 from, Vector3 dir, int segs, float segLen, float width, Material m, System.Random rng, float jag)
        {
            var list = new List<GameObject>(); var a = from; float ang = Mathf.Atan2(dir.y, dir.x);
            for (int i = 0; i < segs; i++)
            {
                ang += ((float)rng.NextDouble() - 0.5f) * jag; float len = segLen * (0.6f + (float)rng.NextDouble() * 0.8f);
                var b = a + V(Mathf.Cos(ang), Mathf.Sin(ang), 0) * len;
                var g = IMsh(p, Geo.Box(1, 1, 1), m, (a + b) / 2, V(0, 0, ang * Mathf.Rad2Deg)); g.transform.localScale = V(len, width * (1 - i * 0.6f / segs), 1); list.Add(g);
                a = b;
            }
            return list;
        }
        void IDebrisAt(Transform p, Vector3 pos, int n, float spd, float size, System.Random rng)
        {
            float R(float lo, float hi) => lo + (hi - lo) * (float)rng.NextDouble();
            for (int i = 0; i < n; i++)
            {
                var g = IMsh(p, Geo.Box(1, 1, 1), iDebM, pos + V(R(-6, 6), R(-6, 6), R(-6, 6)), V(R(0, 360), R(0, 360), R(0, 360)), Vector3.one * R(size * 0.4f, size));
                g.layer = CINE_LAYER; iDebris.Add(new IDeb { t = g.transform, v = V(R(-spd, spd), R(-spd * 0.3f, spd * 0.6f), R(-spd, spd)), spin = V(R(-200, 200), R(-200, 200), R(-200, 200)), life = 6 });
            }
        }

        // the low camera shots fly over the land: no tower may stand in their way
        static bool INearPath(Vector3 p)
        {
            float SegD(Vector3 a, Vector3 b) { var ab = b - a; ab.y = 0; var ap = p - a; ap.y = 0; float k = Mathf.Clamp01(Vector3.Dot(ap, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude)); var q = ap - ab * k; return q.magnitude; }
            return SegD(new Vector3(-60, 0, 170), new Vector3(-34, 0, 104)) < 30 || SegD(new Vector3(10, 0, 40), new Vector3(-4, 0, 52)) < 45 || SegD(new Vector3(-20, 0, 120), new Vector3(10, 0, 330)) < 30;
        }
        void BuildIntro()
        {
            if (introRoot) Mk.DestroyTree(introRoot.gameObject);
            introRoot = new GameObject("intro").transform; introRoot.SetParent(world, false);
            iSets.Clear(); iSlabs.Clear(); iHalos.Clear(); iTendrils.Clear(); iGears.Clear(); iSpins.Clear(); iCracks.Clear(); iBolts.Clear(); iDebris.Clear(); iFalls.Clear(); iSoftTex = null;
            int pl = Mk.layer; bool pc = Mk.colliders; Mk.colliders = false;
            var rng = new System.Random(7);
            float R(float a, float b) => a + (b - a) * (float)rng.NextDouble();

            // ---- 0: the ancient world (and its fall) ----
            {
                var s = ISet(0); var O = s.position;
                iSkyM = INF(Mats.Basic(0xffffff, IGrad(0x141a36, 0xe8a868), true)); IBill(s, O + V(0, 140, -760), 3000, 1100, iSkyM);
                iSunGlowM = IDiscGlow(s, O + V(0, 75, -600), 0xffd890, 260, 0.05f);
                IMsh(s, Geo.Sphere(34, 24, 16), INF(Mats.Basic(0xfff0d0)), O + V(0, 75, -610));
                // the empire: carved marble and stone, lit by its own low sun (GameIntroArch.cs)
                IArchMats();
                var groundM = IM(Mats.Std(0xffffff, 0.9f, 0, 0, IStoneTex(0x8a7a62, 0x5a4c3c, true, 5))); groundM.SetTextureScale("_BaseMap", new Vector2(14, 14));
                var lava = IM(Mats.Basic(0xff3a10));
                IMsh(s, Geo.Box(1600, 1, 1400), lava, O + V(0, -60, -300));
                for (int i = -3; i <= 3; i++) for (int j = 0; j < 5; j++)
                {
                    var c = O + V(i * 152, -12, 60 - j * 152);
                    var slab = IA(s, Geo.Box(150, 24, 150), groundM, c).transform;
                    var dir = V(c.x - O.x, 0, c.z - O.z + 200).normalized; iSlabs.Add((slab, c, dir, R(20, 70), V(R(-8, 8), R(-10, 10), R(-8, 8))));
                }
                Transform SlabOf(Vector3 p) { var best = iSlabs[0].t; float bd = 1e9f; foreach (var sl in iSlabs) { float d = (sl.home.x - p.x) * (sl.home.x - p.x) + (sl.home.z - p.z) * (sl.home.z - p.z); if (d < bd) { bd = d; best = sl.t; } } return best; }
                IBuildEmpire(s, O, rng);
                // colossal columns crowned with guardian statues and halos
                foreach (var sx in new[] { -1f, 1f })
                {
                    var col = IRoot(s, O + V(sx * 330, 0, -540)); IColumn(col, 0, 0, 230, 11); IStatue(col, 0, 230, 0, 62);
                    var hm = IM(Mats.Add(0xffd070, 0.7f, null, false)); iHalos.Add(hm); IMsh(col, Geo.Torus(13, 1.2f, 40, 5), hm, col.position + V(0, 230 + 62 * 0.84f, -8));
                    col.SetParent(SlabOf(col.position), true); IFallReg(col, sx < 0 ? 1.6f : 2.3f, 230, rng);
                }
                // the kingdom floating in the sky: towers and a dome on an upturned crag
                iKingdom = new GameObject("kingdom").transform; iKingdom.SetParent(s, false); iKingdomHome = O + V(-200, 160, -650); iKingdom.position = iKingdomHome;
                IA(iKingdom, Geo.Cone(70, 80, 10), iStoneDk, iKingdomHome + V(0, -40, 0), V(180, 0, 0));
                ISpire(iKingdom, iKingdomHome + V(-28, 0, 4), 72, 3); ISpire(iKingdom, iKingdomHome + V(32, 0, -12), 56, 5); ISpire(iKingdom, iKingdomHome + V(-4, 0, -30), 44, 9); IBasilica(iKingdom, iKingdomHome + V(8, 0, 16), 13);
                // the four guardians: colossal winged statues standing round the sun
                iGuardM = iMarble;
                float[] gx = { -230, -80, 80, 230 };
                for (int k = 0; k < 4; k++)
                {
                    var bp = O + V(gx[k], 0, -360 + Mathf.Abs(gx[k]) * 0.25f);
                    var g = IRoot(s, bp); IA(g, Geo.Box(26, 10, 26), iStone, bp + V(0, 5, 0)); IA(g, Geo.Box(30, 2, 30), iMarble, bp + V(0, 10.5f, 0)); IStatue(g, 0, 11, 0, 92);
                    var hm = IM(Mats.Add(0xffd070, 0.9f, null, false)); iHalos.Add(hm); IMsh(g, Geo.Torus(12, 1.1f, 32, 5), hm, bp + V(0, 11 + 92 * 0.86f, -7));
                    g.SetParent(SlabOf(bp), true);
                }
                // the great tower that breaks and falls (pivoted at its foot)
                iTower = ISpire(s, O + V(70, 0, -120), 160, 7);
                // the sky cracking: jagged black rents with red light along them
                var crackM = INF(Mats.Basic(0x020002)); var crackGlow = IM(Mats.Add(0xff2a18, 0.9f, null, false));
                for (int k = 0; k < 7; k++)
                {
                    var st = O + V(R(-420, 420), R(60, 300), -700); var dir = V(R(-1, 1), R(-1, -0.2f), 0).normalized;
                    var segs = IJag(s, st, dir, 9, R(28, 50), R(3, 6), crackM, rng, 1.1f); var glows = IJag(s, st + V(0, 0, 1.5f), dir, 9, R(28, 50), 1.2f, crackGlow, new System.Random(100 + k), 1.1f);
                    segs.AddRange(glows); foreach (var g in segs) g.SetActive(false); iCracks.Add(segs);
                }
                // lightning
                iBoltM = IM(Mats.Add(0xe0d0ff, 1f, null, false));
                for (int k = 0; k < 4; k++) { var b = new GameObject("bolt"); b.transform.SetParent(s, false); foreach (var g in IJag(b.transform, O + V(R(-350, 350), R(220, 300), -560 + R(-60, 60)), V(R(-0.3f, 0.3f), -1, 0), 10, R(18, 30), 2.2f, iBoltM, rng, 1.3f)) { } b.SetActive(false); iBolts.Add(b); }
                // the Void
                iVoid = IMsh(s, Geo.Sphere(1, 24, 16), INF(Mats.Basic(0x000000)), O + V(0, 75, -590)).transform; iVoid.localScale = Vector3.zero;
                iCoronaM = ISmoke(0x0a0108, 0.0f); iCorona = IBill(s, O + V(0, 75, -585), 10, 10, iCoronaM).transform;
                var tm = INF(Mats.Basic(0x000000));
                for (int k = 0; k < 18; k++)
                {
                    float a = k * Mathf.PI * 2 / 18 + R(-0.15f, 0.15f);
                    var piv = new GameObject("tendril").transform; piv.SetParent(s, false); piv.position = O + V(0, 75, -588); piv.rotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
                    var seg = IMsh(piv, Geo.Box(1, 1, 1), tm, piv.position).transform; seg.localPosition = V(0.5f, 0, 0); seg.localScale = V(1, R(3, 8), 1);
                    piv.localScale = V(0.01f, 1, 1); iTendrils.Add(piv);
                }
                // smoke rising out of the tearing land (invisible until it tears)
                iRiseSmokeM = ISmoke(0x140a0a, 0f);
                for (int k = 0; k < 22; k++) { var p = IBill(s, O + V(R(-380, 380), R(0, 80), R(-420, 40)), R(50, 110), R(50, 110), iRiseSmokeM).transform; IDrift(0, p, V(R(-2, 2), R(6, 12), 0), O + V(0, 70, -190), V(400, 80, 240)); }
                for (int k = 0; k < 50; k++) { var m = IGlow(s, O + V(R(-200, 200), R(10, 120), R(-300, 120)), 0xffe0a0, R(1.2f, 2.6f), 0.7f).transform; IDrift(0, m, V(R(-1, 1), R(1, 3), R(-1, 1)), O + V(0, 65, -90), V(220, 60, 220)); }
            }
            // ---- 1: the ruins ----
            {
                var s = ISet(1); var O = s.position;
                IBill(s, O + V(0, 120, -620), 2600, 1000, INF(Mats.Basic(0xffffff, IGrad(0x040102, 0x6a1606), true)));
                IDiscGlow(s, O + V(40, 30, -560), 0xff3a10, 360, 0.03f, 12);
                var dark = IM(Mats.Basic(0x100604)); IMsh(s, Geo.Box(1600, 1, 1600), dark, O + V(0, -0.5f, -300));
                var bld = IM(Mats.Basic(0x160a08));
                for (int k = 0; k < 46; k++)
                {
                    float x = R(-320, 320), z = R(-460, -60), w = R(10, 30), h = R(20, 95), lean = rng.NextDouble() < 0.3 ? R(-14, 14) : 0;
                    var b = IMsh(s, Geo.Box(w, h, R(10, 26)), bld, O + V(x, h / 2, z), V(0, R(-20, 20), lean)).transform;
                    if (rng.NextDouble() < 0.6) IMsh(b, Geo.Box(w * 0.6f, R(6, 18), w * 0.5f), bld, b.position + b.up * (h / 2 + 3), V(R(-30, 30), R(0, 90), R(-35, 35)));
                }
                foreach (var sx in new[] { -1f, 1f }) { IMsh(s, Geo.Box(16, 110, 16), bld, O + V(sx * 34, 55, -260)); IMsh(s, Geo.Cone(12, 40, 4), bld, O + V(sx * 34, 128, -260), V(0, 45, sx * 8)); }
                IMsh(s, Geo.Torus(34, 6, 24, 4, Mathf.PI * 0.7f), bld, O + V(0, 70, -262), V(0, 0, 20));
                for (int k = 0; k < 16; k++) IGlow(s, O + V(R(-260, 260), R(2, 12), R(-380, -60)), 0xff5010, R(30, 70), 0.45f);
                for (int k = 0; k < 90; k++) { var m = IGlow(s, O + V(R(-150, 150), R(0, 80), R(-200, 60)), 0xff7020, R(0.7f, 1.6f), 0.8f).transform; IDrift(1, m, V(R(-2, 3), R(4, 9), R(-1, 1)), O + V(0, 40, -60), V(170, 45, 140)); }
                var sm = ISmoke(0x120806, 0.6f);
                for (int k = 0; k < 24; k++) { var p = IBill(s, O + V(R(-220, 220), R(10, 70), R(-300, -40)), R(60, 140), R(50, 110), sm).transform; IDrift(1, p, V(R(1, 4), R(1, 3), 0), O + V(0, 45, -170), V(260, 40, 140)); }
            }
            // ---- 2-6: the five silhouettes, in near darkness ----
            IBossSet(0, 2, 26, 0x46525e, 0x1c242c, 0x050709); // the Frozen Traitor: an extremely dark blizzard
            IBossSet(1, 3, 34, 0x5a2410, 0x220a04, 0x040101); // the Hollow King: thick smoke, a dull fire behind it
            IBossSet(2, 4, 30, 0x6a5c40, 0x2a2214, 0x060504); // the Seraph: a dim glow, far off
            IBossSet(3, 5, 34, 0x3e4a48, 0x161e1e, 0x040606); // the Warden of Hours: grey murk, mechanisms behind
            IBossSet(4, 6, 24, 0x6a1c06, 0x260a03, 0x050101); // Carnifex: furnace heat breathing behind him, the pit's smoke and embers (first out of the dark)

            Mk.layer = pl; Mk.colliders = pc;
            Mk.SetLayerRecursive(introRoot.gameObject, CINE_LAYER);
            foreach (var st in iSets) st.gameObject.SetActive(false);
        }
        // the four guardians' silhouette poses (and the small life in each): the Traitor a fallen warrior king, head bowed,
        // a hand on his planted sword; the Hollow King hunched and heavy, one huge arm hanging, the other half raised; the
        // Seraph floating, wings spread to their full span, arms flung out; the Warden tilted, one hand raised over time, the
        // other at rest, his dial face square to the camera
        TraitorRig iTrR; HollowRig iHkR; SerRig iSrR; WardenRig iWdR;
        void ISilPose(int k, float t)
        {
            float b = Mathf.Sin(t * 1.1f), b2 = Mathf.Sin(t * 0.7f + 1);
            if (k == 0 && iTrR != null)
            {
                var r = iTrR; Sm(r.root, 1, -0.75f, 1); Sm(r.torso, 0, 0.16f + b * 0.01f, 1); // (turned three-quarters to us: the kneel and the bowed head read in the outline)
                if (r.neck != null) Sm(r.neck, 0, 0.35f, 1); Sm(r.torso, 1, -0.1f, 1); Sm(r.torso, 2, -0.06f, 1); Sm(r.head, 0, 0.7f + b2 * 0.03f, 1); Sm(r.jaw, 0, 0, 1);
                Sm(r.rSh, 0, -0.35f, 1); Sm(r.rSh, 2, 0.62f, 1); Sm(r.rEl, 0, -0.35f, 1);                          // the sword hand, out at his side, resting on the pommel                           // the sword hand, resting forward on the pommel
                Sm(r.lSh, 0, 0.1f + b * 0.02f, 1); Sm(r.lSh, 2, -0.3f, 1); Sm(r.lEl, 0, -0.2f, 1);              // the other hanging, heavy
                Sm(r.thigh[0], 0, -1.35f, 1); Sm(r.knee[0], 0, 1.45f, 1); Sm(r.thigh[1], 0, 0.3f, 1); Sm(r.knee[1], 0, 1.75f, 1); // down on one knee
                if (r.hips != null) r.hips.py = 0.78f;
            }
            else if (k == 1 && iHkR != null)
            {
                var r = iHkR; Sm(r.torso, 0, 0.42f + b * 0.025f, 1); Sm(r.torso, 1, -0.18f, 1); Sm(r.head, 0, 0.28f, 1); Sm(r.jaw, 0, 0.25f, 1);
                Sm(r.lSh, 0, 0.22f, 1); Sm(r.lSh, 2, -0.6f, 1); Sm(r.lEl, 0, -0.1f, 1);                          // one massive arm hanging low, knuckles near the ground
                Sm(r.rSh, 0, -1.05f + b2 * 0.04f, 1); Sm(r.rSh, 2, 0.75f, 1); Sm(r.rEl, 0, -0.85f, 1);             // the other half raised, clawed
                Sm(r.thigh[0], 0, -0.22f, 1); Sm(r.thigh[1], 0, 0.1f, 1); Sm(r.knee[0], 0, 0.35f, 1); Sm(r.knee[1], 0, 0.3f, 1);
                if (r.hips != null) r.hips.py = 1.45f;
            }
            else if (k == 2 && iSrR != null)
            {
                var r = iSrR;
                for (int i = 0; i < r.wings.Count; i++) { float s = r.wingS[i]; int w = i % 3; Sm(r.wings[i], 2, s * (new[] { 0.62f, 0.3f, 0.0f }[w] + Mathf.Sin(t * 1.2f + i * 0.4f) * 0.04f), 1); Sm(r.wings[i], 1, s * 0.05f, 1); r.wings[i].t.localScale = Vector3.one * new[] { 3.4f, 3.1f, 2.7f }[w]; } // enormous: raised in a great V behind her, beating slowly
                Sm(r.sh[0], 0, -0.1f, 1); Sm(r.sh[0], 2, -1.2f, 1); if (r.el[0] != null) Sm(r.el[0], 0, -0.1f, 1);   // arms flung out
                Sm(r.sh[1], 0, -0.1f, 1); Sm(r.sh[1], 2, 1.2f, 1); if (r.el[1] != null) Sm(r.el[1], 0, -0.1f, 1);
                Sm(r.spear, 0, -1.35f, 1); Sm(r.torso, 0, -0.12f, 1); Sm(r.head, 0, -0.18f, 1);
                if (r.root != null) r.root.py = Mathf.Sin(t * 1.5f) * 0.1f;
            }
            else if (k == 3 && iWdR != null)
            {
                var r = iWdR; Sm(r.chest, 0, -0.06f, 1); Sm(r.chest, 2, 0.16f, 1); Sm(r.chest, 1, 0, 1); Sm(r.hood, 0, 0, 1); // tilted, the dial square to us
                Sm(r.lSh, 0, -0.55f + b * 0.04f, 1); Sm(r.lSh, 2, -2.15f, 1); Sm(r.lEl, 0, -0.6f, 1);              // one hand raised out and up, over time
                if (r.lHand != null) Sm(r.lHand, 2, Mathf.Sin(t * 0.9f) * 0.25f, 1);                                 // turning slowly, as if winding it
                Sm(r.rSh, 0, 0.18f, 1); Sm(r.rSh, 2, -0.28f, 1); Sm(r.rEl, 0, -0.3f, 1);                            // the other at rest
                if (r.body != null) r.body.py = 1.6f + Mathf.Sin(t * 1.1f) * 0.3f;
            }
        }
        void IBossSet(int k, int set, float H, int core, int mid, int edge)
        {
            var s = ISet(set); var O = s.position; var rng = new System.Random(31 + k);
            float R(float a, float b) => a + (b - a) * (float)rng.NextDouble();
            // a dim backdrop and a faint glow behind the figure - their brightness is what the flickers of light drive
            iBack[k] = IM(Mats.Basic(0xffffff, IRadial(core, mid, edge), true)); IBill(s, O + V(0, H * 0.6f, -220), 1400, 1000, iBack[k]);
            iBloomM[k] = IDiscGlow(s, O + V(0, H * 0.62f, -60), core, H * 2.3f, 0.02f);
            IMsh(s, Geo.Box(900, 1, 900), IM(Mats.Basic(0x020202)), O + V(0, -0.5f, 0));
            // the figure: the real boss model, every surface flat black, every glow off
            var e = NewEnt("introBoss"); Mk.colliders = false; Mk.layer = CINE_LAYER;
            e.g.transform.SetParent(s, false); e.g.transform.position = O;
            try
            {
                // each built in its own pose before it is measured and blacked out - the pose IS the silhouette
                if (k == 0) { iTrR = BuildTraitor(e); ISilPose(0, 0); }
                else if (k == 1) { iHkR = BuildHollow(e); ISilPose(1, 0); }
                else if (k == 2) { iSrR = BuildSeraph(e); ISilPose(2, 0); }
                else if (k == 4) { var cr = BuildCarnifex(e); Sm(cr.torso, 0, 0.32f, 1); Sm(cr.head, 0, 0.15f, 1); Sm(cr.rSh, 2, 0.55f, 1); Sm(cr.lSh, 2, -0.55f, 1); Sm(cr.rSh, 0, -0.35f, 1); Sm(cr.lSh, 0, -0.2f, 1); Sm(cr.rEl, 0, -0.7f, 1); Sm(cr.lEl, 0, -0.8f, 1); Sm(cr.jaw, 0, 0.35f, 1); } // (hunched, arms hanging wide: the titan)
                else { iWdR = BuildWarden(e); foreach (var sp in iWdR.spins) iSpins.Add(sp); ISilPose(3, 0); }
            }
            catch (System.Exception ex) { Debug.LogWarning("intro silhouette " + k + ": " + ex.Message); }
            Mk.colliders = false;
            foreach (var c in e.g.GetComponentsInChildren<Collider>(true)) Destroy(c);
            var rb = e.g.GetComponent<Rigidbody>(); if (rb) Destroy(rb);
            var black = IM(Mats.Basic(0x000000, null, true));
            var rends = e.g.GetComponentsInChildren<Renderer>(true); var bb = new Bounds(); bool any = false;
            foreach (var rr in rends)
            {
                var sm = rr.sharedMaterial;
                if (sm == null || sm.renderQueue >= 2450 || rr.gameObject.name == "glow" || !rr.gameObject.activeInHierarchy) { rr.enabled = false; continue; }
                if (!(testMode && Arg("-brsilcolor") != null)) { var ms = rr.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = black; rr.sharedMaterials = ms; } // (-brsilcolor: keep the colours, to check the poses - test)
                rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (!any) { bb = rr.bounds; any = true; } else bb.Encapsulate(rr.bounds);
            }
            float sc = any && bb.size.y > 0.01f ? H / bb.size.y : 1;
            e.g.transform.localScale = e.g.transform.localScale * sc;
            if (any) { var off = (bb.center - O) * sc; e.g.transform.position = O - V(off.x, (bb.min.y - O.y) * sc - (k == 2 ? H * 0.12f : 0), off.z); }
            iBoss[k] = e.g.transform; iBossH[k] = H;
            // the Traitor's greatsword, planted point-down before him, his hand on its pommel (his own rig carries no blade)
            if (k == 0 && iTrR?.rHand != null)
            {
                // planted out from his side and leaning in to his hand, so the blade stands clear of his body in the outline
                var hp = iTrR.rHand.t.position; var outw = V(hp.x - O.x, 0, hp.z - O.z); outw = outw.sqrMagnitude > 0.01f ? outw.normalized : Vector3.right;
                var tip = V(hp.x, O.y, hp.z) + outw * H * 0.13f; var ax = hp - tip; float len = Mathf.Max(1, ax.magnitude);
                var sw = new GameObject("silSword").transform; sw.SetParent(s, false); sw.position = tip; sw.rotation = Quaternion.FromToRotation(Vector3.up, ax / len);
                IMsh(sw, Geo.Box(H * 0.06f, len * 0.74f, H * 0.014f), black, sw.TransformPoint(V(0, len * 0.37f, 0)), sw.eulerAngles);                 // the blade
                IMsh(sw, Geo.Box(H * 0.24f, H * 0.022f, H * 0.035f), black, sw.TransformPoint(V(0, len * 0.76f, 0)), sw.eulerAngles);                // the crossguard
                IMsh(sw, Geo.Box(H * 0.026f, len * 0.2f, H * 0.026f), black, sw.TransformPoint(V(0, len * 0.87f, 0)), sw.eulerAngles);              // the grip
                IMsh(sw, Geo.Sphere(H * 0.026f, 8, 6), black, sw.TransformPoint(V(0, len * 0.98f, 0)));                                              // the pommel, under his hand
            }
            // fog banks in front and behind: most of the figure sinks into them
            iMistM[k] = ISmoke(mid, 0.55f); var mistDark = ISmoke(edge, 0.7f);
            for (int i = 0; i < 12; i++) { var p = IBill(s, O + V(R(-90, 90), R(1, H * 0.5f), R(-40, H * 0.9f)), R(60, 140), R(16, 40), i % 2 == 0 ? mistDark : iMistM[k]).transform; IDrift(set, p, V(R(1.5f, 4), R(-0.3f, 0.3f), 0), O + V(0, H * 0.25f, H * 0.2f), V(110, H * 0.3f, H * 0.7f)); }
            switch (k)
            {
                case 0: // the blizzard: driven snow, streaming gusts
                    for (int i = 0; i < 300; i++) { var f = IGlow(s, O + V(R(-60, 60), R(0, H * 1.6f), R(-30, H * 2)), 0x7a8898, R(0.25f, 0.7f), 0.55f).transform; IDrift(set, f, V(R(-38, -24), R(-9, -5), R(-3, 3)), O + V(0, H * 0.8f, H * 0.6f), V(70, H * 0.85f, H * 1.3f)); }
                    var gust = ISmoke(0x3a4652, 0.35f);
                    for (int i = 0; i < 12; i++) { var p = IBill(s, O + V(R(-80, 80), R(4, H * 1.2f), R(-10, H * 0.8f)), R(30, 70), R(14, 30), gust).transform; IDrift(set, p, V(R(-24, -14), R(-3, -1), 0), O + V(0, H * 0.6f, H * 0.3f), V(90, H * 0.6f, H * 0.6f)); }
                    break;
                case 1: // thick smoke rolling over it, embers, wreckage
                    var smk = ISmoke(0x0c0806, 0.7f);
                    for (int i = 0; i < 30; i++) { var p = IBill(s, O + V(R(-70, 70), R(0, H * 1.3f), R(-20, H * 1.1f)), R(22, 55), R(22, 55), smk).transform; IDrift(set, p, V(R(1, 4), R(2, 5), 0), O + V(0, H * 0.7f, H * 0.3f), V(80, H * 0.75f, H * 0.8f)); }
                    for (int i = 0; i < 70; i++) { var f = IGlow(s, O + V(R(-50, 50), R(0, H * 1.5f), R(-20, H * 1.5f)), 0x8a3a14, R(0.3f, 0.8f), 0.7f).transform; IDrift(set, f, V(R(-2, 2), R(6, 12), R(-1, 1)), O + V(0, H * 0.75f, H * 0.5f), V(60, H * 0.8f, H)); }
                    var wreck = IM(Mats.Basic(0x000000));
                    for (int i = 0; i < 14; i++) IMsh(s, Geo.Box(R(4, 14), R(3, 10), R(4, 10)), wreck, O + V(R(-70, 70), R(1, 4), R(-30, 30)), V(R(-30, 30), R(0, 90), R(-40, 40)));
                    break;
                case 2: // a few faint rays wheeling far behind her, and the barest outline of a halo
                    iRays = new GameObject("rays").transform; iRays.SetParent(s, false); iRays.position = O + V(0, H * 0.75f, -40);
                    var ray = IM(Mats.Add(0x8a7650, 0.03f, null, false));
                    for (int i = 0; i < 16; i++) { var rp = new GameObject("ray").transform; rp.SetParent(iRays, false); rp.localRotation = Quaternion.Euler(0, 0, i * 360f / 16 + R(-4, 4)); var rm = IMsh(rp, Geo.Cone(1, 1, 4), ray, iRays.position).transform; rm.localPosition = V(0, 160, 0); rm.localRotation = Quaternion.Euler(180, 0, 0); rm.localScale = V(R(4, 10), 320, 0.5f); }
                    iSerHaloM = IM(Mats.Add(0x9a8a60, 0.12f, null, false));
                    iSerHalo = IMsh(s, Geo.Torus(H * 0.14f, H * 0.005f, 48, 4), iSerHaloM, O + V(0, H * 1.02f, 2)).transform;
                    for (int i = 0; i < 50; i++) { var f = IGlow(s, O + V(R(-50, 50), R(0, H * 1.5f), R(-20, H)), 0x6a5a40, R(0.3f, 0.9f), 0.45f).transform; IDrift(set, f, V(R(-1, 1), R(-2, -0.5f), R(-1, 1)), O + V(0, H * 0.75f, H * 0.3f), V(60, H * 0.8f, H * 0.8f)); }
                    break;
                case 3: // indistinct mechanisms turning behind him in the murk
                    var gm = IM(Mats.Basic(0x000000, null, true));
                    foreach (var (x, y, z, r, sp) in new[] { (-62f, 58f, -150f, 40f, 0.22f), (58f, 70f, -175f, 52f, -0.14f), (-8f, 92f, -200f, 30f, 0.35f), (96f, 30f, -150f, 24f, -0.45f), (-110f, 26f, -160f, 22f, 0.5f) })
                    {
                        var g = new GameObject("gear").transform; g.SetParent(s, false); g.position = O + V(x, y, z);
                        IMsh(g, Geo.Ring(r * 0.55f, r, 48), gm, g.position);
                        for (int i = 0; i < 6; i++) IMsh(g, Geo.Box(r * 1.1f, r * 0.08f, 0.5f), gm, g.position, V(0, 0, i * 30));
                        int teeth = Mathf.RoundToInt(r * 0.9f);
                        for (int i = 0; i < teeth; i++) { float a = i * 360f / teeth; var tt = IMsh(g, Geo.Box(r * 0.12f, r * 0.16f, 0.5f), gm, g.position, V(0, 0, a)).transform; tt.position = g.position + Quaternion.Euler(0, 0, a) * V(0, r + r * 0.07f, 0); }
                        iGears.Add((g, sp));
                    }
                    // a murk in front of the gears, so they are only half there
                    var murk = ISmoke(mid, 0.5f);
                    for (int i = 0; i < 8; i++) { var p = IBill(s, O + V(R(-120, 120), R(20, 110), R(-120, -90)), R(80, 160), R(60, 120), murk).transform; IDrift(set, p, V(R(1, 3), R(-0.5f, 0.5f), 0), O + V(0, 60, -105), V(140, 50, 15)); }
                    for (int i = 0; i < 40; i++) { var f = IGlow(s, O + V(R(-50, 50), R(0, H * 1.5f), R(-20, H)), 0x5a6a68, R(0.3f, 0.7f), 0.4f).transform; IDrift(set, f, V(R(-1, 1), R(-1, 1), R(-1, 1)), O + V(0, H * 0.75f, H * 0.3f), V(60, H * 0.8f, H * 0.8f)); }
                    break;
                case 4: // the pit: heavy smoke rolling up past him, embers streaming, chains hanging black out of the dark
                    var psm = ISmoke(0x0a0504, 0.72f);
                    for (int i = 0; i < 26; i++) { var p = IBill(s, O + V(R(-70, 70), R(0, H * 1.4f), R(-20, H * 1.2f)), R(22, 50), R(22, 50), psm).transform; IDrift(set, p, V(R(-1, 1), R(4, 8), 0), O + V(0, H * 0.7f, H * 0.3f), V(80, H * 0.8f, H * 0.8f)); }
                    for (int i = 0; i < 110; i++) { var f = IGlow(s, O + V(R(-50, 50), R(0, H * 1.6f), R(-20, H * 1.5f)), 0x9a3810, R(0.25f, 0.7f), 0.75f).transform; IDrift(set, f, V(R(-1.5f, 1.5f), R(8, 15), R(-1, 1)), O + V(0, H * 0.8f, H * 0.5f), V(60, H * 0.85f, H)); }
                    var chainM = IM(Mats.Basic(0x000000));
                    foreach (var (x, z, len) in new[] { (-34f, -30f, 70f), (-18f, -60f, 90f), (26f, -40f, 80f), (44f, -70f, 100f), (8f, -90f, 110f) }) for (int j = 0; j < (int)(len / 2.6f); j++) IMsh(s, Geo.Torus(1.0f, 0.24f, 8, 4), chainM, O + V(x, H * 2.6f - j * 2.4f, z), V(0, j % 2 == 0 ? 0 : 90, 0));
                    break;
            }
        }

        // ================= scenes, sound =================
        static int ISetOf(int sc) => sc >= 1 && sc <= 4 ? 0 : sc == 5 ? 1 : sc >= 7 && sc <= 11 ? 2 + ISIL[sc - 7] : -1;
        void IntroScene(int sc)
        {
            foreach (var st in iSets) st.gameObject.SetActive(false);
            int set = ISetOf(sc);
            if (set >= 0 && set < iSets.Count) iSets[set].gameObject.SetActive(true);
            cineCam.backgroundColor = Color.black;
            RenderSettings.fog = sc >= 1 && sc <= 5;
            if (sc >= 1 && sc <= 2) { RenderSettings.fogColor = Mats.Hex(0xc89868); RenderSettings.fogStartDistance = 70; RenderSettings.fogEndDistance = 900; }
            if (sc == 5) { RenderSettings.fogColor = Mats.Hex(0x300a04); RenderSettings.fogStartDistance = 30; RenderSettings.fogEndDistance = 480; }
            if (iLight) iLight.enabled = sc >= 1 && sc <= 4;
        }
        void ICue(string key, float t, float at, string sfx, float vol) { if (t >= at && iCues.Add(key) && t - at < 0.6f) Sfx(sfx, vol); } // (cues long past - joining part-way - stay silent)
        // the sound design, cued on the timeline (so it stays in step on every machine in co-op)
        void IntroSound(float t)
        {
            // darkness: the far rumble, whispers, metal, then reality distorting
            ICue("r0", t, 0.25f, "rumble", 0.55f); ICue("w0", t, 0.9f, "iWhisper", 0.8f); ICue("c0", t, 1.7f, "iCreak", 0.6f); ICue("d0", t, 2.2f, "iDistort", 0.7f); ICue("dr0", t, 2.6f, "iDrone", 0.75f);
            ICue("sw", t, IN_A + 0.2f, "iSwell", 0.55f); ICue("wi1", t, IN_A + 0.5f, "iWind", 0.5f); ICue("wi2", t, IN_A2, "iWind", 0.45f); ICue("sw2", t, IN_A2 + 0.3f, "iSwell", 0.45f);
            // the disturbance
            ICue("cr", t, IN_B, "iCrack", 1f); ICue("ds", t, IN_B + 0.1f, "iDistort", 0.9f); ICue("th1", t, IN_B + 0.9f, "iThunder", 0.9f); ICue("cl", t, IN_B + 1.0f, "iCollapse", 0.9f);
            ICue("th2", t, IN_B + 2.4f, "iThunder", 0.8f); ICue("dr1", t, IN_B + 0.3f, "iDrone", 0.9f);
            ICue("vd", t, IN_B2, "iVoid", 1f); ICue("rf", t, IN_B2 + 0.8f, "iRoarFar", 0.7f); ICue("th3", t, IN_B2 + 1.8f, "iThunder", 0.7f); ICue("cl2", t, IN_B2 + 2.2f, "iCollapse", 0.7f); ICue("th4", t, IN_B2 + 3.0f, "iThunder", 0.6f);
            ICue("ru", t, IN_C, "iWhoosh", 0.7f); ICue("ru2", t, IN_C + 0.3f, "rumble", 0.6f); ICue("cr2", t, IN_C + 1.3f, "iCreak", 0.5f);
            // (IN_C2..IN_D: silence)
            // the silhouettes: a heartbeat as each comes out of the dark, and the sound of its place
            for (int j = 0; j < 5; j++)
            {
                int k = ISIL[j]; float b = IN_D + j * IN_BOSS;
                ICue("hb" + k, t, b + 0.3f, "iBeat", 0.9f);
                if (k == 0) { ICue("b0w", t, b + 0.1f, "iWind", 0.9f); ICue("b0x", t, b + 1.3f, "iWhisper", 0.6f); ICue("b0t", t, b + 1.25f, "iThunder", 0.35f); }
                if (k == 1) { ICue("b1r", t, b + 1.0f, "iRoarFar", 0.6f); ICue("b1c", t, b + 0.6f, "iCreak", 0.6f); }
                if (k == 2) { ICue("b2w", t, b + 0.5f, "iWhisper", 0.8f); ICue("b2s", t, b + 0.9f, "iSwell", 0.3f); }
                if (k == 3) { for (int n = 0; n < 5; n++) ICue("b3t" + n, t, b + 0.4f + n * 0.5f, "tick", 0.5f); ICue("b3c", t, b + 0.8f, "iCreak", 0.55f); }
                if (k == 4) { ICue("b4r", t, b + 0.2f, "rumble", 0.7f); ICue("b4f", t, b + 1.8f, "iRoarFar", 0.75f); ICue("b4c", t, b + 0.9f, "clank", 0.2f); }
            }
            ICue("dr2", t, IN_D + 0.1f, "iDrone", 0.7f); ICue("dr3", t, IN_D + 2 * IN_BOSS, "iDrone", 0.7f);
            // narration, title
            ICue("nw", t, IN_N + 0.2f, "iWhisper", 0.4f); ICue("ndr", t, IN_N + 0.4f, "iDrone", 0.6f);
            ICue("ti", t, IN_T, "iTitle", 1f);
        }
        void IntroCam(Vector3 a, Vector3 b, Vector3 la, Vector3 lb, float u, float shake, float roll = 0)
        {
            float e = ease3(Mathf.Clamp01(u));
            var pos = Vector3.Lerp(a, b, e); var look = Vector3.Lerp(la, lb, e);
            float sh = shake + INTRO.shake; if (sh > 0) pos += V(Mathf.PerlinNoise(INTRO.t * 9, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, INTRO.t * 9) - 0.5f, 0) * sh;
            CineLook(pos, look); if (roll != 0) cineCam.transform.rotation = cineCam.transform.rotation * Quaternion.Euler(0, 0, roll);
        }
        static float Pulse(float u, float at, float w) { float d = (u - at) / w; return Mathf.Exp(-d * d); }
        void IntroAnimate(int sc, float t, float rdt)
        {
            INTRO.shake = Mathf.Max(0, INTRO.shake - rdt * 2); iFlashA = Mathf.Max(0, iFlashA - rdt * 3);
            int set = ISetOf(sc);
            if (set >= 0 && iParts[set] != null)
                foreach (var p in iParts[set])
                {
                    if (!p.t) continue; var q = p.t.position + p.v * rdt; var d = q - p.home;
                    if (d.x > p.box.x) q.x -= 2 * p.box.x; else if (d.x < -p.box.x) q.x += 2 * p.box.x;
                    if (d.y > p.box.y) q.y -= 2 * p.box.y; else if (d.y < -p.box.y) q.y += 2 * p.box.y;
                    if (d.z > p.box.z) q.z -= 2 * p.box.z; else if (d.z < -p.box.z) q.z += 2 * p.box.z;
                    p.t.position = q;
                }
            for (int i = iDebris.Count - 1; i >= 0; i--)
            {
                var db = iDebris[i]; if (!db.t) { iDebris.RemoveAt(i); continue; }
                db.v.y -= 32 * rdt; db.t.position += db.v * rdt; db.t.Rotate(db.spin * rdt); db.life -= rdt;
                if (db.life <= 0) { db.t.gameObject.SetActive(false); iDebris.RemoveAt(i); }
            }
            var O0 = IO[0];
            switch (sc)
            {
                case 1: // the reveal: low between the towers, rising slowly as dawn comes up
                {
                    float u = (t - IN_A) / (IN_A2 - IN_A);
                    IntroCam(O0 + V(-60, 6, 170), O0 + V(-34, 26, 104), O0 + V(-10, 40, -300), O0 + V(0, 60, -360), u, 0);
                    float dawn = Mathf.Clamp01(u * 1.2f); Mats.SetColor(iSkyM, Color.Lerp(Mats.Hex(0x30303c), Color.white, dawn)); Mats.SetAlpha(iSunGlowM, 0.05f * dawn);
                    if (iLight) { iLight.color = Mats.Hex(0xffc890); iLight.intensity = Mathf.Lerp(0.08f, 1.6f, dawn * dawn); }
                    IArchAmbient(Color.Lerp(Mats.Hex(0x0c0c18), Mats.Hex(0x5a5a78), dawn), Color.Lerp(Mats.Hex(0x0a0808), Mats.Hex(0x6a5040), dawn), Color.Lerp(Mats.Hex(0x040404), Mats.Hex(0x2a1e16), dawn));
                    RenderSettings.fogColor = Color.Lerp(Mats.Hex(0x2a2430), Mats.Hex(0xc89868), dawn);
                    break;
                }
                case 2: // the wide view of the old world: high, drifting slowly across it
                {
                    float u = (t - IN_A2) / (IN_B - IN_A2);
                    IntroCam(O0 + V(170, 95, 270), O0 + V(120, 82, 230), O0 + V(-30, 70, -420), O0 + V(-20, 74, -420), u, 0);
                    Mats.SetColor(iSkyM, Color.white); Mats.SetAlpha(iSunGlowM, 0.05f);
                    if (iLight) { iLight.color = Mats.Hex(0xffcc94); iLight.intensity = 1.6f; } IArchAmbient(Mats.Hex(0x5a5a78), Mats.Hex(0x6a5040), Mats.Hex(0x2a1e16));
                    float bob = Mathf.Sin(t * 0.6f) * 3; iKingdom.position = iKingdomHome + V(0, bob, 0);
                    break;
                }
                case 3: // the disturbance: a low, tilted shot under the great tower as the sky cracks and it breaks
                {
                    float u = (t - IN_B) / (IN_B2 - IN_B), lt = t - IN_B;
                    IntroCam(O0 + V(10, 5, 40), O0 + V(-4, 7, 52), O0 + V(70, 95, -140), O0 + V(80, 55, -230), u, 0.6f + u * 1.6f, -9 + u * 4);
                    for (int k = 0; k < iCracks.Count; k++) { int show = Mathf.FloorToInt(Mathf.Clamp01((lt - 0.05f - k * 0.18f) / 1.2f) * iCracks[k].Count / 2f); for (int j = 0; j < iCracks[k].Count; j++) { bool on = (j % (iCracks[k].Count / 2)) < show; if (iCracks[k][j].activeSelf != on) iCracks[k][j].SetActive(on); } }
                    Mats.SetColor(iSkyM, Color.Lerp(Color.white, Mats.Hex(0x8a5048), Mathf.Clamp01(lt / 2.5f)));
                    RenderSettings.fogColor = Color.Lerp(Mats.Hex(0xc89868), Mats.Hex(0x5a2a28), Mathf.Clamp01(lt / 2.5f));
                    float red = Mathf.Clamp01(lt / 2.5f);
                    if (iLight) { iLight.color = Color.Lerp(Mats.Hex(0xffcc94), Mats.Hex(0xff5a3a), red); iLight.intensity = Mathf.Lerp(1.6f, 1.05f, red); }
                    IArchAmbient(Color.Lerp(Mats.Hex(0x5a5a78), Mats.Hex(0x4a2028), red), Color.Lerp(Mats.Hex(0x6a5040), Mats.Hex(0x3a1814), red), Mats.Hex(0x1a0c0a));
                    Mats.SetAlpha(iCrackM, Mathf.Clamp01((lt - 0.6f) / 2.4f) * 0.55f * (0.8f + 0.2f * Mathf.PerlinNoise(t * 9, 4)));
                    // the tower: it shudders, cracks, then topples (debris shaking out of it)
                    float fall = Mathf.Clamp01((lt - 0.9f) / 2.4f); fall = fall * fall;
                    iTower.rotation = Quaternion.Euler(-fall * 64 + Mathf.Sin(t * 40) * 0.4f * (lt > 0.5f ? 1 : 0), 0, -fall * 14); // (it falls away from us, into the haze)
                    if (lt > 0.95f && iCues.Add("deb0")) { IDebrisAt(iSets[0], iTower.position + iTower.up * 110, 30, 12, 6, new System.Random(3)); INTRO.shake = 2.5f; }
                    if (lt > 1.8f && iCues.Add("deb1")) IDebrisAt(iSets[0], iTower.position + iTower.up * 140, 26, 14, 5, new System.Random(4));
                    if (lt > 2.6f && iCues.Add("deb2")) { IDebrisAt(iSets[0], O0 + V(-30, 120, -60), 30, 10, 4, new System.Random(5)); INTRO.shake = 2; }
                    IBolt(lt, new[] { 0.85f, 2.35f, 2.55f });
                    break;
                }
                case 4: // the Void: pulling back wide as it swallows the sun, the floating kingdom, the sky; the land tears apart
                {
                    float u = Mathf.Clamp01((t - IN_B2) / (IN_C - IN_B2)), lt = t - IN_B2;
                    IntroCam(O0 + V(-20, 30, 120), O0 + V(10, 90, 330), O0 + V(-40, 90, -500), O0 + V(-10, 80, -480), u, 0.8f + u * 1.4f);
                    foreach (var cr in iCracks) foreach (var g in cr) if (!g.activeSelf) g.SetActive(true);
                    float vs = Mathf.Lerp(4, 300, Mathf.SmoothStep(0, 1, u * 1.05f)); iVoid.localScale = Vector3.one * vs;
                    iCorona.localScale = V(vs * 3.6f, vs * 3.6f, 1); Mats.SetAlpha(iCoronaM, Mathf.Clamp01(u * 1.6f) * 0.85f);
                    Mats.SetAlpha(iSunGlowM, 0.05f * (1 - Mathf.Clamp01(u * 1.5f)));
                    Mats.SetColor(iSkyM, Color.Lerp(Mats.Hex(0x8a5048), Mats.Hex(0x2a0a14), Mathf.Clamp01(u * 1.2f)));
                    RenderSettings.fogColor = Color.Lerp(Mats.Hex(0x5a2a28), Mats.Hex(0x16040a), Mathf.Clamp01(u * 1.3f)); RenderSettings.fogEndDistance = Mathf.Lerp(900, 560, u);
                    Mats.SetColor(iGuardM, Color.Lerp(Color.white, Mats.Hex(0x806060), u)); // (the marble darkens)
                    if (iLight) { iLight.color = Color.Lerp(Mats.Hex(0xff5a3a), Mats.Hex(0x9a1a10), u); iLight.intensity = Mathf.Lerp(1.05f, 0.75f, u); }
                    IArchAmbient(Color.Lerp(Mats.Hex(0x4a2028), Mats.Hex(0x2a0c12), u), Color.Lerp(Mats.Hex(0x3a1814), Mats.Hex(0x240c08), u), Mats.Hex(0x0a0404));
                    Mats.SetAlpha(iCrackM, (0.55f + 0.45f * Mathf.Clamp01(u * 2)) * (0.7f + 0.3f * Mathf.PerlinNoise(t * 12, 5)));
                    IArchCollapse(lt, rdt);
                    bool flick = Mathf.PerlinNoise(t * 14, 1) > 0.45f;
                    foreach (var h in iHalos) { Mats.SetColor(h, Color.Lerp(Mats.Hex(0xffd070), Mats.Hex(0xff1a10), Mathf.Clamp01(u * 2 - 0.2f))); Mats.SetAlpha(h, u > 0.25f && flick ? 0.2f : 0.9f); }
                    for (int i = 0; i < iTendrils.Count; i++) { var tr = iTendrils[i]; float len = Mathf.Lerp(0.01f, 600 + (i % 4) * 160, Mathf.SmoothStep(0, 1, Mathf.Clamp01(u * 1.4f - 0.1f - (i % 3) * 0.05f))); tr.localScale = V(len, 1, 1); }
                    // the floating kingdom is drawn in and swallowed
                    var toV = iVoid.position - iKingdomHome; float reach = Mathf.Clamp01((vs - 120) / 120f);
                    iKingdom.position = Vector3.Lerp(iKingdomHome, iVoid.position, reach * reach * 0.8f); iKingdom.localScale = Vector3.one * Mathf.Max(0.001f, 1 - reach); iKingdom.Rotate(0, 0, reach * 30 * rdt);
                    float tear = Mathf.SmoothStep(0, 1, Mathf.Clamp01(u * 1.3f - 0.15f));
                    foreach (var sl in iSlabs) { sl.t.position = sl.home + sl.dir * (tear * 40) + V(0, -tear * sl.sink * 0.6f, 0); sl.t.rotation = Quaternion.Euler(sl.rot * tear); }
                    Mats.SetAlpha(iRiseSmokeM, 0.55f * tear);
                    if (lt > 1.5f && iCues.Add("deb3")) IDebrisAt(iSets[0], O0 + V(-160, 160, -400), 40, 16, 9, new System.Random(6));
                    if (lt > 2.6f && iCues.Add("deb4")) IDebrisAt(iSets[0], O0 + V(150, 140, -380), 40, 16, 9, new System.Random(8));
                    IBolt(lt, new[] { 0.5f, 1.75f, 1.95f, 2.95f });
                    break;
                }
                case 5: // the ruins: a slow, tilted track past the burning city
                {
                    float u = (t - IN_C) / (IN_C2 - IN_C); var O = IO[1];
                    IntroCam(O + V(-70, 14, 96), O + V(10, 20, 82), O + V(-40, 34, -220), O + V(10, 40, -220), u, 0.35f, 4 - u * 6);
                    break;
                }
                case 7: case 8: case 9: case 10: case 11: // a silhouette in the dark
                {
                    int j = sc - 7, k = ISIL[j]; var O = IO[2 + k]; float H = iBossH[k], u = Mathf.Clamp01((t - IN_D - j * IN_BOSS) / IN_BOSS);
                    float lum;
                    switch (k)
                    {
                        case 0: // through the blizzard, pushing slowly in; lightning somewhere beyond lights the outline for an instant
                            IntroCam(O + V(-8, H * 0.1f, H * 3.5f), O + V(-3, H * 0.13f, H * 2.8f), O + V(0, H * 0.55f, 0), O + V(0, H * 0.58f, 0), u, 0.3f);
                            lum = 0.22f + 0.9f * Pulse(u, 0.38f, 0.025f) + 0.5f * Pulse(u, 0.44f, 0.02f) + 0.7f * Pulse(u, 0.72f, 0.03f);
                            break;
                        case 1: // low before it, tilting slowly up its height as fire breathes through the smoke
                            IntroCam(O + V(12, H * 0.05f, H * 2.4f), O + V(8, H * 0.06f, H * 2.25f), O + V(0, H * 0.2f, 0), O + V(0, H * 0.78f, 0), u, 0.12f);
                            lum = 0.25f + 0.2f * Mathf.PerlinNoise(t * 3, 2) + 0.45f * Mathf.SmoothStep(0, 1, (u - 0.35f) / 0.25f) * (1 - Mathf.SmoothStep(0, 1, (u - 0.72f) / 0.2f));
                            break;
                        case 2: // far off, high; a slow swell of light behind her, then gone
                            IntroCam(O + V(0, H * 0.22f, H * 4.4f), O + V(4, H * 0.26f, H * 4.0f), O + V(0, H * 0.6f, 0), O + V(0, H * 0.62f, 0), u, 0.05f);
                            lum = 0.2f + 0.6f * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Clamp01((u - 0.15f) / 0.75f)), 3);
                            if (iBoss[2]) iBoss[2].position += V(0, Mathf.Sin(t * 1.6f) * 0.6f * rdt, 0);
                            if (iSerHaloM) Mats.SetAlpha(iSerHaloM, 0.04f + 0.12f * lum);
                            break;
                        case 4: // low in the smoke, pushing slowly in on his bulk; the furnace behind breathes, then flares once - the whole outline, then dark
                            IntroCam(O + V(-H * 0.55f, H * 0.07f, H * 2.7f), O + V(-H * 0.3f, H * 0.1f, H * 2.25f), O + V(0, H * 0.48f, 0), O + V(0, H * 0.55f, 0), u, 0.1f);
                            lum = 0.2f + 0.18f * (0.5f + 0.5f * Mathf.Sin(t * 4.2f)) * Mathf.SmoothStep(0, 1, u / 0.35f) + 0.85f * Pulse(u, 0.62f, 0.05f) + 0.3f * Pulse(u, 0.7f, 0.04f);
                            if (iMistM[4]) Mats.SetAlpha(iMistM[4], 0.5f + 0.1f * Mathf.Sin(t * 1.3f));
                            break;
                        default: // a slow arc past him; the murk pulses dimly with each beat of a clock
                            IntroCam(O + V(-H * 1.2f, H * 0.15f, H * 2.5f), O + V(H * 0.9f, H * 0.17f, H * 2.4f), O + V(0, H * 0.6f, 0), O + V(0, H * 0.6f, 0), u, 0.06f);
                            float lt3 = (t - IN_D - j * IN_BOSS - 0.4f); float beat = lt3 > 0 ? Mathf.Exp(-(lt3 % 0.5f) * 7) : 0;
                            lum = 0.2f + 0.35f * beat * Mathf.SmoothStep(0, 1, u / 0.3f);
                            break;
                    }
                    lum = Mathf.Clamp(lum, 0, 1.2f); ISilPose(k, t);
                    if (iBack[k]) Mats.SetColor(iBack[k], Color.white * lum);
                    if (iBloomM[k]) Mats.SetAlpha(iBloomM[k], 0.022f * lum);
                    if (iRays) iRays.localRotation = Quaternion.Euler(0, 0, t * 5);
                    foreach (var g in iGears) if (g.t) g.t.Rotate(0, 0, g.sp * 60 * rdt, Space.Self);
                    if (k == 3) foreach (var sp in iSpins) if (sp.t) sp.t.Rotate(sp.axis, sp.sp * 57.3f * rdt, Space.Self);
                    break;
                }
                default: CineLook(V(IO[0].x, -500, 0), V(IO[0].x, -500, -10)); break; // (nothing in view: black)
            }
        }
        // lightning: each bolt flickers for a moment, the sky lights up and the screen with it
        void IBolt(float lt, float[] at)
        {
            for (int i = 0; i < iBolts.Count; i++) if (iBolts[i].activeSelf) iBolts[i].SetActive(false);
            for (int i = 0; i < at.Length; i++)
            {
                float d = lt - at[i]; if (d < 0 || d > 0.2f) continue;
                bool on = d < 0.05f || (d > 0.09f && d < 0.15f); var b = iBolts[i % iBolts.Count]; b.SetActive(on);
                if (on) { iFlashA = Mathf.Max(iFlashA, 0.08f); if (iLight) { iLight.intensity = 2.0f; iLight.color = Mats.Hex(0xc8c0f0); } Mats.SetColor(iSkyM, Color.Lerp(iSkyM.GetColor("_BaseColor"), Mats.Hex(0xd8c8ff), 0.35f)); }
            }
        }

        // ================= overlay: letterbox, fades, words, the SKIP button =================
        void IntroOverlay()
        {
            if (!introUI)
            {
                introUI = Fill(Rt(canvasRT, "intro"));
                iFade = Img(introUI, Texture2D.whiteTexture, Color.black, "ifade"); Fill(iFade.rectTransform);
                iBarTop = Place(Qd(introUI, Color.black, "barT").rectTransform, 0, 1, 0, 1, 0, 0, 0, 0); iBarTop.anchorMin = V(0, 0.885f, 0); iBarTop.anchorMax = V(1, 1, 0); iBarTop.offsetMin = iBarTop.offsetMax = Vector2.zero;
                iBarBot = Place(Qd(introUI, Color.black, "barB").rectTransform, 0, 0, 0, 0, 0, 0, 0, 0); iBarBot.anchorMin = V(0, 0, 0); iBarBot.anchorMax = V(1, 0.115f, 0); iBarBot.offsetMin = iBarBot.offsetMax = Vector2.zero;
                iNarrT = Txt(introUI, "", 30, C("#e2d4c4"), TextAnchor.MiddleCenter, true, 4, 2); Place(iNarrT.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 1700, 110);
                iTitleT = Txt(introUI, "", 168, C("#c8141c"), TextAnchor.MiddleCenter, false, 20, 6); iTitleRT = Place(iTitleT.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 20, 1800, 200);
                iRule = Place(Qd(introUI, C("#c8141c"), "rule").rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, -88, 900, 4);
                iFlash = Img(introUI, Texture2D.whiteTexture, new Color(1, 1, 1, 0), "iflash"); Fill(iFlash.rectTransform);
                // SKIP: small, in the corner, the host's alone
                iSkipRT = Place(Rt(introUI, "iskip"), 1, 0, 1, 0, -34, 26, 150, 34);
                var hit = iSkipRT.gameObject.AddComponent<Quad>(); hit.Solid(new Color(0, 0, 0, 0.001f)); hit.raycastTarget = true;
                iSkipT = Txt(iSkipRT, "SKIP  ▸", 15, C("#8a7a72"), TextAnchor.MiddleRight, true, 5, 1); Fill(iSkipT.rectTransform);
                var sb = iSkipRT.gameObject.AddComponent<BRButton>(); sb.onClick = () => IntroSkip();
            }
            introUI.SetAsLastSibling(); introUI.gameObject.SetActive(true);
            iBarTop.gameObject.SetActive(true); iBarBot.gameObject.SetActive(true); iRule.gameObject.SetActive(true); iRule.localScale = V(0, 1, 1);
            iSkipRT.gameObject.SetActive(iCanSkip);
            iFade.color = Color.black; iNarrT.text = iTitleT.text = ""; iFlash.color = new Color(1, 1, 1, 0);
        }
        static float IWin(float t, float a, float b, float fin, float fout) => Mathf.Clamp01((t - a) / Mathf.Max(0.001f, fin)) * Mathf.Clamp01((b - t) / Mathf.Max(0.001f, fout));
        void IntroText(float t, float rdt)
        {
            // the black between and around the scenes: a slow fade up on the old world, a short dip into the wide view, a hard
            // cut to the disturbance, dips through the Void and the ruins, darkness, each silhouette out of and back into it
            float f = 1;
            if (t >= IN_A && t < IN_A2) f = 1 - IWin(t, IN_A, IN_A2, 2.4f, 0.35f);
            else if (t >= IN_A2 && t < IN_B) f = 1 - IWin(t, IN_A2, IN_B, 0.4f, 0.02f);
            else if (t >= IN_B && t < IN_B2) f = 1 - IWin(t, IN_B, IN_B2, 0.02f, 0.3f);
            else if (t >= IN_B2 && t < IN_C) f = 1 - IWin(t, IN_B2, IN_C, 0.3f, 0.5f);
            else if (t >= IN_C && t < IN_C2) f = 1 - IWin(t, IN_C, IN_C2, 0.5f, 0.9f);
            else if (t >= IN_D && t < IN_DE) { float lt = (t - IN_D) % IN_BOSS; f = 1 - IWin(lt, 0, IN_BOSS, 0.9f, 0.8f); }
            iFade.color = new Color(0, 0, 0, f);
            iFlash.color = new Color(0.85f, 0.8f, 1, iFlashA);
            // the narration: three lines, one at a time, out of the dark
            string[] lines = { "Once, the guardians protected existence.", "Now, the realms have fallen... and something ancient awakens.", "The truth lies beyond the shattered realms." };
            int li = Mathf.FloorToInt((t - IN_N) / IN_LINE); float na = 0;
            if (t >= IN_N && t < IN_T && li >= 0 && li < 3) { float lt = t - IN_N - li * IN_LINE; na = IWin(lt, 0.15f, IN_LINE - 0.1f, 0.6f, 0.55f); iNarrT.text = lines[li]; iNarrT.rectTransform.localScale = Vector3.one * (1 + lt * 0.01f); } else iNarrT.text = "";
            iNarrT.color = new Color(0.89f, 0.83f, 0.77f, na);
            // the title: slammed in out of the dark, a flash, a blood-red rule drawn under it, then into the game
            if (t >= IN_T)
            {
                float lt = t - IN_T, a = Mathf.Clamp01(lt / 0.1f) * Mathf.Clamp01((IN_END - 0.2f - t) / 0.7f);
                iTitleT.text = "BLOODRUSH"; iTitleT.color = new Color(0.8f, 0.07f, 0.1f, a); iTitleRT.localScale = Vector3.one * Mathf.Lerp(1.4f, 1, Mathf.Clamp01(lt / 0.16f)) * (1 + lt * 0.014f);
                iFlash.color = new Color(1, 0.8f, 0.75f, Mathf.Max(iFlashA, Mathf.Clamp01(1 - lt / 0.4f) * 0.5f));
                if (lt < 0.25f) INTRO.shake = 0;
                iRule.localScale = V(Smoother(Mathf.Clamp01((lt - 0.3f) / 0.7f)), 1, 1); iRule.GetComponent<Graphic>().color = new Color(0.78f, 0.08f, 0.1f, a);
            }
            else iTitleT.text = "";
            // the host's skip, fading in after a moment
            if (iCanSkip) iSkipT.color = new Color(0.55f, 0.48f, 0.45f, Mathf.Clamp01((t - 1.5f) / 1f) * 0.8f);
        }
    }
}
