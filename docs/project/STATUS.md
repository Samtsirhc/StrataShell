# Live project status

Last updated: 2026-08-16 (Asia/Hong_Kong)

## Current phase

`Phase 5 - published 0.1.0 preview and external compatibility validation`

## Completed in the current host run

- Researched and license-reviewed the main commercial and open alternatives.
- Downloaded and hash-verified Start11, Windhawk, RetroBar, and Cairo packages.
- Ran RetroBar after installing its official .NET 10 Desktop dependency; it was
  stable but visually and functionally not a match for the requested product.
- Selected and implemented a .NET 8/WPF companion-panel plus recoverable AppBar
  taskbar architecture, with Explorer injection deliberately excluded.
- Built the full-screen launcher, native Win-key policy, settings, tray,
  sign-in startup, multi-row taskbar, quick-launch management, diagnostics,
  atomic settings, and independent crash watchdog.
- Passed 24 core tests and 4 live-Windows integration tests with a zero-warning
  Release build and clean formatting check.
- Captured physical 2560x1440 panel/search evidence, real two-row task-window
  and 30-window overflow evidence, settings UI, and before/after forced-crash
  recovery evidence.
- Completed dependency-vulnerability and secret scans, recorded coverage and
  five-minute endurance baselines, then rebuilt and revalidated the extracted
  portable package including forced main-process termination and watchdog exit.
- Published the public `Samtsirhc/StrataShell` repository and `v0.1.0` release;
  both Windows CI runs passed, with the final run using current Node 24-based
  GitHub Actions and no workflow annotations.

## Current release boundary

The 0.1.0 package is an honest public preview. The two-monitor, bottom-edge
workflow is implemented and locally validated. The broader final-product matrix
for wider monitor/DPIs, accessibility modes, display and power transitions,
virtual desktops, task attention/progress/groups, clean-machine update/uninstall,
and a physical hardware Windows-key witness remains `IN_PROGRESS`.

## No current implementation blocker

Remaining work is compatibility breadth rather than an architectural impasse:
physical Win-key evidence, clean-machine install/update/removal, accessibility,
power/session transitions, virtual desktops, and wider DPI/display topologies.
