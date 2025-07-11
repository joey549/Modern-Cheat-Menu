# Modern Cheat Menu

**Version**: 2.1.3 | **Author**: darkness | **Refactored by**: joey549 (SarkastikDemon)
**Target Game**: Schedule I (by TVGS)  
**Mod Loader**: MelonLoader  
**Frameworks**: UniverseLib, Il2CppInterop, Harmony

---

## 📌 Overview

**Modern Cheat Menu** is a feature-rich developer/debugging tool designed for the game *Schedule I*. Whether you're modding, testing, or just having fun, this mod brings you an intuitive in-game UI for accessing and manipulating internal game data — including items, players, quests, and more.

This mod is **open-source**, built to be both powerful and extendable.  Anyone wanting to use any of the code within this project is fully and freely allowed to do so.

---

## ✨ Features

- 🎛️ Toggleable UI Menu (Default key: `F10`)
- 🧰 Custom GUI Text Field (keyboard focus, cursor, and input handling)
- 👥 Player List with Interactive Exploits
- 💥 Explosions at crosshair (Keybind: `Left Alt`)
- 📦 Item management (spawn, unlock, quality settings)
- 🚗 Vehicle spawning and teleportation
- 👥 Adding employees ( Now with a max of 50 employees per property! )
- 🧠 Quest manipulation and emotion setting
- 🔐 Relationship and employee system hooks
- 📜 JSON and Il2Cpp-based internal data handling
- 🧩 UniverseLib support for runtime UI
- 🗑️ Settable trash settings including max trash

> Designed for modders and developers looking to dive deep into the mechanics of Schedule I.
> This refactoring is ment to help other developers understand and added to this project.
> This project may differ majorly from the original creator via their [github](https://github.com/tcphdr/Modern-Cheat-Menu). (Big thanks to tcphdr for creating this beast menu in the first place!)

---

## 🐛 Know Bugs
- ☰ When quiting to main menu and then loading in to game again, the menu will loose it's background but menu still functions. ( WIP )
- 
---

---

## 🖥️ Installation

### Prerequisites:
- [MelonLoader](https://melonwiki.xyz/)
- Game: *Schedule I* by TVGS
- Proper game dump (Il2Cpp)

### Steps:
1. Download the latest [release](https://github.com/joey549/Modern-Cheat-Menu/releases).
2. Place the compiled `ModernCheatMenu.dll` into your game’s `Mods` folder (`<GameDirectory>/Mods/`).
3. Launch the game and press `F10` to open the cheat menu.

---

## 🎮 Usage

- **Toggling Menu**: `F10`
- **Trigger Explosion at Crosshair**: `Left Alt` ( Now has a toggle setting in 'Player' tab and is FALSE/OFF by default)

The menu displays a categorized set of commands. Use input fields to provide arguments for commands such as teleporting, spawning items, or modifying player state.

Player-based commands (like targeted exploits) are visible in the player list and UI is rendered via Unity’s GUI system.

---

## 🧱 Code Structure

- `Core`: Main mod logic, including UI, input handling, and command registration.
- `CustomTextField`: Replaces Unity’s standard input field with custom cursor and input logic.
- `CommandCategory` & `Command`: Modular command definitions.
- `NetworkPlayerCategory`: For managing networked player commands and UI.
- WIP: More to be added/refactored/updated.
---

## 📚 Future Roadmap

Planned command implementations:
- `setqueststate`
- `setemotion`

---
