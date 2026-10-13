# Product
<!-- impeccable:product-schema 1 -->

## Platform
Native Windows desktop (WPF). The skill's web/mobile platform enum does not describe this product; do not convert it to a web app.

## Stack
C#, XAML, WPF, .NET 10; self-contained portable Windows x64 distribution first.

## Users and purpose
Everyday IT/security work. Tools supply capabilities; reusable configured actions execute them; workflows will combine actions. The compact tray is for execution and the main window is for configuration. See PRODUCT-REQUIREMENTS.md for the full agreed baseline.

## Brand commitments
The owner requested a modern, enterprise-quality, lightweight interface: smoky black blur, neutral charcoal and white, subtle Raycast influence. Preserve SableLatch's compact size and familiar pin/search/click-to-act UX while improving UI craft. On 2026-10-11 the owner explicitly authorized implementation and chose building the working WPF interface directly.

## Evidence on hand
SableLatch reference commit 59a077a: MainWindow.xaml is 342 × 411 DIP; committed vault/actions screenshots match the compact dark controls and progressive editor. It uses search on demand, pinning, focus-loss hiding and fixed editor actions. Originally inspected as a UX reference. In 0.1.2 the owner requested matching compact-window behavior and controls: their Windows glyphs, adapted borderless styling, saved placement and title reset are used, with the SableLatch MIT notice preserved. Profiles use arrows and wheel navigation; the tray displays quick results before an explicit tool handoff. SableNet uses a compact address/port form and native command output. No vault implementation is copied.

## Principles
Real execution and truthful status; lazy windows; local data; explicit inheritance; no background clipboard polling; no secrets in ordinary configuration. Initial prototype is a subset of the intended bundle, not a finished security product.

## Accessibility
Keyboard navigation, visible focus, named controls, reduced motion, scalable text, theme choices and high-contrast fallback. Native Windows affordances take priority over ornamental motion.
