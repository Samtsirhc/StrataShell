# Release acceptance matrix

Status values are `NOT_STARTED`, `IN_PROGRESS`, `PASS`, `FAIL`, and `BLOCKED`.
`PASS` means the named 0.1.0 scope has reproducible evidence. Broader final
product requirements remain visible as `IN_PROGRESS` instead of being hidden.

## Full-screen panel

| ID | Acceptance criterion | Status | Evidence |
|---|---|---|---|
| PANEL-001 | Bare Win toggles a genuinely monitor-filling panel. | IN_PROGRESS | 2560x1440 physical coverage passes in [`VALIDATION_REPORT`](../qa/VALIDATION_REPORT.md); physical hardware Win witness remains. |
| PANEL-002 | Win shortcuts pass through and injected input cannot trigger the panel. | PASS | State-machine tests plus live native hook install/dispose; 18/18 core and 3/3 integration tests. |
| PANEL-003 | Open, focus, Escape, Enter, mouse launch, and deactivation rules are reliable. | IN_PROGRESS | Implemented and runtime previewed; repeated-input/endurance matrix remains. |
| PANEL-004 | Catalog, search, launch, and quick-launch management work. | PASS | Real 233-entry catalog; [`4-result search capture`](../images/panel-search.png); pin/unpin persists in schema 2 settings. |
| PANEL-005 | Keyboard, mouse, touch, high contrast, screen reader, focus, and reduced motion are verified. | IN_PROGRESS | Keyboard/mouse/reduced-motion implemented; accessibility breadth remains. |
| PANEL-006 | Multiple monitors and 100/125/150/200% DPI have no clipping/drift. | IN_PROGRESS | Per-Monitor-V2 physical primary-display path passes; full matrix remains. |
| PANEL-007 | Visual hierarchy, typography, icons, contrast, motion, states, and supported themes pass review. | IN_PROGRESS | Dark-theme primary/search captures pass local review; dark is the only 0.1.0 theme. |

## Taskbar and icon management

| ID | Acceptance criterion | Status | Evidence |
|---|---|---|---|
| TASKBAR-001 | Height/row/icon controls persist and survive app restart. | PASS | Versioned atomic settings plus normalization/round-trip tests. |
| TASKBAR-002 | Quick launch, task windows, and tray form an intentional two-row layout. | PASS | Seven-window load in [`taskbar-two-row.png`](../images/taskbar-two-row.png). |
| TASKBAR-003 | Active/minimized windows, launch and quick-launch pinning are functional; attention/progress/groups are tracked. | IN_PROGRESS | Active styling, minimize/activate, launch, pin/unpin implemented; richer states remain. |
| TASKBAR-004 | Notification icons remain usable and limitations are documented. | IN_PROGRESS | ManagedShell notification area implemented; overflow/context breadth remains. |
| TASKBAR-005 | Auto-hide, supported placement, monitors, virtual desktops, full-screen apps, Explorer restart pass. | IN_PROGRESS | Bottom/primary, auto-hide and strict foreground full-screen detection implemented; matrix remains. |
| TASKBAR-006 | Crash or exit never strands the user without Explorer taskbars. | PASS | Forced-kill [`before`](../images/watchdog-before-crash.png) / [`after`](../images/watchdog-after-crash.png) evidence. |

## Settings, lifecycle, and packaging

| ID | Acceptance criterion | Status | Evidence |
|---|---|---|---|
| APP-001 | Settings controls every implemented panel/taskbar/lifecycle feature and offers diagnostics/reset/update links. | PASS | [`settings.png`](../images/settings.png). Unsupported theme/recent controls are not exposed. |
| APP-002 | Tray exposes state, panel enable, settings, startup, restart, diagnostics, and exit. | PASS | Runtime tray implementation and state refresh. |
| APP-003 | Per-user sign-in startup can be enabled/disabled without admin. | PASS | Live HKCU round-trip integration test with backup/restore. |
| APP-004 | Settings are versioned, bounded, atomic, migratable, and recover safely. | PASS | Schema 2 core tests and normalized startup persistence. |
| APP-005 | Portable install/removal and package checksums exist; full update/rollback matrix passes. | IN_PROGRESS | [`INSTALL.md`](../release/INSTALL.md) and deterministic publish script; clean-host matrix remains. |

## Quality, performance, and maintainability

| ID | Acceptance criterion | Status | Evidence |
|---|---|---|---|
| QA-001 | Unit, integration, end-to-end, accessibility, fault, and visual suites pass. | IN_PROGRESS | 21 automated tests plus runtime visual/fault checks; accessibility/endurance breadth remains. |
| QA-002 | Resource/latency/growth measurements meet recorded budgets. | IN_PROGRESS | 30-second CPU/memory baseline in [`VALIDATION_REPORT`](../qa/VALIDATION_REPORT.md); endurance remains. |
| QA-003 | Input stress, Explorer/display/power/session changes and abnormal termination recover. | IN_PROGRESS | Abnormal termination passes; remaining transition matrix is open. |
| QA-004 | Formatting, build, dependency/license, secret, and CI checks pass. | IN_PROGRESS | Local checks pass; Windows CI is configured but awaits the authenticated push. |
| QA-005 | Architecture, setup, contribution, troubleshooting, security/recovery, and release procedures are documented. | PASS | README and `docs/`, `CONTRIBUTING.md`, `SECURITY.md`. |
| QA-006 | Final clean-machine run has no unresolved critical/high defects. | IN_PROGRESS | Current-host preview passes; clean-machine gate remains. |

## Publication

| ID | Acceptance criterion | Status | Evidence |
|---|---|---|---|
| RELEASE-001 | Name, license, notices, screenshots, README, roadmap, security and contribution material are ready. | PASS | Repository root and `docs/images/`. |
| RELEASE-002 | Public GitHub source/CI and tagged checksum release exist. | IN_PROGRESS | Local release candidate ready; public verification pending. |
| RELEASE-003 | A fresh user can install, use both surfaces, configure, recover, and remove it. | IN_PROGRESS | Published procedure ready; independent clean-host witness pending. |
