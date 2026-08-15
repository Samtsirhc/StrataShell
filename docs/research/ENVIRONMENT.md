# Current validation environment

Captured: 2026-08-16 (Asia/Hong_Kong)

## Operating system

- Caption: Microsoft Windows 11 Pro
- Display version: 25H2
- Build: 26200.9168, x64
- Note: legacy `ProductName` compatibility registry and `Get-ComputerInfo`
  fields still say `Windows 10 Pro`; `Win32_OperatingSystem.Caption`, build,
  and display version establish the actual Win11 environment.
- CPU: AMD Ryzen 9 9900X, 12 cores
- Physical memory: approximately 64 GiB

## Development tools at project start

- .NET SDK 8.0.403; .NET Desktop Runtime 8.0.10 and 9.0.16
- Node.js 22.23.2 / npm 10.9.8
- GitHub CLI 2.92.0, authenticated as `Samtsirhc`
- Rust and CMake were not present on PATH
- Windows SDK bins present through 10.0.20348; .NET targeting packs may still
  supply sufficient Win32 interop because the selected path uses P/Invoke.

## Workspace state

The workspace was empty and was not a Git repository at project start. No
existing user source or changes had to be preserved. Git initialization and
public repository creation are intentionally deferred until naming, license,
and architecture records are ready.
