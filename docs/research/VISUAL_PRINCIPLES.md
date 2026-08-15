# Visual and motion principles

The product should feel native to Windows 11 while learning from Apple's rigor,
not copying Apple assets or making Windows imitate another operating system.

## Principles

1. **Calm hierarchy:** one obvious primary action/area, restrained accent use,
   and progressively disclosed secondary controls.
2. **Scan before decorate:** consistent grid, alignment, optical icon sizing,
   typography, and spacing take priority over glass effects.
3. **Contextual material:** use Mica-like opaque tint for persistent settings
   surfaces and acrylic only for transient/light-dismiss surfaces. Provide
   solid and higher-contrast fallbacks.
4. **Purposeful motion:** opening establishes spatial origin; selection gives
   brief feedback; exit combines direct movement with fade. Nothing waits on
   animation, and reduced-motion turns it into an immediate/fade transition.
5. **Adaptive density:** comfortable, compact, and touch targets preserve the
   same hierarchy; grid cell and label sizing adapt instead of merely scaling
   the entire panel.
6. **Coherent icons:** prefer app-provided high-resolution icons and Segoe
   Fluent system glyphs; never mix arbitrary outline weights or blurry
   downscaled bitmaps.
7. **Resilient states:** loading, empty, unavailable, permission-limited, and
   crash-recovered states receive the same visual care as the happy path.

## Initial measurable baseline

- Use a 4 px spatial unit and consistent 8/12/16/24/32 group spacing.
- Aim for a normal app tile visual cell around 80-96 logical pixels, with
  configurable compact and touch modes proven at each target DPI.
- Direct open/close motion starts with Windows' 167 ms baseline; longer 250 ms
  motion is reserved for spatial re-layout, and all animations are interruptible.
- All text and meaningful non-text states must satisfy the recorded WCAG/Windows
  contrast and accessibility checks; transparency is never required to read UI.

These are hypotheses for visual prototypes and comparison testing, not frozen
brand specifications.
