# Desktop surfaces

## Current direction — 2026-10-15 / 0.1.5

Keep the action grid and stable four-tab navigation: Actions, Profiles, Tools, Preferences. Main and network content is opaque. Search expands inline; both action surfaces expose list and grid directly. The main window has bounded edge/corner resizing.

A single opaque800x540 action dialog disables/dims its owner and has a soft shadow. Save closes it and briefly highlights the saved card. Unsaved close remains guarded. Purpose choice, Setup/Behavior and the non-executing sticky preview remain.

Profiles use faded inline creation, double-click rename, a library picker and animated drag order. The quick panel shares drag ordering. Tray 342x411 and 18-DIP bottom-right placement still match SableLatch. The divider precedes one tool/profile row; tool labels collapse responsively. SableNet remains the only installed tool, with address/port/presets/native-output hierarchy and a bottom check.

Reduced motion disables the save sheen and reorder transitions. Alpha translucency in the quick panel does not provide desktop blur. Client captures and routed-event checks do not verify physical drag feel, compositor edges or live DPI changes. See VERIFICATION.md.
