# Changelog

All notable changes to this project will be documented in this file.

---

## [0.7.2] 2026-10-17
### Added
- **Stopwatch Section**: Added interactive stopwatch with Start, Pause, Resume, Reset, and Lap tracking positioned directly under Strategy Selection in Discord-dark styling.

### Optimized
- **Build Output Binary Size**: Reduced single-file executable size by ~81% (from ~77 MB down to ~14.3 MB) and memory usage by removing heavy Windows Forms runtime dependencies in favor of native Win32 APIs (`comdlg32`, `user32`), enabling assembly trimming (`PublishTrimmed`), disabling ReadyToRun duplication, and introducing source-generated `JsonSerializerContext`.

### Fixed
- **Window Dragging Layout Stability**: Fixed jitter, flickering, and down-expansion of instructions during window movement by applying drag deltas before `ImGui.Begin` and stabilizing layout calculations.

---

## [0.7.1] 2026-10-15
### Added
- **Multi-Player Strategy Support**: Added player selection (`Player 1`, `Player 2`, etc.) when creating and editing strategies, allowing separate steps, notes, and placement images for each player.
- **Player Hierarchy in Strategy Menu**: Strategies with multiple players now display as an intuitive hierarchy in the dropdown (`STRATEGY NAME` -> `- PLAYER 1` -> `- PLAYER 2`) with quick player switch pills for instant switching.

### Fixed
- **Check for Updates Rate Limiting**: Fixed update check failing with "CheckUpdatesError" by querying public GitHub Atom feeds (`tags.atom`, `releases.atom`) first, completely bypassing unauthenticated GitHub REST API 403 rate limits.
- **Window & Text Transparency**: Fixed text and card backgrounds remaining opaque when adjusting window transparency by binding `style.Alpha` and custom vector draw lists directly to `WindowOpacity`.
- **Strategy Dropdown Arrow Glyph**: Replaced non-standard Unicode arrow character with ASCII `->` to prevent missing glyph question mark (`?`) in the strategy selector.

---

## [0.7.0] 2026-10-14
### Added
- **OBS / Screen Capture Protection**: Added an option in Settings to hide the overlay from OBS, Discord, and screen capture/recording tools (using Windows `SetWindowDisplayAffinity` API with `WDA_EXCLUDEFROMCAPTURE`).
- **Dynamic Card Auto-Sizing**: Instruction and General Info cards now dynamically measure wrapped text content and auto-size their height with cache, eliminating inner scrollbars and text cutoff.
- **Current Step Indicator**: Display current step number next to the active wave counter (`Step X/Y`).
- **Custom Title Bar & Header Dragging**: Added custom title bar supporting window dragging for both docked and floating windows without scrolling inner content.
- **Persistent Header & Settings Menu on Image Tab**: Added custom title bar with hamburger settings menu and close button to `SeparateImageWindow`, allowing full menu and settings access even when docked as a tab.
- **Commander & Medic Ability Alerts**: Periodic audio reminders and popups for Chain Commander and Medic abilities across configured wave ranges.
- **Custom Blurple Checkboxes**: Discord-style custom interactive task checkboxes with strikethrough for completed instructions.
- **High-Visibility Resize Handle**: Dedicated bottom-right corner resize grip with smooth window resizing.
- **Portable Base Directory**: All application paths (`strategies/`, `images/`, `settings.json`, `completed_tasks.json`) are now strictly anchored to the executable's directory (`AppDomain.CurrentDomain.BaseDirectory`), making the application fully portable regardless of working directory.
- **Privacy & Network Toggle**: Added an option in Settings to enable/disable automatic update checks on startup to give users full control over outgoing network requests.
- **Random Footer Splash Text**: Footer text is now randomly selected on startup from an external `quotes.txt` file (or falls back to default list).
- **Interactive Footer Quote Cycling**: Clicking on the footer splash quote immediately cycles to another random quote from `quotes.txt` with a pointer hand cursor and hover text highlight.

### Changed
- **Unified UI Theme**: Redesigned color palette with modern dark Discord-gray action buttons and compact block spacing.
- **Dedicated Image Window**: Strategy placement images now always open in a separate movable window (removed obsolete embedded image mode).
- **Tab Bar Behavior**: Tab bar is now hidden by default on all windows and docking nodes for a cleaner overlay look.
- **Cleaned Button Labels**: Removed bracket wrappers `[...]` from Copy/Clear buttons and trailing ellipses `...` from Browse Photo buttons.
- **Removed Toggle Descriptions in Settings**: Removed redundant descriptive subtext under setting switches for a cleaner, compact dialog.
- **Editor Cleanup**: Removed markdown syntax prompt below Steps in Strategy Editor and reserved red accent colors strictly for destructive delete actions.
- **Settings & Translations**: Renamed Russian language option to "Russian" in settings.
- **Typography**: Prioritized JetBrains Mono font loading with native Cyrillic glyph support.

