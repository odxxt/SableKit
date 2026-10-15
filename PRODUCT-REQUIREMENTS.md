# SableKit product requirements

Status: agreed product baseline from the owner's 2026-10-11 brief. Appearance remains subject to review. Implementation details in the companion architecture are proposals.

## 1. Purpose and model

SableKit is a lightweight, portable Windows toolkit for everyday work, especially IT and security tasks.

- A **tool** supplies capabilities, defaults, configuration, and its own interface where needed.
- An **action** is a configured invocation of an operation. Multiple actions can use one tool.
- A **workflow** runs a configured sequence of actions with explicit execution policies.

Detailed customization belongs in the main application. The tray panel finds and executes actions quickly. Opening a tool does not require an embedded tray interface.

## 2. Platform and performance

Use C#, WPF, XAML, and .NET 10. Intended targets are Windows 10 and Windows 11; exact supported builds must be validated against runtime support before release. No control library has been chosen.

Create the main window and tool views only on demand. Load addon implementations when needed. Avoid unnecessary background work and stop hidden animations. Read clipboard content when the panel opens or an action is invoked, rather than continuously monitoring it.

The portable distribution must contain its required runtime. Measure cold/warm startup, tray opening latency, idle memory, and idle CPU on Windows. Report archive/download size, extracted size, and runtime performance separately. Establish budgets from measured prototypes rather than inventing current performance claims.

## 3. UI review required

Before layouts, styling, mockups, or UI code, ask the owner for current preferences and reference images. Review the proposed design and obtain agreement before implementation. Mockups show the popup alone, without desktop, wallpaper, or taskbar.

Background only: compact, modern, minimal visual quality inspired by Raycast; smoky black translucent glass, blur, rounded corners, subtle border, crisp text, and neutral charcoal/gray/white. Earlier blue and amber directions were superseded. Approximately 342 × 411 logical pixels was an earlier reference, followed by a preference for something smaller. Final dimensions are open.

The previous structure to review consists of small search and pin controls, search revealed on demand, a contextual suggestion above pinned actions, a pinned-action list/grid switch, a much smaller tool strip with short names and navigation arrows, and a subtle footer for mascot/name, profile selection, and main-window access. This is not an approved layout.

Current owner clarification on 2026-10-11: a small compact panel comparable to SableLatch, a normal-sized main window for configuration, slight Raycast influence, and a luxurious smoky black blurred feel. This clarifies visual direction; reference images, exact dimensions, density, and layout still need review before implementation.

Support keyboard navigation, visible focus, tooltips, reduced motion, and light/dark/system themes. Keep the mascot subtle. Exact appearance remains open.

## 4. Tray and action behavior

Pinning retains the panel when focus moves elsewhere. Per-action panel behavior supports stay open, close immediately, close after completion, or open the owning tool interface. Define precedence between pinning and an explicit action policy during interaction review.

Actions expose meaningful idle, running, completed, failed, and unavailable states. Toggle operations report actual underlying state. Cancellation must be distinguishable from success or failure.

After successful password copying, the button briefly displays a centered checkmark and “Copied,” then restores its original appearance without changing dimensions. Show success only after the clipboard write succeeds; respect reduced motion.

SableType primarily needs actions and main-window settings. SableLatch hides the SableKit panel and opens its compact vault. SableNet has a compact troubleshooting panel. Larger tools may open in the main window.

## 5. Action customization

Provide a guided editor, accessible advanced settings, a resulting-button preview, configuration preview, and a separate explicit test-run control. Preview must never execute an action.

Configuration covers:

- Name, description, icon, accent, label visibility, size, and success/error feedback.
- Profile placement, groups, order, favorites, tray visibility, search visibility, aliases, and global hotkeys.
- Fixed values, current clipboard, prompted values, selected files, profile variables, and previous-step results.
- Tool defaults versus explicit action overrides, with reset to inheritance.
- Start delay, timeout, cancellation, duplicate-click policy, confirmation, and failure behavior.
- Copy/display/open-main-window/save-file/pass-result output handling.
- Panel behavior, duplication, disabling, export, and understandable missing-dependency explanations.

An inherited setting follows future tool-default changes. An override remains unchanged. For example, updating SableType's default typing speed changes ordinary inheriting actions but preserves a slow-console action's override. Show the effective configuration before an explicit test run.

## 6. Built-in application and command actions

Application launching and command/script execution are host capabilities, requiring no addon. Configure executable or shell, arguments, inline command or script path, working directory, environment, terminal visibility, keep-open behavior, timeout, output handling, and explicit elevation. Visible terminal is the proposed default.

Treat extracted/clipboard input as validated data passed through operation-specific parameters. It must not become executable shell syntax through interpolation. Distinguish an intentionally authored script from untrusted action input. Both visible-terminal and background modes must have accurate output, cancellation, and completion capabilities.

## 7. SableType

