# 0.1.0 validation report

Date: 2026-08-16

Host: Windows 11 Pro 25H2, build 26200.9168, x64
Display evidence: primary display captured at 2560x1440 physical pixels

## Automated checks

| Check | Result |
|---|---|
| `dotnet format StrataShell.sln --verify-no-changes` | PASS |
| Release solution build | PASS, 0 warnings / 0 errors |
| Core unit suite | PASS, 18/18 |
| Live Windows integration suite | PASS, 3/3 |
| HKCU sign-in startup round trip | PASS; prior value backed up/restored |
| Start-menu catalog uniqueness/existence | PASS |
| Native low-level keyboard-hook install/dispose | PASS |
| Transitive NuGet vulnerability audit | PASS; no known vulnerable packages |
| Extracted release-package smoke test | PASS; main and watchdog launched from ZIP |

The core suite covers bounds normalization, schema versioning, quick-launch
deduplication, atomic JSON round trips, taskbar grid calculations, injected
event rejection, bare-Win toggles, and Win-shortcut pass-through policy.

## Runtime and visual checks

| Scenario | Result | Public evidence |
|---|---|---|
| Full physical-monitor panel | PASS via deterministic `--panel-primary` runtime path | [`panel-primary.png`](../images/panel-primary.png) |
| Search `Visual` across 233 catalog entries | PASS; 4 matching real shortcuts and icons | [`panel-search.png`](../images/panel-search.png) |
| Two-row task windows under 7-window load | PASS | [`taskbar-two-row.png`](../images/taskbar-two-row.png) |
| Forced main-process termination | PASS; watchdog exited and restored both Explorer taskbars | [`before`](../images/watchdog-before-crash.png), [`after`](../images/watchdog-after-crash.png) |
| Settings surface | PASS for implemented controls | [`settings.png`](../images/settings.png) |

The physical screenshot tool opts into Per-Monitor-V2 DPI awareness and uses
native desktop capture, avoiding the earlier 2048x1152 logical-coordinate crop.

## Performance sample

With the custom taskbar enabled and the panel closed:

- Main process after 30 seconds: 150.3 MiB working set, 85.5 MiB private bytes.
- CPU over seconds 10-30: 0.156 CPU seconds, or 0.78% of one logical core.
- Independent watchdog: 20.1 MiB working set, 5.4 MiB private bytes.
- Full panel after catalog/icon load: approximately 220-256 MiB working set on
  this host, depending on icon cache state.

This is a baseline, not an endurance claim. Long-run allocation/handle growth
and cold/open latency distributions remain open acceptance work.

## Explicitly unproven in this report

- A captured physical hardware Windows-key press. The pure policy and actual
  hook installation pass, but synthetic input is intentionally ignored.
- Touch, screen reader, high contrast, multiple displays/DPIs, display hot-plug,
  sleep/resume, lock/unlock, and virtual desktops.
- Attention/progress overlays, multi-window grouping, arbitrary taskbar edges,
  and a full clean-machine install/update/rollback/uninstall matrix.

These gaps are release limitations for 0.1.0 and remain tracked in the
acceptance matrix; they are not represented as passing tests.

## Release artifact

`StrataShell-0.1.0-win-x64.zip` was rebuilt after the extracted-package smoke
test exposed and fixed a missing watchdog assembly. Final SHA-256:

`ceb560981774c5f54d0ab5bcd197fd0176247281626ce1b55a8c435c598017c3`
