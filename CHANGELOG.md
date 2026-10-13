# Changelog

All notable changes to this project will be documented in this file.

---

## Unreleased

---

<<<<<<< HEAD
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

=======
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
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
<<<<<<< HEAD
- Created a first version of app.
=======
- Created a first version of app.
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
