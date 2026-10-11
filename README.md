# SableKit foundation

Status: product baseline and proposed architecture; no application implemented.

Prepared on 2026-10-11 from the owner's consolidated requirements. The owner confirmed `odxxt/SableKit` as the new repository. This initial documentation establishes the baseline for incremental implementation.

## Inspection

- GitHub repository discovered: https://github.com/odxxt/SableKit (repository ID 1414216860), created 2026-10-11, default branch name `main`.
- GitHub's contents endpoint explicitly reported that the repository is empty. No source, build files, README, or repository instructions were available to inspect.
- The current chat directory is not a Git checkout. It contains `work/` and `outputs/`.
- The available Windows host is x64 and has .NET 10.0.10 and Windows Desktop 10.0.10 runtimes. `dotnet --info` reported no installed SDKs.
- No application build, tests, WPF interactions, or performance measurements have been performed.

## Documents

- [Product requirements](PRODUCT-REQUIREMENTS.md): agreed behavior, defaults, scope, and open decisions.
- [Architecture proposal](ARCHITECTURE.md): boundaries and contracts to refine during implementation.
- [Implementation milestones](MILESTONES.md): incremental delivery and evidence required at each stage.

## UI gate

Ask the owner for current visual preferences and reference images before designing or implementing UI. Previous visual discussions are background, not approval. No layouts, styling, mockups, control library, or UI code have been selected in this foundation.

Current preference, confirmed 2026-10-11: a small compact panel comparable to SableLatch, a normal-sized main configuration window, subtle Raycast influence, and a luxurious smoky black blurred appearance. Reference images, exact dimensions, density, and layout remain pending review.

## Status vocabulary

- **Planned:** described but not implemented.
- **Prototype:** limited, functioning implementation with disclosed gaps.
- **Implemented:** working behavior with recorded verification and limitations.
- **Verified on Windows:** exercised on the named Windows version and environment; not inferred from compilation.

Every feature is currently planned. The first tray prototype is not the completed default bundle.
