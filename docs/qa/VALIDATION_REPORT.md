# 0.1.1 validation report

Date: 2026-08-16

Host: Windows 11 Pro 25H2, build 26200.9168, x64
Display evidence: primary display captured at 2560x1440 physical pixels

## Automated checks

| Check | Result |
|---|---|
| `dotnet format StrataShell.sln --verify-no-changes` | PASS |
| Release solution build | PASS, 0 warnings / 0 errors |
| Core unit suite | PASS, 24/24 |
| Live Windows integration suite | PASS, 4/4 |
| HKCU sign-in startup round trip | PASS; prior value backed up/restored |
| Post-taskbar settings-store round trip | PASS via `--qa-settings-roundtrip`; exit 0 and watchdog exited |
| Start-menu catalog uniqueness/existence | PASS |
| Native low-level keyboard-hook install/dispose | PASS |
| Transitive NuGet vulnerability audit | PASS; no known vulnerable packages |
| Extracted release-package smoke test | PASS; main and watchdog launched from ZIP |
| Core coverage baseline | 93.64% line / 84.84% branch |
| Windows integration coverage baseline | 14.26% line / 12.00% branch; native smoke focus |
| Public Windows CI | PASS; final run completed without workflow annotations |
| Out-of-process Windows UI Automation | PASS; 6/6 surfaces, focus transitions, and 3/3 overflow invocations |

The core suite covers bounds normalization, schema versioning, quick-launch
deduplication, atomic JSON round trips, taskbar grid calculations, injected
event rejection, bare-Win toggles, Win-shortcut pass-through policy, and
lossless corrupt-settings recovery to a timestamped backup.

## Runtime and visual checks

| Scenario | Result | Public evidence |
|---|---|---|
| Full physical-monitor panel | PASS via deterministic `--panel-primary` runtime path | [`panel-primary.png`](../images/panel-primary.png) |
| Search `Visual` across 233 catalog entries | PASS; 4 matching real shortcuts and icons | [`panel-search.png`](../images/panel-search.png) |
| Two-row task windows under 7-window load | PASS | [`taskbar-two-row.png`](../images/taskbar-two-row.png) |
| Forced main-process termination | PASS; watchdog exited and restored both Explorer taskbars | [`before`](../images/watchdog-before-crash.png), [`after`](../images/watchdog-after-crash.png) |
| Settings surface | PASS for implemented controls | [`settings.png`](../images/settings.png) |
| Secondary-monitor full panel | PASS at physical -2560,-4 / 2560x1440 | [`panel-secondary-monitor.png`](../images/panel-secondary-monitor.png) |
| Simultaneous taskbars on two monitors | PASS; DISPLAY1 and DISPLAY2 both registered | [`sanitized log`](evidence/2026-08-16-multi-monitor.txt) |
| Quick-launch settings management | PASS visual surface; remove/clear paths persist through schema 3 | [`settings-taskbar.png`](../images/settings-taskbar.png) |
| 30-window task load and overflow access | PASS; 16 visible in two rows and all 30 enumerated by the window overflow | [`taskbar-overflow-render.png`](../images/taskbar-overflow-render.png), [`sanitized log`](evidence/2026-08-16-overflow.txt) |
| Accessible names, focus, and overflow invocation | PASS across four settings tabs, panel, and taskbar | [`sanitized log`](evidence/2026-08-16-accessibility.txt) |

The physical screenshot tool opts into Per-Monitor-V2 DPI awareness and uses
native desktop capture, avoiding the earlier 2048x1152 logical-coordinate crop.
Transparent WPF AppBars can intermittently be omitted by BitBlt/PrintWindow even
when HWND inspection proves them visible and topmost. The overflow baseline is
therefore a live `RenderTargetBitmap` of the same on-screen visual tree, paired
with native HWND and item-count evidence; it is not represented as a desktop
compositor screenshot.

## Performance and endurance samples

With custom taskbars enabled on both connected monitors and the panel closed:

- Main process after 30 seconds: 166.0 MiB working set, 105.1 MiB private bytes,
  863 handles, and 31 threads.
- CPU over seconds 10-30: 0.141 CPU seconds, or 0.70% of one logical core.
- Independent watchdog: 20.1 MiB working set, 5.4 MiB private bytes.
- Full panel after catalog/icon load: approximately 220-256 MiB working set on
  this host, depending on icon cache state.

This is a baseline, not an endurance claim. Long-run allocation/handle growth
and cold/open latency distributions remain open acceptance work.

A separate five-minute release-build endurance run sampled the enabled custom
taskbar every 30 seconds on the monitor connected at the time:

- Working set stayed between 144.2 and 149.1 MiB (+4.1 MiB first-to-last).
- Private bytes stayed between 80.2 and 84.4 MiB (+4.0 MiB first-to-last).
- Handles decreased by 9 and threads decreased by 5; no monotonic handle or
  thread leak was observed.
- CPU after the initial 30 seconds averaged 0.36% of one logical core.

Five minutes is useful regression evidence, not proof of multi-day stability.
Cold/open latency distributions and longer sleep/resume endurance remain open.

## Explicitly unproven in this report

- A captured physical hardware Windows-key press. The pure policy and actual
  hook installation pass, but synthetic input is intentionally ignored.
- Touch, human Narrator/NVDA review, high contrast, wider display/DPI topologies, display hot-plug,
  sleep/resume, lock/unlock, and virtual desktops.
- Attention/progress overlays, multi-window grouping, arbitrary taskbar edges,
  and a full clean-machine install/update/rollback/uninstall matrix.

These gaps are release limitations for 0.1.1 and remain tracked in the
acceptance matrix; they are not represented as passing tests.

## Release artifact

`StrataShell-0.1.1-win-x64.zip` was rebuilt after the extracted-package smoke
test exposed and fixed a missing watchdog assembly. Final SHA-256:

`000429f65a9bf7f7a38d5492d2c6146a1860ec40eee4b50a6640ddb11fcc8dbb`
