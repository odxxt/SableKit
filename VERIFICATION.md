# Verification - prototype 0.1.4 main-window and editing redesign

Recorded 2026-10-14 on Windows 11 build 26200, SDK 10.0.401 / runtime 10.0.12. Release build and self-contained x64 publish passed.

## Current evidence

**94 in-process WPF checks and 46 core checks passed**, with 28 diagnostic client captures. New checks cover: SableLatch-matching 342 x 411 compact dimensions within device-pixel rounding; direct profile choice and dismissal; fixed tab positions across all five pages; fully opaque main/editor surfaces; expandable action search; shorter/wider editor; single-editor reuse with cancelled-discard protection; new-action purpose choice; sticky side preview; operation fields directly available in Setup; and readable panel-behavior choices. Earlier real clipboard, native loopback network output, cancellation, saved placement, action/profile and draft checks still pass.

Visual inspection covered the action chooser, Setup and Behavior, application and network editing, normal/small editor, main Actions/Profiles/Preferences/Tools/Activity, and compact list/grid. Final confirmation checked the readable enum labels and simplified planned-tools disclosure. No independent design approval is claimed for 0.1.4.

The harness uses real WPF controls, clipboard APIs and local command processes, with routed/programmatic input. Captures are RenderTargetBitmap client images. They do not prove physical tray/menu interaction, actual compositor edges, screen-reader behavior or live monitor/DPI transitions. These still require hands-on validation, along with Windows 10 and clean-machine portability.

## Performance sample

One separate Release panel-only process measured startup to UI idle at 1,577 ms; five warm opens at 0.7–1.3 ms. Visible working set/private bytes were 152.1/78.3 MiB, hidden 152.5/79.0 MiB. Both five-second CPU samples measured 0% of one logical core. These are short local samples, not display-presentation timings or performance guarantees. Main/editor windows were not created by this probe. Runtime-inclusive package size is reported separately.

## Scope

Main/editor content is now intentionally opaque, as requested. Compact shells remain translucent without real desktop blur. Only one editor can exist per main window; switching actions protects unsaved changes. New action offers only implemented types. SableType, SableLatch/security, workflows and the addon catalog remain planned. The running older app was not terminated or replaced.

Prior-version results below are historical and do not override this build's behavior or limitations.

---

# Verification - prototype 0.1.3 action-grid and shell refinement

Recorded 2026-10-14 on Windows 11 build 26200, SDK 10.0.401 / runtime 10.0.12. Release WPF build and self-contained x64 publish passed.

## Current evidence

- **46 core checks passed.** Existing parsing, settings, password generation, local network and cancellation checks remain green.
- **76 in-process WPF checks passed.** This includes real clipboard read/write, contested-reader response, stable Copied feedback, native local network commands, target selection, tray placement/toggle logic, and the earlier action/profile flows. New checks cover the action grid, separate protected drafts, non-executing live preview, sticky preview, top tabs, removal of standalone password/Open app shortcuts, profile wheel gating, bounded scrolling, two-column layout at 720 DIP, and outline-free keyboard focus on closed choices.
- **21 client-content captures** cover compact/main/network/editor views, light/dark themes and small/simulated work areas. Rounded corner pixels are transparent and the windows have no WindowChrome instance.
- A fresh Impeccable review found a narrow-grid wrapping defect and invisible dropdown keyboard focus. Both were corrected and their regression checks passed. The original desktop-blur promise remains unmet: current layered shells use alpha translucency and bypass DWM backdrop requests. This prototype is not full visual-material approval.

The 0.1.2 successful-clipboard retest was previously blocked by Windows access denial. Clipboard access was available for this complete 0.1.3 run; the new asynchronous reader and successful copy path passed. Tests temporarily use and restore clipboard contents.

The harness raises routed events and uses actual WPF controls, clipboard APIs and local ping/Test-NetConnection processes. It does not physically click the Windows tray icon or drive a full native mouse/keyboard journey. RenderTargetBitmap captures show client content, not desktop compositing. Transparent-pixel checks do not establish the real desktop edge appearance. Simulated work areas do not validate live DPI/monitor transitions.

## Local opening and idle sample

A separate published Release process created only the normal tray/hotkey/panel path, with no main or tool window. CPU intervals lasted five seconds; warm openings were sampled five times.

| Measurement | Local run |
|---|---:|
| Process start to UI idle | 1,294 ms |
| Warm open to UI idle | 0.9-2.3 ms |
| Visible working set / private bytes | 150.5 / 76.7 MiB |
| Hidden working set / private bytes | 151.5 / 77.8 MiB |
| Visible / hidden CPU, one logical core | 0.31% / 0% |

These are one short local diagnostic sample, with another SableKit version running, not display-present timestamps or performance guarantees. The panel opens without waiting for the asynchronous clipboard read to finish. No DWM backdrop was requested by these transparent shells. Runtime-inclusive download size is separate from memory/CPU usage. The final corrections affect main-grid sizing and dropdown focus, neither created by this panel-only probe.

