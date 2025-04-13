# Modern Cheat Menu

**Version**: 2.0.0  
**Author**: darkness  
**Target Game**: Schedule I (by TVGS)  
**Mod Loader**: MelonLoader  
**Frameworks**: UniverseLib, Il2CppInterop, Harmony

---

## 📌 Overview

**Modern Cheat Menu** is a feature-rich developer/debugging tool designed for the game *Schedule I*. Whether you're modding, testing, or just having fun, this mod brings you an intuitive in-game UI for accessing and manipulating internal game data — including items, players, quests, and more.

This mod is **open-source**, built to be both powerful and extendable.

---

## ✨ Features

- 🎛️ Toggleable UI Menu (Default key: `F10`)
- 🧰 Custom GUI Text Field (keyboard focus, cursor, and input handling)
- 👥 Player List with Interactive Exploits
- 💥 Explosions at crosshair (Keybind: `Left Alt`)
- 📦 Item management (spawn, unlock, quality settings)
- 🚗 Vehicle spawning and teleportation
- 🧠 Quest manipulation and emotion setting
- 🔐 Relationship and employee system hooks
- 📜 JSON and Il2Cpp-based internal data handling
- 🧩 UniverseLib support for runtime UI

> Designed for modders and developers looking to dive deep into the mechanics of Schedule I.

---

## 🖥️ Installation

### Prerequisites:
- [MelonLoader](https://melonwiki.xyz/)
- Game: *Schedule I* by TVGS
- Proper game dump (Il2Cpp)

### Steps:
1. Download the latest [release](https://github.com/YOUR_USERNAME/YOUR_REPO/releases).
2. Place the compiled `ModernCheatMenu.dll` into your game’s `Mods` folder (`<GameDirectory>/Mods/`).
3. Launch the game and press `F10` to open the cheat menu.

---

## 🎮 Usage

- **Toggling Menu**: `F10`
- **Trigger Explosion at Crosshair**: `Left Alt`

The menu displays a categorized set of commands. Use input fields to provide arguments for commands such as teleporting, spawning items, or modifying player state.

Player-based commands (like targeted exploits) are visible in the player list and UI is rendered via Unity’s GUI system.

---

## 🧱 Code Structure

- `Core`: Main mod logic, including UI, input handling, and command registration.
- `CustomTextField`: Replaces Unity’s standard input field with custom cursor and input logic.
- `CommandCategory` & `Command`: Modular command definitions.
- `NetworkPlayerCategory`: For managing networked player commands and UI.

---

## 📚 Future Roadmap

Planned command implementations:
- `packageproduct`
- `spawnvehicle`
- `teleport`
- `setowned`
- `setqueststate`
- `setemotion`
- `setrelationship`
- `addemployee`
- `setdiscovered`
- `setunlocked`

---