# Desktop surfaces

Mode: Operate. Native WPF tray, action library, separate editors and compact network checks.

## Direction contract
THESIS: Find and run an action quickly; configure deliberately in the larger workspace.
OWN-WORLD: Neutral smoky charcoal, white labels, gray metadata, Segoe UI and MDL2 icons. Thin field outlines; no button/tab/native-frame outlines. Rounded transparent20-DIP shells currently provide alpha translucency only. Original desktop blur remains unmet.
STORY: Open, find, run, inspect real output; explicitly open the owning tool for detail. Preview never executes.

## Approved composition - 2026-10-14
Tray336x404: borderless header search/pin/settings/close, conditional local clipboard suggestion, pinned list/grid or inline results, SableNet tool shortcut, profile carousel. No footer Open app; password generation/apps are built in. Wheel dead zone120 and420ms cooldown.
Main up to920x640 capped90%/88% work area: caption controls and simple horizontal top tabs. Default action grid with search;118-DIP cards,12 gaps, two columns at720. Clicking opens a separate settings window.
Editor660x650 capped to work area: sticky82-DIP live list/grid preview above scrolling fields, fixed Save/Test/Duplicate below. ActionVisual shared with tray; independent unsaved draft guards.
SableNet356x454: editable address, optional Port, presets that fill without execution, native output, bottom Check/Stop and Copy.

## Behavior
Content wheel scrolling uses140ms easing, usual27-DIP step and90-DIP queue bound; reduced motion skips animation. Keyboard focus remains visible through fill/foreground. Tray placement/toggle/pin and explicit result handoff retain SableLatch-inspired behavior.

## Evidence and limits
Current0.1.3:76 in-process WPF checks,46 core checks,21 client renders. Narrow-grid and closed-choice focus fixes passed targeted local checks. Independent initial finish review exists; confirmation/documenter hit usage limits, so final approval is not claimed. Client renders do not validate desktop compositing or physical input. Blur is not active; Windows10, high contrast, native resizing and live DPI/monitor transitions remain unverified. See VERIFICATION.md and DESIGN.md.