Implement original code. Workflow: invoke Type clipboard, hide SableKit, change the cursor to a crosshair, click a destination, then type the captured clipboard text. Escape cancels target selection or interrupts typing. Also support typing directly into the currently focused application.

| Setting | Requirement/default |
| --- | --- |
| Engine | SendInput by default |
| Alternatives requested | AutoIt Send and Forms.SendKeys, subject to dependency, behavior, and redistribution review |
| Start delay | Configurable, 0 ms |
| Keystroke delay | Configurable, 15 ms |
| Long-text confirmation | Optional, disabled; threshold 100 characters |
| Global shortcut | Ctrl+Alt+V; customizable or removable |
| Modifiers | Control, Alt, Shift, Windows |
| Shortcut mode | Select destination or type into current focus |
| Cancellation | Escape while targeting or typing |

Account for a fixed 100 ms settling delay plus the configured delay, focus acquisition, and modifier-key release. Provide global defaults and per-action overrides. Initial actions read the current clipboard; saved-text actions are a later extension.

Research behavior without copying third-party implementation. Keep the research project's name out of application text, source comments, and project documentation. Preserve all required notices for any components actually reused. Do not expose an unsupported engine as implemented. Record Windows integrity-level and UIAccess restrictions; portability does not promise typing into every elevated application.

## 8. SableNet

On opening, inspect the clipboard once and populate a valid target. Users can edit the target or explicitly Paste. Later clipboard changes must not silently replace visible input; subsequent checks use that input.

The compact panel needs an editable IP/hostname, Paste, optional port, ping, continuous ping, trace route, DNS lookup, reverse lookup, and configurable TCP buttons such as SSH, HTTPS, RDP, SMB, and custom ports. Provide results, elapsed time, Stop, Retry, Copy result, and Open in terminal.

Main-window configuration includes saved targets, port presets, timeouts, ping counts, output preferences, subnet calculations, and broader settings.

| Input or action | Behavior |
| --- | --- |
| `10.0.0.25`, Check clipboard address | Ping |
| `10.0.0.25:443` | TCP test, port 443 |
| `[2001:db8::25]:443` | IPv6 TCP test, port 443 |
| Ping clipboard IP | Command Prompt running ping with the validated extracted address |
| Check HTTPS | TCP reachability on port 443 |
| Saved target action | Configured target |
| Multiple addresses | Explicit target picker |
| Invalid explicit port | Explain and allow correction; never downgrade silently to ping |

Extract candidates, then properly validate addresses, hostnames, and ports. Preserve the distinction between absent and invalid ports. Distinguish TCP reachability from application-service health.

## 9. Clipboard suggestions

Inspect locally when the tray opens. Suggest installed-tool operations such as Ping/Open SableNet for an address, TCP test for address plus port, or Format JSON if a suitable tool is installed. Suggestions execute only when clicked; hide the area when there is no useful suggestion. Do not log clipboard contents by default.

## 10. Profiles

Ship a ready-to-use Default profile. Allow Lab, Work, Personal, and customer-specific profiles from the first release. Use a shared action library; each profile chooses pinned actions, order, and preferred view. Reuse action references or duplicate actions for independent settings.

Include tray profile switching, global favorites, and profile variables. Automatic active-application profile switching is a later optional enhancement.

## 11. Workflows

Combine sequences and workspace launchers into workflows. “Start lab work” authenticates through SableLatch, launches selected saved RDP connections, opens configured ESXi pages, and opens folders, notes, terminals, and other applications.

Support step ordering, configured inputs/delays, explicit completion conditions, compatible result passing, cancellation, and failure policy. Continuing independent steps after a failure requires an explicit setting; dependent steps cannot run without valid required results. Hotkeys invoke workflows as well as actions. Window positioning and monitor layouts are later enhancements.

## 12. Shared versioned API

Expose operations rather than automating another tool's UI. Support discovery, compatible API versions, accepted input/output types, execution, progress, completion, failure, cancellation, defaults/overrides, opening tool interfaces, passing compatible results, creating actions from a tool, and appropriate access control.

“Add to SableKit” in SableLatch lets the user select an entry and operation, then customize the action. Store an opaque entry reference, never its password. Updates affect future runs; deletion makes the action unavailable with a clear explanation. Protected operations enforce their own authentication. Other tools can cooperate, for example transforming text before SableType.

## 13. SableLatch and shared security

Provide standalone and integrated editions, sharing useful vault core where practical and using different startup/authentication integration. Encrypted export/import is sufficient; automatic migration is not required.

In the integrated edition, SableKit owns shared master-password, recovery-key, and optional-PIN setup. Defer setup until enabling the first protected tool, unless that tool is enabled during onboarding. Ordinary launch, typing, password generation, and networking work without vault setup. Future protected tools reuse appropriate setup, while protected entry actions still enforce required authentication.

