# ADR-0003: Bogus Sandbox Boundary

Status: Accepted  
Date: 2026-04-28  
Related issue: `#9`

## Context

Integration tests exercise the real pipeline, but they use hand-crafted scripts that are tightly coupled to specific command names and argument shapes. Adding coverage for edge cases (aliases, embedded strings, large inputs, partial catalogs) multiplies fixture maintenance burden. We need a way to generate varied, deterministic inputs without coupling to specific file content.

## Decision

Bogus is adopted as an emulation layer for input data only, confined to a dedicated `Tests.Sandbox` project. The boundary is strict:

**Real (multitool-controlled):** `PowerShellScriptAnalyzer`, `InMemoryWrapperCatalog`, `TextCommandTransformer`, `MinimalModuleBuilder`, `TransformationPipeline` — all execute unmodified in sandbox tests.

**Emulated (Bogus-generated):** script content, command name selection, argument values, module names.

Rules:
1. `Bogus` is allowed only in `Tests.Sandbox`.
2. `Core.*` and `Host.PowerShell` have no Bogus dependency.
3. All seeds are fixed constants in `SandboxSeeds.cs` — changing a seed is a breaking change to the scenario.
4. Per-instance seeding (`new Faker { Random = new Randomizer(seed) }`) must be used; no global `Randomizer.Seed`.

## Consequences

Positive:
- edge-case coverage (aliases, embedded strings, large inputs, partial catalogs) without manual fixture authoring,
- deterministic and reproducible across runs and environments,
- CI-safe — no external secrets, no file dependencies beyond temp dir.

Tradeoff:
- generated scripts are synthetic; they may not represent real-world script complexity or nested constructs.

## Implementation Phases

1. Add `Bogus` to `Directory.Packages.props` (central version management).
2. Create `Tests.Sandbox` project with dependency on `Host.PowerShell` only.
3. Implement `SandboxScriptFaker` with catalog/non-catalog/alias constants and deterministic generation methods.
4. Write `SandboxPipelineScenarioTests` with seven deterministic scenarios covering: all-catalog, mixed, unknown-only, empty, large, aliases, and embedded-string preservation.
