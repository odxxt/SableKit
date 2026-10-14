---
name: SableKit
description: Native Windows utility UI in smoky charcoal and white.
colors:
  surface: "#18181B"
  panel-surface: "#18181BEE"
  inset: "#232326"
  hover: "#333337"
  border: "#434348"
  text: "#F4F4F5"
  muted: "#ADADB6"
  accent: "#F4F4F5"
  accent-text: "#19191C"
  good: "#A1D5B0"
  bad: "#FFB1AB"
  light-surface: "#F6F6F7"
  light-panel-surface: "#F6F6F7F2"
  light-inset: "#EBEBEF"
  light-hover: "#DFDFE5"
  light-border: "#C7C7CE"
  light-text: "#202024"
  light-muted: "#62626B"
  light-accent: "#242429"
  light-accent-text: "#FFFFFF"
  light-good: "#25643D"
  light-bad: "#A02826"
typography:
  headline:
    fontFamily: "Segoe UI"
    fontSize: "24 DIP"
    fontWeight: 600
  title:
    fontFamily: "Segoe UI"
    fontSize: "18 DIP"
    fontWeight: 600
  body:
    fontFamily: "Segoe UI"
    fontSize: "13 DIP"
    fontWeight: 400
  label:
    fontFamily: "Segoe UI"
    fontSize: "12 DIP"
    fontWeight: 600
  metadata:
    fontFamily: "Segoe UI"
    fontSize: "12 DIP"
    fontWeight: 400
  compact-metadata:
    fontFamily: "Segoe UI"
    fontSize: "10 DIP"
    fontWeight: 400
  result:
    fontFamily: "Consolas"
    fontSize: "11 DIP"
    fontWeight: 400
  quick-result:
    fontFamily: "Consolas"
    fontSize: "10 DIP"
    fontWeight: 400
rounded:
  control: "12 DIP"
  panel: "20 DIP"
  header-control: "8 DIP"
  choice-item: "5 DIP"
  scrollbar-thumb: "3 DIP"
spacing:
  gap-6: "6 DIP"
  gap-7: "7 DIP"
  gap-8: "8 DIP"
  inset-10: "10 DIP"
  inset-12: "12 DIP"
  section-16: "16 DIP"
  section-22: "22 DIP"
  section-24: "24 DIP"
components:
  button-primary:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.accent-text}"
    rounded: "{rounded.control}"
    padding: "8 DIP 12 DIP"
  button-primary-hover:
    backgroundColor: "{colors.hover}"
    textColor: "{colors.text}"
    rounded: "{rounded.control}"
  button-filled:
    backgroundColor: "{colors.inset}"
    textColor: "{colors.text}"
    rounded: "{rounded.control}"
    padding: "8 DIP 12 DIP"
  button-ghost:
    backgroundColor: "transparent"
    textColor: "{colors.text}"
    rounded: "{rounded.control}"
    padding: "8 DIP 12 DIP"
  button-icon:
    backgroundColor: "transparent"
    textColor: "{colors.text}"
    rounded: "{rounded.control}"
    padding: "0 DIP"
    width: "30 DIP"
    height: "30 DIP"
  button-header:
    backgroundColor: "transparent"
    textColor: "{colors.muted}"
    rounded: "{rounded.header-control}"
    padding: "0 DIP"
    width: "28 DIP"
    height: "28 DIP"
  button-header-active:
    backgroundColor: "{colors.hover}"
    textColor: "{colors.muted}"
    rounded: "{rounded.header-control}"
  compact-panel:
    backgroundColor: "{colors.panel-surface}"
    rounded: "{rounded.panel}"
    padding: "12 DIP 14 DIP 14 DIP 14 DIP"
  input:
    backgroundColor: "{colors.inset}"
    textColor: "{colors.text}"
    rounded: "{rounded.control}"
    padding: "8 DIP 10 DIP"
  navigation-selected:
    backgroundColor: "{colors.hover}"
    textColor: "{colors.text}"
    rounded: "{rounded.control}"
    padding: "7 DIP 14 DIP"
  action-list:
    backgroundColor: "transparent"
    textColor: "{colors.text}"
    rounded: "{rounded.control}"
    padding: "6 DIP 9 DIP"
    height: "46 DIP"
  action-grid:
    backgroundColor: "{colors.inset}"
    textColor: "{colors.text}"
    rounded: "{rounded.control}"
    padding: "6 DIP 9 DIP"
    height: "72 DIP"
  configuration-preview:
    backgroundColor: "transparent"
    textColor: "{colors.text}"
    rounded: "{rounded.control}"
    padding: "6 DIP 9 DIP"
