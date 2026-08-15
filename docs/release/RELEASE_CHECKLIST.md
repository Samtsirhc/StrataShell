# 0.1.0 preview release checklist

- [x] License and third-party notices are complete.
- [x] Secrets and machine-specific runtime data are absent from tracked files.
- [x] Zero-warning Release build, format check, and 28 automated tests pass.
- [x] Panel, search, real two-row/overflow layout, settings, secondary-monitor placement, and crash recovery have visual evidence.
- [x] Recovery-first portable install/removal instructions are public.
- [x] Unsigned preview status, .NET runtime dependency, limitations, and checksums are documented.
- [x] CI reproduces restore, format, build, test, vulnerability audit, publish, and artifact upload.
- [ ] Clean Windows 11 install/update/rollback/uninstall matrix passes.
- [ ] Physical Windows-key, accessibility, multi-monitor/DPI, display/power/session, and endurance matrices pass.
- [x] Public repository and tagged release are published; attached ZIP hash verification is recorded in the validation report.

Unchecked items are final-product gates and explicit 0.1.0 limitations, not
silent claims of completeness.
