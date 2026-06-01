# Changelog

All notable changes to SeeDither are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

---

## [1.4.0] - 2026-06-01

### Fixed
- S30 plate scale corrected to 3.99 arcsec/px (was incorrectly returning S50's 2.39).
- Plate scale auto-detection now matches on both "S30" and "S50" camera name substrings (case-insensitive), with S30 as the catch-all default.

---

## [1.3.1] - 2026-05-31

### Changed
- Cleaned up repository for public release: removed AI artifacts, plans, session summaries, and publish binaries.
- Updated `.gitignore` to exclude AI artifact files.
- Updated README.

---

## [1.3.0] - 2026-05-21

### Added
- Full unit test suite (NINA.Plugin.SeeDither.Tests).
- Ko-fi homepage link on plugin options page.

### Fixed
- Thread-safe `Random` usage (was using a shared unsynchronized instance).
- Empty catch blocks now log warnings instead of swallowing exceptions.
- `PropertyChanged` raised inside lock statements (could cause deadlocks with WPF.
- Resource leaks from unsubscribed event handlers.
- Mixed JSON serializers (`JsonSerializer` / `Newtonsoft.Json`) in settings persistence — now uses `JsonSerializer` consistently.
- Keystroke interference between Min/Max offset textboxes (enter-key handling).

---

## [1.2.0] - 2026-05-20

### Fixed
- Exposure detection rewritten to use `IExposureItem` interface instead of fragile string matching on instruction names.
- Settings deserialization race condition on plugin load.

---

## [1.1.0] - 2026-05-19

### Added
- Ko-fi support link in plugin page description (LongDescription metadata).

---

## [1.0.0] - 2026-05-19

### Added
- Initial release — absolute GoTo coordinate-offset dithering for Seestar S30/S50 mounts.
- Sequencer trigger fires after configurable number of exposures.
- Random dither within configurable min/max offset range (arcseconds).
- Plate scale setting and pixel-equivalent readout in options UI.
- Auto-detects plate scale from connected camera.

---

[1.4.0]: https://github.com/cstovi/NINA_SeeDither/releases/tag/v1.4.0
[1.3.1]: https://github.com/cstovi/NINA_SeeDither/releases/tag/v1.3.1
[1.3.0]: https://github.com/cstovi/NINA_SeeDither/releases/tag/v1.3.0
[1.2.0]: https://github.com/cstovi/NINA_SeeDither/releases/tag/v1.2.0
[1.1.0]: https://github.com/cstovi/NINA_SeeDither/releases/tag/v1.1.0
[1.0.0]: https://github.com/cstovi/NINA_SeeDither/releases/tag/v1.0.0
