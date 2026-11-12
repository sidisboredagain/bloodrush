# Blood Rush Coop

**A fast-paced, cooperative 2D shooter built with Unity**

---

## Table of Contents
- [Overview](#overview)
- [Features](#features)
- [Installation & Setup](#installation--setup)
- [Controls](#controls)
- [Gameplay](#gameplay)
- [Contributing](#contributing)
- [License](#license)
- [Acknowledgements](#acknowledgements)

---

## Overview
Blood Rush Coop is a top‑down, twin‑stick shooter designed for local and online cooperative play.  Players team up to survive endless waves of enemies while upgrading their weapons and abilities.  The project showcases:
- Unity's new URP pipeline
- ScriptableObject‑driven data architecture
- Multiplayer via Unity Netcode for GameObjects
- A modular UI system based on Unity UI Toolkit

---

## Features
- **Co‑op Multiplayer** – Local split‑screen and online (up to 4 players).
- **Procedural Wave Generation** – Enemy types, spawn patterns and difficulty scale automatically.
- **Upgrade System** – Collect blood to unlock weapons, abilities and cosmetic skins.
- **Extensible Architecture** – All gameplay data lives in ScriptableObjects, making it easy to add new weapons, enemies, or maps without code changes.
- **Cross‑platform** – Tested on Windows, macOS and Linux.
- **Unit & Integration Tests** – Uses Unity Test Framework and PlayMode tests.

---

## Installation & Setup
1. **Clone the repository**
   ```powershell
   git clone <repository-url> "d:\Unity\Unity Projects\BloodRushCoop"
   ```
2. **Open the project in Unity**
   - Minimum Unity version: **2022.3 LTS** (URP 12.x).
   - Open the folder `d:\Unity\Unity Projects\BloodRushCoop` with the Unity Hub.
3. **Resolve Packages**
   - Unity will automatically download required packages via the Package Manager.
   - If prompted, install **Netcode for GameObjects** (com.unity.netcode.gameobjects) and **Input System**.
4. **Play the game**
   - Press the Play button in the Unity Editor.
   - For a built executable, go to **File → Build Settings**, select your target platform, and click **Build**.

---

## Controls
| Action | Keyboard | Gamepad |
|--------|----------|---------|
| Move   | WASD / Arrow Keys | Left Stick |
| Aim / Shoot | Mouse (aim) / Right Click (shoot) | Right Stick |
| Switch Weapon | 1‑4 | D‑Pad Up/Down |
| Interact / Pick‑up | E | X |
| Pause | Esc | Start |

---

## Gameplay
- **Objective**: Survive as long as possible while collecting blood drops.
- **Scoring**: Points are awarded based on enemies killed, wave number, and blood collected.
- **Rounds**: Each round consists of a **Preparation Phase** (choose load‑out) and a **Combat Phase** (waves of enemies).
- **Power‑ups**: Randomly spawned items grant temporary buffs (speed, damage, shield).

---

## Contributing
We welcome contributions! Please follow these steps:
1. Fork the repository.
2. Create a feature branch:
   ```bash
   git checkout -b feature/awesome-feature
   ```
3. Make your changes and ensure all tests pass:
   ```powershell
   .\RunTests.ps1   # provided script runs EditMode & PlayMode tests
   ```
4. Submit a Pull Request with a clear description of the change.

**Code Style**
- C# scripts should follow the *Unity C# Coding Standards*.
- Use `PascalCase` for class names and public members, `camelCase` for private fields.
- Keep line length under 120 characters.

---

## License
This project is licensed under the **MIT License** – see the `LICENSE` file for details.

---

## Acknowledgements
- Unity Technologies – Unity Engine & URP
- Unity Netcode for GameObjects team
- The open‑source community for assets and utilities used in this project.

---

*Happy blood‑rushing!*