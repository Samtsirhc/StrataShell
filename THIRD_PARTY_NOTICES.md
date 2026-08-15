# Third-party notices

StrataShell is MIT-licensed. Its distributed build includes the following
runtime dependencies under their own licenses:

- [ManagedShell](https://github.com/cairoshell/ManagedShell), Apache License 2.0.
  It provides AppBar, running-window, notification-area, and shell interop
  foundations. StrataShell does not copy ExplorerPatcher or Start11 code.
- Microsoft .NET runtime libraries, MIT License and accompanying Microsoft
  notices.

The complete transitive dependency inventory is produced during release QA
with `dotnet list package --include-transitive`.
