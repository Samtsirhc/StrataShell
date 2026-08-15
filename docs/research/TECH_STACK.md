# Technology-stack research

## Evaluation criteria

- True native window/input/shell interop on Windows 11.
- Fast cold start and panel-open latency, low idle working set/CPU, and no
  embedded browser requirement.
- High-quality text, icons, animation, accessibility, DPI, and light/dark UI.
- Testable domain/core logic and isolatable undocumented integrations.
- Straightforward public build, packaging, crash reporting opt-in, and updates.

## Selected stack

Use C# on .NET 8 with WPF, split into clean
core, Windows interop, shell services, and presentation assemblies. WPF is the
native UI substrate used by the Apache-licensed ManagedShell library and lets
the same process host AppBar windows, task/window models, notification area,
the full-screen panel, settings, and the product's tray icon.

Use a small in-repo WPF design system so the release carries no unused theme
framework. Windows App SDK APIs can be adopted incrementally where their
runtime and deployment cost is justified. Avoid WebView/Electron for the
shell-critical path.

## Alternatives

### WinUI 3

Excellent Fluent defaults and Microsoft's recommendation for new desktop apps,
but it does not directly consume ManagedShell's WPF window/dependency-object
model. A two-process WPF shell host plus WinUI panel/settings would add process,
IPC, packaging, focus, and crash-coordination complexity. It remains a future
presentation option if WPF cannot meet the visual baseline.

### C++ / Win32

Offers the smallest native footprint and maximum hook control, as illustrated
by Windhawk and AnyFSE, but raises implementation time and memory-safety burden
for the large settings/search/layout surface. Reserve C++ for a very small,
optional native integration component only if the C# input prototype fails.

### Rust

Memory-safe and capable of native interop, but the local toolchain is absent and
the mature Windows shell/task/tray reference code selected for evaluation is
.NET/WPF. It currently adds integration cost without solving the hardest UX or
undocumented-shell problems.

## Dependency rule

No candidate library is adopted merely from repository claims. Its license,
transitive dependencies, current-host runtime behavior, release cadence, and
failure/recovery behavior must be captured in an ADR and automated spike first.
