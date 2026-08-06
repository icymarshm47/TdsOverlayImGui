# Tower Defense Simulator Overlay
> [!IMPORTANT]
> Roblox TDS Strategy Overlay is safe, an external, non-invasive overlay. It does NOT inject into RobloxPlayerBeta.exe memory or modify game files. It simply renders a transparent Windows window on top and uses Windows OCR screen capture.

[Читать на русском языке](README_RU.md)

An ultra-lightweight, **Always-On-Top Overlay** for **Roblox Tower Defense Simulator** built with C#, .NET 10, Dear ImGui (DirectX 11), and Native Windows OCR.

## Features
- **Always-On-Top**: Stays transparently over Roblox without performance impact.
- **Windows 10/11 OCR**: Auto-detects in-game wave numbers from screen capture and highlights current steps automatically.
- **Placement Map Screenshots**: Zoomable & draggable placement image viewer (embedded or separate window).
- **General Info & Loadout Card**: Keep overall strategy notes visible across all waves.
- **Multi-file JSON & ZIP**: Export/Import strategies with all attached screenshots in `.zip` archives.
- **Localization**: Full Russian and English language support.

## How to download
- Go to the **Releases** section.
- Download the latest `TdsOverlayImGui.zip` or `TdsOverlayImgui.7z`.
- Extract the archive and run the application.

## How to Build
Requires **.NET 10 SDK** (Windows 10/11).
```bash
dotnet publish -c Release -f net10.0-windows10.0.19041.0 -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Credits
Code fully written with **Google Gemini**.