Implement a real shared security service. Sharing authentication must not expose vault contents or recovery material to every addon. Moving existing protected data to a different Windows environment must require the recovery key: the master password alone must not unlock it there. Define and test the Windows-environment boundary before claiming compliance.

## 14. Addons and default bundle

Start with a curated approved catalog plus manual community ZIP installation. Additional catalog sources are later. Tools declare operations, configuration, compatibility, dependencies, and required capabilities. Explain missing/incompatible dependencies. Inclusion does not require startup loading or activation.

The intended default bundle contains SableLatch, SableType, SableNet, password generation, application and command/script actions, profiles, action hotkeys, workflows, and clipboard suggestions.

## 15. Scope beyond the initial prototype

Early priorities include reusable presets/templates, aliases, prompted inputs, profile variables, button states, and shared API foundations. Further ideas include text transformations, action/profile export/import, optional context profiles, and workspace positioning. Imports identify missing tools and request local paths or credential references instead of copying secrets into action settings.

Expansion tools, not confirmed initial-bundle commitments:

- SableLens: certificate details, SANs, expiry, fingerprints, chains.
- SableDiff: configuration/text comparison.
- SableText: JSON/XML formatting, encoding, cleanup, deduplication.
- SableCheck: file hashes and checksum comparison.
- SableMask: sensitive-text redaction.

## 16. Delivery rules and open decisions

Inspect the new repository before assuming implementation. Document requirements, architecture, milestones, and actual status. After design agreement, deliver a working tray prototype demonstrating opening, pinning, search, list/grid, profile switching, real action execution, and results. Build the default bundle incrementally without silently substituting mocks.

Open decisions include current appearance/reference images; dimensions and density; control-library evaluation; pin/action-policy precedence; precise Windows support matrix; addon trust/process isolation; security environment binding and recovery protocol; alternative typing-engine feasibility; and performance budgets. These require focused review at their implementation stages, not speculative UI choices now.


## Approved compact interaction refinements (2026-10-13)

Preserve moved tray position across reopening and persist it; double-click the brand to reset bottom-right like SableLatch. Tray clicks toggle visibility even when pinned. Use its installed Windows icon family and borderless active-fill header treatment without a Pinned suffix, underline, or gray outer window stroke. Keep the existing main window within the monitor work area.

The compact footer switches profiles horizontally with arrows and mouse wheel. Show quick-action output in the compact panel first, then explicitly open its owning tool with the same result. Keep the saved-action heading stable.

SableNet should be a compact panel with address followed by a separate optional port field. Presets fill the field without running. The primary check action belongs at the bottom and Enter invokes it. Show actual Windows ping and Test-NetConnection output. Inspect copied IPs, hostnames and websites on opening; once visible, retain the edited target until Paste is explicitly used. Infer a nearby common port in prose, or an explicitly labeled valid port, and ignore out-of-range loosely associated numbers. An invalid colon-delimited port remains a correction error. Multiple detected targets require a choice. Clipboard suggestions remain click-to-run.

## UI refinement approved 2026-10-14
- Remove gray native window frames and preserve rounded transparent outer corners across tray, SableNet, main and action settings.
- Open configuration from the tray header Settings control; remove the footer Open app control.
- Give profile-wheel switching a dead zone and short settling interval; bound and ease content scrolling while honoring reduced motion.
- Treat password generation, application launching and future workflows as core SableKit capabilities, not separate installed tools.
- Default the main window to an action grid with simple horizontal top tabs. Selecting an action opens a separate settings window.
- Keep a live, non-executing button preview visible while the new/edit action form scrolls. Protect unsaved drafts independently.
- Use fill/foreground changes for visible keyboard focus instead of click outlines on buttons and navigation.

## Further approved refinement - 2026-10-14
The owner rejected the cramped action form and the non-Actions main pages. Main/editor content must be opaque; action settings shorter and wider, with one editor reused when switching actions and unsaved drafts protected. Keep the action grid, make search expandable, and give top tabs fixed positions. Reorganize Profiles, Preferences, Activity and Tools around their actual tasks; start new actions with a clear choice of purpose. Match SableLatch's 342 x 411 DIP compact size and 18-DIP default/reset offset, add an actions/tools separator, move the profile control to the side without paired arrows, and replace list/grid view glyphs.


## Owner refinement, 2026-10-15

Action editing must disable and dim its owner and visually separate the dialog. Save closes the editor and briefly highlights the saved action. Expose list and grid directly in both action surfaces, and expand search inline beside its icon. Remove Activity. Make the main window resizable within useful screen-relative limits; opening a compact tool hides the main window.

Create profiles through one button and a faded inline draft; discard an unnamed draft on click-away and rename by double-click. Add shared actions to profiles and reorder through drag/drop in Profiles and the quick panel, following SableLatch's gesture and motion behavior. Share the footer row between tool shortcuts and profiles; use icon-only tools above two tools or when space requires it. Refine SableNet's address/port/output hierarchy without replacing native command output.
