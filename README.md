# 🔴 BLOODRUSH
### THE VOID DOES NOT FORGIVE.

<p align="center">

**A Fast-Paced First-Person Shooter Built with Unity 6**

25 Levels · 5 Acts · Co-op for up to 4 Players

</p>

<p align="center">
  ![Unity](https://img.shields.io/badge/Unity-6000.6.6f1-black?style=for-the-badge&logo=unity)
  ![C#](https://img.shields.io/badge/Language-C%23-purple?style=for-the-badge&logo=csharp)
  ![URP](https://img.shields.io/badge/Render%20Pipeline-URP-blue?style=for-the-badge)
  ![Multiplayer](https://img.shields.io/badge/Multiplayer-1--4%20Players-red?style=for-the-badge)
</p>

---

## ⚔️ THE GAME

**BLOODRUSH** is a fast-paced first-person shooter built around intense combat, distinctive enemies, challenging boss encounters and a world connected by a mysterious force known as the Void.

Fight through 25 levels spanning five acts. Face increasingly dangerous encounters, master your weapons and movement, and confront powerful beings whose stories are tied to the Void.

This project includes a dedicated **1–4 player cooperative version**, allowing friends to experience the campaign together through online or LAN multiplayer.

---

## 🔥 GAMEPLAY

BloodRush combines fast-paced first-person combat with a variety of movement, weapon and execution mechanics.

- **First-Person Combat** — Fight enemies using the game's weapon arsenal.
- **Grappling Hook** — Traverse the battlefield using the hook and rope mechanics.
- **Sliding & Movement** — Move through combat with dynamic movement mechanics.
- **Executions** — Perform close-range executions against enemies.
- **Boss Encounters** — Face major bosses with distinct attacks, cinematic introductions and multiple phases.
- **Progressive Campaign** — Fight through 25 levels across five acts.
- **Co-op Gameplay** — Experience the game with up to three other players.

---

## 🌑 THE WORLD & ITS BOSSES

The story of BloodRush connects its major bosses to the **Void**, represented by a black rift and black fire.

Each boss has a different connection to it.

| Boss | Level | Story |
|---|---:|---|
| Carnifex, the Flesh Titan | 5 | The furnace's gaoler, consumed by black fire. |
| The Frozen Traitor | 10 | A king who touched the Void to save his people. |
| The Hollow King | 15 | A king who rebuilt himself out of his city. |
| The Warden of Hours | 20 | A being who loops time. |
| The Seraph | 25 | A judge of a Void rift who was corrupted by it. |

### Boss Encounters

**01 — Carnifex, the Flesh Titan**

The furnace's gaoler, consumed by black fire.

**02 — The Seraph**

The final boss. Her story unfolds across three connected cinematics, with a transformation into a second phase and a corrupted arena.

**03 — The Frozen Traitor**

A king whose attempt to save his people led him to touch the Void.

**04 — The Hollow King**

A king who rebuilt himself from the remains of his own city.

**05 — The Warden of Hours**

A master of looping time, with time manipulation abilities that affect the battlefield.

---

## 🤝 CO-OP MULTIPLAYER

The co-op edition supports **1–4 players**, with dedicated online and LAN multiplayer.

### Play Together

| Mode | Description |
|---|---|
| Host Online | Create a session using Unity Relay and Lobby. |
| Join Online | Join friends using a six-letter session code. |
| Host LAN | Host a local network session. |
| Join LAN | Connect using an IP address and port. |

### Cooperative Features

- **Downed & Revive System** — Players can be revived by a standing teammate.
- **Dynamic Difficulty Scaling** — Enemy counts, enemy stats and boss health scale with player count.
- **Synchronized Boss Encounters** — Boss attacks, phase transitions and important world changes are synchronized.
- **Team Portal System** — Players must gather at the portal before the host moves the team to the next level.
- **Animated Teammates** — Other players display movement, grappling, sliding, weapon handling, executions and combat animations.
- **Shared Campaign Progression** — The host controls level transitions, waves and the ending cutscene.

### Difficulty Scaling

| Players | Enemy Count | Enemy HP | Enemy Damage | Boss HP |
|---:|---:|---:|---:|---:|
| 1 | 1× | 1× | 1× | 1× |
| 2 | 1.6× | 1.12× | 1.08× | 1.7× |
| 3 | 2.1× | 1.22× | 1.15× | 2.3× |
| 4 | 2.6× | 1.3× | 1.2× | 2.8× |

*Scaling values are based on the documented co-op implementation. Some exact-damage attacks are not scaled.*

---

## 🎬 CINEMATICS & PRESENTATION

BloodRush features a dedicated cinematic system for its major boss encounters.

- Boss introduction cinematics.
- Boss phase-transition cinematics.
- Boss defeat cinematics.
- Opening cinematic featuring five boss silhouettes.
- Subtitle and synthesized voice presentation.
- Cinematic camera paths, lighting and environment setups.
- Co-op cinematic synchronization, with skip voting.

The Seraph's story is presented through three connected cinematics, forming a larger narrative arc.

---

## 🛠️ TECHNICAL OVERVIEW

BloodRush is built using **Unity 6 and C#**, with its gameplay systems implemented through code.

### Technology

| Technology | Purpose |
|---|---|
| Unity 6 (6000.6.6f1) | Game engine |
| Universal Render Pipeline | Rendering |
| C# | Gameplay and game systems |
| Unity Netcode for GameObjects | Network messaging |
| Unity Transport | Network transport |
| Unity Multiplayer Services | Online sessions, Relay and Lobby |
| Git & GitHub | Version control |

### Architecture

One unusual aspect of BloodRush is that it does not rely on traditional Unity scene content or prefabs.

The game builds its levels, models, UI and synthesized audio at runtime.

Its gameplay code is organized around a large partial `Game` class, divided across multiple C# files.

The co-op implementation uses Unity Netcode for GameObjects as a messaging layer, rather than relying on NetworkObjects and network prefabs.

---

## 📡 HOW MULTIPLAYER WORKS

The co-op implementation uses a host-authoritative architecture.

- The host controls random enemy decisions, spawns, waves and level transitions.
- Player state is transmitted at 30 Hz.
- Enemy and boss snapshots are transmitted at 15 Hz.
- Teammate positions are interpolated for smoother movement.
- Enemy corrections are blended smoothly on clients.
- Important events such as spawning, kills, warps and cutscenes are synchronized.

Online sessions use Unity Multiplayer Services with Relay and Lobby. LAN sessions are also supported.

---

## 🚀 GETTING STARTED

### Requirements

- Unity Hub
- Unity Editor **6000.6.6f1**
- Git
- Windows for the documented Windows build workflow

### Clone the Repository

```bash
git clone https://github.com/sidisboredagain/bloodrush.git
```

### Open the Project

1. Open Unity Hub.
2. Select **Add Project from Disk**.
3. Select the cloned project directory.
4. Open it using Unity 6000.6.6f1.
5. Allow Unity to import the required packages.

### Online Multiplayer Setup

The project uses Unity Gaming Services.

For online hosting:

1. Open **Edit → Project Settings → Services**.
2. Sign in to Unity.
3. Link the project to its Unity Cloud project if necessary.
4. Enable **Relay** and **Lobby** under Multiplayer Services.
5. Build and launch the game.

**Note:** LAN hosting and joining work without an online Unity account.

### Building the Windows Version

The project includes a batch build method:

```bash
Unity.exe -batchmode -nographics -projectPath "PATH_TO_PROJECT" -executeMethod BRBuild.BuildWindows -quit -logFile build.log
```

A successful build reports:

```text
BR_BUILD_RESULT Succeeded errors=0
```

The Windows executable is generated at:

```text
Build/BLOODRUSH.exe
```

---

## ⚠️ KNOWN MULTIPLAYER LIMITATIONS

- Teammates currently do not collide with one another.
- High network latency can make some enemy corrections visible.
- There is no host migration. If the host leaves, everyone returns to the co-op menu.
- Tutorial and boss practice from the main menu remain solo modes.
- Another player's execution can have a different visual presentation on other clients.

---

## 📁 PROJECT STRUCTURE

```text
BloodRushCoop/
│
├── Assets/
│   └── BloodRush/
│       └── Scripts/
│           └── Game/
│               ├── Game.cs
│               ├── GameEnemies.cs
│               ├── GameBosses.cs
│               ├── GameCoop.cs
│               ├── GameNet.cs
│               └── ...
│
├── Packages/
├── ProjectSettings/
│
├── COOP_NOTES.md
├── HANDOFF.md
└── README.md
```

---

## 🧪 TESTING

The project includes automated testing and multiplayer testing tools.

Documented testing includes:

- Solo campaign testing.
- Two-player LAN gameplay.
- Three-player and four-player co-op testing.
- Enemy and boss synchronization.
- Downed and revive mechanics.
- Portal waiting and level transitions.
- Boss phase transitions and ending cinematics.
- Online Relay session testing.

---

## 👨‍💻 PROJECT INFORMATION

| | |
|---|---|
| Project | BLOODRUSH CO-OP |
| Engine | Unity 6 |
| Editor Version | 6000.6.6f1 |
| Language | C# |
| Players | 1–4 |
| Campaign | 25 Levels |
| Acts | 5 |
| Repository | [sidisboredagain/bloodrush](https://github.com/sidisboredagain/bloodrush) |

---

<p align="center">

### THE VOID IS NOT THE END.

**BLOODRUSH**

*Fight. Survive. Face the Void.*

</p>
