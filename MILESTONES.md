# Implementation milestones

Status on 2026-10-13: the new repository was inspected, the owner approved direct WPF implementation, and prototype 0.1.2 now provides working portions of stages 1, 2, 3 and 5. A self-contained Windows x64 binary exists. Core and in-process WPF checks ran on Windows 11. See [implementation status](IMPLEMENTATION-STATUS.md) and [verification](VERIFICATION.md) for the precise boundary. No entire stage is declared complete while its required checks or features remain outstanding.

## Stage 0 — establish the actual baseline

- Confirmed repository: `odxxt/SableKit`; establish these documents as its initial baseline.
- Inspect any instructions/files added since the empty-repository check; do not carry forward claims from the deleted repository.
- Capture current UI preferences and references before visual design. The owner chose direct implementation of the working interface rather than a mockup review; that authorization is recorded in PRODUCT.md.
- Make the .NET 10 SDK available for development and record the selected SDK. The installed runtime alone cannot build the application.
- Validate the Windows support matrix and identify Windows 10/11 test environments.

Exit: repository documentation established, explicit UI design agreement recorded, build prerequisites available. Non-UI contract work can proceed independently of visual decisions after repository confirmation.

## Stage 1 — build and domain foundations

- Set up solution/project boundaries, reproducible builds, and Windows CI.
- Implement action/profile models, shared library references, explicit overrides, defaults resolution, typed operation contracts, availability, and versioned persistence.
- Supply a Default profile and real password generation/application-launch operations.
- Keep main/tool windows and protected setup deferred.

Verification: build on Windows; test inheritance including false/zero overrides, shared versus duplicated actions, configuration round trips/migrations, unavailable dependencies, operation result/cancellation states, and preview without execution.

## Stage 2 — first working tray prototype, after UI agreement

- Implement approved tray opening, pinning, on-demand search, pinned-action list/grid switching, and profile switching.
- Execute real password generation/copy and application launch; display actual results/errors and consistent states.
- Implement approved per-action panel behavior and the dimension-stable “Copied” feedback after successful clipboard writes.
- Implement keyboard focus/navigation and reduced-motion behavior; create the main window only when requested.

Verification: exercise the complete interaction checklist on Windows; deliberately fail a clipboard write and confirm no false success; verify pin/focus behavior, profile persistence, keyboard access, and action search. Measure startup, panel latency, idle memory, and CPU. Clearly label this a prototype, not the full bundle.

## Stage 3 — action authoring and host execution

- Implement guided editing, explicit test-run, configuration/button preview, inheritance/reset, placement, aliases, favorites, duplication/disabling, and presets.
- Add hotkeys for actions/workflows with conflict reporting, prompted/file/profile-variable inputs, execution policies, and result routing.
- Implement application and command/script options, visible/background execution, output handling, timeouts, and explicit elevation.

Verification: preview never executes; shell metacharacters in clipboard inputs remain data; path/argument handling works with spaces/Unicode; hotkey conflicts are understandable; cancellation/timeout and elevated/terminal limitations are recorded accurately.

## Stage 4 — SableType

- Implement original SendInput provider, target-selection cursor flow, direct-focus mode, Escape cancellation, settling/modifier release, defaults, and per-action overrides.
- Validate optional long-text confirmation and stated timing defaults.
- Evaluate alternative engine dependencies, behavior, and redistribution before implementation; show them as unavailable/planned until proven.

Verification: Windows 10/11 targeting and cursor restoration, cancellation at every phase, multiline/Unicode text, keyboard layouts, held modifiers, empty/unavailable clipboard, long input, focus changes, and elevated-target limitations. Record observed behavior rather than universal compatibility claims.

## Stage 5 — SableNet and clipboard suggestions

- Implement validated target extraction including IPv4, IPv6, hostnames, explicit ports, ambiguous/multiple candidates, and correction flows.
- Add ping/continuous ping, trace route, DNS/reverse DNS, TCP presets/custom ports, elapsed time, cancellation, retry/copy/terminal output, and saved-target settings/subnet calculations.
- Inspect clipboard on opening; subsequent checks use the visible target. Add local installed-tool suggestion discovery; execute suggestions only on click.

Verification: valid/invalid/boundary ports, bracketed/bare IPv6, multiple candidates, changed clipboard after opening, unreachable hosts, DNS failures, cancellation, and terminal parameter safety. Record network-dependent results and avoid equating TCP success with service health.

## Stage 6 — workflows and cooperation API

- Implement ordered steps, result typing/binding, delays, completion conditions, cancellation, failure/independence rules, and workflow hotkeys.
- Implement action creation from a tool, interface requests, API compatibility validation, and progress/results across boundaries.
- Demonstrate a workspace launcher using functioning installed operations; protected lab steps remain unavailable until the security stage passes.

Verification: mismatched types, deleted actions, action snapshots, profile changes during execution, failed prerequisites, explicit independent-step continuation, cancellation, and observable completion conditions.

## Stage 7 — shared security and SableLatch

- Inspect actual SableLatch source before choosing shared components.
- Review threat model, environment binding, key hierarchy, recovery, PIN/session policies, process boundaries, and addon access.
- Implement deferred shared setup, standalone/integrated host separation, protected operations, opaque entry references, and encrypted export/import.
- Complete “Add to SableKit” and the authenticated lab workflow with real vault entries and selected connections/pages.

Verification: ordinary tools need no vault setup; protected operations require scoped authentication; entry update/delete is respected; unrelated addons cannot obtain vault/recovery secrets through the service; master password alone cannot unlock copied protected data in a different environment; recovery explicitly restores access. Do not claim security completion without these tests and design review.

## Stage 8 — addon installation and full-bundle portability

- Implement approved catalog and validated community ZIP installation with compatibility/dependency explanations and on-demand loading.
- Complete remaining first-release authoring/configuration capabilities and bundle integration.
- Publish self-contained portable candidates and verify a clean machine without a separately installed .NET runtime.
- Record package size separately from runtime performance and document supported Windows builds/architectures.

Verification: malformed/path-traversal ZIP rejection, oversized archive handling, incompatible manifests, missing dependencies, update/rollback, writable/read-only paths, deferred protected setup, cleanup/shutdown, clean-machine launch, and full Windows interaction regressions.

## Release evidence

For each stage, record commit/build, feature status, test environment, checks performed, observed failures, remaining limitations, and artifact location. Keep planned, prototype, implemented, and Windows-verified claims separate. Set performance targets after the first baseline and track regressions under comparable conditions.

The intended bundle is complete only when SableLatch, SableType, SableNet, password generation, app/command actions, profiles, hotkeys, workflows, and clipboard suggestions actually function together. Expansion tools, saved-text typing, automatic profiles, extra catalog sources, and workspace window positioning stay outside the initial commitment unless separately approved.
