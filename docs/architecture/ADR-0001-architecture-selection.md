# ADR-0001: Recoverable companion shell on .NET 8/WPF

Status: Accepted for 0.1.x

## Context

Windows 11 does not expose a supported API for all requested Start/taskbar
behavior. The product must balance visual quality, low idle cost, native input
latency, maintainability, and recovery across Windows updates.

## Decision

Use C#/.NET 8 and WPF in separated assemblies:

- `StrataShell.Core`: settings, normalization, and input state policy.
- `StrataShell.Windows`: hooks, startup, app catalog, Explorer visibility, and
  ManagedShell-backed task/tray/AppBar integration.
- `StrataShell.App`: panel, taskbar orchestration, settings, and tray UX.
- `StrataShell.Watchdog`: independent process that restores Explorer taskbars
  after abnormal main-process exit.

The stock Explorer shell remains running and recoverable. StrataShell does not
inject code into `explorer.exe`, patch system files, require elevation, or
replace the configured Windows shell. The custom taskbar is opt-in.

## Why this option

It provides native window/input access and reuses the most relevant
Apache-licensed task/tray abstractions without importing a whole alternative
desktop. It is easier to audit and recover than Explorer injection, while WPF
allows a coherent high-DPI visual layer without an embedded browser runtime.

## Consequences

- Baseline memory is higher than a small C++ Win32 implementation; the measured
  preview budget is recorded rather than hidden.
- ManagedShell and some Windows shell behavior rely on compatibility-sensitive
  interfaces, so current Windows builds require ongoing runtime QA.
- Primary-monitor bottom-edge support ships first. Multi-monitor/edge expansion
  must pass the same crash-recovery gate before release.
