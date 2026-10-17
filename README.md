# Tower Defense Simulator Strategy Overlay

> [!NOTE]
> The entire project codebase was fully written with **Google Gemini**.

> [!IMPORTANT]
> **Roblox TDS Strategy Overlay** is a completely external, non-invasive overlay.  
> The application does NOT inject into the `RobloxPlayerBeta.exe` process memory, does not read game pointers, does not modify game files, and does not trigger the Hyperion (Byfron) anti-cheat. The overlay renders as a transparent Windows window on top of the game client and reads the wave number via the native Windows OCR API.

[Читать на русском языке](README_RU.md)

---

An ultra-lightweight, fast, and responsive **Always-On-Top Overlay** for **Roblox Tower Defense Simulator**, built with C#, .NET 10, Dear ImGui (DirectX 11), and native Windows OCR. 

Helps beat difficult maps (Fallen, Hardcore, Special Events) by automatically tracking the current wave, reminding of tower abilities (Commander/DJ/Medic), and displaying step-by-step instructions and placement maps for solo and multiplayer.

---

## Table of Contents
- [Features](#features)
- [System Requirements](#system-requirements)
- [Quick Start](#quick-start)
- [User Guide](#user-guide)
  - [OCR Setup and Calibration](#1-ocr-setup-and-calibration)
  - [Multiplayer Strategies and Roles](#2-multiplayer-strategies-and-roles)
  - [Placement Maps Viewer](#3-placement-maps-viewer)
  - [Interactive Tasks (Checkboxes)](#4-interactive-tasks-checkboxes)
  - [Built-in Stopwatch](#5-built-in-stopwatch)
  - [Stream & Screen Share Protection (OBS / Discord)](#6-stream--screen-share-protection-obs--discord)
- [Creating and Sharing Strategies](#creating-and-sharing-strategies)
- [Frequently Asked Questions (FAQ)](#frequently-asked-questions-faq)
- [Building from Source](#building-from-source)
- [Credits](#credits)

---

## Features

- **Ultra-lightweight and portable**: A single self-contained executable (~14 MB) that requires no third-party runtimes.
- **Automatic Wave Detection (Windows 10/11 OCR)**: Automatically scans the on-screen wave counter and highlights the matching strategy step with zero lag and no need to manually click waves.
- **Multiplayer Support (Trio / Quad / Duo)**: Seamlessly switch between players (`Player 1`, `Player 2`, etc.) with personalized steps, loadout notes, and placement images for each role.
- **Interactive Task Checkboxes**: Mark completed actions directly in the overlay with clickable checkboxes and automatic progress saving across sessions.
- **Placement Maps Viewer**: View placement maps embedded in the step card or in a separate detached window with smooth zooming and panning.
- **Built-in Stopwatch**: Track total match duration, timing for specific waves, and record split laps.
- **Ability Audio Alerts**: Timers and reminders for Commander (Call to Arms), Medic cleanse, DJ abilities, and wave transition audio cues.
- **Stream & Screen Share Protection**: Native `SetWindowDisplayAffinity` support — the overlay is invisible to OBS Studio, Discord Screen Share, and other screen capture software.
- **Easy Import & Export**: Full `.zip` archive support, direct Drag & Drop into the app window, and Base64 clipboard sharing.
- **Bilingual Interface**: Instantly switch between English and Russian directly in the settings.

---

## System Requirements

| Component | Requirement |
| :--- | :--- |
| **Operating System** | Windows 10 (version 1809+) or Windows 11 (64-bit) *(required for Windows Media OCR)* |
| **Roblox Client** | Official Desktop Windows Client |
| **Roblox Window Mode** | **Windowed** or **Borderless Windowed** *(F11)* |
| **Additional Software** | None (all libraries and .NET 10 runtime are embedded) |

---

## Quick Start

1. Go to the **[Releases](https://github.com/icymarshm47/TdsOverlayImGui/releases)** page.
2. Download the latest `TdsOverlayImGui.zip` (or `.7z`).
3. Extract the archive into any folder (the application is fully portable).
4. Launch `TdsOverlayImGui.exe`.

---

## User Guide

### 1. OCR Setup and Calibration
To allow the overlay to automatically detect wave transitions:
1. Open the settings menu in the title bar.
2. Make sure the wave change sound or auto-OCR is enabled, and calibrate the detection area.
3. Click the **"Select screen area"** button: a colored frame will appear on screen.
4. Move and resize the frame tightly around the wave counter in the Roblox interface (e.g. `Wave 15/40` or simply `15`).
5. Close calibration mode — the overlay will now detect the wave number automatically.

### 2. Multiplayer Strategies and Roles
- For multi-player strategies, player selection buttons (`Player 1`, `Player 2`, etc.) appear in the strategy card.
- Switching players automatically updates:
  - Recommended loadout.
  - General notes and assigned role instructions.
  - Step-by-step actions for each wave.
  - Placement map images for the selected role.

### 3. Placement Maps Viewer
- In Settings, choose whether to display placement images **inside the step card** or in a **separate floating window** (ideal for a second monitor).
- **Image Controls**:
  - `Mouse Wheel` — zoom in / zoom out.
  - `Hold LMB` — pan (drag) the magnified map.
  - `Click MMB (Wheel)` — reset zoom and position to default.

### 4. Interactive Tasks (Checkboxes)
- Lines starting with `- [ ]` markdown syntax turn into interactive checkboxes.
- Clicking a checkbox marks the task as complete (strikes through the text).
- Progress is automatically saved across app restarts, and the **"Clear checks"** button resets all checkboxes before a new match.

### 5. Built-in Stopwatch
- Located directly below the strategy selection card.
- Tracks total match time.
- Supports Start, Pause, Resume, Reset, and **Lap** splits to compare wave completion timings.

### 6. Stream & Screen Share Protection (OBS / Discord)
- In Settings, enable **"Hide overlay from OBS and screen recording"**.
- The overlay remains visible to you on your monitor, but stays completely invisible in game capture, Discord streams, and OBS recordings.

---

## Creating and Sharing Strategies

- **Built-in Editor**: Create custom strategies, configure waves, steps, tower loadouts, and add placement screenshots from files or directly from clipboard.
- **Export to ZIP**: Exports the entire strategy into a single `.zip` archive containing the `.json` structure and all attached screenshots.
- **Import / Drag & Drop**: To import a strategy, simply drag and drop the `.zip` or `.json` file directly into the overlay window, or use the **"Import file..."** button.
- **Clipboard Exchange**: Strategies can also be shared as plain text using the **"Export to clipboard"** and **"Import from clipboard"** buttons.
- All data is saved locally in the application folder.

---

## Frequently Asked Questions (FAQ)

<details>
<summary><b>1. Is this overlay safe to use? Can I get banned?</b></summary>
The overlay is completely safe. It works on the exact same principle as Discord Overlay, MSI Afterburner, or Windows Game Bar: it renders a transparent DirectX 11 window on top of the screen and captures a small selected screen area for OCR. There is zero memory injection, DLL hooking, or network packet manipulation.
</details>

<details>
<summary><b>2. The overlay disappears when clicking inside Roblox / does not stay on top</b></summary>
Ensure that:
1. The <b>"Always on top (over game)"</b> setting is enabled in the overlay settings.
2. Roblox is running in Windowed or Borderless Windowed mode (if in exclusive fullscreen, press <code>F11</code>).
</details>

<details>
<summary><b>3. OCR fails to detect wave numbers or detects them incorrectly</b></summary>
1. Check calibration: ensure the OCR box covers only the wave digits without overlapping nearby health bars or icons.
2. In settings, enable <b>"Enhance text contrast for OCR"</b>.
3. Verify Windows display scaling (100% or 125% is recommended).
</details>

<details>
<summary><b>4. Where are my settings and strategies stored?</b></summary>
The application is completely portable. All files are stored directly in the overlay directory:
- <code>settings.json</code> — app settings and OCR coordinates;
- <code>completed_tasks.json</code> — saved state of completed checkboxes;
- <code>strategies/</code> — strategy files (.json);
- <code>images/</code> — attached placement screenshots.
</details>

---

## Building from Source

If you want to build the overlay yourself:

### Requirements
- [.NET 10.0 SDK](https://dotnet.microsoft.com/)
- Windows 10/11 x64 operating system

### Build Commands
```bash
# Clone the repository
git clone https://github.com/icymarshm47/TdsOverlayImGui.git
cd TdsOverlayImGui

# Build the optimized single-file executable
dotnet publish -c Release -r win-x64 --self-contained -o ./publish
```

---

## Credits

- The entire project codebase was fully written with **Google Gemini**.
- The project uses open-source libraries and technologies: [.NET](https://dotnet.microsoft.com/), [Dear ImGui](https://github.com/ocornut/imgui), [ClickableTransparentOverlay](https://github.com/zaap1234/ClickableTransparentOverlay), [Vortice.Windows](https://github.com/amerkoleci/Vortice.Windows), and native **Windows Media OCR**.