---

# Design System: SableKit

## Overview

**Creative North Star: "Smoky black, neutral charcoal and white"**

Native WPF, Segoe UI and installed Segoe MDL2 Assets carry the owner's modern, compact SableLatch-inspired utility direction. This records the approved 0.1.3 refinement on 2026-10-14. The user explicitly requested an action grid, top tabs, separate action editors, sticky button preview, quieter scrolling and removal of gray window frames. Preserve the existing palette and action-first interaction.

Source of truth: ThemeManager.cs, Theme.xaml, Ui.cs, ActionVisual.cs and the window XAML/code-behind files under src/SableKit.Desktop. All measurements are WPF DIP. Frontmatter uses CSS-compatible RRGGBBAA for alpha colors; WPF uses AARRGGBB. Browser sidecar samples are illustrations only.

The build passed 76 in-process WPF checks and 46 core checks, with 21 client renders. The independent initial review found narrow-grid and choice-focus defects; local corrections and regression checks passed. Its confirmation/documentation handoffs hit a usage limit, so no independent final sign-off is claimed. Desktop blur remains unmet. See VERIFICATION.md for scope.

## Colors

Use dynamic resource roles. Surface is opaque charcoal; PanelSurface is its translucent shell fill. Inset separates fields and tiles; Hover marks interaction or selection. Text and Muted carry primary and supporting text. Accent/AccentText invert the primary button; Bad supplies failure text. Good is declared but not a general success fill. Copied uses normal foreground.

ThemeManager overrides bootstrap Inset, Hover and Border values; frontmatter records runtime values. Dark/light/system themes exist. High contrast maps to SystemColors WindowBrush, WindowTextBrush and HighlightBrush; its complete interaction remains unverified. Thin field outlines remain, while buttons and window shells have no outer stroke.

## Typography

Segoe UI is the native interface face; Consolas is reserved for raw network results. WPF supplies normal line height and spacing. Main headings are 24 DIP, body 13, labels 12 semibold, action-library names 14 semibold and tool metadata 11. Compact list names are 12 and grid names 11, with 10-DIP metadata. Header glyphs are 14 DIP. Results use 11 DIP in SableNet and 10 in the tray.

Compact names trim; explanatory copy wraps. Raw output preserves command line breaks without wrapping and supports both scroll axes. Window layout rounding and Display text formatting are enabled. No fonts are redistributed.

## Layout

Tray: fixed 336 x 404, shell radius20, left/top/right/bottom padding14/12/14/14, header38. Search and clipboard suggestions are conditional. Pinned actions occupy the flexible area as list or two-column grid. Results replace that area with an explicit tool handoff. SableNet is the sole current tool shortcut; password generation and app launching belong to SableKit. The header Settings icon opens configuration; no footer Open app. The footer profile carousel uses arrows, wheel and keyboard.

Main: up to920 x640, capped at90%/88% of work-area width/height, minimums relaxed for small screens. Shell padding22/8/22/18, caption row38, top navigation row46. Simple adjacent Actions/Profiles/Tools/Activity/Preferences tabs replace the sidebar. Default Actions contains search and a responsive grid of118-DIP tiles with12-DIP gaps and16-DIP inner padding; two columns fit at720 width. Clicking a tile opens settings, not execution.

Action editor: separate owned660 x650 window capped to90%/88% of work area. Shell padding22/10/22/20 and caption38. An82-DIP preview area stays above the scrolling fields, with List/Grid selectors and a308-DIP preview region. Save/Test/Duplicate stay below the form. Existing editors are reused by action ID; closing protects drafts, including when closing the owner. Captured profile membership is stable while editing.

