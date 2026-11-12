# BLOODRUSH – Handoff

## What it is
BLOODRUSH is a fast first-person shooter made in Unity 6 (6000.6.6f1, URP). There is **no scene content and there are no prefabs**. The code builds everything at runtime: levels, models, UI and sounds, which are synthesised in `Scripts/Audio/AudioMgr.cs`. The game lives in one big `partial class Game` spread across `Assets/BloodRush/Scripts/Game/*.cs`.

There are 25 levels in 5 acts. Six bosses:

| # | Boss | Level | Code |
|---|---|---|---|
| 1 | Carnifex, the Flesh Titan | 5 | `GameBosses.cs` |
| 3 | The Frozen Traitor | 10 | `GameTraitor.cs` |
| 4 | The Hollow King | 15 | `GameHollowKing.cs` |
| 5 | The Warden of Hours | 20 | `GameWarden.cs` |
| 2 | The Seraph (final) | 25 | `GameBosses.cs` + `GameSeraph2.cs` |

## The two projects (keep them in step)
| | Source | Built game |
|---|---|---|
| **Co-op (main, 1-4 players online)** | `D:\Unity\Unity Projects\BloodRushCoop` | `...\BloodRushCoop\Build\BLOODRUSH.exe` |
| Solo | `D:\Unity\Unity Projects\_BloodRushBuild` | built to `_BloodRushBuild\Build`, then copied to `D:\Unity\Unity Projects\BR` |

**Standing rule from the user:** every change goes into co-op first, and solo is kept identical where it can be.

- **Shared files, identical in both projects (copy them across):** `GameBossCine.cs`, `GameBossCineScenes.cs`, `GameIntro.cs`, `GameIntroArch.cs`.
- **Files that differ** (co-op has networking): `GameWarden.cs`, `GameEnemies.cs`, `GameBosses.cs`, `GameNet.cs`, `GameCoop*.cs`. Edit these by hand in both projects; never copy them over each other.
- **More co-op detail:** `COOP_NOTES.md` in this folder.

## Build (batch mode; close the Unity Hub/Editor first)
```
"D:\Unity\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe" -batchmode -nographics -projectPath "<project>" -executeMethod BRBuild.BuildWindows -quit -logFile build.log
```
Delete `<project>\Library\EditorUserBuildSettings.asset` first. A good build prints `BR_BUILD_RESULT Succeeded errors=0` in the log.

To ship solo:
```
robocopy "_BloodRushBuild\Build" "BR" /MIR
```
Exit code 1 means files were copied (success).

## Testing (autopilot, screenshots)
```
BLOODRUSH.exe -brtest <secs> -brlevel <n> -brshot <every s> -brout <dir> -screen-fullscreen 0 -screen-width 960 -screen-height 540 [flags]
```
- **Co-op flags:** `-brhost 2` on the host; `-brjoin 127.0.0.1` on the client.
- **Opening cinematic:** `-brintro`, `-brintroat <t>` to start part-way, `-brsilcolor` to keep silhouette colours when checking poses.
- **Boss cinematics:** `-brbcine` (they're off in tests without it).
- **Boss state:** `-brkill` (boss dies to the next hit), `-brenrage` (jump to phase 2), `-brwp2` (Warden phase 2), `-brwmove sand` (force a Warden attack).
- **Player:** `-brgun 3` (sniper), `-brmortal`.
- **Warning:** run ONE game instance at a time. Parallel instances hung, became unkillable and stalled Unity builds. The only exception is a 2-player co-op test, which needs exactly two.

## Cinematics system (the recent month's main work)
**`GameBossCine.cs` is the engine.**
- Cutscene ids are `boss*10 + kind`, where kind is 0 = intro, 1 = phase 2, 2 = defeat. Hooks: `StartBossIntro`, `SeraphFallBegin`, the Warden's phase-2 line, and the end of `KillBoss`.
- Shots are added with `BShotAt(dur, set, run, upd, fin, fout, start, fov, xin, anim)`.
  - `set >= 0` is a built memory set (on `CINE_LAYER`); `set -1` is the real arena.
  - `run` = the game keeps running underneath; otherwise the game is paused.
- Other helpers: `BPath` (spline camera), `BSay` (subtitle plus synth voice), `BCue` (sound), `BTitle`, `BEnv` (set lighting).
- Arena cameras are clamped inside `LB` and collide with `LayerLevel`. `bcCamFree` turns the collision off.
- Co-op: the host starts cutscenes and broadcasts them (`GameCoopBossCine.cs`). Skipping is by vote.

**`GameBossCineScenes.cs` holds all 12 cutscenes plus their memory-set builders** (furnace, frost kingdom, city, Heaven, clockwork).

**The story** connects every boss to **the Void** (a black rift / black fire):
- **Carnifex:** the furnace's gaoler, consumed by black fire.
- **Traitor:** a king who touched the Void to save his people.
- **Hollow King:** a king who rebuilt himself out of his city.
- **Seraph:** judged a Void rift and was corrupted by it. Her three cutscenes form one arc; her phase 2 follows `SC.t` from `GameSeraph2.cs`.
- **Warden:** loops time.

**`GameIntro.cs` is the opening cinematic**, 5 boss silhouettes long. `ISIL` sets their order, and `ISilPose` poses the real rigs and blacks them out.

## Recent fixes worth knowing
- **Heal:** heal comes only from hits within range (`InHealRange`; Seraph 28 m). It falls off with recent healing, and single hits of 60 or more keep at least half (the sniper).
- **Warden sand columns:** now use a `poured` flag. They used to re-pour forever.
- **Seraph HP:** 430.
- **Seraph phase 2:** the cutscene clock is slaved to `SC.t`, so the line always shows.

## Not fully tested
- The Hollow King defeat after its last dust and lighting fix.
- The camera-collision change, across every boss.
- Seraph balance at 430 HP.
- Co-op cutscenes other than the Hollow King's.

## Other copies (separate, not in step)
- **Mobile/Android:** `D:\Unity\BloodRushMobile`.
- **Older:** a Roblox port and the original HTML version.
