# Test strategy
## Layers

- Pure unit tests for layout, search ranking, configuration migration, model
  state, command routing, and failure policies.
- Component tests for Windows interop behind testable adapters.
- Integration tests against a real Explorer session and representative windows.
- End-to-end input tests using actual keyboard/mouse events and screenshots.
- Visual regression at supported themes, resolutions, DPIs, row counts, and
  content densities, with human review for motion and aesthetic coherence.
- Performance and endurance measurements, including repeated open/close and
  long-running idle/active sessions.
- Fault injection for corrupted settings, Explorer restart, display changes,
  process crashes, denied access, missing icons, and update interruption.
- Runtime-order probes such as `--qa-settings-roundtrip` exercise persistence
  only after the custom taskbar and independent watchdog have initialized.
- `scripts/qa-accessibility.ps1` launches six real UI surfaces, reads them from
  a separate process through Windows UI Automation, moves actual focus, rejects
  implementation-object names, and invokes every taskbar overflow menu.

## Evidence rules

- Runtime logs must include build identity, OS build, display/DPI, test ID, and
  monotonic timestamps.
- Screenshots must identify the scenario and expected result; visual diffs keep
  both baseline and actual images.
- Manual observations supplement automation but cannot replace repeatable checks
  for deterministic behavior.
- A release summary links each acceptance row to the exact report or artifact.
