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
    fontSize: "12 DIP"
    fontWeight: 400
rounded:
  control: "12 DIP"
  panel: "15 DIP"
  window-chrome: "16 DIP"
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
  input:
    backgroundColor: "{colors.inset}"
    textColor: "{colors.text}"
    rounded: "{rounded.control}"
    padding: "8 DIP 10 DIP"
  navigation-selected:
    backgroundColor: "{colors.hover}"
    textColor: "{colors.text}"
    rounded: "{rounded.control}"
    padding: "11 DIP 10 DIP"
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
    backgroundColor: "{colors.inset}"
    textColor: "{colors.text}"
    rounded: "{rounded.control}"
    padding: "12 DIP 14 DIP"
---

# Design System: SableKit

## Overview

**Creative North Star: "Smoky black, neutral charcoal and white"**

SableKit uses the owner's chosen smoky black, neutral charcoal and white direction for a modern, enterprise-quality desktop utility. Subtle Raycast influence and the compact pin/search/click-to-act familiarity of SableLatch inform the implementation. Native Segoe UI, quiet metadata, thin outlines and restrained rounded controls keep actions readable.

This is an extraction of prototype 0.1, implemented directly in WPF without an approved image composition. It records the current code, not a new branding commitment. Tonal layering supplies the dependable structure; native Windows material is an enhancement whose compositor appearance still needs validation.

**Key Characteristics:**

- Compact controls with clear action labels and quieter supporting text.
- Charcoal and white emphasis, with semantic status resources.
- Original line geometry and native Windows typography.
- Stable action dimensions during running and Copied feedback.

Source of truth: `src/SableKit.Desktop/ThemeManager.cs`, `Theme.xaml`, `Ui.cs`, the three window XAML/code-behind files, `Icon.cs` and `NativeWindow.cs`. Product constraints live in PRODUCT.md; desktop composition and task strategy live in .impeccable/surfaces/desktop.md.

All layout and type measurements here are **WPF device-independent pixels (DIP, 1/96 inch)**. They are not CSS runtime values. Frontmatter padding lists top/right/bottom/left in that order; WPF Thickness constructors use left/top/right/bottom. The sidecar's browser samples translate these measurements to CSS pixels for illustration only.

The 0.1.1 WPF verification harness passed 37 checks and produced 13 client-content captures. This evidence does not validate physical input, compositor blur, live DPI/monitor changes, high contrast, or Windows 10.

## Colors

A neutral palette gives primary actions contrast without introducing a brand hue.

### Primary

- **White emphasis** (`accent`) and **charcoal-on-white** (`accent-text`) form the dark-theme primary button. Hover moves to the neutral hover surface and foreground text.
- Light mode inverts the emphasis through the corresponding `light-accent` roles.

### Neutral

- **Smoked charcoal** (`surface`) is the opaque foundation; `panel-surface` is the translucent client panel fill.
- **Inset charcoal** (`inset`) differentiates fields, filled buttons, grid actions and side navigation.
- **Hover charcoal** (`hover`) marks interaction or selection; `border` supplies thin separators and outlines.
- **Soft white** (`text`) carries foreground and focus; **quiet gray** (`muted`) carries secondary descriptions and shortcut hints.
- Every dark role has its extracted light counterpart in the frontmatter. `ThemeManager.Apply` selects Dark, Light or the system preference.

### Semantic status

- `bad` colors the quick-panel failure result; accompanying text states the failure.
- `good` is a declared theme resource, not a generally applied success fill in the current screens. Copied feedback uses the normal foreground.

**The Dynamic Role Rule.** Use WPF DynamicResource/SetResourceReference role keys for themed UI rather than hardcoded per-control colors.

The runtime theme manager overrides the bootstrap dictionary's Inset, Hover and Border values. Frontmatter records the runtime values. The two panel colors are serialized as CSS-compatible RRGGBBAA for DESIGN.md portability; the WPF source uses AARRGGBB, with alpha EE in dark mode and F2 in light mode. This is a format conversion, not a second palette.

High contrast overrides Surface, PanelSurface and Inset with SystemColors.WindowBrush; Text, Muted, Border, Accent, Good and Bad with WindowTextBrush; AccentText with WindowBrush; and Hover with HighlightBrush. Those are OS values, not fixed hex tokens. Implementation of this mapping is not proof of its end-to-end accessibility.

## Typography

Segoe UI carries interface text, with Consolas for the SableNet result field. WPF defaults supply line height and letter spacing; no custom type scale ratio or font fallback stack is established.

The frontmatter captures the main page heading, semibold editor title, body, field label, standard metadata, compact metadata and network-result roles. Observed local variants include the main brand at 21 DIP, SableNet heading at 26 DIP, tool titles at 16 DIP, grid action names at 11 DIP and list action names at 12 DIP. These are existing component choices, not a new global scale.

Compact names and metadata trim with ellipses; explanatory form copy and result text wrap. Short supporting text should remain readable when copied into new controls. Layout rounding and Display text formatting are enabled on windows.

## Layout

The quick panel is fixed at 336 × 404 DIP. Its outer border has one-DIP stroke and 15/12/15/12 DIP left/top/right/bottom padding. The header has a fixed 32-DIP row, search and suggestion rows are conditional, and the action area scrolls above the tool strip and footer. Pinned actions render as a list or a two-column grid; searching switches to a list.

The main window opens at up to 920 × 640 DIP, capped to 90% of its monitor work-area width and 88% of its height. Minimum sizes relax to fit a smaller work area. Its 40-DIP draggable title bar always exposes minimize, maximize/restore and close. The sidebar is 154 DIP wide with scrolling navigation; content margins are 20/18/20/16 DIP. Actions allocates 36% to the library (including a 16-DIP gap) and 64% to the editor. Fields scroll and editor commands wrap below them. Profiles caps its flexible form at 590 DIP; Preferences caps at 540 DIP.

