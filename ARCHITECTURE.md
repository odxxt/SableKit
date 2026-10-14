# Proposed architecture

Status: target architecture. Prototype 0.1 now implements a small core library and WPF host, described in [implementation status](IMPLEMENTATION-STATUS.md). Names below are proposed future boundaries, not claims that all those projects or services exist. Review current Microsoft platform documentation when implementing native/runtime details.

## Dependency boundaries

| Boundary | Responsibility |
| --- | --- |
| Contracts | Versioned tool manifests, operation descriptors, typed inputs/results, progress, cancellation, interface requests |
| Domain | Action library, profiles, workflows, settings inheritance, validation, availability; no WPF dependencies |
| Application | Input resolution, execution coordination, workflow orchestration, result routing, capability checks |
| Windows services | Clipboard, global hotkeys, process/terminal launch, focus/typing, native tray and operating-system integration |
| WPF host | Approved tray/main/tool presentation, lazy window lifecycle, accessibility, visible action states |
| Built-in operations | Application and command/script actions, independent of addon installation |
| Tool modules | Password generation, SableType, SableNet, integrated SableLatch; implementation loaded on demand |
| Security service | Environment-bound protected storage, authentication sessions, narrowly scoped protected operations |
| Persistence/catalog | Versioned configuration, migrations, atomic writes, catalog/install validation, dependency resolution |

Use presentation-independent contracts and view models; choose exact MVVM/control dependencies only after review. Discover manifests without constructing every tool or its views. Distinguish metadata availability, loaded implementation, activated protected capability, and open interface.

## Configuration and execution

An action has a stable ID, tool/operation reference and compatible version, input bindings, explicitly stored overrides, execution/result/panel policy, presentation metadata, aliases, and optional hotkey. Profile placement references IDs and stores ordering/view preferences. Keep global favorites separate from profile membership.

Represent inheritance explicitly. Absence of an override means “Use tool default”; values such as zero, false, or empty text must remain valid deliberate overrides where allowed. Reset removes the override. Resolve defaults plus overrides at execution time and capture an immutable effective-settings snapshot for that run. Preview uses the same resolver but cannot call execution.

Input bindings identify source and expected type. Resolve clipboard once at the appropriate invocation boundary; never let an unrelated clipboard change retarget an active operation. Resolve workflow profile variables against a run snapshot so profile switching cannot mutate a running workflow. Sensitive values must not be written to action configuration or ordinary diagnostics.

Execution flow: resolve operation and dependencies, validate capabilities and input types, resolve settings, request required user input/authentication, apply confirmation/duplicate-click policy, execute asynchronously, publish progress, then route a typed result. Terminal states include completed, failed, cancelled, and timed out; unavailable is a pre-execution state with a reason. UI completion feedback follows actual operation success.

Timeout and cancellation need operation-specific cleanup, not just abandoning a task. Describe supported cancellation accurately, including processes already launched and partially completed typing. Toggle state comes from the tool. Keep a distinct execution ID so overlapping invocations cannot overwrite one another's result state.

## Windows adapters

- Clipboard access handles thread-affinity and contention; bounded retries must remain cancellable. Password “Copied” follows a confirmed write. Read failures are actionable errors.
- Hotkeys register only configured enabled bindings and report conflicts. Unregister on shutdown or configuration changes. Workflow and action hotkeys share collision handling.
- Process launch separates executable and argument data. Shell-specific operations need a deliberate data-passing strategy; do not concatenate clipboard data into commands. Elevation is explicit. Document output/cancellation limits of visible-terminal and elevated execution.
- SableType uses an explicit targeting/settling/typing/cancelled state machine with cleanup in all exit paths. Preserve cursor state, release hooks, account for held modifiers and focus, and test Unicode/newlines/layouts. Alternative engines are separate providers, unavailable until implemented and validated.
- SableNet parsing produces candidates with absent/valid/invalid port states. Bare IPv6 must not be heuristically split into host and port. Network operations have cancellation/timeouts and structured results; a successful TCP connection does not prove a healthy service.

