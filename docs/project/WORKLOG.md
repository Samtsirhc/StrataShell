# Work log

## 2026-08-16

- Established the persistent product goal, documentation taxonomy, and
  evidence-first release gates.
- Surveyed official, GitHub, community, and video sources for full-screen Start
  and Windows 11 taskbar work; inspected upstream source and licenses.
- Verified the host as Windows 11 Pro 25H2 build 26200.9168 and verified
  candidate downloads by hashes/signatures. Ran RetroBar hands-on and rejected
  it as the final visual/functional direction.
- Recorded the C#/.NET 8/WPF + ManagedShell architecture decision and built the
  solution into Core, Windows, App, Watchdog, tests, and QA-capture projects.
- Implemented the full-screen panel, catalog/search/icon loading, launch and
  quick-launch pinning, native bare-Win policy, settings, tray, sign-in startup,
  taskbar, and fail-safe recovery.
- Fixed DPI-unaware QA capture, false full-screen detection, unbounded wrapping,
  shortcut-setting loss, schema migration persistence, and watchdog packaging.
- Passed 21 automated tests and a zero-warning Release build; captured panel,
  filtered search, settings, two-row window load, and forced-crash recovery.
- Measured a 30-second enabled-taskbar sample at about 150 MiB working set,
  86 MiB private bytes, and 0.78% of one CPU core over the final 20 seconds;
  the separate watchdog used about 20 MiB working set.
- Prepared the portable release script, CI, README, licensing, contribution,
  security, recovery, and validation documentation for public preview.
- Caught a missing watchdog DLL by running the extracted ZIP, fixed packaging,
  then proved both main/watchdog startup and watchdog exit/recovery from the
  rebuilt artifact. Upgraded test dependencies until the NuGet vulnerability
  scan reported no known vulnerable packages.
- Replaced destructive corrupt-settings fallback with timestamped byte-for-byte
  preservation and added a regression test. Added settings-page review/remove/
  clear management for quick-launch pins.
- Added optional taskbars on every monitor, schema 3 migration, display-change
  rebuild suppression, deterministic secondary-panel QA, and virtual-desktop
  capture. Verified two simultaneous taskbars and a full physical 2560x1440
  secondary panel on the current two-monitor/125% DPI topology.
- Added concurrent atomic-save serialization, inaccessible-folder-safe catalog
  enumeration, non-crashing launch errors, a missing-shortcut Windows test, and
  10,000-cycle Win-key state stress coverage.
- Replaced clipping-only high-load behavior with dedicated quick-launch,
  running-window, and notification overflow menus. A controlled 20-window host
  plus existing windows produced 30 tasks; 16 fit visibly across two rows and
  all 30 were exposed by the overflow source. Added deterministic WPF render
  snapshots and native bottom-window inspection after documenting transparent
  AppBar capture limitations.
- Added an editable SVG brand mark and deterministic multi-resolution ICO
  generator, then applied the icon to both executables, WPF windows, and the
  notification-area status icon.
- Recorded coverage baselines (Core 93.64% line / 84.84% branch; native Windows
  integration 14.26% line / 12.00% branch) and a five-minute taskbar endurance
  sample with about 4 MiB private-byte growth, falling handle/thread counts,
  and 0.36% average CPU use after warm-up.
- A final lifecycle review found that the settings store was accidentally
  disposed while starting the watchdog. Moved disposal to application exit and
  added a deterministic post-taskbar runtime round trip; it exited 0, wrote the
  pass witness, and left no watchdog process behind.
