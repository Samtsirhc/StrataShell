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
