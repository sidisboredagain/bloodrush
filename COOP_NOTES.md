# BLOODRUSH CO-OP — notes

This is a separate copy of `D:\Unity\_BloodRushBuild` with 1–4 player online co-op. Solo is unchanged and stays offline.
Build it like the main game: `Unity.exe -batchmode -nographics -projectPath D:\Unity\BloodRushCoop -executeMethod BRBuild.BuildWindows -quit -logFile <log>`.
That writes `Build\BLOODRUSH.exe`. The product name is `BLOODRUSH COOP`, so its settings and high score are kept apart from the solo game.

## Playing

The main menu has a new **CO-OP** button:
- **HOST ONLINE** starts a Unity Relay + Lobby session and shows a 6-letter code. Friends type that code and press **JOIN ONLINE**.
- **HOST LAN** listens on UDP 7777 and shows this PC's address. **JOIN LAN** takes an IP, or `ip:port`. Leave it blank for this PC.
- In the **lobby**, the host presses START, or picks one of the boss fights. A client waits there.

Players can only drop in **between levels**: in the lobby, before a wave starts, after it is cleared, while the portal is open, or during the warp.
During a wave the host turns a joiner away with a message, and the joiner can try again shortly.

### Co-op rules
- **Downed** at 0 HP: you crawl slowly with only the revolver. A standing teammate within 2.6 m for 3 s brings you back at 40 HP.
  Kills near you no longer heal you while you're down. When every player is down, it's game over for everyone.
- **Scaling** by number of players (2 / 3 / 4): enemy counts ×1.6 / 2.1 / 2.6, boss HP ×1.7 / 2.3 / 2.8.
- **Portal**: touching it holds you in the light. The host warps everyone once every player who isn't downed is inside.
  Downed players come along and arrive standing.
- The **host** triggers level changes and the ending cutscene for everyone.
- **Cheats** (BANG, LOL) are host only.
- **Time effects** (deadeye slow-mo, execution slow-down, hit-stop, boss intro, kill slow-mo) only slow your own gun and camera.
  The shared world always runs at real time. Solo is exactly as before.
- In co-op the world keeps running while you're in the pause menu.

## How it works

### The player list (`GameCoop.cs`)
- `P` is still **the local player**. The camera, HUD, viewmodel, hook and weapons are untouched.
- `players` holds everyone, with P first. Remote entries are filled in from the network and drawn as the cutscene hero (`BuildHero`),
  with a coloured beacon overhead. The beacon turns red and pulsing when that player is down.
- `MP` means a session is running. `Auth` is true for solo and for the host: that machine makes the random decisions,
  spawns, kills, runs waves and decides the portal.

### Enemy code follows one rule: aim at `Tgt`, hurt `P`
- `Tgt = TargetOf(e, dt)` is the player this enemy hunts: the nearest standing player, and it sticks with that player unless another is clearly closer.
  Use it for movement, facing, aiming and attack decisions: `Tgt.pos`, `Tgt.vel`, `Tgt.onGround`, `EyeOf(Tgt)`, `ChestOf(Tgt)`, `Down(Tgt)`.
- Damage only ever goes to the **local** player, through the existing `DamagePlayer()`, tested against `P` / `ChestPos` / `MeIn(center, r, halfHeight)`.
- Every machine simulates every enemy, so each player is hit by exactly what they see on their own screen.
  The host's copy hurts the host, and a client's copy hurts that client.
  The same goes for boss hazards (rings, spikes), enemy projectiles and level mechanics.
- Random decisions run only `if (Auth)`: attack starts, leaps, vaults, boss attack picks (`PickCarnifexAttack` / `PickSeraph`),
  teleport and burrow destinations, minion spawns and `Unstick`. A client's copy receives them from the host's snapshot.
- Other helpers: `NearestPlayer(pos)`, `PlayersInRadius(c, r)`, `FarFromPlayers(x, z, d)` (spawn spots), `CoopCount(n)` (wave sizes), `CoopBossHp`.

### Networking (`GameNet.cs`)
It uses Unity Netcode for GameObjects only as a message pipe, with no NetworkObjects or prefabs, because the game builds everything in code.
There is one named message, `"br"`, and its first byte is the message type.