### Removed
- **Compact Mode**: Completely removed obsolete Compact Mode option.

### Fixed
- **Docked Tab Title Bar & Settings Menu**: Fixed custom title bar and hamburger settings menu `[≡]` disappearing/clipping behind tab bars when windows (`TDS Strategy Overlay (ImGui)` and `Placement Image`) are docked together into tabs, anchoring title bar rendering to the content cursor (`GetCursorScreenPos()`).
- **Wave Instructions Text Wrapping**: Fixed horizontal cursor drift and stair-stepping layout so instruction lines and checkboxes wrap smoothly without clipping or disappearing when resizing the window.
- **Tag & Badge Wrapping**: Fixed boundary calculation for instruction lines containing `<ocr>` alert tags and badges to wrap cleanly at window edges.
- **OCR Screen Clamping**: Clamped screen capture bounds against `SystemInformation.VirtualScreen` to prevent `Win32Exception` when coordinates exceed monitor borders.
- **Memory Safety in Docking**: Removed direct byte pointer offset writes into ImGui internal dock structures, switching to official DockBuilder APIs.
- **JSON Null-Safety**: Sanitized all deserialized strategy collections and string fields across file, clipboard, and ZIP import handlers.
- **VRAM Texture Cleanup**: Added individual texture unloading when images are removed or replaced in the Strategy Editor, and on editor cancel.
- **OCR Concurrency & Noise Filtering**: Made OCR pending raw text thread-safe via `Interlocked` and added debounce filtering for transient wave rollbacks.
- **Whitespace Preservation**: Fixed regex tokenization in instruction fragment rendering so that multiple spaces and indentation are never stripped.
- **Title Bar Alignments**: Centered the close button cross (`X`) and aligned navigation buttons.
- **Footer Overlap**: Fixed vertical layout to prevent instruction cards from overlapping the bottom footer when scaling window height.
- **Audio & Alert Reliability**: Switched audio alert beeps to `SystemSounds` for reliable playback.

---

## [0.6] 2026-10-13
### Added
- Added native Windows Drag & Drop support for importing `.zip` and `.json` strategy files directly into the overlay window.
- Added an option in Settings to toggle `<ocr>` sound alerts on or off.
- Added high-contrast image preprocessing and 4x upscaling with padding for Windows OCR to significantly improve wave number recognition.
- Added a search filter inside the strategy selection dropdown.
- Added step reordering buttons (`^` / `v`) and clipboard/file image pickers in the Strategy Editor.
- Added clear `[ON]` / `[OFF]` status indicators for Auto-OCR.

### Changed
- Enabled hardware VSync to lock the overlay framerate to the monitor's refresh rate and reduce CPU/GPU usage.
- Renamed "Wave Scanner (OCR)" to "Auto-OCR" for a cleaner UI.
- Polished UI styling, colors, and window rounding.

### Fixed
- Fixed language switching in the Settings modal not applying immediately.
- Fixed the `(Auto-OCR)` status tag remaining visible next to the current wave when Auto-OCR is disabled.
- Fixed vertical text clipping on the height resize handle button.
- Fixed duplicate header text inside the "Formatting Cheat Sheet" modal.
- Optimized disk I/O by saving settings only after releasing mouse sliders and resize handles instead of every frame.

---

## [0.5] 2026-8-6
### Added
- Added window background transparency slider to settings.
- Added sound notifications for <ocr> tags.
- Added strategy import and export via clipboard.
- Added file picker dialogs for import and export locations.
- Added autosaving for checkbox states.
- Added `.json` locale.

### Refactored
- Refactored `Program.cs` into modular UI components and views for better code maintainability.

---

## [0.4] 2026-8-6
### Added
- Insert images via Clipboard Ctrl + V or native File Picker.
- Added Compact Mode.
- Added DJ track switch popup toasts.
- Added text formatting guide and cheat sheet
- Added custom application icon
- Support for combining checkboxes and OCR tags together.

### Changed
- Auto-dismiss update check notice after 5 seconds.
- Moved the "Current Wave" progress card above the "General Info" section.
- Buttons and input fields now dynamically scale and wrap when resizing the window.
- Removed character limits for step instructions and text fields.

### Fixed
- Fixed button clipping and text overlapping when changing overlay window dimensions.
- Auto-OCR fixes.
- Fixed Settings rendering behind the overlay.

---

## [0.3] 2026-8-5
### Added
- Automatic and manual update checks via Github.
- Native window close button in the ImGui title bar.
- Support for `- [ ]` checkboxes and `<ocr>` tags.
- The list of authors in the "About" window has been updated.

### Changed
- The Windows OCR wave scanner has been optimized.

### Removed
- Removed the strategy search bar from the main window.

---

## [0.2] - 2026-8-4
### Added
- Checkboxes
- Windows OCR for wave scanner.

## [0.1] - 2026-8-4
- Created a first version of app.