SableNet: fixed356 x454, radius20, padding14/12/14/14, header32. Address and optional Port, DNS and fill-only presets precede flexible raw output. Bottom Check connection/Stop and Copy stay reachable. Tray and SableNet remember dragged placement and reset on a title double-click.

Field labels have16-DIP separation above and7 below; helper text follows by6. Profiles caps its form at590 and Preferences at540. Small work-area captures are layout evidence, not physical monitor/DPI validation.

## Elevation & Depth

The reliable depth system is tonal surfaces and thin field outlines, with no custom shadow. All four windows use AllowsTransparency=True, transparent backgrounds and rounded WPF shell borders without WindowChrome. NativeWindow bypasses DWM backdrop requests for these shells. **The current material is smoky alpha translucency, not desktop blur.** Actual compositor edges still need hands-on validation; no API or client screenshot proves them.

The original blur direction remains a requirement, not an accepted alpha-only replacement. Maintain readable contrast without relying on blur. No custom DropShadowEffect or shadow scale exists.

Copied fades from0.65 to1 in140ms only while visible and motion is enabled. Content scrolling uses140ms cubic easing, a short normal step and bounded queued travel; it stops on hidden/unloaded/input changes. App and Windows reduced-motion settings suppress animation. No continuous idle animation is added.

## Shapes

All window shells use20-DIP rounded corners and no native outer frame. Controls, action tiles and text fields use12-DIP corners; header buttons8, choice items5 and scrollbar thumbs3. Native installed MDL2 glyphs retain SableLatch familiarity. No raster assets or browser runtime are shipped.

## Components

### Buttons and navigation

Buttons have no stroke or FocusVisualStyle outline. Keyboard focus and hover use tonal fill; primary foreground changes to Text when its fill becomes Hover. Header glyphs brighten on focus/hover and retain active pin/search fill. Pressed opacity is0.72 for common buttons,0.55 for header glyphs; disabled is0.45/0.3. Header controls are28 x28, main captions36 x28. Tab padding is14/7 horizontal/vertical, minimum height34, selected fill Hover and semibold Text.

### Inputs and choices

TextBox focus changes its fill to Hover, preserving its thin resting field outline. Closed ComboBox focus changes the Toggle's fill to Hover through the template; no focus border is added. Fields have persistent labels and automation names. Choice popups use rounded Surface,4-DIP padding and280 maximum height. Multiple-address pickers start unselected; explicit result handoff clears unrelated choices. Preserve native keyboard expectations.

### Action buttons and sticky preview

ActionVisual creates both real tray buttons and preview content. List buttons are308 x46; preview grid buttons148 x72. Names, kind, password inheritance/override and description update the preview without dispatching an action. Its control is non-interactive. Only explicit Test run executes. The preview stays above the scrolling form and uses the current draft with the actual shared button rendering.

Running/Copied preserves dimensions and keyboard focus. Successful clipboard writes alone produce the centered17-DIP checkmark and Copied with7-DIP gap. Password values never appear in activity output.

### Scrolling and profiles

Content wheel steps are27 DIP at the usual three-line setting and queue at most90 DIP from the current position. The profile wheel accumulates120 units, ignores burst input for420ms after an accepted step and resets accumulation when direction reverses. Arrow controls remain immediate and repeated keydown is ignored. This is source/in-process evidence; physical-wheel feel remains to be reviewed by the user.

### Network results

Selectable, read-only Consolas preserves actual ping, Test-NetConnection and nslookup output. TCP success means reachability, not service health. SSH/HTTP/HTTPS/RDP/SMB presets fill Port only. Empty port selects ping; entered port selects TCP. Check connection becomes Stop during work; Copy enables only with output. Quick results appear in the tray before explicit Open SableNet handoff.

## Do's and Don'ts

- Use dynamic WPF palette roles and shared action rendering.
- Keep keyboard focus visible through fill or foreground changes.
- Protect drafts and separate non-executing preview from Test run.
- Keep configuration in the main/editor windows and execution in the compact tray.
- Do not reintroduce a sidebar, bottom Open app or standalone password tool shortcut.
- Do not call alpha translucency blur or client renders compositor validation.
- Do not animate hidden windows or claim the complete default bundle is implemented.
