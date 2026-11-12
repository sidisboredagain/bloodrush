using UnityEngine;
using UnityEngine.UI;

namespace BR
{
    // CO-OP menus: CO-OP (host / join online with a code, or over LAN / IP), the lobby, and the teammates list on the HUD.
    public partial class Game
    {
        Text coopStatusT, lobbyCodeT, lobbyPlayersT, lobbyStatusT, teamT; InputField codeIn, ipIn; GameObject lobbyStartGO;

        InputField MakeInput(RectTransform parent, string placeholder, float w)
        {
            var root = Place(Rt(parent, "input"), 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, w, 46);
            var o = Qd(root, C("#c22")); Fill(o.rectTransform);
            var bg = Qd(root, C("#200")); Fill(bg.rectTransform); bg.rectTransform.offsetMin = new Vector2(2, 2); bg.rectTransform.offsetMax = new Vector2(-2, -2); bg.raycastTarget = true;
            var t = Txt(root, "", 26, Color.white, TextAnchor.MiddleCenter, true, 0, 0); Fill(t.rectTransform); t.supportRichText = false;
            var ph = Txt(root, placeholder, 20, C("#a55"), TextAnchor.MiddleCenter, true, 0, 0); Fill(ph.rectTransform);
            var f = root.gameObject.AddComponent<InputField>(); f.textComponent = t; f.placeholder = ph; f.targetGraphic = bg; f.characterLimit = 48; f.caretColor = Color.white;
            return f;
        }
        void BuildCoopScreens()
        {
            {
                var s = MakeScreen("coopMenu");
                var big = Line(s, "CO-OP", 120, Color.white, false, 10, 6, 125);
                var tag = Line(s, "1-4 PLAYERS · ONLINE WITH A CODE, OR ON YOUR LAN", 16, C("#f66"), true, 8, 0, 20);
                var b1 = Btn(s, "HOST ONLINE", "hostRelay", 0, true);
                codeIn = MakeInput(s, "HOST'S CODE", 380); var b2 = Btn(s, "JOIN ONLINE", "joinRelay", 0);
                var b3 = Btn(s, "HOST LAN", "hostLan", 0, false, true);
                ipIn = MakeInput(s, "HOST IP (BLANK = THIS PC)", 380); var b4 = Btn(s, "JOIN LAN", "joinLan", 0, false, true);
                var b5 = Btn(s, "BACK", "coopBack", 0, false, true);
                coopStatusT = Txt(s, "", 16, C("#ffd24a"), TextAnchor.MiddleCenter, true, 2, 0); var st = Place(coopStatusT.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 1500, 44);
                Stack(s, 0, (big, 125, 6), (tag, 20, 22), (b1, 54, 18), (codeIn.GetComponent<RectTransform>(), 46, 6), (b2, 54, 18),
                    (b3, 42, 8), (ipIn.GetComponent<RectTransform>(), 46, 6), (b4, 42, 14), (st, 44, 10), (b5, 42, 0));
                Scanlines(s);
            }
            {
                var s = MakeScreen("coopLobby");
                var big = Line(s, "LOBBY", 120, Color.white, false, 10, 6, 125);
                lobbyCodeT = Txt(s, "", 54, C("#ffd24a"), TextAnchor.MiddleCenter, true, 10, 3); var code = Place(lobbyCodeT.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 1600, 64);
                lobbyPlayersT = Txt(s, "", 22, Color.white, TextAnchor.MiddleCenter, true, 4, 0); var pl = Place(lobbyPlayersT.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 1600, 30);
                lobbyStatusT = Txt(s, "", 16, C("#f88"), TextAnchor.MiddleCenter, true, 2, 0); var st = Place(lobbyStatusT.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0, 1600, 24);
                var b1 = Btn(s, "START", "coopStart", 0, true); lobbyStartGO = b1.gameObject;
                // every boss, straight from the lobby (the host's pick)
            var bc = Btn(s, "BOSS: CARNIFEX", "coopBoss1", 0, false, true); var bt = Btn(s, "BOSS: FROZEN TRAITOR", "coopBoss3", 0, false, true, 2);
            var bh = Btn(s, "BOSS: HOLLOW KING", "coopBoss4", 0, false, true, 3); var bw = Btn(s, "BOSS: WARDEN OF HOURS", "coopBoss5", 0, false, true, 4);
            var bs = Btn(s, "BOSS: SERAPH", "coopBoss2", 0, true, true);
                var b4 = Btn(s, "LEAVE", "coopLeave", 0, false, true);
                var hdr = Line(s, "BOSS PRACTICE", 16, C("#f66"), true, 8, 0, 20);
                foreach (var bb in new[] { bc, bt, bh, bw, bs }) { bb.sizeDelta = new Vector2(440, bb.sizeDelta.y); foreach (RectTransform ch in bb) ch.sizeDelta = new Vector2(440, ch.sizeDelta.y); } // (wide enough for the longest name)
                Stack(s, 0, (big, 110, 6), (code, 56, 6), (pl, 28, 6), (st, 22, 22), (b1, 54, 28), (hdr, 20, 12), (bc, 42, 12), (bt, 42, 12), (bh, 42, 12), (bw, 42, 12), (bs, 42, 28), (b4, 42, 0));
                Scanlines(s);
            }
        }
        bool CoopAct(string a)
        {
            switch (a)
            {
                case "coop": if (net != null && !net.active && !net.connecting) net.status = ""; ShowScreen(MP ? "coopLobby" : "coopMenu"); return true;
                case "coopBack": ShowScreen("mainMenu"); return true;
                case "hostRelay": NetHostRelay(); return true;
                case "joinRelay": NetJoinRelay(codeIn ? codeIn.text : ""); return true;
                case "hostLan": NetHostLan(); return true;
                case "joinLan": NetJoinLan(ipIn ? ipIn.text : ""); return true;
                case "coopLeave": NetLeave(); ShowScreen("coopMenu"); return true;
                case "coopStart": case "coopBoss1": case "coopBoss2": case "coopBoss3": case "coopBoss4": case "coopBoss5":
                    if (MP && Auth) { practiceWave = a == "coopBoss1" ? 5 : a == "coopBoss3" ? 10 : a == "coopBoss4" ? 15 : a == "coopBoss5" ? 20 : a == "coopBoss2" ? TOTAL_WAVES : 0; tutorialRun = false; StartGame(); }
                    return true;
            }
            if (MP && !Auth && (a == "restart" || a == "play" || a.StartsWith("boss") || a == "tutorial")) { Banner("ONLY THE HOST CAN RESTART", "", "#ffd24a"); return true; }
            return false;
        }
        void UpdateLobbyText()
        {
            if (net == null) return;
            string list = "";
            for (int s = 0; s < 4; s++) { var p = PlayerBySlot(s); list += (s > 0 ? "     " : "") + (p != null ? $"<color=#{(s == net.mySlot ? "ffd24a" : "ffffff")}>P{s + 1}</color>" : $"<color=#7a3a3a>P{s + 1}</color>"); }
            if (currentScreen == "coopLobby" && lobbyCodeT)
            {
                lobbyCodeT.text = net.relay ? (string.IsNullOrEmpty(net.code) ? "" : "CODE  " + net.code) : (net.isHost ? net.lanAddr : "LAN");
                lobbyPlayersT.supportRichText = true; lobbyPlayersT.text = list;
                lobbyStatusT.text = net.isHost ? (players.Count > 1 ? "PRESS START WHEN EVERYONE IS IN" : "WAITING FOR PLAYERS · YOU CAN ALSO START ALONE") : net.status;
                if (lobbyStartGO && lobbyStartGO.activeSelf != net.isHost) lobbyStartGO.SetActive(net.isHost);
            }
            if (currentScreen == "coopMenu" && coopStatusT) coopStatusT.text = net.status;
            // HUD: who's in, and how they're doing
            if (!teamT && hudRT) { teamT = Txt(hudRT, "", 16, Color.white, TextAnchor.UpperLeft, true, 1, 2); Place(teamT.rectTransform, 0, 0.5f, 0, 0.5f, 24, 60, 360, 120); }
            if (teamT)
            {
                if (!MP || !started) { teamT.text = ""; return; }
                var sb = new System.Text.StringBuilder();
                foreach (var p in players)
                {
                    if (p == P) continue;
                    string state = p.downed ? "<color=#ff3a3a>DOWN</color>" : p.inPortal ? "<color=#80e8ff>IN PORTAL</color>" : $"<color=#5aff8a>{Mathf.CeilToInt(p.hp)}</color>";
                    sb.Append($"<color=#{SLOT_COL[p.slot & 3]:x6}>{p.name}</color>  {state}\n");
                }
                teamT.text = sb.ToString();
            }
        }
    }
}
