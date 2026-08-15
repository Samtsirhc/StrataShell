# Product goal
Build, validate, and publicly release a maintainable open-source Windows 11
application that:

1. Restores a genuinely full-screen panel when the user presses the Windows
   key, with visual quality at least consistent with Windows 11 and informed
   by Apple's clarity, motion, spacing, and restraint.
2. Provides a configurable taskbar replacement or extension whose height can
   be changed and whose shortcut, running-window, and status areas can use two
   or more rows without sacrificing usability.
3. Runs automatically at sign-in when enabled, exposes a complete settings
   interface, and provides a tray icon that shows status and controls enable,
   settings, and auto-start behavior.
4. Remains low-overhead, resilient, readable, testable, portable as a public
   repository, and recoverable if Windows Explorer or the application fails.

## Definition of done

The goal is complete only when every mandatory row in
`docs/requirements/ACCEPTANCE.md` is marked `PASS` with a reproducible evidence
link, a clean installation can reproduce the experience on Windows 11, and the
source plus release artifacts are available from the intended public GitHub
repository. A build, prototype, static screenshot, or implementer's manual
self-check is not sufficient by itself.

## Guardrails

- The original Windows shell must remain recoverable. Experimental Explorer
  manipulation is opt-in and must include a documented safe-mode or reset path.
- No third-party source will be copied until its license permits the intended
  use and attribution obligations have been recorded.
- Closed-source paid products may be evaluated for behavior and visual ideas,
  but their code or protected assets will not be copied.
- Secrets, browser cookies, personal paths, and the provided Feishu webhook
  must never be committed.
