# Existing software candidate matrix

Candidates were evaluated by source/license review, official documentation,
verified packages, and hands-on checks where the risk was acceptable.

| Product | License/cost | Requested fit | Hands-on/result |
|---|---|---|---|
| Start11 v2 | Closed; paid after trial | Closest full-screen behavior and visual benchmark; not an open implementation base | Signed official installer verified. Not installed because shell mutation was unnecessary after documentation established the benchmark. |
| Windhawk 1.7.3 + mods | GPL-3.0; free | Open proof for taskbar height, multi-row buttons, icon size, and tray grids; no full-screen panel | Release/source inspected and package verified. Rejected as the default because it injects into Explorer. |
| RetroBar 1.22.122 | Apache-2.0; free | Custom portable taskbar, but deliberately classic and not a full-screen launcher | Ran successfully after installing its official .NET 10 Desktop runtime. Roughly 197 MiB working set; one-row classic visual was unsuitable. |
| Cairo Desktop 0.4.434 | Apache-2.0; free | Coherent shell/taskbar foundation, no equivalent full-screen Start surface | Package/hash and source verified. Larger desktop-replacement scope than required. |
| ManagedShell | Apache-2.0; free library | Relevant AppBar, task-window, tray and full-screen building blocks | Source-reviewed and adopted behind StrataShell's recoverable taskbar boundary. |
| Open-Shell 4.4.198 | MIT; free | Mature Start takeover, but no requested modern full-screen experience | Source-reviewed; rejected for visual/product mismatch and Explorer hooks. |
| ExplorerPatcher | Mostly GPL-2.0; free | Can restore taskbar behavior, but not the requested coherent panel | Rejected as a base: current taskbar binary source/legal boundary and build-coupling risk. |
| AnyFSE | MIT; free | Full-screen gaming home, not a desktop Win-key panel/taskbar | Source-reviewed; rejected for workflow mismatch. |
| StartAllBack | Closed; paid | Polished classic restoration without confirmed requested full-screen mode | Research-only benchmark. |

## Conclusion

No evaluated product supplies the complete request as one open, modern,
low-risk package. StrataShell therefore uses an original panel and controlled
custom AppBar taskbar. Explorer remains the fallback; injection and system-file
patching are excluded from the default architecture.
