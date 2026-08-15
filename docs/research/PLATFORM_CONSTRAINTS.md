# Windows 11 platform constraints

This document will distinguish supported Windows APIs, stable-but-limited
interop, and undocumented Explorer hooks. No implementation will be selected
until the following questions have source and runtime evidence:

- How can a Windows-key-only gesture be captured while preserving Win+key
  shortcuts, accessibility, elevated apps, and secure desktop behavior?
- Which APIs can enumerate, activate, minimize, group, and observe top-level
  windows across virtual desktops and integrity levels?
- Can notification-area icons be represented or managed without unsupported
  Explorer injection, and what graceful limitation is necessary otherwise?
- How can an AppBar or replacement bar reserve screen work area on every
  monitor and coexist with full-screen apps, auto-hide, DPI, and Explorer?
- Which packaging and auto-start mechanisms work without avoidable elevation
  and recover safely after update or uninstall?

## Findings so far

### Supported foundation

- A custom edge bar can use the documented AppBar protocol (`ABM_NEW`,
  `ABM_QUERYPOS`, `ABM_SETPOS`, `ABM_REMOVE`) and receive position/full-screen
  notifications. This can reserve an arbitrary-height work area without
  patching the stock Win11 taskbar.
- Top-level windows can be enumerated and then tracked through shell/WinEvent
  notifications. ManagedShell already implements state for active, inactive,
  flashing, minimized, hidden/cloaked, icons, monitors, and window operations.
- An application can publish its own tray status using `Shell_NotifyIcon`.
- ManagedShell can represent the notification area both as the shell and while
  coexisting with Explorer, but this must be runtime-tested on build 26200.9168
  because there is no public general-purpose tray enumeration API.

### Native Win11 taskbar limitation

Win11's stock settings do not restore the old unlocked/drag-resizable multi-row
taskbar. Windhawk proves that XAML layout can be hooked to create multiple task
rows and a tray grid, but that approach compiles/injects code into Explorer and
binds to internal element names and symbols. It is useful as a benchmark and an
optional backend, not the safest default.

### Start-key takeover

Open-Shell's current source demonstrates why a reliable bare Windows-key
takeover is not a simple `RegisterHotKey` call: its Win11 route injects into
Explorer, blocks internal shell hotkey registration, and reroutes shell
messages. An original product should first prototype a narrowly scoped
out-of-process low-level keyboard state machine with explicit pass-through for
Win+key combinations and a watchdog disable/recovery mechanism. If that cannot
meet the acceptance matrix on elevated apps and OS updates, a separately
packaged, opt-in Explorer integration may be necessary.

### Recovery implication

The default mode should run alongside Explorer, hide only the stock taskbar
while the custom taskbar is healthy, and immediately restore it on disable,
graceful exit, watchdog failure, or uninstall. Full Explorer replacement can be
offered only as an experimental advanced mode after independent recovery tests.
