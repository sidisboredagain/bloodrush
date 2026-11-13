# 🔴 BLOODRUSH

<h3 align="center">THE VOID DOES NOT FORGIVE.</h3>

<p align="center">
  <strong>A Fast-Paced First-Person Shooter Built with Unity 6</strong>
</p>

<p align="center">
  25 Levels · 5 Acts · Co-op for up to 4 Players
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6000.6.6f1-black?style=for-the-badge&logo=unity" alt="Unity 6" />
  <img src="https://img.shields.io/badge/Language-C%23-purple?style=for-the-badge&logo=csharp" alt="C Sharp" />
  <img src="https://img.shields.io/badge/Render%20Pipeline-URP-blue?style=for-the-badge" alt="URP" />
  <img src="https://img.shields.io/badge/Multiplayer-1--4%20Players-red?style=for-the-badge" alt="1 to 4 Players" />
</p>

---

## ⚔️ THE GAME

**BLOODRUSH** is a fast-paced first-person shooter built around intense combat, distinctive enemies, challenging boss encounters and a world connected by a mysterious force known as the Void.

Fight through 25 levels spanning five acts. Face increasingly dangerous encounters, master your weapons and movement, and confront powerful beings whose stories are tied to the Void.

## 🔥 KEY FEATURES

- **25-Level Campaign** – Five acts filled with enemies, encounters and boss fights.
- **Fast-Paced FPS Combat** – A combat system built around weapons, movement and aggressive gameplay.
- **Advanced Movement** – Grappling, sliding and other movement mechanics.
- **Executions** – Brutal finishing moves against enemies.
- **Boss Encounters** – Five documented major boss encounters across the campaign.
- **Cinematic Storytelling** – Boss introductions, phase transitions and defeat sequences.
- **Online Co-op** – Play with up to four players.
- **LAN Multiplayer** – Play together over a local network.
- **Dynamic Difficulty Scaling** – Enemy counts, health, damage and boss health scale with player count.

## 👹 BOSSES

The campaign features major encounters tied to the mysterious Void.

| Boss | Level | Story |
|---|---:|---|
| Carnifex, the Flesh Titan | 5 | The furnace's gaoler, eventually consumed by black fire. |
| The Frozen Traitor | 10 | A king who touched the Void to save his people. |
| The Hollow King | 15 | A king who rebuilt himself from his own city. |
| The Warden of Hours | 20 | A being who loops and manipulates time. |
| The Seraph | 25 | A judge of a Void rift who became corrupted by it. |

## 🤝 MULTIPLAYER

BLOODRUSH supports online and local-network co-op for **1–4 players**.

### Online Co-op
- Host an online session using Unity Relay and Lobby.
- Join friends using a six-letter session code.
- Host controls the lobby and campaign progression.

### LAN Co-op
- Host a local multiplayer session over UDP.
- Join using the host's IP address and port.
- No online account is required for LAN play.

### Co-op Gameplay
- Players can revive downed teammates.
- Downed players can crawl and use their revolver.
- Teammates revive players by staying close for three seconds.
- Enemy and boss difficulty scales with the number of players.
- Players must reach the portal before progressing to the next level.
- Boss encounters and cinematics are synchronized across the session.

### Difficulty Scaling

| Players | Enemy Count | Enemy HP | Enemy Damage | Boss HP |
|---|---:|---:|---:|---:|
| 2 | ×1.6 | ×1.12 | ×1.08 | ×1.7 |
| 3 | ×2.1 | ×1.22 | ×1.15 | ×2.3 |
| 4 | ×2.6 | ×1.30 | ×1.20 | ×2.8 |

Enemy count scaling also has per-enemy-type caps. Boss health is rescaled if players join or leave during a fight.

## 🎬 CINEMATICS & STORY

BLOODRUSH features an opening cinematic and dedicated boss cinematics.

- Boss introductions, phase-two transitions and defeat sequences.
- Story connections between the bosses and the Void.
- Camera shots, character poses, dialogue and environmental effects.
- Synchronized boss cinematics in multiplayer, controlled by the host.
- Players can vote to skip multiplayer cinematics.

## 🛠️ BUILT WITH

| Technology | Purpose |
|---|---|
| Unity 6 | Game engine |
| C# | Gameplay programming |
| Universal Render Pipeline (URP) | Rendering |
| Netcode for GameObjects | Multiplayer networking |
| Unity Transport | Network transport |
| Unity Multiplayer Services | Online sessions, Lobby and Relay |
| Git & GitHub | Version control |

## 🧠 TECHNICAL OVERVIEW

One of the distinctive aspects of BLOODRUSH is that its gameplay world is constructed at runtime.

- Levels, models, UI and audio are created through code.
- No traditional scene content or prefab-based gameplay setup.
- Audio is synthesized through the game's audio manager.
- Gameplay code is organized across multiple files using a shared partial `Game` class.

### Multiplayer Architecture

- Host-authoritative control over important gameplay decisions.
- Player state synchronization at 30 Hz.
- Enemy and boss snapshots sent at 15 Hz.
- Reliable messages for important events such as spawning and killing enemies.
- Teammate movement interpolation for smoother remote-player visuals.

## 💻 GETTING STARTED

### Requirements

- Unity Hub
- Unity Editor `6000.6.6f1`
- Git

### Clone the Repository

```bash
git clone https://github.com/sidisboredagain/bloodrush.git
```

Open the cloned project folder through Unity Hub using the matching Unity Editor version.

Unity will restore the required packages listed in `Packages/manifest.json`.

### Building the Game

The project includes a custom Windows build method named `BRBuild.BuildWindows`.

The build can be run in batch mode using:

```powershell
"D:\Unity\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe" -batchmode -nographics -projectPath "PATH_TO_PROJECT" -executeMethod BRBuild.BuildWindows -quit -logFile build.log
```

Replace `PATH_TO_PROJECT` with the location of your cloned project.

## 📁 PROJECT STRUCTURE

```text
BloodRushCoop/
│
├── Assets/
│   └── BloodRush/
│       └── Scripts/
│           └── Game/
│               ├── Game.cs
│               ├── GameBosses.cs
│               ├── GameEnemies.cs
│               ├── GamePlayer.cs
│               ├── GameNet.cs
│               ├── GameCoop.cs
│               └── ...
│
├── Packages/
├── ProjectSettings/
├── COOP_NOTES.md
├── HANDOFF.md
└── .gitignore
```

## 🧪 TESTING

Multiplayer and campaign functionality has been tested across several scenarios, including:

- Solo gameplay and campaign progression.
- Two-player and three-player LAN sessions.
- Four-player co-op testing in progress.
- Enemy and boss health scaling.
- Downed-player and revive mechanics.
- Portal progression and level transitions.
- Boss encounters and synchronized cinematics.
- Online Relay session joining.

## ⚠️ KNOWN LIMITATIONS

- High network latency can cause occasional visual corrections.
- Host migration is not supported. If the host leaves, players return to the co-op menu.
- Some remote execution effects are represented differently for other players.

## 📌 PROJECT INFORMATION

**Project:** BLOODRUSH  
**Engine:** Unity 6  
**Version:** 6000.6.6f1  
**Genre:** First-Person Shooter  
**Campaign:** 25 Levels · 5 Acts  
**Multiplayer:** 1–4 Players  
**Platform:** Windows  

---

<p align="center">
  <strong>THE VOID DOES NOT FORGIVE.</strong>
</p>
<p align="center">
  <em>Enter the Bloodrush.</em>
</p>