These are requirements for adapters, not verified native implementations.

## Workflows

Start with ordered execution. Each step identifies an action reference, explicit inputs, expected output types, delay, completion condition, and failure policy. Validate incompatible result bindings and missing references before starting. Define whether action changes affect future workflow runs and snapshot the effective actions for each run.

Independent-step continuation is explicit. A failed authentication prerequisite cannot be bypassed by a generic continue policy. Cancellation stops scheduling further steps and requests cancellation of active work. Report completed, skipped, failed, and cancelled steps accurately. Launch completion and application readiness are different conditions; each operation advertises what it can observe.

## API and addon trust

Manifests describe stable tool/operation IDs, contract compatibility, configuration schema, types, capabilities, dependencies, and interface support. Separate manifest and API versioning from persisted configuration schema versions. Unsupported operations become unavailable with explanations instead of simulated success.

An in-process .NET addon is not a security sandbox. Before supporting community code, choose and document the trust model and process boundary. Capability declarations alone do not isolate hostile code. Protected storage and recovery secrets must remain behind a real security boundary; do not hand raw vault objects, session master keys, or recovery material to addons.

Manual ZIP installation must validate paths, archive size/expansion limits, manifests, compatibility, and dependencies before activation. Prevent path traversal and unsafe link extraction. Stage updates before switching the active version; preserve rollback and user data. Catalog approval and package integrity evidence must be explicit and separately reported.

## Shared security design gate

The owner requires both user authentication and binding to the original Windows environment. A password-only portable decryption path would violate the migration requirement. Before implementation, define what constitutes a different environment, the threat model, key hierarchy, recovery enrollment/rebinding, optional PIN policy, lock/session lifetimes, and backup/export behavior.

Select established primitives and platform facilities after review; do not invent cryptography in this document. Required validation includes copied protected data plus correct password failing in another environment, recovery succeeding through an explicit rebind flow, and addons being unable to retrieve recovery material or unrelated vault entries through the API.

Store opaque protected-entry IDs in action settings. Resolve them and authorize the requested operation on each execution. Deletion/revocation must invalidate availability or execution even when metadata was cached. Authentication reuse is scoped and expires; it is not blanket addon access.

Sharing useful vault core between standalone/integrated editions must not blur their host authentication responsibilities. Inspect the actual SableLatch repository before assuming reusable components exist.

## Storage, portability, and diagnostics

Use a versioned action/profile/workflow store with atomic replacement, migration validation, and recoverable backups. Choose data-folder behavior explicitly for writable portable folders and read-only locations. Keep protected storage separate from ordinary settings. Export portable configuration with unresolved local paths and credential references where appropriate; never export live secrets in action JSON.

Start with self-contained Windows distribution candidates and measure them. Do not assume single-file publishing, trimming, or compression improves latency or memory; verify compatibility and performance before adoption. Runtime servicing must be part of release maintenance.

Record durations, operation IDs, result codes, and dependency failures without routine recording of clipboard text, generated passwords, vault values, command secrets, or sensitive network output. Capture cold/warm start, panel latency distributions, idle private/working-set memory, CPU over a fixed interval, runtime/package sizes, and loaded modules, with hardware/OS/build details.

## Implemented presentation refinement (0.1.3)
The main window owns a set of lazy action-editor windows, one per opened action. Editors retain a captured profile for pin membership and guard their own drafts; main-window closing checks all owned drafts before closing. A shared ActionVisual renderer supplies tray buttons and the editor preview, so previewing settings cannot dispatch execution. Password generation and application actions remain host capabilities. The shell uses WPF layered windows with transparent rounded corners; it currently bypasses the native backdrop path. Content wheel motion is bounded and stops on unload/hidden state, while the profile selector has an independent dead-zone/cooldown gate.
