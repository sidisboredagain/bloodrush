using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace BR
{
    // THE BOSSES' STORIES: a cinematic before each of the four great fights, after each death, and at the Seraph's and the
    // Warden's turns to phase 2 (ids: boss * 10 + 0 intro / 1 phase 2 / 2 defeat). The sequences: GameBossCineScenes.cs.
    //   rendered at full resolution through the cutscene camera with its own grading: ACES, bloom, vignette, grain, and a
    //   depth of field that follows whatever the camera looks at; shots dissolve into one another (the last frame of each
    //   is held and faded over the next); cameras ride smooth splines with a breath of handheld drift
    //   flashbacks are built far from the level on the cutscene layer (the opening's tools, GameIntro / GameIntroArch) and
    //   filmed while the game is frozen; present-day shots film the real arena and the real boss - frozen but alive (its
    //   own animator keeps it breathing), or with the game running underneath while every player is locked and untouchable
    //   Space / Enter / Esc votes to skip (solo: skips). Co-op (GameCoopBossCine.cs): the host starts each for everyone,
    //   keeps everyone on the same frame, counts the votes and ends it.
    // Desperation (the Frozen Traitor) -> loss (the Hollow King) -> corruption (the Seraph) -> the truth (the Warden).
    public partial class Game
    {
        class BCState { public bool active, voted; public int id, boss, kind, shot = -1; public float t; public Ent e; }
        readonly BCState BC = new BCState();
        readonly HashSet<int> bcDone = new HashSet<int>();
        int bcVotes, bcVoters = 1; bool bcGroup;
        partial void BCMayStart(ref bool ok);
        partial void BCNetStart();
        partial void BCNetTick(float rdt);
        partial void BCNetEnd();
        partial void BCNetVote();

        class BShot { public float t0, dur, fin = 0.05f, fout = 0.05f, fov = 48, xin = 0.9f; public int set = -1; public bool run, anim; public Action<float, float, float> upd; public Action start; public bool captured; }
        readonly List<BShot> bcShots = new List<BShot>();
        readonly List<(float at, float dur, string text)> bcLines = new List<(float, float, string)>();
        readonly List<(float at, string sfx, float vol)> bcCueList = new List<(float, string, float)>(); readonly HashSet<int> bcCueDone = new HashSet<int>();
        string bcTitle; float bcTitleAt = -1, bcTitleDur = 3, bcTotal, bcCur;
        class ISetEnv { public Color fogC, ambS, ambE, ambG, lightC, bg, grade = Color.white; public float fs = 60, fe = 600, lightI = 1.4f, exposure = 0, bloom = 0.8f; public Vector3 lightDir = new Vector3(-0.75f, -0.38f, -0.55f); public bool fog = true; }
        readonly Dictionary<int, ISetEnv> bcEnv = new Dictionary<int, ISetEnv>();
        bool sFog, sSunOn; Color sFogC, sAmbS, sAmbE, sAmbG, sSunC; float sFogS, sFogE, sSunI; UnityEngine.Rendering.AmbientMode sAmbM; bool bcEnvTouched;
        readonly List<GameObject> bcLive = new List<GameObject>(); // things made in the real arena for a cinematic (gone after it)
        Vector3 bcBossPos0; Quaternion bcBossRot0; Text bcVoteT; float bcShake;
        Volume bcVol; Bloom bcBloom; ColorAdjustments bcCA; Vignette bcVig; DepthOfField bcDof; FilmGrain bcGrain;
        RawImage bcXf; Texture2D bcXfTex; float bcXfT, bcXfDur = 1; bool bcCapturing;
        class IWalker { public Transform t; public Vector3 v; public IFig f; public bool fade; public float fadeT = 1; }
        readonly List<IWalker> iWalkers = new List<IWalker>();
        readonly List<IFig> iFigs = new List<IFig>();
        readonly List<(Transform t, float amp, float sp, float ph, Vector3 axis)> iSway = new List<(Transform, float, float, float, Vector3)>();
        readonly List<(Transform t, Vector3 c, float r, float sp, float ph, float y)> iBirds = new List<(Transform, Vector3, float, float, float, float)>();

        // ================= triggering =================
        bool TryBossCine(int boss, int kind, Ent e)
        {
            if (boss < 1 || boss > 5 || (kind == 1 && boss != 2 && boss != 5)) return false;
            if (testMode && Arg("-brbcine") == null) return false;
            int id = boss * 10 + kind; if (bcDone.Contains(id) || BC.active || INTRO.active || CINE.active) return bcDone.Contains(id) && kind == 0;
            bool ok = true; BCMayStart(ref ok); if (!ok) return true; // (co-op client: the host's word starts it)
            bcDone.Add(id); StartBossCine(id, 0); return true;
        }
        Ent BCEnt() => BC.kind == 2 ? bossDeath?.e : (boss ?? bossDeath?.e);
        bool BCHoldsNova => BC.active && BC.id == 22; // (the Seraph's supernova waits for her story before the ending)

        // ================= start / end =================
        void StartBossCine(int id, float at)
        {
            if (BC.active) return;
            BC.active = true; BC.id = id; BC.boss = id / 10; BC.kind = id % 10; BC.t = at; BC.shot = -1; BC.voted = false; bcVotes = 0; bcVoters = 1; bcShake = 0;
            BC.e = BCEnt(); bcDone.Add(id); bcCamFree = false;
            bcShots.Clear(); bcLines.Clear(); bcCueList.Clear(); bcCueDone.Clear(); bcEnv.Clear(); bcTitle = null; bcTitleAt = -1; bcLive.Clear(); iWalkers.Clear(); iFigs.Clear(); iSway.Clear(); iBirds.Clear(); bcCur = 0; bcEnvTouched = false; iCues.Clear();
            sFog = RenderSettings.fog; sFogC = RenderSettings.fogColor; sFogS = RenderSettings.fogStartDistance; sFogE = RenderSettings.fogEndDistance;
            sAmbM = RenderSettings.ambientMode; sAmbS = RenderSettings.ambientSkyColor; sAmbE = RenderSettings.ambientEquatorColor; sAmbG = RenderSettings.ambientGroundColor; sSunI = sunLight.intensity; sSunC = sunLight.color; sSunOn = sunLight.enabled;
            if (introRoot) Mk.DestroyTree(introRoot.gameObject);
            introRoot = new GameObject("bossCine").transform; introRoot.SetParent(world, false);
            iSets.Clear(); iSlabs.Clear(); iHalos.Clear(); iTendrils.Clear(); iGears.Clear(); iSpins.Clear(); iCracks.Clear(); iBolts.Clear(); iDebris.Clear(); iFalls.Clear(); iSoftTex = null;
            for (int i = 0; i < iParts.Length; i++) iParts[i] = null;
            if (BC.e != null && BC.e.g) { bcBossPos0 = BC.e.g.transform.position; bcBossRot0 = BC.e.g.transform.rotation; }
            ClothTextures();
            int pl = Mk.layer; bool pc = Mk.colliders; Mk.colliders = false;
            try { DefineBossCine(id); } catch (Exception ex) { Debug.LogError("boss cine " + id + ": " + ex); }
            Mk.layer = pl; Mk.colliders = pc;
            Mk.SetLayerRecursive(introRoot.gameObject, CINE_LAYER);
            foreach (var st in iSets) st.gameObject.SetActive(false);
            bcTotal = 0; foreach (var s in bcShots) bcTotal += s.dur;
            if (bcShots.Count == 0) { BC.active = false; return; }
            iCanSkip = false; IntroOverlay(); BCOverlay();
            SetCineHQ(true); if (cineVolume) cineVolume.enabled = false; BCMakeVolume();
            IArchLight(true); if (iLight) iLight.enabled = false;
            ReleaseHook(); mouseDown = false; sawHeld = false; laserHeld = false; audioMgr.Hum(0);
            SetHudVisible(false); if (vm) vm.gameObject.SetActive(false); ShowScreen(null);
            locked = true; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            BCNetStart();
        }
        void BCMakeVolume()
        {
            if (bcVol) Destroy(bcVol.gameObject);
            var go = new GameObject("bossCineVolume"); bcVol = go.AddComponent<Volume>(); bcVol.isGlobal = true; bcVol.priority = 30;
            var p = ScriptableObject.CreateInstance<VolumeProfile>(); bcVol.sharedProfile = p;
            var tm = p.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
            bcBloom = p.Add<Bloom>(true); bcBloom.intensity.Override(0.8f); bcBloom.threshold.Override(0.85f); bcBloom.scatter.Override(0.72f);
            bcCA = p.Add<ColorAdjustments>(true); bcCA.contrast.Override(14); bcCA.saturation.Override(-4); bcCA.postExposure.Override(0); bcCA.colorFilter.Override(Color.white);
            bcVig = p.Add<Vignette>(true); bcVig.intensity.Override(0.34f); bcVig.smoothness.Override(0.5f);
            bcGrain = p.Add<FilmGrain>(true); bcGrain.type.Override(FilmGrainLookup.Medium2); bcGrain.intensity.Override(0.26f); bcGrain.response.Override(0.75f);
            bcDof = p.Add<DepthOfField>(true); bcDof.mode.Override(DepthOfFieldMode.Bokeh); bcDof.focusDistance.Override(20); bcDof.aperture.Override(5.6f); bcDof.focalLength.Override(55); bcDof.bladeCount.Override(6);
        }
        void EndBossCine()
        {
            if (!BC.active) return;
            int id = BC.id; var e = BC.e; BC.active = false;
            foreach (var g in bcLive) if (g) Destroy(g); bcLive.Clear();
            if (introRoot) Mk.DestroyTree(introRoot.gameObject); introRoot = null;
            foreach (var m in iMats) if (m) Destroy(m); iMats.Clear(); foreach (var t in iTex) if (t) Destroy(t); iTex.Clear();
            iSets.Clear(); iSlabs.Clear(); iHalos.Clear(); iTendrils.Clear(); iGears.Clear(); iSpins.Clear(); iCracks.Clear(); iBolts.Clear(); iDebris.Clear(); iFalls.Clear(); iWalkers.Clear(); iFigs.Clear(); iSway.Clear(); iBirds.Clear();
            for (int i = 0; i < iParts.Length; i++) iParts[i] = null;
            IArchLight(false);
            if (bcVol) { Destroy(bcVol.sharedProfile); Destroy(bcVol.gameObject); } bcVol = null;
            if (bcXfTex) Destroy(bcXfTex); bcXfTex = null; if (bcXf) bcXf.color = new Color(1, 1, 1, 0);
            SetCineHQ(false);
            if (bcEnvTouched) BCRestoreEnv(); sunLight.intensity = sSunI; sunLight.color = sSunC; sunLight.enabled = sSunOn;
            if (e != null && e.g && !e.dead && BC.kind != 1) { e.g.transform.rotation = bcBossRot0; SyncEnt(e); }
            cineCam.enabled = false; cam.enabled = true; cineCam.fieldOfView = 50;
            SetHudVisible(true); if (vm) vm.gameObject.SetActive(true);
            iNarrT.text = iTitleT.text = ""; iNarrT.rectTransform.anchoredPosition = V(0, 0, 0); iTitleT.fontSize = 168; iTitleRT.anchoredPosition = V(0, 20, 0); iRule.anchoredPosition = V(0, -88, 0);
            iFlash.color = new Color(1, 1, 1, 0); iBarTop.gameObject.SetActive(false); iBarBot.gameObject.SetActive(false); iRule.gameObject.SetActive(false); if (bcVoteT) bcVoteT.text = "";
            INTRO.fadeOut = id == 22 ? 0 : 0.8f;
            if (started) { locked = true; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; mouseDown = false; }
            skipDelta = true;
            if (id == 10 && e != null && !e.dead && e.state == "intro" && e.cr != null) { e.risen = true; e.roared = true; e.stateT = Mathf.Min(e.stateT, 0.9f); e.cr.root.py = 0; } // Carnifex: already up out of the pit (no second burst)
            if (id == 30 && tBlock) tBlock.SetActive(true);                                                       // (the ice prison: the game finishes breaking it)
            if (id == 22) { ClearBossDeath(); StartCutscene(); }                                                  // the Seraph: after her supernova and her story, the ending
            if (id == 52 && e != null && e.g && e.g.activeSelf) { e.g.transform.position = bcBossPos0; e.g.transform.rotation = bcBossRot0; } // the Warden: his own death (the dial shattering) plays on in the hall
            if (id == 20 && e != null && e.g && !e.dead && e.state == "intro") { float y = Mathf.Clamp(e.g.transform.position.y, 10, 45), ez = (45 - y) / 35f; e.stateT = Mathf.Min(e.stateT, 3.2f * Mathf.Sqrt(1 - ez)); e.pos.y = y; } // the Seraph: her descent picks up where the cinematic left her
            if (id == 21 && e != null && e.g && !e.dead) { var to = (e.g.transform.position + V(0, 2, 0)) - (P.pos + V(0, P.eye, 0)); P.yaw = Mathf.Atan2(-to.x, -to.z); P.pitch = Mathf.Clamp(Mathf.Atan2(to.y, Hypot(to.x, to.z)), -1.4f, 1.4f); } // (the Seraph: the fight picks up looking where the camera came down)
            if (id == 51 && e != null && !e.dead && e.state == "stunned") e.stateT = Mathf.Max(e.stateT, 2.6f);  // (his reeling moment is kept for the fight)
            BCNetEnd();
        }
        void BCSkip()
        {
            if (!BC.active) return;
            if (!BC.voted) { BC.voted = true; Sfx("ui"); }
            BCNetVote();
            if (!bcGroup) EndBossCine();
        }
        void BCRestoreEnv()
        {
            RenderSettings.fog = sFog; RenderSettings.fogColor = sFogC; RenderSettings.fogStartDistance = sFogS; RenderSettings.fogEndDistance = sFogE;
            RenderSettings.ambientMode = sAmbM; RenderSettings.ambientSkyColor = sAmbS; RenderSettings.ambientEquatorColor = sAmbE; RenderSettings.ambientGroundColor = sAmbG;
        }
        void BCNextShot() { if (BC.shot >= 0 && BC.shot + 1 < bcShots.Count) BC.t = Mathf.Max(BC.t, bcShots[BC.shot + 1].t0); }

        // ================= the player =================
        bool UpdateBossCine(float rdt)
        {
            if (!BC.active) return false;
            if (KeyDown(UnityEngine.InputSystem.Key.Space) || KeyDown(UnityEngine.InputSystem.Key.Enter) || KeyDown(UnityEngine.InputSystem.Key.Escape)) BCSkip();
            if (!BC.active) return false;
            BC.t += rdt; float t = BC.t; bcShake = Mathf.Max(0, bcShake - rdt * 2); iFlashA = Mathf.Max(0, iFlashA - rdt * 2.2f);
            BCNetTick(rdt); if (!BC.active) return false;
            if (t >= bcTotal) { EndBossCine(); return false; }
            int idx = 0; for (int i = 0; i < bcShots.Count; i++) if (t >= bcShots[i].t0) idx = i;
            var sh = bcShots[idx]; float lt = t - sh.t0, u = Mathf.Clamp01(lt / sh.dur);
            if (idx != BC.shot) { int prev = BC.shot; BC.shot = idx; BCShotStart(sh, prev >= 0); }
            for (int i = 0; i < bcCueList.Count; i++) { var c = bcCueList[i]; if (t >= c.at && bcCueDone.Add(i) && t - c.at < 0.6f) Sfx(c.sfx, c.vol); }
            if (sh.anim && sh.set < 0 && !sh.run) BCAnimateBoss(BC.e, rdt);
            try { sh.upd?.Invoke(u, lt, rdt); } catch (Exception ex) { Debug.LogWarning("boss cine shot: " + ex.Message); }
            if (sh.set >= 0) { IDriftUpdate(sh.set, rdt); IWalkersUpdate(rdt); ISwayUpdate(rdt); }
            IDebrisUpdate(rdt);
            BCText(t, sh, lt);
            int nx = idx + 1; if (nx < bcShots.Count && !sh.captured && bcShots[nx].xin > 0 && sh.dur - lt < rdt * 1.6f + 0.02f) { sh.captured = true; if (!bcCapturing) StartCoroutine(BCCapture()); }
            if (bcXf) { bcXfT = Mathf.Max(0, bcXfT - rdt); float a = bcXfT / Mathf.Max(0.01f, bcXfDur); bcXf.color = new Color(1, 1, 1, a * a * (3 - 2 * a)); }
            if (!sh.run)
            {
                if (sh.set < 0) { time += rdt; UpdateParticles(rdt); UpdateDecor(time, rdt); }
                cam.enabled = false; cineCam.enabled = true; return true;
            }
            P.iframes = Mathf.Max(P.iframes, 0.3f); mouseDown = false; P.vel.x = 0; P.vel.z = 0;
            return false;
        }
        IEnumerator BCCapture()
        {
            bcCapturing = true; yield return new WaitForEndOfFrame();
            try { var tx = ScreenCapture.CaptureScreenshotAsTexture(); if (bcXfTex) Destroy(bcXfTex); bcXfTex = tx; } catch (Exception ex) { Debug.LogWarning("dissolve: " + ex.Message); }
            bcCapturing = false;
        }
        void BCAnimateBoss(Ent e, float dt)
        {
            if (e == null || !e.g || e.dead) return;
            try
            {
                if (e.boss == 1 && e.cr != null) AnimateCarnifex(e, dt);
                else if (e.boss == 3 && e.tr != null) AnimateTraitor(e, dt);
                else if (e.boss == 4 && e.hk != null) AnimateHollow(e, dt);
                else if (e.boss == 2 && e.sr != null) AnimateSeraph(e, dt);
                else if (e.boss == 5 && e.wd != null) AnimateWarden(e, dt, 1);
            }
            catch (Exception) { }
        }
        void BossCineFrameEnd() { if (!BC.active) return; cam.enabled = false; cineCam.enabled = true; }
        void BCShotStart(BShot sh, bool dissolve)
        {
            foreach (var st in iSets) st.gameObject.SetActive(false);
            if (sh.set >= 0 && sh.set < iSets.Count)
            {
                iSets[sh.set].gameObject.SetActive(true);
                var env = bcEnv.TryGetValue(sh.set, out var ev) ? ev : new ISetEnv { fogC = Color.black, ambS = Color.gray, ambE = Color.gray, ambG = Color.black, lightC = Color.white, bg = Color.black };
                RenderSettings.fog = env.fog; RenderSettings.fogColor = env.fogC; RenderSettings.fogStartDistance = env.fs; RenderSettings.fogEndDistance = env.fe;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight; IArchAmbient(env.ambS, env.ambE, env.ambG); bcEnvTouched = true;
                if (iLight) { iLight.enabled = true; iLight.color = env.lightC; iLight.intensity = env.lightI; iLight.transform.rotation = Quaternion.LookRotation(env.lightDir.normalized); }
                sunLight.enabled = false;
                cineCam.cullingMask = 1 << CINE_LAYER; cineCam.clearFlags = CameraClearFlags.SolidColor; cineCam.backgroundColor = env.bg;
                if (bcCA) { bcCA.colorFilter.Override(env.grade); bcCA.postExposure.Override(env.exposure); } if (bcBloom) bcBloom.intensity.Override(env.bloom);
            }
            else
            {
                if (bcEnvTouched) { BCRestoreEnv(); bcEnvTouched = false; }
                if (iLight) iLight.enabled = false; sunLight.enabled = true;
                cineCam.cullingMask = ~((1 << CINE_LAYER) | (1 << CINE_SKY) | (1 << VM_ENV_LAYER)); cineCam.clearFlags = CameraClearFlags.SolidColor; cineCam.backgroundColor = cam.backgroundColor;
                if (bcCA) { bcCA.colorFilter.Override(BC.boss == 1 ? Mats.Hex(0xffe2cc) : BC.boss == 3 ? Mats.Hex(0xdce8ff) : BC.boss == 4 ? Mats.Hex(0xffe8d8) : BC.boss == 2 ? Mats.Hex(0xfff4e0) : Mats.Hex(0xf0ece4)); bcCA.postExposure.Override(0.15f); }
                if (bcBloom) bcBloom.intensity.Override(1.0f);
            }
            cineCam.fieldOfView = sh.fov;
            if (dissolve && sh.xin > 0 && bcXfTex && bcXf) { bcXf.texture = bcXfTex; bcXfDur = sh.xin; bcXfT = sh.xin; bcXf.color = Color.white; }
            sh.start?.Invoke();
        }
        void BCOverlay()
        {
            iBarTop.gameObject.SetActive(true); iBarBot.gameObject.SetActive(true);
            iNarrT.rectTransform.anchoredPosition = V(0, -318, 0);
            iTitleT.fontSize = 104; iTitleRT.anchoredPosition = V(0, -10, 0); iRule.anchoredPosition = V(0, -78, 0); iRule.localScale = V(0, 1, 1);
            if (!bcVoteT) { bcVoteT = Txt(introUI, "", 15, C("#8a7a72"), TextAnchor.MiddleRight, true, 4, 1); Place(bcVoteT.rectTransform, 1, 0, 1, 0, -34, 26, 600, 30); }
            if (!bcXf) { bcXf = Img(introUI, Texture2D.whiteTexture, new Color(1, 1, 1, 0), "bcDissolve"); Fill(bcXf.rectTransform); }
            bcXf.transform.SetAsFirstSibling(); bcXf.color = new Color(1, 1, 1, 0); bcXfT = 0;
            bcVoteT.transform.SetAsLastSibling(); bcVoteT.text = "";
        }
        void BCText(float t, BShot sh, float lt)
        {
            float f = 1 - IWin(lt, 0, sh.dur, sh.fin, sh.fout);
            iFade.color = new Color(0, 0, 0, f);
            iFlash.color = new Color(0.88f, 0.92f, 1, iFlashA);
            string line = ""; float la = 0;
            foreach (var l in bcLines) if (t >= l.at && t < l.at + l.dur) { line = l.text; la = IWin(t, l.at, l.at + l.dur, 0.55f, 0.6f); }
            iNarrT.text = line; iNarrT.color = new Color(0.94f, 0.9f, 0.84f, la); iNarrT.rectTransform.anchoredPosition = V(0, -318 + (1 - la) * -8, 0);
            if (bcTitle != null && t >= bcTitleAt && t < bcTitleAt + bcTitleDur)
            {
                float tt = t - bcTitleAt, a = Mathf.Clamp01(tt / 0.15f) * Mathf.Clamp01((bcTitleAt + bcTitleDur - t) / 0.7f);
                iTitleT.text = bcTitle; iTitleT.color = new Color(0.86f, 0.1f, 0.12f, a); iTitleRT.localScale = Vector3.one * (Mathf.Lerp(1.3f, 1, Smoother(Mathf.Clamp01(tt / 0.22f))) + tt * 0.01f);
                iRule.gameObject.SetActive(true); iRule.localScale = V(Smoother(Mathf.Clamp01((tt - 0.2f) / 0.7f)), 1, 1); iRule.GetComponent<Graphic>().color = new Color(0.78f, 0.08f, 0.1f, a);
            }
            else { iTitleT.text = ""; iRule.gameObject.SetActive(false); }
            if (bcVoteT) { bcVoteT.text = bcGroup ? (BC.voted ? $"WAITING TO SKIP  {bcVotes}/{bcVoters}" : $"SPACE  VOTE TO SKIP  {bcVotes}/{bcVoters}") : "SPACE  SKIP"; bcVoteT.color = new Color(0.6f, 0.52f, 0.48f, Mathf.Clamp01((t - 1) / 0.8f) * 0.8f); }
        }

        // ================= the camera =================
        static Vector3 CRom(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t) { float t2 = t * t, t3 = t2 * t; return 0.5f * ((2 * b) + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t2 + (-a + 3 * b - 3 * c + d) * t3); }
        static Vector3 Spline(Vector3[] p, float u)
        {
            if (p.Length == 1) return p[0]; if (p.Length == 2) return Vector3.Lerp(p[0], p[1], u);
            float f = Mathf.Clamp01(u) * (p.Length - 1); int i = Mathf.Min((int)f, p.Length - 2); float t = f - i;
            return CRom(p[Mathf.Max(0, i - 1)], p[i], p[i + 1], p[Mathf.Min(p.Length - 1, i + 2)], t);
        }
        static float BEase(float u) { u = Mathf.Clamp01(u); float s = u * u * (3 - 2 * u); return Mathf.Lerp(u, s, 0.65f); }
        void BPath(Vector3[] p, Vector3[] l, float u, float shake = 0, float roll = 0, float hand = 1)
        {
            float e = BEase(u); var pos = Spline(p, e); var look = Spline(l, e);
            if (BC.shot >= 0 && BC.shot < bcShots.Count && bcShots[BC.shot].set < 0)
            {
                pos = BCInArena(pos); // (a shot of the real arena never puts the camera out past its walls...)
                if (!bcCamFree && Physics.Linecast(look, pos, out var hit, 1 << Mk.LayerLevel, QueryTriggerInteraction.Ignore)) pos = hit.point + hit.normal * 0.6f; // (...or inside a column or a wall: it stops in front of it)
            }
            float sh = shake + bcShake; if (sh > 0) pos += V(Mathf.PerlinNoise(BC.t * 9, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, BC.t * 9) - 0.5f, Mathf.PerlinNoise(BC.t * 7, 5) - 0.5f) * sh;
            CineLook(pos, look);
            float hx = (Mathf.PerlinNoise(BC.t * 0.45f, 11) - 0.5f) * 0.9f * hand, hy = (Mathf.PerlinNoise(13, BC.t * 0.4f) - 0.5f) * 0.9f * hand;
            cineCam.transform.rotation = cineCam.transform.rotation * Quaternion.Euler(hx, hy, roll + (Mathf.PerlinNoise(BC.t * 0.3f, 17) - 0.5f) * 0.5f * hand);
            if (bcDof) bcDof.focusDistance.Override(Mathf.Max(0.5f, Vector3.Distance(pos, look)));
        }
        bool bcCamFree; // (set by a shot that must reach an exact spot, e.g. your own eyes, whatever stands in the way)
        Vector3 BCInArena(Vector3 p) { float lim = LB - 3; var h = V(p.x, 0, p.z); if (h.magnitude > lim) { h = h.normalized * lim; p.x = h.x; p.z = h.z; } return p; }
        void BCam(Vector3 a, Vector3 b, Vector3 la, Vector3 lb, float u, float shake = 0, float roll = 0) => BPath(new[] { a, b }, new[] { la, lb }, u, shake, roll);
        static Vector3[] VA(params Vector3[] p) => p;

        float BShotAt(float dur, int set, bool run, Action<float, float, float> upd, float fin = 0.05f, float fout = 0.05f, Action start = null, float fov = 48, float xin = 0.9f, bool anim = false)
        {
            float t0 = bcCur; bcShots.Add(new BShot { t0 = t0, dur = dur, set = set, run = run, upd = upd, fin = fin, fout = fout, start = start, fov = fov, xin = xin, anim = anim }); bcCur += dur; return t0;
        }
        void BSay(float at, float dur, string text, string voice) { bcLines.Add((at, dur, text)); if (voice != null) { BCue(at, voice, 0.9f); BCue(at + 0.35f, voice, 0.3f); } }
        void BCue(float at, string sfx, float vol = 1) => bcCueList.Add((at, sfx, vol));
        void BTitle(float at, string name, float dur = 3.2f) { bcTitle = name; bcTitleAt = at; bcTitleDur = dur; BCue(at, "iTitle", 0.75f); BCue(at + 0.05f, "iBeat", 0.6f); }
        void BEnv(int set, int fog, float fs, float fe, int sky, int eq, int gr, int light, float li, int bg, Vector3? dir = null, int grade = 0xffffff, float exposure = 0, float bloom = 0.8f) =>
            bcEnv[set] = new ISetEnv { fogC = Mats.Hex(fog), fs = fs, fe = fe, ambS = Mats.Hex(sky), ambE = Mats.Hex(eq), ambG = Mats.Hex(gr), lightC = Mats.Hex(light), lightI = li, bg = Mats.Hex(bg), lightDir = dir ?? new Vector3(-0.75f, -0.38f, -0.55f), grade = Mats.Hex(grade), exposure = exposure, bloom = bloom };

        // ================= shared helpers =================
        void IDriftUpdate(int set, float rdt)
        {
            if (set < 0 || set >= iParts.Length || iParts[set] == null) return;
            foreach (var p in iParts[set])
            {
                if (!p.t) continue; var q = p.t.position + p.v * rdt; var d = q - p.home;
                if (d.x > p.box.x) q.x -= 2 * p.box.x; else if (d.x < -p.box.x) q.x += 2 * p.box.x;
                if (d.y > p.box.y) q.y -= 2 * p.box.y; else if (d.y < -p.box.y) q.y += 2 * p.box.y;
                if (d.z > p.box.z) q.z -= 2 * p.box.z; else if (d.z < -p.box.z) q.z += 2 * p.box.z;
                p.t.position = q;
            }
        }
        void IDebrisUpdate(float rdt)
        {
            for (int i = iDebris.Count - 1; i >= 0; i--)
            {
                var db = iDebris[i]; if (!db.t) { iDebris.RemoveAt(i); continue; }
                db.v.y -= 32 * rdt; db.t.position += db.v * rdt; db.t.Rotate(db.spin * rdt); db.life -= rdt;
                if (db.life <= 0) { db.t.gameObject.SetActive(false); iDebris.RemoveAt(i); }
            }
        }
        void ISwayUpdate(float rdt)
        {
            foreach (var s in iSway) if (s.t) s.t.localRotation = Quaternion.AngleAxis(Mathf.Sin(BC.t * s.sp + s.ph) * s.amp + Mathf.Sin(BC.t * s.sp * 2.3f + s.ph * 1.7f) * s.amp * 0.3f, s.axis);
            foreach (var b in iBirds) if (b.t) { float a = BC.t * b.sp + b.ph; b.t.position = b.c + V(Mathf.Cos(a) * b.r, b.y + Mathf.Sin(a * 2.1f) * 3, Mathf.Sin(a) * b.r); b.t.rotation = Quaternion.LookRotation(V(-Mathf.Sin(a), 0, Mathf.Cos(a)) * Mathf.Sign(b.sp)) * Quaternion.Euler(0, 0, Mathf.Sin(BC.t * 14 + b.ph) * 30); }
        }
        void IWalkersUpdate(float rdt)
        {
            foreach (var w in iWalkers)
            {
                if (!w.t || !w.t.gameObject.activeInHierarchy) continue;
                if (w.fade) { w.fadeT = Mathf.Max(0, w.fadeT - rdt * 1.4f); w.t.localScale = V(Mathf.Max(0.001f, w.fadeT), Mathf.Max(0.001f, w.fadeT * w.fadeT), Mathf.Max(0.001f, w.fadeT)); if (w.fadeT <= 0) { w.t.gameObject.SetActive(false); continue; } }
                w.t.position += w.v * rdt;
                if (w.v.sqrMagnitude > 0.01f) w.t.rotation = Quaternion.Slerp(w.t.rotation, Quaternion.LookRotation(V(w.v.x, 0, w.v.z)), 1 - Mathf.Exp(-6 * rdt));
                if (w.f != null) w.f.speed = w.v.magnitude;
            }
            foreach (var f in iFigs) IFigAnimate(f, rdt);
        }
        void IWindow(Transform root, float x, float y, float z, float w, float h, Material m) { IMsh(root, Geo.Box(w, h, 0.05f), m, L(root, x, y, z), root.eulerAngles); }
        void IArchMatsIce()
        {
            iMarble = IM(Mats.Std(0xffffff, 0.35f, 0.05f, 0, IStoneTex(0xdce8f2, 0x9ab4cc, false, 11)));
            iStone = IM(Mats.Std(0xffffff, 0.6f, 0, 0, IStoneTex(0xa8bccc, 0x7a90a4, true, 12)));
            iStoneDk = IM(Mats.Std(0xffffff, 0.7f, 0, 0, IStoneTex(0x6a7c8c, 0x4a5a6a, true, 13)));
            iGold2 = IM(Mats.Std(0xc8dcf0, 0.25f, 0.85f)); iBronze = IM(Mats.Std(0x8aa4c0, 0.35f, 0.7f));
            iRoof = IM(Mats.Std(0xffffff, 0.8f, 0, 0, IStoneTex(0xf0f4f8, 0xc8d4e0, false, 14))); iDoorM = IM(Mats.Std(0x10141a, 0.9f));
            iCrackM = IM(Mats.Add(0x60a0ff, 0f, Tex.Cracks, true)); iDebM = iStone; ITile();
        }
        void IArchMatsHeaven()
        {
            iMarble = IM(Mats.Std(0xffffff, 0.3f, 0.05f, 0, IStoneTex(0xfcf8f0, 0xd8c8a8, false, 21)));
            iStone = IM(Mats.Std(0xffffff, 0.5f, 0, 0, IStoneTex(0xf0e8d8, 0xc8b890, true, 22)));
            iStoneDk = IM(Mats.Std(0xffffff, 0.6f, 0, 0, IStoneTex(0xd8ccb4, 0xa89870, true, 23)));
            iGold2 = IM(Mats.Std(0xf0c050, 0.2f, 0.95f)); iBronze = IM(Mats.Std(0xe8b848, 0.25f, 0.9f));
            iRoof = IM(Mats.Std(0xe8c060, 0.3f, 0.8f)); iDoorM = IM(Mats.Std(0x2a2418, 0.9f));
            iCrackM = IM(Mats.Add(0xffc040, 0f, Tex.Cracks, true)); iDebM = iStone; ITile();
        }
        void ISnow(int set, Vector3 c, Vector3 box, int n, int color = 0xffffff, float size = 0.5f, Vector3? vel = null, float op = 0.8f)
        {
            var rng = new System.Random(set * 7 + n); float R(float a, float b) => a + (b - a) * (float)rng.NextDouble();
            var s = iSets[set];
            for (int i = 0; i < n; i++) { var f = IGlow(s, c + V(R(-box.x, box.x), R(-box.y, box.y), R(-box.z, box.z)), color, R(size * 0.5f, size), op).transform; var v = vel ?? V(R(-1.5f, -0.5f), R(-3, -1.5f), R(-0.5f, 0.5f)); IDrift(set, f, v * R(0.7f, 1.3f), c, box); }
        }
        void IBanner(Transform p, Vector3 pos, float h, Material cloth, Material pole, float wind = 1)
        {
            IA(p, Geo.Cyl(0.12f, 0.15f, h, 6), pole, pos + V(0, h / 2, 0)); IA(p, Geo.Sphere(0.3f, 8, 6), iGold2, pos + V(0, h, 0));
            var a = IRoot(p, pos + V(0.15f, h - 0.4f, 0), 90); IA(a, Geo.Box(0.05f, h * 0.3f, h * 0.2f), cloth, L(a, 0, -h * 0.15f, h * 0.1f));
            var b = IRoot(a, L(a, 0, -h * 0.3f, 0), 90); IA(b, Geo.Box(0.05f, h * 0.22f, h * 0.2f), cloth, L(b, 0, -h * 0.11f, h * 0.1f));
            float ph = pos.x * 0.13f + pos.z * 0.07f; iSway.Add((a, 9 * wind, 1.6f, ph, Vector3.up)); iSway.Add((b, 14 * wind, 2.2f, ph + 0.8f, Vector3.up));
        }
        void ILantern(Transform p, Vector3 pos, Material iron, int color = 0xffb060)
        {
            IA(p, Geo.Cyl(0.1f, 0.14f, 3.4f, 6), iron, pos + V(0, 1.7f, 0)); IA(p, Geo.Box(0.5f, 0.6f, 0.5f), iron, pos + V(0, 3.6f, 0));
            IMsh(p, Geo.Box(0.4f, 0.45f, 0.4f), IM(Mats.Basic(color)), pos + V(0, 3.6f, 0)); IGlow(p, pos + V(0, 3.6f, 0), color, 4.5f, 0.55f);
        }
        void IBirdFlock(Transform s, Vector3 c, int n, float r, int color, float scale = 1)
        {
            var m = IM(Mats.Basic(color)); var rng = new System.Random(n + (int)c.x);
            for (int i = 0; i < n; i++) { var b = new GameObject("bird").transform; b.SetParent(s, false); foreach (var sx in new[] { -1f, 1f }) IMsh(b, Geo.Box(0.9f * scale, 0.05f * scale, 0.3f * scale), m, Vector3.zero, V(0, 0, sx * 18)).transform.localPosition = V(sx * 0.42f * scale, 0, 0); iBirds.Add((b, c, r * (0.7f + (float)rng.NextDouble() * 0.6f), (0.15f + (float)rng.NextDouble() * 0.12f) * (rng.NextDouble() < 0.5 ? 1 : -1), (float)rng.NextDouble() * 6, (float)rng.NextDouble() * 20)); }
        }
        void IChimney(int set, Vector3 pos, Material smoke, float size = 6, float rise = 4)
        {
            var s = iSets[set]; var rng = new System.Random((int)(pos.x * 7 + pos.z));
            for (int i = 0; i < 5; i++) { var p = IBill(s, pos + V(0, i * size * 0.8f, 0), size * (1 + i * 0.4f), size * (1 + i * 0.4f), smoke).transform; IDrift(set, p, V((float)rng.NextDouble() * 1.2f, rise, 0), pos + V(0, size * 2, 0), V(size * 2, size * 2.2f, size * 2)); }
        }
        List<IFall> ITakeFalls() { var l = new List<IFall>(iFalls); iFalls.Clear(); return l; }
        void IFallsAt(List<IFall> list, float lt)
        {
            foreach (var f in list)
            {
                if (!f.t) continue; float p = Mathf.Clamp01((lt - f.at) / 1.8f); p = p * p;
                f.t.localRotation = f.rot0 * Quaternion.AngleAxis(f.ang * p, f.axis); f.t.localPosition = f.pos0 - V(0, f.sink * p, 0);
                if (!f.shed && p > 0.05f && lt > 0) { f.shed = true; if (iDebris.Count < 220) IDebrisAt(f.t.parent, f.t.position + V(0, f.h * 0.5f, 0), f.h > 30 ? 12 : 4, 9, f.h > 30 ? 3 : 1.5f, new System.Random((int)(f.at * 1000))); }
            }
        }
        Material BCBlack() => INF(Mats.Basic(0x000000));
        Vector3 BCFwd(Ent e) { var d = P.pos - e.pos; d.y = 0; return d.sqrMagnitude > 1 ? d.normalized : Vector3.forward; }

        // ================= the people =================
        // An articulated figure: legs (thigh, knee, boot), hips, torso and chest, neck and head with a face (brow, nose,
        // eyes, a beard, hair, a hood or a helm), arms in two parts with hands; dressed as a king (long layered robe, fur
        // mantle, gilt pauldrons, medallion and belt, a jewelled crown, a cape of hinged panels), a guard (helm and crest,
        // breastplate, short cloak, shield, spear) or a citizen (tunic and trousers, or a long dress and a headscarf; a child;
        // some carry lanterns). Every joint eases toward its pose - walking, standing and breathing, looking, reaching,
        // kneeling - nothing snaps, nothing stands stock still; capes ripple with the wind and with speed.
        class IFig
        {
            public Transform root, hips, torso, chest, neck, head, shL, elL, shR, elR, thL, knL, thR, knR; public List<Transform> cape = new List<Transform>();
            public float h, ph, speed, breath, lean, lookYaw, lookPitch, armR, armL, elbR = 0.25f, elbL = 0.25f, kneel, armSpreadR, armSpreadL, wind = 1; public bool robe;
            public float cLook, cPitch, cArmR, cArmL, cElR = 0.25f, cElL = 0.25f, cLean, cKneel, cSpR, cSpL;
        }
        enum FigKind { King, Guard, Man, Woman, Child }
        IFig IFigure(Transform p, Vector3 pos, float yaw, FigKind kind, float h, int[] colors, System.Random rng, bool lantern = false, Material crownM = null)
        {
            var f = new IFig { h = h, ph = (float)rng.NextDouble() * 6, breath = (float)rng.NextDouble() * 6, robe = kind == FigKind.King || kind == FigKind.Woman };
            Material Cloth(int c, float rough = 0.85f) => IM(Mats.Std(c, rough, 0, 0, fabricTex));
            int ci = 0; int Col() => colors[(ci++) % colors.Length];
            var skin = IM(Mats.Std(new[] { 0xe8c0a0, 0xd8a888, 0xc08a68, 0x9a6a4a, 0x7a5038 }[rng.Next(5)], 0.65f));
            var hair = IM(Mats.Std(new[] { 0x1a1410, 0x3a2418, 0x6a4a2a, 0x8a7a6a, 0xc8c0b8 }[rng.Next(5)], 0.8f));
            var main = Cloth(Col()); var second = Cloth(Col()); var dark = Cloth(0x2a2420); var metal = IM(Mats.Std(kind == FigKind.Guard ? 0x9aa0a8 : 0xd8a848, 0.3f, 0.85f));
            f.root = IRoot(p, pos, yaw);
            f.hips = IRoot(f.root, L(f.root, 0, h * 0.52f, 0), yaw);
            foreach (var sx in new[] { -1f, 1f })
            {
                var th = IRoot(f.hips, L(f.hips, sx * h * 0.06f, 0, 0), yaw);
                IA(th, Geo.Cyl(h * 0.045f, h * 0.055f, h * 0.25f, 8), f.robe ? dark : second, L(th, 0, -h * 0.125f, 0));
                var kn = IRoot(th, L(th, 0, -h * 0.25f, 0), yaw);
                IA(kn, Geo.Cyl(h * 0.035f, h * 0.045f, h * 0.23f, 8), f.robe ? dark : second, L(kn, 0, -h * 0.115f, 0));
                IA(kn, Geo.Box(h * 0.07f, h * 0.05f, h * 0.13f), IM(Mats.Std(0x2a1a10, 0.8f)), L(kn, 0, -h * 0.25f, h * 0.025f), V(0, yaw, 0));
                if (sx < 0) { f.thL = th; f.knL = kn; } else { f.thR = th; f.knR = kn; }
            }
            if (f.robe)
            {
                IA(f.hips, Geo.Cyl(h * 0.13f, h * 0.23f, h * 0.5f, 16), main, L(f.hips, 0, -h * 0.25f, 0));
                IA(f.hips, Geo.Torus(h * 0.22f, h * 0.012f, 20, 4), kind == FigKind.King ? metal : second, L(f.hips, 0, -h * 0.49f, 0), V(90, yaw, 0));
                if (kind == FigKind.King) IA(f.hips, Geo.Box(h * 0.1f, h * 0.46f, h * 0.02f), second, L(f.hips, 0, -h * 0.24f, h * 0.165f), V(-14, yaw, 0));
            }
            else IA(f.hips, Geo.Cyl(h * 0.12f, h * 0.15f, h * 0.12f, 12), main, L(f.hips, 0, -h * 0.04f, 0));
            f.torso = IRoot(f.hips, L(f.hips, 0, 0, 0), yaw);
            IA(f.torso, Geo.Cyl(h * 0.1f, h * 0.115f, h * 0.15f, 12), main, L(f.torso, 0, h * 0.075f, 0));
            IA(f.torso, Geo.Torus(h * 0.11f, h * 0.015f, 16, 4), kind == FigKind.Guard ? dark : metal, L(f.torso, 0, h * 0.02f, 0), V(90, yaw, 0));
            f.chest = IRoot(f.torso, L(f.torso, 0, h * 0.15f, 0), yaw);
            IA(f.chest, Geo.Sphere(1, 12, 10), kind == FigKind.Guard ? metal : main, L(f.chest, 0, h * 0.07f, 0), V(0, yaw, 0), V(h * 0.135f, h * 0.11f, h * 0.085f));
            if (kind == FigKind.King)
            {
                IA(f.chest, Geo.Torus(h * 0.12f, h * 0.04f, 20, 8), IM(Mats.Std(0xe8e4dc, 0.95f, 0, 0, knitTex)), L(f.chest, 0, h * 0.15f, -h * 0.01f), V(90, yaw, 0), V(1, 1, 0.8f));
                foreach (var sx in new[] { -1f, 1f }) IA(f.chest, Geo.Sphere(h * 0.055f, 10, 6, 0, Mathf.PI * 2, 0, Mathf.PI / 2), metal, L(f.chest, sx * h * 0.135f, h * 0.13f, 0));
                IA(f.chest, Geo.Cyl(h * 0.03f, h * 0.03f, h * 0.008f, 10), metal, L(f.chest, 0, h * 0.06f, h * 0.088f), V(90, yaw, 0));
            }
            if (kind == FigKind.Guard) IA(f.chest, Geo.Box(h * 0.2f, h * 0.02f, h * 0.17f), dark, L(f.chest, 0, 0, 0), V(0, yaw, 0));
            f.neck = IRoot(f.chest, L(f.chest, 0, h * 0.15f, 0), yaw);
            IA(f.neck, Geo.Cyl(h * 0.03f, h * 0.035f, h * 0.05f, 8), skin, L(f.neck, 0, h * 0.02f, 0));
            f.head = IRoot(f.neck, L(f.neck, 0, h * 0.045f, 0), yaw);
            IA(f.head, Geo.Sphere(1, 14, 12), skin, L(f.head, 0, h * 0.065f, 0), V(0, yaw, 0), V(h * 0.058f, h * 0.072f, h * 0.064f));
            IA(f.head, Geo.Box(h * 0.016f, h * 0.026f, h * 0.022f), skin, L(f.head, 0, h * 0.06f, h * 0.062f), V(10, yaw, 0));
            IA(f.head, Geo.Box(h * 0.085f, h * 0.012f, h * 0.02f), hair, L(f.head, 0, h * 0.085f, h * 0.052f), V(0, yaw, 0));
            foreach (var sx in new[] { -1f, 1f }) IA(f.head, Geo.Sphere(h * 0.008f, 6, 4), IM(Mats.Std(0x1a1410, 0.4f)), L(f.head, sx * h * 0.022f, h * 0.074f, h * 0.056f));
            bool beard = kind == FigKind.King || (kind == FigKind.Man && rng.NextDouble() < 0.5) || (kind == FigKind.Guard && rng.NextDouble() < 0.4);
            if (beard) IA(f.head, Geo.Cone(h * 0.04f, h * 0.07f, 8), hair, L(f.head, 0, h * 0.01f, h * 0.045f), V(180 - 15, yaw, 0));
            if (kind == FigKind.Guard)
            {
                IA(f.head, Geo.Sphere(h * 0.07f, 12, 8, 0, Mathf.PI * 2, 0, Mathf.PI * 0.6f), metal, L(f.head, 0, h * 0.07f, 0));
                IA(f.head, Geo.Box(h * 0.012f, h * 0.05f, h * 0.012f), metal, L(f.head, 0, h * 0.06f, h * 0.068f), V(0, yaw, 0));
                IA(f.head, Geo.Box(h * 0.012f, h * 0.05f, h * 0.09f), Cloth(0x8a1a1a), L(f.head, 0, h * 0.15f, -h * 0.01f), V(0, yaw, 0));
            }
            else if (kind == FigKind.Woman) IA(f.head, Geo.Sphere(h * 0.072f, 12, 8, 0, Mathf.PI * 2, 0, Mathf.PI * 0.62f), second, L(f.head, 0, h * 0.072f, -h * 0.008f), V(-8, yaw, 0), V(1, 1.1f, 1.05f));
            else IA(f.head, Geo.Sphere(h * 0.064f, 12, 8, 0, Mathf.PI * 2, 0, Mathf.PI * 0.5f), hair, L(f.head, 0, h * 0.075f, -h * 0.006f));
            if (kind == FigKind.King)
            {
                var cm = crownM ?? metal;
                IA(f.head, Geo.Torus(h * 0.06f, h * 0.012f, 20, 5), cm, L(f.head, 0, h * 0.125f, 0), V(90, yaw, 0));
                for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; IA(f.head, Geo.Cone(h * 0.012f, h * (k % 2 == 0 ? 0.05f : 0.035f), 4), cm, L(f.head, Mathf.Cos(a) * h * 0.06f, h * 0.15f, Mathf.Sin(a) * h * 0.06f)); if (k % 2 == 0) IMsh(f.head, Geo.Sphere(h * 0.008f, 6, 4), IM(Mats.Basic(k == 0 ? 0xff3040 : 0x60a0ff)), L(f.head, Mathf.Cos(a) * h * 0.062f, h * 0.128f, Mathf.Sin(a) * h * 0.062f)); }
            }
            foreach (var sx in new[] { -1f, 1f })
            {
                var sh = IRoot(f.chest, L(f.chest, sx * h * 0.135f, h * 0.12f, 0), yaw);
                IA(sh, Geo.Cyl(h * 0.032f, h * 0.04f, h * 0.17f, 8), kind == FigKind.Guard ? metal : main, L(sh, 0, -h * 0.085f, 0));
                var el = IRoot(sh, L(sh, 0, -h * 0.17f, 0), yaw);
                IA(el, Geo.Cyl(h * 0.028f, h * 0.034f, h * 0.16f, 8), main, L(el, 0, -h * 0.08f, 0));
                if (f.robe) IA(el, Geo.Cyl(h * 0.05f, h * 0.035f, h * 0.08f, 10), main, L(el, 0, -h * 0.13f, 0));
                IA(el, Geo.Sphere(1, 8, 6), kind == FigKind.King || kind == FigKind.Guard ? metal : skin, L(el, 0, -h * 0.18f, 0), V(0, yaw, 0), V(h * 0.028f, h * 0.038f, h * 0.02f));
                if (sx < 0) { f.shL = sh; f.elL = el; } else { f.shR = sh; f.elR = el; }
            }
            if (kind == FigKind.Guard)
            {
                IA(f.elL, Geo.Cyl(h * 0.13f, h * 0.13f, h * 0.02f, 16), Cloth(0x6a1a1a), L(f.elL, -h * 0.03f, -h * 0.1f, 0), V(0, yaw, 90));
                IA(f.elL, Geo.Sphere(h * 0.025f, 8, 6), metal, L(f.elL, -h * 0.045f, -h * 0.1f, 0));
                IA(f.elR, Geo.Box(h * 0.015f, h * 1.2f, h * 0.015f), IM(Mats.Std(0x4a3420, 0.8f)), L(f.elR, 0, -h * 0.1f, 0)); IA(f.elR, Geo.Cone(h * 0.025f, h * 0.09f, 4), metal, L(f.elR, 0, h * 0.54f, 0));
            }
            if (lantern) { var lm = IM(Mats.Basic(0xffb060)); IMsh(f.elR, Geo.Box(h * 0.05f, h * 0.07f, h * 0.05f), lm, L(f.elR, 0, -h * 0.25f, 0)); IGlow(f.elR, L(f.elR, 0, -h * 0.25f, 0), 0xffb060, h * 1.4f, 0.5f); }
            if (kind == FigKind.King || kind == FigKind.Guard)
            {
                var capeM = Cloth(kind == FigKind.King ? Col() : 0x3a2a2a, 0.9f); int n = kind == FigKind.King ? 5 : 3; float len = kind == FigKind.King ? h * 0.78f : h * 0.42f;
                for (int i = 0; i < n; i++)
                {
                    float x = (i - (n - 1) / 2f) * h * 0.07f;
                    var c1 = IRoot(f.chest, L(f.chest, x, h * 0.14f, -h * 0.075f), yaw); IA(c1, Geo.Box(h * 0.075f, len * 0.5f, h * 0.012f), capeM, L(c1, 0, -len * 0.25f, -h * 0.01f), V(0, yaw, 0));
                    var c2 = IRoot(c1, L(c1, 0, -len * 0.5f, -h * 0.01f), yaw); IA(c2, Geo.Box(h * 0.078f, len * 0.5f, h * 0.012f), capeM, L(c2, 0, -len * 0.25f, 0), V(0, yaw, 0));
                    f.cape.Add(c1); f.cape.Add(c2);
                }
            }
            iFigs.Add(f); return f;
        }
        void IFigAnimate(IFig f, float dt)
        {
            if (f == null || !f.root || !f.root.gameObject.activeInHierarchy) return;
            float k = 1 - Mathf.Exp(-5 * dt), sp = Mathf.Min(2.5f, f.speed / 1.6f), stride = Mathf.Clamp01(sp);
            f.ph += dt * (1.6f + f.speed * 2.6f); f.breath += dt * 1.4f;
            float s = Mathf.Sin(f.ph), c = Mathf.Cos(f.ph);
            f.cLook = Mathf.Lerp(f.cLook, f.lookYaw, k * 0.6f); f.cPitch = Mathf.Lerp(f.cPitch, f.lookPitch, k * 0.6f); f.cArmR = Mathf.Lerp(f.cArmR, f.armR, k); f.cArmL = Mathf.Lerp(f.cArmL, f.armL, k);
            f.cElR = Mathf.Lerp(f.cElR, f.elbR, k); f.cElL = Mathf.Lerp(f.cElL, f.elbL, k); f.cLean = Mathf.Lerp(f.cLean, f.lean, k * 0.7f); f.cKneel = Mathf.Lerp(f.cKneel, f.kneel, k * 0.6f); f.cSpR = Mathf.Lerp(f.cSpR, f.armSpreadR, k); f.cSpL = Mathf.Lerp(f.cSpL, f.armSpreadL, k);
            float kn = f.cKneel;
            f.thL.localRotation = Quaternion.Euler(-s * 32 * stride - kn * 80, 0, 0); f.thR.localRotation = Quaternion.Euler(s * 32 * stride + kn * 10, 0, 0);
            f.knL.localRotation = Quaternion.Euler(Mathf.Max(0, s) * 50 * stride + kn * 80, 0, 0); f.knR.localRotation = Quaternion.Euler(Mathf.Max(0, -s) * 50 * stride + kn * 100, 0, 0);
            f.hips.localPosition = V(0, f.h * 0.52f - Mathf.Abs(c) * f.h * 0.012f * stride - kn * f.h * 0.2f, 0);
            float br = Mathf.Sin(f.breath) * 0.012f;
            f.torso.localRotation = Quaternion.Euler(f.cLean * 25 + 4 * stride + Mathf.Sin(f.breath * 0.5f) * 0.8f, s * 5 * stride, 0);
            f.chest.localScale = V(1 + br, 1 + br * 0.5f, 1 + br);
            f.head.localRotation = Quaternion.Euler(f.cPitch, f.cLook + Mathf.Sin(f.breath * 0.37f + f.ph) * 3, Mathf.Sin(f.breath * 0.23f) * 1.5f);
            f.shR.localRotation = Quaternion.Euler(-f.cArmR + s * 25 * stride * (1 - Mathf.Clamp01(f.cArmR / 40)), 0, -f.cSpR - 4); f.shL.localRotation = Quaternion.Euler(-f.cArmL - s * 25 * stride * (1 - Mathf.Clamp01(f.cArmL / 40)), 0, f.cSpL + 4);
            f.elR.localRotation = Quaternion.Euler(-f.cElR * 57.3f - 10 * stride, 0, 0); f.elL.localRotation = Quaternion.Euler(-f.cElL * 57.3f - 10 * stride, 0, 0);
            for (int i = 0; i < f.cape.Count; i++)
            {
                bool lower = i % 2 == 1; float w = Mathf.Sin(BC.t * 2.4f + i * 0.9f + f.ph * 0.2f) * (6 + sp * 10) * f.wind, w2 = Mathf.Sin(BC.t * 1.7f + i) * 3 * f.wind;
                f.cape[i].localRotation = Quaternion.Euler((lower ? 6 + sp * 25 : 10 + sp * 28) + w, 0, w2);
            }
        }
        void IFigPose(IFig f, float lookYaw = 0, float lookPitch = 0, float armR = 0, float armL = 0, float lean = 0, float elbR = 0.25f, float elbL = 0.25f, float kneel = 0, float spreadR = 0, float spreadL = 0)
        { if (f == null) return; f.lookYaw = lookYaw; f.lookPitch = lookPitch; f.armR = armR; f.armL = armL; f.lean = lean; f.elbR = elbR; f.elbL = elbL; f.kneel = kneel; f.armSpreadR = spreadR; f.armSpreadL = spreadL; }
        void ICrowd(Transform s, Vector3 c, float rx, float rz, int n, System.Random rng, bool moving, float speed = 1.2f, int[] palette = null)
        {
            float R(float a, float b) => a + (b - a) * (float)rng.NextDouble();
            var cols = palette ?? new[] { 0x6a2a2a, 0x2a4a6a, 0x4a6a3a, 0x7a6a3a, 0x5a3a6a, 0x8a5a3a, 0x3a3a4a, 0x7a7068, 0x2a5a5a };
            for (int i = 0; i < n; i++)
            {
                double r = rng.NextDouble(); var kind = r < 0.2 ? FigKind.Child : r < 0.6 ? FigKind.Man : FigKind.Woman;
                float h = kind == FigKind.Child ? R(1.05f, 1.3f) : R(1.65f, 1.92f);
                var pos = c + V(R(-rx, rx), 0, R(-rz, rz)); float yaw = R(0, 360);
                var f = IFigure(s, pos, yaw, kind, h, new[] { cols[rng.Next(cols.Length)], cols[rng.Next(cols.Length)] }, rng, rng.NextDouble() < 0.12);
                var dir = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                iWalkers.Add(new IWalker { t = f.root, f = f, v = moving && rng.NextDouble() < 0.75 ? dir * R(speed * 0.6f, speed) : Vector3.zero });
                if (!moving || rng.NextDouble() < 0.3) IFigPose(f, R(-30, 30), R(-5, 5), R(0, 15), R(0, 15));
            }
        }
    }
}
