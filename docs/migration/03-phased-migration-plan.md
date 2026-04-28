# Incremental Migration Plan: TAF to Alloyed DevOps Multitool

## Assumptions

- Migration goal is conceptual hardening first, production extraction second.
- We prioritize low-risk progress over speed.
- No external deadlines — personal portfolio project.

## Phase 0 — Baseline Freeze ✓

Deliverables: `01-goal-intake.md`, `02-resource-study.md`, this phased plan.

Verification: documents reviewed for factual consistency against `C:\Stash\TAF` source.

Status: **complete.**

## Phase 1 — Alloy Specification ✓

Deliverables: `04-architecture-vision.md`, `05-mvp-scope.md`, `06-naming.md`, `07-blueprint-v1.md`.

Verification: spec maps every core component to concrete source paths or contract definitions.

Status: **complete.**

## Phase 2 — MVP Vertical Slice ✓

Scope: Iterations 0–5 — scaffold, AST (heuristic), catalog, transform, builders, decoration, host pipeline, PowerShell cmdlets.

Verification:

- All unit and integration tests pass.
- CI runs cross-platform (Windows + Linux via GitHub Actions).
- PowerShell smoke test imports generated module and executes transformed sample.

Status: **complete** (2026-04-25).

## Phase 3 — CI/CD Hardening ✓

Scope:

- GitHub Actions triggers on `pull_request` only (removed `push:` trigger).
- Pre-push git hook: build + format check + PS lint + unit tests, runs locally before every push.
- Hook installed via `dev.ps1 -Stage setup`.

Safety checkpoint: no external services involved — hook runs entirely locally.

Verification:

- `push` to any branch does not trigger GHA.
- PRs trigger full pipeline on GHA (Windows + Linux matrix).
- `git push` runs hook automatically; violations block the push.

Rollback: `git config --unset core.hooksPath` removes the hook; restore `push:` trigger in `ci.yml` if needed.

## Phase 4 — System Ports Expansion (iterative)

Scope: expand `InMemoryWrapperCatalog` and generated wrappers one port group at a time.

Port group priority order:

1. Utility (broadest applicability)
1. Diagnostics
1. Archive
1. Management
1. Security
1. Host

Each group: add catalog mappings → implement wrapper stubs → add golden tests → update integration test fixtures.

Safety checkpoint: each group ships as an isolated increment; no cross-group dependencies required.

Verification:

- All commands in a group transform correctly (golden tests pass).
- Integration test covers at least one end-to-end transform per new group.
- No regressions in previously covered groups.

Rollback: revert catalog entries and test fixtures for the affected group.

## Phase 5 — Real PowerShell AST

Scope: replace `HeuristicScriptAnalyzer` with an implementation backed by `System.Management.Automation.Language.Parser`.

Trigger: text-based transformation produces incorrect output for a concrete script (splatting, script-blocks, dynamic expressions).

Safety checkpoint: `IScriptAnalyzer` interface is stable — external callers unaffected by internal swap.

Verification:

- All existing golden tests continue to pass with new analyzer.
- New test cases cover patterns that previously required workarounds.

Rollback: revert `Core.Ast` implementation; interface contract unchanged.

## Phase 6 — Catalog and Config Maturation

Scope:

- Dynamic catalog source: load mappings from YAML/JSON file or C# attributes.
- Minimal configuration layer: settings object for pipeline behavior (output path defaults, decorator toggles, catalog source path).

Trigger for dynamic catalog: hardcoded mapping count makes additions error-prone.

Trigger for config: first scenario where pipeline defaults need to vary by caller.

Verification:

- Catalog loaded from external file matches hardcoded baseline (parity test).
- Config values flow through pipeline and affect observable output.

Rollback: revert to `InMemoryWrapperCatalog` with hardcoded entries.

## Phase 7 — Additional Decorators

Scope: add `ValidationDecorator` and `CachingDecorator` when concrete use cases arise.

Trigger: specific port group where input validation or repeated-call caching provides measurable value.

Verification: decorator ordering tests updated; new decorator does not break existing chain.

Rollback: remove decorator and its registration; chain falls back to three-decorator baseline.

## Phase 8 — Publishing

Scope: PSGallery and/or NuGet publish pipeline in Jenkins.

Trigger: project can port and decorate commands from all six system port groups.

Verification:

- Published module installs cleanly from gallery on a clean PowerShell 7+ instance.
- Version and manifest metadata are accurate.

## Migration Invariants (Must Hold Across All Phases)

- AST transformation remains deterministic and inspectable.
- Decoration stays composable and externalized (no business-logic pollution).
- Wrapper generation conventions remain explicit and test-covered.
- Every phase is independently reversible.
- PowerShell 7+ / .NET 8+ only — no legacy target reintroduced.