| message | direction | how sent | what it carries |
|---|---|---|---|
| PState | both | 30 Hz, unreliable | position, velocity, yaw/pitch, flags (ground, slide, downed, in portal, hook…), weapon, HP, revive progress, shot counter. The host relays everyone. |
| Snap | host → clients | 15 Hz, unreliable, in chunks of 22 | level, phase, wave and level clock, plus for each enemy: id, position, velocity, yaw, HP, attack/stun/shoot/spawn/stagger timers. Bosses add their state, state timer and aim directions. |
| Spawn / Kill | host → clients | reliable | an enemy appears (kind, position, scaled HP) or dies (killer, hit flags, weapon), so the gore matches |
| Hit / Stagger | client → host | reliable | a hit claim (`DamageEnemy` on a client sends it, shows the hit at once, and plays a sure kill straight away) |
| Hold | client → host | 30 Hz | an enemy this client is moving itself: reeling it in with the hook, or executing it |
| Event | host → clients | reliable | Start(level), Wave, Warp(level), Cutscene, GameOver |
| Hello / Welcome / Roster | — | reliable | joining, slot assignment, who's in |

- Teammates are drawn 100 ms in the past, interpolated between their states. Their shots are redrawn from the shot counter,
  as a tracer along their aim plus a sound.
- A client merges host corrections into its enemies smoothly; it snaps them into place only when they are more than 4 m off.
  The client adopts a boss state straight away when its own copy is idling (chase or hover), or after 0.3 s of disagreement.
- Online play uses Multiplayer Services sessions (`CreateSessionAsync(...WithRelayNetwork())` / `JoinSessionByCodeAsync`).
  Each process signs in anonymously with its own profile, so two copies of the game on one PC count as two players.

