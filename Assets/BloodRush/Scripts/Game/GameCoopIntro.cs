using UnityEngine;

namespace BR
{
    // CO-OP side of the opening (GameIntro.cs). The host starts it for everyone (with the time it is at, so a player who
    // joins while it plays picks it up there), keeps everyone in step, and ends it for everyone - when it finishes, or when
    // the host presses SKIP (only the host has that button). Nobody plays until it is over.
    public partial class Game
    {
        int introLastCount; float introSyncT;
        int IntroMs => Mathf.RoundToInt(INTRO.t * 1000);
        partial void IntroMayStart(ref bool ok) { if (MP && !Auth) ok = false; } // (a client waits for the host's word)
        partial void IntroMaySkip(ref bool ok) { if (MP && !Auth) ok = false; }  // (the skip is the host's alone)
        partial void IntroNetStart()
        {
            introLastCount = players.Count; introSyncT = 0;
            if (MP && Auth) NetEvent(NetEv.Intro, IntroMs);
        }
        partial void IntroNetTick(float rdt)
        {
            if (!MP || !Auth) return;
            // someone joined (or left), and every couple of seconds anyway: tell everyone where it is - a newcomer starts it
            // there, everyone else stays in step with the host
            introSyncT -= rdt;
            if (players.Count != introLastCount || introSyncT <= 0) { introLastCount = players.Count; introSyncT = 2; NetEvent(NetEv.Intro, IntroMs); }
        }
        partial void IntroNetEnd() { if (MP && Auth) NetEvent(NetEv.IntroEnd); }

        // client: what the host says about the opening
        void RecvIntroEvent(NetEv ev, int arg)
        {
            switch (ev)
            {
                case NetEv.Intro:
                    if (!started) break;
                    if (!INTRO.active && !INTRO.done) StartIntro(arg / 1000f);
                    else if (INTRO.active && Mathf.Abs(INTRO.t - arg / 1000f) > 0.25f) INTRO.t = arg / 1000f; // keep in step
                    break;
                case NetEv.IntroEnd: if (INTRO.active) EndIntro(); INTRO.done = true; break; // the host skipped (or it ended): into the game together
            }
        }
    }
}
