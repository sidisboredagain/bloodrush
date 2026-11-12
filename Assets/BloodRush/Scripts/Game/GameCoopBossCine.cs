using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace BR
{
    // CO-OP side of the boss cinematics (GameBossCine.cs). Only the host starts one (at its own moment: the fight's start,
    // the turn to phase 2, the death) and tells everyone; it keeps everyone on the same frame, counts skip votes (every
    // player present must ask; a player who leaves takes their vote with them) and ends it for everyone at once - so no
    // one ever sees a different boss than the others. A client never starts one itself.
    public partial class Game
    {
        readonly HashSet<int> bcVoteSlots = new HashSet<int>(); int bcLastCount; float bcSyncT;
        partial void BCMayStart(ref bool ok) { if (MP && !Auth) ok = false; }
        partial void BCNetStart()
        {
            bcGroup = MP; bcVoteSlots.Clear(); bcLastCount = players.Count; bcSyncT = 0; bcVoters = Mathf.Max(1, players.Count);
            if (MP && Auth) NetEvent(NetEv.BossCine, BC.id);
        }
        partial void BCNetVote()
        {
            if (!MP) return;
            if (Auth) bcVoteSlots.Add(net.mySlot);
            else Send(Msg.Event, w => { w.WriteValueSafe((byte)NetEv.BossCineVote); w.WriteValueSafe(net.mySlot); }, true);
        }
        partial void BCNetTick(float rdt)
        {
            bcGroup = MP; if (!MP || !Auth) return;
            bcVoteSlots.RemoveWhere(s => PlayerBySlot(s) == null);
            bcVotes = bcVoteSlots.Count; bcVoters = Mathf.Max(1, players.Count);
            bcSyncT -= rdt;
            if (bcSyncT <= 0 || players.Count != bcLastCount)
            {
                bcSyncT = 1.5f; bcLastCount = players.Count;
                NetEvent(NetEv.BossCineAt, BC.id * 100000 + Mathf.Clamp(Mathf.RoundToInt(BC.t * 1000), 0, 99999));
                NetEvent(NetEv.BossCineVotes, bcVotes | (bcVoters << 8));
            }
            if (bcVotes >= bcVoters) EndBossCine(); // everyone asked to skip
        }
        partial void BCNetEnd() { if (MP && Auth) NetEvent(NetEv.BossCineEnd, BC.id); }

        // client: what the host says
        void RecvBossCineEvent(NetEv ev, int arg)
        {
            switch (ev)
            {
                case NetEv.BossCine: if (started && !BC.active && !bcDone.Contains(arg)) StartBossCine(arg, 0); break;
                case NetEv.BossCineAt:
                {
                    int id = arg / 100000; float t = (arg % 100000) / 1000f;
                    if (!started) break;
                    if (!BC.active && !bcDone.Contains(id)) StartBossCine(id, t);             // (a newcomer, or a missed start)
                    else if (BC.active && BC.id == id && Mathf.Abs(BC.t - t) > 0.3f) BC.t = t; // keep in step
                    break;
                }
                case NetEv.BossCineEnd: bcDone.Add(arg); if (BC.active && BC.id == arg) EndBossCine(); break;
                case NetEv.BossCineVotes: bcVotes = arg & 255; bcVoters = Mathf.Max(1, arg >> 8); break;
            }
        }
        // host: a client's vote
        void RecvClientEvent(int from, int ev, int arg)
        {
            if ((NetEv)ev == NetEv.BossCineVote && BC.active && from >= 0) bcVoteSlots.Add(from);
        }
    }
}
