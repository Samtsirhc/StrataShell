# Contributing

StrataShell welcomes focused, test-backed changes.

## Local development

1. Install the .NET 8 SDK on Windows 11.
2. Run `dotnet restore StrataShell.sln`.
3. Run `dotnet build StrataShell.sln -c Debug --no-restore`.
4. Run `dotnet test StrataShell.sln -c Debug --no-build`.

Taskbar changes must include a recovery test. Windows-key changes must keep
Win-key shortcuts fail-open and add state-machine coverage. Visual changes
should include a 100% or 125% DPI screenshot and a short rationale grounded in
the principles under `docs/research/VISUAL_PRINCIPLES.md`.

Do not add Explorer injection to the default path. Keep undocumented shell
integration isolated, optional, and documented.
