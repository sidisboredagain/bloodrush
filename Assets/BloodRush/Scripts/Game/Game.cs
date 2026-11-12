using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BR
{
    public static class Boot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Start()
        {
            // The game builds everything from code, exactly like the HTML version: clear the template scene.
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects()) UnityEngine.Object.Destroy(go);
            new GameObject("BLOODRUSH").AddComponent<Game>();
        }
    }

    public class Player
    {
        public Vector3 pos, vel, dashDir = new Vector3(0, 0, -1), slideDir, wallNormal;
        public float yaw, pitch, eye = 1.6f, height = 1.8f, hp = 100, stamina = 3, dashT, slamStartY, slamBounceT, slamPower, coyote, jumpBuf, iframes,
            coins = 4, cd, punchCd, airJumpCd, slamCd, shake, roll, spin, heat, overheat, coreChargeT, coreCd, kick, knockT, cheatT;
        public int weapon, prevWeapon, wallJumps;
        public bool onGround, sliding, slamming, dead, coreCharging;
        // co-op (see GameCoop.cs): slot 0..3, downed = crawling at 0 HP until a teammate revives, inPortal = waiting in the portal
        public int slot; public bool remote, downed, inPortal, hooking; public float revive; public int shots; public string name = "P1";
        // co-op: what this player is doing, for teammates to animate (1 reload, 2 chainsaw, 4 execution, 8 weapon swap, 16 dash, 32 hook pulling)
        public byte act; public Vector3 hookPt; public ushort execNid; public byte execKind;
    }
    public class StyleState { public int rank = -1; public float fill, score; public List<(string txt, float t)> feed = new List<(string, float)>(); public bool dirty = true; }

    public partial class Game : MonoBehaviour
    {
        public static Game I;
        const float GRAV = 28f, JUMP = 11f, RUN = 14f, DASH = 40f, SLIDE = 21f, R = 0.4f, SLAM = 60f;
        const int TOTAL_WAVES = 25; // hell 1-5, frozen circle 6-10, dead earth 11-15, purgatory 16-20, heaven 21-25
        static float rand => UnityEngine.Random.value;
        static float clamp(float v, float a, float b) => Mathf.Clamp(v, a, b);
        static float lerp(float a, float b, float t) => a + (b - a) * t;
        static float ease(float t) => t <= 0 ? 0 : t >= 1 ? 1 : t * t * (3 - 2 * t);
        static readonly Vector3 UP = Vector3.up;
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static float Hypot(float a, float b) => Mathf.Sqrt(a * a + b * b);

        // ---------- engine objects ----------
        Camera cam, cineCam; Transform camT; RenderTexture rt; RawImage screenImg; Light sunLight; Light muzzleLight;
        public AudioMgr audioMgr;
        Transform world;              // parent for transient world objects
        int rtW, rtH;

        // ---------- settings ----------
        float setSens = 1, setFov = 95, setVol = 0.8f, setMusic = 0.7f; bool setShake = true; float hiScore;
        public static bool GOD;

        // ---------- game state ----------
        Player P; StyleState S;
        int wave; string phase = "pre"; float waveDelay = 2, freeze, slowmoT, timeScale = 1, hurtFlash, whiteFlash, victoryT, runTime;
        bool started, locked, won, mouseDown, crouchPressed; float time, menuT; int practiceWave;
        float recoil, swapT, bobT, fistT, killChainT, flashT, swayX, swayY, displayScore, ambientT; int killChain;
        bool skipDelta;
        readonly System.Text.StringBuilder cheatBuf = new System.Text.StringBuilder();

        void Awake()
        {
            I = this;
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 240;
            Physics.simulationMode = SimulationMode.Script; // raycasts only, movement is the game's own AABB code
            Physics.autoSyncTransforms = true;
            Mats.Init(); Tex.Init();
            LoadSettings();
            world = new GameObject("World").transform;
            SetupRendering();
            audioMgr = new AudioMgr(); audioMgr.Init(gameObject);
            InitShared();
            BuildViewmodels();
            BuildUI();
            if (Keyboard.current != null) Keyboard.current.onTextInput += OnText;
            BuildLevel(1);
            ResetGame();
            ShowScreen("mainMenu"); audioMgr.Start(6);
            ApplySettings();
            TestAwake();
        }
        void OnDestroy() { if (Keyboard.current != null) Keyboard.current.onTextInput -= OnText; }

        // ---------- rendering: half-res, pixelated, three.js view convention ----------
        void SetupRendering()
        {
            var cg = new GameObject("Camera"); cam = cg.AddComponent<Camera>(); camT = cg.transform;
            cg.AddComponent<AudioListener>();
            ConfigCam(cam, 0.03f, 460); cam.cullingMask = ~((1 << CINE_LAYER) | (1 << VM_ENV_LAYER));
            var cc = new GameObject("CineCamera"); cineCam = cc.AddComponent<Camera>(); ConfigCam(cineCam, 0.1f, 2500); cineCam.fieldOfView = 50; cineCam.enabled = false;
            var sg = new GameObject("Sun"); sunLight = sg.AddComponent<Light>(); sunLight.type = LightType.Directional; sunLight.shadows = LightShadows.None;
            sunLight.transform.rotation = Quaternion.LookRotation(-V(30, 50, 15).normalized); sunLight.cullingMask = ~(1 << CINE_LAYER);
            var ml = new GameObject("MuzzleLight"); muzzleLight = ml.AddComponent<Light>(); muzzleLight.type = LightType.Point; muzzleLight.range = 14; muzzleLight.intensity = 0; muzzleLight.color = Mats.Hex(0xffcc66); muzzleLight.shadows = LightShadows.None;
            ml.transform.SetParent(camT, false); ml.transform.localPosition = V(0.3f, -0.2f, -1);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.reflectionIntensity = 0;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.sun = sunLight;
            MakeRT();
        }
        // holder for everything drawn as part of the player's view (gun, both arms): see LateUpdate
        const float VM_FOV = 95; Transform vmRoot;
        Transform VmRoot() { if (!vmRoot) { vmRoot = new GameObject("VMRoot").transform; vmRoot.SetParent(camT, false); } return vmRoot; }
        static void ConfigCam(Camera c, float near, float far)
        {
            c.clearFlags = CameraClearFlags.SolidColor; c.nearClipPlane = near; c.farClipPlane = far; c.fieldOfView = 95;
            c.allowHDR = false; c.allowMSAA = false;
            var d = c.GetComponent<UniversalAdditionalCameraData>() ?? c.gameObject.AddComponent<UniversalAdditionalCameraData>();
            d.renderPostProcessing = false; d.renderShadows = false; d.antialiasing = AntialiasingMode.None;
        }
        void MakeRT()
        {
            rtW = Mathf.Max(2, Screen.width / 2); rtH = Mathf.Max(2, Screen.height / 2);
            if (rt) { cam.targetTexture = null; cineCam.targetTexture = null; rt.Release(); Destroy(rt); }
            rt = new RenderTexture(rtW, rtH, 24, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point, antiAliasing = 1 };
            rt.Create();
            cam.targetTexture = rt; cineCam.targetTexture = cineHQ ? null : rt;
            cam.aspect = (float)Screen.width / Screen.height; cineCam.aspect = cam.aspect;
            if (screenImg) screenImg.texture = rt;
        }

        void LateUpdate()
        {
            if (Screen.width / 2 != rtW || Screen.height / 2 != rtH) MakeRT();
            var active = cineCam.enabled ? cineCam : cam;
            // three.js view: camera looks down its local -Z. Using the transform's inverse directly keeps every
            // position/rotation from the HTML game numerically identical (meshes are wound to match).
            cam.worldToCameraMatrix = camT.worldToLocalMatrix;
            cineCam.worldToCameraMatrix = cineCam.transform.worldToLocalMatrix;
            // the arms and gun keep the shape they have at the default FOV, whatever the view's FOV: squashing their holder
            // across and up by the ratio of the FOVs cancels the wider projection exactly (depth is untouched)
            if (vmRoot) { float s = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(VM_FOV * 0.5f * Mathf.Deg2Rad); vmRoot.localScale = V(s, s, 1); }
            var rot = active.transform.rotation;
            for (int i = Mk.billboards.Count - 1; i >= 0; i--)
            {
                var b = Mk.billboards[i];
                if (!b) { Mk.billboards.RemoveAt(i); continue; }
                b.rotation = rot;
            }
            if (RenderSettings.fog) Shader.SetGlobalVector("_BRFog", new Vector4(RenderSettings.fogStartDistance, RenderSettings.fogEndDistance, 0, 0));
            else Shader.SetGlobalVector("_BRFog", new Vector4(1e6f, 1e6f + 1, 0, 0));
            Shader.SetGlobalColor("_BRFogColor", RenderSettings.fogColor);
            DrawParticles();
        }

        // ---------- input ----------
        static Keyboard Kb => Keyboard.current;
        static Mouse Ms => Mouse.current;
        static bool Key(Key k) => Kb != null && Kb[k].isPressed;
        static bool KeyDown(Key k) => Kb != null && Kb[k].wasPressedThisFrame;
        static bool KeyUp(Key k) => Kb != null && Kb[k].wasReleasedThisFrame;
        void OnText(char ch)
        {
            if (!started || !locked || P == null || P.dead || WARP.active || CINE.active) return;
            if (!Auth) return; // co-op: cheats are the host's alone
            char u = char.ToUpperInvariant(ch);
            if (u < 'A' || u > 'Z') return;
            cheatBuf.Append(u); if (cheatBuf.Length > 8) cheatBuf.Remove(0, cheatBuf.Length - 8);
            if (cheatBuf.ToString().EndsWith("BANG")) { cheatBuf.Clear(); ActivateBang(); }
            else if (cheatBuf.ToString().EndsWith("LOL")) { cheatBuf.Clear(); SkipWave(); }
        }
        // L-O-L cheat: skip the current wave - warp straight on to the next level (the last wave goes to the ending)
        void SkipWave()
        {
            if (won || bossDeath != null) return;
            Sfx("cheat");
            if (levelNum >= TOTAL_WAVES) { foreach (var e in enemies) if (e.g) Destroy(e.g); enemies.Clear(); boss = null; HideBossBar(); audioMgr.Stop(1); NetEvent(NetEv.Cutscene); StartCutscene(); return; }
            wave = levelNum; phase = "portal";
            audioMgr.Stop(0.5f);
            NetWarp(levelNum + 1);
        }

        void Lock()
        {
            if (!started) return;
            locked = true; skipDelta = true;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            ShowScreen(null);
            AudioListener.pause = false;
        }
        void Unlock()
        {
            locked = false;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            mouseDown = false;
            audioMgr.Hum(0);
            if (P != null && P.coreCharging) P.coreCharging = false;
            if (started && !P.dead && !won && !CINE.active) { ShowScreen("pauseMenu"); if (!MP) AudioListener.pause = true; } // co-op: the world keeps going behind the menu
        }
        void OnApplicationFocus(bool f) { if (!f && locked && !testMode) Unlock(); }

        void PlayerInput()
        {
            if (Kb == null) return;
            if (KeyDown(UnityEngine.InputSystem.Key.Escape)) { Unlock(); return; }
            if (P.dead || WARP.active) return;
            if (BC.active) { mouseDown = false; return; } // hands off during a boss cinematic
            if (BossIntroActive) { mouseDown = false; return; } // hands off while the boss is introduced
            // mouse look
            if (Ms != null && !DE.active)
            {
                var d = Ms.delta.ReadValue();
                if (skipDelta) { d = Vector2.zero; skipDelta = false; }
                float s = 0.0022f * setSens;
                P.yaw -= d.x * s; P.pitch += d.y * s; // Input System delta.y is up-positive (browser movementY is down-positive)
                P.pitch = clamp(P.pitch, -1.55f, 1.55f);
                swayX += d.x * 0.3f; swayY -= d.y * 0.3f;
            }
            if (P.downed || P.inPortal) // co-op: crawling on the floor with only the revolver, or waiting in the portal
            {
                if (Ms != null && Ms.leftButton.wasPressedThisFrame && !P.inPortal) mouseDown = true;
                if (Ms == null || !Ms.leftButton.isPressed || P.inPortal) mouseDown = false;
                if (KeyDown(UnityEngine.InputSystem.Key.R)) StartReload();
                return;
            }
            if (KeyDown(UnityEngine.InputSystem.Key.Space)) P.jumpBuf = 0.12f;
            if (KeyDown(UnityEngine.InputSystem.Key.LeftShift) || KeyDown(UnityEngine.InputSystem.Key.RightShift)) TryDash();
            if (KeyDown(UnityEngine.InputSystem.Key.C)) crouchPressed = true;
            if (KeyDown(UnityEngine.InputSystem.Key.F)) Punch();
            if (!Key(UnityEngine.InputSystem.Key.F) && !testMode) sawHeld = false;
            if (KeyDown(UnityEngine.InputSystem.Key.Q)) ThrowHook();
            if (KeyUp(UnityEngine.InputSystem.Key.Q)) ReleaseHook();
            if (KeyDown(UnityEngine.InputSystem.Key.V)) StartInspect();
            if (KeyDown(UnityEngine.InputSystem.Key.R)) StartReload();
            if (KeyDown(UnityEngine.InputSystem.Key.Digit1)) SetWeapon(0);
            if (KeyDown(UnityEngine.InputSystem.Key.Digit2)) SetWeapon(1);
            if (KeyDown(UnityEngine.InputSystem.Key.Digit3)) SetWeapon(2);
            if (KeyDown(UnityEngine.InputSystem.Key.Digit4)) SetWeapon(3);
            if (Ms == null) return;
            if (Ms.middleButton.wasPressedThisFrame) ThrowHook();
            if (Ms.middleButton.wasReleasedThisFrame) ReleaseHook();
            if (!DE.active)
            {
                if (Ms.leftButton.wasPressedThisFrame) mouseDown = true;
                if (Ms.rightButton.wasPressedThisFrame)
                {
                    if (P.weapon == 1) { if (P.coreCd <= 0) { P.coreCharging = true; P.coreChargeT = 0; } }
                    else if (P.weapon == 3) StartDeadeye();
                    else if (P.weapon == 2) { laserHeld = true; if (P.overheat > 0) Sfx("deny"); }
                    else if (P.weapon == 4) BangWipe();
                    else ThrowCoin();
                }
                float sc = Ms.scroll.ReadValue().y;
                if (sc != 0) SetWeapon((P.weapon + (sc < 0 ? 1 : 3)) % 4);
            }
            if (Ms.leftButton.wasReleasedThisFrame) mouseDown = false;
            if (Ms.rightButton.wasReleasedThisFrame && P.coreCharging) LaunchCore();
            if (!Ms.rightButton.isPressed && !testMode) laserHeld = false;
        }

        // ---------- main loop (frame() in the HTML game) ----------
        void Update()
        {
            float rdt = testRec ? 1 / 60f : Mathf.Min(0.05f, Time.unscaledDeltaTime); // -brrec: fixed clock, however slow the capture
            audioMgr.Tick(rdt);
            TestUpdate(rdt);
            NetUpdate(rdt);
            if (UpdateIntro(rdt)) { UpdateUIAnims(rdt); return; } // the opening (GameIntro.cs): the game waits behind it
            if (UpdateBossCine(rdt)) { UpdateUIAnims(rdt); return; } // a boss cinematic's frozen shot (GameBossCine.cs)
            if (CINE.active)
            {
                if (KeyDown(UnityEngine.InputSystem.Key.Space) || KeyDown(UnityEngine.InputSystem.Key.Enter) || KeyDown(UnityEngine.InputSystem.Key.Escape)) EndCutscene();
                else
                {
                    UpdateCine(rdt);
                    if (CINE.active) { cam.enabled = false; cineCam.enabled = true; UpdateUIAnims(rdt); return; }
                }
            }
            cam.enabled = true; cineCam.enabled = false;
            if (started && P.dead && !MP && KeyDown(UnityEngine.InputSystem.Key.R)) { StartGame(); return; }
            // co-op: the shared world never pauses, so it also runs while this player sits in a menu (no input then)
            if (started && (locked || (MP && !P.dead && !won)))
            {
                runTime += rdt;
                if (locked) PlayerInput(); else mouseDown = false;
                if (locked || MP)
                {
                    if (WARP.active) UpdateWarp(rdt);
                    else
                    {
                        UpdateDeadeye(rdt); UpdateExecution(rdt);
                        slowmoT = Mathf.Max(0, slowmoT - rdt);
                        timeScale = BossIntroActive ? 0.6f : EX.active ? (EX.t < EX.rip ? 0.12f : 0.4f) : DE.active ? 0.2f : P.dead ? 0.35f : slowmoT > 0 ? 0.3f : 1;
                        float dt = rdt * timeScale;
                        if (freeze > 0) { freeze -= rdt; dt = 0; }
                        // solo: slow-mo and hit-stop scale everything. Co-op: they only slow this player (camera, gun, own
                        // movement); enemies, hazards and the wave clock always run at real time for everyone.
                        if (!MP) { if (dt > 0) GameUpdate(dt, dt); }
                        else GameUpdate(dt, rdt);
                    }
                    UpdateCamera(rdt);
                    UpdateBossIntro(rdt); UpdateWardenCam(rdt); UpdateSeraphCam(rdt);
                }
            }
            else if (!started) MenuCamera(rdt);
            DrawHook();
            if (started) { UpdateHUD(rdt); UpdateTutorial(rdt); UpdateDamageArcs(rdt); }
            BossCineFrameEnd(); // (a boss cinematic filming the running game: its camera, not the player's)
            UpdateUIAnims(rdt);
        }

        // ---------- settings ----------
        void LoadSettings()
        {
            setSens = PlayerPrefs.GetFloat("br_sens", 1); setFov = PlayerPrefs.GetFloat("br_fov", 95);
            setVol = PlayerPrefs.GetFloat("br_vol", 0.8f); setMusic = PlayerPrefs.GetFloat("br_music", 0.7f);
            setShake = PlayerPrefs.GetInt("br_shake", 1) == 1; hiScore = PlayerPrefs.GetFloat("br_hi", 0);
        }
        void SaveSettings()
        {
            PlayerPrefs.SetFloat("br_sens", setSens); PlayerPrefs.SetFloat("br_fov", setFov); PlayerPrefs.SetFloat("br_vol", setVol);
            PlayerPrefs.SetFloat("br_music", setMusic); PlayerPrefs.SetInt("br_shake", setShake ? 1 : 0); PlayerPrefs.Save();
        }
        void ApplySettings() { AudioListener.volume = setVol; audioMgr.musicVol = setMusic; }
        bool SaveHi()
        {
            if (S.score > hiScore) { hiScore = S.score; PlayerPrefs.SetFloat("br_hi", hiScore); PlayerPrefs.Save(); return true; }
            return false;
        }
        void Sfx(string n, float v = 1) => audioMgr.Play(n, v);
    }
}