## Remaining validation and scope

Desktop blur is **not implemented by the current transparent-shell path**. Physical tray clicks/drag/title-double-click, native resizing, full keyboard/screen-reader use, live DPI/monitor transitions, compositor edge appearance, high contrast, Windows 10 and a clean machine without .NET remain unverified. The older running user application was not replaced or terminated. Quit it before launching the new portable build.

SableType, integrated SableLatch/security, workflows, addon API/catalog and broader action customization remain planned. See IMPLEMENTATION-STATUS.md. The prior-version evidence below is retained as history and does not override current results or limitations.

---

# Verification - prototype 0.1.2 compact interaction update

Recorded 2026-10-13 on Windows 11 build 26200, SDK 10.0.401 / runtime 10.0.12. Release WPF build and self-contained x64 publish passed.

## Passed

The 56-check UI run below preceded the final asynchronous clipboard-read change. Its full rerun stopped before modifying the clipboard because Windows returned access denied (Win32 error 5), both inside and outside the sandbox. The final code builds and its opening/idle timing probe passes under that unavailable-clipboard condition; successful clipboard extraction through the new reader still needs a rerun with clipboard access restored. The harness preserves the original clipboard before tests and aborts if it cannot do so.

- **46 core checks**, including URL/address extraction, nearby common/labeled ports, invalid explicit ports, multiple targets, duplicate candidates, oversized input, and the existing settings/password/network checks.
- **56 in-process WPF checks**: compact dimensions, stable pin/search states without a title suffix, search/grid/profiles, actual clipboard copy and stable feedback/focus, native loopback ping and Test-NetConnection output, cancellation, port presets that only populate, multiple-target selection, result handoff and picker cleanup, horizontal result scrolling, saved panel position and tray-toggle event sequence, bottom-right reset, main caption/bounds and unsaved-draft protection.
- **17 client-content captures** across dark/light tray list/grid/search/Copied/results, compact network checks/presets/picker/output, main pages and small/simulated work areas.
- Independent Impeccable review disposition **ship**, at the scope of its two listed fixes: output scrolling and target-picker clarity/cleanup. Both were scored resolved. This is not full product or native-compositor approval.

The harness raises routed events and calls real controls, clipboard and local network processes. It exercises the tray-toggle sequence in process, not by physically clicking the Windows notification icon. Captures use RenderTargetBitmap and do not show DWM blur. Work-area simulation does not validate live monitor or DPI transitions.

## Native network output

The UI captures Windows [ping](https://learn.microsoft.com/en-gb/windows-server/administration/windows-commands/ping), [Test-NetConnection](https://learn.microsoft.com/en-us/powershell/module/nettcpip/test-netconnection?view=windowsserver2025-ps), and nslookup rather than synthesizing their output. The TCP test uses a fixed PowerShell script; validated host/port cross as environment data. Ping/nslookup use literal process arguments. Commands have a 45-second bound and can be cancelled. Test-NetConnection may take several seconds and indicates TCP reachability, not application-service health.

An invalid explicit colon port stays an error. An out-of-range loose prose port is ignored; an unlabeled nearby number is inferred only when it uniquely matches a common-port list. These are bounded heuristics, not arbitrary natural-language understanding. URLs without an explicit port suggest ping. Public DNS/unreachable networks, non-English command-output encoding, and broad hostname/IPv6 prose cases need further coverage.

## Opening and idle measurements

The first short run measured warm opening at 2.7-7.6 ms. A later run exposed a repeatable 1.07-second delay in synchronous clipboard access while Windows denied access. Stage timing isolated that delay to the clipboard read, not search or action layout. The final reader runs off the UI thread, returns a busy state within 250 ms, allows at most one outstanding read, and ignores stale results after another request or manual target edit.

The final Release process, with the clipboard still unavailable, recorded:

| Measurement | Final local run |
|---|---:|
| Process start to UI idle | 2,235 ms |
| Warm open to UI idle | 2.9-4.7 ms |
| Visible working set / private bytes | 158.1 / 85.1 MiB |
| Hidden working set / private bytes | 157.4 / 84.0 MiB |
| Visible / hidden CPU, one logical core | 0% / 0% |

Each CPU sample lasted five seconds. These are single local diagnostic samples with another SableKit version running, not latency guarantees, display-present timestamps, or a clean-machine benchmark. The package size includes the runtime and is separate from memory/CPU use. Full successful-clipboard timing and broader performance targets remain to be established.

## Remaining validation

Physical tray clicks/drag/double-click, full keyboard and screen-reader use, live monitor/DPI changes, compositor blur, high contrast, Windows 10, and a clean machine without .NET remain unverified. The user already has a 0.1.1 process running during this session; it was not terminated or replaced. Quit it before launching the new build. Protected tools, workflows and the rest of the intended bundle remain planned as listed in IMPLEMENTATION-STATUS.md.

---

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
