# Research source ledger

Every consequential claim will record a direct URL, access date, source type,
and whether it was independently reproduced. Primary Microsoft documentation
and upstream repositories take precedence over summaries.

| ID | Topic | Source | Accessed | Notes |
|---|---|---|---|---|
| MS-APPBAR | AppBar lifecycle, sizing, work-area, auto-hide, and full-screen notifications | https://learn.microsoft.com/en-us/windows/win32/shell/application-desktop-toolbars | 2026-08-16 | Primary Microsoft documentation. |
| MS-NOTIFY | `Shell_NotifyIcon` contract for application status icons | https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyiconw | 2026-08-16 | Public API sends icons to the shell; it does not provide a public enumeration API. |
| MS-WINEVENT | `SetWinEventHook` event-monitoring contract | https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwineventhook | 2026-08-16 | Primary input for out-of-process window tracking. |
| MS-HOOK | `SetWindowsHookEx` hook contract and architecture boundaries | https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowshookexw | 2026-08-16 | Primary Microsoft documentation; global hooks require careful message-loop and bitness handling. |
| MS-DESIGN | Windows 11 design principles and signature experiences | https://learn.microsoft.com/en-us/windows/apps/design/design-principles | 2026-08-16 | Effortless, calm, personal, familiar, coherent; color, layers, icons, materials, geometry, type, motion. |
| MS-MOTION | Windows motion guidance | https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/motion | 2026-08-16 | Direct entrance/exits and 83/167/250/333 ms timing guidance. |
| MS-SDK | Windows App SDK and framework guidance | https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/system-requirements | 2026-08-16 | WinUI is recommended for new apps; Windows App SDK can also augment WPF. |
| MS-MIGRATE | WPF/Win32 modernization decision guide | https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/migration-decision-guide | 2026-08-16 | Microsoft explicitly keeps WPF supported and recommends incremental modernization for many existing/native-interoperability apps. |
| APPLE-LAYOUT | Apple HIG layout and visual hierarchy | https://developer.apple.com/design/human-interface-guidelines/layout | 2026-08-16 | Alignment, hierarchy, progressive disclosure, adequate spacing, and graceful adaptation. |
| APPLE-MOTION | Apple HIG motion | https://developer.apple.com/design/human-interface-guidelines/motion | 2026-08-16 | Purposeful, brief, interruptible, optional motion tied to spatial expectations. |
| APPLE-MATERIAL | Apple HIG materials | https://developer.apple.com/design/human-interface-guidelines/materials | 2026-08-16 | Materials establish depth and hierarchy; accessibility settings must alter transparency/contrast. |
| START11-PRODUCT | Start11 official product and trial page | https://www.stardock.com/products/start11/ | 2026-08-16 | Explicitly offers a full-screen Start menu on Windows 11; closed paid product with trial. |
| START11-DOC | Start11 v2 Windows 11 style documentation | https://support.stardock.com/space/SHC/1995309123/Start11%2Bv2%2BUI%3A%2BWindows%2B11%2BStyle | 2026-08-16 | Documents full-screen mode, icon sizes, appearance, recent items, search, and app layout controls. |
| WINDHAWK-MULTIROW | Windhawk multi-row taskbar mod | https://windhawk.net/mods/taskbar-multirow | 2026-08-16 | Current mod 1.1.2; task-list rows only, requires separate height and tray-grid mods. |
| WINDHAWK-TRAYGRID | Windhawk taskbar tray spacing and grid mod | https://windhawk.net/mods/taskbar-notification-icon-spacing | 2026-08-16 | Current mod 1.3.1; supports two-row tray grid on Win11 22H2+. |
| WINDHAWK-SOURCE | Windhawk community mod source | https://github.com/ramensoftware/windhawk-mods | 2026-08-16 | Inspected `taskbar-multirow`, `taskbar-icon-size`, and `taskbar-notification-icon-spacing` sources. |
| MANAGEDSHELL | ManagedShell upstream source | https://github.com/cairoshell/ManagedShell | 2026-08-16 | Apache-2.0, active .NET/WPF library; tasks, notification area, AppBar, and Explorer coexistence. |
| CAIRO | Cairo Desktop upstream source | https://github.com/cairoshell/cairoshell | 2026-08-16 | Apache-2.0 reference implementation for ManagedShell, settings, pinned tray icons, and hiding/restoring Explorer taskbar. |
| OPEN-SHELL | Open-Shell upstream source | https://github.com/Open-Shell/Open-Shell-Menu | 2026-08-16 | MIT; source inspection shows deep Explorer injection and undocumented Win11 shell-hotkey interception. |
| EXPLORERPATCHER | ExplorerPatcher taskbar implementation notes | https://github.com/valinet/ExplorerPatcher/wiki/ExplorerPatcher%27s-taskbar-implementation | 2026-08-16 | Current taskbar implementation is not source-available for stated legal reasons; Windhawk mods are recommended for tweaks. |
| ANYFSE | AnyFSE full-screen gaming home source | https://github.com/ashpynov/AnyFSE | 2026-08-16 | MIT, but targets Windows Gaming Full Screen Experience rather than desktop Start. |
| REDDIT-MULTIROW | Public Windhawk multi-row announcement/discussion | https://www.reddit.com/r/Windhawk/comments/1h917wn | 2026-08-16 | Confirms separate height mod requirement and user interest; public search fallback because authenticated Reddit backend was unavailable. |
| BILI-WIN10-FULL | Bilibili result documenting demand for Win10 full-screen Start | https://www.bilibili.com/video/BV1Jf4y1b7Fn | 2026-08-16 | 14k+ plays at access; research metadata only. |
| BILI-WINDHAWK | Bilibili Windhawk customization overview | https://www.bilibili.com/video/BV17fz6BkEAa | 2026-08-16 | 85k+ plays at access; taskbar/start customization interest. |
| YT-MULTIROW | YouTube Win11 multi-row taskbar walkthrough | https://www.youtube.com/watch?v=s-w5Edsu_7s | 2026-08-16 | 16k+ views at access; hands-on tutorial evidence, not an API authority. |

## Channel limitations

The installed Twitter CLI could not decrypt/find a current authenticated
session, and both OpenCLI/rdt Reddit searches lacked a usable authenticated
backend. No credentials were requested because these channels are not blocking
the primary-source, source-code, Bilibili, YouTube, and public Reddit evidence.
The `agent-reach doctor --json` command itself also failed to return within the
initial diagnostic window; individual channel executables and versions were
therefore checked directly.
