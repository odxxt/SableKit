# Desktop surfaces

Mode: Operate. Scope: native tray, action library/configuration, compact network checks.

## Direction contract
THESIS: Reach a useful action in one click, then return to work. Configuration belongs in a deliberate larger workspace.

OWN-WORLD: Smoked charcoal, white action labels, quiet gray metadata, thin neutral field outlines, 12-DIP control radii, 20-DIP compact shells, native Segoe UI and installed Segoe MDL2 Assets icons. Compact shells have no gray outer stroke. System material is requested when supported; an opaque charcoal fallback maintains readability, while compositor appearance remains unverified.

STORY: Open, find, run, see the actual result. Pin when staying; edit without making the tray into a settings form.

FIRST VIEWPORT: 336 x 404 DIP tray with a 38-DIP header, conditional search/suggestion, pinned list/grid, secondary tool strip and profile carousel footer. Shell padding is 14/12/14/14 DIP left/top/right/bottom. Main window remains capped at 920 x 640 DIP and 90%/88% of monitor work-area width/height, with a visible 40-DIP caption row. Compact header controls are 28 x 28 DIP with 14-DIP SableLatch-style Windows glyphs, borderless active fill and brighter glyphs on hover/focus. There is no focus underline or Pinned suffix in the brand title.

INTERACTION: Dragged tray placement persists; double-clicking the brand resets it. Profile arrows, mouse wheel and left/right keys on the profile button cycle profiles. Quick results replace the action area and offer a handoff to the owning tool: network target/output opens in SableNet; other details open Activity. Successful password copying retains stable-sized Copied feedback.

SABLENET: Fixed 356 x 454 DIP compact panel, with 20-DIP shell/WindowChrome radius, 14/12/14/14 DIP padding, a 32-DIP header and a 13-DIP brand. Address, optional Port, DNS and port presets precede raw native command output; Check connection/Stop and Copy output remain at the bottom. Presets populate the port without executing. Multiple clipboard addresses show an unselected “Choose detected address” picker; explicit target/result handoff clears and hides the picker. SableNet also remembers placement and supports brand double-click reset. Raw result text is Consolas 11 DIP here and 10 DIP in the tray, without wrapping and with horizontal/vertical scrolling. The TextBox template respects zero BorderThickness on both output fields.

FORM: User-pinned SableLatch/Raycast-adjacent operation surface. Context launcher and concept-seed were attempted but their engine download failed; no seed key exists. Explicit user direction and direct-code choice govern this build, not an invented roll. Signature feedback is a stable-sized centered checkmark and Copied only after successful clipboard write.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

No shipping rasters planned. Application icons use installed Windows Segoe MDL2 Assets throughout; no font files are redistributed. Adapted SableLatch header styling retains its MIT notice. No animation runs while hidden. The 0.1.2 refresh includes 17 diagnostic client-content captures; 56 in-process WPF checks passed before the final asynchronous clipboard change; the full retest is blocked by Windows clipboard access denial. Physical mouse interaction, compositor appearance, live DPI/monitor changes, high contrast and Windows 10 remain unverified. Review must distinguish captured WPF rendering from compositor/native-interaction validation.
