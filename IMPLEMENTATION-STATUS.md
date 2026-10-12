# Implementation status — prototype 0.1

Recorded 2026-10-12. "Prototype" means working code with the limits below, not completion of the product requirement or its entire milestone.

| Area | Current working slice | Remaining |
|---|---|---|
| Tray | Open/hide, pin, search/aliases, list/grid, profile switch, results, Alt+1–9 visible-action access | Full physical-input and multi-monitor validation; configurable hotkeys |
| Actions | Shared IDs, editable name/description/aliases, kind, enabled state, profile pins, duplicate, preview/test, three panel policies | Icons/colors/sizes, favorites/groups/templates, prompts/files/variables, timeouts/confirmations/result routing, owning-tool policy |
| Passwords | CSPRNG generation, default length and nullable override, clipboard success/error, dimension-stable Copied feedback | Broader generation policy and customization |
| Applications | Executable and literal argument list; real Process.Start; unavailable path explanation | Inline/file scripts, shell/working-directory/environment controls, elevation, capture, timeout and keep-open options |
| Profiles | Default and Lab, create/rename, shared action references, pin order, per-profile view | Variables, deletion/import/export, favorites |
| SableNet | Strict single IP/hostname and optional port; ping/TCP, presets, DNS/reverse, visible target retained, stop/retry/copy | Multiple-target picker, extraction from prose, continuous ping, trace route, terminal launch, subnet tools, editable presets/timeouts/counts |
| Suggestions | Valid single clipboard network target on panel open; click to execute | Installed-tool discovery and other content types |
| Appearance | Dark/light/system, reduced motion, high-contrast fallback code, native backdrop request and opaque fallback | Hands-on high-contrast, compositor appearance, screen-reader and multiple-DPI validation; final mascot |
| Persistence | Schema 1, validation, atomic replacement/backup, corrupt-file preservation | Migration from future versions, import/export and full recovery UI |
| Portable build | Self-contained Windows x64 folder, runtime included | Clean-machine validation, signing, ARM64, supported Windows-build matrix, performance targets |
| SableType | Planned; no engine advertised as implemented | Original targeting/typing/cancellation and engine evaluation |
| SableLatch/security | Planned; no vault or fake authentication | Shared service, environment binding/recovery, standalone/integrated editions, scoped entry operations |
| Workflows/API/addons | Requirements and architecture only | Runtime, typed versioned cooperation, discovery, installer/catalog and security boundaries |

## Architecture as implemented

The executable references a separate core library. Core models and operations have no WPF dependency; the desktop host currently dispatches the three built-in action kinds through AppState. This is intentionally a small foundation, not yet the proposed versioned addon API. Do not build third-party integrations against the internal dispatcher.

Windows own presentation and invoke the host/core services. The main configuration and network windows are lazy. The tray uses WinForms NotifyIcon, a message-only WPF HWND for Ctrl+Alt+K, and DWM attributes for supported backdrop/corner behavior. There is no browser runtime, polling clipboard watcher, or external UI control package.

Configuration contains ordinary action settings, never vault contents. Application arguments use ProcessStartInfo.ArgumentList with shell execution disabled. Clipboard network values pass strict validation before network APIs receive them; they are never interpolated into commands. Passwords are not included in activity details. The application is not elevated and does not request UIAccess.

These boundaries do not constitute a finished security review. The protected-data portability/recovery requirement remains mandatory and unimplemented.
