using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BR
{
    // First-time tutorial cards (each ability is explained once, the first time it matters; the TUTORIAL button in the
    // main menu forgets them and starts a run that shows them all again) and the off-screen damage indicator.
    public partial class Game
    {
        // ---------- tutorial cards ----------
        class Tut { public string id, title, body; public Func<bool> when, done; }
        List<Tut> TUTS; RectTransform tutRT; CanvasGroup tutGroup; Text tutTitle, tutBody;
        Tut tutCur; float tutT, tutGap = 1.5f, tutDoneAt = -1; bool levelCardPending, tutorialRun; // cards only show in a run started from the TUTORIAL button
        const string TUT_KEY = "br_tut_seen";

        void BuildTutorialUI()
        {
            tutRT = Place(Rt(hudRT, "tutorial"), 0, 0.64f, 0, 0.5f, 24, 0, 470, 104); tutGroup = tutRT.gameObject.AddComponent<CanvasGroup>(); tutGroup.alpha = 0;
            var border = Qd(tutRT, C("#c22")); Fill(border.rectTransform);
            var bg = Qd(tutRT, CA("#0a0000", 0.86f)); Fill(bg.rectTransform); bg.rectTransform.offsetMin = new Vector2(4, 2); bg.rectTransform.offsetMax = new Vector2(-2, -2);
            var tag = Txt(tutRT, "NEW", 13, C("#ffd24a"), TextAnchor.UpperRight, false, 3, 1); Place(tag.rectTransform, 1, 1, 1, 1, -12, -9, 100, 16);
            tutTitle = Txt(tutRT, "", 26, C("#ff4040"), TextAnchor.UpperLeft, false, 4, 2); Place(tutTitle.rectTransform, 0, 1, 0, 1, 18, -8, 420, 30);
            tutBody = Txt(tutRT, "", 16, Color.white, TextAnchor.UpperLeft, true, 0, 1); Place(tutBody.rectTransform, 0, 1, 0, 1, 18, -42, 440, 60);
            tutBody.horizontalOverflow = HorizontalWrapMode.Wrap; tutBody.lineSpacing = 1.15f;
            for (int i = 0; i < dmgArcs.Length; i++)
            {
                var piv = Place(Rt(hudRT, "dmgDir"), 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 640, 640);
                var img = Img(piv, Tex.DamageArc, new Color(1, 1, 1, 0), "arc"); Fill(img.rectTransform);
                dmgArcs[i] = (piv, img, Vector3.zero, 0);
            }
        }
        void DefineTutorials()
        {
            bool fight() => phase == "fight";
            TUTS = new List<Tut>
            {
                new Tut { id = "move", title = "MOVE", body = "<color=#ffd24a>WASD</color> to move   <color=#ffd24a>SPACE</color> to jump\nkeep moving - standing still gets you killed",
                    when = () => true, done = () => !P.onGround },
                new Tut { id = "djump", title = "DOUBLE JUMP", body = "press <color=#ffd24a>SPACE</color> again in mid-air to jump again\nrecharges in 2 seconds",
                    when = () => !P.onGround && P.airJumpCd <= 0, done = () => P.airJumpCd > 0 },
                new Tut { id = "dash", title = "DASH", body = "<color=#ffd24a>SHIFT</color> to dash - you can't be hurt mid-dash\n3 charges, they refill over time",
                    when = () => true, done = () => P.dashT > 0 },
                new Tut { id = "shoot", title = "WEAPONS", body = "<color=#ffd24a>LMB</color> to fire   <color=#ffd24a>1-4</color> or <color=#ffd24a>SCROLL</color> to switch\nhurting enemies up close <color=#ff6060>heals you</color>",
                    when = fight, done = () => mouseDown },
                new Tut { id = "coin", title = "COIN TOSS", body = "Revolver: <color=#ffd24a>RMB</color> tosses a coin\nshoot the coin - it ricochets into a headshot",
                    when = () => fight() && P.weapon == 0, done = () => coins.Count > 0 },
                new Tut { id = "hook", title = "GRAPPLE HOOK", body = "<color=#ffd24a>Q</color> fires the hook from your wrist\nsmall enemies get reeled in - big ones pull YOU",
                    when = fight, done = () => HK.state != "idle" },
                new Tut { id = "slide", title = "SLIDE & SLAM", body = "<color=#ffd24a>C</color> on the ground to slide\n<color=#ffd24a>C</color> in the air to slam down - hits everything nearby",
                    when = fight, done = () => P.sliding || P.slamming },
                new Tut { id = "punch", title = "CHAINSAW", body = "hold <color=#ffd24a>F</color>: a chainsaw slides out of your elbow - grinding <color=#ff6060>heals you</color>\ntap F into orange enemy shots to send them back",
                    when = fight, done = () => fistT > 0 },
                new Tut { id = "exec", title = "EXECUTIONS", body = "slams, parried blasts, point-blank shotgun and the grapple <color=#ff4040>STUN</color> enemies\nget close to one with a red ring and press <color=#ffd24a>F</color> to EXECUTE it",
                    when = () => enemies.Exists(e => e.stagT > 0), done = () => EX.active },
                new Tut { id = "core", title = "SHOTGUN CORE", body = "Shotgun: hold <color=#ffd24a>RMB</color>, release to lob a core\n<color=#80e8ff>SHOOT IT</color> mid-air: implosion + chain lightning",
                    when = () => P.weapon == 1, done = () => cores.Count > 0 },
                new Tut { id = "minigun", title = "MINIGUN", body = "hold <color=#ffd24a>LMB</color> to spin up and shred - watch the heat bar\nhold <color=#ffd24a>RMB</color>: MELTDOWN LASER - huge damage, then a 10s lockout",
                    when = () => P.weapon == 2, done = () => P.spin > 0.5f },
                new Tut { id = "sniper", title = "SNIPER & DEADEYE", body = "one shot pierces everything in a line\n<color=#ffd24a>RMB</color>: DEADEYE - mark targets, then fire",
                    when = () => P.weapon == 3, done = () => DE.active },
                new Tut { id = "inspect", title = "LOOK COOL", body = "<color=#ffd24a>V</color> to inspect your weapon\nstyle points come from variety - mix it up",
                    when = () => phase == "cleared" || phase == "portal", done = () => inspectT > 0 },
                new Tut { id = "reload", title = "RELOAD", body = "<color=#ffd24a>R</color> to reload early - empty guns reload on their own\nshotgun: fire mid-reload to shoot what's loaded",
                    when = () => NeedsAmmo(P.weapon) && (reloadT > 0 || ammo[P.weapon] <= MAG[P.weapon] / 2), done = () => reloadT > 0 },
            };
        }
        int tutSeenTest; // self-tests must not touch the player's saved progress
        int TutSeen() => testMode ? tutSeenTest : PlayerPrefs.GetInt(TUT_KEY, 0);
        void MarkTut(Tut t)
        {
            int bit = 1 << TUTS.IndexOf(t);
            if (testMode) { tutSeenTest |= bit; return; }
            PlayerPrefs.SetInt(TUT_KEY, TutSeen() | bit); PlayerPrefs.Save();
        }
        // TUTORIAL button: forget what was seen and start a run where every card shows again
        void StartTutorialRun()
        {
            PlayerPrefs.SetInt(TUT_KEY, 0); PlayerPrefs.Save();
            practiceWave = 0; StartGame(); tutorialRun = true;
        }
        void ResetTutorialRun() { tutCur = null; tutT = 0; tutGap = 1.5f; tutDoneAt = -1; if (tutGroup) tutGroup.alpha = 0; }
        void UpdateTutorial(float rdt)
        {
            if (TUTS == null) DefineTutorials();
            if (!tutorialRun || !started || P == null || P.dead || won || CINE.active || (testMode && Arg("-brtut") == null)) { if (tutGroup) tutGroup.alpha = Mathf.Max(0, tutGroup.alpha - rdt * 4); return; }
            if (tutCur == null)
            {
                tutGap -= rdt;
                if (tutGap <= 0)
                {
                    int seen = TutSeen();
                    for (int i = 0; i < TUTS.Count; i++)
                        if ((seen & (1 << i)) == 0 && TUTS[i].when()) { tutCur = TUTS[i]; tutT = 0; tutTitle.text = tutCur.title; tutBody.text = tutCur.body; MarkTut(tutCur); Sfx("ui"); break; }
                }
            }
            else
            {
                tutT += rdt;
                // stays at least 3 s, leaves 1.5 s after you've done it (or after 9 s)
                if (tutDoneAt < 0 && tutT > 0.8f && tutCur.done()) tutDoneAt = tutT;
                if ((tutDoneAt >= 0 && tutT > Mathf.Max(3, tutDoneAt + 1.5f)) || tutT > 9) { tutCur = null; tutDoneAt = -1; tutGap = 2.2f; }
            }
            float target = tutCur != null ? 1 : 0;
            tutGroup.alpha += (target - tutGroup.alpha) * (1 - Mathf.Exp(-10 * rdt));
            var p = tutRT.anchoredPosition; p.x = 24 - (1 - tutGroup.alpha) * 60; tutRT.anchoredPosition = p;
        }

        // ---------- off-screen damage indicator: a red arc at the screen edge pointing at what hit you ----------
        readonly (RectTransform piv, RawImage img, Vector3 from, float t)[] dmgArcs = new (RectTransform, RawImage, Vector3, float)[4];
        int dmgNext;
        void ShowDamageFrom(Vector3 from)
        {
            // reuse the arc already pointing that way, otherwise take the next one
            int idx = -1;
            for (int i = 0; i < dmgArcs.Length; i++) if (dmgArcs[i].t > 0 && Vector3.Distance(dmgArcs[i].from, from) < 3) idx = i;
            if (idx < 0) { idx = dmgNext; dmgNext = (dmgNext + 1) % dmgArcs.Length; }
            dmgArcs[idx].from = from; dmgArcs[idx].t = 1.3f;
        }
        void UpdateDamageArcs(float rdt)
        {
            for (int i = 0; i < dmgArcs.Length; i++)
            {
                var a = dmgArcs[i];
                if (a.t <= 0 || P == null) { a.img.color = new Color(1, 1, 1, 0); continue; }
                a.t -= rdt;
                var d = a.from - P.pos; d.y = 0;
                float fx = Vector3.Dot(d, FlatRight), fz = Vector3.Dot(d, FlatFwd);
                float ang = Mathf.Atan2(fx, fz) * Mathf.Rad2Deg;
                a.piv.localRotation = Quaternion.Euler(0, 0, -ang);
                float al = Mathf.Clamp01(a.t / 0.6f) * (0.75f + 0.25f * Mathf.Sin(time * 30));
                a.img.color = new Color(1, 1, 1, al);
                dmgArcs[i] = a;
            }
        }
    }
}
