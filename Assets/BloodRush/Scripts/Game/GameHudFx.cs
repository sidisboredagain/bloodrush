using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BR
{
    // HUD showmanship: an odometer score, a style rank that slams/shakes/burns, a screen crack at low health,
    // and a boss intro card (letterbox, camera on the boss, name slammed on screen).
    public partial class Game
    {
        // ---------- rolling score: seven odometer wheels ----------
        const int SCORE_DIGITS = 7; const float DIG_W = 29, DIG_H = 50;
        readonly RectTransform[] scoreStrips = new RectTransform[SCORE_DIGITS]; RectTransform scoreRow;
        readonly List<Text> scoreDigitTexts = new List<Text>();
        float scorePunch, lastScoreSeen;
        void BuildScoreRoller(RectTransform sb)
        {
            scoreRow = Place(Rt(sb, "scoreRow"), 0, 1, 0, 1, 0, -16, DIG_W * SCORE_DIGITS, DIG_H);
            for (int c = 0; c < SCORE_DIGITS; c++)
            {
                var col = Place(Rt(scoreRow, "wheel"), 0, 1, 0, 1, c * DIG_W, 0, DIG_W, DIG_H);
                col.gameObject.AddComponent<RectMask2D>();
                var strip = Place(Rt(col, "strip"), 0, 1, 0, 1, 0, 0, DIG_W, DIG_H * 11);
                for (int d = 0; d <= 10; d++)
                {
                    var t = Txt(strip, (d % 10).ToString(), 46, Color.white, TextAnchor.MiddleCenter, false, 0, 3);
                    Place(t.rectTransform, 0, 1, 0, 1, 0, -d * DIG_H, DIG_W, DIG_H); scoreDigitTexts.Add(t);
                }
                scoreStrips[SCORE_DIGITS - 1 - c] = strip; // index 0 = ones
            }
        }
        void UpdateScoreRoller(float rdt)
        {
            float v = Mathf.Max(0, displayScore);
            for (int i = 0; i < SCORE_DIGITS; i++)
            {
                float pow = Mathf.Pow(10, i), place = v / pow;
                float digit = Mathf.Floor(place) % 10, pos;
                if (i == 0) pos = v % 10; // the ones wheel spins freely
                else { float lower = (v % pow) / pow; pos = digit + Mathf.Max(0, (lower - 0.9f) / 0.1f); } // tens+ roll over at the end
                scoreStrips[i].anchoredPosition = new Vector2(0, pos * DIG_H);
            }
            if (S.score > lastScoreSeen + 0.5f) scorePunch = 1; lastScoreSeen = S.score;
            scorePunch = Mathf.Max(0, scorePunch - rdt * 4);
            scoreRow.localScale = Vector3.one * (1 + scorePunch * 0.12f);
            var col = Color.Lerp(Color.white, C("#ffd24a"), scorePunch);
            foreach (var t in scoreDigitTexts) t.color = col;
        }

        // ---------- style rank: slams in, shakes at S+, burns at SSS ----------
        int lastRank = -1; float rankPunch, rankDrop; Text rankGhost;
        void BuildRankFx(RectTransform sty)
        {
            rankGhost = Txt(sty, "", 80, Color.white, TextAnchor.UpperRight, false, 0, 0); Place(rankGhost.rectTransform, 1, 1, 1, 1, 0, 0, 300, 82);
            rankGhost.transform.SetSiblingIndex(0);
        }
        void UpdateRankFx(float rdt)
        {
            int r = S.rank;
            if (r != lastRank)
            {
                if (r > lastRank && r >= 0) { rankPunch = 1; rankGhost.text = RANKS[r].l; Sfx(r >= 4 ? "parry" : "ui"); }
                else if (r < lastRank) rankDrop = 1;
                lastRank = r;
            }
            rankPunch = Mathf.Max(0, rankPunch - rdt * 3); rankDrop = Mathf.Max(0, rankDrop - rdt * 3);
            var rt = rankLT.rectTransform;
            if (r < 0) { rt.localScale = Vector3.one; rankGhost.color = Color.clear; return; }
            // slam: overshoots big then settles with a little bounce
            float e = rankPunch, slam = 1 + e * e * 1.1f - Mathf.Sin((1 - e) * Mathf.PI * 3) * 0.08f * e;
            float shake = r >= 4 ? (r - 3) * 1.6f : 0;
            var jitter = new Vector2((rand - 0.5f) * shake, (rand - 0.5f) * shake) + new Vector2(0, -rankDrop * 10);
            rt.anchoredPosition = jitter; rt.localScale = Vector3.one * slam;
            rt.localRotation = Quaternion.Euler(0, 0, r >= 5 ? Mathf.Sin(time * 9) * (r - 4) * 2.5f : 0);
            var baseC = C(RANKS[r].c);
            if (r >= 6 && r < 7) baseC = Color.Lerp(C("#ff1a1a"), C("#ffb020"), 0.5f + 0.5f * Mathf.Sin(time * 22 + Mathf.Sin(time * 7) * 2)); // SSS burns
            if (r == 7) baseC = Color.Lerp(C("#ffd700"), Color.white, 0.5f + 0.5f * Mathf.Sin(time * 14));
            rankLT.color = Color.Lerp(baseC, Color.white, e * 0.8f);
            if (rankDrop > 0) rankLT.color = Color.Lerp(rankLT.color, C("#660000"), rankDrop);
            // ghost: a big afterimage that flies out of the letter when you rank up
            rankGhost.rectTransform.localScale = Vector3.one * (1 + (1 - e) * 1.4f);
            rankGhost.color = new Color(baseC.r, baseC.g, baseC.b, e * 0.6f);
            if (r >= 6 && rand < 0.5f) rankNT.color = Color.Lerp(C("#ff6040"), C("#ffd060"), rand); else rankNT.color = Color.white;
        }

        // ---------- screen crack at low health ----------
        RawImage crackImg; float crackHit, lastHp = 100;
        void BuildCrack() { crackImg = Img(hudRT, Tex.Cracks, new Color(1, 1, 1, 0), "cracks"); Fill(crackImg.rectTransform); }
        void UpdateCrack(float rdt)
        {
            if (P.hp < lastHp - 0.5f && P.hp < 40) crackHit = 1; lastHp = P.hp;
            crackHit = Mathf.Max(0, crackHit - rdt * 2);
            float low = P.dead ? 0 : Mathf.Clamp01((38 - P.hp) / 28);
            crackImg.color = new Color(1, 1, 1, Mathf.Clamp01(low * 0.85f + crackHit * low * 0.3f));
            crackImg.rectTransform.localScale = Vector3.one * (1 + crackHit * 0.015f);
        }

        // ---------- boss intro card ----------
        const float BOSS_INTRO = 3.4f;
        float bossIntroT; Vector3 introCamFrom; Quaternion introCamFromRot;
        RectTransform introRT, barTop, barBot, introName; CanvasGroup introGroup; Text introMain, introSub, introTag;
        void BuildBossIntro()
        {
            introRT = Fill(Rt(hudRT, "bossIntro")); introGroup = introRT.gameObject.AddComponent<CanvasGroup>(); introGroup.alpha = 0;
            barTop = Qd(introRT, Color.black).rectTransform; barTop.anchorMin = new Vector2(0, 1); barTop.anchorMax = new Vector2(1, 1); barTop.pivot = new Vector2(0.5f, 1); barTop.sizeDelta = new Vector2(0, 0);
            barBot = Qd(introRT, Color.black).rectTransform; barBot.anchorMin = new Vector2(0, 0); barBot.anchorMax = new Vector2(1, 0); barBot.pivot = new Vector2(0.5f, 0); barBot.sizeDelta = new Vector2(0, 0);
            introName = Place(Rt(introRT, "name"), 0.08f, 0.32f, 0, 0.5f, 0, 0, 1600, 220);
            var stripe = Qd(introName, CA("#b00000", 0.85f)); Place(stripe.rectTransform, 0, 0.5f, 0, 0.5f, -40, -6, 1500, 120);
            var stripe2 = Qd(introName, C("#ffd24a")); Place(stripe2.rectTransform, 0, 0.5f, 0, 0.5f, -40, -70, 900, 6);
            introTag = Txt(introName, "", 22, C("#ffd24a"), TextAnchor.LowerLeft, false, 8, 2); Place(introTag.rectTransform, 0, 0.5f, 0, 0, 0, 66, 1400, 30);
            introMain = Txt(introName, "", 120, Color.white, TextAnchor.MiddleLeft, false, 10, 6); Place(introMain.rectTransform, 0, 0.5f, 0, 0.5f, 0, -6, 1600, 130);
            introSub = Txt(introName, "", 30, C("#ffe0c0"), TextAnchor.UpperLeft, false, 8, 3); Place(introSub.rectTransform, 0, 0.5f, 0, 1, 0, -78, 1400, 36);
        }
        void StartBossIntro(Ent b)
        {
            if (b != null && TryBossCine(b.boss, 0, b)) return; // the four great bosses get their story instead (GameBossCine.cs)
            if (b == null || testMode && Arg("-brbossintro") == null && Arg("-brtut") == null) return;
            string n = b.name ?? "BOSS"; int comma = n.IndexOf(','); string main = comma > 0 ? n.Substring(0, comma) : n.Split(' ')[0];
            string sub = comma > 0 ? n.Substring(comma + 1).Trim() : n.Substring(main.Length).Trim();
            if (comma < 0 && n.StartsWith("THE ")) { main = n; sub = ""; } // "THE HOLLOW KING": the whole name on the red band, nothing under it
            introMain.text = main; introSub.text = sub; introTag.text = $"LEVEL {levelNum}  ·  BOSS";
            bossIntroT = BOSS_INTRO; introCamFrom = camT.position; introCamFromRot = camT.rotation;
            mouseDown = false; Sfx("roar");
        }
        bool BossIntroActive => bossIntroT > 0;
        // runs after UpdateCamera: takes the camera to the boss and back
        void UpdateBossIntro(float rdt)
        {
            if (bossIntroT <= 0) { if (introGroup.alpha > 0) introGroup.alpha = Mathf.Max(0, introGroup.alpha - rdt * 4); return; }
            bossIntroT -= rdt;
            float t = BOSS_INTRO - bossIntroT, inK = Smoother(t / 0.55f), outK = Smoother(bossIntroT / 0.45f), k = Mathf.Min(inK, outK);
            if (boss != null && !boss.dead)
            {
                var flat = P.pos - boss.pos; flat.y = 0; flat = flat.sqrMagnitude > 1 ? flat.normalized : Vector3.forward;
                var side = Vector3.Cross(Vector3.up, flat);
                float push = t * 0.9f;
                var look = TargetPoint(boss) - V(0, boss.headY * 0.25f, 0);
                var pos = boss.pos + flat * (6 + boss.s * 3.2f - push) + side * (2.5f - t * 0.6f) + V(0, boss.headY * 0.45f, 0);
                var rot = Quaternion.LookRotation(pos - look); // three.js-style camera looks down -Z
                camT.position = Vector3.Lerp(camT.position, pos, k); camT.rotation = Quaternion.Slerp(camT.rotation, rot, k);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 42, k);
            }
            vm.gameObject.SetActive(k < 0.3f && !DE.active);
            // card: bars close in, name slams from the left, everything leaves together
            introGroup.alpha = Mathf.Min(1, Smoother((t - 0.15f) / 0.2f)) * outK;
            float bar = 110 * k; barTop.sizeDelta = new Vector2(0, bar); barBot.sizeDelta = new Vector2(0, bar);
            float slam = Smoother((t - 0.35f) / 0.25f);
            introName.anchoredPosition = new Vector2(Mathf.Lerp(-900, 0, slam) + t * 14, 0);
            introMain.rectTransform.localScale = Vector3.one * (1 + (1 - Smoother((t - 0.55f) / 0.25f)) * 0.35f * slam);
            if (t > 0.55f && t - rdt <= 0.55f) { P.shake = Mathf.Max(P.shake, 0.5f); Sfx("slam"); }
        }
    }
}