### Files
- New: `GameCoop.cs` (player list, targeting, downed/revive, scaling, host rules, teammates' bodies), `GameNet.cs`, `GameCoopUI.cs` (menus, lobby, team HUD), `GameCoopTest.cs` (autopilot flags).
- Hooks in existing files, each marked with a `co-op` comment:
  - `Game.cs`: `Player` fields, Update loop (`NetUpdate`, world keeps running, `GameUpdate(dt, wdt)`), downed input, host-only cheats.
  - `GameFx.cs`: `GameUpdate` split (player clock vs world clock), host-only wave clock, `CoopCount`, `FarFromPlayers`, portal waiting, `NetWarp`, `ArriveLevel`, `StartGame` / `QuitToMenu`.
  - `GameEnemies.cs`: `Ent` network fields, `NetOnSpawn` in `FinishEnt`, `NetClaimHit` / `NetKill` / `LocalHit` in `DamageEnemy` / `KillEnemy`, the `Tgt` targeting, projectile target.
  - `GameBosses.cs`: `Tgt` targeting, `Auth` gates, `MeIn` hit tests, `CoopBossHp`.
  - `GamePlayer.cs`: `CoopResetPlayers`, downed instead of `Die()` in co-op, `Heal` while downed, spawn offset per slot.
  - `GameMechanics.cs`: `HazardHitEnemies` host only.
  - `GameExecution.cs`: `NetClaimStagger`.
  - `GameWeapons.cs`: `Hit.wpn`, shot counter.
  - `GameUI.cs`: CO-OP button, `BuildCoopScreens`, `CoopAct`.
  - `GameTest.cs`: `CoopTest` / `CoopTestSteer`.
- Packages: `com.unity.netcode.gameobjects` 2.13.3, `com.unity.transport` 2.7.4, `com.unity.services.multiplayer` 2.4.1.
  NGO 3.x needs Unity 6000.7, so it isn't an option here.

## Porting new content from the main project
The main project (`_BloodRushBuild`) is getting levels 6–20 (heaven moving to 21–25) and about 12 new enemy types.
The simplest way to bring them over is to copy the new or changed files across, then re-apply the hooks listed above.
Search for `co-op` / `Tgt` / `Auth` / `MeIn` in this copy.

**New levels and mechanics:** copy them as they are.
- Level hazards already hurt only `P`, so that stays correct per machine.
- `HazardHitEnemies` is host only automatically.
- Hazards that spawn at random around `P` (meteors, sigils, storm, spear tide, sentinels) run separately on each machine: everyone gets their own.
  That's fine, but you can aim them with `NearestPlayer` instead if you like.
- If heaven moves to 21–25, update `GetWaveSpec` / `TOTAL_WAVES` the same way as in the main project; nothing in the net code depends on them.
  The level number travels as a byte (up to 255).
- Wrap any new wave counts in `CoopCount(...)`.

**A new enemy type:**
1. In its AI, change `P.pos` / `P.vel` / `P.onGround` to `Tgt.…`, change `EyePos` / `ChestPos` used for **aiming** to `EyeOf(Tgt)` / `ChestOf(Tgt)`, and change `!P.dead` to `!Down(Tgt)`.
   `Tgt` is already set for each enemy by `UpdateEnemies`. If the enemy is updated somewhere else, call `Tgt = TargetOf(e, dt)` first.
2. Leave damage tests on the local player: `DamagePlayer(...)` after `MeIn(e.pos, r, h)` or a `ChestPos` distance check.
3. Put `Auth &&` in front of any decision that uses `rand` or that starts an attack, and in front of any teleport or spawn of minions.
4. Give it a kind code in `KindOf()` and a matching branch in `RecvSpawn()` (`GameNet.cs`), so clients can create it.
5. Its state already reaches clients if it uses the synced fields: `attackT`, `strikeT`, `stun`, `shootT`, `spawnT`, `stagT`, `cd`, `leaping`, `charging`.
   Bosses also sync `state`, `stateT`, `vidx`, `dashes`, `chargeDir`, `dashDir`, `beamDir`, `groundY`, `orbit`, `leapVX` / `leapVZ`.
   If it keeps attack state elsewhere, add it to `WriteEnt` / `ReadEnt`. Use half-floats (`WH` / `RH`) and keep each enemy under about 50 bytes.
6. A new boss: gate its attack picker with `Auth`, multiply its HP by `CoopBossHp`, and if it has a beam or similar object that its own state turns off,
   also turn it off in `ReadEnt` when a client is pushed out of that state.

## Unity Gaming Services (Relay + Lobby)
The project is already linked to cloud project `6843674d-fa81-4163-a048-b45d7cfa72e5` (organisation `sidisboredagain`).
An online session between two copies on this PC worked in testing: the host got code `CLWJWM`, the client joined with it, and they played two levels.
If online hosting ever reports `ONLINE HOST FAILED`:
1. Open the project in the Unity Editor (Unity Hub → BloodRushCoop), go to **Edit → Project Settings → Services**, sign in, and link the project to a cloud project.
2. On **cloud.unity.com**, choose the project, go to **Products / Multiplayer**, and enable **Relay** and **Lobby** (press "Get started" on each).
   **Authentication** with anonymous sign-in is on by default.
3. Rebuild. Until then, **HOST LAN / JOIN LAN** works without any account.

## Testing
Use the `-brtest` autopilot from the main game, plus these flags (`GameCoopTest.cs`):
- `-brhost [n]` hosts on LAN and starts once n players are in. `-brjoin [ip]` joins.
  Add `-brrelay` to go online instead: the host writes `relay_code.txt` into its `-brout` folder, and the client gets `-brjoin <that file>`.
- `-brmortal` turns off god mode. `-brdown s` makes this player go down after s seconds, to test reviving.
  `-brwatchmate` holds fire and looks at a teammate, for screenshots of their body. `-brcoopui` takes screenshots of the menu and lobby.
- Each instance logs `<out>\coop.txt` once a second: level, wave, phase, HP, enemy count, boss state and HP, hit claims, and teammates as that machine sees them.

Example, two windows on one PC:
```
BLOODRUSH.exe -brtest 120 -brhost 2 -brout tests\host -screen-fullscreen 0 -screen-width 800 -screen-height 450
BLOODRUSH.exe -brtest 120 -brjoin 127.0.0.1 -brdown 12 -brout tests\client -screen-fullscreen 0 -screen-width 800 -screen-height 450
```

What was verified (logs and screenshots in `Build\tests`):
- Solo autopilot run.
- 2 players on LAN through levels 1–3: wave sync, kills, revive, portal waiting, warps.
- Level 5 Carnifex in co-op: boss state and HP identical on both machines (442 = 260 × 1.7), kill, then on to level 6.
- Level 10 Seraph: same, then the ending cutscene and victory on both.
- Mortal run: real downs, revives in both directions, game over when both were down.
- 3 players on LAN: 2.1× scaling, three-way portal.
- An online Relay session joined by code.

## Known limitations
- Players pass through each other, since teammates have no collision.
- Corrections can be visible at high ping. A client's copy of an enemy may start an attack up to about one round trip late.
  It still hits only that client, so it's fair from their point of view.
- Someone else's execution looks like a chainsaw kill (torn apart) to everyone else, because the cut-apart corpse exists only on the executor's screen.
- There's no host migration: if the host quits, everyone returns to the CO-OP menu with a message.
- Tutorial and boss practice from the main menu are solo. The lobby has its own boss buttons.

## November 2026: rebased onto the full game (all 25 levels)
The co-op copy was rebuilt on top of the current main game (`_BloodRushBuild`): every act, level, enemy, boss, model, trap, the
Seraph's phase-2 cinematic and arena corruption, music. The co-op layer above was re-applied. The old co-op scripts are backed up in
`D:\Claude\bloodrush_backups\coop_src_2026-11-06_before_overhaul`.
- **Re-applying after future main-game changes:** run the scripts that did this (diff co-op vs main, keep only the hunks with co-op
  markers, hand-merge the mixed ones). The approach: copy main's `Assets\BloodRush` over, then restore `GameCoop*.cs`, `GameNet.cs`,
  `Editor\BRBuild.cs` and the hooked lines.
- **Spawns** carry the act kind as a string (`e.kind`) and the Mirror Twin's emergence; bosses are kind `10 + n` (1 Carnifex, 2 Seraph, 3 Traitor, 4 Hollow King, 5 Warden).
- **Boss attacks**: the host picks (`Auth`), calls `NetAct(e, a)` and the attack's setup function (`HollowBegin` / `TraitorBegin` / `WardenBegin`).
  The pick travels in the snapshot with a counter; each client runs the same setup when it arrives (`NetBossBegin`), so warnings, aim
  and sounds match. Phase 2 travels as a flag (`NetPhase2`: Carnifex/Seraph enrage - the Seraph's fall cinematic plays on every machine -,
  the Traitor's fall).
- **World changes** that must match everywhere go through `Msg.Fx` (`NetFx` / `RecvFx`): Hollow King buildings and Warden clock towers.
- The snapshot also carries the **trap clock**, so hammers, mirrors, the fog tide, grimoires... fire together on every screen.
- **Warden**: whoever stands inside his Time Stop is frozen on their own machine; he teleports behind and punches his target; the
  snare circle drops on his target; the rewind pulls every player back along their own past; his health rewind is the host's.
- **Scaling** (2/3/4 players): wave sizes ×1.6/2.1/2.6 (each kind capped at +22), enemy HP ×1.12/1.22/1.3, enemy damage ×1.08/1.15/1.2
  (exact-damage hits like trains and the Warden's 50 stay exact), boss HP ×1.7/2.3/2.8 - rescaled live if someone joins or leaves mid-fight.
- **Teammates' bodies** (GameCoop `UpdateBodies`): every joint eases to its pose; walk/run, rising/falling, landing dip, slide, dash,
  grapple (arm to the hook, the rope), shot recoil + muzzle flash, reload, weapon swap, chainsaw, execution blows, hit flinch, downed, dead.
  They hold a copy of the gun they actually use. PState carries `act` bits, the hook point and the execution target.
- Test flags added: `-brmateshow` (host runs through every action; watch with `-brwatchmate`), `-brkind`, `-brsp2`, `-brwmove`, `-brbosshold`.
- Verified: 2 players on levels 16, 18, 20 (Warden: state and HP identical, time-stop punch), 25 (Seraph fall + corruption on both,
  no duplicate minions); 3 players Hollow King (the same 5 buildings fell on all three); 4 players Hollow King in step; solo on 1, 11, 16, 20.