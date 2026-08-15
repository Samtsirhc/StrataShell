# Security policy

## Supported versions

Security fixes are provided for the latest published StrataShell release.

## Reporting a vulnerability

Please use GitHub's private vulnerability reporting feature instead of a
public issue. Include the Windows build, StrataShell version, reproduction
steps, and whether the Windows-key hook or custom taskbar was enabled.

## Recovery

StrataShell runs without administrator rights and does not replace
`explorer.exe`. When the custom taskbar is enabled, a separate watchdog restores
Explorer's taskbars if the main process crashes. If recovery is ever needed
manually, terminate StrataShell and restart Explorer from Task Manager.

Do not report ordinary appearance or compatibility issues as vulnerabilities;
use a normal issue for those.
