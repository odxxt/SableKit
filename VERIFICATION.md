# Verification — prototype 0.1.1 UI fixes

Recorded 2026-10-13. Release build/publish passed on Windows 11 build 26200 with SDK 10.0.401 / runtime 10.0.12. The WPF harness passed **37 checks**, including the existing action/clipboard/network/draft checks and these UI regressions:

- Borderless pin control, explicit pinned/unpinned title, and persistent active search fill.
- Small icons lay out at their requested dimensions, allowing one device pixel for layout rounding.
- Both default grid rows fit with no vertical scrolling; the tool strip has room for complete icons and labels.
- Initial main-window bounds fit the current monitor work area.
- Visible minimize/maximize/restore/close commands execute successfully.
- Main-window fitting stays within simulated logical work areas of 853 × 480, 1024 × 700 (negative monitor origin), and 1920 × 1040.
- Thirteen fresh WPF client-content captures inspected across list/grid/search/pinned/unpinned/Copied, main normal/small/scaled-workarea, network and light/dark states.

The work-area simulations are layout checks, not physical multi-monitor/DPI-switch tests. Captures include the new WPF title bar, but still do not capture DWM blur. Physical input, live monitor/DPI changes, high contrast, compositor appearance and Windows 10 remain pending. Performance figures below are historical 0.1 baselines; this patch has not remeasured normal idle performance. The harness now starts with fresh diagnostic defaults on each verification run; normal user settings are unchanged.

---

# Verification — prototype 0.1

Recorded 2026-10-12 on Windows 11 x64, OS build 26200, .NET SDK 10.0.401 and published runtime 10.0.12. This documents local evidence; a CI workflow being present is not a claim that its remote run passed.

## Passed

- Release WPF build and self-contained x64 publish: zero warnings and errors.
- 32 core checks: settings defaults/overrides and validation, persistence, malformed-file preservation, target/address/port validation, password properties, real local TCP connectivity and cancellation.
- 23 in-process WPF checks: panel dimensions, suggestion detection, pin state, search/empty results, grid persistence, profile references, actual clipboard write before Copied, focus through running/Copied/idle, fixed feedback dimensions, actual local TCP result, retained target after clipboard changes, invalid-port rejection, cancelled draft navigation, filtering with a draft, duplicate validation and unsaved protection, and preference draft/save behavior.
- Ten rendered client-content captures inspected: compact list/grid/Copied/light; main Actions/Profiles/Preferences/minimum size/light; network results. An independent Impeccable finish review identified contrast and draft/focus issues; fixes were exercised by the UI checks.

The WPF harness raises routed events and uses real controls and clipboard/network APIs. Its screenshots use RenderTargetBitmap, so they show client content, not the compositor's blur or native title bar. This does not substitute for physical keyboard/mouse and screen-reader testing.

## Performance baseline

Separate Release processes created only the normal tray/hotkey/panel path. Each used a five-second visible CPU sample, a five-second hidden settling period, a five-second hidden CPU sample, and five warm openings. UI-idle timestamps do not measure display presentation. CPU percentages below are relative to one logical core. These are single short local runs, not hardware-independent targets.

| Measurement | Native backdrop requested | Opaque comparison |
|---|---:|---:|
| Process start to first UI idle | 1,382 ms | 1,279 ms |
| Warm panel open to UI idle | 7.7–21.8 ms | 8.0–14.1 ms |
| Visible working set / private bytes | 174.6 / 100.9 MiB | 172.5 / 99.0 MiB |
| Hidden working set / private bytes | 158.6 / 85.9 MiB | 156.7 / 83.1 MiB |
| Visible CPU | 1.25% | 0.94% |
| Hidden CPU | 0.31% | 0.00% |

WPF render tier was 2; the session was local. Windows accepted the native backdrop API request. This is not visual confirmation of the resulting blur. The memory figures are an initial baseline requiring optimization and broader measurement, not proof of a finished lightweight target. Runtime-inclusive download size is reported separately with the portable artifact.

The screenshot-heavy verification process has much higher CPU/memory readings (about 49% of one core in its final short sample); those measurements are retained in its raw report and are not used as the normal idle baseline. The distinction matters because rendering multiple diagnostic windows is a different workload.

## Still required

- Physical tray clicks, global hotkey/focus-loss behavior, end-to-end keyboard navigation and screen-reader behavior.
- Compositor blur, transparency disabled, high contrast, reduced-motion OS changes, multiple DPI/monitors, remote desktop, and Windows 10 fallback.
- Deliberately contested/failed clipboard writes; code handles errors, but contention failure has not been induced in this run.
- Application launching across real programs, Unicode/path/argument cases, missing dependencies and hotkey conflicts under actual usage.
- A clean Windows machine without an installed .NET runtime, unsigned-download launch behavior, read-only/external-media paths, longer idle sampling and regression targets.
- Full default-bundle tests once its currently planned modules exist.

No Windows 10 run, physical-input automation, clean-machine runtime test, protected vault, typing engine, or completed workflow is claimed. The independent design review resolved six implementation findings; native material/accessibility validation remains partial, so its finish disposition remains **fix**, not **ship**. This artifact is for prototype review.
