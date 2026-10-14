# SableKit

A portable Windows toolkit built around tools, reusable actions, and profiles. Native C# / WPF / XAML on .NET 10.

**Current status: working prototype 0.1.3.** The compact panel and configuration window execute real password, application-launch, and network operations. This is the first usable slice of the intended bundle; protected tools and workflows are still planned.

## Run the portable build

1. Extract the entire `SableKit-win-x64` folder to a writable location.
2. Run `SableKit.exe`. The .NET runtime is included; keep the accompanying files together.
3. Open the quick panel from its tray icon or **Ctrl+Alt+K**. Use the header **Settings** icon for configuration. Right-click the tray icon and choose **Quit** to exit.

Settings are stored in `SableKit-data/settings.json` beside the executable, with a backup on replacement. Ordinary tools require no vault setup. The binary is unsigned. Windows 10 and 11 are intended targets; this prototype has been exercised on Windows 11 build 26200, x64. A clean machine without .NET and Windows 10 remain to be tested.

## Changes in 0.1.3

The main window opens to a responsive action grid with simple horizontal tabs. Selecting a tile opens its settings in a separate window. New and existing actions share a live list/grid button preview that stays visible while fields scroll; previewing does not execute anything. Unsaved edits remain protected when closing either window.

The compact panels and main/editor windows use transparent rounded shells without the native gray frame. Their smoky alpha surface currently has no desktop blur. The footer Open app button is removed; Settings in the header is the configuration entry point. Password generation and application launching belong to SableKit itself, leaving SableNet as the current tool shortcut; workflows remain planned built-in functionality.

Profile wheel gestures accumulate a small dead zone and pause for 420 ms after one step. Content scrolling uses short bounded movement with 140 ms easing, disabled by reduced motion. Button and tab focus uses fill or foreground changes instead of outlines.

## Changes retained from 0.1.2

The tray follows SableLatch's compact header and placement behavior: drag to move, reopen in place, double-click the name to reset to bottom-right, and click the tray icon again to hide even when pinned. Pin/search retain their active fill without a title suffix or underline. The earlier native-frame approach is superseded by the transparent shells in 0.1.3. Profiles switch with left/right arrows, keyboard arrows, or the mouse wheel.

Quick-action output appears in the panel with an explicit **Open SableNet** handoff. SableNet is now a 356 x 454 DIP panel with separate address and optional port fields, fill-only presets, and a bottom Check connection button (Enter). It shows actual Windows ping, Test-NetConnection or nslookup output. Native TCP checks can take several seconds.

Clipboard detection understands IPs, hostnames, HTTP(S) URLs, explicit ports and nearby port descriptions. Multiple targets require a choice. Out-of-range loose prose ports are ignored; malformed explicit endpoints such as host:99999 require correction. Detection never runs a command on its own. Clipboard reads run on demand off the UI thread with a 250 ms response limit, so an unavailable clipboard does not delay panel opening.

Quit the previous version before launching this build. When extracting to a new folder, copy your existing SableKit-data folder there before launch to retain settings.

## Working now

- Compact 336 × 404 logical-pixel panel, pinning, on-demand search and aliases, pinned list/grid view, Default/Lab profiles, results, and action states.
- Real cryptographic password generation and clipboard copy, with centered **Copied** feedback only after a successful write.
- Application actions with an executable, separate literal arguments, and per-action panel behavior.
- SableNet: editable clipboard-initialized target, ping, TCP checks and port presets, DNS/reverse lookup, cancellation, elapsed time, and result copying.
- Main action grid and separate editors with a sticky live button preview, explicit default inheritance or override, test run, duplication, enabling/disabling, profile membership/order, theme, and reduced motion.
- Versioned local settings; main, network and editor windows created on demand; no clipboard polling. Rounded transparent shells; dark, light, and system themes.

**Planned:** SableType, integrated SableLatch and shared security, workflows, addon catalog/API, configurable action hotkeys, advanced command/script execution, richer inputs/results/customization, and the remaining SableNet operations. See [implementation status](IMPLEMENTATION-STATUS.md). No placeholder is presented as a working protected tool.

## Build

Install the .NET 10 SDK selected by `global.json` and run on Windows:

```powershell
./build.ps1 -Portable
```

This runs the dependency-free core checks, builds WPF, and publishes a self-contained x64 folder to `artifacts/SableKit-win-x64`. Windows CI uses the same script. No third-party UI library is required. Preserve the runtime notices included by the SDK when redistributing its files.

For the optional Windows UI harness, run the published executable with `--verify <absolute-output-folder>`. It opens test windows, temporarily uses and then restores the clipboard, runs local ping/TCP checks, writes client-content captures and a JSON report, and exits. `--measure <absolute-output-folder>` measures a separate panel-only process; `--opaque` is a legacy backdrop comparison flag and does not change the current layered shells. These are developer diagnostics, not ordinary startup options.

## Project map

- `src/SableKit.Core`: action/profile models, settings persistence, defaults, validation, passwords, network parsing/checks.
- `src/SableKit.Desktop`: WPF windows, theme/resources, native tray/hotkey/window integration, execution and diagnostics.
- `tests/SableKit.Tests`: meaningful core checks without external test packages.
- [Product requirements](PRODUCT-REQUIREMENTS.md), [architecture](ARCHITECTURE.md), [milestones](MILESTONES.md), [verification](VERIFICATION.md), [design system](DESIGN.md).

The new repository was inspected empty before the foundation was written. The owner subsequently approved direct implementation and a smoky charcoal visual direction. SableLatch was inspected as a UX reference; 0.1.2 adapts its borderless header styling and placement behavior; 0.1.3 also adapts its smooth-scrolling approach and uses the same installed Windows icon family. See THIRD-PARTY-NOTICES.txt and SABLELATCH-NOTICE.txt. No font files are redistributed.