SableNet starts at 510 × 560 DIP, with a 430 × 480 minimum and a 24-DIP content inset. Its result field absorbs remaining height. Action rows use wrapping containers where implemented.

Spacing is extracted from repeated code values rather than an imposed scale. Field labels sit 16 DIP below the preceding content and 7 DIP above their control; helpers follow by 6 DIP. There are no web breakpoints or mobile layouts. Minimum-window captures were inspected; broader DPI, text scaling and multi-monitor behavior still need native testing.

## Elevation & Depth

The reliable depth system uses tonal surfaces and one-DIP borders. The project defines no custom DropShadowEffect or shadow scale. On supported Windows versions, NativeWindow requests native rounded corners and system backdrop: transient-window material for the compact panel, main-window material for the larger windows. The backdrop request is gated to Windows build 22621 or later, outside high contrast and when the app has not disabled it. Older platforms retain the ordinary window background.

**The Readable Surface Rule.** Maintain contrast on the ordinary charcoal/light surface; native material must not be necessary to understand a control.

The API accepted the request in the recorded Windows 11 run, but RenderTargetBitmap does not capture compositor blur or native title bars. Do not treat those captures as visual confirmation of material, transparency-disabled behavior or the Windows 10 fallback.

The only custom visible feedback animation is a 140-ms opacity change from 0.65 to 1 on Copied content. It runs only while the panel is visible, application reduced motion is off and Windows client-area animation is enabled. There is no continuous animation.

## Shapes

Controls, navigation items, action tiles, fields and the configuration preview share the control radius. The compact panel uses separate outer-border and window-chrome radii recorded above. Dropdown items and scrollbar thumbs use smaller radii. Thin strokes define fields and secondary actions; primary actions rely on filled contrast.

Header/caption icons use the installed Segoe MDL2 Assets font (matching SableLatch); no font files are redistributed. Other icons are original WPF Geometry paths on a 24-unit coordinate system with a 1.45-unit stroke, rounded caps and joins, scaled to the control's displayed size. Typical sizes range from 14 to 19 DIP. No shipping raster asset is used by this prototype.

## Components

### Buttons

Primary buttons are semibold, use accent fill and its paired foreground, then use Hover/Text on hover. Filled buttons use Inset with a Border stroke; ghost buttons begin transparent. The common minimum height is 32 DIP, with a zero-padding 30-DIP icon control as an explicit compact exception. New action uses 18/10 horizontal/vertical padding.

Header controls use the same installed Segoe MDL2 Assets glyphs as SableLatch: Search E721, Pin E718 and Close E8BB. Their adapted styling has no border. Active search/pin uses a persistent Hover fill; pinned state also adds “Pinned” to the panel title. Keyboard focus uses a short underline and brighter glyph, distinct from the active fill. Caption buttons use the same treatment. Other buttons follow the common styles below.

Pressed chrome uses 0.72 opacity. Disabled buttons use 0.45 opacity. Keyboard focus changes the border to Text; the Focus style also supplies a one-DIP outline with a two-DIP inset. These are WPF state triggers, not CSS transitions.

### Inputs and choices

Text fields use Inset, Text, Border and the shared control radius, with a 36-DIP minimum height. The template provides muted placeholder text from Tag only while empty; persistent labels and automation names identify form fields. Focus changes the border to Text. Disabled fields use 0.45 opacity.

Combo boxes have a 32-DIP minimum, a right-side chevron, a bordered rounded popup with 4-DIP inset and 280-DIP maximum height, and highlighted/selected item backgrounds. Preserve native keyboard expectations; full keyboard and high-contrast behavior remain verification items.

### Navigation and library rows

Main navigation combines a line icon and body text, with the selected destination filled by Hover. Library rows use a bold name and muted tool description. Selection uses Hover, while mouse-over uses Inset. The current implementation's trigger precedence is the source of truth; do not infer new multi-state behavior from a static sample.

### Action list and grid

A list action holds an icon, semibold name, metadata and an Alt-number hint for the first nine visible actions. Grid actions show icon, a single-line ellipsized name and tool metadata. Tooltips retain the full name. Their distinct fixed heights are in the frontmatter.

**The Stable Feedback Rule.** Running and Copied feedback retain the action's dimensions and preserve keyboard focus across updates.

Successful clipboard actions replace the content with a centered checkmark and Copied. The checkmark is 17 DIP with a 7-DIP label gap. Running, failed and cancelled text reflects actual execution state; a failed clipboard write must not produce Copied.

### Configuration preview and results

The configuration preview is a simple inset, rounded container showing the draft action name. It is not an elevated dashboard card. Network output is selectable, read-only, wrapped monospaced text with scrolling. Status summaries use text and polite live-region metadata where implemented. Copy result remains disabled until a result exists.

## Do's and Don'ts

### Do:

- **Do** use the dynamic WPF resource roles so theme changes reach controls.
- **Do** preserve action bounds and keyboard focus when feedback changes.
- **Do** show Copied only after the clipboard write succeeds.
- **Do** keep settings labels visible and editor actions reachable below the scrolling form.
- **Do** treat native material, high contrast and physical-input behavior as pending platform verification.

### Don't:

- **Don't** add continuous or hidden-window animation.
- **Don't** rely on blur for contrast or distinguish status by color alone.
- **Don't** present browser component previews as native WPF or compositor evidence.
- **Don't** describe the prototype or its lightweight performance goal as ship-validated.

