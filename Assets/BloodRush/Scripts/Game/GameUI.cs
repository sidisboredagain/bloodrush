using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BR
{
    // Solid or gradient quad, optionally a parallelogram (CSS clip-path on the menu buttons).
    public class Quad : MaskableGraphic
    {
        public Color tl = Color.white, tr = Color.white, bl = Color.white, br = Color.white; public float slant;
        public void Solid(Color c) { tl = tr = bl = br = c; SetVerticesDirty(); }
        public void Vertical(Color top, Color bottom) { tl = tr = top; bl = br = bottom; SetVerticesDirty(); }
        public void Horizontal(Color left, Color right) { tl = bl = left; tr = br = right; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = GetPixelAdjustedRect(); var c = color;
            vh.AddVert(new Vector3(r.xMin, r.yMin), bl * c, Vector2.zero);
            vh.AddVert(new Vector3(r.xMin + slant, r.yMax), tl * c, Vector2.zero);
            vh.AddVert(new Vector3(r.xMax, r.yMax), tr * c, Vector2.zero);
            vh.AddVert(new Vector3(r.xMax - slant, r.yMin), br * c, Vector2.zero);
            vh.AddTriangle(0, 1, 2); vh.AddTriangle(2, 3, 0);
        }
    }
    // CSS letter-spacing for legacy Text (per line, respecting the alignment).
    public class Spacing : BaseMeshEffect
    {
        public float px;
        readonly List<UIVertex> verts = new List<UIVertex>();
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || px == 0) return;
            verts.Clear(); vh.GetUIVertexStream(verts);
            var txt = GetComponent<Text>(); float align = 0;
            if (txt) { var a = txt.alignment; align = a == TextAnchor.UpperCenter || a == TextAnchor.MiddleCenter || a == TextAnchor.LowerCenter ? 0.5f : a == TextAnchor.UpperRight || a == TextAnchor.MiddleRight || a == TextAnchor.LowerRight ? 1 : 0; }
            int quads = verts.Count / 6; var lineOf = new int[quads]; var lineCount = new List<int>(); var lineY = new List<float>();
            for (int q = 0; q < quads; q++)
            {
                float y = (verts[q * 6].position.y + verts[q * 6 + 2].position.y) * 0.5f; int li = -1;
                for (int l = 0; l < lineY.Count; l++) if (Mathf.Abs(lineY[l] - y) < (txt ? txt.fontSize * 0.6f : 10)) { li = l; break; }
                if (li < 0) { li = lineY.Count; lineY.Add(y); lineCount.Add(0); }
                lineOf[q] = li;
            }
            var idx = new int[lineY.Count];
            for (int q = 0; q < quads; q++)
            {
                int l = lineOf[q]; float off = idx[l]++ * px - (lineCount.Count > 0 ? 0 : 0);
                for (int k = 0; k < 6; k++) { var v = verts[q * 6 + k]; v.position.x += off; verts[q * 6 + k] = v; }
            }
            // re-centre each line
            var total = new int[lineY.Count]; for (int q = 0; q < quads; q++) total[lineOf[q]]++;
            for (int q = 0; q < quads; q++)
            {
                float shift = -(total[lineOf[q]] - 1) * px * align;
                for (int k = 0; k < 6; k++) { var v = verts[q * 6 + k]; v.position.x += shift; verts[q * 6 + k] = v; }
            }
            vh.Clear(); vh.AddUIVertexTriangleStream(verts);
        }
    }
    public class BRButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Action onClick; public bool hover; public float h;
        public void OnPointerEnter(PointerEventData e) => hover = true;
        public void OnPointerExit(PointerEventData e) => hover = false;
        public void OnPointerClick(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) onClick?.Invoke(); }
    }

    public partial class Game
    {
        Font fD, fM; Camera uiCam;
        RectTransform canvasRT, hudRT;
        GameObject hudGO, uiScope, bossBarGO, hintGO, cheatGO, heatGO, cineGO, cineTitleGO;
        CanvasGroup warpGroup, bannerGroup, scorePopGroup, hintGroup, cheatTxtGroup, cineCapGroup, cineSmallGroup, cineTitleGroup;
        Text scoreNum, multT, scorePopT, speedT, topT, bossNameT, bannerMain, bannerSub, hintT, hpText, cheatT, wlistT, wnameT, wtipT, rankLT, rankNT, feedT, cineCapT, cineSmallT, deadStatsT, winStatsT;
        RectTransform hpFill, bossFill, bossTrailRT, cheatFill, heatFill, rankFill, bannerRT, scorePopRT, warpRays, warpPulse, cineTopBar, cineBotBar, titleRT, cineTitleRT;
        Text ammoT; readonly List<Quad> ammoPips = new List<Quad>();
        Quad rankFillQ, heatFillQ; RawImage hurtImg, flashImg, cineFade, warpBg; readonly RawImage[] speedImgs = new RawImage[2];
        readonly List<RectTransform> stamFills = new List<RectTransform>(); readonly List<Graphic> coinEls = new List<Graphic>();
        (Text t, Quad bg, Quad border)[] abil;
        readonly Dictionary<string, GameObject> screens = new Dictionary<string, GameObject>();
        readonly List<(BRButton b, RectTransform inner, Quad bgO, Quad bgI, Spacing sp, bool holy, bool small)> buttons = new List<(BRButton, RectTransform, Quad, Quad, Spacing, bool, bool)>();
        string currentScreen = "mainMenu", panelReturn = "mainMenu";
        float bannerT = 99, scorePopT_ = 99, cineBarT = 99, cineTitleT = 99, uiTime, bossTrail = 1; bool cineCapOn, cineSmallOn;

        static Color C(string css) => Mats.Css(css);
        static Color CA(string css, float a) { var c = Mats.Css(css); c.a = a; return c; }

        // ---------- builders ----------
        RectTransform Rt(Transform parent, string name = "ui")
        {
            var g = new GameObject(name, typeof(RectTransform)); g.layer = 5; var rt = (RectTransform)g.transform; rt.SetParent(parent, false); return rt;
        }
        // place with CSS-like anchoring: anchor (0..1) both min/max, pivot and pixel offset (y up)
        static RectTransform Place(RectTransform rt, float ax, float ay, float px, float py, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay); rt.pivot = new Vector2(px, py);
            rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h); return rt;
        }
        static RectTransform Fill(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; return rt; }
        Text Txt(Transform parent, string s, int size, Color c, TextAnchor a, bool mono = false, float spacing = 0, float shadow = 2, string name = "txt")
        {
            var rt = Rt(parent, name); var t = rt.gameObject.AddComponent<Text>();
            t.font = mono ? fM : fD; t.fontSize = size; t.color = c; t.alignment = a; t.text = s; t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false; t.lineSpacing = 1;
            if (spacing > 0) rt.gameObject.AddComponent<Spacing>().px = spacing;
            if (shadow > 0) { var sh = rt.gameObject.AddComponent<Shadow>(); sh.effectColor = Color.black; sh.effectDistance = new Vector2(shadow, -shadow); }
            return t;
        }
        Quad Qd(Transform parent, Color c, string name = "q")
        {
            var rt = Rt(parent, name); var q = rt.gameObject.AddComponent<Quad>(); q.Solid(c); q.raycastTarget = false; return q;
        }
        RawImage Img(Transform parent, Texture tex, Color c, string name = "img")
        {
            var rt = Rt(parent, name); var r = rt.gameObject.AddComponent<RawImage>(); r.texture = tex; r.color = c; r.raycastTarget = false; return r;
        }
        // a bordered bar: outline (outer), border, background and a fill child anchored left
        RectTransform Bar(RectTransform parent, float x, float y, float w, float h, Color bg, Color border, float bw, Color outline, float ow, out RectTransform inner)
        {
            var o = Qd(parent, ow > 0 ? outline : Color.clear, "outline").rectTransform; Place(o, 0, 0, 0, 0, x - ow, y - ow, w + ow * 2, h + ow * 2);
            var b = Qd(o, border, "border").rectTransform; Fill(b); b.offsetMin = new Vector2(ow, ow); b.offsetMax = new Vector2(-ow, -ow);
            var g = Qd(b, bg, "bg").rectTransform; Fill(g); g.offsetMin = new Vector2(bw, bw); g.offsetMax = new Vector2(-bw, -bw);
            inner = g; return o;
        }
        RectTransform FillChild(RectTransform parent, Color c, out Quad q)
        {
            q = Qd(parent, c, "fill"); var rt = q.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0, 0.5f); rt.offsetMin = rt.offsetMax = Vector2.zero; return rt;
        }
        static void SetFill(RectTransform fill, float f) { fill.anchorMax = new Vector2(Mathf.Clamp01(f), 1); }

        void BuildUI()
        {
            fD = Resources.Load<Font>("Fonts/BR_Display"); fM = Resources.Load<Font>("Fonts/BR_Mono");
            if (!fD) fD = Font.CreateDynamicFontFromOSFont(new[] { "Impact", "Arial Black", "Arial" }, 32);
            if (!fM) fM = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Courier New" }, 16);
            // a camera that only clears the backbuffer; the game itself is the half-res RenderTexture shown below
            var uc = uiCam = new GameObject("UICamera").AddComponent<Camera>(); uc.cullingMask = 0; uc.clearFlags = CameraClearFlags.SolidColor; uc.backgroundColor = Color.black; uc.depth = 100;
            var ucd = uc.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); ucd.renderPostProcessing = false;
            var cg = new GameObject("Canvas"); cg.layer = 5;
            var canvas = cg.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var sc = cg.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            cg.AddComponent<GraphicRaycaster>();
            canvasRT = (RectTransform)cg.transform;
            var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); var im = es.AddComponent<InputSystemUIInputModule>(); im.AssignDefaultActions();

            screenImg = Img(canvasRT, rt, Color.white, "screen"); Fill(screenImg.rectTransform);
            BuildHUD();
            BuildWarp();
            BuildCineOverlay();
            BuildScreens();
        }

        void BuildHUD()
        {
            hudRT = Fill(Rt(canvasRT, "hud")); hudGO = hudRT.gameObject;
            // crosshair
            var cross = Place(Rt(hudRT, "cross"), 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 18, 18);
            foreach (var (w, h) in new[] { (2f, 18f), (18f, 2f) })
            {
                var o = Qd(cross, Color.black).rectTransform; Place(o, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, w + 2, h + 2);
                Place(Qd(o, Color.white).rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, w, h);
            }
            // score box
            var sb = Place(Rt(hudRT, "scoreBox"), 0, 1, 0, 1, 24, -18, 400, 140);
            Place(Txt(sb, "SCORE", 14, C("#f55"), TextAnchor.UpperLeft, false, 4, 1).rectTransform, 0, 1, 0, 1, 0, 0, 200, 18);
            scoreNum = Txt(sb, "", 46, Color.white, TextAnchor.UpperLeft, false, 3, 3); Place(scoreNum.rectTransform, 0, 1, 0, 1, 0, -16, 300, 50);
            BuildScoreRoller(sb); // the score is shown by the odometer wheels; scoreNum stays empty
            multT = Txt(sb, "", 20, C("#ffd24a"), TextAnchor.LowerLeft, false, 0, 2); Place(multT.rectTransform, 0, 1, 0, 0, 200, -58, 120, 24);
            scorePopT = Txt(sb, "", 22, C("#ffe066"), TextAnchor.UpperLeft, false, 0, 2); scorePopRT = Place(scorePopT.rectTransform, 0, 1, 0, 0.5f, 0, -76, 200, 24);
            scorePopGroup = scorePopT.gameObject.AddComponent<CanvasGroup>(); scorePopGroup.alpha = 0;
            speedT = Txt(sb, "", 14, C("#ccc"), TextAnchor.UpperLeft, false, 0, 2); Place(speedT.rectTransform, 0, 1, 0, 1, 0, -92, 200, 18);
            // top status line
            topT = Txt(hudRT, "", 21, Color.white, TextAnchor.UpperCenter, false, 2, 2); Place(topT.rectTransform, 0.5f, 1, 0.5f, 1, 0, -18, 1600, 26);
            // boss bar
            var bb = Place(Rt(hudRT, "bossBar"), 0.5f, 1, 0.5f, 1, 0, -52, 640, 50); bossBarGO = bb.gameObject;
            bossNameT = Txt(bb, "", 22, C("#ffdddd"), TextAnchor.UpperCenter, false, 5, 3); Place(bossNameT.rectTransform, 0.5f, 1, 0.5f, 1, 0, 0, 640, 26);
            var bo = Bar(bb, 0, 0, 640, 16, C("#1a0000"), Color.black, 2, C("#800"), 2, out var bin); Place(bo, 0.5f, 1, 0.5f, 1, 0, -30, 644, 20);
            bossTrailRT = FillChild(bin, Color.white, out _);
            bossFill = FillChild(bin, Color.white, out var bfq); bfq.Vertical(C("#ff3a3a"), C("#900")); bossFillQ = bfq;
            bossMidMark = Qd(bo, C("#ffd24a")).gameObject; Place(bossMidMark.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 2, 28);
            bossBarGO.SetActive(false);
            // banner
            bannerRT = Place(Rt(hudRT, "banner"), 0.5f, 0.7f, 0.5f, 1, 0, 0, 1900, 130); bannerGroup = bannerRT.gameObject.AddComponent<CanvasGroup>(); bannerGroup.alpha = 0;
            bannerMain = Txt(bannerRT, "", 80, C("#f33"), TextAnchor.UpperCenter, false, 10, 5); Place(bannerMain.rectTransform, 0.5f, 1, 0.5f, 1, 0, 0, 1900, 90);
            bannerSub = Txt(bannerRT, "", 26, Color.white, TextAnchor.UpperCenter, false, 6, 3); Place(bannerSub.rectTransform, 0.5f, 1, 0.5f, 1, 0, -92, 1900, 32);
            // hint
            hintT = Txt(hudRT, "", 24, Color.white, TextAnchor.UpperCenter, false, 6, 3); Place(hintT.rectTransform, 0.5f, 0.38f, 0.5f, 1, 0, 0, 1600, 30);
            hintGO = hintT.gameObject; hintGroup = hintGO.AddComponent<CanvasGroup>(); hintGO.SetActive(false);
            // health, stamina, coins
            var st = Place(Rt(hudRT, "stats"), 0, 0, 0, 0, 24, 24, 280, 100);
            var hpo = Bar(st, 0, 68, 280, 30, C("#300"), Color.black, 2, C("#a00"), 2, out var hpin);
            hpFill = FillChild(hpin, Color.white, out var hq); hq.Vertical(C("#f33"), C("#a00"));
            hpText = Txt(hpo, "100", 22, Color.white, TextAnchor.MiddleRight, false, 0, 2); Place(hpText.rectTransform, 1, 0.5f, 1, 0.5f, -10, 1, 100, 30);
            for (int i = 0; i < 3; i++)
            {
                float w = (280 - 12) / 3f;
                Bar(st, i * (w + 6), 48, w, 12, C("#123"), Color.black, 2, C("#39f"), 1, out var pin);
                stamFills.Add(FillChild(pin, C("#4cf"), out _));
            }
            for (int i = 0; i < 4; i++)
            {
                var o = Img(st, Tex.Circle, Color.black, "coin"); Place(o.rectTransform, 0, 0, 0, 0, i * 22, 24, 16, 16);
                var inner = Img(o.rectTransform, Tex.Circle, C("#fc3")); Fill(inner.rectTransform); inner.rectTransform.offsetMin = new Vector2(2, 2); inner.rectTransform.offsetMax = new Vector2(-2, -2);
                var cgp = o.gameObject.AddComponent<CanvasGroup>(); coinEls.Add(o);
            }
            // BANG cheat timer
            var ch = Place(Rt(hudRT, "cheat"), 0.5f, 0, 0.5f, 0, 0, 120, 300, 44); cheatGO = ch.gameObject;
            cheatT = Txt(ch, "", 24, C("#ffcc40"), TextAnchor.UpperCenter, false, 6, 2); Place(cheatT.rectTransform, 0.5f, 1, 0.5f, 1, 0, 0, 400, 30);
            cheatTxtGroup = cheatT.gameObject.AddComponent<CanvasGroup>();
            Bar(ch, 0, 0, 300, 8, C("#220"), Color.black, 1, C("#a80"), 1, out var cin); cheatFill = FillChild(cin, Color.white, out var cfq); cfq.Horizontal(C("#ff2a2a"), C("#ffcc40"));
            cheatGO.SetActive(false);
            // ability chips
            abil = new (Text, Quad, Quad)[3];
            for (int i = 0; i < 3; i++)
            {
                var border = Qd(hudRT, C("#444"), "abil"); Place(border.rectTransform, 0.5f, 0, 0.5f, 0, 0, 26, 100, 27);
                var bg = Qd(border.rectTransform, CA("#000", 0.6f)); Fill(bg.rectTransform); bg.rectTransform.offsetMin = Vector2.one; bg.rectTransform.offsetMax = -Vector2.one;
                var t = Txt(border.rectTransform, "", 15, C("#888"), TextAnchor.MiddleCenter, false, 2, 1); Fill(t.rectTransform);
                abil[i] = (t, bg, border);
            }
            // weapon panel
            var wp = Place(Rt(hudRT, "weapon"), 1, 0, 1, 0, -24, 24, 340, 120);
            heatGO = Bar(wp, 0, 0, 340, 8, C("#222"), Color.black, 1, Color.clear, 0, out var hin).gameObject; heatFill = FillChild(hin, Color.white, out heatFillQ); heatFillQ.Horizontal(C("#fa0"), C("#f30"));
            wtipT = Txt(wp, "", 14, C("#ddd"), TextAnchor.LowerRight, true, 0, 1); Place(wtipT.rectTransform, 1, 0, 1, 0, 0, 14, 600, 18);
            wnameT = Txt(wp, "", 30, Color.white, TextAnchor.LowerRight, false, 0, 3); Place(wnameT.rectTransform, 1, 0, 1, 0, 0, 30, 600, 36);
            wlistT = Txt(wp, "", 21, C("#8a8a8a"), TextAnchor.LowerRight, false, 1, 2); Place(wlistT.rectTransform, 1, 0, 1, 0, 0, 70, 760, 28);
            wtipT.gameObject.SetActive(false); // the per-gun hint line is gone; the controls screen explains the alt-fires
            // ammo: a row of round pips plus a label (count / RELOADING / R RELOAD)
            ammoT = Txt(wp, "", 40, Color.white, TextAnchor.LowerRight, false, 3, 3); Place(ammoT.rectTransform, 1, 0, 1, 0, 0, 92, 600, 46);
            for (int i = 0; i < 6; i++) { var q = Qd(wp, Color.white, "pip"); Place(q.rectTransform, 1, 0, 1, 0, -i * 20, 142, 14, 32); ammoPips.Add(q); }
            // style meter
            var sty = Place(Rt(hudRT, "style"), 1, 1, 1, 1, -24, -24, 300, 400);
            rankLT = Txt(sty, "", 80, Color.white, TextAnchor.UpperRight, false, 0, 4); Place(rankLT.rectTransform, 1, 1, 1, 1, 0, 0, 300, 82);
            rankNT = Txt(sty, "", 20, Color.white, TextAnchor.UpperRight, false, 2, 2); Place(rankNT.rectTransform, 1, 1, 1, 1, 0, -82, 300, 24);
            BuildRankFx(sty);
            var rb = Bar(sty, 0, 0, 300, 8, C("#222"), Color.black, 1, Color.clear, 0, out var rin); Place(rb, 1, 1, 1, 1, 0, -112, 300, 8);
            rankFill = FillChild(rin, Color.white, out rankFillQ); rankFillQ.raycastTarget = false;
            feedT = Txt(sty, "", 18, Color.white, TextAnchor.UpperRight, false, 0, 2); Place(feedT.rectTransform, 1, 1, 1, 1, 0, -130, 300, 200); feedT.lineSpacing = 1.05f;
            // DEADEYE scope
            var sc = Fill(Rt(hudRT, "scope")); uiScope = sc.gameObject;
            var mask = Img(sc, Tex.ScopeMask, Color.white, "mask"); Place(mask.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 1080, 1080);
            foreach (var (ax, px) in new[] { (0f, 0f), (1f, 1f) }) { var side = Qd(sc, CA("#000", 0.92f)); var r = side.rectTransform; r.anchorMin = new Vector2(ax, 0); r.anchorMax = new Vector2(ax, 1); r.pivot = new Vector2(px, 0.5f); r.sizeDelta = new Vector2(2000, 0); r.anchoredPosition = Vector2.zero; scopeSides.Add(r); }
            scopeMask = mask.rectTransform;
            scopeRing = Img(sc, Tex.Ring, Color.white, "ring").rectTransform;
            scopeH = Qd(sc, C("#9ff")).rectTransform; scopeV = Qd(sc, C("#9ff")).rectTransform;
            scopeLbl = Txt(sc, "DEADEYE", 30, C("#9ff"), TextAnchor.UpperCenter, false, 12, 0); scopeLbl.gameObject.AddComponent<Outline>().effectColor = CA("#0ff", 0.4f);
            uiScope.SetActive(false);
            for (int i = 0; i < 2; i++) // grapple pull rush (two variants flickered for motion)
            {
                var sl = Img(hudRT, Tex.SpeedLines(17 + i * 31), new Color(1, 1, 1, 0), "speedLines"); Fill(sl.rectTransform); speedImgs[i] = sl;
            }
            BuildTutorialUI();
            BuildCrack();
            hurtImg = Img(hudRT, Tex.Vignette, new Color(1, 1, 1, 0), "hurt"); Fill(hurtImg.rectTransform);
            flashImg = Img(hudRT, Texture2D.whiteTexture, new Color(1, 1, 1, 0), "flash"); Fill(flashImg.rectTransform);
            BuildBossIntro();
            hudGO.SetActive(false);
        }
        RectTransform scopeMask, scopeRing, scopeH, scopeV; Text scopeLbl; readonly List<RectTransform> scopeSides = new List<RectTransform>();
        void LayoutScope()
        {
            var size = canvasRT.rect.size; float vmin = Mathf.Min(size.x, size.y) / 100f;
            Place(scopeMask, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 100 * vmin, 100 * vmin);
            float sideW = (size.x - 100 * vmin) / 2 + 2; foreach (var s in scopeSides) s.sizeDelta = new Vector2(Mathf.Max(0, sideW), 0);
            Place(scopeRing, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 62 * vmin + 6, 62 * vmin + 6);
            Place(scopeH, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 60 * vmin, 2); Place(scopeV, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 2, 60 * vmin);
            Place(scopeLbl.rectTransform, 0.5f, 0.5f, 0.5f, 1, 0, -33 * vmin, 800, 40);
        }

        void BuildWarp()
        {
            var w = Fill(Rt(canvasRT, "warp")); warpGroup = w.gameObject.AddComponent<CanvasGroup>(); warpGroup.alpha = 0; warpGroup.blocksRaycasts = false;
            var mask = w.gameObject.AddComponent<RectMask2D>();
            warpBg = Img(w, Tex.WarpBg("#ffe0a0", "#ff2000"), Color.white, "bg"); Fill(warpBg.rectTransform);
            warpRays = Img(w, Tex.Rays, Color.white, "rays").rectTransform; Place(warpRays, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 4400, 4400);
            warpPulse = Img(w, Tex.Glow, Color.white, "pulse").rectTransform; Place(warpPulse, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 1100, 1100);
        }
        void SetWarpColors(string a, string b) { if (warpBg.texture) Destroy(warpBg.texture); warpBg.texture = Tex.WarpBg(a, b); }

        void BuildCineOverlay()
        {
            var c = Fill(Rt(canvasRT, "cine")); cineGO = c.gameObject;
            cineTopBar = Qd(c, Color.black).rectTransform; Place(cineTopBar, 0.5f, 1, 0.5f, 1, 0, 0, 4000, 0);
            cineBotBar = Qd(c, Color.black).rectTransform; Place(cineBotBar, 0.5f, 0, 0.5f, 0, 0, 0, 4000, 0);
            var cap = Rt(c, "cap"); cap.anchorMin = new Vector2(0.08f, 0.15f); cap.anchorMax = new Vector2(0.92f, 0.15f); cap.pivot = new Vector2(0.5f, 0); cap.sizeDelta = new Vector2(0, 100); cap.anchoredPosition = Vector2.zero;
            cineCapT = Txt(cap, "", 34, Color.white, TextAnchor.LowerCenter, false, 6, 3); Place(cineCapT.rectTransform, 0.5f, 0, 0.5f, 0, 0, 34, 1800, 40);
            cineCapGroup = cineCapT.gameObject.AddComponent<CanvasGroup>(); cineCapGroup.alpha = 0;
            cineSmallT = Txt(cap, "", 24, C("#ffd890"), TextAnchor.LowerCenter, false, 6, 3); Place(cineSmallT.rectTransform, 0.5f, 0, 0.5f, 0, 0, 0, 1800, 30);
            cineSmallGroup = cineSmallT.gameObject.AddComponent<CanvasGroup>(); cineSmallGroup.alpha = 0;
            cineTitleRT = Fill(Rt(c, "title")); cineTitleGO = cineTitleRT.gameObject; cineTitleGroup = cineTitleGO.AddComponent<CanvasGroup>();
            var t1 = Txt(cineTitleRT, "BLOOD<color=#ff2020>RUSH</color>", 150, Color.white, TextAnchor.MiddleCenter, false, 8, 6); Place(t1.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 50, 1900, 170);
            var t2 = Txt(cineTitleRT, "THE WORLD IS SAVED", 34, C("#ffd870"), TextAnchor.MiddleCenter, false, 12, 3); Place(t2.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, -60, 1900, 40);
            var t3 = Txt(cineTitleRT, "THANK YOU FOR PLAYING", 16, C("#aaa"), TextAnchor.MiddleCenter, true, 6, 0); Place(t3.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, -110, 1900, 20);
            cineTitleGO.SetActive(false);
            var skip = Txt(c, "SPACE · SKIP", 13, C("#999"), TextAnchor.LowerRight, true, 3, 0); skipRT = Place(skip.rectTransform, 1, 0, 1, 0, -22, 140, 400, 16);
            cineFade = Img(c, Texture2D.whiteTexture, new Color(1, 1, 1, 0), "fade"); Fill(cineFade.rectTransform);
            cineGO.SetActive(false);
        }
        RectTransform skipRT;
        void CineOverlay(bool on) { cineGO.SetActive(on); if (on) { cineBarT = 0; cineFade.color = new Color(0, 0, 0, 0); cineTitleGO.SetActive(false); } }
        void SetCineCaption(string cap, string cap2)
        {
            cineCapT.text = cap ?? ""; cineSmallT.text = cap2 ?? "";
            cineCapGroup.alpha = 0; cineSmallGroup.alpha = 0; cineCapOn = !string.IsNullOrEmpty(cap); cineSmallOn = false;
            var p = cineCapT.rectTransform.anchoredPosition; p.y = string.IsNullOrEmpty(cap2) ? 0 : 34; cineCapT.rectTransform.anchoredPosition = p;
        }
        void SetCineTitle(bool on) { if (on && !cineTitleGO.activeSelf) cineTitleT = 0; cineTitleGO.SetActive(on); }
        void SetCineFade(Color c, float a) { c.a = Mathf.Clamp01(a); cineFade.color = c; }

        // ---------- menu screens ----------
        RectTransform MakeScreen(string id)
        {
            var s = Fill(Rt(canvasRT, id));
            var bg = Img(s, Tex.ScreenBg, Color.white, "bg"); Fill(bg.rectTransform); bg.raycastTarget = true;
            screens[id] = s.gameObject; s.gameObject.SetActive(false);
            return s;
        }
        void Scanlines(RectTransform s)
        {
            var sl = Img(s, Tex.Scan, Color.white, "scan"); Fill(sl.rectTransform); sl.uvRect = new Rect(0, 0, 1920 / 4f, 1080 / 4f); scanImgs.Add(sl);
        }
        readonly List<RawImage> scanImgs = new List<RawImage>();
        // button colour themes besides the red default and the gold "holy": 2 = ice (the Frozen Traitor), 3 = ember (the Hollow King)
        readonly Dictionary<BRButton, int> btnTheme = new Dictionary<BRButton, int>();
        RectTransform Btn(RectTransform parent, string label, string act, float y, bool holy = false, bool small = false, int theme = 0)
        {
            float w = small ? 260 : 380, h = small ? 42 : 54;
            var root = Place(Rt(parent, "btn"), 0.5f, 0.5f, 0.5f, 0.5f, 0, y, w, h);
            var hit = root.gameObject.AddComponent<Quad>(); hit.Solid(new Color(0, 0, 0, 0)); hit.raycastTarget = true;
            var inner = Place(Rt(root, "in"), 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, w, h);
            var o = Qd(inner, holy ? C("#ffd870") : theme == 2 ? C("#a8e8ff") : theme == 3 ? C("#ffa050") : C("#c22")); Fill(o.rectTransform); o.slant = 14;
            var i = Qd(inner, Color.white); Fill(i.rectTransform); i.rectTransform.offsetMin = new Vector2(2, 2); i.rectTransform.offsetMax = new Vector2(-2, -2); i.slant = 13;
            var t = Txt(inner, label, small ? 20 : 28, Color.white, TextAnchor.MiddleCenter, false, 6, 0); Fill(t.rectTransform);
            var b = root.gameObject.AddComponent<BRButton>(); b.onClick = () => HandleAct(act); if (theme >= 2) btnTheme[b] = theme;
            buttons.Add((b, inner, o, i, t.GetComponent<Spacing>(), holy, small));
            return root;
        }
        void Stack(RectTransform parent, float startY, params (RectTransform rt, float h, float gapAfter)[] items)
        {
            float total = 0; foreach (var it in items) total += it.h + it.gapAfter;
            float y = startY + total / 2;
            foreach (var it in items) { var p = it.rt.anchoredPosition; p.y = y - it.h / 2; it.rt.anchoredPosition = p; y -= it.h + it.gapAfter; }
        }
        RectTransform Line(RectTransform parent, string s, int size, Color c, bool mono, float spacing, float shadow, float h)
        {
            var t = Txt(parent, s, size, c, TextAnchor.MiddleCenter, mono, spacing, shadow); return Place(t.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 1900, h);
        }

        void BuildScreens()
        {
            // main menu
            {
                var s = MakeScreen("mainMenu");
                var title = Line(s, "BLOOD<color=#ff2020>RUSH</color>", 160, Color.white, false, 6, 6, 150); titleRT = title;
                var b1 = Btn(s, "PLAY", "play", 0); var b2 = Btn(s, "BOSS PRACTICE", "practice", 0); var b3 = Btn(s, "CONTROLS", "controls", 0); var b4 = Btn(s, "SETTINGS", "settings", 0);
                var b6 = Btn(s, "EXIT", "exit", 0); var b7 = Btn(s, "CO-OP", "coop", 0, true); // (no tutorial in the co-op build)
                Stack(s, 0, (title, 150, 40), (b1, 54, 12), (b7, 54, 12), (b2, 54, 12), (b3, 54, 12), (b4, 54, 12), (b6, 54, 0));
                Scanlines(s);
            }
            // exit: are you sure?
            {
                var s = MakeScreen("exitConfirm");
                var big = Line(s, "EXIT GAME?", 96, Color.white, false, 10, 6, 110);
                var tag = Line(s, "DO YOU WANT TO QUIT?", 18, C("#f66"), true, 6, 0, 24);
                var yes = Btn(s, "YES, EXIT", "exitYes", 0); var no = Btn(s, "NO, GO BACK", "exitNo", 0);
                Stack(s, 0, (big, 110, 10), (tag, 24, 34), (yes, 54, 12), (no, 54, 0));
                Scanlines(s);
            }
            // boss practice
            {
                var s = MakeScreen("practiceMenu");
                var big = Line(s, "BOSS PRACTICE", 120, Color.white, false, 10, 6, 125);
                var tag = Line(s, "JUMP STRAIGHT INTO A BOSS FIGHT", 16, C("#f66"), true, 8, 0, 20);
                var b1 = Btn(s, "CARNIFEX", "boss1", 0); var bF = Btn(s, "FROZEN TRAITOR", "boss3", 0, false, false, 2); var bH = Btn(s, "HOLLOW KING", "boss4", 0, false, false, 3); var bW = Btn(s, "WARDEN OF HOURS", "boss5", 0, false, false, 4); var b2 = Btn(s, "SERAPH", "boss2", 0, true); var b3 = Btn(s, "BACK", "back", 0, false, true);
                Stack(s, 0, (big, 125, 10), (tag, 20, 30), (b1, 50, 10), (bF, 50, 10), (bH, 50, 10), (bW, 50, 10), (b2, 50, 10), (b3, 42, 0));
                Scanlines(s);
            }
            // pause
            {
                var s = MakeScreen("pauseMenu");
                var big = Line(s, "PAUSED", 120, Color.white, false, 10, 6, 125);
                var bs = new[] { Btn(s, "RESUME", "resume", 0), Btn(s, "RESTART", "restart", 0), Btn(s, "CONTROLS", "controls", 0), Btn(s, "SETTINGS", "settings", 0), Btn(s, "QUIT TO MENU", "menu", 0) };
                Stack(s, 0, (big, 125, 24), (bs[0], 54, 12), (bs[1], 54, 12), (bs[2], 54, 12), (bs[3], 54, 12), (bs[4], 54, 0));
                Scanlines(s);
            }
            // controls
            {
                var s = MakeScreen("controlsPanel");
                var rows = new (string k, string d)[] {
                    ("W A S D", "Move"),
                    ("SPACE", "Jump · DOUBLE JUMP in the air · wall-jump"),
                    ("SHIFT", "Dash (invulnerable while dashing) · dash + jump = dash-jump"),
                    ("C", "Slide on the ground · GROUND SLAM in the air · jump after a slam to bounce"),
                    ("MOUSE 3 / Q", "Grappling hook: pulls you to walls and big enemies, yanks small ones to you"),
                    ("LMB", "Fire"),
                    ("RMB", "Revolver: toss a coin, shoot it to ricochet\nShotgun: CORE EJECT - hold to charge, shoot the core to detonate it\nMinigun: MELTDOWN LASER - hold to melt everything in a line\nSniper: DEADEYE - auto-aims and fires at every target"),
                    ("F", "Hold: CHAINSAW out of the elbow - grinds enemies and heals you\nTap: PARRY orange shots\nOn a STUNNED enemy (red ring): EXECUTION\nStun them with a slam, a parry blast, a point-blank shotgun, the core or a grapple yank"),
                    ("V", "Inspect your weapon"),
                    ("1 2 3 4 / WHEEL", "Revolver · Shotgun · Minigun · Sniper"),
                    ("ESC", "Pause") };
                float pw = 1080, rowsH = 0; foreach (var r in rows) rowsH += (r.d.Split('\n').Length * 19) + 8;
                float ph = 26 + 52 + rowsH + 16 + 40 + 20 + 42 + 26;
                var panel = PanelBox(s, pw, ph);
                float y = -26;
                Place(Txt(panel, "CONTROLS", 44, C("#ff3030"), TextAnchor.UpperCenter, false, 8, 3).rectTransform, 0.5f, 1, 0.5f, 1, 0, y, pw, 50); y -= 52 + 16 - 16;
                foreach (var r in rows)
                {
                    int lines = r.d.Split('\n').Length; float rh = lines * 19;
                    var kt = Txt(panel, r.k, 16, Color.white, TextAnchor.MiddleCenter, false, 2, 0);
                    float kw = kt.preferredWidth + 2 * (r.k.Length) + 18;
                    var kb = Qd(panel, C("#c33"), "kbd"); Place(kb.rectTransform, 0, 1, 1, 1, 34 + 190, y, kw, 22);
                    var kbi = Qd(kb.rectTransform, C("#400")); Fill(kbi.rectTransform); kbi.rectTransform.offsetMin = Vector2.one; kbi.rectTransform.offsetMax = -Vector2.one;
                    kt.rectTransform.SetParent(kb.rectTransform, false); Fill(kt.rectTransform);
                    var dt = Txt(panel, r.d, 15, C("#ddd"), TextAnchor.UpperLeft, true, 0, 0); Place(dt.rectTransform, 0, 1, 0, 1, 34 + 190 + 22, y - 1, pw - 280, rh);
                    y -= rh + 8;
                }
                var tip = Txt(panel, "Clear a wave and the crystal tears open a portal: walk into it to descend to the next level. Damaging enemies up close sprays\nblood that heals you. Glowing eyes are weak points. Jump over shockwave rings. Lava and holy fire burn.", 13, C("#f88"), TextAnchor.UpperCenter, true, 0, 0);
                Place(tip.rectTransform, 0.5f, 1, 0.5f, 1, 0, y - 8, pw - 68, 40);
                var back = Btn(panel, "BACK", "back", 0, false, true); Place(back, 0.5f, 0, 0.5f, 0, 0, 26, 260, 42);
                Scanlines(s);
            }
            // settings
            {
                var s = MakeScreen("settingsPanel");
                float pw = 34 + 150 + 14 + 260 + 14 + 60 + 34, ph = 26 + 60 + 5 * 34 + 22 + 42 + 26;
                var panel = PanelBox(s, pw, ph);
                Place(Txt(panel, "SETTINGS", 44, C("#ff3030"), TextAnchor.UpperCenter, false, 8, 3).rectTransform, 0.5f, 1, 0.5f, 1, 0, -26, pw, 50);
                float y = -26 - 60;
                SliderRow(panel, "SENSITIVITY", 0.2f, 3, 0.05f, () => setSens, v => setSens = v, v => v.ToString("0.00"), ref y);
                SliderRow(panel, "FIELD OF VIEW", 75, 120, 1, () => setFov, v => setFov = v, v => Mathf.RoundToInt(v).ToString(), ref y);
                SliderRow(panel, "MASTER VOLUME", 0, 1, 0.05f, () => setVol, v => setVol = v, v => Mathf.RoundToInt(v * 100) + "%", ref y);
                SliderRow(panel, "MUSIC VOLUME", 0, 1, 0.05f, () => setMusic, v => setMusic = v, v => Mathf.RoundToInt(v * 100) + "%", ref y);
                // screen shake checkbox
                Place(Txt(panel, "SCREEN SHAKE", 15, Color.white, TextAnchor.MiddleLeft, true, 0, 0).rectTransform, 0, 1, 0, 0.5f, 34, y - 15, 150, 30);
                var box = Qd(panel, C("#e22"), "chk"); Place(box.rectTransform, 0, 1, 0, 0.5f, 34 + 164, y - 15, 20, 20); box.raycastTarget = true;
                var boxIn = Qd(box.rectTransform, C("#fff")); Fill(boxIn.rectTransform); boxIn.rectTransform.offsetMin = new Vector2(2, 2); boxIn.rectTransform.offsetMax = new Vector2(-2, -2);
                var tick = Qd(boxIn.rectTransform, C("#e22")); Fill(tick.rectTransform); tick.rectTransform.offsetMin = new Vector2(3, 3); tick.rectTransform.offsetMax = new Vector2(-3, -3);
                var tg = box.gameObject.AddComponent<Toggle>(); tg.targetGraphic = box; tg.graphic = tick; tg.isOn = setShake; tg.transition = Selectable.Transition.None;
                tg.onValueChanged.AddListener(v => { setShake = v; SaveSettings(); });
                var back = Btn(panel, "BACK", "back", 0, false, true); Place(back, 0.5f, 0, 0.5f, 0, 0, 26, 260, 42);
                Scanlines(s);
            }
            // death
            {
                var s = MakeScreen("deadScreen");
                var big = Line(s, "YOU DIED", 120, C("#ff2a2a"), false, 10, 6, 125);
                deadStatsT = Txt(s, "", 20, C("#ddd"), TextAnchor.MiddleCenter, true, 3, 0); deadStatsT.lineSpacing = 1.4f; var st = Place(deadStatsT.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 1900, 102);
                var b1 = Btn(s, "RESTART (R)", "restart", 0); var b2 = Btn(s, "MAIN MENU", "menu", 0);
                Stack(s, 0, (big, 125, 6), (st, 102, 26), (b1, 54, 12), (b2, 54, 0));
                Scanlines(s);
            }
            // victory
            {
                var s = MakeScreen("winScreen");
                var big = Line(s, "GAME OVER", 120, C("#ff2a2a"), false, 10, 6, 125);
                var tag = Line(s, "YOU SAVED THE WORLD", 16, C("#ffd870"), true, 8, 0, 20);
                winStatsT = Txt(s, "", 20, C("#ddd"), TextAnchor.MiddleCenter, true, 3, 0); winStatsT.lineSpacing = 1.4f; var st = Place(winStatsT.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 1900, 102);
                var b1 = Btn(s, "PLAY AGAIN", "restart", 0, true); var b2 = Btn(s, "MAIN MENU", "menu", 0);
                Stack(s, 0, (big, 125, 0), (tag, 20, 8), (st, 102, 26), (b1, 54, 12), (b2, 54, 0));
                Scanlines(s);
            }
            BuildCoopScreens();
        }
        RectTransform PanelBox(RectTransform s, float w, float h)
        {
            var o = Qd(s, C("#a22"), "panel"); Place(o.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, w, h);
            var i = Qd(o.rectTransform, CA("#140000", 0.85f)); Fill(i.rectTransform); i.rectTransform.offsetMin = new Vector2(2, 2); i.rectTransform.offsetMax = new Vector2(-2, -2);
            return o.rectTransform;
        }
        void SliderRow(RectTransform panel, string label, float min, float max, float step, Func<float> get, Action<float> set, Func<float, string> fmt, ref float y)
        {
            Place(Txt(panel, label, 15, Color.white, TextAnchor.MiddleLeft, true, 0, 0).rectTransform, 0, 1, 0, 0.5f, 34, y - 15, 150, 30);
            var root = Place(Rt(panel, "slider"), 0, 1, 0, 0.5f, 34 + 164, y - 15, 260, 20);
            var track = Qd(root, C("#5a2020")); Place(track.rectTransform, 0, 0.5f, 0, 0.5f, 0, 0, 260, 6); track.rectTransform.anchorMax = new Vector2(1, 0.5f); track.rectTransform.sizeDelta = new Vector2(0, 6);
            var fillArea = Rt(root, "fillArea"); fillArea.anchorMin = new Vector2(0, 0.5f); fillArea.anchorMax = new Vector2(1, 0.5f); fillArea.sizeDelta = new Vector2(0, 6); fillArea.anchoredPosition = Vector2.zero;
            var fill = Qd(fillArea, C("#e22")); fill.rectTransform.sizeDelta = Vector2.zero;
            var handleArea = Rt(root, "handleArea"); Fill(handleArea); handleArea.offsetMin = new Vector2(8, 0); handleArea.offsetMax = new Vector2(-8, 0);
            var handle = Img(handleArea, Tex.Circle, C("#e22"), "handle"); handle.rectTransform.sizeDelta = new Vector2(16, 0); handle.raycastTarget = true;
            var bg = root.gameObject.AddComponent<Quad>(); bg.Solid(new Color(0, 0, 0, 0)); bg.raycastTarget = true;
            var sl = root.gameObject.AddComponent<Slider>(); sl.fillRect = fill.rectTransform; sl.handleRect = handle.rectTransform; sl.targetGraphic = handle;
            sl.minValue = min; sl.maxValue = max; sl.transition = Selectable.Transition.None; sl.value = get();
            var vt = Txt(panel, fmt(get()), 15, Color.white, TextAnchor.MiddleLeft, true, 0, 0); Place(vt.rectTransform, 0, 1, 0, 0.5f, 34 + 164 + 274, y - 15, 60, 30);
            sl.onValueChanged.AddListener(v => { v = Mathf.Round(v / step) * step; set(v); vt.text = fmt(v); ApplySettings(); SaveSettings(); });
            y -= 34;
        }

        void ShowScreen(string id)
        {
            foreach (var kv in screens) kv.Value.SetActive(kv.Key == id);
            currentScreen = id;
            foreach (var b in buttons) b.b.hover = false;
        }
        void HandleAct(string a)
        {
            Sfx("ui");
            if (CoopAct(a)) return; // co-op menu, lobby, and what host-only buttons do for clients (GameCoopUI.cs)
            if (a == "play") { practiceWave = 0; StartGame(); tutorialRun = false; }
            else if (a == "tutorial") StartTutorialRun();
            else if (a == "restart") StartGame();
            else if (a == "boss1" || a == "boss2" || a == "boss3" || a == "boss4" || a == "boss5") { practiceWave = a == "boss1" ? 5 : a == "boss3" ? 10 : a == "boss4" ? 15 : a == "boss5" ? 20 : TOTAL_WAVES; StartGame(); tutorialRun = false; }
            else if (a == "practice") { panelReturn = "mainMenu"; ShowScreen("practiceMenu"); }
            else if (a == "resume") Lock();
            else if (a == "controls" || a == "settings") { panelReturn = currentScreen; ShowScreen(a == "controls" ? "controlsPanel" : "settingsPanel"); }
            else if (a == "back") ShowScreen(panelReturn);
            else if (a == "menu") QuitToMenu();
            else if (a == "exit") ShowScreen("exitConfirm");
            else if (a == "exitNo") ShowScreen("mainMenu");
            else if (a == "exitYes")
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        // ---------- HUD hooks used by the game code ----------
        void SetHudVisible(bool on) => hudGO.SetActive(on);
        void SetHint(string s) { if (s == null) { hintGO.SetActive(false); return; } hintT.text = s; hintGO.SetActive(true); }
        void Banner(string main, string sub = "", string color = "#ff3333")
        {
            bannerMain.text = main; bannerSub.text = sub ?? ""; bannerMain.color = C(color); bannerT = 0;
        }
        void ScorePop(string s) { scorePopT.text = s; scorePopT_ = 0; }
        void SetDeadStats(string s) => deadStatsT.text = s;
        void SetWinStats(string s) => winStatsT.text = s;
        Quad bossFillQ;
        GameObject bossMidMark; // the enrage marker at half health (the Frozen Traitor has none: his phase 2 refills the bar instead)
        void ShowBossBar(Ent e) { bossNameT.text = e.name; bossBarGO.SetActive(true); if (bossMidMark) bossMidMark.SetActive(e.boss != 3); SetFill(bossFill, 1); SetFill(bossTrailRT, 1); bossTrail = 1; BossBarTint(C("#ff3a3a"), C("#900")); }
        // recolour the boss bar's fill (the Frozen Traitor's phase-2 refill floods it with frost)
        void BossBarTint(Color top, Color bottom) { if (bossFillQ) bossFillQ.Vertical(top, bottom); }
        void SetBossName(string n) { if (bossNameT) bossNameT.text = n; }
        void HideBossBar() { if (bossBarGO) bossBarGO.SetActive(false); }
        void SetWeaponHud(int i)
        {
            if (wnameT == null) return;
            wnameT.text = guns[i].name; wtipT.text = guns[i].tip;
            if (i == 4) wlistT.text = "<color=#ffffff>★ CHEAT ACTIVE ★</color>";
            else
            {
                var parts = new List<string>();
                for (int k = 0; k < 4; k++) parts.Add(k == i ? $"<color=#ff3a3a>{k + 1}</color> <color=#ffffff>{guns[k].name}</color>" : $"<color=#5a2a2a>{k + 1}</color> {guns[k].name}");
                wlistT.text = string.Join("    ", parts);
            }
            heatGO.SetActive(i == 2);
            var p2 = wnameT.rectTransform.anchoredPosition; p2.y = i == 2 ? 16 : 4; wnameT.rectTransform.anchoredPosition = p2;
            var p3 = wlistT.rectTransform.anchoredPosition; p3.y = p2.y + 38; wlistT.rectTransform.anchoredPosition = p3;
        }
        void Ability(int i, bool ready, string label, string cd)
        {
            var a = abil[i];
            a.t.text = ready ? label : (cd.Length > 0 ? $"{label} {cd}" : label);
            a.t.color = ready ? Color.white : C("#888"); a.border.Solid(ready ? C("#c33") : C("#444"));
        }

        void UpdateHUD(float rdt)
        {
            SetFill(hpFill, P.hp / 100f); hpText.text = Mathf.CeilToInt(P.hp).ToString();
            for (int i = 0; i < 3; i++) SetFill(stamFills[i], clamp(P.stamina - i, 0, 1));
            for (int i = 0; i < 4; i++) coinEls[i].GetComponent<CanvasGroup>().alpha = P.coins >= i + 1 ? 1 : 0.2f;
            SetFill(heatFill, P.heat);
            if (P.overheat > 0) heatFillQ.Solid(Mathf.Sin(time * 30) > 0 ? Color.white : C("#f30")); else heatFillQ.Horizontal(C("#fa0"), C("#f30"));
            speedT.text = "";
            ammoT.text = AmmoText();
            int aw = P.weapon, mag = NeedsAmmo(aw) ? MAG[aw] : 0;
            for (int i = 0; i < ammoPips.Count; i++)
            {
                // pips fill right-to-left as rounds; the next one to fire is on the left
                bool on = i < mag; ammoPips[i].gameObject.SetActive(on); if (!on) continue;
                bool full = (mag - 1 - i) < ammo[aw];
                ammoPips[i].Solid(full ? C("#ffe6a0") : CA("#ff3030", reloadT > 0 ? 0.25f : 0.5f));
            }
            topT.text = phase == "fight" ? $"{LEVEL.name} — WAVE {wave}" : LEVEL.name;
            displayScore += (S.score - displayScore) * Mathf.Min(1, rdt * 8); if (Mathf.Abs(S.score - displayScore) < 1) displayScore = S.score;
            UpdateScoreRoller(rdt);
            multT.text = S.rank >= 0 ? $"x{StyleMult:0.00}" : "";
            var mp = multT.rectTransform.anchoredPosition; mp.x = DIG_W * SCORE_DIGITS * scoreRow.localScale.x + 12; multT.rectTransform.anchoredPosition = mp;
            if (S.rank >= 0)
            {
                var r = RANKS[S.rank];
                rankLT.text = r.l; rankLT.color = C(r.c); rankNT.text = r.n;
                SetFill(rankFill, S.fill); rankFillQ.Solid(C(r.c)); rankFill.parent.parent.gameObject.SetActive(true);
            }
            else { rankLT.text = ""; rankNT.text = ""; rankFill.parent.parent.gameObject.SetActive(false); }
            UpdateRankFx(rdt); UpdateCrack(rdt);
            if (S.dirty)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var f in S.feed) sb.Append(f.txt).Append('\n');
                feedT.text = sb.ToString(); S.dirty = false;
            }
            if (boss != null)
            {
                float f = clamp(boss.hp / boss.maxHp, 0, 1);
                bossTrail = Mathf.Max(f, bossTrail - rdt * 0.25f);
                SetFill(bossFill, f); SetFill(bossTrailRT, bossTrail);
            }
            Ability(0, HK.cd <= 0 && HK.state == "idle", "HOOK", "");
            Ability(1, P.coreCd <= 0, "CORE", Mathf.CeilToInt(Mathf.Max(0, P.coreCd)) + "s");
            Ability(2, DE.cd <= 0 && !DE.active, "DEADEYE", DE.active ? "" : Mathf.CeilToInt(DE.cd) + "s");
            // lay the ability chips out like an inline-flex row
            float total = 0; var ws = new float[3];
            for (int i = 0; i < 3; i++) { ws[i] = abil[i].t.preferredWidth + abil[i].t.text.Length * 2 + 20 + 2; total += ws[i]; }
            total += 20; float x = -total / 2;
            for (int i = 0; i < 3; i++) { var rt = abil[i].border.rectTransform; rt.sizeDelta = new Vector2(ws[i], 27); rt.anchoredPosition = new Vector2(x + ws[i] / 2, 26); x += ws[i] + 10; }
            crossVisible(!DE.active);
            bool cheatOn = P.cheatT > 0;
            cheatGO.SetActive(cheatOn);
            if (cheatOn) { cheatT.text = $"★ BANG {P.cheatT:0.0}s ★"; SetFill(cheatFill, P.cheatT / 10f); }
            hurtFlash = Mathf.Max(0, hurtFlash - rdt * 2); whiteFlash = Mathf.Max(0, whiteFlash - rdt * 3);
            hurtImg.color = new Color(1, 1, 1, Mathf.Max(hurtFlash, P.hp < 30 && !P.dead ? 0.35f + Mathf.Sin(time * 8) * 0.1f : 0));
            flashImg.color = new Color(1, 1, 1, whiteFlash);
        }
        void crossVisible(bool on) { var c = hudRT.Find("cross"); if (c) c.gameObject.SetActive(on); }

        // CSS animations
        void UpdateUIAnims(float rdt)
        {
            uiTime += rdt;
            // banner: 2.8s, scale 1.6 -> 1 in the first 8%, fade out over the last 20%
            bannerT += rdt;
            {
                float p = bannerT / 2.8f, a, s;
                if (p >= 1) { a = 0; s = 1; }
                else if (p < 0.08f) { float k = p / 0.08f; a = k; s = lerp(1.6f, 1, 1 - (1 - k) * (1 - k)); }
                else if (p < 0.8f) { a = 1; s = 1; }
                else { a = 1 - (p - 0.8f) / 0.2f; s = 1; }
                bannerGroup.alpha = a; bannerRT.localScale = Vector3.one * s;
            }
            // score pop: .8s ease-out
            scorePopT_ += rdt;
            {
                float p = Mathf.Clamp01(scorePopT_ / 0.8f), e = 1 - (1 - p) * (1 - p);
                scorePopGroup.alpha = 1 - e; scorePopRT.localScale = Vector3.one * lerp(1.3f, 1, e);
                var pp = scorePopRT.anchoredPosition; pp.y = -76 - lerp(6, -14, e); scorePopRT.anchoredPosition = pp;
            }
            // blink animations
            if (hintGroup) hintGroup.alpha = 1 - 0.65f * (0.5f - 0.5f * Mathf.Cos(uiTime * Mathf.PI * 2 / 1f));
            if (cheatTxtGroup) cheatTxtGroup.alpha = 1 - 0.65f * (0.5f - 0.5f * Mathf.Cos(uiTime * Mathf.PI * 2 / 0.5f));
            // title glitch
            if (titleRT)
            {
                float g = (uiTime % 3) / 3; Vector2 off = Vector2.zero; float sk = 0;
                if (g >= 0.92f && g < 0.94f) { off = new Vector2(4, 2); sk = -6; }
                else if (g >= 0.94f && g < 0.96f) off = new Vector2(-5, -2);
                else if (g >= 0.96f && g < 0.98f) { off = new Vector2(2, 0); sk = 4; }
                var tp = titleRT.anchoredPosition; titleRT.anchoredPosition = new Vector2(off.x, tp.y); titleRT.localRotation = Quaternion.Euler(0, 0, -sk * 0.3f);
            }
            // buttons: hover slides right, brightens and widens the letter spacing
            foreach (var b in buttons)
            {
                b.b.h = Mathf.MoveTowards(b.b.h, b.b.hover ? 1 : 0, rdt / 0.12f);
                float h = b.b.h;
                b.inner.anchoredPosition = new Vector2(10 * h, 0);
                if (b.sp) { b.sp.px = lerp(6, 9, h); b.sp.GetComponent<Graphic>().SetVerticesDirty(); }
                Color l0 = b.holy ? CA("#c8aa5a", 0.8f) : CA("#960000", 0.75f), r0 = b.holy ? CA("#3c321e", 0.8f) : CA("#280000", 0.75f);
                Color l1 = b.holy ? C("#ffe090") : C("#ff2a2a"), r1 = b.holy ? C("#a07820") : C("#900");
                if (btnTheme.TryGetValue(b.b, out var th))
                {
                    if (th == 2) { l0 = CA("#3a7aa8", 0.8f); r0 = CA("#0c2436", 0.8f); l1 = C("#9ae0ff"); r1 = C("#2a6a9a"); }
                    else if (th == 4) { l0 = CA("#8a8a6a", 0.8f); r0 = CA("#1e1e16", 0.8f); l1 = C("#f0e0a0"); r1 = C("#6a6038"); }
                    else { l0 = CA("#b0501a", 0.8f); r0 = CA("#2e1406", 0.8f); l1 = C("#ff9a40"); r1 = C("#a04010"); }
                }
                b.bgI.Horizontal(Color.Lerp(l0, l1, h), Color.Lerp(r0, r1, h));
            }
            if (warpGroup.alpha > 0)
            {
                warpRays.localRotation = Quaternion.Euler(0, 0, -(uiTime / 1.4f % 1) * 360);
                float pp = Mathf.PingPong(uiTime / 0.5f, 1), e = pp * pp * (3 - 2 * pp);
                warpPulse.localScale = Vector3.one * lerp(1, 1.6f, e); warpPulse.GetComponent<RawImage>().color = new Color(1, 1, 1, lerp(1, 0.6f, e));
                var sz = canvasRT.rect.size; float diag = sz.magnitude; warpPulse.sizeDelta = new Vector2(diag * 0.44f * 1.4f, diag * 0.44f * 1.4f);
                warpRays.sizeDelta = Vector2.one * Mathf.Max(sz.x, sz.y) * 2.2f;
            }
            if (cineGO.activeSelf)
            {
                var sz = canvasRT.rect.size; float vh = sz.y / 100f;
                cineBarT += rdt; float k = Mathf.Clamp01(cineBarT / 1.4f); k = 1 - (1 - k) * (1 - k) * (1 - k);
                if (testRec) { k = 0; skipRT.gameObject.SetActive(false); } // -brrec: full-frame footage, no letterbox or skip hint
                cineTopBar.sizeDelta = new Vector2(sz.x + 10, 12 * vh * k); cineBotBar.sizeDelta = new Vector2(sz.x + 10, 12 * vh * k);
                skipRT.anchoredPosition = new Vector2(-22, 12 * vh + 12);
                cineCapGroup.alpha = Mathf.MoveTowards(cineCapGroup.alpha, cineCapOn ? 1 : 0, rdt / 1.1f);
                cineSmallGroup.alpha = Mathf.MoveTowards(cineSmallGroup.alpha, cineSmallOn ? 1 : 0, rdt / 1.1f);
                if (cineTitleGO.activeSelf)
                {
                    cineTitleT += rdt; float t = Mathf.Clamp01(cineTitleT / 3f), e = 1 - (1 - t) * (1 - t);
                    cineTitleGroup.alpha = e; cineTitleRT.localScale = Vector3.one * lerp(1.15f, 1, e);
                }
            }
            if (uiScope.activeSelf) LayoutScope();
            foreach (var s in scanImgs) { var sz = canvasRT.rect.size; s.uvRect = new Rect(0, 0, sz.x / 4f, sz.y / 4f); }
        }
    }
}
