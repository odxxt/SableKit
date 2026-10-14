# Desktop surfaces

Mode: Operate. Native WPF tray, action grid, one action editor and compact network checks.

## Current direction - 2026-10-14 / 0.1.4
Keep the action grid the owner likes. Main/editor content is opaque. Main tabs have fixed positions and restrained fill; search expands from a header icon. Profiles uses a profile list and ordered pins. Preferences uses labeled sections with fixed Save. Tools leads with available SableNet and separates planned tools. Activity prioritizes real output.

Action settings reuse a single800x540 window. Protect unsaved changes before switching actions. New action begins with a purpose choice, then Setup/Behavior sections and a sticky side preview. Preview never executes.

Tray342x411 exactly matches inspected SableLatch dimensions, subject to device-pixel rounding. Default/reset sits18DIP from right/bottom of work area; dragged positions persist. A thin divider separates actions/tools. The profile control sits on the right with direct choices and gated wheel navigation. List/grid use original vector view glyphs.

Main/editor opaque Surface, compact alpha PanelSurface; all shells rounded20DIP without native frame chrome. Compact blur remains unimplemented. Source and local in-process evidence do not prove compositor edges or physical mouse/keyboard travel. See VERIFICATION.md.
