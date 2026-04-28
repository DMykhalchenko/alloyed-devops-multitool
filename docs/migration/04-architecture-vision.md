# Architecture Vision: alloyed-devops-multitool

## Product Direction

Build a .NET-first platform that can analyze PowerShell scripts via AST, transform command calls into generated wrappers, and execute through a decorator-enabled runtime pipeline.

TAF is used as a reference model, not as a direct migration target.

## Scope Decision (Current)

- Keep:
  - AST analysis and deterministic code transformation.
  - Own decoration pipeline (observability, error handling, correlation) — no Castle.DynamicProxy.
  - Builder-centric generation model for code artifacts.
  - System port coverage expanding incrementally (Utility → Diagnostics → Archive → Management → Security → Host).

- Defer:
  - Real PowerShell AST (`System.Management.Automation.Language.Parser`) — currently heuristic; upgrade when text-based approach breaks.
  - Additional decorators (Validation, Caching) — add when concrete scenarios demand them.
  - Dynamic catalog source (YAML/JSON/attributes) — add when hardcoded mappings become a maintenance burden.
  - Simple configuration layer — add before first publish milestone.
  - Script-mocking wrapper generation as a primary feature.
  - Precompiled external tool ports at scale.
  - Sandbox orchestration with fake data/container workflows.
  - VS Code dashboard/UI.

- Dropped permanently:
  - Legacy runtime support (`net472`, Windows PowerShell 5.1).
  - Castle.DynamicProxy dependency.

## Technical Baseline

- Runtime target: .NET 8 LTS (SDK: .NET 10).
- PowerShell target: PowerShell 7+ portable module model.
- Design priority: cross-platform, deterministic, dependency-minimal.

## Architecture Layers

### Core.Ast

- Parse PowerShell source to AST.
- Extract command graph and usage metadata.
- Produce transform plan with traceable replacements.
- Current implementation: `HeuristicScriptAnalyzer` (text-based bridge, no external deps).
- Planned upgrade: `System.Management.Automation.Language.Parser` when heuristic is insufficient.

### Core.Builders

- Reusable builder primitives (class/method/file builders).
- Specialized emitters for PowerShell and C#.
- Deterministic output policies (ordering, formatting, naming).

### Core.Decoration

- Own interception pipeline (no dynamic proxy library).
- Explicit decorator ordering by priority.
- Clear separation of business logic and cross-cutting concerns.
- MVP decorators: ErrorHandling, Observability, Correlation.

### Core.Catalog

- Wrapper and module capability catalog.
- Command-to-wrapper resolution and fallback policies.
- Current implementation: `InMemoryWrapperCatalog` (hardcoded, case-insensitive).
- Planned upgrade: external source (YAML/JSON/attributes) when mapping count warrants it.

### Host.PowerShell

- Portable module assembly and manifest generation.
- `analyze -> resolve -> transform -> assemble` orchestration.

### Tests

- Golden tests for AST transform output.
- Unit tests for decorators/builders/catalog.
- Container-based integration tests (replaces PowerShell smoke tests).

## Non-Functional Properties

- Determinism: same input yields same transform output.
- Composability: decorators and builders remain independently extensible.
- Observability: runtime behavior is inspectable by policy.
- Portability: module works cross-platform under PowerShell 7+.
- Incrementality: each capability can ship in isolated slices.

## Out-of-Scope for Initial Release

- Rich GUI/dashboard.
- Full ecosystem port coverage.
- Advanced sandbox orchestration with synthetic data engines.

## Naming Baseline

- Repository: `alloyed-devops-multitool`
- Root namespace: `Alloyed.DevOps.Multitool`
- PowerShell module: `Alloyed.DevOps.Multitool`
