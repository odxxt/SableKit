# SableKit

A portable Windows toolkit built around tools, reusable actions, and profiles. Native C# / WPF / XAML on .NET 10.

**Current status: working prototype 0.1.1.** The compact panel and configuration window execute real password, application-launch, and network operations. This is the first usable slice of the intended bundle; protected tools and workflows are still planned.

## Run the portable build

1. Extract the entire `SableKit-win-x64` folder to a writable location.
2. Run `SableKit.exe`. The .NET runtime is included; keep the accompanying files together.
3. Open the quick panel from its tray icon or **Ctrl+Alt+K**. Use **Open app** for configuration. Right-click the tray icon and choose **Quit** to exit.

Settings are stored in `SableKit-data/settings.json` beside the executable, with a backup on replacement. Ordinary tools require no vault setup. The binary is unsigned. Windows 10 and 11 are intended targets; this prototype has been exercised on Windows 11 build 26200, x64. A clean machine without .NET and Windows 10 remain to be tested.

## UI fixes in 0.1.1

Borderless SableLatch search/pin glyphs now show persistent active states. The compact tool strip and grid no longer clip their default contents. The main window fits its monitor work area and includes an explicit minimize/maximize/close bar.

Quit the previous version before launching this build. When extracting to a new folder, copy your existing SableKit-data folder there before launch to retain settings.

## Working now

- Compact 336 × 404 logical-pixel panel, pinning, on-demand search and aliases, pinned list/grid view, Default/Lab profiles, results, and action states.
- Real cryptographic password generation and clipboard copy, with centered **Copied** feedback only after a successful write.
- Application actions with an executable, separate literal arguments, and per-action panel behavior.
- SableNet: editable clipboard-initialized target, ping, TCP checks and port presets, DNS/reverse lookup, cancellation, elapsed time, and result copying.
- Main window for shared actions, explicit password-default inheritance or override, non-executing preview, explicit test run, duplication, enabling/disabling, profile membership/order, theme, and reduced motion.
- Versioned local settings; main and network windows created on demand; no clipboard polling. Windows 11 native backdrop request with opaque fallback; dark, light, and system themes.

**Planned:** SableType, integrated SableLatch and shared security, workflows, addon catalog/API, configurable action hotkeys, advanced command/script execution, richer inputs/results/customization, and the remaining SableNet operations. See [implementation status](IMPLEMENTATION-STATUS.md). No placeholder is presented as a working protected tool.

## Build

Install the .NET 10 SDK selected by `global.json` and run on Windows:

```powershell
./build.ps1 -Portable
```

This runs the dependency-free core checks, builds WPF, and publishes a self-contained x64 folder to `artifacts/SableKit-win-x64`. Windows CI uses the same script. No third-party UI library is required. Preserve the runtime notices included by the SDK when redistributing its files.

For the optional Windows UI harness, run the published executable with `--verify <absolute-output-folder>`. It opens test windows, temporarily uses and then restores the clipboard, runs local TCP checks, writes client-content captures and a JSON report, and exits. `--measure <absolute-output-folder>` measures a separate panel-only process; `--opaque` disables its backdrop request for comparison. These are developer diagnostics, not ordinary startup options.

## Project map

- `src/SableKit.Core`: action/profile models, settings persistence, defaults, validation, passwords, network parsing/checks.
- `src/SableKit.Desktop`: WPF windows, theme/resources, native tray/hotkey/backdrop integration, execution and diagnostics.
- `tests/SableKit.Tests`: meaningful core checks without external test packages.
- [Product requirements](PRODUCT-REQUIREMENTS.md), [architecture](ARCHITECTURE.md), [milestones](MILESTONES.md), [verification](VERIFICATION.md), [design system](DESIGN.md).

The new repository was inspected empty before the foundation was written. The owner subsequently approved direct implementation and a smoky charcoal visual direction. SableLatch was inspected as a UX reference; 0.1.1 adapts its borderless header styling and uses the same Windows search/pin glyphs. See THIRD-PARTY-NOTICES.txt and SABLELATCH-NOTICE.txt. Other vector icons are original.
